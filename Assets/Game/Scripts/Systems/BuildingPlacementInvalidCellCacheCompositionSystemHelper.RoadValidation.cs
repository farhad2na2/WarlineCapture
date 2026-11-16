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
#if UNITY_EDITOR
            try
            {
                S002PhysicalStartupObservation.OriginalPlacement?.Invoke(new S002PhysicalStartupObservation.OriginalPlacementFacts
                {
                    Definition = definition, Origin = originCell, Footprint = footprintCells, EffectiveRect = placementRect,
                    Rotated = rotateVertical, Grid = grid, HasInvalidPrefix = _hasPlacementInvalidPrefix, RoadGate = roadGate,
                    InvalidPrefix = _placementInvalidPrefix, PrefixWidth = _placementInvalidPrefixWidth, PrefixHeight = _placementInvalidPrefixHeight,
                    Blockers = blockerData, Buildings = AuthoredBuildings, Roads = RoadSurfaces,
                    Startup = startupSystem, Dependencies = dependencySystem, OverlapsRuntimeBuilding = overlapsRuntimeBuilding
                });
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception);
            }
#endif
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
            if (!roadGate)
            {
                var source = new OrdinaryPlacementValidationSource(this, placementRect, grid, roads, blockerData,
                    dependencySystem, startupSystem, overlapsRuntimeBuilding);
                return BuildingPlacementOrdinaryValidationPolicy.IsValid(ref source);
            }
            return !AuthoredBuildings.Overlaps(grid, placementRect.position, placementRect.size) &&
                !RoadSurfaces.OverlapsWater(grid,placementRect.position,placementRect.size) &&
                (roadGate || !RoadSurfaces.Overlaps(grid,placementRect.position,placementRect.size)) &&
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
        private readonly struct OrdinaryPlacementValidationSource : IOrdinaryPlacementValidationSource
        {
            private readonly BuildingPlacementInvalidCellCacheCompositionSystemHelper owner;
            private readonly RectInt rect;
            private readonly GridConfig grid;
            private readonly DynamicBuffer<GridRoad> roads;
            private readonly DynamicBlockerComponent blockers;
            private readonly BuildingGameplayDependencyCompositionSystemHelper dependency;
            private readonly BuildingPlacementStartupSystemHelper startup;
            private readonly System.Func<RectInt, bool> overlap;
            internal OrdinaryPlacementValidationSource(BuildingPlacementInvalidCellCacheCompositionSystemHelper owner, RectInt rect,
                GridConfig grid, DynamicBuffer<GridRoad> roads, DynamicBlockerComponent blockers,
                BuildingGameplayDependencyCompositionSystemHelper dependency, BuildingPlacementStartupSystemHelper startup,
                System.Func<RectInt, bool> overlap)
            { this.owner=owner;this.rect=rect;this.grid=grid;this.roads=roads;this.blockers=blockers;this.dependency=dependency;this.startup=startup;this.overlap=overlap; }
            public bool AuthoredOverlap()=>owner.AuthoredBuildings.Overlaps(grid,rect.position,rect.size);
            public bool WaterOverlap()=>owner.RoadSurfaces.OverlapsWater(grid,rect.position,rect.size);
            public bool RoadOverlap()=>owner.RoadSurfaces.Overlaps(grid,rect.position,rect.size);
            public bool ValidRect()
            {
                var currentOwner=owner;var currentStartup=startup;
                return BuildingPlacementValidationUtilitySystemHelper.IsPlacementRectValid(rect,grid,roads,blockers,
                    owner._hasPlacementInvalidPrefix,owner._placementInvalidPrefix,owner._placementInvalidPrefixWidth,owner._placementInvalidPrefixHeight,
                    dependency.IsRuntimeBlockerCell,
                    (queryGrid,queryOrigin,queryFootprint)=>currentOwner.HasRoadInFootprint(currentStartup,queryGrid,queryOrigin,queryFootprint),overlap,false);
            }
        }
    }
}
