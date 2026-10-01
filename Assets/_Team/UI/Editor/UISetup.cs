using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace FindMe.UI.EditorTools
{
    /// <summary>
    /// One-click setup, menu FindMe ▸ UI. Run the items in order the first time; each is safe to re-run.
    /// 1. Re-imports the art as sprites, creates TextMeshPro fonts, creates/fills the UITheme asset.
    /// 2. Saves the UIRoot prefab (Canvas + UIManager). 3. Creates the UI_Test scene.
    /// </summary>
    public static class UISetup
    {
        const string UI = "Assets/_Team/UI";
        const string Art = UI + "/Art";
        const string FontOut = Art + "/Fonts/SDF";
        const string ThemePath = UI + "/Theme/UITheme.asset";
        const string PrefabPath = UI + "/Prefabs/UIRoot.prefab";
        const string ScenePath = UI + "/Scenes/UI_Test.unity";

        [MenuItem("FindMe/UI/1. Setup art, fonts + theme")]
        public static void SetupTheme()
        {
            // Force the import rule (UIArtImporter) onto art that was imported before it existed.
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Art }))
                AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(guid), ImportAssetOptions.ForceUpdate);

            var theme = AssetDatabase.LoadAssetAtPath<UITheme>(ThemePath);
            if (theme == null)
            {
                theme = ScriptableObject.CreateInstance<UITheme>();
                AssetDatabase.CreateAsset(theme, ThemePath);
            }
            theme.heading = MakeFont("Fredoka-SemiBold");
            theme.body = MakeFont("Nunito-Regular");
            theme.bodyBold = MakeFont("Nunito-Bold");
            theme.bodyBlack = MakeFont("Nunito-ExtraBold");
            // <b> in rich text uses the real heavier Nunito file instead of a fake (smeared) bold.
            // Index 7 = weight 700 ("bold") in TMP's weight table.
            theme.body.fontWeightTable[7].regularTypeface = theme.bodyBold;
            theme.bodyBold.fontWeightTable[7].regularTypeface = theme.bodyBlack;
            EditorUtility.SetDirty(theme.body);
            EditorUtility.SetDirty(theme.bodyBold);

            theme.rounded = LoadSprite(Art + "/Sprites/rounded_28.png");
            theme.circle = LoadSprite(Art + "/Sprites/circle.png");
            theme.logoBackground = LoadSprite(Art + "/Generated/logo_bg.png");
            theme.logoKey = LoadSprite(Art + "/Generated/logo_key.png");
            theme.logoKeySad = LoadSprite(Art + "/Generated/logo_key_sad.png");
            theme.ring = LoadSprite(Art + "/Generated/ring.png");
            theme.arc = LoadSprite(Art + "/Generated/arc.png");
            theme.bracket = LoadSprite(Art + "/Generated/bracket.png");
            theme.crownFill = LoadSprite(Art + "/Generated/crown_fill.png");
            theme.crownLine = LoadSprite(Art + "/Generated/crown_line.png");
            theme.icons = AssetDatabase.FindAssets("t:Sprite", new[] { Art + "/Icons" })
                .Select(g => LoadSprite(AssetDatabase.GUIDToAssetPath(g)))
                .Where(s => s != null).OrderBy(s => s.name).ToArray();

            EditorUtility.SetDirty(theme);
            AssetDatabase.SaveAssets();
            Debug.Log($"[UI] Theme ready: {theme.icons.Length} icons, fonts in {FontOut}");
        }

        [MenuItem("FindMe/UI/2. Build UIRoot prefab")]
        public static void BuildPrefab()
        {
            var theme = AssetDatabase.LoadAssetAtPath<UITheme>(ThemePath);
            if (theme == null) { Debug.LogError("[UI] Run step 1 first."); return; }

            var go = new GameObject("UIRoot", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.layer = 5;
            var mgr = go.AddComponent<UIManager>();
            // Private [SerializeField] fields are set through SerializedObject, exactly like dragging in the Inspector.
            var so = new SerializedObject(mgr);
            so.FindProperty("theme").objectReferenceValue = theme;
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
            UnityEngine.Object.DestroyImmediate(go);
            Debug.Log("[UI] Saved " + PrefabPath);
        }

        [MenuItem("FindMe/UI/3. Create UI_Test scene")]
        public static void CreateTestScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cam = new GameObject("Main Camera", typeof(Camera)).GetComponent<Camera>();
            cam.tag = "MainCamera";
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color32(0x1E, 0x17, 0x40, 255);   // dark grape stands in for the camera feed

            // The project uses the new Input System only, so UI clicks need its UI input module.
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null) { Debug.LogError("[UI] Run step 2 first."); return; }
            PrefabUtility.InstantiatePrefab(prefab);

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("[UI] Saved " + ScenePath + ". Set the Game view to 1170x2532 (iPhone 13) and press Play.");
        }

        /// <summary>
        /// Sets the Game view to iPhone 13 portrait (1170 × 2532), adding that size once. Unity has no public API
        /// for Game view sizes, so this goes through the editor's internal GameViewSizes class by reflection.
        /// </summary>
        [MenuItem("FindMe/UI/Game view: iPhone 13 (1170x2532)")]
        public static void GameViewIPhone13()
        {
            const string Label = "iPhone 13 (FindMe)";
            var asm = typeof(EditorWindow).Assembly;
            var sizesType = asm.GetType("UnityEditor.GameViewSizes");
            var sizes = typeof(ScriptableSingleton<>).MakeGenericType(sizesType).GetProperty("instance").GetValue(null);
            var groupType = sizesType.GetProperty("currentGroupType").GetValue(sizes);
            var group = sizesType.GetMethod("GetGroup").Invoke(sizes, new[] { groupType });
            var gt = group.GetType();
            var texts = (string[])gt.GetMethod("GetDisplayTexts").Invoke(group, null);
            int index = Array.FindIndex(texts, t => t.Contains(Label));
            if (index < 0)
            {
                var sizeType = asm.GetType("UnityEditor.GameViewSize");
                var kind = asm.GetType("UnityEditor.GameViewSizeType");
                var size = Activator.CreateInstance(sizeType, Enum.Parse(kind, "FixedResolution"), 1170, 2532, Label);
                gt.GetMethod("AddCustomSize").Invoke(group, new[] { size });
                index = (int)gt.GetMethod("GetTotalCount").Invoke(group, null) - 1;
            }
            var gvType = asm.GetType("UnityEditor.GameView");
            var gv = EditorWindow.GetWindow(gvType, false, null, false);
            gvType.GetMethod("SizeSelectionCallback", System.Reflection.BindingFlags.Instance |
                             System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)
                  .Invoke(gv, new object[] { index, null });
        }

        /// <summary>
        /// TextMeshPro font asset from a TTF in Art/Fonts. Dynamic: glyphs not pre-baked (e.g. accents someone types)
        /// are added on the fly from the TTF. Basic Latin is pre-baked so the first frames don't stutter.
        /// </summary>
        static TMP_FontAsset MakeFont(string ttfName)
        {
            string path = $"{FontOut}/{ttfName} SDF.asset";
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (existing != null) return existing;

            Directory.CreateDirectory(FontOut);
            var ttf = AssetDatabase.LoadAssetAtPath<Font>($"{Art}/Fonts/{ttfName}.ttf");
            // 64 pt samples, 6 px padding, one 1024² atlas: sharp up to the 52 px brand title, ~140 glyphs.
            var fa = TMP_FontAsset.CreateFontAsset(ttf, 64, 6, GlyphRenderMode.SDFAA, 1024, 1024,
                                                   AtlasPopulationMode.Dynamic, false);
            fa.name = ttfName + " SDF";
            AssetDatabase.CreateAsset(fa, path);
            // The material and atlas texture must be saved inside the font asset, or they vanish on reload.
            fa.material.name = fa.name + " Material";
            AssetDatabase.AddObjectToAsset(fa.material, fa);
            fa.atlasTextures[0].name = fa.name + " Atlas";
            AssetDatabase.AddObjectToAsset(fa.atlasTextures[0], fa);

            string ascii = new string(Enumerable.Range(32, 95).Select(i => (char)i).ToArray());
            fa.TryAddCharacters(ascii + "…’‘“”·–—•");     // (no ✓ in these fonts: use the check icon)
            EditorUtility.SetDirty(fa);
            return fa;
        }

        static Sprite LoadSprite(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    /// <summary>
    /// Renders UI screens to PNGs exactly as an iPhone 13 would show them (1170 × 2532 px = 390 × 844 points × 3,
    /// safe area 47 pt top / 34 pt bottom), without entering Play mode. Used to compare each Unity screen
    /// side by side with the HTML prototype. Output: Temp/UICaptures/&lt;id&gt;.png (menu FindMe ▸ UI ▸ Capture…).
    /// </summary>
    public static class UICapture
    {
        const int W = 1170, H = 2532, Scale = 3;
        const float SafeTop = 47f, SafeBottom = 34f;    // iPhone 13 portrait, in points
        public static readonly string OutDir = Path.GetFullPath("Temp/UICaptures");

        [MenuItem("FindMe/UI/Capture screens (iPhone 13)")]
        public static void CaptureAll() => Capture(("splash", null), ("welcome", null),
            ("welcome_2", s => ((WelcomeScreen)s).SetSlide(1)), ("welcome_3", s => ((WelcomeScreen)s).SetSlide(2)),
            ("signup", null), ("login", null), ("profile", null), ("perms", null), ("camoff", null), ("askreg", null),
            ("find", null), ("what", null), ("things", null), ("history", null), ("family", null), ("settings", null),
            ("paywall", null), ("item", null), ("reg1", null), ("reg2", null), ("reg3", null),
            // Search screens, in story order: each step leaves the fake search in the state the next one needs.
            ("analyzing", s => { var a = AppOf(s); a.Search.Reset(a.State.mine[0]); a.Search.startTime -= 95f;
                                 a.ShowItemCard(new FindMe.ItemProfile { name = "keys", likelyPlaces = new System.Collections.Generic.List<string> { "Coat pockets", "Sofa cushions", "Bag" } });
                                 s.OnShow(); }),
            ("mapping", s => { var a = AppOf(s); a.SetReadiness(1f); a.SetScanEnabled(true); }),
            ("scan", s => { var a = AppOf(s); a.SetProgress(0, 4); a.ShowStatus("Found 4 places to check"); }),
            ("scan_spot", s => { var a = AppOf(s); a.SetProgress(1, 4); a.ShowSpotPanel("bag", "Open the bag and look inside", 0f, true); }),
            ("scan_match", s => { var a = AppOf(s); a.HideSpotPanel(); a.ShowStatus("There it is! Is that it?"); a.ShowMatch("sofa cushions"); }),
            ("foundWhere", null),
            ("found", s => { var a = AppOf(s); a.Search.foundAt = "under the side table"; a.Search.endTime = Time.unscaledTime; s.OnShow(); }),
            ("summary", null), ("notfound", null),
            ("family_home", s => { AppOf(s).State.carePlan = true; s.OnShow(); }));

        static UIManager AppOf(UIScreen s) => s.GetComponentInParent<UIManager>(true);

        /// <summary>Capture just these screen ids (no setup step), e.g. CaptureOnly("find", "what").</summary>
        public static void CaptureOnly(params string[] ids) =>
            Capture(Array.ConvertAll(ids, id => (id, (Action<UIScreen>)null)));

        /// <summary>Each entry: file name, and an optional tweak run after the screen is shown (e.g. pick a slide).
        /// The screen id is the file name up to the first '_'.</summary>
        public static void Capture(params (string name, Action<UIScreen> setup)[] shots)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Team/UI/Prefabs/UIRoot.prefab");
            var root = UnityEngine.Object.Instantiate(prefab);
            root.hideFlags = HideFlags.HideAndDontSave;          // never saved into whatever scene is open
            var camGo = new GameObject("UICaptureCam") { hideFlags = HideFlags.HideAndDontSave };
            var rt = new RenderTexture(W, H, 24);
            try
            {
                var cam = camGo.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Color.black;
                cam.targetTexture = rt;
                cam.cullingMask = 1 << 5;                        // UI layer only

                var mgr = root.GetComponent<UIManager>();
                mgr.Build();
                // Render the overlay canvas through our camera at a fixed 3× scale: 390 × 844 design px → 1170 × 2532.
                var canvas = root.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cam;
                canvas.planeDistance = 1f;
                var scaler = root.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                scaler.scaleFactor = Scale;

                Directory.CreateDirectory(OutDir);
                foreach (var (name, setup) in shots)
                {
                    var screen = mgr.Router.ShowNow(name.Split('_')[0]);
                    setup?.Invoke(screen);
                    FakeSafeArea(root);
                    Canvas.ForceUpdateCanvases();
                    LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)screen.transform);
                    Canvas.ForceUpdateCanvases();
                    screen.AfterLayout();
                    Canvas.ForceUpdateCanvases();
                    cam.Render();
                    File.WriteAllBytes(Path.Combine(OutDir, name + ".png"), ReadPixels(rt));
                }
                Debug.Log($"[UI] {shots.Length} captures in {OutDir}");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(camGo);
                rt.Release();
            }
        }

        // SafeArea only runs in Play mode, so set the iPhone 13 insets by hand.
        static void FakeSafeArea(GameObject root)
        {
            foreach (var sa in root.GetComponentsInChildren<SafeArea>(true))
            {
                var r = (RectTransform)sa.transform;
                r.anchorMin = new Vector2(0f, SafeBottom / 844f);
                r.anchorMax = new Vector2(1f, sa.applyTop ? 1f - SafeTop / 844f : 1f);
                r.offsetMin = r.offsetMax = Vector2.zero;
            }
        }

        static byte[] ReadPixels(RenderTexture rt)
        {
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            RenderTexture.active = prev;
            var png = tex.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(tex);
            return png;
        }
    }
}
