using UnityEngine;

namespace FindMe.UI
{
    /// <summary>Prototype "login": small logo, social logins, email + password.</summary>
    public class LoginScreen : UIScreen
    {
        protected override void Build()
        {
            var page = Page();
            Widgets.TopBar(page.transform, "", App.Router.Back);
            var body = Body(page.transform).transform;

            UIKit.Space(body, 4f);
            var logoRow = UIKit.Row(body, "LogoRow");          // keeps the 70 px logo at its own size, left aligned
            LogoMark.Create(logoRow.transform, 70f, pulse: false);
            Widgets.TitleLead(body, "Welcome back", "Log in to pick up where you left off.", 6f);
            Widgets.Social(body, "G", T.white, new Color32(0x42, 0x85, 0xF4, 255), "Continue with Google", App.AfterLogin);
            Widgets.Social(body, "+", T.lavenderLight, T.grape, "Other social login", App.AfterLogin);
            Widgets.OrDivider(body, "or with email");
            Widgets.Field(body, "Email", "you@example.com");
            Widgets.Field(body, "Password", "Password", "", password: true);

            var foot = Foot(page.transform);
            UIKit.Button(foot.transform, "Log in", ButtonKind.Dark, App.AfterLogin);
        }
    }
}
