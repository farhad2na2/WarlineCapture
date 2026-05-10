using System;
using System.Linq;
using Game.Scripts.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using Unity.Scenes;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class WarlineCaptureGameLegecySceneIsolationBuilder
{
    private const string GameScenePath = "Assets/Game/Scenes/Game.unity";
    private const string LegacyScenePath = "Assets/Game/Scenes/Game_Legecy.unity";
    private const string ProductionSubScenePath = "Assets/Game/Scenes/Game/GameSubScene.unity";
    private const string LegacySubSceneFolder = "Assets/Game/Scenes/Game_Legecy";
    private const string LegacySubScenePath = "Assets/Game/Scenes/Game_Legecy/GameSubScene.unity";
    private const string ProductionDecorationRootName = "RuntimeDecorations_Production";

    private static readonly string[] LegacyRootNames =
    {
        "Main Camera_Experiment",
        "Global Volume_Experiment",
        "SM_Skydome_01",
        "Ground",
        "Decorations",
        "UI_Canvas",
        "Directional Light (1)"
    };

    private static readonly string[] ProductionRootsRemovedFromLegacy =
    {
        "WarlineCaptureUIBootstrap",
        "Chapter01_TacticalMissionRuntime",
        "RuntimeDecorations_Production",
        "Main Camera",
        "Global Volume",
        "Directional Light"
    };

    private static readonly string[] PrototypeRootsRemovedFromProduction =
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

    [MenuItem("WarlineCapture/Scenes/Build Game Legecy Scene Isolation")]
    public static void Build()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(LegacyScenePath) == null)
        {
            if (!AssetDatabase.CopyAsset(GameScenePath, LegacyScenePath))
                throw new InvalidOperationException($"Failed to create legacy scene copy at {LegacyScenePath}.");

            AssetDatabase.ImportAsset(LegacyScenePath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        }

        Scene legacyScene = EditorSceneManager.OpenScene(LegacyScenePath, OpenSceneMode.Single);
        EnsureLegacySubSceneAsset();
        AssignSubSceneAsset(legacyScene, LegacySubScenePath);
        int removedLegacyProductionRoots = RemoveRoots(legacyScene, ProductionRootsRemovedFromLegacy);
        ConfigureLegacyBootstrap(legacyScene);

        EditorSceneManager.MarkSceneDirty(legacyScene);
        if (!EditorSceneManager.SaveScene(legacyScene))
            throw new InvalidOperationException($"Failed to save isolated legacy scene at {LegacyScenePath}.");

        Scene productionScene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
        int removedProductionPrototypeRoots = RemoveRoots(productionScene, PrototypeRootsRemovedFromProduction);
        GameObject productionDecorationRoot = EnsureProductionDecorationRoot(productionScene);
        ConfigureProductionBootstrap(productionScene, productionDecorationRoot.transform);

        EditorSceneManager.MarkSceneDirty(productionScene);
        if (!EditorSceneManager.SaveScene(productionScene))
            throw new InvalidOperationException($"Failed to save cleaned production scene at {GameScenePath}.");

        ValidateScenes();
        Debug.Log($"WARLINECAPTURE_GAME_LEGECY_SCENE_ISOLATION_BUILT legacyScene={LegacyScenePath} productionScene={GameScenePath} removedLegacyProductionRoots={removedLegacyProductionRoots} removedProductionPrototypeRoots={removedProductionPrototypeRoots}");
    }

    [MenuItem("WarlineCapture/Scenes/Validate Game Legecy Scene Isolation")]
    public static void ValidateScenes()
    {
        Scene legacyScene = EditorSceneManager.OpenScene(LegacyScenePath, OpenSceneMode.Single);
        foreach (string rootName in LegacyRootNames)
            RequireRoot(legacyScene, rootName);
        foreach (string rootName in ProductionRootsRemovedFromLegacy)
            RequireNoRoot(legacyScene, rootName);
        ValidateSubSceneAssetReference(legacyScene, LegacySubScenePath);

        GameBootstrap legacyBootstrap = FindSceneComponent<GameBootstrap>(legacyScene);
        if (legacyBootstrap == null)
            throw new InvalidOperationException("Game_Legecy.unity must retain GameBootstrap.");
        if (legacyBootstrap.Chapter01TacticalBinder != null)
            throw new InvalidOperationException("Game_Legecy.unity GameBootstrap must not reference the Chapter 1 tactical production binder.");
        if (legacyBootstrap.WorldCamera == null || legacyBootstrap.WorldCamera.name != "Main Camera_Experiment")
            throw new InvalidOperationException("Game_Legecy.unity GameBootstrap must use Main Camera_Experiment.");
        if (legacyBootstrap.GlobalVolume == null || legacyBootstrap.GlobalVolume.name != "Global Volume_Experiment")
            throw new InvalidOperationException("Game_Legecy.unity GameBootstrap must use Global Volume_Experiment.");
        if (legacyBootstrap.DirectionalLight == null || legacyBootstrap.DirectionalLight.name != "Directional Light (1)")
            throw new InvalidOperationException("Game_Legecy.unity GameBootstrap must use Directional Light (1).");
        if (legacyBootstrap.DecorationRoot == null || legacyBootstrap.DecorationRoot.name != "Decorations")
            throw new InvalidOperationException("Game_Legecy.unity GameBootstrap must use the legacy Decorations root.");
        if (FindSceneComponent<MenuView>(legacyScene) == null)
            throw new InvalidOperationException("Game_Legecy.unity must retain the legacy UI_Canvas MenuView.");

        Scene productionScene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
        foreach (string rootName in PrototypeRootsRemovedFromProduction)
            RequireNoRoot(productionScene, rootName);

        RequireRoot(productionScene, "Bootstrap");
        RequireRoot(productionScene, "GameSubScene");
        ValidateSubSceneAssetReference(productionScene, ProductionSubScenePath);
        RequireRoot(productionScene, "Main Camera");
        RequireRoot(productionScene, "WarlineCaptureUIBootstrap");
        RequireRoot(productionScene, "Chapter01_TacticalMissionRuntime");
        RequireRoot(productionScene, ProductionDecorationRootName);
        if (CountDirectionalLightRoots(productionScene) > 1)
            throw new InvalidOperationException("Cleaned Game.unity must not contain duplicate directional-light roots.");

        GameBootstrap bootstrap = FindSceneComponent<GameBootstrap>(productionScene);
        if (bootstrap == null)
            throw new InvalidOperationException("Cleaned Game.unity must retain GameBootstrap.");
        if (bootstrap.DecorationRoot == null || bootstrap.DecorationRoot.name != ProductionDecorationRootName)
            throw new InvalidOperationException($"Cleaned Game.unity GameBootstrap must use {ProductionDecorationRootName} for runtime decorations.");
        if (bootstrap.DecorationCombinedMeshBaker != null)
            throw new InvalidOperationException("Cleaned Game.unity GameBootstrap must not reference the removed legacy Decorations mesh baker.");
        if (bootstrap.GlobalVolume != null)
            throw new InvalidOperationException("Cleaned Game.unity GameBootstrap must not reference the removed old prototype Global Volume.");
        if (bootstrap.Chapter01TacticalBinder == null)
            throw new InvalidOperationException("Cleaned Game.unity must keep the production Chapter 1 tactical binder.");

        Debug.Log($"WARLINECAPTURE_GAME_LEGECY_SCENE_ISOLATION_VALIDATED legacyScene={LegacyScenePath} productionScene={GameScenePath}");
    }

    [MenuItem("WarlineCapture/Scenes/Validate Game SubScene Isolation")]
    public static void ValidateSubSceneIsolation()
    {
        Scene productionScene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
        ValidateSubSceneAssetReference(productionScene, ProductionSubScenePath);

        Scene legacyScene = EditorSceneManager.OpenScene(LegacyScenePath, OpenSceneMode.Single);
        ValidateSubSceneAssetReference(legacyScene, LegacySubScenePath);

        Debug.Log($"WARLINECAPTURE_GAME_SUBSCENE_ISOLATION_VALIDATED productionSubScene={ProductionSubScenePath} legacySubScene={LegacySubScenePath}");
    }

    private static void EnsureLegacySubSceneAsset()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(LegacySubScenePath) != null)
            return;

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ProductionSubScenePath) == null)
            throw new InvalidOperationException($"Production SubScene asset is missing at {ProductionSubScenePath}.");

        if (!AssetDatabase.IsValidFolder(LegacySubSceneFolder))
        {
            string guid = AssetDatabase.CreateFolder("Assets/Game/Scenes", "Game_Legecy");
            if (string.IsNullOrEmpty(guid))
                throw new InvalidOperationException($"Failed to create legacy SubScene folder at {LegacySubSceneFolder}.");
        }

        if (!AssetDatabase.CopyAsset(ProductionSubScenePath, LegacySubScenePath))
            throw new InvalidOperationException($"Failed to create legacy SubScene asset at {LegacySubScenePath}.");

        AssetDatabase.ImportAsset(LegacySubScenePath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
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

    private static void ConfigureProductionBootstrap(Scene scene, Transform productionDecorationRoot)
    {
        GameBootstrap bootstrap = FindSceneComponent<GameBootstrap>(scene);
        if (bootstrap == null)
            throw new InvalidOperationException("Game.unity is missing GameBootstrap.");

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

    private static void ConfigureLegacyBootstrap(Scene scene)
    {
        GameBootstrap bootstrap = FindSceneComponent<GameBootstrap>(scene);
        if (bootstrap == null)
            throw new InvalidOperationException("Game_Legecy.unity is missing GameBootstrap.");

        MenuView menuView = FindSceneComponent<MenuView>(scene);
        Camera worldCamera = RequireComponent<Camera>(scene, "Main Camera_Experiment");
        Volume globalVolume = RequireComponent<Volume>(scene, "Global Volume_Experiment");
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
            throw new InvalidOperationException($"{scene.path} must not contain legacy root '{rootName}'.");
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
