using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FindMe.UI
{
    /// <summary>More prototype pieces: pick tile, stat tile, progress bar, voice-note button, success burst, chevron rows.</summary>
    public static partial class Widgets2
    {
        static UITheme T => UIKit.T;

        /// <summary>.pick: tile + name in a white rounded box; selected = grape 4 px outline. Returns a setter.</summary>
        public static Action<bool> Pick(Transform parent, Thing t, Action onClick)
        {
            var col = UIKit.Column(parent, "Pick " + t.name, 8f, UIKit.Pad(6, 14, 6, 14), TextAnchor.UpperCenter);
            UIKit.Size(col, -1, -1, 1);
            var border = UIKit.Backdrop(col.transform, T.cloud, 22f);
            var fill = UIKit.Backdrop(col.transform, T.white, 20f, 0f, 2f);
            Widgets.IconTile(col.transform, t.icon, UIKit.Swatch(t.color), 46f);
            var n = UIKit.Text(col.transform, t.name, TextStyle.Label, null, TextAlignmentOptions.Center);
            n.fontSize = 13f;
            n.textWrappingMode = TextWrappingModes.NoWrap;
            Tap(col.gameObject, border, onClick);
            return on =>
            {
                // .pick.on: grape border + 2 px inset ring = 4 px of grape
                border.color = on ? T.grape : T.cloud;
                UIKit.Stretch((RectTransform)fill.transform, on ? 4 : 2, on ? 4 : 2, on ? 4 : 2, on ? 4 : 2);
            };
        }

        /// <summary>.stat: colored tile with a big Fredoka number/word and a label (Summary, Item detail, Family).</summary>
        public static VerticalLayoutGroup Stat(Transform parent, Color bg, string big, string label, bool labelFirst = false,
                                               string extra = null, float bigSize = 30f)
        {
            var col = UIKit.Column(parent, "Stat", labelFirst ? 6f : 2f, UIKit.Pad(14, 14, 14, 12));
            UIKit.Size(col, -1, -1, 1);
            UIKit.Backdrop(col.transform, bg, 22f);
            TextMeshProUGUI l = null;
            if (labelFirst) l = UIKit.Text(col.transform, label, TextStyle.Label);
            if (big == "✓")
            {
                // The fonts have no ✓ glyph: a row the height of the number line, holding the check icon.
                var line = UIKit.Row(col.transform, "CheckLine", 0f, UIKit.Pad(-4, 0, 0, 0));
                UIKit.Size(line, -1, bigSize * 1.15f);
                UIKit.Icon(line.transform, "check", bigSize * 1.15f);
            }
            else
            {
                var b = UIKit.Text(col.transform, big, TextStyle.H2);
                b.fontSize = bigSize;
                b.lineSpacing = labelFirst ? 0f : -24f;
            }
            if (!labelFirst) l = UIKit.Text(col.transform, label, TextStyle.Label);
            l.fontSize = 14f;
            if (extra != null)
            {
                var e = UIKit.Text(col.transform, extra, TextStyle.Body);
                e.fontSize = 13.5f;
                e.color = new Color(T.grape.r, T.grape.g, T.grape.b, 0.75f);
            }
            return col;
        }

        /// <summary>.prob: 12 px rounded track with a colored fill (0..1). Returns the fill to animate.</summary>
        public static RectTransform Bar(Transform parent, float value, Color fill, float height = 12f)
        {
            var track = UIKit.Box(parent, "Track", T.lavenderLight, 99f);
            UIKit.Size(track, -1, height);
            var f = UIKit.Box(track.transform, "Fill", fill, 99f);
            var rt = (RectTransform)f.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = new Vector2(Mathf.Clamp01(value), 1f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }

        /// <summary>.voice: mint card with a round grape mic button, a title and a hint. Returns (title, hint).</summary>
        public static (TextMeshProUGUI title, TextMeshProUGUI hint) Voice(Transform parent, string title, string hint, Action onClick)
        {
            var row = UIKit.Row(parent, "Voice", 16f, UIKit.Pad(18, 16, 18, 16));
            var bg = UIKit.Backdrop(row.transform, T.mint, 28f);
            var mic = UIKit.Circle(row.transform, "Mic", T.grape, 62f);
            var ic = UIKit.Icon(mic.transform, "mic_white", 26f);
            UIKit.Place((RectTransform)ic.transform, new Vector2(0.5f, 0.5f), new Vector2(26f, 26f));
            var col = UIKit.Column(row.transform, "Text", 2f);
            UIKit.Size(col, -1, -1, 1);
            var t = UIKit.Text(col.transform, title, TextStyle.H2);
            t.fontSize = 19f;
            var h = UIKit.Text(col.transform, hint, TextStyle.Body);
            h.fontSize = 14.5f;
            h.lineSpacing = 6f;
            Tap(row.gameObject, bg, onClick);
            return (t, h);
        }

        /// <summary>.burst .ok: 140 px mint circle on an 8 px edge with a big check, popping in (Saved / Found / All set).</summary>
        public static RectTransform Burst(Transform parent, MonoBehaviour host, bool pop = true)
        {
            var holder = UIKit.Rect(parent, "Burst");
            UIKit.Size(holder, 180f, 180f);
            var edge = UIKit.Circle(holder, "Edge", T.mintEdge, 140f);
            UIKit.Place((RectTransform)edge.transform, new Vector2(0.5f, 0.5f), new Vector2(140f, 140f), new Vector2(0f, -8f));
            var ok = UIKit.Circle(holder, "Ok", T.mint, 140f);
            UIKit.Place((RectTransform)ok.transform, new Vector2(0.5f, 0.5f), new Vector2(140f, 140f));
            var ic = UIKit.Icon(ok.transform, "check", 84f);     // the icon art has padding: 84 px ≈ the CSS 66 px stroke box
            UIKit.Place((RectTransform)ic.transform, new Vector2(0.5f, 0.5f), new Vector2(84f, 84f));
            if (pop && Application.isPlaying)
            {
                // Pop in with an overshoot (cubic-bezier(.3,1.6,.5,1) in the CSS), the edge with it.
                host.StartCoroutine(Tweens.Pop(ok.transform, 0.2f, 0.5f));
                host.StartCoroutine(Tweens.Pop(edge.transform, 0.2f, 0.5f));
            }
            return holder;
        }

        /// <summary>A list row: icon tile · text (· optional small value) · chevron.</summary>
        public static HorizontalLayoutGroup NavRow(VerticalLayoutGroup list, string icon, Color tile, string text,
                                                   Action onClick, string value = null, bool chevron = true)
        {
            var row = Widgets.ListRow(list, onClick);
            Widgets.IconTile(row.transform, icon, tile, 38f);
            Widgets.Grow(UIKit.Text(row.transform, text, TextStyle.Body));
            if (value != null) UIKit.Text(row.transform, value, TextStyle.Small);
            if (chevron) UIKit.Icon(row.transform, "chev", 20f, new Color(1f, 1f, 1f, 0.5f));
            return row;
        }

        /// <summary>Makes any element tappable (invisible hit area = its backdrop).</summary>
        public static void Tap(GameObject go, Graphic hit, Action onClick)
        {
            hit.raycastTarget = true;
            var b = go.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            b.targetGraphic = hit;
            b.onClick.AddListener(() => onClick?.Invoke());
        }

        /// <summary>Two equal columns (CSS grid 2 × 1fr) — returns a function that gives the next cell's parent.</summary>
        public static Func<Transform> Grid2(Transform parent, float gap = 12f)
        {
            var col = UIKit.Column(parent, "Grid", gap);
            col.childForceExpandWidth = true;
            HorizontalLayoutGroup row = null;
            int n = 0;
            return () =>
            {
                if (n++ % 2 == 0)
                {
                    row = UIKit.Row(col.transform, "GridRow", gap, null, TextAnchor.UpperLeft);
                    row.childForceExpandWidth = true;
                    row.childForceExpandHeight = true;
                }
                return row.transform;
            };
        }
    }
}
