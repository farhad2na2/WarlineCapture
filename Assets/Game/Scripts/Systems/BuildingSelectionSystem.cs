using System.Collections.Generic;
using UnityEngine;

internal sealed class BuildingSelectionSystem
{
    public delegate bool TryGetGridDelegate(out GridConfig grid);
    public delegate Vector3 GetFootprintCenterDelegate(Vector2Int originCell, Vector2Int footprintCells, GridConfig grid);
    public delegate bool BuildingIdAction(int buildingId);
    public delegate bool BuildingMoveOrderAction(Vector2Int minCell, Vector2Int sizeCells);
    public delegate bool ScreenPositionPredicate(Vector2 screenPosition);
    public delegate bool BuildingDefinitionPredicate(BuildingDefinition definition);
    public delegate void RuntimeAction();
    public delegate void BuildingHudSelectionAction(RuntimeBuildingEntity building);
    public delegate void CameraFocusAction(Vector3 worldPosition);

    public readonly struct Source
    {
        public readonly RuntimeBuildingSystem<RuntimeBuildingEntity> RuntimeBuildingSystem;
        public readonly IReadOnlyDictionary<int, RuntimeBuildingEntity> RuntimeBuildings;
        public readonly Camera WorldCamera;
        public readonly TryGetGridDelegate TryGetGrid;
        public readonly GetFootprintCenterDelegate GetFootprintCenter;
        public readonly RuntimeAction SuppressNextWorldClick;
        public readonly RuntimeAction RefreshMarkers;
        public readonly RuntimeAction ClearFocusedUnit;
        public readonly BuildingHudSelectionAction ShowHudSelection;
        public readonly CameraFocusAction SmoothMoveCameraGroundCenterTo;
        public readonly ScreenPositionPredicate IsBoardablePlayerTransportClick;
        public readonly BuildingIdAction TryAssignSelectedHaulerOrders;
        public readonly BuildingMoveOrderAction TryIssueMoveOrderToBuilding;
        public readonly BuildingDefinitionPredicate ShouldUseExpandedSelectionArea;

        public Source(
            RuntimeBuildingSystem<RuntimeBuildingEntity> runtimeBuildingSystem,
            IReadOnlyDictionary<int, RuntimeBuildingEntity> runtimeBuildings,
            Camera worldCamera,
            TryGetGridDelegate tryGetGrid,
            GetFootprintCenterDelegate getFootprintCenter,
            RuntimeAction suppressNextWorldClick,
            RuntimeAction refreshMarkers,
            RuntimeAction clearFocusedUnit,
            BuildingHudSelectionAction showHudSelection,
            CameraFocusAction smoothMoveCameraGroundCenterTo,
            ScreenPositionPredicate isBoardablePlayerTransportClick,
            BuildingIdAction tryAssignSelectedHaulerOrders,
            BuildingMoveOrderAction tryIssueMoveOrderToBuilding,
            BuildingDefinitionPredicate shouldUseExpandedSelectionArea)
        {
            RuntimeBuildingSystem = runtimeBuildingSystem;
            RuntimeBuildings = runtimeBuildings;
            WorldCamera = worldCamera;
            TryGetGrid = tryGetGrid;
            GetFootprintCenter = getFootprintCenter;
            SuppressNextWorldClick = suppressNextWorldClick;
            RefreshMarkers = refreshMarkers;
            ClearFocusedUnit = clearFocusedUnit;
            ShowHudSelection = showHudSelection;
            SmoothMoveCameraGroundCenterTo = smoothMoveCameraGroundCenterTo;
            IsBoardablePlayerTransportClick = isBoardablePlayerTransportClick;
            TryAssignSelectedHaulerOrders = tryAssignSelectedHaulerOrders;
            TryIssueMoveOrderToBuilding = tryIssueMoveOrderToBuilding;
            ShouldUseExpandedSelectionArea = shouldUseExpandedSelectionArea;
        }
    }

    public readonly struct Context
    {
        public readonly RuntimeBuildingSystem<RuntimeBuildingEntity> RuntimeBuildingSystem;
        public readonly IReadOnlyDictionary<int, RuntimeBuildingEntity> RuntimeBuildings;
        public readonly Camera WorldCamera;
        public readonly TryGetGridDelegate TryGetGrid;
        public readonly GetFootprintCenterDelegate GetFootprintCenter;
        public readonly RuntimeAction SuppressNextWorldClick;
        public readonly RuntimeAction RefreshMarkers;
        public readonly RuntimeAction ClearFocusedUnit;
        public readonly BuildingHudSelectionAction ShowHudSelection;
        public readonly CameraFocusAction SmoothMoveCameraGroundCenterTo;
        public readonly ScreenPositionPredicate IsBoardablePlayerTransportClick;
        public readonly BuildingIdAction TryAssignSelectedHaulerOrders;
        public readonly BuildingMoveOrderAction TryIssueMoveOrderToBuilding;
        public readonly BuildingDefinitionPredicate ShouldUseExpandedSelectionArea;

        public Context(
            RuntimeBuildingSystem<RuntimeBuildingEntity> runtimeBuildingSystem,
            IReadOnlyDictionary<int, RuntimeBuildingEntity> runtimeBuildings,
            Camera worldCamera,
            TryGetGridDelegate tryGetGrid,
            GetFootprintCenterDelegate getFootprintCenter,
            RuntimeAction suppressNextWorldClick,
            RuntimeAction refreshMarkers,
            RuntimeAction clearFocusedUnit,
            BuildingHudSelectionAction showHudSelection,
            CameraFocusAction smoothMoveCameraGroundCenterTo,
            ScreenPositionPredicate isBoardablePlayerTransportClick,
            BuildingIdAction tryAssignSelectedHaulerOrders,
            BuildingMoveOrderAction tryIssueMoveOrderToBuilding,
            BuildingDefinitionPredicate shouldUseExpandedSelectionArea)
        {
            RuntimeBuildingSystem = runtimeBuildingSystem;
            RuntimeBuildings = runtimeBuildings;
            WorldCamera = worldCamera;
            TryGetGrid = tryGetGrid;
            GetFootprintCenter = getFootprintCenter;
            SuppressNextWorldClick = suppressNextWorldClick;
            RefreshMarkers = refreshMarkers;
            ClearFocusedUnit = clearFocusedUnit;
            ShowHudSelection = showHudSelection;
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

    public void DeleteSelectedBuilding(Context context, BuildingIdAction deleteBuildingById)
    {
        int? buildingId = context.RuntimeBuildingSystem?.CurrentActiveBuildingId;
        if (!buildingId.HasValue)
            return;

        deleteBuildingById?.Invoke(buildingId.Value);
    }

    public Context CreateContext(Source source)
    {
        return new Context(
            source.RuntimeBuildingSystem,
            source.RuntimeBuildings,
            source.WorldCamera,
            source.TryGetGrid,
            source.GetFootprintCenter,
            source.SuppressNextWorldClick,
            source.RefreshMarkers,
            source.ClearFocusedUnit,
            source.ShowHudSelection,
            source.SmoothMoveCameraGroundCenterTo,
            source.IsBoardablePlayerTransportClick,
            source.TryAssignSelectedHaulerOrders,
            source.TryIssueMoveOrderToBuilding,
            source.ShouldUseExpandedSelectionArea);
    }

    public Context CreateContext(
        RuntimeBuildingSystem<RuntimeBuildingEntity> runtimeBuildingSystem,
        IReadOnlyDictionary<int, RuntimeBuildingEntity> runtimeBuildings,
        Camera worldCamera,
        TryGetGridDelegate tryGetGrid,
        GetFootprintCenterDelegate getFootprintCenter,
        RuntimeAction suppressNextWorldClick,
        RuntimeAction refreshMarkers,
        RuntimeAction clearFocusedUnit,
        BuildingHudSelectionAction showHudSelection,
        CameraFocusAction smoothMoveCameraGroundCenterTo,
        ScreenPositionPredicate isBoardablePlayerTransportClick,
        BuildingIdAction tryAssignSelectedHaulerOrders,
        BuildingMoveOrderAction tryIssueMoveOrderToBuilding,
        BuildingDefinitionPredicate shouldUseExpandedSelectionArea)
    {
        return CreateContext(new Source(
            runtimeBuildingSystem,
            runtimeBuildings,
            worldCamera,
            tryGetGrid,
            getFootprintCenter,
            suppressNextWorldClick,
            refreshMarkers,
            clearFocusedUnit,
            showHudSelection,
            smoothMoveCameraGroundCenterTo,
            isBoardablePlayerTransportClick,
            tryAssignSelectedHaulerOrders,
            tryIssueMoveOrderToBuilding,
            shouldUseExpandedSelectionArea));
    }

    public void SelectAndFocusBuilding(Context context, RuntimeBuildingEntity building)
    {
        if (building == null)
            return;

        context.RuntimeBuildingSystem?.SelectBuilding(building.Id);
        context.SuppressNextWorldClick?.Invoke();
        context.RefreshMarkers?.Invoke();
        context.ClearFocusedUnit?.Invoke();
        context.ShowHudSelection?.Invoke(building);

        Vector3 focusWorldPosition = ResolveBuildingFocusWorldPosition(context, building);
        context.SmoothMoveCameraGroundCenterTo?.Invoke(focusWorldPosition);
    }

    public Vector3 ResolveBuildingFocusWorldPosition(Context context, RuntimeBuildingEntity building)
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

    public bool TryResolveBuildingFocusWorldPosition(Context context, RuntimeBuildingEntity building, out Vector3 worldPosition)
    {
        worldPosition = Vector3.zero;
        if (building == null)
            return false;

        worldPosition = ResolveBuildingFocusWorldPosition(context, building);
        return true;
    }

    public bool HasVisibleSelectableBuilding(Context context, Camera camera, int screenWidth, int screenHeight)
    {
        if (camera == null || context.RuntimeBuildings == null)
            return false;

        Rect screenRect = new(0f, 0f, screenWidth, screenHeight);
        foreach (KeyValuePair<int, RuntimeBuildingEntity> pair in context.RuntimeBuildings)
        {
            RuntimeBuildingEntity building = pair.Value;
            if (building == null || building.IsDestroyed || building.Instance == null || !building.Instance.activeInHierarchy)
                continue;

            Vector3 screen = camera.WorldToScreenPoint(ResolveBuildingFocusWorldPosition(context, building));
            if (screen.z > 0f && screenRect.Contains(new Vector2(screen.x, screen.y)))
                return true;
        }

        return false;
    }

    public bool SelectFirstBuildingInScreenRect(Context context, Rect screenRect)
    {
        if (context.WorldCamera == null || context.RuntimeBuildings == null)
            return false;

        foreach (KeyValuePair<int, RuntimeBuildingEntity> entry in context.RuntimeBuildings)
        {
            RuntimeBuildingEntity building = entry.Value;
            if (building?.Definition == null || building.IsDestroyed || building.Instance == null || !building.Instance.activeInHierarchy)
                continue;

            if (!TryGetBuildingScreenRect(context.WorldCamera, building, out Rect buildingRect, out _))
                continue;
            if (!screenRect.Overlaps(buildingRect))
                continue;

            Vector2Int min = building.OriginCell;
            Vector2Int size = building.Definition.FootprintCells;
            return SelectBuildingCandidate(context, entry.Key, min, size);
        }

        return false;
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

        foreach (KeyValuePair<int, RuntimeBuildingEntity> entry in context.RuntimeBuildings)
        {
            RuntimeBuildingEntity building = entry.Value;
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

            return SelectBuildingCandidate(context, entry.Key, min, size);
        }

        return TrySelectVisualBuildingAtScreenPosition(context, screenPosition);
    }

    private static bool SelectBuildingCandidate(
        Context context,
        int buildingId,
        Vector2Int min,
        Vector2Int size)
    {
        if (context.TryAssignSelectedHaulerOrders != null &&
            context.TryAssignSelectedHaulerOrders(buildingId))
        {
            context.SuppressNextWorldClick?.Invoke();
            context.ClearFocusedUnit?.Invoke();
            return true;
        }

        context.RuntimeBuildingSystem?.SelectBuilding(buildingId);
        context.SuppressNextWorldClick?.Invoke();
        context.RefreshMarkers?.Invoke();
        context.ClearFocusedUnit?.Invoke();
        RuntimeBuildingEntity selectedBuilding = context.RuntimeBuildings != null &&
                                               context.RuntimeBuildings.TryGetValue(buildingId, out RuntimeBuildingEntity building)
            ? building
            : null;
        context.ShowHudSelection?.Invoke(selectedBuilding);
        return true;
    }

    private static bool TrySelectVisualBuildingAtScreenPosition(Context context, Vector2 screenPosition)
    {
        if (context.WorldCamera == null || context.RuntimeBuildings == null)
            return false;

        int bestBuildingId = 0;
        RuntimeBuildingEntity bestBuilding = null;
        float bestDepth = float.MaxValue;
        float bestArea = float.MaxValue;

        foreach (KeyValuePair<int, RuntimeBuildingEntity> entry in context.RuntimeBuildings)
        {
            RuntimeBuildingEntity building = entry.Value;
            if (building == null || building.IsDestroyed || building.Instance == null || !building.Instance.activeInHierarchy)
                continue;

            if (!TryGetBuildingScreenRect(context.WorldCamera, building, out Rect rect, out float depth))
                continue;
            if (!rect.Contains(screenPosition))
                continue;

            float area = rect.width * rect.height;
            if (depth > bestDepth + 0.001f)
                continue;
            if (Mathf.Abs(depth - bestDepth) <= 0.001f && area >= bestArea)
                continue;

            bestBuildingId = entry.Key;
            bestBuilding = building;
            bestDepth = depth;
            bestArea = area;
        }

        if (bestBuilding == null || bestBuilding.Definition == null)
            return false;

        Vector2Int min = bestBuilding.OriginCell;
        Vector2Int size = bestBuilding.Definition.FootprintCells;
        if (context.ShouldUseExpandedSelectionArea != null &&
            context.ShouldUseExpandedSelectionArea(bestBuilding.Definition))
        {
            min -= Vector2Int.one;
            size += new Vector2Int(2, 2);
        }

        return SelectBuildingCandidate(context, bestBuildingId, min, size);
    }

    private static bool TryGetBuildingScreenRect(Camera camera, RuntimeBuildingEntity building, out Rect rect, out float depth)
    {
        rect = default;
        depth = float.MaxValue;

        Renderer[] renderers = building.FactionVisualRenderers;
        if (renderers == null || renderers.Length == 0)
            renderers = building.Instance.GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0)
            return false;

        bool hasPoint = false;
        Vector2 min = new(float.MaxValue, float.MaxValue);
        Vector2 max = new(float.MinValue, float.MinValue);
        float minDepth = float.MaxValue;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                continue;

            Bounds bounds = renderer.bounds;
            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 world = new(
                    (corner & 1) == 0 ? bounds.min.x : bounds.max.x,
                    (corner & 2) == 0 ? bounds.min.y : bounds.max.y,
                    (corner & 4) == 0 ? bounds.min.z : bounds.max.z);
                Vector3 screen = camera.WorldToScreenPoint(world);
                if (screen.z <= 0f)
                    continue;

                hasPoint = true;
                min.x = Mathf.Min(min.x, screen.x);
                min.y = Mathf.Min(min.y, screen.y);
                max.x = Mathf.Max(max.x, screen.x);
                max.y = Mathf.Max(max.y, screen.y);
                minDepth = Mathf.Min(minDepth, screen.z);
            }
        }

        if (!hasPoint)
            return false;

        const float PaddingPixels = 8f;
        rect = Rect.MinMaxRect(
            min.x - PaddingPixels,
            min.y - PaddingPixels,
            max.x + PaddingPixels,
            max.y + PaddingPixels);
        depth = minDepth;
        return rect.width > 0f && rect.height > 0f;
    }
}
