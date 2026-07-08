using TMPro;
using UnityEngine.UI;
using Game.UI.Contracts;

namespace Game.UI.Runtime
{
    internal sealed class AssistantPanelUiSystemHelper
    {
        private TMP_Text _stateText;
        private TMP_Text _goalsBodyText;
        private TMP_Text _recommendationBodyText;
        private TMP_Text _nextActionLabelText;
        private TMP_Text _giveControlLabelText;
        private Button _nextActionButton;
        private Button _giveControlButton;
        private uint _lastAppliedReadModelVersion = uint.MaxValue;

        public void Bind(
            TMP_Text stateText,
            TMP_Text goalsBodyText,
            TMP_Text recommendationBodyText,
            Button nextActionButton,
            Button giveControlButton,
            TMP_Text nextActionLabelText,
            TMP_Text giveControlLabelText)
        {
            _stateText = stateText;
            _goalsBodyText = goalsBodyText;
            _recommendationBodyText = recommendationBodyText;
            _nextActionButton = nextActionButton;
            _giveControlButton = giveControlButton;
            _nextActionLabelText = nextActionLabelText;
            _giveControlLabelText = giveControlLabelText;
            _lastAppliedReadModelVersion = uint.MaxValue;
        }

        public void Unbind()
        {
            _stateText = null;
            _goalsBodyText = null;
            _recommendationBodyText = null;
            _nextActionLabelText = null;
            _giveControlLabelText = null;
            _nextActionButton = null;
            _giveControlButton = null;
            _lastAppliedReadModelVersion = uint.MaxValue;
        }

        public void ApplyReadModel(UiAssistantPanelModel model)
        {
            if (_lastAppliedReadModelVersion == model.Version)
                return;

            _lastAppliedReadModelVersion = model.Version;
            if (_stateText != null)
                _stateText.text = string.IsNullOrWhiteSpace(model.OwnershipText) ? "PLAYER CONTROL" : model.OwnershipText;
            if (_goalsBodyText != null)
                _goalsBodyText.text = string.IsNullOrWhiteSpace(model.GoalsText) ? "No active objectives" : model.GoalsText;
            if (_recommendationBodyText != null)
            {
                _recommendationBodyText.text = model.HasRecommendation
                    ? $"{model.RecommendationPriorityText}: {model.RecommendationTitle}\n{model.RecommendationBody}"
                    : model.RecommendationBody;
            }

            if (_nextActionButton != null)
                _nextActionButton.interactable = model.CanShow;
            if (_giveControlButton != null)
                _giveControlButton.interactable = model.CanTakeControl;
            if (_nextActionLabelText != null)
                _nextActionLabelText.text = string.IsNullOrWhiteSpace(model.RecommendationActionLabel) ? "SHOW ME" : model.RecommendationActionLabel;
            if (_giveControlLabelText != null)
                _giveControlLabelText.text = model.CanTakeControl ? "GIVE CONTROL" : "CONTROL LOCKED";
        }
    }
}
