using UnityEngine;

namespace FindMe.UI
{
    /// <summary>
    /// White sheet that slides up from the bottom (0.3 s, ease-out-back = tiny bounce) and back down.
    /// No dim background: the camera view must stay visible behind it.
    /// </summary>
    public class UIBottomSheet : MonoBehaviour
    {
        public RectTransform sheet;     // anchored to the bottom edge, pivot at the bottom

        public bool IsOpen { get; private set; }
        const float Dur = 0.3f;

        Coroutine anim;

        // At least 420 design-px tall for the maths: before the first layout pass the sheet's height can still read 0.
        float HiddenY => -(Mathf.Max(sheet.rect.height, UITheme.Px(420)) + UITheme.Px(40));

        /// <summary>Slide up. Safe to call every frame (the spot panel does): it only animates the first time.</summary>
        public void Show()
        {
            if (IsOpen) return;
            IsOpen = true;
            if (anim != null) StopCoroutine(anim);
            sheet.anchoredPosition = new Vector2(0f, HiddenY);
            anim = Tweens.Move(this, sheet, Vector2.zero, Dur, Tweens.EaseOutBack);
        }

        public void Hide()
        {
            if (!IsOpen) return;
            IsOpen = false;
            if (anim != null) StopCoroutine(anim);
            anim = Tweens.Move(this, sheet, new Vector2(0f, HiddenY), 0.2f, Tweens.EaseOut);
        }

        // Parked below the screen at start (stays active, just off-screen, so Awake order never matters).
        void Awake() { sheet.anchoredPosition = new Vector2(0f, HiddenY); }
    }
}
