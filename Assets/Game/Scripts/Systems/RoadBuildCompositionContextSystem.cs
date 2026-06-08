using System.Collections.Generic;
using UnityEngine;
using RoadVisualType = RoadNetworkSystem.RoadVisualType;
using TileConnectionMask = RoadNetworkSystem.TileConnectionMask;
using VariantData = RoadVisualVariantSystem.VariantData;

internal sealed class RoadBuildCompositionContextSystem
{
    public RoadFootprintQuerySystem.Context CreateRoadFootprintQueryContext(RoadBuildCompositionSourceSystem source)
    {
        return source.RoadGridContextSystem.CreateFootprintQueryContext(CreateRoadGridContext(source));
    }

    public RoadRuntimeGenerationSystem.Context CreateRoadRuntimeGenerationContext(RoadBuildCompositionSourceSystem source)
    {
        return source.RoadRuntimeGenerationContextSystem.CreateContext(CreateRoadRuntimeGenerationContextSource(source));
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
            source.RoadMinimapEventSystem.PublishStaticMinimapChanged,
            ApplyBuildCommandMode,
            ClearCommandMode,
            () => source.RoadBuildDependencyState.BuildingPlacementInteractionSystem?.ClearSelectedBuilding(
                source.RoadBuildDependencyState.BuildingPlacementInteractionContext,
                "RoadBuild.ClearSelectedBuilding"),
            () => source.RoadBuildDependencyState.BuildingPlacementInteractionSystem?.CancelBuildingPlacement(
                source.RoadBuildDependencyState.BuildingPlacementInteractionContext),
            () => source.RoadBuildInputSystem.CancelPendingBuild(CreateRoadBuildInputContext(source)),
            () => source.RoadBuildPlacementVisualSystem.HidePlacementOutline(source.RoadBuildPlacementVisualState),
            () => source.RoadPreviewSystem.UpdatePreview(
                CreateRoadPreviewContext(source),
                source.RoadBuildInputState.IsDrawing,
                source.RoadBuildInputState.PendingStartCell,
                source.RoadBuildInputState.CurrentDragCell,
                source.RoadBuildInputState.DragFirstAxis),
            (Vector2 screenPosition, out Vector2Int cell) => TryGetHoveredCell(source, screenPosition, out cell),
            source.RoadPreviewSystem.ClearPreview,
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

    private RoadGridContextSystem.Context CreateRoadGridContext(RoadBuildCompositionSourceSystem source)
    {
        return new RoadGridContextSystem.Context(
            source.RoadNetworkSystem,
            source.RoadSpecialVisualSystem,
            source.RoadVisualVariantSystem,
            source.RoadFootprintQuerySystem,
            source.RoadBuildStartupState);
    }

    private RoadGridProjectionSystem.Context CreateRoadGridProjectionContext(RoadBuildCompositionSourceSystem source)
    {
        return source.RoadGridContextSystem.CreateGridProjectionContext(CreateRoadGridContext(source));
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
            source.RoadBuildVisualContextSystem,
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
            source.RoadVisualResolutionSystem,
            CreateRoadVisualResolutionContext(source));
    }

    private RoadChunkVisualSystem.Context CreateRoadChunkVisualContext(RoadBuildCompositionSourceSystem source)
    {
        return source.RoadBuildVisualContextSystem.CreateChunkContext(CreateRoadBuildVisualContext(source));
    }

    private RoadPreviewSystem.Context CreateRoadPreviewContext(RoadBuildCompositionSourceSystem source)
    {
        return source.RoadBuildVisualContextSystem.CreatePreviewContext(CreateRoadBuildVisualContext(source));
    }

    private RoadSpecialVisualSystem.Context CreateRoadSpecialVisualContext(RoadBuildCompositionSourceSystem source)
    {
        return source.RoadBuildVisualContextSystem.CreateSpecialContext(CreateRoadBuildVisualContext(source));
    }

    private RoadBuildMutationSystem.Context CreateRoadBuildMutationContext(RoadBuildCompositionSourceSystem source)
    {
        return new RoadBuildMutationSystem.Context(
            source.RoadNetworkSystem,
            dirtyCells => source.RoadVisualRefreshSystem.RefreshCells(CreateRoadVisualRefreshContext(source), dirtyCells),
            () => source.RoadVisualRefreshSystem.RebuildRoadStateFromCurrentTiles(CreateRoadVisualRefreshContext(source)));
    }

    private RoadBuildContextSystem.Context CreateRoadBuildContext(RoadBuildCompositionSourceSystem source)
    {
        RoadBuildGridQuerySystem.State gridState = ConfigureRoadBuildGridState(source);
        return new RoadBuildContextSystem.Context(
            source.RoadBuildEcsBoundarySystem.TryGetEntityManager,
            gridState.TryGetGridData,
            gridState.GetFootprintCenter,
            source.RoadBuildDependencyState.BuildingPlacementInteractionSystem,
            source.RoadBuildDependencyState.BuildingPlacementInteractionContext,
            source.BuildingSpawnRandomState);
    }

    private RoadBuildEcsBoundarySystem.Context CreateRoadBuildEcsContext(RoadBuildCompositionSourceSystem source)
    {
        return source.RoadBuildContextSystem.CreateEcsContext(CreateRoadBuildContext(source));
    }

    private RoadBuildGridQuerySystem.Context CreateRoadBuildGridContext(RoadBuildCompositionSourceSystem source)
    {
        return new RoadBuildGridQuerySystem.Context(
            source.RoadGridProjectionSystem,
            source.RoadBuildStartupState.WorldCamera,
            source.RoadBuildStartupState.BuildPlaneY);
    }

    private RoadBuildGridQuerySystem.State ConfigureRoadBuildGridState(RoadBuildCompositionSourceSystem source)
    {
        source.RoadBuildGridState.Configure(CreateRoadBuildGridContext(source));
        return source.RoadBuildGridState;
    }

    private RoadBuildBuildingPlacementSystem.Context CreateRoadBuildPlacementContext(RoadBuildCompositionSourceSystem source)
    {
        RoadBuildStartupSystem.State startupState = source.RoadBuildStartupState;
        RoadBuildGridQuerySystem.State gridState = ConfigureRoadBuildGridState(source);
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
            gridState.TryGetGridData,
            gridState.TryGetGridCell,
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
        if (!source.RoadBuildGridQuerySystem.TryGetGridConfig(
                CreateRoadBuildGridContext(source),
                out GridConfig grid))
            return false;
        if (grid.CellSize <= 0f)
            return false;

        roadCellSizeInGridCells = Mathf.Max(1, Mathf.RoundToInt(startupState.RoadGridSize / grid.CellSize));
        return true;
    }

    private RoadVisualType ResolveVisualType(
        RoadBuildCompositionSourceSystem source,
        Vector2Int cell,
        TileConnectionMask mask)
    {
        return source.RoadVisualResolutionSystem.ResolveVisualType(CreateRoadVisualResolutionContext(source), cell, mask);
    }

    private GameObject GetPrefab(RoadBuildCompositionSourceSystem source, RoadVisualType type)
    {
        return source.RoadVisualResolutionSystem.GetPrefab(CreateRoadVisualResolutionContext(source), type);
    }

    private bool TryGetVariant(
        RoadBuildCompositionSourceSystem source,
        RoadVisualType type,
        TileConnectionMask mask,
        out VariantData variant)
    {
        return source.RoadVisualResolutionSystem.TryGetVariant(CreateRoadVisualResolutionContext(source), type, mask, out variant);
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
        source.RoadGridProjectionSystem.RemoveRuntimeBlockersUnderRoads(
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
        RuntimeGridBlockerSystem runtimeGridBlockers = source.RoadBuildDependencyState.RuntimeGridBlockers;
        return runtimeGridBlockers != null && runtimeGridBlockers.IsRuntimeBlockerCell(x, y, width, height);
    }

    private static void ApplyBuildCommandMode()
    {
        BattleHudRuntimeFeedbackSystem.ApplyCommandMode(TacticalCommandMode.Build);
    }

    private static void ClearCommandMode()
    {
        BattleHudRuntimeFeedbackSystem.ClearCommandMode();
    }
}
