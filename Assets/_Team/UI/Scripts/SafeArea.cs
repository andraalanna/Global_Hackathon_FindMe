using UnityEngine;

namespace FindMe.UI
{
    /// <summary>
    /// Keeps this panel inside the phone's safe area (notch, camera hole, home bar).
    /// Put all screen content under a SafeArea panel; never hard-code insets.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [ExecuteAlways]
    public class SafeArea : MonoBehaviour
    {
        [Tooltip("Editor only. The Game view reports no insets, so tick this to preview iPhone 13 insets (47 pt top, 34 pt bottom of 844 pt).")]
        public bool simulateIPhone13InEditor = true;

        Rect last;
        Vector2Int lastScreen;

        void OnEnable() => Apply();
        void Update()
        {
            if (Screen.safeArea != last || lastScreen.x != Screen.width || lastScreen.y != Screen.height) Apply();
        }

        void Apply()
        {
            var rt = (RectTransform)transform;
            Rect sa = Screen.safeArea;
            last = sa; lastScreen = new Vector2Int(Screen.width, Screen.height);

            // Screen.safeArea is in pixels; anchors are 0..1, so divide by the screen size.
            Vector2 min = new Vector2(sa.xMin / Screen.width, sa.yMin / Screen.height);
            Vector2 max = new Vector2(sa.xMax / Screen.width, sa.yMax / Screen.height);

#if UNITY_EDITOR
            // Editor only: pretend we're on an iPhone 13 when Unity reports "no insets".
            if (simulateIPhone13InEditor && sa.width >= Screen.width && sa.height >= Screen.height)
            {
                min = new Vector2(0f, 34f / 844f);
                max = new Vector2(1f, 1f - 47f / 844f);
            }
#endif
            rt.anchorMin = min; rt.anchorMax = max;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}
