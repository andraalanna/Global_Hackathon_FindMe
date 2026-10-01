using TMPro;
using UnityEngine;

namespace FindMe.UI
{
    /// <summary>Prototype "reg2": where it usually lives + optional voice note.</summary>
    public class RegisterSpotScreen : UIScreen
    {
        TMP_InputField spot;
        TextMeshProUGUI voiceTitle, voiceHint;

        protected override void Build()
        {
            var page = Page();
            Widgets.TopBar(page.transform, "", App.Router.Back);
            var body = Body(page.transform).transform;
            Widgets.TitleLead(body, "Where does it usually live?", "The place you'd look first. FIND ME will start there.");
            spot = Widgets.Input(body, "e.g. By the radio in the living room");
            Widgets.SectionLabel(body, "Add a voice note (optional)");
            (voiceTitle, voiceHint) = Widgets2.Voice(body, "Record a voice note", "Say why it lives there. It helps you remember later.", Record);

            var foot = Foot(page.transform);
            UIKit.Button(foot.transform, "Save this thing", ButtonKind.Primary, Save);
        }

        public override void OnShow()
        {
            spot.SetTextWithoutNotify(App.Draft.spot);
            voiceTitle.text = "Record a voice note";
            voiceHint.text = "Say why it lives there. It helps you remember later.";
        }

        // Prototype: "Listening…" for 1.4 s, then a saved sample note.
        void Record()
        {
            voiceTitle.text = "Listening…";
            StartCoroutine(Later(1.4f, () =>
            {
                App.Draft.voice = "\"I always put it there after I come home.\"";
                voiceTitle.text = "Voice note saved";
                voiceHint.text = App.Draft.voice;
            }));
        }

        System.Collections.IEnumerator Later(float s, System.Action a)
        {
            yield return new WaitForSecondsRealtime(s);
            a();
        }

        void Save()
        {
            var d = App.Draft;
            d.spot = spot.text.Trim();
            string[] colors = { "pink", "sky", "mint", "sun", "lav" };
            string place = d.spot.Length > 0 ? d.spot : "Not set";
            App.State.mine.Add(new Thing
            {
                id = "t" + Time.frameCount, name = d.name.Length > 0 ? d.name : "New thing", icon = d.icon,
                color = colors[App.State.mine.Count % 5], spot = place, last = d.spot.Length > 0 ? d.spot : "Not seen yet",
                lastTime = "Just added", voice = d.voice,
                top = { (d.spot.Length > 0 ? d.spot : "Its usual place", 60), ("Sofa cushions", 25), ("Bag", 15) },
            });
            App.Router.Go("reg3");
        }
    }
}
