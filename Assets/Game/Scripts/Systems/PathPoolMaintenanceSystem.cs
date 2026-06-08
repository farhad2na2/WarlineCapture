using Unity.Entities;

[UpdateAfter(typeof(UnitGridMovementSystem))]
public partial struct PathPoolMaintenanceSystem : ISystem
{
    private EntityQuery _activePaths;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<GridConfig>();
        state.RequireForUpdate<PathPoolComponent>();
        _activePaths = state.GetEntityQuery(ComponentType.ReadOnly<UnitPathRange>());
    }

    public void OnUpdate(ref SystemState state)
    {
        if (_activePaths.CalculateEntityCount() != 0)
            return;

        var gridEntity = SystemAPI.GetSingletonEntity<GridConfig>();
        var pool = state.EntityManager.GetComponentData<PathPoolComponent>(gridEntity);
        if (!pool.Cells.IsCreated || pool.Cells.Length == 0)
            return;

        pool.Cells.Clear();
        state.EntityManager.SetComponentData(gridEntity, pool);
    }
}

