using System;
using UnityEngine;

internal sealed class GameplayFeatureStartupSystem
{
    public readonly struct Result
    {
        public readonly RuntimeCityCompositionSystem RuntimeCity;
        public readonly RuntimeGridBlockerSystem RuntimeGridBlockers;
        public readonly RuntimeDecorationSpawnerSystem RuntimeDecorations;

        public Result(
            RuntimeCityCompositionSystem runtimeCity,
            RuntimeGridBlockerSystem runtimeGridBlockers,
            RuntimeDecorationSpawnerSystem runtimeDecorations)
        {
            RuntimeCity = runtimeCity;
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
        Action<MainMenuPlayUI, SelectionUiCameraSystem, SelectionBuildingInteractionSystem, RuntimeGridBlockerSystem, RuntimeCityCompositionSystem, CitizenPopulationSystem> bindBuildingGameplayFeatures,
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
        var runtimeCity = new RuntimeCityCompositionSystem();
        runtimeCity.Configure(
            runtimeCitySpawnerConfig,
            roadBuild,
            buildingRuntimeCitySpawn,
            buildingRuntimeCitySpawnContext,
            runtimeCityRoot,
            mainMenu);

        var runtimeGridBlockers = new RuntimeGridBlockerSystem();
        runtimeGridBlockers.Init(runtimeGridBlockerConfig, runtimeBlockerRoot, runtimeCity.ReadModel);
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
            runtimeCity,
            citizenPopulation);

        var runtimeDecorations = new RuntimeDecorationSpawnerSystem();
        runtimeDecorations.Init(
            runtimeDecorationSpawnerConfig,
            decorationRoot,
            decorationCombinedMeshBaker,
            runtimeCity.ReadModel,
            runtimeGridBlockers);

        return new Result(runtimeCity, runtimeGridBlockers, runtimeDecorations);
    }
}
