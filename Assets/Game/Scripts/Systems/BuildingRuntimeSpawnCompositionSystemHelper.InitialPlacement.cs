using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using Game.Components;

namespace Game.Runtime
{
    internal sealed partial class BuildingRuntimeSpawnCompositionSystemHelper
    {
        public bool TryFindValidInitialBuildingOrigin(
            Context context,
            BuildingDefinition definition,
            Vector2Int preferredOrigin,
            bool rotateVertical,
            GridConfig grid,
            DynamicBuffer<GridRoad> roads,
            DynamicBlockerComponent blockerData,
            out Vector2Int originCell)
        {
            originCell = default;
            if (definition == null || context.GetPlacementFootprint == null || context.GetEffectivePlacementRect == null || context.IsPlacementValid == null)
                return false;

            Vector2Int placementFootprint = context.GetPlacementFootprint(definition, rotateVertical);
            Vector2Int clampedPreferred = new(
                Mathf.Clamp(preferredOrigin.x, 0, Mathf.Max(0, grid.Width - placementFootprint.x)),
                Mathf.Clamp(preferredOrigin.y, 0, Mathf.Max(0, grid.Height - placementFootprint.y)));

            RectInt preferredPlacementRect = context.GetEffectivePlacementRect(definition, clampedPreferred, grid, rotateVertical);
            int footprintSearchRadius = Mathf.Max(placementFootprint.x, placementFootprint.y) * 4;
            int maxSearchRadius = Mathf.Max(
                24,
                Mathf.Min(
                    160,
                    Mathf.Max(
                        footprintSearchRadius,
                        preferredPlacementRect.width,
                        preferredPlacementRect.height)));
            for (int radius = 0; radius <= maxSearchRadius; radius++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    for (int dx = -radius; dx <= radius; dx++)
                    {
                        if (radius > 0 && Mathf.Abs(dx) != radius && Mathf.Abs(dy) != radius)
                            continue;

                        Vector2Int candidate = clampedPreferred + new Vector2Int(dx, dy);
                        RectInt candidateRect = context.GetEffectivePlacementRect(definition, candidate, grid, rotateVertical);
                        if (!BuildingBarrierUtilitySystemHelper.IsWallGateDefinition(definition) && context.HasCachedInvalidCellInFootprint != null &&
                            context.HasCachedInvalidCellInFootprint(candidateRect.position, candidateRect.size))
                        {
                            continue;
                        }

                        if (!context.IsPlacementValid(definition, candidate, placementFootprint, rotateVertical, grid, roads, blockerData))
                            continue;

                        originCell = candidate;
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
