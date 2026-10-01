using UnityEngine;

namespace FindMe.UI
{
    /// <summary>Turns this element forever (spinner arcs). Negative = clockwise.</summary>
    public class Spin : MonoBehaviour
    {
        public float degreesPerSecond = 180f;

        void Update()
        {
            if (Tweens.ReduceMotion) return;
            transform.Rotate(0f, 0f, degreesPerSecond * Time.unscaledDeltaTime);
        }
    }
}
