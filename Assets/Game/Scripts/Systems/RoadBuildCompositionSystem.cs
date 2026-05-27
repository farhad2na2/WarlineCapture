using System;
using UnityEngine;

internal sealed class RoadBuildCompositionSystem
{
    private RoadBuildRuntimeStateSystem _roadState;

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
        var roadBuild = new RoadBuildRuntimeStateSystem(roadSource);
        _roadState = roadBuild;
        roadBuild.Init(roadBuildConfig, worldCamera, runtimeUiRoot, null);

        RoadBuildReadModelSystem roadBuildReadModel = roadSource.RoadBuildReadModelSystem;

        return new Result(
            roadBuildReadModel,
            roadBuild.RoadRuntimeGenerationSystem,
            roadBuild.RoadRuntimeGenerationContext,
            roadBuild.RoadFootprintQuerySystem,
            roadBuild.RoadFootprintQueryContext,
            () => roadSource.RoadBuildRuntimeActionSystem.Update(roadSource.RoadBuildRuntimeActionState),
            () => roadSource.RoadBuildRuntimeActionSystem.OnGui(roadSource.RoadBuildRuntimeActionState),
            roadBuild.Dispose);
    }

    public void BindBuildingInteraction(
        Result result,
        BuildingPlacementInteractionSystem buildingPlacementInteraction,
        BuildingPlacementInteractionSystem.Context buildingPlacementInteractionContext)
    {
        _roadState?.BindDependencies(
            buildingPlacementInteraction,
            buildingPlacementInteractionContext);
    }

    public void BindMainMenu(
        Result result,
        BuildingPlacementInteractionSystem buildingPlacementInteraction,
        BuildingPlacementInteractionSystem.Context buildingPlacementInteractionContext,
        MainMenuPlayUI mainMenu)
    {
        _roadState?.BindDependencies(
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
        _roadState?.BindDependencies(
            buildingPlacementInteraction,
            buildingPlacementInteractionContext,
            mainMenu,
            runtimeGridBlockers);
    }
}
