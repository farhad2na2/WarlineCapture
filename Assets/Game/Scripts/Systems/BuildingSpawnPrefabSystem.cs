using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

internal sealed class BuildingSpawnPrefabSystem
{
    public delegate string ResolveSpawnableLookupKeyDelegate(GameObject prefab);

    public readonly struct Context
    {
        public readonly IReadOnlyList<GameObject> UnitSpawnPrefabs;
        public readonly EntityQuery UnitPrefabRegistryQuery;
        public readonly EntityQuery SpawnPrefabCandidatesQuery;
        public readonly EntityQuery LivePlayerUnitsQuery;
        public readonly ResolveSpawnableLookupKeyDelegate ResolveSpawnableLookupKey;

        public Context(
            IReadOnlyList<GameObject> unitSpawnPrefabs,
            EntityQuery unitPrefabRegistryQuery,
            EntityQuery spawnPrefabCandidatesQuery,
            EntityQuery livePlayerUnitsQuery,
            ResolveSpawnableLookupKeyDelegate resolveSpawnableLookupKey = null)
        {
            UnitSpawnPrefabs = unitSpawnPrefabs;
            UnitPrefabRegistryQuery = unitPrefabRegistryQuery;
            SpawnPrefabCandidatesQuery = spawnPrefabCandidatesQuery;
            LivePlayerUnitsQuery = livePlayerUnitsQuery;
            ResolveSpawnableLookupKey = resolveSpawnableLookupKey;
        }
    }

    public bool TryResolveSpawnUnitPrefabFromRegistry(Context context, EntityManager em, Entity prefabEntity, out GameObject spawnUnitPrefab)
    {
        spawnUnitPrefab = null;
        if (prefabEntity == Entity.Null ||
            context.UnitPrefabRegistryQuery.IsEmptyIgnoreFilter ||
            context.UnitSpawnPrefabs == null ||
            context.UnitSpawnPrefabs.Count == 0)
        {
            return false;
        }

        Entity registryEntity = context.UnitPrefabRegistryQuery.GetSingletonEntity();
        DynamicBuffer<UnitPrefabRegistryEntry> registry = em.GetBuffer<UnitPrefabRegistryEntry>(registryEntity);
        int count = math.min(registry.Length, context.UnitSpawnPrefabs.Count);
        if (count <= 0)
            return false;

        for (int i = 0; i < count; i++)
        {
            if (registry[i].Prefab != prefabEntity)
                continue;

            spawnUnitPrefab = context.UnitSpawnPrefabs[i];
            return spawnUnitPrefab != null;
        }

        return false;
    }

    public bool TryGetSpawnUnitPrefabEntity(Context context, EntityManager em, GameObject spawnUnitPrefab, out Entity prefabEntity)
    {
        prefabEntity = Entity.Null;
        if (spawnUnitPrefab == null)
            return false;

        return TryGetSpawnUnitPrefabEntityFromRegistry(context, em, spawnUnitPrefab, out prefabEntity) ||
               TryGetSpawnUnitPrefabEntityFromPrefabQuery(context, em, spawnUnitPrefab, out prefabEntity) ||
               TryGetPlayerUnitPrefabEntityFromLiveUnits(context, em, spawnUnitPrefab, out prefabEntity);
    }

    private bool TryGetSpawnUnitPrefabEntityFromRegistry(Context context, EntityManager em, GameObject spawnUnitPrefab, out Entity prefabEntity)
    {
        prefabEntity = Entity.Null;
        if (context.UnitPrefabRegistryQuery.IsEmptyIgnoreFilter ||
            context.UnitSpawnPrefabs == null ||
            context.UnitSpawnPrefabs.Count == 0)
        {
            return false;
        }

        Entity registryEntity = context.UnitPrefabRegistryQuery.GetSingletonEntity();
        DynamicBuffer<UnitPrefabRegistryEntry> registry = em.GetBuffer<UnitPrefabRegistryEntry>(registryEntity);
        string targetKey = GetSpawnableLookupKey(context, spawnUnitPrefab);
        int count = math.min(registry.Length, context.UnitSpawnPrefabs.Count);
        if (string.IsNullOrEmpty(targetKey) || count <= 0)
            return false;

        for (int i = 0; i < count; i++)
        {
            GameObject configuredPrefab = context.UnitSpawnPrefabs[i];
            if (configuredPrefab == null)
                continue;

            if (!NamesMatch(GetSpawnableLookupKey(context, configuredPrefab), targetKey))
                continue;

            prefabEntity = registry[i].Prefab;
            return prefabEntity != Entity.Null;
        }

        return false;
    }

    private bool TryGetSpawnUnitPrefabEntityFromPrefabQuery(Context context, EntityManager em, GameObject spawnUnitPrefab, out Entity prefabEntity)
    {
        prefabEntity = Entity.Null;
        string targetName = spawnUnitPrefab.name;
        EntityTypeHandle entityType = em.GetEntityTypeHandle();
        using NativeArray<ArchetypeChunk> chunks = context.SpawnPrefabCandidatesQuery.ToArchetypeChunkArray(Allocator.Temp);

        for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
        {
            NativeArray<Entity> entities = chunks[chunkIndex].GetNativeArray(entityType);
            for (int i = 0; i < entities.Length; i++)
            {
                Entity candidate = entities[i];
                if (!NamesMatch(em.GetName(candidate), targetName))
                    continue;

                prefabEntity = candidate;
                return true;
            }
        }

        return false;
    }

    private bool TryGetPlayerUnitPrefabEntityFromLiveUnits(Context context, EntityManager em, GameObject spawnUnitPrefab, out Entity prefabEntity)
    {
        prefabEntity = Entity.Null;
        string targetName = spawnUnitPrefab != null ? spawnUnitPrefab.name : string.Empty;
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
                if (!FactionIdentitySystem.IsPlayerControlled(factions[i].Id))
                    continue;

                Entity candidate = respawnPrefabs[i].Prefab;
                if (candidate == Entity.Null)
                    continue;
                if (!NamesMatch(em.GetName(candidate), targetName))
                    continue;

                prefabEntity = candidate;
                return true;
            }
        }

        return false;
    }

    private static string GetSpawnableLookupKey(Context context, GameObject prefab)
    {
        if (prefab == null)
            return string.Empty;

        string configuredKey = context.ResolveSpawnableLookupKey?.Invoke(prefab);
        if (!string.IsNullOrWhiteSpace(configuredKey))
            return NormalizeSpawnableKey(configuredKey);

        return NormalizeSpawnableKey(prefab.name);
    }

    private static string NormalizeSpawnableKey(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();
    }

    private static bool NamesMatch(string candidateName, string targetName)
    {
        if (string.IsNullOrWhiteSpace(candidateName) || string.IsNullOrWhiteSpace(targetName))
            return false;

        return string.Equals(candidateName, targetName, System.StringComparison.OrdinalIgnoreCase) ||
               string.Equals(candidateName.Replace(" (Clone)", string.Empty), targetName, System.StringComparison.OrdinalIgnoreCase);
    }
}
