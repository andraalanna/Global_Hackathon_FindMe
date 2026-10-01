using System.Collections.Generic;
using UnityEngine;

namespace FindMe.UI
{
    /// <summary>
    /// Shows exactly one full screen at a time: the old one fades out, the new one fades in
    /// while sliding up 24 design px (72 canvas px) in 0.25 s. Screens are direct children with a CanvasGroup.
    /// </summary>
    public class ScreenRouter : MonoBehaviour
    {
        public const float SlidePx = 24f * UITheme.K;
        public const float Duration = 0.25f;

        readonly Dictionary<string, CanvasGroup> screens = new Dictionary<string, CanvasGroup>();
        CanvasGroup current;
        public string CurrentName { get; private set; }

        void Awake()
        {
            // Every direct child with a CanvasGroup is a screen; all start hidden.
            foreach (Transform child in transform)
            {
                var g = child.GetComponent<CanvasGroup>();
                if (g == null) continue;
                screens[child.name] = g;
                Hide(g);
            }
        }

        public bool Has(string screenName) => screens.ContainsKey(screenName);

        /// <summary>Show the screen whose GameObject is called screenName. Unknown names log a warning.</summary>
        public void Show(string screenName)
        {
            if (!screens.TryGetValue(screenName, out var next)) { Debug.LogWarning($"ScreenRouter: no screen '{screenName}'"); return; }
            if (next == current) return;

            if (current != null)
            {
                var old = current;
                // Only deactivate the old screen if it wasn't brought back in the meantime.
                Tweens.Fade(this, old, 0f, Duration, () => { if (old != current) Hide(old); });
            }

            current = next; CurrentName = screenName;
            next.gameObject.SetActive(true);
            next.alpha = 0f;
            next.blocksRaycasts = true; next.interactable = true;
            var rt = (RectTransform)next.transform;
            rt.anchoredPosition = new Vector2(0f, -SlidePx);   // start a little low...
            Tweens.Move(this, rt, Vector2.zero, Duration);      // ...and glide up to rest
            Tweens.Fade(this, next, 1f, Duration);
        }

        /// <summary>Hide whatever is showing (e.g. when only the AR view should be visible).</summary>
        public void HideAll()
        {
            if (current != null) Hide(current);
            current = null; CurrentName = null;
        }

        static void Hide(CanvasGroup g)
        {
            g.alpha = 0f; g.blocksRaycasts = false; g.interactable = false;
            g.gameObject.SetActive(false);
        }
    }
}
