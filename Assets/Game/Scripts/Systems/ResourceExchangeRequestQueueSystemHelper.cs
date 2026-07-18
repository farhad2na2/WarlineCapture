using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Runtime
{
    internal static class ResourceExchangeRequestQueueSystemHelper
    {
        internal static int Enqueue(
            EntityManager entityManager,
            Entity exchangeEntity,
            ResourceExchangeRequestKind requestKind,
            byte factionId,
            int frameCount,
            FixedString128Bytes recipeId = default,
            int inputAmount = 0,
            int queueItemId = 0,
            int rushTickets = 0)
        {
            ResourceExchangeRequestQueueComponent requestQueue =
                entityManager.GetComponentData<ResourceExchangeRequestQueueComponent>(exchangeEntity);
            requestQueue.LastRequestId++;
            entityManager.SetComponentData(exchangeEntity, requestQueue);
            entityManager.GetBuffer<ResourceExchangeRequestComponent>(exchangeEntity).Add(
                new ResourceExchangeRequestComponent
                {
                    RequestId = requestQueue.LastRequestId,
                    RequestKind = requestKind,
                    FactionId = factionId,
                    RecipeId = recipeId,
                    InputAmount = inputAmount,
                    QueueItemId = queueItemId,
                    RushTickets = rushTickets,
                    FrameCount = frameCount
                });

            return requestQueue.LastRequestId;
        }

        internal static bool TryGetResult(
            EntityManager entityManager,
            Entity exchangeEntity,
            int requestId,
            out ResourceExchangeResultComponent result)
        {
            result = default;
            DynamicBuffer<ResourceExchangeResultComponent> results =
                entityManager.GetBuffer<ResourceExchangeResultComponent>(exchangeEntity, true);
            for (int i = 0; i < results.Length; i++)
            {
                if (results[i].RequestId != requestId)
                    continue;

                result = results[i];
                return true;
            }

            return false;
        }

        internal static ResourceExchangeReason InsufficientReason(ResourceExchangeResourceKind resourceKind)
        {
            switch (resourceKind)
            {
                case ResourceExchangeResourceKind.Materials:
                    return ResourceExchangeReason.InsufficientMaterials;
                case ResourceExchangeResourceKind.Oil:
                    return ResourceExchangeReason.InsufficientOil;
                case ResourceExchangeResourceKind.Fuel:
                    return ResourceExchangeReason.InsufficientFuel;
                case ResourceExchangeResourceKind.RushTickets:
                    return ResourceExchangeReason.InsufficientRushTickets;
                default:
                    return ResourceExchangeReason.InvalidResource;
            }
        }

        internal static bool CanCancel(in ResourceExchangeQueueComponent item)
        {
            if (item.OutputApplied != 0)
                return false;

            return item.State == ResourceExchangeQueueState.Pending ||
                   item.State == ResourceExchangeQueueState.InProgress ||
                   item.State == ResourceExchangeQueueState.Blocked;
        }

        internal static int CalculateRefundAmount(in ResourceExchangeQueueComponent item) =>
            item.PresentationStarted == 0 ? math.max(0, item.ReservedInputAmount) : 0;
    }
}
