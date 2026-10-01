using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FindMe.UI
{
    /// <summary>
    /// Screen 3: what Gemini learned (name, shape, size) and its "Likely hiding spots" list.
    /// Back returns to the input screen; Start scanning moves on to the AR readiness step.
    /// </summary>
    public class ItemCardScreen : MonoBehaviour
    {
        public UITheme theme;
        public Image itemIcon;
        public TMP_Text itemName;
        public UIChip shapeChip, sizeChip;
        public RectTransform spotList;     // rows are created here
        public UISpotRow rowPrefab;
        public Button backButton;
        public ChunkyButton startButton;

        public event Action onBack, onStart;

        const int MaxRows = 5;

        void Awake()
        {
            backButton.onClick.AddListener(() => onBack?.Invoke());
            startButton.Button.onClick.AddListener(() => onStart?.Invoke());
        }

        public void Show(ItemProfile p)
        {
            itemName.text = Capitalize(p.name);
            itemIcon.sprite = theme.GetItemIcon(p.name);

            shapeChip.SetText(Capitalize(p.shape));
            sizeChip.SetText(p.sizeCm != null && p.sizeCm.Length >= 2 ? $"{p.WidthCm:0}×{p.HeightCm:0} cm" : "");
            sizeChip.gameObject.SetActive(p.sizeCm != null && p.sizeCm.Length >= 2);

            // Rebuild the list: destroy old rows, then one per place Gemini suggested (max 5 so it fits the screen).
            for (int i = spotList.childCount - 1; i >= 0; i--) Destroy(spotList.GetChild(i).gameObject);
            if (p.likelyPlaces == null) return;
            for (int i = 0; i < p.likelyPlaces.Count && i < MaxRows; i++)
                Instantiate(rowPrefab, spotList).Set(i + 1, Capitalize(p.likelyPlaces[i]));
        }

        static string Capitalize(string s) => string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);
    }
}
