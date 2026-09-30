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
    public class SearchController : MonoBehaviour
    {
        [SerializeField] GeminiServiceProvider gemini;
        [SerializeField] ARReadiness readiness;
        [SerializeField] SnapshotCapture snapshot;
        [SerializeField] SpotPlacer placer;
        [SerializeField] PlaneVisibility planes;
        [Tooltip("Any component implementing ISearchUI (UIManager, or DevUI for now). Found automatically if empty.")]
        [SerializeField] MonoBehaviour ui;

        [Header("Scan")]
        [SerializeField] int scanWidth = 768;          // px sent to Gemini Call 2
        [SerializeField] int maxSpotsPerScan = 5;

        public SearchState State { get; private set; } = SearchState.Idle;
        public ItemProfile Profile { get; private set; }
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
            if (ui == null) ui = FindSearchUI();
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
        }

        // ---------- Called by UI ----------

        /// <summary>Idle → Profiling → WaitingForAR. Starting again mid-search resets everything.</summary>
        public async void StartSearch(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            text = text.Trim();
            var ct = NewSearchToken();
            placer.ClearCues();          // a new search starts from an empty room
            planes.SetVisible(true);

            SetState(SearchState.Profiling);
            UI?.SetScanEnabled(false);
            UI?.ShowStatus($"Getting to know your {text}…");

            ItemProfile profile = null;
            try
            {
                profile = await gemini.Service.GetItemProfile(text, ct);
            }
            catch (OperationCanceledException) { return; }       // a newer search replaced this one
            catch (Exception e) { Debug.LogWarning($"[Search] Profile failed, using fallback: {e.Message}"); }

            // Plan: if profiling fails, carry on with the typed text as the name and a default size.
            profile ??= new ItemProfile();
            profile.FillDefaults(text);
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
            if (snapshot.IsBusy) return;
            // Without tracking the camera pose is stale, so every cue from this scan would land in the wrong place.
            if (ARSession.state != ARSessionState.SessionTracking)
            {
                UI?.ShowStatus("I lost track of the room. Move your phone slowly, then try again.");
                return;
            }

            stateBeforeScan = State;
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

            spots = SortAndCap(spots, maxSpotsPerScan);
            SetLastScan(snap, spots);   // the pose stays with its spots: it's what the boxes are relative to

            SpotPlacer.PlaceResult placed;
            try
            {
                // Uses snap.pose, NOT the current camera: the boxes describe what the phone saw back then.
                placed = await placer.PlaceSpots(spots, snap.pose, cts.Token);
            }
            catch (OperationCanceledException) { return; }
            EndScan(placed.added, ScanMessage(placed));
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
