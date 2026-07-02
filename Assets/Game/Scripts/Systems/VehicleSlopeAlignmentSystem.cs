using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Game.Components;

namespace Game.Runtime
{
    [BurstCompile]
    [UpdateAfter(typeof(UnitGridMovementSystem))]
    [UpdateAfter(typeof(UnitEngagedMovementSystem))]
    [UpdateAfter(typeof(UnitGroundingSystem))]
    [UpdateAfter(typeof(UnitAirMovementSystem))]
    public partial struct VehicleSlopeAlignmentSystem : ISystem
    {
        private const float MaxPitchRollDegrees = 20f;
        private const float AlignmentSharpness = 10f;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<VehicleSurfaceAlignmentComponent>();
            state.RequireForUpdate<UnitSurfaceComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new AlignJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime
            }.ScheduleParallel();
        }

        [BurstCompile]
        [WithNone(typeof(UnitAirMovement), typeof(StaticGridBlocker), typeof(UnitDeathAnimationComponent))]
        private partial struct AlignJob : IJobEntity
        {
            public float DeltaTime;

            public void Execute(
                ref LocalTransform transform,
                ref VehicleSurfaceAlignmentComponent alignment,
                in UnitSurfaceComponent surface,
                in UnitFootprint footprint,
                in UnitMovementBehavior movementBehavior)
            {
                if (!UnitVehicleMovementUtility.IsVehicle(footprint, movementBehavior))
                    return;

                float3 normal = surface.HasSurface != 0
                    ? math.normalizesafe(surface.LastSampledNormal, math.up())
                    : math.up();
                normal = ClampNormalSlope(normal, MaxPitchRollDegrees);

                float3 flatForward = UnitVehicleMovementUtility.Forward(transform.Rotation);
                float3 surfaceForward = flatForward - normal * math.dot(flatForward, normal);
                if (math.lengthsq(surfaceForward) < 1e-8f)
                    surfaceForward = math.cross(normal, math.right());
                surfaceForward = math.normalizesafe(surfaceForward, new float3(0f, 0f, 1f));

                quaternion targetRotation = quaternion.LookRotationSafe(surfaceForward, normal);
                float weight = 1f - math.exp(-AlignmentSharpness * math.max(0f, DeltaTime));
                transform.Rotation = math.slerp(transform.Rotation, targetRotation, math.saturate(weight));

                alignment.SurfaceNormal = normal;
                alignment.PitchDegrees = math.clamp(math.degrees(math.atan2(normal.z, normal.y)), -MaxPitchRollDegrees, MaxPitchRollDegrees);
                alignment.RollDegrees = math.clamp(math.degrees(-math.atan2(normal.x, normal.y)), -MaxPitchRollDegrees, MaxPitchRollDegrees);
                alignment.AlignmentWeight = math.saturate(weight);
            }

            private static float3 ClampNormalSlope(float3 normal, float maxDegrees)
            {
                float3 up = math.up();
                float dot = math.clamp(math.dot(up, normal), -1f, 1f);
                float angle = math.acos(dot);
                float maxAngle = math.radians(math.max(0f, maxDegrees));
                if (angle <= maxAngle)
                    return normal;

                float3 axis = math.cross(up, normal);
                if (math.lengthsq(axis) < 1e-8f)
                    return up;

                axis = math.normalize(axis);
                return math.normalizesafe(math.rotate(quaternion.AxisAngle(axis, maxAngle), up), up);
            }
        }
    }
}
