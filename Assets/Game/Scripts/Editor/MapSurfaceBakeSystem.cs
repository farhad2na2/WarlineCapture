using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public readonly struct MapSurfaceBakeRequest
{
    public readonly float3 GridOrigin;
    public readonly float CellSize;
    public readonly int2 Dimensions;

    public MapSurfaceBakeRequest(float3 gridOrigin, float cellSize, int2 dimensions)
    {
        GridOrigin = gridOrigin;
        CellSize = cellSize;
        Dimensions = dimensions;
    }
}

public readonly struct MapSurfaceMeshBakeSource
{
    public readonly Mesh Mesh;
    public readonly Matrix4x4 LocalToWorld;
    public readonly MapSurfaceType SurfaceType;
    public readonly MapSurfaceFlags Flags;
    public readonly MapSurfaceMovementMask MovementMask;
    public readonly int LayerId;

    public MapSurfaceMeshBakeSource(
        Mesh mesh,
        Matrix4x4 localToWorld,
        MapSurfaceType surfaceType,
        MapSurfaceFlags flags,
        MapSurfaceMovementMask movementMask,
        int layerId)
    {
        Mesh = mesh;
        LocalToWorld = localToWorld;
        SurfaceType = surfaceType;
        Flags = flags;
        MovementMask = movementMask;
        LayerId = layerId;
    }
}

public sealed class MapSurfaceBakeSystem
{
    private const string MissingSurfaceReferenceError = "Match must have exactly one active MapSurfaceAuthoring with a baked MapSurfaceDataAsset reference.";
    private const string MultipleSurfaceReferencesError = "Match has multiple active MapSurfaceAuthoring baked surface references. Keep exactly one active map-surface data reference.";

    public bool TryValidateSingleActiveSurfaceReference(
        MapSurfaceAuthoring[] authorings,
        out MapSurfaceDataAsset surfaceData,
        out string error)
    {
        surfaceData = null;
        error = string.Empty;

        if (authorings == null)
        {
            error = MissingSurfaceReferenceError;
            return false;
        }

        for (int i = 0; i < authorings.Length; i++)
        {
            MapSurfaceAuthoring authoring = authorings[i];
            if (authoring == null || !authoring.isActiveAndEnabled || authoring.BakedSurfaceData == null)
                continue;

            if (surfaceData != null)
            {
                error = MultipleSurfaceReferencesError;
                surfaceData = null;
                return false;
            }

            surfaceData = authoring.BakedSurfaceData;
        }

        if (surfaceData != null)
            return true;

        error = MissingSurfaceReferenceError;
        return false;
    }

    public MapSurfaceDataAsset CreateFlatEquivalentDataAsset(MapSurfaceBakeRequest request)
    {
        if (request.CellSize <= 0f || request.Dimensions.x <= 0 || request.Dimensions.y <= 0)
            return null;

        var asset = ScriptableObject.CreateInstance<MapSurfaceDataAsset>();
        asset.ConfigureFlatEquivalent(
            new Vector3(request.GridOrigin.x, request.GridOrigin.y, request.GridOrigin.z),
            request.CellSize,
            new Vector2Int(request.Dimensions.x, request.Dimensions.y));
        return asset;
    }

    public bool TryBuildFlatEquivalent(
        MapSurfaceBakeRequest request,
        Allocator allocator,
        out BlobAssetReference<MapSurfaceBlob> surfaceBlob)
    {
        surfaceBlob = default;

        if (request.CellSize <= 0f || request.Dimensions.x <= 0 || request.Dimensions.y <= 0)
            return false;

        int cellCount = request.Dimensions.x * request.Dimensions.y;
        using var builder = new BlobBuilder(Allocator.Temp);
        var roadPrioritySystem = new MapSurfaceRoadPrioritySystem();
        ref MapSurfaceBlob root = ref builder.ConstructRoot<MapSurfaceBlob>();
        root.GridOrigin = request.GridOrigin;
        root.CellSize = request.CellSize;
        root.Dimensions = request.Dimensions;

        BlobBuilderArray<MapSurfaceCell> cells = builder.Allocate(ref root.Cells, cellCount);
        BlobBuilderArray<MapSurfaceSample> samples = builder.Allocate(ref root.Samples, cellCount);
        builder.Allocate(ref root.Connections, 0);

        for (int y = 0; y < request.Dimensions.y; y++)
        {
            for (int x = 0; x < request.Dimensions.x; x++)
            {
                int index = x + y * request.Dimensions.x;
                var cell = new int2(x, y);

                cells[index] = new MapSurfaceCell
                {
                    FirstSurfaceIndex = index,
                    SurfaceCount = 1,
                    InlineSurfaceIndex = (ushort)index
                };

                samples[index] = new MapSurfaceSample
                {
                    Cell = cell,
                    SurfaceId = index,
                    LayerId = 0,
                    Height = request.GridOrigin.y,
                    Normal = new float3(0f, 1f, 0f),
                    SlopeDegrees = 0f,
                    SurfaceType = MapSurfaceType.Terrain,
                    MovementMask = MapSurfaceMovementMask.AllGroundUnits |
                                   MapSurfaceMovementMask.AirGrounded |
                                   MapSurfaceMovementMask.BuildingPlacement,
                    Flags = roadPrioritySystem.NormalizeFlagsForSurfaceType(MapSurfaceType.Terrain, MapSurfaceFlags.None),
                    FirstConnectionIndex = 0,
                    ConnectionCount = 0
                };
            }
        }

        surfaceBlob = builder.CreateBlobAssetReference<MapSurfaceBlob>(allocator);
        return surfaceBlob.IsCreated;
    }

    public bool TryBuildSingleLayerTerrain(
        MapSurfaceBakeRequest request,
        MapSurfaceMeshBakeSource[] terrainSources,
        Allocator allocator,
        out BlobAssetReference<MapSurfaceBlob> surfaceBlob)
    {
        surfaceBlob = default;

        if (request.CellSize <= 0f || request.Dimensions.x <= 0 || request.Dimensions.y <= 0)
            return false;

        int cellCount = request.Dimensions.x * request.Dimensions.y;
        using var builder = new BlobBuilder(Allocator.Temp);
        var roadPrioritySystem = new MapSurfaceRoadPrioritySystem();
        ref MapSurfaceBlob root = ref builder.ConstructRoot<MapSurfaceBlob>();
        root.GridOrigin = request.GridOrigin;
        root.CellSize = request.CellSize;
        root.Dimensions = request.Dimensions;

        BlobBuilderArray<MapSurfaceCell> cells = builder.Allocate(ref root.Cells, cellCount);
        BlobBuilderArray<MapSurfaceSample> samples = builder.Allocate(ref root.Samples, cellCount);
        builder.Allocate(ref root.Connections, 0);

        for (int y = 0; y < request.Dimensions.y; y++)
        {
            for (int x = 0; x < request.Dimensions.x; x++)
            {
                int index = x + y * request.Dimensions.x;
                var cell = new int2(x, y);
                float3 worldCenter = request.GridOrigin + new float3(
                    (x + 0.5f) * request.CellSize,
                    0f,
                    (y + 0.5f) * request.CellSize);

                cells[index] = new MapSurfaceCell
                {
                    FirstSurfaceIndex = index,
                    SurfaceCount = 1,
                    InlineSurfaceIndex = (ushort)index
                };

                bool sampled = TrySampleHighestTerrain(
                    terrainSources,
                    new float2(worldCenter.x, worldCenter.z),
                    out float height,
                    out float3 normal,
                    out MapSurfaceType surfaceType,
                    out MapSurfaceFlags flags,
                    out MapSurfaceMovementMask movementMask,
                    out int layerId);

                if (!sampled)
                {
                    height = request.GridOrigin.y;
                    normal = new float3(0f, 1f, 0f);
                    surfaceType = MapSurfaceType.Terrain;
                    flags = MapSurfaceFlags.None;
                    movementMask = MapSurfaceMovementMask.AllGroundUnits |
                                   MapSurfaceMovementMask.AirGrounded |
                                   MapSurfaceMovementMask.BuildingPlacement;
                    layerId = 0;
                }

                samples[index] = new MapSurfaceSample
                {
                    Cell = cell,
                    SurfaceId = index,
                    LayerId = layerId,
                    Height = height,
                    Normal = math.normalizesafe(normal, new float3(0f, 1f, 0f)),
                    SlopeDegrees = CalculateSlopeDegrees(normal),
                    SurfaceType = surfaceType,
                    MovementMask = movementMask,
                    Flags = roadPrioritySystem.NormalizeFlagsForSurfaceType(surfaceType, flags),
                    FirstConnectionIndex = 0,
                    ConnectionCount = 0
                };
            }
        }

        surfaceBlob = builder.CreateBlobAssetReference<MapSurfaceBlob>(allocator);
        return surfaceBlob.IsCreated;
    }

    private static bool TrySampleHighestTerrain(
        MapSurfaceMeshBakeSource[] terrainSources,
        float2 sampleXZ,
        out float height,
        out float3 normal,
        out MapSurfaceType surfaceType,
        out MapSurfaceFlags flags,
        out MapSurfaceMovementMask movementMask,
        out int layerId)
    {
        height = 0f;
        normal = new float3(0f, 1f, 0f);
        surfaceType = MapSurfaceType.Terrain;
        flags = MapSurfaceFlags.None;
        movementMask = MapSurfaceMovementMask.AllGroundUnits |
                       MapSurfaceMovementMask.AirGrounded |
                       MapSurfaceMovementMask.BuildingPlacement;
        layerId = 0;

        if (terrainSources == null)
            return false;

        bool found = false;
        for (int sourceIndex = 0; sourceIndex < terrainSources.Length; sourceIndex++)
        {
            MapSurfaceMeshBakeSource source = terrainSources[sourceIndex];
            if (source.Mesh == null)
                continue;

            Vector3[] vertices = source.Mesh.vertices;
            int[] triangles = source.Mesh.triangles;
            for (int triangleIndex = 0; triangleIndex + 2 < triangles.Length; triangleIndex += 3)
            {
                float3 a = source.LocalToWorld.MultiplyPoint3x4(vertices[triangles[triangleIndex]]);
                float3 b = source.LocalToWorld.MultiplyPoint3x4(vertices[triangles[triangleIndex + 1]]);
                float3 c = source.LocalToWorld.MultiplyPoint3x4(vertices[triangles[triangleIndex + 2]]);

                if (!TrySampleTriangleHeight(sampleXZ, a, b, c, out float candidateHeight))
                    continue;

                if (found && candidateHeight <= height)
                    continue;

                found = true;
                height = candidateHeight;
                normal = math.normalizesafe(math.cross(b - a, c - a), new float3(0f, 1f, 0f));
                if (normal.y < 0f)
                    normal = -normal;
                surfaceType = source.SurfaceType;
                flags = source.Flags;
                movementMask = source.MovementMask;
                layerId = source.LayerId;
            }
        }

        return found;
    }

    private static bool TrySampleTriangleHeight(float2 sampleXZ, float3 a, float3 b, float3 c, out float height)
    {
        height = 0f;

        float2 a2 = new float2(a.x, a.z);
        float2 b2 = new float2(b.x, b.z);
        float2 c2 = new float2(c.x, c.z);
        float denominator = ((b2.y - c2.y) * (a2.x - c2.x)) +
                            ((c2.x - b2.x) * (a2.y - c2.y));
        if (math.abs(denominator) < 0.000001f)
            return false;

        float weightA = (((b2.y - c2.y) * (sampleXZ.x - c2.x)) +
                         ((c2.x - b2.x) * (sampleXZ.y - c2.y))) / denominator;
        float weightB = (((c2.y - a2.y) * (sampleXZ.x - c2.x)) +
                         ((a2.x - c2.x) * (sampleXZ.y - c2.y))) / denominator;
        float weightC = 1f - weightA - weightB;
        const float edgeTolerance = -0.0001f;
        if (weightA < edgeTolerance || weightB < edgeTolerance || weightC < edgeTolerance)
            return false;

        height = (weightA * a.y) + (weightB * b.y) + (weightC * c.y);
        return true;
    }

    private static float CalculateSlopeDegrees(float3 normal)
    {
        float3 safeNormal = math.normalizesafe(normal, new float3(0f, 1f, 0f));
        float upDot = math.saturate(math.abs(safeNormal.y));
        return math.degrees(math.acos(upDot));
    }
}
