using Game.Components;
using Game.Missions.Contracts;
using Unity.Entities;

namespace Game.Runtime
{
    internal sealed class AudioGameplayStateQueryCache : System.IDisposable
    {
        private World _world;
        private EntityQuery _query;
        private EntityQuery _missionQuery;
        private bool _hasQuery;

        public bool IsSimulationActive(EntityManager entityManager)
        {
            EnsureQuery(entityManager);
            if (_query.IsEmptyIgnoreFilter)
                return false;

            RuntimeGameplayStateComponent state = _query.GetSingleton<RuntimeGameplayStateComponent>();
            if (state.PlayRequested == 0 || state.SimulationActive == 0) return false;
            // Mission completion precedes debrief/popup presentation. Stop voices at
            // the outcome boundary, even before the simulation gate is updated.
            return _missionQuery.IsEmptyIgnoreFilter ||
                   _missionQuery.GetSingleton<CampaignMissionRuntimeComponent>().Outcome == MissionOutcomeKind.None;
        }

        public void Dispose()
        {
            if (_hasQuery && _world != null && _world.IsCreated)
            {
                _query.Dispose();
                _missionQuery.Dispose();
            }

            _query = default;
            _missionQuery = default;
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
            _missionQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<CampaignMissionRuntimeComponent>());
            _hasQuery = true;
        }
    }
}
