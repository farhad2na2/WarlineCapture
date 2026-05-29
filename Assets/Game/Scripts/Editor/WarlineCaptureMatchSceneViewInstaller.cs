#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class WarlineCaptureMatchSceneViewInstaller
{
    private const string MatchScenePath = "Assets/Game/Scenes/Match.unity";
    private const string LegacyGameBootstrapScriptGuid = "d4010000000000000000000000000001";

    private static readonly string[] RequiredMatchSceneFields =
    {
        "menuView",
        "worldCamera",
        "directionalLight",
        "globalVolume",
        "decorationCombinedMeshBaker"
    };

    [MenuItem("WarlineCapture/Scenes/Install Match Scene View")]
    public static void InstallMatchSceneView()
    {
        Scene scene = EditorSceneManager.OpenScene(MatchScenePath, OpenSceneMode.Single);
        MatchSceneView view = FindSingleSceneComponent<MatchSceneView>(scene);
        AssertNoLegacyGameBootstrapScriptReference();
        if (view == null)
            throw new InvalidOperationException($"Expected one MatchSceneView in {scene.path} before removing the legacy GameBootstrap scene dependency.");

        EditorUtility.SetDirty(view);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene, MatchScenePath))
            throw new InvalidOperationException($"Failed to save Match scene at {MatchScenePath}.");

        AssetDatabase.ImportAsset(MatchScenePath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        ValidateMatchSceneView();
        Debug.Log($"WARLINECAPTURE_MATCH_SCENE_VIEW_INSTALLED scene={MatchScenePath}");
    }

    [MenuItem("WarlineCapture/Scenes/Validate Match Scene View")]
    public static void ValidateMatchSceneView()
    {
        Scene scene = EditorSceneManager.OpenScene(MatchScenePath, OpenSceneMode.Single);
        MatchSceneView view = FindSingleSceneComponent<MatchSceneView>(scene);
        AssertNoLegacyGameBootstrapScriptReference();

        ValidateRequiredMatchSceneFields(view);
        Debug.Log($"WARLINECAPTURE_MATCH_SCENE_VIEW_VALIDATED scene={MatchScenePath}");
    }

    private static void ValidateRequiredMatchSceneFields(MatchSceneView destination)
    {
        SerializedObject destinationObject = new(destination);
        foreach (string fieldName in RequiredMatchSceneFields)
        {
            SerializedProperty destinationProperty = destinationObject.FindProperty(fieldName);
            if (destinationProperty == null)
                throw new InvalidOperationException($"Missing MatchSceneView field {fieldName}.");
            if (destinationProperty.objectReferenceValue == null)
                throw new InvalidOperationException($"MatchSceneView field {fieldName} must be assigned.");
        }
    }

    private static T FindSingleSceneComponent<T>(Scene scene) where T : Component
    {
        T result = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (T component in root.GetComponentsInChildren<T>(true))
            {
                if (result != null)
                    throw new InvalidOperationException($"Expected exactly one {typeof(T).Name} in {scene.path}.");

                result = component;
            }
        }

        if (result == null)
            throw new InvalidOperationException($"Expected one {typeof(T).Name} in {scene.path}.");

        return result;
    }

    private static void AssertNoLegacyGameBootstrapScriptReference()
    {
        string sceneText = File.ReadAllText(MatchScenePath);
        if (sceneText.Contains(LegacyGameBootstrapScriptGuid, StringComparison.Ordinal) ||
            sceneText.Contains("Assembly-CSharp::GameBootstrap", StringComparison.Ordinal))
            throw new InvalidOperationException($"{MatchScenePath} must not contain the retired GameBootstrap script reference.");
    }
}
#endif
