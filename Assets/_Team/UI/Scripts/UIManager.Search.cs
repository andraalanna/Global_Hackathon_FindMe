using UnityEngine;

namespace FindMe.UI
{
    /// <summary>
    /// UIManager, part 2: the search. SearchController calls the ISearchUI methods below; they update
    /// <see cref="Search"/> (the view model) and move between the search screens. Buttons call
    /// <see cref="Actions"/> (the real SearchController, or FakeSearchDriver in UI_Test).
    /// </summary>
    public partial class UIManager : ISearchUI
    {
        [Header("Search")]
        [Tooltip("The AR lead's SearchController. Found automatically; if there is none (UI_Test), FakeSearchDriver plays a fake search.")]
        [SerializeField] SearchController search;

        public SearchView Search { get; } = new SearchView();
        public ISearchActions Actions { get; private set; }

        AnalyzingScreen analyzing;
        MappingScreen mapping;
        ScanScreen scan;
        FoundScreen found;

        void SetupSearch()
        {
            if (search == null) search = FindAnyObjectByType<SearchController>();
            if (search != null) Actions = new SearchControllerActions(search);
            else
            {
                if (!TryGetComponent(out FakeSearchDriver fake)) fake = gameObject.AddComponent<FakeSearchDriver>();
                fake.ui = this;
                Actions = fake;
            }

            analyzing = Add<AnalyzingScreen>("analyzing");
            mapping = Add<MappingScreen>("mapping");
            scan = Add<ScanScreen>("scan");
            Add<FoundWhereScreen>("foundWhere");
            found = Add<FoundScreen>("found");
            Add<SummaryScreen>("summary");
            Add<NotFoundScreen>("notfound");
        }

        // ---------- called by screens ----------

        /// <summary>"Continue" on What / "Find my keys" on an item: start searching for the selected thing.</summary>
        public void BeginSearch()
        {
            var t = State.selected ?? State.mine[0];
            Search.Reset(t);
            Router.Go("analyzing");
            Actions.StartSearch(t.name.ToLowerInvariant());
        }

        /// <summary>Exit (X) during a search: stop everything and show "Not found yet".</summary>
        public void ExitSearch()
        {
            Actions.StopSearch();
            Router.Go("notfound");
        }

        /// <summary>The search is over with a result the user picked by hand ("Where did you find it?").</summary>
        public void FoundManually(string place)
        {
            Actions.StopSearch();
            Search.endTime = Time.unscaledTime;
            Search.foundAt = place;
            RecordFound();
            Router.Go("found");
        }

        // ---------- ISearchUI (called by SearchController) ----------

        public void ShowItemCard(ItemProfile profile)
        {
            Search.profile = profile;
            analyzing.ProfileReady();
        }

        public void ShowStatus(string message)
        {
            Search.status = message;
            scan.Refresh();
        }

        public void HideStatus()
        {
            Search.status = null;
            scan.Refresh();
        }

        public void SetReadiness(float progress01)
        {
            Search.readiness = progress01;
            mapping.Refresh();
        }

        public void SetScanEnabled(bool on)
        {
            Search.scanEnabled = on;
            mapping.Refresh();
            scan.Refresh();
        }

        public void SetProgress(int checkedCount, int total)
        {
            Search.checkedCount = checkedCount;
            Search.total = total;
            scan.Refresh();
        }

        public void ShowSpotPanel(string label, string tip, float dwell01, bool manualButtons)
        {
            Search.panelOn = true;
            Search.panelLabel = label;
            Search.panelTip = tip;
            Search.panelDwell = dwell01;
            Search.panelManual = manualButtons;
            scan.Refresh();
        }

        public void HideSpotPanel()
        {
            if (!Search.panelOn) return;
            Search.panelOn = false;
            scan.Refresh();
        }

        public void ShowCheckedToast(string label) => scan.ShowCheckedToast(label);

        public void ShowMatch(string label)
        {
            if (Router.Current != scan) Router.Go("scan");
            scan.ShowMatch(label);
        }

        public void ShowFound(string label)
        {
            Search.endTime = Time.unscaledTime;
            Search.foundAt = label;
            RecordFound();
            Router.Go("found");
        }

        public void ShowSummary(SearchSession session) => Search.session = session;

        /// <summary>Like the prototype's summary: log the search in History and update the thing's last-seen place.</summary>
        void RecordFound()
        {
            var t = Search.thing;
            if (t == null) return;
            string place = Cap(Search.foundAt ?? "");
            State.history.Insert(0, new HistoryEntry
            {
                name = t.name, icon = t.icon, color = t.color, oneTime = t.oneTime, ok = true,
                when = "Just now", result = "Found · " + place,
            });
            if (!t.oneTime)
            {
                t.last = place;
                t.lastTime = "Just now";
                t.finds++;
            }
        }

        public static string Cap(string s) => string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s.Substring(1);
    }
}
