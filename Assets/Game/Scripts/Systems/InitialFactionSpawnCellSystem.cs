using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Game.Components;

namespace Game.Runtime
{
    public readonly struct InitialFactionSpawnCellFallbackEntry
    {
        public readonly byte FactionId;
        public readonly int2 SpawnCell;

        public InitialFactionSpawnCellFallbackEntry(byte factionId, int2 spawnCell)
        {
            FactionId = factionId;
            SpawnCell = spawnCell;
        }
    }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct InitialFactionSpawnCellSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            // RequireForUpdate intentionally omitted: disabled startup helper; composition calls its methods directly.
            state.Enabled = false;
        }

        public void OnUpdate(ref SystemState state)
        {
        }

        public bool TryGetConfiguredFactionSpawnCell(
            EntityManager em,
            IReadOnlyList<InitialFactionSpawnCellFallbackEntry> fallbackFactionSpawns,
            byte factionId,
            out int2 spawnCell)
        {
            if (TryGetBakedFactionSpawnCell(em, factionId, out spawnCell))
                return true;

            return TryGetFallbackFactionSpawnCell(fallbackFactionSpawns, factionId, out spawnCell);
        }

        private static bool TryGetBakedFactionSpawnCell(EntityManager em, byte factionId, out int2 spawnCell)
        {
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

        private static bool TryGetFallbackFactionSpawnCell(
            IReadOnlyList<InitialFactionSpawnCellFallbackEntry> fallbackFactionSpawns,
            byte factionId,
            out int2 spawnCell)
        {
            if (fallbackFactionSpawns == null)
            {
                spawnCell = default;
                return false;
            }

            for (int i = 0; i < fallbackFactionSpawns.Count; i++)
            {
                InitialFactionSpawnCellFallbackEntry faction = fallbackFactionSpawns[i];
                if (faction.FactionId != factionId)
                    continue;

                spawnCell = faction.SpawnCell;
                return true;
            }

            spawnCell = default;
            return false;
        }
    }
}
