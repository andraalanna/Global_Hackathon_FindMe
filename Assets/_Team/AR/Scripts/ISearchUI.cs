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
    }
}
