using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FindMe.UI
{
    /// <summary>
    /// A pretend SearchController for UI_Test (no AR, no Gemini). It answers the UI's buttons with the same
    /// ISearchUI calls the real one makes, with realistic delays, and then plays a short demo story:
    /// spot 1 gets checked automatically, spot 2 needs opening, spot 3 is a match ("Is this your keys?").
    /// UIManager adds it automatically when the scene has no SearchController.
    /// </summary>
    public class FakeSearchDriver : MonoBehaviour, ISearchActions
    {
        public UIManager ui;
        [Tooltip("Speed up every delay (2 = twice as fast).")]
        public float speed = 1f;

        readonly string[] spots = { "desk drawer", "bag", "sofa cushions", "coat hook" };
        string item = "keys";
        int checkedCount;
        string current;               // spot shown in the panel (target for Checked / Found it)
        string lastChecked;
        bool waitingForMatch;
        SearchSession session;

        public void StartSearch(string text)
        {
            StopAllCoroutines();
            item = text;
            checkedCount = 0;
            current = lastChecked = null;
            session = new SearchSession { item = text, startTime = Time.time };
            StartCoroutine(Profile());
        }

        IEnumerator Profile()
        {
            ui.SetScanEnabled(false);
            ui.ShowStatus($"Getting to know your {item}…");
            yield return Wait(2.4f);
            ui.ShowItemCard(new ItemProfile
            {
                name = item, sizeCm = new[] { 9f, 4f }, shape = "small object",
                likelyPlaces = new List<string> { "Coat pockets", "Sofa cushions", "Bag" },
            });
            ui.ShowStatus("Move your phone slowly around the room");
            for (float p = 0f; p < 1f; p += Time.deltaTime * speed / 3.5f)
            {
                ui.SetReadiness(p);
                yield return null;
            }
            ui.SetReadiness(1f);
            ui.SetScanEnabled(true);
            ui.ShowStatus("Point at part of the room, then tap Scan");
        }

        public void StartSearchWithImage(byte[] jpg, string text) => StartSearch(string.IsNullOrEmpty(text) ? "item" : text);

        public void ScanArea()
        {
            StopAllCoroutines();
            StartCoroutine(Scan());
        }

        IEnumerator Scan()
        {
            session.scans++;
            ui.SetScanEnabled(false);
            ui.HideSpotPanel();
            ui.ShowStatus("Looking for hiding spots…");
            yield return Wait(1.6f);
            ui.SetScanEnabled(true);
            ui.SetProgress(checkedCount, spots.Length);
            ui.ShowStatus($"Found {spots.Length} places to check");
            yield return Wait(2f);
            yield return Story();
        }

        // The demo: what happens when the user holds the phone on each spot in turn.
        IEnumerator Story()
        {
            yield return Dwell(spots[0], "Hold still…");
            yield return Checking(spots[0]);
            Check(spots[0]);
            yield return Wait(3f);

            yield return Dwell(spots[1], "Hold still…");
            yield return Checking(spots[1]);
            current = spots[1];
            ui.ShowSpotPanel(spots[1], "Open the bag and look inside", 0f, true);   // unclear → manual buttons
            yield return Wait(5f);

            yield return Dwell(spots[2], "Hold still…");
            yield return Checking(spots[2]);
            ui.HideSpotPanel();
            current = spots[2];
            waitingForMatch = true;
            ui.ShowMatch(spots[2]);                                               // "Is this your keys?"
        }

        IEnumerator Dwell(string spot, string tip)
        {
            current = spot;
            for (float d = 0f; d < 1f; d += Time.deltaTime * speed / 2f)
            {
                ui.ShowSpotPanel(spot, tip, d, false);
                yield return null;
            }
        }

        IEnumerator Checking(string spot)
        {
            ui.ShowSpotPanel(spot, "Looking…", 1f, false);
            yield return Wait(1.3f);
        }

        void Check(string spot)
        {
            checkedCount++;
            lastChecked = spot;
            current = null;
            ui.HideSpotPanel();
            ui.SetProgress(checkedCount, spots.Length);
            ui.ShowCheckedToast(spot);
        }

        public void CheckHere()
        {
            if (current == null) { ui.ShowStatus("Point at one of the marked spots, then tap Check here."); return; }
            StopAllCoroutines();
            StartCoroutine(CheckNow(current));
        }

        IEnumerator CheckNow(string spot)
        {
            yield return Checking(spot);
            Check(spot);
        }

        public void MarkChecked()
        {
            if (current == null) return;
            Check(current);
        }

        public bool MarkFound()
        {
            if (current == null) return false;
            Found(current);
            return true;
        }

        public List<string> SpotLabels() => new List<string>(spots);

        public bool MarkFoundAt(string label)
        {
            foreach (var s in spots) if (string.Equals(s, label, System.StringComparison.OrdinalIgnoreCase)) { Found(s); return true; }
            return false;
        }

        public void UndoChecked()
        {
            if (lastChecked == null) return;
            checkedCount--;
            lastChecked = null;
            ui.SetProgress(checkedCount, spots.Length);
        }

        public void ConfirmMatch(bool yes)
        {
            if (!waitingForMatch) return;
            waitingForMatch = false;
            if (yes) { Found(current); return; }
            ui.ShowStatus("Okay, let's keep looking!");
            current = null;
        }

        void Found(string spot)
        {
            StopAllCoroutines();
            ui.HideSpotPanel();
            session.foundLabel = spot;
            session.foundTime = Time.time;
            ui.ShowStatus($"You found your {item}!");
            ui.ShowFound(spot);
            ui.ShowSummary(session);
        }

        public void StopSearch()
        {
            StopAllCoroutines();
            current = null;
            waitingForMatch = false;
            ui.HideSpotPanel();
            ui.SetScanEnabled(false);
        }

        WaitForSeconds Wait(float s) => new WaitForSeconds(s / Mathf.Max(0.01f, speed));
    }
}
