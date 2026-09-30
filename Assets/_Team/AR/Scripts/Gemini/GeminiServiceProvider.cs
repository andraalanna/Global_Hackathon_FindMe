using UnityEngine;

namespace FindMe
{
    /// <summary>
    /// Chooses real or mock Gemini (plan Section 4). Loads the API key from Resources; if it's missing,
    /// the app silently runs in mock mode and the DebugOverlay says so.
    /// Read <see cref="Service"/> at every call (don't cache it) so the Mock toggle works mid-session.
    /// </summary>
    public class GeminiServiceProvider : MonoBehaviour
    {
        [SerializeField] GeminiConfig config;
        [Tooltip("Force the offline mock even when a key is present.")]
        [SerializeField] bool useMock;

        IGeminiService real;
        readonly IGeminiService mock = new MockGeminiService();

        public bool HasKey => real != null;
        public bool IsMock => useMock || real == null;
        public IGeminiService Service => IsMock ? mock : real;
        public string ModeLabel => !HasKey ? "Mock mode (no key)" : useMock ? "Mock mode (toggled)" : "Real Gemini";

        void Awake()
        {
            if (config == null) config = ScriptableObject.CreateInstance<GeminiConfig>();  // defaults

            // The key file sits in a Resources folder so it's packed into the APK;
            // any other folder can't be read at runtime on Android.
            var keyAsset = Resources.Load<TextAsset>(config.keyResource);
            string key = keyAsset != null ? keyAsset.text.Trim() : null;
            if (keyAsset != null) Resources.UnloadAsset(keyAsset);

            if (!string.IsNullOrEmpty(key)) real = new GeminiClient(key, config);
            Debug.Log($"[Gemini] {ModeLabel}");
        }

        public void SetMock(bool on) => useMock = on;
    }
}
