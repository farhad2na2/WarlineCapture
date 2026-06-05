using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class MatchOverlayCommandInputSystem
{
    private readonly Dictionary<MatchOverlayCommandControlsView, Binding> _bindings = new();

    public void Bind(
        MatchOverlayCommandControlsView view,
        SelectionUiCommandSystem selectionUiCommandSystem)
    {
        if (view == null)
            return;

        Unbind(view);

        var binding = new Binding(view, selectionUiCommandSystem);
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

        public Binding(
            MatchOverlayCommandControlsView view,
            SelectionUiCommandSystem selectionUiCommandSystem)
        {
            _view = view;
            _selectionUiCommandSystem = selectionUiCommandSystem;
        }

        public void Bind()
        {
            _view.SelectButton?.onClick.AddListener(OnSelectButtonClicked);
            _view.HoldButton?.onClick.AddListener(OnHoldButtonClicked);
            _view.StopButton?.onClick.AddListener(OnStopButtonClicked);
            _view.CommandWheelStopButton?.onClick.AddListener(OnCommandWheelStopButtonClicked);

            Debug.Log(
                $"WARLINECAPTURE_MATCHHUD_COMMAND_INPUT_BOUND object={_view.name} " +
                $"selectBound={_view.SelectButton != null} holdBound={_view.HoldButton != null} " +
                $"stopBound={_view.StopButton != null} commandWheelStopBound={_view.CommandWheelStopButton != null} " +
                $"commandSystemBound={_selectionUiCommandSystem != null}");
        }

        public void Unbind()
        {
            _view.SelectButton?.onClick.RemoveListener(OnSelectButtonClicked);
            _view.HoldButton?.onClick.RemoveListener(OnHoldButtonClicked);
            _view.StopButton?.onClick.RemoveListener(OnStopButtonClicked);
            _view.CommandWheelStopButton?.onClick.RemoveListener(OnCommandWheelStopButtonClicked);
        }

        private void OnSelectButtonClicked()
        {
            bool queued = _selectionUiCommandSystem != null &&
                _selectionUiCommandSystem.RequestEnterSelectionMode();

            Debug.Log(
                $"WARLINECAPTURE_MATCHHUD_SELECT_CLICK object={_view.name} button={ButtonName(_view.SelectButton)} " +
                $"active={IsActive(_view.SelectButton)} interactable={IsInteractable(_view.SelectButton)} " +
                $"commandSystemBound={_selectionUiCommandSystem != null} queued={queued} frame={Time.frameCount}");

            if (!queued)
                Debug.LogWarning("WARLINECAPTURE_MATCHHUD_SELECT_CLICK_FAILED reason=SelectionCommandQueueUnavailable");
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
    }
}
