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
        Action issueHoldPositionOrder,
        Action issueStopOrder,
        Action destroyFocusedUnit,
        Func<bool> issueFocusedMissileLauncherRadarAttack,
        Func<bool> armFocusedAttackTargetMode,
        Action cancelExplicitAttackTargetMode)
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
            () => hudFeedbackSystem.ClearSelection(hudFeedbackContext),
            () => hudFeedbackSystem.ClearCommandMode(hudFeedbackContext),
            visible => hudFeedbackSystem.SetWorldMarkersVisible(hudFeedbackContext, visible),
            setCameraDragging,
            setExplicitAttackTargetModeActive,
            logSelectionDiagnostic,
            describeEntity,
            validateControllableEntity,
            issueHoldPositionOrder,
            issueStopOrder,
            destroyFocusedUnit,
            issueFocusedMissileLauncherRadarAttack,
            armFocusedAttackTargetMode,
            cancelExplicitAttackTargetMode);
    }
}
