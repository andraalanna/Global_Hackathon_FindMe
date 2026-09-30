namespace FindMe
{
    /// <summary>
    /// The calls SearchController makes into the screen UI (plan Section 8). The UI lead's UIManager
    /// implements this; until then Dev/DevUI does. An interface (instead of calling UIManager directly)
    /// keeps our assembly free of any dependency on the UI folder.
    /// More methods get added as phases need them (spot panel, toasts, match, found, summary).
    /// </summary>
    public interface ISearchUI
    {
        void ShowItemCard(ItemProfile profile);
        void ShowStatus(string message);
        void HideStatus();
        void SetReadiness(float progress01);
        void SetScanEnabled(bool on);
        /// <summary>Progress chip: "2 of 5 spots checked".</summary>
        void SetProgress(int checkedCount, int total);
        /// <summary>
        /// Panel for the cue being looked at: its label, Gemini's tip, dwell progress 0..1.
        /// manualButtons: also show "✓ Checked" / "Found it" (after a failed or unclear automatic check).
        /// </summary>
        void ShowSpotPanel(string label, string tip, float dwell01, bool manualButtons);
        void HideSpotPanel();
        /// <summary>"Drawer checked ✓" toast with an Undo button (about 4 s) that calls UndoChecked().</summary>
        void ShowCheckedToast(string label);
        /// <summary>"Is this your wallet?" sheet: Yes → ConfirmMatch(true), Not it → ConfirmMatch(false).</summary>
        void ShowMatch(string label);
        /// <summary>Celebration: the item was found at this spot.</summary>
        void ShowFound(string label);
    }
}
