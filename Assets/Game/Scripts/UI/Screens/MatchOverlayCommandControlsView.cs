using UnityEngine;
using UnityEngine.UI;

public sealed class MatchOverlayCommandControlsView : MonoBehaviour
{
    [SerializeField] private Button selectButton;
    [SerializeField] private Button moveButton;
    [SerializeField] private Button attackButton;
    [SerializeField] private Button buildButton;
    [SerializeField] private Button holdButton;
    [SerializeField] private Button stopButton;
    [SerializeField] private Button commandWheelStopButton;
    [SerializeField] private CommandWheelPanelSystem commandWheelPanel;
    [SerializeField] private MatchOverlayCommandTabGroupView commandTabGroup;
    [SerializeField] private GameObject buildDrawerPopupPrefab;

    public Button SelectButton => selectButton;
    public Button MoveButton => moveButton;
    public Button AttackButton => attackButton;
    public Button BuildButton => buildButton;
    public Button HoldButton => holdButton;
    public Button StopButton => stopButton;
    public Button CommandWheelStopButton => commandWheelStopButton;
    public CommandWheelPanelSystem CommandWheelPanel => commandWheelPanel;
    public MatchOverlayCommandTabGroupView CommandTabGroup => commandTabGroup;
    public GameObject BuildDrawerPopupPrefab => buildDrawerPopupPrefab;
}
