using System;
using Unity.Entities;

internal sealed class BuildingRuntimeResourcePrefabContextSystem
{
    public readonly struct Source
    {
        public readonly RuntimeResourceSystem RuntimeResourceSystem;
        public readonly RuntimeUnitPrefabSystem RuntimeUnitPrefabSystem;
        public readonly BuildingDefinitionSystem DefinitionSystem;
        public readonly RuntimeBuildingSystem<RuntimeBuildingEntity> RuntimeBuildingSystem;
        public readonly BuildingSpawnPrefabSystem SpawnPrefabSystem;
        public readonly CitizenPrefabSystem.TryGetEntityManagerDelegate TryGetEntityManager;
        public readonly Action<EntityManager> EnsureEntityQueries;
        public readonly EntityQuery UnitPrefabRegistryQuery;
        public readonly EntityQuery SpawnPrefabCandidatesQuery;
        public readonly EntityQuery LivePlayerUnitsQuery;
        public readonly Func<Source> CreateCurrentSource;

        public Source(
            RuntimeResourceSystem runtimeResourceSystem,
            RuntimeUnitPrefabSystem runtimeUnitPrefabSystem,
            BuildingDefinitionSystem definitionSystem,
            RuntimeBuildingSystem<RuntimeBuildingEntity> runtimeBuildingSystem,
            BuildingSpawnPrefabSystem spawnPrefabSystem,
            CitizenPrefabSystem.TryGetEntityManagerDelegate tryGetEntityManager,
            Action<EntityManager> ensureEntityQueries,
            EntityQuery unitPrefabRegistryQuery,
            EntityQuery spawnPrefabCandidatesQuery,
            EntityQuery livePlayerUnitsQuery,
            Func<Source> createCurrentSource = null)
        {
            RuntimeResourceSystem = runtimeResourceSystem;
            RuntimeUnitPrefabSystem = runtimeUnitPrefabSystem;
            DefinitionSystem = definitionSystem;
            RuntimeBuildingSystem = runtimeBuildingSystem;
            SpawnPrefabSystem = spawnPrefabSystem;
            TryGetEntityManager = tryGetEntityManager;
            EnsureEntityQueries = ensureEntityQueries;
            UnitPrefabRegistryQuery = unitPrefabRegistryQuery;
            SpawnPrefabCandidatesQuery = spawnPrefabCandidatesQuery;
            LivePlayerUnitsQuery = livePlayerUnitsQuery;
            CreateCurrentSource = createCurrentSource;
        }
    }

    public Source CreateSource(
        RuntimeResourceSystem runtimeResourceSystem,
        RuntimeUnitPrefabSystem runtimeUnitPrefabSystem,
        BuildingDefinitionSystem definitionSystem,
        RuntimeBuildingSystem<RuntimeBuildingEntity> runtimeBuildingSystem,
        BuildingSpawnPrefabSystem spawnPrefabSystem,
        CitizenPrefabSystem.TryGetEntityManagerDelegate tryGetEntityManager,
        Action<EntityManager> ensureEntityQueries,
        EntityQuery unitPrefabRegistryQuery,
        EntityQuery spawnPrefabCandidatesQuery,
        EntityQuery livePlayerUnitsQuery,
        Func<Source> createCurrentSource = null)
    {
        return new Source(
            runtimeResourceSystem,
            runtimeUnitPrefabSystem,
            definitionSystem,
            runtimeBuildingSystem,
            spawnPrefabSystem,
            tryGetEntityManager,
            ensureEntityQueries,
            unitPrefabRegistryQuery,
            spawnPrefabCandidatesQuery,
            livePlayerUnitsQuery,
            createCurrentSource);
    }

    public CitizenResourceSystem.Context CreateCitizenResourceContext(Source source)
    {
        return source.RuntimeResourceSystem.CreateCitizenResourceContext();
    }

    public RuntimeUnitPrefabSystem.Context CreateRuntimeUnitPrefabContext(Source source)
    {
        return new RuntimeUnitPrefabSystem.Context(
            source.DefinitionSystem,
            source.SpawnPrefabSystem,
            source.RuntimeBuildingSystem != null ? source.RuntimeBuildingSystem.Buildings : null,
            source.TryGetEntityManager,
            source.EnsureEntityQueries,
            () => CreateBuildingSpawnPrefabContext(
                source.CreateCurrentSource != null ? source.CreateCurrentSource() : source));
    }

    public CitizenPrefabSystem.Context CreateCitizenPrefabContext(Source source)
    {
        return source.RuntimeUnitPrefabSystem.CreateCitizenPrefabContext(CreateRuntimeUnitPrefabContext(source));
    }

    public BuildingSpawnPrefabSystem.Context CreateBuildingSpawnPrefabContext(Source source)
    {
        return new BuildingSpawnPrefabSystem.Context(
            source.DefinitionSystem.ConfiguredUnitSpawnPrefabs,
            source.UnitPrefabRegistryQuery,
            source.SpawnPrefabCandidatesQuery,
            source.LivePlayerUnitsQuery);
    }
}
