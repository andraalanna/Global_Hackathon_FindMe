namespace FindMe.UI
{
    /// <summary>Prototype "profile": name + what you lose most often (multi-select chips).</summary>
    public class ProfileScreen : UIScreen
    {
        protected override void Build()
        {
            var page = Page();
            Widgets.TopBar(page.transform, "", App.Router.Back);
            var body = Body(page.transform).transform;
            Widgets.TitleLead(body, "A little about you", "This helps FIND ME suggest the right spots first.");
            var name = Widgets.Field(body, "What should we call you?", "", App.State.userName);
            name.onEndEdit.AddListener(v => { if (!string.IsNullOrWhiteSpace(v)) App.State.userName = v.Trim(); });

            Widgets.SectionLabel(body, "What do you lose most often?", 6f);
            var chips = Widgets.Chips(body);
            string[] things = { "Keys", "Glasses", "Wallet", "Phone", "Medicine", "Earbuds" };
            for (int i = 0; i < things.Length; i++) Widgets.Chip(chips.transform, things[i], i < 2, c => c.Toggle());

            var foot = Foot(page.transform);
            UIKit.Button(foot.transform, "Continue", ButtonKind.Dark, () => App.Router.Go("perms"));
        }
    }
}
