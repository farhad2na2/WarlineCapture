using System.Collections.Generic;
using UnityEngine;

internal sealed class BuildingDefinition
{
    public sealed class ProductionSlotDefinition
    {
        public GameObject SpawnUnitPrefab;
    }

    public string DisplayName;
    public string Description;
    public int MaxHealth;
    public List<ProductionSlotDefinition> ProductionSlots;
    public GameObject SpawnUnitPrefab;
    public GameObject SecondarySpawnUnitPrefab;
    public GameObject TertiarySpawnUnitPrefab;
    public GameObject QuaternarySpawnUnitPrefab;
    public GameObject Prefab;
    public Vector2Int FootprintCells;
    public BuildingRole Role;
    public bool IsWall;
    public float OilBarrelsPerDay;
    public int OilStorageCapacity;
    public float FuelBarrelsPerDay;
    public int FuelStorageCapacity;
    public int RefugeeCapacity;
    public int RefugeeUpkeepPerCitizenPerDay;
    public ThreatDetectionKind ThreatDetectionKind;
    public int ThreatDetectionRadiusCells;
    public Bounds LocalBounds;
    public bool HasLocalBounds;
    public GameObject VisualTemplate;
    public List<Mesh> GeneratedMeshes;
    public Vector3[] ProductionSpawnLocalPositions;
    public bool HasRunway;
    public Vector3 RunwayLocalPosition;
    public Quaternion RunwayLocalRotation;
    public Vector3 RunwayHalfExtents;
}
