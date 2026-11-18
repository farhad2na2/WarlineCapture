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

        private void LateUpdate()
        {
            if (feedbackPanel != null && feedbackPanel.activeInHierarchy)
                ApplyV3FeedbackLayout();
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
            if (feedbackIcon != null)
                feedbackIcon.color = accent;
            ApplyV3FeedbackLayout();
        }

        private void ApplyV3FeedbackLayout()
        {
            if (feedbackPanel.transform is not RectTransform panelRect)
                return;

            MainMenuV3SectionLayoutView responsiveLayout = panelRect.GetComponentInParent<MainMenuV3SectionLayoutView>();
            float extraWidth = responsiveLayout != null ? responsiveLayout.LastAppliedExtraWidth : 0f;
            panelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 943f + extraWidth);
            KeepAriaControlsClear(panelRect);
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
