using Unity.Mathematics;
using Unity.Entities;
using UnityEngine;

public sealed class ResourceHaulerSystem
{
    public enum TimedActionState : byte
    {
        Started = 0,
        Waiting = 1,
        Ready = 2
    }

    public enum ResourceHaulPhase : byte
    {
        None = 0,
        ToSource = 1,
        Loading = 2,
        ToDestination = 3,
        Unloading = 4
    }

    public enum ResourceHaulKind : byte
    {
        Oil = 0,
        Fuel = 1
    }

    public bool IsOilSourceBuilding(FactionResourceCompositionSystemHelper.IResourceBuilding building)
    {
        return building != null &&
               building.OilBarrelsPerDay > 0f &&
               building.OilStorageCapacity > 0;
    }

    public bool IsFuelBuilding(FactionResourceCompositionSystemHelper.IResourceBuilding building)
    {
        return building != null &&
               building.FuelBarrelsPerDay > 0f;
    }

    public bool IsFuelStorageSourceBuilding(FactionResourceCompositionSystemHelper.IResourceBuilding building)
    {
        return building != null &&
               building.FuelBarrelsPerDay > 0f &&
               building.FuelStorageCapacity > 0;
    }

    public bool HasAvailableFuelForHauler(FactionResourceCompositionSystemHelper.IResourceBuilding building)
    {
        return IsFuelStorageSourceBuilding(building) &&
               building.StoredFuelBarrels >= 1f;
    }

    public UnitResourceHaulOrder CreateOrder(
        int sourceBuildingId,
        int destinationBuildingId,
        int2 targetCell,
        ResourceHaulKind resourceKind)
    {
        return new UnitResourceHaulOrder
        {
            SourceBuildingId = sourceBuildingId,
            DestinationBuildingId = destinationBuildingId,
            TargetCell = targetCell,
            ActionEndsAt = 0f,
            Phase = (byte)ResourceHaulPhase.ToSource,
            ResourceKind = (byte)resourceKind
        };
    }

    public void SetPhase(ref UnitResourceHaulOrder order, ResourceHaulPhase phase)
    {
        order.Phase = (byte)phase;
        order.ActionEndsAt = 0f;
    }

    public void SetTravelPhase(ref UnitResourceHaulOrder order, ResourceHaulPhase phase, int2 targetCell)
    {
        order.TargetCell = targetCell;
        SetPhase(ref order, phase);
    }

    public void ResetActionTimer(ref UnitResourceHaulOrder order)
    {
        order.ActionEndsAt = 0f;
    }

    public TimedActionState AdvanceTimedAction(ref UnitResourceHaulOrder order, float now, float durationSeconds)
    {
        if (order.ActionEndsAt <= 0f)
        {
            order.ActionEndsAt = now + Mathf.Max(0f, durationSeconds);
            return TimedActionState.Started;
        }

        return now < order.ActionEndsAt
            ? TimedActionState.Waiting
            : TimedActionState.Ready;
    }

    public float GetLoadAmount(UnitResourceHauler hauler)
    {
        return Mathf.Max(0f, hauler.BarrelCapacity);
    }

    public float GetCargo(UnitResourceHauler hauler, ResourceHaulKind resourceKind)
    {
        return resourceKind == ResourceHaulKind.Fuel
            ? Mathf.Max(0f, hauler.CargoFuelBarrels)
            : Mathf.Max(0f, hauler.CargoOilBarrels);
    }

    public float GetOilReceivingFreeCapacity(FactionResourceCompositionSystemHelper.IResourceBuilding building)
    {
        if (building == null)
            return 0f;

        if (building.OilStorageCapacity > 0)
            return Mathf.Max(0f, building.OilStorageCapacity - building.StoredOilBarrels);

        if (building.FuelBarrelsPerDay > 0f)
            return float.MaxValue;

        return 0f;
    }

    public float GetFuelReceivingFreeCapacity(FactionResourceCompositionSystemHelper.IResourceBuilding building)
    {
        if (building == null)
            return 0f;

        if (building.FuelStorageCapacity > 0)
            return Mathf.Max(0f, building.FuelStorageCapacity - building.StoredFuelBarrels);

        return 0f;
    }

    public bool HasEnoughSourceResource(FactionResourceCompositionSystemHelper.IResourceBuilding source, ResourceHaulKind resourceKind, float loadAmount)
    {
        if (source == null || loadAmount <= 0f)
            return false;

        float stored = resourceKind == ResourceHaulKind.Fuel ? source.StoredFuelBarrels : source.StoredOilBarrels;
        return stored + 0.001f >= loadAmount;
    }

    public bool TryCompleteLoad(
        FactionResourceCompositionSystemHelper.IResourceBuilding source,
        ResourceHaulKind resourceKind,
        float loadAmount,
        ref UnitResourceHauler hauler)
    {
        loadAmount = Mathf.Max(0f, loadAmount);
        if (!HasEnoughSourceResource(source, resourceKind, loadAmount))
            return false;

        if (resourceKind == ResourceHaulKind.Fuel)
        {
            source.StoredFuelBarrels = Mathf.Max(0f, source.StoredFuelBarrels - loadAmount);
            hauler.CargoFuelBarrels = loadAmount;
            hauler.CargoOilBarrels = 0f;
        }
        else
        {
            source.StoredOilBarrels = Mathf.Max(0f, source.StoredOilBarrels - loadAmount);
            hauler.CargoOilBarrels = loadAmount;
            hauler.CargoFuelBarrels = 0f;
        }

        return true;
    }

    public void RevertLoad(
        FactionResourceCompositionSystemHelper.IResourceBuilding source,
        ResourceHaulKind resourceKind,
        float loadAmount,
        ref UnitResourceHauler hauler)
    {
        if (source == null || loadAmount <= 0f)
            return;

        if (resourceKind == ResourceHaulKind.Fuel)
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

    public bool HasReceivingCapacity(FactionResourceCompositionSystemHelper.IResourceBuilding destination, ResourceHaulKind resourceKind, float cargo)
    {
        if (destination == null || cargo <= 0f)
            return false;

        float freeSpace = resourceKind == ResourceHaulKind.Fuel
            ? GetFuelReceivingFreeCapacity(destination)
            : GetOilReceivingFreeCapacity(destination);
        return freeSpace + 0.001f >= cargo;
    }

    public bool TryCompleteUnload(
        FactionResourceCompositionSystemHelper.IResourceBuilding destination,
        ResourceHaulKind resourceKind,
        ref UnitResourceHauler hauler)
    {
        if (destination == null)
            return false;

        float cargo = resourceKind == ResourceHaulKind.Fuel
            ? Mathf.Max(0f, hauler.CargoFuelBarrels)
            : Mathf.Max(0f, hauler.CargoOilBarrels);
        if (!HasReceivingCapacity(destination, resourceKind, cargo))
            return false;

        if (resourceKind == ResourceHaulKind.Fuel)
        {
            destination.StoredFuelBarrels += cargo;
            if (destination.FuelStorageCapacity > 0)
                destination.StoredFuelBarrels = Mathf.Min(destination.FuelStorageCapacity, destination.StoredFuelBarrels);
            hauler.CargoFuelBarrels = 0f;
        }
        else
        {
            destination.StoredOilBarrels += cargo;
            if (destination.OilStorageCapacity > 0)
                destination.StoredOilBarrels = Mathf.Min(destination.OilStorageCapacity, destination.StoredOilBarrels);
            hauler.CargoOilBarrels = 0f;
        }

        return true;
    }
}
