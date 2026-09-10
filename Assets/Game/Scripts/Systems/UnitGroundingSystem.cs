using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Game.Components;

namespace Game.Runtime
{
    [BurstCompile]
    [UpdateAfter(typeof(UnitSurfaceTrackingSystem))]
    [UpdateAfter(typeof(UnitAnimationIndexSystem))]
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
                AnimationOffsets=SystemAPI.GetBufferLookup<UnitAnimationGroundOffset>(true),
                AnimationIndices=SystemAPI.GetComponentLookup<UnitResolvedAnimationIndex>(true),
                GroundOffsetLookup = SystemAPI.GetComponentLookup<UnitGroundOffsetComponent>(true)
            };
            state.Dependency = job.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        [WithNone(typeof(UnitAirMovement))]
        private partial struct GroundUnitsJob : IJobEntity
        {
            [ReadOnly] public BufferLookup<UnitAnimationGroundOffset> AnimationOffsets;
            [ReadOnly] public ComponentLookup<UnitResolvedAnimationIndex> AnimationIndices;
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

                if(AnimationOffsets.HasBuffer(entity) && AnimationIndices.HasComponent(entity))
                {
                    var offsets=AnimationOffsets[entity];int index=AnimationIndices[entity].Value;
                    if((uint)index<(uint)offsets.Length)offset=offsets[index].Value*transform.Scale;
                }
                float3 position = transform.Position;
                position.y = unitSurface.LastSampledHeight + offset;
                transform.Position = position;

                unitSurface.LastSampledNormal = math.normalizesafe(unitSurface.LastSampledNormal, new float3(0f, 1f, 0f));
                unitSurface.IsGrounded = 1;
            }
        }
    }
}
