using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FindMe.UI
{
    /// <summary>
    /// Duolingo-style button: a colored "face" sits on a darker "edge" strip. Pressing pushes the face
    /// down 4 design px so it looks like a physical key. Click events come from the normal Button on this object.
    /// Hierarchy (built by UIFactory): root[Button, ChunkyButton] > Edge(Image), Face(Image) > Icon, Label.
    /// </summary>
    [RequireComponent(typeof(Button), typeof(CanvasGroup))]
    public class ChunkyButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public RectTransform face;
        public Image faceImage;
        public Image edgeImage;
        public TMP_Text label;
        public Image icon;           // optional

        public const float PressPx = 4f * UITheme.K;

        Vector2 restMin, restMax;    // the face's resting offsets (raised above the edge)
        bool pressed, restCached;

        public Button Button => GetComponent<Button>();
        public string Text { get => label.text; set => label.text = value; }

        void Awake() => CacheRest();

        void CacheRest()
        {
            if (restCached) return;
            restMin = face.offsetMin; restMax = face.offsetMax; restCached = true;
        }

        /// <summary>Re-colours the button at runtime (e.g. the HUD's main button: pink "Scan" -> mint "I found it").</summary>
        public void Apply(UITheme th, ButtonStyle style)
        {
            th.GetButtonColors(style, out var faceCol, out var edgeCol, out var labelCol);
            faceImage.color = faceCol;
            edgeImage.color = edgeCol;
            label.color = labelCol;
            if (icon != null) icon.color = labelCol;
        }

        /// <summary>Greys the button out (35 % opacity, no clicks), like the design's disabled state.</summary>
        public void SetInteractable(bool on)
        {
            Button.interactable = on;
            GetComponent<CanvasGroup>().alpha = on ? 1f : 0.35f;
            if (!on) Release();
        }

        public void OnPointerDown(PointerEventData e) { if (Button.interactable) Press(); }
        public void OnPointerUp(PointerEventData e) => Release();
        public void OnPointerExit(PointerEventData e) => Release();   // finger slid off: spring back

        void Press()
        {
            CacheRest();
            pressed = true;
            // Moving both offsets by the same amount shifts the face down without resizing it.
            Offset(-PressPx);
        }

        void Release()
        {
            if (!pressed) return;
            pressed = false;
            Offset(0f);
        }

        void Offset(float dy)
        {
            face.offsetMin = restMin + new Vector2(0f, dy);
            face.offsetMax = restMax + new Vector2(0f, dy);
        }
    }
}
