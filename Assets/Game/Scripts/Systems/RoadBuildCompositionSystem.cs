using System;
using UnityEngine;

internal sealed class RoadBuildCompositionSystem
{
    public readonly struct Result
    {
        public readonly RoadBuildRuntimeStateSystem RoadState;
        public readonly RoadBuildReadModelSystem RoadBuildReadModel;
        public readonly RoadRuntimeGenerationSystem RoadRuntimeGeneration;
        public readonly RoadRuntimeGenerationSystem.Context RoadRuntimeGenerationContext;
        public readonly RoadFootprintQuerySystem RoadFootprintQuery;
        public readonly RoadFootprintQuerySystem.Context RoadFootprintQueryContext;
        public readonly Action RuntimeUpdate;
        public readonly Action OnGui;
        public readonly Action Dispose;

        public Result(
            RoadBuildRuntimeStateSystem roadBuild,
            RoadBuildReadModelSystem roadBuildReadModel,
            RoadRuntimeGenerationSystem roadRuntimeGeneration,
            RoadRuntimeGenerationSystem.Context roadRuntimeGenerationContext,
            RoadFootprintQuerySystem roadFootprintQuery,
            RoadFootprintQuerySystem.Context roadFootprintQueryContext,
            Action runtimeUpdate,
            Action onGui,
            Action dispose)
        {
            RoadState = roadBuild;
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
        var roadBuild = new RoadBuildRuntimeStateSystem();
        roadBuild.Init(roadBuildConfig, worldCamera, runtimeUiRoot, null);

        var roadBuildReadModel = new RoadBuildReadModelSystem();
        roadBuildReadModel.Configure(
            () => roadBuild.IsRoadBuildModeActive,
            () => roadBuild.IsDraggingBuildInteraction,
            () => roadBuild.HasPendingBuildingPlacement,
            () => roadBuild.HasSelectedBuilding,
            () => roadBuild.CanConfirmBuildingPlacement);

        return new Result(
            roadBuild,
            roadBuildReadModel,
            roadBuild.RoadRuntimeGenerationSystem,
            roadBuild.RoadRuntimeGenerationContext,
            roadBuild.RoadFootprintQuerySystem,
            roadBuild.RoadFootprintQueryContext,
            () => roadBuild.RoadBuildInputSystem.Update(
                roadBuild.RoadBuildInputContext,
                roadBuild.RoadBuildInputCamera),
            () => roadBuild.RoadDeletePromptSystem.OnGui(roadBuild.RoadDeletePromptContext),
            roadBuild.Dispose);
    }

    public void BindBuildingInteraction(
        Result result,
        BuildingPlacementInteractionSystem buildingPlacementInteraction,
        BuildingPlacementInteractionSystem.Context buildingPlacementInteractionContext)
    {
        result.RoadState?.BindDependencies(
            buildingPlacementInteraction,
            buildingPlacementInteractionContext);
    }

    public void BindMainMenu(
        Result result,
        BuildingPlacementInteractionSystem buildingPlacementInteraction,
        BuildingPlacementInteractionSystem.Context buildingPlacementInteractionContext,
        MainMenuPlayUI mainMenu)
    {
        result.RoadState?.BindDependencies(
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
        result.RoadState?.BindDependencies(
            buildingPlacementInteraction,
            buildingPlacementInteractionContext,
            mainMenu,
            runtimeGridBlockers);
    }
}
