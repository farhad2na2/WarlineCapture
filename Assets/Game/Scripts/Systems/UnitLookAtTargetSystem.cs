using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
[UpdateAfter(typeof(UnitGridMovementSystem))]
public partial struct UnitLookAtTargetSystem : ISystem
{
    private EntityQuery _gridQuery;

    public void OnCreate(ref SystemState state)
    {
        _gridQuery = state.GetEntityQuery(ComponentType.ReadOnly<GridConfig>());
        state.RequireForUpdate(_gridQuery);
        state.RequireForUpdate<UnitTarget>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        Entity gridEntity = _gridQuery.GetSingletonEntity();
        GridConfig grid = state.EntityManager.GetComponentData<GridConfig>(gridEntity);
        new LookAtTargetJob
        {
            Grid = grid
        }.ScheduleParallel();
    }

    [BurstCompile]
    [WithNone(typeof(EngageTarget), typeof(UnitRotationHold), typeof(UnitAirMovement))]
    private partial struct LookAtTargetJob : IJobEntity
    {
        public GridConfig Grid;

        public void Execute(ref LocalTransform transform, in UnitTarget target, in UnitFootprint footprint, in UnitMovementBehavior movementBehavior)
        {
            if (UnitVehicleMovementUtility.IsVehicle(footprint, movementBehavior))
                return;

            float3 targetPos = GridUtils.CellToWorldCenter(Grid, target.Cell);
            float3 dir = targetPos - transform.Position;
            dir.y = 0f;
            if (math.lengthsq(dir) < 1e-8f)
                return;
            transform.Rotation = quaternion.LookRotationSafe(math.normalizesafe(dir), math.up());
        }
    }
}
