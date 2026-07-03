using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Game.Components;

namespace Game.Runtime
{
    public sealed class BuildingRuntimeProcessingCompositionSystemHelper
    {
        private const float PublishIntervalSeconds = 0.125f;
        private const float IdleRequestProbeIntervalSeconds = 0.1f;
        private const int MaxRuntimeSpawnRequestsPerUpdate = 16;

        private readonly List<byte> _factionIds = new();
        private readonly List<int> _pendingSpawnRequestIndices = new();
        private readonly Dictionary<GameObject, FixedString128Bytes> _boundaryIdsByPrefab = new();
        private readonly Dictionary<string, FixedString128Bytes> _boundaryIdsByFallback = new();
        private readonly Dictionary<ProductionSummaryKey, ProductionSummaryCounts> _productionSummaryCountsScratch = new();
        private readonly HashSet<int> _producedReadModelBuildingIdsScratch = new();
        private readonly BuildingRuntimeSurfaceOverlaySystem _surfaceOverlaySystem = new();
        private float _nextPublishAt;
        private bool _forcePublishNextUpdate;
        private bool _configuredReadModelsPublished;
        private int _lastPublishedRuntimeBuildingSignature = int.MinValue;
        private int _lastPublishedOwnedBuildingSummarySignature = int.MinValue;
        private float _nextResourceSellProbeAt;
        private float _nextProductionRequestProbeAt;
        private float _nextRuntimeSpawnRequestProbeAt;
        private PublishPhase _nextPublishPhase = PublishPhase.FactionSummaries;
        private Unity.Entities.World _cachedBoundaryWorld;
        private Entity _cachedBoundaryEntity;

        private enum PublishPhase : byte
        {
            FactionSummaries = 0,
            OwnedBuildingSummaries = 1,
            UnitProductionSummaries = 2,
            BuildingSetReadModels = 3
        }

        private readonly struct ProductionSummaryKey : IEquatable<ProductionSummaryKey>
        {
            public readonly byte FactionId;
            public readonly FixedString128Bytes UnitId;

            public ProductionSummaryKey(byte factionId, FixedString128Bytes unitId)
            {
                FactionId = factionId;
                UnitId = unitId;
            }

            public bool Equals(ProductionSummaryKey other)
            {
                return FactionId == other.FactionId && UnitId.Equals(other.UnitId);
            }

            public override bool Equals(object obj)
            {
                return obj is ProductionSummaryKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (FactionId * 397) ^ UnitId.GetHashCode();
                }
            }
        }

        private struct ProductionSummaryCounts
        {
            public int ProducedCount;
            public int QueuedCount;
        }

        internal void Update(
            BuildingDefinitionPrefabSystemHelper definitionSystem,
            BuildingRuntimeSpawnCompositionSystemHelper runtimeSpawnSystem,
            BuildingRuntimeSpawnCompositionSystemHelper.Context runtimeSpawnContext,
            BuildingProductionRequestSystemHelper productionRequestSystem,
            BuildingProductionRequestSystemHelper.Context productionRequestContext,
            BuildingRuntimeReadModelCompositionSystemHelper runtimeQuerySystem,
            BuildingRuntimeReadModelCompositionSystemHelper.Context runtimeQueryContext,
            FactionResourceCompositionSystemHelper factionResourceSystem,
            EntityManager em,
            EntityQuery boundaryQuery,
            IReadOnlyDictionary<int, RuntimeBuildingEntity> runtimeBuildings,
            float now,
            int frameCount)
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
                now,
                frameCount);
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
            BuildingDefinitionPrefabSystemHelper definitionSystem,
            BuildingRuntimeSpawnCompositionSystemHelper runtimeSpawnSystem,
            BuildingRuntimeSpawnCompositionSystemHelper.Context runtimeSpawnContext,
            BuildingProductionRequestSystemHelper productionRequestSystem,
            BuildingProductionRequestSystemHelper.Context productionRequestContext,
            BuildingRuntimeReadModelCompositionSystemHelper runtimeQuerySystem,
            BuildingRuntimeReadModelCompositionSystemHelper.Context runtimeQueryContext,
            FactionResourceCompositionSystemHelper factionResourceSystem,
            IReadOnlyDictionary<int, RuntimeBuildingEntity> runtimeBuildings,
            EntityManager em,
            Entity boundaryEntity,
            float now,
            int frameCount)
        {
            ProcessResourceSellRequests(factionResourceSystem, runtimeBuildings, em, boundaryEntity, now);
            ProcessUiProductionRequests(productionRequestSystem, productionRequestContext, em, frameCount, now);
            ProcessProductionRequests(productionRequestSystem, productionRequestContext, runtimeQuerySystem, runtimeQueryContext, em, boundaryEntity, now);
            ProcessRuntimeSpawnRequests(definitionSystem, runtimeSpawnSystem, runtimeSpawnContext, em, boundaryEntity, now);
        }

        internal void ProcessRuntimeSpawnRequestsForBoundary(
            BuildingDefinitionPrefabSystemHelper definitionSystem,
            BuildingRuntimeSpawnCompositionSystemHelper runtimeSpawnSystem,
            BuildingRuntimeSpawnCompositionSystemHelper.Context runtimeSpawnContext,
            EntityManager em,
            Entity boundaryEntity)
        {
            if (definitionSystem == null ||
                runtimeSpawnSystem == null ||
                boundaryEntity == Entity.Null ||
                !em.Exists(boundaryEntity))
            {
                return;
            }

            ProcessRuntimeSpawnRequests(definitionSystem, runtimeSpawnSystem, runtimeSpawnContext, em, boundaryEntity, now: 0f, forceScan: true);
        }

        private static void ProcessUiProductionRequests(
            BuildingProductionRequestSystemHelper productionRequestSystem,
            BuildingProductionRequestSystemHelper.Context productionRequestContext,
            EntityManager em,
            int frameCount,
            float now)
        {
            productionRequestSystem.ProcessPendingUiProductionCommandsIfPresent(
                em,
                productionRequestContext,
                frameCount,
                now);
            productionRequestSystem.ProcessPendingUiCampItemCommandsIfPresent(
                em,
                productionRequestContext,
                frameCount);
        }

        private void ProcessResourceSellRequests(
            FactionResourceCompositionSystemHelper factionResourceSystem,
            IReadOnlyDictionary<int, RuntimeBuildingEntity> runtimeBuildings,
            EntityManager em,
            Entity boundaryEntity,
            float now)
        {
            if (now < _nextResourceSellProbeAt)
                return;

            DynamicBuffer<BuildingFactionResourceSellRequest> sellRequests =
                EnsureBoundaryBuffer<BuildingFactionResourceSellRequest>(em, boundaryEntity);
            bool foundPending = false;
            for (int i = 0; i < sellRequests.Length; i++)
            {
                BuildingFactionResourceSellRequest request = sellRequests[i];
                if (request.Status != BuildingFactionResourceSellRequest.Pending)
                    continue;

                foundPending = true;
                float soldOil = factionResourceSystem.DrainFactionResource(
                    runtimeBuildings,
                    request.FactionId,
                    Mathf.Max(0f, request.RequestedOilBarrels),
                    FactionResourceCompositionSystemHelper.ResourceKind.Oil);
                float soldFuel = factionResourceSystem.DrainFactionResource(
                    runtimeBuildings,
                    request.FactionId,
                    Mathf.Max(0f, request.RequestedFuelBarrels),
                    FactionResourceCompositionSystemHelper.ResourceKind.Fuel);
                request.Status = BuildingFactionResourceSellRequest.Succeeded;
                request.ResultCode = soldOil > 0f || soldFuel > 0f
                    ? (byte)0
                    : BuildingFactionResourceSellRequest.NoneSold;
                request.SoldOilBarrels = soldOil;
                request.SoldFuelBarrels = soldFuel;
                sellRequests[i] = request;
            }

            _nextResourceSellProbeAt = foundPending
                ? now
                : now + IdleRequestProbeIntervalSeconds;
        }

        private void ProcessProductionRequests(
            BuildingProductionRequestSystemHelper productionRequestSystem,
            BuildingProductionRequestSystemHelper.Context productionRequestContext,
            BuildingRuntimeReadModelCompositionSystemHelper runtimeQuerySystem,
            BuildingRuntimeReadModelCompositionSystemHelper.Context runtimeQueryContext,
            EntityManager em,
            Entity boundaryEntity,
            float now)
        {
            if (now < _nextProductionRequestProbeAt)
                return;

            DynamicBuffer<BuildingFactionUnitProductionRequest> productionRequests =
                EnsureBoundaryBuffer<BuildingFactionUnitProductionRequest>(em, boundaryEntity);
            bool foundPending = false;
            for (int i = 0; i < productionRequests.Length; i++)
            {
                BuildingFactionUnitProductionRequest request = productionRequests[i];
                if (request.Status != BuildingFactionUnitProductionRequest.Pending)
                    continue;

                foundPending = true;
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

            _nextProductionRequestProbeAt = foundPending
                ? now
                : now + IdleRequestProbeIntervalSeconds;
        }

        private void ProcessRuntimeSpawnRequests(
            BuildingDefinitionPrefabSystemHelper definitionSystem,
            BuildingRuntimeSpawnCompositionSystemHelper runtimeSpawnSystem,
            BuildingRuntimeSpawnCompositionSystemHelper.Context runtimeSpawnContext,
            EntityManager em,
            Entity boundaryEntity,
            float now,
            bool forceScan = false)
        {
            if (!forceScan && now < _nextRuntimeSpawnRequestProbeAt)
                return;

            _pendingSpawnRequestIndices.Clear();
            DynamicBuffer<BuildingRuntimeSpawnRequest> spawnRequests =
                EnsureBoundaryBuffer<BuildingRuntimeSpawnRequest>(em, boundaryEntity);
            for (int i = 0; i < spawnRequests.Length; i++)
            {
                if (spawnRequests[i].Status == BuildingRuntimeSpawnRequest.Pending)
                    _pendingSpawnRequestIndices.Add(i);
            }

            int processedRequests = 0;
            if (_pendingSpawnRequestIndices.Count == 0)
            {
                if (!forceScan)
                    _nextRuntimeSpawnRequestProbeAt = now + IdleRequestProbeIntervalSeconds;
                return;
            }

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

                if (!TryResolveConfiguredBuildingDefinition(definitionSystem, request.BuildingId.ToString(), out BuildingDefinition definition))
                {
                    if (definitionSystem.ConfiguredSpawnableCount == 0)
                        continue;

                    processedRequests++;
                    request.Status = BuildingRuntimeSpawnRequest.Failed;
                    request.ResultCode = BuildingRuntimeSpawnRequest.MissingConfig;
                    WriteRuntimeSpawnRequest(em, boundaryEntity, i, request);
                    continue;
                }

                processedRequests++;
                if (!definitionSystem.TryGetConfiguredSpawnable(definition.Prefab, out var spawnable))
                    spawnable = BuildingDefinitionPrefabSystemHelper.BuildConfiguredSpawnableEntry(definition);
                if (spawnable.Prefab == null || !spawnable.CanRequest)
                {
                    request.Status = BuildingRuntimeSpawnRequest.Failed;
                    request.ResultCode = BuildingRuntimeSpawnRequest.MissingConfig;
                    WriteRuntimeSpawnRequest(em, boundaryEntity, i, request);
                    continue;
                }

                // Runtime spawn creates/updates entities, so any DynamicBuffer handle captured before it is invalid afterwards.
                bool placed;
                BuildingRuntimeSpawnCompositionSystemHelper.SpawnRuntimeBuildingResult result = default;
                if (request.RequestKind == BuildingRuntimeSpawnRequest.KindWallRun)
                {
                    int spawned = runtimeSpawnSystem.TrySpawnRuntimeWallRun(
                        runtimeSpawnContext,
                        spawnable.Prefab,
                        new Vector2Int(request.PreferredOrigin.x, request.PreferredOrigin.y),
                        new Vector2Int(request.EndOrigin.x, request.EndOrigin.y),
                        ResolveOwnerFaction(request));
                    placed = spawned > 0;
                    request.SpawnedCount = spawned;
                }
                else if (request.RequestKind == BuildingRuntimeSpawnRequest.KindWallSegment)
                {
                    Vector2Int requestedOrigin = new(request.PreferredOrigin.x, request.PreferredOrigin.y);
                    Vector2Int wallFootprint = default;
                    bool resolvedFootprint = runtimeSpawnSystem.TryGetRuntimeWallSegmentFootprint(
                        runtimeSpawnContext,
                        spawnable.Prefab,
                        request.RotateVertical != 0,
                        out wallFootprint);
                    placed = runtimeSpawnSystem.TrySpawnRuntimeWallSegment(
                        runtimeSpawnContext,
                        spawnable.Prefab,
                        requestedOrigin,
                        request.RotateVertical != 0,
                        ResolveOwnerFaction(request),
                        request.AllowExistingWallOverlap != 0);
                    request.SpawnedCount = placed ? 1 : 0;
                    if (placed && resolvedFootprint)
                        result = new BuildingRuntimeSpawnCompositionSystemHelper.SpawnRuntimeBuildingResult(0, requestedOrigin, wallFootprint);
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
                        ResolveOwnerFaction(request),
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

            if (!forceScan)
            {
                _nextRuntimeSpawnRequestProbeAt = processedRequests >= MaxRuntimeSpawnRequestsPerUpdate
                    ? now
                    : now + IdleRequestProbeIntervalSeconds;
            }
        }

        private static byte? ResolveOwnerFaction(BuildingRuntimeSpawnRequest request)
        {
            return request.HasOwnerFaction != 0 ? request.FactionId : null;
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
            BuildingDefinitionPrefabSystemHelper definitionSystem,
            BuildingRuntimeReadModelCompositionSystemHelper runtimeQuerySystem,
            BuildingRuntimeReadModelCompositionSystemHelper.Context runtimeQueryContext,
            FactionResourceCompositionSystemHelper factionResourceSystem,
            EntityManager em,
            Entity boundaryEntity,
            IReadOnlyDictionary<int, RuntimeBuildingEntity> runtimeBuildings,
            float now)
        {
            bool forcePublish = _forcePublishNextUpdate;
            if (!forcePublish && _configuredReadModelsPublished && now < _nextPublishAt)
                return;

            _forcePublishNextUpdate = false;
            _nextPublishAt = now + PublishIntervalSeconds;
            if (!_configuredReadModelsPublished)
            {
                PublishConfiguredSpawnablesReadModel(definitionSystem, em, boundaryEntity);
                PublishConfiguredUnitsReadModel(definitionSystem, em, boundaryEntity);
                PublishProductionSlotsReadModel(definitionSystem, em, boundaryEntity);
                _configuredReadModelsPublished = true;
                PublishAllDynamicReadModels(
                    definitionSystem,
                    runtimeQuerySystem,
                    runtimeQueryContext,
                    factionResourceSystem,
                    em,
                    boundaryEntity,
                    runtimeBuildings);
                _lastPublishedRuntimeBuildingSignature = ComputeRuntimeBuildingSignature(runtimeBuildings);
                _lastPublishedOwnedBuildingSummarySignature = _lastPublishedRuntimeBuildingSignature;
                _nextPublishPhase = PublishPhase.FactionSummaries;
                return;
            }

            if (forcePublish)
            {
                PublishAllDynamicReadModels(
                    definitionSystem,
                    runtimeQuerySystem,
                    runtimeQueryContext,
                    factionResourceSystem,
                    em,
                    boundaryEntity,
                    runtimeBuildings);
                _lastPublishedRuntimeBuildingSignature = ComputeRuntimeBuildingSignature(runtimeBuildings);
                _lastPublishedOwnedBuildingSummarySignature = _lastPublishedRuntimeBuildingSignature;
                _nextPublishPhase = PublishPhase.FactionSummaries;
                return;
            }

            PublishNextDynamicReadModelPhase(
                definitionSystem,
                runtimeQuerySystem,
                runtimeQueryContext,
                factionResourceSystem,
                em,
                boundaryEntity,
                runtimeBuildings);
        }

        private void PublishAllDynamicReadModels(
            BuildingDefinitionPrefabSystemHelper definitionSystem,
            BuildingRuntimeReadModelCompositionSystemHelper runtimeQuerySystem,
            BuildingRuntimeReadModelCompositionSystemHelper.Context runtimeQueryContext,
            FactionResourceCompositionSystemHelper factionResourceSystem,
            EntityManager em,
            Entity boundaryEntity,
            IReadOnlyDictionary<int, RuntimeBuildingEntity> runtimeBuildings)
        {
            PublishRuntimeFactionSummaries(factionResourceSystem, runtimeQuerySystem, runtimeQueryContext, em, boundaryEntity, runtimeBuildings);
            PublishRuntimeOwnedBuildingSummaries(definitionSystem, runtimeBuildings, em, boundaryEntity);
            PublishRuntimeUnitProductionSummaries(definitionSystem, runtimeQueryContext, em, boundaryEntity);
            PublishBuildingSetReadModels(em, boundaryEntity, runtimeBuildings);
        }

        private void PublishNextDynamicReadModelPhase(
            BuildingDefinitionPrefabSystemHelper definitionSystem,
            BuildingRuntimeReadModelCompositionSystemHelper runtimeQuerySystem,
            BuildingRuntimeReadModelCompositionSystemHelper.Context runtimeQueryContext,
            FactionResourceCompositionSystemHelper factionResourceSystem,
            EntityManager em,
            Entity boundaryEntity,
            IReadOnlyDictionary<int, RuntimeBuildingEntity> runtimeBuildings)
        {
            switch (_nextPublishPhase)
            {
                case PublishPhase.FactionSummaries:
                    PublishRuntimeFactionSummaries(factionResourceSystem, runtimeQuerySystem, runtimeQueryContext, em, boundaryEntity, runtimeBuildings);
                    _nextPublishPhase = PublishPhase.OwnedBuildingSummaries;
                    break;
                case PublishPhase.OwnedBuildingSummaries:
                    int ownedBuildingSignature = ComputeRuntimeBuildingSignature(runtimeBuildings);
                    if (ownedBuildingSignature != _lastPublishedOwnedBuildingSummarySignature)
                    {
                        PublishRuntimeOwnedBuildingSummaries(definitionSystem, runtimeBuildings, em, boundaryEntity);
                        _lastPublishedOwnedBuildingSummarySignature = ownedBuildingSignature;
                    }

                    _nextPublishPhase = PublishPhase.UnitProductionSummaries;
                    break;
                case PublishPhase.UnitProductionSummaries:
                    PublishRuntimeUnitProductionSummaries(definitionSystem, runtimeQueryContext, em, boundaryEntity);
                    _nextPublishPhase = PublishPhase.BuildingSetReadModels;
                    break;
                default:
                    int runtimeBuildingSignature = ComputeRuntimeBuildingSignature(runtimeBuildings);
                    if (runtimeBuildingSignature != _lastPublishedRuntimeBuildingSignature)
                    {
                        PublishBuildingSetReadModels(em, boundaryEntity, runtimeBuildings);
                        _lastPublishedRuntimeBuildingSignature = runtimeBuildingSignature;
                    }

                    _nextPublishPhase = PublishPhase.FactionSummaries;
                    break;
            }
        }

        private void PublishBuildingSetReadModels(
            EntityManager em,
            Entity boundaryEntity,
            IReadOnlyDictionary<int, RuntimeBuildingEntity> runtimeBuildings)
        {
            PublishFactionProductionSpawnPointsReadModel(em, boundaryEntity, runtimeBuildings);
            PublishFactionRunwaysReadModel(em, boundaryEntity, runtimeBuildings);
            _surfaceOverlaySystem.Publish(em, boundaryEntity, runtimeBuildings);
        }

        private void PublishConfiguredSpawnablesReadModel(BuildingDefinitionPrefabSystemHelper definitionSystem, EntityManager em, Entity boundaryEntity)
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

        private void PublishConfiguredUnitsReadModel(BuildingDefinitionPrefabSystemHelper definitionSystem, EntityManager em, Entity boundaryEntity)
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

        private void PublishProductionSlotsReadModel(BuildingDefinitionPrefabSystemHelper definitionSystem, EntityManager em, Entity boundaryEntity)
        {
            DynamicBuffer<BuildingProductionSlotReadModel> buffer =
                EnsureBoundaryBuffer<BuildingProductionSlotReadModel>(em, boundaryEntity);
            buffer.Clear();

            IReadOnlyList<BuildingDefinition> definitions = definitionSystem.ConfiguredSpawnableDefinitions;
            for (int i = 0; i < definitions.Count; i++)
            {
                BuildingDefinition definition = definitions[i];
                if (definition == null || definition.Prefab == null)
                    continue;

                FixedString128Bytes buildingId = ResolveBoundaryId(definition.Prefab, definition.DisplayName);
                int productionCount = BuildingDefinitionPrefabSystemHelper.GetProductionCount(definition);
                for (int slotIndex = 0; slotIndex < productionCount; slotIndex++)
                {
                    GameObject unitPrefab = BuildingDefinitionPrefabSystemHelper.GetProductionPrefab(definition, slotIndex);
                    if (unitPrefab == null)
                        continue;

                    buffer.Add(new BuildingProductionSlotReadModel
                    {
                        BuildingId = buildingId,
                        SlotIndex = slotIndex,
                        UnitSourceKey = ToUnitSourceKey(unitPrefab),
                        UnitId = ResolveBoundaryId(unitPrefab, unitPrefab.name)
                    });
                }
            }
        }

        private void PublishRuntimeFactionSummaries(
            FactionResourceCompositionSystemHelper factionResourceSystem,
            BuildingRuntimeReadModelCompositionSystemHelper runtimeQuerySystem,
            BuildingRuntimeReadModelCompositionSystemHelper.Context runtimeQueryContext,
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
                    out FactionResourceCompositionSystemHelper.ResourceEconomySnapshot economy);
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
            BuildingDefinitionPrefabSystemHelper definitionSystem,
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
            BuildingDefinitionPrefabSystemHelper definitionSystem,
            BuildingRuntimeReadModelCompositionSystemHelper.Context runtimeQueryContext,
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

            _productionSummaryCountsScratch.Clear();
            _producedReadModelBuildingIdsScratch.Clear();

            EntityManager producedUnitEntityManager = default;
            bool hasEntityManager = runtimeQueryContext.TryGetEntityManager != null &&
                                    runtimeQueryContext.TryGetEntityManager(out producedUnitEntityManager);
            DynamicBuffer<BuildingProducedUnitReadModel> producedUnitRows = default;
            bool hasProducedUnitRows = em.HasBuffer<BuildingProducedUnitReadModel>(boundaryEntity);
            if (hasProducedUnitRows)
                producedUnitRows = em.GetBuffer<BuildingProducedUnitReadModel>(boundaryEntity, true);

            if (hasEntityManager && hasProducedUnitRows)
            {
                AccumulateProducedUnitReadModelSummaries(
                    runtimeBuildings,
                    producedUnitRows,
                    producedUnitEntityManager);
            }

            if (runtimeBuildings is Dictionary<int, RuntimeBuildingEntity> runtimeBuildingMap)
            {
                foreach (KeyValuePair<int, RuntimeBuildingEntity> pair in runtimeBuildingMap)
                    AccumulateRuntimeUnitProductionSummaryForBuilding(
                        runtimeQueryContext,
                        producedUnitEntityManager,
                        hasEntityManager,
                        pair.Value);
            }
            else
            {
                foreach (KeyValuePair<int, RuntimeBuildingEntity> pair in runtimeBuildings)
                    AccumulateRuntimeUnitProductionSummaryForBuilding(
                        runtimeQueryContext,
                        producedUnitEntityManager,
                        hasEntityManager,
                        pair.Value);
            }

            PublishAccumulatedProductionSummaries(buffer);
        }

        private void AccumulateRuntimeUnitProductionSummaryForBuilding(
            BuildingRuntimeReadModelCompositionSystemHelper.Context runtimeQueryContext,
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
                    AddProductionSummaryCount(factionId, unitId, producedDelta: 0, queuedDelta: 1);
                }
            }

            if (_producedReadModelBuildingIdsScratch.Contains(building.Id))
                return;

            if (!hasEntityManager || building.ProducedUnits == null)
                return;

            runtimeQueryContext.ProductionSystem?.PruneProducedUnits(
                building.ProducedUnits,
                building.ProducedUnitSlots,
                building.ProducedUnitPrefabs,
                producedUnitEntityManager,
                building.ProducedUnitSourceKeys);
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

                AddProductionSummaryCount(factionId, unitId, producedDelta: 1, queuedDelta: 0);
            }
        }

        private void AccumulateProducedUnitReadModelSummaries(
            IReadOnlyDictionary<int, RuntimeBuildingEntity> runtimeBuildings,
            DynamicBuffer<BuildingProducedUnitReadModel> producedUnitRows,
            EntityManager em)
        {
            for (int i = 0; i < producedUnitRows.Length; i++)
            {
                BuildingProducedUnitReadModel producedUnit = producedUnitRows[i];
                _producedReadModelBuildingIdsScratch.Add(producedUnit.BuildingRuntimeId);
                if (!TryGetRuntimeBuilding(runtimeBuildings, producedUnit.BuildingRuntimeId, out RuntimeBuildingEntity building) ||
                    building == null ||
                    building.IsDestroyed ||
                    !building.HasOwnerFaction)
                {
                    continue;
                }

                byte factionId = building.OwnerFactionId;
                if (producedUnit.HasOwnerFaction == 0 ||
                    producedUnit.OwnerFactionId != factionId ||
                    !IsProducedUnitAlive(producedUnit.Unit, em) ||
                    !TryResolveProducedReadModelUnitId(producedUnit, em, out FixedString128Bytes unitId))
                {
                    continue;
                }

                AddProductionSummaryCount(factionId, unitId, producedDelta: 1, queuedDelta: 0);
            }
        }

        private static bool TryGetRuntimeBuilding(
            IReadOnlyDictionary<int, RuntimeBuildingEntity> runtimeBuildings,
            int buildingRuntimeId,
            out RuntimeBuildingEntity building)
        {
            if (runtimeBuildings is Dictionary<int, RuntimeBuildingEntity> runtimeBuildingMap)
                return runtimeBuildingMap.TryGetValue(buildingRuntimeId, out building);

            foreach (KeyValuePair<int, RuntimeBuildingEntity> pair in runtimeBuildings)
            {
                if (pair.Key != buildingRuntimeId)
                    continue;

                building = pair.Value;
                return true;
            }

            building = null;
            return false;
        }

        private void PublishConfiguredUnitProductionSummaryRows(
            BuildingDefinitionPrefabSystemHelper definitionSystem,
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

        private void AddProductionSummaryCount(
            byte factionId,
            FixedString128Bytes unitId,
            int producedDelta,
            int queuedDelta)
        {
            if (unitId.Length == 0 || (producedDelta == 0 && queuedDelta == 0))
                return;

            ProductionSummaryKey key = new(factionId, unitId);
            _productionSummaryCountsScratch.TryGetValue(key, out ProductionSummaryCounts counts);
            counts.ProducedCount += producedDelta;
            counts.QueuedCount += queuedDelta;
            _productionSummaryCountsScratch[key] = counts;
        }

        private void PublishAccumulatedProductionSummaries(
            DynamicBuffer<BuildingRuntimeUnitProductionSummary> buffer)
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                BuildingRuntimeUnitProductionSummary summary = buffer[i];
                ProductionSummaryKey key = new(summary.FactionId, summary.UnitId);
                if (!_productionSummaryCountsScratch.TryGetValue(key, out ProductionSummaryCounts counts))
                    continue;

                summary.ProducedCount += counts.ProducedCount;
                summary.QueuedCount += counts.QueuedCount;
                buffer[i] = summary;
                _productionSummaryCountsScratch.Remove(key);
            }

            _productionSummaryCountsScratch.Clear();
        }

        private bool TryResolveProducedUnitId(
            RuntimeBuildingEntity building,
            Entity unit,
            EntityManager em,
            out FixedString128Bytes unitId)
        {
            if (building?.ProducedUnitSourceKeys != null &&
                building.ProducedUnitSourceKeys.TryGetValue(unit, out FixedString64Bytes sourceKeyFromBuilding) &&
                sourceKeyFromBuilding.Length > 0)
            {
                unitId = ToFixedString128(BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey(sourceKeyFromBuilding.ToString()));
                return unitId.Length > 0;
            }

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
                unitId = ToFixedString128(BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey(
                    em.GetComponentData<UnitSourcePrefabKey>(unit).Value.ToString()));
                return unitId.Length > 0;
            }

            unitId = default;
            return false;
        }

        private static bool TryResolveProducedReadModelUnitId(
            BuildingProducedUnitReadModel producedUnit,
            EntityManager em,
            out FixedString128Bytes unitId)
        {
            if (producedUnit.UnitSourceKey.Length > 0)
            {
                unitId = ToFixedString128(BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey(producedUnit.UnitSourceKey.ToString()));
                return unitId.Length > 0;
            }

            if (producedUnit.Unit != Entity.Null &&
                em.Exists(producedUnit.Unit) &&
                em.HasComponent<UnitSourcePrefabKey>(producedUnit.Unit))
            {
                unitId = ToFixedString128(BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey(
                    em.GetComponentData<UnitSourcePrefabKey>(producedUnit.Unit).Value.ToString()));
                return unitId.Length > 0;
            }

            unitId = default;
            return false;
        }

        private static bool IsProducedUnitAlive(Entity unit, EntityManager em)
        {
            if (unit == Entity.Null || !em.Exists(unit))
                return false;

            return !em.HasComponent<UnitHealth>(unit) ||
                   em.GetComponentData<UnitHealth>(unit).Current > 0;
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
                    BuildingRuntimeId = building.Id,
                    SlotIndex = i,
                    Cell = cell,
                    WorldPosition = new float3(world.x, world.y, world.z)
                });
            }
        }

        private void PublishFactionRunwaysReadModel(
            EntityManager em,
            Entity boundaryEntity,
            IReadOnlyDictionary<int, RuntimeBuildingEntity> runtimeBuildings)
        {
            DynamicBuffer<BuildingFactionRunwayReadModel> buffer =
                EnsureBoundaryBuffer<BuildingFactionRunwayReadModel>(em, boundaryEntity);
            buffer.Clear();

            if (!TryGetGridConfig(em, out GridConfig grid))
                return;

            if (runtimeBuildings is Dictionary<int, RuntimeBuildingEntity> runtimeBuildingMap)
            {
                foreach (KeyValuePair<int, RuntimeBuildingEntity> entry in runtimeBuildingMap)
                    PublishFactionRunwayForBuilding(buffer, grid, entry.Value);
            }
            else
            {
                foreach (KeyValuePair<int, RuntimeBuildingEntity> entry in runtimeBuildings)
                    PublishFactionRunwayForBuilding(buffer, grid, entry.Value);
            }
        }

        private void PublishFactionRunwayForBuilding(
            DynamicBuffer<BuildingFactionRunwayReadModel> buffer,
            GridConfig grid,
            RuntimeBuildingEntity building)
        {
            if (building == null ||
                building.IsDestroyed ||
                !building.HasOwnerFaction ||
                building.Instance == null ||
                building.Definition == null ||
                building.Definition.Prefab == null ||
                !building.Definition.HasRunway)
            {
                return;
            }

            Transform transform = building.Instance.transform;
            Vector3 center = transform.TransformPoint(BuildingRunwaySystem.ResolveRuntimeRunwayLocalPosition(building.Definition));
            Quaternion rotation = transform.rotation * building.Definition.RunwayLocalRotation;
            Vector3 direction = rotation * Vector3.forward;
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.0001f)
                direction = Vector3.forward;
            direction.Normalize();

            Vector3 scaledHalfExtents = Vector3.Scale(building.Definition.RunwayHalfExtents, transform.lossyScale);
            float halfWidth = Mathf.Max(1f, Mathf.Abs(scaledHalfExtents.x));
            float halfLength = Mathf.Max(8f, Mathf.Abs(scaledHalfExtents.z));
            Vector3 takeoffPosition = center - direction * halfLength;
            Vector3 landingPosition = center + direction * halfLength;
            takeoffPosition.y = center.y;
            landingPosition.y = center.y;

            int2 takeoffCell = GridUtils.WorldToCell(grid, new float3(takeoffPosition.x, takeoffPosition.y, takeoffPosition.z));
            int2 landingCell = GridUtils.WorldToCell(grid, new float3(landingPosition.x, landingPosition.y, landingPosition.z));
            if (!GridUtils.InBounds(takeoffCell, grid.Width, grid.Height) ||
                !GridUtils.InBounds(landingCell, grid.Width, grid.Height))
            {
                return;
            }

            buffer.Add(new BuildingFactionRunwayReadModel
            {
                FactionId = building.OwnerFactionId,
                BuildingId = ResolveBoundaryId(building.Definition.Prefab, building.Definition.DisplayName),
                BuildingRuntimeId = building.Id,
                TakeoffCell = takeoffCell,
                LandingCell = landingCell,
                TakeoffPosition = new float3(takeoffPosition.x, takeoffPosition.y, takeoffPosition.z),
                LandingPosition = new float3(landingPosition.x, landingPosition.y, landingPosition.z),
                Center = new float3(center.x, center.y, center.z),
                Direction = new float3(direction.x, direction.y, direction.z),
                HalfExtents = new float2(halfWidth, halfLength)
            });
        }

        private static bool TryResolveConfiguredBuildingDefinition(
            BuildingDefinitionPrefabSystemHelper definitionSystem,
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

            string normalized = BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey(buildingId);
            for (int i = 0; i < definitionSystem.ConfiguredSpawnableCount; i++)
            {
                if (!definitionSystem.TryGetConfiguredDefinition(i, out BuildingDefinition candidate))
                    continue;

                if (!BuildingDefinitionPrefabSystemHelper.RuntimeDefinitionMatchesId(candidate, normalized))
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

        private bool TryGetBoundaryEntity(EntityManager em, EntityQuery boundaryQuery, out Entity boundaryEntity)
        {
            boundaryEntity = Entity.Null;
            Unity.Entities.World world = em.World;
            if (world == null || !world.IsCreated)
            {
                _cachedBoundaryWorld = null;
                _cachedBoundaryEntity = Entity.Null;
                return false;
            }

            if (_cachedBoundaryWorld == world &&
                _cachedBoundaryEntity != Entity.Null &&
                em.Exists(_cachedBoundaryEntity))
            {
                boundaryEntity = _cachedBoundaryEntity;
                return true;
            }

            if (!boundaryQuery.IsEmptyIgnoreFilter)
                boundaryEntity = boundaryQuery.GetSingletonEntity();

            bool valid = boundaryEntity != Entity.Null && em.Exists(boundaryEntity);
            _cachedBoundaryWorld = valid ? world : null;
            _cachedBoundaryEntity = valid ? boundaryEntity : Entity.Null;
            return valid;
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

                string normalized = BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey(prefab.name);
                FixedString128Bytes resolved = ToFixedString128(string.IsNullOrEmpty(normalized) ? fallback : normalized);
                _boundaryIdsByPrefab[prefab] = resolved;
                return resolved;
            }

            string fallbackKey = fallback ?? string.Empty;
            if (_boundaryIdsByFallback.TryGetValue(fallbackKey, out FixedString128Bytes fallbackCached))
                return fallbackCached;

            string normalizedFallback = BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey(fallback);
            FixedString128Bytes fallbackResolved = ToFixedString128(string.IsNullOrEmpty(normalizedFallback) ? fallback : normalizedFallback);
            _boundaryIdsByFallback[fallbackKey] = fallbackResolved;
            return fallbackResolved;
        }

        private static FixedString128Bytes ToFixedString128(string value)
        {
            return new FixedString128Bytes(value ?? string.Empty);
        }

        private static FixedString64Bytes ToUnitSourceKey(GameObject prefab)
        {
            return new FixedString64Bytes(prefab != null ? prefab.name : string.Empty);
        }
    }
}
