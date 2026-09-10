using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Game.Components;
using Game.Configs;
using Game.Tactical.Contracts;

namespace Game.Runtime
{
    internal sealed partial class BuildingProductionRequestSystemHelper
    {
        public int ProcessReadyOperationMapProductions(
            Context context,
            EntityManager em,
            float now,
            LogWarningDelegate logWarning,
            out OperationMapProductionSchedulerDiagnostics diagnostics)
        {
            context.UpdateOperationMapProductionDeliveryLifecycle?.Invoke(now);
            if (float.IsNaN(now) ||
                float.IsInfinity(now) ||
                em.World == null ||
                !em.World.IsCreated)
            {
                diagnostics = CreateSchedulerDiagnostics(OperationMapProductionSchedulerOutcome.InvalidRuntime);
                return 0;
            }

            using EntityQuery producerQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<OperationMapBuildingComponent>(),
                ComponentType.ReadOnly<OperationMapBuildingProductionQueueComponent>(),
                ComponentType.ReadOnly<OperationMapBuildingUnitProductionRequest>());
            using NativeArray<ArchetypeChunk> producerQueryChunks = producerQuery.ToArchetypeChunkArray(Allocator.Temp);
            var producersType = em.GetEntityTypeHandle();
            int pendingRequestCount = 0;
            int readyProducerCount = 0;
            foreach(var sourceChunk in producerQueryChunks)
            {
                var producers = sourceChunk.GetNativeArray(producersType);
                for (int index = 0; index < producers.Length; index++)
                {
                    DynamicBuffer<OperationMapBuildingUnitProductionRequest> queue =
                        em.GetBuffer<OperationMapBuildingUnitProductionRequest>(producers[index], true);
                    for (int requestIndex = 0; requestIndex < queue.Length; requestIndex++)
                    {
                        if (queue[requestIndex].Status == OperationMapBuildingUnitProductionRequest.Pending)
                            pendingRequestCount++;
                    }

                    if (TryPeekReadyOperationMapProduction(em, producers[index], now, out _))
                        readyProducerCount++;
                }
            }

            if (pendingRequestCount == 0)
            {
                diagnostics = CreateSchedulerDiagnostics(
                    OperationMapProductionSchedulerOutcome.NoPendingRequests,
                    producerQuery.CalculateEntityCount());
                ResetOperationMapSchedulerDiagnosticState();
                return 0;
            }

            if (readyProducerCount == 0)
            {
                diagnostics = CreateSchedulerDiagnostics(
                    OperationMapProductionSchedulerOutcome.PendingNotReady,
                    producerQuery.CalculateEntityCount(),
                    pendingRequestCount);
                return 0;
            }

            using EntityQuery authoredGridQuery = em.CreateEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<GridConfig>(),
                    ComponentType.ReadOnly<GridWalkable>(),
                    ComponentType.ReadOnly<DynamicBlockerComponent>(),
                    ComponentType.ReadOnly<DynamicOccupancyComponent>()
                },
                None = new[] { ComponentType.ReadOnly<RuntimeGridBootstrapGridTag>() }
            });
            int authoredGridCount = authoredGridQuery.CalculateEntityCount();
            if (authoredGridCount > 1)
            {
                diagnostics = CreateSchedulerDiagnostics(
                    OperationMapProductionSchedulerOutcome.AmbiguousAuthoredGrid,
                    producerQuery.CalculateEntityCount(),
                    pendingRequestCount,
                    readyProducerCount,
                    authoredGridCount,
                    authoredGridCount);
                ReportOperationMapSchedulerStall(now, logWarning, diagnostics);
                return 0;
            }

            Entity gridEntity;
            int completeGridCount;
            if (authoredGridCount == 1)
            {
                gridEntity = authoredGridQuery.GetSingletonEntity();
                completeGridCount = 1;
            }
            else
            {
                using EntityQuery fallbackGridQuery = em.CreateEntityQuery(
                    ComponentType.ReadOnly<GridConfig>(),
                    ComponentType.ReadOnly<GridWalkable>(),
                    ComponentType.ReadOnly<DynamicBlockerComponent>(),
                    ComponentType.ReadOnly<DynamicOccupancyComponent>());
                completeGridCount = fallbackGridQuery.CalculateEntityCount();
                if (completeGridCount != 1)
                {
                    diagnostics = CreateSchedulerDiagnostics(
                        OperationMapProductionSchedulerOutcome.MissingCompleteGrid,
                        producerQuery.CalculateEntityCount(),
                        pendingRequestCount,
                        readyProducerCount,
                        authoredGridCount,
                        completeGridCount);
                    ReportOperationMapSchedulerStall(now, logWarning, diagnostics);
                    return 0;
                }

                gridEntity = fallbackGridQuery.GetSingletonEntity();
            }
            GridConfig grid = em.GetComponentData<GridConfig>(gridEntity);
            NativeArray<GridWalkable> walkable = em.GetBuffer<GridWalkable>(gridEntity, true).AsNativeArray();
            NativeBitArray blocked = em.GetComponentData<DynamicBlockerComponent>(gridEntity).Blocked;
            NativeBitArray occupied = em.GetComponentData<DynamicOccupancyComponent>(gridEntity).Occupied;
            long gridSize64 = (long)grid.Width * grid.Height;
            if (gridSize64 <= 0 ||
                gridSize64 > int.MaxValue ||
                walkable.Length != (int)gridSize64 ||
                !blocked.IsCreated ||
                blocked.Length != (int)gridSize64 ||
                !occupied.IsCreated ||
                occupied.Length != (int)gridSize64)
            {
                diagnostics = CreateSchedulerDiagnostics(
                    OperationMapProductionSchedulerOutcome.InvalidGrid,
                    producerQuery.CalculateEntityCount(),
                    pendingRequestCount,
                    readyProducerCount,
                    authoredGridCount,
                    completeGridCount);
                ReportOperationMapSchedulerStall(now, logWarning, diagnostics);
                return 0;
            }

            var readyProducers = new NativeList<ReadyOperationMapProducer>(producerQuery.CalculateEntityCount(), Allocator.Temp);
            foreach(var sourceChunk in producerQueryChunks)
            {
                var producers = sourceChunk.GetNativeArray(producersType);
                for (int index = 0; index < producers.Length; index++)
                {
                    Entity producer = producers[index];
                    if (!TryPeekReadyOperationMapProduction(
                            em,
                            producer,
                            now,
                            out OperationMapBuildingUnitProductionRequest request))
                    {
                        continue;
                    }

                    readyProducers.Add(new ReadyOperationMapProducer
                    {
                        Building = producer,
                        PlacementIndex = em.GetComponentData<OperationMapBuildingComponent>(producer).PlacementIndex,
                        RequestId = request.RequestId
                    });
                }
            }

            for (int index = 1; index < readyProducers.Length; index++)
            {
                ReadyOperationMapProducer candidate = readyProducers[index];
                int cursor = index - 1;
                while (cursor >= 0 && CompareReadyOperationMapProducers(candidate, readyProducers[cursor]) < 0)
                {
                    readyProducers[cursor + 1] = readyProducers[cursor];
                    cursor--;
                }

                readyProducers[cursor + 1] = candidate;
            }

            var reserved = new NativeBitArray((int)gridSize64, Allocator.Temp, NativeArrayOptions.ClearMemory);
            try
            {
                int resolveLimit = math.min(MaxOperationMapProductionSpawnsPerUpdate, readyProducers.Length);
                using NativeList<ResolvedOperationMapProductionSpawn> resolved = new(resolveLimit, Allocator.Temp);
                for (int index = 0; index < resolveLimit; index++)
                {
                    ReadyOperationMapProducer candidate = readyProducers[index];
                    if (!TryResolveReadyOperationMapProductionSpawn(
                            em,
                            candidate.Building,
                            candidate.RequestId,
                            now,
                            grid,
                            walkable,
                            blocked,
                            occupied,
                            ref reserved,
                            out int2 spawnCell,
                            out float3 spawnPosition))
                    {
                        continue;
                    }

                    resolved.Add(new ResolvedOperationMapProductionSpawn
                    {
                        Building = candidate.Building,
                        RequestId = candidate.RequestId,
                        Cell = spawnCell,
                        Position = spawnPosition
                    });
                }

                int spawnedCount = 0;
                int deliveryInProgressCount = 0;
                for (int index = 0; index < resolved.Length; index++)
                {
                    ResolvedOperationMapProductionSpawn spawn = resolved[index];
                    float3 committedSpawnPosition = spawn.Position;
                    OperationMapProductionDeliveryResult deliveryResult =
                        ResolveOperationMapProductionDelivery(
                            context,
                            em,
                            spawn.Building,
                            spawn.RequestId,
                            ref committedSpawnPosition,
                            now);
                    if (deliveryResult == OperationMapProductionDeliveryResult.InProgress)
                    {
                        deliveryInProgressCount++;
                        continue;
                    }
                    if (deliveryResult == OperationMapProductionDeliveryResult.Rejected)
                    {
                        continue;
                    }

                    if (TrySpawnReadyOperationMapProduction(
                            in context,
                            em,
                            spawn.Building,
                            spawn.RequestId,
                            now,
                            GridUtils.WorldToCell(grid, committedSpawnPosition),
                            committedSpawnPosition,
                            out _))
                    {
                        spawnedCount++;
                    }
                }

                OperationMapProductionSchedulerOutcome outcome = spawnedCount > 0
                    ? OperationMapProductionSchedulerOutcome.Spawned
                    : deliveryInProgressCount > 0
                        ? OperationMapProductionSchedulerOutcome.DeliveryInProgress
                    : resolved.Length > 0
                        ? OperationMapProductionSchedulerOutcome.SpawnRejected
                        : OperationMapProductionSchedulerOutcome.SpawnCellUnresolved;
                diagnostics = CreateSchedulerDiagnostics(
                    outcome,
                    producerQuery.CalculateEntityCount(),
                    pendingRequestCount,
                    readyProducerCount,
                    authoredGridCount,
                    completeGridCount,
                    resolved.Length,
                    spawnedCount);
                if (spawnedCount > 0 || deliveryInProgressCount > 0)
                    ResetOperationMapSchedulerDiagnosticState();
                else
                    ReportOperationMapSchedulerStall(now, logWarning, diagnostics);
                return spawnedCount;
            }
            finally
            {
                reserved.Dispose();
                readyProducers.Dispose();
            }
        }
    }
}
