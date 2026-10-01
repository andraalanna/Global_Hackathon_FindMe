using System;
using TMPro;
using UnityEngine;

namespace FindMe.UI
{
    /// <summary>
    /// Screen 1: "What are you looking for?". Tap a quick-pick chip or type a description, then Continue.
    /// The screen only reports what the user chose (onContinue); UIManager decides what happens next.
    /// </summary>
    public class InputScreen : MonoBehaviour
    {
        public TMP_InputField field;
        public ChunkyButton continueButton;
        public UIChip[] chips;     // Keys, Wallet, Glasses, ID card

        public event Action<string> onContinue;

        void Awake()
        {
            // Tapping a chip fills the text field and highlights only that chip.
            foreach (var chip in chips)
            {
                var c = chip;
                c.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() =>
                {
                    field.text = c.label.text;
                    Refresh();
                });
            }
            // Typing by hand clears the chip highlight (the text no longer matches a chip) and re-checks Continue.
            field.onValueChanged.AddListener(_ => Refresh());
            continueButton.Button.onClick.AddListener(() =>
            {
                string text = field.text.Trim();
                if (text.Length > 0) onContinue?.Invoke(text);
            });
            Refresh();
        }

        // Called each time this screen is shown again (e.g. after Back) so it starts clean.
        public void ResetScreen() { field.text = ""; Refresh(); }

        void Refresh()
        {
            string text = field.text.Trim();
            foreach (var chip in chips) chip.SetSelected(string.Equals(chip.label.text, text, StringComparison.OrdinalIgnoreCase));
            continueButton.SetInteractable(text.Length > 0);   // can't continue with an empty box
        }
    }
}
