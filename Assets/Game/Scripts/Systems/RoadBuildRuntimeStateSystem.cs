using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using static UnityEngine.Object;
using CombinedRoadVisualData = RoadFootprintQuerySystem.CombinedRoadVisualData;
using EdgeKey = RoadNetworkSystem.EdgeKey;
using RoadTileData = RoadNetworkSystem.RoadTileData;
using VariantData = RoadVisualVariantSystem.VariantData;
using RoadVisualType = RoadNetworkSystem.RoadVisualType;
using StrokeData = RoadNetworkSystem.StrokeData;
using TileConnectionMask = RoadNetworkSystem.TileConnectionMask;
using PlacementState = BuildingPlacementLifecycleSystem.PlacementState;

internal sealed class RoadBuildRuntimeStateSystem
{
    private readonly RoadBuildCompositionSourceSystem _source;

    private RuntimeGameplayStateSystem _runtimeGameplayStateSystem => _source.RuntimeGameplayStateSystem;
    private RoadBuildStartupSystem _roadBuildStartupSystem => _source.RoadBuildStartupSystem;
    private RoadBuildDependencySystem _roadBuildDependencySystem => _source.RoadBuildDependencySystem;
    private RoadBuildReadModelSystem _roadBuildReadModelSystem => _source.RoadBuildReadModelSystem;
    private RoadBuildVisualContextSystem _roadBuildVisualContextSystem => _source.RoadBuildVisualContextSystem;
    private RoadBuildInteractionContextSystem _roadBuildInteractionContextSystem => _source.RoadBuildInteractionContextSystem;
    private RoadGridContextSystem _roadGridContextSystem => _source.RoadGridContextSystem;
    private RoadBuildConfigSystem _roadBuildConfigSystem => _source.RoadBuildConfigSystem;
    private RoadRuntimeRootSystem _roadRuntimeRootSystem => _source.RoadRuntimeRootSystem;
    private RoadNetworkSystem _roadNetworkSystem => _source.RoadNetworkSystem;
    private RoadPathPlanningSystem _roadPathPlanningSystem => _source.RoadPathPlanningSystem;
    private RoadFootprintQuerySystem _roadFootprintQuerySystem => _source.RoadFootprintQuerySystem;
    private RoadGridProjectionSystem _roadGridProjectionSystem => _source.RoadGridProjectionSystem;
    private RoadVisualVariantSystem _roadVisualVariantSystem => _source.RoadVisualVariantSystem;
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
    private BuildingRoadLegacyContextSystem _buildingRoadLegacyContextSystem => _source.BuildingRoadLegacyContextSystem;
    private BuildingRoadLegacyEcsSystem _buildingRoadLegacyEcsSystem => _source.BuildingRoadLegacyEcsSystem;
    private RoadRuntimeGenerationSystem _roadRuntimeGenerationSystem => _source.RoadRuntimeGenerationSystem;
    private RoadRuntimeGenerationContextSystem _roadRuntimeGenerationContextSystem => _source.RoadRuntimeGenerationContextSystem;

    private RoadBuildStartupSystem.State _startupState = new();
    private RoadBuildDependencySystem.State _dependencyState;
    private uint _buildingSpawnRandomState = 0x12345678u;
    private GameObject _placementOutline;
    private Transform[] _placementOutlineEdges;
    private MeshRenderer[] _placementOutlineRenderers;
    private bool _isDraggingBuildingPlacement;

    private static readonly Vector2Int North = new(0, 1), East = new(1, 0), South = new(0, -1), West = new(-1, 0);

    public RoadBuildRuntimeStateSystem(RoadBuildCompositionSourceSystem source)
    {
        _source = source;
        _dependencyState = _roadBuildDependencySystem.CreateState();
    }

    private Dictionary<EdgeKey, int> _edgeCounts => _roadNetworkSystem.EdgeCounts;
    private Dictionary<Vector2Int, List<int>> _strokeIdsByCell => _roadNetworkSystem.StrokeIdsByCell;
    private Dictionary<int, StrokeData> _strokes => _roadNetworkSystem.Strokes;
    private Dictionary<Vector2Int, RoadTileData> _roadTiles => _roadNetworkSystem.RoadTiles;
    private HashSet<Vector2Int> _autobahnCells => _roadNetworkSystem.AutobahnCells;
    private HashSet<Vector2Int> _autobahnConnectorCells => _roadNetworkSystem.AutobahnConnectorCells;
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
    internal RoadBuildInputSystem RoadBuildInputSystem => _roadBuildInputSystem;
    internal RoadBuildInputSystem.Context RoadBuildInputContext => CreateRoadBuildInputContext();
    internal Camera RoadBuildInputCamera => worldCamera;
    internal RoadDeletePromptSystem RoadDeletePromptSystem => _roadDeletePromptSystem;
    internal RoadDeletePromptSystem.Context RoadDeletePromptContext => CreateRoadDeletePromptContext();

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
            CaptureRoadBuildSessionSnapshot,
            RestoreRoadBuildSession,
            RemoveRuntimeBlockersUnderRoads,
            _roadMinimapEventSystem.PublishStaticMinimapChanged,
            ApplyBuildCommandMode,
            ClearCommandMode,
            ClearSelectedBuilding,
            CancelBuildingPlacement,
            CancelPendingBuild,
            HidePlacementOutline,
            UpdatePreview,
            TryGetHoveredCell,
            ClearPreview,
            UpdateBuildingPlacement,
            path => CreateStroke(path),
            () => _buildingRoadLegacyStorageSystem.HasPendingBuildingPlacement,
            value => _isDraggingBuildingPlacement = value,
            ClearRoadBuildDragState,
            DeleteStroke);
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

    private BuildingRoadLegacyContextSystem.Context CreateBuildingRoadLegacyContext()
    {
        return new BuildingRoadLegacyContextSystem.Context(
            TryGetEntityManager,
            TryGetGridData,
            GetFootprintCenter,
            BuildingPlacementInteraction,
            BuildingPlacementInteractionContext,
            _buildingSpawnRandomState);
    }

    private BuildingRoadLegacyEcsSystem.Context CreateBuildingRoadLegacyEcsContext()
    {
        return _buildingRoadLegacyContextSystem.CreateEcsContext(CreateBuildingRoadLegacyContext());
    }

    private RoadRuntimeGenerationContextSystem.Context CreateRoadRuntimeGenerationContextSource()
    {
        return new RoadRuntimeGenerationContextSystem.Context(
            TryGetRoadCellSizeInGridCellsInternal,
            BeginDeferredRoadEcsSyncInternal,
            EndDeferredRoadEcsSyncInternal,
            CreateStroke,
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
            () => _isDraggingBuildingPlacement);
    }

    public void BeginDeferredRoadEcsSync()
    {
        _roadRuntimeGenerationSystem.BeginDeferredRoadEcsSync(CreateRoadRuntimeGenerationContext());
    }

    private void BeginDeferredRoadEcsSyncInternal()
    {
        _roadGridProjectionSystem.BeginDeferredRoadEcsSync();
    }

    public void EndDeferredRoadEcsSync()
    {
        _roadRuntimeGenerationSystem.EndDeferredRoadEcsSync(CreateRoadRuntimeGenerationContext());
    }

    private void EndDeferredRoadEcsSyncInternal()
    {
        _roadGridProjectionSystem.EndDeferredRoadEcsSync(CreateRoadGridProjectionContext());
    }

    public bool TryGetRoadCellSizeInGridCells(out int roadCellSizeInGridCells)
    {
        return _roadRuntimeGenerationSystem.TryGetRoadCellSizeInGridCells(
            CreateRoadRuntimeGenerationContext(),
            out roadCellSizeInGridCells);
    }

    private bool TryGetRoadCellSizeInGridCellsInternal(out int roadCellSizeInGridCells)
    {
        roadCellSizeInGridCells = 0;
        if (roadGridSize <= 0f)
            return false;
        if (!TryGetGridConfig(out GridConfig grid))
            return false;
        if (grid.CellSize <= 0f)
            return false;

        roadCellSizeInGridCells = Mathf.Max(1, Mathf.RoundToInt(roadGridSize / grid.CellSize));
        return true;
    }

    public bool CreateRoadStrokeFromRoadCells(IReadOnlyList<Vector2Int> cells)
    {
        return _roadRuntimeGenerationSystem.CreateRoadStrokeFromRoadCells(
            CreateRoadRuntimeGenerationContext(),
            cells);
    }

    public bool CreateAutobahnStrokeFromRoadCells(IReadOnlyList<Vector2Int> cells)
    {
        return _roadRuntimeGenerationSystem.CreateAutobahnStrokeFromRoadCells(
            CreateRoadRuntimeGenerationContext(),
            cells);
    }

    public bool CreateAutobahnStrokeFromRoadCells(
        IReadOnlyList<Vector2Int> cells,
        bool useAutobahnConnectorAtStart,
        bool useAutobahnConnectorAtEnd)
    {
        return _roadRuntimeGenerationSystem.CreateAutobahnStrokeFromRoadCells(
            CreateRoadRuntimeGenerationContext(),
            cells,
            useAutobahnConnectorAtStart,
            useAutobahnConnectorAtEnd);
    }

    public bool TryGetAutobahnConnectorRoadCell(Vector2Int connectorCell, out Vector2Int roadConnectionCell)
    {
        return _roadRuntimeGenerationSystem.TryGetAutobahnConnectorRoadCell(
            CreateRoadRuntimeGenerationContext(),
            connectorCell,
            out roadConnectionCell);
    }

    public bool TryLogRoadConnectMarkers(Vector2Int roadCell)
    {
        return _roadRuntimeGenerationSystem.TryLogRoadConnectMarkers(
            CreateRoadRuntimeGenerationContext(),
            roadCell);
    }

    public bool CreateStandaloneStraightRoadChainFromConnector(Vector2Int connectorCell, Vector2Int direction, int length)
    {
        return _roadRuntimeGenerationSystem.CreateStandaloneStraightRoadChainFromConnector(
            CreateRoadRuntimeGenerationContext(),
            connectorCell,
            direction,
            length);
    }

    public bool TryGetStandaloneStraightChainEndRoadCell(Vector2Int direction, out Vector2Int roadConnectionCell)
    {
        return _roadRuntimeGenerationSystem.TryGetStandaloneStraightChainEndRoadCell(
            CreateRoadRuntimeGenerationContext(),
            direction,
            out roadConnectionCell);
    }

    public bool CreateStandaloneDebugCityRoadNetworkFromStraightChain(Vector2Int direction, int branchLength)
    {
        return _roadRuntimeGenerationSystem.CreateStandaloneDebugCityRoadNetworkFromStraightChain(
            CreateRoadRuntimeGenerationContext(),
            direction,
            branchLength);
    }

    public bool HasRoadInFootprint(GridConfig grid, Vector2Int originCell, Vector2Int footprintCells)
    {
        return _roadFootprintQuerySystem.HasRoadInFootprint(
            CreateRoadFootprintQueryContext(),
            grid,
            originCell,
            footprintCells);
    }

    public void FillRoadFootprintMask(GridConfig grid, bool[] occupiedCells)
    {
        _roadFootprintQuerySystem.FillRoadFootprintMask(
            CreateRoadFootprintQueryContext(),
            grid,
            occupiedCells);
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

        BuildDefinitions();
        CreatePlacementOutline();
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

        _roadBuildStartupSystem.DisposeRuntimeRoots(_startupState, _roadRuntimeRootSystem);

        if (_placementOutline != null)
        {
            Destroy(_placementOutline);
            _placementOutline = null;
            _placementOutlineEdges = null;
            _placementOutlineRenderers = null;
        }

        _roadVisualVariantSystem.DisposeCachedVisualData();

        _roadPreviewSystem.DisposePreview();
        _roadChunkVisualSystem.DisposeChunks();

        foreach (var building in _buildingRoadLegacyStorageSystem.RuntimeBuildings.Values)
        {
            if (building.Instance != null)
                Destroy(building.Instance);

            if (building.CombatEntity != Entity.Null &&
                World.DefaultGameObjectInjectionWorld != null &&
                World.DefaultGameObjectInjectionWorld.IsCreated)
            {
                var em = World.DefaultGameObjectInjectionWorld.EntityManager;
                if (em.Exists(building.CombatEntity))
                    em.DestroyEntity(building.CombatEntity);
            }

            if (building.BlockerEntity != Entity.Null &&
                World.DefaultGameObjectInjectionWorld != null &&
                World.DefaultGameObjectInjectionWorld.IsCreated)
            {
                var em = World.DefaultGameObjectInjectionWorld.EntityManager;
                if (em.Exists(building.BlockerEntity))
                    em.DestroyEntity(building.BlockerEntity);
            }
        }

        _roadSpecialVisualSystem.DisposeVisuals();
        _roadMinimapEventSystem.Clear();

        ClearRoadDataInEcs();

        _roadTiles.Clear();
        _buildingRoadLegacyStorageSystem.Clear();
    }

    public void Update()
    {
        _roadBuildInputSystem.Update(CreateRoadBuildInputContext(), worldCamera);
    }

    public void OnGui()
    {
        _roadDeletePromptSystem.OnGui(CreateRoadDeletePromptContext());
    }

    public void ActivateRoadBuildMode()
    {
        _roadBuildCommandSystem.ActivateRoadBuildMode(CreateRoadBuildCommandContext());
    }

    public void ConfirmRoadBuildSession()
    {
        _roadBuildCommandSystem.ConfirmRoadBuildSession(CreateRoadBuildCommandContext());
    }

    public void CancelRoadBuildSession()
    {
        _roadBuildCommandSystem.CancelRoadBuildSession(CreateRoadBuildCommandContext());
    }

    public void BeginSoldierBasePlacement()
    {
        BuildingPlacementInteraction?.BeginSoldierBasePlacement(BuildingPlacementInteractionContext);
    }

    public void ConfirmBuildingPlacement()
    {
        BuildingPlacementInteraction?.ConfirmBuildingPlacement(BuildingPlacementInteractionContext);
    }

    public void CancelBuildingPlacement()
    {
        BuildingPlacementInteraction?.CancelBuildingPlacement(BuildingPlacementInteractionContext);
    }

    public void CreateSoldierFromSelectedBuilding()
    {
        BuildingPlacementInteraction?.CreateUnitFromSelectedBuilding(BuildingPlacementInteractionContext);
    }

    public void DeleteSelectedBuilding()
    {
        BuildingPlacementInteraction?.DeleteSelectedBuilding(BuildingPlacementInteractionContext);
    }

    public void ClearSelectedBuilding()
    {
        BuildingPlacementInteraction?.ClearSelectedBuilding(BuildingPlacementInteractionContext, "RoadBuild.ClearSelectedBuilding");
    }

    public void ExitBuildMode()
    {
        _roadBuildCommandSystem.ExitBuildMode(CreateRoadBuildCommandContext());
    }

    private void ClearRoadBuildDragState()
    {
        _isDraggingBuildingPlacement = false;
    }

    private void CreateStroke(
        List<Vector2Int> cells,
        bool isAutobahn = false,
        bool useAutobahnConnectorAtStart = false,
        bool useAutobahnConnectorAtEnd = false)
    {
        if (_roadNetworkSystem.CreateStroke(
                cells,
                isAutobahn,
                useAutobahnConnectorAtStart,
                useAutobahnConnectorAtEnd,
                out var dirtyCells))
        {
            RefreshCells(dirtyCells);
        }
    }

    private void DeleteStroke(int strokeId)
    {
        if (_roadNetworkSystem.DeleteStroke(strokeId, out var dirtyCells))
            RefreshCells(dirtyCells);
    }

    private void RefreshCells(HashSet<Vector2Int> dirtyCells)
    {
        foreach (var cell in dirtyCells)
            RefreshCell(cell);

        _roadGridProjectionSystem.RequestRoadEcsSync(CreateRoadGridProjectionContext());

        _roadChunkVisualSystem.RebuildDirtyChunks(CreateRoadChunkVisualContext());
        RebuildSpecialRoadObjects(dirtyCells);
    }

    private void RefreshCell(Vector2Int cell)
    {
        TileConnectionMask mask = GetMask(cell);
        RoadVisualType targetType = ResolveVisualType(cell, mask);
        if (targetType == RoadVisualType.None)
        {
            _roadTiles.Remove(cell);
            _roadChunkVisualSystem.RemoveCellFromChunk(CreateRoadChunkVisualContext(), cell);

            return;
        }

        if (!TryGetVariant(targetType, mask, out var variant))
            return;

        if (_roadTiles.TryGetValue(cell, out var current) &&
            current.Type == targetType &&
            current.Mask.Equals(mask) &&
            current.Rotation == variant.Rotation &&
            current.Scale == variant.Scale)
        {
            return;
        }

        _roadTiles[cell] = new RoadTileData
        {
            Type = targetType,
            Mask = mask,
            Rotation = variant.Rotation,
            Scale = variant.Scale
        };

        _roadChunkVisualSystem.AddCellToChunk(CreateRoadChunkVisualContext(), cell);
    }

    private TileConnectionMask GetMask(Vector2Int cell) => _roadNetworkSystem.GetMask(cell);

    private bool HasEdge(Vector2Int a, Vector2Int b) => _roadNetworkSystem.HasEdge(a, b);

    private RoadVisualType ResolveVisualType(Vector2Int cell, TileConnectionMask mask)
    {
        if (_autobahnConnectorCells.Contains(cell))
            return RoadVisualType.AutobahnConnect;

        if (_autobahnCells.Contains(cell))
            return RoadVisualType.Autobahn;

        bool isStraight = (mask.North && mask.South) || (mask.East && mask.West);
        if (isStraight)
        {
            // Straight roads keep using the standard road visuals unless explicitly marked as autobahn.
        }

        switch (mask.Count)
        {
            case 0:
                return RoadVisualType.None;

            case 1:
                return RoadVisualType.End;

            case 2:
                if (mask.North && mask.South)
                    return RoadVisualType.Straight;

                if (mask.East && mask.West)
                    return RoadVisualType.Straight;

                if (mask.North && mask.East)
                    return RoadVisualType.Corner;
                return RoadVisualType.Corner;

            case 3:
                return RoadVisualType.TIntersection;

            default:
                return RoadVisualType.Intersection;
        }
    }

    private GameObject GetPrefab(RoadVisualType type)
    {
        return _roadBuildVisualContextSystem.GetPrefab(CreateRoadBuildVisualContext(), type);
    }

    private bool TryGetVariant(RoadVisualType type, TileConnectionMask mask, out VariantData variant)
    {
        return _roadVisualVariantSystem.TryGetVariant(type, mask, out variant);
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

    private RoadNetworkSystem.Snapshot CaptureRoadBuildSessionSnapshot()
    {
        return _roadNetworkSystem.CaptureSnapshot();
    }

    private void RestoreRoadBuildSession(RoadNetworkSystem.Snapshot snapshot)
    {
        _roadNetworkSystem.RestoreSnapshot(snapshot);
        RebuildRoadStateFromCurrentTiles();
    }

    private void RebuildRoadStateFromCurrentTiles()
    {
        RebuildSpecialRoadCellMetadata();

        _roadChunkVisualSystem.ClearChunks();
        _roadSpecialVisualSystem.ClearSpecialRoadObjects();

        foreach (var cell in _roadTiles.Keys)
            _roadChunkVisualSystem.AddCellToChunk(CreateRoadChunkVisualContext(), cell);

        SyncRoadCellsToEcs();
        _roadChunkVisualSystem.RebuildDirtyChunks(CreateRoadChunkVisualContext());
        _roadSpecialVisualSystem.RebuildSpecialRoadObjects(CreateRoadSpecialVisualContext());
    }

    private void RebuildSpecialRoadCellMetadata()
    {
        _roadNetworkSystem.RebuildSpecialRoadCellMetadata();
    }

    private static void ApplyBuildCommandMode()
    {
        BattleHudGameplayBridge.ResolveActive()?.ApplyCommandMode(TacticalCommandMode.Build);
    }

    private static void ClearCommandMode()
    {
        BattleHudGameplayBridge.ResolveActive()?.ClearCommandMode();
    }

    private void BuildDefinitions()
    {
        var soldierBaseDefinition = new BuildingDefinition
        {
            DisplayName = "Soldier Base",
            Prefab = soldierBasePrefab,
            FootprintCells = new Vector2Int(
                Mathf.Max(1, soldierBaseFootprintCells.x),
                Mathf.Max(1, soldierBaseFootprintCells.y))
        };

        CacheBuildingBounds(soldierBaseDefinition);
        _buildingRoadLegacyStorageSystem.SetSoldierBaseDefinition(soldierBaseDefinition);
    }

    private void CacheBuildingBounds(BuildingDefinition definition)
    {
        if (definition == null || definition.Prefab == null || definition.HasLocalBounds)
            return;

        GameObject temp = Instantiate(definition.Prefab);
        temp.hideFlags = HideFlags.HideAndDontSave;
        if (TryGetLocalBounds(temp, out Bounds localBounds))
        {
            definition.LocalBounds = localBounds;
            definition.HasLocalBounds = true;
        }

        Destroy(temp);
    }

    private void CreatePlacementOutline()
    {
        _placementOutline = new GameObject("PlacementOutline");
        _placementOutline.transform.SetParent(RuntimeRoot, false);
        _placementOutlineEdges = new Transform[4];
        _placementOutlineRenderers = new MeshRenderer[4];

        for (int i = 0; i < 4; i++)
        {
            GameObject edge = GameObject.CreatePrimitive(PrimitiveType.Cube);
            edge.name = $"PlacementOutlineEdge_{i}";
            edge.transform.SetParent(_placementOutline.transform, false);
            var collider = edge.GetComponent<Collider>();
            if (collider != null)
                Destroy(collider);

            var renderer = edge.GetComponent<MeshRenderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.material = CreatePlacementMaterial();
            _placementOutlineEdges[i] = edge.transform;
            _placementOutlineRenderers[i] = renderer;
        }

        ApplyPlacementMaterialColor(placementValidColor);
        _placementOutline.SetActive(false);
    }

    private void BeginBuildingPlacement(BuildingDefinition definition)
    {
        CancelBuildingPlacementInternal();
        _isDraggingBuildingPlacement = false;

        _buildingRoadLegacyStorageSystem.BeginPlacement(
            definition,
            Instantiate(definition.Prefab, BuildingRoot),
            GetCenterScreenPlacementOrigin(definition.FootprintCells));

        UpdateBuildingPlacementVisual(_buildingRoadLegacyStorageSystem.ActivePlacement, updateCellFromPointer: false);
    }

    private void CancelBuildingPlacementInternal()
    {
        GameObject previewInstance = _buildingRoadLegacyStorageSystem.ClearActivePlacement();
        if (previewInstance != null)
            Destroy(previewInstance);

        _isDraggingBuildingPlacement = false;
        HidePlacementOutline();
    }

    private void UpdateBuildingPlacement(Vector2 screenPosition)
    {
        PlacementState activePlacement = _buildingRoadLegacyStorageSystem.ActivePlacement;
        if (activePlacement == null)
            return;

        UpdateBuildingPlacementVisual(activePlacement, updateCellFromPointer: _isDraggingBuildingPlacement, screenPosition);
    }

    private void UpdateBuildingPlacementVisual(PlacementState placement, bool updateCellFromPointer, Vector2 screenPosition = default)
    {
        if (placement == null || placement.PreviewInstance == null)
            return;

        if (TryGetGridData(out _, out GridConfig grid, out DynamicBuffer<GridRoad> roads, out DynamicBlockerData blockerData) && updateCellFromPointer)
        {
            if (TryGetGridCell(screenPosition, grid, out Vector2Int hoveredCell))
                placement.OriginCell = CenterCellToOrigin(hoveredCell, placement.Definition.FootprintCells);
        }

        if (!TryGetGridData(out _, out grid, out roads, out blockerData))
        {
            placement.IsValid = false;
            HidePlacementOutline();
            return;
        }

        placement.IsValid = IsBuildingPlacementValid(placement.OriginCell, placement.Definition.FootprintCells, grid, roads, blockerData);
        PositionBuildingObject(placement.PreviewInstance, placement.OriginCell, placement.Definition, grid);
        UpdatePlacementOutline(placement.OriginCell, placement.Definition.FootprintCells, grid, placement.IsValid);
    }

    private RuntimeBuildingData PlaceBuilding(PlacementState placement)
    {
        GameObject previewInstance = placement.PreviewInstance;
        int buildingId = _buildingRoadLegacyStorageSystem.AllocateBuildingId();
        previewInstance.name = $"{placement.Definition.DisplayName}_{buildingId}";

        RuntimeGridBlockers?.RemoveBlockersOverlappingFootprint(placement.OriginCell, placement.Definition.FootprintCells);
        BuildingRoadLegacyEcsSystem.Context legacyEcsContext = CreateBuildingRoadLegacyEcsContext();
        Entity blockerEntity = _buildingRoadLegacyEcsSystem.CreateBlockerEntity(legacyEcsContext, placement.OriginCell, placement.Definition.FootprintCells);
        Entity combatEntity = _buildingRoadLegacyEcsSystem.CreateBuildingCombatEntity(legacyEcsContext, placement.OriginCell, placement.Definition);

        var building = new RuntimeBuildingData
        {
            Id = buildingId,
            Definition = placement.Definition,
            Instance = previewInstance,
            OriginCell = placement.OriginCell,
            CombatEntity = combatEntity,
            BlockerEntity = blockerEntity
        };

        _buildingRoadLegacyEcsSystem.AttachRuntimeLink(legacyEcsContext, building);
        _buildingRoadLegacyStorageSystem.AddBuilding(building);
        _buildingRoadLegacyStorageSystem.ReleaseActivePlacementPreview();
        return building;
    }

    private void PositionBuildingObject(GameObject instance, Vector2Int originCell, BuildingDefinition definition, GridConfig grid)
    {
        if (instance == null)
            return;

        Vector3 center = GetFootprintCenter(originCell, definition.FootprintCells, grid);
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

    private Vector3 GetFootprintCenter(Vector2Int originCell, Vector2Int footprintCells, GridConfig grid)
    {
        return new Vector3(
            grid.Origin.x + (originCell.x + footprintCells.x * 0.5f) * grid.CellSize,
            buildPlaneY,
            grid.Origin.z + (originCell.y + footprintCells.y * 0.5f) * grid.CellSize);
    }

    private Vector2Int GetCenterScreenPlacementOrigin(Vector2Int footprintCells)
    {
        if (!TryGetGridData(out _, out GridConfig grid, out _, out _))
            return Vector2Int.zero;

        Vector2 centerScreen = new(Screen.width * 0.5f, Screen.height * 0.5f);
        if (TryGetGridCell(centerScreen, grid, out Vector2Int centerCell))
            return CenterCellToOrigin(centerCell, footprintCells);

        return Vector2Int.zero;
    }

    private static Vector2Int CenterCellToOrigin(Vector2Int centerCell, Vector2Int footprintCells)
    {
        return new Vector2Int(
            centerCell.x - Mathf.FloorToInt(footprintCells.x * 0.5f),
            centerCell.y - Mathf.FloorToInt(footprintCells.y * 0.5f));
    }

    private bool IsBuildingPlacementValid(Vector2Int originCell, Vector2Int footprintCells, GridConfig grid, DynamicBuffer<GridRoad> roads, DynamicBlockerData blockerData)
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
                    (RuntimeGridBlockers == null || !RuntimeGridBlockers.IsRuntimeBlockerCell(x, y, grid.Width, grid.Height)))
                    return false;
            }
        }

        return true;
    }

    private void UpdatePlacementOutline(Vector2Int originCell, Vector2Int footprintCells, GridConfig grid, bool valid)
    {
        if (_placementOutline == null || _placementOutlineEdges == null || _placementOutlineRenderers == null)
            return;

        float width = footprintCells.x * grid.CellSize;
        float depth = footprintCells.y * grid.CellSize;
        float thickness = Mathf.Max(0.2f, placementOutlineWidth);
        float height = Mathf.Max(0.08f, placementOutlineHeight);
        Vector3 center = GetFootprintCenter(originCell, footprintCells, grid);
        center.y = buildPlaneY + height * 0.5f;

        _placementOutline.transform.SetPositionAndRotation(center, Quaternion.identity);

        _placementOutlineEdges[0].localPosition = new Vector3(0f, 0f, depth * 0.5f);
        _placementOutlineEdges[0].localScale = new Vector3(width + thickness, height, thickness);

        _placementOutlineEdges[1].localPosition = new Vector3(0f, 0f, -depth * 0.5f);
        _placementOutlineEdges[1].localScale = new Vector3(width + thickness, height, thickness);

        _placementOutlineEdges[2].localPosition = new Vector3(width * 0.5f, 0f, 0f);
        _placementOutlineEdges[2].localScale = new Vector3(thickness, height, depth + thickness);

        _placementOutlineEdges[3].localPosition = new Vector3(-width * 0.5f, 0f, 0f);
        _placementOutlineEdges[3].localScale = new Vector3(thickness, height, depth + thickness);

        ApplyPlacementMaterialColor(valid ? placementValidColor : placementInvalidColor);
        _placementOutline.SetActive(true);
    }

    private void HidePlacementOutline()
    {
        if (_placementOutline != null)
            _placementOutline.SetActive(false);
    }

    private void HandleBuildingSelectionClick(Vector2 screenPosition)
    {
        if (_roadBuildInputSystem.IsPointerOverUI(screenPosition))
            return;

        if (!TryGetGridData(out _, out GridConfig grid, out _, out _))
            return;

        if (!TryGetGridCell(screenPosition, grid, out Vector2Int cell))
        {
            ClearSelectedBuilding();
            return;
        }

        foreach (var entry in _buildingRoadLegacyStorageSystem.RuntimeBuildings)
        {
            Vector2Int min = entry.Value.OriginCell;
            Vector2Int size = entry.Value.Definition.FootprintCells;
            if (cell.x < min.x || cell.y < min.y || cell.x >= min.x + size.x || cell.y >= min.y + size.y)
                continue;

            SelectBuilding(entry.Key);
            return;
        }

        ClearSelectedBuilding();
    }

    private void SelectBuilding(int buildingId)
    {
        _buildingRoadLegacyStorageSystem.SelectBuilding(buildingId);
    }

    private void DeleteBuilding(int buildingId, bool destroyVisual)
    {
        if (!_buildingRoadLegacyStorageSystem.TryGetBuilding(buildingId, out RuntimeBuildingData building))
            return;

        if (building.CombatEntity != Entity.Null && TryGetEntityManager(out EntityManager em) && em.Exists(building.CombatEntity))
            em.DestroyEntity(building.CombatEntity);

        if (destroyVisual && building.Instance != null)
            Destroy(building.Instance);

        if (building.BlockerEntity != Entity.Null &&
            World.DefaultGameObjectInjectionWorld != null &&
            World.DefaultGameObjectInjectionWorld.IsCreated)
        {
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            if (entityManager.Exists(building.BlockerEntity))
                entityManager.DestroyEntity(building.BlockerEntity);
        }

        _buildingRoadLegacyStorageSystem.RemoveBuilding(buildingId);
    }

    private bool TryGetGridData(out Entity gridEntity, out GridConfig grid, out DynamicBuffer<GridRoad> roads, out DynamicBlockerData blockerData)
    {
        return _roadGridProjectionSystem.TryGetGridData(out gridEntity, out grid, out roads, out blockerData);
    }

    private static bool TryGetGridConfig(out GridConfig grid)
    {
        grid = default;
        if (!TryGetEntityManager(out EntityManager entityManager))
            return false;

        using EntityQuery query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<GridConfig>());
        if (query.IsEmptyIgnoreFilter)
            return false;

        grid = entityManager.GetComponentData<GridConfig>(query.GetSingletonEntity());
        return true;
    }

    private static bool TryGetEntityManager(out EntityManager entityManager)
    {
        entityManager = default;
        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return false;

        entityManager = world.EntityManager;
        return true;
    }

    private bool TryGetGridCell(Vector2 screenPosition, GridConfig grid, out Vector2Int cell)
    {
        cell = default;
        Ray ray = worldCamera.ScreenPointToRay(screenPosition);
        Plane plane = new(Vector3.up, new Vector3(0f, buildPlaneY, 0f));
        if (!plane.Raycast(ray, out float distance))
            return false;

        Vector3 worldPoint = ray.GetPoint(distance);
        int2 gridCell = GridUtils.WorldToCell(grid, worldPoint);
        if (!GridUtils.InBounds(gridCell, grid.Width, grid.Height))
            return false;

        cell = new Vector2Int(gridCell.x, gridCell.y);
        return true;
    }

    private static bool TryGetLocalBounds(GameObject target, out Bounds bounds)
    {
        bounds = default;
        var renderers = target.GetComponentsInChildren<Renderer>(true);
        bool hasBounds = false;
        Matrix4x4 worldToLocal = target.transform.worldToLocalMatrix;
        foreach (Renderer renderer in renderers)
        {
            Bounds rendererBounds = renderer.bounds;
            Vector3 min = rendererBounds.min;
            Vector3 max = rendererBounds.max;
            for (int x = 0; x < 2; x++)
            {
                for (int y = 0; y < 2; y++)
                {
                    for (int z = 0; z < 2; z++)
                    {
                        Vector3 corner = new(
                            x == 0 ? min.x : max.x,
                            y == 0 ? min.y : max.y,
                            z == 0 ? min.z : max.z);
                        Vector3 localCorner = worldToLocal.MultiplyPoint3x4(corner);
                        if (!hasBounds)
                        {
                            bounds = new Bounds(localCorner, Vector3.zero);
                            hasBounds = true;
                        }
                        else
                        {
                            bounds.Encapsulate(localCorner);
                        }
                    }
                }
            }
        }

        return hasBounds;
    }

    private Material CreatePlacementMaterial()
    {
        Shader shader =
            Shader.Find("Universal Render Pipeline/Unlit") ??
            Shader.Find("Universal Render Pipeline/Simple Lit") ??
            Shader.Find("Universal Render Pipeline/Lit") ??
            Shader.Find("Sprites/Default") ??
            Shader.Find("Unlit/Color") ??
            Shader.Find("Standard");

        var material = new Material(shader);
        material.renderQueue = (int)RenderQueue.Transparent;
        material.SetOverrideTag("RenderType", "Transparent");

        if (material.HasProperty("_Surface"))
            material.SetFloat("_Surface", 1f);
        if (material.HasProperty("_Blend"))
            material.SetFloat("_Blend", 0f);
        if (material.HasProperty("_SrcBlend"))
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        if (material.HasProperty("_DstBlend"))
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        if (material.HasProperty("_ZWrite"))
            material.SetFloat("_ZWrite", 0f);

        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        return material;
    }

    private void ApplyPlacementMaterialColor(Color color)
    {
        if (_placementOutlineRenderers == null)
            return;

        Color transparentColor = color;
        transparentColor.a = 0.22f;
        for (int i = 0; i < _placementOutlineRenderers.Length; i++)
        {
            var renderer = _placementOutlineRenderers[i];
            if (renderer == null)
                continue;

            Material material = renderer.material;
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", transparentColor);
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", transparentColor);
        }
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

    private void RebuildSpecialRoadObjects(HashSet<Vector2Int> dirtyCells)
    {
        _roadSpecialVisualSystem.RebuildSpecialRoadObjects(CreateRoadSpecialVisualContext());
    }

    private void SyncRoadCellsToEcs()
    {
        _roadGridProjectionSystem.SyncRoadCellsToEcs(CreateRoadGridProjectionContext());
    }

    private void RemoveRuntimeBlockersUnderRoads()
    {
        _roadGridProjectionSystem.RemoveRuntimeBlockersUnderRoads(
            CreateRoadGridProjectionContext(),
            RuntimeGridBlockers);
    }

    private void ClearRoadDataInEcs()
    {
        _roadGridProjectionSystem.ClearRoadDataInEcs();
    }
}
