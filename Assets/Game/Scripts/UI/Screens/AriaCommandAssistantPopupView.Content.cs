using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Game.Tactical.Contracts;
using Game.UI.Contracts;

namespace Game.UI.Runtime
{
    public sealed partial class AriaCommandAssistantPopupView
    {
        public void ApplyAccessibility(bool largeTextEnabled, bool highContrastEnabled)
        {
            if (_tutorialBriefing != null)
                _tutorialBriefing.ApplyAccessibility(largeTextEnabled, highContrastEnabled);

            byte state = (byte)((largeTextEnabled ? 1 : 0) | (highContrastEnabled ? 2 : 0));
            if (_accessibilityState == state || _accessibilityTexts == null)
                return;

            _accessibilityState = state;
            for (int i = 0; i < _accessibilityTexts.Length; i++)
            {
                TMP_Text text = _accessibilityTexts[i];
                if (text == null)
                    continue;

                float scale = largeTextEnabled ? 1.08f : 1f;
                if (text.enableAutoSizing)
                {
                    text.fontSizeMin = _normalFontSizeMin[i] * scale;
                    text.fontSizeMax = _normalFontSizeMax[i] * scale;
                }
                else
                {
                    text.fontSize = _normalFontSizes[i] * scale;
                }

                text.color = highContrastEnabled
                    ? ResolveHighContrastColor(_normalTextColors[i])
                    : _normalTextColors[i];
            }
        }

        public void ApplyElapsed(bool visible, int elapsedWholeSeconds)
        {
            SetActive(_elapsedChip, visible);
            if (!visible)
                return;

            int totalSeconds = Mathf.Max(0, elapsedWholeSeconds);
            int hours = totalSeconds / 3600;
            int minutes = totalSeconds / 60 % 60;
            int seconds = totalSeconds % 60;
            if (hours > 0)
                _elapsedText.SetText(
                    UiShellRuntimeGateway.Localization.Get("ui.aria.elapsed_hms", "ELAPSED: {0}:{1:00}:{2:00}"),
                    hours,
                    minutes,
                    seconds);
            else
                _elapsedText.SetText(
                    UiShellRuntimeGateway.Localization.Get("ui.aria.elapsed_ms", "ELAPSED: {0:00}:{1:00}"),
                    minutes,
                    seconds);
        }

        public void ApplyGoal(int index, UiAssistantGoalRowModel model)
        {
            if (!TryGetRow(_goalRows, index, out GoalRowBinding row))
                return;

            SetActive(row.Root, model.Visible);
            if (!model.Visible)
                return;

            SetText(row.Title, model.Title);
            SetText(row.Body, model.Body);
            SetText(
                row.State,
                _takeoverVisible
                    ? TakeoverGoalStateText(model.State, model.IsPrimary)
                    : GoalStateText(model.State, model.IsPrimary));
            SetActive(row.Icon, true);
            SetActive(row.StateChip, true);
            SetActive(row.PriorityRail, model.Priority > 0 || model.IsPrimary);
        }

        public void ApplyLegacyGoals(string goalsText)
        {
            HideRows(_goalRows);
            if (string.IsNullOrWhiteSpace(goalsText) || _goalRows == null || _goalRows.Length == 0)
                return;

            GoalRowBinding row = _goalRows[0];
            SetActive(row.Root, true);
            SetText(row.Title, goalsText);
            SetText(row.Body, string.Empty);
            SetText(row.State, string.Empty);
            SetActive(row.Icon, false);
            SetActive(row.StateChip, false);
            SetActive(row.PriorityRail, false);
        }

        public void ApplyAlert(int index, UiAssistantMessageRowModel model)
        {
            ApplyMessage(_alertRows, index, model);
        }

        public void ApplyReport(int index, UiAssistantMessageRowModel model)
        {
            ApplyMessage(_reportRows, index, model);
        }

        public void ApplyLegacyAlerts(string alertsText)
        {
            HideRows(_alertRows);
            HideRows(_reportRows);
            if (string.IsNullOrWhiteSpace(alertsText) || _alertRows == null || _alertRows.Length == 0)
                return;

            MessageRowBinding row = _alertRows[0];
            SetActive(row.Root, true);
            SetText(row.Body, alertsText);
            SetText(row.Detail, string.Empty);
            SetText(row.Priority, string.Empty);
            SetActive(row.Icon, false);
            SetActive(row.PriorityChip, false);
            SetActive(row.PriorityRail, false);
        }

        public void ApplyRecommendation(UiAssistantPanelModel model)
        {
            bool visible = model.HasRecommendation;
            // Tutorial guidance is rendered by the one permanent Match HUD ARIA panel.
            // POP13 remains the optional full command-assistant surface and must never
            // replace that panel with a second tutorial layout.
            SetActive(_landscapeLayout != null ? _landscapeLayout.gameObject : null, true);
            SetActive(_tutorialBriefing != null ? _tutorialBriefing.gameObject : null, false);

            SetText(_recommendationTitle, visible ? model.RecommendationTitle : string.Empty);
            SetText(_recommendationReason, visible ? model.RecommendationBody : string.Empty);
            SetText(_takeoverIntentTitle, visible ? model.RecommendationTitle : string.Empty);
            SetText(_takeoverIntentDetail, visible ? model.RecommendationBody : string.Empty);
            SetText(_recommendationPriorityText, visible ? model.RecommendationPriorityText : string.Empty);
            SetText(
                _recommendationTargetSummary,
                visible && model.TargetLock.Visible ? model.TargetLock.TargetName : string.Empty);
            SetActive(_recommendationSignalLine, visible);

            if (_showMeButton != null)
                _showMeButton.interactable = model.CanShow;
            if (_doItButton != null)
                _doItButton.interactable = model.CanExecute;
            if (_stopButton != null)
                _stopButton.interactable = model.CanStop;
            SetText(_showMeButtonLabel, "SHOW ME");
            SetText(_doItButtonLabel, "DO IT");
            SetText(_stopButtonLabel, _takeoverVisible ? "STOP ARIA" : "STOP");
        }

    }
}
