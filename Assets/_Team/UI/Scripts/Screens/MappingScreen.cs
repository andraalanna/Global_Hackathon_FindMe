using TMPro;
using UnityEngine;

namespace FindMe.UI
{
    /// <summary>
    /// Prototype "mapping" over the live camera: a mint scan line sweeps down while the sheet shows how much
    /// of the room AR has understood (SetReadiness). "Start scanning" unlocks with SetScanEnabled(true) and
    /// takes the first photo (ScanArea), then opens the scan HUD.
    /// </summary>
    public class MappingScreen : UIScreen
    {
        TextMeshProUGUI pct, phase;
        RectTransform fill;
        ChunkyButton start;

        protected override void Build()
        {
            Background.color = Color.clear;                        // the AR camera shows through
            ScanLine.Create(Root);
            var page = Page();
            var top = CamWidgets.Top(page.transform, App.ExitSearch);
            UIKit.Space(top.transform).flexibleWidth = 1;            // pushes the pill to the right
            CamWidgets.Pill(top.transform, null, "Mapping the room", out _);
            UIKit.Space(page.transform, 0f, 1f);                   // camera area

            var sheet = CamWidgets.Sheet(page.transform);
            CamWidgets.SheetText(sheet.transform, "Slowly move your phone around",
                                 "We're building a map so we can remember every spot you check.");
            (pct, phase, fill) = CamWidgets.Progress(sheet.transform, T.mint);
            UIKit.Space(sheet.transform, 16f);
            start = UIKit.Button(sheet.transform, "Start scanning", ButtonKind.Primary, () =>
            {
                App.Actions.ScanArea();
                App.Router.Go("scan");
            });
        }

        public override void OnShow() => Refresh();

        /// <summary>Called on every SetReadiness / SetScanEnabled.</summary>
        public void Refresh()
        {
            if (pct == null) return;
            float p = App.Search.readiness;
            int n = Mathf.RoundToInt(p * 100f);
            pct.text = n + "%";
            // Same words as the prototype as the meter fills.
            phase.text = p < 0.4f ? "Finding surfaces…" : p < 0.8f ? "Marking furniture…" : p < 1f ? "Almost there…" : "Room mapped";
            CamWidgets.SetBar(fill, p);
            start.SetInteractable(App.Search.scanEnabled);
        }
    }
}
