using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public readonly struct InitialUnitSpawnBatchSystem
{
    public readonly struct EntryBatch
    {
        public readonly int UnitIndex;
        public readonly InitialUnitsFactionUnitSpawnEntry UnitSpawn;
        public readonly InitialUnitsFactionUnitSpawnProgress EntryProgress;
        public readonly int ToSpawn;
        public readonly bool HasPrefab;

        public EntryBatch(
            int unitIndex,
            InitialUnitsFactionUnitSpawnEntry unitSpawn,
            InitialUnitsFactionUnitSpawnProgress entryProgress,
            int toSpawn,
            bool hasPrefab)
        {
            UnitIndex = unitIndex;
            UnitSpawn = unitSpawn;
            EntryProgress = entryProgress;
            ToSpawn = toSpawn;
            HasPrefab = hasPrefab;
        }
    }

    public readonly struct SpawnPlan
    {
        public readonly int2 UnitSpawnCenter;
        public readonly int2 FootprintSize;
        public readonly bool IsAirUnit;

        public SpawnPlan(int2 unitSpawnCenter, int2 footprintSize, bool isAirUnit)
        {
            UnitSpawnCenter = unitSpawnCenter;
            FootprintSize = footprintSize;
            IsAirUnit = isAirUnit;
        }
    }

    public bool TryCreateEntryBatch(
        DynamicBuffer<InitialUnitsFactionUnitSpawnEntry> unitSpawns,
        DynamicBuffer<InitialUnitsFactionUnitSpawnProgress> unitProgress,
        int unitIndex,
        int remainingBatch,
        out EntryBatch batch)
    {
        InitialUnitsFactionUnitSpawnEntry unitSpawn = unitSpawns[unitIndex];
        InitialUnitsFactionUnitSpawnProgress entryProgress = unitProgress[unitIndex];
        int remaining = math.max(0, unitSpawn.Count - entryProgress.Spawned);
        int toSpawn = math.min(remainingBatch, remaining);
        bool hasPrefab = unitSpawn.Prefab != Entity.Null;
        batch = new EntryBatch(unitIndex, unitSpawn, entryProgress, toSpawn, hasPrefab);
        return toSpawn > 0;
    }

    public bool TryCreateSpawnPlan(
        EntityManager em,
        NativeArray<InitialUnitsFactionSpawnEntry> factionSpawns,
        in EntryBatch batch,
        out SpawnPlan plan)
    {
        if (!TryGetFactionSpawnCell(factionSpawns, batch.UnitSpawn.FactionId, out int2 factionSpawnCell))
        {
            plan = default;
            return false;
        }

        int2 unitSpawnCenter = factionSpawnCell + batch.UnitSpawn.SpawnOffset;
        int2 footprintSize = batch.HasPrefab && em.HasComponent<UnitFootprint>(batch.UnitSpawn.Prefab)
            ? em.GetComponentData<UnitFootprint>(batch.UnitSpawn.Prefab).Size
            : new int2(1, 1);
        bool isAirUnit = batch.HasPrefab && em.HasComponent<UnitAirMovement>(batch.UnitSpawn.Prefab);
        plan = new SpawnPlan(unitSpawnCenter, footprintSize, isAirUnit);
        return true;
    }

    public void ApplySpawnedCount(
        DynamicBuffer<InitialUnitsFactionUnitSpawnProgress> unitProgress,
        in EntryBatch batch,
        int spawnedThisEntry,
        ref int remainingBatch)
    {
        InitialUnitsFactionUnitSpawnProgress entryProgress = batch.EntryProgress;
        entryProgress.Spawned += spawnedThisEntry;
        unitProgress[batch.UnitIndex] = entryProgress;
        remainingBatch -= spawnedThisEntry;
    }

    private static bool TryGetFactionSpawnCell(
        NativeArray<InitialUnitsFactionSpawnEntry> spawns,
        byte factionId,
        out int2 spawnCell)
    {
        for (int i = 0; i < spawns.Length; i++)
        {
            if (spawns[i].FactionId != factionId)
                continue;

            spawnCell = spawns[i].SpawnCell;
            return true;
        }

        spawnCell = default;
        return false;
    }
}
