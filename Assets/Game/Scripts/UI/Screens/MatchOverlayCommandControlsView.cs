using UnityEngine;
using UnityEngine.UI;

public sealed class MatchOverlayCommandControlsView : MonoBehaviour
{
    [SerializeField] private Button selectButton;
    [SerializeField] private Button holdButton;
    [SerializeField] private Button stopButton;
    [SerializeField] private Button commandWheelStopButton;
    [SerializeField] private CommandWheelPanelController commandWheelPanel;

    public Button SelectButton => selectButton;
    public Button HoldButton => holdButton;
    public Button StopButton => stopButton;
    public Button CommandWheelStopButton => commandWheelStopButton;
    public CommandWheelPanelController CommandWheelPanel => commandWheelPanel;
}
