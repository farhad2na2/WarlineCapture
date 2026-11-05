using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Game.Tactical.Contracts;
using Game.UI.Contracts;

namespace Game.UI.Runtime
{
    public sealed partial class BattleHudRuntimeFeedbackView
    {
        private AriaTutorialBriefingView _feedbackAria;
        private readonly Vector3[] _feedbackCorners = new Vector3[4];
        private readonly Vector3[] _ariaCorners = new Vector3[4];
        private bool _feedbackExpandedError;

        private void LateUpdate()
        {
            if (feedbackPanel != null && feedbackPanel.activeInHierarchy)
                ApplyV3FeedbackLayout(_feedbackExpandedError);
        }

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
            if (feedbackText != null)
                feedbackText.color = new Color32(244, 246, 242, 255);
            if (feedbackIcon != null)
                feedbackIcon.color = accent;
            _feedbackExpandedError = severity == CommandFeedbackSeverity.Error;
            ApplyV3FeedbackLayout(_feedbackExpandedError);
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
                ? new Vector2(719f, -659f)
                : new Vector2(719f, -683f);
            panelRect.sizeDelta = expandedError
                ? new Vector2(943f + extraWidth, 94f)
                : new Vector2(943f + extraWidth, 70f);

            KeepAriaControlsClear(panelRect);

            if (feedbackIcon != null)
            {
                RectTransform iconRect = feedbackIcon.rectTransform;
                iconRect.anchorMin = iconRect.anchorMax = new Vector2(0f, 1f);
                iconRect.pivot = new Vector2(0f, 1f);
                iconRect.anchoredPosition = expandedError
                    ? new Vector2(16f, -25f)
                    : new Vector2(16f, -15f);
                iconRect.sizeDelta = expandedError ? new Vector2(44f, 44f) : new Vector2(40f, 40f);
            }

            if (feedbackText != null)
            {
                RectTransform textRect = feedbackText.rectTransform;
                textRect.anchorMin = textRect.anchorMax = new Vector2(0f, 1f);
                textRect.pivot = new Vector2(0f, 1f);
                textRect.anchoredPosition = expandedError
                    ? new Vector2(76f, -9f)
                    : new Vector2(76f, -7f);
                textRect.sizeDelta = expandedError
                    ? new Vector2(panelRect.sizeDelta.x - 96f, 76f)
                    : new Vector2(panelRect.sizeDelta.x - 96f, 56f);
                feedbackText.enableAutoSizing = false;
                feedbackText.fontSize = 26f;
                feedbackText.textWrappingMode = TMPro.TextWrappingModes.Normal;
                feedbackText.margin = UnityEngine.Vector4.zero;
            }
        }

        private void KeepAriaControlsClear(RectTransform feedback)
        {
            if (_feedbackAria == null) _feedbackAria = UnityEngine.Object.FindAnyObjectByType<AriaTutorialBriefingView>();
            var aria = _feedbackAria != null ? _feedbackAria.transform as RectTransform : null;
            if (aria == null || !aria.gameObject.activeInHierarchy || !_feedbackAria.IsPresentationVisible) return;
            feedback.GetWorldCorners(_feedbackCorners); aria.GetWorldCorners(_ariaCorners);
            Transform parent = feedback.parent;
            Vector3 lower = parent.InverseTransformPoint(_feedbackCorners[0]);
            Vector3 upper = parent.InverseTransformPoint(_feedbackCorners[2]);
            Vector3 ariaLower = parent.InverseTransformPoint(_ariaCorners[0]);
            Vector3 ariaUpper = parent.InverseTransformPoint(_ariaCorners[2]);
            if (lower.y < ariaUpper.y && upper.y > ariaLower.y && upper.x > ariaLower.x && lower.x < ariaUpper.x)
                feedback.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Max(360f, ariaLower.x - lower.x - 12f));
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
