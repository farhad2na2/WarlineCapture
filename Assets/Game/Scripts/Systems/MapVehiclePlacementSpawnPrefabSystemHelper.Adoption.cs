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
        internal static bool TryFindAuthoredVehicleEntity(
            EntityManager em,
            int placementIndex,
            MapVehiclePlacementConfigEntry placement,
            NativeHashSet<Entity> claimedEntities,
            out Entity entity)
        {
            entity = Entity.Null;
            if (placement == null)
                return false;

            float3 target = ToFloat3(placement.WorldPosition);
            float maximumDistanceSquared = AuthoredVehicleAdoptionDistance * AuthoredVehicleAdoptionDistance;
            float bestDistanceSquared = maximumDistanceSquared;
            using EntityQuery query = em.CreateEntityQuery(
                ComponentType.ReadOnly<Faction>(),
                ComponentType.ReadOnly<UnitGrid>(),
                ComponentType.ReadOnly<UnitMove>(),
                ComponentType.ReadOnly<UnitMovementBehavior>(),
                ComponentType.ReadOnly<UnitRespawnPrefab>(),
                ComponentType.ReadOnly<LocalTransform>());
            using NativeArray<ArchetypeChunk> queryChunks = query.ToArchetypeChunkArray(Allocator.Temp);
            var entitiesType = em.GetEntityTypeHandle();

            // Entity-presentation candidates already carry the canonical placement identity on
            // their detailed visual root. Resolve that identity before considering the legacy
            // transform heuristic: render-bound pivots and migration transforms are not required
            // to remain within the compatibility placement's one-metre adoption radius.
            if (placementIndex >= 0)
            {
                foreach(var sourceChunk in queryChunks)
                {
                    var entities = sourceChunk.GetNativeArray(entitiesType);
                    for (int i = 0; i < entities.Length; i++)
                    {
                        Entity candidate = entities[i];
                        if (!IsUnclaimedNeutralAuthoredVehicle(em, candidate, claimedEntities) ||
                            !em.HasComponent<OperationMapAuthoredVehiclePresentation>(candidate) ||
                            !em.HasComponent<UnitDetailedVisualReference>(candidate))
                        {
                            continue;
                        }

                        Entity visualRoot = em.GetComponentData<UnitDetailedVisualReference>(candidate).Root;
                        if (visualRoot == Entity.Null ||
                            !em.Exists(visualRoot) ||
                            !em.HasComponent<OperationMapEntityPresentationIdentity>(visualRoot) ||
                            em.GetComponentData<OperationMapEntityPresentationIdentity>(visualRoot).PlacementIndex != placementIndex)
                        {
                            continue;
                        }

                        entity = candidate;
                        return true;
                    }
                }
            }

            foreach(var sourceChunk in queryChunks)
            {
                var entities = sourceChunk.GetNativeArray(entitiesType);
                for (int i = 0; i < entities.Length; i++)
                {
                    Entity candidate = entities[i];
                    if (!IsUnclaimedNeutralAuthoredVehicle(em, candidate, claimedEntities) ||
                        em.HasComponent<OperationMapAuthoredVehiclePresentation>(candidate))
                    {
                        continue;
                    }

                    float3 candidatePosition = em.GetComponentData<LocalTransform>(candidate).Position;
                    float distanceSquared = math.distancesq(target, candidatePosition);
                    if (distanceSquared > bestDistanceSquared)
                        continue;

                    if (entity == Entity.Null ||
                        distanceSquared < bestDistanceSquared ||
                        candidate.Index < entity.Index)
                    {
                        entity = candidate;
                        bestDistanceSquared = distanceSquared;
                    }
                }
            }

            return entity != Entity.Null;
        }
    }
}
