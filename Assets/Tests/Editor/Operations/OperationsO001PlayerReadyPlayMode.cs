#if UNITY_EDITOR
using System.IO;
using Game.Operations.Capture;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Tests.Editor.Operations
{
    [InitializeOnLoad]
    public static class OperationsO001PlayerReadyPlayMode
    {
        const string ActiveKey = "OperationsO001PlayerReady.Active";
        const string NewProfileKey = "OperationsO001PlayerReady.NewProfile";

        static OperationsO001PlayerReadyPlayMode()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        [MenuItem("Operations/P4R/Play O001 Player Shell")]
        public static void PlayExistingProfile() => Enter(false);

        [MenuItem("Operations/P4R/Play O001 New Profile")]
        public static void PlayNewProfile() => Enter(true);

        static void Enter(bool newProfile)
        {
            SessionState.SetBool(ActiveKey, true);
            SessionState.SetBool(NewProfileKey, newProfile);
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            EditorApplication.EnterPlaymode();
        }

        static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.EnteredPlayMode)
                return;
            if (!SessionState.GetBool(ActiveKey, false))
                return;
            SessionState.EraseBool(ActiveKey);
            string directory = Path.Combine(Application.persistentDataPath, "OperationsO001Player");
            if (SessionState.GetBool(NewProfileKey, false))
            {
                SessionState.EraseBool(NewProfileKey);
                if (Directory.Exists(directory))
                    Directory.Delete(directory, true);
            }

            OperationsO001PlayerReadyView.Begin(directory);
            Debug.Log("[OperationsO001PlayerReady] play_mode profile=" + directory);
        }
    }
}
#endif
