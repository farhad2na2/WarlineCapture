using Unity.Entities;
using UnityEngine;

internal sealed class BuildingGridCompositionSystem
{
    internal delegate bool TryGetEntityManagerDelegate(out EntityManager entityManager);

    public bool TryGetGridData(
        BuildingGameplayCompositionSourceSystem source,
        TryGetEntityManagerDelegate tryGetEntityManager,
        out Entity gridEntity,
        out GridConfig grid,
        out DynamicBuffer<GridRoad> roads,
        out DynamicBlockerComponent blockerData)
    {
        return source.BuildingGameplayGridDataSystem.TryGetGridData(
            source.BuildingGameplayEcsQuerySystem,
            (out EntityManager entityManager) => tryGetEntityManager(out entityManager),
            out gridEntity,
            out grid,
            out roads,
            out blockerData);
    }

    public bool TryGetGridForSelection(
        BuildingGameplayCompositionSourceSystem source,
        TryGetEntityManagerDelegate tryGetEntityManager,
        out GridConfig grid)
    {
        return source.BuildingGameplayGridDataSystem.TryGetGridForSelection(
            source.BuildingGameplayEcsQuerySystem,
            (out EntityManager entityManager) => tryGetEntityManager(out entityManager),
            out grid);
    }

    public bool TryGetGridForPlacementInput(
        BuildingGameplayCompositionSourceSystem source,
        TryGetEntityManagerDelegate tryGetEntityManager,
        out GridConfig grid)
    {
        return source.BuildingGameplayGridDataSystem.TryGetGridForPlacementInput(
            source.BuildingGameplayEcsQuerySystem,
            (out EntityManager entityManager) => tryGetEntityManager(out entityManager),
            out grid);
    }

    public bool TryGetGridCell(
        BuildingGameplayCompositionSourceSystem source,
        Vector2 screenPosition,
        GridConfig grid,
        out Vector2Int cell)
    {
        return source.BuildingGameplayGridDataSystem.TryGetGridCell(
            source.BuildingPlacementGridSystem,
            source.BuildingPlacementStartupSystem,
            screenPosition,
            grid,
            out cell);
    }
}
