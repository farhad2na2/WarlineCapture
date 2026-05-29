using System;
using UnityEngine;

public sealed class RtsSelectionRuntimeInputSystem
{
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
        public readonly Func<Vector2, bool> TryIssueBoardTransportOrderToClickedUnit;
        public readonly Func<Vector2, bool> TryFocusUnit;
        public readonly Action<Vector2> PanCamera;
        public readonly Action<Vector2> IssueMoveOrder;
        public readonly Action ProcessSelectionRectangleRequests;

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
            Func<Vector2, bool> tryIssueBoardTransportOrderToClickedUnit,
            Func<Vector2, bool> tryFocusUnit,
            Action<Vector2> panCamera,
            Action<Vector2> issueMoveOrder,
            Action processSelectionRectangleRequests)
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
            TryIssueBoardTransportOrderToClickedUnit = tryIssueBoardTransportOrderToClickedUnit;
            TryFocusUnit = tryFocusUnit;
            PanCamera = panCamera;
            IssueMoveOrder = issueMoveOrder;
            ProcessSelectionRectangleRequests = processSelectionRectangleRequests;
        }
    }

    public void ProcessQueuedMoveOrder(Context context)
    {
        if (!context.InputSystem.TryConsumeQueuedMoveOrder(Time.frameCount, out Vector2 screenPosition))
            return;

        if (!context.RuntimeGameplayStateSystem.PlayRequested || context.RuntimeGameplayStateSystem.BuildModeActive)
            return;

        if (context.RuntimeGameplayStateSystem.SuppressNextWorldClick)
            return;

        context.IssueMoveOrder?.Invoke(screenPosition);
    }

    public void UpdateNormalPointerInput(Context context)
    {
        RtsSelectionInputSystem input = context.InputSystem;
        RuntimeGameplayStateSystem runtime = context.RuntimeGameplayStateSystem;
        if (!GamePointerInput.TryGetPrimaryPointer(out GamePointerState pointer))
            return;

        if (input.IgnoreUiClickUntilRelease)
        {
            if (pointer.WasReleasedThisFrame || !pointer.IsPressed)
            {
                input.IgnoreUiClickUntilRelease = false;
                input.IgnoreNextLeftMouseRelease = false;
                input.SkipNextWorldReleaseAfterSelection = false;
            }

            return;
        }

        if (Time.frameCount <= input.IgnoreWorldCommandsUntilFrame)
            return;

        Vector2 pointerPosition = pointer.Position;
        input.UpdateLastKnownPointerPosition(pointerPosition);
        UpdateSelectionModeHold(context, pointer.IsPressed, pointerPosition);

        if (pointer.WasReleasedThisFrame && input.IgnoreNextLeftMouseRelease)
        {
            input.IgnoreNextLeftMouseRelease = false;
            input.SkipNextWorldReleaseAfterSelection = false;
            runtime.SuppressNextWorldClick = false;
            if (runtime.SelectionModeActive && (input.IsDraggingSelection || input.HasLiveSelectionRect))
                runtime.SelectionModeActive = false;
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
        input.BeginPointerPress(pointerPosition, !runtime.PlayRequested && pointerOverBlockingUi);
        context.SetCameraDragging?.Invoke(false);

        if (runtime.SelectionModeActive)
            return;

        if (!input.PointerPressedOverUi)
        {
            context.SetCameraDragging?.Invoke(true);
            input.ArmSelectionModeHold(Time.unscaledTime);
        }
        else
        {
            context.SetCameraDragging?.Invoke(true);
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
        else if (context.GetCameraDragging?.Invoke() == true && frameDelta.sqrMagnitude > 0f)
        {
            context.PanCamera?.Invoke(frameDelta);
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

        if (input.PointerPressedOverUi || releasePointerOverBlockingUi)
        {
            input.PointerPressedOverUi = false;
            input.IsDraggingSelection = false;
            context.SetCameraDragging?.Invoke(false);
            input.SelectionModeHoldArmed = false;
            input.HasLiveSelectionRect = false;
            return;
        }

        if (input.SkipNextWorldReleaseAfterSelection)
        {
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
                context.TryFocusUnit?.Invoke(pointerPosition);
            }

            runtime.SelectionModeActive = false;
            runtime.SuppressNextWorldClick = false;
        }
        else if (Vector2.Distance(input.DragStart, pointerPosition) < context.DragThresholdPixels)
        {
            if (runtime.SuppressNextWorldClick)
            {
                runtime.SuppressNextWorldClick = false;
            }
            else if (!releasePointerOverBlockingUi)
            {
                if (context.GetExplicitAttackTargetModeActive?.Invoke() == true)
                {
                    if (context.TryIssueAttackOrderToClickedUnit?.Invoke(pointerPosition) == true)
                        context.SetExplicitAttackTargetModeActive?.Invoke(false);
                }
                else if (context.TryIssueAttackOrderToClickedUnit?.Invoke(pointerPosition) == true)
                {
                    runtime.SuppressNextWorldClick = false;
                }
                else if (context.TryIssueBoardTransportOrderToClickedUnit?.Invoke(pointerPosition) == true)
                {
                    runtime.SuppressNextWorldClick = false;
                }
                else if (context.TryFocusUnit?.Invoke(pointerPosition) == true)
                {
                    runtime.SuppressNextWorldClick = false;
                }
                else
                {
                    input.QueueMoveOrder(pointerPosition, Time.frameCount + 1);
                }
            }
        }

        input.IsDraggingSelection = false;
        context.SetCameraDragging?.Invoke(false);
        input.PointerPressedOverUi = false;
        input.SelectionModeHoldArmed = false;
        input.HasLiveSelectionRect = false;
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

    private static Rect GetScreenRect(Vector2 a, Vector2 b)
    {
        Vector2 min = Vector2.Min(a, b);
        Vector2 max = Vector2.Max(a, b);
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }
}
