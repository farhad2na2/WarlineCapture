using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.InputSystem;
using Game.Tactical.Contracts;
using Game.UI.Contracts;
using Game.Components;
using Game.Configs;

namespace Game.Runtime
{
    internal sealed class SelectionGameplayStartupSystemHelper
    {
        private static readonly ProfilerMarker SelectionCommandFlushMarker = new("GameplayRuntimeUpdate.Selection.CommandFlush");
        private static readonly ProfilerMarker SelectionInputMarker = new("GameplayRuntimeUpdate.Selection.Input");
        private static readonly ProfilerMarker SelectionFocusedReadModelMarker = new("GameplayRuntimeUpdate.Selection.FocusedReadModel");
        private static readonly ProfilerMarker SelectionPanelMarker = new("GameplayRuntimeUpdate.Selection.Panel");
        private static readonly ProfilerMarker SelectionTacticalCameraMarker = new("GameplayRuntimeUpdate.Selection.TacticalCamera");
        private static readonly ProfilerMarker SelectionMarkerPreviewMarker = new("GameplayRuntimeUpdate.Selection.MarkerPreview");
        private static readonly ProfilerMarker SelectionCameraMarker = new("GameplayRuntimeUpdate.Selection.Camera");

        public readonly struct Result
        {
            public readonly System.Action<IMatchRuntimeUi> BindSelectionMainMenu;
            public readonly System.Action<IMatchHudSelectionPanelView> BindMatchHudSelectionPanel;
            public readonly System.Action SelectionRuntimeUpdate;
            public readonly System.Action DisposeSelection;
            public readonly SelectionUiCommandUiSystemHelper SelectionUiCommand;
            public readonly SelectionUiReadModelUiSystemHelper SelectionUiReadModel;
            public readonly SelectionUiCameraSystemHelper SelectionUiCamera;
            public readonly SelectionBuildingInteractionCompositionSystemHelper SelectionBuildingInteraction;
            public readonly SelectionScreenMarkerUiSystemHelper SelectionScreenMarkers;
            public readonly ISelectionRectangleView SelectionRectangleView;
            public readonly System.Func<bool> ShouldBlockBuildingSelectionClick;

            public Result(
                System.Action<IMatchRuntimeUi> bindSelectionMainMenu,
                System.Action<IMatchHudSelectionPanelView> bindMatchHudSelectionPanel,
                System.Action selectionRuntimeUpdate,
                System.Action disposeSelection,
                SelectionUiCommandUiSystemHelper selectionUiCommand,
                SelectionUiReadModelUiSystemHelper selectionUiReadModel,
                SelectionUiCameraSystemHelper selectionUiCamera,
                SelectionBuildingInteractionCompositionSystemHelper selectionBuildingInteraction,
                SelectionScreenMarkerUiSystemHelper selectionScreenMarkers,
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
            RoadBuildReadModelCompositionSystemHelper roadBuildReadModel,
            BuildingPlacementInteractionCompositionSystemHelper buildingInteraction,
            BuildingPlacementInteractionCompositionSystemHelper.Context buildingInteractionContext,
            System.Func<Rect, bool> trySelectFirstBuildingInScreenRect,
            SelectionHudFeedbackUiSystemHelper.ResolveSelectionPortraitSpriteDelegate resolveSelectionPortraitSprite,
            SelectionHudFeedbackUiSystemHelper.ResolveSelectionPortraitSpriteDelegate resolveSelectionCardPortraitSprite,
            System.Func<Sprite> resolveSelectedBuildingPortraitSprite,
            SelectionOrderMarkerPresentationSystemHelper.TryResolveRuntimeBuildingInstanceDelegate tryResolveRuntimeBuildingInstance,
            FactionVisualSettings factionVisuals,
            IMatchIntroStateQuery matchIntroStateQuery)
        {
            IMatchIntroStateQuery resolvedMatchIntroStateQuery = matchIntroStateQuery ?? NullMatchIntroStateQuery.Instance;
            SelectionRuntimeDiagnosticsSystemHelper selectionRuntimeDiagnosticsSystem = ResolveSelectionRuntimeDiagnosticsSystem();
            SelectionRuntimeConfigStartupSystemHelper.State runtimeConfig = SelectionRuntimeConfigStartupSystemHelper.CreateStateFromConfig(rtsSelectionConfig, worldCamera);
            var runtimeGameplayStateSystem = new RuntimeGameplayStateSystem();
            var rtsSelectionInputSystem = new RtsSelectionInputCompositionSystemHelper();
            var rtsSelectionRuntimeInputSystem = new RtsSelectionRuntimeInputCompositionSystemHelper();
            RtsSelectionRuntimeCameraSystemHelper rtsSelectionRuntimeCameraSystem = ResolveRtsSelectionRuntimeCameraSystemHelper();
            var rtsSelectionCommandResultFlushSystem = new RtsSelectionCommandResultFlushCompositionSystemHelper();
            var rtsSelectionFocusCommandSystem = new RtsSelectionFocusCommandCompositionSystemHelper();
            var rtsSelectionPointerTargetCommandSystem = new RtsSelectionPointerTargetCommandCompositionSystemHelper();
            RtsCameraSystem rtsCameraSystem = ResolveRtsCameraSystem();
            RtsCameraRequestSystem rtsCameraRequestSystem = ResolveRtsCameraRequestSystem();
            var selectionUiCommand = new SelectionUiCommandUiSystemHelper(IsMatchIntroGameplayInputLocked);
            var selectionUiReadModel = new SelectionUiReadModelUiSystemHelper();
            var selectionUiCamera = new SelectionUiCameraSystemHelper(rtsCameraSystem, rtsCameraRequestSystem);
            var selectionScreenMarkers = new SelectionScreenMarkerUiSystemHelper();
            var selectionStateSystem = new SelectionStateCompositionSystemHelper();
            var selectionUiReadModelLookup = new SelectionUiReadModelLookup();
            var focusedUnitUiReadModelSystem = new FocusedUnitUiReadModelUiSystemHelper();
            var visibleUnitSelectionSystem = new VisibleUnitSelectionCameraSystemHelper();
            var selectionRectangleRequestSystem = new SelectionRectangleRequestCompositionSystemHelper();
            var unitMoveOrderSystem = new UnitMoveOrderSystem();
            var selectedMoveOrderCommandSystem = new SelectedMoveOrderCommandSystem();
            var attackOrderCommandSystem = new AttackOrderCommandSystem();
            var scanIntelCommandSystem = new ScanIntelCommandSystem();
            var selectionOrderMarkerSystem = new SelectionOrderMarkerPresentationSystemHelper();
            var selectionHudFeedbackSystem = new SelectionHudFeedbackUiSystemHelper();
            var focusedUnitCommandSystem = new FocusedUnitCommandSystem();
            var focusedUnitLifecycleSystem = new FocusedUnitLifecycleCompositionSystemHelper();
            var selectedUnitOrderSnapshotSystem = new SelectedUnitOrderSnapshotCompositionSystemHelper();
            var buildingTargetMoveOrderSystem = new BuildingTargetMoveOrderSystem();
            var transportBoardingCommandSystem = new TransportBoardingCommandSystem();
            var tacticalFollowCameraModeSystem = new TacticalFollowCameraModeSystemHelper();
            var focusableUnitLookupSystem = new FocusableUnitLookupCameraSystemHelper();
            var matchHudSquadTraySelectionSystem = new MatchHudSquadTraySelectionUiSystemHelper();
            var unitTransportCapacitySystem = new UnitTransportCapacitySystem();
            var unitTransportAirPickupSystem = new UnitTransportAirPickupSystem();
            var selectionBuildingInteraction = new SelectionBuildingInteractionCompositionSystemHelper();
            var visibleSelectionScratch = new List<Entity>();
            var transportPassengerPanelItems = new List<MatchHudSelectionPanelPassengerItemModel>();
            IMatchRuntimeUi mainMenuPlayUi = null;
            IMatchHudSquadTrayView matchHudSquadTrayView = null;
            RtsSelectionRuntimeInputCompositionSystemHelper.Context runtimeInputContext = default;
            bool hasRuntimeInputContext = false;
            RtsSelectionRuntimeCameraSystemHelper.Context runtimeCameraContext = default;
            bool hasRuntimeCameraContext = false;
            RtsSelectionCommandResultFlushCompositionSystemHelper.Context commandResultFlushContext = default;
            bool hasCommandResultFlushContext = false;
            RtsSelectionPointerTargetCommandCompositionSystemHelper.Context pointerTargetCommandContext = default;
            bool hasPointerTargetCommandContext = false;
            SelectionHudFeedbackUiSystemHelper.Context hudFeedbackContext = default;
            bool hasHudFeedbackContext = false;
            TacticalFollowCameraModeSystemHelper.Context tacticalFollowCameraContext = default;
            bool hasTacticalFollowCameraContext = false;
            IMatchHudSelectionPanelView matchHudSelectionPanelView = null;
            int lastTacticalFollowCameraFeedbackSequence = 0;
            bool hasLastTacticalFollowPose = false;
            TacticalFollowCameraPoseSource lastTacticalFollowPoseSource = TacticalFollowCameraPoseSource.None;
            Vector3 lastTacticalFollowDesiredPosition = Vector3.zero;
            Vector3 lastTacticalFollowLookAt = Vector3.zero;
            bool lastTacticalFollowOrthographic = false;
            Unity.Entities.World selectionRuntimeQueryWorld = null;
            EntityQuery selectedMoveQuery = default;
            EntityQuery moveTargetCommandQueueQuery = default;
            EntityQuery moveTargetRuntimeStateQuery = default;
            EntityQuery moveTargetSelectedMoveQuery = default;
            EntityQuery selectAllCommandQueueQuery = default;
            EntityQuery immediateRespawnQueueQuery = default;
            EntityQuery immediateBuildingRuntimeStateQuery = default;
            EntityQuery selectedTagQuery = default;
            EntityQuery gridConfigQuery = default;
            EntityQuery mapSurfaceQuery = default;
            System.Action<EntityManager, Entity> applyHudSelectionAction = ApplyHudSelection;
            System.Action<int> applyHudSquadSelectionAction = ApplyHudSquadSelection;
            SelectionRectangleRequestCompositionSystemHelper.ApplyHudSelectionAction applyRectangleHudSelectionAction = ApplyHudSelection;
            SelectionRectangleRequestCompositionSystemHelper.ApplyHudSquadSelectionAction applyRectangleHudSquadSelectionAction = ApplyHudSquadSelection;
            System.Action clearHudSelectionAction = ClearHudSelection;
            RoadBuildReadModelCompositionSystemHelper roadBuildReadState = roadBuildReadModel;
            BuildingPlacementInteractionCompositionSystemHelper buildingPlacementInteractionSystem = buildingInteraction;
            BuildingPlacementInteractionCompositionSystemHelper.Context buildingPlacementInteractionContext = buildingInteractionContext;
            bool explicitAttackTargetModeActive = false;
            bool attackModeOrderSnapshotActive = false;
            string attackModeOrderSnapshotText = string.Empty;
            SelectionHudFeedbackUiSystemHelper.RefreshFocusedUnitDelegate refreshFocusedUnitAction = RefreshFocusedUnit;
            System.Func<Sprite> resolveActiveSquadTrayPortraitSpriteAction = ResolveActiveSquadTrayPortraitSprite;
            System.Func<bool> hasSelectedBuildingAction = HasSelectedBuilding;
            System.Func<string> selectedBuildingLabelAction = SelectedBuildingLabel;
            SelectionHudFeedbackUiSystemHelper.TryGetSelectedBuildingResourceStorageDelegate tryGetSelectedBuildingResourceStorageAction =
                TryGetSelectedBuildingResourceStorage;
            SelectionHudFeedbackUiSystemHelper.IsBoardCommandAvailableDelegate isBoardCommandAvailableAction =
                IsBoardCommandAvailable;
            SelectionHudFeedbackUiSystemHelper.HasSelectedBoardActionDelegate hasSelectedBoardAction =
                HasSelectedBoardAction;
            SelectionOrderMarkerPresentationSystemHelper.IsPreviewTargetValidWithSourceDelegate isValidBoardTransportPreviewTargetAction =
                IsValidBoardTransportPreviewTarget;
            SelectionOrderMarkerPresentationSystemHelper.IsPreviewTargetValidWithSourceDelegate isValidBoardPassengerPreviewTargetAction =
                IsValidBoardPassengerPreviewTarget;

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
                mainMenuPlayUi?.ConfigureMatchHudRuntimeFeedbackSinkBinding(BindBattleHudRuntimeFeedback);
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
                view?.BindCameraAction(RequestToggleTacticalFollowCameraModeFromPanel);
                view?.BindTransportPassengerActions(
                    () => { },
                    () => { },
                    () => selectionUiCommand.RequestFocusedTransportDisembark(),
                    passenger => selectionUiCommand.RequestFocusedTransportPassengerDisembark(ToEntity(passenger)));
            }

            void BindBattleHudRuntimeFeedback(IBattleHudRuntimeFeedbackSink feedbackSink)
            {
                selectionHudFeedbackSystem.BindBattleHudRuntimeFeedback(feedbackSink);
                hasCommandResultFlushContext = false;
            }

            void RequestBoardTargetModeFromPanel()
            {
                if (selectionUiCommand.RequestBoardTargetMode())
                    return;

                selectionHudFeedbackSystem.ApplyCommandResult(
                    GetHudFeedbackContext(),
                    TacticalCommandResult.Rejected(TacticalCommandReasonCode.CommandUnavailable, "Board command unavailable."));
            }

            void RequestToggleTacticalFollowCameraModeFromPanel()
            {
                if (selectionUiCommand.RequestToggleTacticalFollowCameraMode())
                    return;

                selectionHudFeedbackSystem.ApplyCommandResult(
                    GetHudFeedbackContext(),
                    TacticalCommandResult.Rejected(TacticalCommandReasonCode.CameraJumpUnavailable, "Camera follow unavailable."));
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
                using (SelectionCommandFlushMarker.Auto())
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
                        UnityEngine.Time.frameCount);
                    rtsSelectionCommandResultFlushSystem.ProcessMoveTargetModeCommandRequests(
                        GetCommandResultFlushContext(),
                        UnityEngine.Time.frameCount);
                    if (TryGetDefaultEntityManager(out EntityManager attackTargetModeEntityManager))
                    {
                        Entity focusedUnit = focusedUnitLifecycleSystem.TryGetFocusedUnitEntity(
                            attackTargetModeEntityManager,
                            selectionStateSystem,
                            out Entity resolvedFocusedUnit)
                            ? resolvedFocusedUnit
                            : Entity.Null;
                        if (!RtsSelectionAttackTargetModeCommandSystem.HasPendingToggleAttackTargetModeRequest(attackTargetModeEntityManager, moveTargetCommandQueueQuery) ||
                            !rtsSelectionCommandResultFlushSystem.ProcessFocusedMissileLauncherRadarAttack(
                                GetCommandResultFlushContext(),
                                focusedUnit))
                        {
                            rtsSelectionCommandResultFlushSystem.ProcessAttackTargetModeCommandRequests(
                                GetCommandResultFlushContext(),
                                UnityEngine.Time.frameCount,
                                focusedUnit);
                        }
                    }
                    rtsSelectionCommandResultFlushSystem.ProcessScanTargetModeCommandRequests(
                        GetCommandResultFlushContext(),
                        UnityEngine.Time.frameCount);
                    rtsSelectionCommandResultFlushSystem.ProcessBoardTargetModeCommandRequests(
                        GetCommandResultFlushContext(),
                        UnityEngine.Time.frameCount);
                    rtsSelectionCommandResultFlushSystem.ProcessCancelActiveCommandModeRequests(GetCommandResultFlushContext());
                    rtsSelectionCommandResultFlushSystem.ProcessImmediateSelectedUnitCommandRequests(
                        GetCommandResultFlushContext(),
                        selectionStateSystem.FocusedUnit);
                    if (runtimeConfig.WorldCamera != null)
                        rtsSelectionCommandResultFlushSystem.ProcessSelectAllCommandRequests(GetCommandResultFlushContext());
                    rtsSelectionCommandResultFlushSystem.ProcessDeselectAllCommandRequests(GetCommandResultFlushContext());
                }

                using (SelectionInputMarker.Auto())
                {
                    if (rtsSelectionInputSystem.HasPendingExternalSelectionCommandRequests())
                        rtsSelectionFocusCommandSystem.ProcessExternalSelectionCommandRequests(CreateFocusCommandContext());
                    RtsSelectionRuntimeInputCompositionSystemHelper.Context inputContext = GetRuntimeInputContext();
                    rtsSelectionRuntimeInputSystem.ProcessQueuedMoveOrder(inputContext);
                }

                using (SelectionTacticalCameraMarker.Auto())
                {
                    ProcessTacticalFollowCameraRequests();
                }

                using (SelectionFocusedReadModelMarker.Auto())
                {
                    selectionHudFeedbackSystem.RefreshFocusedSelectionReadModels(
                        GetHudFeedbackContext(),
                        selectionStateSystem,
                        focusedUnitUiReadModelSystem,
                        unitTransportCapacitySystem,
                        EnsureRuntimeSelectionDependencies,
                        refreshFocusedUnitAction,
                        UnityEngine.Time.time);
                }

                using (SelectionPanelMarker.Auto())
                {
                    selectionHudFeedbackSystem.UpdateMatchHudSelectionPanel(
                        GetHudFeedbackContext(),
                        selectionStateSystem,
                        focusedUnitLifecycleSystem,
                        focusedUnitUiReadModelSystem,
                        transportPassengerPanelItems,
                        EnsureRuntimeSelectionDependencies,
                        TryGetAttackModeOrderSnapshot,
                        resolveSelectionCardPortraitSprite,
                        resolveSelectedBuildingPortraitSprite,
                        resolveActiveSquadTrayPortraitSpriteAction,
                        hasSelectedBuildingAction,
                        selectedBuildingLabelAction,
                        tryGetSelectedBuildingResourceStorageAction,
                        isBoardCommandAvailableAction,
                        hasSelectedBoardAction);
                }

                using (SelectionTacticalCameraMarker.Auto())
                {
                    RefreshTacticalFollowCameraPose();
                    ApplyTacticalFollowCameraUiReadModel();
                }

                using (SelectionMarkerPreviewMarker.Auto())
                {
                    rtsSelectionCommandResultFlushSystem.UpdateOrderMarkerVisibility(GetCommandResultFlushContext());
                    rtsSelectionCommandResultFlushSystem.UpdateCommandPreviewMarkers(
                        GetCommandResultFlushContext(),
                        explicitAttackTargetModeActive,
                        isValidBoardTransportPreviewTargetAction,
                        isValidBoardPassengerPreviewTargetAction);
                }

                RtsSelectionRuntimeCameraSystemHelper.Context cameraContext = GetRuntimeCameraContext();
                RtsSelectionRuntimeInputCompositionSystemHelper.Context cameraInputContext = GetRuntimeInputContext();
                using (SelectionCameraMarker.Auto())
                {
                    if (rtsSelectionRuntimeCameraSystem != null &&
                        rtsSelectionRuntimeCameraSystem.UpdateRuntimeCameraTick(cameraContext))
                    {
                        rtsSelectionRuntimeInputSystem.UpdateNormalPointerInput(cameraInputContext);
                    }
                    UpdateTacticalFollowCameraPose();
                }
            }

            void ProcessTacticalFollowCameraRequests()
            {
                if (!TryGetDefaultEntityManager(out EntityManager em))
                    return;

                bool processed = tacticalFollowCameraModeSystem.ProcessPendingRequests(
                    em,
                    runtimeConfig.WorldCamera,
                    GetTacticalFollowCameraContext());
                if (!processed ||
                    !tacticalFollowCameraModeSystem.TryReadUiReadModel(em, out TacticalFollowCameraUiReadModelComponent readModel) ||
                    readModel.ReasonCode == (int)TacticalCommandReasonCode.None)
                {
                    return;
                }

                selectionHudFeedbackSystem.ApplyCommandResult(
                    GetHudFeedbackContext(),
                    TacticalCommandResult.Rejected((TacticalCommandReasonCode)readModel.ReasonCode));
            }

            void ApplyTacticalFollowCameraUiReadModel()
            {
                if (matchHudSelectionPanelView == null ||
                    !TryGetDefaultEntityManager(out EntityManager em) ||
                    !tacticalFollowCameraModeSystem.TryReadUiReadModel(em, out TacticalFollowCameraUiReadModelComponent readModel))
                {
                    return;
                }

                matchHudSelectionPanelView.SetCameraActionEnabled(readModel.Enabled != 0);
                matchHudSelectionPanelView.SetCameraActionSelected(readModel.Selected != 0);
                ApplyTacticalFollowCameraFeedback(readModel);
            }

            void ApplyTacticalFollowCameraFeedback(TacticalFollowCameraUiReadModelComponent readModel)
            {
                if (readModel.FeedbackSequence <= 0 ||
                    readModel.FeedbackSequence == lastTacticalFollowCameraFeedbackSequence ||
                    readModel.FeedbackCode == (int)TacticalFollowCameraFeedbackCode.None)
                {
                    return;
                }

                lastTacticalFollowCameraFeedbackSequence = readModel.FeedbackSequence;
                TacticalCommandResult result =
                    (TacticalFollowCameraFeedbackCode)readModel.FeedbackCode switch
                    {
                        TacticalFollowCameraFeedbackCode.EnteredFollowMode =>
                            TacticalCommandResult.Success("Camera follow active."),
                        TacticalFollowCameraFeedbackCode.ExitedFollowMode =>
                            TacticalCommandResult.Success("RTS camera restored."),
                        TacticalFollowCameraFeedbackCode.TargetLost =>
                            TacticalCommandResult.Rejected(TacticalCommandReasonCode.CameraJumpUnavailable, "Follow target lost."),
                        _ => TacticalCommandResult.Success()
                    };

                if (!string.IsNullOrWhiteSpace(result.Message) || !result.Accepted)
                    selectionHudFeedbackSystem.ApplyCommandResult(GetHudFeedbackContext(), result);
            }

            void RefreshTacticalFollowCameraPose()
            {
                if (!TryGetDefaultEntityManager(out EntityManager em))
                    return;

                tacticalFollowCameraModeSystem.RefreshActiveTargetAndPose(em, GetTacticalFollowCameraContext());
            }

            TacticalFollowCameraModeSystemHelper.Context GetTacticalFollowCameraContext()
            {
                if (!hasTacticalFollowCameraContext)
                {
                    tacticalFollowCameraContext = CreateTacticalFollowCameraContext();
                    hasTacticalFollowCameraContext = true;
                }

                return tacticalFollowCameraContext;
            }

            TacticalFollowCameraModeSystemHelper.Context CreateTacticalFollowCameraContext()
            {
                return new TacticalFollowCameraModeSystemHelper.Context(TryResolveSelectedBuildingFollowTarget);
            }

            bool TryResolveSelectedBuildingFollowTarget(out Vector3 worldPosition, out float boundsRadius)
            {
                worldPosition = Vector3.zero;
                boundsRadius = 0f;
                return buildingPlacementInteractionSystem != null &&
                       buildingPlacementInteractionSystem.TryResolveSelectedBuildingFollowTarget(
                           buildingPlacementInteractionContext,
                           out worldPosition,
                           out boundsRadius);
            }

            void UpdateTacticalFollowCameraPose()
            {
                if (rtsSelectionRuntimeCameraSystem == null ||
                    runtimeConfig.WorldCamera == null ||
                    !TryGetDefaultEntityManager(out EntityManager em) ||
                    !tacticalFollowCameraModeSystem.TryReadPose(em, out TacticalFollowCameraPoseComponent pose))
                {
                    hasLastTacticalFollowPose = false;
                    return;
                }

                Vector3 desiredPosition = ToVector3(pose.DesiredPosition);
                Vector3 lookAt = ToVector3(pose.LookAt);
                bool orthographic = pose.Orthographic != 0;
                if (ShouldSuppressTacticalFollowVerticalJitter(
                        hasLastTacticalFollowPose,
                        lastTacticalFollowPoseSource,
                        lastTacticalFollowOrthographic,
                        lastTacticalFollowDesiredPosition,
                        lastTacticalFollowLookAt,
                        pose.Source,
                        orthographic,
                        desiredPosition,
                        lookAt))
                {
                    desiredPosition.y = lastTacticalFollowDesiredPosition.y;
                    lookAt.y = lastTacticalFollowLookAt.y;
                }

                bool resetVelocity =
                    !hasLastTacticalFollowPose ||
                    lastTacticalFollowPoseSource != pose.Source ||
                    lastTacticalFollowOrthographic != orthographic ||
                    Vector3.Distance(lastTacticalFollowDesiredPosition, desiredPosition) > 1.25f ||
                    Vector3.Distance(lastTacticalFollowLookAt, lookAt) > 1.25f;

                rtsCameraRequestSystem.RemoveRequestsSuppressedByTacticalFollow(em);
                rtsCameraRequestSystem.QueueClearSmoothFocusTarget(em);
                rtsCameraRequestSystem.QueueUpdateTacticalFollowPose(
                    em,
                    desiredPosition,
                    lookAt,
                    pose.FieldOfView,
                    pose.PositionDampingSeconds,
                    orthographic,
                    pose.OrthographicSize,
                    resetVelocity,
                    pose.Source == TacticalFollowCameraPoseSource.RestoreDefault
                        ? ToQuaternion(pose.DesiredRotation)
                        : null);
                rtsSelectionRuntimeCameraSystem.ProcessCameraRequests(GetRuntimeCameraContext(), em);

                hasLastTacticalFollowPose = true;
                lastTacticalFollowPoseSource = pose.Source;
                lastTacticalFollowDesiredPosition = desiredPosition;
                lastTacticalFollowLookAt = lookAt;
                lastTacticalFollowOrthographic = orthographic;

                if (pose.Source == TacticalFollowCameraPoseSource.RestoreDefault &&
                    IsTacticalFollowRestorePoseReached(runtimeConfig.WorldCamera, pose))
                {
                    tacticalFollowCameraModeSystem.ClearPose(em);
                    hasLastTacticalFollowPose = false;
                }
            }

            static bool IsTacticalFollowRestorePoseReached(Camera worldCamera, TacticalFollowCameraPoseComponent pose)
            {
                if (worldCamera == null)
                    return false;

                Vector3 desiredPosition = ToVector3(pose.DesiredPosition);
                float positionDistance = Vector3.Distance(worldCamera.transform.position, desiredPosition);
                float rotationAngle;
                if (pose.Source == TacticalFollowCameraPoseSource.RestoreDefault)
                {
                    rotationAngle = Quaternion.Angle(worldCamera.transform.rotation, ToQuaternion(pose.DesiredRotation));
                }
                else
                {
                    Vector3 lookAt = ToVector3(pose.LookAt);
                    Vector3 desiredForward = lookAt - desiredPosition;
                    if (desiredForward.sqrMagnitude <= 0.0001f)
                        desiredForward = ToVector3(math.forward(pose.DesiredRotation));
                    rotationAngle = Vector3.Angle(worldCamera.transform.forward, desiredForward.normalized);
                }

                float zoomDelta = pose.Orthographic != 0
                    ? Mathf.Abs(worldCamera.orthographicSize - pose.OrthographicSize)
                    : Mathf.Abs(worldCamera.fieldOfView - pose.FieldOfView);
                return positionDistance <= 0.1f &&
                       rotationAngle <= 0.5f &&
                       zoomDelta <= 0.1f &&
                       worldCamera.orthographic == (pose.Orthographic != 0);
            }

            static bool ShouldSuppressTacticalFollowVerticalJitter(
                bool hasLastPose,
                TacticalFollowCameraPoseSource lastSource,
                bool lastOrthographic,
                Vector3 lastDesiredPosition,
                Vector3 lastLookAt,
                TacticalFollowCameraPoseSource source,
                bool orthographic,
                Vector3 desiredPosition,
                Vector3 lookAt)
            {
                const float horizontalEpsilon = 0.35f;
                const float verticalEpsilon = 0.35f;
                if (!hasLastPose ||
                    source != TacticalFollowCameraPoseSource.BaseTarget ||
                    lastSource != source ||
                    lastOrthographic != orthographic)
                {
                    return false;
                }

                return HorizontalDistanceSq(lastDesiredPosition, desiredPosition) <= horizontalEpsilon * horizontalEpsilon &&
                       HorizontalDistanceSq(lastLookAt, lookAt) <= horizontalEpsilon * horizontalEpsilon &&
                       Mathf.Abs(lastDesiredPosition.y - desiredPosition.y) <= verticalEpsilon &&
                       Mathf.Abs(lastLookAt.y - lookAt.y) <= verticalEpsilon;
            }

            static float HorizontalDistanceSq(Vector3 a, Vector3 b)
            {
                float dx = a.x - b.x;
                float dz = a.z - b.z;
                return dx * dx + dz * dz;
            }

            RtsSelectionRuntimeInputCompositionSystemHelper.Context GetRuntimeInputContext()
            {
                if (!hasRuntimeInputContext)
                {
                    runtimeInputContext = CreateRuntimeInputContext();
                    hasRuntimeInputContext = true;
                }

                return runtimeInputContext;
            }

            RtsSelectionRuntimeCameraSystemHelper.Context GetRuntimeCameraContext()
            {
                if (!hasRuntimeCameraContext)
                {
                    runtimeCameraContext = CreateRuntimeCameraContext();
                    hasRuntimeCameraContext = true;
                }

                return runtimeCameraContext;
            }

            RtsSelectionCommandResultFlushCompositionSystemHelper.Context GetCommandResultFlushContext()
            {
                if (!hasCommandResultFlushContext)
                {
                    commandResultFlushContext = CreateCommandResultFlushContext();
                    hasCommandResultFlushContext = true;
                }

                return commandResultFlushContext;
            }

            RtsSelectionRuntimeInputCompositionSystemHelper.Context CreateRuntimeInputContext()
            {
                return new RtsSelectionRuntimeInputCompositionSystemHelper.Context(
                    runtimeGameplayStateSystem,
                    rtsSelectionInputSystem,
                    mainMenuPlayUi,
                    runtimeConfig.DragThresholdPixels,
                    runtimeConfig.SelectionModeHoldSeconds,
                    () => explicitAttackTargetModeActive,
                    SetExplicitAttackTargetModeActive,
                    () => rtsCameraSystem != null && rtsCameraSystem.IsDragging,
                    value => rtsSelectionRuntimeCameraSystem?.SetCameraDragging(GetRuntimeCameraContext(), value),
                    pointerPosition => IsPointerOverRaycastableUi(pointerPosition, out _),
                    pointerPosition => IsPointerOverGameplayUi(pointerPosition, out _),
                    screenPosition => rtsSelectionPointerTargetCommandSystem.TryRequestAttackOrderToClickedUnit(
                        GetPointerTargetCommandContext(),
                        screenPosition),
                    screenPosition => rtsSelectionPointerTargetCommandSystem.TryRequestScanOrder(
                        GetPointerTargetCommandContext(),
                        screenPosition),
                    selectionOrderMarkerSystem,
                    TryGetDefaultEntityManager,
                    (Vector2 screenPosition, EntityManager em, out int2 cell, out Vector3 worldPoint) =>
                        rtsSelectionPointerTargetCommandSystem.TryGetClickedCell(
                            GetPointerTargetCommandContext(),
                            screenPosition,
                            em,
                            out cell,
                            out worldPoint),
                    visible => selectionHudFeedbackSystem.SetWorldMarkersVisible(GetHudFeedbackContext(), visible),
                    screenPosition => rtsSelectionPointerTargetCommandSystem.TryRequestBoardTransportOrderToClickedUnit(
                        GetPointerTargetCommandContext(),
                        screenPosition),
                    (transport, pointerPosition) => rtsSelectionPointerTargetCommandSystem.TryRequestBoardSelectedTransportOrderToClickedUnit(
                        GetPointerTargetCommandContext(),
                        transport,
                        pointerPosition),
                    (transport, screenRect) => rtsSelectionPointerTargetCommandSystem.TryRequestBoardSelectedTransportOrdersToPassengerRect(
                        GetPointerTargetCommandContext(),
                        transport,
                        screenRect),
                    (transport, pointerPosition) => rtsSelectionPointerTargetCommandSystem.IsBoardSelectedTransportPassengerTarget(
                        GetPointerTargetCommandContext(),
                        transport,
                        pointerPosition),
                    screenPosition => rtsSelectionFocusCommandSystem.QueueFocusUnitCommand(
                        CreateFocusCommandContext(),
                        screenPosition),
                    screenDelta => rtsSelectionRuntimeCameraSystem?.PanCamera(GetRuntimeCameraContext(), screenDelta),
                    screenPosition => rtsSelectionPointerTargetCommandSystem.RequestMoveOrder(
                        GetPointerTargetCommandContext(),
                        screenPosition),
                    ProcessSelectionRectangleRequests,
                    () => selectionHudFeedbackSystem.ClearCommandMode(GetHudFeedbackContext()),
                    LogSelectionClickDiagnostic,
                    pointerPosition => rtsSelectionPointerTargetCommandSystem.BuildClickDebugSummary(
                        GetPointerTargetCommandContext(),
                        pointerPosition),
                    IsMatchIntroGameplayInputLocked);
            }

            RtsSelectionRuntimeCameraSystemHelper.Context CreateRuntimeCameraContext()
            {
                return new RtsSelectionRuntimeCameraSystemHelper.Context(
                    runtimeGameplayStateSystem,
                    rtsSelectionInputSystem,
                    rtsCameraSystem,
                    rtsCameraRequestSystem,
                    runtimeConfig.WorldCamera,
                    mainMenuPlayUi,
                    roadBuildReadState,
                    buildingPlacementInteractionSystem,
                    buildingPlacementInteractionContext,
                    TryGetDefaultEntityManager,
                    resolvedMatchIntroStateQuery,
                    IsPointerOverGameplayUi,
                    pointerPosition => rtsSelectionInputSystem.UpdateLastKnownPointerPosition(pointerPosition),
                    () => selectionScreenMarkers?.RequestHideOrderMarkers(),
                    runtimeConfig.PanSensitivity,
                    runtimeConfig.ZoomSpeed,
                    runtimeConfig.MinZoomHeight,
                    runtimeConfig.MaxZoomHeight,
                    runtimeConfig.NormalModeZoomHeight,
                    runtimeConfig.BuildModeZoomHeight,
                    runtimeConfig.NormalModePitch,
                    runtimeConfig.BuildModePitch,
                    runtimeConfig.NormalModeYaw,
                    runtimeConfig.BuildModeYaw,
                    runtimeConfig.NormalModeFieldOfView,
                    runtimeConfig.BuildModeFieldOfView,
                    runtimeConfig.FullscreenIsoZoomHeight,
                    runtimeConfig.FullscreenIsoPitch,
                    runtimeConfig.FullscreenIsoYaw,
                    runtimeConfig.FullscreenIsoOrthographicSize,
                    runtimeConfig.ZoomTransitionSmoothTime);
            }

            RtsSelectionCommandResultFlushCompositionSystemHelper.Context CreateCommandResultFlushContext()
            {
                if (TryGetDefaultEntityManager(out EntityManager em))
                    EnsureRuntimeSelectionDependencies(em);

                SelectionHudFeedbackUiSystemHelper.Context hudFeedbackContext = GetHudFeedbackContext();

                return new RtsSelectionCommandResultFlushCompositionSystemHelper.Context(
                    rtsSelectionInputSystem,
                    selectionHudFeedbackSystem,
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
                    selectedMoveQuery,
                    moveTargetCommandQueueQuery,
                    moveTargetRuntimeStateQuery,
                    moveTargetSelectedMoveQuery,
                    selectAllCommandQueueQuery,
                    immediateRespawnQueueQuery,
                    immediateBuildingRuntimeStateQuery,
                    selectedTagQuery,
                    gridConfigQuery,
                    mapSurfaceQuery,
                    TryGetDefaultEntityManager,
                    EnsureRuntimeSelectionDependencies,
                    ClearCurrentSelection,
                    mode => selectionHudFeedbackSystem.ApplyCommandMode(hudFeedbackContext, mode),
                    (direction, boardAllInteractable) =>
                        selectionHudFeedbackSystem.ApplyBoardCommandMode(hudFeedbackContext, direction, boardAllInteractable),
                    result => selectionHudFeedbackSystem.ApplyCommandResult(hudFeedbackContext, result),
                    () => selectionHudFeedbackSystem.ClearSelection(hudFeedbackContext),
                    (entityManager, entity) => selectionHudFeedbackSystem.ApplySelection(hudFeedbackContext, entityManager, entity),
                    () => selectionHudFeedbackSystem.ClearCommandMode(hudFeedbackContext),
                    SetExplicitAttackTargetModeActive,
                    visible => selectionHudFeedbackSystem.SetWorldMarkersVisible(hudFeedbackContext, visible),
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
                            GetPointerTargetCommandContext(),
                            screenPosition,
                            entityManager,
                            out entity),
                    (Vector2 screenPosition, EntityManager entityManager, out int2 cell, out Vector3 worldPoint) =>
                        rtsSelectionPointerTargetCommandSystem.TryGetMoveCommandCell(
                            GetPointerTargetCommandContext(),
                            screenPosition,
                            entityManager,
                            out cell,
                            out worldPoint),
                    (Vector2 screenPosition, EntityManager entityManager, out int2 cell, out Vector3 worldPoint) =>
                        rtsSelectionPointerTargetCommandSystem.TryGetClickedCell(
                            GetPointerTargetCommandContext(),
                            screenPosition,
                            entityManager,
                            out cell,
                            out worldPoint),
                    (Vector2 screenPosition, EntityManager entityManager, out Entity entity) =>
                        rtsSelectionPointerTargetCommandSystem.TryGetClickedAttackTargetEntity(
                            GetPointerTargetCommandContext(),
                            screenPosition,
                            entityManager,
                            out entity),
                    (Vector2 screenPosition, EntityManager entityManager, out Entity entity) =>
                        rtsSelectionPointerTargetCommandSystem.TryGetClickedUnitEntity(
                            GetPointerTargetCommandContext(),
                            screenPosition,
                            entityManager,
                            out entity),
                    (Vector2 screenPosition, EntityManager entityManager, out int2 cell, out Vector3 worldPoint) =>
                        rtsSelectionPointerTargetCommandSystem.TryGetClickedCell(
                            GetPointerTargetCommandContext(),
                            screenPosition,
                            entityManager,
                            out cell,
                            out worldPoint));
            }

            RtsSelectionFocusCommandCompositionSystemHelper.Context CreateFocusCommandContext()
            {
                SelectionHudFeedbackUiSystemHelper.Context hudFeedbackContext = GetHudFeedbackContext();

                return new RtsSelectionFocusCommandCompositionSystemHelper.Context(
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
                    (entityManager, entity) => selectionHudFeedbackSystem.ApplySelection(hudFeedbackContext, entityManager, entity),
                    result => selectionHudFeedbackSystem.ApplyCommandResult(hudFeedbackContext, result),
                    mode => selectionHudFeedbackSystem.ApplyCommandMode(hudFeedbackContext, mode),
                    () => selectionHudFeedbackSystem.ClearSelection(hudFeedbackContext),
                    () => selectionHudFeedbackSystem.ClearCommandMode(hudFeedbackContext),
                    visible => selectionHudFeedbackSystem.SetWorldMarkersVisible(hudFeedbackContext, visible),
                    value => rtsSelectionRuntimeCameraSystem?.SetCameraDragging(GetRuntimeCameraContext(), value),
                    SetExplicitAttackTargetModeActive,
                    LogSelectionClickDiagnostic,
                    DescribeTransportBoardingEntity,
                    screenPosition => rtsSelectionPointerTargetCommandSystem.TryFocusUnit(
                        GetPointerTargetCommandContext(),
                        screenPosition));
            }

            RtsSelectionPointerTargetCommandCompositionSystemHelper.Context GetPointerTargetCommandContext()
            {
                if (!hasPointerTargetCommandContext)
                {
                    pointerTargetCommandContext = CreatePointerTargetCommandContext();
                    hasPointerTargetCommandContext = true;
                }

                return pointerTargetCommandContext;
            }

            RtsSelectionPointerTargetCommandCompositionSystemHelper.Context CreatePointerTargetCommandContext()
            {
                SelectionHudFeedbackUiSystemHelper.Context hudFeedbackContext = GetHudFeedbackContext();

                return new RtsSelectionPointerTargetCommandCompositionSystemHelper.Context(
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
                    mode => selectionHudFeedbackSystem.ApplyCommandMode(hudFeedbackContext, mode),
                    result => selectionHudFeedbackSystem.ApplyCommandResult(hudFeedbackContext, result),
                    () => selectionHudFeedbackSystem.ClearSelection(hudFeedbackContext),
                    () => selectionHudFeedbackSystem.ClearCommandMode(hudFeedbackContext),
                    (entityManager, entity) => selectionHudFeedbackSystem.ApplySelection(hudFeedbackContext, entityManager, entity),
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
                    selectionUiReadModelLookup,
                    visibleUnitSelectionSystem,
                    visibleSelectionScratch);
            }

            SelectionHudFeedbackUiSystemHelper.Context GetHudFeedbackContext()
            {
                if (!hasHudFeedbackContext)
                {
                    hudFeedbackContext = CreateHudFeedbackContext();
                    hasHudFeedbackContext = true;
                }

                return hudFeedbackContext;
            }

            SelectionHudFeedbackUiSystemHelper.Context CreateHudFeedbackContext()
            {
                return new SelectionHudFeedbackUiSystemHelper.Context(
                    selectionUiReadModelLookup,
                    TryGetDefaultEntityManager,
                    resolveSelectionPortraitSprite);
            }

            bool IsValidBoardTransportPreviewTarget(EntityManager em, Entity source, Entity target)
            {
                return rtsSelectionPointerTargetCommandSystem.IsValidBoardTransportPreviewTarget(
                    GetPointerTargetCommandContext(),
                    em,
                    source,
                    target);
            }

            bool IsValidBoardPassengerPreviewTarget(EntityManager em, Entity source, Entity target)
            {
                return rtsSelectionPointerTargetCommandSystem.IsValidBoardPassengerPreviewTarget(
                    GetPointerTargetCommandContext(),
                    em,
                    source,
                    target);
            }

            void RefreshFocusedUnit(EntityManager em, SelectionStateCompositionSystemHelper state)
            {
                focusedUnitLifecycleSystem.RefreshFocusedUnit(em, state, applyHudSelectionAction);
            }

            bool HasSelectedBuilding()
            {
                return buildingPlacementInteractionSystem != null &&
                       buildingPlacementInteractionSystem.HasSelectedBuilding(buildingPlacementInteractionContext);
            }

            string SelectedBuildingLabel()
            {
                return buildingPlacementInteractionSystem != null
                    ? buildingPlacementInteractionSystem.SelectedBuildingLabel(buildingPlacementInteractionContext)
                    : string.Empty;
            }

            bool TryGetSelectedBuildingResourceStorage(
                out int oilCurrent,
                out int oilCapacity,
                out int fuelCurrent,
                out int fuelCapacity)
            {
                oilCurrent = 0;
                oilCapacity = 0;
                fuelCurrent = 0;
                fuelCapacity = 0;
                return buildingPlacementInteractionSystem != null &&
                       buildingPlacementInteractionSystem.TryGetSelectedBuildingResourceStorage(
                           buildingPlacementInteractionContext,
                           out oilCurrent,
                           out oilCapacity,
                           out fuelCurrent,
                           out fuelCapacity);
            }

            bool IsBoardCommandAvailable(EntityManager em, Entity entity)
            {
                return rtsSelectionPointerTargetCommandSystem.IsBoardCommandAvailable(
                    GetPointerTargetCommandContext(),
                    em,
                    entity);
            }

            bool HasSelectedBoardAction(EntityManager em)
            {
                return rtsSelectionPointerTargetCommandSystem.HasSelectedBoardAction(
                    GetPointerTargetCommandContext(),
                    em);
            }

            void ApplyHudSelection(EntityManager entityManager, Entity entity)
            {
                selectionHudFeedbackSystem.ApplySelection(GetHudFeedbackContext(), entityManager, entity);
            }

            void ApplyHudSquadSelection(int selectedCount)
            {
                selectionHudFeedbackSystem.ApplySquadSelection(GetHudFeedbackContext(), selectedCount);
            }

            void ClearHudSelection()
            {
                selectionHudFeedbackSystem.ClearSelection(GetHudFeedbackContext());
            }

            MatchHudSquadTraySelectionUiSystemHelper.Context CreateSquadTraySelectionContext()
            {
                return new MatchHudSquadTraySelectionUiSystemHelper.Context(
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

            void EnsureSelectionRuntimeEntityQueries(EntityManager em)
            {
                Unity.Entities.World world = em.World;
                if (selectionRuntimeQueryWorld == world && world != null && world.IsCreated)
                    return;

                selectionRuntimeQueryWorld = world;
                selectedMoveQuery = em.CreateEntityQuery(
                    ComponentType.ReadOnly<SelectedUnitTag>(),
                    ComponentType.ReadOnly<UnitGrid>(),
                    ComponentType.ReadOnly<UnitMove>());
                moveTargetCommandQueueQuery = em.CreateEntityQuery(
                    ComponentType.ReadWrite<RtsSelectionInputStateComponent>(),
                    ComponentType.ReadWrite<RtsSelectionCommandIntentRequestElement>());
                moveTargetRuntimeStateQuery = em.CreateEntityQuery(ComponentType.ReadWrite<RuntimeGameplayStateComponent>());
                moveTargetSelectedMoveQuery = em.CreateEntityQuery(
                    ComponentType.ReadOnly<SelectedUnitTag>(),
                    ComponentType.ReadOnly<Faction>(),
                    ComponentType.ReadOnly<UnitGrid>(),
                    ComponentType.ReadOnly<UnitMove>(),
                    ComponentType.Exclude<Disabled>(),
                    ComponentType.Exclude<UnitTransportPassenger>());
                selectAllCommandQueueQuery = em.CreateEntityQuery(
                    ComponentType.ReadWrite<RtsSelectionInputRequestQueueComponent>(),
                    ComponentType.ReadWrite<RtsSelectionInputStateComponent>(),
                    ComponentType.ReadWrite<RtsSelectionCommandIntentRequestElement>(),
                    ComponentType.ReadWrite<RtsSelectionPointerRequestElement>());
                immediateRespawnQueueQuery = em.CreateEntityQuery(
                    ComponentType.ReadOnly<RespawnQueueTag>(),
                    ComponentType.ReadOnly<RespawnQueueComponent>());
                immediateBuildingRuntimeStateQuery = em.CreateEntityQuery(ComponentType.ReadOnly<BuildingRuntimeStateTag>());
                gridConfigQuery = em.CreateEntityQuery(ComponentType.ReadOnly<GridConfig>());
                selectedTagQuery = em.CreateEntityQuery(ComponentType.ReadOnly<SelectedUnitTag>());
                mapSurfaceQuery = em.CreateEntityQuery(ComponentType.ReadOnly<MapSurfaceComponent>());
            }

            bool TryGetDefaultEntityManager(out EntityManager em)
            {
                em = default;
                Unity.Entities.World world = Unity.Entities.World.DefaultGameObjectInjectionWorld;
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
                EnsureSelectionRuntimeEntityQueries(em);
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
                    SelectionRuntimeDiagnosticsSystemHelper.EnqueueSelectionDiagnosticMessage(message);
            }

            void LogSelectionClickDiagnostic(string message)
            {
                if (selectionRuntimeDiagnosticsSystem != null)
                    selectionRuntimeDiagnosticsSystem.LogSelectionClickDiagnostic(message);
                else
                    SelectionRuntimeDiagnosticsSystemHelper.LogSelectionClickDiagnosticMessage(message);
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
                    GetHudFeedbackContext(),
                    selectionStateSystem,
                    focusedUnitLifecycleSystem,
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
                    selectionUiReadModelLookup,
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
                VisibleUnitSelectionCameraSystemHelper.Filter filter = VisibleUnitSelectionCameraSystemHelper.Filter.All)
            {
                rtsSelectionInputSystem.QueueSelectionRectangleRequest(kind, screenRect, UnityEngine.Time.frameCount, filter);
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
                    SelectionRuntimeDiagnosticsSystemHelper.LogMoveCommandTrace(
                        $"gameplayUiHit source={source} pos={screenPosition} frame={UnityEngine.Time.frameCount}");
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
            Unity.Entities.World world = Unity.Entities.World.DefaultGameObjectInjectionWorld;
            return world != null && world.IsCreated
                ? world.GetOrCreateSystemManaged<RtsCameraRequestSystem>()
                : null;
        }

        private static RtsCameraSystem ResolveRtsCameraSystem()
        {
            Unity.Entities.World world = Unity.Entities.World.DefaultGameObjectInjectionWorld;
            return world != null && world.IsCreated
                ? world.GetOrCreateSystemManaged<RtsCameraSystem>()
                : null;
        }

        private static RtsSelectionRuntimeCameraSystemHelper ResolveRtsSelectionRuntimeCameraSystemHelper()
        {
            return new RtsSelectionRuntimeCameraSystemHelper();
        }

        private static SelectionRuntimeDiagnosticsSystemHelper ResolveSelectionRuntimeDiagnosticsSystem()
        {
            return new SelectionRuntimeDiagnosticsSystemHelper();
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

        private static Vector3 ToVector3(float3 value)
        {
            return new Vector3(value.x, value.y, value.z);
        }

        private static Quaternion ToQuaternion(quaternion value)
        {
            return new Quaternion(value.value.x, value.value.y, value.value.z, value.value.w);
        }
    }
}
