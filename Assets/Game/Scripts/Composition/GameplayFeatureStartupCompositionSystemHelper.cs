using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

internal sealed class GameplayFeatureStartupCompositionSystemHelper
{
    public readonly struct Result
    {
        public readonly RuntimeCityCompositionSystemHelper RuntimeCity;
        public readonly RuntimeGridBlockerPresentationSystemHelper RuntimeGridBlockers;
        public readonly RuntimeDecorationSpawnerPresentationSystemHelper RuntimeDecorations;

        public Result(
            RuntimeCityCompositionSystemHelper runtimeCity,
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
        BuildingPlacementInteractionBoundaryCompositionSystemHelper buildingPlacementInteraction,
        BuildingPlacementInteractionBoundaryCompositionSystemHelper.Context buildingPlacementInteractionContext,
        Action<IMatchRuntimeUi, SelectionUiCameraSystem, SelectionBuildingInteractionSystem, RuntimeGridBlockerPresentationSystemHelper, RuntimeCityCompositionSystemHelper, CitizenPopulationEventSystem> bindBuildingGameplayFeatures,
        IMatchRuntimeUi mainMenu,
        SelectionUiCameraSystem selectionUiCameraSystem,
        SelectionBuildingInteractionSystem selectionBuildingInteractionSystem,
        CitizenPopulationEventSystem citizenPopulationEventSystem,
        Transform runtimeCityRoot,
        Transform runtimeBlockerRoot,
        Transform decorationRoot,
        CombinedMeshBaker decorationCombinedMeshBaker,
        IReadOnlyList<GridAuthoring> runtimeGridDebugViews,
        GameplaySceneBindingSceneSystemHelper sceneBindingSystem)
    {
        RuntimeCityCompositionSystemHelper runtimeCity = ResolveRuntimeCityCompositionSystemHelper();
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
        sceneBindingSystem?.BindRuntimeGridBlockerDebugViews(runtimeGridBlockers, runtimeGridDebugViews);
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

    private static RuntimeCityCompositionSystemHelper ResolveRuntimeCityCompositionSystemHelper()
    {
        return new RuntimeCityCompositionSystemHelper();
    }

    private static RuntimeDecorationSpawnerPresentationSystemHelper ResolveRuntimeDecorationSpawnerPresentationHelper()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        return world != null && world.IsCreated
            ? new RuntimeDecorationSpawnerPresentationSystemHelper()
            : null;
    }
}
