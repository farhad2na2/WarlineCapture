using Game.Composition;
using Game.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Editor
{
    public static class FirstLaunchNarrativeReviewEntry
    {
        private const string MenuScenePath = "Assets/Game/Scenes/Menu.unity";
        private const string ResetProgressMenuPath = "Game/Narrative/First Launch/Reset Progress";

        [MenuItem(ResetProgressMenuPath)]
        public static void ResetProgress()
        {
            SaveService.CreateDefault().ResetFirstLaunchProgress();
            FirstLaunchNarrativeReviewUtilitySystemHelper.ClearRequest();
            Debug.Log(
                $"[FirstLaunchNarrative] Progress reset. Next Play will start the first-launch sequence. " +
                $"Profile={Application.persistentDataPath}/{SaveService.ProfileFileName}");
        }

        [MenuItem(ResetProgressMenuPath, true)]
        private static bool CanResetProgress()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        [MenuItem("Game/Narrative/First Launch/Review In Play Mode")]
        public static void StartReview()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
            FirstLaunchNarrativeReviewUtilitySystemHelper.Request();
            EditorApplication.isPlaying = true;
        }
    }
}
