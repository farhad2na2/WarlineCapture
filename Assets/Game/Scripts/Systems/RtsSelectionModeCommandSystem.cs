using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[DisableAutoCreation]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct RtsSelectionModeCommandSystem : ISystem
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
        ProcessPendingRequests(
            state.EntityManager,
            _commandQueueQuery,
            _runtimeStateQuery,
            Time.frameCount,
            out _,
            out _,
            out _);
    }

    public static bool ProcessPendingRequests(
        EntityManager em,
        int currentFrame,
        out bool enteredSelectionMode,
        out bool exitedSelectionMode,
        out RtsSelectionCommandIntentKind lastProcessedKind)
    {
        using EntityQuery commandQueueQuery = em.CreateEntityQuery(
            ComponentType.ReadWrite<RtsSelectionInputStateComponent>(),
            ComponentType.ReadWrite<RtsSelectionCommandIntentRequestElement>());
        using EntityQuery runtimeStateQuery = em.CreateEntityQuery(ComponentType.ReadWrite<RuntimeGameplayStateComponent>());
        return ProcessPendingRequests(
            em,
            commandQueueQuery,
            runtimeStateQuery,
            currentFrame,
            out enteredSelectionMode,
            out exitedSelectionMode,
            out lastProcessedKind);
    }

    private static bool ProcessPendingRequests(
        EntityManager em,
        EntityQuery commandQueueQuery,
        EntityQuery runtimeStateQuery,
        int currentFrame,
        out bool enteredSelectionMode,
        out bool exitedSelectionMode,
        out RtsSelectionCommandIntentKind lastProcessedKind)
    {
        enteredSelectionMode = false;
        exitedSelectionMode = false;
        lastProcessedKind = RtsSelectionCommandIntentKind.None;
        if (commandQueueQuery.IsEmptyIgnoreFilter || runtimeStateQuery.IsEmptyIgnoreFilter)
            return false;

        Entity commandEntity = commandQueueQuery.GetSingletonEntity();
        Entity runtimeEntity = runtimeStateQuery.GetSingletonEntity();
        DynamicBuffer<RtsSelectionCommandIntentRequestElement> commandRequests =
            em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        RtsSelectionInputStateComponent inputState = em.GetComponentData<RtsSelectionInputStateComponent>(commandEntity);
        RuntimeGameplayStateComponent runtimeState = em.GetComponentData<RuntimeGameplayStateComponent>(runtimeEntity);
        bool handledAny = false;
        bool clearMoveRequests = HasRequest(commandRequests, RtsSelectionCommandIntentKind.EnterSelectionMode);

        for (int i = 0; i < commandRequests.Length;)
        {
            RtsSelectionCommandIntentKind kind = commandRequests[i].Kind;
            if (kind == RtsSelectionCommandIntentKind.Move && clearMoveRequests)
            {
                commandRequests.RemoveAt(i);
                continue;
            }

            if (kind != RtsSelectionCommandIntentKind.EnterSelectionMode &&
                kind != RtsSelectionCommandIntentKind.ExitSelectionMode)
            {
                i++;
                continue;
            }

            commandRequests.RemoveAt(i);
            handledAny = true;
            lastProcessedKind = kind;
            if (kind == RtsSelectionCommandIntentKind.EnterSelectionMode)
            {
                ApplyEnterSelectionMode(ref inputState, ref runtimeState, currentFrame);
                enteredSelectionMode = true;
            }
            else
            {
                ApplyExitSelectionMode(ref inputState, ref runtimeState, currentFrame);
                exitedSelectionMode = true;
            }
        }

        if (!handledAny)
            return false;

        em.SetComponentData(commandEntity, inputState);
        em.SetComponentData(runtimeEntity, runtimeState);
        return true;
    }

    private static bool HasRequest(
        DynamicBuffer<RtsSelectionCommandIntentRequestElement> commandRequests,
        RtsSelectionCommandIntentKind kind)
    {
        for (int i = 0; i < commandRequests.Length; i++)
        {
            if (commandRequests[i].Kind == kind)
                return true;
        }

        return false;
    }

    private static void ApplyEnterSelectionMode(
        ref RtsSelectionInputStateComponent inputState,
        ref RuntimeGameplayStateComponent runtimeState,
        int currentFrame)
    {
        float2 pointer = inputState.HasLastKnownPointerPosition != 0
            ? inputState.LastKnownPointerPosition
            : float2.zero;
        ClearCommandMode(ref inputState);
        ResetSelectionDragState(ref inputState, pointer);
        ClearQueuedMoveOrder(ref inputState);
        inputState.IgnoreNextLeftMouseRelease = 1;
        inputState.SkipNextWorldReleaseAfterSelection = 1;
        inputState.IgnoreWorldCommandsUntilFrame = currentFrame + 1;
        runtimeState.SelectionModeActive = 1;
        runtimeState.SuppressNextWorldClick = 1;
    }

    private static void ApplyExitSelectionMode(
        ref RtsSelectionInputStateComponent inputState,
        ref RuntimeGameplayStateComponent runtimeState,
        int currentFrame)
    {
        float2 pointer = inputState.HasLastKnownPointerPosition != 0
            ? inputState.LastKnownPointerPosition
            : float2.zero;
        ClearCommandMode(ref inputState);
        ResetSelectionDragState(ref inputState, pointer);
        inputState.IgnoreNextLeftMouseRelease = 1;
        inputState.SkipNextWorldReleaseAfterSelection = 0;
        inputState.IgnoreWorldCommandsUntilFrame = currentFrame + 1;
        runtimeState.SelectionModeActive = 0;
        runtimeState.SuppressNextWorldClick = 1;
    }

    private static void ResetSelectionDragState(ref RtsSelectionInputStateComponent inputState, float2 pointer)
    {
        inputState.DragStart = pointer;
        inputState.DragCurrent = pointer;
        inputState.LastPointerPosition = pointer;
        inputState.PointerPressedOverUi = 0;
        inputState.IsDraggingSelection = 0;
        inputState.SelectionModeHoldArmed = 0;
        inputState.LastLiveSelectionRect = new float4(pointer.x, pointer.y, pointer.x, pointer.y);
        inputState.HasLiveSelectionRect = 0;
        inputState.BoardPassengerDragArmed = 0;
    }

    private static void ClearQueuedMoveOrder(ref RtsSelectionInputStateComponent inputState)
    {
        inputState.QueuedMoveOrderToken++;
        inputState.HasQueuedMoveOrder = 0;
        inputState.QueuedMoveOrderScreenPosition = default;
        inputState.QueuedMoveOrderFrame = -1;
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
