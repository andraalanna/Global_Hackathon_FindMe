using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace FindMe
{
    /// <summary>
    /// Real Gemini REST client (plan Section 6). One generic <see cref="Generate"/> does the HTTP;
    /// the three public calls only build a prompt + JSON schema and parse the answer.
    /// </summary>
    public class GeminiClient : IGeminiService
    {
        readonly string apiKey;
        readonly GeminiConfig cfg;

        public GeminiClient(string apiKey, GeminiConfig cfg)
        {
            this.apiKey = apiKey;
            this.cfg = cfg;
        }

        // Prompts are text files (Assets/_Team/AI/Prompts/Resources/FindMePrompts/*.txt) so they can be tuned
        // without code, and the eval script (Docs/ai/eval) tests exactly what the app sends.
        LostItemKnowledge Knowledge => LostItemKnowledge.Load();

        // ---------- Call 1: item profile (text only) ----------
        public async Task<ItemProfile> GetItemProfile(string userText, CancellationToken ct)
        {
            string prompt = PromptLibrary.Fill("profile",
                ("userText", userText),
                ("categories", Knowledge.CategoryList()));
            var schema = Obj(("name", Str()), ("category", Str()), ("sizeCm", Arr(Num())), ("shape", Str()), ("likelyPlaces", Arr(Str())));

            string json = await Generate("profile", cfg.profile, true, prompt, null, schema, cfg.profileTimeoutSec, ct);
            var profile = JsonConvert.DeserializeObject<ItemProfile>(json);
            return ApplyKnowledge(profile, userText, Knowledge);
        }

        /// <summary>
        /// Known item? Then sizes, shape and typical places come from the knowledge base (reliable), while the
        /// name keeps the user's words (colour etc.). Unknown items keep Gemini's answer.
        /// </summary>
        public static ItemProfile ApplyKnowledge(ItemProfile p, string userText, LostItemKnowledge kb)
        {
            p ??= new ItemProfile();
            var info = kb.Match(userText, p.category) ?? kb.Match(p.name ?? "");
            if (info == null) { p.category = "other"; return p; }
            p.category = info.id;
            p.sizeCm = info.sizeCm;
            p.shape = info.shape;
            p.likelyPlaces = new List<string>(info.typical);
            return p;
        }

        /// <summary>Call 1 from a photo: same profile, plus "appearance" so later prompts know what THIS item looks like.</summary>
        public async Task<ItemProfile> GetItemProfileFromPhoto(byte[] jpg, string userText, CancellationToken ct)
        {
            string prompt = PromptLibrary.Fill("profile_photo",
                ("userText", string.IsNullOrWhiteSpace(userText) ? "(nothing)" : userText),
                ("categories", Knowledge.CategoryList()));
            var schema = Obj(("objectsSeen", Arr(Str())), ("name", Str()), ("category", Str()), ("appearance", Str()),
                             ("sizeCm", Arr(Num())), ("shape", Str()), ("likelyPlaces", Arr(Str())));

            string json = await Generate("profile", cfg.spots, true, prompt, jpg, schema, cfg.spotsTimeoutSec, ct);
            var profile = JsonConvert.DeserializeObject<ItemProfile>(json);
            // Several objects and no words saying which: don't guess (the model happily picks the biggest one).
            var seen = JObject.Parse(json)["objectsSeen"]?.ToObject<List<string>>() ?? new List<string>();
            if (profile != null) profile.ambiguous = CountsAsAmbiguous(seen, userText);
            profile = ApplyKnowledge(profile, profile?.name ?? userText ?? "", Knowledge);
            profile.referenceJpg = jpg;
            return profile;
        }

        static readonly string[] NotSeparateObjects = { "case", "surface", "table", "desk", "floor", "wall", "shelf", "counter", "background", "hand" };

        /// <summary>
        /// Two or more real objects and no words → ask which. An item's own case, the table it lies on, or the
        /// hand holding it are not "another object" (glasses in their case is one thing to look for).
        /// </summary>
        public static bool CountsAsAmbiguous(IEnumerable<string> objectsSeen, string userText)
        {
            if (!string.IsNullOrWhiteSpace(userText)) return false;
            int n = 0;
            foreach (var o in objectsSeen)
                if (!NotSeparateObjects.Any(w => LostItemKnowledge.ContainsWords(o, w))) n++;
            return n >= 2;
        }

        // ---------- Call 2: find hiding spots (768 px snapshot) ----------
        public async Task<List<HidingSpot>> FindSpots(byte[] jpg, ItemProfile item, CancellationToken ct)
        {
            var info = Knowledge.Match(item.name ?? "", item.category);
            string prompt = PromptLibrary.Fill("spots",
                ("itemName", item.name),
                ("itemDescription", Describe(item)),
                ("size", $"{item.WidthCm:0} x {item.HeightCm:0} cm"),
                ("lookalike", info?.lookalike ?? $"the actual {item.name}, not something that only looks similar"),
                ("knowledge", LostItemKnowledge.SpotsBlock(item.name, info)));

            var itemBox = Arr(Int());
            itemBox["nullable"] = true;
            // Order matters: the model writes "itemEvidence" and each spot's "seen" BEFORE deciding (grounding).
            var schema = Obj(("itemEvidence", Str()), ("itemVisible", Bool()), ("itemConfidence", Int()), ("itemBox", itemBox),
                ("spots", Arr(Obj(("seen", Str()), ("label", Str()), ("box_2d", Arr(Int())), ("likelihood", Int()), ("hint", Str())))));

            string json = await Generate("spots", cfg.spots, true, prompt, jpg, schema, cfg.spotsTimeoutSec, ct, item.referenceJpg);
            return ParseSpots(json, item, Knowledge);
        }

        /// <summary>
        /// Gemini's answer → spots for the app. The item itself (if visible) comes first, flagged isItem.
        /// Impossible places (snack bag, chapstick for glasses…) are dropped here even if the model slipped.
        /// </summary>
        public static List<HidingSpot> ParseSpots(string json, ItemProfile item, LostItemKnowledge kb)
        {
            var root = JObject.Parse(json);
            var result = new List<HidingSpot>();
            var info = kb.Match(item.name ?? "", item.category);

            var box = root["itemBox"]?.Type == JTokenType.Array ? root["itemBox"].ToObject<int[]>() : null;
            int confidence = root["itemConfidence"]?.Type == JTokenType.Integer ? root["itemConfidence"].Value<int>() : 3;
            if (root["itemVisible"]?.Value<bool>() == true && box is { Length: 4 } && !EvidenceIsWeak((string)root["itemEvidence"], item))
            {
                // Certain → "There it is!". Probably → just the top spot to look at (a wrong "found it" is
                // worse than one extra glance). A guess → ignored.
                if (confidence >= 3)
                    result.Add(new HidingSpot { label = item.name, box_2d = box, likelihood = 3, isItem = true,
                                                hint = "Is this it?", seen = (string)root["itemEvidence"] });
                else if (confidence == 2)
                    result.Add(new HidingSpot { label = "maybe " + item.name, box_2d = box, likelihood = 3,
                                                hint = "Look closely here", seen = (string)root["itemEvidence"] });
            }

            var spots = root["spots"]?.ToObject<List<HidingSpot>>() ?? new List<HidingSpot>();
            float size = item.sizeCm is { Length: >= 2 } ? Mathf.Max(item.sizeCm[0], item.sizeCm[1]) : 10f;
            foreach (var s in spots)
            {
                if (s == null) continue;
                if (kb.IsImpossiblePlace(s.label, info, size) || LostItemKnowledge.IsTheItem(s.label, item.name))
                {
                    Debug.Log($"[Gemini] dropped impossible spot \"{s.label}\" for {item.name}");
                    continue;
                }
                result.Add(s);
            }
            return result;
        }

        // ---------- Call 3: spot check (512 px frame) ----------
        public async Task<SpotCheckResult> CheckSpot(byte[] jpg, ItemProfile item, string spotLabel, string note, CancellationToken ct)
        {
            var info = Knowledge.Match(item.name ?? "", item.category);
            string prompt = PromptLibrary.Fill("check",
                ("itemName", item.name),
                ("itemDescription", Describe(item)),
                ("spotLabel", spotLabel),
                ("lookalike", info?.lookalike ?? $"the actual {item.name}"),
                ("checkKnowledge", LostItemKnowledge.CheckBlock(spotLabel, info)),
                ("note", string.IsNullOrWhiteSpace(note) ? "" : $"Important: {note}\n"));

            var box = Arr(Int());
            box["nullable"] = true;
            var spotBox = Arr(Int());
            spotBox["nullable"] = true;
            var schema = Obj(("seen", Str()), ("itemEvidence", Str()), ("itemVisible", Bool()), ("itemBox", box), ("closed", Bool()),
                             ("insideVisible", Bool()), ("mightBeHidden", Bool()), ("spotBox", spotBox), ("tip", Str()));

            // No retry: checks re-trigger naturally from dwell, and a stale answer is worse than none.
            string json = await Generate("check", cfg.check, false, prompt, jpg, schema, cfg.checkTimeoutSec, ct, item.referenceJpg);
            return ParseCheck(json, item);
        }

        /// <summary>
        /// A "found it" claim needs evidence that points at something: colour + place, not our own words echoed
        /// back. In tests the model once "saw" glasses in a bedroom with none, evidence = "folded glasses frame"
        /// (our description, word for word). Such claims are ignored.
        /// </summary>
        public static bool EvidenceIsWeak(string evidence, ItemProfile item)
        {
            if (string.IsNullOrWhiteSpace(evidence)) return true;
            string e = evidence.Trim();
            if (e.Length < 12 || e.Equals("none", StringComparison.OrdinalIgnoreCase)) return true;
            return item != null && (LostItemKnowledge.ContainsWords(Describe(item), e) ||
                                    LostItemKnowledge.ContainsWords(item.shape ?? "~", e));
        }

        /// <summary>
        /// "Checked" needs proof: the inside is visible, it isn't closed, AND nothing there could still hide the
        /// item (no pile, not too dark, nothing of the same colour/shape). A closed glasses case can't pass, and a
        /// basket of dark clothes asks the user to look closer instead of saying "not here".
        /// </summary>
        public static SpotCheckResult ParseCheck(string json, ItemProfile item = null)
        {
            var r = JsonConvert.DeserializeObject<SpotCheckResult>(json) ?? new SpotCheckResult();
            if (r.itemVisible && item != null && EvidenceIsWeak(r.itemEvidence ?? r.seen, item)) r.itemVisible = false;
            r.spotFullyVisible = r.insideVisible && !r.closed && !r.itemVisible && !r.mightBeHidden;
            if (!r.itemVisible) r.itemBox = null;
            return r;
        }

        /// <summary>Appearance from the user's photo beats the generic shape ("teal frames…" vs "folded glasses frame").</summary>
        static string Describe(ItemProfile item) =>
            (string.IsNullOrWhiteSpace(item.appearance) ? item.shape : item.appearance) + $", about {item.WidthCm:0} x {item.HeightCm:0} cm";

        // ---------- HTTP ----------

        /// <summary>
        /// POSTs one generateContent request and returns the model's JSON text (fences stripped).
        /// If <paramref name="allowRetry"/>, a network error or overload (429/5xx) gets ONE more try on
        /// <see cref="GeminiConfig.fallback"/>: a different model is usually not the one that's overloaded.
        /// </summary>
        async Task<string> Generate(string call, GeminiConfig.ModelChoice choice, bool allowRetry,
                                    string prompt, byte[] jpg, JObject schema, int timeoutSec, CancellationToken ct,
                                    byte[] reference = null)
        {
            var parts = new JArray();
            // Optional reference photo FIRST, captioned, so the model knows which image is the item and which the room.
            if (reference != null)
            {
                parts.Add(new JObject { ["text"] = "Reference photo of the lost item (or one just like it):" });
                parts.Add(Image(reference));
                parts.Add(new JObject { ["text"] = "Photo to analyse:" });
            }
            if (jpg != null) parts.Add(Image(jpg));
            parts.Add(new JObject { ["text"] = prompt });

            for (int attempt = 1; ; attempt++)
            {
                byte[] payload = BuildBody(parts, schema, choice);
                long t = GeminiLog.Begin();
                bool ok = false;
                string logText = null;
                try
                {
                    using var req = new UnityWebRequest(string.Format(cfg.endpointFormat, choice.model), UnityWebRequest.kHttpVerbPOST)
                    {
                        uploadHandler = new UploadHandlerRaw(payload),
                        downloadHandler = new DownloadHandlerBuffer(),
                        timeout = timeoutSec,
                    };
                    req.SetRequestHeader("Content-Type", "application/json");
                    req.SetRequestHeader("x-goog-api-key", apiKey);

                    // Unity 6 makes the request awaitable; it resumes on the main thread. Cancelling aborts it.
                    using (ct.Register(req.Abort))
                        await req.SendWebRequest();
                    ct.ThrowIfCancellationRequested();

                    if (req.result == UnityWebRequest.Result.Success)
                    {
                        string text = StripFences(ExtractText(req.downloadHandler.text));
                        ok = true;
                        logText = text;
                        return text;
                    }

                    logText = $"{req.result} {req.responseCode} {req.error} {req.downloadHandler.text}";
                    // Wi-Fi blip / timeout / "high demand" are worth a retry; a 400/403/404 will just fail again.
                    bool transient = req.result == UnityWebRequest.Result.ConnectionError
                                     || req.responseCode == 429 || req.responseCode >= 500;
                    if (!allowRetry || !transient || attempt >= 2)
                        throw new Exception($"Gemini {call} failed: {logText}");
                }
                finally
                {
                    GeminiLog.End(t, call, choice.model, jpg?.Length ?? 0, ok, logText ?? "cancelled");
                }
                choice = cfg.fallback;
            }
        }

        static JObject Image(byte[] jpg) =>
            new() { ["inline_data"] = new JObject { ["mime_type"] = "image/jpeg", ["data"] = Convert.ToBase64String(jpg) } };

        byte[] BuildBody(JArray parts, JObject schema, GeminiConfig.ModelChoice choice)
        {
            var gen = new JObject
            {
                ["temperature"] = cfg.temperature,
                ["responseMimeType"] = "application/json",
                ["responseSchema"] = schema,     // forces the exact JSON shape we parse
            };
            // Least thinking the model allows = lowest latency. Format differs per model family (see GeminiConfig).
            if (!string.IsNullOrWhiteSpace(choice.thinkingJson)) gen["thinkingConfig"] = JObject.Parse(choice.thinkingJson);

            var body = new JObject
            {
                ["contents"] = new JArray { new JObject { ["role"] = "user", ["parts"] = parts } },
                ["generationConfig"] = gen,
            };
            return Encoding.UTF8.GetBytes(body.ToString(Formatting.None));
        }

        /// <summary>Answer text lives at candidates[0].content.parts[*].text.</summary>
        static string ExtractText(string responseJson)
        {
            var parts = JObject.Parse(responseJson)["candidates"]?[0]?["content"]?["parts"] as JArray;
            if (parts == null) throw new Exception("Gemini returned no candidates: " + responseJson);
            var sb = new StringBuilder();
            foreach (var p in parts)
                if (p["thought"]?.Value<bool>() != true) sb.Append((string)p["text"]);
            return sb.ToString();
        }

        /// <summary>Pitfall 6: models sometimes wrap JSON in ```json ... ``` even in JSON mode.</summary>
        public static string StripFences(string s)
        {
            s = s.Trim();
            if (!s.StartsWith("```")) return s;
            int firstNewline = s.IndexOf('\n');
            int lastFence = s.LastIndexOf("```", StringComparison.Ordinal);
            if (firstNewline < 0 || lastFence <= firstNewline) return s.Trim('`');
            return s.Substring(firstNewline + 1, lastFence - firstNewline - 1).Trim();
        }

        // ---------- JSON schema helpers (Gemini uses OpenAPI-style upper-case type names) ----------
        static JObject Str() => new() { ["type"] = "STRING" };
        static JObject Num() => new() { ["type"] = "NUMBER" };
        static JObject Int() => new() { ["type"] = "INTEGER" };
        static JObject Bool() => new() { ["type"] = "BOOLEAN" };
        static JObject Arr(JObject items) => new() { ["type"] = "ARRAY", ["items"] = items };

        /// <summary>
        /// Object schema. Every property is required unless its schema says "nullable".
        /// propertyOrdering makes the model write the fields in THIS order (e.g. "seen" before "label").
        /// </summary>
        static JObject Obj(params (string name, JObject schema)[] props)
        {
            var properties = new JObject();
            var required = new JArray();
            var ordering = new JArray();
            foreach (var (name, schema) in props)
            {
                properties[name] = schema;
                ordering.Add(name);
                if (schema["nullable"]?.Value<bool>() != true) required.Add(name);
            }
            return new JObject
            {
                ["type"] = "OBJECT", ["properties"] = properties, ["required"] = required, ["propertyOrdering"] = ordering,
            };
        }
    }
}
