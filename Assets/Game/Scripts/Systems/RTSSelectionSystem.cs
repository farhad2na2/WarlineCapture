using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.InputSystem;
using Unity.Transforms;
using UnityEngine;

public sealed class RTSSelectionSystem
{

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

    private readonly RuntimeGameplayStateSystem _runtimeGameplayStateSystem = new();
    private readonly RtsSelectionInputSystem _rtsSelectionInputSystem = new();
    private RtsCameraSystem _rtsCameraSystem = new();
    private RtsCameraRequestSystem _rtsCameraRequestSystem = new();
    private SelectionScreenMarkerSystem _selectionScreenMarkerSystem;
    private SelectionStateSystem _selectionStateSystem = new();
    private readonly SelectionUiQuerySystem _selectionUiQuerySystem = new();
    private readonly FocusedUnitUiReadModelSystem _focusedUnitUiReadModelSystem = new();
    private readonly VisibleUnitSelectionSystem _visibleUnitSelectionSystem = new();
    private readonly SelectionRectangleRequestSystem _selectionRectangleRequestSystem = new();
    private readonly UnitMoveOrderSystem _unitMoveOrderSystem = new();
    private readonly SelectedMoveOrderCommandSystem _selectedMoveOrderCommandSystem = new();
    private readonly SelectionMoveCommandRequestSystem _selectionMoveCommandRequestSystem = new();
    private readonly UnitTargetOrderSystem _unitTargetOrderSystem = new();
    private readonly AttackOrderCommandSystem _attackOrderCommandSystem = new();
    private readonly SelectionAttackCommandRequestSystem _selectionAttackCommandRequestSystem = new();
    private readonly SelectionOrderMarkerSystem _selectionOrderMarkerSystem = new();
    private readonly SelectionHudFeedbackSystem _selectionHudFeedbackSystem = new();
    private readonly FocusedUnitCommandSystem _focusedUnitCommandSystem = new();
    private readonly FocusedUnitLifecycleSystem _focusedUnitLifecycleSystem = new();
    private readonly SelectedUnitOrderSnapshotSystem _selectedUnitOrderSnapshotSystem = new();
    private readonly BuildingTargetMoveOrderSystem _buildingTargetMoveOrderSystem = new();
    private readonly TransportBoardingCommandSystem _transportBoardingCommandSystem = new();
    private readonly SelectionTransportCommandRequestSystem _selectionTransportCommandRequestSystem = new();
    private readonly FocusableUnitLookupSystem _focusableUnitLookupSystem = new();
    private UnitTransportBoardingSystem _unitTransportBoardingSystem;
    private List<Entity> _cachedSelectedMoveEntities => _selectionStateSystem.CachedSelectedMoveEntities;
    private bool _cameraDragging
    {
        get => _rtsCameraSystem.IsDragging;
        set => SetCameraDragging(value);
    }

    private bool _wasPlayRequested
    {
        get => _rtsCameraSystem.WasPlayRequested;
        set => SetCameraWasPlayRequested(value);
    }

    private bool _wasBuildModeActive
    {
        get => _rtsCameraSystem.WasBuildModeActive;
        set => SetCameraWasBuildModeActive(value);
    }

    private bool _isZoomTransitionActive
    {
        get => _rtsCameraSystem.IsZoomTransitionActive;
        set => SetCameraZoomTransitionActive(value);
    }

    private float _fullscreenIsoTargetHeight
    {
        get => _rtsCameraSystem.FullscreenIsoTargetHeight;
        set => SetFullscreenIsoTargets(value, _rtsCameraSystem.FullscreenIsoTargetOrthographicSize);
    }

    private float _fullscreenIsoTargetOrthographicSize
    {
        get => _rtsCameraSystem.FullscreenIsoTargetOrthographicSize;
        set => SetFullscreenIsoTargets(_rtsCameraSystem.FullscreenIsoTargetHeight, value);
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
    private readonly List<Entity> _visibleSelectionScratch = new();
    private readonly List<RtsSelectionCommandIntentKind> _externalSelectionCommandScratch = new();
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
            return TryReadFocusedUnitUiModel(out FocusedUnitUiReadModelComponent model) &&
                   model.HasFocusedUnit != 0;
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
            return TryReadFocusedUnitUiModel(out FocusedUnitUiReadModelComponent model)
                ? model.Label.ToString()
                : "Unit";
        }
    }

    public string FocusedUnitDescription
    {
        get
        {
            return TryReadFocusedUnitUiModel(out FocusedUnitUiReadModelComponent model)
                ? model.Description.ToString()
                : "Select a unit to inspect it.";
        }
    }

    public string FocusedUnitHealthText
    {
        get
        {
            return TryReadFocusedUnitUiModel(out FocusedUnitUiReadModelComponent model)
                ? model.HealthText.ToString()
                : "Health: -";
        }
    }

    public bool TryGetFocusedUnitHealth(out int current, out int max)
    {
        current = 0;
        max = 0;

        if (!TryReadFocusedUnitUiModel(out FocusedUnitUiReadModelComponent model) || model.HasHealth == 0)
            return false;

        current = model.HealthCurrent;
        max = model.HealthMax;
        return true;
    }

    public bool TryGetFocusedUnitCapacityInfo(out int current, out int max, out float progress01)
    {
        current = 0;
        max = 0;
        progress01 = 0f;

        if (!TryReadFocusedUnitUiModel(out FocusedUnitUiReadModelComponent model) || model.HasCapacity == 0)
            return false;

        current = model.CapacityCurrent;
        max = model.CapacityMax;
        progress01 = model.CapacityProgress01;
        return true;
    }

    public bool FocusedUnitOwnedByPlayer
    {
        get
        {
            return TryReadFocusedUnitUiModel(out FocusedUnitUiReadModelComponent model) &&
                   model.OwnedByPlayer != 0;
        }
    }

    public bool CanDestroyFocusedUnit => FocusedUnitOwnedByPlayer;

    public bool CanCommandFocusedUnit => HasFocusedUnit && FocusedUnitOwnedByPlayer;

    public bool FocusedUnitIsVehicle
    {
        get
        {
            return TryReadFocusedUnitUiModel(out FocusedUnitUiReadModelComponent model) &&
                   model.IsVehicle != 0;
        }
    }

    public bool CanReturnFocusedUnitToBase => CanCommandFocusedUnit && !FocusedUnitIsVehicle;

    public bool CanFocusedUnitUseAutoAttack => CanCommandFocusedUnit && !FocusedUnitIsVehicle;

    public bool FocusedUnitCanAttack
    {
        get
        {
            return TryReadFocusedUnitUiModel(out FocusedUnitUiReadModelComponent model) &&
                   model.CanAttack != 0;
        }
    }

    public bool ExplicitAttackTargetModeActive => _explicitAttackTargetModeActive;

    public int FocusedTransportPassengerCount
    {
        get
        {
            return TryReadFocusedUnitUiModel(out FocusedUnitUiReadModelComponent model)
                ? model.PassengerCount
                : 0;
        }
    }

    public bool CanDisembarkFocusedTransport => FocusedTransportPassengerCount > 0;

    public void GetFocusedTransportPassengers(List<TransportPassengerUiInfo> results)
    {
        if (results == null)
            return;

        results.Clear();
        if (TryReadFocusedUnitUiModel(
                out _,
                out DynamicBuffer<FocusedUnitPassengerUiReadModelElement> passengers))
        {
            for (int i = 0; i < passengers.Length; i++)
            {
                FocusedUnitPassengerUiReadModelElement passenger = passengers[i];
                results.Add(new TransportPassengerUiInfo(
                    passenger.Passenger,
                    passenger.DisplayName.ToString(),
                    passenger.HealthCurrent,
                    passenger.HealthMax));
            }
        }
    }

    public void DisembarkFocusedTransport()
    {
        if (!TryGetFocusedUnitEntity(out _, out Entity transport))
            return;
        if (!_rtsSelectionInputSystem.QueueDisembarkTransportCommandRequest(transport, Time.frameCount))
            return;

        ProcessTransportCommandRequests();
    }

    public bool TryGetFocusedUnitWorldPosition(out Vector3 worldPosition)
    {
        worldPosition = default;
        if (!TryReadFocusedUnitUiModel(out FocusedUnitUiReadModelComponent model) || model.HasWorldPosition == 0)
            return false;

        worldPosition = new Vector3(model.WorldPosition.x, model.WorldPosition.y, model.WorldPosition.z);
        return true;
    }

    public bool TryGetFocusedUnitEntityForUi(out Entity entity)
    {
        entity = Entity.Null;
        if (!TryReadFocusedUnitUiModel(out FocusedUnitUiReadModelComponent model) || model.HasFocusedUnit == 0)
            return false;

        entity = model.FocusedUnit;
        return true;
    }

    public FocusedUnitUiStatus GetFocusedUnitUiStatus()
    {
        return TryReadFocusedUnitUiModel(out FocusedUnitUiReadModelComponent model)
            ? ToFocusedUnitUiStatus(model.Status)
            : FocusedUnitUiStatus.Idle;
    }

    public bool TryGetFocusedUnitPortraitPose(out Vector3 worldPosition, out Vector3 forward)
    {
        worldPosition = default;
        forward = Vector3.forward;

        if (!TryReadFocusedUnitUiModel(out FocusedUnitUiReadModelComponent model) || model.HasPortraitPose == 0)
            return false;

        worldPosition = new Vector3(model.PortraitWorldPosition.x, model.PortraitWorldPosition.y, model.PortraitWorldPosition.z);
        forward = new Vector3(model.PortraitForward.x, model.PortraitForward.y, model.PortraitForward.z);
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

    private bool TryReadFocusedUnitUiModel(out FocusedUnitUiReadModelComponent model)
    {
        return TryReadFocusedUnitUiModel(out model, out _);
    }

    private void PublishFocusedUnitUiReadModel()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return;

        _focusedUnitUiReadModelSystem.Publish(
            world.EntityManager,
            _selectionStateSystem,
            _selectionUiQuerySystem,
            _unitTransportBoardingSystem,
            Time.time);
    }

    private bool TryReadFocusedUnitUiModel(
        out FocusedUnitUiReadModelComponent model,
        out DynamicBuffer<FocusedUnitPassengerUiReadModelElement> passengers)
    {
        model = default;
        passengers = default;
        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return false;

        EntityManager em = world.EntityManager;
        PublishFocusedUnitUiReadModel();
        return _focusedUnitUiReadModelSystem.TryRead(em, out model, out passengers);
    }

    private static FocusedUnitUiStatus ToFocusedUnitUiStatus(int status)
    {
        return (SelectionUiQuerySystem.FocusedUnitUiStatus)status switch
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

    public void BindCameraBoundary(
        RtsCameraSystem cameraSystem,
        RtsCameraRequestSystem cameraRequestSystem,
        SelectionScreenMarkerSystem screenMarkerSystem)
    {
        _rtsCameraSystem = cameraSystem ?? _rtsCameraSystem ?? new RtsCameraSystem();
        _rtsCameraRequestSystem = cameraRequestSystem ?? _rtsCameraRequestSystem ?? new RtsCameraRequestSystem();
        _selectionScreenMarkerSystem = screenMarkerSystem;
    }

    public void BindSelectionState(SelectionStateSystem selectionStateSystem)
    {
        _selectionStateSystem = selectionStateSystem ?? _selectionStateSystem ?? new SelectionStateSystem();
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
        _selectionHudFeedbackSystem.QueueSelection(em, entity, _selectionUiQuerySystem);
        _selectionHudFeedbackSystem.ProcessPendingFeedback(em);
    }

    private void ApplyHudSquadSelection(int selectedCount)
    {
        if (!TryGetDefaultEntityManager(out EntityManager em))
            return;

        _selectionHudFeedbackSystem.QueueSquadSelection(em, selectedCount);
        _selectionHudFeedbackSystem.ProcessPendingFeedback(em);
    }

    private void ClearHudSelection()
    {
        if (!TryGetDefaultEntityManager(out EntityManager em))
            return;

        _selectionHudFeedbackSystem.QueueClearSelection(em);
        _selectionHudFeedbackSystem.ProcessPendingFeedback(em);
    }

    private void ApplyHudCommandMode(TacticalCommandMode mode)
    {
        if (!TryGetDefaultEntityManager(out EntityManager em))
            return;

        _selectionHudFeedbackSystem.QueueCommandMode(em, mode);
        _selectionHudFeedbackSystem.ProcessPendingFeedback(em);
    }

    private void ClearHudCommandMode()
    {
        if (!TryGetDefaultEntityManager(out EntityManager em))
            return;

        _selectionHudFeedbackSystem.QueueClearCommandMode(em);
        _selectionHudFeedbackSystem.ProcessPendingFeedback(em);
    }

    private void ApplyHudCommandResult(TacticalCommandResult result)
    {
        if (!TryGetDefaultEntityManager(out EntityManager em))
            return;

        _selectionHudFeedbackSystem.QueueCommandResult(em, result);
        _selectionHudFeedbackSystem.ProcessPendingFeedback(em);
    }

    private void SetHudWorldMarkersVisible(bool visible)
    {
        if (!TryGetDefaultEntityManager(out EntityManager em))
            return;

        _selectionHudFeedbackSystem.QueueWorldMarkersVisible(em, visible);
        _selectionHudFeedbackSystem.ProcessPendingFeedback(em);
    }

    private bool TryGetDefaultEntityManager(out EntityManager em)
    {
        em = default;
        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return false;

        em = world.EntityManager;
        return true;
    }

    private void ProcessCameraRequests(EntityManager em)
    {
        _rtsCameraRequestSystem.ProcessPendingRequests(em, _rtsCameraSystem, worldCamera, HideOrderScreenMarkers);
    }

    private void HideOrderScreenMarkers()
    {
        _selectionScreenMarkerSystem?.RequestHideOrderMarkers();
    }

    private void RequestMoveOrderScreenMarker(Vector2 screenPosition)
    {
        _selectionScreenMarkerSystem?.RequestMoveOrderMarker(screenPosition);
    }

    private void RequestAttackOrderScreenMarker(Vector2 screenPosition)
    {
        _selectionScreenMarkerSystem?.RequestAttackOrderMarker(screenPosition);
    }

    private void SetCameraDragging(bool isDragging)
    {
        if (_rtsCameraSystem.IsDragging == isDragging)
            return;

        if (!TryGetDefaultEntityManager(out EntityManager em))
            return;

        _rtsCameraRequestSystem.QueueSetDragging(em, isDragging);
        ProcessCameraRequests(em);
    }

    private void SetCameraWasPlayRequested(bool wasPlayRequested)
    {
        if (_rtsCameraSystem.WasPlayRequested == wasPlayRequested)
            return;

        if (!TryGetDefaultEntityManager(out EntityManager em))
            return;

        _rtsCameraRequestSystem.QueueSetWasPlayRequested(em, wasPlayRequested);
        ProcessCameraRequests(em);
    }

    private void SetCameraWasBuildModeActive(bool wasBuildModeActive)
    {
        if (_rtsCameraSystem.WasBuildModeActive == wasBuildModeActive)
            return;

        if (!TryGetDefaultEntityManager(out EntityManager em))
            return;

        _rtsCameraRequestSystem.QueueSetWasBuildModeActive(em, wasBuildModeActive);
        ProcessCameraRequests(em);
    }

    private void SetCameraZoomTransitionActive(bool isActive)
    {
        if (_rtsCameraSystem.IsZoomTransitionActive == isActive)
            return;

        if (!TryGetDefaultEntityManager(out EntityManager em))
            return;

        _rtsCameraRequestSystem.QueueSetZoomTransitionActive(em, isActive);
        ProcessCameraRequests(em);
    }

    private void SetCameraNormalIsoModeActive(bool isActive)
    {
        if (_rtsCameraSystem.NormalIsoModeActive == isActive)
            return;

        if (!TryGetDefaultEntityManager(out EntityManager em))
            return;

        _rtsCameraRequestSystem.QueueSetNormalIsoModeActive(em, isActive);
        ProcessCameraRequests(em);
    }

    private void SetFullscreenIsoTargets(float height, float orthographicSize)
    {
        if (Mathf.Approximately(_rtsCameraSystem.FullscreenIsoTargetHeight, height) &&
            Mathf.Approximately(_rtsCameraSystem.FullscreenIsoTargetOrthographicSize, orthographicSize))
            return;

        if (!TryGetDefaultEntityManager(out EntityManager em))
            return;

        _rtsCameraRequestSystem.QueueSetFullscreenIsoTargets(em, height, orthographicSize);
        ProcessCameraRequests(em);
    }

    private void ResetCameraSession()
    {
        if (!TryGetDefaultEntityManager(out EntityManager em))
            return;

        _rtsCameraRequestSystem.QueueResetSession(em);
        ProcessCameraRequests(em);
    }

    private void ResetCameraModeSession()
    {
        if (!TryGetDefaultEntityManager(out EntityManager em))
            return;

        _rtsCameraRequestSystem.QueueResetCameraModeSession(em);
        ProcessCameraRequests(em);
    }

    public void Dispose()
    {
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
        ProcessTransportCommandRequests();
        ProcessExternalSelectionCommandRequests();
        ProcessQueuedMoveOrder();
        RefreshFocusedUnit();
        PublishFocusedUnitUiReadModel();
        _selectionOrderMarkerSystem.UpdateMoveOrderMarkerVisibility(SetHudWorldMarkersVisible);
        _selectionOrderMarkerSystem.UpdateAttackOrderMarkerVisibility(SetHudWorldMarkersVisible);

        if (!_runtimeGameplayStateSystem.PlayRequested)
        {
            ResetCameraSession();
            ResetCameraModeSession();
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
            UpdateFullscreenIsoCameraMode();
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
            UpdateFullscreenIsoCameraMode();
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
                        QueueSelectionRectangleRequest(liveRect, RtsSelectionPointerRequestKind.SelectionRectUpdated);
                        ProcessSelectionRectangleRequests();
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
                    {
                        QueueSelectionRectangleRequest(GetScreenRect(_dragStart, _dragCurrent), RtsSelectionPointerRequestKind.SelectionRectCommitted);
                        ProcessSelectionRectangleRequests();
                    }
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

    private void QueueSelectionRectangleRequest(
        Rect screenRect,
        RtsSelectionPointerRequestKind kind,
        VisibleUnitSelectionSystem.Filter filter = VisibleUnitSelectionSystem.Filter.All)
    {
        _rtsSelectionInputSystem.QueueSelectionRectangleRequest(kind, screenRect, Time.frameCount, filter);
    }

    private void ProcessSelectionRectangleRequests()
    {
        if (TryGetDefaultEntityManager(out EntityManager defaultEntityManager))
            _selectionHudFeedbackSystem.EnsureFeedbackQueue(defaultEntityManager);

        if (!_rtsSelectionInputSystem.TryGetPointerRequests(out EntityManager em, out DynamicBuffer<RtsSelectionPointerRequestElement> pointerRequests))
            return;

        EnsureEntityQueries(em);
        _selectionRectangleRequestSystem.ProcessPendingRequests(
            em,
            pointerRequests,
            worldCamera,
            _selectionUiQuerySystem,
            _visibleUnitSelectionSystem,
            _selectionStateSystem,
            _focusedUnitLifecycleSystem,
            _visibleSelectionScratch,
            ClearCurrentSelection,
            CacheSelectedMoveEntities,
            ApplyHudSelection,
            ApplyHudSquadSelection,
            LogSelectionDiagnostic,
            ClearSelectedBuildingAfterRectangleSelection);
    }

    private void ClearSelectedBuildingAfterRectangleSelection()
    {
        _buildingPlacementInteractionSystem?.ClearSelectedBuilding(_buildingPlacementInteractionContext, "RTSSelection.SelectUnitsInRectangle");
    }

    private void IssueMoveOrder(Vector2 screenPosition)
    {
        _explicitAttackTargetModeActive = false;
        ApplyHudCommandMode(TacticalCommandMode.Move);

        if (!_rtsSelectionInputSystem.QueueMoveCommandRequest(screenPosition, Time.frameCount))
        {
            ApplyHudCommandResult(TacticalCommandResult.Rejected(TacticalCommandReasonCode.NoSelection));
            ClearHudCommandMode();
            return;
        }

        ProcessMoveCommandRequests();
    }

    private void ProcessMoveCommandRequests()
    {
        if (TryGetDefaultEntityManager(out EntityManager defaultEntityManager))
            _selectionHudFeedbackSystem.EnsureFeedbackQueue(defaultEntityManager);

        if (!_rtsSelectionInputSystem.TryGetCommandBuffers(
                out EntityManager em,
                out DynamicBuffer<RtsSelectionCommandIntentRequestElement> commandRequests,
                out DynamicBuffer<RtsSelectionCommandResultElement> commandResults))
        {
            ApplyHudCommandResult(TacticalCommandResult.Rejected(TacticalCommandReasonCode.NoSelection));
            ClearHudCommandMode();
            return;
        }

        EnsureEntityQueries(em);
        _selectionMoveCommandRequestSystem.ProcessPendingRequests(
            em,
            commandRequests,
            commandResults,
            _selectedMoveQuery,
            _gridConfigQuery,
            _unitMoveOrderSystem,
            _selectionOrderMarkerSystem,
            _selectedMoveOrderCommandSystem,
            TryGetClickedUnitEntity,
            TryGetClickedCell);

        bool handled = false;
        for (int i = 0; i < commandResults.Length;)
        {
            RtsSelectionCommandResultElement result = commandResults[i];
            if (result.Kind != RtsSelectionCommandIntentKind.Move)
            {
                i++;
                continue;
            }

            commandResults.RemoveAt(i);
            ApplyHudCommandResult(ToTacticalCommandResult(result));
            ClearHudCommandMode();
            if (result.EmitScreenMarker != 0)
                RequestMoveOrderScreenMarker(new Vector2(result.ScreenPosition.x, result.ScreenPosition.y));
            if (result.ShowWorldMarkers != 0)
                SetHudWorldMarkersVisible(true);
            handled = true;
        }

        if (!handled)
        {
            ApplyHudCommandResult(TacticalCommandResult.Rejected(TacticalCommandReasonCode.NoSelection));
            ClearHudCommandMode();
        }
    }

    private static TacticalCommandResult ToTacticalCommandResult(RtsSelectionCommandResultElement result)
    {
        return result.Accepted != 0
            ? TacticalCommandResult.Success()
            : TacticalCommandResult.Rejected((TacticalCommandReasonCode)result.ReasonCode);
    }

    private bool ProcessAttackCommandRequests()
    {
        if (TryGetDefaultEntityManager(out EntityManager defaultEntityManager))
            _selectionHudFeedbackSystem.EnsureFeedbackQueue(defaultEntityManager);

        if (!_rtsSelectionInputSystem.TryGetCommandBuffers(
                out EntityManager em,
                out DynamicBuffer<RtsSelectionCommandIntentRequestElement> commandRequests,
                out DynamicBuffer<RtsSelectionCommandResultElement> commandResults))
        {
            if (_explicitAttackTargetModeActive)
                ApplyHudCommandResult(TacticalCommandResult.Rejected(TacticalCommandReasonCode.NoSelection));
            return false;
        }

        EnsureEntityQueries(em);
        _selectionAttackCommandRequestSystem.ProcessPendingRequests(
            em,
            commandRequests,
            commandResults,
            _attackOrderCommandSystem,
            _unitTargetOrderSystem,
            TryGetClickedUnitEntity,
            _buildingPlacementInteractionSystem,
            _buildingPlacementInteractionContext);

        bool issued = false;
        for (int i = 0; i < commandResults.Length;)
        {
            RtsSelectionCommandResultElement result = commandResults[i];
            if (result.Kind != RtsSelectionCommandIntentKind.Attack)
            {
                i++;
                continue;
            }

            commandResults.RemoveAt(i);
            if (result.HasCommandResult != 0)
            {
                ApplyHudCommandResult(ToTacticalCommandResult(result));
            }

            if (result.Accepted == 0)
                continue;

            if (result.HasWorldPosition != 0)
                _selectionOrderMarkerSystem.ShowAttackOrderMarker(em, result.WorldPosition);
            if (result.EmitScreenMarker != 0)
                RequestAttackOrderScreenMarker(new Vector2(result.ScreenPosition.x, result.ScreenPosition.y));
            ClearCurrentSelection(em, "AttackOrderIssued");
            _focusedUnitLifecycleSystem.ClearFocusedUnit(_selectionStateSystem);
            _cameraDragging = false;
            ClearHudCommandMode();
            if (result.ShowWorldMarkers != 0)
                SetHudWorldMarkersVisible(true);
            issued = true;
        }

        return issued;
    }

    private bool TryIssueBoardTransportOrderToClickedUnit(Vector2 screenPosition)
    {
        if (!_rtsSelectionInputSystem.QueueBoardTransportCommandRequest(screenPosition, Time.frameCount))
            return false;

        return ProcessTransportCommandRequests();
    }

    private bool ProcessTransportCommandRequests()
    {
        if (TryGetDefaultEntityManager(out EntityManager defaultEntityManager))
            _selectionHudFeedbackSystem.EnsureFeedbackQueue(defaultEntityManager);

        if (!_rtsSelectionInputSystem.TryGetCommandBuffers(
                out EntityManager em,
                out DynamicBuffer<RtsSelectionCommandIntentRequestElement> commandRequests,
                out DynamicBuffer<RtsSelectionCommandResultElement> commandResults))
        {
            return false;
        }

        EnsureEntityQueries(em);
        _selectionTransportCommandRequestSystem.ProcessPendingRequests(
            em,
            commandRequests,
            commandResults,
            _transportBoardingCommandSystem,
            _unitTransportBoardingSystem,
            _unitMoveOrderSystem,
            _selectionStateSystem,
            TryGetClickedUnitEntity,
            TryGetClickedCell);

        bool accepted = false;
        for (int i = 0; i < commandResults.Length;)
        {
            RtsSelectionCommandResultElement result = commandResults[i];
            if (result.Kind != RtsSelectionCommandIntentKind.BoardTransport &&
                result.Kind != RtsSelectionCommandIntentKind.DisembarkTransport)
            {
                i++;
                continue;
            }

            commandResults.RemoveAt(i);
            if (result.Accepted == 0)
                continue;

            accepted = true;
            if (result.Kind != RtsSelectionCommandIntentKind.BoardTransport)
                continue;

            if (result.HasTargetCell != 0 && result.HasWorldPosition != 0)
            {
                _selectionOrderMarkerSystem.ShowMoveOrderMarker(
                    em,
                    result.TargetCell,
                    result.WorldPosition,
                    result.MarkerFactionId);
            }
            if (result.EmitScreenMarker != 0)
                RequestMoveOrderScreenMarker(new Vector2(result.ScreenPosition.x, result.ScreenPosition.y));
            ClearCurrentSelection(em, "BoardTransportOrderIssued");
            _focusedUnitLifecycleSystem.ClearFocusedUnit(_selectionStateSystem);
            _cameraDragging = false;
        }

        return accepted;
    }

    private void ProcessExternalSelectionCommandRequests()
    {
        if (!_rtsSelectionInputSystem.TryGetCommandBuffers(
                out _,
                out DynamicBuffer<RtsSelectionCommandIntentRequestElement> commandRequests,
                out DynamicBuffer<RtsSelectionCommandResultElement> _))
        {
            return;
        }

        _externalSelectionCommandScratch.Clear();
        for (int i = 0; i < commandRequests.Length;)
        {
            RtsSelectionCommandIntentRequestElement request = commandRequests[i];
            if (!IsExternalSelectionCommand(request.Kind))
            {
                i++;
                continue;
            }

            commandRequests.RemoveAt(i);
            _externalSelectionCommandScratch.Add(request.Kind);
        }

        for (int i = 0; i < _externalSelectionCommandScratch.Count; i++)
            ProcessExternalSelectionCommand(_externalSelectionCommandScratch[i]);
    }

    private static bool IsExternalSelectionCommand(RtsSelectionCommandIntentKind kind)
    {
        return kind == RtsSelectionCommandIntentKind.SelectAll ||
               kind == RtsSelectionCommandIntentKind.SelectAllSoldiers ||
               kind == RtsSelectionCommandIntentKind.SelectAllVehicles ||
               kind == RtsSelectionCommandIntentKind.DeselectAll ||
               kind == RtsSelectionCommandIntentKind.HoldPosition ||
               kind == RtsSelectionCommandIntentKind.Stop ||
               kind == RtsSelectionCommandIntentKind.DestroyFocusedUnit ||
               kind == RtsSelectionCommandIntentKind.ToggleAttackTargetMode ||
               kind == RtsSelectionCommandIntentKind.CancelAttackTargetMode;
    }

    private void ProcessExternalSelectionCommand(RtsSelectionCommandIntentKind kind)
    {
        switch (kind)
        {
            case RtsSelectionCommandIntentKind.SelectAll:
                SelectAllVisiblePlayerUnits();
                break;
            case RtsSelectionCommandIntentKind.SelectAllSoldiers:
                SelectAllVisiblePlayerSoldiers();
                break;
            case RtsSelectionCommandIntentKind.SelectAllVehicles:
                SelectAllVisiblePlayerVehicles();
                break;
            case RtsSelectionCommandIntentKind.DeselectAll:
                DeselectAllUnits("SelectionUiCommandSystem");
                break;
            case RtsSelectionCommandIntentKind.HoldPosition:
                IssueHoldPositionOrder();
                break;
            case RtsSelectionCommandIntentKind.Stop:
                IssueStopOrder();
                break;
            case RtsSelectionCommandIntentKind.DestroyFocusedUnit:
                DestroyFocusedUnit();
                break;
            case RtsSelectionCommandIntentKind.ToggleAttackTargetMode:
                if (!IssueFocusedMissileLauncherRadarAttack())
                    ArmFocusedAttackTargetMode();
                break;
            case RtsSelectionCommandIntentKind.CancelAttackTargetMode:
                CancelExplicitAttackTargetMode();
                break;
        }
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
            RequestMoveOrderScreenMarker(markerScreenPosition);
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
        if (!TryGetDefaultEntityManager(out EntityManager em))
            return;

        _rtsCameraRequestSystem.QueuePan(em, screenDelta, panSensitivity);
        ProcessCameraRequests(em);
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

            if (!TryGetDefaultEntityManager(out EntityManager em))
                return;

            _rtsCameraRequestSystem.QueueUpdatePerspectiveMode(
                em,
                targetHeight,
                targetPitch,
                targetYaw,
                targetFieldOfView,
                zoomTransitionSmoothTime,
                completeTransitionOnArrive: true);
            ProcessCameraRequests(em);

            return;
        }

        float zoomDirection = 0f;
        if (_runtimeGameplayStateSystem.ZoomInHeld)
            zoomDirection += 1f;
        if (_runtimeGameplayStateSystem.ZoomOutHeld)
            zoomDirection -= 1f;

        if (Mathf.Approximately(zoomDirection, 0f))
            return;

        if (!TryGetDefaultEntityManager(out EntityManager defaultEntityManager))
            return;

        _rtsCameraRequestSystem.QueuePerspectiveZoom(defaultEntityManager, zoomDirection, zoomSpeed, Time.deltaTime, minZoomHeight, maxZoomHeight);
        ProcessCameraRequests(defaultEntityManager);
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

        if (Mathf.Approximately(zoomDirection, 0f))
            return;

        if (!TryGetDefaultEntityManager(out EntityManager em))
            return;

        _rtsCameraRequestSystem.QueueFullscreenIsoZoom(em, zoomDirection, zoomSpeed, Time.deltaTime, minZoomHeight, maxZoomHeight);
        ProcessCameraRequests(em);
    }

    private void UpdateFullscreenIsoCameraMode()
    {
        if (!TryGetDefaultEntityManager(out EntityManager em))
            return;

        _rtsCameraRequestSystem.QueueUpdateFullscreenIsoMode(
            em,
            _fullscreenIsoTargetHeight,
            _fullscreenIsoTargetOrthographicSize,
            fullscreenIsoPitch,
            fullscreenIsoYaw,
            zoomTransitionSmoothTime);
        ProcessCameraRequests(em);
    }

    private void UpdateBuildModeCameraTransition()
    {
        if (worldCamera == null)
            return;

        SyncCameraZoomModeState();

        if (!TryGetDefaultEntityManager(out EntityManager em))
            return;

        _rtsCameraRequestSystem.QueueUpdatePerspectiveMode(
            em,
            buildModeZoomHeight,
            buildModePitch,
            buildModeYaw,
            buildModeFieldOfView,
            zoomTransitionSmoothTime,
            completeTransitionOnArrive: false);
        ProcessCameraRequests(em);
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
            if (TryGetDefaultEntityManager(out EntityManager em))
            {
                _rtsCameraRequestSystem.QueueApplyPerspectiveModeInstant(em, normalModeZoomHeight, normalModePitch, normalModeYaw, normalModeFieldOfView);
                if (worldCamera != null)
                    _rtsCameraRequestSystem.QueueMoveGroundCenterTo(em, focusWorldPosition);
                _rtsCameraRequestSystem.QueueResetTransitionVelocities(em);
                ProcessCameraRequests(em);
            }

            _wasPlayRequested = true;
            _wasBuildModeActive = _runtimeGameplayStateSystem.BuildModeActive;
            _isZoomTransitionActive = _runtimeGameplayStateSystem.BuildModeActive;
            return;
        }

        _wasPlayRequested = _runtimeGameplayStateSystem.PlayRequested;

        if (_wasBuildModeActive != _runtimeGameplayStateSystem.BuildModeActive)
        {
            if (!TryGetDefaultEntityManager(out EntityManager em))
                return;

            _rtsCameraRequestSystem.QueueBeginZoomTransition(em, _runtimeGameplayStateSystem.BuildModeActive);
            ProcessCameraRequests(em);
        }
    }

    private void ConsumeInitialCameraFocusRequest()
    {
        if (!_runtimeGameplayStateSystem.InitialCameraFocusRequested || worldCamera == null)
            return;

        if (!TryGetDefaultEntityManager(out EntityManager em))
            return;

        _rtsCameraRequestSystem.QueueMoveGroundCenterTo(em, _runtimeGameplayStateSystem.InitialCameraFocusWorld);
        _rtsCameraRequestSystem.QueueClearSmoothFocusTarget(em);
        ProcessCameraRequests(em);
        _runtimeGameplayStateSystem.InitialCameraFocusRequested = false;
    }

    private void UpdateSmoothCameraFocus()
    {
        if (!_rtsCameraSystem.HasSmoothFocusTarget || worldCamera == null)
            return;

        if (!TryGetDefaultEntityManager(out EntityManager em))
            return;

        _rtsCameraRequestSystem.QueueUpdateSmoothFocus(em, zoomTransitionSmoothTime);
        ProcessCameraRequests(em);
    }

    public void EnterFullscreenMapIsoMode(Vector3 focusWorldPosition)
    {
        if (worldCamera == null)
            return;

        _fullscreenIsoTargetHeight = Mathf.Clamp(fullscreenIsoZoomHeight, minZoomHeight, maxZoomHeight);
        _fullscreenIsoTargetOrthographicSize = Mathf.Clamp(fullscreenIsoOrthographicSize, 8f, 48f);
        if (TryGetDefaultEntityManager(out EntityManager em))
        {
            _rtsCameraRequestSystem.QueueMoveGroundCenterTo(em, focusWorldPosition);
            _rtsCameraRequestSystem.QueueApplyFullscreenIsoModeInstant(em, _fullscreenIsoTargetHeight, _fullscreenIsoTargetOrthographicSize, fullscreenIsoPitch, fullscreenIsoYaw);
            ProcessCameraRequests(em);
        }

        _runtimeGameplayStateSystem.FullscreenMapIsoMode = true;
        _runtimeGameplayStateSystem.FullscreenMapOpen = true;
        _cameraDragging = false;
    }

    public void ExitFullscreenMapIsoMode()
    {
        if (worldCamera != null)
        {
            if (TryGetDefaultEntityManager(out EntityManager em))
            {
                _rtsCameraRequestSystem.QueueApplyPerspectiveModeInstant(em, normalModeZoomHeight, normalModePitch, normalModeYaw, normalModeFieldOfView);
                ProcessCameraRequests(em);
            }
        }

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
        if (TryGetDefaultEntityManager(out EntityManager em))
        {
            _rtsCameraRequestSystem.QueueApplyFullscreenIsoModeInstant(em, _fullscreenIsoTargetHeight, _fullscreenIsoTargetOrthographicSize, fullscreenIsoPitch, fullscreenIsoYaw);
            _rtsCameraRequestSystem.QueueMoveGroundCenterTo(em, focusWorldPosition);
            _rtsCameraRequestSystem.QueueSetNormalIsoModeActive(em, true);
            ProcessCameraRequests(em);
        }

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
            if (TryGetDefaultEntityManager(out EntityManager em))
            {
                _rtsCameraRequestSystem.QueueApplyPerspectiveModeInstant(em, targetHeight, normalModePitch, normalModeYaw, normalModeFieldOfView);
                _rtsCameraRequestSystem.QueueMoveGroundCenterTo(em, focusWorldPosition);
                ProcessCameraRequests(em);
            }
        }

        SetCameraNormalIsoModeActive(false);
        _cameraDragging = false;
    }

    public void MoveCameraGroundCenterTo(Vector3 focusWorldPosition)
    {
        if (!TryGetDefaultEntityManager(out EntityManager em))
            return;

        _rtsCameraRequestSystem.QueueMoveGroundCenterTo(em, focusWorldPosition);
        ProcessCameraRequests(em);
    }

    public void SmoothMoveCameraGroundCenterTo(Vector3 focusWorldPosition)
    {
        if (worldCamera == null)
            return;

        if (!TryGetDefaultEntityManager(out EntityManager em))
            return;

        _rtsCameraRequestSystem.QueueSetSmoothFocusTarget(em, focusWorldPosition, resetVelocity: true);
        _rtsCameraRequestSystem.QueueClearDragging(em);
        ProcessCameraRequests(em);
    }

    public void FollowCameraGroundCenterTo(Vector3 focusWorldPosition)
    {
        if (worldCamera == null)
            return;

        if (!TryGetDefaultEntityManager(out EntityManager em))
            return;

        _rtsCameraRequestSystem.QueueSetSmoothFocusTarget(em, focusWorldPosition, resetVelocity: false);
        _rtsCameraRequestSystem.QueueClearDragging(em);
        ProcessCameraRequests(em);
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

        QueueSelectionRectangleRequest(
            new Rect(0f, 0f, Screen.width, Screen.height),
            RtsSelectionPointerRequestKind.SelectionRectCommitted,
            filter);
        ProcessSelectionRectangleRequests();
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
        if (!_rtsSelectionInputSystem.QueueAttackCommandRequest(
                screenPosition,
                _explicitAttackTargetModeActive,
                Time.frameCount))
        {
            if (_explicitAttackTargetModeActive)
                ApplyHudCommandResult(TacticalCommandResult.Rejected(TacticalCommandReasonCode.NoSelection));
            return false;
        }

        return ProcessAttackCommandRequests();
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
