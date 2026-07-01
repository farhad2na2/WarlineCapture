using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using RoadVisualType = RoadNetworkCompositionSystemHelper.RoadVisualType;
using TileConnectionMask = RoadNetworkCompositionSystemHelper.TileConnectionMask;
using VariantData = RoadVisualVariantSystem.VariantData;

internal sealed class RoadBuildCompositionContextCompositionSystemHelper
{
    public RoadGridProjectionSystem.RoadFootprintState CreateRoadFootprintState(RoadBuildCompositionSourceCompositionSystemHelper source)
    {
        return new RoadGridProjectionSystem.RoadFootprintState(
            source.RoadNetworkCompositionSystemHelper.RoadTiles,
            source.RoadSpecialVisualSystem?.SpecialRoadObjects,
            source.RoadVisualVariantSystem?.VisualData ?? new Dictionary<RoadVisualType, RoadGridProjectionSystem.CombinedRoadVisualData>(),
            source.RoadBuildStartupState.GridOrigin,
            source.RoadBuildStartupState.BuildPlaneY,
            source.RoadBuildStartupState.RoadGridSize);
    }

    public RoadRuntimeGenerationCompositionSystemHelper.Context CreateRoadRuntimeGenerationContext(RoadBuildCompositionSourceCompositionSystemHelper source)
    {
        return RoadRuntimeGenerationContextCompositionSystemHelper.CreateContext(CreateRoadRuntimeGenerationContextSource(source));
    }

    public RoadBuildReadModelCompositionSystemHelper.Context CreateRoadBuildReadModelContext(RoadBuildCompositionSourceCompositionSystemHelper source)
    {
        return new RoadBuildReadModelCompositionSystemHelper.Context(
            source.RuntimeGameplayStateSystem,
            source.RoadBuildSessionCompositionSystemHelper,
            source.RoadBuildSessionState,
            source.RoadBuildInputCompositionSystemHelper,
            source.RoadBuildInputState,
            source.RoadBuildPlacementStorageCompositionSystemHelper,
            source.RoadBuildDependencyState,
            () => source.RoadBuildPlacementState.IsDraggingBuildingPlacement);
    }

    public RoadBuildInteractionContextCompositionSystemHelper.Context CreateRoadBuildInteractionContext(RoadBuildCompositionSourceCompositionSystemHelper source)
    {
        return new RoadBuildInteractionContextCompositionSystemHelper.Context(
            source.RuntimeGameplayStateSystem,
            source.RoadBuildSessionCompositionSystemHelper,
            source.RoadBuildSessionState,
            source.RoadBuildInputCompositionSystemHelper,
            source.RoadBuildInputState,
            source.RoadBuildCommandCompositionSystemHelper,
            source.RoadPathPlanningUtilitySystemHelper,
            source.RoadNetworkCompositionSystemHelper,
            () => source.RoadBuildMutationCompositionSystemHelper.CaptureRoadBuildSessionSnapshot(CreateRoadBuildMutationContext(source)),
            snapshot => source.RoadBuildMutationCompositionSystemHelper.RestoreRoadBuildSession(CreateRoadBuildMutationContext(source), snapshot),
            () => RemoveRuntimeBlockersUnderRoads(source),
            () => source.RoadMinimapEventUiSystemHelper?.PublishStaticMinimapChanged(),
            () => ApplyBuildCommandMode(source),
            () => ClearCommandMode(source),
            () => source.RoadBuildDependencyState.BuildingPlacementInteractionCompositionSystemHelper?.ClearSelectedBuilding(
                source.RoadBuildDependencyState.BuildingPlacementInteractionContext,
                "RoadBuild.ClearSelectedBuilding"),
            () => source.RoadBuildDependencyState.BuildingPlacementInteractionCompositionSystemHelper?.CancelBuildingPlacement(
                source.RoadBuildDependencyState.BuildingPlacementInteractionContext),
            () => source.RoadBuildInputCompositionSystemHelper.CancelPendingBuild(CreateRoadBuildInputContext(source)),
            () => source.RoadBuildPlacementVisualSystem?.HidePlacementOutline(source.RoadBuildPlacementVisualState),
            () => source.RoadPreviewPresentationSystemHelper?.UpdatePreview(
                CreateRoadPreviewContext(source),
                source.RoadBuildInputState.IsDrawing,
                source.RoadBuildInputState.PendingStartCell,
                source.RoadBuildInputState.CurrentDragCell,
                source.RoadBuildInputState.DragFirstAxis),
            (Vector2 screenPosition, out Vector2Int cell) => TryGetHoveredCell(source, screenPosition, out cell),
            () => source.RoadPreviewPresentationSystemHelper?.ClearPreview(),
            screenPosition => source.RoadBuildBuildingPlacementCompositionSystemHelper.UpdateBuildingPlacement(
                CreateRoadBuildPlacementContext(source),
                screenPosition),
            path => source.RoadBuildMutationCompositionSystemHelper.CreateStroke(CreateRoadBuildMutationContext(source), path),
            path => source.RoadSurfacePlacementUtilitySystemHelper.IsPathSurfaceValid(path),
            () => source.RoadBuildPlacementStorageCompositionSystemHelper.HasPendingBuildingPlacement,
            value => source.RoadBuildBuildingPlacementCompositionSystemHelper.SetDragging(source.RoadBuildPlacementState, value),
            () => source.RoadBuildBuildingPlacementCompositionSystemHelper.SetDragging(source.RoadBuildPlacementState, false),
            strokeId => source.RoadBuildMutationCompositionSystemHelper.DeleteStroke(CreateRoadBuildMutationContext(source), strokeId));
    }

    public RoadBuildInputCompositionSystemHelper.Context CreateRoadBuildInputContext(RoadBuildCompositionSourceCompositionSystemHelper source)
    {
        return source.RoadBuildInteractionContextCompositionSystemHelper.CreateInputContext(CreateRoadBuildInteractionContext(source));
    }

    public RoadBuildCommandCompositionSystemHelper.Context CreateRoadBuildCommandContext(RoadBuildCompositionSourceCompositionSystemHelper source)
    {
        return source.RoadBuildInteractionContextCompositionSystemHelper.CreateCommandContext(CreateRoadBuildInteractionContext(source));
    }

    public RoadDeletePromptUiSystemHelper.Context CreateRoadDeletePromptContext(RoadBuildCompositionSourceCompositionSystemHelper source)
    {
        return source.RoadBuildInteractionContextCompositionSystemHelper.CreateDeletePromptContext(CreateRoadBuildInteractionContext(source));
    }

    public RoadBuildDisposalCompositionSystemHelper.Context CreateRoadBuildDisposalContext(RoadBuildCompositionSourceCompositionSystemHelper source)
    {
        return new RoadBuildDisposalCompositionSystemHelper.Context(
            source.RoadBuildStartupSystem,
            source.RoadBuildStartupState,
            source.RoadRuntimeRootSceneSystemHelper,
            source.RoadBuildPlacementVisualSystem,
            source.RoadBuildPlacementVisualState,
            source.RoadVisualVariantSystem,
            source.RoadPreviewPresentationSystemHelper,
            source.RoadChunkVisualSystem,
            source.RoadBuildEcsCompositionSystemHelper,
            source.RoadBuildPlacementStorageCompositionSystemHelper,
            source.RoadSpecialVisualSystem,
            source.RoadMinimapEventUiSystemHelper,
            source.RoadGridProjectionSystem,
            source.RoadNetworkCompositionSystemHelper.RoadTiles);
    }

    private RoadGridProjectionSystem.Context CreateRoadGridProjectionContext(RoadBuildCompositionSourceCompositionSystemHelper source)
    {
        return new RoadGridProjectionSystem.Context(
            source.RoadNetworkCompositionSystemHelper.RoadTiles,
            CreateRoadFootprintState(source),
            source.RoadBuildStartupState.RoadGridSize);
    }

    private RoadBuildVisualContextPresentationSystemHelper.Context CreateRoadBuildVisualContext(RoadBuildCompositionSourceCompositionSystemHelper source)
    {
        return new RoadBuildVisualContextPresentationSystemHelper.Context(
            source.RoadNetworkCompositionSystemHelper,
            source.RoadPathPlanningUtilitySystemHelper,
            source.RoadVisualVariantSystem,
            source.RoadBuildStartupSystem,
            source.RoadBuildStartupState,
            (cell, mask) => ResolveVisualType(source, cell, mask),
            (RoadVisualType type, TileConnectionMask mask, out VariantData variant) =>
                TryGetVariant(source, type, mask, out variant),
            type => GetPrefab(source, type),
            (RoadVisualType type, TileConnectionMask mask, out VariantData variant) =>
                TryGetVariant(source, type, mask, out variant));
    }

    private RoadVisualResolutionSystem.Context CreateRoadVisualResolutionContext(RoadBuildCompositionSourceCompositionSystemHelper source)
    {
        return new RoadVisualResolutionSystem.Context(
            source.RoadNetworkCompositionSystemHelper,
            source.RoadVisualVariantSystem,
            CreateRoadBuildVisualContext(source));
    }

    private RoadVisualRefreshPresentationSystemHelper.Context CreateRoadVisualRefreshContext(RoadBuildCompositionSourceCompositionSystemHelper source)
    {
        return new RoadVisualRefreshPresentationSystemHelper.Context(
            source.RoadNetworkCompositionSystemHelper,
            source.RoadGridProjectionSystem,
            CreateRoadGridProjectionContext(source),
            source.RoadChunkVisualSystem,
            CreateRoadChunkVisualContext(source),
            source.RoadSpecialVisualSystem,
            CreateRoadSpecialVisualContext(source),
            CreateRoadVisualResolutionContext(source));
    }

    private RoadChunkVisualSystem.Context CreateRoadChunkVisualContext(RoadBuildCompositionSourceCompositionSystemHelper source)
    {
        return RoadBuildVisualContextPresentationSystemHelper.CreateChunkContext(CreateRoadBuildVisualContext(source));
    }

    private RoadPreviewPresentationSystemHelper.Context CreateRoadPreviewContext(RoadBuildCompositionSourceCompositionSystemHelper source)
    {
        return RoadBuildVisualContextPresentationSystemHelper.CreatePreviewContext(CreateRoadBuildVisualContext(source));
    }

    private RoadSpecialVisualSystem.Context CreateRoadSpecialVisualContext(RoadBuildCompositionSourceCompositionSystemHelper source)
    {
        return RoadBuildVisualContextPresentationSystemHelper.CreateSpecialContext(CreateRoadBuildVisualContext(source));
    }

    private RoadBuildMutationCompositionSystemHelper.Context CreateRoadBuildMutationContext(RoadBuildCompositionSourceCompositionSystemHelper source)
    {
        return new RoadBuildMutationCompositionSystemHelper.Context(
            source.RoadNetworkCompositionSystemHelper,
            dirtyCells => RoadVisualRefreshPresentationSystemHelper.RefreshCells(CreateRoadVisualRefreshContext(source), dirtyCells),
            () => RoadVisualRefreshPresentationSystemHelper.RebuildRoadStateFromCurrentTiles(CreateRoadVisualRefreshContext(source)));
    }

    private RoadBuildContextCompositionSystemHelper.Context CreateRoadBuildContext(RoadBuildCompositionSourceCompositionSystemHelper source)
    {
        return new RoadBuildContextCompositionSystemHelper.Context(
            source.RoadBuildEcsCompositionSystemHelper.TryGetEntityManager,
            (out Entity gridEntity, out GridConfig grid, out DynamicBuffer<GridRoad> roads, out DynamicBlockerComponent blockerData) =>
                TryGetRoadBuildGridData(source, out gridEntity, out grid, out roads, out blockerData),
            (originCell, footprintCells, grid) => GetRoadBuildFootprintCenter(source, originCell, footprintCells, grid),
            source.RoadBuildDependencyState.BuildingPlacementInteractionCompositionSystemHelper,
            source.RoadBuildDependencyState.BuildingPlacementInteractionContext,
            source.RoadBuildDependencyState.RuntimeBuildingEntityLinks,
            source.BuildingSpawnRandomState);
    }

    private RoadBuildEcsCompositionSystemHelper.Context CreateRoadBuildEcsContext(RoadBuildCompositionSourceCompositionSystemHelper source)
    {
        return source.RoadBuildContextCompositionSystemHelper.CreateEcsContext(CreateRoadBuildContext(source));
    }

    private RoadBuildBuildingPlacementCompositionSystemHelper.Context CreateRoadBuildPlacementContext(RoadBuildCompositionSourceCompositionSystemHelper source)
    {
        RoadBuildStartupSystem.State startupState = source.RoadBuildStartupState;
        return new RoadBuildBuildingPlacementCompositionSystemHelper.Context(
            source.RoadBuildPlacementStorageCompositionSystemHelper,
            source.RoadBuildPlacementState,
            source.RoadBuildPlacementVisualSystem,
            source.RoadBuildPlacementVisualState,
            startupState.RuntimeRoots.BuildingRoot,
            startupState.BuildPlaneY,
            startupState.PlacementOutlineWidth,
            startupState.PlacementOutlineHeight,
            startupState.PlacementValidColor,
            startupState.PlacementInvalidColor,
            (out Entity gridEntity, out GridConfig grid, out DynamicBuffer<GridRoad> roads, out DynamicBlockerComponent blockerData) =>
                TryGetRoadBuildGridData(source, out gridEntity, out grid, out roads, out blockerData),
            (Vector2 screenPosition, GridConfig grid, out Vector2Int cell) => TryGetRoadBuildGridCell(source, screenPosition, grid, out cell),
            (int x, int y, int width, int height) => IsRuntimeBlockerCell(source, x, y, width, height));
    }

    private RoadRuntimeGenerationContextCompositionSystemHelper.Context CreateRoadRuntimeGenerationContextSource(RoadBuildCompositionSourceCompositionSystemHelper source)
    {
        return new RoadRuntimeGenerationContextCompositionSystemHelper.Context(
            (out int roadCellSizeInGridCells) => TryGetRoadCellSizeInGridCellsInternal(source, out roadCellSizeInGridCells),
            source.RoadGridProjectionSystem,
            CreateRoadGridProjectionContext(source),
            (cells, isAutobahn, useAutobahnConnectorAtStart, useAutobahnConnectorAtEnd) =>
                source.RoadBuildMutationCompositionSystemHelper.CreateStroke(
                    CreateRoadBuildMutationContext(source),
                    cells,
                    isAutobahn,
                    useAutobahnConnectorAtStart,
                    useAutobahnConnectorAtEnd),
            source.RoadSpecialVisualSystem,
            CreateRoadSpecialVisualContext(source));
    }

    private bool TryGetRoadCellSizeInGridCellsInternal(RoadBuildCompositionSourceCompositionSystemHelper source, out int roadCellSizeInGridCells)
    {
        roadCellSizeInGridCells = 0;
        RoadBuildStartupSystem.State startupState = source.RoadBuildStartupState;
        if (startupState.RoadGridSize <= 0f)
            return false;
        if (!TryGetRoadBuildGridConfig(source, out GridConfig grid))
            return false;
        if (grid.CellSize <= 0f)
            return false;

        roadCellSizeInGridCells = Mathf.Max(1, Mathf.RoundToInt(startupState.RoadGridSize / grid.CellSize));
        return true;
    }

    private static bool TryGetRoadBuildGridData(
        RoadBuildCompositionSourceCompositionSystemHelper source,
        out Entity gridEntity,
        out GridConfig grid,
        out DynamicBuffer<GridRoad> roads,
        out DynamicBlockerComponent blockerData)
    {
        gridEntity = Entity.Null;
        grid = default;
        roads = default;
        blockerData = default;
        return source.RoadGridProjectionSystem != null &&
               source.RoadGridProjectionSystem.TryGetGridData(out gridEntity, out grid, out roads, out blockerData);
    }

    private static bool TryGetRoadBuildGridConfig(RoadBuildCompositionSourceCompositionSystemHelper source, out GridConfig grid)
    {
        grid = default;
        return source.RoadGridProjectionSystem != null &&
               source.RoadGridProjectionSystem.TryGetGridConfig(out grid);
    }

    private static Vector3 GetRoadBuildFootprintCenter(
        RoadBuildCompositionSourceCompositionSystemHelper source,
        Vector2Int originCell,
        Vector2Int footprintCells,
        GridConfig grid)
    {
        return new Vector3(
            grid.Origin.x + (originCell.x + footprintCells.x * 0.5f) * grid.CellSize,
            source.RoadBuildStartupState.BuildPlaneY,
            grid.Origin.z + (originCell.y + footprintCells.y * 0.5f) * grid.CellSize);
    }

    private static bool TryGetRoadBuildGridCell(
        RoadBuildCompositionSourceCompositionSystemHelper source,
        Vector2 screenPosition,
        GridConfig grid,
        out Vector2Int cell)
    {
        cell = default;
        RoadBuildStartupSystem.State startupState = source.RoadBuildStartupState;
        Camera worldCamera = startupState.WorldCamera;
        if (worldCamera == null)
            return false;

        Ray ray = worldCamera.ScreenPointToRay(screenPosition);
        Plane plane = new(Vector3.up, new Vector3(0f, startupState.BuildPlaneY, 0f));
        if (!plane.Raycast(ray, out float distance))
            return false;

        Vector3 worldPoint = ray.GetPoint(distance);
        int2 gridCell = GridUtils.WorldToCell(grid, worldPoint);
        if (!GridUtils.InBounds(gridCell, grid.Width, grid.Height))
            return false;

        cell = new Vector2Int(gridCell.x, gridCell.y);
        return true;
    }

    private RoadVisualType ResolveVisualType(
        RoadBuildCompositionSourceCompositionSystemHelper source,
        Vector2Int cell,
        TileConnectionMask mask)
    {
        return RoadVisualResolutionSystem.ResolveVisualType(CreateRoadVisualResolutionContext(source), cell, mask);
    }

    private GameObject GetPrefab(RoadBuildCompositionSourceCompositionSystemHelper source, RoadVisualType type)
    {
        return RoadVisualResolutionSystem.GetPrefab(CreateRoadVisualResolutionContext(source), type);
    }

    private bool TryGetVariant(
        RoadBuildCompositionSourceCompositionSystemHelper source,
        RoadVisualType type,
        TileConnectionMask mask,
        out VariantData variant)
    {
        return RoadVisualResolutionSystem.TryGetVariant(CreateRoadVisualResolutionContext(source), type, mask, out variant);
    }

    private bool TryGetHoveredCell(RoadBuildCompositionSourceCompositionSystemHelper source, Vector2 screenPosition, out Vector2Int cell)
    {
        cell = default;
        RoadBuildStartupSystem.State startupState = source.RoadBuildStartupState;
        Camera worldCamera = startupState.WorldCamera;
        if (worldCamera == null)
            return false;

        Ray ray = worldCamera.ScreenPointToRay(screenPosition);
        Plane plane = new(Vector3.up, new Vector3(0f, startupState.BuildPlaneY, 0f));
        if (!plane.Raycast(ray, out float distance))
            return false;

        Vector3 worldPoint = ray.GetPoint(distance);
        if (startupState.RoadGridSize <= 0f)
            return false;

        Vector3 localPoint = worldPoint - startupState.GridOrigin;
        cell = new Vector2Int(
            Mathf.FloorToInt(localPoint.x / startupState.RoadGridSize),
            Mathf.FloorToInt(localPoint.z / startupState.RoadGridSize));
        return true;
    }

    private void RemoveRuntimeBlockersUnderRoads(RoadBuildCompositionSourceCompositionSystemHelper source)
    {
        source.RoadGridProjectionSystem?.RemoveRuntimeBlockersUnderRoads(
            CreateRoadGridProjectionContext(source),
            source.RoadBuildDependencyState.RuntimeGridBlockers);
    }

    private static bool IsRuntimeBlockerCell(
        RoadBuildCompositionSourceCompositionSystemHelper source,
        int x,
        int y,
        int width,
        int height)
    {
        RuntimeGridBlockerPresentationSystemHelper runtimeGridBlockers = source.RoadBuildDependencyState.RuntimeGridBlockers;
        return runtimeGridBlockers != null && runtimeGridBlockers.IsRuntimeBlockerCell(x, y, width, height);
    }

    private static void ApplyBuildCommandMode(RoadBuildCompositionSourceCompositionSystemHelper source)
    {
        source.RoadBuildDependencyCompositionSystemHelper.ApplyBuildCommandMode(source.RoadBuildDependencyState);
    }

    private static void ClearCommandMode(RoadBuildCompositionSourceCompositionSystemHelper source)
    {
        source.RoadBuildDependencyCompositionSystemHelper.ClearCommandMode(source.RoadBuildDependencyState);
    }
}
