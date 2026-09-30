using System;
using UnityEngine;

namespace FindMe
{
    /// <summary>All Gemini knobs in one asset, so swapping a model is an Inspector change, not a code change.</summary>
    [CreateAssetMenu(menuName = "FindMe/Gemini Config", fileName = "GeminiConfig")]
    public class GeminiConfig : ScriptableObject
    {
        // The plan's gemini-2.5-* models return 404 "no longer available to new users" for our key (Oct 2026).
        // Gemini 3.x rejects thinkingBudget and wants thinkingLevel instead; lite models accept "minimal",
        // flash models only "low" and up. Hence model + thinking are configured together.
        const string Minimal = "{\"thinkingLevel\":\"minimal\"}";
        const string Low = "{\"thinkingLevel\":\"low\"}";

        [Serializable]
        public class ModelChoice
        {
            public string model;
            [Tooltip("Raw JSON for generationConfig.thinkingConfig. Empty = don't send one.")]
            public string thinkingJson;
            public ModelChoice(string model, string thinkingJson) { this.model = model; this.thinkingJson = thinkingJson; }
        }

        [Header("Models")]
        [Tooltip("Call 1: text only, easy task, so the fast lite model is enough.")]
        public ModelChoice profile = new("gemini-3.5-flash-lite", Minimal);
        [Tooltip("Call 2: box quality matters most here.")]
        public ModelChoice spots = new("gemini-3.8-flash", Low);
        [Tooltip("Call 3: runs often, must be fast.")]
        public ModelChoice check = new("gemini-3.5-flash-lite", Minimal);
        [Tooltip("Used for the one retry of Calls 1-2 when the first try fails with a network error or 429/5xx (\"high demand\").")]
        public ModelChoice fallback = new("gemini-3.5-flash-lite", Minimal);

        [Header("HTTP")]
        public string endpointFormat = "https://generativelanguage.googleapis.com/v1beta/models/{0}:generateContent";
        public int profileTimeoutSec = 8;
        public int spotsTimeoutSec = 8;
        public int checkTimeoutSec = 5;
        [Range(0f, 1f)] public float temperature = 0.2f;

        [Header("Key")]
        [Tooltip("Resources path of the key TextAsset: Assets/_Team/AI/Secrets/Resources/gemini_key.txt")]
        public string keyResource = "gemini_key";
    }
}
