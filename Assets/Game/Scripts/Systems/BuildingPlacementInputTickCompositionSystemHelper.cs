using System;
using UnityEngine;

internal sealed class BuildingPlacementInputTickCompositionSystemHelper
{
    public BuildingPlacementInputRuntimeTickUiSystemHelper.Context Create(
        BuildingGameplaySourceCompositionSystemHelper source,
        BuildingPlacementInteractionCompositionSystemHelper.Context interactionContext,
        MaterialPropertyBlock markerPropertyBlock,
        float clickDragThresholdPixels,
        Func<BuildingGameplaySourceCompositionSystemHelper, BuildingPlacementInteractionCompositionSystemHelper.Context, MaterialPropertyBlock, BuildingPlacementCommandRequestCompositionSystemHelper.Context> createPlacementCommandContext,
        Func<BuildingGameplaySourceCompositionSystemHelper, BuildingPlacementInteractionCompositionSystemHelper.Context, MaterialPropertyBlock, BuildingPlacementInputUiSystemHelper.ActivePlacementPointerContext> createActivePlacementPointerContext,
        Func<BuildingGameplaySourceCompositionSystemHelper, BuildingSelectionClickUtilitySystemHelper.Context> createSelectionClickContext)
    {
        return new BuildingPlacementInputRuntimeTickUiSystemHelper.Context(
            () => source.BuildingPlacementStartupSystemHelper.WorldCamera,
            () => source.BuildingPlacementLifecycleCompositionSystemHelper.ActivePlacement,
            source.BuildingPlacementInputUiSystemHelper,
            createActivePlacementPointerContext(source, interactionContext, markerPropertyBlock),
            () => source.RuntimeGameplayStateSystem.PlayRequested,
            () => source.RuntimeGameplayStateSystem.BuildModeActive,
            source.BuildingPlacementPreviewPresentationSystemHelper,
            () => source.RuntimeBuildingSystem.CurrentActiveBuildingId.HasValue,
            source.RuntimeGameplayStateSystem,
            () => source.BuildingGameplayDependencyCompositionSystemHelper.MainMenuPlayUi,
            source.BuildingSelectionClickUtilitySystemHelper,
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
        BuildingPlacementInteractionCompositionSystemHelper.Context interactionContext,
        MaterialPropertyBlock markerPropertyBlock,
        Func<BuildingGameplaySourceCompositionSystemHelper, BuildingPlacementInteractionCompositionSystemHelper.Context, MaterialPropertyBlock, BuildingPlacementCommandRequestCompositionSystemHelper.Context> createPlacementCommandContext)
    {
        if (source?.BuildingPlacementCommandRequestCompositionSystemHelper == null ||
            createPlacementCommandContext == null ||
            !source.BuildingEntityManagerAccessSystem.TryGetEntityManager(out Unity.Entities.EntityManager entityManager))
        {
            return;
        }

        source.BuildingPlacementCommandRequestCompositionSystemHelper.ProcessPendingUiPlacementCommandsIfPresent(
            entityManager,
            createPlacementCommandContext(source, interactionContext, markerPropertyBlock));
    }
}
