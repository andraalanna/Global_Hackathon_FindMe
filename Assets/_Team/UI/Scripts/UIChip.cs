using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FindMe.UI
{
    /// <summary>
    /// 40 px pill: white with a 2 px cloud border; selected = grape fill + white text.
    /// The border is the outer image, the fill is a slightly smaller image on top of it.
    /// </summary>
    public class UIChip : MonoBehaviour
    {
        public Image border;     // outer pill (cloud, or grape when selected)
        public Image fill;       // inner pill (white, or grape when selected)
        public TMP_Text label;
        public Image icon;                 // optional
        public Sprite iconNormal, iconSelected;
        public UITheme theme;

        public void SetText(string s) => label.text = s;

        public void SetSelected(bool on)
        {
            border.color = on ? theme.grape : theme.cloud;
            fill.color = on ? theme.grape : theme.white;
            label.color = on ? theme.white : theme.grape;
            if (icon != null) icon.sprite = on ? iconSelected : iconNormal;   // grape icon on white, white icon on grape
        }
    }
}
