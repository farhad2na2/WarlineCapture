using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public sealed partial class BuildingUiQuerySystem : SystemBase
{
    protected override void OnCreate()
    {
        Enabled = false;
    }

    protected override void OnUpdate()
    {
    }

    public delegate bool TryGetEntityManagerDelegate(out EntityManager entityManager);
    public delegate bool TryGetSelectedBuildingHealthDelegate(out int current, out int max);
    public delegate bool TryGetSelectedBuildingPreviewPrefabDelegate(out GameObject prefab);
    public delegate bool TryGetRuntimeBuildingOwnerFactionDelegate(int buildingId, out byte ownerFactionId);
    public delegate bool TryResolveLiveUnitPreviewPrefabDelegate(Entity unitEntity, out GameObject prefab);

    public readonly struct Context
    {
        internal readonly IReadOnlyDictionary<int, RuntimeBuildingEntity> RuntimeBuildings;
        internal readonly Func<int?> GetActiveBuildingId;
        internal readonly TryGetEntityManagerDelegate TryGetEntityManager;
        internal readonly BuildingProductionSystem ProductionSystem;
        internal readonly Func<float> GetNow;
        internal readonly Func<bool> HasSelectedBuilding;
        internal readonly Func<bool> HasActiveBuilding;
        internal readonly Func<string> GetPlacementStatusText;
        internal readonly Func<string> GetSelectedBuildingLabel;
        internal readonly Func<string> GetSelectedBuildingDisplayName;
        internal readonly Func<string> GetSelectedBuildingDescription;
        internal readonly TryGetSelectedBuildingHealthDelegate TryGetSelectedBuildingHealth;
        internal readonly TryGetSelectedBuildingPreviewPrefabDelegate TryGetSelectedBuildingPreviewPrefab;
        internal readonly BuildingProductionRequestBoundary ProductionRequestSystem;
        internal readonly Func<BuildingProductionRequestBoundary.Context> CreateProductionRequestContext;
        internal readonly Func<int, bool> IsRuntimeBuildingWall;
        internal readonly Func<int, bool> IsRuntimeBuildingCityGenerated;
        internal readonly TryGetRuntimeBuildingOwnerFactionDelegate TryGetRuntimeBuildingOwnerFaction;
        internal readonly Func<Camera, bool> HasVisibleSelectableBuilding;
        internal readonly TryResolveLiveUnitPreviewPrefabDelegate TryResolveLiveUnitPreviewPrefab;

        internal Context(
            IReadOnlyDictionary<int, RuntimeBuildingEntity> runtimeBuildings,
            Func<int?> getActiveBuildingId,
            TryGetEntityManagerDelegate tryGetEntityManager,
            BuildingProductionSystem productionSystem,
            Func<float> getNow,
            Func<bool> hasSelectedBuilding,
            Func<bool> hasActiveBuilding,
            Func<string> getPlacementStatusText,
            Func<string> getSelectedBuildingLabel,
            Func<string> getSelectedBuildingDisplayName,
            Func<string> getSelectedBuildingDescription,
            TryGetSelectedBuildingHealthDelegate tryGetSelectedBuildingHealth,
            TryGetSelectedBuildingPreviewPrefabDelegate tryGetSelectedBuildingPreviewPrefab,
            BuildingProductionRequestBoundary productionRequestSystem,
            Func<BuildingProductionRequestBoundary.Context> createProductionRequestContext,
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
            HasSelectedBuilding = hasSelectedBuilding;
            HasActiveBuilding = hasActiveBuilding;
            GetPlacementStatusText = getPlacementStatusText;
            GetSelectedBuildingLabel = getSelectedBuildingLabel;
            GetSelectedBuildingDisplayName = getSelectedBuildingDisplayName;
            GetSelectedBuildingDescription = getSelectedBuildingDescription;
            TryGetSelectedBuildingHealth = tryGetSelectedBuildingHealth;
            TryGetSelectedBuildingPreviewPrefab = tryGetSelectedBuildingPreviewPrefab;
            ProductionRequestSystem = productionRequestSystem;
            CreateProductionRequestContext = createProductionRequestContext;
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
        public readonly int PendingProductionIndex;
        public readonly GameObject Prefab;
        public readonly float RemainingSeconds;
        public readonly float DurationSeconds;
        public readonly float Progress01;
        public readonly float StartedAt;
        public readonly float ReadyAt;
        public readonly string ProducerDisplayName;

        public PendingProductionUiEntry(int buildingId, GameObject prefab, float remainingSeconds, float durationSeconds, float progress01, float startedAt, float readyAt)
            : this(buildingId, -1, prefab, remainingSeconds, durationSeconds, progress01, startedAt, readyAt, string.Empty)
        {
        }

        public PendingProductionUiEntry(int buildingId, GameObject prefab, float remainingSeconds, float durationSeconds, float progress01, float startedAt, float readyAt, string producerDisplayName)
            : this(buildingId, -1, prefab, remainingSeconds, durationSeconds, progress01, startedAt, readyAt, producerDisplayName)
        {
        }

        public PendingProductionUiEntry(int buildingId, int pendingProductionIndex, GameObject prefab, float remainingSeconds, float durationSeconds, float progress01, float startedAt, float readyAt, string producerDisplayName)
        {
            BuildingId = buildingId;
            PendingProductionIndex = pendingProductionIndex;
            Prefab = prefab;
            RemainingSeconds = remainingSeconds;
            DurationSeconds = durationSeconds;
            Progress01 = progress01;
            StartedAt = startedAt;
            ReadyAt = readyAt;
            ProducerDisplayName = producerDisplayName ?? string.Empty;
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
            !context.RuntimeBuildings.TryGetValue(buildingId.Value, out RuntimeBuildingEntity building) ||
            building == null)
        {
            return;
        }

        building.ProducedUnits ??= new List<Entity>();
        GetProducedUnits(building.ProducedUnits, em, context.ProductionSystem, results);
    }

    internal bool HasSelectedBuilding(Context context)
    {
        return context.HasSelectedBuilding != null &&
               context.HasSelectedBuilding();
    }

    internal bool HasActiveBuilding(Context context)
    {
        return context.HasActiveBuilding != null &&
               context.HasActiveBuilding();
    }

    internal string PlacementStatusText(Context context)
    {
        return context.GetPlacementStatusText?.Invoke() ?? string.Empty;
    }

    internal string SelectedBuildingLabel(Context context)
    {
        return context.GetSelectedBuildingLabel?.Invoke() ?? string.Empty;
    }

    internal string SelectedBuildingDisplayName(Context context)
    {
        return context.GetSelectedBuildingDisplayName?.Invoke() ?? string.Empty;
    }

    internal string SelectedBuildingDescription(Context context)
    {
        return context.GetSelectedBuildingDescription?.Invoke() ?? string.Empty;
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

    internal bool CanCreateUnitFromSelectedBuilding(Context context, int productionIndex)
    {
        return context.ProductionRequestSystem != null &&
               context.ProductionRequestSystem.CanCreateUnitFromSelectedBuilding(
                   context.CreateProductionRequestContext != null ? context.CreateProductionRequestContext() : default,
                   context.GetActiveBuildingId?.Invoke(),
                   productionIndex);
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
        Dictionary<Entity, FixedString64Bytes> producedUnitSourceKeys,
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
            productionSystem?.PruneProducedUnits(
                producedUnits,
                null,
                producedUnitPrefabs,
                entityManager,
                producedUnitSourceKeys);
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
            !context.RuntimeBuildings.TryGetValue(buildingId.Value, out RuntimeBuildingEntity building) ||
            building == null)
        {
            return;
        }

        building.ProducedUnits ??= new List<Entity>();
        building.ProducedUnitPrefabs ??= new Dictionary<Entity, GameObject>();
        AddProducedUnitEntries(
            building.ProducedUnits,
            building.ProducedUnitPrefabs,
            building.ProducedUnitSourceKeys,
            building.PendingProductions,
            em,
            context.ProductionSystem,
            context.GetNow != null ? context.GetNow() : UnityEngine.Time.time,
            entries);
    }

    public void AddPendingProductionUiEntries(
        int buildingId,
        IEnumerable<BuildingProductionSystem.IPendingProduction> pendingProductions,
        BuildingProductionSystem productionSystem,
        float now,
        List<PendingProductionUiEntry> entries,
        string producerDisplayName = "")
    {
        if (pendingProductions == null || productionSystem == null || entries == null)
            return;

        int pendingIndex = 0;
        foreach (BuildingProductionSystem.IPendingProduction pending in pendingProductions)
        {
            if (pending == null || pending.Prefab == null)
            {
                pendingIndex++;
                continue;
            }

            BuildingProductionSystem.PendingProductionProgress progress = productionSystem.GetProgress(pending, now, false);
            entries.Add(new PendingProductionUiEntry(
                buildingId,
                pendingIndex,
                pending.Prefab,
                progress.RemainingSeconds,
                progress.DurationSeconds,
                progress.Progress01,
                pending.StartedAt,
                pending.ReadyAt,
                producerDisplayName));
            pendingIndex++;
        }
    }

    internal void GetFriendlyPendingProductionUiEntries(Context context, List<PendingProductionUiEntry> entries)
    {
        if (entries == null)
            return;

        entries.Clear();
        if (context.RuntimeBuildings == null)
            return;

        float now = context.GetNow != null ? context.GetNow() : UnityEngine.Time.time;
        foreach (KeyValuePair<int, RuntimeBuildingEntity> pair in context.RuntimeBuildings)
        {
            RuntimeBuildingEntity building = pair.Value;
            if (building == null ||
                building.IsDestroyed ||
                building.PendingProductions == null ||
                building.PendingProductions.Count == 0)
            {
                continue;
            }

            if (building.IsCityGenerated)
                continue;
            if (!IsFriendlyProductionBuilding(building))
                continue;

            AddPendingProductionUiEntries(
                pair.Key,
                building.PendingProductions,
                context.ProductionSystem,
                now,
                entries,
                ResolveProducerDisplayName(pair.Key, building));
        }
    }

    private static bool IsFriendlyProductionBuilding(RuntimeBuildingEntity building)
    {
        if (building == null)
            return false;

        return !building.HasOwnerFaction ||
               building.OwnerFactionId == FactionIdentity.NeutralFactionId ||
               building.OwnerFactionId == FactionIdentity.PlayerFactionId;
    }

    private static string ResolveProducerDisplayName(int buildingId, RuntimeBuildingEntity building)
    {
        string displayName = building != null && building.Definition != null
            ? building.Definition.DisplayName
            : string.Empty;
        return string.IsNullOrWhiteSpace(displayName)
            ? $"Building {buildingId}"
            : displayName;
    }
}
