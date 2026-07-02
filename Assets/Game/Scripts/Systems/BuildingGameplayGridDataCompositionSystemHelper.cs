using Unity.Entities;
using UnityEngine;
using Game.Components;

namespace Game.Runtime
{
    internal sealed class BuildingGameplayGridDataCompositionSystemHelper
    {
        internal delegate bool TryGetEntityManagerDelegate(out EntityManager entityManager);

        internal bool TryGetGridForPlacementInput(
            BuildingGameplayEcsQueryCompositionSystemHelper ecsQuerySystem,
            TryGetEntityManagerDelegate tryGetEntityManager,
            out GridConfig grid)
        {
            return TryGetGridData(ecsQuerySystem, tryGetEntityManager, out _, out grid, out _, out _);
        }

        internal bool TryGetGridForSelection(
            BuildingGameplayEcsQueryCompositionSystemHelper ecsQuerySystem,
            TryGetEntityManagerDelegate tryGetEntityManager,
            out GridConfig grid)
        {
            return TryGetGridData(ecsQuerySystem, tryGetEntityManager, out _, out grid, out _, out _);
        }

        internal bool TryGetGridData(
            BuildingGameplayEcsQueryCompositionSystemHelper ecsQuerySystem,
            TryGetEntityManagerDelegate tryGetEntityManager,
            out Entity gridEntity,
            out GridConfig grid,
            out DynamicBuffer<GridRoad> roads,
            out DynamicBlockerComponent blockerData)
        {
            gridEntity = Entity.Null;
            grid = default;
            roads = default;
            blockerData = default;

            if (!tryGetEntityManager(out EntityManager em))
                return false;

            ecsQuerySystem.EnsureEntityQueries(em);
            EntityQuery gridDataQuery = ecsQuerySystem.GridDataQuery;
            if (gridDataQuery.IsEmptyIgnoreFilter)
                return false;

            gridEntity = gridDataQuery.GetSingletonEntity();
            grid = em.GetComponentData<GridConfig>(gridEntity);
            roads = em.GetBuffer<GridRoad>(gridEntity);
            blockerData = em.GetComponentData<DynamicBlockerComponent>(gridEntity);
            return true;
        }

        internal bool TryGetGridCell(
            BuildingPlacementGridCameraSystemHelper gridSystem,
            BuildingPlacementStartupSystemHelper startupSystem,
            Vector2 screenPosition,
            GridConfig grid,
            out Vector2Int cell)
        {
            return gridSystem.TryGetGridCell(screenPosition, grid, startupSystem.WorldCamera, startupSystem.BuildPlaneY, out cell);
        }
    }
}
