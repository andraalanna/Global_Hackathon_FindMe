using TMPro;
using UnityEngine;

namespace FindMe.UI
{
    /// <summary>Prototype "settings" tab: Care plan banner, Reminders, Privacy, Account.</summary>
    public class SettingsScreen : UIScreen
    {
        TextMeshProUGUI planTitle, planText;

        protected override void Build()
        {
            var page = Page();
            Widgets.Header(page.transform, "", "Settings");
            var body = Body(page.transform).transform;

            // .weekly banner: lemon crown tile · Care plan · chevron, on grape.
            var plan = UIKit.Row(body, "CarePlan", 14f, UIKit.Pad(18, 16, 18, 16));
            var bg = UIKit.Backdrop(plan.transform, T.grape, 28f);
            var env = UIKit.Box(plan.transform, "Env", T.lemon, 16f);
            UIKit.Size(env, 54f, 54f);
            var cr = UIKit.Sprite(env.transform, "Crown", T.crownFill, T.grape, 37f, 37f);
            UIKit.Place((RectTransform)cr.transform, new Vector2(0.5f, 0.5f), new Vector2(37f, 37f));
            var col = UIKit.Column(plan.transform, "Text", 2f);
            UIKit.Size(col, 0f, -1, 1);
            planTitle = UIKit.Text(col.transform, "Care plan", TextStyle.H2, T.white);
            planTitle.fontSize = 19f;
            planText = UIKit.Text(col.transform, "Unlock Family and help a parent", TextStyle.Body, new Color32(0xD8, 0xD2, 0xEE, 255));
            planText.fontSize = 14.5f;
            UIKit.Icon(plan.transform, "chev_white", 20f);
            Widgets2.Tap(plan.gameObject, bg, () => App.Router.Go(App.State.carePlan ? "family" : "paywall"));

            Widgets.SectionLabel(body, "Reminders", 18f);
            var rem = Widgets.ListBox(body);
            Widgets2.NavRow(rem, "moon", T.lavender, "Evening check", () => App.Toast("Evening check: 20:30, Mon–Fri"), "20:30");
            Widgets2.NavRow(rem, "grid", T.soda, "Lock screen widgets", () => App.Toast("Add FIND ME from your lock screen editor"));
            var notif = Widgets2.NavRow(rem, "bell", T.lemon, "Notifications", null, null, false);
            Switch.Create(notif.transform, true);

            Widgets.SectionLabel(body, "Privacy");
            var priv = Widgets.ListBox(body);
            var dev = Widgets.ListRow(priv);
            Widgets.IconTile(dev.transform, "shield", T.mint, 38f);
            var dc = UIKit.Column(dev.transform, "Text", 2f);
            UIKit.Size(dc, 0f, -1, 1);
            UIKit.Text(dc.transform, "Process scans on device", TextStyle.Body);
            UIKit.Text(dc.transform, "Room video never leaves your phone", TextStyle.Small);
            Switch.Create(dev.transform, true);
            var hist = Widgets2.NavRow(priv, "clock", T.bubblegum, "Save search history", null, null, false);
            Switch.Create(hist.transform, true);
            var del = Widgets.ListRow(priv, () => App.Toast("You'll be asked to confirm first"));
            Widgets.IconTile(del.transform, "x", T.bubblegumLight, 38f);
            Widgets.Grow(UIKit.Text(del.transform, "Delete all history", TextStyle.Body, T.bubblegumEdge));

            Widgets.SectionLabel(body, "Account");
            var acc = Widgets.ListBox(body);
            Widgets2.NavRow(acc, "user", T.lavender, "Profile", () => App.Router.Go("profile"));
            Widgets2.NavRow(acc, "door", T.soda, "Sign out", () => App.Router.Go("splash"), null, false);

            TabBar.Create(page.transform, App, "settings");
        }

        public override void OnShow()
        {
            planTitle.text = App.State.carePlan ? "Care plan active" : "Care plan";
            planText.text = App.State.carePlan ? "Family tab unlocked" : "Unlock Family and help a parent";
        }
    }
}
