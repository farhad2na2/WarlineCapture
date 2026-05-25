using System;
using UnityEngine;

internal sealed class GameplayFeatureStartupSystem
{
    public readonly struct Result
    {
        public readonly RuntimeCitySpawnerSystem RuntimeCitySpawner;
        public readonly RuntimeGridBlockerSystem RuntimeGridBlockers;
        public readonly RuntimeDecorationSpawnerSystem RuntimeDecorations;

        public Result(
            RuntimeCitySpawnerSystem runtimeCitySpawner,
            RuntimeGridBlockerSystem runtimeGridBlockers,
            RuntimeDecorationSpawnerSystem runtimeDecorations)
        {
            RuntimeCitySpawner = runtimeCitySpawner;
            RuntimeGridBlockers = runtimeGridBlockers;
            RuntimeDecorations = runtimeDecorations;
        }
    }

    public Result Initialize(
        RuntimeCitySpawnerSystemConfig runtimeCitySpawnerConfig,
        RuntimeGridBlockerSystemConfig runtimeGridBlockerConfig,
        RuntimeDecorationSpawnerSystemConfig runtimeDecorationSpawnerConfig,
        RoadBuildSystem roadBuild,
        BuildingRuntimeCitySpawnSystem buildingRuntimeCitySpawn,
        BuildingRuntimeCitySpawnSystem.Context buildingRuntimeCitySpawnContext,
        BuildingPlacementInteractionSystem buildingPlacementInteraction,
        BuildingPlacementInteractionSystem.Context buildingPlacementInteractionContext,
        Action<MainMenuPlayUI, SelectionUiCameraSystem, SelectionBuildingInteractionSystem, RuntimeGridBlockerSystem, RuntimeCitySpawnerSystem, CitizenPopulationSystem> bindBuildingGameplayFeatures,
        MainMenuPlayUI mainMenu,
        SelectionUiCameraSystem selectionUiCameraSystem,
        SelectionBuildingInteractionSystem selectionBuildingInteractionSystem,
        CitizenPopulationSystem citizenPopulation,
        Transform runtimeCityRoot,
        Transform runtimeBlockerRoot,
        Transform decorationRoot,
        CombinedMeshBaker decorationCombinedMeshBaker,
        GameplaySceneBindingSystem sceneBindingSystem)
    {
        var runtimeCitySpawner = new RuntimeCitySpawnerSystem();
        runtimeCitySpawner.Init(
            runtimeCitySpawnerConfig,
            roadBuild,
            buildingRuntimeCitySpawn,
            buildingRuntimeCitySpawnContext,
            runtimeCityRoot,
            mainMenu);

        var runtimeGridBlockers = new RuntimeGridBlockerSystem();
        runtimeGridBlockers.Init(runtimeGridBlockerConfig, runtimeBlockerRoot, runtimeCitySpawner);
        roadBuild?.BindDependencies(
            buildingPlacementInteraction,
            buildingPlacementInteractionContext,
            mainMenu,
            runtimeGridBlockers);
        sceneBindingSystem?.BindRuntimeGridBlockerDebugViews(runtimeGridBlockers);
        bindBuildingGameplayFeatures?.Invoke(
            mainMenu,
            selectionUiCameraSystem,
            selectionBuildingInteractionSystem,
            runtimeGridBlockers,
            runtimeCitySpawner,
            citizenPopulation);

        var runtimeDecorations = new RuntimeDecorationSpawnerSystem();
        runtimeDecorations.Init(
            runtimeDecorationSpawnerConfig,
            decorationRoot,
            decorationCombinedMeshBaker,
            runtimeCitySpawner,
            runtimeGridBlockers);

        return new Result(runtimeCitySpawner, runtimeGridBlockers, runtimeDecorations);
    }
}
