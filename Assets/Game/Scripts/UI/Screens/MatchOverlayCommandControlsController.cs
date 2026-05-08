using UnityEngine;
using UnityEngine.UI;

public sealed class MatchOverlayCommandControlsController : MonoBehaviour
{
    [SerializeField] private Button holdButton;
    [SerializeField] private Button stopButton;
    [SerializeField] private Button commandWheelStopButton;
    [SerializeField] private CommandWheelPanelController commandWheelPanel;
    private RTSSelectionSystem _selectionSystemOverride;

    public Button HoldButton => holdButton;
    public Button StopButton => stopButton;
    public Button CommandWheelStopButton => commandWheelStopButton;

    private void Awake()
    {
        if (holdButton != null)
            holdButton.onClick.AddListener(OnHoldButtonClicked);

        if (stopButton != null)
            stopButton.onClick.AddListener(OnStopButtonClicked);

        if (commandWheelStopButton != null)
            commandWheelStopButton.onClick.AddListener(IssueCommandWheelStopCommand);
    }

    private void OnDestroy()
    {
        if (holdButton != null)
            holdButton.onClick.RemoveListener(OnHoldButtonClicked);

        if (stopButton != null)
            stopButton.onClick.RemoveListener(OnStopButtonClicked);

        if (commandWheelStopButton != null)
            commandWheelStopButton.onClick.RemoveListener(IssueCommandWheelStopCommand);
    }

    public void SetSelectionSystemForTests(RTSSelectionSystem selectionSystem)
    {
        _selectionSystemOverride = selectionSystem;
    }

    public bool IssueHoldCommand()
    {
        RTSSelectionSystem selectionSystem = ResolveSelectionSystem();
        return selectionSystem != null && selectionSystem.IssueHoldPositionOrder();
    }

    public bool IssueStopCommand()
    {
        RTSSelectionSystem selectionSystem = ResolveSelectionSystem();
        return selectionSystem != null && selectionSystem.IssueStopOrder();
    }

    private void OnHoldButtonClicked()
    {
        IssueHoldCommand();
    }

    private void OnStopButtonClicked()
    {
        IssueStopCommand();
    }

    private void IssueCommandWheelStopCommand()
    {
        if (commandWheelPanel != null)
            commandWheelPanel.Close();

        IssueStopCommand();
    }

    private RTSSelectionSystem ResolveSelectionSystem()
    {
        return _selectionSystemOverride ?? RTSSelectionSystem.Instance;
    }
}
