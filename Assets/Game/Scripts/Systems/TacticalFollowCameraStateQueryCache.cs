using Unity.Entities;
using Game.Components;

namespace Game.Runtime
{
    internal sealed class TacticalFollowCameraStateQueryCache
    {
        private World _world;
        private EntityQuery _modeQuery;
        private EntityQuery _poseQuery;

        public bool IsPanInputLocked(EntityManager entityManager)
        {
            EnsureQueries(entityManager);
            if (_modeQuery.IsEmptyIgnoreFilter)
                return false;

            TacticalFollowCameraModeComponent mode =
                entityManager.GetComponentData<TacticalFollowCameraModeComponent>(_modeQuery.GetSingletonEntity());
            return mode.Enabled != 0 && mode.PanInputLocked != 0;
        }

        public bool HasValidPose(EntityManager entityManager)
        {
            EnsureQueries(entityManager);
            if (_poseQuery.IsEmptyIgnoreFilter)
                return false;

            TacticalFollowCameraPoseComponent pose =
                entityManager.GetComponentData<TacticalFollowCameraPoseComponent>(_poseQuery.GetSingletonEntity());
            return pose.Valid != 0;
        }

        private void EnsureQueries(EntityManager entityManager)
        {
            World world = entityManager.World;
            if (_world == world && world != null && world.IsCreated)
                return;

            _world = world;
            _modeQuery = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<TacticalFollowCameraModeComponent>());
            _poseQuery = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<TacticalFollowCameraPoseComponent>());
        }
    }
}
