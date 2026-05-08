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
    private const int TransportBoardingClearanceCells = 4;
    private const int TransportDisembarkClearanceCells = 4;
    private const int AirTransportDirectBoardingCells = 1;
    private const float AirTransportBoardingGroundedHeightTolerance = 3f;
    private const int ManualMoveGoalSearchRadiusInfantry = 12;
    private const int ManualMoveGoalSearchRadiusVehicle = 20;
    private const int ManualMoveGoalPaddingInfantry = 1;
    private const int ManualMoveGoalPaddingVehicle = 0;
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

    public static RTSSelectionSystem Instance { get; private set; }
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

    private Vector2 _dragStart;
    private Vector2 _dragCurrent;
    private Vector2 _lastPointerPosition;
    private bool _pointerPressedOverUi;
    private bool _dragging;
    private bool _cameraDragging;
    private bool _ignoreNextLeftMouseRelease;
    private bool _skipNextWorldReleaseAfterSelection;
    private int _ignoreWorldCommandsUntilFrame;
    private bool _ignoreUiClickUntilRelease;
    private bool _selectionModeHoldArmed;
    private float _selectionModeHoldStartTime;
    private bool _wasPlayRequested;
    private bool _wasBuildModeActive;
    private bool _isZoomTransitionActive;
    private float _zoomTransitionVelocity;
    private float _pitchTransitionVelocity;
    private float _yawTransitionVelocity;
    private float _fieldOfViewTransitionVelocity;
    private float _fullscreenIsoTargetHeight;
    private float _fullscreenIsoTargetOrthographicSize;
    private float _orthographicSizeTransitionVelocity;
    private bool _normalIsoModeActive;
    private bool _hasSmoothCameraFocusTarget;
    private Vector3 _smoothCameraFocusTarget;
    private Vector3 _smoothCameraFocusVelocity;
    private Texture2D _pixel;
    private Entity _focusedUnit;
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
    private readonly List<Entity> _cachedSelectedMoveEntities = new();
    private readonly List<Entity> _selectedBoardingSourceEntities = new();
    private uint _queuedMoveOrderToken;
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
    private bool _hasQueuedMoveOrder;
    private Vector2 _queuedMoveOrderScreenPosition;
    private int _queuedMoveOrderFrame = -1;
    private bool _explicitAttackTargetModeActive;
    private float _selectionModeHoldSeconds = 1f;
    private Rect _lastLiveSelectionRect;
    private bool _hasLiveSelectionRect;
    private Vector2 _lastKnownPointerPosition;
    private bool _hasLastKnownPointerPosition;

    public bool HasFocusedUnit
    {
        get
        {
            if (_focusedUnit == Entity.Null || World.DefaultGameObjectInjectionWorld == null)
                return false;

            var em = World.DefaultGameObjectInjectionWorld.EntityManager;
            return em.Exists(_focusedUnit) && em.HasComponent<Faction>(_focusedUnit);
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
            return !_selectedTagQuery.IsEmptyIgnoreFilter;
        }
    }

    public string FocusedUnitLabel
    {
        get
        {
            if (!TryGetFocusedUnitEntity(out var em, out Entity entity))
                return "Unit";

            return ResolveFocusedUnitName(em, entity);
        }
    }

    public string FocusedUnitDescription
    {
        get
        {
            if (!TryGetFocusedUnitEntity(out var em, out Entity entity))
                return "Select a unit to inspect it.";

            if (em.HasComponent<UnitDisplayInfo>(entity))
            {
                string configuredDescription = em.GetComponentData<UnitDisplayInfo>(entity).Description.ToString();
                if (!string.IsNullOrWhiteSpace(configuredDescription))
                    return configuredDescription;
            }

            byte factionId = em.HasComponent<Faction>(entity) ? em.GetComponentData<Faction>(entity).Id : (byte)0;
            bool movable = em.HasComponent<UnitMove>(entity);
            bool isVehicle = IsVehicleForVisibleSelection(em, entity);
            bool canAttack = em.HasComponent<UnitCombat>(entity) && em.GetComponentData<UnitCombat>(entity).CanAttack != 0;

            if (factionId == 0)
            {
                if (!movable)
                    return "Player-controlled unit.";
                if (isVehicle)
                    return canAttack
                        ? "Heavy combat APC. Faster than the base APC and can attack enemies."
                        : "Support APC vehicle. Mobile but cannot attack, and will retreat when attacked.";

                return "Player soldier. Click ground to issue a move order.";
            }

            if (!movable)
                return "Enemy unit. Read-only info.";
            if (isVehicle)
                return canAttack ? "Enemy combat vehicle." : "Enemy support vehicle.";
            return "Enemy mobile unit. Read-only info.";
        }
    }

    public string FocusedUnitHealthText
    {
        get
        {
            if (!TryGetFocusedUnitEntity(out var em, out Entity entity) || !em.HasComponent<UnitHealth>(entity))
                return "Health: -";

            UnitHealth health = em.GetComponentData<UnitHealth>(entity);
            return $"Health: {health.Current}/{health.Max}";
        }
    }

    public bool TryGetFocusedUnitHealth(out int current, out int max)
    {
        current = 0;
        max = 0;

        if (!TryGetFocusedUnitEntity(out var em, out Entity entity) || !em.HasComponent<UnitHealth>(entity))
            return false;

        UnitHealth health = em.GetComponentData<UnitHealth>(entity);
        current = health.Current;
        max = health.Max;
        return true;
    }

    public bool TryGetFocusedUnitCapacityInfo(out int current, out int max, out float progress01)
    {
        current = 0;
        max = 0;
        progress01 = 0f;

        if (!TryGetFocusedUnitEntity(out var em, out Entity entity) || !em.HasComponent<UnitResourceHauler>(entity))
            return false;

        UnitResourceHauler hauler = em.GetComponentData<UnitResourceHauler>(entity);
        max = Mathf.Max(0, hauler.BarrelCapacity);
        if (max <= 0)
            return false;

        float cargo = Mathf.Clamp(hauler.CargoOilBarrels + hauler.CargoFuelBarrels, 0f, max);

        if (em.HasComponent<UnitResourceHaulOrder>(entity))
        {
            const byte LoadingPhase = 2;
            const byte UnloadingPhase = 4;

            UnitResourceHaulOrder order = em.GetComponentData<UnitResourceHaulOrder>(entity);
            if (order.ActionEndsAt > 0f)
            {
                if (order.Phase == LoadingPhase && hauler.FillDurationSeconds > 0.01f)
                {
                    float startedAt = order.ActionEndsAt - hauler.FillDurationSeconds;
                    float fill01 = Mathf.Clamp01((Time.time - startedAt) / hauler.FillDurationSeconds);
                    cargo = Mathf.Max(cargo, fill01 * max);
                }
                else if (order.Phase == UnloadingPhase && hauler.UnloadDurationSeconds > 0.01f)
                {
                    float startedAt = order.ActionEndsAt - hauler.UnloadDurationSeconds;
                    float unload01 = Mathf.Clamp01((Time.time - startedAt) / hauler.UnloadDurationSeconds);
                    cargo = Mathf.Min(cargo, (1f - unload01) * max);
                }
            }
        }

        progress01 = max > 0 ? Mathf.Clamp01(cargo / max) : 0f;
        current = Mathf.Clamp(Mathf.RoundToInt(cargo), 0, max);
        return true;
    }

    public bool FocusedUnitOwnedByPlayer
    {
        get
        {
            if (!TryGetFocusedUnitEntity(out var em, out Entity entity) || !em.HasComponent<Faction>(entity))
                return false;

            return em.GetComponentData<Faction>(entity).Id == 0;
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

            return IsVehicleUnit(em, entity);
        }
    }

    public bool CanReturnFocusedUnitToBase => CanCommandFocusedUnit && !FocusedUnitIsVehicle;

    public bool CanFocusedUnitUseAutoAttack => CanCommandFocusedUnit && !FocusedUnitIsVehicle;

    public bool FocusedUnitCanAttack
    {
        get
        {
            if (!TryGetFocusedUnitEntity(out var em, out Entity entity) || !em.HasComponent<UnitCombat>(entity))
                return false;

            return em.GetComponentData<UnitCombat>(entity).CanAttack != 0;
        }
    }

    public bool ExplicitAttackTargetModeActive => _explicitAttackTargetModeActive;

    public int FocusedTransportPassengerCount
    {
        get
        {
            if (!TryGetFocusedUnitEntity(out var em, out Entity entity) || !TryEnsureTransportCapacity(em, entity))
                return 0;

            return em.GetBuffer<UnitTransportPassengerElement>(entity).Length;
        }
    }

    public bool CanDisembarkFocusedTransport => FocusedTransportPassengerCount > 0;

    public void GetFocusedTransportPassengers(List<TransportPassengerUiInfo> results)
    {
        if (results == null)
            return;

        results.Clear();
        if (!TryGetFocusedUnitEntity(out var em, out Entity transport) ||
            !TryEnsureTransportCapacity(em, transport) ||
            !em.HasBuffer<UnitTransportPassengerElement>(transport))
        {
            return;
        }

        DynamicBuffer<UnitTransportPassengerElement> passengers = em.GetBuffer<UnitTransportPassengerElement>(transport);
        for (int i = 0; i < passengers.Length; i++)
        {
            Entity passenger = passengers[i].Passenger;
            if (!em.Exists(passenger))
                continue;

            int current = 0;
            int max = 0;
            if (em.HasComponent<UnitHealth>(passenger))
            {
                UnitHealth health = em.GetComponentData<UnitHealth>(passenger);
                current = health.Current;
                max = health.Max;
            }

            results.Add(new TransportPassengerUiInfo(passenger, ResolveFocusedUnitName(em, passenger), current, max));
        }
    }

    public void DisembarkFocusedTransport()
    {
        if (!TryGetFocusedUnitEntity(out var em, out Entity transport) ||
            !TryEnsureTransportCapacity(em, transport) ||
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

        if (IsRopeDisembarkTransport(em, transport))
        {
            StartRopeDisembarkTransport(em, transport, referenceCell);
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

            if (!TryFindTransportDisembarkCell(grid, walkable, blocked, occupied, reservedCells, transportCell, transportSize, referenceCell, out int2 cell))
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
            ClearMovementOrderComponents(em, passenger);

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
        if (!TryGetFocusedUnitEntity(out var em, out Entity entity) || !em.HasComponent<LocalToWorld>(entity))
            return false;

        worldPosition = em.GetComponentData<LocalToWorld>(entity).Position;
        return true;
    }

    public bool TryGetFocusedUnitEntityForUi(out Entity entity)
    {
        return TryGetFocusedUnitEntity(out _, out entity);
    }

    public FocusedUnitUiStatus GetFocusedUnitUiStatus()
    {
        if (!TryGetFocusedUnitEntity(out var em, out Entity entity))
            return FocusedUnitUiStatus.Idle;

        if (em.HasComponent<UnitAirState>(entity) && em.GetComponentData<UnitAirState>(entity).ReturningHome != 0)
            return FocusedUnitUiStatus.ReturningToBase;

        if (em.HasComponent<EngageTarget>(entity))
            return FocusedUnitUiStatus.Engaged;

        if (em.HasComponent<UnitTarget>(entity) ||
            em.HasComponent<UnitPathRequest>(entity) ||
            em.HasComponent<UnitPathFollow>(entity) ||
            em.HasComponent<ManualMoveOrderTag>(entity))
        {
            return FocusedUnitUiStatus.Moving;
        }

        return FocusedUnitUiStatus.Idle;
    }

    public bool TryGetFocusedUnitPortraitPose(out Vector3 worldPosition, out Vector3 forward)
    {
        worldPosition = default;
        forward = Vector3.forward;

        if (!TryGetFocusedUnitEntity(out var em, out Entity entity))
            return false;

        if (em.HasComponent<LocalToWorld>(entity))
            worldPosition = em.GetComponentData<LocalToWorld>(entity).Position;
        else if (em.HasComponent<LocalTransform>(entity))
            worldPosition = em.GetComponentData<LocalTransform>(entity).Position;
        else
            return false;

        if (em.HasComponent<LocalTransform>(entity))
        {
            quaternion rotation = em.GetComponentData<LocalTransform>(entity).Rotation;
            float3 facing = math.mul(rotation, new float3(0f, 0f, 1f));
            forward = new Vector3(facing.x, 0f, facing.z);
            if (forward.sqrMagnitude > 0.0001f)
                forward.Normalize();
            else
                forward = Vector3.forward;
        }

        return true;
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
        if (selectedEntities.Length == 0)
            return false;

        Vector3 sum = Vector3.zero;
        int counted = 0;
        Vector3 min = new(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
        Vector3 max = new(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
        Entity forwardEntity = Entity.Null;

        for (int i = 0; i < selectedEntities.Length; i++)
        {
            Entity entity = selectedEntities[i];
            if (!em.Exists(entity) || !em.HasComponent<LocalToWorld>(entity))
                continue;

            Vector3 position = em.GetComponentData<LocalToWorld>(entity).Position;
            sum += position;
            min = Vector3.Min(min, position);
            max = Vector3.Max(max, position);
            counted++;

            if (forwardEntity == Entity.Null)
                forwardEntity = entity;
        }

        if (counted == 0)
            return false;

        centerWorldPosition = sum / counted;
        Vector3 extents = max - min;
        framingRadius = Mathf.Max(1f, Mathf.Max(extents.x, extents.z) * 0.65f);

        Entity poseEntity = HasFocusedUnit && _focusedUnit != Entity.Null ? _focusedUnit : forwardEntity;
        if (poseEntity != Entity.Null && em.Exists(poseEntity) && em.HasComponent<LocalTransform>(poseEntity))
        {
            quaternion rotation = em.GetComponentData<LocalTransform>(poseEntity).Rotation;
            float3 facing = math.mul(rotation, new float3(0f, 0f, 1f));
            forward = new Vector3(facing.x, 0f, facing.z);
            if (forward.sqrMagnitude > 0.0001f)
                forward.Normalize();
            else
                forward = Vector3.forward;
        }

        return true;
    }

    public void GetSelectedUnitEntities(List<Entity> entities)
    {
        if (entities == null)
            return;

        entities.Clear();

        if (World.DefaultGameObjectInjectionWorld == null)
            return;

        var em = World.DefaultGameObjectInjectionWorld.EntityManager;
        EnsureEntityQueries(em);
        using var selectedEntities = _selectedTagQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
        for (int i = 0; i < selectedEntities.Length; i++)
        {
            Entity entity = selectedEntities[i];
            if (!em.Exists(entity))
                continue;

            entities.Add(entity);
        }
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
        if (Instance != null && Instance != this)
            Instance.Dispose();

        Instance = this;
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

    private static string ResolveFocusedUnitName(EntityManager em, Entity entity)
    {
        if (em.HasComponent<UnitDisplayInfo>(entity))
        {
            string configuredName = em.GetComponentData<UnitDisplayInfo>(entity).Name.ToString();
            if (!string.IsNullOrWhiteSpace(configuredName))
                return configuredName;
        }

        byte factionId = em.HasComponent<Faction>(entity) ? em.GetComponentData<Faction>(entity).Id : (byte)0;
        if (!em.HasComponent<UnitMove>(entity))
            return factionId == 0 ? "Player Unit" : "Enemy Unit";

        bool isVehicle = IsVehicleUnit(em, entity);
        if (!isVehicle)
            return factionId == 0 ? "Soldier" : "Enemy Soldier";

        bool canAttack = em.HasComponent<UnitCombat>(entity) && em.GetComponentData<UnitCombat>(entity).CanAttack != 0;
        if (canAttack)
            return factionId == 0 ? "Heavy APC" : "Enemy Heavy APC";

        float speed = em.HasComponent<UnitMove>(entity) ? em.GetComponentData<UnitMove>(entity).Speed : 0f;
        if (speed >= 10.5f)
            return factionId == 0 ? "APC 02" : "Enemy APC 02";

        return factionId == 0 ? "APC 01" : "Enemy APC 01";
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

        bridge.ApplySelection(ResolveFocusedUnitName(em, entity), ResolveHudSelectionStatus(em, entity));
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

    private static string ResolveHudSelectionStatus(EntityManager em, Entity entity)
    {
        var parts = new List<string>();

        if (em.HasComponent<Faction>(entity))
            parts.Add(em.GetComponentData<Faction>(entity).Id == 0 ? "PLAYER" : "ENEMY");

        if (em.HasComponent<UnitHealth>(entity))
        {
            UnitHealth health = em.GetComponentData<UnitHealth>(entity);
            parts.Add($"HP {health.Current}/{health.Max}");
        }

        if (em.HasComponent<EngageTarget>(entity))
            parts.Add("ENGAGED");
        else if (em.HasComponent<UnitTarget>(entity) || em.HasComponent<UnitPathRequest>(entity) || em.HasComponent<UnitPathFollow>(entity))
            parts.Add("MOVING");
        else
            parts.Add("READY");

        return string.Join(" / ", parts);
    }

    private static bool IsVehicleUnit(EntityManager em, Entity entity)
    {
        if (!em.HasComponent<UnitFootprint>(entity) || !em.HasComponent<UnitMovementBehavior>(entity))
            return false;

        return UnitVehicleMovementUtility.IsVehicle(
            em.GetComponentData<UnitFootprint>(entity),
            em.GetComponentData<UnitMovementBehavior>(entity));
    }

    private static bool IsVehicleForVisibleSelection(EntityManager em, Entity entity)
    {
        if (em.HasComponent<UnitSourcePrefabKey>(entity))
        {
            string sourceKey = em.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString();
            if (sourceKey.StartsWith("Unit_Veh_", System.StringComparison.OrdinalIgnoreCase))
                return true;
            if (sourceKey.StartsWith("Unit_Chr_", System.StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return IsVehicleUnit(em, entity);
    }

    public void Dispose()
    {
        if (Instance == this)
            Instance = null;
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

        if (!InitialUnitsRuntimeState.PlayRequested)
        {
            _wasPlayRequested = false;
            _wasBuildModeActive = false;
            _isZoomTransitionActive = false;
            _normalIsoModeActive = false;
            _hasSmoothCameraFocusTarget = false;
            _smoothCameraFocusVelocity = Vector3.zero;
            _zoomTransitionVelocity = 0f;
            _pitchTransitionVelocity = 0f;
            _yawTransitionVelocity = 0f;
            _fieldOfViewTransitionVelocity = 0f;
            InitialUnitsRuntimeState.FullscreenMapOpen = false;
            InitialUnitsRuntimeState.FullscreenMapIsoMode = false;
            InitialUnitsRuntimeState.InitialCameraFocusRequested = false;
            return;
        }

        if (InitialUnitsRuntimeState.FullscreenMapIsoMode)
        {
            if (worldCamera == null)
                return;

            UpdateFullscreenIsoZoom();
            UpdateFullscreenIsoCameraMode(_fullscreenIsoTargetHeight, _fullscreenIsoTargetOrthographicSize, fullscreenIsoPitch, fullscreenIsoYaw);
            HandleFullscreenIsoCameraPan();
            return;
        }

        if (InitialUnitsRuntimeState.FullscreenMapOpen)
            return;

        if (InitialUnitsRuntimeState.BuildModeActive)
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
            InitialUnitsRuntimeState.SuppressNextWorldClick = false;
            if (InitialUnitsRuntimeState.SelectionModeActive && (_dragging || _hasLiveSelectionRect))
                InitialUnitsRuntimeState.SelectionModeActive = false;
            _dragging = false;
            _cameraDragging = false;
            _selectionModeHoldArmed = false;
            _lastPointerPosition = pointerPosition;
            return;
        }

        if (pointer.WasPressedThisFrame)
        {
            _mainMenuPlayUi ??= MainMenuPlayUI.Instance;
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
            bool pointerOverBlockingUi = InitialUnitsRuntimeState.PlayRequested ? pointerOverGameplayUi : (pointerOverAnyUi || pointerOverGameplayUi);
            _pointerPressedOverUi = !InitialUnitsRuntimeState.PlayRequested && pointerOverBlockingUi;
            _dragStart = pointerPosition;
            _dragCurrent = _dragStart;
            _lastPointerPosition = pointerPosition;
            _dragging = false;
            _cameraDragging = false;
            _selectionModeHoldArmed = false;

            if (_explicitAttackTargetModeActive && !_pointerPressedOverUi)
            {
                if (TryIssueAttackOrderToClickedUnit(pointerPosition))
                    _explicitAttackTargetModeActive = false;

                _skipNextWorldReleaseAfterSelection = true;
                InitialUnitsRuntimeState.SuppressNextWorldClick = true;
                _lastPointerPosition = pointerPosition;
                return;
            }

            if (!InitialUnitsRuntimeState.SelectionModeActive)
            {
                if (!_pointerPressedOverUi)
                {
                    if (TryIssueAttackOrderToClickedUnit(pointerPosition))
                    {
                        InitialUnitsRuntimeState.SuppressNextWorldClick = true;
                    }
                    else if (TryIssueBoardTransportOrderToClickedUnit(pointerPosition))
                    {
                        InitialUnitsRuntimeState.SuppressNextWorldClick = true;
                    }
                    else if (TryFocusUnit(pointerPosition))
                    {
                        _skipNextWorldReleaseAfterSelection = true;
                        InitialUnitsRuntimeState.SuppressNextWorldClick = true;
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

            if (InitialUnitsRuntimeState.SelectionModeActive)
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
            bool releasePointerOverBlockingUi = InitialUnitsRuntimeState.PlayRequested ? releasePointerOverGameplayUi : (releasePointerOverAnyUi || releasePointerOverGameplayUi);

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
                InitialUnitsRuntimeState.SuppressNextWorldClick = false;
                _dragging = false;
                _cameraDragging = false;
                _selectionModeHoldArmed = false;
                _hasLiveSelectionRect = false;
                return;
            }

            if (InitialUnitsRuntimeState.SelectionModeActive)
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

                InitialUnitsRuntimeState.SelectionModeActive = false;
                InitialUnitsRuntimeState.SuppressNextWorldClick = false;
            }
            else if (Vector2.Distance(_dragStart, _dragCurrent) < dragThresholdPixels)
            {
                if (InitialUnitsRuntimeState.SuppressNextWorldClick)
                {
                    InitialUnitsRuntimeState.SuppressNextWorldClick = false;
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
        _queuedMoveOrderToken++;
        _hasQueuedMoveOrder = true;
        _queuedMoveOrderScreenPosition = screenPosition;
        _queuedMoveOrderFrame = Time.frameCount + 1;
    }

    private void ArmSelectionModeHold()
    {
        _selectionModeHoldArmed = true;
        _selectionModeHoldStartTime = Time.unscaledTime;
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

        if (InitialUnitsRuntimeState.SelectionModeActive)
        {
            _selectionModeHoldArmed = false;
            return;
        }

        _mainMenuPlayUi ??= MainMenuPlayUI.Instance;
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
        if (!_hasQueuedMoveOrder || Time.frameCount < _queuedMoveOrderFrame)
            return;

        _hasQueuedMoveOrder = false;
        uint token = _queuedMoveOrderToken;
        Vector2 screenPosition = _queuedMoveOrderScreenPosition;

        if (token != _queuedMoveOrderToken)
            return;

        if (!InitialUnitsRuntimeState.PlayRequested || InitialUnitsRuntimeState.BuildModeActive)
            return;

        if (InitialUnitsRuntimeState.SuppressNextWorldClick)
            return;

        IssueMoveOrder(screenPosition);
    }

    public void OnGui()
    {
        if (!_dragging || !InitialUnitsRuntimeState.PlayRequested || !InitialUnitsRuntimeState.SelectionModeActive)
            return;

        var rect = GetGuiRect(_dragStart, _dragCurrent);
        DrawRect(rect, selectionFill);
        DrawBorder(rect, 2f, selectionBorder);
    }

    private void HandleBuildModeCameraPan()
    {
        if (worldCamera == null)
            return;

        _roadBuildController ??= RoadBuildSystem.Instance;
        _buildingPlacementController ??= BuildingPlacementSystem.Instance;
        _mainMenuPlayUi ??= MainMenuPlayUI.Instance;

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

            bool isVehicle = IsVehicleForVisibleSelection(em, entity);
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

            bool isVehicle = IsVehicleForVisibleSelection(em, entity);
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
            _buildingPlacementController ??= BuildingPlacementSystem.Instance;
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
        var selectedCurrentCells = BuildSelectedCurrentFootprintCells(em, grid, entities);
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
            int2 issuedGoal = FindManualMoveGoal(
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

            if (em.HasComponent<EngageTarget>(entity))
            {
                em.RemoveComponent<EngageTarget>(entity);
                structuralRemoves++;
            }
            if (em.HasComponent<UnitPathFollow>(entity))
            {
                em.RemoveComponent<UnitPathFollow>(entity);
                structuralRemoves++;
            }
            if (em.HasComponent<UnitPathRange>(entity))
            {
                em.RemoveComponent<UnitPathRange>(entity);
                structuralRemoves++;
            }
            if (em.HasComponent<UnitLongDistanceMove>(entity))
            {
                em.RemoveComponent<UnitLongDistanceMove>(entity);
                structuralRemoves++;
            }
            if (em.HasComponent<AutoWanderMoveTag>(entity))
            {
                em.RemoveComponent<AutoWanderMoveTag>(entity);
                structuralRemoves++;
            }
            if (!em.HasComponent<ManualMoveGroupMemberTag>(entity))
            {
                em.AddComponent<ManualMoveGroupMemberTag>(entity);
                structuralAdds++;
            }

            if (em.HasComponent<UnitTarget>(entity))
                em.SetComponentData(entity, new UnitTarget { Cell = issuedGoal });
            else
            {
                em.AddComponentData(entity, new UnitTarget { Cell = issuedGoal });
                structuralAdds++;
            }

            if (!em.HasComponent<UnitAirMovement>(entity))
            {
                bool issuePathNow =
                    !staggerGroundPathRequests ||
                    immediateGroundPathRequests < GroupMoveImmediatePathRequests;
                if (issuePathNow)
                {
                    if (em.HasComponent<UnitPathRetryCooldown>(entity))
                    {
                        em.RemoveComponent<UnitPathRetryCooldown>(entity);
                        structuralRemoves++;
                    }

                    if (em.HasComponent<UnitPathRequest>(entity))
                        em.SetComponentData(entity, new UnitPathRequest { Goal = issuedGoal });
                    else
                    {
                        em.AddComponentData(entity, new UnitPathRequest { Goal = issuedGoal });
                        structuralAdds++;
                    }
                    pathRequestCount++;
                    immediateGroundPathRequests++;
                }
                else
                {
                    if (em.HasComponent<UnitPathRequest>(entity))
                    {
                        em.RemoveComponent<UnitPathRequest>(entity);
                        structuralRemoves++;
                    }

                    int resumeFrame = currentFrame + 1 + (staggeredPathRequestCount / GroupMovePathRequestsPerFrame);
                    maxStaggerDelayFrames = math.max(maxStaggerDelayFrames, resumeFrame - currentFrame);
                    var cooldown = new UnitPathRetryCooldown { ResumeFrame = resumeFrame };
                    if (em.HasComponent<UnitPathRetryCooldown>(entity))
                        em.SetComponentData(entity, cooldown);
                    else
                    {
                        em.AddComponentData(entity, cooldown);
                        structuralAdds++;
                    }
                    staggeredPathRequestCount++;
                }
            }
            else if (em.HasComponent<UnitPathRequest>(entity))
            {
                em.RemoveComponent<UnitPathRequest>(entity);
                structuralRemoves++;
                if (em.HasComponent<UnitPathRetryCooldown>(entity))
                {
                    em.RemoveComponent<UnitPathRetryCooldown>(entity);
                    structuralRemoves++;
                }
                airUnitCount++;
            }
            else
            {
                if (em.HasComponent<UnitPathRetryCooldown>(entity))
                {
                    em.RemoveComponent<UnitPathRetryCooldown>(entity);
                    structuralRemoves++;
                }
                airUnitCount++;
            }

            if (!em.HasComponent<ManualMoveOrderTag>(entity))
            {
                em.AddComponent<ManualMoveOrderTag>(entity);
                structuralAdds++;
            }

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

    private static int2 FindManualMoveGoal(
        EntityManager em,
        in GridConfig grid,
        in Unity.Collections.NativeArray<GridWalkable> walkable,
        in Unity.Collections.NativeBitArray blocked,
        in Unity.Collections.NativeArray<byte> friendlyPassFactionIds,
        in Unity.Collections.NativeBitArray occupied,
        HashSet<int> reservedGoalCells,
        HashSet<int> selectedCurrentCells,
        Entity entity,
        int2 desiredGoal,
        int slotIndex)
    {
        int2 footprintSize = em.HasComponent<UnitFootprint>(entity)
            ? em.GetComponentData<UnitFootprint>(entity).Size
            : new int2(1, 1);
        UnitMovementBehavior movementBehavior = em.HasComponent<UnitMovementBehavior>(entity)
            ? em.GetComponentData<UnitMovementBehavior>(entity)
            : default;
        bool isVehicle = UnitVehicleMovementUtility.IsVehicle(new UnitFootprint { Size = footprintSize }, movementBehavior);
        byte factionId = em.HasComponent<Faction>(entity) ? em.GetComponentData<Faction>(entity).Id : (byte)0;
        int goalPadding = isVehicle ? ManualMoveGoalPaddingVehicle : ManualMoveGoalPaddingInfantry;
        int2 slotAnchor = desiredGoal + GetManualMoveFormationOffset(slotIndex, footprintSize, goalPadding);

        if (CanReserveManualMoveGoal(
                grid,
                walkable,
                blocked,
                friendlyPassFactionIds,
                occupied,
                reservedGoalCells,
                selectedCurrentCells,
                slotAnchor,
                footprintSize,
                goalPadding,
                factionId))
        {
            ReserveManualMoveGoalFootprint(grid, reservedGoalCells, slotAnchor, footprintSize, goalPadding);
            return slotAnchor;
        }

        int maxRadius = isVehicle ? ManualMoveGoalSearchRadiusVehicle : ManualMoveGoalSearchRadiusInfantry;
        for (int radius = 1; radius <= maxRadius; radius++)
        {
            int ringLen = math.max(1, 8 * radius);
            for (int step = 0; step < ringLen; step++)
            {
                int2 candidate = SquareRingOffset(radius, step) + slotAnchor;
                if (!CanReserveManualMoveGoal(
                        grid,
                        walkable,
                        blocked,
                        friendlyPassFactionIds,
                        occupied,
                        reservedGoalCells,
                        selectedCurrentCells,
                        candidate,
                        footprintSize,
                        goalPadding,
                        factionId))
                    continue;

                ReserveManualMoveGoalFootprint(grid, reservedGoalCells, candidate, footprintSize, goalPadding);
                return candidate;
            }
        }

        return slotAnchor;
    }

    private static int2 GetManualMoveFormationOffset(int slotIndex, int2 footprintSize, int padding)
    {
        if (slotIndex <= 0)
            return int2.zero;

        int2 size = UnitFootprintUtility.ClampSize(footprintSize);
        int stride = math.max(1, math.max(size.x, size.y) + (padding * 2));
        int ringIndex = slotIndex - 1;
        int radius = 1;
        int accumulated = 0;
        while (true)
        {
            int ringLen = math.max(1, 8 * radius);
            if (ringIndex < accumulated + ringLen)
            {
                int step = ringIndex - accumulated;
                return SquareRingOffset(radius, step) * stride;
            }

            accumulated += ringLen;
            radius++;
        }
    }

    private static bool CanReserveManualMoveGoal(
        in GridConfig grid,
        in Unity.Collections.NativeArray<GridWalkable> walkable,
        in Unity.Collections.NativeBitArray blocked,
        in Unity.Collections.NativeArray<byte> friendlyPassFactionIds,
        in Unity.Collections.NativeBitArray occupied,
        HashSet<int> reservedGoalCells,
        HashSet<int> selectedCurrentCells,
        int2 cell,
        int2 footprintSize,
        int padding,
        byte factionId)
    {
        int2 size = UnitFootprintUtility.ClampSize(footprintSize);
        int2 min = UnitFootprintUtility.GetMinCell(cell, size);
        int2 max = min + size;
        int2 paddedMin = min - new int2(padding, padding);
        int2 paddedMax = max + new int2(padding, padding);

        if (paddedMin.x < 0 || paddedMin.y < 0 || paddedMax.x > grid.Width || paddedMax.y > grid.Height)
            return false;

        for (int y = paddedMin.y; y < paddedMax.y; y++)
        {
            int row = y * grid.Width;
            for (int x = paddedMin.x; x < paddedMax.x; x++)
            {
                int idx = row + x;
                bool insideActualFootprint = x >= min.x && x < max.x && y >= min.y && y < max.y;
                if (insideActualFootprint)
                {
                    if (walkable[idx].Value == 0)
                        return false;
                    if (blocked.IsCreated && blocked.IsSet(idx) &&
                        (!friendlyPassFactionIds.IsCreated || (uint)idx >= (uint)friendlyPassFactionIds.Length || friendlyPassFactionIds[idx] != factionId))
                        return false;
                }
                if (occupied.IsCreated && occupied.IsSet(idx) && !selectedCurrentCells.Contains(idx))
                    return false;
                if (reservedGoalCells.Contains(idx))
                    return false;
            }
        }

        return true;
    }

    private static HashSet<int> BuildSelectedCurrentFootprintCells(EntityManager em, in GridConfig grid, Unity.Collections.NativeArray<Entity> entities)
    {
        var cells = new HashSet<int>();
        if (entities.Length == 0)
            return cells;

        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            if (!em.HasComponent<UnitGrid>(entity))
                continue;

            int2 unitCell = em.GetComponentData<UnitGrid>(entity).Cell;
            int2 unitSize = em.HasComponent<UnitFootprint>(entity)
                ? em.GetComponentData<UnitFootprint>(entity).Size
                : new int2(1, 1);
            int2 min = UnitFootprintUtility.GetMinCell(unitCell, UnitFootprintUtility.ClampSize(unitSize));
            int2 max = min + UnitFootprintUtility.ClampSize(unitSize);

            for (int y = min.y; y < max.y; y++)
            {
                if (y < 0 || y >= grid.Height)
                    continue;

                int row = y * grid.Width;
                for (int x = min.x; x < max.x; x++)
                {
                    if (x < 0 || x >= grid.Width)
                        continue;

                    cells.Add(row + x);
                }
            }
        }

        return cells;
    }

    private static void ReserveManualMoveGoalFootprint(
        in GridConfig grid,
        HashSet<int> reservedGoalCells,
        int2 cell,
        int2 footprintSize,
        int padding)
    {
        int2 size = UnitFootprintUtility.ClampSize(footprintSize);
        int2 min = UnitFootprintUtility.GetMinCell(cell, size);
        int2 max = min + size;
        int2 paddedMin = min - new int2(padding, padding);
        int2 paddedMax = max + new int2(padding, padding);
        for (int y = paddedMin.y; y < paddedMax.y; y++)
        {
            int row = y * grid.Width;
            for (int x = paddedMin.x; x < paddedMax.x; x++)
                reservedGoalCells.Add(row + x);
        }
    }

    private static int2 SquareRingOffset(int radius, int step)
    {
        int topLen = (2 * radius) + 1;
        if (step < topLen)
            return new int2(-radius + step, radius);

        step -= topLen;
        int rightLen = 2 * radius;
        if (step < rightLen)
            return new int2(radius, (radius - 1) - step);

        step -= rightLen;
        int bottomLen = 2 * radius;
        if (step < bottomLen)
            return new int2((radius - 1) - step, -radius);

        step -= bottomLen;
        return new int2(-radius, (-radius + 1) + step);
    }

    private bool TryIssueBoardTransportOrderToClickedUnit(Vector2 screenPosition)
    {
        if (World.DefaultGameObjectInjectionWorld == null)
            return false;

        var em = World.DefaultGameObjectInjectionWorld.EntityManager;
        EnsureEntityQueries(em);
        if (!TryGetClickedOrNearbyBoardableTransport(screenPosition, em, out Entity transport))
            return false;

        if (!IsBoardablePlayerTransport(em, transport))
        {
            LogTransportBoarding($"result=TransportNotBoardable transport={DescribeTransportBoardingEntity(em, transport)} {DescribeTransportAirState(em, transport)}");
            return false;
        }

        bool airTransport = em.HasComponent<UnitAirMovement>(transport);
        bool transportLanded = IsTransportLandedForBoarding(em, transport);
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
            if (!TryFindAirTransportPickupForBoarding(
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

            if (!IsSoldierBoardingCandidate(em, passenger))
            {
                LogTransportBoarding($"result=SkipPassenger reason=NotSoldierBoardingCandidate passenger={DescribeTransportBoardingEntity(em, passenger)} transport={DescribeTransportBoardingEntity(em, transport)}");
                continue;
            }

            int2 referenceCell = em.GetComponentData<UnitGrid>(passenger).Cell;
            byte passengerFaction = em.GetComponentData<Faction>(passenger).Id;
            int2 passengerFootprint = em.GetComponentData<UnitFootprint>(passenger).Size;
            if (!TryFindTransportApproachCell(
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
                    em.HasComponent<UnitAirMovement>(transport) ? AirTransportDirectBoardingCells : TransportBoardingClearanceCells,
                    passengerFaction,
                    out int2 goal))
            {
                LogTransportBoarding(
                    $"result=NoApproach passenger={DescribeTransportBoardingEntity(em, passenger)} transport={DescribeTransportBoardingEntity(em, transport)} " +
                    $"passengerCell={referenceCell} transportCell={transportCell} transportSize={boardingTransportSize} directCells={(em.HasComponent<UnitAirMovement>(transport) ? AirTransportDirectBoardingCells : TransportBoardingClearanceCells)}");
                continue;
            }

            boardingOrders.Add(new PendingTransportBoardingOrder
            {
                Passenger = passenger,
                PassengerCell = referenceCell,
                Goal = goal,
                DirectBoarding = goal.Equals(referenceCell)
            });
            ReserveFootprintCells(grid, goal, passengerFootprint, reservedBoardingCells);
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
            CommandAirTransportPickup(em, transport, grid, pendingAirPickupCell);
            LogTransportBoarding($"result=AirPickupLanding transport={DescribeTransportBoardingEntity(em, transport)} landing={pendingAirPickupCell}");
        }

        for (int i = 0; i < boardingOrders.Count; i++)
        {
            Entity passenger = boardingOrders[i].Passenger;
            int2 goal = boardingOrders[i].Goal;
            if (!em.Exists(passenger) || !IsSoldierBoardingCandidate(em, passenger))
                continue;

            ClearMovementOrderComponents(em, passenger);
            if (!em.HasBuffer<UnitTransportHiddenVisualScale>(passenger))
                em.AddBuffer<UnitTransportHiddenVisualScale>(passenger);
            em.AddComponentData(passenger, new UnitTarget { Cell = goal });
            em.AddComponentData(passenger, new UnitPathRequest { Goal = goal });
            if (!em.HasComponent<ManualMoveOrderTag>(passenger))
                em.AddComponent<ManualMoveOrderTag>(passenger);
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
        if (hasClickedEntity && IsBoardablePlayerTransport(em, clickedEntity))
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
                $"player={(IsPlayerFaction(em, clickedEntity) ? 1 : 0)} landed={(IsTransportLandedForBoarding(em, clickedEntity) ? 1 : 0)} {DescribeTransportAirState(em, clickedEntity)}");
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
            if (!IsBoardablePlayerTransport(em, candidate))
                continue;

            int2 cell = em.GetComponentData<UnitGrid>(candidate).Cell;
            int2 footprint = em.GetComponentData<UnitFootprint>(candidate).Size;
            int clickPaddingCells = GetTransportBoardingClickPaddingCells(em, candidate, footprint);
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

    private static int GetTransportBoardingClickPaddingCells(EntityManager em, Entity transport, int2 footprint)
    {
        int footprintMax = math.max(footprint.x, footprint.y);
        if (em.Exists(transport) && em.HasComponent<UnitAirMovement>(transport))
            return math.max(24, footprintMax + 24);

        return math.max(6, footprintMax + 4);
    }

    private static bool IsBoardablePlayerTransport(EntityManager em, Entity transport)
    {
        return em.Exists(transport) &&
               TryEnsureTransportCapacity(em, transport) &&
               em.HasComponent<Faction>(transport) &&
               em.GetComponentData<Faction>(transport).Id == 0 &&
               em.HasComponent<UnitGrid>(transport) &&
               em.HasComponent<UnitFootprint>(transport) &&
               em.HasComponent<LocalTransform>(transport);
    }

    private static bool TryEnsureTransportCapacity(EntityManager em, Entity transport)
    {
        if (!em.Exists(transport))
            return false;

        int capacity = 0;
        if (em.HasComponent<UnitTransportCapacity>(transport))
            capacity = math.max(0, em.GetComponentData<UnitTransportCapacity>(transport).SoldierCapacity);

        if (capacity <= 0)
            capacity = ResolveTransportCapacity(em, transport);
        if (capacity <= 0)
            return false;

        if (em.HasComponent<UnitTransportCapacity>(transport))
            em.SetComponentData(transport, new UnitTransportCapacity { SoldierCapacity = capacity });
        else
            em.AddComponentData(transport, new UnitTransportCapacity { SoldierCapacity = capacity });

        if (!em.HasBuffer<UnitTransportPassengerElement>(transport))
            em.AddBuffer<UnitTransportPassengerElement>(transport);

        return true;
    }

    private static int ResolveTransportCapacity(EntityManager em, Entity entity)
    {
        string sourceName = string.Empty;
        if (em.HasComponent<UnitSourcePrefabKey>(entity))
            sourceName = em.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString();
        if (string.IsNullOrWhiteSpace(sourceName))
            sourceName = em.GetName(entity);

        return IsPersonnelTransportName(sourceName) ? 10 : 0;
    }

    private static bool IsPersonnelTransportName(string sourceName)
    {
        if (string.IsNullOrWhiteSpace(sourceName))
            return false;

        return sourceName.IndexOf("Unit_Veh_APC_Fast", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
               sourceName.IndexOf("Unit_Veh_APC_Heavy", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
               sourceName.IndexOf("Unit_Veh_APC_Slow", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
               sourceName.IndexOf("Unit_Veh_APC_01", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
               sourceName.IndexOf("Unit_Veh_APC_02", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
               sourceName.IndexOf("Unit_Veh_Truck_Canopy", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
               sourceName.IndexOf("Unit_Veh_Helicopter_Transport", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool IsRopeDisembarkTransport(EntityManager em, Entity transport)
    {
        if (!em.Exists(transport) || !em.HasComponent<UnitAirMovement>(transport))
            return false;

        string sourceName = ResolveUnitSourceName(em, transport);
        return sourceName.IndexOf("Unit_Veh_Helicopter_Transport", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool IsTransportLandedForBoarding(EntityManager em, Entity transport)
    {
        if (!em.Exists(transport) || !em.HasComponent<UnitAirMovement>(transport))
            return true;

        if (!em.HasComponent<UnitAirState>(transport) || !em.HasComponent<LocalTransform>(transport))
            return false;

        UnitAirState airState = em.GetComponentData<UnitAirState>(transport);
        LocalTransform transform = em.GetComponentData<LocalTransform>(transport);
        float groundY = airState.HomeInitialized != 0 ? airState.HomePosition.y : transform.Position.y;
        bool grounded =
            airState.Airborne == 0 &&
            airState.TakeoffRolling == 0 &&
            airState.LandingRolling == 0 &&
            transform.Position.y <= groundY + AirTransportBoardingGroundedHeightTolerance;
        return grounded && !em.HasComponent<UnitTransportRopeDisembarkRequest>(transport);
    }

    private static bool TryPrepareAirTransportPickupForBoarding(
        EntityManager em,
        Entity transport,
        in GridConfig grid,
        in Unity.Collections.NativeArray<GridWalkable> walkable,
        in Unity.Collections.NativeBitArray blocked,
        in Unity.Collections.NativeArray<byte> friendlyPassFactionIds,
        in Unity.Collections.NativeBitArray occupied,
        int2 transportCell,
        int2 transportSize,
        List<Entity> selectedPassengers,
        int selectedCount,
        in Unity.Collections.NativeArray<Entity> liveUnitEntities,
        in Unity.Collections.NativeArray<UnitGrid> liveUnitGrids,
        in Unity.Collections.NativeArray<UnitFootprint> liveUnitFootprints,
        out int2 pickupCell)
    {
        if (!TryFindAirTransportPickupForBoarding(
                em,
                transport,
                grid,
                walkable,
                blocked,
                friendlyPassFactionIds,
                occupied,
                transportCell,
                transportSize,
                selectedPassengers,
                selectedCount,
                liveUnitEntities,
                liveUnitGrids,
                liveUnitFootprints,
                out pickupCell))
        {
            return false;
        }

        CommandAirTransportPickup(em, transport, grid, pickupCell);
        LogTransportBoarding($"result=AirPickupLanding transport={DescribeTransportBoardingEntity(em, transport)} landing={pickupCell}");
        return true;
    }

    private static bool TryFindAirTransportPickupForBoarding(
        EntityManager em,
        Entity transport,
        in GridConfig grid,
        in Unity.Collections.NativeArray<GridWalkable> walkable,
        in Unity.Collections.NativeBitArray blocked,
        in Unity.Collections.NativeArray<byte> friendlyPassFactionIds,
        in Unity.Collections.NativeBitArray occupied,
        int2 transportCell,
        int2 transportSize,
        List<Entity> selectedPassengers,
        int selectedCount,
        in Unity.Collections.NativeArray<Entity> liveUnitEntities,
        in Unity.Collections.NativeArray<UnitGrid> liveUnitGrids,
        in Unity.Collections.NativeArray<UnitFootprint> liveUnitFootprints,
        out int2 pickupCell)
    {
        pickupCell = default;
        if (!em.Exists(transport) ||
            !em.HasComponent<UnitAirMovement>(transport) ||
            !em.HasComponent<UnitAirState>(transport) ||
            !em.HasComponent<LocalTransform>(transport))
        {
            return false;
        }

        byte factionId = em.HasComponent<Faction>(transport) ? em.GetComponentData<Faction>(transport).Id : (byte)0;
        int count = math.min(selectedCount, selectedPassengers.Count);
        for (int i = 0; i < count; i++)
        {
            Entity passenger = selectedPassengers[i];
            if (!IsSoldierBoardingCandidate(em, passenger) || !em.HasComponent<UnitGrid>(passenger))
                continue;

            int2 passengerCell = em.GetComponentData<UnitGrid>(passenger).Cell;
            if (!TryFindAirTransportPickupCellNearPassenger(
                    grid,
                    walkable,
                    blocked,
                    friendlyPassFactionIds,
                    occupied,
                    transportCell,
                    transportSize,
                    passengerCell,
                    transport,
                    liveUnitEntities,
                    liveUnitGrids,
                    liveUnitFootprints,
                    factionId,
                    out pickupCell))
            {
                continue;
            }

            return true;
        }

        return false;
    }

    private static bool TryFindAirTransportPickupCellNearPassenger(
        in GridConfig grid,
        in Unity.Collections.NativeArray<GridWalkable> walkable,
        in Unity.Collections.NativeBitArray blocked,
        in Unity.Collections.NativeArray<byte> friendlyPassFactionIds,
        in Unity.Collections.NativeBitArray occupied,
        int2 transportCell,
        int2 transportSize,
        int2 passengerCell,
        Entity transport,
        in Unity.Collections.NativeArray<Entity> liveUnitEntities,
        in Unity.Collections.NativeArray<UnitGrid> liveUnitGrids,
        in Unity.Collections.NativeArray<UnitFootprint> liveUnitFootprints,
        byte factionId,
        out int2 pickupCell)
    {
        pickupCell = default;
        for (int radius = 2; radius <= 10; radius++)
        {
            int bestScore = int.MaxValue;
            bool found = false;
            int minX = passengerCell.x - radius;
            int minY = passengerCell.y - radius;
            int maxX = passengerCell.x + radius;
            int maxY = passengerCell.y + radius;
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    if (x != minX && x != maxX && y != minY && y != maxY)
                        continue;

                    int2 candidate = new int2(x, y);
                    if (!IsTransportApproachPassable(
                            grid,
                            walkable,
                            blocked,
                            friendlyPassFactionIds,
                            occupied,
                            candidate,
                            transportSize,
                            transportCell,
                            transport,
                            liveUnitEntities,
                            liveUnitGrids,
                            liveUnitFootprints,
                            Entity.Null,
                            default,
                            default,
                            null,
                            candidate,
                            factionId,
                            false))
                    {
                        continue;
                    }

                    int2 delta = candidate - passengerCell;
                    int score = math.abs(delta.x) + math.abs(delta.y);
                    if (score >= bestScore)
                        continue;

                    bestScore = score;
                    pickupCell = candidate;
                    found = true;
                }
            }

            if (found)
                return true;
        }

        return false;
    }

    private static void CommandAirTransportPickup(EntityManager em, Entity transport, in GridConfig grid, int2 pickupCell)
    {
        ClearMovementOrderComponents(em, transport);

        UnitAirState airState = em.GetComponentData<UnitAirState>(transport);
        LocalTransform transform = em.GetComponentData<LocalTransform>(transport);
        float groundY = airState.HomeInitialized != 0 ? airState.HomePosition.y : grid.Origin.y;
        float3 pickupPosition = GridUtils.CellToWorldCenter(grid, pickupCell);
        pickupPosition.y = groundY;

        airState.HomePosition = pickupPosition;
        airState.HomeCell = pickupCell;
        airState.HomeInitialized = 1;
        airState.ReturningHome = 0;
        airState.TakeoffRolling = 0;
        airState.LandingRolling = 0;
        airState.AttackRunActive = 0;
        airState.ReturnApproachInitialized = 0;
        if (transform.Position.y > groundY + AirTransportBoardingGroundedHeightTolerance)
            airState.Airborne = 1;
        em.SetComponentData(transport, airState);

        em.AddComponentData(transport, new UnitTarget { Cell = pickupCell });
        if (!em.HasComponent<ManualMoveOrderTag>(transport))
            em.AddComponent<ManualMoveOrderTag>(transport);
    }

    private static void StartRopeDisembarkTransport(EntityManager em, Entity transport, int2 referenceCell)
    {
        if (!em.Exists(transport) || !em.HasBuffer<UnitTransportPassengerElement>(transport))
            return;

        DynamicBuffer<UnitTransportPassengerElement> passengers = em.GetBuffer<UnitTransportPassengerElement>(transport);
        if (passengers.Length <= 0)
            return;

        ClearMovementOrderComponents(em, transport);
        if (em.HasComponent<UnitAirMovement>(transport) &&
            em.HasComponent<UnitAirState>(transport) &&
            em.HasComponent<LocalTransform>(transport))
        {
            UnitAirMovement airMovement = em.GetComponentData<UnitAirMovement>(transport);
            UnitAirState airState = em.GetComponentData<UnitAirState>(transport);
            LocalTransform transform = em.GetComponentData<LocalTransform>(transport);
            float groundY = airState.HomeInitialized != 0 ? airState.HomePosition.y : transform.Position.y;
            if (airState.Airborne == 0)
            {
                transform.Position.y = groundY + math.max(3f, airMovement.CruiseHeight);
                em.SetComponentData(transport, transform);
            }

            airState.ReturningHome = 0;
            airState.Airborne = 1;
            airState.TakeoffRolling = 0;
            airState.LandingRolling = 0;
            airState.AttackRunActive = 0;
            airState.ReturnApproachInitialized = 0;
            em.SetComponentData(transport, airState);
        }

        UnitTransportRopeDisembarkRequest request = new()
        {
            ReferenceCell = referenceCell,
            NextDropAt = 0f,
            DropIntervalSeconds = 0.8f
        };

        if (em.HasComponent<UnitTransportRopeDisembarkRequest>(transport))
            em.SetComponentData(transport, request);
        else
            em.AddComponentData(transport, request);
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

    private static bool IsKnownPersonnelTransport(EntityManager em, Entity entity)
    {
        if (!em.Exists(entity))
            return false;

        if (em.HasComponent<UnitTransportCapacity>(entity) &&
            math.max(0, em.GetComponentData<UnitTransportCapacity>(entity).SoldierCapacity) > 0)
        {
            return true;
        }

        return ResolveTransportCapacity(em, entity) > 0;
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
        _cachedSelectedMoveEntities.Clear();
        for (int i = 0; i < entities.Count; i++)
            CacheSelectedMoveEntity(em, entities[i]);
    }

    private void CacheSelectedMoveEntity(EntityManager em, Entity entity)
    {
        if (!IsCacheableSelectedMoveEntity(em, entity))
            return;
        if (_cachedSelectedMoveEntities.Contains(entity))
            return;

        _cachedSelectedMoveEntities.Add(entity);
    }

    private static bool IsCacheableSelectedMoveEntity(EntityManager em, Entity entity)
    {
        return em.Exists(entity) &&
               em.HasComponent<Faction>(entity) &&
               em.GetComponentData<Faction>(entity).Id == 0 &&
               em.HasComponent<UnitGrid>(entity) &&
               em.HasComponent<UnitMove>(entity) &&
               !em.HasComponent<Disabled>(entity) &&
               !em.HasComponent<UnitTransportPassenger>(entity);
    }

    private static bool IsSoldierBoardingCandidate(EntityManager em, Entity entity)
    {
        if (!em.Exists(entity) ||
            !em.HasComponent<Faction>(entity) ||
            em.GetComponentData<Faction>(entity).Id != 0 ||
            !em.HasComponent<UnitGrid>(entity) ||
            !em.HasComponent<UnitMove>(entity) ||
            !em.HasComponent<UnitFootprint>(entity) ||
            !em.HasComponent<UnitMovementBehavior>(entity) ||
            em.HasComponent<UnitAirMovement>(entity) ||
            em.HasComponent<UnitTransportPassenger>(entity))
        {
            return false;
        }

        string sourceName = ResolveUnitSourceName(em, entity);
        if (sourceName.IndexOf("_Chr_", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            sourceName.StartsWith("Unit_Chr", System.StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (sourceName.IndexOf("_Veh_", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            sourceName.StartsWith("Unit_Veh", System.StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return !UnitVehicleMovementUtility.IsVehicle(
            em.GetComponentData<UnitFootprint>(entity),
            em.GetComponentData<UnitMovementBehavior>(entity));
    }

    private static void ClearMovementOrderComponents(EntityManager em, Entity entity)
    {
        RemoveComponentIfPresent<UnitTarget>(em, entity);
        RemoveComponentIfPresent<UnitPathRequest>(em, entity);
        RemoveComponentIfPresent<UnitPathFollow>(em, entity);
        RemoveComponentIfPresent<UnitPathRange>(em, entity);
        RemoveComponentIfPresent<ManualMoveOrderTag>(em, entity);
        RemoveComponentIfPresent<AutoWanderMoveTag>(em, entity);
        RemoveComponentIfPresent<EngageTarget>(em, entity);
    }

    private static void RemoveComponentIfPresent<T>(EntityManager em, Entity entity)
        where T : unmanaged, IComponentData
    {
        if (em.Exists(entity) && em.HasComponent<T>(entity))
            em.RemoveComponent<T>(entity);
    }

    private static bool TryFindTransportApproachCell(
        in GridConfig grid,
        in Unity.Collections.NativeArray<GridWalkable> walkable,
        in Unity.Collections.NativeBitArray blocked,
        in Unity.Collections.NativeArray<byte> friendlyPassFactionIds,
        in Unity.Collections.NativeBitArray occupied,
        int2 transportCell,
        int2 transportSize,
        int2 referenceCell,
        int2 passengerFootprint,
        Entity passenger,
        in Unity.Collections.NativeArray<Entity> liveUnitEntities,
        in Unity.Collections.NativeArray<UnitGrid> liveUnitGrids,
        in Unity.Collections.NativeArray<UnitFootprint> liveUnitFootprints,
        Entity ignoredOccupancyEntity,
        int2 ignoredOccupancyCell,
        int2 ignoredOccupancySize,
        HashSet<int> reservedCells,
        int directBoardingCells,
        byte factionId,
        out int2 goal)
    {
        return TryFindNearbyTransportApproachCell(
            grid,
            walkable,
            blocked,
            friendlyPassFactionIds,
            occupied,
            transportCell,
            transportSize,
            referenceCell,
            passengerFootprint,
            passenger,
            liveUnitEntities,
            liveUnitGrids,
            liveUnitFootprints,
            ignoredOccupancyEntity,
            ignoredOccupancyCell,
            ignoredOccupancySize,
            reservedCells,
            directBoardingCells,
            factionId,
            out goal);
    }

    private static bool TryFindNearbyTransportApproachCell(
        in GridConfig grid,
        in Unity.Collections.NativeArray<GridWalkable> walkable,
        in Unity.Collections.NativeBitArray blocked,
        in Unity.Collections.NativeArray<byte> friendlyPassFactionIds,
        in Unity.Collections.NativeBitArray occupied,
        int2 transportCell,
        int2 transportSize,
        int2 referenceCell,
        int2 passengerFootprint,
        Entity passenger,
        in Unity.Collections.NativeArray<Entity> liveUnitEntities,
        in Unity.Collections.NativeArray<UnitGrid> liveUnitGrids,
        in Unity.Collections.NativeArray<UnitFootprint> liveUnitFootprints,
        Entity ignoredOccupancyEntity,
        int2 ignoredOccupancyCell,
        int2 ignoredOccupancySize,
        HashSet<int> reservedCells,
        int directBoardingCells,
        byte factionId,
        out int2 goal)
    {
        goal = default;
        if (!GridUtils.InBounds(referenceCell, grid.Width, grid.Height))
            return false;

        int gridSize = grid.Width * grid.Height;
        if (gridSize <= 0 || walkable.Length < gridSize)
            return false;

        int2 size = UnitFootprintUtility.ClampSize(transportSize);
        int2 min = UnitFootprintUtility.GetMinCell(transportCell, size);
        int2 max = min + size;
        if (directBoardingCells > TransportBoardingClearanceCells &&
            UnitFootprintUtility.ContainsCellWithPadding(transportCell, size, referenceCell, directBoardingCells))
        {
            goal = referenceCell;
            return true;
        }

        int maxRadius = math.max(1, directBoardingCells);
        int bestScore = int.MaxValue;
        bool found = false;
        for (int radius = 1; radius <= maxRadius; radius++)
        {
            int minX = min.x - radius;
            int minY = min.y - radius;
            int maxX = max.x - 1 + radius;
            int maxY = max.y - 1 + radius;

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    bool onRing = x == minX || x == maxX || y == minY || y == maxY;
                    if (!onRing)
                        continue;

                    int2 candidate = new int2(x, y);
                    if (!GridUtils.InBounds(candidate, grid.Width, grid.Height))
                        continue;

                    if (!IsTransportApproachPassable(
                            grid,
                            walkable,
                            blocked,
                            friendlyPassFactionIds,
                            occupied,
                            candidate,
                            passengerFootprint,
                            referenceCell,
                            passenger,
                            liveUnitEntities,
                            liveUnitGrids,
                            liveUnitFootprints,
                            ignoredOccupancyEntity,
                            ignoredOccupancyCell,
                            ignoredOccupancySize,
                            reservedCells,
                            referenceCell,
                            factionId,
                            candidate.Equals(referenceCell)))
                    {
                        continue;
                    }

                    int2 delta = candidate - referenceCell;
                    int score = math.abs(delta.x) + math.abs(delta.y);
                    if (score >= bestScore)
                        continue;

                    bestScore = score;
                    goal = candidate;
                    found = true;
                }
            }

            if (found)
                return true;
        }

        return false;
    }

    private static bool IsTransportApproachPassable(
        in GridConfig grid,
        in Unity.Collections.NativeArray<GridWalkable> walkable,
        in Unity.Collections.NativeBitArray blocked,
        in Unity.Collections.NativeArray<byte> friendlyPassFactionIds,
        in Unity.Collections.NativeBitArray occupied,
        int2 cell,
        int2 footprintSize,
        int2 currentCell,
        Entity movingEntity,
        in Unity.Collections.NativeArray<Entity> liveUnitEntities,
        in Unity.Collections.NativeArray<UnitGrid> liveUnitGrids,
        in Unity.Collections.NativeArray<UnitFootprint> liveUnitFootprints,
        Entity ignoredOccupancyEntity,
        int2 ignoredOccupancyCell,
        int2 ignoredOccupancySize,
        HashSet<int> reservedCells,
        int2 referenceCell,
        byte factionId,
        bool allowReferenceCellOccupied)
    {
        int2 clamped = UnitFootprintUtility.ClampSize(footprintSize);
        int2 min = UnitFootprintUtility.GetMinCell(cell, clamped);
        int2 max = min + clamped;
        if (min.x < 0 || min.y < 0 || max.x > grid.Width || max.y > grid.Height)
            return false;

        for (int y = min.y; y < max.y; y++)
        {
            int row = y * grid.Width;
            for (int x = min.x; x < max.x; x++)
            {
                int index = row + x;
                if ((uint)index >= (uint)walkable.Length || walkable[index].Value == 0)
                    return false;
                if (reservedCells != null && reservedCells.Contains(index))
                    return false;

                if (blocked.IsCreated && blocked.IsSet(index) &&
                    (!friendlyPassFactionIds.IsCreated || (uint)index >= (uint)friendlyPassFactionIds.Length || friendlyPassFactionIds[index] != factionId))
                {
                    return false;
                }

                bool isReferenceCell = x == referenceCell.x && y == referenceCell.y;
                bool isCurrentFootprintCell = UnitFootprintUtility.ContainsCell(currentCell, clamped, new int2(x, y));
                bool isIgnoredOccupancyCell =
                    ignoredOccupancyEntity != Entity.Null &&
                    UnitFootprintUtility.ContainsCell(ignoredOccupancyCell, ignoredOccupancySize, new int2(x, y));
                if (!isCurrentFootprintCell &&
                    occupied.IsCreated &&
                    occupied.IsSet(index) &&
                    (!allowReferenceCellOccupied || !isReferenceCell) &&
                    !isIgnoredOccupancyCell)
                {
                    return false;
                }
            }
        }

        for (int i = 0; i < liveUnitEntities.Length; i++)
        {
            Entity other = liveUnitEntities[i];
            if (other == movingEntity || other == ignoredOccupancyEntity)
                continue;

            int2 otherCell = liveUnitGrids[i].Cell;
            int2 otherSize = liveUnitFootprints[i].Size;
            if (UnitFootprintUtility.Overlaps(cell, clamped, otherCell, otherSize) &&
                !UnitFootprintUtility.Overlaps(currentCell, clamped, otherCell, otherSize))
            {
                return false;
            }
        }

        return true;
    }

    private static void ReserveFootprintCells(GridConfig grid, int2 cell, int2 footprintSize, HashSet<int> reservedCells)
    {
        if (reservedCells == null)
            return;

        int2 clamped = UnitFootprintUtility.ClampSize(footprintSize);
        int2 min = UnitFootprintUtility.GetMinCell(cell, clamped);
        int2 max = min + clamped;
        for (int y = min.y; y < max.y; y++)
        {
            for (int x = min.x; x < max.x; x++)
            {
                int2 reservedCell = new int2(x, y);
                if (GridUtils.InBounds(reservedCell, grid.Width, grid.Height))
                    reservedCells.Add(GridUtils.CellToIndex(reservedCell, grid.Width));
            }
        }
    }

    private static bool TryFindTransportDisembarkCell(
        in GridConfig grid,
        in Unity.Collections.NativeArray<GridWalkable> walkable,
        in Unity.Collections.NativeBitArray blocked,
        in Unity.Collections.NativeBitArray occupied,
        HashSet<int> reservedCells,
        int2 transportCell,
        int2 transportSize,
        int2 referenceCell,
        out int2 goal)
    {
        return TryFindTransportRingCell(
            grid,
            walkable,
            blocked,
            occupied,
            reservedCells,
            transportCell,
            transportSize,
            referenceCell,
            TransportDisembarkClearanceCells,
            false,
            out goal);
    }

    private static bool TryFindTransportRingCell(
        in GridConfig grid,
        in Unity.Collections.NativeArray<GridWalkable> walkable,
        in Unity.Collections.NativeBitArray blocked,
        in Unity.Collections.NativeBitArray occupied,
        HashSet<int> reservedCells,
        int2 transportCell,
        int2 transportSize,
        int2 referenceCell,
        int minRadius,
        bool allowReferenceCellOccupied,
        out int2 goal)
    {
        goal = default;
        int2 size = UnitFootprintUtility.ClampSize(transportSize);
        int2 min = UnitFootprintUtility.GetMinCell(transportCell, size);
        int2 max = min + size;
        int bestScore = int.MaxValue;
        bool found = false;
        int startRadius = math.max(1, minRadius);
        int maxRadius = math.max(8, math.max(size.x, size.y) + 6);

        for (int radius = startRadius; radius <= maxRadius; radius++)
        {
            int minX = min.x - radius;
            int minY = min.y - radius;
            int maxX = max.x - 1 + radius;
            int maxY = max.y - 1 + radius;

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    bool onRing = x == minX || x == maxX || y == minY || y == maxY;
                    if (!onRing)
                        continue;

                    int2 candidate = new int2(x, y);
                    if (!GridUtils.InBounds(candidate, grid.Width, grid.Height))
                        continue;

                    int index = GridUtils.CellToIndex(candidate, grid.Width);
                    if (reservedCells != null && reservedCells.Contains(index))
                        continue;
                    if (walkable[index].Value == 0)
                        continue;
                    if (blocked.IsCreated && blocked.IsSet(index))
                        continue;

                    bool isReferenceCell = candidate.Equals(referenceCell);
                    if (occupied.IsCreated && occupied.IsSet(index) && (!allowReferenceCellOccupied || !isReferenceCell))
                        continue;

                    int2 delta = candidate - referenceCell;
                    int score = math.abs(delta.x) + math.abs(delta.y);
                    if (score >= bestScore)
                        continue;

                    bestScore = score;
                    goal = candidate;
                    found = true;
                }
            }

            if (found)
                return true;
        }

        return false;
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
        _lastKnownPointerPosition = pointerPosition;
        _hasLastKnownPointerPosition = true;
    }

    private bool TryGetPointerPosition(out Vector2 pointerPosition)
    {
        if (GamePointerInput.TryGetPrimaryPointer(out GamePointerState pointer))
        {
            pointerPosition = pointer.Position;
            UpdateLastKnownPointerPosition(pointerPosition);
            return true;
        }

        pointerPosition = _lastKnownPointerPosition;
        return _hasLastKnownPointerPosition;
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
        _mainMenuPlayUi ??= MainMenuPlayUI.Instance;
        if (_mainMenuPlayUi != null)
            return _mainMenuPlayUi.IsPointerOverAnyGameplayUi(screenPosition, out source);

        return IsPointerOverUI(screenPosition, out source);
    }

    private void PanCamera(Vector2 screenDelta)
    {
        OrderScreenMarkersHideRequested?.Invoke();

        Vector3 flatRight = worldCamera.transform.right;
        flatRight.y = 0f;
        flatRight.Normalize();

        Vector3 flatForward = worldCamera.transform.forward;
        flatForward.y = 0f;
        flatForward.Normalize();

        Vector3 worldDelta =
            (-flatRight * screenDelta.x + -flatForward * screenDelta.y) * panSensitivity;

        worldCamera.transform.position += worldDelta;
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
            {
                _isZoomTransitionActive = false;
                _zoomTransitionVelocity = 0f;
                _pitchTransitionVelocity = 0f;
                _yawTransitionVelocity = 0f;
                _fieldOfViewTransitionVelocity = 0f;
            }

            return;
        }

        float zoomDirection = 0f;
        if (InitialUnitsRuntimeState.ZoomInHeld)
            zoomDirection += 1f;
        if (InitialUnitsRuntimeState.ZoomOutHeld)
            zoomDirection -= 1f;

        if (Mathf.Approximately(zoomDirection, 0f))
            return;

        Vector3 zoomDelta = worldCamera.transform.forward * (zoomDirection * zoomSpeed * Time.deltaTime);
        Vector3 currentPosition = worldCamera.transform.position;
        Vector3 targetPosition = currentPosition + zoomDelta;

        float clampedHeight = Mathf.Clamp(targetPosition.y, minZoomHeight, maxZoomHeight);
        if (!Mathf.Approximately(targetPosition.y, currentPosition.y))
        {
            float t = (clampedHeight - currentPosition.y) / (targetPosition.y - currentPosition.y);
            targetPosition = currentPosition + (zoomDelta * t);
        }
        else
        {
            targetPosition.y = clampedHeight;
        }

        worldCamera.transform.position = targetPosition;
    }

    private void UpdateFullscreenIsoZoom()
    {
        if (worldCamera == null)
            return;

        float zoomDirection = 0f;
        if (InitialUnitsRuntimeState.ZoomInHeld)
            zoomDirection += 1f;
        if (InitialUnitsRuntimeState.ZoomOutHeld)
            zoomDirection -= 1f;

        if (Mathf.Approximately(zoomDirection, 0f))
            return;

        _fullscreenIsoTargetHeight = Mathf.Clamp(
            _fullscreenIsoTargetHeight - (zoomDirection * zoomSpeed * Time.deltaTime),
            minZoomHeight,
            maxZoomHeight);
        _fullscreenIsoTargetOrthographicSize = Mathf.Clamp(
            _fullscreenIsoTargetOrthographicSize - (zoomDirection * (zoomSpeed * 0.6f) * Time.deltaTime),
            8f,
            48f);
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
            _wasPlayRequested = InitialUnitsRuntimeState.PlayRequested;
            _wasBuildModeActive = InitialUnitsRuntimeState.BuildModeActive;
            _isZoomTransitionActive = false;
            return;
        }

        if (!_wasPlayRequested && InitialUnitsRuntimeState.PlayRequested)
        {
            Vector3 focusWorldPosition = worldCamera != null ? GetCameraGroundCenterWorld() : Vector3.zero;
            ApplyPerspectiveCameraModeInstant(normalModeZoomHeight, normalModePitch, normalModeYaw, normalModeFieldOfView);
            if (worldCamera != null)
                MoveCameraGroundCenterTo(focusWorldPosition);
            _wasBuildModeActive = InitialUnitsRuntimeState.BuildModeActive;
            _wasPlayRequested = true;
            _isZoomTransitionActive = InitialUnitsRuntimeState.BuildModeActive;
            _zoomTransitionVelocity = 0f;
            _pitchTransitionVelocity = 0f;
            _yawTransitionVelocity = 0f;
            _fieldOfViewTransitionVelocity = 0f;
            _orthographicSizeTransitionVelocity = 0f;
            return;
        }

        _wasPlayRequested = InitialUnitsRuntimeState.PlayRequested;

        if (_wasBuildModeActive != InitialUnitsRuntimeState.BuildModeActive)
        {
            _wasBuildModeActive = InitialUnitsRuntimeState.BuildModeActive;
            _isZoomTransitionActive = true;
            _zoomTransitionVelocity = 0f;
        }
    }

    private void ConsumeInitialCameraFocusRequest()
    {
        if (!InitialUnitsRuntimeState.InitialCameraFocusRequested || worldCamera == null)
            return;

        MoveCameraGroundCenterTo(InitialUnitsRuntimeState.InitialCameraFocusWorld);
        InitialUnitsRuntimeState.InitialCameraFocusRequested = false;
        _hasSmoothCameraFocusTarget = false;
        _smoothCameraFocusVelocity = Vector3.zero;
    }

    private void UpdateSmoothCameraFocus()
    {
        if (!_hasSmoothCameraFocusTarget || worldCamera == null)
            return;

        Vector3 currentGroundCenter = GetCameraGroundCenterWorld();
        Vector3 smoothedCenter = Vector3.SmoothDamp(
            currentGroundCenter,
            _smoothCameraFocusTarget,
            ref _smoothCameraFocusVelocity,
            Mathf.Max(0.01f, zoomTransitionSmoothTime));
        MoveCameraGroundCenterTo(smoothedCenter);

        Vector2 remaining = new(
            _smoothCameraFocusTarget.x - smoothedCenter.x,
            _smoothCameraFocusTarget.z - smoothedCenter.z);
        if (remaining.sqrMagnitude <= 0.01f)
        {
            MoveCameraGroundCenterTo(_smoothCameraFocusTarget);
            _hasSmoothCameraFocusTarget = false;
            _smoothCameraFocusVelocity = Vector3.zero;
        }
    }

    private void ApplyPerspectiveCameraModeInstant(float height, float pitch, float yaw, float fieldOfView)
    {
        if (worldCamera == null)
            return;

        worldCamera.orthographic = false;
        Vector3 position = worldCamera.transform.position;
        position.y = height;
        worldCamera.transform.position = position;
        worldCamera.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        worldCamera.fieldOfView = fieldOfView;
    }

    private void ApplyFullscreenIsoCameraModeInstant(float height, float orthographicSize, float pitch, float yaw)
    {
        if (worldCamera == null)
            return;

        worldCamera.orthographic = true;
        Vector3 position = worldCamera.transform.position;
        position.y = height;
        worldCamera.transform.position = position;
        worldCamera.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        worldCamera.orthographicSize = orthographicSize;
    }

    public void EnterFullscreenMapIsoMode(Vector3 focusWorldPosition)
    {
        if (worldCamera == null)
            return;

        _fullscreenIsoTargetHeight = Mathf.Clamp(fullscreenIsoZoomHeight, minZoomHeight, maxZoomHeight);
        _fullscreenIsoTargetOrthographicSize = Mathf.Clamp(fullscreenIsoOrthographicSize, 8f, 48f);
        MoveCameraGroundCenterTo(focusWorldPosition);
        ApplyFullscreenIsoCameraModeInstant(_fullscreenIsoTargetHeight, _fullscreenIsoTargetOrthographicSize, fullscreenIsoPitch, fullscreenIsoYaw);
        InitialUnitsRuntimeState.FullscreenMapIsoMode = true;
        InitialUnitsRuntimeState.FullscreenMapOpen = true;
        _cameraDragging = false;
    }

    public void ExitFullscreenMapIsoMode()
    {
        if (worldCamera != null)
            ApplyPerspectiveCameraModeInstant(normalModeZoomHeight, normalModePitch, normalModeYaw, normalModeFieldOfView);

        InitialUnitsRuntimeState.FullscreenMapIsoMode = false;
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
        if (worldCamera == null)
            return;

        Vector3 currentGroundCenter = GetCameraGroundCenterWorld();
        Vector3 position = worldCamera.transform.position;
        position.x += focusWorldPosition.x - currentGroundCenter.x;
        position.z += focusWorldPosition.z - currentGroundCenter.z;
        worldCamera.transform.position = position;
    }

    public void SmoothMoveCameraGroundCenterTo(Vector3 focusWorldPosition)
    {
        if (worldCamera == null)
            return;

        focusWorldPosition.y = 0f;
        _smoothCameraFocusTarget = focusWorldPosition;
        _hasSmoothCameraFocusTarget = true;
        _smoothCameraFocusVelocity = Vector3.zero;
        _cameraDragging = false;
    }

    public void FollowCameraGroundCenterTo(Vector3 focusWorldPosition)
    {
        if (worldCamera == null)
            return;

        focusWorldPosition.y = 0f;
        _smoothCameraFocusTarget = focusWorldPosition;
        _hasSmoothCameraFocusTarget = true;
        _cameraDragging = false;
    }

    private Vector3 GetCameraGroundCenterWorld()
    {
        if (worldCamera == null)
            return Vector3.zero;

        Plane groundPlane = new(Vector3.up, Vector3.zero);
        Ray ray = worldCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        return groundPlane.Raycast(ray, out float distance) ? ray.GetPoint(distance) : worldCamera.transform.position;
    }

    private float GetVisibleGroundVerticalSpan()
    {
        if (worldCamera == null)
            return 0f;

        if (!TryGetGroundPointFromViewport(new Vector2(0.5f, 0f), out Vector3 topPoint) ||
            !TryGetGroundPointFromViewport(new Vector2(0.5f, 1f), out Vector3 bottomPoint))
            return 0f;

        return Vector3.Distance(topPoint, bottomPoint);
    }

    private bool TryGetGroundPointFromViewport(Vector2 viewport, out Vector3 point)
    {
        point = Vector3.zero;
        if (worldCamera == null)
            return false;

        Plane groundPlane = new(Vector3.up, Vector3.zero);
        Ray ray = worldCamera.ViewportPointToRay(new Vector3(viewport.x, viewport.y, 0f));
        if (!groundPlane.Raycast(ray, out float distance))
            return false;

        point = ray.GetPoint(distance);
        return true;
    }

    private float CalculateOrthographicSizeForGroundSpan(float targetGroundSpan, float height, float pitch, float yaw)
    {
        if (worldCamera == null || targetGroundSpan <= 0.01f)
            return fullscreenIsoOrthographicSize;

        bool originalOrthographic = worldCamera.orthographic;
        Vector3 originalPosition = worldCamera.transform.position;
        Quaternion originalRotation = worldCamera.transform.rotation;
        float originalFieldOfView = worldCamera.fieldOfView;
        float originalOrthographicSize = worldCamera.orthographicSize;

        try
        {
            ApplyFullscreenIsoCameraModeInstant(height, 1f, pitch, yaw);
            float spanAtUnitSize = GetVisibleGroundVerticalSpan();
            if (spanAtUnitSize <= 0.01f)
                return fullscreenIsoOrthographicSize;

            return targetGroundSpan / spanAtUnitSize;
        }
        finally
        {
            worldCamera.orthographic = originalOrthographic;
            worldCamera.transform.position = originalPosition;
            worldCamera.transform.rotation = originalRotation;
            worldCamera.fieldOfView = originalFieldOfView;
            worldCamera.orthographicSize = originalOrthographicSize;
        }
    }

    private float CalculatePerspectiveHeightForGroundSpan(float targetGroundSpan, float pitch, float yaw, float fieldOfView)
    {
        if (worldCamera == null || targetGroundSpan <= 0.01f)
            return normalModeZoomHeight;

        bool originalOrthographic = worldCamera.orthographic;
        Vector3 originalPosition = worldCamera.transform.position;
        Quaternion originalRotation = worldCamera.transform.rotation;
        float originalFieldOfView = worldCamera.fieldOfView;
        float originalOrthographicSize = worldCamera.orthographicSize;

        try
        {
            float low = minZoomHeight;
            float high = maxZoomHeight;

            for (int i = 0; i < 18; i++)
            {
                float mid = (low + high) * 0.5f;
                ApplyPerspectiveCameraModeInstant(mid, pitch, yaw, fieldOfView);
                float span = GetVisibleGroundVerticalSpan();
                if (span < targetGroundSpan)
                    low = mid;
                else
                    high = mid;
            }

            return (low + high) * 0.5f;
        }
        finally
        {
            worldCamera.orthographic = originalOrthographic;
            worldCamera.transform.position = originalPosition;
            worldCamera.transform.rotation = originalRotation;
            worldCamera.fieldOfView = originalFieldOfView;
            worldCamera.orthographicSize = originalOrthographicSize;
        }
    }

    private bool UpdatePerspectiveCameraMode(float targetHeight, float targetPitch, float targetYaw, float targetFieldOfView)
    {
        if (worldCamera == null)
            return true;

        if (worldCamera.orthographic)
            worldCamera.orthographic = false;

        float newHeight = Mathf.SmoothDamp(
            worldCamera.transform.position.y,
            targetHeight,
            ref _zoomTransitionVelocity,
            zoomTransitionSmoothTime);

        Vector3 position = worldCamera.transform.position;
        position.y = newHeight;
        worldCamera.transform.position = position;

        Vector3 euler = worldCamera.transform.rotation.eulerAngles;
        float newPitch = Mathf.SmoothDampAngle(euler.x, targetPitch, ref _pitchTransitionVelocity, zoomTransitionSmoothTime);
        float newYaw = Mathf.SmoothDampAngle(euler.y, targetYaw, ref _yawTransitionVelocity, zoomTransitionSmoothTime);
        worldCamera.transform.rotation = Quaternion.Euler(newPitch, newYaw, 0f);

        worldCamera.fieldOfView = Mathf.SmoothDamp(
            worldCamera.fieldOfView,
            targetFieldOfView,
            ref _fieldOfViewTransitionVelocity,
            zoomTransitionSmoothTime);

        return Mathf.Abs(newHeight - targetHeight) <= 0.05f &&
               Mathf.Abs(Mathf.DeltaAngle(newPitch, targetPitch)) <= 0.1f &&
               Mathf.Abs(Mathf.DeltaAngle(newYaw, targetYaw)) <= 0.1f &&
               Mathf.Abs(worldCamera.fieldOfView - targetFieldOfView) <= 0.05f;
    }

    private bool UpdateFullscreenIsoCameraMode(float targetHeight, float targetOrthographicSize, float targetPitch, float targetYaw)
    {
        if (worldCamera == null)
            return true;

        if (!worldCamera.orthographic)
            worldCamera.orthographic = true;

        float newHeight = Mathf.SmoothDamp(
            worldCamera.transform.position.y,
            targetHeight,
            ref _zoomTransitionVelocity,
            zoomTransitionSmoothTime);

        Vector3 position = worldCamera.transform.position;
        position.y = newHeight;
        worldCamera.transform.position = position;

        Vector3 euler = worldCamera.transform.rotation.eulerAngles;
        float newPitch = Mathf.SmoothDampAngle(euler.x, targetPitch, ref _pitchTransitionVelocity, zoomTransitionSmoothTime);
        float newYaw = Mathf.SmoothDampAngle(euler.y, targetYaw, ref _yawTransitionVelocity, zoomTransitionSmoothTime);
        worldCamera.transform.rotation = Quaternion.Euler(newPitch, newYaw, 0f);

        worldCamera.orthographicSize = Mathf.SmoothDamp(
            worldCamera.orthographicSize,
            targetOrthographicSize,
            ref _orthographicSizeTransitionVelocity,
            zoomTransitionSmoothTime);

        return Mathf.Abs(newHeight - targetHeight) <= 0.05f &&
               Mathf.Abs(Mathf.DeltaAngle(newPitch, targetPitch)) <= 0.1f &&
               Mathf.Abs(Mathf.DeltaAngle(newYaw, targetYaw)) <= 0.1f &&
               Mathf.Abs(worldCamera.orthographicSize - targetOrthographicSize) <= 0.05f;
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
        _buildingPlacementController ??= BuildingPlacementSystem.Instance;
        _buildingPlacementController?.ClearSelectedBuilding("RTSSelection.FocusUnitEntity");
        _ignoreNextLeftMouseRelease = true;
        _ignoreWorldCommandsUntilFrame = Time.frameCount + 1;
        InitialUnitsRuntimeState.SuppressNextWorldClick = true;
        _cameraDragging = false;
        if (em.HasComponent<UnitAirMovement>(entity))
            ClearAccidentalAirSelectionMove(em, entity);
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

            IssueMoveCommand(em, entity, goal);
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
        TacticalCommandResult targetValidation = ValidateAttackTarget(em, targetEntity);
        if (!targetValidation.Accepted)
            return ApplyAndReturn(targetValidation);

        using var selectedEntities = _selectedAttackQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
        if (selectedEntities.Length == 0)
            return ApplyAndReturn(TacticalCommandResult.Rejected(TacticalCommandReasonCode.NoSelection));

        LocalTransform targetTransform = em.GetComponentData<LocalTransform>(targetEntity);
        int2 targetCell = em.HasComponent<UnitGrid>(targetEntity)
            ? em.GetComponentData<UnitGrid>(targetEntity).Cell
            : default;
        int issuedCount = 0;
        for (int i = 0; i < selectedEntities.Length; i++)
        {
            Entity entity = selectedEntities[i];
            TacticalCommandResult sourceValidation = ValidateControllableEntity(entity);
            if (!sourceValidation.Accepted ||
                !em.HasComponent<UnitCombat>(entity) ||
                em.GetComponentData<UnitCombat>(entity).CanAttack == 0)
            {
                continue;
            }

            if (em.HasComponent<ManualMoveOrderTag>(entity))
                em.RemoveComponent<ManualMoveOrderTag>(entity);
            if (em.HasComponent<AutoWanderMoveTag>(entity))
                em.RemoveComponent<AutoWanderMoveTag>(entity);
            if (em.HasComponent<UnitPathFollow>(entity))
                em.RemoveComponent<UnitPathFollow>(entity);
            if (em.HasComponent<UnitPathRange>(entity))
                em.RemoveComponent<UnitPathRange>(entity);
            if (em.HasComponent<UnitPathRequest>(entity))
                em.RemoveComponent<UnitPathRequest>(entity);
            if (em.HasComponent<UnitTarget>(entity))
                em.RemoveComponent<UnitTarget>(entity);
            if (em.HasComponent<BaseBreachOrder>(entity))
                em.RemoveComponent<BaseBreachOrder>(entity);

            EngageTarget engageTarget = new()
            {
                Target = targetEntity,
                Cell = targetCell,
                Position = targetTransform.Position,
                IsCommanded = 1
            };
            if (em.HasComponent<EngageTarget>(entity))
                em.SetComponentData(entity, engageTarget);
            else
                em.AddComponentData(entity, engageTarget);
            issuedCount++;
        }

        TacticalCommandResult result = issuedCount > 0
            ? TacticalCommandResult.Success()
            : TacticalCommandResult.Rejected(TacticalCommandReasonCode.TargetNotAttackable);
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

    private static TacticalCommandResult ValidateAttackTarget(EntityManager em, Entity targetEntity)
    {
        if (targetEntity == Entity.Null ||
            !em.Exists(targetEntity) ||
            !em.HasComponent<Faction>(targetEntity) ||
            !em.HasComponent<LocalTransform>(targetEntity))
        {
            return TacticalCommandResult.Rejected(TacticalCommandReasonCode.TargetNotAttackable);
        }

        if (em.GetComponentData<Faction>(targetEntity).Id == 0)
            return TacticalCommandResult.Rejected(TacticalCommandReasonCode.TargetNotAttackable);
        if (em.HasComponent<UnitHealth>(targetEntity) && em.GetComponentData<UnitHealth>(targetEntity).Current <= 0)
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
        _ignoreUiClickUntilRelease = true;
        _ignoreNextLeftMouseRelease = true;
        _pointerPressedOverUi = true;
        _dragging = false;
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
        IssueMoveCommand(em, entity, goal);
    }

    public void EnableFocusedUnitAutoAttack()
    {
        if (!TryGetFocusedUnitEntity(out var em, out Entity entity) || !FocusedUnitOwnedByPlayer)
            return;

        if (em.HasComponent<EngageTarget>(entity))
            em.RemoveComponent<EngageTarget>(entity);
        if (em.HasComponent<UnitTarget>(entity))
            em.RemoveComponent<UnitTarget>(entity);
        if (em.HasComponent<UnitPathRequest>(entity))
            em.RemoveComponent<UnitPathRequest>(entity);
        if (em.HasComponent<UnitPathFollow>(entity))
            em.RemoveComponent<UnitPathFollow>(entity);
        if (em.HasComponent<UnitPathRange>(entity))
            em.RemoveComponent<UnitPathRange>(entity);
        if (em.HasComponent<ManualMoveOrderTag>(entity))
            em.RemoveComponent<ManualMoveOrderTag>(entity);
        if (em.HasComponent<AutoWanderMoveTag>(entity))
            em.RemoveComponent<AutoWanderMoveTag>(entity);
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
        if (!TryFindRadarTargetForMissileLauncher(em, factionId, mode, launcher, out Entity target, out int2 targetCell, out float3 targetPosition))
            return false;

        if (em.HasComponent<ManualMoveOrderTag>(launcher))
            em.RemoveComponent<ManualMoveOrderTag>(launcher);
        if (em.HasComponent<AutoWanderMoveTag>(launcher))
            em.RemoveComponent<AutoWanderMoveTag>(launcher);
        if (em.HasComponent<UnitPathFollow>(launcher))
            em.RemoveComponent<UnitPathFollow>(launcher);
        if (em.HasComponent<UnitPathRange>(launcher))
            em.RemoveComponent<UnitPathRange>(launcher);
        if (em.HasComponent<UnitPathRequest>(launcher))
            em.RemoveComponent<UnitPathRequest>(launcher);
        if (em.HasComponent<UnitTarget>(launcher))
            em.RemoveComponent<UnitTarget>(launcher);
        if (em.HasComponent<BaseBreachOrder>(launcher))
            em.RemoveComponent<BaseBreachOrder>(launcher);

        EngageTarget engage = new()
        {
            Target = target,
            Cell = targetCell,
            Position = targetPosition,
            IsCommanded = 1
        };

        if (em.HasComponent<EngageTarget>(launcher))
            em.SetComponentData(launcher, engage);
        else
            em.AddComponentData(launcher, engage);

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
        InitialUnitsRuntimeState.SelectionModeActive = false;
        InitialUnitsRuntimeState.SuppressNextWorldClick = true;
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

            RemoveComponentIfPresent<UnitTarget>(em, entity);
            RemoveComponentIfPresent<UnitPathRequest>(em, entity);
            RemoveComponentIfPresent<UnitPathFollow>(em, entity);
            RemoveComponentIfPresent<UnitPathRange>(em, entity);
            RemoveComponentIfPresent<UnitPathRetryCooldown>(em, entity);
            RemoveComponentIfPresent<AutoWanderMoveTag>(em, entity);
            RemoveComponentIfPresent<BaseBreachOrder>(em, entity);
            if (clearEngageTarget)
                RemoveComponentIfPresent<EngageTarget>(em, entity);
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

    private static bool TryFindRadarTargetForMissileLauncher(
        EntityManager em,
        byte factionId,
        MissileLauncherTargetMode mode,
        Entity launcher,
        out Entity bestTarget,
        out int2 bestTargetCell,
        out float3 bestTargetPosition)
    {
        bestTarget = Entity.Null;
        bestTargetCell = default;
        bestTargetPosition = default;

        int detectorKind = mode == MissileLauncherTargetMode.Air
            ? (byte)ThreatDetectionKind.Air
            : (byte)ThreatDetectionKind.Ground;

        using EntityQuery detectorQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<ThreatDetector>(),
            ComponentType.ReadOnly<Faction>(),
            ComponentType.ReadOnly<UnitGrid>(),
            ComponentType.ReadOnly<UnitHealth>());
        using EntityQuery targetQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<Faction>(),
            ComponentType.ReadOnly<UnitGrid>(),
            ComponentType.ReadOnly<UnitHealth>(),
            ComponentType.ReadOnly<LocalTransform>());

        using var detectors = detectorQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
        using var targets = targetQuery.ToEntityArray(Unity.Collections.Allocator.Temp);

        int2 launcherCell = em.HasComponent<UnitGrid>(launcher)
            ? em.GetComponentData<UnitGrid>(launcher).Cell
            : default;
        int bestLauncherDistance = int.MaxValue;

        for (int i = 0; i < targets.Length; i++)
        {
            Entity target = targets[i];
            if (!em.Exists(target) || target == launcher)
                continue;
            if (em.HasComponent<RuntimeBuildingCombatTag>(target))
                continue;

            Faction targetFaction = em.GetComponentData<Faction>(target);
            if (targetFaction.Id == factionId)
                continue;

            UnitHealth targetHealth = em.GetComponentData<UnitHealth>(target);
            if (targetHealth.Current <= 0)
                continue;

            bool isAirTarget = em.HasComponent<UnitAirMovement>(target);
            if ((mode == MissileLauncherTargetMode.Air && !isAirTarget) ||
                (mode == MissileLauncherTargetMode.Ground && isAirTarget))
                continue;
            if (mode == MissileLauncherTargetMode.Ground && !em.HasComponent<UnitMove>(target))
                continue;

            int2 targetCell = em.GetComponentData<UnitGrid>(target).Cell;
            if (!IsInFriendlyDetectorRadius(em, detectors, factionId, detectorKind, targetCell))
                continue;

            int launcherDistance = ChebyshevDistance(launcherCell, targetCell);
            if (launcherDistance >= bestLauncherDistance)
                continue;

            bestTarget = target;
            bestTargetCell = targetCell;
            bestTargetPosition = em.GetComponentData<LocalTransform>(target).Position;
            bestLauncherDistance = launcherDistance;
        }

        return bestTarget != Entity.Null;
    }

    private static bool IsInFriendlyDetectorRadius(EntityManager em, Unity.Collections.NativeArray<Entity> detectors, byte factionId, int detectorKind, int2 targetCell)
    {
        for (int i = 0; i < detectors.Length; i++)
        {
            Entity detector = detectors[i];
            if (!em.Exists(detector))
                continue;

            Faction detectorFaction = em.GetComponentData<Faction>(detector);
            if (detectorFaction.Id != factionId)
                continue;

            UnitHealth detectorHealth = em.GetComponentData<UnitHealth>(detector);
            if (detectorHealth.Current <= 0)
                continue;

            ThreatDetector threatDetector = em.GetComponentData<ThreatDetector>(detector);
            if (threatDetector.Kind != detectorKind || threatDetector.RadiusCells <= 0)
                continue;

            int2 detectorCell = em.GetComponentData<UnitGrid>(detector).Cell;
            if (ChebyshevDistance(detectorCell, targetCell) <= threatDetector.RadiusCells)
                return true;
        }

        return false;
    }

    private static int ChebyshevDistance(int2 a, int2 b)
    {
        int2 delta = math.abs(a - b);
        return math.max(delta.x, delta.y);
    }

    private bool TryFocusUnit(Vector2 screenPosition)
    {
        if (World.DefaultGameObjectInjectionWorld == null)
            return false;

        var em = World.DefaultGameObjectInjectionWorld.EntityManager;
        EnsureEntityQueries(em);
        if (!TryGetClickedUnitEntity(screenPosition, em, out Entity bestEntity))
            return false;
        if (IsBuildingEntity(em, bestEntity))
            return false;

        ClearCurrentSelection(em, "TryFocusUnit");
        if (em.GetComponentData<Faction>(bestEntity).Id == 0 && !em.HasComponent<SelectedUnitTag>(bestEntity))
            em.AddComponent<SelectedUnitTag>(bestEntity);
        CacheSelectedMoveEntity(em, bestEntity);
        LogSelectionDiagnostic($"result=Focus source=TryFocusUnit entity={DescribeTransportBoardingEntity(em, bestEntity)} cache={_cachedSelectedMoveEntities.Count}");

        _focusedUnit = bestEntity;
        _buildingPlacementController ??= BuildingPlacementSystem.Instance;
        _buildingPlacementController?.ClearSelectedBuilding("RTSSelection.TryFocusUnit");
        _ignoreNextLeftMouseRelease = true;
        _ignoreWorldCommandsUntilFrame = Time.frameCount + 1;
        InitialUnitsRuntimeState.SuppressNextWorldClick = true;
        _cameraDragging = false;
        if (em.HasComponent<UnitAirMovement>(bestEntity))
            ClearAccidentalAirSelectionMove(em, bestEntity);
        ApplyHudSelection(em, bestEntity);
        return true;
    }

    private static void ClearAccidentalAirSelectionMove(EntityManager em, Entity entity)
    {
        if (!em.Exists(entity) ||
            !em.HasComponent<UnitAirMovement>(entity) ||
            !em.HasComponent<UnitGrid>(entity) ||
            em.HasComponent<EngageTarget>(entity) ||
            !em.HasComponent<UnitTarget>(entity) ||
            !em.HasComponent<ManualMoveOrderTag>(entity))
        {
            return;
        }

        int2 currentCell = em.GetComponentData<UnitGrid>(entity).Cell;
        int2 targetCell = em.GetComponentData<UnitTarget>(entity).Cell;
        int2 delta = targetCell - currentCell;
        if (math.abs(delta.x) > 1 || math.abs(delta.y) > 1)
            return;

        em.RemoveComponent<UnitTarget>(entity);
        if (em.HasComponent<UnitPathRequest>(entity))
            em.RemoveComponent<UnitPathRequest>(entity);
        if (em.HasComponent<UnitPathFollow>(entity))
            em.RemoveComponent<UnitPathFollow>(entity);
        if (em.HasComponent<UnitPathRange>(entity))
            em.RemoveComponent<UnitPathRange>(entity);
        if (em.HasComponent<ManualMoveOrderTag>(entity))
            em.RemoveComponent<ManualMoveOrderTag>(entity);
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
        if (!em.HasComponent<Faction>(targetEntity) || em.GetComponentData<Faction>(targetEntity).Id == 0)
        {
            if (_explicitAttackTargetModeActive)
                ApplyHudCommandResult(TacticalCommandResult.Rejected(TacticalCommandReasonCode.TargetNotAttackable));
            return false;
        }

        using var selectedEntities = _selectedAttackQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
        if (selectedEntities.Length == 0)
        {
            ApplyHudCommandResult(TacticalCommandResult.Rejected(TacticalCommandReasonCode.NoSelection));
            return false;
        }

        LocalTransform targetTransform = em.GetComponentData<LocalTransform>(targetEntity);
        int2 targetCell = em.HasComponent<UnitGrid>(targetEntity)
            ? em.GetComponentData<UnitGrid>(targetEntity).Cell
            : default;
        ShowAttackOrderMarker(em, targetTransform.Position);

        for (int i = 0; i < selectedEntities.Length; i++)
        {
            Entity entity = selectedEntities[i];
            if (em.GetComponentData<UnitCombat>(entity).CanAttack == 0)
                continue;

            if (em.HasComponent<ManualMoveOrderTag>(entity))
                em.RemoveComponent<ManualMoveOrderTag>(entity);
            if (em.HasComponent<AutoWanderMoveTag>(entity))
                em.RemoveComponent<AutoWanderMoveTag>(entity);
            Entity engageTarget = targetEntity;
            int2 engageCell = targetCell;
            float3 engagePosition = targetTransform.Position;
            bool issuedBreachOrder = false;
            if (BuildingPlacementSystem.Instance != null &&
                em.HasComponent<Faction>(entity) &&
                em.HasComponent<UnitGrid>(entity) &&
                BuildingPlacementSystem.Instance.TryResolveBaseBreachTarget(
                    em.GetComponentData<Faction>(entity).Id,
                    targetEntity,
                    targetCell,
                    em.GetComponentData<UnitGrid>(entity).Cell,
                    out Entity breachTarget,
                    out int2 breachCell,
                    out float3 breachPosition,
                    out _))
            {
                engageTarget = breachTarget;
                engageCell = breachCell;
                engagePosition = breachPosition;
                issuedBreachOrder = true;
            }

            if (issuedBreachOrder)
            {
                if (em.HasComponent<EngageTarget>(entity))
                    em.RemoveComponent<EngageTarget>(entity);
                if (em.HasComponent<UnitTarget>(entity))
                    em.SetComponentData(entity, new UnitTarget { Cell = engageCell });
                else
                    em.AddComponentData(entity, new UnitTarget { Cell = engageCell });
                if (em.HasComponent<UnitPathRequest>(entity))
                    em.SetComponentData(entity, new UnitPathRequest { Goal = engageCell });
                else
                    em.AddComponentData(entity, new UnitPathRequest { Goal = engageCell });
                if (!em.HasComponent<ManualMoveOrderTag>(entity))
                    em.AddComponent<ManualMoveOrderTag>(entity);
            }
            else if (em.HasComponent<EngageTarget>(entity))
            {
                em.SetComponentData(entity, new EngageTarget
                {
                    Target = engageTarget,
                    Cell = engageCell,
                    Position = engagePosition,
                    IsCommanded = 1
                });
            }
            else
            {
                em.AddComponentData(entity, new EngageTarget
                {
                    Target = engageTarget,
                    Cell = engageCell,
                    Position = engagePosition,
                    IsCommanded = 1
                });
            }

            if (issuedBreachOrder)
            {
                BaseBreachOrder breachOrder = new()
                {
                    FinalTarget = targetEntity,
                    FinalCell = targetCell,
                    FinalPosition = targetTransform.Position,
                    BreachTarget = engageTarget,
                    BreachCell = engageCell,
                    BreachPosition = engagePosition,
                    Stage = BaseBreachOrder.StageMovingToEnemyBreach,
                    IsCommanded = 1
                };

                if (em.HasComponent<BaseBreachOrder>(entity))
                    em.SetComponentData(entity, breachOrder);
                else
                    em.AddComponentData(entity, breachOrder);
            }
            else if (em.HasComponent<BaseBreachOrder>(entity))
            {
                em.RemoveComponent<BaseBreachOrder>(entity);
            }

            if (em.HasComponent<UnitPathFollow>(entity))
                em.RemoveComponent<UnitPathFollow>(entity);
            if (em.HasComponent<UnitPathRange>(entity))
                em.RemoveComponent<UnitPathRange>(entity);
            if (!issuedBreachOrder && em.HasComponent<UnitPathRequest>(entity))
                em.RemoveComponent<UnitPathRequest>(entity);
            if (!issuedBreachOrder && em.HasComponent<UnitTarget>(entity))
                em.RemoveComponent<UnitTarget>(entity);
        }

        bool issuedAttackOrder = false;
        for (int i = 0; i < selectedEntities.Length; i++)
        {
            if (em.GetComponentData<UnitCombat>(selectedEntities[i]).CanAttack != 0)
            {
                issuedAttackOrder = true;
                break;
            }
        }

        if (!issuedAttackOrder)
        {
            ApplyHudCommandResult(TacticalCommandResult.Rejected(TacticalCommandReasonCode.TargetNotAttackable));
            return false;
        }

        AttackOrderScreenMarkerRequested?.Invoke(screenPosition);
        ClearCurrentSelection(em, "AttackOrderIssued");
        _focusedUnit = Entity.Null;
        _cameraDragging = false;
        ApplyHudCommandResult(TacticalCommandResult.Success());
        ClearHudCommandMode();
        SetHudWorldMarkersVisible(true);
        return true;
    }

    private static bool IsBuildingEntity(EntityManager em, Entity entity)
    {
        if (entity == Entity.Null || !em.Exists(entity))
            return false;
        if (em.HasComponent<UnitMove>(entity))
            return false;
        if (!em.HasComponent<UnitHealth>(entity) || !em.HasComponent<UnitRespawnPrefab>(entity))
            return false;

        return em.GetComponentData<UnitRespawnPrefab>(entity).Prefab == Entity.Null;
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

    private static void IssueMoveCommand(EntityManager em, Entity entity, int2 goal)
    {
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

    private static T ResolveDependency<T>() where T : class
    {
        if (typeof(T) == typeof(MainMenuPlayUI))
            return MainMenuPlayUI.Instance as T;
        if (typeof(T) == typeof(RoadBuildSystem))
            return RoadBuildSystem.Instance as T;
        if (typeof(T) == typeof(BuildingPlacementSystem))
            return BuildingPlacementSystem.Instance as T;
        return null;
    }
}
