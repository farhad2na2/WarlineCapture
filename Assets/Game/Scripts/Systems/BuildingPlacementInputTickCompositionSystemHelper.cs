using System;
using UnityEngine;

internal sealed class BuildingPlacementInputTickCompositionSystemHelper
{
    public BuildingPlacementInputRuntimeTickSystem.Context Create(
        BuildingGameplaySourceCompositionSystemHelper source,
        BuildingPlacementInteractionSystem.Context interactionContext,
        MaterialPropertyBlock markerPropertyBlock,
        float clickDragThresholdPixels,
        Func<BuildingGameplaySourceCompositionSystemHelper, BuildingPlacementInteractionSystem.Context, MaterialPropertyBlock, BuildingPlacementCommandSystem.Context> createPlacementCommandContext,
        Func<BuildingGameplaySourceCompositionSystemHelper, BuildingPlacementInteractionSystem.Context, MaterialPropertyBlock, BuildingPlacementInputSystem.ActivePlacementPointerContext> createActivePlacementPointerContext,
        Func<BuildingGameplaySourceCompositionSystemHelper, BuildingSelectionClickSystem.Context> createSelectionClickContext)
    {
        return new BuildingPlacementInputRuntimeTickSystem.Context(
            () => source.BuildingPlacementStartupSystemHelper.WorldCamera,
            () => source.BuildingPlacementLifecycleCompositionSystemHelper.ActivePlacement,
            source.BuildingPlacementInputSystem,
            createActivePlacementPointerContext(source, interactionContext, markerPropertyBlock),
            () => source.RuntimeGameplayStateSystem.PlayRequested,
            () => source.RuntimeGameplayStateSystem.BuildModeActive,
            source.BuildingPlacementPreviewPresentationSystemHelper,
            () => source.RuntimeBuildingSystem.CurrentActiveBuildingId.HasValue,
            source.RuntimeGameplayStateSystem,
            () => source.BuildingGameplayDependencyCompositionSystemHelper.MainMenuPlayUi,
            source.BuildingSelectionClickSystem,
            createSelectionClickContext(source),
            () => source.BuildingGameplayDependencyCompositionSystemHelper.IsBuildingSelectionClickBlocked(),
            clickDragThresholdPixels,
            () => ProcessPendingPlacementCommands(
                source,
                interactionContext,
                markerPropertyBlock,
                createPlacementCommandContext));
    }

    private static void ProcessPendingPlacementCommands(
        BuildingGameplaySourceCompositionSystemHelper source,
        BuildingPlacementInteractionSystem.Context interactionContext,
        MaterialPropertyBlock markerPropertyBlock,
        Func<BuildingGameplaySourceCompositionSystemHelper, BuildingPlacementInteractionSystem.Context, MaterialPropertyBlock, BuildingPlacementCommandSystem.Context> createPlacementCommandContext)
    {
        if (source?.BuildingPlacementCommandSystem == null ||
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
