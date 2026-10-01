using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FindMe.UI
{
    /// <summary>
    /// Root of the whole 2D UI (sits on the UIRoot prefab, next to its Canvas).
    /// On Awake it builds every screen from code and opens the first one.
    /// The search side (ISearchUI for SearchController) is in UIManager.Search.cs.
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public partial class UIManager : MonoBehaviour
    {
        [SerializeField] UITheme theme;
        [Tooltip("Screen shown at start. 'splash' for the app; other ids help when testing one screen.")]
        [SerializeField] string startScreen = "splash";

        public ScreenRouter Router { get; private set; }
        /// <summary>Account, things and history shown around the search (sample data for now).</summary>
        public AppState State { get; } = new AppState();
        /// <summary>The thing being registered in the "Add a thing" flow.</summary>
        public ThingDraft Draft { get; set; } = new ThingDraft();
        /// <summary>Set when the app was opened from a family invite (parent side of the flow).</summary>
        public bool FromInvite { get; set; }

        RectTransform toastLayer;

        void Awake() => Build();

        /// <summary>Builds all screens once. Awake calls it in Play mode; the Editor capture tool calls it directly.</summary>
        public void Build()
        {
            if (Router != null) return;
            UIKit.Init(theme);
            Tweens.ReduceMotion = theme.reduceMotion;
            SetupCanvas();

            var screens = UIKit.Stretch(UIKit.Rect(transform, "Screens"));
            Router = screens.gameObject.AddComponent<ScreenRouter>();
            foreach (var id in new[] { "find", "history", "things", "family", "settings" }) Router.Roots.Add(id);

            // Onboarding
            Add<SplashScreen>("splash");
            Add<WelcomeScreen>("welcome");
            Add<SignUpScreen>("signup");
            Add<LoginScreen>("login");
            Add<ProfileScreen>("profile");
            Add<PermissionsScreen>("perms");
            Add<CameraOffScreen>("camoff");
            Add<AskRegisterScreen>("askreg");
            // Registering things
            Add<RegisterPhotoScreen>("reg1");
            Add<RegisterSpotScreen>("reg2");
            Add<RegisterDoneScreen>("reg3");
            // Tabs
            Add<FindHomeScreen>("find");
            Add<HistoryScreen>("history");
            Add<ThingsScreen>("things");
            Add<FamilyScreen>("family");
            Add<SettingsScreen>("settings");
            Add<PaywallScreen>("paywall");
            Add<ItemScreen>("item");
            // Starting a search + the search itself (UIManager.Search.cs)
            Add<WhatScreen>("what");
            SetupSearch();

            // Toasts float above every screen.
            toastLayer = UIKit.Stretch(UIKit.Rect(transform, "Toasts"));
        }

        void Start() => Router.Go(startScreen);

        TScreen Add<TScreen>(string id) where TScreen : UIScreen
        {
            var s = UIScreen.Create<TScreen>(this, Router.transform, id);
            Router.Add(s);
            return s;
        }

        // ---------- small flows shared by several screens ----------

        /// <summary>After logging in: the invite flow continues to "Join family", otherwise home.</summary>
        public void AfterLogin() => Router.Go(FromInvite ? "joinFam" : "find");

        /// <summary>End of onboarding ("Later" / "Done").</summary>
        public void AfterOnboarding() => Router.Go(FromInvite ? "joinFam" : "find");

        /// <summary>
        /// .toast: grape pill near the bottom, fades in, stays ~2 s, fades out (prototype toast(), 2.4 s total).
        /// </summary>
        public void Toast(string message)
        {
            var row = UIKit.Row(toastLayer, "Toast", 0f, UIKit.Pad(18, 12, 18, 12), TextAnchor.MiddleCenter);
            var rt = (RectTransform)row.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 110f);
            row.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            row.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            UIKit.Backdrop(row.transform, theme.grape, 99f);
            var t = UIKit.Text(row.transform, message, TextStyle.Label, theme.white);
            t.fontSize = 14.5f;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            var g = row.gameObject.AddComponent<CanvasGroup>();
            g.blocksRaycasts = false;
            StartCoroutine(ToastLife(row.gameObject, g, rt));
        }

        IEnumerator ToastLife(GameObject go, CanvasGroup g, RectTransform rt)
        {
            g.alpha = 0f;
            StartCoroutine(Tweens.SlideIn(rt, new Vector2(0f, -10f), 0.24f));
            yield return Tweens.Fade(g, 1f, 0.24f);
            yield return new WaitForSecondsRealtime(1.8f);
            yield return Tweens.Fade(g, 0f, 0.36f);
            Destroy(go);
        }

        /// <summary>
        /// The canvas uses the prototype's own frame, 390 × 844 design px (iPhone 13 points), so every size
        /// can be copied straight from the CSS. Scale With Screen Size stretches it to any phone; match 0.5
        /// splits the difference between width and height on phones with other shapes (e.g. the Galaxy A80).
        /// </summary>
        void SetupCanvas()
        {
            var canvas = GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;                      // above any dev/debug canvases

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
