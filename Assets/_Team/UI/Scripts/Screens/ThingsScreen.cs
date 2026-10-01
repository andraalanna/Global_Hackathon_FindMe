using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FindMe.UI
{
    /// <summary>Prototype "things" tab: 2-column grid of registered things + a dashed "Add a thing" card.</summary>
    public class ThingsScreen : UIScreen
    {
        Transform list;

        protected override void Build()
        {
            var page = Page();
            Widgets.Header(page.transform, "", "Your things", p => Widgets.SquareButton(p, "plus", NewThing));
            var body = Body(page.transform).transform;
            var lead = UIKit.Text(body, "Registered things only. One-time searches live in History.", TextStyle.Lead);
            UIKit.Space(body, 14f);
            list = UIKit.Column(body, "Cards", 0f).transform;
            list.GetComponent<VerticalLayoutGroup>().childForceExpandWidth = true;
            TabBar.Create(page.transform, App, "things");
        }

        public override void OnShow()
        {
            UIKit.Clear(list);
            var cell = Widgets2.Grid2(list);
            foreach (var t in App.State.mine) Card(cell(), t);
            AddCard(cell());
            if (App.State.mine.Count % 2 == 0) UIKit.Size(UIKit.Rect(cell(), "Filler"), 0f, -1, 1);   // keep the last card half width
        }

        void NewThing()
        {
            App.Draft = new ThingDraft();
            App.Router.Go("reg1");
        }

        // .thingc: tile 56 · name (Fredoka 19) · "last place, time"
        void Card(Transform parent, Thing t)
        {
            var c = UIKit.Column(parent, "Thing " + t.name, 10f, UIKit.Pad(18, 18, 18, 18));
            UIKit.Size(c, 0f, -1, 1);
            var bg = UIKit.Frame(c.transform, T.white, T.cloud, 26f);
            var tileRow = UIKit.Row(c.transform, "TileRow");
            Widgets.IconTile(tileRow.transform, t.icon, UIKit.Swatch(t.color), 56f);
            var n = UIKit.Text(c.transform, t.name, TextStyle.H2);
            n.fontSize = 19f;
            n.textWrappingMode = TextWrappingModes.NoWrap;
            n.overflowMode = TextOverflowModes.Ellipsis;
            var s = UIKit.Text(c.transform, $"{t.last}, {t.lastTime.Replace("Today, ", "")}", TextStyle.Small);
            s.textWrappingMode = TextWrappingModes.NoWrap;
            s.overflowMode = TextOverflowModes.Ellipsis;
            Widgets2.Tap(c.gameObject, bg, () => { App.State.selected = t; App.Router.Go("item"); });
        }

        // Dashed "Add a thing" card (drawn with a solid cloud border: uGUI has no dashed outline).
        void AddCard(Transform parent)
        {
            var c = UIKit.Column(parent, "Add", 10f, UIKit.Pad(18, 18, 18, 18), TextAnchor.MiddleCenter);
            UIKit.Size(c, 0f, -1, 1);
            var bg = UIKit.Frame(c.transform, T.marshmallow, T.cloud, 26f);
            var tile = UIKit.Box(c.transform, "Tile", new Color32(0xF3, 0xF0, 0xFC, 255), 17f);
            UIKit.Size(tile, 56f, 56f);
            var ic = UIKit.Icon(tile.transform, "plus", 32f, new Color(1, 1, 1, 0.75f));
            UIKit.Place((RectTransform)ic.transform, new Vector2(0.5f, 0.5f), new Vector2(32f, 32f));
            var t = UIKit.Text(c.transform, "Add a thing", TextStyle.H2, T.muted, TextAlignmentOptions.Center);
            t.fontSize = 16f;
            Widgets2.Tap(c.gameObject, bg, NewThing);
        }
    }
}
