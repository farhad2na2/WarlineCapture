using Game.Components;
using Unity.Collections;
using Unity.Entities;

namespace Game.Runtime
{
    public static class ResourceExchangeToastTextUtility
    {
        public static bool TryAppendToast(
            DynamicBuffer<ResourceExchangeToastComponent> toasts,
            bool emitToasts,
            in ResourceExchangeResultComponent result)
        {
            if (!emitToasts)
                return false;

            if (!TryCreateToast(result, toasts.Length + 1, out ResourceExchangeToastComponent toast))
                return false;

            toasts.Add(toast);
            return true;
        }

        public static bool TryCreateToast(
            in ResourceExchangeResultComponent result,
            int sequenceId,
            out ResourceExchangeToastComponent toast)
        {
            toast = default;
            if (!ShouldEmit(result))
                return false;

            ResourceExchangeToastKind toastKind = ResolveToastKind(result);
            toast = new ResourceExchangeToastComponent
            {
                SequenceId = sequenceId,
                RequestId = result.RequestId,
                QueueItemId = result.QueueItemId,
                FactionId = result.FactionId,
                ToastKind = toastKind,
                Severity = ResolveSeverity(toastKind),
                ResultKind = result.ResultKind,
                Reason = result.Reason,
                InputResource = result.InputResource,
                OutputResource = result.OutputResource,
                InputAmount = result.InputAmount,
                OutputAmount = result.OutputAmount,
                RushTicketsSpent = result.RushTicketsSpent,
                RecipeId = result.RecipeId,
                Title = ResolveTitle(toastKind),
                Body = ResolveBody(result, toastKind)
            };
            return true;
        }

        public static FixedString128Bytes ResolveReasonBody(ResourceExchangeReason reason)
        {
            switch (reason)
            {
                case ResourceExchangeReason.ExchangeUnavailable:
                    return new FixedString128Bytes("Resource Exchange is unavailable.");
                case ResourceExchangeReason.RecipeLocked:
                    return new FixedString128Bytes("Exchange route is locked.");
                case ResourceExchangeReason.InsufficientCredits:
                    return new FixedString128Bytes("Not enough Credits.");
                case ResourceExchangeReason.InsufficientMaterials:
                    return new FixedString128Bytes("Not enough Materials.");
                case ResourceExchangeReason.InsufficientOil:
                    return new FixedString128Bytes("Not enough Oil.");
                case ResourceExchangeReason.InsufficientFuel:
                    return new FixedString128Bytes("Not enough Fuel.");
                case ResourceExchangeReason.InputBelowMinimum:
                    return new FixedString128Bytes("Amount is below the minimum.");
                case ResourceExchangeReason.InputAboveMaximum:
                    return new FixedString128Bytes("Amount is above the maximum.");
                case ResourceExchangeReason.InputStepInvalid:
                    return new FixedString128Bytes("Amount must match the route step.");
                case ResourceExchangeReason.QueueFull:
                    return new FixedString128Bytes("Exchange queue is full.");
                case ResourceExchangeReason.StorageFull:
                    return new FixedString128Bytes("Output storage is full.");
                case ResourceExchangeReason.StorageMissing:
                    return new FixedString128Bytes("Required storage is missing.");
                case ResourceExchangeReason.TransportUnavailable:
                    return new FixedString128Bytes("Transport is unavailable.");
                case ResourceExchangeReason.RushUnavailable:
                    return new FixedString128Bytes("Rush is unavailable for this exchange.");
                case ResourceExchangeReason.InsufficientRushTickets:
                    return new FixedString128Bytes("Not enough Rush Tickets.");
                case ResourceExchangeReason.CancelUnavailable:
                    return new FixedString128Bytes("Exchange cannot be cancelled.");
                case ResourceExchangeReason.MissionEnding:
                    return new FixedString128Bytes("Mission is ending.");
                case ResourceExchangeReason.MissingRecipeId:
                    return new FixedString128Bytes("Recipe id is missing.");
                case ResourceExchangeReason.DuplicateRecipeId:
                    return new FixedString128Bytes("Duplicate exchange recipe id.");
                case ResourceExchangeReason.InvalidRecipe:
                    return new FixedString128Bytes("Exchange recipe is invalid.");
                case ResourceExchangeReason.InvalidResource:
                    return new FixedString128Bytes("Exchange resource is invalid.");
                case ResourceExchangeReason.InvalidRate:
                    return new FixedString128Bytes("Exchange rate is invalid.");
                case ResourceExchangeReason.InvalidDuration:
                    return new FixedString128Bytes("Exchange duration is invalid.");
                case ResourceExchangeReason.InvalidRushRule:
                    return new FixedString128Bytes("Exchange rush rule is invalid.");
                case ResourceExchangeReason.InvalidScenarioGate:
                    return new FixedString128Bytes("Exchange scenario gate is invalid.");
                default:
                    return new FixedString128Bytes("Exchange request rejected.");
            }
        }

        private static bool ShouldEmit(in ResourceExchangeResultComponent result)
        {
            if (result.Accepted == 0)
                return true;

            switch (result.ResultKind)
            {
                case ResourceExchangeResultKind.RequestAccepted:
                    return result.QueueItemId > 0 && !result.RecipeId.IsEmpty;
                case ResourceExchangeResultKind.QueueBlocked:
                case ResourceExchangeResultKind.QueueCompleted:
                case ResourceExchangeResultKind.QueueCancelled:
                case ResourceExchangeResultKind.RushAccepted:
                case ResourceExchangeResultKind.RushRejected:
                    return true;
                default:
                    return false;
            }
        }

        private static ResourceExchangeToastKind ResolveToastKind(in ResourceExchangeResultComponent result)
        {
            if (result.Accepted == 0 ||
                result.ResultKind == ResourceExchangeResultKind.RequestRejected ||
                result.ResultKind == ResourceExchangeResultKind.RushRejected ||
                result.ResultKind == ResourceExchangeResultKind.QueueBlocked)
            {
                return ResourceExchangeToastKind.Rejected;
            }

            switch (result.ResultKind)
            {
                case ResourceExchangeResultKind.RequestAccepted:
                case ResourceExchangeResultKind.QueueStarted:
                    return ResourceExchangeToastKind.QueueStarted;
                case ResourceExchangeResultKind.QueueCompleted:
                    return ResourceExchangeToastKind.QueueCompleted;
                case ResourceExchangeResultKind.QueueCancelled:
                    return ResourceExchangeToastKind.QueueCancelled;
                case ResourceExchangeResultKind.RushAccepted:
                    return ResourceExchangeToastKind.RushAccepted;
                default:
                    return ResourceExchangeToastKind.None;
            }
        }

        private static ResourceExchangeToastSeverity ResolveSeverity(ResourceExchangeToastKind toastKind)
        {
            switch (toastKind)
            {
                case ResourceExchangeToastKind.QueueCompleted:
                    return ResourceExchangeToastSeverity.Success;
                case ResourceExchangeToastKind.QueueCancelled:
                    return ResourceExchangeToastSeverity.Warning;
                case ResourceExchangeToastKind.Rejected:
                    return ResourceExchangeToastSeverity.Error;
                default:
                    return ResourceExchangeToastSeverity.Info;
            }
        }

        private static FixedString128Bytes ResolveTitle(ResourceExchangeToastKind toastKind)
        {
            switch (toastKind)
            {
                case ResourceExchangeToastKind.QueueStarted:
                    return new FixedString128Bytes("EXCHANGE QUEUED");
                case ResourceExchangeToastKind.QueueCompleted:
                    return new FixedString128Bytes("EXCHANGE COMPLETE");
                case ResourceExchangeToastKind.QueueCancelled:
                    return new FixedString128Bytes("EXCHANGE CANCELLED");
                case ResourceExchangeToastKind.RushAccepted:
                    return new FixedString128Bytes("RUSH APPLIED");
                case ResourceExchangeToastKind.Rejected:
                    return new FixedString128Bytes("EXCHANGE BLOCKED");
                default:
                    return new FixedString128Bytes("RESOURCE EXCHANGE");
            }
        }

        private static FixedString128Bytes ResolveBody(
            in ResourceExchangeResultComponent result,
            ResourceExchangeToastKind toastKind)
        {
            switch (toastKind)
            {
                case ResourceExchangeToastKind.QueueStarted:
                    return new FixedString128Bytes("Exchange route queued.");
                case ResourceExchangeToastKind.QueueCompleted:
                    return new FixedString128Bytes("Exchange output received.");
                case ResourceExchangeToastKind.QueueCancelled:
                    return result.Reason == ResourceExchangeReason.MissionEnding
                        ? ResolveReasonBody(result.Reason)
                        : new FixedString128Bytes("Exchange cancelled. Reserved resources refunded.");
                case ResourceExchangeToastKind.RushAccepted:
                    return new FixedString128Bytes("Rush Tickets applied.");
                case ResourceExchangeToastKind.Rejected:
                    return ResolveReasonBody(result.Reason);
                default:
                    return new FixedString128Bytes("Resource Exchange updated.");
            }
        }
    }
}
