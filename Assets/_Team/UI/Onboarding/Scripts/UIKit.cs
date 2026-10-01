using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FindMe.Onboarding
{
    /// <summary>Text looks from the prototype CSS (sizes are design px; the canvas is 390 px wide like the prototype).</summary>
    public enum TextStyle
    {
        Brand,   // .brand   Fredoka 52
        Title,   // .title   Fredoka 34
        H2,      // .sec / sheet h3  Fredoka 22
        Lead,    // .lead    Nunito 16.5, muted
        Body,    // Nunito Bold 16
        Label,   // .label   Nunito ExtraBold 15.5
        Small,   // .small   Nunito Bold 14, muted
        Button,  // .btn     Nunito ExtraBold 17.5
    }

    /// <summary>
    /// Small factory for building screens in code: boxes, text, rows/columns, icons.
    /// Every helper returns the new component so calls can be chained or tweaked afterwards.
    /// Call UIKit.Init(theme) once before building anything.
    /// </summary>
    public static class UIKit
    {
        public static OnboardingTheme T { get; private set; }
        const float RoundedBorder = 56f;     // px of the 9-slice border in Art/Sprites/rounded_28.png

        public static void Init(OnboardingTheme theme) => T = theme;

        // ---------- plain rects ----------

        public static RectTransform Rect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;                                         // "UI" layer
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        /// <summary>Fill the parent, with optional insets (left, top, right, bottom).</summary>
        public static RectTransform Stretch(RectTransform rt, float l = 0, float t = 0, float r = 0, float b = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(l, b);
            rt.offsetMax = new Vector2(-r, -t);
            return rt;
        }

        /// <summary>Fixed size, pinned at an anchor point of the parent (0.5,0.5 = centre).</summary>
        public static RectTransform Place(RectTransform rt, Vector2 anchor, Vector2 size, Vector2 pos = default)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            return rt;
        }

        /// <summary>
        /// Removes all children (for lists rebuilt on every show). Hidden at once so the layout ignores them
        /// this frame; DestroyImmediate outside Play mode, where Destroy isn't allowed (Editor capture tool).
        /// </summary>
        public static void Clear(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--)
            {
                var c = t.GetChild(i).gameObject;
                if (!Application.isPlaying) { UnityEngine.Object.DestroyImmediate(c); continue; }
                c.SetActive(false);
                UnityEngine.Object.Destroy(c);
            }
        }

        // ---------- images ----------

        public static Image Box(Transform parent, string name, Color color, float radius = 0f)
        {
            var img = Rect(parent, name).gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            if (radius > 0f) Round(img, radius);
            return img;
        }

        /// <summary>
        /// Any corner radius from one 9-sliced sprite: the corner is drawn `RoundedBorder / multiplier` px big.
        /// A huge radius (999) gives a pill; Unity shrinks corners that don't fit.
        /// </summary>
        public static Image Round(Image img, float radius)
        {
            img.sprite = T.rounded;
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = RoundedBorder / radius;
            // 99+ means "fully round": let PillShape keep the radius at half the short side as the size changes.
            if (radius >= 99f && !img.TryGetComponent(out PillShape _)) img.gameObject.AddComponent<PillShape>();
            return img;
        }

        /// <summary>
        /// Background shape behind a Row/Column's content (cards, pills). ignoreLayout keeps it out of the
        /// layout, so it just stretches under the children. bottomInset leaves room for a solid edge below.
        /// </summary>
        public static Image Backdrop(Transform parent, Color color, float radius, float bottomInset = 0f, float inset = 0f)
        {
            var img = Box(parent, "Backdrop", color, radius);
            Stretch((RectTransform)img.transform, inset, inset, inset, inset + bottomInset);
            img.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            // Behind the content, but in front of backdrops added earlier (so a border goes under its fill).
            int i = 0;
            while (i < parent.childCount && parent.GetChild(i) != img.transform && parent.GetChild(i).name == "Backdrop") i++;
            img.transform.SetSiblingIndex(i);
            return img;
        }

        /// <summary>
        /// Bordered shape behind a Row/Column: border color underneath, fill inset by `border` px on top
        /// (CSS "background + 2px solid border"). Returns the fill.
        /// </summary>
        public static Image Frame(Transform parent, Color fill, Color border, float radius, float borderPx = 2f)
        {
            Backdrop(parent, border, radius);
            return Backdrop(parent, fill, Mathf.Max(1f, radius - borderPx), 0f, borderPx);
        }

        /// <summary>Named swatches used by item tiles in the prototype (c-pink, c-sky, c-mint, c-sun, c-lav).</summary>
        public static Color Swatch(string key) => key switch
        {
            "pink" => T.bubblegum,
            "sky" => T.soda,
            "mint" => T.mint,
            "sun" => T.lemon,
            _ => T.lavender,
        };

        public static Image Circle(Transform parent, string name, Color color, float size)
        {
            var img = Box(parent, name, color);
            img.sprite = T.circle;
            Size(img, size, size);
            ((RectTransform)img.transform).sizeDelta = new Vector2(size, size);
            return img;
        }

        public static Image Sprite(Transform parent, string name, Sprite sprite, Color color, float w, float h)
        {
            var img = Box(parent, name, color);
            img.sprite = sprite;
            img.preserveAspect = true;
            ((RectTransform)img.transform).sizeDelta = new Vector2(w, h);
            Size(img, w, h);
            return img;
        }

        /// <summary>Line icon from Art/Icons, tinted (the grape icons are recolored by multiplying, so pass white to keep grape).</summary>
        public static Image Icon(Transform parent, string icon, float size, Color? tint = null) =>
            Sprite(parent, "Icon " + icon, T.Icon(icon), tint ?? Color.white, size, size);

        // ---------- text ----------

        public static TextMeshProUGUI Text(Transform parent, string text, TextStyle style, Color? color = null,
                                           TextAlignmentOptions align = TextAlignmentOptions.Left)
        {
            var tmp = Rect(parent, "Text").gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.alignment = align;
            tmp.raycastTarget = false;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.color = color ?? T.grape;
            switch (style)
            {
                case TextStyle.Brand:  Set(tmp, T.heading, 52f, -14f); break;
                case TextStyle.Title:  Set(tmp, T.heading, 34f, -16f); break;
                case TextStyle.H2:     Set(tmp, T.heading, 22f, -8f); break;
                case TextStyle.Lead:   Set(tmp, T.body, 16.5f, 6f); tmp.color = color ?? T.muted; tmp.characterSpacing = 1f; break;
                case TextStyle.Body:   Set(tmp, T.bodyBold, 16f, 0f); break;
                case TextStyle.Label:  Set(tmp, T.bodyBlack, 15.5f, 0f); break;
                case TextStyle.Small:  Set(tmp, T.bodyBold, 14f, 0f); tmp.color = color ?? T.muted; break;
                case TextStyle.Button: Set(tmp, T.bodyBlack, 17.5f, 0f); break;
            }
            return tmp;
        }

        // lineSpacing is in TMP "em/100" units: negative tightens lines (titles), positive opens them (body copy).
        static void Set(TextMeshProUGUI t, TMP_FontAsset font, float size, float lineSpacing)
        {
            t.font = font;
            t.fontSize = size;
            t.lineSpacing = lineSpacing;
        }

        // ---------- layout ----------

        /// <summary>Children stacked top to bottom; parent sizes them (like CSS flex column).</summary>
        public static VerticalLayoutGroup Column(Transform parent, string name, float gap = 0f, RectOffset pad = null,
                                                 TextAnchor align = TextAnchor.UpperLeft)
        {
            var v = Rect(parent, name).gameObject.AddComponent<VerticalLayoutGroup>();
            Configure(v, gap, pad, align);
            return v;
        }

        public static HorizontalLayoutGroup Row(Transform parent, string name, float gap = 0f, RectOffset pad = null,
                                                TextAnchor align = TextAnchor.MiddleLeft)
        {
            var h = Rect(parent, name).gameObject.AddComponent<HorizontalLayoutGroup>();
            Configure(h, gap, pad, align);
            return h;
        }

        static void Configure(HorizontalOrVerticalLayoutGroup g, float gap, RectOffset pad, TextAnchor align)
        {
            g.spacing = gap;
            g.padding = pad ?? new RectOffset();
            g.childAlignment = align;
            g.childControlWidth = g.childControlHeight = true;     // the group sets child sizes from LayoutElements
            g.childForceExpandWidth = g.childForceExpandHeight = false;
        }

        /// <summary>Preferred / flexible size for a child of a Row or Column (-1 = leave as is).</summary>
        public static LayoutElement Size(Component c, float w = -1, float h = -1, float flexW = -1, float flexH = -1)
        {
            // TryGetComponent, not "?? AddComponent": Unity's fake-null objects break the ?? operator.
            if (!c.TryGetComponent(out LayoutElement le)) le = c.gameObject.AddComponent<LayoutElement>();
            if (w >= 0) { le.preferredWidth = w; le.minWidth = w; }
            if (h >= 0) { le.preferredHeight = h; le.minHeight = h; }
            if (flexW >= 0) le.flexibleWidth = flexW;
            if (flexH >= 0) le.flexibleHeight = flexH;
            return le;
        }

        /// <summary>Empty spacer: fixed height, or flexible (pushes the next items down).</summary>
        public static LayoutElement Space(Transform parent, float h = 0f, float flex = 0f) =>
            Size(Rect(parent, "Space"), -1, h, -1, flex);

        public static RectOffset Pad(int l, int t, int r, int b) => new RectOffset(l, r, t, b);

        // ---------- buttons ----------

        public static ChunkyButton Button(Transform parent, string label, ButtonKind kind, Action onClick, string icon = null) =>
            ChunkyButton.Create(parent, label, kind, onClick, icon);

        /// <summary>Plain text button (.link: "Log in", "Skip", "Later").</summary>
        public static Button Link(Transform parent, string label, Action onClick, Color? color = null)
        {
            var t = Text(parent, label, TextStyle.Label, color ?? T.muted, TextAlignmentOptions.Center);
            t.raycastTarget = true;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            var b = t.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            b.onClick.AddListener(() => onClick?.Invoke());
            Size(t, -1, 37);                                 // .link: 8 px padding + 21 px line + 8 px
            return b;
        }
    }
}
