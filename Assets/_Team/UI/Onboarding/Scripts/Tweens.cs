using System;
using System.Collections;
using UnityEngine;

namespace FindMe.Onboarding
{
    /// <summary>
    /// Tiny coroutine tweens (no packages). Usage: StartCoroutine(Tweens.Fade(group, 1f, 0.25f)).
    /// With ReduceMotion on, movement is skipped and only short fades remain (DESIGN_SPEC §7).
    /// </summary>
    public static class Tweens
    {
        public static bool ReduceMotion;

        // Ease curves: t goes 0→1, the result is how far along the value is.
        public static float OutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);
        public static float OutBack(float t)
        {
            const float c = 1.70158f;   // standard "overshoot a little, then settle"
            return 1f + (c + 1f) * Mathf.Pow(t - 1f, 3f) + c * Mathf.Pow(t - 1f, 2f);
        }

        /// <summary>Calls step(eased 0..1) every frame for `duration` seconds (unscaled time, so pausing the game doesn't freeze the UI).</summary>
        public static IEnumerator Run(float duration, Func<float, float> ease, Action<float> step)
        {
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                step(ease(Mathf.Clamp01(t / duration)));
                yield return null;
            }
            step(1f);
        }

        public static IEnumerator Fade(CanvasGroup g, float to, float duration)
        {
            float from = g.alpha;
            return Run(duration, OutCubic, k => g.alpha = Mathf.LerpUnclamped(from, to, k));
        }

        /// <summary>Slides from `offset` back to the rest position (used for "screen comes in from 8 px below").</summary>
        public static IEnumerator SlideIn(RectTransform rt, Vector2 offset, float duration, Func<float, float> ease = null)
        {
            Vector2 rest = rt.anchoredPosition;
            if (ReduceMotion) yield break;
            yield return Run(duration, ease ?? OutCubic, k => rt.anchoredPosition = rest + offset * (1f - k));
        }

        /// <summary>Scale pop from `from` to 1 (Found burst, sheets).</summary>
        public static IEnumerator Pop(Transform t, float from, float duration)
        {
            if (ReduceMotion) { t.localScale = Vector3.one; yield break; }
            yield return Run(duration, OutBack, k => t.localScale = Vector3.one * Mathf.LerpUnclamped(from, 1f, k));
        }
    }
}
