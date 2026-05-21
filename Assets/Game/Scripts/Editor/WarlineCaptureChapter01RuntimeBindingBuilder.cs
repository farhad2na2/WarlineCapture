#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class WarlineCaptureChapter01RuntimeBindingBuilder
{
    private const string GameScenePath = "Assets/Game/Scenes/Game2D.unity";
    private const string DefinitionPath = "Assets/Game/Data/TacticalMaps/Chapter01/iso.ch01.district_edge_01.asset";
    private const string GridConfigPath = "Assets/Game/Data/TacticalMaps/Chapter01/iso.ch01.district_edge_01.grid.asset";
    private const string BindingObjectName = "Chapter01_TacticalMissionRuntime";

    [MenuItem("WarlineCapture/Design/Bind Chapter01 Tactical Runtime To Game Scene")]
    public static void Build()
    {
        AssetDatabase.Refresh();

        Scene scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
        TacticalMapDefinition definition = AssetDatabase.LoadAssetAtPath<TacticalMapDefinition>(DefinitionPath);
        GridAuthoringConfig gridConfig = AssetDatabase.LoadAssetAtPath<GridAuthoringConfig>(GridConfigPath);
        if (definition == null || gridConfig == null)
        {
            Debug.LogError($"WARLINECAPTURE_CH01_RUNTIME_BINDING_MISSING_DATA definition={DefinitionPath} grid={GridConfigPath}");
            return;
        }

        GameBootstrap bootstrap = Object.FindAnyObjectByType<GameBootstrap>();
        if (bootstrap == null)
        {
            Debug.LogError($"WARLINECAPTURE_CH01_RUNTIME_BINDING_MISSING_BOOTSTRAP scene={GameScenePath}");
            return;
        }

        GameObject bindingObject = GameObject.Find(BindingObjectName);
        if (bindingObject == null)
            bindingObject = new GameObject(BindingObjectName);

        TacticalMapRuntimeLoader loader = bindingObject.GetComponent<TacticalMapRuntimeLoader>();
        if (loader == null)
            loader = bindingObject.AddComponent<TacticalMapRuntimeLoader>();

        Camera worldCamera = bootstrap.WorldCamera != null ? bootstrap.WorldCamera : Camera.main;
        loader.Configure(definition, gridConfig, worldCamera);
        SetSerializedBool(loader, "loadOnAwake", false);

        Chapter01MissionTacticalRuntimeBinder binder = bindingObject.GetComponent<Chapter01MissionTacticalRuntimeBinder>();
        if (binder == null)
            binder = bindingObject.AddComponent<Chapter01MissionTacticalRuntimeBinder>();

        SerializedObject binderObject = new(binder);
        binderObject.FindProperty("tacticalMapLoader").objectReferenceValue = loader;
        SerializedProperty definitions = binderObject.FindProperty("missionDefinitions");
        definitions.arraySize = 1;
        definitions.GetArrayElementAtIndex(0).objectReferenceValue = definition;
        SerializedProperty grids = binderObject.FindProperty("missionGridConfigs");
        grids.arraySize = 1;
        grids.GetArrayElementAtIndex(0).objectReferenceValue = gridConfig;
        binderObject.FindProperty("useDefaultMissionWhenNoSession").boolValue = true;
        binderObject.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject bootstrapObject = new(bootstrap);
        bootstrapObject.FindProperty("chapter01TacticalBinder").objectReferenceValue = binder;
        bootstrapObject.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(loader);
        EditorUtility.SetDirty(binder);
        EditorUtility.SetDirty(bootstrap);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.Refresh();

        Debug.Log($"WARLINECAPTURE_CH01_RUNTIME_BINDING_BUILT scene={GameScenePath} map={definition.MapId} grid={definition.GridWidth}x{definition.GridHeight}");
    }

    private static void SetSerializedBool(Object target, string propertyName, bool value)
    {
        SerializedObject serializedObject = new(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
        {
            Debug.LogWarning($"WARLINECAPTURE_CH01_RUNTIME_BINDING_PROPERTY_MISSING property={propertyName}");
            return;
        }

        property.boolValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }
}
#endif
