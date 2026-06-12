using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public sealed class BuildingRuntimeBoundarySystem
{
    private const float PublishIntervalSeconds = 0.5f;
    private const int MaxRuntimeSpawnRequestsPerUpdate = 16;

    private readonly List<byte> _factionIds = new();
    private readonly List<int> _pendingSpawnRequestIndices = new();
    private readonly Dictionary<GameObject, FixedString128Bytes> _boundaryIdsByPrefab = new();
    private readonly Dictionary<string, FixedString128Bytes> _boundaryIdsByFallback = new();
    private readonly BuildingRuntimeSurfaceOverlaySystem _surfaceOverlaySystem = new();
    private float _nextPublishAt;
    private bool _forcePublishNextUpdate;
    private bool _configuredReadModelsPublished;
    private int _lastPublishedRuntimeBuildingSignature = int.MinValue;

    internal void Update(
        BuildingDefinitionSystem definitionSystem,
        BuildingRuntimeSpawnSystem runtimeSpawnSystem,
        BuildingRuntimeSpawnSystem.Context runtimeSpawnContext,
        BuildingProductionRequestSystem productionRequestSystem,
        BuildingProductionRequestSystem.Context productionRequestContext,
        BuildingRuntimeQuerySystem runtimeQuerySystem,
        BuildingRuntimeQuerySystem.Context runtimeQueryContext,
        FactionResourceSystem factionResourceSystem,
        EntityManager em,
        EntityQuery boundaryQuery,
        IReadOnlyDictionary<int, RuntimeBuildingEntity> runtimeBuildings,
        float now)
    {
        if (definitionSystem == null ||
            runtimeSpawnSystem == null ||
            productionRequestSystem == null ||
            runtimeQuerySystem == null ||
            factionResourceSystem == null ||
            runtimeBuildings == null)
        {
            return;
        }

        if (!TryGetBoundaryEntity(em, boundaryQuery, out Entity boundaryEntity))
            return;

        ProcessRequests(
            definitionSystem,
            runtimeSpawnSystem,
            runtimeSpawnContext,
            productionRequestSystem,
            productionRequestContext,
            runtimeQuerySystem,
            runtimeQueryContext,
            factionResourceSystem,
            runtimeBuildings,
            em,
            boundaryEntity,
            now);
        PublishReadModelIfDue(
            definitionSystem,
            runtimeQuerySystem,
            runtimeQueryContext,
            factionResourceSystem,
            em,
            boundaryEntity,
            runtimeBuildings,
            now);
    }

    private void ProcessRequests(
        BuildingDefinitionSystem definitionSystem,
        BuildingRuntimeSpawnSystem runtimeSpawnSystem,
        BuildingRuntimeSpawnSystem.Context runtimeSpawnContext,
        BuildingProductionRequestSystem productionRequestSystem,
        BuildingProductionRequestSystem.Context productionRequestContext,
        BuildingRuntimeQuerySystem runtimeQuerySystem,
        BuildingRuntimeQuerySystem.Context runtimeQueryContext,
        FactionResourceSystem factionResourceSystem,
        IReadOnlyDictionary<int, RuntimeBuildingEntity> runtimeBuildings,
        EntityManager em,
        Entity boundaryEntity,
        float now)
    {
        ProcessResourceSellRequests(factionResourceSystem, runtimeBuildings, em, boundaryEntity);
        ProcessProductionRequests(productionRequestSystem, productionRequestContext, runtimeQuerySystem, runtimeQueryContext, em, boundaryEntity, now);
        ProcessRuntimeSpawnRequests(definitionSystem, runtimeSpawnSystem, runtimeSpawnContext, em, boundaryEntity);
    }

    private void ProcessResourceSellRequests(
        FactionResourceSystem factionResourceSystem,
        IReadOnlyDictionary<int, RuntimeBuildingEntity> runtimeBuildings,
        EntityManager em,
        Entity boundaryEntity)
    {
        DynamicBuffer<BuildingFactionResourceSellRequest> sellRequests =
            EnsureBoundaryBuffer<BuildingFactionResourceSellRequest>(em, boundaryEntity);
        for (int i = 0; i < sellRequests.Length; i++)
        {
            BuildingFactionResourceSellRequest request = sellRequests[i];
            if (request.Status != BuildingFactionResourceSellRequest.Pending)
                continue;

            float soldOil = factionResourceSystem.DrainFactionResource(
                runtimeBuildings,
                request.FactionId,
                Mathf.Max(0f, request.RequestedOilBarrels),
                FactionResourceSystem.ResourceKind.Oil);
            float soldFuel = factionResourceSystem.DrainFactionResource(
                runtimeBuildings,
                request.FactionId,
                Mathf.Max(0f, request.RequestedFuelBarrels),
                FactionResourceSystem.ResourceKind.Fuel);
            request.Status = BuildingFactionResourceSellRequest.Succeeded;
            request.ResultCode = soldOil > 0f || soldFuel > 0f
                ? (byte)0
                : BuildingFactionResourceSellRequest.NoneSold;
            request.SoldOilBarrels = soldOil;
            request.SoldFuelBarrels = soldFuel;
            sellRequests[i] = request;
        }
    }

    private void ProcessProductionRequests(
        BuildingProductionRequestSystem productionRequestSystem,
        BuildingProductionRequestSystem.Context productionRequestContext,
        BuildingRuntimeQuerySystem runtimeQuerySystem,
        BuildingRuntimeQuerySystem.Context runtimeQueryContext,
        EntityManager em,
        Entity boundaryEntity,
        float now)
    {
        DynamicBuffer<BuildingFactionUnitProductionRequest> productionRequests =
            EnsureBoundaryBuffer<BuildingFactionUnitProductionRequest>(em, boundaryEntity);
        for (int i = 0; i < productionRequests.Length; i++)
        {
            BuildingFactionUnitProductionRequest request = productionRequests[i];
            if (request.Status != BuildingFactionUnitProductionRequest.Pending)
                continue;

            string unitId = productionRequestSystem.GetCachedUnitIdString(request.UnitId);
            bool queued = productionRequestSystem.QueueFactionUnitProductionRequest(
                productionRequestContext,
                request.FactionId,
                unitId,
                em,
                now,
                ref request,
                unitIdIsNormalized: true);
            if (queued)
                _forcePublishNextUpdate = true;
            request.Status = queued
                ? BuildingFactionUnitProductionRequest.Succeeded
                : BuildingFactionUnitProductionRequest.Failed;
            if (request.ResultCode != BuildingFactionUnitProductionRequest.MissingUnitConfig)
            {
                request.QueueCount = runtimeQuerySystem.CountPendingProductionsForFaction(runtimeQueryContext, request.FactionId, unitId);
                request.ProducedCount = runtimeQuerySystem.CountRuntimeProducedUnitsForFaction(runtimeQueryContext, request.FactionId, unitId);
            }
            productionRequests[i] = request;
        }
    }

    private void ProcessRuntimeSpawnRequests(
        BuildingDefinitionSystem definitionSystem,
        BuildingRuntimeSpawnSystem runtimeSpawnSystem,
        BuildingRuntimeSpawnSystem.Context runtimeSpawnContext,
        EntityManager em,
        Entity boundaryEntity)
    {
        _pendingSpawnRequestIndices.Clear();
        DynamicBuffer<BuildingRuntimeSpawnRequest> spawnRequests =
            EnsureBoundaryBuffer<BuildingRuntimeSpawnRequest>(em, boundaryEntity);
        for (int i = 0; i < spawnRequests.Length; i++)
        {
            if (spawnRequests[i].Status == BuildingRuntimeSpawnRequest.Pending)
                _pendingSpawnRequestIndices.Add(i);
        }

        int processedRequests = 0;
        for (int pendingIndex = 0; pendingIndex < _pendingSpawnRequestIndices.Count; pendingIndex++)
        {
            if (processedRequests >= MaxRuntimeSpawnRequestsPerUpdate)
                break;

            int i = _pendingSpawnRequestIndices[pendingIndex];
            spawnRequests = EnsureBoundaryBuffer<BuildingRuntimeSpawnRequest>(em, boundaryEntity);
            if ((uint)i >= (uint)spawnRequests.Length)
                continue;

            BuildingRuntimeSpawnRequest request = spawnRequests[i];
            if (request.Status != BuildingRuntimeSpawnRequest.Pending)
                continue;

            processedRequests++;
            if (!TryResolveConfiguredBuildingDefinition(definitionSystem, request.BuildingId.ToString(), out BuildingDefinition definition))
            {
                request.Status = BuildingRuntimeSpawnRequest.Failed;
                request.ResultCode = BuildingRuntimeSpawnRequest.MissingConfig;
                WriteRuntimeSpawnRequest(em, boundaryEntity, i, request);
                continue;
            }

            if (!definitionSystem.TryGetConfiguredSpawnable(definition.Prefab, out var spawnable))
                spawnable = BuildingDefinitionSystem.BuildConfiguredSpawnableEntry(definition);
            if (spawnable.Prefab == null || !spawnable.CanRequest)
            {
                request.Status = BuildingRuntimeSpawnRequest.Failed;
                request.ResultCode = BuildingRuntimeSpawnRequest.MissingConfig;
                WriteRuntimeSpawnRequest(em, boundaryEntity, i, request);
                continue;
            }

            // Runtime spawn creates/updates entities, so any DynamicBuffer handle captured before it is invalid afterwards.
            bool placed;
            BuildingRuntimeSpawnSystem.SpawnRuntimeBuildingResult result = default;
            if (request.RequestKind == BuildingRuntimeSpawnRequest.KindWallRun)
            {
                int spawned = runtimeSpawnSystem.TrySpawnRuntimeWallRun(
                    runtimeSpawnContext,
                    spawnable.Prefab,
                    new Vector2Int(request.PreferredOrigin.x, request.PreferredOrigin.y),
                    new Vector2Int(request.EndOrigin.x, request.EndOrigin.y),
                    request.FactionId);
                placed = spawned > 0;
                request.SpawnedCount = spawned;
            }
            else if (request.RequestKind == BuildingRuntimeSpawnRequest.KindWallSegment)
            {
                placed = runtimeSpawnSystem.TrySpawnRuntimeWallSegment(
                    runtimeSpawnContext,
                    spawnable.Prefab,
                    new Vector2Int(request.PreferredOrigin.x, request.PreferredOrigin.y),
                    request.RotateVertical != 0,
                    request.FactionId,
                    request.AllowExistingWallOverlap != 0);
                request.SpawnedCount = placed ? 1 : 0;
            }
            else
            {
                placed = runtimeSpawnSystem.TryPlaceRuntimeBuilding(
                    runtimeSpawnContext,
                    spawnable.Prefab,
                    new Vector2Int(request.PreferredOrigin.x, request.PreferredOrigin.y),
                    spawnable.DisplayName,
                    spawnable.Description,
                    null,
                    500,
                    false,
                    request.FactionId,
                    request.RotateVertical != 0,
                    out result);
                request.SpawnedCount = placed ? 1 : 0;
            }

            request.Status = placed
                ? BuildingRuntimeSpawnRequest.Succeeded
                : BuildingRuntimeSpawnRequest.Failed;
            if (placed)
                _forcePublishNextUpdate = true;
            request.ResultCode = placed ? (byte)0 : BuildingRuntimeSpawnRequest.Blocked;
            request.BuildingRuntimeId = placed ? result.BuildingId : 0;
            request.ActualOrigin = placed ? new int2(result.ActualOrigin.x, result.ActualOrigin.y) : default;
            request.ActualFootprint = placed ? new int2(result.ActualFootprint.x, result.ActualFootprint.y) : default;
            WriteRuntimeSpawnRequest(em, boundaryEntity, i, request);
        }
    }

    private static void WriteRuntimeSpawnRequest(
        EntityManager em,
        Entity boundaryEntity,
        int index,
        BuildingRuntimeSpawnRequest request)
    {
        DynamicBuffer<BuildingRuntimeSpawnRequest> spawnRequests =
            EnsureBoundaryBuffer<BuildingRuntimeSpawnRequest>(em, boundaryEntity);
        if ((uint)index >= (uint)spawnRequests.Length)
            return;

        spawnRequests[index] = request;
    }

    private void PublishReadModelIfDue(
        BuildingDefinitionSystem definitionSystem,
        BuildingRuntimeQuerySystem runtimeQuerySystem,
        BuildingRuntimeQuerySystem.Context runtimeQueryContext,
        FactionResourceSystem factionResourceSystem,
        EntityManager em,
        Entity boundaryEntity,
        IReadOnlyDictionary<int, RuntimeBuildingEntity> runtimeBuildings,
        float now)
    {
        bool forcePublish = _forcePublishNextUpdate;
        if (!forcePublish && _configuredReadModelsPublished && now < _nextPublishAt)
            return;

        int runtimeBuildingSignature = ComputeRuntimeBuildingSignature(runtimeBuildings);
        bool buildingSetChanged = runtimeBuildingSignature != _lastPublishedRuntimeBuildingSignature;
        _forcePublishNextUpdate = false;
        _nextPublishAt = now + PublishIntervalSeconds;
        if (!_configuredReadModelsPublished)
        {
            PublishConfiguredSpawnablesReadModel(definitionSystem, em, boundaryEntity);
            PublishConfiguredUnitsReadModel(definitionSystem, em, boundaryEntity);
            _configuredReadModelsPublished = true;
        }

        PublishRuntimeFactionSummaries(factionResourceSystem, runtimeQuerySystem, runtimeQueryContext, em, boundaryEntity, runtimeBuildings);
        PublishRuntimeOwnedBuildingSummaries(definitionSystem, runtimeBuildings, em, boundaryEntity);
        PublishRuntimeUnitProductionSummaries(definitionSystem, runtimeQueryContext, em, boundaryEntity);
        if (forcePublish || buildingSetChanged)
        {
            PublishFactionProductionSpawnPointsReadModel(em, boundaryEntity, runtimeBuildings);
            _surfaceOverlaySystem.Publish(em, boundaryEntity, runtimeBuildings);
            _lastPublishedRuntimeBuildingSignature = runtimeBuildingSignature;
        }
    }

    private void PublishConfiguredSpawnablesReadModel(BuildingDefinitionSystem definitionSystem, EntityManager em, Entity boundaryEntity)
    {
        DynamicBuffer<BuildingConfiguredSpawnableReadModel> buffer =
            EnsureBoundaryBuffer<BuildingConfiguredSpawnableReadModel>(em, boundaryEntity);
        buffer.Clear();

        for (int i = 0; i < definitionSystem.ConfiguredSpawnableCount; i++)
        {
            if (!definitionSystem.TryGetConfiguredDefinition(i, out BuildingDefinition definition))
                continue;

            if (!definitionSystem.TryGetConfiguredSpawnable(i, out var entry))
                continue;
            if (entry.Prefab == null)
                continue;

            buffer.Add(new BuildingConfiguredSpawnableReadModel
            {
                BuildingId = ResolveBoundaryId(entry.Prefab, entry.DisplayName),
                DisplayName = ToFixedString128(entry.DisplayName),
                Price = Mathf.Max(0, entry.Price),
                FootprintCells = new int2(
                    Mathf.Max(1, definition.FootprintCells.x),
                    Mathf.Max(1, definition.FootprintCells.y)),
                CanRequest = entry.CanRequest ? (byte)1 : (byte)0
            });
        }
    }

    private void PublishConfiguredUnitsReadModel(BuildingDefinitionSystem definitionSystem, EntityManager em, Entity boundaryEntity)
    {
        DynamicBuffer<BuildingConfiguredUnitReadModel> buffer =
            EnsureBoundaryBuffer<BuildingConfiguredUnitReadModel>(em, boundaryEntity);
        buffer.Clear();

        for (int i = 0; i < definitionSystem.ConfiguredUnitCount; i++)
        {
            if (!definitionSystem.TryGetConfiguredUnitReadModel(
                    i,
                    out GameObject prefab,
                    out string displayName,
                    out int price,
                    out bool canRequest,
                    out bool isVehicle) ||
                prefab == null)
            {
                continue;
            }

            buffer.Add(new BuildingConfiguredUnitReadModel
            {
                UnitId = ResolveBoundaryId(prefab, displayName),
                DisplayName = ToFixedString128(displayName),
                Price = Mathf.Max(0, price),
                CanRequest = canRequest ? (byte)1 : (byte)0,
                IsVehicle = isVehicle ? (byte)1 : (byte)0
            });
        }
    }

    private void PublishRuntimeFactionSummaries(
        FactionResourceSystem factionResourceSystem,
        BuildingRuntimeQuerySystem runtimeQuerySystem,
        BuildingRuntimeQuerySystem.Context runtimeQueryContext,
        EntityManager em,
        Entity boundaryEntity,
        IReadOnlyDictionary<int, RuntimeBuildingEntity> runtimeBuildings)
    {
        RefreshFactionIds(runtimeBuildings);
        DynamicBuffer<BuildingRuntimeFactionSummary> buffer =
            EnsureBoundaryBuffer<BuildingRuntimeFactionSummary>(em, boundaryEntity);
        buffer.Clear();

        for (int i = 0; i < _factionIds.Count; i++)
        {
            byte factionId = _factionIds[i];
            factionResourceSystem.TryGetFactionResourceEconomy(
                runtimeBuildings,
                factionId,
                out FactionResourceSystem.ResourceEconomySnapshot economy);
            buffer.Add(new BuildingRuntimeFactionSummary
            {
                FactionId = factionId,
                BuildingCount = runtimeQuerySystem.CountRuntimeBuildingsForFaction(runtimeQueryContext, factionId),
                StoredOilBarrels = economy.StoredOilBarrels,
                StoredFuelBarrels = economy.StoredFuelBarrels,
                OilBarrelsPerDay = economy.OilBarrelsPerDay,
                FuelBarrelsPerDay = economy.FuelBarrelsPerDay
            });
        }
    }

    private void PublishRuntimeOwnedBuildingSummaries(
        BuildingDefinitionSystem definitionSystem,
        IReadOnlyDictionary<int, RuntimeBuildingEntity> runtimeBuildings,
        EntityManager em,
        Entity boundaryEntity)
    {
        DynamicBuffer<BuildingRuntimeOwnedBuildingSummary> buffer =
            EnsureBoundaryBuffer<BuildingRuntimeOwnedBuildingSummary>(em, boundaryEntity);
        buffer.Clear();

        if (runtimeBuildings != null)
        {
            if (runtimeBuildings is Dictionary<int, RuntimeBuildingEntity> runtimeBuildingMap)
            {
                foreach (KeyValuePair<int, RuntimeBuildingEntity> pair in runtimeBuildingMap)
                    PublishOwnedBuildingSummary(buffer, pair.Value);
            }
            else
            {
                foreach (KeyValuePair<int, RuntimeBuildingEntity> pair in runtimeBuildings)
                    PublishOwnedBuildingSummary(buffer, pair.Value);
            }
        }

        for (int factionIndex = 0; factionIndex < _factionIds.Count; factionIndex++)
        {
            byte factionId = _factionIds[factionIndex];
            for (int i = 0; i < definitionSystem.ConfiguredSpawnableCount; i++)
            {
                if (!definitionSystem.TryGetConfiguredSpawnable(i, out var entry))
                    continue;
                if (entry.Prefab == null)
                    continue;

                FixedString128Bytes buildingId = ResolveBoundaryId(entry.Prefab, entry.DisplayName);
                AddOrIncrementOwnedBuildingSummary(buffer, factionId, buildingId, 0);
            }
        }
    }

    private void PublishOwnedBuildingSummary(
        DynamicBuffer<BuildingRuntimeOwnedBuildingSummary> buffer,
        RuntimeBuildingEntity building)
    {
        if (building == null ||
            building.IsDestroyed ||
            !building.HasOwnerFaction ||
            building.Definition == null)
        {
            return;
        }

        FixedString128Bytes buildingId = ResolveBoundaryId(building.Definition.Prefab, building.Definition.DisplayName);
        AddOrIncrementOwnedBuildingSummary(buffer, building.OwnerFactionId, buildingId, 1);
    }

    private static void AddOrIncrementOwnedBuildingSummary(
        DynamicBuffer<BuildingRuntimeOwnedBuildingSummary> buffer,
        byte factionId,
        FixedString128Bytes buildingId,
        int countDelta)
    {
        for (int i = 0; i < buffer.Length; i++)
        {
            BuildingRuntimeOwnedBuildingSummary summary = buffer[i];
            if (summary.FactionId != factionId || !summary.BuildingId.Equals(buildingId))
                continue;

            summary.Count += countDelta;
            buffer[i] = summary;
            return;
        }

        buffer.Add(new BuildingRuntimeOwnedBuildingSummary
        {
            FactionId = factionId,
            BuildingId = buildingId,
            Count = countDelta
        });
    }

    private void PublishRuntimeUnitProductionSummaries(
        BuildingDefinitionSystem definitionSystem,
        BuildingRuntimeQuerySystem.Context runtimeQueryContext,
        EntityManager em,
        Entity boundaryEntity)
    {
        DynamicBuffer<BuildingRuntimeUnitProductionSummary> buffer =
            EnsureBoundaryBuffer<BuildingRuntimeUnitProductionSummary>(em, boundaryEntity);
        buffer.Clear();
        PublishConfiguredUnitProductionSummaryRows(definitionSystem, buffer);

        IReadOnlyDictionary<int, RuntimeBuildingEntity> runtimeBuildings = runtimeQueryContext.RuntimeBuildings;
        if (runtimeBuildings == null)
            return;

        EntityManager producedUnitEntityManager = default;
        bool hasEntityManager = runtimeQueryContext.TryGetEntityManager != null &&
                                runtimeQueryContext.TryGetEntityManager(out producedUnitEntityManager);
        if (runtimeBuildings is Dictionary<int, RuntimeBuildingEntity> runtimeBuildingMap)
        {
            foreach (KeyValuePair<int, RuntimeBuildingEntity> pair in runtimeBuildingMap)
                PublishRuntimeUnitProductionSummaryForBuilding(
                    runtimeQueryContext,
                    buffer,
                    producedUnitEntityManager,
                    hasEntityManager,
                    pair.Value);
        }
        else
        {
            foreach (KeyValuePair<int, RuntimeBuildingEntity> pair in runtimeBuildings)
                PublishRuntimeUnitProductionSummaryForBuilding(
                    runtimeQueryContext,
                    buffer,
                    producedUnitEntityManager,
                    hasEntityManager,
                    pair.Value);
        }
    }

    private void PublishRuntimeUnitProductionSummaryForBuilding(
        BuildingRuntimeQuerySystem.Context runtimeQueryContext,
        DynamicBuffer<BuildingRuntimeUnitProductionSummary> buffer,
        EntityManager producedUnitEntityManager,
        bool hasEntityManager,
        RuntimeBuildingEntity building)
    {
        if (building == null || building.IsDestroyed || !building.HasOwnerFaction)
            return;

        byte factionId = building.OwnerFactionId;
        if (building.PendingProductions != null)
        {
            for (int i = 0; i < building.PendingProductions.Count; i++)
            {
                RuntimeBuildingEntity.PendingProduction pending = building.PendingProductions[i];
                if (pending?.Prefab == null)
                    continue;

                FixedString128Bytes unitId = ResolveBoundaryId(pending.Prefab, pending.Prefab.name);
                IncrementExistingProductionSummary(buffer, factionId, unitId, 0, 1);
            }
        }

        if (!hasEntityManager || building.ProducedUnits == null)
            return;

        runtimeQueryContext.ProductionSystem?.PruneProducedUnits(
            building.ProducedUnits,
            building.ProducedUnitSlots,
            building.ProducedUnitPrefabs,
            producedUnitEntityManager);
        for (int i = 0; i < building.ProducedUnits.Count; i++)
        {
            Entity unit = building.ProducedUnits[i];
            if (producedUnitEntityManager.HasComponent<Faction>(unit) &&
                producedUnitEntityManager.GetComponentData<Faction>(unit).Id != factionId)
            {
                continue;
            }

            if (!TryResolveProducedUnitId(building, unit, producedUnitEntityManager, out FixedString128Bytes unitId))
                continue;

            IncrementExistingProductionSummary(buffer, factionId, unitId, 1, 0);
        }
    }

    private void PublishConfiguredUnitProductionSummaryRows(
        BuildingDefinitionSystem definitionSystem,
        DynamicBuffer<BuildingRuntimeUnitProductionSummary> buffer)
    {
        for (int factionIndex = 0; factionIndex < _factionIds.Count; factionIndex++)
        {
            byte factionId = _factionIds[factionIndex];
            for (int i = 0; i < definitionSystem.ConfiguredUnitCount; i++)
            {
                if (!definitionSystem.TryGetConfiguredUnitReadModel(
                        i,
                        out GameObject prefab,
                        out string displayName,
                        out _,
                        out _,
                        out _) ||
                    prefab == null)
                {
                    continue;
                }

                buffer.Add(new BuildingRuntimeUnitProductionSummary
                {
                    FactionId = factionId,
                    UnitId = ResolveBoundaryId(prefab, displayName),
                    ProducedCount = 0,
                    QueuedCount = 0
                });
            }
        }
    }

    private static void IncrementExistingProductionSummary(
        DynamicBuffer<BuildingRuntimeUnitProductionSummary> buffer,
        byte factionId,
        FixedString128Bytes unitId,
        int producedDelta,
        int queuedDelta)
    {
        for (int i = 0; i < buffer.Length; i++)
        {
            BuildingRuntimeUnitProductionSummary summary = buffer[i];
            if (summary.FactionId != factionId || !summary.UnitId.Equals(unitId))
                continue;

            summary.ProducedCount += producedDelta;
            summary.QueuedCount += queuedDelta;
            buffer[i] = summary;
            return;
        }
    }

    private bool TryResolveProducedUnitId(
        RuntimeBuildingEntity building,
        Entity unit,
        EntityManager em,
        out FixedString128Bytes unitId)
    {
        if (building?.ProducedUnitPrefabs != null &&
            building.ProducedUnitPrefabs.TryGetValue(unit, out GameObject prefab) &&
            prefab != null)
        {
            unitId = ResolveBoundaryId(prefab, prefab.name);
            return true;
        }

        if (unit != Entity.Null &&
            em.Exists(unit) &&
            em.HasComponent<UnitSourcePrefabKey>(unit))
        {
            unitId = ToFixedString128(BuildingDefinitionSystem.NormalizeSpawnableKey(
                em.GetComponentData<UnitSourcePrefabKey>(unit).Value.ToString()));
            return unitId.Length > 0;
        }

        unitId = default;
        return false;
    }

    private void PublishFactionProductionSpawnPointsReadModel(
        EntityManager em,
        Entity boundaryEntity,
        IReadOnlyDictionary<int, RuntimeBuildingEntity> runtimeBuildings)
    {
        DynamicBuffer<BuildingFactionProductionSpawnPointReadModel> buffer =
            EnsureBoundaryBuffer<BuildingFactionProductionSpawnPointReadModel>(em, boundaryEntity);
        buffer.Clear();

        if (!TryGetGridConfig(em, out GridConfig grid))
            return;

        if (runtimeBuildings is Dictionary<int, RuntimeBuildingEntity> runtimeBuildingMap)
        {
            foreach (KeyValuePair<int, RuntimeBuildingEntity> entry in runtimeBuildingMap)
                PublishFactionProductionSpawnPointsForBuilding(buffer, grid, entry.Value);
        }
        else
        {
            foreach (KeyValuePair<int, RuntimeBuildingEntity> entry in runtimeBuildings)
                PublishFactionProductionSpawnPointsForBuilding(buffer, grid, entry.Value);
        }
    }

    private void PublishFactionProductionSpawnPointsForBuilding(
        DynamicBuffer<BuildingFactionProductionSpawnPointReadModel> buffer,
        GridConfig grid,
        RuntimeBuildingEntity building)
    {
        if (building == null ||
            building.IsDestroyed ||
            !building.HasOwnerFaction ||
            building.Instance == null ||
            building.Definition == null ||
            building.Definition.Prefab == null ||
            building.ProductionSpawnLocalPositions == null ||
            building.ProductionSpawnLocalPositions.Length == 0)
        {
            return;
        }

        FixedString128Bytes buildingId = ResolveBoundaryId(building.Definition.Prefab, building.Definition.DisplayName);
        for (int i = 0; i < building.ProductionSpawnLocalPositions.Length; i++)
        {
            Vector3 world = building.Instance.transform.TransformPoint(building.ProductionSpawnLocalPositions[i]);
            int2 cell = GridUtils.WorldToCell(grid, world);
            if (!GridUtils.InBounds(cell, grid.Width, grid.Height))
                continue;

            buffer.Add(new BuildingFactionProductionSpawnPointReadModel
            {
                FactionId = building.OwnerFactionId,
                BuildingId = buildingId,
                SlotIndex = i,
                Cell = cell,
                WorldPosition = new float3(world.x, world.y, world.z)
            });
        }
    }

    private static bool TryResolveConfiguredBuildingDefinition(
        BuildingDefinitionSystem definitionSystem,
        string buildingId,
        out BuildingDefinition definition)
    {
        definition = null;
        if (definitionSystem == null)
            return false;

        if (definitionSystem.TryResolveConfiguredSpawnablePrefab(buildingId, out GameObject prefab) &&
            definitionSystem.TryGetConfiguredDefinition(prefab, out definition))
        {
            return true;
        }

        string normalized = BuildingDefinitionSystem.NormalizeSpawnableKey(buildingId);
        for (int i = 0; i < definitionSystem.ConfiguredSpawnableCount; i++)
        {
            if (!definitionSystem.TryGetConfiguredDefinition(i, out BuildingDefinition candidate))
                continue;

            if (!BuildingDefinitionSystem.RuntimeDefinitionMatchesId(candidate, normalized))
                continue;

            definition = candidate;
            return true;
        }

        return false;
    }

    private void RefreshFactionIds(IReadOnlyDictionary<int, RuntimeBuildingEntity> runtimeBuildings)
    {
        _factionIds.Clear();
        if (runtimeBuildings is Dictionary<int, RuntimeBuildingEntity> runtimeBuildingMap)
        {
            foreach (KeyValuePair<int, RuntimeBuildingEntity> pair in runtimeBuildingMap)
                AddFactionIdForBuilding(pair.Value);
        }
        else
        {
            foreach (KeyValuePair<int, RuntimeBuildingEntity> pair in runtimeBuildings)
                AddFactionIdForBuilding(pair.Value);
        }
    }

    private void AddFactionIdForBuilding(RuntimeBuildingEntity building)
    {
        if (building == null || !building.HasOwnerFaction)
            return;

        AddFactionId(building.OwnerFactionId);
    }

    private void AddFactionId(byte factionId)
    {
        for (int i = 0; i < _factionIds.Count; i++)
        {
            if (_factionIds[i] == factionId)
                return;
        }

        _factionIds.Add(factionId);
    }

    private static int ComputeRuntimeBuildingSignature(IReadOnlyDictionary<int, RuntimeBuildingEntity> runtimeBuildings)
    {
        if (runtimeBuildings == null)
            return 0;

        unchecked
        {
            int hash = 17;
            if (runtimeBuildings is Dictionary<int, RuntimeBuildingEntity> runtimeBuildingMap)
            {
                foreach (KeyValuePair<int, RuntimeBuildingEntity> pair in runtimeBuildingMap)
                    hash = AddRuntimeBuildingSignature(hash, pair.Key, pair.Value);
            }
            else
            {
                foreach (KeyValuePair<int, RuntimeBuildingEntity> pair in runtimeBuildings)
                    hash = AddRuntimeBuildingSignature(hash, pair.Key, pair.Value);
            }

            return hash;
        }
    }

    private static int AddRuntimeBuildingSignature(int hash, int key, RuntimeBuildingEntity building)
    {
        hash = (hash * 31) + key;
        if (building == null)
            return hash;

        hash = (hash * 31) + (building.IsDestroyed ? 1 : 0);
        hash = (hash * 31) + (building.HasOwnerFaction ? building.OwnerFactionId : 255);
        hash = (hash * 31) + (building.ProductionSpawnLocalPositions?.Length ?? 0);
        return hash;
    }

    private static bool TryGetBoundaryEntity(EntityManager em, EntityQuery boundaryQuery, out Entity boundaryEntity)
    {
        boundaryEntity = Entity.Null;
        if (!boundaryQuery.IsEmptyIgnoreFilter)
            boundaryEntity = boundaryQuery.GetSingletonEntity();

        return boundaryEntity != Entity.Null && em.Exists(boundaryEntity);
    }

    private static bool TryGetGridConfig(EntityManager em, out GridConfig grid)
    {
        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<GridConfig>());
        EntityTypeHandle entityType = em.GetEntityTypeHandle();
        using NativeArray<ArchetypeChunk> chunks = query.ToArchetypeChunkArray(Allocator.Temp);
        Entity gridEntity = Entity.Null;
        for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
        {
            NativeArray<Entity> gridEntities = chunks[chunkIndex].GetNativeArray(entityType);
            for (int i = 0; i < gridEntities.Length; i++)
            {
                Entity candidate = gridEntities[i];
                if (gridEntity == Entity.Null)
                    gridEntity = candidate;
                if (!em.HasComponent<RuntimeGridBootstrapGridTag>(candidate))
                {
                    gridEntity = candidate;
                    break;
                }
            }

            if (gridEntity != Entity.Null && !em.HasComponent<RuntimeGridBootstrapGridTag>(gridEntity))
                break;
        }

        if (gridEntity == Entity.Null)
        {
            grid = default;
            return false;
        }

        grid = em.GetComponentData<GridConfig>(gridEntity);
        return true;
    }

    private static DynamicBuffer<T> EnsureBoundaryBuffer<T>(EntityManager em, Entity entity)
        where T : unmanaged, IBufferElementData
    {
        if (!em.HasBuffer<T>(entity))
            em.AddBuffer<T>(entity);

        return em.GetBuffer<T>(entity);
    }

    private FixedString128Bytes ResolveBoundaryId(GameObject prefab, string fallback)
    {
        if (prefab != null)
        {
            if (_boundaryIdsByPrefab.TryGetValue(prefab, out FixedString128Bytes cached))
                return cached;

            string normalized = BuildingDefinitionSystem.NormalizeSpawnableKey(prefab.name);
            FixedString128Bytes resolved = ToFixedString128(string.IsNullOrEmpty(normalized) ? fallback : normalized);
            _boundaryIdsByPrefab[prefab] = resolved;
            return resolved;
        }

        string fallbackKey = fallback ?? string.Empty;
        if (_boundaryIdsByFallback.TryGetValue(fallbackKey, out FixedString128Bytes fallbackCached))
            return fallbackCached;

        string normalizedFallback = BuildingDefinitionSystem.NormalizeSpawnableKey(fallback);
        FixedString128Bytes fallbackResolved = ToFixedString128(string.IsNullOrEmpty(normalizedFallback) ? fallback : normalizedFallback);
        _boundaryIdsByFallback[fallbackKey] = fallbackResolved;
        return fallbackResolved;
    }

    private static FixedString128Bytes ToFixedString128(string value)
    {
        return new FixedString128Bytes(value ?? string.Empty);
    }
}
