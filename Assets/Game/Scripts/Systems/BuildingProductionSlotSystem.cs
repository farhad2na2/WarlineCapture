using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using RuntimeBuildingData = BuildingPlacementSystem.RuntimeBuildingData;

internal sealed class BuildingProductionSlotSystem
{
    public bool TryReserveProductionSlot(RuntimeBuildingData building, EntityManager entityManager, out int reservedProductionSlotIndex)
    {
        reservedProductionSlotIndex = -1;
        if (building?.ProducedUnitSlots == null ||
            building.ProductionSpawnLocalPositions == null ||
            building.ProductionSpawnLocalPositions.Length <= 0)
        {
            return false;
        }

        int count = math.min(building.ProductionSpawnLocalPositions.Length, building.ProducedUnitSlots.Length);
        for (int i = 0; i < count; i++)
        {
            if (IsProductionSlotReservedByPending(building, i))
                continue;

            Entity occupant = building.ProducedUnitSlots[i];
            if (IsProducedUnitAlive(occupant, entityManager))
                continue;

            if (occupant != Entity.Null)
                building.ProducedUnitSlots[i] = Entity.Null;

            reservedProductionSlotIndex = i;
            return true;
        }

        return false;
    }

    public bool TryGetAvailableProductionSpawnSlot(RuntimeBuildingData building, EntityManager em, out int slotIndex, out Vector3 spawnLocalPosition)
    {
        slotIndex = -1;
        spawnLocalPosition = Vector3.zero;
        if (building == null || building.ProductionSpawnLocalPositions == null || building.ProducedUnitSlots == null)
            return false;

        int count = math.min(building.ProductionSpawnLocalPositions.Length, building.ProducedUnitSlots.Length);
        for (int i = 0; i < count; i++)
        {
            if (IsProductionSlotOccupied(building, em, i))
                continue;

            slotIndex = i;
            spawnLocalPosition = building.ProductionSpawnLocalPositions[i];
            return true;
        }

        return false;
    }

    public bool IsProductionSlotReservedByPending(RuntimeBuildingData building, int slotIndex)
    {
        IReadOnlyList<RuntimeBuildingData.PendingProduction> pendingProductions = building?.PendingProductions;
        if (pendingProductions == null)
            return false;

        for (int i = 0; i < pendingProductions.Count; i++)
        {
            RuntimeBuildingData.PendingProduction pending = pendingProductions[i];
            if (pending != null && pending.ReservedProductionSlotIndex == slotIndex)
                return true;
        }

        return false;
    }

    public bool IsProductionSlotOccupied(RuntimeBuildingData building, EntityManager em, int slotIndex)
    {
        if (building?.ProducedUnitSlots == null ||
            slotIndex < 0 ||
            slotIndex >= building.ProducedUnitSlots.Length)
        {
            return false;
        }

        Entity occupant = building.ProducedUnitSlots[slotIndex];
        bool occupied = IsProducedUnitAlive(occupant, em);
        if (!occupied && occupant != Entity.Null)
            building.ProducedUnitSlots[slotIndex] = Entity.Null;

        return occupied;
    }

    private static bool IsProducedUnitAlive(Entity unit, EntityManager entityManager)
    {
        if (unit == Entity.Null || !entityManager.Exists(unit))
            return false;

        return !entityManager.HasComponent<UnitHealth>(unit) ||
               entityManager.GetComponentData<UnitHealth>(unit).Current > 0;
    }
}
