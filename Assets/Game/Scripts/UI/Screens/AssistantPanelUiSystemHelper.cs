using TMPro;
using Game.UI.Contracts;

namespace Game.UI.Runtime
{
    internal sealed class AssistantPanelUiSystemHelper
    {
        private AriaCommandAssistantPopupView _view;
        private TMP_Text _accessStateText;
        private TMP_Text _accessCueText;
        private uint _lastAppliedReadModelVersion = uint.MaxValue;

        public void Bind(
            AriaCommandAssistantPopupView view,
            TMP_Text accessStateText,
            TMP_Text accessCueText)
        {
            _view = view;
            _accessStateText = accessStateText;
            _accessCueText = accessCueText;
            _lastAppliedReadModelVersion = uint.MaxValue;
        }

        public void Unbind()
        {
            _view = null;
            _accessStateText = null;
            _accessCueText = null;
            _lastAppliedReadModelVersion = uint.MaxValue;
        }

        public void ApplyReadModel(UiAssistantPanelModel model)
        {
            if (_view == null || _lastAppliedReadModelVersion == model.Version)
                return;

            _lastAppliedReadModelVersion = model.Version;
            string ownership = string.IsNullOrWhiteSpace(model.OwnershipText)
                ? "PLAYER CONTROL"
                : model.OwnershipText;
            SetText(_accessStateText, ownership);
            _view.ApplyControlState(ownership);
            _view.ApplyAccessibility(model.LargeTextEnabled, model.HighContrastEnabled);
            _view.ApplyElapsed(model.ElapsedVisible, model.ElapsedWholeSeconds);

            bool hasStructuredGoals = model.Goal0.Visible || model.Goal1.Visible || model.Goal2.Visible;
            if (hasStructuredGoals)
            {
                _view.ApplyGoal(0, model.Goal0);
                _view.ApplyGoal(1, model.Goal1);
                _view.ApplyGoal(2, model.Goal2);
            }
            else
            {
                _view.ApplyLegacyGoals(model.GoalsText);
            }

            bool hasStructuredMessages = model.Alert0.Visible ||
                                         model.Alert1.Visible ||
                                         model.Alert2.Visible ||
                                         model.Report0.Visible ||
                                         model.Report1.Visible;
            if (hasStructuredMessages)
            {
                _view.ApplyAlert(0, model.Alert0);
                _view.ApplyAlert(1, model.Alert1);
                _view.ApplyAlert(2, model.Alert2);
                _view.ApplyReport(0, model.Report0);
                _view.ApplyReport(1, model.Report1);
            }
            else
            {
                _view.ApplyLegacyAlerts(model.AlertsText);
            }

            _view.ApplyRecommendation(model);
            _view.ApplyTargetLock(model.TargetLock);
            _view.ApplyNarration(
                model.Narration,
                model.NarrationSubtitleText,
                model.NarrationSubtitlesVisible);
            ApplyAccessCue(model);
        }

        private void ApplyAccessCue(UiAssistantPanelModel model)
        {
            if (_accessCueText == null)
                return;

            int priority = -1;
            priority = MaxVisiblePriority(priority, model.Alert0);
            priority = MaxVisiblePriority(priority, model.Alert1);
            priority = MaxVisiblePriority(priority, model.Alert2);
            if (priority < 0 && model.HasAlerts)
                priority = 1;

            SetText(_accessCueText, priority switch
            {
                3 => "CRITICAL",
                2 => "HIGH",
                1 => "ALERT",
                0 => "REPORT",
                _ => string.Empty
            });
        }

        private static int MaxVisiblePriority(int current, UiAssistantMessageRowModel row)
        {
            return row.Visible && !row.Acknowledged && row.Priority > current
                ? row.Priority
                : current;
        }

        private static void SetText(TMP_Text target, string value)
        {
            if (target == null)
                return;

            string resolved = value ?? string.Empty;
            if (target.text != resolved)
                target.text = resolved;
            if (target.gameObject.activeSelf != (resolved.Length > 0))
                target.gameObject.SetActive(resolved.Length > 0);
        }
    }
}
