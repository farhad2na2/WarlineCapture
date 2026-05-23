using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public sealed class InitialFactionSpawnCellSystem
{
    private World _world;
    private InitialUnitsSpawnerAuthoringConfig _fallbackInitialUnitsConfig;

    public void Configure(World world, InitialUnitsSpawnerAuthoringConfig fallbackInitialUnitsConfig)
    {
        _world = world;
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
        if (_world == null || !_world.IsCreated)
        {
            spawnCell = default;
            return false;
        }

        EntityManager em = _world.EntityManager;
        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<InitialUnitsSpawnConfig>());
        using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
        for (int entityIndex = 0; entityIndex < entities.Length; entityIndex++)
        {
            Entity entity = entities[entityIndex];
            if (!em.Exists(entity) || !em.HasBuffer<InitialUnitsFactionSpawnEntry>(entity))
                continue;

            DynamicBuffer<InitialUnitsFactionSpawnEntry> factionSpawns = em.GetBuffer<InitialUnitsFactionSpawnEntry>(entity);
            for (int i = 0; i < factionSpawns.Length; i++)
            {
                if (factionSpawns[i].FactionId != factionId)
                    continue;

                spawnCell = factionSpawns[i].SpawnCell;
                return true;
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
