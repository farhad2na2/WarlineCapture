using Game.Components;
using Unity.Entities;

namespace Game.Runtime
{
    internal sealed class AudioGameplayStateQueryCache : System.IDisposable
    {
        private World _world;
        private EntityQuery _query;
        private bool _hasQuery;

        public bool IsSimulationActive(EntityManager entityManager)
        {
            EnsureQuery(entityManager);
            if (_query.IsEmptyIgnoreFilter)
                return false;

            RuntimeGameplayStateComponent state = _query.GetSingleton<RuntimeGameplayStateComponent>();
            return state.PlayRequested != 0 && state.SimulationActive != 0;
        }

        public void Dispose()
        {
            if (_hasQuery && _world != null && _world.IsCreated)
                _query.Dispose();

            _query = default;
            _hasQuery = false;
            _world = null;
        }

        private void EnsureQuery(EntityManager entityManager)
        {
            World world = entityManager.World;
            if (_hasQuery && _world == world && world != null && world.IsCreated)
                return;

            Dispose();
            _world = world;
            _query = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<RuntimeGameplayStateComponent>());
            _hasQuery = true;
        }
    }
}
