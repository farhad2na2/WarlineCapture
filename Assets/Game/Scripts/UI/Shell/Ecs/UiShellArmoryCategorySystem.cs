using Unity.Entities;
using Game.UI.Shell.Contracts.Ecs;

namespace Game.UI.Shell.Ecs
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct UiShellArmoryCategorySystem : ISystem
    {
        private EntityQuery boundaryQuery;

        public void OnCreate(ref SystemState state)
        {
            UiShellEcsGateway.RegisterAsRuntimeGateway();
            boundaryQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<UiShellStateComponent>(),
                ComponentType.ReadWrite<UiShellArmoryCategoryComponent>(),
                ComponentType.ReadWrite<UiShellArmoryCategoryRequestComponent>());
            state.RequireForUpdate(boundaryQuery);
        }

        public void OnUpdate(ref SystemState state)
        {
            if (boundaryQuery.IsEmptyIgnoreFilter)
                return;

            Entity boundary = boundaryQuery.GetSingletonEntity();
            DynamicBuffer<UiShellArmoryCategoryRequestComponent> requests =
                state.EntityManager.GetBuffer<UiShellArmoryCategoryRequestComponent>(boundary);
            if (requests.Length == 0)
                return;

            UiShellArmoryCategoryComponent categoryState =
                state.EntityManager.GetComponentData<UiShellArmoryCategoryComponent>(boundary);
            categoryState.Category = requests[requests.Length - 1].Category;
            requests.Clear();
            state.EntityManager.SetComponentData(boundary, categoryState);
        }
    }
}
