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
        private void LocateVrp067Target(EntityManager entityManager)
        {
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


            Entity target = Entity.Null;
            foreach(var sourceChunk in queryChunks)
            {
                var entities = sourceChunk.GetNativeArray(entitiesType);
                var presentations = sourceChunk.GetNativeArray(ref presentationsType);
                for (int i = 0; i < presentations.Length; i++)
                {
                    if (presentations[i].StateOwnerIndex !=
                        _vrp067StateOwnerIndex)
                    {
                        continue;
                    }

                    if (target != Entity.Null)
                    {
                        FailVrp067("state owner resolves to multiple buildings");
                        return;
                    }

                    target = entities[i];
                }
            }

            if (target == Entity.Null)
                return;

            UnitHealth health = entityManager.GetComponentData<UnitHealth>(target);
            OperationMapBuildingComponent building =
                entityManager.GetComponentData<OperationMapBuildingComponent>(
                    target);
            if (entityManager.IsComponentEnabled<
                    OperationMapBuildingDestroyedComponent>(target) ||
                health.Current <= 0 ||
                building.BlockerPolicy !=
                OperationMapBuildingBlockerPolicy.RubbleRemainsBlocked)
            {
                FailVrp067("target building is not intact and destructible");
                return;
            }

            _vrp067TargetEntity = target;
            _vrp067StableId = building.StableId.ToString();
            _vrp067TargetPosition = entityManager
                .GetComponentData<LocalTransform>(target).Position;
            _vrp067InitialStateChangeVersion =
                ReadVrp067StateChangeVersion(entityManager);
            _vrp067Phase = Vrp067Phase.Center;
            _vrp067PhaseFrameCount = 0;
            LogNoStackTrace(
                "[VRP-067 DestructionMatrix] phase=Located " +
                $"family={_vrp067Family} " +
                $"stateOwner={_vrp067StateOwnerIndex} " +
                $"stableId={_vrp067StableId} " +
                $"position={_vrp067TargetPosition.x:F3}," +
                $"{_vrp067TargetPosition.y:F3}," +
                $"{_vrp067TargetPosition.z:F3} " +
                $"initialSequence={_vrp067InitialStateChangeVersion}");
        }

        private static void ReadVrp067VirtualizationMetrics(
            EntityManager entityManager,
            out int enabledSlots,
            out int activeCells,
            out int activePlacements,
            out int overflow,
            out int deficit,
            out int2 envelopeMin,
            out int2 envelopeMax)
        {
            enabledSlots = 0;
            activeCells = 0;
            activePlacements = 0;
            overflow = int.MaxValue;
            deficit = int.MaxValue;
            envelopeMin = int2.zero;
            envelopeMax = int2.zero;
            using EntityQuery metricsQuery = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<
                    OperationMapRenderVirtualizationMetricsComponent>());
            using EntityQuery stateQuery = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<
                    OperationMapRenderVirtualizationStateComponent>());
            if (metricsQuery.CalculateEntityCount() != 1 ||
                stateQuery.CalculateEntityCount() != 1)
                return;

            OperationMapRenderVirtualizationMetricsComponent metrics =
                metricsQuery.GetSingleton<
                    OperationMapRenderVirtualizationMetricsComponent>();
            OperationMapRenderVirtualizationStateComponent state =
                stateQuery.GetSingleton<
                    OperationMapRenderVirtualizationStateComponent>();
            enabledSlots = metrics.EnabledSlotCount;
            activeCells = metrics.ActiveCellCount;
            activePlacements = metrics.ActivePlacementCount;
            overflow = metrics.OverflowCount;
            deficit = metrics.HighestDeficit;
            envelopeMin = state.ActiveEnvelopeMin;
            envelopeMax = state.ActiveEnvelopeMax;
        }
    }
}
