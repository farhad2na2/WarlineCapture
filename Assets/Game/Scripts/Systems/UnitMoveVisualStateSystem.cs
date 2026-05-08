using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
[UpdateAfter(typeof(UnitGridMovementSystem))]
[UpdateAfter(typeof(UnitEngagedMovementSystem))]
[UpdateAfter(typeof(UnitAirMovementSystem))]
public partial struct UnitMoveVisualStateSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<UnitPrevWorldPos>();
        state.RequireForUpdate<UnitMoveVisualState>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float epsSq = 0.00025f * 0.00025f;
        float stopHoldSeconds = 0.15f;
        float dt = SystemAPI.Time.DeltaTime;
        var handle = new UpdateJob
        {
            DeltaTime = dt,
            EpsilonSq = epsSq,
            StopHoldSeconds = stopHoldSeconds
        }.ScheduleParallel(state.Dependency);

        state.Dependency = handle;
    }

    [BurstCompile]
    [WithNone(typeof(StaticGridBlocker))]
    private partial struct UpdateJob : IJobEntity
    {
        public float DeltaTime;
        public float EpsilonSq;
        public float StopHoldSeconds;

        public void Execute(ref UnitPrevWorldPos prev, ref UnitMoveVisualState vis, in LocalTransform transform)
        {
            float3 cur = transform.Position;
            float3 delta = cur - prev.Value;
            delta.y = 0f;
            bool moved = math.lengthsq(delta) > EpsilonSq;

            if (moved)
            {
                vis.IsMoving = 1;
                vis.StillSeconds = 0f;
            }
            else
            {
                vis.StillSeconds += DeltaTime;
                if (vis.IsMoving != 0 && vis.StillSeconds < StopHoldSeconds)
                {
                    // Keep "walk" on brief stalls (path cell snapping / occupancy waits).
                }
                else
                {
                    vis.IsMoving = 0;
                }
            }
            prev.Value = cur;
        }
    }
}
