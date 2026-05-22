using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using Unity.Transforms;
using static UnityEngine.Object;

public sealed class RoadBuildSystem
{
    public enum BuildToolMode
    {
        None,
        Road,
        SoldierBase
    }

    private enum DragFirstAxis
    {
        None,
        Horizontal,
        Vertical
    }

    private enum RoadVisualType
    {
        None,
        End,
        Straight,
        Corner,
        TIntersection,
        Intersection,
        Autobahn,
        AutobahnConnect
    }

    private readonly struct TileConnectionMask : IEquatable<TileConnectionMask>
    {
        public readonly bool North;
        public readonly bool East;
        public readonly bool South;
        public readonly bool West;

        public TileConnectionMask(bool north, bool east, bool south, bool west)
        {
            North = north;
            East = east;
            South = south;
            West = west;
        }

        public int Count =>
            (North ? 1 : 0) +
            (East ? 1 : 0) +
            (South ? 1 : 0) +
            (West ? 1 : 0);

        public bool Equals(TileConnectionMask other) =>
            North == other.North &&
            East == other.East &&
            South == other.South &&
            West == other.West;

        public override bool Equals(object obj) => obj is TileConnectionMask other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(North, East, South, West);
    }

    private readonly struct EdgeKey : IEquatable<EdgeKey>
    {
        public readonly Vector2Int A;
        public readonly Vector2Int B;

        public EdgeKey(Vector2Int first, Vector2Int second)
        {
            if (first.x < second.x || (first.x == second.x && first.y <= second.y))
            {
                A = first;
                B = second;
            }
            else
            {
                A = second;
                B = first;
            }
        }

        public bool Equals(EdgeKey other) => A == other.A && B == other.B;

        public override bool Equals(object obj) => obj is EdgeKey other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(A, B);
    }

    private sealed class StrokeData
    {
        public int Id;
        public List<Vector2Int> Cells = new();
        public bool IsAutobahn;
        public bool UseAutobahnConnectorAtStart;
        public bool UseAutobahnConnectorAtEnd;
    }

    private sealed class RoadTileData
    {
        public RoadVisualType Type;
        public TileConnectionMask Mask;
        public Quaternion Rotation;
        public Vector3 Scale;
    }

    private sealed class ChunkRenderData
    {
        public GameObject GameObject;
        public Mesh Mesh;
    }

    private sealed class CombinedRoadVisualData
    {
        public Mesh Mesh;
        public Material[] Materials;
        public List<FootprintBoundsData> FootprintBounds = new();
    }

    private enum FootprintKind
    {
        Dirt,
        Sidewalk
    }

    private sealed class FootprintBoundsData
    {
        public Bounds Bounds;
        public FootprintKind Kind;
    }

    private struct RoadBuffersData
    {
        public DynamicBuffer<GridRoad> Roads;
        public DynamicBuffer<GridRoadSidewalk> Sidewalks;
        public DynamicBuffer<GridRoadDirt> DirtRoads;
        public GridConfig Grid;

        public RoadBuffersData(
            DynamicBuffer<GridRoad> roads,
            DynamicBuffer<GridRoadSidewalk> sidewalks,
            DynamicBuffer<GridRoadDirt> dirtRoads,
            GridConfig grid)
        {
            Roads = roads;
            Sidewalks = sidewalks;
            DirtRoads = dirtRoads;
            Grid = grid;
        }
    }

    private sealed class RoadBuildSessionSnapshot
    {
        public int NextStrokeId;
        public Dictionary<EdgeKey, int> EdgeCounts = new();
        public Dictionary<Vector2Int, List<int>> StrokeIdsByCell = new();
        public Dictionary<int, StrokeData> Strokes = new();
        public Dictionary<Vector2Int, RoadTileData> RoadTiles = new();
    }

    private sealed class BuildingDefinition
    {
        public string DisplayName;
        public GameObject Prefab;
        public Vector2Int FootprintCells;
        public Bounds LocalBounds;
        public bool HasLocalBounds;
    }

    private sealed class RuntimeBuildingData
    {
        public int Id;
        public BuildingDefinition Definition;
        public GameObject Instance;
        public Vector2Int OriginCell;
        public Entity CombatEntity;
        public Entity BlockerEntity;
    }

    private sealed class BuildingPlacementState
    {
        public BuildingDefinition Definition;
        public GameObject PreviewInstance;
        public Vector2Int OriginCell;
        public bool IsValid;
    }

    private readonly struct VariantData
    {
        public readonly Quaternion Rotation;
        public readonly Vector3 Scale;

        public VariantData(Quaternion rotation, Vector3 scale)
        {
            Rotation = rotation;
            Scale = scale;
        }
    }

    private readonly struct ConnectorMarkerData
    {
        public readonly Vector3 RoadConnectLocalPosition;
        public readonly Vector3 AutobahnConnectLocalPosition;
        public readonly Vector3 Center;

        public ConnectorMarkerData(Vector3 roadConnectLocalPosition, Vector3 autobahnConnectLocalPosition, Vector3 center)
        {
            RoadConnectLocalPosition = roadConnectLocalPosition;
            AutobahnConnectLocalPosition = autobahnConnectLocalPosition;
            Center = center;
        }
    }

    private sealed class MarkerLayoutData
    {
        public readonly List<Vector3> ConnectLocalPositions = new();
        public Vector3? RoadConnectLocalPosition;
        public Vector3? AutobahnConnectLocalPosition;
        public Vector3 Center;
    }

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

    private readonly Dictionary<EdgeKey, int> _edgeCounts = new();
    private readonly Dictionary<Vector2Int, List<int>> _strokeIdsByCell = new();
    private readonly Dictionary<int, StrokeData> _strokes = new();
    private readonly Dictionary<Vector2Int, RoadTileData> _roadTiles = new();
    private readonly Dictionary<Vector2Int, ChunkRenderData> _chunks = new();
    private readonly Dictionary<Vector2Int, HashSet<Vector2Int>> _chunkCells = new();
    private readonly HashSet<Vector2Int> _dirtyChunks = new();
    private readonly Dictionary<RoadVisualType, Dictionary<TileConnectionMask, VariantData>> _variants = new();
    private readonly Dictionary<RoadVisualType, CombinedRoadVisualData> _visualData = new();
    private readonly Dictionary<RoadVisualType, MarkerLayoutData> _markerLayouts = new();
    private ConnectorMarkerData? _autobahnConnectorMarkerData;
    private readonly List<GameObject> _previewObjects = new();
    private readonly Dictionary<RoadVisualType, Stack<GameObject>> _previewPool = new();
    private readonly Dictionary<GameObject, RoadVisualType> _previewObjectTypes = new();
    private readonly Dictionary<int, RuntimeBuildingData> _runtimeBuildings = new();
    private readonly HashSet<Vector2Int> _autobahnCells = new();
    private readonly HashSet<Vector2Int> _autobahnConnectorCells = new();
    private readonly Dictionary<Vector2Int, GameObject> _specialRoadObjects = new();
    private readonly List<GameObject> _debugStraightRoadObjects = new();

    private int _nextStrokeId = 1;
    private int _nextBuildingId = 1;
    private uint _buildingSpawnRandomState = 0x12345678u;
    private Transform _roadRoot;
    private Transform _specialRoadRoot;
    private Transform _specialRoadConnectorRoot;
    private Transform _debugStraightRoadRoot;
    private Transform _buildingRoot;
    private GameObject _placementOutline;
    private Transform[] _placementOutlineEdges;
    private MeshRenderer[] _placementOutlineRenderers;
    private Vector2Int? _pendingStartCell;
    private Vector2Int _currentDragCell;
    private int? _pendingDeleteStrokeId;
    private string _pendingDeleteMessage;
    private int _skipBuildClickFrames;
    private bool _isDrawing;
    private bool _isDraggingBuildingPlacement;
    private bool _pressedOnExistingRoad;
    private Vector2Int _pressedRoadCell;
    private int _pressedRoadStrokeId;
    private DragFirstAxis _dragFirstAxis;
    private BuildToolMode _activeBuildTool;
    private BuildingDefinition _soldierBaseDefinition;
    private BuildingPlacementState _activeBuildingPlacement;
    private RoadBuildSessionSnapshot _roadBuildSessionSnapshot;
    private int? _selectedBuildingId;
    private BuildingPlacementSystem _buildingPlacementController;
    private MainMenuPlayUI _mainMenuPlayUi;
    private RuntimeGridBlockerSystem _runtimeGridBlockers;
    private World _queryWorld;
    private EntityQuery _gridDataQuery;
    private EntityQuery _roadBufferQuery;
    private EntityQuery _roadBuffersQuery;
    private int _deferRoadEcsSyncDepth;
    private bool _pendingRoadEcsSync;
    private Transform _runtimeRoot;

    private static readonly Vector2Int North = new(0, 1);
    private static readonly Vector2Int East = new(1, 0);
    private static readonly Vector2Int South = new(0, -1);
    private static readonly Vector2Int West = new(-1, 0);

    private void EnsureEntityQueries(EntityManager entityManager)
    {
        World world = entityManager.World;
        if (_queryWorld == world && world != null && world.IsCreated)
            return;

        _queryWorld = world;
        _gridDataQuery = entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<GridConfig>(),
            ComponentType.ReadOnly<GridRoad>(),
            ComponentType.ReadOnly<DynamicBlockerData>());
        _roadBufferQuery = entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<GridConfig>(),
            ComponentType.ReadWrite<GridRoad>());
        _roadBuffersQuery = entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<GridConfig>(),
            ComponentType.ReadWrite<GridRoad>(),
            ComponentType.ReadWrite<GridRoadSidewalk>(),
            ComponentType.ReadWrite<GridRoadDirt>());
    }

    public bool HasPendingBuildingPlacement => _activeBuildingPlacement != null;

    public void BeginDeferredRoadEcsSync()
    {
        _deferRoadEcsSyncDepth++;
    }

    public void EndDeferredRoadEcsSync()
    {
        if (_deferRoadEcsSyncDepth <= 0)
            return;

        _deferRoadEcsSyncDepth--;
        if (_deferRoadEcsSyncDepth == 0 && _pendingRoadEcsSync)
        {
            SyncRoadCellsToEcs();
            _pendingRoadEcsSync = false;
        }
    }

    public bool CanConfirmBuildingPlacement =>
        _buildingPlacementController != null
            ? _buildingPlacementController.CanConfirmBuildingPlacement
            : _activeBuildingPlacement != null && _activeBuildingPlacement.IsValid;

    public bool HasSelectedBuilding =>
        _buildingPlacementController != null
            ? _buildingPlacementController.HasSelectedBuilding
            : _selectedBuildingId.HasValue && _runtimeBuildings.ContainsKey(_selectedBuildingId.Value);

    public bool IsRoadBuildModeActive => InitialUnitsRuntimeState.BuildModeActive && _activeBuildTool == BuildToolMode.Road;
    public bool IsDraggingBuildInteraction => _isDrawing || (_activeBuildingPlacement != null && _isDraggingBuildingPlacement);

    private void OnValidate()
    {
        ApplyConfigIfAvailable();
    }

    public bool TryGetRoadCellSizeInGridCells(out int roadCellSizeInGridCells)
    {
        roadCellSizeInGridCells = 0;
        if (roadGridSize <= 0f)
            return false;
        if (!TryGetGridData(out _, out GridConfig grid, out _, out _))
            return false;
        if (grid.CellSize <= 0f)
            return false;

        roadCellSizeInGridCells = Mathf.Max(1, Mathf.RoundToInt(roadGridSize / grid.CellSize));
        return true;
    }

    public bool CreateRoadStrokeFromRoadCells(IReadOnlyList<Vector2Int> cells)
    {
        if (cells == null || cells.Count < 2)
            return false;

        var path = new List<Vector2Int>(cells.Count);
        for (int i = 0; i < cells.Count; i++)
            path.Add(cells[i]);

        CreateStroke(path);
        return true;
    }

    public bool CreateAutobahnStrokeFromRoadCells(IReadOnlyList<Vector2Int> cells)
    {
        return CreateAutobahnStrokeFromRoadCells(cells, true, false);
    }

    public bool CreateAutobahnStrokeFromRoadCells(
        IReadOnlyList<Vector2Int> cells,
        bool useAutobahnConnectorAtStart,
        bool useAutobahnConnectorAtEnd)
    {
        if (cells == null || cells.Count < 3)
            return false;

        var path = new List<Vector2Int>(cells.Count);
        for (int i = 0; i < cells.Count; i++)
            path.Add(cells[i]);

        CreateStroke(
            path,
            isAutobahn: true,
            useAutobahnConnectorAtStart: useAutobahnConnectorAtStart,
            useAutobahnConnectorAtEnd: useAutobahnConnectorAtEnd);
        return true;
    }

    public bool TryGetAutobahnConnectorRoadCell(Vector2Int connectorCell, out Vector2Int roadConnectionCell)
    {
        roadConnectionCell = default;

        if (!_specialRoadObjects.TryGetValue(connectorCell, out var connectorObject) || connectorObject == null)
            return false;
        if (!_markerLayouts.TryGetValue(RoadVisualType.AutobahnConnect, out var connectorLayout) ||
            !connectorLayout.RoadConnectLocalPosition.HasValue)
            return false;
        if (roadGridSize <= 0f)
            return false;

        Vector3 worldPosition = GetObjectMarkerWorldPosition(
            connectorObject.transform,
            connectorLayout.RoadConnectLocalPosition.Value);
        Vector3 localPoint = worldPosition - gridOrigin;
        Vector2 rawRoadCell = new(
            localPoint.x / roadGridSize,
            localPoint.z / roadGridSize);
        roadConnectionCell = new Vector2Int(
            Mathf.RoundToInt(rawRoadCell.x),
            Mathf.RoundToInt(rawRoadCell.y));
        return true;
    }

    public bool TryLogRoadConnectMarkers(Vector2Int roadCell)
    {
        if (!_roadTiles.TryGetValue(roadCell, out var tile))
            return false;
        if (!_markerLayouts.TryGetValue(tile.Type, out var layout) || layout.ConnectLocalPositions.Count == 0)
            return false;

        VariantData variant = new(tile.Rotation, tile.Scale);
        Vector3 placedRoadPosition = GetPlacementPosition(roadCell, variant);
        for (int i = 0; i < layout.ConnectLocalPositions.Count; i++)
        {
            Vector3 worldPosition = placedRoadPosition + variant.Rotation * Vector3.Scale(layout.ConnectLocalPositions[i], variant.Scale);
        }

        return true;
    }

    public bool CreateStandaloneStraightRoadChainFromConnector(Vector2Int connectorCell, Vector2Int direction, int length)
    {
        ClearDebugStraightRoadObjects();

        if (length <= 0)
            return false;
        if (!_specialRoadObjects.TryGetValue(connectorCell, out var connectorObject) || connectorObject == null)
            return false;
        if (!_markerLayouts.TryGetValue(RoadVisualType.AutobahnConnect, out var connectorLayout) ||
            !connectorLayout.RoadConnectLocalPosition.HasValue)
            return false;
        if (!_markerLayouts.TryGetValue(RoadVisualType.Straight, out var straightLayout) ||
            straightLayout.ConnectLocalPositions.Count < 2)
            return false;
        if (!TryGetVariant(RoadVisualType.Straight, BuildAxisMask(direction), out var straightVariant))
            return false;

        Vector3 previousConnectWorldPosition = GetObjectMarkerWorldPosition(
            connectorObject.transform,
            connectorLayout.RoadConnectLocalPosition.Value);

        for (int i = 0; i < length; i++)
        {
            GameObject prefab = GetPrefab(RoadVisualType.Straight);
            if (prefab == null)
                return false;

            GameObject roadObject = Instantiate(
                prefab,
                _debugStraightRoadRoot != null ? _debugStraightRoadRoot : _roadRoot);
            roadObject.name = $"{prefab.name}_Debug_{connectorCell.x}_{connectorCell.y}_{i}";

            Vector3 incomingMarkerLocalPosition = GetMarkerLocalPositionForDirection(
                straightLayout,
                straightVariant,
                -direction);
            Vector3 outgoingMarkerLocalPosition = GetMarkerLocalPositionForDirection(
                straightLayout,
                straightVariant,
                direction);

            PlaceObjectByMarker(
                roadObject.transform,
                straightVariant,
                incomingMarkerLocalPosition,
                previousConnectWorldPosition);
            previousConnectWorldPosition = GetObjectMarkerWorldPosition(
                roadObject.transform,
                outgoingMarkerLocalPosition);
            _debugStraightRoadObjects.Add(roadObject);
        }

        return true;
    }

    public bool TryGetStandaloneStraightChainEndRoadCell(Vector2Int direction, out Vector2Int roadConnectionCell)
    {
        roadConnectionCell = default;

        if (_debugStraightRoadObjects.Count == 0)
            return false;
        if (!_markerLayouts.TryGetValue(RoadVisualType.Straight, out var straightLayout) ||
            straightLayout.ConnectLocalPositions.Count < 2)
            return false;
        if (roadGridSize <= 0f)
            return false;

        GameObject lastStraight = _debugStraightRoadObjects[_debugStraightRoadObjects.Count - 1];
        if (lastStraight == null)
            return false;

        VariantData variant = new(lastStraight.transform.rotation, lastStraight.transform.localScale);
        Vector3 endMarkerWorldPosition = GetObjectMarkerWorldPosition(
            lastStraight.transform,
            GetMarkerLocalPositionForDirection(straightLayout, variant, direction));

        Vector3 localPoint = endMarkerWorldPosition - gridOrigin;
        roadConnectionCell = new Vector2Int(
            Mathf.RoundToInt(localPoint.x / roadGridSize),
            Mathf.RoundToInt(localPoint.z / roadGridSize));
        return true;
    }

    public bool CreateStandaloneDebugCityRoadNetworkFromStraightChain(Vector2Int direction, int branchLength)
    {
        if (_debugStraightRoadObjects.Count == 0 || branchLength <= 0)
            return false;
        if (!_markerLayouts.TryGetValue(RoadVisualType.Straight, out var straightLayout) ||
            straightLayout.ConnectLocalPositions.Count < 2)
            return false;
        if (!_markerLayouts.TryGetValue(RoadVisualType.Intersection, out var intersectionLayout) ||
            intersectionLayout.ConnectLocalPositions.Count < 4)
            return false;

        GameObject lastStraight = _debugStraightRoadObjects[_debugStraightRoadObjects.Count - 1];
        VariantData lastStraightVariant = new(lastStraight.transform.rotation, lastStraight.transform.localScale);
        Vector3 chainEndWorldPosition = GetObjectMarkerWorldPosition(
            lastStraight.transform,
            GetMarkerLocalPositionForDirection(straightLayout, lastStraightVariant, direction));

        Vector2Int leftDirection = new(-direction.y, direction.x);
        Vector2Int rightDirection = new(direction.y, -direction.x);
        TileConnectionMask intersectionMask = BuildMaskFromDirections(direction, -direction, leftDirection, rightDirection);
        if (!TryGetVariant(RoadVisualType.Intersection, intersectionMask, out var intersectionVariant))
            return false;
        GameObject intersectionPrefab = GetPrefab(RoadVisualType.Intersection);
        if (intersectionPrefab == null)
            return false;

        GameObject intersectionObject = Instantiate(
            intersectionPrefab,
            _debugStraightRoadRoot != null ? _debugStraightRoadRoot : _roadRoot);
        intersectionObject.name = $"{intersectionPrefab.name}_DebugCityHub";
        PlaceObjectByMarker(
            intersectionObject.transform,
            intersectionVariant,
            GetMarkerLocalPositionForDirection(intersectionLayout, intersectionVariant, -direction),
            chainEndWorldPosition);
        _debugStraightRoadObjects.Add(intersectionObject);

        CreateStandaloneStraightBranch(intersectionObject.transform, intersectionLayout, intersectionVariant, direction, branchLength + 3);
        CreateStandaloneStraightBranch(intersectionObject.transform, intersectionLayout, intersectionVariant, leftDirection, branchLength + 2);
        CreateStandaloneStraightBranch(intersectionObject.transform, intersectionLayout, intersectionVariant, rightDirection, branchLength + 2);

        return true;
    }

    public bool HasRoadInFootprint(GridConfig grid, Vector2Int originCell, Vector2Int footprintCells)
    {
        if (_roadTiles.Count == 0)
            return false;

        int buildingMinX = originCell.x;
        int buildingMinY = originCell.y;
        int buildingMaxX = originCell.x + footprintCells.x;
        int buildingMaxY = originCell.y + footprintCells.y;

        foreach (var entry in _roadTiles)
        {
            Vector2Int roadCell = entry.Key;
            bool foundOverlap = false;
            ForEachRoadWorldFootprint(roadCell, entry.Value, (worldMin, worldMax) =>
            {
                float3 localMin = (float3)(worldMin - (Vector3)grid.Origin);
                float3 localMax = (float3)(worldMax - (Vector3)grid.Origin);

                int minX = Mathf.Clamp(Mathf.FloorToInt(localMin.x / grid.CellSize), 0, grid.Width);
                int minY = Mathf.Clamp(Mathf.FloorToInt(localMin.z / grid.CellSize), 0, grid.Height);
                int maxX = Mathf.Clamp(Mathf.CeilToInt(localMax.x / grid.CellSize), 0, grid.Width);
                int maxY = Mathf.Clamp(Mathf.CeilToInt(localMax.z / grid.CellSize), 0, grid.Height);

                bool overlaps = false;
                for (int y = minY; y < maxY && !overlaps; y++)
                {
                    for (int x = minX; x < maxX; x++)
                    {
                        if (!IsGridCellCenterInsideBounds(grid, x, y, worldMin, worldMax))
                            continue;

                        if (x < buildingMinX || y < buildingMinY || x >= buildingMaxX || y >= buildingMaxY)
                            continue;

                        overlaps = true;
                        break;
                    }
                }

                if (!overlaps)
                    return true;

                foundOverlap = true;
                return false;
            });

            if (foundOverlap)
                return true;
        }

        return false;
    }

    public void FillRoadFootprintMask(GridConfig grid, bool[] occupiedCells)
    {
        if (occupiedCells == null || occupiedCells.Length < grid.Width * grid.Height)
            return;

        foreach (var entry in _roadTiles)
        {
            Vector2Int roadCell = entry.Key;
            ForEachRoadWorldFootprint(roadCell, entry.Value, (worldMin, worldMax) =>
            {
                float3 localMin = (float3)(worldMin - (Vector3)grid.Origin);
                float3 localMax = (float3)(worldMax - (Vector3)grid.Origin);

                int minX = Mathf.Clamp(Mathf.FloorToInt(localMin.x / grid.CellSize), 0, grid.Width);
                int minY = Mathf.Clamp(Mathf.FloorToInt(localMin.z / grid.CellSize), 0, grid.Height);
                int maxX = Mathf.Clamp(Mathf.CeilToInt(localMax.x / grid.CellSize), 0, grid.Width);
                int maxY = Mathf.Clamp(Mathf.CeilToInt(localMax.z / grid.CellSize), 0, grid.Height);

                for (int y = minY; y < maxY; y++)
                {
                    for (int x = minX; x < maxX; x++)
                    {
                        if (!IsGridCellCenterInsideBounds(grid, x, y, worldMin, worldMax))
                            continue;

                        occupiedCells[GridUtils.CellToIndex(new int2(x, y), grid.Width)] = true;
                    }
                }

                return true;
            });
        }
    }

    public string PlacementStatusText
    {
        get
        {
            if (_buildingPlacementController != null && _buildingPlacementController.HasPendingBuildingPlacement)
                return _buildingPlacementController.PlacementStatusText;

            if (_activeBuildingPlacement == null)
                return "Choose a build type.";

            string state = _activeBuildingPlacement.IsValid ? "Valid placement" : "Blocked by road or blocker";
            Vector2Int origin = _activeBuildingPlacement.OriginCell;
            Vector2Int size = _activeBuildingPlacement.Definition.FootprintCells;
            return $"{_activeBuildingPlacement.Definition.DisplayName}: {state} ({origin.x},{origin.y}) {size.x}x{size.y}";
        }
    }

    public string SelectedBuildingLabel
    {
        get
        {
            if (_buildingPlacementController != null && _buildingPlacementController.HasActiveBuilding)
                return _buildingPlacementController.SelectedBuildingLabel;

            if (!HasSelectedBuilding)
                return "Building";

            RuntimeBuildingData building = _runtimeBuildings[_selectedBuildingId.Value];
            return $"{building.Definition.DisplayName} ({building.OriginCell.x},{building.OriginCell.y})";
        }
    }

    public string ActiveModeStatusText
    {
        get
        {
            if (_activeBuildTool == BuildToolMode.Road)
                return "Road build mode active";
            if (HasSelectedBuilding)
                return "Building selected";
            if (InitialUnitsRuntimeState.BuildModeActive)
                return "Build mode active";
            return "Simulation running";
        }
    }

    public void Init(RoadBuildSystemConfig configAsset, Camera sceneWorldCamera, Transform runtimeRoot, BuildingPlacementSystem buildingPlacementController)
    {
        config = configAsset;
        worldCamera = sceneWorldCamera;
        _runtimeRoot = runtimeRoot;
        _buildingPlacementController = buildingPlacementController;
        ApplyConfigIfAvailable();
        _roadRoot = CreateRuntimeChildRoot("RuntimeRoads");
        _specialRoadRoot = CreateRuntimeChildRoot("RuntimeAutobahns");
        _specialRoadConnectorRoot = CreateRuntimeChildRoot("RuntimeAutobahnConnectors");
        _debugStraightRoadRoot = CreateRuntimeChildRoot("RuntimeDebugStraightRoads");
        _buildingRoot = CreateRuntimeChildRoot("RuntimeBuildings");

        CacheVariants();
        BuildDefinitions();
        CreatePlacementOutline();
    }

    public void BindDependencies(
        BuildingPlacementSystem buildingPlacementController,
        MainMenuPlayUI mainMenuPlayUi = null,
        RuntimeGridBlockerSystem runtimeGridBlockers = null)
    {
        _buildingPlacementController = buildingPlacementController;
        _mainMenuPlayUi = mainMenuPlayUi;
        if (runtimeGridBlockers != null)
            _runtimeGridBlockers = runtimeGridBlockers;
    }

    private void ApplyConfigIfAvailable()
    {
        if (config == null)
            return;

        if (config.WorldCamera != null)
            worldCamera = config.WorldCamera;
        straightPrefab = config.StraightPrefab;
        tIntersectionPrefab = config.TIntersectionPrefab;
        intersectionPrefab = config.IntersectionPrefab;
        endPrefab = config.EndPrefab;
        cornerPrefab = config.CornerPrefab;
        autobahnPrefab = config.AutobahnPrefab;
        autobahnConnectPrefab = config.AutobahnConnectPrefab;
        gridOrigin = config.GridOrigin;
        buildPlaneY = config.BuildPlaneY;
        roadGridSize = config.RoadGridSize;
        chunkSizeInCells = config.ChunkSizeInCells;
        previewAlpha = config.PreviewAlpha;
        soldierBasePrefab = config.SoldierBasePrefab;
        soldierBaseFootprintCells = config.SoldierBaseFootprintCells;
        placementOutlineHeight = config.PlacementOutlineHeight;
        placementOutlineWidth = config.PlacementOutlineWidth;
        placementValidColor = config.PlacementValidColor;
        placementInvalidColor = config.PlacementInvalidColor;
    }

    public void Dispose()
    {
        ExitBuildMode();
        _skipBuildClickFrames = 0;

        if (_roadRoot != null)
        {
            Destroy(_roadRoot.gameObject);
            _roadRoot = null;
        }

        if (_specialRoadRoot != null)
        {
            Destroy(_specialRoadRoot.gameObject);
            _specialRoadRoot = null;
        }

        if (_specialRoadConnectorRoot != null)
        {
            Destroy(_specialRoadConnectorRoot.gameObject);
            _specialRoadConnectorRoot = null;
        }

        if (_debugStraightRoadRoot != null)
        {
            Destroy(_debugStraightRoadRoot.gameObject);
            _debugStraightRoadRoot = null;
        }

        if (_buildingRoot != null)
        {
            Destroy(_buildingRoot.gameObject);
            _buildingRoot = null;
        }

        if (_placementOutline != null)
        {
            Destroy(_placementOutline);
            _placementOutline = null;
            _placementOutlineEdges = null;
            _placementOutlineRenderers = null;
        }

        foreach (var visual in _visualData.Values)
        {
            if (visual.Mesh != null)
                Destroy(visual.Mesh);
        }

        _previewObjects.Clear();
        _previewPool.Clear();
        _previewObjectTypes.Clear();
        foreach (var chunk in _chunks.Values)
        {
            if (chunk.Mesh != null)
                Destroy(chunk.Mesh);
        }

        foreach (var building in _runtimeBuildings.Values)
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

        foreach (var roadObject in _specialRoadObjects.Values)
        {
            if (roadObject != null)
                Destroy(roadObject);
        }

        foreach (var roadObject in _debugStraightRoadObjects)
        {
            if (roadObject != null)
                Destroy(roadObject);
        }

        _specialRoadObjects.Clear();
        _debugStraightRoadObjects.Clear();

        ClearRoadDataInEcs();

        _roadTiles.Clear();
        _chunks.Clear();
        _chunkCells.Clear();
        _dirtyChunks.Clear();
        _visualData.Clear();
        _runtimeBuildings.Clear();
    }

    public void Update()
    {
        bool roadModeActive = InitialUnitsRuntimeState.PlayRequested && InitialUnitsRuntimeState.BuildModeActive && _activeBuildTool == BuildToolMode.Road;
        if (!roadModeActive)
        {
            ClearPreview();
        }

        if (worldCamera == null)
            return;

        if (!GamePointerInput.TryGetPrimaryPointer(out GamePointerState pointer))
            return;

        if (_skipBuildClickFrames > 0)
        {
            _skipBuildClickFrames--;
            return;
        }

        if (_activeBuildingPlacement != null)
        {
            Vector2 pointerPosition = pointer.Position;
            bool pointerOverUi = IsPointerOverUI(pointerPosition);
            if (pointer.WasPressedThisFrame && !pointerOverUi)
                _isDraggingBuildingPlacement = true;
            if (pointer.WasReleasedThisFrame)
                _isDraggingBuildingPlacement = false;

            UpdateBuildingPlacement(pointerPosition);
            return;
        }

        if (!InitialUnitsRuntimeState.PlayRequested || !InitialUnitsRuntimeState.BuildModeActive)
        {
            HidePlacementOutline();
            return;
        }

        if (_activeBuildTool == BuildToolMode.Road)
            UpdatePreview();

        if (_pendingDeleteStrokeId.HasValue)
            return;

        if (_activeBuildTool != BuildToolMode.Road)
            return;

        bool hasHoveredCell = TryGetHoveredCell(pointer.Position, out var cell);

        if (pointer.WasPressedThisFrame)
            HandlePointerPressed(hasHoveredCell, cell);

        if (_isDrawing && pointer.IsPressed && hasHoveredCell)
        {
            _currentDragCell = cell;
            UpdateDragAxis(cell);
            UpdatePreview();
        }

        if (pointer.WasReleasedThisFrame)
            HandlePointerReleased(hasHoveredCell, cell);
    }

    public void OnGui()
    {
        if (!InitialUnitsRuntimeState.PlayRequested || !InitialUnitsRuntimeState.BuildModeActive || !_pendingDeleteStrokeId.HasValue)
            return;

        const int deleteRoadWindowId = 12001;
        const float width = 320f;
        const float height = 150f;
        Rect windowRect = new(
            (Screen.width - width) * 0.5f,
            (Screen.height - height) * 0.5f,
            width,
            height);

        GUI.ModalWindow(deleteRoadWindowId, windowRect, DrawDeleteWindow, "Delete Road");
    }

    public static void SetBuildMode(bool enabled)
    {
        if (enabled && WarlineCaptureMissionRules.TryRejectBuildForActiveMission())
        {
            InitialUnitsRuntimeState.BuildModeActive = false;
            return;
        }

        InitialUnitsRuntimeState.BuildModeActive = enabled;
        if (enabled)
            InitialUnitsRuntimeState.SelectionModeActive = false;
    }

    private Transform CreateRuntimeChildRoot(string name)
    {
        var child = new GameObject(name).transform;
        child.SetParent(_runtimeRoot, false);
        child.localPosition = Vector3.zero;
        child.localRotation = Quaternion.identity;
        child.localScale = Vector3.one;
        return child;
    }

    public void ActivateRoadBuildMode()
    {
        if (WarlineCaptureMissionRules.TryRejectBuildForActiveMission())
            return;

        InitialUnitsRuntimeState.BuildModeActive = true;
        InitialUnitsRuntimeState.SelectionModeActive = false;
        BattleHudGameplayBridge.ResolveActive()?.ApplyCommandMode(TacticalCommandMode.Build);

        if (_activeBuildTool != BuildToolMode.Road)
            BeginRoadBuildSession();

        _activeBuildTool = BuildToolMode.Road;
        ClearSelectedBuilding();
        CancelBuildingPlacementInternal();
        UpdatePreview();
    }

    public void ConfirmRoadBuildSession()
    {
        RemoveRuntimeBlockersUnderRoads();
        _roadBuildSessionSnapshot = null;
        _mainMenuPlayUi?.NotifyStaticMinimapChanged();
    }

    public void CancelRoadBuildSession()
    {
        if (_roadBuildSessionSnapshot == null)
            return;

        RestoreRoadBuildSession(_roadBuildSessionSnapshot);
        _roadBuildSessionSnapshot = null;
        _mainMenuPlayUi?.NotifyStaticMinimapChanged();
    }

    public void BeginSoldierBasePlacement()
    {
        if (WarlineCaptureMissionRules.TryRejectBuildForActiveMission())
            return;

        if (_buildingPlacementController != null)
        {
            _buildingPlacementController.BeginSoldierBasePlacement();
            return;
        }

        if (_soldierBaseDefinition == null || soldierBasePrefab == null)
        {
            Debug.LogWarning("RoadBuildSystem is missing the Soldiers_Base prefab reference.");
            return;
        }

        InitialUnitsRuntimeState.BuildModeActive = true;
        InitialUnitsRuntimeState.SelectionModeActive = false;
        BattleHudGameplayBridge.ResolveActive()?.ApplyCommandMode(TacticalCommandMode.Build);
        _activeBuildTool = BuildToolMode.SoldierBase;
        _pendingDeleteStrokeId = null;
        _pendingDeleteMessage = null;
        CancelPendingBuild();
        ClearSelectedBuilding();
        BeginBuildingPlacement(_soldierBaseDefinition);
    }

    public void ConfirmBuildingPlacement()
    {
        if (_buildingPlacementController != null)
        {
            _buildingPlacementController.ConfirmBuildingPlacement();
            return;
        }

        if (_activeBuildingPlacement == null || !_activeBuildingPlacement.IsValid)
            return;

        PlaceBuilding(_activeBuildingPlacement);
        ExitBuildMode();
    }

    public void CancelBuildingPlacement()
    {
        if (_buildingPlacementController != null)
        {
            _buildingPlacementController.CancelBuildingPlacement();
            return;
        }

        CancelBuildingPlacementInternal();
        _activeBuildTool = BuildToolMode.None;
        HidePlacementOutline();
        BattleHudGameplayBridge.ResolveActive()?.ClearCommandMode();
    }

    public void CreateSoldierFromSelectedBuilding()
    {
        if (_buildingPlacementController != null)
        {
            _buildingPlacementController.CreateUnitFromSelectedBuilding();
            return;
        }

        if (!HasSelectedBuilding)
            return;

        RuntimeBuildingData building = _runtimeBuildings[_selectedBuildingId.Value];
        if (!TrySpawnPlayerUnitNearBuilding(building))
            Debug.LogWarning("Unable to create a soldier for the selected building.");
    }

    public void DeleteSelectedBuilding()
    {
        if (_buildingPlacementController != null)
        {
            _buildingPlacementController.DeleteSelectedBuilding();
            return;
        }

        if (!HasSelectedBuilding)
            return;

        DeleteBuilding(_selectedBuildingId.Value, destroyVisual: true);
        ClearSelectedBuilding();
    }

    public void ClearSelectedBuilding()
    {
        _selectedBuildingId = null;
    }

    public void ExitBuildMode()
    {
        InitialUnitsRuntimeState.BuildModeActive = false;
        _activeBuildTool = BuildToolMode.None;
        _isDraggingBuildingPlacement = false;
        CancelPendingBuild();
        CancelBuildingPlacementInternal();
        ClearSelectedBuilding();
        ClearDeletePrompt();
        HidePlacementOutline();
        BattleHudGameplayBridge.ResolveActive()?.ClearCommandMode();
    }

    private void DrawDeleteWindow(int windowId)
    {
        GUILayout.Space(12f);
        GUILayout.Label(_pendingDeleteMessage ?? "Delete this road?");
        GUILayout.FlexibleSpace();
        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Delete", GUILayout.Height(32f)))
        {
            if (_pendingDeleteStrokeId.HasValue)
                DeleteStroke(_pendingDeleteStrokeId.Value);

            ClearDeletePrompt();
        }

        if (GUILayout.Button("Cancel", GUILayout.Height(32f)))
            ClearDeletePrompt();

        GUILayout.EndHorizontal();
        GUILayout.Space(8f);
    }

    private void HandlePointerPressed(bool hasHoveredCell, Vector2Int cell)
    {
        if (_skipBuildClickFrames > 0 || !hasHoveredCell)
            return;

        if (_strokeIdsByCell.TryGetValue(cell, out var strokeIds) && strokeIds.Count > 0)
        {
            _pressedOnExistingRoad = true;
            _pressedRoadCell = cell;
            _pressedRoadStrokeId = strokeIds[strokeIds.Count - 1];
            ClearPreview();
            return;
        }

        _pressedOnExistingRoad = false;
        _pressedRoadStrokeId = 0;
        _pendingStartCell = cell;
        _currentDragCell = cell;
        _isDrawing = true;
        _dragFirstAxis = DragFirstAxis.None;
        UpdatePreview();
    }

    private void HandlePointerReleased(bool hasHoveredCell, Vector2Int cell)
    {
        if (_skipBuildClickFrames > 0)
            return;

        if (_pressedOnExistingRoad)
        {
            if (hasHoveredCell && cell == _pressedRoadCell)
            {
                _pendingDeleteStrokeId = _pressedRoadStrokeId;
                _pendingDeleteMessage = "Delete the clicked road?";
            }

            _pressedOnExistingRoad = false;
            _pressedRoadStrokeId = 0;
            return;
        }

        if (!_isDrawing || !_pendingStartCell.HasValue)
            return;

        if (hasHoveredCell)
            _currentDragCell = cell;

        List<Vector2Int> path = BuildPath(_pendingStartCell.Value, _currentDragCell, _dragFirstAxis);
        if (path.Count > 1)
            CreateStroke(path);

        CancelPendingBuild();
    }

    private void UpdateDragAxis(Vector2Int hoveredCell)
    {
        if (!_pendingStartCell.HasValue)
            return;

        Vector2Int delta = hoveredCell - _pendingStartCell.Value;
        if (delta.x == 0 && delta.y == 0)
            return;

        if (_dragFirstAxis == DragFirstAxis.None)
            _dragFirstAxis = Mathf.Abs(delta.x) >= Mathf.Abs(delta.y) ? DragFirstAxis.Horizontal : DragFirstAxis.Vertical;
    }

    private void CreateStroke(
        List<Vector2Int> cells,
        bool isAutobahn = false,
        bool useAutobahnConnectorAtStart = false,
        bool useAutobahnConnectorAtEnd = false)
    {
        if (cells.Count < 2)
            return;

        var stroke = new StrokeData
        {
            Id = _nextStrokeId++,
            Cells = cells,
            IsAutobahn = isAutobahn,
            UseAutobahnConnectorAtStart = useAutobahnConnectorAtStart,
            UseAutobahnConnectorAtEnd = useAutobahnConnectorAtEnd
        };

        _strokes.Add(stroke.Id, stroke);

        var dirtyCells = new HashSet<Vector2Int>();
        for (int i = 0; i < cells.Count; i++)
        {
            Vector2Int cell = cells[i];

            if (!_strokeIdsByCell.TryGetValue(cell, out var strokeIds))
            {
                strokeIds = new List<int>();
                _strokeIdsByCell.Add(cell, strokeIds);
            }

            strokeIds.Add(stroke.Id);
            dirtyCells.Add(cell);
            AddNeighborCells(cell, dirtyCells);

            if (i == 0)
                continue;

            AddEdge(cells[i - 1], cell);
        }

        AddEndpointConnections(cells, dirtyCells);
        RebuildSpecialRoadCellMetadata();

        RefreshCells(dirtyCells);
    }

    private void DeleteStroke(int strokeId)
    {
        if (!_strokes.TryGetValue(strokeId, out var stroke))
            return;

        var dirtyCells = new HashSet<Vector2Int>();
        for (int i = 0; i < stroke.Cells.Count; i++)
        {
            Vector2Int cell = stroke.Cells[i];

            if (_strokeIdsByCell.TryGetValue(cell, out var strokeIds))
            {
                strokeIds.Remove(strokeId);
                if (strokeIds.Count == 0)
                    _strokeIdsByCell.Remove(cell);
            }

            dirtyCells.Add(cell);
            AddNeighborCells(cell, dirtyCells);

            if (i == 0)
                continue;

            RemoveEdge(stroke.Cells[i - 1], cell);
        }

        _strokes.Remove(strokeId);
        RebuildSpecialRoadCellMetadata();
        RefreshCells(dirtyCells);
    }

    private void AddEdge(Vector2Int a, Vector2Int b)
    {
        var key = new EdgeKey(a, b);
        _edgeCounts.TryGetValue(key, out int count);
        _edgeCounts[key] = count + 1;
    }

    private void RemoveEdge(Vector2Int a, Vector2Int b)
    {
        var key = new EdgeKey(a, b);
        if (!_edgeCounts.TryGetValue(key, out int count))
            return;

        if (count <= 1)
            _edgeCounts.Remove(key);
        else
            _edgeCounts[key] = count - 1;
    }

    private void RefreshCells(HashSet<Vector2Int> dirtyCells)
    {
        foreach (var cell in dirtyCells)
            RefreshCell(cell);

        if (_deferRoadEcsSyncDepth > 0)
        {
            _pendingRoadEcsSync = true;
        }
        else
        {
            SyncRoadCellsToEcs();
        }

        RebuildDirtyChunks();
        RebuildSpecialRoadObjects(dirtyCells);
    }

    private void RefreshCell(Vector2Int cell)
    {
        TileConnectionMask mask = GetMask(cell);
        RoadVisualType targetType = ResolveVisualType(cell, mask);
        if (targetType == RoadVisualType.None)
        {
            _roadTiles.Remove(cell);
            RemoveCellFromChunk(cell);

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

        AddCellToChunk(cell);
    }

    private TileConnectionMask GetMask(Vector2Int cell)
    {
        return new TileConnectionMask(
            HasEdge(cell, cell + North),
            HasEdge(cell, cell + East),
            HasEdge(cell, cell + South),
            HasEdge(cell, cell + West));
    }

    private bool HasEdge(Vector2Int a, Vector2Int b) => _edgeCounts.ContainsKey(new EdgeKey(a, b));

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
        return type switch
        {
            RoadVisualType.End => endPrefab,
            RoadVisualType.Straight => straightPrefab,
            RoadVisualType.Corner => cornerPrefab,
            RoadVisualType.TIntersection => tIntersectionPrefab,
            RoadVisualType.Intersection => intersectionPrefab,
            RoadVisualType.Autobahn => autobahnPrefab,
            RoadVisualType.AutobahnConnect => autobahnConnectPrefab,
            _ => null
        };
    }

    private void CacheVariants()
    {
        _variants.Clear();
        _visualData.Clear();
        _markerLayouts.Clear();
        _autobahnConnectorMarkerData = null;

        CacheVisualData(RoadVisualType.End, endPrefab);
        CacheVisualData(RoadVisualType.Straight, straightPrefab);
        CacheVisualData(RoadVisualType.Corner, cornerPrefab);
        CacheVisualData(RoadVisualType.TIntersection, tIntersectionPrefab);
        CacheVisualData(RoadVisualType.Intersection, intersectionPrefab);
        CacheVisualData(RoadVisualType.Autobahn, autobahnPrefab);
        CacheVisualData(RoadVisualType.AutobahnConnect, autobahnConnectPrefab);
    }

    private void CacheVisualData(RoadVisualType type, GameObject prefab)
    {
        if (prefab == null)
            return;

        GameObject temp = Instantiate(prefab);
        temp.hideFlags = HideFlags.HideAndDontSave;

        var connectLocalPositions = new List<Vector3>();
        var markerLayout = new MarkerLayoutData();
        Vector3? roadConnectLocalPosition = null;
        Vector3? autobahnConnectLocalPosition = null;
        var allTransforms = temp.GetComponentsInChildren<Transform>(true);
        foreach (var child in allTransforms)
        {
            if (child.name == "Connect")
            {
                Vector3 localPosition = temp.transform.InverseTransformPoint(child.position);
                connectLocalPositions.Add(localPosition);
                markerLayout.ConnectLocalPositions.Add(localPosition);
                if (type == RoadVisualType.AutobahnConnect)
                {
                    roadConnectLocalPosition = localPosition;
                    markerLayout.RoadConnectLocalPosition = localPosition;
                }
            }
            else if (type == RoadVisualType.AutobahnConnect && child.name == "ConnectAutoBahn")
            {
                Vector3 localPosition = temp.transform.InverseTransformPoint(child.position);
                connectLocalPositions.Add(localPosition);
                autobahnConnectLocalPosition = localPosition;
                markerLayout.AutobahnConnectLocalPosition = localPosition;
            }
        }

        if (connectLocalPositions.Count == 0)
        {
            Destroy(temp);
            return;
        }

        Vector3 min = new(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
        Vector3 max = new(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
        var renderers = temp.GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
        {
            Bounds bounds = renderer.bounds;
            Vector3 rendererMin = bounds.min;
            Vector3 rendererMax = bounds.max;

            for (int x = 0; x < 2; x++)
            {
                for (int y = 0; y < 2; y++)
                {
                    for (int z = 0; z < 2; z++)
                    {
                        Vector3 corner = new(
                            x == 0 ? rendererMin.x : rendererMax.x,
                            y == 0 ? rendererMin.y : rendererMax.y,
                            z == 0 ? rendererMin.z : rendererMax.z);
                        Vector3 localCorner = temp.transform.InverseTransformPoint(corner);
                        min = Vector3.Min(min, localCorner);
                        max = Vector3.Max(max, localCorner);
                    }
                }
            }
        }

        Vector3 center = (min + max) * 0.5f;
        markerLayout.Center = center;
        if (type == RoadVisualType.AutobahnConnect && roadConnectLocalPosition.HasValue && autobahnConnectLocalPosition.HasValue)
            _autobahnConnectorMarkerData = new ConnectorMarkerData(roadConnectLocalPosition.Value, autobahnConnectLocalPosition.Value, center);

        var variantMap = new Dictionary<TileConnectionMask, VariantData>();
        int[] rotationAngles = { 0, 90, 180, 270 };
        int[] flipValues = { 1, -1 };
        foreach (int angle in rotationAngles)
        {
            Quaternion rotation = Quaternion.Euler(0f, angle, 0f);
            foreach (int scaleX in flipValues)
            {
                foreach (int scaleZ in flipValues)
                {
                    Vector3 scale = new(scaleX, 1f, scaleZ);
                    TileConnectionMask mask = BuildVariantMask(connectLocalPositions, center, rotation, scale);
                    if (!variantMap.ContainsKey(mask))
                        variantMap.Add(mask, new VariantData(rotation, scale));
                }
            }
        }

        Destroy(temp);

        _variants[type] = variantMap;
        _visualData[type] = BuildCombinedVisualData(prefab);
        _markerLayouts[type] = markerLayout;
    }

    private CombinedRoadVisualData BuildCombinedVisualData(GameObject prefab)
    {
        GameObject temp = Instantiate(prefab);
        temp.hideFlags = HideFlags.HideAndDontSave;

        var materialOrder = new List<Material>();
        var combinesByMaterial = new Dictionary<Material, List<CombineInstance>>();
        var footprintBounds = new List<FootprintBoundsData>();
        var meshFilters = temp.GetComponentsInChildren<MeshFilter>(true);
        foreach (var meshFilter in meshFilters)
        {
            if (meshFilter.sharedMesh == null)
                continue;

            MeshRenderer meshRenderer = meshFilter.GetComponent<MeshRenderer>();
            if (TryGetFootprintKind(
                    meshFilter.transform,
                    prefab == autobahnPrefab || prefab == autobahnConnectPrefab,
                    out FootprintKind footprintKind))
            {
                Bounds localBounds = TransformBounds(
                    meshFilter.sharedMesh.bounds,
                    temp.transform.worldToLocalMatrix * meshFilter.transform.localToWorldMatrix);
                footprintBounds.Add(new FootprintBoundsData
                {
                    Bounds = localBounds,
                    Kind = footprintKind
                });
            }

            if (meshRenderer == null || !meshRenderer.enabled)
                continue;
            if (!meshFilter.sharedMesh.isReadable)
                continue;

            Material[] materials = meshRenderer.sharedMaterials;
            int subMeshCount = Mathf.Min(meshFilter.sharedMesh.subMeshCount, materials.Length);
            Matrix4x4 localMatrix = temp.transform.worldToLocalMatrix * meshFilter.transform.localToWorldMatrix;
            for (int subMeshIndex = 0; subMeshIndex < subMeshCount; subMeshIndex++)
            {
                Material material = materials[subMeshIndex];
                if (material == null)
                    continue;

                if (!combinesByMaterial.TryGetValue(material, out var combines))
                {
                    combines = new List<CombineInstance>();
                    combinesByMaterial.Add(material, combines);
                    materialOrder.Add(material);
                }

                combines.Add(new CombineInstance
                {
                    mesh = meshFilter.sharedMesh,
                    subMeshIndex = subMeshIndex,
                    transform = localMatrix
                });
            }
        }

        Destroy(temp);

        if (materialOrder.Count == 0)
            return new CombinedRoadVisualData();

        var finalSubmeshCombines = new CombineInstance[materialOrder.Count];
        for (int i = 0; i < materialOrder.Count; i++)
        {
            Mesh submeshMesh = new Mesh
            {
                name = $"{prefab.name}_{materialOrder[i].name}_Combined"
            };
            submeshMesh.CombineMeshes(combinesByMaterial[materialOrder[i]].ToArray(), true, true, false);
            finalSubmeshCombines[i] = new CombineInstance
            {
                mesh = submeshMesh,
                subMeshIndex = 0,
                transform = Matrix4x4.identity
            };
        }

        Mesh finalMesh = new Mesh
        {
            name = $"{prefab.name}_Combined"
        };
        finalMesh.CombineMeshes(finalSubmeshCombines, false, false, false);

        for (int i = 0; i < finalSubmeshCombines.Length; i++)
            Destroy(finalSubmeshCombines[i].mesh);

        return new CombinedRoadVisualData
        {
            Mesh = finalMesh,
            Materials = materialOrder.ToArray(),
            FootprintBounds = footprintBounds
        };
    }

    private static TileConnectionMask BuildVariantMask(
        List<Vector3> connectLocalPositions,
        Vector3 center,
        Quaternion rotation,
        Vector3 scale)
    {
        bool north = false;
        bool east = false;
        bool south = false;
        bool west = false;
        for (int i = 0; i < connectLocalPositions.Count; i++)
        {
            Vector3 offset = connectLocalPositions[i] - center;
            Vector3 scaledOffset = Vector3.Scale(offset, scale);
            Vector3 transformedOffset = rotation * scaledOffset;

            if (Mathf.Abs(transformedOffset.x) > Mathf.Abs(transformedOffset.z))
            {
                if (transformedOffset.x >= 0f)
                    east = true;
                else
                    west = true;
            }
            else
            {
                if (transformedOffset.z >= 0f)
                    north = true;
                else
                    south = true;
            }
        }

        return new TileConnectionMask(north, east, south, west);
    }

    private bool TryGetVariant(RoadVisualType type, TileConnectionMask mask, out VariantData variant)
    {
        variant = default;
        if (!_variants.TryGetValue(type, out var variantsByMask))
            return false;

        if (variantsByMask.TryGetValue(mask, out variant))
            return true;

        if (type == RoadVisualType.Autobahn || type == RoadVisualType.AutobahnConnect)
        {
            TileConnectionMask normalizedMask = NormalizeAutobahnMask(mask);
            if (variantsByMask.TryGetValue(normalizedMask, out variant))
                return true;
        }

        return false;
    }

    private static TileConnectionMask NormalizeAutobahnMask(TileConnectionMask mask)
    {
        if (mask.North || mask.South)
            return new TileConnectionMask(true, false, true, false);

        if (mask.East || mask.West)
            return new TileConnectionMask(false, true, false, true);

        return mask;
    }

    private void ApplyPlacement(Transform target, Vector2Int cell, VariantData variant)
    {
        target.SetPositionAndRotation(
            GetPlacementPosition(cell, variant),
            variant.Rotation);
        target.localScale = variant.Scale;
    }

    private void ApplyPivotPlacement(Transform target, Vector2Int cell, VariantData variant)
    {
        Vector3 basePosition = gridOrigin + new Vector3(cell.x * roadGridSize, buildPlaneY, cell.y * roadGridSize);
        target.SetPositionAndRotation(basePosition, variant.Rotation);
        target.localScale = variant.Scale;
    }

    private void ClearPreview()
    {
        for (int i = 0; i < _previewObjects.Count; i++)
        {
            if (_previewObjects[i] != null)
                ReleasePreviewObject(_previewObjects[i]);
        }

        _previewObjects.Clear();
    }

    private void UpdatePreview()
    {
        if (!_isDrawing || !_pendingStartCell.HasValue)
        {
            ClearPreview();
            return;
        }

        RebuildPreview(_pendingStartCell.Value, _currentDragCell, _dragFirstAxis);
    }

    private void RebuildPreview(Vector2Int startCell, Vector2Int endCell, DragFirstAxis dragFirstAxis)
    {
        ClearPreview();

        List<Vector2Int> path = BuildPath(startCell, endCell, dragFirstAxis);
        if (path.Count == 0)
            return;

        if (path.Count == 1)
        {
            TileConnectionMask defaultMask = new(false, true, false, false);
            if (TryGetVariant(RoadVisualType.End, defaultMask, out var defaultVariant) && endPrefab != null)
            {
                GameObject preview = GetPreviewObject(RoadVisualType.End);
                if (preview == null)
                    return;

                preview.name = $"End_Preview_{startCell.x}_{startCell.y}";
                ApplyPlacement(preview.transform, startCell, defaultVariant);
                _previewObjects.Add(preview);
            }

            return;
        }

        var proposedEdges = new HashSet<EdgeKey>();
        var dirtyCells = new HashSet<Vector2Int>();
        for (int i = 0; i < path.Count; i++)
        {
            dirtyCells.Add(path[i]);
            AddNeighborCells(path[i], dirtyCells);

            if (i > 0)
                proposedEdges.Add(new EdgeKey(path[i - 1], path[i]));
        }

        AddEndpointPreviewConnections(path, proposedEdges, dirtyCells);

        foreach (var cell in dirtyCells)
        {
            TileConnectionMask mask = GetPreviewMask(cell, proposedEdges);
            RoadVisualType type = ResolveVisualType(cell, mask);
            if (type == RoadVisualType.None || !TryGetVariant(type, mask, out var variant))
                continue;

            GameObject preview = GetPreviewObject(type);
            if (preview == null)
                continue;

            preview.name = $"{type}_Preview_{cell.x}_{cell.y}";
            ApplyPlacement(preview.transform, cell, variant);
            _previewObjects.Add(preview);
        }
    }

    private Vector3 GetPlacementPosition(Vector2Int cell, VariantData variant)
    {
        Vector3 basePosition = gridOrigin + new Vector3(cell.x * roadGridSize, buildPlaneY, cell.y * roadGridSize);
        Vector3[] corners =
        {
            new(0f, 0f, 0f),
            new(roadGridSize, 0f, 0f),
            new(0f, 0f, roadGridSize),
            new(roadGridSize, 0f, roadGridSize)
        };

        float minX = float.PositiveInfinity;
        float minZ = float.PositiveInfinity;
        for (int i = 0; i < corners.Length; i++)
        {
            Vector3 transformed = variant.Rotation * Vector3.Scale(corners[i], variant.Scale);
            if (transformed.x < minX)
                minX = transformed.x;
            if (transformed.z < minZ)
                minZ = transformed.z;
        }

        return basePosition - new Vector3(minX, 0f, minZ);
    }

    private void CancelPendingBuild()
    {
        _pendingStartCell = null;
        _isDrawing = false;
        _dragFirstAxis = DragFirstAxis.None;
        ClearPreview();
    }

    private void BeginRoadBuildSession()
    {
        _roadBuildSessionSnapshot = CaptureRoadBuildSessionSnapshot();
    }

    private RoadBuildSessionSnapshot CaptureRoadBuildSessionSnapshot()
    {
        var snapshot = new RoadBuildSessionSnapshot
        {
            NextStrokeId = _nextStrokeId
        };

        foreach (var entry in _edgeCounts)
            snapshot.EdgeCounts.Add(entry.Key, entry.Value);

        foreach (var entry in _strokeIdsByCell)
            snapshot.StrokeIdsByCell.Add(entry.Key, new List<int>(entry.Value));

        foreach (var entry in _strokes)
        {
            snapshot.Strokes.Add(entry.Key, new StrokeData
            {
                Id = entry.Value.Id,
                Cells = new List<Vector2Int>(entry.Value.Cells),
                IsAutobahn = entry.Value.IsAutobahn,
                UseAutobahnConnectorAtStart = entry.Value.UseAutobahnConnectorAtStart,
                UseAutobahnConnectorAtEnd = entry.Value.UseAutobahnConnectorAtEnd
            });
        }

        foreach (var entry in _roadTiles)
        {
            snapshot.RoadTiles.Add(entry.Key, new RoadTileData
            {
                Type = entry.Value.Type,
                Mask = entry.Value.Mask,
                Rotation = entry.Value.Rotation,
                Scale = entry.Value.Scale
            });
        }

        return snapshot;
    }

    private void RestoreRoadBuildSession(RoadBuildSessionSnapshot snapshot)
    {
        _nextStrokeId = snapshot.NextStrokeId;

        _edgeCounts.Clear();
        foreach (var entry in snapshot.EdgeCounts)
            _edgeCounts.Add(entry.Key, entry.Value);

        _strokeIdsByCell.Clear();
        foreach (var entry in snapshot.StrokeIdsByCell)
            _strokeIdsByCell.Add(entry.Key, new List<int>(entry.Value));

        _strokes.Clear();
        foreach (var entry in snapshot.Strokes)
        {
            _strokes.Add(entry.Key, new StrokeData
            {
                Id = entry.Value.Id,
                Cells = new List<Vector2Int>(entry.Value.Cells),
                IsAutobahn = entry.Value.IsAutobahn,
                UseAutobahnConnectorAtStart = entry.Value.UseAutobahnConnectorAtStart,
                UseAutobahnConnectorAtEnd = entry.Value.UseAutobahnConnectorAtEnd
            });
        }

        RebuildSpecialRoadCellMetadata();
        _roadTiles.Clear();
        foreach (var entry in snapshot.RoadTiles)
        {
            _roadTiles.Add(entry.Key, new RoadTileData
            {
                Type = entry.Value.Type,
                Mask = entry.Value.Mask,
                Rotation = entry.Value.Rotation,
                Scale = entry.Value.Scale
            });
        }

        RebuildRoadStateFromCurrentTiles();
    }

    private void RebuildRoadStateFromCurrentTiles()
    {
        RebuildSpecialRoadCellMetadata();

        foreach (var chunk in _chunks.Values)
        {
            if (chunk.Mesh != null)
                Destroy(chunk.Mesh);
            if (chunk.GameObject != null)
                Destroy(chunk.GameObject);
        }

        _chunks.Clear();
        _chunkCells.Clear();
        _dirtyChunks.Clear();
        ClearSpecialRoadObjects();

        foreach (var cell in _roadTiles.Keys)
            AddCellToChunk(cell);

        SyncRoadCellsToEcs();
        RebuildDirtyChunks();
        RebuildAllSpecialRoadObjects();
    }

    private void RebuildSpecialRoadCellMetadata()
    {
        _autobahnCells.Clear();
        _autobahnConnectorCells.Clear();

        foreach (var stroke in _strokes.Values)
        {
            if (!stroke.IsAutobahn || stroke.Cells.Count == 0)
                continue;

            int startIndex = 0;
            int endIndex = stroke.Cells.Count - 1;

            if (stroke.UseAutobahnConnectorAtStart)
            {
                _autobahnConnectorCells.Add(stroke.Cells[startIndex]);
                startIndex++;
            }

            if (stroke.UseAutobahnConnectorAtEnd && endIndex >= 0)
            {
                _autobahnConnectorCells.Add(stroke.Cells[endIndex]);
                endIndex--;
            }
            for (int i = startIndex; i <= endIndex; i++)
                _autobahnCells.Add(stroke.Cells[i]);
        }

        _autobahnCells.ExceptWith(_autobahnConnectorCells);
    }

    private void ClearDeletePrompt()
    {
        _pendingDeleteStrokeId = null;
        _pendingDeleteMessage = null;
        _skipBuildClickFrames = 2;
    }

    private void BuildDefinitions()
    {
        _soldierBaseDefinition = new BuildingDefinition
        {
            DisplayName = "Soldier Base",
            Prefab = soldierBasePrefab,
            FootprintCells = new Vector2Int(
                Mathf.Max(1, soldierBaseFootprintCells.x),
                Mathf.Max(1, soldierBaseFootprintCells.y))
        };

        CacheBuildingBounds(_soldierBaseDefinition);
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

        _activeBuildingPlacement = new BuildingPlacementState
        {
            Definition = definition,
            PreviewInstance = Instantiate(definition.Prefab, _buildingRoot),
            OriginCell = GetCenterScreenPlacementOrigin(definition.FootprintCells)
        };

        UpdateBuildingPlacementVisual(_activeBuildingPlacement, updateCellFromPointer: false);
    }

    private void CancelBuildingPlacementInternal()
    {
        if (_activeBuildingPlacement?.PreviewInstance != null)
            Destroy(_activeBuildingPlacement.PreviewInstance);

        _activeBuildingPlacement = null;
        _isDraggingBuildingPlacement = false;
        HidePlacementOutline();
    }

    private void UpdateBuildingPlacement(Vector2 screenPosition)
    {
        if (_activeBuildingPlacement == null)
            return;

        UpdateBuildingPlacementVisual(_activeBuildingPlacement, updateCellFromPointer: _isDraggingBuildingPlacement, screenPosition);
    }

    private void UpdateBuildingPlacementVisual(BuildingPlacementState placement, bool updateCellFromPointer, Vector2 screenPosition = default)
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

    private RuntimeBuildingData PlaceBuilding(BuildingPlacementState placement)
    {
        GameObject previewInstance = placement.PreviewInstance;
        previewInstance.name = $"{placement.Definition.DisplayName}_{_nextBuildingId}";

        _runtimeGridBlockers?.RemoveBlockersOverlappingFootprint(placement.OriginCell, placement.Definition.FootprintCells);
        Entity blockerEntity = CreateBlockerEntity(placement.OriginCell, placement.Definition.FootprintCells);
        Entity combatEntity = CreateBuildingCombatEntity(placement.OriginCell, placement.Definition);

        var building = new RuntimeBuildingData
        {
            Id = _nextBuildingId++,
            Definition = placement.Definition,
            Instance = previewInstance,
            OriginCell = placement.OriginCell,
            CombatEntity = combatEntity,
            BlockerEntity = blockerEntity
        };

        AttachRuntimeLink(building);
        _runtimeBuildings.Add(building.Id, building);
        placement.PreviewInstance = null;
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
        if (IsPointerOverUI(screenPosition))
            return;

        if (!TryGetGridData(out _, out GridConfig grid, out _, out _))
            return;

        if (!TryGetGridCell(screenPosition, grid, out Vector2Int cell))
        {
            ClearSelectedBuilding();
            return;
        }

        foreach (var entry in _runtimeBuildings)
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
        if (_runtimeBuildings.ContainsKey(buildingId))
            _selectedBuildingId = buildingId;
    }

    private void DeleteBuilding(int buildingId, bool destroyVisual)
    {
        if (!_runtimeBuildings.TryGetValue(buildingId, out RuntimeBuildingData building))
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

        _runtimeBuildings.Remove(buildingId);
    }

    public void HandleRuntimeBuildingEntityDestroyed(int buildingId, Entity blockerEntity, GameObject buildingObject)
    {
        if (_buildingPlacementController != null)
        {
            _buildingPlacementController.HandleRuntimeBuildingEntityDestroyed(buildingId, blockerEntity, buildingObject);
            return;
        }

        if (_selectedBuildingId == buildingId)
            _selectedBuildingId = null;

        if (blockerEntity != Entity.Null && TryGetEntityManager(out EntityManager em) && em.Exists(blockerEntity))
            em.DestroyEntity(blockerEntity);

        _runtimeBuildings.Remove(buildingId);
        if (buildingObject != null)
            Destroy(buildingObject);
    }

    private Entity CreateBlockerEntity(Vector2Int originCell, Vector2Int footprintCells)
    {
        if (!TryGetEntityManager(out EntityManager em))
            return Entity.Null;

        Entity entity = em.CreateEntity();
        em.AddComponentData(entity, new UnitGrid { Cell = new int2(originCell.x, originCell.y) });
        em.AddComponentData(entity, new GridBlockerSize
        {
            Size = new int2(Mathf.Max(1, footprintCells.x), Mathf.Max(1, footprintCells.y))
        });
        em.AddComponent<StaticGridBlocker>(entity);
        return entity;
    }

    private Entity CreateBuildingCombatEntity(Vector2Int originCell, BuildingDefinition definition)
    {
        if (!TryGetEntityManager(out EntityManager em))
            return Entity.Null;
        if (!TryGetGridData(out _, out GridConfig grid, out _, out _))
            return Entity.Null;

        Entity entity = em.CreateEntity();
        float3 center = GetFootprintCenter(originCell, definition.FootprintCells, grid);

        em.AddComponentData(entity, new LocalTransform
        {
            Position = center,
            Rotation = quaternion.identity,
            Scale = 1f
        });
        em.AddComponentData(entity, new LocalToWorld());
        em.AddComponentData(entity, new UnitGrid
        {
            Cell = new int2(originCell.x, originCell.y)
        });
        em.AddComponentData(entity, new UnitGridInitialized());
        em.AddComponentData(entity, new Faction { Id = 0 });
        em.AddComponentData(entity, new UnitHealth { Current = 500, Max = 500 });
        em.AddComponentData(entity, new UnitRespawnPrefab { Prefab = Entity.Null });
        em.AddComponentData(entity, new UnitPrevWorldPos { Value = center });
        em.AddComponentData(entity, new UnitMoveVisualState { IsMoving = 0, StillSeconds = 0f });
        return entity;
    }

    private void AttachRuntimeLink(RuntimeBuildingData building)
    {
        if (building.Instance == null)
            return;

        RuntimeBuildingEntityLink link = building.Instance.GetComponent<RuntimeBuildingEntityLink>();
        if (link == null)
            link = building.Instance.AddComponent<RuntimeBuildingEntityLink>();

        link.Configure(this, building.Id, building.CombatEntity, building.BlockerEntity);
    }

    private bool TrySpawnPlayerUnitNearBuilding(RuntimeBuildingData building)
    {
        if (!TryGetEntityManager(out EntityManager em))
            return false;
        if (!TryGetGridData(out Entity gridEntity, out GridConfig grid, out _, out DynamicBlockerData blockerData))
            return false;
        if (!TryGetPlayerUnitPrefabEntity(em, out Entity prefabEntity))
            return false;

        var walkable = em.GetBuffer<GridWalkable>(gridEntity).AsNativeArray();
        var occupied = em.GetComponentData<DynamicOccupancyData>(gridEntity).Occupied;
        var reserved = new NativeBitArray(grid.Width * grid.Height, Allocator.Temp);
        try
        {
            _buildingSpawnRandomState = math.max(1u, _buildingSpawnRandomState + 1u);
            var rng = new Unity.Mathematics.Random(_buildingSpawnRandomState);
            Vector2Int size = building.Definition.FootprintCells;
            int2 center = new(building.OriginCell.x + size.x / 2, building.OriginCell.y + size.y / 2);
            int radius = Mathf.Max(size.x, size.y) + 4;
            int2 cell = SpawnCellUtility.FindSpawnCellNear(ref rng, grid, walkable, blockerData.Blocked, occupied, ref reserved, center, radius);

            Entity instance = em.Instantiate(prefabEntity);
            float3 pos = GridUtils.CellToWorldCenter(grid, cell);
            em.SetComponentData(instance, new UnitGrid { Cell = cell });
            em.SetComponentData(instance, LocalTransform.FromPosition(pos));
            if (em.HasComponent<UnitPrevWorldPos>(instance))
                em.SetComponentData(instance, new UnitPrevWorldPos { Value = pos });
            if (em.HasComponent<UnitMoveVisualState>(instance))
                em.SetComponentData(instance, new UnitMoveVisualState { IsMoving = 0, StillSeconds = 0f });
            if (em.HasComponent<Faction>(instance))
                em.SetComponentData(instance, new Faction { Id = 0 });
            if (em.HasComponent<UnitRespawnPrefab>(instance))
                em.SetComponentData(instance, new UnitRespawnPrefab { Prefab = prefabEntity });
            if (em.HasComponent<UnitIdleWanderState>(instance))
            {
                _buildingSpawnRandomState = math.max(1u, _buildingSpawnRandomState + 1u);
                em.SetComponentData(instance, new UnitIdleWanderState
                {
                    RandomState = _buildingSpawnRandomState,
                    RetrySeconds = 0f,
                    CurrentIdleDelaySeconds = 0f
                });
            }
            if (em.HasComponent<UnitPathFollow>(instance))
                em.RemoveComponent<UnitPathFollow>(instance);
            if (em.HasComponent<UnitPathRange>(instance))
                em.RemoveComponent<UnitPathRange>(instance);
            if (em.HasComponent<EngageTarget>(instance))
                em.RemoveComponent<EngageTarget>(instance);
            if (em.HasComponent<UnitPathRequest>(instance))
                em.RemoveComponent<UnitPathRequest>(instance);
            if (em.HasComponent<UnitTarget>(instance))
                em.RemoveComponent<UnitTarget>(instance);
            if (em.HasComponent<AutoWanderMoveTag>(instance))
                em.RemoveComponent<AutoWanderMoveTag>(instance);

            return true;
        }
        finally
        {
            reserved.Dispose();
        }
    }

    private static bool TryGetPlayerUnitPrefabEntity(EntityManager em, out Entity prefabEntity)
    {
        prefabEntity = Entity.Null;
        using var query = em.CreateEntityQuery(
            ComponentType.ReadOnly<Faction>(),
            ComponentType.ReadOnly<UnitRespawnPrefab>());
        using var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            if (em.HasComponent<StaticGridBlocker>(entity))
                continue;
            if (em.GetComponentData<Faction>(entity).Id != 0)
                continue;

            Entity candidate = em.GetComponentData<UnitRespawnPrefab>(entity).Prefab;
            if (candidate == Entity.Null)
                continue;

            prefabEntity = candidate;
            return true;
        }

        return false;
    }

    private bool TryGetGridData(out Entity gridEntity, out GridConfig grid, out DynamicBuffer<GridRoad> roads, out DynamicBlockerData blockerData)
    {
        gridEntity = Entity.Null;
        grid = default;
        roads = default;
        blockerData = default;

        if (!TryGetEntityManager(out EntityManager em))
            return false;

        EnsureEntityQueries(em);
        if (_gridDataQuery.IsEmptyIgnoreFilter)
            return false;

        gridEntity = _gridDataQuery.GetSingletonEntity();
        grid = em.GetComponentData<GridConfig>(gridEntity);
        roads = em.GetBuffer<GridRoad>(gridEntity);
        blockerData = em.GetComponentData<DynamicBlockerData>(gridEntity);
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

    private static bool IsPointerOverUI(Vector2 screenPosition)
    {
        return false;
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

    private static void AddNeighborCells(Vector2Int cell, HashSet<Vector2Int> cells)
    {
        cells.Add(cell + North);
        cells.Add(cell + East);
        cells.Add(cell + South);
        cells.Add(cell + West);
    }

    private void AddEndpointConnections(List<Vector2Int> path, HashSet<Vector2Int> dirtyCells)
    {
        AddEndpointConnectionsForCell(path, 0, dirtyCells);
        AddEndpointConnectionsForCell(path, path.Count - 1, dirtyCells);
    }

    private void AddEndpointConnectionsForCell(List<Vector2Int> path, int index, HashSet<Vector2Int> dirtyCells)
    {
        if (path.Count == 0)
            return;

        Vector2Int endpoint = path[index];
        Vector2Int inwardNeighbor = index == 0 ? path[1] : path[path.Count - 2];

        foreach (Vector2Int neighbor in GetAdjacentRoadCells(endpoint))
        {
            if (neighbor == inwardNeighbor)
                continue;

            AddEdge(endpoint, neighbor);
            dirtyCells.Add(neighbor);
            AddNeighborCells(neighbor, dirtyCells);
        }
    }

    private void AddEndpointPreviewConnections(List<Vector2Int> path, HashSet<EdgeKey> proposedEdges, HashSet<Vector2Int> dirtyCells)
    {
        AddEndpointPreviewConnectionsForCell(path, 0, proposedEdges, dirtyCells);
        AddEndpointPreviewConnectionsForCell(path, path.Count - 1, proposedEdges, dirtyCells);
    }

    private void AddEndpointPreviewConnectionsForCell(
        List<Vector2Int> path,
        int index,
        HashSet<EdgeKey> proposedEdges,
        HashSet<Vector2Int> dirtyCells)
    {
        if (path.Count < 2)
            return;

        Vector2Int endpoint = path[index];
        Vector2Int inwardNeighbor = index == 0 ? path[1] : path[path.Count - 2];

        foreach (Vector2Int neighbor in GetAdjacentRoadCells(endpoint))
        {
            if (neighbor == inwardNeighbor)
                continue;

            proposedEdges.Add(new EdgeKey(endpoint, neighbor));
            dirtyCells.Add(neighbor);
            AddNeighborCells(neighbor, dirtyCells);
        }
    }

    private IEnumerable<Vector2Int> GetAdjacentRoadCells(Vector2Int cell)
    {
        Vector2Int[] neighbors = { cell + North, cell + East, cell + South, cell + West };
        for (int i = 0; i < neighbors.Length; i++)
        {
            if (_strokeIdsByCell.ContainsKey(neighbors[i]))
                yield return neighbors[i];
        }
    }

    private TileConnectionMask GetPreviewMask(Vector2Int cell, HashSet<EdgeKey> proposedEdges)
    {
        return new TileConnectionMask(
            HasPreviewEdge(cell, cell + North, proposedEdges),
            HasPreviewEdge(cell, cell + East, proposedEdges),
            HasPreviewEdge(cell, cell + South, proposedEdges),
            HasPreviewEdge(cell, cell + West, proposedEdges));
    }

    private bool HasPreviewEdge(Vector2Int a, Vector2Int b, HashSet<EdgeKey> proposedEdges)
    {
        var key = new EdgeKey(a, b);
        return _edgeCounts.ContainsKey(key) || proposedEdges.Contains(key);
    }

    private static List<Vector2Int> BuildPath(Vector2Int startCell, Vector2Int endCell, DragFirstAxis dragFirstAxis)
    {
        var cells = new List<Vector2Int>();
        cells.Add(startCell);
        if (startCell == endCell)
            return cells;

        if (startCell.x == endCell.x || startCell.y == endCell.y)
        {
            AppendStraightSegment(cells, startCell, endCell);
            return cells;
        }

        Vector2Int corner = dragFirstAxis == DragFirstAxis.Vertical
            ? new Vector2Int(startCell.x, endCell.y)
            : new Vector2Int(endCell.x, startCell.y);

        AppendStraightSegment(cells, startCell, corner);
        AppendStraightSegment(cells, corner, endCell);
        return cells;
    }

    private static void AppendStraightSegment(List<Vector2Int> cells, Vector2Int from, Vector2Int to)
    {
        Vector2Int direction = new(Math.Sign(to.x - from.x), Math.Sign(to.y - from.y));
        Vector2Int current = cells[cells.Count - 1];
        while (current != to)
        {
            current += direction;
            if (cells.Count == 0 || cells[cells.Count - 1] != current)
                cells.Add(current);
        }
    }

    private static void SetPreviewMaterials(Renderer renderer, float alpha)
    {
        var materials = renderer.sharedMaterials;
        var previewMaterials = new Material[materials.Length];
        for (int i = 0; i < materials.Length; i++)
        {
            if (materials[i] == null)
            {
                previewMaterials[i] = null;
                continue;
            }

            previewMaterials[i] = new Material(materials[i]);
            if (previewMaterials[i].HasProperty("_Color"))
            {
                Color color = previewMaterials[i].color;
                color.a = alpha;
                previewMaterials[i].color = color;
            }
        }

        renderer.sharedMaterials = previewMaterials;
    }

    private GameObject CreateRuntimeRoadObject(RoadVisualType type, bool preview)
    {
        if (!_visualData.TryGetValue(type, out var visualData) || visualData.Mesh == null || visualData.Materials == null)
            return null;

        GameObject roadObject = new(preview ? $"{type}_Preview" : type.ToString());
        roadObject.transform.SetParent(_roadRoot, false);

        var meshFilter = roadObject.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = visualData.Mesh;

        var meshRenderer = roadObject.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterials = visualData.Materials;

        if (preview)
            SetPreviewMaterials(meshRenderer, previewAlpha);

        return roadObject;
    }

    private void MarkChunkDirty(Vector2Int chunkCoord)
    {
        _dirtyChunks.Add(chunkCoord);
    }

    private void RebuildDirtyChunks()
    {
        if (_dirtyChunks.Count == 0 || _roadRoot == null)
            return;

        foreach (var chunkCoord in _dirtyChunks)
            RebuildChunk(chunkCoord);

        _dirtyChunks.Clear();
    }

    private void RebuildChunk(Vector2Int chunkCoord)
    {
        var materialOrder = new List<Material>();
        var combinesByMaterial = new Dictionary<Material, List<CombineInstance>>();

        if (!_chunkCells.TryGetValue(chunkCoord, out var chunkCellSet) || chunkCellSet.Count == 0)
        {
            if (_chunks.TryGetValue(chunkCoord, out var emptyChunk))
            {
                if (emptyChunk.Mesh != null)
                    Destroy(emptyChunk.Mesh);
                if (emptyChunk.GameObject != null)
                    Destroy(emptyChunk.GameObject);
                _chunks.Remove(chunkCoord);
            }

            return;
        }

        foreach (var cell in chunkCellSet)
        {
            if (!_roadTiles.TryGetValue(cell, out var tile))
                continue;

            if (IsSpecialRoadCell(cell))
                continue;

            if (!_visualData.TryGetValue(tile.Type, out var visualData) || visualData.Mesh == null || visualData.Materials == null)
                continue;

            Matrix4x4 matrix = Matrix4x4.TRS(
                GetPlacementPosition(cell, new VariantData(tile.Rotation, tile.Scale)),
                tile.Rotation,
                tile.Scale);

            for (int subMeshIndex = 0; subMeshIndex < visualData.Materials.Length; subMeshIndex++)
            {
                Material material = visualData.Materials[subMeshIndex];
                if (material == null)
                    continue;

                if (!combinesByMaterial.TryGetValue(material, out var combines))
                {
                    combines = new List<CombineInstance>();
                    combinesByMaterial.Add(material, combines);
                    materialOrder.Add(material);
                }

                combines.Add(new CombineInstance
                {
                    mesh = visualData.Mesh,
                    subMeshIndex = subMeshIndex,
                    transform = matrix
                });
            }
        }

        if (materialOrder.Count == 0)
        {
            if (_chunks.TryGetValue(chunkCoord, out var emptyRenderableChunk))
            {
                if (emptyRenderableChunk.Mesh != null)
                    Destroy(emptyRenderableChunk.Mesh);
                if (emptyRenderableChunk.GameObject != null)
                    Destroy(emptyRenderableChunk.GameObject);
                _chunks.Remove(chunkCoord);
            }

            return;
        }

        Mesh combinedMesh = BuildChunkMesh(chunkCoord, materialOrder, combinesByMaterial);
        if (combinedMesh == null)
            return;

        if (!_chunks.TryGetValue(chunkCoord, out var chunk))
        {
            chunk = new ChunkRenderData
            {
                GameObject = new GameObject($"RoadChunk_{chunkCoord.x}_{chunkCoord.y}")
            };
            chunk.GameObject.transform.SetParent(_roadRoot, false);
            chunk.GameObject.AddComponent<MeshFilter>();
            chunk.GameObject.AddComponent<MeshRenderer>();
            _chunks.Add(chunkCoord, chunk);
        }

        if (chunk.Mesh != null)
            Destroy(chunk.Mesh);

        chunk.Mesh = combinedMesh;
        var filter = chunk.GameObject.GetComponent<MeshFilter>();
        filter.sharedMesh = chunk.Mesh;

        var renderer = chunk.GameObject.GetComponent<MeshRenderer>();
        renderer.sharedMaterials = materialOrder.ToArray();
    }

    private bool IsSpecialRoadCell(Vector2Int cell) => _autobahnCells.Contains(cell) || _autobahnConnectorCells.Contains(cell);

    private void RebuildSpecialRoadObjects(HashSet<Vector2Int> dirtyCells)
    {
        RebuildAllSpecialRoadObjects();
    }

    private void RebuildAllSpecialRoadObjects()
    {
        var expectedCells = new HashSet<Vector2Int>();
        foreach (var stroke in _strokes.Values)
        {
            if (!stroke.IsAutobahn || stroke.Cells.Count < 2)
                continue;

            RebuildSpecialRoadStrokeObjects(stroke, expectedCells);
        }

        var cellsToRemove = new List<Vector2Int>();
        foreach (var cell in _specialRoadObjects.Keys)
        {
            if (!expectedCells.Contains(cell))
                cellsToRemove.Add(cell);
        }

        for (int i = 0; i < cellsToRemove.Count; i++)
            DestroySpecialRoadObject(cellsToRemove[i]);
    }

    private void RebuildSpecialRoadStrokeObjects(StrokeData stroke, HashSet<Vector2Int> expectedCells)
    {
        if (stroke.Cells.Count < 2)
            return;

        int firstAutobahnCellIndex = stroke.UseAutobahnConnectorAtStart ? 1 : 0;
        int lastAutobahnCellIndex = stroke.Cells.Count - (stroke.UseAutobahnConnectorAtEnd ? 2 : 1);
        if (firstAutobahnCellIndex > lastAutobahnCellIndex)
            return;

        Vector2Int autobahnDirection = stroke.Cells[Mathf.Min(firstAutobahnCellIndex, stroke.Cells.Count - 1)] - stroke.Cells[Mathf.Max(firstAutobahnCellIndex - 1, 0)];
        if (autobahnDirection == Vector2Int.zero)
            autobahnDirection = stroke.Cells[Mathf.Min(firstAutobahnCellIndex + 1, stroke.Cells.Count - 1)] - stroke.Cells[firstAutobahnCellIndex];

        Vector2Int cityDirection = -autobahnDirection;
        Vector3 previousConnectWorldPosition = default;
        bool hasPreviousConnectWorldPosition = false;

        if (stroke.UseAutobahnConnectorAtStart)
        {
            Vector2Int connectorCell = stroke.Cells[0];
            if (TryGetAutobahnConnectorVariant(autobahnDirection, out var connectorVariant) &&
                _markerLayouts.TryGetValue(RoadVisualType.AutobahnConnect, out var connectorLayout) &&
                connectorLayout.RoadConnectLocalPosition.HasValue &&
                connectorLayout.AutobahnConnectLocalPosition.HasValue &&
                TryGetNeighborRoadConnectWorldPosition(connectorCell, cityDirection, out Vector3 cityConnectWorldPosition))
            {
                if (stroke.Cells.Count > firstAutobahnCellIndex &&
                    _markerLayouts.TryGetValue(RoadVisualType.Autobahn, out var startAutobahnLayout) &&
                    startAutobahnLayout.ConnectLocalPositions.Count >= 2 &&
                    TryGetVariant(RoadVisualType.Autobahn, BuildAxisMask(autobahnDirection), out var startAutobahnVariant))
                {
                    Vector3 autobahnTargetWorldPosition = cityConnectWorldPosition +
                        new Vector3(autobahnDirection.x, 0f, autobahnDirection.y) * roadGridSize;

                    if (TryGetAutobahnConnectorVariantForTargets(
                        cityConnectWorldPosition,
                        autobahnTargetWorldPosition,
                        autobahnDirection,
                        out var bestConnectorVariant))
                    {
                        connectorVariant = bestConnectorVariant;
                    }
                }

                GameObject connectorObject = GetOrCreateSpecialRoadObject(connectorCell, RoadVisualType.AutobahnConnect);
                PlaceObjectByMarker(
                    connectorObject.transform,
                    connectorVariant,
                    connectorLayout.RoadConnectLocalPosition.Value,
                    cityConnectWorldPosition);
                previousConnectWorldPosition = GetObjectMarkerWorldPosition(
                    connectorObject.transform,
                    connectorLayout.AutobahnConnectLocalPosition.Value);
                hasPreviousConnectWorldPosition = true;
                expectedCells.Add(connectorCell);
            }
            else
            {
                DestroySpecialRoadObject(connectorCell);
            }
        }

        if (!_markerLayouts.TryGetValue(RoadVisualType.Autobahn, out var autobahnLayout) || autobahnLayout.ConnectLocalPositions.Count < 2)
            return;

        int availableAutobahnCellCount = lastAutobahnCellIndex - firstAutobahnCellIndex + 1;
        int autobahnSpanInCells = GetAutobahnSpanInCells(autobahnLayout);
        int autobahnObjectCount = Mathf.Max(1, Mathf.FloorToInt(availableAutobahnCellCount / (float)autobahnSpanInCells));

        for (int pieceIndex = 0; pieceIndex < autobahnObjectCount; pieceIndex++)
        {
            int sampleOffset = Mathf.FloorToInt((pieceIndex * availableAutobahnCellCount) / (float)autobahnObjectCount);
            int cellIndex = Mathf.Clamp(firstAutobahnCellIndex + sampleOffset, firstAutobahnCellIndex, lastAutobahnCellIndex);
            Vector2Int cell = stroke.Cells[cellIndex];
            Vector2Int forwardDirection = autobahnDirection;

            if (!TryGetVariant(RoadVisualType.Autobahn, BuildAxisMask(forwardDirection), out var autobahnVariant))
                continue;

            Vector3 incomingMarkerLocalPosition = GetMarkerLocalPositionForDirection(
                autobahnLayout,
                autobahnVariant,
                -forwardDirection);
            Vector3 outgoingMarkerLocalPosition = GetMarkerLocalPositionForDirection(
                autobahnLayout,
                autobahnVariant,
                forwardDirection);

            if (!hasPreviousConnectWorldPosition)
            {
                if (!TryGetNeighborRoadConnectWorldPosition(cell, -forwardDirection, out previousConnectWorldPosition))
                    continue;
                hasPreviousConnectWorldPosition = true;
            }

            GameObject autobahnObject = GetOrCreateSpecialRoadObject(cell, RoadVisualType.Autobahn);
            PlaceObjectByMarker(
                autobahnObject.transform,
                autobahnVariant,
                incomingMarkerLocalPosition,
                previousConnectWorldPosition);
            Vector3 autobahnOutgoingWorldPosition = GetObjectMarkerWorldPosition(
                autobahnObject.transform,
                outgoingMarkerLocalPosition);
            previousConnectWorldPosition = autobahnOutgoingWorldPosition;
            expectedCells.Add(cell);
        }

        if (stroke.UseAutobahnConnectorAtEnd)
        {
            Vector2Int connectorCell = stroke.Cells[stroke.Cells.Count - 1];
            Vector2Int connectorAutobahnDirection = stroke.Cells[stroke.Cells.Count - 2] - connectorCell;
            Vector2Int connectorRoadDirection = -connectorAutobahnDirection;

            if (_markerLayouts.TryGetValue(RoadVisualType.AutobahnConnect, out var connectorLayout) &&
                connectorLayout.RoadConnectLocalPosition.HasValue &&
                connectorLayout.AutobahnConnectLocalPosition.HasValue)
            {
                bool hasCityTarget = TryGetNeighborRoadConnectWorldPosition(
                    connectorCell,
                    connectorRoadDirection,
                    out Vector3 cityConnectWorldPosition);

                VariantData connectorVariant;
                if (hasCityTarget &&
                    hasPreviousConnectWorldPosition &&
                    TryGetAutobahnConnectorVariantForTargets(
                        cityConnectWorldPosition,
                        previousConnectWorldPosition,
                        connectorAutobahnDirection,
                        out var bestConnectorVariant))
                {
                    connectorVariant = bestConnectorVariant;
                }
                else if (!TryGetAutobahnConnectorVariant(connectorAutobahnDirection, out connectorVariant))
                {
                    DestroySpecialRoadObject(connectorCell);
                    return;
                }

                GameObject connectorObject = GetOrCreateSpecialRoadObject(connectorCell, RoadVisualType.AutobahnConnect);
                if (hasCityTarget)
                {
                    PlaceObjectByMarker(
                        connectorObject.transform,
                        connectorVariant,
                        connectorLayout.RoadConnectLocalPosition.Value,
                        cityConnectWorldPosition);

                    if (hasPreviousConnectWorldPosition)
                    {
                        Vector3 currentAutobahnWorldPosition = GetObjectMarkerWorldPosition(
                            connectorObject.transform,
                            connectorLayout.AutobahnConnectLocalPosition.Value);
                        Vector3 desiredDelta = previousConnectWorldPosition - currentAutobahnWorldPosition;
                        Vector3 axis = new(connectorAutobahnDirection.x, 0f, connectorAutobahnDirection.y);
                        if (axis.sqrMagnitude > 0.0001f)
                        {
                            axis.Normalize();
                            float signedDistance = Vector3.Dot(desiredDelta, axis);
                            connectorObject.transform.position += axis * signedDistance;
                        }
                    }
                }
                else if (hasPreviousConnectWorldPosition)
                {
                    PlaceObjectByMarker(
                        connectorObject.transform,
                        connectorVariant,
                        connectorLayout.AutobahnConnectLocalPosition.Value,
                        previousConnectWorldPosition);
                }
                else
                {
                    DestroySpecialRoadObject(connectorCell);
                    return;
                }

                expectedCells.Add(connectorCell);
            }
            else
            {
                DestroySpecialRoadObject(connectorCell);
            }
        }
    }

    private int GetAutobahnSpanInCells(MarkerLayoutData layout)
    {
        if (layout == null || layout.ConnectLocalPositions.Count < 2 || roadGridSize <= 0f)
            return 1;

        float span = 0f;
        for (int i = 0; i < layout.ConnectLocalPositions.Count; i++)
        {
            for (int j = i + 1; j < layout.ConnectLocalPositions.Count; j++)
            {
                Vector3 delta = layout.ConnectLocalPositions[j] - layout.ConnectLocalPositions[i];
                float axisSpan = Mathf.Max(Mathf.Abs(delta.x), Mathf.Abs(delta.z));
                if (axisSpan > span)
                    span = axisSpan;
            }
        }

        if (span <= 0f)
            return 1;

        return Mathf.Max(1, Mathf.RoundToInt(span / roadGridSize));
    }

    private GameObject GetOrCreateSpecialRoadObject(Vector2Int cell, RoadVisualType type)
    {
        if (_specialRoadObjects.TryGetValue(cell, out var roadObject) && roadObject != null)
            return roadObject;

        GameObject prefab = GetPrefab(type);
        Transform parent = type == RoadVisualType.AutobahnConnect
            ? (_specialRoadConnectorRoot != null ? _specialRoadConnectorRoot : _roadRoot)
            : (_specialRoadRoot != null ? _specialRoadRoot : _roadRoot);

        roadObject = Instantiate(prefab, parent);
        roadObject.name = $"{prefab.name}_{cell.x}_{cell.y}";
        _specialRoadObjects[cell] = roadObject;
        return roadObject;
    }

    private bool TryGetNeighborRoadConnectWorldPosition(Vector2Int cell, Vector2Int direction, out Vector3 worldPosition)
    {
        Vector2Int neighborCell = cell + direction;
        if (!_roadTiles.TryGetValue(neighborCell, out var tile))
        {
            worldPosition = default;
            return false;
        }

        if (!_markerLayouts.TryGetValue(tile.Type, out var layout) || layout.ConnectLocalPositions.Count == 0)
        {
            worldPosition = default;
            return false;
        }

        VariantData variant = new(tile.Rotation, tile.Scale);
        Vector3 localMarkerPosition = GetMarkerLocalPositionForDirection(layout, variant, -direction);
        Vector3 placedRoadPosition = GetPlacementPosition(neighborCell, variant);
        worldPosition = placedRoadPosition + variant.Rotation * Vector3.Scale(localMarkerPosition, variant.Scale);

        return true;
    }

    private Vector3 GetMarkerLocalPositionForDirection(MarkerLayoutData layout, VariantData variant, Vector2Int direction)
    {
        Vector3 selectedLocalPosition = layout.ConnectLocalPositions[0];
        float bestScore = float.NegativeInfinity;
        for (int i = 0; i < layout.ConnectLocalPositions.Count; i++)
        {
            Vector3 offset = layout.ConnectLocalPositions[i] - layout.Center;
            Vector3 transformedOffset = variant.Rotation * Vector3.Scale(offset, variant.Scale);
            float score = direction.x * transformedOffset.x + direction.y * transformedOffset.z;
            if (score > bestScore)
            {
                bestScore = score;
                selectedLocalPosition = layout.ConnectLocalPositions[i];
            }
        }

        return selectedLocalPosition;
    }

    private Vector3 GetCellMarkerWorldPosition(Vector2Int cell, Vector3 localMarkerPosition, VariantData variant)
    {
        Vector3 placementPosition = GetPlacementPosition(cell, variant);
        return placementPosition + variant.Rotation * Vector3.Scale(localMarkerPosition, variant.Scale);
    }

    private Vector3 GetObjectMarkerWorldPosition(Transform target, Vector3 localMarkerPosition)
    {
        return target.position + target.rotation * Vector3.Scale(localMarkerPosition, target.localScale);
    }

    private void PlaceObjectByMarker(Transform target, VariantData variant, Vector3 localMarkerPosition, Vector3 targetWorldPosition)
    {
        Vector3 worldPosition = targetWorldPosition - variant.Rotation * Vector3.Scale(localMarkerPosition, variant.Scale);
        target.SetPositionAndRotation(worldPosition, variant.Rotation);
        target.localScale = variant.Scale;
    }

    private bool TryBuildSpecialRoadMask(Vector2Int cell, out TileConnectionMask mask)
    {
        foreach (var stroke in _strokes.Values)
        {
            if (!stroke.IsAutobahn || stroke.Cells.Count < 2)
                continue;

            if (stroke.UseAutobahnConnectorAtStart && stroke.Cells[0] == cell)
            {
                Vector2Int direction = stroke.Cells[0] - stroke.Cells[1];
                mask = BuildAxisMask(direction);
                return true;
            }

            if (stroke.UseAutobahnConnectorAtEnd && stroke.Cells[stroke.Cells.Count - 1] == cell)
            {
                Vector2Int direction = stroke.Cells[stroke.Cells.Count - 2] - stroke.Cells[stroke.Cells.Count - 1];
                mask = BuildAxisMask(direction);
                return true;
            }

            for (int i = 1; i < stroke.Cells.Count - 1; i++)
            {
                if (stroke.Cells[i] != cell)
                    continue;

                Vector2Int direction = stroke.Cells[i + 1] - stroke.Cells[i];
                mask = BuildAxisMask(direction);
                return true;
            }
        }

        mask = default;
        return false;
    }

    private static TileConnectionMask BuildAxisMask(Vector2Int direction)
    {
        if (direction.x != 0)
            return new TileConnectionMask(false, true, false, true);

        return new TileConnectionMask(true, false, true, false);
    }

    private static TileConnectionMask BuildMaskFromDirections(params Vector2Int[] directions)
    {
        bool north = false;
        bool east = false;
        bool south = false;
        bool west = false;

        for (int i = 0; i < directions.Length; i++)
        {
            Vector2Int direction = directions[i];
            if (direction == North)
                north = true;
            else if (direction == East)
                east = true;
            else if (direction == South)
                south = true;
            else if (direction == West)
                west = true;
        }

        return new TileConnectionMask(north, east, south, west);
    }

    private bool TryGetAutobahnDirection(Vector2Int cell, out Vector2Int direction)
    {
        foreach (var stroke in _strokes.Values)
        {
            if (!stroke.IsAutobahn || stroke.Cells.Count < 2)
                continue;

            if (stroke.UseAutobahnConnectorAtStart && stroke.Cells[0] == cell)
            {
                direction = stroke.Cells[1] - stroke.Cells[0];
                return true;
            }

            if (stroke.UseAutobahnConnectorAtEnd && stroke.Cells[stroke.Cells.Count - 1] == cell)
            {
                direction = stroke.Cells[stroke.Cells.Count - 2] - stroke.Cells[stroke.Cells.Count - 1];
                return true;
            }
        }

        direction = default;
        return false;
    }

    private bool TryGetAutobahnConnectorVariantForTargets(
        Vector3 cityConnectWorldPosition,
        Vector3 autobahnTargetWorldPosition,
        Vector2Int autobahnDirection,
        out VariantData variant)
    {
        variant = default;
        if (!_autobahnConnectorMarkerData.HasValue)
            return false;

        ConnectorMarkerData markerData = _autobahnConnectorMarkerData.Value;
        int[] rotationAngles = { 0, 90, 180, 270 };
        int[] flipValues = { 1, -1 };
        float bestDistanceSq = float.PositiveInfinity;
        float bestDirectionScore = float.NegativeInfinity;
        bool found = false;
        Vector3 desiredDelta = autobahnTargetWorldPosition - cityConnectWorldPosition;
        Vector3 desiredPlanarDirection = new(desiredDelta.x, 0f, desiredDelta.z);
        bool hasDesiredDirection = desiredPlanarDirection.sqrMagnitude > 0.0001f;
        if (hasDesiredDirection)
            desiredPlanarDirection.Normalize();

        foreach (int angle in rotationAngles)
        {
            Quaternion rotation = Quaternion.Euler(0f, angle, 0f);
            foreach (int scaleX in flipValues)
            {
                foreach (int scaleZ in flipValues)
                {
                    Vector3 scale = new(scaleX, 1f, scaleZ);
                    VariantData candidate = new(rotation, scale);
                    Vector3 candidatePosition = cityConnectWorldPosition - rotation * Vector3.Scale(markerData.RoadConnectLocalPosition, scale);
                    Vector3 candidateAutobahnWorldPosition = candidatePosition + rotation * Vector3.Scale(markerData.AutobahnConnectLocalPosition, scale);
                    float distanceSq = (candidateAutobahnWorldPosition - autobahnTargetWorldPosition).sqrMagnitude;
                    Vector3 candidateDelta = candidateAutobahnWorldPosition - cityConnectWorldPosition;
                    bool matchesDirection =
                        autobahnDirection.x > 0 ? candidateDelta.x > 0f && Mathf.Abs(candidateDelta.x) >= Mathf.Abs(candidateDelta.z) :
                        autobahnDirection.x < 0 ? candidateDelta.x < 0f && Mathf.Abs(candidateDelta.x) >= Mathf.Abs(candidateDelta.z) :
                        autobahnDirection.y > 0 ? candidateDelta.z > 0f && Mathf.Abs(candidateDelta.z) >= Mathf.Abs(candidateDelta.x) :
                        candidateDelta.z < 0f && Mathf.Abs(candidateDelta.z) >= Mathf.Abs(candidateDelta.x);

                    if (!matchesDirection)
                        continue;

                    Vector3 candidatePlanarDirection = new(candidateDelta.x, 0f, candidateDelta.z);
                    float directionScore = 0f;
                    if (hasDesiredDirection && candidatePlanarDirection.sqrMagnitude > 0.0001f)
                    {
                        candidatePlanarDirection.Normalize();
                        directionScore = Vector3.Dot(candidatePlanarDirection, desiredPlanarDirection);
                    }

                    const float distanceEpsilon = 0.0001f;
                    bool isBetterDistance = distanceSq < bestDistanceSq - distanceEpsilon;
                    bool isTieButBetterDirection = Mathf.Abs(distanceSq - bestDistanceSq) <= distanceEpsilon &&
                                                  directionScore > bestDirectionScore + 0.0001f;

                    if (!isBetterDistance && !isTieButBetterDirection)
                        continue;

                    bestDistanceSq = distanceSq;
                    bestDirectionScore = directionScore;
                    variant = candidate;
                    found = true;
                }
            }
        }

        return found;
    }

    private bool TryGetAutobahnConnectorVariant(Vector2Int autobahnDirection, out VariantData variant)
    {
        variant = default;
        if (!_autobahnConnectorMarkerData.HasValue)
            return false;

        ConnectorMarkerData markerData = _autobahnConnectorMarkerData.Value;
        int[] rotationAngles = { 0, 90, 180, 270 };
        int[] flipValues = { 1, -1 };
        foreach (int angle in rotationAngles)
        {
            Quaternion rotation = Quaternion.Euler(0f, angle, 0f);
            foreach (int scaleX in flipValues)
            {
                foreach (int scaleZ in flipValues)
                {
                    Vector3 scale = new(scaleX, 1f, scaleZ);
                    Vector3 roadOffset = rotation * Vector3.Scale(markerData.RoadConnectLocalPosition - markerData.Center, scale);
                    Vector3 autobahnOffset = rotation * Vector3.Scale(markerData.AutobahnConnectLocalPosition - markerData.Center, scale);
                    Vector3 delta = autobahnOffset - roadOffset;

                    bool matchesDirection =
                        autobahnDirection.x > 0 ? delta.x > 0f && Mathf.Abs(delta.x) >= Mathf.Abs(delta.z) :
                        autobahnDirection.x < 0 ? delta.x < 0f && Mathf.Abs(delta.x) >= Mathf.Abs(delta.z) :
                        autobahnDirection.y > 0 ? delta.z > 0f && Mathf.Abs(delta.z) >= Mathf.Abs(delta.x) :
                        delta.z < 0f && Mathf.Abs(delta.z) >= Mathf.Abs(delta.x);

                    if (!matchesDirection)
                        continue;

                    variant = new VariantData(rotation, scale);
                    return true;
                }
            }
        }

        return false;
    }

    private void GetRoadWorldFootprint(Vector2Int roadCell, RoadTileData tile, out Vector3 worldMin, out Vector3 worldMax)
    {
        bool hasBounds = false;
        Bounds combinedBounds = default;

        ForEachRoadWorldFootprint(roadCell, tile, (footprintMin, footprintMax) =>
        {
            var footprintBounds = new Bounds((footprintMin + footprintMax) * 0.5f, footprintMax - footprintMin);
            if (!hasBounds)
            {
                combinedBounds = footprintBounds;
                hasBounds = true;
            }
            else
            {
                combinedBounds.Encapsulate(footprintMin);
                combinedBounds.Encapsulate(footprintMax);
            }

            return true;
        });

        if (hasBounds)
        {
            worldMin = combinedBounds.min;
            worldMax = combinedBounds.max;
            return;
        }

        worldMin = gridOrigin + new Vector3(roadCell.x * roadGridSize, 0f, roadCell.y * roadGridSize);
        worldMax = worldMin + new Vector3(roadGridSize, 0f, roadGridSize);
    }

    private void ForEachRoadWorldFootprint(Vector2Int roadCell, RoadTileData tile, Func<Vector3, Vector3, bool> visitor)
    {
        ForEachRoadWorldFootprintKind(roadCell, tile, (worldMin, worldMax, _) => visitor(worldMin, worldMax));
    }

    private void ForEachRoadWorldFootprintKind(Vector2Int roadCell, RoadTileData tile, Func<Vector3, Vector3, FootprintKind, bool> visitor)
    {
        if (_specialRoadObjects.TryGetValue(roadCell, out var specialRoadObject) && specialRoadObject != null)
        {
            MeshFilter[] meshFilters = specialRoadObject.GetComponentsInChildren<MeshFilter>(true);
            bool foundSpecialBounds = false;
            for (int i = 0; i < meshFilters.Length; i++)
            {
                MeshFilter meshFilter = meshFilters[i];
                if (meshFilter.sharedMesh == null)
                    continue;
                if (!TryGetFootprintKind(
                        meshFilter.transform,
                        tile.Type == RoadVisualType.Autobahn || tile.Type == RoadVisualType.AutobahnConnect,
                        out FootprintKind footprintKind))
                    continue;

                Bounds worldBounds = TransformBounds(meshFilter.sharedMesh.bounds, meshFilter.transform.localToWorldMatrix);
                foundSpecialBounds = true;
                if (!visitor(worldBounds.min, worldBounds.max, footprintKind))
                    return;
            }

            if (foundSpecialBounds)
                return;
        }

        if (_visualData.TryGetValue(tile.Type, out var visualData) &&
            visualData.FootprintBounds != null &&
            visualData.FootprintBounds.Count > 0)
        {
            Vector3 basePosition = GetPlacementPosition(roadCell, new VariantData(tile.Rotation, tile.Scale));
            for (int boundsIndex = 0; boundsIndex < visualData.FootprintBounds.Count; boundsIndex++)
            {
                FootprintBoundsData footprintData = visualData.FootprintBounds[boundsIndex];
                Bounds localBounds = footprintData.Bounds;
                Vector3[] corners =
                {
                    new(localBounds.min.x, localBounds.min.y, localBounds.min.z),
                    new(localBounds.min.x, localBounds.min.y, localBounds.max.z),
                    new(localBounds.min.x, localBounds.max.y, localBounds.min.z),
                    new(localBounds.min.x, localBounds.max.y, localBounds.max.z),
                    new(localBounds.max.x, localBounds.min.y, localBounds.min.z),
                    new(localBounds.max.x, localBounds.min.y, localBounds.max.z),
                    new(localBounds.max.x, localBounds.max.y, localBounds.min.z),
                    new(localBounds.max.x, localBounds.max.y, localBounds.max.z)
                };

                Vector3 worldMin = new(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
                Vector3 worldMax = new(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
                for (int i = 0; i < corners.Length; i++)
                {
                    Vector3 worldCorner = basePosition + tile.Rotation * Vector3.Scale(corners[i], tile.Scale);
                    worldMin = Vector3.Min(worldMin, worldCorner);
                    worldMax = Vector3.Max(worldMax, worldCorner);
                }

                if (!visitor(worldMin, worldMax, footprintData.Kind))
                    return;
            }

            return;
        }

        if (_visualData.TryGetValue(tile.Type, out var fallbackVisualData) && fallbackVisualData.Mesh != null)
        {
            Bounds localBounds = fallbackVisualData.Mesh.bounds;
            Vector3 basePosition = GetPlacementPosition(roadCell, new VariantData(tile.Rotation, tile.Scale));
            Vector3[] corners =
            {
                new(localBounds.min.x, localBounds.min.y, localBounds.min.z),
                new(localBounds.min.x, localBounds.min.y, localBounds.max.z),
                new(localBounds.min.x, localBounds.max.y, localBounds.min.z),
                new(localBounds.min.x, localBounds.max.y, localBounds.max.z),
                new(localBounds.max.x, localBounds.min.y, localBounds.min.z),
                new(localBounds.max.x, localBounds.min.y, localBounds.max.z),
                new(localBounds.max.x, localBounds.max.y, localBounds.min.z),
                new(localBounds.max.x, localBounds.max.y, localBounds.max.z)
            };

            Vector3 worldMin = new(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            Vector3 worldMax = new(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
            for (int i = 0; i < corners.Length; i++)
            {
                Vector3 worldCorner = basePosition + tile.Rotation * Vector3.Scale(corners[i], tile.Scale);
                worldMin = Vector3.Min(worldMin, worldCorner);
                worldMax = Vector3.Max(worldMax, worldCorner);
            }

            visitor(worldMin, worldMax, FootprintKind.Dirt);
            return;
        }

        Vector3 fallbackMin = gridOrigin + new Vector3(roadCell.x * roadGridSize, 0f, roadCell.y * roadGridSize);
        Vector3 fallbackMax = fallbackMin + new Vector3(roadGridSize, 0f, roadGridSize);
        visitor(fallbackMin, fallbackMax, FootprintKind.Dirt);
    }

    private static bool ShouldReserveRoadRenderer(string name)
    {
        if (string.IsNullOrEmpty(name))
            return false;

        return IsReserveMarkerName(name) ||
               name.IndexOf("sm_env_dirt", StringComparison.OrdinalIgnoreCase) >= 0 ||
               name.IndexOf("sm_env_sidewalk", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool TryGetFootprintKind(Transform transform, bool typeOverride, out FootprintKind kind)
    {
        Transform current = transform;
        while (current != null)
        {
            if (IsSidewalkMarkerName(current.name))
            {
                kind = FootprintKind.Sidewalk;
                return true;
            }

            if (IsDirtMarkerName(current.name))
            {
                kind = FootprintKind.Dirt;
                return true;
            }

            if (!typeOverride && ShouldReserveRoadRenderer(current.name))
            {
                kind = current.name.IndexOf("sidewalk", StringComparison.OrdinalIgnoreCase) >= 0
                    ? FootprintKind.Sidewalk
                    : FootprintKind.Dirt;
                return true;
            }

            current = current.parent;
        }

        kind = FootprintKind.Dirt;
        return false;
    }

    private static bool IsReserveMarkerName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return false;

        return IsDirtMarkerName(name) || IsSidewalkMarkerName(name);
    }

    private static bool IsDirtMarkerName(string name)
    {
        return string.Equals(name, "Dirt", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSidewalkMarkerName(string name)
    {
        return string.Equals(name, "Sidewalk", StringComparison.OrdinalIgnoreCase);
    }

    private static Bounds TransformBounds(Bounds localBounds, Matrix4x4 matrix)
    {
        Vector3[] corners =
        {
            new(localBounds.min.x, localBounds.min.y, localBounds.min.z),
            new(localBounds.min.x, localBounds.min.y, localBounds.max.z),
            new(localBounds.min.x, localBounds.max.y, localBounds.min.z),
            new(localBounds.min.x, localBounds.max.y, localBounds.max.z),
            new(localBounds.max.x, localBounds.min.y, localBounds.min.z),
            new(localBounds.max.x, localBounds.min.y, localBounds.max.z),
            new(localBounds.max.x, localBounds.max.y, localBounds.min.z),
            new(localBounds.max.x, localBounds.max.y, localBounds.max.z)
        };

        Vector3 min = new(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
        Vector3 max = new(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
        for (int i = 0; i < corners.Length; i++)
        {
            Vector3 worldCorner = matrix.MultiplyPoint3x4(corners[i]);
            min = Vector3.Min(min, worldCorner);
            max = Vector3.Max(max, worldCorner);
        }

        return new Bounds((min + max) * 0.5f, max - min);
    }

    private void DestroySpecialRoadObject(Vector2Int cell)
    {
        if (!_specialRoadObjects.TryGetValue(cell, out var roadObject))
            return;

        if (roadObject != null)
        {
            Destroy(roadObject);
        }

        _specialRoadObjects.Remove(cell);
    }

    private void ClearSpecialRoadObjects()
    {
        foreach (var roadObject in _specialRoadObjects.Values)
        {
            if (roadObject != null)
                Destroy(roadObject);
        }

        _specialRoadObjects.Clear();
    }

    private void ClearDebugStraightRoadObjects()
    {
        foreach (var roadObject in _debugStraightRoadObjects)
        {
            if (roadObject != null)
                Destroy(roadObject);
        }

        _debugStraightRoadObjects.Clear();
    }

    private GameObject CreateStandaloneStraightBranch(
        Transform sourceTransform,
        MarkerLayoutData sourceLayout,
        VariantData sourceVariant,
        Vector2Int branchDirection,
        int branchLength)
    {
        if (branchLength <= 0)
            return sourceTransform != null ? sourceTransform.gameObject : null;
        if (!_markerLayouts.TryGetValue(RoadVisualType.Straight, out var straightLayout) ||
            straightLayout.ConnectLocalPositions.Count < 2)
            return sourceTransform != null ? sourceTransform.gameObject : null;
        if (!TryGetVariant(RoadVisualType.Straight, BuildAxisMask(branchDirection), out var straightVariant))
            return sourceTransform != null ? sourceTransform.gameObject : null;

        Vector3 previousConnectWorldPosition = GetObjectMarkerWorldPosition(
            sourceTransform,
            GetMarkerLocalPositionForDirection(sourceLayout, sourceVariant, branchDirection));
        GameObject lastRoadObject = sourceTransform.gameObject;

        for (int i = 0; i < branchLength; i++)
        {
            GameObject prefab = GetPrefab(RoadVisualType.Straight);
            if (prefab == null)
                return lastRoadObject;

            GameObject roadObject = Instantiate(
                prefab,
                _debugStraightRoadRoot != null ? _debugStraightRoadRoot : _roadRoot);
            roadObject.name = $"{prefab.name}_DebugCityBranch_{branchDirection.x}_{branchDirection.y}_{i}";

            Vector3 incomingMarkerLocalPosition = GetMarkerLocalPositionForDirection(
                straightLayout,
                straightVariant,
                -branchDirection);
            Vector3 outgoingMarkerLocalPosition = GetMarkerLocalPositionForDirection(
                straightLayout,
                straightVariant,
                branchDirection);

            PlaceObjectByMarker(
                roadObject.transform,
                straightVariant,
                incomingMarkerLocalPosition,
                previousConnectWorldPosition);
            previousConnectWorldPosition = GetObjectMarkerWorldPosition(
                roadObject.transform,
                outgoingMarkerLocalPosition);
            _debugStraightRoadObjects.Add(roadObject);
            lastRoadObject = roadObject;
        }

        return lastRoadObject;
    }

    private static Mesh BuildChunkMesh(
        Vector2Int chunkCoord,
        List<Material> materialOrder,
        Dictionary<Material, List<CombineInstance>> combinesByMaterial)
    {
        var submeshCombines = new CombineInstance[materialOrder.Count];
        for (int i = 0; i < materialOrder.Count; i++)
        {
            var combines = combinesByMaterial[materialOrder[i]];
            Mesh submeshMesh = new Mesh
            {
                name = $"RoadChunk_{chunkCoord.x}_{chunkCoord.y}_{i}"
            };
            submeshMesh.indexFormat = IndexFormat.UInt32;
            submeshMesh.CombineMeshes(combines.ToArray(), true, true, false);
            submeshCombines[i] = new CombineInstance
            {
                mesh = submeshMesh,
                subMeshIndex = 0,
                transform = Matrix4x4.identity
            };
        }

        Mesh combinedMesh = new Mesh
        {
            name = $"RoadChunk_{chunkCoord.x}_{chunkCoord.y}_Combined",
            indexFormat = IndexFormat.UInt32
        };
        combinedMesh.CombineMeshes(submeshCombines, false, false, false);

        for (int i = 0; i < submeshCombines.Length; i++)
            UnityEngine.Object.Destroy(submeshCombines[i].mesh);

        return combinedMesh;
    }

    private Vector2Int GetChunkCoord(Vector2Int cell)
    {
        int chunkSize = Mathf.Max(1, chunkSizeInCells);
        return new Vector2Int(
            Mathf.FloorToInt((float)cell.x / chunkSize),
            Mathf.FloorToInt((float)cell.y / chunkSize));
    }

    private void AddCellToChunk(Vector2Int cell)
    {
        Vector2Int chunkCoord = GetChunkCoord(cell);
        if (!_chunkCells.TryGetValue(chunkCoord, out var cells))
        {
            cells = new HashSet<Vector2Int>();
            _chunkCells.Add(chunkCoord, cells);
        }

        cells.Add(cell);
        MarkChunkDirty(chunkCoord);
    }

    private void RemoveCellFromChunk(Vector2Int cell)
    {
        Vector2Int chunkCoord = GetChunkCoord(cell);
        if (_chunkCells.TryGetValue(chunkCoord, out var cells))
        {
            cells.Remove(cell);
            if (cells.Count == 0)
                _chunkCells.Remove(chunkCoord);
        }

        MarkChunkDirty(chunkCoord);
    }

    private GameObject GetPreviewObject(RoadVisualType type)
    {
        if (_previewPool.TryGetValue(type, out var pool))
        {
            while (pool.Count > 0)
            {
                GameObject pooled = pool.Pop();
                if (pooled == null)
                    continue;

                pooled.SetActive(true);
                return pooled;
            }
        }

        GameObject preview = CreateRuntimeRoadObject(type, true);
        if (preview != null)
            _previewObjectTypes[preview] = type;

        return preview;
    }

    private void ReleasePreviewObject(GameObject preview)
    {
        if (preview == null)
            return;

        preview.SetActive(false);

        if (!_previewObjectTypes.TryGetValue(preview, out var type))
        {
            Destroy(preview);
            return;
        }

        if (!_previewPool.TryGetValue(type, out var pool))
        {
            pool = new Stack<GameObject>();
            _previewPool.Add(type, pool);
        }

        pool.Push(preview);
    }

    private void SyncRoadCellsToEcs()
    {
        if (!TryGetRoadBuffers(out var roadBuffers))
            return;

        for (int i = 0; i < roadBuffers.Roads.Length; i++)
        {
            roadBuffers.Roads[i] = new GridRoad { Value = 0 };
            roadBuffers.Sidewalks[i] = new GridRoadSidewalk { Value = 0 };
            roadBuffers.DirtRoads[i] = new GridRoadDirt { Value = 0 };
        }

        GridConfig grid = roadBuffers.Grid;
        if (roadGridSize <= 0f || grid.CellSize <= 0f)
            return;

        foreach (var entry in _roadTiles)
        {
            Vector2Int roadCell = entry.Key;
            ForEachRoadWorldFootprintKind(roadCell, entry.Value, (worldMin, worldMax, kind) =>
            {
                float3 localMin = (float3)(worldMin - (Vector3)grid.Origin);
                float3 localMax = (float3)(worldMax - (Vector3)grid.Origin);

                int minX = Mathf.FloorToInt(localMin.x / grid.CellSize);
                int minY = Mathf.FloorToInt(localMin.z / grid.CellSize);
                int maxX = Mathf.CeilToInt(localMax.x / grid.CellSize);
                int maxY = Mathf.CeilToInt(localMax.z / grid.CellSize);

                minX = Mathf.Clamp(minX, 0, grid.Width);
                minY = Mathf.Clamp(minY, 0, grid.Height);
                maxX = Mathf.Clamp(maxX, 0, grid.Width);
                maxY = Mathf.Clamp(maxY, 0, grid.Height);

                for (int y = minY; y < maxY; y++)
                {
                    for (int x = minX; x < maxX; x++)
                    {
                        if (!IsGridCellCenterInsideBounds(grid, x, y, worldMin, worldMax))
                            continue;

                        int index = GridUtils.CellToIndex(new Unity.Mathematics.int2(x, y), grid.Width);
                        roadBuffers.Roads[index] = new GridRoad { Value = 1 };
                        if (kind == FootprintKind.Sidewalk)
                            roadBuffers.Sidewalks[index] = new GridRoadSidewalk { Value = 1 };
                        else
                            roadBuffers.DirtRoads[index] = new GridRoadDirt { Value = 1 };
                    }
                }

                return true;
            });
        }
    }

    private void RemoveRuntimeBlockersUnderRoads()
    {
        if (_runtimeGridBlockers == null || !TryGetRoadBuffer(out _, out var grid))
            return;

        foreach (var entry in _roadTiles)
        {
            Vector2Int roadCell = entry.Key;
            ForEachRoadWorldFootprint(roadCell, entry.Value, (worldMin, worldMax) =>
            {
                float3 localMin = (float3)(worldMin - (Vector3)grid.Origin);
                float3 localMax = (float3)(worldMax - (Vector3)grid.Origin);

                int minX = Mathf.Clamp(Mathf.FloorToInt(localMin.x / grid.CellSize), 0, grid.Width);
                int minY = Mathf.Clamp(Mathf.FloorToInt(localMin.z / grid.CellSize), 0, grid.Height);
                int maxX = Mathf.Clamp(Mathf.CeilToInt(localMax.x / grid.CellSize), 0, grid.Width);
                int maxY = Mathf.Clamp(Mathf.CeilToInt(localMax.z / grid.CellSize), 0, grid.Height);

                int overlapMinX = int.MaxValue;
                int overlapMinY = int.MaxValue;
                int overlapMaxX = int.MinValue;
                int overlapMaxY = int.MinValue;

                for (int y = minY; y < maxY; y++)
                {
                    for (int x = minX; x < maxX; x++)
                    {
                        if (!IsGridCellCenterInsideBounds(grid, x, y, worldMin, worldMax))
                            continue;

                        overlapMinX = Mathf.Min(overlapMinX, x);
                        overlapMinY = Mathf.Min(overlapMinY, y);
                        overlapMaxX = Mathf.Max(overlapMaxX, x + 1);
                        overlapMaxY = Mathf.Max(overlapMaxY, y + 1);
                    }
                }

                if (overlapMaxX > overlapMinX && overlapMaxY > overlapMinY)
                {
                    _runtimeGridBlockers.RemoveBlockersOverlappingFootprint(
                        new Vector2Int(overlapMinX, overlapMinY),
                        new Vector2Int(overlapMaxX - overlapMinX, overlapMaxY - overlapMinY));
                }
                return true;
            });
        }
    }

    private static bool IsGridCellCenterInsideBounds(GridConfig grid, int x, int y, Vector3 worldMin, Vector3 worldMax)
    {
        Vector3 center = (Vector3)grid.Origin + new Vector3((x + 0.5f) * grid.CellSize, 0f, (y + 0.5f) * grid.CellSize);
        return center.x >= worldMin.x && center.x < worldMax.x &&
               center.z >= worldMin.z && center.z < worldMax.z;
    }

    private void ClearRoadDataInEcs()
    {
        if (!TryGetRoadBuffers(out var roadBuffers))
            return;

        for (int i = 0; i < roadBuffers.Roads.Length; i++)
        {
            roadBuffers.Roads[i] = new GridRoad { Value = 0 };
            roadBuffers.Sidewalks[i] = new GridRoadSidewalk { Value = 0 };
            roadBuffers.DirtRoads[i] = new GridRoadDirt { Value = 0 };
        }
    }

    private bool TryGetRoadBuffer(out DynamicBuffer<GridRoad> roads, out GridConfig grid)
    {
        roads = default;
        grid = default;

        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return false;

        var entityManager = world.EntityManager;
        EnsureEntityQueries(entityManager);
        if (_roadBufferQuery.IsEmptyIgnoreFilter)
            return false;

        Entity gridEntity = _roadBufferQuery.GetSingletonEntity();
        grid = entityManager.GetComponentData<GridConfig>(gridEntity);
        roads = entityManager.GetBuffer<GridRoad>(gridEntity);
        return true;
    }

    private bool TryGetRoadBuffers(out RoadBuffersData roadBuffers)
    {
        roadBuffers = default;

        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return false;

        var entityManager = world.EntityManager;
        EnsureEntityQueries(entityManager);
        if (_roadBuffersQuery.IsEmptyIgnoreFilter)
            return false;

        Entity gridEntity = _roadBuffersQuery.GetSingletonEntity();
        roadBuffers = new RoadBuffersData(
            entityManager.GetBuffer<GridRoad>(gridEntity),
            entityManager.GetBuffer<GridRoadSidewalk>(gridEntity),
            entityManager.GetBuffer<GridRoadDirt>(gridEntity),
            entityManager.GetComponentData<GridConfig>(gridEntity));
        return true;
    }
}
