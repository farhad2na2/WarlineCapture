using System;
using System.Linq;
using Game.Scripts.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using Unity.Scenes;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class WarlineCaptureGameSceneIsolationBuilder
{
    private const string Old2DScenePath = "Assets/Game/Scenes/Game2D.unity";
    private const string DefaultScenePath = "Assets/Game/Scenes/Game.unity";
    private const string Old2DSubScenePath = "Assets/Game/Scenes/Game2D/GameSubScene.unity";
    private const string DefaultSubSceneFolder = "Assets/Game/Scenes/Game";
    private const string DefaultSubScenePath = "Assets/Game/Scenes/Game/GameSubScene.unity";
    private const string ProductionDecorationRootName = "RuntimeDecorations_Production";

    private static readonly string[] DefaultGameRootNames =
    {
        "Main Camera",
        "Global Volume",
        "SM_Skydome_01",
        "Ground",
        "Decorations",
        "UI_Canvas",
        "Directional Light (1)"
    };

    private static readonly string[] Old2DRootsRemovedFromDefault =
    {
        "WarlineCaptureUIBootstrap",
        "Chapter01_TacticalMissionRuntime",
        "RuntimeDecorations_Production",
        "Directional Light"
    };

    private static readonly string[] DefaultGameRootsRemovedFromOld2D =
    {
        "Main Camera_Experiment",
        "Global Volume_Experiment",
        "SM_Skydome_01",
        "Ground",
        "Decorations",
        "UI_Canvas",
        "Directional Light (1)",
        "Global Volume"
    };

    [MenuItem("WarlineCapture/Scenes/Build Game Scene Isolation")]
    public static void Build()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(DefaultScenePath) == null)
        {
            if (!AssetDatabase.CopyAsset(Old2DScenePath, DefaultScenePath))
                throw new InvalidOperationException($"Failed to create default Game scene copy at {DefaultScenePath}.");

            AssetDatabase.ImportAsset(DefaultScenePath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        }

        Scene defaultScene = EditorSceneManager.OpenScene(DefaultScenePath, OpenSceneMode.Single);
        EnsureDefaultSubSceneAsset();
        AssignSubSceneAsset(defaultScene, DefaultSubScenePath);
        int removedOld2DRootsFromDefault = RemoveRoots(defaultScene, Old2DRootsRemovedFromDefault);
        ConfigureDefaultGameBootstrap(defaultScene);

        EditorSceneManager.MarkSceneDirty(defaultScene);
        if (!EditorSceneManager.SaveScene(defaultScene))
            throw new InvalidOperationException($"Failed to save isolated default Game scene at {DefaultScenePath}.");

        Scene old2DScene = EditorSceneManager.OpenScene(Old2DScenePath, OpenSceneMode.Single);
        int removedDefaultRootsFromOld2D = RemoveRoots(old2DScene, DefaultGameRootsRemovedFromOld2D);
        GameObject productionDecorationRoot = EnsureProductionDecorationRoot(old2DScene);
        ConfigureOld2DBootstrap(old2DScene, productionDecorationRoot.transform);

        EditorSceneManager.MarkSceneDirty(old2DScene);
        if (!EditorSceneManager.SaveScene(old2DScene))
            throw new InvalidOperationException($"Failed to save cleaned old 2D scene at {Old2DScenePath}.");

        ValidateScenes();
        Debug.Log($"WARLINECAPTURE_GAME_SCENE_ISOLATION_BUILT defaultScene={DefaultScenePath} old2DScene={Old2DScenePath} removedOld2DRootsFromDefault={removedOld2DRootsFromDefault} removedDefaultRootsFromOld2D={removedDefaultRootsFromOld2D}");
    }

    [MenuItem("WarlineCapture/Scenes/Validate Game Scene Isolation")]
    public static void ValidateScenes()
    {
        Scene defaultScene = EditorSceneManager.OpenScene(DefaultScenePath, OpenSceneMode.Single);
        foreach (string rootName in DefaultGameRootNames)
            RequireRoot(defaultScene, rootName);
        foreach (string rootName in Old2DRootsRemovedFromDefault)
            RequireNoRoot(defaultScene, rootName);
        ValidateSubSceneAssetReference(defaultScene, DefaultSubScenePath);

        GameBootstrap defaultBootstrap = FindSceneComponent<GameBootstrap>(defaultScene);
        if (defaultBootstrap == null)
            throw new InvalidOperationException("Game.unity must retain GameBootstrap.");
        if (defaultBootstrap.Chapter01TacticalBinder != null)
            throw new InvalidOperationException("Game.unity GameBootstrap must not reference the Chapter 1 tactical production binder.");
        if (defaultBootstrap.WorldCamera == null || defaultBootstrap.WorldCamera.name != "Main Camera")
            throw new InvalidOperationException("Game.unity GameBootstrap must use Main Camera.");
        if (defaultBootstrap.GlobalVolume == null || defaultBootstrap.GlobalVolume.name != "Global Volume")
            throw new InvalidOperationException("Game.unity GameBootstrap must use Global Volume.");
        if (defaultBootstrap.DirectionalLight == null || defaultBootstrap.DirectionalLight.name != "Directional Light (1)")
            throw new InvalidOperationException("Game.unity GameBootstrap must use Directional Light (1).");
        if (defaultBootstrap.DecorationRoot == null || defaultBootstrap.DecorationRoot.name != "Decorations")
            throw new InvalidOperationException("Game.unity GameBootstrap must use the default Decorations root.");
        if (FindSceneComponent<MenuView>(defaultScene) == null)
            throw new InvalidOperationException("Game.unity must retain the default UI_Canvas MenuView.");

        Scene old2DScene = EditorSceneManager.OpenScene(Old2DScenePath, OpenSceneMode.Single);
        foreach (string rootName in DefaultGameRootsRemovedFromOld2D)
            RequireNoRoot(old2DScene, rootName);

        RequireRoot(old2DScene, "Bootstrap");
        RequireRoot(old2DScene, "GameSubScene");
        ValidateSubSceneAssetReference(old2DScene, Old2DSubScenePath);
        RequireRoot(old2DScene, "Main Camera");
        RequireRoot(old2DScene, "WarlineCaptureUIBootstrap");
        RequireRoot(old2DScene, "Chapter01_TacticalMissionRuntime");
        RequireRoot(old2DScene, ProductionDecorationRootName);
        if (CountDirectionalLightRoots(old2DScene) > 1)
            throw new InvalidOperationException("Game2D.unity must not contain duplicate directional-light roots.");

        GameBootstrap bootstrap = FindSceneComponent<GameBootstrap>(old2DScene);
        if (bootstrap == null)
            throw new InvalidOperationException("Game2D.unity must retain GameBootstrap.");
        if (bootstrap.DecorationRoot == null || bootstrap.DecorationRoot.name != ProductionDecorationRootName)
            throw new InvalidOperationException($"Game2D.unity GameBootstrap must use {ProductionDecorationRootName} for runtime decorations.");
        if (bootstrap.DecorationCombinedMeshBaker != null)
            throw new InvalidOperationException("Game2D.unity GameBootstrap must not reference the removed default Decorations mesh baker.");
        if (bootstrap.GlobalVolume != null)
            throw new InvalidOperationException("Game2D.unity GameBootstrap must not reference the removed old prototype Global Volume.");
        if (bootstrap.Chapter01TacticalBinder == null)
            throw new InvalidOperationException("Game2D.unity must keep the old 2D Chapter 1 tactical binder.");

        Debug.Log($"WARLINECAPTURE_GAME_SCENE_ISOLATION_VALIDATED defaultScene={DefaultScenePath} old2DScene={Old2DScenePath}");
    }

    [MenuItem("WarlineCapture/Scenes/Validate Game SubScene Isolation")]
    public static void ValidateSubSceneIsolation()
    {
        Scene old2DScene = EditorSceneManager.OpenScene(Old2DScenePath, OpenSceneMode.Single);
        ValidateSubSceneAssetReference(old2DScene, Old2DSubScenePath);

        Scene defaultScene = EditorSceneManager.OpenScene(DefaultScenePath, OpenSceneMode.Single);
        ValidateSubSceneAssetReference(defaultScene, DefaultSubScenePath);

        Debug.Log($"WARLINECAPTURE_GAME_SUBSCENE_ISOLATION_VALIDATED old2DSubScene={Old2DSubScenePath} defaultSubScene={DefaultSubScenePath}");
    }

    private static void EnsureDefaultSubSceneAsset()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(DefaultSubScenePath) != null)
            return;

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Old2DSubScenePath) == null)
            throw new InvalidOperationException($"Old 2D SubScene asset is missing at {Old2DSubScenePath}.");

        if (!AssetDatabase.IsValidFolder(DefaultSubSceneFolder))
        {
            string guid = AssetDatabase.CreateFolder("Assets/Game/Scenes", "Game");
            if (string.IsNullOrEmpty(guid))
                throw new InvalidOperationException($"Failed to create default SubScene folder at {DefaultSubSceneFolder}.");
        }

        if (!AssetDatabase.CopyAsset(Old2DSubScenePath, DefaultSubScenePath))
            throw new InvalidOperationException($"Failed to create default SubScene asset at {DefaultSubScenePath}.");

        AssetDatabase.ImportAsset(DefaultSubScenePath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
    }

    private static void AssignSubSceneAsset(Scene scene, string expectedSubScenePath)
    {
        SubScene subScene = RequireComponent<SubScene>(scene, "GameSubScene");
        SceneAsset expectedAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(expectedSubScenePath);
        if (expectedAsset == null)
            throw new InvalidOperationException($"Expected SubScene asset is missing at {expectedSubScenePath}.");

        if (subScene.SceneAsset == expectedAsset)
            return;

        subScene.SceneAsset = expectedAsset;
        EditorUtility.SetDirty(subScene);
    }

    private static void ValidateSubSceneAssetReference(Scene scene, string expectedSubScenePath)
    {
        SubScene subScene = RequireComponent<SubScene>(scene, "GameSubScene");
        string actualPath = subScene.SceneAsset != null ? AssetDatabase.GetAssetPath(subScene.SceneAsset) : string.Empty;
        if (!string.Equals(actualPath, expectedSubScenePath, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"{scene.path} GameSubScene must reference '{expectedSubScenePath}' instead of '{actualPath}'. " +
                "Unity.Entities does not support multiple active SubScene components referencing the same scene asset.");
        }
    }

    private static int RemoveRoots(Scene scene, string[] rootNames)
    {
        int removed = 0;
        foreach (GameObject root in scene.GetRootGameObjects().ToArray())
        {
            if (!rootNames.Contains(root.name, StringComparer.Ordinal))
                continue;

            UnityEngine.Object.DestroyImmediate(root);
            removed++;
        }

        return removed;
    }

    private static GameObject EnsureProductionDecorationRoot(Scene scene)
    {
        GameObject existing = FindRoot(scene, ProductionDecorationRootName);
        if (existing != null)
            return existing;

        GameObject root = new(ProductionDecorationRootName);
        SceneManager.MoveGameObjectToScene(root, scene);
        return root;
    }

    private static void ConfigureOld2DBootstrap(Scene scene, Transform productionDecorationRoot)
    {
        GameBootstrap bootstrap = FindSceneComponent<GameBootstrap>(scene);
        if (bootstrap == null)
            throw new InvalidOperationException("Game2D.unity is missing GameBootstrap.");

        SerializedObject serializedBootstrap = new(bootstrap);
        SetObjectReference(serializedBootstrap, "menuView", null);
        SetObjectReference(serializedBootstrap, "globalVolume", null);
        SetObjectReference(serializedBootstrap, "decorationCombinedMeshBaker", null);
        SetObjectReference(serializedBootstrap, "decorationRoot", productionDecorationRoot);

        SerializedProperty legacyVisualRoots = serializedBootstrap.FindProperty("legacyVisualRootsDisabledForM01");
        if (legacyVisualRoots != null)
            legacyVisualRoots.arraySize = 0;

        serializedBootstrap.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureDefaultGameBootstrap(Scene scene)
    {
        GameBootstrap bootstrap = FindSceneComponent<GameBootstrap>(scene);
        if (bootstrap == null)
            throw new InvalidOperationException("Game.unity is missing GameBootstrap.");

        MenuView menuView = FindSceneComponent<MenuView>(scene);
        Camera worldCamera = RequireComponent<Camera>(scene, "Main Camera");
        Volume globalVolume = RequireComponent<Volume>(scene, "Global Volume");
        Light directionalLight = RequireComponent<Light>(scene, "Directional Light (1)");
        GameObject uiCanvas = RequireRoot(scene, "UI_Canvas");
        GameObject decorations = RequireRoot(scene, "Decorations");
        CombinedMeshBaker meshBaker = decorations.GetComponent<CombinedMeshBaker>();

        uiCanvas.SetActive(true);

        SerializedObject serializedBootstrap = new(bootstrap);
        SetObjectReference(serializedBootstrap, "menuView", menuView);
        SetObjectReference(serializedBootstrap, "worldCamera", worldCamera);
        SetObjectReference(serializedBootstrap, "directionalLight", directionalLight);
        SetObjectReference(serializedBootstrap, "globalVolume", globalVolume);
        SetObjectReference(serializedBootstrap, "decorationCombinedMeshBaker", meshBaker);
        SetObjectReference(serializedBootstrap, "decorationRoot", decorations.transform);
        SetObjectReference(serializedBootstrap, "chapter01TacticalBinder", null);

        SerializedProperty legacyVisualRoots = serializedBootstrap.FindProperty("legacyVisualRootsDisabledForM01");
        if (legacyVisualRoots != null)
            legacyVisualRoots.arraySize = 0;

        serializedBootstrap.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetObjectReference(SerializedObject serializedObject, string propertyName, UnityEngine.Object value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
            property.objectReferenceValue = value;
    }

    private static GameObject RequireRoot(Scene scene, string rootName)
    {
        GameObject root = FindRoot(scene, rootName);
        if (root == null)
            throw new InvalidOperationException($"{scene.path} must contain root '{rootName}'.");

        return root;
    }

    private static void RequireNoRoot(Scene scene, string rootName)
    {
        if (FindRoot(scene, rootName) != null)
            throw new InvalidOperationException($"{scene.path} must not contain migrated-away root '{rootName}'.");
    }

    private static T RequireComponent<T>(Scene scene, string rootName) where T : Component
    {
        GameObject root = RequireRoot(scene, rootName);
        T component = root.GetComponent<T>();
        if (component == null)
            throw new InvalidOperationException($"{scene.path} root '{rootName}' must contain {typeof(T).Name}.");

        return component;
    }

    private static int CountDirectionalLightRoots(Scene scene)
    {
        int count = 0;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Light light = root.GetComponent<Light>();
            if (light != null && light.type == LightType.Directional)
                count++;
        }

        return count;
    }

    private static GameObject FindRoot(Scene scene, string rootName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == rootName)
                return root;
        }

        return null;
    }

    private static T FindSceneComponent<T>(Scene scene) where T : Component
    {
        foreach (T component in Resources.FindObjectsOfTypeAll<T>())
        {
            if (component != null && component.gameObject.scene == scene)
                return component;
        }

        return null;
    }
}
