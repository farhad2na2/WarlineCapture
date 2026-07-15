using Game.Components;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Runtime
{
    internal static class UnitMoveOrderQueueRequest
    {
        public static bool EnqueueAndProcessTargetPathMoveOrder(
            EntityManager entityManager,
            Entity entity,
            int2 goal,
            EntityQuery queueQuery)
        {
            int requestId = UnitMoveOrderRequestSystem.EnqueueMoveOrder(
                entityManager,
                entity,
                goal,
                UnitMoveOrderRequestKind.TargetPathOnly,
                issueGroundPathNow: true,
                useGroundPathRetryCooldown: false,
                resumeFrame: 0,
                currentFrame: 0,
                query: queueQuery);
            UnitMoveOrderRequestSystem.ProcessPendingRequests(entityManager, queueQuery);
            return UnitMoveOrderRequestSystem.TryGetResult(
                       entityManager,
                       requestId,
                       queueQuery,
                       out UnitMoveOrderResultElement result) &&
                   result.Issued != 0;
        }
    }
}
