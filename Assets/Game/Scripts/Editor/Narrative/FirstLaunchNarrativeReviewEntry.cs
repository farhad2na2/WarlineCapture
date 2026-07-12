using Game.Composition;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Game.Editor
{
    public static class FirstLaunchNarrativeReviewEntry
    {
        private const string MenuScenePath = "Assets/Game/Scenes/Menu.unity";

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
