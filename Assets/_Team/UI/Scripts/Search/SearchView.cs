using UnityEngine;

namespace FindMe.UI
{
    /// <summary>
    /// Everything the search screens show, written by UIManager's ISearchUI methods and read by the screens.
    /// One object, so a screen opened late (e.g. the HUD after mapping) still shows the latest values.
    /// </summary>
    public class SearchView
    {
        public Thing thing;                 // what we look for (name, icon, color)
        public ItemProfile profile;         // Gemini's description, once known
        public string status;               // last ShowStatus text (null = hidden)
        public float readiness;             // 0..1 room mapping
        public bool scanEnabled;
        public int checkedCount, total;

        // Spot panel (the cue being looked at)
        public bool panelOn, panelManual;
        public string panelLabel, panelTip;
        public float panelDwell;

        // Result
        public float startTime;             // Time.unscaledTime when the search started
        public float endTime = -1f;         // when found
        public string foundAt;
        public SearchSession session;
        public int scans;

        public float Elapsed => (endTime >= 0f ? endTime : Time.unscaledTime) - startTime;

        /// <summary>"1:35" like the prototype timer.</summary>
        public string ElapsedText
        {
            get
            {
                int s = Mathf.Max(1, Mathf.RoundToInt(Elapsed));
                return $"{s / 60}:{s % 60:00}";
            }
        }

        public void Reset(Thing t)
        {
            thing = t;
            profile = null;
            status = null;
            readiness = 0f;
            scanEnabled = false;
            checkedCount = total = 0;
            panelOn = panelManual = false;
            startTime = Time.unscaledTime;
            endTime = -1f;
            foundAt = null;
            session = null;
            scans = 0;
        }
    }
}
