using UnityEngine;

namespace FindMe
{
    /// <summary>
    /// SearchController, part 2: everything about individual spots once cues exist
    /// (manual buttons now; the automatic Gemini check is added in Phase 8).
    /// </summary>
    public partial class SearchController
    {
        /// <summary>"✓ Checked" button: mark the cue being looked at as checked.</summary>
        public void MarkChecked()
        {
            SpotCue cue = focus.Focused;
            if (State != SearchState.Searching || cue == null) return;
            cue.SetState(SpotState.Checked);
            RefreshCues();
        }

        /// <summary>"Found it" button: the user found the item at the cue being looked at.</summary>
        public void MarkFound()
        {
            SpotCue cue = focus.Focused;
            if (State != SearchState.Searching || cue == null) return;
            cue.SetState(SpotState.Found);
            RefreshCues();
        }
    }
}
