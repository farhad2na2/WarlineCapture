using System;
using UnityEngine;

internal sealed class BuildingRuntimeTickCompositionSystem
{
    public BuildingPlacementRuntimeTickContextSystem.Source Create(
        BuildingGameplayCompositionSourceSystem source,
        BuildingPlacementInteractionSystem.Context interactionContext,
        MaterialPropertyBlock markerPropertyBlock,
        Func<BuildingGameplayCompositionSourceSystem, BuildingRuntimeContextSystem.RuntimeSource> createRuntimeContextSource,
        Func<BuildingGameplayCompositionSourceSystem, BuildingPlacementInteractionSystem.Context, MaterialPropertyBlock, BuildingPlacementInputRuntimeTickSystem.Context> createInputRuntimeTickContext,
        Func<BuildingGameplayCompositionSourceSystem, BuildingProductionRuntimeTickSystem.Context> createProductionRuntimeTickContext,
        Func<BuildingGameplayCompositionSourceSystem, BuildingPlacementInteractionSystem.Context, MaterialPropertyBlock, BuildingRuntimeBoundaryPublishSystem.Context> createRuntimeBoundaryPublishContext,
        Func<BuildingGameplayCompositionSourceSystem, BuildingPlacementInteractionSystem.Context, MaterialPropertyBlock, Action> createMapBuildingPlacementSpawnUpdate,
        Func<BuildingGameplayCompositionSourceSystem, BuildingPlacementInteractionSystem.Context, MaterialPropertyBlock, Action> createMapVehiclePlacementSpawnUpdate,
        float destroyedBuildingLifetimeSeconds)
    {
        BuildingRuntimeContextSystem.RuntimeSource runtimeSource = createRuntimeContextSource(source);
        BuildingRuntimeVisualSystem.Context runtimeVisualContext = source.BuildingRuntimeContextSystem.CreateRuntimeVisualContext(runtimeSource);
        BuildingSelectionMarkerSystem.Context selectionMarkerContext =
            source.BuildingRuntimeContextSystem.CreateSelectionMarkerContext(
                runtimeSource,
                source.BuildingPlacementStartupSystem.BuildingSelectionMarkerPrefab,
                source.BuildingPlacementStartupSystem.BuildingRoot,
                markerPropertyBlock,
                source.BuildingRuntimeObjectSystem.DestroyRuntimeObject);
        BuildingCombatSystem.Context<RuntimeBuildingEntity> combatContext = source.BuildingRuntimeContextSystem.CreateCombatContext(runtimeSource);
        BuildingBarrierSystem.Context barrierContext = source.BuildingRuntimeContextSystem.CreateBarrierContext(runtimeSource);
        BuildingPlacementInputRuntimeTickSystem.Context inputContext = createInputRuntimeTickContext(source, interactionContext, markerPropertyBlock);
        return new BuildingPlacementRuntimeTickContextSystem.Source(
            createProductionRuntimeTickContext(source),
            createRuntimeBoundaryPublishContext(source, interactionContext, markerPropertyBlock),
            () => source.BuildingRuntimeVisualSystem.UpdateBuildingResourceVisuals(runtimeVisualContext, Time.time),
            () => source.BuildingCombatSystem.SyncDestroyedRuntimeBuildingCombatEntities(
                combatContext,
                Time.time,
                destroyedBuildingLifetimeSeconds),
            () => source.BuildingCombatSystem.UpdateDestroyedBuildings(combatContext, Time.time),
            () => source.BuildingBarrierSystem.UpdateRoadBarrierDoors(barrierContext, Time.deltaTime),
            () => source.BuildingPlacementRedirectSystem.FlushPendingMarkerRefresh(
                () => source.BuildingSelectionMarkerSystem.Refresh(selectionMarkerContext)),
            createMapBuildingPlacementSpawnUpdate?.Invoke(source, interactionContext, markerPropertyBlock),
            createMapVehiclePlacementSpawnUpdate?.Invoke(source, interactionContext, markerPropertyBlock),
            () => source.BuildingPlacementInputRuntimeTickSystem.Update(inputContext),
            CreateRuntimeTickDiagnosticsContext(source));
    }

    private static BuildingPlacementRuntimeTickDiagnosticsSystem.Context CreateRuntimeTickDiagnosticsContext(BuildingGameplayCompositionSourceSystem source)
    {
        return new BuildingPlacementRuntimeTickDiagnosticsSystem.Context(
            () => source.RuntimeBuildingSystem.Count,
            Debug.Log);
    }
}
