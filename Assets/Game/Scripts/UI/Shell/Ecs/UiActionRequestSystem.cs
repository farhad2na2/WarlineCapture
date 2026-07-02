using Unity.Collections;
using Unity.Entities;
using Game.Tactical.Contracts;
using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;
using Game.Components;

namespace Game.UI.Shell.Ecs
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct UiActionRequestSystem : ISystem
    {
        private EntityQuery boundaryQuery;
        private EntityQuery selectionInputQuery;
        private EntityQuery buildingPlacementCommandQuery;

        public void OnCreate(ref SystemState state)
        {
            boundaryQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<UiShellStateComponent>(),
                ComponentType.ReadWrite<UiActionRequestComponent>(),
                ComponentType.ReadWrite<UiDiagnosticsOverlayComponent>(),
                ComponentType.ReadWrite<UiMatchHudPassengerDrawerStateComponent>(),
                ComponentType.ReadWrite<UiMatchHudSquadTrayStateComponent>(),
                ComponentType.ReadWrite<UiBuildDrawerStateComponent>(),
                ComponentType.ReadWrite<UiBuildCatalogRequestComponent>(),
                ComponentType.ReadWrite<UiBuildProductionRequestComponent>(),
                ComponentType.ReadWrite<UiBuildPrimaryRequestComponent>(),
                ComponentType.ReadWrite<UiShellPopupRequestComponent>(),
                ComponentType.ReadWrite<UiShellRouteRequestComponent>());
            selectionInputQuery = state.GetEntityQuery(
                ComponentType.ReadWrite<RtsSelectionInputStateComponent>(),
                ComponentType.ReadWrite<RtsSelectionInputRequestQueueComponent>(),
                ComponentType.ReadWrite<RtsSelectionCommandIntentRequestElement>());
            buildingPlacementCommandQuery = state.GetEntityQuery(
                ComponentType.ReadWrite<BuildingUiPlacementCommandQueueComponent>(),
                ComponentType.ReadWrite<BuildingUiPlacementCommandRequestElement>());
            state.RequireForUpdate(boundaryQuery);
        }

        public void OnUpdate(ref SystemState state)
        {
            Entity boundary = ResolveFirstEntity(boundaryQuery);
            DynamicBuffer<UiActionRequestComponent> actionRequests = state.EntityManager.GetBuffer<UiActionRequestComponent>(boundary);
            if (actionRequests.Length == 0)
                return;

            if (!TryResolveSelectionInputEntity(ref state, out Entity selectionInput))
                return;

            DynamicBuffer<RtsSelectionCommandIntentRequestElement> commandRequests =
                state.EntityManager.GetBuffer<RtsSelectionCommandIntentRequestElement>(selectionInput);
            DynamicBuffer<UiShellPopupRequestComponent> popupRequests =
                state.EntityManager.GetBuffer<UiShellPopupRequestComponent>(boundary);
            DynamicBuffer<UiShellRouteRequestComponent> routeRequests =
                state.EntityManager.GetBuffer<UiShellRouteRequestComponent>(boundary);
            DynamicBuffer<UiBuildCatalogRequestComponent> buildCatalogRequests =
                state.EntityManager.GetBuffer<UiBuildCatalogRequestComponent>(boundary);
            DynamicBuffer<UiBuildProductionRequestComponent> buildProductionRequests =
                state.EntityManager.GetBuffer<UiBuildProductionRequestComponent>(boundary);
            DynamicBuffer<UiBuildPrimaryRequestComponent> buildPrimaryRequests =
                state.EntityManager.GetBuffer<UiBuildPrimaryRequestComponent>(boundary);
            UiMatchHudPassengerDrawerStateComponent passengerDrawerState =
                state.EntityManager.GetComponentData<UiMatchHudPassengerDrawerStateComponent>(boundary);
            UiMatchHudSquadTrayStateComponent squadTrayState =
                state.EntityManager.GetComponentData<UiMatchHudSquadTrayStateComponent>(boundary);
            UiBuildDrawerStateComponent buildDrawerState =
                state.EntityManager.GetComponentData<UiBuildDrawerStateComponent>(boundary);
            UiDiagnosticsOverlayComponent diagnosticsOverlay =
                state.EntityManager.GetComponentData<UiDiagnosticsOverlayComponent>(boundary);
            RtsSelectionInputStateComponent inputState =
                state.EntityManager.GetComponentData<RtsSelectionInputStateComponent>(selectionInput);
            RtsSelectionInputRequestQueueComponent queue =
                state.EntityManager.GetComponentData<RtsSelectionInputRequestQueueComponent>(selectionInput);
            bool needsPlacementCommandQueue = HasBuildPlacementAction(actionRequests);
            Entity placementCommand = Entity.Null;
            DynamicBuffer<BuildingUiPlacementCommandRequestElement> placementRequests = default;
            BuildingUiPlacementCommandQueueComponent placementQueue = default;
            if (needsPlacementCommandQueue)
            {
                if (!TryResolveBuildingPlacementCommandEntity(ref state, out placementCommand))
                    return;

                placementRequests = state.EntityManager.GetBuffer<BuildingUiPlacementCommandRequestElement>(placementCommand);
                placementQueue = state.EntityManager.GetComponentData<BuildingUiPlacementCommandQueueComponent>(placementCommand);
            }
            int frame = UnityEngine.Time.frameCount;

            for (int i = 0; i < actionRequests.Length; i++)
            {
                ProcessRequest(
                    actionRequests[i],
                    ref inputState,
                    ref queue,
                    commandRequests,
                    popupRequests,
                    routeRequests,
                    buildCatalogRequests,
                    buildProductionRequests,
                    buildPrimaryRequests,
                    placementRequests,
                    ref diagnosticsOverlay,
                    ref passengerDrawerState,
                    ref squadTrayState,
                    ref buildDrawerState,
                    ref placementQueue,
                    frame);
            }

            actionRequests.Clear();
            state.EntityManager.SetComponentData(boundary, passengerDrawerState);
            state.EntityManager.SetComponentData(boundary, squadTrayState);
            state.EntityManager.SetComponentData(boundary, buildDrawerState);
            state.EntityManager.SetComponentData(boundary, diagnosticsOverlay);
            state.EntityManager.SetComponentData(selectionInput, inputState);
            state.EntityManager.SetComponentData(selectionInput, queue);
            if (needsPlacementCommandQueue)
                state.EntityManager.SetComponentData(placementCommand, placementQueue);
        }

        private bool TryResolveSelectionInputEntity(ref SystemState state, out Entity entity)
        {
            entity = Entity.Null;
            if (!selectionInputQuery.IsEmptyIgnoreFilter)
            {
                entity = ResolveFirstEntity(selectionInputQuery);
                EnsureSelectionBuffers(ref state, entity);
                return true;
            }

            entity = state.EntityManager.CreateEntity(
                typeof(RtsSelectionInputStateComponent),
                typeof(RtsSelectionInputRequestQueueComponent));
            state.EntityManager.SetComponentData(entity, new RtsSelectionInputStateComponent
            {
                QueuedMoveOrderFrame = -1
            });
            EnsureSelectionBuffers(ref state, entity);
            return false;
        }

        private static void EnsureSelectionBuffers(ref SystemState state, Entity entity)
        {
            if (!state.EntityManager.HasBuffer<RtsSelectionPointerRequestElement>(entity))
                state.EntityManager.AddBuffer<RtsSelectionPointerRequestElement>(entity);
            if (!state.EntityManager.HasBuffer<RtsSelectionCommandIntentRequestElement>(entity))
                state.EntityManager.AddBuffer<RtsSelectionCommandIntentRequestElement>(entity);
            if (!state.EntityManager.HasBuffer<RtsSelectionCommandResultElement>(entity))
                state.EntityManager.AddBuffer<RtsSelectionCommandResultElement>(entity);
        }

        private bool TryResolveBuildingPlacementCommandEntity(ref SystemState state, out Entity entity)
        {
            entity = Entity.Null;
            if (!buildingPlacementCommandQuery.IsEmptyIgnoreFilter)
            {
                entity = ResolveFirstEntity(buildingPlacementCommandQuery);
                EnsureBuildingPlacementCommandBuffers(ref state, entity);
                return true;
            }

            entity = state.EntityManager.CreateEntity(typeof(BuildingUiPlacementCommandQueueComponent));
            state.EntityManager.SetName(entity, "BuildingUiPlacementCommands");
            state.EntityManager.SetComponentData(entity, new BuildingUiPlacementCommandQueueComponent
            {
                LastRequestId = 0
            });
            EnsureBuildingPlacementCommandBuffers(ref state, entity);
            return false;
        }

        private static void EnsureBuildingPlacementCommandBuffers(ref SystemState state, Entity entity)
        {
            if (!state.EntityManager.HasBuffer<BuildingUiPlacementCommandRequestElement>(entity))
                state.EntityManager.AddBuffer<BuildingUiPlacementCommandRequestElement>(entity);
            if (!state.EntityManager.HasBuffer<BuildingUiPlacementCommandResultElement>(entity))
                state.EntityManager.AddBuffer<BuildingUiPlacementCommandResultElement>(entity);
        }

        private static Entity ResolveFirstEntity(EntityQuery query)
        {
            if (query.CalculateEntityCount() == 1)
                return query.GetSingletonEntity();

            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            return entities.Length > 0 ? entities[0] : Entity.Null;
        }

        private static void ProcessRequest(
            UiActionRequestComponent request,
            ref RtsSelectionInputStateComponent inputState,
            ref RtsSelectionInputRequestQueueComponent queue,
            DynamicBuffer<RtsSelectionCommandIntentRequestElement> commandRequests,
            DynamicBuffer<UiShellPopupRequestComponent> popupRequests,
            DynamicBuffer<UiShellRouteRequestComponent> routeRequests,
            DynamicBuffer<UiBuildCatalogRequestComponent> buildCatalogRequests,
            DynamicBuffer<UiBuildProductionRequestComponent> buildProductionRequests,
            DynamicBuffer<UiBuildPrimaryRequestComponent> buildPrimaryRequests,
            DynamicBuffer<BuildingUiPlacementCommandRequestElement> placementRequests,
            ref UiDiagnosticsOverlayComponent diagnosticsOverlay,
            ref UiMatchHudPassengerDrawerStateComponent passengerDrawerState,
            ref UiMatchHudSquadTrayStateComponent squadTrayState,
            ref UiBuildDrawerStateComponent buildDrawerState,
            ref BuildingUiPlacementCommandQueueComponent placementQueue,
            int frame)
        {
            switch (request.Kind)
            {
                case UiActionKind.MatchMenu:
                    routeRequests.Add(new UiShellRouteRequestComponent
                    {
                        Route = UIRoute.MainMenu,
                        Intent = UiShellRouteIntent.ReturnToMainMenu,
                        PushHistory = 0
                    });
                    break;
                case UiActionKind.Pause:
                    EnqueuePopup(popupRequests, UiShellPopupKind.Pause, UiShellPopupIntent.Show, request.PayloadId);
                    break;
                case UiActionKind.OpenSettings:
                    EnqueuePopup(popupRequests, UiShellPopupKind.Settings, UiShellPopupIntent.Show, request.PayloadId);
                    break;
                case UiActionKind.RightBuild:
                case UiActionKind.Build:
                    CaptureUiClickSequence(ref inputState, commandRequests, frame);
                    EnqueuePopup(popupRequests, UiShellPopupKind.BuildDrawer, UiShellPopupIntent.Show, request.PayloadId);
                    break;
                case UiActionKind.CloseBuildDrawer:
                    CaptureUiClickSequence(ref inputState, commandRequests, frame);
                    EnqueuePopup(popupRequests, UiShellPopupKind.BuildDrawer, UiShellPopupIntent.Hide, request.PayloadId);
                    EnqueueSelectionIntent(ref queue, commandRequests, RtsSelectionCommandIntentKind.CancelActiveCommandMode, frame);
                    break;
                case UiActionKind.BuildCatalogItem:
                    CaptureUiClickSequence(ref inputState, commandRequests, frame);
                    queue.LastRequestId++;
                    buildDrawerState.SelectedCatalogSlot = request.PayloadId;
                    buildCatalogRequests.Add(new UiBuildCatalogRequestComponent
                    {
                        CatalogSlot = request.PayloadId,
                        RequestId = queue.LastRequestId
                    });
                    break;
                case UiActionKind.BuildDrawerTab:
                    CaptureUiClickSequence(ref inputState, commandRequests, frame);
                    if (TryResolveBuildDrawerCategory(request.PayloadId, out BuildDrawerCategory category))
                    {
                        buildDrawerState.ActiveCategory = category;
                        buildDrawerState.SelectedCatalogSlot = 0;
                    }
                    break;
                case UiActionKind.BuildDrawerPrimaryBuild:
                    CaptureUiClickSequence(ref inputState, commandRequests, frame);
                    queue.LastRequestId++;
                    buildPrimaryRequests.Add(new UiBuildPrimaryRequestComponent
                    {
                        RequestId = queue.LastRequestId
                    });
                    break;
                case UiActionKind.BuildProductionRush:
                    CaptureUiClickSequence(ref inputState, commandRequests, frame);
                    EnqueueBuildProductionRequest(ref queue, buildProductionRequests, UiBuildProductionActionKind.Rush, 0);
                    break;
                case UiActionKind.BuildProductionClear:
                    CaptureUiClickSequence(ref inputState, commandRequests, frame);
                    EnqueueBuildProductionRequest(ref queue, buildProductionRequests, UiBuildProductionActionKind.Clear, 0);
                    break;
                case UiActionKind.BuildProductionCancelActive:
                    CaptureUiClickSequence(ref inputState, commandRequests, frame);
                    EnqueueBuildProductionRequest(ref queue, buildProductionRequests, UiBuildProductionActionKind.CancelActive, 0);
                    break;
                case UiActionKind.BuildProductionCancelQueued:
                    CaptureUiClickSequence(ref inputState, commandRequests, frame);
                    EnqueueBuildProductionRequest(ref queue, buildProductionRequests, UiBuildProductionActionKind.CancelQueued, request.PayloadId);
                    break;
                case UiActionKind.BuildPlacementConfirm:
                    CaptureUiClickSequence(ref inputState, commandRequests, frame);
                    EnqueueBuildPlacementRequest(
                        ref placementQueue,
                        placementRequests,
                        BuildingUiPlacementCommandRequestElement.KindConfirmPlacement,
                        true);
                    break;
                case UiActionKind.BuildPlacementCancel:
                    CaptureUiClickSequence(ref inputState, commandRequests, frame);
                    EnqueueBuildPlacementRequest(
                        ref placementQueue,
                        placementRequests,
                        BuildingUiPlacementCommandRequestElement.KindCancelPlacement,
                        true);
                    break;
                case UiActionKind.BuildPlacementRotate:
                    CaptureUiClickSequence(ref inputState, commandRequests, frame);
                    EnqueueBuildPlacementRequest(
                        ref placementQueue,
                        placementRequests,
                        BuildingUiPlacementCommandRequestElement.KindRotatePlacement,
                        false);
                    break;
                case UiActionKind.Select:
                    CaptureUiClickSequence(ref inputState, commandRequests, frame);
                    EnqueueSelectionIntent(
                        ref queue,
                        commandRequests,
                        IsActiveMode(inputState, TacticalCommandMode.Select)
                            ? RtsSelectionCommandIntentKind.ExitSelectionMode
                            : RtsSelectionCommandIntentKind.EnterSelectionMode,
                        frame);
                    break;
                case UiActionKind.Move:
                    CaptureUiClickSequence(ref inputState, commandRequests, frame);
                    EnqueueSelectionIntent(ref queue, commandRequests, RtsSelectionCommandIntentKind.EnterMoveTargetMode, frame);
                    break;
                case UiActionKind.Attack:
                    CaptureUiClickSequence(ref inputState, commandRequests, frame);
                    EnqueueSelectionIntent(ref queue, commandRequests, RtsSelectionCommandIntentKind.EnterAttackTargetMode, frame);
                    break;
                case UiActionKind.Hold:
                    CaptureUiClickSequence(ref inputState, commandRequests, frame);
                    EnqueueSelectionIntent(ref queue, commandRequests, RtsSelectionCommandIntentKind.HoldPosition, frame);
                    break;
                case UiActionKind.Stop:
                    CaptureUiClickSequence(ref inputState, commandRequests, frame);
                    EnqueueSelectionIntent(ref queue, commandRequests, RtsSelectionCommandIntentKind.Stop, frame);
                    break;
                case UiActionKind.Scan:
                case UiActionKind.Support:
                case UiActionKind.RightSupport:
                    CaptureUiClickSequence(ref inputState, commandRequests, frame);
                    EnqueueSelectionIntent(ref queue, commandRequests, RtsSelectionCommandIntentKind.EnterScanTargetMode, frame);
                    break;
                case UiActionKind.SquadSlot1:
                case UiActionKind.SquadSlot2:
                case UiActionKind.SquadSlot3:
                case UiActionKind.SquadSlot4:
                case UiActionKind.SquadSlot5:
                    CaptureUiClickSequence(ref inputState, commandRequests, frame);
                    squadTrayState.SelectedSlot = ToSquadTraySlot(request.Kind);
                    break;
                case UiActionKind.ReturnSelection:
                    CaptureUiClickSequence(ref inputState, commandRequests, frame);
                    EnqueueSelectionIntent(ref queue, commandRequests, RtsSelectionCommandIntentKind.ReturnToBase, frame);
                    break;
                case UiActionKind.DestroySelection:
                    CaptureUiClickSequence(ref inputState, commandRequests, frame);
                    EnqueueSelectionIntent(ref queue, commandRequests, RtsSelectionCommandIntentKind.DestroyFocusedUnit, frame);
                    break;
                case UiActionKind.BoardSelection:
                    CaptureUiClickSequence(ref inputState, commandRequests, frame);
                    EnqueueSelectionIntent(ref queue, commandRequests, RtsSelectionCommandIntentKind.EnterBoardTargetMode, frame);
                    break;
                case UiActionKind.TogglePassengerDrawer:
                    CaptureUiClickSequence(ref inputState, commandRequests, frame);
                    passengerDrawerState.Visible = passengerDrawerState.Visible == 0 ? (byte)1 : (byte)0;
                    break;
                case UiActionKind.ClosePassengerDrawer:
                    CaptureUiClickSequence(ref inputState, commandRequests, frame);
                    passengerDrawerState.Visible = 0;
                    break;
                case UiActionKind.ExitAllPassengers:
                    CaptureUiClickSequence(ref inputState, commandRequests, frame);
                    passengerDrawerState.Visible = 0;
                    EnqueueSelectionIntent(ref queue, commandRequests, RtsSelectionCommandIntentKind.DisembarkTransportPassenger, frame);
                    break;
                case UiActionKind.BoardAll:
                    CaptureUiClickSequence(ref inputState, commandRequests, frame);
                    EnqueueSelectionIntent(ref queue, commandRequests, RtsSelectionCommandIntentKind.BoardAllSelectedTransport, frame);
                    break;
                case UiActionKind.CancelFeedback:
                    CaptureUiClickSequence(ref inputState, commandRequests, frame);
                    EnqueueSelectionIntent(ref queue, commandRequests, RtsSelectionCommandIntentKind.CancelActiveCommandMode, frame);
                    break;
                case UiActionKind.ToggleDiagnosticsOverlay:
                    diagnosticsOverlay.LogVisible = diagnosticsOverlay.LogVisible == 0 ? (byte)1 : (byte)0;
                    break;
                case UiActionKind.CloseDiagnosticsOverlay:
                    diagnosticsOverlay.LogVisible = 0;
                    break;
            }
        }

        private static void CaptureUiClickSequence(
            ref RtsSelectionInputStateComponent inputState,
            DynamicBuffer<RtsSelectionCommandIntentRequestElement> commandRequests,
            int frame)
        {
            inputState.QueuedMoveOrderToken++;
            inputState.HasQueuedMoveOrder = 0;
            inputState.QueuedMoveOrderScreenPosition = default;
            inputState.QueuedMoveOrderFrame = -1;
            int ignoreUntilFrame = frame + 1;
            if (inputState.IgnoreWorldCommandsUntilFrame < ignoreUntilFrame)
                inputState.IgnoreWorldCommandsUntilFrame = ignoreUntilFrame;
            inputState.IgnoreUiClickUntilRelease = 1;
            inputState.IgnoreNextLeftMouseRelease = 1;
            inputState.PointerPressedOverUi = 1;
            inputState.IsDraggingSelection = 0;
            inputState.HasLiveSelectionRect = 0;
            inputState.BoardPassengerDragArmed = 0;
            ClearPendingMoveRequests(commandRequests);
        }

        private static void ClearPendingMoveRequests(DynamicBuffer<RtsSelectionCommandIntentRequestElement> commandRequests)
        {
            for (int i = commandRequests.Length - 1; i >= 0; i--)
            {
                if (commandRequests[i].Kind == RtsSelectionCommandIntentKind.Move)
                    commandRequests.RemoveAt(i);
            }
        }

        private static bool IsActiveMode(RtsSelectionInputStateComponent inputState, TacticalCommandMode mode)
        {
            return (TacticalCommandMode)inputState.ActiveCommandMode == mode;
        }

        private static void EnqueueSelectionIntent(
            ref RtsSelectionInputRequestQueueComponent queue,
            DynamicBuffer<RtsSelectionCommandIntentRequestElement> commandRequests,
            RtsSelectionCommandIntentKind kind,
            int frame)
        {
            queue.LastRequestId++;
            commandRequests.Add(new RtsSelectionCommandIntentRequestElement
            {
                Kind = kind,
                RequestId = queue.LastRequestId,
                Frame = frame
            });
        }

        private static void EnqueueBuildProductionRequest(
            ref RtsSelectionInputRequestQueueComponent queue,
            DynamicBuffer<UiBuildProductionRequestComponent> buildProductionRequests,
            UiBuildProductionActionKind actionKind,
            int queueSlot)
        {
            queue.LastRequestId++;
            buildProductionRequests.Add(new UiBuildProductionRequestComponent
            {
                ActionKind = actionKind,
                QueueSlot = queueSlot,
                RequestId = queue.LastRequestId
            });
        }

        private static void EnqueueBuildPlacementRequest(
            ref BuildingUiPlacementCommandQueueComponent queue,
            DynamicBuffer<BuildingUiPlacementCommandRequestElement> placementRequests,
            byte requestKind,
            bool clearBuildingSelection)
        {
            queue.LastRequestId++;
            placementRequests.Add(new BuildingUiPlacementCommandRequestElement
            {
                RequestId = queue.LastRequestId,
                BuildingId = default,
                RequestKind = requestKind,
                ClearBuildingSelection = clearBuildingSelection ? (byte)1 : (byte)0
            });
        }

        private static bool HasBuildPlacementAction(DynamicBuffer<UiActionRequestComponent> actionRequests)
        {
            for (int i = 0; i < actionRequests.Length; i++)
            {
                if (actionRequests[i].Kind is
                    UiActionKind.BuildPlacementConfirm or
                    UiActionKind.BuildPlacementCancel or
                    UiActionKind.BuildPlacementRotate)
                {
                    return true;
                }
            }

            return false;
        }

        private static MatchHudSquadTraySlot ToSquadTraySlot(UiActionKind kind)
        {
            return kind switch
            {
                UiActionKind.SquadSlot1 => MatchHudSquadTraySlot.Soldiers,
                UiActionKind.SquadSlot2 => MatchHudSquadTraySlot.CombatVehicles,
                UiActionKind.SquadSlot3 => MatchHudSquadTraySlot.AttackHelicopter,
                UiActionKind.SquadSlot4 => MatchHudSquadTraySlot.Jet,
                UiActionKind.SquadSlot5 => MatchHudSquadTraySlot.Transport,
                _ => MatchHudSquadTraySlot.None
            };
        }

        private static bool TryResolveBuildDrawerCategory(int payloadId, out BuildDrawerCategory category)
        {
            if (payloadId >= (int)BuildDrawerCategory.Buildings &&
                payloadId <= (int)BuildDrawerCategory.Soldiers)
            {
                category = (BuildDrawerCategory)payloadId;
                return true;
            }

            category = BuildDrawerCategory.Buildings;
            return false;
        }

        private static void EnqueuePopup(
            DynamicBuffer<UiShellPopupRequestComponent> popupRequests,
            UiShellPopupKind kind,
            UiShellPopupIntent intent,
            int payloadId)
        {
            popupRequests.Add(new UiShellPopupRequestComponent
            {
                PopupKind = kind,
                Intent = intent,
                PayloadId = payloadId
            });
        }
    }
}
