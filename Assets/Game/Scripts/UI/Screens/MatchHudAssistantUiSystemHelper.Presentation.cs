using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Game.UI.Contracts;
using Game.Tactical.Contracts;

namespace Game.UI.Runtime
{
    internal sealed partial class MatchHudAssistantUiSystemHelper
    {
        private bool _loggedMissingButton;
        private bool _loggedInvalidButton;

        private RectTransform _buttonRoot;
        private Button _button;
        private TMP_Text _accessStateText;
        private TMP_Text _accessCueText;
        private GameObject _objectivePanel;
        private bool _objectivePanelOriginalActive;
        private RectTransform _popupLayer;
        private GameObject _popupPrefab;
        private GameObject _popupInstance;
        private AriaCommandAssistantPopupView _popupView;
        private AriaTutorialBriefingView _embeddedTutorialView;
        private readonly AssistantPanelUiSystemHelper _panelUiSystem = new();
        private readonly AssistantHighlightPresentationSystemHelper _highlightPresentationSystem = new();
        private Action _captureGameplayUiClick;
        private Action _beforePanelOpen;
        private Action<bool> _panelOpenChanged;
        private bool _mirroredPanelOpen;
        private UiAssistantPanelModel _lastPanelModel = UiAssistantPanelModel.Empty;
        private UiAssistantHighlightModel _lastHighlightModel = UiAssistantHighlightModel.Empty;
        private MatchOverlayCommandControlsView _commandControlsView;
        private TacticalCommandMode _activeCommandMode;
        private bool _tutorialWorldTargetCompleted;

        public bool IsPanelOpen => _popupView != null && _popupView.IsOpen;
        public bool IsBound =>
            _buttonRoot != null && _popupView != null && _embeddedTutorialView != null;

        public void Bind(
            GameObject headerContent,
            RectTransform popupLayer,
            GameObject popupPrefab,
            Action captureGameplayUiClick,
            Action beforePanelOpen,
            Action<bool> panelOpenChanged)
        {
            RebindState rebindState = CaptureRebindState(
                headerContent != null && popupLayer != null && popupPrefab != null);
            Unbind();
            RestoreRebindState(in rebindState);
            _highlightPresentationSystem.BindResourceStrip(_boundResourceStrip);
            _captureGameplayUiClick = captureGameplayUiClick;
            _beforePanelOpen = beforePanelOpen;
            _panelOpenChanged = panelOpenChanged;
            _popupLayer = popupLayer;
            _popupPrefab = popupPrefab;

            RectTransform headerRect = headerContent != null ? headerContent.transform as RectTransform : null;
            if (headerRect == null || _popupLayer == null || _popupPrefab == null)
                return;

            _buttonRoot = MatchHudAssistantReferenceUiSystemHelper.ResolveButton(headerContent, out _button);
            if (_buttonRoot == null)
            {
                LogMissingButton();
                return;
            }

            RectTransform objectiveRect = MatchHudAssistantReferenceUiSystemHelper.ResolveObjectivesPanel(
                headerContent,
                _buttonRoot);
            if (objectiveRect != null)
            {
                _objectivePanel = objectiveRect.gameObject;
                _objectivePanelOriginalActive = _objectivePanel.activeSelf;
            }

            if (!EnsurePopupView())
                return;

            if (!TryBindButtonHierarchy())
            {
                LogInvalidButton();
                DestroyPopupInstance();
                _buttonRoot = null;
                return;
            }

            _panelUiSystem.Bind(_popupView, _accessStateText, _accessCueText);
            _panelUiSystem.ApplyReadModel(_lastPanelModel);
            _popupView.Hide();
            MirrorPanelOpen(false, force: true);

            // ARIA augments the authored HUD. Mission objectives remain visible while its controls are bound.
        }

        public void Unbind()
        {
            MirrorPanelOpen(false, force: true);
            if (_button != null)
                _button.onClick.RemoveListener(TogglePanel);

            if (_popupView != null)
                _popupView.UnbindActions();
            if (_embeddedTutorialView != null)
                _embeddedTutorialView.UnbindActions();
            DestroyPopupInstance();
            if (_objectivePanel != null)
                _objectivePanel.SetActive(_objectivePanelOriginalActive);

            _buttonRoot = null;
            _button = null;
            _accessStateText = null;
            _accessCueText = null;
            _embeddedTutorialView = null;
            _objectivePanel = null;
            _objectivePanelOriginalActive = false;
            _popupLayer = null;
            _popupPrefab = null;
            _captureGameplayUiClick = null;
            _beforePanelOpen = null;
            _panelOpenChanged = null;
            _panelUiSystem.Unbind();
            _highlightPresentationSystem.Unbind();
            _lastPanelModel = UiAssistantPanelModel.Empty;
            _lastHighlightModel = UiAssistantHighlightModel.Empty;
            _commandControlsView = null;
            _activeCommandMode = TacticalCommandMode.None;
            _tutorialWorldTargetCompleted = false;
            _executeBuildingPlacementStep = null;
            _boundResourceStrip = null;
            ClearTutorialPresentationState();
        }

        public void ApplyReadModel(UiAssistantPanelModel model)
        {
            byte previousTutorialStep = _lastPanelModel.TutorialStep;
            if (previousTutorialStep != model.TutorialStep)
            {
                _highlightPresentationSystem.ClearUiSurfaceCue();
                ClearPendingM02DoIt();
            }
            _lastPanelModel = model;
            if (previousTutorialStep != model.TutorialStep)
                _tutorialWorldTargetCompleted = false;
            if (_buttonRoot == null)
                return;

            if (_popupView == null && !EnsurePopupView())
                return;
            _panelUiSystem.ApplyReadModel(model);
            _embeddedTutorialView.Apply(model);
            _embeddedTutorialView.ApplyInteractionState(
                _activeCommandMode,
                _tutorialWorldTargetCompleted);
            if (model.TutorialStep == 0 || !model.HasRecommendation)
                _embeddedTutorialView.SetPresentationVisible(false);
            QueueTutorialPresentation(model, previousTutorialStep);
        }

        public void ApplyHighlightReadModel(UiAssistantHighlightModel model)
        {
            _lastHighlightModel = model;
            if (_buttonRoot == null)
                return;

            if (_popupView == null && !EnsurePopupView())
                return;
            _highlightPresentationSystem.ApplyReadModel(model);
        }

        public void ResetForMissionAttempt()
        {
            _lastHighlightModel = UiAssistantHighlightModel.Empty;
            _activeCommandMode = TacticalCommandMode.None;
            _tutorialWorldTargetCompleted = false;
            _highlightPresentationSystem.ResetForMissionAttempt();
            ClearTutorialPresentationState();
            ClosePanelWithoutInputCapture();
        }

        public void SuspendForCinematic()
        {
            _tutorialCinematicSuspended = true;
            _tutorialShowAtUnscaledTime = -1f;
            HideEmbeddedTutorial();
            ClosePanelWithoutInputCapture();
        }

        public void BindSquadTray(MatchHudSquadTrayView squadTrayView)
        {
            _highlightPresentationSystem.BindSquadTray(squadTrayView);
        }

        public void BindCommandControls(MatchOverlayCommandControlsView commandControlsView)
        {
            _commandControlsView = commandControlsView;
            _highlightPresentationSystem.BindCommandControls(commandControlsView);
        }

        public void BindBuildButton(Button buildButton) =>
            _highlightPresentationSystem.BindBuildButton(buildButton);

        public void BindBuildDrawer(BuildDrawerView buildDrawerView) =>
            _highlightPresentationSystem.BindBuildDrawer(buildDrawerView);

        public void BindWorldCamera(Camera worldCamera)
        {
            _highlightPresentationSystem.BindWorldCamera(worldCamera);
        }

        public void ApplyCommandMode(TacticalCommandMode mode)
        {
            _activeCommandMode = mode;
            _highlightPresentationSystem.ApplyCommandMode(mode);
            _popupView?.ApplyTutorialInteractionState(
                mode,
                _tutorialWorldTargetCompleted);
            _embeddedTutorialView?.ApplyInteractionState(
                mode,
                _tutorialWorldTargetCompleted);
        }

        public void AcknowledgeCommandMode(TacticalCommandMode mode)
        {
            _activeCommandMode = mode;
            _highlightPresentationSystem.AcknowledgeCommandMode(mode);
            _popupView?.ApplyTutorialInteractionState(
                mode,
                _tutorialWorldTargetCompleted);
            _embeddedTutorialView?.ApplyInteractionState(
                mode,
                _tutorialWorldTargetCompleted);
        }

        public void CompleteWorldTarget(TacticalCommandMode mode)
        {
            _highlightPresentationSystem.CompleteWorldTarget(mode);
            bool completesTutorialTarget =
                (_lastPanelModel.TutorialStep == 2 && mode == TacticalCommandMode.Move) ||
                (_lastPanelModel.TutorialStep is 3 or 4 && mode == TacticalCommandMode.Attack);
            if (!completesTutorialTarget)
                return;

            _tutorialWorldTargetCompleted = true;
            CompleteTutorialStep(
                _lastPanelModel.TutorialStep,
                finalStep: mode == TacticalCommandMode.Attack);
        }

        public void TickHighlight(float unscaledTime)
        {
            _highlightPresentationSystem.Tick();
            TickPendingM02DoIt(unscaledTime);
            TickTutorialPresentation(unscaledTime);
        }

        public bool TryClosePanel()
        {
            if (!IsPanelOpen)
                return false;

            ClosePanel();
            return true;
        }

        public void ClosePanelWithoutInputCapture()
        {
            SetPanelOpen(false);
        }

        public bool ContainsScreenPoint(Vector2 screenPosition)
        {
            Camera eventCamera = ResolveEventCamera(_buttonRoot);
            return ContainsRect(_buttonRoot, screenPosition, eventCamera) ||
                   (_popupView != null && _popupView.ContainsScreenPoint(screenPosition));
        }

        private bool EnsurePopupView()
        {
            if (_popupView != null)
                return true;
            if (_popupLayer == null || _popupPrefab == null)
                return false;

            _popupInstance = UnityEngine.Object.Instantiate(_popupPrefab, _popupLayer, false);
            _popupInstance.name = _popupPrefab.name;
            _popupView = _popupInstance.GetComponent<AriaCommandAssistantPopupView>();
            if (_popupView == null || !_popupView.TryBindHierarchy())
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogError("[ARIA] POP13 popup is missing AriaCommandAssistantPopupView or its locked LandscapeLayout hierarchy.");
#endif
                DestroyPopupInstance();
                return false;
            }

            RectTransform layout = _popupView.LandscapeLayout;
            MainMenuV3SectionLayoutView responsiveLayout =
                layout.GetComponent<MainMenuV3SectionLayoutView>();
            if (responsiveLayout != null)
            {
                responsiveLayout.RefreshLayout();
            }
            else
            {
                layout.anchorMin = new Vector2(0.5f, 0.5f);
                layout.anchorMax = new Vector2(0.5f, 0.5f);
                layout.pivot = new Vector2(0.5f, 0.5f);
                layout.anchoredPosition = new Vector2(0f, 156f);
                layout.sizeDelta = new Vector2(2460f, 1510f);
            }
            _popupView.BindActions(
                ClosePanel,
                ShowRecommendation,
                ExecuteRecommendation,
                StopAssistantControl);
            _panelUiSystem.Bind(_popupView, _accessStateText, _accessCueText);
            _highlightPresentationSystem.Bind(
                _popupView.PreviewHighlight,
                HandleGuidedCommandModeAcknowledged,
                HandleSquadSelectionAcknowledged,
                HandleUiSurfaceAcknowledged);
            _panelUiSystem.ApplyReadModel(_lastPanelModel);
            _highlightPresentationSystem.ApplyReadModel(_lastHighlightModel);
            _popupView.Hide();
            return true;
        }

        private bool TryBindButtonHierarchy()
        {
            if (_buttonRoot == null)
                return false;

            if (_button == null ||
                !MatchHudAssistantReferenceUiSystemHelper.TryResolveButtonText(
                    _buttonRoot,
                    out _accessStateText,
                    out _accessCueText))
                return false;

            _embeddedTutorialView = _buttonRoot.GetComponent<AriaTutorialBriefingView>();
            if (_embeddedTutorialView == null || !_embeddedTutorialView.TryBindHierarchy())
                return false;

            MatchHudCanvasBatchingUtility.EnsureLocalCanvas(_buttonRoot.gameObject, needsRaycaster: true);
            _button.onClick.RemoveListener(TogglePanel);
            _button.onClick.AddListener(TogglePanel);
            _embeddedTutorialView.BindActions(
                null,
                ShowRecommendation,
                ExecuteRecommendation);
            _embeddedTutorialView.SetPresentationVisible(false);
            return true;
        }

        private void TogglePanel()
        {
            CaptureUiOnly();
            if (_lastPanelModel.TutorialStep > 0)
            {
                ShowEmbeddedTutorial();
                return;
            }
            SetPanelOpen(!IsPanelOpen);
        }

        private void ClosePanel()
        {
            CaptureUiOnly();
            SetPanelOpen(false);
        }

        private void ShowRecommendation()
        {
            CaptureUiOnly();
            TryShowRecommendation();
        }

        private bool TryShowRecommendation(bool preferPanelRecommendation = false)
        {
            if (!UiShellRuntimeGateway.TryEnqueueAssistantCommandIntent(UiAssistantCommandIntentKind.ShowRecommendation))
                return false;

            // The structured panel projection can trail the highlight projection by one
            // simulation update after the player completes a tutorial action. Prefer the
            // active highlight so the next explicit Show Me teaches the current step rather
            // than replaying the previous panel recommendation.
            bool useHighlightRecommendation =
                !preferPanelRecommendation &&
                _lastHighlightModel.Active &&
                (_lastPanelModel.RecommendationKind == 0 ||
                 _lastHighlightModel.RecommendationKind == _lastPanelModel.RecommendationKind);
            byte recommendationKind = useHighlightRecommendation
                ? _lastHighlightModel.RecommendationKind
                : _lastPanelModel.RecommendationKind;
            byte targetKind = useHighlightRecommendation
                ? _lastHighlightModel.TargetKind
                : _lastPanelModel.RecommendationTargetKind;
            _highlightPresentationSystem.BeginPendingShowMe(recommendationKind, targetKind);
            if (_lastPanelModel.TutorialStep == 0)
                SetPanelOpen(false);
            return true;
        }

        private void ExecuteRecommendation()
        {
            CaptureUiOnly();
            if (IsM02DoItStep(in _lastPanelModel))
            {
                if (TryExecuteM02DoIt(in _lastPanelModel))
                {
                    ClearPendingM02DoIt();
                    ClosePanelWithoutInputCapture();
                }
                else
                {
                    QueueM02DoItRetry(_lastPanelModel.TutorialStep, Time.unscaledTime);
                }
                return;
            }
            if (TryAdvanceTutorialCommandSubstep())
                return;

            if (!UiShellRuntimeGateway.TryEnqueueAssistantCommandIntent(
                    UiAssistantCommandIntentKind.ExecuteRecommendation,
                    fromTakeover: true))
                return;

            if (_lastPanelModel.TutorialStep == 0)
                SetPanelOpen(false);
            else
                CompleteTutorialStep(
                    _lastPanelModel.TutorialStep,
                    finalStep: _lastPanelModel.RecommendationKind == 3);
        }

        private bool TryAdvanceTutorialCommandSubstep()
        {
            if (_lastPanelModel.TutorialStep == 0)
                return false;

            TacticalCommandMode requiredMode = _lastPanelModel.RecommendationKind switch
            {
                2 => TacticalCommandMode.Move,
                3 => TacticalCommandMode.Attack,
                _ => TacticalCommandMode.None
            };
            if (requiredMode == TacticalCommandMode.None || _activeCommandMode == requiredMode)
                return false;

            Button commandButton = requiredMode == TacticalCommandMode.Move
                ? _commandControlsView?.MoveButton
                : _commandControlsView?.AttackButton;
            if (commandButton != null && commandButton.IsActive() && commandButton.IsInteractable())
                commandButton.onClick.Invoke();

            // Tutorial automation must never bypass the command-button instruction.
            return true;
        }

        private void StopAssistantControl()
        {
            CaptureUiOnly();
            UiShellRuntimeGateway.TryEnqueueAssistantCommandIntent(UiAssistantCommandIntentKind.StopAssistantControl);
        }

        private void SetPanelOpen(bool open)
        {
            if (open)
            {
                _beforePanelOpen?.Invoke();
                if (!EnsurePopupView())
                    return;
                _popupView.Show();
            }
            else
            {
                if (_popupView != null)
                    _popupView.Hide();
            }

            MirrorPanelOpen(open);
        }

        private void ShowEmbeddedTutorial()
        {
            if (_embeddedTutorialView == null || _lastPanelModel.TutorialStep == 0)
                return;

            if (IsPanelOpen)
                SetPanelOpen(false);
            _embeddedTutorialView.Apply(_lastPanelModel);
            _embeddedTutorialView.ApplyInteractionState(
                _activeCommandMode,
                _tutorialWorldTargetCompleted);
            if (_accessCueText != null)
                _accessCueText.gameObject.SetActive(false);
            _embeddedTutorialView.SetPresentationVisible(true);
        }

        private void HideEmbeddedTutorial()
        {
            _embeddedTutorialView?.SetPresentationVisible(false);
            if (_accessStateText != null)
            {
                string ownership = string.IsNullOrWhiteSpace(_lastPanelModel.OwnershipText)
                    ? "PLAYER CONTROL"
                    : _lastPanelModel.OwnershipText;
                _accessStateText.text = ownership;
                _accessStateText.gameObject.SetActive(true);
            }
            if (_accessCueText != null)
                _accessCueText.gameObject.SetActive(!string.IsNullOrWhiteSpace(_accessCueText.text));
        }

        private void HandleGuidedCommandModeAcknowledged(TacticalCommandMode mode)
        {
            _activeCommandMode = mode;
            _popupView?.ApplyTutorialInteractionState(
                mode,
                _tutorialWorldTargetCompleted);
            _embeddedTutorialView?.ApplyInteractionState(
                mode,
                _tutorialWorldTargetCompleted);
            if ((_lastPanelModel.TutorialStep == 2 && mode == TacticalCommandMode.Move) ||
                (_lastPanelModel.TutorialStep is 3 or 4 && mode == TacticalCommandMode.Attack))
            {
                ScheduleTutorialSubstep(_lastPanelModel.TutorialStep, Time.unscaledTime);
            }
        }

        private void HandleUiSurfaceAcknowledged(byte recommendationKind)
        {
            byte step = recommendationKind switch
            {
                4 => 2,
                1 => 3,
                5 => 6,
                9 => 5,
                _ => (byte)0
            };
            if (step == 0)
                return;

            UiCampaignGuidanceTargetKind target =
                ResolveM02AcknowledgementTarget(recommendationKind);
            if (target != UiCampaignGuidanceTargetKind.None)
                UiShellRuntimeGateway.TryAcknowledgeCampaignGuidanceTarget(
                    target);
            CompleteTutorialStep(step, finalStep: false);
        }

    }
}
