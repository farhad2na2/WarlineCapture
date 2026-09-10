using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Game.Tactical.Contracts;
using Game.UI.Contracts;

namespace Game.UI.Runtime
{
    public sealed partial class BattleHudRuntimeFeedbackView
    {
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
            if (feedbackText != null)
                feedbackText.color = accent;
            if (feedbackIcon != null)
                feedbackIcon.color = accent;
            ApplyV3FeedbackLayout(severity == CommandFeedbackSeverity.Error);
        }

        private void ApplyV3FeedbackLayout(bool expandedError)
        {
            RectTransform panelRect = feedbackPanel != null ? feedbackPanel.transform as RectTransform : null;
            if (panelRect == null)
                return;

            MainMenuV3SectionLayoutView responsiveLayout = panelRect.GetComponentInParent<MainMenuV3SectionLayoutView>();
            float extraWidth = responsiveLayout != null ? responsiveLayout.LastAppliedExtraWidth : 0f;
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(0f, 1f);
            panelRect.pivot = new Vector2(0f, 1f);
            panelRect.anchoredPosition = expandedError
                ? new Vector2(465f, -660f)
                : new Vector2(591f, -710f);
            panelRect.sizeDelta = expandedError
                ? new Vector2(816f + extraWidth, 70f)
                : new Vector2(660f + extraWidth, 48f);

            if (feedbackIcon != null)
            {
                RectTransform iconRect = feedbackIcon.rectTransform;
                iconRect.anchorMin = iconRect.anchorMax = new Vector2(0f, 1f);
                iconRect.pivot = new Vector2(0f, 1f);
                iconRect.anchoredPosition = expandedError
                    ? new Vector2(14f, -13f)
                    : new Vector2(12f, -9f);
                iconRect.sizeDelta = expandedError ? new Vector2(44f, 44f) : new Vector2(30f, 30f);
            }

            if (feedbackText != null)
            {
                RectTransform textRect = feedbackText.rectTransform;
                textRect.anchorMin = textRect.anchorMax = new Vector2(0f, 1f);
                textRect.pivot = new Vector2(0f, 1f);
                textRect.anchoredPosition = expandedError
                    ? new Vector2(66f, -5f)
                    : new Vector2(53f, -4f);
                textRect.sizeDelta = expandedError
                    ? new Vector2(734f + extraWidth, 60f)
                    : new Vector2(590f + extraWidth, 40f);
                feedbackText.fontSize = expandedError ? 24f : 18f;
            }
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
