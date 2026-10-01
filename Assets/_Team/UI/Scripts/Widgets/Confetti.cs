using UnityEngine;
using UnityEngine.UI;

namespace FindMe.UI
{
    /// <summary>
    /// Found-it confetti (prototype .burst i): 14 small rounded rectangles in the theme colors fly out from
    /// the centre in a ring, spin, and fade over 1.1 s. Call Play() to fire again.
    /// </summary>
    public class Confetti : MonoBehaviour
    {
        const int Count = 14;
        const float Duration = 1.1f;
        readonly RectTransform[] bits = new RectTransform[Count];
        readonly Vector2[] target = new Vector2[Count];
        readonly float[] spin = new float[Count];
        float started = -10f;

        public static Confetti Create(Transform parent)
        {
            var T = UIKit.T;
            var rt = UIKit.Rect(parent, "Confetti");
            UIKit.Stretch(rt);
            var c = rt.gameObject.AddComponent<Confetti>();
            Color[] colors = { T.bubblegum, T.soda, T.mint, T.lemon, T.lavender };
            for (int i = 0; i < Count; i++)
            {
                var img = UIKit.Box(rt, "Bit", colors[i % colors.Length], 4f);
                c.bits[i] = (RectTransform)img.transform;
                UIKit.Place(c.bits[i], new Vector2(0.5f, 0.5f), new Vector2(12f, 18f));
                c.bits[i].gameObject.SetActive(false);
            }
            return c;
        }

        public void Play()
        {
            if (Tweens.ReduceMotion) return;
            for (int i = 0; i < Count; i++)
            {
                float a = i / (float)Count * Mathf.PI * 2f, d = 95f + Random.value * 30f;   // ring of 95–125 px
                target[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * d;
                spin[i] = Random.value * 360f;
                bits[i].gameObject.SetActive(true);
            }
            started = Time.unscaledTime;
        }

        void Update()
        {
            float t = (Time.unscaledTime - started) / Duration;
            if (t > 1.2f) return;
            float k = 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f);                          // ease-out
            for (int i = 0; i < Count; i++)
            {
                bits[i].anchoredPosition = target[i] * k;
                bits[i].localEulerAngles = new Vector3(0, 0, spin[i] * k);
                bits[i].localScale = Vector3.one * Mathf.Lerp(0.4f, 1f, k);
                var img = bits[i].GetComponent<Image>();
                var c = img.color;
                img.color = new Color(c.r, c.g, c.b, 1f - Mathf.Clamp01(t));
                if (t >= 1f) bits[i].gameObject.SetActive(false);
            }
        }
    }
}
