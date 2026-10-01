using UnityEngine;

namespace FindMe.Onboarding
{
    /// <summary>
    /// Shrinks this RectTransform to Screen.safeArea: below the iPhone notch / status bar and above the home
    /// indicator. Screen backgrounds stay full-screen; only the content inside goes in here.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeArea : MonoBehaviour
    {
        [Tooltip("Off for screens without a top bar: in the prototype their centered content uses the full height.")]
        public bool applyTop = true;

        Rect applied;
        Vector2Int screen;

        void OnEnable() => Apply();
        void Update()
        {
            // Rotation is locked to portrait, but the Editor's Game view can change size: re-check cheaply.
            if (Screen.safeArea != applied || screen.x != Screen.width || screen.y != Screen.height) Apply();
        }

        void Apply()
        {
            applied = Screen.safeArea;
            screen = new Vector2Int(Screen.width, Screen.height);
            if (Screen.width <= 0 || Screen.height <= 0) return;
            var rt = (RectTransform)transform;
            // Anchors are fractions of the parent (the full-screen canvas), so pixels / screen size.
            rt.anchorMin = new Vector2(applied.xMin / Screen.width, applied.yMin / Screen.height);
            rt.anchorMax = new Vector2(applied.xMax / Screen.width, applyTop ? applied.yMax / Screen.height : 1f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}
