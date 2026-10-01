using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FindMe.UI
{
    public enum StepState { Pending, Active, Done }

    /// <summary>
    /// One line of the "Getting to know your wallet" list. The badge is a rounded square:
    /// empty cloud outline (waiting), pink outline that gently pulses (working), or a mint square with a check (done).
    /// </summary>
    public class UIStepRow : MonoBehaviour
    {
        public RectTransform badge;   // the whole badge; scaled for the pulse
        public Image ring;            // outline square (cloud, or pink while working)
        public Image hole;            // background-coloured square inside it, so only an outline shows
        public Image doneFill;        // mint square
        public Image check;           // grape check mark
        public TMP_Text label;
        public Color pendingColor, activeColor;

        public StepState State { get; private set; }

        public void SetState(StepState s)
        {
            State = s;
            bool working = s != StepState.Done;
            ring.enabled = working;
            hole.enabled = working;
            ring.color = s == StepState.Active ? activeColor : pendingColor;
            doneFill.enabled = s == StepState.Done;
            check.enabled = s == StepState.Done;
            if (s != StepState.Active) badge.localScale = Vector3.one;
        }

        void Update()
        {
            // "Working" = a soft 10 % pulse; unscaled time so it still plays if timeScale is 0.
            if (State == StepState.Active && !Tweens.ReduceMotion)
                badge.localScale = Vector3.one * (1f + 0.1f * Mathf.Sin(Time.unscaledTime * 6f));
        }
    }
}
