using Unity.Entities;
using Unity.Mathematics;
using Game.Tactical.Contracts;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Game.UI.Shell.Contracts.Ecs;
using Game.Components;
using Game.Runtime;

namespace Game.UI.Shell.Ecs
{
    internal static class UiActionRequestDispatchSystemHelper
    {
        internal static void ProcessRequest(
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
            ref UiResourceExchangeStateComponent resourceExchangeState,
            bool hasResourceExchangeState,
            ref BuildingUiPlacementCommandQueueComponent placementQueue,
            bool canOpenResourceExchange,
            EntityManager entityManager,
            Entity resourceExchangeRequestEntity,
            in ResourceExchangeEnabledComponent resourceExchangeRuntimeState,
            bool hasResourceExchangeRequestEntity,
            int frame,
            World world)
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
                case UiActionKind.OpenResourceExchange:
                    CaptureUiClickSequence(ref inputState, commandRequests, frame);
                    if (canOpenResourceExchange)
                        EnqueuePopup(popupRequests, UiShellPopupKind.ResourceExchange, UiShellPopupIntent.Show, request.PayloadId);
                    break;
                case UiActionKind.CloseResourceExchange:
                    CaptureUiClickSequence(ref inputState, commandRequests, frame);
                    EnqueuePopup(popupRequests, UiShellPopupKind.ResourceExchange, UiShellPopupIntent.Hide, request.PayloadId);
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
                case UiActionKind.ResourceExchangeTab:
                    CaptureUiClickSequence(ref inputState, commandRequests, frame);
                    if (hasResourceExchangeState &&
                        TryResolveResourceExchangeTab(request.PayloadId, out UiResourceExchangeTab exchangeTab))
                    {
                        resourceExchangeState.ActiveTab = exchangeTab;
                        resourceExchangeState.SelectedRecipeSlot = 0;
                        resourceExchangeState.SelectedInputAmount = 0;
                        resourceExchangeState.Version++;
                    }
                    break;
                case UiActionKind.ResourceExchangeRecipe:
                    CaptureUiClickSequence(ref inputState, commandRequests, frame);
                    if (hasResourceExchangeState)
                    {
                        resourceExchangeState.SelectedRecipeSlot = math.max(0, request.PayloadId);
                        resourceExchangeState.SelectedInputAmount = 0;
                        resourceExchangeState.Version++;
                    }
                    break;
                case UiActionKind.ResourceExchangeAmountDecrease:
                    CaptureUiClickSequence(ref inputState, commandRequests, frame);
                    UiActionResourceExchangeRequestSystemHelper.AdjustAmount(
                        entityManager,
                        resourceExchangeRequestEntity,
                        hasResourceExchangeRequestEntity,
                        ref resourceExchangeState,
                        hasResourceExchangeState,
                        -1);
                    break;
                case UiActionKind.ResourceExchangeAmountIncrease:
                    CaptureUiClickSequence(ref inputState, commandRequests, frame);
                    UiActionResourceExchangeRequestSystemHelper.AdjustAmount(
                        entityManager,
                        resourceExchangeRequestEntity,
                        hasResourceExchangeRequestEntity,
                        ref resourceExchangeState,
                        hasResourceExchangeState,
                        1);
                    break;
                case UiActionKind.ResourceExchangeConfirm:
                    CaptureUiClickSequence(ref inputState, commandRequests, frame);
                    UiActionResourceExchangeRequestSystemHelper.EnqueueConfirm(
                        entityManager,
                        resourceExchangeRequestEntity,
                        resourceExchangeRuntimeState,
                        hasResourceExchangeRequestEntity,
                        resourceExchangeState,
                        hasResourceExchangeState,
                        frame);
                    break;
                case UiActionKind.ResourceExchangeRushAll:
                    CaptureUiClickSequence(ref inputState, commandRequests, frame);
                    if (hasResourceExchangeRequestEntity)
                    {
                        ResourceExchangeRequestValidationSystem.EnqueueRushAllRequest(
                            entityManager,
                            resourceExchangeRequestEntity,
                            0,
                            resourceExchangeRuntimeState.FactionId,
                            frame);
                    }
                    break;
                case UiActionKind.ResourceExchangeClearCompleted:
                    CaptureUiClickSequence(ref inputState, commandRequests, frame);
                    if (hasResourceExchangeRequestEntity)
                    {
                        ResourceExchangeRequestValidationSystem.EnqueueClearCompletedRequest(
                            entityManager,
                            resourceExchangeRequestEntity,
                            resourceExchangeRuntimeState.FactionId,
                            frame);
                    }
                    break;
                case UiActionKind.ResourceExchangeQueueRush:
                    CaptureUiClickSequence(ref inputState, commandRequests, frame);
                    if (hasResourceExchangeRequestEntity && request.PayloadId > 0)
                    {
                        ResourceExchangeRequestValidationSystem.EnqueueRushRequest(
                            entityManager,
                            resourceExchangeRequestEntity,
                            request.PayloadId,
                            1,
                            resourceExchangeRuntimeState.FactionId,
                            frame);
                    }
                    break;
                case UiActionKind.ResourceExchangeQueueCancel:
                    CaptureUiClickSequence(ref inputState, commandRequests, frame);
                    if (hasResourceExchangeRequestEntity && request.PayloadId > 0)
                    {
                        ResourceExchangeRequestValidationSystem.EnqueueCancelRequest(
                            entityManager,
                            resourceExchangeRequestEntity,
                            request.PayloadId,
                            resourceExchangeRuntimeState.FactionId,
                            frame);
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
                    EmitDrawerAudio(world, passengerDrawerState.Visible != 0);
                    break;
                case UiActionKind.ClosePassengerDrawer:
                    CaptureUiClickSequence(ref inputState, commandRequests, frame);
                    passengerDrawerState.Visible = 0;
                    EmitDrawerAudio(world, open: false);
                    break;
                case UiActionKind.ExitAllPassengers:
                    CaptureUiClickSequence(ref inputState, commandRequests, frame);
                    passengerDrawerState.Visible = 0;
                    EmitDrawerAudio(world, open: false);
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

        private static void EmitDrawerAudio(World world, bool open)
        {
            UIAudioEventKind kind = open ? UIAudioEventKind.DrawerOpen : UIAudioEventKind.DrawerClose;
            if (UIAudioEventGateway.TryCreateRequest(kind, out UIAudioEventRequest request))
                UiAudioEventBridgeSystem.TryEnqueue(world, request);
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

        internal static bool HasBuildPlacementAction(DynamicBuffer<UiActionRequestComponent> actionRequests)
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

        private static bool TryResolveResourceExchangeTab(int payloadId, out UiResourceExchangeTab tab)
        {
            if (payloadId == (int)UiResourceExchangeTab.Import)
            {
                tab = UiResourceExchangeTab.Import;
                return true;
            }

            if (payloadId == (int)UiResourceExchangeTab.Export)
            {
                tab = UiResourceExchangeTab.Export;
                return true;
            }

            tab = UiResourceExchangeTab.Export;
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
