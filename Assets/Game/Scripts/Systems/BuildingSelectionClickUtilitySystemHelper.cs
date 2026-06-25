using System;
using UnityEngine;

public sealed class BuildingSelectionClickUtilitySystemHelper
{
    public delegate bool TryGetGridDelegate(out GridConfig grid);
    public delegate bool TryGetGridCellDelegate(Vector2 screenPosition, GridConfig grid, out Vector2Int cell);
    public delegate bool HandleCellSelectionDelegate(Vector2 screenPosition, Vector2Int cell);

    public readonly struct Source
    {
        public readonly Func<bool> HasPendingPathJob;
        public readonly TryGetGridDelegate TryGetGrid;
        public readonly TryGetGridCellDelegate TryGetGridCell;
        public readonly HandleCellSelectionDelegate HandleCellSelection;

        public Source(
            Func<bool> hasPendingPathJob,
            TryGetGridDelegate tryGetGrid,
            TryGetGridCellDelegate tryGetGridCell,
            HandleCellSelectionDelegate handleCellSelection)
        {
            HasPendingPathJob = hasPendingPathJob;
            TryGetGrid = tryGetGrid;
            TryGetGridCell = tryGetGridCell;
            HandleCellSelection = handleCellSelection;
        }
    }

    public readonly struct Context
    {
        public readonly Func<bool> HasPendingPathJob;
        public readonly TryGetGridDelegate TryGetGrid;
        public readonly TryGetGridCellDelegate TryGetGridCell;
        public readonly HandleCellSelectionDelegate HandleCellSelection;

        public Context(
            Func<bool> hasPendingPathJob,
            TryGetGridDelegate tryGetGrid,
            TryGetGridCellDelegate tryGetGridCell,
            HandleCellSelectionDelegate handleCellSelection)
        {
            HasPendingPathJob = hasPendingPathJob;
            TryGetGrid = tryGetGrid;
            TryGetGridCell = tryGetGridCell;
            HandleCellSelection = handleCellSelection;
        }
    }

    public bool HandleBuildingSelectionClick(Context context, Vector2 screenPosition)
    {
        if (context.HasPendingPathJob != null && context.HasPendingPathJob())
            return false;

        if (context.TryGetGrid == null || !context.TryGetGrid(out GridConfig grid))
            return false;

        Vector2Int cell = new(int.MinValue / 2, int.MinValue / 2);
        if (context.TryGetGridCell != null)
            context.TryGetGridCell(screenPosition, grid, out cell);

        return context.HandleCellSelection != null &&
               context.HandleCellSelection(screenPosition, cell);
    }

    public Context CreateContext(Source source)
    {
        return new Context(
            source.HasPendingPathJob,
            source.TryGetGrid,
            source.TryGetGridCell,
            source.HandleCellSelection);
    }

    public Context CreateContext(
        Func<bool> hasPendingPathJob,
        TryGetGridDelegate tryGetGrid,
        TryGetGridCellDelegate tryGetGridCell,
        HandleCellSelectionDelegate handleCellSelection)
    {
        return CreateContext(new Source(
            hasPendingPathJob,
            tryGetGrid,
            tryGetGridCell,
            handleCellSelection));
    }
}
