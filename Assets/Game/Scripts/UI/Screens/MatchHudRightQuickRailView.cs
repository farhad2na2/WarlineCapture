using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class MatchHudRightQuickRailView : MonoBehaviour
{
    [SerializeField] private Button buildButton;

    private Action _buildCommandClicked;
    private SelectionUiCommandSystem _selectionUiCommandSystem;
    private UIShellContentView _shellContentView;
    private bool _buildButtonListenerInstalled;

    public Button BuildButton => buildButton;

    private void OnEnable()
    {
        InstallBuildButtonListener();
        BindToParentShellContentIfNeeded();
        ClearBuildButtonSelection();
    }

    private void OnDisable()
    {
        UninstallBuildButtonListener();
    }

    public void BindBuildCommand(Action buildCommandClicked, SelectionUiCommandSystem selectionUiCommandSystem)
    {
        _buildCommandClicked = buildCommandClicked;
        _selectionUiCommandSystem = selectionUiCommandSystem;
        InstallBuildButtonListener();
        ClearBuildButtonSelection();
    }

    public void UnbindBuildCommand()
    {
        _buildCommandClicked = null;
        _selectionUiCommandSystem = null;
    }

    public bool ContainsScreenPoint(Vector2 screenPosition)
    {
        Camera eventCamera = ResolveEventCamera();
        if (ContainsButton(buildButton, screenPosition, eventCamera))
            return true;

        RectTransform root = transform as RectTransform;
        return root != null &&
               RectTransformUtility.RectangleContainsScreenPoint(root, screenPosition, eventCamera);
    }

    private void OnBuildButtonClicked()
    {
        TriggerBuildCommand();
    }

    private void TriggerBuildCommand()
    {
        BindToParentShellContentIfNeeded();
        _selectionUiCommandSystem?.CaptureUiClickSequence();

        if (_buildCommandClicked != null)
        {
            _buildCommandClicked.Invoke();
            return;
        }

        Debug.LogWarning("Build drawer command clicked before the right quick rail was bound to the shell.");
    }

    private void BindToParentShellContentIfNeeded()
    {
        if (_buildCommandClicked != null)
            return;

        if (_shellContentView == null)
            _shellContentView = GetComponentInParent<UIShellContentView>();

        _shellContentView?.TryBindMatchHudRightQuickRailView(this);
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
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        return canvas.worldCamera;
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
