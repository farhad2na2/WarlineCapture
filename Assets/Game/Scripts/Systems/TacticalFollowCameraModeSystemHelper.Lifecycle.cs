using Unity.Entities;
using Game.Components;

namespace Game.Runtime
{
    public sealed partial class TacticalFollowCameraModeSystemHelper
    {
        public void Dispose()
        {
            ReleaseSingletonQueries();
            _stateQueryCache.Dispose();
        }

        private void EnsureSingletonQueries(EntityManager entityManager)
        {
            World world = entityManager.World;
            if (_singletonQueryWorld == world && world != null && world.IsCreated)
                return;

            ReleaseSingletonQueries();
            _singletonQueryWorld = world;
            _targetQuery = entityManager.CreateEntityQuery(ComponentType.ReadWrite<TacticalFollowCameraTargetComponent>());
            _poseQuery = entityManager.CreateEntityQuery(ComponentType.ReadWrite<TacticalFollowCameraPoseComponent>());
            _requestQueueQuery = entityManager.CreateEntityQuery(
                ComponentType.ReadWrite<TacticalFollowCameraRequestQueueComponent>(),
                ComponentType.ReadWrite<TacticalFollowCameraRequestElement>());
            _modeQuery = entityManager.CreateEntityQuery(ComponentType.ReadWrite<TacticalFollowCameraModeComponent>());
            _uiReadModelQuery = entityManager.CreateEntityQuery(ComponentType.ReadWrite<TacticalFollowCameraUiReadModelComponent>());
        }

        private void ReleaseSingletonQueries()
        {
            if (_singletonQueryWorld != null && _singletonQueryWorld.IsCreated)
            {
                _targetQuery.Dispose();
                _poseQuery.Dispose();
                _requestQueueQuery.Dispose();
                _modeQuery.Dispose();
                _uiReadModelQuery.Dispose();
            }

            _singletonQueryWorld = null;
            _targetQuery = default;
            _poseQuery = default;
            _requestQueueQuery = default;
            _modeQuery = default;
            _uiReadModelQuery = default;
        }
    }
}
