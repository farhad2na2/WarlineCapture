using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Editor
{
    [InitializeOnLoad]
    internal static class MatchPlayModeMenuRedirectEditor
    {
        private const string MatchScenePath = "Assets/Game/Scenes/Match.unity";
        private const string MenuScenePath = "Assets/Game/Scenes/Menu.unity";
        private const string RestoreMatchSceneKey = "Game.RestoreMatchSceneAfterPlay";
        private const string RestoreScenePathKey = "Game.RestoreScenePathAfterPlay";

        static MatchPlayModeMenuRedirectEditor()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                RestoreMatchSceneAfterPlay();
                return;
            }

            if (state != PlayModeStateChange.ExitingEditMode)
            {
                return;
            }

            Scene activeScene = SceneManager.GetActiveScene();
            bool startedFromMatchScene = activeScene.path == MatchScenePath;
            bool startedFromCameralessPreviewScene = ShouldStartMenuFromCameralessPreviewScene(activeScene);
            if (!startedFromMatchScene && !startedFromCameralessPreviewScene)
            {
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorApplication.isPlaying = false;
                return;
            }

            SessionState.SetBool(RestoreMatchSceneKey, startedFromMatchScene);
            if (ShouldRestoreSceneAfterPlay(activeScene.path))
                SessionState.SetString(RestoreScenePathKey, activeScene.path);
            else
                SessionState.EraseString(RestoreScenePathKey);

            EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
        }

        private static void RestoreMatchSceneAfterPlay()
        {
            string restoreScenePath = SessionState.GetString(RestoreScenePathKey, string.Empty);
            bool restoreMatchScene = SessionState.GetBool(RestoreMatchSceneKey, false);
            if (!restoreMatchScene && string.IsNullOrEmpty(restoreScenePath))
            {
                return;
            }

            SessionState.EraseBool(RestoreMatchSceneKey);
            SessionState.EraseString(RestoreScenePathKey);

            Scene activeScene = SceneManager.GetActiveScene();
            string targetScenePath = restoreMatchScene ? MatchScenePath : restoreScenePath;
            if (activeScene.path == targetScenePath)
            {
                return;
            }

            EditorSceneManager.OpenScene(targetScenePath, OpenSceneMode.Single);
        }

        private static bool ShouldStartMenuFromCameralessPreviewScene(Scene scene)
        {
            if (scene.path == MenuScenePath || scene.path == MatchScenePath)
                return false;

            return IsPreviewLikeScenePath(scene.path) && !HasEnabledCamera(scene);
        }

        private static bool IsPreviewLikeScenePath(string scenePath)
        {
            return string.IsNullOrEmpty(scenePath) || scenePath.StartsWith("Temp/", System.StringComparison.Ordinal);
        }

        private static bool ShouldRestoreSceneAfterPlay(string scenePath)
        {
            return !string.IsNullOrEmpty(scenePath) &&
                scenePath.StartsWith("Assets/", System.StringComparison.Ordinal) &&
                scenePath != MenuScenePath;
        }

        private static bool HasEnabledCamera(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
                return false;

            GameObject[] rootGameObjects = scene.GetRootGameObjects();
            for (int i = 0; i < rootGameObjects.Length; i++)
            {
                Camera[] cameras = rootGameObjects[i].GetComponentsInChildren<Camera>(true);
                for (int cameraIndex = 0; cameraIndex < cameras.Length; cameraIndex++)
                {
                    Camera camera = cameras[cameraIndex];
                    if (camera != null && camera.enabled && camera.gameObject.activeInHierarchy)
                        return true;
                }
            }

            return false;
        }
    }
}
