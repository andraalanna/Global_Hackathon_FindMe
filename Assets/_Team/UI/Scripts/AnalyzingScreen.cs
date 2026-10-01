using System.Collections;
using TMPro;
using UnityEngine;

namespace FindMe.UI
{
    /// <summary>
    /// Screen 2: "Getting to know your wallet". The checklist is a made-up progress animation (we don't know how
    /// long Gemini takes), so it ticks on a timer and keeps the last row spinning until the item card replaces it.
    /// </summary>
    public class AnalyzingScreen : MonoBehaviour
    {
        public TMP_Text title;
        public UIStepRow[] rows;           // 3 rows
        public RectTransform mascot;       // gets a gentle bounce

        const float StepSeconds = 1.1f;    // time between ticks
        Coroutine run;

        /// <summary>Show with the status text from SearchController, e.g. "Getting to know your black wallet…".</summary>
        public void Begin(string message)
        {
            // The design has no ellipsis in the title.
            title.text = message.TrimEnd('…', '.', ' ');
            if (run != null) StopCoroutine(run);
            run = StartCoroutine(Animate());
        }

        /// <summary>Called when the real result arrives: tick everything off.</summary>
        public void Finish()
        {
            if (run != null) StopCoroutine(run);
            run = null;
            foreach (var r in rows) r.SetState(StepState.Done);
        }

        IEnumerator Animate()
        {
            foreach (var r in rows) r.SetState(StepState.Pending);
            if (mascot != null) Tweens.Punch(this, mascot, 0.06f, 0.5f);

            // Row i is "working" for one step, then done; the last row keeps spinning (we never fake completion).
            for (int i = 0; i < rows.Length; i++)
            {
                rows[i].SetState(StepState.Active);
                if (i == rows.Length - 1) yield break;
                yield return new WaitForSecondsRealtime(StepSeconds);
                rows[i].SetState(StepState.Done);
            }
        }
    }
}
