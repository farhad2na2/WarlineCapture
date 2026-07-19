using Game.Components;
using Game.UI.Shell.Contracts.Ecs;
using Unity.Burst;
using Unity.Entities;

namespace Game.UI.Shell.Ecs
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [BurstCompile]
    public partial struct UiMatchIdentityReadModelSystem : ISystem
    {
        private EntityQuery _boundaryQuery;
        private EntityQuery _activeMapQuery;

        public void OnCreate(ref SystemState state)
        {
            _boundaryQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<UiShellRootComponent>(),
                ComponentType.ReadWrite<UiMatchIdentityReadModelComponent>());
            _activeMapQuery = state.GetEntityQuery(ComponentType.ReadOnly<ActiveOperationMapComponent>());
            state.RequireForUpdate(_boundaryQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            UiMatchIdentityReadModelComponent next = default;
            if (_activeMapQuery.CalculateEntityCount() == 1)
            {
                ActiveOperationMapComponent activeMap =
                    _activeMapQuery.GetSingleton<ActiveOperationMapComponent>();
                next.OperationMapId = activeMap.OperationMapId;
                next.ScenarioId = activeMap.ScenarioId;
                next.MissionId = activeMap.MissionId;
            }

            Entity boundary = _boundaryQuery.GetSingletonEntity();
            UiMatchIdentityReadModelComponent current =
                state.EntityManager.GetComponentData<UiMatchIdentityReadModelComponent>(boundary);
            if (HasSameIdentity(current, next))
                return;

            next.Version = current.Version == uint.MaxValue ? 1u : current.Version + 1u;
            state.EntityManager.SetComponentData(boundary, next);
        }

        private static bool HasSameIdentity(
            in UiMatchIdentityReadModelComponent left,
            in UiMatchIdentityReadModelComponent right)
        {
            return left.OperationMapId.Equals(right.OperationMapId) &&
                   left.ScenarioId.Equals(right.ScenarioId) &&
                   left.MissionId.Equals(right.MissionId);
        }
    }
}
