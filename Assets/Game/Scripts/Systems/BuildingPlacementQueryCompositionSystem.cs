using Unity.Entities;

internal sealed class BuildingPlacementQueryCompositionSystem
{
    public BuildingPlacementQuerySystem.Context Create(BuildingGameplayCompositionSourceSystem source)
    {
        return source.BuildingPlacementQuerySystem.CreateContext(new BuildingPlacementQuerySystem.Source(
            source.RuntimeBuildingSystem.Buildings,
            () => source.RuntimeBuildingSystem.CurrentActiveBuildingId,
            BuildingDefinitionSystem.GetProductionCount,
            BuildingDefinitionSystem.GetProductionPrefab,
            TryGetEntityManager));
    }

    private static bool TryGetEntityManager(out EntityManager entityManager)
    {
        entityManager = default;
        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return false;

        entityManager = world.EntityManager;
        return true;
    }
}
