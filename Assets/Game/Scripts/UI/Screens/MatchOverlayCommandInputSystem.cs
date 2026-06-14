using System;
using System.Collections.Generic;
using UnityEngine.UI;

public sealed class MatchOverlayCommandInputSystem
{
    private readonly Dictionary<MatchOverlayCommandControlsView, Binding> _bindings = new();

    public void Bind(
        MatchOverlayCommandControlsView view,
        ISelectionUiCommand selectionUiCommandSystem,
        BattleHudRuntimeFeedbackView runtimeFeedbackView = null,
        Action showBuildDrawer = null,
        Action closeBuildDrawer = null,
        ISelectionDiagnosticsSink diagnosticsSink = null)
    {
        if (view == null)
            return;

        Unbind(view);
        ResetCommandControlRuntimeListeners(view);

        var binding = new Binding(view, selectionUiCommandSystem, runtimeFeedbackView, showBuildDrawer, closeBuildDrawer, diagnosticsSink);
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

    private static void ResetCommandControlRuntimeListeners(MatchOverlayCommandControlsView view)
    {
        ClearButtonListeners(view.SelectButton);
        ClearButtonListeners(view.MoveButton);
        ClearButtonListeners(view.AttackButton);
        ClearButtonListeners(view.ScanButton);
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

    private sealed class Binding
    {
        private readonly MatchOverlayCommandControlsView _view;
        private readonly ISelectionUiCommand _selectionUiCommandSystem;
        private readonly BattleHudRuntimeFeedbackView _runtimeFeedbackView;
        private readonly Action _showBuildDrawer;
        private readonly Action _closeBuildDrawer;
        private readonly ISelectionDiagnosticsSink _diagnosticsSink;
        private bool _buildDrawerOpen;

        public Binding(
            MatchOverlayCommandControlsView view,
            ISelectionUiCommand selectionUiCommandSystem,
            BattleHudRuntimeFeedbackView runtimeFeedbackView,
            Action showBuildDrawer,
            Action closeBuildDrawer,
            ISelectionDiagnosticsSink diagnosticsSink)
        {
            _view = view;
            _selectionUiCommandSystem = selectionUiCommandSystem;
            _runtimeFeedbackView = runtimeFeedbackView;
            _showBuildDrawer = showBuildDrawer;
            _closeBuildDrawer = closeBuildDrawer;
            _diagnosticsSink = diagnosticsSink;
        }

        public void Bind()
        {
            LogMoveCommandTrace(
                $"matchHudCommandControlsBind view={_view.name} " +
                $"select={DescribeButton(_view.SelectButton)} move={DescribeButton(_view.MoveButton)} " +
                $"attack={DescribeButton(_view.AttackButton)} scan={DescribeButton(_view.ScanButton)} " +
                $"build={DescribeButton(_view.BuildButton)} tabs={CountTabs(_view.CommandTabGroup)}");
            _runtimeFeedbackView?.BindFeedbackActionCallbacks(OnBoardAllFeedbackClicked, OnCancelFeedbackClicked);

            _view.SelectButton?.onClick.AddListener(OnSelectButtonClicked);
            _view.MoveButton?.onClick.AddListener(OnMoveButtonClicked);
            _view.AttackButton?.onClick.AddListener(OnAttackButtonClicked);
            _view.ScanButton?.onClick.AddListener(OnScanButtonClicked);
            _view.BuildButton?.onClick.AddListener(OnBuildButtonClicked);
            _view.HoldButton?.onClick.AddListener(OnHoldButtonClicked);
            _view.StopButton?.onClick.AddListener(OnStopButtonClicked);
            _view.CommandWheelStopButton?.onClick.AddListener(OnCommandWheelStopButtonClicked);
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

        public void Unbind()
        {
            _runtimeFeedbackView?.ClearFeedbackActionCallbacks();

            _view.SelectButton?.onClick.RemoveListener(OnSelectButtonClicked);
            _view.MoveButton?.onClick.RemoveListener(OnMoveButtonClicked);
            _view.AttackButton?.onClick.RemoveListener(OnAttackButtonClicked);
            _view.ScanButton?.onClick.RemoveListener(OnScanButtonClicked);
            _view.BuildButton?.onClick.RemoveListener(OnBuildButtonClicked);
            _view.HoldButton?.onClick.RemoveListener(OnHoldButtonClicked);
            _view.StopButton?.onClick.RemoveListener(OnStopButtonClicked);
            _view.CommandWheelStopButton?.onClick.RemoveListener(OnCommandWheelStopButtonClicked);
        }

        private void OnSelectButtonClicked()
        {
            bool enterSelectionMode = !IsCommandModePresented(TacticalCommandMode.Select);
            bool queued = _selectionUiCommandSystem != null &&
                (enterSelectionMode
                    ? _selectionUiCommandSystem.RequestEnterSelectionMode()
                    : _selectionUiCommandSystem.RequestExitSelectionMode());

            if (!queued)
                BattleHudRuntimeFeedbackSystem.ApplyCommandResult(_runtimeFeedbackView, TacticalCommandResult.Rejected(
                    TacticalCommandReasonCode.CommandUnavailable,
                    "Selection command unavailable."));
        }

        private void OnBuildButtonClicked()
        {
            if (_showBuildDrawer != null)
            {
                _showBuildDrawer.Invoke();
                _buildDrawerOpen = true;
                BattleHudRuntimeFeedbackSystem.ApplyStickyCommandMode(_runtimeFeedbackView, TacticalCommandMode.Build);
                return;
            }

            BattleHudRuntimeFeedbackSystem.ApplyCommandResult(_runtimeFeedbackView, TacticalCommandResult.Rejected(
                TacticalCommandReasonCode.BuildUnavailable,
                "Build drawer is not ready."));
        }

        private void OnMoveButtonClicked()
        {
            LogMoveCommandTrace(
                $"moveButtonClicked view={_view.name} hasSelectionUi={_selectionUiCommandSystem != null}");
            bool queued = _selectionUiCommandSystem != null &&
                _selectionUiCommandSystem.RequestMoveCommandMode();
            LogMoveCommandTrace($"moveButtonRequestMoveCommandMode queued={queued}");

            if (!queued)
                BattleHudRuntimeFeedbackSystem.ApplyCommandResult(_runtimeFeedbackView, TacticalCommandResult.Rejected(
                    TacticalCommandReasonCode.CommandUnavailable,
                    "Move command unavailable."));
        }

        private void OnAttackButtonClicked()
        {
            bool queued = _selectionUiCommandSystem != null &&
                _selectionUiCommandSystem.RequestAttackCommandMode();

            if (!queued)
                BattleHudRuntimeFeedbackSystem.ApplyCommandResult(_runtimeFeedbackView, TacticalCommandResult.Rejected(
                    TacticalCommandReasonCode.CommandUnavailable,
                    "Attack command unavailable."));
        }

        private void OnScanButtonClicked()
        {
            CloseBuildDrawerIfOpen();
            bool queued = _selectionUiCommandSystem != null &&
                _selectionUiCommandSystem.RequestScanCommandMode();

            if (!queued)
                BattleHudRuntimeFeedbackSystem.ApplyCommandResult(_runtimeFeedbackView, TacticalCommandResult.Rejected(
                    TacticalCommandReasonCode.CommandUnavailable,
                    "Scan command unavailable."));
        }

        private void CloseBuildDrawerIfOpen()
        {
            if (!_buildDrawerOpen)
                return;

            if (_closeBuildDrawer != null)
                _closeBuildDrawer.Invoke();

            _buildDrawerOpen = false;
            BattleHudRuntimeFeedbackSystem.ClearStickyCommandMode(_runtimeFeedbackView, TacticalCommandMode.Build);
        }

        private bool IsCommandModePresented(TacticalCommandMode mode)
        {
            BattleHudRuntimeFeedbackState state = BattleHudRuntimeFeedbackSystem.GetState(_runtimeFeedbackView);
            return state.CurrentCommandMode == mode ||
                state.StickyCommandMode == mode;
        }

        private void OnHoldButtonClicked()
        {
            _selectionUiCommandSystem?.RequestHoldPosition();
        }

        private void OnStopButtonClicked()
        {
            _selectionUiCommandSystem?.RequestStop();
        }

        private void OnCommandWheelStopButtonClicked()
        {
            _view.CommandWheelPanel?.Close();
            _selectionUiCommandSystem?.RequestStop();
        }

        private void OnBoardAllFeedbackClicked()
        {
            bool queued = _selectionUiCommandSystem != null &&
                _selectionUiCommandSystem.RequestBoardAllSelectedTransport();

            if (!queued)
                BattleHudRuntimeFeedbackSystem.ApplyCommandResult(_runtimeFeedbackView, TacticalCommandResult.Rejected(
                    TacticalCommandReasonCode.CommandUnavailable,
                    "Board all unavailable."));
        }

        private void OnCancelFeedbackClicked()
        {
            bool queued = _selectionUiCommandSystem != null &&
                _selectionUiCommandSystem.RequestCancelActiveCommandMode();

            if (!queued)
                BattleHudRuntimeFeedbackSystem.ApplyCommandResult(_runtimeFeedbackView, TacticalCommandResult.Rejected(
                    TacticalCommandReasonCode.CommandUnavailable,
                    "Cancel unavailable."));
        }

        private void LogMoveCommandTrace(string message)
        {
            _diagnosticsSink?.LogMoveCommandTrace(message);
        }
    }
}
