using System;
using TMPro;
using UnityEngine;

namespace FindMe.UI
{
    public enum ButtonStyle { Primary, Success, Calm, Secondary, Dark }

    /// <summary>
    /// Single source of truth for colors, fonts and sprites (DESIGN_SPEC section 2).
    /// Screens never hard-code a color; they read it from here.
    /// </summary>
    [CreateAssetMenu(menuName = "FindMe/UI Theme", fileName = "UITheme")]
    public class UITheme : ScriptableObject
    {
        /// <summary>
        /// Design frames are 390x844 (iPhone 13). The Canvas reference is 1170x2532, exactly 3x,
        /// so "18 px in the design" = Px(18) = 54 Unity canvas pixels.
        /// </summary>
        public const float K = 3f;
        public static float Px(float designPx) => designPx * K;
        public static readonly Vector2 ReferenceResolution = new Vector2(1170f, 2532f);

        [Header("Colors (Sherbet Pop)")]
        public Color grape = Hex("3A2E6B");
        public Color grapeDark = Hex("2B2250");
        public Color bubblegum = Hex("FF7EB6");
        public Color bubblegumEdge = Hex("D9559A");
        public Color mint = Hex("4FE3A8");
        public Color mintEdge = Hex("26B07B");
        public Color lavender = Hex("C3B1FF");
        public Color lavenderEdge = Hex("9A84EA");
        public Color lemon = Hex("FFE45C");
        public Color soda = Hex("6CC4FF");
        public Color tangerine = Hex("FFA77A");
        public Color marshmallow = Hex("FFF7FB");
        public Color cloud = Hex("E4E0F7");
        public Color cloudEdge = Hex("D8D2EE");   // secondary button edge
        public Color muted = Hex("655A8A");
        public Color white = Color.white;

        [Header("Fonts (TMP)")]
        public TMP_FontAsset heading;      // Fredoka SemiBold: headings and buttons
        public TMP_FontAsset bodyRegular;  // Nunito
        public TMP_FontAsset bodyBold;
        public TMP_FontAsset bodyExtraBold;

        [Header("9-sliced sprites (white, tinted with Image.color)")]
        public Sprite rounded18;  // buttons, inputs
        public Sprite rounded28;  // cards, sheets
        public Sprite pill;       // chips, bars
        public Sprite circle;

        [Serializable] public struct ItemIcon { public string keyword; public Sprite icon; }

        [Header("Item icons (grape art, used with a white tint)")]
        public ItemIcon[] itemIcons;
        public Sprite fallbackItemIcon;

        /// <summary>Picks an icon for "black wallet", "house keys", ... by keyword; falls back to a tag icon.</summary>
        public Sprite GetItemIcon(string itemName)
        {
            string n = (itemName ?? "").ToLowerInvariant();
            if (itemIcons != null)
                foreach (var e in itemIcons)
                    if (!string.IsNullOrEmpty(e.keyword) && n.Contains(e.keyword)) return e.icon;
            return fallbackItemIcon;
        }

        static Color Hex(string h) { ColorUtility.TryParseHtmlString("#" + h, out var c); return c; }

        /// <summary>Face / edge / label colors for each chunky button variant (DESIGN_SPEC section 5).</summary>
        public void GetButtonColors(ButtonStyle s, out Color face, out Color edge, out Color label)
        {
            switch (s)
            {
                case ButtonStyle.Success:   face = mint;      edge = mintEdge;      label = grape; break;
                case ButtonStyle.Calm:      face = lavender;  edge = lavenderEdge;  label = grape; break;
                case ButtonStyle.Secondary: face = white;     edge = cloudEdge;     label = grape; break;
                case ButtonStyle.Dark:      face = grape;     edge = grapeDark;     label = white; break;
                default:                    face = bubblegum; edge = bubblegumEdge; label = grape; break; // Primary
            }
        }
    }
}
