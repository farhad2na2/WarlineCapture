using UnityEngine;

public sealed class BattleScenarioLabSceneReferences : MonoBehaviour
{
    [SerializeField] private BattleScenarioDefinition scenarioDefinition;
    [SerializeField] private Camera scenarioCamera;
    [SerializeField] private Transform launcherMarker;
    [SerializeField] private Transform radarMarker;
    [SerializeField] private Transform incomingThreatStartMarker;
    [SerializeField] private Transform defendedTargetMarker;

    public BattleScenarioDefinition ScenarioDefinition => scenarioDefinition;
    public Camera ScenarioCamera => scenarioCamera;
    public Transform LauncherMarker => launcherMarker;
    public Transform RadarMarker => radarMarker;
    public Transform IncomingThreatStartMarker => incomingThreatStartMarker;
    public Transform DefendedTargetMarker => defendedTargetMarker;
}
