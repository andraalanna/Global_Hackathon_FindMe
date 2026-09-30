using UnityEngine;

namespace FindMe.Dev
{
    /// <summary>
    /// TEMPORARY readiness meter, drawn with IMGUI so it needs no Canvas or UI lead prefab.
    /// Remove (or disable) once the UI lead's UIManager.SetReadiness is wired.
    /// Note: IMGUI shows up in ScreenCapture, so it must be off before Phase 3 snapshots.
    /// </summary>
    public class ReadinessMeterDev : MonoBehaviour
    {
        [SerializeField] ARReadiness readiness;

        static readonly Color Pink = new Color32(0xFF, 0x7E, 0xB6, 0xFF);
        static readonly Color Mint = new Color32(0x4F, 0xE3, 0xA8, 0xFF);
        GUIStyle style;

        void Awake()
        {
            if (readiness == null) readiness = FindAnyObjectByType<ARReadiness>();
        }

        void OnGUI()
        {
            if (readiness == null) return;

            // Design at 1080 px wide (the A80's width) and scale, so it also looks sane in the editor.
            float s = Screen.width / 1080f;
            // IMGUI's origin is top-left; safeArea's is bottom-left. This is the top inset in IMGUI terms.
            float top = Screen.height - Screen.safeArea.yMax + 40f * s;

            style ??= new GUIStyle(GUI.skin.label);
            style.fontSize = Mathf.RoundToInt(36 * s);
            style.normal.textColor = Color.white;

            var bar = new Rect(40 * s, top, Screen.width - 80 * s, 36 * s);
            GUI.color = new Color(0f, 0f, 0f, 0.5f);
            GUI.DrawTexture(bar, Texture2D.whiteTexture);
            GUI.color = readiness.IsReady ? Mint : Pink;
            GUI.DrawTexture(new Rect(bar.x, bar.y, bar.width * readiness.Progress01, bar.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            string text =
                $"{(readiness.IsReady ? "READY" : "Move your phone slowly around the room")}  {readiness.Progress01 * 100f:0}%\n" +
                $"tracking: {readiness.IsTracking}   planes: {readiness.PlaneArea:0.00} m²\n" +
                $"moved: {readiness.MovedMeters:0.00} m   turned: {readiness.RotatedDegrees:0}°";
            GUI.Label(new Rect(bar.x, bar.yMax + 10 * s, bar.width, 200 * s), text, style);
        }
    }
}
