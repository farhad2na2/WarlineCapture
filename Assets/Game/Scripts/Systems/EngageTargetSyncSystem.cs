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
            _ecbSingletonQuery = new EntityQueryBuilder(Allocator.Temp).WithAll<EndSimulationEntityCommandBufferSystem.Singleton>()
                .WithOptions(EntityQueryOptions.IncludeSystems).Build(ref state);
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
                bool hasTransform=engage.Target!=Entity.Null&&TransformLookup.HasComponent(engage.Target);
                Unity.Mathematics.float3 position=default;
                Unity.Mathematics.int2 cell=default;
                if (hasTransform)
                {
                    position=TransformLookup[engage.Target].Position;
                    cell=GridUtils.WorldToCell(Grid,position);
                }
                var original=new EngagementTargetState{TargetIndex=engage.Target.Index,TargetVersion=engage.Target.Version,
                    CellX=engage.Cell.x,CellZ=engage.Cell.y,X=engage.Position.x,Y=engage.Position.y,Z=engage.Position.z,IsCommanded=engage.IsCommanded};
                var result=EngageTargetLifecycleKernel.Sync(original,hasTransform,position.x,position.y,position.z,cell.x,cell.y);
                engage.Target=new Entity{Index=result.State.TargetIndex,Version=result.State.TargetVersion};
                engage.Cell=new Unity.Mathematics.int2(result.State.CellX,result.State.CellZ);
                engage.Position=new Unity.Mathematics.float3(result.State.X,result.State.Y,result.State.Z);
                engage.IsCommanded=result.State.IsCommanded;
                if(result.RemoveAtEndSimulation)Ecb.RemoveComponent<EngageTarget>(sortKey,entity);
            }
        }
    }
}
