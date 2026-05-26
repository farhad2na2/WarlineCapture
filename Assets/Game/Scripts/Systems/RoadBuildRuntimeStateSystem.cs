using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using static UnityEngine.Object;
using CombinedRoadVisualData = RoadFootprintQuerySystem.CombinedRoadVisualData;
using BuildToolMode = RoadBuildSessionSystem.BuildToolMode;
using EdgeKey = RoadNetworkSystem.EdgeKey;
using ConnectorMarkerData = RoadVisualVariantSystem.ConnectorMarkerData;
using MarkerLayoutData = RoadVisualVariantSystem.MarkerLayoutData;
using RoadTileData = RoadNetworkSystem.RoadTileData;
using VariantData = RoadVisualVariantSystem.VariantData;
using RoadVisualType = RoadNetworkSystem.RoadVisualType;
using StrokeData = RoadNetworkSystem.StrokeData;
using TileConnectionMask = RoadNetworkSystem.TileConnectionMask;
using PlacementState = BuildingPlacementLifecycleSystem.PlacementState;

internal sealed class RoadBuildRuntimeStateSystem
{
    private readonly RuntimeGameplayStateSystem _runtimeGameplayStateSystem = new();
    private readonly RoadBuildConfigSystem _roadBuildConfigSystem = new();
    private readonly RoadRuntimeRootSystem _roadRuntimeRootSystem = new();
    private readonly RoadNetworkSystem _roadNetworkSystem = new();
    private readonly RoadPathPlanningSystem _roadPathPlanningSystem = new();
    private readonly RoadFootprintQuerySystem _roadFootprintQuerySystem = new();
    private readonly RoadGridProjectionSystem _roadGridProjectionSystem = new();
    private readonly RoadVisualVariantSystem _roadVisualVariantSystem = new();
    private readonly RoadChunkVisualSystem _roadChunkVisualSystem = new();
    private readonly RoadPreviewSystem _roadPreviewSystem = new();
    private readonly RoadSpecialVisualSystem _roadSpecialVisualSystem = new();
    private readonly RoadBuildSessionSystem _roadBuildSessionSystem = new();
    private readonly RoadBuildSessionSystem.State _roadBuildSessionState = new();
    private readonly RoadMinimapEventSystem _roadMinimapEventSystem = new();
    private readonly RoadBuildInputSystem _roadBuildInputSystem = new();
    private readonly RoadBuildInputSystem.State _roadBuildInputState = new();
    private readonly RoadBuildCommandSystem _roadBuildCommandSystem = new();
    private readonly RoadDeletePromptSystem _roadDeletePromptSystem = new();
    private readonly BuildingRoadLegacyStorageSystem _buildingRoadLegacyStorageSystem = new();
    private readonly BuildingRoadLegacyEcsSystem _buildingRoadLegacyEcsSystem = new();
    private readonly RoadRuntimeGenerationSystem _roadRuntimeGenerationSystem = new();

    [SerializeField] private RoadBuildSystemConfig config;
    [Header("References")]
    [SerializeField, HideInInspector] private Camera worldCamera;
    [SerializeField, HideInInspector] private GameObject straightPrefab;
    [SerializeField, HideInInspector] private GameObject tIntersectionPrefab;
    [SerializeField, HideInInspector] private GameObject intersectionPrefab;
    [SerializeField, HideInInspector] private GameObject endPrefab;
    [SerializeField, HideInInspector] private GameObject cornerPrefab;
    [SerializeField, HideInInspector] private GameObject autobahnPrefab;
    [SerializeField, HideInInspector] private GameObject autobahnConnectPrefab;

    [Header("Placement")]
    [SerializeField, HideInInspector] private Vector3 gridOrigin = Vector3.zero;
    [SerializeField, HideInInspector] private float buildPlaneY = 0f;
    [SerializeField, HideInInspector] private float roadGridSize = 20f;
    [SerializeField, HideInInspector] private int chunkSizeInCells = 8;
    [SerializeField, HideInInspector] private float previewAlpha = 0.65f;

    [Header("Buildings")]
    [SerializeField, HideInInspector] private GameObject soldierBasePrefab;
    [SerializeField, HideInInspector] private Vector2Int soldierBaseFootprintCells = new(20, 20);
    [SerializeField, HideInInspector] private float placementOutlineHeight = 0.15f;
    [SerializeField, HideInInspector] private float placementOutlineWidth = 0.35f;
    [SerializeField, HideInInspector] private Color placementValidColor = new(0.15f, 0.85f, 0.2f, 1f);
    [SerializeField, HideInInspector] private Color placementInvalidColor = new(0.9f, 0.2f, 0.2f, 1f);

    private uint _buildingSpawnRandomState = 0x12345678u;
    private RoadRuntimeRootSystem.Roots _runtimeRoots;
    private GameObject _placementOutline;
    private Transform[] _placementOutlineEdges;
    private MeshRenderer[] _placementOutlineRenderers;
    private bool _isDraggingBuildingPlacement;
    private BuildingPlacementInteractionSystem _buildingPlacementInteractionSystem;
    private BuildingPlacementInteractionSystem.Context _buildingPlacementInteractionContext;
    private MainMenuPlayUI _mainMenuPlayUi;
    private RuntimeGridBlockerSystem _runtimeGridBlockers;
    private Transform _runtimeRoot;

    private static readonly Vector2Int North = new(0, 1), East = new(1, 0), South = new(0, -1), West = new(-1, 0);

    private Dictionary<EdgeKey, int> _edgeCounts => _roadNetworkSystem.EdgeCounts;
    private Dictionary<Vector2Int, List<int>> _strokeIdsByCell => _roadNetworkSystem.StrokeIdsByCell;
    private Dictionary<int, StrokeData> _strokes => _roadNetworkSystem.Strokes;
    private Dictionary<Vector2Int, RoadTileData> _roadTiles => _roadNetworkSystem.RoadTiles;
    private HashSet<Vector2Int> _autobahnCells => _roadNetworkSystem.AutobahnCells;
    private HashSet<Vector2Int> _autobahnConnectorCells => _roadNetworkSystem.AutobahnConnectorCells;
    private Dictionary<RoadVisualType, CombinedRoadVisualData> _visualData => _roadVisualVariantSystem.VisualData;
    private Dictionary<RoadVisualType, MarkerLayoutData> _markerLayouts => _roadVisualVariantSystem.MarkerLayouts;
    private ConnectorMarkerData? _autobahnConnectorMarkerData => _roadVisualVariantSystem.AutobahnConnectorMarkerData;
    private Dictionary<Vector2Int, GameObject> _specialRoadObjects => _roadSpecialVisualSystem.SpecialRoadObjects;

    private Transform RoadRoot => _runtimeRoots.RoadRoot;
    private Transform SpecialRoadRoot => _runtimeRoots.SpecialRoadRoot;
    private Transform SpecialRoadConnectorRoot => _runtimeRoots.SpecialRoadConnectorRoot;
    private Transform DebugStraightRoadRoot => _runtimeRoots.DebugStraightRoadRoot;
    private Transform BuildingRoot => _runtimeRoots.BuildingRoot;

    internal RoadRuntimeGenerationSystem RoadRuntimeGenerationSystem => _roadRuntimeGenerationSystem;
    internal RoadRuntimeGenerationSystem.Context RoadRuntimeGenerationContext => CreateRoadRuntimeGenerationContext();
    internal RoadFootprintQuerySystem RoadFootprintQuerySystem => _roadFootprintQuerySystem;
    internal RoadFootprintQuerySystem.Context RoadFootprintQueryContext => CreateRoadFootprintQueryContext();
    internal RoadBuildInputSystem RoadBuildInputSystem => _roadBuildInputSystem;
    internal RoadBuildInputSystem.Context RoadBuildInputContext => CreateRoadBuildInputContext();
    internal Camera RoadBuildInputCamera => worldCamera;
    internal RoadDeletePromptSystem RoadDeletePromptSystem => _roadDeletePromptSystem;
    internal RoadDeletePromptSystem.Context RoadDeletePromptContext => CreateRoadDeletePromptContext();

    private RoadFootprintQuerySystem.Context CreateRoadFootprintQueryContext()
    {
        return new RoadFootprintQuerySystem.Context(
            _roadTiles,
            _specialRoadObjects,
            _visualData,
            gridOrigin,
            buildPlaneY,
            roadGridSize);
    }

    private RoadGridProjectionSystem.Context CreateRoadGridProjectionContext()
    {
        return new RoadGridProjectionSystem.Context(
            _roadTiles,
            _roadFootprintQuerySystem,
            CreateRoadFootprintQueryContext(),
            roadGridSize);
    }

    private RoadVisualVariantSystem.Prefabs CreateRoadPrefabSet()
    {
        return new RoadVisualVariantSystem.Prefabs(
            endPrefab,
            straightPrefab,
            cornerPrefab,
            tIntersectionPrefab,
            intersectionPrefab,
            autobahnPrefab,
            autobahnConnectPrefab);
    }

    private RoadChunkVisualSystem.Context CreateRoadChunkVisualContext()
    {
        return new RoadChunkVisualSystem.Context(
            _roadTiles,
            _visualData,
            _autobahnCells,
            _autobahnConnectorCells,
            RoadRoot,
            gridOrigin,
            buildPlaneY,
            roadGridSize,
            chunkSizeInCells);
    }

    private RoadPreviewSystem.Context CreateRoadPreviewContext()
    {
        return new RoadPreviewSystem.Context(
            _visualData,
            RoadRoot,
            gridOrigin,
            buildPlaneY,
            roadGridSize,
            previewAlpha,
            endPrefab,
            _roadPathPlanningSystem,
            _roadNetworkSystem,
            ResolveVisualType,
            TryGetVariant);
    }

    private RoadSpecialVisualSystem.Context CreateRoadSpecialVisualContext()
    {
        return new RoadSpecialVisualSystem.Context(
            _roadTiles,
            _strokes,
            _markerLayouts,
            _autobahnConnectorMarkerData,
            RoadRoot,
            SpecialRoadRoot,
            SpecialRoadConnectorRoot,
            DebugStraightRoadRoot,
            gridOrigin,
            buildPlaneY,
            roadGridSize,
            chunkSizeInCells,
            GetPrefab,
            TryGetVariant);
    }

    private RoadBuildSessionSystem.Context CreateRoadBuildSessionContext()
    {
        return new RoadBuildSessionSystem.Context(
            _roadBuildSessionState,
            _runtimeGameplayStateSystem,
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
            UpdatePreview);
    }

    private RoadBuildInputSystem.Context CreateRoadBuildInputContext()
    {
        return new RoadBuildInputSystem.Context(
            _roadBuildInputState,
            _runtimeGameplayStateSystem,
            _roadBuildSessionSystem,
            _roadBuildSessionState,
            _roadPathPlanningSystem,
            _roadNetworkSystem,
            TryGetHoveredCell,
            ClearPreview,
            UpdatePreview,
            HidePlacementOutline,
            UpdateBuildingPlacement,
            path => CreateStroke(path),
            () => _buildingRoadLegacyStorageSystem.HasPendingBuildingPlacement,
            value => _isDraggingBuildingPlacement = value);
    }

    private RoadBuildCommandSystem.Context CreateRoadBuildCommandContext()
    {
        return new RoadBuildCommandSystem.Context(
            _runtimeGameplayStateSystem,
            _roadBuildSessionSystem,
            CreateRoadBuildSessionContext(),
            ClearRoadBuildDragState);
    }

    private RoadDeletePromptSystem.Context CreateRoadDeletePromptContext()
    {
        return new RoadDeletePromptSystem.Context(
            _runtimeGameplayStateSystem,
            _roadBuildSessionSystem,
            _roadBuildSessionState,
            DeleteStroke);
    }

    private BuildingRoadLegacyEcsSystem.Context CreateBuildingRoadLegacyEcsContext()
    {
        return new BuildingRoadLegacyEcsSystem.Context(
            TryGetEntityManager,
            TryGetGridData,
            GetFootprintCenter,
            _buildingPlacementInteractionSystem,
            _buildingPlacementInteractionContext,
            _buildingSpawnRandomState);
    }

    private RoadRuntimeGenerationSystem.Context CreateRoadRuntimeGenerationContext()
    {
        return new RoadRuntimeGenerationSystem.Context(
            TryGetRoadCellSizeInGridCellsInternal,
            BeginDeferredRoadEcsSyncInternal,
            EndDeferredRoadEcsSyncInternal,
            CreateStroke,
            _roadSpecialVisualSystem,
            CreateRoadSpecialVisualContext());
    }

    public bool HasPendingBuildingPlacement => _buildingRoadLegacyStorageSystem.HasPendingBuildingPlacement;

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

    public bool CanConfirmBuildingPlacement =>
        _buildingPlacementInteractionSystem != null
            ? _buildingPlacementInteractionSystem.CanConfirmBuildingPlacement(_buildingPlacementInteractionContext)
            : _buildingRoadLegacyStorageSystem.CanConfirmBuildingPlacement;

    public bool HasSelectedBuilding =>
        _buildingPlacementInteractionSystem != null
            ? _buildingPlacementInteractionSystem.HasSelectedBuilding(_buildingPlacementInteractionContext)
            : _buildingRoadLegacyStorageSystem.HasSelectedBuilding;

    public bool IsRoadBuildModeActive => _roadBuildSessionSystem.IsRoadBuildModeActive(CreateRoadBuildSessionContext());
    public bool IsDraggingBuildInteraction => _roadBuildInputSystem.IsDrawing(_roadBuildInputState) || (_buildingRoadLegacyStorageSystem.HasPendingBuildingPlacement && _isDraggingBuildingPlacement);

    private void OnValidate()
    {
        ApplyConfigIfAvailable();
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

    public string PlacementStatusText
    {
        get
        {
            if (_buildingPlacementInteractionSystem != null &&
                _buildingPlacementInteractionSystem.HasPendingBuildingPlacement(_buildingPlacementInteractionContext))
            {
                return _buildingPlacementInteractionSystem.PlacementStatusText(_buildingPlacementInteractionContext);
            }

            PlacementState activePlacement = _buildingRoadLegacyStorageSystem.ActivePlacement;
            if (activePlacement == null)
                return "Choose a build type.";

            string state = activePlacement.IsValid ? "Valid placement" : "Blocked by road or blocker";
            Vector2Int origin = activePlacement.OriginCell;
            Vector2Int size = activePlacement.Definition.FootprintCells;
            return $"{activePlacement.Definition.DisplayName}: {state} ({origin.x},{origin.y}) {size.x}x{size.y}";
        }
    }

    public string SelectedBuildingLabel
    {
        get
        {
            if (_buildingPlacementInteractionSystem != null &&
                _buildingPlacementInteractionSystem.HasActiveBuilding(_buildingPlacementInteractionContext))
            {
                return _buildingPlacementInteractionSystem.SelectedBuildingLabel(_buildingPlacementInteractionContext);
            }

            if (!HasSelectedBuilding)
                return "Building";

            return _buildingRoadLegacyStorageSystem.TryGetSelectedBuilding(out RuntimeBuildingData building)
                ? $"{building.Definition.DisplayName} ({building.OriginCell.x},{building.OriginCell.y})"
                : "Building";
        }
    }

    public string ActiveModeStatusText
    {
        get
        {
            if (_roadBuildSessionSystem.IsActiveTool(_roadBuildSessionState, BuildToolMode.Road))
                return "Road build mode active";
            if (HasSelectedBuilding)
                return "Building selected";
            if (_runtimeGameplayStateSystem.BuildModeActive)
                return "Build mode active";
            return "Simulation running";
        }
    }

    public void Init(
        RoadBuildSystemConfig configAsset,
        Camera sceneWorldCamera,
        Transform runtimeRoot,
        BuildingPlacementInteractionSystem buildingPlacementInteractionSystem,
        BuildingPlacementInteractionSystem.Context buildingPlacementInteractionContext = default)
    {
        config = configAsset;
        worldCamera = sceneWorldCamera;
        _runtimeRoot = runtimeRoot;
        _buildingPlacementInteractionSystem = buildingPlacementInteractionSystem;
        _buildingPlacementInteractionContext = buildingPlacementInteractionContext;
        ApplyConfigIfAvailable();
        _runtimeRoots = _roadRuntimeRootSystem.CreateRoots(runtimeRoot);

        CacheVariants();
        BuildDefinitions();
        CreatePlacementOutline();
    }

    public void BindDependencies(
        BuildingPlacementInteractionSystem buildingPlacementInteractionSystem,
        BuildingPlacementInteractionSystem.Context buildingPlacementInteractionContext = default,
        MainMenuPlayUI mainMenuPlayUi = null,
        RuntimeGridBlockerSystem runtimeGridBlockers = null)
    {
        _buildingPlacementInteractionSystem = buildingPlacementInteractionSystem;
        _buildingPlacementInteractionContext = buildingPlacementInteractionContext;
        _mainMenuPlayUi = mainMenuPlayUi;
        _roadMinimapEventSystem.Configure(mainMenuPlayUi);
        if (runtimeGridBlockers != null)
            _runtimeGridBlockers = runtimeGridBlockers;
    }

    private void ApplyConfigIfAvailable()
    {
        if (!_roadBuildConfigSystem.TryCreateSnapshot(config, out RoadBuildConfigSystem.Snapshot snapshot))
            return;

        ApplyConfigSnapshot(snapshot);
    }

    private void ApplyConfigSnapshot(RoadBuildConfigSystem.Snapshot snapshot)
    {
        if (snapshot.WorldCamera != null)
            worldCamera = snapshot.WorldCamera;
        straightPrefab = snapshot.StraightPrefab;
        tIntersectionPrefab = snapshot.TIntersectionPrefab;
        intersectionPrefab = snapshot.IntersectionPrefab;
        endPrefab = snapshot.EndPrefab;
        cornerPrefab = snapshot.CornerPrefab;
        autobahnPrefab = snapshot.AutobahnPrefab;
        autobahnConnectPrefab = snapshot.AutobahnConnectPrefab;
        gridOrigin = snapshot.GridOrigin;
        buildPlaneY = snapshot.BuildPlaneY;
        roadGridSize = snapshot.RoadGridSize;
        chunkSizeInCells = snapshot.ChunkSizeInCells;
        previewAlpha = snapshot.PreviewAlpha;
        soldierBasePrefab = snapshot.SoldierBasePrefab;
        soldierBaseFootprintCells = snapshot.SoldierBaseFootprintCells;
        placementOutlineHeight = snapshot.PlacementOutlineHeight;
        placementOutlineWidth = snapshot.PlacementOutlineWidth;
        placementValidColor = snapshot.PlacementValidColor;
        placementInvalidColor = snapshot.PlacementInvalidColor;
    }

    public void Dispose()
    {
        ExitBuildMode();
        _roadBuildSessionSystem.ResetSkipBuildClickFrames(_roadBuildSessionState);

        _roadRuntimeRootSystem.DisposeRoots(_runtimeRoots);
        _runtimeRoots = default;

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

    public static void SetBuildMode(bool enabled)
    {
        var commandSystem = new RoadBuildCommandSystem();
        var runtimeGameplayStateSystem = new RuntimeGameplayStateSystem();
        commandSystem.SetBuildMode(
            new RoadBuildCommandSystem.Context(
                runtimeGameplayStateSystem,
                new RoadBuildSessionSystem(),
                default,
                null),
            enabled);
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
        _buildingPlacementInteractionSystem?.BeginSoldierBasePlacement(_buildingPlacementInteractionContext);
    }

    public void ConfirmBuildingPlacement()
    {
        _buildingPlacementInteractionSystem?.ConfirmBuildingPlacement(_buildingPlacementInteractionContext);
    }

    public void CancelBuildingPlacement()
    {
        _buildingPlacementInteractionSystem?.CancelBuildingPlacement(_buildingPlacementInteractionContext);
    }

    public void CreateSoldierFromSelectedBuilding()
    {
        _buildingPlacementInteractionSystem?.CreateUnitFromSelectedBuilding(_buildingPlacementInteractionContext);
    }

    public void DeleteSelectedBuilding()
    {
        _buildingPlacementInteractionSystem?.DeleteSelectedBuilding(_buildingPlacementInteractionContext);
    }

    public void ClearSelectedBuilding()
    {
        _buildingPlacementInteractionSystem?.ClearSelectedBuilding(_buildingPlacementInteractionContext, "RoadBuild.ClearSelectedBuilding");
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
        return _roadVisualVariantSystem.GetPrefab(CreateRoadPrefabSet(), type);
    }

    private void CacheVariants()
    {
        _roadVisualVariantSystem.CacheVariants(CreateRoadPrefabSet());
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
        _placementOutline.transform.SetParent(_runtimeRoot, false);
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

        _runtimeGridBlockers?.RemoveBlockersOverlappingFootprint(placement.OriginCell, placement.Definition.FootprintCells);
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
                    (_runtimeGridBlockers == null || !_runtimeGridBlockers.IsRuntimeBlockerCell(x, y, grid.Width, grid.Height)))
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
            _runtimeGridBlockers);
    }

    private void ClearRoadDataInEcs()
    {
        _roadGridProjectionSystem.ClearRoadDataInEcs();
    }
}
