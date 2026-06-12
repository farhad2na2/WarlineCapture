using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
[UpdateAfter(typeof(UnitSurfaceTrackingSystem))]
[UpdateBefore(typeof(UnitMoveVisualStateSystem))]
public partial struct UnitGroundingSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<UnitSurfaceComponent>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var job = new GroundUnitsJob
        {
            GroundOffsetLookup = SystemAPI.GetComponentLookup<UnitGroundOffsetComponent>(true)
        };
        state.Dependency = job.ScheduleParallel(state.Dependency);
    }

    [BurstCompile]
    [WithNone(typeof(UnitAirMovement))]
    private partial struct GroundUnitsJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<UnitGroundOffsetComponent> GroundOffsetLookup;

        public void Execute(
            Entity entity,
            ref LocalTransform transform,
            ref UnitSurfaceComponent unitSurface)
        {
            if (unitSurface.HasSurface == 0)
                return;

            float offset = GroundOffsetLookup.HasComponent(entity)
                ? GroundOffsetLookup[entity].Value
                : 0f;

            float3 position = transform.Position;
            position.y = unitSurface.LastSampledHeight + offset;
            transform.Position = position;

            unitSurface.LastSampledNormal = math.normalizesafe(unitSurface.LastSampledNormal, new float3(0f, 1f, 0f));
            unitSurface.IsGrounded = 1;
        }
    }
}
