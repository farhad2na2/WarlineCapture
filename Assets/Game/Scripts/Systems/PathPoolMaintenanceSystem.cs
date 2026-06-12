using Unity.Burst;
using Unity.Entities;

[BurstCompile]
[UpdateAfter(typeof(UnitGridMovementSystem))]
public partial struct PathPoolMaintenanceSystem : ISystem
{
    private EntityQuery _activePaths;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<GridConfig>();
        state.RequireForUpdate<PathPoolComponent>();
        _activePaths = state.GetEntityQuery(ComponentType.ReadOnly<UnitPathRange>());
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (_activePaths.CalculateEntityCount() != 0)
            return;

        RefRW<PathPoolComponent> pool = SystemAPI.GetSingletonRW<PathPoolComponent>();
        if (!pool.ValueRO.Cells.IsCreated || pool.ValueRO.Cells.Length == 0)
            return;

        pool.ValueRW.Cells.Clear();
    }
}
