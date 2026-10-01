using UnityEngine;
using UnityEngine.UI;

namespace FindMe.UI
{
    /// <summary>
    /// A ring that grows from 78 % to 122 % while fading out, then starts again (prototype .findbtn .ring,
    /// 2.6 s). Two rings half a cycle apart make the "pulsing radar" around the FIND button.
    /// </summary>
    public class Ripple : MonoBehaviour
    {
        public float period = 2.6f, delay;
        Graphic g;

        void Awake() => g = GetComponent<Graphic>();

        void Update()
        {
            if (Tweens.ReduceMotion) { g.color = new Color(g.color.r, g.color.g, g.color.b, 0f); return; }
            float t = Mathf.Repeat((Time.unscaledTime - delay) / period, 1f);
            float k = 1f - Mathf.Pow(1f - t, 2f);                     // ease-out
            transform.localScale = Vector3.one * Mathf.Lerp(0.78f, 1.22f, k);
            var c = g.color;
            g.color = new Color(c.r, c.g, c.b, Mathf.Lerp(0.7f, 0f, k));
        }
    }
}
