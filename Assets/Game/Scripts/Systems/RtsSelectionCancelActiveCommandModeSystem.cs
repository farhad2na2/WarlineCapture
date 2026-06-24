using Unity.Entities;

[DisableAutoCreation]
public partial struct RtsSelectionCancelActiveCommandModeSystem : ISystem
{
    private EntityQuery _commandQueueQuery;
    private EntityQuery _runtimeStateQuery;

    public void OnCreate(ref SystemState state)
    {
        _commandQueueQuery = state.GetEntityQuery(
            ComponentType.ReadWrite<RtsSelectionInputStateComponent>(),
            ComponentType.ReadWrite<RtsSelectionCommandIntentRequestElement>());
        _runtimeStateQuery = state.GetEntityQuery(ComponentType.ReadWrite<RuntimeGameplayStateComponent>());
        state.RequireForUpdate(_commandQueueQuery);
    }

    public void OnUpdate(ref SystemState state)
    {
        ProcessPendingRequests(state.EntityManager, _commandQueueQuery, _runtimeStateQuery, out _);
    }

    public static bool ProcessPendingRequests(EntityManager em)
    {
        return ProcessPendingRequests(em, out _);
    }

    public static bool ProcessPendingRequests(
        EntityManager em,
        out RtsSelectionCommandIntentKind processedKind)
    {
        using EntityQuery commandQueueQuery = em.CreateEntityQuery(
            ComponentType.ReadWrite<RtsSelectionInputStateComponent>(),
            ComponentType.ReadWrite<RtsSelectionCommandIntentRequestElement>());
        using EntityQuery runtimeStateQuery = em.CreateEntityQuery(ComponentType.ReadWrite<RuntimeGameplayStateComponent>());
        return ProcessPendingRequests(em, commandQueueQuery, runtimeStateQuery, out processedKind);
    }

    private static bool ProcessPendingRequests(
        EntityManager em,
        EntityQuery commandQueueQuery,
        EntityQuery runtimeStateQuery,
        out RtsSelectionCommandIntentKind processedKind)
    {
        processedKind = RtsSelectionCommandIntentKind.None;
        if (commandQueueQuery.IsEmptyIgnoreFilter || runtimeStateQuery.IsEmptyIgnoreFilter)
            return false;

        Entity commandEntity = commandQueueQuery.GetSingletonEntity();
        DynamicBuffer<RtsSelectionCommandIntentRequestElement> commandRequests =
            em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        bool handledAny = false;

        for (int i = 0; i < commandRequests.Length;)
        {
            RtsSelectionCommandIntentKind kind = commandRequests[i].Kind;
            if (!IsCancelCommand(kind))
            {
                i++;
                continue;
            }

            if (processedKind == RtsSelectionCommandIntentKind.None)
                processedKind = kind;
            commandRequests.RemoveAt(i);
            handledAny = true;
        }

        if (!handledAny)
            return false;

        Entity runtimeEntity = runtimeStateQuery.GetSingletonEntity();
        RtsSelectionInputStateComponent inputState = em.GetComponentData<RtsSelectionInputStateComponent>(commandEntity);
        RuntimeGameplayStateComponent runtimeState = em.GetComponentData<RuntimeGameplayStateComponent>(runtimeEntity);
        ClearCommandMode(ref inputState);
        runtimeState.SelectionModeActive = 0;
        runtimeState.SuppressNextWorldClick = 1;
        em.SetComponentData(commandEntity, inputState);
        em.SetComponentData(runtimeEntity, runtimeState);
        return true;
    }

    private static bool IsCancelCommand(RtsSelectionCommandIntentKind kind)
    {
        return kind == RtsSelectionCommandIntentKind.CancelActiveCommandMode ||
               kind == RtsSelectionCommandIntentKind.CancelAttackTargetMode;
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
    }
}
