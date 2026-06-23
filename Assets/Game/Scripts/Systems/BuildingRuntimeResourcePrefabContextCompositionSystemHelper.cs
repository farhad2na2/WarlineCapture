using System;
using Unity.Entities;

internal sealed class BuildingRuntimeResourcePrefabContextCompositionSystemHelper
{
    public readonly struct Source
    {
        public readonly RuntimeResourceSystem RuntimeResourceSystem;
        public readonly RuntimeUnitPrefabSystem RuntimeUnitPrefabSystem;
        public readonly BuildingDefinitionPrefabSystemHelper DefinitionSystem;
        public readonly RuntimeBuildingCollection<RuntimeBuildingEntity> RuntimeBuildingSystem;
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
            BuildingDefinitionPrefabSystemHelper definitionSystem,
            RuntimeBuildingCollection<RuntimeBuildingEntity> runtimeBuildingSystem,
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

    public static Source CreateSource(
        BuildingRuntimeResourcePrefabContextCompositionSystemHelper system,
        RuntimeResourceSystem runtimeResourceSystem,
        RuntimeUnitPrefabSystem runtimeUnitPrefabSystem,
        BuildingDefinitionPrefabSystemHelper definitionSystem,
        RuntimeBuildingCollection<RuntimeBuildingEntity> runtimeBuildingSystem,
        BuildingSpawnPrefabSystem spawnPrefabSystem,
        CitizenPrefabSystem.TryGetEntityManagerDelegate tryGetEntityManager,
        Action<EntityManager> ensureEntityQueries,
        EntityQuery unitPrefabRegistryQuery,
        EntityQuery spawnPrefabCandidatesQuery,
        EntityQuery livePlayerUnitsQuery,
        Func<Source> createCurrentSource = null)
    {
        return system != null
            ? system.CreateSource(
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
                createCurrentSource)
            : CreateSourceState(
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

    public Source CreateSource(
        RuntimeResourceSystem runtimeResourceSystem,
        RuntimeUnitPrefabSystem runtimeUnitPrefabSystem,
        BuildingDefinitionPrefabSystemHelper definitionSystem,
        RuntimeBuildingCollection<RuntimeBuildingEntity> runtimeBuildingSystem,
        BuildingSpawnPrefabSystem spawnPrefabSystem,
        CitizenPrefabSystem.TryGetEntityManagerDelegate tryGetEntityManager,
        Action<EntityManager> ensureEntityQueries,
        EntityQuery unitPrefabRegistryQuery,
        EntityQuery spawnPrefabCandidatesQuery,
        EntityQuery livePlayerUnitsQuery,
        Func<Source> createCurrentSource = null)
    {
        return CreateSourceState(
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

    private static Source CreateSourceState(
        RuntimeResourceSystem runtimeResourceSystem,
        RuntimeUnitPrefabSystem runtimeUnitPrefabSystem,
        BuildingDefinitionPrefabSystemHelper definitionSystem,
        RuntimeBuildingCollection<RuntimeBuildingEntity> runtimeBuildingSystem,
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

    public static CitizenResourceSystem.Context CreateCitizenResourceContext(
        BuildingRuntimeResourcePrefabContextCompositionSystemHelper system,
        Source source)
    {
        return system != null
            ? system.CreateCitizenResourceContext(source)
            : CreateCitizenResourceContextState(source);
    }

    public CitizenResourceSystem.Context CreateCitizenResourceContext(Source source)
    {
        return CreateCitizenResourceContextState(source);
    }

    private static CitizenResourceSystem.Context CreateCitizenResourceContextState(Source source)
    {
        return source.RuntimeResourceSystem.CreateCitizenResourceContext();
    }

    public static RuntimeUnitPrefabSystem.Context CreateRuntimeUnitPrefabContext(
        BuildingRuntimeResourcePrefabContextCompositionSystemHelper system,
        Source source)
    {
        return system != null
            ? system.CreateRuntimeUnitPrefabContext(source)
            : CreateRuntimeUnitPrefabContextState(source);
    }

    public RuntimeUnitPrefabSystem.Context CreateRuntimeUnitPrefabContext(Source source)
    {
        return CreateRuntimeUnitPrefabContextState(source);
    }

    private static RuntimeUnitPrefabSystem.Context CreateRuntimeUnitPrefabContextState(Source source)
    {
        return new RuntimeUnitPrefabSystem.Context(
            source.SpawnPrefabSystem,
            source.TryGetEntityManager,
            source.EnsureEntityQueries,
            () => CreateBuildingSpawnPrefabContextState(
                source.CreateCurrentSource != null ? source.CreateCurrentSource() : source));
    }

    public static CitizenPrefabSystem.Context CreateCitizenPrefabContext(
        BuildingRuntimeResourcePrefabContextCompositionSystemHelper system,
        Source source)
    {
        return system != null
            ? system.CreateCitizenPrefabContext(source)
            : CreateCitizenPrefabContextState(source);
    }

    public CitizenPrefabSystem.Context CreateCitizenPrefabContext(Source source)
    {
        return CreateCitizenPrefabContextState(source);
    }

    private static CitizenPrefabSystem.Context CreateCitizenPrefabContextState(Source source)
    {
        RuntimeUnitPrefabSystem.Context runtimeUnitPrefabContext = CreateRuntimeUnitPrefabContextState(source);
        return source.RuntimeUnitPrefabSystem.CreateCitizenPrefabContext(runtimeUnitPrefabContext);
    }

    public static BuildingSpawnPrefabSystem.Context CreateBuildingSpawnPrefabContext(
        BuildingRuntimeResourcePrefabContextCompositionSystemHelper system,
        Source source)
    {
        return system != null
            ? system.CreateBuildingSpawnPrefabContext(source)
            : CreateBuildingSpawnPrefabContextState(source);
    }

    public BuildingSpawnPrefabSystem.Context CreateBuildingSpawnPrefabContext(Source source)
    {
        return CreateBuildingSpawnPrefabContextState(source);
    }

    private static BuildingSpawnPrefabSystem.Context CreateBuildingSpawnPrefabContextState(Source source)
    {
        return new BuildingSpawnPrefabSystem.Context(
            source.UnitPrefabRegistryQuery,
            source.SpawnPrefabCandidatesQuery,
            source.LivePlayerUnitsQuery);
    }
}
