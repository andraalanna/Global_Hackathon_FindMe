using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem.UI;
using UnityEngine.EventSystems;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace FindMe.UI.EditorTools
{
    /// <summary>
    /// One menu click (FindMe > UI > Build Foundation) builds the whole UI:
    /// sprite import settings, TMP font assets, UITheme, screens 1-11, the UIRoot prefab and the UI_Test scene.
    /// Safe to run again: it updates the same assets in place.
    /// </summary>
    public static class FoundationBuilder
    {
        const string Root = "Assets/_Team/UI";
        const string ThemePath = Root + "/Theme/UITheme.asset";
        const string PrefabPath = Root + "/Prefabs/UIRoot.prefab";
        const string ScenePath = Root + "/Scenes/UI_Test.unity";

        [MenuItem("FindMe/UI/Build Foundation")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            ConfigureSprites();
            var theme = BuildTheme();
            // A missing font would silently turn into TMP's default LiberationSans, so fail loudly instead.
            if (theme.heading == null || theme.bodyRegular == null || theme.bodyBold == null || theme.bodyExtraBold == null)
                Debug.LogError("FindMe UI: a TMP font asset is missing from UITheme; text would fall back to LiberationSans.");
            BuildUIRootAndTestScene(theme);
            AssetDatabase.SaveAssets();
            Debug.Log("FindMe UI: Foundation built. Open Assets/_Team/UI/Scenes/UI_Test.unity and press Play (Game view 1170x2532).");
        }

        // ---------- 1. sprites ----------
        // The art PNGs import as plain textures. Turn them into Sprites. The rounded shapes are drawn at 2x of the
        // 390-wide design, so PPU = 100 * 2/3 makes them render at the 3x canvas scale, and the 9-slice border
        // (corner radius in texture px) stays the right size.
        static void ConfigureSprites()
        {
            AssetDatabase.StartAssetEditing();
            try
            {
                SetSprite(Root + "/Art/Sprites/rounded_18.png", 36, 200f / 3f);
                SetSprite(Root + "/Art/Sprites/rounded_28.png", 56, 200f / 3f);
                SetSprite(Root + "/Art/Sprites/pill.png", 100, 200f / 3f);
                SetSprite(Root + "/Art/Sprites/circle.png", 0, 200f / 3f);
                SetSprite(Root + "/Art/Sprites/logo.jpg", 0, 100f);
                foreach (var dir in new[] { "Icons", "Icons/white", "Mascot" })
                    foreach (var f in Directory.GetFiles(Root + "/Art/" + dir, "*.png"))
                        SetSprite(f.Replace('\\', '/'), 0, 100f);
            }
            finally { AssetDatabase.StopAssetEditing(); }
            AssetDatabase.Refresh();
        }

        static void SetSprite(string path, float border, float ppu)
        {
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) { Debug.LogWarning("FindMe UI: missing " + path); return; }
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = ppu;
            ti.spriteBorder = new Vector4(border, border, border, border);
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.SaveAndReimport();
        }

        static Sprite LoadSprite(string rel) => AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/Art/" + rel);

        // ---------- 2+3. fonts and theme ----------
        static TMP_FontAsset MakeFont(string ttfName)
        {
            string ttf = $"{Root}/Art/Fonts/{ttfName}.ttf";
            string outPath = $"{Root}/Art/Fonts/{ttfName} SDF.asset";
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(outPath);
            if (existing != null) return existing;

            var font = AssetDatabase.LoadAssetAtPath<Font>(ttf);
            // Dynamic = glyphs are added to the atlas as they're needed; 1024 atlas, SDF so text stays sharp at any size.
            var fa = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            fa.name = ttfName + " SDF";
            AssetDatabase.CreateAsset(fa, outPath);
            fa.material.name = fa.name + " Material";
            AssetDatabase.AddObjectToAsset(fa.material, fa);
            foreach (var tex in fa.atlasTextures) { tex.name = fa.name + " Atlas"; AssetDatabase.AddObjectToAsset(tex, fa); }

            // Pre-fill Latin so first use doesn't hitch: ASCII plus the typographic marks the copy uses.
            var sb = new System.Text.StringBuilder();
            for (char c = ' '; c <= '~'; c++) sb.Append(c);
            sb.Append("’“”…·–—•×");
            fa.TryAddCharacters(sb.ToString(), out _);
            EditorUtility.SetDirty(fa);
            return fa;
        }

        // Which icon stands for which item (matched by a word in the item name). Grape art, tinted white at use.
        static void SetItemIcons(UITheme theme)
        {
            UITheme.ItemIcon I(string keyword, string icon) =>
                new UITheme.ItemIcon { keyword = keyword, icon = AssetDatabase.LoadAssetAtPath<Sprite>($"{Root}/Art/Icons/{icon}.png") };
            theme.itemIcons = new[]
            {
                I("wallet", "icon_wallet"), I("key", "icon_key"), I("glasses", "icon_glasses"), I("id", "icon_id"),
                I("card", "icon_id"), I("bag", "icon_bag"), I("backpack", "icon_bag"), I("phone", "icon_phone"),
                I("earbud", "icon_buds"), I("airpod", "icon_buds"), I("pill", "icon_pill"),
            };
            theme.fallbackItemIcon = AssetDatabase.LoadAssetAtPath<Sprite>($"{Root}/Art/Icons/icon_tag.png");
        }

        static UITheme BuildTheme()
        {
            var theme = AssetDatabase.LoadAssetAtPath<UITheme>(ThemePath);
            if (theme == null)
            {
                theme = ScriptableObject.CreateInstance<UITheme>();
                AssetDatabase.CreateAsset(theme, ThemePath);
            }
            theme.heading = MakeFont("Fredoka-SemiBold");
            theme.bodyRegular = MakeFont("Nunito-Regular");
            theme.bodyBold = MakeFont("Nunito-Bold");
            theme.bodyExtraBold = MakeFont("Nunito-ExtraBold");
            theme.rounded18 = LoadSprite("Sprites/rounded_18.png");
            theme.rounded28 = LoadSprite("Sprites/rounded_28.png");
            theme.pill = LoadSprite("Sprites/pill.png");
            theme.circle = LoadSprite("Sprites/circle.png");
            SetItemIcons(theme);
            EditorUtility.SetDirty(theme);
            AssetDatabase.SaveAssets();
            return theme;
        }


        // ---------- 4. UIRoot prefab + UI_Test scene ----------
        const string SpotRowPath = Root + "/Prefabs/SpotRow.prefab";

        static void BuildUIRootAndTestScene(UITheme th)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // --- UIRoot: full-screen Canvas = Screens (each has its own SafeArea) + an Overlay layer for the toast. ---
            var rootGo = new GameObject("UIRoot", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = rootGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = rootGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = UITheme.ReferenceResolution;   // 1170x2532 = iPhone 13 x3
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var screens = UIFactory.Rect("Screens", rootGo.transform);
            UIFactory.Stretch(screens);
            var router = screens.gameObject.AddComponent<ScreenRouter>();

            var spotRow = ScreensBuilder.BuildSpotRowPrefab(th, SpotRowPath);
            var input = ScreensBuilder.BuildInput(th, screens);
            var analyzing = ScreensBuilder.BuildAnalyzing(th, screens);
            var itemCard = ScreensBuilder.BuildItemCard(th, screens, spotRow);
            var readiness = ScreensBuilder.BuildReadiness(th, screens);
            var hud = ScreensBuilder.BuildHud(th, screens);
            var found = ScreensBuilder.BuildFound(th, screens);
            var summary = ScreensBuilder.BuildSummary(th, screens);

            // Bottom sheets sit directly under the canvas (not in a SafeArea) so they reach the screen's bottom edge.
            var spotPanel = ScreensBuilder.BuildSpotPanel(th, rootGo.transform);
            var matchSheet = ScreensBuilder.BuildMatchSheet(th, rootGo.transform);
            var allChecked = ScreensBuilder.BuildAllChecked(th, rootGo.transform);

            // The toast (and later the sheets) sit above the screens, inside the safe area.
            var overlay = UIFactory.Rect("Overlay", rootGo.transform);
            UIFactory.Stretch(overlay);
            overlay.gameObject.AddComponent<SafeArea>();
            var toast = UIFactory.Toast(th, overlay, AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/Art/Icons/white/icon_check_white.png"));

            var mgr = rootGo.AddComponent<UIManager>();
            mgr.theme = th; mgr.router = router;
            mgr.inputScreen = input; mgr.analyzingScreen = analyzing; mgr.itemCardScreen = itemCard; mgr.toast = toast;
            mgr.readinessScreen = readiness; mgr.hudScreen = hud;
            mgr.spotPanel = spotPanel; mgr.matchSheet = matchSheet; mgr.allCheckedSheet = allChecked;
            mgr.foundScreen = found; mgr.summaryScreen = summary;

            var prefab = PrefabUtility.SaveAsPrefabAsset(rootGo, PrefabPath);
            Object.DestroyImmediate(rootGo);

            // --- Test scene: an instance of the prefab + the fake Gemini driver + a camera and an input module. ---
            var camGo = new GameObject("Main Camera", typeof(Camera));
            camGo.tag = "MainCamera";
            var cam = camGo.GetComponent<Camera>();
            // Stands in for the AR camera feed: the design's muted purple-grey room colour, so HUD overlays read like on the phone.
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color32(0x6F, 0x68, 0x83, 255);
            cam.orthographic = true;

            // The project uses the new Input System only, so the UI needs its module.
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            var driver = new GameObject("FakeSearchDriver").AddComponent<FakeSearchDriver>();
            driver.ui = instance.GetComponent<UIManager>();

            EditorSceneManager.SaveScene(scene, ScenePath);
        }
    }
}
