using System;
using Unity.Entities;
using UnityEngine;

internal sealed class BuildingRuntimeContextCompositionSystemHelper
{
    internal delegate bool TryGetEntityManagerDelegate(out EntityManager entityManager);

    internal delegate bool TryGetGridDataDelegate(
        BuildingGameplaySourceCompositionSystemHelper source,
        out Entity gridEntity,
        out GridConfig grid,
        out DynamicBuffer<GridRoad> roads,
        out DynamicBlockerComponent blockerData);

    internal delegate RectInt GetEffectivePlacementRectDelegate(
        BuildingGameplaySourceCompositionSystemHelper source,
        BuildingDefinition definition,
        Vector2Int originCell,
        GridConfig grid,
        bool rotateVertical);

    internal delegate bool OverlapsAnyRuntimeBuildingDelegate(
        BuildingGameplaySourceCompositionSystemHelper source,
        RectInt candidateRect);

    internal delegate bool IsHouseBuildingDelegate(
        BuildingGameplaySourceCompositionSystemHelper source,
        RuntimeBuildingEntity building);

    internal delegate bool TryResolveBuildingFocusWorldPositionDelegate(
        BuildingGameplaySourceCompositionSystemHelper source,
        RuntimeBuildingEntity building,
        out Vector3 worldPosition);

    internal delegate bool TryGetRuntimeBuildingDelegate(
        BuildingGameplaySourceCompositionSystemHelper source,
        int id,
        out RuntimeBuildingEntity building);

    public BuildingRuntimeContextFactoryCompositionSystemHelper.Source CreateBuildingRuntimeContextSource(
        BuildingGameplaySourceCompositionSystemHelper source,
        BuildingPlacementInteractionBoundaryCompositionSystemHelper.Context interactionContext,
        MaterialPropertyBlock markerPropertyBlock,
        TryGetEntityManagerDelegate tryGetEntityManager,
        TryGetGridDataDelegate tryGetGridData,
        GetEffectivePlacementRectDelegate getEffectivePlacementRect,
        OverlapsAnyRuntimeBuildingDelegate overlapsAnyRuntimeBuilding,
        IsHouseBuildingDelegate isHouseBuilding,
        TryResolveBuildingFocusWorldPositionDelegate tryResolveBuildingFocusWorldPosition,
        TryGetRuntimeBuildingDelegate tryGetRuntimeBuilding,
        Action<BuildingGameplaySourceCompositionSystemHelper> beginDeferredRuntimeBuildingSideEffects,
        Action<BuildingGameplaySourceCompositionSystemHelper> endDeferredRuntimeBuildingSideEffects,
        float destroyedBuildingLifetimeSeconds)
    {
        BuildingRuntimeContextFactoryCompositionSystemHelper.RuntimeSource CreateRuntimeSource() =>
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

        return new BuildingRuntimeContextFactoryCompositionSystemHelper.Source(
            source.BuildingPlacementStartupSystemHelper.BuildingRoot,
            source.BuildingDefinitionPrefabSystemHelper,
            source.BuildingRunwaySystem,
            source.BuildingPlacementValidationUtilitySystemHelper,
            new BuildingPlacementValidationUtilitySystemHelper.WallValidationContext(
                source.RuntimeBuildingSystem.Buildings,
                source.BuildingGameplayDependencyCompositionSystemHelper.IsRuntimeBlockerCell,
                (grid, origin, footprint) => source.BuildingPlacementInvalidCellCacheCompositionSystemHelper.HasRoadInFootprint(source.BuildingPlacementStartupSystemHelper, grid, origin, footprint)),
            (out Entity gridEntity, out GridConfig grid, out DynamicBuffer<GridRoad> roads, out DynamicBlockerComponent blockerData) =>
                tryGetGridData(source, out gridEntity, out grid, out roads, out blockerData),
            source.BuildingPlacementGridCameraSystemHelper.GetPlacementFootprint,
            (definition, origin, grid, rotateVertical) => getEffectivePlacementRect(source, definition, origin, grid, rotateVertical),
            (definition, origin, footprint, rotateVertical, grid, roads, blockerData) => source.BuildingPlacementAdapterCompositionSystemHelper.IsPlacementValid(
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
            source.BuildingPlacementInvalidCellCacheCompositionSystemHelper.HasCachedInvalidCellInFootprint,
            source.BuildingPlacementVisualPresentationSystemHelper.CreateBuildingVisualInstance,
            (instance, originCell, definition, grid, rotateVertical) => source.BuildingPlacementVisualPresentationSystemHelper.PositionBuildingObject(
                instance,
                originCell,
                definition,
                grid,
                rotateVertical,
                source.BuildingPlacementGridCameraSystemHelper.GetPlacementFootprint,
                (origin, footprint, gridConfig) => source.BuildingPlacementGridCameraSystemHelper.GetFootprintCenter(origin, footprint, gridConfig, source.BuildingPlacementStartupSystemHelper.BuildPlaneY),
                (Vector2Int origin, BuildingDefinition definition, out bool gateVertical) => source.BuildingPlacementAdapterCompositionSystemHelper.TryAlignGateToNearbyWall(
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
            (definition, instance, originCell, removeOverlappingBlockers) => source.BuildingRuntimeCreationCompositionSystemHelper.RegisterRuntimeBuilding(
                source.BuildingRuntimeContextFactoryCompositionSystemHelper.CreateCreationContext(CreateBuildingRuntimeContextSource(
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
                source.BuildingRuntimeContextFactoryCompositionSystemHelper.CreateOwnershipContext(CreateBuildingRuntimeContextSource(
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
            source.BuildingPlacementInteractionBoundaryCompositionSystemHelper,
            interactionContext,
            source.RuntimeBuildingEntityLinkRegistry,
            () => source.BuildingPlacementRedirectCompositionSystemHelper.IsDeferringSideEffects,
            (out GridConfig grid) => tryGetGridData(source, out _, out grid, out _, out _),
            (definition, origin, grid) => getEffectivePlacementRect(source, definition, origin, grid, false),
            source.BuildingGameplayDependencyCompositionSystemHelper.RemoveBlockersOverlappingFootprint,
            source.BuildingRuntimeEntitySystem,
            CreateRuntimeEntityContext(),
            source.BuildingPlacementRedirectCompositionSystemHelper,
            source.BuildingGameplayEcsQueryCompositionSystemHelper.EnsureEntityQueries,
            () => source.BuildingGameplayEcsQueryCompositionSystemHelper.RedirectUnitsQuery,
            building => source.BuildingRuntimeVisualPresentationSystemHelper.InitializeBuildingVisuals(
                source.BuildingRuntimeContextFactoryCompositionSystemHelper.CreateRuntimeVisualContext(CreateRuntimeSource()),
                building),
            () => source.BuildingSelectionMarkerSystem.Refresh(
                source.BuildingRuntimeContextFactoryCompositionSystemHelper.CreateSelectionMarkerContext(
                    CreateRuntimeSource(),
                    source.BuildingPlacementStartupSystemHelper.BuildingSelectionMarkerPrefab,
                    source.BuildingPlacementStartupSystemHelper.BuildingRoot,
                    markerPropertyBlock,
                    source.RuntimeObjectPresentationHelper.DestroyRuntimeObject)),
            (out EntityManager entityManager) => tryGetEntityManager(out entityManager),
            source.BuildingVisualSystem,
            source.BuildingFactionVisualSystem,
            source.BuildingGameplayDependencyCompositionSystemHelper.FactionVisualSettings,
            markerPropertyBlock,
            source.BuildingGameplayDependencyCompositionSystemHelper.BuildingFactionTintStrength,
            buildingId => source.BuildingRuntimeEntitySystem.DeleteBuildingById(CreateRuntimeEntityContext(), buildingId),
            () => beginDeferredRuntimeBuildingSideEffects(source),
            () => endDeferredRuntimeBuildingSideEffects(source));
    }

    public BuildingRuntimeEntitySystem.Context CreateBuildingRuntimeEntityContext(
        BuildingGameplaySourceCompositionSystemHelper source,
        TryGetEntityManagerDelegate tryGetEntityManager,
        TryGetGridDataDelegate tryGetGridData,
        IsHouseBuildingDelegate isHouseBuilding,
        TryResolveBuildingFocusWorldPositionDelegate tryResolveBuildingFocusWorldPosition,
        TryGetRuntimeBuildingDelegate tryGetRuntimeBuilding,
        GetEffectivePlacementRectDelegate getEffectivePlacementRect,
        float destroyedBuildingLifetimeSeconds)
    {
        BuildingRuntimeContextFactoryCompositionSystemHelper.RuntimeSource runtimeSource = CreateRuntimeContextSource(
            source,
            (out EntityManager entityManager) => tryGetEntityManager(out entityManager),
            tryGetGridData,
            isHouseBuilding,
            tryResolveBuildingFocusWorldPosition,
            tryGetRuntimeBuilding,
            getEffectivePlacementRect);
        BuildingCombatUtilitySystemHelper.Context<RuntimeBuildingEntity> combatContext =
            source.BuildingRuntimeContextFactoryCompositionSystemHelper.CreateCombatContext(runtimeSource);
        return source.BuildingRuntimeContextFactoryCompositionSystemHelper.CreateRuntimeEntityContext(
            runtimeSource,
            source.BuildingCombatUtilitySystemHelper,
            combatContext,
            () => UnityEngine.Time.time,
            destroyedBuildingLifetimeSeconds);
    }

    public BuildingRuntimeContextFactoryCompositionSystemHelper.RuntimeSource CreateRuntimeContextSource(
        BuildingGameplaySourceCompositionSystemHelper source,
        TryGetEntityManagerDelegate tryGetEntityManager,
        TryGetGridDataDelegate tryGetGridData,
        IsHouseBuildingDelegate isHouseBuilding,
        TryResolveBuildingFocusWorldPositionDelegate tryResolveBuildingFocusWorldPosition,
        TryGetRuntimeBuildingDelegate tryGetRuntimeBuilding,
        GetEffectivePlacementRectDelegate getEffectivePlacementRect)
    {
        return new BuildingRuntimeContextFactoryCompositionSystemHelper.RuntimeSource(
            source.RuntimeBuildingSystem,
            source.BuildingProductionQueueCompositionSystemHelper,
            source.BuildingProductionSlotUtilitySystemHelper,
            source.BuildingSpawnPrefabSystem,
            BuildingRuntimeResourcePrefabContextCompositionSystemHelper.CreateBuildingSpawnPrefabContext(
                source.BuildingRuntimeResourcePrefabContextCompositionSystemHelper,
                BuildingRuntimeResourcePrefabCompositionSystemHelper.Create(
                    source.BuildingRuntimeResourcePrefabCompositionHelper,
                    source)),
            source.BuildingVisualSystem,
            source.BuildingRuntimeVisualPresentationSystemHelper,
            source.BuildingFactionVisualSystem,
            source.BuildingDestroyedVisualPresentationSystemHelper,
            source.BuildingBarrierUtilitySystemHelper,
            source.BuildingResourceHaulerBridgeCompositionSystemHelper,
            source.ResourceHaulerSystem,
            source.FactionResourceSystem,
            source.BuildingProductionContextCompositionSystemHelper,
            source.BuildingGameplayDependencyCompositionSystemHelper.FactionVisualSettings,
            null,
            source.BuildingGameplayDependencyCompositionSystemHelper.BuildingFactionTintStrength,
            source.BuildingGameplayEcsQueryCompositionSystemHelper.LiveUnitFootprintQuery,
            source.BuildingGameplayEcsQueryCompositionSystemHelper.RedirectUnitsQuery,
            source.BuildingGameplayEcsQueryCompositionSystemHelper.HaulerUnitsQuery,
            source.BuildingGameplayEcsQueryCompositionSystemHelper.SelectedUnitsQuery,
            source.BuildingGameplayEcsQueryCompositionSystemHelper.LiveFactionUnitsQuery,
            source.BuildingGameplayEcsQueryCompositionSystemHelper.BuildingRuntimeBoundaryQuery,
            () => source.RuntimeBuildingSystem.CurrentActiveBuildingId,
            (out EntityManager entityManager) => tryGetEntityManager(out entityManager),
            (out Entity gridEntity, out GridConfig grid, out DynamicBuffer<GridRoad> roads, out DynamicBlockerComponent blockerData) =>
                tryGetGridData(source, out gridEntity, out grid, out roads, out blockerData),
            source.BuildingGameplayEcsQueryCompositionSystemHelper.EnsureEntityQueries,
            (origin, footprint, grid) => source.BuildingPlacementGridCameraSystemHelper.GetFootprintCenter(origin, footprint, grid, source.BuildingPlacementStartupSystemHelper.BuildPlaneY),
            building => isHouseBuilding(source, building),
            (RuntimeBuildingEntity building, out Vector3 worldPosition) => tryResolveBuildingFocusWorldPosition(source, building, out worldPosition),
            (int id, out RuntimeBuildingEntity building) => tryGetRuntimeBuilding(source, id, out building),
            (building, grid) => getEffectivePlacementRect(source, building.Definition, building.OriginCell, grid, false),
            building => source.BuildingBarrierUtilitySystemHelper.RememberOpenBaseBreach(
                source.BuildingRuntimeContextFactoryCompositionSystemHelper.CreateBarrierContext(CreateRuntimeContextSource(
                    source,
                    tryGetEntityManager,
                    tryGetGridData,
                    isHouseBuilding,
                    tryResolveBuildingFocusWorldPosition,
                    tryGetRuntimeBuilding,
                    getEffectivePlacementRect)),
                building),
            source.BuildingGameplayDependencyCompositionSystemHelper.NotifyHomeBuildingDestroyed,
            source.RuntimeObjectPresentationHelper.DestroyRuntimeObject,
            () => source.BuildingSelectionMarkerSystem.Refresh(
                source.BuildingRuntimeContextFactoryCompositionSystemHelper.CreateSelectionMarkerContext(
                    CreateRuntimeContextSource(
                        source,
                        tryGetEntityManager,
                        tryGetGridData,
                        isHouseBuilding,
                        tryResolveBuildingFocusWorldPosition,
                        tryGetRuntimeBuilding,
                        getEffectivePlacementRect),
                    source.BuildingPlacementStartupSystemHelper.BuildingSelectionMarkerPrefab,
                    source.BuildingPlacementStartupSystemHelper.BuildingRoot,
                    null,
                    source.RuntimeObjectPresentationHelper.DestroyRuntimeObject)),
            source.BuildingGameplayDependencyCompositionSystemHelper.NotifyStaticMinimapChanged,
            message => Debug.Log(message),
            false);
    }
}
