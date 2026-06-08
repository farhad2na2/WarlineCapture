using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public readonly struct InitialRespawnQueueProjectionSystem
{
    public Entity GetOrCreateQueue(ref SystemState state)
    {
        return RespawnQueueUtility.GetOrCreateQueue(ref state);
    }

    public RespawnQueueComponent ProjectInitialConfig(
        EntityManager em,
        Entity queueEntity,
        InitialUnitsSpawnConfig config,
        NativeArray<InitialUnitsFactionSpawnEntry> factionSpawns)
    {
        RespawnQueueComponent queueState = em.GetComponentData<RespawnQueueComponent>(queueEntity);
        queueState.SpawnRadiusCells = math.max(0, config.SpawnRadiusCells);
        queueState.RespawnDelaySeconds = math.max(0.01f, config.RespawnDelaySeconds);

        DynamicBuffer<RespawnFactionSpawnPoint> respawnSpawnPoints = em.GetBuffer<RespawnFactionSpawnPoint>(queueEntity);
        respawnSpawnPoints.Clear();
        for (int factionIndex = 0; factionIndex < factionSpawns.Length; factionIndex++)
        {
            respawnSpawnPoints.Add(new RespawnFactionSpawnPoint
            {
                FactionId = factionSpawns[factionIndex].FactionId,
                SpawnCell = factionSpawns[factionIndex].SpawnCell
            });
        }

        return queueState;
    }

    public void WriteRandomState(
        EntityManager em,
        Entity queueEntity,
        RespawnQueueComponent queueState,
        uint randomState)
    {
        queueState.RandomState = randomState;
        em.SetComponentData(queueEntity, queueState);
    }
}
