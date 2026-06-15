using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public sealed partial class InitialFactionSpawnCellSystem : SystemBase
{
    private InitialUnitsSpawnerAuthoringConfig _fallbackInitialUnitsConfig;

    protected override void OnCreate()
    {
        Enabled = false;
    }

    protected override void OnUpdate()
    {
    }

    public void Configure(InitialUnitsSpawnerAuthoringConfig fallbackInitialUnitsConfig)
    {
        _fallbackInitialUnitsConfig = fallbackInitialUnitsConfig;
    }

    public bool TryGetConfiguredFactionSpawnCell(byte factionId, out int2 spawnCell)
    {
        if (TryGetBakedFactionSpawnCell(factionId, out spawnCell))
            return true;

        return TryGetFallbackFactionSpawnCell(factionId, out spawnCell);
    }

    private bool TryGetBakedFactionSpawnCell(byte factionId, out int2 spawnCell)
    {
        EntityManager em = EntityManager;
        using EntityQuery query = em.CreateEntityQuery(
            ComponentType.ReadOnly<InitialUnitsSpawnConfig>(),
            ComponentType.ReadOnly<InitialUnitsFactionSpawnEntry>());
        EntityTypeHandle entityType = em.GetEntityTypeHandle();
        using NativeArray<ArchetypeChunk> chunks = query.ToArchetypeChunkArray(Allocator.Temp);
        for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
        {
            ArchetypeChunk chunk = chunks[chunkIndex];
            NativeArray<Entity> entities = chunk.GetNativeArray(entityType);
            for (int entityIndex = 0; entityIndex < entities.Length; entityIndex++)
            {
                DynamicBuffer<InitialUnitsFactionSpawnEntry> factionSpawns = em.GetBuffer<InitialUnitsFactionSpawnEntry>(entities[entityIndex]);
                for (int i = 0; i < factionSpawns.Length; i++)
                {
                    if (factionSpawns[i].FactionId != factionId)
                        continue;

                    spawnCell = factionSpawns[i].SpawnCell;
                    return true;
                }
            }
        }

        spawnCell = default;
        return false;
    }

    private bool TryGetFallbackFactionSpawnCell(byte factionId, out int2 spawnCell)
    {
        if (_fallbackInitialUnitsConfig == null || _fallbackInitialUnitsConfig.Factions == null)
        {
            spawnCell = default;
            return false;
        }

        for (int i = 0; i < _fallbackInitialUnitsConfig.Factions.Count; i++)
        {
            InitialUnitsSpawnerAuthoringConfig.FactionEntry faction = _fallbackInitialUnitsConfig.Factions[i];
            if (faction == null || faction.FactionId != factionId)
                continue;

            spawnCell = new int2(faction.SpawnCell.x, faction.SpawnCell.y);
            return true;
        }

        spawnCell = default;
        return false;
    }
}
