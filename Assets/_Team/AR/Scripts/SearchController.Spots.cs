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

            // The check photo is a close-up: if it boxed the spot, re-measure where the spot really is.
            if (r.HasSpotBox) placer.RelocateFromPhoto(cue, r.spotBox, snap);

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

        /// <summary>The scan photo already shows the item: point straight at it and ask.</summary>
        void ShowSighting(SpotCue cue)
        {
            if (cue == null || State != SearchState.Searching) return;
            CancelCheck();
            candidate = cue;
            cue.SetState(SpotState.Candidate);
            SetState(SearchState.FoundConfirm);
            UI?.ShowStatus("There it is! Is that it?");
            UI?.ShowMatch(cue.Label);
        }

        void ShowCandidate(SpotCue cue, SpotCheckResult r, CapturePose pose)
        {
            candidate = cue;
            cue.SetState(SpotState.Candidate);
            if (r.HasItemBox) candidateMarker = placer.PlaceMarker(r.itemBox, pose, cue.transform.position, LastCheck?.grid);
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
            if (cue.IsSighting)
            {
                // A sighting is only the item or nothing: remove it rather than turn it into a hiding place.
                placer.RemoveCue(cue);
                SetState(SearchState.Searching);
                UI?.ShowStatus("Okay, let's keep looking!");
                RefreshCues();
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
            string place = PlaceOf(cue);
            UI?.ShowStatus($"You found your {Profile.name}!");
            UI?.ShowFound(place);
            RecordSession(place);
            UI?.ShowSummary(Session);
        }

        /// <summary>Is there a cue "I found it" would land on (the one looked at, a failed one, or one pointed at)?</summary>
        public bool HasFoundTarget => State == SearchState.Searching && (ManualTarget != null || CueNearAim(includeChecked: true) != null);

        /// <summary>"I found it" with no marked spot anywhere near: the search still ends as found (place unknown).</summary>
        void FoundWithoutSpot()
        {
            CancelCheck();
            SetState(SearchState.Found);
            RefreshCues();
            UI?.ShowStatus($"You found your {Profile?.name ?? "item"}!");
            UI?.ShowFound(null);
            RecordSession(null);
            UI?.ShowSummary(Session);
        }

        /// <summary>
        /// Where the item was, in words, for "Found it!" and the summary. A hiding spot names itself ("drawer").
        /// The item seen out in the open is labelled with the item's own name ("wallet"), so then the place comes
        /// from Gemini's evidence ("black wallet on the desk next to the laptop" → "on the desk next to the laptop"),
        /// else the nearest other marked spot ("near the bag"). Null = unknown.
        /// </summary>
        string PlaceOf(SpotCue cue)
        {
            string item = (Profile?.name ?? "").ToLowerInvariant().Trim();
            string label = (cue.Label ?? "").ToLowerInvariant().Replace("maybe ", "").Trim();
            bool isItem = cue.IsSighting || (item.Length > 0 && label.Length > 0 && (label.Contains(item) || item.Contains(label)));
            if (!isItem) return cue.Label;
            string fromEvidence = PlacePhrase(cue.Seen);
            if (fromEvidence != null) return fromEvidence;
            SpotCue near = null;
            float best = 2f;
            foreach (var c in placer.Cues)
            {
                if (c == null || c == cue || c.IsSighting) continue;
                float d = Vector3.Distance(c.transform.position, cue.transform.position);
                if (d < best) { best = d; near = c; }
            }
            return near != null ? "near the " + near.Label : null;
        }

        /// <summary>"black wallet on the desk next to the laptop" → "on the desk next to the laptop" (null if no place in it).</summary>
        public static string PlacePhrase(string evidence)
        {
            if (string.IsNullOrWhiteSpace(evidence)) return null;
            string s = " " + evidence.Trim().TrimEnd('.') + " ";
            string[] preps = { " on ", " in ", " inside ", " under ", " beneath ", " behind ", " next to ", " beside ", " between ", " near ", " by " };
            int at = -1;
            foreach (var p in preps)
            {
                int i = s.IndexOf(p, System.StringComparison.OrdinalIgnoreCase);
                if (i >= 0 && (at < 0 || i < at)) at = i;
            }
            return at < 0 ? null : s.Substring(at + 1).Trim();
        }

        /// <summary>Freeze the session at the moment of finding: which spots, what state, when.</summary>
        void RecordSession(string place)
        {
            if (Session == null) return;
            Session.foundLabel = place ?? "";
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

        /// <summary>
        /// "Check here" button: the user points the phone at the real hiding place and taps. Faster than holding
        /// still on a cue, and it works even if the cue drifted: the cue moves to what's in the middle of the
        /// screen (the user's aim beats our estimate), then gets checked right away.
        /// </summary>
        public void CheckHere()
        {
            if (State != SearchState.Searching || checker.Busy) return;
            SpotCue cue = focus.Focused != null ? focus.Focused : CueNearAim();
            // Nothing open there? The user may be pointing at a spot that was already checked, after moving things
            // (lifting papers, opening a bag): that's a deliberate "look again".
            if (cue == null) cue = CueNearAim(includeChecked: true);
            if (cue == null)
            {
                UI?.ShowStatus("Point at one of the marked spots, then tap Check here.");
                return;
            }
            if (cue.State == SpotState.Checked) Reopen(cue);
            placer.TryRelocateToScreenCenter(cue);
            cue.CooldownUntil = 0f;
            OnDwellCompleted(cue);
        }

        /// <summary>
        /// "Look again" at a checked spot: back to "to check", with a note so Gemini looks extra carefully
        /// (things were moved; the item can be half under something, or hard to see like clear glasses).
        /// </summary>
        void Reopen(SpotCue cue)
        {
            cue.SetState(SpotState.ToCheck);
            cue.CheckedAt = -1f;
            if (lastChecked == cue) lastChecked = null;
            string again = $"The user moved things at this spot and wants a careful second look. The {Profile?.name ?? "item"} " +
                           "may be partly hidden, under or between other things, or transparent.";
            cue.Note = string.IsNullOrEmpty(cue.Note) ? again : cue.Note + " " + again;
            UI?.ShowStatus($"Looking at the {cue.Label} again…");
            RefreshCues();
        }

        /// <summary>Labels of all marked spots in this search (for the UI's "Where did you find it?" list).</summary>
        public System.Collections.Generic.List<string> SpotLabels()
        {
            var list = new System.Collections.Generic.List<string>();
            foreach (var c in placer.Cues) if (c != null && !list.Contains(c.Label)) list.Add(c.Label);
            return list;
        }

        /// <summary>
        /// "I found it" at a marked spot, picked by the user (e.g. under papers the AI had already checked):
        /// ends the search on that cue exactly like Found it on the cue. False if there is no such spot.
        /// </summary>
        public bool MarkFoundAt(string label)
        {
            if (!(State is SearchState.Searching or SearchState.FoundConfirm)) return false;
            foreach (var c in placer.Cues)
            {
                if (c == null || !string.Equals(c.Label, label, System.StringComparison.OrdinalIgnoreCase)) continue;
                if (candidate != null && candidate != c && candidate.State == SpotState.Candidate) candidate.SetState(SpotState.ToCheck);
                candidate = null;                                   // a pending "Is this it?" is answered by the user's pick
                if (candidateMarker != null) Destroy(candidateMarker);
                ConfirmFound(c);
                return true;
            }
            return false;
        }

        /// <summary>The open cue closest to what the user is pointing at (or, if nothing is measured there, by angle).</summary>
        SpotCue CueNearAim(bool includeChecked = false)
        {
            Transform cam = Camera.main.transform;
            bool haveAim = placer.TryScreenCenterPoint(out Vector3 aim);
            SpotCue best = null;
            float bestScore = float.MaxValue;
            foreach (var c in placer.Cues)
            {
                bool usable = c != null && (c.IsOpen || (includeChecked && c.State == SpotState.Checked));
                if (!usable || c.State == SpotState.Checking) continue;
                float score = haveAim ? Vector3.Distance(aim, c.transform.position)          // metres
                                      : Vector3.Angle(cam.forward, c.transform.position - cam.position);   // degrees
                if (score < bestScore) { best = c; bestScore = score; }
            }
            // Nothing marked anywhere near the aim: don't guess which spot the user means.
            return bestScore <= (haveAim ? 2f : 45f) ? best : null;
        }

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
            if (State != SearchState.Searching) return;
            // The cue being looked at / whose check failed, else whatever marked spot the phone points at, even an
            // already-checked one (the user lifted the papers and found the glasses there after all).
            SpotCue cue = ManualTarget;
            if (cue == null) cue = CueNearAim(includeChecked: true);
            if (cue == null) { FoundWithoutSpot(); return; }
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
