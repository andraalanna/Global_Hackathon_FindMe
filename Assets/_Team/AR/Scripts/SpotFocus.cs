using System;
using UnityEngine;

namespace FindMe
{
    /// <summary>
    /// Every frame (plan Phase 7): which cue is the user looking at, is the phone steady, and how long
    /// have they held it there. After <see cref="dwellSeconds"/> of steady focus it fires
    /// <see cref="DwellCompleted"/>, which starts the automatic Gemini check. No per-frame allocations.
    /// </summary>
    public class SpotFocus : MonoBehaviour
    {
        [SerializeField] SpotPlacer placer;
        [SerializeField] Camera arCamera;

        [Header("Focus")]
        [SerializeField] float maxDistance = 2.2f;     // m: further away you're not "checking" it
        [SerializeField] float maxAngle = 14f;         // degrees from the screen centre
        [SerializeField] float switchMargin = 3f;      // hysteresis: a new cue must be this much closer to centre
        [SerializeField] float loseFocusGrace = 0.3f;  // s: brief glances away don't reset the dwell

        [Header("Steadiness (0.5 s rolling average)")]
        [SerializeField] float maxAngularSpeed = 25f;  // deg/s
        [SerializeField] float maxLinearSpeed = 0.25f; // m/s
        [SerializeField] float window = 0.5f;

        [Header("Dwell")]
        [SerializeField] float dwellSeconds = 2f;

        public SpotCue Focused { get; private set; }
        public float Dwell01 => Mathf.Clamp01(dwell / dwellSeconds);
        public bool IsSteady { get; private set; }
        public float AngularSpeed { get; private set; }
        public float LinearSpeed { get; private set; }

        /// <summary>Set by SearchController: false while a check is running, so dwell pauses.</summary>
        public bool CanTrigger { get; set; } = true;

        public event Action<SpotCue> DwellCompleted;
        /// <summary>Fires at dwell 0% (started) and 50%: hook for the soft tick sound (Section 8).</summary>
        public event Action<SpotCue, float> Tick;

        float dwell, lostTimer;

        // Ring buffer of recent motion samples (fixed size = no garbage).
        struct Sample { public float dt, deg, metres; }
        readonly Sample[] samples = new Sample[64];
        int head, count;
        Vector3 lastPos;
        Quaternion lastRot;
        bool hasLast;

        void Awake()
        {
            if (arCamera == null) arCamera = Camera.main;
        }

        void OnDisable() => ClearFocus();

        void Update()
        {
            UpdateSteadiness();
            SpotCue best = FindBest(out float bestAngle);

            // Hysteresis: keep the current cue unless another is clearly closer to the centre.
            float currentAngle = 0f;
            bool currentValid = Focused != null && IsFocusable(Focused, out currentAngle);
            SpotCue target = currentValid ? Focused : null;
            if (best != null && ShouldSwitch(currentValid, currentAngle, bestAngle, switchMargin))
                target = best;

            if (target == null)
            {
                // Brief glances away are forgiven; after the grace period the dwell resets.
                if (Focused != null && (lostTimer += Time.deltaTime) > loseFocusGrace) ClearFocus();
                return;
            }
            lostTimer = 0f;
            if (target != Focused) SetFocus(target);

            // Dwell only grows while steady, off cooldown, and nothing else is being checked. Unsteady = pause.
            bool coolingDown = Time.time < Focused.CooldownUntil;
            if (!IsSteady || !CanTrigger || coolingDown) return;

            float before = dwell;
            dwell += Time.deltaTime;
            if (before == 0f) Tick?.Invoke(Focused, 0f);
            if (before < dwellSeconds * 0.5f && dwell >= dwellSeconds * 0.5f) Tick?.Invoke(Focused, 0.5f);
            Focused.SetDwell(Dwell01);

            if (dwell >= dwellSeconds)
            {
                var cue = Focused;
                Release(false);            // the checker now owns the cue's state
                DwellCompleted?.Invoke(cue);
            }
        }

        /// <summary>Hysteresis rule, pulled out so it can be unit-tested.</summary>
        public static bool ShouldSwitch(bool haveCurrent, float currentAngle, float candidateAngle, float margin) =>
            !haveCurrent || candidateAngle <= currentAngle - margin;

        SpotCue FindBest(out float bestAngle)
        {
            SpotCue best = null;
            bestAngle = float.MaxValue;
            foreach (var c in placer.Cues)
            {
                if (c == null || !IsFocusable(c, out float a) || a >= bestAngle) continue;
                best = c;
                bestAngle = a;
            }
            return best;
        }

        bool IsFocusable(SpotCue c, out float angle)
        {
            angle = float.MaxValue;
            if (!(c.State is SpotState.ToCheck or SpotState.NeedsOpen or SpotState.Focused)) return false;
            Transform cam = arCamera.transform;
            Vector3 to = c.transform.position - cam.position;
            if (to.magnitude > maxDistance) return false;
            if (arCamera.WorldToViewportPoint(c.transform.position).z <= 0f) return false;   // behind you
            angle = Vector3.Angle(cam.forward, to);
            return angle <= maxAngle;
        }

        void SetFocus(SpotCue cue)
        {
            Release(true);
            Focused = cue;
            dwell = 0f;
            // A "needs opening" cue keeps its look and Gemini's tip ("Open the drawer"): only its ring fills.
            if (cue.State == SpotState.ToCheck) cue.SetState(SpotState.Focused);
        }

        void ClearFocus() => Release(true);

        /// <summary>Drops focus. restoreState: a Focused cue goes back to ToCheck.</summary>
        void Release(bool restoreState)
        {
            if (Focused != null && restoreState && Focused.State == SpotState.Focused)
                Focused.SetState(SpotState.ToCheck);
            if (Focused != null) Focused.SetDwell(0f);
            Focused = null;
            dwell = 0f;
            lostTimer = 0f;
        }

        /// <summary>Rolling 0.5 s average of how fast the phone turns and moves.</summary>
        void UpdateSteadiness()
        {
            Transform cam = arCamera.transform;
            if (hasLast)
            {
                samples[head] = new Sample
                {
                    dt = Time.deltaTime,
                    deg = Quaternion.Angle(lastRot, cam.rotation),
                    metres = Vector3.Distance(lastPos, cam.position),
                };
                head = (head + 1) % samples.Length;
                count = Mathf.Min(count + 1, samples.Length);
            }
            lastPos = cam.position;
            lastRot = cam.rotation;
            hasLast = true;

            float t = 0f, deg = 0f, m = 0f;
            for (int i = 0; i < count && t < window; i++)
            {
                var s = samples[(head - 1 - i + samples.Length) % samples.Length];
                t += s.dt;
                deg += s.deg;
                m += s.metres;
            }
            AngularSpeed = t > 0f ? deg / t : 0f;
            LinearSpeed = t > 0f ? m / t : 0f;
            IsSteady = t > 0f && AngularSpeed < maxAngularSpeed && LinearSpeed < maxLinearSpeed;
        }
    }
}
