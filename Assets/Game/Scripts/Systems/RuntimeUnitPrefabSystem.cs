using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

internal sealed class RuntimeUnitPrefabSystem
{
    public readonly struct Context
    {
        public readonly BuildingDefinitionSystem DefinitionSystem;
        public readonly BuildingSpawnPrefabSystem SpawnPrefabSystem;
        public readonly IReadOnlyDictionary<int, RuntimeBuildingData> RuntimeBuildings;
        public readonly CitizenPrefabSystem.TryGetEntityManagerDelegate TryGetEntityManager;
        public readonly Action<EntityManager> EnsureEntityQueries;
        public readonly Func<BuildingSpawnPrefabSystem.Context> CreateSpawnPrefabContext;

        public Context(
            BuildingDefinitionSystem definitionSystem,
            BuildingSpawnPrefabSystem spawnPrefabSystem,
            IReadOnlyDictionary<int, RuntimeBuildingData> runtimeBuildings,
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
            context.DefinitionSystem,
            context.SpawnPrefabSystem,
            context.TryGetEntityManager,
            context.EnsureEntityQueries,
            context.CreateSpawnPrefabContext);
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
        return context.SpawnPrefabSystem != null &&
               context.SpawnPrefabSystem.TryGetSpawnUnitPrefabEntity(
                   context.CreateSpawnPrefabContext != null ? context.CreateSpawnPrefabContext() : default,
                   em,
                   unitPrefab,
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
        return context.SpawnPrefabSystem != null &&
               context.SpawnPrefabSystem.TryResolveSpawnUnitPrefabFromRegistry(
                   context.CreateSpawnPrefabContext != null ? context.CreateSpawnPrefabContext() : default,
                   em,
                   prefabEntity,
                   out spawnUnitPrefab);
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
            foreach (KeyValuePair<int, RuntimeBuildingData> pair in context.RuntimeBuildings)
            {
                RuntimeBuildingData building = pair.Value;
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
}
