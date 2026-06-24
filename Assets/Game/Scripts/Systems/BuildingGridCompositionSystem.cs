using Unity.Entities;
using UnityEngine;

[UpdateInGroup(typeof(SimulationSystemGroup))]
internal partial struct BuildingGridCompositionSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        // RequireForUpdate intentionally omitted: disabled composition helper; OnUpdate never runs.
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
            source.BuildingGameplayEcsQueryCompositionSystemHelper,
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
            source.BuildingGameplayEcsQueryCompositionSystemHelper,
            tryGetEntityManager,
            out grid);
    }

    public bool TryGetGridForPlacementInput(
        BuildingGameplaySourceCompositionSystemHelper source,
        BuildingGameplayGridDataCompositionSystemHelper.TryGetEntityManagerDelegate tryGetEntityManager,
        out GridConfig grid)
    {
        return source.BuildingGameplayGridDataCompositionSystemHelper.TryGetGridForPlacementInput(
            source.BuildingGameplayEcsQueryCompositionSystemHelper,
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
            source.BuildingPlacementStartupSystemHelper,
            screenPosition,
            grid,
            out cell);
    }
}
