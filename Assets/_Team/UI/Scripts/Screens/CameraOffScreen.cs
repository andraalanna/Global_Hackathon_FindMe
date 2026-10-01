using TMPro;

namespace FindMe.UI
{
    /// <summary>Prototype "camoff": sad mascot, why the camera matters, continue anyway.</summary>
    public class CameraOffScreen : UIScreen
    {
        protected override void Build()
        {
            var page = Page();
            Widgets.TopBar(page.transform, "", App.Router.Back);
            var mid = Mid(page.transform);
            LogoMark.Create(mid.transform, 110f, pulse: false, sad: true);
            UIKit.Space(mid.transform, 26f);
            UIKit.Text(mid.transform, "Scanning needs your camera", TextStyle.Title, null, TextAlignmentOptions.Center);
            UIKit.Space(mid.transform, 10f);
            UIKit.Text(mid.transform, "You can still save your things and use check-ins. To scan rooms later, allow camera access in your phone's settings for FIND ME.",
                       TextStyle.Lead, null, TextAlignmentOptions.Center);
            UIKit.Space(mid.transform, 20f);

            var foot = Foot(page.transform);
            UIKit.Button(foot.transform, "Continue", ButtonKind.Dark, () => App.Router.Go("askreg"));
        }
    }
}
