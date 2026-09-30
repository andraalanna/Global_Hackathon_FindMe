using System;
using System.Threading;
using UnityEngine;

namespace FindMe
{
    /// <summary>
    /// Runs the automatic "is it here?" check for one cue (plan Phase 8): 512 px snapshot → Gemini CheckSpot.
    /// Only ONE check in flight at a time (heat + cost); extra triggers are ignored.
    /// It reports the answer; SearchController decides what the answer means for the cue and the UI.
    /// </summary>
    public class SpotChecker : MonoBehaviour
    {
        [SerializeField] SnapshotCapture snapshot;
        [SerializeField] GeminiServiceProvider gemini;
        [SerializeField] int checkWidth = 512;
        [Tooltip("After an answer, the same cue can't be checked again for this long (s).")]
        [SerializeField] float cooldownSeconds = 1.5f;

        public bool Busy => Current != null;
        public SpotCue Current { get; private set; }

        /// <summary>Answer arrived. The receiver owns the Snapshot (destroy its texture when done).</summary>
        public event Action<SpotCue, SpotCheckResult, Snapshot> Completed;
        /// <summary>Timeout, network error or bad answer (not a cancel).</summary>
        public event Action<SpotCue, string> Failed;

        CancellationTokenSource cts;

        /// <summary>Starts a check. False if one is already running (the caller should leave the cue alone).</summary>
        public bool TryCheck(SpotCue cue, ItemProfile item)
        {
            if (Busy || cue == null || snapshot.IsBusy) return false;
            Current = cue;
            cts = new CancellationTokenSource();
            var ct = cts.Token;
            StartCoroutine(snapshot.Capture(checkWidth, false, snap => Run(cue, item, snap, ct)));
            return true;
        }

        /// <summary>
        /// Stops the running check without reporting it (a new scan, a reset, or the app going to background).
        /// Returns the cue that was being checked so the caller can put it back.
        /// </summary>
        public SpotCue Cancel()
        {
            var cue = Current;
            cts?.Cancel();
            Current = null;
            return cue;
        }

        async void Run(SpotCue cue, ItemProfile item, Snapshot snap, CancellationToken ct)
        {
            if (snap == null)   // camera was busy (e.g. a scan grabbed it first)
            {
                Done(cue, ct);
                if (!ct.IsCancellationRequested) Failed?.Invoke(cue, "camera busy");
                return;
            }

            SpotCheckResult result;
            try
            {
                result = await gemini.Service.CheckSpot(snap.jpg, item, cue.Label, cue.Note, ct);
                if (result == null) throw new Exception("empty answer");
            }
            catch (Exception e)
            {
                Destroy(snap.texture);
                bool cancelled = e is OperationCanceledException || ct.IsCancellationRequested;
                Done(cue, ct);
                if (!cancelled) Failed?.Invoke(cue, e.Message);
                return;
            }

            if (ct.IsCancellationRequested) { Destroy(snap.texture); return; }   // answer came after a cancel: drop it
            Done(cue, ct);
            Completed?.Invoke(cue, result, snap);
        }

        void Done(SpotCue cue, CancellationToken ct)
        {
            if (!ct.IsCancellationRequested) Current = null;   // a cancel already cleared it (maybe for a newer check)
            if (cue != null) cue.CooldownUntil = Time.time + cooldownSeconds;
        }

        // Plan Section 11: pause checks when the app goes to the background.
        void OnApplicationPause(bool paused)
        {
            if (paused && Busy) CancelledByPause?.Invoke(Cancel());
        }

        /// <summary>The app was backgrounded mid-check; the cue needs putting back.</summary>
        public event Action<SpotCue> CancelledByPause;
    }
}
