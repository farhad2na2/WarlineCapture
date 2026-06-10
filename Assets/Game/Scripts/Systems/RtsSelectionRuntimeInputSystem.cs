using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public sealed class RtsSelectionRuntimeInputSystem
{
    public delegate bool TryGetEntityManagerDelegate(out EntityManager em);

    public readonly struct Context
    {
        public readonly RuntimeGameplayStateSystem RuntimeGameplayStateSystem;
        public readonly RtsSelectionInputSystem InputSystem;
        public readonly MainMenuPlayUI MainMenuPlayUi;
        public readonly float DragThresholdPixels;
        public readonly float SelectionModeHoldSeconds;
        public readonly Func<bool> GetExplicitAttackTargetModeActive;
        public readonly Action<bool> SetExplicitAttackTargetModeActive;
        public readonly Func<bool> GetCameraDragging;
        public readonly Action<bool> SetCameraDragging;
        public readonly Func<Vector2, bool> IsPointerOverAnyUi;
        public readonly Func<Vector2, bool> IsPointerOverGameplayUi;
        public readonly Func<Vector2, bool> TryIssueAttackOrderToClickedUnit;
        public readonly Func<Vector2, bool> TryIssueScanOrder;
        public readonly SelectionOrderMarkerSystem OrderMarkerSystem;
        public readonly TryGetEntityManagerDelegate TryGetDefaultEntityManager;
        public readonly SelectedMoveOrderCommandSystem.ClickedCellResolver TryGetScanClickedCell;
        public readonly Action<bool> SetHudWorldMarkersVisible;
        public readonly Func<Vector2, bool> TryIssueBoardTransportOrderToClickedUnit;
        public readonly Func<Entity, Vector2, bool> TryIssueBoardSelectedTransportOrderToClickedUnit;
        public readonly Func<Entity, Rect, bool> TryIssueBoardSelectedTransportOrderToPassengerRect;
        public readonly Func<Vector2, bool> TryFocusUnit;
        public readonly Action<Vector2> PanCamera;
        public readonly Action<Vector2> IssueMoveOrder;
        public readonly Action ProcessSelectionRectangleRequests;
        public readonly Action ClearCommandMode;
        public readonly Action<string> LogClickDiagnostic;
        public readonly Func<Vector2, string> BuildClickDebugSummary;
        public readonly Func<bool> IsGameplayInputLocked;

        public Context(
            RuntimeGameplayStateSystem runtimeGameplayStateSystem,
            RtsSelectionInputSystem inputSystem,
            MainMenuPlayUI mainMenuPlayUi,
            float dragThresholdPixels,
            float selectionModeHoldSeconds,
            Func<bool> getExplicitAttackTargetModeActive,
            Action<bool> setExplicitAttackTargetModeActive,
            Func<bool> getCameraDragging,
            Action<bool> setCameraDragging,
            Func<Vector2, bool> isPointerOverAnyUi,
            Func<Vector2, bool> isPointerOverGameplayUi,
            Func<Vector2, bool> tryIssueAttackOrderToClickedUnit,
            Func<Vector2, bool> tryIssueScanOrder,
            SelectionOrderMarkerSystem orderMarkerSystem,
            TryGetEntityManagerDelegate tryGetDefaultEntityManager,
            SelectedMoveOrderCommandSystem.ClickedCellResolver tryGetScanClickedCell,
            Action<bool> setHudWorldMarkersVisible,
            Func<Vector2, bool> tryIssueBoardTransportOrderToClickedUnit,
            Func<Entity, Vector2, bool> tryIssueBoardSelectedTransportOrderToClickedUnit,
            Func<Entity, Rect, bool> tryIssueBoardSelectedTransportOrderToPassengerRect,
            Func<Vector2, bool> tryFocusUnit,
            Action<Vector2> panCamera,
            Action<Vector2> issueMoveOrder,
            Action processSelectionRectangleRequests,
            Action clearCommandMode,
            Action<string> logClickDiagnostic,
            Func<Vector2, string> buildClickDebugSummary,
            Func<bool> isGameplayInputLocked)
        {
            RuntimeGameplayStateSystem = runtimeGameplayStateSystem;
            InputSystem = inputSystem;
            MainMenuPlayUi = mainMenuPlayUi;
            DragThresholdPixels = dragThresholdPixels;
            SelectionModeHoldSeconds = selectionModeHoldSeconds;
            GetExplicitAttackTargetModeActive = getExplicitAttackTargetModeActive;
            SetExplicitAttackTargetModeActive = setExplicitAttackTargetModeActive;
            GetCameraDragging = getCameraDragging;
            SetCameraDragging = setCameraDragging;
            IsPointerOverAnyUi = isPointerOverAnyUi;
            IsPointerOverGameplayUi = isPointerOverGameplayUi;
            TryIssueAttackOrderToClickedUnit = tryIssueAttackOrderToClickedUnit;
            TryIssueScanOrder = tryIssueScanOrder;
            OrderMarkerSystem = orderMarkerSystem;
            TryGetDefaultEntityManager = tryGetDefaultEntityManager;
            TryGetScanClickedCell = tryGetScanClickedCell;
            SetHudWorldMarkersVisible = setHudWorldMarkersVisible;
            TryIssueBoardTransportOrderToClickedUnit = tryIssueBoardTransportOrderToClickedUnit;
            TryIssueBoardSelectedTransportOrderToClickedUnit = tryIssueBoardSelectedTransportOrderToClickedUnit;
            TryIssueBoardSelectedTransportOrderToPassengerRect = tryIssueBoardSelectedTransportOrderToPassengerRect;
            TryFocusUnit = tryFocusUnit;
            PanCamera = panCamera;
            IssueMoveOrder = issueMoveOrder;
            ProcessSelectionRectangleRequests = processSelectionRectangleRequests;
            ClearCommandMode = clearCommandMode;
            LogClickDiagnostic = logClickDiagnostic;
            BuildClickDebugSummary = buildClickDebugSummary;
            IsGameplayInputLocked = isGameplayInputLocked;
        }
    }

    public void ProcessQueuedMoveOrder(Context context)
    {
        if (IsGameplayInputLocked(context))
        {
            context.InputSystem.ClearQueuedMoveOrder();
            int removedMoveCommands = context.InputSystem.ClearPendingMoveCommandRequests();
            if (removedMoveCommands > 0)
                context.LogClickDiagnostic?.Invoke($"queuedMoveCanceled reason=IntroInputLocked removed={removedMoveCommands} frame={Time.frameCount}");
            return;
        }

        if (!context.InputSystem.TryConsumeQueuedMoveOrder(Time.frameCount, out Vector2 screenPosition))
            return;

        if (!context.InputSystem.HasActiveWorldTargetCommandMode(out TacticalCommandMode activeMode) ||
            activeMode != TacticalCommandMode.Move)
        {
            context.LogClickDiagnostic?.Invoke($"queuedMoveCanceled reason=MoveModeInactive activeMode={activeMode} pos={screenPosition} frame={Time.frameCount}");
            return;
        }

        if (!context.RuntimeGameplayStateSystem.PlayRequested || context.RuntimeGameplayStateSystem.BuildModeActive)
        {
            context.LogClickDiagnostic?.Invoke(
                $"queuedMoveCanceled reason={(context.RuntimeGameplayStateSystem.PlayRequested ? "BuildMode" : "NotPlaying")} pos={screenPosition} frame={Time.frameCount}");
            return;
        }

        if (Time.frameCount <= context.InputSystem.IgnoreWorldCommandsUntilFrame)
        {
            context.LogClickDiagnostic?.Invoke(
                $"queuedMoveCanceled reason=IgnoreWorldCommandsUntilFrame current={Time.frameCount} until={context.InputSystem.IgnoreWorldCommandsUntilFrame} pos={screenPosition}");
            return;
        }

        if (context.RuntimeGameplayStateSystem.SuppressNextWorldClick)
        {
            context.LogClickDiagnostic?.Invoke($"queuedMoveCanceled reason=SuppressNextWorldClick pos={screenPosition} frame={Time.frameCount}");
            return;
        }

        context.LogClickDiagnostic?.Invoke($"queuedMoveIssue pos={screenPosition} frame={Time.frameCount}");
        context.IssueMoveOrder?.Invoke(screenPosition);
    }

    public void UpdateNormalPointerInput(Context context)
    {
        RtsSelectionInputSystem input = context.InputSystem;
        RuntimeGameplayStateSystem runtime = context.RuntimeGameplayStateSystem;
        if (!GamePointerInput.TryGetPrimaryPointer(out GamePointerState pointer))
            return;

        if (IsGameplayInputLocked(context))
        {
            if (pointer.WasPressedThisFrame || pointer.WasReleasedThisFrame)
                context.LogClickDiagnostic?.Invoke($"worldInputBlocked reason=IntroInputLocked pressed={pointer.WasPressedThisFrame} released={pointer.WasReleasedThisFrame} pos={pointer.Position} frame={Time.frameCount}");

            input.ClearQueuedMoveOrder();
            input.ClearPendingMoveCommandRequests();
            input.ClearPointerReleaseState();
            input.SelectionModeHoldArmed = false;
            runtime.SuppressNextWorldClick = true;
            context.SetCameraDragging?.Invoke(false);
            return;
        }

        if (input.IgnoreUiClickUntilRelease)
        {
            if (pointer.WasReleasedThisFrame || !pointer.IsPressed)
            {
                context.LogClickDiagnostic?.Invoke($"ignoreUiClickUntilRelease cleared pos={pointer.Position} frame={Time.frameCount}");
                input.IgnoreUiClickUntilRelease = false;
                input.IgnoreNextLeftMouseRelease = false;
                input.SkipNextWorldReleaseAfterSelection = false;
            }

            return;
        }

        if (Time.frameCount <= input.IgnoreWorldCommandsUntilFrame)
        {
            if (pointer.WasPressedThisFrame || pointer.WasReleasedThisFrame)
                context.LogClickDiagnostic?.Invoke($"worldClickIgnoredUntilFrame current={Time.frameCount} until={input.IgnoreWorldCommandsUntilFrame} pressed={pointer.WasPressedThisFrame} released={pointer.WasReleasedThisFrame} pos={pointer.Position}");
            return;
        }

        Vector2 pointerPosition = pointer.Position;
        input.UpdateLastKnownPointerPosition(pointerPosition);
        UpdateSelectionModeHold(context, pointer.IsPressed, pointerPosition);

        if (pointer.WasReleasedThisFrame && input.IgnoreNextLeftMouseRelease)
        {
            context.LogClickDiagnostic?.Invoke($"releaseIgnored reason=IgnoreNextLeftMouseRelease pos={pointerPosition} frame={Time.frameCount}");
            input.IgnoreNextLeftMouseRelease = false;
            input.SkipNextWorldReleaseAfterSelection = false;
            runtime.SuppressNextWorldClick = false;
            if (runtime.SelectionModeActive && (input.IsDraggingSelection || input.HasLiveSelectionRect))
                CompleteSelectionMode(context);
            input.IsDraggingSelection = false;
            context.SetCameraDragging?.Invoke(false);
            input.SelectionModeHoldArmed = false;
            input.LastPointerPosition = pointerPosition;
            return;
        }

        if (pointer.WasPressedThisFrame)
            HandlePointerPressed(context, pointerPosition);

        if (pointer.IsPressed)
            HandlePointerHeld(context, pointerPosition);

        if (pointer.WasReleasedThisFrame)
            HandlePointerReleased(context, pointerPosition);
    }

    private static void HandlePointerPressed(Context context, Vector2 pointerPosition)
    {
        RtsSelectionInputSystem input = context.InputSystem;
        RuntimeGameplayStateSystem runtime = context.RuntimeGameplayStateSystem;
        MainMenuPlayUI mainMenu = context.MainMenuPlayUi;
        if (mainMenu != null && mainMenu.IsPointerOverSelectionCancelUi(pointerPosition))
        {
            context.LogClickDiagnostic?.Invoke($"pressSelectionCancelUi pos={pointerPosition} frame={Time.frameCount}");
            mainMenu.TriggerSelectionCancel();
            input.PointerPressedOverUi = true;
            input.IsDraggingSelection = false;
            context.SetCameraDragging?.Invoke(false);
            input.LastPointerPosition = pointerPosition;
            return;
        }

        bool pointerOverAnyUi = context.IsPointerOverAnyUi?.Invoke(pointerPosition) == true;
        bool pointerOverGameplayUi = context.IsPointerOverGameplayUi?.Invoke(pointerPosition) == true;
        bool pointerOverBlockingUi = runtime.PlayRequested ? pointerOverGameplayUi : (pointerOverAnyUi || pointerOverGameplayUi);
        context.LogClickDiagnostic?.Invoke(
            $"press pos={pointerPosition} frame={Time.frameCount} play={runtime.PlayRequested} selectionMode={runtime.SelectionModeActive} anyUi={pointerOverAnyUi} gameplayUi={pointerOverGameplayUi} blockingUi={pointerOverBlockingUi}");
        input.BeginPointerPress(pointerPosition, pointerOverBlockingUi);
        context.SetCameraDragging?.Invoke(false);

        if (runtime.SelectionModeActive)
            return;

        if (input.HasActiveWorldTargetCommandMode(out _))
        {
            context.SetCameraDragging?.Invoke(false);
            input.SelectionModeHoldArmed = false;
            return;
        }

        if (!input.PointerPressedOverUi)
        {
            context.SetCameraDragging?.Invoke(true);
            input.ArmSelectionModeHold(Time.unscaledTime);
        }
        else
        {
            context.SetCameraDragging?.Invoke(false);
        }
    }

    private static void HandlePointerHeld(Context context, Vector2 pointerPosition)
    {
        RtsSelectionInputSystem input = context.InputSystem;
        RuntimeGameplayStateSystem runtime = context.RuntimeGameplayStateSystem;
        Vector2 frameDelta = pointerPosition - input.LastPointerPosition;
        input.DragCurrent = pointerPosition;
        float dragDistance = Vector2.Distance(input.DragStart, input.DragCurrent);

        if (runtime.SelectionModeActive)
        {
            if (!input.IsDraggingSelection && dragDistance >= context.DragThresholdPixels)
                input.IsDraggingSelection = true;

            if (input.IsDraggingSelection)
            {
                Rect liveRect = GetScreenRect(input.DragStart, input.DragCurrent);
                if (!input.HasLiveSelectionRect || !ApproximatelyEqualRect(input.LastLiveSelectionRect, liveRect))
                {
                    input.QueueSelectionRectangleRequest(
                        RtsSelectionPointerRequestKind.SelectionRectUpdated,
                        liveRect,
                        Time.frameCount,
                        VisibleUnitSelectionSystem.Filter.All);
                    context.ProcessSelectionRectangleRequests?.Invoke();
                    input.LastLiveSelectionRect = liveRect;
                    input.HasLiveSelectionRect = true;
                }
            }
        }
        else if (IsTransportFirstBoardMode(input) && !input.PointerPressedOverUi)
        {
            if (!input.IsDraggingSelection && dragDistance >= context.DragThresholdPixels)
                input.IsDraggingSelection = true;

            if (input.IsDraggingSelection)
            {
                Rect liveRect = GetScreenRect(input.DragStart, input.DragCurrent);
                input.LastLiveSelectionRect = liveRect;
                input.HasLiveSelectionRect = true;
            }
        }
        else if (context.GetCameraDragging?.Invoke() == true && frameDelta.sqrMagnitude > 0f)
        {
            context.PanCamera?.Invoke(frameDelta);
        }

        if (!input.PointerPressedOverUi &&
            input.HasActiveWorldTargetCommandMode(out TacticalCommandMode activeMode) &&
            activeMode == TacticalCommandMode.Scan)
        {
            UpdateScanTargetPreview(context, pointerPosition);
        }

        if (dragDistance >= context.DragThresholdPixels)
            input.SelectionModeHoldArmed = false;

        input.LastPointerPosition = pointerPosition;
    }

    private static void HandlePointerReleased(Context context, Vector2 pointerPosition)
    {
        RtsSelectionInputSystem input = context.InputSystem;
        RuntimeGameplayStateSystem runtime = context.RuntimeGameplayStateSystem;
        input.DragCurrent = pointerPosition;
        bool releasePointerOverAnyUi = context.IsPointerOverAnyUi?.Invoke(pointerPosition) == true;
        bool releasePointerOverGameplayUi = context.IsPointerOverGameplayUi?.Invoke(pointerPosition) == true;
        bool releasePointerOverBlockingUi = runtime.PlayRequested ? releasePointerOverGameplayUi : (releasePointerOverAnyUi || releasePointerOverGameplayUi);
        float dragDistance = Vector2.Distance(input.DragStart, pointerPosition);
        context.LogClickDiagnostic?.Invoke(
            $"release pos={pointerPosition} frame={Time.frameCount} play={runtime.PlayRequested} selectionMode={runtime.SelectionModeActive} drag={dragDistance:F1}/{context.DragThresholdPixels:F1} pressedOverUi={input.PointerPressedOverUi} anyUi={releasePointerOverAnyUi} gameplayUi={releasePointerOverGameplayUi} blockingUi={releasePointerOverBlockingUi} suppress={runtime.SuppressNextWorldClick} skip={input.SkipNextWorldReleaseAfterSelection} dragging={input.IsDraggingSelection} liveRect={input.HasLiveSelectionRect}");

        if (input.PointerPressedOverUi || releasePointerOverBlockingUi)
        {
            context.LogClickDiagnostic?.Invoke($"releaseBlocked reason={(input.PointerPressedOverUi ? "PressedOverUi" : "ReleasedOverBlockingUi")} pos={pointerPosition}");
            LogOneClickDebug(context, pointerPosition, "ReleaseBlocked");
            input.PointerPressedOverUi = false;
            input.IsDraggingSelection = false;
            context.SetCameraDragging?.Invoke(false);
            input.SelectionModeHoldArmed = false;
            input.HasLiveSelectionRect = false;
            return;
        }

        if (input.SkipNextWorldReleaseAfterSelection)
        {
            context.LogClickDiagnostic?.Invoke($"releaseSkipped reason=SkipNextWorldReleaseAfterSelection pos={pointerPosition}");
            LogOneClickDebug(context, pointerPosition, "ReleaseSkipped");
            input.SkipNextWorldReleaseAfterSelection = false;
            runtime.SuppressNextWorldClick = false;
            input.IsDraggingSelection = false;
            context.SetCameraDragging?.Invoke(false);
            input.SelectionModeHoldArmed = false;
            input.HasLiveSelectionRect = false;
            return;
        }

        if (runtime.SelectionModeActive)
        {
            if (input.IsDraggingSelection)
            {
                if (!input.HasLiveSelectionRect)
                {
                    context.LogClickDiagnostic?.Invoke($"selectionRectCommitted pos={pointerPosition} start={input.DragStart} current={input.DragCurrent}");
                    input.QueueSelectionRectangleRequest(
                        RtsSelectionPointerRequestKind.SelectionRectCommitted,
                        GetScreenRect(input.DragStart, input.DragCurrent),
                        Time.frameCount,
                        VisibleUnitSelectionSystem.Filter.All);
                    context.ProcessSelectionRectangleRequests?.Invoke();
                }
            }
            else if (!releasePointerOverBlockingUi)
            {
                bool focused = context.TryFocusUnit?.Invoke(pointerPosition) == true;
                context.LogClickDiagnostic?.Invoke($"selectionModeClickFocus result={focused} pos={pointerPosition}");
                if (focused)
                {
                    input.ClearQueuedMoveOrder();
                    int removedMoveCommands = input.ClearPendingMoveCommandRequests();
                    if (removedMoveCommands > 0)
                        context.LogClickDiagnostic?.Invoke($"selectionClearedPendingMoveCommands count={removedMoveCommands} pos={pointerPosition}");
                }
            }

            CompleteSelectionMode(context);
        }
        else if (dragDistance < context.DragThresholdPixels)
        {
            if (runtime.SuppressNextWorldClick)
            {
                bool sameGuardWindow = Time.frameCount <= input.IgnoreWorldCommandsUntilFrame;
                bool focusedWhileSuppressed = sameGuardWindow && context.TryFocusUnit?.Invoke(pointerPosition) == true;
                context.LogClickDiagnostic?.Invoke($"clickSuppressed reason=SuppressNextWorldClick sameGuardWindow={sameGuardWindow} focusOverride={focusedWhileSuppressed} pos={pointerPosition}");
                runtime.SuppressNextWorldClick = false;
                if (sameGuardWindow)
                {
                    input.ClearQueuedMoveOrder();
                    int removedMoveCommands = input.ClearPendingMoveCommandRequests();
                    if (removedMoveCommands > 0)
                        context.LogClickDiagnostic?.Invoke($"selectionClearedPendingMoveCommands count={removedMoveCommands} pos={pointerPosition}");
                    LogOneClickDebug(context, pointerPosition, focusedWhileSuppressed ? "SuppressedFocus" : "SuppressedGuard");
                    input.IsDraggingSelection = false;
                    context.SetCameraDragging?.Invoke(false);
                    input.PointerPressedOverUi = false;
                    input.SelectionModeHoldArmed = false;
                    input.HasLiveSelectionRect = false;
                    return;
                }

                context.LogClickDiagnostic?.Invoke($"staleSuppressCleared action=ContinueWorldClick pos={pointerPosition} frame={Time.frameCount}");
            }

            if (!releasePointerOverBlockingUi)
            {
                if (input.HasActiveWorldTargetCommandMode(out TacticalCommandMode activeMode))
                {
                    bool handledCommandTarget = HandleWorldTargetCommand(context, input, activeMode, pointerPosition);
                    context.LogClickDiagnostic?.Invoke($"clickWorldTargetCommand mode={activeMode} result={handledCommandTarget} pos={pointerPosition}");
                    LogOneClickDebug(context, pointerPosition, handledCommandTarget ? $"{activeMode}Target" : $"{activeMode}TargetUnhandled");
                }
                else if (input.IsMoveTargetDoubleClick(pointerPosition, Time.unscaledTime))
                {
                    bool handledDoubleClick = HandlePersistentMoveTargetDoubleClick(context, input, pointerPosition);
                    context.LogClickDiagnostic?.Invoke($"clickMoveDoubleClickRetain result={handledDoubleClick} pos={pointerPosition}");
                    LogOneClickDebug(context, pointerPosition, handledDoubleClick ? "MoveDoubleClickRetain" : "MoveDoubleClickRetainFailed");
                }
                else if (context.GetExplicitAttackTargetModeActive?.Invoke() == true)
                {
                    bool attackIssued = context.TryIssueAttackOrderToClickedUnit?.Invoke(pointerPosition) == true;
                    context.LogClickDiagnostic?.Invoke($"clickExplicitAttack result={attackIssued} pos={pointerPosition}");
                    LogOneClickDebug(context, pointerPosition, attackIssued ? "ExplicitAttack" : "ExplicitAttackMiss");
                    if (attackIssued)
                        context.SetExplicitAttackTargetModeActive?.Invoke(false);
                }
                else if (context.TryFocusUnit?.Invoke(pointerPosition) == true)
                {
                    context.LogClickDiagnostic?.Invoke($"clickFocus result=True pos={pointerPosition}");
                    input.ClearQueuedMoveOrder();
                    int removedMoveCommands = input.ClearPendingMoveCommandRequests();
                    if (removedMoveCommands > 0)
                        context.LogClickDiagnostic?.Invoke($"selectionClearedPendingMoveCommands count={removedMoveCommands} pos={pointerPosition}");
                    runtime.SuppressNextWorldClick = false;
                    LogOneClickDebug(context, pointerPosition, "FocusUnit");
                }
                else
                {
                    context.LogClickDiagnostic?.Invoke($"clickFocus result=False action=NoCommand pos={pointerPosition}");
                    LogOneClickDebug(context, pointerPosition, "NoCommand");
                }
            }
        }
        else
        {
            bool handledBoardRect = HandleBoardPassengerRectCommand(context, input, pointerPosition);
            context.LogClickDiagnostic?.Invoke($"releaseDragCommand boardRect={handledBoardRect} pos={pointerPosition} drag={dragDistance:F1}");
            LogOneClickDebug(context, pointerPosition, handledBoardRect ? "BoardPassengerRect" : "DragIgnored");
        }

        input.IsDraggingSelection = false;
        context.SetCameraDragging?.Invoke(false);
        input.PointerPressedOverUi = false;
        input.SelectionModeHoldArmed = false;
        input.HasLiveSelectionRect = false;
    }

    private static bool HandleWorldTargetCommand(
        Context context,
        RtsSelectionInputSystem input,
        TacticalCommandMode activeMode,
        Vector2 pointerPosition)
    {
        if (activeMode == TacticalCommandMode.Move)
        {
            if (context.IssueMoveOrder == null)
            {
                input.ClearActiveCommandMode();
                context.ClearCommandMode?.Invoke();
                return false;
            }

            bool persistentMove = input.IsMoveTargetDoubleClick(pointerPosition, Time.unscaledTime);
            if (persistentMove)
            {
                input.ArmCommandMode(
                    TacticalCommandMode.Move,
                    Time.frameCount,
                    oneShot: false,
                    requiresWorldTarget: true);
            }

            input.RecordMoveTargetClick(pointerPosition, Time.unscaledTime);
            context.IssueMoveOrder.Invoke(pointerPosition);
            return true;
        }

        if (activeMode == TacticalCommandMode.Attack)
        {
            if (context.TryIssueAttackOrderToClickedUnit == null)
            {
                input.ClearActiveCommandMode();
                context.SetExplicitAttackTargetModeActive?.Invoke(false);
                context.ClearCommandMode?.Invoke();
                return false;
            }

            bool attackIssued = context.TryIssueAttackOrderToClickedUnit.Invoke(pointerPosition);
            if (attackIssued)
                context.SetExplicitAttackTargetModeActive?.Invoke(false);
            return true;
        }

        if (activeMode == TacticalCommandMode.Scan)
        {
            if (context.TryIssueScanOrder == null)
            {
                input.ClearActiveCommandMode();
                context.ClearCommandMode?.Invoke();
                return false;
            }

            context.TryIssueScanOrder.Invoke(pointerPosition);
            return true;
        }

        if (activeMode == TacticalCommandMode.Board)
        {
            if (!input.TryGetActiveBoardCommandMode(out BoardCommandModeDirection direction, out Entity transport))
            {
                input.ClearActiveCommandMode();
                context.ClearCommandMode?.Invoke();
                return false;
            }

            if (direction == BoardCommandModeDirection.PassengerToTransport)
            {
                if (context.TryIssueBoardTransportOrderToClickedUnit == null)
                {
                    input.ClearActiveCommandMode();
                    context.ClearCommandMode?.Invoke();
                    return false;
                }

                context.TryIssueBoardTransportOrderToClickedUnit.Invoke(pointerPosition);
                return true;
            }

            if (direction == BoardCommandModeDirection.TransportToPassenger)
            {
                if (context.TryIssueBoardSelectedTransportOrderToClickedUnit == null)
                {
                    input.ClearActiveCommandMode();
                    context.ClearCommandMode?.Invoke();
                    return false;
                }

                context.TryIssueBoardSelectedTransportOrderToClickedUnit.Invoke(transport, pointerPosition);
                return true;
            }
        }

        input.ClearActiveCommandMode();
        context.ClearCommandMode?.Invoke();
        return false;
    }

    private static bool HandleBoardPassengerRectCommand(
        Context context,
        RtsSelectionInputSystem input,
        Vector2 pointerPosition)
    {
        if (!input.TryGetActiveBoardCommandMode(out BoardCommandModeDirection direction, out Entity transport) ||
            direction != BoardCommandModeDirection.TransportToPassenger ||
            context.TryIssueBoardSelectedTransportOrderToPassengerRect == null)
        {
            return false;
        }

        Rect screenRect = GetScreenRect(input.DragStart, pointerPosition);
        return context.TryIssueBoardSelectedTransportOrderToPassengerRect.Invoke(transport, screenRect);
    }

    private static bool IsTransportFirstBoardMode(RtsSelectionInputSystem input)
    {
        return input.TryGetActiveBoardCommandMode(out BoardCommandModeDirection direction, out _) &&
               direction == BoardCommandModeDirection.TransportToPassenger;
    }

    private static void UpdateScanTargetPreview(Context context, Vector2 pointerPosition)
    {
        if (context.OrderMarkerSystem == null ||
            context.TryGetDefaultEntityManager == null ||
            context.TryGetScanClickedCell == null ||
            !context.TryGetDefaultEntityManager(out EntityManager em))
        {
            return;
        }

        if (!context.TryGetScanClickedCell(pointerPosition, em, out int2 cell, out Vector3 worldPoint))
            return;

        context.OrderMarkerSystem.ShowScanOrderMarker(
            em,
            cell,
            worldPoint,
            ScanIntelCommandSystem.DefaultScanRadiusCells,
            visibleSeconds: 0.2f);
        context.SetHudWorldMarkersVisible?.Invoke(true);
    }

    private static bool HandlePersistentMoveTargetDoubleClick(
        Context context,
        RtsSelectionInputSystem input,
        Vector2 pointerPosition)
    {
        if (context.IssueMoveOrder == null)
            return false;

        input.ArmCommandMode(
            TacticalCommandMode.Move,
            Time.frameCount,
            oneShot: false,
            requiresWorldTarget: true);
        input.RecordMoveTargetClick(pointerPosition, Time.unscaledTime);
        context.IssueMoveOrder.Invoke(pointerPosition);
        return true;
    }

    private static void CompleteSelectionMode(Context context)
    {
        RuntimeGameplayStateSystem runtime = context.RuntimeGameplayStateSystem;
        if (!runtime.SelectionModeActive)
            return;

        runtime.SelectionModeActive = false;
        runtime.SuppressNextWorldClick = false;
        context.InputSystem.ClearActiveCommandMode();
        context.ClearCommandMode?.Invoke();
    }

    private static void LogOneClickDebug(Context context, Vector2 pointerPosition, string action)
    {
        string summary = context.BuildClickDebugSummary?.Invoke(pointerPosition) ?? "summary=unavailable";
        context.LogClickDiagnostic?.Invoke($"ONE_CLICK_DEBUG action={action} pos={pointerPosition} frame={Time.frameCount} {summary}");
    }

    private static void UpdateSelectionModeHold(Context context, bool pointerPressed, Vector2 pointerPosition)
    {
        RtsSelectionInputSystem input = context.InputSystem;
        RuntimeGameplayStateSystem runtime = context.RuntimeGameplayStateSystem;
        MainMenuPlayUI mainMenu = context.MainMenuPlayUi;
        if (!input.SelectionModeHoldArmed)
            return;

        if (!pointerPressed)
        {
            input.SelectionModeHoldArmed = false;
            return;
        }

        if (runtime.SelectionModeActive)
        {
            input.SelectionModeHoldArmed = false;
            return;
        }

        if (mainMenu == null || !mainMenu.CanTriggerSelectionModeFromHold())
        {
            input.SelectionModeHoldArmed = false;
            return;
        }

        if (mainMenu.IsPointerOverZoomControls(pointerPosition))
        {
            input.SelectionModeHoldArmed = false;
            return;
        }

        if (Vector2.Distance(input.DragStart, pointerPosition) >= context.DragThresholdPixels)
        {
            input.SelectionModeHoldArmed = false;
            return;
        }

        if (Time.unscaledTime - input.SelectionModeHoldStartTime < context.SelectionModeHoldSeconds)
            return;

        input.SelectionModeHoldArmed = false;
        input.PointerPressedOverUi = false;
        input.IsDraggingSelection = false;
        context.SetCameraDragging?.Invoke(false);
        input.IgnoreNextLeftMouseRelease = true;
        mainMenu.TriggerSelectionModeFromHold();
    }

    private static bool ApproximatelyEqualRect(Rect a, Rect b)
    {
        return Mathf.Abs(a.x - b.x) < 0.5f &&
               Mathf.Abs(a.y - b.y) < 0.5f &&
               Mathf.Abs(a.width - b.width) < 0.5f &&
               Mathf.Abs(a.height - b.height) < 0.5f;
    }

    private static bool IsGameplayInputLocked(Context context)
    {
        return context.IsGameplayInputLocked?.Invoke() == true;
    }

    private static Rect GetScreenRect(Vector2 a, Vector2 b)
    {
        Vector2 min = Vector2.Min(a, b);
        Vector2 max = Vector2.Max(a, b);
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }
}
