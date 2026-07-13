using Game.Components;
using Unity.Mathematics;

namespace Game.Runtime
{
    public static class BuildingResourceStorageTransferSystemHelper
    {
        public const byte OilResourceKind = (byte)ResourceKind.Oil;
        public const byte FuelResourceKind = (byte)ResourceKind.Fuel;

        public static float GetOilReceivingFreeCapacity(in BuildingResourceStorageComponent storage)
        {
            if (storage.OilStorageCapacity > 0)
                return math.max(0f, storage.OilStorageCapacity - storage.StoredOilBarrels - storage.ReservedOilInboundBarrels);

            return storage.FuelBarrelsPerDay > 0f ? float.MaxValue : 0f;
        }

        public static float GetFuelReceivingFreeCapacity(in BuildingResourceStorageComponent storage)
        {
            return storage.FuelStorageCapacity > 0
                ? math.max(0f, storage.FuelStorageCapacity - storage.StoredFuelBarrels - storage.ReservedFuelInboundBarrels)
                : 0f;
        }

        public static float GetAvailableSourceResource(
            in BuildingResourceStorageComponent storage,
            byte resourceKind)
        {
            return resourceKind == FuelResourceKind
                ? math.max(0f, storage.StoredFuelBarrels - storage.ReservedFuelOutboundBarrels)
                : math.max(0f, storage.StoredOilBarrels - storage.ReservedOilOutboundBarrels);
        }

        public static bool HasEnoughSourceResource(
            in BuildingResourceStorageComponent source,
            byte resourceKind,
            float loadAmount)
        {
            if (loadAmount <= 0f)
                return false;

            return GetAvailableSourceResource(source, resourceKind) + 0.001f >= loadAmount;
        }

        public static bool TryReserveSource(
            ref BuildingResourceStorageComponent source,
            byte resourceKind,
            float loadAmount)
        {
            loadAmount = math.max(0f, loadAmount);
            if (!HasEnoughSourceResource(source, resourceKind, loadAmount))
                return false;

            if (resourceKind == FuelResourceKind)
                source.ReservedFuelOutboundBarrels += loadAmount;
            else
                source.ReservedOilOutboundBarrels += loadAmount;
            IncrementVersion(ref source);
            return true;
        }

        public static bool TryReserveDestination(
            ref BuildingResourceStorageComponent destination,
            byte resourceKind,
            float cargo)
        {
            cargo = math.max(0f, cargo);
            if (!HasReceivingCapacity(destination, resourceKind, cargo))
                return false;

            if (resourceKind == FuelResourceKind)
                destination.ReservedFuelInboundBarrels += cargo;
            else
                destination.ReservedOilInboundBarrels += cargo;
            IncrementVersion(ref destination);
            return true;
        }

        public static void ReleaseSourceReservation(
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

        public static void ReleaseDestinationReservation(
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

        public static bool TryConsumeSourceReservation(
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

        public static bool TryConsumeAvailableSourceResource(
            ref BuildingResourceStorageComponent source,
            byte resourceKind,
            float amount)
        {
            amount = math.max(0f, amount);
            if (amount <= 0f || !HasEnoughSourceResource(source, resourceKind, amount))
                return false;

            if (resourceKind == FuelResourceKind)
                source.StoredFuelBarrels = math.max(0f, source.StoredFuelBarrels - amount);
            else
                source.StoredOilBarrels = math.max(0f, source.StoredOilBarrels - amount);

            IncrementVersion(ref source);
            return true;
        }

        public static bool TryCompleteReservedDelivery(
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

        public static bool TryCompleteLoad(
            ref BuildingResourceStorageComponent source,
            byte resourceKind,
            float loadAmount,
            ref UnitResourceHauler hauler)
        {
            loadAmount = math.max(0f, loadAmount);
            if (!HasEnoughSourceResource(source, resourceKind, loadAmount))
                return false;

            if (resourceKind == FuelResourceKind)
            {
                source.StoredFuelBarrels = math.max(0f, source.StoredFuelBarrels - loadAmount);
                hauler.CargoFuelBarrels = loadAmount;
                hauler.CargoOilBarrels = 0f;
            }
            else
            {
                source.StoredOilBarrels = math.max(0f, source.StoredOilBarrels - loadAmount);
                hauler.CargoOilBarrels = loadAmount;
                hauler.CargoFuelBarrels = 0f;
            }

            IncrementVersion(ref source);
            return true;
        }

        public static void RevertLoad(
            ref BuildingResourceStorageComponent source,
            byte resourceKind,
            float loadAmount,
            ref UnitResourceHauler hauler)
        {
            if (loadAmount <= 0f)
                return;

            if (resourceKind == FuelResourceKind)
            {
                source.StoredFuelBarrels += loadAmount;
                hauler.CargoFuelBarrels = 0f;
            }
            else
            {
                source.StoredOilBarrels += loadAmount;
                hauler.CargoOilBarrels = 0f;
            }

            IncrementVersion(ref source);
        }

        public static bool HasReceivingCapacity(
            in BuildingResourceStorageComponent destination,
            byte resourceKind,
            float cargo)
        {
            if (cargo <= 0f)
                return false;

            float freeSpace = resourceKind == FuelResourceKind
                ? GetFuelReceivingFreeCapacity(destination)
                : GetOilReceivingFreeCapacity(destination);
            return freeSpace + 0.001f >= cargo;
        }

        public static bool TryCompleteUnload(
            ref BuildingResourceStorageComponent destination,
            byte resourceKind,
            ref UnitResourceHauler hauler)
        {
            float cargo = resourceKind == FuelResourceKind
                ? math.max(0f, hauler.CargoFuelBarrels)
                : math.max(0f, hauler.CargoOilBarrels);
            if (!HasReceivingCapacity(destination, resourceKind, cargo))
                return false;

            if (resourceKind == FuelResourceKind)
            {
                destination.StoredFuelBarrels += cargo;
                if (destination.FuelStorageCapacity > 0)
                    destination.StoredFuelBarrels = math.min(destination.FuelStorageCapacity, destination.StoredFuelBarrels);
                hauler.CargoFuelBarrels = 0f;
            }
            else
            {
                destination.StoredOilBarrels += cargo;
                if (destination.OilStorageCapacity > 0)
                    destination.StoredOilBarrels = math.min(destination.OilStorageCapacity, destination.StoredOilBarrels);
                hauler.CargoOilBarrels = 0f;
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
