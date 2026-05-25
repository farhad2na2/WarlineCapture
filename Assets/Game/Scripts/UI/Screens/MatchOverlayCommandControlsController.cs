using UnityEngine;
using UnityEngine.UI;

public sealed class MatchOverlayCommandControlsController : MonoBehaviour
{
    [SerializeField] private Button holdButton;
    [SerializeField] private Button stopButton;
    [SerializeField] private Button commandWheelStopButton;
    [SerializeField] private CommandWheelPanelController commandWheelPanel;
    private SelectionUiCommandSystem _selectionUiCommandSystem;

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

    public void SetSelectionUiCommandSystemForTests(SelectionUiCommandSystem selectionUiCommandSystem)
    {
        BindDependencies(selectionUiCommandSystem);
    }

    public void BindDependencies(SelectionUiCommandSystem selectionUiCommandSystem)
    {
        _selectionUiCommandSystem = selectionUiCommandSystem;
    }

    public bool IssueHoldCommand()
    {
        return _selectionUiCommandSystem != null && _selectionUiCommandSystem.RequestHoldPosition();
    }

    public bool IssueStopCommand()
    {
        return _selectionUiCommandSystem != null && _selectionUiCommandSystem.RequestStop();
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

}
