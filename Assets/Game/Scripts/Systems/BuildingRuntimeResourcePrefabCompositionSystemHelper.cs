namespace Game.Runtime
{
    internal sealed class BuildingRuntimeResourcePrefabCompositionSystemHelper
    {
        public static BuildingRuntimeResourcePrefabContextCompositionSystemHelper.Source Create(
            BuildingRuntimeResourcePrefabCompositionSystemHelper system,
            BuildingGameplaySourceCompositionSystemHelper source)
        {
            return system != null ? system.Create(source) : CreateSource(source);
        }

        public BuildingRuntimeResourcePrefabContextCompositionSystemHelper.Source Create(
            BuildingGameplaySourceCompositionSystemHelper source)
        {
            return CreateSource(source);
        }

        private static BuildingRuntimeResourcePrefabContextCompositionSystemHelper.Source CreateSource(
            BuildingGameplaySourceCompositionSystemHelper source)
        {
            return BuildingRuntimeResourcePrefabContextCompositionSystemHelper.CreateSource(
                source.BuildingRuntimeResourcePrefabContextCompositionSystemHelper,
                source.RuntimeResourceUtilitySystemHelper,
                source.RuntimeUnitPrefabSystem,
                source.BuildingDefinitionPrefabSystemHelper,
                source.RuntimeBuildingSystem,
                source.BuildingSpawnPrefabSystem,
                source.BuildingEntityManagerAccessSystem.TryGetEntityManager,
                source.BuildingGameplayEcsQueryCompositionSystemHelper.EnsureEntityQueries,
                source.BuildingGameplayEcsQueryCompositionSystemHelper.UnitPrefabRegistryQuery,
                source.BuildingGameplayEcsQueryCompositionSystemHelper.SpawnPrefabCandidatesQuery,
                source.BuildingGameplayEcsQueryCompositionSystemHelper.LivePlayerUnitsQuery,
                createCurrentSource: () => Create(source.BuildingRuntimeResourcePrefabCompositionHelper, source));
        }
    }
}
