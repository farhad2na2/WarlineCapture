using Game.UI.Contracts;
using UnityEngine;

namespace Game.UI.Runtime
{
    internal sealed partial class MatchHudAssistantUiSystemHelper
    {
        public void TickHighlight(float unscaledTime)
        {
            _highlightPresentationSystem.Tick();
            TickPendingM02DoIt(unscaledTime);
            TickTutorialPresentation(unscaledTime);
            TickNextTutorialAction();
            RefreshShowMeAvailability();
        }

        private bool _focusNextTutorialWorld;
        private bool UsesNextTutorialAction => _lastPanelModel.TutorialStepCount == 12 ||
            _lastPanelModel.TutorialStepCount == 9 && _lastPanelModel.TutorialStep is 3 or 4 or 5 or 6;

        private bool ShowNextTutorialAction()
        {
            _focusNextTutorialWorld = true;
            try { TickNextTutorialAction(); }
            finally { _focusNextTutorialWorld = false; }
            RefreshShowMeAvailability();
            return _highlightPresentationSystem.HasDirectTutorialTarget;
        }

        private void ShowTutorialWorld(Vector3 position, bool selection)
        {
            _highlightPresentationSystem.ShowTutorialWorld(position);
            if (_focusNextTutorialWorld) UiShellRuntimeGateway.TryFocusMissionTutorialTarget(selection);
        }

        private void RefreshShowMeAvailability()
        {
            if (!UsesNextTutorialAction || _embeddedTutorialView?.ShowMeButton == null) return;
            // A wait, missing target or disabled placement is not an actionable Show Me step.
            _embeddedTutorialView.ShowMeButton.interactable = _lastPanelModel.CanShow &&
                _highlightPresentationSystem.HasDirectTutorialTarget && !_tutorialCinematicSuspended;
            _popupView?.SetShowMeAvailable(_embeddedTutorialView.ShowMeButton.interactable);
        }
    }
}
