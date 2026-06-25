using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using static UnityEngine.Object;

internal sealed class RoadBuildBuildingPlacementCompositionSystemHelper
{
    public delegate bool TryGetGridCellDelegate(Vector2 screenPosition, GridConfig grid, out Vector2Int cell);
    public delegate bool IsRuntimeBlockerCellDelegate(int x, int y, int width, int height);

    internal sealed class State
    {
        public bool IsDraggingBuildingPlacement;
    }

    public readonly struct Context
    {
        public readonly RoadBuildPlacementStorageSystem StorageSystem;
        public readonly State PlacementState;
        public readonly RoadBuildPlacementVisualSystem PlacementVisualSystem;
        public readonly RoadBuildPlacementVisualSystem.State PlacementVisualState;
        public readonly Transform BuildingRoot;
        public readonly float BuildPlaneY;
        public readonly float PlacementOutlineWidth;
        public readonly float PlacementOutlineHeight;
        public readonly Color PlacementValidColor;
        public readonly Color PlacementInvalidColor;
        public readonly RoadBuildEcsBoundaryCompositionSystemHelper.TryGetGridDataDelegate TryGetGridData;
        public readonly TryGetGridCellDelegate TryGetGridCell;
        public readonly IsRuntimeBlockerCellDelegate IsRuntimeBlockerCell;

        public Context(
            RoadBuildPlacementStorageSystem storageSystem,
            State placementState,
            RoadBuildPlacementVisualSystem placementVisualSystem,
            RoadBuildPlacementVisualSystem.State placementVisualState,
            Transform buildingRoot,
            float buildPlaneY,
            float placementOutlineWidth,
            float placementOutlineHeight,
            Color placementValidColor,
            Color placementInvalidColor,
            RoadBuildEcsBoundaryCompositionSystemHelper.TryGetGridDataDelegate tryGetGridData,
            TryGetGridCellDelegate tryGetGridCell,
            IsRuntimeBlockerCellDelegate isRuntimeBlockerCell)
        {
            StorageSystem = storageSystem;
            PlacementState = placementState;
            PlacementVisualSystem = placementVisualSystem;
            PlacementVisualState = placementVisualState;
            BuildingRoot = buildingRoot;
            BuildPlaneY = buildPlaneY;
            PlacementOutlineWidth = placementOutlineWidth;
            PlacementOutlineHeight = placementOutlineHeight;
            PlacementValidColor = placementValidColor;
            PlacementInvalidColor = placementInvalidColor;
            TryGetGridData = tryGetGridData;
            TryGetGridCell = tryGetGridCell;
            IsRuntimeBlockerCell = isRuntimeBlockerCell;
        }
    }

    public State CreateState()
    {
        return new State();
    }

    public void SetDragging(State state, bool value)
    {
        if (state != null)
            state.IsDraggingBuildingPlacement = value;
    }

    public void BeginBuildingPlacement(Context context, BuildingDefinition definition)
    {
        if (definition == null || context.StorageSystem == null)
            return;

        CancelBuildingPlacement(context);
        SetDragging(context.PlacementState, false);

        context.StorageSystem.BeginPlacement(
            definition,
            Instantiate(definition.Prefab, context.BuildingRoot),
            GetCenterScreenPlacementOrigin(context, definition.FootprintCells));

        UpdateBuildingPlacementVisual(context, context.StorageSystem.ActivePlacement, updateCellFromPointer: false);
    }

    public void CancelBuildingPlacement(Context context)
    {
        GameObject previewInstance = context.StorageSystem?.ClearActivePlacement();
        if (previewInstance != null)
            Destroy(previewInstance);

        SetDragging(context.PlacementState, false);
        context.PlacementVisualSystem?.HidePlacementOutline(context.PlacementVisualState);
    }

    public void UpdateBuildingPlacement(Context context, Vector2 screenPosition)
    {
        BuildingPlacementLifecycleCompositionSystemHelper.PlacementState activePlacement = context.StorageSystem?.ActivePlacement;
        if (activePlacement == null)
            return;

        UpdateBuildingPlacementVisual(
            context,
            activePlacement,
            context.PlacementState != null && context.PlacementState.IsDraggingBuildingPlacement,
            screenPosition);
    }

    private void UpdateBuildingPlacementVisual(
        Context context,
        BuildingPlacementLifecycleCompositionSystemHelper.PlacementState placement,
        bool updateCellFromPointer,
        Vector2 screenPosition = default)
    {
        if (placement == null || placement.PreviewInstance == null)
            return;

        if (context.TryGetGridData != null &&
            context.TryGetGridData(out _, out GridConfig grid, out DynamicBuffer<GridRoad> roads, out DynamicBlockerComponent blockerData) &&
            updateCellFromPointer)
        {
            if (context.TryGetGridCell != null && context.TryGetGridCell(screenPosition, grid, out Vector2Int hoveredCell))
                placement.OriginCell = CenterCellToOrigin(hoveredCell, placement.Definition.FootprintCells);
        }

        if (context.TryGetGridData == null || !context.TryGetGridData(out _, out grid, out roads, out blockerData))
        {
            placement.IsValid = false;
            context.PlacementVisualSystem?.HidePlacementOutline(context.PlacementVisualState);
            return;
        }

        placement.IsValid = IsBuildingPlacementValid(
            context,
            placement.OriginCell,
            placement.Definition.FootprintCells,
            grid,
            roads,
            blockerData);
        PositionBuildingObject(context, placement.PreviewInstance, placement.OriginCell, placement.Definition, grid);
        context.PlacementVisualSystem?.UpdatePlacementOutline(
            context.PlacementVisualState,
            placement.OriginCell,
            placement.Definition.FootprintCells,
            grid,
            context.BuildPlaneY,
            context.PlacementOutlineWidth,
            context.PlacementOutlineHeight,
            context.PlacementValidColor,
            context.PlacementInvalidColor,
            placement.IsValid);
    }

    private static void PositionBuildingObject(
        Context context,
        GameObject instance,
        Vector2Int originCell,
        BuildingDefinition definition,
        GridConfig grid)
    {
        if (instance == null)
            return;

        Vector3 center = GetFootprintCenter(context, originCell, definition.FootprintCells, grid);
        Vector3 offset = Vector3.zero;
        if (definition.HasLocalBounds)
            offset = new Vector3(definition.LocalBounds.center.x, 0f, definition.LocalBounds.center.z);

        instance.transform.SetPositionAndRotation(center, Quaternion.identity);
        instance.transform.localScale = Vector3.one;

        if (instance.transform.childCount > 0)
        {
            Transform visualRoot = instance.transform.GetChild(0);
            visualRoot.localPosition = -offset;
            visualRoot.localRotation = Quaternion.identity;
            visualRoot.localScale = Vector3.one;
        }
    }

    private static Vector3 GetFootprintCenter(Context context, Vector2Int originCell, Vector2Int footprintCells, GridConfig grid)
    {
        return new Vector3(
            grid.Origin.x + (originCell.x + footprintCells.x * 0.5f) * grid.CellSize,
            context.BuildPlaneY,
            grid.Origin.z + (originCell.y + footprintCells.y * 0.5f) * grid.CellSize);
    }

    private static Vector2Int GetCenterScreenPlacementOrigin(Context context, Vector2Int footprintCells)
    {
        if (context.TryGetGridData == null || !context.TryGetGridData(out _, out GridConfig grid, out _, out _))
            return Vector2Int.zero;

        Vector2 centerScreen = new(Screen.width * 0.5f, Screen.height * 0.5f);
        if (context.TryGetGridCell != null && context.TryGetGridCell(centerScreen, grid, out Vector2Int centerCell))
            return CenterCellToOrigin(centerCell, footprintCells);

        return Vector2Int.zero;
    }

    private static Vector2Int CenterCellToOrigin(Vector2Int centerCell, Vector2Int footprintCells)
    {
        return new Vector2Int(
            centerCell.x - Mathf.FloorToInt(footprintCells.x * 0.5f),
            centerCell.y - Mathf.FloorToInt(footprintCells.y * 0.5f));
    }

    private static bool IsBuildingPlacementValid(
        Context context,
        Vector2Int originCell,
        Vector2Int footprintCells,
        GridConfig grid,
        DynamicBuffer<GridRoad> roads,
        DynamicBlockerComponent blockerData)
    {
        if (originCell.x < 0 || originCell.y < 0)
            return false;
        if (originCell.x + footprintCells.x > grid.Width || originCell.y + footprintCells.y > grid.Height)
            return false;

        for (int y = originCell.y; y < originCell.y + footprintCells.y; y++)
        {
            for (int x = originCell.x; x < originCell.x + footprintCells.x; x++)
            {
                int index = GridUtils.CellToIndex(new int2(x, y), grid.Width);
                if (roads[index].Value != 0)
                    return false;
                if (blockerData.Blocked.IsCreated &&
                    blockerData.Blocked.IsSet(index) &&
                    (context.IsRuntimeBlockerCell == null || !context.IsRuntimeBlockerCell(x, y, grid.Width, grid.Height)))
                    return false;
            }
        }

        return true;
    }
}
