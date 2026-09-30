using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.XR.ARFoundation;
using FindMe.Dev;
using ETouch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace FindMe
{
    /// <summary>
    /// Debug overlay (plan Section 9). IMGUI panel toggled by the "DBG" button or a 3-finger tap.
    /// Shows FPS, AR/tracking, search state, Gemini mode + recent calls, the parsed profile,
    /// and the last snapshot thumbnail (what Gemini actually saw).
    /// </summary>
    public class DebugOverlay : MonoBehaviour
    {
        [SerializeField] SearchController search;
        [SerializeField] GeminiServiceProvider gemini;
        [SerializeField] ARReadiness readiness;
        [SerializeField] ARPlaneManager planeManager;
        [SerializeField] SnapshotCapture snapshot;
        [SerializeField] SpotPlacer placer;
        [SerializeField] SpotFocus focus;
        [SerializeField] bool visible = true;

        float fps;
        bool threeFingersDown;
        readonly StringBuilder sb = new();
        GUIStyle text, button, small, tagStyle;
        Snapshot testSnap;     // from the "Snap" button; owned (and destroyed) by this overlay

        void OnEnable() => EnhancedTouchSupport.Enable();

        void OnDestroy()
        {
            if (testSnap != null) Destroy(testSnap.texture);
        }

        void Update()
        {
            fps = Mathf.Lerp(fps, 1f / Mathf.Max(Time.unscaledDeltaTime, 0.0001f), 0.05f);

            // 3-finger tap toggles. Edge-triggered so holding 3 fingers doesn't flicker it.
            bool three = ETouch.activeTouches.Count >= 3;
            if (three && !threeFingersDown) visible = !visible;
            threeFingersDown = three;
        }

        void OnGUI()
        {
            if (SnapshotCapture.IsCapturing) return;   // the overlay must never end up in a snapshot
            float s = DevGui.Scale;
            text ??= DevGui.Style(GUI.skin.label, 26, Color.white);
            button ??= DevGui.Style(GUI.skin.button, 28);
            small ??= DevGui.Style(GUI.skin.label, 22, Color.white);

            float top = DevGui.SafeTop + 380 * s;
            if (GUI.Button(new Rect(Screen.width - 150 * s, top, 120 * s, 80 * s), "DBG", button)) visible = !visible;
            if (!visible) return;

            var panel = new Rect(20 * s, top, 640 * s, 0f);
            float y = panel.y + 10 * s;

            string info = BuildText();
            panel.height = text.CalcHeight(new GUIContent(info), panel.width - 20 * s) + 20 * s + 90 * s;
            DevGui.Fill(panel, DevGui.Shade);
            GUI.Label(new Rect(panel.x + 10 * s, y, panel.width - 20 * s, panel.height), info, text);

            // Buttons row at the bottom of the panel.
            float by = panel.yMax - 85 * s, bw = 200 * s, bh = 75 * s;
            if (gemini != null && GUI.Button(new Rect(panel.x + 10 * s, by, bw, bh), gemini.IsMock ? "Mock: ON" : "Mock: OFF", button))
                gemini.SetMock(!gemini.IsMock);
            if (snapshot != null && GUI.Button(new Rect(panel.x + 20 * s + bw, by, bw, bh), "Snap", button))
                StartCoroutine(snapshot.Capture(768, true, OnTestSnap));
            if (placer != null && GUI.Button(new Rect(panel.x + 30 * s + 2 * bw, by, bw, bh), "Clear cues", button))
                placer.ClearCues();

            DrawThumbnail(new Rect(panel.xMax + 20 * s, top + 100 * s, Screen.width - panel.xMax - 40 * s, 0f));
        }

        void OnTestSnap(Snapshot snap)
        {
            if (snap == null) return;
            if (testSnap != null) Destroy(testSnap.texture);
            testSnap = snap;
        }

        /// <summary>
        /// Newest snapshot (scan or test) scaled to the given width, with Gemini's boxes on top when it's a scan.
        /// Fastest way to tell who's wrong: if boxes miss the furniture HERE, it's Gemini; if they're right
        /// here but the 3D cues are off, it's our box → ray → anchor math.
        /// </summary>
        void DrawThumbnail(Rect area)
        {
            Snapshot scan = search != null ? search.LastScan : null;
            bool showScan = scan != null && scan.texture != null && (testSnap == null || scan.pose.time >= testSnap.pose.time);
            Snapshot snap = showScan ? scan : testSnap;
            if (snap == null || snap.texture == null) return;

            float s = DevGui.Scale;
            var tex = snap.texture;
            var r = new Rect(area.x, area.y, area.width, area.width * tex.height / tex.width);
            DevGui.Fill(new Rect(r.x - 4 * s, r.y - 4 * s, r.width + 8 * s, r.height + 8 * s), Color.white);
            GUI.DrawTexture(r, tex, ScaleMode.StretchToFill);
            if (showScan) DrawBoxes(r, search.LastSpots);
            GUI.Label(new Rect(r.x, r.yMax + 6 * s, r.width, 60 * s),
                      $"{tex.width}×{tex.height}  {snap.jpg.Length / 1024}KB  {Time.time - snap.pose.time:0}s ago", small);
        }

        void DrawBoxes(Rect img, IReadOnlyList<HidingSpot> spots)
        {
            float s = DevGui.Scale;
            tagStyle ??= DevGui.Style(GUI.skin.label, 18, Color.black);
            for (int i = 0; i < spots.Count; i++)
            {
                int[] b = spots[i].box_2d;   // [ymin, xmin, ymax, xmax], 0..1000
                // IMGUI's origin is top-left, same as Gemini's, so NO Y flip here (unlike Unity screen space).
                var box = new Rect(img.x + b[1] / 1000f * img.width, img.y + b[0] / 1000f * img.height,
                                   (b[3] - b[1]) / 1000f * img.width, (b[2] - b[0]) / 1000f * img.height);
                Color c = spots[i].likelihood >= 3 ? DevGui.Pink : spots[i].likelihood == 2 ? DevGui.Lemon : DevGui.Blue;
                DevGui.Outline(box, c, 3f * s);
                var tag = new Rect(box.x, box.y, box.width, 30 * s);
                DevGui.Fill(tag, c);
                GUI.Label(tag, $" {i + 1}. {spots[i].label} ({spots[i].likelihood})", tagStyle);
            }
        }

        string BuildText()
        {
            sb.Clear();
            sb.Append($"FPS {fps:0}   AR {ARSession.state}\n");
            if (planeManager != null && readiness != null)
                sb.Append($"planes {planeManager.trackables.count} ({readiness.PlaneArea:0.00} m²)  " +
                          $"moved {readiness.MovedMeters:0.0} m  turned {readiness.RotatedDegrees:0}°  ready {readiness.Progress01 * 100f:0}%\n");
            if (search != null) sb.Append($"state {search.State}\n");
            if (placer != null) sb.Append($"cues {placer.Cues.Count}\n");
            if (focus != null)
                sb.Append($"focus {(focus.Focused != null ? focus.Focused.Label : "-")}  dwell {focus.Dwell01 * 100f:0}%  " +
                          $"{(focus.IsSteady ? "steady" : "MOVING")} {focus.AngularSpeed:0}°/s {focus.LinearSpeed:0.00}m/s\n");

            if (gemini != null) sb.Append($"Gemini: {gemini.ModeLabel}   in flight {GeminiLog.InFlight}\n");
            foreach (var e in GeminiLog.Recent)
                sb.Append($"  {e.call} {e.model} {e.kb:0}KB {e.ms}ms {(e.ok ? "ok" : "FAIL")}\n");
            if (GeminiLog.Recent.Count > 0) sb.Append($"last: {GeminiLog.Recent[^1].raw}\n");

            if (search != null && search.Profile != null)
                sb.Append($"profile: {JsonConvert.SerializeObject(search.Profile)}\n");
            return sb.ToString();
        }
    }
}
