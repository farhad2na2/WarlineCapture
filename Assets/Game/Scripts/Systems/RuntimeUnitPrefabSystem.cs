using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

internal sealed partial class RuntimeUnitPrefabSystem : SystemBase
{
    public readonly struct Context
    {
        public readonly BuildingDefinitionSystem DefinitionSystem;
        public readonly BuildingSpawnPrefabSystem SpawnPrefabSystem;
        public readonly IReadOnlyDictionary<int, RuntimeBuildingEntity> RuntimeBuildings;
        public readonly CitizenPrefabSystem.TryGetEntityManagerDelegate TryGetEntityManager;
        public readonly Action<EntityManager> EnsureEntityQueries;
        public readonly Func<BuildingSpawnPrefabSystem.Context> CreateSpawnPrefabContext;

        public Context(
            BuildingDefinitionSystem definitionSystem,
            BuildingSpawnPrefabSystem spawnPrefabSystem,
            IReadOnlyDictionary<int, RuntimeBuildingEntity> runtimeBuildings,
            CitizenPrefabSystem.TryGetEntityManagerDelegate tryGetEntityManager,
            Action<EntityManager> ensureEntityQueries,
            Func<BuildingSpawnPrefabSystem.Context> createSpawnPrefabContext)
        {
            DefinitionSystem = definitionSystem;
            SpawnPrefabSystem = spawnPrefabSystem;
            RuntimeBuildings = runtimeBuildings;
            TryGetEntityManager = tryGetEntityManager;
            EnsureEntityQueries = ensureEntityQueries;
            CreateSpawnPrefabContext = createSpawnPrefabContext;
        }
    }

    public CitizenPrefabSystem.Context CreateCitizenPrefabContext(Context context)
    {
        return new CitizenPrefabSystem.Context(
            context.SpawnPrefabSystem,
            context.TryGetEntityManager,
            context.EnsureEntityQueries,
            context.CreateSpawnPrefabContext);
    }

    protected override void OnCreate()
    {
        Enabled = false;
    }

    protected override void OnUpdate()
    {
    }

    public bool TryResolveConfiguredUnitPrefabEntity(Context context, GameObject unitPrefab, out Entity prefabEntity)
    {
        prefabEntity = Entity.Null;
        if (unitPrefab == null ||
            context.TryGetEntityManager == null ||
            !context.TryGetEntityManager(out EntityManager em))
        {
            return false;
        }

        context.EnsureEntityQueries?.Invoke(em);
        FixedString64Bytes sourceKey = GetUnitPrefabSourceKey(unitPrefab);
        return context.SpawnPrefabSystem.TryGetSpawnUnitPrefabEntity(
            context.CreateSpawnPrefabContext != null ? context.CreateSpawnPrefabContext() : default,
            em,
            sourceKey,
            out prefabEntity);
    }

    public bool TryResolveSpawnUnitPrefab(Context context, Entity prefabEntity, out GameObject spawnUnitPrefab)
    {
        spawnUnitPrefab = null;
        if (prefabEntity == Entity.Null ||
            context.TryGetEntityManager == null ||
            !context.TryGetEntityManager(out EntityManager em))
        {
            return false;
        }

        context.EnsureEntityQueries?.Invoke(em);
        if (!context.SpawnPrefabSystem.TryResolveSpawnUnitSourceKey(
                context.CreateSpawnPrefabContext != null ? context.CreateSpawnPrefabContext() : default,
                em,
                prefabEntity,
                out FixedString64Bytes sourceKey) ||
            sourceKey.Length == 0 ||
            context.DefinitionSystem == null)
        {
            return false;
        }

        return context.DefinitionSystem.TryResolveConfiguredUnitSpawnPrefab(sourceKey.ToString(), out spawnUnitPrefab) &&
               spawnUnitPrefab != null;
    }

    public bool TryResolveLiveUnitPreviewPrefab(Context context, Entity unitEntity, out GameObject prefab)
    {
        prefab = null;
        if (unitEntity == Entity.Null ||
            context.TryGetEntityManager == null ||
            !context.TryGetEntityManager(out EntityManager em) ||
            !em.Exists(unitEntity))
        {
            return false;
        }

        if (em.HasComponent<UnitRespawnPrefab>(unitEntity))
        {
            Entity prefabEntity = em.GetComponentData<UnitRespawnPrefab>(unitEntity).Prefab;
            if (prefabEntity != Entity.Null &&
                TryResolveSpawnUnitPrefab(context, prefabEntity, out prefab) &&
                prefab != null)
            {
                return true;
            }
        }

        if (context.RuntimeBuildings != null)
        {
            foreach (KeyValuePair<int, RuntimeBuildingEntity> pair in context.RuntimeBuildings)
            {
                RuntimeBuildingEntity building = pair.Value;
                if (building?.ProducedUnitPrefabs == null)
                    continue;

                if (building.ProducedUnitPrefabs.TryGetValue(unitEntity, out prefab) && prefab != null)
                    return true;
            }
        }

        if (em.HasComponent<UnitSourcePrefabKey>(unitEntity))
        {
            string key = em.GetComponentData<UnitSourcePrefabKey>(unitEntity).Value.ToString();
            if (!string.IsNullOrEmpty(key) &&
                context.DefinitionSystem != null &&
                context.DefinitionSystem.TryResolveConfiguredUnitSpawnPrefab(key, out prefab))
            {
                return true;
            }
        }

        return false;
    }

    private static FixedString64Bytes GetUnitPrefabSourceKey(GameObject unitPrefab)
    {
        string sourceKey = BuildingDefinitionSystem.GetSpawnableLookupKey(unitPrefab);
        return string.IsNullOrWhiteSpace(sourceKey) ? default : new FixedString64Bytes(sourceKey);
    }
}
