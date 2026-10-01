using System.IO;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using FindMe.Dev;

namespace FindMe.EditorTools
{
    /// <summary>
    /// Phase 0 in code, so the setup is readable and can be re-run.
    ///   FindMe/1. Apply Project Settings: Android player settings + Cues/Overlay layers
    ///   FindMe/2. Build Main Scene: creates the Main scene hierarchy from the plan
    ///   FindMe/3. Add Search Systems: adds/wires the Phase 2+ components into the open Main scene
    /// </summary>
    public static class FindMeProjectSetup
    {
        const string Root = "Assets/_Team/AR";
        const string ScenePath = Root + "/Scenes/Main.unity";
        const string MaterialPath = Root + "/Materials/PlaneDots.mat";
        const string PlanePrefabPath = Root + "/Prefabs/ARPlaneDots.prefab";
        const string PlaneShader = "FindMe/PlaneDots";
        const string GeminiConfigPath = Root + "/Settings/GeminiConfig.asset";
        public const string CuesLayer = "Cues";
        public const string OverlayLayer = "Overlay";

        [MenuItem("FindMe/1. Apply Project Settings")]
        public static void ApplyProjectSettings()
        {
            var android = NamedBuildTarget.Android;
            PlayerSettings.SetApplicationIdentifier(android, "com.aislaves.findme");
            // Plan says 24+, but Unity 6.5's floor is 26. The A80 runs Android 11 (API 30),
            // so the template's 34 would block install; 26 is the lowest Unity allows.
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.SetScriptingBackend(android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });
            PlayerSettings.Android.forceInternetPermission = true;  // "Internet Access: Require"

            // Portrait locked: stored screen size/projection must match between snapshot and raycast.
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;

            EnsureLayer(CuesLayer);
            EnsureLayer(OverlayLayer);
            AssetDatabase.SaveAssets();
            Debug.Log("[FindMe] Project settings applied.");
        }

        [MenuItem("FindMe/2. Build Main Scene")]
        public static void BuildMainSceneMenu()
        {
            if (File.Exists(ScenePath) &&
                !EditorUtility.DisplayDialog("Rebuild Main scene?",
                    "Main.unity already exists. Rebuilding replaces it (manual edits are lost).", "Rebuild", "Cancel"))
                return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            BuildMainScene();
        }

        public static void BuildMainScene()
        {
            foreach (var dir in new[] { "Scenes", "Materials", "Prefabs" })
                Directory.CreateDirectory($"{Root}/{dir}");
            AssetDatabase.Refresh();

            var planePrefab = EnsurePlanePrefab();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Let AR Foundation build these two: it wires the camera, background and TrackedPoseDriver correctly.
            Selection.activeObject = null;  // menu items create under the selection otherwise
            EditorApplication.ExecuteMenuItem("GameObject/XR/AR Session");
            EditorApplication.ExecuteMenuItem("GameObject/XR/XR Origin (Mobile AR)");
            var origin = Object.FindAnyObjectByType<XROrigin>();

            var planes = GetOrAdd<ARPlaneManager>(origin.gameObject);
            planes.planePrefab = planePrefab;
            // "Everything" for ARCore = horizontal + vertical (it has no other plane types).
            planes.requestedDetectionMode = PlaneDetectionMode.Horizontal | PlaneDetectionMode.Vertical;
            GetOrAdd<ARRaycastManager>(origin.gameObject);
            GetOrAdd<ARAnchorManager>(origin.gameObject);

            // Phase 3 moves this to the snapshot pose to cast rays. It never renders, so the Camera is off.
            var ghost = new GameObject("GhostCamera");
            ghost.AddComponent<Camera>().enabled = false;

            var systems = new GameObject("Systems");
            systems.AddComponent<AppBootstrap>();
            var readiness = systems.AddComponent<ARReadiness>();
            SetRef(readiness, "planeManager", planes);
            SetRef(readiness, "cameraTransform", origin.Camera.transform);

            new GameObject("Cues").layer = LayerMask.NameToLayer(CuesLayer);
            new GameObject("UI");  // the UI lead's prefab goes here

            var overlay = new GameObject("DebugOverlay") { layer = LayerMask.NameToLayer(OverlayLayer) };
            SetRef(overlay.AddComponent<ReadinessMeterDev>(), "readiness", readiness);

            AddSearchSystems();
            EditorSceneManager.SaveScene(scene, ScenePath);
            // Main is the only build scene. SampleScene stays in the project, just not in the build.
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            Debug.Log($"[FindMe] Built {ScenePath}");
        }

        [MenuItem("FindMe/3. Add Search Systems (Phases 2-5)")]
        public static void AddSearchSystemsMenu()
        {
            if (SceneManager.GetActiveScene().path != ScenePath)
            {
                EditorUtility.DisplayDialog("Open Main first", $"Open {ScenePath}, then run this again.", "OK");
                return;
            }
            AddSearchSystems();
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            Debug.Log("[FindMe] Search systems added to Main.");
        }

        /// <summary>
        /// Idempotent: safe to run again after pulling new scripts. Adds missing components to the
        /// existing objects and (re)wires their references. Never deletes anything.
        /// </summary>
        public static void AddSearchSystems()
        {
            var origin = Object.FindAnyObjectByType<XROrigin>();
            var planes = origin.GetComponent<ARPlaneManager>();
            var systems = GameObject.Find("Systems");
            var readiness = systems.GetComponent<ARReadiness>();

            // Phase 2: Gemini provider, search brain, dev UI, debug overlay
            var gemini = GetOrAdd<GeminiServiceProvider>(systems);
            SetRef(gemini, "config", EnsureGeminiConfig());

            var search = GetOrAdd<SearchController>(systems);
            SetRef(search, "gemini", gemini);
            SetRef(search, "readiness", readiness);
            SetRef(search, "arSession", Object.FindAnyObjectByType<ARSession>());

            var uiRoot = GameObject.Find("UI");
            var devUiGo = GameObject.Find("DevUI") ?? new GameObject("DevUI");
            devUiGo.transform.SetParent(uiRoot.transform, false);
            var devUi = GetOrAdd<DevUI>(devUiGo);
            SetRef(devUi, "search", search);
            SetRef(search, "ui", devUi);

            var overlayGo = GameObject.Find("DebugOverlay");
            var overlay = GetOrAdd<DebugOverlay>(overlayGo);
            SetRef(overlay, "search", search);
            SetRef(overlay, "gemini", gemini);
            SetRef(overlay, "readiness", readiness);
            SetRef(overlay, "planeManager", planes);
            // DevUI now draws the readiness meter and DebugOverlay the numbers; the Phase 1 meter would overlap.
            if (overlayGo.TryGetComponent<ReadinessMeterDev>(out var oldMeter)) oldMeter.enabled = false;

            // Phase 3: snapshot + ghost camera
            var snapshot = GetOrAdd<SnapshotCapture>(systems);
            SetRef(snapshot, "arCamera", origin.Camera);
            var gridSampler = GetOrAdd<SurfaceGridSampler>(systems);
            SetRef(gridSampler, "raycastManager", origin.GetComponent<ARRaycastManager>());
            SetRef(snapshot, "surfaceGrid", gridSampler);
            SetRef(overlay, "snapshot", snapshot);
            SetRef(search, "snapshot", snapshot);
            var ghost = GetOrAdd<GhostCamera>(GameObject.Find("GhostCamera"));

            // Phase 5: place cues in 3D
            var placer = GetOrAdd<SpotPlacer>(systems);
            SetRef(placer, "raycastManager", origin.GetComponent<ARRaycastManager>());
            SetRef(placer, "anchorManager", origin.GetComponent<ARAnchorManager>());
            SetRef(placer, "planeManager", planes);
            SetRef(placer, "ghost", ghost);
            SetRef(placer, "arCamera", origin.Camera);

            // Depth (ARCore Depth API): lets raycasts hit the real surface at a pixel, so cues sit ON the bag
            // instead of floating where a distance was guessed. Occlusion OFF: cues must never hide behind furniture.
            var occlusion = GetOrAdd<AROcclusionManager>(origin.Camera.gameObject);
            occlusion.requestedEnvironmentDepthMode = EnvironmentDepthMode.Fastest;   // cheapest; the A80 runs warm
            occlusion.requestedOcclusionPreferenceMode = OcclusionPreferenceMode.NoOcclusion;
            occlusion.environmentDepthTemporalSmoothingRequested = true;              // steadier depth = steadier hits
            EditorUtility.SetDirty(occlusion);
            SetRef(placer, "cuesRoot", GameObject.Find("Cues").transform);
            var planeVis = GetOrAdd<PlaneVisibility>(systems);
            SetRef(planeVis, "planeManager", planes);
            SetRef(search, "placer", placer);
            SetRef(search, "planes", planeVis);
            SetRef(overlay, "placer", placer);

            // Phase 6: cue prefab + off-screen arrow
            var cuePrefab = AssetDatabase.LoadAssetAtPath<SpotCue>(SpotCuePrefabBuilder.PrefabPath);
            if (cuePrefab == null) cuePrefab = SpotCuePrefabBuilder.Build();
            SetRef(placer, "cuePrefab", cuePrefab);
            var offscreen = GetOrAdd<OffscreenIndicator>(overlayGo);
            SetRef(offscreen, "arCamera", origin.Camera);
            SetRef(offscreen, "arrowSprite", CueArtBaker.Arrow);
            SetRef(search, "offscreen", offscreen);

            // Phase 7: focus + steadiness + dwell
            var focus = GetOrAdd<SpotFocus>(systems);
            SetRef(focus, "placer", placer);
            SetRef(focus, "arCamera", origin.Camera);
            SetRef(search, "focus", focus);
            var focusSo = new SerializedObject(focus);
            focusSo.FindProperty("dwellSeconds").floatValue = 1.2f;   // faster automatic checks (was 2 s)
            focusSo.ApplyModifiedPropertiesWithoutUndo();
            SetRef(overlay, "focus", focus);

            // Phase 8: automatic check
            var checker = GetOrAdd<SpotChecker>(systems);
            SetRef(checker, "snapshot", snapshot);
            SetRef(checker, "gemini", gemini);
            SetRef(search, "checker", checker);

            // Phase 9: scanning sweep
            SetRef(GetOrAdd<ScanSweep>(overlayGo), "search", search);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        static GeminiConfig EnsureGeminiConfig()
        {
            var cfg = AssetDatabase.LoadAssetAtPath<GeminiConfig>(GeminiConfigPath);
            if (cfg != null)
            {
                // Rewrite the file with the current fields (drops renamed/removed ones).
                AssetDatabase.ForceReserializeAssets(new[] { GeminiConfigPath });
                return cfg;
            }
            Directory.CreateDirectory(Path.GetDirectoryName(GeminiConfigPath));
            cfg = ScriptableObject.CreateInstance<GeminiConfig>();
            AssetDatabase.CreateAsset(cfg, GeminiConfigPath);
            return cfg;
        }

        static GameObject EnsurePlanePrefab()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (mat == null)
            {
                mat = new Material(Shader.Find(PlaneShader));
                AssetDatabase.CreateAsset(mat, MaterialPath);
            }

            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PlanePrefabPath);
            if (existing != null) return existing;

            var go = new GameObject("ARPlaneDots");
            GetOrAdd<MeshFilter>(go);
            GetOrAdd<ARPlane>(go);
            GetOrAdd<ARPlaneMeshVisualizer>(go);  // turns the plane boundary into a mesh each update
            var r = GetOrAdd<MeshRenderer>(go);
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, PlanePrefabPath);
            Object.DestroyImmediate(go);
            return prefab;
        }

        /// <summary>Writes a name into the first free user layer slot (8-31) if it isn't there yet.</summary>
        static void EnsureLayer(string name)
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tagManager.FindProperty("layers");
            for (int i = 0; i < layers.arraySize; i++)
                if (layers.GetArrayElementAtIndex(i).stringValue == name) return;

            for (int i = 8; i < layers.arraySize; i++)
            {
                var slot = layers.GetArrayElementAtIndex(i);
                if (!string.IsNullOrEmpty(slot.stringValue)) continue;
                slot.stringValue = name;
                tagManager.ApplyModifiedPropertiesWithoutUndo();
                return;
            }
            Debug.LogError($"[FindMe] No free layer slot for '{name}'.");
        }

        // Sets a private [SerializeField] from the editor, so runtime scripts don't need public setters.
        static void SetRef(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static T GetOrAdd<T>(GameObject go) where T : Component =>
            go.TryGetComponent<T>(out var c) ? c : go.AddComponent<T>();
    }
}
