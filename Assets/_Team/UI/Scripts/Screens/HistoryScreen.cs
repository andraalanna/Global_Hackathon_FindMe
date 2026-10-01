using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FindMe.UI
{
    /// <summary>
    /// Prototype "history" tab. "Searches": a pattern card + every search with its result ("Mark found" on lost ones).
    /// "Last seen": where each registered thing was last seen.
    /// </summary>
    public class HistoryScreen : UIScreen
    {
        Chip searches, lastSeen;
        Transform content;

        protected override void Build()
        {
            var page = Page();
            Widgets.Header(page.transform, "", "History");
            var body = Body(page.transform).transform;
            var chips = Widgets.Chips(body);
            searches = Widgets.Chip(chips.transform, "Searches", true, c => SetTab("searches"));
            lastSeen = Widgets.Chip(chips.transform, "Last seen", false, c => SetTab("last"));
            UIKit.Space(body, 16f);
            content = UIKit.Column(body, "Content", 0f).transform;
            content.GetComponent<VerticalLayoutGroup>().childForceExpandWidth = true;
            TabBar.Create(page.transform, App, "history");
        }

        public override void OnShow() => SetTab(App.State.historyTab);

        void SetTab(string tab)
        {
            App.State.historyTab = tab;
            bool s = tab == "searches";
            searches.Set(s);
            lastSeen.Set(!s);
            UIKit.Clear(content);
            if (s) Searches(); else LastSeen();
        }

        void Searches()
        {
            // .pattern on grape: "Keys go missing on busy mornings" + Set an evening check.
            var card = UIKit.Column(content, "Pattern", 0f, UIKit.Pad(18, 18, 18, 18));
            card.childForceExpandWidth = true;
            UIKit.Backdrop(card.transform, T.grape, 26f);
            UIKit.Text(card.transform, "Pattern spotted", TextStyle.Label, T.lavender).fontSize = 14.5f;
            UIKit.Space(card.transform, 4f);
            var h = UIKit.Text(card.transform, "Keys go missing on busy mornings", TextStyle.H2, T.white);
            h.fontSize = 21f;
            UIKit.Space(card.transform, 8f);
            var p = UIKit.Text(card.transform, "3 of your last 4 key searches were between 7 and 9 AM.", TextStyle.Body, new Color32(0xD8, 0xD2, 0xEE, 255));
            p.font = T.bodyBold;
            p.lineSpacing = 6f;
            UIKit.Space(card.transform, 14f);
            var btnRow = UIKit.Row(card.transform, "BtnRow");
            PillButton(btnRow.transform, "Set an evening check", T.bubblegum, T.grape, () => App.Toast("Evening check is set for 20:30"));
            UIKit.Space(content, 16f);

            var list = Widgets.ListBox(content);
            foreach (var e in App.State.history)
            {
                var entry = e;
                var row = Widgets.ListRow(list);
                Widgets.IconTile(row.transform, e.icon, UIKit.Swatch(e.color), 46f);
                var col = UIKit.Column(row.transform, "Text", 2f);
                UIKit.Size(col, 0f, -1, 1);
                var top = UIKit.Row(col.transform, "Name", 6f);
                UIKit.Text(top.transform, e.name, TextStyle.Body);
                if (e.oneTime) Widgets.Tag(top.transform, "One-time", T.lemonLight, new Color32(0x8A, 0x6A, 0x00, 255));
                else Widgets.Tag(top.transform, "Registered", T.lavenderLight, new Color32(0x5B, 0x45, 0xB8, 255));
                var res = UIKit.Text(col.transform, $"<color=#{ColorUtility.ToHtmlStringRGB(e.ok ? T.mintEdge : T.bubblegumEdge)}><b>{e.result}</b></color> · {e.when}", TextStyle.Small);
                res.textWrappingMode = TextWrappingModes.Normal;
                if (e.lost)
                    PillButton(row.transform, "Mark found", T.white, T.grape, () =>
                    {
                        entry.lost = false; entry.ok = true; entry.result = "Found later";
                        App.Toast("Marked as found");
                        SetTab("searches");
                    }, outline: true, small: true);
            }
        }

        void LastSeen()
        {
            var list = Widgets.ListBox(content);
            foreach (var t in App.State.mine)
            {
                var thing = t;
                var row = Widgets.ListRow(list, () => { App.State.selected = thing; App.Router.Go("item"); });
                Widgets.IconTile(row.transform, t.icon, UIKit.Swatch(t.color), 44f);
                var col = UIKit.Column(row.transform, "Text", 2f);
                UIKit.Size(col, 0f, -1, 1);
                UIKit.Text(col.transform, t.name, TextStyle.Body);
                UIKit.Text(col.transform, t.last, TextStyle.Small);
                UIKit.Text(row.transform, t.lastTime, TextStyle.Small);
            }
        }

        /// <summary>.pillb: fully round text button (grape, pink or white outline).</summary>
        public static void PillButton(Transform parent, string label, Color bg, Color fg, System.Action onClick,
                                      bool outline = false, bool small = false)
        {
            var row = UIKit.Row(parent, "Pill " + label, 0f, small ? UIKit.Pad(12, 8, 12, 8) : UIKit.Pad(22, 12, 22, 12), TextAnchor.MiddleCenter);
            var hit = outline ? UIKit.Frame(row.transform, UIKit.T.white, UIKit.T.cloud, 99f) : UIKit.Backdrop(row.transform, bg, 99f);
            var t = UIKit.Text(row.transform, label, TextStyle.Label, fg);
            t.fontSize = small ? 13.5f : 16f;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            Widgets2.Tap(row.gameObject, hit, onClick);
        }
    }
}
