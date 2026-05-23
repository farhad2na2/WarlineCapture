using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public sealed class BuildingRuntimeBoundarySystem
{
    private const float PublishIntervalSeconds = 0.5f;

    private readonly List<byte> _factionIds = new();
    private float _nextPublishAt;

    internal void Update(
        BuildingPlacementSystem buildingPlacement,
        EntityManager em,
        EntityQuery boundaryQuery,
        IReadOnlyDictionary<int, RuntimeBuildingData> runtimeBuildings,
        float now)
    {
        if (buildingPlacement == null || runtimeBuildings == null)
            return;

        if (!TryGetBoundaryEntity(em, boundaryQuery, out Entity boundaryEntity))
            return;

        ProcessRequests(buildingPlacement, em, boundaryEntity);
        PublishReadModelIfDue(buildingPlacement, em, boundaryEntity, runtimeBuildings, now);
    }

    private void ProcessRequests(BuildingPlacementSystem buildingPlacement, EntityManager em, Entity boundaryEntity)
    {
        DynamicBuffer<BuildingFactionUnitProductionRequest> productionRequests =
            EnsureBoundaryBuffer<BuildingFactionUnitProductionRequest>(em, boundaryEntity);
        for (int i = 0; i < productionRequests.Length; i++)
        {
            BuildingFactionUnitProductionRequest request = productionRequests[i];
            if (request.Status != BuildingFactionUnitProductionRequest.Pending)
                continue;

            bool queued = buildingPlacement.TryQueueFactionUnitProduction(
                request.FactionId,
                request.UnitId.ToString(),
                out BuildingPlacementSystem.FactionUnitProductionResult result);
            request.Status = queued
                ? BuildingFactionUnitProductionRequest.Succeeded
                : BuildingFactionUnitProductionRequest.Failed;
            request.ResultCode = (byte)result.Code;
            request.ProducerDisplayName = ToFixedString128(result.ProducerDisplayName);
            request.UnitDisplayName = ToFixedString128(result.UnitDisplayName);
            request.Cost = result.Cost;
            request.QueueCount = result.QueueCount;
            request.ProducedCount = result.ProducedCount;
            productionRequests[i] = request;
        }

        DynamicBuffer<BuildingRuntimeSpawnRequest> spawnRequests =
            EnsureBoundaryBuffer<BuildingRuntimeSpawnRequest>(em, boundaryEntity);
        for (int i = 0; i < spawnRequests.Length; i++)
        {
            BuildingRuntimeSpawnRequest request = spawnRequests[i];
            if (request.Status != BuildingRuntimeSpawnRequest.Pending)
                continue;

            if (!buildingPlacement.TryGetConfiguredSpawnable(request.BuildingId.ToString(), out BuildingPlacementSystem.ConfiguredSpawnableEntry spawnable) ||
                spawnable.Prefab == null ||
                !spawnable.CanRequest)
            {
                request.Status = BuildingRuntimeSpawnRequest.Failed;
                request.ResultCode = BuildingRuntimeSpawnRequest.MissingConfig;
                spawnRequests[i] = request;
                continue;
            }

            bool placed = buildingPlacement.TrySpawnRuntimeBuilding(
                spawnable.Prefab,
                new Vector2Int(request.PreferredOrigin.x, request.PreferredOrigin.y),
                out int buildingId,
                out Vector2Int actualOrigin,
                out Vector2Int actualFootprint,
                spawnable.DisplayName,
                spawnable.DisplayName,
                null,
                500,
                false,
                request.FactionId,
                request.RotateVertical != 0);

            request.Status = placed
                ? BuildingRuntimeSpawnRequest.Succeeded
                : BuildingRuntimeSpawnRequest.Failed;
            request.ResultCode = placed ? (byte)0 : BuildingRuntimeSpawnRequest.Blocked;
            request.BuildingRuntimeId = placed ? buildingId : 0;
            request.ActualOrigin = placed ? new int2(actualOrigin.x, actualOrigin.y) : default;
            request.ActualFootprint = placed ? new int2(actualFootprint.x, actualFootprint.y) : default;
            spawnRequests[i] = request;
        }
    }

    private void PublishReadModelIfDue(
        BuildingPlacementSystem buildingPlacement,
        EntityManager em,
        Entity boundaryEntity,
        IReadOnlyDictionary<int, RuntimeBuildingData> runtimeBuildings,
        float now)
    {
        if (now < _nextPublishAt)
            return;

        _nextPublishAt = now + PublishIntervalSeconds;
        PublishConfiguredSpawnablesReadModel(buildingPlacement, em, boundaryEntity);
        PublishConfiguredUnitsReadModel(buildingPlacement, em, boundaryEntity);
        PublishRuntimeFactionSummaries(buildingPlacement, em, boundaryEntity, runtimeBuildings);
        PublishRuntimeOwnedBuildingSummaries(buildingPlacement, em, boundaryEntity);
        PublishRuntimeUnitProductionSummaries(buildingPlacement, em, boundaryEntity);
    }

    private void PublishConfiguredSpawnablesReadModel(BuildingPlacementSystem buildingPlacement, EntityManager em, Entity boundaryEntity)
    {
        DynamicBuffer<BuildingConfiguredSpawnableReadModel> buffer =
            EnsureBoundaryBuffer<BuildingConfiguredSpawnableReadModel>(em, boundaryEntity);
        buffer.Clear();

        for (int i = 0; i < buildingPlacement.ConfiguredSpawnableCount; i++)
        {
            if (!buildingPlacement.TryGetConfiguredSpawnable(i, out BuildingPlacementSystem.ConfiguredSpawnableEntry entry) || entry.Prefab == null)
                continue;

            buffer.Add(new BuildingConfiguredSpawnableReadModel
            {
                BuildingId = ResolveBoundaryId(entry.Prefab, entry.DisplayName),
                DisplayName = ToFixedString128(entry.DisplayName),
                Price = Mathf.Max(0, entry.Price),
                CanRequest = entry.CanRequest ? (byte)1 : (byte)0
            });
        }
    }

    private void PublishConfiguredUnitsReadModel(BuildingPlacementSystem buildingPlacement, EntityManager em, Entity boundaryEntity)
    {
        DynamicBuffer<BuildingConfiguredUnitReadModel> buffer =
            EnsureBoundaryBuffer<BuildingConfiguredUnitReadModel>(em, boundaryEntity);
        buffer.Clear();

        for (int i = 0; i < buildingPlacement.ConfiguredUnitCount; i++)
        {
            if (!buildingPlacement.TryGetConfiguredUnit(i, out BuildingPlacementSystem.ConfiguredUnitEntry entry) || entry.Prefab == null)
                continue;

            buffer.Add(new BuildingConfiguredUnitReadModel
            {
                UnitId = ResolveBoundaryId(entry.Prefab, entry.DisplayName),
                DisplayName = ToFixedString128(entry.DisplayName),
                Price = Mathf.Max(0, entry.Price),
                CanRequest = entry.CanRequest ? (byte)1 : (byte)0,
                IsVehicle = entry.IsVehicle ? (byte)1 : (byte)0
            });
        }
    }

    private void PublishRuntimeFactionSummaries(
        BuildingPlacementSystem buildingPlacement,
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
            buildingPlacement.TryGetFactionResourceEconomy(factionId, out BuildingPlacementSystem.FactionResourceEconomySnapshot economy);
            buffer.Add(new BuildingRuntimeFactionSummary
            {
                FactionId = factionId,
                BuildingCount = buildingPlacement.CountRuntimeBuildingsForFaction(factionId),
                StoredOilBarrels = economy.StoredOilBarrels,
                StoredFuelBarrels = economy.StoredFuelBarrels,
                OilBarrelsPerDay = economy.OilBarrelsPerDay,
                FuelBarrelsPerDay = economy.FuelBarrelsPerDay
            });
        }
    }

    private void PublishRuntimeOwnedBuildingSummaries(BuildingPlacementSystem buildingPlacement, EntityManager em, Entity boundaryEntity)
    {
        DynamicBuffer<BuildingRuntimeOwnedBuildingSummary> buffer =
            EnsureBoundaryBuffer<BuildingRuntimeOwnedBuildingSummary>(em, boundaryEntity);
        buffer.Clear();

        for (int factionIndex = 0; factionIndex < _factionIds.Count; factionIndex++)
        {
            byte factionId = _factionIds[factionIndex];
            for (int i = 0; i < buildingPlacement.ConfiguredSpawnableCount; i++)
            {
                if (!buildingPlacement.TryGetConfiguredSpawnable(i, out BuildingPlacementSystem.ConfiguredSpawnableEntry entry) || entry.Prefab == null)
                    continue;

                FixedString128Bytes buildingId = ResolveBoundaryId(entry.Prefab, entry.DisplayName);
                buffer.Add(new BuildingRuntimeOwnedBuildingSummary
                {
                    FactionId = factionId,
                    BuildingId = buildingId,
                    Count = buildingPlacement.CountRuntimeBuildingsForFaction(factionId, buildingId.ToString())
                });
            }
        }
    }

    private void PublishRuntimeUnitProductionSummaries(BuildingPlacementSystem buildingPlacement, EntityManager em, Entity boundaryEntity)
    {
        DynamicBuffer<BuildingRuntimeUnitProductionSummary> buffer =
            EnsureBoundaryBuffer<BuildingRuntimeUnitProductionSummary>(em, boundaryEntity);
        buffer.Clear();

        for (int factionIndex = 0; factionIndex < _factionIds.Count; factionIndex++)
        {
            byte factionId = _factionIds[factionIndex];
            for (int i = 0; i < buildingPlacement.ConfiguredUnitCount; i++)
            {
                if (!buildingPlacement.TryGetConfiguredUnit(i, out BuildingPlacementSystem.ConfiguredUnitEntry entry) || entry.Prefab == null)
                    continue;

                FixedString128Bytes unitId = ResolveBoundaryId(entry.Prefab, entry.DisplayName);
                string unitIdString = unitId.ToString();
                buffer.Add(new BuildingRuntimeUnitProductionSummary
                {
                    FactionId = factionId,
                    UnitId = unitId,
                    ProducedCount = buildingPlacement.CountRuntimeProducedUnitsForFaction(factionId, unitIdString),
                    QueuedCount = buildingPlacement.CountPendingProductionsForFaction(factionId, unitIdString)
                });
            }
        }
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
