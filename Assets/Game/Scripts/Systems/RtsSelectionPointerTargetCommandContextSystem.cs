using System;
using Unity.Entities;
using UnityEngine;

public sealed class RtsSelectionPointerTargetCommandContextSystem
{
    public RtsSelectionPointerTargetCommandSystem.Context Create(
        RuntimeGameplayStateSystem runtimeGameplayStateSystem,
        RtsSelectionInputSystem inputSystem,
        SelectionStateSystem selectionStateSystem,
        FocusedUnitLifecycleSystem focusedUnitLifecycleSystem,
        UnitTargetOrderSystem unitTargetOrderSystem,
        FocusableUnitLookupSystem focusableUnitLookupSystem,
        TransportBoardingCommandSystem transportBoardingCommandSystem,
        UnitTransportBoardingSystem unitTransportBoardingSystem,
        BuildingTargetMoveOrderSystem buildingTargetMoveOrderSystem,
        BuildingPlacementInteractionSystem buildingPlacementInteractionSystem,
        BuildingPlacementInteractionSystem.Context buildingPlacementInteractionContext,
        Camera worldCamera,
        RtsSelectionPointerTargetCommandSystem.TryGetEntityManagerDelegate tryGetEntityManager,
        RtsSelectionPointerTargetCommandSystem.TryGetPointerPositionDelegate tryGetPointerPosition,
        Func<bool> getExplicitAttackTargetModeActive,
        Action<bool> setExplicitAttackTargetModeActive,
        SelectionHudFeedbackSystem hudFeedbackSystem,
        SelectionHudFeedbackSystem.Context hudFeedbackContext,
        Action<EntityManager, string> clearCurrentSelection,
        Action<Vector2> requestMoveOrderScreenMarker,
        Action<bool> setCameraDragging,
        Func<bool> processAttackCommandRequests,
        Func<bool> processTransportCommandRequests,
        Action processMoveCommandRequests,
        Action<string> logSelectionDiagnostic,
        FocusedUnitLifecycleSystem.DescribeEntityDelegate describeEntity)
    {
        return new RtsSelectionPointerTargetCommandSystem.Context(
            runtimeGameplayStateSystem,
            inputSystem,
            selectionStateSystem,
            focusedUnitLifecycleSystem,
            unitTargetOrderSystem,
            focusableUnitLookupSystem,
            transportBoardingCommandSystem,
            unitTransportBoardingSystem,
            buildingTargetMoveOrderSystem,
            buildingPlacementInteractionSystem,
            buildingPlacementInteractionContext,
            worldCamera,
            tryGetEntityManager,
            tryGetPointerPosition,
            getExplicitAttackTargetModeActive,
            setExplicitAttackTargetModeActive,
            mode => hudFeedbackSystem.ApplyCommandMode(hudFeedbackContext, mode),
            result => hudFeedbackSystem.ApplyCommandResult(hudFeedbackContext, result),
            () => hudFeedbackSystem.ClearSelection(hudFeedbackContext),
            () => hudFeedbackSystem.ClearCommandMode(hudFeedbackContext),
            (em, entity) => hudFeedbackSystem.ApplySelection(hudFeedbackContext, em, entity),
            clearCurrentSelection,
            requestMoveOrderScreenMarker,
            setCameraDragging,
            processAttackCommandRequests,
            processTransportCommandRequests,
            processMoveCommandRequests,
            logSelectionDiagnostic,
            describeEntity);
    }
}
