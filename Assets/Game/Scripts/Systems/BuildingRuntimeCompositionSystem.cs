using System;
using Unity.Entities;
using UnityEngine;

internal sealed partial class BuildingRuntimeCompositionSystem : SystemBase
{
    protected override void OnCreate()
    {
        Enabled = false;
    }

    protected override void OnUpdate()
    {
    }

    internal delegate bool TryGetEntityManagerDelegate(out EntityManager entityManager);

    internal delegate bool TryGetGridDataDelegate(
        BuildingGameplayCompositionSourceSystem source,
        out Entity gridEntity,
        out GridConfig grid,
        out DynamicBuffer<GridRoad> roads,
        out DynamicBlockerComponent blockerData);

    internal delegate RectInt GetEffectivePlacementRectDelegate(
        BuildingGameplayCompositionSourceSystem source,
        BuildingDefinition definition,
        Vector2Int originCell,
        GridConfig grid,
        bool rotateVertical);

    internal delegate bool OverlapsAnyRuntimeBuildingDelegate(
        BuildingGameplayCompositionSourceSystem source,
        RectInt candidateRect);

    internal delegate bool IsHouseBuildingDelegate(
        BuildingGameplayCompositionSourceSystem source,
        RuntimeBuildingEntity building);

    internal delegate bool TryResolveBuildingFocusWorldPositionDelegate(
        BuildingGameplayCompositionSourceSystem source,
        RuntimeBuildingEntity building,
        out Vector3 worldPosition);

    internal delegate bool TryGetRuntimeBuildingDelegate(
        BuildingGameplayCompositionSourceSystem source,
        int id,
        out RuntimeBuildingEntity building);

    public BuildingRuntimeContextSystem.Source CreateBuildingRuntimeContextSource(
        BuildingGameplayCompositionSourceSystem source,
        BuildingPlacementInteractionSystem.Context interactionContext,
        MaterialPropertyBlock markerPropertyBlock,
        TryGetEntityManagerDelegate tryGetEntityManager,
        TryGetGridDataDelegate tryGetGridData,
        GetEffectivePlacementRectDelegate getEffectivePlacementRect,
        OverlapsAnyRuntimeBuildingDelegate overlapsAnyRuntimeBuilding,
        IsHouseBuildingDelegate isHouseBuilding,
        TryResolveBuildingFocusWorldPositionDelegate tryResolveBuildingFocusWorldPosition,
        TryGetRuntimeBuildingDelegate tryGetRuntimeBuilding,
        Action<BuildingGameplayCompositionSourceSystem> beginDeferredRuntimeBuildingSideEffects,
        Action<BuildingGameplayCompositionSourceSystem> endDeferredRuntimeBuildingSideEffects,
        float destroyedBuildingLifetimeSeconds)
    {
        BuildingRuntimeContextSystem.RuntimeSource CreateRuntimeSource() =>
            CreateRuntimeContextSource(
                source,
                tryGetEntityManager,
                tryGetGridData,
                isHouseBuilding,
                tryResolveBuildingFocusWorldPosition,
                tryGetRuntimeBuilding,
                getEffectivePlacementRect);

        BuildingRuntimeEntitySystem.Context CreateRuntimeEntityContext() =>
            CreateBuildingRuntimeEntityContext(
                source,
                tryGetEntityManager,
                tryGetGridData,
                isHouseBuilding,
                tryResolveBuildingFocusWorldPosition,
                tryGetRuntimeBuilding,
                getEffectivePlacementRect,
                destroyedBuildingLifetimeSeconds);

        return new BuildingRuntimeContextSystem.Source(
            source.BuildingPlacementStartupSystem.BuildingRoot,
            source.BuildingDefinitionSystem,
            source.BuildingRunwaySystem,
            source.BuildingPlacementValidationSystem,
            new BuildingPlacementValidationSystem.WallValidationContext(
                source.RuntimeBuildingSystem.Buildings,
                source.BuildingGameplayDependencySystem.IsRuntimeBlockerCell,
                (grid, origin, footprint) => source.BuildingPlacementInvalidCellSystem.HasRoadInFootprint(source.BuildingPlacementStartupSystem, grid, origin, footprint)),
            (out Entity gridEntity, out GridConfig grid, out DynamicBuffer<GridRoad> roads, out DynamicBlockerComponent blockerData) =>
                tryGetGridData(source, out gridEntity, out grid, out roads, out blockerData),
            source.BuildingPlacementGridSystem.GetPlacementFootprint,
            (definition, origin, grid, rotateVertical) => getEffectivePlacementRect(source, definition, origin, grid, rotateVertical),
            (definition, origin, footprint, rotateVertical, grid, roads, blockerData) => source.BuildingPlacementAdapterSystem.IsPlacementValid(
                source,
                definition,
                origin,
                footprint,
                rotateVertical,
                grid,
                roads,
                blockerData,
                (placementSource, placementDefinition, placementOrigin, placementGrid, placementRotateVertical) =>
                    getEffectivePlacementRect(placementSource, placementDefinition, placementOrigin, placementGrid, placementRotateVertical),
                (placementSource, candidateRect) => overlapsAnyRuntimeBuilding(placementSource, candidateRect)),
            source.BuildingPlacementInvalidCellSystem.HasCachedInvalidCellInFootprint,
            source.BuildingPlacementVisualSystem.CreateBuildingVisualInstance,
            (instance, originCell, definition, grid, rotateVertical) => source.BuildingPlacementVisualSystem.PositionBuildingObject(
                instance,
                originCell,
                definition,
                grid,
                rotateVertical,
                source.BuildingPlacementGridSystem.GetPlacementFootprint,
                (origin, footprint, gridConfig) => source.BuildingPlacementGridSystem.GetFootprintCenter(origin, footprint, gridConfig, source.BuildingPlacementStartupSystem.BuildPlaneY),
                (Vector2Int origin, BuildingDefinition definition, out bool gateVertical) => source.BuildingPlacementAdapterSystem.TryAlignGateToNearbyWall(
                    source,
                    origin,
                    definition,
                    runtimeSourceSource => CreateRuntimeContextSource(
                        runtimeSourceSource,
                        tryGetEntityManager,
                        tryGetGridData,
                        isHouseBuilding,
                        tryResolveBuildingFocusWorldPosition,
                        tryGetRuntimeBuilding,
                        getEffectivePlacementRect),
                    out gateVertical)),
            (definition, instance, originCell, removeOverlappingBlockers) => source.BuildingRuntimeCreationSystem.RegisterRuntimeBuilding(
                source.BuildingRuntimeContextSystem.CreateCreationContext(CreateBuildingRuntimeContextSource(
                    source,
                    interactionContext,
                    markerPropertyBlock,
                    tryGetEntityManager,
                    tryGetGridData,
                    getEffectivePlacementRect,
                    overlapsAnyRuntimeBuilding,
                    isHouseBuilding,
                    tryResolveBuildingFocusWorldPosition,
                    tryGetRuntimeBuilding,
                    beginDeferredRuntimeBuildingSideEffects,
                    endDeferredRuntimeBuildingSideEffects,
                    destroyedBuildingLifetimeSeconds)),
                definition,
                instance,
                originCell,
                removeOverlappingBlockers),
            (building, ownerFactionId) => source.BuildingRuntimeOwnershipSystem.SetRuntimeBuildingOwnerFaction(
                source.BuildingRuntimeContextSystem.CreateOwnershipContext(CreateBuildingRuntimeContextSource(
                    source,
                    interactionContext,
                    markerPropertyBlock,
                    tryGetEntityManager,
                    tryGetGridData,
                    getEffectivePlacementRect,
                    overlapsAnyRuntimeBuilding,
                    isHouseBuilding,
                    tryResolveBuildingFocusWorldPosition,
                    tryGetRuntimeBuilding,
                    beginDeferredRuntimeBuildingSideEffects,
                    endDeferredRuntimeBuildingSideEffects,
                    destroyedBuildingLifetimeSeconds)),
                building,
                ownerFactionId),
            source.RuntimeBuildingSystem,
            source.BuildingPlacementInteractionSystem,
            interactionContext,
            () => source.BuildingPlacementRedirectSystem.IsDeferringSideEffects,
            (out GridConfig grid) => tryGetGridData(source, out _, out grid, out _, out _),
            (definition, origin, grid) => getEffectivePlacementRect(source, definition, origin, grid, false),
            source.BuildingGameplayDependencySystem.RemoveBlockersOverlappingFootprint,
            source.BuildingRuntimeEntitySystem,
            CreateRuntimeEntityContext(),
            source.BuildingPlacementRedirectSystem,
            source.BuildingGameplayEcsQuerySystem.EnsureEntityQueries,
            () => source.BuildingGameplayEcsQuerySystem.RedirectUnitsQuery,
            building => source.BuildingRuntimeVisualSystem.InitializeBuildingVisuals(
                source.BuildingRuntimeContextSystem.CreateRuntimeVisualContext(CreateRuntimeSource()),
                building),
            () => source.BuildingSelectionMarkerSystem.Refresh(
                source.BuildingRuntimeContextSystem.CreateSelectionMarkerContext(
                    CreateRuntimeSource(),
                    source.BuildingPlacementStartupSystem.BuildingSelectionMarkerPrefab,
                    source.BuildingPlacementStartupSystem.BuildingRoot,
                    markerPropertyBlock,
                    source.BuildingRuntimeObjectSystem.DestroyRuntimeObject)),
            (out EntityManager entityManager) => tryGetEntityManager(out entityManager),
            source.BuildingVisualSystem,
            source.BuildingFactionVisualSystem,
            source.BuildingGameplayDependencySystem.FactionVisualSettings,
            markerPropertyBlock,
            source.BuildingGameplayDependencySystem.BuildingFactionTintStrength,
            buildingId => source.BuildingRuntimeEntitySystem.DeleteBuildingById(CreateRuntimeEntityContext(), buildingId),
            () => beginDeferredRuntimeBuildingSideEffects(source),
            () => endDeferredRuntimeBuildingSideEffects(source));
    }

    public BuildingRuntimeEntitySystem.Context CreateBuildingRuntimeEntityContext(
        BuildingGameplayCompositionSourceSystem source,
        TryGetEntityManagerDelegate tryGetEntityManager,
        TryGetGridDataDelegate tryGetGridData,
        IsHouseBuildingDelegate isHouseBuilding,
        TryResolveBuildingFocusWorldPositionDelegate tryResolveBuildingFocusWorldPosition,
        TryGetRuntimeBuildingDelegate tryGetRuntimeBuilding,
        GetEffectivePlacementRectDelegate getEffectivePlacementRect,
        float destroyedBuildingLifetimeSeconds)
    {
        BuildingRuntimeContextSystem.RuntimeSource runtimeSource = CreateRuntimeContextSource(
            source,
            (out EntityManager entityManager) => tryGetEntityManager(out entityManager),
            tryGetGridData,
            isHouseBuilding,
            tryResolveBuildingFocusWorldPosition,
            tryGetRuntimeBuilding,
            getEffectivePlacementRect);
        BuildingCombatSystem.Context<RuntimeBuildingEntity> combatContext =
            source.BuildingRuntimeContextSystem.CreateCombatContext(runtimeSource);
        return source.BuildingRuntimeContextSystem.CreateRuntimeEntityContext(
            runtimeSource,
            source.BuildingCombatSystem,
            combatContext,
            () => UnityEngine.Time.time,
            destroyedBuildingLifetimeSeconds);
    }

    public BuildingRuntimeContextSystem.RuntimeSource CreateRuntimeContextSource(
        BuildingGameplayCompositionSourceSystem source,
        TryGetEntityManagerDelegate tryGetEntityManager,
        TryGetGridDataDelegate tryGetGridData,
        IsHouseBuildingDelegate isHouseBuilding,
        TryResolveBuildingFocusWorldPositionDelegate tryResolveBuildingFocusWorldPosition,
        TryGetRuntimeBuildingDelegate tryGetRuntimeBuilding,
        GetEffectivePlacementRectDelegate getEffectivePlacementRect)
    {
        return new BuildingRuntimeContextSystem.RuntimeSource(
            source.RuntimeBuildingSystem,
            source.BuildingProductionSystem,
            source.BuildingProductionSlotSystem,
            source.BuildingSpawnPrefabSystem,
            BuildingRuntimeResourcePrefabContextSystem.CreateBuildingSpawnPrefabContext(
                source.BuildingRuntimeResourcePrefabContextSystem,
                BuildingRuntimeResourcePrefabCompositionSystem.Create(
                    source.BuildingRuntimeResourcePrefabCompositionSystem,
                    source)),
            source.BuildingVisualSystem,
            source.BuildingRuntimeVisualSystem,
            source.BuildingFactionVisualSystem,
            source.BuildingDestroyedVisualSystem,
            source.BuildingBarrierSystem,
            source.BuildingResourceHaulerBridgeSystem,
            source.ResourceHaulerSystem,
            source.FactionResourceSystem,
            source.BuildingProductionContextSystem,
            source.BuildingGameplayDependencySystem.FactionVisualSettings,
            null,
            source.BuildingGameplayDependencySystem.BuildingFactionTintStrength,
            source.BuildingGameplayEcsQuerySystem.LiveUnitFootprintQuery,
            source.BuildingGameplayEcsQuerySystem.RedirectUnitsQuery,
            source.BuildingGameplayEcsQuerySystem.HaulerUnitsQuery,
            source.BuildingGameplayEcsQuerySystem.SelectedUnitsQuery,
            source.BuildingGameplayEcsQuerySystem.LiveFactionUnitsQuery,
            () => source.RuntimeBuildingSystem.CurrentActiveBuildingId,
            (out EntityManager entityManager) => tryGetEntityManager(out entityManager),
            (out Entity gridEntity, out GridConfig grid, out DynamicBuffer<GridRoad> roads, out DynamicBlockerComponent blockerData) =>
                tryGetGridData(source, out gridEntity, out grid, out roads, out blockerData),
            source.BuildingGameplayEcsQuerySystem.EnsureEntityQueries,
            (origin, footprint, grid) => source.BuildingPlacementGridSystem.GetFootprintCenter(origin, footprint, grid, source.BuildingPlacementStartupSystem.BuildPlaneY),
            building => isHouseBuilding(source, building),
            (RuntimeBuildingEntity building, out Vector3 worldPosition) => tryResolveBuildingFocusWorldPosition(source, building, out worldPosition),
            (int id, out RuntimeBuildingEntity building) => tryGetRuntimeBuilding(source, id, out building),
            (building, grid) => getEffectivePlacementRect(source, building.Definition, building.OriginCell, grid, false),
            building => source.BuildingBarrierSystem.RememberOpenBaseBreach(
                source.BuildingRuntimeContextSystem.CreateBarrierContext(CreateRuntimeContextSource(
                    source,
                    tryGetEntityManager,
                    tryGetGridData,
                    isHouseBuilding,
                    tryResolveBuildingFocusWorldPosition,
                    tryGetRuntimeBuilding,
                    getEffectivePlacementRect)),
                building),
            source.BuildingGameplayDependencySystem.NotifyHomeBuildingDestroyed,
            source.BuildingRuntimeObjectSystem.DestroyRuntimeObject,
            () => source.BuildingSelectionMarkerSystem.Refresh(
                source.BuildingRuntimeContextSystem.CreateSelectionMarkerContext(
                    CreateRuntimeContextSource(
                        source,
                        tryGetEntityManager,
                        tryGetGridData,
                        isHouseBuilding,
                        tryResolveBuildingFocusWorldPosition,
                        tryGetRuntimeBuilding,
                        getEffectivePlacementRect),
                    source.BuildingPlacementStartupSystem.BuildingSelectionMarkerPrefab,
                    source.BuildingPlacementStartupSystem.BuildingRoot,
                    null,
                    source.BuildingRuntimeObjectSystem.DestroyRuntimeObject)),
            source.BuildingGameplayDependencySystem.NotifyStaticMinimapChanged,
            message => Debug.Log(message),
            false);
    }
}
