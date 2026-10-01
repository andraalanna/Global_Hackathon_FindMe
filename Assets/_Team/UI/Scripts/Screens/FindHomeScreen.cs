using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FindMe.UI
{
    /// <summary>
    /// Prototype "find" (home tab): greeting, the big pink FIND button with two ripple rings,
    /// "Last seen" cards that scroll sideways, and the tab bar.
    /// </summary>
    public class FindHomeScreen : UIScreen
    {
        TextMeshProUGUI greeting;
        HorizontalLayoutGroup lastSeen;

        protected override void Build()
        {
            var page = Page();
            var head = Widgets.Header(page.transform, "Good morning", "What's missing?", p => Widgets.Avatar(p, "J"));
            greeting = head.GetComponentInChildren<TextMeshProUGUI>();

            // .findwrap: the FIND button and hint, centered in the leftover height.
            var wrap = UIKit.Column(page.transform, "FindWrap", 16f, UIKit.Pad(22, 0, 22, 0), TextAnchor.MiddleCenter);
            UIKit.Size(wrap, -1, -1, 1, 1);
            FindButton(wrap.transform);
            var hint = UIKit.Text(wrap.transform, "Tap once to start a search", TextStyle.Label, T.muted, TextAlignmentOptions.Center);
            hint.fontSize = 16f;

            var seen = UIKit.Column(page.transform, "LastSeen", 8f, UIKit.Pad(0, 0, 0, 16));
            seen.childForceExpandWidth = true;
            var label = UIKit.Text(seen.transform, "Last seen", TextStyle.Small);
            label.font = T.bodyBlack;
            label.margin = new Vector4(22, 0, 22, 0);
            lastSeen = Widgets.HScroll(seen.transform, 80f);

            TabBar.Create(page.transform, App, "find");
        }

        public override void OnShow()
        {
            greeting.text = "Good morning, " + App.State.userName;
            UIKit.Clear(lastSeen.transform);
            foreach (var t in App.State.mine) Mini(lastSeen.transform, t);
        }

        /// <summary>230 px button: two ripple rings + 184 px pink core on a 9 px edge, search icon and "FIND".</summary>
        void FindButton(Transform parent)
        {
            var root = UIKit.Rect(parent, "FindButton");
            UIKit.Size(root, 230f, 230f);
            for (int i = 0; i < 2; i++)
            {
                var ring = UIKit.Sprite(root, "Ring", T.ring, T.bubblegum, 230f, 230f);
                UIKit.Stretch((RectTransform)ring.transform);
                ring.gameObject.AddComponent<Ripple>().delay = i * 1.3f;
            }
            var edge = UIKit.Circle(root, "Edge", T.bubblegumEdge, 184f);
            UIKit.Place((RectTransform)edge.transform, new Vector2(0.5f, 0.5f), new Vector2(184f, 184f), new Vector2(0f, -9f));
            var core = UIKit.Circle(root, "Core", T.bubblegum, 184f);
            var crt = (RectTransform)core.transform;
            UIKit.Place(crt, new Vector2(0.5f, 0.5f), new Vector2(184f, 184f));
            core.raycastTarget = true;

            var col = UIKit.Column(core.transform, "Label", 0f, UIKit.Pad(0, 0, 0, 10), TextAnchor.MiddleCenter);
            UIKit.Stretch((RectTransform)col.transform);
            UIKit.Icon(col.transform, "search", 64f);
            var t = UIKit.Text(col.transform, "FIND", TextStyle.H2, T.grape, TextAlignmentOptions.Center);
            t.fontSize = 26f;

            // Press: the core sinks 5 px onto its edge (like .findbtn:active).
            var b = core.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            b.onClick.AddListener(() => App.Router.Go("what"));
            var press = core.gameObject.AddComponent<PressSink>();
            press.depth = 5f;
        }

        /// <summary>.mini: 176 px card with item tile, name and last-seen place.</summary>
        void Mini(Transform parent, Thing t)
        {
            var row = UIKit.Row(parent, "Mini " + t.name, 12f, UIKit.Pad(14, 14, 14, 14));
            UIKit.Size(row, 176f, 72f);
            UIKit.Frame(row.transform, T.white, T.cloud, 22f);
            Widgets.IconTile(row.transform, t.icon, UIKit.Swatch(t.color), 44f);
            var col = UIKit.Column(row.transform, "Text", 0f, null, TextAnchor.MiddleLeft);
            UIKit.Size(col, -1, -1, 1);
            var n = UIKit.Text(col.transform, t.name, TextStyle.H2);
            n.fontSize = 17f;
            n.textWrappingMode = TextWrappingModes.NoWrap;
            n.overflowMode = TextOverflowModes.Ellipsis;
            var s = UIKit.Text(col.transform, t.last, TextStyle.Small);
            s.textWrappingMode = TextWrappingModes.NoWrap;
            s.overflowMode = TextOverflowModes.Ellipsis;
            var hit = row.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            var b = row.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            b.onClick.AddListener(() => { App.State.selected = t; App.Router.Go("item"); });
        }
    }
}
