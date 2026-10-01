using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FindMe.UI
{
    /// <summary>
    /// Banner that slides down from the top, stays a few seconds, then slides away.
    /// Optional action button (e.g. "Undo"). Tangerine = gentle nudge, soda = checked.
    /// </summary>
    public class UIToast : MonoBehaviour
    {
        public RectTransform box;          // the colored rounded rectangle that moves
        public Image boxImage;
        public TMP_Text message;
        public TMP_Text hint;              // optional second line
        public ChunkyButton actionButton;  // small button on the right; hidden when no action
        public UITheme theme;

        public const float SlideDur = 0.2f;

        Coroutine running;
        Action onAction;
        float hiddenY, shownY;

        void Awake()
        {
            shownY = box.anchoredPosition.y;
            hiddenY = shownY + box.rect.height + UITheme.Px(80);   // parked above the screen (stays active, just off-screen)
            box.anchoredPosition = new Vector2(box.anchoredPosition.x, hiddenY);
            actionButton.Button.onClick.AddListener(() => { var a = onAction; Hide(); a?.Invoke(); });
        }

        /// <summary>Show text for `seconds`. If actionLabel is set, a button appears and calls onAction (then the toast closes).</summary>
        public void Show(string text, Color color, float seconds = 4f, string actionLabel = null, Action onAction = null, string hintText = null)
        {
            this.onAction = onAction;
            message.text = text;
            hint.text = hintText ?? "";
            hint.gameObject.SetActive(!string.IsNullOrEmpty(hintText));   // no empty line when there is no hint
            boxImage.color = color;
            actionButton.gameObject.SetActive(!string.IsNullOrEmpty(actionLabel));
            if (!string.IsNullOrEmpty(actionLabel)) actionButton.Text = actionLabel;

            // The text just changed: rebuild the row's layout and the text mesh now, so the label is sized
            // correctly on the very first frame instead of staying invisible at width 0.
            message.ForceMeshUpdate();
            if (hint.gameObject.activeSelf) hint.ForceMeshUpdate();
            LayoutRebuilder.ForceRebuildLayoutImmediate(box);

            if (running != null) StopCoroutine(running);
            running = StartCoroutine(Routine(seconds));
        }

        public void Hide()
        {
            if (running != null) StopCoroutine(running);
            running = StartCoroutine(HideRoutine());
        }

        IEnumerator Routine(float seconds)
        {
            Tweens.Move(this, box, new Vector2(box.anchoredPosition.x, shownY), SlideDur);
            yield return new WaitForSecondsRealtime(seconds);
            yield return HideRoutine();
        }

        IEnumerator HideRoutine()
        {
            bool done = false;
            Tweens.Move(this, box, new Vector2(box.anchoredPosition.x, hiddenY), SlideDur, null, () => done = true);
            if (Tweens.ReduceMotion) done = true;
            while (!done) yield return null;
            running = null;
        }
    }
}
