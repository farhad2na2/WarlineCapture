using Unity.Entities;
using Unity.Mathematics;

public readonly struct InitialMissionRosterSystem
{
    public bool ShouldSkipInitialBuildingRequests(bool useM01CompactRuntime)
    {
        return useM01CompactRuntime;
    }

    public void ApplyM01CompactUnitRoster(
        DynamicBuffer<InitialUnitsFactionUnitSpawnEntry> unitSpawns,
        DynamicBuffer<InitialUnitsFactionUnitSpawnProgress> unitProgress)
    {
        bool playerAssigned = false;
        bool enemyAssigned = false;
        for (int i = 0; i < unitSpawns.Length; i++)
        {
            InitialUnitsFactionUnitSpawnEntry unit = unitSpawns[i];
            bool isPlayer = unit.FactionId == 0;
            bool isEnemy = unit.FactionId == 1;
            bool keep = unit.Prefab != Entity.Null &&
                ((isPlayer && !playerAssigned) || (isEnemy && !enemyAssigned));

            unit.Count = keep ? 1 : 0;
            unit.SpawnOffset = int2.zero;
            unitSpawns[i] = unit;

            if (!keep)
            {
                InitialUnitsFactionUnitSpawnProgress progress = unitProgress[i];
                progress.Spawned = 0;
                unitProgress[i] = progress;
                continue;
            }

            if (isPlayer)
                playerAssigned = true;
            if (isEnemy)
                enemyAssigned = true;
        }
    }
}
