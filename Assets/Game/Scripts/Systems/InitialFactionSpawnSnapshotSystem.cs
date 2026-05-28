using Unity.Collections;
using Unity.Entities;

public readonly struct InitialFactionSpawnSnapshotSystem
{
    public NativeArray<InitialUnitsFactionSpawnEntry> Create(
        EntityManager em,
        Entity configEntity,
        Allocator allocator)
    {
        DynamicBuffer<InitialUnitsFactionSpawnEntry> factionSpawnsBuffer = em.GetBuffer<InitialUnitsFactionSpawnEntry>(configEntity);
        var factionSpawns = new NativeArray<InitialUnitsFactionSpawnEntry>(factionSpawnsBuffer.Length, allocator);
        for (int factionIndex = 0; factionIndex < factionSpawnsBuffer.Length; factionIndex++)
            factionSpawns[factionIndex] = factionSpawnsBuffer[factionIndex];

        return factionSpawns;
    }
}
