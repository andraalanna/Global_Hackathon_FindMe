using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace FindMe.UI.EditorTools
{
    /// <summary>
    /// Builds the real screens (design frames 390x844) out of the UIFactory parts. Each Build* method returns the
    /// screen's component with all its references filled in. Layout numbers are design px, converted by UITheme.Px.
    /// </summary>
    public static class ScreensBuilder
    {
        const string Root = "Assets/_Team/UI";

        // HUD bottom row sizes (design px, 1 = 3 canvas px). Change these, then run FindMe > UI > Build Foundation.
        const float HudButtonHeight = 56;      // height of the main button AND the two side buttons (face only)
        const float HudSideButtonWidth = 56;   // width of the stop / calm buttons
        static float Px(float d) => UITheme.Px(d);

        static Sprite Grape(string icon) => AssetDatabase.LoadAssetAtPath<Sprite>($"{Root}/Art/Icons/{icon}.png");
        static Sprite White(string icon) => AssetDatabase.LoadAssetAtPath<Sprite>($"{Root}/Art/Icons/white/{icon}_white.png");
        static Sprite Mascot(string n) => AssetDatabase.LoadAssetAtPath<Sprite>($"{Root}/Art/Mascot/{n}.png");

        /// <summary>
        /// A full screen: CanvasGroup (for fading), an optional solid background that covers the notch too,
        /// and a SafeArea child that holds the content. Returns the content rect.
        /// </summary>
        static RectTransform NewScreen(UITheme th, Transform screens, string name, out GameObject screenRoot, bool background = true)
        {
            var root = UIFactory.Rect(name, screens);
            UIFactory.Stretch(root);
            root.gameObject.AddComponent<CanvasGroup>();
            if (background) UIFactory.Img(root, "Background", null, th.marshmallow, false);   // camera screens skip it

            var safe = UIFactory.Rect("Safe", root);
            UIFactory.Stretch(safe);
            safe.gameObject.AddComponent<SafeArea>();

            var content = UIFactory.Rect("Content", safe);
            UIFactory.Stretch(content);
            screenRoot = root.gameObject;
            return content;
        }

        static RectOffset Pad(float l, float t, float r, float b) => new RectOffset((int)Px(l), (int)Px(r), (int)Px(t), (int)Px(b));

        // ---------- Screen 1: What are you looking for? ----------
        public static InputScreen BuildInput(UITheme th, Transform screens)
        {
            var content = NewScreen(th, screens, "Input", out var go);
            UIFactory.VGroup(content, Px(12), Pad(24, 24, 24, 16));

            var title = UIFactory.Text(content, "Title", "What are you looking for?", th.heading, 32, th.grape, TextAlignmentOptions.TopLeft);
            title.lineSpacing = -10;   // Fredoka's default leading is loose for a big headline
            var label1 = UIFactory.Text(content, "Label", "One of your things", th.bodyExtraBold, 16, th.muted, TextAlignmentOptions.MidlineLeft);
            UIFactory.Size(label1.rectTransform, -1, Px(28));

            // Quick-pick chips in two rows, as in the design (a flow layout would wrap, but two fixed rows is simpler).
            var chips = new System.Collections.Generic.List<UIChip>();
            var row1 = ChipRow(content, "ChipsRow1");
            chips.Add(UIFactory.Chip(th, row1, "Keys", "Keys", false, Grape("icon_key"), White("icon_key"), true));
            chips.Add(UIFactory.Chip(th, row1, "Wallet", "Wallet", false, Grape("icon_wallet"), White("icon_wallet"), true));
            chips.Add(UIFactory.Chip(th, row1, "Glasses", "Glasses", false, Grape("icon_glasses"), White("icon_glasses"), true));
            var row2 = ChipRow(content, "ChipsRow2");
            chips.Add(UIFactory.Chip(th, row2, "IdCard", "ID card", false, Grape("icon_id"), White("icon_id"), true));

            var label2 = UIFactory.Text(content, "Label2", "No photo? Describe it", th.bodyExtraBold, 16, th.grape, TextAlignmentOptions.MidlineLeft);
            UIFactory.Size(label2.rectTransform, -1, Px(28));

            var inputRow = UIFactory.Rect("InputRow", content);
            UIFactory.Size(inputRow, -1, Px(52));
            UIFactory.HGroup(inputRow, Px(10), new RectOffset(), TextAnchor.MiddleCenter, false);   // text box flexes, mic keeps its size
            var field = UIFactory.InputField(th, inputRow, "Field", "Black leather wallet, a bit worn");
            UIFactory.IconButton(th, inputRow, "Mic", Grape("icon_mic"), th.lavender, 52);   // decorative for now (guide: mic does nothing yet)

            UIFactory.Spacer(content, 1);
            var cont = UIFactory.Button(th, content, "Continue", "Continue", ButtonStyle.Primary, 56);

            var screen = go.AddComponent<InputScreen>();
            screen.field = field; screen.continueButton = cont; screen.chips = chips.ToArray();
            return screen;
        }

        static RectTransform ChipRow(Transform parent, string name)
        {
            var row = UIFactory.Rect(name, parent);
            UIFactory.Size(row, -1, Px(40));
            UIFactory.HGroup(row, Px(8), new RectOffset(), TextAnchor.MiddleLeft);
            return row;
        }

        // ---------- Screen 2: Getting to know your wallet ----------
        public static AnalyzingScreen BuildAnalyzing(UITheme th, Transform screens)
        {
            var content = NewScreen(th, screens, "Analyzing", out var go);
            UIFactory.VGroup(content, Px(12), Pad(24, 0, 24, 16), TextAnchor.MiddleCenter);

            UIFactory.Spacer(content, 1);
            var mascot = UIFactory.Mascot(content, "Mascot", Mascot("mascot_look_scan"), 165);
            UIFactory.Spacer(content, 0, 16);
            var title = UIFactory.Text(content, "Title", "Getting to know your item", th.heading, 28, th.grape, TextAlignmentOptions.Top);
            title.lineSpacing = -10;
            var sub = UIFactory.Text(content, "Sub", "This takes a few seconds.", th.bodyRegular, 16, th.muted, TextAlignmentOptions.Top);
            UIFactory.Size(sub.rectTransform, -1, Px(28));

            // Three checklist rows, kept narrow and left-aligned like the design.
            var list = UIFactory.Rect("Steps", content);
            UIFactory.VGroup(list, Px(14), Pad(48, 12, 48, 0), TextAnchor.UpperLeft);
            var check = White("icon_check");
            var rows = new[]
            {
                UIFactory.StepRow(th, list, "Step1", "Shape and color", check, th.marshmallow),
                UIFactory.StepRow(th, list, "Step2", "Rough size", check, th.marshmallow),
                UIFactory.StepRow(th, list, "Step3", "Where it likes to hide", check, th.marshmallow),
            };
            // The check mark sits on a mint circle and the design shows it dark; tint the white art grape.
            foreach (var r in rows) r.check.color = th.grape;

            UIFactory.Spacer(content, 1.4f);

            var screen = go.AddComponent<AnalyzingScreen>();
            screen.title = title; screen.rows = rows; screen.mascot = mascot.rectTransform;
            return screen;
        }

        // ---------- Screen 3: Item card ----------
        public static UISpotRow BuildSpotRowPrefab(UITheme th, string path)
        {
            var temp = new GameObject("SpotRow", typeof(RectTransform));
            var rt = (RectTransform)temp.transform;
            UIFactory.HGroup(rt, Px(14), new RectOffset(), TextAnchor.UpperLeft);

            var tile = UIFactory.Img(rt, "Number", th.rounded18, th.grape);
            UIFactory.Size(tile.rectTransform, Px(30), Px(30));
            var num = UIFactory.Text(tile.transform, "Text", "1", th.heading, 15, th.white);

            var col = UIFactory.Rect("Texts", rt);
            UIFactory.VGroup(col, Px(2), new RectOffset(), TextAnchor.UpperLeft);
            UIFactory.Size(col, -1, -1, 1);
            var title = UIFactory.Text(col, "Title", "Place", th.heading, 18, th.grape, TextAlignmentOptions.TopLeft);
            var sub = UIFactory.Text(col, "Subtitle", "", th.bodyRegular, 14, th.muted, TextAlignmentOptions.TopLeft);

            var row = temp.AddComponent<UISpotRow>();
            row.number = num; row.title = title; row.subtitle = sub;

            var prefab = PrefabUtility.SaveAsPrefabAsset(temp, path);
            Object.DestroyImmediate(temp);
            return prefab.GetComponent<UISpotRow>();
        }

        public static ItemCardScreen BuildItemCard(UITheme th, Transform screens, UISpotRow rowPrefab)
        {
            var content = NewScreen(th, screens, "ItemCard", out var go);
            UIFactory.VGroup(content, Px(14), Pad(20, 12, 20, 16));

            // Back button, left-aligned (wrapped in a row so the layout doesn't stretch it full width).
            var backRow = UIFactory.Rect("BackRow", content);
            UIFactory.Size(backRow, -1, Px(44));
            UIFactory.HGroup(backRow, 0, new RectOffset(), TextAnchor.MiddleLeft);
            var back = UIFactory.IconButton(th, backRow, "Back", Grape("icon_back"), th.white, 44, true);

            // Header card: yellow item tile + name + shape/size tags.
            var cardFill = UIFactory.Bordered(content, "HeaderCard", th.rounded28, th.cloud, th.white, out _);
            UIFactory.HGroup(cardFill.transform, Px(16), Pad(16, 16, 16, 16), TextAnchor.MiddleLeft);
            var tile = UIFactory.IconTile(th, cardFill.transform, "ItemTile", th.fallbackItemIcon, th.lemon, Color.white, 72);
            var texts = UIFactory.Rect("Texts", cardFill.transform);
            UIFactory.VGroup(texts, Px(8), new RectOffset(), TextAnchor.MiddleLeft);
            UIFactory.Size(texts, -1, -1, 1);
            var name = UIFactory.Text(texts, "Name", "Black wallet", th.heading, 26, th.grape, TextAlignmentOptions.MidlineLeft);
            var tags = UIFactory.Rect("Tags", texts);
            UIFactory.Size(tags, -1, Px(26));
            UIFactory.HGroup(tags, Px(8), new RectOffset(), TextAnchor.MiddleLeft);
            var shapeChip = UIFactory.Chip(th, tags, "Shape", "Flat", false, null, null, false, true);
            var sizeChip = UIFactory.Chip(th, tags, "Size", "11×9 cm", false, null, null, false, true);

            var h2 = UIFactory.Text(content, "SpotsTitle", "Likely hiding spots", th.heading, 22, th.grape, TextAlignmentOptions.MidlineLeft);
            UIFactory.Size(h2.rectTransform, -1, Px(34));

            var list = UIFactory.Rect("SpotList", content);
            UIFactory.VGroup(list, Px(16), new RectOffset(), TextAnchor.UpperLeft);

            UIFactory.Spacer(content, 1);
            var start = UIFactory.Button(th, content, "Start", "Start scanning", ButtonStyle.Primary, 56, White("icon_camera"));

            var screen = go.AddComponent<ItemCardScreen>();
            screen.theme = th;
            screen.itemIcon = tile.transform.Find("Icon").GetComponent<Image>();
            screen.itemName = name; screen.shapeChip = shapeChip; screen.sizeChip = sizeChip;
            screen.spotList = list; screen.rowPrefab = rowPrefab;
            screen.backButton = back; screen.startButton = start;
            return screen;
        }

        // ---------- Screen 6: Spot panel (bottom sheet) ----------
        public static SpotPanel BuildSpotPanel(UITheme th, Transform parent)
        {
            var sheet = UIFactory.Sheet(th, parent, "SpotPanel", 0, out var content);   // 0 = height follows content

            // Mascot on the left, chip + spot name on the right.
            var head = UIFactory.Rect("Head", content);
            UIFactory.HGroup(head, Px(14), new RectOffset(), TextAnchor.MiddleLeft);
            UIFactory.Mascot(head, "Mascot", Mascot("mascot_look"), 52);
            var col = UIFactory.Rect("Texts", head);
            UIFactory.VGroup(col, Px(6), new RectOffset(), TextAnchor.MiddleLeft);
            UIFactory.Size(col, -1, -1, 1);

            var chipRow = UIFactory.Rect("ChipRow", col);
            UIFactory.Size(chipRow, -1, Px(26));
            UIFactory.HGroup(chipRow, 0, new RectOffset(), TextAnchor.MiddleLeft);
            var chip = UIFactory.Img(chipRow, "Chip", th.rounded18, th.lemon);
            UIFactory.SetRadius(chip, th.rounded18, 8);
            chip.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            UIFactory.HGroup(chip.transform, 0, Pad(12, 0, 12, 0));
            var chipText = UIFactory.Text(chip.transform, "Text", "Only glanced", th.bodyExtraBold, 13, th.grape);

            var title = UIFactory.Text(col, "Title", "Drawer", th.heading, 24, th.grape, TextAlignmentOptions.MidlineLeft);
            var body = UIFactory.Text(content, "Body", "", th.bodyRegular, 16, th.muted, TextAlignmentOptions.TopLeft);
            var bar = UIFactory.Bar(th, content, "Dwell", th.mint, 10);

            var buttons = UIFactory.Rect("Buttons", content);
            UIFactory.Size(buttons, -1, Px(56) + UIFactory.EdgePx);
            UIFactory.HGroup(buttons, Px(12), new RectOffset(), TextAnchor.MiddleCenter, false);
            var checkedBtn = UIFactory.Button(th, buttons, "Checked", "Checked", ButtonStyle.Secondary, 56, Grape("icon_check"));
            UIFactory.Size(checkedBtn, -1, -1, 1);
            var foundBtn = UIFactory.Button(th, buttons, "Found", "Found it", ButtonStyle.Success, 56);
            UIFactory.Size(foundBtn, -1, -1, 1);
            buttons.gameObject.SetActive(false);

            var panel = sheet.gameObject.AddComponent<SpotPanel>();
            panel.theme = th; panel.sheet = sheet;
            panel.chipBackground = chip; panel.chipText = chipText; panel.title = title; panel.body = body;
            panel.dwellBar = bar; panel.buttonsRow = buttons.gameObject;
            panel.checkedButton = checkedBtn; panel.foundButton = foundBtn;
            return panel;
        }

        // ---------- Screen 8: "Is this your wallet?" sheet ----------
        public static MatchSheet BuildMatchSheet(UITheme th, Transform parent)
        {
            var sheet = UIFactory.Sheet(th, parent, "MatchSheet", 0, out var content);

            var title = UIFactory.Text(content, "Title", "Is this your wallet?", th.heading, 26, th.grape, TextAlignmentOptions.MidlineLeft);
            var body = UIFactory.Text(content, "Body", "", th.bodyRegular, 17, th.muted, TextAlignmentOptions.TopLeft);
            UIFactory.Spacer(content, 0, 4);

            var buttons = UIFactory.Rect("Buttons", content);
            UIFactory.Size(buttons, -1, Px(56) + UIFactory.EdgePx);
            UIFactory.HGroup(buttons, Px(12), new RectOffset(), TextAnchor.MiddleCenter, false);
            var notIt = UIFactory.Button(th, buttons, "NotIt", "Not it", ButtonStyle.Secondary, 56);
            UIFactory.Size(notIt, -1, -1, 1);
            var yes = UIFactory.Button(th, buttons, "Yes", "Yes, that’s it!", ButtonStyle.Success, 56);
            UIFactory.Size(yes, -1, -1, 1);

            var match = sheet.gameObject.AddComponent<MatchSheet>();
            match.sheet = sheet; match.title = title; match.body = body; match.notItButton = notIt; match.yesButton = yes;
            return match;
        }

        // ---------- Screen 9: Found it! ----------
        public static FoundScreen BuildFound(UITheme th, Transform screens)
        {
            var content = NewScreen(th, screens, "Found", out var go, false);   // camera shows behind the card
            UIFactory.VGroup(content, Px(12), Pad(24, 0, 24, 16), TextAnchor.MiddleCenter);

            UIFactory.Spacer(content, 1);
            var card = UIFactory.Img(content, "Card", th.rounded28, th.white);
            UIFactory.VGroup(card.transform, Px(10), Pad(24, 84, 24, 24), TextAnchor.UpperCenter);

            // The mascot pokes out of the top of the card: it ignores the layout and is pinned to the card's top edge.
            var mascot = UIFactory.Mascot(card.transform, "Mascot", Mascot("mascot_happy_wave"), 80);
            mascot.GetComponent<LayoutElement>().ignoreLayout = true;
            var mr = mascot.rectTransform;
            mr.anchorMin = mr.anchorMax = new Vector2(0.5f, 1f);
            mr.pivot = new Vector2(0.5f, 1f);
            mr.anchoredPosition = new Vector2(0f, Px(46));      // 46 px above the card's top edge

            var title = UIFactory.Text(card.transform, "Title", "Found it!", th.heading, 40, th.grape);
            var sentence = UIFactory.Text(card.transform, "Sentence", "Your item was hiding in the bag.", th.bodyRegular, 18, th.grape, TextAlignmentOptions.Top);
            UIFactory.Spacer(card.transform, 0, 8);
            var done = UIFactory.Button(th, card.transform, "Done", "Nice, done", ButtonStyle.Success, 56);
            UIFactory.Spacer(content, 1.2f);

            // Confetti layer: full screen, on top, 40 reusable pieces.
            var layer = UIFactory.Rect("Confetti", go.transform);
            UIFactory.Stretch(layer);
            var pieces = new Image[40];
            for (int i = 0; i < pieces.Length; i++)
            {
                pieces[i] = UIFactory.Img(layer, "Piece" + i, th.rounded18, th.bubblegum);
                UIFactory.SetRadius(pieces[i], th.rounded18, 3);
                var pr = pieces[i].rectTransform;
                pr.anchorMin = pr.anchorMax = new Vector2(0.5f, 0.5f);   // positions are relative to the screen centre
                pr.sizeDelta = new Vector2(Px(10), Px(18));
                pieces[i].enabled = false;
            }
            var confetti = layer.gameObject.AddComponent<Confetti>();
            confetti.area = layer; confetti.pieces = pieces;
            confetti.colors = new[] { th.bubblegum, th.mint, th.lemon, th.lavender, th.soda, th.tangerine };

            var screen = go.AddComponent<FoundScreen>();
            screen.sentence = sentence; screen.mascot = mr; screen.confetti = confetti; screen.doneButton = done;
            return screen;
        }

        // ---------- Screen 10: Nice search! (summary) ----------
        public static SummaryScreen BuildSummary(UITheme th, Transform screens)
        {
            var content = NewScreen(th, screens, "Summary", out var go);
            UIFactory.VGroup(content, Px(16), Pad(20, 16, 20, 16));

            var head = UIFactory.Rect("Head", content);
            UIFactory.Size(head, -1, Px(80));
            UIFactory.HGroup(head, Px(16), Pad(8, 0, 0, 0), TextAnchor.MiddleLeft);
            UIFactory.Mascot(head, "Mascot", Mascot("mascot_happy"), 50);
            var title = UIFactory.Text(head, "Title", "Nice search!", th.heading, 32, th.grape, TextAlignmentOptions.MidlineLeft);
            UIFactory.Size(title.rectTransform, -1, -1, 1);

            // Three stat tiles side by side: time (pink), spots checked (blue), scans (yellow).
            var row = UIFactory.Rect("Stats", content);
            UIFactory.Size(row, -1, Px(104));
            UIFactory.HGroup(row, Px(10), new RectOffset(), TextAnchor.MiddleCenter, false);
            var time = StatTile(th, row, "Time", th.bubblegum, "Time");
            var spots = StatTile(th, row, "Spots", th.soda, "Spots checked");
            var scans = StatTile(th, row, "Scans", th.lemon, "Scans");

            var card = UIFactory.Card(th, content, "FoundCard");
            var foundLine = UIFactory.Text(card, "Line", "Your item was in the bag.", th.heading, 20, th.grape, TextAlignmentOptions.MidlineLeft);

            UIFactory.Spacer(content, 1);
            var home = UIFactory.Button(th, content, "Home", "Back home", ButtonStyle.Primary, 56);

            var screen = go.AddComponent<SummaryScreen>();
            screen.timeValue = time; screen.spotsValue = spots; screen.scansValue = scans;
            screen.foundLine = foundLine; screen.homeButton = home;
            return screen;
        }

        // One coloured stat tile: big value on top, small label under it. Returns the value text.
        static TMP_Text StatTile(UITheme th, Transform parent, string name, Color color, string label)
        {
            var tile = UIFactory.Img(parent, name, th.rounded28, color);
            UIFactory.Size(tile.rectTransform, -1, Px(104), 1);
            UIFactory.VGroup(tile.transform, Px(2), Pad(14, 14, 14, 12), TextAnchor.UpperLeft);
            var value = UIFactory.Text(tile.transform, "Value", "0", th.heading, 30, th.grape, TextAlignmentOptions.MidlineLeft);
            UIFactory.Text(tile.transform, "Label", label, th.bodyExtraBold, 14, th.grape, TextAlignmentOptions.TopLeft);
            return value;
        }

        // ---------- Screen 11: all spots checked ----------
        public static AllCheckedSheet BuildAllChecked(UITheme th, Transform parent)
        {
            var sheet = UIFactory.Sheet(th, parent, "AllCheckedSheet", 0, out var content);

            var head = UIFactory.Rect("Head", content);
            UIFactory.HGroup(head, Px(14), new RectOffset(), TextAnchor.MiddleLeft);
            UIFactory.Mascot(head, "Mascot", Mascot("mascot_calm"), 48);
            var texts = UIFactory.Rect("Texts", head);
            UIFactory.VGroup(texts, Px(4), new RectOffset(), TextAnchor.MiddleLeft);
            UIFactory.Size(texts, -1, -1, 1);
            UIFactory.Text(texts, "Title", "All spots checked", th.heading, 24, th.grape, TextAlignmentOptions.MidlineLeft);
            UIFactory.Text(texts, "Body", "Not found yet, and that’s okay. Let’s scan another part of the room.", th.bodyRegular, 16, th.muted, TextAlignmentOptions.TopLeft);

            UIFactory.Spacer(content, 0, 4);
            var scan = UIFactory.Button(th, content, "Scan", "Scan another area", ButtonStyle.Primary, 56, White("icon_camera"));
            var notNow = UIFactory.Button(th, content, "NotNow", "Not now", ButtonStyle.Secondary, 56);

            var comp = sheet.gameObject.AddComponent<AllCheckedSheet>();
            comp.sheet = sheet; comp.scanButton = scan; comp.notNowButton = notNow;
            return comp;
        }

        // ---------- Screen 4: Move your phone slowly (card over the camera) ----------
        public static ReadinessScreen BuildReadiness(UITheme th, Transform screens)
        {
            var content = NewScreen(th, screens, "Readiness", out var go, false);   // no background: the camera shows through
            UIFactory.VGroup(content, Px(12), Pad(24, 0, 24, 16));

            UIFactory.Spacer(content, 1);
            var card = UIFactory.Img(content, "Card", th.rounded28, th.white);
            UIFactory.VGroup(card.transform, Px(10), Pad(22, 22, 22, 22));

            var tileRow = UIFactory.Rect("TileRow", card.transform);
            UIFactory.Size(tileRow, -1, Px(64));
            UIFactory.HGroup(tileRow, 0, new RectOffset(), TextAnchor.MiddleCenter);
            UIFactory.IconTile(th, tileRow, "Tile", Grape("icon_phone"), th.lavender, Color.white, 64);

            var title = UIFactory.Text(card.transform, "Title", "Move your phone slowly", th.heading, 24, th.grape);
            var body = UIFactory.Text(card.transform, "Body", "Point at the floor and furniture so we can map the room.", th.bodyRegular, 16, th.muted, TextAlignmentOptions.Top);
            UIFactory.Spacer(card.transform, 0, 6);
            var bar = UIFactory.Bar(th, card.transform, "Bar", th.mint, 10);

            UIFactory.Spacer(content, 1.6f);
            var cancel = UIFactory.Button(th, content, "Cancel", "Cancel", ButtonStyle.Secondary, 56);

            var screen = go.AddComponent<ReadinessScreen>();
            screen.bar = bar; screen.cancelButton = cancel;
            return screen;
        }

        // ---------- Screen 5: Scan HUD (over the camera) ----------
        public static ScanHudScreen BuildHud(UITheme th, Transform screens)
        {
            var content = NewScreen(th, screens, "Hud", out var go, false);
            UIFactory.VGroup(content, Px(8), Pad(16, 8, 16, 12));

            // Top bar: item tile + "Looking for / name / progress bar" + big "2/5 checked".
            var face = UIFactory.EdgedCard(th, content, "TopBar", 68, 4, 20);
            UIFactory.HGroup(face.transform, Px(12), Pad(14, 12, 14, 12), TextAnchor.MiddleLeft);
            var tile = UIFactory.IconTile(th, face.transform, "ItemTile", th.fallbackItemIcon, th.bubblegum, Color.white, 44);
            var mid = UIFactory.Rect("Mid", face.transform);
            UIFactory.VGroup(mid, Px(2), new RectOffset(), TextAnchor.MiddleLeft);
            UIFactory.Size(mid, -1, -1, 1);
            var lookingFor = UIFactory.Text(mid, "LookingFor", "Looking for", th.bodyBold, 12, th.muted, TextAlignmentOptions.MidlineLeft);
            var itemName = UIFactory.Text(mid, "Name", "Black wallet", th.heading, 17, th.grape, TextAlignmentOptions.MidlineLeft);
            var bar = UIFactory.Bar(th, mid, "Bar", th.soda, 8);
            var right = UIFactory.Rect("Right", face.transform);
            UIFactory.VGroup(right, 0, new RectOffset(), TextAnchor.MiddleRight);
            UIFactory.Size(right, Px(64), -1);
            var count = UIFactory.Text(right, "Count", "", th.heading, 22, th.grape, TextAlignmentOptions.MidlineRight);
            var checkedLabel = UIFactory.Text(right, "CheckedLabel", "", th.bodyBold, 11, th.muted, TextAlignmentOptions.MidlineRight);

            // Legend: what the cue colours mean.
            var legend = UIFactory.Rect("Legend", content);
            UIFactory.Size(legend, -1, Px(28));
            UIFactory.HGroup(legend, Px(6), new RectOffset(), TextAnchor.MiddleLeft);
            UIFactory.LegendChip(th, legend, "Not seen", th.cloud);
            UIFactory.LegendChip(th, legend, "Glanced", th.lemon);
            UIFactory.LegendChip(th, legend, "Checked", th.soda);

            UIFactory.Spacer(content, 1);   // the middle stays empty: that's where the AR cues are

            // Status line (e.g. "Found 3 places to check"). Hidden when there is no message.
            var status = UIFactory.Img(content, "Status", th.rounded18, new Color(th.grapeDark.r, th.grapeDark.g, th.grapeDark.b, 0.92f));
            UIFactory.SetRadius(status, th.rounded18, 14);
            UIFactory.VGroup(status.transform, 0, Pad(16, 10, 16, 10));
            var statusText = UIFactory.Text(status.transform, "Text", "", th.bodyBold, 15, th.white);

            var scanMore = UIFactory.Button(th, content, "ScanMore", "Scan another area", ButtonStyle.Secondary, 44, null, 15);

            // Bottom row: stop | main button | calm.
            var bottom = UIFactory.Rect("Bottom", content);
            UIFactory.Size(bottom, -1, Px(HudButtonHeight) + UIFactory.EdgePx);
            // expandW=false: side buttons keep their fixed width, only the main button (flexibleWidth 1) takes the leftover space.
            UIFactory.HGroup(bottom, Px(12), new RectOffset(), TextAnchor.MiddleCenter, false);
            // Square chunky buttons (same 56 face + 5 edge as the main button) so all three line up.
            var stop = UIFactory.Button(th, bottom, "Stop", "", ButtonStyle.Secondary, HudButtonHeight, White("icon_x"));
            UIFactory.Size(stop, Px(HudSideButtonWidth), Px(HudButtonHeight) + UIFactory.EdgePx);
            var main = UIFactory.Button(th, bottom, "Main", "Scan this area", ButtonStyle.Primary, HudButtonHeight, White("icon_camera"));
            UIFactory.Size(main, -1, -1, 1);
            var calm = UIFactory.Button(th, bottom, "Calm", "", ButtonStyle.Calm, HudButtonHeight, White("icon_breath"));
            UIFactory.Size(calm, Px(HudSideButtonWidth), Px(HudButtonHeight) + UIFactory.EdgePx);

            var screen = go.AddComponent<ScanHudScreen>();
            screen.theme = th;
            screen.itemIcon = tile.transform.Find("Icon").GetComponent<Image>();
            screen.itemName = itemName; screen.bar = bar; screen.countText = count; screen.checkedLabel = checkedLabel;
            screen.statusChip = status.gameObject; screen.statusText = statusText;
            screen.mainButton = main; screen.scanMoreButton = scanMore;
            screen.stopButton = stop.Button; screen.calmButton = calm.Button;
            screen.scanIcon = White("icon_camera"); screen.foundIcon = White("icon_check");
            status.gameObject.SetActive(false);
            scanMore.gameObject.SetActive(false);
            return screen;
        }
    }
}
