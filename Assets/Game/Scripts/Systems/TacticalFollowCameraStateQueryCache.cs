using Unity.Entities;
using Unity.Transforms;
using Game.Components;

namespace Game.Runtime
{
    internal sealed class TacticalFollowCameraStateQueryCache
    {
        private World _world;
        private EntityQuery _modeQuery;
        private EntityQuery _poseQuery;
        private EntityQuery _followableSelectedUnitQuery;
        private EntityQuery _focusedUnitReadModelQuery;

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

        public bool HasFollowableSelectedUnit(EntityManager entityManager)
        {
            EnsureQueries(entityManager);
            return !_followableSelectedUnitQuery.IsEmptyIgnoreFilter;
        }

        public bool TryReadFocusedUnit(
            EntityManager entityManager,
            out FocusedUnitUiReadModelComponent model)
        {
            EnsureQueries(entityManager);
            if (_focusedUnitReadModelQuery.IsEmptyIgnoreFilter)
            {
                model = default;
                return false;
            }

            model = entityManager.GetComponentData<FocusedUnitUiReadModelComponent>(
                _focusedUnitReadModelQuery.GetSingletonEntity());
            return true;
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
            _followableSelectedUnitQuery = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<SelectedUnitTag>(),
                ComponentType.ReadOnly<LocalTransform>(),
                ComponentType.Exclude<Disabled>(),
                ComponentType.Exclude<UnitTransportPassenger>(),
                ComponentType.Exclude<UnitTransportCargoPassenger>());
            _focusedUnitReadModelQuery = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<FocusedUnitUiReadModelComponent>());
        }
    }
}
