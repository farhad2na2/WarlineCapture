using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public sealed class RtsSelectionPointerTargetCommandSystem
{
    private const float UnitClickScreenFallbackRadiusPixels = 54f;
    private const int TraversableTargetSearchRadius = 24;

    public delegate bool TryGetEntityManagerDelegate(out EntityManager em);
    public delegate bool TryGetPointerPositionDelegate(out Vector2 pointerPosition);

    public struct Context
    {
        public RuntimeGameplayStateSystem RuntimeGameplayStateSystem;
        public readonly RtsSelectionInputSystem InputSystem;
        public readonly SelectionStateSystem SelectionStateSystem;
        public readonly FocusedUnitLifecycleSystem FocusedUnitLifecycleSystem;
        public readonly FocusableUnitLookupSystem FocusableUnitLookupSystem;
        public readonly SelectionUiReadModelLookup SelectionUiReadModelLookup;
        public readonly VisibleUnitSelectionSystem VisibleUnitSelectionSystem;
        public readonly TransportBoardingCommandSystem TransportBoardingCommandSystem;
        public readonly UnitTransportCapacitySystem UnitTransportCapacitySystem;
        public readonly UnitTransportAirPickupSystem UnitTransportAirPickupSystem;
        public readonly BuildingTargetMoveOrderSystem BuildingTargetMoveOrderSystem;
        public readonly BuildingPlacementInteractionSystem BuildingPlacementInteractionSystem;
        public readonly BuildingPlacementInteractionSystem.Context BuildingPlacementInteractionContext;
        public readonly Camera WorldCamera;
        public readonly TryGetEntityManagerDelegate TryGetEntityManager;
        public readonly TryGetPointerPositionDelegate TryGetPointerPosition;
        public readonly Func<bool> GetExplicitAttackTargetModeActive;
        public readonly Action<bool> SetExplicitAttackTargetModeActive;
        public readonly Action<TacticalCommandMode> ApplyHudCommandMode;
        public readonly Action<TacticalCommandResult> ApplyHudCommandResult;
        public readonly Action ClearHudSelection;
        public readonly Action ClearHudCommandMode;
        public readonly Action<EntityManager, Entity> ApplyHudSelection;
        public readonly Action<EntityManager, string> ClearCurrentSelection;
        public readonly Action<Vector2> RequestMoveOrderScreenMarker;
        public readonly Action<bool> SetCameraDragging;
        public readonly Func<bool> ProcessAttackCommandRequests;
        public readonly Func<bool> ProcessScanCommandRequests;
        public readonly Func<bool> ProcessTransportCommandRequests;
        public readonly Action ProcessMoveCommandRequests;
        public readonly Action<string> LogSelectionDiagnostic;
        public readonly FocusedUnitLifecycleSystem.DescribeEntityDelegate DescribeEntity;
        public readonly List<Entity> VisibleSelectionScratch;

        public Context(
            RuntimeGameplayStateSystem runtimeGameplayStateSystem,
            RtsSelectionInputSystem inputSystem,
            SelectionStateSystem selectionStateSystem,
            FocusedUnitLifecycleSystem focusedUnitLifecycleSystem,
            FocusableUnitLookupSystem focusableUnitLookupSystem,
            TransportBoardingCommandSystem transportBoardingCommandSystem,
            UnitTransportCapacitySystem unitTransportCapacitySystem,
            UnitTransportAirPickupSystem unitTransportAirPickupSystem,
            BuildingTargetMoveOrderSystem buildingTargetMoveOrderSystem,
            BuildingPlacementInteractionSystem buildingPlacementInteractionSystem,
            BuildingPlacementInteractionSystem.Context buildingPlacementInteractionContext,
            Camera worldCamera,
            TryGetEntityManagerDelegate tryGetEntityManager,
            TryGetPointerPositionDelegate tryGetPointerPosition,
            Func<bool> getExplicitAttackTargetModeActive,
            Action<bool> setExplicitAttackTargetModeActive,
            Action<TacticalCommandMode> applyHudCommandMode,
            Action<TacticalCommandResult> applyHudCommandResult,
            Action clearHudSelection,
            Action clearHudCommandMode,
            Action<EntityManager, Entity> applyHudSelection,
            Action<EntityManager, string> clearCurrentSelection,
            Action<Vector2> requestMoveOrderScreenMarker,
            Action<bool> setCameraDragging,
            Func<bool> processAttackCommandRequests,
            Func<bool> processScanCommandRequests,
            Func<bool> processTransportCommandRequests,
            Action processMoveCommandRequests,
            Action<string> logSelectionDiagnostic,
            FocusedUnitLifecycleSystem.DescribeEntityDelegate describeEntity,
            SelectionUiReadModelLookup selectionUiReadModelLookup = null,
            VisibleUnitSelectionSystem visibleUnitSelectionSystem = null,
            List<Entity> visibleSelectionScratch = null)
        {
            RuntimeGameplayStateSystem = runtimeGameplayStateSystem;
            InputSystem = inputSystem;
            SelectionStateSystem = selectionStateSystem;
            FocusedUnitLifecycleSystem = focusedUnitLifecycleSystem;
            FocusableUnitLookupSystem = focusableUnitLookupSystem;
            SelectionUiReadModelLookup = selectionUiReadModelLookup;
            VisibleUnitSelectionSystem = visibleUnitSelectionSystem;
            TransportBoardingCommandSystem = transportBoardingCommandSystem;
            UnitTransportCapacitySystem = unitTransportCapacitySystem;
            UnitTransportAirPickupSystem = unitTransportAirPickupSystem;
            BuildingTargetMoveOrderSystem = buildingTargetMoveOrderSystem;
            BuildingPlacementInteractionSystem = buildingPlacementInteractionSystem;
            BuildingPlacementInteractionContext = buildingPlacementInteractionContext;
            WorldCamera = worldCamera;
            TryGetEntityManager = tryGetEntityManager;
            TryGetPointerPosition = tryGetPointerPosition;
            GetExplicitAttackTargetModeActive = getExplicitAttackTargetModeActive;
            SetExplicitAttackTargetModeActive = setExplicitAttackTargetModeActive;
            ApplyHudCommandMode = applyHudCommandMode;
            ApplyHudCommandResult = applyHudCommandResult;
            ClearHudSelection = clearHudSelection;
            ClearHudCommandMode = clearHudCommandMode;
            ApplyHudSelection = applyHudSelection;
            ClearCurrentSelection = clearCurrentSelection;
            RequestMoveOrderScreenMarker = requestMoveOrderScreenMarker;
            SetCameraDragging = setCameraDragging;
            ProcessAttackCommandRequests = processAttackCommandRequests;
            ProcessScanCommandRequests = processScanCommandRequests;
            ProcessTransportCommandRequests = processTransportCommandRequests;
            ProcessMoveCommandRequests = processMoveCommandRequests;
            LogSelectionDiagnostic = logSelectionDiagnostic;
            DescribeEntity = describeEntity;
            VisibleSelectionScratch = visibleSelectionScratch;
        }
    }

    private Unity.Entities.World _queryWorld;
    private EntityQuery _gridConfigQuery;
    private EntityQuery _mapSurfaceQuery;
    private EntityQuery _runtimeBuildingCombatQuery;
    private readonly MapSurfaceSampler _mapSurfaceQuerySystem = new();
    private readonly MapSurfaceSlopeClassifier _mapSurfaceSlopeClassificationSystem = new();
    private readonly MapSurfacePathfindingSnapshot _mapSurfaceReadSystem = new();
    private readonly UnitMoveOrderSystem _mapSurfaceMoveOrderSystem = new();
    private readonly List<Entity> _mapSurfaceSelectedMoveEntities = new();

    public readonly struct MapSurfaceCommandTargetResult
    {
        public readonly int2 Cell;
        public readonly Vector3 WorldPoint;
        public readonly MapSurfaceSample Surface;
        public readonly bool HasSurface;

        private MapSurfaceCommandTargetResult(int2 cell, Vector3 worldPoint, MapSurfaceSample surface, bool hasSurface)
        {
            Cell = cell;
            WorldPoint = worldPoint;
            Surface = surface;
            HasSurface = hasSurface;
        }

        public static MapSurfaceCommandTargetResult FlatFallback(int2 cell, Vector3 worldPoint)
        {
            return new MapSurfaceCommandTargetResult(cell, worldPoint, default, false);
        }

        public static MapSurfaceCommandTargetResult SurfaceHit(int2 cell, Vector3 worldPoint, MapSurfaceSample surface)
        {
            return new MapSurfaceCommandTargetResult(cell, worldPoint, surface, true);
        }
    }

    private readonly struct PointerTargetBoundaryPass
    {
        private readonly RtsSelectionPointerTargetCommandSystem _owner;
        private readonly Context _context;

        public PointerTargetBoundaryPass(RtsSelectionPointerTargetCommandSystem owner, Context context)
        {
            _owner = owner;
            _context = context;
        }

        public bool TryGetClickedUnitEntity(Vector2 screenPosition, EntityManager em, out Entity bestEntity)
        {
            return _owner.TryGetClickedUnitEntityFromBoundary(_context, screenPosition, em, out bestEntity);
        }

        public bool TryGetClickedAttackTargetEntity(Vector2 screenPosition, EntityManager em, out Entity bestEntity)
        {
            return _owner.TryGetClickedAttackTargetEntityFromBoundary(_context, screenPosition, em, out bestEntity);
        }

        public bool TryGetClickedCell(Vector2 screenPosition, EntityManager em, out int2 cell, out Vector3 worldPoint)
        {
            return _owner.TryGetClickedCellFromBoundary(_context, screenPosition, em, out cell, out worldPoint);
        }

        public bool TryGetMoveCommandCell(Vector2 screenPosition, EntityManager em, out int2 cell, out Vector3 worldPoint)
        {
            return _owner.TryGetMoveCommandCellFromBoundary(_context, screenPosition, em, out cell, out worldPoint);
        }
    }

    private PointerTargetBoundaryPass CreatePointerTargetBoundaryPass(Context context)
    {
        return new PointerTargetBoundaryPass(this, context);
    }

    public void RequestMoveOrder(Context context, Vector2 screenPosition)
    {
        if (SelectionRuntimeDiagnosticsSystem.EnableMoveCommandTrace)
        {
            SelectionRuntimeDiagnosticsSystem.LogMoveCommandTrace(
                $"issueMoveOrderEnter screen={screenPosition} frame={UnityEngine.Time.frameCount} " +
                $"hasInput={context.InputSystem != null} hasProcess={context.ProcessMoveCommandRequests != null}");
        }

        context.SetExplicitAttackTargetModeActive?.Invoke(false);
        context.ApplyHudCommandMode?.Invoke(TacticalCommandMode.Move);
        context.LogSelectionDiagnostic?.Invoke($"moveAttempt pos={screenPosition} frame={UnityEngine.Time.frameCount}");

        bool queued = TryQueueResolvedMoveCommand(context, screenPosition, out bool queuedResolvedTarget);
        if (!queued)
        {
            if (SelectionRuntimeDiagnosticsSystem.EnableMoveCommandTrace)
            {
                SelectionRuntimeDiagnosticsSystem.LogMoveCommandTrace(
                    $"issueMoveOrderQueueFailed screen={screenPosition} frame={UnityEngine.Time.frameCount}");
            }

            context.LogSelectionDiagnostic?.Invoke($"moveAttempt result=False reason=QueueFailed pos={screenPosition} frame={UnityEngine.Time.frameCount}");
            context.ClearHudCommandMode?.Invoke();
            context.InputSystem.ClearActiveCommandMode();
            context.ApplyHudCommandResult?.Invoke(TacticalCommandResult.Rejected(TacticalCommandReasonCode.NoSelection));
            return;
        }

        if (SelectionRuntimeDiagnosticsSystem.EnableMoveCommandTrace)
            SelectionRuntimeDiagnosticsSystem.LogMoveCommandTrace($"issueMoveOrderQueued screen={screenPosition} frame={UnityEngine.Time.frameCount}");
        if (!queuedResolvedTarget)
        {
            context.ProcessMoveCommandRequests?.Invoke();
            if (SelectionRuntimeDiagnosticsSystem.EnableMoveCommandTrace)
                SelectionRuntimeDiagnosticsSystem.LogMoveCommandTrace($"issueMoveOrderProcessReturned screen={screenPosition} frame={UnityEngine.Time.frameCount}");
        }
    }

    private bool TryQueueResolvedMoveCommand(Context context, Vector2 screenPosition, out bool queuedResolvedTarget)
    {
        queuedResolvedTarget = false;
        int frame = UnityEngine.Time.frameCount;
        if (context.TryGetEntityManager == null ||
            !context.TryGetEntityManager(out EntityManager em))
        {
            return context.InputSystem.QueueMoveCommandRequest(screenPosition, frame);
        }

        PointerTargetBoundaryPass targetBoundary = CreatePointerTargetBoundaryPass(context);
        if (targetBoundary.TryGetClickedUnitEntity(screenPosition, em, out _))
            return context.InputSystem.QueueMoveCommandRequest(screenPosition, frame);

        if (!targetBoundary.TryGetMoveCommandCell(screenPosition, em, out int2 targetCell, out Vector3 worldPoint))
            return context.InputSystem.QueueMoveCommandRequest(screenPosition, frame);

        queuedResolvedTarget = context.InputSystem.QueueMoveCommandRequest(screenPosition, targetCell, worldPoint, frame);
        return queuedResolvedTarget;
    }

    public bool TryRequestAttackOrderToClickedUnit(Context context, Vector2 screenPosition)
    {
        bool explicitAttackTargetModeActive = context.GetExplicitAttackTargetModeActive?.Invoke() == true;
        if (!TryQueueResolvedAttackCommand(
                context,
                screenPosition,
                explicitAttackTargetModeActive,
                out bool queuedResolvedTarget))
        {
            if (explicitAttackTargetModeActive)
                context.ApplyHudCommandResult?.Invoke(TacticalCommandResult.Rejected(TacticalCommandReasonCode.NoSelection));
            return false;
        }

        if (queuedResolvedTarget)
            return true;

        return context.ProcessAttackCommandRequests?.Invoke() == true;
    }

    private bool TryQueueResolvedAttackCommand(
        Context context,
        Vector2 screenPosition,
        bool explicitAttackTargetModeActive,
        out bool queuedResolvedTarget)
    {
        queuedResolvedTarget = false;
        int frame = UnityEngine.Time.frameCount;
        if (context.TryGetEntityManager == null ||
            !context.TryGetEntityManager(out EntityManager em))
        {
            return context.InputSystem.QueueAttackCommandRequest(screenPosition, explicitAttackTargetModeActive, frame);
        }

        PointerTargetBoundaryPass targetBoundary = CreatePointerTargetBoundaryPass(context);
        if (!targetBoundary.TryGetClickedUnitEntity(screenPosition, em, out Entity targetEntity) ||
            !IsDirectResolvedAttackTarget(em, targetEntity))
        {
            return context.InputSystem.QueueAttackCommandRequest(screenPosition, explicitAttackTargetModeActive, frame);
        }

        queuedResolvedTarget = context.InputSystem.QueueAttackCommandRequest(
            screenPosition,
            targetEntity,
            explicitAttackTargetModeActive,
            frame);
        return queuedResolvedTarget;
    }

    private static bool IsDirectResolvedAttackTarget(
        EntityManager em,
        Entity targetEntity)
    {
        if (targetEntity == Entity.Null ||
            !em.Exists(targetEntity) ||
            em.HasComponent<RuntimeBuildingCombatTag>(targetEntity) ||
            em.HasComponent<RuntimeBuildingCombatInfo>(targetEntity) ||
            em.HasComponent<StaticGridBlocker>(targetEntity) ||
            !em.HasComponent<Faction>(targetEntity) ||
            !em.HasComponent<LocalTransform>(targetEntity))
        {
            return false;
        }

        if (!FactionIdentity.IsHostileToPlayer(em.GetComponentData<Faction>(targetEntity).Id))
            return false;

        return !em.HasComponent<UnitHealth>(targetEntity) ||
               em.GetComponentData<UnitHealth>(targetEntity).Current > 0;
    }

    public bool TryRequestScanOrder(Context context, Vector2 screenPosition)
    {
        context.SetExplicitAttackTargetModeActive?.Invoke(false);
        context.ApplyHudCommandMode?.Invoke(TacticalCommandMode.Scan);
        context.LogSelectionDiagnostic?.Invoke($"scanAttempt pos={screenPosition} frame={UnityEngine.Time.frameCount}");
        SelectionRuntimeDiagnosticsSystem.LogScanCommandTrace(
            $"scanTargetTap pos={screenPosition} frame={UnityEngine.Time.frameCount}");

        bool queued = TryQueueResolvedScanCommand(context, screenPosition, out bool queuedResolvedTarget);
        if (!queued)
        {
            context.LogSelectionDiagnostic?.Invoke($"scanAttempt result=False reason=QueueFailed pos={screenPosition} frame={UnityEngine.Time.frameCount}");
            SelectionRuntimeDiagnosticsSystem.LogScanCommandTrace(
                $"scanTargetTapQueued result=False reason=QueueFailed pos={screenPosition} frame={UnityEngine.Time.frameCount}");
            context.ApplyHudCommandResult?.Invoke(TacticalCommandResult.Rejected(TacticalCommandReasonCode.ScanUnavailable));
            return false;
        }

        SelectionRuntimeDiagnosticsSystem.LogScanCommandTrace(
            $"scanTargetTapQueued result=True resolvedTarget={queuedResolvedTarget} pos={screenPosition} frame={UnityEngine.Time.frameCount}");

        if (!queuedResolvedTarget)
        {
            bool processed = context.ProcessScanCommandRequests?.Invoke() == true;
            SelectionRuntimeDiagnosticsSystem.LogScanCommandTrace(
                $"scanTargetTapProcessedImmediately result={processed} pos={screenPosition} frame={UnityEngine.Time.frameCount}");
            return processed;
        }

        return true;
    }

    private bool TryQueueResolvedScanCommand(Context context, Vector2 screenPosition, out bool queuedResolvedTarget)
    {
        queuedResolvedTarget = false;
        int frame = UnityEngine.Time.frameCount;
        if (context.TryGetEntityManager == null ||
            !context.TryGetEntityManager(out EntityManager em))
        {
            SelectionRuntimeDiagnosticsSystem.LogScanCommandTrace(
                $"scanTargetResolveSkipped reason=NoEntityManager pos={screenPosition} frame={frame}");
            return context.InputSystem.QueueScanCommandRequest(screenPosition, frame);
        }

        PointerTargetBoundaryPass targetBoundary = CreatePointerTargetBoundaryPass(context);
        if (!targetBoundary.TryGetClickedCell(screenPosition, em, out int2 targetCell, out Vector3 worldPoint))
        {
            SelectionRuntimeDiagnosticsSystem.LogScanCommandTrace(
                $"scanTargetResolveSkipped reason=NoClickedCell pos={screenPosition} frame={frame}");
            return context.InputSystem.QueueScanCommandRequest(screenPosition, frame);
        }

        queuedResolvedTarget = context.InputSystem.QueueScanCommandRequest(screenPosition, targetCell, worldPoint, frame);
        SelectionRuntimeDiagnosticsSystem.LogScanCommandTrace(
            $"scanTargetResolved queued={queuedResolvedTarget} cell={targetCell} world={worldPoint} pos={screenPosition} frame={frame}");
        return queuedResolvedTarget;
    }

    public bool TryRequestBoardTransportOrderToClickedUnit(Context context, Vector2 screenPosition)
    {
        if (!TryQueueResolvedBoardTransportCommand(context, screenPosition, out bool queuedResolvedTarget))
            return false;

        if (!queuedResolvedTarget)
            return context.ProcessTransportCommandRequests?.Invoke() == true;

        return true;
    }

    private bool TryQueueResolvedBoardTransportCommand(Context context, Vector2 screenPosition, out bool queuedResolvedTarget)
    {
        queuedResolvedTarget = false;
        int frame = UnityEngine.Time.frameCount;
        if (context.TryGetEntityManager == null ||
            !context.TryGetEntityManager(out EntityManager em))
        {
            return context.InputSystem.QueueBoardTransportCommandRequest(screenPosition, frame);
        }

        PointerTargetBoundaryPass targetBoundary = CreatePointerTargetBoundaryPass(context);
        if (!context.TransportBoardingCommandSystem.TryResolveBoardablePlayerTransportClick(
                em,
                screenPosition,
                (Vector2 position, EntityManager entityManager, out Entity entity) => targetBoundary.TryGetClickedUnitEntity(position, entityManager, out entity),
                (Vector2 position, EntityManager entityManager, out int2 cell, out Vector3 worldPoint) => targetBoundary.TryGetClickedCell(position, entityManager, out cell, out worldPoint),
                out Entity transport))
        {
            return context.InputSystem.QueueBoardTransportCommandRequest(screenPosition, frame);
        }

        queuedResolvedTarget = context.InputSystem.QueueBoardTransportCommandRequest(transport, screenPosition, frame);
        return queuedResolvedTarget;
    }

    public bool IsBoardablePlayerTransportClick(Context context, Vector2 screenPosition)
    {
        if (!context.TryGetEntityManager(out EntityManager em))
            return false;

        PointerTargetBoundaryPass targetBoundary = CreatePointerTargetBoundaryPass(context);
        return context.TransportBoardingCommandSystem.IsBoardablePlayerTransportClick(
            em,
            screenPosition,
            (Vector2 position, EntityManager entityManager, out Entity entity) => targetBoundary.TryGetClickedUnitEntity(position, entityManager, out entity),
            (Vector2 position, EntityManager entityManager, out int2 cell, out Vector3 worldPoint) => targetBoundary.TryGetClickedCell(position, entityManager, out cell, out worldPoint));
    }

    public bool TryRequestBoardSelectedTransportOrderToClickedUnit(Context context, Entity transport, Vector2 screenPosition)
    {
        if (context.InputSystem == null ||
            !context.InputSystem.QueueBoardSelectedTransportCommandRequest(transport, screenPosition, UnityEngine.Time.frameCount))
        {
            return false;
        }

        return context.ProcessTransportCommandRequests?.Invoke() == true;
    }

    public bool TryRequestBoardSelectedTransportOrdersToPassengerRect(Context context, Entity transport, Rect screenRect)
    {
        if (context.VisibleUnitSelectionSystem == null ||
            context.SelectionUiReadModelLookup == null ||
            context.VisibleSelectionScratch == null ||
            context.WorldCamera == null ||
            context.InputSystem == null ||
            context.TryGetEntityManager == null ||
            !context.TryGetEntityManager(out EntityManager em))
        {
            return false;
        }

        context.VisibleUnitSelectionSystem.CollectVisiblePlayerUnits(
            em,
            context.WorldCamera,
            context.SelectionUiReadModelLookup,
            screenRect,
            VisibleUnitSelectionSystem.Filter.Soldiers,
            context.VisibleSelectionScratch);

        int queued = 0;
        for (int i = 0; i < context.VisibleSelectionScratch.Count; i++)
        {
            Entity passenger = context.VisibleSelectionScratch[i];
            if (!IsValidBoardPassengerPreviewTarget(context, em, transport, passenger))
                continue;

            if (context.InputSystem.QueueBoardSelectedTransportPassengerCommandRequest(transport, passenger, screenRect, UnityEngine.Time.frameCount))
                queued++;
        }

        if (queued <= 0)
        {
            context.ApplyHudCommandResult?.Invoke(
                TacticalCommandResult.Rejected(TacticalCommandReasonCode.CommandUnavailable, "Tap units to board."));
            return false;
        }

        return context.ProcessTransportCommandRequests?.Invoke() == true;
    }

    public bool IsBoardSelectedTransportPassengerTarget(Context context, Entity transport, Vector2 screenPosition)
    {
        if (context.TryGetEntityManager == null ||
            !context.TryGetEntityManager(out EntityManager em))
        {
            return false;
        }

        return TryGetClickedUnitEntity(context, screenPosition, em, out Entity passenger) &&
               IsValidBoardPassengerPreviewTarget(context, em, transport, passenger);
    }

    public bool IsValidBoardTransportPreviewTarget(Context context, EntityManager em, Entity source, Entity target)
    {
        if (!TransportBoardingCommandSystem.TryResolveBoardingPassengerKind(em, target, source, out byte passengerKind, out _))
            return false;

        return IsBoardTransportWithAvailableSlots(em, target, passengerKind);
    }

    public bool IsValidBoardPassengerPreviewTarget(Context context, EntityManager em, Entity transport, Entity passenger)
    {
        if (transport == Entity.Null ||
            passenger == Entity.Null ||
            transport == passenger ||
            !IsBoardCommandAvailable(context, em, transport))
        {
            return false;
        }

        return TransportBoardingCommandSystem.IsBoardingCandidateForTransport(em, transport, passenger) &&
               TransportBoardingCommandSystem.IsWithinTransportBoardingCommandRange(em, transport, passenger) &&
               TransportBoardingCommandSystem.TryResolveBoardingPassengerKind(em, transport, passenger, out byte passengerKind, out _) &&
               IsBoardTransportWithAvailableSlots(em, transport, passengerKind);
    }

    public bool IsBoardCommandAvailable(Context context, EntityManager em, Entity entity)
    {
        if (!IsOwnedByPlayer(em, entity))
            return false;

        if (TransportBoardingCommandSystem.IsSoldierBoardingCandidate(em, entity))
            return true;

        if (TransportBoardingCommandSystem.IsPotentialVehicleCargoPassenger(em, entity))
            return true;

        return IsBoardTransportWithAvailableSeats(em, entity);
    }

    public bool HasSelectedBoardAction(Context context, EntityManager em)
    {
        if (context.FocusedUnitLifecycleSystem != null &&
            context.SelectionStateSystem != null &&
            context.FocusedUnitLifecycleSystem.TryGetFocusedUnitEntity(
                em,
                context.SelectionStateSystem,
                out Entity focusedUnit) &&
            em.Exists(focusedUnit) &&
            TransportBoardingCommandSystem.IsBoardablePlayerTransport(em, focusedUnit) &&
            IsBoardCommandAvailable(context, em, focusedUnit))
        {
            return true;
        }

        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<SelectedUnitTag>());
        if (query.IsEmptyIgnoreFilter)
            return false;

        EntityTypeHandle entityType = em.GetEntityTypeHandle();
        using NativeArray<ArchetypeChunk> chunks = query.ToArchetypeChunkArray(Allocator.Temp);
        for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
        {
            NativeArray<Entity> entities = chunks[chunkIndex].GetNativeArray(entityType);
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                if (!em.Exists(entity))
                    continue;

                if (TransportBoardingCommandSystem.IsSoldierBoardingCandidate(em, entity))
                    return true;

                if (TransportBoardingCommandSystem.IsPotentialVehicleCargoPassenger(em, entity))
                    return true;

                if (TransportBoardingCommandSystem.IsBoardablePlayerTransport(em, entity) &&
                    IsBoardCommandAvailable(context, em, entity))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool IsBoardTransportWithAvailableSeats(EntityManager em, Entity entity)
    {
        if (!IsOwnedByPlayer(em, entity))
            return false;

        if (!TransportBoardingCommandSystem.IsBoardablePlayerTransport(em, entity))
            return false;

        return IsBoardTransportWithAvailableSlots(em, entity, UnitTransportPassengerKind.Soldier) ||
               IsBoardTransportWithAvailableSlots(em, entity, UnitTransportPassengerKind.Vehicle);
    }

    private bool IsBoardTransportWithAvailableSlots(EntityManager em, Entity entity, byte passengerKind)
    {
        if (!IsOwnedByPlayer(em, entity))
            return false;

        if (!TransportBoardingCommandSystem.IsBoardablePlayerTransport(em, entity))
            return false;

        return TransportBoardingCommandSystem.HasAvailableTransportBoardingSlot(
                   em,
                   entity,
                   passengerKind,
                   out int occupied,
                   out int capacity) &&
               capacity > occupied + CountPendingBoardingOrders(em, entity, passengerKind);
    }

    private static bool IsOwnedByPlayer(EntityManager em, Entity entity)
    {
        return entity != Entity.Null &&
               em.Exists(entity) &&
               em.HasComponent<Faction>(entity) &&
               FactionIdentity.IsPlayerControlled(em.GetComponentData<Faction>(entity).Id);
    }

    private static int CountPendingBoardingOrders(EntityManager em, Entity transport)
    {
        return CountPendingBoardingOrders(em, transport, UnitTransportPassengerKind.Soldier) +
               CountPendingBoardingOrders(em, transport, UnitTransportPassengerKind.Vehicle);
    }

    private static int CountPendingBoardingOrders(EntityManager em, Entity transport, byte passengerKind)
    {
        int count = 0;
        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<UnitTransportBoardingTarget>());
        if (query.IsEmptyIgnoreFilter)
            return 0;

        ComponentTypeHandle<UnitTransportBoardingTarget> targetType = em.GetComponentTypeHandle<UnitTransportBoardingTarget>(true);
        using NativeArray<ArchetypeChunk> chunks = query.ToArchetypeChunkArray(Allocator.Temp);
        for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
        {
            NativeArray<UnitTransportBoardingTarget> targets = chunks[chunkIndex].GetNativeArray(ref targetType);
            for (int i = 0; i < targets.Length; i++)
                if (targets[i].Transport == transport && ResolvePassengerKind(targets[i].PassengerKind) == passengerKind)
                    count++;
        }

        return count;
    }

    private static byte ResolvePassengerKind(byte passengerKind)
    {
        return passengerKind == UnitTransportPassengerKind.Vehicle
            ? UnitTransportPassengerKind.Vehicle
            : UnitTransportPassengerKind.Soldier;
    }

    public bool TryRequestMoveOrderToBuilding(Context context, Vector2Int originCell, Vector2Int footprintCells)
    {
        if (!context.TryGetEntityManager(out EntityManager em))
            return false;

        bool issued = context.BuildingTargetMoveOrderSystem.TryRequestMoveOrderToBuilding(
            em,
            new int2(originCell.x, originCell.y),
            new int2(footprintCells.x, footprintCells.y));
        if (!issued)
            return false;

        context.ClearCurrentSelection?.Invoke(em, "MoveOrderToBuilding");
        context.FocusedUnitLifecycleSystem.ClearFocusedUnit(context.SelectionStateSystem);
        if (context.TryGetPointerPosition(out Vector2 markerScreenPosition))
            context.RequestMoveOrderScreenMarker?.Invoke(markerScreenPosition);
        return true;
    }

    public bool TryFocusUnit(Context context, Vector2 screenPosition)
    {
        if (!context.TryGetEntityManager(out EntityManager em))
        {
            context.LogSelectionDiagnostic?.Invoke($"focusAttempt result=False reason=NoEntityManager pos={screenPosition} frame={UnityEngine.Time.frameCount}");
            return false;
        }

        context.LogSelectionDiagnostic?.Invoke($"focusAttempt pos={screenPosition} frame={UnityEngine.Time.frameCount}");
        PointerTargetBoundaryPass targetBoundary = CreatePointerTargetBoundaryPass(context);
        if (!context.FocusedUnitLifecycleSystem.TryFocusUnit(
                em,
                screenPosition,
                context.SelectionStateSystem,
                (Vector2 position, EntityManager entityManager, out Entity entity) => targetBoundary.TryGetClickedUnitEntity(position, entityManager, out entity),
                "TryFocusUnit",
                "TryFocusUnit",
                context.LogSelectionDiagnostic,
                context.DescribeEntity,
                context.ClearHudSelection,
                context.ApplyHudSelection,
                out _))
        {
            context.LogSelectionDiagnostic?.Invoke($"focusAttempt result=False reason=TryFocusUnitFailed pos={screenPosition} frame={UnityEngine.Time.frameCount}");
            return false;
        }

        context.BuildingPlacementInteractionSystem?.ClearSelectedBuilding(context.BuildingPlacementInteractionContext, "RTSSelection.TryFocusUnit");
        context.InputSystem.ClearQueuedMoveOrder();
        int removedMoveCommands = context.InputSystem.ClearPendingMoveCommandRequests();
        context.InputSystem.IgnoreWorldCommandsUntilFrame = UnityEngine.Time.frameCount + 1;
        context.SetCameraDragging?.Invoke(false);
        context.LogSelectionDiagnostic?.Invoke($"focusAttempt result=True pos={screenPosition} ignoreWorldUntil={context.InputSystem.IgnoreWorldCommandsUntilFrame} clearedMoveCommands={removedMoveCommands}");
        return true;
    }

    public bool TryGetClickedUnitEntity(Context context, Vector2 screenPosition, EntityManager em, out Entity bestEntity)
    {
        return CreatePointerTargetBoundaryPass(context).TryGetClickedUnitEntity(screenPosition, em, out bestEntity);
    }

    private bool TryGetClickedUnitEntityFromBoundary(Context context, Vector2 screenPosition, EntityManager em, out Entity bestEntity)
    {
        bestEntity = Entity.Null;
        bool hasFlatClickedCell = TryGetFlatClickedCell(context, screenPosition, em, out int2 flatClickedCell);
        if (hasFlatClickedCell &&
            context.FocusableUnitLookupSystem.TryGetClickedUnitEntity(
                em,
                context.WorldCamera,
                flatClickedCell,
                screenPosition,
                out bestEntity))
        {
            context.LogSelectionDiagnostic?.Invoke($"unitLookup result=True route=FlatGrid pos={screenPosition} cell={flatClickedCell} entity={DescribeClickedEntity(em, bestEntity)}");
            return true;
        }

        if (!TryGetClickedCellFromBoundary(context, screenPosition, em, out int2 clickedCell, out _) ||
            (hasFlatClickedCell && clickedCell.Equals(flatClickedCell)))
        {
            bool fallbackHit = context.FocusableUnitLookupSystem.TryGetClickedUnitEntityByScreenDistance(
                em,
                context.WorldCamera,
                screenPosition,
                UnitClickScreenFallbackRadiusPixels,
                out bestEntity);
            context.LogSelectionDiagnostic?.Invoke($"unitLookup result={fallbackHit} route=ScreenDistanceAfterFlat pos={screenPosition} flatCellValid={hasFlatClickedCell} flatCell={flatClickedCell} entity={DescribeClickedEntity(em, bestEntity)} radius={UnitClickScreenFallbackRadiusPixels}");
            return fallbackHit;
        }

        if (context.FocusableUnitLookupSystem.TryGetClickedUnitEntity(
            em,
            context.WorldCamera,
            clickedCell,
            screenPosition,
            out bestEntity))
        {
            context.LogSelectionDiagnostic?.Invoke($"unitLookup result=True route=SurfaceGrid pos={screenPosition} flatCellValid={hasFlatClickedCell} flatCell={flatClickedCell} surfaceCell={clickedCell} entity={DescribeClickedEntity(em, bestEntity)}");
            return true;
        }

        bool screenHit = context.FocusableUnitLookupSystem.TryGetClickedUnitEntityByScreenDistance(
            em,
            context.WorldCamera,
            screenPosition,
            UnitClickScreenFallbackRadiusPixels,
            out bestEntity);
        context.LogSelectionDiagnostic?.Invoke($"unitLookup result={screenHit} route=ScreenDistanceAfterSurface pos={screenPosition} flatCellValid={hasFlatClickedCell} flatCell={flatClickedCell} surfaceCell={clickedCell} entity={DescribeClickedEntity(em, bestEntity)} radius={UnitClickScreenFallbackRadiusPixels}");
        return screenHit;
    }

    public bool TryGetClickedAttackTargetEntity(Context context, Vector2 screenPosition, EntityManager em, out Entity bestEntity)
    {
        return CreatePointerTargetBoundaryPass(context).TryGetClickedAttackTargetEntity(screenPosition, em, out bestEntity);
    }

    public string BuildClickDebugSummary(Context context, Vector2 screenPosition)
    {
        if (context.TryGetEntityManager == null ||
            !context.TryGetEntityManager(out EntityManager em))
        {
            return "world=missing";
        }

        string clickedCell = TryGetClickedCell(context, screenPosition, em, out int2 cell, out Vector3 worldPoint)
            ? $"{cell}@{worldPoint.x:F1},{worldPoint.y:F1},{worldPoint.z:F1}"
            : "none";
        SelectionStateSystem selectionState = context.SelectionStateSystem;
        Entity focusedUnit = selectionState != null ? selectionState.FocusedUnit : Entity.Null;
        string focused = DescribeClickDebugEntity(em, focusedUnit);
        List<Entity> cached = selectionState?.CachedSelectedMoveEntities;
        int cachedCount = cached?.Count ?? 0;
        string selected0 = cachedCount > 0 ? DescribeClickDebugEntity(em, cached[0]) : "none";
        int selectedTagCount = CountSelectedTags(em);
        bool suppressNextWorldClick = context.RuntimeGameplayStateSystem.SuppressNextWorldClick;
        int ignoreWorldCommandsUntilFrame = context.InputSystem != null
            ? context.InputSystem.IgnoreWorldCommandsUntilFrame
            : 0;
        return $"clickedCell={clickedCell} focused={focused} cachedCount={cachedCount} selectedTags={selectedTagCount} selected0={selected0} suppress={suppressNextWorldClick} ignoreUntil={ignoreWorldCommandsUntilFrame}";
    }

    private bool TryGetClickedAttackTargetEntityFromBoundary(Context context, Vector2 screenPosition, EntityManager em, out Entity bestEntity)
    {
        if (TryGetClickedUnitEntityFromBoundary(context, screenPosition, em, out bestEntity))
            return true;

        bool hasFlatClickedCell = TryGetFlatClickedCell(context, screenPosition, em, out int2 flatClickedCell);
        if (hasFlatClickedCell &&
            TryGetClickedRuntimeBuildingCombatEntity(em, context.WorldCamera, flatClickedCell, screenPosition, out bestEntity))
        {
            context.LogSelectionDiagnostic?.Invoke($"attackTargetLookup result=True route=FlatRuntimeBuilding pos={screenPosition} cell={flatClickedCell} entity={DescribeClickedEntity(em, bestEntity)}");
            return true;
        }

        if (!TryGetClickedCellFromBoundary(context, screenPosition, em, out int2 clickedCell, out _) ||
            (hasFlatClickedCell && clickedCell.Equals(flatClickedCell)))
        {
            context.LogSelectionDiagnostic?.Invoke($"attackTargetLookup result=False route=RuntimeBuilding pos={screenPosition} flatCellValid={hasFlatClickedCell} flatCell={flatClickedCell}");
            return false;
        }

        if (TryGetClickedRuntimeBuildingCombatEntity(em, context.WorldCamera, clickedCell, screenPosition, out bestEntity))
        {
            context.LogSelectionDiagnostic?.Invoke($"attackTargetLookup result=True route=SurfaceRuntimeBuilding pos={screenPosition} flatCellValid={hasFlatClickedCell} flatCell={flatClickedCell} surfaceCell={clickedCell} entity={DescribeClickedEntity(em, bestEntity)}");
            return true;
        }

        context.LogSelectionDiagnostic?.Invoke($"attackTargetLookup result=False route=SurfaceRuntimeBuilding pos={screenPosition} flatCellValid={hasFlatClickedCell} flatCell={flatClickedCell} surfaceCell={clickedCell}");
        return false;
    }

    private static string DescribeClickedEntity(EntityManager em, Entity entity)
    {
        if (entity == Entity.Null || !em.Exists(entity))
            return "null";

        string source = em.HasComponent<UnitSourcePrefabKey>(entity)
            ? em.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString()
            : em.GetName(entity);
        byte faction = em.HasComponent<Faction>(entity)
            ? em.GetComponentData<Faction>(entity).Id
            : (byte)0;
        bool selected = em.HasComponent<SelectedUnitTag>(entity);
        bool hasMove = em.HasComponent<UnitMove>(entity);
        bool hasGrid = em.HasComponent<UnitGrid>(entity);
        bool disabled = em.HasComponent<Disabled>(entity);
        bool passenger = em.HasComponent<UnitTransportPassenger>(entity);
        return $"{entity}/{source}/faction={faction}/selected={selected}/move={hasMove}/grid={hasGrid}/disabled={disabled}/passenger={passenger}";
    }

    private static int CountSelectedTags(EntityManager em)
    {
        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<SelectedUnitTag>());
        return query.CalculateEntityCount();
    }

    private static string DescribeClickDebugEntity(EntityManager em, Entity entity)
    {
        if (entity == Entity.Null || !em.Exists(entity))
            return "null";

        string source = em.HasComponent<UnitSourcePrefabKey>(entity)
            ? em.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString()
            : em.GetName(entity);
        byte faction = em.HasComponent<Faction>(entity)
            ? em.GetComponentData<Faction>(entity).Id
            : (byte)0;
        string grid = em.HasComponent<UnitGrid>(entity)
            ? em.GetComponentData<UnitGrid>(entity).Cell.ToString()
            : "none";
        string target = em.HasComponent<UnitTarget>(entity)
            ? em.GetComponentData<UnitTarget>(entity).Cell.ToString()
            : "none";
        string pathRequest = em.HasComponent<UnitPathRequest>(entity)
            ? em.GetComponentData<UnitPathRequest>(entity).Goal.ToString()
            : "none";
        bool selected = em.HasComponent<SelectedUnitTag>(entity);
        bool pathFollow = em.HasComponent<UnitPathFollow>(entity);
        bool manual = em.HasComponent<ManualMoveOrderTag>(entity);
        bool engage = em.HasComponent<EngageTarget>(entity);
        return $"{entity}/{source}/faction={faction}/selected={selected}/grid={grid}/target={target}/pathRequest={pathRequest}/pathFollow={pathFollow}/manual={manual}/engage={engage}";
    }

    private bool TryGetFlatClickedCell(Context context, Vector2 screenPosition, EntityManager em, out int2 cell)
    {
        cell = default;
        if (context.WorldCamera == null)
            return false;

        EnsureEntityQueries(em);
        if (_gridConfigQuery.IsEmptyIgnoreFilter)
            return false;

        GridConfig grid = em.GetComponentData<GridConfig>(_gridConfigQuery.GetSingletonEntity());
        Ray ray = context.WorldCamera.ScreenPointToRay(screenPosition);
        Plane plane = new(Vector3.up, new Vector3(0f, grid.Origin.y, 0f));
        if (!plane.Raycast(ray, out float distance))
            return false;

        Vector3 worldPoint = ray.GetPoint(distance);
        cell = GridUtils.WorldToCell(grid, worldPoint);
        return GridUtils.InBounds(cell, grid.Width, grid.Height);
    }

    private bool TryGetClickedRuntimeBuildingCombatEntity(
        EntityManager em,
        Camera worldCamera,
        int2 clickedCell,
        Vector2 screenPosition,
        out Entity bestEntity)
    {
        bestEntity = Entity.Null;
        if (worldCamera == null)
            return false;

        EnsureEntityQueries(em);
        if (_runtimeBuildingCombatQuery.IsEmptyIgnoreFilter)
            return false;

        float bestDistanceSq = float.MaxValue;
        EntityTypeHandle entityType = em.GetEntityTypeHandle();
        ComponentTypeHandle<RuntimeBuildingCombatInfo> combatInfoType = em.GetComponentTypeHandle<RuntimeBuildingCombatInfo>(true);
        ComponentTypeHandle<Faction> factionType = em.GetComponentTypeHandle<Faction>(true);
        ComponentTypeHandle<UnitHealth> healthType = em.GetComponentTypeHandle<UnitHealth>(true);
        ComponentTypeHandle<LocalTransform> transformType = em.GetComponentTypeHandle<LocalTransform>(true);
        using NativeArray<ArchetypeChunk> chunks = _runtimeBuildingCombatQuery.ToArchetypeChunkArray(Allocator.Temp);
        for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
        {
            ArchetypeChunk chunk = chunks[chunkIndex];
            NativeArray<Entity> entities = chunk.GetNativeArray(entityType);
            NativeArray<RuntimeBuildingCombatInfo> combatInfos = chunk.GetNativeArray(ref combatInfoType);
            NativeArray<Faction> factions = chunk.GetNativeArray(ref factionType);
            NativeArray<UnitHealth> healths = chunk.GetNativeArray(ref healthType);
            NativeArray<LocalTransform> transforms = chunk.GetNativeArray(ref transformType);
            for (int i = 0; i < entities.Length; i++)
            {
                if (!IsAttackableRuntimeBuildingCandidate(factions[i], healths[i], combatInfos[i], clickedCell))
                    continue;

                Vector3 screen = worldCamera.WorldToScreenPoint(transforms[i].Position);
                if (screen.z <= 0f)
                    continue;

                float distanceSq = (new Vector2(screen.x, screen.y) - screenPosition).sqrMagnitude;
                if (distanceSq >= bestDistanceSq)
                    continue;

                bestDistanceSq = distanceSq;
                bestEntity = entities[i];
            }
        }

        return bestEntity != Entity.Null;
    }

    private static bool IsAttackableRuntimeBuildingCandidate(
        Faction faction,
        UnitHealth health,
        RuntimeBuildingCombatInfo info,
        int2 clickedCell)
    {
        if (!FactionIdentity.IsHostileToPlayer(faction.Id))
            return false;

        if (health.Current <= 0)
            return false;

        int2 origin = info.OriginCell;
        int2 size = UnitFootprintUtility.ClampSize(info.FootprintCells);
        int2 max = origin + size;
        return clickedCell.x >= origin.x &&
               clickedCell.y >= origin.y &&
               clickedCell.x < max.x &&
               clickedCell.y < max.y;
    }

    public bool TryGetClickedCell(Context context, Vector2 screenPosition, EntityManager em, out int2 cell, out Vector3 worldPoint)
    {
        return CreatePointerTargetBoundaryPass(context).TryGetClickedCell(screenPosition, em, out cell, out worldPoint);
    }

    public bool TryGetMoveCommandCell(Context context, Vector2 screenPosition, EntityManager em, out int2 cell, out Vector3 worldPoint)
    {
        return CreatePointerTargetBoundaryPass(context).TryGetMoveCommandCell(screenPosition, em, out cell, out worldPoint);
    }

    private bool TryGetClickedCellFromBoundary(Context context, Vector2 screenPosition, EntityManager em, out int2 cell, out Vector3 worldPoint)
    {
        cell = default;
        worldPoint = default;
        if (context.WorldCamera == null)
            return false;

        EnsureEntityQueries(em);
        if (_gridConfigQuery.IsEmptyIgnoreFilter)
            return false;

        GridConfig grid = em.GetComponentData<GridConfig>(_gridConfigQuery.GetSingletonEntity());
        if (!TryResolveMapSurfaceCommandTarget(
                em,
                _mapSurfaceQuery,
                grid,
                context.WorldCamera,
                screenPosition,
                context.SelectionStateSystem,
                out MapSurfaceCommandTargetResult target))
        {
            return false;
        }

        cell = target.Cell;
        worldPoint = target.WorldPoint;
        if (SelectionRuntimeDiagnosticsSystem.EnableMoveCommandTrace)
        {
            SelectionRuntimeDiagnosticsSystem.LogMoveCommandTrace(
                $"moveTargetResolved screen={screenPosition} cell={cell} world={worldPoint} surface={target.HasSurface} " +
                $"surfaceId={(target.HasSurface ? target.Surface.SurfaceId : -1)} layer={(target.HasSurface ? target.Surface.LayerId : -1)} " +
                $"height={(target.HasSurface ? target.Surface.Height : worldPoint.y):F2}");
        }

        context.LogSelectionDiagnostic?.Invoke(
            $"moveTargetResolved pos={screenPosition} cell={cell} world={worldPoint} surface={target.HasSurface} surfaceId={(target.HasSurface ? target.Surface.SurfaceId : -1)} layer={(target.HasSurface ? target.Surface.LayerId : -1)} height={(target.HasSurface ? target.Surface.Height : worldPoint.y):F2}");
        return true;
    }

    private bool TryGetMoveCommandCellFromBoundary(Context context, Vector2 screenPosition, EntityManager em, out int2 cell, out Vector3 worldPoint)
    {
        cell = default;
        worldPoint = default;
        if (context.WorldCamera == null)
            return false;

        EnsureEntityQueries(em);
        if (_gridConfigQuery.IsEmptyIgnoreFilter)
            return false;

        Entity gridEntity = _gridConfigQuery.GetSingletonEntity();
        GridConfig grid = em.GetComponentData<GridConfig>(gridEntity);
        if (!TryResolveMapSurfaceMoveCommandTarget(
                em,
                _mapSurfaceQuery,
                gridEntity,
                grid,
                context.WorldCamera,
                screenPosition,
                context.SelectionStateSystem,
                out MapSurfaceCommandTargetResult target))
        {
            return false;
        }

        cell = target.Cell;
        worldPoint = target.WorldPoint;
        if (SelectionRuntimeDiagnosticsSystem.EnableMoveCommandTrace)
        {
            SelectionRuntimeDiagnosticsSystem.LogMoveCommandTrace(
                $"moveTargetResolved screen={screenPosition} cell={cell} world={worldPoint} surface={target.HasSurface} " +
                $"surfaceId={(target.HasSurface ? target.Surface.SurfaceId : -1)} layer={(target.HasSurface ? target.Surface.LayerId : -1)} " +
                $"height={(target.HasSurface ? target.Surface.Height : worldPoint.y):F2}");
        }

        context.LogSelectionDiagnostic?.Invoke(
            $"moveTargetResolved pos={screenPosition} cell={cell} world={worldPoint} surface={target.HasSurface} surfaceId={(target.HasSurface ? target.Surface.SurfaceId : -1)} layer={(target.HasSurface ? target.Surface.LayerId : -1)} height={(target.HasSurface ? target.Surface.Height : worldPoint.y):F2}");
        return true;
    }

    public bool TryResolveMapSurfaceCommandTarget(
        EntityManager entityManager,
        EntityQuery surfaceQuery,
        GridConfig grid,
        Camera worldCamera,
        Vector2 screenPosition,
        SelectionStateSystem selectionStateSystem,
        out MapSurfaceCommandTargetResult result)
    {
        result = default;
        if (worldCamera == null)
            return false;

        Ray ray = worldCamera.ScreenPointToRay(screenPosition);
        if (!TryResolveFlatFallback(grid, ray, out int2 fallbackCell, out Vector3 fallbackWorldPoint))
            return false;

        if (!_mapSurfaceQuerySystem.TryCreateContext(entityManager, surfaceQuery, out MapSurfaceSampler.Context surfaceContext))
        {
            result = MapSurfaceCommandTargetResult.FlatFallback(fallbackCell, fallbackWorldPoint);
            return true;
        }

        TryResolvePreferredSelectionLayer(entityManager, selectionStateSystem, out int preferredSurfaceId, out int preferredLayerId);
        MapSurfaceMovementMask movementMask = ResolveSelectedMovementMask(entityManager, selectionStateSystem);
        if (TryResolveSurfaceHit(
                surfaceContext,
                grid,
                ray,
                fallbackCell,
                movementMask,
                preferredSurfaceId,
                preferredLayerId,
                out result))
        {
            return true;
        }

        if (TryResolveNearestTraversableTarget(surfaceContext, grid, fallbackCell, movementMask, out result))
            return true;

        result = MapSurfaceCommandTargetResult.FlatFallback(fallbackCell, fallbackWorldPoint);
        return true;
    }

    public bool TryResolveMapSurfaceMoveCommandTarget(
        EntityManager entityManager,
        EntityQuery surfaceQuery,
        Entity gridEntity,
        GridConfig grid,
        Camera worldCamera,
        Vector2 screenPosition,
        SelectionStateSystem selectionStateSystem,
        out MapSurfaceCommandTargetResult result)
    {
        if (!TryResolveMapSurfaceCommandTarget(
                entityManager,
                surfaceQuery,
                grid,
                worldCamera,
                screenPosition,
                selectionStateSystem,
                out result))
        {
            return false;
        }

        if (TryResolveSelectedMoveFootprintTarget(
                entityManager,
                surfaceQuery,
                gridEntity,
                grid,
                selectionStateSystem,
                result.Cell,
                out _,
                out MapSurfaceCommandTargetResult footprintResult))
        {
            result = footprintResult;
        }

        return true;
    }

    public bool TryResolveSelectedMoveFootprintTarget(
        EntityManager entityManager,
        EntityQuery surfaceQuery,
        Entity gridEntity,
        GridConfig grid,
        SelectionStateSystem selectionStateSystem,
        int2 desiredGoal,
        out int2 resolvedCell,
        out MapSurfaceCommandTargetResult result)
    {
        resolvedCell = desiredGoal;
        result = default;
        if (!TryBuildSelectedGroundMoveEntityList(entityManager, selectionStateSystem, out Entity primaryEntity) ||
            !TryReadGridPathingData(
                entityManager,
                gridEntity,
                out NativeArray<GridWalkable> walkable,
                out DynamicBlockerComponent blockerData,
                out DynamicOccupancyComponent occupancyData))
        {
            return false;
        }

        MapSurfacePathfindingSnapshot.Context surfaceContext =
            _mapSurfaceReadSystem.TryCreateContext(entityManager, surfaceQuery, out MapSurfacePathfindingSnapshot.Context resolvedSurfaceContext)
                ? resolvedSurfaceContext
                : _mapSurfaceReadSystem.CreateFlatFallbackContext();

        var reservedGoalCells = new HashSet<int>();
        HashSet<int> selectedCurrentCells = _mapSurfaceMoveOrderSystem.BuildSelectedCurrentFootprintCells(entityManager, grid, _mapSurfaceSelectedMoveEntities);
        resolvedCell = _mapSurfaceMoveOrderSystem.FindManualMoveGoal(
            entityManager,
            grid,
            walkable,
            blockerData.Blocked,
            blockerData.FriendlyPassFactionIds,
            occupancyData.Occupied,
            reservedGoalCells,
            selectedCurrentCells,
            primaryEntity,
            desiredGoal,
            0,
            surfaceContext);

        MapSurfaceMovementMask movementMask = ResolveSelectedMovementMask(entityManager, selectionStateSystem);
        if (_mapSurfaceQuerySystem.TryCreateContext(entityManager, surfaceQuery, out MapSurfaceSampler.Context queryContext) &&
            TryResolveTraversableCell(queryContext, grid, resolvedCell, movementMask, out result))
        {
            return true;
        }

        Vector3 worldPoint = GridUtils.CellToWorldCenter(grid, resolvedCell);
        result = MapSurfaceCommandTargetResult.FlatFallback(resolvedCell, worldPoint);
        return true;
    }

    private static bool TryResolveFlatFallback(GridConfig grid, Ray ray, out int2 cell, out Vector3 worldPoint)
    {
        cell = default;
        worldPoint = default;

        Plane plane = new(Vector3.up, new Vector3(0f, grid.Origin.y, 0f));
        if (!plane.Raycast(ray, out float distance))
            return false;

        worldPoint = ray.GetPoint(distance);
        cell = GridUtils.WorldToCell(grid, worldPoint);
        return GridUtils.InBounds(cell, grid.Width, grid.Height);
    }

    private bool TryBuildSelectedGroundMoveEntityList(
        EntityManager entityManager,
        SelectionStateSystem selectionStateSystem,
        out Entity primaryEntity)
    {
        primaryEntity = Entity.Null;
        _mapSurfaceSelectedMoveEntities.Clear();
        if (selectionStateSystem == null)
            return false;

        TryAddGroundMoveEntity(entityManager, selectionStateSystem.FocusedUnit);
        List<Entity> selected = selectionStateSystem.CachedSelectedMoveEntities;
        for (int i = 0; i < selected.Count; i++)
            TryAddGroundMoveEntity(entityManager, selected[i]);

        if (_mapSurfaceSelectedMoveEntities.Count == 0)
            return false;

        primaryEntity = _mapSurfaceSelectedMoveEntities[0];
        return true;
    }

    private void TryAddGroundMoveEntity(EntityManager entityManager, Entity entity)
    {
        if (entity == Entity.Null ||
            _mapSurfaceSelectedMoveEntities.Contains(entity) ||
            !SelectionStateSystem.IsCacheableSelectedMoveEntity(entityManager, entity) ||
            entityManager.HasComponent<UnitAirMovement>(entity) ||
            !entityManager.HasComponent<UnitFootprint>(entity) ||
            !entityManager.HasComponent<UnitMovementBehavior>(entity))
        {
            return;
        }

        _mapSurfaceSelectedMoveEntities.Add(entity);
    }

    private static bool TryReadGridPathingData(
        EntityManager entityManager,
        Entity gridEntity,
        out NativeArray<GridWalkable> walkable,
        out DynamicBlockerComponent blockerData,
        out DynamicOccupancyComponent occupancyData)
    {
        walkable = default;
        blockerData = default;
        occupancyData = default;
        if (gridEntity == Entity.Null ||
            !entityManager.Exists(gridEntity) ||
            !entityManager.HasBuffer<GridWalkable>(gridEntity))
        {
            return false;
        }

        DynamicBuffer<GridWalkable> walkableBuffer = entityManager.GetBuffer<GridWalkable>(gridEntity);
        if (walkableBuffer.Length == 0)
            return false;

        walkable = walkableBuffer.AsNativeArray();
        blockerData = entityManager.HasComponent<DynamicBlockerComponent>(gridEntity)
            ? entityManager.GetComponentData<DynamicBlockerComponent>(gridEntity)
            : default;
        occupancyData = entityManager.HasComponent<DynamicOccupancyComponent>(gridEntity)
            ? entityManager.GetComponentData<DynamicOccupancyComponent>(gridEntity)
            : default;
        return true;
    }

    private bool TryResolveSurfaceHit(
        MapSurfaceSampler.Context context,
        GridConfig grid,
        Ray ray,
        int2 fallbackCell,
        MapSurfaceMovementMask movementMask,
        int preferredSurfaceId,
        int preferredLayerId,
        out MapSurfaceCommandTargetResult result)
    {
        result = default;
        float bestScore = float.MaxValue;
        bool found = false;

        for (int y = -1; y <= 1; y++)
        {
            for (int x = -1; x <= 1; x++)
            {
                int2 candidateCell = fallbackCell + new int2(x, y);
                if (!GridUtils.InBounds(candidateCell, grid.Width, grid.Height) ||
                    !_mapSurfaceQuerySystem.TryGetSurfaceRange(context, candidateCell, out MapSurfaceCellSurfaceRange range))
                {
                    continue;
                }

                for (int i = 0; i < range.SurfaceCount; i++)
                {
                    if (!_mapSurfaceQuerySystem.TryGetSurfaceInRange(context, range, i, out MapSurfaceSample sample) ||
                        !CanTraverse(sample, movementMask) ||
                        !TryIntersectSurface(grid, ray, sample, out Vector3 worldPoint, out float distance))
                    {
                        continue;
                    }

                    int2 hitCell = GridUtils.WorldToCell(grid, worldPoint);
                    if (!hitCell.Equals(sample.Cell) || !GridUtils.InBounds(hitCell, grid.Width, grid.Height))
                        continue;

                    float score = distance;
                    if (sample.SurfaceId == preferredSurfaceId)
                        score -= 0.05f;
                    if (sample.LayerId == preferredLayerId)
                        score -= 0.025f;

                    if (!found || score < bestScore)
                    {
                        bestScore = score;
                        result = MapSurfaceCommandTargetResult.SurfaceHit(hitCell, worldPoint, sample);
                        found = true;
                    }
                }
            }
        }

        return found;
    }

    private bool TryResolveNearestTraversableTarget(
        MapSurfaceSampler.Context context,
        GridConfig grid,
        int2 originCell,
        MapSurfaceMovementMask movementMask,
        out MapSurfaceCommandTargetResult result)
    {
        result = default;
        if (movementMask == MapSurfaceMovementMask.None)
            return false;

        if (TryResolveTraversableCell(context, grid, originCell, movementMask, out result))
            return true;

        for (int radius = 1; radius <= TraversableTargetSearchRadius; radius++)
        {
            int ringLen = math.max(1, 8 * radius);
            for (int step = 0; step < ringLen; step++)
            {
                int2 candidate = originCell + SquareRingOffset(radius, step);
                if (TryResolveTraversableCell(context, grid, candidate, movementMask, out result))
                    return true;
            }
        }

        return false;
    }

    private bool TryResolveTraversableCell(
        MapSurfaceSampler.Context context,
        GridConfig grid,
        int2 cell,
        MapSurfaceMovementMask movementMask,
        out MapSurfaceCommandTargetResult result)
    {
        result = default;
        if (!GridUtils.InBounds(cell, grid.Width, grid.Height) ||
            !_mapSurfaceQuerySystem.TryGetSurfaceRange(context, cell, out MapSurfaceCellSurfaceRange range))
        {
            return false;
        }

        for (int i = 0; i < range.SurfaceCount; i++)
        {
            if (!_mapSurfaceQuerySystem.TryGetSurfaceInRange(context, range, i, out MapSurfaceSample sample) ||
                !CanTraverse(sample, movementMask))
            {
                continue;
            }

            Vector3 worldPoint = GridUtils.CellToWorldCenter(grid, cell);
            worldPoint.y = sample.Height;
            result = MapSurfaceCommandTargetResult.SurfaceHit(cell, worldPoint, sample);
            return true;
        }

        return false;
    }

    private static bool TryIntersectSurface(GridConfig grid, Ray ray, MapSurfaceSample sample, out Vector3 worldPoint, out float distance)
    {
        worldPoint = default;
        distance = 0f;

        Vector3 sampleCenter = GridUtils.CellToWorldCenter(grid, sample.Cell);
        sampleCenter.y = sample.Height;
        Vector3 normal = math.lengthsq(sample.Normal) > 0.0001f
            ? new Vector3(sample.Normal.x, sample.Normal.y, sample.Normal.z).normalized
            : Vector3.up;

        Plane plane = new(normal, sampleCenter);
        if (!plane.Raycast(ray, out distance) || distance < 0f)
            return false;

        worldPoint = ray.GetPoint(distance);
        return true;
    }

    private static void TryResolvePreferredSelectionLayer(
        EntityManager entityManager,
        SelectionStateSystem selectionStateSystem,
        out int preferredSurfaceId,
        out int preferredLayerId)
    {
        preferredSurfaceId = -1;
        preferredLayerId = -1;
        if (selectionStateSystem == null)
            return;

        if (TryReadSurface(entityManager, selectionStateSystem.FocusedUnit, out preferredSurfaceId, out preferredLayerId))
            return;

        List<Entity> selected = selectionStateSystem.CachedSelectedMoveEntities;
        for (int i = 0; i < selected.Count; i++)
        {
            if (TryReadSurface(entityManager, selected[i], out preferredSurfaceId, out preferredLayerId))
                return;
        }
    }

    private static bool TryReadSurface(EntityManager entityManager, Entity entity, out int surfaceId, out int layerId)
    {
        surfaceId = -1;
        layerId = -1;
        if (entity == Entity.Null ||
            !entityManager.Exists(entity) ||
            !entityManager.HasComponent<UnitSurfaceComponent>(entity))
        {
            return false;
        }

        UnitSurfaceComponent surface = entityManager.GetComponentData<UnitSurfaceComponent>(entity);
        if (surface.HasSurface == 0)
            return false;

        surfaceId = surface.SurfaceId;
        layerId = surface.LayerId;
        return true;
    }

    private MapSurfaceMovementMask ResolveSelectedMovementMask(
        EntityManager entityManager,
        SelectionStateSystem selectionStateSystem)
    {
        if (selectionStateSystem == null)
            return MapSurfaceMovementMask.Infantry;

        bool hasGroundUnit = false;
        bool hasVehicle = false;
        if (TryReadMovement(entityManager, selectionStateSystem.FocusedUnit, out bool focusedVehicle))
        {
            hasGroundUnit = true;
            hasVehicle |= focusedVehicle;
        }

        List<Entity> selected = selectionStateSystem.CachedSelectedMoveEntities;
        for (int i = 0; i < selected.Count; i++)
        {
            if (!TryReadMovement(entityManager, selected[i], out bool vehicle))
                continue;

            hasGroundUnit = true;
            hasVehicle |= vehicle;
        }

        if (!hasGroundUnit)
            return MapSurfaceMovementMask.Infantry;

        return hasVehicle
            ? MapSurfaceMovementMask.WheeledVehicle | MapSurfaceMovementMask.TrackedVehicle
            : MapSurfaceMovementMask.Infantry;
    }

    private static bool TryReadMovement(EntityManager entityManager, Entity entity, out bool isVehicle)
    {
        isVehicle = false;
        if (entity == Entity.Null ||
            !entityManager.Exists(entity) ||
            entityManager.HasComponent<UnitAirMovement>(entity) ||
            !entityManager.HasComponent<UnitFootprint>(entity) ||
            !entityManager.HasComponent<UnitMovementBehavior>(entity))
        {
            return false;
        }

        UnitFootprint footprint = entityManager.GetComponentData<UnitFootprint>(entity);
        UnitMovementBehavior movementBehavior = entityManager.GetComponentData<UnitMovementBehavior>(entity);
        isVehicle = UnitVehicleMovementUtility.IsVehicle(footprint, movementBehavior);
        return true;
    }

    private bool CanTraverse(MapSurfaceSample sample, MapSurfaceMovementMask movementMask)
    {
        return _mapSurfaceSlopeClassificationSystem.AllowsMovement(sample, movementMask);
    }

    private static int2 SquareRingOffset(int radius, int step)
    {
        int topLen = (2 * radius) + 1;
        if (step < topLen)
            return new int2(-radius + step, radius);

        step -= topLen;
        int rightLen = 2 * radius;
        if (step < rightLen)
            return new int2(radius, (radius - 1) - step);

        step -= rightLen;
        int bottomLen = 2 * radius;
        if (step < bottomLen)
            return new int2((radius - 1) - step, -radius);

        step -= bottomLen;
        return new int2(-radius, (-radius + 1) + step);
    }

    private void EnsureEntityQueries(EntityManager em)
    {
        Unity.Entities.World world = em.World;
        if (_queryWorld == world && world != null && world.IsCreated)
            return;

        _queryWorld = world;
        _gridConfigQuery = em.CreateEntityQuery(ComponentType.ReadOnly<GridConfig>());
        _mapSurfaceQuery = em.CreateEntityQuery(ComponentType.ReadOnly<MapSurfaceComponent>());
        _runtimeBuildingCombatQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<RuntimeBuildingCombatTag>(),
            ComponentType.ReadOnly<RuntimeBuildingCombatInfo>(),
            ComponentType.ReadOnly<Faction>(),
            ComponentType.ReadOnly<UnitHealth>(),
            ComponentType.ReadOnly<LocalTransform>());
    }
}
