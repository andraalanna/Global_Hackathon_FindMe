using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FindMe
{
    /// <summary>
    /// Screen-edge arrow toward the "next" cue when it's out of view (plan Phase 6), e.g. "Bag" with a pink arrow.
    /// Builds its own small overlay canvas at runtime. SnapshotCapture hides screen-space canvases,
    /// so the arrow never ends up in a photo sent to Gemini.
    /// </summary>
    public class OffscreenIndicator : MonoBehaviour
    {
        [SerializeField] Camera arCamera;
        [SerializeField] Sprite arrowSprite;
        [Tooltip("Keep the arrow this far (fraction of the screen) from the edges.")]
        [SerializeField] float edgeMargin = 0.08f;

        /// <summary>The cue to point at (null = hide). Set by SearchController.</summary>
        public SpotCue Target { get; set; }

        static readonly Color Pink = new Color32(0xFF, 0x7E, 0xB6, 0xFF);
        static readonly Color Grape = new Color32(0x3A, 0x2E, 0x6B, 0xFF);

        RectTransform root, arrow;
        TextMeshProUGUI label;
        Image labelBack;

        void Awake()
        {
            if (arCamera == null) arCamera = Camera.main;
            Build();
        }

        void LateUpdate()
        {
            Vector2 pos01 = default;
            float angle = 0f;
            bool show = Target != null && Target.IsOpen && ComputeEdgePoint(out pos01, out angle);
            root.gameObject.SetActive(show);
            if (!show) return;

            var canvasSize = ((RectTransform)root.parent).rect.size;
            root.anchoredPosition = new Vector2(pos01.x * canvasSize.x, pos01.y * canvasSize.y);
            arrow.localRotation = Quaternion.Euler(0f, 0f, angle);
            // Label sits on the inner side of the arrow so it never goes off-screen.
            Vector2 inward = -new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
            ((RectTransform)labelBack.transform).anchoredPosition = inward * 160f;
            label.text = Target.Label;
        }

        /// <summary>
        /// Where on the screen edge to put the arrow (0..1, bottom-left origin) and which way it points (degrees).
        /// Returns false when the target is comfortably on screen.
        /// </summary>
        bool ComputeEdgePoint(out Vector2 pos01, out float angle)
        {
            Vector3 vp = arCamera.WorldToViewportPoint(Target.transform.position);
            bool inFront = vp.z > 0f;
            pos01 = default;
            angle = 0f;
            if (inFront && vp.x is > 0.02f and < 0.98f && vp.y is > 0.02f and < 0.98f) return false;

            // Direction from the screen centre. Behind the camera, the projection is mirrored: flip it.
            Vector2 dir = new Vector2(vp.x - 0.5f, vp.y - 0.5f);
            if (!inFront) dir = -dir;
            if (dir.sqrMagnitude < 1e-6f) dir = Vector2.down;   // straight behind: point down ("turn around")

            // Push the point out until it touches the (margin-inset) screen rectangle.
            float half = 0.5f - edgeMargin;
            float k = half / Mathf.Max(Mathf.Abs(dir.x), Mathf.Abs(dir.y));
            pos01 = new Vector2(0.5f, 0.5f) + dir * k;
            angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            return true;
        }

        void Build()
        {
            var canvasGo = new GameObject("OffscreenCanvas", typeof(RectTransform));
            canvasGo.transform.SetParent(transform, false);
            canvasGo.layer = LayerMask.NameToLayer("Overlay");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 2340f);   // designed for the A80
            scaler.matchWidthOrHeight = 0f;

            root = new GameObject("Indicator", typeof(RectTransform)).GetComponent<RectTransform>();
            root.SetParent(canvasGo.transform, false);
            root.anchorMin = root.anchorMax = Vector2.zero;   // anchoredPosition = pixels from bottom-left

            arrow = new GameObject("Arrow", typeof(RectTransform)).GetComponent<RectTransform>();
            arrow.SetParent(root, false);
            arrow.sizeDelta = new Vector2(96f, 96f);
            var arrowImg = arrow.gameObject.AddComponent<Image>();
            arrowImg.sprite = arrowSprite;                    // points right at 0°
            arrowImg.color = Pink;
            arrowImg.raycastTarget = false;

            labelBack = new GameObject("Label", typeof(RectTransform)).AddComponent<Image>();
            labelBack.transform.SetParent(root, false);
            labelBack.rectTransform.sizeDelta = new Vector2(320f, 64f);
            labelBack.color = new Color(1f, 1f, 1f, 0.92f);
            labelBack.raycastTarget = false;

            label = new GameObject("Text", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            label.transform.SetParent(labelBack.transform, false);
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
            label.fontSize = 36f;
            label.enableAutoSizing = true;       // long labels shrink instead of "pile of clo…"
            label.fontSizeMin = 24f;
            label.fontSizeMax = 36f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Grape;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.raycastTarget = false;

            root.gameObject.SetActive(false);
        }
    }
}
