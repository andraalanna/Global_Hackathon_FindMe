using System.Collections.Generic;
using UnityEngine;

namespace FindMe.UI
{
    /// <summary>
    /// Shows one screen at a time, with a back stack (like the prototype's go()/back()).
    /// Entering screen: fades in while rising 8 px (prototype "in" animation, 0.26 s).
    /// </summary>
    public class ScreenRouter : MonoBehaviour
    {
        readonly Dictionary<string, UIScreen> screens = new();
        readonly Stack<string> history = new();
        /// <summary>Tab roots and the splash clear the back stack (prototype ROOTS).</summary>
        public readonly HashSet<string> Roots = new() { "splash" };

        public UIScreen Current { get; private set; }

        public void Add(UIScreen s) => screens[s.Id] = s;

        public bool Has(string id) => screens.ContainsKey(id);

        /// <summary>Open a screen. Remembers the current one for Back(), unless the target is a root.</summary>
        public void Go(string id)
        {
            if (!screens.ContainsKey(id))
            {
                Debug.LogWarning($"[UI] No screen '{id}' yet (later phase).");
                return;
            }
            if (Roots.Contains(id)) history.Clear();
            else if (Current != null && Current.Id != id) history.Push(Current.Id);
            Show(screens[id]);
        }

        /// <summary>Show a screen fully, with no animation (Editor capture tool).</summary>
        public UIScreen ShowNow(string id)
        {
            var s = screens[id];
            if (Current != null && Current != s) Current.gameObject.SetActive(false);
            Current = s;
            s.Root.anchoredPosition = Vector2.zero;
            s.gameObject.SetActive(true);
            s.OnShow();
            s.Group.alpha = 1f;
            return s;
        }

        public void Back() => Show(screens[history.Count > 0 ? history.Pop() : "splash"]);

        void Show(UIScreen next)
        {
            StopAllCoroutines();
            if (Current != null && Current != next) Current.gameObject.SetActive(false);
            Current = next;
            next.Root.anchoredPosition = Vector2.zero;
            next.gameObject.SetActive(true);
            next.OnShow();

            next.Group.alpha = 0f;
            StartCoroutine(Tweens.Fade(next.Group, 1f, Tweens.ReduceMotion ? 0.15f : 0.26f));
            StartCoroutine(Tweens.SlideIn(next.Root, new Vector2(0f, -8f), 0.26f));   // -8: starts slightly lower
        }
    }
}
