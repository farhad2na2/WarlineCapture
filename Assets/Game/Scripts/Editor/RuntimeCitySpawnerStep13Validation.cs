#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class RuntimeCitySpawnerStep13Validation
{
    private const string GameScenePath = "Assets/Game/Scenes/Game.unity";

    public static void RunGameSceneSmokeValidation()
    {
        try
        {
            EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);

            GameBootstrap bootstrap = FindSingleBootstrap();
            ValidateRuntimeCityConfig(bootstrap.RuntimeCitySpawnerConfig);
            ValidateRoadConfig(bootstrap.RoadBuildConfig);
            ValidateBlockerConfig(bootstrap.RuntimeGridBlockerConfig);
            ValidateCityPrefabsAreSpawnable(
                bootstrap.RuntimeCitySpawnerConfig,
                bootstrap.BuildingPlacementConfig);
            ValidateNoMissingScripts();

            Debug.Log(
                "[RuntimeCityGameSceneSmokeValidation] result=Passed " +
                $"cityPrefabs={CountCityPrefabs(bootstrap.RuntimeCitySpawnerConfig)} " +
                $"buildingSpawnables={bootstrap.BuildingPlacementConfig.Spawnables.Count} " +
                $"blockerPrefabs={bootstrap.RuntimeGridBlockerConfig.Prefabs.Count}");
            EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError("[RuntimeCityGameSceneSmokeValidation] result=Failed");
            EditorApplication.Exit(1);
        }
    }

    private static GameBootstrap FindSingleBootstrap()
    {
        GameBootstrap[] bootstraps = UnityEngine.Object.FindObjectsByType<GameBootstrap>(
            FindObjectsInactive.Include);
        AssertCondition(bootstraps.Length == 1, $"Expected exactly one GameBootstrap in {GameScenePath}, found {bootstraps.Length}.");
        return bootstraps[0];
    }

    private static void ValidateRuntimeCityConfig(RuntimeCitySpawnerSystemConfig config)
    {
        AssertCondition(config != null, "GameBootstrap must reference RuntimeCitySpawnerSystemConfig.");
        AssertCondition(config.SpawnOnStart, "Runtime city spawning should stay enabled for the Game scene.");
        AssertCondition(config.GenerateBuildings, "Runtime city building generation should stay enabled for the Game scene.");
        AssertCondition(config.CityCount > 0, "Runtime city config must generate at least one city.");
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

    private static void ValidateRoadConfig(RoadBuildSystemConfig config)
    {
        AssertCondition(config != null, "GameBootstrap must reference RoadBuildSystemConfig.");
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
        AssertCondition(config != null, "GameBootstrap must reference RuntimeGridBlockerSystemConfig.");
        AssertCondition(config.SpawnOnStart, "Runtime grid blocker spawning should stay enabled for the Game scene.");
        AssertCondition(config.BlockerCount > 0, "Runtime blocker config must spawn blockers.");
        AssertCondition(config.Prefabs.Count > 0, "Runtime blocker config needs blocker prefabs.");
        AssertNoNullPrefabs(config.Prefabs, "runtime blocker");
    }

    private static void ValidateCityPrefabsAreSpawnable(
        RuntimeCitySpawnerSystemConfig cityConfig,
        BuildingPlacementSystemConfig buildingConfig)
    {
        AssertCondition(buildingConfig != null, "GameBootstrap must reference BuildingPlacementSystemConfig.");
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
