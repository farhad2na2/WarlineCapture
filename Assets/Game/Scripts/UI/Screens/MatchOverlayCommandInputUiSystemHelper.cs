using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Game.Tactical.Contracts;
using Game.UI.Contracts;

namespace Game.UI.Runtime
{
    public sealed partial class MatchOverlayCommandInputUiSystemHelper
    {
        private readonly Dictionary<MatchOverlayCommandControlsView, Binding> _bindings = new();

        public void Bind(
            MatchOverlayCommandControlsView view,
            ISelectionUiCommand selectionUiCommandSystem,
            BattleHudRuntimeFeedbackView runtimeFeedbackView = null,
            Action showBuildDrawer = null,
            Action closeBuildDrawer = null,
            ISelectionDiagnosticsSink diagnosticsSink = null,
            ISelectionUiReadModel selectionUiReadModel = null,
            Action captureGameplayUiClick = null,
            IGameTextResolver gameTextResolver = null,
            Action<TacticalCommandMode> commandModeQueued = null)
        {
            if (view == null)
                return;

            Unbind(view);
            ResetCommandControlRuntimeListeners(view);

            var binding = new Binding(
                view,
                selectionUiCommandSystem,
                runtimeFeedbackView,
                showBuildDrawer,
                closeBuildDrawer,
                diagnosticsSink,
                selectionUiReadModel,
                captureGameplayUiClick,
                gameTextResolver,
                commandModeQueued);
            binding.Bind();
            _bindings.Add(view, binding);
        }

        public void Unbind(MatchOverlayCommandControlsView view)
        {
            if (view == null || !_bindings.TryGetValue(view, out Binding binding))
                return;

            binding.Unbind();
            _bindings.Remove(view);
        }

        public void RefreshCommandControlState(ISelectionUiReadModel selectionUiReadModel = null)
        {
            foreach (Binding binding in _bindings.Values)
                binding.RefreshCommandControlState(selectionUiReadModel);
        }

        private static void ResetCommandControlRuntimeListeners(MatchOverlayCommandControlsView view)
        {
            ClearButtonListeners(view.SelectButton);
            ClearButtonListeners(view.MoveButton);
            ClearButtonListeners(view.AttackButton);
            ClearButtonListeners(view.ScanButton);
            ClearButtonListeners(view.BoardButton);
            ClearButtonListeners(view.BuildButton);
            ClearButtonListeners(view.HoldButton);
            ClearButtonListeners(view.StopButton);
            ClearButtonListeners(view.CommandWheelStopButton);

            MatchOverlayCommandTabView[] tabs = view.CommandTabGroup != null ? view.CommandTabGroup.Tabs : null;
            if (tabs == null)
                return;

            for (int i = 0; i < tabs.Length; i++)
                ClearButtonListeners(tabs[i]?.Button);
        }

        private static void ClearButtonListeners(Button button)
        {
            button?.onClick.RemoveAllListeners();
        }

        private sealed partial class Binding
        {
            private readonly MatchOverlayCommandControlsView _view;
            private readonly ISelectionUiCommand _selectionUiCommandSystem;
            private readonly BattleHudRuntimeFeedbackView _runtimeFeedbackView;
            private readonly Action _showBuildDrawer;
            private readonly Action _closeBuildDrawer;
            private readonly ISelectionDiagnosticsSink _diagnosticsSink;
            private readonly ISelectionUiReadModel _selectionUiReadModel;
            private readonly Action _captureGameplayUiClick;
            private readonly IGameTextResolver _gameTextResolver;
            private readonly Action<TacticalCommandMode> _commandModeQueued;
            private readonly List<(Button Button, UnityEngine.Events.UnityAction Action)> _commandTabRuntimeListeners = new();
            private bool _buildDrawerOpen;
            private bool _hasAppliedVersionedCommandState;
            private uint _lastAppliedCommandStateVersion;

            public Binding(
                MatchOverlayCommandControlsView view,
                ISelectionUiCommand selectionUiCommandSystem,
                BattleHudRuntimeFeedbackView runtimeFeedbackView,
                Action showBuildDrawer,
                Action closeBuildDrawer,
                ISelectionDiagnosticsSink diagnosticsSink,
                ISelectionUiReadModel selectionUiReadModel,
                Action captureGameplayUiClick,
                IGameTextResolver gameTextResolver,
                Action<TacticalCommandMode> commandModeQueued)
            {
                _view = view;
                _selectionUiCommandSystem = selectionUiCommandSystem;
                _runtimeFeedbackView = runtimeFeedbackView;
                _showBuildDrawer = showBuildDrawer;
                _closeBuildDrawer = closeBuildDrawer;
                _diagnosticsSink = diagnosticsSink;
                _selectionUiReadModel = selectionUiReadModel;
                _captureGameplayUiClick = captureGameplayUiClick;
                _gameTextResolver = gameTextResolver ?? FallbackGameTextResolver.Instance;
                _commandModeQueued = commandModeQueued;
            }

            public void Bind()
            {
                _view.CommandWheelPanel?.BindGameTextResolver(_gameTextResolver);
                RepairScanButtonRaycastTarget();
                LogMoveCommandTrace(
                    $"matchHudCommandControlsBind view={_view.name} " +
                    $"select={DescribeButton(_view.SelectButton)} move={DescribeButton(_view.MoveButton)} " +
                    $"attack={DescribeButton(_view.AttackButton)} scan={DescribeButton(_view.ScanButton)} " +
                    $"board={DescribeButton(_view.BoardButton)} " +
                    $"build={DescribeButton(_view.BuildButton)} tabs={CountTabs(_view.CommandTabGroup)}");
                _runtimeFeedbackView?.BindFeedbackActionCallbacks(OnBoardAllFeedbackClicked, OnCancelFeedbackClicked);

                _view.SelectButton?.onClick.AddListener(OnSelectButtonClicked);
                _view.MoveButton?.onClick.AddListener(OnMoveButtonClicked);
                _view.AttackButton?.onClick.AddListener(OnAttackButtonClicked);
                _view.ScanButton?.onClick.AddListener(OnScanButtonClicked);
                _view.BoardButton?.onClick.AddListener(OnBoardButtonClicked);
                _view.BuildButton?.onClick.AddListener(OnBuildButtonClicked);
                _view.HoldButton?.onClick.AddListener(OnHoldButtonClicked);
                _view.StopButton?.onClick.AddListener(OnStopButtonClicked);
                _view.CommandWheelStopButton?.onClick.AddListener(OnCommandWheelStopButtonClicked);
                BindCommandTabRuntimeFallbacks();
                RefreshCommandControlState();
            }

            private static string DescribeButton(Button button)
            {
                return button != null ? button.name : "null";
            }

            private static int CountTabs(MatchOverlayCommandTabGroupView tabGroup)
            {
                if (tabGroup == null)
                    return -1;

                MatchOverlayCommandTabView[] tabs = tabGroup.Tabs;
                return tabs != null ? tabs.Length : 0;
            }

            private void RepairScanButtonRaycastTarget()
            {
                Button button = _view.ScanButton;
                if (button == null)
                    return;

                Graphic currentTarget = button.targetGraphic;
                bool targetBelongsToButton = currentTarget != null &&
                    currentTarget.transform != null &&
                    currentTarget.transform.IsChildOf(button.transform);
                if (targetBelongsToButton && currentTarget.raycastTarget)
                    return;

                Image hitTarget = button.GetComponent<Image>();
                if (hitTarget == null)
                    hitTarget = button.gameObject.AddComponent<Image>();

                hitTarget.color = new Color(0f, 0f, 0f, 0f);
                hitTarget.raycastTarget = true;
                button.targetGraphic = hitTarget;
            }

            public void Unbind()
            {
                _runtimeFeedbackView?.ClearFeedbackActionCallbacks();

                _view.SelectButton?.onClick.RemoveListener(OnSelectButtonClicked);
                _view.MoveButton?.onClick.RemoveListener(OnMoveButtonClicked);
                _view.AttackButton?.onClick.RemoveListener(OnAttackButtonClicked);
                _view.ScanButton?.onClick.RemoveListener(OnScanButtonClicked);
                _view.BoardButton?.onClick.RemoveListener(OnBoardButtonClicked);
                _view.BuildButton?.onClick.RemoveListener(OnBuildButtonClicked);
                _view.HoldButton?.onClick.RemoveListener(OnHoldButtonClicked);
                _view.StopButton?.onClick.RemoveListener(OnStopButtonClicked);
                _view.CommandWheelStopButton?.onClick.RemoveListener(OnCommandWheelStopButtonClicked);
                UnbindCommandTabRuntimeFallbacks();
            }

            private void OnSelectButtonClicked()
            {
                CaptureCommandUiClick();
                bool enterSelectionMode = !IsCommandModePresented(TacticalCommandMode.Select);
                bool queued = _selectionUiCommandSystem != null &&
                    (enterSelectionMode
                        ? _selectionUiCommandSystem.RequestEnterSelectionMode()
                        : _selectionUiCommandSystem.RequestExitSelectionMode());

                if (!queued)
                    ApplyCommandResult(TacticalCommandResult.Rejected(
                        TacticalCommandReasonCode.CommandUnavailable,
                        "Selection command unavailable."));
            }

            private void OnBuildButtonClicked()
            {
                CaptureCommandUiClick();
                if (_showBuildDrawer != null)
                {
                    _showBuildDrawer.Invoke();
                    _buildDrawerOpen = true;
                    BattleHudRuntimeFeedbackUiSystemHelper.ApplyStickyCommandMode(_runtimeFeedbackView, TacticalCommandMode.Build, _gameTextResolver);
                    return;
                }

                ApplyCommandResult(TacticalCommandResult.Rejected(
                    TacticalCommandReasonCode.BuildUnavailable,
                    _gameTextResolver.Get("build.feedback.drawer_not_ready", "Build drawer is not ready.")));
            }

            private void OnScanButtonClicked()
            {
                CaptureCommandUiClick();
                if (!TryAcceptCapability(CommandCapability.Scan))
                    return;

                CloseBuildDrawerIfOpen();
                bool queued = _selectionUiCommandSystem != null &&
                    _selectionUiCommandSystem.RequestScanCommandMode();

                if (!queued)
                    ApplyCommandResult(TacticalCommandResult.Rejected(
                        ResolveFallbackReason(CommandCapability.Scan),
                        ResolveUnavailableFeedbackMessage(CommandCapability.Scan, ResolveFallbackReason(CommandCapability.Scan))));
            }

            private void OnBoardButtonClicked()
            {
                CaptureCommandUiClick();
                CloseBuildDrawerIfOpen();
                bool queued = _selectionUiCommandSystem != null &&
                    _selectionUiCommandSystem.RequestBoardTargetMode();

                if (!queued)
                    ApplyCommandResult(TacticalCommandResult.Rejected(
                        TacticalCommandReasonCode.CommandUnavailable,
                        "Board command unavailable."));
            }

            private void BindCommandTabRuntimeFallbacks()
            {
                MatchOverlayCommandTabView[] tabs = _view.CommandTabGroup != null ? _view.CommandTabGroup.Tabs : null;
                if (tabs == null)
                    return;

                for (int i = 0; i < tabs.Length; i++)
                {
                    Button button = tabs[i]?.Button;
                    if (button == null)
                        continue;

                    Button capturedButton = button;
                    bool scanAlias = IsScanAliasCommandButton(capturedButton);
                    UnityEngine.Events.UnityAction action = () => OnCommandTabRuntimeClick(capturedButton, scanAlias);
                    capturedButton.onClick.AddListener(action);
                    _commandTabRuntimeListeners.Add((capturedButton, action));
                }
            }

            private void UnbindCommandTabRuntimeFallbacks()
            {
                for (int i = 0; i < _commandTabRuntimeListeners.Count; i++)
                    _commandTabRuntimeListeners[i].Button?.onClick.RemoveListener(_commandTabRuntimeListeners[i].Action);

                _commandTabRuntimeListeners.Clear();
            }

            private void OnCommandTabRuntimeClick(Button button, bool scanAlias)
            {
                if (scanAlias)
                {
                    OnScanButtonClicked();
                    return;
                }

                CaptureCommandUiClick();
            }

            private void CaptureCommandUiClick()
            {
                UIAudioEventGateway.Raise(UIAudioEventKind.ButtonPrimaryClick);
                _captureGameplayUiClick?.Invoke();
            }

            private void ApplyCommandResult(TacticalCommandResult result)
            {
                BattleHudRuntimeFeedbackUiSystemHelper.ApplyCommandResult(_runtimeFeedbackView, result, _gameTextResolver);
            }

            private bool IsKnownCommandButton(Button button)
            {
                return button != null &&
                       (button == _view.SelectButton ||
                        button == _view.MoveButton ||
                        button == _view.AttackButton ||
                        button == _view.ScanButton ||
                        button == _view.BoardButton ||
                        button == _view.BuildButton ||
                        button == _view.HoldButton ||
                        button == _view.StopButton ||
                        button == _view.CommandWheelStopButton);
            }

            private bool IsScanAliasCommandButton(Button button)
            {
                return button != null &&
                       !IsKnownCommandButton(button) &&
                       string.Equals(button.name, "SupportCommand", StringComparison.OrdinalIgnoreCase);
            }

            private void CloseBuildDrawerIfOpen()
            {
                if (!_buildDrawerOpen)
                    return;

                if (_closeBuildDrawer != null)
                    _closeBuildDrawer.Invoke();

                _buildDrawerOpen = false;
                BattleHudRuntimeFeedbackUiSystemHelper.ClearStickyCommandMode(_runtimeFeedbackView, TacticalCommandMode.Build);
            }

            private bool IsCommandModePresented(TacticalCommandMode mode)
            {
                BattleHudRuntimeFeedbackState state = BattleHudRuntimeFeedbackUiSystemHelper.GetState(_runtimeFeedbackView);
                return state.CurrentCommandMode == mode ||
                    state.StickyCommandMode == mode;
            }

            private void OnHoldButtonClicked()
            {
                CaptureCommandUiClick();
                if (!TryAcceptCapability(CommandCapability.Hold))
                    return;

                bool queued = _selectionUiCommandSystem != null &&
                    _selectionUiCommandSystem.RequestHoldPosition();

                if (!queued)
                    ApplyCommandResult(TacticalCommandResult.Rejected(
                        ResolveFallbackReason(CommandCapability.Hold),
                        ResolveUnavailableFeedbackMessage(CommandCapability.Hold, ResolveFallbackReason(CommandCapability.Hold))));
            }

            private void OnStopButtonClicked()
            {
                CaptureCommandUiClick();
                if (!TryAcceptCapability(CommandCapability.Stop))
                    return;

                bool queued = _selectionUiCommandSystem != null &&
                    _selectionUiCommandSystem.RequestStop();

                if (!queued)
                    ApplyCommandResult(TacticalCommandResult.Rejected(
                        ResolveFallbackReason(CommandCapability.Stop),
                        ResolveUnavailableFeedbackMessage(CommandCapability.Stop, ResolveFallbackReason(CommandCapability.Stop))));
            }

            private void OnCommandWheelStopButtonClicked()
            {
                CaptureCommandUiClick();
                if (!TryAcceptCapability(CommandCapability.Stop))
                    return;

                _view.CommandWheelPanel?.Close();
                bool queued = _selectionUiCommandSystem != null &&
                    _selectionUiCommandSystem.RequestStop();

                if (!queued)
                    ApplyCommandResult(TacticalCommandResult.Rejected(
                        ResolveFallbackReason(CommandCapability.Stop),
                        ResolveUnavailableFeedbackMessage(CommandCapability.Stop, ResolveFallbackReason(CommandCapability.Stop))));
            }

            private void OnBoardAllFeedbackClicked()
            {
                bool queued = _selectionUiCommandSystem != null &&
                    _selectionUiCommandSystem.RequestBoardAllSelectedTransport();

                if (!queued)
                    ApplyCommandResult(TacticalCommandResult.Rejected(
                        TacticalCommandReasonCode.CommandUnavailable,
                        "Board all unavailable."));
            }

            private void OnCancelFeedbackClicked()
            {
                bool queued = _selectionUiCommandSystem != null &&
                    _selectionUiCommandSystem.RequestCancelActiveCommandMode();

                if (!queued)
                    ApplyCommandResult(TacticalCommandResult.Rejected(
                        TacticalCommandReasonCode.CommandUnavailable,
                        "Cancel unavailable."));
            }

            private void LogMoveCommandTrace(string message)
            {
                _diagnosticsSink?.LogMoveCommandTrace(message);
            }

            private enum CommandCapability
            {
                Hold,
                Stop,
                Scan
            }
        }
    }
}
