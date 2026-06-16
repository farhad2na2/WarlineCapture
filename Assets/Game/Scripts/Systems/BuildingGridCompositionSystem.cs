using Unity.Entities;
using UnityEngine;

internal partial struct BuildingGridCompositionSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.Enabled = false;
    }

    public void OnUpdate(ref SystemState state)
    {
    }

    public bool TryGetGridData(
        BuildingGameplayCompositionSourceSystem source,
        BuildingGameplayGridDataSystem.TryGetEntityManagerDelegate tryGetEntityManager,
        out Entity gridEntity,
        out GridConfig grid,
        out DynamicBuffer<GridRoad> roads,
        out DynamicBlockerComponent blockerData)
    {
        return source.BuildingGameplayGridDataSystem.TryGetGridData(
            source.BuildingGameplayEcsQuerySystem,
            tryGetEntityManager,
            out gridEntity,
            out grid,
            out roads,
            out blockerData);
    }

    public bool TryGetGridForSelection(
        BuildingGameplayCompositionSourceSystem source,
        BuildingGameplayGridDataSystem.TryGetEntityManagerDelegate tryGetEntityManager,
        out GridConfig grid)
    {
        return source.BuildingGameplayGridDataSystem.TryGetGridForSelection(
            source.BuildingGameplayEcsQuerySystem,
            tryGetEntityManager,
            out grid);
    }

    public bool TryGetGridForPlacementInput(
        BuildingGameplayCompositionSourceSystem source,
        BuildingGameplayGridDataSystem.TryGetEntityManagerDelegate tryGetEntityManager,
        out GridConfig grid)
    {
        return source.BuildingGameplayGridDataSystem.TryGetGridForPlacementInput(
            source.BuildingGameplayEcsQuerySystem,
            tryGetEntityManager,
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
