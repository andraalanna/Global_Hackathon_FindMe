using UnityEngine;

namespace FindMe.UI
{
    /// <summary>Prototype "signup": social logins, or email + password.</summary>
    public class SignUpScreen : UIScreen
    {
        protected override void Build()
        {
            var page = Page();
            Widgets.TopBar(page.transform, "", App.Router.Back);
            var body = Body(page.transform).transform;
            Widgets.TitleLead(body, "Create your account", "Keep your things and your family in sync on every phone.");
            Widgets.Social(body, "G", T.white, new Color32(0x42, 0x85, 0xF4, 255), "Continue with Google", () => App.Router.Go("profile"));
            Widgets.Social(body, "+", T.lavenderLight, T.grape, "Other social login", () => App.Router.Go("profile"));
            Widgets.OrDivider(body, "or with email");
            Widgets.Field(body, "Email", "you@example.com");
            Widgets.Field(body, "Password", "At least 8 characters", "", password: true);

            var foot = Foot(page.transform);
            UIKit.Button(foot.transform, "Create account", ButtonKind.Primary, () => App.Router.Go("profile"));
            UIKit.Link(foot.transform, "I already have an account", () => App.Router.Go("login"));
        }
    }
}
