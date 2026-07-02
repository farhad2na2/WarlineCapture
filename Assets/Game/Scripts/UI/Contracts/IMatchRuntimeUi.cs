using System;
using UnityEngine;
using Game.Tactical.Contracts;

namespace Game.UI.Contracts
{
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

        bool TryShowMatchHudThreatWarning(string title, float visibleUntilTime);

        void Update();
    }
}
