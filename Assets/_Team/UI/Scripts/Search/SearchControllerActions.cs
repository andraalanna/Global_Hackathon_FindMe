namespace FindMe.UI
{
    /// <summary>The real search: forwards every UI button to the AR lead's SearchController.</summary>
    public class SearchControllerActions : ISearchActions
    {
        readonly SearchController search;

        public SearchControllerActions(SearchController search) => this.search = search;

        public void StartSearch(string text) => search.StartSearch(text);
        public void ScanArea() => search.ScanArea();
        public void CheckHere() => search.CheckHere();
        public void MarkChecked() => search.MarkChecked();
        public void UndoChecked() => search.UndoChecked();
        public void ConfirmMatch(bool yes) => search.ConfirmMatch(yes);
        public void StopSearch() => search.StopSearch();

        public bool MarkFound()
        {
            // MarkFound only works on a targeted cue; it switches the state to Found (and calls ShowFound) when it does.
            search.MarkFound();
            return search.State == SearchState.Found;
        }
    }
}
