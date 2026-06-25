using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

internal sealed class BuildingProductionContextCompositionSystemHelper
{
    public readonly struct Source
    {
        public readonly IReadOnlyDictionary<int, RuntimeBuildingEntity> RuntimeBuildings;
        public readonly Camera WorldCamera;
        public readonly BuildingDefinitionPrefabSystemHelper DefinitionSystem;
        public readonly BuildingProductionQueueCompositionSystemHelper ProductionSystem;
        public readonly BuildingProductionUpdateCompositionSystemHelper ProductionUpdateSystem;
        public readonly BuildingProductionTransportPresentationSystemHelper TransportSystem;
        public readonly BuildingProductionTransportBridgeCompositionSystemHelper TransportBridgeSystem;
        public readonly BuildingProductionSlotUtilitySystemHelper ProductionSlotSystem;
        public readonly BuildingRunwaySystem RunwaySystem;
        public readonly BuildingVisualSystem VisualSystem;
        public readonly BuildingSpawnSystem SpawnSystem;
        public readonly BuildingSpawnSystem.Context SpawnContext;
        public readonly int ResourceDollars;
        public readonly BuildingProductionRequestBoundary.BeginPlacementForConfiguredSpawnableDelegate BeginPlacementForConfiguredSpawnable;
        public readonly BuildingProductionRequestBoundary.TrySpendDollarsDelegate TrySpendDollars;
        public readonly BuildingProductionRequestBoundary.RefundDollarsDelegate RefundDollars;
        public readonly BuildingProductionRequestBoundary.SetActivePlacementCostDelegate SetActivePlacementCost;
        public readonly BuildingProductionRequestBoundary.TryQueuePlayerUnitDelegate TryQueuePlayerUnit;
        public readonly BuildingProductionRequestBoundary.SelectRuntimeBuildingDelegate SelectRuntimeBuilding;
        public readonly BuildingProductionRequestBoundary.RuntimeGameplayAction SuppressNextWorldClick;
        public readonly BuildingProductionRequestBoundary.RuntimeGameplayAction RefreshBuildingMarkers;
        public readonly BuildingProductionRequestBoundary.RuntimeGameplayAction ClearFocusedUnit;
        public readonly BuildingProductionTransportBridgeCompositionSystemHelper.BooleanQuery IsBuildDrawerOpen;
        public readonly BuildingProductionRequestBoundary.CameraFocusAction SmoothMoveCameraGroundCenterTo;
        public readonly BuildingProductionRequestBoundary.ResolveBuildingFocusWorldPositionDelegate ResolveBuildingFocusWorldPosition;
        public readonly BuildingProductionRequestBoundary.RecordUnitOrderedDelegate RecordUnitOrdered;
        public readonly BuildingProductionRequestBoundary.LogWarningDelegate LogWarning;
        public readonly BuildingProductionRequestBoundary.CountFactionUnitsDelegate CountPendingProductionsForFaction;
        public readonly BuildingProductionRequestBoundary.CountFactionUnitsDelegate CountRuntimeProducedUnitsForFaction;
        public readonly ResourceHaulerSystem ResourceHaulerSystem;
        public readonly FactionResourceSystem FactionResourceSystem;
        public readonly BuildingResourceHaulerBridgeSystem.TryGetEntityManagerDelegate TryGetEntityManager;
        public readonly BuildingResourceHaulerBridgeSystem.TryGetGridDataDelegate TryGetGridData;
        public readonly BuildingResourceHaulerBridgeSystem.EnsureEntityQueriesDelegate EnsureEntityQueries;
        public readonly BuildingResourceHaulerBridgeSystem.GetEntityQueryDelegate GetHaulerUnitsQuery;
        public readonly BuildingResourceHaulerBridgeSystem.GetEntityQueryDelegate GetSelectedUnitsQuery;
        public readonly BuildingResourceHaulerBridgeSystem.TryGetRuntimeBuildingDelegate TryGetRuntimeBuilding;
        public readonly BuildingResourceHaulerBridgeSystem.GetEffectivePlacementRectDelegate GetEffectivePlacementRect;
        public readonly BuildingProductionTransportPresentationSystemHelper.PrepareTransportDropVisualDelegate PrepareTransportDropVisual;

        public Source(
            IReadOnlyDictionary<int, RuntimeBuildingEntity> runtimeBuildings,
            Camera worldCamera,
            BuildingDefinitionPrefabSystemHelper definitionSystem,
            BuildingProductionQueueCompositionSystemHelper productionSystem,
            BuildingProductionUpdateCompositionSystemHelper productionUpdateSystem,
            BuildingProductionTransportPresentationSystemHelper transportSystem,
            BuildingProductionTransportBridgeCompositionSystemHelper transportBridgeSystem,
            BuildingProductionSlotUtilitySystemHelper productionSlotSystem,
            BuildingRunwaySystem runwaySystem,
            BuildingVisualSystem visualSystem,
            BuildingSpawnSystem spawnSystem,
            BuildingSpawnSystem.Context spawnContext,
            int resourceDollars,
            BuildingProductionRequestBoundary.BeginPlacementForConfiguredSpawnableDelegate beginPlacementForConfiguredSpawnable,
            BuildingProductionRequestBoundary.TrySpendDollarsDelegate trySpendDollars,
            BuildingProductionRequestBoundary.RefundDollarsDelegate refundDollars,
            BuildingProductionRequestBoundary.SetActivePlacementCostDelegate setActivePlacementCost,
            BuildingProductionRequestBoundary.TryQueuePlayerUnitDelegate tryQueuePlayerUnit,
            BuildingProductionRequestBoundary.SelectRuntimeBuildingDelegate selectRuntimeBuilding,
            BuildingProductionRequestBoundary.RuntimeGameplayAction suppressNextWorldClick,
            BuildingProductionRequestBoundary.RuntimeGameplayAction refreshBuildingMarkers,
            BuildingProductionRequestBoundary.RuntimeGameplayAction clearFocusedUnit,
            BuildingProductionTransportBridgeCompositionSystemHelper.BooleanQuery isBuildDrawerOpen,
            BuildingProductionRequestBoundary.CameraFocusAction smoothMoveCameraGroundCenterTo,
            BuildingProductionRequestBoundary.ResolveBuildingFocusWorldPositionDelegate resolveBuildingFocusWorldPosition,
            BuildingProductionRequestBoundary.RecordUnitOrderedDelegate recordUnitOrdered,
            BuildingProductionRequestBoundary.LogWarningDelegate logWarning,
            BuildingProductionRequestBoundary.CountFactionUnitsDelegate countPendingProductionsForFaction,
            BuildingProductionRequestBoundary.CountFactionUnitsDelegate countRuntimeProducedUnitsForFaction,
            ResourceHaulerSystem resourceHaulerSystem,
            FactionResourceSystem factionResourceSystem,
            BuildingResourceHaulerBridgeSystem.TryGetEntityManagerDelegate tryGetEntityManager,
            BuildingResourceHaulerBridgeSystem.TryGetGridDataDelegate tryGetGridData,
            BuildingResourceHaulerBridgeSystem.EnsureEntityQueriesDelegate ensureEntityQueries,
            BuildingResourceHaulerBridgeSystem.GetEntityQueryDelegate getHaulerUnitsQuery,
            BuildingResourceHaulerBridgeSystem.GetEntityQueryDelegate getSelectedUnitsQuery,
            BuildingResourceHaulerBridgeSystem.TryGetRuntimeBuildingDelegate tryGetRuntimeBuilding,
            BuildingResourceHaulerBridgeSystem.GetEffectivePlacementRectDelegate getEffectivePlacementRect,
            BuildingProductionTransportPresentationSystemHelper.PrepareTransportDropVisualDelegate prepareTransportDropVisual = null)
        {
            RuntimeBuildings = runtimeBuildings;
            WorldCamera = worldCamera;
            DefinitionSystem = definitionSystem;
            ProductionSystem = productionSystem;
            ProductionUpdateSystem = productionUpdateSystem;
            TransportSystem = transportSystem;
            TransportBridgeSystem = transportBridgeSystem;
            ProductionSlotSystem = productionSlotSystem;
            RunwaySystem = runwaySystem;
            VisualSystem = visualSystem;
            SpawnSystem = spawnSystem;
            SpawnContext = spawnContext;
            ResourceDollars = resourceDollars;
            BeginPlacementForConfiguredSpawnable = beginPlacementForConfiguredSpawnable;
            TrySpendDollars = trySpendDollars;
            RefundDollars = refundDollars;
            SetActivePlacementCost = setActivePlacementCost;
            TryQueuePlayerUnit = tryQueuePlayerUnit;
            SelectRuntimeBuilding = selectRuntimeBuilding;
            SuppressNextWorldClick = suppressNextWorldClick;
            RefreshBuildingMarkers = refreshBuildingMarkers;
            ClearFocusedUnit = clearFocusedUnit;
            IsBuildDrawerOpen = isBuildDrawerOpen;
            SmoothMoveCameraGroundCenterTo = smoothMoveCameraGroundCenterTo;
            ResolveBuildingFocusWorldPosition = resolveBuildingFocusWorldPosition;
            RecordUnitOrdered = recordUnitOrdered;
            LogWarning = logWarning;
            CountPendingProductionsForFaction = countPendingProductionsForFaction;
            CountRuntimeProducedUnitsForFaction = countRuntimeProducedUnitsForFaction;
            ResourceHaulerSystem = resourceHaulerSystem;
            FactionResourceSystem = factionResourceSystem;
            TryGetEntityManager = tryGetEntityManager;
            TryGetGridData = tryGetGridData;
            EnsureEntityQueries = ensureEntityQueries;
            GetHaulerUnitsQuery = getHaulerUnitsQuery;
            GetSelectedUnitsQuery = getSelectedUnitsQuery;
            TryGetRuntimeBuilding = tryGetRuntimeBuilding;
            GetEffectivePlacementRect = getEffectivePlacementRect;
            PrepareTransportDropVisual = prepareTransportDropVisual;
        }
    }

    public Source CreateSource(
        IReadOnlyDictionary<int, RuntimeBuildingEntity> runtimeBuildings,
        Camera worldCamera,
        BuildingDefinitionPrefabSystemHelper definitionSystem,
        BuildingProductionQueueCompositionSystemHelper productionSystem,
        BuildingProductionUpdateCompositionSystemHelper productionUpdateSystem,
        BuildingProductionTransportPresentationSystemHelper transportSystem,
        BuildingProductionTransportBridgeCompositionSystemHelper transportBridgeSystem,
        BuildingProductionSlotUtilitySystemHelper productionSlotSystem,
        BuildingRunwaySystem runwaySystem,
        BuildingVisualSystem visualSystem,
        BuildingSpawnSystem spawnSystem,
        BuildingSpawnSystem.Context spawnContext,
        int resourceDollars,
        BuildingProductionRequestBoundary.BeginPlacementForConfiguredSpawnableDelegate beginPlacementForConfiguredSpawnable,
        BuildingProductionRequestBoundary.TrySpendDollarsDelegate trySpendDollars,
        BuildingProductionRequestBoundary.RefundDollarsDelegate refundDollars,
        BuildingProductionRequestBoundary.SetActivePlacementCostDelegate setActivePlacementCost,
        BuildingProductionRequestBoundary.TryQueuePlayerUnitDelegate tryQueuePlayerUnit,
        BuildingProductionRequestBoundary.SelectRuntimeBuildingDelegate selectRuntimeBuilding,
        BuildingProductionRequestBoundary.RuntimeGameplayAction suppressNextWorldClick,
        BuildingProductionRequestBoundary.RuntimeGameplayAction refreshBuildingMarkers,
        BuildingProductionRequestBoundary.RuntimeGameplayAction clearFocusedUnit,
        BuildingProductionTransportBridgeCompositionSystemHelper.BooleanQuery isBuildDrawerOpen,
        BuildingProductionRequestBoundary.CameraFocusAction smoothMoveCameraGroundCenterTo,
        BuildingProductionRequestBoundary.ResolveBuildingFocusWorldPositionDelegate resolveBuildingFocusWorldPosition,
        BuildingProductionRequestBoundary.RecordUnitOrderedDelegate recordUnitOrdered,
        BuildingProductionRequestBoundary.LogWarningDelegate logWarning,
        BuildingProductionRequestBoundary.CountFactionUnitsDelegate countPendingProductionsForFaction,
        BuildingProductionRequestBoundary.CountFactionUnitsDelegate countRuntimeProducedUnitsForFaction,
        ResourceHaulerSystem resourceHaulerSystem,
        FactionResourceSystem factionResourceSystem,
        BuildingResourceHaulerBridgeSystem.TryGetEntityManagerDelegate tryGetEntityManager,
        BuildingResourceHaulerBridgeSystem.TryGetGridDataDelegate tryGetGridData,
        BuildingResourceHaulerBridgeSystem.EnsureEntityQueriesDelegate ensureEntityQueries,
        BuildingResourceHaulerBridgeSystem.GetEntityQueryDelegate getHaulerUnitsQuery,
        BuildingResourceHaulerBridgeSystem.GetEntityQueryDelegate getSelectedUnitsQuery,
        BuildingResourceHaulerBridgeSystem.TryGetRuntimeBuildingDelegate tryGetRuntimeBuilding,
        BuildingResourceHaulerBridgeSystem.GetEffectivePlacementRectDelegate getEffectivePlacementRect,
        BuildingProductionTransportPresentationSystemHelper.PrepareTransportDropVisualDelegate prepareTransportDropVisual = null)
    {
        return new Source(
            runtimeBuildings,
            worldCamera,
            definitionSystem,
            productionSystem,
            productionUpdateSystem,
            transportSystem,
            transportBridgeSystem,
            productionSlotSystem,
            runwaySystem,
            visualSystem,
            spawnSystem,
            spawnContext,
            resourceDollars,
            beginPlacementForConfiguredSpawnable,
            trySpendDollars,
            refundDollars,
            setActivePlacementCost,
            tryQueuePlayerUnit,
            selectRuntimeBuilding,
            suppressNextWorldClick,
            refreshBuildingMarkers,
            clearFocusedUnit,
            isBuildDrawerOpen,
            smoothMoveCameraGroundCenterTo,
            resolveBuildingFocusWorldPosition,
            recordUnitOrdered,
            logWarning,
            countPendingProductionsForFaction,
            countRuntimeProducedUnitsForFaction,
            resourceHaulerSystem,
            factionResourceSystem,
            tryGetEntityManager,
            tryGetGridData,
            ensureEntityQueries,
            getHaulerUnitsQuery,
            getSelectedUnitsQuery,
            tryGetRuntimeBuilding,
            getEffectivePlacementRect,
            prepareTransportDropVisual);
    }

    public BuildingProductionUpdateCompositionSystemHelper.Context CreateProductionUpdateContext(Source source)
    {
        source.ProductionSystem?.PrewarmPendingProductionPool();
        source.TransportSystem?.PrewarmConfiguredProductionTransportPools(
            source.ProductionSystem,
            source.DefinitionSystem?.ConfiguredUnitSpawnPrefabs,
            source.DefinitionSystem?.UnitSpawnPrefabsByKey,
            BuildingDefinitionPrefabSystemHelper.TryGetPrefabLocalBounds,
            source.VisualSystem);
        source.TransportSystem?.PrewarmProductionTransportPools(
            source.RuntimeBuildings,
            source.VisualSystem);

        return new BuildingProductionUpdateCompositionSystemHelper.Context(
            source.RuntimeBuildings,
            source.ProductionSystem,
            source.TransportSystem,
            CreateProductionTransportContext(source));
    }

    public BuildingProductionTransportPresentationSystemHelper.Context CreateProductionTransportContext(Source source)
    {
        return new BuildingProductionTransportPresentationSystemHelper.Context(
            source.RuntimeBuildings,
            source.WorldCamera,
            source.ProductionSystem,
            source.VisualSystem,
            source.RunwaySystem,
            source.TransportBridgeSystem,
            CreateProductionTransportBridgeContext(source),
            source.PrepareTransportDropVisual);
    }

    public BuildingProductionTransportBridgeCompositionSystemHelper.Context CreateProductionTransportBridgeContext(Source source)
    {
        return new BuildingProductionTransportBridgeCompositionSystemHelper.Context(
            (out EntityManager entityManager) => source.TryGetEntityManager(out entityManager),
            (out Entity gridEntity, out GridConfig grid, out DynamicBuffer<GridRoad> roads, out DynamicBlockerComponent blockerData) =>
                source.TryGetGridData(out gridEntity, out grid, out roads, out blockerData),
            entityManager => source.EnsureEntityQueries?.Invoke(entityManager),
            source.SpawnSystem,
            source.SpawnContext,
            () => source.IsBuildDrawerOpen?.Invoke() == true,
            worldPosition => source.SmoothMoveCameraGroundCenterTo?.Invoke(worldPosition));
    }

    public BuildingProductionRequestBoundary.Context CreateProductionRequestContext(Source source)
    {
        source.ProductionSystem?.PrewarmPendingProductionPool();
        source.ProductionSystem?.PrewarmProductionTransportSettings(
            source.DefinitionSystem.ConfiguredUnitSpawnPrefabs,
            source.DefinitionSystem.UnitSpawnPrefabsByKey,
            BuildingDefinitionPrefabSystemHelper.TryGetPrefabLocalBounds);

        return new BuildingProductionRequestBoundary.Context(
            source.RuntimeBuildings,
            source.DefinitionSystem.ConfiguredSpawnableDefinitions,
            source.DefinitionSystem.ConfiguredDefinitionsByPrefab,
            source.DefinitionSystem.ConfiguredUnitSpawnPrefabs,
            source.DefinitionSystem.UnitSpawnPrefabsByKey,
            source.ResourceDollars,
            source.ProductionSystem,
            CreateProductionQueueContext(source),
            source.RunwaySystem,
            BuildingDefinitionPrefabSystemHelper.GetProductionPrefab,
            BuildingDefinitionPrefabSystemHelper.TryGetPrefabLocalBounds,
            source.BeginPlacementForConfiguredSpawnable,
            source.TrySpendDollars,
            source.RefundDollars,
            source.SetActivePlacementCost,
            source.TryQueuePlayerUnit,
            source.SelectRuntimeBuilding,
            source.SuppressNextWorldClick,
            source.RefreshBuildingMarkers,
            source.ClearFocusedUnit,
            source.SmoothMoveCameraGroundCenterTo,
            source.ResolveBuildingFocusWorldPosition,
            source.RecordUnitOrdered,
            source.LogWarning,
            source.CountPendingProductionsForFaction,
            source.CountRuntimeProducedUnitsForFaction,
            source.DefinitionSystem.TryGetConfiguredUnitReadModel);
    }

    public BuildingProductionQueueCompositionSystemHelper.QueueContext CreateProductionQueueContext(Source source)
    {
        return new BuildingProductionQueueCompositionSystemHelper.QueueContext(
            source.DefinitionSystem.ConfiguredUnitSpawnPrefabs,
            source.DefinitionSystem.UnitSpawnPrefabsByKey,
            source.ProductionSlotSystem,
            BuildingDefinitionPrefabSystemHelper.TryGetPrefabLocalBounds,
            BuildingDefinitionPrefabSystemHelper.RuntimeBuildingMatchesId,
            source.SpawnContext.TryGetRuntimeBoundaryEntity == null
                ? null
                : (BuildingProductionQueueCompositionSystemHelper.TryGetRuntimeBoundaryEntityDelegate)(
                    (EntityManager em, out Entity entity) => source.SpawnContext.TryGetRuntimeBoundaryEntity(em, out entity)));
    }

    public bool TryQueuePlayerUnitProduction(
        Source source,
        RuntimeBuildingEntity building,
        int productionIndex,
        GameObject spawnUnitPrefab,
        float now)
    {
        if (!source.TryGetEntityManager(out EntityManager entityManager))
            return false;

        return source.ProductionSystem.TryQueuePlayerUnitFromBuilding(
            CreateProductionQueueContext(source),
            building,
            productionIndex,
            spawnUnitPrefab,
            entityManager,
            now);
    }

    public BuildingResourceHaulerBridgeSystem.Context CreateResourceHaulerBridgeContext(Source source)
    {
        return new BuildingResourceHaulerBridgeSystem.Context(
            source.RuntimeBuildings,
            source.ResourceHaulerSystem,
            source.FactionResourceSystem,
            source.TryGetEntityManager,
            source.TryGetGridData,
            source.EnsureEntityQueries,
            source.GetHaulerUnitsQuery,
            source.GetSelectedUnitsQuery,
            source.TryGetRuntimeBuilding,
            building => source.ResolveBuildingFocusWorldPosition(building),
            source.GetEffectivePlacementRect);
    }
}
