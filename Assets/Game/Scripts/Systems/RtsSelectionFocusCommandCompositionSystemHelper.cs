using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public sealed class RtsSelectionFocusCommandCompositionSystemHelper
{
    public delegate bool TryGetEntityManagerDelegate(out EntityManager em);

    public struct Context
    {
        public RuntimeGameplayStateSystem RuntimeGameplayStateSystem;
        public readonly RtsSelectionInputCompositionSystemHelper InputSystem;
        public readonly SelectionStateCompositionSystemHelper SelectionStateCompositionSystemHelper;
        public readonly FocusedUnitLifecycleCompositionSystemHelper FocusedUnitLifecycleCompositionSystemHelper;
        public readonly BuildingPlacementInteractionCompositionSystemHelper BuildingPlacementInteractionCompositionSystemHelper;
        public readonly BuildingPlacementInteractionCompositionSystemHelper.Context BuildingPlacementInteractionContext;
        public readonly Camera WorldCamera;
        public readonly TryGetEntityManagerDelegate TryGetEntityManager;
        public readonly Action<EntityManager> EnsureEntityQueries;
        public readonly Action<EntityManager, string> ClearCurrentSelection;
        public readonly Action<Rect, RtsSelectionPointerRequestKind, VisibleUnitSelectionCameraSystemHelper.Filter> QueueSelectionRectangleRequest;
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
        public readonly FocusedUnitLifecycleCompositionSystemHelper.DescribeEntityDelegate DescribeEntity;
        public readonly Func<Vector2, bool> TryFocusScreenPosition;

        public Context(
            RuntimeGameplayStateSystem runtimeGameplayStateSystem,
            RtsSelectionInputCompositionSystemHelper inputSystem,
            SelectionStateCompositionSystemHelper selectionStateSystem,
            FocusedUnitLifecycleCompositionSystemHelper focusedUnitLifecycleSystem,
            BuildingPlacementInteractionCompositionSystemHelper buildingPlacementInteractionSystem,
            BuildingPlacementInteractionCompositionSystemHelper.Context buildingPlacementInteractionContext,
            Camera worldCamera,
            TryGetEntityManagerDelegate tryGetEntityManager,
            Action<EntityManager> ensureEntityQueries,
            Action<EntityManager, string> clearCurrentSelection,
            Action<Rect, RtsSelectionPointerRequestKind, VisibleUnitSelectionCameraSystemHelper.Filter> queueSelectionRectangleRequest,
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
            FocusedUnitLifecycleCompositionSystemHelper.DescribeEntityDelegate describeEntity,
            Func<Vector2, bool> tryFocusScreenPosition)
        {
            RuntimeGameplayStateSystem = runtimeGameplayStateSystem;
            InputSystem = inputSystem;
            SelectionStateCompositionSystemHelper = selectionStateSystem;
            FocusedUnitLifecycleCompositionSystemHelper = focusedUnitLifecycleSystem;
            BuildingPlacementInteractionCompositionSystemHelper = buildingPlacementInteractionSystem;
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
            TryFocusScreenPosition = tryFocusScreenPosition;
        }
    }

    private readonly List<RtsSelectionCommandIntentRequestElement> _externalSelectionCommandScratch = new();

    public bool QueueFocusUnitCommand(Context context, Vector2 screenPosition)
    {
        if (context.InputSystem == null ||
            !context.InputSystem.QueueFocusUnitCommandRequest(screenPosition, UnityEngine.Time.frameCount))
        {
            context.LogSelectionDiagnostic?.Invoke($"focusCommandEnqueue result=False pos={screenPosition} frame={UnityEngine.Time.frameCount}");
            return false;
        }

        bool processed = ProcessExternalSelectionCommandRequests(context);
        context.LogSelectionDiagnostic?.Invoke($"focusCommandProcessed result={processed} pos={screenPosition} frame={UnityEngine.Time.frameCount}");
        return processed;
    }

    public bool ProcessExternalSelectionCommandRequests(Context context)
    {
        if (!context.InputSystem.TryGetCommandBuffers(
                out _,
                out DynamicBuffer<RtsSelectionCommandIntentRequestElement> commandRequests,
                out _))
        {
            SelectionRuntimeDiagnosticsSystemHelper.LogMoveCommandTrace(
                $"externalSelectionCommandsNoBuffers frame={UnityEngine.Time.frameCount}");
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
        {
            processedAny |= ProcessExternalSelectionCommand(context, _externalSelectionCommandScratch[i]);
        }

        return processedAny;
    }

    public void ClearFocusedUnit(Context context)
    {
        context.FocusedUnitLifecycleCompositionSystemHelper.ClearFocusedUnit(context.SelectionStateCompositionSystemHelper);
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
            context.FocusedUnitLifecycleCompositionSystemHelper.ClearFocusedUnit(context.SelectionStateCompositionSystemHelper);
            context.SetExplicitAttackTargetModeActive?.Invoke(false);
            context.InputSystem.ClearActiveCommandMode();
            context.ClearHudSelection?.Invoke();
            context.ClearHudCommandMode?.Invoke();
            context.SetHudWorldMarkersVisible?.Invoke(false);
            return;
        }

        context.ClearCurrentSelection?.Invoke(em, reason);
        context.FocusedUnitLifecycleCompositionSystemHelper.ClearFocusedUnit(context.SelectionStateCompositionSystemHelper);
        context.SetExplicitAttackTargetModeActive?.Invoke(false);
        context.InputSystem.ClearActiveCommandMode();
        context.ClearHudSelection?.Invoke();
        context.ClearHudCommandMode?.Invoke();
        context.SetHudWorldMarkersVisible?.Invoke(false);
    }

    public void SelectAllVisiblePlayerUnits(Context context, VisibleUnitSelectionCameraSystemHelper.Filter filter)
    {
        context.SetExplicitAttackTargetModeActive?.Invoke(false);
        context.InputSystem.ClearActiveCommandMode();
        context.ClearHudCommandMode?.Invoke();
        context.SetHudWorldMarkersVisible?.Invoke(false);

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
        if (!context.FocusedUnitLifecycleCompositionSystemHelper.FocusUnitEntity(
                em,
                entity,
                context.SelectionStateCompositionSystemHelper,
                "FocusUnitEntity",
                "FocusUnitEntity",
                context.LogSelectionDiagnostic,
                context.DescribeEntity,
                context.ClearHudSelection,
                context.ApplyHudSelection))
        {
            return false;
        }

        context.BuildingPlacementInteractionCompositionSystemHelper?.ClearSelectedBuilding(context.BuildingPlacementInteractionContext, "RTSSelection.FocusUnitEntity");
        context.InputSystem.ClearActiveCommandMode();
        context.InputSystem.ClearQueuedMoveOrder();
        int removedMoveCommands = context.InputSystem.ClearPendingMoveCommandRequests();
        context.InputSystem.IgnoreNextLeftMouseRelease = true;
        context.InputSystem.IgnoreWorldCommandsUntilFrame = UnityEngine.Time.frameCount + 1;
        context.RuntimeGameplayStateSystem.SuppressNextWorldClick = false;
        context.SetCameraDragging?.Invoke(false);
        context.LogSelectionDiagnostic?.Invoke(
            $"focusEntityInputGuard entity={entity} frame={UnityEngine.Time.frameCount} " +
            $"ignoreRelease={context.InputSystem.IgnoreNextLeftMouseRelease} ignoreWorldUntil={context.InputSystem.IgnoreWorldCommandsUntilFrame} " +
            $"suppress={context.RuntimeGameplayStateSystem.SuppressNextWorldClick} clearedMoveCommands={removedMoveCommands}");
        return true;
    }

    public TacticalCommandResult TrySelectRuntimeEntity(Context context, Entity entity)
    {
        TacticalCommandResult result = ValidateControllableEntity(context, entity);
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

    private static TacticalCommandResult ValidateControllableEntity(Context context, Entity entity)
    {
        if (entity == Entity.Null ||
            context.TryGetEntityManager == null ||
            !context.TryGetEntityManager(out EntityManager em))
        {
            return TacticalCommandResult.Rejected(TacticalCommandReasonCode.TargetNotAttackable);
        }

        if (!em.Exists(entity) ||
            !em.HasComponent<Faction>(entity) ||
            !em.HasComponent<UnitMove>(entity))
        {
            return TacticalCommandResult.Rejected(TacticalCommandReasonCode.TargetNotAttackable);
        }

        if (!FactionIdentity.IsPlayerControlled(em.GetComponentData<Faction>(entity).Id))
            return TacticalCommandResult.Rejected(TacticalCommandReasonCode.TargetNotAttackable);

        if (em.HasComponent<UnitHealth>(entity) && em.GetComponentData<UnitHealth>(entity).Current <= 0)
            return TacticalCommandResult.Rejected(TacticalCommandReasonCode.TargetNotAttackable);

        return TacticalCommandResult.Success();
    }

    private static bool IsExternalSelectionCommand(RtsSelectionCommandIntentKind kind)
    {
        return kind == RtsSelectionCommandIntentKind.SelectAll ||
               kind == RtsSelectionCommandIntentKind.FocusUnit ||
               kind == RtsSelectionCommandIntentKind.SelectAllSoldiers ||
               kind == RtsSelectionCommandIntentKind.SelectAllVehicles ||
               kind == RtsSelectionCommandIntentKind.EnterSelectionMode ||
               kind == RtsSelectionCommandIntentKind.ExitSelectionMode ||
               kind == RtsSelectionCommandIntentKind.DeselectAll;
    }

    private bool ProcessExternalSelectionCommand(Context context, RtsSelectionCommandIntentRequestElement request)
    {
        switch (request.Kind)
        {
            case RtsSelectionCommandIntentKind.FocusUnit:
                return request.HasScreenPosition != 0 &&
                       context.TryFocusScreenPosition?.Invoke(new Vector2(request.ScreenPosition.x, request.ScreenPosition.y)) == true;
            case RtsSelectionCommandIntentKind.SelectAll:
                SelectAllVisiblePlayerUnits(context, VisibleUnitSelectionCameraSystemHelper.Filter.All);
                return true;
            case RtsSelectionCommandIntentKind.SelectAllSoldiers:
                SelectAllVisiblePlayerUnits(context, VisibleUnitSelectionCameraSystemHelper.Filter.Soldiers);
                return true;
            case RtsSelectionCommandIntentKind.SelectAllVehicles:
                SelectAllVisiblePlayerUnits(context, VisibleUnitSelectionCameraSystemHelper.Filter.Vehicles);
                return true;
            case RtsSelectionCommandIntentKind.EnterSelectionMode:
                EnterExplicitSelectionMode(context);
                return true;
            case RtsSelectionCommandIntentKind.ExitSelectionMode:
                ExitExplicitSelectionMode(context);
                return true;
            case RtsSelectionCommandIntentKind.DeselectAll:
                DeselectAllUnits(context, "SelectionUiCommandUiSystemHelper");
                return true;
            default:
                return false;
        }
    }

    private static void EnterExplicitSelectionMode(Context context)
    {
        context.SetExplicitAttackTargetModeActive?.Invoke(false);
        context.InputSystem.ClearActiveCommandMode();
        context.BuildingPlacementInteractionCompositionSystemHelper?.ClearSelectedBuilding(
            context.BuildingPlacementInteractionContext,
            "SelectionUiCommandUiSystemHelper.EnterSelectionMode");
        context.InputSystem.ClearQueuedMoveOrder();
        context.InputSystem.ClearPendingMoveCommandRequests();
        Vector2 pointerPosition = context.InputSystem.HasLastKnownPointerPosition
            ? context.InputSystem.LastKnownPointerPosition
            : Vector2.zero;
        context.InputSystem.ResetSelectionDragState(pointerPosition);
        context.InputSystem.IgnoreNextLeftMouseRelease = true;
        context.InputSystem.SkipNextWorldReleaseAfterSelection = true;
        context.InputSystem.IgnoreWorldCommandsUntilFrame = UnityEngine.Time.frameCount + 1;
        context.RuntimeGameplayStateSystem.SelectionModeActive = true;
        context.RuntimeGameplayStateSystem.SuppressNextWorldClick = true;
        context.SetCameraDragging?.Invoke(false);
        context.SetHudWorldMarkersVisible?.Invoke(false);
        context.ApplyHudCommandMode?.Invoke(TacticalCommandMode.Select);
        context.LogSelectionDiagnostic?.Invoke($"selectionModeEntered source=ui frame={UnityEngine.Time.frameCount} dragReset={pointerPosition}");
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
        context.InputSystem.IgnoreWorldCommandsUntilFrame = UnityEngine.Time.frameCount + 1;
        context.RuntimeGameplayStateSystem.SelectionModeActive = false;
        context.RuntimeGameplayStateSystem.SuppressNextWorldClick = true;
        context.SetCameraDragging?.Invoke(false);
        context.SetHudWorldMarkersVisible?.Invoke(false);
        context.ClearHudCommandMode?.Invoke();
        context.LogSelectionDiagnostic?.Invoke($"selectionModeExited source=ui frame={UnityEngine.Time.frameCount} dragReset={pointerPosition}");
    }

}
