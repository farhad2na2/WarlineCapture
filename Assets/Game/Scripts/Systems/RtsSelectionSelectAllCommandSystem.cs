using Unity.Entities;

[DisableAutoCreation]
public partial struct RtsSelectionSelectAllCommandSystem : ISystem
{
    private EntityQuery _commandQueueQuery;

    public void OnCreate(ref SystemState state)
    {
        _commandQueueQuery = state.GetEntityQuery(
            ComponentType.ReadWrite<RtsSelectionInputRequestQueueComponent>(),
            ComponentType.ReadWrite<RtsSelectionInputStateComponent>(),
            ComponentType.ReadWrite<RtsSelectionCommandIntentRequestElement>(),
            ComponentType.ReadWrite<RtsSelectionPointerRequestElement>());
        state.RequireForUpdate(_commandQueueQuery);
    }

    public void OnUpdate(ref SystemState state)
    {
        ProcessPendingRequests(state.EntityManager, _commandQueueQuery);
    }

    public static bool ProcessPendingRequests(EntityManager em)
    {
        using EntityQuery commandQueueQuery = em.CreateEntityQuery(
            ComponentType.ReadWrite<RtsSelectionInputRequestQueueComponent>(),
            ComponentType.ReadWrite<RtsSelectionInputStateComponent>(),
            ComponentType.ReadWrite<RtsSelectionCommandIntentRequestElement>(),
            ComponentType.ReadWrite<RtsSelectionPointerRequestElement>());
        return ProcessPendingRequests(em, commandQueueQuery);
    }

    private static bool ProcessPendingRequests(EntityManager em, EntityQuery commandQueueQuery)
    {
        if (commandQueueQuery.IsEmptyIgnoreFilter)
            return false;

        Entity commandEntity = commandQueueQuery.GetSingletonEntity();
        DynamicBuffer<RtsSelectionCommandIntentRequestElement> commandRequests =
            em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        DynamicBuffer<RtsSelectionPointerRequestElement> pointerRequests =
            em.GetBuffer<RtsSelectionPointerRequestElement>(commandEntity);
        RtsSelectionInputStateComponent inputState = em.GetComponentData<RtsSelectionInputStateComponent>(commandEntity);
        RtsSelectionInputRequestQueueComponent queue = em.GetComponentData<RtsSelectionInputRequestQueueComponent>(commandEntity);
        bool handledAny = false;

        for (int i = 0; i < commandRequests.Length;)
        {
            RtsSelectionCommandIntentRequestElement request = commandRequests[i];
            if (!IsSelectAllRequest(request.Kind) ||
                request.HasScreenRect == 0)
            {
                i++;
                continue;
            }

            commandRequests.RemoveAt(i);
            ClearCommandMode(ref inputState);
            queue.LastRequestId++;
            pointerRequests.Add(new RtsSelectionPointerRequestElement
            {
                Kind = RtsSelectionPointerRequestKind.SelectionRectCommitted,
                RequestId = queue.LastRequestId,
                Frame = request.Frame,
                ScreenPosition = request.ScreenPosition,
                DragStart = request.DragStart,
                DragCurrent = request.DragCurrent,
                SelectionFilter = ResolveSelectionFilter(request.Kind)
            });
            handledAny = true;
        }

        if (handledAny)
        {
            em.SetComponentData(commandEntity, inputState);
            em.SetComponentData(commandEntity, queue);
        }

        return handledAny;
    }

    private static bool IsSelectAllRequest(RtsSelectionCommandIntentKind kind)
    {
        return kind == RtsSelectionCommandIntentKind.SelectAll ||
               kind == RtsSelectionCommandIntentKind.SelectAllSoldiers ||
               kind == RtsSelectionCommandIntentKind.SelectAllVehicles;
    }

    private static byte ResolveSelectionFilter(RtsSelectionCommandIntentKind kind)
    {
        return kind switch
        {
            RtsSelectionCommandIntentKind.SelectAllSoldiers => (byte)VisibleUnitSelectionSystem.Filter.Soldiers,
            RtsSelectionCommandIntentKind.SelectAllVehicles => (byte)VisibleUnitSelectionSystem.Filter.Vehicles,
            _ => (byte)VisibleUnitSelectionSystem.Filter.All
        };
    }

    private static void ClearCommandMode(ref RtsSelectionInputStateComponent inputState)
    {
        inputState.ActiveCommandMode = (int)TacticalCommandMode.None;
        inputState.ActiveCommandModeFrame = 0;
        inputState.ActiveCommandModeOneShot = 0;
        inputState.ActiveCommandModeRequiresWorldTarget = 0;
        inputState.ActiveBoardCommandDirection = 0;
        inputState.ActiveBoardTransport = Entity.Null;
        inputState.BoardPassengerDragArmed = 0;
        inputState.IgnoreNextLeftMouseRelease = 0;
        inputState.SkipNextWorldReleaseAfterSelection = 0;
    }
}
