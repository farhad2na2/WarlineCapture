using System;
using System.Collections.Generic;
using UnityEngine;

internal sealed class RoadBuildInteractionContextSystem
{
    public struct Context
    {
        public RuntimeGameplayStateSystem RuntimeGameplayStateSystem;
        public readonly RoadBuildSessionCompositionSystemHelper SessionSystem;
        public readonly RoadBuildSessionCompositionSystemHelper.State SessionState;
        public readonly RoadBuildInputCompositionSystemHelper InputSystem;
        public readonly RoadBuildInputCompositionSystemHelper.State InputState;
        public readonly RoadBuildCommandCompositionSystemHelper CommandSystem;
        public readonly RoadPathPlanningSystem PathPlanningSystem;
        public readonly RoadNetworkSystem NetworkSystem;
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
        public readonly RoadBuildInputCompositionSystemHelper.TryGetHoveredCellAction TryGetHoveredCell;
        public readonly Action ClearPreview;
        public readonly Action<Vector2> UpdateBuildingPlacement;
        public readonly Action<List<Vector2Int>> CreateStroke;
        public readonly Func<List<Vector2Int>, bool> IsRoadPathSurfaceValid;
        public readonly Func<bool> HasActiveBuildingPlacement;
        public readonly Action<bool> SetIsDraggingBuildingPlacement;
        public readonly Action ClearRoadBuildDragState;
        public readonly Action<int> DeleteStroke;

        public Context(
            RuntimeGameplayStateSystem runtimeGameplayStateSystem,
            RoadBuildSessionCompositionSystemHelper sessionSystem,
            RoadBuildSessionCompositionSystemHelper.State sessionState,
            RoadBuildInputCompositionSystemHelper inputSystem,
            RoadBuildInputCompositionSystemHelper.State inputState,
            RoadBuildCommandCompositionSystemHelper commandSystem,
            RoadPathPlanningSystem pathPlanningSystem,
            RoadNetworkSystem networkSystem,
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
            Action updatePreview,
            RoadBuildInputCompositionSystemHelper.TryGetHoveredCellAction tryGetHoveredCell,
            Action clearPreview,
            Action<Vector2> updateBuildingPlacement,
            Action<List<Vector2Int>> createStroke,
            Func<List<Vector2Int>, bool> isRoadPathSurfaceValid,
            Func<bool> hasActiveBuildingPlacement,
            Action<bool> setIsDraggingBuildingPlacement,
            Action clearRoadBuildDragState,
            Action<int> deleteStroke)
        {
            RuntimeGameplayStateSystem = runtimeGameplayStateSystem;
            SessionSystem = sessionSystem;
            SessionState = sessionState;
            InputSystem = inputSystem;
            InputState = inputState;
            CommandSystem = commandSystem;
            PathPlanningSystem = pathPlanningSystem;
            NetworkSystem = networkSystem;
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
            TryGetHoveredCell = tryGetHoveredCell;
            ClearPreview = clearPreview;
            UpdateBuildingPlacement = updateBuildingPlacement;
            CreateStroke = createStroke;
            IsRoadPathSurfaceValid = isRoadPathSurfaceValid;
            HasActiveBuildingPlacement = hasActiveBuildingPlacement;
            SetIsDraggingBuildingPlacement = setIsDraggingBuildingPlacement;
            ClearRoadBuildDragState = clearRoadBuildDragState;
            DeleteStroke = deleteStroke;
        }
    }

    public RoadBuildSessionCompositionSystemHelper.Context CreateSessionContext(Context context)
    {
        return new RoadBuildSessionCompositionSystemHelper.Context(
            context.SessionState,
            context.RuntimeGameplayStateSystem,
            context.CaptureRoadBuildSessionSnapshot,
            context.RestoreRoadBuildSession,
            context.RemoveRuntimeBlockersUnderRoads,
            context.NotifyStaticMinimapChanged,
            context.ApplyBuildCommandMode,
            context.ClearCommandMode,
            context.ClearSelectedBuilding,
            context.CancelBuildingPlacement,
            context.CancelPendingBuild,
            context.HidePlacementOutline,
            context.UpdatePreview);
    }

    public RoadBuildInputCompositionSystemHelper.Context CreateInputContext(Context context)
    {
        return new RoadBuildInputCompositionSystemHelper.Context(
            context.InputState,
            context.RuntimeGameplayStateSystem,
            context.SessionSystem,
            context.SessionState,
            context.PathPlanningSystem,
            context.NetworkSystem,
            context.TryGetHoveredCell,
            context.ClearPreview,
            context.UpdatePreview,
            context.HidePlacementOutline,
            context.UpdateBuildingPlacement,
            context.CreateStroke,
            context.IsRoadPathSurfaceValid,
            context.HasActiveBuildingPlacement,
            context.SetIsDraggingBuildingPlacement);
    }

    public RoadBuildCommandCompositionSystemHelper.Context CreateCommandContext(Context context)
    {
        return new RoadBuildCommandCompositionSystemHelper.Context(
            context.RuntimeGameplayStateSystem,
            context.SessionSystem,
            CreateSessionContext(context),
            context.ClearRoadBuildDragState);
    }

    public RoadDeletePromptSystem.Context CreateDeletePromptContext(Context context)
    {
        return new RoadDeletePromptSystem.Context(
            context.RuntimeGameplayStateSystem,
            context.SessionSystem,
            context.SessionState,
            context.DeleteStroke);
    }
}
