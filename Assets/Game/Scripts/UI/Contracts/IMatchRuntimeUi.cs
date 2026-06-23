using System;
using UnityEngine;

public interface IMatchRuntimeUi
{
    bool IsBuildDrawerOpen { get; }

    void ApplyMatchHudCommandMode(TacticalCommandMode mode);

    bool CanTriggerSelectionModeFromHold();

    void ClearMatchHudCommandMode();

    void ConfigureMatchHudRuntimeFeedbackSinkBinding(Action<IBattleHudRuntimeFeedbackSink> bindMatchHudRuntimeFeedback);

    void ConfigureMatchHudSelectionPanelBinding(Action<IMatchHudSelectionPanelView> bindMatchHudSelectionPanel);

    void ConfigureMatchHudSquadTrayBinding(Action<IMatchHudSquadTrayView> bindMatchHudSquadTray);

    bool IsPointerOverAnyGameplayUi(Vector2 screenPosition, out string source);

    bool IsPointerOverBuildToolMenu(Vector2 screenPosition);

    bool IsPointerOverPlacementUi(Vector2 screenPosition);

    bool IsPointerOverRaycastableUi(Vector2 screenPosition, out string source);

    bool IsPointerOverSelectionCancelUi(Vector2 screenPosition);

    bool IsPointerOverUnitCommandUi(Vector2 screenPosition, out string source);

    bool IsPointerOverZoomControls(Vector2 screenPosition);

    void NotifyStaticMinimapChanged();

    bool ShouldIgnoreBuildingSelectionThisFrame();

    void TriggerSelectionCancel();

    void TriggerSelectionModeFromHold();

    void Update();
}
