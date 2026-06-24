using Unity.Entities;

[UpdateInGroup(typeof(SimulationSystemGroup))]
internal partial struct BuildingEntityManagerAccessSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        // RequireForUpdate intentionally omitted: disabled access helper; composition calls TryGetEntityManager directly.
        state.Enabled = false;
    }

    public void OnUpdate(ref SystemState state)
    {
    }

    public bool TryGetEntityManager(out EntityManager entityManager)
    {
        entityManager = default;
        Unity.Entities.World world = Unity.Entities.World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return false;

        entityManager = world.EntityManager;
        return true;
    }
}
