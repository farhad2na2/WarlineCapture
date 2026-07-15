using System;
using Unity.Entities;

namespace Game.Runtime
{
    internal sealed class BuildingRuntimeResourcePrefabContextCompositionSystemHelper
    {
        public readonly struct Source
        {
            public readonly RuntimeFactionResourceSystemHelper RuntimeFactionResourceSystemHelper;
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
                RuntimeFactionResourceSystemHelper factionResourceSystem,
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
                RuntimeFactionResourceSystemHelper = factionResourceSystem;
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
            RuntimeFactionResourceSystemHelper factionResourceSystem,
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
                    factionResourceSystem,
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
                    factionResourceSystem,
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
            RuntimeFactionResourceSystemHelper factionResourceSystem,
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
                factionResourceSystem,
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
            RuntimeFactionResourceSystemHelper factionResourceSystem,
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
                factionResourceSystem,
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

        public static CitizenResourceCompositionSystemHelper.Context CreateCitizenResourceContext(
            BuildingRuntimeResourcePrefabContextCompositionSystemHelper system,
            Source source)
        {
            return system != null
                ? system.CreateCitizenResourceContext(source)
                : CreateCitizenResourceContextState(source);
        }

        public CitizenResourceCompositionSystemHelper.Context CreateCitizenResourceContext(Source source)
        {
            return CreateCitizenResourceContextState(source);
        }

        private static CitizenResourceCompositionSystemHelper.Context CreateCitizenResourceContextState(Source source)
        {
            return source.RuntimeFactionResourceSystemHelper.CreateCitizenResourceContext();
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
}
