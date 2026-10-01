using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FindMe.UI
{
    /// <summary>
    /// Reusable pieces from the prototype CSS, one static method each: top bar, header, icon tile, chip,
    /// card, list box, text field, avatar. Each returns its root so the screen can tweak it.
    /// </summary>
    public static class Widgets
    {
        static UITheme T => UIKit.T;

        /// <summary>.bar: 48 px back button · centered title · right slot (48 px spacer by default).</summary>
        public static HorizontalLayoutGroup TopBar(Transform page, string title, Action onBack, Func<Transform, Component> right = null)
        {
            var bar = UIKit.Row(page, "TopBar", 10f, UIKit.Pad(18, 16, 18, 6));
            bool bare = onBack == null && right == null;          // title only: the bar is just as tall as the text
            if (onBack != null) SquareButton(bar.transform, "back", onBack);
            else UIKit.Size(UIKit.Rect(bar.transform, "Spacer"), 48f, bare ? 0f : 48f);
            var t = UIKit.Text(bar.transform, title ?? "", TextStyle.H2, null, TextAlignmentOptions.Center);
            t.fontSize = 19f;
            UIKit.Size(t, -1, -1, 1);
            if (right != null) right(bar.transform);
            else UIKit.Size(UIKit.Rect(bar.transform, "Spacer"), 48f, bare ? 0f : 48f);
            return bar;
        }

        /// <summary>.back: 48 px white rounded square with a 2 px border and an icon (back, plus, x…).</summary>
        public static Button SquareButton(Transform parent, string icon, Action onClick)
        {
            var rt = UIKit.Rect(parent, "Square " + icon);
            UIKit.Size(rt, 48f, 48f);
            var hit = rt.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            UIKit.Frame(rt, T.white, T.cloud, 15f);
            var ic = UIKit.Icon(rt, icon, 22f);
            UIKit.Place((RectTransform)ic.transform, new Vector2(0.5f, 0.5f), new Vector2(22f, 22f));
            ic.GetComponent<LayoutElement>().ignoreLayout = true;
            var b = rt.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            b.onClick.AddListener(() => onClick?.Invoke());
            return b;
        }

        /// <summary>.back with a word instead of an icon (e.g. "Edit"): 48 px tall, padding 0 18.</summary>
        public static Button TextSquare(Transform parent, string label, Action onClick)
        {
            var row = UIKit.Row(parent, "Square " + label, 0f, UIKit.Pad(18, 0, 18, 0), TextAnchor.MiddleCenter);
            UIKit.Size(row, -1, 48f);
            var border = UIKit.Frame(row.transform, T.white, T.cloud, 15f);
            UIKit.Text(row.transform, label, TextStyle.Label).fontSize = 16f;
            border.raycastTarget = true;
            var b = row.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            b.targetGraphic = border;
            b.onClick.AddListener(() => onClick?.Invoke());
            return b;
        }

        /// <summary>.head: eyebrow + big title on the left, optional element (avatar) on the right.</summary>
        public static HorizontalLayoutGroup Header(Transform page, string eyebrow, string title, Func<Transform, Component> right = null)
        {
            var row = UIKit.Row(page, "Header", 12f, UIKit.Pad(22, 20, 22, 10), TextAnchor.UpperLeft);
            var col = UIKit.Column(row.transform, "Titles", 2f);
            UIKit.Size(col, -1, -1, 1);
            if (!string.IsNullOrEmpty(eyebrow))
            {
                var e = UIKit.Text(col.transform, eyebrow, TextStyle.Small);
                e.fontSize = 16f;
            }
            var h = UIKit.Text(col.transform, title, TextStyle.Title);
            h.lineSpacing = -20f;                                      // line-height 1.05
            right?.Invoke(row.transform);
            return row;
        }

        /// <summary>.avatar: pink circle with an initial.</summary>
        public static Image Avatar(Transform parent, string letter, float size = 54f, Color? color = null)
        {
            var c = UIKit.Circle(parent, "Avatar", color ?? T.bubblegum, size);
            var t = UIKit.Text(c.transform, letter, TextStyle.H2, T.grape, TextAlignmentOptions.Center);
            t.fontSize = size * 0.41f;
            UIKit.Stretch((RectTransform)t.transform);
            return c;
        }

        /// <summary>.it: colored rounded square (radius 30 % of its size) with a grape icon at 58 %.</summary>
        public static Image IconTile(Transform parent, string icon, Color color, float size = 56f)
        {
            var tile = UIKit.Box(parent, "Tile " + icon, color, size * 0.3f);
            UIKit.Size(tile, size, size);
            ((RectTransform)tile.transform).sizeDelta = new Vector2(size, size);
            var ic = UIKit.Icon(tile.transform, icon, size * 0.58f);
            UIKit.Place((RectTransform)ic.transform, new Vector2(0.5f, 0.5f), new Vector2(size * 0.58f, size * 0.58f));
            return tile;
        }

        /// <summary>Wrapping chip row (.chips).</summary>
        public static FlowLayout Chips(Transform parent, float gap = 10f, bool center = false)
        {
            var f = UIKit.Rect(parent, "Chips").gameObject.AddComponent<FlowLayout>();
            f.gap = f.lineGap = gap;
            f.center = center;
            return f;
        }

        /// <summary>.chip: pill with a 2 px border; "on" = grape fill + white text. Tapping calls onClick.</summary>
        public static Chip Chip(Transform parent, string label, bool on, Action<Chip> onClick = null) =>
            UI.Chip.Create(parent, label, on, onClick);

        /// <summary>.card: white, 2 px border, radius 26, padding 18. Add children to the returned column.</summary>
        public static VerticalLayoutGroup Card(Transform parent, int pad = 18, float gap = 10f, float radius = 26f)
        {
            int p = pad + 2;                                         // CSS padding is inside the 2 px border
            var c = UIKit.Column(parent, "Card", gap, UIKit.Pad(p, p, p, p));
            c.childForceExpandWidth = true;
            UIKit.Frame(c.transform, T.white, T.cloud, radius);
            return c;
        }

        /// <summary>.list: white rounded box whose rows are divided by 2 px lines. Use ListRow to add rows.</summary>
        public static VerticalLayoutGroup ListBox(Transform parent)
        {
            var c = UIKit.Column(parent, "List", 0f, UIKit.Pad(2, 2, 2, 2));
            c.childForceExpandWidth = true;
            UIKit.Frame(c.transform, T.white, T.cloud, 24f);
            return c;
        }

        /// <summary>One row of a ListBox (padding 15/16, gap 12). A divider is added above every row but the first.</summary>
        public static HorizontalLayoutGroup ListRow(VerticalLayoutGroup list, Action onClick = null)
        {
            if (list.transform.childCount > 2) UIKit.Size(UIKit.Box(list.transform, "Divider", T.cloud), -1, 2f);
            var row = UIKit.Row(list.transform, "Row", 12f, UIKit.Pad(16, 15, 16, 15));
            if (onClick != null)
            {
                var hit = row.gameObject.AddComponent<Image>();
                hit.color = Color.clear;
                var b = row.gameObject.AddComponent<Button>();
                b.transition = Selectable.Transition.None;
                b.onClick.AddListener(() => onClick());
            }
            return row;
        }

        /// <summary>Grows to fill a row (pushes the next items to the right edge).</summary>
        public static TextMeshProUGUI Grow(TextMeshProUGUI t)
        {
            UIKit.Size(t, -1, -1, 1);
            return t;
        }

        /// <summary>Label + 60 px text field (.fl + .input).</summary>
        public static TMP_InputField Field(Transform parent, string label, string placeholder, string value = "",
                                           bool password = false)
        {
            var col = UIKit.Column(parent, "Field", 8f, UIKit.Pad(0, 0, 0, 14));
            col.childForceExpandWidth = true;
            if (label != null) UIKit.Text(col.transform, label, TextStyle.Label).fontSize = 15f;
            return Input(col.transform, placeholder, value, password);
        }

        /// <summary>.input: white, 2 px border (lavender while typing), radius 20.</summary>
        public static TMP_InputField Input(Transform parent, string placeholder, string value = "", bool password = false,
                                           float height = 60f)
        {
            var rt = UIKit.Rect(parent, "Input");
            UIKit.Size(rt, -1, height);
            var border = UIKit.Box(rt, "Border", T.cloud, 20f);
            UIKit.Stretch((RectTransform)border.transform);
            border.raycastTarget = true;                             // the tap target that focuses the field
            UIKit.Stretch((RectTransform)UIKit.Box(rt, "Fill", T.white, 18f).transform, 2, 2, 2, 2);

            var area = UIKit.Stretch(UIKit.Rect(rt, "Text Area"), 20, 0, 20, 0);
            area.gameObject.AddComponent<RectMask2D>();
            var ph = UIKit.Text(area, placeholder, TextStyle.Body, new Color32(0xB3, 0xAD, 0xC2, 255));
            var tx = UIKit.Text(area, value, TextStyle.Body);
            foreach (var t in new[] { ph, tx })
            {
                t.fontSize = 17f;
                t.textWrappingMode = TextWrappingModes.NoWrap;
                t.alignment = TextAlignmentOptions.MidlineLeft;
                UIKit.Stretch((RectTransform)t.transform);
            }

            var input = rt.gameObject.AddComponent<TMP_InputField>();
            input.textViewport = area;
            input.textComponent = tx;
            input.placeholder = ph;
            input.targetGraphic = border;
            input.transition = Selectable.Transition.None;
            input.text = value;
            input.contentType = password ? TMP_InputField.ContentType.Password : TMP_InputField.ContentType.Standard;
            input.onSelect.AddListener(_ => border.color = T.lavender);   // .input:focus
            input.onDeselect.AddListener(_ => border.color = T.cloud);
            return input;
        }

        /// <summary>.label section heading ("What do you lose most often?").</summary>
        public static TextMeshProUGUI SectionLabel(Transform parent, string text, float top = 20f)
        {
            UIKit.Space(parent, top);
            var t = UIKit.Text(parent, text, TextStyle.Label);
            UIKit.Space(parent, 10f);
            return t;
        }

        /// <summary>
        /// .hscroll: a horizontally scrolling row that bleeds to the screen edges (margin 0 -22, padding 0 22).
        /// Add cards to the returned row.
        /// </summary>
        public static HorizontalLayoutGroup HScroll(Transform parent, float height, float gap = 12f)
        {
            var view = UIKit.Rect(parent, "HScroll");
            UIKit.Size(view, -1, height);
            view.gameObject.AddComponent<RectMask2D>();
            var hit = view.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            var row = UIKit.Row(view, "Content", gap, UIKit.Pad(22, 2, 22, 6));
            row.childForceExpandHeight = true;
            var crt = (RectTransform)row.transform;
            crt.anchorMin = Vector2.zero;
            crt.anchorMax = new Vector2(0f, 1f);               // full height, grows to the right
            crt.pivot = new Vector2(0f, 0.5f);
            crt.offsetMin = crt.offsetMax = Vector2.zero;
            row.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            var sr = view.gameObject.AddComponent<ScrollRect>();
            sr.vertical = false;
            sr.viewport = view;
            sr.content = crt;
            return row;
        }

        /// <summary>.social:58 px white button with a round badge (G / +) and a label.</summary>
        public static Button Social(Transform parent, string badge, Color badgeBg, Color badgeFg, string label, Action onClick)
        {
            var row = UIKit.Row(parent, "Social", 12f, UIKit.Pad(20, 0, 20, 0));
            UIKit.Size(row, -1, 58f);
            var border = UIKit.Frame(row.transform, T.white, T.cloud, 22f);
            var b0 = UIKit.Circle(row.transform, "Badge", badgeBg, 26f);
            if (badge == "G") UIKit.Frame(b0.transform, T.white, T.cloud, 13f);
            var bt = UIKit.Text(b0.transform, badge, TextStyle.H2, badgeFg, TextAlignmentOptions.Center);
            bt.fontSize = 17f;
            UIKit.Stretch((RectTransform)bt.transform);
            var t = UIKit.Text(row.transform, label, TextStyle.Label);
            t.fontSize = 16.5f;
            border.raycastTarget = true;
            var b = row.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            b.targetGraphic = border;
            b.onClick.AddListener(() => onClick?.Invoke());
            UIKit.Space(parent, 10f);
            return b;
        }

        /// <summary>.or: "── or with email ──".</summary>
        public static void OrDivider(Transform parent, string text)
        {
            UIKit.Space(parent, 6f);           // CSS margins collapse: the 10 px under the social button + 6 = 16
            var row = UIKit.Row(parent, "Or", 12f);
            UIKit.Size(UIKit.Box(row.transform, "Line", T.cloud), -1, 2f, 1);
            var t = UIKit.Text(row.transform, text, TextStyle.Label, T.faint);
            t.fontSize = 13f;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            UIKit.Size(UIKit.Box(row.transform, "Line", T.cloud), -1, 2f, 1);
            UIKit.Space(parent, 16f);
        }

        /// <summary>.privacy: mint-tinted note with an icon (shield / spark) and text (rich text allowed).</summary>
        public static HorizontalLayoutGroup Note(Transform parent, string icon, string richText, Color? bg = null)
        {
            var row = UIKit.Row(parent, "Note", 12f, UIKit.Pad(16, 16, 16, 16), TextAnchor.UpperLeft);
            UIKit.Backdrop(row.transform, bg ?? T.mintLight, 22f);
            UIKit.Icon(row.transform, icon, 24f);
            var t = UIKit.Text(row.transform, richText, TextStyle.Body);
            t.font = T.bodyBold;
            t.fontSize = 14.5f;
            t.lineSpacing = 6f;
            UIKit.Size(t, -1, -1, 1);
            return row;
        }

        /// <summary>.title + .lead block at the top of a form body.</summary>
        public static void TitleLead(Transform body, string title, string lead, float titleTop = 8f)
        {
            UIKit.Space(body, titleTop);
            UIKit.Text(body, title, TextStyle.Title);
            UIKit.Space(body, 10f);
            if (lead == null) return;
            UIKit.Text(body, lead, TextStyle.Lead);
            UIKit.Space(body, 20f);
        }

        /// <summary>.tag: tiny pill ("One-time", "Registered", "Found").</summary>
        public static HorizontalLayoutGroup Tag(Transform parent, string text, Color bg, Color fg)
        {
            var row = UIKit.Row(parent, "Tag", 0f, UIKit.Pad(9, 4, 9, 4), TextAnchor.MiddleCenter);
            UIKit.Backdrop(row.transform, bg, 99f);
            var t = UIKit.Text(row.transform, text, TextStyle.Label, fg);
            t.fontSize = 12f;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            return row;
        }
    }
}
