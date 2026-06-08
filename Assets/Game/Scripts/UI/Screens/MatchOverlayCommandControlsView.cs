using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public sealed class MatchOverlayCommandControlsView : MonoBehaviour
{
    private static readonly List<MatchOverlayCommandControlsView> RegisteredInstances = new();

    [SerializeField] private Button selectButton;
    [SerializeField] private Button moveButton;
    [SerializeField] private Button attackButton;
    [SerializeField] private Button scanButton;
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
    public Button ScanButton => scanButton;
    public Button BuildButton => buildButton;
    public Button HoldButton => holdButton;
    public Button StopButton => stopButton;
    public Button CommandWheelStopButton => commandWheelStopButton;
    public CommandWheelPanelSystem CommandWheelPanel => commandWheelPanel;
    public MatchOverlayCommandTabGroupView CommandTabGroup => commandTabGroup;
    public GameObject BuildDrawerPopupPrefab => buildDrawerPopupPrefab;
    public static IReadOnlyList<MatchOverlayCommandControlsView> Instances => RegisteredInstances;

    private void OnEnable()
    {
        if (!RegisteredInstances.Contains(this))
            RegisteredInstances.Add(this);
    }

    private void OnDisable()
    {
        RegisteredInstances.Remove(this);
    }
}
