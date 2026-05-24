using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public sealed class BuildingUiQuerySystem
{
    public delegate bool TryGetEntityManagerDelegate(out EntityManager entityManager);
    public delegate bool TryGetSelectedBuildingHealthDelegate(out int current, out int max);
    public delegate bool TryGetSelectedBuildingPreviewPrefabDelegate(out GameObject prefab);
    public delegate bool TryGetRuntimeBuildingOwnerFactionDelegate(int buildingId, out byte ownerFactionId);
    public delegate bool TryResolveLiveUnitPreviewPrefabDelegate(Entity unitEntity, out GameObject prefab);

    internal readonly struct Context
    {
        public readonly IReadOnlyDictionary<int, RuntimeBuildingData> RuntimeBuildings;
        public readonly Func<int?> GetActiveBuildingId;
        public readonly TryGetEntityManagerDelegate TryGetEntityManager;
        public readonly BuildingProductionSystem ProductionSystem;
        public readonly Func<float> GetNow;
        public readonly Func<bool> HasActiveBuilding;
        public readonly Func<string> GetSelectedBuildingDisplayName;
        public readonly TryGetSelectedBuildingHealthDelegate TryGetSelectedBuildingHealth;
        public readonly TryGetSelectedBuildingPreviewPrefabDelegate TryGetSelectedBuildingPreviewPrefab;
        public readonly Func<int, bool> IsRuntimeBuildingWall;
        public readonly Func<int, bool> IsRuntimeBuildingCityGenerated;
        public readonly TryGetRuntimeBuildingOwnerFactionDelegate TryGetRuntimeBuildingOwnerFaction;
        public readonly Func<Camera, bool> HasVisibleSelectableBuilding;
        public readonly TryResolveLiveUnitPreviewPrefabDelegate TryResolveLiveUnitPreviewPrefab;

        public Context(
            IReadOnlyDictionary<int, RuntimeBuildingData> runtimeBuildings,
            Func<int?> getActiveBuildingId,
            TryGetEntityManagerDelegate tryGetEntityManager,
            BuildingProductionSystem productionSystem,
            Func<float> getNow,
            Func<bool> hasActiveBuilding,
            Func<string> getSelectedBuildingDisplayName,
            TryGetSelectedBuildingHealthDelegate tryGetSelectedBuildingHealth,
            TryGetSelectedBuildingPreviewPrefabDelegate tryGetSelectedBuildingPreviewPrefab,
            Func<int, bool> isRuntimeBuildingWall,
            Func<int, bool> isRuntimeBuildingCityGenerated,
            TryGetRuntimeBuildingOwnerFactionDelegate tryGetRuntimeBuildingOwnerFaction,
            Func<Camera, bool> hasVisibleSelectableBuilding,
            TryResolveLiveUnitPreviewPrefabDelegate tryResolveLiveUnitPreviewPrefab)
        {
            RuntimeBuildings = runtimeBuildings;
            GetActiveBuildingId = getActiveBuildingId;
            TryGetEntityManager = tryGetEntityManager;
            ProductionSystem = productionSystem;
            GetNow = getNow;
            HasActiveBuilding = hasActiveBuilding;
            GetSelectedBuildingDisplayName = getSelectedBuildingDisplayName;
            TryGetSelectedBuildingHealth = tryGetSelectedBuildingHealth;
            TryGetSelectedBuildingPreviewPrefab = tryGetSelectedBuildingPreviewPrefab;
            IsRuntimeBuildingWall = isRuntimeBuildingWall;
            IsRuntimeBuildingCityGenerated = isRuntimeBuildingCityGenerated;
            TryGetRuntimeBuildingOwnerFaction = tryGetRuntimeBuildingOwnerFaction;
            HasVisibleSelectableBuilding = hasVisibleSelectableBuilding;
            TryResolveLiveUnitPreviewPrefab = tryResolveLiveUnitPreviewPrefab;
        }
    }

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

    internal void GetSelectedBuildingProducedUnits(Context context, List<Entity> results)
    {
        results?.Clear();
        if (results == null ||
            context.RuntimeBuildings == null ||
            context.GetActiveBuildingId == null ||
            context.TryGetEntityManager == null ||
            !context.TryGetEntityManager(out EntityManager em))
        {
            return;
        }

        int? buildingId = context.GetActiveBuildingId();
        if (!buildingId.HasValue ||
            !context.RuntimeBuildings.TryGetValue(buildingId.Value, out RuntimeBuildingData building) ||
            building == null)
        {
            return;
        }

        building.ProducedUnits ??= new List<Entity>();
        GetProducedUnits(building.ProducedUnits, em, context.ProductionSystem, results);
    }

    internal bool HasActiveBuilding(Context context)
    {
        return context.HasActiveBuilding != null &&
               context.HasActiveBuilding();
    }

    internal string SelectedBuildingDisplayName(Context context)
    {
        return context.GetSelectedBuildingDisplayName?.Invoke() ?? string.Empty;
    }

    internal bool TryGetSelectedBuildingHealth(Context context, out int current, out int max)
    {
        current = 0;
        max = 0;
        return context.TryGetSelectedBuildingHealth != null &&
               context.TryGetSelectedBuildingHealth(out current, out max);
    }

    internal bool TryGetSelectedBuildingPreviewPrefab(Context context, out GameObject prefab)
    {
        prefab = null;
        return context.TryGetSelectedBuildingPreviewPrefab != null &&
               context.TryGetSelectedBuildingPreviewPrefab(out prefab);
    }

    internal bool IsRuntimeBuildingWall(Context context, int buildingId)
    {
        return context.IsRuntimeBuildingWall != null &&
               context.IsRuntimeBuildingWall(buildingId);
    }

    internal bool IsRuntimeBuildingCityGenerated(Context context, int buildingId)
    {
        return context.IsRuntimeBuildingCityGenerated != null &&
               context.IsRuntimeBuildingCityGenerated(buildingId);
    }

    internal bool TryGetRuntimeBuildingOwnerFaction(Context context, int buildingId, out byte ownerFactionId)
    {
        ownerFactionId = 0;
        return context.TryGetRuntimeBuildingOwnerFaction != null &&
               context.TryGetRuntimeBuildingOwnerFaction(buildingId, out ownerFactionId);
    }

    internal bool HasVisibleSelectableBuilding(Context context, Camera camera)
    {
        return context.HasVisibleSelectableBuilding != null &&
               context.HasVisibleSelectableBuilding(camera);
    }

    internal bool TryResolveLiveUnitPreviewPrefab(Context context, Entity unitEntity, out GameObject prefab)
    {
        prefab = null;
        return context.TryResolveLiveUnitPreviewPrefab != null &&
               context.TryResolveLiveUnitPreviewPrefab(unitEntity, out prefab);
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

    internal void GetSelectedBuildingProducedUnitEntries(Context context, List<ProducedUnitUiEntry> entries)
    {
        entries?.Clear();
        if (entries == null ||
            context.RuntimeBuildings == null ||
            context.GetActiveBuildingId == null ||
            context.TryGetEntityManager == null ||
            !context.TryGetEntityManager(out EntityManager em))
        {
            return;
        }

        int? buildingId = context.GetActiveBuildingId();
        if (!buildingId.HasValue ||
            !context.RuntimeBuildings.TryGetValue(buildingId.Value, out RuntimeBuildingData building) ||
            building == null)
        {
            return;
        }

        building.ProducedUnits ??= new List<Entity>();
        building.ProducedUnitPrefabs ??= new Dictionary<Entity, GameObject>();
        AddProducedUnitEntries(
            building.ProducedUnits,
            building.ProducedUnitPrefabs,
            building.PendingProductions,
            em,
            context.ProductionSystem,
            context.GetNow != null ? context.GetNow() : Time.time,
            entries);
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

    internal void GetFriendlyPendingProductionUiEntries(Context context, List<PendingProductionUiEntry> entries)
    {
        if (entries == null)
            return;

        entries.Clear();
        if (context.RuntimeBuildings == null)
            return;

        float now = context.GetNow != null ? context.GetNow() : Time.time;
        foreach (KeyValuePair<int, RuntimeBuildingData> pair in context.RuntimeBuildings)
        {
            RuntimeBuildingData building = pair.Value;
            if (building == null ||
                building.IsDestroyed ||
                building.PendingProductions == null ||
                building.PendingProductions.Count == 0)
            {
                continue;
            }

            if (building.IsCityGenerated)
                continue;
            if (building.HasOwnerFaction && building.OwnerFactionId != 0)
                continue;

            AddPendingProductionUiEntries(
                pair.Key,
                building.PendingProductions,
                context.ProductionSystem,
                now,
                entries);
        }
    }
}
