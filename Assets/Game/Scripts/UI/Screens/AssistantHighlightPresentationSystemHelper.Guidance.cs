using System;
using Game.UI.Contracts;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Game.Tactical.Contracts;

namespace Game.UI.Runtime
{
    internal sealed partial class AssistantHighlightPresentationSystemHelper
    {
        private const string WorldRingName = "AriaAssistantPreviewHighlightRuntime";
        private const int WorldRingSegments = 48;
        private const float WorldRingRadius = 4f;
        private const float WorldRingHeightOffset = 0.38f;
        private const float WorldRingWidth = 0.38f;
        private const byte SelectRecommendationKind = 1;
        private const byte MoveRecommendationKind = 2;
        private const byte AttackRecommendationKind = 3;
        private const byte WorldPositionTargetKind = 1;

        private Image _panelPulse;
        private GameObject _worldRingRoot;
        private LineRenderer _worldRingRenderer;
        private Material _worldRingMaterial;
        private MatchHudSquadTrayView _squadTrayView;
        private MatchOverlayCommandControlsView _commandControlsView;
        private RectTransform _screenTargetIndicator;
        private TextMeshProUGUI _screenTargetLabel;
        private Canvas _screenTargetCanvas;
        private Camera _worldCamera;
        private Vector3 _screenTargetWorld;
        private bool _screenTargetActive;
        private bool _commandCueActive;
        private bool _commandGuidanceArmed;
        private bool _awaitingNextShowMe;
        private bool _worldTargetShowRequested;
        private bool _pendingFirstShowMe;
        private bool _selectSquadCompleted;
        private Button _squadGuidanceButton;
        private Button _moveGuidanceButton;
        private Button _attackGuidanceButton;
        private TacticalCommandMode _activeCommandMode;
        private TacticalCommandMode _awaitingWorldTargetMode;
        private Action<TacticalCommandMode> _commandModeAcknowledged;
        private Action _squadSelectionAcknowledged;
        private readonly Vector3[] _commandButtonCorners = new Vector3[4];
        private uint _lastVersion = uint.MaxValue;

        public UiAssistantHighlightModel LastAppliedModel { get; private set; } = UiAssistantHighlightModel.Empty;

        public void Bind(
            Image panelPulse,
            Action<TacticalCommandMode> commandModeAcknowledged = null,
            Action squadSelectionAcknowledged = null)
        {
            _panelPulse = panelPulse;
            _commandModeAcknowledged = commandModeAcknowledged;
            _squadSelectionAcknowledged = squadSelectionAcknowledged;
            if (_panelPulse != null)
                _panelPulse.raycastTarget = false;
            _lastVersion = uint.MaxValue;
            LastAppliedModel = UiAssistantHighlightModel.Empty;
            EnsureScreenTargetIndicator();
            ApplyVisual(UiAssistantHighlightModel.Empty);
        }

        public void Unbind()
        {
            DetachSquadGuidanceButton();
            DetachCommandGuidanceButtons();
            _squadTrayView?.ClearAssistantGuidance();
            _panelPulse = null;
            _squadTrayView = null;
            _commandControlsView = null;
            DestroyObject(_worldRingRoot);
            DestroyObject(_worldRingMaterial);
            DestroyObject(_screenTargetIndicator != null ? _screenTargetIndicator.gameObject : null);
            _worldRingRoot = null;
            _worldRingRenderer = null;
            _worldRingMaterial = null;
            _screenTargetIndicator = null;
            _screenTargetLabel = null;
            _screenTargetCanvas = null;
            _worldCamera = null;
            _screenTargetActive = false;
            _commandCueActive = false;
            _commandGuidanceArmed = false;
            _awaitingNextShowMe = false;
            _worldTargetShowRequested = false;
            _pendingFirstShowMe = false;
            _selectSquadCompleted = false;
            _activeCommandMode = TacticalCommandMode.None;
            _awaitingWorldTargetMode = TacticalCommandMode.None;
            _commandModeAcknowledged = null;
            _squadSelectionAcknowledged = null;
            _lastVersion = uint.MaxValue;
            LastAppliedModel = UiAssistantHighlightModel.Empty;
        }

        public void ResetForMissionAttempt()
        {
            _squadTrayView?.ClearAssistantGuidance();
            _screenTargetActive = false;
            _commandCueActive = false;
            _commandGuidanceArmed = false;
            _awaitingNextShowMe = false;
            _worldTargetShowRequested = false;
            _pendingFirstShowMe = false;
            _selectSquadCompleted = false;
            _activeCommandMode = TacticalCommandMode.None;
            _awaitingWorldTargetMode = TacticalCommandMode.None;
            _lastVersion = uint.MaxValue;
            LastAppliedModel = UiAssistantHighlightModel.Empty;
            ApplyVisual(LastAppliedModel);
        }

        public void BindSquadTray(MatchHudSquadTrayView squadTrayView)
        {
            DetachSquadGuidanceButton();
            _squadTrayView?.ClearAssistantGuidance();
            _squadTrayView = squadTrayView;
            _squadTrayView?.ClearAssistantGuidance();
            RectTransform target = _squadTrayView?.AssistantGuidanceTarget;
            _squadGuidanceButton = target != null ? target.GetComponent<Button>() : null;
            _squadGuidanceButton?.onClick.AddListener(AcknowledgeSquadSelection);
            ApplyVisual(LastAppliedModel);
        }

        public void BindCommandControls(MatchOverlayCommandControlsView commandControlsView)
        {
            DetachCommandGuidanceButtons();
            _commandControlsView = commandControlsView;
            _moveGuidanceButton = _commandControlsView?.MoveButton;
            _attackGuidanceButton = _commandControlsView?.AttackButton;
            _moveGuidanceButton?.onClick.AddListener(AcknowledgeMoveCommand);
            _attackGuidanceButton?.onClick.AddListener(AcknowledgeAttackCommand);
            ApplyVisual(LastAppliedModel);
        }

        public void BindWorldCamera(Camera worldCamera)
        {
            _worldCamera = worldCamera;
        }

        public void ApplyCommandMode(TacticalCommandMode mode)
        {
            bool changed = _activeCommandMode != mode;
            _activeCommandMode = mode;
            if (changed && _commandCueActive && MatchesGuidedCommand(LastAppliedModel, mode))
                ArmGuidedCommand(mode);
            ApplyVisual(LastAppliedModel);
        }

        public void AcknowledgeCommandMode(TacticalCommandMode mode)
        {
            _activeCommandMode = mode;
            if (MatchesGuidedCommand(LastAppliedModel, mode))
                ArmGuidedCommand(mode);
            ApplyVisual(LastAppliedModel);
            _commandModeAcknowledged?.Invoke(mode);
        }

        private void ArmGuidedCommand(TacticalCommandMode mode)
        {
            _commandGuidanceArmed = true;
            _awaitingNextShowMe = true;
            _worldTargetShowRequested = false;
            _awaitingWorldTargetMode = mode;
        }

        public void CompleteWorldTarget(TacticalCommandMode mode)
        {
            if (MatchesGuidedRecommendation(LastAppliedModel, mode) && _commandGuidanceArmed)
            {
                _awaitingNextShowMe = true;
                _worldTargetShowRequested = false;
                _awaitingWorldTargetMode = mode;
            }
            ApplyVisual(LastAppliedModel);
        }

        public void BeginPendingShowMe(byte recommendationKind, byte targetKind)
        {
            if (recommendationKind == 0)
                return;

            // The squad selection callback runs after the real tray selection callback.
            // If the ECS panel projection is one frame behind, never teach Select twice:
            // the next explicit Show Me step is the Move command button.
            if (_selectSquadCompleted && recommendationKind == SelectRecommendationKind)
            {
                recommendationKind = MoveRecommendationKind;
                targetKind = WorldPositionTargetKind;
            }

            if (_awaitingNextShowMe)
            {
                bool continuesCurrentCommand =
                    LastAppliedModel.Active &&
                    LastAppliedModel.RecommendationKind == recommendationKind &&
                    MatchesGuidedRecommendation(LastAppliedModel, _awaitingWorldTargetMode);
                if (continuesCurrentCommand)
                {
                    // The player explicitly asked ARIA for the second half of the step.
                    // Reveal the world target now when the resolved ECS preview is already
                    // available; otherwise preserve the request until that preview arrives.
                    _awaitingNextShowMe = false;
                    _worldTargetShowRequested = true;
                    _commandGuidanceArmed = true;
                    _pendingFirstShowMe = false;
                    ApplyVisual(LastAppliedModel);
                    return;
                }

                // The recommendation advanced (for example Move -> Attack). Do not let the
                // completed command's wait state suppress the new command-button cue.
                _awaitingNextShowMe = false;
                _worldTargetShowRequested = false;
                _commandGuidanceArmed = false;
                _awaitingWorldTargetMode = TacticalCommandMode.None;
            }

            _pendingFirstShowMe = true;
            LastAppliedModel = new UiAssistantHighlightModel(
                uint.MaxValue,
                true,
                0,
                0,
                recommendationKind,
                targetKind,
                0f,
                0f,
                0f,
                1f);
            ApplyVisual(LastAppliedModel);
        }

        public void ApplyReadModel(UiAssistantHighlightModel model)
        {
            if (model.Active &&
                model.RecommendationKind == SelectRecommendationKind &&
                _selectSquadCompleted)
            {
                return;
            }
            if (!model.Active && (_pendingFirstShowMe || _awaitingNextShowMe))
                return;
            bool appliedStateMatchesReadModel =
                LastAppliedModel.Active == model.Active &&
                (!model.Active ||
                 LastAppliedModel.RequestId == model.RequestId &&
                 LastAppliedModel.RecommendationId == model.RecommendationId);
            if (_lastVersion == model.Version && appliedStateMatchesReadModel)
                return;

            if (model.Active)
            {
                _pendingFirstShowMe = false;
            }
            bool changedGuidance = _lastVersion != model.Version || !appliedStateMatchesReadModel;
            _lastVersion = model.Version;
            LastAppliedModel = model;
            if (changedGuidance)
            {
                bool sameGuidedCommand = MatchesGuidedRecommendation(model, _awaitingWorldTargetMode);
                if (_awaitingNextShowMe && sameGuidedCommand && !_worldTargetShowRequested)
                {
                    // A refreshed read model alone must not reveal the ground target. The
                    // player still needs to press Show Me for the next explicit instruction.
                    _commandGuidanceArmed = true;
                }
                else
                {
                    _commandGuidanceArmed = _worldTargetShowRequested && sameGuidedCommand;
                    _awaitingNextShowMe = false;
                    _worldTargetShowRequested = false;
                    if (!sameGuidedCommand)
                        _awaitingWorldTargetMode = TacticalCommandMode.None;
                }
            }
            ApplyVisual(model);
        }

        public void Tick()
        {
            if ((!_screenTargetActive && !_commandCueActive) ||
                _screenTargetIndicator == null ||
                _screenTargetCanvas == null)
                return;

            if (_commandCueActive)
            {
                TickCommandCue();
                return;
            }

            Camera worldCamera = _worldCamera;
            if (worldCamera == null || !worldCamera.isActiveAndEnabled)
            {
                worldCamera = Camera.main;
                if (worldCamera != null && worldCamera.isActiveAndEnabled)
                    _worldCamera = worldCamera;
            }

            if (worldCamera == null || !worldCamera.isActiveAndEnabled)
            {
                ShowScreenTargetFallback();
                return;
            }

            Vector3 viewport = worldCamera.WorldToViewportPoint(
                _screenTargetWorld + Vector3.up * 2.8f);
            if (viewport.z <= 0f)
            {
                ShowScreenTargetFallback();
                return;
            }

            Vector2 viewportAnchor = new(
                Mathf.Clamp(viewport.x, 0.08f, 0.92f),
                Mathf.Clamp(viewport.y, 0.26f, 0.88f));
            SetAnchorsIfChanged(_screenTargetIndicator, viewportAnchor);
            SetAnchoredPositionIfChanged(_screenTargetIndicator, Vector2.zero);
            _screenTargetIndicator.localScale = Vector3.one;
            if (!_screenTargetIndicator.gameObject.activeSelf)
                _screenTargetIndicator.gameObject.SetActive(true);
        }

        private void ApplyVisual(UiAssistantHighlightModel model)
        {
            if (_awaitingNextShowMe)
                model = UiAssistantHighlightModel.Empty;

            if (_panelPulse != null)
            {
                _panelPulse.gameObject.SetActive(model.Active);
                float strength = Mathf.Clamp01(model.Strength);
                _panelPulse.color = new Color(0.45f, 0.95f, 1f, 0.18f + strength * 0.32f);
            }

            // Keep guidance on the isolated top-level HUD canvas. A cue parented to
            // the authored squad card is clipped by the real tray hierarchy.
            _squadTrayView?.ClearAssistantGuidance();

            bool showCommandCue = ShouldShowCommandCue(model);
            bool suppressWorldRing = showCommandCue && IsGuidedCommand(model);
            ApplyWorldRing(model, !suppressWorldRing && !_pendingFirstShowMe);
            ApplyScreenTargetIndicator(model);
        }

        private void TickCommandCue()
        {
            RectTransform buttonRect = ResolveGuidedCommandButton(LastAppliedModel);
            if (buttonRect == null || !buttonRect.gameObject.activeInHierarchy ||
                !(_screenTargetCanvas.transform is RectTransform canvasRect))
            {
                _screenTargetIndicator.gameObject.SetActive(false);
                return;
            }

            buttonRect.GetWorldCorners(_commandButtonCorners);
            Vector3 buttonTopCenter = (_commandButtonCorners[1] + _commandButtonCorners[2]) * 0.5f;
            Canvas buttonCanvas = buttonRect.GetComponentInParent<Canvas>();
            Camera buttonCamera = buttonCanvas == null || buttonCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : buttonCanvas.worldCamera;
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(buttonCamera, buttonTopCenter);
            screenPoint.y += 12f;
            Camera eventCamera = _screenTargetCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : _screenTargetCanvas.worldCamera;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect, screenPoint, eventCamera, out Vector2 localPoint))
            {
                _screenTargetIndicator.gameObject.SetActive(false);
                return;
            }

            Vector2 half = _screenTargetIndicator.sizeDelta * 0.5f;
            Rect bounds = canvasRect.rect;
            localPoint.x = Mathf.Clamp(localPoint.x, bounds.xMin + half.x, bounds.xMax - half.x);
            localPoint.y = Mathf.Clamp(localPoint.y, bounds.yMin, bounds.yMax - _screenTargetIndicator.sizeDelta.y);
            SetAnchorsIfChanged(_screenTargetIndicator, new Vector2(0.5f, 0.5f));
            SetAnchoredPositionIfChanged(_screenTargetIndicator, localPoint);
            _screenTargetIndicator.localScale = Vector3.one;
            if (!_screenTargetIndicator.gameObject.activeSelf)
                _screenTargetIndicator.gameObject.SetActive(true);
        }

        private bool ShouldShowCommandCue(UiAssistantHighlightModel model)
        {
            bool selectSquad = model.RecommendationKind == SelectRecommendationKind &&
                               _squadTrayView?.AssistantGuidanceTarget != null;
            return model.Active && (selectSquad || IsGuidedCommand(model)) && !_commandGuidanceArmed;
        }

        private static bool IsGuidedCommand(UiAssistantHighlightModel model)
        {
            return model.RecommendationKind == MoveRecommendationKind ||
                   model.RecommendationKind == AttackRecommendationKind;
        }

        private static bool MatchesGuidedCommand(UiAssistantHighlightModel model, TacticalCommandMode mode)
        {
            return model.Active &&
                   (model.RecommendationKind == MoveRecommendationKind && mode == TacticalCommandMode.Move ||
                    model.RecommendationKind == AttackRecommendationKind && mode == TacticalCommandMode.Attack);
        }

        private static bool MatchesGuidedRecommendation(
            UiAssistantHighlightModel model,
            TacticalCommandMode mode)
        {
            return model.Active &&
                   (model.RecommendationKind == MoveRecommendationKind && mode == TacticalCommandMode.Move ||
                    model.RecommendationKind == AttackRecommendationKind && mode == TacticalCommandMode.Attack);
        }

        private RectTransform ResolveGuidedCommandButton(UiAssistantHighlightModel model)
        {
            if (model.RecommendationKind == SelectRecommendationKind)
                return _squadTrayView?.AssistantGuidanceTarget;

            Button button = model.RecommendationKind == MoveRecommendationKind
                ? _commandControlsView != null ? _commandControlsView.MoveButton : null
                : model.RecommendationKind == AttackRecommendationKind
                    ? _commandControlsView != null ? _commandControlsView.AttackButton : null
                    : null;
            return button != null ? button.transform as RectTransform : null;
        }

        private static string ResolveIndicatorText(UiAssistantHighlightModel model, bool commandCue)
        {
            if (model.RecommendationKind == SelectRecommendationKind)
                return "SELECT SQUAD\n\u25bc";
            if (model.RecommendationKind == MoveRecommendationKind)
                return commandCue ? "PRESS MOVE\n\u25bc" : "CLICK DESTINATION\n\u25bc";
            if (model.RecommendationKind == AttackRecommendationKind)
                return commandCue ? "PRESS ATTACK\n\u25bc" : "CLICK ENEMY\n\u25bc";
            return "ARIA TARGET\n\u25bc";
        }

        private void AcknowledgeSquadSelection()
        {
            if (_squadTrayView == null ||
                !_squadTrayView.IsAssistantGuidanceTargetSelected ||
                !LastAppliedModel.Active ||
                LastAppliedModel.RecommendationKind != SelectRecommendationKind)
            {
                return;
            }

            _selectSquadCompleted = true;
            _pendingFirstShowMe = false;
            _commandGuidanceArmed = false;
            _awaitingNextShowMe = false;
            _worldTargetShowRequested = false;
            _awaitingWorldTargetMode = TacticalCommandMode.None;
            LastAppliedModel = UiAssistantHighlightModel.Empty;
            ApplyVisual(LastAppliedModel);
            _squadSelectionAcknowledged?.Invoke();
            UiShellRuntimeGateway.TryEnqueueAssistantCommandIntent(
                UiAssistantCommandIntentKind.StopAssistantControl);
        }

        private void DetachSquadGuidanceButton()
        {
            _squadGuidanceButton?.onClick.RemoveListener(AcknowledgeSquadSelection);
            _squadGuidanceButton = null;
        }

        private void AcknowledgeMoveCommand() =>
            AcknowledgeCommandMode(TacticalCommandMode.Move);

        private void AcknowledgeAttackCommand() =>
            AcknowledgeCommandMode(TacticalCommandMode.Attack);

        private void DetachCommandGuidanceButtons()
        {
            _moveGuidanceButton?.onClick.RemoveListener(AcknowledgeMoveCommand);
            _attackGuidanceButton?.onClick.RemoveListener(AcknowledgeAttackCommand);
            _moveGuidanceButton = null;
            _attackGuidanceButton = null;
        }

    }
}
