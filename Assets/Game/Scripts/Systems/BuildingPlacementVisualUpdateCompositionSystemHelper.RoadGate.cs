using System;
using Game.Components;
using Unity.Entities;
using UnityEngine;

namespace Game.Runtime
{
    internal sealed partial class BuildingPlacementVisualUpdateCompositionSystemHelper
    {
        private static void AlignRoadGate(Context context,
            BuildingPlacementLifecycleCompositionSystemHelper.PlacementState placement,
            GridConfig grid, DynamicBuffer<GridRoad> roads)
        {
            if (placement.ManualRotation || !BuildingBarrierUtilitySystemHelper.IsWallGateDefinition(placement.Definition) ||
                placement.LastRoadSampleOrigin == placement.OriginCell) return;
            placement.LastRoadSampleOrigin = placement.OriginCell;
            var footprint = context.GetPlacementFootprint(placement.Definition, placement.AutoRotateVertical);
            var center = placement.OriginCell + new Vector2Int(footprint.x / 2, footprint.y / 2);
            var roadQuery = context.ContextSystem.CreateWallValidationContext(context.CreatePlacementContextSource()).HasRoadInFootprint;
            bool IsRoad(Vector2Int cell) => cell.x >= 0 && cell.y >= 0 && cell.x < grid.Width && cell.y < grid.Height &&
                (roads[cell.y * grid.Width + cell.x].Value != 0 || roadQuery != null && roadQuery(grid, cell, Vector2Int.one));
            if (!TryResolveRoadGateRotation(center, IsRoad, out bool vertical)) return;
            if (vertical != placement.AutoRotateVertical)
            {
                placement.AutoRotateVertical = vertical;
                var rotated = context.GetPlacementFootprint(placement.Definition, vertical);
                placement.OriginCell = center - new Vector2Int(rotated.x / 2, rotated.y / 2);
            }
            placement.LastRoadAlignmentOrigin = placement.OriginCell;
            placement.LastRoadSampleOrigin = placement.OriginCell;
        }

        // Bounded local road sampling. At ambiguous intersections leave orientation to Rotate.
        internal static bool TryResolveRoadGateRotation(Vector2Int center, Func<Vector2Int, bool> isRoad, out bool vertical)
        {
            vertical = false;
            Vector2Int nearest = center;
            bool found = false;
            for (int radius = 0; radius <= 10 && !found; radius++)
                for (int y = -radius; y <= radius && !found; y++)
                    for (int x = -radius; x <= radius; x++)
                    {
                        if (Mathf.Abs(x) != radius && Mathf.Abs(y) != radius) continue;
                        var cell = center + new Vector2Int(x, y);
                        if (!isRoad(cell)) continue;
                        nearest = cell; found = true; break;
                    }
            if (!found) return false;
            int horizontal = RoadRun(nearest, Vector2Int.right, isRoad);
            int longitudinal = RoadRun(nearest, Vector2Int.up, isRoad);
            if (Mathf.Abs(horizontal - longitudinal) < 4) return false;
            vertical = horizontal > longitudinal;
            return true;
        }

        private static int RoadRun(Vector2Int cell, Vector2Int axis, Func<Vector2Int, bool> isRoad)
        {
            int length = 1;
            for (int direction = -1; direction <= 1; direction += 2)
                for (int offset = 1; offset <= 32; offset++)
                {
                    if (!isRoad(cell + axis * (offset * direction))) break;
                    length++;
                }
            return length;
        }
    }
}
