using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace FindMe
{
    /// <summary>What we're looking for (Gemini Call 1). Field names match the JSON keys.</summary>
    [Serializable]
    public class ItemProfile
    {
        public string name;                // "black wallet"
        public float[] sizeCm;             // [width, height], e.g. [11, 9]
        public string shape;               // "flat rectangle"
        public List<string> likelyPlaces;

        [JsonIgnore] public float WidthCm => sizeCm[0];
        [JsonIgnore] public float HeightCm => sizeCm[1];
        [JsonIgnore] public string Summary => $"{name} · {WidthCm:0}×{HeightCm:0} cm · {shape}";

        /// <summary>
        /// Gemini (or a failed call) can leave fields empty. Fill the gaps so later prompts never
        /// print "null": the plan says a failed profile falls back to the user's text + a default size.
        /// </summary>
        public void FillDefaults(string userText)
        {
            if (string.IsNullOrWhiteSpace(name)) name = userText.Trim().ToLowerInvariant();
            if (sizeCm == null || sizeCm.Length < 2 || sizeCm[0] <= 0f || sizeCm[1] <= 0f)
                sizeCm = new[] { 10f, 10f };
            if (string.IsNullOrWhiteSpace(shape)) shape = "small object";
            likelyPlaces ??= new List<string>();
        }
    }
}
