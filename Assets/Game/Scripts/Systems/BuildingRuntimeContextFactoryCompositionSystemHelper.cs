using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Game.Components;

namespace Game.Runtime
{
    internal sealed class BuildingRuntimeContextFactoryCompositionSystemHelper
    {
        public readonly struct RuntimeSource
        {
            public readonly RuntimeBuildingCollection<RuntimeBuildingEntity> RuntimeBuildingSystem;
            public readonly BuildingProductionQueueCompositionSystemHelper ProductionSystem;
            public readonly BuildingProductionSlotUtilitySystemHelper ProductionSlotSystem;
            public readonly BuildingSpawnPrefabSystem SpawnPrefabSystem;
            public readonly BuildingSpawnPrefabSystem.Context SpawnPrefabContext;
            public readonly BuildingVisualSystem BuildingVisualSystem;
            public readonly BuildingRuntimeVisualPresentationSystemHelper RuntimeVisualSystem;
            public readonly BuildingFactionVisualSystem BuildingFactionVisualSystem;
            public readonly BuildingDestroyedVisualPresentationSystemHelper BuildingDestroyedVisualPresentationSystemHelper;
            public readonly BuildingBarrierUtilitySystemHelper BarrierSystem;
            public readonly BuildingResourceHaulerBridgeCompositionSystemHelper ResourceHaulerBridgeSystem;
            public readonly ResourceHaulerUtilitySystemHelper ResourceHaulerUtilitySystemHelper;
            public readonly FactionResourceCompositionSystemHelper FactionResourceCompositionSystemHelper;
            public readonly BuildingProductionContextCompositionSystemHelper ProductionContextSystem;
            public readonly FactionVisualSettings FactionVisualSettings;
            public readonly MaterialPropertyBlock MarkerPropertyBlock;
            public readonly float BuildingFactionTintStrength;
            public readonly EntityQuery LiveUnitFootprintQuery;
            public readonly EntityQuery RedirectUnitsQuery;
            public readonly EntityQuery HaulerUnitsQuery;
            public readonly EntityQuery SelectedUnitsQuery;
            public readonly EntityQuery LiveFactionUnitsQuery;
            public readonly EntityQuery BuildingRuntimeStateQuery;
            public readonly Func<int?> GetActiveBuildingId;
            public readonly BuildingRuntimeEntityCompositionSystemHelper.TryGetEntityManagerDelegate TryGetEntityManager;
            public readonly BuildingRuntimeEntityCompositionSystemHelper.TryGetGridDataDelegate TryGetGridData;
            public readonly Action<EntityManager> EnsureEntityQueries;
            public readonly BuildingRuntimeEntityCompositionSystemHelper.GetFootprintCenterDelegate GetFootprintCenter;
            public readonly BuildingRuntimeReadModelCompositionSystemHelper.BuildingPredicate IsHouseBuilding;
            public readonly BuildingRuntimeReadModelCompositionSystemHelper.TryResolveBuildingWorldPositionDelegate TryResolveBuildingFocusWorldPosition;
            public readonly BuildingResourceHaulerBridgeCompositionSystemHelper.TryGetRuntimeBuildingDelegate TryGetRuntimeBuilding;
            public readonly BuildingResourceHaulerBridgeCompositionSystemHelper.GetEffectivePlacementRectDelegate GetEffectivePlacementRect;
            public readonly BuildingCombatUtilitySystemHelper.BuildingAction<RuntimeBuildingEntity> RememberOpenBaseBreach;
            public readonly BuildingCombatUtilitySystemHelper.BuildingIdAction NotifyHomeBuildingDestroyed;
            public readonly BuildingCombatUtilitySystemHelper.ObjectAction DestroyObject;
            public readonly Action RefreshBuildingMarkerVisibility;
            public readonly Action NotifyStaticMinimapChanged;
            public readonly BuildingCombatUtilitySystemHelper.LogAction Log;
            public readonly bool EnableDestroyDiagnostics;

            public RuntimeSource(
                RuntimeBuildingCollection<RuntimeBuildingEntity> runtimeBuildingSystem,
                BuildingProductionQueueCompositionSystemHelper productionSystem,
                BuildingProductionSlotUtilitySystemHelper productionSlotSystem,
                BuildingSpawnPrefabSystem spawnPrefabSystem,
                BuildingSpawnPrefabSystem.Context spawnPrefabContext,
                BuildingVisualSystem buildingVisualSystem,
                BuildingRuntimeVisualPresentationSystemHelper runtimeVisualSystem,
                BuildingFactionVisualSystem buildingFactionVisualSystem,
                BuildingDestroyedVisualPresentationSystemHelper buildingDestroyedVisualPresentationHelper,
                BuildingBarrierUtilitySystemHelper barrierSystem,
                BuildingResourceHaulerBridgeCompositionSystemHelper resourceHaulerBridgeSystem,
                ResourceHaulerUtilitySystemHelper resourceHaulerSystem,
                FactionResourceCompositionSystemHelper factionResourceSystem,
                BuildingProductionContextCompositionSystemHelper productionContextSystem,
                FactionVisualSettings factionVisualSettings,
                MaterialPropertyBlock markerPropertyBlock,
                float buildingFactionTintStrength,
                EntityQuery liveUnitFootprintQuery,
                EntityQuery redirectUnitsQuery,
                EntityQuery haulerUnitsQuery,
                EntityQuery selectedUnitsQuery,
                EntityQuery liveFactionUnitsQuery,
                EntityQuery buildingRuntimeBoundaryQuery,
                Func<int?> getActiveBuildingId,
                BuildingRuntimeEntityCompositionSystemHelper.TryGetEntityManagerDelegate tryGetEntityManager,
                BuildingRuntimeEntityCompositionSystemHelper.TryGetGridDataDelegate tryGetGridData,
                Action<EntityManager> ensureEntityQueries,
                BuildingRuntimeEntityCompositionSystemHelper.GetFootprintCenterDelegate getFootprintCenter,
                BuildingRuntimeReadModelCompositionSystemHelper.BuildingPredicate isHouseBuilding,
                BuildingRuntimeReadModelCompositionSystemHelper.TryResolveBuildingWorldPositionDelegate tryResolveBuildingFocusWorldPosition,
                BuildingResourceHaulerBridgeCompositionSystemHelper.TryGetRuntimeBuildingDelegate tryGetRuntimeBuilding,
                BuildingResourceHaulerBridgeCompositionSystemHelper.GetEffectivePlacementRectDelegate getEffectivePlacementRect,
                BuildingCombatUtilitySystemHelper.BuildingAction<RuntimeBuildingEntity> rememberOpenBaseBreach,
                BuildingCombatUtilitySystemHelper.BuildingIdAction notifyHomeBuildingDestroyed,
                BuildingCombatUtilitySystemHelper.ObjectAction destroyObject,
                Action refreshBuildingMarkerVisibility,
                Action notifyStaticMinimapChanged,
                BuildingCombatUtilitySystemHelper.LogAction log,
                bool enableDestroyDiagnostics)
            {
                RuntimeBuildingSystem = runtimeBuildingSystem;
                ProductionSystem = productionSystem;
                ProductionSlotSystem = productionSlotSystem;
                SpawnPrefabSystem = spawnPrefabSystem;
                SpawnPrefabContext = spawnPrefabContext;
                BuildingVisualSystem = buildingVisualSystem;
                RuntimeVisualSystem = runtimeVisualSystem;
                BuildingFactionVisualSystem = buildingFactionVisualSystem;
                BuildingDestroyedVisualPresentationSystemHelper = buildingDestroyedVisualPresentationHelper;
                BarrierSystem = barrierSystem;
                ResourceHaulerBridgeSystem = resourceHaulerBridgeSystem;
                ResourceHaulerUtilitySystemHelper = resourceHaulerSystem;
                FactionResourceCompositionSystemHelper = factionResourceSystem;
                ProductionContextSystem = productionContextSystem;
                FactionVisualSettings = factionVisualSettings;
                MarkerPropertyBlock = markerPropertyBlock;
                BuildingFactionTintStrength = Mathf.Clamp01(buildingFactionTintStrength);
                LiveUnitFootprintQuery = liveUnitFootprintQuery;
                RedirectUnitsQuery = redirectUnitsQuery;
                HaulerUnitsQuery = haulerUnitsQuery;
                SelectedUnitsQuery = selectedUnitsQuery;
                LiveFactionUnitsQuery = liveFactionUnitsQuery;
                BuildingRuntimeStateQuery = buildingRuntimeBoundaryQuery;
                GetActiveBuildingId = getActiveBuildingId;
                TryGetEntityManager = tryGetEntityManager;
                TryGetGridData = tryGetGridData;
                EnsureEntityQueries = ensureEntityQueries;
                GetFootprintCenter = getFootprintCenter;
                IsHouseBuilding = isHouseBuilding;
                TryResolveBuildingFocusWorldPosition = tryResolveBuildingFocusWorldPosition;
                TryGetRuntimeBuilding = tryGetRuntimeBuilding;
                GetEffectivePlacementRect = getEffectivePlacementRect;
                RememberOpenBaseBreach = rememberOpenBaseBreach;
                NotifyHomeBuildingDestroyed = notifyHomeBuildingDestroyed;
                DestroyObject = destroyObject;
                RefreshBuildingMarkerVisibility = refreshBuildingMarkerVisibility;
                NotifyStaticMinimapChanged = notifyStaticMinimapChanged;
                Log = log;
                EnableDestroyDiagnostics = enableDestroyDiagnostics;
            }
        }

        public readonly struct Source
        {
            public readonly Transform BuildingRoot;
            public readonly BuildingDefinitionPrefabSystemHelper DefinitionSystem;
            public readonly BuildingRunwaySystem RunwaySystem;
            public readonly BuildingPlacementValidationUtilitySystemHelper PlacementValidationSystem;
            public readonly BuildingPlacementValidationUtilitySystemHelper.WallValidationContext WallValidationContext;
            public readonly BuildingRuntimeSpawnCompositionSystemHelper.TryGetGridDataDelegate TryGetGridData;
            public readonly BuildingRuntimeSpawnCompositionSystemHelper.GetPlacementFootprintDelegate GetPlacementFootprint;
            public readonly BuildingRuntimeSpawnCompositionSystemHelper.GetEffectivePlacementRectDelegate GetEffectivePlacementRect;
            public readonly BuildingRuntimeSpawnCompositionSystemHelper.IsPlacementValidDelegate IsPlacementValid;
            public readonly BuildingRuntimeSpawnCompositionSystemHelper.HasCachedInvalidCellInFootprintDelegate HasCachedInvalidCellInFootprint;
            public readonly BuildingRuntimeSpawnCompositionSystemHelper.CreateBuildingVisualInstanceDelegate CreateBuildingVisualInstance;
            public readonly BuildingRuntimeSpawnCompositionSystemHelper.PositionBuildingObjectDelegate PositionBuildingObject;
            public readonly BuildingRuntimeSpawnCompositionSystemHelper.RegisterRuntimeBuildingDelegate RegisterRuntimeBuilding;
            public readonly BuildingRuntimeSpawnCompositionSystemHelper.SetRuntimeBuildingOwnerFactionDelegate SetRuntimeBuildingOwnerFaction;
            public readonly RuntimeBuildingCollection<RuntimeBuildingEntity> RuntimeBuildingSystem;
            public readonly BuildingPlacementInteractionCompositionSystemHelper RuntimeLinkInteractionSystem;
            public readonly BuildingPlacementInteractionCompositionSystemHelper.Context RuntimeLinkInteractionContext;
            public readonly RuntimeBuildingEntityLinkRegistry RuntimeBuildingEntityLinks;
            public readonly Func<bool> IsDeferringSideEffects;
            public readonly BuildingRuntimeCreationCompositionSystemHelper.TryGetGridDelegate TryGetGridForRuntimeCreation;
            public readonly BuildingRuntimeCreationCompositionSystemHelper.ResolvePlacementRectDelegate ResolvePlacementRect;
            public readonly BuildingRuntimeCreationCompositionSystemHelper.RemoveOverlappingBlockersDelegate RemoveOverlappingBlockers;
            public readonly BuildingRuntimeEntityCompositionSystemHelper RuntimeEntitySystem;
            public readonly BuildingRuntimeEntityCompositionSystemHelper.Context RuntimeEntityContext;
            public readonly BuildingPlacementRedirectCompositionSystemHelper PlacementRedirectSystem;
            public readonly BuildingPlacementRedirectCompositionSystemHelper.EnsureEntityQueriesDelegate EnsureEntityQueries;
            public readonly BuildingPlacementRedirectCompositionSystemHelper.GetRedirectUnitsQueryDelegate GetRedirectUnitsQuery;
            public readonly BuildingRuntimeCreationCompositionSystemHelper.RuntimeBuildingAction InitializeVisuals;
            public readonly BuildingRuntimeCreationCompositionSystemHelper.RuntimeAction RefreshMarkers;
            public readonly BuildingRuntimeOwnershipCompositionSystemHelper.TryGetEntityManagerDelegate TryGetEntityManager;
            public readonly BuildingVisualSystem BuildingVisualSystem;
            public readonly BuildingFactionVisualSystem BuildingFactionVisualSystem;
            public readonly FactionVisualSettings FactionVisualSettings;
            public readonly MaterialPropertyBlock MarkerPropertyBlock;
            public readonly float BuildingFactionTintStrength;
            public readonly Func<int, bool> DeleteBuildingById;
            public readonly Action BeginDeferredRuntimeBuildingSideEffects;
            public readonly Action EndDeferredRuntimeBuildingSideEffects;

            public Source(
                Transform buildingRoot,
                BuildingDefinitionPrefabSystemHelper definitionSystem,
                BuildingRunwaySystem runwaySystem,
                BuildingPlacementValidationUtilitySystemHelper placementValidationSystem,
                BuildingPlacementValidationUtilitySystemHelper.WallValidationContext wallValidationContext,
                BuildingRuntimeSpawnCompositionSystemHelper.TryGetGridDataDelegate tryGetGridData,
                BuildingRuntimeSpawnCompositionSystemHelper.GetPlacementFootprintDelegate getPlacementFootprint,
                BuildingRuntimeSpawnCompositionSystemHelper.GetEffectivePlacementRectDelegate getEffectivePlacementRect,
                BuildingRuntimeSpawnCompositionSystemHelper.IsPlacementValidDelegate isPlacementValid,
                BuildingRuntimeSpawnCompositionSystemHelper.HasCachedInvalidCellInFootprintDelegate hasCachedInvalidCellInFootprint,
                BuildingRuntimeSpawnCompositionSystemHelper.CreateBuildingVisualInstanceDelegate createBuildingVisualInstance,
                BuildingRuntimeSpawnCompositionSystemHelper.PositionBuildingObjectDelegate positionBuildingObject,
                BuildingRuntimeSpawnCompositionSystemHelper.RegisterRuntimeBuildingDelegate registerRuntimeBuilding,
                BuildingRuntimeSpawnCompositionSystemHelper.SetRuntimeBuildingOwnerFactionDelegate setRuntimeBuildingOwnerFaction,
                RuntimeBuildingCollection<RuntimeBuildingEntity> runtimeBuildingSystem,
                BuildingPlacementInteractionCompositionSystemHelper runtimeLinkInteractionSystem,
                BuildingPlacementInteractionCompositionSystemHelper.Context runtimeLinkInteractionContext,
                RuntimeBuildingEntityLinkRegistry runtimeBuildingEntityLinks,
                Func<bool> isDeferringSideEffects,
                BuildingRuntimeCreationCompositionSystemHelper.TryGetGridDelegate tryGetGridForRuntimeCreation,
                BuildingRuntimeCreationCompositionSystemHelper.ResolvePlacementRectDelegate resolvePlacementRect,
                BuildingRuntimeCreationCompositionSystemHelper.RemoveOverlappingBlockersDelegate removeOverlappingBlockers,
                BuildingRuntimeEntityCompositionSystemHelper runtimeEntitySystem,
                BuildingRuntimeEntityCompositionSystemHelper.Context runtimeEntityContext,
                BuildingPlacementRedirectCompositionSystemHelper placementRedirectSystem,
                BuildingPlacementRedirectCompositionSystemHelper.EnsureEntityQueriesDelegate ensureEntityQueries,
                BuildingPlacementRedirectCompositionSystemHelper.GetRedirectUnitsQueryDelegate getRedirectUnitsQuery,
                BuildingRuntimeCreationCompositionSystemHelper.RuntimeBuildingAction initializeVisuals,
                BuildingRuntimeCreationCompositionSystemHelper.RuntimeAction refreshMarkers,
                BuildingRuntimeOwnershipCompositionSystemHelper.TryGetEntityManagerDelegate tryGetEntityManager,
                BuildingVisualSystem buildingVisualSystem,
                BuildingFactionVisualSystem buildingFactionVisualSystem,
                FactionVisualSettings factionVisualSettings,
                MaterialPropertyBlock markerPropertyBlock,
                float buildingFactionTintStrength,
                Func<int, bool> deleteBuildingById,
                Action beginDeferredRuntimeBuildingSideEffects,
                Action endDeferredRuntimeBuildingSideEffects)
            {
                BuildingRoot = buildingRoot;
                DefinitionSystem = definitionSystem;
                RunwaySystem = runwaySystem;
                PlacementValidationSystem = placementValidationSystem;
                WallValidationContext = wallValidationContext;
                TryGetGridData = tryGetGridData;
                GetPlacementFootprint = getPlacementFootprint;
                GetEffectivePlacementRect = getEffectivePlacementRect;
                IsPlacementValid = isPlacementValid;
                HasCachedInvalidCellInFootprint = hasCachedInvalidCellInFootprint;
                CreateBuildingVisualInstance = createBuildingVisualInstance;
                PositionBuildingObject = positionBuildingObject;
                RegisterRuntimeBuilding = registerRuntimeBuilding;
                SetRuntimeBuildingOwnerFaction = setRuntimeBuildingOwnerFaction;
                RuntimeBuildingSystem = runtimeBuildingSystem;
                RuntimeLinkInteractionSystem = runtimeLinkInteractionSystem;
                RuntimeLinkInteractionContext = runtimeLinkInteractionContext;
                RuntimeBuildingEntityLinks = runtimeBuildingEntityLinks;
                IsDeferringSideEffects = isDeferringSideEffects;
                TryGetGridForRuntimeCreation = tryGetGridForRuntimeCreation;
                ResolvePlacementRect = resolvePlacementRect;
                RemoveOverlappingBlockers = removeOverlappingBlockers;
                RuntimeEntitySystem = runtimeEntitySystem;
                RuntimeEntityContext = runtimeEntityContext;
                PlacementRedirectSystem = placementRedirectSystem;
                EnsureEntityQueries = ensureEntityQueries;
                GetRedirectUnitsQuery = getRedirectUnitsQuery;
                InitializeVisuals = initializeVisuals;
                RefreshMarkers = refreshMarkers;
                TryGetEntityManager = tryGetEntityManager;
                BuildingVisualSystem = buildingVisualSystem;
                BuildingFactionVisualSystem = buildingFactionVisualSystem;
                FactionVisualSettings = factionVisualSettings;
                MarkerPropertyBlock = markerPropertyBlock;
                BuildingFactionTintStrength = Mathf.Clamp01(buildingFactionTintStrength);
                DeleteBuildingById = deleteBuildingById;
                BeginDeferredRuntimeBuildingSideEffects = beginDeferredRuntimeBuildingSideEffects;
                EndDeferredRuntimeBuildingSideEffects = endDeferredRuntimeBuildingSideEffects;
            }
        }

        public BuildingRuntimeSpawnCompositionSystemHelper.Context CreateSpawnContext(Source source)
        {
            return new BuildingRuntimeSpawnCompositionSystemHelper.Context(
                source.BuildingRoot,
                source.DefinitionSystem,
                source.RunwaySystem,
                source.PlacementValidationSystem,
                source.WallValidationContext,
                source.TryGetGridData,
                source.GetPlacementFootprint,
                source.GetEffectivePlacementRect,
                source.IsPlacementValid,
                source.HasCachedInvalidCellInFootprint,
                source.CreateBuildingVisualInstance,
                source.PositionBuildingObject,
                source.RegisterRuntimeBuilding,
                source.SetRuntimeBuildingOwnerFaction);
        }

        public BuildingRuntimeSpawnCommandSystemHelper.Context CreateSpawnCommandContext(
            Source source,
            BuildingRuntimeSpawnCompositionSystemHelper runtimeSpawnSystem)
        {
            return new BuildingRuntimeSpawnCommandSystemHelper.Context(
                runtimeSpawnSystem,
                CreateSpawnContext(source));
        }

        public BuildingRuntimeCreationCompositionSystemHelper.Context CreateCreationContext(Source source)
        {
            return new BuildingRuntimeCreationCompositionSystemHelper.Context(
                source.RuntimeBuildingSystem,
                source.RuntimeLinkInteractionSystem,
                source.RuntimeLinkInteractionContext,
                source.RuntimeBuildingEntityLinks,
                source.IsDeferringSideEffects?.Invoke() == true,
                source.TryGetEntityManager,
                source.TryGetGridForRuntimeCreation,
                source.ResolvePlacementRect,
                definition => source.RuntimeEntitySystem == null || source.RuntimeEntitySystem.ShouldRuntimeBuildingBlockPathing(definition),
                source.RemoveOverlappingBlockers,
                (definition, originCell, footprintCells) => source.RuntimeEntitySystem != null
                    ? source.RuntimeEntitySystem.CreateBlockerEntity(source.RuntimeEntityContext, definition, originCell, footprintCells)
                    : Entity.Null,
                (originCell, definition, ownerFactionId, worldRotation) => source.RuntimeEntitySystem != null
                    ? source.RuntimeEntitySystem.CreateBuildingCombatEntity(source.RuntimeEntityContext, originCell, definition, ownerFactionId, worldRotation)
                    : Entity.Null,
                footprint => source.PlacementRedirectSystem?.RedirectUnitsAroundPlacedBuilding(CreateCreationRedirectContext(source), footprint),
                footprint => source.PlacementRedirectSystem?.AddDeferredRedirectFootprint(footprint),
                () => source.PlacementRedirectSystem?.MarkPendingMarkerRefresh(),
                source.InitializeVisuals,
                source.RefreshMarkers);
        }

        public BuildingRuntimeOwnershipCompositionSystemHelper.Context CreateOwnershipContext(Source source)
        {
            return new BuildingRuntimeOwnershipCompositionSystemHelper.Context(
                source.TryGetEntityManager,
                source.FactionVisualSettings,
                source.MarkerPropertyBlock,
                source.BuildingFactionVisualSystem,
                source.BuildingFactionTintStrength);
        }

        public BuildingRuntimeCitySpawnBridgeCompositionSystemHelper.Context CreateCitySpawnContext(
            Source source,
            BuildingRuntimeSpawnCommandSystemHelper runtimeSpawnCommandBoundary,
            BuildingRuntimeSpawnCommandSystemHelper.Context runtimeSpawnCommandContext,
            BuildingRuntimeProcessingCompositionSystemHelper runtimeBoundarySystem)
        {
            return new BuildingRuntimeCitySpawnBridgeCompositionSystemHelper.Context(
                runtimeSpawnCommandBoundary,
                runtimeSpawnCommandContext,
                source.DefinitionSystem,
                runtimeBoundarySystem,
                source.TryGetEntityManager,
                source.DeleteBuildingById,
                source.BeginDeferredRuntimeBuildingSideEffects,
                source.EndDeferredRuntimeBuildingSideEffects);
        }

        public BuildingSpawnCompositionSystemHelper.Context CreateBuildingSpawnContext(RuntimeSource source)
        {
            return new BuildingSpawnCompositionSystemHelper.Context(
                source.RuntimeBuildingSystem.Buildings,
                source.LiveUnitFootprintQuery,
                source.ProductionSystem,
                source.SpawnPrefabSystem,
                source.SpawnPrefabContext,
                source.ProductionSlotSystem,
                BuildingDefinitionPrefabSystemHelper.RuntimeBuildingMatchesId,
                BuildingDefinitionPrefabSystemHelper.TryGetProductionSourceKey,
                (EntityManager em, out Entity boundaryEntity) =>
                    TryGetRuntimeBoundaryEntity(source.BuildingRuntimeStateQuery, em, out boundaryEntity));
        }

        private static bool TryGetRuntimeBoundaryEntity(EntityQuery boundaryQuery, EntityManager em, out Entity boundaryEntity)
        {
            boundaryEntity = Entity.Null;
            if (em.World == null || !em.World.IsCreated || boundaryQuery.IsEmptyIgnoreFilter)
                return false;

            boundaryEntity = boundaryQuery.GetSingletonEntity();
            return boundaryEntity != Entity.Null && em.Exists(boundaryEntity);
        }

        public BuildingRuntimeEntityCompositionSystemHelper.Context CreateRuntimeEntityContext(
            RuntimeSource source,
            BuildingCombatUtilitySystemHelper combatSystem,
            BuildingCombatUtilitySystemHelper.Context<RuntimeBuildingEntity> combatContext,
            Func<float> getTime,
            float destroyedBuildingLifetimeSeconds)
        {
            return new BuildingRuntimeEntityCompositionSystemHelper.Context(
                source.TryGetEntityManager,
                source.TryGetGridData,
                source.GetFootprintCenter,
                combatSystem,
                combatContext,
                getTime,
                destroyedBuildingLifetimeSeconds);
        }

        public BuildingRuntimeVisualPresentationSystemHelper.Context CreateRuntimeVisualContext(RuntimeSource source)
        {
            return new BuildingRuntimeVisualPresentationSystemHelper.Context(
                source.RuntimeBuildingSystem.Buildings,
                source.BuildingVisualSystem,
                source.BuildingFactionVisualSystem,
                source.BarrierSystem,
                source.FactionVisualSettings,
                source.MarkerPropertyBlock,
                source.BuildingFactionTintStrength);
        }

        public BuildingSelectionMarkerPresentationSystemHelper.Context CreateSelectionMarkerContext(
            RuntimeSource source,
            GameObject markerPrefab,
            Transform markerParent,
            MaterialPropertyBlock markerPropertyBlock,
            BuildingSelectionMarkerPresentationSystemHelper.DestroyObjectDelegate destroyObject)
        {
            return new BuildingSelectionMarkerPresentationSystemHelper.Context(
                source.RuntimeBuildingSystem,
                source.RuntimeBuildingSystem.Buildings,
                (out GridConfig grid) => source.TryGetGridData(out _, out grid, out _, out _),
                (originCell, footprintCells, grid) => source.GetFootprintCenter(originCell, footprintCells, grid),
                markerPrefab,
                markerParent,
                source.BuildingVisualSystem,
                source.FactionVisualSettings,
                markerPropertyBlock ?? source.MarkerPropertyBlock,
                destroyObject);
        }

        public BuildingPlacementRedirectCompositionSystemHelper.Context CreateRedirectContext(RuntimeSource source)
        {
            return new BuildingPlacementRedirectCompositionSystemHelper.Context(
                (out EntityManager entityManager) => source.TryGetEntityManager(out entityManager),
                (out Entity gridEntity, out GridConfig grid, out DynamicBuffer<GridRoad> roads, out DynamicBlockerComponent blockerData) =>
                    source.TryGetGridData(out gridEntity, out grid, out roads, out blockerData),
                entityManager => source.EnsureEntityQueries?.Invoke(entityManager),
                () => source.RedirectUnitsQuery);
        }

        public BuildingCombatUtilitySystemHelper.Context<RuntimeBuildingEntity> CreateCombatContext(RuntimeSource source)
        {
            return new BuildingCombatUtilitySystemHelper.Context<RuntimeBuildingEntity>(
                source.RuntimeBuildingSystem,
                source.RuntimeBuildingSystem.Buildings,
                (out EntityManager entityManager) => source.TryGetEntityManager(out entityManager),
                source.RememberOpenBaseBreach,
                source.NotifyHomeBuildingDestroyed,
                source.BuildingDestroyedVisualPresentationSystemHelper,
                new BuildingDestroyedVisualPresentationSystemHelper.Context(
                    source.BuildingVisualSystem,
                    source.DestroyObject),
                source.DestroyObject,
                source.RefreshBuildingMarkerVisibility,
                source.NotifyStaticMinimapChanged,
                source.Log,
                source.EnableDestroyDiagnostics);
        }

        public BuildingRuntimeReadModelCompositionSystemHelper.Context CreateRuntimeQueryContext(RuntimeSource source)
        {
            return new BuildingRuntimeReadModelCompositionSystemHelper.Context(
                source.RuntimeBuildingSystem.Buildings,
                (out EntityManager entityManager) => source.TryGetEntityManager(out entityManager),
                (EntityManager em, out Entity boundaryEntity) =>
                    TryGetRuntimeBoundaryEntity(source.BuildingRuntimeStateQuery, em, out boundaryEntity),
                source.ProductionSystem,
                BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey,
                source.IsHouseBuilding,
                BuildingDefinitionPrefabSystemHelper.RuntimeBuildingMatchesId,
                BuildingDefinitionPrefabSystemHelper.UnitPrefabMatchesId,
                source.TryResolveBuildingFocusWorldPosition,
                (RuntimeBuildingEntity building, int2 unitFootprint, int2 referenceCell, out int2 goal) =>
                {
                    goal = default;
                    return source.ResourceHaulerBridgeSystem != null &&
                        source.ResourceHaulerBridgeSystem.TryGetRuntimeBuildingApproachCell(
                        CreateResourceHaulerBridgeContext(source),
                        building,
                        unitFootprint,
                        referenceCell,
                        out goal);
                },
                (RuntimeBuildingEntity building, int2 currentCell, int2 unitFootprint) =>
                    source.ResourceHaulerBridgeSystem != null &&
                    source.ResourceHaulerBridgeSystem.IsRuntimeBuildingApproachCell(
                        CreateResourceHaulerBridgeContext(source),
                        building,
                        currentCell,
                        unitFootprint),
                source.BarrierSystem.IsWallGateDefinitionCached,
                (byte attackerFactionId,
                    Entity finalTarget,
                    int2 finalTargetCell,
                    int2 attackerCell,
                    out Entity breachTarget,
                    out int2 breachCell,
                    out float3 breachPosition,
                    out string reason) =>
                    source.BarrierSystem.TryResolveBaseBreachTarget(
                        CreateBarrierContext(source),
                        attackerFactionId,
                        finalTarget,
                        finalTargetCell,
                        attackerCell,
                        out breachTarget,
                        out breachCell,
                        out breachPosition,
                        out reason));
        }

        public BuildingBarrierUtilitySystemHelper.Context CreateBarrierContext(RuntimeSource source)
        {
            return new BuildingBarrierUtilitySystemHelper.Context(
                source.RuntimeBuildingSystem.Buildings,
                (out EntityManager entityManager) => source.TryGetEntityManager(out entityManager),
                (out Entity gridEntity, out GridConfig grid, out DynamicBuffer<GridRoad> roads, out DynamicBlockerComponent blockerData) =>
                    source.TryGetGridData(out gridEntity, out grid, out roads, out blockerData),
                entityManager => source.EnsureEntityQueries?.Invoke(entityManager),
                () => source.LiveFactionUnitsQuery,
                source.BarrierSystem.IsWallGateDefinitionCached,
                (RuntimeBuildingEntity building, int2 unitFootprint, int2 referenceCell, out int2 goal) =>
                {
                    goal = default;
                    return source.ResourceHaulerBridgeSystem != null &&
                        source.ResourceHaulerBridgeSystem.TryGetRuntimeBuildingApproachCell(
                        CreateResourceHaulerBridgeContext(source),
                        building,
                        unitFootprint,
                        referenceCell,
                        out goal);
                });
        }

        private static BuildingPlacementRedirectCompositionSystemHelper.Context CreateCreationRedirectContext(Source source)
        {
            return new BuildingPlacementRedirectCompositionSystemHelper.Context(
                (out EntityManager entityManager) => source.TryGetEntityManager(out entityManager),
                (out Entity gridEntity, out GridConfig grid, out DynamicBuffer<GridRoad> roads, out DynamicBlockerComponent blockerData) =>
                    source.TryGetGridData(out gridEntity, out grid, out roads, out blockerData),
                entityManager => source.EnsureEntityQueries?.Invoke(entityManager),
                source.GetRedirectUnitsQuery);
        }

        public BuildingResourceHaulerBridgeCompositionSystemHelper.Context CreateResourceHaulerBridgeContext(RuntimeSource source)
        {
            return new BuildingResourceHaulerBridgeCompositionSystemHelper.Context(
                source.RuntimeBuildingSystem.Buildings,
                source.ResourceHaulerUtilitySystemHelper,
                source.FactionResourceCompositionSystemHelper,
                (out EntityManager entityManager) => source.TryGetEntityManager(out entityManager),
                (out Entity gridEntity, out GridConfig grid, out DynamicBuffer<GridRoad> roads, out DynamicBlockerComponent blockerData) =>
                    source.TryGetGridData(out gridEntity, out grid, out roads, out blockerData),
                entityManager => source.EnsureEntityQueries?.Invoke(entityManager),
                () => source.HaulerUnitsQuery,
                () => source.SelectedUnitsQuery,
                source.TryGetRuntimeBuilding,
                building => source.TryResolveBuildingFocusWorldPosition(building, out Vector3 worldPosition) ? worldPosition : Vector3.zero,
                source.GetEffectivePlacementRect);
        }

        public bool TryAssignSelectedHaulerOrders(RuntimeSource source, int clickedBuildingId)
        {
            return source.ResourceHaulerBridgeSystem != null &&
                source.ResourceHaulerBridgeSystem.TryAssignSelectedHaulerOrders(
                    CreateResourceHaulerBridgeContext(source),
                    clickedBuildingId);
        }
    }
}
