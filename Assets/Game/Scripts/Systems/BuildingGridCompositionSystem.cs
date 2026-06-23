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
        BuildingGameplaySourceCompositionSystemHelper source,
        BuildingGameplayGridDataCompositionSystemHelper.TryGetEntityManagerDelegate tryGetEntityManager,
        out Entity gridEntity,
        out GridConfig grid,
        out DynamicBuffer<GridRoad> roads,
        out DynamicBlockerComponent blockerData)
    {
        return source.BuildingGameplayGridDataCompositionSystemHelper.TryGetGridData(
            source.BuildingGameplayEcsQuerySystem,
            tryGetEntityManager,
            out gridEntity,
            out grid,
            out roads,
            out blockerData);
    }

    public bool TryGetGridForSelection(
        BuildingGameplaySourceCompositionSystemHelper source,
        BuildingGameplayGridDataCompositionSystemHelper.TryGetEntityManagerDelegate tryGetEntityManager,
        out GridConfig grid)
    {
        return source.BuildingGameplayGridDataCompositionSystemHelper.TryGetGridForSelection(
            source.BuildingGameplayEcsQuerySystem,
            tryGetEntityManager,
            out grid);
    }

    public bool TryGetGridForPlacementInput(
        BuildingGameplaySourceCompositionSystemHelper source,
        BuildingGameplayGridDataCompositionSystemHelper.TryGetEntityManagerDelegate tryGetEntityManager,
        out GridConfig grid)
    {
        return source.BuildingGameplayGridDataCompositionSystemHelper.TryGetGridForPlacementInput(
            source.BuildingGameplayEcsQuerySystem,
            tryGetEntityManager,
            out grid);
    }

    public bool TryGetGridCell(
        BuildingGameplaySourceCompositionSystemHelper source,
        Vector2 screenPosition,
        GridConfig grid,
        out Vector2Int cell)
    {
        return source.BuildingGameplayGridDataCompositionSystemHelper.TryGetGridCell(
            source.BuildingPlacementGridSystem,
            source.BuildingPlacementStartupSystem,
            screenPosition,
            grid,
            out cell);
    }
}
