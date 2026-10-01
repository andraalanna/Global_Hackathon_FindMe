using TMPro;
using UnityEngine;

namespace FindMe.UI
{
    /// <summary>Prototype "askreg": bobbing mascot, "Want to add your things now?", 4 item tiles.</summary>
    public class AskRegisterScreen : UIScreen
    {
        protected override bool UsesFullTop => true;

        protected override void Build()
        {
            var page = Page();
            var mid = Mid(page.transform);
            Mascot(mid.transform, 110f, bob: true);

            UIKit.Space(mid.transform, 26f);
            UIKit.Text(mid.transform, "Want to add your things now?", TextStyle.Title, null, TextAlignmentOptions.Center);
            UIKit.Space(mid.transform, 10f);
            UIKit.Text(mid.transform, "Things you add get remembered: where they usually live, and where you last had them. Takes about a minute.",
                       TextStyle.Lead, null, TextAlignmentOptions.Center);
            UIKit.Space(mid.transform, 20f);
            var row = UIKit.Row(mid.transform, "Tiles", 10f, null, TextAnchor.MiddleCenter);
            foreach (var t in App.State.mine) Widgets.IconTile(row.transform, t.icon, UIKit.Swatch(t.color), 52f);

            var foot = Foot(page.transform);
            UIKit.Button(foot.transform, "Yes, add my things", ButtonKind.Primary, () => { App.Draft = new ThingDraft(); App.Router.Go("reg1"); });
            UIKit.Link(foot.transform, "Later", App.AfterOnboarding);
        }
    }
}
