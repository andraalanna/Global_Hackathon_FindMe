using System;

namespace FindMe
{
    /// <summary>Answer to "is it here?" for one spot (Gemini Call 3). Used from Phase 8.</summary>
    [Serializable]
    public class SpotCheckResult
    {
        public bool itemVisible;
        public int[] itemBox;           // [ymin, xmin, ymax, xmax] 0..1000, null when not visible
        public bool spotFullyVisible;
        public string tip;              // "Open the drawer"
    }

    public enum SpotState { ToCheck, Focused, Checking, NeedsOpen, Checked, Candidate, Found }
}
