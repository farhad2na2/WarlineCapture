using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using Game.Components;

namespace Game.Runtime
{
    [BurstCompile]
    [UpdateAfter(typeof(UnitMoveVisualStateSystem))]
    public partial struct UnitRotationHoldSystem : ISystem
    {
        private EntityQuery _ecbSingletonQuery;

        public void OnCreate(ref SystemState state)
        {
            _ecbSingletonQuery = state.GetEntityQuery(ComponentType.ReadOnly<EndSimulationEntityCommandBufferSystem.Singleton>());
            state.RequireForUpdate<UnitRotationHold>();
            state.RequireForUpdate(_ecbSingletonQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            Entity ecbEntity = _ecbSingletonQuery.GetSingletonEntity();
            var ecbSystem = state.EntityManager.GetComponentData<EndSimulationEntityCommandBufferSystem.Singleton>(ecbEntity);
            var ecb = ecbSystem.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

            var handle = new HoldJob
            {
                Ecb = ecb
            }.ScheduleParallel(state.Dependency);

            state.Dependency = handle;
        }

        [BurstCompile]
        [WithNone(typeof(EngageTarget), typeof(StaticGridBlocker))]
        private partial struct HoldJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter Ecb;

            public void Execute([EntityIndexInQuery] int sortKey, Entity entity, ref LocalTransform transform, in UnitMoveVisualComponent vis, in UnitRotationHold hold)
            {
                if (vis.IsMoving != 0)
                {
                    Ecb.RemoveComponent<UnitRotationHold>(sortKey, entity);
                    return;
                }

                transform.Rotation = hold.Rotation;
            }
        }
    }
}
