using System;
using UnityEngine;

internal sealed class RoadBuildCompositionSystem
{
    private RoadBuildCompositionSourceSystem _roadSource;

    public readonly struct Result
    {
        public readonly RoadBuildReadModelSystem RoadBuildReadModel;
        public readonly RoadRuntimeGenerationSystem RoadRuntimeGeneration;
        public readonly RoadRuntimeGenerationSystem.Context RoadRuntimeGenerationContext;
        public readonly RoadFootprintQuerySystem RoadFootprintQuery;
        public readonly RoadFootprintQuerySystem.Context RoadFootprintQueryContext;
        public readonly Action RuntimeUpdate;
        public readonly Action OnGui;
        public readonly Action Dispose;

        public Result(
            RoadBuildReadModelSystem roadBuildReadModel,
            RoadRuntimeGenerationSystem roadRuntimeGeneration,
            RoadRuntimeGenerationSystem.Context roadRuntimeGenerationContext,
            RoadFootprintQuerySystem roadFootprintQuery,
            RoadFootprintQuerySystem.Context roadFootprintQueryContext,
            Action runtimeUpdate,
            Action onGui,
            Action dispose)
        {
            RoadBuildReadModel = roadBuildReadModel;
            RoadRuntimeGeneration = roadRuntimeGeneration;
            RoadRuntimeGenerationContext = roadRuntimeGenerationContext;
            RoadFootprintQuery = roadFootprintQuery;
            RoadFootprintQueryContext = roadFootprintQueryContext;
            RuntimeUpdate = runtimeUpdate;
            OnGui = onGui;
            Dispose = dispose;
        }
    }

    public Result Initialize(
        RoadBuildSystemConfig roadBuildConfig,
        Camera worldCamera,
        Transform runtimeUiRoot)
    {
        var roadSource = new RoadBuildCompositionSourceSystem();
        _roadSource = roadSource;
        RoadBuildCompositionContextSystem contextSystem = roadSource.RoadBuildCompositionContextSystem;
        RoadBuildCompositionLifecycleSystem lifecycleSystem = roadSource.RoadBuildCompositionLifecycleSystem;
        lifecycleSystem.Init(roadSource, contextSystem, roadBuildConfig, worldCamera, runtimeUiRoot, null);

        RoadBuildReadModelSystem roadBuildReadModel = roadSource.RoadBuildReadModelSystem;

        return new Result(
            roadBuildReadModel,
            roadSource.RoadRuntimeGenerationSystem,
            contextSystem.CreateRoadRuntimeGenerationContext(roadSource),
            roadSource.RoadFootprintQuerySystem,
            contextSystem.CreateRoadFootprintQueryContext(roadSource),
            () => roadSource.RoadBuildRuntimeActionSystem.Update(roadSource.RoadBuildRuntimeActionState),
            () => roadSource.RoadBuildRuntimeActionSystem.OnGui(roadSource.RoadBuildRuntimeActionState),
            () => lifecycleSystem.Dispose(roadSource, contextSystem));
    }

    public void BindBuildingInteraction(
        Result result,
        BuildingPlacementInteractionSystem buildingPlacementInteraction,
        BuildingPlacementInteractionSystem.Context buildingPlacementInteractionContext)
    {
        BindDependencies(
            buildingPlacementInteraction,
            buildingPlacementInteractionContext);
    }

    public void BindMainMenu(
        Result result,
        BuildingPlacementInteractionSystem buildingPlacementInteraction,
        BuildingPlacementInteractionSystem.Context buildingPlacementInteractionContext,
        MainMenuPlayUI mainMenu)
    {
        BindDependencies(
            buildingPlacementInteraction,
            buildingPlacementInteractionContext,
            mainMenu);
    }

    public void BindRuntimeGameplayFeatures(
        Result result,
        BuildingPlacementInteractionSystem buildingPlacementInteraction,
        BuildingPlacementInteractionSystem.Context buildingPlacementInteractionContext,
        MainMenuPlayUI mainMenu,
        RuntimeGridBlockerSystem runtimeGridBlockers)
    {
        BindDependencies(
            buildingPlacementInteraction,
            buildingPlacementInteractionContext,
            mainMenu,
            runtimeGridBlockers);
    }

    private void BindDependencies(
        BuildingPlacementInteractionSystem buildingPlacementInteraction,
        BuildingPlacementInteractionSystem.Context buildingPlacementInteractionContext = default,
        MainMenuPlayUI mainMenu = null,
        RuntimeGridBlockerSystem runtimeGridBlockers = null)
    {
        _roadSource?.RoadBuildCompositionLifecycleSystem.BindDependencies(
            _roadSource,
            buildingPlacementInteraction,
            buildingPlacementInteractionContext,
            mainMenu,
            runtimeGridBlockers);
    }
}
