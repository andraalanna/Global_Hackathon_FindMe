using TMPro;
using UnityEngine;

namespace FindMe.UI
{
    /// <summary>Prototype "reg3": check burst, "Saved to Your things", the new thing's card, Add another / Done.</summary>
    public class RegisterDoneScreen : UIScreen
    {
        Transform cardSlot;
        RectTransform burst;
        protected override bool UsesFullTop => true;

        protected override void Build()
        {
            var page = Page();
            var mid = Mid(page.transform, 22);
            mid.childForceExpandWidth = true;
            var burstRow = UIKit.Row(mid.transform, "BurstRow", 0f, null, TextAnchor.MiddleCenter);
            burst = Widgets2.Burst(burstRow.transform, this, false);
            UIKit.Space(mid.transform, 22f);
            UIKit.Text(mid.transform, "Saved to Your things", TextStyle.Title, null, TextAlignmentOptions.Center);
            UIKit.Space(mid.transform, 10f);
            cardSlot = UIKit.Column(mid.transform, "CardSlot").transform;
            cardSlot.GetComponent<UnityEngine.UI.VerticalLayoutGroup>().childForceExpandWidth = true;

            var foot = Foot(page.transform);
            UIKit.Button(foot.transform, "Add another", ButtonKind.Outline, () => { App.Draft = new ThingDraft(); App.Router.Go("reg1"); });
            UIKit.Button(foot.transform, "Done", ButtonKind.Dark, App.AfterOnboarding);
        }

        public override void OnShow()
        {
            UIKit.Clear(cardSlot);
            var x = App.State.mine[App.State.mine.Count - 1];
            var card = Widgets.Card(cardSlot, 18, 0f);
            var row = UIKit.Row(card.transform, "Row", 12f);
            Widgets.IconTile(row.transform, x.icon, UIKit.Swatch(x.color), 52f);
            var col = UIKit.Column(row.transform, "Text", 2f);
            UIKit.Size(col, 0f, -1, 1);
            UIKit.Text(col.transform, x.name, TextStyle.Body).fontSize = 17f;
            UIKit.Text(col.transform, "Usually: " + x.spot, TextStyle.Small);
            // Pop the check in every time the screen opens.
            if (Application.isPlaying) foreach (Transform b in burst) StartCoroutine(Tweens.Pop(b, 0.2f, 0.5f));
        }
    }
}
