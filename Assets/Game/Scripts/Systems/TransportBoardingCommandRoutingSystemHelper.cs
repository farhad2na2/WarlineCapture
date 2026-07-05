using Unity.Collections;
using Unity.Entities;
using Game.Components;
using Game.Tactical.Contracts;

namespace Game.Runtime
{
    internal static class TransportBoardingCommandRoutingSystemHelper
    {
        public static bool IsTransportCommandIntent(RtsSelectionCommandIntentKind kind)
        {
            return kind == RtsSelectionCommandIntentKind.BoardTransport ||
                   kind == RtsSelectionCommandIntentKind.BoardSelectedTransport ||
                   kind == RtsSelectionCommandIntentKind.BoardSelectedTransportPassenger ||
                   kind == RtsSelectionCommandIntentKind.BoardNearestSoldiers ||
                   kind == RtsSelectionCommandIntentKind.BoardAllSelectedTransport ||
                   kind == RtsSelectionCommandIntentKind.DisembarkTransport ||
                   kind == RtsSelectionCommandIntentKind.DisembarkTransportPassenger;
        }

        public static bool IsPreResolvedTransportCommandIntent(RtsSelectionCommandIntentRequestElement request)
        {
            return (request.Kind == RtsSelectionCommandIntentKind.BoardTransport &&
                    request.HasTargetEntity != 0) ||
                   (request.Kind == RtsSelectionCommandIntentKind.BoardSelectedTransportPassenger &&
                    request.HasTargetEntity != 0 &&
                    request.HasSecondaryTargetEntity != 0) ||
                   (request.Kind == RtsSelectionCommandIntentKind.DisembarkTransport &&
                    request.HasTargetEntity != 0) ||
                   (request.Kind == RtsSelectionCommandIntentKind.DisembarkTransportPassenger &&
                    request.HasTargetEntity != 0 &&
                    request.HasSecondaryTargetEntity != 0);
        }

        public static void AddCommandResult(
            EntityManager em,
            Entity commandEntity,
            DynamicBuffer<RtsSelectionCommandResultElement> fallbackResults,
            RtsSelectionCommandResultElement result)
        {
            if (commandEntity != Entity.Null && em.Exists(commandEntity) && em.HasBuffer<RtsSelectionCommandResultElement>(commandEntity))
            {
                em.GetBuffer<RtsSelectionCommandResultElement>(commandEntity).Add(result);
                return;
            }

            fallbackResults.Add(result);
        }

        public static RtsSelectionCommandResultElement ToBoardingCommandResultElement(
            RtsSelectionCommandIntentRequestElement request,
            TransportBoardingCommandSystem.Result result)
        {
            return new RtsSelectionCommandResultElement
            {
                Kind = request.Kind,
                RequestId = request.RequestId,
                Frame = request.Frame,
                TargetCell = result.MarkerCell,
                ScreenPosition = request.ScreenPosition,
                WorldPosition = result.MarkerPosition,
                TargetKind = result.Accepted ? RtsSelectionCommandTargetKind.Cell : RtsSelectionCommandTargetKind.None,
                TargetEntity = request.TargetEntity,
                CommandMode = (int)TacticalCommandMode.Board,
                HasCommandResult = 1,
                Accepted = result.Accepted ? (byte)1 : (byte)0,
                ReasonCode = result.Accepted ? 0 : (int)result.ReasonCode,
                FeedbackLifetime = RtsSelectionCommandFeedbackLifetime.Transient,
                EmitScreenMarker = result.Accepted ? (byte)1 : (byte)0,
                MarkerFactionId = result.MarkerFactionId,
                HasTargetEntity = result.Accepted && request.HasTargetEntity != 0 ? (byte)1 : (byte)0,
                HasTargetCell = result.Accepted ? (byte)1 : (byte)0,
                HasWorldPosition = result.Accepted ? (byte)1 : (byte)0,
                ShowWorldMarkers = result.Accepted ? (byte)1 : (byte)0,
                Message = result.Message
            };
        }

        public static RtsSelectionCommandResultElement ToBoardAllCommandResultElement(
            RtsSelectionCommandIntentRequestElement request,
            bool accepted,
            TacticalCommandReasonCode reasonCode,
            string message)
        {
            return new RtsSelectionCommandResultElement
            {
                Kind = request.Kind,
                RequestId = request.RequestId,
                Frame = request.Frame,
                CommandMode = (int)TacticalCommandMode.Board,
                HasCommandResult = 1,
                Accepted = accepted ? (byte)1 : (byte)0,
                ReasonCode = accepted ? 0 : (int)reasonCode,
                FeedbackLifetime = RtsSelectionCommandFeedbackLifetime.Transient,
                Message = new FixedString64Bytes(message ?? string.Empty)
            };
        }
    }
}
