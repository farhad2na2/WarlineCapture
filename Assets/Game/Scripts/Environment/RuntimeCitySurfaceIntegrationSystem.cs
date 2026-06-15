using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

internal sealed partial class RuntimeCitySurfaceIntegrationSystem : SystemBase
{
    private const float RuntimeCityMaxSurfaceHeightDelta = 0.5f;
    private const float RuntimeCityMaxSurfaceSlopeDegrees = 45f;

    private readonly BuildingSurfacePlacementSystem _buildingSurfacePlacementSystem = new();
    private readonly RoadSurfacePlacementSystem _roadSurfacePlacementSystem = new();
    private MapSurfaceComponent _surface;
    private bool _hasSurface;

    protected override void OnCreate()
    {
        Enabled = false;
    }

    protected override void OnUpdate()
    {
    }

    protected override void OnDestroy()
    {
        Clear();
    }

    public void Configure(MapSurfaceComponent surface)
    {
        _surface = surface;
        _hasSurface = surface.HasSurfaceData != 0 && surface.SurfaceBlob.IsCreated;
        if (_hasSurface)
            _roadSurfacePlacementSystem.Configure(surface);
        else
            _roadSurfacePlacementSystem.Clear();
    }

    public void Clear()
    {
        _surface = default;
        _hasSurface = false;
        _roadSurfacePlacementSystem.Clear();
    }

    public Vector3 ResolveFootprintCenter(
        Vector2Int originCell,
        Vector2Int footprintCells,
        GridConfig grid,
        Vector3 fallbackCenter)
    {
        if (!_hasSurface ||
            !_buildingSurfacePlacementSystem.TryEvaluateFootprint(
                _surface,
                originCell,
                footprintCells,
                RuntimeCityMaxSurfaceHeightDelta,
                RuntimeCityMaxSurfaceSlopeDegrees,
                out BuildingSurfacePlacementSystem.Result surfaceResult))
        {
            return fallbackCenter;
        }

        fallbackCenter.y = surfaceResult.FoundationHeight;
        return fallbackCenter;
    }

    public bool CanReserveFootprint(Vector2Int originCell, Vector2Int footprintCells)
    {
        if (!_hasSurface)
            return true;

        return _buildingSurfacePlacementSystem.TryEvaluateFootprint(
                _surface,
                originCell,
                footprintCells,
                RuntimeCityMaxSurfaceHeightDelta,
                RuntimeCityMaxSurfaceSlopeDegrees,
                out BuildingSurfacePlacementSystem.Result surfaceResult) &&
            surfaceResult.IsValid;
    }

    public bool IsRoadPathSurfaceValid(List<Vector2Int> cells)
    {
        return _roadSurfacePlacementSystem.IsPathSurfaceValid(cells);
    }

    public bool TryResolvePrimarySurface(Vector2Int cell, out MapSurfaceSample sample)
    {
        sample = default;
        if (!_hasSurface)
            return false;

        int2 surfaceCell = new(cell.x, cell.y);
        if ((uint)surfaceCell.x >= (uint)_surface.Dimensions.x ||
            (uint)surfaceCell.y >= (uint)_surface.Dimensions.y)
        {
            return false;
        }

        ref MapSurfaceBlob blob = ref _surface.SurfaceBlob.Value;
        int index = surfaceCell.x + surfaceCell.y * _surface.Dimensions.x;
        if ((uint)index >= (uint)blob.Cells.Length)
            return false;

        MapSurfaceCell cellData = blob.Cells[index];
        if (cellData.SurfaceCount == 0 || (uint)cellData.FirstSurfaceIndex >= (uint)blob.Samples.Length)
            return false;

        sample = blob.Samples[cellData.FirstSurfaceIndex];
        return true;
    }
}
