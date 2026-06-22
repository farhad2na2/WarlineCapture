using System;

public sealed class RoadBuildSessionSystem
{
    public enum BuildToolMode
    {
        None,
        Road,
        SoldierBase
    }

    public sealed class State
    {
        public BuildToolMode ActiveBuildTool;
        public RoadNetworkSystem.Snapshot RoadBuildSessionSnapshot;
        public int? PendingDeleteStrokeId;
        public string PendingDeleteMessage;
        public int SkipBuildClickFrames;
    }

    public struct Context
    {
        public readonly State State;
        public RuntimeGameplayStateSystem RuntimeGameplayStateSystem;
        public readonly Func<RoadNetworkSystem.Snapshot> CaptureRoadBuildSessionSnapshot;
        public readonly Action<RoadNetworkSystem.Snapshot> RestoreRoadBuildSession;
        public readonly Action RemoveRuntimeBlockersUnderRoads;
        public readonly Action NotifyStaticMinimapChanged;
        public readonly Action ApplyBuildCommandMode;
        public readonly Action ClearCommandMode;
        public readonly Action ClearSelectedBuilding;
        public readonly Action CancelBuildingPlacement;
        public readonly Action CancelPendingBuild;
        public readonly Action HidePlacementOutline;
        public readonly Action UpdatePreview;

        public Context(
            State state,
            RuntimeGameplayStateSystem runtimeGameplayStateSystem,
            Func<RoadNetworkSystem.Snapshot> captureRoadBuildSessionSnapshot,
            Action<RoadNetworkSystem.Snapshot> restoreRoadBuildSession,
            Action removeRuntimeBlockersUnderRoads,
            Action notifyStaticMinimapChanged,
            Action applyBuildCommandMode,
            Action clearCommandMode,
            Action clearSelectedBuilding,
            Action cancelBuildingPlacement,
            Action cancelPendingBuild,
            Action hidePlacementOutline,
            Action updatePreview)
        {
            State = state;
            RuntimeGameplayStateSystem = runtimeGameplayStateSystem;
            CaptureRoadBuildSessionSnapshot = captureRoadBuildSessionSnapshot;
            RestoreRoadBuildSession = restoreRoadBuildSession;
            RemoveRuntimeBlockersUnderRoads = removeRuntimeBlockersUnderRoads;
            NotifyStaticMinimapChanged = notifyStaticMinimapChanged;
            ApplyBuildCommandMode = applyBuildCommandMode;
            ClearCommandMode = clearCommandMode;
            ClearSelectedBuilding = clearSelectedBuilding;
            CancelBuildingPlacement = cancelBuildingPlacement;
            CancelPendingBuild = cancelPendingBuild;
            HidePlacementOutline = hidePlacementOutline;
            UpdatePreview = updatePreview;
        }
    }

    public bool IsRoadBuildModeActive(Context context) =>
        context.RuntimeGameplayStateSystem.BuildModeActive &&
        context.State.ActiveBuildTool == BuildToolMode.Road;

    public bool IsActiveTool(State state, BuildToolMode tool) => state.ActiveBuildTool == tool;

    public void SetActiveTool(State state, BuildToolMode tool)
    {
        state.ActiveBuildTool = tool;
    }

    public void ResetSkipBuildClickFrames(State state)
    {
        state.SkipBuildClickFrames = 0;
    }

    public bool ShouldSkipBuildClick(State state) => state.SkipBuildClickFrames > 0;

    public bool HasDeletePrompt(State state) => state.PendingDeleteStrokeId.HasValue;

    public string GetDeletePromptMessage(State state, string fallback)
    {
        return state.PendingDeleteMessage ?? fallback;
    }

    public bool TryGetDeleteStrokeId(State state, out int strokeId)
    {
        if (state.PendingDeleteStrokeId.HasValue)
        {
            strokeId = state.PendingDeleteStrokeId.Value;
            return true;
        }

        strokeId = 0;
        return false;
    }

    public bool TryConsumeSkipBuildClickFrame(State state)
    {
        if (state.SkipBuildClickFrames <= 0)
            return false;

        state.SkipBuildClickFrames--;
        return true;
    }

    public void SetDeletePrompt(State state, int strokeId, string message)
    {
        state.PendingDeleteStrokeId = strokeId;
        state.PendingDeleteMessage = message;
    }

    public void ClearDeletePrompt(State state)
    {
        state.PendingDeleteStrokeId = null;
        state.PendingDeleteMessage = null;
        state.SkipBuildClickFrames = 2;
    }

    public bool ActivateRoadBuildMode(Context context)
    {
        context.RuntimeGameplayStateSystem.BuildModeActive = true;
        context.RuntimeGameplayStateSystem.SelectionModeActive = false;
        context.ApplyBuildCommandMode?.Invoke();

        if (context.State.ActiveBuildTool != BuildToolMode.Road)
            BeginRoadBuildSession(context);

        context.State.ActiveBuildTool = BuildToolMode.Road;
        context.ClearSelectedBuilding?.Invoke();
        context.CancelBuildingPlacement?.Invoke();
        context.UpdatePreview?.Invoke();
        return true;
    }

    public bool ActivateSoldierBaseMode(Context context)
    {
        context.RuntimeGameplayStateSystem.BuildModeActive = true;
        context.RuntimeGameplayStateSystem.SelectionModeActive = false;
        context.ApplyBuildCommandMode?.Invoke();
        context.State.ActiveBuildTool = BuildToolMode.SoldierBase;
        context.State.PendingDeleteStrokeId = null;
        context.State.PendingDeleteMessage = null;
        context.CancelPendingBuild?.Invoke();
        context.ClearSelectedBuilding?.Invoke();
        return true;
    }

    public void ConfirmRoadBuildSession(Context context)
    {
        context.RemoveRuntimeBlockersUnderRoads?.Invoke();
        context.State.RoadBuildSessionSnapshot = null;
        context.NotifyStaticMinimapChanged?.Invoke();
    }

    public bool CancelRoadBuildSession(Context context)
    {
        if (context.State.RoadBuildSessionSnapshot == null)
            return false;

        context.RestoreRoadBuildSession?.Invoke(context.State.RoadBuildSessionSnapshot);
        context.State.RoadBuildSessionSnapshot = null;
        context.NotifyStaticMinimapChanged?.Invoke();
        return true;
    }

    public void ExitBuildMode(Context context)
    {
        context.RuntimeGameplayStateSystem.BuildModeActive = false;
        context.State.ActiveBuildTool = BuildToolMode.None;
        context.CancelPendingBuild?.Invoke();
        context.CancelBuildingPlacement?.Invoke();
        context.ClearSelectedBuilding?.Invoke();
        ClearDeletePrompt(context.State);
        context.HidePlacementOutline?.Invoke();
        context.ClearCommandMode?.Invoke();
    }

    private void BeginRoadBuildSession(Context context)
    {
        context.State.RoadBuildSessionSnapshot = context.CaptureRoadBuildSessionSnapshot?.Invoke();
    }
}
