namespace Game.UI.Runtime
{
    public sealed partial class MainMenuPlayUI
    {
        private UnityEngine.UI.Button ResolveMatchHudBuildGuidanceButton()
        {
            // V3 owns Build in the footer command rail. Retain the legacy right-rail
            // route only as a compatibility fallback for older HUD compositions.
            return _matchHudCommandControlsView != null &&
                   _matchHudCommandControlsView.BuildButton != null
                ? _matchHudCommandControlsView.BuildButton
                : _matchHudRightQuickRailView != null
                    ? _matchHudRightQuickRailView.BuildButton
                    : null;
        }

        private void RebindAssistantBuildGuidanceButton() =>
            _matchHudAssistantUiSystem.BindBuildButton(
                ResolveMatchHudBuildGuidanceButton());

        private void RebindAssistant()
        {
            // Bind resets the assistant presentation helper, so restore controls that the
            // Match HUD may already have installed before its late ARIA binding completes.
            _matchHudAssistantUiSystem.BindSquadTray(_matchHudSquadTrayView);
            _matchHudAssistantUiSystem.BindCommandControls(_matchHudCommandControlsView);
            RebindAssistantBuildGuidanceButton();
            _matchHudAssistantUiSystem.BindBuildDrawer(_buildDrawerView);
            _matchHudAssistantUiSystem.BindWorldCamera(_guidanceWorldCamera);
            _matchHudAssistantUiSystem.BindResourceStrip(_guidedResourceStrip);
            _nextAssistantPanelRefreshTime = 0f;
        }
    }
}
