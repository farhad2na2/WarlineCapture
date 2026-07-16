using Game.Components;
using Game.Runtime;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.UI.Shell.Ecs
{
    internal static class AssistantPreviewTargetUtility
    {
        public static bool TryResolve(
            EntityManager entityManager,
            EntityQuery operationMapMetadataQuery,
            in AssistantCommandIntentRequestElement request,
            out float3 position)
        {
            position = default;
            if (request.TargetKind == AssistantTargetKind.WorldPosition && IsFinite(request.WorldPosition))
            {
                position = request.WorldPosition;
                return true;
            }

            if (request.TargetKind == AssistantTargetKind.Entity)
            {
                if (TryReadEntityPosition(entityManager, request.TargetEntity, out position) ||
                    TryReadEntityPosition(entityManager, request.SourceEntity, out position))
                {
                    return true;
                }

                if (IsFinite(request.WorldPosition))
                {
                    position = request.WorldPosition;
                    return true;
                }
            }

            return request.TargetKind == AssistantTargetKind.Objective &&
                   !request.TargetId.IsEmpty &&
                   TryResolveObjectiveAnchor(
                       entityManager,
                       operationMapMetadataQuery,
                       in request.TargetId,
                       out position);
        }

        private static bool TryResolveObjectiveAnchor(
            EntityManager entityManager,
            EntityQuery operationMapMetadataQuery,
            in FixedString64Bytes anchorId,
            out float3 position)
        {
            position = default;
            if (operationMapMetadataQuery.CalculateEntityCount() != 1)
                return false;

            Entity metadataEntity = operationMapMetadataQuery.GetSingletonEntity();
            ActiveOperationMapComponent active =
                entityManager.GetComponentData<ActiveOperationMapComponent>(metadataEntity);
            OperationMapMetadataComponent metadata =
                entityManager.GetComponentData<OperationMapMetadataComponent>(metadataEntity);
            OperationMapReadinessComponent readiness =
                entityManager.GetComponentData<OperationMapReadinessComponent>(metadataEntity);
            if (!metadata.Blob.IsCreated ||
                active.Generation <= 0 ||
                active.Generation != metadata.Generation ||
                active.Generation != readiness.Generation ||
                (readiness.ReadyFlags & OperationMapReadinessFlags.Metadata) == 0 ||
                (readiness.FailedFlags & OperationMapReadinessFlags.Metadata) != 0)
            {
                return false;
            }

            ref OperationMapBlob operationMap = ref metadata.Blob.Value;
            if (!OperationMapMetadataUtility.TryFindAnchor(
                    ref operationMap,
                    in anchorId,
                    out OperationMapAnchorBlob anchor) ||
                anchor.Kind != OperationMapAnchorKind.Objective ||
                !IsFinite(anchor.Position))
            {
                return false;
            }

            position = anchor.Position;
            return true;
        }

        private static bool TryReadEntityPosition(
            EntityManager entityManager,
            Entity entity,
            out float3 position)
        {
            position = default;
            if (entity == Entity.Null ||
                !entityManager.Exists(entity) ||
                !entityManager.HasComponent<LocalTransform>(entity))
            {
                return false;
            }

            position = entityManager.GetComponentData<LocalTransform>(entity).Position;
            return IsFinite(position);
        }

        private static bool IsFinite(float3 value)
        {
            return math.all(math.isfinite(value));
        }
    }
}
