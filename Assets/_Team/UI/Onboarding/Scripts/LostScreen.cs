using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FindMe.Onboarding
{
    /// <summary>
    /// "Can't find it": a kind sorry screen (sad mascot, "Losing things happens to everyone"), then
    /// "Get back on track": things to buy, picked from what was lost (a new one like it, a matching accessory that
    /// stops it happening again, and FIND ME Dots). "View" opens a partner store search with our affiliate tag.
    /// Back home ends the search; Keep looking goes back to it.
    /// </summary>
    public class LostScreen : UIScreen
    {
        /// <summary>Affiliate tag added to store links (placeholder: set the real one from the partner programme).</summary>
        public static string AffiliateTag = "findme-20";

        TextMeshProUGUI title;
        Transform products;
        Action onHome, onKeepLooking;

        protected override void Build()
        {
            var page = Page();
            var bar = UIKit.Row(page.transform, "Bar", 0f, UIKit.Pad(22, 16, 22, 6), TextAnchor.MiddleCenter);
            var h = UIKit.Text(bar.transform, "Let's handle it", TextStyle.H2, null, TextAlignmentOptions.Center);
            h.fontSize = 19f;
            UIKit.Size(h, -1, 48f, 1);

            var body = Body(page.transform, 0);
            body.childAlignment = TextAnchor.UpperCenter;
            Mascot(body.transform, 96f, sad: true);
            UIKit.Space(body.transform, 16f);
            title = UIKit.Text(body.transform, "", TextStyle.Title, null, TextAlignmentOptions.Center);
            title.fontSize = 28f;
            UIKit.Space(body.transform, 8f);
            UIKit.Text(body.transform, "Losing things happens to everyone. If it turns up later, you can still mark it found.",
                       TextStyle.Lead, null, TextAlignmentOptions.Center);
            UIKit.Space(body.transform, 22f);
            UIKit.Text(body.transform, "Get back on track", TextStyle.Label);
            UIKit.Space(body.transform, 10f);
            products = UIKit.Column(body.transform, "Products", 10f).transform;
            products.GetComponent<VerticalLayoutGroup>().childForceExpandWidth = true;

            var foot = Foot(page.transform);
            UIKit.Button(foot.transform, "Back home", ButtonKind.Dark, () => onHome?.Invoke());
            UIKit.Link(foot.transform, "Keep looking", () => onKeepLooking?.Invoke());
        }

        /// <summary>Fill in for this item. appearance/category come from Gemini's item profile (may be empty).</summary>
        public void Set(string item, string appearance, string category, Action home, Action keepLooking)
        {
            onHome = home;
            onKeepLooking = keepLooking;
            item = string.IsNullOrWhiteSpace(item) ? "item" : item.Trim().ToLowerInvariant();
            title.text = $"We couldn't find your {item}";
            UIKit.Clear(products);
            foreach (var p in Suggestions(item, appearance, category)) Product(p);
        }

        struct Suggestion { public string title, text, query, icon, color, url; public bool ours; }

        /// <summary>
        /// Three picks, based on what was lost: (1) a new one like it (using how it looked, e.g. "teal rectangular
        /// frames"), (2) the accessory that stops it getting lost again, (3) FIND ME Dots.
        /// </summary>
        static List<Suggestion> Suggestions(string item, string appearance, string category)
        {
            string key = ((category ?? "") + " " + item).ToLowerInvariant();
            string icon = key.Contains("key") ? "key" : key.Contains("wallet") || key.Contains("card") ? "wallet"
                        : key.Contains("glass") ? "glasses" : key.Contains("phone") ? "phone"
                        : key.Contains("pill") || key.Contains("medic") ? "pill" : key.Contains("bud") ? "buds" : "bag";
            string look = string.IsNullOrWhiteSpace(appearance) || appearance.StartsWith("MOCK") ? null : appearance.Trim();

            var list = new List<Suggestion>
            {
                new Suggestion { title = $"A new {item}", text = look != null ? $"Like yours: {look}" : "Similar to the one you lost",
                                 query = look != null ? $"{look} {item}" : item, icon = icon, color = "pink" },
            };
            (string t, string d, string q) acc =
                key.Contains("key") ? ("Key tracker tag", "Rings from your phone when keys go missing", "bluetooth key finder tag") :
                key.Contains("wallet") ? ("Slim wallet with tracker slot", "Holds a card-size tracker", "slim wallet tracker card") :
                key.Contains("glass") ? ("Glasses case with a strap", "Always in the same place", "glasses case with strap") :
                key.Contains("phone") ? ("Phone strap", "Hard to put down and forget", "phone wrist strap") :
                key.Contains("hat") || key.Contains("cap") || key.Contains("beanie") ? ("Hat clip for your bag", "Keeps it with you when it's off your head", "hat clip for bag") :
                key.Contains("bud") ? ("Earbuds case keychain", "Clips onto your keys", "earbuds case keychain") :
                key.Contains("pill") ? ("Weekly pill organiser", "One home for every dose", "weekly pill organizer") :
                ("Bluetooth tracker tag", "Attach it, then find it with your phone", "bluetooth tracker tag");
            list.Add(new Suggestion { title = acc.t, text = acc.d, query = acc.q, icon = "tag", color = "sun" });
            list.Add(new Suggestion { title = "FIND ME Dots", text = "Stickers your camera spots from across the room", icon = "check",
                                      color = "mint", url = "https://findme.app/dots", ours = true });
            return list;
        }

        static string StoreUrl(string query) =>
            $"https://www.amazon.com/s?k={Uri.EscapeDataString(query)}&tag={Uri.EscapeDataString(AffiliateTag)}";

        // White card: tile · title + description · "View" pill (FIND ME Dots: grape card, our own product).
        void Product(Suggestion s)
        {
            var row = UIKit.Row(products, "Product", 14f, UIKit.Pad(16, 16, 16, 16));
            if (s.ours) UIKit.Backdrop(row.transform, T.grape, 24f);
            else UIKit.Frame(row.transform, T.white, T.cloud, 24f);
            var tile = UIKit.Box(row.transform, "Tile", UIKit.Swatch(s.color), 15f);
            UIKit.Size(tile, 52f, 52f);
            var ic = UIKit.Icon(tile.transform, s.icon, 30f);
            UIKit.Place((RectTransform)ic.transform, new Vector2(0.5f, 0.5f), new Vector2(30f, 30f));
            var col = UIKit.Column(row.transform, "Text", 2f);
            UIKit.Size(col, 0f, -1, 1);
            UIKit.Text(col.transform, s.title, TextStyle.Body, s.ours ? T.white : (Color?)null).fontSize = 16.5f;
            var d = UIKit.Text(col.transform, s.text, TextStyle.Small, s.ours ? new Color32(0xD8, 0xD2, 0xEE, 255) : (Color?)null);
            d.fontSize = 13.5f;

            var pill = UIKit.Row(row.transform, "View", 0f, UIKit.Pad(14, 9, 14, 9), TextAnchor.MiddleCenter);
            var bg = UIKit.Backdrop(pill.transform, s.ours ? T.bubblegum : T.grape, 99f);
            var t = UIKit.Text(pill.transform, "View", TextStyle.Label, s.ours ? T.grape : T.white);
            t.fontSize = 14f;
            bg.raycastTarget = true;
            var b = pill.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            b.targetGraphic = bg;
            string url = s.url ?? StoreUrl(s.query);
            b.onClick.AddListener(() => Application.OpenURL(url));
        }
    }
}
