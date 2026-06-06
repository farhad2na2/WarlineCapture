using UnityEngine;
using UnityEngine.UI;

public sealed class MatchOverlayCommandControlsView : MonoBehaviour
{
    [SerializeField] private Button selectButton;
    [SerializeField] private Button buildButton;
    [SerializeField] private Button holdButton;
    [SerializeField] private Button stopButton;
    [SerializeField] private Button commandWheelStopButton;
    [SerializeField] private CommandWheelPanelController commandWheelPanel;
    [SerializeField] private MatchOverlayCommandTabGroupView commandTabGroup;
    [SerializeField] private GameObject buildDrawerPopupPrefab;

    public Button SelectButton => selectButton;
    public Button BuildButton => buildButton;
    public Button HoldButton => holdButton;
    public Button StopButton => stopButton;
    public Button CommandWheelStopButton => commandWheelStopButton;
    public CommandWheelPanelController CommandWheelPanel => commandWheelPanel;
    public MatchOverlayCommandTabGroupView CommandTabGroup => commandTabGroup;
    public GameObject BuildDrawerPopupPrefab => buildDrawerPopupPrefab;
}
