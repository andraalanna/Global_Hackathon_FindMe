using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.Simulation;
using FindMe.UI;

namespace FindMe.Sim
{
    /// <summary>
    /// A pretend user for Play mode in Main (Editor only): walks the simulated phone around the XR Simulation
    /// living room, taps the real UI buttons, holds the phone on cues so the automatic checks run, and tries
    /// the awkward cases (cancel mid-scan, undo, "Not it", app pause, double taps…).
    /// After every step it checks the search state AND what the UI shows. Report: Temp/Sim/report.md.
    /// Uses the mock Gemini (no API calls); its fixed answers make the story repeatable:
    /// every spot is "closed" on odd checks and "open" on even ones; the bag holds the item when open.
    /// </summary>
    public class SearchSimulator : MonoBehaviour
    {
        public static readonly string OutDir = Path.GetFullPath("Temp/Sim");
        public bool Done { get; private set; }
        [Tooltip("Run only scenario 9 (placement accuracy).")]
        public bool accuracyOnly;

        UIManager ui;
        SearchController search;
        SpotPlacer placer;
        SpotFocus focus;
        SimulationCameraPoseProvider phone;
        Camera cam;

        readonly StringBuilder report = new();
        readonly List<string> errors = new();
        int pass, fail, shot;
        string scenario;

        void Start()
        {
            Directory.CreateDirectory(OutDir);
            foreach (var f in Directory.GetFiles(OutDir)) File.Delete(f);
            Application.logMessageReceived += OnLog;
            StartCoroutine(RunAll());
        }

        void OnDestroy() => Application.logMessageReceived -= OnLog;

        // Every error/exception in the console during the run is a failure of the scenario it happened in.
        void OnLog(string msg, string stack, LogType type)
        {
            if (type is LogType.Error or LogType.Exception or LogType.Assert)
                errors.Add($"[{scenario}] {type}: {msg}\n{stack.Split('\n').FirstOrDefault()}");
        }

        IEnumerator RunAll()
        {
            yield return null;
            ui = FindAnyObjectByType<UIManager>();
            search = FindAnyObjectByType<SearchController>();
            placer = FindAnyObjectByType<SpotPlacer>();
            focus = FindAnyObjectByType<SpotFocus>();
            phone = FindAnyObjectByType<SimulationCameraPoseProvider>();
            cam = Camera.main;
            Line($"# FIND ME search simulation\n\n{DateTime.Now:yyyy-MM-dd HH:mm}  ·  screen {Screen.width}×{Screen.height}");
            if (ui == null || search == null || placer == null || focus == null || phone == null || cam == null)
            {
                Line($"**Setup failed**: ui={ui} search={search} placer={placer} focus={focus} phone={phone} cam={cam}");
                Finish();
                yield break;
            }
            FindAnyObjectByType<GeminiServiceProvider>().SetMock(true);          // canned answers, no API calls
            yield return new WaitForSeconds(1f);

            if (accuracyOnly)
            {
                yield return Run("9. Placement accuracy (ground-truth boxes)", Accuracy());
                Finish();
                yield break;
            }
            yield return Run("1. Full search: walk, scan, check, undo, Not it, found", FullSearch());
            yield return Run("2. Exit while Gemini profiles the item", ExitDuringProfiling());
            yield return Run("3. Exit while a scan is in flight", ExitDuringScan());
            yield return Run("4. App goes to background while mapping", PauseDuringMapping());
            yield return Run("5. Double tap Start scanning", DoubleTapScan());
            yield return Run("6. Check here, Found without a target, exit mid-check", ManualPaths());
            yield return Run("7. Scan the same area twice (merge, no duplicates)", Rescan());
            yield return Run("8. Check every spot (all-checked nudge)", CheckEverything());
            yield return Run("9. Placement accuracy (ground-truth boxes)", Accuracy());
            yield return Run("10. Found it myself under the papers (spot already checked)", FoundMyself());
            yield return Run("11. Look again at a checked spot", LookAgain());
            Finish();
        }

        IEnumerator Run(string name, IEnumerator body)
        {
            scenario = name;
            int errBefore = errors.Count;
            Line($"\n## {name}");
            // Watchdog: a scenario that hangs (waiting on something that never happens) is a failure, not a freeze.
            bool finished = false;
            var co = StartCoroutine(Wrap(body, () => finished = true));
            float end = Time.time + 150f;
            while (!finished && Time.time < end) yield return null;
            if (!finished)
            {
                StopCoroutine(co);
                Check(false, $"scenario finished within 150 s (stuck: state {search.State}, screen '{ui.Router.Current.Id}')");
            }
            for (int i = errBefore; i < errors.Count; i++) Check(false, "console: " + errors[i]);
            // Clean slate for the next scenario.
            search.StopSearch();
            ui.Router.Go("find");
            yield return new WaitForSeconds(0.5f);
        }

        static IEnumerator Wrap(IEnumerator body, Action done)
        {
            yield return body;
            done();
        }

        void Finish()
        {
            Line($"\n---\n**{pass} passed, {fail} failed**");
            File.WriteAllText(Path.Combine(OutDir, "report.md"), report.ToString());
            Debug.Log($"[Sim] Done: {pass} passed, {fail} failed. Report: {OutDir}/report.md");
            Done = true;
        }

        // ======================================================================= scenarios

        IEnumerator FullSearch()
        {
            yield return StartFromHome("Wallet");
            yield return Until(() => ui.Search.profile != null, 5f, "profile arrives (ShowItemCard)");
            yield return Until(() => search.State == SearchState.WaitingForAR, 3f, "state WaitingForAR");
            Check(ui.Router.Current.Id == "analyzing", "still on Analyzing while AR warms up");
            yield return new WaitForSeconds(2.2f);                                // steps tick, button fades in
            Tap("Scan the room");
            Check(ui.Router.Current.Id == "mapping", "Scan the room → Mapping");
            Check(!Button("Start scanning").interactable, "Start scanning is disabled before the room is mapped");

            // Walk and look around like a person until AR says the room is understood.
            float maxSeen = 0f;
            bool ready = false;
            yield return Walk(() => { maxSeen = Mathf.Max(maxSeen, ui.Search.readiness); return ready = search.State == SearchState.ReadyToScan; }, 45f);
            Check(ready, $"room mapping completes by walking (readiness reached {maxSeen:0.00})");
            if (!ready) search.DevSkipReadiness();
            Check(Button("Start scanning").interactable, "Start scanning unlocks when ready");
            Check(Text("Prog").Contains("100%") || ui.Search.readiness >= 0.99f, "mapping shows 100%");
            Snap("mapping_ready");

            yield return Face(new Vector3(0, 1.4f, -0.6f), new Vector3(0f, 0.8f, 2.8f));   // look at sofa + desk + bag
            Tap("Start scanning");
            Check(ui.Router.Current.Id == "scan", "Start scanning → scan HUD");
            Check(search.State == SearchState.Scanning, "state Scanning");
            yield return Until(() => search.State == SearchState.Searching, 10f, "spots placed → state Searching");
            yield return new WaitForSeconds(1f);
            var cues = Live();
            Check(cues.Count > 0, $"cues placed: {cues.Count} ({string.Join(", ", cues.Select(c => c.Label))})");
            Check(ui.Search.total == cues.Count, $"progress total {ui.Search.total} = cues {cues.Count}");
            foreach (var c in cues)
            {
                var p = c.transform.position;
                bool inRoom = p.y > -0.1f && p.y < 2.7f && Mathf.Abs(p.x) < 3.1f && p.z > -2.6f && p.z < 3.6f;
                Check(inRoom, $"cue '{c.Label}' inside the room at {p:F2} ({c.Quality})");
            }
            Snap("cues_placed");

            // Check spot 1 by holding the phone on it: mock says closed (needs opening) first, then open → checked.
            var first = placer.PickNext(cam.transform.position);
            Check(first != null, $"there is a 'next' cue ({first?.Label})");
            if (first == null) yield break;
            yield return Hold(first);
            yield return Until(() => first.State == SpotState.NeedsOpen, 6f, $"1st check of '{first.Label}' → needs opening");
            yield return Until(() => ui.Search.panelOn && ui.Search.panelTip == "Open it up", 2f, "spot panel shows the tip 'Open it up'");
            Snap("needs_open");
            yield return Hold(first);
            yield return Until(() => first.State == SpotState.Checked, 8f, $"2nd check of '{first.Label}' → checked");
            yield return Until(() => ToastVisible(), 2f, "checked toast slides in");
            Check(ui.Search.checkedCount == 1, $"progress 1 of {ui.Search.total} (is {ui.Search.checkedCount})");
            Check(Text("CheckedToast").ToLower().Contains(first.Label.ToLower()), $"toast names the spot ('{Text("CheckedToast")}')");
            Snap("checked_toast");

            // Undo puts it back.
            TapIn("CheckedToast", "Undo");
            yield return null;
            Check(first.State is SpotState.ToCheck or SpotState.Focused, $"Undo → spot open again ({first.State}; Focused = still aimed at)");
            Check(ui.Search.checkedCount == 0, "Undo → progress back to 0");
            Check(!ToastVisible(), "Undo hides the toast");

            // The bag: closed → open with the item → "Is this your wallet?" → Not it → again → Yes.
            var bagCue = Live().FirstOrDefault(c => c.Label.Contains("bag"));
            Check(bagCue != null, "a 'bag' cue exists");
            if (bagCue == null) yield break;
            yield return Hold(bagCue);
            yield return Until(() => bagCue.State == SpotState.NeedsOpen, 6f, "bag: 1st check → needs opening");
            yield return Hold(bagCue);
            yield return Until(() => search.State == SearchState.FoundConfirm, 8f, "bag: 2nd check sees the item → FoundConfirm");
            yield return Until(() => Active("Match"), 2f, "match sheet 'Is this your wallet?' is shown");
            Check(Text("Match").Contains("wallet"), $"match sheet names the item ('{Text("Match").Split('\n')[0]}')");
            Check(!Active("BubbleRow"), "status bubble hidden behind the match question");
            Snap("match");
            TapIn("Match", "Not it");
            yield return Until(() => search.State == SearchState.Searching, 2f, "Not it → back to Searching");
            Check(bagCue.State is SpotState.ToCheck or SpotState.Focused, $"Not it → bag open again ({bagCue.State})");
            Check(Active("Info") && !Active("Match"), "Not it → normal sheet is back");

            yield return Hold(bagCue);
            yield return Until(() => bagCue.State == SpotState.NeedsOpen, 6f, "bag: 3rd check → needs opening");
            yield return Hold(bagCue);
            yield return Until(() => Active("Match"), 8f, "bag: 4th check → match again");
            TapIn("Match", "Yes, that's it!");
            yield return Until(() => ui.Router.Current.Id == "found", 2f, "Yes → Found it! screen");
            Check(search.State == SearchState.Found, "state Found");
            Check(ui.Search.session != null && ui.Search.session.Found, "ShowSummary delivered a session");
            Check(Text("Found it!").Length > 0 || ui.Router.Current.Id == "found", "Found it! is on screen");
            Check(ui.State.history[0].result.ToLower().Contains("bag"), $"History logs it ('{ui.State.history[0].result}')");
            Snap("found");
            Tap("Nice, done");
            Check(ui.Router.Current.Id == "summary", "Nice, done → Summary");
            int spotsShown = ui.Search.session?.CheckedCount ?? -1;
            Check(spotsShown >= 1, $"summary counts checked spots ({spotsShown})");
            Snap("summary");
            Tap("Back home");
            Check(ui.Router.Current.Id == "find", "Back home → Find");
        }

        IEnumerator ExitDuringProfiling()
        {
            yield return StartFromHome("Keys");
            Check(search.State == SearchState.Profiling, "state Profiling");
            ui.ExitSearch();
            Check(ui.Router.Current.Id == "notfound", "X → Not found yet");
            yield return new WaitForSeconds(2.5f);                            // the cancelled profile would have arrived by now
            Check(search.State == SearchState.Idle, $"stays Idle (is {search.State})");
            Check(ui.Router.Current.Id == "notfound", $"no screen jump after cancel (on '{ui.Router.Current.Id}')");
        }

        IEnumerator ExitDuringScan()
        {
            yield return ReadyToScan("Glasses");
            Tap("Start scanning");
            Check(search.State == SearchState.Scanning, "scanning");
            yield return new WaitForSeconds(0.4f);                            // photo taken, Gemini call in flight
            ui.ExitSearch();
            yield return new WaitForSeconds(3f);
            Check(search.State == SearchState.Idle, $"stays Idle after the late scan answer (is {search.State})");
            Check(Live().Count == 0, $"no cues appear after exit ({Live().Count})");
            Check(ui.Router.Current.Id == "notfound", $"still on Not found (on '{ui.Router.Current.Id}')");
        }

        IEnumerator PauseDuringMapping()
        {
            yield return StartFromHome("Keys");
            yield return Until(() => search.State == SearchState.WaitingForAR, 5f, "WaitingForAR");
            yield return new WaitForSeconds(2.2f);
            Tap("Scan the room");
            yield return Walk(() => ui.Search.readiness > 0.4f, 15f);
            float before = ui.Search.readiness;
            search.SendMessage("OnApplicationPause", true);
            search.SendMessage("OnApplicationPause", false);
            yield return null;
            Check(before > 0.3f, $"meter had progress before the pause ({before:0.00})");
            Check(ui.Search.readiness < before - 0.1f, $"returning from background restarts the meter ({before:0.00} → {ui.Search.readiness:0.00})");
            Check(!Button("Start scanning").interactable, "Start scanning locked again after the pause");
            Check(ui.Router.Current.Id == "mapping", "still on Mapping");
        }

        IEnumerator DoubleTapScan()
        {
            yield return ReadyToScan("Wallet");
            Tap("Start scanning");
            var scanBtn = ButtonIn("Buttons", "Scan");
            if (scanBtn != null) scanBtn.onClick.Invoke();                   // impatient second tap on the HUD's Scan
            yield return Until(() => search.State == SearchState.Searching, 10f, "Searching");
            yield return new WaitForSeconds(0.8f);
            Check(search.Session.scans == 1, $"only one scan ran ({search.Session.scans})");
            var labels = Live().Select(c => c.Label).ToList();
            Check(labels.Count == labels.Distinct().Count(), $"no duplicate cues ({string.Join(", ", labels)})");
        }

        IEnumerator ManualPaths()
        {
            yield return ReadyToScan("Keys");
            yield return Face(new Vector3(0, 1.4f, -0.6f), new Vector3(0f, 0.8f, 2.8f));
            Tap("Start scanning");
            yield return Until(() => search.State == SearchState.Searching, 10f, "Searching");
            yield return new WaitForSeconds(0.6f);

            // Found with nothing targeted → ask where.
            yield return Face(cam.transform.position, cam.transform.position + new Vector3(0f, 2f, 0.3f));   // look up at the ceiling
            yield return new WaitForSeconds(0.5f);
            TapIn("Buttons", "Found");
            Check(ui.Router.Current.Id == "foundWhere", "Found with no spot targeted → 'Where did you find it?'");
            Check(!Button("Save").interactable, "Save disabled until a place is picked");
            ui.Router.Back();
            Check(ui.Router.Current.Id == "scan", "Back → scan HUD again");

            // "Check here": point roughly at a cue and tap (no 2 s hold).
            var drawer = Live().FirstOrDefault(c => c.Label.Contains("drawer")) ?? placer.PickNext(cam.transform.position);
            yield return Face(Approach(drawer), drawer.transform.position);
            yield return new WaitForSeconds(0.3f);
            TapIn("Buttons", "Check here");
            yield return null;
            Check(drawer.State == SpotState.Checking, $"Check here starts a check right away ('{drawer.Label}' is {drawer.State})");
            Check(ui.Search.panelTip == "Looking…", "panel says Looking…");

            // Exit while that check is running: nothing may change afterwards.
            ui.ExitSearch();
            yield return new WaitForSeconds(1.5f);
            Check(search.State == SearchState.Idle, "exit mid-check → Idle");
            Check(Live().Count == 0, "cues cleared");
            Check(ui.Router.Current.Id == "notfound", "on Not found");
        }

        IEnumerator Rescan()
        {
            yield return ReadyToScan("Wallet");
            yield return Face(new Vector3(0, 1.4f, -0.6f), new Vector3(0f, 0.8f, 2.8f));
            Tap("Start scanning");
            yield return Until(() => search.State == SearchState.Searching, 10f, "first scan placed spots");
            yield return new WaitForSeconds(1f);
            int n1 = Live().Count;
            TapIn("Buttons", "Scan");
            yield return Until(() => search.State == SearchState.Scanning, 2f, "second scan starts");
            yield return Until(() => search.State == SearchState.Searching, 10f, "second scan done");
            yield return new WaitForSeconds(1f);
            int n2 = Live().Count;
            Check(n2 == n1, $"same view scanned twice merges into the same cues ({n1} → {n2})");
            Check(search.Session.scans == 2, $"2 scans counted ({search.Session.scans})");
            Check(ui.Search.total == n2, $"progress total follows ({ui.Search.total})");
            Snap("rescan");
        }

        IEnumerator CheckEverything()
        {
            yield return ReadyToScan("Keys");
            yield return Face(new Vector3(0, 1.4f, -0.6f), new Vector3(0f, 0.8f, 2.8f));
            Tap("Start scanning");
            yield return Until(() => search.State == SearchState.Searching, 10f, "Searching");
            yield return new WaitForSeconds(1f);
            // Every non-bag spot: two holds (closed, then open → checked). The bag would be a match, so skip it.
            foreach (var c in Live().Where(c => !c.Label.Contains("bag")).ToList())
            {
                for (int i = 0; i < 2 && c.State != SpotState.Checked; i++)
                {
                    yield return Hold(c);
                    yield return Until(() => c.State is SpotState.NeedsOpen or SpotState.Checked, 8f, $"'{c.Label}' check #{i + 1}");
                }
                Check(c.State == SpotState.Checked, $"'{c.Label}' ends checked ({c.State})");
            }
            var bag = Live().FirstOrDefault(c => c.Label.Contains("bag"));
            if (bag != null)
            {
                // Mark the bag by hand from the manual buttons: hold until the panel offers them, or use Check here.
                yield return Face(Approach(bag), bag.transform.position);
                yield return new WaitForSeconds(0.4f);
                Check(focus.Focused == bag, $"phone aimed at the bag focuses it ({focus.Focused?.Label})");
                search.MarkChecked();
                yield return null;
            }
            int open = Live().Count(c => c.IsOpen);
            Check(open == 0, $"no spots left to check ({open})");
            Check(ui.Search.checkedCount == ui.Search.total, $"progress {ui.Search.checkedCount} of {ui.Search.total}");
            yield return new WaitForSeconds(0.5f);
            Check((ui.Search.status ?? "").Contains("Scan another area"), $"nudge to scan another area (status '{ui.Search.status}')");
            Snap("all_checked");
        }

        /// <summary>
        /// How close do cues land to the real objects? Gemini is replaced by GroundTruthGemini, whose boxes are the
        /// real objects projected through each snapshot's own pose, so every centimetre of error is ours.
        /// Pass mark: the cue within 15 cm of the object's surface (cues float 3 cm in front of it by design).
        /// </summary>
        IEnumerator Accuracy()
        {
            yield return ReadyToScan("Wallet");
            // Let AR learn the room's surfaces (sofa seat, tables, floor, walls) like a user wandering about.
            Vector3[] stops = { new(1.0f, 1.4f, 0.8f), new(-1.3f, 1.3f, 1.0f), new(0.2f, 1.5f, 1.2f), new(0f, 1.4f, -0.6f) };
            Vector3[] looks = { new(1.8f, 0.5f, 2.8f), new(-0.8f, 0.4f, 3.0f), new(0.6f, 0.1f, 2.3f), new(0f, 0.7f, 2.8f) };
            for (int i = 0; i < stops.Length; i++) yield return Face(stops[i], looks[i], 0.35f, 35f);
            var planes = FindAnyObjectByType<ARPlaneManager>();
            Line($"  AR found {planes.trackables.count} planes while walking");

            var room = new GroundTruthRoom(phone, cam);
            Check(room.Found && room.wallet != null, $"found the room's real objects ({room.targets.Count} spots + wallet)");
            if (!room.Found) yield break;
            var gt = new GroundTruthGemini(room, search, cam);
            var provider = FindAnyObjectByType<GeminiServiceProvider>();
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            var realField = typeof(GeminiServiceProvider).GetField("real", flags);
            var mockField = typeof(GeminiServiceProvider).GetField("useMock", flags);
            object oldReal = realField.GetValue(provider), oldMock = mockField.GetValue(provider);
            realField.SetValue(provider, gt);
            mockField.SetValue(provider, false);

            var all = new List<float>();
            (string name, Vector3 pos, Vector3 look, float roll)[] views =
            {
                ("front of the room, 3-4 m away", new(0f, 1.4f, -0.8f), new(0f, 0.6f, 2.6f), 0f),
                ("closer, looking down at the bag", new(0.4f, 1.35f, 1.0f), new(0.7f, 0.15f, 2.4f), 0f),
                ("right side, desk and drawer", new(1.0f, 1.4f, 0.9f), new(1.6f, 0.6f, 2.7f), 0f),
                ("left side, sofa", new(-1.5f, 1.3f, 1.3f), new(-0.7f, 0.55f, 2.9f), 0f),
                ("crouching low near the coffee table", new(-0.2f, 0.7f, 0.6f), new(0.3f, 0.3f, 2.6f), 0f),
            };
            foreach (var v in views)
            {
                // Measuring placement only: no automatic checks while the phone rests on a view (holding still on a
                // cue would start one, and an "Is this your wallet?" question blocks scanning).
                if (search.State == SearchState.FoundConfirm) search.ConfirmMatch(false);
                focus.CanTrigger = false;
                placer.ClearCues();
                yield return Face(v.pos, v.look, 0.4f, 40f, null, v.roll);
                yield return new WaitForSeconds(0.8f);
                Line($"  **View: {v.name}** (phone at {cam.transform.position:F2}, held {ImageOrientation.Detect(cam.transform)})");
                search.ScanArea();
                yield return Until(() => search.State != SearchState.Scanning, 10f, $"scan from '{v.name}' finishes");
                yield return new WaitForSeconds(1.8f);                    // cues pop in 0.15 s apart
                Line("  " + gt.log.LastOrDefault());
                var cues = Live();
                Check(cues.Count > 0, $"'{v.name}': {cues.Count} cues placed");
                foreach (var c in cues)
                {
                    var t = room.TargetFor(c.Label);
                    if (t == null) continue;
                    float err = room.Distance(t.parts, c.transform.position);
                    all.Add(err);
                    Check(err < 0.15f, $"'{c.Label}' cue is {err * 100f:0} cm from the real object ({c.Quality})");
                }
            }

            // Up close: hold on the bag. The check photo re-measures the cue, and the wallet gets a mint marker.
            if (search.State == SearchState.FoundConfirm) search.ConfirmMatch(false);
            focus.CanTrigger = false;
            placer.ClearCues();
            yield return Face(new Vector3(0.4f, 1.35f, 1.0f), new Vector3(0.7f, 0.15f, 2.4f), 0.4f, 40f);
            yield return new WaitForSeconds(0.8f);
            search.ScanArea();
            yield return Until(() => search.State != SearchState.Scanning, 10f, "scan before the close-up check");
            yield return new WaitForSeconds(1.8f);
            var bag = Live().FirstOrDefault(c => c.Label == "bag");
            Check(bag != null, "bag cue for the close-up check");
            focus.CanTrigger = true;                                      // now let the hold start the real check
            if (bag != null)
            {
                var bagT = room.TargetFor("bag");
                float before = room.Distance(bagT.parts, bag.transform.position);
                yield return Hold(bag);
                yield return Until(() => search.State == SearchState.FoundConfirm, 6f, "the check photo sees the wallet: 'Is this your wallet?'");
                yield return new WaitForSeconds(0.5f);
                Line("  " + gt.log.LastOrDefault());
                float after = room.Distance(bagT.parts, bag.transform.position);
                Check(after < 0.15f, $"bag cue after the close-up: {after * 100f:0} cm (before: {before * 100f:0} cm)");
                var marker = GameObject.Find("ItemMarker");
                Check(marker != null, "mint marker placed where the wallet was seen");
                if (marker != null)
                {
                    float m = room.Distance(new[] { room.wallet }, marker.transform.position);
                    Check(m < 0.12f, $"marker is {m * 100f:0} cm from the real wallet");
                }
                Snap("accuracy_match");
                search.ConfirmMatch(false);
            }

            if (all.Count > 0)
            {
                all.Sort();
                Line($"  **Cue error over {all.Count} cues: median {all[all.Count / 2] * 100f:0} cm, worst {all[all.Count - 1] * 100f:0} cm**");
            }
            realField.SetValue(provider, oldReal);
            mockField.SetValue(provider, oldMock);
        }

        /// <summary>
        /// The glasses-under-papers case: the AI checked a spot ("nothing here"), the user lifts things and finds the
        /// item there anyway. "Found it" must end the search ON that spot, not leave the user chasing more cues.
        /// </summary>
        IEnumerator FoundMyself()
        {
            yield return ReadyToScan("Glasses");
            yield return Face(new Vector3(0, 1.4f, -0.6f), new Vector3(0f, 0.8f, 2.8f));
            Tap("Start scanning");
            yield return Until(() => search.State == SearchState.Searching, 10f, "Searching");
            yield return new WaitForSeconds(1f);
            var spot = Live().FirstOrDefault(c => !c.Label.Contains("bag"));
            if (spot == null) { Check(false, "a spot to check"); yield break; }
            for (int i = 0; i < 2 && spot.State != SpotState.Checked; i++)
            {
                yield return Hold(spot);
                yield return Until(() => spot.State is SpotState.NeedsOpen or SpotState.Checked, 8f, $"'{spot.Label}' check #{i + 1}");
            }
            Check(spot.State == SpotState.Checked, $"the AI marked '{spot.Label}' as checked (item not seen)");
            int open = Live().Count(c => c.IsOpen);

            // The user finds it there anyway and taps "Found it" while the phone points at the checked spot.
            TapIn("Buttons", "Found it");
            Check(ui.Router.Current.Id == "foundWhere", "Found it on a checked spot → 'Where did you find it?'");
            string chips = Text("Chips");
            Check(chips.ToLower().Contains(spot.Label.ToLower()), $"the list offers the marked spots ('{chips.Replace('\n', ',')}')");
            Tap(MainCap(spot.Label));
            Check(Button("Save").interactable, "picking the spot enables Save");
            Tap("Save");
            yield return null;
            Check(search.State == SearchState.Found, $"search ends as Found on that spot (state {search.State})");
            Check(ui.Router.Current.Id == "found", $"Found it! screen ({ui.Router.Current.Id})");
            Check(search.Session != null && search.Session.foundLabel == spot.Label, $"session found at '{search.Session?.foundLabel}'");
            Check(spot.State == SpotState.Found, $"that cue shows as found ({spot.State})");
            Check(open > 0, $"there were still {open} other spots it no longer asks about");
            Tap("Nice, done");
            Check(ui.Router.Current.Id == "summary", "→ Summary");
        }

        /// <summary>Pointing at a checked spot and tapping Check here re-opens it for a careful second look.</summary>
        IEnumerator LookAgain()
        {
            yield return ReadyToScan("Glasses");
            yield return Face(new Vector3(0, 1.4f, -0.6f), new Vector3(0f, 0.8f, 2.8f));
            Tap("Start scanning");
            yield return Until(() => search.State == SearchState.Searching, 10f, "Searching");
            yield return new WaitForSeconds(1f);
            var spot = Live().FirstOrDefault(c => !c.Label.Contains("bag"));
            if (spot == null) { Check(false, "a spot to check"); yield break; }
            for (int i = 0; i < 2 && spot.State != SpotState.Checked; i++)
            {
                yield return Hold(spot);
                yield return Until(() => spot.State is SpotState.NeedsOpen or SpotState.Checked, 8f, $"'{spot.Label}' check #{i + 1}");
            }
            Check(spot.State == SpotState.Checked, $"'{spot.Label}' is checked");
            int before = ui.Search.checkedCount;
            yield return new WaitForSeconds(0.5f);
            Check(focus.Focused != spot, "a checked spot doesn't start checks by itself");
            TapIn("Buttons", "Check here");
            yield return null;
            Check(spot.State == SpotState.Checking, $"Check here on a checked spot looks again ({spot.State})");
            Check((spot.Note ?? "").Contains("second look"), "Gemini is told to look extra carefully");
            Check((ui.Search.status ?? "").Contains("again"), $"status says so ('{ui.Search.status}')");
            Check(ui.Search.checkedCount == before - 1, $"progress counts it as open again ({before} → {ui.Search.checkedCount})");
            yield return Until(() => spot.State != SpotState.Checking, 6f, "the second look finishes");
        }

        static string MainCap(string s) => string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s.Substring(1);

        // ======================================================================= user actions

        IEnumerator StartFromHome(string thing)
        {
            ui.Router.Go("find");
            yield return null;
            Tap("FIND");
            Check(ui.Router.Current.Id == "what", "FIND → What are you looking for?");
            Check(!Button("Continue").interactable, "Continue disabled until something is picked");
            Tap(thing);
            Check(Button("Continue").interactable, $"picking {thing} enables Continue");
            Tap("Continue");
            Check(ui.Router.Current.Id == "analyzing", "Continue → Analyzing");
            yield return null;
        }

        /// <summary>Start a search and get to "ready to scan" fast (the full walk is tested in scenario 1).</summary>
        IEnumerator ReadyToScan(string thing)
        {
            yield return StartFromHome(thing);
            yield return Until(() => search.State == SearchState.WaitingForAR, 5f, "WaitingForAR");
            yield return new WaitForSeconds(2.2f);
            Tap("Scan the room");
            yield return Walk(() => search.State == SearchState.ReadyToScan, 20f);
            if (search.State != SearchState.ReadyToScan) search.DevSkipReadiness();
        }

        /// <summary>Slow look-around: small steps and turns (well under the "human speed" limits).</summary>
        IEnumerator Walk(Func<bool> until, float timeout)
        {
            Vector3[] stops = { new(0.4f, 1.4f, -0.6f), new(-0.4f, 1.35f, -0.3f), new(0.2f, 1.45f, 0.1f), new(-0.2f, 1.4f, -0.8f) };
            Vector3[] looks = { new(1.5f, 0.6f, 2.8f), new(-1.2f, 0.5f, 2.8f), new(0.3f, 0.3f, 2.0f), new(0f, 0.9f, 3.0f) };
            float end = Time.time + timeout;
            for (int i = 0; Time.time < end && !until(); i = (i + 1) % stops.Length)
                yield return Face(stops[i], looks[i], 0.35f, 35f, until);
        }

        /// <summary>Walk to ~1 m in front of a cue, aim at it, and hold still until its check starts (or 5 s).</summary>
        IEnumerator Hold(SpotCue cue)
        {
            yield return Face(Approach(cue), cue.transform.position);
            float end = Time.time + 6f;
            while (Time.time < end && cue.State != SpotState.Checking) yield return null;
            Check(cue.State == SpotState.Checking || cue.State == SpotState.Checked, $"holding on '{cue.Label}' starts its check");
            while (cue.State == SpotState.Checking && Time.time < end + 4f) yield return null;
            yield return new WaitForSeconds(0.2f);
        }

        /// <summary>Where to stand to check a cue: 1 m back toward the room centre, a bit above it.</summary>
        Vector3 Approach(SpotCue cue)
        {
            Vector3 p = cue.transform.position;
            Vector3 back = new Vector3(0f, 0f, 0.3f) - new Vector3(p.x, 0f, p.z);
            back = back.sqrMagnitude < 0.01f ? Vector3.back : back.normalized;
            return p + back * 1.0f + Vector3.up * 0.45f;
        }

        /// <summary>
        /// Moves the simulated phone smoothly to `pos`, looking at `target` (≤ speed m/s, ≤ turn °/s, like a hand),
        /// then holds still briefly. The XR Simulation camera pose provider is the "device"; the AR camera follows it.
        /// </summary>
        IEnumerator Face(Vector3 pos, Vector3 target, float speed = 0.6f, float turn = 60f, Func<bool> stopEarly = null, float roll = 0f)
        {
            Vector3 dir = target - pos;
            // XR Simulation limits the pitch to ±85°: keep the aim inside that, or the move could never finish.
            Vector3 flat = new Vector3(dir.x, 0f, dir.z);
            if (flat.sqrMagnitude < 1e-4f) flat = new Vector3(cam.transform.forward.x, 0f, cam.transform.forward.z);
            if (flat.sqrMagnitude < 1e-4f) flat = Vector3.forward;
            float pitch = Mathf.Clamp(Mathf.Atan2(dir.y, flat.magnitude) * Mathf.Rad2Deg, -80f, 80f);
            Quaternion rot = Quaternion.LookRotation(flat.normalized, Vector3.up) * Quaternion.Euler(-pitch, 0f, 0f) * Quaternion.Euler(0f, 0f, roll);
            float giveUp = Time.time + 12f;
            // Closed loop: every frame the phone moves by what's still missing, mapped from AR-world into the
            // simulator's device space with the CURRENT phone↔camera rotation (ARSession.Reset() re-centres
            // tracking, so that relation can change mid-test; a one-time mapping would drift off).
            while (true)
            {
                Vector3 cur = cam.transform.position;
                Quaternion curRot = cam.transform.rotation;
                Vector3 nextPos = Vector3.MoveTowards(cur, pos, speed * Time.deltaTime);
                Quaternion nextRot = Quaternion.RotateTowards(curRot, rot, turn * Time.deltaTime);
                Quaternion toDevice = phone.transform.rotation * Quaternion.Inverse(curRot);
                phone.transform.SetPositionAndRotation(phone.transform.position + toDevice * (nextPos - cur), toDevice * nextRot);
                yield return null;
                if (stopEarly != null && stopEarly()) yield break;
                if (Vector3.Distance(cam.transform.position, pos) < 0.02f && Quaternion.Angle(cam.transform.rotation, rot) < 1f) break;
                if (Time.time > giveUp) { Line($"  (move to {pos:F2} gave up at {cam.transform.position:F2})"); break; }
            }
            yield return new WaitForSeconds(0.25f);
        }

        // ======================================================================= UI helpers

        void Tap(string label)
        {
            var b = Button(label);
            if (b == null) { Check(false, $"button '{label}' exists on '{ui.Router.Current.Id}'"); return; }
            if (!b.interactable) { Check(false, $"button '{label}' is tappable"); return; }
            b.onClick.Invoke();
        }

        void TapIn(string container, string label)
        {
            var b = ButtonIn(container, label);
            if (b == null) { Check(false, $"button '{label}' in {container}"); return; }
            b.onClick.Invoke();
        }

        Button Button(string label) => Find(ui.Router.Current.transform, label);

        Button ButtonIn(string container, string label)
        {
            var c = FindChild(ui.Router.Current.transform, container);
            return c == null ? null : Find(c, label);
        }

        static Button Find(Transform root, string label)
        {
            foreach (var b in root.GetComponentsInChildren<Button>(false))
            {
                var t = b.GetComponentInChildren<TextMeshProUGUI>();
                string txt = t != null ? t.text : b.name;
                if (txt == label) return b;
            }
            foreach (var b in root.GetComponentsInChildren<Button>(false))
            {
                var t = b.GetComponentInChildren<TextMeshProUGUI>();
                if (t != null && t.text.Contains(label)) return b;
            }
            return null;
        }

        static Transform FindChild(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            return null;
        }

        bool Active(string name)
        {
            var t = FindChild(ui.Router.Current.transform, name);
            return t != null && t.gameObject.activeInHierarchy;
        }

        bool ToastVisible()
        {
            var t = FindChild(ui.Router.Current.transform, "CheckedToast");
            return t != null && t.gameObject.activeInHierarchy;
        }

        /// <summary>All visible text under a named element (or "" if missing).</summary>
        string Text(string name)
        {
            var t = FindChild(ui.Router.Current.transform, name);
            if (t == null) return "";
            return string.Join("\n", t.GetComponentsInChildren<TextMeshProUGUI>(false).Select(x => x.text));
        }

        List<SpotCue> Live() => placer.Cues.Where(c => c != null).ToList();

        // ======================================================================= reporting

        IEnumerator Until(Func<bool> cond, float timeout, string what)
        {
            float end = Time.time + timeout;
            while (!cond() && Time.time < end) yield return null;
            Check(cond(), what + (cond() ? "" : $" (timed out after {timeout:0}s; state {search.State}, screen '{ui.Router.Current.Id}')"));
        }

        void Check(bool ok, string what)
        {
            if (ok) pass++; else fail++;
            Line($"- {(ok ? "PASS" : "**FAIL**")} {what}");
        }

        void Line(string s)
        {
            report.AppendLine(s);
            if (s.Contains("FAIL")) Debug.LogWarning("[Sim] " + s);
            File.WriteAllText(Path.Combine(OutDir, "report.md"), report.ToString());   // live: open it while it runs
        }

        void Snap(string name) => ScreenCapture.CaptureScreenshot(Path.Combine(OutDir, $"{++shot:00}_{name}.png"));
    }
}
