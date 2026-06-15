using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

internal sealed class SelectionGameplayStartupSystem
{
    public readonly struct Result
    {
        public readonly System.Action<IMatchRuntimeUi> BindSelectionMainMenu;
        public readonly System.Action<IMatchHudSelectionPanelView> BindMatchHudSelectionPanel;
        public readonly System.Action SelectionRuntimeUpdate;
        public readonly System.Action DisposeSelection;
        public readonly SelectionUiCommandSystem SelectionUiCommand;
        public readonly SelectionUiReadModelSystem SelectionUiReadModel;
        public readonly SelectionUiCameraSystem SelectionUiCamera;
        public readonly SelectionBuildingInteractionSystem SelectionBuildingInteraction;
        public readonly SelectionScreenMarkerSystem SelectionScreenMarkers;
        public readonly ISelectionRectangleView SelectionRectangleView;
        public readonly System.Func<bool> ShouldBlockBuildingSelectionClick;

        public Result(
            System.Action<IMatchRuntimeUi> bindSelectionMainMenu,
            System.Action<IMatchHudSelectionPanelView> bindMatchHudSelectionPanel,
            System.Action selectionRuntimeUpdate,
            System.Action disposeSelection,
            SelectionUiCommandSystem selectionUiCommand,
            SelectionUiReadModelSystem selectionUiReadModel,
            SelectionUiCameraSystem selectionUiCamera,
            SelectionBuildingInteractionSystem selectionBuildingInteraction,
            SelectionScreenMarkerSystem selectionScreenMarkers,
            ISelectionRectangleView selectionRectangleView,
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
        System.Func<Transform, RTSSelectionSystemConfig, ISelectionRectangleView> createSelectionRectangleView,
        RoadBuildReadModelSystem roadBuildReadModel,
        BuildingPlacementInteractionSystem buildingInteraction,
        BuildingPlacementInteractionSystem.Context buildingInteractionContext,
        System.Func<Rect, bool> trySelectFirstBuildingInScreenRect,
        SelectionHudFeedbackBoundary.ResolveSelectionPortraitSpriteDelegate resolveSelectionPortraitSprite,
        SelectionHudFeedbackBoundary.ResolveSelectionPortraitSpriteDelegate resolveSelectionCardPortraitSprite,
        System.Func<Sprite> resolveSelectedBuildingPortraitSprite,
        SelectionOrderMarkerSystem.TryResolveRuntimeBuildingInstanceDelegate tryResolveRuntimeBuildingInstance,
        FactionVisualSettings factionVisuals,
        IMatchIntroStateQuery matchIntroStateQuery)
    {
        IMatchIntroStateQuery resolvedMatchIntroStateQuery = matchIntroStateQuery ?? NullMatchIntroStateQuery.Instance;
        SelectionRuntimeDiagnosticsSystem selectionRuntimeDiagnosticsSystem = ResolveSelectionRuntimeDiagnosticsSystem();
        SelectionRuntimeConfigSystem selectionRuntimeConfigSystem = ResolveSelectionRuntimeConfigSystem();
        var selectionRuntimeQuerySystem = new SelectionRuntimeQuerySystem();
        SelectionRuntimeConfigSystem.State runtimeConfig = selectionRuntimeConfigSystem != null
            ? selectionRuntimeConfigSystem.CreateState(rtsSelectionConfig, worldCamera)
            : SelectionRuntimeConfigSystem.CreateStateFromConfig(rtsSelectionConfig, worldCamera);
        var runtimeGameplayStateSystem = new RuntimeGameplayStateSystem();
        var rtsSelectionInputSystem = new RtsSelectionInputSystem();
        var rtsSelectionRuntimeInputSystem = new RtsSelectionRuntimeInputSystem();
        var rtsSelectionRuntimeInputContextSystem = new RtsSelectionRuntimeInputContextSystem();
        RtsSelectionRuntimeCameraSystem rtsSelectionRuntimeCameraSystem = ResolveRtsSelectionRuntimeCameraSystem();
        var rtsSelectionRuntimeCameraContextSystem = new RtsSelectionRuntimeCameraContextSystem();
        var rtsSelectionCommandResultFlushSystem = new RtsSelectionCommandResultFlushSystem();
        var rtsSelectionCommandResultContextSystem = new RtsSelectionCommandResultContextSystem();
        var rtsSelectionFocusCommandSystem = new RtsSelectionFocusCommandSystem();
        var rtsSelectionFocusCommandContextSystem = new RtsSelectionFocusCommandContextSystem();
        var rtsSelectionPointerTargetCommandSystem = new RtsSelectionPointerTargetCommandSystem();
        var rtsSelectionPointerTargetCommandContextSystem = new RtsSelectionPointerTargetCommandContextSystem();
        RtsCameraSystem rtsCameraSystem = ResolveRtsCameraSystem();
        RtsCameraRequestSystem rtsCameraRequestSystem = ResolveRtsCameraRequestSystem();
        var selectionUiCommand = new SelectionUiCommandSystem(IsMatchIntroGameplayInputLocked);
        var selectionUiReadModel = new SelectionUiReadModelSystem();
        var selectionUiCamera = new SelectionUiCameraSystem(rtsCameraSystem, rtsCameraRequestSystem);
        SelectionScreenMarkerSystem selectionScreenMarkers = ResolveSelectionScreenMarkerSystem();
        var selectionStateSystem = new SelectionStateSystem();
        var selectionUiQuerySystem = new SelectionUiQuerySystem();
        var selectionSummaryQuerySystem = new SelectionSummaryQuerySystem();
        var focusedUnitUiReadModelSystem = new FocusedUnitUiReadModelSystem();
        var visibleUnitSelectionSystem = new VisibleUnitSelectionSystem();
        var selectionRectangleRequestSystem = new SelectionRectangleRequestSystem();
        var unitMoveOrderSystem = new UnitMoveOrderSystem();
        var selectedMoveOrderCommandSystem = new SelectedMoveOrderCommandSystem();
        var attackOrderCommandSystem = new AttackOrderCommandSystem();
        var scanIntelCommandSystem = new ScanIntelCommandSystem();
        var selectionOrderMarkerSystem = new SelectionOrderMarkerSystem();
        var selectionHudFeedbackSystem = new SelectionHudFeedbackBoundary();
        var focusedUnitCommandSystem = new FocusedUnitCommandSystem();
        var focusedUnitLifecycleSystem = new FocusedUnitLifecycleSystem();
        var selectedUnitOrderSnapshotSystem = new SelectedUnitOrderSnapshotSystem();
        var buildingTargetMoveOrderSystem = new BuildingTargetMoveOrderSystem();
        var transportBoardingCommandSystem = new TransportBoardingCommandSystem();
        var focusableUnitLookupSystem = new FocusableUnitLookupSystem();
        var matchHudSquadTraySelectionSystem = new MatchHudSquadTraySelectionSystem();
        var unitTransportCapacitySystem = new UnitTransportCapacitySystem();
        var unitTransportAirPickupSystem = new UnitTransportAirPickupSystem();
        var selectionBuildingInteraction = new SelectionBuildingInteractionSystem();
        var visibleSelectionScratch = new List<Entity>();
        var transportPassengerPanelItems = new List<MatchHudSelectionPanelPassengerItemModel>();
        IMatchRuntimeUi mainMenuPlayUi = null;
        IMatchHudSquadTrayView matchHudSquadTrayView = null;
        RtsSelectionRuntimeInputSystem.Context runtimeInputContext = default;
        bool hasRuntimeInputContext = false;
        RtsSelectionRuntimeCameraSystem.Context runtimeCameraContext = default;
        bool hasRuntimeCameraContext = false;
        RtsSelectionCommandResultFlushSystem.Context commandResultFlushContext = default;
        bool hasCommandResultFlushContext = false;
        System.Action<EntityManager, Entity> applyHudSelectionAction = ApplyHudSelection;
        System.Action<int> applyHudSquadSelectionAction = ApplyHudSquadSelection;
        SelectionRectangleRequestSystem.ApplyHudSelectionAction applyRectangleHudSelectionAction = ApplyHudSelection;
        SelectionRectangleRequestSystem.ApplyHudSquadSelectionAction applyRectangleHudSquadSelectionAction = ApplyHudSquadSelection;
        System.Action clearHudSelectionAction = ClearHudSelection;
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
            createSelectionRectangleView?.Invoke(runtimeUiRoot, rtsSelectionConfig),
            ShouldBlockBuildingSelectionClick);

        bool ShouldBlockBuildingSelectionClick()
        {
            return explicitAttackTargetModeActive ||
                   rtsSelectionInputSystem.HasActiveWorldTargetCommandMode(out _);
        }

        void BindSelectionMainMenu(IMatchRuntimeUi mainMenu)
        {
            mainMenuPlayUi = mainMenu;
            roadBuildReadState = roadBuildReadModel;
            buildingPlacementInteractionSystem = buildingInteraction;
            buildingPlacementInteractionContext = buildingInteractionContext;
            hasRuntimeInputContext = false;
            hasRuntimeCameraContext = false;
            hasCommandResultFlushContext = false;
            mainMenuPlayUi?.ConfigureMatchHudSelectionPanelBinding(BindMatchHudSelectionPanel);
            mainMenuPlayUi?.ConfigureMatchHudRuntimeFeedbackBinding(BindBattleHudRuntimeFeedback);
            mainMenuPlayUi?.ConfigureMatchHudSquadTrayBinding(BindMatchHudSquadTray);
        }

        void BindMatchHudSelectionPanel(IMatchHudSelectionPanelView view)
        {
            selectionHudFeedbackSystem.BindMatchHudSelectionPanel(view);
            selectionBuildingInteraction.BindMatchHudSelectionPanel(view);
            hasCommandResultFlushContext = false;
            view?.BindActions(
                () => selectionUiCommand.RequestReturnToBase(),
                () => selectionUiCommand.RequestDestroyFocusedUnit(),
                RequestBoardTargetModeFromPanel);
            view?.BindTransportPassengerActions(
                () => { },
                () => { },
                () => selectionUiCommand.RequestFocusedTransportDisembark(),
                passenger => selectionUiCommand.RequestFocusedTransportPassengerDisembark(ToEntity(passenger)));
        }

        void BindBattleHudRuntimeFeedback(IBattleHudRuntimeFeedbackView view)
        {
            selectionHudFeedbackSystem.BindBattleHudRuntimeFeedback(view);
            hasCommandResultFlushContext = false;
        }

        void RequestBoardTargetModeFromPanel()
        {
            if (selectionUiCommand.RequestBoardTargetMode())
                return;

            selectionHudFeedbackSystem.ApplyCommandResult(
                CreateHudFeedbackContext(),
                TacticalCommandResult.Rejected(TacticalCommandReasonCode.CommandUnavailable, "Board command unavailable."));
        }

        void BindMatchHudSquadTray(IMatchHudSquadTrayView view)
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
                rtsSelectionCommandResultFlushSystem.ProcessTransportCommandRequests(GetCommandResultFlushContext());
            if (rtsSelectionInputSystem.HasPendingMoveCommandRequestsOrResults())
                rtsSelectionCommandResultFlushSystem.ProcessMoveCommandRequests(GetCommandResultFlushContext());
            if (rtsSelectionInputSystem.HasPendingAttackCommandRequestsOrResults())
                rtsSelectionCommandResultFlushSystem.ProcessAttackCommandRequests(
                    GetCommandResultFlushContext(),
                    explicitAttackTargetModeActive);
            if (rtsSelectionInputSystem.HasPendingScanCommandRequestsOrResults())
                rtsSelectionCommandResultFlushSystem.ProcessScanCommandRequests(GetCommandResultFlushContext());
            rtsSelectionCommandResultFlushSystem.ProcessSelectionModeCommandRequests(
                GetCommandResultFlushContext(),
                Time.frameCount);
            rtsSelectionCommandResultFlushSystem.ProcessMoveTargetModeCommandRequests(
                GetCommandResultFlushContext(),
                Time.frameCount);
            if (TryGetDefaultEntityManager(out EntityManager attackTargetModeEntityManager))
            {
                Entity focusedUnit = focusedUnitLifecycleSystem.TryGetFocusedUnitEntity(
                    attackTargetModeEntityManager,
                    selectionStateSystem,
                    out Entity resolvedFocusedUnit)
                    ? resolvedFocusedUnit
                    : Entity.Null;
                if (!RtsSelectionAttackTargetModeCommandSystem.HasPendingToggleAttackTargetModeRequest(attackTargetModeEntityManager) ||
                    !rtsSelectionCommandResultFlushSystem.ProcessFocusedMissileLauncherRadarAttack(
                        GetCommandResultFlushContext(),
                        focusedUnit))
                {
                    rtsSelectionCommandResultFlushSystem.ProcessAttackTargetModeCommandRequests(
                        GetCommandResultFlushContext(),
                        Time.frameCount,
                        focusedUnit);
                }
            }
            rtsSelectionCommandResultFlushSystem.ProcessScanTargetModeCommandRequests(
                GetCommandResultFlushContext(),
                Time.frameCount);
            rtsSelectionCommandResultFlushSystem.ProcessBoardTargetModeCommandRequests(
                GetCommandResultFlushContext(),
                Time.frameCount);
            rtsSelectionCommandResultFlushSystem.ProcessCancelActiveCommandModeRequests(GetCommandResultFlushContext());
            rtsSelectionCommandResultFlushSystem.ProcessImmediateSelectedUnitCommandRequests(
                GetCommandResultFlushContext(),
                selectionStateSystem.FocusedUnit);
            if (runtimeConfig.WorldCamera != null)
                rtsSelectionCommandResultFlushSystem.ProcessSelectAllCommandRequests(GetCommandResultFlushContext());
            rtsSelectionCommandResultFlushSystem.ProcessDeselectAllCommandRequests(GetCommandResultFlushContext());
            if (rtsSelectionInputSystem.HasPendingExternalSelectionCommandRequests())
                rtsSelectionFocusCommandSystem.ProcessExternalSelectionCommandRequests(CreateFocusCommandContext());
            RtsSelectionRuntimeInputSystem.Context inputContext = GetRuntimeInputContext();
            rtsSelectionRuntimeInputSystem.ProcessQueuedMoveOrder(inputContext);
            selectionHudFeedbackSystem.RefreshFocusedSelectionReadModels(
                CreateHudFeedbackContext(),
                selectionStateSystem,
                focusedUnitUiReadModelSystem,
                unitTransportCapacitySystem,
                EnsureRuntimeSelectionDependencies,
                (em, state) => focusedUnitLifecycleSystem.RefreshFocusedUnit(
                    em,
                    state,
                    applyHudSelectionAction),
                Time.time);
            selectionHudFeedbackSystem.UpdateMatchHudSelectionPanel(
                CreateHudFeedbackContext(),
                selectionStateSystem,
                focusedUnitLifecycleSystem,
                focusedUnitUiReadModelSystem,
                selectionSummaryQuerySystem,
                transportPassengerPanelItems,
                EnsureRuntimeSelectionDependencies,
                TryGetAttackModeOrderSnapshot,
                resolveSelectionCardPortraitSprite,
                resolveSelectedBuildingPortraitSprite,
                ResolveActiveSquadTrayPortraitSprite,
                () => buildingPlacementInteractionSystem != null &&
                      buildingPlacementInteractionSystem.HasSelectedBuilding(buildingPlacementInteractionContext),
                () => buildingPlacementInteractionSystem != null
                    ? buildingPlacementInteractionSystem.SelectedBuildingLabel(buildingPlacementInteractionContext)
                    : string.Empty,
                (em, entity) => rtsSelectionPointerTargetCommandSystem.IsBoardCommandAvailable(
                    CreatePointerTargetCommandContext(),
                    em,
                    entity),
                em => rtsSelectionPointerTargetCommandSystem.HasSelectedBoardAction(
                    CreatePointerTargetCommandContext(),
                    em));
            rtsSelectionCommandResultFlushSystem.UpdateOrderMarkerVisibility(GetCommandResultFlushContext());
            rtsSelectionCommandResultFlushSystem.UpdateCommandPreviewMarkers(
                GetCommandResultFlushContext(),
                explicitAttackTargetModeActive,
                (em, source, target) => rtsSelectionPointerTargetCommandSystem.IsValidBoardTransportPreviewTarget(
                    CreatePointerTargetCommandContext(),
                    em,
                    source,
                    target),
                (em, source, target) => rtsSelectionPointerTargetCommandSystem.IsValidBoardPassengerPreviewTarget(
                    CreatePointerTargetCommandContext(),
                    em,
                    source,
                    target));

            RtsSelectionRuntimeCameraSystem.Context cameraContext = GetRuntimeCameraContext();
            if (rtsSelectionRuntimeCameraSystem != null &&
                rtsSelectionRuntimeCameraSystem.UpdateRuntimeCameraTick(cameraContext))
            {
                rtsSelectionRuntimeInputSystem.UpdateNormalPointerInput(inputContext);
            }
        }

        RtsSelectionRuntimeInputSystem.Context GetRuntimeInputContext()
        {
            if (!hasRuntimeInputContext)
            {
                runtimeInputContext = CreateRuntimeInputContext();
                hasRuntimeInputContext = true;
            }

            return runtimeInputContext;
        }

        RtsSelectionRuntimeCameraSystem.Context GetRuntimeCameraContext()
        {
            if (!hasRuntimeCameraContext)
            {
                runtimeCameraContext = CreateRuntimeCameraContext();
                hasRuntimeCameraContext = true;
            }

            return runtimeCameraContext;
        }

        RtsSelectionCommandResultFlushSystem.Context GetCommandResultFlushContext()
        {
            if (!hasCommandResultFlushContext)
            {
                commandResultFlushContext = CreateCommandResultFlushContext();
                hasCommandResultFlushContext = true;
            }

            return commandResultFlushContext;
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
                () => rtsCameraSystem != null && rtsCameraSystem.IsDragging,
                value => rtsSelectionRuntimeCameraSystem?.SetCameraDragging(GetRuntimeCameraContext(), value),
                pointerPosition => IsPointerOverRaycastableUi(pointerPosition, out _),
                pointerPosition => IsPointerOverGameplayUi(pointerPosition, out _),
                screenPosition => rtsSelectionPointerTargetCommandSystem.TryIssueAttackOrderToClickedUnit(
                    CreatePointerTargetCommandContext(),
                    screenPosition),
                screenPosition => rtsSelectionPointerTargetCommandSystem.TryIssueScanOrder(
                    CreatePointerTargetCommandContext(),
                    screenPosition),
                selectionOrderMarkerSystem,
                TryGetDefaultEntityManager,
                (Vector2 screenPosition, EntityManager em, out int2 cell, out Vector3 worldPoint) =>
                    rtsSelectionPointerTargetCommandSystem.TryGetClickedCell(
                        CreatePointerTargetCommandContext(),
                        screenPosition,
                        em,
                        out cell,
                        out worldPoint),
                visible => selectionHudFeedbackSystem.SetWorldMarkersVisible(CreateHudFeedbackContext(), visible),
                screenPosition => rtsSelectionPointerTargetCommandSystem.TryIssueBoardTransportOrderToClickedUnit(
                    CreatePointerTargetCommandContext(),
                    screenPosition),
                (transport, pointerPosition) => rtsSelectionPointerTargetCommandSystem.TryIssueBoardSelectedTransportOrderToClickedUnit(
                    CreatePointerTargetCommandContext(),
                    transport,
                    pointerPosition),
                (transport, screenRect) => rtsSelectionPointerTargetCommandSystem.TryIssueBoardSelectedTransportOrdersToPassengerRect(
                    CreatePointerTargetCommandContext(),
                    transport,
                    screenRect),
                (transport, pointerPosition) => rtsSelectionPointerTargetCommandSystem.IsBoardSelectedTransportPassengerTarget(
                    CreatePointerTargetCommandContext(),
                    transport,
                    pointerPosition),
                screenPosition => rtsSelectionFocusCommandSystem.QueueFocusUnitCommand(
                    CreateFocusCommandContext(),
                    screenPosition),
                screenDelta => rtsSelectionRuntimeCameraSystem?.PanCamera(GetRuntimeCameraContext(), screenDelta),
                screenPosition => rtsSelectionPointerTargetCommandSystem.IssueMoveOrder(
                    CreatePointerTargetCommandContext(),
                    screenPosition),
                ProcessSelectionRectangleRequests,
                () => selectionHudFeedbackSystem.ClearCommandMode(CreateHudFeedbackContext()),
                LogSelectionClickDiagnostic,
                pointerPosition => rtsSelectionPointerTargetCommandSystem.BuildClickDebugSummary(
                    CreatePointerTargetCommandContext(),
                    pointerPosition),
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
                resolvedMatchIntroStateQuery,
                IsPointerOverGameplayUi,
                pointerPosition => rtsSelectionInputSystem.UpdateLastKnownPointerPosition(pointerPosition),
                () => selectionScreenMarkers?.RequestHideOrderMarkers());
        }

        RtsSelectionCommandResultFlushSystem.Context CreateCommandResultFlushContext()
        {
            if (TryGetDefaultEntityManager(out EntityManager em))
                EnsureRuntimeSelectionDependencies(em);

            return rtsSelectionCommandResultContextSystem.Create(
                rtsSelectionInputSystem,
                selectionHudFeedbackSystem,
                CreateHudFeedbackContext(),
                selectionOrderMarkerSystem,
                selectedMoveOrderCommandSystem,
                attackOrderCommandSystem,
                scanIntelCommandSystem,
                transportBoardingCommandSystem,
                unitMoveOrderSystem,
                unitTransportCapacitySystem,
                unitTransportAirPickupSystem,
                selectionStateSystem,
                buildingPlacementInteractionSystem,
                buildingPlacementInteractionContext,
                selectionRuntimeQuerySystem,
                TryGetDefaultEntityManager,
                EnsureRuntimeSelectionDependencies,
                ClearCurrentSelection,
                SetExplicitAttackTargetModeActive,
                ProcessSelectionRectangleRequests,
                LogSelectionClickDiagnostic,
                screenPosition => selectionScreenMarkers?.RequestMoveOrderMarker(screenPosition),
                screenPosition => selectionScreenMarkers?.RequestAttackOrderMarker(screenPosition),
                value => rtsSelectionRuntimeCameraSystem?.SetCameraDragging(GetRuntimeCameraContext(), value),
                focusedUnitLifecycleSystem.ClearFocusedUnit,
                (em, state) => focusedUnitLifecycleSystem.RefreshFocusedUnit(
                    em,
                    state,
                    applyHudSelectionAction),
                focusedUnitLifecycleSystem.SetFocusedUnit,
                (Vector2 screenPosition, EntityManager entityManager, out Entity entity) =>
                    rtsSelectionPointerTargetCommandSystem.TryGetClickedUnitEntity(
                        CreatePointerTargetCommandContext(),
                        screenPosition,
                        entityManager,
                        out entity),
                (Vector2 screenPosition, EntityManager entityManager, out int2 cell, out Vector3 worldPoint) =>
                    rtsSelectionPointerTargetCommandSystem.TryGetMoveCommandCell(
                        CreatePointerTargetCommandContext(),
                        screenPosition,
                        entityManager,
                        out cell,
                        out worldPoint),
                (Vector2 screenPosition, EntityManager entityManager, out int2 cell, out Vector3 worldPoint) =>
                    rtsSelectionPointerTargetCommandSystem.TryGetClickedCell(
                        CreatePointerTargetCommandContext(),
                        screenPosition,
                        entityManager,
                        out cell,
                        out worldPoint),
                (Vector2 screenPosition, EntityManager entityManager, out Entity entity) =>
                    rtsSelectionPointerTargetCommandSystem.TryGetClickedAttackTargetEntity(
                        CreatePointerTargetCommandContext(),
                        screenPosition,
                        entityManager,
                        out entity),
                (Vector2 screenPosition, EntityManager entityManager, out Entity entity) =>
                    rtsSelectionPointerTargetCommandSystem.TryGetClickedUnitEntity(
                        CreatePointerTargetCommandContext(),
                        screenPosition,
                        entityManager,
                        out entity),
                (Vector2 screenPosition, EntityManager entityManager, out int2 cell, out Vector3 worldPoint) =>
                    rtsSelectionPointerTargetCommandSystem.TryGetClickedCell(
                        CreatePointerTargetCommandContext(),
                        screenPosition,
                        entityManager,
                        out cell,
                        out worldPoint));
        }

        RtsSelectionFocusCommandSystem.Context CreateFocusCommandContext()
        {
            return rtsSelectionFocusCommandContextSystem.Create(
                runtimeGameplayStateSystem,
                rtsSelectionInputSystem,
                selectionStateSystem,
                focusedUnitLifecycleSystem,
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
                value => rtsSelectionRuntimeCameraSystem?.SetCameraDragging(GetRuntimeCameraContext(), value),
                SetExplicitAttackTargetModeActive,
                LogSelectionClickDiagnostic,
                DescribeTransportBoardingEntity,
                screenPosition => rtsSelectionPointerTargetCommandSystem.TryFocusUnit(
                    CreatePointerTargetCommandContext(),
                    screenPosition));
        }

        RtsSelectionPointerTargetCommandSystem.Context CreatePointerTargetCommandContext()
        {
            return rtsSelectionPointerTargetCommandContextSystem.Create(
                runtimeGameplayStateSystem,
                rtsSelectionInputSystem,
                selectionStateSystem,
                focusedUnitLifecycleSystem,
                focusableUnitLookupSystem,
                selectionUiQuerySystem,
                visibleUnitSelectionSystem,
                transportBoardingCommandSystem,
                unitTransportCapacitySystem,
                unitTransportAirPickupSystem,
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
                screenPosition => selectionScreenMarkers?.RequestMoveOrderMarker(screenPosition),
                value => rtsSelectionRuntimeCameraSystem?.SetCameraDragging(GetRuntimeCameraContext(), value),
                () => rtsSelectionCommandResultFlushSystem.ProcessAttackCommandRequests(
                    GetCommandResultFlushContext(),
                    explicitAttackTargetModeActive),
                () => rtsSelectionCommandResultFlushSystem.ProcessScanCommandRequests(GetCommandResultFlushContext()),
                () => rtsSelectionCommandResultFlushSystem.ProcessTransportCommandRequests(GetCommandResultFlushContext()),
                () => rtsSelectionCommandResultFlushSystem.ProcessMoveCommandRequests(GetCommandResultFlushContext()),
                LogSelectionClickDiagnostic,
                DescribeTransportBoardingEntity,
                visibleSelectionScratch);
        }

        SelectionHudFeedbackBoundary.Context CreateHudFeedbackContext()
        {
            return new SelectionHudFeedbackBoundary.Context(
                selectionUiQuerySystem,
                TryGetDefaultEntityManager,
                resolveSelectionPortraitSprite);
        }

        void ApplyHudSelection(EntityManager entityManager, Entity entity)
        {
            selectionHudFeedbackSystem.ApplySelection(CreateHudFeedbackContext(), entityManager, entity);
        }

        void ApplyHudSquadSelection(int selectedCount)
        {
            selectionHudFeedbackSystem.ApplySquadSelection(CreateHudFeedbackContext(), selectedCount);
        }

        void ClearHudSelection()
        {
            selectionHudFeedbackSystem.ClearSelection(CreateHudFeedbackContext());
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
                applyHudSelectionAction,
                applyHudSquadSelectionAction,
                LogSelectionClickDiagnostic,
                selectionStateSystem,
                focusedUnitLifecycleSystem);
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
            return resolvedMatchIntroStateQuery.IsGameplayInputLocked();
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
            transportBoardingCommandSystem.EnsureEntityQueries(em);
        }

        void EnqueueSelectionDiagnostic(string message)
        {
            if (selectionRuntimeDiagnosticsSystem != null)
                selectionRuntimeDiagnosticsSystem.EnqueueSelectionDiagnostic(message);
            else
                SelectionRuntimeDiagnosticsSystem.EnqueueSelectionDiagnosticMessage(message);
        }

        void LogSelectionClickDiagnostic(string message)
        {
            if (selectionRuntimeDiagnosticsSystem != null)
                selectionRuntimeDiagnosticsSystem.LogSelectionClickDiagnostic(message);
            else
                SelectionRuntimeDiagnosticsSystem.LogSelectionClickDiagnosticMessage(message);
        }

        Sprite ResolveActiveSquadTrayPortraitSprite()
        {
            if (matchHudSquadTrayView == null)
                return null;

            return matchHudSquadTrayView.TryGetPortraitSprite(matchHudSquadTraySelectionSystem.ActiveSlot, out Sprite sprite)
                ? sprite
                : null;
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
            attackModeOrderSnapshotText = selectionHudFeedbackSystem.ResolveCurrentSelectionOrderTextSnapshot(
                CreateHudFeedbackContext(),
                selectionStateSystem,
                focusedUnitLifecycleSystem,
                selectionSummaryQuerySystem,
                EnsureRuntimeSelectionDependencies,
                () => buildingPlacementInteractionSystem != null &&
                      buildingPlacementInteractionSystem.HasSelectedBuilding(buildingPlacementInteractionContext));
            attackModeOrderSnapshotActive = true;
        }

        void ClearAttackModeOrderSnapshot()
        {
            attackModeOrderSnapshotActive = false;
            attackModeOrderSnapshotText = string.Empty;
        }

        static Entity ToEntity(UiEntityHandle handle)
        {
            return handle.IsNull
                ? Entity.Null
                : new Entity { Index = handle.Index, Version = handle.Version };
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
                applyRectangleHudSelectionAction,
                applyRectangleHudSquadSelectionAction,
                EnqueueSelectionDiagnostic,
                () => buildingPlacementInteractionSystem?.ClearSelectedBuilding(
                    buildingPlacementInteractionContext,
                    "RTSSelection.SelectUnitsInRectangle"),
                screenRect => trySelectFirstBuildingInScreenRect != null &&
                    trySelectFirstBuildingInScreenRect(screenRect));
        }

        void ClearCurrentSelection(EntityManager em, string reason = "Unspecified")
        {
            matchHudSquadTraySelectionSystem.ClearActiveSlot(matchHudSquadTrayView);
            focusedUnitLifecycleSystem.ClearCurrentSelection(
                em,
                selectionStateSystem,
                reason,
                EnqueueSelectionDiagnostic,
                clearHudSelectionAction);
        }

        void QueueSelectionRectangleRequest(
            Rect screenRect,
            RtsSelectionPointerRequestKind kind,
            VisibleUnitSelectionSystem.Filter filter = VisibleUnitSelectionSystem.Filter.All)
        {
            rtsSelectionInputSystem.QueueSelectionRectangleRequest(kind, screenRect, Time.frameCount, filter);
        }

        bool TryGetPointerPosition(out Vector2 pointerPosition)
        {
            if (GamePointerInput.TryGetPrimaryPointer(out GamePointerState pointer))
            {
                pointerPosition = pointer.Position;
                rtsSelectionInputSystem.UpdateLastKnownPointerPosition(pointerPosition);
                return true;
            }

            return rtsSelectionInputSystem.TryGetLastKnownPointerPosition(out pointerPosition);
        }

        bool IsPointerOverGameplayUi(Vector2 screenPosition, out string source)
        {
            if (mainMenuPlayUi != null &&
                mainMenuPlayUi.IsPointerOverAnyGameplayUi(screenPosition, out source))
            {
                SelectionRuntimeDiagnosticsSystem.LogMoveCommandTrace(
                    $"gameplayUiHit source={source} pos={screenPosition} frame={Time.frameCount}");
                return true;
            }

            return IsPointerOverRaycastableUi(screenPosition, out source);
        }

        bool IsPointerOverRaycastableUi(Vector2 screenPosition, out string source)
        {
            source = null;
            return mainMenuPlayUi != null &&
                   mainMenuPlayUi.IsPointerOverRaycastableUi(screenPosition, out source);
        }
    }

    private static RtsCameraRequestSystem ResolveRtsCameraRequestSystem()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        return world != null && world.IsCreated
            ? world.GetOrCreateSystemManaged<RtsCameraRequestSystem>()
            : null;
    }

    private static RtsCameraSystem ResolveRtsCameraSystem()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        return world != null && world.IsCreated
            ? world.GetOrCreateSystemManaged<RtsCameraSystem>()
            : null;
    }

    private static RtsSelectionRuntimeCameraSystem ResolveRtsSelectionRuntimeCameraSystem()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        return world != null && world.IsCreated
            ? world.GetOrCreateSystemManaged<RtsSelectionRuntimeCameraSystem>()
            : null;
    }

    private static SelectionScreenMarkerSystem ResolveSelectionScreenMarkerSystem()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        return world != null && world.IsCreated
            ? world.GetOrCreateSystemManaged<SelectionScreenMarkerSystem>()
            : null;
    }

    private static SelectionRuntimeConfigSystem ResolveSelectionRuntimeConfigSystem()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        return world != null && world.IsCreated
            ? world.GetOrCreateSystemManaged<SelectionRuntimeConfigSystem>()
            : null;
    }

    private static SelectionRuntimeDiagnosticsSystem ResolveSelectionRuntimeDiagnosticsSystem()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        return world != null && world.IsCreated
            ? world.GetOrCreateSystemManaged<SelectionRuntimeDiagnosticsSystem>()
            : null;
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

}
