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
        GUIStyle label, button, field;

        void Awake()
        {
            if (search == null) search = FindAnyObjectByType<SearchController>();
        }

        // ---------- ISearchUI ----------
        public void ShowItemCard(ItemProfile p) => itemCard = "Looking for: " + p.Summary;
        public void ShowStatus(string message) => status = message;
        public void HideStatus() => status = null;
        public void SetReadiness(float p01) => readiness = p01;
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
