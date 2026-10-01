using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FindMe
{
    /// <summary>
    /// One hiding-spot cue in the world (plan Phase 6). The prefab (FindMe/4. Build SpotCue Prefab) holds the parts:
    /// a flat ring on the surface, a progress ring, a stem, and a bubble (badge + label + hint) that faces you.
    /// This script only decides what those parts look like for each <see cref="SpotState"/>.
    /// Every state has its own colour AND its own badge icon, so colour-blind users can tell them apart.
    /// </summary>
    public class SpotCue : MonoBehaviour
    {
        [Header("Parts (set by the prefab builder)")]
        [SerializeField] Image groundRing;
        [SerializeField] Image progressRing;       // Image.fillAmount 0..1 = dwell progress
        [SerializeField] LineRenderer stem;
        [SerializeField] Transform bubble;         // faces the camera, keeps a constant size on screen
        [SerializeField] CanvasGroup bubbleGroup;  // fades the whole bubble
        [SerializeField] Image panel;
        [SerializeField] Image badgeIcon;
        [SerializeField] TMP_Text badgeText, labelText, hintText;
        [SerializeField] GameObject nextTag;
        [SerializeField] ParticleSystem foundFx;
        [SerializeField] Sprite checkIcon, starIcon;

        [Header("Readability")]
        [SerializeField] float bubbleBaseScale = 0.0005f;   // canvas px → metres at 1 m away
        [Tooltip("Bubble height above the cue point (m). Short, so the bubble reads as sitting ON the object, not floating above it.")]
        [SerializeField] float stemHeight = 0.08f;
        [SerializeField] float hintMaxDistance = 2f;        // hint text only up close
        [SerializeField] float farDistance = 5f;            // beyond this the cue fades to 40%

        /// <summary>Accessibility: replace pulses/bounces with calm fades. Set from settings.</summary>
        public static bool ReduceMotion;

        // Design-system colours (plan Phase 6 table)
        static readonly Color Pink = Hex(0xFF7EB6), Lavender = Hex(0xC3B1FF), Tangerine = Hex(0xFFA77A),
                              SodaBlue = Hex(0x6CC4FF), Mint = Hex(0x4FE3A8), Grape = Hex(0x3A2E6B);

        public string Label { get; private set; }
        public string Hint { get; private set; }
        public string Tip { get; private set; }             // latest advice from Gemini ("Open the drawer")
        public int Likelihood { get; set; }
        public int Priority { get; private set; }           // 1 = first to check (its badge number)
        public SpotState State { get; private set; } = SpotState.ToCheck;
        public bool IsNext { get; private set; }

        // Bookkeeping for the automatic check (Phase 8)
        public int Attempts { get; set; }                   // checks without a clear answer
        public float CooldownUntil { get; set; }            // no re-check before this Time.time
        public string Note { get; set; }
        /// <summary>What Gemini said it saw ("black wallet on the desk next to the laptop"): the found place for sightings.</summary>
        public string Seen { get; private set; }
        /// <summary>Scans in a row that looked right at this cue's spot and didn't find its object there (it was moved away).</summary>
        public int Misses { get; set; }                    // e.g. "user said a previous match was not the item"
        public SpotState StateBeforeCheck { get; set; }     // restored if a check fails or is cancelled

        // Where the cue came from, so SpotPlacer can re-locate it when ARCore knows the room better.
        public CapturePose SourcePose { get; set; }         // the photo's camera
        public int[] SourceBox { get; set; }                // the spot's box in that photo
        public PlacementQuality Quality { get; set; }       // how trustworthy the current position is
        public bool IsSighting { get; set; }                // marks the item itself (seen in the open), not a hiding place
        public float CheckedAt { get; set; } = -1f;         // Time.time when it became Checked/Found (session summary)

        Camera cam;
        float stateTime;          // seconds in the current state (drives the looping motions)
        float oneShotTime = -1f;  // seconds into the current one-shot animation, < 0 = none
        OneShot oneShot;
        float fade = 1f;          // set by the controller (e.g. others fade to 30% when found)

        enum OneShot { Pop, Wiggle, Squash, Bounce }

        public void Init(HidingSpot spot, int priority)
        {
            Seen = spot.seen;
            if (stem != null) stem.SetPosition(1, new Vector3(0f, stemHeight, 0f));
            Label = spot.label;
            Hint = spot.hint;
            Likelihood = spot.likelihood;
            Priority = priority;
            name = $"SpotCue {priority}: {Label}";
            Play(OneShot.Pop);
            ApplyLook();
        }

        /// <summary>Changes state (and the Gemini tip, if given) and plays that state's one-shot animation.</summary>
        public void SetState(SpotState s, string tip = null)
        {
            if (tip != null) Tip = tip;
            if (s == State) { ApplyLook(); return; }
            State = s;
            stateTime = 0f;
            switch (s)
            {
                case SpotState.NeedsOpen: Play(OneShot.Wiggle); break;
                case SpotState.Checked: Play(OneShot.Squash); break;
                case SpotState.Candidate: Play(OneShot.Bounce); break;
                case SpotState.Found:
                    Play(OneShot.Bounce);
                    if (foundFx != null) foundFx.Play();
                    break;
            }
            if (s != SpotState.Focused && s != SpotState.Checking) SetDwell(0f);
            ApplyLook();
        }

        public void SetNext(bool next)
        {
            if (IsNext == next) return;
            IsNext = next;
            ApplyLook();
        }

        /// <summary>Dwell progress 0..1 while the user holds the phone on this cue.</summary>
        public void SetDwell(float dwell01)
        {
            if (progressRing == null) return;
            progressRing.fillAmount = dwell01;
            // Also visible on a "needs opening" cue the user is holding on (it keeps its own look).
            progressRing.enabled = dwell01 > 0f || State is SpotState.Focused or SpotState.Checking;
        }

        public void SetFade(float alpha) => fade = alpha;

        /// <summary>Is the cue still something the user has to look at?</summary>
        public bool IsOpen => State is SpotState.ToCheck or SpotState.Focused or SpotState.Checking or SpotState.NeedsOpen;

        // ---------- look per state ----------

        void ApplyLook()
        {
            if (panel == null) return;   // no prefab parts (shouldn't happen in the app)

            Color c = State switch
            {
                SpotState.Checking => Lavender,
                SpotState.NeedsOpen => Tangerine,
                SpotState.Checked => SodaBlue,
                SpotState.Candidate or SpotState.Found => Mint,
                _ => Pink,
            };
            panel.color = State == SpotState.Candidate ? Color.white : c;   // Candidate = mint outline look
            groundRing.color = new Color(c.r, c.g, c.b, IsNext ? 1f : 0.75f);
            if (stem != null) stem.startColor = stem.endColor = new Color(c.r, c.g, c.b, 0.9f);

            // Badge: number, or an icon. Text is always dark grape on the light bubble (never white on pastel).
            string badge = null;
            Sprite icon = null;
            switch (State)
            {
                case SpotState.ToCheck:
                case SpotState.Focused: badge = Priority.ToString(); break;
                case SpotState.Checking: badge = "..."; break;
                case SpotState.NeedsOpen: badge = "!"; break;       // "act on it" (plan: hand icon; "!" until we have art)
                case SpotState.Checked: icon = checkIcon; break;
                case SpotState.Candidate: badge = "?"; break;
                case SpotState.Found: icon = starIcon; break;
            }
            badgeText.text = badge ?? "";
            badgeText.color = Grape;
            badgeIcon.enabled = icon != null;
            badgeIcon.sprite = icon;
            badgeIcon.color = c == Pink ? Grape : Color.Lerp(c, Grape, 0.35f);

            labelText.color = hintText.color = Grape;
            labelText.text = Capitalize(Label);
            hintText.text = State switch
            {
                SpotState.ToCheck => IsNext ? "Check here next" : Hint,
                SpotState.Focused => "Hold still…",
                SpotState.Checking => "Looking…",
                SpotState.NeedsOpen => string.IsNullOrEmpty(Tip) ? "Open it up" : Tip,
                SpotState.Checked => "Checked",
                SpotState.Candidate => "Is this it?",
                SpotState.Found => "Found it!",
                _ => "",
            };
            if (nextTag != null) nextTag.SetActive(IsNext && (State == SpotState.ToCheck || State == SpotState.NeedsOpen));

            bool ringOn = State is SpotState.Focused or SpotState.Checking;
            progressRing.enabled = ringOn || progressRing.fillAmount > 0f;
            progressRing.color = State == SpotState.Checking ? Lavender : State == SpotState.NeedsOpen ? Tangerine : Pink;
            if (State == SpotState.Checking) progressRing.fillAmount = 1f;
        }

        // ---------- per-frame: facing, size, fading, motion ----------

        void LateUpdate()
        {
            if (cam == null) cam = Camera.main;
            if (cam == null || bubble == null) return;
            stateTime += Time.deltaTime;

            Vector3 camPos = cam.transform.position;
            float d = Vector3.Distance(camPos, transform.position);

            // Billboard on yaw only: the bubble turns toward you but stays upright.
            Vector3 toCam = camPos - bubble.position;
            toCam.y = 0f;
            if (toCam.sqrMagnitude > 1e-4f) bubble.rotation = Quaternion.LookRotation(-toCam, Vector3.up);

            // Motion multipliers: (sx, sy) scale, extra z rotation (wiggle), y offset (bob).
            Vector2 s = Vector2.one * StateScale();
            float rotZ = 0f;
            s = ApplyOneShot(s, ref rotZ);
            // Constant on-screen size: grow linearly with distance (clamped so it's never tiny or huge).
            float k = bubbleBaseScale * Mathf.Clamp(d, 0.5f, 4f);
            bubble.localScale = new Vector3(s.x * k, s.y * k, k);
            bubble.localRotation *= Quaternion.Euler(0f, 0f, rotZ);
            bubble.localPosition = new Vector3(0f, stemHeight + Bob(), 0f);

            if (State == SpotState.Checking) progressRing.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -stateTime * 120f);

            float alpha = fade * (d > farDistance ? 0.4f : 1f) * (State == SpotState.Checked ? 0.85f : 1f);
            if (ReduceMotion && oneShot == OneShot.Pop && oneShotTime >= 0f) alpha *= Mathf.Clamp01(oneShotTime / 0.35f);
            bubbleGroup.alpha = alpha;
            groundRing.canvasRenderer.SetAlpha(alpha);
            hintText.enabled = d <= hintMaxDistance || State != SpotState.ToCheck;   // state text always shows
        }

        float StateScale()
        {
            if (ReduceMotion) return State == SpotState.Focused ? 1.15f : 1f;
            float t = stateTime;
            return State switch
            {
                // Soft pulse 1.0 → 1.08 every 1.4 s; stronger when it's the next one.
                SpotState.ToCheck => 1f + (IsNext ? 0.12f : 0.08f) * 0.5f * (1f - Mathf.Cos(t * 2f * Mathf.PI / 1.4f)),
                SpotState.Focused => 1.15f,
                SpotState.Candidate => 1.05f + 0.05f * Mathf.Sin(t * 6f),
                _ => 1f,
            };
        }

        float Bob() => !ReduceMotion && IsNext && State == SpotState.ToCheck ? 0.015f * Mathf.Sin(stateTime * 3f) : 0f;

        void Play(OneShot a)
        {
            oneShot = a;
            oneShotTime = 0f;
        }

        Vector2 ApplyOneShot(Vector2 s, ref float rotZ)
        {
            if (oneShotTime < 0f) return s;
            oneShotTime += Time.deltaTime;
            float t = oneShotTime;
            switch (oneShot)
            {
                case OneShot.Pop:                                   // 0 → 1.1 → 1 over 0.35 s (ease-out-back)
                    if (t >= 0.35f) break;
                    return s * (ReduceMotion ? 1f : EaseOutBack(t / 0.35f));
                case OneShot.Wiggle:                                // ±6° twice over 0.4 s
                    if (t >= 0.4f || ReduceMotion) break;
                    rotZ = 6f * Mathf.Sin(t / 0.4f * 4f * Mathf.PI);
                    return s;
                case OneShot.Squash:                                // (1.15, 0.85) → (1, 1) over 0.25 s
                    if (t >= 0.25f || ReduceMotion) break;
                    float k = 1f - t / 0.25f;
                    return new Vector2(s.x * (1f + 0.15f * k), s.y * (1f - 0.15f * k));
                case OneShot.Bounce:                                // 3 bounces over 0.9 s
                    if (t >= 0.9f || ReduceMotion) break;
                    return s * (1f + 0.2f * Mathf.Abs(Mathf.Sin(t / 0.9f * 3f * Mathf.PI)) * (1f - t / 0.9f));
            }
            oneShotTime = -1f;
            return s;
        }

        static float EaseOutBack(float x)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
        }

        static string Capitalize(string s) => string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s.Substring(1);

        static Color Hex(uint rgb) => new Color32((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, 255);
    }
}
