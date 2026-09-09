using UnityEngine;
using UnityEngine.UI;
using Game.UI.Contracts;
using Game.Tactical.Contracts;

namespace Game.UI.Runtime
{
    internal sealed partial class MatchHudAssistantUiSystemHelper
    {
        private void ShowRecommendation()
        {
            CaptureUiOnly();
            TryShowRecommendation();
        }

        private bool TryShowRecommendation(bool preferPanelRecommendation = false)
        {
            if (UiShellRuntimeGateway.TryReadMissionExtraction(out _)) return UiShellRuntimeGateway.TryRequestExtractionAction(UiMissionExtractionAction.ShowLesson);
            if (_lastPanelModel.TutorialStepCount==12) return ShowDefenseGuidance();
            if (!UiShellRuntimeGateway.TryEnqueueAssistantCommandIntent(UiAssistantCommandIntentKind.ShowRecommendation))
                return false;

            // The structured panel projection can trail the highlight projection by one
            // simulation update after the player completes a tutorial action. Prefer the
            // active highlight so the next explicit Show Me teaches the current step rather
            // than replaying the previous panel recommendation.
            bool useHighlightRecommendation =
                !preferPanelRecommendation &&
                _lastHighlightModel.Active &&
                (_lastPanelModel.RecommendationKind == 0 ||
                 _lastHighlightModel.RecommendationKind == _lastPanelModel.RecommendationKind);
            byte recommendationKind = useHighlightRecommendation
                ? _lastHighlightModel.RecommendationKind
                : _lastPanelModel.RecommendationKind;
            byte targetKind = useHighlightRecommendation
                ? _lastHighlightModel.TargetKind
                : _lastPanelModel.RecommendationTargetKind;
            _highlightPresentationSystem.BeginPendingShowMe(recommendationKind, targetKind);
            if (_lastPanelModel.TutorialStep == 0)
                SetPanelOpen(false);
            return true;
        }

        private void ExecuteRecommendation()
        {
            CaptureUiOnly();
            if (UiShellRuntimeGateway.TryReadMissionExtraction(out _)) { ExecuteExtractionGuidance(); return; }
            if (_lastPanelModel.TutorialStepCount==12) { ExecuteDefenseGuidance(); return; }
            if (IsM02DoItStep(in _lastPanelModel))
            {
                if (TryExecuteM02DoIt(in _lastPanelModel))
                {
                    ClearPendingM02DoIt();
                    ClosePanelWithoutInputCapture();
                }
                else
                {
                    QueueM02DoItRetry(_lastPanelModel.TutorialStep, Time.unscaledTime);
                }
                return;
            }
            if (TryAdvanceTutorialCommandSubstep())
                return;

            if (!UiShellRuntimeGateway.TryEnqueueAssistantCommandIntent(
                    UiAssistantCommandIntentKind.ExecuteRecommendation,
                    fromTakeover: true))
                return;

            if (_lastPanelModel.TutorialStep == 0)
                SetPanelOpen(false);
            else
                CompleteTutorialStep(
                    _lastPanelModel.TutorialStep,
                    finalStep: _lastPanelModel.RecommendationKind == 3);
        }

        private bool TryAdvanceTutorialCommandSubstep()
        {
            if (_lastPanelModel.TutorialStep == 0)
                return false;

            TacticalCommandMode requiredMode = _lastPanelModel.RecommendationKind switch
            {
                2 => TacticalCommandMode.Move,
                3 => TacticalCommandMode.Attack,
                _ => TacticalCommandMode.None
            };
            if (requiredMode == TacticalCommandMode.None || _activeCommandMode == requiredMode)
                return false;

            Button commandButton = requiredMode == TacticalCommandMode.Move
                ? _commandControlsView?.MoveButton
                : _commandControlsView?.AttackButton;
            if (commandButton != null && commandButton.IsActive() && commandButton.IsInteractable())
                commandButton.onClick.Invoke();

            // Tutorial automation must never bypass the command-button instruction.
            return true;
        }

        private void StopAssistantControl()
        {
            CaptureUiOnly();
            UiShellRuntimeGateway.TryEnqueueAssistantCommandIntent(UiAssistantCommandIntentKind.StopAssistantControl);
        }

    }
}
