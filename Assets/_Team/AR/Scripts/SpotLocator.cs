using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace FindMe
{
    /// <summary>How sure we are about a cue's 3D position, worst → best. Used to decide when to move a cue.</summary>
    public enum PlacementQuality
    {
        Guess,          // nothing hit: fixed-ish distance along the ray (can float in mid-air)
        Support,        // ray met the floor/table height (things rest on surfaces), no surface seen right there
        FeaturePoint,   // hit ARCore's tracked points on a textured surface (bags, clothes)
        Plane,          // hit a detected plane
        Depth,          // hit the depth map: the actual first surface along the ray (best)
    }

    /// <summary>A 3D spot for a cue, plus how much to trust it.</summary>
    public struct Placement
    {
        public Vector3 position;
        public ARPlane plane;              // for plane-attached anchors (may be null)
        public PlacementQuality quality;
    }

    /// <summary>
    /// "Where in the room is this box?" (split out of SpotPlacer). A box gives a DIRECTION from the camera;
    /// the hard part is the DISTANCE. Best first: depth map → detected planes / feature points →
    /// where the ray meets a floor/table height → a guess. Five rays per box, median distance.
    /// </summary>
    public class SpotLocator
    {
        // Everything ARCore can raycast against. Depth needs AROcclusionManager's environment depth turned on.
        const TrackableType HitTypes = TrackableType.PlaneWithinPolygon | TrackableType.PlaneEstimated |
                                       TrackableType.FeaturePoint | TrackableType.Depth;
        const float MinHitDistance = 0.15f, MaxHitDistance = 8f;   // ignore noise at the lens and far away

        // (u, v) inside the box, 0..1, v = 0 at the TOP of the box (Gemini's y goes down).
        static readonly Vector2[] SamplePoints =
            { new(0.5f, 0.5f), new(0.3f, 0.4f), new(0.7f, 0.4f), new(0.3f, 0.7f), new(0.7f, 0.7f) };
        // "under the bed": the hiding place is the bottom of the box.
        static readonly Vector2[] UnderPoints =
            { new(0.5f, 0.8f), new(0.3f, 0.7f), new(0.7f, 0.7f), new(0.3f, 0.9f), new(0.7f, 0.9f) };

        readonly ARRaycastManager raycastManager;
        readonly ARPlaneManager planeManager;
        readonly GhostCamera ghost;
        readonly Camera arCamera;
        readonly float surfaceOffset;

        readonly List<ARRaycastHit> hits = new();
        readonly List<Candidate> candidates = new();
        readonly List<float> distances = new();

        struct Candidate
        {
            public Placement placement;
            public float distance;
            public bool preferred;
        }

        enum Preference { None, Vertical, Horizontal }

        public SpotLocator(ARRaycastManager raycastManager, ARPlaneManager planeManager, GhostCamera ghost,
                           Camera arCamera, float surfaceOffset)
        {
            this.raycastManager = raycastManager;
            this.planeManager = planeManager;
            this.ghost = ghost;
            this.arCamera = arCamera;
            this.surfaceOffset = surfaceOffset;
        }

        /// <summary>
        /// Where a spot's box (from a photo taken at <paramref name="pose"/>) is in the world.
        /// Best source: the photo's own <see cref="SurfaceGrid"/> (measured at photo time). Else rays.
        /// </summary>
        public Placement Locate(string label, int[] box, CapturePose pose, SurfaceGrid grid = null)
        {
            label = (label ?? "").ToLowerInvariant();
            if (grid != null && TryLocateFromGrid(label, box, pose, grid, out var fromGrid)) return fromGrid;
            var points = label.Contains("under") ? UnderPoints : SamplePoints;
            var preference = PreferenceFor(label);
            // Phone still where the photo was taken? Then cast from the live camera through the same pixels:
            // ARCore answers depth-map hits reliably for screen raycasts.
            bool sameView = SameViewpoint(pose);

            candidates.Clear();
            Ray centerRay = default;
            for (int i = 0; i < points.Length; i++)
            {
                Vector2 px = SpotPlacer.BoxPointToScreen(box, points[i].x, points[i].y, pose);
                Ray ray = ghost.ScreenRay(pose, px);   // from where the phone WAS, not where it is now
                if (i == 0) centerRay = ray;

                bool hit = sameView ? raycastManager.Raycast(px, hits, HitTypes) : raycastManager.Raycast(ray, hits, HitTypes);
                if (hit && TryPick(ray, preference, out var c)) candidates.Add(c);
            }

            if (candidates.Count == 0) return Guess(centerRay, pose);

            // If any ray hit the preferred kind of plane (e.g. vertical for a drawer front), trust only those.
            if (candidates.Exists(c => c.preferred)) candidates.RemoveAll(c => !c.preferred);

            // Median by distance: one stray ray that slipped past the object onto the far wall can't drag the cue.
            candidates.Sort((a, b) => a.distance.CompareTo(b.distance));
            return candidates[candidates.Count / 2].placement;
        }

        readonly List<float> gridDist = new();
        readonly List<int> gridIdx = new();

        /// <summary>
        /// Grid cells inside the box (inner 70%, or the bottom 40% for "under …"), measured at photo time.
        /// Their distances are clustered; the densest cluster is the object the box is around (the wall behind
        /// and a chair leg in front are smaller clusters). Cue = centre of that cluster's points.
        /// </summary>
        bool TryLocateFromGrid(string label, int[] box, CapturePose pose, SurfaceGrid grid, out Placement placement)
        {
            placement = default;
            bool under = label.Contains("under");
            float w = box[3] - box[1], h = box[2] - box[0];
            // Inner part of the box in upright 0..1000 coordinates: avoids the background at the box edges.
            float x0 = box[1] + w * 0.15f, x1 = box[3] - w * 0.15f;
            float y0 = under ? box[0] + h * 0.6f : box[0] + h * 0.15f, y1 = under ? box[2] : box[2] - h * 0.15f;

            CollectCells(grid, pose, x0, x1, y0, y1);
            if (gridIdx.Count < 3) CollectCells(grid, pose, box[1], box[3], box[0], box[2]);   // small box: use all of it
            if (gridIdx.Count < 3) return false;                                                // too small/far: rays decide

            // Sort indices by distance, cluster.
            gridIdx.Sort((a, b) => Vector3.Distance(pose.position, grid.points[a]).CompareTo(Vector3.Distance(pose.position, grid.points[b])));
            gridDist.Clear();
            foreach (int i in gridIdx) gridDist.Add(Vector3.Distance(pose.position, grid.points[i]));
            var (start, end) = SurfaceGrid.DensestCluster(gridDist);

            Vector3 sum = Vector3.zero;
            int depthHits = 0, planeHits = 0;
            for (int k = start; k <= end; k++)
            {
                int i = gridIdx[k];
                sum += grid.points[i];
                if (grid.kinds[i] == PlacementQuality.Depth) depthHits++;
                else if (grid.kinds[i] == PlacementQuality.Plane) planeHits++;
            }
            int n = end - start + 1;
            Vector3 centre = sum / n;
            // Pull slightly toward the camera so the cue floats just in front of the surface.
            Vector3 toCam = (pose.position - centre).normalized;
            placement = new Placement
            {
                position = centre + toCam * surfaceOffset,
                quality = depthHits * 2 >= n ? PlacementQuality.Depth
                        : planeHits * 2 >= n ? PlacementQuality.Plane
                        : PlacementQuality.FeaturePoint,
            };
            return true;
        }

        /// <summary>Indices of measured grid cells whose centres fall inside an upright-image rectangle (0..1000).</summary>
        void CollectCells(SurfaceGrid grid, CapturePose pose, float x0, float x1, float y0, float y1)
        {
            gridIdx.Clear();
            for (int r = 0; r < grid.rows; r++)
            for (int c = 0; c < grid.cols; c++)
            {
                int i = r * grid.cols + c;
                if (grid.kinds[i] == PlacementQuality.Guess) continue;
                Vector2 vp = grid.CellViewport(c, r);
                // Viewport (bottom-left) → screen image (top-left) → upright image, where Gemini's box lives.
                Vector2 u = ImageOrientation.ScreenToUpright(new Vector2(vp.x, 1f - vp.y), pose.up) * 1000f;
                if (u.x >= x0 && u.x <= x1 && u.y >= y0 && u.y <= y1) gridIdx.Add(i);
            }
        }

        /// <summary>What's in the middle of the screen right now (the user is pointing the phone at it).</summary>
        public bool TryLocateScreenCenter(out Placement placement)
        {
            var px = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            Ray ray = arCamera.ScreenPointToRay(px);
            placement = default;
            if (!raycastManager.Raycast(px, hits, HitTypes) || !TryPick(ray, Preference.None, out var c)) return false;
            placement = c.placement;
            return true;
        }

        bool SameViewpoint(CapturePose pose) =>
            Vector3.Distance(pose.position, arCamera.transform.position) < 0.05f &&
            Quaternion.Angle(pose.rotation, arCamera.transform.rotation) < 2f &&
            pose.screenWidth == Screen.width && pose.screenHeight == Screen.height;

        /// <summary>
        /// One ray's answer. A depth hit is the actual first surface the ray meets, so it wins. Otherwise the
        /// preferred kind of plane (drawer → vertical), otherwise the closest thing hit (a feature point ON the bag
        /// beats the floor plane BEHIND it).
        /// </summary>
        bool TryPick(Ray ray, Preference preference, out Candidate result)
        {
            result = default;
            int depthIdx = -1, preferredIdx = -1, closestIdx = -1;
            for (int i = 0; i < hits.Count; i++)   // hits are sorted closest first
            {
                var h = hits[i];
                if (h.distance < MinHitDistance || h.distance > MaxHitDistance) continue;
                if (closestIdx < 0) closestIdx = i;
                if (depthIdx < 0 && (h.hitType & TrackableType.Depth) != 0) depthIdx = i;
                if (preferredIdx < 0 && h.trackable is ARPlane p && Matches(p.alignment, preference)) preferredIdx = i;
            }
            int pick = depthIdx >= 0 ? depthIdx : preferredIdx >= 0 ? preferredIdx : closestIdx;
            if (pick < 0) return false;

            var hit = hits[pick];
            var plane = hit.trackable as ARPlane;
            result = new Candidate
            {
                placement = new Placement
                {
                    position = hit.pose.position - ray.direction * surfaceOffset,
                    plane = plane,
                    quality = pick == depthIdx ? PlacementQuality.Depth
                            : plane != null ? PlacementQuality.Plane
                            : PlacementQuality.FeaturePoint,
                },
                distance = hit.distance,
                preferred = pick == preferredIdx && preferredIdx >= 0,
            };
            return true;
        }

        /// <summary>
        /// Nothing hit. Old behaviour put the cue ~1.5 m along the ray, which floats in mid-air when the object
        /// is further away (looking down at a bag on the floor 3 m away). Things rest on surfaces, so first try
        /// where the ray meets a table/floor HEIGHT, then the floor; only then a distance guess.
        /// </summary>
        Placement Guess(Ray ray, CapturePose pose)
        {
            if (TrySupportDistance(ray, out float t))
                return new Placement { position = ray.GetPoint(t), quality = PlacementQuality.Support };
            return new Placement { position = ray.GetPoint(FallbackDistance(pose)), quality = PlacementQuality.Guess };
        }

        bool TrySupportDistance(Ray ray, out float t)
        {
            t = float.MaxValue;
            float floorY = float.MaxValue;
            foreach (var plane in planeManager.trackables)
            {
                if (plane.alignment != PlaneAlignment.HorizontalUp || plane.subsumedBy != null ||
                    plane.trackingState != TrackingState.Tracking) continue;
                float y = plane.center.y;
                floorY = Mathf.Min(floorY, y);   // the lowest up-facing plane is (almost always) the floor
                // A table/bed top counts if the ray crosses its height near where the plane really is.
                if (TryIntersectHorizontal(ray, y, 0.3f, 6f, out float tp) && NearPlane(plane, ray.GetPoint(tp), 0.5f))
                    t = Mathf.Min(t, tp);
            }
            if (t < float.MaxValue) return true;
            // Otherwise the floor, extended under the whole room.
            return floorY < float.MaxValue && TryIntersectHorizontal(ray, floorY, 0.3f, 6f, out t);
        }

        /// <summary>Distance along a ray to the horizontal surface at height y (false if the ray doesn't go down to it).</summary>
        public static bool TryIntersectHorizontal(Ray ray, float y, float minT, float maxT, out float t)
        {
            t = 0f;
            if (ray.direction.y > -0.05f) return false;   // looking level or up never reaches a surface below
            t = (y - ray.origin.y) / ray.direction.y;
            return t >= minT && t <= maxT;
        }

        static bool NearPlane(ARPlane plane, Vector3 world, float margin)
        {
            // Plane space: x/z on the surface; extents = half size; boundary centred on centerInPlaneSpace.
            Vector3 local = plane.transform.InverseTransformPoint(world);
            Vector2 c = plane.centerInPlaneSpace;
            return Mathf.Abs(local.x - c.x) <= plane.extents.x + margin && Mathf.Abs(local.z - c.y) <= plane.extents.y + margin;
        }

        /// <summary>Last resort: median distance of the planes the camera could see (0.6–3.5 m), else 1.5 m.</summary>
        float FallbackDistance(CapturePose pose)
        {
            distances.Clear();
            foreach (var plane in planeManager.trackables)
            {
                Vector3 vp = ghost.WorldToViewport(pose, plane.center);
                if (vp.z > 0f && vp.x is >= 0f and <= 1f && vp.y is >= 0f and <= 1f)
                    distances.Add(Vector3.Distance(pose.position, plane.center));
            }
            if (distances.Count == 0) return 1.5f;
            distances.Sort();
            return Mathf.Clamp(distances[distances.Count / 2], 0.6f, 3.5f);
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
    }
}
