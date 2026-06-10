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

    private Camera ResolveEventCamera()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        return canvas.worldCamera;
    }

    private static bool ContainsButton(Button button, Vector2 screenPosition, Camera eventCamera)
    {
        RectTransform rect = button != null ? button.transform as RectTransform : null;
        return rect != null &&
               button.gameObject.activeInHierarchy &&
               RectTransformUtility.RectangleContainsScreenPoint(rect, screenPosition, eventCamera);
    }
}
