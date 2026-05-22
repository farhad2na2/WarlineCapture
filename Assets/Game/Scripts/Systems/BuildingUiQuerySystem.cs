using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public sealed class BuildingUiQuerySystem
{
    public void GetProducedUnits(
        List<Entity> producedUnits,
        EntityManager entityManager,
        BuildingProductionSystem productionSystem,
        List<Entity> results)
    {
        results?.Clear();
        if (producedUnits == null || productionSystem == null || results == null)
            return;

        productionSystem.PruneProducedUnits(producedUnits, null, null, entityManager);
        for (int i = 0; i < producedUnits.Count; i++)
            results.Add(producedUnits[i]);
    }

    public void AddProducedUnitEntries(
        List<Entity> producedUnits,
        Dictionary<Entity, GameObject> producedUnitPrefabs,
        IEnumerable<BuildingProductionSystem.IPendingProduction> pendingProductions,
        EntityManager entityManager,
        BuildingProductionSystem productionSystem,
        float now,
        List<BuildingPlacementSystem.ProducedUnitUiEntry> entries)
    {
        if (entries == null)
            return;

        if (producedUnits != null)
        {
            productionSystem?.PruneProducedUnits(producedUnits, null, producedUnitPrefabs, entityManager);
            for (int i = 0; i < producedUnits.Count; i++)
            {
                Entity unit = producedUnits[i];
                GameObject prefab = null;
                producedUnitPrefabs?.TryGetValue(unit, out prefab);
                entries.Add(new BuildingPlacementSystem.ProducedUnitUiEntry(unit, prefab, true, 1f));
            }
        }

        AddPendingProducedUnitEntries(pendingProductions, productionSystem, now, entries);
    }

    public void AddPendingProducedUnitEntries(
        IEnumerable<BuildingProductionSystem.IPendingProduction> pendingProductions,
        BuildingProductionSystem productionSystem,
        float now,
        List<BuildingPlacementSystem.ProducedUnitUiEntry> entries)
    {
        if (pendingProductions == null || productionSystem == null || entries == null)
            return;

        foreach (BuildingProductionSystem.IPendingProduction pending in pendingProductions)
        {
            if (pending == null || pending.Prefab == null)
                continue;

            BuildingProductionSystem.PendingProductionProgress progress = productionSystem.GetProgress(pending, now, true);
            entries.Add(new BuildingPlacementSystem.ProducedUnitUiEntry(Entity.Null, pending.Prefab, false, progress.Progress01));
        }
    }

    public void AddPendingProductionUiEntries(
        int buildingId,
        IEnumerable<BuildingProductionSystem.IPendingProduction> pendingProductions,
        BuildingProductionSystem productionSystem,
        float now,
        List<BuildingPlacementSystem.PendingProductionUiEntry> entries)
    {
        if (pendingProductions == null || productionSystem == null || entries == null)
            return;

        foreach (BuildingProductionSystem.IPendingProduction pending in pendingProductions)
        {
            if (pending == null || pending.Prefab == null)
                continue;

            BuildingProductionSystem.PendingProductionProgress progress = productionSystem.GetProgress(pending, now, false);
            entries.Add(new BuildingPlacementSystem.PendingProductionUiEntry(
                buildingId,
                pending.Prefab,
                progress.RemainingSeconds,
                progress.DurationSeconds,
                progress.Progress01,
                pending.StartedAt,
                pending.ReadyAt));
        }
    }
}
