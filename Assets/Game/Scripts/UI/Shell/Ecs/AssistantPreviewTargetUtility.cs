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
        public static bool TryQueueResolvedCameraPreview(
            EntityManager entityManager,
            EntityQuery operationMapMetadataQuery,
            EntityQuery cameraRequestQuery,
            in AssistantCommandIntentRequestElement request)
        {
            if (cameraRequestQuery.CalculateEntityCount() != 1 ||
                !TryResolve(entityManager, operationMapMetadataQuery, in request, out float3 position))
                return false;

            QueueCameraPreview(entityManager, cameraRequestQuery.GetSingletonEntity(), position);
            return true;
        }

        public static void QueueCameraPreview(EntityManager entityManager, Entity cameraEntity, float3 focusWorldPosition)
        {
            RtsCameraRequestQueueComponent queue =
                entityManager.GetComponentData<RtsCameraRequestQueueComponent>(cameraEntity);
            DynamicBuffer<RtsCameraRequestElement> requests =
                entityManager.GetBuffer<RtsCameraRequestElement>(cameraEntity);
            requests.Add(new RtsCameraRequestElement
            {
                Kind = RtsCameraRequestKind.ApplyPerspectiveModeInstant,
                RequestId = ++queue.LastRequestId,
                Value = RuntimeCameraFocusRequestUtility.TacticalRevealHeight,
                Value2 = RuntimeCameraFocusRequestUtility.TacticalRevealPitch,
                Value3 = RuntimeCameraFocusRequestUtility.TacticalRevealYaw,
                Value4 = RuntimeCameraFocusRequestUtility.TacticalRevealFieldOfView
            });
            requests.Add(new RtsCameraRequestElement
            {
                Kind = RtsCameraRequestKind.CompleteZoomTransition,
                RequestId = ++queue.LastRequestId
            });
            requests.Add(new RtsCameraRequestElement
            {
                Kind = RtsCameraRequestKind.SetSmoothFocusTarget,
                RequestId = ++queue.LastRequestId,
                WorldPosition = focusWorldPosition,
                Flag = 1
            });
            requests.Add(new RtsCameraRequestElement
            {
                Kind = RtsCameraRequestKind.ClearDragging,
                RequestId = ++queue.LastRequestId
            });
            entityManager.SetComponentData(cameraEntity, queue);
        }

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

            if (request.TargetKind == AssistantTargetKind.Squad)
            {
                Entity representative = request.TargetEntity != Entity.Null
                    ? request.TargetEntity
                    : request.SourceEntity;
                if (TryReadMissionSquadCentroid(entityManager, representative, out position))
                    return true;
            }

            if (request.TargetKind == AssistantTargetKind.Entity ||
                request.TargetKind == AssistantTargetKind.Squad)
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
                   TryResolveNamedAnchor(
                       entityManager,
                       operationMapMetadataQuery,
                       in request.TargetId,
                       out position);
        }

        private static bool TryResolveNamedAnchor(
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

        private static bool TryReadMissionSquadCentroid(
            EntityManager entityManager,
            Entity representative,
            out float3 position)
        {
            position = default;
            if (representative == Entity.Null ||
                !entityManager.Exists(representative) ||
                !entityManager.HasComponent<CampaignMissionUnitRoleComponent>(representative))
            {
                return false;
            }

            CampaignMissionUnitRoleComponent representativeRole =
                entityManager.GetComponentData<CampaignMissionUnitRoleComponent>(representative);
            using EntityQuery query = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<CampaignMissionUnitRoleComponent>(),
                ComponentType.ReadOnly<Faction>(),
                ComponentType.ReadOnly<UnitMove>(),
                ComponentType.ReadOnly<LocalTransform>());
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            using NativeArray<CampaignMissionUnitRoleComponent> roles =
                query.ToComponentDataArray<CampaignMissionUnitRoleComponent>(Allocator.Temp);
            using NativeArray<Faction> factions = query.ToComponentDataArray<Faction>(Allocator.Temp);
            using NativeArray<LocalTransform> transforms =
                query.ToComponentDataArray<LocalTransform>(Allocator.Temp);

            int count = 0;
            float3 total = default;
            for (int i = 0; i < entities.Length; i++)
            {
                if (!roles[i].SessionToken.Equals(representativeRole.SessionToken) ||
                    !roles[i].UnitGroupId.Equals(representativeRole.UnitGroupId) ||
                    !FactionIdentity.IsPlayerControlled(factions[i].Id) ||
                    (entityManager.HasComponent<UnitHealth>(entities[i]) &&
                     entityManager.GetComponentData<UnitHealth>(entities[i]).Current <= 0))
                {
                    continue;
                }

                total += transforms[i].Position;
                count++;
            }

            if (count == 0)
                return false;

            position = total / count;
            return IsFinite(position);
        }

        private static bool IsFinite(float3 value)
        {
            return math.all(math.isfinite(value));
        }
    }
}
