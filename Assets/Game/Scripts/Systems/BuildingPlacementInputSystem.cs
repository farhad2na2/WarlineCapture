using System.Collections.Generic;
using UnityEngine;

internal sealed class BuildingPlacementInputSystem
{
    public enum DragFirstAxis
    {
        None,
        Horizontal,
        Vertical
    }

    public sealed class WallRun
    {
        public List<Vector2Int> Origins;
        public bool Vertical;
    }

    public interface IPlacementState
    {
        BuildingPlacementSystem.BuildingDefinition Definition { get; }
        Vector2Int OriginCell { get; set; }
        Vector2Int CommittedOriginCell { get; set; }
        Vector2Int DragStartOriginCell { get; set; }
        Vector2Int DragCurrentOriginCell { get; set; }
        DragFirstAxis DragFirstAxis { get; set; }
        bool HideCurrentWallPreview { get; set; }
        bool IsValid { get; }
        float LastPointerMovedAt { get; set; }
        Vector2 LastPointerScreenPosition { get; set; }
        List<WallRun> CommittedWallRuns { get; set; }
    }

    public delegate bool TryGetGridCellDelegate(Vector2 screenPosition, GridConfig grid, out Vector2Int cell);
    public delegate bool TryGetGridForInputDelegate(out GridConfig grid);
    public delegate Vector2Int CenterCellToOriginDelegate(Vector2Int centerCell, Vector2Int footprintCells);
    public delegate Vector2Int GetWallSegmentFootprintDelegate(BuildingPlacementSystem.BuildingDefinition definition, bool vertical);
    public delegate bool IsPointerOverPlacementUiDelegate(Vector2 screenPosition);
    public delegate bool IsLinearWallDefinitionDelegate(BuildingPlacementSystem.BuildingDefinition definition);
    public delegate void UpdatePlacementFromPointerDelegate(Vector2 screenPosition);

    public readonly struct ActivePlacementPointerContext
    {
        public readonly TryGetGridForInputDelegate TryGetGridForInput;
        public readonly TryGetGridCellDelegate TryGetGridCell;
        public readonly CenterCellToOriginDelegate CenterCellToOrigin;
        public readonly GetWallSegmentFootprintDelegate GetWallSegmentFootprint;
        public readonly IsPointerOverPlacementUiDelegate IsPointerOverPlacementUi;
        public readonly IsLinearWallDefinitionDelegate IsLinearWallDefinition;
        public readonly UpdatePlacementFromPointerDelegate UpdatePlacementFromPointer;

        public ActivePlacementPointerContext(
            TryGetGridForInputDelegate tryGetGridForInput,
            TryGetGridCellDelegate tryGetGridCell,
            CenterCellToOriginDelegate centerCellToOrigin,
            GetWallSegmentFootprintDelegate getWallSegmentFootprint,
            IsPointerOverPlacementUiDelegate isPointerOverPlacementUi,
            IsLinearWallDefinitionDelegate isLinearWallDefinition,
            UpdatePlacementFromPointerDelegate updatePlacementFromPointer)
        {
            TryGetGridForInput = tryGetGridForInput;
            TryGetGridCell = tryGetGridCell;
            CenterCellToOrigin = centerCellToOrigin;
            GetWallSegmentFootprint = getWallSegmentFootprint;
            IsPointerOverPlacementUi = isPointerOverPlacementUi;
            IsLinearWallDefinition = isLinearWallDefinition;
            UpdatePlacementFromPointer = updatePlacementFromPointer;
        }
    }

    public bool IsDraggingPlacement { get; private set; }
    public bool IgnorePointerUpdatesUntilRelease { get; private set; }
    public bool ShouldUpdateCellFromPointer => IsDraggingPlacement && !IgnorePointerUpdatesUntilRelease;

    public void Reset()
    {
        IsDraggingPlacement = false;
        IgnorePointerUpdatesUntilRelease = false;
    }

    public void NotifyPlacementUiPointerDown(IPlacementState placement)
    {
        if (placement != null)
            placement.CommittedOriginCell = placement.OriginCell;

        IsDraggingPlacement = false;
        IgnorePointerUpdatesUntilRelease = true;
    }

    public void UpdateActivePlacementPointer(
        IPlacementState placement,
        GamePointerState pointer,
        ActivePlacementPointerContext context)
    {
        if (placement == null)
            return;

        Vector2 pointerPosition = pointer.Position;
        bool isLinearWall = context.IsLinearWallDefinition != null && context.IsLinearWallDefinition(placement.Definition);
        if (pointer.WasPressedThisFrame)
        {
            GridConfig inputGrid = default;
            bool hasGridForInput = context.TryGetGridForInput != null && context.TryGetGridForInput(out inputGrid);
            bool isPointerOverPlacementUi = context.IsPointerOverPlacementUi != null && context.IsPointerOverPlacementUi(pointerPosition);
            TryBeginDrag(
                placement,
                pointerPosition,
                isPointerOverPlacementUi,
                isLinearWall,
                hasGridForInput,
                inputGrid,
                context.TryGetGridCell,
                context.CenterCellToOrigin);
        }

        if (pointer.WasReleasedThisFrame)
        {
            HandlePointerRelease(
                placement,
                isLinearWall,
                context.GetWallSegmentFootprint);
        }

        if (!pointer.IsPressed)
            HandlePointerNotPressed();

        context.UpdatePlacementFromPointer?.Invoke(pointerPosition);
    }

    public void TryBeginDrag(
        IPlacementState placement,
        Vector2 pointerPosition,
        bool isPointerOverPlacementUi,
        bool isLinearWall,
        bool hasGrid,
        GridConfig grid,
        TryGetGridCellDelegate tryGetGridCell,
        CenterCellToOriginDelegate centerCellToOrigin)
    {
        if (placement == null || IgnorePointerUpdatesUntilRelease || isPointerOverPlacementUi)
            return;

        bool canStartDrag = hasGrid && IsPointerOverPlacement(placement, pointerPosition, grid, tryGetGridCell);
        if (!canStartDrag &&
            isLinearWall &&
            hasGrid &&
            tryGetGridCell != null &&
            centerCellToOrigin != null &&
            tryGetGridCell(pointerPosition, grid, out Vector2Int clickedCell))
        {
            Vector2Int clickedOrigin = centerCellToOrigin(clickedCell, placement.Definition.FootprintCells);
            placement.OriginCell = clickedOrigin;
            placement.CommittedOriginCell = clickedOrigin;
            placement.DragStartOriginCell = clickedOrigin;
            placement.DragCurrentOriginCell = clickedOrigin;
            placement.DragFirstAxis = DragFirstAxis.None;
            placement.HideCurrentWallPreview = false;
            canStartDrag = true;
        }

        if (!canStartDrag)
            return;

        IsDraggingPlacement = true;
        placement.CommittedOriginCell = placement.OriginCell;
        placement.DragStartOriginCell = placement.OriginCell;
        placement.DragCurrentOriginCell = placement.OriginCell;
        placement.DragFirstAxis = DragFirstAxis.None;
        placement.HideCurrentWallPreview = false;
    }

    public void HandlePointerRelease(
        IPlacementState placement,
        bool isLinearWall,
        GetWallSegmentFootprintDelegate getWallSegmentFootprint)
    {
        if (IsDraggingPlacement && placement != null && isLinearWall && placement.IsValid)
            CommitCurrentWallRun(placement, getWallSegmentFootprint);

        IsDraggingPlacement = false;
        IgnorePointerUpdatesUntilRelease = false;
    }

    public void HandlePointerNotPressed()
    {
        if (IsDraggingPlacement)
            IsDraggingPlacement = false;
    }

    public bool ApplyPointerHover(
        IPlacementState placement,
        bool updateCellFromPointer,
        Vector2 screenPosition,
        GridConfig grid,
        float currentTime,
        TryGetGridCellDelegate tryGetGridCell,
        CenterCellToOriginDelegate centerCellToOrigin)
    {
        if (placement == null)
            return false;

        if (updateCellFromPointer)
        {
            if ((screenPosition - placement.LastPointerScreenPosition).sqrMagnitude > 1f)
            {
                placement.LastPointerMovedAt = currentTime;
                placement.LastPointerScreenPosition = screenPosition;
            }

            bool pointerIdle = currentTime - placement.LastPointerMovedAt >= 1f;
            if (!pointerIdle &&
                tryGetGridCell != null &&
                centerCellToOrigin != null &&
                tryGetGridCell(screenPosition, grid, out Vector2Int hoveredCell))
            {
                Vector2Int newOrigin = centerCellToOrigin(hoveredCell, placement.Definition.FootprintCells);
                placement.OriginCell = newOrigin;
                placement.CommittedOriginCell = placement.OriginCell;
                placement.DragCurrentOriginCell = placement.OriginCell;
                UpdateWallDragAxis(placement);
            }
        }

        return currentTime - placement.LastPointerMovedAt >= 1f;
    }

    public bool IsPointerOverPlacement(
        IPlacementState placement,
        Vector2 screenPosition,
        GridConfig grid,
        TryGetGridCellDelegate tryGetGridCell)
    {
        if (placement == null || tryGetGridCell == null || !tryGetGridCell(screenPosition, grid, out Vector2Int cell))
            return false;

        Vector2Int origin = placement.OriginCell;
        Vector2Int size = placement.Definition.FootprintCells;
        return cell.x >= origin.x &&
               cell.y >= origin.y &&
               cell.x < origin.x + size.x &&
               cell.y < origin.y + size.y;
    }

    public bool IsWallPlacementVertical(IPlacementState placement)
    {
        if (placement == null)
            return false;

        Vector2Int delta = placement.DragCurrentOriginCell - placement.DragStartOriginCell;
        return Mathf.Abs(delta.y) > Mathf.Abs(delta.x);
    }

    public List<Vector2Int> BuildWallPlacementOrigins(
        IPlacementState placement,
        GetWallSegmentFootprintDelegate getWallSegmentFootprint)
    {
        var origins = new List<Vector2Int>();
        if (placement == null || getWallSegmentFootprint == null)
            return origins;

        Vector2Int start = placement.DragStartOriginCell;
        Vector2Int end = placement.DragCurrentOriginCell;
        bool vertical = IsWallPlacementVertical(placement);
        Vector2Int footprint = getWallSegmentFootprint(placement.Definition, vertical);
        if (vertical)
            end.x = start.x;
        else
            end.y = start.y;

        origins.Add(start);
        if (start == end)
            return origins;

        if (vertical)
        {
            int stepCells = Mathf.Max(1, footprint.y);
            int delta = end.y - start.y;
            int direction = delta >= 0 ? 1 : -1;
            int segmentCount = Mathf.Abs(delta) / stepCells;
            for (int i = 1; i <= segmentCount; i++)
                origins.Add(new Vector2Int(start.x, start.y + (direction * stepCells * i)));
        }
        else
        {
            int stepCells = Mathf.Max(1, footprint.x);
            int delta = end.x - start.x;
            int direction = delta >= 0 ? 1 : -1;
            int segmentCount = Mathf.Abs(delta) / stepCells;
            for (int i = 1; i <= segmentCount; i++)
                origins.Add(new Vector2Int(start.x + (direction * stepCells * i), start.y));
        }

        return origins;
    }

    public List<Vector2Int> GetAllWallPlacementOrigins(IPlacementState placement, IReadOnlyList<Vector2Int> currentOrigins)
    {
        var origins = new List<Vector2Int>();
        if (placement?.CommittedWallRuns != null)
        {
            for (int i = 0; i < placement.CommittedWallRuns.Count; i++)
            {
                WallRun run = placement.CommittedWallRuns[i];
                if (run?.Origins == null)
                    continue;

                origins.AddRange(run.Origins);
            }
        }

        if (placement != null && !placement.HideCurrentWallPreview && currentOrigins != null)
            origins.AddRange(currentOrigins);

        return origins;
    }

    public List<WallRun> BuildFinalWallRuns(IPlacementState placement, GetWallSegmentFootprintDelegate getWallSegmentFootprint)
    {
        var runs = new List<WallRun>();
        if (placement?.CommittedWallRuns != null)
        {
            for (int i = 0; i < placement.CommittedWallRuns.Count; i++)
            {
                WallRun run = placement.CommittedWallRuns[i];
                if (run?.Origins == null || run.Origins.Count == 0)
                    continue;

                runs.Add(run);
            }
        }

        if (placement != null && !placement.HideCurrentWallPreview)
        {
            List<Vector2Int> currentOrigins = BuildWallPlacementOrigins(placement, getWallSegmentFootprint);
            if (currentOrigins.Count > 0)
            {
                runs.Add(new WallRun
                {
                    Origins = currentOrigins,
                    Vertical = IsWallPlacementVertical(placement)
                });
            }
        }

        return runs;
    }

    public void CommitCurrentWallRun(IPlacementState placement, GetWallSegmentFootprintDelegate getWallSegmentFootprint)
    {
        if (placement == null)
            return;

        List<Vector2Int> origins = BuildWallPlacementOrigins(placement, getWallSegmentFootprint);
        if (origins.Count == 0)
            return;

        placement.CommittedWallRuns ??= new List<WallRun>();
        placement.CommittedWallRuns.Add(new WallRun
        {
            Origins = origins,
            Vertical = IsWallPlacementVertical(placement)
        });
        placement.HideCurrentWallPreview = true;
    }

    private static void UpdateWallDragAxis(IPlacementState placement)
    {
        Vector2Int delta = placement.DragCurrentOriginCell - placement.DragStartOriginCell;
        if (delta.x == 0 && delta.y == 0)
        {
            placement.DragFirstAxis = DragFirstAxis.None;
            return;
        }

        placement.DragFirstAxis = Mathf.Abs(delta.x) >= Mathf.Abs(delta.y)
            ? DragFirstAxis.Horizontal
            : DragFirstAxis.Vertical;
    }
}
