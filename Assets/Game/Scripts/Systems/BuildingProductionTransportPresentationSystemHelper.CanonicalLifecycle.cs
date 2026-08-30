using Game.Components;
using Unity.Entities;

namespace Game.Runtime
{
    internal sealed partial class BuildingProductionTransportPresentationSystemHelper
    {
        private void BeginCanonicalDeparture(CanonicalDeliverySession session, float now)
        {
            session.TransportTransform.position = session.HoverPosition;
            session.TransportTransform.rotation = session.HoverRotation;
            session.Phase = CanonicalDeliveryDeparturePhase;
            session.PhaseStartedAt = now;
            SetCanonicalTransportDoorOpen01(session, 0f);
        }

        private static bool TryReadCanonicalDeliveryRemainingQuantity(
            Context context,
            CanonicalDeliveryKey key,
            out int remainingQuantity)
        {
            remainingQuantity = 0;
            BuildingProductionTransportBridgeCompositionSystemHelper.TryGetEntityManagerDelegate tryGetEntityManager =
                context.TransportBridgeContext.TryGetEntityManager;
            if (tryGetEntityManager == null ||
                !tryGetEntityManager(out EntityManager entityManager) ||
                key.Producer == Entity.Null ||
                !entityManager.Exists(key.Producer) ||
                !entityManager.HasBuffer<OperationMapBuildingUnitProductionRequest>(key.Producer))
            {
                return false;
            }

            DynamicBuffer<OperationMapBuildingUnitProductionRequest> queue =
                entityManager.GetBuffer<OperationMapBuildingUnitProductionRequest>(key.Producer, true);
            for (int index = 0; index < queue.Length; index++)
            {
                OperationMapBuildingUnitProductionRequest request = queue[index];
                if (request.RequestId != key.RequestId ||
                    request.Status != OperationMapBuildingUnitProductionRequest.Pending ||
                    request.RemainingQuantity <= 0)
                {
                    continue;
                }

                remainingQuantity = request.RemainingQuantity;
                return true;
            }

            return false;
        }
    }
}
