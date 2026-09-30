using System;
using UnityEngine;

namespace FindMe
{
    /// <summary>One place the item could be hiding (Gemini Call 2). Field names match the JSON keys.</summary>
    [Serializable]
    public class HidingSpot
    {
        public string label;       // "drawer"
        public int[] box_2d;       // [ymin, xmin, ymax, xmax], 0..1000, origin TOP-left of the image
        public int likelihood;     // 1..3 (3 = most likely)
        public string hint;        // "Wallets slide under clothes"
        public string seen;        // what Gemini says is really there, e.g. "grey backpack on chair" (grounding + debug)

        /// <summary>
        /// Cleans up what the model sent. Returns false if the spot is unusable (no label or no box),
        /// so the caller can drop it instead of crashing on it later.
        /// </summary>
        public bool Sanitize()
        {
            if (string.IsNullOrWhiteSpace(label) || box_2d == null || box_2d.Length != 4) return false;
            for (int i = 0; i < 4; i++) box_2d[i] = Mathf.Clamp(box_2d[i], 0, 1000);
            // Models occasionally swap min/max; a flipped box would still sample fine but looks wrong in debug.
            if (box_2d[0] > box_2d[2]) (box_2d[0], box_2d[2]) = (box_2d[2], box_2d[0]);
            if (box_2d[1] > box_2d[3]) (box_2d[1], box_2d[3]) = (box_2d[3], box_2d[1]);
            // A box under 1% of the image in width or height isn't a real hiding place (and can't be raycast well).
            if (box_2d[2] - box_2d[0] < 10 || box_2d[3] - box_2d[1] < 10) return false;
            label = label.Trim();
            likelihood = Mathf.Clamp(likelihood, 1, 3);
            hint ??= "";
            seen ??= "";
            return true;
        }
    }
}
