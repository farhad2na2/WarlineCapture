using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public readonly struct MapSurfaceBakeRequest
{
    public readonly float3 GridOrigin;
    public readonly float CellSize;
    public readonly int2 Dimensions;
    public readonly int SamplesPerCellAxis;
    public readonly float MaxSampleHeightDelta;

    public MapSurfaceBakeRequest(
        float3 gridOrigin,
        float cellSize,
        int2 dimensions,
        int samplesPerCellAxis = 1,
        float maxSampleHeightDelta = 0.25f)
    {
        GridOrigin = gridOrigin;
        CellSize = cellSize;
        Dimensions = dimensions;
        SamplesPerCellAxis = math.max(1, samplesPerCellAxis);
        MaxSampleHeightDelta = math.max(0f, maxSampleHeightDelta);
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
    private const int SpatialBucketSizeInCells = 8;
    private const float BlockerBelowSurfaceIgnoreTolerance = 0.25f;
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
        var roadPrioritySystem = new MapSurfaceRoadPriorityPolicy();
        ref MapSurfaceBlob root = ref builder.ConstructRoot<MapSurfaceBlob>();
        root.GridOrigin = request.GridOrigin;
        root.CellSize = request.CellSize;
        root.Dimensions = request.Dimensions;
        root.RuntimeEncoding = MapSurfaceRuntimeEncoding.Full;

        BlobBuilderArray<MapSurfaceCell> cells = builder.Allocate(ref root.Cells, cellCount);
        BlobBuilderArray<MapSurfaceSample> samples = builder.Allocate(ref root.Samples, cellCount);
        builder.Allocate(ref root.Connections, 0);
        builder.Allocate(ref root.CompactSamples, 0);

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
        out BlobAssetReference<MapSurfaceBlob> surfaceBlob,
        Func<int, int, bool> shouldCancel = null)
    {
        surfaceBlob = default;

        if (request.CellSize <= 0f || request.Dimensions.x <= 0 || request.Dimensions.y <= 0)
            return false;

        SpatialTriangleIndex spatialIndex = SpatialTriangleIndex.Build(request, terrainSources);
        int cellCount = request.Dimensions.x * request.Dimensions.y;
        using var builder = new BlobBuilder(Allocator.Temp);
        var roadPrioritySystem = new MapSurfaceRoadPriorityPolicy();
        ref MapSurfaceBlob root = ref builder.ConstructRoot<MapSurfaceBlob>();
        root.GridOrigin = request.GridOrigin;
        root.CellSize = request.CellSize;
        root.Dimensions = request.Dimensions;
        root.RuntimeEncoding = MapSurfaceRuntimeEncoding.Full;

        BlobBuilderArray<MapSurfaceCell> cells = builder.Allocate(ref root.Cells, cellCount);
        BlobBuilderArray<MapSurfaceSample> samples = builder.Allocate(ref root.Samples, cellCount);
        builder.Allocate(ref root.Connections, 0);
        builder.Allocate(ref root.CompactSamples, 0);

        for (int y = 0; y < request.Dimensions.y; y++)
        {
            if (shouldCancel != null && (y & 7) == 0 && shouldCancel(y, request.Dimensions.y))
                return false;

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

                bool sampled = TrySampleCellGroundingSurface(
                    spatialIndex,
                    request,
                    cell,
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

                bool blockerCoversCell = TrySampleCellBlockerSurface(
                    spatialIndex,
                    request,
                    cell,
                    out float blockerHeight,
                    out _,
                    out _,
                    out _,
                    out _,
                    out _);
                bool blockerIsAtSurface = blockerCoversCell &&
                    blockerHeight >= height - BlockerBelowSurfaceIgnoreTolerance;
                if (blockerIsAtSurface && !IsRoadLikeSurface(surfaceType, flags))
                {
                    surfaceType = MapSurfaceType.Blocked;
                    flags = MapSurfaceFlags.None;
                    movementMask = MapSurfaceMovementMask.None;
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

    private static bool TrySampleCellGroundingSurface(
        SpatialTriangleIndex spatialIndex,
        MapSurfaceBakeRequest request,
        int2 cell,
        out float height,
        out float3 normal,
        out MapSurfaceType surfaceType,
        out MapSurfaceFlags flags,
        out MapSurfaceMovementMask movementMask,
        out int layerId)
    {
        return TrySampleCellSurface(
            spatialIndex,
            request,
            cell,
            includeBlocked: false,
            preferRoadLike: true,
            out height,
            out normal,
            out surfaceType,
            out flags,
            out movementMask,
            out layerId);
    }

    private static bool TrySampleCellBlockerSurface(
        SpatialTriangleIndex spatialIndex,
        MapSurfaceBakeRequest request,
        int2 cell,
        out float height,
        out float3 normal,
        out MapSurfaceType surfaceType,
        out MapSurfaceFlags flags,
        out MapSurfaceMovementMask movementMask,
        out int layerId)
    {
        return TrySampleCellSurface(
            spatialIndex,
            request,
            cell,
            includeBlocked: true,
            preferRoadLike: false,
            out height,
            out normal,
            out surfaceType,
            out flags,
            out movementMask,
            out layerId);
    }

    private static bool TrySampleCellSurface(
        SpatialTriangleIndex spatialIndex,
        MapSurfaceBakeRequest request,
        int2 cell,
        bool includeBlocked,
        bool preferRoadLike,
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
        movementMask = includeBlocked
            ? MapSurfaceMovementMask.None
            : MapSurfaceMovementMask.AllGroundUnits |
              MapSurfaceMovementMask.AirGrounded |
              MapSurfaceMovementMask.BuildingPlacement;
        layerId = 0;

        if (spatialIndex == null || request.CellSize <= 0f)
            return false;

        bool found = false;
        bool foundCenter = false;
        bool foundRoadLikeSample = false;
        float minHeight = float.MaxValue;
        float maxHeight = float.MinValue;
        SurfacePointSample centerSample = default;
        SurfacePointSample highestSample = default;
        SurfacePointSample highestRoadLikeSample = default;

        if (TrySampleSurfacePoint(
                spatialIndex,
                request,
                cell,
                new float2(0.5f, 0.5f),
                includeBlocked,
                preferRoadLike,
                out SurfacePointSample pointSample))
        {
            found = true;
            foundCenter = true;
            centerSample = pointSample;
            highestSample = pointSample;
            minHeight = pointSample.Height;
            maxHeight = pointSample.Height;
            if (preferRoadLike && IsRoadLikeSurface(pointSample.SurfaceType, pointSample.Flags))
            {
                foundRoadLikeSample = true;
                highestRoadLikeSample = pointSample;
            }
        }

        int samplesPerAxis = math.max(1, request.SamplesPerCellAxis);
        if (samplesPerAxis > 1)
        {
            for (int y = 0; y < samplesPerAxis; y++)
            {
                for (int x = 0; x < samplesPerAxis; x++)
                {
                    float2 normalized = new(
                        (x + 0.5f) / samplesPerAxis,
                        (y + 0.5f) / samplesPerAxis);
                    if (math.lengthsq(normalized - new float2(0.5f, 0.5f)) <= 0.000001f)
                        continue;

                    if (!TrySampleSurfacePoint(
                            spatialIndex,
                            request,
                            cell,
                            normalized,
                            includeBlocked,
                            preferRoadLike,
                            out pointSample))
                    {
                        continue;
                    }

                    if (!found || pointSample.Height > highestSample.Height)
                        highestSample = pointSample;
                    if (preferRoadLike && IsRoadLikeSurface(pointSample.SurfaceType, pointSample.Flags))
                    {
                        if (!foundRoadLikeSample || pointSample.Height > highestRoadLikeSample.Height)
                            highestRoadLikeSample = pointSample;
                        foundRoadLikeSample = true;
                    }
                    minHeight = found ? math.min(minHeight, pointSample.Height) : pointSample.Height;
                    maxHeight = found ? math.max(maxHeight, pointSample.Height) : pointSample.Height;
                    found = true;
                }
            }
        }

        if (!found)
            return false;

        SurfacePointSample selected = preferRoadLike && foundRoadLikeSample
            ? highestRoadLikeSample
            : !foundCenter || maxHeight - minHeight > request.MaxSampleHeightDelta
            ? highestSample
            : centerSample;
        height = selected.Height;
        normal = selected.Normal;
        surfaceType = selected.SurfaceType;
        flags = selected.Flags;
        movementMask = selected.MovementMask;
        layerId = selected.LayerId;
        return true;
    }

    private static bool TrySampleSurfacePoint(
        SpatialTriangleIndex spatialIndex,
        MapSurfaceBakeRequest request,
        int2 cell,
        float2 normalizedCellPoint,
        bool includeBlocked,
        bool preferRoadLike,
        out SurfacePointSample sample)
    {
        sample = default;
        float2 sampleXZ = new(
            request.GridOrigin.x + ((cell.x + normalizedCellPoint.x) * request.CellSize),
            request.GridOrigin.z + ((cell.y + normalizedCellPoint.y) * request.CellSize));
        float height;
        float3 normal;
        MapSurfaceType surfaceType;
        MapSurfaceFlags flags;
        MapSurfaceMovementMask movementMask;
        int layerId;
        bool sampled;
        if (includeBlocked)
        {
            sampled = TrySampleHighestSurface(
                spatialIndex,
                cell,
                sampleXZ,
                includeBlocked: true,
                preferRoadLike,
                out height,
                out normal,
                out surfaceType,
                out flags,
                out movementMask,
                out layerId);
        }
        else
        {
            sampled = TrySampleGroundingSurface(
                spatialIndex,
                cell,
                sampleXZ,
                out height,
                out normal,
                out surfaceType,
                out flags,
                out movementMask,
                out layerId);
        }

        if (!sampled)
            return false;

        sample = new SurfacePointSample(height, normal, surfaceType, flags, movementMask, layerId);
        return true;
    }

    private static bool TrySampleGroundingSurface(
        SpatialTriangleIndex spatialIndex,
        int2 cell,
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

        if (spatialIndex == null)
            return false;

        List<TriangleCandidate> candidates = spatialIndex.GetCandidates(cell);
        if (candidates == null || candidates.Count == 0)
            return false;

        bool found = false;
        bool foundRoadLike = false;
        for (int i = 0; i < candidates.Count; i++)
        {
            TriangleCandidate candidate = candidates[i];
            bool isBlocked = candidate.SurfaceType == MapSurfaceType.Blocked ||
                             candidate.MovementMask == MapSurfaceMovementMask.None;
            if (isBlocked)
                continue;

            if (!TrySampleTriangleHeight(sampleXZ, candidate.A, candidate.B, candidate.C, out float candidateHeight))
                continue;

            bool candidateRoadLike = IsRoadLikeSurface(candidate.SurfaceType, candidate.Flags);
            if (candidateRoadLike)
            {
                if (foundRoadLike && candidateHeight <= height)
                    continue;

                found = true;
                foundRoadLike = true;
            }
            else
            {
                if (foundRoadLike)
                    continue;
                if (found && candidateHeight <= height)
                    continue;

                found = true;
            }

            height = candidateHeight;
            normal = candidate.Normal;
            surfaceType = candidate.SurfaceType;
            flags = candidate.Flags;
            movementMask = candidate.MovementMask;
            layerId = candidate.LayerId;
        }

        return found;
    }

    private static bool TrySampleHighestSurface(
        SpatialTriangleIndex spatialIndex,
        int2 cell,
        float2 sampleXZ,
        bool includeBlocked,
        bool preferRoadLike,
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

        if (spatialIndex == null)
            return false;

        List<TriangleCandidate> candidates = spatialIndex.GetCandidates(cell);
        if (candidates == null || candidates.Count == 0)
            return false;

        bool found = false;
        bool foundRoadLike = false;
        for (int i = 0; i < candidates.Count; i++)
        {
            TriangleCandidate candidate = candidates[i];
            bool isBlocked = candidate.SurfaceType == MapSurfaceType.Blocked ||
                             candidate.MovementMask == MapSurfaceMovementMask.None;
            if (includeBlocked != isBlocked)
                continue;

            if (!TrySampleTriangleHeight(sampleXZ, candidate.A, candidate.B, candidate.C, out float candidateHeight))
                continue;

            bool candidateRoadLike = IsRoadLikeSurface(candidate.SurfaceType, candidate.Flags);
            if (preferRoadLike)
            {
                if (foundRoadLike && !candidateRoadLike)
                    continue;
                if (candidateRoadLike && !foundRoadLike)
                    found = false;
            }

            if (found && candidateHeight <= height)
                continue;

            found = true;
            foundRoadLike |= candidateRoadLike;
            height = candidateHeight;
            normal = candidate.Normal;
            surfaceType = candidate.SurfaceType;
            flags = candidate.Flags;
            movementMask = candidate.MovementMask;
            layerId = candidate.LayerId;
        }

        return found;
    }

    private static bool IsRoadLikeSurface(MapSurfaceType surfaceType, MapSurfaceFlags flags)
    {
        return surfaceType == MapSurfaceType.Road ||
               surfaceType == MapSurfaceType.DirtRoad ||
               surfaceType == MapSurfaceType.Highway ||
               surfaceType == MapSurfaceType.BridgeDeck ||
               surfaceType == MapSurfaceType.Ramp ||
               (flags & MapSurfaceFlags.Road) != 0;
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

    private sealed class SpatialTriangleIndex
    {
        private readonly int bucketWidth;
        private readonly int bucketHeight;
        private readonly List<TriangleCandidate>[] buckets;

        private SpatialTriangleIndex(int bucketWidth, int bucketHeight)
        {
            this.bucketWidth = math.max(1, bucketWidth);
            this.bucketHeight = math.max(1, bucketHeight);
            buckets = new List<TriangleCandidate>[this.bucketWidth * this.bucketHeight];
        }

        public static SpatialTriangleIndex Build(MapSurfaceBakeRequest request, MapSurfaceMeshBakeSource[] sources)
        {
            int bucketWidth = (request.Dimensions.x + SpatialBucketSizeInCells - 1) / SpatialBucketSizeInCells;
            int bucketHeight = (request.Dimensions.y + SpatialBucketSizeInCells - 1) / SpatialBucketSizeInCells;
            SpatialTriangleIndex index = new(bucketWidth, bucketHeight);
            if (sources == null)
                return index;

            for (int sourceIndex = 0; sourceIndex < sources.Length; sourceIndex++)
            {
                MapSurfaceMeshBakeSource source = sources[sourceIndex];
                if (source.Mesh == null)
                    continue;

                Vector3[] vertices = source.Mesh.vertices;
                int[] triangles = source.Mesh.triangles;
                for (int triangleIndex = 0; triangleIndex + 2 < triangles.Length; triangleIndex += 3)
                {
                    float3 a = source.LocalToWorld.MultiplyPoint3x4(vertices[triangles[triangleIndex]]);
                    float3 b = source.LocalToWorld.MultiplyPoint3x4(vertices[triangles[triangleIndex + 1]]);
                    float3 c = source.LocalToWorld.MultiplyPoint3x4(vertices[triangles[triangleIndex + 2]]);

                    if (!TryGetTriangleCellRange(request, a, b, c, out int2 minCell, out int2 maxCell))
                        continue;

                    float3 normal = math.normalizesafe(math.cross(b - a, c - a), new float3(0f, 1f, 0f));
                    if (normal.y < 0f)
                        normal = -normal;

                    TriangleCandidate candidate = new(
                        a,
                        b,
                        c,
                        normal,
                        source.SurfaceType,
                        source.Flags,
                        source.MovementMask,
                        source.LayerId);

                    index.Add(candidate, minCell, maxCell);
                }
            }

            return index;
        }

        public List<TriangleCandidate> GetCandidates(int2 cell)
        {
            int bucketX = math.clamp(cell.x / SpatialBucketSizeInCells, 0, bucketWidth - 1);
            int bucketY = math.clamp(cell.y / SpatialBucketSizeInCells, 0, bucketHeight - 1);
            return buckets[bucketX + bucketY * bucketWidth];
        }

        private void Add(TriangleCandidate candidate, int2 minCell, int2 maxCell)
        {
            int minBucketX = math.clamp(minCell.x / SpatialBucketSizeInCells, 0, bucketWidth - 1);
            int maxBucketX = math.clamp(maxCell.x / SpatialBucketSizeInCells, 0, bucketWidth - 1);
            int minBucketY = math.clamp(minCell.y / SpatialBucketSizeInCells, 0, bucketHeight - 1);
            int maxBucketY = math.clamp(maxCell.y / SpatialBucketSizeInCells, 0, bucketHeight - 1);

            for (int y = minBucketY; y <= maxBucketY; y++)
            {
                for (int x = minBucketX; x <= maxBucketX; x++)
                {
                    int index = x + y * bucketWidth;
                    buckets[index] ??= new List<TriangleCandidate>(8);
                    buckets[index].Add(candidate);
                }
            }
        }
    }

    private readonly struct TriangleCandidate
    {
        public readonly float3 A;
        public readonly float3 B;
        public readonly float3 C;
        public readonly float3 Normal;
        public readonly MapSurfaceType SurfaceType;
        public readonly MapSurfaceFlags Flags;
        public readonly MapSurfaceMovementMask MovementMask;
        public readonly int LayerId;

        public TriangleCandidate(
            float3 a,
            float3 b,
            float3 c,
            float3 normal,
            MapSurfaceType surfaceType,
            MapSurfaceFlags flags,
            MapSurfaceMovementMask movementMask,
            int layerId)
        {
            A = a;
            B = b;
            C = c;
            Normal = normal;
            SurfaceType = surfaceType;
            Flags = flags;
            MovementMask = movementMask;
            LayerId = layerId;
        }
    }

    private readonly struct SurfacePointSample
    {
        public readonly float Height;
        public readonly float3 Normal;
        public readonly MapSurfaceType SurfaceType;
        public readonly MapSurfaceFlags Flags;
        public readonly MapSurfaceMovementMask MovementMask;
        public readonly int LayerId;

        public SurfacePointSample(
            float height,
            float3 normal,
            MapSurfaceType surfaceType,
            MapSurfaceFlags flags,
            MapSurfaceMovementMask movementMask,
            int layerId)
        {
            Height = height;
            Normal = normal;
            SurfaceType = surfaceType;
            Flags = flags;
            MovementMask = movementMask;
            LayerId = layerId;
        }
    }

    private static bool TryGetTriangleCellRange(
        MapSurfaceBakeRequest request,
        float3 a,
        float3 b,
        float3 c,
        out int2 minCell,
        out int2 maxCell)
    {
        float minX = math.min(a.x, math.min(b.x, c.x));
        float maxX = math.max(a.x, math.max(b.x, c.x));
        float minZ = math.min(a.z, math.min(b.z, c.z));
        float maxZ = math.max(a.z, math.max(b.z, c.z));

        int minCellX = (int)math.floor((minX - request.GridOrigin.x) / request.CellSize);
        int maxCellX = (int)math.floor((maxX - request.GridOrigin.x) / request.CellSize);
        int minCellY = (int)math.floor((minZ - request.GridOrigin.z) / request.CellSize);
        int maxCellY = (int)math.floor((maxZ - request.GridOrigin.z) / request.CellSize);

        if (maxCellX < 0 ||
            maxCellY < 0 ||
            minCellX >= request.Dimensions.x ||
            minCellY >= request.Dimensions.y)
        {
            minCell = default;
            maxCell = default;
            return false;
        }

        minCell = new int2(
            math.clamp(minCellX, 0, request.Dimensions.x - 1),
            math.clamp(minCellY, 0, request.Dimensions.y - 1));
        maxCell = new int2(
            math.clamp(maxCellX, 0, request.Dimensions.x - 1),
            math.clamp(maxCellY, 0, request.Dimensions.y - 1));
        return true;
    }
}
