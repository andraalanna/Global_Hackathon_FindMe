using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace FindMe
{
    /// <summary>
    /// Turns Gemini's 2D boxes into 3D cues anchored next to the real furniture (plan Phase 5):
    /// box → screen pixels → ray from the capture-time camera → AR raycast → median hit → anchor → cue.
    /// </summary>
    public class SpotPlacer : MonoBehaviour
    {
        [SerializeField] ARRaycastManager raycastManager;
        [SerializeField] ARAnchorManager anchorManager;
        [SerializeField] ARPlaneManager planeManager;
        [SerializeField] GhostCamera ghost;
        [Tooltip("Parent for cues that couldn't get an anchor.")]
        [SerializeField] Transform cuesRoot;
        [Tooltip("Phase 6 prefab. Empty = code-built placeholder.")]
        [SerializeField] SpotCue cuePrefab;
        [Tooltip("Optional pop sound; pitch rises 5% per cue.")]
        [SerializeField] AudioSource popSound;

        [SerializeField] float mergeDistance = 0.35f;   // m: closer than this + similar label = same spot
        [SerializeField] int maxCues = 8;
        [SerializeField] float spawnInterval = 0.15f;
        [Tooltip("Pull the cue this far back toward the camera so it floats in front of the surface.")]
        [SerializeField] float surfaceOffset = 0.03f;

        public IReadOnlyList<SpotCue> Cues => cues;

        const TrackableType HitTypes = TrackableType.PlaneWithinPolygon | TrackableType.PlaneEstimated;

        // (u, v) inside the box, 0..1, v = 0 at the TOP of the box (Gemini's y goes down).
        static readonly Vector2[] SamplePoints =
            { new(0.5f, 0.5f), new(0.3f, 0.4f), new(0.7f, 0.4f), new(0.3f, 0.7f), new(0.7f, 0.7f) };
        // "under the bed": the hiding place is the bottom of the box.
        static readonly Vector2[] UnderPoints =
            { new(0.5f, 0.8f), new(0.3f, 0.7f), new(0.7f, 0.7f), new(0.3f, 0.9f), new(0.7f, 0.9f) };

        readonly List<SpotCue> cues = new();
        readonly List<ARRaycastHit> hits = new();
        readonly List<Candidate> candidates = new();
        readonly List<float> planeDistances = new();

        struct Candidate
        {
            public Vector3 position;
            public float distance;
            public ARPlane plane;
            public bool preferred;
        }

        enum Preference { None, Vertical, Horizontal }

        /// <summary>
        /// Places cues for spots from ONE snapshot (already sorted, highest likelihood first), 150 ms apart.
        /// Returns how many NEW cues were spawned (merged duplicates don't count).
        /// </summary>
        public async Task<int> PlaceSpots(IReadOnlyList<HidingSpot> spots, CapturePose pose, CancellationToken ct)
        {
            cues.RemoveAll(c => c == null);
            int spawned = 0;
            foreach (var spot in spots)
            {
                ct.ThrowIfCancellationRequested();
                var (position, plane) = FindPosition(spot, pose);

                // Same spot seen again (e.g. a second scan): keep the old cue AND its state.
                var existing = FindDuplicate(spot.label, position);
                if (existing != null)
                {
                    existing.Likelihood = Mathf.Max(existing.Likelihood, spot.likelihood);
                    continue;
                }
                if (cues.Count >= maxCues) break;

                Transform parent = await CreateAnchor(position, plane);
                ct.ThrowIfCancellationRequested();
                cues.Add(Spawn(spot, position, parent, cues.Count + 1, spawned));
                spawned++;
                await Awaitable.WaitForSecondsAsync(spawnInterval, ct);   // stagger: one pop at a time
            }
            return spawned;
        }

        /// <summary>Removes every cue and its anchor.</summary>
        public void ClearCues()
        {
            foreach (var cue in cues)
            {
                if (cue == null) continue;
                var anchor = cue.GetComponentInParent<ARAnchor>();
                if (anchor != null && anchorManager != null && anchorManager.enabled) anchorManager.TryRemoveAnchor(anchor);
                Destroy(anchor != null ? anchor.gameObject : cue.gameObject);
            }
            cues.Clear();
        }

        // ---------- box → world position ----------

        /// <summary>Where in the world a spot's box is: median of up to 5 raycast hits, or a guess along the center ray.</summary>
        (Vector3 position, ARPlane plane) FindPosition(HidingSpot spot, CapturePose pose)
        {
            string label = spot.label.ToLowerInvariant();
            var points = label.Contains("under") ? UnderPoints : SamplePoints;
            var preference = PreferenceFor(label);

            candidates.Clear();
            Ray centerRay = default;
            for (int i = 0; i < points.Length; i++)
            {
                Vector2 px = BoxPointToScreen(spot.box_2d, points[i].x, points[i].y, pose);
                Ray ray = ghost.ScreenRay(pose, px);   // from where the phone WAS, not where it is now
                if (i == 0) centerRay = ray;

                // Raycast against the planes as tracked NOW (they may have grown since the snapshot).
                if (!raycastManager.Raycast(ray, hits, HitTypes)) continue;
                candidates.Add(PickHit(ray, preference));
            }

            if (candidates.Count == 0)
                return (centerRay.GetPoint(FallbackDistance(pose)), null);

            // If any ray hit the preferred kind of plane (e.g. vertical for a drawer front), trust only those.
            bool anyPreferred = candidates.Exists(c => c.preferred);
            if (anyPreferred) candidates.RemoveAll(c => !c.preferred);

            // Median by distance: one stray ray that slipped past the furniture onto the far wall can't drag the cue.
            candidates.Sort((a, b) => a.distance.CompareTo(b.distance));
            var median = candidates[candidates.Count / 2];
            return (median.position, median.plane);
        }

        /// <summary>From one ray's hits (closest first), take the closest preferred-alignment hit, else the closest.</summary>
        Candidate PickHit(Ray ray, Preference preference)
        {
            ARRaycastHit chosen = hits[0];
            bool preferred = false;
            foreach (var h in hits)
            {
                if (h.trackable is ARPlane p && Matches(p.alignment, preference))
                {
                    chosen = h;
                    preferred = true;
                    break;
                }
            }
            return new Candidate
            {
                position = chosen.pose.position - ray.direction * surfaceOffset,
                distance = chosen.distance,
                plane = chosen.trackable as ARPlane,
                preferred = preferred,
            };
        }

        static Preference PreferenceFor(string label)
        {
            if (label.Contains("drawer") || label.Contains("cabinet") || label.Contains("wardrobe") || label.Contains("door"))
                return Preference.Vertical;
            if (label.Contains("bed") || label.Contains("desk") || label.Contains("pile") || label.Contains("table") || label.Contains("floor"))
                return Preference.Horizontal;
            return Preference.None;
        }

        static bool Matches(PlaneAlignment a, Preference p) =>
            (p == Preference.Vertical && a.IsVertical()) || (p == Preference.Horizontal && a.IsHorizontal());

        /// <summary>No hit: guess the depth from how far the planes the camera could see were (median, 0.6–3.5 m).</summary>
        float FallbackDistance(CapturePose pose)
        {
            planeDistances.Clear();
            foreach (var plane in planeManager.trackables)
            {
                Vector3 vp = ghost.WorldToViewport(pose, plane.center);
                if (vp.z > 0f && vp.x is >= 0f and <= 1f && vp.y is >= 0f and <= 1f)
                    planeDistances.Add(Vector3.Distance(pose.position, plane.center));
            }
            if (planeDistances.Count == 0) return 1.5f;
            planeDistances.Sort();
            return Mathf.Clamp(planeDistances[planeDistances.Count / 2], 0.6f, 3.5f);
        }

        /// <summary>
        /// Gemini box point → Unity screen pixel. Gemini: [ymin, xmin, ymax, xmax] in 0..1000, origin TOP-left.
        /// Unity screen: pixels, origin BOTTOM-left. (u, v) pick a point inside the box; (0.5, 0.5) = center.
        /// </summary>
        public static Vector2 BoxPointToScreen(int[] b, float u, float v, CapturePose p)
        {
            float x01 = Mathf.Lerp(b[1], b[3], u) / 1000f;
            float y01 = Mathf.Lerp(b[0], b[2], v) / 1000f;
            return new Vector2(x01 * p.screenWidth, (1f - y01) * p.screenHeight);   // 1 - y: flip Y
        }

        // ---------- merge ----------

        SpotCue FindDuplicate(string label, Vector3 position)
        {
            foreach (var cue in cues)
                if (Vector3.Distance(cue.transform.position, position) <= mergeDistance && LabelsSimilar(cue.Label, label))
                    return cue;
            return null;
        }

        static readonly string[] Filler = { "the", "of", "on", "in", "a", "an", "top", "under", "side" };

        /// <summary>"drawer" ≈ "top drawer" (one contains the other); "pile of clothes" ≈ "clothes pile" (shared word).</summary>
        public static bool LabelsSimilar(string a, string b)
        {
            a = (a ?? "").Trim().ToLowerInvariant();
            b = (b ?? "").Trim().ToLowerInvariant();
            if (a.Length == 0 || b.Length == 0) return false;
            if (a.Contains(b) || b.Contains(a)) return true;

            foreach (var wa in a.Split(' '))
            {
                if (wa.Length < 3 || System.Array.IndexOf(Filler, wa) >= 0) continue;
                foreach (var wb in b.Split(' '))
                    if (wa == wb) return true;
            }
            return false;
        }

        // ---------- anchor + spawn ----------

        /// <summary>
        /// AR Foundation 6.5 API. Plane hit → anchor attached to that plane (follows the plane's corrections);
        /// otherwise a free anchor. Upright rotation so cue visuals stay vertical even on walls.
        /// Returns the transform to parent the cue under (null if anchoring failed).
        /// </summary>
        async Task<Transform> CreateAnchor(Vector3 position, ARPlane plane)
        {
            if (anchorManager == null || !anchorManager.enabled || anchorManager.subsystem == null) return null;
            var pose = new Pose(position, Quaternion.identity);

            if (plane != null)
            {
                ARAnchor attached = anchorManager.AttachAnchor(plane, pose);
                if (attached != null) return attached.transform;
            }

            var result = await anchorManager.TryAddAnchorAsync(pose);
            if (result.status.IsSuccess() && result.value != null) return result.value.transform;

            Debug.LogWarning($"[SpotPlacer] Anchor failed ({result.status}); cue will not follow tracking corrections.");
            return null;
        }

        SpotCue Spawn(HidingSpot spot, Vector3 position, Transform anchor, int priority, int popIndex)
        {
            SpotCue cue = cuePrefab != null ? Instantiate(cuePrefab) : SpotCue.CreatePlaceholder();
            cue.transform.SetPositionAndRotation(position, Quaternion.identity);
            // Parented under the anchor, the cue moves whenever ARCore refines where that anchor really is.
            cue.transform.SetParent(anchor != null ? anchor : cuesRoot, true);
            SetLayer(cue.gameObject, LayerMask.NameToLayer("Cues"));
            cue.Init(spot, priority);

            if (popSound != null)
            {
                popSound.pitch = 1f + 0.05f * popIndex;
                popSound.Play();
            }
            return cue;
        }

        static void SetLayer(GameObject go, int layer)
        {
            if (layer < 0) return;
            foreach (var t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;
        }
    }
}
