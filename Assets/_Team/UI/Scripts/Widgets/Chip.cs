using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FindMe.UI
{
    /// <summary>
    /// .chip: a tappable pill (padding 11/18, 2 px border). Selected = grape fill, white text.
    /// The screen decides what a tap means (toggle, or pick one of a group) through onClick.
    /// </summary>
    public class Chip : MonoBehaviour
    {
        public bool On { get; private set; }
        public string Label => label.text;

        Image border, fill;
        TextMeshProUGUI label;

        public static Chip Create(Transform parent, string text, bool on, Action<Chip> onClick)
        {
            var T = UIKit.T;
            var row = UIKit.Row(parent, "Chip " + text, 0f, UIKit.Pad(20, 13, 20, 13), TextAnchor.MiddleCenter);
            var c = row.gameObject.AddComponent<Chip>();
            c.border = UIKit.Backdrop(row.transform, T.cloud, 99f);
            c.border.raycastTarget = true;
            c.fill = UIKit.Backdrop(row.transform, T.white, 99f, 0f, 2f);
            c.label = UIKit.Text(row.transform, text, TextStyle.Label);
            c.label.fontSize = 15f;
            c.label.textWrappingMode = TextWrappingModes.NoWrap;

            var b = row.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            b.targetGraphic = c.border;
            b.onClick.AddListener(() => onClick?.Invoke(c));
            c.Set(on);
            return c;
        }

        public void Toggle() => Set(!On);

        public void Set(bool on)
        {
            var T = UIKit.T;
            On = on;
            border.color = on ? T.grape : T.cloud;
            fill.color = on ? T.grape : T.white;
            label.color = on ? T.white : T.grape;
        }

        /// <summary>Single choice: select this chip, unselect its siblings.</summary>
        public void Pick()
        {
            foreach (var other in transform.parent.GetComponentsInChildren<Chip>()) other.Set(other == this);
        }
    }
}
