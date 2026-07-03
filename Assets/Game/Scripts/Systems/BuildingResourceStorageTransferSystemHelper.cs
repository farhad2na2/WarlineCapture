using Game.Components;
using Unity.Mathematics;

namespace Game.Runtime
{
    public static class BuildingResourceStorageTransferSystemHelper
    {
        public const byte OilResourceKind = 0;
        public const byte FuelResourceKind = 1;

        public static float GetOilReceivingFreeCapacity(in BuildingResourceStorageComponent storage)
        {
            if (storage.OilStorageCapacity > 0)
                return math.max(0f, storage.OilStorageCapacity - storage.StoredOilBarrels);

            return storage.FuelBarrelsPerDay > 0f ? float.MaxValue : 0f;
        }

        public static float GetFuelReceivingFreeCapacity(in BuildingResourceStorageComponent storage)
        {
            return storage.FuelStorageCapacity > 0
                ? math.max(0f, storage.FuelStorageCapacity - storage.StoredFuelBarrels)
                : 0f;
        }

        public static bool HasEnoughSourceResource(
            in BuildingResourceStorageComponent source,
            byte resourceKind,
            float loadAmount)
        {
            if (loadAmount <= 0f)
                return false;

            float stored = resourceKind == FuelResourceKind
                ? source.StoredFuelBarrels
                : source.StoredOilBarrels;
            return stored + 0.001f >= loadAmount;
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

            return true;
        }
    }
}
