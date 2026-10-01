using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FindMe.UI
{
    /// <summary>
    /// Screen 5: the overlay shown while searching. Top: what we look for + how many spots are checked.
    /// Bottom: stop, the big main button, calm. The middle stays empty for the AR cues.
    /// Main button: pink "Scan this area" until spots exist, then mint "I found it" (with a small
    /// "Scan another area" button above it so the user can still scan more of the room).
    /// </summary>
    public class ScanHudScreen : MonoBehaviour
    {
        public UITheme theme;
        public Image itemIcon;
        public TMP_Text itemName;
        public UIProgressBar bar;
        public TMP_Text countText, checkedLabel;
        public GameObject statusChip;
        public TMP_Text statusText;
        public ChunkyButton mainButton;
        public ChunkyButton scanMoreButton;
        public Button stopButton, calmButton;
        public Sprite scanIcon, foundIcon;

        public event Action onScan, onFound, onStop;

        bool scanEnabled;
        bool hasSpots;

        void Awake()
        {
            mainButton.Button.onClick.AddListener(() => { if (hasSpots) onFound?.Invoke(); else onScan?.Invoke(); });
            scanMoreButton.Button.onClick.AddListener(() => onScan?.Invoke());
            stopButton.onClick.AddListener(() => onStop?.Invoke());
            // calmButton: decorative until the breathing screen exists.
        }

        public void SetItem(ItemProfile p)
        {
            itemName.text = string.IsNullOrEmpty(p?.name) ? "" : char.ToUpperInvariant(p.name[0]) + p.name.Substring(1);
            itemIcon.sprite = theme.GetItemIcon(p?.name);
        }

        /// <summary>"2 of 5 checked": big "2/5", a small label, and the bar. Before the first scan (total 0) both stay empty.</summary>
        public void SetProgress(int done, int total)
        {
            hasSpots = total > 0;
            countText.text = hasSpots ? $"{done}/{total}" : "";
            checkedLabel.text = hasSpots ? "checked" : "";
            bar.Set(hasSpots ? (float)done / total : 0f);
            Refresh();
        }

        public void SetStatus(string message)
        {
            bool has = !string.IsNullOrEmpty(message);
            statusChip.SetActive(has);
            if (has) statusText.text = message;
        }

        public void SetScanEnabled(bool on)
        {
            scanEnabled = on;
            Refresh();
        }

        /// <summary>Back to the pre-search look (used when the search is stopped).</summary>
        public void ResetHud()
        {
            hasSpots = false; scanEnabled = false;
            SetProgress(0, 0);
            SetStatus(null);
        }

        void Refresh()
        {
            // No spots yet: pink "Scan this area". Spots exist: mint "I found it" + a small "Scan another area".
            mainButton.Apply(theme, hasSpots ? ButtonStyle.Success : ButtonStyle.Primary);
            mainButton.Text = hasSpots ? "I found it" : "Scan this area";
            if (mainButton.icon != null) mainButton.icon.sprite = hasSpots ? foundIcon : scanIcon;
            mainButton.SetInteractable(scanEnabled);
            scanMoreButton.gameObject.SetActive(hasSpots);
            scanMoreButton.SetInteractable(scanEnabled);
        }
    }
}
