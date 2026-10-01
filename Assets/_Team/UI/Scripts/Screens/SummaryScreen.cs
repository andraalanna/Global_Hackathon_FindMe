using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FindMe.UI
{
    /// <summary>
    /// Prototype "summary": item + Found tag, three stat tiles (time · spots · mode), "Found at", and a note:
    /// "Noted" for registered things, or "Save it to Your things?" for one-time searches.
    /// </summary>
    public class SummaryScreen : UIScreen
    {
        Transform body;

        protected override void Build()
        {
            var page = Page();
            Widgets.TopBar(page.transform, "Search summary", null);
            body = Body(page.transform).transform;
            var foot = Foot(page.transform);
            UIKit.Button(foot.transform, "Back home", ButtonKind.Dark, () => App.Router.Go("find"));
        }

        public override void OnShow()
        {
            UIKit.Clear(body);
            var v = App.Search;
            var x = v.thing ?? App.State.mine[0];

            var head = UIKit.Row(body, "Head", 12f, UIKit.Pad(0, 0, 0, 16));
            Widgets.IconTile(head.transform, x.icon, UIKit.Swatch(x.color), 60f);
            var hc = UIKit.Column(head.transform, "Text", 4f);
            var n = UIKit.Text(hc.transform, x.name, TextStyle.H2);
            n.fontSize = 24f;
            var tags = UIKit.Row(hc.transform, "Tags", 6f);
            Widgets.Tag(tags.transform, "Found", T.mintLight, T.mintEdge);
            if (x.oneTime) Widgets.Tag(tags.transform, "One-time", T.lemonLight, new Color32(0x8A, 0x6A, 0x00, 255));
            else Widgets.Tag(tags.transform, "Registered", T.lavenderLight, new Color32(0x5B, 0x45, 0xB8, 255));

            // Spots checked: from the backend's session when there is one, else the HUD's counter.
            int spots = v.session != null ? v.session.CheckedCount : v.checkedCount;
            var stats = UIKit.Row(body, "Stats", 10f, null, TextAnchor.UpperLeft);
            stats.childForceExpandWidth = true;
            stats.childForceExpandHeight = true;
            UIKit.Size(Widgets2.Stat(stats.transform, T.mint, v.ElapsedText, "Time"), 0f, -1, 1);
            UIKit.Size(Widgets2.Stat(stats.transform, T.soda, spots.ToString(), "Spots"), 0f, -1, 1);
            UIKit.Size(Widgets2.Stat(stats.transform, T.lemon, "Quick", "Mode"), 0f, -1, 1);
            UIKit.Space(body, 10f);

            var at = Widgets.Card(body, 14, 4f, 22f);
            UIKit.Text(at.transform, "Found at", TextStyle.Small);
            var b = UIKit.Text(at.transform, UIManager.Cap(v.foundAt ?? "Right here"), TextStyle.H2);
            b.fontSize = 22f;

            // .pattern note
            UIKit.Space(body, 16f);
            var note = Widgets.Card(body, 18, 6f);
            if (x.oneTime)
            {
                UIKit.Text(note.transform, $"Save {x.name.ToLower()} to Your things?", TextStyle.Label).fontSize = 17.5f;
                var p = UIKit.Text(note.transform, "Next time FIND ME will remember where it usually lives and where you last had it.", TextStyle.Body);
                p.lineSpacing = 6f;
                UIKit.Space(note.transform, 8f);
                var row = UIKit.Row(note.transform, "Buttons", 10f);
                HistoryScreen.PillButton(row.transform, "Save to Things", T.grape, T.white, () =>
                {
                    App.Draft = new ThingDraft { name = x.name, icon = x.icon };
                    App.Router.Go("reg1");
                });
                HistoryScreen.PillButton(row.transform, "No thanks", T.white, T.grape, () => App.Router.Go("find"), outline: true);
            }
            else
            {
                var hr = UIKit.Row(note.transform, "Title", 8f);
                UIKit.Circle(hr.transform, "Pin", T.bubblegum, 12f);
                UIKit.Text(hr.transform, "Noted", TextStyle.Label).fontSize = 17.5f;
                var p = UIKit.Text(note.transform, "We updated its last-seen place, so next time FIND ME looks there first.", TextStyle.Body);
                p.lineSpacing = 6f;
            }
        }
    }
}
