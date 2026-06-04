using Unity.Collections;
using Unity.Entities;

[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial struct UiShellBoundarySystem : ISystem
{
    private EntityQuery boundaryQuery;

    public void OnCreate(ref SystemState state)
    {
        boundaryQuery = state.GetEntityQuery(ComponentType.ReadOnly<UiShellBoundaryComponent>());
    }

    public void OnUpdate(ref SystemState state)
    {
        if (!boundaryQuery.IsEmptyIgnoreFilter)
            return;

        Entity boundary = state.EntityManager.CreateEntity(typeof(UiShellBoundaryComponent));
        state.EntityManager.AddComponentData(boundary, new UiShellStateComponent
        {
            CurrentMode = UiShellMode.None,
            ActiveRoute = WarlineCaptureRoute.Splash,
            Phase = UiShellTransitionPhase.Idle,
            TransitionSequenceId = 0,
            IsTransitionRunning = 0
        });
        state.EntityManager.AddComponentData(boundary, new UiShellLoadingProgressComponent
        {
            Progress01 = 0f,
            Status = new FixedString64Bytes("Starting"),
            IsComplete = 0
        });
        state.EntityManager.AddComponentData(boundary, new UiShellArmoryCategoryComponent
        {
            Category = ArmoryCatalogCategory.Characters
        });
        state.EntityManager.AddBuffer<UiShellArmoryCategoryRequestComponent>(boundary);
        state.EntityManager.AddBuffer<UiShellRouteRequestComponent>(boundary);
        state.EntityManager.AddBuffer<UiShellRouteHistoryComponent>(boundary);
        state.EntityManager.AddBuffer<UiShellPopupRequestComponent>(boundary);
        state.EntityManager.AddBuffer<UiShellPresentationCommandComponent>(boundary);
        state.EntityManager.AddBuffer<UiShellTransitionCompleteComponent>(boundary);
    }
}
