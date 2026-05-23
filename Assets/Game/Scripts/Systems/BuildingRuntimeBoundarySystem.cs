using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public sealed class BuildingRuntimeBoundarySystem
{
    private const float PublishIntervalSeconds = 0.5f;

    private readonly List<byte> _factionIds = new();
    private readonly List<int> _pendingSpawnRequestIndices = new();
    private float _nextPublishAt;
    private bool _forcePublishNextUpdate;

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
        IReadOnlyDictionary<int, RuntimeBuildingData> runtimeBuildings,
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
        IReadOnlyDictionary<int, RuntimeBuildingData> runtimeBuildings,
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
        IReadOnlyDictionary<int, RuntimeBuildingData> runtimeBuildings,
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

            bool queued = productionRequestSystem.QueueFactionUnitProductionRequest(
                productionRequestContext,
                request.FactionId,
                request.UnitId.ToString(),
                em,
                now,
                ref request);
            request.Status = queued
                ? BuildingFactionUnitProductionRequest.Succeeded
                : BuildingFactionUnitProductionRequest.Failed;
            if (request.ResultCode != BuildingFactionUnitProductionRequest.MissingUnitConfig)
            {
                string unitId = request.UnitId.ToString();
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

        for (int pendingIndex = 0; pendingIndex < _pendingSpawnRequestIndices.Count; pendingIndex++)
        {
            int i = _pendingSpawnRequestIndices[pendingIndex];
            spawnRequests = EnsureBoundaryBuffer<BuildingRuntimeSpawnRequest>(em, boundaryEntity);
            if ((uint)i >= (uint)spawnRequests.Length)
                continue;

            BuildingRuntimeSpawnRequest request = spawnRequests[i];
            if (request.Status != BuildingRuntimeSpawnRequest.Pending)
                continue;

            if (!TryResolveConfiguredBuildingDefinition(definitionSystem, request.BuildingId.ToString(), out BuildingDefinition definition))
            {
                request.Status = BuildingRuntimeSpawnRequest.Failed;
                request.ResultCode = BuildingRuntimeSpawnRequest.MissingConfig;
                WriteRuntimeSpawnRequest(em, boundaryEntity, i, request);
                continue;
            }

            var spawnable = BuildingDefinitionSystem.BuildConfiguredSpawnableEntry(definition);
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
        IReadOnlyDictionary<int, RuntimeBuildingData> runtimeBuildings,
        float now)
    {
        if (!_forcePublishNextUpdate && now < _nextPublishAt)
            return;

        _forcePublishNextUpdate = false;
        _nextPublishAt = now + PublishIntervalSeconds;
        PublishConfiguredSpawnablesReadModel(definitionSystem, em, boundaryEntity);
        PublishConfiguredUnitsReadModel(definitionSystem, em, boundaryEntity);
        PublishRuntimeFactionSummaries(factionResourceSystem, runtimeQuerySystem, runtimeQueryContext, em, boundaryEntity, runtimeBuildings);
        PublishRuntimeOwnedBuildingSummaries(definitionSystem, runtimeQuerySystem, runtimeQueryContext, em, boundaryEntity);
        PublishRuntimeUnitProductionSummaries(definitionSystem, runtimeQuerySystem, runtimeQueryContext, em, boundaryEntity);
        PublishFactionProductionSpawnPointsReadModel(em, boundaryEntity, runtimeBuildings);
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

            var entry = BuildingDefinitionSystem.BuildConfiguredSpawnableEntry(definition);
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
        IReadOnlyDictionary<int, RuntimeBuildingData> runtimeBuildings)
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
        BuildingRuntimeQuerySystem runtimeQuerySystem,
        BuildingRuntimeQuerySystem.Context runtimeQueryContext,
        EntityManager em,
        Entity boundaryEntity)
    {
        DynamicBuffer<BuildingRuntimeOwnedBuildingSummary> buffer =
            EnsureBoundaryBuffer<BuildingRuntimeOwnedBuildingSummary>(em, boundaryEntity);
        buffer.Clear();

        for (int factionIndex = 0; factionIndex < _factionIds.Count; factionIndex++)
        {
            byte factionId = _factionIds[factionIndex];
            for (int i = 0; i < definitionSystem.ConfiguredSpawnableCount; i++)
            {
                if (!definitionSystem.TryGetConfiguredDefinition(i, out BuildingDefinition definition))
                    continue;

                var entry = BuildingDefinitionSystem.BuildConfiguredSpawnableEntry(definition);
                if (entry.Prefab == null)
                    continue;

                FixedString128Bytes buildingId = ResolveBoundaryId(entry.Prefab, entry.DisplayName);
                buffer.Add(new BuildingRuntimeOwnedBuildingSummary
                {
                    FactionId = factionId,
                    BuildingId = buildingId,
                    Count = runtimeQuerySystem.CountRuntimeBuildingsForFaction(runtimeQueryContext, factionId, buildingId.ToString())
                });
            }
        }
    }

    private void PublishRuntimeUnitProductionSummaries(
        BuildingDefinitionSystem definitionSystem,
        BuildingRuntimeQuerySystem runtimeQuerySystem,
        BuildingRuntimeQuerySystem.Context runtimeQueryContext,
        EntityManager em,
        Entity boundaryEntity)
    {
        DynamicBuffer<BuildingRuntimeUnitProductionSummary> buffer =
            EnsureBoundaryBuffer<BuildingRuntimeUnitProductionSummary>(em, boundaryEntity);
        buffer.Clear();

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

                FixedString128Bytes unitId = ResolveBoundaryId(prefab, displayName);
                string unitIdString = unitId.ToString();
                buffer.Add(new BuildingRuntimeUnitProductionSummary
                {
                    FactionId = factionId,
                    UnitId = unitId,
                    ProducedCount = runtimeQuerySystem.CountRuntimeProducedUnitsForFaction(runtimeQueryContext, factionId, unitIdString),
                    QueuedCount = runtimeQuerySystem.CountPendingProductionsForFaction(runtimeQueryContext, factionId, unitIdString)
                });
            }
        }
    }

    private void PublishFactionProductionSpawnPointsReadModel(
        EntityManager em,
        Entity boundaryEntity,
        IReadOnlyDictionary<int, RuntimeBuildingData> runtimeBuildings)
    {
        DynamicBuffer<BuildingFactionProductionSpawnPointReadModel> buffer =
            EnsureBoundaryBuffer<BuildingFactionProductionSpawnPointReadModel>(em, boundaryEntity);
        buffer.Clear();

        if (!TryGetGridConfig(em, out GridConfig grid))
            return;

        foreach (KeyValuePair<int, RuntimeBuildingData> entry in runtimeBuildings)
        {
            RuntimeBuildingData building = entry.Value;
            if (building == null ||
                building.IsDestroyed ||
                !building.HasOwnerFaction ||
                building.Instance == null ||
                building.Definition == null ||
                building.Definition.Prefab == null ||
                building.ProductionSpawnLocalPositions == null ||
                building.ProductionSpawnLocalPositions.Length == 0)
            {
                continue;
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

    private void RefreshFactionIds(IReadOnlyDictionary<int, RuntimeBuildingData> runtimeBuildings)
    {
        _factionIds.Clear();
        foreach (KeyValuePair<int, RuntimeBuildingData> pair in runtimeBuildings)
        {
            RuntimeBuildingData building = pair.Value;
            if (building == null || !building.HasOwnerFaction)
                continue;

            AddFactionId(building.OwnerFactionId);
        }
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
        if (query.IsEmptyIgnoreFilter)
        {
            grid = default;
            return false;
        }

        grid = em.GetComponentData<GridConfig>(query.GetSingletonEntity());
        return true;
    }

    private static DynamicBuffer<T> EnsureBoundaryBuffer<T>(EntityManager em, Entity entity)
        where T : unmanaged, IBufferElementData
    {
        if (!em.HasBuffer<T>(entity))
            em.AddBuffer<T>(entity);

        return em.GetBuffer<T>(entity);
    }

    private static FixedString128Bytes ResolveBoundaryId(GameObject prefab, string fallback)
    {
        string source = prefab != null ? prefab.name : fallback;
        string normalized = BuildingDefinitionSystem.NormalizeSpawnableKey(source);
        return ToFixedString128(string.IsNullOrEmpty(normalized) ? fallback : normalized);
    }

    private static FixedString128Bytes ToFixedString128(string value)
    {
        return new FixedString128Bytes(value ?? string.Empty);
    }
}
