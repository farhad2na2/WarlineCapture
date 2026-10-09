using Unity.Entities;
using UnityEngine;
using Game.Components;

namespace Game.Runtime
{
    internal sealed partial class BuildingPlacementInvalidCellCacheCompositionSystemHelper
    {
        internal delegate RectInt GetEffectivePlacementRectDelegate(
            BuildingDefinition definition,
            Vector2Int originCell,
            GridConfig grid,
            bool rotateVertical);

        private int[] _placementInvalidPrefix;
        internal readonly BuildingPlacementAuthoredRoadCache RoadSurfaces=new();
        internal readonly BuildingPlacementAuthoredBuildingCache AuthoredBuildings = new();
        private bool _hasPlacementInvalidPrefix;
        private int _placementInvalidPrefixWidth;
        private int _placementInvalidPrefixHeight;
        private World _authoredGeometryWorld;
        private GridConfig _authoredGeometryGrid;
        private int _authoredGeometryFrame = -1;

        internal void Clear()
        {
            _hasPlacementInvalidPrefix = false;
            _authoredGeometryFrame = -1;
        }

        internal void EnsureAuthoredGeometry(EntityManager em, EntityQuery surfaceQuery, GridConfig grid)
        {
            // Preview searches validate many candidates in one frame. Initialize
            // the actual player cache once per frame, rather than relying on the
            // separate starting-grant cache or scanning every renderer per candidate.
            if (_authoredGeometryWorld == em.World && _authoredGeometryFrame == Time.frameCount &&
                _authoredGeometryGrid.Width == grid.Width && _authoredGeometryGrid.Height == grid.Height &&
                _authoredGeometryGrid.CellSize == grid.CellSize && _authoredGeometryGrid.Origin.Equals(grid.Origin)) return;
            RoadSurfaces.Ensure(em, surfaceQuery, grid);
            AuthoredBuildings.Ensure(em, grid);
            _authoredGeometryWorld = em.World;
            _authoredGeometryGrid = grid;
            _authoredGeometryFrame = Time.frameCount;
            _hasPlacementInvalidPrefix = false;
        }

        internal void RebuildPlacementInvalidPrefix(
            BuildingGameplayGridDataCompositionSystemHelper gridDataSystem,
            BuildingGameplayEcsQueryCompositionSystemHelper ecsQuerySystem,
            BuildingGameplayGridDataCompositionSystemHelper.TryGetEntityManagerDelegate tryGetEntityManager,
            BuildingPlacementStartupSystemHelper startupSystem,
            BuildingGameplayDependencyCompositionSystemHelper dependencySystem)
        {
            _hasPlacementInvalidPrefix = false;
            if (!gridDataSystem.TryGetGridData(ecsQuerySystem, tryGetEntityManager, out _, out GridConfig grid, out DynamicBuffer<GridRoad> roads, out DynamicBlockerComponent blockerData))
                return;

            bool[] roadMask = new bool[grid.Width * grid.Height];
            startupSystem.FillRoadFootprintMask(grid, roadMask);
            if(tryGetEntityManager(out var em))
            {
                EnsureAuthoredGeometry(em,ecsQuerySystem.SurfaceQuery,grid);
            }
            AuthoredBuildings.AppendTo(roadMask);
            RoadSurfaces.AppendTo(roadMask);
            RoadSurfaces.AppendWaterTo(roadMask);

            BuildingPlacementValidationUtilitySystemHelper.RebuildInvalidPrefix(
                grid,
                roads,
                blockerData,
                roadMask,
                dependencySystem.IsRuntimeBlockerCell,
                ref _placementInvalidPrefix,
                out _placementInvalidPrefixWidth,
                out _placementInvalidPrefixHeight,
                out _hasPlacementInvalidPrefix);
        }


        internal bool HasCachedInvalidCellInFootprint(Vector2Int originCell, Vector2Int footprintCells)
        {
            if (!_hasPlacementInvalidPrefix)
                return false;

            return BuildingPlacementValidationUtilitySystemHelper.HasCachedInvalidCellInFootprint(
                _placementInvalidPrefix,
                _placementInvalidPrefixWidth,
                _placementInvalidPrefixHeight,
                originCell,
                footprintCells);
        }

        internal bool HasRoadInFootprint(
            BuildingPlacementStartupSystemHelper startupSystem,
            GridConfig grid,
            Vector2Int originCell,
            Vector2Int footprintCells)
        {
            if(RoadSurfaces.Overlaps(grid,originCell,footprintCells)) return true;
            return startupSystem.HasRoadInFootprint(grid, originCell, footprintCells);
        }

    }
}
