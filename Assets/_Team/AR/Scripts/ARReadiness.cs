using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace FindMe
{
    /// <summary>
    /// Decides when AR tracking is good enough to scan (plan Phase 1).
    /// Ready = all three conditions true for <see cref="holdSeconds"/> in a row:
    ///   1. ARSession is tracking
    ///   2. Detected planes add up to at least <see cref="minPlaneArea"/> m²
    ///   3. The user has looked around (moved or rotated enough since start)
    /// </summary>
    public class ARReadiness : MonoBehaviour
    {
        [SerializeField] ARPlaneManager planeManager;
        [Tooltip("The AR camera (XR Origin > Camera Offset > Main Camera).")]
        [SerializeField] Transform cameraTransform;

        [Header("Thresholds")]
        [SerializeField] float minPlaneArea = 0.25f;     // m², summed over all planes
        [SerializeField] float minMoveMeters = 0.5f;     // total walked/swept distance
        [SerializeField] float minRotateDegrees = 60f;   // total turning
        [SerializeField] float holdSeconds = 1f;         // conditions must hold this long

        [Header("Jitter filter")]
        [Tooltip("Ignore position changes smaller than this, so tracking noise doesn't add up.")]
        [SerializeField] float moveStep = 0.01f;         // 1 cm
        [SerializeField] float rotateStep = 1f;          // 1 degree
        [Tooltip("Ignore single-frame jumps bigger than this (tracking relocalised, not the user moving).")]
        [SerializeField] float maxJumpMeters = 0.5f;
        [SerializeField] float maxJumpDegrees = 30f;     // 30° in one frame = 900°/s, not a human

        // Public API (used by SearchController + UI)
        public bool IsReady { get; private set; }
        public float Progress01 { get; private set; }

        // Debug readouts (for the dev meter / DebugOverlay)
        public bool IsTracking { get; private set; }
        public float PlaneArea { get; private set; }
        public float MovedMeters { get; private set; }
        public float RotatedDegrees { get; private set; }

        Vector3 lastPos;
        Quaternion lastRot;
        bool hasLastPose;
        float allTrueTimer;

        /// <summary>Start over, e.g. when a new search begins.</summary>
        public void ResetReadiness()
        {
            MovedMeters = RotatedDegrees = allTrueTimer = 0f;
            hasLastPose = false;
            IsReady = false;
            Progress01 = 0f;
        }

        void Awake()
        {
            if (planeManager == null) planeManager = FindAnyObjectByType<ARPlaneManager>();
            if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
        }

        void Update()
        {
            IsTracking = ARSession.state == ARSessionState.SessionTracking;
            PlaneArea = SumPlaneArea();
            // Poses are garbage while not tracking; re-baseline when tracking comes back.
            if (IsTracking) AccumulateMotion();
            else hasLastPose = false;

            bool planesOk = PlaneArea >= minPlaneArea;
            float lookFrac = Mathf.Max(MovedMeters / minMoveMeters, RotatedDegrees / minRotateDegrees);
            bool lookedAround = lookFrac >= 1f;

            // "Continuous" second: any failing condition resets the timer.
            if (IsTracking && planesOk && lookedAround) allTrueTimer += Time.deltaTime;
            else allTrueTimer = 0f;

            IsReady = allTrueTimer >= holdSeconds;

            // Meter: weighted so each condition visibly moves it; only hits 1.0 when actually ready.
            Progress01 = (IsTracking ? 0.2f : 0f)
                       + 0.35f * Mathf.Clamp01(PlaneArea / minPlaneArea)
                       + 0.35f * Mathf.Clamp01(lookFrac)
                       + 0.10f * Mathf.Clamp01(allTrueTimer / holdSeconds);
        }

        float SumPlaneArea()
        {
            if (planeManager == null) return 0f;
            float area = 0f;
            // foreach over trackables uses a struct enumerator: no garbage per frame.
            foreach (var plane in planeManager.trackables)
            {
                // A subsumed plane was merged into a bigger one; counting both would double count.
                if (plane.subsumedBy != null || plane.trackingState != TrackingState.Tracking) continue;
                area += plane.size.x * plane.size.y;  // bounding rectangle, good enough per the plan
            }
            return area;
        }

        void AccumulateMotion()
        {
            if (cameraTransform == null) return;
            Vector3 pos = cameraTransform.position;
            Quaternion rot = cameraTransform.rotation;

            if (!hasLastPose)
            {
                lastPos = pos; lastRot = rot; hasLastPose = true;
                return;
            }

            // Only "commit" movement once it passes the step size. Standing still, tracking noise
            // wiggles the pose by millimetres; without this it would slowly add up to 0.5 m.
            float moved = Vector3.Distance(pos, lastPos);
            if (moved > maxJumpMeters) lastPos = pos;           // teleport: re-baseline, don't count
            else if (moved >= moveStep) { MovedMeters += moved; lastPos = pos; }

            float turned = Quaternion.Angle(rot, lastRot);
            if (turned > maxJumpDegrees) lastRot = rot;          // pose snapped, don't count
            else if (turned >= rotateStep) { RotatedDegrees += turned; lastRot = rot; }
        }
    }
}
