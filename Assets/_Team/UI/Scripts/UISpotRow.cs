using TMPro;
using UnityEngine;

namespace FindMe.UI
{
    /// <summary>One line of "Likely hiding spots": a numbered dark tile, the place name and an optional hint under it.</summary>
    public class UISpotRow : MonoBehaviour
    {
        public TMP_Text number;
        public TMP_Text title;
        public TMP_Text subtitle;

        public void Set(int n, string place, string hint = null)
        {
            number.text = n.ToString();
            title.text = place;
            // Gemini gives place names only; hide the hint line when there is none so rows don't have a gap.
            subtitle.text = hint ?? "";
            subtitle.gameObject.SetActive(!string.IsNullOrEmpty(hint));
        }
    }
}
