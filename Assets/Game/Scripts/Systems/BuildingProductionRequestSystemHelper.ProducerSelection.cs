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
        public bool TryFindFirstFriendlyOperationMapProducer(
            Context context,
            GameObject unitPrefab,
            out Entity buildingEntity,
            out int productionIndex,
            out string buildingDisplayName)
        {
            buildingEntity = Entity.Null;
            productionIndex = -1;
            buildingDisplayName = string.Empty;
            if (unitPrefab == null ||
                context.TryGetEntityManager == null ||
                !context.TryGetEntityManager(out EntityManager em) ||
                em.World == null ||
                !em.World.IsCreated)
            {
                return false;
            }

            FixedString64Bytes unitSourceKey = new(unitPrefab.name);
            using EntityQuery query = em.CreateEntityQuery(
                ComponentType.ReadOnly<OperationMapBuildingComponent>(),
                ComponentType.ReadOnly<Faction>(),
                ComponentType.ReadOnly<UnitHealth>(),
                ComponentType.ReadOnly<UnitDisplayInfo>(),
                ComponentType.ReadOnly<OperationMapBuildingProductionPrefab>());
            using NativeArray<ArchetypeChunk> queryChunks = query.ToArchetypeChunkArray(Allocator.Temp);
            var candidatesType = em.GetEntityTypeHandle();
            for (int pass = 0; pass < 2; pass++)
            {
                Entity bestEntity = Entity.Null;
                int bestProductionIndex = -1;
                int bestProductionQuantity = int.MinValue;
                int bestPlacementIndex = int.MaxValue;
                string bestDisplayName = string.Empty;
                foreach(var sourceChunk in queryChunks)
                {
                    var candidates = sourceChunk.GetNativeArray(candidatesType);
                    for (int candidateIndex = 0; candidateIndex < candidates.Length; candidateIndex++)
                    {
                        Entity candidate = candidates[candidateIndex];
                        byte factionId = em.GetComponentData<Faction>(candidate).Id;
                        bool passMatches = pass == 0
                            ? factionId == FactionIdentity.PlayerFactionId
                            : factionId == FactionIdentity.NeutralFactionId;
                        if (!passMatches || em.GetComponentData<UnitHealth>(candidate).Current <= 0)
                            continue;
                        if (em.HasComponent<Prefab>(candidate))
                            continue;
                        if (em.HasComponent<OperationMapBuildingDestroyedComponent>(candidate) &&
                            em.IsComponentEnabled<OperationMapBuildingDestroyedComponent>(candidate))
                        {
                            continue;
                        }

                        DynamicBuffer<OperationMapBuildingProductionPrefab> productions =
                            em.GetBuffer<OperationMapBuildingProductionPrefab>(candidate, true);
                        int matchingProductionIndex = -1;
                        int matchingProductionQuantity = 0;
                        for (int bufferIndex = 0; bufferIndex < productions.Length; bufferIndex++)
                        {
                            OperationMapBuildingProductionPrefab production = productions[bufferIndex];
                            if (production.Prefab == Entity.Null ||
                                !em.Exists(production.Prefab) ||
                                production.SourceKey != unitSourceKey)
                            {
                                continue;
                            }

                            matchingProductionIndex = production.ProductionIndex;
                            matchingProductionQuantity = math.max(1, production.Quantity);
                            break;
                        }

                        if (matchingProductionIndex < 0)
                            continue;

                        int placementIndex = em.GetComponentData<OperationMapBuildingComponent>(candidate).PlacementIndex;
                        if (bestEntity != Entity.Null &&
                            (matchingProductionQuantity < bestProductionQuantity ||
                             (matchingProductionQuantity == bestProductionQuantity &&
                              (placementIndex > bestPlacementIndex ||
                               (placementIndex == bestPlacementIndex && candidate.Index >= bestEntity.Index)))))
                        {
                            continue;
                        }

                        bestEntity = candidate;
                        bestProductionIndex = matchingProductionIndex;
                        bestProductionQuantity = matchingProductionQuantity;
                        bestPlacementIndex = placementIndex;
                        bestDisplayName = em.GetComponentData<UnitDisplayInfo>(candidate).Name.ToString();
                    }
                }

                if (bestEntity == Entity.Null)
                    continue;

                buildingEntity = bestEntity;
                productionIndex = bestProductionIndex;
                buildingDisplayName = bestDisplayName;
                return true;
            }

            return false;
        }
    }
}
