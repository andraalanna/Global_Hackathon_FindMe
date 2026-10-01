using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FindMe.UI
{
    /// <summary>Prototype "paywall": Care plan crown tile, 4 features with mint checks, Monthly / Yearly, Subscribe.</summary>
    public class PaywallScreen : UIScreen
    {
        readonly List<(Image border, Image fill)> plans = new();

        protected override void Build()
        {
            var page = Page();
            Widgets.TopBar(page.transform, "", App.Router.Back);
            var body = Body(page.transform).transform;

            var head = UIKit.Column(body, "Head", 0f, null, TextAnchor.UpperCenter);
            UIKit.Space(head.transform, 6f);
            FamilyScreen.CrownTile(head.transform, 96f);
            UIKit.Space(head.transform, 14f);
            UIKit.Text(head.transform, "Care plan", TextStyle.Title, null, TextAlignmentOptions.Center);
            UIKit.Space(head.transform, 10f);
            UIKit.Text(head.transform, "Help someone you love keep track of their things. Unlocks the Family tab.", TextStyle.Lead, null, TextAlignmentOptions.Center);
            UIKit.Space(head.transform, 20f);

            var card = Widgets.Card(body, 18, 12f);
            foreach (var f in new[] { "Add parents and set your role as caregiver", "See when they're searching, and send a hint",
                                      "Set things up together with voice notes", "A weekly note with patterns and conversation starters" })
                Feature(card.transform, f);
            UIKit.Space(card.transform, 4f);
            var row = UIKit.Row(card.transform, "Plans", 10f, null, TextAnchor.UpperLeft);
            row.childForceExpandWidth = true;
            Plan(row.transform, "Monthly", false);
            Plan(row.transform, "Yearly", true);

            var foot = Foot(page.transform);
            UIKit.Button(foot.transform, "Subscribe", ButtonKind.Primary, () =>
            {
                App.State.carePlan = true;
                App.Toast("Care plan active. Family unlocked");
                App.Router.Go("family");
            });
            UIKit.Link(foot.transform, "Not now", App.Router.Back);
        }

        // .feat: 26 px mint circle with a check, then the text.
        void Feature(Transform parent, string text)
        {
            var row = UIKit.Row(parent, "Feature", 12f, null, TextAnchor.UpperLeft);
            var dot = UIKit.Circle(row.transform, "Check", T.mint, 26f);
            var ic = UIKit.Icon(dot.transform, "check", 16f);
            UIKit.Place((RectTransform)ic.transform, new Vector2(0.5f, 0.5f), new Vector2(16f, 16f));
            var t = UIKit.Text(row.transform, text, TextStyle.Body);
            t.lineSpacing = 6f;
            UIKit.Size(t, 0f, -1, 1);
        }

        // .planc: white option card; selected = grape 4 px outline.
        void Plan(Transform parent, string label, bool on)
        {
            var col = UIKit.Column(parent, "Plan " + label, 2f, UIKit.Pad(16, 16, 16, 16));
            UIKit.Size(col, 0f, -1, 1);
            var border = UIKit.Backdrop(col.transform, T.cloud, 22f);
            var fill = UIKit.Backdrop(col.transform, T.white, 20f, 0f, 2f);
            UIKit.Text(col.transform, label, TextStyle.Small);
            var b = UIKit.Text(col.transform, "Price TBD", TextStyle.H2);
            b.fontSize = 19f;
            plans.Add((border, fill));
            int index = plans.Count - 1;
            Widgets2.Tap(col.gameObject, border, () => Select(index));
            if (on) Select(index);
        }

        void Select(int index)
        {
            for (int i = 0; i < plans.Count; i++)
            {
                bool on = i == index;
                plans[i].border.color = on ? T.grape : T.cloud;
                float inset = on ? 4f : 2f;
                UIKit.Stretch((RectTransform)plans[i].fill.transform, inset, inset, inset, inset);
            }
        }
    }
}
