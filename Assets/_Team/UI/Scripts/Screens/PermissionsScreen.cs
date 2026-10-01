using UnityEngine;
using UnityEngine.UI;

namespace FindMe.UI
{
    /// <summary>
    /// Prototype "perms": camera / notifications / location switches and the privacy note.
    /// Camera off → "Scanning needs your camera". (The real iOS camera prompt appears when AR starts.)
    /// </summary>
    public class PermissionsScreen : UIScreen
    {
        Switch camera;

        protected override void Build()
        {
            var page = Page();
            Widgets.TopBar(page.transform, "", App.Router.Back);
            var body = Body(page.transform).transform;
            Widgets.TitleLead(body, "Before we start", "FIND ME only asks for what it needs.");

            var card = Widgets.Card(body, 18, 0f);
            card.padding = UIKit.Pad(20, 6, 20, 6);              // .card padding 4 18, inside the 2 px border
            camera = Row(card, "camera", T.bubblegum, "Camera", "To scan rooms and spot your things", true, false);
            Row(card, "bell", T.lemon, "Notifications", "Evening check reminders", true, false);
            Row(card, "compass", T.soda, "Location", "Optional · remembers where you last had things", false, true);

            UIKit.Space(body, 16f);
            Widgets.Note(body, "shield", "<b>Your rooms stay private.</b> Room scans are processed on your phone, and you can delete your history any time.");

            var foot = Foot(page.transform);
            UIKit.Button(foot.transform, "Let's go", ButtonKind.Primary, () => App.Router.Go(camera.On ? "askreg" : "camoff"));
        }

        // .perm: icon tile · name + description · switch, with a divider below (except the last).
        Switch Row(VerticalLayoutGroup card, string icon, Color c, string name, string desc, bool on, bool last)
        {
            var row = UIKit.Row(card.transform, "Perm " + name, 14f, UIKit.Pad(0, 14, 0, 14));
            Widgets.IconTile(row.transform, icon, c, 46f);
            var col = UIKit.Column(row.transform, "Text", 2f);
            UIKit.Size(col, -1, -1, 1);
            UIKit.Text(col.transform, name, TextStyle.Label).fontSize = 16f;
            UIKit.Text(col.transform, desc, TextStyle.Small);
            var sw = Switch.Create(row.transform, on);
            if (!last) UIKit.Size(UIKit.Box(card.transform, "Divider", T.cloud), -1, 2f);
            return sw;
        }
    }
}
