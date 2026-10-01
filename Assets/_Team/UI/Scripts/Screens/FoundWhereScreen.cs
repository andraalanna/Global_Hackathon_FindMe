using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace FindMe.UI
{
    /// <summary>
    /// Prototype "foundWhere": the user found it without the app pointing at it. Pick a place (Gemini's likely
    /// places, or the marked spots) or type one, then Save → Found it!
    /// </summary>
    public class FoundWhereScreen : UIScreen
    {
        Transform tileSlot, chipsParent;
        TextMeshProUGUI title;
        TMP_InputField other;
        ChunkyButton save;
        string picked;

        protected override void Build()
        {
            var page = Page();
            Widgets.TopBar(page.transform, "", App.Router.Back);
            var body = Body(page.transform).transform;
            var head = UIKit.Row(body, "Head", 12f, UIKit.Pad(0, 0, 0, 6));
            tileSlot = UIKit.Rect(head.transform, "TileSlot");
            UIKit.Size(tileSlot.GetComponent<RectTransform>(), 56f, 56f);
            Widgets.Tag(head.transform, "Found", T.mintLight, T.mintEdge);
            UIKit.Space(body, 8f);
            title = UIKit.Text(body, "", TextStyle.Title);
            UIKit.Space(body, 10f);
            UIKit.Text(body, "We'll save it as the last-seen place.", TextStyle.Lead);
            UIKit.Space(body, 20f);
            chipsParent = Widgets.Chips(body).transform;
            UIKit.Space(body, 18f);
            other = Widgets.Field(body, "Somewhere else", "e.g. In my jacket pocket");
            other.onValueChanged.AddListener(v =>
            {
                foreach (var c in chipsParent.GetComponentsInChildren<Chip>()) c.Set(false);
                picked = v.Trim().Length > 0 ? UIManager.Cap(v.Trim()) : null;
                save.SetInteractable(picked != null);
            });

            var foot = Foot(page.transform);
            save = UIKit.Button(foot.transform, "Save", ButtonKind.Primary, Save);
        }

        public override void OnShow()
        {
            var t = App.Search.thing ?? App.State.mine[0];
            UIKit.Clear(tileSlot);
            var tile = Widgets.IconTile(tileSlot, t.icon, UIKit.Swatch(t.color), 56f);
            UIKit.Stretch((RectTransform)tile.transform);
            title.text = $"Where did you find your {t.name.ToLower()}?";

            UIKit.Clear(chipsParent);
            foreach (var place in Places(t))
            {
                string p = place;
                Widgets.Chip(chipsParent, p, false, c => { c.Pick(); picked = p; other.SetTextWithoutNotify(""); save.SetInteractable(true); });
            }
            other.SetTextWithoutNotify("");
            picked = null;
            save.SetInteractable(false);
        }

        /// <summary>
        /// A marked spot? The search ends ON that cue (backend Found: summary, checked spots, cue burst).
        /// Somewhere else (typed, or a likely place with no cue)? The UI records it and stops the search.
        /// </summary>
        void Save()
        {
            foreach (var label in App.Actions.SpotLabels())
                if (string.Equals(label, picked, System.StringComparison.OrdinalIgnoreCase) && App.Actions.MarkFoundAt(label)) return;
            App.FoundManually(picked);
        }

        /// <summary>The spots marked in this search first, then Gemini's likely places, else the thing's usual ones.</summary>
        List<string> Places(Thing t)
        {
            var list = new List<string>();
            foreach (var label in App.Actions.SpotLabels()) if (!list.Contains(UIManager.Cap(label))) list.Add(UIManager.Cap(label));
            var p = App.Search.profile;
            if (p?.likelyPlaces != null)
                foreach (var x in p.likelyPlaces) if (list.Count < 6 && !list.Contains(UIManager.Cap(x))) list.Add(UIManager.Cap(x));
            if (list.Count == 0) foreach (var (place, _) in t.top) list.Add(place);
            if (list.Count == 0) list.AddRange(new[] { "Coat hook", "Sofa cushions", "Side table", "Coffee table" });
            return list.GetRange(0, Mathf.Min(6, list.Count));
        }
    }
}
