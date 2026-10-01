using System;
using UnityEngine;

namespace FindMe.UI
{
    /// <summary>
    /// The UI lead's side of the contract: implements <see cref="ISearchUI"/> so SearchController can drive the screens,
    /// and forwards button presses back to SearchController. Lives on the UIRoot prefab.
    /// All 11 screens are real. Every ISearchUI method is implemented.
    /// </summary>
    public class UIManager : MonoBehaviour, ISearchUI
    {
        public UITheme theme;
        public ScreenRouter router;
        public InputScreen inputScreen;
        public AnalyzingScreen analyzingScreen;
        public ItemCardScreen itemCardScreen;
        public ReadinessScreen readinessScreen;
        public ScanHudScreen hudScreen;
        public SpotPanel spotPanel;
        public MatchSheet matchSheet;
        public AllCheckedSheet allCheckedSheet;
        public FoundScreen foundScreen;
        public SummaryScreen summaryScreen;
        public UIToast toast;

        [Tooltip("Optional. Left empty in UI_Test; in the Main scene it is found automatically.")]
        public SearchController search;

        // Raised instead of calling SearchController when there is none (UI_Test's FakeSearchDriver listens to these).
        public event Action<string> StartRequested;
        public event Action ScanStartRequested, ScanRequested, FoundRequested, StopRequested, CheckedRequested;
        public event Action<bool> MatchAnswered;
        public event Action UndoRequested;

        ItemProfile profile;
        bool scanning;        // the user pressed "Start scanning" on the item card
        bool scanEnabled;     // last SetScanEnabled value from SearchController
        string lastStatus;
        bool allCheckedDismissed;     // user tapped "Not now": don't show the sheet again until something changes
        int lastChecked = -1, lastTotal = -1;

        const string AllCheckedMessage = "All spots checked. Scan another area?";   // SearchController's exact text

        void Start()
        {
            if (search == null) search = FindAnyObjectByType<SearchController>();

            inputScreen.onContinue += text =>
            {
                if (search != null) search.StartSearch(text);
                else StartRequested?.Invoke(text);
            };
            itemCardScreen.onBack += GoHome;
            itemCardScreen.onStart += () =>
            {
                scanning = true;
                ScanStartRequested?.Invoke();
                // Already ready (AR was quick)? Go straight to the HUD; otherwise show the "move your phone" card.
                if (scanEnabled) ShowHud(); else router.Show("Readiness");
            };
            readinessScreen.onCancel += Stop;

            hudScreen.onScan += RequestScan;
            hudScreen.onFound += () => { if (search != null) search.MarkFound(); else FoundRequested?.Invoke(); };
            hudScreen.onStop += Stop;

            // Spot panel's manual buttons and the match sheet go straight to SearchController (or the test driver).
            spotPanel.onChecked += () => { if (search != null) search.MarkChecked(); else CheckedRequested?.Invoke(); };
            spotPanel.onFound += () => { if (search != null) search.MarkFound(); else FoundRequested?.Invoke(); };
            matchSheet.onAnswer += yes => { if (search != null) search.ConfirmMatch(yes); else MatchAnswered?.Invoke(yes); };

            allCheckedSheet.onScan += RequestScan;
            allCheckedSheet.onNotNow += () => allCheckedDismissed = true;
            foundScreen.onDone += () => router.Show("Summary");
            summaryScreen.onHome += Stop;

            router.Show("Input");
        }

        void RequestScan()
        {
            allCheckedSheet.Hide();
            if (search != null) search.ScanArea(); else ScanRequested?.Invoke();
        }

        // ---------- navigation helpers ----------
        // Stop the search and return to the start screen (HUD stop button, readiness cancel).
        void Stop()
        {
            StopRequested?.Invoke();
            if (search != null) search.StopSearch();
            GoHome();
        }

        void GoHome()
        {
            spotPanel.Hide();
            matchSheet.Hide();
            allCheckedSheet.Hide();
            allCheckedDismissed = false;
            lastChecked = lastTotal = -1;
            if (search != null && !scanning) search.StopSearch();   // Back on the item card also ends the search
            scanning = false;
            lastStatus = null;
            hudScreen.ResetHud();
            inputScreen.ResetScreen();
            router.Show("Input");
        }

        void ShowHud()
        {
            hudScreen.SetItem(profile);
            hudScreen.SetStatus(lastStatus);
            router.Show("Hud");
        }

        // ---------- ISearchUI: screens 1-3 ----------
        public void ShowStatus(string message)
        {
            // SearchController says "Getting to know your {item}…" while Gemini profiles the item: that's the Analyzing screen.
            if (message != null && message.StartsWith("Getting to know"))
            {
                router.Show("Analyzing");        // show first: a screen must be active before it can run its animation
                analyzingScreen.Begin(message);
                return;
            }
            lastStatus = message;
            hudScreen.SetStatus(message);
            // Every spot looked at and not found: offer to scan elsewhere (once, until the numbers change).
            if (message == AllCheckedMessage && !allCheckedDismissed && router.CurrentName == "Hud") allCheckedSheet.Show();
        }

        public void HideStatus()
        {
            lastStatus = null;
            hudScreen.SetStatus(null);
        }

        public void ShowItemCard(ItemProfile p)
        {
            profile = p;
            analyzingScreen.Finish();
            itemCardScreen.Show(p);
            router.Show("ItemCard");
        }

        // ---------- ISearchUI: screens 4-5 ----------
        public void SetReadiness(float progress01) => readinessScreen.SetProgress(progress01);

        public void SetScanEnabled(bool on)
        {
            scanEnabled = on;
            hudScreen.SetScanEnabled(on);
            if (!on) allCheckedSheet.Hide();   // a scan started (or a sheet took over)
            // AR just became ready while the readiness card is up: hand over to the HUD.
            if (on && scanning && router.CurrentName == "Readiness") ShowHud();
        }

        public void SetProgress(int checkedCount, int total)
        {
            hudScreen.SetProgress(checkedCount, total);
            // New numbers (an Undo, a new scan) mean the "all checked" situation is over, so reset the sheet.
            if (checkedCount != lastChecked || total != lastTotal)
            {
                lastChecked = checkedCount; lastTotal = total;
                allCheckedDismissed = false;
                allCheckedSheet.Hide();
            }
        }

        // ---------- ISearchUI: screens 6-8 ----------
        public void ShowSpotPanel(string label, string tip, float dwell01, bool manualButtons) =>
            spotPanel.Show(label, tip, dwell01, manualButtons);

        public void HideSpotPanel() => spotPanel.Hide();

        public void ShowMatch(string label)
        {
            spotPanel.Hide();   // the match sheet takes its place
            matchSheet.Show(profile?.name ?? "item", label);
        }

        // ---------- ISearchUI: screens 9-10 ----------
        public void ShowFound(string label)
        {
            spotPanel.Hide(); matchSheet.Hide(); allCheckedSheet.Hide();
            router.Show("Found");               // show first: the confetti and bounce need an active screen
            foundScreen.Play(profile?.name ?? "item", label);
        }

        // SearchController calls this right after ShowFound; the summary is shown when the user taps "Nice, done".
        public void ShowSummary(SearchSession session) => summaryScreen.Set(session, profile?.name ?? "item");

        // The toast already exists in the kit, so this one is real.
        public void ShowCheckedToast(string label)
        {
            string text = string.IsNullOrEmpty(label) ? "Checked" : char.ToUpperInvariant(label[0]) + label.Substring(1) + " checked";
            toast.Show(text, theme.soda, 4f, "Undo", () => { if (search != null) search.UndoChecked(); else UndoRequested?.Invoke(); },
                "Tap Undo if you missed it");
        }
    }
}
