using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FindMe.UI
{
    /// <summary>
    /// Bottom tabs (.tabs): History · Things · [Find] · Family · Settings. The middle Find button is a raised
    /// pink circle with a solid edge and a white ring. Family shows a small lemon crown while it is locked.
    /// </summary>
    public static class TabBar
    {
        public static RectTransform Create(Transform page, UIManager app, string active)
        {
            var T = UIKit.T;
            var bar = UIKit.Row(page, "TabBar", 0f, UIKit.Pad(6, 8, 6, 12), TextAnchor.LowerCenter);
            bar.childForceExpandWidth = true;
            // White background with a 2 px top line; it reaches 60 px below the safe area so it also fills
            // the strip behind the iPhone home indicator.
            var bg = UIKit.Box(bar.transform, "Background", T.white);
            UIKit.Stretch((RectTransform)bg.transform, 0, 0, 0, -60);
            bg.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            var line = UIKit.Box(bg.transform, "TopLine", T.cloud);
            var lrt = (RectTransform)line.transform;
            lrt.anchorMin = new Vector2(0, 1); lrt.anchorMax = Vector2.one;
            lrt.offsetMin = new Vector2(0, -2); lrt.offsetMax = Vector2.zero;
            bg.transform.SetAsFirstSibling();

            Tab(bar.transform, app, "history", "clock", "History", active);
            Tab(bar.transform, app, "things", "grid", "Things", active);
            Center(bar.transform, app, active == "find");
            var fam = Tab(bar.transform, app, "family", "users", "Family", active);
            // The crown dot hides itself once the Care plan is active (re-checked every time the tab bar shows).
            var badge = fam.gameObject.AddComponent<CarePlanBadge>();
            badge.app = app;
            badge.dot = LockDot(fam);
            Tab(bar.transform, app, "settings", "sliders", "Settings", active);
            return (RectTransform)bar.transform;
        }

        static RectTransform Tab(Transform bar, UIManager app, string id, string icon, string label, string active)
        {
            var T = UIKit.T;
            bool on = id == active;
            var col = UIKit.Column(bar, "Tab " + id, 4f, UIKit.Pad(0, 6, 0, 6), TextAnchor.LowerCenter);
            // Active tab: icon sits on a 52 × 32 lavender pill.
            var tb = UIKit.Box(col.transform, "Pill", on ? T.lavenderLight : Color.clear, 14f);
            UIKit.Size(tb, 52f, 32f);
            var ic = UIKit.Icon(tb.transform, icon, 25f, on ? Color.white : new Color(1f, 1f, 1f, 0.45f));
            UIKit.Place((RectTransform)ic.transform, new Vector2(0.5f, 0.5f), new Vector2(25f, 25f));
            var t = UIKit.Text(col.transform, label, TextStyle.Label, on ? T.grape : T.faint, TextAlignmentOptions.Center);
            t.fontSize = 12f;
            AddTap(col.gameObject, () => app.Router.Go(id));
            return (RectTransform)col.transform;
        }

        /// <summary>Raised 62 px pink circle (edge 5 px, white ring 6 px) that sticks out 32 px above the bar.</summary>
        static void Center(Transform bar, UIManager app, bool on)
        {
            var T = UIKit.T;
            var col = UIKit.Column(bar, "Tab find", 4f, UIKit.Pad(0, 6, 0, 6), TextAnchor.LowerCenter);
            var holder = UIKit.Rect(col.transform, "Holder");
            UIKit.Size(holder, 62f, 32f);                    // takes 32 px in the bar; the circle overflows upward
            var ring = UIKit.Circle(holder, "Ring", T.white, 74f);
            UIKit.Place((RectTransform)ring.transform, new Vector2(0.5f, 0f), new Vector2(74f, 74f), new Vector2(0f, 31f));
            var edge = UIKit.Circle(holder, "Edge", T.bubblegumEdge, 62f);
            UIKit.Place((RectTransform)edge.transform, new Vector2(0.5f, 0f), new Vector2(62f, 62f), new Vector2(0f, 32f));
            var face = UIKit.Circle(holder, "Face", T.bubblegum, 62f);
            UIKit.Place((RectTransform)face.transform, new Vector2(0.5f, 0f), new Vector2(62f, 62f), new Vector2(0f, 37f));
            var ic = UIKit.Icon(face.transform, "search", 30f);
            UIKit.Place((RectTransform)ic.transform, new Vector2(0.5f, 0.5f), new Vector2(30f, 30f));
            var t = UIKit.Text(col.transform, "Find", TextStyle.Label, on ? T.grape : T.faint, TextAlignmentOptions.Center);
            t.fontSize = 12f;
            AddTap(col.gameObject, () => app.Router.Go("find"));
        }

        static GameObject LockDot(RectTransform tab)
        {
            var T = UIKit.T;
            var dot = UIKit.Circle(tab, "Lock", T.lemon, 18f);
            dot.GetComponent<LayoutElement>().ignoreLayout = true;
            var rt = (RectTransform)dot.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-21f, -11f);
            var c = UIKit.Sprite(dot.transform, "Crown", T.crownFill, T.grape, 13f, 13f);
            UIKit.Place((RectTransform)c.transform, new Vector2(0.5f, 0.5f), new Vector2(13f, 13f));
            return dot.gameObject;
        }

        static void AddTap(GameObject go, System.Action onClick)
        {
            var hit = go.AddComponent<Image>();
            hit.color = Color.clear;
            var b = go.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            b.onClick.AddListener(() => onClick());
        }
    }
}
