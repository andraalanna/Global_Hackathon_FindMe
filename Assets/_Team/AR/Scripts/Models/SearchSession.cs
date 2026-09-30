using System.Collections.Generic;

namespace FindMe
{
    /// <summary>
    /// What happened in one search (plan Phase 10), for the summary screen: "Found in 2m 41s · 4 spots checked".
    /// Times are Time.time seconds. Lives only for this session (no saving, per the plan's scope).
    /// </summary>
    public class SearchSession
    {
        public struct SpotRecord
        {
            public string label;
            public SpotState state;
            public float checkedAt;      // -1 = never checked
        }

        public string item;
        public float startTime;
        public int scans;
        public string foundLabel;        // null until found
        public float foundTime = -1f;
        public readonly List<SpotRecord> spots = new();

        public bool Found => foundLabel != null;
        public float DurationSeconds(float now) => (Found ? foundTime : now) - startTime;

        public int CheckedCount
        {
            get
            {
                int n = 0;
                foreach (var s in spots) if (s.state is SpotState.Checked or SpotState.Found) n++;
                return n;
            }
        }

        /// <summary>"Found in 2m 41s · 4 spots checked" (plan wording). Pure, so it's unit-tested.</summary>
        public static string FormatSummary(float seconds, int checkedCount)
        {
            int total = UnityEngine.Mathf.Max(0, UnityEngine.Mathf.RoundToInt(seconds));
            int m = total / 60, s = total % 60;
            string time = m > 0 ? $"{m}m {s}s" : $"{s}s";
            string spotsText = checkedCount == 1 ? "1 spot checked" : $"{checkedCount} spots checked";
            return $"Found in {time} · {spotsText}";
        }

        public string Summary(float now) => FormatSummary(DurationSeconds(now), CheckedCount);
    }
}
