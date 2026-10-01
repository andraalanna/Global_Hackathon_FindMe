using System;
using UnityEngine;
using UnityEngine.UI;

namespace FindMe.UI
{
    /// <summary>.sw: 54 × 32 track with a 26 px white knob. On = mint-edge green, knob slides right (0.2 s).</summary>
    public class Switch : MonoBehaviour
    {
        public bool On { get; private set; }
        public event Action<bool> Changed;

        Image track;
        RectTransform knob;

        public static Switch Create(Transform parent, bool on)
        {
            var T = UIKit.T;
            var rt = UIKit.Rect(parent, "Switch");
            UIKit.Size(rt, 54f, 32f);
            rt.sizeDelta = new Vector2(54f, 32f);
            var s = rt.gameObject.AddComponent<Switch>();
            s.track = UIKit.Box(rt, "Track", T.cloud, 99f);
            UIKit.Stretch((RectTransform)s.track.transform);
            s.track.raycastTarget = true;
            s.knob = (RectTransform)UIKit.Circle(rt, "Knob", T.white, 26f).transform;
            s.knob.anchorMin = s.knob.anchorMax = new Vector2(0f, 0.5f);
            var b = rt.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            b.targetGraphic = s.track;
            b.onClick.AddListener(() => s.Set(!s.On, true));
            s.Set(on, false);
            return s;
        }

        public void Set(bool on, bool animate)
        {
            On = on;
            track.color = on ? UIKit.T.mintEdge : UIKit.T.cloud;
            float x = on ? 54f - 3f - 13f : 3f + 13f;           // knob centre: 3 px from the edge
            if (animate && isActiveAndEnabled)
            {
                StopAllCoroutines();
                float from = knob.anchoredPosition.x;
                StartCoroutine(Tweens.Run(0.2f, Tweens.OutCubic, k => knob.anchoredPosition = new Vector2(Mathf.Lerp(from, x, k), 0f)));
                Changed?.Invoke(on);
            }
            else knob.anchoredPosition = new Vector2(x, 0f);
        }
    }
}
