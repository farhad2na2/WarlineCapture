using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[DisableAutoCreation]
public partial struct RtsSelectionImmediateSelectedUnitCommandSystem : ISystem
{
    private EntityQuery _commandQueueQuery;
    private EntityQuery _runtimeStateQuery;
    private EntityQuery _respawnQueueQuery;
    private EntityQuery _selectedQuery;
    private EntityQuery _selectedMoveQuery;

    public void OnCreate(ref SystemState state)
    {
        _commandQueueQuery = state.GetEntityQuery(
            ComponentType.ReadWrite<RtsSelectionInputStateComponent>(),
            ComponentType.ReadWrite<RtsSelectionCommandIntentRequestElement>());
        _runtimeStateQuery = state.GetEntityQuery(ComponentType.ReadWrite<RuntimeGameplayStateComponent>());
        _respawnQueueQuery = state.GetEntityQuery(
            ComponentType.ReadOnly<RespawnQueueTag>(),
            ComponentType.ReadOnly<RespawnQueueComponent>());
        _selectedQuery = state.GetEntityQuery(ComponentType.ReadOnly<SelectedUnitTag>());
        _selectedMoveQuery = state.GetEntityQuery(
            ComponentType.ReadOnly<SelectedUnitTag>(),
            ComponentType.ReadOnly<UnitGrid>(),
            ComponentType.ReadOnly<UnitMove>());
    }

    public void OnUpdate(ref SystemState state)
    {
        ProcessPendingRequests(
            state.EntityManager,
            _commandQueueQuery,
            _runtimeStateQuery,
            _respawnQueueQuery,
            _selectedQuery,
            _selectedMoveQuery,
            Entity.Null,
            out _,
            out _,
            out _,
            out _);
    }

    public static bool ProcessPendingRequests(
        EntityManager em,
        out RtsSelectionCommandIntentKind processedKind,
        out bool accepted,
        out TacticalCommandReasonCode rejectionReason)
    {
        return ProcessPendingRequests(
            em,
            Entity.Null,
            out processedKind,
            out accepted,
            out rejectionReason,
            out _);
    }

    public static bool ProcessPendingRequests(
        EntityManager em,
        Entity focusedUnit,
        out RtsSelectionCommandIntentKind processedKind,
        out bool accepted,
        out TacticalCommandReasonCode rejectionReason,
        out int issuedCount)
    {
        using EntityQuery commandQueueQuery = em.CreateEntityQuery(
            ComponentType.ReadWrite<RtsSelectionInputStateComponent>(),
            ComponentType.ReadWrite<RtsSelectionCommandIntentRequestElement>());
        using EntityQuery runtimeStateQuery = em.CreateEntityQuery(ComponentType.ReadWrite<RuntimeGameplayStateComponent>());
        using EntityQuery respawnQueueQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<RespawnQueueTag>(),
            ComponentType.ReadOnly<RespawnQueueComponent>());
        using EntityQuery selectedQuery = em.CreateEntityQuery(ComponentType.ReadOnly<SelectedUnitTag>());
        using EntityQuery selectedMoveQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<SelectedUnitTag>(),
            ComponentType.ReadOnly<UnitGrid>(),
            ComponentType.ReadOnly<UnitMove>());

        return ProcessPendingRequests(
            em,
            commandQueueQuery,
            runtimeStateQuery,
            respawnQueueQuery,
            selectedQuery,
            selectedMoveQuery,
            focusedUnit,
            out processedKind,
            out accepted,
            out rejectionReason,
            out issuedCount);
    }

    private static bool ProcessPendingRequests(
        EntityManager em,
        EntityQuery commandQueueQuery,
        EntityQuery runtimeStateQuery,
        EntityQuery respawnQueueQuery,
        EntityQuery selectedQuery,
        EntityQuery selectedMoveQuery,
        Entity focusedUnit,
        out RtsSelectionCommandIntentKind processedKind,
        out bool accepted,
        out TacticalCommandReasonCode rejectionReason,
        out int issuedCount)
    {
        processedKind = RtsSelectionCommandIntentKind.None;
        accepted = false;
        rejectionReason = TacticalCommandReasonCode.None;
        issuedCount = 0;
        if (commandQueueQuery.IsEmptyIgnoreFilter || runtimeStateQuery.IsEmptyIgnoreFilter)
            return false;

        Entity commandEntity = commandQueueQuery.GetSingletonEntity();
        DynamicBuffer<RtsSelectionCommandIntentRequestElement> commandRequests =
            em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        if (!RemoveImmediateRequests(commandRequests, out processedKind))
            return false;

        if (processedKind == RtsSelectionCommandIntentKind.ReturnToBase)
        {
            return ProcessReturnToBase(
                em,
                commandEntity,
                runtimeStateQuery,
                respawnQueueQuery,
                selectedQuery,
                focusedUnit,
                out accepted,
                out rejectionReason,
                out issuedCount);
        }

        if (processedKind == RtsSelectionCommandIntentKind.DestroyFocusedUnit)
        {
            return ProcessDestroyFocusedUnit(
                em,
                selectedQuery,
                focusedUnit,
                out accepted,
                out rejectionReason,
                out issuedCount);
        }

        bool holdPosition = processedKind == RtsSelectionCommandIntentKind.HoldPosition;
        RtsSelectionInputStateComponent inputState = em.GetComponentData<RtsSelectionInputStateComponent>(commandEntity);
        ClearCommandMode(ref inputState);

        if (selectedMoveQuery.IsEmptyIgnoreFilter)
        {
            em.SetComponentData(commandEntity, inputState);
            rejectionReason = TacticalCommandReasonCode.NoSelection;
            return true;
        }

        using NativeArray<Entity> selectedEntities = selectedMoveQuery.ToEntityArray(Allocator.Temp);
        if (!ApplyImmediateSelectedUnitOrder(em, selectedEntities, holdPosition))
        {
            em.SetComponentData(commandEntity, inputState);
            rejectionReason = TacticalCommandReasonCode.NoSelection;
            return true;
        }

        commandRequests = em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        RemoveRequests(commandRequests, RtsSelectionCommandIntentKind.Move);
        ClearQueuedMoveOrder(ref inputState);
        inputState.IsDraggingSelection = 0;
        em.SetComponentData(commandEntity, inputState);

        Entity runtimeEntity = runtimeStateQuery.GetSingletonEntity();
        RuntimeGameplayStateComponent runtimeState = em.GetComponentData<RuntimeGameplayStateComponent>(runtimeEntity);
        runtimeState.SelectionModeActive = 0;
        runtimeState.SuppressNextWorldClick = 1;
        em.SetComponentData(runtimeEntity, runtimeState);

        accepted = true;
        issuedCount = selectedEntities.Length;
        return true;
    }

    private static bool ProcessReturnToBase(
        EntityManager em,
        Entity commandEntity,
        EntityQuery runtimeStateQuery,
        EntityQuery respawnQueueQuery,
        EntityQuery selectedQuery,
        Entity focusedUnit,
        out bool accepted,
        out TacticalCommandReasonCode rejectionReason,
        out int issuedCount)
    {
        accepted = false;
        rejectionReason = TacticalCommandReasonCode.None;
        issuedCount = 0;

        RtsSelectionInputStateComponent inputState = em.GetComponentData<RtsSelectionInputStateComponent>(commandEntity);
        ClearCommandMode(ref inputState);
        em.SetComponentData(commandEntity, inputState);

        if (respawnQueueQuery.IsEmptyIgnoreFilter)
        {
            rejectionReason = TacticalCommandReasonCode.NoSelection;
            ApplyReturnToBaseRuntimeState(em, runtimeStateQuery);
            return true;
        }

        Entity queueEntity = respawnQueueQuery.GetSingletonEntity();
        if (focusedUnit != Entity.Null &&
            em.Exists(focusedUnit) &&
            IsPlayerControlled(em, focusedUnit))
        {
            if (TryIssueReturnToBase(em, queueEntity, focusedUnit))
                issuedCount = 1;
        }
        else if (!selectedQuery.IsEmptyIgnoreFilter)
        {
            using NativeArray<Entity> selectedEntities = selectedQuery.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < selectedEntities.Length; i++)
            {
                Entity entity = selectedEntities[i];
                if (!em.Exists(entity) || !IsPlayerControlled(em, entity))
                    continue;

                if (TryIssueReturnToBase(em, queueEntity, entity))
                    issuedCount++;
            }
        }

        ApplyReturnToBaseRuntimeState(em, runtimeStateQuery);
        if (issuedCount <= 0)
        {
            rejectionReason = TacticalCommandReasonCode.NoSelection;
            return true;
        }

        accepted = true;
        return true;
    }

    private static bool ProcessDestroyFocusedUnit(
        EntityManager em,
        EntityQuery selectedQuery,
        Entity focusedUnit,
        out bool accepted,
        out TacticalCommandReasonCode rejectionReason,
        out int issuedCount)
    {
        accepted = false;
        rejectionReason = TacticalCommandReasonCode.None;
        issuedCount = 0;

        if (focusedUnit != Entity.Null && em.Exists(focusedUnit))
        {
            if (!IsPlayerControlled(em, focusedUnit))
            {
                rejectionReason = TacticalCommandReasonCode.TargetNotAttackable;
                return true;
            }

            DestroyUnit(em, focusedUnit);
            accepted = true;
            issuedCount = 1;
            return true;
        }

        if (!selectedQuery.IsEmptyIgnoreFilter)
        {
            using NativeArray<Entity> selectedEntities = selectedQuery.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < selectedEntities.Length; i++)
            {
                Entity entity = selectedEntities[i];
                if (!em.Exists(entity) || !IsPlayerControlled(em, entity))
                    continue;

                DestroyUnit(em, entity);
                issuedCount++;
            }
        }

        if (issuedCount <= 0)
        {
            rejectionReason = TacticalCommandReasonCode.NoSelection;
            return true;
        }

        accepted = true;
        return true;
    }

    private static bool ApplyImmediateSelectedUnitOrder(
        EntityManager em,
        NativeArray<Entity> selectedEntities,
        bool holdPosition)
    {
        bool issuedAny = false;
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        try
        {
            for (int i = 0; i < selectedEntities.Length; i++)
            {
                Entity entity = selectedEntities[i];
                if (!em.Exists(entity))
                    continue;

                ClearImmediateOrderComponents(em, ecb, entity, holdPosition);
                if (holdPosition)
                {
                    if (!em.HasComponent<HoldPositionOrderTag>(entity))
                        ecb.AddComponent<HoldPositionOrderTag>(entity);
                    if (em.HasComponent<UnitCombat>(entity))
                    {
                        UnitCombat combat = em.GetComponentData<UnitCombat>(entity);
                        if (combat.CanAttack != 0)
                        {
                            combat.AutoEngage = 1;
                            ecb.SetComponent(entity, combat);
                        }
                    }
                }
                else
                {
                    RemoveComponentIfPresent<HoldPositionOrderTag>(em, ecb, entity);
                    if (em.HasComponent<UnitCombat>(entity))
                    {
                        UnitCombat combat = em.GetComponentData<UnitCombat>(entity);
                        if (combat.CanAttack != 0)
                        {
                            combat.AutoEngage = 0;
                            ecb.SetComponent(entity, combat);
                        }
                    }
                }

                if (!em.HasComponent<ManualMoveOrderTag>(entity))
                    ecb.AddComponent<ManualMoveOrderTag>(entity);
                issuedAny = true;
            }

            ecb.Playback(em);
        }
        finally
        {
            ecb.Dispose();
        }

        return issuedAny;
    }

    private static bool TryIssueReturnToBase(EntityManager em, Entity queueEntity, Entity entity)
    {
        byte factionId = em.HasComponent<Faction>(entity) ? em.GetComponentData<Faction>(entity).Id : (byte)0;
        int2 goal = default;
        if (em.HasBuffer<RespawnFactionSpawnPoint>(queueEntity))
        {
            DynamicBuffer<RespawnFactionSpawnPoint> points = em.GetBuffer<RespawnFactionSpawnPoint>(queueEntity);
            for (int i = 0; i < points.Length; i++)
            {
                if (points[i].FactionId != factionId)
                    continue;

                goal = points[i].SpawnCell;
                break;
            }
        }

        UnitMoveOrderRequestSystem.EnqueueAndProcessImmediateMoveOrder(em, entity, goal);
        return true;
    }

    private static bool RemoveImmediateRequests(
        DynamicBuffer<RtsSelectionCommandIntentRequestElement> commandRequests,
        out RtsSelectionCommandIntentKind lastKind)
    {
        bool removedAny = false;
        lastKind = RtsSelectionCommandIntentKind.None;
        for (int i = 0; i < commandRequests.Length;)
        {
            RtsSelectionCommandIntentKind kind = commandRequests[i].Kind;
            if (kind != RtsSelectionCommandIntentKind.HoldPosition &&
                kind != RtsSelectionCommandIntentKind.Stop &&
                kind != RtsSelectionCommandIntentKind.ReturnToBase &&
                kind != RtsSelectionCommandIntentKind.DestroyFocusedUnit)
            {
                i++;
                continue;
            }

            lastKind = kind;
            commandRequests.RemoveAt(i);
            removedAny = true;
        }

        return removedAny;
    }

    private static void ClearImmediateOrderComponents(
        EntityManager em,
        EntityCommandBuffer ecb,
        Entity entity,
        bool holdPosition)
    {
        UnitMoveOrderRequestSystem.ClearMovementOrderComponents(em, ecb, entity);
        if (holdPosition)
            HoldRuntimeMotion(em, ecb, entity);
        else
            StopRuntimeMotion(em, ecb, entity);
    }

    private static void HoldRuntimeMotion(EntityManager em, EntityCommandBuffer ecb, Entity entity)
    {
        StopVehicleKinematics(em, ecb, entity);
    }

    private static void StopRuntimeMotion(EntityManager em, EntityCommandBuffer ecb, Entity entity)
    {
        StopVehicleKinematics(em, ecb, entity);

        if (!em.HasComponent<UnitAirComponent>(entity))
            return;

        UnitAirComponent airState = em.GetComponentData<UnitAirComponent>(entity);
        airState.ReturningHome = 0;
        airState.TakeoffRolling = 0;
        airState.LandingRolling = 0;
        airState.AttackRunActive = 0;
        airState.ReturnApproachInitialized = 0;
        ecb.SetComponent(entity, airState);
    }

    private static void StopVehicleKinematics(EntityManager em, EntityCommandBuffer ecb, Entity entity)
    {
        if (em.HasComponent<UnitVehicleKinematics>(entity))
        {
            UnitVehicleKinematics kinematics = em.GetComponentData<UnitVehicleKinematics>(entity);
            kinematics.CurrentSpeed = 0f;
            kinematics.StallSeconds = 0f;
            ecb.SetComponent(entity, kinematics);
        }
    }

    private static bool RemoveRequests(
        DynamicBuffer<RtsSelectionCommandIntentRequestElement> commandRequests,
        RtsSelectionCommandIntentKind kind)
    {
        bool removedAny = false;
        for (int i = commandRequests.Length - 1; i >= 0; i--)
        {
            if (commandRequests[i].Kind != kind)
                continue;

            commandRequests.RemoveAt(i);
            removedAny = true;
        }

        return removedAny;
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

    private static void ApplyReturnToBaseRuntimeState(EntityManager em, EntityQuery runtimeStateQuery)
    {
        if (runtimeStateQuery.IsEmptyIgnoreFilter)
            return;

        Entity runtimeEntity = runtimeStateQuery.GetSingletonEntity();
        RuntimeGameplayStateComponent runtimeState = em.GetComponentData<RuntimeGameplayStateComponent>(runtimeEntity);
        runtimeState.SuppressNextWorldClick = 1;
        em.SetComponentData(runtimeEntity, runtimeState);
    }

    private static void RemoveComponentIfPresent<T>(EntityManager em, EntityCommandBuffer ecb, Entity entity)
        where T : unmanaged, IComponentData
    {
        if (em.HasComponent<T>(entity))
            ecb.RemoveComponent<T>(entity);
    }

    private static void DestroyUnit(EntityManager em, Entity entity)
    {
        if (em.HasComponent<SelectedUnitTag>(entity))
            em.RemoveComponent<SelectedUnitTag>(entity);

        if (em.HasComponent<UnitHealth>(entity))
        {
            UnitHealth health = em.GetComponentData<UnitHealth>(entity);
            health.Current = 0;
            em.SetComponentData(entity, health);
            return;
        }

        em.DestroyEntity(entity);
    }

    private static bool IsPlayerControlled(EntityManager em, Entity entity)
    {
        return em.HasComponent<Faction>(entity) &&
               FactionIdentity.IsPlayerControlled(em.GetComponentData<Faction>(entity).Id);
    }
}
