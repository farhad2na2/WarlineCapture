using Unity.Entities;
using UnityEngine;

internal sealed class RoadBuildCompositionLifecycleCompositionSystemHelper
{
    public void Init(
        RoadBuildCompositionSourceSystem source,
        RoadBuildCompositionContextCompositionSystemHelper contextSystem,
        RoadBuildSystemConfig configAsset,
        Camera sceneWorldCamera,
        Transform runtimeRoot,
        BuildingPlacementInteractionBoundaryCompositionSystemHelper buildingPlacementInteractionSystem,
        BuildingPlacementInteractionBoundaryCompositionSystemHelper.Context buildingPlacementInteractionContext = default)
    {
        source.RoadBuildStartupState = source.RoadBuildStartupSystem.Initialize(
            configAsset,
            sceneWorldCamera,
            runtimeRoot,
            source.RoadBuildConfigSystem,
            source.RoadRuntimeRootSystem,
            source.RoadVisualVariantSystem);
        source.RoadBuildDependencySystem.BindBuildingInteraction(
            source.RoadBuildDependencyState,
            buildingPlacementInteractionSystem,
            buildingPlacementInteractionContext);
        source.RoadBuildReadModelCompositionSystemHelper.Configure(contextSystem.CreateRoadBuildReadModelContext(source));
        RoadBuildRuntimeActionCompositionSystemHelper.ConfigureInput(
            source.RoadBuildRuntimeActionState,
            source.RoadBuildInteractionContextSystem,
            contextSystem.CreateRoadBuildInteractionContext(source),
            source.RoadBuildStartupState.WorldCamera);
        RoadBuildRuntimeActionCompositionSystemHelper.ConfigureCommands(
            source.RoadBuildRuntimeActionState,
            source.RoadBuildCommandCompositionSystemHelper,
            contextSystem.CreateRoadBuildCommandContext(source),
            source.RoadBuildEcsBoundaryCompositionSystemHelper.TryGetEntityManager);
        RoadBuildRuntimeActionCompositionSystemHelper.ConfigureGui(
            source.RoadBuildRuntimeActionState,
            source.RoadDeletePromptSystem,
            contextSystem.CreateRoadDeletePromptContext(source));

        source.RoadBuildDefinitionProjectionSystem.BuildDefinitions(
            source.RoadBuildStartupState.SoldierBasePrefab,
            source.RoadBuildStartupState.SoldierBaseFootprintCells,
            source.RoadBuildPlacementStorageCompositionSystemHelper);
        source.RoadBuildPlacementVisualSystem?.CreatePlacementOutline(
            source.RoadBuildPlacementVisualState,
            source.RoadBuildStartupState.RuntimeRoot,
            source.RoadBuildStartupState.PlacementValidColor);
    }

    public void BindDependencies(
        RoadBuildCompositionSourceSystem source,
        BuildingPlacementInteractionBoundaryCompositionSystemHelper buildingPlacementInteractionSystem,
        BuildingPlacementInteractionBoundaryCompositionSystemHelper.Context buildingPlacementInteractionContext = default,
        IMatchRuntimeUi mainMenuPlayUi = null,
        RuntimeGridBlockerPresentationSystemHelper runtimeGridBlockers = null,
        RuntimeBuildingEntityLinkRegistry runtimeBuildingEntityLinks = null)
    {
        source.RoadBuildDependencySystem.BindDependencies(
            source.RoadBuildDependencyState,
            buildingPlacementInteractionSystem,
            buildingPlacementInteractionContext,
            mainMenuPlayUi,
            runtimeGridBlockers,
            runtimeBuildingEntityLinks,
            source.RoadMinimapEventSystem);
    }

    public void Dispose(
        RoadBuildCompositionSourceSystem source,
        RoadBuildCompositionContextCompositionSystemHelper contextSystem)
    {
        RoadBuildCommandCompositionSystemHelper.Context commandContext = contextSystem.CreateRoadBuildCommandContext(source);
        if (source.RoadBuildEcsBoundaryCompositionSystemHelper.TryGetEntityManager(out EntityManager entityManager))
            source.RoadBuildCommandCompositionSystemHelper.EnqueueAndProcessExitBuildMode(entityManager, commandContext);
        else
            ExitBuildModeWithoutEntityManager(commandContext);
        source.RoadBuildSessionCompositionSystemHelper.ResetSkipBuildClickFrames(source.RoadBuildSessionState);
        source.RoadBuildDisposalSystem.Dispose(contextSystem.CreateRoadBuildDisposalContext(source));
    }

    private static void ExitBuildModeWithoutEntityManager(RoadBuildCommandCompositionSystemHelper.Context commandContext)
    {
        commandContext.ClearRoadBuildDragState?.Invoke();
        commandContext.SessionSystem?.ExitBuildMode(commandContext.SessionContext);
    }
}
