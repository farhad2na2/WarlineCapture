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
        private UiTutorialNarrationPhase _pendingTutorialPhase;
        private UiTutorialNarrationPhase _displayedTutorialPhase;
        private ushort _narratedTutorialCues;
        private ushort _autoShownTutorialCues;
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
                _pendingTutorialPhase = UiTutorialNarrationPhase.PrimaryAction;
                _displayedTutorialPhase = UiTutorialNarrationPhase.PrimaryAction;
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
            _pendingTutorialPhase = UiTutorialNarrationPhase.PrimaryAction;
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
            _displayedTutorialPhase = _pendingTutorialPhase;
            if (!IsPanelOpen)
                SetPanelOpen(true);
            if (!WasTutorialCueAutoShown(_displayedTutorialStep, _displayedTutorialPhase) &&
                TryShowRecommendation(
                    preferPanelRecommendation:
                    _displayedTutorialPhase == UiTutorialNarrationPhase.PrimaryAction))
            {
                MarkTutorialCueAutoShown(_displayedTutorialStep, _displayedTutorialPhase);
            }
            if (CanUseTutorialNarration(_lastPanelModel.TutorialStepCount) &&
                (_lastPanelModel.TutorialStepCount == 9 ||
                 _lastPanelModel.RecommendationTargetKind != 4) &&
                !WasTutorialCueNarrated(
                    _displayedTutorialStep,
                    _displayedTutorialPhase))
            {
                string narrationText = _popupView?.CurrentTutorialInstructionBody;
                if (string.IsNullOrWhiteSpace(narrationText))
                    narrationText = _lastPanelModel.RecommendationBody;
                if (UiShellRuntimeGateway.TryEnqueueTutorialNarration(
                        _displayedTutorialStep,
                        _lastPanelModel.TutorialStepCount,
                        _displayedTutorialPhase,
                        narrationText))
                {
                    MarkTutorialCueNarrated(
                        _displayedTutorialStep,
                        _displayedTutorialPhase);
                }
            }
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
            _pendingTutorialPhase = UiTutorialNarrationPhase.PrimaryAction;
            _displayedTutorialPhase = UiTutorialNarrationPhase.PrimaryAction;
            _tutorialShowAtUnscaledTime = -1f;
            _finalTutorialSuppressed |= finalStep;
            ClosePanelWithoutInputCapture();
        }

        private void ScheduleTutorialSubstep(byte step, float unscaledTime)
        {
            if (_finalTutorialSuppressed || step == 0 || step <= _completedTutorialStep)
                return;
            _pendingTutorialStep = step;
            _pendingTutorialPhase = UiTutorialNarrationPhase.WorldTarget;
            _displayedTutorialStep = 0;
            _displayedTutorialPhase = UiTutorialNarrationPhase.PrimaryAction;
            _tutorialShowAtUnscaledTime = unscaledTime + TutorialStepDelaySeconds;
            ClosePanelWithoutInputCapture();
        }

        private void ClearTutorialPresentationState()
        {
            ClearPendingM02DoIt();
            _pendingTutorialStep = 0;
            _completedTutorialStep = 0;
            _displayedTutorialStep = 0;
            _pendingTutorialPhase = UiTutorialNarrationPhase.PrimaryAction;
            _displayedTutorialPhase = UiTutorialNarrationPhase.PrimaryAction;
            _narratedTutorialCues = 0;
            _autoShownTutorialCues = 0;
            _tutorialShowAtUnscaledTime = -1f;
            _tutorialCinematicSuspended = false;
            _finalTutorialSuppressed = false;
        }

        private bool WasTutorialCueNarrated(byte step, UiTutorialNarrationPhase phase)
        {
            int bit = TutorialCueBit(step, phase);
            return bit >= 0 && (_narratedTutorialCues & (1 << bit)) != 0;
        }

        private void MarkTutorialCueNarrated(byte step, UiTutorialNarrationPhase phase)
        {
            int bit = TutorialCueBit(step, phase);
            if (bit >= 0)
                _narratedTutorialCues |= (ushort)(1 << bit);
        }

        private bool WasTutorialCueAutoShown(byte step, UiTutorialNarrationPhase phase)
        {
            int bit = TutorialCueBit(step, phase);
            return bit >= 0 && (_autoShownTutorialCues & (1 << bit)) != 0;
        }

        private void MarkTutorialCueAutoShown(byte step, UiTutorialNarrationPhase phase)
        {
            int bit = TutorialCueBit(step, phase);
            if (bit >= 0)
                _autoShownTutorialCues |= (ushort)(1 << bit);
        }

        private static int TutorialCueBit(byte step, UiTutorialNarrationPhase phase)
        {
            if (step is < 1 or > 8 || phase > UiTutorialNarrationPhase.WorldTarget)
                return -1;
            return ((step - 1) * 2) + (int)phase;
        }

        internal static bool CanUseTutorialNarration(byte tutorialStepCount) =>
            tutorialStepCount is 5 or 9;
    }
}
