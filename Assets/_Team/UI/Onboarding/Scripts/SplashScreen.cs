using TMPro;
using UnityEngine;

namespace FindMe.Onboarding
{
    /// <summary>Prototype "splash" / design Main.png: grape background, pulsing logo, FIND ME!, Get started.</summary>
    public class SplashScreen : UIScreen
    {
        protected override bool UsesFullTop => true;

        protected override void Build()
        {
            Background.color = T.grape;
            var page = Page();

            var mid = Mid(page.transform);
            LogoMark.Create(mid.transform, 150f, pulse: true);
            UIKit.Space(mid.transform, 22f);
            UIKit.Text(mid.transform, "FIND ME!", TextStyle.Brand, T.white, TextAlignmentOptions.Center);
            UIKit.Space(mid.transform, 4f);
            UIKit.Text(mid.transform, "Lost it? Let's look together.", TextStyle.Label, T.lavender, TextAlignmentOptions.Center)
                 .fontSize = 17.5f;

            var foot = Foot(page.transform);
            UIKit.Button(foot.transform, "Get started", ButtonKind.Primary, () => App.Router.Go("welcome"));
        }
    }
}
