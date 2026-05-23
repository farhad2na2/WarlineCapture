using System.Collections.Generic;
using UnityEngine;
using RuntimeBuildingData = BuildingPlacementSystem.RuntimeBuildingData;
using BuildingDefinition = BuildingPlacementSystem.BuildingDefinition;

internal sealed class BuildingSelectionSystem
{
    public delegate bool TryGetGridDelegate(out GridConfig grid);
    public delegate Vector3 GetFootprintCenterDelegate(Vector2Int originCell, Vector2Int footprintCells, GridConfig grid);
    public delegate bool BuildingIdAction(int buildingId);
    public delegate bool BuildingMoveOrderAction(Vector2Int minCell, Vector2Int sizeCells);
    public delegate bool ScreenPositionPredicate(Vector2 screenPosition);
    public delegate bool BuildingDefinitionPredicate(BuildingDefinition definition);
    public delegate void RuntimeAction();
    public delegate void CameraFocusAction(Vector3 worldPosition);

    public readonly struct Context
    {
        public readonly RuntimeBuildingSystem<RuntimeBuildingData> RuntimeBuildingSystem;
        public readonly IReadOnlyDictionary<int, RuntimeBuildingData> RuntimeBuildings;
        public readonly TryGetGridDelegate TryGetGrid;
        public readonly GetFootprintCenterDelegate GetFootprintCenter;
        public readonly RuntimeAction SuppressNextWorldClick;
        public readonly RuntimeAction RefreshMarkers;
        public readonly RuntimeAction ClearFocusedUnit;
        public readonly CameraFocusAction SmoothMoveCameraGroundCenterTo;
        public readonly ScreenPositionPredicate IsBoardablePlayerTransportClick;
        public readonly BuildingIdAction TryAssignSelectedHaulerOrders;
        public readonly BuildingMoveOrderAction TryIssueMoveOrderToBuilding;
        public readonly BuildingDefinitionPredicate ShouldUseExpandedSelectionArea;

        public Context(
            RuntimeBuildingSystem<RuntimeBuildingData> runtimeBuildingSystem,
            IReadOnlyDictionary<int, RuntimeBuildingData> runtimeBuildings,
            TryGetGridDelegate tryGetGrid,
            GetFootprintCenterDelegate getFootprintCenter,
            RuntimeAction suppressNextWorldClick,
            RuntimeAction refreshMarkers,
            RuntimeAction clearFocusedUnit,
            CameraFocusAction smoothMoveCameraGroundCenterTo,
            ScreenPositionPredicate isBoardablePlayerTransportClick,
            BuildingIdAction tryAssignSelectedHaulerOrders,
            BuildingMoveOrderAction tryIssueMoveOrderToBuilding,
            BuildingDefinitionPredicate shouldUseExpandedSelectionArea)
        {
            RuntimeBuildingSystem = runtimeBuildingSystem;
            RuntimeBuildings = runtimeBuildings;
            TryGetGrid = tryGetGrid;
            GetFootprintCenter = getFootprintCenter;
            SuppressNextWorldClick = suppressNextWorldClick;
            RefreshMarkers = refreshMarkers;
            ClearFocusedUnit = clearFocusedUnit;
            SmoothMoveCameraGroundCenterTo = smoothMoveCameraGroundCenterTo;
            IsBoardablePlayerTransportClick = isBoardablePlayerTransportClick;
            TryAssignSelectedHaulerOrders = tryAssignSelectedHaulerOrders;
            TryIssueMoveOrderToBuilding = tryIssueMoveOrderToBuilding;
            ShouldUseExpandedSelectionArea = shouldUseExpandedSelectionArea;
        }
    }

    public void ClearSelectedBuilding(Context context)
    {
        context.RuntimeBuildingSystem?.ClearSelection();
        context.RefreshMarkers?.Invoke();
    }

    public void SelectAndFocusBuilding(Context context, RuntimeBuildingData building)
    {
        if (building == null)
            return;

        context.RuntimeBuildingSystem?.SelectBuilding(building.Id);
        context.SuppressNextWorldClick?.Invoke();
        context.RefreshMarkers?.Invoke();
        context.ClearFocusedUnit?.Invoke();

        Vector3 focusWorldPosition = ResolveBuildingFocusWorldPosition(context, building);
        context.SmoothMoveCameraGroundCenterTo?.Invoke(focusWorldPosition);
    }

    public Vector3 ResolveBuildingFocusWorldPosition(Context context, RuntimeBuildingData building)
    {
        if (building?.Instance == null)
            return Vector3.zero;

        if (building.Definition != null &&
            context.TryGetGrid != null &&
            context.GetFootprintCenter != null &&
            context.TryGetGrid(out GridConfig grid))
        {
            return context.GetFootprintCenter(building.OriginCell, building.Definition.FootprintCells, grid);
        }

        Vector3 position = building.Instance.transform.position;
        position.y = 0f;
        return position;
    }

    public bool HandleBuildingSelectionClick(Context context, Vector2 screenPosition, Vector2Int cell)
    {
        if (context.RuntimeBuildings == null)
            return false;

        if (context.IsBoardablePlayerTransportClick != null &&
            context.IsBoardablePlayerTransportClick(screenPosition))
        {
            return true;
        }

        foreach (KeyValuePair<int, RuntimeBuildingData> entry in context.RuntimeBuildings)
        {
            RuntimeBuildingData building = entry.Value;
            if (building?.Definition == null)
                continue;

            Vector2Int min = building.OriginCell;
            Vector2Int size = building.Definition.FootprintCells;
            if (context.ShouldUseExpandedSelectionArea != null &&
                context.ShouldUseExpandedSelectionArea(building.Definition))
            {
                min -= Vector2Int.one;
                size += new Vector2Int(2, 2);
            }

            if (cell.x < min.x || cell.y < min.y || cell.x >= min.x + size.x || cell.y >= min.y + size.y)
                continue;

            if (context.TryAssignSelectedHaulerOrders != null &&
                context.TryAssignSelectedHaulerOrders(entry.Key))
            {
                context.SuppressNextWorldClick?.Invoke();
                context.ClearFocusedUnit?.Invoke();
                return true;
            }

            if (context.TryIssueMoveOrderToBuilding != null &&
                context.TryIssueMoveOrderToBuilding(min, size))
            {
                context.SuppressNextWorldClick?.Invoke();
                ClearSelectedBuilding(context);
                return true;
            }

            context.RuntimeBuildingSystem?.SelectBuilding(entry.Key);
            context.SuppressNextWorldClick?.Invoke();
            context.ClearFocusedUnit?.Invoke();
            return true;
        }

        return false;
    }
}
