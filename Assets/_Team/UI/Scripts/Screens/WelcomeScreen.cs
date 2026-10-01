using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FindMe.UI
{
    /// <summary>
    /// Prototype "welcome": 3 onboarding slides (blob + title + text + dots). Slide 3 is the Care plan,
    /// a premium feature: a tilted lemon crown sits on the blob and a "Premium" tag sits under the title.
    /// </summary>
    public class WelcomeScreen : UIScreen
    {
        struct Slide { public Color color; public string icon, title, text; public bool premium; }

        Slide[] slides;
        int index;

        CanvasGroup midGroup;
        RectTransform midRt;
        Image blob, blobIcon;
        GameObject crown, premiumTag;
        RectTransform tagRt;
        float lastTitleWidth = -1f;
        TextMeshProUGUI title, lead;
        readonly List<Image> dots = new();

        protected override void Build()
        {
            // Copy is from the prototype; colors are Sherbet Pop (soda replaces the prototype's sky blue).
            slides = new[]
            {
                new Slide { color = T.bubblegum, icon = "camera", title = "Point. Scan. Found.",
                    text = "Your camera remembers every spot you already checked, so you never search the same place twice." },
                new Slide { color = T.mint, icon = "sun", title = "Stressed? We slow down with you.",
                    text = "If a quick search doesn't work, FIND ME switches to a calm mode to help you breathe and retrace your steps." },
                new Slide { color = T.soda, icon = "users", title = "Help the people you love.",
                    text = "Set up things together with a parent, and see when they need a hand.", premium = true },
            };

            var page = Page();

            // Top bar: "Log in" left, "Skip" right.
            var bar = UIKit.Row(page.transform, "Bar", 0f, UIKit.Pad(22, 16, 22, 6));
            UIKit.Link(bar.transform, "Log in", () => App.Router.Go("login"));
            UIKit.Space(bar.transform).flexibleWidth = 1;
            UIKit.Link(bar.transform, "Skip", () => App.Router.Go("signup"));

            var mid = Mid(page.transform);
            midRt = (RectTransform)mid.transform;
            midGroup = mid.gameObject.AddComponent<CanvasGroup>();

            BuildBlob(mid.transform);
            UIKit.Space(mid.transform, 30f);
            title = UIKit.Text(mid.transform, "", TextStyle.Title, null, TextAlignmentOptions.Center);
            premiumTag = BuildPremiumTag(title.transform);
            UIKit.Space(mid.transform, 10f);
            lead = UIKit.Text(mid.transform, "", TextStyle.Lead, null, TextAlignmentOptions.Center);
            UIKit.Space(mid.transform, 20f + 18f);           // .lead margin-bottom 20 + .dots margin-top 18

            var dotRow = UIKit.Row(mid.transform, "Dots", 6f, null, TextAnchor.MiddleCenter);
            for (int i = 0; i < slides.Length; i++) dots.Add(UIKit.Box(dotRow.transform, "Dot", T.cloud, 99f));

            var foot = Foot(page.transform);
            UIKit.Button(foot.transform, "Next", ButtonKind.Dark, Next);
        }

        /// <summary>The 210 px colored blob with its icon, and the crown that only shows on the premium slide.</summary>
        void BuildBlob(Transform parent)
        {
            // A fixed-size holder sits in the column; the blob and crown move freely inside it.
            var holder = UIKit.Rect(parent, "BlobHolder");
            UIKit.Size(holder, 210f, 210f);

            blob = UIKit.Circle(holder, "Blob", T.bubblegum, 210f);
            UIKit.Place((RectTransform)blob.transform, new Vector2(0.5f, 0.5f), new Vector2(210f, 210f));
            // The prototype blob slowly morphs (8 s). A gentle wide/tall stretch gives the same squishy feel.
            var wobble = blob.gameObject.AddComponent<LoopMotion>();
            wobble.period = 8f;
            wobble.stretch = new Vector2(0.04f, -0.04f);
            blobIcon = UIKit.Icon(blob.transform, "camera", 97f);   // 46 % of the blob, like the CSS

            // Crown: a darker lemon copy 3 px lower (solid edge), the lemon fill, then the grape outline.
            // 66 px sprite → a ~50 px wide crown whose base sits on the blob's top edge (measured from the HTML).
            const float S = 66f;
            var c = UIKit.Rect(holder, "Crown");
            UIKit.Place(c, new Vector2(0.5f, 0.5f), new Vector2(S, S), new Vector2(-6f, 101f));
            c.localEulerAngles = new Vector3(0, 0, 14f);            // tilted: +z leans it to the left, like rotate(-14deg)
            var edge = UIKit.Sprite(c, "Edge", T.crownFill, T.lemonEdge, S, S);
            UIKit.Place((RectTransform)edge.transform, new Vector2(0.5f, 0.5f), new Vector2(S, S), new Vector2(0f, -3f));
            UIKit.Stretch((RectTransform)UIKit.Sprite(c, "Fill", T.crownFill, T.lemon, S, S).transform);
            UIKit.Stretch((RectTransform)UIKit.Sprite(c, "Line", T.crownLine, Color.white, S, S).transform);
            // Rocks between 14° and 8° and lifts 4 px, every 3 s.
            var rock = c.gameObject.AddComponent<LoopMotion>();
            rock.period = 3f;
            rock.rotate = -6f;
            rock.move = new Vector2(0f, 4f);
            crown = c.gameObject;
        }

        /// <summary>
        /// Lemon pill (crown + "Premium") on a 3 px darker edge. It lives inside the title text, outside the layout,
        /// and PlaceTag() parks it right after the title's last word, like the CSS inline tag.
        /// </summary>
        GameObject BuildPremiumTag(Transform title)
        {
            var pill = UIKit.Row(title, "Premium", 5f, UIKit.Pad(11, 5, 11, 5), TextAnchor.MiddleCenter);
            pill.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            var fit = pill.gameObject.AddComponent<ContentSizeFitter>();    // sizes itself to its content
            fit.horizontalFit = fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            tagRt = (RectTransform)pill.transform;
            tagRt.anchorMin = tagRt.anchorMax = new Vector2(0.5f, 0.5f);    // same origin as TMP's character positions
            tagRt.pivot = new Vector2(0f, 0.5f);
            var edge = UIKit.Backdrop(pill.transform, T.lemonEdge, 99f);     // box-shadow 0 3px 0: 3 px below the face
            UIKit.Stretch((RectTransform)edge.transform, 0, 3, 0, -3);
            UIKit.Backdrop(pill.transform, T.lemon, 99f);
            UIKit.Sprite(pill.transform, "Crown", T.crownFill, T.grape, 15f, 15f);
            var t = UIKit.Text(pill.transform, "Premium", TextStyle.Label, T.grape);
            t.fontSize = 13f;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            return pill.gameObject;
        }

        /// <summary>After layout: put the Premium tag 8 px after the title's last character, vertically centred on that line.</summary>
        public override void AfterLayout()
        {
            if (!premiumTag.activeSelf) return;
            LayoutRebuilder.ForceRebuildLayoutImmediate(tagRt);
            title.ForceMeshUpdate();
            var info = title.textInfo;
            if (info.characterCount == 0) return;
            var ch = info.characterInfo[info.characterCount - 1];
            var line = info.lineInfo[ch.lineNumber];
            // CSS: margin-left 8, vertical-align middle (x-height centre, a bit under the line centre), translateY(-3px).
            tagRt.anchoredPosition = new Vector2(ch.topRight.x + 8f, (line.ascender + line.descender) * 0.5f - 4f);
            lastTitleWidth = title.rectTransform.rect.width;
        }

        void LateUpdate()
        {
            // The layout can change the title width after ShowSlide (first frame, screen size): re-place then.
            if (premiumTag.activeSelf && !Mathf.Approximately(lastTitleWidth, title.rectTransform.rect.width)) AfterLayout();
        }

        public override void OnShow()
        {
            index = 0;
            ShowSlide(false);
        }

        /// <summary>Jump to a slide without animation (capture tool / tests).</summary>
        public void SetSlide(int i)
        {
            index = Mathf.Clamp(i, 0, slides.Length - 1);
            ShowSlide(false);
        }

        float TagWidth()
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(tagRt);
            return LayoutUtility.GetPreferredWidth(tagRt);
        }

        void Next()
        {
            if (index < slides.Length - 1)
            {
                index++;
                ShowSlide(true);
            }
            else App.Router.Go("signup");
        }

        void ShowSlide(bool animate)
        {
            var s = slides[index];
            blob.color = s.color;
            blobIcon.sprite = T.Icon(s.icon);
            // A blank gap the width of the tag (+8 margin) keeps room for it at the end of the title, and makes the
            // title wrap the same way the HTML does when the tag doesn't fit.
            title.text = s.premium ? $"{s.title}<space={TagWidth() + 8f}>" : s.title;
            lastTitleWidth = -1f;
            lead.text = s.text;
            crown.SetActive(s.premium);
            premiumTag.SetActive(s.premium);
            for (int i = 0; i < dots.Count; i++)
            {
                // Current dot: a 26 px grape pill. Others: 9 px cloud dots.
                dots[i].color = i == index ? T.grape : T.cloud;
                UIKit.Size(dots[i], i == index ? 26f : 9f, 9f);
            }
            if (!animate) return;
            // Same entrance as a new screen: fade in from 8 px lower.
            StopAllCoroutines();
            midGroup.alpha = 0f;
            StartCoroutine(Tweens.Fade(midGroup, 1f, 0.26f));
            StartCoroutine(Tweens.SlideIn(midRt, new Vector2(0f, -8f), 0.26f));
        }
    }
}
