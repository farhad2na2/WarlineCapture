using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Game.Components;

namespace Game.Runtime
{
    internal sealed class BuildingPlacementGridCameraSystemHelper
    {
        public Vector3 GetFootprintCenter(Vector2Int originCell, Vector2Int footprintCells, GridConfig grid, float buildPlaneY)
        {
            return new Vector3(
                grid.Origin.x + (originCell.x + footprintCells.x * 0.5f) * grid.CellSize,
                buildPlaneY,
                grid.Origin.z + (originCell.y + footprintCells.y * 0.5f) * grid.CellSize);
        }

        public Vector2Int GetCenterScreenPlacementOrigin(
            Vector2Int footprintCells,
            GridConfig grid,
            Camera worldCamera,
            float buildPlaneY,
            Vector2 screenSize)
        {
            Vector2 centerScreen = new(screenSize.x * 0.5f, screenSize.y * 0.5f);
            return TryGetGridCell(centerScreen, grid, worldCamera, buildPlaneY, out Vector2Int centerCell)
                ? CenterCellToOrigin(centerCell, footprintCells)
                : Vector2Int.zero;
        }

        public static Vector2Int CenterCellToOrigin(Vector2Int centerCell, Vector2Int footprintCells)
        {
            return new Vector2Int(
                centerCell.x - Mathf.FloorToInt(footprintCells.x * 0.5f),
                centerCell.y - Mathf.FloorToInt(footprintCells.y * 0.5f));
        }

        public bool TryGetGridCell(
            Vector2 screenPosition,
            GridConfig grid,
            Camera worldCamera,
            float buildPlaneY,
            out Vector2Int cell)
        {
            cell = default;
            if (worldCamera == null)
                return false;

            Ray ray = worldCamera.ScreenPointToRay(screenPosition);
            Plane plane = new(Vector3.up, new Vector3(0f, buildPlaneY, 0f));
            if (!plane.Raycast(ray, out float distance))
                return false;

            int2 gridCell = GridUtils.WorldToCell(grid, ray.GetPoint(distance));
            if (!GridUtils.InBounds(gridCell, grid.Width, grid.Height))
                return false;

            cell = new Vector2Int(gridCell.x, gridCell.y);
            return true;
        }

        public Vector2Int GetPlacementFootprint(BuildingDefinition definition, bool rotateVertical)
        {
            if (definition == null)
                return Vector2Int.one;

            if (!rotateVertical)
                return definition.FootprintCells;

            if (BuildingBarrierUtilitySystemHelper.IsLinearWallDefinition(definition))
                return BuildingPlacementCommitCompositionSystemHelper.GetWallSegmentFootprint(definition, true);

            return new Vector2Int(definition.FootprintCells.y, definition.FootprintCells.x);
        }

        public Vector3 ResolvePlacementFocusWorldPosition(
            BuildingPlacementInputUiSystemHelper.IPlacementState placement,
            IReadOnlyList<Vector2Int> allOrigins,
            GridConfig grid,
            Vector2Int wallFootprint,
            float buildPlaneY)
        {
            if (placement == null)
                return Vector3.zero;

            if (allOrigins == null || allOrigins.Count == 0)
                return GetFootprintCenter(placement.OriginCell, wallFootprint, grid, buildPlaneY);

            int minX = int.MaxValue;
            int minY = int.MaxValue;
            int maxX = int.MinValue;
            int maxY = int.MinValue;
            for (int i = 0; i < allOrigins.Count; i++)
            {
                Vector2Int origin = allOrigins[i];
                minX = Mathf.Min(minX, origin.x);
                minY = Mathf.Min(minY, origin.y);
                maxX = Mathf.Max(maxX, origin.x + wallFootprint.x);
                maxY = Mathf.Max(maxY, origin.y + wallFootprint.y);
            }

            return GetFootprintCenter(new Vector2Int(minX, minY), new Vector2Int(maxX - minX, maxY - minY), grid, buildPlaneY);
        }
    }
}
