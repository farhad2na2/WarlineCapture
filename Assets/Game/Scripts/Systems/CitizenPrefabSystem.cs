using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

internal sealed class CitizenPrefabSystem
{
    public delegate bool TryGetEntityManagerDelegate(out EntityManager entityManager);

    public readonly struct Context
    {
        public readonly BuildingDefinitionSystem DefinitionSystem;
        public readonly BuildingSpawnPrefabSystem SpawnPrefabSystem;
        public readonly TryGetEntityManagerDelegate TryGetEntityManager;
        public readonly Action<EntityManager> EnsureEntityQueries;
        public readonly Func<BuildingSpawnPrefabSystem.Context> CreateSpawnPrefabContext;

        public Context(
            BuildingDefinitionSystem definitionSystem,
            BuildingSpawnPrefabSystem spawnPrefabSystem,
            TryGetEntityManagerDelegate tryGetEntityManager,
            Action<EntityManager> ensureEntityQueries,
            Func<BuildingSpawnPrefabSystem.Context> createSpawnPrefabContext)
        {
            DefinitionSystem = definitionSystem;
            SpawnPrefabSystem = spawnPrefabSystem;
            TryGetEntityManager = tryGetEntityManager;
            EnsureEntityQueries = ensureEntityQueries;
            CreateSpawnPrefabContext = createSpawnPrefabContext;
        }
    }

    public void LoadConfiguredUnitSpawnPrefabs(Context context, IReadOnlyList<string> unitNames, List<GameObject> results)
    {
        if (results == null)
            return;

        results.Clear();
        if (context.DefinitionSystem == null || unitNames == null)
            return;

        for (int i = 0; i < unitNames.Count; i++)
        {
            if (!context.DefinitionSystem.TryResolveConfiguredUnitSpawnPrefab(unitNames[i], out GameObject prefab))
                continue;
            if (prefab != null)
                results.Add(prefab);
        }
    }

    public bool TryResolveConfiguredUnitPrefabEntity(Context context, GameObject unitPrefab, out Entity prefabEntity)
    {
        prefabEntity = Entity.Null;
        if (unitPrefab == null ||
            context.SpawnPrefabSystem == null ||
            context.TryGetEntityManager == null ||
            context.CreateSpawnPrefabContext == null ||
            !context.TryGetEntityManager(out EntityManager em))
        {
            return false;
        }

        context.EnsureEntityQueries?.Invoke(em);
        return context.SpawnPrefabSystem.TryGetSpawnUnitPrefabEntity(
            context.CreateSpawnPrefabContext(),
            em,
            unitPrefab,
            out prefabEntity);
    }
}
