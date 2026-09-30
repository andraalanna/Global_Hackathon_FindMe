using UnityEngine;
using UnityEngine.UI;

namespace FindMe
{
    /// <summary>
    /// "I'm looking" feedback while a scan runs (plan Phase 9): a soft pink band sweeping top → bottom, 1.2 s loop.
    /// Its own overlay canvas; SnapshotCapture hides screen-space canvases, so it never appears in the photo.
    /// </summary>
    public class ScanSweep : MonoBehaviour
    {
        [SerializeField] SearchController search;
        [SerializeField] float loopSeconds = 1.2f;
        [SerializeField] float bandHeight = 180f;      // px at the 1080×2340 reference size

        RectTransform band;
        Canvas canvas;
        float t;

        void Awake() => Build();

        void OnEnable() => search.StateChanged += OnStateChanged;
        void OnDisable() => search.StateChanged -= OnStateChanged;

        void OnStateChanged(SearchState s)
        {
            canvas.enabled = s == SearchState.Scanning;
            t = 0f;
        }

        void Update()
        {
            if (!canvas.enabled) return;
            t = (t + Time.deltaTime / loopSeconds) % 1f;
            float h = ((RectTransform)canvas.transform).rect.height;
            // From just above the top edge to just below the bottom edge.
            band.anchoredPosition = new Vector2(0f, Mathf.Lerp(h + bandHeight * 0.5f, -bandHeight * 0.5f, t));
        }

        void Build()
        {
            var go = new GameObject("ScanSweepCanvas", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            go.layer = LayerMask.NameToLayer("Overlay");
            canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 40;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 2340f);
            scaler.matchWidthOrHeight = 0f;

            band = new GameObject("Band", typeof(RectTransform)).GetComponent<RectTransform>();
            band.SetParent(go.transform, false);
            band.anchorMin = new Vector2(0f, 0f);
            band.anchorMax = new Vector2(1f, 0f);            // full width, positioned from the bottom
            band.sizeDelta = new Vector2(0f, bandHeight);
            var img = band.gameObject.AddComponent<RawImage>();
            img.texture = GradientTexture();
            img.color = new Color32(0xFF, 0x7E, 0xB6, 0x90);  // pink, semi-transparent
            img.raycastTarget = false;                        // never blocks taps

            canvas.enabled = false;
        }

        /// <summary>1×64 texture: transparent at both ends, brightest in the middle (a soft line).</summary>
        static Texture2D GradientTexture()
        {
            var tex = new Texture2D(1, 64, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < 64; y++)
            {
                float a = Mathf.Sin(Mathf.PI * y / 63f);
                tex.SetPixel(0, y, new Color(1f, 1f, 1f, a * a * a));   // cubed: a thin bright core, soft edges
            }
            tex.Apply();
            return tex;
        }
    }
}
