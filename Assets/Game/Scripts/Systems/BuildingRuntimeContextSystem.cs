using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

internal sealed class BuildingRuntimeContextSystem
{
    public readonly struct RuntimeSource
    {
        public readonly RuntimeBuildingSystem<RuntimeBuildingData> RuntimeBuildingSystem;
        public readonly BuildingProductionSystem ProductionSystem;
        public readonly BuildingProductionSlotSystem ProductionSlotSystem;
        public readonly BuildingSpawnPrefabSystem SpawnPrefabSystem;
        public readonly BuildingSpawnPrefabSystem.Context SpawnPrefabContext;
        public readonly BuildingVisualSystem BuildingVisualSystem;
        public readonly BuildingRuntimeVisualSystem RuntimeVisualSystem;
        public readonly BuildingBarrierSystem BarrierSystem;
        public readonly BuildingResourceHaulerBridgeSystem ResourceHaulerBridgeSystem;
        public readonly ResourceHaulerSystem ResourceHaulerSystem;
        public readonly FactionResourceSystem FactionResourceSystem;
        public readonly BuildingProductionContextSystem ProductionContextSystem;
        public readonly FactionVisualSettings FactionVisualSettings;
        public readonly MaterialPropertyBlock MarkerPropertyBlock;
        public readonly EntityQuery LiveUnitFootprintQuery;
        public readonly EntityQuery RedirectUnitsQuery;
        public readonly EntityQuery HaulerUnitsQuery;
        public readonly EntityQuery SelectedUnitsQuery;
        public readonly EntityQuery LiveFactionUnitsQuery;
        public readonly Func<int?> GetActiveBuildingId;
        public readonly BuildingRuntimeEntitySystem.TryGetEntityManagerDelegate TryGetEntityManager;
        public readonly BuildingRuntimeEntitySystem.TryGetGridDataDelegate TryGetGridData;
        public readonly Action<EntityManager> EnsureEntityQueries;
        public readonly BuildingRuntimeEntitySystem.GetFootprintCenterDelegate GetFootprintCenter;
        public readonly BuildingRuntimeQuerySystem.BuildingPredicate IsHouseBuilding;
        public readonly BuildingRuntimeQuerySystem.TryResolveBuildingWorldPositionDelegate TryResolveBuildingFocusWorldPosition;
        public readonly BuildingResourceHaulerBridgeSystem.TryGetRuntimeBuildingDelegate TryGetRuntimeBuilding;
        public readonly BuildingResourceHaulerBridgeSystem.GetEffectivePlacementRectDelegate GetEffectivePlacementRect;
        public readonly BuildingCombatSystem.BuildingAction<RuntimeBuildingData> RememberOpenBaseBreach;
        public readonly BuildingCombatSystem.BuildingIdAction NotifyHomeBuildingDestroyed;
        public readonly BuildingCombatSystem.ObjectAction DestroyObject;
        public readonly Action RefreshBuildingMarkerVisibility;
        public readonly Action NotifyStaticMinimapChanged;
        public readonly BuildingCombatSystem.LogAction Log;
        public readonly bool EnableDestroyDiagnostics;

        public RuntimeSource(
            RuntimeBuildingSystem<RuntimeBuildingData> runtimeBuildingSystem,
            BuildingProductionSystem productionSystem,
            BuildingProductionSlotSystem productionSlotSystem,
            BuildingSpawnPrefabSystem spawnPrefabSystem,
            BuildingSpawnPrefabSystem.Context spawnPrefabContext,
            BuildingVisualSystem buildingVisualSystem,
            BuildingRuntimeVisualSystem runtimeVisualSystem,
            BuildingBarrierSystem barrierSystem,
            BuildingResourceHaulerBridgeSystem resourceHaulerBridgeSystem,
            ResourceHaulerSystem resourceHaulerSystem,
            FactionResourceSystem factionResourceSystem,
            BuildingProductionContextSystem productionContextSystem,
            FactionVisualSettings factionVisualSettings,
            MaterialPropertyBlock markerPropertyBlock,
            EntityQuery liveUnitFootprintQuery,
            EntityQuery redirectUnitsQuery,
            EntityQuery haulerUnitsQuery,
            EntityQuery selectedUnitsQuery,
            EntityQuery liveFactionUnitsQuery,
            Func<int?> getActiveBuildingId,
            BuildingRuntimeEntitySystem.TryGetEntityManagerDelegate tryGetEntityManager,
            BuildingRuntimeEntitySystem.TryGetGridDataDelegate tryGetGridData,
            Action<EntityManager> ensureEntityQueries,
            BuildingRuntimeEntitySystem.GetFootprintCenterDelegate getFootprintCenter,
            BuildingRuntimeQuerySystem.BuildingPredicate isHouseBuilding,
            BuildingRuntimeQuerySystem.TryResolveBuildingWorldPositionDelegate tryResolveBuildingFocusWorldPosition,
            BuildingResourceHaulerBridgeSystem.TryGetRuntimeBuildingDelegate tryGetRuntimeBuilding,
            BuildingResourceHaulerBridgeSystem.GetEffectivePlacementRectDelegate getEffectivePlacementRect,
            BuildingCombatSystem.BuildingAction<RuntimeBuildingData> rememberOpenBaseBreach,
            BuildingCombatSystem.BuildingIdAction notifyHomeBuildingDestroyed,
            BuildingCombatSystem.ObjectAction destroyObject,
            Action refreshBuildingMarkerVisibility,
            Action notifyStaticMinimapChanged,
            BuildingCombatSystem.LogAction log,
            bool enableDestroyDiagnostics)
        {
            RuntimeBuildingSystem = runtimeBuildingSystem;
            ProductionSystem = productionSystem;
            ProductionSlotSystem = productionSlotSystem;
            SpawnPrefabSystem = spawnPrefabSystem;
            SpawnPrefabContext = spawnPrefabContext;
            BuildingVisualSystem = buildingVisualSystem;
            RuntimeVisualSystem = runtimeVisualSystem;
            BarrierSystem = barrierSystem;
            ResourceHaulerBridgeSystem = resourceHaulerBridgeSystem;
            ResourceHaulerSystem = resourceHaulerSystem;
            FactionResourceSystem = factionResourceSystem;
            ProductionContextSystem = productionContextSystem;
            FactionVisualSettings = factionVisualSettings;
            MarkerPropertyBlock = markerPropertyBlock;
            LiveUnitFootprintQuery = liveUnitFootprintQuery;
            RedirectUnitsQuery = redirectUnitsQuery;
            HaulerUnitsQuery = haulerUnitsQuery;
            SelectedUnitsQuery = selectedUnitsQuery;
            LiveFactionUnitsQuery = liveFactionUnitsQuery;
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
        public readonly BuildingDefinitionSystem DefinitionSystem;
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
        public readonly RuntimeBuildingSystem<RuntimeBuildingData> RuntimeBuildingSystem;
        public readonly BuildingPlacementInteractionSystem RuntimeLinkInteractionSystem;
        public readonly BuildingPlacementInteractionSystem.Context RuntimeLinkInteractionContext;
        public readonly Func<bool> IsDeferringSideEffects;
        public readonly BuildingRuntimeCreationSystem.TryGetGridDelegate TryGetGridForRuntimeCreation;
        public readonly BuildingRuntimeCreationSystem.ResolvePlacementRectDelegate ResolvePlacementRect;
        public readonly BuildingRuntimeCreationSystem.RemoveOverlappingBlockersDelegate RemoveOverlappingBlockers;
        public readonly BuildingRuntimeEntitySystem RuntimeEntitySystem;
        public readonly BuildingRuntimeEntitySystem.Context RuntimeEntityContext;
        public readonly BuildingPlacementRedirectSystem PlacementRedirectSystem;
        public readonly BuildingPlacementRedirectSystem.EnsureEntityQueriesDelegate EnsureEntityQueries;
        public readonly BuildingPlacementRedirectSystem.GetRedirectUnitsQueryDelegate GetRedirectUnitsQuery;
        public readonly BuildingRuntimeCreationSystem.RuntimeBuildingAction InitializeVisuals;
        public readonly BuildingRuntimeCreationSystem.RuntimeAction RefreshMarkers;
        public readonly BuildingRuntimeOwnershipSystem.TryGetEntityManagerDelegate TryGetEntityManager;
        public readonly BuildingVisualSystem BuildingVisualSystem;
        public readonly FactionVisualSettings FactionVisualSettings;
        public readonly MaterialPropertyBlock MarkerPropertyBlock;
        public readonly Func<int, bool> DeleteBuildingById;
        public readonly Action BeginDeferredRuntimeBuildingSideEffects;
        public readonly Action EndDeferredRuntimeBuildingSideEffects;

        public Source(
            Transform buildingRoot,
            BuildingDefinitionSystem definitionSystem,
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
            RuntimeBuildingSystem<RuntimeBuildingData> runtimeBuildingSystem,
            BuildingPlacementInteractionSystem runtimeLinkInteractionSystem,
            BuildingPlacementInteractionSystem.Context runtimeLinkInteractionContext,
            Func<bool> isDeferringSideEffects,
            BuildingRuntimeCreationSystem.TryGetGridDelegate tryGetGridForRuntimeCreation,
            BuildingRuntimeCreationSystem.ResolvePlacementRectDelegate resolvePlacementRect,
            BuildingRuntimeCreationSystem.RemoveOverlappingBlockersDelegate removeOverlappingBlockers,
            BuildingRuntimeEntitySystem runtimeEntitySystem,
            BuildingRuntimeEntitySystem.Context runtimeEntityContext,
            BuildingPlacementRedirectSystem placementRedirectSystem,
            BuildingPlacementRedirectSystem.EnsureEntityQueriesDelegate ensureEntityQueries,
            BuildingPlacementRedirectSystem.GetRedirectUnitsQueryDelegate getRedirectUnitsQuery,
            BuildingRuntimeCreationSystem.RuntimeBuildingAction initializeVisuals,
            BuildingRuntimeCreationSystem.RuntimeAction refreshMarkers,
            BuildingRuntimeOwnershipSystem.TryGetEntityManagerDelegate tryGetEntityManager,
            BuildingVisualSystem buildingVisualSystem,
            FactionVisualSettings factionVisualSettings,
            MaterialPropertyBlock markerPropertyBlock,
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
            FactionVisualSettings = factionVisualSettings;
            MarkerPropertyBlock = markerPropertyBlock;
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

    public BuildingRuntimeSpawnCommandSystem.Context CreateSpawnCommandContext(
        Source source,
        BuildingRuntimeSpawnSystem runtimeSpawnSystem,
        BuildingDefinition soldierBaseDefinition,
        BuildingDefinition soldierTentDefinition,
        BuildingDefinition factoryDefinition)
    {
        return new BuildingRuntimeSpawnCommandSystem.Context(
            runtimeSpawnSystem,
            CreateSpawnContext(source),
            soldierBaseDefinition,
            soldierTentDefinition,
            factoryDefinition);
    }

    public BuildingRuntimeCreationSystem.Context CreateCreationContext(Source source)
    {
        return new BuildingRuntimeCreationSystem.Context(
            source.RuntimeBuildingSystem,
            source.RuntimeLinkInteractionSystem,
            source.RuntimeLinkInteractionContext,
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
            source.BuildingVisualSystem,
            source.FactionVisualSettings,
            source.MarkerPropertyBlock);
    }

    public BuildingRuntimeCitySpawnSystem.Context CreateCitySpawnContext(
        Source source,
        BuildingRuntimeSpawnCommandSystem runtimeSpawnCommandSystem,
        BuildingRuntimeSpawnCommandSystem.Context runtimeSpawnCommandContext)
    {
        return new BuildingRuntimeCitySpawnSystem.Context(
            runtimeSpawnCommandSystem,
            runtimeSpawnCommandContext,
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
            BuildingDefinitionSystem.GetProductionPrefab,
            BuildingDefinitionSystem.RuntimeBuildingMatchesId);
    }

    public BuildingRuntimeEntitySystem.Context CreateRuntimeEntityContext(
        RuntimeSource source,
        BuildingCombatSystem combatSystem,
        BuildingCombatSystem.Context<RuntimeBuildingData> combatContext,
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

    public BuildingRuntimeVisualSystem.Context CreateRuntimeVisualContext(RuntimeSource source)
    {
        return new BuildingRuntimeVisualSystem.Context(
            source.RuntimeBuildingSystem.Buildings,
            source.BuildingVisualSystem,
            source.BarrierSystem,
            source.FactionVisualSettings,
            source.MarkerPropertyBlock,
            source.GetActiveBuildingId);
    }

    public BuildingPlacementRedirectSystem.Context CreateRedirectContext(RuntimeSource source)
    {
        return new BuildingPlacementRedirectSystem.Context(
            (out EntityManager entityManager) => source.TryGetEntityManager(out entityManager),
            (out Entity gridEntity, out GridConfig grid, out DynamicBuffer<GridRoad> roads, out DynamicBlockerData blockerData) =>
                source.TryGetGridData(out gridEntity, out grid, out roads, out blockerData),
            entityManager => source.EnsureEntityQueries?.Invoke(entityManager),
            () => source.RedirectUnitsQuery);
    }

    public BuildingCombatSystem.Context<RuntimeBuildingData> CreateCombatContext(RuntimeSource source)
    {
        return new BuildingCombatSystem.Context<RuntimeBuildingData>(
            source.RuntimeBuildingSystem,
            source.RuntimeBuildingSystem.Buildings,
            (out EntityManager entityManager) => source.TryGetEntityManager(out entityManager),
            source.RememberOpenBaseBreach,
            source.NotifyHomeBuildingDestroyed,
            source.BuildingVisualSystem.SetTransformVisible,
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
            source.ProductionSystem,
            BuildingDefinitionSystem.NormalizeSpawnableKey,
            source.IsHouseBuilding,
            BuildingDefinitionSystem.RuntimeBuildingMatchesId,
            BuildingDefinitionSystem.UnitPrefabMatchesId,
            source.TryResolveBuildingFocusWorldPosition,
            (RuntimeBuildingData building, int2 unitFootprint, int2 referenceCell, out int2 goal) =>
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
            (RuntimeBuildingData building, int2 currentCell, int2 unitFootprint) =>
                source.ResourceHaulerBridgeSystem != null &&
                source.ResourceHaulerBridgeSystem.IsRuntimeBuildingApproachCell(
                    CreateResourceHaulerBridgeContext(source),
                    building,
                    currentCell,
                    unitFootprint),
            BuildingBarrierSystem.IsWallGateDefinition,
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

    public BuildingBarrierSystem.Context CreateBarrierContext(RuntimeSource source)
    {
        return new BuildingBarrierSystem.Context(
            source.RuntimeBuildingSystem.Buildings,
            (out EntityManager entityManager) => source.TryGetEntityManager(out entityManager),
            (out Entity gridEntity, out GridConfig grid, out DynamicBuffer<GridRoad> roads, out DynamicBlockerData blockerData) =>
                source.TryGetGridData(out gridEntity, out grid, out roads, out blockerData),
            entityManager => source.EnsureEntityQueries?.Invoke(entityManager),
            () => source.LiveFactionUnitsQuery,
            BuildingBarrierSystem.IsWallGateDefinition,
            (RuntimeBuildingData building, int2 unitFootprint, int2 referenceCell, out int2 goal) =>
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

    private static BuildingPlacementRedirectSystem.Context CreateCreationRedirectContext(Source source)
    {
        return new BuildingPlacementRedirectSystem.Context(
            (out EntityManager entityManager) => source.TryGetEntityManager(out entityManager),
            (out Entity gridEntity, out GridConfig grid, out DynamicBuffer<GridRoad> roads, out DynamicBlockerData blockerData) =>
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
            (out Entity gridEntity, out GridConfig grid, out DynamicBuffer<GridRoad> roads, out DynamicBlockerData blockerData) =>
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
