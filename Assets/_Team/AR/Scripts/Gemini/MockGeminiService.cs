using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace FindMe
{
    /// <summary>
    /// Offline stand-in for Gemini (plan Section 6, "Mock service"). Canned answers after a fake delay,
    /// so the whole flow runs in the Editor with XR Simulation and no internet or key.
    /// </summary>
    public class MockGeminiService : IGeminiService
    {
        const string Model = "mock";

        // How many times each spot label has been checked; drives the "closed → open → found" cycle.
        readonly Dictionary<string, int> checkCounts = new();

        public async Task<ItemProfile> GetItemProfile(string userText, CancellationToken ct)
        {
            long t = GeminiLog.Begin();
            bool ok = false;
            ItemProfile p = null;
            try
            {
                await Awaitable.WaitForSecondsAsync(0.6f, ct);
                p = GuessProfile(userText);
                ok = true;
                return p;
            }
            finally { GeminiLog.End(t, "profile", Model, 0, ok, ok ? JsonConvert.SerializeObject(p) : "cancelled"); }
        }

        public async Task<List<HidingSpot>> FindSpots(byte[] jpg, ItemProfile item, CancellationToken ct)
        {
            long t = GeminiLog.Begin();
            bool ok = false;
            List<HidingSpot> spots = null;
            try
            {
                await Awaitable.WaitForSecondsAsync(1.2f, ct);
                // Canned boxes at FIXED screen positions, [ymin, xmin, ymax, xmax] with 0,0 = top-left.
                // They don't look at the image: in mock mode a "shelf" appears even on a bare wall. That's the
                // mock, not Gemini. Turn Mock OFF to test what real Gemini sees.
                spots = new List<HidingSpot>
                {
                    new() { label = "pile of clothes", box_2d = new[] { 380, 30, 620, 330 },  likelihood = 3, hint = "Wallets slide under clothes", seen = "MOCK: fake clothes pile" },
                    new() { label = "drawer",          box_2d = new[] { 650, 350, 850, 650 }, likelihood = 2, hint = "Check inside", seen = "MOCK: fake drawer" },
                    new() { label = "bag",             box_2d = new[] { 380, 690, 620, 960 }, likelihood = 2, hint = "Look in the pockets", seen = "MOCK: fake bag" },
                    new() { label = "shelf",           box_2d = new[] { 80, 300, 230, 700 },  likelihood = 1, hint = "Scan the top", seen = "MOCK: fake shelf" },
                };
                ok = true;
                return spots;
            }
            finally { GeminiLog.End(t, "spots", Model, jpg?.Length ?? 0, ok, ok ? JsonConvert.SerializeObject(spots) : "cancelled"); }
        }

        public async Task<SpotCheckResult> CheckSpot(byte[] jpg, ItemProfile item, string spotLabel, string note, CancellationToken ct)
        {
            long t = GeminiLog.Begin();
            bool ok = false;
            SpotCheckResult r = null;
            try
            {
                await Awaitable.WaitForSecondsAsync(0.8f, ct);
                string key = (spotLabel ?? "").ToLowerInvariant();
                checkCounts.TryGetValue(key, out int n);
                checkCounts[key] = ++n;

                bool open = n % 2 == 0;   // 1st check: closed, 2nd: open, 3rd: closed again...
                bool itemHere = open && key.Contains("bag");
                r = new SpotCheckResult
                {
                    spotFullyVisible = open,
                    itemVisible = itemHere,
                    itemBox = itemHere ? new[] { 400, 400, 600, 600 } : null,
                    tip = open ? "" : "Open it up",
                };
                ok = true;
                return r;
            }
            finally { GeminiLog.End(t, "check", Model, jpg?.Length ?? 0, ok, ok ? JsonConvert.SerializeObject(r) : "cancelled"); }
        }

        static ItemProfile GuessProfile(string text)
        {
            string name = text.Trim().ToLowerInvariant();
            var p = new ItemProfile
            {
                name = name,
                likelyPlaces = new List<string> { "bag", "pile of clothes", "sofa cushions", "drawer", "desk" },
            };
            // A few common items so the prompts downstream look realistic.
            if (name.Contains("wallet"))       { p.sizeCm = new[] { 11f, 9f };  p.shape = "flat rectangle"; }
            else if (name.Contains("key"))     { p.sizeCm = new[] { 8f, 4f };   p.shape = "small metal keys"; }
            else if (name.Contains("phone"))   { p.sizeCm = new[] { 15f, 7f };  p.shape = "flat rectangle"; }
            else if (name.Contains("glasses")) { p.sizeCm = new[] { 14f, 5f };  p.shape = "folded glasses"; }
            else if (name.Contains("remote"))  { p.sizeCm = new[] { 18f, 5f };  p.shape = "long thin bar"; }
            p.FillDefaults(text);
            return p;
        }
    }
}
