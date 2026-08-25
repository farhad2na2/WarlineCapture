using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Game.Tactical.Contracts;
using Game.UI.Contracts;
using RTLTMPro;

namespace Game.UI.Runtime
{
    [DisallowMultipleComponent]
    public sealed class AriaTutorialBriefingView : MonoBehaviour
    {
        [SerializeField] private RectTransform briefingLayout;
        [SerializeField] private Image portraitImage;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button showMeButton;
        [SerializeField] private Button doItButton;
        [SerializeField] private TMP_Text showMeButtonLabel;
        [SerializeField] private TMP_Text doItButtonLabel;
        [SerializeField] private TMP_FontAsset persianFont;

        private Action _closeRequested;
        private Action _showRecommendationRequested;
        private Action _executeRecommendationRequested;
        private byte _tutorialStep;
        private byte _tutorialStepCount;
        private byte _recommendationKind;
        private string _defaultTitle = string.Empty;
        private string _defaultBody = string.Empty;
        private string _currentInstructionBody = string.Empty;
        private UiTutorialNarrationPhase _currentNarrationPhase;
        private bool _rightToLeft;
        private TMP_Text[] _localizedTextTargets;
        private TMP_FontAsset[] _defaultFonts;
        private TextAlignmentOptions[] _defaultAlignments;
        private readonly FastStringBuilder _rtlBuffer = new(RTLSupport.DefaultBufferSize);

        public RectTransform BriefingLayout => briefingLayout;
        public Image PortraitImage => portraitImage;
        public TMP_Text TitleText => titleText;
        public TMP_Text BodyText => bodyText;
        public TMP_Text ProgressText => progressText;
        public Button CloseButton => closeButton;
        public Button ShowMeButton => showMeButton;
        public Button DoItButton => doItButton;
        public string CurrentInstructionBody => _currentInstructionBody;
        public UiTutorialNarrationPhase CurrentNarrationPhase => _currentNarrationPhase;

        public bool TryBindHierarchy()
        {
            return briefingLayout != null && portraitImage != null && titleText != null &&
                   bodyText != null && progressText != null && closeButton != null &&
                   showMeButton != null && doItButton != null &&
                   showMeButtonLabel != null && doItButtonLabel != null;
        }

        public void BindActions(
            Action closeRequested,
            Action showRecommendationRequested,
            Action executeRecommendationRequested)
        {
            UnbindActions();
            _closeRequested = closeRequested;
            _showRecommendationRequested = showRecommendationRequested;
            _executeRecommendationRequested = executeRecommendationRequested;
            closeButton.onClick.AddListener(RequestClose);
            showMeButton.onClick.AddListener(RequestShowRecommendation);
            doItButton.onClick.AddListener(RequestExecuteRecommendation);
        }

        public void UnbindActions()
        {
            if (closeButton != null)
                closeButton.onClick.RemoveListener(RequestClose);
            if (showMeButton != null)
                showMeButton.onClick.RemoveListener(RequestShowRecommendation);
            if (doItButton != null)
                doItButton.onClick.RemoveListener(RequestExecuteRecommendation);

            _closeRequested = null;
            _showRecommendationRequested = null;
            _executeRecommendationRequested = null;
        }

        public void Apply(UiAssistantPanelModel model)
        {
            _tutorialStep = model.TutorialStep;
            _tutorialStepCount = model.TutorialStepCount;
            _recommendationKind = model.RecommendationKind;
            _rightToLeft = model.TutorialRightToLeft;
            ApplyLanguagePresentation();
            _defaultTitle = _rightToLeft
                ? model.RecommendationTitle ?? string.Empty
                : (model.RecommendationTitle ?? string.Empty).ToUpperInvariant();
            _defaultBody = model.RecommendationBody ?? string.Empty;
            ApplyInteractionState(TacticalCommandMode.None, worldTargetCompleted: false);
            showMeButton.interactable = model.CanShow;
            doItButton.interactable = model.CanExecute;
            SetLocalizedText(showMeButtonLabel, _rightToLeft ? "نشانم بده" : "SHOW ME");
            SetLocalizedText(doItButtonLabel, _rightToLeft ? "انجامش بده" : "DO IT");
            closeButton.gameObject.SetActive(false);
        }

        public void ApplyInteractionState(
            TacticalCommandMode mode,
            bool worldTargetCompleted)
        {
            if (_tutorialStep == 2 && _recommendationKind == 2)
            {
                if (worldTargetCompleted)
                {
                    ApplyInstruction(
                        _rightToLeft ? "در حال حرکت به پوشش" : "MOVING TO COVER",
                        _rightToLeft
                            ? "گروه شما در حال حرکت به موقعیت پوشش علامت‌گذاری‌شده است."
                            : "Your squad is moving to the marked cover position.",
                        UiTutorialNarrationPhase.WorldTarget);
                    return;
                }

                bool choosingDestination = mode == TacticalCommandMode.Move;
                ApplyInstruction(
                    _rightToLeft
                        ? choosingDestination ? "مقصد را انتخاب کنید" : "حرکت را بزنید"
                        : choosingDestination ? "CHOOSE DESTINATION" : "PRESS MOVE",
                    choosingDestination
                        ? _rightToLeft
                            ? "برای حرکت گروه، روی مقصد علامت‌گذاری‌شده بزنید."
                            : "Tap the highlighted destination to move your squad."
                        : _rightToLeft
                            ? "برای انتخاب دستور حرکت، روی «حرکت» بزنید."
                            : "Tap MOVE to select the move command.",
                    choosingDestination
                        ? UiTutorialNarrationPhase.WorldTarget
                        : UiTutorialNarrationPhase.PrimaryAction);
                return;
            }

            if (_tutorialStep is 3 or 4 && _recommendationKind == 3)
            {
                if (worldTargetCompleted)
                {
                    ApplyInstruction(
                        _rightToLeft ? "دستور حمله صادر شد" : "ATTACK ORDER ISSUED",
                        _rightToLeft
                            ? "گروه شما در حال درگیری با دشمن علامت‌گذاری‌شده است."
                            : "Your squad is engaging the highlighted enemy.",
                        UiTutorialNarrationPhase.WorldTarget);
                    return;
                }

                bool choosingEnemy = mode == TacticalCommandMode.Attack;
                ApplyInstruction(
                    _rightToLeft
                        ? choosingEnemy ? "دشمن را انتخاب کنید" : "حمله را بزنید"
                        : choosingEnemy ? "CHOOSE ENEMY" : "PRESS ATTACK",
                    choosingEnemy
                        ? _rightToLeft
                            ? "برای صدور دستور حمله، روی دشمن علامت‌گذاری‌شده بزنید."
                            : "Tap the highlighted enemy to issue the attack."
                        : _rightToLeft
                            ? "برای انتخاب دستور حمله، روی «حمله» بزنید."
                            : "Tap ATTACK to select the attack command.",
                    choosingEnemy
                        ? UiTutorialNarrationPhase.WorldTarget
                        : UiTutorialNarrationPhase.PrimaryAction);
                return;
            }

            ApplyInstruction(
                _defaultTitle,
                _defaultBody,
                UiTutorialNarrationPhase.PrimaryAction);
        }

        public void ApplyAccessibility(bool largeTextEnabled, bool highContrastEnabled)
        {
            float scale = largeTextEnabled ? 1.08f : 1f;
            titleText.fontSize = 58f * scale;
            bodyText.fontSize = 38f * scale;
            progressText.fontSize = 30f * scale;
            Color primary = highContrastEnabled ? Color.white : new Color(0.95f, 0.92f, 0.82f, 1f);
            titleText.color = primary;
            bodyText.color = primary;
        }

        public bool ContainsScreenPoint(Vector2 screenPosition)
        {
            if (!gameObject.activeInHierarchy || briefingLayout == null)
                return false;

            Canvas canvas = briefingLayout.GetComponentInParent<Canvas>();
            Camera eventCamera = canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : canvas.worldCamera;
            return RectTransformUtility.RectangleContainsScreenPoint(briefingLayout, screenPosition, eventCamera);
        }

        private void OnDestroy()
        {
            UnbindActions();
        }

        private void RequestClose() => _closeRequested?.Invoke();
        private void RequestShowRecommendation() => _showRecommendationRequested?.Invoke();
        private void RequestExecuteRecommendation() => _executeRecommendationRequested?.Invoke();

        private void ApplyInstruction(
            string title,
            string body,
            UiTutorialNarrationPhase narrationPhase)
        {
            _currentInstructionBody = body ?? string.Empty;
            _currentNarrationPhase = narrationPhase;
            SetLocalizedText(titleText, title);
            SetLocalizedText(bodyText, _currentInstructionBody);
            ApplyProgress(narrationPhase);
        }

        private void ApplyProgress(UiTutorialNarrationPhase narrationPhase)
        {
            int step = Mathf.Max(1, _tutorialStep);
            if (_tutorialStep == 2 && narrationPhase == UiTutorialNarrationPhase.WorldTarget)
                step = 3;
            else if (_tutorialStep is 3 or 4)
                step = narrationPhase == UiTutorialNarrationPhase.WorldTarget ? 5 : 4;

            int count = Mathf.Max(step, _tutorialStepCount);
            SetLocalizedText(
                progressText,
                _rightToLeft ? $"آموزش {step} / {count}" : $"TRAINING {step} / {count}");
        }

        private void ApplyLanguagePresentation()
        {
            if (_localizedTextTargets == null)
            {
                _localizedTextTargets = new[]
                {
                    titleText, bodyText, progressText, showMeButtonLabel, doItButtonLabel
                };
                _defaultFonts = new TMP_FontAsset[_localizedTextTargets.Length];
                _defaultAlignments = new TextAlignmentOptions[_localizedTextTargets.Length];
                for (int i = 0; i < _localizedTextTargets.Length; i++)
                {
                    _defaultFonts[i] = _localizedTextTargets[i].font;
                    _defaultAlignments[i] = _localizedTextTargets[i].alignment;
                }
            }

            for (int i = 0; i < _localizedTextTargets.Length; i++)
            {
                TMP_Text target = _localizedTextTargets[i];
                target.font = _rightToLeft && persianFont != null
                    ? persianFont
                    : _defaultFonts[i];
                target.alignment = _rightToLeft
                    ? ToRightAligned(_defaultAlignments[i])
                    : _defaultAlignments[i];
            }
        }

        private void SetLocalizedText(TMP_Text target, string value)
        {
            string display = value ?? string.Empty;
            if (_rightToLeft && display.Length > 0)
            {
                _rtlBuffer.Clear();
                RTLSupport.FixRTL(
                    display,
                    _rtlBuffer,
                    farsi: true,
                    fixTextTags: true,
                    preserveNumbers: true);
                _rtlBuffer.Reverse();
                display = _rtlBuffer.ToString();
            }

            target.isRightToLeftText = _rightToLeft;
            if (target.text != display)
                target.text = display;
        }

        private static TextAlignmentOptions ToRightAligned(TextAlignmentOptions alignment)
        {
            return alignment switch
            {
                TextAlignmentOptions.Left => TextAlignmentOptions.Right,
                TextAlignmentOptions.TopLeft => TextAlignmentOptions.TopRight,
                TextAlignmentOptions.BottomLeft => TextAlignmentOptions.BottomRight,
                _ => alignment
            };
        }
    }
}
