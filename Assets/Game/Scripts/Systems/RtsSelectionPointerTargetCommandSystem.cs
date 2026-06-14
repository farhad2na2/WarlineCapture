using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public sealed class RtsSelectionPointerTargetCommandSystem
{
    private const float UnitClickScreenFallbackRadiusPixels = 54f;

    public delegate bool TryGetEntityManagerDelegate(out EntityManager em);
    public delegate bool TryGetPointerPositionDelegate(out Vector2 pointerPosition);

    public readonly struct Context
    {
        public readonly RuntimeGameplayStateSystem RuntimeGameplayStateSystem;
        public readonly RtsSelectionInputSystem InputSystem;
        public readonly SelectionStateSystem SelectionStateSystem;
        public readonly FocusedUnitLifecycleSystem FocusedUnitLifecycleSystem;
        public readonly UnitTargetOrderSystem UnitTargetOrderSystem;
        public readonly FocusableUnitLookupSystem FocusableUnitLookupSystem;
        public readonly TransportBoardingCommandSystem TransportBoardingCommandSystem;
        public readonly UnitTransportCapacitySystem UnitTransportCapacitySystem;
        public readonly UnitTransportBoardingQuerySystem UnitTransportBoardingQuerySystem;
        public readonly UnitTransportBoardingRuleSystem UnitTransportBoardingRuleSystem;
        public readonly UnitTransportApproachCellSystem UnitTransportApproachCellSystem;
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

        public Context(
            RuntimeGameplayStateSystem runtimeGameplayStateSystem,
            RtsSelectionInputSystem inputSystem,
            SelectionStateSystem selectionStateSystem,
            FocusedUnitLifecycleSystem focusedUnitLifecycleSystem,
            UnitTargetOrderSystem unitTargetOrderSystem,
            FocusableUnitLookupSystem focusableUnitLookupSystem,
            TransportBoardingCommandSystem transportBoardingCommandSystem,
            UnitTransportCapacitySystem unitTransportCapacitySystem,
            UnitTransportBoardingQuerySystem unitTransportBoardingQuerySystem,
            UnitTransportBoardingRuleSystem unitTransportBoardingRuleSystem,
            UnitTransportApproachCellSystem unitTransportApproachCellSystem,
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
            FocusedUnitLifecycleSystem.DescribeEntityDelegate describeEntity)
        {
            RuntimeGameplayStateSystem = runtimeGameplayStateSystem;
            InputSystem = inputSystem;
            SelectionStateSystem = selectionStateSystem;
            FocusedUnitLifecycleSystem = focusedUnitLifecycleSystem;
            UnitTargetOrderSystem = unitTargetOrderSystem;
            FocusableUnitLookupSystem = focusableUnitLookupSystem;
            TransportBoardingCommandSystem = transportBoardingCommandSystem;
            UnitTransportCapacitySystem = unitTransportCapacitySystem;
            UnitTransportBoardingQuerySystem = unitTransportBoardingQuerySystem;
            UnitTransportBoardingRuleSystem = unitTransportBoardingRuleSystem;
            UnitTransportApproachCellSystem = unitTransportApproachCellSystem;
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
        }
    }

    private World _queryWorld;
    private EntityQuery _gridConfigQuery;
    private EntityQuery _mapSurfaceQuery;
    private EntityQuery _runtimeBuildingCombatQuery;
    private readonly MapSurfaceCommandTargetSystem _mapSurfaceCommandTargetSystem = new();

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

    public void IssueMoveOrder(Context context, Vector2 screenPosition)
    {
        if (SelectionRuntimeDiagnosticsSystem.EnableMoveCommandTrace)
        {
            SelectionRuntimeDiagnosticsSystem.LogMoveCommandTrace(
                $"issueMoveOrderEnter screen={screenPosition} frame={Time.frameCount} " +
                $"hasInput={context.InputSystem != null} hasProcess={context.ProcessMoveCommandRequests != null}");
        }

        context.SetExplicitAttackTargetModeActive?.Invoke(false);
        context.ApplyHudCommandMode?.Invoke(TacticalCommandMode.Move);
        context.LogSelectionDiagnostic?.Invoke($"moveAttempt pos={screenPosition} frame={Time.frameCount}");

        bool queued = TryQueueResolvedMoveCommand(context, screenPosition, out bool queuedResolvedTarget);
        if (!queued)
        {
            if (SelectionRuntimeDiagnosticsSystem.EnableMoveCommandTrace)
            {
                SelectionRuntimeDiagnosticsSystem.LogMoveCommandTrace(
                    $"issueMoveOrderQueueFailed screen={screenPosition} frame={Time.frameCount}");
            }

            context.LogSelectionDiagnostic?.Invoke($"moveAttempt result=False reason=QueueFailed pos={screenPosition} frame={Time.frameCount}");
            context.ClearHudCommandMode?.Invoke();
            context.InputSystem.ClearActiveCommandMode();
            context.ApplyHudCommandResult?.Invoke(TacticalCommandResult.Rejected(TacticalCommandReasonCode.NoSelection));
            return;
        }

        if (SelectionRuntimeDiagnosticsSystem.EnableMoveCommandTrace)
            SelectionRuntimeDiagnosticsSystem.LogMoveCommandTrace($"issueMoveOrderQueued screen={screenPosition} frame={Time.frameCount}");
        if (!queuedResolvedTarget)
        {
            context.ProcessMoveCommandRequests?.Invoke();
            if (SelectionRuntimeDiagnosticsSystem.EnableMoveCommandTrace)
                SelectionRuntimeDiagnosticsSystem.LogMoveCommandTrace($"issueMoveOrderProcessReturned screen={screenPosition} frame={Time.frameCount}");
        }
    }

    private bool TryQueueResolvedMoveCommand(Context context, Vector2 screenPosition, out bool queuedResolvedTarget)
    {
        queuedResolvedTarget = false;
        int frame = Time.frameCount;
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

    public bool TryIssueAttackOrderToClickedUnit(Context context, Vector2 screenPosition)
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
        int frame = Time.frameCount;
        if (context.TryGetEntityManager == null ||
            !context.TryGetEntityManager(out EntityManager em))
        {
            return context.InputSystem.QueueAttackCommandRequest(screenPosition, explicitAttackTargetModeActive, frame);
        }

        PointerTargetBoundaryPass targetBoundary = CreatePointerTargetBoundaryPass(context);
        if (!targetBoundary.TryGetClickedUnitEntity(screenPosition, em, out Entity targetEntity) ||
            !IsDirectResolvedAttackTarget(em, targetEntity, context.UnitTargetOrderSystem))
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
        Entity targetEntity,
        UnitTargetOrderSystem targetOrderSystem)
    {
        if (targetEntity == Entity.Null ||
            !em.Exists(targetEntity) ||
            em.HasComponent<RuntimeBuildingCombatTag>(targetEntity) ||
            em.HasComponent<RuntimeBuildingCombatInfo>(targetEntity) ||
            em.HasComponent<StaticGridBlocker>(targetEntity))
        {
            return false;
        }

        targetOrderSystem ??= new UnitTargetOrderSystem();
        return targetOrderSystem.ValidateAttackTarget(em, targetEntity).Accepted;
    }

    public bool TryIssueScanOrder(Context context, Vector2 screenPosition)
    {
        context.SetExplicitAttackTargetModeActive?.Invoke(false);
        context.ApplyHudCommandMode?.Invoke(TacticalCommandMode.Scan);
        context.LogSelectionDiagnostic?.Invoke($"scanAttempt pos={screenPosition} frame={Time.frameCount}");

        bool queued = TryQueueResolvedScanCommand(context, screenPosition, out bool queuedResolvedTarget);
        if (!queued)
        {
            context.LogSelectionDiagnostic?.Invoke($"scanAttempt result=False reason=QueueFailed pos={screenPosition} frame={Time.frameCount}");
            context.ApplyHudCommandResult?.Invoke(TacticalCommandResult.Rejected(TacticalCommandReasonCode.ScanUnavailable));
            return false;
        }

        if (!queuedResolvedTarget)
            return context.ProcessScanCommandRequests?.Invoke() == true;

        return true;
    }

    private bool TryQueueResolvedScanCommand(Context context, Vector2 screenPosition, out bool queuedResolvedTarget)
    {
        queuedResolvedTarget = false;
        int frame = Time.frameCount;
        if (context.TryGetEntityManager == null ||
            !context.TryGetEntityManager(out EntityManager em))
        {
            return context.InputSystem.QueueScanCommandRequest(screenPosition, frame);
        }

        PointerTargetBoundaryPass targetBoundary = CreatePointerTargetBoundaryPass(context);
        if (!targetBoundary.TryGetClickedCell(screenPosition, em, out int2 targetCell, out Vector3 worldPoint))
            return context.InputSystem.QueueScanCommandRequest(screenPosition, frame);

        queuedResolvedTarget = context.InputSystem.QueueScanCommandRequest(screenPosition, targetCell, worldPoint, frame);
        return queuedResolvedTarget;
    }

    public bool TryIssueBoardTransportOrderToClickedUnit(Context context, Vector2 screenPosition)
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
        int frame = Time.frameCount;
        if (context.TryGetEntityManager == null ||
            !context.TryGetEntityManager(out EntityManager em))
        {
            return context.InputSystem.QueueBoardTransportCommandRequest(screenPosition, frame);
        }

        PointerTargetBoundaryPass targetBoundary = CreatePointerTargetBoundaryPass(context);
        if (!context.TransportBoardingCommandSystem.TryResolveBoardablePlayerTransportClick(
                em,
                screenPosition,
                context.UnitTransportBoardingRuleSystem,
                context.UnitTransportBoardingQuerySystem,
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
            context.UnitTransportBoardingRuleSystem,
            context.UnitTransportBoardingQuerySystem,
            (Vector2 position, EntityManager entityManager, out Entity entity) => targetBoundary.TryGetClickedUnitEntity(position, entityManager, out entity),
            (Vector2 position, EntityManager entityManager, out int2 cell, out Vector3 worldPoint) => targetBoundary.TryGetClickedCell(position, entityManager, out cell, out worldPoint));
    }

    public bool TryIssueMoveOrderToBuilding(Context context, Vector2Int originCell, Vector2Int footprintCells)
    {
        if (!context.TryGetEntityManager(out EntityManager em))
            return false;

        bool issued = context.BuildingTargetMoveOrderSystem.TryIssueMoveOrderToBuilding(
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
            context.LogSelectionDiagnostic?.Invoke($"focusAttempt result=False reason=NoEntityManager pos={screenPosition} frame={Time.frameCount}");
            return false;
        }

        context.LogSelectionDiagnostic?.Invoke($"focusAttempt pos={screenPosition} frame={Time.frameCount}");
        PointerTargetBoundaryPass targetBoundary = CreatePointerTargetBoundaryPass(context);
        if (!context.FocusedUnitLifecycleSystem.TryFocusUnit(
                em,
                screenPosition,
                context.SelectionStateSystem,
                context.UnitTargetOrderSystem,
                (Vector2 position, EntityManager entityManager, out Entity entity) => targetBoundary.TryGetClickedUnitEntity(position, entityManager, out entity),
                "TryFocusUnit",
                "TryFocusUnit",
                context.LogSelectionDiagnostic,
                context.DescribeEntity,
                context.ClearHudSelection,
                context.ApplyHudSelection,
                out _))
        {
            context.LogSelectionDiagnostic?.Invoke($"focusAttempt result=False reason=TryFocusUnitFailed pos={screenPosition} frame={Time.frameCount}");
            return false;
        }

        context.BuildingPlacementInteractionSystem?.ClearSelectedBuilding(context.BuildingPlacementInteractionContext, "RTSSelection.TryFocusUnit");
        context.InputSystem.ClearQueuedMoveOrder();
        int removedMoveCommands = context.InputSystem.ClearPendingMoveCommandRequests();
        context.InputSystem.IgnoreWorldCommandsUntilFrame = Time.frameCount + 1;
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
        if (!FactionIdentitySystem.IsHostileToPlayer(faction.Id))
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
        if (!_mapSurfaceCommandTargetSystem.TryResolveCommandTarget(
                em,
                _mapSurfaceQuery,
                grid,
                context.WorldCamera,
                screenPosition,
                context.SelectionStateSystem,
                out MapSurfaceCommandTargetSystem.Result target))
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
        if (!_mapSurfaceCommandTargetSystem.TryResolveMoveCommandTarget(
                em,
                _mapSurfaceQuery,
                gridEntity,
                grid,
                context.WorldCamera,
                screenPosition,
                context.SelectionStateSystem,
                out MapSurfaceCommandTargetSystem.Result target))
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

    private void EnsureEntityQueries(EntityManager em)
    {
        World world = em.World;
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
