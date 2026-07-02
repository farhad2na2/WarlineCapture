using Unity.Entities;
using Unity.Collections;
using Game.Components;

namespace Game.Runtime
{
    public static class RespawnQueueUtility
    {
        public static Entity GetOrCreateQueue(ref SystemState state)
        {
            var em = state.EntityManager;
            using var q = em.CreateEntityQuery(ComponentType.ReadOnly<RespawnQueueTag>());
            return GetOrCreateQueue(ref state, q);
        }

        public static Entity GetOrCreateQueue(ref SystemState state, EntityQuery queueQuery)
        {
            var em = state.EntityManager;
            if (!queueQuery.IsEmptyIgnoreFilter)
                return queueQuery.GetSingletonEntity();

            var e = em.CreateEntity();
            em.AddComponentData(e, new RespawnQueueTag());
            em.AddComponentData(e, new RespawnQueueComponent
            {
                RandomState = 0x12345678u,
                SpawnRadiusCells = 0,
                RespawnDelaySeconds = 10f
            });
            em.AddBuffer<RespawnRequest>(e);
            em.AddBuffer<RespawnFactionSpawnPoint>(e);
            return e;
        }
    }
}
