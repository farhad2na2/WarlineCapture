using System.Collections.Generic;
using UnityEngine;

public sealed class FactionResourceSystem
{
    public enum ResourceKind : byte
    {
        Oil = 0,
        Fuel = 1
    }

    public readonly struct ResourceEconomySnapshot
    {
        public readonly float StoredOilBarrels;
        public readonly float StoredFuelBarrels;
        public readonly float OilBarrelsPerDay;
        public readonly float FuelBarrelsPerDay;
        public readonly int ResourceBuildingCount;

        public ResourceEconomySnapshot(
            float storedOilBarrels,
            float storedFuelBarrels,
            float oilBarrelsPerDay,
            float fuelBarrelsPerDay,
            int resourceBuildingCount)
        {
            StoredOilBarrels = storedOilBarrels;
            StoredFuelBarrels = storedFuelBarrels;
            OilBarrelsPerDay = oilBarrelsPerDay;
            FuelBarrelsPerDay = fuelBarrelsPerDay;
            ResourceBuildingCount = resourceBuildingCount;
        }
    }

    public readonly struct FactionResourceEconomySnapshot
    {
        public readonly float StoredOilBarrels;
        public readonly float StoredFuelBarrels;
        public readonly float OilBarrelsPerDay;
        public readonly float FuelBarrelsPerDay;
        public readonly int ResourceBuildingCount;

        public FactionResourceEconomySnapshot(
            float storedOilBarrels,
            float storedFuelBarrels,
            float oilBarrelsPerDay,
            float fuelBarrelsPerDay,
            int resourceBuildingCount)
        {
            StoredOilBarrels = storedOilBarrels;
            StoredFuelBarrels = storedFuelBarrels;
            OilBarrelsPerDay = oilBarrelsPerDay;
            FuelBarrelsPerDay = fuelBarrelsPerDay;
            ResourceBuildingCount = resourceBuildingCount;
        }
    }

    public readonly struct ResourceProductionTickResult
    {
        public readonly float OilExtractedBarrels;
        public readonly float FuelProducedBarrels;

        public ResourceProductionTickResult(float oilExtractedBarrels, float fuelProducedBarrels)
        {
            OilExtractedBarrels = oilExtractedBarrels;
            FuelProducedBarrels = fuelProducedBarrels;
        }
    }

    public interface IResourceBuilding
    {
        bool IsDestroyed { get; }
        bool HasOwnerFaction { get; }
        byte OwnerFactionId { get; }
        int OilStorageCapacity { get; }
        int FuelStorageCapacity { get; }
        float OilBarrelsPerDay { get; }
        float FuelBarrelsPerDay { get; }
        float StoredOilBarrels { get; set; }
        float StoredFuelBarrels { get; set; }
    }

    public int GetDisplayedOilCapacity(IResourceBuilding building, float oilBarrelsPerFuelBarrel)
    {
        if (building == null)
            return 0;

        int explicitOilCapacity = Mathf.Max(0, building.OilStorageCapacity);
        if (explicitOilCapacity > 0)
            return explicitOilCapacity;

        if (building.FuelBarrelsPerDay > 0f)
        {
            int derivedFromFuel = Mathf.CeilToInt(Mathf.Max(1f, building.FuelStorageCapacity) * Mathf.Max(0f, oilBarrelsPerFuelBarrel));
            return Mathf.Max(1, derivedFromFuel);
        }

        return 0;
    }

    public bool TryGetPrimaryCapacityInfo(IResourceBuilding building, float oilBarrelsPerFuelBarrel, out int current, out int max, out float progress01)
    {
        current = 0;
        max = 0;
        progress01 = 0f;
        if (building == null || building.IsDestroyed)
            return false;

        max = GetDisplayedOilCapacity(building, oilBarrelsPerFuelBarrel);
        if (max > 0)
        {
            current = Mathf.Clamp(Mathf.CeilToInt(building.StoredOilBarrels), 0, max);
            progress01 = Mathf.Clamp01(building.StoredOilBarrels / max);
            return true;
        }

        return TryGetFuelCapacityInfo(building, out current, out max, out progress01);
    }

    public bool TryGetFuelCapacityInfo(IResourceBuilding building, out int current, out int max, out float progress01)
    {
        current = 0;
        max = 0;
        progress01 = 0f;
        if (building == null)
            return false;

        max = Mathf.Max(0, building.FuelStorageCapacity);
        if (max <= 0)
            return false;

        current = Mathf.Clamp(Mathf.FloorToInt(building.StoredFuelBarrels), 0, max);
        progress01 = Mathf.Clamp01(building.StoredFuelBarrels / max);
        return true;
    }

    public void GetResourceTotals<TBuilding>(IReadOnlyDictionary<int, TBuilding> buildings, out int oilBarrels, out int fuelBarrels)
        where TBuilding : class, IResourceBuilding
    {
        oilBarrels = 0;
        fuelBarrels = 0;
        if (buildings == null)
            return;

        foreach (var entry in buildings)
        {
            TBuilding building = entry.Value;
            if (!IsResourceStorageBuilding(building))
                continue;

            if (building.OilStorageCapacity > 0)
                oilBarrels += Mathf.Max(0, Mathf.FloorToInt(building.StoredOilBarrels));
            if (building.FuelStorageCapacity > 0)
                fuelBarrels += Mathf.Max(0, Mathf.FloorToInt(building.StoredFuelBarrels));
        }
    }

    public bool TryGetFactionResourceEconomy<TBuilding>(IReadOnlyDictionary<int, TBuilding> buildings, byte factionId, out ResourceEconomySnapshot snapshot)
        where TBuilding : class, IResourceBuilding
    {
        float oil = 0f;
        float fuel = 0f;
        float oilRate = 0f;
        float fuelRate = 0f;
        int resourceBuildingCount = 0;

        if (buildings != null)
        {
            foreach (var entry in buildings)
            {
                TBuilding building = entry.Value;
                if (!IsFactionResourceBuilding(building, factionId))
                    continue;

                resourceBuildingCount++;
                oil += Mathf.Max(0f, building.StoredOilBarrels);
                fuel += Mathf.Max(0f, building.StoredFuelBarrels);
                oilRate += Mathf.Max(0f, building.OilBarrelsPerDay);
                fuelRate += Mathf.Max(0f, building.FuelBarrelsPerDay);
            }
        }

        snapshot = new ResourceEconomySnapshot(oil, fuel, oilRate, fuelRate, resourceBuildingCount);
        return resourceBuildingCount > 0;
    }

    public float DrainFactionResource<TBuilding>(
        IReadOnlyDictionary<int, TBuilding> buildings,
        byte factionId,
        float requestedBarrels,
        ResourceKind resourceKind)
        where TBuilding : class, IResourceBuilding
    {
        if (buildings == null || requestedBarrels <= 0f)
            return 0f;

        float remaining = requestedBarrels;
        foreach (var entry in buildings)
        {
            TBuilding building = entry.Value;
            if (!IsFactionResourceBuilding(building, factionId))
                continue;

            float stored = resourceKind == ResourceKind.Fuel ? building.StoredFuelBarrels : building.StoredOilBarrels;
            float drained = Mathf.Min(Mathf.Max(0f, stored), remaining);
            if (drained <= 0f)
                continue;

            if (resourceKind == ResourceKind.Fuel)
                building.StoredFuelBarrels = Mathf.Max(0f, building.StoredFuelBarrels - drained);
            else
                building.StoredOilBarrels = Mathf.Max(0f, building.StoredOilBarrels - drained);

            remaining -= drained;
            if (remaining <= 0.001f)
                break;
        }

        return requestedBarrels - remaining;
    }

    public ResourceProductionTickResult UpdateResourceProduction<TBuilding>(
        IReadOnlyDictionary<int, TBuilding> buildings,
        float secondsPerDay,
        float deltaTime,
        float oilBarrelsPerFuelBarrel)
        where TBuilding : class, IResourceBuilding
    {
        if (buildings == null || buildings.Count == 0)
            return new ResourceProductionTickResult(0f, 0f);

        secondsPerDay = Mathf.Max(1f, secondsPerDay);
        deltaTime = Mathf.Max(0f, deltaTime);
        oilBarrelsPerFuelBarrel = Mathf.Max(0.001f, oilBarrelsPerFuelBarrel);

        float oilExtracted = 0f;
        float fuelProduced = 0f;

        foreach (var pair in buildings)
        {
            TBuilding building = pair.Value;
            if (building == null || building.IsDestroyed)
                continue;

            int oilCapacity = Mathf.Max(0, building.OilStorageCapacity);
            float oilBarrelsPerDay = Mathf.Max(0f, building.OilBarrelsPerDay);
            if (oilCapacity > 0 && oilBarrelsPerDay > 0f)
            {
                if (building.StoredOilBarrels >= oilCapacity)
                {
                    building.StoredOilBarrels = oilCapacity;
                }
                else
                {
                    float barrelsPerSecond = oilBarrelsPerDay / secondsPerDay;
                    float previousOil = building.StoredOilBarrels;
                    building.StoredOilBarrels = Mathf.Min(oilCapacity, building.StoredOilBarrels + barrelsPerSecond * deltaTime);
                    oilExtracted += building.StoredOilBarrels - previousOil;
                }
            }

            float fuelBarrelsPerDay = Mathf.Max(0f, building.FuelBarrelsPerDay);
            int fuelCapacity = Mathf.Max(0, building.FuelStorageCapacity);
            if (fuelBarrelsPerDay <= 0f)
                continue;

            float maxFuelFromOil = building.StoredOilBarrels / oilBarrelsPerFuelBarrel;
            if (maxFuelFromOil <= 0f)
                continue;

            float desiredFuel = (fuelBarrelsPerDay / secondsPerDay) * deltaTime;
            float producedFuel = Mathf.Min(desiredFuel, maxFuelFromOil);
            if (fuelCapacity > 0)
                producedFuel = Mathf.Min(producedFuel, Mathf.Max(0f, fuelCapacity - building.StoredFuelBarrels));

            if (producedFuel <= 0f)
                continue;

            building.StoredOilBarrels = Mathf.Max(0f, building.StoredOilBarrels - (producedFuel * oilBarrelsPerFuelBarrel));
            if (fuelCapacity > 0)
                building.StoredFuelBarrels = Mathf.Min(fuelCapacity, building.StoredFuelBarrels + producedFuel);
            fuelProduced += producedFuel;
        }

        return new ResourceProductionTickResult(oilExtracted, fuelProduced);
    }

    public bool IsResourceStorageBuilding(IResourceBuilding building)
    {
        if (building == null)
            return false;

        bool storesOil = building.OilStorageCapacity > 0;
        bool storesFuel = building.FuelStorageCapacity > 0;
        bool producesOil = building.OilBarrelsPerDay > 0f;
        bool producesFuel = building.FuelBarrelsPerDay > 0f;
        return (storesOil || storesFuel) && !producesOil && !producesFuel;
    }

    public bool IsFactionResourceBuilding(IResourceBuilding building, byte factionId)
    {
        if (building == null || building.IsDestroyed || !building.HasOwnerFaction || building.OwnerFactionId != factionId)
            return false;

        return building.OilStorageCapacity > 0 ||
               building.FuelStorageCapacity > 0 ||
               building.OilBarrelsPerDay > 0f ||
               building.FuelBarrelsPerDay > 0f;
    }
}
