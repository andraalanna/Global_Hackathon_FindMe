using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace FindMe
{
    public enum SearchState { Idle, Profiling, WaitingForAR, ReadyToScan, Scanning, Searching, FoundConfirm, Found }

    /// <summary>
    /// The brain (plan Section 2): owns the search state machine and connects Gemini, AR and UI.
    /// UI buttons call the public methods; this class calls back into the UI through <see cref="ISearchUI"/>.
    /// </summary>
    public partial class SearchController : MonoBehaviour
    {
        [SerializeField] GeminiServiceProvider gemini;
        [SerializeField] ARReadiness readiness;
        [Tooltip("Reset on every new search, so the readiness meter really starts from zero.")]
        [SerializeField] ARSession arSession;
        [SerializeField] SnapshotCapture snapshot;
        [SerializeField] SpotPlacer placer;
        [SerializeField] PlaneVisibility planes;
        [SerializeField] OffscreenIndicator offscreen;
        [SerializeField] SpotFocus focus;
        [Tooltip("Any component implementing ISearchUI (UIManager, or DevUI for now). Found automatically if empty.")]
        [SerializeField] MonoBehaviour ui;

        [Header("Scan")]
        [SerializeField] int scanWidth = 768;          // px sent to Gemini Call 2
        [SerializeField] int maxSpotsPerScan = 5;
        [Tooltip("While searching, quietly re-scan this often (s) so cues follow things the user moves (a bag put down " +
                 "elsewhere). Only existing cues move; no new ones appear. 0 = off. Each one is a Gemini call.")]
        [SerializeField] float quietRescanSeconds = 8f;

        float lastScanTime;
        bool quietScanRunning;

        public SearchState State { get; private set; } = SearchState.Idle;
        public ItemProfile Profile { get; private set; }
        /// <summary>This search's history, for the summary ("Found in 2m 41s · 4 spots checked").</summary>
        public SearchSession Session { get; private set; }
        public event Action<SearchState> StateChanged;

        /// <summary>Last scan's snapshot (image + pose) and the spots Gemini found in it. For DebugOverlay.</summary>
        public Snapshot LastScan { get; private set; }
        public IReadOnlyList<HidingSpot> LastSpots => lastSpots;

        ISearchUI UI => ui as ISearchUI;
        CancellationTokenSource cts;   // cancels whatever Gemini request belongs to the current search
        List<HidingSpot> lastSpots = new();
        SearchState stateBeforeScan;

        void Awake()
        {
            // Privacy + battery: the camera (AR session) is off until a search starts, and off again when it stops.
            SetCamera(false);
            if (ui == null) ui = FindSearchUI();
            focus.enabled = false;                      // only runs while Searching
            focus.DwellCompleted += OnDwellCompleted;
            AwakeSpots();
        }

        void OnDestroy()
        {
            cts?.Cancel();
            if (LastScan != null) Destroy(LastScan.texture);
        }

        void Update()
        {
            if (State == SearchState.WaitingForAR)
            {
                UI?.SetReadiness(readiness.Progress01);
                if (readiness.IsReady) EnterReadyToScan();  // IsReady already includes the "for 1 s" hold
            }
            else if (State == SearchState.Searching)
            {
                UpdateSpotPanel();
                UpdateQuietRescan();
            }
        }

        /// <summary>
        /// The spot panel follows the cue being checked or looked at. Manual buttons appear for a cue whose
        /// check failed or stayed unclear, so the user is never stuck waiting on the AI.
        /// </summary>
        void UpdateSpotPanel()
        {
            // Only ONE bright cue while holding/checking: the one being checked. Others fade back, so it's
            // obvious which spot the automatic check is about.
            SpotCue target = checker.Busy ? checker.Current : focus.Focused;
            foreach (var cue in placer.Cues)
                if (cue != null) cue.SetFade(target == null || cue == target ? 1f : 0.25f);

            if (checker.Busy)
            {
                UI?.ShowSpotPanel(checker.Current.Label, "Looking…", 1f, false);
                return;
            }
            SpotCue c = focus.Focused;
            if (c != null)
            {
                bool manual = c == manualCue || c.Attempts >= maxUnclearAttempts;
                UI?.ShowSpotPanel(c.Label, c.State == SpotState.NeedsOpen ? c.Tip : "Hold still…", focus.Dwell01, manual);
            }
            else if (manualCue != null && manualCue.IsOpen)
                UI?.ShowSpotPanel(manualCue.Label, "Checked it yourself?", 0f, true);
            else
                UI?.HideSpotPanel();
        }

        // ---------- Called by UI ----------

        /// <summary>Idle → Profiling → WaitingForAR. Starting again mid-search resets everything.</summary>
        public void StartSearch(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            BeginSearch(text.Trim(), null);
        }

        /// <summary>
        /// "Use photo": the user points the camera at the item's twin, or a photo of it on another screen.
        /// A small photo is taken; Gemini describes the item from it, and it rides along with every scan/check.
        /// </summary>
        public void StartSearchWithPhoto(string text)
        {
            if (snapshot.IsBusy) return;
            SetCamera(true);                       // the photo needs the camera
            UI?.ShowStatus("Hold the item (or its photo) in view…");
            StartCoroutine(snapshot.Capture(384, true, snap =>
            {
                if (snap == null) return;
                Destroy(snap.texture);
                BeginSearch(string.IsNullOrWhiteSpace(text) ? "" : text.Trim(), snap.jpg);
            }));
        }

        async void BeginSearch(string text, byte[] photo)
        {
            var ct = NewSearchToken();
            ResetSpots();                // a new search starts from an empty room
            SetCamera(true);             // camera on only from here (it was off on the app's other screens)
            RestartAR();
            Session = new SearchSession { item = text, startTime = Time.time };
            planes.SetVisible(true);
            RefreshCues();

            SetState(SearchState.Profiling);
            UI?.SetScanEnabled(false);
            UI?.ShowStatus(photo != null ? "Looking at your photo…" : $"Getting to know your {text}…");

            ItemProfile profile = null;
            try
            {
                profile = photo != null
                    ? await gemini.Service.GetItemProfileFromPhoto(photo, text, ct)
                    : await gemini.Service.GetItemProfile(text, ct);
            }
            catch (OperationCanceledException) { return; }       // a newer search replaced this one
            catch (Exception e) { Debug.LogWarning($"[Search] Profile failed, using fallback: {e.Message}"); }

            // Plan: if profiling fails, carry on with the typed text as the name and a default size.
            profile ??= new ItemProfile();
            profile.FillDefaults(string.IsNullOrWhiteSpace(text) ? "item" : text);
            if (photo != null) profile.referenceJpg ??= photo;   // keep the photo even if profiling failed
            if (profile.ambiguous)
            {
                // A photo of a backpack next to a pineapple, and no words: guessing would search for the wrong thing.
                SetState(SearchState.Idle);
                UI?.ShowStatus("I see more than one thing. Type what you lost, or show only that item, then tap Use photo again.");
                return;
            }
            Profile = profile;
            UI?.ShowItemCard(profile);

            SetState(SearchState.WaitingForAR);
            UI?.ShowStatus("Move your phone slowly around the room");
        }

        /// <summary>
        /// Scan tap: snapshot (768 px) → Gemini FindSpots → spots sorted by likelihood → cues placed in 3D.
        /// Allowed when ready, and again while searching ("scan more areas").
        /// </summary>
        public void ScanArea()
        {
            if (State != SearchState.ReadyToScan && State != SearchState.Searching) return;
            CancelCheck();               // a scan takes priority over a running spot check
            if (snapshot.IsBusy) return;
            // Without tracking the camera pose is stale, so every cue from this scan would land in the wrong place.
            if (ARSession.state != ARSessionState.SessionTracking)
            {
                UI?.ShowStatus("I lost track of the room. Move your phone slowly, then try again.");
                return;
            }

            stateBeforeScan = State;
            lastScanTime = Time.time;
            if (Session != null) Session.scans++;
            SetState(SearchState.Scanning);
            UI?.SetScanEnabled(false);
            UI?.ShowStatus("Looking for hiding spots…");
            StartCoroutine(snapshot.Capture(scanWidth, true, OnScanCaptured));
        }

        async void OnScanCaptured(Snapshot snap)
        {
            // A new search may have started while the frame was being captured.
            if (snap == null || State != SearchState.Scanning)
            {
                if (snap != null) Destroy(snap.texture);
                if (State == SearchState.Scanning) EndScan(0);
                return;
            }
            SetLastScan(snap, new List<HidingSpot>());

            List<HidingSpot> spots;
            try
            {
                spots = await gemini.Service.FindSpots(snap.jpg, Profile, cts.Token);
            }
            catch (OperationCanceledException) { return; }
            catch (Exception e)
            {
                Debug.LogWarning($"[Search] FindSpots failed: {e.Message}");
                EndScan(0, "Hmm, I couldn't see clearly. Try again.");
                return;
            }

            // The item itself, out in the open? That's not a hiding place: it's the answer. Handle it separately.
            HidingSpot sighting = spots?.Find(s => s != null && s.isItem && s.Sanitize());
            spots?.RemoveAll(s => s == null || s.isItem);
            spots = SortAndCap(spots, maxSpotsPerScan);
            var shown = new List<HidingSpot>(spots);
            if (sighting != null) shown.Insert(0, sighting);
            SetLastScan(snap, shown);   // the pose stays with its spots: it's what the boxes are relative to

            SpotPlacer.PlaceResult placed;
            try
            {
                // Uses snap.pose, NOT the current camera: the boxes describe what the phone saw back then.
                placed = await placer.PlaceSpots(spots, snap.pose, snap.grid, cts.Token);
            }
            catch (OperationCanceledException) { return; }
            EndScan(placed.added, ScanMessage(placed));
            RefreshCues();

            if (sighting != null && State == SearchState.Searching)
            {
                SpotCue cue;
                try { cue = await placer.PlaceItemSighting(sighting, snap.pose, snap.grid); }
                catch (OperationCanceledException) { return; }
                ShowSighting(cue);
            }
        }

        /// <summary>
        /// Every few seconds while searching, if the phone is calm and nothing is being checked: take a silent photo
        /// (no flash) and ask for the spots again, only to move existing cues whose objects moved (or drop ones whose
        /// objects are gone). No status noise unless something moved; the item seen in the open still pops "There it is!".
        /// </summary>
        void UpdateQuietRescan()
        {
            if (quietRescanSeconds <= 0f || quietScanRunning || checker.Busy || snapshot.IsBusy) return;
            if (Time.time - lastScanTime < quietRescanSeconds) return;
            if (focus.Focused != null || !focus.IsSteady || ARSession.state != ARSessionState.SessionTracking) return;
            lastScanTime = Time.time;
            quietScanRunning = true;
            var ct = cts.Token;
            StartCoroutine(snapshot.Capture(scanWidth, false, snap => QuietScan(snap, ct)));
        }

        async void QuietScan(Snapshot snap, CancellationToken ct)
        {
            try
            {
                if (snap == null) return;
                if (State != SearchState.Searching) { Destroy(snap.texture); return; }
                List<HidingSpot> spots;
                try { spots = await gemini.Service.FindSpots(snap.jpg, Profile, ct); }
                catch (OperationCanceledException) { Destroy(snap.texture); return; }
                catch (Exception e)
                {
                    Debug.LogWarning($"[Search] Quiet re-scan failed: {e.Message}");
                    Destroy(snap.texture);
                    return;
                }
                if (State != SearchState.Searching) { Destroy(snap.texture); return; }

                HidingSpot sighting = spots?.Find(s => s != null && s.isItem && s.Sanitize());
                spots?.RemoveAll(s => s == null || s.isItem);
                spots = SortAndCap(spots, maxSpotsPerScan);
                SetLastScan(snap, spots);
                var placed = await placer.PlaceSpots(spots, snap.pose, snap.grid, ct, onlyExisting: true);
                if (placed.moved > 0)
                    UI?.ShowStatus(placed.moved == 1 ? "Something moved, so its marker moved with it." : $"{placed.moved} things moved; their markers followed.");
                RefreshCues();

                if (sighting != null && State == SearchState.Searching)
                    ShowSighting(await placer.PlaceItemSighting(sighting, snap.pose, snap.grid));
            }
            catch (OperationCanceledException) { }
            finally { quietScanRunning = false; }
        }

        /// <summary>Scanning → Searching if we have spots, else back to where we were.</summary>
        void EndScan(int newSpots, string failMessage = null)
        {
            bool haveSpots = newSpots > 0 || stateBeforeScan == SearchState.Searching;
            SetState(haveSpots ? SearchState.Searching : stateBeforeScan);
            UI?.SetScanEnabled(true);
            UI?.ShowStatus(failMessage ?? (newSpots > 0
                ? $"Found {newSpots} places to check"
                : "No hiding spots here. Try another part of the room."));
        }

        /// <summary>Status text for scans that found spots but added none (null = use the default text).</summary>
        static string ScanMessage(SpotPlacer.PlaceResult r)
        {
            if (r.added > 0) return r.overCap > 0 ? $"Found {r.added} more places. That's the most I can show at once." : null;
            if (r.overCap > 0) return "You have plenty of spots to check. Check some before scanning more.";
            if (r.merged > 0) return "I already marked these spots. Try another part of the room.";
            return null;
        }

        void SetLastScan(Snapshot snap, List<HidingSpot> spots)
        {
            if (LastScan != null && LastScan != snap) Destroy(LastScan.texture);
            LastScan = snap;
            lastSpots = spots;
        }

        /// <summary>Drops broken spots, sorts by likelihood (3 first; ties keep Gemini's order), keeps the top N.</summary>
        public static List<HidingSpot> SortAndCap(IEnumerable<HidingSpot> spots, int max) =>
            (spots ?? Enumerable.Empty<HidingSpot>())
                .Where(s => s != null && s.Sanitize())
                .OrderByDescending(s => s.likelihood)   // OrderBy is stable, so equal likelihoods keep their order
                .Take(max)
                .ToList();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>Dev only: jump from WaitingForAR to ReadyToScan without walking around (Inspector ⋮ menu).</summary>
        [ContextMenu("Dev: Skip AR readiness")]
        public void DevSkipReadiness()
        {
            if (State == SearchState.WaitingForAR) EnterReadyToScan();
        }
#endif

        /// <summary>
        /// After anything changes: pick the "next" cue (highlighted + off-screen arrow) and update the progress chip.
        /// </summary>
        void RefreshCues()
        {
            Vector3 camPos = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
            SpotCue next = State == SearchState.Searching ? placer.PickNext(camPos) : null;
            int done = 0, total = 0;
            foreach (var c in placer.Cues)
            {
                if (c == null) continue;
                c.SetNext(c == next);
                total++;
                if (c.State is SpotState.Checked or SpotState.Found) done++;
            }
            if (offscreen != null) offscreen.Target = next;
            UI?.SetProgress(done, total);

            // Every spot looked at and still not found: nudge toward scanning somewhere else.
            if (State == SearchState.Searching && total > 0 && next == null && !checker.Busy && !AnyOpen())
                UI?.ShowStatus("All spots checked. Scan another area?");
        }

        bool AnyOpen()
        {
            foreach (var c in placer.Cues) if (c != null && c.IsOpen) return true;
            return false;
        }

        /// <summary>
        /// Fresh start for the readiness meter: forget the planes ARCore found (ARSession.Reset) and the
        /// "moved / turned" totals. Otherwise a second search, or reopening the app, starts with a full meter.
        /// </summary>
        void RestartAR()
        {
            if (arSession != null) arSession.Reset();
            readiness.ResetReadiness();
        }

        /// <summary>
        /// Android "closing" usually only backgrounds the app: on return, everything is still in memory.
        /// If the user hadn't scanned yet, make them re-check the room from zero (tracking may have drifted too).
        /// Mid-search we keep the cues: re-scanning everything would cost more than it saves.
        /// </summary>
        void OnApplicationPause(bool paused)
        {
            if (paused || !(State is SearchState.WaitingForAR or SearchState.ReadyToScan)) return;
            RestartAR();
            SetState(SearchState.WaitingForAR);
            UI?.SetReadiness(0f);
            UI?.ShowStatus("Move your phone slowly around the room");
        }

        /// <summary>"Stop" button: cancel everything and go back to the start.</summary>
        public void StopSearch()
        {
            NewSearchToken();            // cancels Gemini requests of this search
            ResetSpots();
            planes.SetVisible(true);
            Profile = null;
            Session = null;
            SetState(SearchState.Idle);
            RefreshCues();
            UI?.SetScanEnabled(false);
            UI?.ShowStatus("Type what you lost");
            SetCamera(false);            // no search, no camera
        }

        /// <summary>
        /// Turns the camera on/off by enabling the AR session: disabled, ARKit/ARCore pause and release the camera
        /// (the phone's camera-in-use indicator goes off). Enabled, tracking resumes.
        /// </summary>
        void SetCamera(bool on)
        {
            if (arSession != null && arSession.enabled != on) arSession.enabled = on;
        }

        // ---------- State helpers ----------

        void EnterReadyToScan()
        {
            SetState(SearchState.ReadyToScan);
            UI?.SetReadiness(1f);
            UI?.SetScanEnabled(true);
            UI?.ShowStatus("Point at part of the room, then tap Scan");
        }

        void SetState(SearchState s)
        {
            if (State == s) return;
            Debug.Log($"[Search] {State} → {s}");
            State = s;
            if (s == SearchState.Searching) planes.SetVisible(false);   // from now on the cues are the star
            focus.enabled = s == SearchState.Searching;                 // no focusing while scanning / confirming
            UI?.SetScanEnabled(s is SearchState.ReadyToScan or SearchState.Searching);   // no Scan behind the "Is this it?" sheet
            if (s != SearchState.Searching) UI?.HideSpotPanel();
            StateChanged?.Invoke(s);
        }

        /// <summary>Cancels the previous search's requests and returns a token for the new one.</summary>
        CancellationToken NewSearchToken()
        {
            cts?.Cancel();
            cts = new CancellationTokenSource();
            return cts.Token;
        }

        static MonoBehaviour FindSearchUI()
        {
            foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude))
                if (mb is ISearchUI) return mb;
            Debug.LogWarning("[Search] No ISearchUI in the scene; UI calls are skipped.");
            return null;
        }
    }
}
