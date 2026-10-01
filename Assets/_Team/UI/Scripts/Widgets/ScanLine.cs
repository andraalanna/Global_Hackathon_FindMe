using UnityEngine;
using UnityEngine.UI;

namespace FindMe.UI
{
    /// <summary>
    /// The mint "scanner" sweeping down the screen while the room is mapped (prototype .scanline, 2.4 s):
    /// a soft see-through band with a bright 3 px line at its bottom edge.
    /// </summary>
    public class ScanLine : MonoBehaviour
    {
        public float period = 2.4f;
        RectTransform rt, parentRt;

        public static ScanLine Create(Transform screenRoot)
        {
            var T = UIKit.T;
            var band = UIKit.Box(screenRoot, "ScanLine", new Color(T.mint.r, T.mint.g, T.mint.b, 0.16f));
            var rt = (RectTransform)band.transform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 0f);                   // bottom edge = the bright line
            rt.sizeDelta = new Vector2(0f, 120f);
            var line = UIKit.Box(band.transform, "Line", T.mint);
            var lrt = (RectTransform)line.transform;
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = new Vector2(1f, 0f);
            lrt.pivot = new Vector2(0.5f, 0f);
            lrt.sizeDelta = new Vector2(0f, 3f);
            band.transform.SetSiblingIndex(1);                  // just above the screen background
            return band.gameObject.AddComponent<ScanLine>();
        }

        void Awake()
        {
            rt = (RectTransform)transform;
            parentRt = (RectTransform)transform.parent;
        }

        void Update()
        {
            if (Tweens.ReduceMotion) { rt.anchoredPosition = new Vector2(0f, 1000f); return; }
            // From just above the top (fully hidden) down to the bottom edge, then start again.
            float t = Mathf.Repeat(Time.unscaledTime / period, 1f);
            float h = parentRt.rect.height;
            rt.anchoredPosition = new Vector2(0f, Mathf.Lerp(0f, -h, t));
        }
    }
}
