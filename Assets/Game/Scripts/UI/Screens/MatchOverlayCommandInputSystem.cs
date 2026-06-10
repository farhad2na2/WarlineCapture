using System;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.UI;

public sealed class MatchOverlayCommandInputSystem
{
    private readonly Dictionary<MatchOverlayCommandControlsView, Binding> _bindings = new();

    public void Bind(
        MatchOverlayCommandControlsView view,
        SelectionUiCommandSystem selectionUiCommandSystem,
        BattleHudRuntimeFeedbackView runtimeFeedbackView = null,
        Action showBuildDrawer = null,
        Action closeBuildDrawer = null)
    {
        if (view == null)
            return;

        Unbind(view);
        ResetCommandControlRuntimeListeners(view);

        var binding = new Binding(view, selectionUiCommandSystem, runtimeFeedbackView, showBuildDrawer, closeBuildDrawer);
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
        private readonly SelectionUiCommandSystem _selectionUiCommandSystem;
        private readonly BattleHudRuntimeFeedbackView _runtimeFeedbackView;
        private readonly Action _showBuildDrawer;
        private readonly Action _closeBuildDrawer;
        private readonly List<TabButtonBinding> _tabButtonBindings = new();
        private MatchOverlayCommandTabVisualSystem _tabVisualSystem;
        private MatchOverlayCommandTabView _selectCommandTab;
        private MatchOverlayCommandTabView _buildCommandTab;
        private MatchOverlayCommandTabView _scanCommandTab;
        private bool _buildDrawerOpen;

        public Binding(
            MatchOverlayCommandControlsView view,
            SelectionUiCommandSystem selectionUiCommandSystem,
            BattleHudRuntimeFeedbackView runtimeFeedbackView,
            Action showBuildDrawer,
            Action closeBuildDrawer)
        {
            _view = view;
            _selectionUiCommandSystem = selectionUiCommandSystem;
            _runtimeFeedbackView = runtimeFeedbackView;
            _showBuildDrawer = showBuildDrawer;
            _closeBuildDrawer = closeBuildDrawer;
        }

        public void Bind()
        {
            BindCommandTabs();

            _view.SelectButton?.onClick.AddListener(OnSelectButtonClicked);
            _view.MoveButton?.onClick.AddListener(OnMoveButtonClicked);
            _view.AttackButton?.onClick.AddListener(OnAttackButtonClicked);
            _view.ScanButton?.onClick.AddListener(OnScanButtonClicked);
            _view.BuildButton?.onClick.AddListener(OnBuildButtonClicked);
            _view.HoldButton?.onClick.AddListener(OnHoldButtonClicked);
            _view.StopButton?.onClick.AddListener(OnStopButtonClicked);
            _view.CommandWheelStopButton?.onClick.AddListener(OnCommandWheelStopButtonClicked);
        }

        public void Unbind()
        {
            UnbindCommandTabs();

            _view.SelectButton?.onClick.RemoveListener(OnSelectButtonClicked);
            _view.MoveButton?.onClick.RemoveListener(OnMoveButtonClicked);
            _view.AttackButton?.onClick.RemoveListener(OnAttackButtonClicked);
            _view.ScanButton?.onClick.RemoveListener(OnScanButtonClicked);
            _view.BuildButton?.onClick.RemoveListener(OnBuildButtonClicked);
            _view.HoldButton?.onClick.RemoveListener(OnHoldButtonClicked);
            _view.StopButton?.onClick.RemoveListener(OnStopButtonClicked);
            _view.CommandWheelStopButton?.onClick.RemoveListener(OnCommandWheelStopButtonClicked);
        }

        private void BindCommandTabs()
        {
            MatchOverlayCommandTabGroupView tabGroup = _view.CommandTabGroup;
            if (tabGroup == null)
                return;

            _tabVisualSystem = new MatchOverlayCommandTabVisualSystem(tabGroup);

            MatchOverlayCommandTabView[] tabs = tabGroup.Tabs;
            if (tabs != null)
            {
                foreach (MatchOverlayCommandTabView tab in tabs)
                {
                    Button button = tab?.Button;
                    if (button == null)
                        continue;

                    if (button == _view.SelectButton)
                        _selectCommandTab = tab;
                    if (button == _view.BuildButton)
                        _buildCommandTab = tab;
                    if (button == _view.ScanButton)
                        _scanCommandTab = tab;

                    MatchOverlayCommandTabView capturedTab = tab;
                    UnityAction listener = () => OnCommandTabClicked(capturedTab);
                    button.onClick.AddListener(listener);
                    _tabButtonBindings.Add(new TabButtonBinding(button, listener));
                }
            }

            _tabVisualSystem.ApplyDefaultSelection();
        }

        private void UnbindCommandTabs()
        {
            foreach (TabButtonBinding binding in _tabButtonBindings)
                binding.Button.onClick.RemoveListener(binding.Listener);

            _tabButtonBindings.Clear();
            _tabVisualSystem = null;
        }

        private void OnCommandTabClicked(MatchOverlayCommandTabView tab)
        {
            if (ReferenceEquals(tab, _buildCommandTab))
            {
                _tabVisualSystem?.Select(_buildCommandTab);
                return;
            }

            CloseBuildDrawerIfOpen();
            _tabVisualSystem?.Toggle(tab);
        }

        private void OnSelectButtonClicked()
        {
            bool selected = _tabVisualSystem == null || _tabVisualSystem.IsSelected(_selectCommandTab);
            bool queued = _selectionUiCommandSystem != null &&
                (selected
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
                _tabVisualSystem?.Select(_buildCommandTab);
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
            bool queued = _selectionUiCommandSystem != null &&
                _selectionUiCommandSystem.RequestMoveCommandMode();

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
            _tabVisualSystem?.Select(_scanCommandTab);
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

        private readonly struct TabButtonBinding
        {
            public readonly Button Button;
            public readonly UnityAction Listener;

            public TabButtonBinding(Button button, UnityAction listener)
            {
                Button = button;
                Listener = listener;
            }
        }
    }
}
