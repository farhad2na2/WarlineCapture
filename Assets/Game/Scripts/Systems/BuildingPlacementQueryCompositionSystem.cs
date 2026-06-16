using Unity.Entities;

internal sealed partial class BuildingPlacementQueryCompositionSystem : SystemBase
{
    protected override void OnCreate()
    {
        Enabled = false;
    }

    protected override void OnUpdate()
    {
    }

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
