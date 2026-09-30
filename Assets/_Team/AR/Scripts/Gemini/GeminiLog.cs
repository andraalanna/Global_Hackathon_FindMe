using System.Collections.Generic;
using System.Diagnostics;

namespace FindMe
{
    /// <summary>
    /// Tiny in-memory log of Gemini calls (real and mock) for the DebugOverlay:
    /// model, image size, latency, success and the first part of the raw answer.
    /// </summary>
    public static class GeminiLog
    {
        public struct Entry
        {
            public string call;     // "profile" / "spots" / "check"
            public string model;
            public float kb;
            public long ms;
            public bool ok;
            public string raw;      // truncated answer or error text
        }

        const int Keep = 6;
        const int RawChars = 200;

        public static int InFlight { get; private set; }
        public static readonly List<Entry> Recent = new();   // newest last

        /// <summary>Call before sending. Returns a timestamp to pass to <see cref="End"/>.</summary>
        public static long Begin()
        {
            InFlight++;
            return Stopwatch.GetTimestamp();
        }

        public static void End(long started, string call, string model, int bytes, bool ok, string raw)
        {
            InFlight--;
            long ms = (Stopwatch.GetTimestamp() - started) * 1000 / Stopwatch.Frequency;
            raw ??= "";
            if (raw.Length > RawChars) raw = raw.Substring(0, RawChars) + "…";
            var e = new Entry { call = call, model = model, kb = bytes / 1024f, ms = ms, ok = ok, raw = raw };

            Recent.Add(e);
            if (Recent.Count > Keep) Recent.RemoveAt(0);
            UnityEngine.Debug.Log($"[Gemini] {call} {model} {e.kb:0}KB {ms}ms ok={ok} {raw}");
        }
    }
}
