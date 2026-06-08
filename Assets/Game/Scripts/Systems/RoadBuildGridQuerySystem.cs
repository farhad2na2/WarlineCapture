using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

internal sealed class RoadBuildGridQuerySystem
{
    public readonly struct Context
    {
        public readonly RoadGridProjectionSystem RoadGridProjectionSystem;
        public readonly Camera WorldCamera;
        public readonly float BuildPlaneY;

        public Context(RoadGridProjectionSystem roadGridProjectionSystem, Camera worldCamera, float buildPlaneY)
        {
            RoadGridProjectionSystem = roadGridProjectionSystem;
            WorldCamera = worldCamera;
            BuildPlaneY = buildPlaneY;
        }
    }

    internal sealed class State
    {
        private readonly RoadBuildGridQuerySystem _system;
        private Context _context;

        public State(RoadBuildGridQuerySystem system)
        {
            _system = system;
        }

        public void Configure(Context context)
        {
            _context = context;
        }

        public bool TryGetGridData(
            out Entity gridEntity,
            out GridConfig grid,
            out DynamicBuffer<GridRoad> roads,
            out DynamicBlockerComponent blockerData)
        {
            return _system.TryGetGridData(_context, out gridEntity, out grid, out roads, out blockerData);
        }

        public Vector3 GetFootprintCenter(Vector2Int originCell, Vector2Int footprintCells, GridConfig grid)
        {
            return _system.GetFootprintCenter(_context, originCell, footprintCells, grid);
        }

        public bool TryGetGridCell(Vector2 screenPosition, GridConfig grid, out Vector2Int cell)
        {
            return _system.TryGetGridCell(_context, screenPosition, grid, out cell);
        }
    }

    public State CreateState()
    {
        return new State(this);
    }

    public bool TryGetGridData(
        Context context,
        out Entity gridEntity,
        out GridConfig grid,
        out DynamicBuffer<GridRoad> roads,
        out DynamicBlockerComponent blockerData)
    {
        gridEntity = Entity.Null;
        grid = default;
        roads = default;
        blockerData = default;
        return context.RoadGridProjectionSystem != null &&
               context.RoadGridProjectionSystem.TryGetGridData(out gridEntity, out grid, out roads, out blockerData);
    }

    public bool TryGetGridConfig(Context context, out GridConfig grid)
    {
        grid = default;
        return context.RoadGridProjectionSystem != null &&
               context.RoadGridProjectionSystem.TryGetGridConfig(out grid);
    }

    public Vector3 GetFootprintCenter(Context context, Vector2Int originCell, Vector2Int footprintCells, GridConfig grid)
    {
        return new Vector3(
            grid.Origin.x + (originCell.x + footprintCells.x * 0.5f) * grid.CellSize,
            context.BuildPlaneY,
            grid.Origin.z + (originCell.y + footprintCells.y * 0.5f) * grid.CellSize);
    }

    public bool TryGetGridCell(Context context, Vector2 screenPosition, GridConfig grid, out Vector2Int cell)
    {
        cell = default;
        if (context.WorldCamera == null)
            return false;

        Ray ray = context.WorldCamera.ScreenPointToRay(screenPosition);
        Plane plane = new(Vector3.up, new Vector3(0f, context.BuildPlaneY, 0f));
        if (!plane.Raycast(ray, out float distance))
            return false;

        Vector3 worldPoint = ray.GetPoint(distance);
        int2 gridCell = GridUtils.WorldToCell(grid, worldPoint);
        if (!GridUtils.InBounds(gridCell, grid.Width, grid.Height))
            return false;

        cell = new Vector2Int(gridCell.x, gridCell.y);
        return true;
    }
}
