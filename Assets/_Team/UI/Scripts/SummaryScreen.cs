using System;
using TMPro;
using UnityEngine;

namespace FindMe.UI
{
    /// <summary>
    /// Screen 10: "Nice search!" with three stat tiles (time, spots checked, scans) and a way back home.
    /// Everything comes from the SearchSession that SearchController hands over right after "Found".
    /// </summary>
    public class SummaryScreen : MonoBehaviour
    {
        public TMP_Text timeValue, spotsValue, scansValue;
        public TMP_Text foundLine;
        public ChunkyButton homeButton;

        public event Action onHome;

        void Awake() => homeButton.Button.onClick.AddListener(() => onHome?.Invoke());

        public void Set(SearchSession s, string itemName)
        {
            timeValue.text = FormatTime(s.DurationSeconds(Time.time));
            spotsValue.text = s.CheckedCount.ToString();
            scansValue.text = s.scans.ToString();
            foundLine.text = FoundScreen.FoundSentence(itemName, s.foundLabel, "was in the");
        }

        /// <summary>"2m 41s" / "38s" (same format as SearchSession's own summary line).</summary>
        public static string FormatTime(float seconds)
        {
            int total = Mathf.Max(0, Mathf.RoundToInt(seconds));
            int m = total / 60, sec = total % 60;
            return m > 0 ? $"{m}m {sec}s" : $"{sec}s";
        }
    }
}
