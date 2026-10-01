using TMPro;
using UnityEngine;

namespace FindMe.Onboarding
{
    /// <summary>
    /// Every color, font and shape the UI uses ("Sherbet Pop", Docs/design/DESIGN_SPEC.md §2).
    /// Screens never hard-code a color: they read it from here, so a palette change is one asset edit.
    /// Create/refresh the asset with the menu FindMe ▸ UI ▸ 1. Setup fonts + theme.
    /// </summary>
    [CreateAssetMenu(menuName = "FindMe/UI Theme", fileName = "OnboardingTheme")]
    public class OnboardingTheme : ScriptableObject
    {
        [Header("Sherbet Pop colors")]
        public Color grape = Hex(0x3A2E6B);          // all text on light, dark buttons
        public Color grapeDark = Hex(0x2B2250);      // HUD chips on camera
        public Color bubblegum = Hex(0xFF7EB6);      // brand, primary button
        public Color bubblegumEdge = Hex(0xD9559A);
        public Color bubblegumLight = Hex(0xFFE5F1);
        public Color mint = Hex(0x4FE3A8);           // success, found
        public Color mintEdge = Hex(0x26B07B);
        public Color mintLight = Hex(0xDCF9EE);
        public Color lavender = Hex(0xC3B1FF);       // calm, secondary cards
        public Color lavenderEdge = Hex(0x9A84EA);
        public Color lavenderLight = Hex(0xEFEAFF);
        public Color lemon = Hex(0xFFE45C);          // highlights, premium
        public Color lemonEdge = Hex(0xE0C43A);
        public Color lemonLight = Hex(0xFFF8CC);
        public Color soda = Hex(0x6CC4FF);           // "checked"
        public Color sodaLight = Hex(0xE2F3FF);
        public Color tangerine = Hex(0xFFA77A);      // gentle nudges
        public Color marshmallow = Hex(0xFFF7FB);    // app background
        public Color cloud = Hex(0xE4E0F7);          // borders, dividers, empty bars
        public Color muted = Hex(0x655A8A);          // secondary text
        public Color faint = Hex(0x9D96B0);          // hints, inactive tabs
        public Color white = Color.white;

        [Header("Fonts (TextMeshPro)")]
        public TMP_FontAsset heading;    // Fredoka SemiBold: titles
        public TMP_FontAsset body;       // Nunito Regular
        public TMP_FontAsset bodyBold;   // Nunito Bold
        public TMP_FontAsset bodyBlack;  // Nunito ExtraBold: buttons, labels (the prototype's weight 800)

        [Header("Shapes (white, tinted by Image.color)")]
        public Sprite rounded;           // rounded_28, 9-sliced; any radius via UIKit.Round
        public Sprite circle;

        [Header("Art")]
        public Sprite logoBackground, logoKey, logoKeySad, bracket, crownFill, crownLine, ring, arc;
        [Tooltip("Icons by file name without 'icon_' (e.g. 'camera'). Filled by the setup menu.")]
        public Sprite[] icons;

        [Header("Accessibility")]
        [Tooltip("Swap movement for short fades and stop looping animations.")]
        public bool reduceMotion;

        /// <summary>Icon sprite by short name ("camera", "search"…), or null.</summary>
        public Sprite Icon(string name)
        {
            foreach (var s in icons) if (s != null && s.name == "icon_" + name) return s;
            Debug.LogWarning($"[UI] Missing icon '{name}'");
            return null;
        }

        static Color Hex(uint rgb) => new Color32((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, 255);
    }
}
