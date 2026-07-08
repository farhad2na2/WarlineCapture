using TMPro;

namespace Game.UI.Runtime
{
    internal sealed class AssistantNarrationPresentationSystemHelper
    {
        private const string FallbackSubtitleText = "No active narration";

        private TMP_Text _subtitleText;
        private string _lastSubtitleText;

        public void Bind(TMP_Text subtitleText)
        {
            _subtitleText = subtitleText;
            _lastSubtitleText = null;
            ApplySubtitle(null);
        }

        public void Unbind()
        {
            _subtitleText = null;
            _lastSubtitleText = null;
        }

        public void ApplySubtitle(string subtitleText)
        {
            string resolvedText = string.IsNullOrWhiteSpace(subtitleText) ? FallbackSubtitleText : subtitleText;
            if (_lastSubtitleText == resolvedText)
                return;

            _lastSubtitleText = resolvedText;
            if (_subtitleText != null)
                _subtitleText.text = resolvedText;
        }
    }
}
