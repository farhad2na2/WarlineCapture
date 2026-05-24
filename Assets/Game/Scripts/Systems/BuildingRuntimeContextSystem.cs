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
        public readonly BuildingProductionContextSystem ProductionContextSystem;
        public readonly BuildingProductionContextSystem.Source ProductionContextSource;
        public readonly FactionVisualSettings FactionVisualSettings;
        public readonly MaterialPropertyBlock MarkerPropertyBlock;
        public readonly EntityQuery LiveUnitFootprintQuery;
        public readonly EntityQuery RedirectUnitsQuery;
        public readonly EntityQuery LiveFactionUnitsQuery;
        public readonly Func<int?> GetActiveBuildingId;
        public readonly BuildingRuntimeEntitySystem.TryGetEntityManagerDelegate TryGetEntityManager;
        public readonly BuildingRuntimeEntitySystem.TryGetGridDataDelegate TryGetGridData;
        public readonly Action<EntityManager> EnsureEntityQueries;
        public readonly BuildingRuntimeEntitySystem.GetFootprintCenterDelegate GetFootprintCenter;
        public readonly BuildingRuntimeQuerySystem.BuildingPredicate IsHouseBuilding;
        public readonly BuildingRuntimeQuerySystem.TryResolveBuildingWorldPositionDelegate TryResolveBuildingFocusWorldPosition;
        public readonly BuildingRuntimeQuerySystem.TryGetBuildingApproachCellDelegate TryGetBuildingApproachCell;
        public readonly BuildingRuntimeQuerySystem.IsBuildingApproachCellDelegate IsBuildingApproachCell;
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
            BuildingProductionContextSystem productionContextSystem,
            BuildingProductionContextSystem.Source productionContextSource,
            FactionVisualSettings factionVisualSettings,
            MaterialPropertyBlock markerPropertyBlock,
            EntityQuery liveUnitFootprintQuery,
            EntityQuery redirectUnitsQuery,
            EntityQuery liveFactionUnitsQuery,
            Func<int?> getActiveBuildingId,
            BuildingRuntimeEntitySystem.TryGetEntityManagerDelegate tryGetEntityManager,
            BuildingRuntimeEntitySystem.TryGetGridDataDelegate tryGetGridData,
            Action<EntityManager> ensureEntityQueries,
            BuildingRuntimeEntitySystem.GetFootprintCenterDelegate getFootprintCenter,
            BuildingRuntimeQuerySystem.BuildingPredicate isHouseBuilding,
            BuildingRuntimeQuerySystem.TryResolveBuildingWorldPositionDelegate tryResolveBuildingFocusWorldPosition,
            BuildingRuntimeQuerySystem.TryGetBuildingApproachCellDelegate tryGetBuildingApproachCell,
            BuildingRuntimeQuerySystem.IsBuildingApproachCellDelegate isBuildingApproachCell,
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
            ProductionContextSystem = productionContextSystem;
            ProductionContextSource = productionContextSource;
            FactionVisualSettings = factionVisualSettings;
            MarkerPropertyBlock = markerPropertyBlock;
            LiveUnitFootprintQuery = liveUnitFootprintQuery;
            RedirectUnitsQuery = redirectUnitsQuery;
            LiveFactionUnitsQuery = liveFactionUnitsQuery;
            GetActiveBuildingId = getActiveBuildingId;
            TryGetEntityManager = tryGetEntityManager;
            TryGetGridData = tryGetGridData;
            EnsureEntityQueries = ensureEntityQueries;
            GetFootprintCenter = getFootprintCenter;
            IsHouseBuilding = isHouseBuilding;
            TryResolveBuildingFocusWorldPosition = tryResolveBuildingFocusWorldPosition;
            TryGetBuildingApproachCell = tryGetBuildingApproachCell;
            IsBuildingApproachCell = isBuildingApproachCell;
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
        public readonly BuildingRuntimeCreationSystem.ShouldBlockPathingDelegate ShouldBlockPathing;
        public readonly BuildingRuntimeCreationSystem.RemoveOverlappingBlockersDelegate RemoveOverlappingBlockers;
        public readonly BuildingRuntimeCreationSystem.CreateBlockerEntityDelegate CreateBlockerEntity;
        public readonly BuildingRuntimeCreationSystem.CreateCombatEntityDelegate CreateCombatEntity;
        public readonly BuildingRuntimeCreationSystem.RedirectUnitsDelegate RedirectUnits;
        public readonly BuildingRuntimeCreationSystem.AddDeferredRedirectFootprintDelegate AddDeferredRedirectFootprint;
        public readonly BuildingRuntimeCreationSystem.RuntimeAction MarkPendingMarkerRefresh;
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
            BuildingRuntimeCreationSystem.ShouldBlockPathingDelegate shouldBlockPathing,
            BuildingRuntimeCreationSystem.RemoveOverlappingBlockersDelegate removeOverlappingBlockers,
            BuildingRuntimeCreationSystem.CreateBlockerEntityDelegate createBlockerEntity,
            BuildingRuntimeCreationSystem.CreateCombatEntityDelegate createCombatEntity,
            BuildingRuntimeCreationSystem.RedirectUnitsDelegate redirectUnits,
            BuildingRuntimeCreationSystem.AddDeferredRedirectFootprintDelegate addDeferredRedirectFootprint,
            BuildingRuntimeCreationSystem.RuntimeAction markPendingMarkerRefresh,
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
            ShouldBlockPathing = shouldBlockPathing;
            RemoveOverlappingBlockers = removeOverlappingBlockers;
            CreateBlockerEntity = createBlockerEntity;
            CreateCombatEntity = createCombatEntity;
            RedirectUnits = redirectUnits;
            AddDeferredRedirectFootprint = addDeferredRedirectFootprint;
            MarkPendingMarkerRefresh = markPendingMarkerRefresh;
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

    public BuildingRuntimeCreationSystem.Context CreateCreationContext(Source source)
    {
        return new BuildingRuntimeCreationSystem.Context(
            source.RuntimeBuildingSystem,
            source.RuntimeLinkInteractionSystem,
            source.RuntimeLinkInteractionContext,
            source.IsDeferringSideEffects?.Invoke() == true,
            source.TryGetGridForRuntimeCreation,
            source.ResolvePlacementRect,
            source.ShouldBlockPathing,
            source.RemoveOverlappingBlockers,
            source.CreateBlockerEntity,
            source.CreateCombatEntity,
            source.RedirectUnits,
            source.AddDeferredRedirectFootprint,
            source.MarkPendingMarkerRefresh,
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

    public BuildingRuntimeCitySpawnSystem.Context CreateCitySpawnContext(Source source)
    {
        return new BuildingRuntimeCitySpawnSystem.Context(
            CreateSpawnContext(source),
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

    public BuildingRuntimeEntitySystem.Context CreateRuntimeEntityContext(RuntimeSource source)
    {
        return new BuildingRuntimeEntitySystem.Context(
            source.TryGetEntityManager,
            source.TryGetGridData,
            source.GetFootprintCenter);
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
            source.TryGetBuildingApproachCell,
            source.IsBuildingApproachCell,
            BuildingBarrierSystem.IsWallGateDefinition);
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
                source.TryGetBuildingApproachCell(building, unitFootprint, referenceCell, out goal));
    }
}
