using System;
using Unity.Entities;
using UnityEngine;

internal sealed class GameplayFeatureStartupCompositionSystemHelper
{
    public readonly struct Result
    {
        public readonly RuntimeCityCompositionSystem RuntimeCity;
        public readonly RuntimeGridBlockerPresentationSystemHelper RuntimeGridBlockers;
        public readonly RuntimeDecorationSpawnerPresentationSystemHelper RuntimeDecorations;

        public Result(
            RuntimeCityCompositionSystem runtimeCity,
            RuntimeGridBlockerPresentationSystemHelper runtimeGridBlockers,
            RuntimeDecorationSpawnerPresentationSystemHelper runtimeDecorations)
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
        RoadRuntimeGenerationSystem roadRuntimeGenerationSystem,
        RoadRuntimeGenerationSystem.Context roadRuntimeGenerationContext,
        Action<IMatchRuntimeUi, RuntimeGridBlockerPresentationSystemHelper> bindRoadGameplayFeatures,
        BuildingRuntimeCitySpawnSystem buildingRuntimeCitySpawn,
        BuildingRuntimeCitySpawnSystem.Context buildingRuntimeCitySpawnContext,
        BuildingPlacementInteractionSystem buildingPlacementInteraction,
        BuildingPlacementInteractionSystem.Context buildingPlacementInteractionContext,
        Action<IMatchRuntimeUi, SelectionUiCameraSystem, SelectionBuildingInteractionSystem, RuntimeGridBlockerPresentationSystemHelper, RuntimeCityCompositionSystem, CitizenPopulationEventSystem> bindBuildingGameplayFeatures,
        IMatchRuntimeUi mainMenu,
        SelectionUiCameraSystem selectionUiCameraSystem,
        SelectionBuildingInteractionSystem selectionBuildingInteractionSystem,
        CitizenPopulationEventSystem citizenPopulationEventSystem,
        Transform runtimeCityRoot,
        Transform runtimeBlockerRoot,
        Transform decorationRoot,
        CombinedMeshBaker decorationCombinedMeshBaker,
        GameplaySceneBindingSceneSystemHelper sceneBindingSystem)
    {
        RuntimeCityCompositionSystem runtimeCity = ResolveRuntimeCityCompositionSystem();
        runtimeCity?.Configure(
            runtimeCitySpawnerConfig,
            roadRuntimeGenerationSystem,
            roadRuntimeGenerationContext,
            buildingRuntimeCitySpawn,
            buildingRuntimeCitySpawnContext,
            runtimeCityRoot,
            mainMenu);

        RuntimeCityReadModelCompositionSystemHelper runtimeCityReadModel = runtimeCity?.ReadModel;
        RuntimeGridBlockerPresentationSystemHelper runtimeGridBlockers = ResolveRuntimeGridBlockerPresentationHelper();
        runtimeGridBlockers?.Init(runtimeGridBlockerConfig, runtimeBlockerRoot, runtimeCityReadModel);
        bindRoadGameplayFeatures?.Invoke(mainMenu, runtimeGridBlockers);
        sceneBindingSystem?.BindRuntimeGridBlockerDebugViews(runtimeGridBlockers);
        bindBuildingGameplayFeatures?.Invoke(
            mainMenu,
            selectionUiCameraSystem,
            selectionBuildingInteractionSystem,
            runtimeGridBlockers,
            runtimeCity,
            citizenPopulationEventSystem);

        RuntimeDecorationSpawnerPresentationSystemHelper runtimeDecorations = ResolveRuntimeDecorationSpawnerPresentationHelper();
        runtimeDecorations?.Init(
            runtimeDecorationSpawnerConfig,
            decorationRoot,
            decorationCombinedMeshBaker,
            runtimeCityReadModel,
            runtimeGridBlockers);

        return new Result(runtimeCity, runtimeGridBlockers, runtimeDecorations);
    }

    private static RuntimeGridBlockerPresentationSystemHelper ResolveRuntimeGridBlockerPresentationHelper()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        return world != null && world.IsCreated
            ? new RuntimeGridBlockerPresentationSystemHelper()
            : null;
    }

    private static RuntimeCityCompositionSystem ResolveRuntimeCityCompositionSystem()
    {
        return new RuntimeCityCompositionSystem();
    }

    private static RuntimeDecorationSpawnerPresentationSystemHelper ResolveRuntimeDecorationSpawnerPresentationHelper()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        return world != null && world.IsCreated
            ? new RuntimeDecorationSpawnerPresentationSystemHelper()
            : null;
    }
}
