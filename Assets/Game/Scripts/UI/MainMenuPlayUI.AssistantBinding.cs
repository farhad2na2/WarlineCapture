namespace Game.UI.Runtime
{
    public sealed partial class MainMenuPlayUI
    {
        private void RebindAssistant()
        {
            // Bind resets the assistant presentation helper, so restore controls that the
            // Match HUD may already have installed before its late ARIA binding completes.
            _matchHudAssistantUiSystem.BindSquadTray(_matchHudSquadTrayView);
            _matchHudAssistantUiSystem.BindCommandControls(_matchHudCommandControlsView);
            _matchHudAssistantUiSystem.BindBuildButton(
                _matchHudRightQuickRailView != null ? _matchHudRightQuickRailView.BuildButton : null);
            _matchHudAssistantUiSystem.BindBuildDrawer(_buildDrawerView);
            _matchHudAssistantUiSystem.BindWorldCamera(_guidanceWorldCamera);
            _matchHudAssistantUiSystem.BindResourceStrip(_guidedResourceStrip);
            _nextAssistantPanelRefreshTime = 0f;
        }
    }
}
