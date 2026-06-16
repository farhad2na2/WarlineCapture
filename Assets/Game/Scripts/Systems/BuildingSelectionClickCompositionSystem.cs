using Unity.Entities;
using UnityEngine;

internal sealed partial class BuildingSelectionClickCompositionSystem : SystemBase
{
    protected override void OnCreate()
    {
        Enabled = false;
    }

    protected override void OnUpdate()
    {
    }

    internal delegate bool TryGetGridForSelectionDelegate(
        BuildingGameplayCompositionSourceSystem source,
        out GridConfig grid);

    internal delegate bool TryGetGridCellDelegate(
        BuildingGameplayCompositionSourceSystem source,
        Vector2 screenPosition,
        GridConfig grid,
        out Vector2Int cell);

    public BuildingSelectionClickSystem.Context Create(
        BuildingGameplayCompositionSourceSystem source,
        TryGetGridForSelectionDelegate tryGetGridForSelection,
        TryGetGridCellDelegate tryGetGridCell,
        System.Func<BuildingGameplayCompositionSourceSystem, BuildingSelectionSystem.Context> createBuildingSelectionContext)
    {
        return source.BuildingSelectionClickSystem.CreateContext(new BuildingSelectionClickSystem.Source(
            source.UnitPathfindingPendingStateReader.HasPendingPathJob,
            (out GridConfig grid) => tryGetGridForSelection(source, out grid),
            (Vector2 screenPosition, GridConfig grid, out Vector2Int cell) => tryGetGridCell(source, screenPosition, grid, out cell),
            (screenPosition, cell) => source.BuildingSelectionSystem.HandleBuildingSelectionClick(
                createBuildingSelectionContext(source),
                screenPosition,
                cell)));
    }
}
