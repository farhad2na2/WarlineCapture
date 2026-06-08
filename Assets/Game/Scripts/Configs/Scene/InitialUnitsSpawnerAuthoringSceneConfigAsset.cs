using UnityEngine;

[CreateAssetMenu(menuName = "Game/Scene Config/Initial Units Spawner Authoring")]
public sealed class InitialUnitsSpawnerAuthoringSceneConfigAsset : InitialUnitsSpawnerAuthoringConfig
{
    [SerializeField] private GameObject sceneUnitSelectionMarkerPrefab;
    [SerializeField] private GameObject sceneUnitHealthBarPrefab;

    public override GameObject UnitSelectionMarkerPrefab =>
        sceneUnitSelectionMarkerPrefab != null ? sceneUnitSelectionMarkerPrefab : base.UnitSelectionMarkerPrefab;

    public override GameObject UnitHealthBarPrefab =>
        sceneUnitHealthBarPrefab != null ? sceneUnitHealthBarPrefab : base.UnitHealthBarPrefab;
}
