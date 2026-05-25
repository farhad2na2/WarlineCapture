using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.InputSystem;
using Unity.Transforms;
using UnityEngine;

public sealed class SelectionRuntimeContextSystem
{
    private readonly SelectionRuntimeDiagnosticsSystem _selectionRuntimeDiagnosticsSystem = new();
    private readonly SelectionRuntimeConfigSystem _selectionRuntimeConfigSystem = new();
    private readonly SelectionRuntimeQuerySystem _selectionRuntimeQuerySystem = new();
    private SelectionRuntimeConfigSystem.State _runtimeConfig;
    private readonly RuntimeGameplayStateSystem _runtimeGameplayStateSystem = new();
    private readonly RtsSelectionInputSystem _rtsSelectionInputSystem = new();
    private readonly RtsSelectionRuntimeInputSystem _rtsSelectionRuntimeInputSystem = new();
    private readonly RtsSelectionRuntimeInputContextSystem _rtsSelectionRuntimeInputContextSystem = new();
    private readonly RtsSelectionRuntimeCameraSystem _rtsSelectionRuntimeCameraSystem = new();
    private readonly RtsSelectionRuntimeCameraContextSystem _rtsSelectionRuntimeCameraContextSystem = new();
    private readonly RtsSelectionCommandResultFlushSystem _rtsSelectionCommandResultFlushSystem = new();
    private readonly RtsSelectionCommandResultContextSystem _rtsSelectionCommandResultContextSystem = new();
    private readonly RtsSelectionFocusCommandSystem _rtsSelectionFocusCommandSystem = new();
    private readonly RtsSelectionFocusCommandContextSystem _rtsSelectionFocusCommandContextSystem = new();
    private readonly RtsSelectionPointerTargetCommandSystem _rtsSelectionPointerTargetCommandSystem = new();
    private readonly RtsSelectionPointerTargetCommandContextSystem _rtsSelectionPointerTargetCommandContextSystem = new();
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
    private MainMenuPlayUI _mainMenuPlayUi;
    private RoadBuildSystem _roadBuildController;
    private BuildingPlacementInteractionSystem _buildingPlacementInteractionSystem;
    private BuildingPlacementInteractionSystem.Context _buildingPlacementInteractionContext;
    private readonly List<Entity> _visibleSelectionScratch = new();
    private Transform _runtimeRoot;
    private bool _explicitAttackTargetModeActive;

    public SelectionRuntimeContextSystem()
    {
        _runtimeConfig = _selectionRuntimeConfigSystem.CreateState(null, null);
    }

    public bool ExplicitAttackTargetModeActive => _explicitAttackTargetModeActive;

    public void DisembarkFocusedTransport()
    {
        if (!TryGetFocusedUnitEntity(out _, out Entity transport))
            return;
        if (!_rtsSelectionInputSystem.QueueDisembarkTransportCommandRequest(transport, Time.frameCount))
            return;

        ProcessTransportCommandRequests();
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

    private bool FocusedUnitOwnedByPlayer
    {
        get
        {
            return TryGetFocusedUnitEntity(out EntityManager em, out Entity entity) &&
                   _selectionUiQuerySystem.IsOwnedByPlayer(em, entity);
        }
    }

    private bool FocusedUnitCanAttack
    {
        get
        {
            return TryGetFocusedUnitEntity(out EntityManager em, out Entity entity) &&
                   _selectionUiQuerySystem.CanAttack(em, entity);
        }
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
        _runtimeConfig = _selectionRuntimeConfigSystem.CreateState(configAsset, sceneWorldCamera);
        _runtimeRoot = runtimeRoot;
        _mainMenuPlayUi = mainMenuPlayUi;
        _roadBuildController = roadBuildController;
        _buildingPlacementInteractionSystem = buildingPlacementInteractionSystem;
        _buildingPlacementInteractionContext = buildingPlacementInteractionContext;
        _selectionHudFeedbackSystem.ResetBridgeCache();

        _selectionOrderMarkerSystem.Initialize(
            _runtimeConfig.MoveOrderMarkerPrefab,
            _runtimeConfig.AttackOrderMarkerPrefab,
            _runtimeConfig.OrderMarkerVisibleSeconds,
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

    private SelectionHudFeedbackSystem.Context CreateHudFeedbackContext()
    {
        return new SelectionHudFeedbackSystem.Context(_selectionUiQuerySystem, TryGetDefaultEntityManager);
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

    public void Dispose()
    {
        _selectionOrderMarkerSystem.Dispose();
    }

    private void EnsureRuntimeSelectionDependencies(EntityManager em)
    {
        _selectionRuntimeQuerySystem.EnsureEntityQueries(em);
        _focusableUnitLookupSystem.EnsureEntityQueries(em);
        _visibleUnitSelectionSystem.EnsureEntityQueries(em);
        _attackOrderCommandSystem.EnsureEntityQueries(em);
        _selectionOrderMarkerSystem.EnsureEntityQueries(em);
        _focusedUnitCommandSystem.EnsureEntityQueries(em);
        _focusedUnitLifecycleSystem.EnsureEntityQueries(em);
        _selectedUnitOrderSnapshotSystem.EnsureEntityQueries(em);
        _buildingTargetMoveOrderSystem.EnsureEntityQueries(em);
        _transportBoardingCommandSystem.EnsureEntityQueries(em);
    }

    public void ProcessQueuedTransportCommands()
    {
        ProcessTransportCommandRequests();
    }

    public void ProcessExternalSelectionCommands()
    {
        _rtsSelectionFocusCommandSystem.ProcessExternalSelectionCommandRequests(FocusCommandContext);
    }

    public void ProcessQueuedMoveOrder()
    {
        _rtsSelectionRuntimeInputSystem.ProcessQueuedMoveOrder(RuntimeInputContext);
    }

    public void RefreshFocusedSelectionReadModels()
    {
        RefreshFocusedUnit();
        PublishFocusedUnitUiReadModel();
    }

    public void UpdateOrderMarkerVisibility()
    {
        _rtsSelectionCommandResultFlushSystem.UpdateOrderMarkerVisibility(CommandResultFlushContext);
    }

    public bool UpdateRuntimeCameraTick()
    {
        return _rtsSelectionRuntimeCameraSystem.UpdateRuntimeCameraTick(RuntimeCameraContext);
    }

    public void UpdateNormalPointerInput()
    {
        _rtsSelectionRuntimeInputSystem.UpdateNormalPointerInput(RuntimeInputContext);
    }

    private RtsSelectionRuntimeInputSystem.Context RuntimeInputContext =>
        _rtsSelectionRuntimeInputContextSystem.Create(
            _runtimeGameplayStateSystem,
            _rtsSelectionInputSystem,
            _mainMenuPlayUi,
            _runtimeConfig,
            () => _explicitAttackTargetModeActive,
            value => _explicitAttackTargetModeActive = value,
            () => _rtsCameraSystem.IsDragging,
            value => _rtsSelectionRuntimeCameraSystem.SetCameraDragging(RuntimeCameraContext, value),
            pointerPosition => IsPointerOverUI(pointerPosition, out _),
            pointerPosition => IsPointerOverGameplayUi(pointerPosition, out _),
            TryIssueAttackOrderToClickedUnit,
            TryIssueBoardTransportOrderToClickedUnit,
            TryFocusUnit,
            screenDelta => _rtsSelectionRuntimeCameraSystem.PanCamera(RuntimeCameraContext, screenDelta),
            IssueMoveOrder,
            ProcessSelectionRectangleRequests);

    private RtsSelectionRuntimeCameraSystem.Context RuntimeCameraContext =>
        _rtsSelectionRuntimeCameraContextSystem.Create(
            _runtimeGameplayStateSystem,
            _rtsSelectionInputSystem,
            _rtsCameraSystem,
            _rtsCameraRequestSystem,
            _runtimeConfig,
            _mainMenuPlayUi,
            _roadBuildController,
            _buildingPlacementInteractionSystem,
            _buildingPlacementInteractionContext,
            TryGetDefaultEntityManager,
            IsPointerOverGameplayUi,
            UpdateLastKnownPointerPosition,
            HideOrderScreenMarkers);

    private RtsSelectionCommandResultFlushSystem.Context CommandResultFlushContext =>
        _rtsSelectionCommandResultContextSystem.Create(
            _rtsSelectionInputSystem,
            _selectionHudFeedbackSystem,
            CreateHudFeedbackContext(),
            _selectionOrderMarkerSystem,
            _selectionMoveCommandRequestSystem,
            _selectionAttackCommandRequestSystem,
            _selectionTransportCommandRequestSystem,
            _selectedMoveOrderCommandSystem,
            _attackOrderCommandSystem,
            _transportBoardingCommandSystem,
            _unitMoveOrderSystem,
            _unitTargetOrderSystem,
            _unitTransportBoardingSystem,
            _selectionStateSystem,
            _buildingPlacementInteractionSystem,
            _buildingPlacementInteractionContext,
            _selectionRuntimeQuerySystem,
            TryGetDefaultEntityManager,
            EnsureRuntimeSelectionDependencies,
            ClearCurrentSelection,
            RequestMoveOrderScreenMarker,
            RequestAttackOrderScreenMarker,
            SetCameraDragging,
            _focusedUnitLifecycleSystem.ClearFocusedUnit,
            TryGetClickedUnitEntity,
            TryGetClickedCell,
            TryGetClickedUnitEntity,
            TryGetClickedUnitEntity,
            TryGetClickedCell);

    private RtsSelectionFocusCommandSystem.Context FocusCommandContext =>
        _rtsSelectionFocusCommandContextSystem.Create(
            _runtimeGameplayStateSystem,
            _rtsSelectionInputSystem,
            _selectionStateSystem,
            _focusedUnitLifecycleSystem,
            _unitTargetOrderSystem,
            _buildingPlacementInteractionSystem,
            _buildingPlacementInteractionContext,
            _runtimeConfig.WorldCamera,
            TryGetDefaultEntityManager,
            EnsureRuntimeSelectionDependencies,
            ClearCurrentSelection,
            QueueSelectionRectangleRequest,
            ProcessSelectionRectangleRequests,
            _selectionHudFeedbackSystem,
            CreateHudFeedbackContext(),
            SetCameraDragging,
            value => _explicitAttackTargetModeActive = value,
            _selectionRuntimeDiagnosticsSystem.EnqueueSelectionDiagnostic,
            DescribeTransportBoardingEntity,
            ValidateControllableEntity,
            () => IssueHoldPositionOrder(),
            () => IssueStopOrder(),
            DestroyFocusedUnit,
            IssueFocusedMissileLauncherRadarAttack,
            ArmFocusedAttackTargetMode,
            CancelExplicitAttackTargetMode);

    private RtsSelectionPointerTargetCommandSystem.Context PointerTargetCommandContext =>
        _rtsSelectionPointerTargetCommandContextSystem.Create(
            _runtimeGameplayStateSystem,
            _rtsSelectionInputSystem,
            _selectionStateSystem,
            _focusedUnitLifecycleSystem,
            _unitTargetOrderSystem,
            _focusableUnitLookupSystem,
            _transportBoardingCommandSystem,
            _unitTransportBoardingSystem,
            _buildingTargetMoveOrderSystem,
            _buildingPlacementInteractionSystem,
            _buildingPlacementInteractionContext,
            _runtimeConfig.WorldCamera,
            TryGetDefaultEntityManager,
            TryGetPointerPosition,
            () => _explicitAttackTargetModeActive,
            value => _explicitAttackTargetModeActive = value,
            _selectionHudFeedbackSystem,
            CreateHudFeedbackContext(),
            ClearCurrentSelection,
            RequestMoveOrderScreenMarker,
            SetCameraDragging,
            ProcessAttackCommandRequests,
            ProcessTransportCommandRequests,
            ProcessMoveCommandRequests,
            _selectionRuntimeDiagnosticsSystem.EnqueueSelectionDiagnostic,
            DescribeTransportBoardingEntity);

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
        if (_runtimeConfig.WorldCamera == null || World.DefaultGameObjectInjectionWorld == null)
            return false;

        var em = World.DefaultGameObjectInjectionWorld.EntityManager;
        EnsureRuntimeSelectionDependencies(em);
        Rect screenRect = new(0f, 0f, Screen.width, Screen.height);
        return _visibleUnitSelectionSystem.HasVisiblePlayerUnits(
            em,
            _runtimeConfig.WorldCamera,
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

        EnsureRuntimeSelectionDependencies(em);
        _selectionRectangleRequestSystem.ProcessPendingRequests(
            em,
            pointerRequests,
            _runtimeConfig.WorldCamera,
            _selectionUiQuerySystem,
            _visibleUnitSelectionSystem,
            _selectionStateSystem,
            _focusedUnitLifecycleSystem,
            _visibleSelectionScratch,
            ClearCurrentSelection,
            CacheSelectedMoveEntities,
            (entityManager, entity) => _selectionHudFeedbackSystem.ApplySelection(CreateHudFeedbackContext(), entityManager, entity),
            selectedCount => _selectionHudFeedbackSystem.ApplySquadSelection(CreateHudFeedbackContext(), selectedCount),
            _selectionRuntimeDiagnosticsSystem.EnqueueSelectionDiagnostic,
            ClearSelectedBuildingAfterRectangleSelection);
    }

    private void ClearSelectedBuildingAfterRectangleSelection()
    {
        _buildingPlacementInteractionSystem?.ClearSelectedBuilding(_buildingPlacementInteractionContext, "RTSSelection.SelectUnitsInRectangle");
    }

    private void IssueMoveOrder(Vector2 screenPosition)
    {
        _rtsSelectionPointerTargetCommandSystem.IssueMoveOrder(PointerTargetCommandContext, screenPosition);
    }

    private void ProcessMoveCommandRequests()
    {
        _rtsSelectionCommandResultFlushSystem.ProcessMoveCommandRequests(CommandResultFlushContext);
    }

    private bool ProcessAttackCommandRequests()
    {
        return _rtsSelectionCommandResultFlushSystem.ProcessAttackCommandRequests(
            CommandResultFlushContext,
            _explicitAttackTargetModeActive);
    }

    private bool TryIssueBoardTransportOrderToClickedUnit(Vector2 screenPosition)
    {
        return _rtsSelectionPointerTargetCommandSystem.TryIssueBoardTransportOrderToClickedUnit(PointerTargetCommandContext, screenPosition);
    }

    private bool ProcessTransportCommandRequests()
    {
        return _rtsSelectionCommandResultFlushSystem.ProcessTransportCommandRequests(CommandResultFlushContext);
    }

    public bool IsBoardablePlayerTransportClick(Vector2 screenPosition)
    {
        return _rtsSelectionPointerTargetCommandSystem.IsBoardablePlayerTransportClick(PointerTargetCommandContext, screenPosition);
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
        return _rtsSelectionPointerTargetCommandSystem.TryIssueMoveOrderToBuilding(
            PointerTargetCommandContext,
            originCell,
            footprintCells);
    }

    private bool TryGetClickedCell(Vector2 screenPosition, EntityManager em, out int2 cell, out Vector3 worldPoint)
    {
        return _rtsSelectionPointerTargetCommandSystem.TryGetClickedCell(
            PointerTargetCommandContext,
            screenPosition,
            em,
            out cell,
            out worldPoint);
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

    private void SetCameraDragging(bool isDragging)
    {
        _rtsSelectionRuntimeCameraSystem.SetCameraDragging(RuntimeCameraContext, isDragging);
    }

    public void EnterFullscreenMapIsoMode(Vector3 focusWorldPosition)
    {
        _rtsSelectionRuntimeCameraSystem.EnterFullscreenMapIsoMode(RuntimeCameraContext, focusWorldPosition);
    }

    public void ExitFullscreenMapIsoMode()
    {
        _rtsSelectionRuntimeCameraSystem.ExitFullscreenMapIsoMode(RuntimeCameraContext);
    }

    public bool IsNormalIsoModeActive => _rtsCameraSystem.NormalIsoModeActive;

    public void ToggleNormalIsoMode()
    {
        _rtsSelectionRuntimeCameraSystem.ToggleNormalIsoMode(RuntimeCameraContext);
    }

    public void EnterNormalIsoMode()
    {
        _rtsSelectionRuntimeCameraSystem.EnterNormalIsoMode(RuntimeCameraContext);
    }

    public void ExitNormalIsoMode()
    {
        _rtsSelectionRuntimeCameraSystem.ExitNormalIsoMode(RuntimeCameraContext);
    }

    public void MoveCameraGroundCenterTo(Vector3 focusWorldPosition)
    {
        _rtsSelectionRuntimeCameraSystem.MoveCameraGroundCenterTo(RuntimeCameraContext, focusWorldPosition);
    }

    public void SmoothMoveCameraGroundCenterTo(Vector3 focusWorldPosition)
    {
        _rtsSelectionRuntimeCameraSystem.SmoothMoveCameraGroundCenterTo(RuntimeCameraContext, focusWorldPosition);
    }

    public void FollowCameraGroundCenterTo(Vector3 focusWorldPosition)
    {
        _rtsSelectionRuntimeCameraSystem.FollowCameraGroundCenterTo(RuntimeCameraContext, focusWorldPosition);
    }

    private void ClearCurrentSelection(EntityManager em, string reason = "Unspecified")
    {
        _focusedUnitLifecycleSystem.ClearCurrentSelection(
            em,
            _selectionStateSystem,
            reason,
            _selectionRuntimeDiagnosticsSystem.EnqueueSelectionDiagnostic,
            () => _selectionHudFeedbackSystem.ClearSelection(CreateHudFeedbackContext()));
    }

    public void ClearFocusedUnit()
    {
        _rtsSelectionFocusCommandSystem.ClearFocusedUnit(FocusCommandContext);
    }

    public void DeselectAllUnits(string reason = "DeselectAllUnits")
    {
        _rtsSelectionFocusCommandSystem.DeselectAllUnits(FocusCommandContext, reason);
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
        _rtsSelectionFocusCommandSystem.SelectAllVisiblePlayerUnits(FocusCommandContext, filter);
    }

    public bool FocusUnitEntity(Entity entity)
    {
        return _rtsSelectionFocusCommandSystem.FocusUnitEntity(FocusCommandContext, entity);
    }

    public TacticalCommandResult TrySelectRuntimeEntity(Entity entity)
    {
        return _rtsSelectionFocusCommandSystem.TrySelectRuntimeEntity(FocusCommandContext, entity);
    }

    public TacticalCommandResult TryIssueMoveToCell(int2 goal)
    {
        _selectionHudFeedbackSystem.ApplyCommandMode(CreateHudFeedbackContext(), TacticalCommandMode.Move);

        if (World.DefaultGameObjectInjectionWorld == null)
            return ApplyAndReturn(TacticalCommandResult.Rejected(TacticalCommandReasonCode.NoSelection));

        EntityManager em = World.DefaultGameObjectInjectionWorld.EntityManager;
        EnsureRuntimeSelectionDependencies(em);
        using var selectedEntities = _selectionRuntimeQuerySystem.SelectedMoveQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
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
            _selectionHudFeedbackSystem.SetWorldMarkersVisible(CreateHudFeedbackContext(), true);
            _selectionHudFeedbackSystem.ClearCommandMode(CreateHudFeedbackContext());
        }

        return ApplyAndReturn(result);
    }

    public TacticalCommandResult TryIssueAttackTarget(Entity targetEntity)
    {
        _selectionHudFeedbackSystem.ApplyCommandMode(CreateHudFeedbackContext(), TacticalCommandMode.Attack);

        if (World.DefaultGameObjectInjectionWorld == null)
            return ApplyAndReturn(TacticalCommandResult.Rejected(TacticalCommandReasonCode.NoSelection));

        EntityManager em = World.DefaultGameObjectInjectionWorld.EntityManager;
        EnsureRuntimeSelectionDependencies(em);
        AttackOrderCommandSystem.Result issueResult =
            _attackOrderCommandSystem.IssueAttackTarget(em, targetEntity, _unitTargetOrderSystem);
        TacticalCommandResult result = issueResult.CommandResult;
        if (result.Accepted)
        {
            _explicitAttackTargetModeActive = false;
            SetCameraDragging(false);
            _selectionHudFeedbackSystem.SetWorldMarkersVisible(CreateHudFeedbackContext(), true);
            _selectionHudFeedbackSystem.ClearCommandMode(CreateHudFeedbackContext());
        }

        return ApplyAndReturn(result);
    }

    private TacticalCommandResult ApplyAndReturn(TacticalCommandResult result)
    {
        _selectionHudFeedbackSystem.ApplyCommandResult(CreateHudFeedbackContext(), result);
        if (!result.Accepted)
            _selectionHudFeedbackSystem.ClearCommandMode(CreateHudFeedbackContext());
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
        _selectionRuntimeDiagnosticsSystem.EnqueueSelectionDiagnostic("result=CaptureUiClickSequence");
        _rtsSelectionInputSystem.CaptureUiClickSequence();
        SetCameraDragging(false);
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
        SetCameraDragging(false);
        _selectionHudFeedbackSystem.ApplyCommandResult(CreateHudFeedbackContext(), TacticalCommandResult.Success());
        _selectionHudFeedbackSystem.ClearCommandMode(CreateHudFeedbackContext());
        _selectionHudFeedbackSystem.SetWorldMarkersVisible(CreateHudFeedbackContext(), true);
        _selectionHudFeedbackSystem.ApplySelection(CreateHudFeedbackContext(), em, launcher);
        return true;
    }

    public bool ArmFocusedAttackTargetMode()
    {
        bool hasFocusedUnit = TryGetFocusedUnitEntity(out _, out _);
        if (!hasFocusedUnit || !FocusedUnitOwnedByPlayer || !FocusedUnitCanAttack)
        {
            _selectionHudFeedbackSystem.ApplyCommandResult(CreateHudFeedbackContext(), TacticalCommandResult.Rejected(
                hasFocusedUnit ? TacticalCommandReasonCode.TargetNotAttackable : TacticalCommandReasonCode.NoSelection));
            return false;
        }

        _explicitAttackTargetModeActive = true;
        _selectionHudFeedbackSystem.ApplyCommandMode(CreateHudFeedbackContext(), TacticalCommandMode.Attack);
        _selectionHudFeedbackSystem.SetWorldMarkersVisible(CreateHudFeedbackContext(), true);
        _runtimeGameplayStateSystem.SelectionModeActive = false;
        _runtimeGameplayStateSystem.SuppressNextWorldClick = true;
        _rtsSelectionInputSystem.IsDraggingSelection = false;
        SetCameraDragging(false);
        _rtsSelectionInputSystem.SkipNextWorldReleaseAfterSelection = true;
        return true;
    }

    public void CancelExplicitAttackTargetMode()
    {
        _explicitAttackTargetModeActive = false;
        _selectionHudFeedbackSystem.ClearCommandMode(CreateHudFeedbackContext());
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
        _selectionHudFeedbackSystem.ApplyCommandMode(CreateHudFeedbackContext(), mode);

        if (World.DefaultGameObjectInjectionWorld == null)
        {
            _selectionHudFeedbackSystem.ApplyCommandResult(CreateHudFeedbackContext(), TacticalCommandResult.Rejected(TacticalCommandReasonCode.NoSelection));
            return false;
        }

        var em = World.DefaultGameObjectInjectionWorld.EntityManager;
        bool issued = _focusedUnitCommandSystem.IssueImmediateSelectedUnitOrder(
            em,
            clearEngageTarget,
            _unitMoveOrderSystem);
        if (!issued)
        {
            _selectionHudFeedbackSystem.ApplyCommandResult(CreateHudFeedbackContext(), TacticalCommandResult.Rejected(TacticalCommandReasonCode.NoSelection));
            return false;
        }

        _explicitAttackTargetModeActive = false;
        SetCameraDragging(false);
        _selectionHudFeedbackSystem.SetWorldMarkersVisible(CreateHudFeedbackContext(), false);
        _selectionHudFeedbackSystem.ApplyCommandResult(CreateHudFeedbackContext(), TacticalCommandResult.Success());
        return true;
    }

    private void RefreshFocusedUnit()
    {
        if (World.DefaultGameObjectInjectionWorld == null)
            return;

        var em = World.DefaultGameObjectInjectionWorld.EntityManager;
        EnsureRuntimeSelectionDependencies(em);
        _focusedUnitLifecycleSystem.RefreshFocusedUnit(
            em,
            _selectionStateSystem,
            (entityManager, entity) => _selectionHudFeedbackSystem.ApplySelection(CreateHudFeedbackContext(), entityManager, entity));
    }

    private bool TryFocusUnit(Vector2 screenPosition)
    {
        return _rtsSelectionPointerTargetCommandSystem.TryFocusUnit(PointerTargetCommandContext, screenPosition);
    }

    private bool TryIssueAttackOrderToClickedUnit(Vector2 screenPosition)
    {
        return _rtsSelectionPointerTargetCommandSystem.TryIssueAttackOrderToClickedUnit(PointerTargetCommandContext, screenPosition);
    }

    private bool TryGetClickedUnitEntity(Vector2 screenPosition, EntityManager em, out Entity bestEntity)
    {
        return _rtsSelectionPointerTargetCommandSystem.TryGetClickedUnitEntity(
            PointerTargetCommandContext,
            screenPosition,
            em,
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
