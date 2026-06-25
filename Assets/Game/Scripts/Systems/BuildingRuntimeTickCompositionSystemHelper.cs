using System;
using UnityEngine;

internal sealed class BuildingRuntimeTickCompositionSystemHelper
{
    public BuildingPlacementRuntimeTickContextCompositionSystemHelper.Source Create(
        BuildingGameplaySourceCompositionSystemHelper source,
        BuildingPlacementInteractionBoundaryCompositionSystemHelper.Context interactionContext,
        MaterialPropertyBlock markerPropertyBlock,
        Func<BuildingGameplaySourceCompositionSystemHelper, BuildingRuntimeContextFactoryCompositionSystemHelper.RuntimeSource> createRuntimeContextSource,
        Func<BuildingGameplaySourceCompositionSystemHelper, BuildingPlacementInteractionBoundaryCompositionSystemHelper.Context, MaterialPropertyBlock, BuildingPlacementInputRuntimeTickUiSystemHelper.Context> createInputRuntimeTickContext,
        Func<BuildingGameplaySourceCompositionSystemHelper, BuildingProductionRuntimeTickCompositionSystemHelper.Context> createProductionRuntimeTickContext,
        Func<BuildingGameplaySourceCompositionSystemHelper, BuildingPlacementInteractionBoundaryCompositionSystemHelper.Context, MaterialPropertyBlock, BuildingRuntimeBoundaryPublishCompositionSystemHelper.Context> createRuntimeBoundaryPublishContext,
        Func<BuildingGameplaySourceCompositionSystemHelper, BuildingPlacementInteractionBoundaryCompositionSystemHelper.Context, MaterialPropertyBlock, Action> createMapBuildingPlacementSpawnUpdate,
        Func<BuildingGameplaySourceCompositionSystemHelper, BuildingPlacementInteractionBoundaryCompositionSystemHelper.Context, MaterialPropertyBlock, Action> createMapVehiclePlacementSpawnUpdate,
        float destroyedBuildingLifetimeSeconds)
    {
        BuildingRuntimeContextFactoryCompositionSystemHelper.RuntimeSource runtimeSource = createRuntimeContextSource(source);
        BuildingRuntimeVisualPresentationSystemHelper.Context runtimeVisualContext = source.BuildingRuntimeContextFactoryCompositionSystemHelper.CreateRuntimeVisualContext(runtimeSource);
        BuildingSelectionMarkerSystem.Context selectionMarkerContext =
            source.BuildingRuntimeContextFactoryCompositionSystemHelper.CreateSelectionMarkerContext(
                runtimeSource,
                source.BuildingPlacementStartupSystemHelper.BuildingSelectionMarkerPrefab,
                source.BuildingPlacementStartupSystemHelper.BuildingRoot,
                markerPropertyBlock,
                source.RuntimeObjectPresentationHelper.DestroyRuntimeObject);
        BuildingCombatUtilitySystemHelper.Context<RuntimeBuildingEntity> combatContext = source.BuildingRuntimeContextFactoryCompositionSystemHelper.CreateCombatContext(runtimeSource);
        BuildingBarrierUtilitySystemHelper.Context barrierContext = source.BuildingRuntimeContextFactoryCompositionSystemHelper.CreateBarrierContext(runtimeSource);
        BuildingPlacementInputRuntimeTickUiSystemHelper.Context inputContext = createInputRuntimeTickContext(source, interactionContext, markerPropertyBlock);
        return new BuildingPlacementRuntimeTickContextCompositionSystemHelper.Source(
            createProductionRuntimeTickContext(source),
            createRuntimeBoundaryPublishContext(source, interactionContext, markerPropertyBlock),
            () => source.BuildingRuntimeVisualPresentationSystemHelper.UpdateBuildingResourceVisuals(runtimeVisualContext, UnityEngine.Time.time),
            () => source.BuildingCombatUtilitySystemHelper.SyncDestroyedRuntimeBuildingCombatEntities(
                combatContext,
                UnityEngine.Time.time,
                destroyedBuildingLifetimeSeconds),
            () => source.BuildingCombatUtilitySystemHelper.UpdateDestroyedBuildings(combatContext, UnityEngine.Time.time),
            () => source.BuildingBarrierUtilitySystemHelper.UpdateRoadBarrierDoors(barrierContext, UnityEngine.Time.deltaTime),
            () => source.BuildingPlacementRedirectCompositionSystemHelper.FlushPendingMarkerRefresh(
                () => source.BuildingSelectionMarkerSystem.Refresh(selectionMarkerContext)),
            createMapBuildingPlacementSpawnUpdate?.Invoke(source, interactionContext, markerPropertyBlock),
            createMapVehiclePlacementSpawnUpdate?.Invoke(source, interactionContext, markerPropertyBlock),
            () => source.BuildingPlacementInputRuntimeTickUiSystemHelper.Update(inputContext),
            BuildingPlacementRuntimeTickDiagnosticsSystemHelper.CreateContext(
                () => source.RuntimeDiagnosticsSystem.ShouldLogBuildingRuntimeSlices,
                () => source.RuntimeBuildingSystem.Count,
                Debug.Log));
    }
}
