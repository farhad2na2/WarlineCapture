using Unity.Entities;

internal partial struct BuildingPlacementQueryCompositionSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.Enabled = false;
    }

    public void OnUpdate(ref SystemState state)
    {
    }

    public BuildingPlacementQueryUiSystemHelper.Context Create(BuildingGameplaySourceCompositionSystemHelper source)
    {
        return source.BuildingPlacementQueryUiSystemHelper.CreateContext(new BuildingPlacementQueryUiSystemHelper.Source(
            source.RuntimeBuildingSystem.Buildings,
            () => source.RuntimeBuildingSystem.CurrentActiveBuildingId,
            BuildingDefinitionSystem.GetProductionCount,
            BuildingDefinitionSystem.GetProductionPrefab,
            source.BuildingEntityManagerAccessSystem.TryGetEntityManager));
    }
}
