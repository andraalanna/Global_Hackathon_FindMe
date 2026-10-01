using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FindMe.Sim
{
    /// <summary>
    /// Menu FindMe ▸ Sim ▸ Run search simulation: switches XR Simulation to the living room, opens Main, enters
    /// Play, adds the SearchSimulator, and when it's done leaves Play and restores the previous environment.
    /// Report + screenshots: Temp/Sim/.
    /// </summary>
    [InitializeOnLoad]
    public static class SimRunner
    {
        const string PrefsAsset = "Assets/XR/UserSimulationSettings/Resources/XRSimulationPreferences.asset";
        const string Pending = "FindMe.Sim.Pending";
        const string AccuracyOnly = "FindMe.Sim.AccuracyOnly";
        const string SavedEnv = "FindMe.Sim.SavedEnvironment";      // asset path of the environment to restore ("" = none)
        const string MainScene = "Assets/_Team/AR/Scenes/Main.unity";

        static SimRunner()
        {
            EditorApplication.playModeStateChanged += OnPlayMode;
        }

        [MenuItem("FindMe/Sim/Run search simulation (all scenarios)")]
        public static void Run() => Run(false);

        [MenuItem("FindMe/Sim/Run placement accuracy test only")]
        public static void RunAccuracy() => Run(true);

        static void Run(bool accuracyOnly)
        {
            if (EditorApplication.isPlaying) { Debug.LogWarning("[Sim] Stop Play mode first."); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var room = LivingRoomBuilder.Build();                       // rebuilt every run: always the latest room

            // Remember the current simulation environment, then use the living room for this run.
            var prefs = new SerializedObject(AssetDatabase.LoadAssetAtPath<Object>(PrefsAsset));
            var env = prefs.FindProperty("m_EnvironmentPrefab");
            if (!SessionState.GetBool(Pending, false))
                EditorPrefs.SetString(SavedEnv, env.objectReferenceValue != null ? AssetDatabase.GetAssetPath(env.objectReferenceValue) : "");
            env.objectReferenceValue = room;
            prefs.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(prefs.targetObject);
            AssetDatabase.SaveAssetIfDirty(prefs.targetObject);

            if (EditorSceneManager.GetActiveScene().path != MainScene) EditorSceneManager.OpenScene(MainScene);
            SessionState.SetBool(Pending, true);
            SessionState.SetBool(AccuracyOnly, accuracyOnly);
            EditorApplication.EnterPlaymode();
        }

        static void OnPlayMode(PlayModeStateChange change)
        {
            if (!SessionState.GetBool(Pending, false)) return;
            if (change == PlayModeStateChange.EnteredPlayMode)
            {
                var sim = new GameObject("SearchSimulator").AddComponent<SearchSimulator>();
                sim.accuracyOnly = SessionState.GetBool(AccuracyOnly, false);
                // The snapshot waits for a rendered frame, and the Editor only renders the Game view while its tab
                // is visible: bring it to the front so scans can't stall behind the Scene view.
                var gameView = System.Type.GetType("UnityEditor.GameView,UnityEditor");
                if (gameView != null) EditorWindow.GetWindow(gameView, false, null, true).Focus();
                EditorApplication.update += WatchForEnd;
                void WatchForEnd()
                {
                    if (sim != null && !sim.Done) return;
                    EditorApplication.update -= WatchForEnd;
                    if (sim == null) Debug.LogError("[Sim] The simulator didn't start (see the error above); leaving Play mode.");
                    EditorApplication.ExitPlaymode();
                }
            }
            else if (change == PlayModeStateChange.EnteredEditMode)
            {
                SessionState.SetBool(Pending, false);
                RestoreEnvironment();
                Debug.Log("[Sim] Finished. Open Temp/Sim/report.md");
            }
        }

        /// <summary>Puts back whatever environment was selected before the simulation run.</summary>
        [MenuItem("FindMe/Sim/Restore previous simulation environment")]
        public static void RestoreEnvironment()
        {
            string path = EditorPrefs.GetString(SavedEnv, "");
            var prefs = new SerializedObject(AssetDatabase.LoadAssetAtPath<Object>(PrefsAsset));
            prefs.FindProperty("m_EnvironmentPrefab").objectReferenceValue =
                string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<GameObject>(path);
            prefs.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(prefs.targetObject);              // without this the change may never reach the file
            AssetDatabase.SaveAssetIfDirty(prefs.targetObject);
            Debug.Log("[Sim] Simulation environment restored to " + (string.IsNullOrEmpty(path) ? "the default" : path));
        }
    }
}
