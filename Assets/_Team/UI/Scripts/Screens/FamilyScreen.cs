using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FindMe.UI
{
    /// <summary>
    /// Prototype "family" tab, two faces:
    ///   free plan  → locked: lemon crown tile, "Care plan" tag, See plans
    ///   Care plan  → "Dad's home": members, today/this week stats, Dad's things, "Dad is searching now", talk about
    /// (Emoji in the prototype are replaced with icon tiles: the fonts have no emoji and the spec says no emoji icons.)
    /// </summary>
    public class FamilyScreen : UIScreen
    {
        GameObject locked, home;
        Transform statsSlot;
        bool week;

        protected override void Build()
        {
            locked = BuildLocked();
            home = BuildHome();
        }

        public override void OnShow()
        {
            locked.SetActive(!App.State.carePlan);
            home.SetActive(App.State.carePlan);
            if (App.State.carePlan) ShowPeriod(week);
        }

        // ---------- free plan ----------

        GameObject BuildLocked()
        {
            var page = Page();
            Widgets.Header(page.transform, "", "Family");
            var mid = Mid(page.transform, 30);
            CrownTile(mid.transform, 110f);
            UIKit.Space(mid.transform, 16f);
            CarePlanTag(mid.transform);
            UIKit.Space(mid.transform, 8f);
            var title = UIKit.Text(mid.transform, "Family is a Care plan feature", TextStyle.Title, null, TextAlignmentOptions.Center);
            title.fontSize = 30f;
            UIKit.Space(mid.transform, 10f);
            UIKit.Text(mid.transform, "With the Care plan, help a parent find things, set them up together, and get a weekly note about how they're doing.",
                       TextStyle.Lead, null, TextAlignmentOptions.Center);
            UIKit.Space(mid.transform, 20f);
            var btn = UIKit.Rect(mid.transform, "BtnHolder");
            UIKit.Size(btn, 330f, 58f);
            var b = UIKit.Button(btn, "See plans", ButtonKind.Primary, () => App.Router.Go("paywall"));
            UIKit.Stretch((RectTransform)b.transform);
            UIKit.Space(mid.transform, 6f);
            UIKit.Link(mid.transform, "Not now", () => App.Router.Go("find"));
            TabBar.Create(page.transform, App, "family");
            return page.gameObject;
        }

        // ---------- Care plan ----------

        GameObject BuildHome()
        {
            var page = Page();
            Widgets.Header(page.transform, "Family", "Dad's home", p => Widgets.Avatar(p, "J"));
            var body = Body(page.transform).transform;

            var members = Widgets.Chips(body, 8f);
            Widgets.Chip(members.transform, "Dad", true, c => c.Pick());
            Widgets.Chip(members.transform, "Mom", false, c => c.Pick());
            Widgets.Chip(members.transform, "+ Add", false, c => App.Toast("Invites come with the family backend"));
            UIKit.Space(body, 4f);
            var role = UIKit.Row(body, "Role", 6f);
            UIKit.Text(role.transform, "You: Caregiver", TextStyle.Small).font = T.bodyBlack;
            UIKit.Icon(role.transform, "chev", 16f, new Color(1, 1, 1, 0.5f));

            // Card: Today / This week + stats + timeline
            UIKit.Space(body, 14f);
            var card = Widgets.Card(body, 16, 0f);
            var period = Widgets.Chips(card.transform);
            Widgets.Chip(period.transform, "Today", true, c => { c.Pick(); ShowPeriod(false); });
            Widgets.Chip(period.transform, "This week", false, c => { c.Pick(); ShowPeriod(true); });
            UIKit.Space(card.transform, 14f);
            statsSlot = UIKit.Column(card.transform, "Stats", 0f).transform;
            statsSlot.GetComponent<VerticalLayoutGroup>().childForceExpandWidth = true;

            // Dad's things
            var sec = UIKit.Row(body, "Sec", 0f, UIKit.Pad(0, 24, 0, 12), TextAnchor.LowerLeft);
            Widgets.Grow(UIKit.Text(sec.transform, "Dad's things", TextStyle.H2));
            UIKit.Link(sec.transform, "+ Add together", () => App.Toast("Set things up together with Dad"), T.grape);
            var things = Widgets.HScroll(body, 166f);
            things.padding = UIKit.Pad(0, 2, 0, 6);          // already inside the body's 22 px padding
            DadThing(things.transform, "key", "pink", "Keys", "Desk, 23:10", "Found today", true);
            DadThing(things.transform, "glasses", "sky", "Glasses", "Radio, 08:40", "Voice note", false);
            DadThing(things.transform, "pill", "mint", "Pill box", "Kitchen, 09:00", "Not searched", false);

            // .helpnow: lemon banner
            UIKit.Space(body, 20f);
            var help = UIKit.Row(body, "HelpNow", 14f, UIKit.Pad(18, 18, 18, 18));
            UIKit.Backdrop(help.transform, T.lemon, 28f);
            UIKit.Icon(help.transform, "search", 40f);
            var hc = UIKit.Column(help.transform, "Text", 2f);
            UIKit.Size(hc, 0f, -1, 1);
            UIKit.Text(hc.transform, "Dad is searching now", TextStyle.H2).fontSize = 18f;
            UIKit.Text(hc.transform, "Looking for: glasses · 1 min", TextStyle.Body).fontSize = 14f;
            HistoryScreen.PillButton(help.transform, "Help", T.grape, T.white, () => App.Toast("Live help: send Dad a hint (next phase)"));

            // Talk about
            UIKit.Space(body, 24f);
            UIKit.Text(body, "Talk about", TextStyle.H2);
            UIKit.Space(body, 12f);
            Talk(body, "bell", T.lavender, "Ask Dad about his morning news routine", "From his glasses voice note");
            Talk(body, "key", T.bubblegum, "His keys ended up in the jacket twice", "Maybe a hook by the door?");

            TabBar.Create(page.transform, App, "family");
            return page.gameObject;
        }

        void ShowPeriod(bool isWeek)
        {
            week = isWeek;
            UIKit.Clear(statsSlot);
            var stats = UIKit.Row(statsSlot, "Tiles", 10f, null, TextAnchor.UpperLeft);
            stats.childForceExpandWidth = true;
            stats.childForceExpandHeight = true;
            string[] big = isWeek ? new[] { "3", "2", "5/7" } : new[] { "1", "1", "✓" };
            string[] label = isWeek ? new[] { "Searches", "Found", "Checks" } : new[] { "Search", "Found", "Pill box" };
            Color[] colors = { T.mint, T.soda, T.lemon };
            for (int i = 0; i < 3; i++)
            {
                var s = Widgets2.Stat(stats.transform, colors[i], big[i], label[i], false, null, 32f);
                s.padding = UIKit.Pad(14, 16, 14, 16);
                UIKit.Size(s, 0f, -1, 1);
            }
            UIKit.Space(statsSlot, 6f);
            var rows = isWeek
                ? new[] { ("Mon", "Found glasses · jacket"), ("Wed", "Found keys · desk"), ("Sat", "Pill box not found yet") }
                : new[] { ("08:10", "Found keys on the desk · 2 min"), ("09:00", "Pill box checked in the kitchen"), ("20:30", "Evening check not done yet") };
            for (int i = 0; i < rows.Length; i++)
            {
                if (i > 0) UIKit.Size(UIKit.Box(statsSlot, "Divider", T.cloud), -1, 2f);
                var r = UIKit.Row(statsSlot, "Wk", 10f, UIKit.Pad(0, 14, 0, 14));
                var when = UIKit.Text(r.transform, rows[i].Item1, TextStyle.Body);
                when.font = T.bodyBlack;
                when.fontSize = 16.5f;
                UIKit.Size(when, isWeek ? 44f : 52f);
                var what = Widgets.Grow(UIKit.Text(r.transform, rows[i].Item2, TextStyle.Body));
                what.fontSize = 16.5f;
            }
            if (!isWeek) return;
            UIKit.Space(statsSlot, 12f);
            var note = UIKit.Row(statsSlot, "Weekly", 14f, UIKit.Pad(18, 16, 18, 16));
            var bg = UIKit.Backdrop(note.transform, T.grape, 28f);
            var env = UIKit.Box(note.transform, "Env", T.bubblegum, 16f);
            UIKit.Size(env, 54f, 54f);
            var ic = UIKit.Icon(env.transform, "mail", 28f);
            UIKit.Place((RectTransform)ic.transform, new Vector2(0.5f, 0.5f), new Vector2(28f, 28f));
            var c = UIKit.Column(note.transform, "Text", 2f);
            UIKit.Size(c, 0f, -1, 1);
            UIKit.Text(c.transform, "Weekly note", TextStyle.H2, T.white).fontSize = 19f;
            UIKit.Text(c.transform, "1 new pattern to look at", TextStyle.Body, new Color32(0xD8, 0xD2, 0xEE, 255)).fontSize = 14.5f;
            UIKit.Icon(note.transform, "chev_white", 20f);
            Widgets2.Tap(note.gameObject, bg, () => App.Toast("Dad's weekly note (next phase)"));
        }

        // .thingc on the family page: 166 px card with tile, name, place/time and a status line.
        void DadThing(Transform parent, string icon, string color, string name, string where, string status, bool good)
        {
            var c = UIKit.Column(parent, "Thing " + name, 10f, UIKit.Pad(18, 18, 18, 18));
            UIKit.Size(c, 166f);
            UIKit.Frame(c.transform, T.white, T.cloud, 26f);
            var tr = UIKit.Row(c.transform, "TileRow");
            Widgets.IconTile(tr.transform, icon, UIKit.Swatch(color), 52f);
            var col = UIKit.Column(c.transform, "Text", 0f);
            UIKit.Text(col.transform, name, TextStyle.H2).fontSize = 19f;
            UIKit.Text(col.transform, where, TextStyle.Small);
            var line = UIKit.Row(c.transform, "Status", 4f);
            if (good) UIKit.Icon(line.transform, "check", 16f);
            var s = UIKit.Text(line.transform, status, TextStyle.Small, good ? T.mintEdge : (Color?)null);
            if (good) s.font = T.bodyBlack;
        }

        // .talk: white card with an icon tile and two lines.
        void Talk(Transform parent, string icon, Color tile, string title, string sub)
        {
            var row = UIKit.Row(parent, "Talk", 14f, UIKit.Pad(18, 18, 18, 18));
            UIKit.Frame(row.transform, T.white, T.cloud, 24f);
            Widgets.IconTile(row.transform, icon, tile, 44f);
            var c = UIKit.Column(row.transform, "Text", 2f);
            UIKit.Size(c, 0f, -1, 1);
            var t = UIKit.Text(c.transform, title, TextStyle.Body);
            t.lineSpacing = 4f;
            UIKit.Text(c.transform, sub, TextStyle.Small);
            UIKit.Space(parent, 10f);
        }

        /// <summary>.lockbig: lemon rounded square (radius 34) on a 7 px darker edge, with a grape crown.</summary>
        public static void CrownTile(Transform parent, float size)
        {
            var T = UIKit.T;
            var holder = UIKit.Rect(parent, "CrownTile");
            UIKit.Size(holder, size, size);
            float r = size * 34f / 110f;
            var edge = UIKit.Box(holder, "Edge", T.lemonEdge, r);
            UIKit.Stretch((RectTransform)edge.transform, 0, 7, 0, -7);
            UIKit.Stretch((RectTransform)UIKit.Box(holder, "Face", T.lemon, r).transform);
            float c = size * 0.47f * 256f / 194f;          // crown art fills 194 of its 256 px
            var crown = UIKit.Sprite(holder, "Crown", T.crownFill, T.grape, c, c);
            UIKit.Place((RectTransform)crown.transform, new Vector2(0.5f, 0.5f), new Vector2(c, c));
        }

        /// <summary>.crownb: lemon pill "♛ Care plan".</summary>
        public static void CarePlanTag(Transform parent)
        {
            var T = UIKit.T;
            var holder = UIKit.Row(parent, "TagHolder", 0f, null, TextAnchor.MiddleCenter);
            var row = UIKit.Row(holder.transform, "CarePlan", 6f, UIKit.Pad(12, 6, 12, 6), TextAnchor.MiddleCenter);
            UIKit.Backdrop(row.transform, T.lemon, 99f);
            UIKit.Sprite(row.transform, "Crown", T.crownFill, T.grape, 18f, 18f);
            var t = UIKit.Text(row.transform, "Care plan", TextStyle.Label, T.grape);
            t.fontSize = 13.5f;
            t.textWrappingMode = TextWrappingModes.NoWrap;
        }
    }
}
