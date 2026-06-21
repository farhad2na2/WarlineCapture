internal sealed class BuildingRuntimeResourcePrefabCompositionSystem
{
    public static BuildingRuntimeResourcePrefabContextSystem.Source Create(
        BuildingRuntimeResourcePrefabCompositionSystem system,
        BuildingGameplayCompositionSourceSystem source)
    {
        return system != null ? system.Create(source) : CreateSource(source);
    }

    public BuildingRuntimeResourcePrefabContextSystem.Source Create(
        BuildingGameplayCompositionSourceSystem source)
    {
        return CreateSource(source);
    }

    private static BuildingRuntimeResourcePrefabContextSystem.Source CreateSource(
        BuildingGameplayCompositionSourceSystem source)
    {
        return BuildingRuntimeResourcePrefabContextSystem.CreateSource(
            source.BuildingRuntimeResourcePrefabContextSystem,
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
            createCurrentSource: () => Create(source.BuildingRuntimeResourcePrefabCompositionSystem, source));
    }
}
