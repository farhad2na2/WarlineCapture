using System;

public sealed class RoadBuildCommandSystem
{
    public readonly struct Context
    {
        public readonly RuntimeGameplayStateSystem RuntimeGameplayStateSystem;
        public readonly RoadBuildSessionSystem SessionSystem;
        public readonly RoadBuildSessionSystem.Context SessionContext;
        public readonly Action ClearRoadBuildDragState;

        public Context(
            RuntimeGameplayStateSystem runtimeGameplayStateSystem,
            RoadBuildSessionSystem sessionSystem,
            RoadBuildSessionSystem.Context sessionContext,
            Action clearRoadBuildDragState)
        {
            RuntimeGameplayStateSystem = runtimeGameplayStateSystem;
            SessionSystem = sessionSystem;
            SessionContext = sessionContext;
            ClearRoadBuildDragState = clearRoadBuildDragState;
        }
    }

    public bool SetBuildMode(Context context, bool enabled)
    {
        if (enabled && WarlineCaptureMissionRules.TryRejectBuildForActiveMission())
        {
            context.RuntimeGameplayStateSystem.BuildModeActive = false;
            return false;
        }

        context.RuntimeGameplayStateSystem.BuildModeActive = enabled;
        if (enabled)
            context.RuntimeGameplayStateSystem.SelectionModeActive = false;
        return true;
    }

    public bool ActivateRoadBuildMode(Context context)
    {
        return context.SessionSystem.ActivateRoadBuildMode(context.SessionContext);
    }

    public void ConfirmRoadBuildSession(Context context)
    {
        context.SessionSystem.ConfirmRoadBuildSession(context.SessionContext);
    }

    public bool CancelRoadBuildSession(Context context)
    {
        return context.SessionSystem.CancelRoadBuildSession(context.SessionContext);
    }

    public void ExitBuildMode(Context context)
    {
        context.ClearRoadBuildDragState?.Invoke();
        context.SessionSystem.ExitBuildMode(context.SessionContext);
    }
}
