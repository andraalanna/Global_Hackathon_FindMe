namespace FindMe.UI
{
    /// <summary>
    /// What the UI's buttons ask the search to do. In the app this is SearchController (through
    /// SearchControllerActions); in UI_Test it is FakeSearchDriver, which plays a scripted search.
    /// Names match SearchController's public methods.
    /// </summary>
    public interface ISearchActions
    {
        void StartSearch(string text);
        void ScanArea();
        /// <summary>"Check here": check the marked spot the phone points at, right away.</summary>
        void CheckHere();
        void MarkChecked();
        /// <summary>"Found it" on a spot. Returns false when no spot is targeted (the UI then asks "Where was it?").</summary>
        bool MarkFound();
        void UndoChecked();
        void ConfirmMatch(bool yes);
        void StopSearch();
    }
}
