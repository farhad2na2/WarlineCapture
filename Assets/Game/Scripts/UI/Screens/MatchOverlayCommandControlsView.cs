using UnityEngine;
using UnityEngine.UI;

public sealed class MatchOverlayCommandControlsView : MonoBehaviour
{
    [SerializeField] private Button selectButton;
    [SerializeField] private Button moveButton;
    [SerializeField] private Button attackButton;
    [SerializeField] private Button scanButton;
    [SerializeField] private Button buildButton;
    [SerializeField] private Button holdButton;
    [SerializeField] private Button stopButton;
    [SerializeField] private Button commandWheelStopButton;
    [SerializeField] private CommandWheelPanelView commandWheelPanel;
    [SerializeField] private MatchOverlayCommandTabGroupView commandTabGroup;

    private Canvas _cachedCanvas;

    public Button SelectButton => selectButton;
    public Button MoveButton => moveButton;
    public Button AttackButton => attackButton;
    public Button ScanButton => scanButton;
    public Button BuildButton => buildButton;
    public Button HoldButton => holdButton;
    public Button StopButton => stopButton;
    public Button CommandWheelStopButton => commandWheelStopButton;
    public CommandWheelPanelView CommandWheelPanel => commandWheelPanel;
    public MatchOverlayCommandTabGroupView CommandTabGroup => commandTabGroup;

    private void OnTransformParentChanged()
    {
        _cachedCanvas = null;
    }

    public bool ContainsScreenPoint(Vector2 screenPosition)
    {
        Camera eventCamera = ResolveEventCamera();
        RectTransform root = transform as RectTransform;
        if (root != null && RectTransformUtility.RectangleContainsScreenPoint(root, screenPosition, eventCamera))
            return true;

        return ContainsButton(selectButton, screenPosition, eventCamera) ||
               ContainsButton(moveButton, screenPosition, eventCamera) ||
               ContainsButton(attackButton, screenPosition, eventCamera) ||
               ContainsButton(scanButton, screenPosition, eventCamera) ||
               ContainsButton(buildButton, screenPosition, eventCamera) ||
               ContainsButton(holdButton, screenPosition, eventCamera) ||
               ContainsButton(stopButton, screenPosition, eventCamera) ||
               ContainsButton(commandWheelStopButton, screenPosition, eventCamera);
    }

    public string DescribeScreenPointHit(Vector2 screenPosition)
    {
        Camera eventCamera = ResolveEventCamera();
        if (ContainsButton(selectButton, screenPosition, eventCamera))
            return "SelectCommand";
        if (ContainsButton(moveButton, screenPosition, eventCamera))
            return "MoveCommand";
        if (ContainsButton(attackButton, screenPosition, eventCamera))
            return "AttackCommand";
        if (ContainsButton(scanButton, screenPosition, eventCamera))
            return "ScanCommand";
        if (ContainsButton(buildButton, screenPosition, eventCamera))
            return "BuildCommand";
        if (ContainsButton(holdButton, screenPosition, eventCamera))
            return "HoldCommand";
        if (ContainsButton(stopButton, screenPosition, eventCamera))
            return "StopCommand";
        if (ContainsButton(commandWheelStopButton, screenPosition, eventCamera))
            return "CommandWheelStop";

        RectTransform root = transform as RectTransform;
        return root != null && RectTransformUtility.RectangleContainsScreenPoint(root, screenPosition, eventCamera)
            ? "CommandControlsRoot"
            : "None";
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
        RectTransform rect = button != null ? button.transform as RectTransform : null;
        return rect != null &&
               button.gameObject.activeInHierarchy &&
               RectTransformUtility.RectangleContainsScreenPoint(rect, screenPosition, eventCamera);
    }
}
