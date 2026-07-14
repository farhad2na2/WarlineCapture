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
        private const string ResetAllLocalDataMenuPath = "Game/Progress/Reset All Local Data (Fresh Install)";

        [MenuItem(ResetAllLocalDataMenuPath)]
        public static void ResetAllLocalData()
        {
            if (!EditorUtility.DisplayDialog(
                    "Reset All Local Data",
                    "Delete all local progress and settings for Warline Capture? The next Play will behave like a fresh installation.",
                    "Reset Everything",
                    "Cancel"))
            {
                return;
            }

            SaveService.CreateDefault().DeleteAllSaveData();
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            Debug.Log(
                $"[LocalDataReset] Fresh-install reset complete. Deleted profile, settings, quick game, and PlayerPrefs. " +
                $"SaveRoot={Application.persistentDataPath}");
        }

        [MenuItem(ResetAllLocalDataMenuPath, true)]
        private static bool CanResetAllLocalData()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }

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
