using System;
using System.Collections.Generic;
using UnityEngine;
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
        ISelectionDiagnosticsSink diagnosticsSink = null,
        ISelectionUiReadModel selectionUiReadModel = null)
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
            selectionUiReadModel);
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
        private readonly ISelectionUiReadModel _selectionUiReadModel;
        private readonly List<(Button Button, UnityEngine.Events.UnityAction Action)> _commandTabRuntimeListeners = new();
        private bool _buildDrawerOpen;

        public Binding(
            MatchOverlayCommandControlsView view,
            ISelectionUiCommand selectionUiCommandSystem,
            BattleHudRuntimeFeedbackView runtimeFeedbackView,
            Action showBuildDrawer,
            Action closeBuildDrawer,
            ISelectionDiagnosticsSink diagnosticsSink,
            ISelectionUiReadModel selectionUiReadModel)
        {
            _view = view;
            _selectionUiCommandSystem = selectionUiCommandSystem;
            _runtimeFeedbackView = runtimeFeedbackView;
            _showBuildDrawer = showBuildDrawer;
            _closeBuildDrawer = closeBuildDrawer;
            _diagnosticsSink = diagnosticsSink;
            _selectionUiReadModel = selectionUiReadModel;
        }

        public void Bind()
        {
            RepairScanButtonRaycastTarget();
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
            _view.BuildButton?.onClick.RemoveListener(OnBuildButtonClicked);
            _view.HoldButton?.onClick.RemoveListener(OnHoldButtonClicked);
            _view.StopButton?.onClick.RemoveListener(OnStopButtonClicked);
            _view.CommandWheelStopButton?.onClick.RemoveListener(OnCommandWheelStopButtonClicked);
            UnbindCommandTabRuntimeFallbacks();
        }

        private void OnSelectButtonClicked()
        {
            bool enterSelectionMode = !IsCommandModePresented(TacticalCommandMode.Select);
            bool queued = _selectionUiCommandSystem != null &&
                (enterSelectionMode
                    ? _selectionUiCommandSystem.RequestEnterSelectionMode()
                    : _selectionUiCommandSystem.RequestExitSelectionMode());

            if (!queued)
                BattleHudRuntimeFeedbackBoundary.ApplyCommandResult(_runtimeFeedbackView, TacticalCommandResult.Rejected(
                    TacticalCommandReasonCode.CommandUnavailable,
                    "Selection command unavailable."));
        }

        private void OnBuildButtonClicked()
        {
            if (_showBuildDrawer != null)
            {
                _showBuildDrawer.Invoke();
                _buildDrawerOpen = true;
                BattleHudRuntimeFeedbackBoundary.ApplyStickyCommandMode(_runtimeFeedbackView, TacticalCommandMode.Build);
                return;
            }

            BattleHudRuntimeFeedbackBoundary.ApplyCommandResult(_runtimeFeedbackView, TacticalCommandResult.Rejected(
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
                BattleHudRuntimeFeedbackBoundary.ApplyCommandResult(_runtimeFeedbackView, TacticalCommandResult.Rejected(
                    TacticalCommandReasonCode.CommandUnavailable,
                    "Move command unavailable."));
        }

        private void OnAttackButtonClicked()
        {
            bool queued = _selectionUiCommandSystem != null &&
                _selectionUiCommandSystem.RequestAttackCommandMode();

            if (!queued)
                BattleHudRuntimeFeedbackBoundary.ApplyCommandResult(_runtimeFeedbackView, TacticalCommandResult.Rejected(
                    TacticalCommandReasonCode.CommandUnavailable,
                    "Attack command unavailable."));
        }

        private void OnScanButtonClicked()
        {
            if (!TryAcceptCapability(CommandCapability.Scan))
                return;

            CloseBuildDrawerIfOpen();
            bool queued = _selectionUiCommandSystem != null &&
                _selectionUiCommandSystem.RequestScanCommandMode();

            if (!queued)
                BattleHudRuntimeFeedbackBoundary.ApplyCommandResult(_runtimeFeedbackView, TacticalCommandResult.Rejected(
                    TacticalCommandReasonCode.CommandUnavailable,
                    "Scan command unavailable."));
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
                OnScanButtonClicked();
        }

        private bool IsKnownCommandButton(Button button)
        {
            return button != null &&
                   (button == _view.SelectButton ||
                    button == _view.MoveButton ||
                    button == _view.AttackButton ||
                    button == _view.ScanButton ||
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
            BattleHudRuntimeFeedbackBoundary.ClearStickyCommandMode(_runtimeFeedbackView, TacticalCommandMode.Build);
        }

        private bool IsCommandModePresented(TacticalCommandMode mode)
        {
            BattleHudRuntimeFeedbackState state = BattleHudRuntimeFeedbackBoundary.GetState(_runtimeFeedbackView);
            return state.CurrentCommandMode == mode ||
                state.StickyCommandMode == mode;
        }

        private void OnHoldButtonClicked()
        {
            if (!TryAcceptCapability(CommandCapability.Hold))
                return;

            _selectionUiCommandSystem?.RequestHoldPosition();
        }

        private void OnStopButtonClicked()
        {
            if (!TryAcceptCapability(CommandCapability.Stop))
                return;

            _selectionUiCommandSystem?.RequestStop();
        }

        private void OnCommandWheelStopButtonClicked()
        {
            if (!TryAcceptCapability(CommandCapability.Stop))
                return;

            _view.CommandWheelPanel?.Close();
            _selectionUiCommandSystem?.RequestStop();
        }

        public void RefreshCommandControlState(ISelectionUiReadModel selectionUiReadModel = null)
        {
            ISelectionUiReadModel readModel = selectionUiReadModel ?? _selectionUiReadModel;
            // Keep the bottom command rail interactive so hover/selected feedback remains visible.
            // Unavailable commands still report the specific rejection reason through TryAcceptCapability.
            ApplyButtonInteractable(_view.HoldButton, true);
            ApplyButtonInteractable(_view.StopButton, true);
            ApplyButtonInteractable(_view.CommandWheelStopButton, readModel == null || readModel.FocusedUnitCanStop);
            // Keep Scan pressable so unavailable units surface an explicit rejection message.
            ApplyButtonInteractable(_view.ScanButton, true);
        }

        private bool TryAcceptCapability(CommandCapability capability)
        {
            ISelectionUiReadModel readModel = _selectionUiReadModel;
            if (readModel == null)
                return true;

            bool accepted = capability switch
            {
                CommandCapability.Hold => readModel.FocusedUnitCanHold,
                CommandCapability.Stop => readModel.FocusedUnitCanStop,
                CommandCapability.Scan => readModel.FocusedUnitCanScan,
                _ => true
            };
            if (accepted)
                return true;

            TacticalCommandReasonCode reason = capability switch
            {
                CommandCapability.Hold => readModel.FocusedUnitHoldDisabledReason,
                CommandCapability.Stop => readModel.FocusedUnitStopDisabledReason,
                CommandCapability.Scan => readModel.FocusedUnitScanDisabledReason,
                _ => TacticalCommandReasonCode.CommandUnavailable
            };
            BattleHudRuntimeFeedbackBoundary.ApplyCommandResult(
                _runtimeFeedbackView,
                TacticalCommandResult.Rejected(reason));
            return false;
        }

        private static void ApplyButtonInteractable(Button button, bool interactable)
        {
            if (button != null)
                button.interactable = interactable;
        }

        private void OnBoardAllFeedbackClicked()
        {
            bool queued = _selectionUiCommandSystem != null &&
                _selectionUiCommandSystem.RequestBoardAllSelectedTransport();

            if (!queued)
                BattleHudRuntimeFeedbackBoundary.ApplyCommandResult(_runtimeFeedbackView, TacticalCommandResult.Rejected(
                    TacticalCommandReasonCode.CommandUnavailable,
                    "Board all unavailable."));
        }

        private void OnCancelFeedbackClicked()
        {
            bool queued = _selectionUiCommandSystem != null &&
                _selectionUiCommandSystem.RequestCancelActiveCommandMode();

            if (!queued)
                BattleHudRuntimeFeedbackBoundary.ApplyCommandResult(_runtimeFeedbackView, TacticalCommandResult.Rejected(
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
