using System;
using System.Collections.Generic;
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

        // ---------- Call 1: item profile (text only) ----------
        public async Task<ItemProfile> GetItemProfile(string userText, CancellationToken ct)
        {
            string prompt =
                $"The user lost: \"{userText}\".\n" +
                "Return JSON with: name (short, lowercase), sizeCm [width, height] using typical real-world size,\n" +
                "shape (2-4 words), likelyPlaces (up to 5 short phrases, most likely first).";
            var schema = Obj(("name", Str()), ("sizeCm", Arr(Num())), ("shape", Str()), ("likelyPlaces", Arr(Str())));

            string json = await Generate("profile", cfg.profile, true, prompt, null, schema, cfg.profileTimeoutSec, ct);
            return JsonConvert.DeserializeObject<ItemProfile>(json);
        }

        // ---------- Call 2: find hiding spots (768 px snapshot) ----------
        public async Task<List<HidingSpot>> FindSpots(byte[] jpg, ItemProfile item, CancellationToken ct)
        {
            // Anti-hallucination (tested on an empty room: the plan's prompt invented a shelf/cabinet/drawer
            // in 4 of 6 runs on flash-lite, this one in 0 of 5):
            //  - no fixed menu of furniture to pick from (examples only, "e.g.")
            //  - an empty list is explicitly a valid answer
            //  - "seen" comes FIRST in each item, so the model describes what's really there before naming it
            string prompt =
                $"We are looking for a lost {Describe(item)}.\n" +
                "List up to 5 places IN THIS PHOTO where it could be hidden.\n" +
                "Rules:\n" +
                "- Only list objects you can actually see in this photo. Never invent furniture that is not there.\n" +
                "- A bare wall, empty floor, ceiling, window, door or empty flat surface is NOT a hiding place.\n" +
                "- Fewer than 5 is fine. If you see no real hiding place, return an empty list [].\n" +
                "- Only include places big enough to hold it. Prefer containers and piles\n" +
                "  (e.g. drawer, cabinet, bag, pile of clothes, sofa cushions, under the bed, coat) over open surfaces.\n" +
                "For each place:\n" +
                "  seen: what you actually see in that part of the photo (max 8 words),\n" +
                "  label: 1-3 words, box_2d: [ymin, xmin, ymax, xmax] normalized 0-1000,\n" +
                "  likelihood: 1-3 (3 = most likely), hint: max 8 words, friendly.";
            var schema = Arr(Obj(("seen", Str()), ("label", Str()), ("box_2d", Arr(Int())), ("likelihood", Int()), ("hint", Str())));

            string json = await Generate("spots", cfg.spots, true, prompt, jpg, schema, cfg.spotsTimeoutSec, ct);
            return JsonConvert.DeserializeObject<List<HidingSpot>>(json) ?? new List<HidingSpot>();
        }

        // ---------- Call 3: spot check (512 px frame) ----------
        public async Task<SpotCheckResult> CheckSpot(byte[] jpg, ItemProfile item, string spotLabel, CancellationToken ct)
        {
            string prompt =
                $"We are looking for a lost {Describe(item)}.\n" +
                $"The user is checking this spot: \"{spotLabel}\".\n" +
                $"itemVisible: true only if the {item.name} itself is clearly visible. If true, give itemBox [ymin, xmin, ymax, xmax] 0-1000.\n" +
                "spotFullyVisible: true only if the inside or underneath of the spot is clearly visible\n" +
                "(drawer open, blanket lifted, bag open). A closed drawer or covered pile is false.\n" +
                "tip: max 6 words telling the user what to do next (e.g. \"Open the drawer\", \"Lift the top shirt\").";
            var itemBox = Arr(Int());
            itemBox["nullable"] = true;
            var schema = Obj(("itemVisible", Bool()), ("itemBox", itemBox), ("spotFullyVisible", Bool()), ("tip", Str()));

            // No retry: checks re-trigger naturally from dwell, and a stale answer is worse than none.
            string json = await Generate("check", cfg.check, false, prompt, jpg, schema, cfg.checkTimeoutSec, ct);
            return JsonConvert.DeserializeObject<SpotCheckResult>(json);
        }

        static string Describe(ItemProfile item) =>
            $"{item.name} ({item.shape}, about {item.WidthCm:0} x {item.HeightCm:0} cm)";

        // ---------- HTTP ----------

        /// <summary>
        /// POSTs one generateContent request and returns the model's JSON text (fences stripped).
        /// If <paramref name="allowRetry"/>, a network error or overload (429/5xx) gets ONE more try on
        /// <see cref="GeminiConfig.fallback"/>: a different model is usually not the one that's overloaded.
        /// </summary>
        async Task<string> Generate(string call, GeminiConfig.ModelChoice choice, bool allowRetry,
                                    string prompt, byte[] jpg, JObject schema, int timeoutSec, CancellationToken ct)
        {
            var parts = new JArray { new JObject { ["text"] = prompt } };
            if (jpg != null)
                parts.Add(new JObject { ["inline_data"] = new JObject { ["mime_type"] = "image/jpeg", ["data"] = Convert.ToBase64String(jpg) } });

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
