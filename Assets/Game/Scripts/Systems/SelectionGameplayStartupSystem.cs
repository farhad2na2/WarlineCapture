using System.Collections.Generic;
using Game.Scripts.UI;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

internal sealed class SelectionGameplayStartupSystem
{
    public readonly struct Result
    {
        public readonly System.Action<MainMenuPlayUI> BindSelectionMainMenu;
        public readonly System.Action SelectionRuntimeUpdate;
        public readonly System.Action DisposeSelection;
        public readonly SelectionUiCommandSystem SelectionUiCommand;
        public readonly SelectionUiReadModelSystem SelectionUiReadModel;
        public readonly SelectionUiCameraSystem SelectionUiCamera;
        public readonly SelectionBuildingInteractionSystem SelectionBuildingInteraction;
        public readonly SelectionScreenMarkerSystem SelectionScreenMarkers;
        public readonly SelectionRectangleView SelectionRectangleView;

        public Result(
            System.Action<MainMenuPlayUI> bindSelectionMainMenu,
            System.Action selectionRuntimeUpdate,
            System.Action disposeSelection,
            SelectionUiCommandSystem selectionUiCommand,
            SelectionUiReadModelSystem selectionUiReadModel,
            SelectionUiCameraSystem selectionUiCamera,
            SelectionBuildingInteractionSystem selectionBuildingInteraction,
            SelectionScreenMarkerSystem selectionScreenMarkers,
            SelectionRectangleView selectionRectangleView)
        {
            BindSelectionMainMenu = bindSelectionMainMenu;
            SelectionRuntimeUpdate = selectionRuntimeUpdate;
            DisposeSelection = disposeSelection;
            SelectionUiCommand = selectionUiCommand;
            SelectionUiReadModel = selectionUiReadModel;
            SelectionUiCamera = selectionUiCamera;
            SelectionBuildingInteraction = selectionBuildingInteraction;
            SelectionScreenMarkers = selectionScreenMarkers;
            SelectionRectangleView = selectionRectangleView;
        }
    }

    public Result Initialize(
        RTSSelectionSystemConfig rtsSelectionConfig,
        Camera worldCamera,
        Transform runtimeUiRoot,
        RoadBuildReadModelSystem roadBuildReadModel,
        BuildingPlacementInteractionSystem buildingInteraction,
        BuildingPlacementInteractionSystem.Context buildingInteractionContext,
        FactionVisualSettings factionVisuals)
    {
        var selectionRuntimeDiagnosticsSystem = new SelectionRuntimeDiagnosticsSystem();
        var selectionRuntimeConfigSystem = new SelectionRuntimeConfigSystem();
        var selectionRuntimeQuerySystem = new SelectionRuntimeQuerySystem();
        SelectionRuntimeConfigSystem.State runtimeConfig = selectionRuntimeConfigSystem.CreateState(rtsSelectionConfig, worldCamera);
        var runtimeGameplayStateSystem = new RuntimeGameplayStateSystem();
        var rtsSelectionInputSystem = new RtsSelectionInputSystem();
        var rtsSelectionRuntimeInputSystem = new RtsSelectionRuntimeInputSystem();
        var rtsSelectionRuntimeInputContextSystem = new RtsSelectionRuntimeInputContextSystem();
        var rtsSelectionRuntimeCameraSystem = new RtsSelectionRuntimeCameraSystem();
        var rtsSelectionRuntimeCameraContextSystem = new RtsSelectionRuntimeCameraContextSystem();
        var rtsSelectionCommandResultFlushSystem = new RtsSelectionCommandResultFlushSystem();
        var rtsSelectionCommandResultContextSystem = new RtsSelectionCommandResultContextSystem();
        var rtsSelectionFocusCommandSystem = new RtsSelectionFocusCommandSystem();
        var rtsSelectionFocusCommandContextSystem = new RtsSelectionFocusCommandContextSystem();
        var rtsSelectionPointerTargetCommandSystem = new RtsSelectionPointerTargetCommandSystem();
        var rtsSelectionPointerTargetCommandContextSystem = new RtsSelectionPointerTargetCommandContextSystem();
        var rtsCameraSystem = new RtsCameraSystem();
        var rtsCameraRequestSystem = new RtsCameraRequestSystem();
        var selectionUiCommand = new SelectionUiCommandSystem();
        var selectionUiReadModel = new SelectionUiReadModelSystem();
        var selectionUiCamera = new SelectionUiCameraSystem(rtsCameraSystem, rtsCameraRequestSystem);
        var selectionScreenMarkers = new SelectionScreenMarkerSystem();
        var selectionStateSystem = new SelectionStateSystem();
        var selectionUiQuerySystem = new SelectionUiQuerySystem();
        var focusedUnitUiReadModelSystem = new FocusedUnitUiReadModelSystem();
        var visibleUnitSelectionSystem = new VisibleUnitSelectionSystem();
        var selectionRectangleRequestSystem = new SelectionRectangleRequestSystem();
        var unitMoveOrderSystem = new UnitMoveOrderSystem();
        var selectedMoveOrderCommandSystem = new SelectedMoveOrderCommandSystem();
        var selectionMoveCommandRequestSystem = new SelectionMoveCommandRequestSystem();
        var unitTargetOrderSystem = new UnitTargetOrderSystem();
        var attackOrderCommandSystem = new AttackOrderCommandSystem();
        var selectionAttackCommandRequestSystem = new SelectionAttackCommandRequestSystem();
        var selectionOrderMarkerSystem = new SelectionOrderMarkerSystem();
        var selectionHudFeedbackSystem = new SelectionHudFeedbackSystem();
        var focusedUnitCommandSystem = new FocusedUnitCommandSystem();
        var focusedUnitLifecycleSystem = new FocusedUnitLifecycleSystem();
        var selectedUnitOrderSnapshotSystem = new SelectedUnitOrderSnapshotSystem();
        var buildingTargetMoveOrderSystem = new BuildingTargetMoveOrderSystem();
        var transportBoardingCommandSystem = new TransportBoardingCommandSystem();
        var selectionTransportCommandRequestSystem = new SelectionTransportCommandRequestSystem();
        var focusableUnitLookupSystem = new FocusableUnitLookupSystem();
        var unitTransportCapacitySystem = new UnitTransportCapacitySystem();
        var unitTransportBoardingQuerySystem = new UnitTransportBoardingQuerySystem();
        var unitTransportBoardingRuleSystem = new UnitTransportBoardingRuleSystem();
        var unitTransportApproachCellSystem = new UnitTransportApproachCellSystem();
        var unitTransportAirPickupSystem = new UnitTransportAirPickupSystem();
        var unitTransportRopeDisembarkCommandSystem = new UnitTransportRopeDisembarkCommandSystem();
        var selectionBuildingInteraction = new SelectionBuildingInteractionSystem();
        var visibleSelectionScratch = new List<Entity>();
        MainMenuPlayUI mainMenuPlayUi = null;
        RoadBuildReadModelSystem roadBuildReadState = roadBuildReadModel;
        BuildingPlacementInteractionSystem buildingPlacementInteractionSystem = buildingInteraction;
        BuildingPlacementInteractionSystem.Context buildingPlacementInteractionContext = buildingInteractionContext;
        bool explicitAttackTargetModeActive = false;

        selectionUiCamera.Init(rtsSelectionConfig, worldCamera);
        selectionBuildingInteraction.Init(selectionStateSystem, selectionScreenMarkers, worldCamera);
        selectionHudFeedbackSystem.ResetBridgeCache();
        selectionOrderMarkerSystem.Initialize(
            runtimeConfig.MoveOrderMarkerPrefab,
            runtimeConfig.AttackOrderMarkerPrefab,
            runtimeConfig.OrderMarkerVisibleSeconds,
            runtimeUiRoot);

        return new Result(
            BindSelectionMainMenu,
            UpdateSelectionRuntimePhases,
            selectionOrderMarkerSystem.Dispose,
            selectionUiCommand,
            selectionUiReadModel,
            selectionUiCamera,
            selectionBuildingInteraction,
            selectionScreenMarkers,
            EnsureSelectionRectangleView(runtimeUiRoot, rtsSelectionConfig));

        void BindSelectionMainMenu(MainMenuPlayUI mainMenu)
        {
            mainMenuPlayUi = mainMenu;
            roadBuildReadState = roadBuildReadModel;
            buildingPlacementInteractionSystem = buildingInteraction;
            buildingPlacementInteractionContext = buildingInteractionContext;
        }

        void UpdateSelectionRuntimePhases()
        {
            ProcessTransportCommandRequests();
            rtsSelectionFocusCommandSystem.ProcessExternalSelectionCommandRequests(CreateFocusCommandContext());
            rtsSelectionRuntimeInputSystem.ProcessQueuedMoveOrder(CreateRuntimeInputContext());
            RefreshFocusedSelectionReadModels();
            rtsSelectionCommandResultFlushSystem.UpdateOrderMarkerVisibility(CreateCommandResultFlushContext());

            if (rtsSelectionRuntimeCameraSystem.UpdateRuntimeCameraTick(CreateRuntimeCameraContext()))
                rtsSelectionRuntimeInputSystem.UpdateNormalPointerInput(CreateRuntimeInputContext());
        }

        RtsSelectionRuntimeInputSystem.Context CreateRuntimeInputContext()
        {
            return rtsSelectionRuntimeInputContextSystem.Create(
                runtimeGameplayStateSystem,
                rtsSelectionInputSystem,
                mainMenuPlayUi,
                runtimeConfig,
                () => explicitAttackTargetModeActive,
                value => explicitAttackTargetModeActive = value,
                () => rtsCameraSystem.IsDragging,
                value => rtsSelectionRuntimeCameraSystem.SetCameraDragging(CreateRuntimeCameraContext(), value),
                pointerPosition => IsPointerOverUI(pointerPosition, out _),
                pointerPosition => IsPointerOverGameplayUi(pointerPosition, out _),
                TryIssueAttackOrderToClickedUnit,
                TryIssueBoardTransportOrderToClickedUnit,
                TryFocusUnit,
                screenDelta => rtsSelectionRuntimeCameraSystem.PanCamera(CreateRuntimeCameraContext(), screenDelta),
                IssueMoveOrder,
                ProcessSelectionRectangleRequests);
        }

        RtsSelectionRuntimeCameraSystem.Context CreateRuntimeCameraContext()
        {
            return rtsSelectionRuntimeCameraContextSystem.Create(
                runtimeGameplayStateSystem,
                rtsSelectionInputSystem,
                rtsCameraSystem,
                rtsCameraRequestSystem,
                runtimeConfig,
                mainMenuPlayUi,
                roadBuildReadState,
                buildingPlacementInteractionSystem,
                buildingPlacementInteractionContext,
                TryGetDefaultEntityManager,
                IsPointerOverGameplayUi,
                UpdateLastKnownPointerPosition,
                HideOrderScreenMarkers);
        }

        RtsSelectionCommandResultFlushSystem.Context CreateCommandResultFlushContext()
        {
            return rtsSelectionCommandResultContextSystem.Create(
                rtsSelectionInputSystem,
                selectionHudFeedbackSystem,
                CreateHudFeedbackContext(),
                selectionOrderMarkerSystem,
                selectionMoveCommandRequestSystem,
                selectionAttackCommandRequestSystem,
                selectionTransportCommandRequestSystem,
                selectedMoveOrderCommandSystem,
                attackOrderCommandSystem,
                transportBoardingCommandSystem,
                unitMoveOrderSystem,
                unitTargetOrderSystem,
                unitTransportCapacitySystem,
                unitTransportBoardingQuerySystem,
                unitTransportBoardingRuleSystem,
                unitTransportApproachCellSystem,
                unitTransportAirPickupSystem,
                unitTransportRopeDisembarkCommandSystem,
                selectionStateSystem,
                buildingPlacementInteractionSystem,
                buildingPlacementInteractionContext,
                selectionRuntimeQuerySystem,
                TryGetDefaultEntityManager,
                EnsureRuntimeSelectionDependencies,
                ClearCurrentSelection,
                RequestMoveOrderScreenMarker,
                RequestAttackOrderScreenMarker,
                SetCameraDragging,
                focusedUnitLifecycleSystem.ClearFocusedUnit,
                TryGetClickedUnitEntity,
                TryGetClickedCell,
                TryGetClickedUnitEntity,
                TryGetClickedUnitEntity,
                TryGetClickedCell);
        }

        RtsSelectionFocusCommandSystem.Context CreateFocusCommandContext()
        {
            return rtsSelectionFocusCommandContextSystem.Create(
                runtimeGameplayStateSystem,
                rtsSelectionInputSystem,
                selectionStateSystem,
                focusedUnitLifecycleSystem,
                unitTargetOrderSystem,
                buildingPlacementInteractionSystem,
                buildingPlacementInteractionContext,
                runtimeConfig.WorldCamera,
                TryGetDefaultEntityManager,
                EnsureRuntimeSelectionDependencies,
                ClearCurrentSelection,
                QueueSelectionRectangleRequest,
                ProcessSelectionRectangleRequests,
                selectionHudFeedbackSystem,
                CreateHudFeedbackContext(),
                SetCameraDragging,
                value => explicitAttackTargetModeActive = value,
                selectionRuntimeDiagnosticsSystem.EnqueueSelectionDiagnostic,
                DescribeTransportBoardingEntity,
                ValidateControllableEntity,
                IssueHoldPositionOrder,
                IssueStopOrder,
                DestroyFocusedUnit,
                IssueFocusedMissileLauncherRadarAttack,
                ArmFocusedAttackTargetMode,
                CancelExplicitAttackTargetMode);
        }

        RtsSelectionPointerTargetCommandSystem.Context CreatePointerTargetCommandContext()
        {
            return rtsSelectionPointerTargetCommandContextSystem.Create(
                runtimeGameplayStateSystem,
                rtsSelectionInputSystem,
                selectionStateSystem,
                focusedUnitLifecycleSystem,
                unitTargetOrderSystem,
                focusableUnitLookupSystem,
                transportBoardingCommandSystem,
                unitTransportCapacitySystem,
                unitTransportBoardingQuerySystem,
                unitTransportBoardingRuleSystem,
                unitTransportApproachCellSystem,
                unitTransportAirPickupSystem,
                unitTransportRopeDisembarkCommandSystem,
                buildingTargetMoveOrderSystem,
                buildingPlacementInteractionSystem,
                buildingPlacementInteractionContext,
                runtimeConfig.WorldCamera,
                TryGetDefaultEntityManager,
                TryGetPointerPosition,
                () => explicitAttackTargetModeActive,
                value => explicitAttackTargetModeActive = value,
                selectionHudFeedbackSystem,
                CreateHudFeedbackContext(),
                ClearCurrentSelection,
                RequestMoveOrderScreenMarker,
                SetCameraDragging,
                ProcessAttackCommandRequests,
                ProcessTransportCommandRequests,
                ProcessMoveCommandRequests,
                selectionRuntimeDiagnosticsSystem.EnqueueSelectionDiagnostic,
                DescribeTransportBoardingEntity);
        }

        SelectionHudFeedbackSystem.Context CreateHudFeedbackContext()
        {
            return new SelectionHudFeedbackSystem.Context(selectionUiQuerySystem, TryGetDefaultEntityManager);
        }

        bool TryGetDefaultEntityManager(out EntityManager em)
        {
            em = default;
            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
                return false;

            em = world.EntityManager;
            return true;
        }

        void EnsureRuntimeSelectionDependencies(EntityManager em)
        {
            selectionRuntimeQuerySystem.EnsureEntityQueries(em);
            focusableUnitLookupSystem.EnsureEntityQueries(em);
            visibleUnitSelectionSystem.EnsureEntityQueries(em);
            attackOrderCommandSystem.EnsureEntityQueries(em);
            selectionOrderMarkerSystem.EnsureEntityQueries(em);
            focusedUnitCommandSystem.EnsureEntityQueries(em);
            focusedUnitLifecycleSystem.EnsureEntityQueries(em);
            selectedUnitOrderSnapshotSystem.EnsureEntityQueries(em);
            buildingTargetMoveOrderSystem.EnsureEntityQueries(em);
            transportBoardingCommandSystem.EnsureEntityQueries(em);
        }

        void RefreshFocusedSelectionReadModels()
        {
            RefreshFocusedUnit();
            PublishFocusedUnitUiReadModel();
        }

        void PublishFocusedUnitUiReadModel()
        {
            if (!TryGetDefaultEntityManager(out EntityManager em))
                return;

            focusedUnitUiReadModelSystem.Publish(
                em,
                selectionStateSystem,
                selectionUiQuerySystem,
                unitTransportCapacitySystem,
                Time.time);
        }

        void RefreshFocusedUnit()
        {
            if (!TryGetDefaultEntityManager(out EntityManager em))
                return;

            EnsureRuntimeSelectionDependencies(em);
            focusedUnitLifecycleSystem.RefreshFocusedUnit(
                em,
                selectionStateSystem,
                (entityManager, entity) => selectionHudFeedbackSystem.ApplySelection(CreateHudFeedbackContext(), entityManager, entity));
        }

        void ProcessSelectionRectangleRequests()
        {
            if (TryGetDefaultEntityManager(out EntityManager defaultEntityManager))
                selectionHudFeedbackSystem.EnsureFeedbackQueue(defaultEntityManager);

            if (!rtsSelectionInputSystem.TryGetPointerRequests(out EntityManager em, out DynamicBuffer<RtsSelectionPointerRequestElement> pointerRequests))
                return;

            EnsureRuntimeSelectionDependencies(em);
            selectionRectangleRequestSystem.ProcessPendingRequests(
                em,
                pointerRequests,
                runtimeConfig.WorldCamera,
                selectionUiQuerySystem,
                visibleUnitSelectionSystem,
                selectionStateSystem,
                focusedUnitLifecycleSystem,
                visibleSelectionScratch,
                ClearCurrentSelection,
                selectionStateSystem.CacheSelectedMoveEntities,
                (entityManager, entity) => selectionHudFeedbackSystem.ApplySelection(CreateHudFeedbackContext(), entityManager, entity),
                selectedCount => selectionHudFeedbackSystem.ApplySquadSelection(CreateHudFeedbackContext(), selectedCount),
                selectionRuntimeDiagnosticsSystem.EnqueueSelectionDiagnostic,
                ClearSelectedBuildingAfterRectangleSelection);
        }

        void ClearSelectedBuildingAfterRectangleSelection()
        {
            buildingPlacementInteractionSystem?.ClearSelectedBuilding(buildingPlacementInteractionContext, "RTSSelection.SelectUnitsInRectangle");
        }

        void ClearCurrentSelection(EntityManager em, string reason = "Unspecified")
        {
            focusedUnitLifecycleSystem.ClearCurrentSelection(
                em,
                selectionStateSystem,
                reason,
                selectionRuntimeDiagnosticsSystem.EnqueueSelectionDiagnostic,
                () => selectionHudFeedbackSystem.ClearSelection(CreateHudFeedbackContext()));
        }

        void QueueSelectionRectangleRequest(
            Rect screenRect,
            RtsSelectionPointerRequestKind kind,
            VisibleUnitSelectionSystem.Filter filter = VisibleUnitSelectionSystem.Filter.All)
        {
            rtsSelectionInputSystem.QueueSelectionRectangleRequest(kind, screenRect, Time.frameCount, filter);
        }

        void IssueMoveOrder(Vector2 screenPosition)
        {
            rtsSelectionPointerTargetCommandSystem.IssueMoveOrder(CreatePointerTargetCommandContext(), screenPosition);
        }

        void ProcessMoveCommandRequests()
        {
            rtsSelectionCommandResultFlushSystem.ProcessMoveCommandRequests(CreateCommandResultFlushContext());
        }

        bool ProcessAttackCommandRequests()
        {
            return rtsSelectionCommandResultFlushSystem.ProcessAttackCommandRequests(
                CreateCommandResultFlushContext(),
                explicitAttackTargetModeActive);
        }

        bool ProcessTransportCommandRequests()
        {
            return rtsSelectionCommandResultFlushSystem.ProcessTransportCommandRequests(CreateCommandResultFlushContext());
        }

        bool TryIssueBoardTransportOrderToClickedUnit(Vector2 screenPosition)
        {
            return rtsSelectionPointerTargetCommandSystem.TryIssueBoardTransportOrderToClickedUnit(CreatePointerTargetCommandContext(), screenPosition);
        }

        bool TryFocusUnit(Vector2 screenPosition)
        {
            return rtsSelectionPointerTargetCommandSystem.TryFocusUnit(CreatePointerTargetCommandContext(), screenPosition);
        }

        bool TryIssueAttackOrderToClickedUnit(Vector2 screenPosition)
        {
            return rtsSelectionPointerTargetCommandSystem.TryIssueAttackOrderToClickedUnit(CreatePointerTargetCommandContext(), screenPosition);
        }

        bool TryGetClickedCell(Vector2 screenPosition, EntityManager em, out int2 cell, out Vector3 worldPoint)
        {
            return rtsSelectionPointerTargetCommandSystem.TryGetClickedCell(
                CreatePointerTargetCommandContext(),
                screenPosition,
                em,
                out cell,
                out worldPoint);
        }

        bool TryGetClickedUnitEntity(Vector2 screenPosition, EntityManager em, out Entity bestEntity)
        {
            return rtsSelectionPointerTargetCommandSystem.TryGetClickedUnitEntity(
                CreatePointerTargetCommandContext(),
                screenPosition,
                em,
                out bestEntity);
        }

        bool TryGetFocusedUnitEntity(out EntityManager em, out Entity entity)
        {
            em = default;
            entity = Entity.Null;
            if (!TryGetDefaultEntityManager(out em))
                return false;

            return focusedUnitLifecycleSystem.TryGetFocusedUnitEntity(em, selectionStateSystem, out entity);
        }

        bool FocusedUnitOwnedByPlayer()
        {
            return TryGetFocusedUnitEntity(out EntityManager em, out Entity entity) &&
                   selectionUiQuerySystem.IsOwnedByPlayer(em, entity);
        }

        bool FocusedUnitCanAttack()
        {
            return TryGetFocusedUnitEntity(out EntityManager em, out Entity entity) &&
                   selectionUiQuerySystem.CanAttack(em, entity);
        }

        void DestroyFocusedUnit()
        {
            if (!TryGetFocusedUnitEntity(out EntityManager em, out Entity entity) || !FocusedUnitOwnedByPlayer())
                return;

            focusedUnitCommandSystem.DestroyFocusedUnit(em, entity);
            focusedUnitLifecycleSystem.ClearFocusedUnit(selectionStateSystem);
        }

        bool IssueFocusedMissileLauncherRadarAttack()
        {
            if (!TryGetFocusedUnitEntity(out EntityManager em, out Entity launcher) || !FocusedUnitOwnedByPlayer())
                return false;
            if (!focusedUnitCommandSystem.TryIssueFocusedMissileLauncherRadarAttack(
                    em,
                    launcher,
                    unitTargetOrderSystem,
                    out float3 targetPosition))
            {
                return false;
            }

            selectionOrderMarkerSystem.ShowAttackOrderMarker(em, targetPosition);
            ClearCurrentSelection(em, "MissileLauncherRadarAttack");
            focusedUnitLifecycleSystem.SetFocusedUnit(selectionStateSystem, launcher);
            explicitAttackTargetModeActive = false;
            SetCameraDragging(false);
            selectionHudFeedbackSystem.ApplyCommandResult(CreateHudFeedbackContext(), TacticalCommandResult.Success());
            selectionHudFeedbackSystem.ClearCommandMode(CreateHudFeedbackContext());
            selectionHudFeedbackSystem.SetWorldMarkersVisible(CreateHudFeedbackContext(), true);
            selectionHudFeedbackSystem.ApplySelection(CreateHudFeedbackContext(), em, launcher);
            return true;
        }

        bool ArmFocusedAttackTargetMode()
        {
            bool hasFocusedUnit = TryGetFocusedUnitEntity(out _, out _);
            if (!hasFocusedUnit || !FocusedUnitOwnedByPlayer() || !FocusedUnitCanAttack())
            {
                selectionHudFeedbackSystem.ApplyCommandResult(CreateHudFeedbackContext(), TacticalCommandResult.Rejected(
                    hasFocusedUnit ? TacticalCommandReasonCode.TargetNotAttackable : TacticalCommandReasonCode.NoSelection));
                return false;
            }

            explicitAttackTargetModeActive = true;
            selectionHudFeedbackSystem.ApplyCommandMode(CreateHudFeedbackContext(), TacticalCommandMode.Attack);
            selectionHudFeedbackSystem.SetWorldMarkersVisible(CreateHudFeedbackContext(), true);
            runtimeGameplayStateSystem.SelectionModeActive = false;
            runtimeGameplayStateSystem.SuppressNextWorldClick = true;
            rtsSelectionInputSystem.IsDraggingSelection = false;
            SetCameraDragging(false);
            rtsSelectionInputSystem.SkipNextWorldReleaseAfterSelection = true;
            return true;
        }

        void CancelExplicitAttackTargetMode()
        {
            explicitAttackTargetModeActive = false;
            selectionHudFeedbackSystem.ClearCommandMode(CreateHudFeedbackContext());
        }

        void IssueHoldPositionOrder()
        {
            IssueImmediateSelectedUnitOrder(TacticalCommandMode.Hold, clearEngageTarget: true);
        }

        void IssueStopOrder()
        {
            IssueImmediateSelectedUnitOrder(TacticalCommandMode.Stop, clearEngageTarget: true);
        }

        bool IssueImmediateSelectedUnitOrder(TacticalCommandMode mode, bool clearEngageTarget)
        {
            selectionHudFeedbackSystem.ApplyCommandMode(CreateHudFeedbackContext(), mode);

            if (!TryGetDefaultEntityManager(out EntityManager em))
            {
                selectionHudFeedbackSystem.ApplyCommandResult(CreateHudFeedbackContext(), TacticalCommandResult.Rejected(TacticalCommandReasonCode.NoSelection));
                return false;
            }

            bool issued = focusedUnitCommandSystem.IssueImmediateSelectedUnitOrder(
                em,
                clearEngageTarget,
                unitMoveOrderSystem);
            if (!issued)
            {
                selectionHudFeedbackSystem.ApplyCommandResult(CreateHudFeedbackContext(), TacticalCommandResult.Rejected(TacticalCommandReasonCode.NoSelection));
                return false;
            }

            explicitAttackTargetModeActive = false;
            SetCameraDragging(false);
            selectionHudFeedbackSystem.SetWorldMarkersVisible(CreateHudFeedbackContext(), false);
            selectionHudFeedbackSystem.ApplyCommandResult(CreateHudFeedbackContext(), TacticalCommandResult.Success());
            return true;
        }

        TacticalCommandResult ValidateControllableEntity(Entity entity)
        {
            if (entity == Entity.Null || !TryGetDefaultEntityManager(out EntityManager em))
                return TacticalCommandResult.Rejected(TacticalCommandReasonCode.TargetNotAttackable);

            if (!em.Exists(entity) || !em.HasComponent<Faction>(entity) || !em.HasComponent<UnitMove>(entity))
                return TacticalCommandResult.Rejected(TacticalCommandReasonCode.TargetNotAttackable);
            if (!FactionIdentitySystem.IsPlayerControlled(em.GetComponentData<Faction>(entity).Id))
                return TacticalCommandResult.Rejected(TacticalCommandReasonCode.TargetNotAttackable);
            if (em.HasComponent<UnitHealth>(entity) && em.GetComponentData<UnitHealth>(entity).Current <= 0)
                return TacticalCommandResult.Rejected(TacticalCommandReasonCode.TargetNotAttackable);

            return TacticalCommandResult.Success();
        }

        void SetCameraDragging(bool isDragging)
        {
            rtsSelectionRuntimeCameraSystem.SetCameraDragging(CreateRuntimeCameraContext(), isDragging);
        }

        void HideOrderScreenMarkers()
        {
            selectionScreenMarkers.RequestHideOrderMarkers();
        }

        void RequestMoveOrderScreenMarker(Vector2 screenPosition)
        {
            selectionScreenMarkers.RequestMoveOrderMarker(screenPosition);
        }

        void RequestAttackOrderScreenMarker(Vector2 screenPosition)
        {
            selectionScreenMarkers.RequestAttackOrderMarker(screenPosition);
        }

        void UpdateLastKnownPointerPosition(Vector2 pointerPosition)
        {
            rtsSelectionInputSystem.UpdateLastKnownPointerPosition(pointerPosition);
        }

        bool TryGetPointerPosition(out Vector2 pointerPosition)
        {
            if (GamePointerInput.TryGetPrimaryPointer(out GamePointerState pointer))
            {
                pointerPosition = pointer.Position;
                UpdateLastKnownPointerPosition(pointerPosition);
                return true;
            }

            return rtsSelectionInputSystem.TryGetLastKnownPointerPosition(out pointerPosition);
        }

        bool IsPointerOverGameplayUi(Vector2 screenPosition, out string source)
        {
            if (mainMenuPlayUi != null)
                return mainMenuPlayUi.IsPointerOverAnyGameplayUi(screenPosition, out source);

            return IsPointerOverUI(screenPosition, out source);
        }
    }

    private static bool IsPointerOverUI(Vector2 screenPosition, out string source)
    {
        source = null;
        return false;
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

    private static SelectionRectangleView EnsureSelectionRectangleView(
        Transform runtimeUiRoot,
        RTSSelectionSystemConfig rtsSelectionConfig)
    {
        if (runtimeUiRoot == null)
            return null;

        SelectionRectangleView view = runtimeUiRoot.GetComponent<SelectionRectangleView>();
        if (view == null)
            view = runtimeUiRoot.gameObject.AddComponent<SelectionRectangleView>();

        view.ApplyConfig(rtsSelectionConfig);
        return view;
    }
}
