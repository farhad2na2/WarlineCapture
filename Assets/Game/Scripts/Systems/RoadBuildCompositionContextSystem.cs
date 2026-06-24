using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using RoadVisualType = RoadNetworkSystem.RoadVisualType;
using TileConnectionMask = RoadNetworkSystem.TileConnectionMask;
using VariantData = RoadVisualVariantSystem.VariantData;

internal sealed class RoadBuildCompositionContextSystem
{
    public RoadGridProjectionSystem.RoadFootprintState CreateRoadFootprintState(RoadBuildCompositionSourceSystem source)
    {
        return new RoadGridProjectionSystem.RoadFootprintState(
            source.RoadNetworkSystem.RoadTiles,
            source.RoadSpecialVisualSystem?.SpecialRoadObjects,
            source.RoadVisualVariantSystem?.VisualData ?? new Dictionary<RoadVisualType, RoadGridProjectionSystem.CombinedRoadVisualData>(),
            source.RoadBuildStartupState.GridOrigin,
            source.RoadBuildStartupState.BuildPlaneY,
            source.RoadBuildStartupState.RoadGridSize);
    }

    public RoadRuntimeGenerationSystem.Context CreateRoadRuntimeGenerationContext(RoadBuildCompositionSourceSystem source)
    {
        return RoadRuntimeGenerationContextSystem.CreateContext(CreateRoadRuntimeGenerationContextSource(source));
    }

    public RoadBuildReadModelSystem.Context CreateRoadBuildReadModelContext(RoadBuildCompositionSourceSystem source)
    {
        return new RoadBuildReadModelSystem.Context(
            source.RuntimeGameplayStateSystem,
            source.RoadBuildSessionSystem,
            source.RoadBuildSessionState,
            source.RoadBuildInputSystem,
            source.RoadBuildInputState,
            source.RoadBuildPlacementStorageSystem,
            source.RoadBuildDependencyState,
            () => source.RoadBuildPlacementState.IsDraggingBuildingPlacement);
    }

    public RoadBuildInteractionContextSystem.Context CreateRoadBuildInteractionContext(RoadBuildCompositionSourceSystem source)
    {
        return new RoadBuildInteractionContextSystem.Context(
            source.RuntimeGameplayStateSystem,
            source.RoadBuildSessionSystem,
            source.RoadBuildSessionState,
            source.RoadBuildInputSystem,
            source.RoadBuildInputState,
            source.RoadBuildCommandSystem,
            source.RoadPathPlanningSystem,
            source.RoadNetworkSystem,
            () => source.RoadBuildMutationSystem.CaptureRoadBuildSessionSnapshot(CreateRoadBuildMutationContext(source)),
            snapshot => source.RoadBuildMutationSystem.RestoreRoadBuildSession(CreateRoadBuildMutationContext(source), snapshot),
            () => RemoveRuntimeBlockersUnderRoads(source),
            () => source.RoadMinimapEventSystem?.PublishStaticMinimapChanged(),
            () => ApplyBuildCommandMode(source),
            () => ClearCommandMode(source),
            () => source.RoadBuildDependencyState.BuildingPlacementInteractionSystem?.ClearSelectedBuilding(
                source.RoadBuildDependencyState.BuildingPlacementInteractionContext,
                "RoadBuild.ClearSelectedBuilding"),
            () => source.RoadBuildDependencyState.BuildingPlacementInteractionSystem?.CancelBuildingPlacement(
                source.RoadBuildDependencyState.BuildingPlacementInteractionContext),
            () => source.RoadBuildInputSystem.CancelPendingBuild(CreateRoadBuildInputContext(source)),
            () => source.RoadBuildPlacementVisualSystem?.HidePlacementOutline(source.RoadBuildPlacementVisualState),
            () => source.RoadPreviewSystem?.UpdatePreview(
                CreateRoadPreviewContext(source),
                source.RoadBuildInputState.IsDrawing,
                source.RoadBuildInputState.PendingStartCell,
                source.RoadBuildInputState.CurrentDragCell,
                source.RoadBuildInputState.DragFirstAxis),
            (Vector2 screenPosition, out Vector2Int cell) => TryGetHoveredCell(source, screenPosition, out cell),
            () => source.RoadPreviewSystem?.ClearPreview(),
            screenPosition => source.RoadBuildBuildingPlacementSystem.UpdateBuildingPlacement(
                CreateRoadBuildPlacementContext(source),
                screenPosition),
            path => source.RoadBuildMutationSystem.CreateStroke(CreateRoadBuildMutationContext(source), path),
            path => source.RoadSurfacePlacementSystem.IsPathSurfaceValid(path),
            () => source.RoadBuildPlacementStorageSystem.HasPendingBuildingPlacement,
            value => source.RoadBuildBuildingPlacementSystem.SetDragging(source.RoadBuildPlacementState, value),
            () => source.RoadBuildBuildingPlacementSystem.SetDragging(source.RoadBuildPlacementState, false),
            strokeId => source.RoadBuildMutationSystem.DeleteStroke(CreateRoadBuildMutationContext(source), strokeId));
    }

    public RoadBuildInputSystem.Context CreateRoadBuildInputContext(RoadBuildCompositionSourceSystem source)
    {
        return source.RoadBuildInteractionContextSystem.CreateInputContext(CreateRoadBuildInteractionContext(source));
    }

    public RoadBuildCommandSystem.Context CreateRoadBuildCommandContext(RoadBuildCompositionSourceSystem source)
    {
        return source.RoadBuildInteractionContextSystem.CreateCommandContext(CreateRoadBuildInteractionContext(source));
    }

    public RoadDeletePromptSystem.Context CreateRoadDeletePromptContext(RoadBuildCompositionSourceSystem source)
    {
        return source.RoadBuildInteractionContextSystem.CreateDeletePromptContext(CreateRoadBuildInteractionContext(source));
    }

    public RoadBuildDisposalSystem.Context CreateRoadBuildDisposalContext(RoadBuildCompositionSourceSystem source)
    {
        return new RoadBuildDisposalSystem.Context(
            source.RoadBuildStartupSystem,
            source.RoadBuildStartupState,
            source.RoadRuntimeRootSystem,
            source.RoadBuildPlacementVisualSystem,
            source.RoadBuildPlacementVisualState,
            source.RoadVisualVariantSystem,
            source.RoadPreviewSystem,
            source.RoadChunkVisualSystem,
            source.RoadBuildEcsBoundarySystem,
            source.RoadBuildPlacementStorageSystem,
            source.RoadSpecialVisualSystem,
            source.RoadMinimapEventSystem,
            source.RoadGridProjectionSystem,
            source.RoadNetworkSystem.RoadTiles);
    }

    private RoadGridProjectionSystem.Context CreateRoadGridProjectionContext(RoadBuildCompositionSourceSystem source)
    {
        return new RoadGridProjectionSystem.Context(
            source.RoadNetworkSystem.RoadTiles,
            CreateRoadFootprintState(source),
            source.RoadBuildStartupState.RoadGridSize);
    }

    private RoadBuildVisualContextSystem.Context CreateRoadBuildVisualContext(RoadBuildCompositionSourceSystem source)
    {
        return new RoadBuildVisualContextSystem.Context(
            source.RoadNetworkSystem,
            source.RoadPathPlanningSystem,
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

    private RoadVisualResolutionSystem.Context CreateRoadVisualResolutionContext(RoadBuildCompositionSourceSystem source)
    {
        return new RoadVisualResolutionSystem.Context(
            source.RoadNetworkSystem,
            source.RoadVisualVariantSystem,
            CreateRoadBuildVisualContext(source));
    }

    private RoadVisualRefreshSystem.Context CreateRoadVisualRefreshContext(RoadBuildCompositionSourceSystem source)
    {
        return new RoadVisualRefreshSystem.Context(
            source.RoadNetworkSystem,
            source.RoadGridProjectionSystem,
            CreateRoadGridProjectionContext(source),
            source.RoadChunkVisualSystem,
            CreateRoadChunkVisualContext(source),
            source.RoadSpecialVisualSystem,
            CreateRoadSpecialVisualContext(source),
            CreateRoadVisualResolutionContext(source));
    }

    private RoadChunkVisualSystem.Context CreateRoadChunkVisualContext(RoadBuildCompositionSourceSystem source)
    {
        return RoadBuildVisualContextSystem.CreateChunkContext(CreateRoadBuildVisualContext(source));
    }

    private RoadPreviewSystem.Context CreateRoadPreviewContext(RoadBuildCompositionSourceSystem source)
    {
        return RoadBuildVisualContextSystem.CreatePreviewContext(CreateRoadBuildVisualContext(source));
    }

    private RoadSpecialVisualSystem.Context CreateRoadSpecialVisualContext(RoadBuildCompositionSourceSystem source)
    {
        return RoadBuildVisualContextSystem.CreateSpecialContext(CreateRoadBuildVisualContext(source));
    }

    private RoadBuildMutationSystem.Context CreateRoadBuildMutationContext(RoadBuildCompositionSourceSystem source)
    {
        return new RoadBuildMutationSystem.Context(
            source.RoadNetworkSystem,
            dirtyCells => RoadVisualRefreshSystem.RefreshCells(CreateRoadVisualRefreshContext(source), dirtyCells),
            () => RoadVisualRefreshSystem.RebuildRoadStateFromCurrentTiles(CreateRoadVisualRefreshContext(source)));
    }

    private RoadBuildContextSystem.Context CreateRoadBuildContext(RoadBuildCompositionSourceSystem source)
    {
        return new RoadBuildContextSystem.Context(
            source.RoadBuildEcsBoundarySystem.TryGetEntityManager,
            (out Entity gridEntity, out GridConfig grid, out DynamicBuffer<GridRoad> roads, out DynamicBlockerComponent blockerData) =>
                TryGetRoadBuildGridData(source, out gridEntity, out grid, out roads, out blockerData),
            (originCell, footprintCells, grid) => GetRoadBuildFootprintCenter(source, originCell, footprintCells, grid),
            source.RoadBuildDependencyState.BuildingPlacementInteractionSystem,
            source.RoadBuildDependencyState.BuildingPlacementInteractionContext,
            source.RoadBuildDependencyState.RuntimeBuildingEntityLinks,
            source.BuildingSpawnRandomState);
    }

    private RoadBuildEcsBoundarySystem.Context CreateRoadBuildEcsContext(RoadBuildCompositionSourceSystem source)
    {
        return source.RoadBuildContextSystem.CreateEcsContext(CreateRoadBuildContext(source));
    }

    private RoadBuildBuildingPlacementSystem.Context CreateRoadBuildPlacementContext(RoadBuildCompositionSourceSystem source)
    {
        RoadBuildStartupSystem.State startupState = source.RoadBuildStartupState;
        return new RoadBuildBuildingPlacementSystem.Context(
            source.RoadBuildPlacementStorageSystem,
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

    private RoadRuntimeGenerationContextSystem.Context CreateRoadRuntimeGenerationContextSource(RoadBuildCompositionSourceSystem source)
    {
        return new RoadRuntimeGenerationContextSystem.Context(
            (out int roadCellSizeInGridCells) => TryGetRoadCellSizeInGridCellsInternal(source, out roadCellSizeInGridCells),
            source.RoadGridProjectionSystem,
            CreateRoadGridProjectionContext(source),
            (cells, isAutobahn, useAutobahnConnectorAtStart, useAutobahnConnectorAtEnd) =>
                source.RoadBuildMutationSystem.CreateStroke(
                    CreateRoadBuildMutationContext(source),
                    cells,
                    isAutobahn,
                    useAutobahnConnectorAtStart,
                    useAutobahnConnectorAtEnd),
            source.RoadSpecialVisualSystem,
            CreateRoadSpecialVisualContext(source));
    }

    private bool TryGetRoadCellSizeInGridCellsInternal(RoadBuildCompositionSourceSystem source, out int roadCellSizeInGridCells)
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
        RoadBuildCompositionSourceSystem source,
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

    private static bool TryGetRoadBuildGridConfig(RoadBuildCompositionSourceSystem source, out GridConfig grid)
    {
        grid = default;
        return source.RoadGridProjectionSystem != null &&
               source.RoadGridProjectionSystem.TryGetGridConfig(out grid);
    }

    private static Vector3 GetRoadBuildFootprintCenter(
        RoadBuildCompositionSourceSystem source,
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
        RoadBuildCompositionSourceSystem source,
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
        RoadBuildCompositionSourceSystem source,
        Vector2Int cell,
        TileConnectionMask mask)
    {
        return RoadVisualResolutionSystem.ResolveVisualType(CreateRoadVisualResolutionContext(source), cell, mask);
    }

    private GameObject GetPrefab(RoadBuildCompositionSourceSystem source, RoadVisualType type)
    {
        return RoadVisualResolutionSystem.GetPrefab(CreateRoadVisualResolutionContext(source), type);
    }

    private bool TryGetVariant(
        RoadBuildCompositionSourceSystem source,
        RoadVisualType type,
        TileConnectionMask mask,
        out VariantData variant)
    {
        return RoadVisualResolutionSystem.TryGetVariant(CreateRoadVisualResolutionContext(source), type, mask, out variant);
    }

    private bool TryGetHoveredCell(RoadBuildCompositionSourceSystem source, Vector2 screenPosition, out Vector2Int cell)
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

    private void RemoveRuntimeBlockersUnderRoads(RoadBuildCompositionSourceSystem source)
    {
        source.RoadGridProjectionSystem?.RemoveRuntimeBlockersUnderRoads(
            CreateRoadGridProjectionContext(source),
            source.RoadBuildDependencyState.RuntimeGridBlockers);
    }

    private static bool IsRuntimeBlockerCell(
        RoadBuildCompositionSourceSystem source,
        int x,
        int y,
        int width,
        int height)
    {
        RuntimeGridBlockerPresentationSystemHelper runtimeGridBlockers = source.RoadBuildDependencyState.RuntimeGridBlockers;
        return runtimeGridBlockers != null && runtimeGridBlockers.IsRuntimeBlockerCell(x, y, width, height);
    }

    private static void ApplyBuildCommandMode(RoadBuildCompositionSourceSystem source)
    {
        source.RoadBuildDependencySystem.ApplyBuildCommandMode(source.RoadBuildDependencyState);
    }

    private static void ClearCommandMode(RoadBuildCompositionSourceSystem source)
    {
        source.RoadBuildDependencySystem.ClearCommandMode(source.RoadBuildDependencyState);
    }
}
