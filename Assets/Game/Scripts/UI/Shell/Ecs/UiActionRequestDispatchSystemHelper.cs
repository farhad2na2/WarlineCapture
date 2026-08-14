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
            ref UiDiagnosticsOverlayComponent diagnosticsOverlay,
            ref UiMatchHudPassengerDrawerStateComponent passengerDrawerState,
            ref UiMatchHudSquadTrayStateComponent squadTrayState,
            ref UiResourceExchangeStateComponent resourceExchangeState,
            bool hasResourceExchangeState,
            bool canPresentResourceExchange,
            EntityManager entityManager,
            Entity uiBoundary,
            Entity resourceExchangeRequestEntity,
            in ResourceExchangeEnabledComponent resourceExchangeRuntimeState,
            bool hasResourceExchangeRequestEntity,
            int frame,
            World world)
        {
            switch (request.Kind)
            {
                case UiActionKind.MatchMenu:
                    if (CampaignMissionExitDispatchUtility.TryHandle(
                            entityManager, uiBoundary, request.PayloadId))
                        break;
                    // Route changes issued from the pause modal must close that modal first.
                    // Otherwise the shell can begin its loading transition while the popup
                    // presentation sequence still owns the popup layer, leaving the loading
                    // sequence waiting indefinitely on packaged Android.
                    EnqueuePopup(popupRequests, UiShellPopupKind.Pause, UiShellPopupIntent.Hide, request.PayloadId);
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
                case UiActionKind.ClosePause:
                    EnqueuePopup(popupRequests, UiShellPopupKind.Pause, UiShellPopupIntent.Hide, request.PayloadId);
                    break;
                case UiActionKind.OpenSettings:
                    EnqueuePopup(popupRequests, UiShellPopupKind.Settings, UiShellPopupIntent.Show, request.PayloadId);
                    break;
                case UiActionKind.OpenResourceExchange:
                    CaptureUiClickSequence(ref inputState, commandRequests, frame);
                    if (canPresentResourceExchange)
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
