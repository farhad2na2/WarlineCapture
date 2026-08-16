using Game.Components;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.UI.Shell.Ecs
{
    internal static class AssistantSelectionCommandUtility
    {
        public static bool TryQueue(
            EntityManager entityManager,
            Entity selectionInput,
            in AssistantCommandIntentRequestElement request,
            out int downstreamRequestId)
        {
            downstreamRequestId = 0;
            RtsSelectionInputRequestQueueComponent queue =
                entityManager.GetComponentData<RtsSelectionInputRequestQueueComponent>(selectionInput);
            DynamicBuffer<RtsSelectionCommandIntentRequestElement> commands =
                entityManager.GetBuffer<RtsSelectionCommandIntentRequestElement>(selectionInput);

            if (request.TargetKind == AssistantTargetKind.Squad ||
                request.TargetKind == AssistantTargetKind.Entity)
            {
                Entity target = Resolve(entityManager, request.TargetEntity);
                if (target == Entity.Null)
                    target = Resolve(entityManager, request.SourceEntity);
                if (target == Entity.Null)
                    return false;

                queue.LastRequestId++;
                downstreamRequestId = queue.LastRequestId;
                commands.Add(new RtsSelectionCommandIntentRequestElement
                {
                    Kind = request.TargetKind == AssistantTargetKind.Squad
                        ? RtsSelectionCommandIntentKind.FocusSquad
                        : RtsSelectionCommandIntentKind.FocusUnit,
                    RequestId = queue.LastRequestId,
                    Frame = request.Frame,
                    SourceEntity = request.SourceEntity,
                    TargetEntity = target,
                    TargetKind = RtsSelectionCommandTargetKind.Entity,
                    WorldPosition = request.WorldPosition,
                    HasSourceEntity = request.SourceEntity != Entity.Null ? (byte)1 : (byte)0,
                    HasTargetEntity = 1,
                    HasWorldPosition = IsFinite(request.WorldPosition) ? (byte)1 : (byte)0
                });
                entityManager.SetComponentData(selectionInput, queue);
                return true;
            }

            if (request.TargetKind != AssistantTargetKind.UiSurface &&
                request.TargetKind != AssistantTargetKind.None)
            {
                return false;
            }

            queue.LastRequestId++;
            downstreamRequestId = queue.LastRequestId;
            commands.Add(new RtsSelectionCommandIntentRequestElement
            {
                Kind = RtsSelectionCommandIntentKind.EnterSelectionMode,
                RequestId = queue.LastRequestId,
                Frame = request.Frame
            });
            entityManager.SetComponentData(selectionInput, queue);
            return true;
        }

        private static Entity Resolve(EntityManager entityManager, Entity entity)
        {
            return entity != Entity.Null && entityManager.Exists(entity) ? entity : Entity.Null;
        }

        private static bool IsFinite(float3 value)
        {
            return math.all(math.isfinite(value));
        }
    }
}
