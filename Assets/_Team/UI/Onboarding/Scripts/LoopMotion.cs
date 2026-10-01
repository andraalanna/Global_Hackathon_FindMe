using UnityEngine;

namespace FindMe.Onboarding
{
    /// <summary>
    /// Endless gentle back-and-forth: move, rotate and/or stretch around the rest pose.
    /// One component covers the logo brackets "focus" pulse, the crown rock, the blob wobble and the mascot bob.
    /// A smooth sine wave: 0 at rest, 1 at the far point, back to 0 every `period` seconds.
    /// </summary>
    public class LoopMotion : MonoBehaviour
    {
        public float period = 2f;
        [Tooltip("0..1: start part-way through the loop, so neighbours don't move in lockstep.")]
        public float phase;
        public Vector2 move;               // px at the far point
        public float rotate;               // degrees added at the far point
        public Vector2 stretch;            // e.g. (0.04, -0.03): 4 % wider, 3 % shorter at the far point

        RectTransform rt;
        Vector2 restPos;
        float restAngle;

        void Awake()
        {
            rt = (RectTransform)transform;
            restPos = rt.anchoredPosition;
            restAngle = rt.localEulerAngles.z;
        }

        void OnDisable()
        {
            if (rt == null) return;                    // put everything back when the screen hides
            rt.anchoredPosition = restPos;
            rt.localEulerAngles = new Vector3(0, 0, restAngle);
            rt.localScale = Vector3.one;
        }

        void Update()
        {
            if (Tweens.ReduceMotion) return;
            // (1 - cos) / 2 goes 0 → 1 → 0 smoothly: an ease-in-out loop like CSS "ease-in-out infinite alternate".
            float k = (1f - Mathf.Cos((Time.unscaledTime / period + phase) * Mathf.PI * 2f)) * 0.5f;
            rt.anchoredPosition = restPos + move * k;
            rt.localEulerAngles = new Vector3(0, 0, restAngle + rotate * k);
            rt.localScale = new Vector3(1f + stretch.x * k, 1f + stretch.y * k, 1f);
        }

        /// <summary>Call after changing the rest position from code (e.g. layout moved it).</summary>
        public void Rebase()
        {
            restPos = rt.anchoredPosition;
            restAngle = rt.localEulerAngles.z;
        }
    }
}
