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

        /// <summary>itemBox is only usable if it's a proper [ymin, xmin, ymax, xmax] inside 0..1000.</summary>
        public bool HasItemBox =>
            itemBox is { Length: 4 } && itemBox[0] < itemBox[2] && itemBox[1] < itemBox[3] &&
            itemBox[0] >= 0 && itemBox[1] >= 0 && itemBox[2] <= 1000 && itemBox[3] <= 1000;
    }

    public enum SpotState { ToCheck, Focused, Checking, NeedsOpen, Checked, Candidate, Found }
}
