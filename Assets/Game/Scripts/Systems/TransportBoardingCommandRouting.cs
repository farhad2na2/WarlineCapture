using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Game.Configs;
using Game.Tactical.Contracts;
using Game.Components;

namespace Game.Runtime
{
    public partial struct TransportBoardingCommandSystem
    {
        public bool ProcessCommandIntentRequests(
            EntityManager em,
            Entity commandEntity,
            DynamicBuffer<RtsSelectionCommandIntentRequestElement> commandRequests,
            DynamicBuffer<RtsSelectionCommandResultElement> commandResults,
            UnitTransportCapacitySystem transportCapacitySystem,
            UnitTransportAirPickupSystem transportAirPickupSystem,
            UnitMoveOrderSystem moveOrderSystem,
            SelectionStateCompositionSystemHelper selectionStateSystem,
            TryGetClickedUnitEntityDelegate tryGetClickedUnitEntity,
            TryGetClickedCellDelegate tryGetClickedCell)
        {
#if UNITY_EDITOR
            long allocationProbeStartBytes = System.GC.GetAllocatedBytesForCurrentThread();
            bool allocationProbeHandled = false;
            try
            {
#endif
            EnsureEntityQueries(em);
            bool handledAny = false;
            for (int i = 0; i < commandRequests.Length;)
            {
                RtsSelectionCommandIntentRequestElement request = commandRequests[i];
                if (!TransportBoardingCommandRoutingSystemHelper.IsTransportCommandIntent(request.Kind))
                {
                    i++;
                    continue;
                }

                commandRequests.RemoveAt(i);
                handledAny = true;
                RtsSelectionCommandResultElement result = request.Kind switch
                {
                    RtsSelectionCommandIntentKind.BoardTransport => request.HasTargetEntity != 0
                        ? ProcessBoardTransportTargetRequest(
                            em,
                            request,
                            transportAirPickupSystem,
                            moveOrderSystem,
                            selectionStateSystem)
                        : ProcessBoardTransportRequest(
                            em,
                            request,
                            transportAirPickupSystem,
                            moveOrderSystem,
                            selectionStateSystem,
                            tryGetClickedUnitEntity,
                            tryGetClickedCell),
                    RtsSelectionCommandIntentKind.BoardSelectedTransport => ProcessBoardSelectedTransportRequest(
                        em,
                        request,
                        transportAirPickupSystem,
                        moveOrderSystem,
                        tryGetClickedUnitEntity),
                    RtsSelectionCommandIntentKind.BoardSelectedTransportPassenger => ProcessBoardSelectedTransportPassengerRequest(
                        em,
                        request,
                        transportAirPickupSystem,
                        moveOrderSystem),
                    RtsSelectionCommandIntentKind.BoardNearestSoldiers => ProcessBoardAllSelectedTransportRequest(
                        em,
                        request,
                        transportCapacitySystem,
                        selectionStateSystem),
                    RtsSelectionCommandIntentKind.BoardAllSelectedTransport => ProcessBoardAllSelectedTransportRequest(
                        em,
                        request,
                        transportCapacitySystem,
                        selectionStateSystem),
                    RtsSelectionCommandIntentKind.DisembarkTransportPassenger => ProcessDisembarkTransportPassengerRequest(
                        em,
                        request,
                        transportCapacitySystem,
                        moveOrderSystem),
                    _ => ProcessDisembarkTransportRequest(
                        em,
                        request,
                        transportCapacitySystem,
                        moveOrderSystem)
                };
                TransportBoardingCommandRoutingSystemHelper.AddCommandResult(em, commandEntity, commandResults, result);
                TransportBoardingCommandRoutingSystemHelper.RefreshCommandBuffers(em, commandEntity, ref commandRequests, ref commandResults);
            }

#if UNITY_EDITOR
            allocationProbeHandled = handledAny;
#endif
            return handledAny;
#if UNITY_EDITOR
            }
            finally
            {
                RuntimeDiagnosticsSystem.RecordEditorTransportBoardingCommandAllocation(
                    System.GC.GetAllocatedBytesForCurrentThread() - allocationProbeStartBytes,
                    allocationProbeHandled);
            }
#endif
        }

        private bool ProcessPreResolvedTransportRequests(EntityManager em)
        {
            if (_commandQueueQuery.IsEmptyIgnoreFilter)
                return false;

            Entity commandEntity = _commandQueueQuery.GetSingletonEntity();
            DynamicBuffer<RtsSelectionCommandIntentRequestElement> commandRequests =
                em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
            DynamicBuffer<RtsSelectionCommandResultElement> commandResults =
                em.GetBuffer<RtsSelectionCommandResultElement>(commandEntity);
            bool handledAny = false;
            var transportCapacitySystem = new UnitTransportCapacitySystem();
            var transportAirPickupSystem = new UnitTransportAirPickupSystem();
            var moveOrderSystem = new UnitMoveOrderSystem();
            var selectionStateSystem = new SelectionStateCompositionSystemHelper();

            for (int i = 0; i < commandRequests.Length;)
            {
                RtsSelectionCommandIntentRequestElement request = commandRequests[i];
                if (!TransportBoardingCommandRoutingSystemHelper.IsPreResolvedTransportCommandIntent(request))
                {
                    i++;
                    continue;
                }

                commandRequests.RemoveAt(i);
                handledAny = true;
                RtsSelectionCommandResultElement result = request.Kind switch
                {
                    RtsSelectionCommandIntentKind.BoardTransport => ProcessBoardTransportTargetRequest(
                        em,
                        request,
                        transportAirPickupSystem,
                        moveOrderSystem,
                        selectionStateSystem),
                    RtsSelectionCommandIntentKind.BoardSelectedTransportPassenger => ProcessBoardSelectedTransportPassengerRequest(
                        em,
                        request,
                        transportAirPickupSystem,
                        moveOrderSystem),
                    RtsSelectionCommandIntentKind.DisembarkTransportPassenger => ProcessDisembarkTransportPassengerRequest(
                        em,
                        request,
                        transportCapacitySystem,
                        moveOrderSystem),
                    _ => ProcessDisembarkTransportRequest(
                        em,
                        request,
                        transportCapacitySystem,
                        moveOrderSystem)
                };

                TransportBoardingCommandRoutingSystemHelper.AddCommandResult(em, commandEntity, commandResults, result);
                TransportBoardingCommandRoutingSystemHelper.RefreshCommandBuffers(em, commandEntity, ref commandRequests, ref commandResults);
            }

            return handledAny;
        }

        private RtsSelectionCommandResultElement ProcessBoardTransportRequest(
            EntityManager em,
            RtsSelectionCommandIntentRequestElement request,
            UnitTransportAirPickupSystem transportAirPickupSystem,
            UnitMoveOrderSystem moveOrderSystem,
            SelectionStateCompositionSystemHelper selectionStateSystem,
            TryGetClickedUnitEntityDelegate tryGetClickedUnitEntity,
            TryGetClickedCellDelegate tryGetClickedCell)
        {
            Vector2 screenPosition = new(request.ScreenPosition.x, request.ScreenPosition.y);
            Result result = TryRequestBoardTransportOrderToClickedUnit(
                em,
                screenPosition,
                transportAirPickupSystem,
                moveOrderSystem,
                selectionStateSystem,
                tryGetClickedUnitEntity,
                tryGetClickedCell);

            return TransportBoardingCommandRoutingSystemHelper.ToBoardingCommandResultElement(request, result);
        }

        private RtsSelectionCommandResultElement ProcessBoardTransportTargetRequest(
            EntityManager em,
            RtsSelectionCommandIntentRequestElement request,
            UnitTransportAirPickupSystem transportAirPickupSystem,
            UnitMoveOrderSystem moveOrderSystem,
            SelectionStateCompositionSystemHelper selectionStateSystem)
        {
            Result result = request.HasTargetEntity != 0
                ? TryIssueBoardTransportOrderToTransport(
                    em,
                    request.TargetEntity,
                    transportAirPickupSystem,
                    moveOrderSystem,
                    selectionStateSystem)
                : Result.Rejected();

            return TransportBoardingCommandRoutingSystemHelper.ToBoardingCommandResultElement(request, result);
        }

        private RtsSelectionCommandResultElement ProcessBoardSelectedTransportRequest(
            EntityManager em,
            RtsSelectionCommandIntentRequestElement request,
            UnitTransportAirPickupSystem transportAirPickupSystem,
            UnitMoveOrderSystem moveOrderSystem,
            TryGetClickedUnitEntityDelegate tryGetClickedUnitEntity)
        {
            Vector2 screenPosition = new(request.ScreenPosition.x, request.ScreenPosition.y);
            Result result = TryIssueBoardSelectedTransportOrderToClickedPassenger(
                em,
                request.TargetEntity,
                screenPosition,
                transportAirPickupSystem,
                moveOrderSystem,
                tryGetClickedUnitEntity);

            return TransportBoardingCommandRoutingSystemHelper.ToBoardingCommandResultElement(request, result);
        }

        private RtsSelectionCommandResultElement ProcessBoardSelectedTransportPassengerRequest(
            EntityManager em,
            RtsSelectionCommandIntentRequestElement request,
            UnitTransportAirPickupSystem transportAirPickupSystem,
            UnitMoveOrderSystem moveOrderSystem)
        {
            Result result = request.HasTargetEntity != 0 &&
                request.HasSecondaryTargetEntity != 0
                ? TryIssueBoardSelectedTransportOrderToPassenger(
                    em,
                    request.TargetEntity,
                    request.SecondaryTargetEntity,
                    transportAirPickupSystem,
                    moveOrderSystem)
                : Result.Rejected();

            return TransportBoardingCommandRoutingSystemHelper.ToBoardingCommandResultElement(request, result);
        }

        private RtsSelectionCommandResultElement ProcessBoardAllSelectedTransportRequest(
            EntityManager em,
            RtsSelectionCommandIntentRequestElement request,
            UnitTransportCapacitySystem transportCapacitySystem,
            SelectionStateCompositionSystemHelper selectionStateSystem)
        {
            if (!TryResolveSelectedBoardTransport(em, selectionStateSystem, out Entity transport))
            {
                return TransportBoardingCommandRoutingSystemHelper.ToBoardAllCommandResultElement(
                    request,
                    false,
                    TacticalCommandReasonCode.CommandUnavailable,
                    GameText.Get("tactical.command.reason.invalid_transport", "Select a transport vehicle or aircraft first."));
            }

            if (!TryIssueBoardNearestSoldierOrders(
                    em,
                    transport,
                    transportCapacitySystem,
                    out int orderedCount))
            {
                return TransportBoardingCommandRoutingSystemHelper.ToBoardAllCommandResultElement(
                    request,
                    false,
                    TacticalCommandReasonCode.CommandUnavailable,
                    "No nearby units can board this transport.");
            }

            string message = TransportBoardingOrderPlanningSystemHelper.ResolveBoardAllAcceptedMessage(orderedCount);
            return TransportBoardingCommandRoutingSystemHelper.ToBoardAllCommandResultElement(
                request,
                true,
                TacticalCommandReasonCode.None,
                message);
        }

        private RtsSelectionCommandResultElement ProcessDisembarkTransportRequest(
            EntityManager em,
            RtsSelectionCommandIntentRequestElement request,
            UnitTransportCapacitySystem transportCapacitySystem,
            UnitMoveOrderSystem moveOrderSystem)
        {
            DisembarkResult result = request.HasTargetEntity != 0
                ? TryDisembarkTransport(
                    em,
                    request.TargetEntity,
                    transportCapacitySystem,
                    moveOrderSystem,
                    _gridPathingQuery,
                    request.TargetCell,
                    request.HasTargetCell)
                : DisembarkResult.Rejected(TacticalCommandReasonCode.InvalidTransport, showFeedback: false);
            if (result.Accepted && request.HasTargetEntity != 0)
                CancelDeployAttackForManualDisembark(em, request.TargetEntity);
            return ToDisembarkCommandResultElement(request, result);
        }

        private RtsSelectionCommandResultElement ProcessDisembarkTransportPassengerRequest(
            EntityManager em,
            RtsSelectionCommandIntentRequestElement request,
            UnitTransportCapacitySystem transportCapacitySystem,
            UnitMoveOrderSystem moveOrderSystem)
        {
            DisembarkResult result = request.HasTargetEntity != 0 && request.HasSecondaryTargetEntity != 0
                ? TryDisembarkTransportPassenger(
                    em,
                    request.TargetEntity,
                    request.SecondaryTargetEntity,
                    transportCapacitySystem,
                    moveOrderSystem,
                    _gridPathingQuery,
                    request.TargetCell,
                    request.HasTargetCell)
                : DisembarkResult.Rejected(TacticalCommandReasonCode.TransportPassengerMissing, showFeedback: false);
            if (result.Accepted && request.HasTargetEntity != 0)
                CancelDeployAttackForManualDisembark(em, request.TargetEntity, request.SecondaryTargetEntity);
            return ToDisembarkCommandResultElement(request, result);
        }

        private static RtsSelectionCommandResultElement ToDisembarkCommandResultElement(
            RtsSelectionCommandIntentRequestElement request,
            DisembarkResult result)
        {
            bool accepted = result.Accepted;
            bool showFeedback = !accepted && result.ShowFeedback;
            return new RtsSelectionCommandResultElement
            {
                Kind = request.Kind,
                RequestId = request.RequestId,
                Frame = request.Frame,
                TargetEntity = request.TargetEntity,
                TargetKind = request.HasTargetEntity != 0 ? RtsSelectionCommandTargetKind.Entity : RtsSelectionCommandTargetKind.None,
                CommandMode = (int)TacticalCommandMode.Board,
                HasCommandResult = accepted || showFeedback ? (byte)1 : (byte)0,
                Accepted = accepted ? (byte)1 : (byte)0,
                ReasonCode = accepted ? 0 : (int)result.ReasonCode,
                FeedbackLifetime = accepted
                    ? RtsSelectionCommandFeedbackLifetime.Transient
                    : showFeedback
                        ? RtsSelectionCommandFeedbackLifetime.Transient
                        : RtsSelectionCommandFeedbackLifetime.Hidden,
                HasTargetEntity = request.HasTargetEntity,
                Message = result.Message
            };
        }


    }
}
