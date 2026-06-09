using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

internal sealed class BuildingProductionContextSystem
{
    public readonly struct Source
    {
        public readonly IReadOnlyDictionary<int, RuntimeBuildingEntity> RuntimeBuildings;
        public readonly Camera WorldCamera;
        public readonly BuildingDefinitionSystem DefinitionSystem;
        public readonly BuildingProductionSystem ProductionSystem;
        public readonly BuildingProductionUpdateSystem ProductionUpdateSystem;
        public readonly BuildingProductionTransportSystem TransportSystem;
        public readonly BuildingProductionTransportBridgeSystem TransportBridgeSystem;
        public readonly BuildingProductionSlotSystem ProductionSlotSystem;
        public readonly BuildingRunwaySystem RunwaySystem;
        public readonly BuildingVisualSystem VisualSystem;
        public readonly BuildingSpawnSystem SpawnSystem;
        public readonly BuildingSpawnSystem.Context SpawnContext;
        public readonly int ResourceDollars;
        public readonly BuildingProductionRequestSystem.BeginPlacementForConfiguredSpawnableDelegate BeginPlacementForConfiguredSpawnable;
        public readonly BuildingProductionRequestSystem.TrySpendDollarsDelegate TrySpendDollars;
        public readonly BuildingProductionRequestSystem.RefundDollarsDelegate RefundDollars;
        public readonly BuildingProductionRequestSystem.SetActivePlacementCostDelegate SetActivePlacementCost;
        public readonly BuildingProductionRequestSystem.TryQueuePlayerUnitDelegate TryQueuePlayerUnit;
        public readonly BuildingProductionRequestSystem.SelectRuntimeBuildingDelegate SelectRuntimeBuilding;
        public readonly BuildingProductionRequestSystem.RuntimeGameplayAction SuppressNextWorldClick;
        public readonly BuildingProductionRequestSystem.RuntimeGameplayAction RefreshBuildingMarkers;
        public readonly BuildingProductionRequestSystem.RuntimeGameplayAction ClearFocusedUnit;
        public readonly BuildingProductionTransportBridgeSystem.BooleanQuery IsBuildDrawerOpen;
        public readonly BuildingProductionRequestSystem.CameraFocusAction SmoothMoveCameraGroundCenterTo;
        public readonly BuildingProductionRequestSystem.ResolveBuildingFocusWorldPositionDelegate ResolveBuildingFocusWorldPosition;
        public readonly BuildingProductionRequestSystem.RecordUnitOrderedDelegate RecordUnitOrdered;
        public readonly BuildingProductionRequestSystem.LogWarningDelegate LogWarning;
        public readonly BuildingProductionRequestSystem.CountFactionUnitsDelegate CountPendingProductionsForFaction;
        public readonly BuildingProductionRequestSystem.CountFactionUnitsDelegate CountRuntimeProducedUnitsForFaction;
        public readonly ResourceHaulerSystem ResourceHaulerSystem;
        public readonly FactionResourceSystem FactionResourceSystem;
        public readonly BuildingResourceHaulerBridgeSystem.TryGetEntityManagerDelegate TryGetEntityManager;
        public readonly BuildingResourceHaulerBridgeSystem.TryGetGridDataDelegate TryGetGridData;
        public readonly BuildingResourceHaulerBridgeSystem.EnsureEntityQueriesDelegate EnsureEntityQueries;
        public readonly BuildingResourceHaulerBridgeSystem.GetEntityQueryDelegate GetHaulerUnitsQuery;
        public readonly BuildingResourceHaulerBridgeSystem.GetEntityQueryDelegate GetSelectedUnitsQuery;
        public readonly BuildingResourceHaulerBridgeSystem.TryGetRuntimeBuildingDelegate TryGetRuntimeBuilding;
        public readonly BuildingResourceHaulerBridgeSystem.GetEffectivePlacementRectDelegate GetEffectivePlacementRect;

        public Source(
            IReadOnlyDictionary<int, RuntimeBuildingEntity> runtimeBuildings,
            Camera worldCamera,
            BuildingDefinitionSystem definitionSystem,
            BuildingProductionSystem productionSystem,
            BuildingProductionUpdateSystem productionUpdateSystem,
            BuildingProductionTransportSystem transportSystem,
            BuildingProductionTransportBridgeSystem transportBridgeSystem,
            BuildingProductionSlotSystem productionSlotSystem,
            BuildingRunwaySystem runwaySystem,
            BuildingVisualSystem visualSystem,
            BuildingSpawnSystem spawnSystem,
            BuildingSpawnSystem.Context spawnContext,
            int resourceDollars,
            BuildingProductionRequestSystem.BeginPlacementForConfiguredSpawnableDelegate beginPlacementForConfiguredSpawnable,
            BuildingProductionRequestSystem.TrySpendDollarsDelegate trySpendDollars,
            BuildingProductionRequestSystem.RefundDollarsDelegate refundDollars,
            BuildingProductionRequestSystem.SetActivePlacementCostDelegate setActivePlacementCost,
            BuildingProductionRequestSystem.TryQueuePlayerUnitDelegate tryQueuePlayerUnit,
            BuildingProductionRequestSystem.SelectRuntimeBuildingDelegate selectRuntimeBuilding,
            BuildingProductionRequestSystem.RuntimeGameplayAction suppressNextWorldClick,
            BuildingProductionRequestSystem.RuntimeGameplayAction refreshBuildingMarkers,
            BuildingProductionRequestSystem.RuntimeGameplayAction clearFocusedUnit,
            BuildingProductionTransportBridgeSystem.BooleanQuery isBuildDrawerOpen,
            BuildingProductionRequestSystem.CameraFocusAction smoothMoveCameraGroundCenterTo,
            BuildingProductionRequestSystem.ResolveBuildingFocusWorldPositionDelegate resolveBuildingFocusWorldPosition,
            BuildingProductionRequestSystem.RecordUnitOrderedDelegate recordUnitOrdered,
            BuildingProductionRequestSystem.LogWarningDelegate logWarning,
            BuildingProductionRequestSystem.CountFactionUnitsDelegate countPendingProductionsForFaction,
            BuildingProductionRequestSystem.CountFactionUnitsDelegate countRuntimeProducedUnitsForFaction,
            ResourceHaulerSystem resourceHaulerSystem,
            FactionResourceSystem factionResourceSystem,
            BuildingResourceHaulerBridgeSystem.TryGetEntityManagerDelegate tryGetEntityManager,
            BuildingResourceHaulerBridgeSystem.TryGetGridDataDelegate tryGetGridData,
            BuildingResourceHaulerBridgeSystem.EnsureEntityQueriesDelegate ensureEntityQueries,
            BuildingResourceHaulerBridgeSystem.GetEntityQueryDelegate getHaulerUnitsQuery,
            BuildingResourceHaulerBridgeSystem.GetEntityQueryDelegate getSelectedUnitsQuery,
            BuildingResourceHaulerBridgeSystem.TryGetRuntimeBuildingDelegate tryGetRuntimeBuilding,
            BuildingResourceHaulerBridgeSystem.GetEffectivePlacementRectDelegate getEffectivePlacementRect)
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
        }
    }

    public Source CreateSource(
        IReadOnlyDictionary<int, RuntimeBuildingEntity> runtimeBuildings,
        Camera worldCamera,
        BuildingDefinitionSystem definitionSystem,
        BuildingProductionSystem productionSystem,
        BuildingProductionUpdateSystem productionUpdateSystem,
        BuildingProductionTransportSystem transportSystem,
        BuildingProductionTransportBridgeSystem transportBridgeSystem,
        BuildingProductionSlotSystem productionSlotSystem,
        BuildingRunwaySystem runwaySystem,
        BuildingVisualSystem visualSystem,
        BuildingSpawnSystem spawnSystem,
        BuildingSpawnSystem.Context spawnContext,
        int resourceDollars,
        BuildingProductionRequestSystem.BeginPlacementForConfiguredSpawnableDelegate beginPlacementForConfiguredSpawnable,
        BuildingProductionRequestSystem.TrySpendDollarsDelegate trySpendDollars,
        BuildingProductionRequestSystem.RefundDollarsDelegate refundDollars,
        BuildingProductionRequestSystem.SetActivePlacementCostDelegate setActivePlacementCost,
        BuildingProductionRequestSystem.TryQueuePlayerUnitDelegate tryQueuePlayerUnit,
        BuildingProductionRequestSystem.SelectRuntimeBuildingDelegate selectRuntimeBuilding,
        BuildingProductionRequestSystem.RuntimeGameplayAction suppressNextWorldClick,
        BuildingProductionRequestSystem.RuntimeGameplayAction refreshBuildingMarkers,
        BuildingProductionRequestSystem.RuntimeGameplayAction clearFocusedUnit,
        BuildingProductionTransportBridgeSystem.BooleanQuery isBuildDrawerOpen,
        BuildingProductionRequestSystem.CameraFocusAction smoothMoveCameraGroundCenterTo,
        BuildingProductionRequestSystem.ResolveBuildingFocusWorldPositionDelegate resolveBuildingFocusWorldPosition,
        BuildingProductionRequestSystem.RecordUnitOrderedDelegate recordUnitOrdered,
        BuildingProductionRequestSystem.LogWarningDelegate logWarning,
        BuildingProductionRequestSystem.CountFactionUnitsDelegate countPendingProductionsForFaction,
        BuildingProductionRequestSystem.CountFactionUnitsDelegate countRuntimeProducedUnitsForFaction,
        ResourceHaulerSystem resourceHaulerSystem,
        FactionResourceSystem factionResourceSystem,
        BuildingResourceHaulerBridgeSystem.TryGetEntityManagerDelegate tryGetEntityManager,
        BuildingResourceHaulerBridgeSystem.TryGetGridDataDelegate tryGetGridData,
        BuildingResourceHaulerBridgeSystem.EnsureEntityQueriesDelegate ensureEntityQueries,
        BuildingResourceHaulerBridgeSystem.GetEntityQueryDelegate getHaulerUnitsQuery,
        BuildingResourceHaulerBridgeSystem.GetEntityQueryDelegate getSelectedUnitsQuery,
        BuildingResourceHaulerBridgeSystem.TryGetRuntimeBuildingDelegate tryGetRuntimeBuilding,
        BuildingResourceHaulerBridgeSystem.GetEffectivePlacementRectDelegate getEffectivePlacementRect)
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
            getEffectivePlacementRect);
    }

    public BuildingProductionUpdateSystem.Context CreateProductionUpdateContext(Source source)
    {
        return new BuildingProductionUpdateSystem.Context(
            source.RuntimeBuildings,
            source.ProductionSystem,
            source.TransportSystem,
            CreateProductionTransportContext(source));
    }

    public BuildingProductionTransportSystem.Context CreateProductionTransportContext(Source source)
    {
        return new BuildingProductionTransportSystem.Context(
            source.RuntimeBuildings,
            source.WorldCamera,
            source.ProductionSystem,
            source.VisualSystem,
            source.RunwaySystem,
            source.TransportBridgeSystem,
            CreateProductionTransportBridgeContext(source));
    }

    public BuildingProductionTransportBridgeSystem.Context CreateProductionTransportBridgeContext(Source source)
    {
        return new BuildingProductionTransportBridgeSystem.Context(
            (out EntityManager entityManager) => source.TryGetEntityManager(out entityManager),
            (out Entity gridEntity, out GridConfig grid, out DynamicBuffer<GridRoad> roads, out DynamicBlockerComponent blockerData) =>
                source.TryGetGridData(out gridEntity, out grid, out roads, out blockerData),
            entityManager => source.EnsureEntityQueries?.Invoke(entityManager),
            source.SpawnSystem,
            source.SpawnContext,
            () => source.IsBuildDrawerOpen?.Invoke() == true,
            worldPosition => source.SmoothMoveCameraGroundCenterTo?.Invoke(worldPosition));
    }

    public BuildingProductionRequestSystem.Context CreateProductionRequestContext(Source source)
    {
        return new BuildingProductionRequestSystem.Context(
            source.RuntimeBuildings,
            source.DefinitionSystem.ConfiguredSpawnableDefinitions,
            source.DefinitionSystem.ConfiguredDefinitionsByPrefab,
            source.DefinitionSystem.ConfiguredUnitSpawnPrefabs,
            source.DefinitionSystem.UnitSpawnPrefabsByKey,
            source.ResourceDollars,
            source.ProductionSystem,
            CreateProductionQueueContext(source),
            source.RunwaySystem,
            BuildingDefinitionSystem.GetProductionPrefab,
            BuildingDefinitionSystem.TryGetPrefabLocalBounds,
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
            source.CountRuntimeProducedUnitsForFaction);
    }

    public BuildingProductionSystem.QueueContext CreateProductionQueueContext(Source source)
    {
        return new BuildingProductionSystem.QueueContext(
            source.DefinitionSystem.ConfiguredUnitSpawnPrefabs,
            source.DefinitionSystem.UnitSpawnPrefabsByKey,
            source.ProductionSlotSystem,
            BuildingDefinitionSystem.TryGetPrefabLocalBounds,
            BuildingDefinitionSystem.RuntimeBuildingMatchesId);
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
