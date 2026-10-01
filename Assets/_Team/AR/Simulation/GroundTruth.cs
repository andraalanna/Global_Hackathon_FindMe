using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.XR.Simulation;

namespace FindMe.Sim
{
    /// <summary>
    /// The living room's real objects, in the AR world's coordinates. The XR Simulation room lives in its own
    /// hidden scene with its own coordinates (the "device" space the simulated phone moves in), so every point
    /// is converted: world = AR camera pose × inverse(simulated phone pose) × room point.
    /// </summary>
    public class GroundTruthRoom
    {
        /// <summary>One real hiding place: its label (as Gemini would say it) and the room objects it is made of.</summary>
        public class Target
        {
            public string label;
            public int likelihood;
            public List<Renderer> parts = new();
        }

        public readonly List<Target> targets = new();
        public Renderer wallet;
        readonly SimulationCameraPoseProvider phone;
        readonly Camera cam;

        // label, likelihood, object paths inside the room prefab
        static readonly (string label, int likelihood, string[] paths)[] Spec =
        {
            ("bag", 3, new[] { "Bag/Body", "Bag/Flap" }),
            ("pile of clothes", 3, new[] { "Sofa/ClothesPile1", "Sofa/ClothesPile2" }),
            ("desk drawer", 2, new[] { "Desk/Drawer" }),
            ("sofa cushions", 2, new[] { "Sofa/CushionL", "Sofa/CushionR" }),
            ("coffee table", 1, new[] { "CoffeeTable/Top" }),
            ("shelf", 1, new[] { "Shelf/Board", "Shelf/Book" }),
        };

        public GroundTruthRoom(SimulationCameraPoseProvider phone, Camera cam)
        {
            this.phone = phone;
            this.cam = cam;
            // The room instance is in the simulation's own scene: find its renderers by path (skip the prefab asset).
            var all = Resources.FindObjectsOfTypeAll<Renderer>().Where(r => r.gameObject.scene.IsValid()).ToList();
            foreach (var (label, likelihood, paths) in Spec)
            {
                var t = new Target { label = label, likelihood = likelihood };
                foreach (var r in all)
                    if (paths.Any(p => Path(r.transform).EndsWith(p))) t.parts.Add(r);
                if (t.parts.Count > 0) targets.Add(t);
            }
            wallet = all.FirstOrDefault(r => Path(r.transform).EndsWith("Bag/Wallet"));
        }

        public bool Found => targets.Count > 0;

        static string Path(Transform t) => t.parent == null ? t.name : Path(t.parent) + "/" + t.name;

        /// <summary>Room point → AR world point (uses the current phone/camera pair; call while the phone is still).</summary>
        public Vector3 ToWorld(Vector3 roomPoint) => cam.transform.TransformPoint(phone.transform.InverseTransformPoint(roomPoint));

        /// <summary>The 8 corners of each part's bounds, in AR world coordinates.</summary>
        public List<Vector3> WorldCorners(IEnumerable<Renderer> parts)
        {
            var list = new List<Vector3>();
            foreach (var r in parts)
            {
                Bounds b = r.bounds;
                for (int i = 0; i < 8; i++)
                {
                    var c = new Vector3((i & 1) == 0 ? b.min.x : b.max.x, (i & 2) == 0 ? b.min.y : b.max.y, (i & 4) == 0 ? b.min.z : b.max.z);
                    list.Add(ToWorld(c));
                }
            }
            return list;
        }

        /// <summary>
        /// How far a world point is from the object (0 = inside it). The object is the union of its parts' boxes,
        /// measured in room space, where the boxes are exact.
        /// </summary>
        public float Distance(IEnumerable<Renderer> parts, Vector3 world)
        {
            Vector3 room = phone.transform.TransformPoint(cam.transform.InverseTransformPoint(world));
            return parts.Min(r => Mathf.Sqrt(r.bounds.SqrDistance(room)));
        }

        /// <summary>
        /// The box a perfect Gemini would draw around these parts in a photo taken at `pose`:
        /// [ymin, xmin, ymax, xmax], 0..1000, top-left origin, in the UPRIGHT image. Null if not in view.
        /// </summary>
        public int[] Box(IEnumerable<Renderer> parts, CapturePose pose, float minVisible = 0.35f)
        {
            // Same camera model as GhostCamera: the snapshot's pose and its projection matrix.
            Matrix4x4 view = Matrix4x4.Scale(new Vector3(1, 1, -1)) * Matrix4x4.TRS(pose.position, pose.rotation, Vector3.one).inverse;
            float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
            foreach (var w in WorldCorners(parts))
            {
                Vector4 clip = pose.projection * view * new Vector4(w.x, w.y, w.z, 1f);
                if (clip.w <= 0.01f) return null;                                  // a corner behind the camera
                var vp = new Vector2(clip.x / clip.w * 0.5f + 0.5f, clip.y / clip.w * 0.5f + 0.5f);
                // Viewport (bottom-left) → screen image (top-left) → upright image, where Gemini's boxes live.
                Vector2 u = ImageOrientation.ScreenToUpright(new Vector2(vp.x, 1f - vp.y), pose.up);
                x0 = Mathf.Min(x0, u.x); x1 = Mathf.Max(x1, u.x);
                y0 = Mathf.Min(y0, u.y); y1 = Mathf.Max(y1, u.y);
            }
            float area = (x1 - x0) * (y1 - y0);
            float cx0 = Mathf.Clamp01(x0), cx1 = Mathf.Clamp01(x1), cy0 = Mathf.Clamp01(y0), cy1 = Mathf.Clamp01(y1);
            float visible = (cx1 - cx0) * (cy1 - cy0);
            if (area <= 0f || visible / area < minVisible || visible < 0.0004f) return null;   // mostly off-screen or a speck
            return new[] { Mathf.RoundToInt(cy0 * 1000), Mathf.RoundToInt(cx0 * 1000), Mathf.RoundToInt(cy1 * 1000), Mathf.RoundToInt(cx1 * 1000) };
        }

        public Target TargetFor(string label)
        {
            label = (label ?? "").ToLowerInvariant();
            return targets.FirstOrDefault(t => label.Contains(t.label) || t.label.Contains(label));
        }
    }

    /// <summary>
    /// A perfect Gemini for the simulation: its boxes are the real objects projected through the snapshot's
    /// own camera pose, so any distance between a cue and its object comes from OUR 2D → 3D placement.
    /// </summary>
    public class GroundTruthGemini : IGeminiService
    {
        readonly GroundTruthRoom room;
        readonly SearchController search;
        readonly Camera cam;
        public readonly List<string> log = new();

        public GroundTruthGemini(GroundTruthRoom room, SearchController search, Camera cam)
        {
            this.room = room;
            this.search = search;
            this.cam = cam;
        }

        public async Task<ItemProfile> GetItemProfile(string userText, CancellationToken ct)
        {
            await Task.Yield();
            var p = new ItemProfile { name = "wallet", sizeCm = new[] { 11f, 9f }, shape = "flat folded rectangle",
                                      likelyPlaces = new List<string> { "bag", "pile of clothes", "desk drawer" } };
            p.FillDefaults(userText);
            return p;
        }

        public Task<ItemProfile> GetItemProfileFromPhoto(byte[] jpg, string userText, CancellationToken ct) => GetItemProfile(userText, ct);

        public async Task<List<HidingSpot>> FindSpots(byte[] jpg, ItemProfile item, CancellationToken ct)
        {
            await Awaitable.WaitForSecondsAsync(0.3f, ct);
            var pose = search.LastScan.pose;                          // the pose recorded with THIS photo
            var spots = new List<HidingSpot>();
            foreach (var t in room.targets)
            {
                var box = room.Box(t.parts, pose);
                if (box != null) spots.Add(new HidingSpot { label = t.label, box_2d = box, likelihood = t.likelihood, hint = "ground truth" });
            }
            log.Add($"FindSpots from {pose.position:F2} ({pose.up}): {string.Join(", ", spots.Select(s => $"{s.label}[{string.Join(",", s.box_2d)}]"))}");
            return spots;
        }

        public async Task<SpotCheckResult> CheckSpot(byte[] jpg, ItemProfile item, string spotLabel, string note, CancellationToken ct)
        {
            // The check photo was just taken with the phone held still on the cue: the live pose is the photo pose.
            var pose = new CapturePose
            {
                position = cam.transform.position, rotation = cam.transform.rotation, projection = cam.projectionMatrix,
                screenWidth = Screen.width, screenHeight = Screen.height, up = ImageOrientation.Detect(cam.transform),
            };
            await Awaitable.WaitForSecondsAsync(0.3f, ct);
            var t = room.TargetFor(spotLabel);
            int[] walletBox = room.wallet != null && (spotLabel ?? "").Contains("bag") ? room.Box(new[] { room.wallet }, pose, 0.6f) : null;
            var r = new SpotCheckResult
            {
                spotFullyVisible = true, insideVisible = true, tip = "",
                spotBox = t != null ? room.Box(t.parts, pose) : null,
                itemVisible = walletBox != null, itemBox = walletBox,
                itemEvidence = walletBox != null ? "dark wallet on the floor in front of the bag" : null,
                seen = spotLabel,
            };
            log.Add($"CheckSpot '{spotLabel}': spotBox={(r.spotBox == null ? "none" : string.Join(",", r.spotBox))} item={(walletBox == null ? "no" : string.Join(",", walletBox))}");
            return r;
        }
    }
}
