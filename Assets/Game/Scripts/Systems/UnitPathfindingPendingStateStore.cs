using Unity.Collections;
using Unity.Entities;

namespace Game.Runtime
{
    public struct UnitPathfindingPendingStateComponent : IComponentData
    {
        public byte HasPendingPathJob;
        public int RequestCount;
        public int RequestBudget;
        public int ScheduledFrame;
    }

    internal struct UnitPathfindingPendingStateStore
    {
        public EntityQuery CreateQuery(ref SystemState state)
        {
            return state.GetEntityQuery(ComponentType.ReadWrite<UnitPathfindingPendingStateComponent>());
        }

        public void EnsureSingleton(ref SystemState state, EntityQuery query)
        {
            if (!query.IsEmptyIgnoreFilter)
                return;

            var ecb = new EntityCommandBuffer(Allocator.Temp);
            Entity entity = ecb.CreateEntity();
            ecb.AddComponent<UnitPathfindingPendingStateComponent>(entity);
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        public static UnitPathfindingPendingStateComponent CreateState(
            bool hasPendingPathJob,
            int requestCount,
            int requestBudget,
            int scheduledFrame)
        {
            return new UnitPathfindingPendingStateComponent
            {
                HasPendingPathJob = hasPendingPathJob ? (byte)1 : (byte)0,
                RequestCount = requestCount,
                RequestBudget = requestBudget,
                ScheduledFrame = scheduledFrame
            };
        }
    }

    internal sealed class UnitPathfindingPendingStateReader
    {
        private EntityQuery _query;
        private World _world;
        private bool _hasQuery;

        public void Bind(EntityManager entityManager)
        {
            World world = entityManager.World;
            if (_hasQuery && _world == world && IsQueryWorldAlive())
                return;

            Dispose();
            _query = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<UnitPathfindingPendingStateComponent>());
            _world = world;
            _hasQuery = true;
        }

        public bool HasPendingPathJob()
        {
            if (!_hasQuery || !IsQueryWorldAlive() || _query.IsEmptyIgnoreFilter)
                return false;

            return _query.GetSingleton<UnitPathfindingPendingStateComponent>().HasPendingPathJob != 0;
        }

        public void Dispose()
        {
            if (_hasQuery && IsQueryWorldAlive())
                _query.Dispose();

            _query = default;
            _world = null;
            _hasQuery = false;
        }

        private bool IsQueryWorldAlive()
        {
            return _world != null && _world.IsCreated;
        }
    }

}
