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

        // Optional extras (added for the smarter check; older code ignores them)
        public string seen;             // what Gemini sees at the spot, e.g. "closed glasses case on desk"
        public string itemEvidence;     // where/what the item is, if Gemini claims to see it
        public bool closed;             // the spot is still closed/covered
        public bool insideVisible;      // the inside/underneath is clearly visible
        public bool mightBeHidden;      // visible, but piled/dark/similar-looking things: could still be there
        public int[] spotBox;           // where the spot is in THIS photo (used to re-measure the cue up close)

        public bool HasSpotBox => IsValidBox(spotBox);

        /// <summary>itemBox is only usable if it's a proper [ymin, xmin, ymax, xmax] inside 0..1000.</summary>
        public bool HasItemBox => IsValidBox(itemBox);

        static bool IsValidBox(int[] b) =>
            b is { Length: 4 } && b[0] < b[2] && b[1] < b[3] && b[0] >= 0 && b[1] >= 0 && b[2] <= 1000 && b[3] <= 1000;
    }

    public enum SpotState { ToCheck, Focused, Checking, NeedsOpen, Checked, Candidate, Found }
}
