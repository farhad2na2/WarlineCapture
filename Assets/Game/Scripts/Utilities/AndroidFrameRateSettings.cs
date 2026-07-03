using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Runtime
{
    public static class AndroidFrameRateSettings
    {
        private const int TargetAndroidFrameRate = 120;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Apply()
        {
    #if UNITY_ANDROID && !UNITY_EDITOR
            QualitySettings.vSyncCount = 0;
            OnDemandRendering.renderFrameInterval = 1;
            Application.targetFrameRate = TargetAndroidFrameRate;
    #endif
        }
    }
}
