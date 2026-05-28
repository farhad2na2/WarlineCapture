using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public readonly struct InitialUnitSourceKeySystem
{
    public bool TryGetCustomGameUnitSourceKey(
        DynamicBuffer<CustomGameFactionUnitSourceSpawnEntry> sourceSpawns,
        bool hasSourceSpawns,
        int unitIndex,
        InitialUnitsFactionUnitSpawnEntry unitSpawn,
        out FixedString64Bytes sourceKey)
    {
        sourceKey = default;
        if (!hasSourceSpawns || unitIndex < 0 || unitIndex >= sourceSpawns.Length)
            return false;

        CustomGameFactionUnitSourceSpawnEntry sourceSpawn = sourceSpawns[unitIndex];
        if (sourceSpawn.FactionId != unitSpawn.FactionId ||
            sourceSpawn.Count != unitSpawn.Count ||
            !math.all(sourceSpawn.SpawnOffset == unitSpawn.SpawnOffset) ||
            sourceSpawn.SourceKey.Length == 0)
        {
            return false;
        }

        sourceKey = sourceSpawn.SourceKey;
        return true;
    }

    public bool TrySkipMissingPrefabUnit(
        EntityManager em,
        InitialUnitsFactionUnitSpawnEntry unitSpawn,
        bool hasPrefab,
        bool hasSourceKey,
        FixedString64Bytes sourceKey,
        ref InitialUnitsFactionUnitSpawnProgress entryProgress,
        ref InitialSpawnDiagnosticLogSystem diagnosticLogSystem)
    {
        if (hasPrefab)
            return false;

        if (hasSourceKey)
            diagnosticLogSystem.EnqueueWarning(em, $"[InitialSpawn] skipped source-key unit because no ECS prefab entity was resolved. sourceKey={sourceKey.ToString()} faction={unitSpawn.FactionId} count={unitSpawn.Count}");

        entryProgress.Spawned = unitSpawn.Count;
        return true;
    }
}
