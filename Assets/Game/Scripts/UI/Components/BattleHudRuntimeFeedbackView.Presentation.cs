using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Game.Tactical.Contracts;
using Game.UI.Contracts;

namespace Game.UI.Runtime
{
    public sealed partial class BattleHudRuntimeFeedbackView
    {
        private static void SetText(TMP_Text label, string text)
        {
            text ??= string.Empty;
            if(label.TryGetComponent<V3LocalizedTextBindingView>(out var binding))
            {
                // Update the binding before an inactive feedback panel is shown again.
                // Otherwise OnEnable restores the prefab's authoring/example message.
                binding.SetLocalizedValue(text);
                return;
            }
            if(label.text!=text) label.text=text;
        }

        private void ApplyFeedbackVisuals(MatchHudCommandFeedbackModel model)
        {
            if (feedbackText != null)
                SetText(feedbackText, model.Message);
            ApplyFeedbackIcon(model.Severity);
            ApplyV3SeverityStyle(model.Severity);
            if (feedbackPanel != null && !feedbackPanel.activeSelf)
                feedbackPanel.SetActive(true);
        }

        private void ApplyV3SeverityStyle(CommandFeedbackSeverity severity)
        {
            if (feedbackPanel == null)
                return;

            Color accent = severity switch
            {
                CommandFeedbackSeverity.Ready => new Color32(13, 194, 232, 255),
                CommandFeedbackSeverity.Warning => new Color32(244, 181, 20, 255),
                CommandFeedbackSeverity.Error => new Color32(238, 76, 43, 255),
                _ => new Color32(13, 194, 232, 255)
            };
            V3GradientGraphic gradient = feedbackPanel.GetComponentInChildren<V3GradientGraphic>(true);
            gradient?.SetBorder(accent, 3f);
            if (feedbackIcon != null)
                feedbackIcon.color = accent;
        }

        private void ApplyFeedbackIcon(CommandFeedbackSeverity severity)
        {
            if (feedbackIcon == null)
                return;

            Sprite icon = ResolveFeedbackIcon(severity);
            if (feedbackIcon.sprite != icon)
                feedbackIcon.sprite = icon;
            bool enabled = icon != null;
            if (feedbackIcon.enabled != enabled)
                feedbackIcon.enabled = enabled;
        }

        private Sprite ResolveFeedbackIcon(CommandFeedbackSeverity severity)
        {
            return severity switch
            {
                CommandFeedbackSeverity.Ready => readyIcon != null ? readyIcon : neutralIcon,
                CommandFeedbackSeverity.Warning => warningIcon != null ? warningIcon : neutralIcon,
                CommandFeedbackSeverity.Error => errorIcon != null ? errorIcon : warningIcon != null ? warningIcon : neutralIcon,
                _ => neutralIcon
            };
        }

    }
}
