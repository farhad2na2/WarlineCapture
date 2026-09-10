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
        private static int CountFriendlyPendingUnitProductions(Context context)
        {
            int count = 0;
            if (context.RuntimeBuildings is Dictionary<int, RuntimeBuildingEntity> runtimeBuildings)
            {
                foreach (KeyValuePair<int, RuntimeBuildingEntity> pair in runtimeBuildings)
                    count += CountPendingProductionsForGlobalLimit(pair.Value);
            }
            else if (context.RuntimeBuildings != null)
            {
                foreach (KeyValuePair<int, RuntimeBuildingEntity> pair in context.RuntimeBuildings)
                    count += CountPendingProductionsForGlobalLimit(pair.Value);
            }

            if (context.TryGetEntityManager == null ||
                !context.TryGetEntityManager(out EntityManager em) ||
                em.World == null ||
                !em.World.IsCreated)
            {
                return count;
            }

            using EntityQuery queueQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<OperationMapBuildingProductionQueueComponent>(),
                ComponentType.ReadOnly<OperationMapBuildingUnitProductionRequest>(),
                ComponentType.ReadOnly<Faction>(),
                ComponentType.ReadOnly<UnitHealth>());
            using NativeArray<ArchetypeChunk> queueQueryChunks = queueQuery.ToArchetypeChunkArray(Allocator.Temp);
            var queueOwnersType = em.GetEntityTypeHandle();
            foreach(var sourceChunk in queueQueryChunks)
            {
                var queueOwners = sourceChunk.GetNativeArray(queueOwnersType);
                for (int ownerIndex = 0; ownerIndex < queueOwners.Length; ownerIndex++)
                {
                    Entity owner = queueOwners[ownerIndex];
                    byte factionId = em.GetComponentData<Faction>(owner).Id;
                    if ((factionId != FactionIdentity.PlayerFactionId &&
                         factionId != FactionIdentity.NeutralFactionId) ||
                        em.GetComponentData<UnitHealth>(owner).Current <= 0 ||
                        em.HasComponent<Prefab>(owner) ||
                        (em.HasComponent<OperationMapBuildingDestroyedComponent>(owner) &&
                         em.IsComponentEnabled<OperationMapBuildingDestroyedComponent>(owner)))
                    {
                        continue;
                    }

                    DynamicBuffer<OperationMapBuildingUnitProductionRequest> queue =
                        em.GetBuffer<OperationMapBuildingUnitProductionRequest>(owner, true);
                    for (int queueIndex = 0; queueIndex < queue.Length; queueIndex++)
                    {
                        if (queue[queueIndex].Status == OperationMapBuildingUnitProductionRequest.Pending)
                            count++;
                    }
                }
            }

            return count;
        }
    }
}
