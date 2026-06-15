using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
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
        var selectedAttackSourceScratch = new List<Entity>();
        var transportPassengerPanelItems = new List<MatchHudSelectionPanelPassengerItemModel>();
        IMatchRuntimeUi mainMenuPlayUi = null;
        IMatchHudSelectionPanelView matchHudSelectionPanelView = null;
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
            matchHudSelectionPanelView = view;
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
                ProcessTransportCommandRequests();
            if (rtsSelectionInputSystem.HasPendingMoveCommandRequestsOrResults())
                ProcessMoveCommandRequests();
            if (rtsSelectionInputSystem.HasPendingAttackCommandRequestsOrResults())
                ProcessAttackCommandRequests();
            if (rtsSelectionInputSystem.HasPendingScanCommandRequestsOrResults())
                ProcessScanCommandRequests();
            ProcessSelectionModeCommandRequests();
            ProcessMoveTargetModeCommandRequests();
            ProcessAttackTargetModeCommandRequests();
            ProcessScanTargetModeCommandRequests();
            ProcessBoardTargetModeCommandRequests();
            ProcessCancelActiveCommandModeRequests();
            ProcessImmediateSelectedUnitCommandRequests();
            ProcessSelectAllCommandRequests();
            ProcessDeselectAllCommandRequests();
            if (rtsSelectionInputSystem.HasPendingExternalSelectionCommandRequests())
                rtsSelectionFocusCommandSystem.ProcessExternalSelectionCommandRequests(CreateFocusCommandContext());
            RtsSelectionRuntimeInputSystem.Context inputContext = GetRuntimeInputContext();
            rtsSelectionRuntimeInputSystem.ProcessQueuedMoveOrder(inputContext);
            RefreshFocusedSelectionReadModels();
            UpdateMatchHudSelectionPanel();
            rtsSelectionCommandResultFlushSystem.UpdateOrderMarkerVisibility(GetCommandResultFlushContext());
            UpdateAttackTargetPreviewMarkers();
            UpdateBoardTargetPreviewMarkers();

            RtsSelectionRuntimeCameraSystem.Context cameraContext = GetRuntimeCameraContext();
            if (rtsSelectionRuntimeCameraSystem.UpdateRuntimeCameraTick(cameraContext))
                rtsSelectionRuntimeInputSystem.UpdateNormalPointerInput(inputContext);
        }

        void ProcessSelectAllCommandRequests()
        {
            if (runtimeConfig.WorldCamera == null ||
                !TryGetDefaultEntityManager(out EntityManager em) ||
                !RtsSelectionSelectAllCommandSystem.ProcessPendingRequests(em))
            {
                return;
            }

            SetExplicitAttackTargetModeActive(false);
            selectionHudFeedbackSystem.ClearCommandMode(CreateHudFeedbackContext());
            selectionHudFeedbackSystem.SetWorldMarkersVisible(CreateHudFeedbackContext(), false);
            if (rtsSelectionInputSystem.HasPendingSelectionRectangleRequests())
                ProcessSelectionRectangleRequests();
            rtsSelectionRuntimeCameraSystem.SetCameraDragging(GetRuntimeCameraContext(), false);
        }

        void ProcessSelectionModeCommandRequests()
        {
            if (!TryGetDefaultEntityManager(out EntityManager em) ||
                !RtsSelectionModeCommandSystem.ProcessPendingRequests(
                    em,
                    Time.frameCount,
                    out bool enteredSelectionMode,
                    out bool exitedSelectionMode,
                    out RtsSelectionCommandIntentKind lastProcessedKind))
            {
                return;
            }

            if (enteredSelectionMode)
            {
                SetExplicitAttackTargetModeActive(false);
                buildingPlacementInteractionSystem?.ClearSelectedBuilding(
                    buildingPlacementInteractionContext,
                    "SelectionUiCommandSystem.EnterSelectionMode");
            }

            selectionHudFeedbackSystem.SetWorldMarkersVisible(CreateHudFeedbackContext(), false);
            if (lastProcessedKind == RtsSelectionCommandIntentKind.EnterSelectionMode)
                selectionHudFeedbackSystem.ApplyCommandMode(CreateHudFeedbackContext(), TacticalCommandMode.Select);
            else if (lastProcessedKind == RtsSelectionCommandIntentKind.ExitSelectionMode)
                selectionHudFeedbackSystem.ClearCommandMode(CreateHudFeedbackContext());

            rtsSelectionRuntimeCameraSystem.SetCameraDragging(GetRuntimeCameraContext(), false);

            if (enteredSelectionMode)
                selectionRuntimeDiagnosticsSystem.LogSelectionClickDiagnostic(
                    $"selectionModeEntered source=ui frame={Time.frameCount} dragReset={rtsSelectionInputSystem.LastPointerPosition}");
            if (exitedSelectionMode)
                selectionRuntimeDiagnosticsSystem.LogSelectionClickDiagnostic(
                    $"selectionModeExited source=ui frame={Time.frameCount} dragReset={rtsSelectionInputSystem.LastPointerPosition}");
        }

        void ProcessMoveTargetModeCommandRequests()
        {
            if (!TryGetDefaultEntityManager(out EntityManager em) ||
                !RtsSelectionMoveTargetModeCommandSystem.ProcessPendingRequests(
                    em,
                    Time.frameCount,
                    out bool accepted,
                    out TacticalCommandReasonCode rejectionReason))
            {
                return;
            }

            SetExplicitAttackTargetModeActive(false);
            buildingPlacementInteractionSystem?.ClearSelectedBuilding(
                buildingPlacementInteractionContext,
                "SelectionUiCommandSystem.EnterMoveTargetMode");
            rtsSelectionRuntimeCameraSystem.SetCameraDragging(GetRuntimeCameraContext(), false);
            if (!accepted)
            {
                selectionHudFeedbackSystem.ClearCommandMode(CreateHudFeedbackContext());
                selectionHudFeedbackSystem.ApplyCommandResult(
                    CreateHudFeedbackContext(),
                    TacticalCommandResult.Rejected(rejectionReason));
                selectionRuntimeDiagnosticsSystem.LogSelectionClickDiagnostic(
                    $"moveModeEntered result=False reason={rejectionReason} frame={Time.frameCount}");
                return;
            }

            selectionHudFeedbackSystem.SetWorldMarkersVisible(CreateHudFeedbackContext(), false);
            selectionHudFeedbackSystem.ApplyCommandMode(CreateHudFeedbackContext(), TacticalCommandMode.Move);
            SelectionRuntimeDiagnosticsSystem.LogMoveCommandTrace(
                $"enterMoveTargetModeArmed mode={TacticalCommandMode.Move} oneShot=True requiresWorldTarget=True " +
                $"ignoreWorldUntil={rtsSelectionInputSystem.IgnoreWorldCommandsUntilFrame} frame={Time.frameCount}");
            selectionRuntimeDiagnosticsSystem.LogSelectionClickDiagnostic(
                $"moveModeEntered result=True frame={Time.frameCount} dragReset={rtsSelectionInputSystem.LastPointerPosition}");
        }

        void ProcessAttackTargetModeCommandRequests()
        {
            if (!TryGetDefaultEntityManager(out EntityManager em))
                return;

            if (RtsSelectionAttackTargetModeCommandSystem.HasPendingToggleAttackTargetModeRequest(em) &&
                IssueFocusedMissileLauncherRadarAttack())
            {
                RtsSelectionAttackTargetModeCommandSystem.ConsumeToggleAttackTargetModeRequest(em);
                return;
            }

            Entity focusedUnit = focusedUnitLifecycleSystem.TryGetFocusedUnitEntity(
                em,
                selectionStateSystem,
                out Entity resolvedFocusedUnit)
                ? resolvedFocusedUnit
                : Entity.Null;
            if (!RtsSelectionAttackTargetModeCommandSystem.ProcessPendingRequests(
                    em,
                    Time.frameCount,
                    focusedUnit,
                    out RtsSelectionCommandIntentKind processedKind,
                    out bool accepted,
                    out bool airDefenseAutoEngageOnly,
                    out TacticalCommandReasonCode rejectionReason))
            {
                return;
            }

            bool enterAttackTargetMode = processedKind == RtsSelectionCommandIntentKind.EnterAttackTargetMode;
            bool toggleAttackTargetMode = processedKind == RtsSelectionCommandIntentKind.ToggleAttackTargetMode;
            if (enterAttackTargetMode)
            {
                SetExplicitAttackTargetModeActive(false);
                buildingPlacementInteractionSystem?.ClearSelectedBuilding(
                    buildingPlacementInteractionContext,
                    "SelectionUiCommandSystem.EnterAttackTargetMode");
            }

            if (enterAttackTargetMode || accepted)
                rtsSelectionRuntimeCameraSystem.SetCameraDragging(GetRuntimeCameraContext(), false);

            if (airDefenseAutoEngageOnly)
            {
                selectionHudFeedbackSystem.ClearCommandMode(CreateHudFeedbackContext());
                selectionHudFeedbackSystem.ApplyCommandResult(
                    CreateHudFeedbackContext(),
                    TacticalCommandResult.Success("Air defense auto-engages aircraft and incoming missiles."));
                selectionHudFeedbackSystem.SetWorldMarkersVisible(CreateHudFeedbackContext(), false);
                selectionRuntimeDiagnosticsSystem.LogSelectionClickDiagnostic(
                    $"attackModeEntered result=False reason=AirDefenseAutoEngage frame={Time.frameCount}");
                return;
            }

            if (!accepted)
            {
                if (enterAttackTargetMode)
                    selectionHudFeedbackSystem.ClearCommandMode(CreateHudFeedbackContext());
                selectionHudFeedbackSystem.ApplyCommandResult(
                    CreateHudFeedbackContext(),
                    TacticalCommandResult.Rejected(rejectionReason));
                if (enterAttackTargetMode)
                    selectionHudFeedbackSystem.SetWorldMarkersVisible(CreateHudFeedbackContext(), false);
                selectionRuntimeDiagnosticsSystem.LogSelectionClickDiagnostic(
                    $"{(toggleAttackTargetMode ? "attackModeToggled" : "attackModeEntered")} result=False reason={rejectionReason} frame={Time.frameCount}");
                return;
            }

            SetExplicitAttackTargetModeActive(true);
            selectionHudFeedbackSystem.SetWorldMarkersVisible(CreateHudFeedbackContext(), true);
            selectionHudFeedbackSystem.ApplyCommandMode(CreateHudFeedbackContext(), TacticalCommandMode.Attack);
            selectionRuntimeDiagnosticsSystem.LogSelectionClickDiagnostic(
                $"{(toggleAttackTargetMode ? "attackModeToggled" : "attackModeEntered")} result=True frame={Time.frameCount} dragReset={rtsSelectionInputSystem.LastPointerPosition}");
        }

        void ProcessScanTargetModeCommandRequests()
        {
            if (!TryGetDefaultEntityManager(out EntityManager em) ||
                !RtsSelectionScanTargetModeCommandSystem.ProcessPendingRequests(em, Time.frameCount))
            {
                return;
            }

            SetExplicitAttackTargetModeActive(false);
            buildingPlacementInteractionSystem?.ExitBuildMode(buildingPlacementInteractionContext);
            buildingPlacementInteractionSystem?.CancelBuildingPlacement(buildingPlacementInteractionContext);
            buildingPlacementInteractionSystem?.ClearSelectedBuilding(
                buildingPlacementInteractionContext,
                "SelectionUiCommandSystem.EnterScanTargetMode");
            rtsSelectionRuntimeCameraSystem.SetCameraDragging(GetRuntimeCameraContext(), false);
            selectionHudFeedbackSystem.SetWorldMarkersVisible(CreateHudFeedbackContext(), false);
            selectionHudFeedbackSystem.ApplyCommandMode(CreateHudFeedbackContext(), TacticalCommandMode.Scan);
            selectionRuntimeDiagnosticsSystem.LogSelectionClickDiagnostic(
                $"scanModeEntered result=True frame={Time.frameCount} dragReset={rtsSelectionInputSystem.LastPointerPosition}");
        }

        void ProcessBoardTargetModeCommandRequests()
        {
            if (!TryGetDefaultEntityManager(out EntityManager em) ||
                !RtsSelectionBoardTargetModeCommandSystem.ProcessPendingRequests(
                    em,
                    Time.frameCount,
                    out bool accepted,
                    out bool toggledOff,
                    out BoardCommandModeDirection direction,
                    out Entity transport,
                    out TacticalCommandReasonCode rejectionReason))
            {
                return;
            }

            SetExplicitAttackTargetModeActive(false);
            buildingPlacementInteractionSystem?.ClearSelectedBuilding(
                buildingPlacementInteractionContext,
                "SelectionUiCommandSystem.EnterBoardTargetMode");
            rtsSelectionRuntimeCameraSystem.SetCameraDragging(GetRuntimeCameraContext(), false);

            if (toggledOff)
            {
                selectionHudFeedbackSystem.ClearCommandMode(CreateHudFeedbackContext());
                selectionHudFeedbackSystem.SetWorldMarkersVisible(CreateHudFeedbackContext(), false);
                selectionRuntimeDiagnosticsSystem.LogSelectionClickDiagnostic(
                    $"boardModeToggledOff frame={Time.frameCount}");
                return;
            }

            if (!accepted)
            {
                string message = rejectionReason == TacticalCommandReasonCode.CommandUnavailable
                    ? "Selected unit cannot board."
                    : "Select units to board.";
                selectionHudFeedbackSystem.ClearCommandMode(CreateHudFeedbackContext());
                selectionHudFeedbackSystem.ApplyCommandResult(
                    CreateHudFeedbackContext(),
                    TacticalCommandResult.Rejected(rejectionReason, message));
                selectionHudFeedbackSystem.SetWorldMarkersVisible(CreateHudFeedbackContext(), false);
                selectionRuntimeDiagnosticsSystem.LogSelectionClickDiagnostic(
                    $"boardModeEntered result=False reason={rejectionReason} message=\"{message}\" frame={Time.frameCount}");
                return;
            }

            selectionHudFeedbackSystem.SetWorldMarkersVisible(CreateHudFeedbackContext(), true);
            bool boardAllInteractable = direction == BoardCommandModeDirection.TransportToPassenger &&
                                        transport != Entity.Null;
            selectionHudFeedbackSystem.ApplyBoardCommandMode(
                CreateHudFeedbackContext(),
                direction,
                boardAllInteractable);
            selectionRuntimeDiagnosticsSystem.LogSelectionClickDiagnostic(
                $"boardModeEntered result=True direction={direction} transport={transport} frame={Time.frameCount} dragReset={rtsSelectionInputSystem.LastPointerPosition}");
        }

        void ProcessCancelActiveCommandModeRequests()
        {
            if (!TryGetDefaultEntityManager(out EntityManager em) ||
                !RtsSelectionCancelActiveCommandModeSystem.ProcessPendingRequests(em))
            {
                return;
            }

            SetExplicitAttackTargetModeActive(false);
            rtsSelectionRuntimeCameraSystem.SetCameraDragging(GetRuntimeCameraContext(), false);
            selectionHudFeedbackSystem.SetWorldMarkersVisible(CreateHudFeedbackContext(), false);
            selectionHudFeedbackSystem.ClearCommandMode(CreateHudFeedbackContext());
        }

        void ProcessImmediateSelectedUnitCommandRequests()
        {
            if (!TryGetDefaultEntityManager(out EntityManager em) ||
                !RtsSelectionImmediateSelectedUnitCommandSystem.ProcessPendingRequests(
                    em,
                    selectionStateSystem.FocusedUnit,
                    out RtsSelectionCommandIntentKind processedKind,
                    out bool accepted,
                    out TacticalCommandReasonCode rejectionReason,
                    out int issuedCount))
            {
                return;
            }

            bool hasCommandMode = TryGetImmediateSelectedUnitCommandMode(processedKind, out TacticalCommandMode mode);
            bool destroyFocusedUnit = processedKind == RtsSelectionCommandIntentKind.DestroyFocusedUnit;
            if (hasCommandMode)
                selectionHudFeedbackSystem.ApplyCommandMode(CreateHudFeedbackContext(), mode);
            if (!accepted)
            {
                if (destroyFocusedUnit &&
                    rejectionReason == TacticalCommandReasonCode.NoSelection &&
                    buildingPlacementInteractionSystem != null &&
                    buildingPlacementInteractionSystem.HasSelectedBuilding(buildingPlacementInteractionContext))
                {
                    buildingPlacementInteractionSystem.DeleteSelectedBuilding(buildingPlacementInteractionContext);
                    selectionHudFeedbackSystem.ClearSelection(CreateHudFeedbackContext());
                    selectionHudFeedbackSystem.ApplyCommandResult(
                        CreateHudFeedbackContext(),
                        TacticalCommandResult.Success("Destroyed selected building."));
                    return;
                }

                selectionHudFeedbackSystem.ApplyCommandResult(
                    CreateHudFeedbackContext(),
                    BuildImmediateSelectedUnitCommandResult(processedKind, accepted, rejectionReason, issuedCount));
                if (hasCommandMode)
                    selectionHudFeedbackSystem.ClearCommandMode(CreateHudFeedbackContext());
                return;
            }

            if (destroyFocusedUnit)
            {
                focusedUnitLifecycleSystem.ClearFocusedUnit(selectionStateSystem);
                selectionHudFeedbackSystem.ClearSelection(CreateHudFeedbackContext());
                selectionHudFeedbackSystem.ApplyCommandResult(
                    CreateHudFeedbackContext(),
                    BuildImmediateSelectedUnitCommandResult(processedKind, accepted, rejectionReason, issuedCount));
                return;
            }

            SetExplicitAttackTargetModeActive(false);
            selectionHudFeedbackSystem.SetWorldMarkersVisible(CreateHudFeedbackContext(), false);
            if (hasCommandMode)
            {
                buildingPlacementInteractionSystem?.ExitBuildMode(buildingPlacementInteractionContext);
                buildingPlacementInteractionSystem?.CancelBuildingPlacement(buildingPlacementInteractionContext);
                buildingPlacementInteractionSystem?.ClearSelectedBuilding(
                    buildingPlacementInteractionContext,
                    $"SelectionUiCommandSystem.{mode}");
                rtsSelectionRuntimeCameraSystem.SetCameraDragging(GetRuntimeCameraContext(), false);
                selectionHudFeedbackSystem.ClearCommandMode(CreateHudFeedbackContext());
                selectionHudFeedbackSystem.ApplyCommandResult(
                    CreateHudFeedbackContext(),
                    BuildImmediateSelectedUnitCommandResult(processedKind, accepted, rejectionReason, issuedCount));
                focusedUnitLifecycleSystem.RefreshFocusedUnit(
                    em,
                    selectionStateSystem,
                    applyHudSelectionAction);
                return;
            }

            selectionHudFeedbackSystem.ApplyCommandResult(
                CreateHudFeedbackContext(),
                BuildImmediateSelectedUnitCommandResult(processedKind, accepted, rejectionReason, issuedCount));
        }

        void ProcessDeselectAllCommandRequests()
        {
            if (!TryGetDefaultEntityManager(out EntityManager em) ||
                !RtsSelectionDeselectAllCommandSystem.ProcessPendingRequests(em))
            {
                return;
            }

            selectionStateSystem.ClearSelectedMoveCache();
            focusedUnitLifecycleSystem.ClearFocusedUnit(selectionStateSystem);
            SetExplicitAttackTargetModeActive(false);
            selectionHudFeedbackSystem.ClearSelection(CreateHudFeedbackContext());
            selectionHudFeedbackSystem.ClearCommandMode(CreateHudFeedbackContext());
            selectionHudFeedbackSystem.SetWorldMarkersVisible(CreateHudFeedbackContext(), false);
            rtsSelectionRuntimeCameraSystem.SetCameraDragging(GetRuntimeCameraContext(), false);
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
                () => rtsCameraSystem.IsDragging,
                value => rtsSelectionRuntimeCameraSystem.SetCameraDragging(GetRuntimeCameraContext(), value),
                pointerPosition => IsPointerOverRaycastableUi(pointerPosition, out _),
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
                screenDelta => rtsSelectionRuntimeCameraSystem.PanCamera(GetRuntimeCameraContext(), screenDelta),
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
                resolvedMatchIntroStateQuery,
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
                RequestMoveOrderScreenMarker,
                RequestAttackOrderScreenMarker,
                SetCameraDragging,
                focusedUnitLifecycleSystem.ClearFocusedUnit,
                TryGetClickedUnitEntity,
                TryGetMoveCommandCell,
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
                TryFocusUnitDirect);
        }

        RtsSelectionPointerTargetCommandSystem.Context CreatePointerTargetCommandContext()
        {
            return rtsSelectionPointerTargetCommandContextSystem.Create(
                runtimeGameplayStateSystem,
                rtsSelectionInputSystem,
                selectionStateSystem,
                focusedUnitLifecycleSystem,
                focusableUnitLookupSystem,
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
                RequestMoveOrderScreenMarker,
                SetCameraDragging,
                ProcessAttackCommandRequests,
                ProcessScanCommandRequests,
                ProcessTransportCommandRequests,
                ProcessMoveCommandRequests,
                selectionRuntimeDiagnosticsSystem.LogSelectionClickDiagnostic,
                DescribeTransportBoardingEntity);
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

            return TransportBoardingCommandSystem.IsSoldierBoardingCandidate(em, passenger);
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
                matchHudSelectionPanelView.Apply(MatchHudSelectionPanelModel.Hidden);
                return;
            }

            EnsureRuntimeSelectionDependencies(em);
            int selectedCount = CountSelectedTags(em);
            if (selectedCount > 1)
            {
                matchHudSelectionPanelView.Apply(BuildSquadPanelModel(em, selectedCount));
                matchHudSelectionPanelView.ApplyTransportPassengers(MatchHudTransportPassengersModel.Hidden);
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
                matchHudSelectionPanelView.ApplyTransportPassengers(MatchHudTransportPassengersModel.Hidden);
                return;
            }

            if (buildingPlacementInteractionSystem != null &&
                buildingPlacementInteractionSystem.HasSelectedBuilding(buildingPlacementInteractionContext))
            {
                matchHudSelectionPanelView.Apply(BuildSelectedBuildingPanelModel());
                matchHudSelectionPanelView.ApplyTransportPassengers(MatchHudTransportPassengersModel.Hidden);
                return;
            }

            matchHudSelectionPanelView.Apply(MatchHudSelectionPanelModel.Hidden);
            matchHudSelectionPanelView.ApplyTransportPassengers(MatchHudTransportPassengersModel.Hidden);
        }

        MatchHudSelectionPanelModel BuildFocusedUnitPanelModel(EntityManager em, Entity entity)
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

            return new MatchHudSelectionPanelModel(
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

        MatchHudTransportPassengersModel BuildTransportPassengersPanelModel(EntityManager em, Entity transport)
        {
            transportPassengerPanelItems.Clear();
            if (!focusedUnitUiReadModelSystem.TryRead(
                    em,
                    out FocusedUnitUiReadModelComponent focusedModel,
                    out DynamicBuffer<FocusedUnitPassengerUiReadModelElement> passengers) ||
                focusedModel.HasFocusedUnit == 0 ||
                focusedModel.FocusedUnit != transport ||
                focusedModel.OwnedByPlayer == 0 ||
                focusedModel.TransportPassengerCapacity <= 0)
            {
                return MatchHudTransportPassengersModel.Hidden;
            }

            int capacity = math.max(0, focusedModel.TransportPassengerCapacity);
            if (capacity <= 0)
                return MatchHudTransportPassengersModel.Hidden;

            for (int i = 0; i < passengers.Length; i++)
            {
                FocusedUnitPassengerUiReadModelElement passengerModel = passengers[i];
                Entity passenger = passengerModel.Passenger;
                if (!em.Exists(passenger))
                    continue;

                BuildHealthModelFromValues(passengerModel.HealthCurrent, passengerModel.HealthMax, out string healthLabel, out float health01);
                Sprite portrait = resolveSelectionCardPortraitSprite?.Invoke(em, passenger);
                portrait ??= resolveSelectionPortraitSprite?.Invoke(em, passenger);
                portrait ??= matchHudSelectionPanelView.ResolveFallbackPortraitSprite(SelectionSummaryPortraitKind.Soldiers);
                transportPassengerPanelItems.Add(new MatchHudSelectionPanelPassengerItemModel(
                    ToUiHandle(passenger),
                    passengerModel.DisplayName.ToString(),
                    ResolvePassengerRoleText(em, passenger),
                    healthLabel,
                    health01,
                    portrait,
                    true));
            }

            return new MatchHudTransportPassengersModel(
                true,
                false,
                ToUiHandle(transport),
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

        MatchHudSelectionPanelModel BuildSquadPanelModel(EntityManager em, int selectedCount)
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
            return new MatchHudSelectionPanelModel(
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
                HasSelectedBoardAction(em));
        }

        Sprite ResolveActiveSquadTrayPortraitSprite()
        {
            if (matchHudSquadTrayView == null)
                return null;

            return matchHudSquadTrayView.TryGetPortraitSprite(matchHudSquadTraySelectionSystem.ActiveSlot, out Sprite sprite)
                ? sprite
                : null;
        }

        MatchHudSelectionPanelModel BuildSelectedBuildingPanelModel()
        {
            string label = buildingPlacementInteractionSystem.SelectedBuildingLabel(buildingPlacementInteractionContext);
            Sprite portraitSprite = resolveSelectedBuildingPortraitSprite?.Invoke();
            portraitSprite ??= matchHudSelectionPanelView.ResolveFallbackPortraitSprite(SelectionSummaryPortraitKind.Buildings);
            return new MatchHudSelectionPanelModel(
                true,
                string.IsNullOrWhiteSpace(label) ? "Selected Building" : label,
                "Base Structure",
                "Structure selected",
                "-",
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

            BuildHealthModelFromValues(current, max, out healthLabel, out health01);
        }

        static void BuildHealthModelFromValues(int current, int max, out string healthLabel, out float health01)
        {
            if (max <= 0)
            {
                healthLabel = "Health: -";
                health01 = 0f;
                return;
            }

            healthLabel = $"Health: {math.max(0, current)}/{max}";
            health01 = math.saturate((float)current / max);
        }

        static UiEntityHandle ToUiHandle(Entity entity)
        {
            return entity == Entity.Null
                ? UiEntityHandle.Null
                : new UiEntityHandle(entity.Index, entity.Version);
        }

        static Entity ToEntity(UiEntityHandle handle)
        {
            return handle.IsNull
                ? Entity.Null
                : new Entity { Index = handle.Index, Version = handle.Version };
        }

        bool IsBoardCommandAvailable(EntityManager em, Entity entity)
        {
            if (!selectionUiQuerySystem.IsOwnedByPlayer(em, entity))
                return false;

            if (TransportBoardingCommandSystem.IsSoldierBoardingCandidate(em, entity))
                return true;

            return IsBoardTransportWithAvailableSeats(em, entity);
        }

        bool IsBoardTransportWithAvailableSeats(EntityManager em, Entity entity)
        {
            if (!selectionUiQuerySystem.IsOwnedByPlayer(em, entity))
                return false;

            if (!TransportBoardingCommandSystem.IsBoardablePlayerTransport(em, entity))
                return false;

            int capacity = em.GetComponentData<UnitTransportCapacity>(entity).SoldierCapacity;
            int passengers = em.HasBuffer<UnitTransportPassengerElement>(entity)
                ? em.GetBuffer<UnitTransportPassengerElement>(entity).Length
                : 0;
            return capacity > passengers + CountPendingBoardingOrders(em, entity);
        }

        bool HasSelectedBoardAction(EntityManager em)
        {
            if (focusedUnitLifecycleSystem.TryGetFocusedUnitEntity(em, selectionStateSystem, out Entity focusedUnit) &&
                em.Exists(focusedUnit) &&
                TransportBoardingCommandSystem.IsBoardablePlayerTransport(em, focusedUnit) &&
                IsBoardCommandAvailable(em, focusedUnit))
            {
                return true;
            }

            using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<SelectedUnitTag>());
            if (query.IsEmptyIgnoreFilter)
                return false;

            EntityTypeHandle entityType = em.GetEntityTypeHandle();
            using NativeArray<ArchetypeChunk> chunks = query.ToArchetypeChunkArray(Allocator.Temp);
            for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
            {
                NativeArray<Entity> entities = chunks[chunkIndex].GetNativeArray(entityType);
                for (int i = 0; i < entities.Length; i++)
                {
                    Entity entity = entities[i];
                    if (!em.Exists(entity))
                        continue;

                    if (TransportBoardingCommandSystem.IsSoldierBoardingCandidate(em, entity))
                        return true;

                    if (TransportBoardingCommandSystem.IsBoardablePlayerTransport(em, entity) &&
                        IsBoardCommandAvailable(em, entity))
                    {
                        return true;
                    }
                }
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
                applyHudSelectionAction);
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
                clearHudSelectionAction);
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
            rtsSelectionCommandResultFlushSystem.ProcessMoveCommandRequests(GetCommandResultFlushContext());
        }

        bool ProcessAttackCommandRequests()
        {
            return rtsSelectionCommandResultFlushSystem.ProcessAttackCommandRequests(
                GetCommandResultFlushContext(),
                explicitAttackTargetModeActive);
        }

        bool ProcessScanCommandRequests()
        {
            return rtsSelectionCommandResultFlushSystem.ProcessScanCommandRequests(GetCommandResultFlushContext());
        }

        bool ProcessTransportCommandRequests()
        {
            return rtsSelectionCommandResultFlushSystem.ProcessTransportCommandRequests(GetCommandResultFlushContext());
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

        bool TryGetMoveCommandCell(Vector2 screenPosition, EntityManager em, out int2 cell, out Vector3 worldPoint)
        {
            return rtsSelectionPointerTargetCommandSystem.TryGetMoveCommandCell(
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

            EntityTypeHandle entityType = em.GetEntityTypeHandle();
            using NativeArray<ArchetypeChunk> chunks = selectedTagQuery.ToArchetypeChunkArray(Allocator.Temp);
            for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
            {
                NativeArray<Entity> selectedEntities = chunks[chunkIndex].GetNativeArray(entityType);
                for (int i = 0; i < selectedEntities.Length; i++)
                    TryAddAttackSource(em, selectedEntities[i], sources);
            }
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

            if (!IsAttackSourceEntity(em, entity))
                return false;

            sources.Add(entity);
            return true;
        }

        static bool IsAttackSourceEntity(EntityManager em, Entity entity)
        {
            if (!em.HasComponent<Faction>(entity) ||
                !FactionIdentitySystem.IsPlayerControlled(em.GetComponentData<Faction>(entity).Id) ||
                !em.HasComponent<UnitMove>(entity) ||
                !em.HasComponent<UnitCombat>(entity) ||
                em.GetComponentData<UnitCombat>(entity).CanAttack == 0)
            {
                return false;
            }

            return !em.HasComponent<UnitHealth>(entity) ||
                   em.GetComponentData<UnitHealth>(entity).Current > 0;
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
            EnsureRuntimeSelectionDependencies(em);
            return selectionRuntimeQuerySystem.SelectedTagQuery.CalculateEntityCount();
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

        int CountPendingBoardingOrders(EntityManager em, Entity transport)
        {
            int count = 0;
            using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<UnitTransportBoardingTarget>());
            if (query.IsEmptyIgnoreFilter)
                return 0;

            ComponentTypeHandle<UnitTransportBoardingTarget> targetType = em.GetComponentTypeHandle<UnitTransportBoardingTarget>(true);
            using NativeArray<ArchetypeChunk> chunks = query.ToArchetypeChunkArray(Allocator.Temp);
            for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
            {
                NativeArray<UnitTransportBoardingTarget> targets = chunks[chunkIndex].GetNativeArray(ref targetType);
                for (int i = 0; i < targets.Length; i++)
                    if (targets[i].Transport == transport)
                        count++;
            }

            return count;
        }

        bool IssueFocusedMissileLauncherRadarAttack()
        {
            if (!TryGetFocusedUnitEntity(out EntityManager em, out Entity launcher))
                return false;
            if (!RtsSelectionMissileLauncherRadarAttackCommandSystem.TryIssuePendingFocusedRadarAttack(
                    em,
                    launcher,
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
            rtsSelectionRuntimeCameraSystem.SetCameraDragging(GetRuntimeCameraContext(), isDragging);
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

    internal static bool TryGetImmediateSelectedUnitCommandMode(
        RtsSelectionCommandIntentKind kind,
        out TacticalCommandMode mode)
    {
        switch (kind)
        {
            case RtsSelectionCommandIntentKind.HoldPosition:
                mode = TacticalCommandMode.Hold;
                return true;
            case RtsSelectionCommandIntentKind.Stop:
                mode = TacticalCommandMode.Stop;
                return true;
            default:
                mode = TacticalCommandMode.None;
                return false;
        }
    }

    internal static TacticalCommandResult BuildImmediateSelectedUnitCommandResult(
        RtsSelectionCommandIntentKind kind,
        bool accepted,
        TacticalCommandReasonCode rejectionReason,
        int issuedCount)
    {
        if (!accepted)
            return TacticalCommandResult.Rejected(rejectionReason);

        return kind switch
        {
            RtsSelectionCommandIntentKind.HoldPosition => TacticalCommandResult.Success("Holding current position."),
            RtsSelectionCommandIntentKind.Stop => TacticalCommandResult.Success("Stopped selected units."),
            RtsSelectionCommandIntentKind.ReturnToBase => TacticalCommandResult.Success(
                issuedCount == 1 ? "Unit returning to base." : $"{issuedCount} units returning to base."),
            RtsSelectionCommandIntentKind.DestroyFocusedUnit => TacticalCommandResult.Success(
                issuedCount == 1 ? "Destroyed selected unit." : $"Destroyed {issuedCount} selected units."),
            _ => TacticalCommandResult.Success()
        };
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
