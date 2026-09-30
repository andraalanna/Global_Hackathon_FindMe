using UnityEngine;

namespace FindMe.Dev
{
    /// <summary>
    /// TEMPORARY stand-in for the UI lead's UIManager (plan Phase 2 "DevUI"). IMGUI, so no Canvas setup.
    /// Implements <see cref="ISearchUI"/>; when UIManager arrives, disable this and point SearchController at it.
    /// </summary>
    public class DevUI : MonoBehaviour, ISearchUI
    {
        [SerializeField] SearchController search;

        string input = "black wallet";
        string status = "Type what you lost";
        string itemCard;
        float readiness = -1f;      // < 0 = hide the meter
        bool scanEnabled;
        string progress;
        string panelLabel, panelTip;
        float panelDwell;
        bool panelManual, panelOn;
        string toast;               // "Drawer checked" + Undo
        float toastUntil;
        string match;               // "Is this your wallet?" sheet
        string found;               // celebration text
        GUIStyle label, button, field;

        void Awake()
        {
            if (search == null) search = FindAnyObjectByType<SearchController>();
        }

        // ---------- ISearchUI ----------
        public void ShowItemCard(ItemProfile p)
        {
            itemCard = "Looking for: " + p.Summary;
            match = found = toast = null;          // a new search clears old sheets
        }
        public void ShowStatus(string message) => status = message;
        public void HideStatus() => status = null;
        public void SetReadiness(float p01) => readiness = p01;
        public void SetProgress(int checkedCount, int total) =>
            progress = total > 0 ? $"{checkedCount} of {total} spots checked" : null;

        public void ShowSpotPanel(string label, string tip, float dwell01, bool manualButtons)
        {
            panelOn = true;
            panelLabel = label;
            panelTip = tip;
            panelDwell = dwell01;
            panelManual = manualButtons;
        }

        public void HideSpotPanel() => panelOn = false;

        public void ShowCheckedToast(string label)
        {
            toast = $"{Cap(label)} checked ✓";
            toastUntil = Time.time + 4f;       // Undo window
        }

        public void ShowMatch(string label) => match = $"Is this your {search.Profile?.name ?? "item"}? ({label})";

        public void ShowFound(string label)
        {
            match = null;
            found = $"Found it! In the {label}.";
        }

        static string Cap(string s) => string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s.Substring(1);

        public void SetScanEnabled(bool on)
        {
            scanEnabled = on;
            if (on) readiness = -1f;   // meter is done once scanning is possible
        }

        void OnGUI()
        {
            if (SnapshotCapture.IsCapturing) return;   // keep dev UI out of the snapshot
            float s = DevGui.Scale;
            label ??= DevGui.Style(GUI.skin.label, 38, Color.white);
            button ??= DevGui.Style(GUI.skin.button, 40);
            field ??= DevGui.Style(GUI.skin.textField, 44);

            // --- Top: status, readiness meter, item card ---
            float x = 40 * s, w = Screen.width - 80 * s, y = DevGui.SafeTop + 30 * s;
            string top = status;
            if (!string.IsNullOrEmpty(progress)) top = string.IsNullOrEmpty(top) ? progress : $"{progress}\n{top}";
            if (!string.IsNullOrEmpty(itemCard)) top = string.IsNullOrEmpty(top) ? itemCard : $"{top}\n{itemCard}";
            if (!string.IsNullOrEmpty(top))
            {
                float h = label.CalcHeight(new GUIContent(top), w - 30 * s) + 20 * s;
                DevGui.Fill(new Rect(x, y, w, h), DevGui.Shade);
                GUI.Label(new Rect(x + 15 * s, y + 10 * s, w - 30 * s, h), top, label);
                y += h + 10 * s;
            }
            if (readiness >= 0f)
            {
                var bar = new Rect(x, y, w, 30 * s);
                DevGui.Fill(bar, DevGui.Shade);
                DevGui.Fill(new Rect(bar.x, bar.y, bar.width * readiness, bar.height), readiness >= 1f ? DevGui.Mint : DevGui.Pink);
            }

            // --- Bottom: item input, presets, Find ---
            float rowH = 110 * s, gap = 16 * s;
            float bottom = DevGui.SafeBottom - 40 * s;

            var findRow = new Rect(x, bottom - rowH, w, rowH);
            input = GUI.TextField(new Rect(findRow.x, findRow.y, findRow.width * 0.68f, rowH), input, field);
            if (GUI.Button(new Rect(findRow.x + findRow.width * 0.7f, findRow.y, findRow.width * 0.3f, rowH), "Find", button))
                search.StartSearch(input);

            // "Is this your wallet?" sheet: big Yes / Not it buttons, above everything else.
            if (match != null)
            {
                var mr = new Rect(x, Screen.height * 0.4f, w, rowH * 2.4f);
                DevGui.Fill(mr, DevGui.Shade);
                GUI.Label(new Rect(mr.x + 15 * s, mr.y + 10 * s, mr.width - 30 * s, rowH), match, label);
                float bw3 = (w - gap * 3) / 2f;
                if (GUI.Button(new Rect(mr.x + gap, mr.yMax - rowH - gap, bw3, rowH), "Yes!", button)) { match = null; search.ConfirmMatch(true); }
                if (GUI.Button(new Rect(mr.x + gap * 2 + bw3, mr.yMax - rowH - gap, bw3, rowH), "Not it", button)) { match = null; search.ConfirmMatch(false); }
            }

            // Found: celebration + Done (back to the start).
            if (found != null)
            {
                var fr = new Rect(x, Screen.height * 0.35f, w, rowH * 2.4f);
                DevGui.Fill(fr, new Color(0.31f, 0.89f, 0.66f, 0.9f));   // mint
                GUI.Label(new Rect(fr.x + 15 * s, fr.y + 10 * s, fr.width - 30 * s, rowH * 1.2f), found, label);
                if (GUI.Button(new Rect(fr.x + gap, fr.yMax - rowH - gap, w - gap * 2, rowH), "Done", button)) { found = null; search.StopSearch(); }
            }

            // Checked toast with Undo (4 s).
            if (toast != null && Time.time < toastUntil)
            {
                var tr = new Rect(x, findRow.y - rowH * 5.6f - gap * 5, w, rowH);
                DevGui.Fill(tr, DevGui.Blue);
                GUI.Label(new Rect(tr.x + 15 * s, tr.y + 20 * s, tr.width * 0.6f, rowH), toast, label);
                if (GUI.Button(new Rect(tr.xMax - w * 0.3f, tr.y + 8 * s, w * 0.3f - 8 * s, rowH - 16 * s), "Undo", button))
                {
                    toastUntil = 0f;
                    search.UndoChecked();
                }
            }

            // Spot panel: the cue being looked at, with its dwell bar (and manual buttons when asked).
            if (panelOn)
            {
                var pr = new Rect(x, findRow.y - rowH * 4.4f - gap * 4, w, rowH * 1.1f);
                DevGui.Fill(pr, DevGui.Shade);
                GUI.Label(new Rect(pr.x + 15 * s, pr.y + 5 * s, pr.width, pr.height * 0.6f), $"{panelLabel}  ·  {panelTip}", label);
                DevGui.Fill(new Rect(pr.x, pr.yMax - 14 * s, pr.width * panelDwell, 14 * s), DevGui.Pink);
                if (panelManual)
                {
                    float bw2 = (w - gap) / 2f;
                    var br = new Rect(x, pr.y - rowH - gap, bw2, rowH);
                    if (GUI.Button(br, "✓ Checked", button)) search.MarkChecked();
                    if (GUI.Button(new Rect(br.xMax + gap, br.y, bw2, rowH), "Found it", button)) search.MarkFound();
                }
            }

            // Scan button, only while scanning is allowed.
            if (scanEnabled && GUI.Button(new Rect(x, findRow.y - rowH * 2.2f - gap * 2, w, rowH * 1.2f), "Scan this area", button))
                search.ScanArea();

            // Presets: typing on a phone through IMGUI is clumsy; one tap is faster for testing.
            string[] presets = { "black wallet", "keys", "glasses" };
            float pw = (w - gap * (presets.Length - 1)) / presets.Length;
            for (int i = 0; i < presets.Length; i++)
                if (GUI.Button(new Rect(x + i * (pw + gap), findRow.y - rowH * 0.8f - gap, pw, rowH * 0.8f), presets[i], button))
                    input = presets[i];
        }
    }
}
