using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Game.Components;
using Game.Configs;

namespace Game.Runtime
{
    internal sealed partial class MapVehiclePlacementSpawnPrefabSystemHelper
    {
        internal static bool IsAuthoredVehiclePresentationReady(
            EntityManager em,
            bool requireReadinessContract = false)
        {
            using EntityQuery contractQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<OperationMapEntityPresentationReadinessContract>());
            if (contractQuery.IsEmptyIgnoreFilter)
                return !requireReadinessContract;

            using NativeArray<ArchetypeChunk> contractQueryChunks = contractQuery.ToArchetypeChunkArray(Allocator.Temp);
            var contractsType = em.GetComponentTypeHandle<OperationMapEntityPresentationReadinessContract>(true);
            int expectedVehicleCount = 0;
            foreach(var sourceChunk in contractQueryChunks)
            {
                var contracts = sourceChunk.GetNativeArray(ref contractsType);
                for (int i = 0; i < contracts.Length; i++)
                    expectedVehicleCount = math.max(expectedVehicleCount, contracts[i].ExpectedGameplayVehicleCount);
            }
            if (expectedVehicleCount <= 0)
                return !requireReadinessContract;

            using EntityQuery vehicleQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<OperationMapAuthoredVehiclePresentation>());
            return vehicleQuery.CalculateEntityCount() >= expectedVehicleCount;
        }

        internal static bool HasPositivePackedPresentationContract(EntityManager em)
        {
            using EntityQuery contractQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<OperationMapEntityPresentationReadinessContract>());
            if (contractQuery.IsEmptyIgnoreFilter)
                return false;

            using NativeArray<ArchetypeChunk> contractQueryChunks = contractQuery.ToArchetypeChunkArray(Allocator.Temp);
            var contractsType = em.GetComponentTypeHandle<OperationMapEntityPresentationReadinessContract>(true);
            foreach(var sourceChunk in contractQueryChunks)
            {
                var contracts = sourceChunk.GetNativeArray(ref contractsType);
                for (int i = 0; i < contracts.Length; i++)
                {
                    if (contracts[i].ExpectedGameplayVehicleCount > 0)
                        return true;
                }
            }

            return false;
        }

        internal static int ReconcileAuthoredVehicleOwnership(
            EntityManager em,
            MapVehiclePlacementConfig config)
        {
            if (config == null || config.Placements == null || config.Placements.Count == 0)
                return 0;

            using EntityQuery query = em.CreateEntityQuery(
                ComponentType.ReadOnly<OperationMapAuthoredVehiclePresentation>(),
                ComponentType.ReadOnly<UnitDetailedVisualReference>(),
                ComponentType.ReadWrite<Faction>());
            using NativeArray<ArchetypeChunk> queryChunks = query.ToArchetypeChunkArray(Allocator.Temp);
            var entitiesType = em.GetEntityTypeHandle();
            using var additions = new EntityCommandBuffer(Allocator.Temp);
            int reconciled = 0;
            foreach(var sourceChunk in queryChunks)
            {
                var entities = sourceChunk.GetNativeArray(entitiesType);
                for (int i = 0; i < entities.Length; i++)
                {
                    Entity candidate = entities[i];
                    if (em.HasComponent<Prefab>(candidate) || em.HasComponent<Disabled>(candidate))
                        continue;

                    Entity visualRoot = em.GetComponentData<UnitDetailedVisualReference>(candidate).Root;
                    if (visualRoot == Entity.Null ||
                        !em.Exists(visualRoot) ||
                        !em.HasComponent<OperationMapEntityPresentationIdentity>(visualRoot))
                    {
                        continue;
                    }

                    int placementIndex =
                        em.GetComponentData<OperationMapEntityPresentationIdentity>(visualRoot).PlacementIndex;
                    if (placementIndex < 0 || placementIndex >= config.Placements.Count)
                        continue;

                    MapVehiclePlacementConfigEntry placement = config.Placements[placementIndex];
                    FixedString64Bytes sourceKey = GetVehiclePrefabSourceKey(placement);
                    if (placement == null || sourceKey.Length == 0)
                        continue;

                    Faction faction = em.GetComponentData<Faction>(candidate);
                    if (faction.Id != placement.FactionId)
                    {
                        faction.Id = placement.FactionId;
                        em.SetComponentData(candidate, faction);
                    }

                    UnitSourcePrefabKey source = new() { Value = sourceKey };
                    if (em.HasComponent<UnitSourcePrefabKey>(candidate))
                        em.SetComponentData(candidate, source);
                    else
                        additions.AddComponent(candidate, source);
                    reconciled++;
                }
            }

            additions.Playback(em);
            return reconciled;
        }

        internal static bool IsAuthoredVehicleOwnershipReady(
            EntityManager em,
            MapVehiclePlacementConfig config,
            bool requireReadinessContract)
        {
            if (!requireReadinessContract)
                return true;
            if (config == null || config.Placements == null || config.Placements.Count == 0)
                return true;
            if (!IsAuthoredVehiclePresentationReady(em, requireReadinessContract: true))
                return false;

            using EntityQuery query = em.CreateEntityQuery(
                ComponentType.ReadOnly<OperationMapAuthoredVehiclePresentation>(),
                ComponentType.ReadOnly<UnitDetailedVisualReference>(),
                ComponentType.ReadOnly<Faction>());
            using NativeArray<ArchetypeChunk> queryChunks = query.ToArchetypeChunkArray(Allocator.Temp);
            var entitiesType = em.GetEntityTypeHandle();
            int ready = 0;
            foreach(var sourceChunk in queryChunks)
            {
                var entities = sourceChunk.GetNativeArray(entitiesType);
                for (int i = 0; i < entities.Length; i++)
                {
                    Entity candidate = entities[i];
                    if (em.HasComponent<Prefab>(candidate) || em.HasComponent<Disabled>(candidate))
                        continue;

                    Entity visualRoot = em.GetComponentData<UnitDetailedVisualReference>(candidate).Root;
                    if (visualRoot == Entity.Null ||
                        !em.Exists(visualRoot) ||
                        !em.HasComponent<OperationMapEntityPresentationIdentity>(visualRoot))
                    {
                        return false;
                    }

                    int placementIndex =
                        em.GetComponentData<OperationMapEntityPresentationIdentity>(visualRoot).PlacementIndex;
                    if (placementIndex < 0 || placementIndex >= config.Placements.Count)
                        return false;

                    MapVehiclePlacementConfigEntry placement = config.Placements[placementIndex];
                    FixedString64Bytes sourceKey = GetVehiclePrefabSourceKey(placement);
                    if (placement == null ||
                        sourceKey.Length == 0 ||
                        em.GetComponentData<Faction>(candidate).Id != placement.FactionId ||
                        !em.HasComponent<UnitSourcePrefabKey>(candidate) ||
                        !em.GetComponentData<UnitSourcePrefabKey>(candidate).Value.Equals(sourceKey))
                    {
                        return false;
                    }

                    ready++;
                }
            }

            using EntityQuery contractQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<OperationMapEntityPresentationReadinessContract>());
            using NativeArray<ArchetypeChunk> contractQueryChunks = contractQuery.ToArchetypeChunkArray(Allocator.Temp);
            var contractsType = em.GetComponentTypeHandle<OperationMapEntityPresentationReadinessContract>(true);
            int expected = 0;
            foreach(var sourceChunk in contractQueryChunks)
            {
                var contracts = sourceChunk.GetNativeArray(ref contractsType);
                for (int i = 0; i < contracts.Length; i++)
                    expected = math.max(expected, contracts[i].ExpectedGameplayVehicleCount);
            }
            return expected > 0 && ready >= expected;
        }
    }
}
