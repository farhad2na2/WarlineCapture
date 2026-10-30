using Game.Components;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Runtime
{
    // Shared cell-origin policy for legacy authored map placements. Hosts pass
    // source-derived grid and footprint data; they do not reimplement centering.
    internal static class MapBuildingPlacementOriginKernel
    {
        internal static int2 ResolvePlacementFootprint(
            int footprintX,
            int footprintY,
            bool isLinearWall,
            float localBoundsSizeX,
            float localBoundsSizeZ,
            bool rotateVertical)
        {
            if (!rotateVertical)
                return new int2(footprintX, footprintY);

            if (!isLinearWall)
                return new int2(footprintY, footprintX);

            return WallSegmentFootprint(localBoundsSizeX, localBoundsSizeZ, true);
        }

        internal static int2 WallSegmentFootprint(float localBoundsSizeX, float localBoundsSizeZ, bool vertical)
        {
            int lengthCells = Mathf.Max(1, Mathf.RoundToInt(Mathf.Max(
                Mathf.Abs(localBoundsSizeX), Mathf.Abs(localBoundsSizeZ))));
            int thicknessCells = Mathf.Max(1, Mathf.RoundToInt(Mathf.Min(
                Mathf.Abs(localBoundsSizeX), Mathf.Abs(localBoundsSizeZ))));

            return vertical
                ? new int2(thicknessCells, lengthCells)
                : new int2(lengthCells, thicknessCells);
        }

        internal static int2 CenterCellToOrigin(int2 centerCell, Vector2Int footprint, GridConfig grid)
        {
            int originX = centerCell.x - Mathf.Max(0, footprint.x - 1) / 2;
            int originY = centerCell.y - Mathf.Max(0, footprint.y - 1) / 2;
            return new int2(
                Mathf.Clamp(originX, 0, Mathf.Max(0, grid.Width - footprint.x)),
                Mathf.Clamp(originY, 0, Mathf.Max(0, grid.Height - footprint.y)));
        }
    }
}
