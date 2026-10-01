using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FindMe.UI
{
    /// <summary>
    /// Screen 6: the bottom sheet for the hiding spot the user is looking at. SearchController calls Show() every frame
    /// while a spot is focused (with the dwell progress), so Show() only touches the UI when something changed.
    /// It tells the story with a chip: "Hold still" -> "Looking..." -> or a tip ("Open it") if Gemini needs more.
    /// The manual buttons (✓ Checked / Found it) appear only after a failed or unclear automatic check.
    /// </summary>
    public class SpotPanel : MonoBehaviour
    {
        public UITheme theme;
        public UIBottomSheet sheet;
        public Image chipBackground;
        public TMP_Text chipText;
        public TMP_Text title;
        public TMP_Text body;
        public UIProgressBar dwellBar;
        public GameObject buttonsRow;
        public ChunkyButton checkedButton, foundButton;

        public event Action onChecked, onFound;

        // The two fixed messages SearchController sends while no tip exists (see UpdateSpotPanel in SearchController).
        const string LookingTip = "Looking…", HoldStillTip = "Hold still…", ManualTip = "Checked it yourself?";

        string shownLabel, shownTip;
        bool shownManual;

        void Awake()
        {
            checkedButton.Button.onClick.AddListener(() => onChecked?.Invoke());
            foundButton.Button.onClick.AddListener(() => onFound?.Invoke());
        }

        public void Show(string label, string tip, float dwell01, bool manualButtons)
        {
            // Texts and colours only change when the spot or the message changes (not 60 times a second).
            if (label != shownLabel || tip != shownTip || manualButtons != shownManual)
            {
                shownLabel = label; shownTip = tip; shownManual = manualButtons;
                title.text = Capitalize(label);
                buttonsRow.SetActive(manualButtons);

                if (tip == LookingTip)        SetChip("Looking…", theme.soda, null);
                else if (tip == HoldStillTip) SetChip("Hold still", theme.lemon, null);
                else if (tip == ManualTip)    SetChip("Need a hand?", theme.lavender, tip);
                else                          SetChip("Open it", theme.tangerine, tip);   // Gemini's tip, e.g. "Open the drawer"
            }
            dwellBar.Set(dwell01, false);   // fed every frame, so no easing
            sheet.Show();                   // no-op if already open
        }

        public void Hide()
        {
            shownLabel = shownTip = null;
            sheet.Hide();
        }

        void SetChip(string text, Color color, string bodyText)
        {
            chipText.text = text;
            chipBackground.color = color;
            body.text = bodyText ?? "";
            body.gameObject.SetActive(!string.IsNullOrEmpty(bodyText));
        }

        static string Capitalize(string s) => string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);
    }
}
