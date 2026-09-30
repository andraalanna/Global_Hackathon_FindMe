using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace FindMe
{
    /// <summary>
    /// Shows/hides the dotted plane visuals (plan: hide planes once Searching starts so cues are the star).
    /// Only the visuals: planes keep tracking and raycasts still hit them.
    /// </summary>
    public class PlaneVisibility : MonoBehaviour
    {
        [SerializeField] ARPlaneManager planeManager;

        public bool Visible { get; private set; } = true;

        void OnEnable() => planeManager.trackablesChanged.AddListener(OnPlanesChanged);
        void OnDisable() => planeManager.trackablesChanged.RemoveListener(OnPlanesChanged);

        public void SetVisible(bool visible)
        {
            if (Visible == visible) return;
            Visible = visible;
            foreach (var plane in planeManager.trackables) Apply(plane);
        }

        // Planes found later must follow the current setting too.
        void OnPlanesChanged(ARTrackablesChangedEventArgs<ARPlane> args)
        {
            foreach (var plane in args.added) Apply(plane);
        }

        void Apply(ARPlane plane)
        {
            // Toggling the visualizer (not the renderer): it re-enables its renderer every update otherwise.
            if (plane.TryGetComponent<ARPlaneMeshVisualizer>(out var viz)) viz.enabled = Visible;
        }
    }
}
