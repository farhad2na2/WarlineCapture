using Unity.Entities;
using UnityEngine;
using Game.Components;

namespace Game.Runtime
{
    internal sealed class BuildingPlacementInvalidCellCacheCompositionSystemHelper
    {
        internal delegate RectInt GetEffectivePlacementRectDelegate(
            BuildingDefinition definition,
            Vector2Int originCell,
            GridConfig grid,
            bool rotateVertical);

        private int[] _placementInvalidPrefix;
        internal readonly BuildingPlacementAuthoredRoadCache RoadSurfaces=new();
        private bool _hasPlacementInvalidPrefix;
        private int _placementInvalidPrefixWidth;
        private int _placementInvalidPrefixHeight;

        internal void Clear()
        {
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
            if(tryGetEntityManager(out var em)) RoadSurfaces.Ensure(em,ecsQuerySystem.SurfaceQuery,grid);
            RoadSurfaces.AppendTo(roadMask);

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

            return !RoadSurfaces.Overlaps(grid,placementRect.position,placementRect.size) &&
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
                overlapsRuntimeBuilding);
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
