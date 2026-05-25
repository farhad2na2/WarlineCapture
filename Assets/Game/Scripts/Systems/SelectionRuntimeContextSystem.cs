using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.InputSystem;
using Unity.Transforms;
using UnityEngine;

public sealed class SelectionRuntimeContextSystem
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

    private readonly SelectionRuntimeDiagnosticsSystem _selectionRuntimeDiagnosticsSystem = new();
    private readonly SelectionRuntimeConfigSystem _selectionRuntimeConfigSystem = new();
    private SelectionRuntimeConfigSystem.State _runtimeConfig;
    private readonly RuntimeGameplayStateSystem _runtimeGameplayStateSystem = new();
    private readonly RtsSelectionInputSystem _rtsSelectionInputSystem = new();
    private readonly RtsSelectionRuntimeInputSystem _rtsSelectionRuntimeInputSystem = new();
    private readonly RtsSelectionRuntimeCameraSystem _rtsSelectionRuntimeCameraSystem = new();
    private readonly RtsSelectionCommandResultFlushSystem _rtsSelectionCommandResultFlushSystem = new();
    private readonly RtsSelectionFocusCommandSystem _rtsSelectionFocusCommandSystem = new();
    private readonly RtsSelectionPointerTargetCommandSystem _rtsSelectionPointerTargetCommandSystem = new();
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
    private World _queryWorld;
    private EntityQuery _selectedMoveQuery;
    private EntityQuery _gridPathingQuery;
    private EntityQuery _gridConfigQuery;
    private EntityQuery _selectedTagQuery;
    private readonly List<Entity> _visibleSelectionScratch = new();
    private Transform _runtimeRoot;
    private bool _explicitAttackTargetModeActive;

    public SelectionRuntimeContextSystem()
    {
        _runtimeConfig = _selectionRuntimeConfigSystem.CreateState(null, null);
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

    public void ProcessQueuedTransportCommands()
    {
        ProcessTransportCommandRequests();
    }

    public void ProcessExternalSelectionCommands()
    {
        _rtsSelectionFocusCommandSystem.ProcessExternalSelectionCommandRequests(CreateFocusCommandContext());
    }

    public void ProcessQueuedMoveOrder()
    {
        _rtsSelectionRuntimeInputSystem.ProcessQueuedMoveOrder(CreateRuntimeInputContext());
    }

    public void RefreshFocusedSelectionReadModels()
    {
        RefreshFocusedUnit();
        PublishFocusedUnitUiReadModel();
    }

    public void UpdateOrderMarkerVisibility()
    {
        _rtsSelectionCommandResultFlushSystem.UpdateOrderMarkerVisibility(CreateCommandResultFlushContext());
    }

    public bool UpdateRuntimeCameraTick()
    {
        return _rtsSelectionRuntimeCameraSystem.UpdateRuntimeCameraTick(CreateRuntimeCameraContext());
    }

    public void UpdateNormalPointerInput()
    {
        _rtsSelectionRuntimeInputSystem.UpdateNormalPointerInput(CreateRuntimeInputContext());
    }

    private RtsSelectionRuntimeInputSystem.Context CreateRuntimeInputContext()
    {
        return new RtsSelectionRuntimeInputSystem.Context(
            _runtimeGameplayStateSystem,
            _rtsSelectionInputSystem,
            _mainMenuPlayUi,
            _runtimeConfig.DragThresholdPixels,
            _runtimeConfig.SelectionModeHoldSeconds,
            () => _explicitAttackTargetModeActive,
            value => _explicitAttackTargetModeActive = value,
            () => _rtsCameraSystem.IsDragging,
            value => _rtsSelectionRuntimeCameraSystem.SetCameraDragging(CreateRuntimeCameraContext(), value),
            pointerPosition => IsPointerOverUI(pointerPosition, out _),
            pointerPosition => IsPointerOverGameplayUi(pointerPosition, out _),
            TryIssueAttackOrderToClickedUnit,
            TryIssueBoardTransportOrderToClickedUnit,
            TryFocusUnit,
            screenDelta => _rtsSelectionRuntimeCameraSystem.PanCamera(CreateRuntimeCameraContext(), screenDelta),
            IssueMoveOrder,
            ProcessSelectionRectangleRequests);
    }

    private RtsSelectionRuntimeCameraSystem.Context CreateRuntimeCameraContext()
    {
        return new RtsSelectionRuntimeCameraSystem.Context(
            _runtimeGameplayStateSystem,
            _rtsSelectionInputSystem,
            _rtsCameraSystem,
            _rtsCameraRequestSystem,
            _runtimeConfig.WorldCamera,
            _mainMenuPlayUi,
            _roadBuildController,
            _buildingPlacementInteractionSystem,
            _buildingPlacementInteractionContext,
            TryGetDefaultEntityManager,
            IsPointerOverGameplayUi,
            UpdateLastKnownPointerPosition,
            HideOrderScreenMarkers,
            _runtimeConfig.PanSensitivity,
            _runtimeConfig.ZoomSpeed,
            _runtimeConfig.MinZoomHeight,
            _runtimeConfig.MaxZoomHeight,
            _runtimeConfig.NormalModeZoomHeight,
            _runtimeConfig.BuildModeZoomHeight,
            _runtimeConfig.NormalModePitch,
            _runtimeConfig.BuildModePitch,
            _runtimeConfig.NormalModeYaw,
            _runtimeConfig.BuildModeYaw,
            _runtimeConfig.NormalModeFieldOfView,
            _runtimeConfig.BuildModeFieldOfView,
            _runtimeConfig.FullscreenIsoZoomHeight,
            _runtimeConfig.FullscreenIsoPitch,
            _runtimeConfig.FullscreenIsoYaw,
            _runtimeConfig.FullscreenIsoOrthographicSize,
            _runtimeConfig.ZoomTransitionSmoothTime);
    }

    private RtsSelectionCommandResultFlushSystem.Context CreateCommandResultFlushContext()
    {
        return new RtsSelectionCommandResultFlushSystem.Context(
            _rtsSelectionInputSystem,
            _selectionHudFeedbackSystem,
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
            _selectedMoveQuery,
            _gridConfigQuery,
            TryGetDefaultEntityManager,
            EnsureEntityQueries,
            ClearCurrentSelection,
            ApplyHudCommandResult,
            ClearHudCommandMode,
            SetHudWorldMarkersVisible,
            RequestMoveOrderScreenMarker,
            RequestAttackOrderScreenMarker,
            SetCameraDragging,
            _focusedUnitLifecycleSystem.ClearFocusedUnit,
            TryGetClickedUnitEntity,
            TryGetClickedCell,
            TryGetClickedUnitEntity,
            TryGetClickedUnitEntity,
            TryGetClickedCell);
    }

    private RtsSelectionFocusCommandSystem.Context CreateFocusCommandContext()
    {
        return new RtsSelectionFocusCommandSystem.Context(
            _runtimeGameplayStateSystem,
            _rtsSelectionInputSystem,
            _selectionStateSystem,
            _focusedUnitLifecycleSystem,
            _unitTargetOrderSystem,
            _buildingPlacementInteractionSystem,
            _buildingPlacementInteractionContext,
            _runtimeConfig.WorldCamera,
            TryGetDefaultEntityManager,
            EnsureEntityQueries,
            ClearCurrentSelection,
            QueueSelectionRectangleRequest,
            ProcessSelectionRectangleRequests,
            ApplyHudSelection,
            ApplyHudCommandResult,
            ClearHudSelection,
            ClearHudCommandMode,
            SetHudWorldMarkersVisible,
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
    }

    private RtsSelectionPointerTargetCommandSystem.Context CreatePointerTargetCommandContext()
    {
        return new RtsSelectionPointerTargetCommandSystem.Context(
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
            ApplyHudCommandMode,
            ApplyHudCommandResult,
            ClearHudSelection,
            ClearHudCommandMode,
            ApplyHudSelection,
            ClearCurrentSelection,
            RequestMoveOrderScreenMarker,
            SetCameraDragging,
            ProcessAttackCommandRequests,
            ProcessTransportCommandRequests,
            ProcessMoveCommandRequests,
            _selectionRuntimeDiagnosticsSystem.EnqueueSelectionDiagnostic,
            DescribeTransportBoardingEntity);
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
        if (_runtimeConfig.WorldCamera == null || World.DefaultGameObjectInjectionWorld == null)
            return false;

        var em = World.DefaultGameObjectInjectionWorld.EntityManager;
        EnsureEntityQueries(em);
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

        EnsureEntityQueries(em);
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
            ApplyHudSelection,
            ApplyHudSquadSelection,
            _selectionRuntimeDiagnosticsSystem.EnqueueSelectionDiagnostic,
            ClearSelectedBuildingAfterRectangleSelection);
    }

    private void ClearSelectedBuildingAfterRectangleSelection()
    {
        _buildingPlacementInteractionSystem?.ClearSelectedBuilding(_buildingPlacementInteractionContext, "RTSSelection.SelectUnitsInRectangle");
    }

    private void IssueMoveOrder(Vector2 screenPosition)
    {
        _rtsSelectionPointerTargetCommandSystem.IssueMoveOrder(CreatePointerTargetCommandContext(), screenPosition);
    }

    private void ProcessMoveCommandRequests()
    {
        _rtsSelectionCommandResultFlushSystem.ProcessMoveCommandRequests(CreateCommandResultFlushContext());
    }

    private bool ProcessAttackCommandRequests()
    {
        return _rtsSelectionCommandResultFlushSystem.ProcessAttackCommandRequests(
            CreateCommandResultFlushContext(),
            _explicitAttackTargetModeActive);
    }

    private bool TryIssueBoardTransportOrderToClickedUnit(Vector2 screenPosition)
    {
        return _rtsSelectionPointerTargetCommandSystem.TryIssueBoardTransportOrderToClickedUnit(CreatePointerTargetCommandContext(), screenPosition);
    }

    private bool ProcessTransportCommandRequests()
    {
        return _rtsSelectionCommandResultFlushSystem.ProcessTransportCommandRequests(CreateCommandResultFlushContext());
    }

    public bool IsBoardablePlayerTransportClick(Vector2 screenPosition)
    {
        return _rtsSelectionPointerTargetCommandSystem.IsBoardablePlayerTransportClick(CreatePointerTargetCommandContext(), screenPosition);
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
            CreatePointerTargetCommandContext(),
            originCell,
            footprintCells);
    }

    private bool TryGetClickedCell(Vector2 screenPosition, EntityManager em, out int2 cell, out Vector3 worldPoint)
    {
        return _rtsSelectionPointerTargetCommandSystem.TryGetClickedCell(
            CreatePointerTargetCommandContext(),
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
        _rtsSelectionRuntimeCameraSystem.SetCameraDragging(CreateRuntimeCameraContext(), isDragging);
    }

    public void EnterFullscreenMapIsoMode(Vector3 focusWorldPosition)
    {
        _rtsSelectionRuntimeCameraSystem.EnterFullscreenMapIsoMode(CreateRuntimeCameraContext(), focusWorldPosition);
    }

    public void ExitFullscreenMapIsoMode()
    {
        _rtsSelectionRuntimeCameraSystem.ExitFullscreenMapIsoMode(CreateRuntimeCameraContext());
    }

    public bool IsNormalIsoModeActive => _rtsCameraSystem.NormalIsoModeActive;

    public void ToggleNormalIsoMode()
    {
        _rtsSelectionRuntimeCameraSystem.ToggleNormalIsoMode(CreateRuntimeCameraContext());
    }

    public void EnterNormalIsoMode()
    {
        _rtsSelectionRuntimeCameraSystem.EnterNormalIsoMode(CreateRuntimeCameraContext());
    }

    public void ExitNormalIsoMode()
    {
        _rtsSelectionRuntimeCameraSystem.ExitNormalIsoMode(CreateRuntimeCameraContext());
    }

    public void MoveCameraGroundCenterTo(Vector3 focusWorldPosition)
    {
        _rtsSelectionRuntimeCameraSystem.MoveCameraGroundCenterTo(CreateRuntimeCameraContext(), focusWorldPosition);
    }

    public void SmoothMoveCameraGroundCenterTo(Vector3 focusWorldPosition)
    {
        _rtsSelectionRuntimeCameraSystem.SmoothMoveCameraGroundCenterTo(CreateRuntimeCameraContext(), focusWorldPosition);
    }

    public void FollowCameraGroundCenterTo(Vector3 focusWorldPosition)
    {
        _rtsSelectionRuntimeCameraSystem.FollowCameraGroundCenterTo(CreateRuntimeCameraContext(), focusWorldPosition);
    }

    private void ClearCurrentSelection(EntityManager em, string reason = "Unspecified")
    {
        _focusedUnitLifecycleSystem.ClearCurrentSelection(
            em,
            _selectionStateSystem,
            reason,
            _selectionRuntimeDiagnosticsSystem.EnqueueSelectionDiagnostic,
            ClearHudSelection);
    }

    public void ClearFocusedUnit()
    {
        _rtsSelectionFocusCommandSystem.ClearFocusedUnit(CreateFocusCommandContext());
    }

    public void DeselectAllUnits(string reason = "DeselectAllUnits")
    {
        _rtsSelectionFocusCommandSystem.DeselectAllUnits(CreateFocusCommandContext(), reason);
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
        _rtsSelectionFocusCommandSystem.SelectAllVisiblePlayerUnits(CreateFocusCommandContext(), filter);
    }

    public bool FocusUnitEntity(Entity entity)
    {
        return _rtsSelectionFocusCommandSystem.FocusUnitEntity(CreateFocusCommandContext(), entity);
    }

    public TacticalCommandResult TrySelectRuntimeEntity(Entity entity)
    {
        return _rtsSelectionFocusCommandSystem.TrySelectRuntimeEntity(CreateFocusCommandContext(), entity);
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
            SetCameraDragging(false);
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
        _rtsSelectionInputSystem.IsDraggingSelection = false;
        SetCameraDragging(false);
        _rtsSelectionInputSystem.SkipNextWorldReleaseAfterSelection = true;
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
        SetCameraDragging(false);
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
        return _rtsSelectionPointerTargetCommandSystem.TryFocusUnit(CreatePointerTargetCommandContext(), screenPosition);
    }

    private bool TryIssueAttackOrderToClickedUnit(Vector2 screenPosition)
    {
        return _rtsSelectionPointerTargetCommandSystem.TryIssueAttackOrderToClickedUnit(CreatePointerTargetCommandContext(), screenPosition);
    }

    private bool TryGetClickedUnitEntity(Vector2 screenPosition, EntityManager em, out Entity bestEntity)
    {
        return _rtsSelectionPointerTargetCommandSystem.TryGetClickedUnitEntity(
            CreatePointerTargetCommandContext(),
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
