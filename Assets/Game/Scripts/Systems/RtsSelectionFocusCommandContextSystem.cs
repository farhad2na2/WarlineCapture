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
        UnitTargetOrderSystem unitTargetOrderSystem,
        BuildingPlacementInteractionSystem buildingPlacementInteractionSystem,
        BuildingPlacementInteractionSystem.Context buildingPlacementInteractionContext,
        Camera worldCamera,
        RtsSelectionFocusCommandSystem.TryGetEntityManagerDelegate tryGetEntityManager,
        Action<EntityManager> ensureRuntimeSelectionDependencies,
        Action<EntityManager, string> clearCurrentSelection,
        Action<Rect, RtsSelectionPointerRequestKind, VisibleUnitSelectionSystem.Filter> queueSelectionRectangleRequest,
        Action processSelectionRectangleRequests,
        SelectionHudFeedbackSystem hudFeedbackSystem,
        SelectionHudFeedbackSystem.Context hudFeedbackContext,
        Action<bool> setCameraDragging,
        Action<bool> setExplicitAttackTargetModeActive,
        Action<string> logSelectionDiagnostic,
        FocusedUnitLifecycleSystem.DescribeEntityDelegate describeEntity,
        RtsSelectionFocusCommandSystem.ValidateControllableEntityDelegate validateControllableEntity,
        RtsSelectionFocusCommandSystem.IsBoardPassengerCandidateDelegate isBoardPassengerCandidate,
        RtsSelectionFocusCommandSystem.IsBoardTransportCandidateDelegate isBoardTransportCandidate,
        Action boardFocusedTransport,
        Func<Vector2, bool> tryFocusScreenPosition)
    {
        return new RtsSelectionFocusCommandSystem.Context(
            runtimeGameplayStateSystem,
            inputSystem,
            selectionStateSystem,
            focusedUnitLifecycleSystem,
            unitTargetOrderSystem,
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
            (direction, boardAllInteractable) => hudFeedbackSystem.ApplyBoardCommandMode(
                hudFeedbackContext,
                direction,
                boardAllInteractable),
            () => hudFeedbackSystem.ClearSelection(hudFeedbackContext),
            () => hudFeedbackSystem.ClearCommandMode(hudFeedbackContext),
            visible => hudFeedbackSystem.SetWorldMarkersVisible(hudFeedbackContext, visible),
            setCameraDragging,
            setExplicitAttackTargetModeActive,
            logSelectionDiagnostic,
            describeEntity,
            validateControllableEntity,
            isBoardPassengerCandidate,
            isBoardTransportCandidate,
            boardFocusedTransport,
            tryFocusScreenPosition);
    }
}
