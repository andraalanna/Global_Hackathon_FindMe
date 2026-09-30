using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FindMe.EditorTools
{
    /// <summary>
    /// Draws the simple white shapes the cues use (ring, disc, bubble, check, star, arrow) into PNG sprites,
    /// so we need no art files. White = tinted per state with Image.color. A designer can replace any PNG.
    /// </summary>
    public static class CueArtBaker
    {
        public const string Folder = "Assets/_Team/AR/Textures/Cue";

        public static Sprite Ring => Load("Ring");
        public static Sprite Disc => Load("Disc");
        public static Sprite Bubble => Load("Bubble");
        public static Sprite Check => Load("Check");
        public static Sprite Star => Load("Star");
        public static Sprite Arrow => Load("Arrow");

        public static void BakeAll()
        {
            Directory.CreateDirectory(Folder);
            // p = pixel centre in -0.5..0.5 (y up). Each function says whether p is inside the shape.
            Bake("Ring", 256, p => p.magnitude is >= 0.37f and <= 0.48f);
            Bake("Disc", 128, p => p.magnitude <= 0.48f);
            // Rounded rectangle for a 9-sliced bubble: 96 px, 40 px corners, border 44 so corners never stretch.
            Bake("Bubble", 96, p => RoundedBox(p * 96f, new Vector2(47f, 47f), 40f), new Vector4(44, 44, 44, 44));
            Bake("Check", 128, p => Segment(p, new(-0.30f, 0.02f), new(-0.08f, -0.22f)) < 0.075f ||
                                     Segment(p, new(-0.08f, -0.22f), new(0.32f, 0.24f)) < 0.075f);
            Bake("Star", 128, p => InsideStar(p, 0.48f, 0.21f));
            Bake("Arrow", 128, p => InsideTriangle(p, new(0.45f, 0f), new(-0.35f, 0.38f), new(-0.35f, -0.38f)));
            AssetDatabase.Refresh();
        }

        static Sprite Load(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"{Folder}/{name}.png");

        static void Bake(string name, int size, Func<Vector2, bool> inside, Vector4 border = default)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var px = new Color32[size * size];
            const int ss = 4;   // 4×4 supersampling = smooth (anti-aliased) edges
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                int hits = 0;
                for (int sy = 0; sy < ss; sy++)
                for (int sx = 0; sx < ss; sx++)
                {
                    var p = new Vector2((x + (sx + 0.5f) / ss) / size - 0.5f, (y + (sy + 0.5f) / ss) / size - 0.5f);
                    if (inside(p)) hits++;
                }
                px[y * size + x] = new Color32(255, 255, 255, (byte)(255 * hits / (ss * ss)));
            }
            tex.SetPixels32(px);

            string path = $"{Folder}/{name}.png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);

            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled = true;          // world-space: seen small and far, mipmaps stop shimmering
            imp.spriteBorder = border;
            imp.SaveAndReimport();
        }

        static bool RoundedBox(Vector2 p, Vector2 half, float r)
        {
            Vector2 q = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - half + new Vector2(r, r);
            return Vector2.Max(q, Vector2.zero).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - r <= 0f;
        }

        static float Segment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return Vector2.Distance(p, a + t * ab);
        }

        static bool InsideTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float Cross(Vector2 u, Vector2 v, Vector2 w) => (v.x - u.x) * (w.y - u.y) - (v.y - u.y) * (w.x - u.x);
            float d1 = Cross(a, b, p), d2 = Cross(b, c, p), d3 = Cross(c, a, p);
            return !((d1 < 0 || d2 < 0 || d3 < 0) && (d1 > 0 || d2 > 0 || d3 > 0));
        }

        static bool InsideStar(Vector2 p, float outer, float inner)
        {
            // 10 points alternating outer/inner radius; even-odd rule point-in-polygon.
            var pts = new Vector2[10];
            for (int i = 0; i < 10; i++)
            {
                float ang = Mathf.PI / 2f + i * Mathf.PI / 5f;
                float r = i % 2 == 0 ? outer : inner;
                pts[i] = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * r;
            }
            bool inside = false;
            for (int i = 0, j = 9; i < 10; j = i++)
                if ((pts[i].y > p.y) != (pts[j].y > p.y) &&
                    p.x < (pts[j].x - pts[i].x) * (p.y - pts[i].y) / (pts[j].y - pts[i].y) + pts[i].x)
                    inside = !inside;
            return inside;
        }
    }
}
