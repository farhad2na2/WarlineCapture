using System;
using Unity.Entities;

internal sealed class RuntimeUnitPrefabSystem
{
    public readonly struct Context
    {
        public readonly BuildingDefinitionSystem DefinitionSystem;
        public readonly BuildingSpawnPrefabSystem SpawnPrefabSystem;
        public readonly CitizenPrefabSystem.TryGetEntityManagerDelegate TryGetEntityManager;
        public readonly Action<EntityManager> EnsureEntityQueries;
        public readonly Func<BuildingSpawnPrefabSystem.Context> CreateSpawnPrefabContext;

        public Context(
            BuildingDefinitionSystem definitionSystem,
            BuildingSpawnPrefabSystem spawnPrefabSystem,
            CitizenPrefabSystem.TryGetEntityManagerDelegate tryGetEntityManager,
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

    public CitizenPrefabSystem.Context CreateCitizenPrefabContext(Context context)
    {
        return new CitizenPrefabSystem.Context(
            context.DefinitionSystem,
            context.SpawnPrefabSystem,
            context.TryGetEntityManager,
            context.EnsureEntityQueries,
            context.CreateSpawnPrefabContext);
    }
}
