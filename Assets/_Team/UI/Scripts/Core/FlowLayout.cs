using UnityEngine;
using UnityEngine.UI;

namespace FindMe.UI
{
    /// <summary>
    /// Wrapping row (CSS flex-wrap): children keep their preferred size and go left to right, starting a
    /// new line when the next one doesn't fit. Used for chips. uGUI has no built-in wrapping layout.
    /// </summary>
    public class FlowLayout : LayoutGroup
    {
        public float gap = 10f, lineGap = 10f;
        public bool center;
        float height;

        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();      // collects rectChildren
            SetLayoutInputForAxis(padding.horizontal, padding.horizontal, -1, 0);
        }

        public override void CalculateLayoutInputVertical()
        {
            height = Arrange(false);
            SetLayoutInputForAxis(height, height, -1, 1);
        }

        public override void SetLayoutHorizontal() => Arrange(true);
        public override void SetLayoutVertical() => Arrange(true);

        /// <summary>Walks the children line by line. place=false only measures the total height.</summary>
        float Arrange(bool place)
        {
            float maxW = rectTransform.rect.width - padding.horizontal;
            float y = padding.top, lineH = 0f;
            int lineStart = 0;
            float x = 0f;
            for (int i = 0; i < rectChildren.Count; i++)
            {
                var c = rectChildren[i];
                float w = Mathf.Min(LayoutUtility.GetPreferredWidth(c), maxW);
                float h = LayoutUtility.GetPreferredHeight(c);
                if (x > 0f && x + w > maxW)                 // doesn't fit: finish this line, start the next
                {
                    if (place) Shift(lineStart, i, maxW - (x - gap));
                    y += lineH + lineGap;
                    x = 0f; lineH = 0f; lineStart = i;
                }
                if (place)
                {
                    SetChildAlongAxis(c, 0, padding.left + x, w);
                    SetChildAlongAxis(c, 1, y, h);
                }
                x += w + gap;
                lineH = Mathf.Max(lineH, h);
            }
            if (place) Shift(lineStart, rectChildren.Count, maxW - (x - gap));
            return y + lineH + padding.bottom;
        }

        // Centered flow: move a finished line right by half its leftover space.
        void Shift(int from, int to, float leftover)
        {
            if (!center || leftover <= 0f) return;
            for (int i = from; i < to; i++) rectChildren[i].anchoredPosition += new Vector2(leftover / 2f, 0f);
        }
    }
}
