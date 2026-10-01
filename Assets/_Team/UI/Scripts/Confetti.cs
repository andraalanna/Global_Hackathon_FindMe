using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace FindMe.UI
{
    /// <summary>
    /// 40 small coloured rectangles that tumble down the screen for ~1.2 s. The pieces are ready-made Images
    /// (built once, reused each Play), so playing it allocates nothing. Skipped when Reduce Motion is on.
    /// </summary>
    public class Confetti : MonoBehaviour
    {
        public RectTransform area;     // the full-screen rect the pieces fall through
        public Image[] pieces;
        public Color[] colors;         // pink, mint, lemon, lavender, soda (theme colours)

        public const float Duration = 1.2f;

        Coroutine running;

        public void Play()
        {
            if (Tweens.ReduceMotion) return;
            if (running != null) StopCoroutine(running);
            running = StartCoroutine(Fall());
        }

        IEnumerator Fall()
        {
            float w = area.rect.width, h = area.rect.height;
            int n = pieces.Length;
            // Each piece gets its own start column, delay, fall speed and spin, picked once up front.
            var x = new float[n]; var delay = new float[n]; var spin = new float[n]; var sway = new float[n];
            for (int i = 0; i < n; i++)
            {
                x[i] = Random.Range(-0.5f, 0.5f) * w;
                delay[i] = Random.Range(0f, 0.35f);
                spin[i] = Random.Range(-540f, 540f);
                sway[i] = Random.Range(-60f, 60f);
                pieces[i].color = colors[Random.Range(0, colors.Length)];
                pieces[i].enabled = false;
            }

            float t = 0f, total = Duration + 0.35f;
            while (t < total)
            {
                t += Time.unscaledDeltaTime;
                for (int i = 0; i < n; i++)
                {
                    float k = (t - delay[i]) / Duration;   // 0..1 progress of this piece
                    bool live = k >= 0f && k <= 1f;
                    pieces[i].enabled = live;
                    if (!live) continue;
                    // Fall from above the top to a bit past the bottom, ease-in so it speeds up like gravity.
                    float y = Mathf.Lerp(h * 0.5f + 100f, -h * 0.5f, k * k * 0.6f + k * 0.4f);
                    var rt = pieces[i].rectTransform;
                    rt.anchoredPosition = new Vector2(x[i] + sway[i] * Mathf.Sin(k * 6f), y);
                    rt.localRotation = Quaternion.Euler(0f, 0f, spin[i] * k);
                    var c = pieces[i].color; c.a = k < 0.8f ? 1f : (1f - k) / 0.2f;   // fade out over the last 20 %
                    pieces[i].color = c;
                }
                yield return null;
            }
            foreach (var p in pieces) p.enabled = false;
            running = null;
        }
    }
}
