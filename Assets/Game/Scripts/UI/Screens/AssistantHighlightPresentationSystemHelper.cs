using Game.UI.Contracts;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    internal sealed class AssistantHighlightPresentationSystemHelper
    {
        private Image _panelPulse;
        private uint _lastVersion = uint.MaxValue;

        public UiAssistantHighlightModel LastAppliedModel { get; private set; } = UiAssistantHighlightModel.Empty;

        public void Bind(Image panelPulse)
        {
            _panelPulse = panelPulse;
            _lastVersion = uint.MaxValue;
            LastAppliedModel = UiAssistantHighlightModel.Empty;
            ApplyVisual(UiAssistantHighlightModel.Empty);
        }

        public void Unbind()
        {
            _panelPulse = null;
            _lastVersion = uint.MaxValue;
            LastAppliedModel = UiAssistantHighlightModel.Empty;
        }

        public void ApplyReadModel(UiAssistantHighlightModel model)
        {
            if (_lastVersion == model.Version)
                return;

            _lastVersion = model.Version;
            LastAppliedModel = model;
            ApplyVisual(model);
        }

        private void ApplyVisual(UiAssistantHighlightModel model)
        {
            if (_panelPulse == null)
                return;

            _panelPulse.gameObject.SetActive(model.Active);
            float strength = Mathf.Clamp01(model.Strength);
            _panelPulse.color = new Color(0.45f, 0.95f, 1f, 0.18f + strength * 0.32f);
        }
    }
}
