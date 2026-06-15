#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class RuntimeCitySpawnerStep13Validation
{
    private const string MatchScenePath = "Assets/Game/Scenes/Match.unity";

    public static void RunGameSceneSmokeValidation()
    {
        try
        {
            EditorSceneManager.OpenScene(MatchScenePath, OpenSceneMode.Single);

            MatchSceneView matchScene = FindSingleMatchSceneView();
            RuntimeCitySpawnerSystemConfig validationCityConfig = CreateValidationRuntimeCityConfig(matchScene.RuntimeCitySpawnerConfig);
            ValidateRuntimeCityConfigReferences(matchScene.RuntimeCitySpawnerConfig);
            ValidateRuntimeCityGenerationConfig(validationCityConfig);
            ValidateRoadConfig(matchScene.RoadBuildConfig);
            ValidateBlockerConfig(matchScene.RuntimeGridBlockerConfig);
            ValidateCityPrefabsAreSpawnable(
                validationCityConfig,
                matchScene.BuildingPlacementConfig);
            ValidateNoMissingScripts();

            Debug.Log(
                "[RuntimeCityGameSceneSmokeValidation] result=Passed " +
                $"cityPrefabs={CountCityPrefabs(validationCityConfig)} " +
                $"productionCityCount={matchScene.RuntimeCitySpawnerConfig.CityCount} " +
                $"validationCityCount={validationCityConfig.CityCount} " +
                $"buildingSpawnables={matchScene.BuildingPlacementConfig.Spawnables.Count} " +
                $"blockerPrefabs={matchScene.RuntimeGridBlockerConfig.Prefabs.Count}");
            EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError("[RuntimeCityGameSceneSmokeValidation] result=Failed");
            EditorApplication.Exit(1);
        }
    }

    public static void RunGameSceneCityDisabledValidation()
    {
        try
        {
            EditorSceneManager.OpenScene(MatchScenePath, OpenSceneMode.Single);

            MatchSceneView matchScene = FindSingleMatchSceneView();
            RuntimeCitySpawnerSystemConfig disabledConfig = CreateValidationRuntimeCityConfig(matchScene.RuntimeCitySpawnerConfig);
            var serialized = new SerializedObject(disabledConfig);
            serialized.FindProperty("cityCount").intValue = 0;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            ValidateRuntimeCityConfigReferences(matchScene.RuntimeCitySpawnerConfig);
            AssertCondition(matchScene.RuntimeCitySpawnerConfig.CityCount >= 0, "Production runtime city config must expose a valid city count.");
            AssertCondition(disabledConfig.CityCount == 0, "Disabled runtime city validation config must force cityCount to 0.");
            AssertCondition(disabledConfig.SpawnOnStart, "Disabled runtime city validation keeps spawn-on-start enabled to validate the no-city startup path.");
            AssertCondition(disabledConfig.GenerateBuildings, "Disabled runtime city validation keeps building generation enabled to prove cityCount gates generation.");
            ValidateRoadConfig(matchScene.RoadBuildConfig);
            ValidateBlockerConfig(matchScene.RuntimeGridBlockerConfig);
            ValidateNoMissingScripts();

            World previousWorld = World.DefaultGameObjectInjectionWorld;
            World validationWorld = null;
            World world = previousWorld;
            if (world == null || !world.IsCreated)
            {
                validationWorld = new World("RuntimeCityDisabledValidationWorld");
                World.DefaultGameObjectInjectionWorld = validationWorld;
                world = validationWorld;
            }

            RuntimeCityCompositionSystem runtimeCity = world.GetOrCreateSystemManaged<RuntimeCityCompositionSystem>();
            try
            {
                runtimeCity.ConfigureForValidation(disabledConfig);
                runtimeCity.Update(1);

                AssertCondition(runtimeCity.SpawnOnStartEnabled, "Runtime city disabled validation must preserve spawn-on-start state.");
                AssertCondition(runtimeCity.HasSpawned, "Runtime city composition must immediately report spawned/complete when cityCount is 0.");
                AssertCondition(!runtimeCity.IsGenerating, "Runtime city composition must not generate when cityCount is 0.");
                AssertCondition(runtimeCity.ReadModel.HasSpawned, "Runtime city read model must publish completed state when cityCount is 0.");
                AssertCondition(!runtimeCity.ReadModel.IsGenerating, "Runtime city read model must publish non-generating state when cityCount is 0.");
                runtimeCity.Dispose();
            }
            finally
            {
                if (validationWorld != null)
                {
                    World.DefaultGameObjectInjectionWorld = previousWorld;
                    validationWorld.Dispose();
                }
            }

            Debug.Log(
                "[RuntimeCityDisabledValidation] result=Passed " +
                $"productionCityCount={matchScene.RuntimeCitySpawnerConfig.CityCount} " +
                $"validationCityCount={disabledConfig.CityCount}");
            EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError("[RuntimeCityDisabledValidation] result=Failed");
            EditorApplication.Exit(1);
        }
    }

    private static MatchSceneView FindSingleMatchSceneView()
    {
        MatchSceneView[] matchScenes = UnityEngine.Object.FindObjectsByType<MatchSceneView>(
            FindObjectsInactive.Include);
        AssertCondition(matchScenes.Length == 1, $"Expected exactly one MatchSceneView in {MatchScenePath}, found {matchScenes.Length}.");
        return matchScenes[0];
    }

    private static RuntimeCitySpawnerSystemConfig CreateValidationRuntimeCityConfig(RuntimeCitySpawnerSystemConfig source)
    {
        AssertCondition(source != null, "MatchSceneView must reference RuntimeCitySpawnerSystemConfig.");

        RuntimeCitySpawnerSystemConfig validationConfig = UnityEngine.Object.Instantiate(source);
        validationConfig.name = source.name + "_ValidationRuntimeCityEnabled";

        var serialized = new SerializedObject(validationConfig);
        serialized.FindProperty("spawnOnStart").boolValue = true;
        serialized.FindProperty("generateBuildings").boolValue = true;
        serialized.FindProperty("cityCount").intValue = Mathf.Max(1, source.CityCount);
        serialized.ApplyModifiedPropertiesWithoutUndo();

        return validationConfig;
    }

    private static void ValidateRuntimeCityConfigReferences(RuntimeCitySpawnerSystemConfig config)
    {
        AssertCondition(config != null, "MatchSceneView must reference RuntimeCitySpawnerSystemConfig.");
        AssertCondition(config.HallPrefabs.Count > 0, "Runtime city config needs hall prefabs.");
        AssertNoNullPrefabs(config.HallPrefabs, "hall");
        AssertCondition(config.ShopPrefabs.Count > 0, "Runtime city config needs shop prefabs.");
        AssertNoNullPrefabs(config.ShopPrefabs, "shop");
        AssertCondition(config.HousePrefabs.Count > 0, "Runtime city config needs house prefabs.");
        AssertNoNullPrefabs(config.HousePrefabs, "house");
        AssertNoNullPrefabs(config.GasStationPrefabs, "gas station");
        AssertNoNullPrefabs(config.OtherBuildingPrefabs, "other building");
        AssertNoNullPrefabs(config.CityDecorationPrefabs, "city decoration");
        AssertNoNullPrefabs(config.HouseWallPrefabs, "house wall");
    }

    private static void ValidateRuntimeCityGenerationConfig(RuntimeCitySpawnerSystemConfig config)
    {
        ValidateRuntimeCityConfigReferences(config);
        AssertCondition(config.SpawnOnStart, "Runtime city validation config must enable spawn-on-start.");
        AssertCondition(config.GenerateBuildings, "Runtime city validation config must enable building generation.");
        AssertCondition(config.CityCount > 0, "Runtime city validation config must generate at least one city.");
    }

    private static void ValidateRoadConfig(RoadBuildSystemConfig config)
    {
        AssertCondition(config != null, "MatchSceneView must reference RoadBuildSystemConfig.");
        AssertCondition(config.RoadGridSize > 0f, "Road grid size must be positive.");
        AssertCondition(config.StraightPrefab != null, "RoadBuildSystemConfig needs a straight road prefab.");
        AssertCondition(config.TIntersectionPrefab != null, "RoadBuildSystemConfig needs a T intersection road prefab.");
        AssertCondition(config.IntersectionPrefab != null, "RoadBuildSystemConfig needs a cross intersection road prefab.");
        AssertCondition(config.EndPrefab != null, "RoadBuildSystemConfig needs an end road prefab.");
        AssertCondition(config.CornerPrefab != null, "RoadBuildSystemConfig needs a corner road prefab.");
        AssertCondition(config.AutobahnPrefab != null, "RoadBuildSystemConfig needs an autobahn prefab.");
        AssertCondition(config.AutobahnConnectPrefab != null, "RoadBuildSystemConfig needs an autobahn connector prefab.");
    }

    private static void ValidateBlockerConfig(RuntimeGridBlockerSystemConfig config)
    {
        AssertCondition(config != null, "MatchSceneView must reference RuntimeGridBlockerSystemConfig.");
        AssertCondition(config.SpawnOnStart, "Runtime grid blocker spawning should stay enabled for the Match scene.");
        AssertCondition(config.BlockerCount > 0, "Runtime blocker config must spawn blockers.");
        AssertCondition(config.Prefabs.Count > 0, "Runtime blocker config needs blocker prefabs.");
        AssertNoNullPrefabs(config.Prefabs, "runtime blocker");
    }

    private static void ValidateCityPrefabsAreSpawnable(
        RuntimeCitySpawnerSystemConfig cityConfig,
        BuildingPlacementSystemConfig buildingConfig)
    {
        AssertCondition(buildingConfig != null, "MatchSceneView must reference BuildingPlacementSystemConfig.");
        AssertCondition(buildingConfig.Spawnables.Count > 0, "BuildingPlacementSystemConfig needs spawnables for runtime city buildings.");

        var spawnablePrefabs = new HashSet<GameObject>(buildingConfig.Spawnables);
        AssertCityPrefabsAreSpawnable(cityConfig.HallPrefabs, spawnablePrefabs, "hall");
        AssertCityPrefabsAreSpawnable(cityConfig.GasStationPrefabs, spawnablePrefabs, "gas station");
        AssertCityPrefabsAreSpawnable(cityConfig.ShopPrefabs, spawnablePrefabs, "shop");
        AssertCityPrefabsAreSpawnable(cityConfig.HousePrefabs, spawnablePrefabs, "house");
        AssertCityPrefabsAreSpawnable(cityConfig.OtherBuildingPrefabs, spawnablePrefabs, "other building");
    }

    private static void AssertCityPrefabsAreSpawnable(
        IReadOnlyList<GameObject> prefabs,
        HashSet<GameObject> spawnablePrefabs,
        string category)
    {
        for (int i = 0; i < prefabs.Count; i++)
        {
            GameObject prefab = prefabs[i];
            if (prefab == null)
                continue;

            AssertCondition(
                spawnablePrefabs.Contains(prefab),
                $"Runtime city {category} prefab is not in BuildingPlacementSystemConfig spawnables: {AssetDatabase.GetAssetPath(prefab)}");
        }
    }

    private static void ValidateNoMissingScripts()
    {
        GameObject[] roots = EditorSceneManager.GetActiveScene().GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            int missing = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(roots[i]);
            AssertCondition(missing == 0, $"Scene root {roots[i].name} has {missing} missing script reference(s).");
        }
    }

    private static void AssertNoNullPrefabs(IReadOnlyList<GameObject> prefabs, string category)
    {
        for (int i = 0; i < prefabs.Count; i++)
            AssertCondition(prefabs[i] != null, $"Runtime city {category} prefab slot {i} is null.");
    }

    private static int CountCityPrefabs(RuntimeCitySpawnerSystemConfig config)
    {
        return config.HallPrefabs.Count +
            config.GasStationPrefabs.Count +
            config.ShopPrefabs.Count +
            config.HousePrefabs.Count +
            config.OtherBuildingPrefabs.Count +
            config.CityDecorationPrefabs.Count;
    }

    private static void AssertCondition(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
#endif
