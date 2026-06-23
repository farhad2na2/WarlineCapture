using Unity.Entities;
using UnityEngine;

internal sealed class RoadBuildCompositionLifecycleSystem
{
    public void Init(
        RoadBuildCompositionSourceSystem source,
        RoadBuildCompositionContextSystem contextSystem,
        RoadBuildSystemConfig configAsset,
        Camera sceneWorldCamera,
        Transform runtimeRoot,
        BuildingPlacementInteractionSystem buildingPlacementInteractionSystem,
        BuildingPlacementInteractionSystem.Context buildingPlacementInteractionContext = default)
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
        source.RoadBuildReadModelSystem.Configure(contextSystem.CreateRoadBuildReadModelContext(source));
        RoadBuildRuntimeActionSystem.ConfigureInput(
            source.RoadBuildRuntimeActionState,
            source.RoadBuildInteractionContextSystem,
            contextSystem.CreateRoadBuildInteractionContext(source),
            source.RoadBuildStartupState.WorldCamera);
        RoadBuildRuntimeActionSystem.ConfigureCommands(
            source.RoadBuildRuntimeActionState,
            source.RoadBuildCommandSystem,
            contextSystem.CreateRoadBuildCommandContext(source),
            source.RoadBuildEcsBoundarySystem.TryGetEntityManager);
        RoadBuildRuntimeActionSystem.ConfigureGui(
            source.RoadBuildRuntimeActionState,
            source.RoadDeletePromptSystem,
            contextSystem.CreateRoadDeletePromptContext(source));

        source.RoadBuildDefinitionProjectionSystem.BuildDefinitions(
            source.RoadBuildStartupState.SoldierBasePrefab,
            source.RoadBuildStartupState.SoldierBaseFootprintCells,
            source.RoadBuildPlacementStorageSystem);
        source.RoadBuildPlacementVisualSystem?.CreatePlacementOutline(
            source.RoadBuildPlacementVisualState,
            source.RoadBuildStartupState.RuntimeRoot,
            source.RoadBuildStartupState.PlacementValidColor);
    }

    public void BindDependencies(
        RoadBuildCompositionSourceSystem source,
        BuildingPlacementInteractionSystem buildingPlacementInteractionSystem,
        BuildingPlacementInteractionSystem.Context buildingPlacementInteractionContext = default,
        IMatchRuntimeUi mainMenuPlayUi = null,
        RuntimeGridBlockerPresentationSystemHelper runtimeGridBlockers = null)
    {
        source.RoadBuildDependencySystem.BindDependencies(
            source.RoadBuildDependencyState,
            buildingPlacementInteractionSystem,
            buildingPlacementInteractionContext,
            mainMenuPlayUi,
            runtimeGridBlockers,
            source.RoadMinimapEventSystem);
    }

    public void Dispose(
        RoadBuildCompositionSourceSystem source,
        RoadBuildCompositionContextSystem contextSystem)
    {
        RoadBuildCommandSystem.Context commandContext = contextSystem.CreateRoadBuildCommandContext(source);
        if (source.RoadBuildEcsBoundarySystem.TryGetEntityManager(out EntityManager entityManager))
            source.RoadBuildCommandSystem.EnqueueAndProcessExitBuildMode(entityManager, commandContext);
        else
            ExitBuildModeWithoutEntityManager(commandContext);
        source.RoadBuildSessionSystem.ResetSkipBuildClickFrames(source.RoadBuildSessionState);
        source.RoadBuildDisposalSystem.Dispose(contextSystem.CreateRoadBuildDisposalContext(source));
    }

    private static void ExitBuildModeWithoutEntityManager(RoadBuildCommandSystem.Context commandContext)
    {
        commandContext.ClearRoadBuildDragState?.Invoke();
        commandContext.SessionSystem?.ExitBuildMode(commandContext.SessionContext);
    }
}
