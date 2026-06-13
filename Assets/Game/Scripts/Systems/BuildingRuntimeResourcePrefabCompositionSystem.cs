internal sealed class BuildingRuntimeResourcePrefabCompositionSystem
{
    public BuildingRuntimeResourcePrefabContextSystem.Source Create(
        BuildingGameplayCompositionSourceSystem source)
    {
        return source.BuildingRuntimeResourcePrefabContextSystem.CreateSource(
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
            resolveSpawnableLookupKey: source.ResolveSpawnableLookupKey,
            createCurrentSource: () => Create(source));
    }
}
