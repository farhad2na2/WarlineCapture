using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

[BurstCompile]
[UpdateAfter(typeof(UnitEngagementSystem))]
[UpdateBefore(typeof(UnitEngagedMovementSystem))]
public partial struct EngageTargetSyncSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<GridConfig>();
        state.RequireForUpdate<EngageTarget>();
        state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var grid = SystemAPI.GetSingleton<GridConfig>();
        var transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);
        var ecbSystem = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
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
