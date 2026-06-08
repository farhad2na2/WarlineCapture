using Unity.Entities;
using Unity.Collections;

public static class RespawnQueueUtility
{
    public static Entity GetOrCreateQueue(ref SystemState state)
    {
        var em = state.EntityManager;
        using var q = em.CreateEntityQuery(ComponentType.ReadOnly<RespawnQueueTag>());
        if (!q.IsEmptyIgnoreFilter)
            return q.GetSingletonEntity();

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
