using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace FindMe.Onboarding
{
    /// <summary>
    /// Menu FindMe ▸ Onboarding ▸ Build prefab: saves OnboardingRoot.prefab (Canvas + OnboardingManager + theme).
    /// The theme asset (OnboardingTheme.asset) already points at its fonts, sprites and icons.
    /// </summary>
    public static class OnboardingSetup
    {
        const string Root = "Assets/_Team/UI/Onboarding";
        public const string PrefabPath = Root + "/OnboardingRoot.prefab";

        [MenuItem("FindMe/Onboarding/Build prefab")]
        public static void BuildPrefab()
        {
            var theme = AssetDatabase.LoadAssetAtPath<OnboardingTheme>(Root + "/OnboardingTheme.asset");
            if (theme == null) { Debug.LogError("[Onboarding] OnboardingTheme.asset missing"); return; }

            var go = new GameObject("OnboardingRoot", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.layer = 5;
            var mgr = go.AddComponent<OnboardingManager>();
            var so = new SerializedObject(mgr);
            so.FindProperty("theme").objectReferenceValue = theme;
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
            Object.DestroyImmediate(go);
            Debug.Log("[Onboarding] Saved " + PrefabPath);
        }
    }
}
