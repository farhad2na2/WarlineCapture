using Unity.Entities;
using UnityEngine;
using Game.Components;

namespace Game.Runtime
{
    internal sealed partial class BuildingPlacementInvalidCellCacheCompositionSystemHelper
    {
        internal bool IsPlacementValid(
            BuildingDefinition definition,
            Vector2Int originCell,
            Vector2Int footprintCells,
            bool rotateVertical,
            GridConfig grid,
            DynamicBuffer<GridRoad> roads,
            DynamicBlockerComponent blockerData,
            BuildingGameplayDependencyCompositionSystemHelper dependencySystem,
            BuildingPlacementStartupSystemHelper startupSystem,
            GetEffectivePlacementRectDelegate getEffectivePlacementRect,
            System.Func<RectInt, bool> overlapsRuntimeBuilding)
        {
            RectInt placementRect = definition != null && getEffectivePlacementRect != null
                ? getEffectivePlacementRect(definition, originCell, grid, rotateVertical)
                : new RectInt(originCell, footprintCells);

            bool roadGate = BuildingBarrierUtilitySystemHelper.IsWallGateDefinition(definition);
            return (roadGate || !RoadSurfaces.Overlaps(grid,placementRect.position,placementRect.size)) &&
                BuildingPlacementValidationUtilitySystemHelper.IsPlacementRectValid(
                placementRect,
                grid,
                roads,
                blockerData,
                _hasPlacementInvalidPrefix,
                _placementInvalidPrefix,
                _placementInvalidPrefixWidth,
                _placementInvalidPrefixHeight,
                dependencySystem.IsRuntimeBlockerCell,
                (queryGrid, queryOrigin, queryFootprint) => HasRoadInFootprint(startupSystem, queryGrid, queryOrigin, queryFootprint),
                overlapsRuntimeBuilding, roadGate);
        }
    }
}
