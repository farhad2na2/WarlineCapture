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

        // Called by the existing HUD update after all guided content and ARIA layout changes.
        public void RenderAttention(float unscaledTime)
        {
            _highlightPresentationSystem.TickAttention(unscaledTime);
            var showMe = _embeddedTutorialView?.ShowMeButton;
            TutorialAttentionPulseView.Present(showMe?.transform as RectTransform, unscaledTime,
                !_tutorialCinematicSuspended && showMe != null && showMe.IsActive() && showMe.IsInteractable(),
                _lastPanelModel.TutorialStep * 100 + (int)_activeCommandMode, 4f);
        }

        private bool _focusNextTutorialWorld;
        private float _tutorialFocusPendingUntil;
        private int _tutorialFocusPendingStep;
        private bool UsesNextTutorialAction => _lastPanelModel.TutorialStepCount is 5 or 8 or 9 or 12;

        private bool ShowNextTutorialAction()
        {
            _focusNextTutorialWorld = true;
            try { TickNextTutorialAction(); }
            finally { _focusNextTutorialWorld = false; }
            RefreshShowMeAvailability();
            return _highlightPresentationSystem.HasDirectTutorialTarget;
        }

        private void ShowTutorialWorld(Vector3 position, bool selection, bool waitingArea = false)
        {
            if (waitingArea) _highlightPresentationSystem.ShowTutorialArea(position, 3f, defensive: true);
            else _highlightPresentationSystem.ShowTutorialWorld(position);
            if (_focusNextTutorialWorld && UiShellRuntimeGateway.TryFocusMissionTutorialTarget(selection))
            {
                _tutorialFocusPendingUntil=Time.unscaledTime+2f;
                _tutorialFocusPendingStep=_lastPanelModel.TutorialStep;
            }
        }

        private void RefreshShowMeAvailability()
        {
            if (_embeddedTutorialView?.ShowMeButton == null) return;
            if(!_selectionActionRequested) _embeddedTutorialView.SetSelectionActionAvailable(false);
            if (!UsesNextTutorialAction) { _embeddedTutorialView.ShowMeButton.gameObject.SetActive(true); _embeddedTutorialView.SetContinueAvailable(false); return; }
            if(_highlightPresentationSystem.HasVisibleDirectTutorialTarget) _tutorialFocusPendingUntil=0;
            bool focusing=_tutorialFocusPendingStep==_lastPanelModel.TutorialStep && Time.unscaledTime<_tutorialFocusPendingUntil;
            // A wait, missing target or disabled placement is not an actionable Show Me step.
            _embeddedTutorialView.ShowMeButton.interactable = _lastPanelModel.CanShow &&
                _highlightPresentationSystem.HasDirectTutorialTarget && !_highlightPresentationSystem.HasVisibleDirectTutorialTarget && !_tutorialCinematicSuspended && !focusing;
            _embeddedTutorialView.ShowMeButton.gameObject.SetActive(_embeddedTutorialView.ShowMeButton.interactable);
            // World selection/dragging and targeting are the player's next gesture, not another mode-button click.
            _embeddedTutorialView.SetContinueAvailable(_continueTutorialAction && !_waitingForTutorialAction &&
                _lastPanelModel.CanExecute && !_tutorialCinematicSuspended);
            _embeddedTutorialView.RefreshContentLayout();
            _popupView?.SetShowMeAvailable(_embeddedTutorialView.ShowMeButton.interactable);
        }
    }
}
