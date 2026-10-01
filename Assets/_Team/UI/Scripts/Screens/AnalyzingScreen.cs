using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FindMe.UI
{
    /// <summary>
    /// Prototype "analyzing" (while Gemini builds the item profile): spinning orb around the item tile,
    /// "Looking into it…", three steps that tick off, then "Scan the room".
    /// Steps 1–2 tick on a timer; step 3 and the button wait for ShowItemCard (the real answer).
    /// </summary>
    public class AnalyzingScreen : UIScreen
    {
        protected override bool UsesFullTop => true;

        RectTransform tileSlot;
        TextMeshProUGUI lead;
        readonly List<(Image ring, Image fill, Image check, TextMeshProUGUI text)> steps = new();
        CanvasGroup buttonGroup;
        bool profileReady;

        protected override void Build()
        {
            var page = Page();
            var mid = Mid(page.transform);
            Orb(mid.transform);
            UIKit.Space(mid.transform, 26f);
            UIKit.Text(mid.transform, "Looking into it…", TextStyle.Title, null, TextAlignmentOptions.Center);
            UIKit.Space(mid.transform, 10f);
            lead = UIKit.Text(mid.transform, "", TextStyle.Lead, null, TextAlignmentOptions.Center);
            UIKit.Space(mid.transform, 20f);
            var list = UIKit.Column(mid.transform, "Steps", 12f);
            list.childForceExpandWidth = true;
            UIKit.Size(list, -1, -1, 1);                       // full width, left aligned (.steps width 100%)
            foreach (var s in new[] { "Recognizing the item", "Checking where you last had it", "Ranking the most likely spots" })
                steps.Add(Step(list.transform, s));

            var foot = Foot(page.transform);
            var b = UIKit.Button(foot.transform, "Scan the room", ButtonKind.Dark, () => App.Router.Go("mapping"));
            buttonGroup = b.GetComponent<CanvasGroup>();
        }

        /// <summary>.orb: two counter-rotating arcs (pink + lavender outside, mint inside) around a 96 px tile.</summary>
        void Orb(Transform parent)
        {
            var orb = UIKit.Rect(parent, "Orb");
            UIKit.Size(orb, 190f, 190f);
            var outer = UIKit.Rect(orb, "Outer");
            UIKit.Stretch(outer);
            Arc(outer, T.bubblegum, 0f);                 // border-top: pink
            Arc(outer, T.lavender, -90f);                // border-right: lavender
            outer.gameObject.AddComponent<Spin>().degreesPerSecond = -360f / 1.3f;   // clockwise, 1.3 s
            var inner = UIKit.Stretch(UIKit.Rect(orb, "Inner"), 20, 20, 20, 20);
            Arc(inner, T.mint, 0f);
            inner.gameObject.AddComponent<Spin>().degreesPerSecond = 360f / 2f;      // reverse, 2 s
            tileSlot = UIKit.Rect(orb, "TileSlot");
            UIKit.Place(tileSlot, new Vector2(0.5f, 0.5f), new Vector2(96f, 96f));
        }

        void Arc(RectTransform parent, Color c, float angle)
        {
            var a = UIKit.Sprite(parent, "Arc", T.arc, c, 10f, 10f);
            UIKit.Stretch((RectTransform)a.transform);
            a.transform.localEulerAngles = new Vector3(0, 0, angle);
        }

        // .steps li: 28 px ring (3 px) + text; done = mint dot with a check, text turns grape.
        (Image, Image, Image, TextMeshProUGUI) Step(Transform parent, string text)
        {
            var row = UIKit.Row(parent, "Step", 12f);
            var ring = UIKit.Circle(row.transform, "Ring", T.faint, 28f);
            var fill = UIKit.Circle(ring.transform, "Hole", T.marshmallow, 22f);
            UIKit.Place((RectTransform)fill.transform, new Vector2(0.5f, 0.5f), new Vector2(22f, 22f));
            var check = UIKit.Icon(ring.transform, "check", 16f);
            UIKit.Place((RectTransform)check.transform, new Vector2(0.5f, 0.5f), new Vector2(16f, 16f));
            var t = UIKit.Text(row.transform, text, TextStyle.Label);
            t.fontSize = 16f;
            return (ring, fill, check, t);
        }

        public override void OnShow()
        {
            var x = App.Search.thing ?? App.State.mine[0];
            UIKit.Clear(tileSlot);
            var tile = Widgets.IconTile(tileSlot, x.icon, UIKit.Swatch(x.color), 96f);
            UIKit.Place((RectTransform)tile.transform, new Vector2(0.5f, 0.5f), new Vector2(96f, 96f));
            lead.text = $"Matching your {x.name.ToLower()} with your habits.";
            profileReady = App.Search.profile != null;
            for (int i = 0; i < steps.Count; i++) SetStep(i, false);
            buttonGroup.alpha = 0f;
            buttonGroup.blocksRaycasts = false;
            if (Application.isPlaying) StartCoroutine(Tick());
            else { for (int i = 0; i < steps.Count; i++) SetStep(i, true); ShowButton(); }   // capture: finished state
        }

        // Prototype timing: a step every 0.65 s from 0.6 s. The last one waits for the real profile.
        IEnumerator Tick()
        {
            yield return new WaitForSecondsRealtime(0.6f);
            SetStep(0, true);
            yield return new WaitForSecondsRealtime(0.65f);
            SetStep(1, true);
            yield return new WaitForSecondsRealtime(0.65f);
            while (!profileReady) yield return null;
            SetStep(2, true);
            yield return new WaitForSecondsRealtime(0.5f);
            ShowButton();
        }

        /// <summary>ShowItemCard arrived: the AI knows what to look for.</summary>
        public void ProfileReady()
        {
            profileReady = true;
            var p = App.Search.profile;
            if (p != null && p.likelyPlaces != null && p.likelyPlaces.Count > 0)
                lead.text = "Likely in: " + string.Join(", ", p.likelyPlaces.GetRange(0, Mathf.Min(3, p.likelyPlaces.Count))).ToLower() + ".";
        }

        void ShowButton()
        {
            buttonGroup.blocksRaycasts = true;
            if (isActiveAndEnabled && Application.isPlaying) StartCoroutine(Tweens.Fade(buttonGroup, 1f, 0.25f));
            else buttonGroup.alpha = 1f;
        }

        void SetStep(int i, bool done)
        {
            var (ring, fill, check, text) = steps[i];
            ring.color = done ? T.mint : new Color32(0xBD, 0xB7, 0xCB, 255);
            fill.enabled = !done;
            check.enabled = done;
            text.color = done ? T.grape : (Color)new Color32(0xBD, 0xB7, 0xCB, 255);
        }
    }
}
