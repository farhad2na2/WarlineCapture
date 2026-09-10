using System;
using System.Collections.Generic;
using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Game.Runtime
{
    public sealed partial class AndroidPerformanceRecorder
    {
        private void LocateVrp095Targets(EntityManager entityManager)
        {
            if (!TryGetVrp095Database(
                    entityManager,
                    out OperationMapRenderDatabaseComponent database))
            {
                return;
            }

            ref OperationMapRenderDatabaseBlob blob = ref database.Blob.Value;
            using EntityQuery query = entityManager.CreateEntityQuery(
                new EntityQueryDesc
                {
                    All = new[]
                    {
                        ComponentType.ReadOnly<
                            OperationMapVirtualizedBuildingPresentationComponent>(),
                        ComponentType.ReadOnly<OperationMapBuildingComponent>(),
                        ComponentType.ReadWrite<UnitHealth>(),
                        ComponentType.ReadOnly<LocalTransform>(),
                        ComponentType.ReadOnly<
                            OperationMapBuildingDestroyedComponent>()
                    },
                    Options = EntityQueryOptions.IgnoreComponentEnabledState
                });
            using NativeArray<ArchetypeChunk> queryChunks = query.ToArchetypeChunkArray(Allocator.Temp);
            var entitiesType = entityManager.GetEntityTypeHandle();
            var presentationsType = entityManager.GetComponentTypeHandle<OperationMapVirtualizedBuildingPresentationComponent>(true);
            var healthType = entityManager.GetComponentTypeHandle<UnitHealth>(true);
            var transformsType = entityManager.GetComponentTypeHandle<LocalTransform>(true);




            var candidates = new List<Vrp095Candidate>(query.CalculateEntityCount());
            foreach(var sourceChunk in queryChunks)
            {
                var entities = sourceChunk.GetNativeArray(entitiesType);
                var presentations = sourceChunk.GetNativeArray(ref presentationsType);
                var health = sourceChunk.GetNativeArray(ref healthType);
                var transforms = sourceChunk.GetNativeArray(ref transformsType);
                for (int i = 0; i < entities.Length; i++)
                {
                    if (health[i].Current <= 0 ||
                        entityManager.IsComponentEnabled<
                            OperationMapBuildingDestroyedComponent>(entities[i]))
                    {
                        continue;
                    }

                    int stateOwner = presentations[i].StateOwnerIndex;
                    ResolveVrp095BucketMasks(
                        ref blob,
                        stateOwner,
                        out uint intactMask,
                        out uint destroyedMask);
                    if (intactMask == 0u || destroyedMask == 0u)
                        continue;

                    candidates.Add(new Vrp095Candidate(
                        entities[i],
                        stateOwner,
                        transforms[i].Position,
                        intactMask,
                        destroyedMask));
                }
            }

            candidates.Sort((left, right) =>
                left.StateOwnerIndex.CompareTo(right.StateOwnerIndex));
            if (!TrySelectVrp095Targets(
                    candidates,
                    out _vrp095Visible,
                    out _vrp095Recycle,
                    out _vrp095OffCamera))
            {
                FailVrp095("three distant compatible intact buildings unavailable");
                return;
            }

            _vrp095InitialSequence =
                ReadVrp067StateChangeVersion(entityManager);
            _vrp095Phase = Vrp095Phase.CenterVisible;
            _vrp095PhaseFrameCount = 0;
            LogNoStackTrace(
                "[VRP-095 StateScenario] phase=Located " +
                $"visible={_vrp095Visible.StateOwnerIndex} " +
                $"recycle={_vrp095Recycle.StateOwnerIndex} " +
                $"offCamera={_vrp095OffCamera.StateOwnerIndex} " +
                $"initialSequence={_vrp095InitialSequence}");
        }

        private static bool TryReadVrp095Snapshot(
            EntityManager entityManager,
            int stateOwnerIndex,
            out Vrp095Snapshot snapshot)
        {
            snapshot = default;
            if (!TryGetVrp095Database(
                    entityManager,
                    out OperationMapRenderDatabaseComponent database))
            {
                return false;
            }

            ref OperationMapRenderDatabaseBlob blob = ref database.Blob.Value;
            using EntityQuery query = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<OperationMapRenderProxySlotComponent>());
            using NativeArray<ArchetypeChunk> queryChunks = query.ToArchetypeChunkArray(Allocator.Temp);
            var slotsType = entityManager.GetComponentTypeHandle<OperationMapRenderProxySlotComponent>(true);
            var assignedSlots = new HashSet<int>();
            int intactCount = 0;
            int destroyedCount = 0;
            foreach(var sourceChunk in queryChunks)
            {
                var slots = sourceChunk.GetNativeArray(ref slotsType);
                for (int i = 0; i < slots.Length; i++)
                {
                    OperationMapRenderProxySlotComponent slot = slots[i];
                    if (slot.PlacementIndex < 0 ||
                        slot.PlacementIndex >= blob.Placements.Length)
                    {
                        continue;
                    }

                    ref OperationMapRenderPlacementBlob placement =
                        ref blob.Placements[slot.PlacementIndex];
                    if (placement.StateOwnerIndex != stateOwnerIndex)
                        continue;

                    assignedSlots.Add(slot.SlotIndex);
                    if (placement.RequiredVisualState ==
                        OperationMapRenderVisualState.Intact)
                    {
                        intactCount++;
                    }
                    else if (placement.RequiredVisualState ==
                             OperationMapRenderVisualState.Destroyed)
                    {
                        destroyedCount++;
                    }
                }
            }

            snapshot = new Vrp095Snapshot(
                assignedSlots.Count,
                intactCount,
                destroyedCount,
                assignedSlots);
            return true;
        }

        private static bool TryResolveVrp095RecycledBuilding(
            EntityManager entityManager,
            IReadOnlyCollection<int> releasedSlots,
            int excludedStateOwnerIndex,
            out Vrp095Candidate candidate,
            out Vrp095Snapshot snapshot,
            out int recycledSlotCount)
        {
            candidate = default;
            snapshot = default;
            recycledSlotCount = 0;
            if (releasedSlots == null || releasedSlots.Count == 0 ||
                !TryGetVrp095Database(
                    entityManager,
                    out OperationMapRenderDatabaseComponent database))
            {
                return false;
            }

            ref OperationMapRenderDatabaseBlob blob = ref database.Blob.Value;
            HashSet<int> releasedSlotSet = releasedSlots as HashSet<int> ??
                                            new HashSet<int>(releasedSlots);
            var overlapByStateOwner = new Dictionary<int, int>();
            using EntityQuery slotQuery = entityManager.CreateEntityQuery(
                       ComponentType.ReadOnly<
                           OperationMapRenderProxySlotComponent>());
            using NativeArray<ArchetypeChunk> slotQueryChunks = slotQuery.ToArchetypeChunkArray(Allocator.Temp);
            var slotsType = entityManager.GetComponentTypeHandle<OperationMapRenderProxySlotComponent>(true);
            {
                foreach(var sourceChunk in slotQueryChunks)
                {
                    var slots = sourceChunk.GetNativeArray(ref slotsType);
                    for (int i = 0; i < slots.Length; i++)
                    {
                        OperationMapRenderProxySlotComponent slot = slots[i];
                        if (!releasedSlotSet.Contains(slot.SlotIndex) ||
                            slot.PlacementIndex < 0 ||
                            slot.PlacementIndex >= blob.Placements.Length)
                        {
                            continue;
                        }

                        ref OperationMapRenderPlacementBlob placement =
                            ref blob.Placements[slot.PlacementIndex];
                        int stateOwner = placement.StateOwnerIndex;
                        if (stateOwner < 0 ||
                            stateOwner == excludedStateOwnerIndex ||
                            placement.RequiredVisualState !=
                            OperationMapRenderVisualState.Intact)
                        {
                            continue;
                        }

                        overlapByStateOwner.TryGetValue(
                            stateOwner,
                            out int count);
                        overlapByStateOwner[stateOwner] = count + 1;
                    }
                }
            }

            if (overlapByStateOwner.Count == 0)
                return false;

            using EntityQuery buildingQuery = entityManager.CreateEntityQuery(
                new EntityQueryDesc
                {
                    All = new[]
                    {
                        ComponentType.ReadOnly<
                            OperationMapVirtualizedBuildingPresentationComponent>(),
                        ComponentType.ReadOnly<OperationMapBuildingComponent>(),
                        ComponentType.ReadOnly<UnitHealth>(),
                        ComponentType.ReadOnly<LocalTransform>(),
                        ComponentType.ReadOnly<
                            OperationMapBuildingDestroyedComponent>()
                    },
                    Options = EntityQueryOptions.IgnoreComponentEnabledState
                });
            using NativeArray<ArchetypeChunk> buildingQueryChunks = buildingQuery.ToArchetypeChunkArray(Allocator.Temp);
            var entitiesType = entityManager.GetEntityTypeHandle();
            var presentationsType = entityManager.GetComponentTypeHandle<OperationMapVirtualizedBuildingPresentationComponent>(true);
            var healthType = entityManager.GetComponentTypeHandle<UnitHealth>(true);
            var transformsType = entityManager.GetComponentTypeHandle<LocalTransform>(true);




            foreach(var sourceChunk in buildingQueryChunks)
            {
                var entities = sourceChunk.GetNativeArray(entitiesType);
                var presentations = sourceChunk.GetNativeArray(ref presentationsType);
                var health = sourceChunk.GetNativeArray(ref healthType);
                var transforms = sourceChunk.GetNativeArray(ref transformsType);
                for (int i = 0; i < entities.Length; i++)
                {
                    int stateOwner = presentations[i].StateOwnerIndex;
                    if (!overlapByStateOwner.TryGetValue(
                            stateOwner,
                            out int overlap) ||
                        health[i].Current <= 0 ||
                        entityManager.IsComponentEnabled<
                            OperationMapBuildingDestroyedComponent>(entities[i]))
                    {
                        continue;
                    }

                    if (overlap < recycledSlotCount ||
                        (overlap == recycledSlotCount &&
                         candidate.Entity != Entity.Null &&
                         stateOwner >= candidate.StateOwnerIndex))
                    {
                        continue;
                    }

                    ResolveVrp095BucketMasks(
                        ref blob,
                        stateOwner,
                        out uint intactMask,
                        out uint destroyedMask);
                    if (intactMask == 0u || destroyedMask == 0u)
                        continue;

                    candidate = new Vrp095Candidate(
                        entities[i],
                        stateOwner,
                        transforms[i].Position,
                        intactMask,
                        destroyedMask);
                    recycledSlotCount = overlap;
                }
            }

            return candidate.Entity != Entity.Null &&
                   TryReadVrp095Snapshot(
                       entityManager,
                       candidate.StateOwnerIndex,
                       out snapshot) &&
                   IsVrp095SnapshotState(
                       snapshot,
                       OperationMapRenderVisualState.Intact) &&
                   CountVrp095Overlap(releasedSlots, snapshot.Slots) ==
                   recycledSlotCount;
        }
    }
}
