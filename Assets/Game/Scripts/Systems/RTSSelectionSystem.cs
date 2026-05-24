using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.InputSystem;
using Unity.Transforms;
using UnityEngine;
using static UnityEngine.Object;

public sealed class RTSSelectionSystem
{
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

    private static bool ShouldQueueTransportBoardingDiagnostics(EntityManager em)
    {
        if (Application.isBatchMode)
            return true;

        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<RuntimeDiagnosticsStateComponent>());
        return !query.IsEmptyIgnoreFilter &&
            em.GetComponentData<RuntimeDiagnosticsStateComponent>(query.GetSingletonEntity()).TransportBoardingDiagnostics != 0;
    }

    private static Entity EnsureTransportBoardingDiagnosticQueue(EntityManager em)
    {
        using EntityQuery query = em.CreateEntityQuery(
            ComponentType.ReadOnly<TransportBoardingDiagnosticLogQueueComponent>(),
            ComponentType.ReadWrite<TransportBoardingDiagnosticLogComponent>());
        if (!query.IsEmptyIgnoreFilter)
            return query.GetSingletonEntity();

        Entity queueEntity = em.CreateEntity(typeof(TransportBoardingDiagnosticLogQueueComponent));
        em.SetName(queueEntity, "TransportBoardingDiagnosticLogQueue");
        em.AddBuffer<TransportBoardingDiagnosticLogComponent>(queueEntity);
        return queueEntity;
    }

    private static void EnqueueTransportBoardingDiagnostic(EntityManager em, FixedString512Bytes message)
    {
        Entity queueEntity = EnsureTransportBoardingDiagnosticQueue(em);
        DynamicBuffer<TransportBoardingDiagnosticLogComponent> logs = em.GetBuffer<TransportBoardingDiagnosticLogComponent>(queueEntity);
        logs.Add(new TransportBoardingDiagnosticLogComponent { Message = message });
    }

    private static void LogSelectionDiagnostic(string message)
    {
        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return;

        EntityManager em = world.EntityManager;
        if (ShouldQueueTransportBoardingDiagnostics(em))
            EnqueueTransportBoardingDiagnostic(em, $"[Selection] {message}");
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
    private readonly VisibleUnitSelectionSystem _visibleUnitSelectionSystem = new();
    private readonly UnitMoveOrderSystem _unitMoveOrderSystem = new();
    private readonly SelectedMoveOrderCommandSystem _selectedMoveOrderCommandSystem = new();
    private readonly UnitTargetOrderSystem _unitTargetOrderSystem = new();
    private readonly AttackOrderCommandSystem _attackOrderCommandSystem = new();
    private readonly SelectionOrderMarkerSystem _selectionOrderMarkerSystem = new();
    private readonly SelectionHudFeedbackSystem _selectionHudFeedbackSystem = new();
    private readonly FocusedUnitCommandSystem _focusedUnitCommandSystem = new();
    private readonly FocusedUnitLifecycleSystem _focusedUnitLifecycleSystem = new();
    private readonly SelectedUnitOrderSnapshotSystem _selectedUnitOrderSnapshotSystem = new();
    private readonly BuildingTargetMoveOrderSystem _buildingTargetMoveOrderSystem = new();
    private readonly TransportBoardingCommandSystem _transportBoardingCommandSystem = new();
    private readonly FocusableUnitLookupSystem _focusableUnitLookupSystem = new();
    private UnitTransportBoardingSystem _unitTransportBoardingSystem;
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
    private BuildingPlacementInteractionSystem _buildingPlacementInteractionSystem;
    private BuildingPlacementInteractionSystem.Context _buildingPlacementInteractionContext;
    private World _queryWorld;
    private EntityQuery _selectedMoveQuery;
    private EntityQuery _gridPathingQuery;
    private EntityQuery _gridConfigQuery;
    private EntityQuery _selectedTagQuery;
    private readonly List<SelectionUiQuerySystem.TransportPassengerUiInfo> _selectionUiPassengerScratch = new();
    private readonly List<Entity> _visibleSelectionScratch = new();
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
            if (World.DefaultGameObjectInjectionWorld == null)
                return false;

            var em = World.DefaultGameObjectInjectionWorld.EntityManager;
            return _focusedUnitLifecycleSystem.TryGetFocusedUnitEntity(em, _selectionStateSystem, out Entity focusedUnit) &&
                   _selectionUiQuerySystem.HasFocusedUnit(em, focusedUnit);
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
            _selectionStateSystem.FocusedUnit,
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
        BuildingPlacementInteractionSystem buildingPlacementInteractionSystem,
        FactionVisualSettings factionVisualSettings)
    {
        Init(
            configAsset,
            sceneWorldCamera,
            runtimeRoot,
            mainMenuPlayUi,
            roadBuildController,
            buildingPlacementInteractionSystem,
            default,
            factionVisualSettings);
    }

    public void Init(
        RTSSelectionSystemConfig configAsset,
        Camera sceneWorldCamera,
        Transform runtimeRoot,
        MainMenuPlayUI mainMenuPlayUi,
        RoadBuildSystem roadBuildController,
        BuildingPlacementInteractionSystem buildingPlacementInteractionSystem,
        BuildingPlacementInteractionSystem.Context buildingPlacementInteractionContext,
        FactionVisualSettings factionVisualSettings)
    {
        config = configAsset;
        worldCamera = sceneWorldCamera;
        _runtimeRoot = runtimeRoot;
        _mainMenuPlayUi = mainMenuPlayUi;
        _roadBuildController = roadBuildController;
        _buildingPlacementInteractionSystem = buildingPlacementInteractionSystem;
        _buildingPlacementInteractionContext = buildingPlacementInteractionContext;
        _selectionHudFeedbackSystem.ResetBridgeCache();
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

        _selectionOrderMarkerSystem.Initialize(
            moveOrderMarkerPrefab,
            attackOrderMarkerPrefab,
            orderMarkerVisibleSeconds,
            _runtimeRoot);
    }

    public void BindDependencies(
        MainMenuPlayUI mainMenuPlayUi,
        RoadBuildSystem roadBuildController,
        BuildingPlacementInteractionSystem buildingPlacementInteractionSystem,
        BuildingPlacementInteractionSystem.Context buildingPlacementInteractionContext)
    {
        _mainMenuPlayUi = mainMenuPlayUi;
        _roadBuildController = roadBuildController;
        _buildingPlacementInteractionSystem = buildingPlacementInteractionSystem;
        _buildingPlacementInteractionContext = buildingPlacementInteractionContext;
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

    private void ApplyHudSelection(EntityManager em, Entity entity)
    {
        _selectionHudFeedbackSystem.ApplySelection(em, entity, _selectionUiQuerySystem);
    }

    private void ApplyHudSquadSelection(int selectedCount)
    {
        _selectionHudFeedbackSystem.ApplySquadSelection(selectedCount);
    }

    private void ClearHudSelection()
    {
        _selectionHudFeedbackSystem.ClearSelection();
    }

    private void ApplyHudCommandMode(TacticalCommandMode mode)
    {
        _selectionHudFeedbackSystem.ApplyCommandMode(mode);
    }

    private void ClearHudCommandMode()
    {
        _selectionHudFeedbackSystem.ClearCommandMode();
    }

    private void ApplyHudCommandResult(TacticalCommandResult result)
    {
        _selectionHudFeedbackSystem.ApplyCommandResult(result);
    }

    private void SetHudWorldMarkersVisible(bool visible)
    {
        _selectionHudFeedbackSystem.SetWorldMarkersVisible(visible);
    }

    public void Dispose()
    {
        if (_pixel != null)
            Destroy(_pixel);
        _selectionOrderMarkerSystem.Dispose();
    }

    private void EnsureEntityQueries(EntityManager em)
    {
        World world = em.World;
        if (_queryWorld == world && world != null && world.IsCreated)
        {
            _focusableUnitLookupSystem.EnsureEntityQueries(em);
            _visibleUnitSelectionSystem.EnsureEntityQueries(em);
            _attackOrderCommandSystem.EnsureEntityQueries(em);
            _selectionOrderMarkerSystem.EnsureEntityQueries(em);
            _focusedUnitCommandSystem.EnsureEntityQueries(em);
            _focusedUnitLifecycleSystem.EnsureEntityQueries(em);
            _selectedUnitOrderSnapshotSystem.EnsureEntityQueries(em);
            _buildingTargetMoveOrderSystem.EnsureEntityQueries(em);
            _transportBoardingCommandSystem.EnsureEntityQueries(em);
            return;
        }

        _queryWorld = world;
        _focusableUnitLookupSystem.EnsureEntityQueries(em);
        _visibleUnitSelectionSystem.EnsureEntityQueries(em);
        _attackOrderCommandSystem.EnsureEntityQueries(em);
        _selectionOrderMarkerSystem.EnsureEntityQueries(em);
        _focusedUnitCommandSystem.EnsureEntityQueries(em);
        _focusedUnitLifecycleSystem.EnsureEntityQueries(em);
        _selectedUnitOrderSnapshotSystem.EnsureEntityQueries(em);
        _buildingTargetMoveOrderSystem.EnsureEntityQueries(em);
        _transportBoardingCommandSystem.EnsureEntityQueries(em);
        _selectedMoveQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<SelectedUnitTag>(),
            ComponentType.ReadOnly<UnitGrid>(),
            ComponentType.ReadOnly<UnitMove>());
        _gridPathingQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<GridConfig>(),
            ComponentType.ReadOnly<GridWalkable>(),
            ComponentType.ReadOnly<DynamicBlockerData>(),
            ComponentType.ReadOnly<DynamicOccupancyData>());
        _gridConfigQuery = em.CreateEntityQuery(ComponentType.ReadOnly<GridConfig>());
        _selectedTagQuery = em.CreateEntityQuery(ComponentType.ReadOnly<SelectedUnitTag>());
    }

    public void Update()
    {
        ProcessQueuedMoveOrder();
        RefreshFocusedUnit();
        _selectionOrderMarkerSystem.UpdateMoveOrderMarkerVisibility(SetHudWorldMarkersVisible);
        _selectionOrderMarkerSystem.UpdateAttackOrderMarkerVisibility(SetHudWorldMarkersVisible);

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
            _rtsCameraSystem.UpdateFullscreenIsoCameraMode(worldCamera, _fullscreenIsoTargetHeight, _fullscreenIsoTargetOrthographicSize, fullscreenIsoPitch, fullscreenIsoYaw, zoomTransitionSmoothTime);
            HandleFullscreenIsoCameraPan();
            return;
        }

        if (_runtimeGameplayStateSystem.FullscreenMapOpen)
            return;

        if (_runtimeGameplayStateSystem.BuildModeActive)
        {
            if (_rtsCameraSystem.NormalIsoModeActive)
                ExitNormalIsoMode();
            UpdateBuildModeCameraTransition();
            UpdateSmoothCameraFocus();
            HandleBuildModeCameraPan();
            return;
        }

        if (worldCamera == null)
            return;

        if (_rtsCameraSystem.NormalIsoModeActive)
        {
            UpdateFullscreenIsoZoom();
            _rtsCameraSystem.UpdateFullscreenIsoCameraMode(worldCamera, _fullscreenIsoTargetHeight, _fullscreenIsoTargetOrthographicSize, fullscreenIsoPitch, fullscreenIsoYaw, zoomTransitionSmoothTime);
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
        bool hasPendingBuildingPlacement = _buildingPlacementInteractionSystem != null &&
                                           _buildingPlacementInteractionSystem.HasPendingBuildingPlacement(_buildingPlacementInteractionContext);
        bool roadToolActive = _roadBuildController != null && _roadBuildController.IsRoadBuildModeActive;
        bool idleBuildMode = !hasPendingBuildingPlacement && !roadToolActive;
        bool interactionActive =
            (_roadBuildController != null && _roadBuildController.IsDraggingBuildInteraction) ||
            (_buildingPlacementInteractionSystem != null &&
             _buildingPlacementInteractionSystem.IsDraggingPlacementPreview(_buildingPlacementInteractionContext));

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
        SelectUnitsInRectangle(screenRect, VisibleUnitSelectionSystem.Filter.All);
    }

    public bool HasVisiblePlayerUnits()
    {
        return HasVisiblePlayerUnits(VisibleUnitSelectionSystem.Filter.All);
    }

    public bool HasVisiblePlayerSoldiers()
    {
        return HasVisiblePlayerUnits(VisibleUnitSelectionSystem.Filter.Soldiers);
    }

    public bool HasVisiblePlayerVehicles()
    {
        return HasVisiblePlayerUnits(VisibleUnitSelectionSystem.Filter.Vehicles);
    }

    private bool HasVisiblePlayerUnits(VisibleUnitSelectionSystem.Filter filter)
    {
        if (worldCamera == null || World.DefaultGameObjectInjectionWorld == null)
            return false;

        var em = World.DefaultGameObjectInjectionWorld.EntityManager;
        EnsureEntityQueries(em);
        Rect screenRect = new(0f, 0f, Screen.width, Screen.height);
        return _visibleUnitSelectionSystem.HasVisiblePlayerUnits(
            em,
            worldCamera,
            _selectionUiQuerySystem,
            screenRect,
            filter);
    }

    private void SelectUnitsInRectangle(Rect screenRect, VisibleUnitSelectionSystem.Filter filter)
    {
        if (World.DefaultGameObjectInjectionWorld == null)
            return;

        var em = World.DefaultGameObjectInjectionWorld.EntityManager;
        EnsureEntityQueries(em);
        int selectedCount = _visibleUnitSelectionSystem.CollectVisiblePlayerUnits(
            em,
            worldCamera,
            _selectionUiQuerySystem,
            screenRect,
            filter,
            _visibleSelectionScratch);

        ClearCurrentSelection(em, "SelectUnitsInRectangle");
        _visibleUnitSelectionSystem.ApplySelectedUnitTags(em, _visibleSelectionScratch);
        CacheSelectedMoveEntities(em, _visibleSelectionScratch);
        LogSelectionDiagnostic($"result=SelectRectangle filter={filter} selected={selectedCount} cache={_cachedSelectedMoveEntities.Count}");

        Entity focusedUnit = _focusedUnitLifecycleSystem.ApplySelectionFocus(
            em,
            _selectionStateSystem,
            _visibleSelectionScratch,
            selectedCount,
            ApplyHudSelection,
            ApplyHudSquadSelection);
        if (focusedUnit != Entity.Null)
        {
            _buildingPlacementInteractionSystem?.ClearSelectedBuilding(_buildingPlacementInteractionContext, "RTSSelection.SelectUnitsInRectangle");
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
        SelectedMoveOrderCommandSystem.Result result = _selectedMoveOrderCommandSystem.TryIssueMoveOrder(
            em,
            screenPosition,
            _selectedMoveQuery,
            _gridConfigQuery,
            _unitMoveOrderSystem,
            _selectionOrderMarkerSystem,
            TryGetClickedUnitEntity,
            TryGetClickedCell,
            Time.frameCount);

        ApplyHudCommandResult(result.CommandResult);
        ClearHudCommandMode();
        if (result.EmitScreenMarker)
            MoveOrderScreenMarkerRequested?.Invoke(screenPosition);
        if (result.ShowWorldMarkers)
            SetHudWorldMarkersVisible(true);
    }

    private bool TryIssueBoardTransportOrderToClickedUnit(Vector2 screenPosition)
    {
        if (World.DefaultGameObjectInjectionWorld == null)
            return false;

        var em = World.DefaultGameObjectInjectionWorld.EntityManager;
        EnsureEntityQueries(em);
        TransportBoardingCommandSystem.Result result = _transportBoardingCommandSystem.TryIssueBoardTransportOrderToClickedUnit(
            em,
            screenPosition,
            _unitTransportBoardingSystem,
            _unitMoveOrderSystem,
            _selectionStateSystem,
            TryGetClickedUnitEntity,
            TryGetClickedCell);
        if (!result.Accepted)
            return false;

        _selectionOrderMarkerSystem.ShowMoveOrderMarker(em, result.MarkerCell, result.MarkerPosition, result.MarkerFactionId);
        MoveOrderScreenMarkerRequested?.Invoke(screenPosition);
        ClearCurrentSelection(em, "BoardTransportOrderIssued");
        _focusedUnitLifecycleSystem.ClearFocusedUnit(_selectionStateSystem);
        _cameraDragging = false;
        return true;
    }

    public bool IsBoardablePlayerTransportClick(Vector2 screenPosition)
    {
        if (World.DefaultGameObjectInjectionWorld == null)
            return false;

        var em = World.DefaultGameObjectInjectionWorld.EntityManager;
        EnsureEntityQueries(em);
        return _transportBoardingCommandSystem.IsBoardablePlayerTransportClick(
            em,
            screenPosition,
            _unitTransportBoardingSystem,
            TryGetClickedUnitEntity,
            TryGetClickedCell);
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

    private void CacheSelectedMoveEntities(EntityManager em, List<Entity> entities)
    {
        _selectionStateSystem.CacheSelectedMoveEntities(em, entities);
    }

    private void CacheSelectedMoveEntity(EntityManager em, Entity entity)
    {
        _selectionStateSystem.CacheSelectedMoveEntity(em, entity);
    }

    public bool TryIssueMoveOrderToBuilding(Vector2Int originCell, Vector2Int footprintCells)
    {
        if (World.DefaultGameObjectInjectionWorld == null)
            return false;

        var em = World.DefaultGameObjectInjectionWorld.EntityManager;
        bool issued = _buildingTargetMoveOrderSystem.TryIssueMoveOrderToBuilding(em, originCell, footprintCells);
        if (!issued)
            return false;

        ClearCurrentSelection(em, "MoveOrderToBuilding");
        _focusedUnitLifecycleSystem.ClearFocusedUnit(_selectionStateSystem);
        if (TryGetPointerPosition(out Vector2 markerScreenPosition))
            MoveOrderScreenMarkerRequested?.Invoke(markerScreenPosition);
        return true;
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

            if (_rtsCameraSystem.UpdatePerspectiveCameraMode(worldCamera, targetHeight, targetPitch, targetYaw, targetFieldOfView, zoomTransitionSmoothTime))
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

        _rtsCameraSystem.UpdatePerspectiveCameraMode(worldCamera, buildModeZoomHeight, buildModePitch, buildModeYaw, buildModeFieldOfView, zoomTransitionSmoothTime);
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
            Vector3 focusWorldPosition = worldCamera != null ? _rtsCameraSystem.GetCameraGroundCenterWorld(worldCamera) : Vector3.zero;
            _rtsCameraSystem.ApplyPerspectiveCameraModeInstant(worldCamera, normalModeZoomHeight, normalModePitch, normalModeYaw, normalModeFieldOfView);
            if (worldCamera != null)
                _rtsCameraSystem.MoveCameraGroundCenterTo(worldCamera, focusWorldPosition);
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

        _rtsCameraSystem.MoveCameraGroundCenterTo(worldCamera, _runtimeGameplayStateSystem.InitialCameraFocusWorld);
        _runtimeGameplayStateSystem.InitialCameraFocusRequested = false;
        _rtsCameraSystem.ClearSmoothFocusTarget();
    }

    private void UpdateSmoothCameraFocus()
    {
        if (!_rtsCameraSystem.HasSmoothFocusTarget || worldCamera == null)
            return;

        Vector3 currentGroundCenter = _rtsCameraSystem.GetCameraGroundCenterWorld(worldCamera);
        Vector3 smoothedCenter = _rtsCameraSystem.UpdateSmoothFocus(currentGroundCenter, zoomTransitionSmoothTime);
        _rtsCameraSystem.MoveCameraGroundCenterTo(worldCamera, smoothedCenter);
    }

    public void EnterFullscreenMapIsoMode(Vector3 focusWorldPosition)
    {
        if (worldCamera == null)
            return;

        _fullscreenIsoTargetHeight = Mathf.Clamp(fullscreenIsoZoomHeight, minZoomHeight, maxZoomHeight);
        _fullscreenIsoTargetOrthographicSize = Mathf.Clamp(fullscreenIsoOrthographicSize, 8f, 48f);
        _rtsCameraSystem.MoveCameraGroundCenterTo(worldCamera, focusWorldPosition);
        _rtsCameraSystem.ApplyFullscreenIsoCameraModeInstant(worldCamera, _fullscreenIsoTargetHeight, _fullscreenIsoTargetOrthographicSize, fullscreenIsoPitch, fullscreenIsoYaw);
        _runtimeGameplayStateSystem.FullscreenMapIsoMode = true;
        _runtimeGameplayStateSystem.FullscreenMapOpen = true;
        _cameraDragging = false;
    }

    public void ExitFullscreenMapIsoMode()
    {
        if (worldCamera != null)
            _rtsCameraSystem.ApplyPerspectiveCameraModeInstant(worldCamera, normalModeZoomHeight, normalModePitch, normalModeYaw, normalModeFieldOfView);

        _runtimeGameplayStateSystem.FullscreenMapIsoMode = false;
        _cameraDragging = false;
    }

    public bool IsNormalIsoModeActive => _rtsCameraSystem.NormalIsoModeActive;

    public void ToggleNormalIsoMode()
    {
        if (_rtsCameraSystem.NormalIsoModeActive)
            ExitNormalIsoMode();
        else
            EnterNormalIsoMode();
    }

    public void EnterNormalIsoMode()
    {
        if (worldCamera == null)
            return;

        Vector3 focusWorldPosition = _rtsCameraSystem.GetCameraGroundCenterWorld(worldCamera);
        float currentGroundSpan = _rtsCameraSystem.GetVisibleGroundVerticalSpan(worldCamera);
        float currentHeight = Mathf.Clamp(worldCamera.transform.position.y, minZoomHeight, maxZoomHeight);
        _fullscreenIsoTargetHeight = currentHeight;
        _fullscreenIsoTargetOrthographicSize = Mathf.Clamp(
            _rtsCameraSystem.CalculateOrthographicSizeForGroundSpan(
                worldCamera,
                currentGroundSpan,
                _fullscreenIsoTargetHeight,
                fullscreenIsoPitch,
                fullscreenIsoYaw,
                fullscreenIsoOrthographicSize),
            8f,
            48f);
        _rtsCameraSystem.ApplyFullscreenIsoCameraModeInstant(worldCamera, _fullscreenIsoTargetHeight, _fullscreenIsoTargetOrthographicSize, fullscreenIsoPitch, fullscreenIsoYaw);
        _rtsCameraSystem.MoveCameraGroundCenterTo(worldCamera, focusWorldPosition);
        _rtsCameraSystem.NormalIsoModeActive = true;
        _cameraDragging = false;
    }

    public void ExitNormalIsoMode()
    {
        Vector3 focusWorldPosition = worldCamera != null ? _rtsCameraSystem.GetCameraGroundCenterWorld(worldCamera) : Vector3.zero;
        if (worldCamera != null)
        {
            float currentGroundSpan = _rtsCameraSystem.GetVisibleGroundVerticalSpan(worldCamera);
            float targetHeight = _rtsCameraSystem.CalculatePerspectiveHeightForGroundSpan(
                worldCamera,
                currentGroundSpan,
                normalModePitch,
                normalModeYaw,
                normalModeFieldOfView,
                minZoomHeight,
                maxZoomHeight,
                normalModeZoomHeight);
            _rtsCameraSystem.ApplyPerspectiveCameraModeInstant(worldCamera, targetHeight, normalModePitch, normalModeYaw, normalModeFieldOfView);
            _rtsCameraSystem.MoveCameraGroundCenterTo(worldCamera, focusWorldPosition);
        }

        _rtsCameraSystem.NormalIsoModeActive = false;
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

    private void ClearCurrentSelection(EntityManager em, string reason = "Unspecified")
    {
        _focusedUnitLifecycleSystem.ClearCurrentSelection(
            em,
            _selectionStateSystem,
            reason,
            LogSelectionDiagnostic,
            ClearHudSelection);
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
        _focusedUnitLifecycleSystem.ClearFocusedUnit(_selectionStateSystem);
        _explicitAttackTargetModeActive = false;
        ClearHudSelection();
        ClearHudCommandMode();
        SetHudWorldMarkersVisible(false);
    }

    public void DeselectAllUnits(string reason = "DeselectAllUnits")
    {
        if (World.DefaultGameObjectInjectionWorld == null)
        {
            _focusedUnitLifecycleSystem.ClearFocusedUnit(_selectionStateSystem);
            _explicitAttackTargetModeActive = false;
            ClearHudSelection();
            ClearHudCommandMode();
            SetHudWorldMarkersVisible(false);
            return;
        }

        var em = World.DefaultGameObjectInjectionWorld.EntityManager;
        ClearCurrentSelection(em, reason);
        _focusedUnitLifecycleSystem.ClearFocusedUnit(_selectionStateSystem);
        _explicitAttackTargetModeActive = false;
        ClearHudSelection();
        ClearHudCommandMode();
        SetHudWorldMarkersVisible(false);
    }

    public void SelectAllVisiblePlayerUnits()
    {
        SelectAllVisiblePlayerUnits(VisibleUnitSelectionSystem.Filter.All);
    }

    public void SelectAllVisiblePlayerSoldiers()
    {
        SelectAllVisiblePlayerUnits(VisibleUnitSelectionSystem.Filter.Soldiers);
    }

    public void SelectAllVisiblePlayerVehicles()
    {
        SelectAllVisiblePlayerUnits(VisibleUnitSelectionSystem.Filter.Vehicles);
    }

    private void SelectAllVisiblePlayerUnits(VisibleUnitSelectionSystem.Filter filter)
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
        if (!_focusedUnitLifecycleSystem.FocusUnitEntity(
                em,
                entity,
                _selectionStateSystem,
                _unitTargetOrderSystem,
                "FocusUnitEntity",
                "FocusUnitEntity",
                LogSelectionDiagnostic,
                DescribeTransportBoardingEntity,
                ClearHudSelection,
                ApplyHudSelection))
        {
            return false;
        }

        _buildingPlacementInteractionSystem?.ClearSelectedBuilding(_buildingPlacementInteractionContext, "RTSSelection.FocusUnitEntity");
        _ignoreNextLeftMouseRelease = true;
        _ignoreWorldCommandsUntilFrame = Time.frameCount + 1;
        _runtimeGameplayStateSystem.SuppressNextWorldClick = true;
        _cameraDragging = false;
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
        AttackOrderCommandSystem.Result issueResult =
            _attackOrderCommandSystem.IssueAttackTarget(em, targetEntity, _unitTargetOrderSystem);
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
        if (World.DefaultGameObjectInjectionWorld == null)
        {
            _selectedUnitOrderSnapshotSystem.Clear();
            return;
        }

        var em = World.DefaultGameObjectInjectionWorld.EntityManager;
        _selectedUnitOrderSnapshotSystem.PreserveSelectedUnitOrders(em);
    }

    public void RestorePreservedUnitOrders()
    {
        if (World.DefaultGameObjectInjectionWorld == null)
        {
            _selectedUnitOrderSnapshotSystem.Clear();
            return;
        }

        var em = World.DefaultGameObjectInjectionWorld.EntityManager;
        _selectedUnitOrderSnapshotSystem.RestorePreservedUnitOrders(em);
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

        _focusedUnitCommandSystem.DestroyFocusedUnit(em, entity);
        _focusedUnitLifecycleSystem.ClearFocusedUnit(_selectionStateSystem);
    }

    public void ReturnFocusedUnitToBase()
    {
        if (!TryGetFocusedUnitEntity(out var em, out Entity entity) || !FocusedUnitOwnedByPlayer)
            return;

        _focusedUnitCommandSystem.ReturnFocusedUnitToBase(em, entity, _unitMoveOrderSystem);
    }

    public void EnableFocusedUnitAutoAttack()
    {
        if (!TryGetFocusedUnitEntity(out var em, out Entity entity) || !FocusedUnitOwnedByPlayer)
            return;

        _focusedUnitCommandSystem.EnableFocusedUnitAutoAttack(em, entity, _unitTargetOrderSystem);
    }

    public bool IssueFocusedMissileLauncherRadarAttack()
    {
        if (!TryGetFocusedUnitEntity(out var em, out Entity launcher) || !FocusedUnitOwnedByPlayer)
            return false;
        if (!_focusedUnitCommandSystem.TryIssueFocusedMissileLauncherRadarAttack(
                em,
                launcher,
                _unitTargetOrderSystem,
                out float3 targetPosition))
        {
            return false;
        }

        _selectionOrderMarkerSystem.ShowAttackOrderMarker(em, targetPosition);
        ClearCurrentSelection(em, "MissileLauncherRadarAttack");
        _focusedUnitLifecycleSystem.SetFocusedUnit(_selectionStateSystem, launcher);
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
        bool issued = _focusedUnitCommandSystem.IssueImmediateSelectedUnitOrder(
            em,
            clearEngageTarget,
            _unitMoveOrderSystem);
        if (!issued)
        {
            ApplyHudCommandResult(TacticalCommandResult.Rejected(TacticalCommandReasonCode.NoSelection));
            return false;
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
        _focusedUnitLifecycleSystem.RefreshFocusedUnit(em, _selectionStateSystem, ApplyHudSelection);
    }

    private bool TryFocusUnit(Vector2 screenPosition)
    {
        if (World.DefaultGameObjectInjectionWorld == null)
            return false;

        var em = World.DefaultGameObjectInjectionWorld.EntityManager;
        EnsureEntityQueries(em);
        if (!_focusedUnitLifecycleSystem.TryFocusUnit(
                em,
                screenPosition,
                _selectionStateSystem,
                _unitTargetOrderSystem,
                TryGetClickedUnitEntity,
                "TryFocusUnit",
                "TryFocusUnit",
                LogSelectionDiagnostic,
                DescribeTransportBoardingEntity,
                ClearHudSelection,
                ApplyHudSelection,
                out _))
        {
            return false;
        }

        _buildingPlacementInteractionSystem?.ClearSelectedBuilding(_buildingPlacementInteractionContext, "RTSSelection.TryFocusUnit");
        _ignoreNextLeftMouseRelease = true;
        _ignoreWorldCommandsUntilFrame = Time.frameCount + 1;
        _runtimeGameplayStateSystem.SuppressNextWorldClick = true;
        _cameraDragging = false;
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
        AttackOrderCommandSystem.Result issueResult = _attackOrderCommandSystem.TryIssueAttackOrderToClickedUnit(
            em,
            screenPosition,
            _unitTargetOrderSystem,
            TryGetClickedUnitEntity,
            _buildingPlacementInteractionSystem,
            _buildingPlacementInteractionContext,
            _explicitAttackTargetModeActive);
        if (!issueResult.Issued)
        {
            if (issueResult.HasCommandResult)
                ApplyHudCommandResult(issueResult.CommandResult);
            return false;
        }

        _selectionOrderMarkerSystem.ShowAttackOrderMarker(em, issueResult.TargetPosition);
        AttackOrderScreenMarkerRequested?.Invoke(screenPosition);
        ClearCurrentSelection(em, "AttackOrderIssued");
        _focusedUnitLifecycleSystem.ClearFocusedUnit(_selectionStateSystem);
        _cameraDragging = false;
        ApplyHudCommandResult(TacticalCommandResult.Success());
        ClearHudCommandMode();
        SetHudWorldMarkersVisible(true);
        return true;
    }

    private bool TryGetClickedUnitEntity(Vector2 screenPosition, EntityManager em, out Entity bestEntity)
    {
        bestEntity = Entity.Null;
        if (!TryGetClickedCell(screenPosition, em, out var clickedCell, out _))
            return false;

        return _focusableUnitLookupSystem.TryGetClickedUnitEntity(
            em,
            worldCamera,
            clickedCell,
            screenPosition,
            out bestEntity);
    }

    private bool TryGetFocusedUnitEntity(out EntityManager em, out Entity entity)
    {
        em = default;
        entity = Entity.Null;

        if (World.DefaultGameObjectInjectionWorld == null)
            return false;

        em = World.DefaultGameObjectInjectionWorld.EntityManager;
        return _focusedUnitLifecycleSystem.TryGetFocusedUnitEntity(em, _selectionStateSystem, out entity);
    }

}
