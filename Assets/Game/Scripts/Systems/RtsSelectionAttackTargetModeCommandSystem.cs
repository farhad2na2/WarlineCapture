using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[DisableAutoCreation]
public partial struct RtsSelectionAttackTargetModeCommandSystem : ISystem
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
            out _);
    }

    public static bool ProcessPendingRequests(
        EntityManager em,
        int currentFrame,
        out bool accepted,
        out bool airDefenseAutoEngageOnly,
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
            out airDefenseAutoEngageOnly,
            out rejectionReason);
    }

    private static bool ProcessPendingRequests(
        EntityManager em,
        EntityQuery commandQueueQuery,
        EntityQuery runtimeStateQuery,
        EntityQuery selectedQuery,
        int currentFrame,
        out bool accepted,
        out bool airDefenseAutoEngageOnly,
        out TacticalCommandReasonCode rejectionReason)
    {
        accepted = false;
        airDefenseAutoEngageOnly = false;
        rejectionReason = TacticalCommandReasonCode.None;
        if (commandQueueQuery.IsEmptyIgnoreFilter || runtimeStateQuery.IsEmptyIgnoreFilter)
            return false;

        Entity commandEntity = commandQueueQuery.GetSingletonEntity();
        Entity runtimeEntity = runtimeStateQuery.GetSingletonEntity();
        DynamicBuffer<RtsSelectionCommandIntentRequestElement> commandRequests =
            em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        if (!HasRequest(commandRequests, RtsSelectionCommandIntentKind.EnterAttackTargetMode))
            return false;

        for (int i = 0; i < commandRequests.Length;)
        {
            RtsSelectionCommandIntentKind kind = commandRequests[i].Kind;
            if (kind == RtsSelectionCommandIntentKind.Move ||
                kind == RtsSelectionCommandIntentKind.EnterAttackTargetMode)
            {
                commandRequests.RemoveAt(i);
                continue;
            }

            i++;
        }

        RtsSelectionInputStateComponent inputState = em.GetComponentData<RtsSelectionInputStateComponent>(commandEntity);
        ClearQueuedMoveOrder(ref inputState);
        AttackModeSelectionState attackSelection = ResolveSelectedAttackModeState(em, selectedQuery);
        if (attackSelection.HasOnlyAirDefenseLauncher)
        {
            ClearCommandMode(ref inputState);
            em.SetComponentData(commandEntity, inputState);
            airDefenseAutoEngageOnly = true;
            return true;
        }

        if (!attackSelection.HasNonAirDefenseAttackSource)
        {
            ClearCommandMode(ref inputState);
            em.SetComponentData(commandEntity, inputState);
            rejectionReason = attackSelection.HasSelected
                ? TacticalCommandReasonCode.TargetNotAttackable
                : TacticalCommandReasonCode.NoSelection;
            return true;
        }

        RuntimeGameplayStateComponent runtimeState = em.GetComponentData<RuntimeGameplayStateComponent>(runtimeEntity);
        ApplyEnterAttackTargetMode(ref inputState, ref runtimeState, currentFrame);
        em.SetComponentData(commandEntity, inputState);
        em.SetComponentData(runtimeEntity, runtimeState);
        accepted = true;
        return true;
    }

    private static AttackModeSelectionState ResolveSelectedAttackModeState(EntityManager em, EntityQuery selectedQuery)
    {
        AttackModeSelectionState selection = default;
        if (selectedQuery.IsEmptyIgnoreFilter)
            return selection;

        using NativeArray<Entity> selectedEntities = selectedQuery.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < selectedEntities.Length; i++)
            IncludeSelectedAttackModeCandidate(em, selectedEntities[i], ref selection);

        return selection;
    }

    private static void IncludeSelectedAttackModeCandidate(
        EntityManager em,
        Entity entity,
        ref AttackModeSelectionState selection)
    {
        if (entity == Entity.Null || !em.Exists(entity))
            return;

        selection.HasSelected = true;
        if (IsAirDefenseLauncher(em, entity))
        {
            selection.HasAirDefenseLauncher = true;
            return;
        }

        if (IsSelectedAttackCapableUnit(em, entity))
            selection.HasNonAirDefenseAttackSource = true;
    }

    private static bool IsSelectedAttackCapableUnit(EntityManager em, Entity entity)
    {
        if (IsAirDefenseLauncher(em, entity))
            return false;

        if (!em.HasComponent<Faction>(entity) ||
            !FactionIdentitySystem.IsPlayerControlled(em.GetComponentData<Faction>(entity).Id) ||
            !em.HasComponent<UnitMove>(entity) ||
            !em.HasComponent<UnitCombat>(entity) ||
            !em.HasComponent<UnitAttack>(entity) ||
            !em.HasComponent<LocalTransform>(entity) ||
            em.GetComponentData<UnitCombat>(entity).CanAttack == 0)
        {
            return false;
        }

        return !em.HasComponent<UnitHealth>(entity) ||
               em.GetComponentData<UnitHealth>(entity).Current > 0;
    }

    private static bool IsAirDefenseLauncher(EntityManager em, Entity entity)
    {
        if (!em.HasComponent<AirMissileLauncherComponent>(entity))
            return false;

        if (em.HasComponent<Faction>(entity) &&
            !FactionIdentitySystem.IsPlayerControlled(em.GetComponentData<Faction>(entity).Id))
        {
            return false;
        }

        return !em.HasComponent<UnitHealth>(entity) ||
               em.GetComponentData<UnitHealth>(entity).Current > 0;
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

    private static void ApplyEnterAttackTargetMode(
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
            TacticalCommandMode.Attack,
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

    private struct AttackModeSelectionState
    {
        public bool HasSelected;
        public bool HasAirDefenseLauncher;
        public bool HasNonAirDefenseAttackSource;

        public readonly bool HasOnlyAirDefenseLauncher =>
            HasSelected &&
            HasAirDefenseLauncher &&
            !HasNonAirDefenseAttackSource;
    }
}
