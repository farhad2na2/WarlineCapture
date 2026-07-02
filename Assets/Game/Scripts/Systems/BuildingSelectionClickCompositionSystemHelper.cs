using UnityEngine;
using Game.Components;

namespace Game.Runtime
{
    internal sealed class BuildingSelectionClickCompositionSystemHelper
    {
        internal delegate bool TryGetGridForSelectionDelegate(
            BuildingGameplaySourceCompositionSystemHelper source,
            out GridConfig grid);

        internal delegate bool TryGetGridCellDelegate(
            BuildingGameplaySourceCompositionSystemHelper source,
            Vector2 screenPosition,
            GridConfig grid,
            out Vector2Int cell);

        public BuildingSelectionClickUtilitySystemHelper.Context Create(
            BuildingGameplaySourceCompositionSystemHelper source,
            TryGetGridForSelectionDelegate tryGetGridForSelection,
            TryGetGridCellDelegate tryGetGridCell,
            System.Func<BuildingGameplaySourceCompositionSystemHelper, BuildingSelectionRuntimeCompositionSystemHelper.Context> createBuildingSelectionContext)
        {
            return source.BuildingSelectionClickUtilitySystemHelper.CreateContext(new BuildingSelectionClickUtilitySystemHelper.Source(
                source.UnitPathfindingPendingStateReader.HasPendingPathJob,
                (out GridConfig grid) => tryGetGridForSelection(source, out grid),
                (Vector2 screenPosition, GridConfig grid, out Vector2Int cell) => tryGetGridCell(source, screenPosition, grid, out cell),
                (screenPosition, cell) => source.BuildingSelectionRuntimeCompositionSystemHelper.HandleBuildingSelectionClick(
                    createBuildingSelectionContext(source),
                    screenPosition,
                    cell)));
        }
    }
}
