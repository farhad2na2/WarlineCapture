using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

public partial struct UnitGridSnapSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<GridConfig>();
        state.RequireForUpdate<UnitGrid>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var grid = SystemAPI.GetSingleton<GridConfig>();
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (unitGrid, transform, entity) in
                 SystemAPI.Query<RefRO<UnitGrid>, RefRW<LocalTransform>>()
                     .WithNone<UnitGridInitialized, UnitAirMovement>()
                     .WithEntityAccess())
        {
            transform.ValueRW.Position = GridUtils.CellToWorldCenter(grid, unitGrid.ValueRO.Cell);
            ecb.AddComponent<UnitGridInitialized>(entity);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
