using System.Collections.Generic;
using UnityEngine;

namespace FindMe
{
    /// <summary>
    /// Loads prompt templates from Resources/FindMePrompts/*.txt and fills {placeholders}.
    /// Keeping prompts as text lets the AI lead tune them without code, and the eval script
    /// (Docs/ai/eval/run_eval.py) reads the very same files.
    /// </summary>
    public static class PromptLibrary
    {
        static readonly Dictionary<string, string> cache = new();

        public static string Template(string name)
        {
            if (cache.TryGetValue(name, out var t)) return t;
            var asset = Resources.Load<TextAsset>("FindMePrompts/" + name);
            if (asset == null) throw new System.Exception($"Prompt FindMePrompts/{name}.txt is missing");
            return cache[name] = asset.text;
        }

        /// <summary>Template with every {key} replaced; unknown keys are left as-is so mistakes are visible.</summary>
        public static string Fill(string name, params (string key, string value)[] values)
        {
            string s = Template(name);
            foreach (var (key, value) in values) s = s.Replace("{" + key + "}", value ?? "");
            return s;
        }
    }
}
