using UnityEngine;

namespace FindMe
{
    /// <summary>
    /// A disabled Camera we move back to where the AR camera was at snapshot time (plan Phase 3).
    /// Asking IT for rays means a pixel in the old snapshot becomes a ray through the old viewpoint,
    /// even if the user has since moved the phone.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class GhostCamera : MonoBehaviour
    {
        Camera cam;

        Camera Cam
        {
            get
            {
                if (cam == null) cam = GetComponent<Camera>();
                return cam;
            }
        }

        void Awake() => Cam.enabled = false;   // never renders; it's only used for its math

        /// <summary>Puts the ghost exactly where the AR camera was: same position, rotation and lens.</summary>
        public void MatchPose(CapturePose pose)
        {
            transform.SetPositionAndRotation(pose.position, pose.rotation);
            // The projection matrix carries FOV, aspect and ARCore's crop; copying it is more exact than FOV.
            Cam.projectionMatrix = pose.projection;
        }

        /// <summary>World-space ray through a pixel of the snapshot (Unity screen pixels, origin bottom-left).</summary>
        public Ray ScreenRay(CapturePose pose, Vector2 screenPx)
        {
            MatchPose(pose);
            // Pixels → viewport (0..1) using the SNAPSHOT's screen size. Going through the viewport avoids
            // Camera.pixelRect, which Unity clamps to the current screen (and the Editor's Game view size).
            var viewport = new Vector3(screenPx.x / pose.screenWidth, screenPx.y / pose.screenHeight, 0f);
            return Cam.ViewportPointToRay(viewport);
        }

        /// <summary>Where a world point appeared in the snapshot: x,y in 0..1 (bottom-left origin), z = depth in metres.</summary>
        public Vector3 WorldToViewport(CapturePose pose, Vector3 world)
        {
            MatchPose(pose);
            return Cam.WorldToViewportPoint(world);
        }
    }
}
