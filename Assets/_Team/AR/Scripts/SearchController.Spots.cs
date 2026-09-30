using UnityEngine;

namespace FindMe
{
    /// <summary>
    /// SearchController, part 2: everything about individual spots once cues exist.
    /// Dwell → automatic Gemini check → cue state + UI; manual buttons; Undo; "Is this it?"; Found.
    /// </summary>
    public partial class SearchController
    {
        [Header("Checks")]
        [SerializeField] SpotChecker checker;
        [Tooltip("After this many unclear answers on one spot, offer the manual buttons.")]
        [SerializeField] int maxUnclearAttempts = 3;

        /// <summary>Last check photo + answer (for DebugOverlay: shows where Gemini saw the item).</summary>
        public Snapshot LastCheck { get; private set; }
        public SpotCheckResult LastCheckResult { get; private set; }

        SpotCue candidate;          // the cue waiting for "Is this your wallet?"
        GameObject candidateMarker; // mint dot where Gemini saw the item
        SpotCue lastChecked;        // for Undo
        SpotCue manualCue;          // the cue whose check failed: offer manual buttons for it

        void AwakeSpots()
        {
            checker.Completed += OnCheckCompleted;
            checker.Failed += OnCheckFailed;
            checker.CancelledByPause += PutBack;
        }

        // ---------- automatic check ----------

        /// <summary>The user held the phone steady on a cue for 2 s: ask Gemini about it.</summary>
        void OnDwellCompleted(SpotCue cue)
        {
            if (State != SearchState.Searching || cue == null) return;
            cue.StateBeforeCheck = cue.State == SpotState.NeedsOpen ? SpotState.NeedsOpen : SpotState.ToCheck;
            if (!checker.TryCheck(cue, Profile)) return;    // one check at a time: ignore extra triggers
            cue.SetState(SpotState.Checking);
            focus.CanTrigger = false;
            RefreshCues();
        }

        void OnCheckCompleted(SpotCue cue, SpotCheckResult r, Snapshot snap)
        {
            focus.CanTrigger = true;
            SetLastCheck(snap, r);
            // The search may have moved on (reset, stopped, another cue being confirmed) while we waited.
            if (cue == null || State != SearchState.Searching || cue.State != SpotState.Checking) return;

            if (r.itemVisible)
            {
                ShowCandidate(cue, r, snap.pose);
            }
            else if (r.spotFullyVisible)
            {
                SetChecked(cue);
                UI?.ShowCheckedToast(cue.Label);            // the UI offers Undo for 4 s
            }
            else
            {
                cue.Attempts++;
                cue.SetState(SpotState.NeedsOpen, string.IsNullOrWhiteSpace(r.tip) ? "Open it up" : r.tip);
                if (cue.Attempts >= maxUnclearAttempts) manualCue = cue;
            }
            RefreshCues();
        }

        void OnCheckFailed(SpotCue cue, string error)
        {
            focus.CanTrigger = true;
            Debug.LogWarning($"[Search] Check failed: {error}");
            if (cue == null || State != SearchState.Searching) return;
            PutBack(cue);
            manualCue = cue;
            UI?.ShowStatus("Hmm, I couldn't see clearly. Try again or mark it yourself.");   // never a raw error
        }

        /// <summary>A check was cancelled or failed: the cue goes back to how it was before.</summary>
        void PutBack(SpotCue cue)
        {
            focus.CanTrigger = true;
            if (cue != null && cue.State == SpotState.Checking) cue.SetState(cue.StateBeforeCheck);
            RefreshCues();
        }

        /// <summary>Stops a running check (a new scan takes priority; plan Section 11).</summary>
        void CancelCheck() => PutBack(checker.Cancel());

        void SetChecked(SpotCue cue)
        {
            cue.SetState(SpotState.Checked);
            cue.Attempts = 0;
            cue.CheckedAt = Time.time;
            lastChecked = cue;
            if (manualCue == cue) manualCue = null;
        }

        // ---------- "Is this your wallet?" ----------

        void ShowCandidate(SpotCue cue, SpotCheckResult r, CapturePose pose)
        {
            candidate = cue;
            cue.SetState(SpotState.Candidate);
            if (r.HasItemBox) candidateMarker = placer.PlaceMarker(r.itemBox, pose, cue.transform.position);
            SetState(SearchState.FoundConfirm);
            UI?.ShowMatch(cue.Label);
        }

        /// <summary>Answer to "Is this your wallet?". Yes → Found. No → back to searching, and Gemini is told.</summary>
        public void ConfirmMatch(bool yes)
        {
            if (State != SearchState.FoundConfirm || candidate == null) return;
            var cue = candidate;
            candidate = null;
            if (candidateMarker != null) Destroy(candidateMarker);

            if (yes)
            {
                ConfirmFound(cue);
                return;
            }
            // Next check of this spot tells Gemini about the false match, so it doesn't say "yes" to it again.
            cue.Note = $"The user said a previous match here was NOT their {Profile.name}. Ignore that object.";
            cue.SetState(SpotState.ToCheck);
            SetState(SearchState.Searching);
            UI?.ShowStatus("Okay, let's keep looking!");
            RefreshCues();
        }

        void ConfirmFound(SpotCue cue)
        {
            CancelCheck();
            cue.SetState(SpotState.Found);
            cue.CheckedAt = Time.time;
            foreach (var c in placer.Cues) if (c != null && c != cue) c.SetFade(0.3f);   // the found one stands out
            SetState(SearchState.Found);
            RefreshCues();
            UI?.ShowStatus($"You found your {Profile.name}!");
            UI?.ShowFound(cue.Label);
            RecordSession(cue);
            UI?.ShowSummary(Session);
        }

        /// <summary>Freeze the session at the moment of finding: which spots, what state, when.</summary>
        void RecordSession(SpotCue found)
        {
            if (Session == null) return;
            Session.foundLabel = found.Label;
            Session.foundTime = Time.time;
            Session.spots.Clear();
            foreach (var c in placer.Cues)
                if (c != null)
                    Session.spots.Add(new SearchSession.SpotRecord { label = c.Label, state = c.State, checkedAt = c.CheckedAt });
        }

        /// <summary>Dev: check the cue being looked at right now, without the 2 s hold.</summary>
        public void DevForceCheck()
        {
            if (focus.Focused != null) OnDwellCompleted(focus.Focused);
        }

        // ---------- buttons ----------

        /// <summary>The cue a manual button applies to: the one being looked at, else the one whose check failed.</summary>
        SpotCue ManualTarget => focus.Focused != null ? focus.Focused : manualCue;

        /// <summary>"✓ Checked" button.</summary>
        public void MarkChecked()
        {
            SpotCue cue = ManualTarget;
            if (State != SearchState.Searching || cue == null || !cue.IsOpen) return;
            if (cue.State == SpotState.Checking) CancelCheck();
            SetChecked(cue);
            RefreshCues();
        }

        /// <summary>"Found it" button: the user found the item themselves (no confirmation needed).</summary>
        public void MarkFound()
        {
            SpotCue cue = ManualTarget;
            if (State != SearchState.Searching || cue == null) return;
            ConfirmFound(cue);
        }

        /// <summary>Undo on the "Checked" toast: the last checked cue becomes "to check" again.</summary>
        public void UndoChecked()
        {
            if (lastChecked == null || lastChecked.State != SpotState.Checked) return;
            lastChecked.SetState(SpotState.ToCheck);
            lastChecked.CheckedAt = -1f;
            lastChecked = null;
            RefreshCues();
        }

        void SetLastCheck(Snapshot snap, SpotCheckResult r)
        {
            if (LastCheck != null) Destroy(LastCheck.texture);
            LastCheck = snap;
            LastCheckResult = r;
        }

        /// <summary>Everything spot-related back to empty (new search / stop).</summary>
        void ResetSpots()
        {
            checker.Cancel();
            focus.CanTrigger = true;
            candidate = lastChecked = manualCue = null;
            if (candidateMarker != null) Destroy(candidateMarker);
            placer.ClearCues();
        }
    }
}
