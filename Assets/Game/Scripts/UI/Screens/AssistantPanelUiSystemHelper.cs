using TMPro;
using UnityEngine.UI;
using Game.UI.Contracts;

namespace Game.UI.Runtime
{
    internal sealed class AssistantPanelUiSystemHelper
    {
        private TMP_Text _stateText;
        private TMP_Text _ownershipBodyText;
        private TMP_Text _goalsBodyText;
        private TMP_Text _alertsBodyText;
        private TMP_Text _recommendationBodyText;
        private TMP_Text _nextActionLabelText;
        private TMP_Text _giveControlLabelText;
        private TMP_Text _stopLabelText;
        private Button _nextActionButton;
        private Button _giveControlButton;
        private Button _stopButton;
        private readonly AssistantNarrationPresentationSystemHelper _narrationPresentationSystem = new();
        private uint _lastAppliedReadModelVersion = uint.MaxValue;

        public void Bind(
            TMP_Text stateText,
            TMP_Text ownershipBodyText,
            TMP_Text goalsBodyText,
            TMP_Text alertsBodyText,
            TMP_Text narrationSubtitleText,
            TMP_Text recommendationBodyText,
            Button nextActionButton,
            Button giveControlButton,
            Button stopButton,
            TMP_Text nextActionLabelText,
            TMP_Text giveControlLabelText,
            TMP_Text stopLabelText)
        {
            _stateText = stateText;
            _ownershipBodyText = ownershipBodyText;
            _goalsBodyText = goalsBodyText;
            _alertsBodyText = alertsBodyText;
            _recommendationBodyText = recommendationBodyText;
            _nextActionButton = nextActionButton;
            _giveControlButton = giveControlButton;
            _stopButton = stopButton;
            _nextActionLabelText = nextActionLabelText;
            _giveControlLabelText = giveControlLabelText;
            _stopLabelText = stopLabelText;
            _narrationPresentationSystem.Bind(narrationSubtitleText);
            _lastAppliedReadModelVersion = uint.MaxValue;
        }

        public void Unbind()
        {
            _stateText = null;
            _ownershipBodyText = null;
            _goalsBodyText = null;
            _alertsBodyText = null;
            _recommendationBodyText = null;
            _nextActionLabelText = null;
            _giveControlLabelText = null;
            _stopLabelText = null;
            _nextActionButton = null;
            _giveControlButton = null;
            _stopButton = null;
            _narrationPresentationSystem.Unbind();
            _lastAppliedReadModelVersion = uint.MaxValue;
        }

        public void ApplyReadModel(UiAssistantPanelModel model)
        {
            if (_lastAppliedReadModelVersion == model.Version)
                return;

            _lastAppliedReadModelVersion = model.Version;
            if (_stateText != null)
                _stateText.text = string.IsNullOrWhiteSpace(model.OwnershipText) ? "PLAYER CONTROL" : model.OwnershipText;
            if (_ownershipBodyText != null)
                _ownershipBodyText.text = string.IsNullOrWhiteSpace(model.OwnershipDetailText)
                    ? (string.IsNullOrWhiteSpace(model.OwnershipText) ? "You are issuing orders directly." : model.OwnershipText)
                    : model.OwnershipDetailText;
            if (_goalsBodyText != null)
                _goalsBodyText.text = string.IsNullOrWhiteSpace(model.GoalsText) ? "No active objectives" : model.GoalsText;
            if (_alertsBodyText != null)
                _alertsBodyText.text = string.IsNullOrWhiteSpace(model.AlertsText) ? "No priority alerts" : model.AlertsText;
            _narrationPresentationSystem.ApplySubtitle(model.NarrationSubtitleText);
            if (_recommendationBodyText != null)
            {
                _recommendationBodyText.text = model.HasRecommendation
                    ? $"{model.RecommendationPriorityText}: {model.RecommendationTitle}\n{model.RecommendationBody}"
                    : model.RecommendationBody;
            }

            if (_nextActionButton != null)
                _nextActionButton.interactable = model.CanShow;
            if (_giveControlButton != null)
                _giveControlButton.interactable = model.CanExecute;
            if (_stopButton != null)
                _stopButton.interactable = model.CanStop;
            if (_nextActionLabelText != null)
                _nextActionLabelText.text = string.IsNullOrWhiteSpace(model.RecommendationActionLabel) ? "SHOW ME" : model.RecommendationActionLabel;
            if (_giveControlLabelText != null)
                _giveControlLabelText.text = model.CanExecute ? "DO IT" : "CONTROL LOCKED";
            if (_stopLabelText != null)
                _stopLabelText.text = model.CanStop ? "STOP" : "STOP";
        }
    }
}
