using System;
using Unity.Collections;
using Unity.Entities;

internal partial struct RuntimeUnitPrefabSystem : ISystem
{
    public readonly struct Context
    {
        public readonly BuildingSpawnPrefabSystem SpawnPrefabSystem;
        public readonly CitizenPrefabSystem.TryGetEntityManagerDelegate TryGetEntityManager;
        public readonly Action<EntityManager> EnsureEntityQueries;
        public readonly Func<BuildingSpawnPrefabSystem.Context> CreateSpawnPrefabContext;

        public Context(
            BuildingSpawnPrefabSystem spawnPrefabSystem,
            CitizenPrefabSystem.TryGetEntityManagerDelegate tryGetEntityManager,
            Action<EntityManager> ensureEntityQueries,
            Func<BuildingSpawnPrefabSystem.Context> createSpawnPrefabContext)
        {
            SpawnPrefabSystem = spawnPrefabSystem;
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
            !context.TryGetEntityManager(out EntityManager em))
        {
            return false;
        }

        context.EnsureEntityQueries?.Invoke(em);
        return context.SpawnPrefabSystem.TryGetSpawnUnitPrefabEntity(
            context.CreateSpawnPrefabContext != null ? context.CreateSpawnPrefabContext() : default,
            em,
            unitPrefabSourceKey,
            out prefabEntity);
    }

    public bool TryResolveSpawnUnitSourceKey(Context context, Entity prefabEntity, out FixedString64Bytes sourceKey)
    {
        sourceKey = default;
        if (prefabEntity == Entity.Null ||
            context.TryGetEntityManager == null ||
            !context.TryGetEntityManager(out EntityManager em))
        {
            return false;
        }

        context.EnsureEntityQueries?.Invoke(em);
        return context.SpawnPrefabSystem.TryResolveSpawnUnitSourceKey(
            context.CreateSpawnPrefabContext != null ? context.CreateSpawnPrefabContext() : default,
            em,
            prefabEntity,
            out sourceKey);
    }
}
