using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FindMe.UI.EditorTools
{
    /// <summary>
    /// Builds the reusable UI pieces as GameObjects, all sized in design px (390x844 frame, 1 design px = 3 canvas px).
    /// Used by the Editor builders (FoundationBuilder, later the screen builders) so every screen is made from the same parts.
    /// Everything non-interactive has raycastTarget off so only buttons catch touches.
    /// </summary>
    public static class UIFactory
    {
        static float Px(float d) => UITheme.Px(d);
        public static readonly float EdgePx = UITheme.Px(5);   // chunky button bottom edge

        // ---------- basics ----------
        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        /// <summary>Fill the parent, with optional insets (canvas px).</summary>
        public static RectTransform Stretch(RectTransform r, float l = 0, float t = 0, float rt = 0, float b = 0)
        {
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
            r.offsetMin = new Vector2(l, b); r.offsetMax = new Vector2(-rt, -t);
            return r;
        }

        public static Image Img(Transform parent, string name, Sprite sprite, Color color, bool sliced = true)
        {
            var img = Rect(name, parent).gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.type = sprite != null && sliced ? Image.Type.Sliced : Image.Type.Simple;
            img.color = color;
            img.raycastTarget = false;
            Stretch(img.rectTransform);
            return img;
        }

        public static TextMeshProUGUI Text(Transform parent, string name, string text, TMP_FontAsset font, float designPx, Color color,
            TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var t = Rect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
            t.font = font; t.text = text; t.fontSize = Px(designPx); t.color = color;
            t.alignment = align; t.raycastTarget = false;
            t.textWrappingMode = TextWrappingModes.Normal;
            Stretch(t.rectTransform);
            return t;
        }

        public static LayoutElement Size(Component c, float width = -1, float height = -1, float flexW = -1)
        {
            var le = c.GetComponent<LayoutElement>() ?? c.gameObject.AddComponent<LayoutElement>();
            if (width >= 0) le.preferredWidth = width;
            if (height >= 0) le.preferredHeight = height;
            if (flexW >= 0) le.flexibleWidth = flexW;
            return le;
        }

        public static HorizontalLayoutGroup HGroup(Transform t, float spacing, RectOffset pad, TextAnchor align = TextAnchor.MiddleCenter, bool expandW = false)
        {
            var g = t.gameObject.AddComponent<HorizontalLayoutGroup>();
            g.spacing = spacing; g.padding = pad; g.childAlignment = align;
            g.childControlWidth = true; g.childControlHeight = true;
            g.childForceExpandWidth = expandW; g.childForceExpandHeight = false;
            return g;
        }

        public static VerticalLayoutGroup VGroup(Transform t, float spacing, RectOffset pad, TextAnchor align = TextAnchor.UpperCenter)
        {
            var g = t.gameObject.AddComponent<VerticalLayoutGroup>();
            g.spacing = spacing; g.padding = pad; g.childAlignment = align;
            g.childControlWidth = true; g.childControlHeight = true;
            g.childForceExpandWidth = true; g.childForceExpandHeight = false;
            return g;
        }

        static RectOffset Pad(float all) { int p = Mathf.RoundToInt(all); return new RectOffset(p, p, p, p); }

        // ---------- chunky button ----------
        /// <summary>
        /// Height is the FACE height in design px (56 default); the 5 px edge is added below it.
        /// Put it in a layout group (it sets its own preferred height) or give it a width yourself.
        /// </summary>
        public static ChunkyButton Button(UITheme th, Transform parent, string name, string label, ButtonStyle style,
            float faceHeight = 56, Sprite icon = null, float fontPx = 18)
        {
            th.GetButtonColors(style, out var faceCol, out var edgeCol, out var labelCol);

            var root = Rect(name, parent);
            Size(root, -1, Px(faceHeight) + EdgePx);

            // Transparent full-size Image: gives the Button something to raycast against (the hit area).
            var hit = root.gameObject.AddComponent<Image>();
            hit.color = new Color(0, 0, 0, 0);
            var btn = root.gameObject.AddComponent<UnityEngine.UI.Button>();
            btn.targetGraphic = hit;
            btn.transition = Selectable.Transition.None;   // ChunkyButton does the press animation itself
            var cb = root.gameObject.AddComponent<ChunkyButton>();

            var edge = Img(root, "Edge", th.rounded18, edgeCol);
            var face = Img(root, "Face", th.rounded18, faceCol);
            // Face sits EdgePx above the bottom so the edge colour shows as a strip underneath.
            // Secondary also insets the sides/top by 2 px so the edge colour reads as a 2 px outline.
            float side = style == ButtonStyle.Secondary ? Px(2) : 0f;
            Stretch(face.rectTransform, side, side, side, EdgePx);

            bool iconOnly = string.IsNullOrEmpty(label);   // square icon buttons (stop, calm): no side padding, no label
            HGroup(face.transform, iconOnly ? 0 : Px(8), iconOnly ? new RectOffset() : new RectOffset((int)Px(16), (int)Px(16), 0, 0));
            if (icon != null)
            {
                var ic = Img(face.transform, "Icon", icon, labelCol, false);
                ic.preserveAspect = true;
                Size(ic.rectTransform, Px(22), Px(22));
                cb.icon = ic;
            }
            var lbl = Text(face.transform, "Label", label, th.heading, fontPx, labelCol);
            lbl.alignment = TextAlignmentOptions.Center;
            if (iconOnly) lbl.gameObject.SetActive(false);

            cb.face = face.rectTransform; cb.faceImage = face; cb.edgeImage = edge; cb.label = lbl;
            return cb;
        }

        // ---------- bordered shapes (chip, card) ----------
        /// <summary>Outer image = border colour, inner image = fill, inset by the border width. Returns the fill (put content inside it).</summary>
        public static Image Bordered(Transform parent, string name, Sprite sprite, Color border, Color fill, out Image outer,
            float borderDesignPx = 2, float radiusDesignPx = 0)
        {
            outer = Img(parent, name, sprite, border);
            SetRadius(outer, sprite, radiusDesignPx);
            var inner = InnerFill(outer.transform, sprite, fill, borderDesignPx);
            SetRadius(inner, sprite, radiusDesignPx - borderDesignPx);   // inner corner = outer corner minus the border width
            return inner;
        }

        /// <summary>
        /// Corner radius in design px for a 9-sliced rounded sprite (0 = the sprite's own radius). Works by scaling the
        /// sprite's fixed corner size: a bigger pixelsPerUnitMultiplier means smaller corners.
        /// </summary>
        public static void SetRadius(Image img, Sprite sprite, float radiusDesignPx, float spriteRadiusDesignPx = 18)
        {
            if (radiusDesignPx <= 0 || sprite == null) return;
            img.pixelsPerUnitMultiplier = spriteRadiusDesignPx / radiusDesignPx;
        }

        static Image InnerFill(Transform outer, Sprite sprite, Color fill, float borderDesignPx)
        {
            var inner = Img(outer, "Fill", sprite, fill);
            Stretch(inner.rectTransform, Px(borderDesignPx), Px(borderDesignPx), Px(borderDesignPx), Px(borderDesignPx));
            return inner;
        }

        /// <summary>
        /// Pill label. Normal chips are 40 px tall (white, cloud border; selected = grape). tag=true makes the small
        /// 26 px filled "Leather / Flat" tag. icon = grape art (iconSelected = its white copy, shown when selected).
        /// clickable adds a Button so the chip can be tapped.
        /// </summary>
        public static UIChip Chip(UITheme th, Transform parent, string name, string text, bool selected = false,
            Sprite icon = null, Sprite iconSelected = null, bool clickable = false, bool tag = false)
        {
            // Rounded rectangle (not a full pill): 12 px corners for chips, 8 px for the small tags.
            var fill = Bordered(parent, name, th.rounded18, th.cloud, tag ? th.cloud : th.white, out var outer, 2, tag ? 8 : 12);
            var chip = outer.gameObject.AddComponent<UIChip>();
            // Root: sized by its text (ContentSizeFitter). Fill is laid out by the root's group.
            Size(outer.rectTransform, -1, Px(tag ? 26 : 40));
            HGroup(outer.transform, 0, Pad(Px(2)));
            outer.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            int sidePad = (int)Px(tag ? 12 : 16);
            HGroup(fill.transform, Px(6), new RectOffset(sidePad, sidePad, 0, 0));

            if (icon != null)
            {
                var ic = Img(fill.transform, "Icon", icon, Color.white, false);
                ic.preserveAspect = true;
                Size(ic.rectTransform, Px(18), Px(18));
                chip.icon = ic; chip.iconNormal = icon; chip.iconSelected = iconSelected != null ? iconSelected : icon;
            }
            var lbl = Text(fill.transform, "Label", text, tag ? th.bodyExtraBold : th.bodyBold, tag ? 13 : 15, th.grape);

            chip.border = outer; chip.fill = fill; chip.label = lbl; chip.theme = th;
            if (!tag) chip.SetSelected(selected);

            if (clickable)
            {
                outer.raycastTarget = true;   // the outer pill is the touch area
                var b = outer.gameObject.AddComponent<UnityEngine.UI.Button>();
                b.targetGraphic = outer; b.transition = Selectable.Transition.None;
                outer.gameObject.AddComponent<PressFeedback>();
            }
            return chip;
        }

        /// <summary>White rounded card with a 2 px cloud border. Returns the content area (a padded vertical group).</summary>
        public static RectTransform Card(UITheme th, Transform parent, string name)
        {
            var fill = Bordered(parent, name, th.rounded28, th.cloud, th.white, out _);
            VGroup(fill.transform, Px(10), Pad(Px(16)));
            return fill.rectTransform;
        }

        // ---------- progress bar / icon tile ----------
        public static UIProgressBar Bar(UITheme th, Transform parent, string name, Color fillColor, float heightDesign = 10)
        {
            var track = Img(parent, name, th.rounded18, th.cloud);
            SetRadius(track, th.rounded18, 4);   // slightly rounded bar, not a full pill
            Size(track.rectTransform, -1, Px(heightDesign));
            var fill = Img(track.transform, "Fill", th.rounded18, fillColor);
            SetRadius(fill, th.rounded18, 4);
            // Left-anchored; UIProgressBar moves anchorMax.x from 0 to 1.
            fill.rectTransform.anchorMin = Vector2.zero; fill.rectTransform.anchorMax = new Vector2(0, 1);
            fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;
            var bar = track.gameObject.AddComponent<UIProgressBar>();
            bar.fill = fill.rectTransform; bar.fillImage = fill;
            return bar;
        }

        /// <summary>Rounded colored square with an icon in the middle (item icon, toast icon, ...).</summary>
        public static Image IconTile(UITheme th, Transform parent, string name, Sprite icon, Color bg, Color iconColor, float sizeDesign = 44)
        {
            var tile = Img(parent, name, th.rounded18, bg);
            Size(tile.rectTransform, Px(sizeDesign), Px(sizeDesign));
            var ic = Img(tile.transform, "Icon", icon, iconColor, false);
            ic.preserveAspect = true;
            float inset = Px(sizeDesign) * 0.22f;
            Stretch(ic.rectTransform, inset, inset, inset, inset);
            return tile;
        }

        // ---------- text input, round buttons, checklist rows, mascot ----------
        /// <summary>White rounded text box with a 2 px cloud border (52 px tall).</summary>
        public static TMP_InputField InputField(UITheme th, Transform parent, string name, string placeholder)
        {
            var fill = Bordered(parent, name, th.rounded18, th.cloud, th.white, out var outer);
            Size(outer.rectTransform, -1, Px(52), 1);
            outer.raycastTarget = true;   // taps anywhere on the box focus the field

            // The viewport clips the text to the box; TMP_InputField needs it as a separate child.
            var viewport = Rect("Text Area", fill.transform);
            Stretch(viewport, Px(16), 0, Px(16), 0);
            viewport.gameObject.AddComponent<RectMask2D>();

            var text = Text(viewport, "Text", "", th.bodyRegular, 16, th.grape, TextAlignmentOptions.MidlineLeft);
            var ph = Text(viewport, "Placeholder", placeholder, th.bodyRegular, 16,
                new Color(th.muted.r, th.muted.g, th.muted.b, 0.6f), TextAlignmentOptions.MidlineLeft);

            var input = outer.gameObject.AddComponent<TMP_InputField>();
            input.targetGraphic = outer;
            input.transition = Selectable.Transition.None;
            input.textViewport = viewport;
            input.textComponent = text;
            input.placeholder = ph;
            input.fontAsset = th.bodyRegular;
            input.pointSize = text.fontSize;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.customCaretColor = true;
            input.caretColor = th.grape;
            input.caretWidth = 3;
            input.selectionColor = new Color(th.lavender.r, th.lavender.g, th.lavender.b, 0.6f);
            return input;
        }

        /// <summary>
        /// Flat square button with an icon: back button (white, cloud border) or mic (lavender).
        /// The icon is grape art tinted white, so it shows its own grape colour.
        /// </summary>
        public static UnityEngine.UI.Button IconButton(UITheme th, Transform parent, string name, Sprite icon, Color bg,
            float sizeDesign = 44, bool bordered = false)
        {
            Image outer, face;
            if (bordered) face = Bordered(parent, name, th.rounded18, th.cloud, bg, out outer);
            else { outer = Img(parent, name, th.rounded18, bg); face = outer; }
            Size(outer.rectTransform, Px(sizeDesign), Px(sizeDesign));
            outer.raycastTarget = true;
            var b = outer.gameObject.AddComponent<UnityEngine.UI.Button>();
            b.targetGraphic = outer; b.transition = Selectable.Transition.None;
            outer.gameObject.AddComponent<PressFeedback>();
            var ic = Img(face.transform, "Icon", icon, Color.white, false);
            ic.preserveAspect = true;
            float inset = Px(sizeDesign) * 0.25f;
            Stretch(ic.rectTransform, inset, inset, inset, inset);
            return b;
        }

        /// <summary>Checklist row: 28 px status circle + label (Analyzing screen).</summary>
        public static UIStepRow StepRow(UITheme th, Transform parent, string name, string text, Sprite checkIcon, Color holeColor)
        {
            var root = Rect(name, parent);
            Size(root, -1, Px(32));
            HGroup(root, Px(12), new RectOffset(), TextAnchor.MiddleLeft);
            var row = root.gameObject.AddComponent<UIStepRow>();

            // 28 px rounded-square badge made of stacked squares; children stretch to the badge's fixed size.
            var badge = Rect("Badge", root);
            Size(badge, Px(28), Px(28));
            row.badge = badge;
            row.pendingColor = th.cloud; row.activeColor = th.bubblegum;
            row.ring = Img(badge, "Ring", th.rounded18, th.cloud);
            SetRadius(row.ring, th.rounded18, 8);
            row.hole = Img(badge, "Hole", th.rounded18, holeColor);
            Stretch(row.hole.rectTransform, Px(4), Px(4), Px(4), Px(4));   // leaves a 4 px outline
            SetRadius(row.hole, th.rounded18, 4);
            row.doneFill = Img(badge, "Done", th.rounded18, th.mint);
            SetRadius(row.doneFill, th.rounded18, 8);
            row.check = Img(badge, "Check", checkIcon, Color.white, false);
            row.check.preserveAspect = true;
            Stretch(row.check.rectTransform, Px(6), Px(6), Px(6), Px(6));

            row.label = Text(root, "Label", text, th.bodyExtraBold, 18, th.grape, TextAlignmentOptions.MidlineLeft);
            Size(row.label.rectTransform, -1, -1, 1);
            row.SetState(StepState.Pending);
            return row;
        }

        /// <summary>Mascot picture at a given width (design px); height follows the sprite's aspect ratio.</summary>
        public static Image Mascot(Transform parent, string name, Sprite sprite, float widthDesign)
        {
            var img = Img(parent, name, sprite, Color.white, false);
            img.preserveAspect = true;
            float h = widthDesign * sprite.rect.height / sprite.rect.width;
            Size(img.rectTransform, Px(widthDesign), Px(h));
            return img;
        }

        /// <summary>
        /// White card with a solid dark strip under it (the "edge" look, used for the HUD top bar over the camera).
        /// Returns the face; put content inside it. heightDesign is the face height; the edge is added below.
        /// </summary>
        public static Image EdgedCard(UITheme th, Transform parent, string name, float heightDesign, float edgeDesign = 4, float radiusDesign = 20)
        {
            var root = Rect(name, parent);
            Size(root, -1, Px(heightDesign + edgeDesign));
            var edge = Img(root, "Edge", th.rounded18, new Color(th.grapeDark.r, th.grapeDark.g, th.grapeDark.b, 0.35f));
            SetRadius(edge, th.rounded18, radiusDesign);
            var face = Img(root, "Face", th.rounded18, th.white);
            SetRadius(face, th.rounded18, radiusDesign);
            Stretch(face.rectTransform, 0, 0, 0, Px(edgeDesign));
            return face;
        }

        /// <summary>Small dark legend chip with a coloured square dot ("Not seen", "Glanced", "Checked") for the HUD.</summary>
        public static RectTransform LegendChip(UITheme th, Transform parent, string text, Color dot)
        {
            var chip = Img(parent, "Legend_" + text, th.rounded18, th.grapeDark);
            SetRadius(chip, th.rounded18, 10);
            Size(chip.rectTransform, -1, Px(28));
            chip.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            HGroup(chip.transform, Px(6), new RectOffset((int)Px(10), (int)Px(10), 0, 0));
            var d = Img(chip.transform, "Dot", th.rounded18, dot);
            SetRadius(d, th.rounded18, 3);
            Size(d.rectTransform, Px(10), Px(10));
            Text(chip.transform, "Label", text, th.bodyBold, 12, th.white);
            return chip.rectTransform;
        }

        /// <summary>Invisible stretchy gap for pushing things apart in a layout group (flex = share of the free space).</summary>
        public static RectTransform Spacer(Transform parent, float flex = 1, float minDesign = 0)
        {
            var r = Rect("Spacer", parent);
            var le = r.gameObject.AddComponent<LayoutElement>();
            le.flexibleHeight = flex; le.minHeight = Px(minDesign);
            return r;
        }

        // ---------- toast / bottom sheet ----------
        /// <summary>Banner that slides from the top. Add it once to UIRoot's SafeArea; call Show() to use it.</summary>
        public static UIToast Toast(UITheme th, Transform parent, Sprite tileIcon)
        {
            var root = Rect("Toast", parent);
            Stretch(root);
            var toast = root.gameObject.AddComponent<UIToast>();

            var box = Img(root, "Box", th.rounded18, th.soda);
            var r = box.rectTransform;
            r.anchorMin = new Vector2(0, 1); r.anchorMax = new Vector2(1, 1); r.pivot = new Vector2(0.5f, 1);
            r.offsetMin = new Vector2(Px(16), 0); r.offsetMax = new Vector2(-Px(16), 0);
            r.sizeDelta = new Vector2(r.sizeDelta.x, Px(72));
            r.anchoredPosition = new Vector2(0, -Px(8));
            HGroup(box.transform, Px(12), Pad(Px(12)), TextAnchor.MiddleLeft);

            IconTile(th, box.transform, "Tile", tileIcon, th.white, th.grape, 40);
            var col = Rect("Texts", box.transform);
            VGroup(col, 0, new RectOffset(), TextAnchor.MiddleLeft);
            Size(col, -1, -1, 1);
            var msg = Text(col, "Message", "", th.heading, 17, th.grape, TextAlignmentOptions.MidlineLeft);
            var sub = Text(col, "Hint", "", th.bodyRegular, 13, th.grape, TextAlignmentOptions.MidlineLeft);

            var act = Button(th, box.transform, "Action", "Undo", ButtonStyle.Dark, 36, null, 15);
            Size(act, Px(84), Px(36) + EdgePx);

            toast.box = r; toast.boxImage = box; toast.message = msg; toast.hint = sub; toast.actionButton = act; toast.theme = th;
            return toast;
        }

        /// <summary>
        /// White bottom sheet (grab handle on top). heightDesign &gt; 0 = fixed height; 0 = grows with its content.
        /// Returns the sheet component and the padded area to put content in.
        /// </summary>
        public static UIBottomSheet Sheet(UITheme th, Transform parent, string name, float heightDesign, out RectTransform content)
        {
            var root = Rect(name, parent);
            Stretch(root);
            var sheetComp = root.gameObject.AddComponent<UIBottomSheet>();

            var sheet = Img(root, "Sheet", th.rounded28, th.white);
            var r = sheet.rectTransform;
            r.anchorMin = new Vector2(0, 0); r.anchorMax = new Vector2(1, 0); r.pivot = new Vector2(0.5f, 0);
            r.offsetMin = r.offsetMax = Vector2.zero;
            sheet.raycastTarget = true;   // taps on the sheet shouldn't fall through to the camera view

            var handle = Img(sheet.transform, "Handle", th.rounded18, th.cloud);
            SetRadius(handle, th.rounded18, 2.5f);   // small rounded bar, not a pill
            var h = handle.rectTransform;
            h.anchorMin = h.anchorMax = new Vector2(0.5f, 1); h.pivot = new Vector2(0.5f, 1);
            h.sizeDelta = new Vector2(Px(44), Px(5)); h.anchoredPosition = new Vector2(0, -Px(10));
            handle.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;   // the handle floats, it is not a layout row

            // 34 pt extra at the bottom: iPhone home-bar zone, since the sheet reaches the screen edge.
            var pad = new RectOffset((int)Px(24), (int)Px(24), (int)Px(28), (int)(Px(24) + Px(34)));
            if (heightDesign > 0)
            {
                r.sizeDelta = new Vector2(0, Px(heightDesign + 34));
                var body = Rect("Content", sheet.transform);
                Stretch(body, Px(24), Px(28), Px(24), Px(24) + Px(34));
                VGroup(body, Px(12), new RectOffset());
                content = body;
            }
            else
            {
                VGroup(sheet.transform, Px(12), pad);
                sheet.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                content = r;
            }
            sheetComp.sheet = r;
            return sheetComp;
        }
    }
}
