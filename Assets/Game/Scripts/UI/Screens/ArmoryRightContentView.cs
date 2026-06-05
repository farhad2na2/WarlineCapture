using UnityEngine;

[DisallowMultipleComponent]
public sealed class ArmoryRightContentView : MonoBehaviour
{
    [SerializeField] private ArmoryInspectionPanelView inspectionPanel;

    public ArmoryInspectionPanelView InspectionPanel => inspectionPanel;
}
