using System;
using UnityEngine;

public sealed class RtsSelectionRuntimeInputContextSystem
{
    public RtsSelectionRuntimeInputSystem.Context Create(
        RuntimeGameplayStateSystem runtimeGameplayStateSystem,
        RtsSelectionInputSystem inputSystem,
        MainMenuPlayUI mainMenuPlayUi,
        SelectionRuntimeConfigSystem.State runtimeConfig,
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
        Action processSelectionRectangleRequests,
        Action<string> logClickDiagnostic,
        Func<Vector2, string> buildClickDebugSummary)
    {
        return new RtsSelectionRuntimeInputSystem.Context(
            runtimeGameplayStateSystem,
            inputSystem,
            mainMenuPlayUi,
            runtimeConfig.DragThresholdPixels,
            runtimeConfig.SelectionModeHoldSeconds,
            getExplicitAttackTargetModeActive,
            setExplicitAttackTargetModeActive,
            getCameraDragging,
            setCameraDragging,
            isPointerOverAnyUi,
            isPointerOverGameplayUi,
            tryIssueAttackOrderToClickedUnit,
            tryIssueBoardTransportOrderToClickedUnit,
            tryFocusUnit,
            panCamera,
            issueMoveOrder,
            processSelectionRectangleRequests,
            logClickDiagnostic,
            buildClickDebugSummary);
    }
}
