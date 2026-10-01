using UnityEngine;

namespace FindMe.UI
{
    /// <summary>
    /// The FIND ME logo built from layers, so the 4 mint corner brackets can move on their own:
    /// background tile → brackets → key (the key overlaps the bottom-left bracket, as in the design).
    /// With `pulse`, the brackets zoom out and back in like a camera finding focus (splash screen).
    /// </summary>
    public static class LogoMark
    {
        public static RectTransform Create(Transform parent, float size, bool pulse, bool sad = false)
        {
            var T = UIKit.T;
            var root = UIKit.Rect(parent, "Logo");
            root.sizeDelta = new Vector2(size, size);
            UIKit.Size(root, size, size);
            float u = size / 100f;          // the art is drawn on a 100 × 100 grid (prototype mascot SVG)

            UIKit.Stretch((RectTransform)UIKit.Sprite(root, "Tile", T.logoBackground, Color.white, size, size).transform);

            // bracket.png is the top-left corner (grid 14..35, so 21 units wide, centred 25.5 units from the middle).
            // The other three are the same sprite turned 90° at a time (positive z = counter-clockwise).
            AddBracket(root, u, -1, +1, 0f, pulse);     // top-left
            AddBracket(root, u, +1, +1, -90f, pulse);   // top-right
            AddBracket(root, u, +1, -1, 180f, pulse);   // bottom-right
            AddBracket(root, u, -1, -1, 90f, pulse);    // bottom-left

            UIKit.Stretch((RectTransform)UIKit.Sprite(root, "Key", sad ? T.logoKeySad : T.logoKey, Color.white, size, size).transform);
            return root;
        }

        static void AddBracket(RectTransform root, float u, int sx, int sy, float angle, bool pulse)
        {
            var img = UIKit.Sprite(root, "Bracket", UIKit.T.bracket, UIKit.T.mint, 21f * u, 21f * u);
            var rt = (RectTransform)img.transform;
            UIKit.Place(rt, new Vector2(0.5f, 0.5f), new Vector2(21f * u, 21f * u), new Vector2(sx, sy) * 25.5f * u);
            rt.localEulerAngles = new Vector3(0, 0, angle);
            if (!pulse) return;

            // Each corner moves 4 grid units outward, diagonally, and back: 1.8 s per in-out cycle.
            var loop = img.gameObject.AddComponent<LoopMotion>();
            loop.period = 1.8f;
            loop.move = new Vector2(sx, sy) * 4f * u;
        }
    }
}
