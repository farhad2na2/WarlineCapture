using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using CombinedRoadVisualData = RoadFootprintQuerySystem.CombinedRoadVisualData;
using EdgeKey = RoadNetworkSystem.EdgeKey;
using RoadTileData = RoadNetworkSystem.RoadTileData;
using VariantData = RoadVisualVariantSystem.VariantData;
using RoadVisualType = RoadNetworkSystem.RoadVisualType;
using StrokeData = RoadNetworkSystem.StrokeData;
using TileConnectionMask = RoadNetworkSystem.TileConnectionMask;

internal sealed class RoadBuildRuntimeStateSystem
{
    private readonly RoadBuildCompositionSourceSystem _source;

    private RuntimeGameplayStateSystem _runtimeGameplayStateSystem => _source.RuntimeGameplayStateSystem;
    private RoadBuildStartupSystem _roadBuildStartupSystem => _source.RoadBuildStartupSystem;
    private RoadBuildDependencySystem _roadBuildDependencySystem => _source.RoadBuildDependencySystem;
    private RoadBuildReadModelSystem _roadBuildReadModelSystem => _source.RoadBuildReadModelSystem;
    private RoadBuildVisualContextSystem _roadBuildVisualContextSystem => _source.RoadBuildVisualContextSystem;
    private RoadBuildInteractionContextSystem _roadBuildInteractionContextSystem => _source.RoadBuildInteractionContextSystem;
    private RoadBuildRuntimeActionSystem _roadBuildRuntimeActionSystem => _source.RoadBuildRuntimeActionSystem;
    private RoadBuildRuntimeActionSystem.State _roadBuildRuntimeActionState => _source.RoadBuildRuntimeActionState;
    private RoadBuildDisposalSystem _roadBuildDisposalSystem => _source.RoadBuildDisposalSystem;
    private RoadGridContextSystem _roadGridContextSystem => _source.RoadGridContextSystem;
    private RoadBuildConfigSystem _roadBuildConfigSystem => _source.RoadBuildConfigSystem;
    private RoadRuntimeRootSystem _roadRuntimeRootSystem => _source.RoadRuntimeRootSystem;
    private RoadNetworkSystem _roadNetworkSystem => _source.RoadNetworkSystem;
    private RoadPathPlanningSystem _roadPathPlanningSystem => _source.RoadPathPlanningSystem;
    private RoadFootprintQuerySystem _roadFootprintQuerySystem => _source.RoadFootprintQuerySystem;
    private RoadGridProjectionSystem _roadGridProjectionSystem => _source.RoadGridProjectionSystem;
    private RoadVisualVariantSystem _roadVisualVariantSystem => _source.RoadVisualVariantSystem;
    private RoadVisualResolutionSystem _roadVisualResolutionSystem => _source.RoadVisualResolutionSystem;
    private RoadVisualRefreshSystem _roadVisualRefreshSystem => _source.RoadVisualRefreshSystem;
    private RoadChunkVisualSystem _roadChunkVisualSystem => _source.RoadChunkVisualSystem;
    private RoadPreviewSystem _roadPreviewSystem => _source.RoadPreviewSystem;
    private RoadSpecialVisualSystem _roadSpecialVisualSystem => _source.RoadSpecialVisualSystem;
    private RoadBuildSessionSystem _roadBuildSessionSystem => _source.RoadBuildSessionSystem;
    private RoadBuildSessionSystem.State _roadBuildSessionState => _source.RoadBuildSessionState;
    private RoadMinimapEventSystem _roadMinimapEventSystem => _source.RoadMinimapEventSystem;
    private RoadBuildInputSystem _roadBuildInputSystem => _source.RoadBuildInputSystem;
    private RoadBuildInputSystem.State _roadBuildInputState => _source.RoadBuildInputState;
    private RoadBuildCommandSystem _roadBuildCommandSystem => _source.RoadBuildCommandSystem;
    private RoadDeletePromptSystem _roadDeletePromptSystem => _source.RoadDeletePromptSystem;
    private BuildingRoadLegacyStorageSystem _buildingRoadLegacyStorageSystem => _source.BuildingRoadLegacyStorageSystem;
    private BuildingRoadLegacyDefinitionSystem _buildingRoadLegacyDefinitionSystem => _source.BuildingRoadLegacyDefinitionSystem;
    private BuildingRoadLegacyPlacementVisualSystem _buildingRoadLegacyPlacementVisualSystem => _source.BuildingRoadLegacyPlacementVisualSystem;
    private BuildingRoadLegacyPlacementSystem _buildingRoadLegacyPlacementSystem => _source.BuildingRoadLegacyPlacementSystem;
    private BuildingRoadLegacyInteractionSystem _buildingRoadLegacyInteractionSystem => _source.BuildingRoadLegacyInteractionSystem;
    private BuildingRoadLegacyGridSystem _buildingRoadLegacyGridSystem => _source.BuildingRoadLegacyGridSystem;
    private BuildingRoadLegacyContextSystem _buildingRoadLegacyContextSystem => _source.BuildingRoadLegacyContextSystem;
    private BuildingRoadLegacyEcsSystem _buildingRoadLegacyEcsSystem => _source.BuildingRoadLegacyEcsSystem;
    private RoadRuntimeGenerationSystem _roadRuntimeGenerationSystem => _source.RoadRuntimeGenerationSystem;
    private RoadRuntimeGenerationContextSystem _roadRuntimeGenerationContextSystem => _source.RoadRuntimeGenerationContextSystem;
    private RoadBuildMutationSystem _roadBuildMutationSystem => _source.RoadBuildMutationSystem;

    private RoadBuildStartupSystem.State _startupState = new();
    private RoadBuildDependencySystem.State _dependencyState;
    private BuildingRoadLegacyPlacementVisualSystem.State _placementVisualState;
    private BuildingRoadLegacyPlacementSystem.State _buildingPlacementState;
    private BuildingRoadLegacyGridSystem.State _buildingGridState;
    private uint _buildingSpawnRandomState = 0x12345678u;

    private static readonly Vector2Int North = new(0, 1), East = new(1, 0), South = new(0, -1), West = new(-1, 0);

    public RoadBuildRuntimeStateSystem(RoadBuildCompositionSourceSystem source)
    {
        _source = source;
        _dependencyState = _roadBuildDependencySystem.CreateState();
        _placementVisualState = _buildingRoadLegacyPlacementVisualSystem.CreateState();
        _buildingPlacementState = _buildingRoadLegacyPlacementSystem.CreateState();
        _buildingGridState = _buildingRoadLegacyGridSystem.CreateState();
    }

    private Dictionary<EdgeKey, int> _edgeCounts => _roadNetworkSystem.EdgeCounts;
    private Dictionary<Vector2Int, List<int>> _strokeIdsByCell => _roadNetworkSystem.StrokeIdsByCell;
    private Dictionary<int, StrokeData> _strokes => _roadNetworkSystem.Strokes;
    private Dictionary<Vector2Int, RoadTileData> _roadTiles => _roadNetworkSystem.RoadTiles;
    private Dictionary<RoadVisualType, CombinedRoadVisualData> _visualData => _roadVisualVariantSystem.VisualData;
    private Dictionary<Vector2Int, GameObject> _specialRoadObjects => _roadSpecialVisualSystem.SpecialRoadObjects;

    private Camera worldCamera => _startupState.WorldCamera;
    private GameObject straightPrefab => _startupState.StraightPrefab;
    private GameObject tIntersectionPrefab => _startupState.TIntersectionPrefab;
    private GameObject intersectionPrefab => _startupState.IntersectionPrefab;
    private GameObject endPrefab => _startupState.EndPrefab;
    private GameObject cornerPrefab => _startupState.CornerPrefab;
    private GameObject autobahnPrefab => _startupState.AutobahnPrefab;
    private GameObject autobahnConnectPrefab => _startupState.AutobahnConnectPrefab;
    private Vector3 gridOrigin => _startupState.GridOrigin;
    private float buildPlaneY => _startupState.BuildPlaneY;
    private float roadGridSize => _startupState.RoadGridSize;
    private int chunkSizeInCells => _startupState.ChunkSizeInCells;
    private float previewAlpha => _startupState.PreviewAlpha;
    private GameObject soldierBasePrefab => _startupState.SoldierBasePrefab;
    private Vector2Int soldierBaseFootprintCells => _startupState.SoldierBaseFootprintCells;
    private float placementOutlineHeight => _startupState.PlacementOutlineHeight;
    private float placementOutlineWidth => _startupState.PlacementOutlineWidth;
    private Color placementValidColor => _startupState.PlacementValidColor;
    private Color placementInvalidColor => _startupState.PlacementInvalidColor;
    private Transform RoadRoot => _startupState.RuntimeRoots.RoadRoot;
    private Transform SpecialRoadRoot => _startupState.RuntimeRoots.SpecialRoadRoot;
    private Transform SpecialRoadConnectorRoot => _startupState.RuntimeRoots.SpecialRoadConnectorRoot;
    private Transform DebugStraightRoadRoot => _startupState.RuntimeRoots.DebugStraightRoadRoot;
    private Transform BuildingRoot => _startupState.RuntimeRoots.BuildingRoot;
    private Transform RuntimeRoot => _startupState.RuntimeRoot;
    private BuildingPlacementInteractionSystem BuildingPlacementInteraction => _dependencyState.BuildingPlacementInteractionSystem;
    private BuildingPlacementInteractionSystem.Context BuildingPlacementInteractionContext => _dependencyState.BuildingPlacementInteractionContext;
    private RuntimeGridBlockerSystem RuntimeGridBlockers => _dependencyState.RuntimeGridBlockers;

    internal RoadRuntimeGenerationSystem RoadRuntimeGenerationSystem => _roadRuntimeGenerationSystem;
    internal RoadRuntimeGenerationSystem.Context RoadRuntimeGenerationContext => CreateRoadRuntimeGenerationContext();
    internal RoadFootprintQuerySystem RoadFootprintQuerySystem => _roadFootprintQuerySystem;
    internal RoadFootprintQuerySystem.Context RoadFootprintQueryContext => CreateRoadFootprintQueryContext();
    private RoadGridContextSystem.Context CreateRoadGridContext()
    {
        return new RoadGridContextSystem.Context(
            _roadNetworkSystem,
            _roadSpecialVisualSystem,
            _roadVisualVariantSystem,
            _roadFootprintQuerySystem,
            _startupState);
    }

    private RoadFootprintQuerySystem.Context CreateRoadFootprintQueryContext()
    {
        return _roadGridContextSystem.CreateFootprintQueryContext(CreateRoadGridContext());
    }

    private RoadGridProjectionSystem.Context CreateRoadGridProjectionContext()
    {
        return _roadGridContextSystem.CreateGridProjectionContext(CreateRoadGridContext());
    }

    private RoadBuildVisualContextSystem.Context CreateRoadBuildVisualContext()
    {
        return new RoadBuildVisualContextSystem.Context(
            _roadNetworkSystem,
            _roadPathPlanningSystem,
            _roadVisualVariantSystem,
            _roadBuildStartupSystem,
            _startupState,
            ResolveVisualType,
            TryGetVariant,
            GetPrefab,
            TryGetVariant);
    }

    private RoadVisualResolutionSystem.Context CreateRoadVisualResolutionContext()
    {
        return new RoadVisualResolutionSystem.Context(
            _roadNetworkSystem,
            _roadVisualVariantSystem,
            _roadBuildVisualContextSystem,
            CreateRoadBuildVisualContext());
    }

    private RoadVisualRefreshSystem.Context CreateRoadVisualRefreshContext()
    {
        return new RoadVisualRefreshSystem.Context(
            _roadNetworkSystem,
            _roadGridProjectionSystem,
            CreateRoadGridProjectionContext(),
            _roadChunkVisualSystem,
            CreateRoadChunkVisualContext(),
            _roadSpecialVisualSystem,
            CreateRoadSpecialVisualContext(),
            _roadVisualResolutionSystem,
            CreateRoadVisualResolutionContext());
    }

    private RoadChunkVisualSystem.Context CreateRoadChunkVisualContext()
    {
        return _roadBuildVisualContextSystem.CreateChunkContext(CreateRoadBuildVisualContext());
    }

    private RoadPreviewSystem.Context CreateRoadPreviewContext()
    {
        return _roadBuildVisualContextSystem.CreatePreviewContext(CreateRoadBuildVisualContext());
    }

    private RoadSpecialVisualSystem.Context CreateRoadSpecialVisualContext()
    {
        return _roadBuildVisualContextSystem.CreateSpecialContext(CreateRoadBuildVisualContext());
    }

    private RoadBuildMutationSystem.Context CreateRoadBuildMutationContext()
    {
        return new RoadBuildMutationSystem.Context(
            _roadNetworkSystem,
            dirtyCells => _roadVisualRefreshSystem.RefreshCells(CreateRoadVisualRefreshContext(), dirtyCells),
            () => _roadVisualRefreshSystem.RebuildRoadStateFromCurrentTiles(CreateRoadVisualRefreshContext()));
    }

    private RoadBuildInteractionContextSystem.Context CreateRoadBuildInteractionContext()
    {
        return new RoadBuildInteractionContextSystem.Context(
            _runtimeGameplayStateSystem,
            _roadBuildSessionSystem,
            _roadBuildSessionState,
            _roadBuildInputSystem,
            _roadBuildInputState,
            _roadBuildCommandSystem,
            _roadPathPlanningSystem,
            _roadNetworkSystem,
            () => _roadBuildMutationSystem.CaptureRoadBuildSessionSnapshot(CreateRoadBuildMutationContext()),
            snapshot => _roadBuildMutationSystem.RestoreRoadBuildSession(CreateRoadBuildMutationContext(), snapshot),
            RemoveRuntimeBlockersUnderRoads,
            _roadMinimapEventSystem.PublishStaticMinimapChanged,
            ApplyBuildCommandMode,
            ClearCommandMode,
            () => BuildingPlacementInteraction?.ClearSelectedBuilding(
                BuildingPlacementInteractionContext,
                "RoadBuild.ClearSelectedBuilding"),
            () => BuildingPlacementInteraction?.CancelBuildingPlacement(BuildingPlacementInteractionContext),
            CancelPendingBuild,
            HidePlacementOutline,
            UpdatePreview,
            TryGetHoveredCell,
            ClearPreview,
            UpdateBuildingPlacement,
            path => _roadBuildMutationSystem.CreateStroke(CreateRoadBuildMutationContext(), path),
            () => _buildingRoadLegacyStorageSystem.HasPendingBuildingPlacement,
            value => _buildingRoadLegacyPlacementSystem.SetDragging(_buildingPlacementState, value),
            ClearRoadBuildDragState,
            strokeId => _roadBuildMutationSystem.DeleteStroke(CreateRoadBuildMutationContext(), strokeId));
    }

    private RoadBuildSessionSystem.Context CreateRoadBuildSessionContext()
    {
        return _roadBuildInteractionContextSystem.CreateSessionContext(CreateRoadBuildInteractionContext());
    }

    private RoadBuildInputSystem.Context CreateRoadBuildInputContext()
    {
        return _roadBuildInteractionContextSystem.CreateInputContext(CreateRoadBuildInteractionContext());
    }

    private RoadBuildCommandSystem.Context CreateRoadBuildCommandContext()
    {
        return _roadBuildInteractionContextSystem.CreateCommandContext(CreateRoadBuildInteractionContext());
    }

    private RoadDeletePromptSystem.Context CreateRoadDeletePromptContext()
    {
        return _roadBuildInteractionContextSystem.CreateDeletePromptContext(CreateRoadBuildInteractionContext());
    }

    private RoadBuildDisposalSystem.Context CreateRoadBuildDisposalContext()
    {
        return new RoadBuildDisposalSystem.Context(
            _roadBuildStartupSystem,
            _startupState,
            _roadRuntimeRootSystem,
            _buildingRoadLegacyPlacementVisualSystem,
            _placementVisualState,
            _roadVisualVariantSystem,
            _roadPreviewSystem,
            _roadChunkVisualSystem,
            _buildingRoadLegacyEcsSystem,
            _buildingRoadLegacyStorageSystem,
            _roadSpecialVisualSystem,
            _roadMinimapEventSystem,
            _roadGridProjectionSystem,
            _roadTiles);
    }

    private BuildingRoadLegacyContextSystem.Context CreateBuildingRoadLegacyContext()
    {
        return new BuildingRoadLegacyContextSystem.Context(
            _buildingRoadLegacyEcsSystem.TryGetEntityManager,
            ConfigureBuildingRoadLegacyGridState().TryGetGridData,
            ConfigureBuildingRoadLegacyGridState().GetFootprintCenter,
            BuildingPlacementInteraction,
            BuildingPlacementInteractionContext,
            _buildingSpawnRandomState);
    }

    private BuildingRoadLegacyEcsSystem.Context CreateBuildingRoadLegacyEcsContext()
    {
        return _buildingRoadLegacyContextSystem.CreateEcsContext(CreateBuildingRoadLegacyContext());
    }

    private BuildingRoadLegacyInteractionSystem.Context CreateBuildingRoadLegacyInteractionContext()
    {
        return new BuildingRoadLegacyInteractionSystem.Context(
            _buildingRoadLegacyStorageSystem,
            _buildingRoadLegacyEcsSystem,
            CreateBuildingRoadLegacyEcsContext(),
            RuntimeGridBlockers,
            _buildingRoadLegacyEcsSystem.TryGetEntityManager,
            ConfigureBuildingRoadLegacyGridState().TryGetGridData,
            ConfigureBuildingRoadLegacyGridState().TryGetGridCell,
            _roadBuildInputSystem.IsPointerOverUI);
    }

    private BuildingRoadLegacyGridSystem.Context CreateBuildingRoadLegacyGridContext()
    {
        return new BuildingRoadLegacyGridSystem.Context(
            _roadGridProjectionSystem,
            worldCamera,
            buildPlaneY);
    }

    private BuildingRoadLegacyGridSystem.State ConfigureBuildingRoadLegacyGridState()
    {
        _buildingGridState.Configure(CreateBuildingRoadLegacyGridContext());
        return _buildingGridState;
    }

    private RoadRuntimeGenerationContextSystem.Context CreateRoadRuntimeGenerationContextSource()
    {
        return new RoadRuntimeGenerationContextSystem.Context(
            TryGetRoadCellSizeInGridCellsInternal,
            _roadGridProjectionSystem,
            CreateRoadGridProjectionContext(),
            (cells, isAutobahn, useAutobahnConnectorAtStart, useAutobahnConnectorAtEnd) =>
                _roadBuildMutationSystem.CreateStroke(
                    CreateRoadBuildMutationContext(),
                    cells,
                    isAutobahn,
                    useAutobahnConnectorAtStart,
                    useAutobahnConnectorAtEnd),
            _roadSpecialVisualSystem,
            CreateRoadSpecialVisualContext());
    }

    private RoadRuntimeGenerationSystem.Context CreateRoadRuntimeGenerationContext()
    {
        return _roadRuntimeGenerationContextSystem.CreateContext(CreateRoadRuntimeGenerationContextSource());
    }

    private RoadBuildReadModelSystem.Context CreateRoadBuildReadModelContext()
    {
        return new RoadBuildReadModelSystem.Context(
            _runtimeGameplayStateSystem,
            _roadBuildSessionSystem,
            _roadBuildSessionState,
            _roadBuildInputSystem,
            _roadBuildInputState,
            _buildingRoadLegacyStorageSystem,
            _dependencyState,
            () => _buildingPlacementState.IsDraggingBuildingPlacement);
    }

    private BuildingRoadLegacyPlacementSystem.Context CreateBuildingRoadLegacyPlacementContext()
    {
        return new BuildingRoadLegacyPlacementSystem.Context(
            _buildingRoadLegacyStorageSystem,
            _buildingPlacementState,
            _buildingRoadLegacyPlacementVisualSystem,
            _placementVisualState,
            BuildingRoot,
            buildPlaneY,
            placementOutlineWidth,
            placementOutlineHeight,
            placementValidColor,
            placementInvalidColor,
            ConfigureBuildingRoadLegacyGridState().TryGetGridData,
            ConfigureBuildingRoadLegacyGridState().TryGetGridCell,
            IsRuntimeBlockerCell);
    }

    private bool TryGetRoadCellSizeInGridCellsInternal(out int roadCellSizeInGridCells)
    {
        roadCellSizeInGridCells = 0;
        if (roadGridSize <= 0f)
            return false;
        if (!_buildingRoadLegacyGridSystem.TryGetGridConfig(CreateBuildingRoadLegacyGridContext(), out GridConfig grid))
            return false;
        if (grid.CellSize <= 0f)
            return false;

        roadCellSizeInGridCells = Mathf.Max(1, Mathf.RoundToInt(roadGridSize / grid.CellSize));
        return true;
    }

    public void Init(
        RoadBuildSystemConfig configAsset,
        Camera sceneWorldCamera,
        Transform runtimeRoot,
        BuildingPlacementInteractionSystem buildingPlacementInteractionSystem,
        BuildingPlacementInteractionSystem.Context buildingPlacementInteractionContext = default)
    {
        _startupState = _roadBuildStartupSystem.Initialize(
            configAsset,
            sceneWorldCamera,
            runtimeRoot,
            _roadBuildConfigSystem,
            _roadRuntimeRootSystem,
            _roadVisualVariantSystem);
        _roadBuildDependencySystem.BindBuildingInteraction(
            _dependencyState,
            buildingPlacementInteractionSystem,
            buildingPlacementInteractionContext);
        _roadBuildReadModelSystem.Configure(CreateRoadBuildReadModelContext());
        _roadBuildRuntimeActionSystem.ConfigureInput(
            _roadBuildRuntimeActionState,
            _roadBuildInteractionContextSystem,
            CreateRoadBuildInteractionContext(),
            worldCamera);
        _roadBuildRuntimeActionSystem.ConfigureGui(
            _roadBuildRuntimeActionState,
            _roadDeletePromptSystem,
            CreateRoadDeletePromptContext());

        _buildingRoadLegacyDefinitionSystem.BuildDefinitions(
            soldierBasePrefab,
            soldierBaseFootprintCells,
            _buildingRoadLegacyStorageSystem);
        _buildingRoadLegacyPlacementVisualSystem.CreatePlacementOutline(
            _placementVisualState,
            RuntimeRoot,
            placementValidColor);
    }

    public void BindDependencies(
        BuildingPlacementInteractionSystem buildingPlacementInteractionSystem,
        BuildingPlacementInteractionSystem.Context buildingPlacementInteractionContext = default,
        MainMenuPlayUI mainMenuPlayUi = null,
        RuntimeGridBlockerSystem runtimeGridBlockers = null)
    {
        _roadBuildDependencySystem.BindDependencies(
            _dependencyState,
            buildingPlacementInteractionSystem,
            buildingPlacementInteractionContext,
            mainMenuPlayUi,
            runtimeGridBlockers,
            _roadMinimapEventSystem);
    }

    public void Dispose()
    {
        ExitBuildMode();
        _roadBuildSessionSystem.ResetSkipBuildClickFrames(_roadBuildSessionState);
        _roadBuildDisposalSystem.Dispose(CreateRoadBuildDisposalContext());
    }

    private void ExitBuildMode()
    {
        _roadBuildCommandSystem.ExitBuildMode(CreateRoadBuildCommandContext());
    }

    private void ClearRoadBuildDragState()
    {
        _buildingRoadLegacyPlacementSystem.SetDragging(_buildingPlacementState, false);
    }

    private RoadVisualType ResolveVisualType(Vector2Int cell, TileConnectionMask mask)
    {
        return _roadVisualResolutionSystem.ResolveVisualType(CreateRoadVisualResolutionContext(), cell, mask);
    }

    private GameObject GetPrefab(RoadVisualType type)
    {
        return _roadVisualResolutionSystem.GetPrefab(CreateRoadVisualResolutionContext(), type);
    }

    private bool TryGetVariant(RoadVisualType type, TileConnectionMask mask, out VariantData variant)
    {
        return _roadVisualResolutionSystem.TryGetVariant(CreateRoadVisualResolutionContext(), type, mask, out variant);
    }

    private void ApplyPivotPlacement(Transform target, Vector2Int cell, VariantData variant)
    {
        Vector3 basePosition = gridOrigin + new Vector3(cell.x * roadGridSize, buildPlaneY, cell.y * roadGridSize);
        target.SetPositionAndRotation(basePosition, variant.Rotation);
        target.localScale = variant.Scale;
    }

    private void ClearPreview()
    {
        _roadPreviewSystem.ClearPreview();
    }

    private void UpdatePreview()
    {
        _roadPreviewSystem.UpdatePreview(
            CreateRoadPreviewContext(),
            _roadBuildInputState.IsDrawing,
            _roadBuildInputState.PendingStartCell,
            _roadBuildInputState.CurrentDragCell,
            _roadBuildInputState.DragFirstAxis);
    }

    private Vector3 GetPlacementPosition(Vector2Int cell, VariantData variant)
    {
        return RoadChunkVisualSystem.GetPlacementPosition(CreateRoadChunkVisualContext(), cell, variant);
    }

    private void CancelPendingBuild()
    {
        _roadBuildInputSystem.CancelPendingBuild(CreateRoadBuildInputContext());
    }

    private static void ApplyBuildCommandMode()
    {
        BattleHudGameplayBridge.ResolveActive()?.ApplyCommandMode(TacticalCommandMode.Build);
    }

    private static void ClearCommandMode()
    {
        BattleHudGameplayBridge.ResolveActive()?.ClearCommandMode();
    }

    private void BeginBuildingPlacement(BuildingDefinition definition)
    {
        _buildingRoadLegacyPlacementSystem.BeginBuildingPlacement(CreateBuildingRoadLegacyPlacementContext(), definition);
    }

    private void CancelBuildingPlacementInternal()
    {
        _buildingRoadLegacyPlacementSystem.CancelBuildingPlacement(CreateBuildingRoadLegacyPlacementContext());
    }

    private void UpdateBuildingPlacement(Vector2 screenPosition)
    {
        _buildingRoadLegacyPlacementSystem.UpdateBuildingPlacement(CreateBuildingRoadLegacyPlacementContext(), screenPosition);
    }

    private void HidePlacementOutline()
    {
        _buildingRoadLegacyPlacementVisualSystem.HidePlacementOutline(_placementVisualState);
    }

    private bool IsRuntimeBlockerCell(int x, int y, int width, int height)
    {
        return RuntimeGridBlockers != null && RuntimeGridBlockers.IsRuntimeBlockerCell(x, y, width, height);
    }

    private bool TryGetHoveredCell(Vector2 screenPosition, out Vector2Int cell)
    {
        cell = default;
        Ray ray = worldCamera.ScreenPointToRay(screenPosition);
        Plane plane = new(Vector3.up, new Vector3(0f, buildPlaneY, 0f));
        if (!plane.Raycast(ray, out float distance))
            return false;

        Vector3 worldPoint = ray.GetPoint(distance);
        if (roadGridSize <= 0f)
            return false;

        Vector3 localPoint = worldPoint - gridOrigin;
        cell = new Vector2Int(
            Mathf.FloorToInt(localPoint.x / roadGridSize),
            Mathf.FloorToInt(localPoint.z / roadGridSize));
        return true;
    }

    private void RemoveRuntimeBlockersUnderRoads()
    {
        _roadGridProjectionSystem.RemoveRuntimeBlockersUnderRoads(
            CreateRoadGridProjectionContext(),
            RuntimeGridBlockers);
    }

}
