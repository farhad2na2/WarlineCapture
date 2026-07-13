using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Runtime
{
    public static class ResourceExchangePhysicalStorageUtilitySystemHelper
    {
        private const float Epsilon = 0.001f;

        public static bool IsPhysicalResource(ResourceExchangeResourceKind resourceKind)
        {
            return resourceKind == ResourceExchangeResourceKind.Oil ||
                   resourceKind == ResourceExchangeResourceKind.Fuel;
        }

        public static bool TryReserveForQueue(
            EntityManager entityManager,
            EntityQuery storageQuery,
            DynamicBuffer<ResourceExchangePhysicalReservationComponent> reservations,
            int queueItemId,
            byte factionId,
            ResourceExchangeResourceKind inputResource,
            int inputAmount,
            ResourceExchangeResourceKind outputResource,
            int outputAmount,
            out ResourceExchangeReason reason)
        {
            ResourceExchangePhysicalStorageReservationMutationSystemHelper.RemoveQueueReservations(
                entityManager,
                reservations,
                queueItemId,
                true);
            if (IsPhysicalResource(inputResource) &&
                !TryReserveResource(
                    entityManager,
                    storageQuery,
                    reservations,
                    queueItemId,
                    factionId,
                    inputResource,
                    ResourceExchangePhysicalReservationKind.Input,
                    inputAmount,
                    out reason))
            {
                return false;
            }

            if (IsPhysicalResource(outputResource) &&
                !TryReserveResource(
                    entityManager,
                    storageQuery,
                    reservations,
                    queueItemId,
                    factionId,
                    outputResource,
                    ResourceExchangePhysicalReservationKind.Output,
                    outputAmount,
                    out reason))
            {
                ResourceExchangePhysicalStorageReservationMutationSystemHelper.RemoveQueueReservations(
                    entityManager,
                    reservations,
                    queueItemId,
                    true);
                return false;
            }

            reason = ResourceExchangeReason.None;
            return true;
        }

        public static ResourceExchangeReason ValidateCompletion(
            EntityManager entityManager,
            DynamicBuffer<ResourceExchangePhysicalReservationComponent> reservations,
            in ResourceExchangeQueueComponent item)
        {
            float reservedInput = 0f;
            float reservedOutput = 0f;
            for (int i = 0; i < reservations.Length; i++)
            {
                ResourceExchangePhysicalReservationComponent reservation = reservations[i];
                if (reservation.QueueItemId != item.QueueItemId)
                    continue;

                if (!entityManager.Exists(reservation.StorageEntity) ||
                    !entityManager.HasComponent<BuildingResourceStorageComponent>(reservation.StorageEntity))
                {
                    return ResourceExchangeReason.StorageMissing;
                }

                BuildingResourceStorageComponent storage =
                    entityManager.GetComponentData<BuildingResourceStorageComponent>(reservation.StorageEntity);
                if (storage.OwnerFactionId != item.FactionId)
                    return ResourceExchangeReason.StorageMissing;

                byte resourceKind = ToStorageResourceKind(reservation.ResourceKind);
                if (reservation.ReservationKind == ResourceExchangePhysicalReservationKind.Input)
                {
                    float reserved = resourceKind == BuildingResourceStorageTransferSystemHelper.FuelResourceKind
                        ? storage.ReservedFuelOutboundBarrels
                        : storage.ReservedOilOutboundBarrels;
                    float stored = resourceKind == BuildingResourceStorageTransferSystemHelper.FuelResourceKind
                        ? storage.StoredFuelBarrels
                        : storage.StoredOilBarrels;
                    if (reserved + Epsilon < reservation.Amount || stored + Epsilon < reservation.Amount)
                        return InsufficientReason(reservation.ResourceKind);
                    reservedInput += reservation.Amount;
                }
                else
                {
                    float reserved = resourceKind == BuildingResourceStorageTransferSystemHelper.FuelResourceKind
                        ? storage.ReservedFuelInboundBarrels
                        : storage.ReservedOilInboundBarrels;
                    if (reserved + Epsilon < reservation.Amount ||
                        !CanCompleteDelivery(storage, resourceKind, reservation.Amount))
                    {
                        return ResourceExchangeReason.StorageFull;
                    }
                    reservedOutput += reservation.Amount;
                }
            }

            if (IsPhysicalResource(item.InputResource) && reservedInput + Epsilon < item.InputAmount)
                return InsufficientReason(item.InputResource);
            if (IsPhysicalResource(item.OutputResource) && reservedOutput + Epsilon < item.OutputAmount)
                return ResourceExchangeReason.StorageMissing;
            return ResourceExchangeReason.None;
        }

        public static bool TryCompleteQueueItem(
            EntityManager entityManager,
            DynamicBuffer<ResourceExchangePhysicalReservationComponent> reservations,
            in ResourceExchangeQueueComponent item,
            out ResourceExchangeReason reason)
        {
            reason = ValidateCompletion(entityManager, reservations, item);
            if (reason != ResourceExchangeReason.None)
                return false;

            for (int i = 0; i < reservations.Length; i++)
            {
                ResourceExchangePhysicalReservationComponent reservation = reservations[i];
                if (reservation.QueueItemId != item.QueueItemId)
                    continue;

                BuildingResourceStorageComponent storage =
                    entityManager.GetComponentData<BuildingResourceStorageComponent>(reservation.StorageEntity);
                byte resourceKind = ToStorageResourceKind(reservation.ResourceKind);
                bool applied = reservation.ReservationKind == ResourceExchangePhysicalReservationKind.Input
                    ? BuildingResourceStorageTransferSystemHelper.TryConsumeSourceReservation(
                        ref storage,
                        resourceKind,
                        reservation.Amount)
                    : BuildingResourceStorageTransferSystemHelper.TryCompleteReservedDelivery(
                        ref storage,
                        resourceKind,
                        reservation.Amount);
                if (!applied)
                {
                    reason = reservation.ReservationKind == ResourceExchangePhysicalReservationKind.Input
                        ? InsufficientReason(reservation.ResourceKind)
                        : ResourceExchangeReason.StorageFull;
                    return false;
                }

                entityManager.SetComponentData(reservation.StorageEntity, storage);
            }

            ResourceExchangePhysicalStorageReservationMutationSystemHelper.RemoveQueueLinesOnly(
                reservations,
                item.QueueItemId);
            reason = ResourceExchangeReason.None;
            return true;
        }

        public static void CancelQueueItem(
            EntityManager entityManager,
            DynamicBuffer<ResourceExchangePhysicalReservationComponent> reservations,
            int queueItemId,
            bool refundInput)
        {
            ResourceExchangePhysicalStorageReservationMutationSystemHelper.RemoveQueueReservations(
                entityManager,
                reservations,
                queueItemId,
                refundInput);
        }

        private static bool TryReserveResource(
            EntityManager entityManager,
            EntityQuery storageQuery,
            DynamicBuffer<ResourceExchangePhysicalReservationComponent> reservations,
            int queueItemId,
            byte factionId,
            ResourceExchangeResourceKind resourceKind,
            ResourceExchangePhysicalReservationKind reservationKind,
            int amount,
            out ResourceExchangeReason reason)
        {
            if (amount <= 0)
            {
                reason = ResourceExchangeReason.InputBelowMinimum;
                return false;
            }

            byte storageResourceKind = ToStorageResourceKind(resourceKind);
            int firstAddedReservation = reservations.Length;
            using var candidates =
                new NativeList<ResourceExchangePhysicalStorageCandidateQuerySystemHelper.StorageCandidate>(
                    Allocator.Temp);
            float totalAvailable = ResourceExchangePhysicalStorageCandidateQuerySystemHelper.CollectCandidates(
                entityManager,
                storageQuery,
                factionId,
                storageResourceKind,
                reservationKind,
                candidates);
            if (totalAvailable + Epsilon < amount)
            {
                reason = reservationKind == ResourceExchangePhysicalReservationKind.Input
                    ? InsufficientReason(resourceKind)
                    : candidates.Length == 0
                        ? ResourceExchangeReason.StorageMissing
                        : ResourceExchangeReason.StorageFull;
                return false;
            }

            candidates.Sort();
            float remaining = amount;
            for (int i = 0; i < candidates.Length && remaining > Epsilon; i++)
            {
                ResourceExchangePhysicalStorageCandidateQuerySystemHelper.StorageCandidate candidate = candidates[i];
                float reservedAmount = math.min(candidate.Available, remaining);
                BuildingResourceStorageComponent storage =
                    entityManager.GetComponentData<BuildingResourceStorageComponent>(candidate.Entity);
                bool reserved = reservationKind == ResourceExchangePhysicalReservationKind.Input
                    ? BuildingResourceStorageTransferSystemHelper.TryReserveSource(
                        ref storage,
                        storageResourceKind,
                        reservedAmount)
                    : BuildingResourceStorageTransferSystemHelper.TryReserveDestination(
                        ref storage,
                        storageResourceKind,
                        reservedAmount);
                if (!reserved)
                {
                    ResourceExchangePhysicalStorageReservationMutationSystemHelper.RollBackAddedReservations(
                        entityManager,
                        reservations,
                        firstAddedReservation);
                    reason = reservationKind == ResourceExchangePhysicalReservationKind.Input
                        ? InsufficientReason(resourceKind)
                        : ResourceExchangeReason.StorageFull;
                    return false;
                }

                entityManager.SetComponentData(candidate.Entity, storage);
                reservations.Add(new ResourceExchangePhysicalReservationComponent
                {
                    QueueItemId = queueItemId,
                    StorageEntity = candidate.Entity,
                    ResourceKind = resourceKind,
                    ReservationKind = reservationKind,
                    Amount = reservedAmount
                });
                remaining -= reservedAmount;
            }

            reason = ResourceExchangeReason.None;
            return true;
        }

        private static bool CanCompleteDelivery(
            in BuildingResourceStorageComponent storage,
            byte resourceKind,
            float amount)
        {
            int capacity = resourceKind == BuildingResourceStorageTransferSystemHelper.FuelResourceKind
                ? storage.FuelStorageCapacity
                : storage.OilStorageCapacity;
            float stored = resourceKind == BuildingResourceStorageTransferSystemHelper.FuelResourceKind
                ? storage.StoredFuelBarrels
                : storage.StoredOilBarrels;
            return (resourceKind == BuildingResourceStorageTransferSystemHelper.OilResourceKind &&
                    capacity <= 0 &&
                    storage.FuelBarrelsPerDay > 0f) ||
                   (capacity > 0 && stored + amount <= capacity + Epsilon);
        }

        internal static byte ToStorageResourceKind(ResourceExchangeResourceKind resourceKind)
        {
            return resourceKind == ResourceExchangeResourceKind.Fuel
                ? BuildingResourceStorageTransferSystemHelper.FuelResourceKind
                : BuildingResourceStorageTransferSystemHelper.OilResourceKind;
        }

        private static ResourceExchangeReason InsufficientReason(ResourceExchangeResourceKind resourceKind)
        {
            return resourceKind == ResourceExchangeResourceKind.Fuel
                ? ResourceExchangeReason.InsufficientFuel
                : ResourceExchangeReason.InsufficientOil;
        }
    }
}
