using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FindMe.UI
{
    /// <summary>Button colors from the prototype (.btn.pink / .mint / .navy / .out) plus Calm (lavender).</summary>
    public enum ButtonKind { Primary, Success, Dark, Outline, Calm }

    /// <summary>
    /// The chunky button: a colored face sitting on a darker 5 px "edge" (solid depth, no blur shadow).
    /// Pressing pushes the face down onto the edge, like a real key. Face 58 px, radius 24 (prototype .btn).
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class ChunkyButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        const float FaceHeight = 58f, Radius = 24f;

        public Button Button { get; private set; }
        public TextMeshProUGUI Label { get; private set; }

        RectTransform face;
        CanvasGroup group;
        float press;

        public static ChunkyButton Create(Transform parent, string label, ButtonKind kind, Action onClick, string icon = null)
        {
            var T = UIKit.T;
            var rt = UIKit.Rect(parent, "Button " + label);
            var b = rt.gameObject.AddComponent<ChunkyButton>();
            b.group = rt.gameObject.AddComponent<CanvasGroup>();

            // Colors per kind: face, edge (null = flat), text.
            Color faceC, textC = T.grape;
            Color? edgeC = null;
            switch (kind)
            {
                case ButtonKind.Primary: faceC = T.bubblegum; edgeC = T.bubblegumEdge; break;
                case ButtonKind.Success: faceC = T.mint; edgeC = T.mintEdge; break;
                case ButtonKind.Calm:    faceC = T.lavender; edgeC = T.lavenderEdge; break;
                case ButtonKind.Outline: faceC = T.white; break;
                default:                 faceC = T.grape; textC = T.white; break;   // Dark
            }
            float edge = edgeC.HasValue ? 5f : 0f;
            b.press = edgeC.HasValue ? 3f : 2f;
            UIKit.Size(rt, -1, FaceHeight);       // the layout only sees the face, like CSS (a box-shadow takes no space)

            // An invisible full-size image catches taps (the Button needs a raycast target).
            var hit = rt.gameObject.AddComponent<Image>();
            hit.color = Color.clear;

            // Edge: the same shape shifted `edge` px down, peeking out under the face.
            if (edgeC.HasValue) UIKit.Stretch((RectTransform)UIKit.Box(rt, "Edge", edgeC.Value, Radius).transform, 0, edge, 0, -edge);

            // Outline kind: a cloud-colored shape behind a white one inset by 2 px = a 2 px border.
            var faceImg = UIKit.Box(rt, "Face", kind == ButtonKind.Outline ? T.cloud : faceC, Radius);
            b.face = UIKit.Stretch((RectTransform)faceImg.transform);
            Transform content = b.face;
            if (kind == ButtonKind.Outline)
                content = UIKit.Stretch((RectTransform)UIKit.Box(b.face, "Fill", T.white, Radius - 2f).transform, 2, 2, 2, 2);

            var row = UIKit.Row(content, "Content", 8f, null, TextAnchor.MiddleCenter);
            UIKit.Stretch((RectTransform)row.transform, 18, 0, 18, 0);
            // Icons are grape line art; on the dark button use the white copy (Art/Icons/white/icon_*_white).
            if (icon != null) UIKit.Icon(row.transform, kind == ButtonKind.Dark ? icon + "_white" : icon, 22f);
            b.Label = UIKit.Text(row.transform, label, TextStyle.Button, textC, TextAlignmentOptions.Center);
            b.Label.textWrappingMode = TextWrappingModes.NoWrap;

            b.Button = rt.GetComponent<Button>();
            b.Button.transition = Selectable.Transition.None;      // our own press look instead of color tints
            b.Button.onClick.AddListener(() => onClick?.Invoke());
            return b;
        }

        /// <summary>Disabled buttons fade to 35 % and ignore taps (prototype .btn:disabled).</summary>
        public void SetInteractable(bool on)
        {
            Button.interactable = on;
            group.alpha = on ? 1f : 0.35f;
            if (!on) SetPressed(false);
        }

        public void OnPointerDown(PointerEventData e) { if (Button.interactable) SetPressed(true); }
        public void OnPointerUp(PointerEventData e) => SetPressed(false);
        public void OnPointerExit(PointerEventData e) => SetPressed(false);

        // Pressed: the whole face moves down by `press` px, covering part of the edge.
        void SetPressed(bool down)
        {
            float d = down ? press : 0f;
            face.offsetMin = new Vector2(0f, -d);
            face.offsetMax = new Vector2(0f, -d);
        }
    }
}
