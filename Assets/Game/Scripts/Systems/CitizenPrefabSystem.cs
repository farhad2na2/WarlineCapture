using System;
using Unity.Collections;
using Unity.Entities;

[UpdateInGroup(typeof(SimulationSystemGroup))]
internal partial struct CitizenPrefabSystem : ISystem
{
    public delegate bool TryGetEntityManagerDelegate(out EntityManager entityManager);

    public readonly struct Context
    {
        public readonly BuildingSpawnPrefabSystem SpawnPrefabSystem;
        public readonly TryGetEntityManagerDelegate TryGetEntityManager;
        public readonly Action<EntityManager> EnsureEntityQueries;
        public readonly Func<BuildingSpawnPrefabSystem.Context> CreateSpawnPrefabContext;

        public Context(
            BuildingSpawnPrefabSystem spawnPrefabSystem,
            TryGetEntityManagerDelegate tryGetEntityManager,
            Action<EntityManager> ensureEntityQueries,
            Func<BuildingSpawnPrefabSystem.Context> createSpawnPrefabContext)
        {
            SpawnPrefabSystem = spawnPrefabSystem;
            TryGetEntityManager = tryGetEntityManager;
            EnsureEntityQueries = ensureEntityQueries;
            CreateSpawnPrefabContext = createSpawnPrefabContext;
        }
    }

    public void OnCreate(ref SystemState state)
    {
        state.Enabled = false;
    }

    public void OnUpdate(ref SystemState state)
    {
    }

    public bool TryResolveConfiguredUnitPrefabEntity(Context context, string unitPrefabSourceKey, out Entity prefabEntity)
    {
        string sourceKey = BuildingDefinitionPrefabSystemHelper.GetSpawnableLookupKey(unitPrefabSourceKey);
        return TryResolveConfiguredUnitPrefabEntity(
            context,
            string.IsNullOrWhiteSpace(sourceKey) ? default : new FixedString64Bytes(sourceKey),
            out prefabEntity);
    }

    public bool TryResolveConfiguredUnitPrefabEntity(Context context, FixedString64Bytes unitPrefabSourceKey, out Entity prefabEntity)
    {
        prefabEntity = Entity.Null;
        if (unitPrefabSourceKey.Length == 0 ||
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
            unitPrefabSourceKey,
            out prefabEntity);
    }
}
