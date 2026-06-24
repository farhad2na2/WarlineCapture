using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[DisableAutoCreation]
public partial struct RtsSelectionBoardTargetModeCommandSystem : ISystem
{
    private EntityQuery _commandQueueQuery;
    private EntityQuery _runtimeStateQuery;
    private EntityQuery _selectedQuery;

    public void OnCreate(ref SystemState state)
    {
        _commandQueueQuery = state.GetEntityQuery(
            ComponentType.ReadWrite<RtsSelectionInputStateComponent>(),
            ComponentType.ReadWrite<RtsSelectionCommandIntentRequestElement>());
        _runtimeStateQuery = state.GetEntityQuery(ComponentType.ReadWrite<RuntimeGameplayStateComponent>());
        _selectedQuery = state.GetEntityQuery(ComponentType.ReadOnly<SelectedUnitTag>());
        state.RequireForUpdate(_commandQueueQuery);
    }

    public void OnUpdate(ref SystemState state)
    {
        ProcessPendingRequests(
            state.EntityManager,
            _commandQueueQuery,
            _runtimeStateQuery,
            _selectedQuery,
            UnityEngine.Time.frameCount,
            out _,
            out _,
            out _,
            out _,
            out _);
    }

    public static bool ProcessPendingRequests(
        EntityManager em,
        int currentFrame,
        out bool accepted,
        out bool toggledOff,
        out BoardCommandModeDirection direction,
        out Entity transport,
        out TacticalCommandReasonCode rejectionReason)
    {
        using EntityQuery commandQueueQuery = em.CreateEntityQuery(
            ComponentType.ReadWrite<RtsSelectionInputStateComponent>(),
            ComponentType.ReadWrite<RtsSelectionCommandIntentRequestElement>());
        using EntityQuery runtimeStateQuery = em.CreateEntityQuery(ComponentType.ReadWrite<RuntimeGameplayStateComponent>());
        using EntityQuery selectedQuery = em.CreateEntityQuery(ComponentType.ReadOnly<SelectedUnitTag>());

        return ProcessPendingRequests(
            em,
            commandQueueQuery,
            runtimeStateQuery,
            selectedQuery,
            currentFrame,
            out accepted,
            out toggledOff,
            out direction,
            out transport,
            out rejectionReason);
    }

    private static bool ProcessPendingRequests(
        EntityManager em,
        EntityQuery commandQueueQuery,
        EntityQuery runtimeStateQuery,
        EntityQuery selectedQuery,
        int currentFrame,
        out bool accepted,
        out bool toggledOff,
        out BoardCommandModeDirection direction,
        out Entity transport,
        out TacticalCommandReasonCode rejectionReason)
    {
        accepted = false;
        toggledOff = false;
        direction = BoardCommandModeDirection.None;
        transport = Entity.Null;
        rejectionReason = TacticalCommandReasonCode.None;
        if (commandQueueQuery.IsEmptyIgnoreFilter || runtimeStateQuery.IsEmptyIgnoreFilter)
            return false;

        Entity commandEntity = commandQueueQuery.GetSingletonEntity();
        Entity runtimeEntity = runtimeStateQuery.GetSingletonEntity();
        DynamicBuffer<RtsSelectionCommandIntentRequestElement> commandRequests =
            em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        if (!HasRequest(commandRequests, RtsSelectionCommandIntentKind.EnterBoardTargetMode))
            return false;

        for (int i = 0; i < commandRequests.Length;)
        {
            RtsSelectionCommandIntentKind kind = commandRequests[i].Kind;
            if (kind == RtsSelectionCommandIntentKind.Move ||
                kind == RtsSelectionCommandIntentKind.EnterBoardTargetMode)
            {
                commandRequests.RemoveAt(i);
                continue;
            }

            i++;
        }

        RtsSelectionInputStateComponent inputState = em.GetComponentData<RtsSelectionInputStateComponent>(commandEntity);
        ClearQueuedMoveOrder(ref inputState);
        if ((TacticalCommandMode)inputState.ActiveCommandMode == TacticalCommandMode.Board)
        {
            ClearCommandMode(ref inputState);
            em.SetComponentData(commandEntity, inputState);
            toggledOff = true;
            return true;
        }

        BoardModeSource source = ResolveBoardModeSource(em, selectedQuery);
        if (!source.Accepted)
        {
            ClearCommandMode(ref inputState);
            em.SetComponentData(commandEntity, inputState);
            rejectionReason = source.HasSelected
                ? TacticalCommandReasonCode.CommandUnavailable
                : TacticalCommandReasonCode.NoSelection;
            return true;
        }

        RuntimeGameplayStateComponent runtimeState = em.GetComponentData<RuntimeGameplayStateComponent>(runtimeEntity);
        ApplyEnterBoardTargetMode(
            ref inputState,
            ref runtimeState,
            source.Direction,
            source.Transport,
            currentFrame);
        em.SetComponentData(commandEntity, inputState);
        em.SetComponentData(runtimeEntity, runtimeState);
        accepted = true;
        direction = source.Direction;
        transport = source.Transport;
        return true;
    }

    private static BoardModeSource ResolveBoardModeSource(EntityManager em, EntityQuery selectedQuery)
    {
        BoardModeSource source = default;
        if (selectedQuery.IsEmptyIgnoreFilter)
            return source;

        Entity firstAvailableTransport = Entity.Null;
        Entity firstDedicatedTransport = Entity.Null;
        EntityTypeHandle entityType = em.GetEntityTypeHandle();
        using NativeArray<ArchetypeChunk> chunks = selectedQuery.ToArchetypeChunkArray(Allocator.Temp);
        for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
        {
            NativeArray<Entity> selectedEntities = chunks[chunkIndex].GetNativeArray(entityType);
            for (int i = 0; i < selectedEntities.Length; i++)
            {
                Entity entity = selectedEntities[i];
                if (!em.Exists(entity))
                    continue;

                source.HasSelected = true;
                bool isBoardTransport = IsBoardTransportWithAvailableSeats(em, entity);
                bool isBoardPassenger = IsBoardPassengerCandidate(em, entity);
                if (isBoardTransport)
                {
                    firstAvailableTransport = firstAvailableTransport == Entity.Null ? entity : firstAvailableTransport;
                    if (!isBoardPassenger)
                        firstDedicatedTransport = firstDedicatedTransport == Entity.Null ? entity : firstDedicatedTransport;
                }

                if (isBoardPassenger)
                    source.HasSelectedBoardPassenger = true;
            }
        }

        if (firstDedicatedTransport != Entity.Null)
        {
            source.Accepted = true;
            source.Direction = BoardCommandModeDirection.TransportToPassenger;
            source.Transport = firstDedicatedTransport;
        }
        else if (source.HasSelectedBoardPassenger)
        {
            source.Accepted = true;
            source.Direction = BoardCommandModeDirection.PassengerToTransport;
        }
        else if (firstAvailableTransport != Entity.Null)
        {
            source.Accepted = true;
            source.Direction = BoardCommandModeDirection.TransportToPassenger;
            source.Transport = firstAvailableTransport;
        }

        return source;
    }

    private static bool IsBoardTransportWithAvailableSeats(EntityManager em, Entity entity)
    {
        if (!IsBoardablePlayerTransport(em, entity))
            return false;

        return HasAvailableBoardingSlot(em, entity, UnitTransportPassengerKind.Soldier) ||
               HasAvailableBoardingSlot(em, entity, UnitTransportPassengerKind.Vehicle);
    }

    private static bool IsBoardablePlayerTransport(EntityManager em, Entity entity)
    {
        return TransportBoardingCommandSystem.IsBoardablePlayerTransport(em, entity);
    }

    private static bool HasAvailableBoardingSlot(EntityManager em, Entity entity, byte passengerKind)
    {
        return TransportBoardingCommandSystem.HasAvailableTransportBoardingSlot(
                   em,
                   entity,
                   passengerKind,
                   out int occupied,
                   out int capacity) &&
               capacity > occupied + CountPendingBoardingOrders(em, entity, passengerKind);
    }

    private static bool IsBoardPassengerCandidate(EntityManager em, Entity entity)
    {
        return IsPlayerFaction(em, entity) &&
               (TransportBoardingCommandSystem.IsSoldierBoardingCandidate(em, entity) ||
                TransportBoardingCommandSystem.IsPotentialVehicleCargoPassenger(em, entity));
    }

    private static bool IsPlayerFaction(EntityManager em, Entity entity)
    {
        return em.Exists(entity) &&
               em.HasComponent<Faction>(entity) &&
               FactionIdentity.IsPlayerControlled(em.GetComponentData<Faction>(entity).Id);
    }

    private static int CountPendingBoardingOrders(EntityManager em, Entity transport, byte passengerKind)
    {
        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<UnitTransportBoardingTarget>());
        if (query.IsEmptyIgnoreFilter)
            return 0;

        int count = 0;
        ComponentTypeHandle<UnitTransportBoardingTarget> targetType =
            em.GetComponentTypeHandle<UnitTransportBoardingTarget>(true);
        using NativeArray<ArchetypeChunk> chunks = query.ToArchetypeChunkArray(Allocator.Temp);
        for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
        {
            NativeArray<UnitTransportBoardingTarget> targets = chunks[chunkIndex].GetNativeArray(ref targetType);
            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i].Transport == transport && ResolvePassengerKind(targets[i].PassengerKind) == passengerKind)
                    count++;
            }
        }

        return count;
    }

    private static byte ResolvePassengerKind(byte passengerKind)
    {
        return passengerKind == UnitTransportPassengerKind.Vehicle
            ? UnitTransportPassengerKind.Vehicle
            : UnitTransportPassengerKind.Soldier;
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

    private static void ApplyEnterBoardTargetMode(
        ref RtsSelectionInputStateComponent inputState,
        ref RuntimeGameplayStateComponent runtimeState,
        BoardCommandModeDirection direction,
        Entity transport,
        int currentFrame)
    {
        float2 pointer = inputState.HasLastKnownPointerPosition != 0
            ? inputState.LastKnownPointerPosition
            : float2.zero;
        ResetSelectionDragState(ref inputState, pointer);
        inputState.IgnoreNextLeftMouseRelease = 1;
        inputState.SkipNextWorldReleaseAfterSelection = 1;
        inputState.IgnoreWorldCommandsUntilFrame = currentFrame + 1;
        inputState.ActiveCommandMode = (int)TacticalCommandMode.Board;
        inputState.ActiveCommandModeFrame = currentFrame;
        inputState.ActiveCommandModeOneShot = 1;
        inputState.ActiveCommandModeRequiresWorldTarget = 1;
        inputState.ActiveBoardCommandDirection = (byte)direction;
        inputState.ActiveBoardTransport = direction == BoardCommandModeDirection.TransportToPassenger
            ? transport
            : Entity.Null;
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

    private struct BoardModeSource
    {
        public bool Accepted;
        public bool HasSelected;
        public bool HasSelectedBoardPassenger;
        public BoardCommandModeDirection Direction;
        public Entity Transport;
    }
}
