using UnityEngine;
using UnityEngine.EventSystems;

namespace FindMe.UI
{
    /// <summary>Tiny press feel for flat buttons (back, mic, round icons): shrinks to 94 % while the finger is down.</summary>
    public class PressFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        const float Pressed = 0.94f;

        public void OnPointerDown(PointerEventData e) => Tweens.Scale(this, transform, Pressed, 0.08f);
        public void OnPointerUp(PointerEventData e) => Tweens.Scale(this, transform, 1f, 0.12f, Tweens.EaseOutBack);
        public void OnPointerExit(PointerEventData e) => Tweens.Scale(this, transform, 1f, 0.12f);

        void OnDisable() => transform.localScale = Vector3.one;   // never stay shrunk if hidden mid-press
    }
}
