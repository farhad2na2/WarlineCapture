using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public sealed class BuildingProductionSystem
{
    public delegate bool TryGetPrefabLocalBoundsDelegate(GameObject prefab, out Bounds localBounds);
    internal delegate bool RuntimeBuildingMatchesIdDelegate(RuntimeBuildingEntity building, string normalizedBuildingId);

    public enum ProductionTransportMode : byte
    {
        Helicopter = 0,
        Plane = 1,
        AirSelf = 2
    }

    public interface IPendingProduction
    {
        int ProductionIndex { get; set; }
        GameObject Prefab { get; set; }
        float StartedAt { get; set; }
        float ReadyAt { get; set; }
        int ReservedProductionSlotIndex { get; set; }
        GameObject TransportPrefab { get; set; }
        float TransportArrivalSeconds { get; set; }
        float TransportHoldForNextReadySeconds { get; set; }
        int TransportMaxConcurrent { get; set; }
        ProductionTransportMode TransportMode { get; set; }
        bool TransportRequiresAirportRunway { get; set; }
    }

    public readonly struct ProductionTransportSettings
    {
        public readonly GameObject TransportPrefab;
        public readonly float ArrivalSeconds;
        public readonly float HoldForNextReadySeconds;
        public readonly int MaxConcurrent;
        public readonly ProductionTransportMode Mode;
        public readonly bool RequiresAirportRunway;

        public ProductionTransportSettings(
            GameObject transportPrefab,
            float arrivalSeconds,
            float holdForNextReadySeconds,
            int maxConcurrent,
            ProductionTransportMode mode,
            bool requiresAirportRunway)
        {
            TransportPrefab = transportPrefab;
            ArrivalSeconds = arrivalSeconds;
            HoldForNextReadySeconds = holdForNextReadySeconds;
            MaxConcurrent = maxConcurrent;
            Mode = mode;
            RequiresAirportRunway = requiresAirportRunway;
        }
    }

    public readonly struct PendingProductionProgress
    {
        public readonly float DurationSeconds;
        public readonly float RemainingSeconds;
        public readonly float Progress01;

        public PendingProductionProgress(float durationSeconds, float remainingSeconds, float progress01)
        {
            DurationSeconds = durationSeconds;
            RemainingSeconds = remainingSeconds;
            Progress01 = progress01;
        }
    }

    internal readonly struct QueueContext
    {
        public readonly IReadOnlyList<GameObject> UnitSpawnPrefabs;
        public readonly IReadOnlyDictionary<string, GameObject> UnitSpawnPrefabsByKey;
        public readonly BuildingProductionSlotSystem ProductionSlotSystem;
        public readonly TryGetPrefabLocalBoundsDelegate TryGetPrefabLocalBounds;
        public readonly RuntimeBuildingMatchesIdDelegate RuntimeBuildingMatchesId;

        public QueueContext(
            IReadOnlyList<GameObject> unitSpawnPrefabs,
            IReadOnlyDictionary<string, GameObject> unitSpawnPrefabsByKey,
            BuildingProductionSlotSystem productionSlotSystem,
            TryGetPrefabLocalBoundsDelegate tryGetPrefabLocalBounds,
            RuntimeBuildingMatchesIdDelegate runtimeBuildingMatchesId)
        {
            UnitSpawnPrefabs = unitSpawnPrefabs;
            UnitSpawnPrefabsByKey = unitSpawnPrefabsByKey;
            ProductionSlotSystem = productionSlotSystem;
            TryGetPrefabLocalBounds = tryGetPrefabLocalBounds;
            RuntimeBuildingMatchesId = runtimeBuildingMatchesId;
        }
    }

    internal bool TryQueuePlayerUnitFromBuilding(
        QueueContext context,
        RuntimeBuildingEntity building,
        int productionIndex,
        GameObject spawnUnitPrefab,
        EntityManager entityManager,
        float now)
    {
        if (building == null || spawnUnitPrefab == null)
            return false;

        building.PendingProductions ??= new List<RuntimeBuildingEntity.PendingProduction>();
        building.ProducedUnits ??= new List<Entity>();

        PruneProducedUnits(building.ProducedUnits, building.ProducedUnitSlots, building.ProducedUnitPrefabs, entityManager);

        int reservedProductionSlotIndex = -1;
        if (building.ProductionSpawnLocalPositions != null &&
            building.ProducedUnitSlots != null &&
            building.ProductionSpawnLocalPositions.Length > 0)
        {
            context.ProductionSlotSystem?.TryReserveProductionSlot(building, entityManager, out reservedProductionSlotIndex);

            bool allowUnreservedHelicopterHelipadSpawn =
                IsHelicopterUnitPrefab(spawnUnitPrefab) &&
                building.HasOwnerFaction &&
                context.RuntimeBuildingMatchesId != null &&
                context.RuntimeBuildingMatchesId(building, "building_helipad");
            if (reservedProductionSlotIndex < 0 && !allowUnreservedHelicopterHelipadSpawn)
                return false;
        }

        ProductionTransportSettings transportSettings = ResolveProductionTransportSettings(
            spawnUnitPrefab,
            context.UnitSpawnPrefabs,
            context.UnitSpawnPrefabsByKey,
            context.TryGetPrefabLocalBounds);

        RuntimeBuildingEntity.PendingProduction queuedProduction = new();
        InitializePendingProduction(
            queuedProduction,
            productionIndex,
            spawnUnitPrefab,
            now,
            ResolveProductionDurationSeconds(spawnUnitPrefab),
            reservedProductionSlotIndex,
            transportSettings.TransportPrefab,
            transportSettings.ArrivalSeconds,
            transportSettings.HoldForNextReadySeconds,
            transportSettings.MaxConcurrent,
            transportSettings.Mode,
            transportSettings.RequiresAirportRunway);
        building.PendingProductions.Add(queuedProduction);
        RebuildPendingProductionTimeline(building.PendingProductions, now, preserveActiveProgress: true);
        return true;
    }

    public void InitializePendingProduction(
        IPendingProduction pending,
        int productionIndex,
        GameObject spawnUnitPrefab,
        float now,
        float productionDurationSeconds,
        int reservedProductionSlotIndex,
        GameObject transportPrefab,
        float transportArrivalSeconds,
        float transportHoldForNextReadySeconds,
        int transportMaxConcurrent,
        ProductionTransportMode transportMode,
        bool transportRequiresAirportRunway)
    {
        if (pending == null)
            return;

        pending.ProductionIndex = productionIndex;
        pending.Prefab = spawnUnitPrefab;
        pending.StartedAt = now;
        pending.ReadyAt = now + Mathf.Max(0.01f, productionDurationSeconds);
        pending.ReservedProductionSlotIndex = reservedProductionSlotIndex;
        pending.TransportPrefab = transportPrefab;
        pending.TransportArrivalSeconds = transportArrivalSeconds;
        pending.TransportHoldForNextReadySeconds = transportHoldForNextReadySeconds;
        pending.TransportMaxConcurrent = transportMaxConcurrent;
        pending.TransportMode = transportMode;
        pending.TransportRequiresAirportRunway = transportRequiresAirportRunway;
    }

    public float ResolveProductionDurationSeconds(GameObject spawnUnitPrefab)
    {
        if (spawnUnitPrefab == null)
            return 60f;

        UnitGridAuthoring authoring = spawnUnitPrefab.GetComponent<UnitGridAuthoring>();
        if (authoring == null)
            return 60f;

        return Mathf.Max(0.01f, authoring.ProductionDurationSeconds);
    }

    public ProductionTransportSettings ResolveProductionTransportSettings(
        GameObject spawnUnitPrefab,
        IReadOnlyList<GameObject> unitSpawnPrefabs,
        IReadOnlyDictionary<string, GameObject> unitSpawnPrefabsByKey,
        TryGetPrefabLocalBoundsDelegate tryGetPrefabLocalBounds)
    {
        GameObject transportPrefab = null;
        float arrivalSeconds = 5f;
        float holdForNextReadySeconds = 4f;
        int maxConcurrent = 1;
        ProductionTransportMode transportMode = ProductionTransportMode.Helicopter;
        bool requiresAirportRunway = false;
        if (spawnUnitPrefab == null)
            return new ProductionTransportSettings(transportPrefab, arrivalSeconds, holdForNextReadySeconds, maxConcurrent, transportMode, requiresAirportRunway);

        UnitGridAuthoring producedAuthoring = spawnUnitPrefab.GetComponent<UnitGridAuthoring>();
        transportPrefab = producedAuthoring != null ? producedAuthoring.ProductionTransportPrefab : null;

        if (transportPrefab == null)
            transportPrefab = TryResolveDefaultProductionTransportPrefab(spawnUnitPrefab, unitSpawnPrefabs, unitSpawnPrefabsByKey, tryGetPrefabLocalBounds);

        if (transportPrefab == null && producedAuthoring != null && producedAuthoring.IsAirUnit)
        {
            transportPrefab = spawnUnitPrefab;
            arrivalSeconds = Mathf.Max(0.5f, producedAuthoring.ProductionTransportArrivalSeconds);
            holdForNextReadySeconds = Mathf.Max(0.5f, producedAuthoring.ProductionTransportHoldForNextReadySeconds);
            maxConcurrent = 64;

            string producedName = spawnUnitPrefab.name;
            bool usesRunwaySelfArrival =
                producedAuthoring.ProductionTransportUsesRunwayLanding ||
                producedAuthoring.ProductionTransportRequiresAirportRunway ||
                producedName.IndexOf("Plane", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                producedName.IndexOf("Drone", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                producedName.IndexOf("Jet", System.StringComparison.OrdinalIgnoreCase) >= 0;

            if (usesRunwaySelfArrival)
            {
                transportMode = ProductionTransportMode.Plane;
                requiresAirportRunway = true;
                maxConcurrent = 1;
            }
            else
            {
                transportMode = ProductionTransportMode.AirSelf;
            }
        }

        if (transportPrefab == null)
            return new ProductionTransportSettings(transportPrefab, arrivalSeconds, holdForNextReadySeconds, maxConcurrent, transportMode, requiresAirportRunway);

        UnitGridAuthoring transportAuthoring = transportPrefab.GetComponent<UnitGridAuthoring>();
        if (transportAuthoring != null)
        {
            arrivalSeconds = transportAuthoring.ProductionTransportArrivalSeconds;
            holdForNextReadySeconds = transportAuthoring.ProductionTransportHoldForNextReadySeconds;
            maxConcurrent = transportAuthoring.ProductionTransportMaxConcurrent;
            requiresAirportRunway = transportAuthoring.ProductionTransportRequiresAirportRunway;
            if (transportAuthoring.ProductionTransportUsesRunwayLanding)
                transportMode = ProductionTransportMode.Plane;
        }

        if (string.Equals(transportPrefab.name, "Unit_Veh_Helicopter_Transport", System.StringComparison.Ordinal))
        {
            maxConcurrent = Mathf.Max(2, maxConcurrent);
        }
        else if (string.Equals(transportPrefab.name, "Unit_Veh_Plane_Transport", System.StringComparison.Ordinal))
        {
            maxConcurrent = 1;
            requiresAirportRunway = true;
            transportMode = ProductionTransportMode.Plane;
        }

        return new ProductionTransportSettings(transportPrefab, arrivalSeconds, holdForNextReadySeconds, maxConcurrent, transportMode, requiresAirportRunway);
    }

    public bool IsHelicopterUnitPrefab(GameObject prefab)
    {
        return prefab != null && prefab.name.StartsWith("Unit_Veh_Helicopter_", System.StringComparison.OrdinalIgnoreCase);
    }

    public PendingProductionProgress GetProgress(IPendingProduction pending, float now, bool capTransportProgress)
    {
        if (pending == null)
            return new PendingProductionProgress(0f, 0f, 0f);

        float duration = Mathf.Max(0.01f, pending.ReadyAt - pending.StartedAt);
        float remaining = Mathf.Max(0f, pending.ReadyAt - now);
        float progress = Mathf.Clamp01((now - pending.StartedAt) / duration);
        if (capTransportProgress && pending.TransportPrefab != null)
            progress = Mathf.Min(progress, 0.97f);

        return new PendingProductionProgress(duration, remaining, progress);
    }

    public bool IsReady(IPendingProduction pending, float now)
    {
        return pending != null && now >= pending.ReadyAt;
    }

    public bool IsReadyWithin(IPendingProduction pending, float now, float maxSeconds)
    {
        if (pending == null)
            return false;

        float remaining = pending.ReadyAt - now;
        return remaining > 0f && remaining <= Mathf.Max(0f, maxSeconds);
    }

    public float GetTransportLaunchAt(IPendingProduction pending)
    {
        if (pending == null)
            return float.PositiveInfinity;

        return pending.ReadyAt - Mathf.Max(0.5f, pending.TransportArrivalSeconds);
    }

    public bool ShouldLaunchTransport(IPendingProduction pending, float now)
    {
        return pending != null &&
               pending.TransportPrefab != null &&
               now >= GetTransportLaunchAt(pending);
    }

    public void RebuildPendingProductionTimeline<TPending>(
        IList<TPending> pendingProductions,
        float now,
        bool preserveActiveProgress)
        where TPending : class, IPendingProduction
    {
        if (pendingProductions == null || pendingProductions.Count == 0)
            return;

        float nextStartAt = now;
        bool activeTimelineAssigned = false;
        for (int i = 0; i < pendingProductions.Count; i++)
        {
            TPending pending = pendingProductions[i];
            if (pending == null)
                continue;

            float duration = Mathf.Max(0.01f, pending.ReadyAt - pending.StartedAt);
            if (!activeTimelineAssigned)
            {
                if (preserveActiveProgress)
                    nextStartAt = pending.StartedAt;

                activeTimelineAssigned = true;
            }

            pending.StartedAt = nextStartAt;
            pending.ReadyAt = nextStartAt + duration;
            nextStartAt = pending.ReadyAt;
        }
    }

    public void DelayPendingProduction(IPendingProduction pending, float deltaTime)
    {
        if (pending == null)
            return;

        float delay = Mathf.Max(0f, deltaTime);
        pending.StartedAt += delay;
        pending.ReadyAt += delay;
    }

    public TPending FindNextReadyTransportPending<TPending>(
        IReadOnlyList<TPending> pendingProductions,
        GameObject transportPrefab,
        float now)
        where TPending : class, IPendingProduction
    {
        if (pendingProductions == null || transportPrefab == null)
            return null;

        for (int i = 0; i < pendingProductions.Count; i++)
        {
            TPending pending = pendingProductions[i];
            if (pending == null || pending.TransportPrefab != transportPrefab)
                continue;
            if (IsReady(pending, now))
                return pending;
        }

        return null;
    }

    public TPending FindNextSoonTransportPending<TPending>(
        IReadOnlyList<TPending> pendingProductions,
        GameObject transportPrefab,
        float now,
        float maxSeconds)
        where TPending : class, IPendingProduction
    {
        if (pendingProductions == null || transportPrefab == null)
            return null;

        for (int i = 0; i < pendingProductions.Count; i++)
        {
            TPending pending = pendingProductions[i];
            if (pending == null || pending.TransportPrefab != transportPrefab)
                continue;
            if (IsReadyWithin(pending, now, maxSeconds))
                return pending;
        }

        return null;
    }

    public bool RemovePendingProduction<TPending>(IList<TPending> pendingProductions, TPending pending)
        where TPending : class, IPendingProduction
    {
        if (pendingProductions == null || pending == null)
            return false;

        int index = pendingProductions.IndexOf(pending);
        if (index < 0)
            return false;

        pendingProductions.RemoveAt(index);
        return true;
    }

    public bool RemovePendingAt<TPending>(IList<TPending> pendingProductions, int index)
        where TPending : class, IPendingProduction
    {
        if (pendingProductions == null || index < 0 || index >= pendingProductions.Count)
            return false;

        pendingProductions.RemoveAt(index);
        return true;
    }

    public void PruneProducedUnits(
        List<Entity> producedUnits,
        Entity[] producedUnitSlots,
        Dictionary<Entity, GameObject> producedUnitPrefabs,
        EntityManager entityManager)
    {
        if (producedUnits != null)
        {
            for (int i = producedUnits.Count - 1; i >= 0; i--)
            {
                Entity unit = producedUnits[i];
                if (IsProducedUnitAlive(unit, entityManager))
                    continue;

                producedUnitPrefabs?.Remove(unit);
                producedUnits.RemoveAt(i);
            }
        }

        if (producedUnitSlots == null)
            return;

        for (int i = 0; i < producedUnitSlots.Length; i++)
        {
            Entity unit = producedUnitSlots[i];
            if (unit != Entity.Null && !IsProducedUnitAlive(unit, entityManager))
                producedUnitSlots[i] = Entity.Null;
        }
    }

    private static bool IsProducedUnitAlive(Entity unit, EntityManager entityManager)
    {
        if (unit == Entity.Null || !entityManager.Exists(unit))
            return false;

        return !entityManager.HasComponent<UnitHealth>(unit) ||
               entityManager.GetComponentData<UnitHealth>(unit).Current > 0;
    }

    private GameObject TryResolveDefaultProductionTransportPrefab(
        GameObject spawnUnitPrefab,
        IReadOnlyList<GameObject> unitSpawnPrefabs,
        IReadOnlyDictionary<string, GameObject> unitSpawnPrefabsByKey,
        TryGetPrefabLocalBoundsDelegate tryGetPrefabLocalBounds)
    {
        if (spawnUnitPrefab == null)
            return null;

        UnitGridAuthoring authoring = spawnUnitPrefab.GetComponent<UnitGridAuthoring>();
        if (authoring == null)
            return null;

        GameObject helicopter = TryResolveConfiguredUnitPrefab(
            "Unit_Veh_Helicopter_Transport",
            unitSpawnPrefabs,
            unitSpawnPrefabsByKey);
        if (helicopter == null)
            return null;

        if (authoring.IsAirUnit)
            return null;

        bool isLikelyVehicle = IsLikelyGroundVehiclePrefab(spawnUnitPrefab);
        if (!isLikelyVehicle)
            return helicopter;

        Vector2Int size = ResolveEffectiveProductionFootprintCells(spawnUnitPrefab, authoring, tryGetPrefabLocalBounds);
        if (size.x <= 1 && size.y <= 1)
            return helicopter;

        return TryResolveConfiguredUnitPrefab(
            "Unit_Veh_Plane_Transport",
            unitSpawnPrefabs,
            unitSpawnPrefabsByKey);
    }

    private static GameObject TryResolveConfiguredUnitPrefab(
        string prefabName,
        IReadOnlyList<GameObject> unitSpawnPrefabs,
        IReadOnlyDictionary<string, GameObject> unitSpawnPrefabsByKey)
    {
        if (string.IsNullOrWhiteSpace(prefabName))
            return null;

        string key = NormalizeLookupKey(prefabName);
        if (unitSpawnPrefabsByKey != null &&
            unitSpawnPrefabsByKey.TryGetValue(key, out GameObject prefab) &&
            prefab != null)
        {
            return prefab;
        }

        if (unitSpawnPrefabs == null)
            return null;

        for (int i = 0; i < unitSpawnPrefabs.Count; i++)
        {
            GameObject candidate = unitSpawnPrefabs[i];
            if (candidate != null && string.Equals(candidate.name, prefabName, System.StringComparison.Ordinal))
                return candidate;
        }

        return null;
    }

    private static bool IsLikelyGroundVehiclePrefab(GameObject prefab)
    {
        if (prefab == null)
            return false;

        string name = prefab.name;
        if (name.IndexOf("_Veh_", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return true;

        if (name.IndexOf("Vehicle", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return true;

        if (name.IndexOf("Tank", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return true;

        if (name.IndexOf("APC", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return true;

        return false;
    }

    private static Vector2Int ResolveEffectiveProductionFootprintCells(
        GameObject spawnUnitPrefab,
        UnitGridAuthoring authoring,
        TryGetPrefabLocalBoundsDelegate tryGetPrefabLocalBounds)
    {
        Vector2Int configured = authoring != null ? authoring.GetConfiguredFootprintCells() : Vector2Int.one;
        if (configured.x > 1 || configured.y > 1)
            return configured;

        if (tryGetPrefabLocalBounds != null && tryGetPrefabLocalBounds(spawnUnitPrefab, out Bounds localBounds))
        {
            Vector2Int modelFootprint = new(
                Mathf.Max(1, Mathf.CeilToInt(localBounds.size.x)),
                Mathf.Max(1, Mathf.CeilToInt(localBounds.size.z)));
            if (modelFootprint.x > configured.x || modelFootprint.y > configured.y)
                return modelFootprint;
        }

        return configured;
    }

    private static string NormalizeLookupKey(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();
    }
}
