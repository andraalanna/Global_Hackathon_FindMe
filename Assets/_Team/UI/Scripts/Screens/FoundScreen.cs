using TMPro;
using UnityEngine;

namespace FindMe.UI
{
    /// <summary>
    /// Prototype "found": soft mint glow, the check pops in with confetti, "Found it!",
    /// "Keys: under the side table. Searched for 1:35.", Nice, done → summary.
    /// </summary>
    public class FoundScreen : UIScreen
    {
        protected override bool UsesFullTop => true;
        TextMeshProUGUI lead;
        RectTransform burst;
        Confetti confetti;

        protected override void Build()
        {
            // The CSS glow is a radial gradient (mint centre → background). Flat UI: stacked see-through mint circles.
            for (int i = 0; i < 3; i++)                         // three soft rings fake the fade-out
            {
                float size = 360f + i * 180f;
                var glow = UIKit.Circle(Root, "Glow", T.mintLight, size);
                glow.transform.SetSiblingIndex(0);
                var grt = (RectTransform)glow.transform;
                grt.anchorMin = grt.anchorMax = new Vector2(0.5f, 0.65f);
                grt.sizeDelta = new Vector2(size, size);
                glow.color = new Color(T.mintLight.r, T.mintLight.g, T.mintLight.b, 0.22f);
            }

            var page = Page();
            var mid = Mid(page.transform);
            burst = Widgets2.Burst(mid.transform, this, false);
            confetti = Confetti.Create(burst);
            UIKit.Space(mid.transform, 22f);
            var t = UIKit.Text(mid.transform, "Found it!", TextStyle.Title, null, TextAlignmentOptions.Center);
            t.fontSize = 46f;
            UIKit.Space(mid.transform, 10f);
            lead = UIKit.Text(mid.transform, "", TextStyle.Lead, null, TextAlignmentOptions.Center);
            UIKit.Space(mid.transform, 20f);

            var foot = Foot(page.transform);
            UIKit.Button(foot.transform, "Nice, done", ButtonKind.Dark, () => App.Router.Go("summary"));
        }

        public override void OnShow()
        {
            var v = App.Search;
            var name = v.thing?.name ?? "Your thing";
            lead.text = $"{name}: {(v.foundAt ?? "right here").ToLower()}.\nSearched for {v.ElapsedText}.";
            if (!Application.isPlaying) return;
            foreach (Transform b in burst) if (b.name != "Confetti") StartCoroutine(Tweens.Pop(b, 0.2f, 0.5f));
            confetti.Play();
        }
    }
}
