using TMPro;
using UnityEngine;

namespace FindMe.UI
{
    /// <summary>
    /// Prototype "notfound" (after exiting a search): sad mascot, "Not found yet", how many spots were checked,
    /// then Search another room / I think it's truly lost / Back home.
    /// </summary>
    public class NotFoundScreen : UIScreen
    {
        TextMeshProUGUI lead;

        protected override void Build()
        {
            var page = Page();
            Widgets.TopBar(page.transform, "", null, p => Widgets.SquareButton(p, "x", () => App.Router.Go("find")));
            // Top-aligned like the prototype (.body with text-align center), not vertically centred.
            var mid = Body(page.transform, 0);
            mid.childAlignment = TextAnchor.UpperCenter;
            Mascot(mid.transform, 100f, sad: true);
            UIKit.Space(mid.transform, 18f);
            UIKit.Text(mid.transform, "Not found yet", TextStyle.Title, null, TextAlignmentOptions.Center);
            UIKit.Space(mid.transform, 10f);
            lead = UIKit.Text(mid.transform, "", TextStyle.Lead, null, TextAlignmentOptions.Center);
            UIKit.Space(mid.transform, 20f);

            var foot = Foot(page.transform);
            UIKit.Button(foot.transform, "Search another room", ButtonKind.Dark, App.BeginSearch);
            UIKit.Button(foot.transform, "I think it's truly lost", ButtonKind.Outline, App.ShowLostAfterExit);
            UIKit.Link(foot.transform, "Back home", () => App.Router.Go("find"));
        }

        public override void OnShow()
        {
            var v = App.Search;
            lead.text = v.total > 0
                ? $"That's okay. You checked {v.checkedCount} of {v.total} spots in this room."
                : "That's okay. Try another room, or come back to it later.";
        }

        // Prototype "lost": saved in History as truly lost; it can be marked found later.
        void MarkLost()
        {
            var t = App.Search.thing ?? App.State.mine[0];
            App.State.history.Insert(0, new HistoryEntry
            {
                name = t.name, icon = t.icon, color = t.color, oneTime = t.oneTime, when = "Just now", result = "Truly lost", lost = true,
            });
            App.Toast("Saved in History. Mark it found any time.");
            App.Router.Go("find");
        }
    }
}
