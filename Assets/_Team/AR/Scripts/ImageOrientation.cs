using UnityEngine;

namespace FindMe
{
    /// <summary>Which edge of the SCREEN image the real-world "up" (the ceiling) points to when a photo is taken.</summary>
    public enum ImageUp { Top, Right, Bottom, Left }

    /// <summary>
    /// Lets the user hold the phone sideways to scan (it sees ~2× more of the room's width).
    /// The app stays portrait-locked, so the screen image is sideways; we send Gemini an UPRIGHT copy
    /// and keep Gemini's boxes in upright coordinates. These functions convert between the two.
    /// All points here are normalized 0..1 with origin TOP-left (Gemini's convention).
    /// </summary>
    public static class ImageOrientation
    {
        /// <summary>
        /// Where world-up points on screen, from the camera's own axes (camera x = screen right, y = screen up).
        /// Sideways only wins clearly (+0.2), so a phone pointed at the floor doesn't flip-flop.
        /// </summary>
        public static ImageUp Detect(Transform camera)
        {
            Vector3 up = camera.InverseTransformDirection(Vector3.up);
            if (Mathf.Abs(up.x) > Mathf.Abs(up.y) + 0.2f) return up.x > 0f ? ImageUp.Right : ImageUp.Left;
            return up.y >= 0f ? ImageUp.Top : ImageUp.Bottom;
        }

        /// <summary>Point in the upright image → same point in the screen image.</summary>
        public static Vector2 UprightToScreen(Vector2 u, ImageUp up) => up switch
        {
            // Up = screen's right edge: the upright image is the screen image turned 90° counter-clockwise.
            ImageUp.Right => new Vector2(1f - u.y, u.x),
            // Up = screen's left edge: turned 90° clockwise.
            ImageUp.Left => new Vector2(u.y, 1f - u.x),
            ImageUp.Bottom => new Vector2(1f - u.x, 1f - u.y),
            _ => u,
        };

        /// <summary>Point in the screen image → same point in the upright image (inverse of <see cref="UprightToScreen"/>).</summary>
        public static Vector2 ScreenToUpright(Vector2 s, ImageUp up) => up switch
        {
            ImageUp.Right => new Vector2(s.y, 1f - s.x),
            ImageUp.Left => new Vector2(1f - s.y, s.x),
            ImageUp.Bottom => new Vector2(1f - s.x, 1f - s.y),
            _ => s,
        };

        static bool Sideways(ImageUp up) => up == ImageUp.Left || up == ImageUp.Right;

        /// <summary>
        /// Returns an upright copy of a screen-image texture (or the same texture if it's already upright).
        /// Uses the exact same mapping as the boxes, so image and boxes can never disagree.
        /// </summary>
        public static Texture2D MakeUpright(Texture2D screen, ImageUp up)
        {
            if (up == ImageUp.Top) return screen;

            int ws = screen.width, hs = screen.height;
            int wu = Sideways(up) ? hs : ws, hu = Sideways(up) ? ws : hs;
            Color32[] src = screen.GetPixels32();
            var dst = new Color32[wu * hu];

            // Texture rows go BOTTOM-up, so row j is at top-left y = 1 - (j + 0.5) / height.
            for (int j = 0; j < hu; j++)
            {
                float yu = 1f - (j + 0.5f) / hu;
                for (int i = 0; i < wu; i++)
                {
                    Vector2 s = UprightToScreen(new Vector2((i + 0.5f) / wu, yu), up);
                    int si = Mathf.Min((int)(s.x * ws), ws - 1);
                    int sj = Mathf.Min((int)((1f - s.y) * hs), hs - 1);
                    dst[j * wu + i] = src[sj * ws + si];
                }
            }

            var result = new Texture2D(wu, hu, TextureFormat.RGBA32, false, !screen.isDataSRGB);
            result.SetPixels32(dst);
            result.Apply();
            return result;
        }
    }
}
