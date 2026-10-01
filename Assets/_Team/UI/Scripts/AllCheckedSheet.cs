using System;
using UnityEngine;

namespace FindMe.UI
{
    /// <summary>
    /// Screen 11: every spot is checked and the item still isn't found, so offer to scan another part of the room.
    /// A gentle sheet, not an error: "Not found yet, and that's okay."
    /// </summary>
    public class AllCheckedSheet : MonoBehaviour
    {
        public UIBottomSheet sheet;
        public ChunkyButton scanButton, notNowButton;

        public event Action onScan, onNotNow;

        void Awake()
        {
            scanButton.Button.onClick.AddListener(() => { sheet.Hide(); onScan?.Invoke(); });
            notNowButton.Button.onClick.AddListener(() => { sheet.Hide(); onNotNow?.Invoke(); });
        }

        public void Show() => sheet.Show();
        public void Hide() => sheet.Hide();
    }
}
