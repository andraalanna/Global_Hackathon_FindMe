using System.Collections;
using TMPro;
using UnityEngine;

namespace FindMe
{
    /// <summary>
    /// One hiding-spot cue in the world. PHASE 5 VERSION: data + a placeholder look (pink ball + label)
    /// built in code. Phase 6 replaces the look with the SpotCue prefab (rings, bubble, states).
    /// </summary>
    public class SpotCue : MonoBehaviour
    {
        public string Label { get; private set; }
        public string Hint { get; private set; }
        public int Likelihood { get; set; }
        public int Priority { get; private set; }            // 1 = first to check
        public SpotState State { get; set; } = SpotState.ToCheck;

        [SerializeField] Transform visual;                    // everything that scales/pops
        [SerializeField] Transform labelPivot;                // turns to face the camera (yaw only)
        [SerializeField] TextMeshPro labelText;

        const float PopSeconds = 0.35f;
        Camera cam;

        public void Init(HidingSpot spot, int priority)
        {
            Label = spot.label;
            Hint = spot.hint;
            Likelihood = spot.likelihood;
            Priority = priority;
            name = $"SpotCue {priority}: {Label}";
            if (labelText != null) labelText.text = $"{priority}. {Label}";
            StartCoroutine(Pop());
        }

        void LateUpdate()
        {
            if (cam == null) cam = Camera.main;
            if (cam == null || visual == null || labelPivot == null) return;

            // Constant-ish on-screen size: grow with distance (plan Phase 6 rule, clamped 0.5..4 m).
            float d = Vector3.Distance(cam.transform.position, transform.position);
            if (!popping) visual.localScale = Vector3.one * Mathf.Clamp(d, 0.5f, 4f);

            // Billboard on yaw only so the text stays upright.
            Vector3 toCam = cam.transform.position - labelPivot.position;
            toCam.y = 0f;
            if (toCam.sqrMagnitude > 1e-4f) labelPivot.rotation = Quaternion.LookRotation(-toCam, Vector3.up);
        }

        bool popping;

        /// <summary>Scale 0 → 1.1 → 1 with an ease-out-back curve.</summary>
        IEnumerator Pop()
        {
            popping = true;
            for (float t = 0f; t < PopSeconds; t += Time.deltaTime)
            {
                float target = cam != null ? Mathf.Clamp(Vector3.Distance(cam.transform.position, transform.position), 0.5f, 4f) : 1f;
                visual.localScale = Vector3.one * (target * EaseOutBack(t / PopSeconds));
                yield return null;
            }
            popping = false;
        }

        static float EaseOutBack(float x)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
        }

        /// <summary>Builds the placeholder cue in code (used when SpotPlacer has no prefab assigned).</summary>
        public static SpotCue CreatePlaceholder()
        {
            var root = new GameObject("SpotCue");
            var cue = root.AddComponent<SpotCue>();

            cue.visual = new GameObject("Visual").transform;
            cue.visual.SetParent(root.transform, false);

            // 5 cm pink ball at the spot itself.
            var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.Destroy(ball.GetComponent<Collider>());
            ball.transform.SetParent(cue.visual, false);
            ball.transform.localScale = Vector3.one * 0.05f;
            ball.GetComponent<Renderer>().material.color = new Color32(0xFF, 0x7E, 0xB6, 0xFF);

            // Label 8 cm above the ball.
            var labelGo = new GameObject("Label");
            // Adding TextMeshPro swaps the Transform for a RectTransform, so grab the transform AFTER it.
            cue.labelText = labelGo.AddComponent<TextMeshPro>();
            cue.labelPivot = labelGo.transform;
            cue.labelPivot.SetParent(cue.visual, false);
            cue.labelPivot.localPosition = new Vector3(0f, 0.08f, 0f);
            cue.labelText.fontSize = 0.35f;
            cue.labelText.alignment = TextAlignmentOptions.Center;
            cue.labelText.color = Color.white;
            cue.labelText.outlineWidth = 0.25f;
            cue.labelText.outlineColor = new Color32(0x3A, 0x2E, 0x6B, 0xFF);   // grape
            cue.labelText.rectTransform.sizeDelta = new Vector2(0.6f, 0.1f);
            return cue;
        }
    }
}
