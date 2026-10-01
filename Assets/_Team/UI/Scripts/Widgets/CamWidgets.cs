using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FindMe.UI
{
    /// <summary>
    /// Pieces for screens that sit on top of the live camera: the top row (exit · item pill · timer), the
    /// white speech bubble for the AI's status line, and the bottom sheet. They keep the middle of the
    /// screen clear for the AR cues.
    /// </summary>
    public static class CamWidgets
    {
        static UITheme T => UIKit.T;
        static readonly Color Smoke = new Color32(0x2B, 0x22, 0x50, 158);     // rgba(43,34,80,.62): grapeDark chips on camera

        /// <summary>.camtop: dark 48 px X button, then whatever the caller adds (pill, timer).</summary>
        public static HorizontalLayoutGroup Top(Transform page, Action onExit)
        {
            var row = UIKit.Row(page, "CamTop", 10f, UIKit.Pad(16, 16, 16, 0));
            var x = UIKit.Rect(row.transform, "Exit");
            UIKit.Size(x, 48f, 48f);
            var bg = UIKit.Box(x, "Bg", new Color(1f, 1f, 1f, 0.16f), 15f);
            UIKit.Stretch((RectTransform)bg.transform);
            var ic = UIKit.Icon(x, "x_white", 22f);
            UIKit.Place((RectTransform)ic.transform, new Vector2(0.5f, 0.5f), new Vector2(22f, 22f));
            Widgets2.Tap(x.gameObject, bg, onExit);
            return row;
        }

        /// <summary>.cpill: translucent dark pill with an optional round item tile and text. Returns the text.</summary>
        public static TextMeshProUGUI Pill(Transform parent, Thing thing, string text, out Image tile)
        {
            var row = UIKit.Row(parent, "CamPill", 8f, thing != null ? UIKit.Pad(7, 7, 14, 7) : UIKit.Pad(14, 9, 14, 9));
            UIKit.Backdrop(row.transform, Smoke, 99f);
            tile = null;
            if (thing != null)
            {
                tile = UIKit.Circle(row.transform, "Tile", UIKit.Swatch(thing.color), 30f);
                var ic = UIKit.Icon(tile.transform, thing.icon, 17f);
                UIKit.Place((RectTransform)ic.transform, new Vector2(0.5f, 0.5f), new Vector2(17f, 17f));
            }
            var t = UIKit.Text(row.transform, text, TextStyle.Label, T.white);
            t.fontSize = 14f;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Ellipsis;
            return t;
        }

        /// <summary>.timer: "1:36" in a dark pill. Set the text with TimerText so the digits don't jiggle.</summary>
        public static TextMeshProUGUI Timer(Transform parent)
        {
            var row = UIKit.Row(parent, "Timer", 0f, UIKit.Pad(13, 9, 13, 9));
            UIKit.Backdrop(row.transform, Smoke, 99f);
            var t = UIKit.Text(row.transform, "0:00", TextStyle.Label, T.white);
            t.fontSize = 14f;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            return t;
        }

        /// <summary>.bubble: white speech bubble, top-left, up to 290 px wide. Use SetBubble to change its text.</summary>
        public static TextMeshProUGUI Bubble(Transform page)
        {
            var holder = UIKit.Row(page, "BubbleRow", 0f, UIKit.Pad(16, 12, 16, 0), TextAnchor.UpperLeft);
            var bubble = UIKit.Row(holder.transform, "Bubble", 0f, UIKit.Pad(16, 12, 16, 12));
            UIKit.Backdrop(bubble.transform, T.white, 22f);
            var corner = UIKit.Box(bubble.transform, "Tail", T.white);           // square bottom-left corner (22 22 22 6)
            corner.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            var crt = (RectTransform)corner.transform;
            crt.anchorMin = crt.anchorMax = Vector2.zero;
            crt.pivot = Vector2.zero;
            crt.sizeDelta = new Vector2(22f, 22f);
            corner.transform.SetSiblingIndex(1);
            var t = UIKit.Text(bubble.transform, "", TextStyle.Label);
            t.fontSize = 15f;
            return t;
        }

        /// <summary>Sets the bubble text and width (fits the text, max 290 px); empty text hides it.</summary>
        public static void SetBubble(TextMeshProUGUI t, string text)
        {
            var row = t.transform.parent.parent.gameObject;
            row.SetActive(!string.IsNullOrEmpty(text));
            if (string.IsNullOrEmpty(text)) return;
            t.text = text;
            float w = Mathf.Min(290f, t.GetPreferredValues(text).x + 32f + 1f);
            UIKit.Size(t.transform.parent.GetComponent<RectTransform>(), w);
            UIKit.Size(t, w - 32f);
        }

        /// <summary>
        /// .sheet: marshmallow panel with 32 px top corners and a grab handle, at the bottom. Its background
        /// runs past the safe area to the bottom edge of the screen. Add content to the returned column.
        /// </summary>
        public static VerticalLayoutGroup Sheet(Transform page)
        {
            var sheet = UIKit.Column(page, "Sheet", 0f, UIKit.Pad(22, 10, 22, 12));
            sheet.childForceExpandWidth = true;
            var bg = UIKit.Box(sheet.transform, "Backdrop", T.marshmallow, 32f);
            UIKit.Stretch((RectTransform)bg.transform, 0, 0, 0, -80);           // under the home indicator too
            bg.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            bg.raycastTarget = true;                                           // taps on the sheet don't reach the camera
            bg.transform.SetAsFirstSibling();
            var handleRow = UIKit.Row(sheet.transform, "HandleRow", 0f, UIKit.Pad(0, 0, 0, 12), TextAnchor.MiddleCenter);
            UIKit.Size(UIKit.Box(handleRow.transform, "Handle", T.cloud, 99f), 44f, 5f);
            return sheet;
        }

        /// <summary>Sheet h3 (Fredoka 24) + p (muted, 14 px under it).</summary>
        public static (TextMeshProUGUI h, TextMeshProUGUI p) SheetText(Transform sheet, string h, string p)
        {
            UIKit.Space(sheet, 2f);
            var ht = UIKit.Text(sheet, h, TextStyle.H2);
            ht.fontSize = 24f;
            ht.characterSpacing = -0.8f;                  // the CSS fits these titles with ~1 px to spare
            UIKit.Space(sheet, 6f);
            var pt = UIKit.Text(sheet, p, TextStyle.Body, T.muted);
            pt.lineSpacing = 6f;
            UIKit.Space(sheet, 14f);
            return (ht, pt);
        }

        /// <summary>.prog: bold left label + small right label above a bar.</summary>
        public static (TextMeshProUGUI left, TextMeshProUGUI right, RectTransform fill) Progress(Transform parent, Color fillColor)
        {
            var row = UIKit.Row(parent, "Prog", 0f, UIKit.Pad(0, 0, 0, 8), TextAnchor.LowerLeft);
            var l = Widgets.Grow(UIKit.Text(row.transform, "", TextStyle.Body));
            l.fontSize = 16.5f;
            l.font = T.bodyBlack;
            var r = UIKit.Text(row.transform, "", TextStyle.Small);
            var fill = Widgets2.Bar(parent, 0f, fillColor);
            return (l, r, fill);
        }

        /// <summary>Fixed-width digits (CSS tabular-nums) via TMP's mspace tag.</summary>
        public static string TimerText(string time) => $"<mspace=0.58em>{time}</mspace>";

        public static void SetBar(RectTransform fill, float v) => fill.anchorMax = new Vector2(Mathf.Clamp01(v), 1f);
    }
}
