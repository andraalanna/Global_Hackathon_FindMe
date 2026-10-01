using UnityEngine;
using UnityEngine.UI;

namespace FindMe.UI
{
    /// <summary>Cloud-colored pill track with a colored fill. The fill grows by changing its right anchor (0..1).</summary>
    public class UIProgressBar : MonoBehaviour
    {
        public RectTransform fill;   // child of the track, left-anchored
        public Image fillImage;

        float value;
        Coroutine anim;

        public float Value => value;

        /// <summary>Set 0..1. animate=true eases to it over 0.4 s (the "count-up" feel).</summary>
        public void Set(float v01, bool animate = true)
        {
            v01 = Mathf.Clamp01(v01);
            if (anim != null) StopCoroutine(anim);
            if (!animate || !isActiveAndEnabled) { Apply(v01); return; }
            float from = value;
            anim = Tweens.Run(this, 0.4f, k => Apply(Mathf.Lerp(from, v01, k)));
        }

        void Apply(float v)
        {
            value = v;
            // A fill narrower than its rounded ends looks glitchy, so hide it at (almost) zero.
            fillImage.enabled = v > 0.01f;
            fill.anchorMax = new Vector2(v, 1f);
        }
    }
}
