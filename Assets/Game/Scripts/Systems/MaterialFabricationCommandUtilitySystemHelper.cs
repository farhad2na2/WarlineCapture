using Game.Components;
using Unity.Entities;

namespace Game.Runtime
{
    public static class MaterialFabricationCommandUtilitySystemHelper
    {
        public static MaterialFabricationResultComponent ApplyProductionRequest(
            ref MaterialFabricationComponent fabrication,
            in MaterialFabricationRequestComponent request)
        {
            MaterialFabricationResultComponent result = new()
            {
                RequestId = request.RequestId,
                ProductionEnabled = fabrication.ProductionEnabled,
                FabricationVersion = fabrication.Version
            };

            if (request.RequestId <= 0 ||
                request.Kind != MaterialFabricationRequestKind.SetProductionEnabled ||
                request.ProductionEnabled > 1)
            {
                result.Code = MaterialFabricationResultCode.InvalidRequest;
                return result;
            }

            if (request.RequesterFactionId != fabrication.OwnerFactionId)
            {
                result.Code = MaterialFabricationResultCode.OwnerMismatch;
                return result;
            }

            byte productionEnabled = request.ProductionEnabled;
            if (fabrication.ProductionEnabled == productionEnabled)
            {
                result.Accepted = 1;
                result.Code = MaterialFabricationResultCode.Unchanged;
                return result;
            }

            fabrication.ProductionEnabled = productionEnabled;
            if (productionEnabled == 0)
            {
                fabrication.Status = MaterialFabricationStatusCode.Disabled;
                fabrication.BlockReason = MaterialFabricationBlockReasonCode.ProductionDisabled;
            }
            else
            {
                fabrication.Status = MaterialFabricationStatusCode.None;
                fabrication.BlockReason = MaterialFabricationBlockReasonCode.None;
            }

            IncrementVersion(ref fabrication.Version);
            result.Accepted = 1;
            result.ProductionEnabled = productionEnabled;
            result.Code = MaterialFabricationResultCode.Applied;
            result.FabricationVersion = fabrication.Version;
            return result;
        }

        public static bool TryEnqueueProductionRequest(
            EntityManager entityManager,
            Entity fabricationEntity,
            byte requesterFactionId,
            bool productionEnabled,
            out int requestId)
        {
            requestId = 0;
            if (fabricationEntity == Entity.Null ||
                !entityManager.Exists(fabricationEntity) ||
                !entityManager.HasComponent<MaterialFabricationComponent>(fabricationEntity) ||
                !entityManager.HasComponent<MaterialFabricationCommandQueueComponent>(fabricationEntity) ||
                !entityManager.HasBuffer<MaterialFabricationRequestComponent>(fabricationEntity) ||
                !entityManager.HasBuffer<MaterialFabricationResultComponent>(fabricationEntity))
            {
                return false;
            }

            MaterialFabricationCommandQueueComponent queue =
                entityManager.GetComponentData<MaterialFabricationCommandQueueComponent>(fabricationEntity);
            requestId = queue.LastRequestId == int.MaxValue ? 1 : queue.LastRequestId + 1;
            queue.LastRequestId = requestId;
            entityManager.SetComponentData(fabricationEntity, queue);

            DynamicBuffer<MaterialFabricationRequestComponent> requests =
                entityManager.GetBuffer<MaterialFabricationRequestComponent>(fabricationEntity);
            while (requests.Length >= MaterialFabricationCommandQueueComponent.Capacity)
                requests.RemoveAt(0);
            requests.Add(new MaterialFabricationRequestComponent
            {
                RequestId = requestId,
                RequesterFactionId = requesterFactionId,
                ProductionEnabled = productionEnabled ? (byte)1 : (byte)0,
                Kind = MaterialFabricationRequestKind.SetProductionEnabled
            });
            return true;
        }

        public static bool TryGetProductionResult(
            EntityManager entityManager,
            Entity fabricationEntity,
            int requestId,
            out MaterialFabricationResultComponent result)
        {
            result = default;
            if (requestId <= 0 ||
                fabricationEntity == Entity.Null ||
                !entityManager.Exists(fabricationEntity) ||
                !entityManager.HasBuffer<MaterialFabricationResultComponent>(fabricationEntity))
            {
                return false;
            }

            DynamicBuffer<MaterialFabricationResultComponent> results =
                entityManager.GetBuffer<MaterialFabricationResultComponent>(fabricationEntity);
            for (int i = results.Length - 1; i >= 0; i--)
            {
                if (results[i].RequestId != requestId)
                    continue;

                result = results[i];
                return true;
            }

            return false;
        }

        private static void IncrementVersion(ref uint version)
        {
            version = version == uint.MaxValue ? 1u : version + 1u;
        }
    }
}
