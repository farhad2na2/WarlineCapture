using Unity.Entities;
using UnityEngine;
using Game.Components;

namespace Game.Runtime
{
    internal sealed partial class BuildingPlacementInvalidCellCacheCompositionSystemHelper
    {
        internal bool[] GetRoadSidewalks()=>RoadSurfaces.GetSidewalks();
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

            bool roadGate = BuildingBarrierUtilitySystemHelper.RequiresRoadPlacement(definition);
            if (roadGate)
            {
                var center=originCell+new Vector2Int(footprintCells.x/2,footprintCells.y/2);
                var sidewalks=RoadSurfaces.GetSidewalks();
                bool IsRoad(Vector2Int cell) => cell.x>=0 && cell.y>=0 && cell.x<grid.Width && cell.y<grid.Height &&
                    (sidewalks==null || !sidewalks[cell.y*grid.Width+cell.x]) &&
                    (roads[cell.y*grid.Width+cell.x].Value!=0 || HasRoadInFootprint(startupSystem,grid,cell,Vector2Int.one));
                if (!IsRoad(center) || !BuildingPlacementVisualUpdateCompositionSystemHelper.TryResolveRoadGateCenter(center,IsRoad,out var roadCenter,out bool acrossRoad) ||
                    roadCenter!=center || acrossRoad!=rotateVertical) return false;
            }
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
