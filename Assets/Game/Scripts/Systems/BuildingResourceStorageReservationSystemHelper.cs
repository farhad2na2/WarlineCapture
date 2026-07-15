using Game.Components;
using Unity.Mathematics;

namespace Game.Runtime
{
    internal static class BuildingResourceStorageReservationSystemHelper
    {
        private const byte OilResourceKind = (byte)ResourceKind.Oil;
        private const byte FuelResourceKind = (byte)ResourceKind.Fuel;

        internal static bool TryReserveSource(
            ref BuildingResourceStorageComponent source,
            byte resourceKind,
            float loadAmount)
        {
            loadAmount = math.max(0f, loadAmount);
            if (!BuildingResourceStorageTransferSystemHelper.HasEnoughSourceResource(
                    source,
                    resourceKind,
                    loadAmount))
            {
                return false;
            }

            if (resourceKind == FuelResourceKind)
                source.ReservedFuelOutboundBarrels += loadAmount;
            else
                source.ReservedOilOutboundBarrels += loadAmount;
            IncrementVersion(ref source);
            return true;
        }

        internal static bool TryReserveDestination(
            ref BuildingResourceStorageComponent destination,
            byte resourceKind,
            float cargo)
        {
            cargo = math.max(0f, cargo);
            if (!BuildingResourceStorageTransferSystemHelper.HasReceivingCapacity(
                    destination,
                    resourceKind,
                    cargo))
            {
                return false;
            }

            if (resourceKind == FuelResourceKind)
                destination.ReservedFuelInboundBarrels += cargo;
            else
                destination.ReservedOilInboundBarrels += cargo;
            IncrementVersion(ref destination);
            return true;
        }

        internal static void ReleaseSourceReservation(
            ref BuildingResourceStorageComponent source,
            byte resourceKind,
            float loadAmount)
        {
            loadAmount = math.max(0f, loadAmount);
            if (loadAmount <= 0f)
                return;

            float previous;
            if (resourceKind == FuelResourceKind)
            {
                previous = source.ReservedFuelOutboundBarrels;
                source.ReservedFuelOutboundBarrels = math.max(0f, previous - loadAmount);
            }
            else
            {
                previous = source.ReservedOilOutboundBarrels;
                source.ReservedOilOutboundBarrels = math.max(0f, previous - loadAmount);
            }

            if (previous > 0f)
                IncrementVersion(ref source);
        }

        internal static void ReleaseDestinationReservation(
            ref BuildingResourceStorageComponent destination,
            byte resourceKind,
            float cargo)
        {
            cargo = math.max(0f, cargo);
            if (cargo <= 0f)
                return;

            float previous;
            if (resourceKind == FuelResourceKind)
            {
                previous = destination.ReservedFuelInboundBarrels;
                destination.ReservedFuelInboundBarrels = math.max(0f, previous - cargo);
            }
            else
            {
                previous = destination.ReservedOilInboundBarrels;
                destination.ReservedOilInboundBarrels = math.max(0f, previous - cargo);
            }

            if (previous > 0f)
                IncrementVersion(ref destination);
        }

        internal static bool TryConsumeSourceReservation(
            ref BuildingResourceStorageComponent source,
            byte resourceKind,
            float amount)
        {
            amount = math.max(0f, amount);
            if (amount <= 0f)
                return false;

            float reserved = resourceKind == FuelResourceKind
                ? source.ReservedFuelOutboundBarrels
                : source.ReservedOilOutboundBarrels;
            float stored = resourceKind == FuelResourceKind
                ? source.StoredFuelBarrels
                : source.StoredOilBarrels;
            if (reserved + 0.001f < amount || stored + 0.001f < amount)
                return false;

            if (resourceKind == FuelResourceKind)
            {
                source.StoredFuelBarrels = math.max(0f, source.StoredFuelBarrels - amount);
                source.ReservedFuelOutboundBarrels = math.max(0f, source.ReservedFuelOutboundBarrels - amount);
            }
            else
            {
                source.StoredOilBarrels = math.max(0f, source.StoredOilBarrels - amount);
                source.ReservedOilOutboundBarrels = math.max(0f, source.ReservedOilOutboundBarrels - amount);
            }

            IncrementVersion(ref source);
            return true;
        }

        internal static bool TryCompleteReservedDelivery(
            ref BuildingResourceStorageComponent destination,
            byte resourceKind,
            float amount)
        {
            amount = math.max(0f, amount);
            if (amount <= 0f)
                return false;

            float reserved = resourceKind == FuelResourceKind
                ? destination.ReservedFuelInboundBarrels
                : destination.ReservedOilInboundBarrels;
            float stored = resourceKind == FuelResourceKind
                ? destination.StoredFuelBarrels
                : destination.StoredOilBarrels;
            int capacity = resourceKind == FuelResourceKind
                ? destination.FuelStorageCapacity
                : destination.OilStorageCapacity;
            bool unlimitedOilInput = resourceKind == OilResourceKind &&
                                     capacity <= 0 &&
                                     destination.FuelBarrelsPerDay > 0f;
            if (reserved + 0.001f < amount ||
                (!unlimitedOilInput && (capacity <= 0 || stored + amount > capacity + 0.001f)))
            {
                return false;
            }

            if (resourceKind == FuelResourceKind)
            {
                destination.StoredFuelBarrels += amount;
                destination.ReservedFuelInboundBarrels =
                    math.max(0f, destination.ReservedFuelInboundBarrels - amount);
            }
            else
            {
                destination.StoredOilBarrels += amount;
                destination.ReservedOilInboundBarrels =
                    math.max(0f, destination.ReservedOilInboundBarrels - amount);
            }

            IncrementVersion(ref destination);
            return true;
        }

        private static void IncrementVersion(ref BuildingResourceStorageComponent storage)
        {
            storage.Version = storage.Version == uint.MaxValue ? 1u : storage.Version + 1u;
        }
    }
}
