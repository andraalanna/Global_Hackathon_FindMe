using System;
using System.Collections;
using TMPro;
using UnityEngine;

namespace FindMe.UI
{
    /// <summary>
    /// Screen 9: "Found it!" The mascot bounces three times, confetti falls, and "Nice, done" moves on to the summary.
    /// The camera view stays visible behind the card (no background).
    /// </summary>
    public class FoundScreen : MonoBehaviour
    {
        public TMP_Text sentence;
        public RectTransform mascot;
        public Confetti confetti;
        public ChunkyButton doneButton;

        public event Action onDone;

        Coroutine bounce;

        void Awake() => doneButton.Button.onClick.AddListener(() => onDone?.Invoke());

        /// <summary>Call after the screen is visible (the router activates it first).</summary>
        public void Play(string itemName, string where)
        {
            sentence.text = string.IsNullOrEmpty(where)
                ? $"You found your {itemName}!"
                : $"Your {itemName} was hiding in the {where}.";
            if (bounce != null) StopCoroutine(bounce);
            bounce = StartCoroutine(Bounce());
            confetti.Play();
        }

        // Mascot pops three times (each pop is a quick scale up and back).
        IEnumerator Bounce()
        {
            for (int i = 0; i < 3; i++)
            {
                Tweens.Punch(this, mascot, 0.15f, 0.3f);
                yield return new WaitForSecondsRealtime(0.35f);
            }
        }
    }
}
