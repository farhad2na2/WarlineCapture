using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public sealed class BuildingProductionQueueCompositionSystemHelper
{
    private const string HelicopterTransportPrefabName = "Unit_Veh_Helicopter_Transport";
    private const string HelicopterTransportLookupKey = "unit_veh_helicopter_transport";
    private const string PlaneTransportPrefabName = "Unit_Veh_Plane_Transport";
    private const string PlaneTransportLookupKey = "unit_veh_plane_transport";
    private const int DefaultPendingProductionPoolPrewarmCount = 256;

    public delegate bool TryGetPrefabLocalBoundsDelegate(GameObject prefab, out Bounds localBounds);
    public delegate bool TryGetUnitProductionMetadataDelegate(GameObject prefab, out UnitProductionMetadata metadata);
    internal delegate bool TryGetRuntimeBoundaryEntityDelegate(EntityManager em, out Entity boundaryEntity);
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

    public readonly struct UnitProductionMetadata
    {
        public readonly float ProductionDurationSeconds;
        public readonly GameObject ProductionTransportPrefab;
        public readonly bool IsAirUnit;
        public readonly float ProductionTransportArrivalSeconds;
        public readonly float ProductionTransportHoldForNextReadySeconds;
        public readonly int ProductionTransportMaxConcurrent;
        public readonly bool ProductionTransportRequiresAirportRunway;
        public readonly bool ProductionTransportUsesRunwayLanding;
        public readonly Vector2Int FootprintCells;

        public UnitProductionMetadata(
            float productionDurationSeconds,
            GameObject productionTransportPrefab,
            bool isAirUnit,
            float productionTransportArrivalSeconds,
            float productionTransportHoldForNextReadySeconds,
            int productionTransportMaxConcurrent,
            bool productionTransportRequiresAirportRunway,
            bool productionTransportUsesRunwayLanding,
            Vector2Int footprintCells)
        {
            ProductionDurationSeconds = productionDurationSeconds;
            ProductionTransportPrefab = productionTransportPrefab;
            IsAirUnit = isAirUnit;
            ProductionTransportArrivalSeconds = productionTransportArrivalSeconds;
            ProductionTransportHoldForNextReadySeconds = productionTransportHoldForNextReadySeconds;
            ProductionTransportMaxConcurrent = productionTransportMaxConcurrent;
            ProductionTransportRequiresAirportRunway = productionTransportRequiresAirportRunway;
            ProductionTransportUsesRunwayLanding = productionTransportUsesRunwayLanding;
            FootprintCells = footprintCells;
        }
    }

    private readonly Dictionary<GameObject, ProductionTransportSettings> _productionTransportSettingsByPrefab = new();
    private IReadOnlyList<GameObject> _cachedTransportUnitSpawnPrefabs;
    private IReadOnlyDictionary<string, GameObject> _cachedTransportUnitSpawnPrefabsByKey;
    private TryGetPrefabLocalBoundsDelegate _cachedTransportBoundsResolver;
    private GameObject _cachedDefaultHelicopterTransportPrefab;
    private GameObject _cachedDefaultPlaneTransportPrefab;
    private TryGetUnitProductionMetadataDelegate _tryGetUnitProductionMetadata;
    private readonly Stack<RuntimeBuildingEntity.PendingProduction> _pendingProductionPool = new();
    private int _createdPendingProductionCount;

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
        public readonly BuildingProductionSlotUtilitySystemHelper ProductionSlotSystem;
        public readonly TryGetPrefabLocalBoundsDelegate TryGetPrefabLocalBounds;
        public readonly RuntimeBuildingMatchesIdDelegate RuntimeBuildingMatchesId;
        public readonly TryGetRuntimeBoundaryEntityDelegate TryGetRuntimeBoundaryEntity;

        public QueueContext(
            IReadOnlyList<GameObject> unitSpawnPrefabs,
            IReadOnlyDictionary<string, GameObject> unitSpawnPrefabsByKey,
            BuildingProductionSlotUtilitySystemHelper productionSlotSystem,
            TryGetPrefabLocalBoundsDelegate tryGetPrefabLocalBounds,
            RuntimeBuildingMatchesIdDelegate runtimeBuildingMatchesId,
            TryGetRuntimeBoundaryEntityDelegate tryGetRuntimeBoundaryEntity = null)
        {
            UnitSpawnPrefabs = unitSpawnPrefabs;
            UnitSpawnPrefabsByKey = unitSpawnPrefabsByKey;
            ProductionSlotSystem = productionSlotSystem;
            TryGetPrefabLocalBounds = tryGetPrefabLocalBounds;
            RuntimeBuildingMatchesId = runtimeBuildingMatchesId;
            TryGetRuntimeBoundaryEntity = tryGetRuntimeBoundaryEntity;
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

        PruneProducedUnits(
            building.ProducedUnits,
            building.ProducedUnitSlots,
            building.ProducedUnitPrefabs,
            entityManager,
            building.ProducedUnitSourceKeys);

        int reservedProductionSlotIndex = -1;
        if (building.ProductionSpawnLocalPositions != null &&
            building.ProducedUnitSlots != null &&
            building.ProductionSpawnLocalPositions.Length > 0)
        {
            TryReserveProductionSlot(context, building, entityManager, out reservedProductionSlotIndex);

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

        RuntimeBuildingEntity.PendingProduction queuedProduction = AcquirePendingProduction();
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

    internal bool HasAvailableProductionSlot(
        QueueContext context,
        RuntimeBuildingEntity building,
        EntityManager entityManager)
    {
        if (building == null)
            return false;

        if (building.ProductionSpawnLocalPositions == null ||
            building.ProducedUnitSlots == null ||
            building.ProductionSpawnLocalPositions.Length <= 0)
        {
            return true;
        }

        return TryFindAvailableProductionSlot(context, building, entityManager, out _);
    }

    private static bool TryReserveProductionSlot(
        QueueContext context,
        RuntimeBuildingEntity building,
        EntityManager entityManager,
        out int reservedProductionSlotIndex)
    {
        reservedProductionSlotIndex = -1;
        if (context.ProductionSlotSystem == null ||
            building?.ProducedUnitSlots == null ||
            building.ProductionSpawnLocalPositions == null ||
            building.ProductionSpawnLocalPositions.Length <= 0)
        {
            return false;
        }

        return TryFindAvailableProductionSlot(context, building, entityManager, out reservedProductionSlotIndex);
    }

    private static bool TryFindAvailableProductionSlot(
        QueueContext context,
        RuntimeBuildingEntity building,
        EntityManager entityManager,
        out int slotIndex)
    {
        slotIndex = -1;
        if (context.ProductionSlotSystem == null ||
            building?.ProducedUnitSlots == null ||
            building.ProductionSpawnLocalPositions == null ||
            building.ProductionSpawnLocalPositions.Length <= 0)
        {
            return false;
        }

        int count = math.min(building.ProductionSpawnLocalPositions.Length, building.ProducedUnitSlots.Length);
        for (int i = 0; i < count; i++)
        {
            if (context.ProductionSlotSystem.IsProductionSlotReservedByPending(building, i) ||
                context.ProductionSlotSystem.IsProductionSlotOccupied(building, entityManager, i) ||
                IsProductionSlotOccupiedByReadModel(context, entityManager, building.Id, i))
            {
                continue;
            }

            slotIndex = i;
            return true;
        }

        return false;
    }

    private static bool IsProductionSlotOccupiedByReadModel(
        QueueContext context,
        EntityManager entityManager,
        int productionSlotBuildingRuntimeId,
        int slotIndex)
    {
        if (productionSlotBuildingRuntimeId <= 0 ||
            slotIndex < 0 ||
            context.TryGetRuntimeBoundaryEntity == null ||
            entityManager.World == null ||
            !entityManager.World.IsCreated ||
            !context.TryGetRuntimeBoundaryEntity(entityManager, out Entity boundaryEntity) ||
            boundaryEntity == Entity.Null ||
            !entityManager.Exists(boundaryEntity) ||
            !entityManager.HasBuffer<BuildingProducedUnitReadModel>(boundaryEntity))
        {
            return false;
        }

        DynamicBuffer<BuildingProducedUnitReadModel> producedUnits =
            entityManager.GetBuffer<BuildingProducedUnitReadModel>(boundaryEntity, true);
        for (int i = 0; i < producedUnits.Length; i++)
        {
            BuildingProducedUnitReadModel producedUnit = producedUnits[i];
            int slotBuildingRuntimeId = producedUnit.ProductionSlotBuildingRuntimeId > 0
                ? producedUnit.ProductionSlotBuildingRuntimeId
                : producedUnit.BuildingRuntimeId;
            if (slotBuildingRuntimeId != productionSlotBuildingRuntimeId ||
                producedUnit.ProductionSlotIndex != slotIndex ||
                !IsProducedUnitAlive(producedUnit.Unit, entityManager))
            {
                continue;
            }

            return true;
        }

        return false;
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

    public void PrewarmPendingProductionPool(int count = DefaultPendingProductionPoolPrewarmCount)
    {
        while (_createdPendingProductionCount < count)
        {
            _pendingProductionPool.Push(new RuntimeBuildingEntity.PendingProduction());
            _createdPendingProductionCount++;
        }
    }

    internal void ReleasePendingProduction(RuntimeBuildingEntity.PendingProduction pending)
    {
        if (pending == null)
            return;

        pending.ProductionIndex = 0;
        pending.Prefab = null;
        pending.StartedAt = 0f;
        pending.ReadyAt = 0f;
        pending.ReservedProductionSlotIndex = 0;
        pending.TransportPrefab = null;
        pending.TransportArrivalSeconds = 0f;
        pending.TransportHoldForNextReadySeconds = 0f;
        pending.TransportMaxConcurrent = 0;
        pending.TransportMode = default;
        pending.TransportRequiresAirportRunway = false;
        _pendingProductionPool.Push(pending);
    }

    private RuntimeBuildingEntity.PendingProduction AcquirePendingProduction()
    {
        if (_pendingProductionPool.Count > 0)
            return _pendingProductionPool.Pop();

        _createdPendingProductionCount++;
        return new RuntimeBuildingEntity.PendingProduction();
    }

    public float ResolveProductionDurationSeconds(GameObject spawnUnitPrefab)
    {
        if (spawnUnitPrefab == null)
            return 60f;

        if (!TryGetUnitProductionMetadata(spawnUnitPrefab, out UnitProductionMetadata metadata))
            return 60f;

        return Mathf.Max(0.01f, metadata.ProductionDurationSeconds);
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

        EnsureProductionTransportCache(unitSpawnPrefabs, unitSpawnPrefabsByKey, tryGetPrefabLocalBounds);
        if (_productionTransportSettingsByPrefab.TryGetValue(spawnUnitPrefab, out ProductionTransportSettings cachedSettings))
            return cachedSettings;

        bool hasProducedMetadata = TryGetUnitProductionMetadata(spawnUnitPrefab, out UnitProductionMetadata producedMetadata);
        transportPrefab = hasProducedMetadata ? producedMetadata.ProductionTransportPrefab : null;

        if (transportPrefab == null)
            transportPrefab = TryResolveDefaultProductionTransportPrefab(spawnUnitPrefab, producedMetadata, hasProducedMetadata, tryGetPrefabLocalBounds);

        if (transportPrefab == null && hasProducedMetadata && producedMetadata.IsAirUnit)
        {
            transportPrefab = spawnUnitPrefab;
            arrivalSeconds = Mathf.Max(0.5f, producedMetadata.ProductionTransportArrivalSeconds);
            holdForNextReadySeconds = Mathf.Max(0.5f, producedMetadata.ProductionTransportHoldForNextReadySeconds);
            maxConcurrent = 64;

            string producedName = spawnUnitPrefab.name;
            bool usesRunwaySelfArrival =
                producedMetadata.ProductionTransportUsesRunwayLanding ||
                producedMetadata.ProductionTransportRequiresAirportRunway ||
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
        {
            return CacheProductionTransportSettings(
                spawnUnitPrefab,
                new ProductionTransportSettings(transportPrefab, arrivalSeconds, holdForNextReadySeconds, maxConcurrent, transportMode, requiresAirportRunway));
        }

        if (TryGetUnitProductionMetadata(transportPrefab, out UnitProductionMetadata transportMetadata))
        {
            arrivalSeconds = transportMetadata.ProductionTransportArrivalSeconds;
            holdForNextReadySeconds = transportMetadata.ProductionTransportHoldForNextReadySeconds;
            maxConcurrent = transportMetadata.ProductionTransportMaxConcurrent;
            requiresAirportRunway = transportMetadata.ProductionTransportRequiresAirportRunway;
            if (transportMetadata.ProductionTransportUsesRunwayLanding)
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

        return CacheProductionTransportSettings(
            spawnUnitPrefab,
            new ProductionTransportSettings(transportPrefab, arrivalSeconds, holdForNextReadySeconds, maxConcurrent, transportMode, requiresAirportRunway));
    }

    public void PrewarmProductionTransportSettings(
        IReadOnlyList<GameObject> unitSpawnPrefabs,
        IReadOnlyDictionary<string, GameObject> unitSpawnPrefabsByKey,
        TryGetPrefabLocalBoundsDelegate tryGetPrefabLocalBounds)
    {
        EnsureProductionTransportCache(unitSpawnPrefabs, unitSpawnPrefabsByKey, tryGetPrefabLocalBounds);
        if (unitSpawnPrefabs == null)
            return;

        for (int i = 0; i < unitSpawnPrefabs.Count; i++)
        {
            GameObject prefab = unitSpawnPrefabs[i];
            if (prefab == null || _productionTransportSettingsByPrefab.ContainsKey(prefab))
                continue;

            ResolveProductionTransportSettings(prefab, unitSpawnPrefabs, unitSpawnPrefabsByKey, tryGetPrefabLocalBounds);
        }
    }

    private void EnsureProductionTransportCache(
        IReadOnlyList<GameObject> unitSpawnPrefabs,
        IReadOnlyDictionary<string, GameObject> unitSpawnPrefabsByKey,
        TryGetPrefabLocalBoundsDelegate tryGetPrefabLocalBounds)
    {
        if (ReferenceEquals(_cachedTransportUnitSpawnPrefabs, unitSpawnPrefabs) &&
            ReferenceEquals(_cachedTransportUnitSpawnPrefabsByKey, unitSpawnPrefabsByKey) &&
            IsSameBoundsResolver(_cachedTransportBoundsResolver, tryGetPrefabLocalBounds))
        {
            return;
        }

        _cachedTransportUnitSpawnPrefabs = unitSpawnPrefabs;
        _cachedTransportUnitSpawnPrefabsByKey = unitSpawnPrefabsByKey;
        _cachedTransportBoundsResolver = tryGetPrefabLocalBounds;
        _cachedDefaultHelicopterTransportPrefab = TryResolveConfiguredUnitPrefab(
            HelicopterTransportPrefabName,
            HelicopterTransportLookupKey,
            unitSpawnPrefabs,
            unitSpawnPrefabsByKey);
        _cachedDefaultPlaneTransportPrefab = TryResolveConfiguredUnitPrefab(
            PlaneTransportPrefabName,
            PlaneTransportLookupKey,
            unitSpawnPrefabs,
            unitSpawnPrefabsByKey);
        _productionTransportSettingsByPrefab.Clear();
    }

    private static bool IsSameBoundsResolver(
        TryGetPrefabLocalBoundsDelegate left,
        TryGetPrefabLocalBoundsDelegate right)
    {
        if (left == null || right == null)
            return left == right;

        return ReferenceEquals(left.Target, right.Target) && left.Method == right.Method;
    }

    private ProductionTransportSettings CacheProductionTransportSettings(
        GameObject spawnUnitPrefab,
        ProductionTransportSettings settings)
    {
        _productionTransportSettingsByPrefab[spawnUnitPrefab] = settings;
        return settings;
    }

    public void ConfigureUnitProductionMetadataResolver(TryGetUnitProductionMetadataDelegate resolver)
    {
        if (IsSameUnitProductionMetadataResolver(_tryGetUnitProductionMetadata, resolver))
            return;

        _tryGetUnitProductionMetadata = resolver;
        _productionTransportSettingsByPrefab.Clear();
    }

    private bool TryGetUnitProductionMetadata(GameObject prefab, out UnitProductionMetadata metadata)
    {
        if (prefab != null &&
            _tryGetUnitProductionMetadata != null &&
            _tryGetUnitProductionMetadata(prefab, out metadata))
        {
            return true;
        }

        metadata = default;
        return false;
    }

    private static bool IsSameUnitProductionMetadataResolver(
        TryGetUnitProductionMetadataDelegate left,
        TryGetUnitProductionMetadataDelegate right)
    {
        if (left == null || right == null)
            return left == right;

        return ReferenceEquals(left.Target, right.Target) && left.Method == right.Method;
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

        TPending pending = pendingProductions[index];
        pendingProductions.RemoveAt(index);
        if (pending is RuntimeBuildingEntity.PendingProduction runtimePending)
            ReleasePendingProduction(runtimePending);
        return true;
    }

    public void PruneProducedUnits(
        List<Entity> producedUnits,
        Entity[] producedUnitSlots,
        Dictionary<Entity, GameObject> producedUnitPrefabs,
        EntityManager entityManager,
        Dictionary<Entity, FixedString64Bytes> producedUnitSourceKeys = null)
    {
        if (producedUnits != null)
        {
            for (int i = producedUnits.Count - 1; i >= 0; i--)
            {
                Entity unit = producedUnits[i];
                if (IsProducedUnitAlive(unit, entityManager))
                    continue;

                producedUnitPrefabs?.Remove(unit);
                producedUnitSourceKeys?.Remove(unit);
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
        UnitProductionMetadata metadata,
        bool hasMetadata,
        TryGetPrefabLocalBoundsDelegate tryGetPrefabLocalBounds)
    {
        if (spawnUnitPrefab == null)
            return null;

        if (!hasMetadata)
            return null;

        GameObject helicopter = _cachedDefaultHelicopterTransportPrefab;
        if (helicopter == null)
            return null;

        if (metadata.IsAirUnit)
            return null;

        bool isLikelyVehicle = IsLikelyGroundVehiclePrefab(spawnUnitPrefab);
        if (!isLikelyVehicle)
            return helicopter;

        Vector2Int size = ResolveEffectiveProductionFootprintCells(spawnUnitPrefab, metadata.FootprintCells, tryGetPrefabLocalBounds);
        if (size.x <= 1 && size.y <= 1)
            return helicopter;

        return _cachedDefaultPlaneTransportPrefab;
    }

    private static GameObject TryResolveConfiguredUnitPrefab(
        string prefabName,
        string prefabKey,
        IReadOnlyList<GameObject> unitSpawnPrefabs,
        IReadOnlyDictionary<string, GameObject> unitSpawnPrefabsByKey)
    {
        if (string.IsNullOrWhiteSpace(prefabName) || string.IsNullOrWhiteSpace(prefabKey))
            return null;

        if (unitSpawnPrefabsByKey != null &&
            unitSpawnPrefabsByKey.TryGetValue(prefabKey, out GameObject prefab) &&
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
        Vector2Int configuredFootprint,
        TryGetPrefabLocalBoundsDelegate tryGetPrefabLocalBounds)
    {
        Vector2Int configured = new(
            Mathf.Max(1, configuredFootprint.x),
            Mathf.Max(1, configuredFootprint.y));
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
