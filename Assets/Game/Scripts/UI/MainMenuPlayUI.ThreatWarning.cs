namespace Game.UI.Runtime
{
    public sealed partial class MainMenuPlayUI
    {
        public bool TryShowMatchHudThreatWarning(string title, float visibleUntilTime)
        {
            if (_matchHudThreatJumpPanel == null || _matchHudThreatTitle == null)
                return false;

            string resolvedTitle = string.IsNullOrWhiteSpace(title) ? "Threat detected" : title;
            if (_matchHudThreatTitle.text != resolvedTitle)
            {
                var binding=_matchHudThreatTitle.GetComponent<V3LocalizedTextBindingView>();
                if(binding!=null) binding.SetLocalizedValue(resolvedTitle);
                else _matchHudThreatTitle.text=resolvedTitle;
            }
            _matchHudThreatVisibleUntil = visibleUntilTime;
            SetMatchHudThreatWarningVisible(true);
            return true;
        }

        public void TickMatchHudThreatWarning(float now)
        {
            if (_matchHudThreatJumpPanel == null)
                return;

            SetMatchHudThreatWarningVisible(now < _matchHudThreatVisibleUntil);
        }

        private void SetMatchHudThreatWarningVisible(bool visible)
        {
            // Cinematic controls occupy the warning strip; combat time is frozen during the tour.
            visible &= !UiShellRuntimeGateway.TryReadMissionCameraTour() &&
                !(_matchHudSelectionPanelView != null && _matchHudSelectionPanelView.IsPassengerDrawerOpen);
            if (_matchHudThreatJumpPanel == null) return;
            var visibility = _matchHudThreatJumpPanel.GetComponent<MatchHudThreatVisibilityView>();
            if (visibility != null) visibility.SetRequested(visible);
            else if (_matchHudThreatJumpPanel.activeSelf != visible) _matchHudThreatJumpPanel.SetActive(visible);
        }
    }
}
