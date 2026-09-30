using UnityEngine;

namespace FindMe
{
    /// <summary>
    /// Runtime settings that can't live in Project Settings. Sits on "Systems" in the Main scene.
    /// </summary>
    public class AppBootstrap : MonoBehaviour
    {
        [Tooltip("30 keeps the Galaxy A80 cooler than 60 and is plenty for AR.")]
        [SerializeField] int targetFps = 30;

        void Awake()
        {
            // On mobile, targetFrameRate is the only frame cap. vSyncCount must be 0 or the
            // editor ignores targetFrameRate (phones ignore vSyncCount anyway).
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = targetFps;

            // The user holds the phone still on a spot for seconds at a time. Don't let the screen dim.
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            // Belt and braces with Player Settings: the box -> screen math assumes portrait.
            Screen.orientation = ScreenOrientation.Portrait;
        }
    }
}
