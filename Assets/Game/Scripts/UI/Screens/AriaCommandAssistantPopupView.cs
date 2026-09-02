using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Game.Tactical.Contracts;
using Game.UI.Contracts;

namespace Game.UI.Runtime
{
    [DisallowMultipleComponent]
    public sealed partial class AriaCommandAssistantPopupView : MonoBehaviour
    {
        private sealed class GoalRowBinding
        {
            public GameObject Root;
            public GameObject Icon;
            public GameObject StateChip;
            public GameObject PriorityRail;
            public TMP_Text Title;
            public TMP_Text Body;
            public TMP_Text State;
        }

        private sealed class MessageRowBinding
        {
            public GameObject Root;
            public GameObject Icon;
            public GameObject PriorityChip;
            public GameObject PriorityRail;
            public TMP_Text Body;
            public TMP_Text Detail;
            public TMP_Text Priority;
        }

        private RectTransform _landscapeLayout;
        private RectTransform _commandAssistantPanel;
        private RectTransform _assistantTakeoverSurface;
        private AriaTutorialBriefingView _tutorialBriefing;
        private Button _headerCloseButton;
        private Button _closeButton;
        private Button _showMeButton;
        private Button _doItButton;
        private Button _stopButton;
        private Button _resumeCommandButton;
        private TMP_Text _showMeButtonLabel;
        private TMP_Text _doItButtonLabel;
        private TMP_Text _stopButtonLabel;
        private TMP_Text _controlStateText;
        private GameObject _elapsedChip;
        private TMP_Text _elapsedText;
        private GoalRowBinding[] _goalRows;
        private MessageRowBinding[] _alertRows;
        private MessageRowBinding[] _reportRows;
        private TMP_Text _recommendationTitle;
        private TMP_Text _recommendationReason;
        private TMP_Text _recommendationPriorityText;
        private TMP_Text _recommendationTargetSummary;
        private GameObject _recommendationSignalLine;
        private TMP_Text _takeoverIntentTitle;
        private TMP_Text _takeoverIntentDetail;
        private GameObject _targetLockPanel;
        private TMP_Text _targetNameText;
        private TMP_Text _sourceNameText;
        private TMP_Text _distanceText;
        private TMP_Text _healthText;
        private TMP_Text _factionRelationText;
        private TMP_Text _readinessText;
        private TMP_Text _targetReasonText;
        private GameObject _targetMarker0;
        private GameObject _targetMarker1;
        private GameObject _targetMarker2;
        private Image _previewHighlight;
        private GameObject _narrationStateChip;
        private TMP_Text _narrationStateText;
        private TMP_Text _narrationSubtitle;
        private TMP_Text _narrationFailureReason;
        private GameObject _narrationWaveform;
        private Toggle _voiceEnabledToggle;
        private RectTransform _voiceEnabledKnob;
        private TMP_Text[] _accessibilityTexts;
        private float[] _normalFontSizes;
        private float[] _normalFontSizeMin;
        private float[] _normalFontSizeMax;
        private Color[] _normalTextColors;
        private byte _accessibilityState = byte.MaxValue;
        private bool _hierarchyBound;
        private Action _closeRequested;
        private Action _showRecommendationRequested;
        private Action _executeRecommendationRequested;
        private Action _stopRequested;
        private bool _takeoverVisible;

        public RectTransform LandscapeLayout => _landscapeLayout;
        public RectTransform CommandAssistantPanel => _commandAssistantPanel;
        public RectTransform AssistantTakeoverSurface => _assistantTakeoverSurface;
        public Image PreviewHighlight => _previewHighlight;
        public bool IsOpen => _hierarchyBound && gameObject.activeInHierarchy;
        public string CurrentTutorialInstructionBody =>
            _tutorialBriefing != null ? _tutorialBriefing.CurrentInstructionBody : string.Empty;
        public UiTutorialNarrationPhase CurrentTutorialNarrationPhase =>
            _tutorialBriefing != null
                ? _tutorialBriefing.CurrentNarrationPhase
                : UiTutorialNarrationPhase.PrimaryAction;

        private void Awake()
        {
            TryBindHierarchy();
        }

        private void OnDestroy()
        {
            UnbindActions();
            if (_voiceEnabledToggle != null)
                _voiceEnabledToggle.onValueChanged.RemoveListener(OnVoiceEnabledChanged);
        }

        public bool TryBindHierarchy()
        {
            if (_hierarchyBound)
                return true;

            _landscapeLayout = FindComponent<RectTransform>("LandscapeLayout");
            _commandAssistantPanel = FindComponent<RectTransform>("CommandAssistantPanel");
            _assistantTakeoverSurface = FindComponent<RectTransform>("AssistantTakeoverSurface");
            _tutorialBriefing = GetComponentInChildren<AriaTutorialBriefingView>(true);
            _headerCloseButton = FindComponent<Button>("HeaderCloseButton");
            _closeButton = FindComponent<Button>("CloseButton");
            _showMeButton = FindComponent<Button>("ShowMeButton");
            _doItButton = FindComponent<Button>("DoItButton");
            _stopButton = FindComponent<Button>("StopButton");
            _resumeCommandButton = FindComponent<Button>("ResumeCommandButton");
            _showMeButtonLabel = FindComponent<TMP_Text>("ShowMeButtonLabel");
            _doItButtonLabel = FindComponent<TMP_Text>("DoItButtonLabel");
            _stopButtonLabel = FindComponent<TMP_Text>("StopButtonLabel");
            _controlStateText = FindComponent<TMP_Text>("ControlStateText");
            _elapsedChip = FindObject("ElapsedChip");
            _elapsedText = FindComponent<TMP_Text>("ElapsedText");

            _goalRows = new[]
            {
                BindGoalRow(0),
                BindGoalRow(1),
                BindGoalRow(2)
            };
            _alertRows = new[]
            {
                BindMessageRow("Alert", 0),
                BindMessageRow("Alert", 1),
                BindMessageRow("Alert", 2)
            };
            _reportRows = new[]
            {
                BindMessageRow("Report", 0),
                BindMessageRow("Report", 1)
            };

            _recommendationTitle = FindComponent<TMP_Text>("RecommendationTitle");
            _recommendationReason = FindComponent<TMP_Text>("RecommendationReason");
            _recommendationPriorityText = FindComponent<TMP_Text>("RecommendationPriorityText");
            _recommendationTargetSummary = FindComponent<TMP_Text>("RecommendationTargetSummary");
            _recommendationSignalLine = FindObject("RecommendationSignalLine");
            _takeoverIntentTitle = FindComponent<TMP_Text>("TakeoverIntentTitle");
            _takeoverIntentDetail = FindComponent<TMP_Text>("TakeoverIntentDetail");
            _targetLockPanel = FindObject("TargetLockPanel");
            _targetNameText = FindComponent<TMP_Text>("TargetNameText");
            _sourceNameText = FindComponent<TMP_Text>("SourceNameText");
            _distanceText = FindComponent<TMP_Text>("DistanceText");
            _healthText = FindComponent<TMP_Text>("HealthText");
            _factionRelationText = FindComponent<TMP_Text>("FactionRelationText");
            _readinessText = FindComponent<TMP_Text>("ReadinessText");
            _targetReasonText = FindComponent<TMP_Text>("TargetReasonText");
            _targetMarker0 = FindObject("TargetMarker0");
            _targetMarker1 = FindObject("TargetMarker1");
            _targetMarker2 = FindObject("TargetMarker2");
            _previewHighlight = FindComponent<Image>("RadarScanDisc");
            _narrationStateChip = FindObject("NarrationStateChip");
            _narrationStateText = FindComponent<TMP_Text>("NarrationStateText");
            _narrationSubtitle = FindComponent<TMP_Text>("NarrationSubtitle");
            _narrationFailureReason = FindComponent<TMP_Text>("NarrationFailureReason");
            _narrationWaveform = FindObject("NarrationWaveform");
            _voiceEnabledToggle = FindComponent<Toggle>("VoiceEnabledToggle");
            _voiceEnabledKnob = FindComponent<RectTransform>("EnabledKnob");
            CacheAccessibilityDefaults();

            _hierarchyBound = _landscapeLayout != null &&
                              _commandAssistantPanel != null &&
                              _assistantTakeoverSurface != null &&
                              _headerCloseButton != null &&
                              _closeButton != null &&
                              _showMeButton != null &&
                              _doItButton != null &&
                              _stopButton != null &&
                              _resumeCommandButton != null &&
                              RowsBound(_goalRows) &&
                              RowsBound(_alertRows) &&
                              RowsBound(_reportRows) &&
                              _recommendationTitle != null &&
                              _recommendationReason != null &&
                              _takeoverIntentTitle != null &&
                              _takeoverIntentDetail != null &&
                              _targetLockPanel != null &&
                              _narrationStateText != null &&
                              _narrationSubtitle != null &&
                              _voiceEnabledToggle != null &&
                              _voiceEnabledKnob != null;
            if (_hierarchyBound)
                BindVoiceEnabledToggle();
            return _hierarchyBound;
        }

        public void BindActions(
            Action closeRequested,
            Action showRecommendationRequested,
            Action executeRecommendationRequested,
            Action stopRequested)
        {
            UnbindActions();
            _closeRequested = closeRequested;
            _showRecommendationRequested = showRecommendationRequested;
            _executeRecommendationRequested = executeRecommendationRequested;
            _stopRequested = stopRequested;

            _headerCloseButton.onClick.AddListener(RequestClose);
            _closeButton.onClick.AddListener(RequestClose);
            _showMeButton.onClick.AddListener(RequestShowRecommendation);
            _doItButton.onClick.AddListener(RequestExecuteRecommendation);
            _stopButton.onClick.AddListener(RequestStop);
            _resumeCommandButton.onClick.AddListener(RequestStop);
            if (_tutorialBriefing != null && _tutorialBriefing.TryBindHierarchy())
            {
                _tutorialBriefing.BindActions(
                    RequestClose,
                    RequestShowRecommendation,
                    RequestExecuteRecommendation);
            }
        }

        public void UnbindActions()
        {
            if (_headerCloseButton != null)
                _headerCloseButton.onClick.RemoveListener(RequestClose);
            if (_closeButton != null)
                _closeButton.onClick.RemoveListener(RequestClose);
            if (_showMeButton != null)
                _showMeButton.onClick.RemoveListener(RequestShowRecommendation);
            if (_doItButton != null)
                _doItButton.onClick.RemoveListener(RequestExecuteRecommendation);
            if (_stopButton != null)
                _stopButton.onClick.RemoveListener(RequestStop);
            if (_resumeCommandButton != null)
                _resumeCommandButton.onClick.RemoveListener(RequestStop);
            if (_tutorialBriefing != null)
                _tutorialBriefing.UnbindActions();

            _closeRequested = null;
            _showRecommendationRequested = null;
            _executeRecommendationRequested = null;
            _stopRequested = null;
        }

        public void Show()
        {
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);
            transform.SetAsLastSibling();
        }

        public void Hide()
        {
            if (gameObject.activeSelf)
                gameObject.SetActive(false);
        }

        public bool ContainsScreenPoint(Vector2 screenPosition)
        {
            if (!IsOpen)
                return false;

            if (_tutorialBriefing != null && _tutorialBriefing.gameObject.activeSelf)
                return _tutorialBriefing.ContainsScreenPoint(screenPosition);
            RectTransform interactionRoot = _takeoverVisible && _assistantTakeoverSurface != null
                ? _assistantTakeoverSurface
                : _commandAssistantPanel != null
                    ? _commandAssistantPanel
                    : _landscapeLayout;
            if (interactionRoot == null)
                return false;

            Canvas canvas = interactionRoot.GetComponentInParent<Canvas>();
            Camera eventCamera = canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : canvas.worldCamera;
            return RectTransformUtility.RectangleContainsScreenPoint(interactionRoot, screenPosition, eventCamera);
        }

        public void ApplyControlState(string stateText)
        {
            _takeoverVisible = string.Equals(
                stateText,
                "ARIA CONTROL",
                StringComparison.OrdinalIgnoreCase);
            SetActive(_commandAssistantPanel != null ? _commandAssistantPanel.gameObject : null, !_takeoverVisible);
            SetActive(_assistantTakeoverSurface != null ? _assistantTakeoverSurface.gameObject : null, _takeoverVisible);
            SetText(_controlStateText, _takeoverVisible ? "ARIA CONTROLLING" : stateText);
        }

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
                _elapsedText.SetText("ELAPSED: {0}:{1:00}:{2:00}", hours, minutes, seconds);
            else
                _elapsedText.SetText("ELAPSED: {0:00}:{1:00}", minutes, seconds);
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

        public void ApplyTutorialInteractionState(
            TacticalCommandMode mode,
            bool worldTargetCompleted)
        {
            if (_tutorialBriefing != null && _tutorialBriefing.gameObject.activeSelf)
                _tutorialBriefing.ApplyInteractionState(mode, worldTargetCompleted);
        }

        public void ApplyTargetLock(UiAssistantTargetLockModel model)
        {
            SetActive(_targetLockPanel, model.Visible);
            if (!model.Visible)
                return;

            SetText(_targetNameText, model.TargetName);
            SetText(_sourceNameText, model.SourceName);
            SetText(_distanceText, model.DistanceText);
            SetText(_healthText, model.HealthText);
            SetText(_factionRelationText, model.FactionRelationText);
            SetText(_readinessText, model.ReadinessText);
            SetText(_targetReasonText, model.ReasonText);

            SetActive(_targetMarker0, model.LockState == 1 || model.LockState == 3);
            SetActive(_targetMarker1, model.LockState == 2 || model.LockState == 4);
            SetActive(_targetMarker2, model.LockState == 5);
        }

        public void ApplyNarration(
            UiAssistantNarrationModel narration,
            string legacySubtitle,
            bool legacySubtitleVisible)
        {
            bool hasStructuredNarration = !string.IsNullOrWhiteSpace(narration.StatusText) ||
                                          !string.IsNullOrWhiteSpace(narration.SubtitleText) ||
                                          !string.IsNullOrWhiteSpace(narration.FailureReasonText);
            string status = hasStructuredNarration ? narration.StatusText : string.Empty;
            string subtitle = hasStructuredNarration ? narration.SubtitleText : legacySubtitle;
            bool subtitleVisible = hasStructuredNarration
                ? !string.IsNullOrWhiteSpace(subtitle)
                : legacySubtitleVisible && !string.IsNullOrWhiteSpace(subtitle);

            SetActive(_narrationStateChip, !string.IsNullOrWhiteSpace(status));
            SetText(_narrationStateText, status);
            SetText(_narrationSubtitle, subtitleVisible ? subtitle : string.Empty);
            SetText(
                _narrationFailureReason,
                hasStructuredNarration ? narration.FailureReasonText : string.Empty);
            SetActive(
                _narrationWaveform,
                hasStructuredNarration && narration.WaveformPulse &&
                (_voiceEnabledToggle == null || _voiceEnabledToggle.isOn));
        }

        private void BindVoiceEnabledToggle()
        {
            _voiceEnabledToggle.onValueChanged.RemoveListener(OnVoiceEnabledChanged);
            bool voiceEnabled = SettingsService.Load().Audio.VoiceEnabled;
            _voiceEnabledToggle.SetIsOnWithoutNotify(voiceEnabled);
            RefreshVoiceEnabledToggle(voiceEnabled);
            _voiceEnabledToggle.onValueChanged.AddListener(OnVoiceEnabledChanged);
        }

        private void OnVoiceEnabledChanged(bool enabled)
        {
            UISettingsModel settings = SettingsService.Load();
            settings.Audio.VoiceEnabled = enabled;
            SettingsService.Save(settings);
            SettingsService.ApplyRuntime(settings);
            RefreshVoiceEnabledToggle(enabled);
            SetActive(_narrationWaveform, enabled);
        }

        private void RefreshVoiceEnabledToggle(bool enabled)
        {
            if (_voiceEnabledKnob == null)
                return;

            Vector2 position = _voiceEnabledKnob.anchoredPosition;
            position.x = enabled ? 38f : 5f;
            _voiceEnabledKnob.anchoredPosition = position;
        }

        private void ApplyMessage(MessageRowBinding[] rows, int index, UiAssistantMessageRowModel model)
        {
            if (!TryGetRow(rows, index, out MessageRowBinding row))
                return;

            bool visible = model.Visible && !model.Acknowledged;
            SetActive(row.Root, visible);
            if (!visible)
                return;

            SetText(row.Body, model.Title);
            SetText(row.Detail, model.Body);
            SetText(row.Priority, $"{PriorityText(model.Priority)} / {AgeStateText(model.AgeState)}");
            SetActive(row.Icon, true);
            SetActive(row.PriorityChip, true);
            SetActive(row.PriorityRail, true);
        }

    }
}
