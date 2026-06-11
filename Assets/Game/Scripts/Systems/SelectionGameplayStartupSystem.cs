using System.Collections.Generic;
using Game.Scripts.UI;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

internal sealed class SelectionGameplayStartupSystem
{
    private readonly struct TransportBoardingOrder
    {
        public readonly Entity Passenger;
        public readonly int2 Goal;

        public TransportBoardingOrder(Entity passenger, int2 goal)
        {
            Passenger = passenger;
            Goal = goal;
        }
    }

    public readonly struct Result
    {
        public readonly System.Action<MainMenuPlayUI> BindSelectionMainMenu;
        public readonly System.Action<MatchHudSelectionPanelView> BindMatchHudSelectionPanel;
        public readonly System.Action SelectionRuntimeUpdate;
        public readonly System.Action DisposeSelection;
        public readonly SelectionUiCommandSystem SelectionUiCommand;
        public readonly SelectionUiReadModelSystem SelectionUiReadModel;
        public readonly SelectionUiCameraSystem SelectionUiCamera;
        public readonly SelectionBuildingInteractionSystem SelectionBuildingInteraction;
        public readonly SelectionScreenMarkerSystem SelectionScreenMarkers;
        public readonly SelectionRectangleView SelectionRectangleView;
        public readonly System.Func<bool> ShouldBlockBuildingSelectionClick;

        public Result(
            System.Action<MainMenuPlayUI> bindSelectionMainMenu,
            System.Action<MatchHudSelectionPanelView> bindMatchHudSelectionPanel,
            System.Action selectionRuntimeUpdate,
            System.Action disposeSelection,
            SelectionUiCommandSystem selectionUiCommand,
            SelectionUiReadModelSystem selectionUiReadModel,
            SelectionUiCameraSystem selectionUiCamera,
            SelectionBuildingInteractionSystem selectionBuildingInteraction,
            SelectionScreenMarkerSystem selectionScreenMarkers,
            SelectionRectangleView selectionRectangleView,
            System.Func<bool> shouldBlockBuildingSelectionClick)
        {
            BindSelectionMainMenu = bindSelectionMainMenu;
            BindMatchHudSelectionPanel = bindMatchHudSelectionPanel;
            SelectionRuntimeUpdate = selectionRuntimeUpdate;
            DisposeSelection = disposeSelection;
            SelectionUiCommand = selectionUiCommand;
            SelectionUiReadModel = selectionUiReadModel;
            SelectionUiCamera = selectionUiCamera;
            SelectionBuildingInteraction = selectionBuildingInteraction;
            SelectionScreenMarkers = selectionScreenMarkers;
            SelectionRectangleView = selectionRectangleView;
            ShouldBlockBuildingSelectionClick = shouldBlockBuildingSelectionClick;
        }
    }

    public Result Initialize(
        RTSSelectionSystemConfig rtsSelectionConfig,
        Camera worldCamera,
        Transform runtimeUiRoot,
        RoadBuildReadModelSystem roadBuildReadModel,
        BuildingPlacementInteractionSystem buildingInteraction,
        BuildingPlacementInteractionSystem.Context buildingInteractionContext,
        System.Func<Rect, bool> trySelectFirstBuildingInScreenRect,
        SelectionHudFeedbackSystem.ResolveSelectionPortraitSpriteDelegate resolveSelectionPortraitSprite,
        SelectionHudFeedbackSystem.ResolveSelectionPortraitSpriteDelegate resolveSelectionCardPortraitSprite,
        System.Func<Sprite> resolveSelectedBuildingPortraitSprite,
        SelectionOrderMarkerSystem.TryResolveRuntimeBuildingInstanceDelegate tryResolveRuntimeBuildingInstance,
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
        World cachedMatchIntroWorld = null;
        EntityQuery matchIntroLockQuery = default;
        bool hasMatchIntroLockQuery = false;
        var selectionUiCommand = new SelectionUiCommandSystem(IsMatchIntroGameplayInputLocked);
        var selectionUiReadModel = new SelectionUiReadModelSystem();
        var selectionUiCamera = new SelectionUiCameraSystem(rtsCameraSystem, rtsCameraRequestSystem);
        var selectionScreenMarkers = new SelectionScreenMarkerSystem();
        var selectionStateSystem = new SelectionStateSystem();
        var selectionUiQuerySystem = new SelectionUiQuerySystem();
        var selectionSummaryQuerySystem = new SelectionSummaryQuerySystem();
        var focusedUnitUiReadModelSystem = new FocusedUnitUiReadModelSystem();
        var visibleUnitSelectionSystem = new VisibleUnitSelectionSystem();
        var selectionRectangleRequestSystem = new SelectionRectangleRequestSystem();
        var unitMoveOrderSystem = new UnitMoveOrderSystem();
        var selectedMoveOrderCommandSystem = new SelectedMoveOrderCommandSystem();
        var selectionMoveCommandRequestSystem = new SelectionMoveCommandRequestSystem();
        var unitTargetOrderSystem = new UnitTargetOrderSystem();
        var attackOrderCommandSystem = new AttackOrderCommandSystem();
        var selectionAttackCommandRequestSystem = new SelectionAttackCommandRequestSystem();
        var scanIntelCommandSystem = new ScanIntelCommandSystem();
        var selectionScanCommandRequestSystem = new SelectionScanCommandRequestSystem();
        var selectionOrderMarkerSystem = new SelectionOrderMarkerSystem();
        var selectionHudFeedbackSystem = new SelectionHudFeedbackSystem();
        var focusedUnitCommandSystem = new FocusedUnitCommandSystem();
        var focusedUnitLifecycleSystem = new FocusedUnitLifecycleSystem();
        var selectedUnitOrderSnapshotSystem = new SelectedUnitOrderSnapshotSystem();
        var buildingTargetMoveOrderSystem = new BuildingTargetMoveOrderSystem();
        var transportBoardingCommandSystem = new TransportBoardingCommandSystem();
        var selectionTransportCommandRequestSystem = new SelectionTransportCommandRequestSystem();
        var focusableUnitLookupSystem = new FocusableUnitLookupSystem();
        var matchHudSquadTraySelectionSystem = new MatchHudSquadTraySelectionSystem();
        var unitTransportCapacitySystem = new UnitTransportCapacitySystem();
        var unitTransportBoardingQuerySystem = new UnitTransportBoardingQuerySystem();
        var unitTransportBoardingRuleSystem = new UnitTransportBoardingRuleSystem();
        var unitTransportApproachCellSystem = new UnitTransportApproachCellSystem();
        var unitTransportAirPickupSystem = new UnitTransportAirPickupSystem();
        var unitTransportRopeDisembarkCommandSystem = new UnitTransportRopeDisembarkCommandSystem();
        var selectionBuildingInteraction = new SelectionBuildingInteractionSystem();
        var visibleSelectionScratch = new List<Entity>();
        var selectedAttackSourceScratch = new List<Entity>();
        var transportPassengerPanelItems = new List<MatchHudSelectionPanelView.PassengerItemModel>();
        MainMenuPlayUI mainMenuPlayUi = null;
        MatchHudSelectionPanelView matchHudSelectionPanelView = null;
        MatchHudSquadTrayView matchHudSquadTrayView = null;
        RoadBuildReadModelSystem roadBuildReadState = roadBuildReadModel;
        BuildingPlacementInteractionSystem buildingPlacementInteractionSystem = buildingInteraction;
        BuildingPlacementInteractionSystem.Context buildingPlacementInteractionContext = buildingInteractionContext;
        bool explicitAttackTargetModeActive = false;
        bool attackModeOrderSnapshotActive = false;
        string attackModeOrderSnapshotText = string.Empty;

        selectionUiCamera.Init(rtsSelectionConfig, worldCamera);
        selectionBuildingInteraction.Init(selectionStateSystem, selectionScreenMarkers, worldCamera);
        selectionHudFeedbackSystem.ResetViewCache();
        selectionOrderMarkerSystem.Initialize(
            runtimeConfig.MoveOrderMarkerPrefab,
            runtimeConfig.AttackOrderMarkerPrefab,
            runtimeConfig.AttackTargetMarkerPrefab,
            tryResolveRuntimeBuildingInstance,
            runtimeConfig.OrderMarkerVisibleSeconds,
            runtimeUiRoot);

        return new Result(
            BindSelectionMainMenu,
            BindMatchHudSelectionPanel,
            UpdateSelectionRuntimePhases,
            selectionOrderMarkerSystem.Dispose,
            selectionUiCommand,
            selectionUiReadModel,
            selectionUiCamera,
            selectionBuildingInteraction,
            selectionScreenMarkers,
            EnsureSelectionRectangleView(runtimeUiRoot, rtsSelectionConfig),
            ShouldBlockBuildingSelectionClick);

        bool ShouldBlockBuildingSelectionClick()
        {
            return explicitAttackTargetModeActive ||
                   rtsSelectionInputSystem.HasActiveWorldTargetCommandMode(out _);
        }

        void BindSelectionMainMenu(MainMenuPlayUI mainMenu)
        {
            mainMenuPlayUi = mainMenu;
            roadBuildReadState = roadBuildReadModel;
            buildingPlacementInteractionSystem = buildingInteraction;
            buildingPlacementInteractionContext = buildingInteractionContext;
            mainMenuPlayUi?.ConfigureMatchHudSelectionPanelBinding(BindMatchHudSelectionPanel);
            mainMenuPlayUi?.ConfigureMatchHudRuntimeFeedbackBinding(BindBattleHudRuntimeFeedback);
            mainMenuPlayUi?.ConfigureMatchHudSquadTrayBinding(BindMatchHudSquadTray);
        }

        void BindMatchHudSelectionPanel(MatchHudSelectionPanelView view)
        {
            matchHudSelectionPanelView = view;
            selectionHudFeedbackSystem.BindMatchHudSelectionPanel(view);
            selectionBuildingInteraction.BindMatchHudSelectionPanel(view);
            view?.BindActions(
                () => selectionUiCommand.RequestReturnToBase(),
                () => selectionUiCommand.RequestDestroyFocusedUnit(),
                RequestBoardTargetModeFromPanel);
            view?.BindTransportPassengerActions(
                () => { },
                () => { },
                () => selectionUiCommand.RequestFocusedTransportDisembark(),
                passenger => selectionUiCommand.RequestFocusedTransportPassengerDisembark(passenger));
        }

        void BindBattleHudRuntimeFeedback(BattleHudRuntimeFeedbackView view)
        {
            selectionHudFeedbackSystem.BindBattleHudRuntimeFeedback(view);
        }

        void RequestBoardTargetModeFromPanel()
        {
            if (selectionUiCommand.RequestBoardTargetMode())
                return;

            selectionHudFeedbackSystem.ApplyCommandResult(
                CreateHudFeedbackContext(),
                TacticalCommandResult.Rejected(TacticalCommandReasonCode.CommandUnavailable, "Board command unavailable."));
        }

        void BindMatchHudSquadTray(MatchHudSquadTrayView view)
        {
            matchHudSquadTrayView = view;
            if (view == null)
                return;

            view.Bind(slot =>
            {
                selectionUiCommand.CaptureUiClickSequence();
                runtimeGameplayStateSystem.SuppressNextWorldClick = true;
                matchHudSquadTraySelectionSystem.SelectSlot(
                    CreateSquadTraySelectionContext(),
                    view,
                    slot);
            });
        }

        void UpdateSelectionRuntimePhases()
        {
            if (rtsSelectionInputSystem.HasPendingTransportCommandRequests())
                ProcessTransportCommandRequests();
            if (rtsSelectionInputSystem.HasPendingExternalSelectionCommandRequests())
                rtsSelectionFocusCommandSystem.ProcessExternalSelectionCommandRequests(CreateFocusCommandContext());
            rtsSelectionRuntimeInputSystem.ProcessQueuedMoveOrder(CreateRuntimeInputContext());
            RefreshFocusedSelectionReadModels();
            UpdateMatchHudSelectionPanel();
            rtsSelectionCommandResultFlushSystem.UpdateOrderMarkerVisibility(CreateCommandResultFlushContext());
            UpdateAttackTargetPreviewMarkers();
            UpdateBoardTargetPreviewMarkers();

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
                SetExplicitAttackTargetModeActive,
                () => rtsCameraSystem.IsDragging,
                value => rtsSelectionRuntimeCameraSystem.SetCameraDragging(CreateRuntimeCameraContext(), value),
                pointerPosition => IsPointerOverUI(pointerPosition, out _),
                pointerPosition => IsPointerOverGameplayUi(pointerPosition, out _),
                TryIssueAttackOrderToClickedUnit,
                TryIssueScanOrder,
                selectionOrderMarkerSystem,
                TryGetDefaultEntityManager,
                TryGetClickedCell,
                visible => selectionHudFeedbackSystem.SetWorldMarkersVisible(CreateHudFeedbackContext(), visible),
                TryIssueBoardTransportOrderToClickedUnit,
                TryIssueBoardSelectedTransportOrderToClickedUnit,
                TryIssueBoardSelectedTransportOrdersToPassengerRect,
                IsBoardSelectedTransportPassengerTarget,
                QueueFocusUnitCommand,
                screenDelta => rtsSelectionRuntimeCameraSystem.PanCamera(CreateRuntimeCameraContext(), screenDelta),
                IssueMoveOrder,
                ProcessSelectionRectangleRequests,
                ClearSelectionCommandMode,
                selectionRuntimeDiagnosticsSystem.LogSelectionClickDiagnostic,
                BuildClickDebugSummary,
                IsMatchIntroGameplayInputLocked);
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
                selectionScanCommandRequestSystem,
                selectionTransportCommandRequestSystem,
                selectedMoveOrderCommandSystem,
                attackOrderCommandSystem,
                scanIntelCommandSystem,
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
                TryGetClickedCell,
                TryGetClickedAttackTargetEntity,
                CollectSelectedAttackSources,
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
                SetExplicitAttackTargetModeActive,
                selectionRuntimeDiagnosticsSystem.LogSelectionClickDiagnostic,
                DescribeTransportBoardingEntity,
                ValidateControllableEntity,
                (em, entity) => em.Exists(entity) && unitTransportBoardingQuerySystem.IsSoldierBoardingCandidate(em, entity),
                (em, entity) => em.Exists(entity) && IsBoardCommandAvailable(em, entity) && unitTransportBoardingQuerySystem.IsBoardablePlayerTransport(em, entity),
                IssueHoldPositionOrder,
                IssueStopOrder,
                DestroyFocusedUnit,
                ReturnFocusedSelectionToBase,
                BoardFocusedTransport,
                TryFocusUnitDirect,
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
                SetExplicitAttackTargetModeActive,
                selectionHudFeedbackSystem,
                CreateHudFeedbackContext(),
                ClearCurrentSelection,
                RequestMoveOrderScreenMarker,
                SetCameraDragging,
                ProcessAttackCommandRequests,
                ProcessScanCommandRequests,
                ProcessTransportCommandRequests,
                ProcessMoveCommandRequests,
                selectionRuntimeDiagnosticsSystem.LogSelectionClickDiagnostic,
                DescribeTransportBoardingEntity);
        }

        SelectionHudFeedbackSystem.Context CreateHudFeedbackContext()
        {
            return new SelectionHudFeedbackSystem.Context(
                selectionUiQuerySystem,
                TryGetDefaultEntityManager,
                resolveSelectionPortraitSprite);
        }

        MatchHudSquadTraySelectionSystem.Context CreateSquadTraySelectionContext()
        {
            return new MatchHudSquadTraySelectionSystem.Context(
                runtimeConfig.WorldCamera,
                TryGetDefaultEntityManager,
                EnsureRuntimeSelectionDependencies,
                ClearCurrentSelection,
                () => buildingPlacementInteractionSystem?.ClearSelectedBuilding(
                    buildingPlacementInteractionContext,
                    "MatchHudSquadTray"),
                (entityManager, entity) => selectionHudFeedbackSystem.ApplySelection(CreateHudFeedbackContext(), entityManager, entity),
                selectedCount => selectionHudFeedbackSystem.ApplySquadSelection(CreateHudFeedbackContext(), selectedCount),
                selectionRuntimeDiagnosticsSystem.LogSelectionClickDiagnostic,
                selectionStateSystem,
                focusedUnitLifecycleSystem);
        }

        void UpdateAttackTargetPreviewMarkers()
        {
            if (!explicitAttackTargetModeActive)
            {
                selectionOrderMarkerSystem.UpdateAttackTargetPreviewMarkers(default, false);
                return;
            }

            if (!TryGetDefaultEntityManager(out EntityManager em))
                return;

            EnsureRuntimeSelectionDependencies(em);
            selectionOrderMarkerSystem.UpdateAttackTargetPreviewMarkers(em, true);
        }

        void UpdateBoardTargetPreviewMarkers()
        {
            if (!rtsSelectionInputSystem.TryGetActiveBoardCommandMode(out BoardCommandModeDirection direction, out Entity transport))
            {
                if (!explicitAttackTargetModeActive)
                    selectionOrderMarkerSystem.UpdateBoardTargetPreviewMarkers(default, false, Entity.Null, null);
                return;
            }

            if (!TryGetDefaultEntityManager(out EntityManager em))
                return;

            EnsureRuntimeSelectionDependencies(em);
            if (direction == BoardCommandModeDirection.PassengerToTransport)
            {
                selectionOrderMarkerSystem.UpdateBoardTargetPreviewMarkers(em, true, Entity.Null, IsValidBoardTransportPreviewTarget);
                return;
            }

            if (direction == BoardCommandModeDirection.TransportToPassenger)
            {
                selectionOrderMarkerSystem.UpdateBoardTargetPreviewMarkers(
                    em,
                    true,
                    transport,
                    IsValidBoardPassengerPreviewTarget);
                return;
            }

            selectionOrderMarkerSystem.UpdateBoardTargetPreviewMarkers(default, false, Entity.Null, null);
        }

        bool IsValidBoardTransportPreviewTarget(EntityManager em, Entity source, Entity target)
        {
            return IsBoardTransportWithAvailableSeats(em, target);
        }

        bool IsValidBoardPassengerPreviewTarget(EntityManager em, Entity transport, Entity passenger)
        {
            if (transport == Entity.Null ||
                passenger == Entity.Null ||
                transport == passenger ||
                !IsBoardCommandAvailable(em, transport))
            {
                return false;
            }

            return unitTransportBoardingQuerySystem.IsSoldierBoardingCandidate(em, passenger);
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

        bool IsMatchIntroGameplayInputLocked()
        {
            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
                return false;

            if (cachedMatchIntroWorld != world || !hasMatchIntroLockQuery)
            {
                cachedMatchIntroWorld = world;
                matchIntroLockQuery = world.EntityManager.CreateEntityQuery(
                    ComponentType.ReadOnly<UiShellBoundaryComponent>(),
                    ComponentType.ReadOnly<MatchIntroTransitionComponent>());
                hasMatchIntroLockQuery = true;
            }

            if (matchIntroLockQuery.IsEmptyIgnoreFilter)
                return false;

            MatchIntroTransitionComponent matchIntro =
                world.EntityManager.GetComponentData<MatchIntroTransitionComponent>(matchIntroLockQuery.GetSingletonEntity());
            return matchIntro.InputLocked != 0;
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

        void UpdateMatchHudSelectionPanel()
        {
            if (matchHudSelectionPanelView == null)
                return;

            if (!TryGetDefaultEntityManager(out EntityManager em))
            {
                matchHudSelectionPanelView.Apply(MatchHudSelectionPanelView.Model.Hidden);
                return;
            }

            EnsureRuntimeSelectionDependencies(em);
            int selectedCount = CountSelectedTags(em);
            if (selectedCount > 1)
            {
                matchHudSelectionPanelView.Apply(BuildSquadPanelModel(em, selectedCount));
                matchHudSelectionPanelView.ApplyTransportPassengers(MatchHudSelectionPanelView.TransportPassengersModel.Hidden);
                return;
            }

            if (focusedUnitLifecycleSystem.TryGetFocusedUnitEntity(em, selectionStateSystem, out Entity focusedUnit) &&
                em.Exists(focusedUnit))
            {
                matchHudSelectionPanelView.Apply(BuildFocusedUnitPanelModel(em, focusedUnit));
                matchHudSelectionPanelView.ApplyTransportPassengers(BuildTransportPassengersPanelModel(em, focusedUnit));
                return;
            }

            if (selectedCount > 0)
            {
                matchHudSelectionPanelView.Apply(BuildSquadPanelModel(em, selectedCount));
                matchHudSelectionPanelView.ApplyTransportPassengers(MatchHudSelectionPanelView.TransportPassengersModel.Hidden);
                return;
            }

            if (buildingPlacementInteractionSystem != null &&
                buildingPlacementInteractionSystem.HasSelectedBuilding(buildingPlacementInteractionContext))
            {
                matchHudSelectionPanelView.Apply(BuildSelectedBuildingPanelModel());
                matchHudSelectionPanelView.ApplyTransportPassengers(MatchHudSelectionPanelView.TransportPassengersModel.Hidden);
                return;
            }

            matchHudSelectionPanelView.Apply(MatchHudSelectionPanelView.Model.Hidden);
            matchHudSelectionPanelView.ApplyTransportPassengers(MatchHudSelectionPanelView.TransportPassengersModel.Hidden);
        }

        MatchHudSelectionPanelView.Model BuildFocusedUnitPanelModel(EntityManager em, Entity entity)
        {
            Sprite portraitSprite = resolveSelectionPortraitSprite?.Invoke(em, entity);
            portraitSprite ??= matchHudSelectionPanelView.ResolveFallbackPortraitSprite(SelectionSummaryPortraitKind.GenericSquad);
            bool owned = selectionUiQuerySystem.IsOwnedByPlayer(em, entity);
            bool movable = em.HasComponent<UnitMove>(entity);
            bool vehicle = selectionUiQuerySystem.IsVehicleForVisibleSelection(em, entity);
            TryGetHealthModel(em, entity, out string healthLabel, out float health01);
            string orderText = ResolveFocusedUnitOrderText(em, entity);
            if (TryGetAttackModeOrderSnapshot(out string attackModeOrderText))
                orderText = attackModeOrderText;

            return new MatchHudSelectionPanelView.Model(
                true,
                selectionUiQuerySystem.ResolveFocusedUnitName(em, entity),
                selectionUiQuerySystem.ResolveFocusedUnitDescription(em, entity),
                orderText,
                healthLabel,
                health01,
                portraitSprite,
                !vehicle,
                null,
                owned && movable && !em.HasComponent<UnitTransportPassenger>(entity),
                owned,
                IsBoardCommandAvailable(em, entity));
        }

        MatchHudSelectionPanelView.TransportPassengersModel BuildTransportPassengersPanelModel(EntityManager em, Entity transport)
        {
            transportPassengerPanelItems.Clear();
            if (!em.Exists(transport) ||
                !selectionUiQuerySystem.IsOwnedByPlayer(em, transport) ||
                !unitTransportCapacitySystem.TryEnsureTransportCapacity(em, transport) ||
                !em.HasComponent<UnitTransportCapacity>(transport) ||
                !em.HasBuffer<UnitTransportPassengerElement>(transport))
            {
                return MatchHudSelectionPanelView.TransportPassengersModel.Hidden;
            }

            int capacity = math.max(0, em.GetComponentData<UnitTransportCapacity>(transport).SoldierCapacity);
            if (capacity <= 0)
                return MatchHudSelectionPanelView.TransportPassengersModel.Hidden;

            DynamicBuffer<UnitTransportPassengerElement> passengers = em.GetBuffer<UnitTransportPassengerElement>(transport);
            for (int i = 0; i < passengers.Length; i++)
            {
                Entity passenger = passengers[i].Passenger;
                if (!em.Exists(passenger))
                    continue;

                TryGetHealthModel(em, passenger, out string healthLabel, out float health01);
                Sprite portrait = resolveSelectionCardPortraitSprite?.Invoke(em, passenger);
                portrait ??= resolveSelectionPortraitSprite?.Invoke(em, passenger);
                portrait ??= matchHudSelectionPanelView.ResolveFallbackPortraitSprite(SelectionSummaryPortraitKind.Soldiers);
                transportPassengerPanelItems.Add(new MatchHudSelectionPanelView.PassengerItemModel(
                    passenger,
                    selectionUiQuerySystem.ResolveFocusedUnitName(em, passenger),
                    ResolvePassengerRoleText(em, passenger),
                    healthLabel,
                    health01,
                    portrait,
                    true));
            }

            return new MatchHudSelectionPanelView.TransportPassengersModel(
                true,
                false,
                transport,
                transportPassengerPanelItems.Count,
                capacity,
                transportPassengerPanelItems.Count > 0,
                transportPassengerPanelItems);
        }

        string ResolvePassengerRoleText(EntityManager em, Entity passenger)
        {
            if (!em.Exists(passenger))
                return "UNIT";

            if (selectionUiQuerySystem.IsVehicleForVisibleSelection(em, passenger))
                return "VEHICLE";

            return "SOLDIER";
        }

        MatchHudSelectionPanelView.Model BuildSquadPanelModel(EntityManager em, int selectedCount)
        {
            bool includeSelectedBuilding = buildingPlacementInteractionSystem != null &&
                                           buildingPlacementInteractionSystem.HasSelectedBuilding(buildingPlacementInteractionContext);
            SelectionSummaryQuerySystem.Summary summary = selectionSummaryQuerySystem.BuildSelectedSummary(
                em,
                selectionUiQuerySystem,
                includeSelectedBuilding);
            string orderText = TryGetAttackModeOrderSnapshot(out string attackModeOrderText)
                ? attackModeOrderText
                : summary.OrderText;
            Sprite portraitSprite = matchHudSelectionPanelView.ResolveFallbackPortraitSprite(summary.PortraitKind);
            portraitSprite ??= ResolveActiveSquadTrayPortraitSprite();
            portraitSprite ??= matchHudSelectionPanelView.ResolveFallbackPortraitSprite(SelectionSummaryPortraitKind.GenericSquad);
            return new MatchHudSelectionPanelView.Model(
                true,
                summary.Title,
                summary.Subtitle,
                orderText,
                summary.HealthText,
                summary.Health01,
                portraitSprite,
                summary.PortraitKind,
                false,
                null,
                selectedCount > 0,
                selectedCount > 0,
                HasSelectedBoardPassenger(em) || TryGetSelectedBoardTransport(em, out _));
        }

        Sprite ResolveActiveSquadTrayPortraitSprite()
        {
            if (matchHudSquadTrayView == null)
                return null;

            return matchHudSquadTrayView.TryGetPortraitSprite(matchHudSquadTraySelectionSystem.ActiveSlot, out Sprite sprite)
                ? sprite
                : null;
        }

        MatchHudSelectionPanelView.Model BuildSelectedBuildingPanelModel()
        {
            string label = buildingPlacementInteractionSystem.SelectedBuildingLabel(buildingPlacementInteractionContext);
            Sprite portraitSprite = resolveSelectedBuildingPortraitSprite?.Invoke();
            portraitSprite ??= matchHudSelectionPanelView.ResolveFallbackPortraitSprite(SelectionSummaryPortraitKind.Buildings);
            return new MatchHudSelectionPanelView.Model(
                true,
                string.IsNullOrWhiteSpace(label) ? "Selected Building" : label,
                "Base structure",
                "Structure selected",
                "Health: -",
                0f,
                portraitSprite,
                false,
                null,
                false,
                true,
                false);
        }

        string ResolveFocusedUnitOrderText(EntityManager em, Entity entity)
        {
            if (em.HasComponent<UnitTransportPassenger>(entity))
                return "In transport";
            if (em.HasComponent<UnitTransportBoardingTarget>(entity))
                return "Boarding transport";

            return selectionUiQuerySystem.GetFocusedUnitUiStatus(em, entity) switch
            {
                SelectionUiQuerySystem.FocusedUnitUiStatus.ReturningToBase => "Returning to base",
                SelectionUiQuerySystem.FocusedUnitUiStatus.MissileLaunched => "Missile launched",
                SelectionUiQuerySystem.FocusedUnitUiStatus.AirspaceClear => "Airspace clear",
                SelectionUiQuerySystem.FocusedUnitUiStatus.TrackingAirTarget => "Tracking air target",
                SelectionUiQuerySystem.FocusedUnitUiStatus.InterceptingMissile => "Intercepting missile",
                SelectionUiQuerySystem.FocusedUnitUiStatus.AirDefenseReloading => "Reloading",
                SelectionUiQuerySystem.FocusedUnitUiStatus.Engaged => "Engaging target",
                SelectionUiQuerySystem.FocusedUnitUiStatus.Moving => "Moving",
                _ => "Idle"
            };
        }

        bool TryGetAttackModeOrderSnapshot(out string orderText)
        {
            orderText = attackModeOrderSnapshotText;
            return explicitAttackTargetModeActive &&
                   attackModeOrderSnapshotActive &&
                   !string.IsNullOrWhiteSpace(orderText);
        }

        void SetExplicitAttackTargetModeActive(bool active)
        {
            if (active)
            {
                if (!explicitAttackTargetModeActive)
                    CaptureAttackModeOrderSnapshot();
            }
            else
            {
                ClearAttackModeOrderSnapshot();
            }

            explicitAttackTargetModeActive = active;
        }

        void CaptureAttackModeOrderSnapshot()
        {
            attackModeOrderSnapshotText = ResolveCurrentSelectionOrderTextSnapshot();
            attackModeOrderSnapshotActive = true;
        }

        void ClearAttackModeOrderSnapshot()
        {
            attackModeOrderSnapshotActive = false;
            attackModeOrderSnapshotText = string.Empty;
        }

        string ResolveCurrentSelectionOrderTextSnapshot()
        {
            if (!TryGetDefaultEntityManager(out EntityManager em))
                return "Idle";

            EnsureRuntimeSelectionDependencies(em);
            if (focusedUnitLifecycleSystem.TryGetFocusedUnitEntity(em, selectionStateSystem, out Entity focusedUnit) &&
                em.Exists(focusedUnit))
            {
                return ResolveFocusedUnitOrderText(em, focusedUnit);
            }

            int selectedCount = CountSelectedTags(em);
            if (selectedCount > 0)
            {
                bool includeSelectedBuilding = buildingPlacementInteractionSystem != null &&
                                               buildingPlacementInteractionSystem.HasSelectedBuilding(buildingPlacementInteractionContext);
                return selectionSummaryQuerySystem.BuildSelectedSummary(
                    em,
                    selectionUiQuerySystem,
                    includeSelectedBuilding).OrderText;
            }

            if (buildingPlacementInteractionSystem != null &&
                buildingPlacementInteractionSystem.HasSelectedBuilding(buildingPlacementInteractionContext))
            {
                return "Structure selected";
            }

            return "Idle";
        }

        void TryGetHealthModel(EntityManager em, Entity entity, out string healthLabel, out float health01)
        {
            if (!selectionUiQuerySystem.TryGetFocusedUnitHealth(em, entity, out int current, out int max) || max <= 0)
            {
                healthLabel = "Health: -";
                health01 = 0f;
                return;
            }

            healthLabel = $"Health: {math.max(0, current)}/{max}";
            health01 = math.saturate((float)current / max);
        }

        bool IsBoardCommandAvailable(EntityManager em, Entity entity)
        {
            if (!selectionUiQuerySystem.IsOwnedByPlayer(em, entity))
                return false;

            if (unitTransportBoardingQuerySystem.IsSoldierBoardingCandidate(em, entity))
                return true;

            return IsBoardTransportWithAvailableSeats(em, entity);
        }

        bool IsBoardTransportWithAvailableSeats(EntityManager em, Entity entity)
        {
            if (!selectionUiQuerySystem.IsOwnedByPlayer(em, entity))
                return false;

            if (!unitTransportBoardingQuerySystem.IsBoardablePlayerTransport(em, entity))
                return false;

            int capacity = em.GetComponentData<UnitTransportCapacity>(entity).SoldierCapacity;
            int passengers = em.HasBuffer<UnitTransportPassengerElement>(entity)
                ? em.GetBuffer<UnitTransportPassengerElement>(entity).Length
                : 0;
            return capacity > passengers + CountPendingBoardingOrders(em, entity);
        }

        bool HasSelectedBoardPassenger(EntityManager em)
        {
            using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<SelectedUnitTag>());
            if (query.IsEmptyIgnoreFilter)
                return false;

            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                if (em.Exists(entity) && unitTransportBoardingQuerySystem.IsSoldierBoardingCandidate(em, entity))
                    return true;
            }

            return false;
        }

        bool TryGetSelectedBoardTransport(EntityManager em, out Entity transport)
        {
            transport = Entity.Null;
            if (focusedUnitLifecycleSystem.TryGetFocusedUnitEntity(em, selectionStateSystem, out Entity focusedUnit) &&
                em.Exists(focusedUnit) &&
                unitTransportBoardingQuerySystem.IsBoardablePlayerTransport(em, focusedUnit) &&
                IsBoardCommandAvailable(em, focusedUnit))
            {
                transport = focusedUnit;
                return true;
            }

            using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<SelectedUnitTag>());
            if (query.IsEmptyIgnoreFilter)
                return false;

            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                if (!em.Exists(entity) ||
                    !unitTransportBoardingQuerySystem.IsBoardablePlayerTransport(em, entity) ||
                    !IsBoardCommandAvailable(em, entity))
                {
                    continue;
                }

                transport = entity;
                return true;
            }

            return false;
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
                ClearSelectedBuildingAfterRectangleSelection,
                screenRect => trySelectFirstBuildingInScreenRect != null &&
                    trySelectFirstBuildingInScreenRect(screenRect));
        }

        void ClearSelectedBuildingAfterRectangleSelection()
        {
            buildingPlacementInteractionSystem?.ClearSelectedBuilding(buildingPlacementInteractionContext, "RTSSelection.SelectUnitsInRectangle");
        }

        void ClearSelectionCommandMode()
        {
            rtsSelectionInputSystem.ClearActiveCommandMode();
            selectionHudFeedbackSystem.ClearCommandMode(CreateHudFeedbackContext());
        }

        void ClearCurrentSelection(EntityManager em, string reason = "Unspecified")
        {
            matchHudSquadTraySelectionSystem.ClearActiveSlot(matchHudSquadTrayView);
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

        bool ProcessScanCommandRequests()
        {
            return rtsSelectionCommandResultFlushSystem.ProcessScanCommandRequests(CreateCommandResultFlushContext());
        }

        bool ProcessTransportCommandRequests()
        {
            return rtsSelectionCommandResultFlushSystem.ProcessTransportCommandRequests(CreateCommandResultFlushContext());
        }

        bool TryIssueBoardTransportOrderToClickedUnit(Vector2 screenPosition)
        {
            return rtsSelectionPointerTargetCommandSystem.TryIssueBoardTransportOrderToClickedUnit(CreatePointerTargetCommandContext(), screenPosition);
        }

        bool TryIssueBoardSelectedTransportOrderToClickedUnit(Entity transport, Vector2 screenPosition)
        {
            if (!rtsSelectionInputSystem.QueueBoardSelectedTransportCommandRequest(transport, screenPosition, Time.frameCount))
                return false;

            return ProcessTransportCommandRequests();
        }

        bool TryIssueBoardSelectedTransportOrdersToPassengerRect(Entity transport, Rect screenRect)
        {
            if (!TryGetDefaultEntityManager(out EntityManager em))
                return false;

            EnsureRuntimeSelectionDependencies(em);
            visibleUnitSelectionSystem.CollectVisiblePlayerUnits(
                em,
                runtimeConfig.WorldCamera,
                selectionUiQuerySystem,
                screenRect,
                VisibleUnitSelectionSystem.Filter.Soldiers,
                visibleSelectionScratch);

            int queued = 0;
            for (int i = 0; i < visibleSelectionScratch.Count; i++)
            {
                Entity passenger = visibleSelectionScratch[i];
                if (!IsValidBoardPassengerPreviewTarget(em, transport, passenger))
                    continue;

                if (rtsSelectionInputSystem.QueueBoardSelectedTransportPassengerCommandRequest(transport, passenger, screenRect, Time.frameCount))
                    queued++;
            }

            if (queued <= 0)
            {
                selectionHudFeedbackSystem.ApplyCommandResult(
                    CreateHudFeedbackContext(),
                    TacticalCommandResult.Rejected(TacticalCommandReasonCode.CommandUnavailable, "Tap units to board."));
                return false;
            }

            return ProcessTransportCommandRequests();
        }

        bool IsBoardSelectedTransportPassengerTarget(Entity transport, Vector2 screenPosition)
        {
            if (!TryGetDefaultEntityManager(out EntityManager em))
                return false;

            return TryGetClickedUnitEntity(screenPosition, em, out Entity passenger) &&
                   IsValidBoardPassengerPreviewTarget(em, transport, passenger);
        }

        bool QueueFocusUnitCommand(Vector2 screenPosition)
        {
            if (!rtsSelectionInputSystem.QueueFocusUnitCommandRequest(screenPosition, Time.frameCount))
            {
                selectionRuntimeDiagnosticsSystem.LogSelectionClickDiagnostic($"focusCommandEnqueue result=False pos={screenPosition} frame={Time.frameCount}");
                return false;
            }

            bool processed = rtsSelectionFocusCommandSystem.ProcessExternalSelectionCommandRequests(CreateFocusCommandContext());
            selectionRuntimeDiagnosticsSystem.LogSelectionClickDiagnostic($"focusCommandProcessed result={processed} pos={screenPosition} frame={Time.frameCount}");
            return processed;
        }

        bool TryFocusUnitDirect(Vector2 screenPosition)
        {
            return rtsSelectionPointerTargetCommandSystem.TryFocusUnit(CreatePointerTargetCommandContext(), screenPosition);
        }

        bool TryIssueAttackOrderToClickedUnit(Vector2 screenPosition)
        {
            return rtsSelectionPointerTargetCommandSystem.TryIssueAttackOrderToClickedUnit(CreatePointerTargetCommandContext(), screenPosition);
        }

        bool TryIssueScanOrder(Vector2 screenPosition)
        {
            return rtsSelectionPointerTargetCommandSystem.TryIssueScanOrder(CreatePointerTargetCommandContext(), screenPosition);
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

        bool TryGetClickedAttackTargetEntity(Vector2 screenPosition, EntityManager em, out Entity bestEntity)
        {
            return rtsSelectionPointerTargetCommandSystem.TryGetClickedAttackTargetEntity(
                CreatePointerTargetCommandContext(),
                screenPosition,
                em,
                out bestEntity);
        }

        void CollectSelectedAttackSources(EntityManager em, List<Entity> sources)
        {
            if (sources == null || em.World == null || !em.World.IsCreated)
                return;

            EnsureRuntimeSelectionDependencies(em);
            TryAddAttackSource(em, selectionStateSystem.FocusedUnit, sources);

            List<Entity> cached = selectionStateSystem.CachedSelectedMoveEntities;
            for (int i = 0; i < cached.Count; i++)
                TryAddAttackSource(em, cached[i], sources);

            EntityQuery selectedTagQuery = selectionRuntimeQuerySystem.SelectedTagQuery;
            if (selectedTagQuery.IsEmptyIgnoreFilter)
                return;

            using NativeArray<Entity> selectedEntities = selectedTagQuery.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < selectedEntities.Length; i++)
                TryAddAttackSource(em, selectedEntities[i], sources);
        }

        bool TryAddAttackSource(EntityManager em, Entity entity, List<Entity> sources)
        {
            if (entity == Entity.Null ||
                !em.Exists(entity) ||
                sources.Contains(entity) ||
                em.HasComponent<Disabled>(entity) ||
                em.HasComponent<UnitTransportPassenger>(entity) ||
                !em.HasComponent<UnitAttack>(entity) ||
                !em.HasComponent<LocalTransform>(entity))
            {
                return false;
            }

            if (!unitTargetOrderSystem.ValidateAttackSource(em, entity).Accepted)
                return false;

            sources.Add(entity);
            return true;
        }

        string BuildClickDebugSummary(Vector2 screenPosition)
        {
            if (!TryGetDefaultEntityManager(out EntityManager em))
                return "world=missing";

            EnsureRuntimeSelectionDependencies(em);
            string clickedCell = TryGetClickedCell(screenPosition, em, out int2 cell, out Vector3 worldPoint)
                ? $"{cell}@{worldPoint.x:F1},{worldPoint.y:F1},{worldPoint.z:F1}"
                : "none";
            string focused = DescribeClickDebugEntity(em, selectionStateSystem.FocusedUnit);
            List<Entity> cached = selectionStateSystem.CachedSelectedMoveEntities;
            string selected0 = cached.Count > 0 ? DescribeClickDebugEntity(em, cached[0]) : "none";
            int selectedTagCount = CountSelectedTags(em);
            return $"clickedCell={clickedCell} focused={focused} cachedCount={cached.Count} selectedTags={selectedTagCount} selected0={selected0} suppress={runtimeGameplayStateSystem.SuppressNextWorldClick} ignoreUntil={rtsSelectionInputSystem.IgnoreWorldCommandsUntilFrame}";
        }

        int CountSelectedTags(EntityManager em)
        {
            using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<SelectedUnitTag>());
            return query.CalculateEntityCount();
        }

        string DescribeClickDebugEntity(EntityManager em, Entity entity)
        {
            if (entity == Entity.Null || !em.Exists(entity))
                return "null";

            string source = em.HasComponent<UnitSourcePrefabKey>(entity)
                ? em.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString()
                : em.GetName(entity);
            byte faction = em.HasComponent<Faction>(entity) ? em.GetComponentData<Faction>(entity).Id : (byte)0;
            string grid = em.HasComponent<UnitGrid>(entity) ? em.GetComponentData<UnitGrid>(entity).Cell.ToString() : "none";
            string target = em.HasComponent<UnitTarget>(entity) ? em.GetComponentData<UnitTarget>(entity).Cell.ToString() : "none";
            string pathRequest = em.HasComponent<UnitPathRequest>(entity) ? em.GetComponentData<UnitPathRequest>(entity).Goal.ToString() : "none";
            bool selected = em.HasComponent<SelectedUnitTag>(entity);
            bool pathFollow = em.HasComponent<UnitPathFollow>(entity);
            bool manual = em.HasComponent<ManualMoveOrderTag>(entity);
            bool engage = em.HasComponent<EngageTarget>(entity);
            return $"{entity}/{source}/faction={faction}/selected={selected}/grid={grid}/target={target}/pathRequest={pathRequest}/pathFollow={pathFollow}/manual={manual}/engage={engage}";
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

        void DestroyFocusedUnit()
        {
            if (!TryGetFocusedUnitEntity(out EntityManager em, out Entity entity))
            {
                int selectedDestroyed = DestroySelectedUnits(em);
                if (selectedDestroyed > 0)
                {
                    focusedUnitLifecycleSystem.ClearFocusedUnit(selectionStateSystem);
                    selectionHudFeedbackSystem.ClearSelection(CreateHudFeedbackContext());
                    selectionHudFeedbackSystem.ApplyCommandResult(
                        CreateHudFeedbackContext(),
                        TacticalCommandResult.Success(selectedDestroyed == 1 ? "Destroyed selected unit." : $"Destroyed {selectedDestroyed} selected units."));
                    return;
                }

                if (buildingPlacementInteractionSystem != null &&
                    buildingPlacementInteractionSystem.HasSelectedBuilding(buildingPlacementInteractionContext))
                {
                    buildingPlacementInteractionSystem.DeleteSelectedBuilding(buildingPlacementInteractionContext);
                    selectionHudFeedbackSystem.ClearSelection(CreateHudFeedbackContext());
                    selectionHudFeedbackSystem.ApplyCommandResult(CreateHudFeedbackContext(), TacticalCommandResult.Success("Destroyed selected building."));
                    return;
                }

                selectionHudFeedbackSystem.ApplyCommandResult(CreateHudFeedbackContext(), TacticalCommandResult.Rejected(TacticalCommandReasonCode.NoSelection));
                return;
            }

            if (!FocusedUnitOwnedByPlayer())
            {
                selectionHudFeedbackSystem.ApplyCommandResult(CreateHudFeedbackContext(), TacticalCommandResult.Rejected(TacticalCommandReasonCode.TargetNotAttackable));
                return;
            }

            focusedUnitCommandSystem.DestroyFocusedUnit(em, entity);
            focusedUnitLifecycleSystem.ClearFocusedUnit(selectionStateSystem);
            selectionHudFeedbackSystem.ClearSelection(CreateHudFeedbackContext());
            selectionHudFeedbackSystem.ApplyCommandResult(CreateHudFeedbackContext(), TacticalCommandResult.Success("Destroyed selected unit."));
        }

        int DestroySelectedUnits(EntityManager em)
        {
            if (em.World == null || !em.World.IsCreated)
                return 0;

            int destroyed = 0;
            using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<SelectedUnitTag>());
            if (query.IsEmptyIgnoreFilter)
                return 0;

            using NativeArray<Entity> selectedEntities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < selectedEntities.Length; i++)
            {
                Entity selectedEntity = selectedEntities[i];
                if (!em.Exists(selectedEntity) || !selectionUiQuerySystem.IsOwnedByPlayer(em, selectedEntity))
                    continue;

                focusedUnitCommandSystem.DestroyFocusedUnit(em, selectedEntity);
                destroyed++;
            }

            return destroyed;
        }

        void ReturnFocusedSelectionToBase()
        {
            if (!TryGetDefaultEntityManager(out EntityManager em))
            {
                selectionHudFeedbackSystem.ApplyCommandResult(CreateHudFeedbackContext(), TacticalCommandResult.Rejected(TacticalCommandReasonCode.NoSelection));
                return;
            }

            EnsureRuntimeSelectionDependencies(em);
            int issued = 0;
            if (focusedUnitLifecycleSystem.TryGetFocusedUnitEntity(em, selectionStateSystem, out Entity focusedUnit) &&
                em.Exists(focusedUnit) &&
                selectionUiQuerySystem.IsOwnedByPlayer(em, focusedUnit))
            {
                issued += focusedUnitCommandSystem.ReturnFocusedUnitToBase(em, focusedUnit, unitMoveOrderSystem) ? 1 : 0;
            }
            else
            {
                using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<SelectedUnitTag>());
                if (!query.IsEmptyIgnoreFilter)
                {
                    using NativeArray<Entity> selectedEntities = query.ToEntityArray(Allocator.Temp);
                    for (int i = 0; i < selectedEntities.Length; i++)
                    {
                        Entity entity = selectedEntities[i];
                        if (!em.Exists(entity) || !selectionUiQuerySystem.IsOwnedByPlayer(em, entity))
                            continue;

                        issued += focusedUnitCommandSystem.ReturnFocusedUnitToBase(em, entity, unitMoveOrderSystem) ? 1 : 0;
                    }
                }
            }

            SetExplicitAttackTargetModeActive(false);
            rtsSelectionInputSystem.ClearActiveCommandMode();
            runtimeGameplayStateSystem.SuppressNextWorldClick = true;
            selectionHudFeedbackSystem.SetWorldMarkersVisible(CreateHudFeedbackContext(), false);
            selectionHudFeedbackSystem.ApplyCommandResult(
                CreateHudFeedbackContext(),
                issued > 0
                    ? TacticalCommandResult.Success(issued == 1 ? "Unit returning to base." : $"{issued} units returning to base.")
                    : TacticalCommandResult.Rejected(TacticalCommandReasonCode.NoSelection));
        }

        void BoardFocusedTransport()
        {
            if (!TryResolveBoardTransport(out EntityManager em, out Entity transport))
            {
                selectionHudFeedbackSystem.ApplyCommandResult(
                    CreateHudFeedbackContext(),
                    TacticalCommandResult.Rejected(TacticalCommandReasonCode.CommandUnavailable, "Select a transport vehicle or aircraft first."));
                return;
            }

            if (!TryIssueFocusedTransportBoarding(em, transport, out int orderedCount))
            {
                selectionHudFeedbackSystem.ApplyCommandResult(
                    CreateHudFeedbackContext(),
                    TacticalCommandResult.Rejected(TacticalCommandReasonCode.CommandUnavailable, "No nearby soldiers can board this transport."));
                return;
            }

            SetExplicitAttackTargetModeActive(false);
            rtsSelectionInputSystem.ClearActiveCommandMode();
            runtimeGameplayStateSystem.SelectionModeActive = false;
            runtimeGameplayStateSystem.SuppressNextWorldClick = true;
            SetCameraDragging(false);
            selectionHudFeedbackSystem.SetWorldMarkersVisible(CreateHudFeedbackContext(), false);
            selectionHudFeedbackSystem.ClearCommandMode(CreateHudFeedbackContext());
            selectionHudFeedbackSystem.ApplyCommandResult(CreateHudFeedbackContext(), TacticalCommandResult.Success($"Boarding {orderedCount} unit{(orderedCount == 1 ? string.Empty : "s")}."));
        }

        bool TryResolveBoardTransport(out EntityManager em, out Entity transport)
        {
            transport = Entity.Null;
            if (!TryGetDefaultEntityManager(out em))
                return false;

            if (focusedUnitLifecycleSystem.TryGetFocusedUnitEntity(em, selectionStateSystem, out Entity focusedUnit) &&
                IsBoardCommandAvailable(em, focusedUnit))
            {
                transport = focusedUnit;
                return true;
            }

            using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<SelectedUnitTag>());
            if (query.IsEmptyIgnoreFilter)
                return false;

            using NativeArray<Entity> selectedEntities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < selectedEntities.Length; i++)
            {
                Entity selected = selectedEntities[i];
                if (!em.Exists(selected) || !IsBoardCommandAvailable(em, selected))
                    continue;

                transport = selected;
                return true;
            }

            return false;
        }

        bool TryIssueFocusedTransportBoarding(EntityManager em, Entity transport, out int orderedCount)
        {
            orderedCount = 0;
            if (!unitTransportBoardingQuerySystem.IsBoardablePlayerTransport(em, transport))
                return false;

            bool transportLanded = unitTransportBoardingRuleSystem.IsTransportLandedForBoarding(em, transport);
            if (!transportLanded || !unitTransportCapacitySystem.TryEnsureTransportCapacity(em, transport))
                return false;

            int capacity = math.max(0, em.GetComponentData<UnitTransportCapacity>(transport).SoldierCapacity);
            int occupiedSeats = em.GetBuffer<UnitTransportPassengerElement>(transport).Length + CountPendingBoardingOrders(em, transport);
            int availableSeats = capacity - occupiedSeats;
            if (availableSeats <= 0)
                return false;

            using EntityQuery gridQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<GridConfig>(),
                ComponentType.ReadOnly<GridWalkable>(),
                ComponentType.ReadOnly<DynamicBlockerComponent>(),
                ComponentType.ReadOnly<DynamicOccupancyComponent>());
            if (gridQuery.IsEmptyIgnoreFilter)
                return false;

            Entity gridEntity = gridQuery.GetSingletonEntity();
            GridConfig grid = em.GetComponentData<GridConfig>(gridEntity);
            NativeArray<GridWalkable> walkable = em.GetBuffer<GridWalkable>(gridEntity).AsNativeArray();
            DynamicBlockerComponent blockerData = em.GetComponentData<DynamicBlockerComponent>(gridEntity);
            NativeBitArray blocked = blockerData.Blocked;
            NativeArray<byte> friendlyPassFactionIds = blockerData.FriendlyPassFactionIds;
            NativeBitArray occupied = em.GetComponentData<DynamicOccupancyComponent>(gridEntity).Occupied;

            using EntityQuery liveQuery = em.CreateEntityQuery(new EntityQueryDesc
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
            using NativeArray<Entity> liveUnitEntities = liveQuery.ToEntityArray(Allocator.Temp);
            using NativeArray<UnitGrid> liveUnitGrids = liveQuery.ToComponentDataArray<UnitGrid>(Allocator.Temp);
            using NativeArray<UnitFootprint> liveUnitFootprints = liveQuery.ToComponentDataArray<UnitFootprint>(Allocator.Temp);

            List<Entity> candidates = CollectNearestBoardingCandidates(em, transport);
            if (candidates.Count == 0)
                return false;

            int2 transportCell = em.GetComponentData<UnitGrid>(transport).Cell;
            int2 transportSize = em.GetComponentData<UnitFootprint>(transport).Size;
            int2 boardingTransportSize = em.HasComponent<UnitAirMovement>(transport) ? new int2(1, 1) : transportSize;
            int directBoardingCells = unitTransportBoardingRuleSystem.GetTransportBoardingDirectCells(em, transport);
            var reservedBoardingCells = new HashSet<int>();
            var plannedOrders = new List<TransportBoardingOrder>(math.min(candidates.Count, availableSeats));

            for (int i = 0; i < candidates.Count && plannedOrders.Count < availableSeats; i++)
            {
                Entity passenger = candidates[i];
                if (!em.Exists(passenger) || !unitTransportBoardingQuerySystem.IsSoldierBoardingCandidate(em, passenger))
                    continue;

                int2 referenceCell = em.GetComponentData<UnitGrid>(passenger).Cell;
                int2 passengerFootprint = em.GetComponentData<UnitFootprint>(passenger).Size;
                byte passengerFaction = em.GetComponentData<Faction>(passenger).Id;
                if (!unitTransportApproachCellSystem.TryFindTransportApproachCell(
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
                        transportCell,
                        transportSize,
                        reservedBoardingCells,
                        directBoardingCells,
                        passengerFaction,
                        out int2 goal))
                {
                    continue;
                }

                unitTransportApproachCellSystem.ReserveFootprintCells(grid, goal, passengerFootprint, reservedBoardingCells);
                plannedOrders.Add(new TransportBoardingOrder(passenger, goal));
            }

            for (int i = 0; i < plannedOrders.Count; i++)
            {
                TransportBoardingOrder order = plannedOrders[i];
                Entity passenger = order.Passenger;
                if (!em.Exists(passenger) || !unitTransportBoardingQuerySystem.IsSoldierBoardingCandidate(em, passenger))
                    continue;

                unitMoveOrderSystem.ClearMovementOrderComponents(em, passenger);
                if (!em.HasBuffer<UnitTransportHiddenVisualScale>(passenger))
                    em.AddBuffer<UnitTransportHiddenVisualScale>(passenger);
                unitMoveOrderSystem.IssueImmediateMoveCommand(em, passenger, order.Goal);
                if (em.HasComponent<UnitTransportBoardingTarget>(passenger))
                    em.SetComponentData(passenger, new UnitTransportBoardingTarget { Transport = transport, Goal = order.Goal });
                else
                    em.AddComponentData(passenger, new UnitTransportBoardingTarget { Transport = transport, Goal = order.Goal });
                orderedCount++;
            }

            return orderedCount > 0;
        }

        List<Entity> CollectNearestBoardingCandidates(EntityManager em, Entity transport)
        {
            var candidates = new List<Entity>();
            int2 transportCell = em.GetComponentData<UnitGrid>(transport).Cell;
            using EntityQuery query = em.CreateEntityQuery(
                ComponentType.ReadOnly<Faction>(),
                ComponentType.ReadOnly<UnitGrid>(),
                ComponentType.ReadOnly<UnitMove>(),
                ComponentType.ReadOnly<UnitFootprint>(),
                ComponentType.ReadOnly<UnitMovementBehavior>());
            if (query.IsEmptyIgnoreFilter)
                return candidates;

            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                if (entity == transport ||
                    !unitTransportBoardingQuerySystem.IsSoldierBoardingCandidate(em, entity) ||
                    em.HasComponent<UnitTransportBoardingTarget>(entity))
                {
                    continue;
                }

                candidates.Add(entity);
            }

            candidates.Sort((left, right) =>
            {
                int2 leftCell = em.GetComponentData<UnitGrid>(left).Cell;
                int2 rightCell = em.GetComponentData<UnitGrid>(right).Cell;
                int leftScore = math.abs(leftCell.x - transportCell.x) + math.abs(leftCell.y - transportCell.y);
                int rightScore = math.abs(rightCell.x - transportCell.x) + math.abs(rightCell.y - transportCell.y);
                return leftScore.CompareTo(rightScore);
            });
            return candidates;
        }

        int CountPendingBoardingOrders(EntityManager em, Entity transport)
        {
            int count = 0;
            using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<UnitTransportBoardingTarget>());
            if (query.IsEmptyIgnoreFilter)
                return 0;

            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
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
            SetExplicitAttackTargetModeActive(false);
            SetCameraDragging(false);
            selectionHudFeedbackSystem.ApplyCommandResult(CreateHudFeedbackContext(), TacticalCommandResult.Success());
            selectionHudFeedbackSystem.ClearCommandMode(CreateHudFeedbackContext());
            selectionHudFeedbackSystem.SetWorldMarkersVisible(CreateHudFeedbackContext(), true);
            selectionHudFeedbackSystem.ApplySelection(CreateHudFeedbackContext(), em, launcher);
            return true;
        }

        bool ArmFocusedAttackTargetMode()
        {
            if (!TryGetDefaultEntityManager(out EntityManager em))
            {
                selectionHudFeedbackSystem.ApplyCommandResult(CreateHudFeedbackContext(), TacticalCommandResult.Rejected(
                    TacticalCommandReasonCode.NoSelection));
                return false;
            }

            selectedAttackSourceScratch.Clear();
            CollectSelectedAttackSources(em, selectedAttackSourceScratch);
            if (selectedAttackSourceScratch.Count == 0)
            {
                selectionHudFeedbackSystem.ApplyCommandResult(CreateHudFeedbackContext(), TacticalCommandResult.Rejected(
                    HasAnySelectionForAttackMode(em) ? TacticalCommandReasonCode.TargetNotAttackable : TacticalCommandReasonCode.NoSelection));
                return false;
            }

            SetExplicitAttackTargetModeActive(true);
            selectionHudFeedbackSystem.ApplyCommandMode(CreateHudFeedbackContext(), TacticalCommandMode.Attack);
            selectionHudFeedbackSystem.SetWorldMarkersVisible(CreateHudFeedbackContext(), true);
            runtimeGameplayStateSystem.SelectionModeActive = false;
            runtimeGameplayStateSystem.SuppressNextWorldClick = true;
            rtsSelectionInputSystem.IsDraggingSelection = false;
            SetCameraDragging(false);
            rtsSelectionInputSystem.SkipNextWorldReleaseAfterSelection = true;
            return true;
        }

        bool HasAnySelectionForAttackMode(EntityManager em)
        {
            if (focusedUnitLifecycleSystem.TryGetFocusedUnitEntity(em, selectionStateSystem, out _))
                return true;
            if (selectionStateSystem.CachedSelectedMoveEntities.Count > 0)
                return true;
            if (buildingPlacementInteractionSystem != null &&
                buildingPlacementInteractionSystem.HasSelectedBuilding(buildingPlacementInteractionContext))
            {
                return true;
            }

            EnsureRuntimeSelectionDependencies(em);
            return !selectionRuntimeQuerySystem.SelectedTagQuery.IsEmptyIgnoreFilter;
        }

        void CancelExplicitAttackTargetMode()
        {
            SetExplicitAttackTargetModeActive(false);
            selectionHudFeedbackSystem.ClearCommandMode(CreateHudFeedbackContext());
        }

        void IssueHoldPositionOrder()
        {
            IssueImmediateSelectedUnitOrder(TacticalCommandMode.Hold, clearEngageTarget: true, holdPosition: true);
        }

        void IssueStopOrder()
        {
            IssueImmediateSelectedUnitOrder(TacticalCommandMode.Stop, clearEngageTarget: true, holdPosition: false);
        }

        bool IssueImmediateSelectedUnitOrder(TacticalCommandMode mode, bool clearEngageTarget, bool holdPosition)
        {
            selectionHudFeedbackSystem.ApplyCommandMode(CreateHudFeedbackContext(), mode);

            if (!TryGetDefaultEntityManager(out EntityManager em))
            {
                selectionHudFeedbackSystem.ApplyCommandResult(CreateHudFeedbackContext(), TacticalCommandResult.Rejected(TacticalCommandReasonCode.NoSelection));
                selectionHudFeedbackSystem.ClearCommandMode(CreateHudFeedbackContext());
                return false;
            }

            bool issued = focusedUnitCommandSystem.IssueImmediateSelectedUnitOrder(
                em,
                clearEngageTarget,
                holdPosition,
                unitMoveOrderSystem);
            if (!issued)
            {
                selectionHudFeedbackSystem.ApplyCommandResult(CreateHudFeedbackContext(), TacticalCommandResult.Rejected(TacticalCommandReasonCode.NoSelection));
                selectionHudFeedbackSystem.ClearCommandMode(CreateHudFeedbackContext());
                return false;
            }

            SetExplicitAttackTargetModeActive(false);
            rtsSelectionInputSystem.ClearActiveCommandMode();
            rtsSelectionInputSystem.ClearQueuedMoveOrder();
            rtsSelectionInputSystem.ClearPendingMoveCommandRequests();
            runtimeGameplayStateSystem.SelectionModeActive = false;
            runtimeGameplayStateSystem.SuppressNextWorldClick = true;
            rtsSelectionInputSystem.IsDraggingSelection = false;
            buildingPlacementInteractionSystem?.ExitBuildMode(buildingPlacementInteractionContext);
            buildingPlacementInteractionSystem?.CancelBuildingPlacement(buildingPlacementInteractionContext);
            buildingPlacementInteractionSystem?.ClearSelectedBuilding(
                buildingPlacementInteractionContext,
                $"SelectionUiCommandSystem.{mode}");
            SetCameraDragging(false);
            selectionHudFeedbackSystem.SetWorldMarkersVisible(CreateHudFeedbackContext(), false);
            selectionHudFeedbackSystem.ClearCommandMode(CreateHudFeedbackContext());
            selectionHudFeedbackSystem.ApplyCommandResult(
                CreateHudFeedbackContext(),
                TacticalCommandResult.Success(holdPosition ? "Holding current position." : "Stopped selected units."));
            focusedUnitLifecycleSystem.RefreshFocusedUnit(
                em,
                selectionStateSystem,
                (entityManager, entity) => selectionHudFeedbackSystem.ApplySelection(CreateHudFeedbackContext(), entityManager, entity));
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
            if (mainMenuPlayUi != null &&
                mainMenuPlayUi.IsPointerOverAnyGameplayUi(screenPosition, out source))
                return true;

            return IsPointerOverUI(screenPosition, out source);
        }
    }

    private static bool IsPointerOverUI(Vector2 screenPosition, out string source)
    {
        source = null;
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
            return false;

        var pointerData = new PointerEventData(eventSystem)
        {
            position = screenPosition
        };
        var results = new List<RaycastResult>();
        eventSystem.RaycastAll(pointerData, results);

        for (int i = 0; i < results.Count; i++)
        {
            RaycastResult result = results[i];
            if (result.gameObject == null || !result.gameObject.activeInHierarchy)
                continue;

            if (result.module is not UnityEngine.UI.GraphicRaycaster)
                continue;

            source = result.gameObject.name;
            return true;
        }

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
