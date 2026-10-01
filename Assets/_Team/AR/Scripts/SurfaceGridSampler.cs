using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace FindMe
{
    /// <summary>
    /// Measures a <see cref="SurfaceGrid"/> at photo time: one ARCore screen raycast per cell.
    /// Screen raycasts let ARCore do the screen → depth-map mapping itself (orientation, crop, flips),
    /// which is the error-prone part when you read the depth image by hand.
    /// Cost: cols × rows raycasts in ONE frame, only when a photo is taken (≈ once per scan/check).
    /// </summary>
    public class SurfaceGridSampler : MonoBehaviour
    {
        [SerializeField] ARRaycastManager raycastManager;
        [Tooltip("Columns × rows of screen points. 10 × 20 suits a tall portrait screen; more = finer, slower.")]
        [SerializeField] int cols = 10;
        [SerializeField] int rows = 20;

        const TrackableType HitTypes = TrackableType.Depth | TrackableType.PlaneWithinPolygon |
                                       TrackableType.PlaneEstimated | TrackableType.FeaturePoint;
        const float MinDistance = 0.15f, MaxDistance = 8f;

        readonly List<ARRaycastHit> hits = new();

        /// <summary>Call in the same frame as the photo, so grid and image match.</summary>
        public SurfaceGrid Sample()
        {
            var grid = new SurfaceGrid(cols, rows);
            if (raycastManager == null || raycastManager.subsystem == null) return grid;

            for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
            {
                Vector2 vp = grid.CellViewport(c, r);
                if (!raycastManager.Raycast(new Vector2(vp.x * Screen.width, vp.y * Screen.height), hits, HitTypes)) continue;

                // First usable hit along the ray = the first surface the camera sees through this pixel.
                // A depth hit beats others (it IS that pixel's surface); else the nearest plane/point.
                int pick = -1;
                for (int i = 0; i < hits.Count; i++)
                {
                    if (hits[i].distance < MinDistance || hits[i].distance > MaxDistance) continue;
                    if ((hits[i].hitType & TrackableType.Depth) != 0) { pick = i; break; }
                    if (pick < 0) pick = i;
                }
                if (pick < 0) continue;

                var h = hits[pick];
                int idx = r * cols + c;
                grid.points[idx] = h.pose.position;
                grid.kinds[idx] = (h.hitType & TrackableType.Depth) != 0 ? PlacementQuality.Depth
                                : h.trackable is ARPlane ? PlacementQuality.Plane
                                : PlacementQuality.FeaturePoint;
            }
            return grid;
        }
    }
}
