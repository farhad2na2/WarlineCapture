using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
internal static class MatchPlayModeMenuRedirectEditor
{
    private const string MatchScenePath = "Assets/Game/Scenes/Match.unity";
    private const string MenuScenePath = "Assets/Game/Scenes/Menu.unity";
    private const string RestoreMatchSceneKey = "WarlineCapture.RestoreMatchSceneAfterPlay";

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
        if (activeScene.path != MatchScenePath)
        {
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            EditorApplication.isPlaying = false;
            return;
        }

        SessionState.SetBool(RestoreMatchSceneKey, true);
        EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
    }

    private static void RestoreMatchSceneAfterPlay()
    {
        if (!SessionState.GetBool(RestoreMatchSceneKey, false))
        {
            return;
        }

        SessionState.EraseBool(RestoreMatchSceneKey);

        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.path == MatchScenePath)
        {
            return;
        }

        EditorSceneManager.OpenScene(MatchScenePath, OpenSceneMode.Single);
    }
}
