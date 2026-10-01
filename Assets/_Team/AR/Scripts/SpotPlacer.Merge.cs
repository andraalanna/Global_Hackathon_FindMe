using System.Collections.Generic;
using UnityEngine;

namespace FindMe
{
    /// <summary>SpotPlacer, part 2: deciding whether a newly found spot is one we already have a cue for.</summary>
    public partial class SpotPlacer
    {
        // ---------- merge ----------

        /// <summary>
        /// An existing cue is the same spot if the labels are similar AND either
        ///   (a) it's within mergeDistance of the new 3D estimate, or
        ///   (b) seen from the NEW photo's camera, it sits inside the new box.
        /// (b) matters because depth from one photo is a guess (no plane hit = fixed guess along the ray),
        /// so the same drawer scanned from two spots can get 3D estimates half a metre apart.
        /// Where it appears in the photo doesn't depend on that guess.
        /// </summary>
        SpotCue FindDuplicate(HidingSpot spot, Vector3 position, CapturePose pose)
        {
            foreach (var cue in cues)
            {
                if (!LabelsSimilar(cue.Label, spot.label)) continue;
                Vector3 p = cue.transform.position;
                float dist = Vector3.Distance(p, position);
                if (dist <= mergeDistance) return cue;

                Vector3 vp = ghost.WorldToViewport(pose, p);
                if (dist <= reprojectMergeDistance && vp.z > 0f && BoxContainsViewportPoint(spot.box_2d, vp, boxMargin, pose.up))
                    return cue;
            }
            return null;
        }

        /// <summary>
        /// Is a viewport point (0..1, origin BOTTOM-left) inside a Gemini box (0..1000, origin TOP-left)?
        /// The box is grown by <paramref name="margin"/> × its size on every side.
        /// <paramref name="up"/>: how the phone was held for the photo the box belongs to.
        /// </summary>
        public static bool BoxContainsViewportPoint(int[] b, Vector2 vp, float margin, ImageUp up = ImageUp.Top)
        {
            // Viewport (bottom-left) → screen image (top-left) → upright image, where the box lives.
            Vector2 u = ImageOrientation.ScreenToUpright(new Vector2(vp.x, 1f - vp.y), up);
            float x = u.x * 1000f;
            float y = u.y * 1000f;
            float mx = (b[3] - b[1]) * margin, my = (b[2] - b[0]) * margin;
            return x >= b[1] - mx && x <= b[3] + mx && y >= b[0] - my && y <= b[2] + my;
        }

        static readonly string[] Filler = { "the", "of", "on", "in", "a", "an", "top", "under", "side" };

        // Gemini words the same thing differently between scans ("bag" / "backpack"). Map each word to one family.
        static readonly Dictionary<string, string> Synonyms = new()
        {
            { "backpack", "bag" }, { "handbag", "bag" }, { "purse", "bag" }, { "tote", "bag" }, { "rucksack", "bag" },
            { "couch", "sofa" }, { "cushion", "sofa" }, { "cushions", "sofa" },
            { "laundry", "clothes" }, { "clothing", "clothes" }, { "shirts", "clothes" }, { "shirt", "clothes" },
            { "drawers", "drawer" }, { "dresser", "drawer" },
            { "shelves", "shelf" }, { "bookshelf", "shelf" }, { "bookcase", "shelf" },
            { "jacket", "coat" }, { "hoodie", "coat" },
            { "cupboard", "cabinet" }, { "wardrobe", "cabinet" }, { "closet", "cabinet" },
        };

        static string Family(string word) => Synonyms.TryGetValue(word, out var f) ? f : word;

        /// <summary>
        /// "drawer" ≈ "top drawer" (one contains the other); "pile of clothes" ≈ "clothes pile" (shared word);
        /// "backpack" ≈ "bag" (same synonym family).
        /// </summary>
        public static bool LabelsSimilar(string a, string b)
        {
            a = (a ?? "").Trim().ToLowerInvariant();
            b = (b ?? "").Trim().ToLowerInvariant();
            if (a.Length == 0 || b.Length == 0) return false;
            if (a.Contains(b) || b.Contains(a)) return true;

            foreach (var wa in a.Split(' '))
            {
                if (wa.Length < 3 || System.Array.IndexOf(Filler, wa) >= 0) continue;
                foreach (var wb in b.Split(' '))
                    if (Family(wa) == Family(wb)) return true;
            }
            return false;
        }
    }
}
