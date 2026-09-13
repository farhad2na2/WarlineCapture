using UnityEngine;
using Game.Components;

namespace Game.Runtime
{
    internal sealed partial class BuildingPlacementInputUiSystemHelper
    {
        private static Vector2Int PointerFootprint(IPlacementState placement)
        {
            var size = placement.Definition.FootprintCells;
            return placement.AutoRotateVertical && !placement.Definition.IsWall ? new Vector2Int(size.y, size.x) : size;
        }
        public bool IsPointerOverPlacement(
            IPlacementState placement,
            Vector2 screenPosition,
            GridConfig grid,
            TryGetGridCellDelegate tryGetGridCell)
        {
            if (placement == null || tryGetGridCell == null || !tryGetGridCell(screenPosition, grid, out Vector2Int cell))
                return false;

            Vector2Int origin = placement.OriginCell;
            Vector2Int size = PointerFootprint(placement);
            return cell.x >= origin.x &&
                   cell.y >= origin.y &&
                   cell.x < origin.x + size.x &&
                   cell.y < origin.y + size.y;
        }
    }
}
