using System;
using Unity.Entities;
using UnityEngine;

public sealed class RtsSelectionFocusCommandContextSystem
{
    public RtsSelectionFocusCommandSystem.Context Create(
        RuntimeGameplayStateSystem runtimeGameplayStateSystem,
        RtsSelectionInputSystem inputSystem,
        SelectionStateSystem selectionStateSystem,
        FocusedUnitLifecycleSystem focusedUnitLifecycleSystem,
        BuildingPlacementInteractionSystem buildingPlacementInteractionSystem,
        BuildingPlacementInteractionSystem.Context buildingPlacementInteractionContext,
        Camera worldCamera,
        RtsSelectionFocusCommandSystem.TryGetEntityManagerDelegate tryGetEntityManager,
        Action<EntityManager> ensureRuntimeSelectionDependencies,
        Action<EntityManager, string> clearCurrentSelection,
        Action<Rect, RtsSelectionPointerRequestKind, VisibleUnitSelectionSystem.Filter> queueSelectionRectangleRequest,
        Action processSelectionRectangleRequests,
        SelectionHudFeedbackBoundary hudFeedbackSystem,
        SelectionHudFeedbackBoundary.Context hudFeedbackContext,
        Action<bool> setCameraDragging,
        Action<bool> setExplicitAttackTargetModeActive,
        Action<string> logSelectionDiagnostic,
        FocusedUnitLifecycleSystem.DescribeEntityDelegate describeEntity,
        Func<Vector2, bool> tryFocusScreenPosition)
    {
        return new RtsSelectionFocusCommandSystem.Context(
            runtimeGameplayStateSystem,
            inputSystem,
            selectionStateSystem,
            focusedUnitLifecycleSystem,
            buildingPlacementInteractionSystem,
            buildingPlacementInteractionContext,
            worldCamera,
            tryGetEntityManager,
            ensureRuntimeSelectionDependencies,
            clearCurrentSelection,
            queueSelectionRectangleRequest,
            processSelectionRectangleRequests,
            (em, entity) => hudFeedbackSystem.ApplySelection(hudFeedbackContext, em, entity),
            result => hudFeedbackSystem.ApplyCommandResult(hudFeedbackContext, result),
            mode => hudFeedbackSystem.ApplyCommandMode(hudFeedbackContext, mode),
            () => hudFeedbackSystem.ClearSelection(hudFeedbackContext),
            () => hudFeedbackSystem.ClearCommandMode(hudFeedbackContext),
            visible => hudFeedbackSystem.SetWorldMarkersVisible(hudFeedbackContext, visible),
            setCameraDragging,
            setExplicitAttackTargetModeActive,
            logSelectionDiagnostic,
            describeEntity,
            tryFocusScreenPosition);
    }
}
