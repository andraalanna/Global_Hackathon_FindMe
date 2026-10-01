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
        /// <summary>Names of the spots marked in this search ("Where did you find it?" list).</summary>
        System.Collections.Generic.List<string> SpotLabels();
        /// <summary>The user found it at this marked spot: end the search there. False if no such spot.</summary>
        bool MarkFoundAt(string label);
        void ConfirmMatch(bool yes);
        void StopSearch();
    }
}
