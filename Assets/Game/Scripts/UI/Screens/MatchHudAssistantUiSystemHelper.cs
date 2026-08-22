using System;
using Game.UI.Contracts;

namespace Game.UI.Runtime
{
    internal sealed partial class MatchHudAssistantUiSystemHelper
    {
        private const float TutorialStepDelaySeconds = 2f;

        private byte _pendingTutorialStep;
        private byte _completedTutorialStep;
        private byte _displayedTutorialStep;
        private float _tutorialShowAtUnscaledTime = -1f;
        private bool _tutorialCinematicSuspended;
        private bool _finalTutorialSuppressed;

        private void QueueTutorialPresentation(UiAssistantPanelModel model, byte previousTutorialStep)
        {
            if (!model.HasRecommendation || model.TutorialStep == 0)
            {
                if (previousTutorialStep != 0 || _displayedTutorialStep != 0)
                    ClosePanelWithoutInputCapture();
                _pendingTutorialStep = 0;
                _displayedTutorialStep = 0;
                _tutorialShowAtUnscaledTime = -1f;
                return;
            }
            if (_finalTutorialSuppressed || model.TutorialStep <= _completedTutorialStep)
            {
                ClosePanelWithoutInputCapture();
                return;
            }

            if (model.TutorialStep == _displayedTutorialStep || model.TutorialStep == _pendingTutorialStep)
                return;

            if (previousTutorialStep != 0 && model.TutorialStep > previousTutorialStep)
                _completedTutorialStep = Math.Max(_completedTutorialStep, previousTutorialStep);
            ClosePanelWithoutInputCapture();
            _displayedTutorialStep = 0;
            _pendingTutorialStep = model.TutorialStep;
            _tutorialShowAtUnscaledTime = -1f;
        }

        private void TickTutorialPresentation(float unscaledTime)
        {
            bool cinematicLocked =
                UiShellRuntimeGateway.TryReadMissionHudRestrictions(
                    out UiMissionHudRestrictionsModel restrictions) &&
                restrictions.CinematicInteractionLocked;
            if (cinematicLocked)
            {
                SuspendForCinematic();
                return;
            }
            if (_finalTutorialSuppressed || _pendingTutorialStep == 0)
                return;

            bool firstInstruction = _completedTutorialStep == 0 && _pendingTutorialStep == 1;
            if (_tutorialCinematicSuspended)
            {
                _tutorialCinematicSuspended = false;
                _tutorialShowAtUnscaledTime = firstInstruction
                    ? unscaledTime
                    : unscaledTime + TutorialStepDelaySeconds;
            }
            else if (_tutorialShowAtUnscaledTime < 0f)
            {
                _tutorialShowAtUnscaledTime = firstInstruction
                    ? unscaledTime
                    : unscaledTime + TutorialStepDelaySeconds;
            }
            if (unscaledTime < _tutorialShowAtUnscaledTime)
                return;

            _displayedTutorialStep = _pendingTutorialStep;
            if (!IsPanelOpen)
                SetPanelOpen(true);
        }

        private void HandleSquadSelectionAcknowledged()
        {
            if (_lastPanelModel.TutorialStep == 1)
                CompleteTutorialStep(1, finalStep: false);
        }

        private void CompleteTutorialStep(byte step, bool finalStep)
        {
            if (step == 0)
                return;
            _completedTutorialStep = Math.Max(_completedTutorialStep, step);
            _pendingTutorialStep = 0;
            _displayedTutorialStep = 0;
            _tutorialShowAtUnscaledTime = -1f;
            _finalTutorialSuppressed |= finalStep;
            ClosePanelWithoutInputCapture();
        }

        private void ScheduleTutorialSubstep(byte step, float unscaledTime)
        {
            if (_finalTutorialSuppressed || step == 0 || step <= _completedTutorialStep)
                return;
            _pendingTutorialStep = step;
            _displayedTutorialStep = 0;
            _tutorialShowAtUnscaledTime = unscaledTime + TutorialStepDelaySeconds;
            ClosePanelWithoutInputCapture();
        }

        private void ClearTutorialPresentationState()
        {
            _pendingTutorialStep = 0;
            _completedTutorialStep = 0;
            _displayedTutorialStep = 0;
            _tutorialShowAtUnscaledTime = -1f;
            _tutorialCinematicSuspended = false;
            _finalTutorialSuppressed = false;
        }
    }
}
