#if UNITY_EDITOR
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class WarlineCaptureM01RuntimeMapLoaderValidationBuilder
{
    private const string DefinitionPath = "Assets/Game/Data/TacticalMaps/Chapter01/iso.ch01.district_edge_01.asset";
    private const string GridConfigPath = "Assets/Game/Data/TacticalMaps/Chapter01/iso.ch01.district_edge_01.grid.asset";
    private const string ScenePath = "Assets/Game/Scenes/DesignTargets/Chapter01/Chapter01_M01_RuntimeMapLoaderValidation.unity";

    [MenuItem("WarlineCapture/Design/Build Chapter01 M01 Runtime Map Loader Validation")]
    public static void Build()
    {
        AssetDatabase.Refresh();

        Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), Path.GetDirectoryName(ScenePath)));
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        TacticalMapDefinition definition = AssetDatabase.LoadAssetAtPath<TacticalMapDefinition>(DefinitionPath);
        GridAuthoringConfig gridConfig = AssetDatabase.LoadAssetAtPath<GridAuthoringConfig>(GridConfigPath);
        if (definition == null || gridConfig == null)
        {
            Debug.LogError($"WARLINECAPTURE_M01_RUNTIME_MAP_LOADER_VALIDATION_MISSING_DATA definition={DefinitionPath} grid={GridConfigPath}");
            return;
        }
        Debug.Log($"WARLINECAPTURE_M01_RUNTIME_MAP_LOADER_VALIDATION_DATA_LOADED definitionName={definition.name} gridName={gridConfig.name}");

        Camera camera = CreateCamera(definition);
        GameObject root = new("Chapter01_M01_RuntimeMapLoaderValidation");
        TacticalMapRuntimeLoader loader = root.AddComponent<TacticalMapRuntimeLoader>();
        loader.Configure(definition, gridConfig, camera);
        SetPrivateField(loader, "loadOnAwake", false);
        loader.Load();
        EditorUtility.SetDirty(loader);
        EditorUtility.SetDirty(root);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePath);
        AssetDatabase.Refresh();
        Debug.Log($"WARLINECAPTURE_M01_RUNTIME_MAP_LOADER_VALIDATION_BUILT scene={ScenePath} definition={DefinitionPath} children={root.transform.childCount}");
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null)
        {
            Debug.LogError($"WARLINECAPTURE_M01_RUNTIME_MAP_LOADER_VALIDATION_FIELD_MISSING field={fieldName}");
            return;
        }

        field.SetValue(target, value);
    }

    private static Camera CreateCamera(TacticalMapDefinition definition)
    {
        GameObject cameraObject = new("M01_RuntimeLoader_CloseGameplayCamera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.035f, 0.039f, 0.040f, 1f);
        camera.orthographic = true;
        camera.orthographicSize = definition.DefaultOrthographicSize;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 100f;
        camera.transform.position = new Vector3(definition.CameraDefaultCenter.x, 10f, definition.CameraDefaultCenter.y);
        camera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        return camera;
    }
}
#endif
