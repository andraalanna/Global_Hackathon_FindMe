using UnityEngine;
using UnityEngine.UI;

namespace FindMe.Onboarding
{
    /// <summary>
    /// The app's opening: splash (logo with zooming corner brackets) and the 3 welcome slides, including the
    /// premium Care plan slide (tilted crown on the blob + lemon "Premium" tag).
    /// It sits on its own canvas ABOVE the app UI (UIRoot); when the user finishes (Next on the last slide,
    /// Skip or Log in) it hides itself and the app's first screen underneath is already there.
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public class OnboardingManager : MonoBehaviour
    {
        [SerializeField] OnboardingTheme theme;

        public ScreenRouter Router { get; private set; }

        void Awake() => Build();

        public void Build()
        {
            if (Router != null) return;
            UIKit.Init(theme);
            Tweens.ReduceMotion = theme.reduceMotion;
            SetupCanvas();

            var screens = UIKit.Stretch(UIKit.Rect(transform, "Screens"));
            Router = screens.gameObject.AddComponent<ScreenRouter>();
            Add<SplashScreen>("splash");
            Add<WelcomeScreen>("welcome");
            lost = Add<LostScreen>("lost");
        }

        LostScreen lost;

        void Start() => Router.Go("splash");

        TScreen Add<TScreen>(string id) where TScreen : UIScreen
        {
            var s = UIScreen.Create<TScreen>(this, Router.transform, id);
            Router.Add(s);
            return s;
        }

        /// <summary>Onboarding done (or the lost screen closed): get out of the way of the app UI.</summary>
        public void Finish()
        {
            if (Router.Current != null) Router.Current.gameObject.SetActive(false);
            GetComponent<Canvas>().enabled = false;
        }

        /// <summary>
        /// The app UI's "Can't find it": the sorry screen with things to buy, on top of the app UI.
        /// Back home / Keep looking close it again and call back.
        /// </summary>
        public void ShowLost(string item, string appearance, string category, System.Action onHome, System.Action onKeepLooking)
        {
            GetComponent<Canvas>().enabled = true;
            lost.Set(item, appearance, category,
                () => { Finish(); onHome?.Invoke(); },
                () => { Finish(); onKeepLooking?.Invoke(); });
            Router.Go("lost");
        }

        /// <summary>
        /// Same frame as the prototype (390 × 844 design px = iPhone 13 points), scaled to any phone.
        /// Sorting order 50 keeps it above the app UI until Finish().
        /// </summary>
        void SetupCanvas()
        {
            var canvas = GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            if (!TryGetComponent(out CanvasScaler scaler)) scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(390f, 844f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            scaler.referencePixelsPerUnit = 100f;
            if (GetComponent<GraphicRaycaster>() == null) gameObject.AddComponent<GraphicRaycaster>();
        }
    }
}
