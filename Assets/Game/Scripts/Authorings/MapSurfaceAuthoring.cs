using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class MapSurfaceAuthoring : MonoBehaviour
{
    [SerializeField] private MapSurfaceDataAsset bakedSurfaceData;
    [SerializeField] private GridAuthoringConfig gridConfig;
    [SerializeField, Min(1)] private int samplesPerCellAxis = 2;
    [SerializeField, Min(0.01f)] private float maxSampleHeightDelta = 0.25f;
    [SerializeField, Min(0f)] private float maxBuildingSlopeDegrees = 8f;
    [SerializeField, Min(0f)] private float maxInfantrySlopeDegrees = 35f;
    [SerializeField, Min(0f)] private float maxVehicleSlopeDegrees = 22f;

    public MapSurfaceDataAsset BakedSurfaceData => bakedSurfaceData;
    public GridAuthoringConfig GridConfig => gridConfig;
    public int SamplesPerCellAxis => samplesPerCellAxis;
    public float MaxSampleHeightDelta => maxSampleHeightDelta;
    public float MaxBuildingSlopeDegrees => maxBuildingSlopeDegrees;
    public float MaxInfantrySlopeDegrees => maxInfantrySlopeDegrees;
    public float MaxVehicleSlopeDegrees => maxVehicleSlopeDegrees;

    private sealed class Baker : Baker<MapSurfaceAuthoring>
    {
        public override void Bake(MapSurfaceAuthoring authoring)
        {
            MapSurfaceDataAsset surfaceData = authoring.BakedSurfaceData;
            if (surfaceData == null ||
                !surfaceData.TryCreateRuntimeBlobAsset(Allocator.Persistent, out BlobAssetReference<MapSurfaceBlob> surfaceBlob))
                return;

            AddBlobAsset(ref surfaceBlob, out var _);

            Entity entity = GetEntity(TransformUsageFlags.None);
            ref MapSurfaceBlob blob = ref surfaceBlob.Value;
            GetSurfaceFeatureFlags(ref blob, out byte hasLayeredCells, out byte hasRoadSurfaces, out byte hasBridgeSurfaces);

            AddComponent(entity, new MapSurfaceComponent
            {
                SurfaceBlob = surfaceBlob,
                GridOrigin = blob.GridOrigin,
                CellSize = blob.CellSize,
                Dimensions = blob.Dimensions,
                HasSurfaceData = 1,
                HasLayeredCells = hasLayeredCells,
                HasRoadSurfaces = hasRoadSurfaces,
                HasBridgeSurfaces = hasBridgeSurfaces
            });
            AddComponent(entity, new MapSurfacePathCostComponent
            {
                EnableSlopeCost = 0,
                GentleSlopeTraversalCost = 0,
                SteepSlopeTraversalCost = 0
            });
        }

        private static void GetSurfaceFeatureFlags(
            ref MapSurfaceBlob blob,
            out byte hasLayeredCells,
            out byte hasRoadSurfaces,
            out byte hasBridgeSurfaces)
        {
            hasLayeredCells = 0;
            hasRoadSurfaces = 0;
            hasBridgeSurfaces = 0;

            for (int i = 0; i < blob.Cells.Length; i++)
            {
                if (blob.Cells[i].SurfaceCount > 1)
                {
                    hasLayeredCells = 1;
                    break;
                }
            }

            for (int i = 0; i < blob.Samples.Length; i++)
            {
                MapSurfaceSample sample = blob.Samples[i];
                if (sample.SurfaceType == MapSurfaceType.Road ||
                    sample.SurfaceType == MapSurfaceType.DirtRoad ||
                    sample.SurfaceType == MapSurfaceType.Highway ||
                    sample.SurfaceType == MapSurfaceType.BridgeDeck ||
                    sample.SurfaceType == MapSurfaceType.Ramp ||
                    (sample.Flags & MapSurfaceFlags.Road) != 0)
                {
                    hasRoadSurfaces = 1;
                }

                if (sample.SurfaceType == MapSurfaceType.BridgeDeck ||
                    (sample.Flags & MapSurfaceFlags.Bridge) != 0)
                {
                    hasBridgeSurfaces = 1;
                }

                if (hasRoadSurfaces != 0 && hasBridgeSurfaces != 0)
                    break;
            }
        }
    }
}
