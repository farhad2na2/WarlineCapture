using System;
using UnityEngine;

internal sealed class RoadBuildCompositionSystemHelper
{
    private RoadBuildCompositionSourceSystem _roadSource;

    public readonly struct Result
    {
        public readonly RoadBuildReadModelCompositionSystemHelper RoadBuildReadModel;
        public readonly RoadRuntimeGenerationCompositionSystemHelper RoadRuntimeGeneration;
        public readonly RoadRuntimeGenerationCompositionSystemHelper.Context RoadRuntimeGenerationContext;
        public readonly RoadGridProjectionSystem.RoadFootprintState RoadFootprintState;
        public readonly Action RuntimeUpdate;
        public readonly Action OnGui;
        public readonly Action Dispose;

        public Result(
            RoadBuildReadModelCompositionSystemHelper roadBuildReadModel,
            RoadRuntimeGenerationCompositionSystemHelper roadRuntimeGeneration,
            RoadRuntimeGenerationCompositionSystemHelper.Context roadRuntimeGenerationContext,
            RoadGridProjectionSystem.RoadFootprintState roadFootprintState,
            Action runtimeUpdate,
            Action onGui,
            Action dispose)
        {
            RoadBuildReadModel = roadBuildReadModel;
            RoadRuntimeGeneration = roadRuntimeGeneration;
            RoadRuntimeGenerationContext = roadRuntimeGenerationContext;
            RoadFootprintState = roadFootprintState;
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
        RoadBuildCompositionContextCompositionSystemHelper contextSystem = roadSource.RoadBuildCompositionContextCompositionSystemHelper;
        RoadBuildCompositionLifecycleCompositionSystemHelper lifecycleSystem = roadSource.RoadBuildCompositionLifecycleCompositionSystemHelper;
        lifecycleSystem.Init(roadSource, contextSystem, roadBuildConfig, worldCamera, runtimeUiRoot, null);

        RoadBuildReadModelCompositionSystemHelper roadBuildReadModel = roadSource.RoadBuildReadModelCompositionSystemHelper;

        return new Result(
            roadBuildReadModel,
            roadSource.RoadRuntimeGenerationCompositionSystemHelper,
            contextSystem.CreateRoadRuntimeGenerationContext(roadSource),
            contextSystem.CreateRoadFootprintState(roadSource),
            () => RoadBuildRuntimeActionCompositionSystemHelper.Update(roadSource.RoadBuildRuntimeActionState),
            () => RoadBuildRuntimeActionCompositionSystemHelper.OnGui(roadSource.RoadBuildRuntimeActionState),
            () => lifecycleSystem.Dispose(roadSource, contextSystem));
    }

    public void BindBuildingInteraction(
        Result result,
        BuildingPlacementInteractionBoundaryCompositionSystemHelper buildingPlacementInteraction,
        BuildingPlacementInteractionBoundaryCompositionSystemHelper.Context buildingPlacementInteractionContext)
    {
        BindDependencies(
            buildingPlacementInteraction,
            buildingPlacementInteractionContext);
    }

    public void BindMainMenu(
        Result result,
        BuildingPlacementInteractionBoundaryCompositionSystemHelper buildingPlacementInteraction,
        BuildingPlacementInteractionBoundaryCompositionSystemHelper.Context buildingPlacementInteractionContext,
        IMatchRuntimeUi mainMenu)
    {
        BindDependencies(
            buildingPlacementInteraction,
            buildingPlacementInteractionContext,
            mainMenu);
    }

    public void BindRuntimeGameplayFeatures(
        Result result,
        BuildingPlacementInteractionBoundaryCompositionSystemHelper buildingPlacementInteraction,
        BuildingPlacementInteractionBoundaryCompositionSystemHelper.Context buildingPlacementInteractionContext,
        IMatchRuntimeUi mainMenu,
        RuntimeGridBlockerPresentationSystemHelper runtimeGridBlockers,
        RuntimeBuildingEntityLinkRegistry runtimeBuildingEntityLinks)
    {
        BindDependencies(
            buildingPlacementInteraction,
            buildingPlacementInteractionContext,
            mainMenu,
            runtimeGridBlockers,
            runtimeBuildingEntityLinks);
    }

    private void BindDependencies(
        BuildingPlacementInteractionBoundaryCompositionSystemHelper buildingPlacementInteraction,
        BuildingPlacementInteractionBoundaryCompositionSystemHelper.Context buildingPlacementInteractionContext = default,
        IMatchRuntimeUi mainMenu = null,
        RuntimeGridBlockerPresentationSystemHelper runtimeGridBlockers = null,
        RuntimeBuildingEntityLinkRegistry runtimeBuildingEntityLinks = null)
    {
        _roadSource?.RoadBuildCompositionLifecycleCompositionSystemHelper.BindDependencies(
            _roadSource,
            buildingPlacementInteraction,
            buildingPlacementInteractionContext,
            mainMenu,
            runtimeGridBlockers,
            runtimeBuildingEntityLinks);
    }
}
