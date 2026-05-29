using UnityEngine;

[DisallowMultipleComponent]
public sealed class MapSurfaceAuthoring : MonoBehaviour
{
    [SerializeField] private MapSurfaceDataAsset bakedSurfaceData;
    [SerializeField] private GridAuthoring gridAuthoring;
    [SerializeField] private Transform terrainRoot;
    [SerializeField] private Transform roadRoot;
    [SerializeField] private Transform bridgeRoot;
    [SerializeField] private Transform rampRoot;
    [SerializeField, Min(1)] private int samplesPerCellAxis = 2;
    [SerializeField, Min(0.01f)] private float maxSampleHeightDelta = 0.25f;
    [SerializeField, Min(0f)] private float maxBuildingSlopeDegrees = 8f;
    [SerializeField, Min(0f)] private float maxInfantrySlopeDegrees = 35f;
    [SerializeField, Min(0f)] private float maxVehicleSlopeDegrees = 22f;

    public MapSurfaceDataAsset BakedSurfaceData => bakedSurfaceData;
    public GridAuthoring GridAuthoring => gridAuthoring;
    public Transform TerrainRoot => terrainRoot;
    public Transform RoadRoot => roadRoot;
    public Transform BridgeRoot => bridgeRoot;
    public Transform RampRoot => rampRoot;
    public int SamplesPerCellAxis => samplesPerCellAxis;
    public float MaxSampleHeightDelta => maxSampleHeightDelta;
    public float MaxBuildingSlopeDegrees => maxBuildingSlopeDegrees;
    public float MaxInfantrySlopeDegrees => maxInfantrySlopeDegrees;
    public float MaxVehicleSlopeDegrees => maxVehicleSlopeDegrees;
}
