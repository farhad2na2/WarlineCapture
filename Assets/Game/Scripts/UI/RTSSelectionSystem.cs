using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.InputSystem;
using Unity.Transforms;
using UnityEngine;
using static UnityEngine.Object;

public sealed class RTSSelectionSystem
{
    private const bool EnableMoveOrderDiagnostics = false;
    private static readonly bool EnableGroupMoveValidationLog = false;
    private const int GroupMoveStaggerMinGroundUnits = 12;
    private const int GroupMoveImmediatePathRequests = 8;
    private const int GroupMovePathRequestsPerFrame = 8;
    public event System.Action<Vector2> MoveOrderScreenMarkerRequested;
    public event System.Action<Vector2> AttackOrderScreenMarkerRequested;
    public event System.Action OrderScreenMarkersHideRequested;

    public readonly struct TransportPassengerUiInfo
    {
        public readonly Entity Entity;
        public readonly string DisplayName;
        public readonly int HealthCurrent;
        public readonly int HealthMax;

        public TransportPassengerUiInfo(Entity entity, string displayName, int healthCurrent, int healthMax)
        {
            Entity = entity;
            DisplayName = displayName;
            HealthCurrent = healthCurrent;
            HealthMax = healthMax;
        }
    }

    public enum FocusedUnitUiStatus
    {
        Idle = 0,
        Moving = 1,
        Engaged = 2,
        ReturningToBase = 3
    }

    private enum VisibleUnitSelectionFilter
    {
        All,
        Soldiers,
        Vehicles
    }

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private struct PreservedOrderState
    {
        public Entity Entity;
        public bool HadEngageTarget;
        public EngageTarget EngageTarget;
        public bool HadUnitTarget;
        public UnitTarget UnitTarget;
        public bool HadUnitPathRequest;
        public UnitPathRequest UnitPathRequest;
        public bool HadUnitPathFollow;
        public UnitPathFollow UnitPathFollow;
        public bool HadUnitPathRange;
        public UnitPathRange UnitPathRange;
    }

    private struct FocusableUnitCoverage
    {
        public int2 Cell;
        public int2 Size;
        public int Padding;
    }

    private struct PendingTransportBoardingOrder
    {
        public Entity Passenger;
        public int2 PassengerCell;
        public int2 Goal;
        public bool DirectBoarding;
    }

    private static void LogTransportBoarding(string message)
    {
        if (InitialUnitsRuntimeState.TransportBoardingDiagnostics)
            Debug.Log($"[TransportBoard] {message}");
    }

    private static void LogSelectionDiagnostic(string message)
    {
        if (InitialUnitsRuntimeState.TransportBoardingDiagnostics)
            Debug.Log($"[Selection] {message}");
    }

    private const float DefaultPanSensitivity = 0.03f;
    private const float DefaultZoomSpeed = 20f;
    private const float DefaultMinZoomHeight = 10f;
    private const float DefaultMaxZoomHeight = 45f;

    [SerializeField] private RTSSelectionSystemConfig config;
    [SerializeField, HideInInspector] private Camera worldCamera;
    [SerializeField, HideInInspector] private GameObject moveOrderMarkerPrefab;
    [SerializeField, HideInInspector] private float orderMarkerVisibleSeconds = 1.25f;
    [SerializeField, HideInInspector] private GameObject attackOrderMarkerPrefab;
    [SerializeField, HideInInspector] private Color selectionFill = new(0.2f, 1f, 0.2f, 0.15f);
    [SerializeField, HideInInspector] private Color selectionBorder = new(0.2f, 1f, 0.2f, 0.95f);
    [SerializeField, HideInInspector] private float dragThresholdPixels = 8f;
    [SerializeField, HideInInspector] private float panSensitivity = DefaultPanSensitivity;
    [SerializeField, HideInInspector] private float zoomSpeed = DefaultZoomSpeed;
    [SerializeField, HideInInspector] private float minZoomHeight = DefaultMinZoomHeight;
    [SerializeField, HideInInspector] private float maxZoomHeight = DefaultMaxZoomHeight;
    [SerializeField, HideInInspector] private float normalModeZoomHeight = 24f;
    [SerializeField, HideInInspector] private float buildModeZoomHeight = 100f;
    [SerializeField, HideInInspector] private float normalModePitch = 58f;
    [SerializeField, HideInInspector] private float buildModePitch = 64f;
    [SerializeField, HideInInspector] private float normalModeYaw = 10f;
    [SerializeField, HideInInspector] private float buildModeYaw = 10f;
    [SerializeField, HideInInspector] private float normalModeFieldOfView = 36f;
    [SerializeField, HideInInspector] private float buildModeFieldOfView = 32f;
    [SerializeField, HideInInspector] private float fullscreenIsoZoomHeight = 40f;
    [SerializeField, HideInInspector] private float fullscreenIsoPitch = 82f;
    [SerializeField, HideInInspector] private float fullscreenIsoYaw = 10f;
    [SerializeField, HideInInspector] private float fullscreenIsoOrthographicSize = 24f;
    [SerializeField, HideInInspector] private float zoomTransitionSmoothTime = 0.25f;

    private Texture2D _pixel;
    private readonly RuntimeGameplayStateSystem _runtimeGameplayStateSystem = new();
    private readonly RtsSelectionInputSystem _rtsSelectionInputSystem = new();
    private readonly RtsCameraSystem _rtsCameraSystem = new();
    private readonly SelectionStateSystem _selectionStateSystem = new();
    private readonly SelectionUiQuerySystem _selectionUiQuerySystem = new();
    private readonly UnitMoveOrderSystem _unitMoveOrderSystem = new();
    private readonly UnitTargetOrderSystem _unitTargetOrderSystem = new();
    private UnitTransportBoardingSystem _unitTransportBoardingSystem;
    private Entity _focusedUnit
    {
        get => _selectionStateSystem.FocusedUnit;
        set => _selectionStateSystem.SetFocusedUnit(value);
    }

    private List<Entity> _cachedSelectedMoveEntities => _selectionStateSystem.CachedSelectedMoveEntities;
    private bool _cameraDragging
    {
        get => _rtsCameraSystem.IsDragging;
        set => _rtsCameraSystem.SetDragging(value);
    }

    private bool _wasPlayRequested
    {
        get => _rtsCameraSystem.WasPlayRequested;
        set => _rtsCameraSystem.WasPlayRequested = value;
    }

    private bool _wasBuildModeActive
    {
        get => _rtsCameraSystem.WasBuildModeActive;
        set => _rtsCameraSystem.WasBuildModeActive = value;
    }

    private bool _isZoomTransitionActive
    {
        get => _rtsCameraSystem.IsZoomTransitionActive;
        set => _rtsCameraSystem.IsZoomTransitionActive = value;
    }

    private float _fullscreenIsoTargetHeight
    {
        get => _rtsCameraSystem.FullscreenIsoTargetHeight;
        set => _rtsCameraSystem.FullscreenIsoTargetHeight = value;
    }

    private float _fullscreenIsoTargetOrthographicSize
    {
        get => _rtsCameraSystem.FullscreenIsoTargetOrthographicSize;
        set => _rtsCameraSystem.FullscreenIsoTargetOrthographicSize = value;
    }

    private bool _normalIsoModeActive
    {
        get => _rtsCameraSystem.NormalIsoModeActive;
        set => _rtsCameraSystem.NormalIsoModeActive = value;
    }

    private Vector2 _dragStart
    {
        get => _rtsSelectionInputSystem.DragStart;
        set => _rtsSelectionInputSystem.DragStart = value;
    }

    private Vector2 _dragCurrent
    {
        get => _rtsSelectionInputSystem.DragCurrent;
        set => _rtsSelectionInputSystem.DragCurrent = value;
    }

    private Vector2 _lastPointerPosition
    {
        get => _rtsSelectionInputSystem.LastPointerPosition;
        set => _rtsSelectionInputSystem.LastPointerPosition = value;
    }

    private bool _pointerPressedOverUi
    {
        get => _rtsSelectionInputSystem.PointerPressedOverUi;
        set => _rtsSelectionInputSystem.PointerPressedOverUi = value;
    }

    private bool _dragging
    {
        get => _rtsSelectionInputSystem.IsDraggingSelection;
        set => _rtsSelectionInputSystem.IsDraggingSelection = value;
    }

    private bool _ignoreNextLeftMouseRelease
    {
        get => _rtsSelectionInputSystem.IgnoreNextLeftMouseRelease;
        set => _rtsSelectionInputSystem.IgnoreNextLeftMouseRelease = value;
    }

    private bool _skipNextWorldReleaseAfterSelection
    {
        get => _rtsSelectionInputSystem.SkipNextWorldReleaseAfterSelection;
        set => _rtsSelectionInputSystem.SkipNextWorldReleaseAfterSelection = value;
    }

    private int _ignoreWorldCommandsUntilFrame
    {
        get => _rtsSelectionInputSystem.IgnoreWorldCommandsUntilFrame;
        set => _rtsSelectionInputSystem.IgnoreWorldCommandsUntilFrame = value;
    }

    private bool _ignoreUiClickUntilRelease
    {
        get => _rtsSelectionInputSystem.IgnoreUiClickUntilRelease;
        set => _rtsSelectionInputSystem.IgnoreUiClickUntilRelease = value;
    }

    private bool _selectionModeHoldArmed
    {
        get => _rtsSelectionInputSystem.SelectionModeHoldArmed;
        set => _rtsSelectionInputSystem.SelectionModeHoldArmed = value;
    }

    private float _selectionModeHoldStartTime
    {
        get => _rtsSelectionInputSystem.SelectionModeHoldStartTime;
        set => _rtsSelectionInputSystem.SelectionModeHoldStartTime = value;
    }

    private MainMenuPlayUI _mainMenuPlayUi;
    private RoadBuildSystem _roadBuildController;
    private BuildingPlacementSystem _buildingPlacementController;
    private FactionVisualSettings _factionVisualSettings;
    private BattleHudGameplayBridge _battleHudBridge;
    private World _queryWorld;
    private EntityQuery _selectRectangleQuery;
    private EntityQuery _selectedMoveQuery;
    private EntityQuery _gridPathingQuery;
    private EntityQuery _allSelectableQuery;
    private EntityQuery _gridConfigQuery;
    private EntityQuery _gridBlockerQuery;
    private EntityQuery _selectedTagQuery;
    private EntityQuery _respawnQueueQuery;
    private EntityQuery _focusableUnitsQuery;
    private EntityQuery _selectedAttackQuery;
    private EntityQuery _transportBoardingTargetQuery;
    private EntityQuery _pathingLiveUnitsQuery;
    private EntityQuery _changedFocusableGridQuery;
    private EntityQuery _changedFocusableFootprintQuery;
    private readonly List<PreservedOrderState> _preservedUiOrders = new();
    private readonly Dictionary<int, List<Entity>> _focusableUnitsByCell = new();
    private readonly Dictionary<Entity, FocusableUnitCoverage> _focusableUnitCoverage = new();
    private readonly List<SelectionUiQuerySystem.TransportPassengerUiInfo> _selectionUiPassengerScratch = new();
    private readonly List<Entity> _selectedBoardingSourceEntities = new();
    private GameObject _moveOrderMarker;
    private Renderer[] _moveOrderMarkerRenderers;
    private MaterialPropertyBlock _moveOrderMarkerPropertyBlock;
    private float _moveOrderMarkerHideTime = -1f;
    private GameObject _attackOrderMarker;
    private Renderer[] _attackOrderMarkerRenderers;
    private MaterialPropertyBlock _attackOrderMarkerPropertyBlock;
    private float _attackOrderMarkerHideTime = -1f;
    private int _lastFocusableUnitCount = -1;
    private Transform _runtimeRoot;
    private bool _explicitAttackTargetModeActive;
    private float _selectionModeHoldSeconds = 1f;
    private Rect _lastLiveSelectionRect
    {
        get => _rtsSelectionInputSystem.LastLiveSelectionRect;
        set => _rtsSelectionInputSystem.LastLiveSelectionRect = value;
    }

    private bool _hasLiveSelectionRect
    {
        get => _rtsSelectionInputSystem.HasLiveSelectionRect;
        set => _rtsSelectionInputSystem.HasLiveSelectionRect = value;
    }

    public bool HasFocusedUnit
    {
        get
        {
            if (_focusedUnit == Entity.Null || World.DefaultGameObjectInjectionWorld == null)
                return false;

            var em = World.DefaultGameObjectInjectionWorld.EntityManager;
            return _selectionUiQuerySystem.HasFocusedUnit(em, _focusedUnit);
        }
    }

    public bool HasAnySelectedUnits
    {
        get
        {
            if (World.DefaultGameObjectInjectionWorld == null)
                return false;

            var em = World.DefaultGameObjectInjectionWorld.EntityManager;
            EnsureEntityQueries(em);
            return _selectionUiQuerySystem.HasAnySelectedUnits(_selectedTagQuery);
        }
    }

    public string FocusedUnitLabel
    {
        get
        {
            if (!TryGetFocusedUnitEntity(out var em, out Entity entity))
                return "Unit";

            return _selectionUiQuerySystem.ResolveFocusedUnitName(em, entity);
        }
    }

    public string FocusedUnitDescription
    {
        get
        {
            if (!TryGetFocusedUnitEntity(out var em, out Entity entity))
                return "Select a unit to inspect it.";

            return _selectionUiQuerySystem.ResolveFocusedUnitDescription(em, entity);
        }
    }

    public string FocusedUnitHealthText
    {
        get
        {
            return TryGetFocusedUnitEntity(out var em, out Entity entity)
                ? _selectionUiQuerySystem.ResolveFocusedUnitHealthText(em, entity)
                : "Health: -";
        }
    }

    public bool TryGetFocusedUnitHealth(out int current, out int max)
    {
        current = 0;
        max = 0;

        if (!TryGetFocusedUnitEntity(out var em, out Entity entity))
            return false;

        return _selectionUiQuerySystem.TryGetFocusedUnitHealth(em, entity, out current, out max);
    }

    public bool TryGetFocusedUnitCapacityInfo(out int current, out int max, out float progress01)
    {
        current = 0;
        max = 0;
        progress01 = 0f;

        return TryGetFocusedUnitEntity(out var em, out Entity entity) &&
               _selectionUiQuerySystem.TryGetFocusedUnitCapacityInfo(em, entity, Time.time, out current, out max, out progress01);
    }

    public bool FocusedUnitOwnedByPlayer
    {
        get
        {
            if (!TryGetFocusedUnitEntity(out var em, out Entity entity))
                return false;

            return _selectionUiQuerySystem.IsOwnedByPlayer(em, entity);
        }
    }

    public bool CanDestroyFocusedUnit => FocusedUnitOwnedByPlayer;

    public bool CanCommandFocusedUnit => HasFocusedUnit && FocusedUnitOwnedByPlayer;

    public bool FocusedUnitIsVehicle
    {
        get
        {
            if (!TryGetFocusedUnitEntity(out var em, out Entity entity))
                return false;

            return _selectionUiQuerySystem.IsVehicleUnit(em, entity);
        }
    }

    public bool CanReturnFocusedUnitToBase => CanCommandFocusedUnit && !FocusedUnitIsVehicle;

    public bool CanFocusedUnitUseAutoAttack => CanCommandFocusedUnit && !FocusedUnitIsVehicle;

    public bool FocusedUnitCanAttack
    {
        get
        {
            if (!TryGetFocusedUnitEntity(out var em, out Entity entity))
                return false;

            return _selectionUiQuerySystem.CanAttack(em, entity);
        }
    }

    public bool ExplicitAttackTargetModeActive => _explicitAttackTargetModeActive;

    public int FocusedTransportPassengerCount
    {
        get
        {
            if (!TryGetFocusedUnitEntity(out var em, out Entity entity))
                return 0;

            return _selectionUiQuerySystem.GetTransportPassengerCount(em, entity, _unitTransportBoardingSystem);
        }
    }

    public bool CanDisembarkFocusedTransport => FocusedTransportPassengerCount > 0;

    public void GetFocusedTransportPassengers(List<TransportPassengerUiInfo> results)
    {
        if (results == null)
            return;

        if (TryGetFocusedUnitEntity(out var em, out Entity transport))
        {
            _selectionUiPassengerScratch.Clear();
            _selectionUiQuerySystem.GetTransportPassengers(em, transport, _unitTransportBoardingSystem, _selectionUiPassengerScratch);
            results.Clear();
            for (int i = 0; i < _selectionUiPassengerScratch.Count; i++)
            {
                SelectionUiQuerySystem.TransportPassengerUiInfo passenger = _selectionUiPassengerScratch[i];
                results.Add(new TransportPassengerUiInfo(passenger.Entity, passenger.DisplayName, passenger.HealthCurrent, passenger.HealthMax));
            }
        }
        else
        {
            results.Clear();
        }
    }

    public void DisembarkFocusedTransport()
    {
        if (!TryGetFocusedUnitEntity(out var em, out Entity transport) ||
            !_unitTransportBoardingSystem.TryEnsureTransportCapacity(em, transport) ||
            !em.HasComponent<UnitGrid>(transport) ||
            !em.HasComponent<UnitFootprint>(transport))
        {
            return;
        }

        EnsureEntityQueries(em);
        if (_gridPathingQuery.IsEmptyIgnoreFilter)
            return;

        Entity gridEntity = _gridPathingQuery.GetSingletonEntity();
        GridConfig grid = em.GetComponentData<GridConfig>(gridEntity);
        var walkable = em.GetBuffer<GridWalkable>(gridEntity).AsNativeArray();
        var blocked = em.GetComponentData<DynamicBlockerData>(gridEntity).Blocked;
        var occupied = em.GetComponentData<DynamicOccupancyData>(gridEntity).Occupied;
        DynamicBuffer<UnitTransportPassengerElement> passengers = em.GetBuffer<UnitTransportPassengerElement>(transport);

        int2 transportCell = em.GetComponentData<UnitGrid>(transport).Cell;
        int2 transportSize = em.GetComponentData<UnitFootprint>(transport).Size;
        int2 referenceCell = transportCell;
        if (em.HasComponent<LocalTransform>(transport))
            referenceCell = GridUtils.WorldToCell(grid, em.GetComponentData<LocalTransform>(transport).Position);

        if (_unitTransportBoardingSystem.IsRopeDisembarkTransport(em, transport))
        {
            _unitTransportBoardingSystem.StartRopeDisembarkTransport(em, transport, referenceCell, _unitMoveOrderSystem);
            return;
        }

        List<Entity> passengerSnapshot = new(passengers.Length);
        for (int i = 0; i < passengers.Length; i++)
            passengerSnapshot.Add(passengers[i].Passenger);
        passengers.Clear();

        HashSet<int> reservedCells = new();
        List<Entity> remainingPassengers = new();
        List<Entity> disembarkingPassengers = new();
        List<int2> disembarkCells = new();
        for (int i = 0; i < passengerSnapshot.Count; i++)
        {
            Entity passenger = passengerSnapshot[i];
            if (!em.Exists(passenger))
                continue;

            if (!_unitTransportBoardingSystem.TryFindTransportDisembarkCell(grid, walkable, blocked, occupied, reservedCells, transportCell, transportSize, referenceCell, out int2 cell))
            {
                remainingPassengers.Add(passenger);
                continue;
            }

            int cellIndex = GridUtils.CellToIndex(cell, grid.Width);
            reservedCells.Add(cellIndex);
            disembarkingPassengers.Add(passenger);
            disembarkCells.Add(cell);
        }

        for (int i = 0; i < disembarkingPassengers.Count; i++)
        {
            Entity passenger = disembarkingPassengers[i];
            int2 cell = disembarkCells[i];
            if (!em.Exists(passenger))
                continue;

            if (em.HasComponent<Disabled>(passenger))
                em.RemoveComponent<Disabled>(passenger);
            if (em.HasComponent<UnitTransportPassenger>(passenger))
                em.RemoveComponent<UnitTransportPassenger>(passenger);
            if (em.HasComponent<UnitTransportBoardingTarget>(passenger))
                em.RemoveComponent<UnitTransportBoardingTarget>(passenger);
            _unitMoveOrderSystem.ClearMovementOrderComponents(em, passenger);

            if (em.HasComponent<UnitGrid>(passenger))
                em.SetComponentData(passenger, new UnitGrid { Cell = cell });
            if (em.HasComponent<LocalTransform>(passenger))
            {
                LocalTransform transform = em.GetComponentData<LocalTransform>(passenger);
                transform.Position = GridUtils.CellToWorldCenter(grid, cell);
                em.SetComponentData(passenger, transform);
            }
            UnitTransportVisualUtility.SetPassengerVisible(em, passenger, true);
        }

        if (remainingPassengers.Count > 0 && em.Exists(transport) && em.HasBuffer<UnitTransportPassengerElement>(transport))
        {
            DynamicBuffer<UnitTransportPassengerElement> remainingBuffer = em.GetBuffer<UnitTransportPassengerElement>(transport);
            for (int i = 0; i < remainingPassengers.Count; i++)
            {
                Entity passenger = remainingPassengers[i];
                if (em.Exists(passenger))
                    remainingBuffer.Add(new UnitTransportPassengerElement { Passenger = passenger });
            }
        }
    }

    public bool TryGetFocusedUnitWorldPosition(out Vector3 worldPosition)
    {
        worldPosition = default;
        if (!TryGetFocusedUnitEntity(out var em, out Entity entity))
            return false;

        return _selectionUiQuerySystem.TryGetFocusedUnitWorldPosition(em, entity, out worldPosition);
    }

    public bool TryGetFocusedUnitEntityForUi(out Entity entity)
    {
        return TryGetFocusedUnitEntity(out _, out entity);
    }

    public FocusedUnitUiStatus GetFocusedUnitUiStatus()
    {
        return TryGetFocusedUnitEntity(out var em, out Entity entity)
            ? ToFocusedUnitUiStatus(_selectionUiQuerySystem.GetFocusedUnitUiStatus(em, entity))
            : FocusedUnitUiStatus.Idle;
    }

    public bool TryGetFocusedUnitPortraitPose(out Vector3 worldPosition, out Vector3 forward)
    {
        worldPosition = default;
        forward = Vector3.forward;

        return TryGetFocusedUnitEntity(out var em, out Entity entity) &&
               _selectionUiQuerySystem.TryGetFocusedUnitPortraitPose(em, entity, out worldPosition, out forward);
    }

    public bool TryGetSelectedUnitsPortraitPose(out Vector3 centerWorldPosition, out Vector3 forward, out float framingRadius)
    {
        centerWorldPosition = default;
        forward = Vector3.forward;
        framingRadius = 1f;

        if (World.DefaultGameObjectInjectionWorld == null)
            return false;

        var em = World.DefaultGameObjectInjectionWorld.EntityManager;
        EnsureEntityQueries(em);
        using var selectedEntities = _selectedTagQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
        return _selectionUiQuerySystem.TryGetSelectedUnitsPortraitPose(
            em,
            selectedEntities,
            _focusedUnit,
            out centerWorldPosition,
            out forward,
            out framingRadius);
    }

    public void GetSelectedUnitEntities(List<Entity> entities)
    {
        if (entities == null)
            return;

        if (World.DefaultGameObjectInjectionWorld == null)
        {
            entities.Clear();
            return;
        }

        var em = World.DefaultGameObjectInjectionWorld.EntityManager;
        EnsureEntityQueries(em);
        using var selectedEntities = _selectedTagQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
        _selectionUiQuerySystem.GetSelectedUnitEntities(em, selectedEntities, entities);
    }

    private static FocusedUnitUiStatus ToFocusedUnitUiStatus(SelectionUiQuerySystem.FocusedUnitUiStatus status)
    {
        return status switch
        {
            SelectionUiQuerySystem.FocusedUnitUiStatus.Moving => FocusedUnitUiStatus.Moving,
            SelectionUiQuerySystem.FocusedUnitUiStatus.Engaged => FocusedUnitUiStatus.Engaged,
            SelectionUiQuerySystem.FocusedUnitUiStatus.ReturningToBase => FocusedUnitUiStatus.ReturningToBase,
            _ => FocusedUnitUiStatus.Idle
        };
    }

    private void OnValidate()
    {
        ApplyConfigIfAvailable();
    }

    public void Init(
        RTSSelectionSystemConfig configAsset,
        Camera sceneWorldCamera,
        Transform runtimeRoot,
        MainMenuPlayUI mainMenuPlayUi,
        RoadBuildSystem roadBuildController,
        BuildingPlacementSystem buildingPlacementController,
        FactionVisualSettings factionVisualSettings)
    {
        config = configAsset;
        worldCamera = sceneWorldCamera;
        _runtimeRoot = runtimeRoot;
        _mainMenuPlayUi = mainMenuPlayUi;
        _roadBuildController = roadBuildController;
        _buildingPlacementController = buildingPlacementController;
        _factionVisualSettings = factionVisualSettings;
        _battleHudBridge = null;
        ApplyConfigIfAvailable();

        if (panSensitivity <= 0f)
            panSensitivity = DefaultPanSensitivity;
        if (zoomSpeed <= 0f)
            zoomSpeed = DefaultZoomSpeed;
        if (minZoomHeight <= 0f)
            minZoomHeight = DefaultMinZoomHeight;
        if (maxZoomHeight <= minZoomHeight)
            maxZoomHeight = Mathf.Max(DefaultMaxZoomHeight, minZoomHeight + 1f);
        if (normalModeZoomHeight <= 0f)
            normalModeZoomHeight = 24f;
        normalModeZoomHeight = Mathf.Min(normalModeZoomHeight, maxZoomHeight);
        if (buildModeZoomHeight < normalModeZoomHeight)
            buildModeZoomHeight = normalModeZoomHeight;
        buildModeZoomHeight = Mathf.Min(buildModeZoomHeight, maxZoomHeight);
        if (normalModeFieldOfView <= 1f)
            normalModeFieldOfView = 36f;
        if (buildModeFieldOfView <= 1f)
            buildModeFieldOfView = normalModeFieldOfView;
        if (zoomTransitionSmoothTime <= 0f)
            zoomTransitionSmoothTime = 0.25f;

        _pixel = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        _pixel.SetPixel(0, 0, Color.white);
        _pixel.Apply();

        CacheMoveOrderMarker();
        CacheAttackOrderMarker();
    }

    public void BindDependencies(MainMenuPlayUI mainMenuPlayUi, RoadBuildSystem roadBuildController, BuildingPlacementSystem buildingPlacementController)
    {
        _mainMenuPlayUi = mainMenuPlayUi;
        _roadBuildController = roadBuildController;
        _buildingPlacementController = buildingPlacementController;
    }

    private void ApplyConfigIfAvailable()
    {
        if (config == null)
            return;

        if (config.WorldCamera != null)
            worldCamera = config.WorldCamera;
        moveOrderMarkerPrefab = config.MoveOrderMarkerPrefab;
        orderMarkerVisibleSeconds = Mathf.Max(0.01f, config.OrderMarkerVisibleSeconds);
        attackOrderMarkerPrefab = config.AttackOrderMarkerPrefab;
        selectionFill = config.SelectionFill;
        selectionBorder = config.SelectionBorder;
        dragThresholdPixels = config.DragThresholdPixels;
        _selectionModeHoldSeconds = Mathf.Max(0.1f, config.SelectionModeHoldSeconds);
        panSensitivity = config.PanSensitivity;
        zoomSpeed = config.ZoomSpeed;
        minZoomHeight = config.MinZoomHeight;
        maxZoomHeight = config.MaxZoomHeight;
        normalModeZoomHeight = config.NormalModeZoomHeight;
        buildModeZoomHeight = config.BuildModeZoomHeight;
        normalModePitch = config.NormalModePitch;
        buildModePitch = config.BuildModePitch;
        normalModeYaw = config.NormalModeYaw;
        buildModeYaw = config.BuildModeYaw;
        normalModeFieldOfView = config.NormalModeFieldOfView;
        buildModeFieldOfView = config.BuildModeFieldOfView;
        fullscreenIsoZoomHeight = config.FullscreenIsoZoomHeight;
        fullscreenIsoPitch = config.FullscreenIsoPitch;
        fullscreenIsoYaw = config.FullscreenIsoYaw;
        fullscreenIsoOrthographicSize = config.FullscreenIsoOrthographicSize;
        zoomTransitionSmoothTime = config.ZoomTransitionSmoothTime;
    }

    private BattleHudGameplayBridge ResolveBattleHudBridge()
    {
        if (_battleHudBridge != null)
            return _battleHudBridge;

        _battleHudBridge = BattleHudGameplayBridge.ResolveActive();
        return _battleHudBridge;
    }

    private void ApplyHudSelection(EntityManager em, Entity entity)
    {
        if (entity == Entity.Null || !em.Exists(entity))
        {
            ClearHudSelection();
            return;
        }

        BattleHudGameplayBridge bridge = ResolveBattleHudBridge();
        if (bridge == null)
            return;

        bridge.ApplySelection(
            _selectionUiQuerySystem.ResolveFocusedUnitName(em, entity),
            _selectionUiQuerySystem.ResolveHudSelectionStatus(em, entity));
    }

    private void ApplyHudSquadSelection(int selectedCount)
    {
        BattleHudGameplayBridge bridge = ResolveBattleHudBridge();
        if (bridge == null)
            return;

        if (selectedCount <= 0)
        {
            bridge.ClearSelection();
            return;
        }

        string unitLabel = selectedCount == 1 ? "UNIT" : "UNITS";
        bridge.ApplySelection($"{selectedCount} {unitLabel}", "SQUAD SELECTED");
    }

    private void ClearHudSelection()
    {
        ResolveBattleHudBridge()?.ClearSelection();
    }

    private void ApplyHudCommandMode(TacticalCommandMode mode)
    {
        ResolveBattleHudBridge()?.ApplyCommandMode(mode);
    }

    private void ClearHudCommandMode()
    {
        ResolveBattleHudBridge()?.ClearCommandMode();
    }

    private void ApplyHudCommandResult(TacticalCommandResult result)
    {
        ResolveBattleHudBridge()?.ApplyCommandResult(result);
    }

    private void SetHudWorldMarkersVisible(bool visible)
    {
        ResolveBattleHudBridge()?.SetWorldMarkersVisible(visible);
    }

    public void Dispose()
    {
        if (_pixel != null)
            Destroy(_pixel);
        if (_moveOrderMarker != null && moveOrderMarkerPrefab != null)
            Destroy(_moveOrderMarker);
    }

    private void EnsureEntityQueries(EntityManager em)
    {
        World world = em.World;
        if (_queryWorld == world && world != null && world.IsCreated)
            return;

        _queryWorld = world;
        _focusableUnitsByCell.Clear();
        _focusableUnitCoverage.Clear();
        _lastFocusableUnitCount = -1;
        _selectRectangleQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<Faction>(),
            ComponentType.ReadOnly<LocalToWorld>(),
            ComponentType.ReadOnly<UnitGrid>());
        _selectedMoveQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<SelectedUnitTag>(),
            ComponentType.ReadOnly<UnitGrid>(),
            ComponentType.ReadOnly<UnitMove>());
        _gridPathingQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<GridConfig>(),
            ComponentType.ReadOnly<GridWalkable>(),
            ComponentType.ReadOnly<DynamicBlockerData>(),
            ComponentType.ReadOnly<DynamicOccupancyData>());
        _allSelectableQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<Faction>(),
            ComponentType.ReadOnly<UnitGrid>(),
            ComponentType.ReadOnly<LocalToWorld>());
        _gridConfigQuery = em.CreateEntityQuery(ComponentType.ReadOnly<GridConfig>());
        _gridBlockerQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<GridConfig>(),
            ComponentType.ReadOnly<GridWalkable>(),
            ComponentType.ReadOnly<DynamicBlockerData>());
        _selectedTagQuery = em.CreateEntityQuery(ComponentType.ReadOnly<SelectedUnitTag>());
        _respawnQueueQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<RespawnQueueTag>(),
            ComponentType.ReadOnly<RespawnQueueState>());
        _focusableUnitsQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<Faction>(),
            ComponentType.ReadOnly<UnitGrid>(),
            ComponentType.ReadOnly<UnitFootprint>(),
            ComponentType.ReadOnly<LocalToWorld>());
        _selectedAttackQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<SelectedUnitTag>(),
            ComponentType.ReadOnly<UnitMove>(),
            ComponentType.ReadOnly<UnitCombat>(),
            ComponentType.ReadOnly<UnitAttack>(),
            ComponentType.ReadOnly<LocalTransform>());
        _transportBoardingTargetQuery = em.CreateEntityQuery(ComponentType.ReadOnly<UnitTransportBoardingTarget>());
        _pathingLiveUnitsQuery = em.CreateEntityQuery(new EntityQueryDesc
        {
            All = new[]
            {
                ComponentType.ReadOnly<UnitGrid>(),
                ComponentType.ReadOnly<UnitFootprint>(),
            },
            None = new[]
            {
                ComponentType.ReadOnly<StaticGridBlocker>(),
                ComponentType.ReadOnly<RuntimeBuildingCombatTag>(),
            }
        });
        _changedFocusableGridQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<Faction>(),
            ComponentType.ReadOnly<UnitGrid>(),
            ComponentType.ReadOnly<UnitFootprint>(),
            ComponentType.ReadOnly<LocalToWorld>());
        _changedFocusableGridQuery.SetChangedVersionFilter(ComponentType.ReadOnly<UnitGrid>());
        _changedFocusableFootprintQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<Faction>(),
            ComponentType.ReadOnly<UnitGrid>(),
            ComponentType.ReadOnly<UnitFootprint>(),
            ComponentType.ReadOnly<LocalToWorld>());
        _changedFocusableFootprintQuery.SetChangedVersionFilter(ComponentType.ReadOnly<UnitFootprint>());
    }

    public void Update()
    {
        ProcessQueuedMoveOrder();
        RefreshFocusedUnit();
        UpdateMoveOrderMarkerVisibility();
        UpdateAttackOrderMarkerVisibility();

        if (!_runtimeGameplayStateSystem.PlayRequested)
        {
            _rtsCameraSystem.ResetSession();
            _rtsCameraSystem.ResetCameraModeSession();
            _runtimeGameplayStateSystem.FullscreenMapOpen = false;
            _runtimeGameplayStateSystem.FullscreenMapIsoMode = false;
            _runtimeGameplayStateSystem.InitialCameraFocusRequested = false;
            return;
        }

        if (_runtimeGameplayStateSystem.FullscreenMapIsoMode)
        {
            if (worldCamera == null)
                return;

            UpdateFullscreenIsoZoom();
            UpdateFullscreenIsoCameraMode(_fullscreenIsoTargetHeight, _fullscreenIsoTargetOrthographicSize, fullscreenIsoPitch, fullscreenIsoYaw);
            HandleFullscreenIsoCameraPan();
            return;
        }

        if (_runtimeGameplayStateSystem.FullscreenMapOpen)
            return;

        if (_runtimeGameplayStateSystem.BuildModeActive)
        {
            if (_normalIsoModeActive)
                ExitNormalIsoMode();
            UpdateBuildModeCameraTransition();
            UpdateSmoothCameraFocus();
            HandleBuildModeCameraPan();
            return;
        }

        if (worldCamera == null)
            return;

        if (_normalIsoModeActive)
        {
            UpdateFullscreenIsoZoom();
            UpdateFullscreenIsoCameraMode(_fullscreenIsoTargetHeight, _fullscreenIsoTargetOrthographicSize, fullscreenIsoPitch, fullscreenIsoYaw);
        }
        else
        {
            SyncCameraZoomModeState();
            ConsumeInitialCameraFocusRequest();
            UpdateZoom();
        }

        UpdateSmoothCameraFocus();

        if (!GamePointerInput.TryGetPrimaryPointer(out GamePointerState pointer))
            return;

        if (_ignoreUiClickUntilRelease)
        {
            if (pointer.WasReleasedThisFrame || !pointer.IsPressed)
            {
                _ignoreUiClickUntilRelease = false;
                _ignoreNextLeftMouseRelease = false;
                _skipNextWorldReleaseAfterSelection = false;
            }
            return;
        }

        if (Time.frameCount <= _ignoreWorldCommandsUntilFrame)
            return;

        Vector2 pointerPosition = pointer.Position;
        UpdateLastKnownPointerPosition(pointerPosition);
        UpdateSelectionModeHold(pointer.IsPressed, pointerPosition);

        if (pointer.WasReleasedThisFrame && _ignoreNextLeftMouseRelease)
        {
            _ignoreNextLeftMouseRelease = false;
            _skipNextWorldReleaseAfterSelection = false;
            _runtimeGameplayStateSystem.SuppressNextWorldClick = false;
            if (_runtimeGameplayStateSystem.SelectionModeActive && (_dragging || _hasLiveSelectionRect))
                _runtimeGameplayStateSystem.SelectionModeActive = false;
            _dragging = false;
            _cameraDragging = false;
            _selectionModeHoldArmed = false;
            _lastPointerPosition = pointerPosition;
            return;
        }

        if (pointer.WasPressedThisFrame)
        {
            if (_mainMenuPlayUi != null && _mainMenuPlayUi.IsPointerOverSelectionCancelUi(pointerPosition))
            {
                _mainMenuPlayUi.TriggerSelectionCancel();
                _pointerPressedOverUi = true;
                _dragging = false;
                _cameraDragging = false;
                _lastPointerPosition = pointerPosition;
                return;
            }

            bool pointerOverAnyUi = IsPointerOverUI(pointerPosition, out string anyUiSource);
            bool pointerOverGameplayUi = IsPointerOverGameplayUi(pointerPosition, out string gameplayUiSource);
            bool pointerOverBlockingUi = _runtimeGameplayStateSystem.PlayRequested ? pointerOverGameplayUi : (pointerOverAnyUi || pointerOverGameplayUi);
            _rtsSelectionInputSystem.BeginPointerPress(pointerPosition, !_runtimeGameplayStateSystem.PlayRequested && pointerOverBlockingUi);
            _cameraDragging = false;

            if (_explicitAttackTargetModeActive && !_pointerPressedOverUi)
            {
                if (TryIssueAttackOrderToClickedUnit(pointerPosition))
                    _explicitAttackTargetModeActive = false;

                _skipNextWorldReleaseAfterSelection = true;
                _runtimeGameplayStateSystem.SuppressNextWorldClick = true;
                _lastPointerPosition = pointerPosition;
                return;
            }

            if (!_runtimeGameplayStateSystem.SelectionModeActive)
            {
                if (!_pointerPressedOverUi)
                {
                    if (TryIssueAttackOrderToClickedUnit(pointerPosition))
                    {
                        _runtimeGameplayStateSystem.SuppressNextWorldClick = true;
                    }
                    else if (TryIssueBoardTransportOrderToClickedUnit(pointerPosition))
                    {
                        _runtimeGameplayStateSystem.SuppressNextWorldClick = true;
                    }
                    else if (TryFocusUnit(pointerPosition))
                    {
                        _skipNextWorldReleaseAfterSelection = true;
                        _runtimeGameplayStateSystem.SuppressNextWorldClick = true;
                    }
                    else
                    {
                        _cameraDragging = true;
                        ArmSelectionModeHold();
                    }
                }
                else
                {
                    _cameraDragging = true;
                }
            }
        }

        if (pointer.IsPressed)
        {
            Vector2 frameDelta = pointerPosition - _lastPointerPosition;
            _dragCurrent = pointerPosition;
            float dragDistance = Vector2.Distance(_dragStart, _dragCurrent);

            if (_runtimeGameplayStateSystem.SelectionModeActive)
            {
                if (!_dragging && dragDistance >= dragThresholdPixels)
                    _dragging = true;

                if (_dragging)
                {
                    Rect liveRect = GetScreenRect(_dragStart, _dragCurrent);
                    if (!_hasLiveSelectionRect || !ApproximatelyEqualRect(_lastLiveSelectionRect, liveRect))
                    {
                        SelectUnitsInRectangle(liveRect);
                        _lastLiveSelectionRect = liveRect;
                        _hasLiveSelectionRect = true;
                    }
                }
            }
            else if (_cameraDragging && frameDelta.sqrMagnitude > 0f)
            {
                PanCamera(frameDelta);
            }

            if (dragDistance >= dragThresholdPixels)
                _selectionModeHoldArmed = false;

            _lastPointerPosition = pointerPosition;
        }

        if (pointer.WasReleasedThisFrame)
        {
            bool releasePointerOverAnyUi = IsPointerOverUI(pointerPosition, out string releaseAnyUiSource);
            bool releasePointerOverGameplayUi = IsPointerOverGameplayUi(pointerPosition, out string releaseGameplayUiSource);
            bool releasePointerOverBlockingUi = _runtimeGameplayStateSystem.PlayRequested ? releasePointerOverGameplayUi : (releasePointerOverAnyUi || releasePointerOverGameplayUi);

            if (_pointerPressedOverUi || releasePointerOverBlockingUi)
            {
                _pointerPressedOverUi = false;
                _dragging = false;
                _cameraDragging = false;
                _selectionModeHoldArmed = false;
                _hasLiveSelectionRect = false;
                return;
            }

            if (_skipNextWorldReleaseAfterSelection)
            {
                _skipNextWorldReleaseAfterSelection = false;
                _runtimeGameplayStateSystem.SuppressNextWorldClick = false;
                _dragging = false;
                _cameraDragging = false;
                _selectionModeHoldArmed = false;
                _hasLiveSelectionRect = false;
                return;
            }

            if (_runtimeGameplayStateSystem.SelectionModeActive)
            {
                if (_dragging)
                {
                    if (!_hasLiveSelectionRect)
                        SelectUnitsInRectangle(GetScreenRect(_dragStart, _dragCurrent));
                }
                else if (!releasePointerOverBlockingUi)
                {
                    TryFocusUnit(pointerPosition);
                }

                _runtimeGameplayStateSystem.SelectionModeActive = false;
                _runtimeGameplayStateSystem.SuppressNextWorldClick = false;
            }
            else if (Vector2.Distance(_dragStart, _dragCurrent) < dragThresholdPixels)
            {
                if (_runtimeGameplayStateSystem.SuppressNextWorldClick)
                {
                    _runtimeGameplayStateSystem.SuppressNextWorldClick = false;
                }
                else if (!releasePointerOverBlockingUi)
                {
                    QueueMoveOrder(pointerPosition);
                }
            }

            _dragging = false;
            _cameraDragging = false;
            _pointerPressedOverUi = false;
            _selectionModeHoldArmed = false;
            _hasLiveSelectionRect = false;
        }
    }

    private static bool ApproximatelyEqualRect(Rect a, Rect b)
    {
        return Mathf.Abs(a.x - b.x) < 0.5f &&
               Mathf.Abs(a.y - b.y) < 0.5f &&
               Mathf.Abs(a.width - b.width) < 0.5f &&
               Mathf.Abs(a.height - b.height) < 0.5f;
    }

    private void QueueMoveOrder(Vector2 screenPosition)
    {
        _rtsSelectionInputSystem.QueueMoveOrder(screenPosition, Time.frameCount + 1);
    }

    private void ArmSelectionModeHold()
    {
        _rtsSelectionInputSystem.ArmSelectionModeHold(Time.unscaledTime);
    }

    private void UpdateSelectionModeHold(bool pointerPressed, Vector2 pointerPosition)
    {
        if (!_selectionModeHoldArmed)
            return;

        if (!pointerPressed)
        {
            _selectionModeHoldArmed = false;
            return;
        }

        if (_runtimeGameplayStateSystem.SelectionModeActive)
        {
            _selectionModeHoldArmed = false;
            return;
        }

        if (_mainMenuPlayUi == null || !_mainMenuPlayUi.CanTriggerSelectionModeFromHold())
        {
            _selectionModeHoldArmed = false;
            return;
        }

        if (_mainMenuPlayUi.IsPointerOverZoomControls(pointerPosition))
        {
            _selectionModeHoldArmed = false;
            return;
        }

        if (Vector2.Distance(_dragStart, pointerPosition) >= dragThresholdPixels)
        {
            _selectionModeHoldArmed = false;
            return;
        }

        if (Time.unscaledTime - _selectionModeHoldStartTime < _selectionModeHoldSeconds)
            return;

        _selectionModeHoldArmed = false;
        _pointerPressedOverUi = false;
        _dragging = false;
        _cameraDragging = false;
        _ignoreNextLeftMouseRelease = true;
        _mainMenuPlayUi.TriggerSelectionModeFromHold();
    }

    private void ProcessQueuedMoveOrder()
    {
        if (!_rtsSelectionInputSystem.TryConsumeQueuedMoveOrder(Time.frameCount, out Vector2 screenPosition))
            return;

        if (!_runtimeGameplayStateSystem.PlayRequested || _runtimeGameplayStateSystem.BuildModeActive)
            return;

        if (_runtimeGameplayStateSystem.SuppressNextWorldClick)
            return;

        IssueMoveOrder(screenPosition);
    }

    public void OnGui()
    {
        if (!_dragging || !_runtimeGameplayStateSystem.PlayRequested || !_runtimeGameplayStateSystem.SelectionModeActive)
            return;

        var rect = GetGuiRect(_dragStart, _dragCurrent);
        DrawRect(rect, selectionFill);
        DrawBorder(rect, 2f, selectionBorder);
    }

    private void HandleBuildModeCameraPan()
    {
        if (worldCamera == null)
            return;

        if (!GamePointerInput.TryGetPrimaryPointer(out GamePointerState pointer))
            return;

        Vector2 pointerPosition = pointer.Position;
        UpdateLastKnownPointerPosition(pointerPosition);
        bool pointerOverGameplayUi = IsPointerOverGameplayUi(pointerPosition, out _);
        bool pointerOverBuildToolMenu = _mainMenuPlayUi != null && _mainMenuPlayUi.IsPointerOverBuildToolMenu(pointerPosition);
        bool hasPendingBuildingPlacement = _buildingPlacementController != null && _buildingPlacementController.HasPendingBuildingPlacement;
        bool roadToolActive = _roadBuildController != null && _roadBuildController.IsRoadBuildModeActive;
        bool idleBuildMode = !hasPendingBuildingPlacement && !roadToolActive;
        bool interactionActive =
            (_roadBuildController != null && _roadBuildController.IsDraggingBuildInteraction) ||
            (_buildingPlacementController != null && _buildingPlacementController.IsDraggingPlacementPreview);

        if (pointerOverGameplayUi)
        {
            _cameraDragging = false;
            _dragging = false;
            return;
        }

        bool panPressed = idleBuildMode && pointer.WasPressedThisFrame;
        bool panHeld = idleBuildMode && pointer.IsPressed;
        bool panReleased = idleBuildMode && pointer.WasReleasedThisFrame;

        if (panPressed)
        {
            _lastPointerPosition = pointerPosition;
            _cameraDragging = !interactionActive && !pointerOverBuildToolMenu;
        }

        if (panHeld && _cameraDragging)
        {
            Vector2 frameDelta = pointerPosition - _lastPointerPosition;
            if (frameDelta.sqrMagnitude > 0f)
                PanCamera(frameDelta);
            _lastPointerPosition = pointerPosition;
        }

        if (panReleased || !panHeld)
            _cameraDragging = false;

        _dragging = false;
    }

    private void SelectUnitsInRectangle(Rect screenRect)
    {
        SelectUnitsInRectangle(screenRect, VisibleUnitSelectionFilter.All);
    }

    public bool HasVisiblePlayerUnits()
    {
        return HasVisiblePlayerUnits(VisibleUnitSelectionFilter.All);
    }

    public bool HasVisiblePlayerSoldiers()
    {
        return HasVisiblePlayerUnits(VisibleUnitSelectionFilter.Soldiers);
    }

    public bool HasVisiblePlayerVehicles()
    {
        return HasVisiblePlayerUnits(VisibleUnitSelectionFilter.Vehicles);
    }

    private bool HasVisiblePlayerUnits(VisibleUnitSelectionFilter filter)
    {
        if (worldCamera == null || World.DefaultGameObjectInjectionWorld == null)
            return false;

        var em = World.DefaultGameObjectInjectionWorld.EntityManager;
        EnsureEntityQueries(em);
        using var entities = _selectRectangleQuery.ToEntityArray(Unity.Collections.Allocator.Temp);

        Rect screenRect = new(0f, 0f, Screen.width, Screen.height);
        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            if (em.HasComponent<Prefab>(entity) || em.HasComponent<StaticGridBlocker>(entity))
                continue;

            if (em.GetComponentData<Faction>(entity).Id != 0)
                continue;

            bool isVehicle = _selectionUiQuerySystem.IsVehicleForVisibleSelection(em, entity);
            if (filter == VisibleUnitSelectionFilter.Soldiers && isVehicle)
                continue;
            if (filter == VisibleUnitSelectionFilter.Vehicles && !isVehicle)
                continue;

            Vector3 screen = worldCamera.WorldToScreenPoint(em.GetComponentData<LocalToWorld>(entity).Position);
            if (screen.z > 0f && screenRect.Contains(new Vector2(screen.x, screen.y)))
                return true;
        }

        return false;
    }

    private void SelectUnitsInRectangle(Rect screenRect, VisibleUnitSelectionFilter filter)
    {
        if (World.DefaultGameObjectInjectionWorld == null)
            return;

        var em = World.DefaultGameObjectInjectionWorld.EntityManager;
        EnsureEntityQueries(em);
        using var entities = _selectRectangleQuery.ToEntityArray(Unity.Collections.Allocator.Temp);

        var selected = new List<Entity>();
        for (int i = 0; i < entities.Length; i++)
        {
            var entity = entities[i];
            if (em.HasComponent<Prefab>(entity) || em.HasComponent<StaticGridBlocker>(entity))
                continue;

            if (em.GetComponentData<Faction>(entity).Id != 0)
                continue;

            bool isVehicle = _selectionUiQuerySystem.IsVehicleForVisibleSelection(em, entity);
            if (filter == VisibleUnitSelectionFilter.Soldiers && isVehicle)
                continue;
            if (filter == VisibleUnitSelectionFilter.Vehicles && !isVehicle)
                continue;

            float3 pos = em.GetComponentData<LocalToWorld>(entity).Position;
            Vector3 screen = worldCamera.WorldToScreenPoint(pos);
            if (screen.z <= 0f)
                continue;

            if (screenRect.Contains(new Vector2(screen.x, screen.y)))
                selected.Add(entity);
        }

        ClearCurrentSelection(em, "SelectUnitsInRectangle");
        for (int i = 0; i < selected.Count; i++)
        {
            if (!em.HasComponent<SelectedUnitTag>(selected[i]))
                em.AddComponent<SelectedUnitTag>(selected[i]);
        }
        CacheSelectedMoveEntities(em, selected);
        LogSelectionDiagnostic($"result=SelectRectangle filter={filter} selected={selected.Count} cache={_cachedSelectedMoveEntities.Count}");

        _focusedUnit = selected.Count == 1 ? selected[0] : Entity.Null;
        if (_focusedUnit != Entity.Null)
        {
            _buildingPlacementController?.ClearSelectedBuilding("RTSSelection.SelectUnitsInRectangle");
            ApplyHudSelection(em, _focusedUnit);
        }
        else
        {
            ApplyHudSquadSelection(selected.Count);
        }
    }

    private void IssueMoveOrder(Vector2 screenPosition)
    {
        _explicitAttackTargetModeActive = false;
        ApplyHudCommandMode(TacticalCommandMode.Move);

        if (World.DefaultGameObjectInjectionWorld == null)
        {
            ApplyHudCommandResult(TacticalCommandResult.Rejected(TacticalCommandReasonCode.NoSelection));
            ClearHudCommandMode();
            return;
        }

        var em = World.DefaultGameObjectInjectionWorld.EntityManager;
        EnsureEntityQueries(em);
        if (TryGetClickedUnitEntity(screenPosition, em, out _))
        {
            ApplyHudCommandResult(TacticalCommandResult.Rejected(TacticalCommandReasonCode.TargetNotAttackable));
            ClearHudCommandMode();
            return;
        }
        using var entities = _selectedMoveQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
        if (entities.Length == 0)
        {
            ApplyHudCommandResult(TacticalCommandResult.Rejected(TacticalCommandReasonCode.NoSelection));
            ClearHudCommandMode();
            return;
        }

        if (!TryGetClickedCell(screenPosition, em, out var goal, out var clickWorldPoint))
        {
            ApplyHudCommandResult(TacticalCommandResult.Rejected(TacticalCommandReasonCode.TargetNotAttackable));
            ClearHudCommandMode();
            return;
        }

        byte factionId = 0;
        if (em.HasComponent<Faction>(entities[0]))
            factionId = em.GetComponentData<Faction>(entities[0]).Id;
        ShowMoveOrderMarker(em, goal, clickWorldPoint, factionId);
        Entity gridEntity = _gridConfigQuery.GetSingletonEntity();
        GridConfig grid = em.GetComponentData<GridConfig>(gridEntity);
        var walkable = em.GetBuffer<GridWalkable>(gridEntity).AsNativeArray();
        DynamicBlockerData blockerData = em.GetComponentData<DynamicBlockerData>(gridEntity);
        var blocked = blockerData.Blocked;
        var friendlyPassFactionIds = blockerData.FriendlyPassFactionIds;
        var occupied = em.GetComponentData<DynamicOccupancyData>(gridEntity).Occupied;
        var reservedGoalCells = new HashSet<int>();
        var selectedCurrentCells = _unitMoveOrderSystem.BuildSelectedCurrentFootprintCells(em, grid, entities);
        var issuedGoals = new int2[entities.Length];
        var skipIssue = new bool[entities.Length];
        bool issuedMoveOrder = false;
        int pathRequestCount = 0;
        int staggeredPathRequestCount = 0;
        int maxStaggerDelayFrames = 0;
        int skippedAlreadyMovingCount = 0;
        int airUnitCount = 0;
        int structuralAdds = 0;
        int structuralRemoves = 0;
        int uniqueGoalCount = 0;
        int groundPathCandidateCount = 0;
        for (int i = 0; i < entities.Length; i++)
        {
            var entity = entities[i];
            int2 issuedGoal = _unitMoveOrderSystem.FindManualMoveGoal(
                em,
                grid,
                walkable,
                blocked,
                friendlyPassFactionIds,
                occupied,
                reservedGoalCells,
                selectedCurrentCells,
                entity,
                goal,
                i);
            issuedGoals[i] = issuedGoal;

            if (IsAlreadyMovingToGoal(em, entity, issuedGoal))
            {
                skipIssue[i] = true;
                skippedAlreadyMovingCount++;
            }
            else if (!em.HasComponent<UnitAirMovement>(entity))
            {
                groundPathCandidateCount++;
            }
        }

        bool staggerGroundPathRequests = groundPathCandidateCount >= GroupMoveStaggerMinGroundUnits;
        int immediateGroundPathRequests = 0;
        int currentFrame = Time.frameCount;
        for (int i = 0; i < entities.Length; i++)
        {
            if (skipIssue[i])
                continue;

            var entity = entities[i];
            int2 issuedGoal = issuedGoals[i];

            bool groundUnit = !em.HasComponent<UnitAirMovement>(entity);
            bool issuePathNow = groundUnit &&
                                (!staggerGroundPathRequests ||
                                 immediateGroundPathRequests < GroupMoveImmediatePathRequests);
            int resumeFrame = groundUnit && !issuePathNow
                ? currentFrame + 1 + (staggeredPathRequestCount / GroupMovePathRequestsPerFrame)
                : 0;

            UnitMoveOrderSystem.MoveOrderCommandResult commandResult = _unitMoveOrderSystem.IssueGroupedManualMoveOrder(
                em,
                entity,
                issuedGoal,
                issuePathNow,
                groundUnit && !issuePathNow,
                resumeFrame,
                currentFrame);

            structuralAdds += commandResult.StructuralAdds;
            structuralRemoves += commandResult.StructuralRemoves;
            pathRequestCount += commandResult.PathRequests;
            staggeredPathRequestCount += commandResult.StaggeredPathRequests;
            maxStaggerDelayFrames = math.max(maxStaggerDelayFrames, commandResult.MaxStaggerDelayFrames);
            airUnitCount += commandResult.AirUnits;
            if (commandResult.PathRequests > 0)
                immediateGroundPathRequests += commandResult.PathRequests;

            issuedMoveOrder = true;
            uniqueGoalCount++;
        }

        if (issuedMoveOrder)
        {
            if (EnableGroupMoveValidationLog && entities.Length > 1)
            {
                Debug.Log(
                    $"[GroupMoveValidate] selected={entities.Length} ground={groundPathCandidateCount} immediate={pathRequestCount} " +
                    $"staggered={staggeredPathRequestCount} perFrame={GroupMovePathRequestsPerFrame} maxDelayFrames={maxStaggerDelayFrames} " +
                    $"uniqueGoals={uniqueGoalCount} skippedSameGoal={skippedAlreadyMovingCount} air={airUnitCount} goal={goal}");
            }

            if (EnableMoveOrderDiagnostics && entities.Length > 1)
                Debug.Log(
                    $"[MoveOrderDiag] frame={Time.frameCount} selected={entities.Length} pathRequests={pathRequestCount} " +
                    $"airUnits={airUnitCount} skippedSameGoal={skippedAlreadyMovingCount} structuralAdds={structuralAdds} structuralRemoves={structuralRemoves} " +
                    $"uniqueGoals={uniqueGoalCount} staggeredPathRequests={staggeredPathRequestCount} goal={goal}");
            MoveOrderScreenMarkerRequested?.Invoke(screenPosition);
            ApplyHudCommandResult(TacticalCommandResult.Success());
            ClearHudCommandMode();
            SetHudWorldMarkersVisible(true);
        }
        else
        {
            ApplyHudCommandResult(TacticalCommandResult.Rejected(TacticalCommandReasonCode.TargetBlocked));
            ClearHudCommandMode();
        }
    }

    private bool TryIssueBoardTransportOrderToClickedUnit(Vector2 screenPosition)
    {
        if (World.DefaultGameObjectInjectionWorld == null)
            return false;

        var em = World.DefaultGameObjectInjectionWorld.EntityManager;
        EnsureEntityQueries(em);
        if (!TryGetClickedOrNearbyBoardableTransport(screenPosition, em, out Entity transport))
            return false;

        if (!_unitTransportBoardingSystem.IsBoardablePlayerTransport(em, transport))
        {
            LogTransportBoarding($"result=TransportNotBoardable transport={DescribeTransportBoardingEntity(em, transport)} {DescribeTransportAirState(em, transport)}");
            return false;
        }

        bool airTransport = em.HasComponent<UnitAirMovement>(transport);
        bool transportLanded = _unitTransportBoardingSystem.IsTransportLandedForBoarding(em, transport);
        if (!transportLanded && !airTransport)
        {
            LogTransportBoarding($"result=TransportNotLanded transport={DescribeTransportBoardingEntity(em, transport)} {DescribeTransportAirState(em, transport)}");
            return false;
        }

        if (!transportLanded && em.HasComponent<UnitTransportRopeDisembarkRequest>(transport))
        {
            LogTransportBoarding($"result=TransportBusyRopeDisembark transport={DescribeTransportBoardingEntity(em, transport)} {DescribeTransportAirState(em, transport)}");
            return false;
        }

        int capacity = math.max(0, em.GetComponentData<UnitTransportCapacity>(transport).SoldierCapacity);
        int occupiedSeats = em.GetBuffer<UnitTransportPassengerElement>(transport).Length + CountPendingBoardingOrders(em, transport);
        int availableSeats = capacity - occupiedSeats;
        if (availableSeats <= 0)
        {
            LogTransportBoarding($"result=NoSeats transport={DescribeTransportBoardingEntity(em, transport)} seats={occupiedSeats}/{capacity}");
            return false;
        }

        int selectedCount = CollectSelectedBoardingSourceEntities(em, _selectedBoardingSourceEntities, out int selectedTagCount, out int selectedMoveCount, out bool usedCachedSelection);
        if (selectedCount == 0)
        {
            LogTransportBoarding(
                $"result=NoSelectedPassengers transport={DescribeTransportBoardingEntity(em, transport)} seats={occupiedSeats}/{capacity} " +
                $"selectedTag={selectedTagCount} selectedMove={selectedMoveCount} cached={_cachedSelectedMoveEntities.Count}");
            return false;
        }

        if (_gridPathingQuery.IsEmptyIgnoreFilter)
        {
            LogTransportBoarding($"result=NoGridPathing transport={DescribeTransportBoardingEntity(em, transport)} selected={selectedCount} usedCache={(usedCachedSelection ? 1 : 0)}");
            return false;
        }

        Entity gridEntity = _gridPathingQuery.GetSingletonEntity();
        GridConfig grid = em.GetComponentData<GridConfig>(gridEntity);
        var walkable = em.GetBuffer<GridWalkable>(gridEntity).AsNativeArray();
        DynamicBlockerData blockerData = em.GetComponentData<DynamicBlockerData>(gridEntity);
        var blocked = blockerData.Blocked;
        var friendlyPassFactionIds = blockerData.FriendlyPassFactionIds;
        var occupied = em.GetComponentData<DynamicOccupancyData>(gridEntity).Occupied;
        int2 transportCell = em.GetComponentData<UnitGrid>(transport).Cell;
        int2 transportSize = em.GetComponentData<UnitFootprint>(transport).Size;
        int2 boardingTransportSize = airTransport ? new int2(1, 1) : transportSize;
        using var liveUnitEntities = _pathingLiveUnitsQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
        using var liveUnitGrids = _pathingLiveUnitsQuery.ToComponentDataArray<UnitGrid>(Unity.Collections.Allocator.Temp);
        using var liveUnitFootprints = _pathingLiveUnitsQuery.ToComponentDataArray<UnitFootprint>(Unity.Collections.Allocator.Temp);

        bool hasPendingAirPickupLanding = false;
        int2 pendingAirPickupCell = default;
        if (airTransport && !transportLanded)
        {
            if (!_unitTransportBoardingSystem.TryFindAirTransportPickupForBoarding(
                    em,
                    transport,
                    grid,
                    walkable,
                    blocked,
                    friendlyPassFactionIds,
                    occupied,
                    transportCell,
                    transportSize,
                    _selectedBoardingSourceEntities,
                    selectedCount,
                    liveUnitEntities,
                    liveUnitGrids,
                    liveUnitFootprints,
                    out pendingAirPickupCell))
            {
                LogTransportBoarding($"result=NoAirPickupLanding transport={DescribeTransportBoardingEntity(em, transport)} selected={selectedCount}");
                return false;
            }

            transportCell = pendingAirPickupCell;
            hasPendingAirPickupLanding = true;
        }

        var boardingOrders = new List<PendingTransportBoardingOrder>();
        var reservedBoardingCells = new HashSet<int>();
        for (int i = 0; i < selectedCount && boardingOrders.Count < availableSeats; i++)
        {
            Entity passenger = _selectedBoardingSourceEntities[i];
            if (passenger == transport)
            {
                LogTransportBoarding($"result=SkipPassenger reason=IsTransport passenger={DescribeTransportBoardingEntity(em, passenger)} transport={DescribeTransportBoardingEntity(em, transport)}");
                continue;
            }

            if (!_unitTransportBoardingSystem.IsSoldierBoardingCandidate(em, passenger))
            {
                LogTransportBoarding($"result=SkipPassenger reason=NotSoldierBoardingCandidate passenger={DescribeTransportBoardingEntity(em, passenger)} transport={DescribeTransportBoardingEntity(em, transport)}");
                continue;
            }

            int2 referenceCell = em.GetComponentData<UnitGrid>(passenger).Cell;
            byte passengerFaction = em.GetComponentData<Faction>(passenger).Id;
            int2 passengerFootprint = em.GetComponentData<UnitFootprint>(passenger).Size;
            int directBoardingCells = _unitTransportBoardingSystem.GetTransportBoardingDirectCells(em, transport);
            if (!_unitTransportBoardingSystem.TryFindTransportApproachCell(
                    grid,
                    walkable,
                    blocked,
                    friendlyPassFactionIds,
                    occupied,
                    transportCell,
                    boardingTransportSize,
                    referenceCell,
                    passengerFootprint,
                    passenger,
                    liveUnitEntities,
                    liveUnitGrids,
                    liveUnitFootprints,
                    transport,
                    em.GetComponentData<UnitGrid>(transport).Cell,
                    transportSize,
                    reservedBoardingCells,
                    directBoardingCells,
                    passengerFaction,
                    out int2 goal))
            {
                LogTransportBoarding(
                    $"result=NoApproach passenger={DescribeTransportBoardingEntity(em, passenger)} transport={DescribeTransportBoardingEntity(em, transport)} " +
                    $"passengerCell={referenceCell} transportCell={transportCell} transportSize={boardingTransportSize} directCells={directBoardingCells}");
                continue;
            }

            boardingOrders.Add(new PendingTransportBoardingOrder
            {
                Passenger = passenger,
                PassengerCell = referenceCell,
                Goal = goal,
                DirectBoarding = goal.Equals(referenceCell)
            });
            _unitTransportBoardingSystem.ReserveFootprintCells(grid, goal, passengerFootprint, reservedBoardingCells);
        }

        if (boardingOrders.Count <= 0)
        {
            LogTransportBoarding(
                $"result=NoBoardingOrders transport={DescribeTransportBoardingEntity(em, transport)} selected={selectedCount} " +
                $"selectedTag={selectedTagCount} selectedMove={selectedMoveCount} usedCache={(usedCachedSelection ? 1 : 0)} seats={occupiedSeats}/{capacity} availableSeats={availableSeats}");
            return false;
        }

        if (hasPendingAirPickupLanding)
        {
            _unitTransportBoardingSystem.CommandAirTransportPickup(em, transport, grid, pendingAirPickupCell, _unitMoveOrderSystem);
            LogTransportBoarding($"result=AirPickupLanding transport={DescribeTransportBoardingEntity(em, transport)} landing={pendingAirPickupCell}");
        }

        for (int i = 0; i < boardingOrders.Count; i++)
        {
            Entity passenger = boardingOrders[i].Passenger;
            int2 goal = boardingOrders[i].Goal;
            if (!em.Exists(passenger) || !_unitTransportBoardingSystem.IsSoldierBoardingCandidate(em, passenger))
                continue;

            _unitMoveOrderSystem.ClearMovementOrderComponents(em, passenger);
            if (!em.HasBuffer<UnitTransportHiddenVisualScale>(passenger))
                em.AddBuffer<UnitTransportHiddenVisualScale>(passenger);
            _unitMoveOrderSystem.IssueImmediateMoveCommand(em, passenger, goal);
            if (em.HasComponent<UnitTransportBoardingTarget>(passenger))
                em.SetComponentData(passenger, new UnitTransportBoardingTarget { Transport = transport, Goal = goal });
            else
                em.AddComponentData(passenger, new UnitTransportBoardingTarget { Transport = transport, Goal = goal });

            LogTransportBoarding(
                $"result=Order passenger={DescribeTransportBoardingEntity(em, passenger)} transport={DescribeTransportBoardingEntity(em, transport)} " +
                $"from={boardingOrders[i].PassengerCell} goal={goal} direct={(boardingOrders[i].DirectBoarding ? 1 : 0)} usedCache={(usedCachedSelection ? 1 : 0)} seats={occupiedSeats + i}/{capacity}");
        }

        ShowMoveOrderMarker(em, transportCell, em.GetComponentData<LocalTransform>(transport).Position, 0);
        MoveOrderScreenMarkerRequested?.Invoke(screenPosition);
        ClearCurrentSelection(em, "BoardTransportOrderIssued");
        _focusedUnit = Entity.Null;
        _cameraDragging = false;
        return true;
    }

    private int CollectSelectedBoardingSourceEntities(
        EntityManager em,
        List<Entity> selectedEntities,
        out int selectedTagCount,
        out int selectedMoveCount,
        out bool usedCachedSelection)
    {
        selectedEntities.Clear();
        selectedTagCount = 0;
        selectedMoveCount = 0;
        usedCachedSelection = false;

        EnsureEntityQueries(em);
        using var selectedMoveEntities = _selectedMoveQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
        selectedMoveCount = selectedMoveEntities.Length;
        if (selectedMoveEntities.Length > 0)
        {
            _cachedSelectedMoveEntities.Clear();
            for (int i = 0; i < selectedMoveEntities.Length; i++)
            {
                Entity entity = selectedMoveEntities[i];
                selectedEntities.Add(entity);
                if (IsCacheableSelectedMoveEntity(em, entity))
                    _cachedSelectedMoveEntities.Add(entity);
            }

            selectedTagCount = selectedMoveCount;
            return selectedEntities.Count;
        }

        using var selectedTagEntities = _selectedTagQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
        selectedTagCount = selectedTagEntities.Length;
        if (selectedTagEntities.Length > 0)
        {
            for (int i = 0; i < selectedTagEntities.Length; i++)
                selectedEntities.Add(selectedTagEntities[i]);
            return selectedEntities.Count;
        }

        for (int i = _cachedSelectedMoveEntities.Count - 1; i >= 0; i--)
        {
            Entity entity = _cachedSelectedMoveEntities[i];
            if (!IsCacheableSelectedMoveEntity(em, entity))
            {
                _cachedSelectedMoveEntities.RemoveAt(i);
                continue;
            }

            selectedEntities.Add(entity);
        }

        if (selectedEntities.Count > 0)
            usedCachedSelection = true;
        return selectedEntities.Count;
    }

    public bool IsBoardablePlayerTransportClick(Vector2 screenPosition)
    {
        if (World.DefaultGameObjectInjectionWorld == null)
            return false;

        var em = World.DefaultGameObjectInjectionWorld.EntityManager;
        EnsureEntityQueries(em);
        return TryGetClickedOrNearbyBoardableTransport(screenPosition, em, out _, false);
    }

    private bool TryGetClickedOrNearbyBoardableTransport(Vector2 screenPosition, EntityManager em, out Entity transport, bool logDiagnostics = true)
    {
        transport = Entity.Null;
        Entity clickedEntity = Entity.Null;
        bool hasClickedEntity = TryGetClickedUnitEntity(screenPosition, em, out clickedEntity);
        if (hasClickedEntity && _unitTransportBoardingSystem.IsBoardablePlayerTransport(em, clickedEntity))
        {
            transport = clickedEntity;
            if (logDiagnostics)
                LogTransportBoarding($"result=ClickedTransport transport={DescribeTransportBoardingEntity(em, transport)} {DescribeTransportAirState(em, transport)}");
            return true;
        }

        if (!TryGetClickedCell(screenPosition, em, out int2 clickedCell, out _))
        {
            if (logDiagnostics && hasClickedEntity && IsKnownPersonnelTransport(em, clickedEntity))
                LogTransportBoarding($"result=NoClickedCell clicked={DescribeTransportBoardingEntity(em, clickedEntity)} {DescribeTransportAirState(em, clickedEntity)}");
            return false;
        }

        if (TryFindNearbyBoardableTransport(em, clickedCell, out transport))
        {
            if (logDiagnostics)
                LogTransportBoarding($"result=NearbyTransport clickedCell={clickedCell} transport={DescribeTransportBoardingEntity(em, transport)} {DescribeTransportAirState(em, transport)}");
            return true;
        }

        if (logDiagnostics && hasClickedEntity && IsKnownPersonnelTransport(em, clickedEntity))
        {
            LogTransportBoarding(
                $"result=ClickedTransportRejected clicked={DescribeTransportBoardingEntity(em, clickedEntity)} " +
                $"player={(IsPlayerFaction(em, clickedEntity) ? 1 : 0)} landed={(_unitTransportBoardingSystem.IsTransportLandedForBoarding(em, clickedEntity) ? 1 : 0)} {DescribeTransportAirState(em, clickedEntity)}");
        }

        if (hasClickedEntity &&
            em.Exists(clickedEntity) &&
            em.HasComponent<UnitMove>(clickedEntity) &&
            !em.HasComponent<RuntimeBuildingCombatTag>(clickedEntity) &&
            !em.HasComponent<StaticGridBlocker>(clickedEntity))
        {
            return false;
        }

        return false;
    }

    private bool TryFindNearbyBoardableTransport(EntityManager em, int2 clickedCell, out Entity transport)
    {
        transport = Entity.Null;
        EnsureEntityQueries(em);
        using var entities = _allSelectableQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
        int bestScore = int.MaxValue;
        for (int i = 0; i < entities.Length; i++)
        {
            Entity candidate = entities[i];
            if (!_unitTransportBoardingSystem.IsBoardablePlayerTransport(em, candidate))
                continue;

            int2 cell = em.GetComponentData<UnitGrid>(candidate).Cell;
            int2 footprint = em.GetComponentData<UnitFootprint>(candidate).Size;
            int clickPaddingCells = _unitTransportBoardingSystem.GetTransportBoardingClickPaddingCells(em, candidate, footprint);
            if (!UnitFootprintUtility.ContainsCellWithPadding(cell, footprint, clickedCell, clickPaddingCells))
                continue;

            int2 delta = clickedCell - cell;
            int score = math.abs(delta.x) + math.abs(delta.y);
            if (score >= bestScore)
                continue;

            bestScore = score;
            transport = candidate;
        }

        return transport != Entity.Null;
    }

    private static string ResolveUnitSourceName(EntityManager em, Entity entity)
    {
        if (!em.Exists(entity))
            return string.Empty;

        if (em.HasComponent<UnitSourcePrefabKey>(entity))
        {
            string sourceName = em.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString();
            if (!string.IsNullOrWhiteSpace(sourceName))
                return sourceName;
        }

        return em.GetName(entity);
    }

    private bool IsKnownPersonnelTransport(EntityManager em, Entity entity)
    {
        if (!em.Exists(entity))
            return false;

        if (em.HasComponent<UnitTransportCapacity>(entity) &&
            math.max(0, em.GetComponentData<UnitTransportCapacity>(entity).SoldierCapacity) > 0)
        {
            return true;
        }

        return _unitTransportBoardingSystem.ResolveTransportCapacity(em, entity) > 0;
    }

    private static bool IsPlayerFaction(EntityManager em, Entity entity)
    {
        return em.Exists(entity) &&
               em.HasComponent<Faction>(entity) &&
               em.GetComponentData<Faction>(entity).Id == 0;
    }

    private static string DescribeTransportBoardingEntity(EntityManager em, Entity entity)
    {
        if (entity == Entity.Null)
            return "null";
        if (!em.Exists(entity))
            return $"{entity}:missing";

        string sourceName = ResolveUnitSourceName(em, entity);
        if (string.IsNullOrWhiteSpace(sourceName))
            sourceName = "<unnamed>";

        string cell = em.HasComponent<UnitGrid>(entity)
            ? em.GetComponentData<UnitGrid>(entity).Cell.ToString()
            : "no-cell";
        string faction = em.HasComponent<Faction>(entity)
            ? em.GetComponentData<Faction>(entity).Id.ToString()
            : "no-faction";
        string health = em.HasComponent<UnitHealth>(entity)
            ? $"{em.GetComponentData<UnitHealth>(entity).Current}/{em.GetComponentData<UnitHealth>(entity).Max}"
            : "no-health";
        string capacity = em.HasComponent<UnitTransportCapacity>(entity)
            ? em.GetComponentData<UnitTransportCapacity>(entity).SoldierCapacity.ToString()
            : "no-capacity";
        string passengers = em.HasBuffer<UnitTransportPassengerElement>(entity)
            ? em.GetBuffer<UnitTransportPassengerElement>(entity).Length.ToString()
            : "no-passengers";

        return $"{sourceName} entity={entity} cell={cell} faction={faction} health={health} seats={passengers}/{capacity}";
    }

    private static string DescribeTransportAirState(EntityManager em, Entity entity)
    {
        if (!em.Exists(entity) || !em.HasComponent<UnitAirMovement>(entity))
            return "air=none";
        if (!em.HasComponent<UnitAirState>(entity))
            return "air=missing-state";

        UnitAirState airState = em.GetComponentData<UnitAirState>(entity);
        return $"airborne={airState.Airborne} takeoff={airState.TakeoffRolling} landing={airState.LandingRolling} returning={airState.ReturningHome} rope={(em.HasComponent<UnitTransportRopeDisembarkRequest>(entity) ? 1 : 0)}";
    }

    private int CountPendingBoardingOrders(EntityManager em, Entity transport)
    {
        EnsureEntityQueries(em);
        using var entities = _transportBoardingTargetQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
        int count = 0;
        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            if (em.Exists(entity) &&
                em.HasComponent<UnitTransportBoardingTarget>(entity) &&
                em.GetComponentData<UnitTransportBoardingTarget>(entity).Transport == transport)
            {
                count++;
            }
        }

        return count;
    }

    private void CacheSelectedMoveEntities(EntityManager em, List<Entity> entities)
    {
        _selectionStateSystem.CacheSelectedMoveEntities(em, entities);
    }

    private void CacheSelectedMoveEntity(EntityManager em, Entity entity)
    {
        _selectionStateSystem.CacheSelectedMoveEntity(em, entity);
    }

    private static bool IsCacheableSelectedMoveEntity(EntityManager em, Entity entity)
    {
        return SelectionStateSystem.IsCacheableSelectedMoveEntity(em, entity);
    }

    public bool TryIssueMoveOrderToBuilding(Vector2Int originCell, Vector2Int footprintCells)
    {
        if (World.DefaultGameObjectInjectionWorld == null)
            return false;

        var em = World.DefaultGameObjectInjectionWorld.EntityManager;
        EnsureEntityQueries(em);
        using var selectedEntities = _selectedMoveQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
        if (selectedEntities.Length == 0)
            return false;

        if (_gridPathingQuery.IsEmptyIgnoreFilter)
            return false;

        Entity gridEntity = _gridPathingQuery.GetSingletonEntity();
        GridConfig grid = em.GetComponentData<GridConfig>(gridEntity);
        var walkable = em.GetBuffer<GridWalkable>(gridEntity).AsNativeArray();
        var blocked = em.GetComponentData<DynamicBlockerData>(gridEntity).Blocked;
        var occupied = em.GetComponentData<DynamicOccupancyData>(gridEntity).Occupied;

        int2 referenceCell = em.GetComponentData<UnitGrid>(selectedEntities[0]).Cell;
        if (!TryFindBuildingApproachCell(grid, walkable, blocked, occupied, originCell, footprintCells, referenceCell, out int2 goal))
            return false;

        for (int i = 0; i < selectedEntities.Length; i++)
        {
            Entity entity = selectedEntities[i];

            if (IsAlreadyMovingToGoal(em, entity, goal))
                continue;

            if (em.HasComponent<EngageTarget>(entity))
                em.RemoveComponent<EngageTarget>(entity);
            if (em.HasComponent<UnitPathFollow>(entity))
                em.RemoveComponent<UnitPathFollow>(entity);
            if (em.HasComponent<UnitPathRange>(entity))
                em.RemoveComponent<UnitPathRange>(entity);
            if (em.HasComponent<AutoWanderMoveTag>(entity))
                em.RemoveComponent<AutoWanderMoveTag>(entity);

            if (em.HasComponent<UnitTarget>(entity))
                em.SetComponentData(entity, new UnitTarget { Cell = goal });
            else
                em.AddComponentData(entity, new UnitTarget { Cell = goal });

            if (!em.HasComponent<UnitAirMovement>(entity))
            {
                if (em.HasComponent<UnitPathRequest>(entity))
                    em.SetComponentData(entity, new UnitPathRequest { Goal = goal });
                else
                    em.AddComponentData(entity, new UnitPathRequest { Goal = goal });
            }
            else if (em.HasComponent<UnitPathRequest>(entity))
            {
                em.RemoveComponent<UnitPathRequest>(entity);
            }

            if (!em.HasComponent<ManualMoveOrderTag>(entity))
                em.AddComponent<ManualMoveOrderTag>(entity);
        }

        ClearCurrentSelection(em, "MoveOrderToBuilding");
        _focusedUnit = Entity.Null;
        if (TryGetPointerPosition(out Vector2 markerScreenPosition))
            MoveOrderScreenMarkerRequested?.Invoke(markerScreenPosition);
        return true;
    }

    private static bool IsAlreadyMovingToGoal(EntityManager em, Entity entity, int2 goal)
    {
        if (!em.Exists(entity))
            return false;

        bool sameTarget =
            em.HasComponent<UnitTarget>(entity) &&
            em.GetComponentData<UnitTarget>(entity).Cell.Equals(goal);
        bool samePendingRequest =
            em.HasComponent<UnitPathRequest>(entity) &&
            em.GetComponentData<UnitPathRequest>(entity).Goal.Equals(goal);
        bool hasActiveMovement =
            em.HasComponent<UnitPathFollow>(entity) ||
            em.HasComponent<UnitPathRequest>(entity);

        return sameTarget && (samePendingRequest || hasActiveMovement);
    }

    private bool TryGetClickedCell(Vector2 screenPosition, EntityManager em, out int2 cell, out Vector3 worldPoint)
    {
        cell = default;
        worldPoint = default;

        EnsureEntityQueries(em);
        if (_gridConfigQuery.IsEmptyIgnoreFilter)
            return false;

        var grid = em.GetComponentData<GridConfig>(_gridConfigQuery.GetSingletonEntity());
        Ray ray = worldCamera.ScreenPointToRay(screenPosition);
        Plane plane = new(Vector3.up, new Vector3(0f, grid.Origin.y, 0f));
        if (!plane.Raycast(ray, out float distance))
            return false;

        worldPoint = ray.GetPoint(distance);
        cell = GridUtils.WorldToCell(grid, worldPoint);
        return GridUtils.InBounds(cell, grid.Width, grid.Height);
    }

    private void CacheMoveOrderMarker()
    {
        _moveOrderMarkerPropertyBlock = new MaterialPropertyBlock();

        if (moveOrderMarkerPrefab != null)
        {
            Object markerInstance = Instantiate((Object)moveOrderMarkerPrefab);
            _moveOrderMarker = markerInstance as GameObject;
            if (_moveOrderMarker == null)
                return;
            _moveOrderMarker.name = "MoveOrderMarkerRuntime";
            if (_runtimeRoot != null)
                _moveOrderMarker.transform.SetParent(_runtimeRoot, false);
            _moveOrderMarkerRenderers = _moveOrderMarker.GetComponentsInChildren<Renderer>(true);
            _moveOrderMarker.SetActive(false);
            return;
        }

        _moveOrderMarker = null;
        _moveOrderMarkerRenderers = null;

        _moveOrderMarker = null;
        _moveOrderMarkerRenderers = null;
    }

    private void CacheAttackOrderMarker()
    {
        _attackOrderMarkerPropertyBlock = new MaterialPropertyBlock();

        if (attackOrderMarkerPrefab != null)
        {
            Object markerInstance = Instantiate((Object)attackOrderMarkerPrefab);
            _attackOrderMarker = markerInstance as GameObject;
            if (_attackOrderMarker == null)
                return;
            _attackOrderMarker.name = "AttackOrderMarkerRuntime";
            if (_runtimeRoot != null)
                _attackOrderMarker.transform.SetParent(_runtimeRoot, false);
            _attackOrderMarkerRenderers = _attackOrderMarker.GetComponentsInChildren<Renderer>(true);
            _attackOrderMarker.SetActive(false);
            return;
        }

        _attackOrderMarker = null;
        _attackOrderMarkerRenderers = null;
    }

    private void UpdateMoveOrderMarkerVisibility()
    {
        if (_moveOrderMarker == null || _moveOrderMarkerHideTime < 0f)
            return;

        if (Time.time < _moveOrderMarkerHideTime)
            return;

        _moveOrderMarker.SetActive(false);
        _moveOrderMarkerHideTime = -1f;
        if (_attackOrderMarkerHideTime < 0f)
            SetHudWorldMarkersVisible(false);
    }

    private void UpdateAttackOrderMarkerVisibility()
    {
        if (_attackOrderMarker == null || _attackOrderMarkerHideTime < 0f)
            return;

        if (Time.time < _attackOrderMarkerHideTime)
            return;

        _attackOrderMarker.SetActive(false);
        _attackOrderMarkerHideTime = -1f;
        if (_moveOrderMarkerHideTime < 0f)
            SetHudWorldMarkersVisible(false);
    }

    private void ShowMoveOrderMarker(EntityManager em, int2 cell, Vector3 worldPoint, byte factionId)
    {
        if (_moveOrderMarker == null || _moveOrderMarkerRenderers == null || _moveOrderMarkerRenderers.Length == 0)
            return;

        EnsureEntityQueries(em);
        if (_gridBlockerQuery.IsEmptyIgnoreFilter)
            return;

        Entity gridEntity = _gridBlockerQuery.GetSingletonEntity();
        GridConfig grid = em.GetComponentData<GridConfig>(gridEntity);
        if (!GridUtils.InBounds(cell, grid.Width, grid.Height))
            return;

        int cellIndex = GridUtils.CellToIndex(cell, grid.Width);
        var walkable = em.GetBuffer<GridWalkable>(gridEntity);
        DynamicBlockerData blockerData = em.GetComponentData<DynamicBlockerData>(gridEntity);
        bool blocked = walkable[cellIndex].Value == 0 || (blockerData.Blocked.IsCreated && blockerData.Blocked.IsSet(cellIndex));

        if (blocked)
        {
            _moveOrderMarker.SetActive(false);
            _moveOrderMarkerHideTime = -1f;
            return;
        }

        Vector3 worldPosition = worldPoint;
        worldPosition.y = grid.Origin.y + 0.05f;

        _moveOrderMarker.transform.position = worldPosition;
        _moveOrderMarker.transform.rotation = Quaternion.identity;
        _moveOrderMarker.SetActive(true);

        for (int i = 0; i < _moveOrderMarkerRenderers.Length; i++)
        {
            Renderer renderer = _moveOrderMarkerRenderers[i];
            if (renderer == null)
                continue;

            _moveOrderMarkerPropertyBlock.Clear();
            renderer.SetPropertyBlock(_moveOrderMarkerPropertyBlock);
        }

        _moveOrderMarkerHideTime = Time.time + orderMarkerVisibleSeconds;
    }

    private void ShowAttackOrderMarker(EntityManager em, Vector3 worldPoint)
    {
        if (_attackOrderMarker == null || _attackOrderMarkerRenderers == null || _attackOrderMarkerRenderers.Length == 0)
            return;

        EnsureEntityQueries(em);
        if (_gridBlockerQuery.IsEmptyIgnoreFilter)
            return;

        Entity gridEntity = _gridBlockerQuery.GetSingletonEntity();
        GridConfig grid = em.GetComponentData<GridConfig>(gridEntity);

        Vector3 worldPosition = worldPoint;
        worldPosition.y = grid.Origin.y + 0.05f;

        _attackOrderMarker.transform.position = worldPosition;
        _attackOrderMarker.transform.rotation = Quaternion.identity;
        _attackOrderMarker.SetActive(true);

        for (int i = 0; i < _attackOrderMarkerRenderers.Length; i++)
        {
            Renderer renderer = _attackOrderMarkerRenderers[i];
            if (renderer == null)
                continue;

            _attackOrderMarkerPropertyBlock.Clear();
            renderer.SetPropertyBlock(_attackOrderMarkerPropertyBlock);
        }

        _attackOrderMarkerHideTime = Time.time + orderMarkerVisibleSeconds;
    }

    private Color ResolveFactionColor(byte factionId)
    {
        FactionVisualSettings settings = _factionVisualSettings;
        if (settings != null)
            return settings.GetColor(factionId);

        return factionId switch
        {
            0 => new Color(0.12f, 0.72f, 1f, 1f),
            1 => new Color(1f, 0.35f, 0.2f, 1f),
            _ => new Color(0.82f, 0.82f, 0.82f, 1f)
        };
    }

    private void UpdateLastKnownPointerPosition(Vector2 pointerPosition)
    {
        _rtsSelectionInputSystem.UpdateLastKnownPointerPosition(pointerPosition);
    }

    private bool TryGetPointerPosition(out Vector2 pointerPosition)
    {
        if (GamePointerInput.TryGetPrimaryPointer(out GamePointerState pointer))
        {
            pointerPosition = pointer.Position;
            UpdateLastKnownPointerPosition(pointerPosition);
            return true;
        }

        return _rtsSelectionInputSystem.TryGetLastKnownPointerPosition(out pointerPosition);
    }

    private static bool TryFindBuildingApproachCell(
        in GridConfig grid,
        in Unity.Collections.NativeArray<GridWalkable> walkable,
        in Unity.Collections.NativeBitArray blocked,
        in Unity.Collections.NativeBitArray occupied,
        Vector2Int originCell,
        Vector2Int footprintCells,
        int2 referenceCell,
        out int2 goal)
    {
        goal = default;
        int maxRadius = math.max(grid.Width, grid.Height);
        int bestScore = int.MaxValue;
        bool found = false;

        for (int extraRadius = 1; extraRadius <= maxRadius; extraRadius++)
        {
            int minX = originCell.x - extraRadius;
            int minY = originCell.y - extraRadius;
            int maxX = originCell.x + footprintCells.x - 1 + extraRadius;
            int maxY = originCell.y + footprintCells.y - 1 + extraRadius;

            for (int x = minX; x <= maxX; x++)
            {
                TryScoreBuildingApproachCandidate(grid, walkable, blocked, occupied, referenceCell, x, minY, ref bestScore, ref goal, ref found);
                if (maxY != minY)
                    TryScoreBuildingApproachCandidate(grid, walkable, blocked, occupied, referenceCell, x, maxY, ref bestScore, ref goal, ref found);
            }

            for (int y = minY + 1; y < maxY; y++)
            {
                TryScoreBuildingApproachCandidate(grid, walkable, blocked, occupied, referenceCell, minX, y, ref bestScore, ref goal, ref found);
                if (maxX != minX)
                    TryScoreBuildingApproachCandidate(grid, walkable, blocked, occupied, referenceCell, maxX, y, ref bestScore, ref goal, ref found);
            }

            if (found)
                return true;
        }

        return false;
    }

    private static void TryScoreBuildingApproachCandidate(
        in GridConfig grid,
        in Unity.Collections.NativeArray<GridWalkable> walkable,
        in Unity.Collections.NativeBitArray blocked,
        in Unity.Collections.NativeBitArray occupied,
        int2 referenceCell,
        int x,
        int y,
        ref int bestScore,
        ref int2 bestCell,
        ref bool found)
    {
        if ((uint)x >= (uint)grid.Width || (uint)y >= (uint)grid.Height)
            return;

        int index = GridUtils.CellToIndex(new int2(x, y), grid.Width);
        if (walkable[index].Value == 0 || blocked.IsSet(index) || occupied.IsSet(index))
            return;

        int score = math.abs(referenceCell.x - x) + math.abs(referenceCell.y - y);
        if (!found || score < bestScore)
        {
            bestScore = score;
            bestCell = new int2(x, y);
            found = true;
        }
    }

    private static bool IsPointerOverUI(Vector2 screenPosition, out string source)
    {
        source = null;
        return false;
    }

    private bool IsPointerOverGameplayUi(Vector2 screenPosition, out string source)
    {
        if (_mainMenuPlayUi != null)
            return _mainMenuPlayUi.IsPointerOverAnyGameplayUi(screenPosition, out source);

        return IsPointerOverUI(screenPosition, out source);
    }

    private void PanCamera(Vector2 screenDelta)
    {
        if (_rtsCameraSystem.PanCamera(worldCamera, screenDelta, panSensitivity))
            OrderScreenMarkersHideRequested?.Invoke();
    }

    private void HandleFullscreenIsoCameraPan()
    {
        if (!GamePointerInput.TryGetPrimaryPointer(out GamePointerState pointer))
            return;

        Vector2 pointerPosition = pointer.Position;
        UpdateLastKnownPointerPosition(pointerPosition);
        bool pointerOverGameplayUi = IsPointerOverGameplayUi(pointerPosition, out _);

        if (pointer.WasPressedThisFrame)
        {
            _lastPointerPosition = pointerPosition;
            _cameraDragging = !pointerOverGameplayUi;
        }

        if (pointer.IsPressed && _cameraDragging && !pointerOverGameplayUi)
        {
            Vector2 frameDelta = pointerPosition - _lastPointerPosition;
            if (frameDelta.sqrMagnitude > 0f)
                PanCamera(frameDelta);
        }

        _lastPointerPosition = pointerPosition;

        if (pointer.WasReleasedThisFrame || !pointer.IsPressed)
            _cameraDragging = false;
    }

    private void UpdateZoom()
    {
        if (_isZoomTransitionActive)
        {
            float targetHeight = _wasBuildModeActive ? buildModeZoomHeight : normalModeZoomHeight;
            float targetPitch = _wasBuildModeActive ? buildModePitch : normalModePitch;
            float targetYaw = _wasBuildModeActive ? buildModeYaw : normalModeYaw;
            float targetFieldOfView = _wasBuildModeActive ? buildModeFieldOfView : normalModeFieldOfView;

            if (UpdatePerspectiveCameraMode(targetHeight, targetPitch, targetYaw, targetFieldOfView))
                _rtsCameraSystem.CompleteZoomTransition();

            return;
        }

        float zoomDirection = 0f;
        if (_runtimeGameplayStateSystem.ZoomInHeld)
            zoomDirection += 1f;
        if (_runtimeGameplayStateSystem.ZoomOutHeld)
            zoomDirection -= 1f;

        _rtsCameraSystem.UpdatePerspectiveZoom(worldCamera, zoomDirection, zoomSpeed, Time.deltaTime, minZoomHeight, maxZoomHeight);
    }

    private void UpdateFullscreenIsoZoom()
    {
        if (worldCamera == null)
            return;

        float zoomDirection = 0f;
        if (_runtimeGameplayStateSystem.ZoomInHeld)
            zoomDirection += 1f;
        if (_runtimeGameplayStateSystem.ZoomOutHeld)
            zoomDirection -= 1f;

        _rtsCameraSystem.UpdateFullscreenIsoZoom(zoomDirection, zoomSpeed, Time.deltaTime, minZoomHeight, maxZoomHeight);
    }

    private void UpdateBuildModeCameraTransition()
    {
        if (worldCamera == null)
            return;

        SyncCameraZoomModeState();

        UpdatePerspectiveCameraMode(buildModeZoomHeight, buildModePitch, buildModeYaw, buildModeFieldOfView);
    }

    private void SyncCameraZoomModeState()
    {
        if (Chapter01M01PlayableRuntime.IsActiveMission())
        {
            _wasPlayRequested = _runtimeGameplayStateSystem.PlayRequested;
            _wasBuildModeActive = _runtimeGameplayStateSystem.BuildModeActive;
            _isZoomTransitionActive = false;
            return;
        }

        if (!_wasPlayRequested && _runtimeGameplayStateSystem.PlayRequested)
        {
            Vector3 focusWorldPosition = worldCamera != null ? GetCameraGroundCenterWorld() : Vector3.zero;
            ApplyPerspectiveCameraModeInstant(normalModeZoomHeight, normalModePitch, normalModeYaw, normalModeFieldOfView);
            if (worldCamera != null)
                MoveCameraGroundCenterTo(focusWorldPosition);
            _wasPlayRequested = true;
            _wasBuildModeActive = _runtimeGameplayStateSystem.BuildModeActive;
            _isZoomTransitionActive = _runtimeGameplayStateSystem.BuildModeActive;
            _rtsCameraSystem.ResetTransitionVelocities();
            return;
        }

        _wasPlayRequested = _runtimeGameplayStateSystem.PlayRequested;

        if (_wasBuildModeActive != _runtimeGameplayStateSystem.BuildModeActive)
        {
            _rtsCameraSystem.BeginZoomTransition(_runtimeGameplayStateSystem.BuildModeActive);
        }
    }

    private void ConsumeInitialCameraFocusRequest()
    {
        if (!_runtimeGameplayStateSystem.InitialCameraFocusRequested || worldCamera == null)
            return;

        MoveCameraGroundCenterTo(_runtimeGameplayStateSystem.InitialCameraFocusWorld);
        _runtimeGameplayStateSystem.InitialCameraFocusRequested = false;
        _rtsCameraSystem.ClearSmoothFocusTarget();
    }

    private void UpdateSmoothCameraFocus()
    {
        if (!_rtsCameraSystem.HasSmoothFocusTarget || worldCamera == null)
            return;

        Vector3 currentGroundCenter = GetCameraGroundCenterWorld();
        Vector3 smoothedCenter = _rtsCameraSystem.UpdateSmoothFocus(currentGroundCenter, zoomTransitionSmoothTime);
        MoveCameraGroundCenterTo(smoothedCenter);
    }

    private void ApplyPerspectiveCameraModeInstant(float height, float pitch, float yaw, float fieldOfView)
    {
        _rtsCameraSystem.ApplyPerspectiveCameraModeInstant(worldCamera, height, pitch, yaw, fieldOfView);
    }

    private void ApplyFullscreenIsoCameraModeInstant(float height, float orthographicSize, float pitch, float yaw)
    {
        _rtsCameraSystem.ApplyFullscreenIsoCameraModeInstant(worldCamera, height, orthographicSize, pitch, yaw);
    }

    public void EnterFullscreenMapIsoMode(Vector3 focusWorldPosition)
    {
        if (worldCamera == null)
            return;

        _fullscreenIsoTargetHeight = Mathf.Clamp(fullscreenIsoZoomHeight, minZoomHeight, maxZoomHeight);
        _fullscreenIsoTargetOrthographicSize = Mathf.Clamp(fullscreenIsoOrthographicSize, 8f, 48f);
        MoveCameraGroundCenterTo(focusWorldPosition);
        ApplyFullscreenIsoCameraModeInstant(_fullscreenIsoTargetHeight, _fullscreenIsoTargetOrthographicSize, fullscreenIsoPitch, fullscreenIsoYaw);
        _runtimeGameplayStateSystem.FullscreenMapIsoMode = true;
        _runtimeGameplayStateSystem.FullscreenMapOpen = true;
        _cameraDragging = false;
    }

    public void ExitFullscreenMapIsoMode()
    {
        if (worldCamera != null)
            ApplyPerspectiveCameraModeInstant(normalModeZoomHeight, normalModePitch, normalModeYaw, normalModeFieldOfView);

        _runtimeGameplayStateSystem.FullscreenMapIsoMode = false;
        _cameraDragging = false;
    }

    public bool IsNormalIsoModeActive => _normalIsoModeActive;

    public void ToggleNormalIsoMode()
    {
        if (_normalIsoModeActive)
            ExitNormalIsoMode();
        else
            EnterNormalIsoMode();
    }

    public void EnterNormalIsoMode()
    {
        if (worldCamera == null)
            return;

        Vector3 focusWorldPosition = GetCameraGroundCenterWorld();
        float currentGroundSpan = GetVisibleGroundVerticalSpan();
        float currentHeight = Mathf.Clamp(worldCamera.transform.position.y, minZoomHeight, maxZoomHeight);
        _fullscreenIsoTargetHeight = currentHeight;
        _fullscreenIsoTargetOrthographicSize = Mathf.Clamp(
            CalculateOrthographicSizeForGroundSpan(currentGroundSpan, _fullscreenIsoTargetHeight, fullscreenIsoPitch, fullscreenIsoYaw),
            8f,
            48f);
        ApplyFullscreenIsoCameraModeInstant(_fullscreenIsoTargetHeight, _fullscreenIsoTargetOrthographicSize, fullscreenIsoPitch, fullscreenIsoYaw);
        MoveCameraGroundCenterTo(focusWorldPosition);
        _normalIsoModeActive = true;
        _cameraDragging = false;
    }

    public void ExitNormalIsoMode()
    {
        Vector3 focusWorldPosition = worldCamera != null ? GetCameraGroundCenterWorld() : Vector3.zero;
        if (worldCamera != null)
        {
            float currentGroundSpan = GetVisibleGroundVerticalSpan();
            float targetHeight = CalculatePerspectiveHeightForGroundSpan(currentGroundSpan, normalModePitch, normalModeYaw, normalModeFieldOfView);
            ApplyPerspectiveCameraModeInstant(targetHeight, normalModePitch, normalModeYaw, normalModeFieldOfView);
            MoveCameraGroundCenterTo(focusWorldPosition);
        }

        _normalIsoModeActive = false;
        _cameraDragging = false;
    }

    public void MoveCameraGroundCenterTo(Vector3 focusWorldPosition)
    {
        _rtsCameraSystem.MoveCameraGroundCenterTo(worldCamera, focusWorldPosition);
    }

    public void SmoothMoveCameraGroundCenterTo(Vector3 focusWorldPosition)
    {
        if (worldCamera == null)
            return;

        _rtsCameraSystem.SetSmoothFocusTarget(focusWorldPosition, resetVelocity: true);
        _rtsCameraSystem.ClearDragging();
    }

    public void FollowCameraGroundCenterTo(Vector3 focusWorldPosition)
    {
        if (worldCamera == null)
            return;

        _rtsCameraSystem.SetSmoothFocusTarget(focusWorldPosition, resetVelocity: false);
        _rtsCameraSystem.ClearDragging();
    }

    private Vector3 GetCameraGroundCenterWorld()
    {
        return _rtsCameraSystem.GetCameraGroundCenterWorld(worldCamera);
    }

    private float GetVisibleGroundVerticalSpan()
    {
        return _rtsCameraSystem.GetVisibleGroundVerticalSpan(worldCamera);
    }

    private bool TryGetGroundPointFromViewport(Vector2 viewport, out Vector3 point)
    {
        return _rtsCameraSystem.TryGetGroundPointFromViewport(worldCamera, viewport, out point);
    }

    private float CalculateOrthographicSizeForGroundSpan(float targetGroundSpan, float height, float pitch, float yaw)
    {
        return _rtsCameraSystem.CalculateOrthographicSizeForGroundSpan(
            worldCamera,
            targetGroundSpan,
            height,
            pitch,
            yaw,
            fullscreenIsoOrthographicSize);
    }

    private float CalculatePerspectiveHeightForGroundSpan(float targetGroundSpan, float pitch, float yaw, float fieldOfView)
    {
        return _rtsCameraSystem.CalculatePerspectiveHeightForGroundSpan(
            worldCamera,
            targetGroundSpan,
            pitch,
            yaw,
            fieldOfView,
            minZoomHeight,
            maxZoomHeight,
            normalModeZoomHeight);
    }

    private bool UpdatePerspectiveCameraMode(float targetHeight, float targetPitch, float targetYaw, float targetFieldOfView)
    {
        return _rtsCameraSystem.UpdatePerspectiveCameraMode(
            worldCamera,
            targetHeight,
            targetPitch,
            targetYaw,
            targetFieldOfView,
            zoomTransitionSmoothTime);
    }

    private bool UpdateFullscreenIsoCameraMode(float targetHeight, float targetOrthographicSize, float targetPitch, float targetYaw)
    {
        return _rtsCameraSystem.UpdateFullscreenIsoCameraMode(
            worldCamera,
            targetHeight,
            targetOrthographicSize,
            targetPitch,
            targetYaw,
            zoomTransitionSmoothTime);
    }

    private void ClearCurrentSelection(EntityManager em, string reason = "Unspecified")
    {
        EnsureEntityQueries(em);
        using var entities = _selectedTagQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
        int cacheBefore = _cachedSelectedMoveEntities.Count;
        if (entities.Length > 0 || cacheBefore > 0)
            LogSelectionDiagnostic($"result=Clear reason={reason} selected={entities.Length} cache={cacheBefore}");
        _cachedSelectedMoveEntities.Clear();
        for (int i = 0; i < entities.Length; i++)
        {
            var entity = entities[i];
            if (!em.HasComponent<SelectedUnitTag>(entity))
                continue;
            em.RemoveComponent<SelectedUnitTag>(entity);
        }
        ClearHudSelection();
    }

    private static Rect GetScreenRect(Vector2 a, Vector2 b)
    {
        Vector2 min = Vector2.Min(a, b);
        Vector2 max = Vector2.Max(a, b);
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    private static Rect GetGuiRect(Vector2 a, Vector2 b)
    {
        var rect = GetScreenRect(a, b);
        rect.y = Screen.height - rect.yMax;
        return rect;
    }

    private void DrawRect(Rect rect, Color color)
    {
        GUI.color = color;
        GUI.DrawTexture(rect, _pixel);
        GUI.color = Color.white;
    }

    private void DrawBorder(Rect rect, float thickness, Color color)
    {
        DrawRect(new Rect(rect.xMin, rect.yMin, rect.width, thickness), color);
        DrawRect(new Rect(rect.xMin, rect.yMax - thickness, rect.width, thickness), color);
        DrawRect(new Rect(rect.xMin, rect.yMin, thickness, rect.height), color);
        DrawRect(new Rect(rect.xMax - thickness, rect.yMin, thickness, rect.height), color);
    }

    public void ClearFocusedUnit()
    {
        _focusedUnit = Entity.Null;
        _explicitAttackTargetModeActive = false;
        ClearHudSelection();
        ClearHudCommandMode();
        SetHudWorldMarkersVisible(false);
    }

    public void DeselectAllUnits(string reason = "DeselectAllUnits")
    {
        if (World.DefaultGameObjectInjectionWorld == null)
        {
            _focusedUnit = Entity.Null;
            _explicitAttackTargetModeActive = false;
            ClearHudSelection();
            ClearHudCommandMode();
            SetHudWorldMarkersVisible(false);
            return;
        }

        var em = World.DefaultGameObjectInjectionWorld.EntityManager;
        ClearCurrentSelection(em, reason);
        _focusedUnit = Entity.Null;
        _explicitAttackTargetModeActive = false;
        ClearHudSelection();
        ClearHudCommandMode();
        SetHudWorldMarkersVisible(false);
    }

    public void SelectAllVisiblePlayerUnits()
    {
        SelectAllVisiblePlayerUnits(VisibleUnitSelectionFilter.All);
    }

    public void SelectAllVisiblePlayerSoldiers()
    {
        SelectAllVisiblePlayerUnits(VisibleUnitSelectionFilter.Soldiers);
    }

    public void SelectAllVisiblePlayerVehicles()
    {
        SelectAllVisiblePlayerUnits(VisibleUnitSelectionFilter.Vehicles);
    }

    private void SelectAllVisiblePlayerUnits(VisibleUnitSelectionFilter filter)
    {
        if (worldCamera == null)
        {
            LogSelectionDiagnostic($"result=SelectAllSkipped reason=NoCamera filter={filter}");
            return;
        }

        SelectUnitsInRectangle(new Rect(0f, 0f, Screen.width, Screen.height), filter);
        _ignoreNextLeftMouseRelease = false;
        _skipNextWorldReleaseAfterSelection = false;
        _cameraDragging = false;
    }

    public bool FocusUnitEntity(Entity entity)
    {
        if (entity == Entity.Null || World.DefaultGameObjectInjectionWorld == null)
            return false;

        var em = World.DefaultGameObjectInjectionWorld.EntityManager;
        EnsureEntityQueries(em);
        if (!em.Exists(entity) || !em.HasComponent<Faction>(entity))
            return false;

        ClearCurrentSelection(em, "FocusUnitEntity");
        if (em.GetComponentData<Faction>(entity).Id == 0 && !em.HasComponent<SelectedUnitTag>(entity))
            em.AddComponent<SelectedUnitTag>(entity);
        CacheSelectedMoveEntity(em, entity);
        LogSelectionDiagnostic($"result=Focus source=FocusUnitEntity entity={DescribeTransportBoardingEntity(em, entity)} cache={_cachedSelectedMoveEntities.Count}");

        _focusedUnit = entity;
        _buildingPlacementController?.ClearSelectedBuilding("RTSSelection.FocusUnitEntity");
        _ignoreNextLeftMouseRelease = true;
        _ignoreWorldCommandsUntilFrame = Time.frameCount + 1;
        _runtimeGameplayStateSystem.SuppressNextWorldClick = true;
        _cameraDragging = false;
        if (em.HasComponent<UnitAirMovement>(entity))
            _unitTargetOrderSystem.ClearAccidentalAirSelectionMove(em, entity);
        ApplyHudSelection(em, entity);
        return true;
    }

    public TacticalCommandResult TrySelectRuntimeEntity(Entity entity)
    {
        TacticalCommandResult result = ValidateControllableEntity(entity);
        if (!result.Accepted)
        {
            ApplyHudCommandResult(result);
            return result;
        }

        result = FocusUnitEntity(entity)
            ? TacticalCommandResult.Success()
            : TacticalCommandResult.Rejected(TacticalCommandReasonCode.TargetNotAttackable);
        ApplyHudCommandResult(result);
        return result;
    }

    public TacticalCommandResult TryIssueMoveToCell(int2 goal)
    {
        ApplyHudCommandMode(TacticalCommandMode.Move);

        if (World.DefaultGameObjectInjectionWorld == null)
            return ApplyAndReturn(TacticalCommandResult.Rejected(TacticalCommandReasonCode.NoSelection));

        EntityManager em = World.DefaultGameObjectInjectionWorld.EntityManager;
        EnsureEntityQueries(em);
        using var selectedEntities = _selectedMoveQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
        if (selectedEntities.Length == 0)
            return ApplyAndReturn(TacticalCommandResult.Rejected(TacticalCommandReasonCode.NoSelection));

        int issuedCount = 0;
        for (int i = 0; i < selectedEntities.Length; i++)
        {
            Entity entity = selectedEntities[i];
            TacticalCommandResult validation = ValidateControllableEntity(entity);
            if (!validation.Accepted)
                continue;

            _unitMoveOrderSystem.IssueImmediateMoveCommand(em, entity, goal);
            issuedCount++;
        }

        TacticalCommandResult result = issuedCount > 0
            ? TacticalCommandResult.Success()
            : TacticalCommandResult.Rejected(TacticalCommandReasonCode.TargetNotAttackable);
        if (result.Accepted)
        {
            SetHudWorldMarkersVisible(true);
            ClearHudCommandMode();
        }

        return ApplyAndReturn(result);
    }

    public TacticalCommandResult TryIssueAttackTarget(Entity targetEntity)
    {
        ApplyHudCommandMode(TacticalCommandMode.Attack);

        if (World.DefaultGameObjectInjectionWorld == null)
            return ApplyAndReturn(TacticalCommandResult.Rejected(TacticalCommandReasonCode.NoSelection));

        EntityManager em = World.DefaultGameObjectInjectionWorld.EntityManager;
        EnsureEntityQueries(em);
        using var selectedEntities = _selectedAttackQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
        UnitTargetOrderSystem.AttackOrderIssueResult issueResult =
            _unitTargetOrderSystem.IssueAttackTarget(em, selectedEntities, targetEntity);
        TacticalCommandResult result = issueResult.CommandResult;
        if (result.Accepted)
        {
            _explicitAttackTargetModeActive = false;
            _cameraDragging = false;
            SetHudWorldMarkersVisible(true);
            ClearHudCommandMode();
        }

        return ApplyAndReturn(result);
    }

    private TacticalCommandResult ApplyAndReturn(TacticalCommandResult result)
    {
        ApplyHudCommandResult(result);
        if (!result.Accepted)
            ClearHudCommandMode();
        return result;
    }

    private static TacticalCommandResult ValidateControllableEntity(Entity entity)
    {
        if (entity == Entity.Null || World.DefaultGameObjectInjectionWorld == null)
            return TacticalCommandResult.Rejected(TacticalCommandReasonCode.TargetNotAttackable);

        EntityManager em = World.DefaultGameObjectInjectionWorld.EntityManager;
        if (!em.Exists(entity) || !em.HasComponent<Faction>(entity) || !em.HasComponent<UnitMove>(entity))
            return TacticalCommandResult.Rejected(TacticalCommandReasonCode.TargetNotAttackable);
        if (em.GetComponentData<Faction>(entity).Id != 0)
            return TacticalCommandResult.Rejected(TacticalCommandReasonCode.TargetNotAttackable);
        if (em.HasComponent<UnitHealth>(entity) && em.GetComponentData<UnitHealth>(entity).Current <= 0)
            return TacticalCommandResult.Rejected(TacticalCommandReasonCode.TargetNotAttackable);

        return TacticalCommandResult.Success();
    }

    public void PreserveSelectedUnitOrders()
    {
        _preservedUiOrders.Clear();

        if (World.DefaultGameObjectInjectionWorld == null)
            return;

        var em = World.DefaultGameObjectInjectionWorld.EntityManager;
        EnsureEntityQueries(em);
        using var entities = _selectedTagQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            var state = new PreservedOrderState
            {
                Entity = entity,
                HadEngageTarget = em.HasComponent<EngageTarget>(entity),
                HadUnitTarget = em.HasComponent<UnitTarget>(entity),
                HadUnitPathRequest = em.HasComponent<UnitPathRequest>(entity),
                HadUnitPathFollow = em.HasComponent<UnitPathFollow>(entity),
                HadUnitPathRange = em.HasComponent<UnitPathRange>(entity)
            };

            if (state.HadEngageTarget)
                state.EngageTarget = em.GetComponentData<EngageTarget>(entity);
            if (state.HadUnitTarget)
                state.UnitTarget = em.GetComponentData<UnitTarget>(entity);
            if (state.HadUnitPathRequest)
                state.UnitPathRequest = em.GetComponentData<UnitPathRequest>(entity);
            if (state.HadUnitPathFollow)
                state.UnitPathFollow = em.GetComponentData<UnitPathFollow>(entity);
            if (state.HadUnitPathRange)
                state.UnitPathRange = em.GetComponentData<UnitPathRange>(entity);

            _preservedUiOrders.Add(state);
        }
    }

    public void RestorePreservedUnitOrders()
    {
        if (World.DefaultGameObjectInjectionWorld == null)
        {
            _preservedUiOrders.Clear();
            return;
        }

        var em = World.DefaultGameObjectInjectionWorld.EntityManager;
        for (int i = 0; i < _preservedUiOrders.Count; i++)
        {
            PreservedOrderState state = _preservedUiOrders[i];
            if (!em.Exists(state.Entity))
                continue;

            RestoreComponent(em, state.Entity, state.HadEngageTarget, state.EngageTarget);
            RestoreComponent(em, state.Entity, state.HadUnitTarget, state.UnitTarget);
            RestoreComponent(em, state.Entity, state.HadUnitPathRequest, state.UnitPathRequest);
            RestoreComponent(em, state.Entity, state.HadUnitPathFollow, state.UnitPathFollow);
            RestoreComponent(em, state.Entity, state.HadUnitPathRange, state.UnitPathRange);
        }

        _preservedUiOrders.Clear();
    }

    public void CaptureUiClickSequence()
    {
        LogSelectionDiagnostic("result=CaptureUiClickSequence");
        _rtsSelectionInputSystem.CaptureUiClickSequence();
        _cameraDragging = false;
    }

    public void DestroyFocusedUnit()
    {
        if (!TryGetFocusedUnitEntity(out var em, out Entity entity) || !FocusedUnitOwnedByPlayer)
            return;

        if (em.HasComponent<SelectedUnitTag>(entity))
            em.RemoveComponent<SelectedUnitTag>(entity);
        if (em.HasComponent<UnitHealth>(entity))
        {
            UnitHealth health = em.GetComponentData<UnitHealth>(entity);
            health.Current = 0;
            em.SetComponentData(entity, health);
        }
        else
        {
            em.DestroyEntity(entity);
        }
        _focusedUnit = Entity.Null;
    }

    public void ReturnFocusedUnitToBase()
    {
        if (!TryGetFocusedUnitEntity(out var em, out Entity entity) || !FocusedUnitOwnedByPlayer)
            return;

        EnsureEntityQueries(em);
        if (_respawnQueueQuery.IsEmptyIgnoreFilter)
            return;

        Entity queueEntity = _respawnQueueQuery.GetSingletonEntity();
        byte factionId = em.HasComponent<Faction>(entity) ? em.GetComponentData<Faction>(entity).Id : (byte)0;
        int2 goal = default;
        if (em.HasBuffer<RespawnFactionSpawnPoint>(queueEntity))
        {
            DynamicBuffer<RespawnFactionSpawnPoint> points = em.GetBuffer<RespawnFactionSpawnPoint>(queueEntity);
            for (int i = 0; i < points.Length; i++)
            {
                if (points[i].FactionId != factionId)
                    continue;

                goal = points[i].SpawnCell;
                break;
            }
        }
        _unitMoveOrderSystem.IssueImmediateMoveCommand(em, entity, goal);
    }

    public void EnableFocusedUnitAutoAttack()
    {
        if (!TryGetFocusedUnitEntity(out var em, out Entity entity) || !FocusedUnitOwnedByPlayer)
            return;

        _unitTargetOrderSystem.ClearCommandedAttackOrderComponents(em, entity);
    }

    public bool IssueFocusedMissileLauncherRadarAttack()
    {
        if (!TryGetFocusedUnitEntity(out var em, out Entity launcher) || !FocusedUnitOwnedByPlayer)
            return false;
        if (!em.HasComponent<UnitCombat>(launcher) || em.GetComponentData<UnitCombat>(launcher).CanAttack == 0)
            return false;

        MissileLauncherTargetMode mode = ResolveMissileLauncherTargetMode(em, launcher);
        if (mode == MissileLauncherTargetMode.None)
            return false;

        byte factionId = em.GetComponentData<Faction>(launcher).Id;
        if (!_unitTargetOrderSystem.TryFindRadarTargetForMissileLauncher(em, factionId, mode == MissileLauncherTargetMode.Air, launcher, out Entity target, out int2 targetCell, out float3 targetPosition))
            return false;

        _unitTargetOrderSystem.IssueDirectAttackTarget(em, launcher, target, targetCell, targetPosition);

        ShowAttackOrderMarker(em, targetPosition);
        ClearCurrentSelection(em, "MissileLauncherRadarAttack");
        _focusedUnit = launcher;
        _explicitAttackTargetModeActive = false;
        _cameraDragging = false;
        ApplyHudCommandResult(TacticalCommandResult.Success());
        ClearHudCommandMode();
        SetHudWorldMarkersVisible(true);
        ApplyHudSelection(em, launcher);
        return true;
    }

    public bool ArmFocusedAttackTargetMode()
    {
        if (!CanCommandFocusedUnit || !FocusedUnitCanAttack)
        {
            ApplyHudCommandResult(TacticalCommandResult.Rejected(
                HasFocusedUnit ? TacticalCommandReasonCode.TargetNotAttackable : TacticalCommandReasonCode.NoSelection));
            return false;
        }

        _explicitAttackTargetModeActive = true;
        ApplyHudCommandMode(TacticalCommandMode.Attack);
        SetHudWorldMarkersVisible(true);
        _runtimeGameplayStateSystem.SelectionModeActive = false;
        _runtimeGameplayStateSystem.SuppressNextWorldClick = true;
        _dragging = false;
        _cameraDragging = false;
        _skipNextWorldReleaseAfterSelection = true;
        return true;
    }

    public void CancelExplicitAttackTargetMode()
    {
        _explicitAttackTargetModeActive = false;
        ClearHudCommandMode();
    }

    public bool IssueHoldPositionOrder()
    {
        return IssueImmediateSelectedUnitOrder(TacticalCommandMode.Hold, clearEngageTarget: true);
    }

    public bool IssueStopOrder()
    {
        return IssueImmediateSelectedUnitOrder(TacticalCommandMode.Stop, clearEngageTarget: true);
    }

    private bool IssueImmediateSelectedUnitOrder(TacticalCommandMode mode, bool clearEngageTarget)
    {
        ApplyHudCommandMode(mode);

        if (World.DefaultGameObjectInjectionWorld == null)
        {
            ApplyHudCommandResult(TacticalCommandResult.Rejected(TacticalCommandReasonCode.NoSelection));
            return false;
        }

        var em = World.DefaultGameObjectInjectionWorld.EntityManager;
        EnsureEntityQueries(em);
        using var selectedEntities = _selectedMoveQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
        if (selectedEntities.Length == 0)
        {
            ApplyHudCommandResult(TacticalCommandResult.Rejected(TacticalCommandReasonCode.NoSelection));
            return false;
        }

        for (int i = 0; i < selectedEntities.Length; i++)
        {
            Entity entity = selectedEntities[i];
            if (!em.Exists(entity))
                continue;

            _unitMoveOrderSystem.RemoveComponentIfPresent<UnitTarget>(em, entity);
            _unitMoveOrderSystem.RemoveComponentIfPresent<UnitPathRequest>(em, entity);
            _unitMoveOrderSystem.RemoveComponentIfPresent<UnitPathFollow>(em, entity);
            _unitMoveOrderSystem.RemoveComponentIfPresent<UnitPathRange>(em, entity);
            _unitMoveOrderSystem.RemoveComponentIfPresent<UnitPathRetryCooldown>(em, entity);
            _unitMoveOrderSystem.RemoveComponentIfPresent<AutoWanderMoveTag>(em, entity);
            _unitMoveOrderSystem.RemoveComponentIfPresent<BaseBreachOrder>(em, entity);
            if (clearEngageTarget)
                _unitMoveOrderSystem.RemoveComponentIfPresent<EngageTarget>(em, entity);
            if (!em.HasComponent<ManualMoveOrderTag>(entity))
                em.AddComponent<ManualMoveOrderTag>(entity);
        }

        _explicitAttackTargetModeActive = false;
        _cameraDragging = false;
        SetHudWorldMarkersVisible(false);
        ApplyHudCommandResult(TacticalCommandResult.Success());
        return true;
    }

    private void RefreshFocusedUnit()
    {
        if (World.DefaultGameObjectInjectionWorld == null)
            return;

        var em = World.DefaultGameObjectInjectionWorld.EntityManager;
        EnsureEntityQueries(em);

        if (_focusedUnit != Entity.Null)
        {
            if (!em.Exists(_focusedUnit))
            {
                _focusedUnit = Entity.Null;
            }
            else if (em.HasComponent<Faction>(_focusedUnit) && em.GetComponentData<Faction>(_focusedUnit).Id != 0 && em.HasComponent<SelectedUnitTag>(_focusedUnit))
            {
                em.RemoveComponent<SelectedUnitTag>(_focusedUnit);
            }
        }

        if (_focusedUnit != Entity.Null)
        {
            ApplyHudSelection(em, _focusedUnit);
            return;
        }

        using var selectedEntities = _selectedTagQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
        if (selectedEntities.Length != 1)
            return;

        Entity selectedEntity = selectedEntities[0];
        if (!em.Exists(selectedEntity) || !em.HasComponent<Faction>(selectedEntity))
            return;

        if (em.GetComponentData<Faction>(selectedEntity).Id != 0)
            return;

        _focusedUnit = selectedEntity;
        ApplyHudSelection(em, _focusedUnit);
    }

    private enum MissileLauncherTargetMode
    {
        None,
        Ground,
        Air
    }

    private static MissileLauncherTargetMode ResolveMissileLauncherTargetMode(EntityManager em, Entity launcher)
    {
        if (!em.HasComponent<UnitSourcePrefabKey>(launcher))
            return MissileLauncherTargetMode.None;

        string sourceKey = em.GetComponentData<UnitSourcePrefabKey>(launcher).Value.ToString();
        if (string.Equals(sourceKey, "Unit_Veh_Missle_Launcher_Air", System.StringComparison.OrdinalIgnoreCase))
            return MissileLauncherTargetMode.Air;
        if (string.Equals(sourceKey, "Unit_Veh_Missle_Launcher_Ground", System.StringComparison.OrdinalIgnoreCase))
            return MissileLauncherTargetMode.Ground;

        return MissileLauncherTargetMode.None;
    }

    private bool TryFocusUnit(Vector2 screenPosition)
    {
        if (World.DefaultGameObjectInjectionWorld == null)
            return false;

        var em = World.DefaultGameObjectInjectionWorld.EntityManager;
        EnsureEntityQueries(em);
        if (!TryGetClickedUnitEntity(screenPosition, em, out Entity bestEntity))
            return false;
        if (_unitTargetOrderSystem.IsBuildingEntity(em, bestEntity))
            return false;

        ClearCurrentSelection(em, "TryFocusUnit");
        if (em.GetComponentData<Faction>(bestEntity).Id == 0 && !em.HasComponent<SelectedUnitTag>(bestEntity))
            em.AddComponent<SelectedUnitTag>(bestEntity);
        CacheSelectedMoveEntity(em, bestEntity);
        LogSelectionDiagnostic($"result=Focus source=TryFocusUnit entity={DescribeTransportBoardingEntity(em, bestEntity)} cache={_cachedSelectedMoveEntities.Count}");

        _focusedUnit = bestEntity;
        _buildingPlacementController?.ClearSelectedBuilding("RTSSelection.TryFocusUnit");
        _ignoreNextLeftMouseRelease = true;
        _ignoreWorldCommandsUntilFrame = Time.frameCount + 1;
        _runtimeGameplayStateSystem.SuppressNextWorldClick = true;
        _cameraDragging = false;
        if (em.HasComponent<UnitAirMovement>(bestEntity))
            _unitTargetOrderSystem.ClearAccidentalAirSelectionMove(em, bestEntity);
        ApplyHudSelection(em, bestEntity);
        return true;
    }

    private bool TryIssueAttackOrderToClickedUnit(Vector2 screenPosition)
    {
        if (World.DefaultGameObjectInjectionWorld == null)
        {
            if (_explicitAttackTargetModeActive)
                ApplyHudCommandResult(TacticalCommandResult.Rejected(TacticalCommandReasonCode.NoSelection));
            return false;
        }

        var em = World.DefaultGameObjectInjectionWorld.EntityManager;
        EnsureEntityQueries(em);
        if (!TryGetClickedUnitEntity(screenPosition, em, out Entity targetEntity))
        {
            if (_explicitAttackTargetModeActive)
                ApplyHudCommandResult(TacticalCommandResult.Rejected(TacticalCommandReasonCode.TargetNotAttackable));
            return false;
        }

        TacticalCommandResult targetValidation = _unitTargetOrderSystem.ValidateAttackTarget(em, targetEntity);
        if (!targetValidation.Accepted)
        {
            if (_explicitAttackTargetModeActive)
                ApplyHudCommandResult(targetValidation);
            return false;
        }

        using var selectedEntities = _selectedAttackQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
        UnitTargetOrderSystem.AttackOrderIssueResult issueResult = _unitTargetOrderSystem.IssueAttackTarget(
            em,
            selectedEntities,
            targetEntity,
            TryResolveBaseBreachTargetForAttackOrder);
        if (!issueResult.CommandResult.Accepted)
        {
            ApplyHudCommandResult(issueResult.CommandResult);
            return false;
        }

        ShowAttackOrderMarker(em, issueResult.TargetPosition);
        AttackOrderScreenMarkerRequested?.Invoke(screenPosition);
        ClearCurrentSelection(em, "AttackOrderIssued");
        _focusedUnit = Entity.Null;
        _cameraDragging = false;
        ApplyHudCommandResult(TacticalCommandResult.Success());
        ClearHudCommandMode();
        SetHudWorldMarkersVisible(true);
        return true;
    }

    private bool TryResolveBaseBreachTargetForAttackOrder(
        byte factionId,
        Entity targetEntity,
        int2 targetCell,
        int2 attackerCell,
        out Entity breachTarget,
        out int2 breachCell,
        out float3 breachPosition)
    {
        breachTarget = Entity.Null;
        breachCell = default;
        breachPosition = default;
        return _buildingPlacementController != null &&
               _buildingPlacementController.TryResolveBaseBreachTarget(
                   factionId,
                   targetEntity,
                   targetCell,
                   attackerCell,
                   out breachTarget,
                   out breachCell,
                   out breachPosition,
                   out _);
    }

    private bool TryGetClickedUnitEntity(Vector2 screenPosition, EntityManager em, out Entity bestEntity)
    {
        bestEntity = Entity.Null;
        if (!TryGetClickedCell(screenPosition, em, out var clickedCell, out _))
            return false;

        EnsureEntityQueries(em);
        RefreshFocusableUnitLookup(em);
        if (_gridConfigQuery.IsEmptyIgnoreFilter)
            return false;

        GridConfig grid = em.GetComponentData<GridConfig>(_gridConfigQuery.GetSingletonEntity());
        int cellIndex = GridUtils.CellToIndex(clickedCell, grid.Width);
        if (!_focusableUnitsByCell.TryGetValue(cellIndex, out List<Entity> candidates) || candidates == null || candidates.Count == 0)
            return false;

        float bestDistanceSq = float.MaxValue;
        for (int i = candidates.Count - 1; i >= 0; i--)
        {
            Entity entity = candidates[i];
            if (!IsFocusableUnitCandidate(em, entity))
            {
                candidates.RemoveAt(i);
                _focusableUnitCoverage.Remove(entity);
                continue;
            }

            int2 cell = em.GetComponentData<UnitGrid>(entity).Cell;
            int2 footprint = em.GetComponentData<UnitFootprint>(entity).Size;
            int padding = GetFocusablePadding(em, entity);
            if (!UnitFootprintUtility.ContainsCellWithPadding(cell, footprint, clickedCell, padding))
            {
                RefreshFocusableUnitLookupEntry(em, grid, entity);
                continue;
            }

            Vector3 screen = worldCamera.WorldToScreenPoint(em.GetComponentData<LocalToWorld>(entity).Position);
            float distanceSq = (new Vector2(screen.x, screen.y) - screenPosition).sqrMagnitude;
            if (distanceSq < bestDistanceSq)
            {
                bestDistanceSq = distanceSq;
                bestEntity = entity;
            }
        }

        if (candidates.Count == 0)
            _focusableUnitsByCell.Remove(cellIndex);

        return bestEntity != Entity.Null;
    }

    private void RefreshFocusableUnitLookup(EntityManager em)
    {
        EnsureEntityQueries(em);
        if (_gridConfigQuery.IsEmptyIgnoreFilter)
            return;

        GridConfig grid = em.GetComponentData<GridConfig>(_gridConfigQuery.GetSingletonEntity());
        int focusableUnitCount = _focusableUnitsQuery.CalculateEntityCount();
        if (_lastFocusableUnitCount < 0 || focusableUnitCount != _lastFocusableUnitCount)
        {
            RebuildFocusableUnitLookup(em, grid, focusableUnitCount);
            return;
        }

        bool gridChanged = !_changedFocusableGridQuery.IsEmptyIgnoreFilter;
        bool footprintChanged = !_changedFocusableFootprintQuery.IsEmptyIgnoreFilter;
        if (!gridChanged && !footprintChanged)
            return;

        var changedEntities = new HashSet<Entity>();
        if (gridChanged)
        {
            using var changedGridEntities = _changedFocusableGridQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
            for (int i = 0; i < changedGridEntities.Length; i++)
                changedEntities.Add(changedGridEntities[i]);
        }

        if (footprintChanged)
        {
            using var changedFootprintEntities = _changedFocusableFootprintQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
            for (int i = 0; i < changedFootprintEntities.Length; i++)
                changedEntities.Add(changedFootprintEntities[i]);
        }

        foreach (Entity entity in changedEntities)
            RefreshFocusableUnitLookupEntry(em, grid, entity);
    }

    private void RebuildFocusableUnitLookup(EntityManager em, GridConfig grid, int focusableUnitCount)
    {
        _focusableUnitsByCell.Clear();
        _focusableUnitCoverage.Clear();

        using var entities = _focusableUnitsQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            if (!IsFocusableUnitCandidate(em, entity))
                continue;

            int2 cell = em.GetComponentData<UnitGrid>(entity).Cell;
            int2 size = em.GetComponentData<UnitFootprint>(entity).Size;
            AddFocusableUnitLookupEntry(em, grid, entity, cell, size);
        }

        _lastFocusableUnitCount = focusableUnitCount;
    }

    private void RefreshFocusableUnitLookupEntry(EntityManager em, GridConfig grid, Entity entity)
    {
        if (_focusableUnitCoverage.TryGetValue(entity, out FocusableUnitCoverage previousCoverage))
        {
            RemoveFocusableUnitLookupEntry(grid, entity, previousCoverage.Cell, previousCoverage.Size, previousCoverage.Padding);
            _focusableUnitCoverage.Remove(entity);
        }

        if (!IsFocusableUnitCandidate(em, entity))
            return;

        int2 cell = em.GetComponentData<UnitGrid>(entity).Cell;
        int2 size = em.GetComponentData<UnitFootprint>(entity).Size;
        AddFocusableUnitLookupEntry(em, grid, entity, cell, size);
    }

    private static bool IsFocusableUnitCandidate(EntityManager em, Entity entity)
    {
        bool hasTransitTag = em.HasComponent<UnitSpawnTransitTag>(entity);
        if (hasTransitTag)
        {
            bool groundedIdleAirUnit =
                em.HasComponent<UnitAirState>(entity) &&
                !em.HasComponent<UnitTarget>(entity) &&
                !em.HasComponent<EngageTarget>(entity) &&
                em.GetComponentData<UnitAirState>(entity).Airborne == 0 &&
                em.GetComponentData<UnitAirState>(entity).ReturningHome == 0 &&
                em.GetComponentData<UnitAirState>(entity).TakeoffRolling == 0 &&
                em.GetComponentData<UnitAirState>(entity).LandingRolling == 0;

            if (!groundedIdleAirUnit)
                return false;
        }

        return em.Exists(entity) &&
            !em.HasComponent<Prefab>(entity) &&
            !em.HasComponent<StaticGridBlocker>(entity) &&
            em.HasComponent<Faction>(entity) &&
            em.HasComponent<UnitGrid>(entity) &&
            em.HasComponent<UnitFootprint>(entity) &&
            em.HasComponent<LocalToWorld>(entity);
    }

    private void AddFocusableUnitLookupEntry(EntityManager em, GridConfig grid, Entity entity, int2 cell, int2 size)
    {
        int2 min;
        int2 max;
        int padding = GetFocusablePadding(em, entity);
        GetPaddedFocusableBounds(grid, cell, size, padding, out min, out max);

        for (int y = min.y; y < max.y; y++)
        {
            int rowStart = y * grid.Width;
            for (int x = min.x; x < max.x; x++)
            {
                int index = rowStart + x;
                if (!_focusableUnitsByCell.TryGetValue(index, out List<Entity> entities))
                {
                    entities = new List<Entity>();
                    _focusableUnitsByCell.Add(index, entities);
                }

                entities.Add(entity);
            }
        }

        _focusableUnitCoverage[entity] = new FocusableUnitCoverage
        {
            Cell = cell,
            Size = size,
            Padding = padding
        };
    }

    private void RemoveFocusableUnitLookupEntry(GridConfig grid, Entity entity, int2 cell, int2 size, int padding)
    {
        int2 min;
        int2 max;
        GetPaddedFocusableBounds(grid, cell, size, padding, out min, out max);

        for (int y = min.y; y < max.y; y++)
        {
            int rowStart = y * grid.Width;
            for (int x = min.x; x < max.x; x++)
            {
                int index = rowStart + x;
                if (!_focusableUnitsByCell.TryGetValue(index, out List<Entity> entities))
                    continue;

                entities.Remove(entity);
                if (entities.Count == 0)
                    _focusableUnitsByCell.Remove(index);
            }
        }
    }

    private static void GetPaddedFocusableBounds(GridConfig grid, int2 centerCell, int2 size, out int2 min, out int2 max)
    {
        GetPaddedFocusableBounds(grid, centerCell, size, 1, out min, out max);
    }

    private static void GetPaddedFocusableBounds(GridConfig grid, int2 centerCell, int2 size, int paddingAmount, out int2 min, out int2 max)
    {
        int2 clampedSize = UnitFootprintUtility.ClampSize(size);
        int2 padding = new int2(paddingAmount, paddingAmount);
        int2 paddedMin = UnitFootprintUtility.GetMinCell(centerCell, clampedSize) - padding;
        int2 paddedMax = paddedMin + clampedSize + (padding * 2);
        min = new int2(math.clamp(paddedMin.x, 0, grid.Width), math.clamp(paddedMin.y, 0, grid.Height));
        max = new int2(math.clamp(paddedMax.x, 0, grid.Width), math.clamp(paddedMax.y, 0, grid.Height));
    }

    private static int GetFocusablePadding(EntityManager em, Entity entity)
    {
        return em.HasComponent<UnitAirMovement>(entity) ? 4 : 1;
    }

    private bool TryGetFocusedUnitEntity(out EntityManager em, out Entity entity)
    {
        em = default;
        entity = Entity.Null;

        if (_focusedUnit == Entity.Null || World.DefaultGameObjectInjectionWorld == null)
            return false;

        em = World.DefaultGameObjectInjectionWorld.EntityManager;
        if (!em.Exists(_focusedUnit))
            return false;

        entity = _focusedUnit;
        return true;
    }

    private static void RestoreComponent<T>(EntityManager em, Entity entity, bool shouldExist, T value)
        where T : unmanaged, IComponentData
    {
        if (shouldExist)
        {
            if (em.HasComponent<T>(entity))
                em.SetComponentData(entity, value);
            else
                em.AddComponentData(entity, value);
        }
        else if (em.HasComponent<T>(entity))
        {
            em.RemoveComponent<T>(entity);
        }
    }

}
