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
        private const int WorldRingSegments = 64;
        private const int WorldAccentSegmentCount = 8;
        private const int WorldBracketCount = 4;
        private const float WorldRingRadius = 3.15f;
        private const float WorldRingHeightOffset = 0.28f;
        private const float WorldRingWidth = 0.16f;
        private const byte SelectRecommendationKind = 1;
        private const byte MoveRecommendationKind = 2;
        private const byte AttackRecommendationKind = 3;
        private const byte BuildRecommendationKind = 4;
        private const byte ProduceRecommendationKind = 5;
        private const byte WorldPositionTargetKind = 1;
        private const byte UiSurfaceTargetKind = 4;

        private Image _panelPulse;
        private GameObject _worldRingRoot;
        private LineRenderer _worldRingRenderer;
        private LineRenderer[] _worldAccentRenderers;
        private LineRenderer[] _worldBracketRenderers;
        private LineRenderer[] _worldCrosshairRenderers;
        private Material _worldRingMaterial;
        private MatchHudSquadTrayView _squadTrayView;
        private MatchOverlayCommandControlsView _commandControlsView;
        private RectTransform _screenTargetIndicator;
        private TextMeshProUGUI _screenTargetLabel;
        private Canvas _screenTargetCanvas;
        private CanvasGroup _screenTargetGroup;
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
        private Button _buildGuidanceButton;
        private Button _barracksGuidanceButton;
        private bool _buildDrawerOpenRequested;
        private BuildDrawerView _buildDrawerView;
        private BuildDrawerCatalogRuntimeView _buildDrawerCatalogRuntimeView;
        private TacticalCommandMode _activeCommandMode;
        private TacticalCommandMode _awaitingWorldTargetMode;
        private Action<TacticalCommandMode> _commandModeAcknowledged;
        private Action _squadSelectionAcknowledged;
        private Action<byte> _uiSurfaceAcknowledged;
        private readonly Vector3[] _commandButtonCorners = new Vector3[4];
        private uint _lastVersion = uint.MaxValue;

        public UiAssistantHighlightModel LastAppliedModel { get; private set; } = UiAssistantHighlightModel.Empty;

        public void Bind(
            Image panelPulse,
            Action<TacticalCommandMode> commandModeAcknowledged = null,
            Action squadSelectionAcknowledged = null,
            Action<byte> uiSurfaceAcknowledged = null)
        {
            _panelPulse = panelPulse;
            _commandModeAcknowledged = commandModeAcknowledged;
            _squadSelectionAcknowledged = squadSelectionAcknowledged;
            _uiSurfaceAcknowledged = uiSurfaceAcknowledged;
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
            DetachBuildGuidanceButton();
            DetachBarracksGuidanceButton();
            DetachResourceGuidanceTarget();
            _squadTrayView?.ClearAssistantGuidance();
            _panelPulse = null;
            _squadTrayView = null;
            _commandControlsView = null;
            _buildDrawerView = null;
            _buildDrawerCatalogRuntimeView = null;
            _buildDrawerOpenRequested = false;
            _resourceGuidanceTarget = null;
            DestroyObject(_worldRingRoot);
            DestroyObject(_worldRingMaterial);
            DestroyObject(_screenTargetIndicator != null ? _screenTargetIndicator.gameObject : null);
            _worldRingRoot = null;
            _worldRingRenderer = null;
            _worldAccentRenderers = null;
            _worldBracketRenderers = null;
            _worldCrosshairRenderers = null;
            _worldRingMaterial = null;
            _screenTargetIndicator = null;
            _screenTargetLabel = null;
            _screenTargetCanvas = null;
            _screenTargetGroup = null;
            _worldCamera = null;
            _screenTargetActive = false;
            _commandCueActive = false;
            _commandGuidanceArmed = false;
            _awaitingNextShowMe = false;
            _worldTargetShowRequested = false;
            _pendingFirstShowMe = false;
            _selectSquadCompleted = false;
            _localUiCueActive = false;
            _activeCommandMode = TacticalCommandMode.None;
            _awaitingWorldTargetMode = TacticalCommandMode.None;
            _commandModeAcknowledged = null;
            _squadSelectionAcknowledged = null;
            _uiSurfaceAcknowledged = null;
            _lastVersion = uint.MaxValue;
            LastAppliedModel = UiAssistantHighlightModel.Empty;
        }

        public void ResetForMissionAttempt()
        {
            _squadTrayView?.ClearAssistantGuidance();
            _buildDrawerOpenRequested = false;
            _screenTargetActive = false;
            _commandCueActive = false;
            _commandGuidanceArmed = false;
            _awaitingNextShowMe = false;
            _worldTargetShowRequested = false;
            _pendingFirstShowMe = false;
            _selectSquadCompleted = false;
            _localUiCueActive = false;
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
            if (_selectSquadCompleted && recommendationKind == SelectRecommendationKind &&
                targetKind != UiSurfaceTargetKind)
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
                    // Keep an unresolved local command cue hidden until ECS returns the
                    // canonical world target. Clearing this flag here would project the
                    // provisional zero position and point near the squad.
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

            _localUiCueActive = targetKind == UiSurfaceTargetKind;
            _pendingFirstShowMe = !_localUiCueActive;
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
            if (_localUiCueActive && !model.Active)
                return;
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
            if (!_commandCueActive || _screenTargetIndicator == null || _screenTargetCanvas == null)
                return;

            TickCommandCue();
        }

        private void ApplyVisual(UiAssistantHighlightModel model)
        {
            if (_awaitingNextShowMe)
                model = UiAssistantHighlightModel.Empty;

            if (_panelPulse != null)
            {
                // Guidance is communicated by stable focus geometry. Flashing the ARIA panel
                // competed with the target and made disabled cards appear inconsistently live.
                _panelPulse.gameObject.SetActive(false);
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
            Canvas buttonCanvas = buttonRect.GetComponentInParent<Canvas>();
            Camera buttonCamera = buttonCanvas == null || buttonCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : buttonCanvas.worldCamera;
            Vector2 bottomLeftScreen = RectTransformUtility.WorldToScreenPoint(
                buttonCamera, _commandButtonCorners[0]);
            Vector2 topRightScreen = RectTransformUtility.WorldToScreenPoint(
                buttonCamera, _commandButtonCorners[2]);
            Camera eventCamera = _screenTargetCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : _screenTargetCanvas.worldCamera;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect, bottomLeftScreen, eventCamera, out Vector2 bottomLeft) ||
                !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect, topRightScreen, eventCamera, out Vector2 topRight))
            {
                _screenTargetIndicator.gameObject.SetActive(false);
                return;
            }

            const float borderPadding = 10f;
            Vector2 size = new(
                Mathf.Max(96f, Mathf.Abs(topRight.x - bottomLeft.x) + borderPadding * 2f),
                Mathf.Max(76f, Mathf.Abs(topRight.y - bottomLeft.y) + borderPadding * 2f));
            _screenTargetIndicator.sizeDelta = size;
            RectTransform caption = _screenTargetLabel != null
                ? _screenTargetLabel.transform.parent as RectTransform
                : null;
            if (caption != null)
                caption.sizeDelta = new Vector2(Mathf.Clamp(size.x - 20f, 240f, 440f), 64f);

            Vector2 localPoint = (bottomLeft + topRight) * 0.5f;
            Vector2 half = size * 0.5f;
            Rect bounds = canvasRect.rect;
            localPoint.x = Mathf.Clamp(localPoint.x, bounds.xMin + half.x, bounds.xMax - half.x);
            localPoint.y = Mathf.Clamp(
                localPoint.y,
                bounds.yMin + half.y,
                bounds.yMax - half.y - 24f);
            SetAnchorsIfChanged(_screenTargetIndicator, new Vector2(0.5f, 0.5f));
            SetAnchoredPositionIfChanged(_screenTargetIndicator, localPoint);
            _screenTargetIndicator.localScale = Vector3.one;
            if (!_screenTargetIndicator.gameObject.activeSelf)
                _screenTargetIndicator.gameObject.SetActive(true);
        }

        private bool ShouldShowCommandCue(UiAssistantHighlightModel model)
        {
            bool selectSquad = model.RecommendationKind == SelectRecommendationKind &&
                               model.TargetKind != UiSurfaceTargetKind &&
                               _squadTrayView?.AssistantGuidanceTarget != null;
            bool uiSurface = model.TargetKind == UiSurfaceTargetKind &&
                             ResolveGuidedCommandButton(model) != null;
            return model.Active && (selectSquad || uiSurface || IsGuidedCommand(model)) &&
                   !_commandGuidanceArmed;
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
