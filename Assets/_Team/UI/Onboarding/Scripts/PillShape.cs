using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FindMe.Onboarding
{
    /// <summary>
    /// Keeps a sliced rounded image a perfect pill (radius = half its shorter side) at any size.
    /// Without this, Unity squeezes oversized 9-slice corners into ovals (e.g. the 26 × 9 page dots).
    /// </summary>
    [ExecuteAlways, RequireComponent(typeof(Image))]
    public class PillShape : UIBehaviour
    {
        const float SpriteBorder = 56f;     // rounded_28 slice border in source pixels

        protected override void OnEnable() => Fit();
        protected override void OnRectTransformDimensionsChange() => Fit();

        void Fit()
        {
            var r = ((RectTransform)transform).rect;
            float radius = Mathf.Min(r.width, r.height) * 0.5f;
            if (radius <= 0f) return;
            GetComponent<Image>().pixelsPerUnitMultiplier = SpriteBorder / radius;
        }
    }
}
