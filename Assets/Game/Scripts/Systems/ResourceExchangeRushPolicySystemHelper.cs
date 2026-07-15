using Game.Components;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Runtime
{
    internal static class ResourceExchangeRushPolicySystemHelper
    {
        internal static ResourceExchangeReason ValidateGate(
            in ResourceExchangeEnabledComponent enabled,
            in ResourceExchangeRequestComponent request,
            out byte factionId)
        {
            factionId = request.FactionId != 0 ? request.FactionId : enabled.FactionId;
            if (enabled.Enabled == 0 || enabled.AllowRush == 0)
                return ResourceExchangeReason.RushUnavailable;

            return enabled.FactionId != 0 && factionId != enabled.FactionId
                ? ResourceExchangeReason.ExchangeUnavailable
                : ResourceExchangeReason.None;
        }

        internal static ResourceExchangeReason ValidateItem(in ResourceExchangeQueueComponent item)
        {
            if (item.OutputApplied != 0 || item.State != ResourceExchangeQueueState.InProgress)
                return ResourceExchangeReason.RushUnavailable;

            return item.RemainingSeconds > 0f
                ? ResourceExchangeReason.None
                : ResourceExchangeReason.RushUnavailable;
        }

        internal static ResourceExchangeReason ValidateRecipe(in ResourceExchangeRecipeComponent recipe)
        {
            return recipe.RushTicketSecondsPerTicket > 0 && recipe.MaxRushTickets > 0
                ? ResourceExchangeReason.None
                : ResourceExchangeReason.RushUnavailable;
        }

        internal static int FindQueueItemIndex(
            DynamicBuffer<ResourceExchangeQueueComponent> queue,
            int queueItemId,
            byte factionId)
        {
            for (int i = 0; i < queue.Length; i++)
            {
                ResourceExchangeQueueComponent item = queue[i];
                if (item.QueueItemId == queueItemId && item.FactionId == factionId)
                    return i;
            }

            return -1;
        }

        internal static int CalculateTicketCapacity(
            in ResourceExchangeQueueComponent item,
            in ResourceExchangeRecipeComponent recipe) =>
            math.max(0, recipe.MaxRushTickets - item.RushTicketsSpent);

        internal static int CalculateTicketsNeeded(
            in ResourceExchangeQueueComponent item,
            in ResourceExchangeRecipeComponent recipe)
        {
            int secondsPerTicket = math.max(1, recipe.RushTicketSecondsPerTicket);
            return item.RemainingSeconds <= 0f
                ? 0
                : math.max(1, (int)math.ceil(item.RemainingSeconds / secondsPerTicket));
        }

        internal static void ApplyTickets(
            ref FactionEconomy economy,
            ref FactionTacticalMaterialsComponent materials,
            ref ResourceExchangeWalletComponent wallet,
            DynamicBuffer<ResourceExchangeQueueComponent> queue,
            DynamicBuffer<ResourceExchangeResultComponent> results,
            DynamicBuffer<ResourceExchangeEconomyEventComponent> economyEvents,
            int queueIndex,
            in ResourceExchangeQueueComponent source,
            in ResourceExchangeRecipeComponent recipe,
            int rushTickets,
            EntityManager entityManager,
            DynamicBuffer<ResourceExchangePhysicalReservationComponent> physicalReservations,
            bool usePhysicalStorage)
        {
            ResourceExchangeQueueComponent item = source;
            item.RushTicketsSpent += rushTickets;
            item.RemainingSeconds = math.max(
                0f,
                item.RemainingSeconds - rushTickets * math.max(1, recipe.RushTicketSecondsPerTicket));
            item.Version++;

            economyEvents.Add(new ResourceExchangeEconomyEventComponent
            {
                QueueItemId = item.QueueItemId,
                FactionId = item.FactionId,
                ResultKind = ResourceExchangeResultKind.RushAccepted,
                ResourceKind = ResourceExchangeResourceKind.RushTickets,
                Amount = -rushTickets,
                RecipeId = item.RecipeId
            });

            if (item.RemainingSeconds <= 0f)
            {
                ResourceExchangeQueueTickSystem.TryCompleteQueueItem(
                    ref economy,
                    ref materials,
                    ref wallet,
                    item,
                    results,
                    economyEvents,
                    entityManager,
                    physicalReservations,
                    usePhysicalStorage,
                    out item);
            }

            queue[queueIndex] = item;
        }

        internal static ResourceExchangeResultComponent Accepted(
            in ResourceExchangeRequestComponent request,
            in ResourceExchangeQueueComponent item,
            int rushTicketsSpent,
            int affectedCount)
        {
            return new ResourceExchangeResultComponent
            {
                RequestId = request.RequestId,
                QueueItemId = item.QueueItemId,
                FactionId = item.FactionId,
                ResultKind = ResourceExchangeResultKind.RushAccepted,
                Accepted = 1,
                Reason = ResourceExchangeReason.None,
                RecipeId = item.RecipeId,
                InputResource = ResourceExchangeResourceKind.RushTickets,
                OutputResource = item.OutputResource,
                InputAmount = affectedCount,
                OutputAmount = item.OutputAmount,
                RushTicketsSpent = rushTicketsSpent
            };
        }

        internal static ResourceExchangeResultComponent Rejected(
            in ResourceExchangeRequestComponent request,
            in ResourceExchangeQueueComponent item,
            ResourceExchangeReason reason)
        {
            return new ResourceExchangeResultComponent
            {
                RequestId = request.RequestId,
                QueueItemId = request.QueueItemId,
                FactionId = request.FactionId,
                ResultKind = ResourceExchangeResultKind.RushRejected,
                Accepted = 0,
                Reason = reason,
                RecipeId = item.RecipeId,
                InputResource = ResourceExchangeResourceKind.RushTickets,
                OutputResource = item.OutputResource,
                RushTicketsSpent = request.RushTickets
            };
        }
    }
}
