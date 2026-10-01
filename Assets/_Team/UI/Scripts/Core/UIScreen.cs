using UnityEngine;
using UnityEngine.UI;

namespace FindMe.UI
{
    /// <summary>
    /// Base for every screen. A screen is a full-screen panel: background color edge to edge, and a
    /// "Safe" child (inside the notch/home bar) where the content is built once, in Build().
    /// </summary>
    public abstract class UIScreen : MonoBehaviour
    {
        public string Id { get; private set; }
        protected UIManager App { get; private set; }
        protected UITheme T => UIKit.T;
        public RectTransform Root { get; private set; }
        public CanvasGroup Group { get; private set; }
        /// <summary>Content area inside the safe area.</summary>
        protected RectTransform Safe { get; private set; }
        protected Image Background { get; private set; }

        /// <summary>Makes the panel, adds the screen script, builds its content, and hides it.</summary>
        public static TScreen Create<TScreen>(UIManager app, Transform parent, string id) where TScreen : UIScreen
        {
            var rt = UIKit.Stretch(UIKit.Rect(parent, "Screen " + id));
            var s = rt.gameObject.AddComponent<TScreen>();
            s.Id = id;
            s.App = app;
            s.Root = rt;
            s.Group = rt.gameObject.AddComponent<CanvasGroup>();
            s.Background = rt.gameObject.AddComponent<Image>();
            s.Background.color = UIKit.T.marshmallow;                 // default app background
            s.Safe = UIKit.Rect(rt, "Safe");
            s.Safe.gameObject.AddComponent<SafeArea>().applyTop = !s.UsesFullTop;
            s.Build();
            rt.gameObject.SetActive(false);
            return s;
        }

        /// <summary>
        /// True for screens with only centered content (no top bar): the prototype centers them in the full
        /// screen height, not below the notch, so the safe area's top inset is skipped.
        /// </summary>
        protected virtual bool UsesFullTop => false;

        /// <summary>Create the screen's content under Safe. Runs once.</summary>
        protected abstract void Build();

        /// <summary>Every time the screen appears (refresh data, restart animations).</summary>
        public virtual void OnShow() { }

        /// <summary>For things placed by hand once the layout has sized everything (e.g. a tag after a title's last word).</summary>
        public virtual void AfterLayout() { }

        // ---------- shared layout (prototype .scr = top bar · body · foot) ----------

        /// <summary>Full-height column filling the safe area: add a top bar, a flexible body and a foot to it.</summary>
        protected VerticalLayoutGroup Page()
        {
            var col = UIKit.Column(Safe, "Page");
            UIKit.Stretch((RectTransform)col.transform);
            col.childForceExpandWidth = true;
            return col;
        }

        /// <summary>Bottom action area (.foot): side padding 22, gap 10, a little room above the home bar.</summary>
        protected VerticalLayoutGroup Foot(Transform page)
        {
            var foot = UIKit.Column(page, "Foot", 10f, UIKit.Pad(22, 12, 22, 12));
            foot.childForceExpandWidth = true;
            return foot;
        }

        /// <summary>
        /// Scrolling content area (.body: padding 4 22 26) that takes the leftover height.
        /// Add the screen's content to the returned column; it scrolls when it is taller than the space.
        /// </summary>
        protected VerticalLayoutGroup Body(Transform page, int top = 4)
        {
            var view = UIKit.Rect(page, "Body");
            UIKit.Size(view, -1, -1, 1, 1);
            view.gameObject.AddComponent<RectMask2D>();                 // clip what scrolls out of view
            var hit = view.gameObject.AddComponent<Image>();
            hit.color = Color.clear;                                    // lets a drag anywhere start a scroll

            var content = UIKit.Column(view, "Content", 0f, UIKit.Pad(22, top, 22, 26));
            content.childForceExpandWidth = true;
            var crt = (RectTransform)content.transform;
            crt.anchorMin = new Vector2(0f, 1f);                        // full width, grows downward from the top
            crt.anchorMax = Vector2.one;
            crt.pivot = new Vector2(0.5f, 1f);
            crt.offsetMin = crt.offsetMax = Vector2.zero;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = view.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.viewport = view;
            scroll.content = crt;
            scroll.scrollSensitivity = 30f;
            return content;
        }

        /// <summary>
        /// The mascot (the logo tile) in a fixed slot, so it can bob (.bob: up 10 px and tilt 3°, every 3 s)
        /// without the layout fighting the animation.
        /// </summary>
        protected RectTransform Mascot(Transform parent, float size, bool bob = false, bool sad = false)
        {
            var holder = UIKit.Rect(parent, "MascotHolder");
            UIKit.Size(holder, size, size);
            var logo = LogoMark.Create(holder, size, pulse: false, sad: sad);
            UIKit.Place(logo, new Vector2(0.5f, 0.5f), new Vector2(size, size));
            if (bob)
            {
                var loop = logo.gameObject.AddComponent<LoopMotion>();
                loop.period = 3f;
                loop.move = new Vector2(0f, 10f);
                loop.rotate = 3f;
            }
            return holder;
        }

        /// <summary>Middle area that takes all the leftover height and centers its content (.mid).</summary>
        protected VerticalLayoutGroup Mid(Transform page, int sidePad = 28)
        {
            var mid = UIKit.Column(page, "Mid", 0f, UIKit.Pad(sidePad, 0, sidePad, 0), TextAnchor.MiddleCenter);
            UIKit.Size(mid, -1, -1, 1, 1);
            return mid;
        }
    }
}
