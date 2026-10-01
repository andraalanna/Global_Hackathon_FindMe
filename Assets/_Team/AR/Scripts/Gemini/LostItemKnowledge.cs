using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;

namespace FindMe
{
    /// <summary>One commonly lost item (see Assets/_Team/AI/Prompts/Resources/FindMePrompts/lost_items.json).</summary>
    [Serializable]
    public class LostItemInfo
    {
        public string id;
        public string[] names;       // words a user might type
        public float[] sizeCm;       // typical [width, height]
        public string shape;
        public string[] typical;     // where it usually ends up, most likely first
        public string[] fitsIn;      // places it physically fits
        public string[] never;       // places that make no sense for it
        public string lookalike;     // what counts as "this is the item" (and what doesn't)
        public string inCase;        // its own case, e.g. "glasses case" (must be OPENED to count as checked)

        public float MaxSizeCm => sizeCm is { Length: >= 2 } ? Mathf.Max(sizeCm[0], sizeCm[1]) : 10f;
    }

    /// <summary>
    /// The FIND ME knowledge base: commonly lost items, where they end up, what they fit in, and which "places"
    /// make no sense. It feeds the prompts AND filters Gemini's answers on the device, so a slip like
    /// "chapstick" as a hiding place for glasses never reaches the user.
    /// </summary>
    public class LostItemKnowledge
    {
        public List<LostItemInfo> items = new();
        public List<string> neverPlaces = new();

        static LostItemKnowledge cached;

        /// <summary>Loads lost_items.json once (Resources), or an empty base if it's missing.</summary>
        public static LostItemKnowledge Load()
        {
            if (cached != null) return cached;
            var asset = Resources.Load<TextAsset>("FindMePrompts/lost_items");
            cached = asset != null ? JsonConvert.DeserializeObject<LostItemKnowledge>(asset.text) : new LostItemKnowledge();
            if (asset == null) Debug.LogWarning("[Knowledge] FindMePrompts/lost_items.json not found; running without it.");
            return cached;
        }

        public string CategoryList()
        {
            var sb = new StringBuilder();
            foreach (var i in items) sb.Append(sb.Length > 0 ? ", " : "").Append(i.id);
            return sb.ToString();
        }

        /// <summary>
        /// Which known item the user means: the category Gemini chose if valid, else the longest known name found
        /// as whole words in their text ("my pink car key" → car_key, not keys). Null if unknown.
        /// </summary>
        public LostItemInfo Match(string userText, string category = null)
        {
            if (!string.IsNullOrEmpty(category))
                foreach (var i in items) if (i.id == category) return i;

            LostItemInfo best = null;
            int bestLen = 0;
            foreach (var i in items)
                foreach (var n in i.names)
                    if (n.Length > bestLen && ContainsWords(userText, n)) { best = i; bestLen = n.Length; }
            return best;
        }

        /// <summary>
        /// Can this "place" never hold the item? True for food/packaging/trash/etc. (unless the item really lives
        /// there, e.g. a charger at a socket), for places listed as nonsense for this item, and for another small
        /// object that isn't at least twice the item's size (glasses can't hide keys; a laptop can). Containers pass.
        /// </summary>
        public bool IsImpossiblePlace(string label, LostItemInfo item, float itemMaxSizeCm)
        {
            if (string.IsNullOrWhiteSpace(label)) return true;
            if (item != null && (ListHas(item.typical, label) || ListHas(item.fitsIn, label))) return false;

            foreach (var w in neverPlaces) if (ContainsWords(label, w)) return true;
            if (item?.never != null) foreach (var w in item.never) if (ContainsWords(label, w)) return true;

            if (!IsContainer(label))
                foreach (var other in items)
                {
                    if (other == item || other.MaxSizeCm >= itemMaxSizeCm * 2f) continue;
                    foreach (var n in other.names) if (ContainsWords(label, n)) return true;   // another small object, not a place
                }
            return false;
        }

        static readonly string[] ContainerWords =
            { "case", "bag", "backpack", "box", "pouch", "pocket", "drawer", "basket", "tray", "dish", "bowl", "jar",
              "cabinet", "wallet", "purse", "folder", "bin", "hamper", "closet", "wardrobe", "shoe", "boot" };

        /// <summary>Is this label the item itself ("black shoe" while looking for a shoe), not a place? Its case is a place.</summary>
        public static bool IsTheItem(string label, string itemName) =>
            ContainsWords(label ?? "", itemName ?? "~") && (!IsContainer(label) || IsContainer(itemName ?? ""));   // a shoe is a container AND can be the item

        static bool IsContainer(string label)
        {
            foreach (var w in ContainerWords) if (ContainsWords(label, w)) return true;
            return false;
        }

        /// <summary>
        /// The label is (part of) a known good place, e.g. "socket" in "wall socket near bed" for a charger.
        /// One direction only: "snack bag" must NOT pass just because glasses fit in a "bag".
        /// </summary>
        static bool ListHas(string[] list, string label)
        {
            if (list == null) return false;
            foreach (var p in list)
                if (ContainsWords(p, label)) return true;
            return false;
        }

        /// <summary>Whole-word, case-insensitive: "glasses case" contains "case" but "suitcase" doesn't; plural -s allowed.</summary>
        public static bool ContainsWords(string text, string phrase)
        {
            if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(phrase)) return false;
            text = " " + Normalize(text) + " ";
            phrase = Normalize(phrase);
            return text.Contains(" " + phrase + " ") || text.Contains(" " + phrase + "s ") || text.Contains(" " + phrase + "es ");
        }

        static string Normalize(string s)
        {
            var sb = new StringBuilder(s.Length);
            foreach (char ch in s.ToLowerInvariant()) sb.Append(char.IsLetterOrDigit(ch) ? ch : ' ');
            return string.Join(" ", sb.ToString().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
        }

        // ---------- prompt blocks ----------

        /// <summary>Item-specific guidance for the "find spots" prompt (empty for unknown items).</summary>
        public static string SpotsBlock(string itemName, LostItemInfo i)
        {
            if (i == null) return "";
            var sb = new StringBuilder();
            sb.Append($"What we know about a {itemName}:\n");
            sb.Append($"- It usually ends up: {string.Join(", ", i.typical)}.\n");
            sb.Append($"- It fits in: {string.Join(", ", i.fitsIn)}.\n");
            if (i.never is { Length: > 0 }) sb.Append($"- Never a place for it: {string.Join(", ", i.never)}.\n");
            if (!string.IsNullOrEmpty(i.inCase))
                sb.Append($"- A {itemName} is often kept in its {i.inCase}. If you see a {i.inCase}, list it as a place " +
                          $"(likelihood 3, hint \"Open the case\"). The {i.inCase} itself is not the {itemName}.\n");
            sb.Append("Places where it usually ends up get likelihood 3; other places that fit get 1-2.\n");
            return sb.ToString();
        }

        /// <summary>Extra rule for the "check" prompt when the spot is the item's own case.</summary>
        public static string CheckBlock(string spotLabel, LostItemInfo i)
        {
            if (i == null || string.IsNullOrEmpty(i.inCase) || !ContainsWords(spotLabel, "case")) return "";
            return $"- This spot is a {i.inCase}. It is only checked when it is OPEN and you can see inside it. " +
                   "A closed case means closed = true and insideVisible = false.\n";
        }
    }
}
