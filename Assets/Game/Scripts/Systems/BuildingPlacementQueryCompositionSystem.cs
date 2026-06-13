internal sealed class BuildingPlacementQueryCompositionSystem
{
    public BuildingPlacementQuerySystem.Context Create(BuildingGameplayCompositionSourceSystem source)
    {
        return source.BuildingPlacementQuerySystem.CreateContext(new BuildingPlacementQuerySystem.Source(
            source.RuntimeBuildingSystem.Buildings,
            () => source.RuntimeBuildingSystem.CurrentActiveBuildingId,
            BuildingDefinitionSystem.GetProductionCount,
            BuildingDefinitionSystem.GetProductionPrefab,
            source.BuildingEntityManagerAccessSystem.TryGetEntityManager));
    }
}
