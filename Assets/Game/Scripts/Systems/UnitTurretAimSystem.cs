using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Game.Components;

namespace Game.Runtime
{
    [BurstCompile]
    [UpdateAfter(typeof(EngageTargetSyncSystem))]
    [UpdateBefore(typeof(UnitEngagedMovementSystem))]
    public partial struct UnitTurretAimSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<UnitTurretReference>();
            state.RequireForUpdate<LocalToWorld>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency.Complete();

            var localTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(false);
            var localToWorldLookup = SystemAPI.GetComponentLookup<LocalToWorld>(true);
            var parentLookup = SystemAPI.GetComponentLookup<Parent>(true);
            var engageLookup = SystemAPI.GetComponentLookup<EngageTarget>(true);

            foreach (var (turretRef, entity) in SystemAPI.Query<RefRO<UnitTurretReference>>().WithEntityAccess())
            {
                if (!engageLookup.HasComponent(entity))
                    continue;

                EngageTarget engage = engageLookup[entity];
                if (engage.Target == Entity.Null ||
                    !localTransformLookup.HasComponent(turretRef.ValueRO.Turret) ||
                    !localToWorldLookup.HasComponent(turretRef.ValueRO.Turret))
                {
                    continue;
                }

                float3 turretWorldPos = localToWorldLookup[turretRef.ValueRO.Turret].Position;
                float3 desiredWorldDir = engage.Position - turretWorldPos;
                desiredWorldDir.y = 0f;
                if (math.lengthsq(desiredWorldDir) < 1e-8f)
                    continue;

                quaternion localRotation;
                if (parentLookup.HasComponent(turretRef.ValueRO.Turret) &&
                    localToWorldLookup.HasComponent(parentLookup[turretRef.ValueRO.Turret].Value))
                {
                    float4x4 parentWorld = localToWorldLookup[parentLookup[turretRef.ValueRO.Turret].Value].Value;
                    float3 localDir = math.mul(math.inverse(new float3x3(parentWorld.c0.xyz, parentWorld.c1.xyz, parentWorld.c2.xyz)), desiredWorldDir);
                    localDir.y = 0f;
                    if (math.lengthsq(localDir) < 1e-8f)
                        continue;
                    localRotation = quaternion.LookRotationSafe(math.normalizesafe(localDir), math.up());
                }
                else
                {
                    localRotation = quaternion.LookRotationSafe(math.normalizesafe(desiredWorldDir), math.up());
                }

                LocalTransform turretTransform = localTransformLookup[turretRef.ValueRO.Turret];
                turretTransform.Rotation = localRotation;
                localTransformLookup[turretRef.ValueRO.Turret] = turretTransform;
            }
        }
    }
}
