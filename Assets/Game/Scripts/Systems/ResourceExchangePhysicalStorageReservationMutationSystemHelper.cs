using Game.Components;
using Unity.Entities;

namespace Game.Runtime
{
    internal static class ResourceExchangePhysicalStorageReservationMutationSystemHelper
    {
        internal static void RemoveQueueReservations(
            EntityManager entityManager,
            DynamicBuffer<ResourceExchangePhysicalReservationComponent> reservations,
            int queueItemId,
            bool refundInput)
        {
            for (int i = reservations.Length - 1; i >= 0; i--)
            {
                ResourceExchangePhysicalReservationComponent reservation = reservations[i];
                if (reservation.QueueItemId != queueItemId)
                    continue;

                if (entityManager.Exists(reservation.StorageEntity) &&
                    entityManager.HasComponent<BuildingResourceStorageComponent>(reservation.StorageEntity))
                {
                    BuildingResourceStorageComponent storage =
                        entityManager.GetComponentData<BuildingResourceStorageComponent>(reservation.StorageEntity);
                    byte resourceKind = ResourceExchangePhysicalStorageUtilitySystemHelper.ToStorageResourceKind(
                        reservation.ResourceKind);
                    if (reservation.ReservationKind == ResourceExchangePhysicalReservationKind.Output)
                    {
                        BuildingResourceStorageTransferSystemHelper.ReleaseDestinationReservation(
                            ref storage,
                            resourceKind,
                            reservation.Amount);
                    }
                    else if (refundInput)
                    {
                        BuildingResourceStorageTransferSystemHelper.ReleaseSourceReservation(
                            ref storage,
                            resourceKind,
                            reservation.Amount);
                    }
                    else
                    {
                        BuildingResourceStorageTransferSystemHelper.TryConsumeSourceReservation(
                            ref storage,
                            resourceKind,
                            reservation.Amount);
                    }

                    entityManager.SetComponentData(reservation.StorageEntity, storage);
                }

                reservations.RemoveAt(i);
            }
        }

        internal static void RemoveQueueLinesOnly(
            DynamicBuffer<ResourceExchangePhysicalReservationComponent> reservations,
            int queueItemId)
        {
            for (int i = reservations.Length - 1; i >= 0; i--)
            {
                if (reservations[i].QueueItemId == queueItemId)
                    reservations.RemoveAt(i);
            }
        }

        internal static void RollBackAddedReservations(
            EntityManager entityManager,
            DynamicBuffer<ResourceExchangePhysicalReservationComponent> reservations,
            int firstAddedReservation)
        {
            for (int i = reservations.Length - 1; i >= firstAddedReservation; i--)
            {
                ResourceExchangePhysicalReservationComponent reservation = reservations[i];
                if (entityManager.Exists(reservation.StorageEntity) &&
                    entityManager.HasComponent<BuildingResourceStorageComponent>(reservation.StorageEntity))
                {
                    BuildingResourceStorageComponent storage =
                        entityManager.GetComponentData<BuildingResourceStorageComponent>(reservation.StorageEntity);
                    byte resourceKind = ResourceExchangePhysicalStorageUtilitySystemHelper.ToStorageResourceKind(
                        reservation.ResourceKind);
                    if (reservation.ReservationKind == ResourceExchangePhysicalReservationKind.Input)
                    {
                        BuildingResourceStorageTransferSystemHelper.ReleaseSourceReservation(
                            ref storage,
                            resourceKind,
                            reservation.Amount);
                    }
                    else
                    {
                        BuildingResourceStorageTransferSystemHelper.ReleaseDestinationReservation(
                            ref storage,
                            resourceKind,
                            reservation.Amount);
                    }

                    entityManager.SetComponentData(reservation.StorageEntity, storage);
                }

                reservations.RemoveAt(i);
            }
        }
    }
}
