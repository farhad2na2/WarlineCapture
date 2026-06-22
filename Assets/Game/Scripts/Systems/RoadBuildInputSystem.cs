using System;
using System.Collections.Generic;
using UnityEngine;
using BuildToolMode = RoadBuildSessionSystem.BuildToolMode;
using DragFirstAxis = RoadPathPlanningSystem.DragFirstAxis;

public sealed class RoadBuildInputSystem
{
    public delegate bool TryGetHoveredCellAction(Vector2 screenPosition, out Vector2Int cell);

    public sealed class State
    {
        public Vector2Int? PendingStartCell;
        public Vector2Int CurrentDragCell;
        public bool IsDrawing;
        public bool PressedOnExistingRoad;
        public Vector2Int PressedRoadCell;
        public int PressedRoadStrokeId;
        public DragFirstAxis DragFirstAxis;
    }

    public struct Context
    {
        public readonly State State;
        public RuntimeGameplayStateSystem RuntimeGameplayStateSystem;
        public readonly RoadBuildSessionSystem SessionSystem;
        public readonly RoadBuildSessionSystem.State SessionState;
        public readonly RoadPathPlanningSystem PathPlanningSystem;
        public readonly RoadNetworkSystem NetworkSystem;
        public readonly TryGetHoveredCellAction TryGetHoveredCell;
        public readonly Action ClearPreview;
        public readonly Action UpdatePreview;
        public readonly Action HidePlacementOutline;
        public readonly Action<Vector2> UpdateBuildingPlacement;
        public readonly Action<List<Vector2Int>> CreateStroke;
        public readonly Func<List<Vector2Int>, bool> IsRoadPathSurfaceValid;
        public readonly Func<bool> HasActiveBuildingPlacement;
        public readonly Action<bool> SetIsDraggingBuildingPlacement;

        public Context(
            State state,
            RuntimeGameplayStateSystem runtimeGameplayStateSystem,
            RoadBuildSessionSystem sessionSystem,
            RoadBuildSessionSystem.State sessionState,
            RoadPathPlanningSystem pathPlanningSystem,
            RoadNetworkSystem networkSystem,
            TryGetHoveredCellAction tryGetHoveredCell,
            Action clearPreview,
            Action updatePreview,
            Action hidePlacementOutline,
            Action<Vector2> updateBuildingPlacement,
            Action<List<Vector2Int>> createStroke,
            Func<List<Vector2Int>, bool> isRoadPathSurfaceValid,
            Func<bool> hasActiveBuildingPlacement,
            Action<bool> setIsDraggingBuildingPlacement)
        {
            State = state;
            RuntimeGameplayStateSystem = runtimeGameplayStateSystem;
            SessionSystem = sessionSystem;
            SessionState = sessionState;
            PathPlanningSystem = pathPlanningSystem;
            NetworkSystem = networkSystem;
            TryGetHoveredCell = tryGetHoveredCell;
            ClearPreview = clearPreview;
            UpdatePreview = updatePreview;
            HidePlacementOutline = hidePlacementOutline;
            UpdateBuildingPlacement = updateBuildingPlacement;
            CreateStroke = createStroke;
            IsRoadPathSurfaceValid = isRoadPathSurfaceValid;
            HasActiveBuildingPlacement = hasActiveBuildingPlacement;
            SetIsDraggingBuildingPlacement = setIsDraggingBuildingPlacement;
        }
    }

    public bool IsDrawing(State state) => state != null && state.IsDrawing;

    public void Update(Context context, Camera worldCamera)
    {
        bool roadModeActive = context.RuntimeGameplayStateSystem.PlayRequested &&
            context.RuntimeGameplayStateSystem.BuildModeActive &&
            context.SessionSystem.IsActiveTool(context.SessionState, BuildToolMode.Road);
        if (!roadModeActive)
            context.ClearPreview?.Invoke();

        if (worldCamera == null)
            return;

        if (!GamePointerInput.TryGetPrimaryPointer(out GamePointerState pointer))
            return;

        if (context.SessionSystem.TryConsumeSkipBuildClickFrame(context.SessionState))
            return;

        if (context.HasActiveBuildingPlacement != null && context.HasActiveBuildingPlacement())
        {
            Vector2 pointerPosition = pointer.Position;
            bool pointerOverUi = IsPointerOverUI(pointerPosition);
            if (pointer.WasPressedThisFrame && !pointerOverUi)
                context.SetIsDraggingBuildingPlacement?.Invoke(true);
            if (pointer.WasReleasedThisFrame)
                context.SetIsDraggingBuildingPlacement?.Invoke(false);

            context.UpdateBuildingPlacement?.Invoke(pointerPosition);
            return;
        }

        if (!context.RuntimeGameplayStateSystem.PlayRequested || !context.RuntimeGameplayStateSystem.BuildModeActive)
        {
            context.HidePlacementOutline?.Invoke();
            return;
        }

        if (context.SessionSystem.IsActiveTool(context.SessionState, BuildToolMode.Road))
            context.UpdatePreview?.Invoke();

        if (context.SessionSystem.HasDeletePrompt(context.SessionState))
            return;

        if (!context.SessionSystem.IsActiveTool(context.SessionState, BuildToolMode.Road))
            return;

        Vector2Int cell = default;
        bool hasHoveredCell = context.TryGetHoveredCell != null &&
            context.TryGetHoveredCell(pointer.Position, out cell);

        if (pointer.WasPressedThisFrame)
            HandlePointerPressed(context, hasHoveredCell, cell);

        if (context.State.IsDrawing && pointer.IsPressed && hasHoveredCell)
        {
            context.State.CurrentDragCell = cell;
            UpdateDragAxis(context, cell);
            context.UpdatePreview?.Invoke();
        }

        if (pointer.WasReleasedThisFrame)
            HandlePointerReleased(context, hasHoveredCell, cell);
    }

    public void CancelPendingBuild(Context context)
    {
        context.State.PendingStartCell = null;
        context.State.IsDrawing = false;
        context.State.DragFirstAxis = DragFirstAxis.None;
        context.ClearPreview?.Invoke();
    }

    private void HandlePointerPressed(Context context, bool hasHoveredCell, Vector2Int cell)
    {
        if (context.SessionSystem.ShouldSkipBuildClick(context.SessionState) || !hasHoveredCell)
            return;

        if (context.NetworkSystem.StrokeIdsByCell.TryGetValue(cell, out List<int> strokeIds) && strokeIds.Count > 0)
        {
            context.State.PressedOnExistingRoad = true;
            context.State.PressedRoadCell = cell;
            context.State.PressedRoadStrokeId = strokeIds[strokeIds.Count - 1];
            context.ClearPreview?.Invoke();
            return;
        }

        context.State.PressedOnExistingRoad = false;
        context.State.PressedRoadStrokeId = 0;
        context.State.PendingStartCell = cell;
        context.State.CurrentDragCell = cell;
        context.State.IsDrawing = true;
        context.State.DragFirstAxis = DragFirstAxis.None;
        context.UpdatePreview?.Invoke();
    }

    private void HandlePointerReleased(Context context, bool hasHoveredCell, Vector2Int cell)
    {
        if (context.SessionSystem.ShouldSkipBuildClick(context.SessionState))
            return;

        if (context.State.PressedOnExistingRoad)
        {
            if (hasHoveredCell && cell == context.State.PressedRoadCell)
            {
                context.SessionSystem.SetDeletePrompt(
                    context.SessionState,
                    context.State.PressedRoadStrokeId,
                    "Delete the clicked road?");
            }

            context.State.PressedOnExistingRoad = false;
            context.State.PressedRoadStrokeId = 0;
            return;
        }

        if (!context.State.IsDrawing || !context.State.PendingStartCell.HasValue)
            return;

        if (hasHoveredCell)
            context.State.CurrentDragCell = cell;

        List<Vector2Int> path = context.PathPlanningSystem.BuildPath(
            context.State.PendingStartCell.Value,
            context.State.CurrentDragCell,
            context.State.DragFirstAxis);
        if (path.Count > 1 &&
            (context.IsRoadPathSurfaceValid == null || context.IsRoadPathSurfaceValid(path)))
        {
            context.CreateStroke?.Invoke(path);
        }

        CancelPendingBuild(context);
    }

    private void UpdateDragAxis(Context context, Vector2Int hoveredCell)
    {
        if (!context.State.PendingStartCell.HasValue)
            return;

        context.State.DragFirstAxis = context.PathPlanningSystem.ResolveDragFirstAxis(
            context.State.PendingStartCell.Value,
            hoveredCell,
            context.State.DragFirstAxis);
    }

    public bool IsPointerOverUI(Vector2 screenPosition)
    {
        return false;
    }
}
