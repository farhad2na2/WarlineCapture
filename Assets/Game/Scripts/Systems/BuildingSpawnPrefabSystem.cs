using Unity.Collections;
using Unity.Entities;

[UpdateInGroup(typeof(SimulationSystemGroup))]
internal partial struct BuildingSpawnPrefabSystem : ISystem
{
    public readonly struct Context
    {
        public readonly EntityQuery UnitPrefabRegistryQuery;
        public readonly EntityQuery SpawnPrefabCandidatesQuery;
        public readonly EntityQuery LivePlayerUnitsQuery;

        public Context(
            EntityQuery unitPrefabRegistryQuery,
            EntityQuery spawnPrefabCandidatesQuery,
            EntityQuery livePlayerUnitsQuery)
        {
            UnitPrefabRegistryQuery = unitPrefabRegistryQuery;
            SpawnPrefabCandidatesQuery = spawnPrefabCandidatesQuery;
            LivePlayerUnitsQuery = livePlayerUnitsQuery;
        }
    }

    public void OnCreate(ref SystemState state)
    {
        // RequireForUpdate intentionally omitted: disabled helper; runtime systems call prefab lookup methods directly.
        state.Enabled = false;
    }

    public void OnUpdate(ref SystemState state)
    {
    }

    public bool TryResolveSpawnUnitSourceKey(
        Context context,
        EntityManager em,
        Entity prefabEntity,
        out FixedString64Bytes sourceKey)
    {
        sourceKey = default;
        if (prefabEntity == Entity.Null || !em.Exists(prefabEntity))
            return false;

        if (em.HasComponent<UnitSourcePrefabKey>(prefabEntity))
            sourceKey = em.GetComponentData<UnitSourcePrefabKey>(prefabEntity).Value;

        if (sourceKey.Length == 0)
        {
            string name = em.GetName(prefabEntity);
            if (!string.IsNullOrWhiteSpace(name))
                sourceKey = new FixedString64Bytes(NormalizeSourceKey(name));
        }

        if (sourceKey.Length == 0)
            return false;

        if (context.UnitPrefabRegistryQuery.IsEmptyIgnoreFilter)
            return true;

        Entity registryEntity = context.UnitPrefabRegistryQuery.GetSingletonEntity();
        DynamicBuffer<UnitPrefabRegistryEntry> registry = em.GetBuffer<UnitPrefabRegistryEntry>(registryEntity);
        for (int i = 0; i < registry.Length; i++)
        {
            if (registry[i].Prefab == prefabEntity)
                return true;
        }

        return true;
    }

    public bool TryGetSpawnUnitPrefabEntity(
        Context context,
        EntityManager em,
        FixedString64Bytes sourceKey,
        out Entity prefabEntity)
    {
        prefabEntity = Entity.Null;
        if (sourceKey.Length == 0)
            return false;

        return TryGetSpawnUnitPrefabEntityFromRegistry(context, em, sourceKey, out prefabEntity) ||
               TryGetSpawnUnitPrefabEntityFromPrefabQuery(context, em, sourceKey, out prefabEntity) ||
               TryGetPlayerUnitPrefabEntityFromLiveUnits(context, em, sourceKey, out prefabEntity);
    }

    private static bool TryGetSpawnUnitPrefabEntityFromRegistry(
        Context context,
        EntityManager em,
        FixedString64Bytes sourceKey,
        out Entity prefabEntity)
    {
        prefabEntity = Entity.Null;
        if (context.UnitPrefabRegistryQuery.IsEmptyIgnoreFilter)
            return false;

        Entity registryEntity = context.UnitPrefabRegistryQuery.GetSingletonEntity();
        DynamicBuffer<UnitPrefabRegistryEntry> registry = em.GetBuffer<UnitPrefabRegistryEntry>(registryEntity);
        for (int i = 0; i < registry.Length; i++)
        {
            Entity candidate = registry[i].Prefab;
            if (candidate == Entity.Null || !EntityMatchesSourceKey(em, candidate, sourceKey))
                continue;

            prefabEntity = candidate;
            return prefabEntity != Entity.Null;
        }

        return false;
    }

    private static bool TryGetSpawnUnitPrefabEntityFromPrefabQuery(
        Context context,
        EntityManager em,
        FixedString64Bytes sourceKey,
        out Entity prefabEntity)
    {
        prefabEntity = Entity.Null;
        EntityTypeHandle entityType = em.GetEntityTypeHandle();
        using NativeArray<ArchetypeChunk> chunks = context.SpawnPrefabCandidatesQuery.ToArchetypeChunkArray(Allocator.Temp);

        for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
        {
            NativeArray<Entity> entities = chunks[chunkIndex].GetNativeArray(entityType);
            for (int i = 0; i < entities.Length; i++)
            {
                Entity candidate = entities[i];
                if (!EntityMatchesSourceKey(em, candidate, sourceKey))
                    continue;

                prefabEntity = candidate;
                return true;
            }
        }

        return false;
    }

    private static bool TryGetPlayerUnitPrefabEntityFromLiveUnits(
        Context context,
        EntityManager em,
        FixedString64Bytes sourceKey,
        out Entity prefabEntity)
    {
        prefabEntity = Entity.Null;
        EntityTypeHandle entityType = em.GetEntityTypeHandle();
        ComponentTypeHandle<Faction> factionType = em.GetComponentTypeHandle<Faction>(true);
        ComponentTypeHandle<UnitRespawnPrefab> respawnPrefabType = em.GetComponentTypeHandle<UnitRespawnPrefab>(true);
        using NativeArray<ArchetypeChunk> chunks = context.LivePlayerUnitsQuery.ToArchetypeChunkArray(Allocator.Temp);
        for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
        {
            ArchetypeChunk chunk = chunks[chunkIndex];
            NativeArray<Entity> entities = chunk.GetNativeArray(entityType);
            NativeArray<Faction> factions = chunk.GetNativeArray(ref factionType);
            NativeArray<UnitRespawnPrefab> respawnPrefabs = chunk.GetNativeArray(ref respawnPrefabType);
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                if (em.HasComponent<StaticGridBlocker>(entity))
                    continue;
                if (!FactionIdentity.IsPlayerControlled(factions[i].Id))
                    continue;

                Entity candidate = respawnPrefabs[i].Prefab;
                if (candidate == Entity.Null)
                    continue;
                if (!EntityMatchesSourceKey(em, candidate, sourceKey))
                    continue;

                prefabEntity = candidate;
                return true;
            }
        }

        return false;
    }

    private static bool EntityMatchesSourceKey(EntityManager em, Entity candidate, FixedString64Bytes sourceKey)
    {
        if (candidate == Entity.Null || !em.Exists(candidate) || sourceKey.Length == 0)
            return false;

        if (em.HasComponent<UnitSourcePrefabKey>(candidate) &&
            SourceKeysMatch(em.GetComponentData<UnitSourcePrefabKey>(candidate).Value, sourceKey))
        {
            return true;
        }

        return SourceKeysMatch(em.GetName(candidate), sourceKey);
    }

    private static bool SourceKeysMatch(FixedString64Bytes candidate, FixedString64Bytes target)
    {
        return SourceKeysMatch(candidate.ToString(), target);
    }

    private static bool SourceKeysMatch(string candidate, FixedString64Bytes target)
    {
        if (string.IsNullOrWhiteSpace(candidate) || target.Length == 0)
            return false;

        string targetKey = NormalizeSourceKey(target.ToString());
        string candidateKey = NormalizeSourceKey(candidate);
        return string.Equals(candidateKey, targetKey, System.StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeSourceKey(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Replace(" (Clone)", string.Empty).Trim().ToLowerInvariant();
    }
}
