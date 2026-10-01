using System;
using TMPro;
using UnityEngine;

namespace FindMe.UI
{
    /// <summary>
    /// Screen 8: "Is this your wallet?" sheet shown when Gemini thinks it saw the item.
    /// Yes -> onAnswer(true), Not it -> onAnswer(false); the sheet closes itself either way.
    /// </summary>
    public class MatchSheet : MonoBehaviour
    {
        public UIBottomSheet sheet;
        public TMP_Text title, body;
        public ChunkyButton notItButton, yesButton;

        public event Action<bool> onAnswer;

        void Awake()
        {
            notItButton.Button.onClick.AddListener(() => Answer(false));
            yesButton.Button.onClick.AddListener(() => Answer(true));
        }

        public void Show(string itemName, string where)
        {
            title.text = $"Is this your {itemName}?";
            body.text = string.IsNullOrEmpty(where) ? "Take a closer look." : $"Spotted in the {where}.";
            sheet.Show();
        }

        public void Hide() => sheet.Hide();

        void Answer(bool yes)
        {
            sheet.Hide();
            onAnswer?.Invoke(yes);
        }
    }
}
