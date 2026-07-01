using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

internal sealed class RoadSurfacePlacementUtilitySystemHelper
{
    private const float MaxRoadSurfaceHeightDelta = 0.35f;
    private const float MaxRoadSurfaceSlopeDegrees = 35f;

    private MapSurfaceComponent _surface;
    private bool _hasSurface;

    public readonly struct Result
    {
        public readonly bool IsValid;
        public readonly float MaxHeightDelta;
        public readonly float MaxSlopeDegrees;
        public readonly int SurfaceId;
        public readonly int LayerId;
        public readonly MapSurfaceType SurfaceType;
        public readonly int SampleCount;

        public Result(
            bool isValid,
            float maxHeightDelta,
            float maxSlopeDegrees,
            int surfaceId,
            int layerId,
            MapSurfaceType surfaceType,
            int sampleCount)
        {
            IsValid = isValid;
            MaxHeightDelta = maxHeightDelta;
            MaxSlopeDegrees = maxSlopeDegrees;
            SurfaceId = surfaceId;
            LayerId = layerId;
            SurfaceType = surfaceType;
            SampleCount = sampleCount;
        }
    }

    public void Configure(MapSurfaceComponent surface)
    {
        _surface = surface;
        _hasSurface = surface.HasSurfaceData != 0 && surface.SurfaceBlob.IsCreated;
    }

    public void Clear()
    {
        _surface = default;
        _hasSurface = false;
    }

    public bool IsPathSurfaceValid(List<Vector2Int> cells)
    {
        if (!_hasSurface)
            return true;

        return TryEvaluatePath(_surface, cells, out Result result) && result.IsValid;
    }

    public bool TryEvaluatePath(MapSurfaceComponent surface, IReadOnlyList<Vector2Int> cells, out Result result)
    {
        result = default;
        if (cells == null || cells.Count == 0)
            return false;
        if (surface.HasSurfaceData == 0 || !surface.SurfaceBlob.IsCreated)
            return false;

        float minHeight = float.MaxValue;
        float maxHeight = float.MinValue;
        float maxSlope = 0f;
        int surfaceId = -1;
        int layerId = 0;
        MapSurfaceType surfaceType = MapSurfaceType.Road;
        int sampleCount = 0;

        for (int i = 0; i < cells.Count; i++)
        {
            Vector2Int cell = cells[i];
            if (!TryGetPrimarySample(surface, new int2(cell.x, cell.y), out MapSurfaceSample sample))
                return false;

            if (surfaceId < 0)
            {
                surfaceId = sample.SurfaceId;
                layerId = sample.LayerId;
                surfaceType = sample.SurfaceType;
            }
            else if (sample.LayerId != layerId)
            {
                result = new Result(false, maxHeight - minHeight, maxSlope, surfaceId, layerId, surfaceType, sampleCount);
                return true;
            }

            if ((sample.MovementMask & MapSurfaceMovementMask.AllGroundUnits) == 0)
            {
                result = new Result(false, maxHeight - minHeight, maxSlope, surfaceId, layerId, surfaceType, sampleCount);
                return true;
            }

            minHeight = math.min(minHeight, sample.Height);
            maxHeight = math.max(maxHeight, sample.Height);
            maxSlope = math.max(maxSlope, sample.SlopeDegrees);
            sampleCount++;
        }

        float heightDelta = maxHeight - minHeight;
        bool valid = heightDelta <= MaxRoadSurfaceHeightDelta &&
            maxSlope <= MaxRoadSurfaceSlopeDegrees;
        result = new Result(valid, heightDelta, maxSlope, surfaceId, layerId, surfaceType, sampleCount);
        return true;
    }

    public MapSurfaceType ResolveRoadSurfaceType(bool isAutobahn, bool isBridgeDeck, bool isRamp)
    {
        if (isBridgeDeck)
            return MapSurfaceType.BridgeDeck;
        if (isRamp)
            return MapSurfaceType.Ramp;
        return isAutobahn ? MapSurfaceType.Highway : MapSurfaceType.Road;
    }

    private bool TryGetPrimarySample(MapSurfaceComponent surface, int2 cell, out MapSurfaceSample sample)
    {
        sample = default;
        if ((uint)cell.x >= (uint)surface.Dimensions.x ||
            (uint)cell.y >= (uint)surface.Dimensions.y)
        {
            return false;
        }

        ref MapSurfaceBlob blob = ref surface.SurfaceBlob.Value;
        return MapSurfaceBlobAccess.TryGetPrimarySurface(ref blob, cell, out sample);
    }
}
