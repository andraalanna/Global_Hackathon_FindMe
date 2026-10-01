using TMPro;
using UnityEngine;

namespace FindMe.UI
{
    /// <summary>Prototype "reg1": photo box, name, type chips.</summary>
    public class RegisterPhotoScreen : UIScreen
    {
        TMP_InputField nameField;
        Chip[] types;
        static readonly (string icon, string label)[] Types =
            { ("key", "Keys"), ("glasses", "Glasses"), ("wallet", "Wallet"), ("phone", "Phone"), ("pill", "Medicine"), ("search", "Other") };

        protected override void Build()
        {
            var page = Page();
            Widgets.TopBar(page.transform, "", App.Router.Back);
            var body = Body(page.transform).transform;
            Widgets.TitleLead(body, "Add a thing", "Take a photo and give it a name, so FIND ME knows what to look for.");

            // .photo: 200 px lavender box with a camera, "Take a photo" and a hint.
            var photo = UIKit.Column(body, "Photo", 6f, UIKit.Pad(20, 20, 20, 20), TextAnchor.MiddleCenter);
            UIKit.Size(photo, -1, 200f);
            var bg = UIKit.Backdrop(photo.transform, T.lavender, 30f);
            UIKit.Icon(photo.transform, "camera", 56f);
            var b = UIKit.Text(photo.transform, "Take a photo", TextStyle.H2, null, TextAlignmentOptions.Center);
            b.fontSize = 21f;
            UIKit.Text(photo.transform, "So the app knows what it looks like", TextStyle.Small, T.grape, TextAlignmentOptions.Center);
            Widgets2.Tap(photo.gameObject, bg, () => App.Toast("The camera opens here in the real build"));

            UIKit.Space(body, 20f);
            nameField = Widgets.Field(body, "What is it?", "e.g. House keys");
            Widgets.SectionLabel(body, "Type", 6f);
            var chips = Widgets.Chips(body);
            types = new Chip[Types.Length];
            for (int i = 0; i < Types.Length; i++) types[i] = Widgets.Chip(chips.transform, Types[i].label, false, c => c.Pick());

            var foot = Foot(page.transform);
            UIKit.Button(foot.transform, "Next", ButtonKind.Dark, () =>
            {
                var d = App.Draft;
                d.name = nameField.text.Trim();
                for (int i = 0; i < types.Length; i++) if (types[i].On) d.icon = Types[i].icon;
                App.Router.Go("reg2");
            });
        }

        public override void OnShow()
        {
            nameField.SetTextWithoutNotify(App.Draft.name);
            foreach (var c in types) c.Set(false);
        }
    }
}
