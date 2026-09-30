using System;
using System.Threading;
using UnityEngine;

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
        [Tooltip("Any component implementing ISearchUI (UIManager, or DevUI for now). Found automatically if empty.")]
        [SerializeField] MonoBehaviour ui;

        public SearchState State { get; private set; } = SearchState.Idle;
        public ItemProfile Profile { get; private set; }
        public event Action<SearchState> StateChanged;

        ISearchUI UI => ui as ISearchUI;
        CancellationTokenSource cts;   // cancels whatever Gemini request belongs to the current search

        void Awake()
        {
            if (ui == null) ui = FindSearchUI();
        }

        void OnDestroy() => cts?.Cancel();

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
