using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[DisableAutoCreation]
public partial struct RtsSelectionMoveTargetModeCommandSystem : ISystem
{
    private EntityQuery _commandQueueQuery;
    private EntityQuery _runtimeStateQuery;
    private EntityQuery _selectedMoveQuery;

    public void OnCreate(ref SystemState state)
    {
        _commandQueueQuery = state.GetEntityQuery(
            ComponentType.ReadWrite<RtsSelectionInputStateComponent>(),
            ComponentType.ReadWrite<RtsSelectionCommandIntentRequestElement>());
        _runtimeStateQuery = state.GetEntityQuery(ComponentType.ReadWrite<RuntimeGameplayStateComponent>());
        _selectedMoveQuery = CreateSelectedMoveQuery(ref state);
        state.RequireForUpdate(_commandQueueQuery);
    }

    public void OnUpdate(ref SystemState state)
    {
        ProcessPendingRequests(
            state.EntityManager,
            _commandQueueQuery,
            _runtimeStateQuery,
            _selectedMoveQuery,
            UnityEngine.Time.frameCount,
            out _,
            out _);
    }

    public static bool ProcessPendingRequests(
        EntityManager em,
        int currentFrame,
        out bool accepted,
        out TacticalCommandReasonCode rejectionReason)
    {
        using EntityQuery commandQueueQuery = em.CreateEntityQuery(
            ComponentType.ReadWrite<RtsSelectionInputStateComponent>(),
            ComponentType.ReadWrite<RtsSelectionCommandIntentRequestElement>());
        using EntityQuery runtimeStateQuery = em.CreateEntityQuery(ComponentType.ReadWrite<RuntimeGameplayStateComponent>());
        using EntityQuery selectedMoveQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<SelectedUnitTag>(),
            ComponentType.ReadOnly<Faction>(),
            ComponentType.ReadOnly<UnitGrid>(),
            ComponentType.ReadOnly<UnitMove>(),
            ComponentType.Exclude<Disabled>(),
            ComponentType.Exclude<UnitTransportPassenger>());

        return ProcessPendingRequests(
            em,
            commandQueueQuery,
            runtimeStateQuery,
            selectedMoveQuery,
            currentFrame,
            out accepted,
            out rejectionReason);
    }

    private static EntityQuery CreateSelectedMoveQuery(ref SystemState state)
    {
        return state.GetEntityQuery(
            ComponentType.ReadOnly<SelectedUnitTag>(),
            ComponentType.ReadOnly<Faction>(),
            ComponentType.ReadOnly<UnitGrid>(),
            ComponentType.ReadOnly<UnitMove>(),
            ComponentType.Exclude<Disabled>(),
            ComponentType.Exclude<UnitTransportPassenger>());
    }

    private static bool ProcessPendingRequests(
        EntityManager em,
        EntityQuery commandQueueQuery,
        EntityQuery runtimeStateQuery,
        EntityQuery selectedMoveQuery,
        int currentFrame,
        out bool accepted,
        out TacticalCommandReasonCode rejectionReason)
    {
        accepted = false;
        rejectionReason = TacticalCommandReasonCode.None;
        if (commandQueueQuery.IsEmptyIgnoreFilter || runtimeStateQuery.IsEmptyIgnoreFilter)
            return false;

        Entity commandEntity = commandQueueQuery.GetSingletonEntity();
        Entity runtimeEntity = runtimeStateQuery.GetSingletonEntity();
        DynamicBuffer<RtsSelectionCommandIntentRequestElement> commandRequests =
            em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        bool hasEnterMoveTargetModeRequest = HasRequest(commandRequests, RtsSelectionCommandIntentKind.EnterMoveTargetMode);
        if (!hasEnterMoveTargetModeRequest)
            return false;

        for (int i = 0; i < commandRequests.Length;)
        {
            RtsSelectionCommandIntentKind kind = commandRequests[i].Kind;
            if (kind == RtsSelectionCommandIntentKind.Move ||
                kind == RtsSelectionCommandIntentKind.EnterMoveTargetMode)
            {
                commandRequests.RemoveAt(i);
                continue;
            }

            i++;
        }

        RtsSelectionInputStateComponent inputState = em.GetComponentData<RtsSelectionInputStateComponent>(commandEntity);
        ClearQueuedMoveOrder(ref inputState);
        if (!HasSelectedMovablePlayerUnit(em, selectedMoveQuery))
        {
            ClearCommandMode(ref inputState);
            em.SetComponentData(commandEntity, inputState);
            rejectionReason = TacticalCommandReasonCode.NoSelection;
            return true;
        }

        RuntimeGameplayStateComponent runtimeState = em.GetComponentData<RuntimeGameplayStateComponent>(runtimeEntity);
        ApplyEnterMoveTargetMode(ref inputState, ref runtimeState, currentFrame);
        em.SetComponentData(commandEntity, inputState);
        em.SetComponentData(runtimeEntity, runtimeState);
        accepted = true;
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

    private static bool HasSelectedMovablePlayerUnit(EntityManager em, EntityQuery selectedMoveQuery)
    {
        if (selectedMoveQuery.IsEmptyIgnoreFilter)
            return false;

        ComponentTypeHandle<Faction> factionType = em.GetComponentTypeHandle<Faction>(true);
        using NativeArray<ArchetypeChunk> chunks = selectedMoveQuery.ToArchetypeChunkArray(Allocator.Temp);
        for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
        {
            NativeArray<Faction> factions = chunks[chunkIndex].GetNativeArray(ref factionType);
            for (int i = 0; i < factions.Length; i++)
            {
                if (FactionIdentity.IsPlayerControlled(factions[i].Id))
                    return true;
            }
        }

        return false;
    }

    private static void ApplyEnterMoveTargetMode(
        ref RtsSelectionInputStateComponent inputState,
        ref RuntimeGameplayStateComponent runtimeState,
        int currentFrame)
    {
        float2 pointer = inputState.HasLastKnownPointerPosition != 0
            ? inputState.LastKnownPointerPosition
            : float2.zero;
        ResetSelectionDragState(ref inputState, pointer);
        inputState.IgnoreNextLeftMouseRelease = 1;
        inputState.SkipNextWorldReleaseAfterSelection = 1;
        inputState.IgnoreWorldCommandsUntilFrame = currentFrame + 1;
        ArmCommandMode(
            ref inputState,
            TacticalCommandMode.Move,
            currentFrame,
            oneShot: true,
            requiresWorldTarget: true);
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

    private static void ArmCommandMode(
        ref RtsSelectionInputStateComponent inputState,
        TacticalCommandMode mode,
        int frame,
        bool oneShot,
        bool requiresWorldTarget)
    {
        inputState.ActiveCommandMode = (int)mode;
        inputState.ActiveCommandModeFrame = frame;
        inputState.ActiveCommandModeOneShot = oneShot ? (byte)1 : (byte)0;
        inputState.ActiveCommandModeRequiresWorldTarget = requiresWorldTarget ? (byte)1 : (byte)0;
        inputState.ActiveBoardCommandDirection = 0;
        inputState.ActiveBoardTransport = Entity.Null;
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
