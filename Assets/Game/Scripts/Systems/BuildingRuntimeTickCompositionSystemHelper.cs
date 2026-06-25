using System;
using UnityEngine;

internal sealed class BuildingRuntimeTickCompositionSystemHelper
{
    public BuildingPlacementRuntimeTickContextCompositionSystemHelper.Source Create(
        BuildingGameplaySourceCompositionSystemHelper source,
        BuildingPlacementInteractionSystem.Context interactionContext,
        MaterialPropertyBlock markerPropertyBlock,
        Func<BuildingGameplaySourceCompositionSystemHelper, BuildingRuntimeContextSystem.RuntimeSource> createRuntimeContextSource,
        Func<BuildingGameplaySourceCompositionSystemHelper, BuildingPlacementInteractionSystem.Context, MaterialPropertyBlock, BuildingPlacementInputRuntimeTickSystem.Context> createInputRuntimeTickContext,
        Func<BuildingGameplaySourceCompositionSystemHelper, BuildingProductionRuntimeTickSystem.Context> createProductionRuntimeTickContext,
        Func<BuildingGameplaySourceCompositionSystemHelper, BuildingPlacementInteractionSystem.Context, MaterialPropertyBlock, BuildingRuntimeBoundaryPublishCompositionSystemHelper.Context> createRuntimeBoundaryPublishContext,
        Func<BuildingGameplaySourceCompositionSystemHelper, BuildingPlacementInteractionSystem.Context, MaterialPropertyBlock, Action> createMapBuildingPlacementSpawnUpdate,
        Func<BuildingGameplaySourceCompositionSystemHelper, BuildingPlacementInteractionSystem.Context, MaterialPropertyBlock, Action> createMapVehiclePlacementSpawnUpdate,
        float destroyedBuildingLifetimeSeconds)
    {
        BuildingRuntimeContextSystem.RuntimeSource runtimeSource = createRuntimeContextSource(source);
        BuildingRuntimeVisualPresentationSystemHelper.Context runtimeVisualContext = source.BuildingRuntimeContextSystem.CreateRuntimeVisualContext(runtimeSource);
        BuildingSelectionMarkerSystem.Context selectionMarkerContext =
            source.BuildingRuntimeContextSystem.CreateSelectionMarkerContext(
                runtimeSource,
                source.BuildingPlacementStartupSystemHelper.BuildingSelectionMarkerPrefab,
                source.BuildingPlacementStartupSystemHelper.BuildingRoot,
                markerPropertyBlock,
                source.RuntimeObjectPresentationHelper.DestroyRuntimeObject);
        BuildingCombatSystem.Context<RuntimeBuildingEntity> combatContext = source.BuildingRuntimeContextSystem.CreateCombatContext(runtimeSource);
        BuildingBarrierUtilitySystemHelper.Context barrierContext = source.BuildingRuntimeContextSystem.CreateBarrierContext(runtimeSource);
        BuildingPlacementInputRuntimeTickSystem.Context inputContext = createInputRuntimeTickContext(source, interactionContext, markerPropertyBlock);
        return new BuildingPlacementRuntimeTickContextCompositionSystemHelper.Source(
            createProductionRuntimeTickContext(source),
            createRuntimeBoundaryPublishContext(source, interactionContext, markerPropertyBlock),
            () => source.BuildingRuntimeVisualPresentationSystemHelper.UpdateBuildingResourceVisuals(runtimeVisualContext, UnityEngine.Time.time),
            () => source.BuildingCombatSystem.SyncDestroyedRuntimeBuildingCombatEntities(
                combatContext,
                UnityEngine.Time.time,
                destroyedBuildingLifetimeSeconds),
            () => source.BuildingCombatSystem.UpdateDestroyedBuildings(combatContext, UnityEngine.Time.time),
            () => source.BuildingBarrierUtilitySystemHelper.UpdateRoadBarrierDoors(barrierContext, UnityEngine.Time.deltaTime),
            () => source.BuildingPlacementRedirectCompositionSystemHelper.FlushPendingMarkerRefresh(
                () => source.BuildingSelectionMarkerSystem.Refresh(selectionMarkerContext)),
            createMapBuildingPlacementSpawnUpdate?.Invoke(source, interactionContext, markerPropertyBlock),
            createMapVehiclePlacementSpawnUpdate?.Invoke(source, interactionContext, markerPropertyBlock),
            () => source.BuildingPlacementInputRuntimeTickSystem.Update(inputContext),
            BuildingPlacementRuntimeTickDiagnosticsSystemHelper.CreateContext(
                () => source.RuntimeDiagnosticsSystem.ShouldLogBuildingRuntimeSlices,
                () => source.RuntimeBuildingSystem.Count,
                Debug.Log));
    }
}
