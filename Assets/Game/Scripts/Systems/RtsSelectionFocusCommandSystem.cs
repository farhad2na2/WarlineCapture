using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

public sealed class RtsSelectionFocusCommandSystem
{
    public delegate bool TryGetEntityManagerDelegate(out EntityManager em);
    public delegate TacticalCommandResult ValidateControllableEntityDelegate(Entity entity);

    public readonly struct Context
    {
        public readonly RuntimeGameplayStateSystem RuntimeGameplayStateSystem;
        public readonly RtsSelectionInputSystem InputSystem;
        public readonly SelectionStateSystem SelectionStateSystem;
        public readonly FocusedUnitLifecycleSystem FocusedUnitLifecycleSystem;
        public readonly UnitTargetOrderSystem UnitTargetOrderSystem;
        public readonly BuildingPlacementInteractionSystem BuildingPlacementInteractionSystem;
        public readonly BuildingPlacementInteractionSystem.Context BuildingPlacementInteractionContext;
        public readonly Camera WorldCamera;
        public readonly TryGetEntityManagerDelegate TryGetEntityManager;
        public readonly Action<EntityManager> EnsureEntityQueries;
        public readonly Action<EntityManager, string> ClearCurrentSelection;
        public readonly Action<Rect, RtsSelectionPointerRequestKind, VisibleUnitSelectionSystem.Filter> QueueSelectionRectangleRequest;
        public readonly Action ProcessSelectionRectangleRequests;
        public readonly Action<EntityManager, Entity> ApplyHudSelection;
        public readonly Action<TacticalCommandResult> ApplyHudCommandResult;
        public readonly Action<TacticalCommandMode> ApplyHudCommandMode;
        public readonly Action ClearHudSelection;
        public readonly Action ClearHudCommandMode;
        public readonly Action<bool> SetHudWorldMarkersVisible;
        public readonly Action<bool> SetCameraDragging;
        public readonly Action<bool> SetExplicitAttackTargetModeActive;
        public readonly Action<string> LogSelectionDiagnostic;
        public readonly FocusedUnitLifecycleSystem.DescribeEntityDelegate DescribeEntity;
        public readonly ValidateControllableEntityDelegate ValidateControllableEntity;
        public readonly Action IssueHoldPositionOrder;
        public readonly Action IssueStopOrder;
        public readonly Action DestroyFocusedUnit;
        public readonly Func<Vector2, bool> TryFocusScreenPosition;
        public readonly Func<bool> IssueFocusedMissileLauncherRadarAttack;
        public readonly Func<bool> ArmFocusedAttackTargetMode;
        public readonly Action CancelExplicitAttackTargetMode;

        public Context(
            RuntimeGameplayStateSystem runtimeGameplayStateSystem,
            RtsSelectionInputSystem inputSystem,
            SelectionStateSystem selectionStateSystem,
            FocusedUnitLifecycleSystem focusedUnitLifecycleSystem,
            UnitTargetOrderSystem unitTargetOrderSystem,
            BuildingPlacementInteractionSystem buildingPlacementInteractionSystem,
            BuildingPlacementInteractionSystem.Context buildingPlacementInteractionContext,
            Camera worldCamera,
            TryGetEntityManagerDelegate tryGetEntityManager,
            Action<EntityManager> ensureEntityQueries,
            Action<EntityManager, string> clearCurrentSelection,
            Action<Rect, RtsSelectionPointerRequestKind, VisibleUnitSelectionSystem.Filter> queueSelectionRectangleRequest,
            Action processSelectionRectangleRequests,
            Action<EntityManager, Entity> applyHudSelection,
            Action<TacticalCommandResult> applyHudCommandResult,
            Action<TacticalCommandMode> applyHudCommandMode,
            Action clearHudSelection,
            Action clearHudCommandMode,
            Action<bool> setHudWorldMarkersVisible,
            Action<bool> setCameraDragging,
            Action<bool> setExplicitAttackTargetModeActive,
            Action<string> logSelectionDiagnostic,
            FocusedUnitLifecycleSystem.DescribeEntityDelegate describeEntity,
            ValidateControllableEntityDelegate validateControllableEntity,
            Action issueHoldPositionOrder,
            Action issueStopOrder,
            Action destroyFocusedUnit,
            Func<Vector2, bool> tryFocusScreenPosition,
            Func<bool> issueFocusedMissileLauncherRadarAttack,
            Func<bool> armFocusedAttackTargetMode,
            Action cancelExplicitAttackTargetMode)
        {
            RuntimeGameplayStateSystem = runtimeGameplayStateSystem;
            InputSystem = inputSystem;
            SelectionStateSystem = selectionStateSystem;
            FocusedUnitLifecycleSystem = focusedUnitLifecycleSystem;
            UnitTargetOrderSystem = unitTargetOrderSystem;
            BuildingPlacementInteractionSystem = buildingPlacementInteractionSystem;
            BuildingPlacementInteractionContext = buildingPlacementInteractionContext;
            WorldCamera = worldCamera;
            TryGetEntityManager = tryGetEntityManager;
            EnsureEntityQueries = ensureEntityQueries;
            ClearCurrentSelection = clearCurrentSelection;
            QueueSelectionRectangleRequest = queueSelectionRectangleRequest;
            ProcessSelectionRectangleRequests = processSelectionRectangleRequests;
            ApplyHudSelection = applyHudSelection;
            ApplyHudCommandResult = applyHudCommandResult;
            ApplyHudCommandMode = applyHudCommandMode;
            ClearHudSelection = clearHudSelection;
            ClearHudCommandMode = clearHudCommandMode;
            SetHudWorldMarkersVisible = setHudWorldMarkersVisible;
            SetCameraDragging = setCameraDragging;
            SetExplicitAttackTargetModeActive = setExplicitAttackTargetModeActive;
            LogSelectionDiagnostic = logSelectionDiagnostic;
            DescribeEntity = describeEntity;
            ValidateControllableEntity = validateControllableEntity;
            IssueHoldPositionOrder = issueHoldPositionOrder;
            IssueStopOrder = issueStopOrder;
            DestroyFocusedUnit = destroyFocusedUnit;
            TryFocusScreenPosition = tryFocusScreenPosition;
            IssueFocusedMissileLauncherRadarAttack = issueFocusedMissileLauncherRadarAttack;
            ArmFocusedAttackTargetMode = armFocusedAttackTargetMode;
            CancelExplicitAttackTargetMode = cancelExplicitAttackTargetMode;
        }
    }

    private readonly List<RtsSelectionCommandIntentRequestElement> _externalSelectionCommandScratch = new();

    public bool ProcessExternalSelectionCommandRequests(Context context)
    {
        if (!context.InputSystem.TryGetCommandBuffers(
                out _,
                out DynamicBuffer<RtsSelectionCommandIntentRequestElement> commandRequests,
                out _))
        {
            return false;
        }

        _externalSelectionCommandScratch.Clear();
        for (int i = 0; i < commandRequests.Length;)
        {
            RtsSelectionCommandIntentRequestElement request = commandRequests[i];
            if (!IsExternalSelectionCommand(request.Kind))
            {
                i++;
                continue;
            }

            commandRequests.RemoveAt(i);
            _externalSelectionCommandScratch.Add(request);
        }

        bool processedAny = false;
        for (int i = 0; i < _externalSelectionCommandScratch.Count; i++)
            processedAny |= ProcessExternalSelectionCommand(context, _externalSelectionCommandScratch[i]);
        return processedAny;
    }

    public void ClearFocusedUnit(Context context)
    {
        context.FocusedUnitLifecycleSystem.ClearFocusedUnit(context.SelectionStateSystem);
        context.SetExplicitAttackTargetModeActive?.Invoke(false);
        context.InputSystem.ClearActiveCommandMode();
        context.ClearHudSelection?.Invoke();
        context.ClearHudCommandMode?.Invoke();
        context.SetHudWorldMarkersVisible?.Invoke(false);
    }

    public void DeselectAllUnits(Context context, string reason)
    {
        if (!context.TryGetEntityManager(out EntityManager em))
        {
            context.FocusedUnitLifecycleSystem.ClearFocusedUnit(context.SelectionStateSystem);
            context.SetExplicitAttackTargetModeActive?.Invoke(false);
            context.InputSystem.ClearActiveCommandMode();
            context.ClearHudSelection?.Invoke();
            context.ClearHudCommandMode?.Invoke();
            context.SetHudWorldMarkersVisible?.Invoke(false);
            return;
        }

        context.ClearCurrentSelection?.Invoke(em, reason);
        context.FocusedUnitLifecycleSystem.ClearFocusedUnit(context.SelectionStateSystem);
        context.SetExplicitAttackTargetModeActive?.Invoke(false);
        context.InputSystem.ClearActiveCommandMode();
        context.ClearHudSelection?.Invoke();
        context.ClearHudCommandMode?.Invoke();
        context.SetHudWorldMarkersVisible?.Invoke(false);
    }

    public void SelectAllVisiblePlayerUnits(Context context, VisibleUnitSelectionSystem.Filter filter)
    {
        if (context.WorldCamera == null)
        {
            context.LogSelectionDiagnostic?.Invoke($"result=SelectAllSkipped reason=NoCamera filter={filter}");
            return;
        }

        context.QueueSelectionRectangleRequest?.Invoke(
            new Rect(0f, 0f, Screen.width, Screen.height),
            RtsSelectionPointerRequestKind.SelectionRectCommitted,
            filter);
        context.ProcessSelectionRectangleRequests?.Invoke();
        context.InputSystem.IgnoreNextLeftMouseRelease = false;
        context.InputSystem.SkipNextWorldReleaseAfterSelection = false;
        context.SetCameraDragging?.Invoke(false);
    }

    public bool FocusUnitEntity(Context context, Entity entity)
    {
        if (entity == Entity.Null || !context.TryGetEntityManager(out EntityManager em))
            return false;

        context.EnsureEntityQueries?.Invoke(em);
        if (!context.FocusedUnitLifecycleSystem.FocusUnitEntity(
                em,
                entity,
                context.SelectionStateSystem,
                context.UnitTargetOrderSystem,
                "FocusUnitEntity",
                "FocusUnitEntity",
                context.LogSelectionDiagnostic,
                context.DescribeEntity,
                context.ClearHudSelection,
                context.ApplyHudSelection))
        {
            return false;
        }

        context.BuildingPlacementInteractionSystem?.ClearSelectedBuilding(context.BuildingPlacementInteractionContext, "RTSSelection.FocusUnitEntity");
        context.InputSystem.ClearActiveCommandMode();
        context.InputSystem.ClearQueuedMoveOrder();
        int removedMoveCommands = context.InputSystem.ClearPendingMoveCommandRequests();
        context.InputSystem.IgnoreNextLeftMouseRelease = true;
        context.InputSystem.IgnoreWorldCommandsUntilFrame = Time.frameCount + 1;
        context.RuntimeGameplayStateSystem.SuppressNextWorldClick = false;
        context.SetCameraDragging?.Invoke(false);
        context.LogSelectionDiagnostic?.Invoke(
            $"focusEntityInputGuard entity={entity} frame={Time.frameCount} " +
            $"ignoreRelease={context.InputSystem.IgnoreNextLeftMouseRelease} ignoreWorldUntil={context.InputSystem.IgnoreWorldCommandsUntilFrame} " +
            $"suppress={context.RuntimeGameplayStateSystem.SuppressNextWorldClick} clearedMoveCommands={removedMoveCommands}");
        return true;
    }

    public TacticalCommandResult TrySelectRuntimeEntity(Context context, Entity entity)
    {
        TacticalCommandResult result = context.ValidateControllableEntity(entity);
        if (!result.Accepted)
        {
            context.ApplyHudCommandResult?.Invoke(result);
            return result;
        }

        result = FocusUnitEntity(context, entity)
            ? TacticalCommandResult.Success()
            : TacticalCommandResult.Rejected(TacticalCommandReasonCode.TargetNotAttackable);
        context.ApplyHudCommandResult?.Invoke(result);
        return result;
    }

    private static bool IsExternalSelectionCommand(RtsSelectionCommandIntentKind kind)
    {
        return kind == RtsSelectionCommandIntentKind.SelectAll ||
               kind == RtsSelectionCommandIntentKind.FocusUnit ||
               kind == RtsSelectionCommandIntentKind.SelectAllSoldiers ||
               kind == RtsSelectionCommandIntentKind.SelectAllVehicles ||
               kind == RtsSelectionCommandIntentKind.EnterSelectionMode ||
               kind == RtsSelectionCommandIntentKind.ExitSelectionMode ||
               kind == RtsSelectionCommandIntentKind.DeselectAll ||
               kind == RtsSelectionCommandIntentKind.EnterMoveTargetMode ||
               kind == RtsSelectionCommandIntentKind.EnterAttackTargetMode ||
               kind == RtsSelectionCommandIntentKind.EnterScanTargetMode ||
               kind == RtsSelectionCommandIntentKind.HoldPosition ||
               kind == RtsSelectionCommandIntentKind.Stop ||
               kind == RtsSelectionCommandIntentKind.DestroyFocusedUnit ||
               kind == RtsSelectionCommandIntentKind.ToggleAttackTargetMode ||
               kind == RtsSelectionCommandIntentKind.CancelAttackTargetMode;
    }

    private bool ProcessExternalSelectionCommand(Context context, RtsSelectionCommandIntentRequestElement request)
    {
        switch (request.Kind)
        {
            case RtsSelectionCommandIntentKind.FocusUnit:
                return request.HasScreenPosition != 0 &&
                       context.TryFocusScreenPosition?.Invoke(new Vector2(request.ScreenPosition.x, request.ScreenPosition.y)) == true;
            case RtsSelectionCommandIntentKind.SelectAll:
                SelectAllVisiblePlayerUnits(context, VisibleUnitSelectionSystem.Filter.All);
                return true;
            case RtsSelectionCommandIntentKind.SelectAllSoldiers:
                SelectAllVisiblePlayerUnits(context, VisibleUnitSelectionSystem.Filter.Soldiers);
                return true;
            case RtsSelectionCommandIntentKind.SelectAllVehicles:
                SelectAllVisiblePlayerUnits(context, VisibleUnitSelectionSystem.Filter.Vehicles);
                return true;
            case RtsSelectionCommandIntentKind.EnterSelectionMode:
                EnterExplicitSelectionMode(context);
                return true;
            case RtsSelectionCommandIntentKind.ExitSelectionMode:
                ExitExplicitSelectionMode(context);
                return true;
            case RtsSelectionCommandIntentKind.DeselectAll:
                DeselectAllUnits(context, "SelectionUiCommandSystem");
                return true;
            case RtsSelectionCommandIntentKind.EnterMoveTargetMode:
                EnterMoveTargetMode(context);
                return true;
            case RtsSelectionCommandIntentKind.EnterAttackTargetMode:
                EnterAttackTargetMode(context);
                return true;
            case RtsSelectionCommandIntentKind.EnterScanTargetMode:
                EnterScanTargetMode(context);
                return true;
            case RtsSelectionCommandIntentKind.HoldPosition:
                context.InputSystem.ClearActiveCommandMode();
                context.IssueHoldPositionOrder?.Invoke();
                return true;
            case RtsSelectionCommandIntentKind.Stop:
                context.InputSystem.ClearActiveCommandMode();
                context.IssueStopOrder?.Invoke();
                return true;
            case RtsSelectionCommandIntentKind.DestroyFocusedUnit:
                context.DestroyFocusedUnit?.Invoke();
                return true;
            case RtsSelectionCommandIntentKind.ToggleAttackTargetMode:
                if (context.IssueFocusedMissileLauncherRadarAttack == null ||
                    !context.IssueFocusedMissileLauncherRadarAttack())
                {
                    context.ArmFocusedAttackTargetMode?.Invoke();
                }
                return true;
            case RtsSelectionCommandIntentKind.CancelAttackTargetMode:
                context.CancelExplicitAttackTargetMode?.Invoke();
                return true;
            default:
                return false;
        }
    }

    private static void EnterExplicitSelectionMode(Context context)
    {
        context.SetExplicitAttackTargetModeActive?.Invoke(false);
        context.InputSystem.ClearActiveCommandMode();
        context.BuildingPlacementInteractionSystem?.ClearSelectedBuilding(
            context.BuildingPlacementInteractionContext,
            "SelectionUiCommandSystem.EnterSelectionMode");
        context.InputSystem.ClearQueuedMoveOrder();
        context.InputSystem.ClearPendingMoveCommandRequests();
        Vector2 pointerPosition = context.InputSystem.HasLastKnownPointerPosition
            ? context.InputSystem.LastKnownPointerPosition
            : Vector2.zero;
        context.InputSystem.ResetSelectionDragState(pointerPosition);
        context.InputSystem.IgnoreNextLeftMouseRelease = true;
        context.InputSystem.SkipNextWorldReleaseAfterSelection = true;
        context.InputSystem.IgnoreWorldCommandsUntilFrame = Time.frameCount + 1;
        context.RuntimeGameplayStateSystem.SelectionModeActive = true;
        context.RuntimeGameplayStateSystem.SuppressNextWorldClick = true;
        context.SetCameraDragging?.Invoke(false);
        context.SetHudWorldMarkersVisible?.Invoke(false);
        context.ApplyHudCommandMode?.Invoke(TacticalCommandMode.Select);
        context.LogSelectionDiagnostic?.Invoke($"selectionModeEntered source=ui frame={Time.frameCount} dragReset={pointerPosition}");
    }

    private static void ExitExplicitSelectionMode(Context context)
    {
        context.InputSystem.ClearActiveCommandMode();
        Vector2 pointerPosition = context.InputSystem.HasLastKnownPointerPosition
            ? context.InputSystem.LastKnownPointerPosition
            : Vector2.zero;
        context.InputSystem.ResetSelectionDragState(pointerPosition);
        context.InputSystem.IgnoreNextLeftMouseRelease = true;
        context.InputSystem.SkipNextWorldReleaseAfterSelection = false;
        context.InputSystem.IgnoreWorldCommandsUntilFrame = Time.frameCount + 1;
        context.RuntimeGameplayStateSystem.SelectionModeActive = false;
        context.RuntimeGameplayStateSystem.SuppressNextWorldClick = true;
        context.SetCameraDragging?.Invoke(false);
        context.SetHudWorldMarkersVisible?.Invoke(false);
        context.ClearHudCommandMode?.Invoke();
        context.LogSelectionDiagnostic?.Invoke($"selectionModeExited source=ui frame={Time.frameCount} dragReset={pointerPosition}");
    }

    private static void EnterMoveTargetMode(Context context)
    {
        context.SetExplicitAttackTargetModeActive?.Invoke(false);
        context.BuildingPlacementInteractionSystem?.ClearSelectedBuilding(
            context.BuildingPlacementInteractionContext,
            "SelectionUiCommandSystem.EnterMoveTargetMode");
        context.InputSystem.ClearQueuedMoveOrder();
        context.InputSystem.ClearPendingMoveCommandRequests();

        if (!TryHasSelectedMovableUnit(context))
        {
            context.InputSystem.ClearActiveCommandMode();
            context.ClearHudCommandMode?.Invoke();
            context.ApplyHudCommandResult?.Invoke(TacticalCommandResult.Rejected(TacticalCommandReasonCode.NoSelection));
            context.SetCameraDragging?.Invoke(false);
            context.LogSelectionDiagnostic?.Invoke($"moveModeEntered result=False reason=NoSelection frame={Time.frameCount}");
            return;
        }

        Vector2 pointerPosition = context.InputSystem.HasLastKnownPointerPosition
            ? context.InputSystem.LastKnownPointerPosition
            : Vector2.zero;
        context.InputSystem.ResetSelectionDragState(pointerPosition);
        context.InputSystem.IgnoreNextLeftMouseRelease = true;
        context.InputSystem.SkipNextWorldReleaseAfterSelection = true;
        context.InputSystem.IgnoreWorldCommandsUntilFrame = Time.frameCount + 1;
        context.InputSystem.ArmCommandMode(
            TacticalCommandMode.Move,
            Time.frameCount,
            oneShot: true,
            requiresWorldTarget: true);
        context.RuntimeGameplayStateSystem.SelectionModeActive = false;
        context.RuntimeGameplayStateSystem.SuppressNextWorldClick = true;
        context.SetCameraDragging?.Invoke(false);
        context.SetHudWorldMarkersVisible?.Invoke(false);
        context.ApplyHudCommandMode?.Invoke(TacticalCommandMode.Move);
        context.LogSelectionDiagnostic?.Invoke($"moveModeEntered result=True frame={Time.frameCount} dragReset={pointerPosition}");
    }

    private static void EnterAttackTargetMode(Context context)
    {
        context.SetExplicitAttackTargetModeActive?.Invoke(false);
        context.BuildingPlacementInteractionSystem?.ClearSelectedBuilding(
            context.BuildingPlacementInteractionContext,
            "SelectionUiCommandSystem.EnterAttackTargetMode");
        context.InputSystem.ClearQueuedMoveOrder();
        context.InputSystem.ClearPendingMoveCommandRequests();

        if (!TryHasSelectedAttackCapableUnit(context, out TacticalCommandReasonCode rejectionReason))
        {
            context.InputSystem.ClearActiveCommandMode();
            context.ClearHudCommandMode?.Invoke();
            context.ApplyHudCommandResult?.Invoke(TacticalCommandResult.Rejected(rejectionReason));
            context.SetCameraDragging?.Invoke(false);
            context.SetHudWorldMarkersVisible?.Invoke(false);
            context.LogSelectionDiagnostic?.Invoke($"attackModeEntered result=False reason={rejectionReason} frame={Time.frameCount}");
            return;
        }

        Vector2 pointerPosition = context.InputSystem.HasLastKnownPointerPosition
            ? context.InputSystem.LastKnownPointerPosition
            : Vector2.zero;
        context.InputSystem.ResetSelectionDragState(pointerPosition);
        context.InputSystem.IgnoreNextLeftMouseRelease = true;
        context.InputSystem.SkipNextWorldReleaseAfterSelection = true;
        context.InputSystem.IgnoreWorldCommandsUntilFrame = Time.frameCount + 1;
        context.InputSystem.ArmCommandMode(
            TacticalCommandMode.Attack,
            Time.frameCount,
            oneShot: true,
            requiresWorldTarget: true);
        context.RuntimeGameplayStateSystem.SelectionModeActive = false;
        context.RuntimeGameplayStateSystem.SuppressNextWorldClick = true;
        context.SetExplicitAttackTargetModeActive?.Invoke(true);
        context.SetCameraDragging?.Invoke(false);
        context.SetHudWorldMarkersVisible?.Invoke(true);
        context.ApplyHudCommandMode?.Invoke(TacticalCommandMode.Attack);
        context.LogSelectionDiagnostic?.Invoke($"attackModeEntered result=True frame={Time.frameCount} dragReset={pointerPosition}");
    }

    private static void EnterScanTargetMode(Context context)
    {
        context.SetExplicitAttackTargetModeActive?.Invoke(false);
        context.BuildingPlacementInteractionSystem?.ExitBuildMode(context.BuildingPlacementInteractionContext);
        context.BuildingPlacementInteractionSystem?.CancelBuildingPlacement(context.BuildingPlacementInteractionContext);
        context.BuildingPlacementInteractionSystem?.ClearSelectedBuilding(
            context.BuildingPlacementInteractionContext,
            "SelectionUiCommandSystem.EnterScanTargetMode");
        context.InputSystem.ClearQueuedMoveOrder();
        context.InputSystem.ClearPendingMoveCommandRequests();

        Vector2 pointerPosition = context.InputSystem.HasLastKnownPointerPosition
            ? context.InputSystem.LastKnownPointerPosition
            : Vector2.zero;
        context.InputSystem.ResetSelectionDragState(pointerPosition);
        context.InputSystem.IgnoreNextLeftMouseRelease = true;
        context.InputSystem.SkipNextWorldReleaseAfterSelection = true;
        context.InputSystem.IgnoreWorldCommandsUntilFrame = Time.frameCount + 1;
        context.InputSystem.ArmCommandMode(
            TacticalCommandMode.Scan,
            Time.frameCount,
            oneShot: true,
            requiresWorldTarget: true);
        context.RuntimeGameplayStateSystem.SelectionModeActive = false;
        context.RuntimeGameplayStateSystem.SuppressNextWorldClick = true;
        context.SetCameraDragging?.Invoke(false);
        context.SetHudWorldMarkersVisible?.Invoke(false);
        context.ApplyHudCommandMode?.Invoke(TacticalCommandMode.Scan);
        context.LogSelectionDiagnostic?.Invoke($"scanModeEntered result=True frame={Time.frameCount} dragReset={pointerPosition}");
    }

    private static bool TryHasSelectedMovableUnit(Context context)
    {
        if (!context.TryGetEntityManager(out EntityManager em))
            return false;

        context.EnsureEntityQueries?.Invoke(em);
        List<Entity> cached = context.SelectionStateSystem.CachedSelectedMoveEntities;
        for (int i = 0; i < cached.Count; i++)
        {
            if (SelectionStateSystem.IsCacheableSelectedMoveEntity(em, cached[i]))
                return true;
        }

        using EntityQuery query = em.CreateEntityQuery(
            ComponentType.ReadOnly<SelectedUnitTag>(),
            ComponentType.ReadOnly<UnitGrid>(),
            ComponentType.ReadOnly<UnitMove>());
        if (query.IsEmptyIgnoreFilter)
            return false;

        using NativeArray<Entity> selectedEntities = query.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < selectedEntities.Length; i++)
        {
            if (SelectionStateSystem.IsCacheableSelectedMoveEntity(em, selectedEntities[i]))
                return true;
        }

        return false;
    }

    private static bool TryHasSelectedAttackCapableUnit(
        Context context,
        out TacticalCommandReasonCode rejectionReason)
    {
        rejectionReason = TacticalCommandReasonCode.NoSelection;
        if (!context.TryGetEntityManager(out EntityManager em))
            return false;

        context.EnsureEntityQueries?.Invoke(em);
        bool hasSelected = false;
        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<SelectedUnitTag>());
        if (query.IsEmptyIgnoreFilter)
            return false;

        using NativeArray<Entity> selectedEntities = query.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < selectedEntities.Length; i++)
        {
            Entity entity = selectedEntities[i];
            if (!em.Exists(entity))
                continue;

            hasSelected = true;
            if (IsSelectedAttackCapableUnit(em, entity))
                return true;
        }

        rejectionReason = hasSelected
            ? TacticalCommandReasonCode.TargetNotAttackable
            : TacticalCommandReasonCode.NoSelection;
        return false;
    }

    private static bool IsSelectedAttackCapableUnit(EntityManager em, Entity entity)
    {
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
}
