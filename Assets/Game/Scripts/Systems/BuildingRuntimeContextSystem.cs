using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

internal sealed class BuildingRuntimeContextSystem
{
    public readonly struct RuntimeSource
    {
        public readonly RuntimeBuildingCollection<RuntimeBuildingEntity> RuntimeBuildingSystem;
        public readonly BuildingProductionSystem ProductionSystem;
        public readonly BuildingProductionSlotSystem ProductionSlotSystem;
        public readonly BuildingSpawnPrefabSystem SpawnPrefabSystem;
        public readonly BuildingSpawnPrefabSystem.Context SpawnPrefabContext;
        public readonly BuildingVisualSystem BuildingVisualSystem;
        public readonly BuildingRuntimeVisualPresentationSystemHelper RuntimeVisualSystem;
        public readonly BuildingFactionVisualSystem BuildingFactionVisualSystem;
        public readonly BuildingDestroyedVisualPresentationSystemHelper BuildingDestroyedVisualPresentationSystemHelper;
        public readonly BuildingBarrierUtilitySystemHelper BarrierSystem;
        public readonly BuildingResourceHaulerBridgeSystem ResourceHaulerBridgeSystem;
        public readonly ResourceHaulerSystem ResourceHaulerSystem;
        public readonly FactionResourceSystem FactionResourceSystem;
        public readonly BuildingProductionContextCompositionSystemHelper ProductionContextSystem;
        public readonly FactionVisualSettings FactionVisualSettings;
        public readonly MaterialPropertyBlock MarkerPropertyBlock;
        public readonly float BuildingFactionTintStrength;
        public readonly EntityQuery LiveUnitFootprintQuery;
        public readonly EntityQuery RedirectUnitsQuery;
        public readonly EntityQuery HaulerUnitsQuery;
        public readonly EntityQuery SelectedUnitsQuery;
        public readonly EntityQuery LiveFactionUnitsQuery;
        public readonly EntityQuery BuildingRuntimeBoundaryQuery;
        public readonly Func<int?> GetActiveBuildingId;
        public readonly BuildingRuntimeEntitySystem.TryGetEntityManagerDelegate TryGetEntityManager;
        public readonly BuildingRuntimeEntitySystem.TryGetGridDataDelegate TryGetGridData;
        public readonly Action<EntityManager> EnsureEntityQueries;
        public readonly BuildingRuntimeEntitySystem.GetFootprintCenterDelegate GetFootprintCenter;
        public readonly BuildingRuntimeQuerySystem.BuildingPredicate IsHouseBuilding;
        public readonly BuildingRuntimeQuerySystem.TryResolveBuildingWorldPositionDelegate TryResolveBuildingFocusWorldPosition;
        public readonly BuildingResourceHaulerBridgeSystem.TryGetRuntimeBuildingDelegate TryGetRuntimeBuilding;
        public readonly BuildingResourceHaulerBridgeSystem.GetEffectivePlacementRectDelegate GetEffectivePlacementRect;
        public readonly BuildingCombatUtilitySystemHelper.BuildingAction<RuntimeBuildingEntity> RememberOpenBaseBreach;
        public readonly BuildingCombatUtilitySystemHelper.BuildingIdAction NotifyHomeBuildingDestroyed;
        public readonly BuildingCombatUtilitySystemHelper.ObjectAction DestroyObject;
        public readonly Action RefreshBuildingMarkerVisibility;
        public readonly Action NotifyStaticMinimapChanged;
        public readonly BuildingCombatUtilitySystemHelper.LogAction Log;
        public readonly bool EnableDestroyDiagnostics;

        public RuntimeSource(
            RuntimeBuildingCollection<RuntimeBuildingEntity> runtimeBuildingSystem,
            BuildingProductionSystem productionSystem,
            BuildingProductionSlotSystem productionSlotSystem,
            BuildingSpawnPrefabSystem spawnPrefabSystem,
            BuildingSpawnPrefabSystem.Context spawnPrefabContext,
            BuildingVisualSystem buildingVisualSystem,
            BuildingRuntimeVisualPresentationSystemHelper runtimeVisualSystem,
            BuildingFactionVisualSystem buildingFactionVisualSystem,
            BuildingDestroyedVisualPresentationSystemHelper buildingDestroyedVisualPresentationHelper,
            BuildingBarrierUtilitySystemHelper barrierSystem,
            BuildingResourceHaulerBridgeSystem resourceHaulerBridgeSystem,
            ResourceHaulerSystem resourceHaulerSystem,
            FactionResourceSystem factionResourceSystem,
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
            BuildingRuntimeEntitySystem.TryGetEntityManagerDelegate tryGetEntityManager,
            BuildingRuntimeEntitySystem.TryGetGridDataDelegate tryGetGridData,
            Action<EntityManager> ensureEntityQueries,
            BuildingRuntimeEntitySystem.GetFootprintCenterDelegate getFootprintCenter,
            BuildingRuntimeQuerySystem.BuildingPredicate isHouseBuilding,
            BuildingRuntimeQuerySystem.TryResolveBuildingWorldPositionDelegate tryResolveBuildingFocusWorldPosition,
            BuildingResourceHaulerBridgeSystem.TryGetRuntimeBuildingDelegate tryGetRuntimeBuilding,
            BuildingResourceHaulerBridgeSystem.GetEffectivePlacementRectDelegate getEffectivePlacementRect,
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
            ResourceHaulerSystem = resourceHaulerSystem;
            FactionResourceSystem = factionResourceSystem;
            ProductionContextSystem = productionContextSystem;
            FactionVisualSettings = factionVisualSettings;
            MarkerPropertyBlock = markerPropertyBlock;
            BuildingFactionTintStrength = Mathf.Clamp01(buildingFactionTintStrength);
            LiveUnitFootprintQuery = liveUnitFootprintQuery;
            RedirectUnitsQuery = redirectUnitsQuery;
            HaulerUnitsQuery = haulerUnitsQuery;
            SelectedUnitsQuery = selectedUnitsQuery;
            LiveFactionUnitsQuery = liveFactionUnitsQuery;
            BuildingRuntimeBoundaryQuery = buildingRuntimeBoundaryQuery;
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
        public readonly BuildingPlacementValidationSystem PlacementValidationSystem;
        public readonly BuildingPlacementValidationSystem.WallValidationContext WallValidationContext;
        public readonly BuildingRuntimeSpawnSystem.TryGetGridDataDelegate TryGetGridData;
        public readonly BuildingRuntimeSpawnSystem.GetPlacementFootprintDelegate GetPlacementFootprint;
        public readonly BuildingRuntimeSpawnSystem.GetEffectivePlacementRectDelegate GetEffectivePlacementRect;
        public readonly BuildingRuntimeSpawnSystem.IsPlacementValidDelegate IsPlacementValid;
        public readonly BuildingRuntimeSpawnSystem.HasCachedInvalidCellInFootprintDelegate HasCachedInvalidCellInFootprint;
        public readonly BuildingRuntimeSpawnSystem.CreateBuildingVisualInstanceDelegate CreateBuildingVisualInstance;
        public readonly BuildingRuntimeSpawnSystem.PositionBuildingObjectDelegate PositionBuildingObject;
        public readonly BuildingRuntimeSpawnSystem.RegisterRuntimeBuildingDelegate RegisterRuntimeBuilding;
        public readonly BuildingRuntimeSpawnSystem.SetRuntimeBuildingOwnerFactionDelegate SetRuntimeBuildingOwnerFaction;
        public readonly RuntimeBuildingCollection<RuntimeBuildingEntity> RuntimeBuildingSystem;
        public readonly BuildingPlacementInteractionBoundaryCompositionSystemHelper RuntimeLinkInteractionSystem;
        public readonly BuildingPlacementInteractionBoundaryCompositionSystemHelper.Context RuntimeLinkInteractionContext;
        public readonly RuntimeBuildingEntityLinkRegistry RuntimeBuildingEntityLinks;
        public readonly Func<bool> IsDeferringSideEffects;
        public readonly BuildingRuntimeCreationSystem.TryGetGridDelegate TryGetGridForRuntimeCreation;
        public readonly BuildingRuntimeCreationSystem.ResolvePlacementRectDelegate ResolvePlacementRect;
        public readonly BuildingRuntimeCreationSystem.RemoveOverlappingBlockersDelegate RemoveOverlappingBlockers;
        public readonly BuildingRuntimeEntitySystem RuntimeEntitySystem;
        public readonly BuildingRuntimeEntitySystem.Context RuntimeEntityContext;
        public readonly BuildingPlacementRedirectCompositionSystemHelper PlacementRedirectSystem;
        public readonly BuildingPlacementRedirectCompositionSystemHelper.EnsureEntityQueriesDelegate EnsureEntityQueries;
        public readonly BuildingPlacementRedirectCompositionSystemHelper.GetRedirectUnitsQueryDelegate GetRedirectUnitsQuery;
        public readonly BuildingRuntimeCreationSystem.RuntimeBuildingAction InitializeVisuals;
        public readonly BuildingRuntimeCreationSystem.RuntimeAction RefreshMarkers;
        public readonly BuildingRuntimeOwnershipSystem.TryGetEntityManagerDelegate TryGetEntityManager;
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
            BuildingPlacementValidationSystem placementValidationSystem,
            BuildingPlacementValidationSystem.WallValidationContext wallValidationContext,
            BuildingRuntimeSpawnSystem.TryGetGridDataDelegate tryGetGridData,
            BuildingRuntimeSpawnSystem.GetPlacementFootprintDelegate getPlacementFootprint,
            BuildingRuntimeSpawnSystem.GetEffectivePlacementRectDelegate getEffectivePlacementRect,
            BuildingRuntimeSpawnSystem.IsPlacementValidDelegate isPlacementValid,
            BuildingRuntimeSpawnSystem.HasCachedInvalidCellInFootprintDelegate hasCachedInvalidCellInFootprint,
            BuildingRuntimeSpawnSystem.CreateBuildingVisualInstanceDelegate createBuildingVisualInstance,
            BuildingRuntimeSpawnSystem.PositionBuildingObjectDelegate positionBuildingObject,
            BuildingRuntimeSpawnSystem.RegisterRuntimeBuildingDelegate registerRuntimeBuilding,
            BuildingRuntimeSpawnSystem.SetRuntimeBuildingOwnerFactionDelegate setRuntimeBuildingOwnerFaction,
            RuntimeBuildingCollection<RuntimeBuildingEntity> runtimeBuildingSystem,
            BuildingPlacementInteractionBoundaryCompositionSystemHelper runtimeLinkInteractionSystem,
            BuildingPlacementInteractionBoundaryCompositionSystemHelper.Context runtimeLinkInteractionContext,
            RuntimeBuildingEntityLinkRegistry runtimeBuildingEntityLinks,
            Func<bool> isDeferringSideEffects,
            BuildingRuntimeCreationSystem.TryGetGridDelegate tryGetGridForRuntimeCreation,
            BuildingRuntimeCreationSystem.ResolvePlacementRectDelegate resolvePlacementRect,
            BuildingRuntimeCreationSystem.RemoveOverlappingBlockersDelegate removeOverlappingBlockers,
            BuildingRuntimeEntitySystem runtimeEntitySystem,
            BuildingRuntimeEntitySystem.Context runtimeEntityContext,
            BuildingPlacementRedirectCompositionSystemHelper placementRedirectSystem,
            BuildingPlacementRedirectCompositionSystemHelper.EnsureEntityQueriesDelegate ensureEntityQueries,
            BuildingPlacementRedirectCompositionSystemHelper.GetRedirectUnitsQueryDelegate getRedirectUnitsQuery,
            BuildingRuntimeCreationSystem.RuntimeBuildingAction initializeVisuals,
            BuildingRuntimeCreationSystem.RuntimeAction refreshMarkers,
            BuildingRuntimeOwnershipSystem.TryGetEntityManagerDelegate tryGetEntityManager,
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

    public BuildingRuntimeSpawnSystem.Context CreateSpawnContext(Source source)
    {
        return new BuildingRuntimeSpawnSystem.Context(
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

    public BuildingRuntimeSpawnCommandBoundary.Context CreateSpawnCommandContext(
        Source source,
        BuildingRuntimeSpawnSystem runtimeSpawnSystem)
    {
        return new BuildingRuntimeSpawnCommandBoundary.Context(
            runtimeSpawnSystem,
            CreateSpawnContext(source));
    }

    public BuildingRuntimeCreationSystem.Context CreateCreationContext(Source source)
    {
        return new BuildingRuntimeCreationSystem.Context(
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

    public BuildingRuntimeOwnershipSystem.Context CreateOwnershipContext(Source source)
    {
        return new BuildingRuntimeOwnershipSystem.Context(
            source.TryGetEntityManager,
            source.FactionVisualSettings,
            source.MarkerPropertyBlock,
            source.BuildingFactionVisualSystem,
            source.BuildingFactionTintStrength);
    }

    public BuildingRuntimeCitySpawnSystem.Context CreateCitySpawnContext(
        Source source,
        BuildingRuntimeSpawnCommandBoundary runtimeSpawnCommandBoundary,
        BuildingRuntimeSpawnCommandBoundary.Context runtimeSpawnCommandContext,
        BuildingRuntimeBoundarySystem runtimeBoundarySystem)
    {
        return new BuildingRuntimeCitySpawnSystem.Context(
            runtimeSpawnCommandBoundary,
            runtimeSpawnCommandContext,
            source.DefinitionSystem,
            runtimeBoundarySystem,
            source.TryGetEntityManager,
            source.DeleteBuildingById,
            source.BeginDeferredRuntimeBuildingSideEffects,
            source.EndDeferredRuntimeBuildingSideEffects);
    }

    public BuildingSpawnSystem.Context CreateBuildingSpawnContext(RuntimeSource source)
    {
        return new BuildingSpawnSystem.Context(
            source.RuntimeBuildingSystem.Buildings,
            source.LiveUnitFootprintQuery,
            source.ProductionSystem,
            source.SpawnPrefabSystem,
            source.SpawnPrefabContext,
            source.ProductionSlotSystem,
            BuildingDefinitionPrefabSystemHelper.RuntimeBuildingMatchesId,
            BuildingDefinitionPrefabSystemHelper.TryGetProductionSourceKey,
            (EntityManager em, out Entity boundaryEntity) =>
                TryGetRuntimeBoundaryEntity(source.BuildingRuntimeBoundaryQuery, em, out boundaryEntity));
    }

    private static bool TryGetRuntimeBoundaryEntity(EntityQuery boundaryQuery, EntityManager em, out Entity boundaryEntity)
    {
        boundaryEntity = Entity.Null;
        if (em.World == null || !em.World.IsCreated || boundaryQuery.IsEmptyIgnoreFilter)
            return false;

        boundaryEntity = boundaryQuery.GetSingletonEntity();
        return boundaryEntity != Entity.Null && em.Exists(boundaryEntity);
    }

    public BuildingRuntimeEntitySystem.Context CreateRuntimeEntityContext(
        RuntimeSource source,
        BuildingCombatUtilitySystemHelper combatSystem,
        BuildingCombatUtilitySystemHelper.Context<RuntimeBuildingEntity> combatContext,
        Func<float> getTime,
        float destroyedBuildingLifetimeSeconds)
    {
        return new BuildingRuntimeEntitySystem.Context(
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

    public BuildingSelectionMarkerSystem.Context CreateSelectionMarkerContext(
        RuntimeSource source,
        GameObject markerPrefab,
        Transform markerParent,
        MaterialPropertyBlock markerPropertyBlock,
        BuildingSelectionMarkerSystem.DestroyObjectDelegate destroyObject)
    {
        return new BuildingSelectionMarkerSystem.Context(
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

    public BuildingRuntimeQuerySystem.Context CreateRuntimeQueryContext(RuntimeSource source)
    {
        return new BuildingRuntimeQuerySystem.Context(
            source.RuntimeBuildingSystem.Buildings,
            (out EntityManager entityManager) => source.TryGetEntityManager(out entityManager),
            (EntityManager em, out Entity boundaryEntity) =>
                TryGetRuntimeBoundaryEntity(source.BuildingRuntimeBoundaryQuery, em, out boundaryEntity),
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

    public BuildingResourceHaulerBridgeSystem.Context CreateResourceHaulerBridgeContext(RuntimeSource source)
    {
        return new BuildingResourceHaulerBridgeSystem.Context(
            source.RuntimeBuildingSystem.Buildings,
            source.ResourceHaulerSystem,
            source.FactionResourceSystem,
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
