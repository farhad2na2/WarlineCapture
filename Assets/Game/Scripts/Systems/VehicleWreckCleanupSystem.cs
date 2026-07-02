using Unity.Collections;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Game.Components;

namespace Game.Runtime
{
    [UpdateAfter(typeof(UnitDeathSystem))]
    public partial struct VehicleWreckCleanupSystem : ISystem
    {
        private EntityQuery _respawnQueueQuery;
        private EntityQuery _wreckQuery;

        public void OnCreate(ref SystemState state)
        {
            _respawnQueueQuery = state.GetEntityQuery(ComponentType.ReadOnly<RespawnQueueTag>());
            _wreckQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<VehicleWreckComponent>(),
                ComponentType.ReadOnly<UnitHealth>());
            state.RequireForUpdate<VehicleWreckComponent>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var queueEntity = RespawnQueueUtility.GetOrCreateQueue(ref state, _respawnQueueQuery);
            var queueState = SystemAPI.GetComponent<RespawnQueueComponent>(queueEntity);
            var em = state.EntityManager;
            float dt = SystemAPI.Time.DeltaTime;
            double now = SystemAPI.Time.ElapsedTime;
            double respawnDelay = math.max(0.01f, queueState.RespawnDelaySeconds);

            int capacity = _wreckQuery.CalculateEntityCount();
            var finalize = new NativeList<Entity>(math.max(1, capacity), Allocator.TempJob);
            state.Dependency = new CollectExpiredWrecksJob
            {
                DeltaTime = dt,
                FinalizeEntities = finalize.AsParallelWriter()
            }.ScheduleParallel(state.Dependency);
            state.Dependency.Complete();

            for (int i = 0; i < finalize.Length; i++)
                UnitDeathSystem.FinalizeDeath(em, queueEntity, finalize[i], now, respawnDelay);

            finalize.Dispose();
        }

        [BurstCompile]
        private partial struct CollectExpiredWrecksJob : IJobEntity
        {
            public float DeltaTime;
            public NativeList<Entity>.ParallelWriter FinalizeEntities;

            public void Execute(Entity entity, ref VehicleWreckComponent wreck, in UnitHealth health)
            {
                if (health.Current > 0)
                    return;

                wreck.TimeRemaining -= DeltaTime;
                if (wreck.TimeRemaining <= 0f)
                    FinalizeEntities.AddNoResize(entity);
            }
        }
    }
}
