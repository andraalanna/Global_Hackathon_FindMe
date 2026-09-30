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
    /// Shows FPS, AR/tracking, search state, Gemini mode + recent calls, and the parsed profile.
    /// </summary>
    public class DebugOverlay : MonoBehaviour
    {
        [SerializeField] SearchController search;
        [SerializeField] GeminiServiceProvider gemini;
        [SerializeField] ARReadiness readiness;
        [SerializeField] ARPlaneManager planeManager;
        [SerializeField] bool visible = true;

        float fps;
        bool threeFingersDown;
        readonly StringBuilder sb = new();
        GUIStyle text, button;

        void OnEnable() => EnhancedTouchSupport.Enable();

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
            float s = DevGui.Scale;
            text ??= DevGui.Style(GUI.skin.label, 26, Color.white);
            button ??= DevGui.Style(GUI.skin.button, 28);

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
        }

        string BuildText()
        {
            sb.Clear();
            sb.Append($"FPS {fps:0}   AR {ARSession.state}\n");
            if (planeManager != null && readiness != null)
                sb.Append($"planes {planeManager.trackables.count} ({readiness.PlaneArea:0.00} m²)  " +
                          $"moved {readiness.MovedMeters:0.0} m  turned {readiness.RotatedDegrees:0}°  ready {readiness.Progress01 * 100f:0}%\n");
            if (search != null) sb.Append($"state {search.State}\n");

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
