using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public sealed class BuildingUiQuerySystem
{
    public readonly struct ProducedUnitUiEntry
    {
        public readonly Entity Unit;
        public readonly GameObject Prefab;
        public readonly bool IsReady;
        public readonly float Progress01;

        public ProducedUnitUiEntry(Entity unit, GameObject prefab, bool isReady, float progress01)
        {
            Unit = unit;
            Prefab = prefab;
            IsReady = isReady;
            Progress01 = progress01;
        }
    }

    public readonly struct PendingProductionUiEntry
    {
        public readonly int BuildingId;
        public readonly GameObject Prefab;
        public readonly float RemainingSeconds;
        public readonly float DurationSeconds;
        public readonly float Progress01;
        public readonly float StartedAt;
        public readonly float ReadyAt;

        public PendingProductionUiEntry(int buildingId, GameObject prefab, float remainingSeconds, float durationSeconds, float progress01, float startedAt, float readyAt)
        {
            BuildingId = buildingId;
            Prefab = prefab;
            RemainingSeconds = remainingSeconds;
            DurationSeconds = durationSeconds;
            Progress01 = progress01;
            StartedAt = startedAt;
            ReadyAt = readyAt;
        }
    }

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
        List<ProducedUnitUiEntry> entries)
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
                entries.Add(new ProducedUnitUiEntry(unit, prefab, true, 1f));
            }
        }

        AddPendingProducedUnitEntries(pendingProductions, productionSystem, now, entries);
    }

    public void AddPendingProducedUnitEntries(
        IEnumerable<BuildingProductionSystem.IPendingProduction> pendingProductions,
        BuildingProductionSystem productionSystem,
        float now,
        List<ProducedUnitUiEntry> entries)
    {
        if (pendingProductions == null || productionSystem == null || entries == null)
            return;

        foreach (BuildingProductionSystem.IPendingProduction pending in pendingProductions)
        {
            if (pending == null || pending.Prefab == null)
                continue;

            BuildingProductionSystem.PendingProductionProgress progress = productionSystem.GetProgress(pending, now, true);
            entries.Add(new ProducedUnitUiEntry(Entity.Null, pending.Prefab, false, progress.Progress01));
        }
    }

    public void AddPendingProductionUiEntries(
        int buildingId,
        IEnumerable<BuildingProductionSystem.IPendingProduction> pendingProductions,
        BuildingProductionSystem productionSystem,
        float now,
        List<PendingProductionUiEntry> entries)
    {
        if (pendingProductions == null || productionSystem == null || entries == null)
            return;

        foreach (BuildingProductionSystem.IPendingProduction pending in pendingProductions)
        {
            if (pending == null || pending.Prefab == null)
                continue;

            BuildingProductionSystem.PendingProductionProgress progress = productionSystem.GetProgress(pending, now, false);
            entries.Add(new PendingProductionUiEntry(
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
