using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FindMe.UI
{
    /// <summary>
    /// Prototype "item": one registered thing. Last seen / usually lives tiles, voice note, where it turns up
    /// (probability bars), evening reminder, and "Find my …" which starts a search for it.
    /// </summary>
    public class ItemScreen : UIScreen
    {
        Transform body;
        TextMeshProUGUI findLabel;

        protected override void Build()
        {
            var page = Page();
            Widgets.TopBar(page.transform, "", App.Router.Back,
                p => Widgets.TextSquare(p, "Edit", () => App.Toast("Editing comes in a later phase")));
            body = Body(page.transform).transform;
            var foot = Foot(page.transform);
            var btn = UIKit.Button(foot.transform, "Find my keys", ButtonKind.Primary, App.BeginSearch, "search");
            findLabel = btn.Label;
        }

        public override void OnShow()
        {
            var x = App.State.selected ?? App.State.mine[0];
            App.State.selected = x;
            findLabel.text = "Find my " + x.name.ToLower();
            UIKit.Clear(body);

            var head = UIKit.Row(body, "Head", 16f);
            Widgets.IconTile(head.transform, x.icon, UIKit.Swatch(x.color), 80f);
            var hc = UIKit.Column(head.transform, "Text", 2f);
            UIKit.Size(hc, 0f, -1, 1);
            UIKit.Text(hc.transform, x.name, TextStyle.Title);
            UIKit.Text(hc.transform, $"Found {x.finds} times · usually in {x.avg}", TextStyle.Small);
            UIKit.Space(body, 18f);

            var duo = UIKit.Row(body, "Duo", 10f, null, TextAnchor.UpperLeft);
            duo.childForceExpandWidth = true;
            duo.childForceExpandHeight = true;
            UIKit.Size(Widgets2.Stat(duo.transform, T.mint, x.last, "Last seen", true, x.lastTime, 19f), 0f, -1, 1);
            UIKit.Size(Widgets2.Stat(duo.transform, T.soda, x.spot, "Usually lives", true, null, 19f), 0f, -1, 1);

            if (!string.IsNullOrEmpty(x.voice))
            {
                UIKit.Space(body, 12f);
                var v = UIKit.Row(body, "VoiceNote", 14f, UIKit.Pad(18, 18, 18, 18));
                var bg = UIKit.Frame(v.transform, T.white, T.cloud, 26f);
                var play = UIKit.Box(v.transform, "Play", T.lavender, 16f);
                UIKit.Size(play, 58f, 58f);
                var pi = UIKit.Icon(play.transform, "play", 26f);
                UIKit.Place((RectTransform)pi.transform, new Vector2(0.5f, 0.5f), new Vector2(26f, 26f));
                var vc = UIKit.Column(v.transform, "Text", 2f);
                UIKit.Size(vc, 0f, -1, 1);
                UIKit.Text(vc.transform, "Your voice note", TextStyle.Body);
                var q = UIKit.Text(vc.transform, x.voice, TextStyle.Small);
                q.fontSize = 14.5f;
                Widgets2.Tap(v.gameObject, bg, () => App.Toast("Playing voice note…"));
            }

            UIKit.Space(body, 24f);
            UIKit.Text(body, "Where it turns up", TextStyle.H2);
            UIKit.Space(body, 12f);
            foreach (var (place, pct) in x.top)
            {
                var r = UIKit.Row(body, "Place", 0f);
                Widgets.Grow(UIKit.Text(r.transform, place, TextStyle.Label));
                UIKit.Text(r.transform, pct + "%", TextStyle.Small);
                UIKit.Space(body, 6f);
                Widgets2.Bar(body, pct / 100f, T.bubblegum);
                UIKit.Space(body, 12f);
            }

            UIKit.Space(body, 10f);
            var list = Widgets.ListBox(body);
            Widgets2.NavRow(list, "moon", T.lavender, "Remind me every evening", () => App.Toast("Evening check is set for 20:30"));
        }
    }
}
