using System;
using Unity.Entities;

internal sealed partial class BuildingRuntimeResourcePrefabContextSystem : SystemBase
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
        public readonly BuildingSpawnPrefabSystem.ResolveSpawnableLookupKeyDelegate ResolveSpawnableLookupKey;
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
            BuildingSpawnPrefabSystem.ResolveSpawnableLookupKeyDelegate resolveSpawnableLookupKey = null,
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
            ResolveSpawnableLookupKey = resolveSpawnableLookupKey;
            CreateCurrentSource = createCurrentSource;
        }
    }

    protected override void OnCreate()
    {
        Enabled = false;
    }

    protected override void OnUpdate()
    {
    }

    public static Source CreateSource(
        BuildingRuntimeResourcePrefabContextSystem system,
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
        BuildingSpawnPrefabSystem.ResolveSpawnableLookupKeyDelegate resolveSpawnableLookupKey = null,
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
                resolveSpawnableLookupKey,
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
                resolveSpawnableLookupKey,
                createCurrentSource);
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
        BuildingSpawnPrefabSystem.ResolveSpawnableLookupKeyDelegate resolveSpawnableLookupKey = null,
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
            resolveSpawnableLookupKey,
            createCurrentSource);
    }

    private static Source CreateSourceState(
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
        BuildingSpawnPrefabSystem.ResolveSpawnableLookupKeyDelegate resolveSpawnableLookupKey = null,
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
            resolveSpawnableLookupKey,
            createCurrentSource);
    }

    public static CitizenResourceSystem.Context CreateCitizenResourceContext(
        BuildingRuntimeResourcePrefabContextSystem system,
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
        BuildingRuntimeResourcePrefabContextSystem system,
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
            source.DefinitionSystem,
            source.SpawnPrefabSystem,
            source.RuntimeBuildingSystem != null ? source.RuntimeBuildingSystem.Buildings : null,
            source.TryGetEntityManager,
            source.EnsureEntityQueries,
            () => CreateBuildingSpawnPrefabContextState(
                source.CreateCurrentSource != null ? source.CreateCurrentSource() : source));
    }

    public static CitizenPrefabSystem.Context CreateCitizenPrefabContext(
        BuildingRuntimeResourcePrefabContextSystem system,
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
        return source.RuntimeUnitPrefabSystem != null
            ? source.RuntimeUnitPrefabSystem.CreateCitizenPrefabContext(runtimeUnitPrefabContext)
            : new CitizenPrefabSystem.Context(
                runtimeUnitPrefabContext.DefinitionSystem,
                runtimeUnitPrefabContext.SpawnPrefabSystem,
                runtimeUnitPrefabContext.TryGetEntityManager,
                runtimeUnitPrefabContext.EnsureEntityQueries,
                runtimeUnitPrefabContext.CreateSpawnPrefabContext);
    }

    public static BuildingSpawnPrefabSystem.Context CreateBuildingSpawnPrefabContext(
        BuildingRuntimeResourcePrefabContextSystem system,
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
            source.DefinitionSystem.ConfiguredUnitSpawnPrefabs,
            source.UnitPrefabRegistryQuery,
            source.SpawnPrefabCandidatesQuery,
            source.LivePlayerUnitsQuery,
            source.ResolveSpawnableLookupKey);
    }
}
