using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using Game.Components;

namespace Game.Runtime
{
    [BurstCompile]
    [UpdateAfter(typeof(UnitEngagementSystem))]
    [UpdateBefore(typeof(UnitEngagedMovementSystem))]
    public partial struct EngageTargetSyncSystem : ISystem
    {
        private EntityQuery _gridQuery;
        private EntityQuery _ecbSingletonQuery;

        public void OnCreate(ref SystemState state)
        {
            _gridQuery = state.GetEntityQuery(ComponentType.ReadOnly<GridConfig>());
            _ecbSingletonQuery = state.GetEntityQuery(ComponentType.ReadOnly<EndSimulationEntityCommandBufferSystem.Singleton>());
            state.RequireForUpdate(_gridQuery);
            state.RequireForUpdate<EngageTarget>();
            state.RequireForUpdate(_ecbSingletonQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            Entity gridEntity = _gridQuery.GetSingletonEntity();
            GridConfig grid = state.EntityManager.GetComponentData<GridConfig>(gridEntity);
            var transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);
            Entity ecbEntity = _ecbSingletonQuery.GetSingletonEntity();
            var ecbSystem = state.EntityManager.GetComponentData<EndSimulationEntityCommandBufferSystem.Singleton>(ecbEntity);
            var ecb = ecbSystem.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

            var handle = new SyncJob
            {
                Grid = grid,
                TransformLookup = transformLookup,
                Ecb = ecb
            }.ScheduleParallel(state.Dependency);

            state.Dependency = handle;
        }

        [BurstCompile]
        private partial struct SyncJob : IJobEntity
        {
            public GridConfig Grid;
            [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
            public EntityCommandBuffer.ParallelWriter Ecb;

            public void Execute([EntityIndexInQuery] int sortKey, Entity entity, in LocalTransform selfTransform, ref EngageTarget engage)
            {
                if (engage.Target == Entity.Null || !TransformLookup.HasComponent(engage.Target))
                {
                    engage.Target = Entity.Null;
                    engage.Cell = default;
                    engage.Position = default;
                    Ecb.RemoveComponent<EngageTarget>(sortKey, entity);
                    return;
                }

                var t = TransformLookup[engage.Target];
                engage.Position = t.Position;
                engage.Cell = GridUtils.WorldToCell(Grid, t.Position);
            }
        }
    }
}
