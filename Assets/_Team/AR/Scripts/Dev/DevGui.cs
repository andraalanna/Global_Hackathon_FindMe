using UnityEngine;

namespace FindMe.Dev
{
    /// <summary>Shared IMGUI helpers for the temporary dev screens (DevUI, DebugOverlay).</summary>
    public static class DevGui
    {
        public static readonly Color Pink = new Color32(0xFF, 0x7E, 0xB6, 0xFF);
        public static readonly Color Mint = new Color32(0x4F, 0xE3, 0xA8, 0xFF);
        public static readonly Color Lemon = new Color32(0xFF, 0xE0, 0x66, 0xFF);
        public static readonly Color Blue = new Color32(0x6C, 0xC4, 0xFF, 0xFF);
        public static readonly Color Shade = new Color(0f, 0f, 0f, 0.55f);

        /// <summary>Layouts are designed at 1080 px wide (the A80) and scaled, so the editor looks the same.</summary>
        public static float Scale => Screen.width / 1080f;

        public static GUIStyle Style(GUIStyle baseStyle, int designSize, Color? color = null)
        {
            var s = new GUIStyle(baseStyle) { fontSize = Mathf.RoundToInt(designSize * Scale), wordWrap = true };
            if (color.HasValue) s.normal.textColor = color.Value;
            return s;
        }

        public static void Fill(Rect r, Color c)
        {
            var old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = old;
        }

        /// <summary>Rectangle outline made of four thin filled rects.</summary>
        public static void Outline(Rect r, Color c, float t)
        {
            Fill(new Rect(r.x, r.y, r.width, t), c);
            Fill(new Rect(r.x, r.yMax - t, r.width, t), c);
            Fill(new Rect(r.x, r.y, t, r.height), c);
            Fill(new Rect(r.xMax - t, r.y, t, r.height), c);
        }

        /// <summary>Top of the usable area in IMGUI coordinates (IMGUI is top-left origin, safeArea is bottom-left).</summary>
        public static float SafeTop => Screen.height - Screen.safeArea.yMax;
        public static float SafeBottom => Screen.height - Screen.safeArea.yMin;
    }
}
