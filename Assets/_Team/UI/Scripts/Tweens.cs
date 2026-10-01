using System;
using System.Collections;
using UnityEngine;

namespace FindMe.UI
{
    /// <summary>
    /// Tiny coroutine tween helper (no packages). All motion goes through here so the
    /// ReduceMotion flag can turn movement into a quick fade in one place.
    /// </summary>
    public static class Tweens
    {
        /// <summary>When true: no sliding/bouncing, only 0.15 s fades (DESIGN_SPEC section 7).</summary>
        public static bool ReduceMotion;
        public const float ReducedDuration = 0.15f;

        // Easing curves: input t in 0..1, output 0..1 (OutBack overshoots a little: the "bouncy" feel).
        public static float EaseOut(float t) { t = 1f - t; return 1f - t * t * t; }
        public static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            float u = t - 1f;
            return 1f + c3 * u * u * u + c1 * u * u;
        }

        /// <summary>Runs step(eased 0..1) every frame for dur seconds on host. Unscaled time, so UI still works at timeScale 0.</summary>
        public static Coroutine Run(MonoBehaviour host, float dur, Action<float> step, Func<float, float> ease = null, Action done = null)
        {
            return host.StartCoroutine(Routine(dur, step, ease ?? EaseOut, done));
        }

        static IEnumerator Routine(float dur, Action<float> step, Func<float, float> ease, Action done)
        {
            float t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                step(ease(Mathf.Clamp01(t / dur)));
                yield return null;
            }
            step(ease(1f));
            done?.Invoke();
        }

        public static Coroutine Fade(MonoBehaviour host, CanvasGroup g, float to, float dur = 0.25f, Action done = null)
        {
            float from = g.alpha;
            if (ReduceMotion) dur = ReducedDuration;
            return Run(host, dur, k => g.alpha = Mathf.LerpUnclamped(from, to, k), EaseOut, done);
        }

        /// <summary>Moves anchoredPosition. With ReduceMotion it just snaps (callers fade alongside).</summary>
        public static Coroutine Move(MonoBehaviour host, RectTransform r, Vector2 to, float dur = 0.25f, Func<float, float> ease = null, Action done = null)
        {
            Vector2 from = r.anchoredPosition;
            if (ReduceMotion) { r.anchoredPosition = to; done?.Invoke(); return null; }
            return Run(host, dur, k => r.anchoredPosition = Vector2.LerpUnclamped(from, to, k), ease, done);
        }

        public static Coroutine Scale(MonoBehaviour host, Transform t, float to, float dur = 0.2f, Func<float, float> ease = null)
        {
            float from = t.localScale.x;
            if (ReduceMotion) { t.localScale = Vector3.one * to; return null; }
            return Run(host, dur, k => t.localScale = Vector3.one * Mathf.LerpUnclamped(from, to, k), ease);
        }

        /// <summary>Quick "pop": scales up then settles back to 1. For mascot bounce and button feedback.</summary>
        public static Coroutine Punch(MonoBehaviour host, Transform t, float amount = 0.15f, float dur = 0.3f)
        {
            if (ReduceMotion) return null;
            return Run(host, dur, k => t.localScale = Vector3.one * (1f + amount * Mathf.Sin(k * Mathf.PI)), x => x);
        }
    }
}
