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
            source.RuntimeResourceSystem,
            source.RuntimeUnitPrefabSystem,
            source.BuildingDefinitionSystem,
            source.RuntimeBuildingSystem,
            source.BuildingSpawnPrefabSystem,
            source.BuildingEntityManagerAccessSystem.TryGetEntityManager,
            source.BuildingGameplayEcsQuerySystem.EnsureEntityQueries,
            source.BuildingGameplayEcsQuerySystem.UnitPrefabRegistryQuery,
            source.BuildingGameplayEcsQuerySystem.SpawnPrefabCandidatesQuery,
            source.BuildingGameplayEcsQuerySystem.LivePlayerUnitsQuery,
            createCurrentSource: () => Create(source.BuildingRuntimeResourcePrefabCompositionHelper, source));
    }
}
