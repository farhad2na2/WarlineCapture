using System;
using UnityEngine;

internal sealed class BuildingPlacementInputTickCompositionSystem
{
    public BuildingPlacementInputRuntimeTickSystem.Context Create(
        BuildingGameplayCompositionSourceSystem source,
        BuildingPlacementInteractionSystem.Context interactionContext,
        MaterialPropertyBlock markerPropertyBlock,
        float clickDragThresholdPixels,
        Func<BuildingGameplayCompositionSourceSystem, BuildingPlacementInteractionSystem.Context, MaterialPropertyBlock, BuildingPlacementCommandSystem.Context> createPlacementCommandContext,
        Func<BuildingGameplayCompositionSourceSystem, BuildingPlacementInteractionSystem.Context, MaterialPropertyBlock, BuildingPlacementInputSystem.ActivePlacementPointerContext> createActivePlacementPointerContext,
        Func<BuildingGameplayCompositionSourceSystem, BuildingSelectionClickSystem.Context> createSelectionClickContext)
    {
        return new BuildingPlacementInputRuntimeTickSystem.Context(
            () => source.BuildingPlacementStartupSystem.WorldCamera,
            () => source.BuildingPlacementLifecycleSystem.ActivePlacement,
            source.BuildingPlacementInputSystem,
            createActivePlacementPointerContext(source, interactionContext, markerPropertyBlock),
            () => source.RuntimeGameplayStateSystem.PlayRequested,
            () => source.RuntimeGameplayStateSystem.BuildModeActive,
            source.BuildingPlacementPreviewSystem,
            () => source.RuntimeBuildingSystem.CurrentActiveBuildingId.HasValue,
            source.RuntimeGameplayStateSystem,
            () => source.BuildingGameplayDependencySystem.MainMenuPlayUi,
            source.BuildingSelectionClickSystem,
            createSelectionClickContext(source),
            () => source.BuildingGameplayDependencySystem.IsBuildingSelectionClickBlocked(),
            clickDragThresholdPixels,
            () => ProcessPendingPlacementCommands(
                source,
                interactionContext,
                markerPropertyBlock,
                createPlacementCommandContext));
    }

    private static void ProcessPendingPlacementCommands(
        BuildingGameplayCompositionSourceSystem source,
        BuildingPlacementInteractionSystem.Context interactionContext,
        MaterialPropertyBlock markerPropertyBlock,
        Func<BuildingGameplayCompositionSourceSystem, BuildingPlacementInteractionSystem.Context, MaterialPropertyBlock, BuildingPlacementCommandSystem.Context> createPlacementCommandContext)
    {
        if (source?.BuildingPlacementCommandSystem == null ||
            source.BuildingEntityManagerAccessSystem == null ||
            createPlacementCommandContext == null ||
            !source.BuildingEntityManagerAccessSystem.TryGetEntityManager(out Unity.Entities.EntityManager entityManager))
        {
            return;
        }

        source.BuildingPlacementCommandSystem.ProcessPendingUiPlacementCommandsIfPresent(
            entityManager,
            createPlacementCommandContext(source, interactionContext, markerPropertyBlock));
    }
}
