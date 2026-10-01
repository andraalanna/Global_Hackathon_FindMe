using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FindMe.UI
{
    /// <summary>
    /// UI_Test only: stands in for SearchController. It plays the same ISearchUI calls the real app makes, so the
    /// whole flow can be clicked through in the Editor:
    ///   Continue -> "Getting to know..." (4 s) -> item card -> Start scanning -> readiness 0..1 (4 s)
    ///   -> HUD -> Scan this area -> "Looking..." (2 s) -> 3 spots -> I found it.
    /// Test keys while playing:  C = mark the next spot checked (toast + progress)   U = undo the last one
    ///                           P = spot panel demo (hold still -> looking -> tip + manual buttons)   H = hide it
    ///                           M = "Is this your ...?" match sheet.
    /// </summary>
    public class FakeSearchDriver : MonoBehaviour
    {
        public UIManager ui;
        [Tooltip("How long the fake Gemini profile call takes.")]
        public float fakeGeminiSeconds = 4f;
        [Tooltip("How long the fake 'move your phone slowly' phase takes.")]
        public float fakeReadinessSeconds = 4f;

        const int FakeSpots = 3;
        readonly string[] fakeSpotNames = { "Clothes", "Bag", "Drawer" };
        Coroutine panelRoutine;
        int checkedCount;
        int spots;

        void Start()
        {
            ui.StartRequested += text => StartCoroutine(Profile(text));
            ui.ScanStartRequested += () => StartCoroutine(Readiness());
            ui.ScanRequested += () => StartCoroutine(Scan());
            ui.FoundRequested += () => StartCoroutine(Found());
            ui.StopRequested += () => { StopAllCoroutines(); checkedCount = spots = 0; };
            ui.CheckedRequested += () => { Debug.Log("[Fake] manual Checked pressed"); StopPanelDemo(); ui.HideSpotPanel(); Mark(); };
            ui.UndoRequested += () => Undo();
            ui.MatchAnswered += yes => Debug.Log("[Fake] match answered: " + (yes ? "Yes" : "Not it"));
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null || spots == 0) return;
            if (kb.cKey.wasPressedThisFrame) Mark();
            if (kb.uKey.wasPressedThisFrame) Undo();
            if (kb.pKey.wasPressedThisFrame) { StopPanelDemo(); panelRoutine = StartCoroutine(PanelDemo()); }
            if (kb.hKey.wasPressedThisFrame) { StopPanelDemo(); ui.HideSpotPanel(); }
            if (kb.mKey.wasPressedThisFrame) ui.ShowMatch("bag");
        }

        void StopPanelDemo()
        {
            if (panelRoutine != null) StopCoroutine(panelRoutine);
            panelRoutine = null;
        }

        void Mark()
        {
            if (checkedCount >= spots) return;
            checkedCount++;
            ui.SetProgress(checkedCount, spots);
            ui.ShowCheckedToast(fakeSpotNames[(checkedCount - 1) % fakeSpotNames.Length]);
            // Last spot checked: SearchController nudges the user to scan elsewhere.
            if (checkedCount == spots) ui.ShowStatus("All spots checked. Scan another area?");
        }

        // What SearchController does when the item is found: ShowFound, then ShowSummary right after.
        IEnumerator Found()
        {
            StopPanelDemo();
            ui.ShowFound("bag");
            var session = new SearchSession { item = "wallet", startTime = Time.time - 161f, scans = 2, foundLabel = "bag", foundTime = Time.time };
            for (int i = 0; i < 4; i++) session.spots.Add(new SearchSession.SpotRecord { label = "spot" + i, state = SpotState.Checked, checkedAt = Time.time });
            ui.ShowSummary(session);
            yield break;
        }

        void Undo()
        {
            if (checkedCount <= 0) return;
            checkedCount--;
            ui.SetProgress(checkedCount, spots);
        }

        // Plays what SearchController sends while the user checks a spot: dwell fills, Gemini looks, then a tip + manual buttons.
        IEnumerator PanelDemo()
        {
            for (float t = 0f; t < 2f; t += Time.unscaledDeltaTime)
            {
                ui.ShowSpotPanel("drawer", "Hold still…", t / 2f, false);
                yield return null;
            }
            for (float t = 0f; t < 1.5f; t += Time.unscaledDeltaTime)
            {
                ui.ShowSpotPanel("drawer", "Looking…", 1f, false);
                yield return null;
            }
            // From here SearchController keeps calling Show every frame until the user acts.
            while (true)
            {
                ui.ShowSpotPanel("drawer", "Open the drawer a little, then point the camera inside.", 0f, true);
                yield return null;
            }
        }

        IEnumerator Profile(string text)
        {
            ui.SetScanEnabled(false);
            ui.ShowStatus($"Getting to know your {text}…");
            yield return new WaitForSecondsRealtime(fakeGeminiSeconds);
            ui.ShowItemCard(new ItemProfile
            {
                name = text,
                sizeCm = new[] { 11f, 9f },
                shape = "flat rectangle",
                likelyPlaces = new List<string> { "Under clothes on the bed", "Inside your bag", "Desk drawer" }
            });
            ui.ShowStatus("Move your phone slowly around the room");
        }

        IEnumerator Readiness()
        {
            for (float t = 0f; t < fakeReadinessSeconds; t += Time.unscaledDeltaTime)
            {
                ui.SetReadiness(t / fakeReadinessSeconds);
                yield return null;
            }
            ui.SetReadiness(1f);
            ui.SetScanEnabled(true);
            ui.ShowStatus("Point at part of the room, then tap Scan");
        }

        IEnumerator Scan()
        {
            ui.SetScanEnabled(false);
            ui.ShowStatus("Looking for hiding spots…");
            yield return new WaitForSecondsRealtime(2f);
            spots = FakeSpots; checkedCount = 0;
            ui.SetProgress(0, spots);
            ui.SetScanEnabled(true);
            ui.ShowStatus($"Found {spots} places to check");
        }
    }
}
