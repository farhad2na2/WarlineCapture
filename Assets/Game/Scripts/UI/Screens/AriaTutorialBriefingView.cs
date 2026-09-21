using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Game.Tactical.Contracts;
using Game.UI.Contracts;
using RTLTMPro;

namespace Game.UI.Runtime
{
    [DisallowMultipleComponent]
    public sealed partial class AriaTutorialBriefingView : MonoBehaviour
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
        [SerializeField] private RectTransform firstStepGuideRoot;
        [SerializeField] private TMP_FontAsset persianFont;
        [SerializeField] private RectTransform missionPortraitClip;
        [SerializeField] private RectTransform missionPortraitStage;
        [SerializeField] private GameObject missionTelemetry;
        private bool _missionLayoutActive, _missionLayoutLarge;

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

        public RectTransform BriefingLayout => briefingLayout;
        public Image PortraitImage => portraitImage;
        public TMP_Text TitleText => titleText;
        public TMP_Text BodyText => bodyText;
        public TMP_Text ProgressText => progressText;
        public Button CloseButton => closeButton;
        public Button ShowMeButton => showMeButton;
        public Button DoItButton => doItButton;
        public RectTransform FirstStepGuideRoot => firstStepGuideRoot;
        public bool OwnsLocalizedText(TMP_Text text) => text!=null &&
            (text==titleText || text==bodyText || text==progressText || text==showMeButtonLabel || text==doItButtonLabel);
        public string CurrentInstructionBody => _currentInstructionBody;
        public UiTutorialNarrationPhase CurrentNarrationPhase => _currentNarrationPhase;
        public bool IsPresentationVisible =>
            briefingLayout != null && briefingLayout.gameObject.activeSelf;

        private void Awake() => PrepareActionButtons();

        private void PrepareActionButtons()
        {
            // Create before the cinematic lock snapshots HUD controls. Cloning a locked
            // button later inherits its CanvasGroup but not its saved unlock state.
            _ = ContinueButton;
            EnsureSelectionButton();
        }

        public bool TryBindHierarchy()
        {
            return briefingLayout != null && portraitImage != null && titleText != null &&
                   bodyText != null && progressText != null &&
                   showMeButton != null && doItButton != null &&
                   showMeButtonLabel != null && doItButtonLabel != null;
        }

        public void BindActions(
            Action closeRequested,
            Action showRecommendationRequested,
            Action executeRecommendationRequested)
        {
            UnbindActions();
            PrepareActionButtons();
            continueConsumed = false;
            _closeRequested = closeRequested;
            _showRecommendationRequested = showRecommendationRequested;
            _executeRecommendationRequested = executeRecommendationRequested;
            if (closeButton != null)
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

        private bool usesExpandedBriefingLayout;

        public void Apply(UiAssistantPanelModel model)
        {
            if (_tutorialStep != model.TutorialStep || _tutorialStepCount != model.TutorialStepCount ||
                _defaultBody != (model.RecommendationBody ?? string.Empty)) continueConsumed = false;
            _tutorialStep = model.TutorialStep;
            _tutorialStepCount = model.TutorialStepCount;
            _defenseTutorial = _tutorialStepCount==12 && UiShellRuntimeGateway.TryReadMissionDefense(out _);
            usesExpandedBriefingLayout = _tutorialStepCount is 8 or 9 or 10 or 12 || UiShellRuntimeGateway.TryReadSkirmish(out _);
            ApplyMissionLayout(usesExpandedBriefingLayout,model.LargeTextEnabled);
            _recommendationKind = model.RecommendationKind;
            _rightToLeft = UiShellRuntimeGateway.Localization.IsRightToLeft;
            ApplyLanguagePresentation();
            _defaultTitle = _rightToLeft
                ? model.RecommendationTitle ?? string.Empty
                : (model.RecommendationTitle ?? string.Empty).ToUpperInvariant();
            _defaultBody = SkirmishWatchInstruction(model.RecommendationBody ?? string.Empty);
            ApplyInteractionState(TacticalCommandMode.None, worldTargetCompleted: false);
            showMeButton.interactable = model.CanShow;
            showMeButton.gameObject.SetActive(model.CanShow);
            SetContinueAvailable(model.CanExecute && IsContinueLabel(model.RecommendationActionLabel));
            SetLocalizedText(showMeButtonLabel, "SHOW ME");
            SetLocalizedText(doItButtonLabel, _tutorialStepCount is 8 or 10 or 12 ? model.RecommendationActionLabel : "DO IT");
            if (closeButton != null)
                closeButton.gameObject.SetActive(true);
            if (firstStepGuideRoot != null)
                firstStepGuideRoot.gameObject.SetActive(_tutorialStep == 1 && _tutorialStepCount is not (8 or 10 or 12));
        }

        public void SetPresentationVisible(bool visible)
        {
            if (briefingLayout != null && briefingLayout.gameObject.activeSelf != visible)
                briefingLayout.gameObject.SetActive(visible);
            RefreshContentLayout();
        }

        public void ApplyInteractionState(
            TacticalCommandMode mode,
            bool worldTargetCompleted)
        {
            if (_tutorialStepCount is not (8 or 10 or 12) && _tutorialStep == 2 && _recommendationKind == 2)
            {
                if (worldTargetCompleted)
                {
                    ApplyInstruction(
                        "MOVING TO COVER",
                        "Your squad is moving to the marked cover position.",
                        UiTutorialNarrationPhase.WorldTarget);
                    return;
                }

                bool choosingDestination = mode == TacticalCommandMode.Move;
                ApplyInstruction(
                    choosingDestination ? "CHOOSE DESTINATION" : "PRESS MOVE",
                    choosingDestination
                        ? "Tap the highlighted destination to move your squad."
                        : "Tap MOVE to select the move command.",
                    choosingDestination
                        ? UiTutorialNarrationPhase.WorldTarget
                        : UiTutorialNarrationPhase.PrimaryAction);
                return;
            }

            if (_tutorialStepCount is not (8 or 10 or 12) && _tutorialStep is 3 or 4 && _recommendationKind == 3)
            {
                if (worldTargetCompleted)
                {
                    ApplyInstruction(
                        "ATTACK ORDER ISSUED",
                        "Your squad is engaging the highlighted enemy.",
                        UiTutorialNarrationPhase.WorldTarget);
                    return;
                }

                bool choosingEnemy = mode == TacticalCommandMode.Attack;
                ApplyInstruction(
                    choosingEnemy ? "CHOOSE ENEMY" : "PRESS ATTACK",
                    choosingEnemy
                        ? "Tap the highlighted enemy to issue the attack."
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
            ApplyMissionLayout(usesExpandedBriefingLayout,largeTextEnabled);
            float scale = largeTextEnabled ? 1.08f : 1f;
            titleText.fontSize = 29f * scale;
            bodyText.fontSize = 21f * scale;
            progressText.fontSize = 17f * scale;
            if(usesExpandedBriefingLayout)
            {
                bodyText.textWrappingMode = TextWrappingModes.Normal;
                titleText.fontSizeMin=largeTextEnabled ? 19 : 17; titleText.fontSizeMax=largeTextEnabled ? 22 : 20;
                bodyText.fontSizeMin=largeTextEnabled ? 17 : 15; bodyText.fontSizeMax=largeTextEnabled ? 19 : 17;
            }
            Color primary = highContrastEnabled ? Color.white : new Color(0.95f, 0.92f, 0.82f, 1f);
            titleText.color = primary;
            bodyText.color = primary;
            _layoutDirty=true; RefreshContentLayout();
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

        private void OnEnable()
        {
            Canvas.willRenderCanvases += RefreshContentLayout;
            if (!TryBindHierarchy()) return;
            _rightToLeft = UiShellRuntimeGateway.Localization.IsRightToLeft;
            ApplyLanguagePresentation();
            // This shared header can render before the first tutorial read model arrives.
            foreach (var target in _localizedTextTargets)
                SetLocalizedText(target, target is RTLTextMeshPro rtl ? rtl.OriginalText : target.text);
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
            _currentInstructionBody = UiLocalizedText.CatalogLabel(body);
            _currentNarrationPhase = narrationPhase;
            SetLocalizedText(titleText, title);
            SetLocalizedText(bodyText, _currentInstructionBody);
            ApplyProgress(narrationPhase);
            RefreshContentLayout();
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
                target.font = UiShellRuntimeGateway.Localization.CurrentFontAsset as TMP_FontAsset
                    ?? (_rightToLeft && persianFont != null ? persianFont : _defaultFonts[i]);
                target.alignment = _rightToLeft
                    ? ToRightAligned(_defaultAlignments[i])
                    : _defaultAlignments[i];
            }
        }

        private void SetLocalizedText(TMP_Text target, string value)
        {
            string display = UiLocalizedText.CatalogLabel(value);
            bool hasArabic = false;
            foreach (char c in display)
                if (c is >= '\u0600' and <= '\u06ff') { hasArabic = true; break; }
            bool rtl = _rightToLeft && hasArabic;
            if (target is RTLTextMeshPro rtlTarget)
            {
                rtlTarget.Farsi = rtl;
                rtlTarget.ForceFix = rtl;
                rtlTarget.PreserveNumbers = !display.Any(c => c is >= '\u06f0' and <= '\u06f9');
                rtlTarget.text = display;
                return;
            }
            if (rtl) display = V3LocalizedTextBindingView.ShapeForRendering(display);
            target.isRightToLeftText = rtl;
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
