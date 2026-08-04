using System;
using Unity.Entities;
using UnityEngine;
using Game.Components;

namespace Game.Runtime
{
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

        internal delegate bool OverlapsAnyPlacementOccupantDelegate(
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
            BuildingPlacementInteractionCompositionSystemHelper.Context interactionContext,
            MaterialPropertyBlock markerPropertyBlock,
            TryGetEntityManagerDelegate tryGetEntityManager,
            TryGetGridDataDelegate tryGetGridData,
            GetEffectivePlacementRectDelegate getEffectivePlacementRect,
            OverlapsAnyPlacementOccupantDelegate overlapsAnyPlacementOccupant,
            IsHouseBuildingDelegate isHouseBuilding,
            TryResolveBuildingFocusWorldPositionDelegate tryResolveBuildingFocusWorldPosition,
            TryGetRuntimeBuildingDelegate tryGetRuntimeBuilding,
            Action<BuildingGameplaySourceCompositionSystemHelper> beginDeferredRuntimeBuildingSideEffects,
            Action<BuildingGameplaySourceCompositionSystemHelper> endDeferredRuntimeBuildingSideEffects,
            float destroyedBuildingLifetimeSeconds)
        {
            BuildingRuntimeContextFactoryCompositionSystemHelper factory = source.BuildingRuntimeContextFactoryCompositionSystemHelper;
            BuildingRuntimeContextFactoryCompositionSystemHelper.RuntimeSource runtimeSource = CreateRuntimeContextSource(
                source,
                tryGetEntityManager,
                tryGetGridData,
                isHouseBuilding,
                tryResolveBuildingFocusWorldPosition,
                tryGetRuntimeBuilding,
                getEffectivePlacementRect);
            BuildingCombatUtilitySystemHelper.Context<RuntimeBuildingEntity> combatContext = factory.CreateCombatContext(runtimeSource);
            BuildingRuntimeEntityCompositionSystemHelper.Context runtimeEntityContext = factory.CreateRuntimeEntityContext(
                runtimeSource,
                source.BuildingCombatUtilitySystemHelper,
                combatContext,
                () => Time.time,
                destroyedBuildingLifetimeSeconds);
            BuildingRuntimeVisualPresentationSystemHelper.Context runtimeVisualContext = factory.CreateRuntimeVisualContext(runtimeSource);
            BuildingSelectionMarkerPresentationSystemHelper.Context selectionMarkerContext = factory.CreateSelectionMarkerContext(
                runtimeSource,
                source.BuildingPlacementStartupSystemHelper.BuildingSelectionMarkerPrefab,
                source.BuildingPlacementStartupSystemHelper.BuildingRoot,
                markerPropertyBlock,
                source.RuntimeObjectPresentationHelper.DestroyRuntimeObject);
            BuildingRuntimeCreationCompositionSystemHelper.Context creationContext = default;
            BuildingRuntimeOwnershipCompositionSystemHelper.Context ownershipContext = default;
            BuildingRuntimeContextFactoryCompositionSystemHelper.Source result = new(
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
                    (placementSource, candidateRect) => overlapsAnyPlacementOccupant(placementSource, candidateRect)),
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
                        _ => runtimeSource,
                        out gateVertical)),
                (definition, instance, originCell, removeOverlappingBlockers) => source.BuildingRuntimeCreationCompositionSystemHelper.RegisterRuntimeBuilding(
                    creationContext,
                    definition,
                    instance,
                    originCell,
                    removeOverlappingBlockers),
                (building, ownerFactionId) => source.BuildingRuntimeOwnershipCompositionSystemHelper.SetRuntimeBuildingOwnerFaction(
                    ownershipContext,
                    building,
                    ownerFactionId),
                source.RuntimeBuildingSystem,
                source.BuildingPlacementInteractionCompositionSystemHelper,
                interactionContext,
                source.RuntimeBuildingEntityLinkRegistry,
                () => source.BuildingPlacementRedirectCompositionSystemHelper.IsDeferringSideEffects,
                (out GridConfig grid) => tryGetGridData(source, out _, out grid, out _, out _),
                (definition, origin, grid) => getEffectivePlacementRect(source, definition, origin, grid, false),
                source.BuildingGameplayDependencyCompositionSystemHelper.RemoveBlockersOverlappingFootprint,
                source.BuildingRuntimeEntityCompositionSystemHelper,
                runtimeEntityContext,
                source.BuildingPlacementRedirectCompositionSystemHelper,
                source.BuildingGameplayEcsQueryCompositionSystemHelper.EnsureEntityQueries,
                () => source.BuildingGameplayEcsQueryCompositionSystemHelper.RedirectUnitsQuery,
                building => source.BuildingRuntimeVisualPresentationSystemHelper.InitializeBuildingVisuals(
                    runtimeVisualContext,
                    building),
                () => source.BuildingSelectionMarkerPresentationSystemHelper.Refresh(
                    selectionMarkerContext),
                (out EntityManager entityManager) => tryGetEntityManager(out entityManager),
                source.BuildingVisualSystem,
                source.BuildingFactionVisualSystem,
                source.BuildingGameplayDependencyCompositionSystemHelper.FactionVisualSettings,
                markerPropertyBlock,
                source.BuildingGameplayDependencyCompositionSystemHelper.BuildingFactionTintStrength,
                buildingId => source.BuildingRuntimeEntityCompositionSystemHelper.DeleteBuildingById(runtimeEntityContext, buildingId),
                () => beginDeferredRuntimeBuildingSideEffects(source),
                () => endDeferredRuntimeBuildingSideEffects(source));
            creationContext = factory.CreateCreationContext(result);
            ownershipContext = factory.CreateOwnershipContext(result);
            return result;
        }

        public BuildingRuntimeEntityCompositionSystemHelper.Context CreateBuildingRuntimeEntityContext(
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
            BuildingRuntimeContextFactoryCompositionSystemHelper factory = source.BuildingRuntimeContextFactoryCompositionSystemHelper;
            BuildingBarrierUtilitySystemHelper.Context barrierContext = default;
            BuildingSelectionMarkerPresentationSystemHelper.Context markerContext = default;
            BuildingRuntimeContextFactoryCompositionSystemHelper.RuntimeSource result = new(
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
                source.ResourceHaulerUtilitySystemHelper,
                source.FactionResourceCompositionSystemHelper,
                source.BuildingProductionContextCompositionSystemHelper,
                source.BuildingGameplayDependencyCompositionSystemHelper.FactionVisualSettings,
                null,
                source.BuildingGameplayDependencyCompositionSystemHelper.BuildingFactionTintStrength,
                source.BuildingGameplayEcsQueryCompositionSystemHelper.LiveUnitFootprintQuery,
                source.BuildingGameplayEcsQueryCompositionSystemHelper.RedirectUnitsQuery,
                source.BuildingGameplayEcsQueryCompositionSystemHelper.HaulerUnitsQuery,
                source.BuildingGameplayEcsQueryCompositionSystemHelper.SelectedUnitsQuery,
                source.BuildingGameplayEcsQueryCompositionSystemHelper.LiveFactionUnitsQuery,
                source.BuildingGameplayEcsQueryCompositionSystemHelper.BuildingRuntimeStateQuery,
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
                source.BuildingGameplayEcsQueryCompositionSystemHelper.TryResolveFactionAIOilAllocationInput,
                building => source.BuildingBarrierUtilitySystemHelper.RememberOpenBaseBreach(
                    barrierContext,
                    building),
                source.BuildingGameplayDependencyCompositionSystemHelper.NotifyHomeBuildingDestroyed,
                source.RuntimeObjectPresentationHelper.DestroyRuntimeObject,
                () => source.BuildingSelectionMarkerPresentationSystemHelper.Refresh(
                    markerContext),
                source.BuildingGameplayDependencyCompositionSystemHelper.NotifyStaticMinimapChanged,
                message => Debug.Log(message),
                false);
            barrierContext = factory.CreateBarrierContext(result);
            markerContext = factory.CreateSelectionMarkerContext(
                result,
                source.BuildingPlacementStartupSystemHelper.BuildingSelectionMarkerPrefab,
                source.BuildingPlacementStartupSystemHelper.BuildingRoot,
                null,
                source.RuntimeObjectPresentationHelper.DestroyRuntimeObject);
            return result;
        }
    }
}
