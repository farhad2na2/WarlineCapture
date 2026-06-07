using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public sealed class MatchOverlayCommandInputSystem
{
    private readonly Dictionary<MatchOverlayCommandControlsView, Binding> _bindings = new();

    public void Bind(
        MatchOverlayCommandControlsView view,
        SelectionUiCommandSystem selectionUiCommandSystem,
        Action showBuildDrawer = null,
        Action closeBuildDrawer = null)
    {
        if (view == null)
            return;

        Unbind(view);

        var binding = new Binding(view, selectionUiCommandSystem, showBuildDrawer, closeBuildDrawer);
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

    private sealed class Binding
    {
        private readonly MatchOverlayCommandControlsView _view;
        private readonly SelectionUiCommandSystem _selectionUiCommandSystem;
        private readonly Action _showBuildDrawer;
        private readonly Action _closeBuildDrawer;
        private readonly List<TabButtonBinding> _tabButtonBindings = new();
        private MatchOverlayCommandTabVisualSystem _tabVisualSystem;
        private MatchOverlayCommandTabView _selectCommandTab;
        private MatchOverlayCommandTabView _buildCommandTab;
        private bool _buildDrawerOpen;

        public Binding(
            MatchOverlayCommandControlsView view,
            SelectionUiCommandSystem selectionUiCommandSystem,
            Action showBuildDrawer,
            Action closeBuildDrawer)
        {
            _view = view;
            _selectionUiCommandSystem = selectionUiCommandSystem;
            _showBuildDrawer = showBuildDrawer;
            _closeBuildDrawer = closeBuildDrawer;
        }

        public void Bind()
        {
            BindCommandTabs();

            _view.SelectButton?.onClick.AddListener(OnSelectButtonClicked);
            _view.MoveButton?.onClick.AddListener(OnMoveButtonClicked);
            _view.BuildButton?.onClick.AddListener(OnBuildButtonClicked);
            _view.HoldButton?.onClick.AddListener(OnHoldButtonClicked);
            _view.StopButton?.onClick.AddListener(OnStopButtonClicked);
            _view.CommandWheelStopButton?.onClick.AddListener(OnCommandWheelStopButtonClicked);

            Debug.Log(
                $"WARLINECAPTURE_MATCHHUD_COMMAND_INPUT_BOUND object={_view.name} " +
                $"selectBound={_view.SelectButton != null} moveBound={_view.MoveButton != null} buildBound={_view.BuildButton != null} holdBound={_view.HoldButton != null} " +
                $"stopBound={_view.StopButton != null} commandWheelStopBound={_view.CommandWheelStopButton != null} " +
                $"commandSystemBound={_selectionUiCommandSystem != null}");
        }

        public void Unbind()
        {
            UnbindCommandTabs();

            _view.SelectButton?.onClick.RemoveListener(OnSelectButtonClicked);
            _view.MoveButton?.onClick.RemoveListener(OnMoveButtonClicked);
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

            Debug.Log(
                $"WARLINECAPTURE_MATCHHUD_SELECT_CLICK object={_view.name} button={ButtonName(_view.SelectButton)} " +
                $"active={IsActive(_view.SelectButton)} interactable={IsInteractable(_view.SelectButton)} " +
                $"selected={selected} commandSystemBound={_selectionUiCommandSystem != null} queued={queued} frame={Time.frameCount}");

            if (!queued)
                Debug.LogWarning("WARLINECAPTURE_MATCHHUD_SELECT_CLICK_FAILED reason=SelectionCommandQueueUnavailable");
        }

        private void OnBuildButtonClicked()
        {
            if (new MissionCommandPolicySystem().TryRejectBuildForActiveOperation())
            {
                _tabVisualSystem?.Select(null);
                _buildDrawerOpen = false;
                return;
            }

            if (_showBuildDrawer != null)
                _showBuildDrawer.Invoke();
            else
                InstallBuildDrawerPopupFallback();

            _tabVisualSystem?.Select(_buildCommandTab);
            _buildDrawerOpen = true;
            BattleHudRuntimeFeedbackSystem.ApplyStickyCommandMode(TacticalCommandMode.Build);
        }

        private void OnMoveButtonClicked()
        {
            bool queued = _selectionUiCommandSystem != null &&
                _selectionUiCommandSystem.RequestMoveCommandMode();

            Debug.Log(
                $"WARLINECAPTURE_MATCHHUD_MOVE_CLICK object={_view.name} button={ButtonName(_view.MoveButton)} " +
                $"active={IsActive(_view.MoveButton)} interactable={IsInteractable(_view.MoveButton)} " +
                $"commandSystemBound={_selectionUiCommandSystem != null} queued={queued} frame={Time.frameCount}");

            if (!queued)
                Debug.LogWarning("WARLINECAPTURE_MATCHHUD_MOVE_CLICK_FAILED reason=SelectionCommandQueueUnavailable");
        }

        private void CloseBuildDrawerIfOpen()
        {
            if (!_buildDrawerOpen)
                return;

            if (_closeBuildDrawer != null)
                _closeBuildDrawer.Invoke();
            else
                CloseBuildDrawerPopupFallback();

            _buildDrawerOpen = false;
            BattleHudRuntimeFeedbackSystem.ClearStickyCommandMode(TacticalCommandMode.Build);
        }

        private void InstallBuildDrawerPopupFallback()
        {
            GameObject prefab = _view.BuildDrawerPopupPrefab;
            if (prefab == null)
                return;

            Transform parent = ResolvePopupParent();
            if (parent == null)
                return;

            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Transform child = parent.GetChild(i);
                if (child != null && child.name == prefab.name)
                    DestroyObject(child.gameObject);
            }

            GameObject instance = UnityEngine.Object.Instantiate(prefab, parent, false);
            instance.name = prefab.name;
            Stretch(instance.GetComponent<RectTransform>());
        }

        private void CloseBuildDrawerPopupFallback()
        {
            GameObject prefab = _view.BuildDrawerPopupPrefab;
            if (prefab == null)
                return;

            Transform parent = ResolvePopupParent();
            if (parent == null)
                return;

            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Transform child = parent.GetChild(i);
                if (child != null && child.name == prefab.name)
                    DestroyObject(child.gameObject);
            }
        }

        private Transform ResolvePopupParent()
        {
            Canvas canvas = _view.GetComponentInParent<Canvas>();
            if (canvas != null)
                return canvas.transform;

            return _view.transform.root;
        }

        private static void Stretch(RectTransform rectTransform)
        {
            if (rectTransform == null)
                return;

            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.localScale = Vector3.one;
        }

        private static void DestroyObject(UnityEngine.Object target)
        {
            if (target == null)
                return;

            if (Application.isPlaying)
                UnityEngine.Object.Destroy(target);
            else
                UnityEngine.Object.DestroyImmediate(target);
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

        private static string ButtonName(Button button)
        {
            return button != null ? button.name : "null";
        }

        private static bool IsActive(Button button)
        {
            return button != null && button.gameObject.activeInHierarchy;
        }

        private static bool IsInteractable(Button button)
        {
            return button != null && button.interactable;
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
