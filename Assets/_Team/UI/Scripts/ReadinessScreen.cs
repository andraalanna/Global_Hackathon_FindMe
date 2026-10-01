using System;
using UnityEngine;

namespace FindMe.UI
{
    /// <summary>
    /// Screen 4: a card over the live camera asking the user to move the phone slowly while AR maps the room.
    /// The bar follows SetReadiness(0..1). When the app is ready, UIManager switches to the scan HUD, so this
    /// screen never needs a "continue" button. Cancel goes back to the start.
    /// </summary>
    public class ReadinessScreen : MonoBehaviour
    {
        public UIProgressBar bar;
        public ChunkyButton cancelButton;

        public event Action onCancel;

        void Awake() => cancelButton.Button.onClick.AddListener(() => onCancel?.Invoke());

        /// <summary>Called every frame by SearchController, so no animation: just follow the value.</summary>
        public void SetProgress(float p01) => bar.Set(p01, false);
    }
}
