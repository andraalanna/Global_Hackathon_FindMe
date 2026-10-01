using UnityEngine;
using UnityEngine.EventSystems;

namespace FindMe.UI
{
    /// <summary>While pressed, this element moves down `depth` px (the chunky "key press" look for round buttons).</summary>
    public class PressSink : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public float depth = 4f;
        RectTransform rt;
        Vector2 rest;
        bool down;

        void Awake()
        {
            rt = (RectTransform)transform;
            rest = rt.anchoredPosition;
        }

        public void OnPointerDown(PointerEventData e) => Set(true);
        public void OnPointerUp(PointerEventData e) => Set(false);
        public void OnPointerExit(PointerEventData e) => Set(false);

        void Set(bool d)
        {
            if (d == down) return;
            down = d;
            rt.anchoredPosition = rest + (d ? new Vector2(0f, -depth) : Vector2.zero);
        }
    }
}
