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
            sentence.text = FoundSentence(itemName, where);
            if (bounce != null) StopCoroutine(bounce);
            bounce = StartCoroutine(Bounce());
            confetti.Play();
        }

        /// <summary>
        /// "Your wallet was hiding in the bag." For places that already say where ("on the desk next to the laptop",
        /// "near the bag", from an item seen in the open) the sentence uses them as they are.
        /// </summary>
        public static string FoundSentence(string item, string where, string verb = "was hiding in the")
        {
            if (string.IsNullOrEmpty(where)) return $"You found your {item}!";
            string w = where.Trim();
            string lower = w.ToLowerInvariant();
            foreach (var p in new[] { "on ", "in ", "inside ", "under ", "beneath ", "behind ", "next to ", "beside ", "between ", "near ", "by ", "at " })
                if (lower.StartsWith(p)) return $"Your {item} was {w}.";
            return $"Your {item} {verb} {w}.";
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
