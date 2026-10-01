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
    /// box → screen pixels → ray from the capture-time camera → <see cref="SpotLocator"/> → anchor → cue.
    /// Cues placed on a weak guess keep re-locating themselves as ARCore learns the room (see Update).
    /// </summary>
    public partial class SpotPlacer : MonoBehaviour
    {
        [SerializeField] ARRaycastManager raycastManager;
        [SerializeField] ARAnchorManager anchorManager;
        [SerializeField] ARPlaneManager planeManager;
        [SerializeField] GhostCamera ghost;
        [SerializeField] Camera arCamera;
        [Tooltip("Parent for cues that couldn't get an anchor.")]
        [SerializeField] Transform cuesRoot;
        [Tooltip("Assets/_Team/AR/Prefabs/SpotCue.prefab (built by FindMe/4).")]
        [SerializeField] SpotCue cuePrefab;
        [Tooltip("Optional pop sound; pitch rises 5% per cue.")]
        [SerializeField] AudioSource popSound;

        [SerializeField] float mergeDistance = 0.35f;   // m: closer than this + similar label = same spot
        [Tooltip("Old cue lands inside the new box + similar label = same spot, if also within this distance (m).")]
        [SerializeField] float reprojectMergeDistance = 1.5f;
        [Tooltip("Grow each box by this fraction on every side for the 'old cue inside new box' test.")]
        [SerializeField] float boxMargin = 0.1f;
        [Tooltip("At most this many cues still to check. Checked cues don't count, so a thorough search never gets stuck.")]
        [SerializeField] int maxCues = 8;
        [Tooltip("Hard limit on all cues (checked ones included), to keep the phone fast.")]
        [SerializeField] int maxTotalCues = 16;
        [SerializeField] float spawnInterval = 0.15f;
        [Tooltip("Pull the cue this far back toward the camera so it floats in front of the surface.")]
        [SerializeField] float surfaceOffset = 0.03f;
        [Tooltip("How often (s) one weakly-placed cue re-tries its rays. One cue per tick keeps it cheap.")]
        [SerializeField] float refineInterval = 0.4f;

        public IReadOnlyList<SpotCue> Cues => cues;

        readonly List<SpotCue> cues = new();
        readonly HashSet<SpotCue> moving = new();   // cues whose new anchor is being created
        SpotLocator locator;
        float refineTimer;
        int refineIndex;

        SpotLocator Locator => locator ??= new SpotLocator(raycastManager, planeManager, ghost,
                                                            arCamera != null ? arCamera : Camera.main, surfaceOffset);

        /// <summary>What one scan did: new cues, spots that matched an existing cue, spots dropped by the cap.</summary>
        public struct PlaceResult
        {
            public int added, merged, overCap;
        }

        /// <summary>
        /// Places cues for spots from ONE snapshot (already sorted, highest likelihood first), 150 ms apart.
        /// Spots that match an existing cue are merged into it, so that cue keeps its state (e.g. Checked).
        /// </summary>
        public async Task<PlaceResult> PlaceSpots(IReadOnlyList<HidingSpot> spots, CapturePose pose, SurfaceGrid grid, CancellationToken ct)
        {
            cues.RemoveAll(c => c == null);   // ARCore can drop an anchor (and its cue) at any time
            var result = new PlaceResult();
            foreach (var spot in spots)
            {
                ct.ThrowIfCancellationRequested();
                Placement placement = Locator.Locate(spot.label, spot.box_2d, pose, grid);
                Vector3 position = placement.position;

                // Same spot seen again (e.g. a second scan): keep the old cue AND its state...
                var existing = FindDuplicate(spot, position, pose);
                if (existing != null)
                {
                    existing.Likelihood = Mathf.Max(existing.Likelihood, spot.likelihood);
                    // ...but if this view measured it better (e.g. scanned from closer), move it there.
                    if (placement.quality > existing.Quality) MoveCue(existing, placement, pose, spot.box_2d);
                    result.merged++;
                    continue;
                }
                if (OpenCount() >= maxCues || cues.Count >= maxTotalCues)
                {
                    result.overCap++;
                    continue;   // keep counting, so the UI can say why nothing new appeared
                }

                Transform parent = await CreateAnchor(position, placement.plane);
                if (ct.IsCancellationRequested)
                {
                    RemoveAnchorOf(parent);   // the search was reset while the anchor was being made
                    ct.ThrowIfCancellationRequested();
                }
                var cue = Spawn(spot, position, parent, cues.Count + 1, result.added);
                Remember(cue, placement, pose, spot.box_2d);
                cues.Add(cue);
                result.added++;
                await Awaitable.WaitForSecondsAsync(spawnInterval, ct);   // stagger: one pop at a time
            }
            return result;
        }

        void RemoveAnchorOf(Transform anchorTransform)
        {
            if (anchorTransform == null || !anchorTransform.TryGetComponent<ARAnchor>(out var anchor)) return;
            if (anchorManager != null && anchorManager.enabled) anchorManager.TryRemoveAnchor(anchor);
            Destroy(anchor.gameObject);
        }

        /// <summary>
        /// Which cue to check next (plan Phase 8): the most likely one still open (to check / needs opening),
        /// ties broken by distance from <paramref name="from"/>. Null when every cue is done.
        /// </summary>
        public SpotCue PickNext(Vector3 from)
        {
            SpotCue best = null;
            float bestDist = float.MaxValue;
            foreach (var c in cues)
            {
                if (c == null || !(c.State is SpotState.ToCheck or SpotState.NeedsOpen)) continue;
                float d = Vector3.Distance(from, c.transform.position);
                if (best == null || c.Likelihood > best.Likelihood || (c.Likelihood == best.Likelihood && d < bestDist))
                {
                    best = c;
                    bestDist = d;
                }
            }
            return best;
        }

        int OpenCount()
        {
            int n = 0;
            foreach (var c in cues) if (c != null && c.IsOpen) n++;
            return n;
        }

        /// <summary>
        /// Small mint marker where Gemini saw the item (centre of itemBox in a check photo).
        /// No plane hit: use the same depth as the spot's cue, since the item is at that spot.
        /// </summary>
        public GameObject PlaceMarker(int[] itemBox, CapturePose pose, Vector3 spotPosition, SurfaceGrid grid = null)
        {
            Placement p = Locator.Locate("item", itemBox, pose, grid);
            Vector3 pos = p.position;
            if (p.quality <= PlacementQuality.Support)
            {
                // No real surface measured: the item is at the spot, so use the spot's distance along the ray.
                Ray ray = ghost.ScreenRay(pose, BoxPointToScreen(itemBox, 0.5f, 0.5f, pose));
                pos = ray.GetPoint(Vector3.Distance(pose.position, spotPosition));
            }

            var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = "ItemMarker";
            Destroy(marker.GetComponent<Collider>());
            marker.transform.SetParent(cuesRoot, false);
            marker.transform.position = pos;
            marker.transform.localScale = Vector3.one * 0.04f;
            marker.GetComponent<Renderer>().material.color = new Color32(0x4F, 0xE3, 0xA8, 0xFF);   // mint
            SetLayer(marker, LayerMask.NameToLayer("Cues"));
            return marker;
        }

        /// <summary>
        /// The item itself was seen in the open: put a cue right on it (no merging, no cap: it's the answer).
        /// </summary>
        public async Task<SpotCue> PlaceItemSighting(HidingSpot item, CapturePose pose, SurfaceGrid grid)
        {
            Placement p = Locator.Locate(item.label, item.box_2d, pose, grid);
            Transform parent = await CreateAnchor(p.position, p.plane);
            var cue = Spawn(item, p.position, parent, cues.Count + 1, 0);
            Remember(cue, p, pose, item.box_2d);
            cue.IsSighting = true;
            cues.Add(cue);
            return cue;
        }

        /// <summary>Removes one cue and its anchor (e.g. a sighting the user said was not their item).</summary>
        public void RemoveCue(SpotCue cue)
        {
            if (cue == null) return;
            cues.Remove(cue);
            var anchor = cue.GetComponentInParent<ARAnchor>();
            if (anchor != null && anchorManager != null && anchorManager.enabled) anchorManager.TryRemoveAnchor(anchor);
            Destroy(anchor != null ? anchor.gameObject : cue.gameObject);
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

        // ---------- re-locating cues ----------

        /// <summary>
        /// One weakly-placed cue per tick re-casts its original rays. When the room is scanned at the start,
        /// ARCore often hasn't seen the floor near a far bag yet; as the user walks, planes/points/depth appear
        /// there and the cue snaps from its guess onto the real surface.
        /// </summary>
        void Update()
        {
            if ((refineTimer += Time.deltaTime) < refineInterval || cues.Count == 0) return;
            refineTimer = 0f;
            refineIndex = (refineIndex + 1) % cues.Count;
            var cue = cues[refineIndex];
            if (cue == null || cue.SourceBox == null || moving.Contains(cue)) return;
            if (cue.Quality > PlacementQuality.Support) return;   // measured at photo time: rays can't beat that
            if (cue.State is SpotState.Checking or SpotState.Candidate or SpotState.Found) return;   // don't jump mid-check

            Placement p = Locator.Locate(cue.Label, cue.SourceBox, cue.SourcePose);
            if (p.quality > cue.Quality) MoveCue(cue, p, cue.SourcePose, cue.SourceBox);
        }

        /// <summary>
        /// "Check here": the user points the phone at the real spot. If something is measured in the middle
        /// of the screen, the cue moves there: the user's aim beats our first guess.
        /// </summary>
        public bool TryRelocateToScreenCenter(SpotCue cue)
        {
            if (cue == null || !Locator.TryLocateScreenCenter(out Placement p)) return false;
            Camera cam = arCamera != null ? arCamera : Camera.main;
            var pose = new CapturePose
            {
                position = cam.transform.position,
                rotation = cam.transform.rotation,
                projection = cam.projectionMatrix,
                screenWidth = Screen.width,
                screenHeight = Screen.height,
                time = Time.time,
            };
            // A small box around the screen centre, so later re-locating re-uses what the user aimed at.
            MoveCue(cue, p, pose, new[] { 450, 450, 550, 550 });
            return true;
        }

        /// <summary>
        /// A check photo (taken up close) also boxed the spot: re-measure it from there. Close-up = more accurate,
        /// so the cue moves if the new measurement is at least as trustworthy as the old one.
        /// </summary>
        public void RelocateFromPhoto(SpotCue cue, int[] box, Snapshot photo)
        {
            if (cue == null || box == null || moving.Contains(cue)) return;
            Placement p = Locator.Locate(cue.Label, box, photo.pose, photo.grid);
            if (p.quality > PlacementQuality.Support && p.quality >= cue.Quality)
                MoveCue(cue, p, photo.pose, box);
        }

        /// <summary>What's in the middle of the screen right now, in the world (for picking the cue the user means).</summary>
        public bool TryScreenCenterPoint(out Vector3 point)
        {
            point = default;
            if (!Locator.TryLocateScreenCenter(out Placement p)) return false;
            point = p.position;
            return true;
        }

        /// <summary>Moves a cue (keeping its state) onto a new anchor at a better position.</summary>
        async void MoveCue(SpotCue cue, Placement p, CapturePose pose, int[] box)
        {
            if (cue == null || !moving.Add(cue)) return;
            try
            {
                var oldAnchor = cue.GetComponentInParent<ARAnchor>();
                Transform parent = await CreateAnchor(p.position, p.plane);
                if (cue == null) { RemoveAnchorOf(parent); return; }   // cleared while waiting
                cue.transform.SetParent(null, true);
                cue.transform.SetPositionAndRotation(p.position, Quaternion.identity);
                cue.transform.SetParent(parent != null ? parent : cuesRoot, true);
                if (oldAnchor != null && oldAnchor.transform != parent) RemoveAnchorOf(oldAnchor.transform);
                Debug.Log($"[SpotPlacer] {cue.Label}: {cue.Quality} → {p.quality}");
                Remember(cue, p, pose, box);
            }
            finally
            {
                moving.Remove(cue);
            }
        }

        static void Remember(SpotCue cue, Placement p, CapturePose pose, int[] box)
        {
            cue.Quality = p.quality;
            cue.SourcePose = pose;
            cue.SourceBox = box;
        }

        /// <summary>How many cues are placed at each quality (DebugOverlay).</summary>
        public string QualitySummary()
        {
            int guess = 0, surface = 0, measured = 0;
            foreach (var c in cues)
            {
                if (c == null) continue;
                if (c.Quality <= PlacementQuality.Guess) guess++;
                else if (c.Quality == PlacementQuality.Support) surface++;
                else measured++;
            }
            return $"{measured} measured, {surface} on floor/table height, {guess} guessed";
        }

        // ---------- box → screen ----------

        /// <summary>
        /// Gemini box point → Unity screen pixel. Gemini: [ymin, xmin, ymax, xmax] in 0..1000, origin TOP-left.
        /// Unity screen: pixels, origin BOTTOM-left. (u, v) pick a point inside the box; (0.5, 0.5) = center.
        /// </summary>
        public static Vector2 BoxPointToScreen(int[] b, float u, float v, CapturePose p)
        {
            // Point in the UPRIGHT image Gemini saw (0..1, top-left origin)...
            var upright = new Vector2(Mathf.Lerp(b[1], b[3], u) / 1000f, Mathf.Lerp(b[0], b[2], v) / 1000f);
            // ...back into the screen image (no-op when the phone was held upright)...
            Vector2 s = ImageOrientation.UprightToScreen(upright, p.up);
            // ...then into Unity pixels. 1 - y: flip Y (top-left origin → bottom-left origin).
            return new Vector2(s.x * p.screenWidth, (1f - s.y) * p.screenHeight);
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
            if (cuePrefab == null) Debug.LogError("[SpotPlacer] No cue prefab. Run FindMe/4. Build SpotCue Prefab, then FindMe/3.");
            SpotCue cue = cuePrefab != null ? Instantiate(cuePrefab) : new GameObject("SpotCue").AddComponent<SpotCue>();
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
