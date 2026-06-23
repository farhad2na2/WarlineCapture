using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class MatchHudRightQuickRailView : MonoBehaviour
{
    [SerializeField] private Button buildButton;

    private Action _buildCommandClicked;
    private ISelectionUiCommand _selectionUiCommandSystem;
    private BattleHudRuntimeFeedbackView _runtimeFeedbackView;
    private bool _buildButtonListenerInstalled;
    private Canvas _cachedCanvas;

    public Button BuildButton => buildButton;

    private void OnEnable()
    {
        InstallBuildButtonListener();
        ClearBuildButtonSelection();
    }

    private void OnDisable()
    {
        UninstallBuildButtonListener();
    }

    private void OnTransformParentChanged()
    {
        _cachedCanvas = null;
    }

    public void BindBuildCommand(
        Action buildCommandClicked,
        ISelectionUiCommand selectionUiCommandSystem,
        BattleHudRuntimeFeedbackView runtimeFeedbackView = null)
    {
        _buildCommandClicked = buildCommandClicked;
        _selectionUiCommandSystem = selectionUiCommandSystem;
        _runtimeFeedbackView = runtimeFeedbackView;
        InstallBuildButtonListener();
        ClearBuildButtonSelection();
    }

    public void UnbindBuildCommand()
    {
        _buildCommandClicked = null;
        _selectionUiCommandSystem = null;
        _runtimeFeedbackView = null;
    }

    public bool ContainsScreenPoint(Vector2 screenPosition)
    {
        Camera eventCamera = ResolveEventCamera();
        return ContainsButton(buildButton, screenPosition, eventCamera);
    }

    private void OnBuildButtonClicked()
    {
        TriggerBuildCommand();
    }

    private void TriggerBuildCommand()
    {
        _selectionUiCommandSystem?.CaptureUiClickSequence();

        if (_buildCommandClicked != null)
        {
            _buildCommandClicked.Invoke();
            return;
        }

        BattleHudRuntimeFeedbackBoundary.ApplyCommandResult(_runtimeFeedbackView, TacticalCommandResult.Rejected(
            TacticalCommandReasonCode.BuildUnavailable,
            "Build drawer is not ready."));
    }

    private void InstallBuildButtonListener()
    {
        if (buildButton == null)
            return;

        buildButton.onClick.RemoveListener(OnBuildButtonClicked);
        buildButton.onClick.AddListener(OnBuildButtonClicked);
        _buildButtonListenerInstalled = true;
    }

    private void ClearBuildButtonSelection()
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null || buildButton == null)
            return;

        if (eventSystem.currentSelectedGameObject == buildButton.gameObject)
            eventSystem.SetSelectedGameObject(null);
    }

    private void UninstallBuildButtonListener()
    {
        if (!_buildButtonListenerInstalled || buildButton == null)
            return;

        buildButton.onClick.RemoveListener(OnBuildButtonClicked);
        _buildButtonListenerInstalled = false;
    }

    private Camera ResolveEventCamera()
    {
        Canvas canvas = ResolveCanvas();
        if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        return canvas.worldCamera;
    }

    private Canvas ResolveCanvas()
    {
        if (_cachedCanvas == null)
            _cachedCanvas = GetComponentInParent<Canvas>();
        return _cachedCanvas;
    }

    private static bool ContainsButton(Button button, Vector2 screenPosition, Camera eventCamera)
    {
        RectTransform rect = button != null && button.targetGraphic != null
            ? button.targetGraphic.rectTransform
            : button != null
                ? button.transform as RectTransform
                : null;

        return rect != null &&
               button.gameObject.activeInHierarchy &&
               RectTransformUtility.RectangleContainsScreenPoint(rect, screenPosition, eventCamera);
    }

}
