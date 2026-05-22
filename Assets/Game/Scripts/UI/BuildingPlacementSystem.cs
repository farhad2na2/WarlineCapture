using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using System.Globalization;
using SnivelerCode.GpuAnimation.Scripts.Authoring;
using SnivelerCode.GpuAnimation.Scripts.Components;
using static UnityEngine.Object;

public sealed class BuildingPlacementSystem
{
    private enum ResourceHaulPhase : byte
    {
        None = 0,
        ToSource = 1,
        Loading = 2,
        ToDestination = 3,
        Unloading = 4
    }

    private enum ResourceHaulKind : byte
    {
        Oil = 0,
        Fuel = 1
    }

    private enum DragFirstAxis
    {
        None,
        Horizontal,
        Vertical
    }

    private enum ProductionTransportMode : byte
    {
        Helicopter = 0,
        Plane = 1,
        AirSelf = 2
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

    public readonly struct ConfiguredSpawnableEntry
    {
        public readonly string DisplayName;
        public readonly string Description;
        public readonly GameObject Prefab;
        public readonly bool CanRequest;
        public readonly int Price;

        public ConfiguredSpawnableEntry(string displayName, string description, GameObject prefab, bool canRequest, int price)
        {
            DisplayName = displayName;
            Description = description;
            Prefab = prefab;
            CanRequest = canRequest;
            Price = price;
        }
    }

    public readonly struct ConfiguredUnitEntry
    {
        public readonly string DisplayName;
        public readonly string Description;
        public readonly GameObject Prefab;
        public readonly bool IsVehicle;
        public readonly bool CanRequest;
        public readonly int Price;

        public ConfiguredUnitEntry(string displayName, string description, GameObject prefab, bool isVehicle, bool canRequest, int price)
        {
            DisplayName = displayName;
            Description = description;
            Prefab = prefab;
            IsVehicle = isVehicle;
            CanRequest = canRequest;
            Price = price;
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

    public enum CampRequestFailure
    {
        None = 0,
        NotEnoughMoney = 1,
        MissingProducerBuilding = 2,
        InvalidSelection = 3
    }

    public enum FactionUnitProductionResultCode
    {
        Queued = 0,
        MissingUnitConfig = 1,
        MissingProducerBuilding = 2,
        ProducerUnavailable = 3
    }

    public readonly struct FactionUnitProductionResult
    {
        public readonly FactionUnitProductionResultCode Code;
        public readonly string ProducerDisplayName;
        public readonly string UnitDisplayName;
        public readonly int Cost;
        public readonly int QueueCount;
        public readonly int ProducedCount;

        public FactionUnitProductionResult(
            FactionUnitProductionResultCode code,
            string producerDisplayName,
            string unitDisplayName,
            int cost,
            int queueCount,
            int producedCount)
        {
            Code = code;
            ProducerDisplayName = producerDisplayName;
            UnitDisplayName = unitDisplayName;
            Cost = cost;
            QueueCount = queueCount;
            ProducedCount = producedCount;
        }
    }

    private static readonly bool EnableBuildingPlacementDiagnostics = false;
    private static readonly bool EnableBuildingDestroyDiagnostics = false;
    private const double FreezeLogThresholdSeconds = 0.05d;
    private static readonly bool VerboseResourceHaulerLogs = false;
    private const float DestroyedBuildingLifetimeSeconds = 5f;
    private int _armedProductionFrame = -1;

    private sealed class BuildingDefinition
    {
        public sealed class ProductionSlotDefinition
        {
            public GameObject SpawnUnitPrefab;
        }

        public string DisplayName;
        public string Description;
        public int MaxHealth;
        public List<ProductionSlotDefinition> ProductionSlots;
        public GameObject SpawnUnitPrefab;
        public GameObject SecondarySpawnUnitPrefab;
        public GameObject TertiarySpawnUnitPrefab;
        public GameObject QuaternarySpawnUnitPrefab;
        public GameObject Prefab;
        public Vector2Int FootprintCells;
        public BuildingRole Role;
        public bool IsWall;
        public float OilBarrelsPerDay;
        public int OilStorageCapacity;
        public float FuelBarrelsPerDay;
        public int FuelStorageCapacity;
        public int RefugeeCapacity;
        public int RefugeeUpkeepPerCitizenPerDay;
        public ThreatDetectionKind ThreatDetectionKind;
        public int ThreatDetectionRadiusCells;
        public Bounds LocalBounds;
        public bool HasLocalBounds;
        public GameObject VisualTemplate;
        public List<Mesh> GeneratedMeshes;
        public Vector3[] ProductionSpawnLocalPositions;
        public bool HasRunway;
        public Vector3 RunwayLocalPosition;
        public Quaternion RunwayLocalRotation;
        public Vector3 RunwayHalfExtents;
    }

    private sealed class RuntimeBuildingData : BuildingCombatSystem.IRuntimeBuilding, FactionResourceSystem.IResourceBuilding
    {
        public sealed class PendingDropVisual
        {
            public PendingProduction Production;
            public GameObject Visual;
            public LineRenderer Rope;
            public float StartedAt;
            public float Duration;
            public Vector3 StartPosition;
            public Vector3 EndPosition;
            public int2 FinalGoalCell;
        }

        public sealed class ActiveProductionTransport
        {
            public int LaneIndex;
            public GameObject Prefab;
            public GameObject Instance;
            public Transform Transform;
            public Transform DoorTransform;
            public float DoorOpenLocalEulerX;
            public Vector3 EntryPosition;
            public Vector3 TouchdownPosition;
            public Vector3 HoverPosition;
            public Vector3 ExitPosition;
            public Quaternion HoverRotation;
            public Quaternion EntryRotation;
            public Quaternion ExitRotation;
            public float ArrivalSeconds;
            public float HoldForNextReadySeconds;
            public float PhaseStartedAt;
            public byte Phase;
            public float HoverEnteredAt;
            public float NextDropReadyAt;
            public ProductionTransportMode Mode;
            public PendingDropVisual ActiveDrop;
        }

        public sealed class PendingProduction
        {
            public int ProductionIndex;
            public GameObject Prefab;
            public float StartedAt;
            public float ReadyAt;
            public int ReservedProductionSlotIndex;
            public GameObject TransportPrefab;
            public float TransportArrivalSeconds;
            public float TransportHoldForNextReadySeconds;
            public int TransportMaxConcurrent;
            public ProductionTransportMode TransportMode;
            public bool TransportRequiresAirportRunway;
        }

        public int Id { get; set; }
        public BuildingDefinition Definition;
        public GameObject Instance;
        public Vector2Int OriginCell;
        public Entity CombatEntity { get; set; }
        public Entity BlockerEntity { get; set; }
        public Transform FactionMarker;
        public Renderer[] FactionMarkerRenderers;
        public Transform SelectionMarker;
        public Transform DoorZ;
        public float DoorClosedLocalEulerZ;
        public float DoorOpenLocalEulerZ;
        public float DoorOpen01;
        public Transform DestroyedVisual;
        public Transform[] AliveVisualRoots;
        public BuildingVisualSystem.AnimatedPart[] AnimatedParts;
        public Vector3[] ProductionSpawnLocalPositions;
        public Entity[] ProducedUnitSlots;
        public List<Entity> ProducedUnits;
        public Dictionary<Entity, GameObject> ProducedUnitPrefabs;
        public List<PendingProduction> PendingProductions;
        public ActiveProductionTransport ActiveTransport;
        public bool IsDestroyed { get; set; }
        public bool IsCityGenerated;
        public bool HasOwnerFaction { get; set; }
        public byte OwnerFactionId { get; set; }
        public float DestroyedCleanupAt { get; set; }
        public float StoredOilBarrels { get; set; }
        public float StoredFuelBarrels { get; set; }
        public int OilStorageCapacity => Definition != null ? Definition.OilStorageCapacity : 0;
        public int FuelStorageCapacity => Definition != null ? Definition.FuelStorageCapacity : 0;
        public float OilBarrelsPerDay => Definition != null ? Definition.OilBarrelsPerDay : 0f;
        public float FuelBarrelsPerDay => Definition != null ? Definition.FuelBarrelsPerDay : 0f;
    }

    private readonly struct RuntimeBaseBreach
    {
        public readonly byte OwnerFactionId;
        public readonly RectInt Rect;

        public RuntimeBaseBreach(byte ownerFactionId, RectInt rect)
        {
            OwnerFactionId = ownerFactionId;
            Rect = rect;
        }
    }

    private sealed class PlacementState
    {
        public sealed class WallRun
        {
            public List<Vector2Int> Origins;
            public bool Vertical;
        }

        public BuildingDefinition Definition;
        public GameObject PreviewInstance;
        public Vector2Int OriginCell;
        public Vector2Int CommittedOriginCell;
        public Vector2Int DragStartOriginCell;
        public Vector2Int DragCurrentOriginCell;
        public DragFirstAxis DragFirstAxis;
        public bool AutoRotateVertical;
        public List<WallRun> CommittedWallRuns;
        public bool HideCurrentWallPreview;
        public bool IsValid;
        public float LastPointerMovedAt;
        public Vector2 LastPointerScreenPosition;
    }

    private sealed class CachedRuntimeBuildingMetadata
    {
        public BuildingDefinitionAuthoring Authoring;
        public bool HasVisualFootprint;
        public Vector2Int VisualFootprint;
        public Bounds LocalBounds;
        public bool HasLocalBounds;
        public bool HasRunway;
        public Vector3 RunwayLocalPosition;
        public Quaternion RunwayLocalRotation;
        public Vector3 RunwayHalfExtents;
        public Vector3[] ProductionSpawnLocalPositions;
    }

    private sealed class RecentSpawnReservation
    {
        public int2 Cell;
        public int2 Size;
        public float ExpiresAt;
    }


    [SerializeField] private BuildingPlacementSystemConfig config;
    [SerializeField, HideInInspector] private Camera worldCamera;
    [SerializeField, HideInInspector] private List<GameObject> spawnables = new();
    [SerializeField, HideInInspector] private UnitPrefabRegistryAuthoringConfig unitPrefabRegistryConfig;
    [SerializeField, HideInInspector] private List<GameObject> unitSpawnPrefabs = new();
    [SerializeField, HideInInspector] private float buildPlaneY = 0f;
    [SerializeField, HideInInspector] private float placementOutlineHeight = 0.15f;
    [SerializeField, HideInInspector] private Color placementValidColor = new(0.15f, 0.85f, 0.2f, 1f);
    [SerializeField, HideInInspector] private Color placementInvalidColor = new(0.9f, 0.2f, 0.2f, 1f);

    private readonly RuntimeBuildingSystem<RuntimeBuildingData> _runtimeBuildingSystem = new();
    private readonly BuildingVisualSystem _buildingVisualSystem = new();
    private readonly BuildingCombatSystem _buildingCombatSystem = new();
    private readonly FactionResourceSystem _factionResourceSystem = new();
    private IReadOnlyDictionary<int, RuntimeBuildingData> _runtimeBuildings => _runtimeBuildingSystem.Buildings;
    private readonly Dictionary<GameObject, CachedRuntimeBuildingMetadata> _runtimeBuildingMetadataCache = new();
    private readonly Dictionary<string, GameObject> _spawnablesByKey = new();
    private readonly Dictionary<string, GameObject> _unitSpawnPrefabsByKey = new();
    private readonly List<BuildingDefinition> _configuredSpawnableDefinitions = new();
    private readonly Dictionary<GameObject, BuildingDefinition> _configuredDefinitionsByPrefab = new();
    private readonly List<RectInt> _deferredRedirectFootprints = new();
    private int[] _placementInvalidPrefix;
    private int _resourceDollars;
    private Transform _buildingRoot;
    private GameObject _placementOutline;
    private MeshRenderer _placementOutlineRenderer;
    private BuildingDefinition _soldierBaseDefinition;
    private BuildingDefinition _soldierTentDefinition;
    private BuildingDefinition _factoryDefinition;
    private PlacementState _activePlacement;
    private int _activePlacementCost;
    private RoadBuildSystem _roadBuildController;
    private MainMenuPlayUI _mainMenuPlayUi;
    private RTSSelectionSystem _selectionSystem;
    private RuntimeGridBlockerSystem _runtimeGridBlockerSystem;
    private RuntimeCitySpawnerSystem _runtimeCitySpawnerSystem;
    private CitizenPopulationSystem _citizenPopulationSystem;
    private FactionVisualSettings _factionVisualSettings;
    private DayNightSystem _dayNightSystem;
    private World _queryWorld;
    private EntityQuery _gridDataQuery;
    private EntityQuery _redirectUnitsQuery;
    private EntityQuery _unitPrefabRegistryQuery;
    private EntityQuery _spawnPrefabCandidatesQuery;
    private EntityQuery _selectedUnitsQuery;
    private EntityQuery _haulerUnitsQuery;
    private EntityQuery _livePlayerUnitsQuery;
    private EntityQuery _liveUnitFootprintQuery;
    private EntityQuery _liveFactionUnitsQuery;
    private uint _buildingSpawnRandomState = 0x12345678u;
    private bool _isDraggingPlacement;
    private bool _ignorePointerUpdatesUntilRelease;
    private MaterialPropertyBlock _markerPropertyBlock;
    private int _deferRuntimeBuildingSideEffectsDepth;
    private bool _pendingMarkerRefresh;
    private RuntimeBuildingData _lastCampProductionFocusBuilding;
    private GameObject _lastCampProductionFocusPrefab;
    private bool _hasPlacementInvalidPrefix;
    private int _placementInvalidPrefixWidth;
    private int _placementInvalidPrefixHeight;
    private Transform _runtimeRoot;
    private readonly List<RecentSpawnReservation> _recentSpawnReservations = new();
    private readonly List<RuntimeBaseBreach> _openBaseBreaches = new();
    private bool _preserveBuildingSelectionOnNextExitBuildMode;
    private const float ProductionTransportLaneSpacing = 12f;
    private const float BarrierDoorOpenCloseSpeed = 2f;
    private const int BarrierDoorDetectPaddingCells = 8;
    private const float OilBarrelsPerFuelBarrel = 2f;

    private int? ActiveBuildingId => _runtimeBuildingSystem.CurrentActiveBuildingId;

    public bool HasPendingBuildingPlacement => _activePlacement != null;
    public bool CanConfirmBuildingPlacement => _activePlacement != null && _activePlacement.IsValid;
    public bool HasSelectedBuilding => _runtimeBuildingSystem.HasSelectedBuilding();
    public bool HasActiveBuilding => ActiveBuildingId.HasValue;
    public int? CurrentActiveBuildingId => ActiveBuildingId;
    public GameObject RoadPreviewPrefab => config != null ? config.RoadPreviewPrefab : null;
    public float BuildButtonPreviewDistanceMultiplier => config != null ? config.BuildButtonPreviewDistanceMultiplier : 1f;
    public float UnitCommandButtonPreviewDistanceMultiplier => config != null ? config.UnitCommandButtonPreviewDistanceMultiplier : 1f;
    public int ConfiguredSpawnableCount => _configuredSpawnableDefinitions.Count;
    public int ConfiguredUnitCount => unitSpawnPrefabs != null ? unitSpawnPrefabs.Count : 0;

    public bool HasVisibleSelectableBuilding(Camera camera = null)
    {
        Camera targetCamera = camera != null ? camera : worldCamera;
        if (targetCamera == null)
            return false;

        Rect screenRect = new(0f, 0f, Screen.width, Screen.height);
        foreach (KeyValuePair<int, RuntimeBuildingData> pair in _runtimeBuildings)
        {
            RuntimeBuildingData building = pair.Value;
            if (building == null || building.IsDestroyed || building.Instance == null || !building.Instance.activeInHierarchy)
                continue;

            Vector3 screen = targetCamera.WorldToScreenPoint(ResolveBuildingFocusWorldPosition(building));
            if (screen.z > 0f && screenRect.Contains(new Vector2(screen.x, screen.y)))
                return true;
        }

        return false;
    }

    public void BeginDeferredRuntimeBuildingSideEffects()
    {
        _deferRuntimeBuildingSideEffectsDepth++;
        if (_deferRuntimeBuildingSideEffectsDepth == 1)
            RebuildPlacementInvalidPrefix();
    }

    public void EndDeferredRuntimeBuildingSideEffects()
    {
        if (_deferRuntimeBuildingSideEffectsDepth <= 0)
            return;

        _deferRuntimeBuildingSideEffectsDepth--;
        if (_deferRuntimeBuildingSideEffectsDepth > 0)
            return;

        if (_deferredRedirectFootprints.Count > 0)
        {
            RedirectUnitsAroundPlacedBuildings(_deferredRedirectFootprints);
            _deferredRedirectFootprints.Clear();
        }

        if (_pendingMarkerRefresh)
        {
            RefreshBuildingMarkerVisibility();
            _pendingMarkerRefresh = false;
        }

        _hasPlacementInvalidPrefix = false;
    }

    private void RebuildPlacementInvalidPrefix()
    {
        _hasPlacementInvalidPrefix = false;
        if (!TryGetGridData(out _, out GridConfig grid, out DynamicBuffer<GridRoad> roads, out DynamicBlockerData blockerData))
            return;

        bool[] roadFootprintMask = null;
        if (_roadBuildController != null)
        {
            roadFootprintMask = new bool[grid.Width * grid.Height];
            _roadBuildController.FillRoadFootprintMask(grid, roadFootprintMask);
        }

        BuildingPlacementValidationSystem.RebuildInvalidPrefix(
            grid,
            roads,
            blockerData,
            roadFootprintMask,
            IsRuntimeBlockerCell,
            ref _placementInvalidPrefix,
            out _placementInvalidPrefixWidth,
            out _placementInvalidPrefixHeight,
            out _hasPlacementInvalidPrefix);
    }

    private bool HasCachedInvalidCellInFootprint(Vector2Int originCell, Vector2Int footprintCells)
    {
        if (!_hasPlacementInvalidPrefix)
            return false;

        return BuildingPlacementValidationSystem.HasCachedInvalidCellInFootprint(
            _placementInvalidPrefix,
            _placementInvalidPrefixWidth,
            _placementInvalidPrefixHeight,
            originCell,
            footprintCells);
    }

    private bool IsRuntimeBlockerCell(int x, int y, int width, int height)
    {
        return _runtimeGridBlockerSystem != null &&
            _runtimeGridBlockerSystem.IsRuntimeBlockerCell(x, y, width, height);
    }
    public GameObject SelectedBuildingPrimarySpawnUnitPrefab => TryGetSelectedBuildingProductionPrefab(CreateSlot.Primary);
    public GameObject SelectedBuildingSecondarySpawnUnitPrefab => TryGetSelectedBuildingProductionPrefab(CreateSlot.Secondary);
    public GameObject SelectedBuildingTertiarySpawnUnitPrefab => TryGetSelectedBuildingProductionPrefab(CreateSlot.Tertiary);
    public GameObject SelectedBuildingQuaternarySpawnUnitPrefab => TryGetSelectedBuildingProductionPrefab(CreateSlot.Quaternary);

    public void GetSelectedBuildingProductionPrefabs(List<GameObject> prefabs)
    {
        prefabs?.Clear();
        int? buildingId = ActiveBuildingId;
        if (!buildingId.HasValue || prefabs == null)
            return;

        if (!_runtimeBuildings.TryGetValue(buildingId.Value, out RuntimeBuildingData building) || building?.Definition == null)
            return;

        int count = GetProductionCount(building.Definition);
        for (int i = 0; i < count; i++)
        {
            GameObject prefab = GetProductionPrefab(building.Definition, i);
            if (prefab != null)
                prefabs.Add(prefab);
        }
    }

    public void GetSelectedBuildingProducedUnits(List<Entity> units)
    {
        units?.Clear();
        int? buildingId = ActiveBuildingId;
        if (!buildingId.HasValue || units == null)
            return;

        if (!_runtimeBuildings.TryGetValue(buildingId.Value, out RuntimeBuildingData building) ||
            !TryGetEntityManager(out EntityManager em))
            return;

        if (building.ProducedUnits == null)
            building.ProducedUnits = new List<Entity>();

        for (int i = building.ProducedUnits.Count - 1; i >= 0; i--)
        {
            Entity unit = building.ProducedUnits[i];
            if (unit == Entity.Null || !em.Exists(unit))
            {
                building.ProducedUnits.RemoveAt(i);
                continue;
            }

            if (em.HasComponent<UnitHealth>(unit) && em.GetComponentData<UnitHealth>(unit).Current <= 0)
            {
                building.ProducedUnits.RemoveAt(i);
                continue;
            }
        }

        for (int i = 0; i < building.ProducedUnits.Count; i++)
        {
            Entity unit = building.ProducedUnits[i];
            units.Add(unit);
        }
    }

    public void GetSelectedBuildingProducedUnitEntries(List<ProducedUnitUiEntry> entries)
    {
        entries?.Clear();
        int? buildingId = ActiveBuildingId;
        if (!buildingId.HasValue || entries == null)
            return;

        if (!_runtimeBuildings.TryGetValue(buildingId.Value, out RuntimeBuildingData building))
            return;

        if (!TryGetEntityManager(out EntityManager em))
            return;

        if (building.ProducedUnits == null)
            building.ProducedUnits = new List<Entity>();
        if (building.ProducedUnitPrefabs == null)
            building.ProducedUnitPrefabs = new Dictionary<Entity, GameObject>();

        for (int i = building.ProducedUnits.Count - 1; i >= 0; i--)
        {
            Entity unit = building.ProducedUnits[i];
            if (unit == Entity.Null || !em.Exists(unit))
            {
                building.ProducedUnitPrefabs.Remove(unit);
                building.ProducedUnits.RemoveAt(i);
                continue;
            }

            if (em.HasComponent<UnitHealth>(unit) && em.GetComponentData<UnitHealth>(unit).Current <= 0)
            {
                building.ProducedUnitPrefabs.Remove(unit);
                building.ProducedUnits.RemoveAt(i);
                continue;
            }

            building.ProducedUnitPrefabs.TryGetValue(unit, out GameObject prefab);
            entries.Add(new ProducedUnitUiEntry(unit, prefab, true, 1f));
        }

        if (building.PendingProductions != null)
        {
            float now = Time.time;
            for (int i = 0; i < building.PendingProductions.Count; i++)
            {
                RuntimeBuildingData.PendingProduction pending = building.PendingProductions[i];
                if (pending == null || pending.Prefab == null)
                    continue;

                float duration = Mathf.Max(0.01f, pending.ReadyAt - pending.StartedAt);
                float progress01 = Mathf.Clamp01((now - pending.StartedAt) / duration);
                if (pending.TransportPrefab != null)
                    progress01 = Mathf.Min(progress01, 0.97f);
                entries.Add(new ProducedUnitUiEntry(Entity.Null, pending.Prefab, false, progress01));
            }
        }
    }

    public bool TryGetSelectedBuildingCapacityInfo(out int current, out int max, out float progress01)
    {
        current = 0;
        max = 0;
        progress01 = 0f;

        int? buildingId = ActiveBuildingId;
        if (!buildingId.HasValue)
            return false;

        if (!_runtimeBuildings.TryGetValue(buildingId.Value, out RuntimeBuildingData building) || building?.Definition == null)
            return false;

        return _factionResourceSystem.TryGetPrimaryCapacityInfo(building, OilBarrelsPerFuelBarrel, out current, out max, out progress01);
    }

    public void GetFriendlyPendingProductionUiEntries(List<PendingProductionUiEntry> entries)
    {
        if (entries == null)
            return;

        entries.Clear();
        float now = Time.time;

        foreach (KeyValuePair<int, RuntimeBuildingData> pair in _runtimeBuildings)
        {
            RuntimeBuildingData building = pair.Value;
            if (building == null || building.IsDestroyed || building.PendingProductions == null || building.PendingProductions.Count == 0)
                continue;
            if (building.IsCityGenerated)
                continue;
            if (building.HasOwnerFaction && building.OwnerFactionId != 0)
                continue;

            for (int i = 0; i < building.PendingProductions.Count; i++)
            {
                RuntimeBuildingData.PendingProduction pending = building.PendingProductions[i];
                if (pending == null || pending.Prefab == null)
                    continue;

                float duration = Mathf.Max(0.01f, pending.ReadyAt - pending.StartedAt);
                float remaining = Mathf.Max(0f, pending.ReadyAt - now);
                float progress = Mathf.Clamp01((now - pending.StartedAt) / duration);
                entries.Add(new PendingProductionUiEntry(pair.Key, pending.Prefab, remaining, duration, progress, pending.StartedAt, pending.ReadyAt));
            }
        }
    }

    public bool TryGetSelectedBuildingCapacity2Info(out int current, out int max, out float progress01)
    {
        current = 0;
        max = 0;
        progress01 = 0f;

        int? buildingId = ActiveBuildingId;
        if (!buildingId.HasValue)
            return false;

        if (!_runtimeBuildings.TryGetValue(buildingId.Value, out RuntimeBuildingData building) || building?.Definition == null)
            return false;

        return _factionResourceSystem.TryGetFuelCapacityInfo(building, out current, out max, out progress01);
    }

    public void GetResourceTotals(out int dollars, out int oilBarrels, out int fuelBarrels)
    {
        dollars = _resourceDollars;
        _factionResourceSystem.GetResourceTotals(_runtimeBuildings, out oilBarrels, out fuelBarrels);
    }

    public int CurrentDollars => _resourceDollars;

    public bool TryGetFactionResourceEconomy(byte factionId, out FactionResourceEconomySnapshot snapshot)
    {
        bool hasEconomy = _factionResourceSystem.TryGetFactionResourceEconomy(
            _runtimeBuildings,
            factionId,
            out FactionResourceSystem.ResourceEconomySnapshot resourceSnapshot);
        snapshot = new FactionResourceEconomySnapshot(
            resourceSnapshot.StoredOilBarrels,
            resourceSnapshot.StoredFuelBarrels,
            resourceSnapshot.OilBarrelsPerDay,
            resourceSnapshot.FuelBarrelsPerDay,
            resourceSnapshot.ResourceBuildingCount);
        return hasEconomy;
    }

    public void SellFactionResources(byte factionId, float requestedOilBarrels, float requestedFuelBarrels, out float soldOilBarrels, out float soldFuelBarrels)
    {
        soldOilBarrels = _factionResourceSystem.DrainFactionResource(
            _runtimeBuildings,
            factionId,
            Mathf.Max(0f, requestedOilBarrels),
            FactionResourceSystem.ResourceKind.Oil);
        soldFuelBarrels = _factionResourceSystem.DrainFactionResource(
            _runtimeBuildings,
            factionId,
            Mathf.Max(0f, requestedFuelBarrels),
            FactionResourceSystem.ResourceKind.Fuel);
    }

    public int CountRuntimeBuildingsForFaction(byte factionId)
    {
        int count = 0;
        foreach (KeyValuePair<int, RuntimeBuildingData> pair in _runtimeBuildings)
        {
            RuntimeBuildingData building = pair.Value;
            if (building == null || building.IsDestroyed || !building.HasOwnerFaction || building.OwnerFactionId != factionId)
                continue;

            count++;
        }

        return count;
    }

    public int CountRuntimeBuildingsForFaction(byte factionId, string buildingId)
    {
        string normalized = NormalizeSpawnableKey(buildingId);
        if (string.IsNullOrEmpty(normalized))
            return CountRuntimeBuildingsForFaction(factionId);

        int count = 0;
        foreach (KeyValuePair<int, RuntimeBuildingData> pair in _runtimeBuildings)
        {
            RuntimeBuildingData building = pair.Value;
            if (building == null || building.IsDestroyed || !building.HasOwnerFaction || building.OwnerFactionId != factionId)
                continue;
            if (!RuntimeBuildingMatchesId(building, normalized))
                continue;

            count++;
        }

        return count;
    }

    public int CountRuntimeProducedUnitsForFaction(byte factionId, string unitId)
    {
        string normalized = NormalizeSpawnableKey(unitId);
        int count = 0;
        if (!TryGetEntityManager(out EntityManager em))
            return 0;

        foreach (KeyValuePair<int, RuntimeBuildingData> pair in _runtimeBuildings)
        {
            RuntimeBuildingData building = pair.Value;
            if (building == null || building.IsDestroyed || !building.HasOwnerFaction || building.OwnerFactionId != factionId)
                continue;
            if (building.ProducedUnits == null)
                continue;

            for (int i = building.ProducedUnits.Count - 1; i >= 0; i--)
            {
                Entity unit = building.ProducedUnits[i];
                bool alive = unit != Entity.Null && em.Exists(unit);
                if (alive && em.HasComponent<UnitHealth>(unit))
                    alive = em.GetComponentData<UnitHealth>(unit).Current > 0;
                if (!alive)
                {
                    building.ProducedUnits.RemoveAt(i);
                    continue;
                }

                if (em.HasComponent<Faction>(unit) && em.GetComponentData<Faction>(unit).Id != factionId)
                    continue;
                if (!RuntimeProducedUnitMatchesId(building, unit, normalized))
                    continue;

                count++;
            }
        }

        return count;
    }

    public int CountPendingProductionsForFaction(byte factionId, string unitId)
    {
        string normalized = NormalizeSpawnableKey(unitId);
        int count = 0;
        foreach (KeyValuePair<int, RuntimeBuildingData> pair in _runtimeBuildings)
        {
            RuntimeBuildingData building = pair.Value;
            if (building == null || building.IsDestroyed || !building.HasOwnerFaction || building.OwnerFactionId != factionId)
                continue;
            if (building.PendingProductions == null)
                continue;

            for (int i = 0; i < building.PendingProductions.Count; i++)
            {
                RuntimeBuildingData.PendingProduction pending = building.PendingProductions[i];
                if (pending == null)
                    continue;
                if (!UnitPrefabMatchesId(pending.Prefab, normalized))
                    continue;

                count++;
            }
        }

        return count;
    }

    public bool TryGetConfiguredUnit(string unitId, out ConfiguredUnitEntry entry)
    {
        string normalized = NormalizeSpawnableKey(unitId);
        if (string.IsNullOrEmpty(normalized))
        {
            entry = default;
            return false;
        }

        for (int i = 0; i < ConfiguredUnitCount; i++)
        {
            if (!TryGetConfiguredUnit(i, out ConfiguredUnitEntry candidate))
                continue;
            if (!UnitPrefabMatchesId(candidate.Prefab, normalized))
                continue;

            entry = candidate;
            return true;
        }

        entry = default;
        return false;
    }

    public bool TryQueueFactionUnitProduction(byte factionId, string unitId, out FactionUnitProductionResult result)
    {
        result = default;
        if (!TryGetConfiguredUnit(unitId, out ConfiguredUnitEntry unit) || unit.Prefab == null || !unit.CanRequest)
        {
            result = new FactionUnitProductionResult(FactionUnitProductionResultCode.MissingUnitConfig, string.Empty, unitId, 0, 0, 0);
            return false;
        }

        if (!TryFindFirstFactionProducerBuilding(factionId, unit.Prefab, out int producerBuildingId, out int productionIndex, out string producerDisplayName))
        {
            result = new FactionUnitProductionResult(FactionUnitProductionResultCode.MissingProducerBuilding, string.Empty, unit.DisplayName, unit.Price, 0, CountRuntimeProducedUnitsForFaction(factionId, unitId));
            return false;
        }

        if (!_runtimeBuildings.TryGetValue(producerBuildingId, out RuntimeBuildingData producerBuilding) || producerBuilding == null)
        {
            result = new FactionUnitProductionResult(FactionUnitProductionResultCode.ProducerUnavailable, producerDisplayName, unit.DisplayName, unit.Price, 0, CountRuntimeProducedUnitsForFaction(factionId, unitId));
            return false;
        }

        if (!TryQueuePlayerUnitFromBuilding(producerBuilding, productionIndex, unit.Prefab))
        {
            result = new FactionUnitProductionResult(FactionUnitProductionResultCode.ProducerUnavailable, producerDisplayName, unit.DisplayName, unit.Price, CountPendingProductionsForFaction(factionId, unitId), CountRuntimeProducedUnitsForFaction(factionId, unitId));
            return false;
        }

        result = new FactionUnitProductionResult(
            FactionUnitProductionResultCode.Queued,
            producerDisplayName,
            unit.DisplayName,
            unit.Price,
            CountPendingProductionsForFaction(factionId, unitId),
            CountRuntimeProducedUnitsForFaction(factionId, unitId));
        return true;
    }

    public void GetRuntimeHouseBuildingIds(List<int> results)
    {
        if (results == null)
            return;

        results.Clear();
        foreach (KeyValuePair<int, RuntimeBuildingData> pair in _runtimeBuildings)
        {
            RuntimeBuildingData building = pair.Value;
            if (building == null || building.IsDestroyed || building.Instance == null)
                continue;
            if (!IsHouseBuilding(building))
                continue;

            results.Add(pair.Key);
        }
    }

    public void GetRuntimeBuildingIdsByRole(BuildingRole role, List<int> results)
    {
        if (results == null)
            return;

        results.Clear();
        foreach (KeyValuePair<int, RuntimeBuildingData> pair in _runtimeBuildings)
        {
            RuntimeBuildingData building = pair.Value;
            if (building?.Definition == null || building.IsDestroyed)
                continue;

            if (building.Definition.Role != role)
                continue;

            results.Add(pair.Key);
        }
    }

    public bool TryGetRuntimeBuildingFocusWorldPosition(int buildingId, out Vector3 worldPosition)
    {
        worldPosition = Vector3.zero;
        if (!_runtimeBuildings.TryGetValue(buildingId, out RuntimeBuildingData building) || building == null)
            return false;

        worldPosition = ResolveBuildingFocusWorldPosition(building);
        return true;
    }

    public bool TryGetRuntimeBuildingDestroyedState(int buildingId, out bool isDestroyed)
    {
        isDestroyed = false;
        if (!_runtimeBuildings.TryGetValue(buildingId, out RuntimeBuildingData building) || building == null)
            return false;

        isDestroyed = building.IsDestroyed;
        return true;
    }

    public bool TryGetRuntimeBuildingRefugeeSettings(int buildingId, out int refugeeCapacity, out int upkeepPerCitizenPerDay)
    {
        refugeeCapacity = 0;
        upkeepPerCitizenPerDay = 0;
        if (!_runtimeBuildings.TryGetValue(buildingId, out RuntimeBuildingData building) || building?.Definition == null)
            return false;

        refugeeCapacity = Mathf.Max(0, building.Definition.RefugeeCapacity);
        upkeepPerCitizenPerDay = Mathf.Max(0, building.Definition.RefugeeUpkeepPerCitizenPerDay);
        return true;
    }

    public bool IsRuntimeBuildingCityGenerated(int buildingId)
    {
        return _runtimeBuildings.TryGetValue(buildingId, out RuntimeBuildingData building) &&
               building != null &&
               building.IsCityGenerated;
    }

    public bool IsRuntimeBuildingWall(int buildingId)
    {
        return _runtimeBuildings.TryGetValue(buildingId, out RuntimeBuildingData building) &&
               building?.Definition != null &&
               building.Definition.IsWall;
    }

    public bool TryGetRuntimeBuildingOwnerFaction(int buildingId, out byte factionId)
    {
        factionId = 0;
        if (!_runtimeBuildings.TryGetValue(buildingId, out RuntimeBuildingData building) || building == null || !building.HasOwnerFaction)
            return false;

        factionId = building.OwnerFactionId;
        return true;
    }

    public bool TryGetRuntimeBuildingCombatInfo(Entity combatEntity, out bool isGate, out bool isWall, out byte ownerFactionId)
    {
        isGate = false;
        isWall = false;
        ownerFactionId = 0;
        if (!TryFindRuntimeBuildingByCombatEntity(combatEntity, out RuntimeBuildingData building) || building?.Definition == null)
            return false;

        isGate = IsWallGateDefinition(building.Definition);
        isWall = building.Definition.IsWall;
        ownerFactionId = building.HasOwnerFaction ? building.OwnerFactionId : (byte)0;
        return true;
    }

    public bool TryResolveBaseBreachTarget(
        byte attackerFactionId,
        Entity finalTarget,
        int2 finalTargetCell,
        int2 attackerCell,
        out Entity breachTarget,
        out int2 breachCell,
        out float3 breachPosition,
        out string reason)
    {
        breachTarget = Entity.Null;
        breachCell = default;
        breachPosition = default;
        reason = string.Empty;

        if (TryFindRuntimeBuildingByCombatEntity(finalTarget, out RuntimeBuildingData finalBuilding) &&
            finalBuilding?.Definition != null &&
            (finalBuilding.Definition.IsWall || IsWallGateDefinition(finalBuilding.Definition)))
            return false;

        if (!TryFindEnemyWallPerimeterContainingCell(attackerFactionId, finalTargetCell, out byte breachedFactionId, out RectInt breachedPerimeter))
            return false;

        if (HasOpenBaseBreach(breachedFactionId, breachedPerimeter))
            return false;

        if (!TryFindBreachBuilding(breachedFactionId, attackerCell, preferGate: true, out RuntimeBuildingData breachBuilding, out reason) &&
            !TryFindBreachBuilding(breachedFactionId, attackerCell, preferGate: false, out breachBuilding, out reason))
        {
            return false;
        }

        if (breachBuilding == null ||
            breachBuilding.CombatEntity == Entity.Null ||
            breachBuilding.CombatEntity == finalTarget ||
            !TryGetEntityManager(out EntityManager em) ||
            !em.Exists(breachBuilding.CombatEntity) ||
            !em.HasComponent<UnitHealth>(breachBuilding.CombatEntity) ||
            em.GetComponentData<UnitHealth>(breachBuilding.CombatEntity).Current <= 0 ||
            !em.HasComponent<LocalTransform>(breachBuilding.CombatEntity))
        {
            return false;
        }

        breachTarget = breachBuilding.CombatEntity;
        int2 centerCell = new(
            breachBuilding.OriginCell.x + Mathf.Max(1, breachBuilding.Definition.FootprintCells.x) / 2,
            breachBuilding.OriginCell.y + Mathf.Max(1, breachBuilding.Definition.FootprintCells.y) / 2);
        breachCell = centerCell;
        if (TryGetGridData(out Entity gridEntity, out GridConfig grid, out _, out DynamicBlockerData blockerData) &&
            em.HasComponent<DynamicOccupancyData>(gridEntity))
        {
            NativeArray<GridWalkable> walkable = em.GetBuffer<GridWalkable>(gridEntity).AsNativeArray();
            NativeBitArray occupied = em.GetComponentData<DynamicOccupancyData>(gridEntity).Occupied;
            if (TryFindBreachApproachCell(
                    grid,
                    walkable,
                    blockerData.Blocked,
                    blockerData.FriendlyPassFactionIds,
                    occupied,
                    breachBuilding.OriginCell,
                    breachBuilding.Definition.FootprintCells,
                    breachedPerimeter,
                    new int2(1, 1),
                    attackerCell,
                    attackerFactionId,
                    out int2 outsideApproachCell))
            {
                breachCell = outsideApproachCell;
            }
            else if (TryFindBuildingApproachCell(
                    grid,
                    walkable,
                    blockerData.Blocked,
                    occupied,
                    breachBuilding.OriginCell,
                    breachBuilding.Definition.FootprintCells,
                    new int2(1, 1),
                    attackerCell,
                    out int2 approachCell))
            {
                breachCell = approachCell;
            }
        }

        breachPosition = em.GetComponentData<LocalTransform>(breachBuilding.CombatEntity).Position;
        return true;
    }

    public bool TryGetRuntimeBuildingApproachCell(int buildingId, int2 unitFootprint, int2 referenceCell, out int2 goal)
    {
        goal = default;
        if (!_runtimeBuildings.TryGetValue(buildingId, out RuntimeBuildingData building) || building == null || building.IsDestroyed)
            return false;
        if (!TryGetEntityManager(out EntityManager em))
            return false;
        if (!TryGetGridData(out Entity gridEntity, out GridConfig grid, out _, out DynamicBlockerData blockerData))
            return false;

        var walkable = em.GetBuffer<GridWalkable>(gridEntity).AsNativeArray();
        var occupied = em.GetComponentData<DynamicOccupancyData>(gridEntity).Occupied;
        return TryFindBuildingApproachCell(
            grid,
            walkable,
            blockerData.Blocked,
            occupied,
            building.OriginCell,
            building.Definition.FootprintCells,
            unitFootprint,
            referenceCell,
            out goal);
    }

    public bool IsRuntimeBuildingApproachCell(int buildingId, int2 currentCell, int2 unitFootprint)
    {
        if (!_runtimeBuildings.TryGetValue(buildingId, out RuntimeBuildingData building) || building == null || building.IsDestroyed)
            return false;
        if (!TryGetGridData(out _, out GridConfig grid, out _, out _))
            return false;

        return IsHaulerAtBuildingApproach(currentCell, unitFootprint, building, grid);
    }

    public bool TryResolveConfiguredUnitPrefabEntity(GameObject unitPrefab, out Entity prefabEntity)
    {
        prefabEntity = Entity.Null;
        if (unitPrefab == null || !TryGetEntityManager(out EntityManager em))
            return false;

        return TryGetSpawnUnitPrefabEntity(em, unitPrefab, out prefabEntity);
    }

    public bool TrySpendDollars(int amount)
    {
        amount = Mathf.Max(0, amount);
        if (amount <= 0)
            return true;
        if (_resourceDollars < amount)
            return false;

        _resourceDollars -= amount;
        return true;
    }

    public void SetInitialResourceTotals(int dollars, int oilBarrels, int fuelBarrels)
    {
        _resourceDollars = Mathf.Max(0, dollars);
    }

    private bool IsHouseBuilding(RuntimeBuildingData building)
    {
        if (building?.Definition == null)
            return false;

        if (building.Definition.Role == BuildingRole.House)
            return true;

        if (building.Definition.Role != BuildingRole.None)
            return false;

        string prefabName = building.Definition.Prefab != null ? building.Definition.Prefab.name : string.Empty;
        if (_runtimeCitySpawnerSystem != null && building.Definition.Prefab != null)
            return _runtimeCitySpawnerSystem.IsConfiguredHousePrefab(building.Definition.Prefab);

        return prefabName.IndexOf("house", System.StringComparison.OrdinalIgnoreCase) >= 0 &&
               !building.Definition.IsWall;
    }

    public bool TryResolveConfiguredSpawnablePrefab(Entity prefabEntity, out GameObject prefab)
    {
        prefab = null;
        if (prefabEntity == Entity.Null || World.DefaultGameObjectInjectionWorld == null)
            return false;

        var em = World.DefaultGameObjectInjectionWorld.EntityManager;
        string key = GetSpawnableLookupKey(em.GetName(prefabEntity));
        return !string.IsNullOrEmpty(key) && _spawnablesByKey.TryGetValue(key, out prefab) && prefab != null;
    }

    public bool TryResolveConfiguredSpawnablePrefab(string lookupKey, out GameObject prefab)
    {
        prefab = null;
        string key = GetSpawnableLookupKey(lookupKey);
        return !string.IsNullOrEmpty(key) && _spawnablesByKey.TryGetValue(key, out prefab) && prefab != null;
    }

    public bool TryResolveConfiguredUnitSpawnPrefab(string lookupKey, out GameObject prefab)
    {
        prefab = null;
        string key = GetSpawnableLookupKey(lookupKey);
        return !string.IsNullOrEmpty(key) && _unitSpawnPrefabsByKey.TryGetValue(key, out prefab) && prefab != null;
    }

    public bool IsDraggingPlacementPreview => _activePlacement != null && _isDraggingPlacement;

    public bool TryResolveSpawnUnitPrefab(Entity prefabEntity, out GameObject spawnUnitPrefab)
    {
        spawnUnitPrefab = null;
        if (prefabEntity == Entity.Null || World.DefaultGameObjectInjectionWorld == null)
            return false;

        var em = World.DefaultGameObjectInjectionWorld.EntityManager;
        EnsureEntityQueries(em);
        return TryResolveSpawnUnitPrefabFromRegistry(em, prefabEntity, out spawnUnitPrefab);
    }

    public bool TryResolveLiveUnitPreviewPrefab(Entity unitEntity, out GameObject prefab)
    {
        prefab = null;
        if (unitEntity == Entity.Null || !TryGetEntityManager(out EntityManager em) || !em.Exists(unitEntity))
            return false;

        if (em.HasComponent<UnitRespawnPrefab>(unitEntity))
        {
            Entity prefabEntity = em.GetComponentData<UnitRespawnPrefab>(unitEntity).Prefab;
            if (prefabEntity != Entity.Null && TryResolveSpawnUnitPrefab(prefabEntity, out prefab) && prefab != null)
                return true;
        }

        foreach (var pair in _runtimeBuildings)
        {
            RuntimeBuildingData building = pair.Value;
            if (building?.ProducedUnitPrefabs == null)
                continue;

            if (building.ProducedUnitPrefabs.TryGetValue(unitEntity, out prefab) && prefab != null)
                return true;
        }

        if (em.HasComponent<UnitSourcePrefabKey>(unitEntity))
        {
            string key = em.GetComponentData<UnitSourcePrefabKey>(unitEntity).Value.ToString();
            if (!string.IsNullOrEmpty(key) &&
                _unitSpawnPrefabsByKey.TryGetValue(GetSpawnableLookupKey(key), out prefab) &&
                prefab != null)
            {
                return true;
            }
        }

        return false;
    }

    private bool TryResolveSpawnUnitPrefabFromRegistry(EntityManager em, Entity prefabEntity, out GameObject spawnUnitPrefab)
    {
        spawnUnitPrefab = null;
        if (_unitPrefabRegistryQuery.IsEmptyIgnoreFilter || unitSpawnPrefabs == null || unitSpawnPrefabs.Count == 0)
            return false;

        Entity registryEntity = _unitPrefabRegistryQuery.GetSingletonEntity();
        DynamicBuffer<UnitPrefabRegistryEntry> registry = em.GetBuffer<UnitPrefabRegistryEntry>(registryEntity);
        int count = math.min(registry.Length, unitSpawnPrefabs.Count);
        if (count <= 0)
            return false;

        for (int i = 0; i < count; i++)
        {
            if (registry[i].Prefab != prefabEntity)
                continue;

            spawnUnitPrefab = unitSpawnPrefabs[i];
            return spawnUnitPrefab != null;
        }

        return false;
    }

    private GameObject TryGetSelectedBuildingProductionPrefab(CreateSlot slot)
    {
        return TryGetSelectedBuildingProductionPrefab((int)slot);
    }

    public GameObject TryGetSelectedBuildingProductionPrefab(int productionIndex)
    {
        int? buildingId = ActiveBuildingId;
        if (!buildingId.HasValue || !_runtimeBuildings.TryGetValue(buildingId.Value, out RuntimeBuildingData building))
            return null;

        return GetProductionPrefab(building.Definition, productionIndex);
    }

    public string PlacementStatusText
    {
        get
        {
            if (_activePlacement == null)
                return "Choose a build type.";

            string state = _activePlacement.IsValid ? "Valid placement" : "Blocked by road or blocker";
            Vector2Int origin = _activePlacement.OriginCell;
            Vector2Int size = _activePlacement.Definition.FootprintCells;
            return $"{_activePlacement.Definition.DisplayName}: {state} ({origin.x},{origin.y}) {size.x}x{size.y}";
        }
    }

    public string SelectedBuildingLabel
    {
        get
        {
            int? buildingId = ActiveBuildingId;
            if (!buildingId.HasValue)
                return "Building";

            RuntimeBuildingData building = _runtimeBuildings[buildingId.Value];
            return $"{building.Definition.DisplayName} ({building.OriginCell.x},{building.OriginCell.y})";
        }
    }

    public string SelectedBuildingDisplayName
    {
        get
        {
            int? buildingId = ActiveBuildingId;
            if (!buildingId.HasValue)
                return "Building";

            RuntimeBuildingData building = _runtimeBuildings[buildingId.Value];
            return string.IsNullOrWhiteSpace(building.Definition.DisplayName)
                ? "Building"
                : building.Definition.DisplayName;
        }
    }

    public string SelectedBuildingDescription
    {
        get
        {
            int? buildingId = ActiveBuildingId;
            if (!buildingId.HasValue)
                return "Select a building to see its options.";

            RuntimeBuildingData building = _runtimeBuildings[buildingId.Value];
            string description = string.IsNullOrWhiteSpace(building.Definition.Description)
                ? "Operational building."
                : building.Definition.Description;
            return $"{description} Footprint: {building.Definition.FootprintCells.x}x{building.Definition.FootprintCells.y}.";
        }
    }

    public bool TryGetSelectedBuildingPreviewPrefab(out GameObject prefab)
    {
        prefab = null;
        int? buildingId = ActiveBuildingId;
        if (!buildingId.HasValue || !_runtimeBuildings.TryGetValue(buildingId.Value, out RuntimeBuildingData building) || building?.Definition == null)
            return false;

        prefab = building.Definition.Prefab;
        return prefab != null;
    }

    public bool TryGetSelectedBuildingHealth(out int current, out int max)
    {
        current = 0;
        max = 0;

        int? buildingId = ActiveBuildingId;
        if (!buildingId.HasValue)
            return false;

        RuntimeBuildingData building = _runtimeBuildings[buildingId.Value];
        max = Mathf.Max(1, building.Definition.MaxHealth);
        current = max;

        if (building.CombatEntity == Entity.Null || World.DefaultGameObjectInjectionWorld == null)
            return true;

        EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        if (!entityManager.Exists(building.CombatEntity) || !entityManager.HasComponent<UnitHealth>(building.CombatEntity))
            return true;

        UnitHealth health = entityManager.GetComponentData<UnitHealth>(building.CombatEntity);
        current = health.Current;
        max = Mathf.Max(1, health.Max);
        return true;
    }

    public string DeleteButtonText => "Destroy";

    private void OnValidate()
    {
        ApplyConfigIfAvailable();
    }

    public void Init(
        BuildingPlacementSystemConfig configAsset,
        Camera sceneWorldCamera,
        Transform runtimeRoot,
        RoadBuildSystem roadBuildController,
        MainMenuPlayUI mainMenuPlayUi,
        FactionVisualSettings factionVisualSettings,
        DayNightSystem dayNightSystem)
    {
        config = configAsset;
        worldCamera = sceneWorldCamera;
        _runtimeRoot = runtimeRoot;
        _roadBuildController = roadBuildController;
        _mainMenuPlayUi = mainMenuPlayUi;
        _factionVisualSettings = factionVisualSettings;
        _dayNightSystem = dayNightSystem;
        ApplyConfigIfAvailable();
        _markerPropertyBlock = new MaterialPropertyBlock();

        _buildingRoot = new GameObject("RuntimeBuildings").transform;
        _buildingRoot.SetParent(_runtimeRoot, false);
        _buildingRoot.localPosition = Vector3.zero;
        _buildingRoot.localRotation = Quaternion.identity;
        _buildingRoot.localScale = Vector3.one;

        RebuildConfiguredSpawnableDefinitions();
        CreatePlacementOutline();
    }

    public void BindDependencies(
        RoadBuildSystem roadBuildController,
        MainMenuPlayUI mainMenuPlayUi,
        DayNightSystem dayNightSystem = null,
        RTSSelectionSystem selectionSystem = null,
        RuntimeGridBlockerSystem runtimeGridBlockerSystem = null,
        RuntimeCitySpawnerSystem runtimeCitySpawnerSystem = null,
        CitizenPopulationSystem citizenPopulationSystem = null)
    {
        _roadBuildController = roadBuildController;
        _mainMenuPlayUi = mainMenuPlayUi;
        if (selectionSystem != null)
            _selectionSystem = selectionSystem;
        if (runtimeGridBlockerSystem != null)
            _runtimeGridBlockerSystem = runtimeGridBlockerSystem;
        if (runtimeCitySpawnerSystem != null)
            _runtimeCitySpawnerSystem = runtimeCitySpawnerSystem;
        if (citizenPopulationSystem != null)
            _citizenPopulationSystem = citizenPopulationSystem;
        if (dayNightSystem != null)
            _dayNightSystem = dayNightSystem;
    }

    private void ApplyConfigIfAvailable()
    {
        if (config == null)
            return;

        if (config.WorldCamera != null)
            worldCamera = config.WorldCamera;
        spawnables = config.Spawnables ?? new List<GameObject>();
        unitPrefabRegistryConfig = config.UnitPrefabRegistryConfig;
        unitSpawnPrefabs = unitPrefabRegistryConfig != null && unitPrefabRegistryConfig.UnitSpawnPrefabs != null
            ? unitPrefabRegistryConfig.UnitSpawnPrefabs
            : new List<GameObject>();
        RebuildSpawnablesLookup();
        buildPlaneY = config.BuildPlaneY;
        placementOutlineHeight = config.PlacementOutlineHeight;
        placementValidColor = config.PlacementValidColor;
        placementInvalidColor = config.PlacementInvalidColor;
    }

    private void RebuildSpawnablesLookup()
    {
        _spawnablesByKey.Clear();
        _unitSpawnPrefabsByKey.Clear();
        if (spawnables == null)
            spawnables = new List<GameObject>();

        for (int i = 0; i < spawnables.Count; i++)
        {
            GameObject prefab = spawnables[i];
            if (prefab == null)
                continue;

            RegisterSpawnableLookupAliases(_spawnablesByKey, prefab);
        }

        if (unitSpawnPrefabs == null)
            return;

        for (int i = 0; i < unitSpawnPrefabs.Count; i++)
        {
            GameObject prefab = unitSpawnPrefabs[i];
            if (prefab == null)
                continue;

            RegisterSpawnableLookupAliases(_unitSpawnPrefabsByKey, prefab);
        }
    }

    private void RebuildConfiguredSpawnableDefinitions()
    {
        for (int i = 0; i < _configuredSpawnableDefinitions.Count; i++)
            CleanupCombinedVisualTemplate(_configuredSpawnableDefinitions[i]);

        _configuredSpawnableDefinitions.Clear();
        _configuredDefinitionsByPrefab.Clear();

        if (spawnables == null)
            return;

        for (int i = 0; i < spawnables.Count; i++)
        {
            GameObject prefab = spawnables[i];
            if (prefab == null)
                continue;

            BuildingDefinition definition = CreateDefinition(
                prefab,
                prefab.name,
                "Operational building.",
                500,
                null,
                null,
                null);
            BuildCombinedVisualTemplate(definition);
            CacheBuildingBounds(definition);
            _configuredSpawnableDefinitions.Add(definition);
            _configuredDefinitionsByPrefab[prefab] = definition;
        }

        _soldierBaseDefinition = FindConfiguredDefinition("Soldier Base");
        _soldierTentDefinition = FindConfiguredDefinition("Soldier Tent");
        _factoryDefinition = FindConfiguredDefinition("Factory");
    }

    private BuildingDefinition FindConfiguredDefinition(string displayName)
    {
        string key = NormalizeSpawnableKey(displayName);
        for (int i = 0; i < _configuredSpawnableDefinitions.Count; i++)
        {
            BuildingDefinition definition = _configuredSpawnableDefinitions[i];
            if (NormalizeSpawnableKey(definition.DisplayName) == key)
                return definition;
        }

        return null;
    }

    public void Dispose()
    {
        ExitBuildMode();

        foreach (var building in _runtimeBuildings.Values)
        {
            if (building.Instance != null)
                DestroyRuntimeObject(building.Instance);

            if (TryGetEntityManager(out EntityManager em))
            {
                if (building.CombatEntity != Entity.Null && em.Exists(building.CombatEntity))
                    em.DestroyEntity(building.CombatEntity);
                if (building.BlockerEntity != Entity.Null && em.Exists(building.BlockerEntity))
                    em.DestroyEntity(building.BlockerEntity);
            }
        }

        _runtimeBuildingSystem.Clear();

        for (int i = 0; i < _configuredSpawnableDefinitions.Count; i++)
            CleanupCombinedVisualTemplate(_configuredSpawnableDefinitions[i]);
        _configuredSpawnableDefinitions.Clear();
        _configuredDefinitionsByPrefab.Clear();
        _unitSpawnPrefabsByKey.Clear();
        _soldierBaseDefinition = null;
        _soldierTentDefinition = null;
        _factoryDefinition = null;

        if (_placementOutline != null)
            DestroyRuntimeObject(_placementOutline);
        if (_buildingRoot != null)
            DestroyRuntimeObject(_buildingRoot.gameObject);
    }

    private static void DestroyRuntimeObject(Object target)
    {
        if (target == null)
            return;

        if (Application.isPlaying)
            Destroy(target);
        else
            DestroyImmediate(target);
    }

    private void EnsureEntityQueries(EntityManager em)
    {
        World world = em.World;
        if (_queryWorld == world && world != null && world.IsCreated)
            return;

        _queryWorld = world;
        _gridDataQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<GridConfig>(),
            ComponentType.ReadOnly<GridRoad>(),
            ComponentType.ReadOnly<DynamicBlockerData>());
        _redirectUnitsQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<UnitMove>(),
            ComponentType.ReadOnly<UnitGrid>(),
            ComponentType.ReadOnly<LocalTransform>());
        _unitPrefabRegistryQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<UnitPrefabRegistryTag>(),
            ComponentType.ReadOnly<UnitPrefabRegistryEntry>());
        _spawnPrefabCandidatesQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<Prefab>(),
            ComponentType.ReadOnly<UnitMove>());
        _selectedUnitsQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<SelectedUnitTag>(),
            ComponentType.ReadOnly<UnitGrid>(),
            ComponentType.ReadOnly<UnitMove>());
        _haulerUnitsQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<UnitResourceHauler>(),
            ComponentType.ReadOnly<UnitResourceHaulOrder>(),
            ComponentType.ReadOnly<UnitGrid>(),
            ComponentType.ReadOnly<UnitMove>());
        _livePlayerUnitsQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<Faction>(),
            ComponentType.ReadOnly<UnitRespawnPrefab>(),
            ComponentType.ReadOnly<UnitMove>());
        _liveUnitFootprintQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<UnitGrid>(),
            ComponentType.ReadOnly<UnitFootprint>());
        _liveFactionUnitsQuery = em.CreateEntityQuery(new EntityQueryDesc
        {
            All = new[]
            {
                ComponentType.ReadOnly<Faction>(),
                ComponentType.ReadOnly<UnitGrid>(),
                ComponentType.ReadOnly<UnitFootprint>()
            },
            None = new[]
            {
                ComponentType.ReadOnly<StaticGridBlocker>(),
                ComponentType.ReadOnly<RuntimeBuildingCombatTag>()
            }
        });
    }

    public void Update()
    {
        double startTime = Time.realtimeSinceStartupAsDouble;
        double afterProductions = startTime;
        double afterResources = startTime;
        double afterHaulers = startTime;
        double afterResourceVisuals = startTime;
        double afterReservations = startTime;
        double afterDestroyed = startTime;
        double afterDoors = startTime;
        double afterMarkers = startTime;
        double afterInputOutline = startTime;
        double afterInputMouse = startTime;
        double afterInputUi = startTime;
        double afterInputBuildingClick = startTime;
        double afterInput = startTime;
        try
        {
        ProcessPendingProductions();
        afterProductions = Time.realtimeSinceStartupAsDouble;
        UpdateResourceProduction();
        afterResources = Time.realtimeSinceStartupAsDouble;
        UpdateResourceHaulers();
        afterHaulers = Time.realtimeSinceStartupAsDouble;
        UpdateBuildingResourceVisuals();
        afterResourceVisuals = Time.realtimeSinceStartupAsDouble;
        CleanupRecentSpawnReservations();
        afterReservations = Time.realtimeSinceStartupAsDouble;
        SyncDestroyedRuntimeBuildingCombatEntities();
        UpdateDestroyedBuildings();
        afterDestroyed = Time.realtimeSinceStartupAsDouble;
        UpdateRoadBarrierDoors(Time.deltaTime);
        afterDoors = Time.realtimeSinceStartupAsDouble;
        if (_pendingMarkerRefresh)
        {
            RefreshBuildingMarkerVisibility();
            _pendingMarkerRefresh = false;
        }
        afterMarkers = Time.realtimeSinceStartupAsDouble;

        if (worldCamera == null)
            return;

        bool hasPointer = GamePointerInput.TryGetPrimaryPointer(out GamePointerState pointer);
        afterInputMouse = Time.realtimeSinceStartupAsDouble;
        if (!hasPointer)
            return;

        if (_activePlacement != null)
        {
            Vector2 pointerPosition = pointer.Position;
            if (pointer.WasPressedThisFrame &&
                !_ignorePointerUpdatesUntilRelease &&
                !IsPointerOverPlacementUi(pointerPosition))
            {
                bool canStartDrag = IsPointerOverActivePlacement(pointerPosition);
                if (!canStartDrag &&
                    IsLinearWallDefinition(_activePlacement.Definition) &&
                    TryGetGridData(out _, out GridConfig grid, out _, out _) &&
                    TryGetGridCell(pointerPosition, grid, out Vector2Int clickedCell))
                {
                    Vector2Int clickedOrigin = CenterCellToOrigin(clickedCell, _activePlacement.Definition.FootprintCells);
                    _activePlacement.OriginCell = clickedOrigin;
                    _activePlacement.CommittedOriginCell = clickedOrigin;
                    _activePlacement.DragStartOriginCell = clickedOrigin;
                    _activePlacement.DragCurrentOriginCell = clickedOrigin;
                    _activePlacement.DragFirstAxis = DragFirstAxis.None;
                    _activePlacement.HideCurrentWallPreview = false;
                    canStartDrag = true;
                }

                if (canStartDrag)
                {
                    _isDraggingPlacement = true;
                    _activePlacement.CommittedOriginCell = _activePlacement.OriginCell;
                    _activePlacement.DragStartOriginCell = _activePlacement.OriginCell;
                    _activePlacement.DragCurrentOriginCell = _activePlacement.OriginCell;
                    _activePlacement.DragFirstAxis = DragFirstAxis.None;
                    _activePlacement.HideCurrentWallPreview = false;
                }
            }
            if (pointer.WasReleasedThisFrame)
            {
                if (_isDraggingPlacement &&
                    _activePlacement != null &&
                    IsLinearWallDefinition(_activePlacement.Definition) &&
                    _activePlacement.IsValid)
                {
                    CommitCurrentWallRun(_activePlacement);
                }
                _isDraggingPlacement = false;
                _ignorePointerUpdatesUntilRelease = false;
            }
            if (_isDraggingPlacement && !pointer.IsPressed)
                _isDraggingPlacement = false;

            UpdatePlacement(pointerPosition);
            afterInput = Time.realtimeSinceStartupAsDouble;
            afterInputOutline = afterInput;
            afterInputUi = afterInput;
            afterInputBuildingClick = afterInput;
            return;
        }

        if (!InitialUnitsRuntimeState.PlayRequested)
        {
            HidePlacementOutline();
            afterInputOutline = Time.realtimeSinceStartupAsDouble;
            afterInput = afterInputOutline;
            afterInputUi = afterInput;
            afterInputBuildingClick = afterInput;
            return;
        }

        if (!InitialUnitsRuntimeState.BuildModeActive)
            HidePlacementOutline();
        afterInputOutline = Time.realtimeSinceStartupAsDouble;

        if (pointer.WasPressedThisFrame)
        {
            Vector2 pointerPosition = pointer.Position;
            bool ignoreBecauseCommandUiPressed = _mainMenuPlayUi != null && _mainMenuPlayUi.ShouldIgnoreBuildingSelectionThisFrame();
            bool overGameplayUi = IsPointerOverAnyGameplayUi(pointerPosition);
            bool overUnitCommandUi = false;
            string unitCommandSource = null;
            if (!ignoreBecauseCommandUiPressed && !overGameplayUi && HasActiveBuilding && _mainMenuPlayUi != null)
                overUnitCommandUi = _mainMenuPlayUi.IsPointerOverUnitCommandUi(pointerPosition, out unitCommandSource);
            afterInputUi = Time.realtimeSinceStartupAsDouble;

            if (!ignoreBecauseCommandUiPressed && !overGameplayUi && overUnitCommandUi && HasActiveBuilding)
            {
                InitialUnitsRuntimeState.SuppressNextWorldClick = true;
                afterInput = Time.realtimeSinceStartupAsDouble;
                afterInputBuildingClick = afterInput;
                return;
            }

            if (!ignoreBecauseCommandUiPressed && !overGameplayUi && !overUnitCommandUi)
            {
                HandleBuildingSelectionClick(pointerPosition);
                afterInputBuildingClick = Time.realtimeSinceStartupAsDouble;
            }
        }
        afterInput = Time.realtimeSinceStartupAsDouble;
        if (afterInputUi < afterInputOutline)
            afterInputUi = afterInputOutline;
        if (afterInputBuildingClick < afterInputUi)
            afterInputBuildingClick = afterInputUi;
        }
        finally
        {
            double elapsed = Time.realtimeSinceStartupAsDouble - startTime;
            if (EnableBuildingPlacementDiagnostics && elapsed >= FreezeLogThresholdSeconds)
            {
                if (afterProductions < startTime) afterProductions = startTime;
                if (afterResources < afterProductions) afterResources = afterProductions;
                if (afterHaulers < afterResources) afterHaulers = afterResources;
                if (afterResourceVisuals < afterHaulers) afterResourceVisuals = afterHaulers;
                if (afterReservations < afterResourceVisuals) afterReservations = afterResourceVisuals;
                if (afterDestroyed < afterReservations) afterDestroyed = afterReservations;
                if (afterDoors < afterDestroyed) afterDoors = afterDestroyed;
                if (afterMarkers < afterDoors) afterMarkers = afterDoors;
                if (afterInputOutline < afterMarkers) afterInputOutline = afterMarkers;
                if (afterInputMouse < afterInputOutline) afterInputMouse = afterInputOutline;
                if (afterInputUi < afterInputMouse) afterInputUi = afterInputMouse;
                if (afterInputBuildingClick < afterInputUi) afterInputBuildingClick = afterInputUi;
                if (afterInput < afterInputBuildingClick) afterInput = afterInputBuildingClick;

                Debug.Log(
                    $"[BuildingPlacementDiag] frame={Time.frameCount} total={elapsed * 1000d:F1}ms " +
                    $"productions={(afterProductions - startTime) * 1000d:F1}ms " +
                    $"resources={(afterResources - afterProductions) * 1000d:F1}ms " +
                    $"haulers={(afterHaulers - afterResources) * 1000d:F1}ms " +
                    $"resourceVisuals={(afterResourceVisuals - afterHaulers) * 1000d:F1}ms " +
                    $"reservations={(afterReservations - afterResourceVisuals) * 1000d:F1}ms " +
                    $"destroyed={(afterDestroyed - afterReservations) * 1000d:F1}ms " +
                    $"doors={(afterDoors - afterDestroyed) * 1000d:F1}ms " +
                    $"markers={(afterMarkers - afterDoors) * 1000d:F1}ms " +
                    $"input={(afterInput - afterMarkers) * 1000d:F1}ms " +
                    $"inputOutline={(afterInputOutline - afterMarkers) * 1000d:F1}ms " +
                    $"inputMouse={(afterInputMouse - afterInputOutline) * 1000d:F1}ms " +
                    $"inputUi={(afterInputUi - afterInputMouse) * 1000d:F1}ms " +
                    $"inputBuilding={(afterInputBuildingClick - afterInputUi) * 1000d:F1}ms " +
                    $"buildings={_runtimeBuildings.Count}");
            }
        }
    }

    private void UpdateResourceProduction()
    {
        if (_runtimeBuildings.Count == 0)
            return;

        float secondsPerDay = _dayNightSystem != null
            ? Mathf.Max(1f, _dayNightSystem.FullDayDurationMinutes * 60f)
            : 300f;
        float deltaTime = Time.deltaTime;

        foreach (var pair in _runtimeBuildings)
        {
            RuntimeBuildingData building = pair.Value;
            if (building == null || building.IsDestroyed || building.Definition == null)
                continue;

            int capacity = Mathf.Max(0, building.Definition.OilStorageCapacity);
            float barrelsPerDay = Mathf.Max(0f, building.Definition.OilBarrelsPerDay);
            if (capacity > 0 && barrelsPerDay > 0f)
            {
                if (building.StoredOilBarrels >= capacity)
                {
                    building.StoredOilBarrels = capacity;
                }
                else
                {
                    float barrelsPerSecond = barrelsPerDay / secondsPerDay;
                    float previousOil = building.StoredOilBarrels;
                    building.StoredOilBarrels = Mathf.Min(capacity, building.StoredOilBarrels + barrelsPerSecond * deltaTime);
                    GameRuntimeStats.RecordOilExtracted(building.StoredOilBarrels - previousOil);
                }
            }

            float fuelBarrelsPerDay = Mathf.Max(0f, building.Definition.FuelBarrelsPerDay);
            int fuelCapacity = Mathf.Max(0, building.Definition.FuelStorageCapacity);
            if (fuelBarrelsPerDay > 0f)
            {
                float maxFuelFromOil = building.StoredOilBarrels / OilBarrelsPerFuelBarrel;
                if (maxFuelFromOil > 0f)
                {
                    float desiredFuel = (fuelBarrelsPerDay / secondsPerDay) * deltaTime;
                    float producedFuel = Mathf.Min(desiredFuel, maxFuelFromOil);
                    if (fuelCapacity > 0)
                        producedFuel = Mathf.Min(producedFuel, Mathf.Max(0f, fuelCapacity - building.StoredFuelBarrels));

                    if (producedFuel > 0f)
                    {
                        building.StoredOilBarrels = Mathf.Max(0f, building.StoredOilBarrels - (producedFuel * OilBarrelsPerFuelBarrel));
                        if (fuelCapacity > 0)
                            building.StoredFuelBarrels = Mathf.Min(fuelCapacity, building.StoredFuelBarrels + producedFuel);
                        GameRuntimeStats.RecordFuelProduced(producedFuel);
                    }
                }
            }
        }
    }

    private void UpdateResourceHaulers()
    {
        if (UnitPathfindingSystem.HasPendingPathJob)
            return;

        if (!TryGetEntityManager(out EntityManager em))
            return;
        EnsureEntityQueries(em);
        if (!TryGetGridData(out _, out GridConfig grid, out _, out _))
            return;

        using var haulerQuery = _haulerUnitsQuery.ToEntityArray(Allocator.Temp);

        if (haulerQuery.Length == 0)
            return;

        float now = Time.time;
        for (int i = 0; i < haulerQuery.Length; i++)
        {
            Entity entity = haulerQuery[i];
            if (!em.Exists(entity))
                continue;

            UnitResourceHauler hauler = em.GetComponentData<UnitResourceHauler>(entity);
            UnitResourceHaulOrder order = em.GetComponentData<UnitResourceHaulOrder>(entity);
            int2 footprintSize = em.HasComponent<UnitFootprint>(entity)
                ? em.GetComponentData<UnitFootprint>(entity).Size
                : new int2(1, 1);
            ResourceHaulKind resourceKind = (ResourceHaulKind)order.ResourceKind;

            if (!TryGetRuntimeBuilding(order.SourceBuildingId, out RuntimeBuildingData source) ||
                !TryGetRuntimeBuilding(order.DestinationBuildingId, out RuntimeBuildingData destination))
            {
                em.RemoveComponent<UnitResourceHaulOrder>(entity);
                continue;
            }

            int2 currentCell = em.GetComponentData<UnitGrid>(entity).Cell;
            switch ((ResourceHaulPhase)order.Phase)
            {
                case ResourceHaulPhase.None:
                {
                    if (!TryIssueHaulerMoveToBuilding(em, entity, source, out int2 goal))
                        continue;

                    order.TargetCell = goal;
                    order.Phase = (byte)ResourceHaulPhase.ToSource;
                    order.ActionEndsAt = 0f;
                    em.SetComponentData(entity, order);
                    break;
                }

                case ResourceHaulPhase.ToSource:
                {
                    if (!IsHaulerAtBuildingApproach(currentCell, footprintSize, source, grid))
                    {
                        if (VerboseResourceHaulerLogs)
                            Debug.Log($"[ResourceHauler] entity={entity} phase=ToSource current={currentCell} target={order.TargetCell} source={source.Id} sourceOrigin={source.OriginCell}");
                        if (!HasGoalOrPathRequest(em, entity, order.TargetCell))
                        {
                            if (VerboseResourceHaulerLogs)
                                Debug.Log($"[ResourceHauler] entity={entity} reissuing-source-move source={source.Id}");
                            TryIssueHaulerMoveToBuilding(em, entity, source, out _);
                        }
                        break;
                    }

                    if (VerboseResourceHaulerLogs)
                        Debug.Log($"[ResourceHauler] entity={entity} arrived-source source={source.Id} current={currentCell}");
                    order.Phase = (byte)ResourceHaulPhase.Loading;
                    order.ActionEndsAt = 0f;
                    em.SetComponentData(entity, order);
                    break;
                }

                case ResourceHaulPhase.Loading:
                {
                    float loadAmount = Mathf.Max(0f, hauler.BarrelCapacity);
                    if (loadAmount <= 0f)
                    {
                        Debug.LogWarning($"[ResourceHauler] entity={entity} invalid-capacity capacity={hauler.BarrelCapacity}");
                        em.RemoveComponent<UnitResourceHaulOrder>(entity);
                        break;
                    }

                    float sourceStored = resourceKind == ResourceHaulKind.Fuel ? source.StoredFuelBarrels : source.StoredOilBarrels;
                    float currentCargo = resourceKind == ResourceHaulKind.Fuel ? hauler.CargoFuelBarrels : hauler.CargoOilBarrels;
                    if (VerboseResourceHaulerLogs)
                        Debug.Log($"[ResourceHauler] entity={entity} phase=Loading resource={resourceKind} current={currentCell} source={source.Id} stored={sourceStored:0.##} cargo={currentCargo:0.##}/{loadAmount:0.##} actionEndsAt={order.ActionEndsAt:0.##} now={now:0.##}");
                    if (sourceStored + 0.001f < loadAmount)
                    {
                        if (VerboseResourceHaulerLogs)
                            Debug.Log($"[ResourceHauler] entity={entity} waiting-for-resource resource={resourceKind} source={source.Id} stored={sourceStored:0.##} need={loadAmount:0.##}");
                        break;
                    }

                    if (order.ActionEndsAt <= 0f)
                    {
                        order.ActionEndsAt = now + Mathf.Max(0f, hauler.FillDurationSeconds);
                        em.SetComponentData(entity, order);
                        if (VerboseResourceHaulerLogs)
                            Debug.Log($"[ResourceHauler] entity={entity} loading-started source={source.Id} fillDuration={hauler.FillDurationSeconds:0.##} completeAt={order.ActionEndsAt:0.##}");
                        break;
                    }

                    if (now < order.ActionEndsAt)
                    {
                        if (VerboseResourceHaulerLogs)
                            Debug.Log($"[ResourceHauler] entity={entity} loading-in-progress source={source.Id} remaining={order.ActionEndsAt - now:0.##}");
                        break;
                    }

                    sourceStored = resourceKind == ResourceHaulKind.Fuel ? source.StoredFuelBarrels : source.StoredOilBarrels;
                    if (sourceStored + 0.001f < loadAmount)
                    {
                        order.ActionEndsAt = 0f;
                        em.SetComponentData(entity, order);
                        if (VerboseResourceHaulerLogs)
                            Debug.Log($"[ResourceHauler] entity={entity} loading-reset-insufficient-resource resource={resourceKind} source={source.Id} stored={sourceStored:0.##} need={loadAmount:0.##}");
                        break;
                    }

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
                    em.SetComponentData(entity, hauler);
                    if (VerboseResourceHaulerLogs)
                        Debug.Log($"[ResourceHauler] entity={entity} loading-complete resource={resourceKind} source={source.Id} loaded={loadAmount:0.##}");

                    if (!TryIssueHaulerMoveToBuilding(em, entity, destination, out int2 destinationGoal))
                    {
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
                        em.SetComponentData(entity, hauler);
                        if (VerboseResourceHaulerLogs)
                            Debug.LogWarning($"[ResourceHauler] entity={entity} failed-destination-move destination={destination.Id} revertedLoad={loadAmount:0.##}");
                        break;
                    }

                    order.TargetCell = destinationGoal;
                    order.Phase = (byte)ResourceHaulPhase.ToDestination;
                    order.ActionEndsAt = 0f;
                    em.SetComponentData(entity, order);
                    if (VerboseResourceHaulerLogs)
                        Debug.Log($"[ResourceHauler] entity={entity} to-destination destination={destination.Id} target={destinationGoal}");
                    break;
                }

                case ResourceHaulPhase.ToDestination:
                {
                    if (!IsHaulerAtBuildingApproach(currentCell, footprintSize, destination, grid))
                    {
                        if (!HasGoalOrPathRequest(em, entity, order.TargetCell))
                            TryIssueHaulerMoveToBuilding(em, entity, destination, out _);
                        break;
                    }

                    order.Phase = (byte)ResourceHaulPhase.Unloading;
                    order.ActionEndsAt = 0f;
                    em.SetComponentData(entity, order);
                    break;
                }

                case ResourceHaulPhase.Unloading:
                {
                    float cargo = resourceKind == ResourceHaulKind.Fuel
                        ? Mathf.Max(0f, hauler.CargoFuelBarrels)
                        : Mathf.Max(0f, hauler.CargoOilBarrels);
                    if (cargo <= 0f)
                    {
                        order.Phase = (byte)ResourceHaulPhase.None;
                        order.ActionEndsAt = 0f;
                        em.SetComponentData(entity, order);
                        break;
                    }

                    float freeSpace = resourceKind == ResourceHaulKind.Fuel
                        ? GetFuelReceivingFreeCapacity(destination)
                        : GetOilReceivingFreeCapacity(destination);
                    if (freeSpace + 0.001f < cargo)
                        break;

                    if (order.ActionEndsAt <= 0f)
                    {
                        order.ActionEndsAt = now + Mathf.Max(0f, hauler.UnloadDurationSeconds);
                        em.SetComponentData(entity, order);
                        break;
                    }

                    if (now < order.ActionEndsAt)
                        break;

                    freeSpace = resourceKind == ResourceHaulKind.Fuel
                        ? GetFuelReceivingFreeCapacity(destination)
                        : GetOilReceivingFreeCapacity(destination);
                    if (freeSpace + 0.001f < cargo)
                    {
                        order.ActionEndsAt = 0f;
                        em.SetComponentData(entity, order);
                        break;
                    }

                    if (resourceKind == ResourceHaulKind.Fuel)
                    {
                        destination.StoredFuelBarrels += cargo;
                        if (destination.Definition.FuelStorageCapacity > 0)
                            destination.StoredFuelBarrels = Mathf.Min(destination.Definition.FuelStorageCapacity, destination.StoredFuelBarrels);
                        hauler.CargoFuelBarrels = 0f;
                    }
                    else
                    {
                        destination.StoredOilBarrels += cargo;
                        if (destination.Definition.OilStorageCapacity > 0)
                            destination.StoredOilBarrels = Mathf.Min(destination.Definition.OilStorageCapacity, destination.StoredOilBarrels);
                        hauler.CargoOilBarrels = 0f;
                    }
                    em.SetComponentData(entity, hauler);

                    if (!TryIssueHaulerMoveToBuilding(em, entity, source, out int2 sourceGoal))
                    {
                        order.Phase = (byte)ResourceHaulPhase.None;
                        order.ActionEndsAt = 0f;
                        em.SetComponentData(entity, order);
                        break;
                    }

                    order.TargetCell = sourceGoal;
                    order.Phase = (byte)ResourceHaulPhase.ToSource;
                    order.ActionEndsAt = 0f;
                    em.SetComponentData(entity, order);
                    break;
                }
            }
        }
    }

    private bool IsHaulerAtBuildingApproach(int2 currentCell, int2 footprintSize, RuntimeBuildingData building, GridConfig grid)
    {
        if (building?.Definition == null)
            return false;

        int2 clampedFootprint = UnitFootprintUtility.ClampSize(footprintSize);
        int2 unitMin = UnitFootprintUtility.GetMinCell(currentCell, clampedFootprint);
        RectInt unitRect = new(unitMin.x, unitMin.y, clampedFootprint.x, clampedFootprint.y);
        RectInt buildingRect = GetEffectivePlacementRect(building.Definition, building.OriginCell, grid);
        if (unitRect.Overlaps(buildingRect))
            return false;

        int distanceX = AxisDistance(unitRect.xMin, unitRect.xMax, buildingRect.xMin, buildingRect.xMax);
        int distanceY = AxisDistance(unitRect.yMin, unitRect.yMax, buildingRect.yMin, buildingRect.yMax);
        int approachDistance = math.max(distanceX, distanceY);

        // Allow a small stand-off so large trucks can wait/load beside a building
        // even when pathfinding settles them slightly outside the tight 1-cell ring.
        return approachDistance <= 2;
    }

    private void CleanupRecentSpawnReservations()
    {
        if (_recentSpawnReservations.Count == 0)
            return;

        float now = Time.time;
        for (int i = _recentSpawnReservations.Count - 1; i >= 0; i--)
        {
            if (_recentSpawnReservations[i].ExpiresAt > now)
                continue;

            _recentSpawnReservations.RemoveAt(i);
        }
    }

    public void BeginSoldierBasePlacement()
    {
        if (WarlineCaptureMissionRules.TryRejectBuildForActiveMission())
            return;

        if (_soldierBaseDefinition == null || _soldierBaseDefinition.Prefab == null)
        {
            Debug.LogWarning("BuildingPlacementSystem is missing the Soldier Base spawnable prefab reference.");
            return;
        }

        BeginPlacement(_soldierBaseDefinition);
    }

    public void BeginSoldierTentPlacement()
    {
        if (WarlineCaptureMissionRules.TryRejectBuildForActiveMission())
            return;

        if (_soldierTentDefinition == null || _soldierTentDefinition.Prefab == null)
        {
            Debug.LogWarning("BuildingPlacementSystem is missing the Soldier Tent spawnable prefab reference.");
            return;
        }

        BeginPlacement(_soldierTentDefinition);
    }

    public void BeginFactoryPlacement()
    {
        if (WarlineCaptureMissionRules.TryRejectBuildForActiveMission())
            return;

        if (_factoryDefinition == null || _factoryDefinition.Prefab == null)
        {
            Debug.LogWarning("BuildingPlacementSystem is missing the Factory spawnable prefab reference.");
            return;
        }

        BeginPlacement(_factoryDefinition);
    }

    public bool TryGetConfiguredSpawnable(int index, out ConfiguredSpawnableEntry entry)
    {
        if (index >= 0 && index < _configuredSpawnableDefinitions.Count)
        {
            entry = BuildConfiguredSpawnableEntry(_configuredSpawnableDefinitions[index]);
            return true;
        }

        entry = default;
        return false;
    }

    public bool TryGetConfiguredSpawnable(string buildingId, out ConfiguredSpawnableEntry entry)
    {
        string normalized = NormalizeSpawnableKey(buildingId);
        if (!string.IsNullOrEmpty(normalized) &&
            _spawnablesByKey.TryGetValue(normalized, out GameObject prefab) &&
            prefab != null &&
            _configuredDefinitionsByPrefab.TryGetValue(prefab, out BuildingDefinition matchedDefinition))
        {
            entry = BuildConfiguredSpawnableEntry(matchedDefinition);
            return true;
        }

        for (int i = 0; i < _configuredSpawnableDefinitions.Count; i++)
        {
            BuildingDefinition definition = _configuredSpawnableDefinitions[i];
            if (definition == null || !RuntimeDefinitionMatchesId(definition, normalized))
                continue;

            entry = BuildConfiguredSpawnableEntry(definition);
            return true;
        }

        entry = default;
        return false;
    }

    public bool TryGetConfiguredUnit(int index, out ConfiguredUnitEntry entry)
    {
        if (unitSpawnPrefabs != null && index >= 0 && index < unitSpawnPrefabs.Count)
        {
            GameObject prefab = unitSpawnPrefabs[index];
            if (prefab != null)
            {
                UnitGridAuthoring authoring = prefab.GetComponent<UnitGridAuthoring>();
                string displayName = prefab.name;
                string description = string.Empty;
                bool isVehicle = false;
                if (authoring != null)
                {
                    displayName = ResolveConfiguredUnitDisplayName(prefab, authoring);
                    description = authoring.ConfiguredDescription;
                    Vector2Int footprint = authoring.GetConfiguredFootprintCells();
                    isVehicle = footprint.x > 1 || footprint.y > 1 || prefab.name.IndexOf("Veh", System.StringComparison.OrdinalIgnoreCase) >= 0;
                }

                int price = authoring != null ? authoring.Price : (isVehicle ? 15000 : 10000);
                entry = new ConfiguredUnitEntry(displayName, description, prefab, isVehicle, authoring == null || authoring.CanRequest, price);
                return true;
            }
        }

        entry = default;
        return false;
    }

    private static ConfiguredSpawnableEntry BuildConfiguredSpawnableEntry(BuildingDefinition definition)
    {
        if (definition == null)
            return default;

        bool canRequest = true;
        int price = 20000;
        BuildingDefinitionAuthoring authoring = definition.Prefab != null ? definition.Prefab.GetComponent<BuildingDefinitionAuthoring>() : null;
        if (authoring != null)
        {
            canRequest = authoring.ConfiguredCanRequest;
            price = authoring.ConfiguredPrice;
        }

        return new ConfiguredSpawnableEntry(definition.DisplayName, definition.Description, definition.Prefab, canRequest, price);
    }

    private static string ResolveConfiguredUnitDisplayName(GameObject prefab, UnitGridAuthoring authoring)
    {
        if (prefab == null)
            return "Unit";

        if (authoring == null)
            return prefab.name;

        return authoring.ConfiguredDisplayName;
    }

    public bool BeginPlacementForConfiguredSpawnable(int index)
    {
        if (WarlineCaptureMissionRules.TryRejectBuildForActiveMission())
            return false;

        if (index < 0 || index >= _configuredSpawnableDefinitions.Count)
            return false;

        BuildingDefinition definition = _configuredSpawnableDefinitions[index];
        if (definition == null || definition.Prefab == null)
            return false;

        BeginPlacement(definition);
        return true;
    }

    public bool BeginPlacementForConfiguredSpawnable(GameObject prefab)
    {
        if (WarlineCaptureMissionRules.TryRejectBuildForActiveMission())
            return false;

        if (prefab == null || !_configuredDefinitionsByPrefab.TryGetValue(prefab, out BuildingDefinition definition) || definition == null)
            return false;

        BeginPlacement(definition);
        return true;
    }

    public bool IsConfiguredSpawnablePrefab(GameObject prefab)
    {
        return prefab != null && _configuredDefinitionsByPrefab.ContainsKey(prefab);
    }

    public bool ConfirmBuildingPlacement()
    {
        if (_activePlacement == null || !_activePlacement.IsValid)
            return false;

        if (IsLinearWallDefinition(_activePlacement.Definition) &&
            (!TryGetGridData(out _, out GridConfig grid, out DynamicBuffer<GridRoad> roads, out DynamicBlockerData blockerData) ||
             !AreAllPendingWallRunsValid(_activePlacement, grid, roads, blockerData)))
            return false;

        int placementCost = Mathf.Max(0, _activePlacementCost);
        if (placementCost > 0 && !TrySpendDollars(placementCost))
            return false;

        _activePlacement.OriginCell = _activePlacement.CommittedOriginCell;
        _activePlacementCost = 0;
        PlaceBuilding(_activePlacement);
        GameRuntimeStats.RecordBuildingBuilt();
        _mainMenuPlayUi?.NotifyStaticMinimapChanged();
        _preserveBuildingSelectionOnNextExitBuildMode = true;
        ExitBuildMode(clearBuildingSelection: false);
        return true;
    }

    public void CancelBuildingPlacement()
    {
        CancelPlacement();
        InitialUnitsRuntimeState.BuildModeActive = false;
        BattleHudGameplayBridge.ResolveActive()?.ClearCommandMode();
    }

    public void CreateUnitFromSelectedBuilding()
    {
        CreateUnitFromSelectedBuilding(0);
    }

    public void CreateUnitFromBuilding(int buildingId)
    {
        CreateUnitFromBuilding(buildingId, 0);
    }

    public void CreateSecondaryUnitFromSelectedBuilding()
    {
        CreateUnitFromSelectedBuilding(1);
    }

    public void CreateSecondaryUnitFromBuilding(int buildingId)
    {
        CreateUnitFromBuilding(buildingId, 1);
    }

    public void CreateTertiaryUnitFromSelectedBuilding()
    {
        CreateUnitFromSelectedBuilding(2);
    }

    public void CreateTertiaryUnitFromBuilding(int buildingId)
    {
        CreateUnitFromBuilding(buildingId, 2);
    }

    public void CreateQuaternaryUnitFromSelectedBuilding()
    {
        CreateUnitFromSelectedBuilding(3);
    }

    public void CreateQuaternaryUnitFromBuilding(int buildingId)
    {
        CreateUnitFromBuilding(buildingId, 3);
    }

    private enum CreateSlot
    {
        Primary,
        Secondary,
        Tertiary,
        Quaternary
    }

    public void CreateUnitFromSelectedBuilding(int productionIndex)
    {
        int? buildingId = ActiveBuildingId;
        if (!buildingId.HasValue)
            return;

        CreateUnitFromBuilding(buildingId.Value, productionIndex);
    }

    public void CreateUnitFromBuilding(int buildingId, int productionIndex)
    {
        bool armed = ConsumeUiProductionArm();
        if (!armed)
            return;

        if (!_runtimeBuildings.TryGetValue(buildingId, out RuntimeBuildingData building))
            return;

        GameObject spawnUnitPrefab = GetProductionPrefab(building.Definition, productionIndex);
        if (spawnUnitPrefab == null)
            return;

        if (!CanQueueUnitFromBuilding(building, spawnUnitPrefab, true))
            return;

        bool queued = TryQueuePlayerUnitFromBuilding(building, productionIndex, spawnUnitPrefab);
        if (!queued)
            Debug.LogWarning($"Unable to create a unit for the selected building '{building.Definition.DisplayName}'.");
    }

    public CampRequestFailure GetCampRequestFailure(GameObject prefab, int price, out string requiredBuildingDisplayName)
    {
        requiredBuildingDisplayName = string.Empty;
        if (prefab == null)
            return CampRequestFailure.InvalidSelection;

        int normalizedPrice = Mathf.Max(0, price);
        if (_resourceDollars < normalizedPrice)
            return CampRequestFailure.NotEnoughMoney;

        if (_configuredDefinitionsByPrefab.ContainsKey(prefab))
            return CampRequestFailure.None;

        if (TryFindFirstFriendlyProducerBuilding(prefab, out _, out _, out _))
            return CampRequestFailure.None;

        TryGetRequiredProducerDisplayName(prefab, out requiredBuildingDisplayName);
        return CampRequestFailure.MissingProducerBuilding;
    }

    public CampRequestFailure TryRequestCampItem(GameObject prefab, int price, out string requiredBuildingDisplayName)
    {
        return TryRequestCampItem(prefab, price, out requiredBuildingDisplayName, true);
    }

    public CampRequestFailure TryRequestCampItem(GameObject prefab, int price, out string requiredBuildingDisplayName, bool focusProducerOnSuccess)
    {
        CampRequestFailure failure = GetCampRequestFailure(prefab, price, out requiredBuildingDisplayName);
        if (failure != CampRequestFailure.None)
            return failure;

        if (_configuredDefinitionsByPrefab.ContainsKey(prefab))
        {
            if (!BeginPlacementForConfiguredSpawnable(prefab))
            {
                return CampRequestFailure.InvalidSelection;
            }

            _activePlacementCost = Mathf.Max(0, price);
            return CampRequestFailure.None;
        }

        if (!TryFindFirstFriendlyProducerBuilding(prefab, out int producerBuildingId, out int productionIndex, out _))
        {
            TryGetRequiredProducerDisplayName(prefab, out requiredBuildingDisplayName);
            return CampRequestFailure.MissingProducerBuilding;
        }

        if (!TrySpendDollars(price))
            return CampRequestFailure.NotEnoughMoney;

        if (!_runtimeBuildings.TryGetValue(producerBuildingId, out RuntimeBuildingData producerBuilding) || producerBuilding == null)
        {
            _resourceDollars += Mathf.Max(0, price);
            return CampRequestFailure.InvalidSelection;
        }

        if (focusProducerOnSuccess)
            SelectBuildingForProductionRequest(producerBuilding, prefab);
        else
            RememberCampProductionFocus(producerBuilding, prefab);
        ArmNextProductionFromUi();
        CreateUnitFromBuilding(producerBuildingId, productionIndex);
        GameRuntimeStats.RecordUnitOrdered(prefab);
        return CampRequestFailure.None;
    }

    public void FocusLastCampProductionRequest()
    {
        if (_lastCampProductionFocusBuilding == null || _lastCampProductionFocusPrefab == null)
            return;

        SelectBuildingForProductionRequest(_lastCampProductionFocusBuilding, _lastCampProductionFocusPrefab);
        _lastCampProductionFocusBuilding = null;
        _lastCampProductionFocusPrefab = null;
    }

    public void ArmNextProductionFromUi()
    {
        _armedProductionFrame = Time.frameCount;
    }

    private bool ConsumeUiProductionArm()
    {
        if (_armedProductionFrame != Time.frameCount)
            return false;

        _armedProductionFrame = -1;
        return true;
    }

    public void CreateSoldierFromSelectedBuilding()
    {
        CreateUnitFromSelectedBuilding();
    }

    public bool CanCreatePrimaryUnitFromSelectedBuilding()
    {
        return CanCreateUnitFromSelectedBuilding(0);
    }

    public bool CanCreateSecondaryUnitFromSelectedBuilding()
    {
        return CanCreateUnitFromSelectedBuilding(1);
    }

    public bool CanCreateTertiaryUnitFromSelectedBuilding()
    {
        return CanCreateUnitFromSelectedBuilding(2);
    }

    public bool CanCreateQuaternaryUnitFromSelectedBuilding()
    {
        return CanCreateUnitFromSelectedBuilding(3);
    }

    public bool CanCreateUnitFromSelectedBuilding(int productionIndex)
    {
        int? buildingId = ActiveBuildingId;
        if (!buildingId.HasValue || !_runtimeBuildings.TryGetValue(buildingId.Value, out RuntimeBuildingData building))
            return false;

        GameObject spawnUnitPrefab = GetProductionPrefab(building.Definition, productionIndex);

        return spawnUnitPrefab != null && CanQueueUnitFromBuilding(building, spawnUnitPrefab, false);
    }

    private bool CanQueueUnitFromBuilding(RuntimeBuildingData building, GameObject spawnUnitPrefab, bool logReason)
    {
        if (building == null || spawnUnitPrefab == null)
            return false;

        ResolveProductionTransportSettings(
            spawnUnitPrefab,
            out GameObject transportPrefab,
            out _,
            out _,
            out _,
            out ProductionTransportMode transportMode,
            out bool transportRequiresAirportRunway);

        if (transportPrefab == null)
            return true;

        if (transportRequiresAirportRunway &&
            transportMode == ProductionTransportMode.Plane &&
            !TryGetNearestAirportRunway(building.Instance != null ? building.Instance.transform.position : Vector3.zero, out _, out _, out _, out _))
        {
            if (logReason)
                Debug.LogWarning($"[BuildingSpawn] No airport runway is available for '{spawnUnitPrefab.name}'.");
            return false;
        }

        return true;
    }

    public void DeleteSelectedBuilding()
    {
        int? buildingId = ActiveBuildingId;
        if (!buildingId.HasValue)
            return;

        DeleteBuildingById(buildingId.Value);
    }

    public bool DeleteBuildingById(int buildingId)
    {
        if (!DeleteBuilding(buildingId, true))
            return false;

        if (_runtimeBuildingSystem.SelectedBuildingId == buildingId || _runtimeBuildingSystem.ActiveBuildingId == buildingId)
            ClearSelectedBuilding("DeleteBuildingById");

        return true;
    }

    public void ClearSelectedBuilding()
    {
        ClearSelectedBuilding("Unknown");
    }

    public void ClearSelectedBuilding(string reason)
    {
        _runtimeBuildingSystem.ClearSelection();
        RefreshBuildingMarkerVisibility();
    }

    public void ExitBuildMode()
    {
        ExitBuildMode(true);
    }

    private void ExitBuildMode(bool clearBuildingSelection)
    {
        bool shouldClearSelection = clearBuildingSelection && !_preserveBuildingSelectionOnNextExitBuildMode;
        InitialUnitsRuntimeState.BuildModeActive = false;
        _isDraggingPlacement = false;
        _ignorePointerUpdatesUntilRelease = false;
        CancelPlacement();
        if (shouldClearSelection)
            ClearSelectedBuilding("ExitBuildMode");
        _preserveBuildingSelectionOnNextExitBuildMode = false;
        HidePlacementOutline();
        BattleHudGameplayBridge.ResolveActive()?.ClearCommandMode();
    }

    public void NotifyPlacementUiPointerDown()
    {
        if (_activePlacement == null)
            return;

        _activePlacement.CommittedOriginCell = _activePlacement.OriginCell;
        _isDraggingPlacement = false;
        _ignorePointerUpdatesUntilRelease = true;
    }

    public void HandleRuntimeBuildingEntityDestroyed(int buildingId, Entity blockerEntity, GameObject buildingObject)
    {
        if (_runtimeBuildings.TryGetValue(buildingId, out RuntimeBuildingData destroyedBuilding) &&
            destroyedBuilding != null &&
            destroyedBuilding.IsDestroyed)
        {
            if (blockerEntity != Entity.Null && TryGetEntityManager(out EntityManager destroyedEm) && destroyedEm.Exists(blockerEntity))
                destroyedEm.DestroyEntity(blockerEntity);

            destroyedBuilding.CombatEntity = Entity.Null;
            destroyedBuilding.BlockerEntity = Entity.Null;
            return;
        }

        if (_runtimeBuildingSystem.SelectedBuildingId == buildingId || _runtimeBuildingSystem.ActiveBuildingId == buildingId)
            _runtimeBuildingSystem.ClearSelection();

        _citizenPopulationSystem?.NotifyHomeBuildingDestroyed(buildingId);
        if (EnableBuildingDestroyDiagnostics)
            Debug.Log($"[BuildingDestroyed] runtimeEntity buildingId={buildingId}");

        if (blockerEntity != Entity.Null && TryGetEntityManager(out EntityManager em) && em.Exists(blockerEntity))
            em.DestroyEntity(blockerEntity);

        _runtimeBuildingSystem.RemoveBuilding(buildingId);
        if (buildingObject != null)
            Destroy(buildingObject);
        RefreshBuildingMarkerVisibility();
    }

    private void CancelPlacement()
    {
        if (_activePlacement?.PreviewInstance != null)
            Destroy(_activePlacement.PreviewInstance);

        _activePlacement = null;
        _activePlacementCost = 0;
        _isDraggingPlacement = false;
        HidePlacementOutline();
    }

    private void BeginPlacement(BuildingDefinition definition)
    {
        if (WarlineCaptureMissionRules.TryRejectBuildForActiveMission())
            return;

        InitialUnitsRuntimeState.BuildModeActive = true;
        InitialUnitsRuntimeState.SelectionModeActive = false;
        BattleHudGameplayBridge.ResolveActive()?.ApplyCommandMode(TacticalCommandMode.Build);
        ClearSelectedBuilding("BeginPlacement");
        CancelPlacement();
        _activePlacementCost = 0;
        _isDraggingPlacement = false;
        _ignorePointerUpdatesUntilRelease = false;

        Vector2Int origin = GetCenterScreenPlacementOrigin(definition.FootprintCells);
        if (TryResolveInitialPlacementOrigin(definition, origin, out Vector2Int resolvedOrigin))
            origin = resolvedOrigin;

        _activePlacement = new PlacementState
        {
            Definition = definition,
            PreviewInstance = CreateBuildingVisualInstance(definition, _buildingRoot),
            OriginCell = origin,
            CommittedOriginCell = origin,
            DragStartOriginCell = origin,
            DragCurrentOriginCell = origin,
            DragFirstAxis = DragFirstAxis.None,
            AutoRotateVertical = false,
            CommittedWallRuns = new List<PlacementState.WallRun>(),
            HideCurrentWallPreview = false,
            LastPointerMovedAt = Time.time,
            LastPointerScreenPosition = GamePointerInput.TryGetPointerPosition(out Vector2 pointerPosition) ? pointerPosition : Vector2.zero
        };

        UpdatePlacementVisual(_activePlacement, false, default);

        if (_activePlacement != null &&
            TryGetGridData(out _, out GridConfig grid, out _, out _))
        {
            _selectionSystem?.SmoothMoveCameraGroundCenterTo(
                ResolveCurrentPlacementFocusWorldPosition(_activePlacement, grid));
        }
    }

    private void UpdatePlacement(Vector2 screenPosition)
    {
        if (_activePlacement == null)
            return;

        UpdatePlacementVisual(_activePlacement, _isDraggingPlacement && !_ignorePointerUpdatesUntilRelease, screenPosition);
    }

    private void UpdatePlacementVisual(PlacementState placement, bool updateCellFromPointer, Vector2 screenPosition)
    {
        if (placement == null || placement.PreviewInstance == null)
            return;

        if (!TryGetGridData(out _, out GridConfig grid, out DynamicBuffer<GridRoad> roads, out DynamicBlockerData blockerData))
        {
            placement.IsValid = false;
            HidePlacementOutline();
            return;
        }

        RTSSelectionSystem selectionSystem = _selectionSystem;

        if (updateCellFromPointer)
        {
            if ((screenPosition - placement.LastPointerScreenPosition).sqrMagnitude > 1f)
            {
                placement.LastPointerMovedAt = Time.time;
                placement.LastPointerScreenPosition = screenPosition;
            }

            bool pointerIdle = Time.time - placement.LastPointerMovedAt >= 1f;
            if (!pointerIdle && TryGetGridCell(screenPosition, grid, out Vector2Int hoveredCell))
            {
                Vector2Int newOrigin = CenterCellToOrigin(hoveredCell, placement.Definition.FootprintCells);
                placement.OriginCell = newOrigin;
                placement.CommittedOriginCell = placement.OriginCell;
                placement.DragCurrentOriginCell = placement.OriginCell;
                UpdateWallDragAxis(placement);
            }
        }

        bool shouldFollowCamera = Time.time - placement.LastPointerMovedAt >= 1f;

        if (IsLinearWallDefinition(placement.Definition))
        {
            List<Vector2Int> wallOrigins = placement.HideCurrentWallPreview
                ? new List<Vector2Int>()
                : BuildWallPlacementOrigins(placement);
            bool vertical = IsWallPlacementVertical(placement);
            placement.AutoRotateVertical = vertical;
            Vector2Int wallFootprint = GetWallSegmentFootprint(placement.Definition, vertical);
            placement.IsValid = placement.HideCurrentWallPreview
                ? AreAllPendingWallRunsValid(placement, grid, roads, blockerData)
                : AreWallPlacementOriginsValid(placement, wallOrigins, wallFootprint, vertical, grid, roads, blockerData);
            RebuildWallPlacementPreview(placement, wallOrigins, vertical, grid);
            UpdateWallPlacementOutline(GetAllWallPlacementOrigins(placement, wallOrigins), wallFootprint, grid, placement.IsValid);
            if (shouldFollowCamera)
                selectionSystem?.FollowCameraGroundCenterTo(ResolvePlacementFocusWorldPosition(placement, grid, wallOrigins, wallFootprint));
            return;
        }

        placement.AutoRotateVertical = ResolvePlacementRotateVertical(placement);
        Vector2Int placementFootprint = GetPlacementFootprint(placement.Definition, placement.AutoRotateVertical);
        placement.IsValid = IsPlacementValid(placement.OriginCell, placementFootprint, grid, roads, blockerData);
        PositionBuildingObject(placement.PreviewInstance, placement.OriginCell, placement.Definition, grid, placement.AutoRotateVertical);
        UpdatePlacementOutline(placement.OriginCell, placementFootprint, grid, placement.IsValid);
        if (shouldFollowCamera)
            selectionSystem?.FollowCameraGroundCenterTo(GetFootprintCenter(placement.OriginCell, placementFootprint, grid));
    }

    private Vector3 ResolvePlacementFocusWorldPosition(
        PlacementState placement,
        GridConfig grid,
        List<Vector2Int> currentWallOrigins,
        Vector2Int wallFootprint)
    {
        if (placement == null)
            return Vector3.zero;

        List<Vector2Int> allOrigins = GetAllWallPlacementOrigins(placement, currentWallOrigins);
        if (allOrigins == null || allOrigins.Count == 0)
            return GetFootprintCenter(placement.OriginCell, wallFootprint, grid);

        int minX = int.MaxValue;
        int minY = int.MaxValue;
        int maxX = int.MinValue;
        int maxY = int.MinValue;
        for (int i = 0; i < allOrigins.Count; i++)
        {
            Vector2Int origin = allOrigins[i];
            minX = Mathf.Min(minX, origin.x);
            minY = Mathf.Min(minY, origin.y);
            maxX = Mathf.Max(maxX, origin.x + wallFootprint.x);
            maxY = Mathf.Max(maxY, origin.y + wallFootprint.y);
        }

        return GetFootprintCenter(new Vector2Int(minX, minY), new Vector2Int(maxX - minX, maxY - minY), grid);
    }

    private Vector3 ResolveCurrentPlacementFocusWorldPosition(PlacementState placement, GridConfig grid)
    {
        if (placement == null)
            return Vector3.zero;

        if (IsLinearWallDefinition(placement.Definition))
        {
            bool vertical = IsWallPlacementVertical(placement);
            Vector2Int wallFootprint = GetWallSegmentFootprint(placement.Definition, vertical);
            return ResolvePlacementFocusWorldPosition(placement, grid, BuildWallPlacementOrigins(placement), wallFootprint);
        }

        bool rotateVertical = ResolvePlacementRotateVertical(placement);
        Vector2Int footprint = GetPlacementFootprint(placement.Definition, rotateVertical);
        return GetFootprintCenter(placement.OriginCell, footprint, grid);
    }

    private void PlaceBuilding(PlacementState placement)
    {
        if (IsLinearWallDefinition(placement.Definition) &&
            TryGetGridData(out _, out GridConfig grid, out _, out _))
        {
            RuntimeBuildingData lastBuilding = null;
            List<PlacementState.WallRun> wallRuns = BuildFinalWallRuns(placement);
            for (int runIndex = 0; runIndex < wallRuns.Count; runIndex++)
            {
                PlacementState.WallRun run = wallRuns[runIndex];
                Vector2Int wallFootprint = GetWallSegmentFootprint(placement.Definition, run.Vertical);
                for (int i = 0; i < run.Origins.Count; i++)
                {
                    GameObject instance = CreateBuildingVisualInstance(placement.Definition, _buildingRoot);
                    if (instance == null)
                        continue;

                    PositionBuildingObject(instance, run.Origins[i], placement.Definition, grid, run.Vertical);
                    BuildingDefinition segmentDefinition = CloneDefinitionWithFootprint(placement.Definition, wallFootprint);
                    lastBuilding = RegisterRuntimeBuilding(segmentDefinition, instance, run.Origins[i]);
                }
            }

            if (placement.PreviewInstance != null)
                Destroy(placement.PreviewInstance);
            placement.PreviewInstance = null;

            if (lastBuilding != null && ShouldAutoSelectAfterPlacement(lastBuilding.Definition))
                SelectAndFocusBuilding(lastBuilding);
            return;
        }

        if (TryGetGridData(out _, out GridConfig placementGrid, out _, out _))
            PositionBuildingObject(placement.PreviewInstance, placement.OriginCell, placement.Definition, placementGrid, placement.AutoRotateVertical);

        RuntimeBuildingData building = RegisterRuntimeBuilding(
            CloneDefinitionWithFootprint(placement.Definition, GetPlacementFootprint(placement.Definition, placement.AutoRotateVertical)),
            placement.PreviewInstance,
            placement.OriginCell);
        placement.PreviewInstance = null;
        if (ShouldAutoSelectAfterPlacement(building.Definition))
            SelectAndFocusBuilding(building);
    }

    private RuntimeBuildingData RegisterRuntimeBuilding(BuildingDefinition definition, GameObject instance, Vector2Int originCell, bool removeOverlappingBlockers = true)
    {
        int buildingId = _runtimeBuildingSystem.AllocateId();
        instance.name = $"{definition.DisplayName}_{buildingId}";

        RectInt occupiedRect = new(originCell, definition.FootprintCells);
        if (TryGetGridData(out _, out GridConfig grid, out _, out _))
            occupiedRect = GetEffectivePlacementRect(definition, originCell, grid);

        bool pathBlocking = ShouldRuntimeBuildingBlockPathing(definition);
        if (removeOverlappingBlockers && pathBlocking)
            _runtimeGridBlockerSystem?.RemoveBlockersOverlappingFootprint(originCell, definition.FootprintCells);
        Entity blockerEntity = pathBlocking ? CreateBlockerEntity(definition, originCell, definition.FootprintCells) : Entity.Null;
        Entity combatEntity = CreateBuildingCombatEntity(originCell, definition, 0, instance.transform.rotation);
        if (_deferRuntimeBuildingSideEffectsDepth > 0)
        {
            if (pathBlocking)
                _deferredRedirectFootprints.Add(occupiedRect);
            _pendingMarkerRefresh = true;
        }
        else if (pathBlocking)
        {
            RedirectUnitsAroundPlacedBuilding(occupiedRect);
        }

        var building = new RuntimeBuildingData
        {
            Id = buildingId,
            Definition = definition,
            Instance = instance,
            OriginCell = originCell,
            CombatEntity = combatEntity,
            BlockerEntity = blockerEntity,
            ProductionSpawnLocalPositions = definition.ProductionSpawnLocalPositions,
            ProducedUnits = new List<Entity>(),
            PendingProductions = new List<RuntimeBuildingData.PendingProduction>(),
            StoredOilBarrels = 0f,
            StoredFuelBarrels = 0f
        };
        if (building.ProductionSpawnLocalPositions != null && building.ProductionSpawnLocalPositions.Length > 0)
            building.ProducedUnitSlots = new Entity[building.ProductionSpawnLocalPositions.Length];

        InitializeBuildingVisuals(building);
        AttachRuntimeLink(building);
        _runtimeBuildingSystem.AddBuilding(building.Id, building);
        if (_deferRuntimeBuildingSideEffectsDepth > 0)
            _pendingMarkerRefresh = true;
        else
            RefreshBuildingMarkerVisibility();
        return building;
    }

    private void SelectAndFocusBuilding(RuntimeBuildingData building)
    {
        if (building == null)
            return;

        _runtimeBuildingSystem.SelectBuilding(building.Id);
        InitialUnitsRuntimeState.SuppressNextWorldClick = true;
        RefreshBuildingMarkerVisibility();
        _selectionSystem?.ClearFocusedUnit();

        Vector3 focusWorldPosition = ResolveBuildingFocusWorldPosition(building);
        _selectionSystem?.SmoothMoveCameraGroundCenterTo(focusWorldPosition);
    }

    private static bool ShouldAutoSelectAfterPlacement(BuildingDefinition definition)
    {
        if (definition == null)
            return false;

        if (definition.ProductionSlots != null)
        {
            for (int i = 0; i < definition.ProductionSlots.Count; i++)
            {
                if (definition.ProductionSlots[i]?.SpawnUnitPrefab != null)
                    return true;
            }
        }

        return definition.SpawnUnitPrefab != null ||
               definition.SecondarySpawnUnitPrefab != null ||
               definition.TertiarySpawnUnitPrefab != null ||
               definition.QuaternarySpawnUnitPrefab != null;
    }

    private Vector3 ResolveBuildingFocusWorldPosition(RuntimeBuildingData building)
    {
        if (building?.Instance == null)
            return Vector3.zero;

        if (building.Definition != null &&
            TryGetGridData(out _, out GridConfig grid, out _, out _))
            return GetFootprintCenter(building.OriginCell, building.Definition.FootprintCells, grid);

        Vector3 position = building.Instance.transform.position;
        position.y = 0f;
        return position;
    }

    public void SpawnInitialTestRoster(Vector2Int anchorCell)
    {
        if (TrySpawnInitialBuilding(_soldierBaseDefinition, anchorCell + new Vector2Int(-18, -10), out _))
        {
        }

        if (TrySpawnInitialBuilding(_soldierTentDefinition, anchorCell + new Vector2Int(-18, 16), out _))
        {
        }

        if (TrySpawnInitialBuilding(_factoryDefinition, anchorCell + new Vector2Int(18, -4), out _))
        {
        }
    }

    public bool TrySpawnRuntimeBuilding(
        GameObject prefab,
        Vector2Int preferredOrigin,
        out int buildingId,
        string fallbackDisplayName = "Building",
        string fallbackDescription = "Operational building.",
        Vector2Int? fallbackFootprint = null,
        int fallbackMaxHealth = 500,
        bool isCityGenerated = false,
        byte? ownerFactionId = null,
        bool rotateVertical = false)
    {
        return TrySpawnRuntimeBuilding(
            prefab,
            preferredOrigin,
            out buildingId,
            out _,
            out _,
            fallbackDisplayName,
            fallbackDescription,
            fallbackFootprint,
            fallbackMaxHealth,
            isCityGenerated,
            ownerFactionId,
            rotateVertical);
    }

    public int TrySpawnRuntimeWallRun(
        GameObject prefab,
        Vector2Int startOrigin,
        Vector2Int endOrigin,
        byte? ownerFactionId = null)
    {
        if (prefab == null)
            return 0;
        if (!TryGetGridData(out _, out GridConfig grid, out _, out _))
            return 0;

        BuildingDefinition definition = CreateRuntimeBuildingDefinition(
            prefab,
            prefab.name,
            "Defensive wall.",
            new Vector2Int(4, 1),
            500);
        definition.IsWall = true;
        if (!IsLinearWallDefinition(definition))
            return 0;

        bool vertical = Mathf.Abs(endOrigin.y - startOrigin.y) > Mathf.Abs(endOrigin.x - startOrigin.x);
        if (vertical)
            endOrigin.x = startOrigin.x;
        else
            endOrigin.y = startOrigin.y;

        Vector2Int wallFootprint = GetWallSegmentFootprint(definition, vertical);
        List<Vector2Int> origins = BuildWallRunOrigins(startOrigin, endOrigin, wallFootprint, vertical);
        int spawned = 0;
        for (int i = 0; i < origins.Count; i++)
        {
            Vector2Int origin = origins[i];
            if (!TryGetGridData(out _, out grid, out DynamicBuffer<GridRoad> currentRoads, out DynamicBlockerData currentBlockerData))
                break;

            if (!IsWallPlacementValid(origin, wallFootprint, vertical, grid, currentRoads, currentBlockerData))
                continue;

            GameObject instance = CreateBuildingVisualInstance(definition, _buildingRoot);
            if (instance == null)
                continue;

            PositionBuildingObject(instance, origin, definition, grid, vertical);
            RuntimeBuildingData building = RegisterRuntimeBuilding(CloneDefinitionWithFootprint(definition, wallFootprint), instance, origin);
            SetRuntimeBuildingOwnerFaction(building, ownerFactionId);
            spawned++;
        }

        return spawned;
    }

    public bool TryGetRuntimeWallSegmentFootprint(GameObject prefab, bool rotateVertical, out Vector2Int footprint)
    {
        footprint = default;
        if (prefab == null)
            return false;

        BuildingDefinition definition = CreateRuntimeBuildingDefinition(
            prefab,
            prefab.name,
            "Defensive wall.",
            new Vector2Int(4, 1),
            500);
        definition.IsWall = true;
        footprint = GetWallSegmentFootprint(definition, rotateVertical);
        return footprint.x > 0 && footprint.y > 0;
    }

    public bool TrySpawnRuntimeWallSegment(
        GameObject prefab,
        Vector2Int origin,
        bool rotateVertical,
        byte? ownerFactionId = null,
        bool allowExistingWallOverlap = false)
    {
        if (prefab == null)
            return false;
        if (!TryGetGridData(out _, out GridConfig grid, out DynamicBuffer<GridRoad> roads, out DynamicBlockerData blockerData))
            return false;

        BuildingDefinition definition = CreateRuntimeBuildingDefinition(
            prefab,
            prefab.name,
            "Defensive wall.",
            new Vector2Int(4, 1),
            500);
        definition.IsWall = true;
        if (!IsLinearWallDefinition(definition))
            return false;

        Vector2Int wallFootprint = GetWallSegmentFootprint(definition, rotateVertical);
        if (!IsWallPlacementValid(origin, wallFootprint, rotateVertical, grid, roads, blockerData, allowExistingWallOverlap))
            return false;

        GameObject instance = CreateBuildingVisualInstance(definition, _buildingRoot);
        if (instance == null)
            return false;

        PositionBuildingObject(instance, origin, definition, grid, rotateVertical);
        RuntimeBuildingData building = RegisterRuntimeBuilding(CloneDefinitionWithFootprint(definition, wallFootprint), instance, origin, removeOverlappingBlockers: !allowExistingWallOverlap);
        SetRuntimeBuildingOwnerFaction(building, ownerFactionId);
        return true;
    }

    public bool TrySpawnRuntimeBuilding(
        GameObject prefab,
        Vector2Int preferredOrigin,
        out int buildingId,
        out Vector2Int actualOrigin,
        out Vector2Int actualFootprint,
        string fallbackDisplayName = "Building",
        string fallbackDescription = "Operational building.",
        Vector2Int? fallbackFootprint = null,
        int fallbackMaxHealth = 500,
        bool isCityGenerated = false,
        byte? ownerFactionId = null,
        bool rotateVertical = false)
    {
        buildingId = 0;
        actualOrigin = default;
        actualFootprint = default;
        if (prefab == null)
            return false;

        BuildingDefinition definition = CreateRuntimeBuildingDefinition(
            prefab,
            fallbackDisplayName,
            fallbackDescription,
            fallbackFootprint ?? new Vector2Int(10, 10),
            fallbackMaxHealth);
        actualFootprint = GetPlacementFootprint(definition, rotateVertical);

        if (!TrySpawnInitialBuilding(definition, preferredOrigin, rotateVertical, out RuntimeBuildingData building))
            return false;

        building.IsCityGenerated = isCityGenerated;
        SetRuntimeBuildingOwnerFaction(building, ownerFactionId);
        buildingId = building.Id;
        actualOrigin = building.OriginCell;
        actualFootprint = building.Definition.FootprintCells;
        return true;
    }

    public bool TryGetRuntimeBuildingPlacementFootprint(GameObject prefab, bool rotateVertical, out Vector2Int footprint)
    {
        footprint = default;
        if (prefab == null)
            return false;

        BuildingDefinition definition = CreateRuntimeBuildingDefinition(
            prefab,
            prefab.name,
            "Operational building.",
            new Vector2Int(10, 10),
            500);
        footprint = GetPlacementFootprint(definition, rotateVertical);
        return footprint.x > 0 && footprint.y > 0;
    }

    public bool TryGetFactionProductionSpawnPoint(
        byte factionId,
        string buildingId,
        int flattenedSlotIndex,
        GridConfig grid,
        out int2 cell,
        out float3 worldPosition)
    {
        cell = default;
        worldPosition = default;
        if (string.IsNullOrWhiteSpace(buildingId))
            return false;

        int remainingSlotIndex = Mathf.Max(0, flattenedSlotIndex);
        string normalizedBuildingId = NormalizeSpawnableKey(buildingId);
        foreach (KeyValuePair<int, RuntimeBuildingData> entry in _runtimeBuildings)
        {
            RuntimeBuildingData building = entry.Value;
            if (building == null ||
                building.IsDestroyed ||
                !building.HasOwnerFaction ||
                building.OwnerFactionId != factionId ||
                building.Instance == null ||
                building.ProductionSpawnLocalPositions == null ||
                building.ProductionSpawnLocalPositions.Length == 0 ||
                !RuntimeBuildingMatchesId(building, normalizedBuildingId))
                continue;

            if (remainingSlotIndex >= building.ProductionSpawnLocalPositions.Length)
            {
                remainingSlotIndex -= building.ProductionSpawnLocalPositions.Length;
                continue;
            }

            Vector3 slotWorldPosition = building.Instance.transform.TransformPoint(building.ProductionSpawnLocalPositions[remainingSlotIndex]);
            cell = GridUtils.WorldToCell(grid, slotWorldPosition);
            worldPosition = slotWorldPosition;
            return GridUtils.InBounds(cell, grid.Width, grid.Height);
        }

        return false;
    }

    public bool TryResolveAvailableFactionHelipadSpawn(byte factionId, int2 unitFootprint, out int2 cell, out float3 worldPosition)
    {
        cell = default;
        worldPosition = default;
        if (!TryGetEntityManager(out EntityManager em))
            return false;
        if (!TryGetGridData(out Entity gridEntity, out GridConfig grid, out _, out DynamicBlockerData blockerData))
            return false;

        var walkable = em.GetBuffer<GridWalkable>(gridEntity).AsNativeArray();
        var occupied = em.GetComponentData<DynamicOccupancyData>(gridEntity).Occupied;
        var reserved = new NativeBitArray(grid.Width * grid.Height, Allocator.Temp);
        try
        {
            ReserveRecentSpawnBuffers(ref reserved, grid);
            _buildingSpawnRandomState = math.max(1u, _buildingSpawnRandomState + 1u);
            var rng = new Unity.Mathematics.Random(_buildingSpawnRandomState);
            return TryResolveHelicopterSpawnForFaction(
                factionId,
                null,
                em,
                ref rng,
                grid,
                walkable,
                blockerData.Blocked,
                occupied,
                ref reserved,
                unitFootprint,
                out cell,
                out worldPosition,
                out _,
                out _);
        }
        finally
        {
            if (reserved.IsCreated)
                reserved.Dispose();
        }
    }

    private static List<Vector2Int> BuildWallRunOrigins(Vector2Int start, Vector2Int end, Vector2Int footprint, bool vertical)
    {
        var origins = new List<Vector2Int> { start };
        if (start == end)
            return origins;

        if (vertical)
        {
            int stepCells = Mathf.Max(1, footprint.y);
            int delta = end.y - start.y;
            int direction = delta >= 0 ? 1 : -1;
            int segmentCount = Mathf.Abs(delta) / stepCells;
            for (int i = 1; i <= segmentCount; i++)
                origins.Add(new Vector2Int(start.x, start.y + direction * stepCells * i));
        }
        else
        {
            int stepCells = Mathf.Max(1, footprint.x);
            int delta = end.x - start.x;
            int direction = delta >= 0 ? 1 : -1;
            int segmentCount = Mathf.Abs(delta) / stepCells;
            for (int i = 1; i <= segmentCount; i++)
                origins.Add(new Vector2Int(start.x + direction * stepCells * i, start.y));
        }

        return origins;
    }

    private BuildingDefinition CreateRuntimeBuildingDefinition(
        GameObject prefab,
        string fallbackDisplayName,
        string fallbackDescription,
        Vector2Int fallbackFootprint,
        int fallbackMaxHealth)
    {
        CachedRuntimeBuildingMetadata metadata = GetOrCreateRuntimeBuildingMetadata(prefab);
        List<BuildingDefinition.ProductionSlotDefinition> productionSlots = BuildProductionSlots(metadata.Authoring, null, null, null, null);

        return new BuildingDefinition
        {
            DisplayName = metadata.Authoring != null && !string.IsNullOrWhiteSpace(metadata.Authoring.ConfiguredDisplayName) ? metadata.Authoring.ConfiguredDisplayName : fallbackDisplayName,
            Description = metadata.Authoring != null && !string.IsNullOrWhiteSpace(metadata.Authoring.ConfiguredDescription) ? metadata.Authoring.ConfiguredDescription : fallbackDescription,
            MaxHealth = metadata.Authoring != null ? Mathf.Max(1, metadata.Authoring.ConfiguredMaxHealth) : Mathf.Max(1, fallbackMaxHealth),
            ProductionSlots = productionSlots,
            SpawnUnitPrefab = GetProductionPrefab(productionSlots, 0),
            SecondarySpawnUnitPrefab = GetProductionPrefab(productionSlots, 1),
            TertiarySpawnUnitPrefab = GetProductionPrefab(productionSlots, 2),
            QuaternarySpawnUnitPrefab = GetProductionPrefab(productionSlots, 3),
            Prefab = prefab,
            FootprintCells = metadata.HasVisualFootprint
                ? metadata.VisualFootprint
                : metadata.Authoring != null
                    ? new Vector2Int(Mathf.Max(1, metadata.Authoring.ConfiguredFootprintCells.x), Mathf.Max(1, metadata.Authoring.ConfiguredFootprintCells.y))
                    : fallbackFootprint,
            Role = metadata.Authoring != null ? metadata.Authoring.ConfiguredRole : BuildingRole.None,
            IsWall = metadata.Authoring != null && metadata.Authoring.ConfiguredIsWall,
            OilBarrelsPerDay = metadata.Authoring != null ? Mathf.Max(0f, metadata.Authoring.ConfiguredOilBarrelsPerDay) : 0f,
            OilStorageCapacity = metadata.Authoring != null ? Mathf.Max(0, metadata.Authoring.ConfiguredOilStorageCapacity) : 0,
            FuelBarrelsPerDay = metadata.Authoring != null ? Mathf.Max(0f, metadata.Authoring.ConfiguredFuelBarrelsPerDay) : 0f,
            FuelStorageCapacity = metadata.Authoring != null ? Mathf.Max(0, metadata.Authoring.ConfiguredFuelStorageCapacity) : 0,
            RefugeeCapacity = metadata.Authoring != null ? Mathf.Max(0, metadata.Authoring.ConfiguredRefugeeCapacity) : 0,
            RefugeeUpkeepPerCitizenPerDay = metadata.Authoring != null ? Mathf.Max(0, metadata.Authoring.ConfiguredRefugeeUpkeepPerCitizenPerDay) : 0,
            LocalBounds = metadata.LocalBounds,
            HasLocalBounds = metadata.HasLocalBounds,
            ProductionSpawnLocalPositions = metadata.ProductionSpawnLocalPositions,
            HasRunway = metadata.HasRunway,
            RunwayLocalPosition = metadata.RunwayLocalPosition,
            RunwayLocalRotation = metadata.RunwayLocalRotation,
            RunwayHalfExtents = metadata.RunwayHalfExtents
        };
    }

    private CachedRuntimeBuildingMetadata GetOrCreateRuntimeBuildingMetadata(GameObject prefab)
    {
        if (prefab == null)
            return new CachedRuntimeBuildingMetadata();

        if (_runtimeBuildingMetadataCache.TryGetValue(prefab, out CachedRuntimeBuildingMetadata cached))
            return cached;

        cached = new CachedRuntimeBuildingMetadata
        {
            Authoring = prefab.GetComponent<BuildingDefinitionAuthoring>()
        };

        if (TryGetFootprintFromVisualBounds(prefab, out Vector2Int visualFootprint))
        {
            cached.HasVisualFootprint = true;
            cached.VisualFootprint = visualFootprint;
        }

        cached.HasLocalBounds = TryGetPrefabLocalBounds(prefab, out cached.LocalBounds);
        cached.HasRunway = TryGetRunwayLocalData(prefab, out cached.RunwayLocalPosition, out cached.RunwayLocalRotation, out cached.RunwayHalfExtents);
        cached.ProductionSpawnLocalPositions = FindProductionSpawnLocalPositions(prefab);
        _runtimeBuildingMetadataCache[prefab] = cached;
        return cached;
    }

    private bool TrySpawnInitialBuilding(
        BuildingDefinition definition,
        Vector2Int preferredOrigin,
        bool rotateVertical,
        out RuntimeBuildingData building)
    {
        building = null;
        if (definition == null || definition.Prefab == null)
            return false;

        if (!TryGetGridData(out _, out GridConfig grid, out DynamicBuffer<GridRoad> roads, out DynamicBlockerData blockerData))
            return false;

        if (!TryFindValidInitialBuildingOrigin(definition, preferredOrigin, rotateVertical, grid, roads, blockerData, out Vector2Int originCell))
            return false;

        GameObject instance = CreateBuildingVisualInstance(definition, _buildingRoot);
        if (instance == null)
            return false;

        PositionBuildingObject(instance, originCell, definition, grid, rotateVertical);
        building = RegisterRuntimeBuilding(CloneDefinitionWithFootprint(definition, GetPlacementFootprint(definition, rotateVertical)), instance, originCell);
        return true;
    }

    private bool TrySpawnInitialBuilding(
        BuildingDefinition definition,
        Vector2Int preferredOrigin,
        out RuntimeBuildingData building)
    {
        return TrySpawnInitialBuilding(definition, preferredOrigin, false, out building);
    }

    private bool TryFindValidInitialBuildingOrigin(
        BuildingDefinition definition,
        Vector2Int preferredOrigin,
        bool rotateVertical,
        GridConfig grid,
        DynamicBuffer<GridRoad> roads,
        DynamicBlockerData blockerData,
        out Vector2Int originCell)
    {
        originCell = default;
        Vector2Int clampedPreferred = new(
            Mathf.Clamp(preferredOrigin.x, 0, Mathf.Max(0, grid.Width - GetPlacementFootprint(definition, rotateVertical).x)),
            Mathf.Clamp(preferredOrigin.y, 0, Mathf.Max(0, grid.Height - GetPlacementFootprint(definition, rotateVertical).y)));

        Vector2Int placementFootprint = GetPlacementFootprint(definition, rotateVertical);
        RectInt preferredPlacementRect = GetEffectivePlacementRect(definition, clampedPreferred, grid, rotateVertical);
        int maxSearchRadius = Mathf.Max(
            24,
            Mathf.Min(
                160,
                Mathf.Max(
                    placementFootprint.x,
                    placementFootprint.y,
                    preferredPlacementRect.width,
                    preferredPlacementRect.height)));
        for (int radius = 0; radius <= maxSearchRadius; radius++)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    if (radius > 0 && Mathf.Abs(dx) != radius && Mathf.Abs(dy) != radius)
                        continue;

                    Vector2Int candidate = clampedPreferred + new Vector2Int(dx, dy);
                    RectInt candidateRect = GetEffectivePlacementRect(definition, candidate, grid, rotateVertical);
                    if (HasCachedInvalidCellInFootprint(candidateRect.position, candidateRect.size))
                        continue;
                    if (!IsPlacementValid(definition, candidate, placementFootprint, rotateVertical, grid, roads, blockerData))
                        continue;

                    originCell = candidate;
                    return true;
                }
            }
        }

        return false;
    }

    private bool TryResolveInitialPlacementOrigin(BuildingDefinition definition, Vector2Int preferredOrigin, out Vector2Int resolvedOrigin)
    {
        resolvedOrigin = preferredOrigin;
        if (definition == null)
            return false;
        if (!TryGetGridData(out _, out GridConfig grid, out DynamicBuffer<GridRoad> roads, out DynamicBlockerData blockerData))
            return false;

        bool rotateVertical = false;
        Vector2Int footprint = GetPlacementFootprint(definition, rotateVertical);
        Vector2Int clampedPreferred = new(
            Mathf.Clamp(preferredOrigin.x, 0, Mathf.Max(0, grid.Width - footprint.x)),
            Mathf.Clamp(preferredOrigin.y, 0, Mathf.Max(0, grid.Height - footprint.y)));

        if (IsPlacementValid(definition, clampedPreferred, footprint, rotateVertical, grid, roads, blockerData))
        {
            resolvedOrigin = clampedPreferred;
            return true;
        }

        int maxRadius = Mathf.Max(grid.Width, grid.Height);
        for (int radius = 1; radius <= maxRadius; radius++)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    if (Mathf.Abs(dx) != radius && Mathf.Abs(dy) != radius)
                        continue;

                    Vector2Int candidate = clampedPreferred + new Vector2Int(dx, dy);
                    candidate.x = Mathf.Clamp(candidate.x, 0, Mathf.Max(0, grid.Width - footprint.x));
                    candidate.y = Mathf.Clamp(candidate.y, 0, Mathf.Max(0, grid.Height - footprint.y));
                    if (!IsPlacementValid(definition, candidate, footprint, rotateVertical, grid, roads, blockerData))
                        continue;

                    resolvedOrigin = candidate;
                    return true;
                }
            }
        }

        for (int y = 0; y <= Mathf.Max(0, grid.Height - footprint.y); y++)
        {
            for (int x = 0; x <= Mathf.Max(0, grid.Width - footprint.x); x++)
            {
                Vector2Int candidate = new(x, y);
                if (!IsPlacementValid(definition, candidate, footprint, rotateVertical, grid, roads, blockerData))
                    continue;

                resolvedOrigin = candidate;
                return true;
            }
        }

        return false;
    }

    private bool DeleteBuilding(int buildingId, bool destroyVisual)
    {
        if (!_runtimeBuildings.TryGetValue(buildingId, out RuntimeBuildingData building))
            return false;

        if (destroyVisual && BeginDestroyedBuildingState(building))
            return true;

        if (TryGetEntityManager(out EntityManager em))
        {
            if (building.CombatEntity != Entity.Null && em.Exists(building.CombatEntity))
                em.DestroyEntity(building.CombatEntity);
            if (building.BlockerEntity != Entity.Null && em.Exists(building.BlockerEntity))
                em.DestroyEntity(building.BlockerEntity);
        }

        if (destroyVisual && building.Instance != null)
            Destroy(building.Instance);

        _runtimeBuildingSystem.RemoveBuilding(buildingId);
        RefreshBuildingMarkerVisibility();
        _mainMenuPlayUi?.NotifyStaticMinimapChanged();
        return true;
    }

    private void UpdateDestroyedBuildings()
    {
        List<int> cleanupIds = _buildingCombatSystem.CollectDestroyedCleanupIds(_runtimeBuildings, Time.time);
        if (cleanupIds == null)
            return;

        for (int i = 0; i < cleanupIds.Count; i++)
            FinalizeDestroyedBuilding(cleanupIds[i]);
    }

    private bool BeginDestroyedBuildingState(RuntimeBuildingData building)
    {
        if (!_buildingCombatSystem.TryMarkDestroyed(building, Time.time, DestroyedBuildingLifetimeSeconds))
            return false;

        _citizenPopulationSystem?.NotifyHomeBuildingDestroyed(building.Id);
        RememberOpenBaseBreach(building);
        DestroyRuntimeBuildingBlockerEntity(building);

        if (_runtimeBuildingSystem.SelectedBuildingId == building.Id || _runtimeBuildingSystem.ActiveBuildingId == building.Id)
            _runtimeBuildingSystem.ClearSelection();

        _buildingVisualSystem.SetTransformVisible(building.SelectionMarker, false);
        _buildingVisualSystem.SetTransformVisible(building.FactionMarker, false);
        if (building.AliveVisualRoots != null)
        {
            for (int i = 0; i < building.AliveVisualRoots.Length; i++)
                _buildingVisualSystem.SetTransformVisible(building.AliveVisualRoots[i], false);
        }
        _buildingVisualSystem.SetTransformVisible(building.DestroyedVisual, true);
        RefreshBuildingMarkerVisibility();
        return true;
    }

    private void SyncDestroyedRuntimeBuildingCombatEntities()
    {
        if (_runtimeBuildings.Count == 0 || !TryGetEntityManager(out EntityManager em))
            return;

        foreach (var entry in _runtimeBuildings)
        {
            RuntimeBuildingData building = entry.Value;
            if (building == null || building.IsDestroyed || building.CombatEntity == Entity.Null)
                continue;

            BuildingCombatSystem.RuntimeCombatState combatState = _buildingCombatSystem.ResolveRuntimeCombatState(building, em);
            if (combatState == BuildingCombatSystem.RuntimeCombatState.MissingCombatEntity)
            {
                BeginDestroyedBuildingState(building);
                building.CombatEntity = Entity.Null;
                continue;
            }

            if (combatState == BuildingCombatSystem.RuntimeCombatState.DeadCombatEntity)
                BeginDestroyedBuildingState(building);
        }
    }

#if UNITY_EDITOR
    public void SyncDestroyedRuntimeBuildingCombatEntitiesForTests()
    {
        SyncDestroyedRuntimeBuildingCombatEntities();
    }
#endif

    private void DestroyRuntimeBuildingBlockerEntity(RuntimeBuildingData building)
    {
        if (building == null)
            return;

        if (!TryGetEntityManager(out EntityManager em))
        {
            building.BlockerEntity = Entity.Null;
            return;
        }

        _buildingCombatSystem.DestroyBlockerEntity(building, em);
    }

    private void RememberOpenBaseBreach(RuntimeBuildingData building)
    {
        if (building?.Definition == null ||
            !building.HasOwnerFaction ||
            (!building.Definition.IsWall && !IsWallGateDefinition(building.Definition)))
        {
            return;
        }

        RectInt rect = new(building.OriginCell, building.Definition.FootprintCells);
        for (int i = 0; i < _openBaseBreaches.Count; i++)
        {
            RuntimeBaseBreach existing = _openBaseBreaches[i];
            if (existing.OwnerFactionId == building.OwnerFactionId && existing.Rect == rect)
                return;
        }

        _openBaseBreaches.Add(new RuntimeBaseBreach(building.OwnerFactionId, rect));
    }

    private bool HasOpenBaseBreach(byte ownerFactionId, RectInt perimeterRect)
    {
        for (int i = 0; i < _openBaseBreaches.Count; i++)
        {
            RuntimeBaseBreach breach = _openBaseBreaches[i];
            if (breach.OwnerFactionId != ownerFactionId)
                continue;
            if (!RectTouchesPerimeter(breach.Rect, perimeterRect))
                continue;
            if (HasActiveWallOrGateOverlapping(breach.Rect, ownerFactionId))
                continue;

            return true;
        }

        return false;
    }

    private bool HasActiveWallOrGateOverlapping(RectInt rect, byte ownerFactionId)
    {
        foreach (var entry in _runtimeBuildings)
        {
            RuntimeBuildingData building = entry.Value;
            if (building == null ||
                building.IsDestroyed ||
                building.Definition == null ||
                !building.HasOwnerFaction ||
                building.OwnerFactionId != ownerFactionId ||
                (!building.Definition.IsWall && !IsWallGateDefinition(building.Definition)))
            {
                continue;
            }

            RectInt buildingRect = new(building.OriginCell, building.Definition.FootprintCells);
            if (RectsOverlap(rect, buildingRect))
                return true;
        }

        return false;
    }

    private static bool RectTouchesPerimeter(RectInt rect, RectInt perimeterRect)
    {
        return RectsOverlap(rect, perimeterRect) ||
               (rect.xMin <= perimeterRect.xMin && rect.xMax > perimeterRect.xMin && rect.yMin < perimeterRect.yMax && rect.yMax > perimeterRect.yMin) ||
               (rect.xMin < perimeterRect.xMax && rect.xMax >= perimeterRect.xMax && rect.yMin < perimeterRect.yMax && rect.yMax > perimeterRect.yMin) ||
               (rect.yMin <= perimeterRect.yMin && rect.yMax > perimeterRect.yMin && rect.xMin < perimeterRect.xMax && rect.xMax > perimeterRect.xMin) ||
               (rect.yMin < perimeterRect.yMax && rect.yMax >= perimeterRect.yMax && rect.xMin < perimeterRect.xMax && rect.xMax > perimeterRect.xMin);
    }

    private static bool RectsOverlap(RectInt a, RectInt b)
    {
        return a.xMin < b.xMax &&
               a.xMax > b.xMin &&
               a.yMin < b.yMax &&
               a.yMax > b.yMin;
    }

    private void FinalizeDestroyedBuilding(int buildingId)
    {
        if (!_runtimeBuildings.TryGetValue(buildingId, out RuntimeBuildingData building))
            return;

        _citizenPopulationSystem?.NotifyHomeBuildingDestroyed(buildingId);

        if (TryGetEntityManager(out EntityManager em))
        {
            if (building.CombatEntity != Entity.Null && em.Exists(building.CombatEntity))
                em.DestroyEntity(building.CombatEntity);
            if (building.BlockerEntity != Entity.Null && em.Exists(building.BlockerEntity))
                em.DestroyEntity(building.BlockerEntity);
        }

        _runtimeBuildingSystem.RemoveBuilding(buildingId);
        if (building.Instance != null)
            Destroy(building.Instance);
        RefreshBuildingMarkerVisibility();
    }

    private void InitializeBuildingVisuals(RuntimeBuildingData building)
    {
        if (building?.Instance == null)
            return;

        Transform visualRoot = building.Instance.transform.childCount > 0
            ? building.Instance.transform.GetChild(0)
            : building.Instance.transform;

        building.FactionMarker = _buildingVisualSystem.FindDescendantByName(visualRoot, "FactionMarker");
        building.SelectionMarker = _buildingVisualSystem.FindDescendantByName(visualRoot, "SelectionMarker");
        building.DoorZ = _buildingVisualSystem.FindDescendantByName(visualRoot, "Door_Z");
        building.DestroyedVisual = _buildingVisualSystem.FindDescendantByName(visualRoot, "Destroyed");

        if (building.DoorZ != null)
        {
            building.DoorClosedLocalEulerZ = 0f;
            building.DoorOpenLocalEulerZ = NormalizeSignedAngle(building.DoorZ.localEulerAngles.z);
            building.DoorOpen01 = 0f;
            SetBarrierDoorOpen01(building, 0f);
        }

        if (building.FactionMarker != null)
            building.FactionMarkerRenderers = building.FactionMarker.GetComponentsInChildren<Renderer>(true);

        var aliveRoots = new List<Transform>();
        for (int i = 0; i < visualRoot.childCount; i++)
        {
            Transform child = visualRoot.GetChild(i);
            if (child == building.DestroyedVisual ||
                child == building.FactionMarker ||
                child == building.SelectionMarker)
                continue;
            aliveRoots.Add(child);
        }

        building.AliveVisualRoots = aliveRoots.ToArray();
        building.AnimatedParts = _buildingVisualSystem.FindAnimatedBuildingParts(visualRoot);

        Color factionColor = _factionVisualSettings != null
            ? _factionVisualSettings.GetColor(0)
            : new Color(0.12f, 0.72f, 1f, 1f);

        _buildingVisualSystem.ApplyMarkerColor(building.FactionMarkerRenderers, factionColor, _markerPropertyBlock);
        _buildingVisualSystem.SetTransformVisible(building.DestroyedVisual, false);
    }

    private void SetRuntimeBuildingOwnerFaction(RuntimeBuildingData building, byte? ownerFactionId)
    {
        if (building == null)
            return;

        building.HasOwnerFaction = ownerFactionId.HasValue;
        building.OwnerFactionId = ownerFactionId.GetValueOrDefault();
        UpdateRuntimeGateFriendlyPassFaction(building, ownerFactionId);
        if (building.CombatEntity != Entity.Null &&
            TryGetEntityManager(out EntityManager em) &&
            em.Exists(building.CombatEntity) &&
            em.HasComponent<Faction>(building.CombatEntity))
        {
            em.SetComponentData(building.CombatEntity, new Faction { Id = building.OwnerFactionId });
        }

        Color factionColor = _factionVisualSettings != null
            ? _factionVisualSettings.GetColor(building.OwnerFactionId)
            : building.OwnerFactionId == 0
                ? new Color(0.12f, 0.72f, 1f, 1f)
                : new Color(0.92f, 0.2f, 0.16f, 1f);
        _buildingVisualSystem.ApplyMarkerColor(building.FactionMarkerRenderers, factionColor, _markerPropertyBlock);
    }

    private void UpdateRuntimeGateFriendlyPassFaction(RuntimeBuildingData building, byte? ownerFactionId)
    {
        if (building?.Definition == null ||
            building.BlockerEntity == Entity.Null ||
            !IsWallGateDefinition(building.Definition) ||
            !TryGetEntityManager(out EntityManager em) ||
            !em.Exists(building.BlockerEntity))
            return;

        if (!ownerFactionId.HasValue)
        {
            if (em.HasComponent<FriendlyPassGridBlocker>(building.BlockerEntity))
                em.RemoveComponent<FriendlyPassGridBlocker>(building.BlockerEntity);
            return;
        }

        var pass = new FriendlyPassGridBlocker { AllowedFactionId = ownerFactionId.Value };
        if (em.HasComponent<FriendlyPassGridBlocker>(building.BlockerEntity))
            em.SetComponentData(building.BlockerEntity, pass);
        else
            em.AddComponentData(building.BlockerEntity, pass);
    }

    private bool TryFindRuntimeBuildingByCombatEntity(Entity combatEntity, out RuntimeBuildingData building)
    {
        building = null;
        if (combatEntity == Entity.Null)
            return false;

        foreach (var entry in _runtimeBuildings)
        {
            RuntimeBuildingData candidate = entry.Value;
            if (candidate == null || candidate.CombatEntity != combatEntity)
                continue;

            building = candidate;
            return true;
        }

        return false;
    }

    private bool TryFindEnemyWallPerimeterContainingCell(byte attackerFactionId, int2 targetCell, out byte breachedFactionId, out RectInt breachedPerimeter)
    {
        breachedFactionId = 0;
        breachedPerimeter = default;
        var perimeters = new Dictionary<byte, RectInt>();

        foreach (var entry in _runtimeBuildings)
        {
            RuntimeBuildingData building = entry.Value;
            if (building == null ||
                building.IsDestroyed ||
                building.Definition == null ||
                !building.HasOwnerFaction ||
                building.OwnerFactionId == attackerFactionId ||
                (!building.Definition.IsWall && !IsWallGateDefinition(building.Definition)))
                continue;

            RectInt rect = new(building.OriginCell, building.Definition.FootprintCells);
            if (perimeters.TryGetValue(building.OwnerFactionId, out RectInt existing))
                perimeters[building.OwnerFactionId] = UnionRects(existing, rect);
            else
                perimeters.Add(building.OwnerFactionId, rect);
        }

        int bestArea = int.MaxValue;
        foreach (var pair in perimeters)
        {
            RectInt rect = pair.Value;
            if (targetCell.x < rect.xMin ||
                targetCell.x >= rect.xMax ||
                targetCell.y < rect.yMin ||
                targetCell.y >= rect.yMax)
                continue;

            int area = Mathf.Max(1, rect.width) * Mathf.Max(1, rect.height);
            if (area >= bestArea)
                continue;

            bestArea = area;
            breachedFactionId = pair.Key;
            breachedPerimeter = rect;
        }

        return bestArea < int.MaxValue;
    }

    private static bool TryFindBreachApproachCell(
        in GridConfig grid,
        in NativeArray<GridWalkable> walkable,
        in NativeBitArray blocked,
        in NativeArray<byte> friendlyPassFactionIds,
        in NativeBitArray occupied,
        Vector2Int originCell,
        Vector2Int footprintCells,
        RectInt perimeterRect,
        int2 unitFootprint,
        int2 referenceCell,
        byte factionId,
        out int2 goal)
    {
        goal = default;
        RectInt breachRect = new(originCell, footprintCells);
        int2 outsideDirection = ResolvePerimeterOutsideDirection(breachRect, perimeterRect);
        if (outsideDirection.x == 0 && outsideDirection.y == 0)
            return false;

        int2 clampedUnitFootprint = UnitFootprintUtility.ClampSize(unitFootprint);
        int2 breachCenter = new(
            breachRect.xMin + Mathf.Max(1, breachRect.width) / 2,
            breachRect.yMin + Mathf.Max(1, breachRect.height) / 2);

        bool found = false;
        int bestScore = int.MaxValue;
        const int maxApproachDistance = 18;
        for (int distance = 1; distance <= maxApproachDistance; distance++)
        {
            int lateralPadding = math.min(6, distance + 2);
            if (outsideDirection.x != 0)
            {
                int x = outsideDirection.x < 0
                    ? breachRect.xMin - distance
                    : breachRect.xMax - 1 + distance;
                for (int y = breachRect.yMin - lateralPadding; y <= breachRect.yMax - 1 + lateralPadding; y++)
                    TryScoreBreachApproachCandidate(grid, walkable, blocked, friendlyPassFactionIds, occupied, perimeterRect, outsideDirection, clampedUnitFootprint, referenceCell, breachCenter, factionId, x, y, ref bestScore, ref goal, ref found);
            }
            else
            {
                int y = outsideDirection.y < 0
                    ? breachRect.yMin - distance
                    : breachRect.yMax - 1 + distance;
                for (int x = breachRect.xMin - lateralPadding; x <= breachRect.xMax - 1 + lateralPadding; x++)
                    TryScoreBreachApproachCandidate(grid, walkable, blocked, friendlyPassFactionIds, occupied, perimeterRect, outsideDirection, clampedUnitFootprint, referenceCell, breachCenter, factionId, x, y, ref bestScore, ref goal, ref found);
            }

            if (found)
                return true;
        }

        return false;
    }

    private static int2 ResolvePerimeterOutsideDirection(RectInt breachRect, RectInt perimeterRect)
    {
        float breachCenterX = breachRect.xMin + (Mathf.Max(1, breachRect.width) * 0.5f);
        float breachCenterY = breachRect.yMin + (Mathf.Max(1, breachRect.height) * 0.5f);
        int distLeft = Mathf.RoundToInt(Mathf.Abs(breachCenterX - perimeterRect.xMin));
        int distRight = Mathf.RoundToInt(Mathf.Abs(breachCenterX - (perimeterRect.xMax - 1)));
        int distBottom = Mathf.RoundToInt(Mathf.Abs(breachCenterY - perimeterRect.yMin));
        int distTop = Mathf.RoundToInt(Mathf.Abs(breachCenterY - (perimeterRect.yMax - 1)));
        int best = Mathf.Min(Mathf.Min(distLeft, distRight), Mathf.Min(distBottom, distTop));

        if (best == distLeft)
            return new int2(-1, 0);
        if (best == distRight)
            return new int2(1, 0);
        if (best == distBottom)
            return new int2(0, -1);
        return new int2(0, 1);
    }

    private static void TryScoreBreachApproachCandidate(
        in GridConfig grid,
        in NativeArray<GridWalkable> walkable,
        in NativeBitArray blocked,
        in NativeArray<byte> friendlyPassFactionIds,
        in NativeBitArray occupied,
        RectInt perimeterRect,
        int2 outsideDirection,
        int2 unitFootprint,
        int2 referenceCell,
        int2 breachCenter,
        byte factionId,
        int x,
        int y,
        ref int bestScore,
        ref int2 bestCell,
        ref bool found)
    {
        if ((uint)x >= (uint)grid.Width || (uint)y >= (uint)grid.Height)
            return;

        int2 candidate = new(x, y);
        if (!IsOutsidePerimeterOnSide(candidate, perimeterRect, outsideDirection))
            return;

        if (!UnitFootprintUtility.CanPlace(grid, walkable, blocked, friendlyPassFactionIds, occupied, candidate, unitFootprint, referenceCell, factionId))
            return;

        int referenceScore = math.abs(referenceCell.x - x) + math.abs(referenceCell.y - y);
        int breachScore = math.abs(breachCenter.x - x) + math.abs(breachCenter.y - y);
        int score = referenceScore + (breachScore * 2);
        if (found && score >= bestScore)
            return;

        bestScore = score;
        bestCell = candidate;
        found = true;
    }

    private static bool IsOutsidePerimeterOnSide(int2 cell, RectInt perimeterRect, int2 outsideDirection)
    {
        if (outsideDirection.x < 0)
            return cell.x < perimeterRect.xMin;
        if (outsideDirection.x > 0)
            return cell.x >= perimeterRect.xMax;
        if (outsideDirection.y < 0)
            return cell.y < perimeterRect.yMin;
        return cell.y >= perimeterRect.yMax;
    }

    private bool TryFindBreachBuilding(byte breachedFactionId, int2 attackerCell, bool preferGate, out RuntimeBuildingData breachBuilding, out string reason)
    {
        breachBuilding = null;
        reason = preferGate ? "Gate" : "Wall";
        int bestScore = int.MaxValue;

        foreach (var entry in _runtimeBuildings)
        {
            RuntimeBuildingData building = entry.Value;
            if (building == null ||
                building.IsDestroyed ||
                building.Definition == null ||
                !building.HasOwnerFaction ||
                building.OwnerFactionId != breachedFactionId ||
                building.CombatEntity == Entity.Null)
                continue;

            bool isGate = IsWallGateDefinition(building.Definition);
            bool isWall = building.Definition.IsWall;
            if (preferGate ? !isGate : (!isWall || isGate))
                continue;

            if (!TryGetEntityManager(out EntityManager em) ||
                !em.Exists(building.CombatEntity) ||
                !em.HasComponent<UnitHealth>(building.CombatEntity) ||
                em.GetComponentData<UnitHealth>(building.CombatEntity).Current <= 0)
                continue;

            int2 center = new(
                building.OriginCell.x + Mathf.Max(1, building.Definition.FootprintCells.x) / 2,
                building.OriginCell.y + Mathf.Max(1, building.Definition.FootprintCells.y) / 2);
            int2 delta = center - attackerCell;
            int score = delta.x * delta.x + delta.y * delta.y;
            if (score >= bestScore)
                continue;

            bestScore = score;
            breachBuilding = building;
        }

        return breachBuilding != null;
    }

    private void UpdateBuildingResourceVisuals()
    {
        if (_runtimeBuildings.Count == 0)
            return;

        float time = Time.time;
        foreach (var entry in _runtimeBuildings)
        {
            RuntimeBuildingData building = entry.Value;
            if (building == null || building.IsDestroyed || building.AnimatedParts == null || building.AnimatedParts.Length == 0 || building.Definition == null)
                continue;

            bool isProducingOil = building.Definition.OilStorageCapacity > 0 &&
                                  building.Definition.OilBarrelsPerDay > 0f &&
                                  building.StoredOilBarrels < building.Definition.OilStorageCapacity;
            bool isProducingFuel = building.Definition.FuelStorageCapacity > 0 &&
                                   building.Definition.FuelBarrelsPerDay > 0f &&
                                   building.StoredOilBarrels > 0f &&
                                   building.StoredFuelBarrels < building.Definition.FuelStorageCapacity;
            _buildingVisualSystem.UpdateAnimatedBuildingParts(building.AnimatedParts, isProducingOil || isProducingFuel, time);
        }
    }

    private void RefreshBuildingMarkerVisibility()
    {
        foreach (var entry in _runtimeBuildings)
        {
            RuntimeBuildingData building = entry.Value;
            bool selected = !building.IsDestroyed && ActiveBuildingId.HasValue && ActiveBuildingId.Value == entry.Key;
            _buildingVisualSystem.SetTransformVisible(building.SelectionMarker, selected);
            if (building.IsDestroyed)
                _buildingVisualSystem.SetTransformVisible(building.FactionMarker, false);
        }
    }

    private void UpdateRoadBarrierDoors(float deltaTime)
    {
        if (_runtimeBuildings.Count == 0)
            return;

        bool hasRoadGate = false;
        foreach (var entry in _runtimeBuildings)
        {
            if (IsActiveRoadGateBuilding(entry.Value))
            {
                hasRoadGate = true;
                break;
            }
        }
        if (!hasRoadGate)
            return;

        if (!TryGetEntityManager(out EntityManager em))
            return;

        EnsureEntityQueries(em);
        if (_liveFactionUnitsQuery.IsEmptyIgnoreFilter)
        {
            foreach (var entry in _runtimeBuildings)
            {
                RuntimeBuildingData building = entry.Value;
                if (IsActiveRoadGateBuilding(building))
                    UpdateRoadBarrierDoorVisual(building, false, deltaTime);
            }
            return;
        }

        using var factions = _liveFactionUnitsQuery.ToComponentDataArray<Faction>(Allocator.Temp);
        using var unitGrids = _liveFactionUnitsQuery.ToComponentDataArray<UnitGrid>(Allocator.Temp);
        using var footprints = _liveFactionUnitsQuery.ToComponentDataArray<UnitFootprint>(Allocator.Temp);

        foreach (var entry in _runtimeBuildings)
        {
            RuntimeBuildingData building = entry.Value;
            if (!IsActiveRoadGateBuilding(building))
                continue;

            bool shouldOpen = building.HasOwnerFaction &&
                HasNearbyFriendlyUnit(building, factions, unitGrids, footprints, building.OwnerFactionId);
            UpdateRoadBarrierDoorVisual(building, shouldOpen, deltaTime);
        }
    }

#if UNITY_EDITOR
    public void UpdateRoadBarrierDoorsForTests(float deltaTime)
    {
        UpdateRoadBarrierDoors(deltaTime);
    }

    public bool TryGetRuntimeBuildingDoorOpen01ForTests(int buildingId, out float open01)
    {
        open01 = 0f;
        if (!_runtimeBuildings.TryGetValue(buildingId, out RuntimeBuildingData building) || building == null)
            return false;

        open01 = building.DoorOpen01;
        return true;
    }

    public bool TryGetRuntimeBuildingEntitiesForTests(int buildingId, out Entity combatEntity, out Entity blockerEntity)
    {
        combatEntity = Entity.Null;
        blockerEntity = Entity.Null;
        if (!_runtimeBuildings.TryGetValue(buildingId, out RuntimeBuildingData building) || building == null)
            return false;

        combatEntity = building.CombatEntity;
        blockerEntity = building.BlockerEntity;
        return true;
    }

    public bool IsRuntimeBuildingDestroyedForTests(int buildingId)
    {
        return _runtimeBuildings.TryGetValue(buildingId, out RuntimeBuildingData building) &&
               building != null &&
               building.IsDestroyed;
    }

    public int GetRuntimeRoadBarrierGateRectsForTests(byte factionId, List<RectInt> rects, List<int> buildingIds = null)
    {
        rects?.Clear();
        buildingIds?.Clear();
        int count = 0;
        foreach (var entry in _runtimeBuildings)
        {
            RuntimeBuildingData building = entry.Value;
            if (!IsActiveRoadGateBuilding(building) ||
                !building.HasOwnerFaction ||
                building.OwnerFactionId != factionId)
            {
                continue;
            }

            count++;
            rects?.Add(new RectInt(building.OriginCell, building.Definition.FootprintCells));
            buildingIds?.Add(building.Id);
        }

        return count;
    }
#endif

    private static bool IsActiveRoadGateBuilding(RuntimeBuildingData building)
    {
        return building != null &&
               !building.IsDestroyed &&
               building.DoorZ != null &&
               IsWallGateDefinition(building.Definition);
    }

    private void UpdateRoadBarrierDoorVisual(RuntimeBuildingData building, bool shouldOpen, float deltaTime)
    {
        if (building == null || building.IsDestroyed || building.DoorZ == null)
            return;
        if (!IsWallGateDefinition(building.Definition))
            return;

        float target = shouldOpen ? 1f : 0f;
        building.DoorOpen01 = Mathf.MoveTowards(building.DoorOpen01, target, deltaTime * BarrierDoorOpenCloseSpeed);
        SetBarrierDoorOpen01(building, building.DoorOpen01);
    }

    private static void SetBarrierDoorOpen01(RuntimeBuildingData building, float open01)
    {
        if (building?.DoorZ == null)
            return;

        Vector3 localEuler = building.DoorZ.localEulerAngles;
        localEuler.z = Mathf.LerpAngle(building.DoorClosedLocalEulerZ, building.DoorOpenLocalEulerZ, Mathf.Clamp01(open01));
        building.DoorZ.localEulerAngles = localEuler;
    }

    private static bool HasNearbyFriendlyUnit(
        RuntimeBuildingData building,
        NativeArray<Faction> factions,
        NativeArray<UnitGrid> unitGrids,
        NativeArray<UnitFootprint> footprints,
        byte factionId)
    {
        if (building?.Definition == null)
            return false;

        Vector2Int origin = building.OriginCell;
        Vector2Int size = building.Definition.FootprintCells;
        int minX = origin.x - BarrierDoorDetectPaddingCells;
        int minY = origin.y - BarrierDoorDetectPaddingCells;
        int maxX = origin.x + size.x + BarrierDoorDetectPaddingCells;
        int maxY = origin.y + size.y + BarrierDoorDetectPaddingCells;

        int count = Mathf.Min(factions.Length, Mathf.Min(unitGrids.Length, footprints.Length));
        for (int i = 0; i < count; i++)
        {
            if (factions[i].Id != factionId)
                continue;

            int2 unitSize = UnitFootprintUtility.ClampSize(footprints[i].Size);
            int2 unitMin = UnitFootprintUtility.GetMinCell(unitGrids[i].Cell, unitSize);
            int2 unitMax = unitMin + unitSize;
            if (unitMin.x < maxX && unitMax.x > minX &&
                unitMin.y < maxY && unitMax.y > minY)
                return true;
        }

        return false;
    }

    private static float NormalizeSignedAngle(float angle)
    {
        angle %= 360f;
        if (angle > 180f)
            angle -= 360f;
        return angle;
    }

    private void RedirectUnitsAroundPlacedBuilding(RectInt footprintRect)
    {
        _deferredRedirectFootprints.Clear();
        _deferredRedirectFootprints.Add(footprintRect);
        RedirectUnitsAroundPlacedBuildings(_deferredRedirectFootprints);
        _deferredRedirectFootprints.Clear();
    }

    private void RedirectUnitsAroundPlacedBuildings(IReadOnlyList<RectInt> placedFootprints)
    {
        if (placedFootprints == null || placedFootprints.Count == 0)
            return;
        if (!TryGetEntityManager(out EntityManager em))
            return;
        if (!TryGetGridData(out Entity gridEntity, out GridConfig grid, out _, out DynamicBlockerData blockerData))
            return;

        var walkable = em.GetBuffer<GridWalkable>(gridEntity).AsNativeArray();
        var occupied = em.GetComponentData<DynamicOccupancyData>(gridEntity).Occupied;
        var reserved = new NativeBitArray(grid.Width * grid.Height, Allocator.Temp);
        var redirectUnits = new NativeList<Entity>(Allocator.Temp);
        var redirectGoals = new NativeList<int2>(Allocator.Temp);
        var overlapFlags = new NativeList<byte>(Allocator.Temp);
        EnsureEntityQueries(em);
        using var units = _redirectUnitsQuery.ToEntityArray(Allocator.Temp);

        try
        {
            for (int footprintIndex = 0; footprintIndex < placedFootprints.Count; footprintIndex++)
            {
                RectInt footprintRect = placedFootprints[footprintIndex];
                ReserveBuildingBuffer(ref reserved, grid, footprintRect.position, footprintRect.size, 0);
            }

            NativeArray<int2> pathPool = default;
            if (em.HasComponent<PathPoolData>(gridEntity))
                pathPool = em.GetComponentData<PathPoolData>(gridEntity).Cells.AsArray();

            for (int i = 0; i < units.Length; i++)
            {
                Entity unit = units[i];
                if (em.HasComponent<Prefab>(unit) || em.HasComponent<StaticGridBlocker>(unit))
                    continue;

                bool needsRedirect = false;
                RectInt matchedFootprint = default;
                int2 currentCell = em.GetComponentData<UnitGrid>(unit).Cell;
                for (int footprintIndex = 0; footprintIndex < placedFootprints.Count; footprintIndex++)
                {
                    RectInt footprintRect = placedFootprints[footprintIndex];
                    if (IsCellInsideFootprint(currentCell, footprintRect.position, footprintRect.size))
                    {
                        matchedFootprint = footprintRect;
                        needsRedirect = true;
                        break;
                    }

                    if (em.HasComponent<UnitTarget>(unit))
                    {
                        int2 targetCell = em.GetComponentData<UnitTarget>(unit).Cell;
                        if (IsCellInsideFootprint(targetCell, footprintRect.position, footprintRect.size))
                        {
                            matchedFootprint = footprintRect;
                            needsRedirect = true;
                            break;
                        }
                    }
                }

                if (!needsRedirect && pathPool.IsCreated && em.HasComponent<UnitPathFollow>(unit) && em.HasComponent<UnitPathRange>(unit))
                {
                    for (int footprintIndex = 0; footprintIndex < placedFootprints.Count; footprintIndex++)
                    {
                        RectInt footprintRect = placedFootprints[footprintIndex];
                        if (!DoesRemainingPathIntersectFootprint(em, unit, pathPool, footprintRect.position, footprintRect.size))
                            continue;

                        matchedFootprint = footprintRect;
                        needsRedirect = true;
                        break;
                    }
                }

                if (!needsRedirect)
                    continue;

                if (!TryFindNearestPerimeterCell(
                    grid,
                    walkable,
                    blockerData.Blocked,
                    occupied,
                    ref reserved,
                    matchedFootprint.position,
                    matchedFootprint.size,
                    currentCell,
                    out int2 goal))
                {
                    continue;
                }

                redirectUnits.Add(unit);
                redirectGoals.Add(goal);
                overlapFlags.Add((byte)(IsCellInsideFootprint(currentCell, matchedFootprint.position, matchedFootprint.size) ? 1 : 0));
            }

            for (int i = 0; i < redirectUnits.Length; i++)
            {
                Entity unit = redirectUnits[i];
                int2 goal = redirectGoals[i];
                bool wasInsideFootprint = overlapFlags[i] != 0;

                if (em.HasComponent<EngageTarget>(unit))
                    em.RemoveComponent<EngageTarget>(unit);
                if (em.HasComponent<UnitPathFollow>(unit))
                    em.RemoveComponent<UnitPathFollow>(unit);
                if (em.HasComponent<UnitPathRange>(unit))
                    em.RemoveComponent<UnitPathRange>(unit);
                if (em.HasComponent<AutoWanderMoveTag>(unit))
                    em.RemoveComponent<AutoWanderMoveTag>(unit);
                if (em.HasComponent<ManualMoveOrderTag>(unit))
                    em.RemoveComponent<ManualMoveOrderTag>(unit);

                if (wasInsideFootprint)
                {
                    float3 worldPosition = GridUtils.CellToWorldCenter(grid, goal);
                    em.SetComponentData(unit, new UnitGrid { Cell = goal });
                    if (em.HasComponent<LocalTransform>(unit))
                        em.SetComponentData(unit, LocalTransform.FromPosition(worldPosition));
                    if (em.HasComponent<UnitPrevWorldPos>(unit))
                        em.SetComponentData(unit, new UnitPrevWorldPos { Value = worldPosition });
                    if (em.HasComponent<UnitMoveVisualState>(unit))
                        em.SetComponentData(unit, new UnitMoveVisualState { IsMoving = 0, StillSeconds = 0f });
                }

                if (wasInsideFootprint)
                {
                    if (em.HasComponent<UnitTarget>(unit))
                        em.RemoveComponent<UnitTarget>(unit);
                    if (em.HasComponent<UnitPathRequest>(unit))
                        em.RemoveComponent<UnitPathRequest>(unit);
                }
                else
                {
                    if (em.HasComponent<UnitTarget>(unit))
                        em.SetComponentData(unit, new UnitTarget { Cell = goal });
                    else
                        em.AddComponentData(unit, new UnitTarget { Cell = goal });

                    if (em.HasComponent<UnitPathRequest>(unit))
                        em.SetComponentData(unit, new UnitPathRequest { Goal = goal });
                    else
                        em.AddComponentData(unit, new UnitPathRequest { Goal = goal });
                }
            }
        }
        finally
        {
            redirectUnits.Dispose();
            redirectGoals.Dispose();
            overlapFlags.Dispose();
            reserved.Dispose();
        }
    }

    private void HandleBuildingSelectionClick(Vector2 screenPosition)
    {
        if (UnitPathfindingSystem.HasPendingPathJob)
            return;

        if (!TryGetGridData(out _, out GridConfig grid, out _, out _))
            return;
        if (!TryGetGridCell(screenPosition, grid, out Vector2Int cell))
            return;

        RTSSelectionSystem selectionSystem = _selectionSystem;
        if (selectionSystem != null && selectionSystem.IsBoardablePlayerTransportClick(screenPosition))
            return;

        foreach (var entry in _runtimeBuildings)
        {
            Vector2Int min = entry.Value.OriginCell;
            Vector2Int size = entry.Value.Definition.FootprintCells;
            if (ShouldUseExpandedSelectionArea(entry.Value.Definition))
            {
                min -= Vector2Int.one;
                size += new Vector2Int(2, 2);
            }

            if (cell.x < min.x || cell.y < min.y || cell.x >= min.x + size.x || cell.y >= min.y + size.y)
                continue;

            if (TryAssignSelectedHaulerOrders(entry.Key))
            {
                InitialUnitsRuntimeState.SuppressNextWorldClick = true;
                selectionSystem?.ClearFocusedUnit();
                return;
            }

            if (selectionSystem != null && selectionSystem.TryIssueMoveOrderToBuilding(min, size))
            {
                InitialUnitsRuntimeState.SuppressNextWorldClick = true;
                ClearSelectedBuilding("MoveOrderToBuilding");
                return;
            }

            _runtimeBuildingSystem.SelectBuilding(entry.Key);
            InitialUnitsRuntimeState.SuppressNextWorldClick = true;
            selectionSystem?.ClearFocusedUnit();
            return;
        }
    }

    private bool TryAssignSelectedHaulerOrders(int clickedBuildingId)
    {
        if (!TryGetEntityManager(out EntityManager em))
            return false;
        if (!TryGetRuntimeBuilding(clickedBuildingId, out RuntimeBuildingData clickedBuilding))
            return false;

        EnsureEntityQueries(em);
        using var selected = _selectedUnitsQuery.ToEntityArray(Allocator.Temp);
        if (selected.Length == 0)
            return false;

        bool clickedIsOilSource = IsOilSourceBuilding(clickedBuilding);
        bool clickedIsFuelBuilding = IsFuelBuilding(clickedBuilding);
        bool clickedIsStorage = _factionResourceSystem.IsResourceStorageBuilding(clickedBuilding);
        if (!clickedIsOilSource && !clickedIsFuelBuilding && !clickedIsStorage)
            return false;

        RuntimeBuildingData source = clickedBuilding;
        RuntimeBuildingData destination = clickedBuilding;
        ResourceHaulKind resourceKind = ResourceHaulKind.Oil;
        if (clickedIsOilSource)
        {
            if (!TryFindNearestBuilding(clickedBuilding, IsFuelBuilding, out destination))
                return false;
            resourceKind = ResourceHaulKind.Oil;
        }
        else if (clickedIsFuelBuilding)
        {
            if (!TryFindNearestBuilding(clickedBuilding, IsOilSourceBuilding, out source))
                return false;
            destination = clickedBuilding;
            resourceKind = ResourceHaulKind.Oil;
        }
        else
        {
            destination = clickedBuilding;
            if (TryFindNearestBuilding(clickedBuilding, HasAvailableFuelForHauler, out source))
                resourceKind = ResourceHaulKind.Fuel;
            else if (TryFindNearestBuilding(clickedBuilding, IsOilSourceBuilding, out source))
                resourceKind = ResourceHaulKind.Oil;
            else
                return false;
        }

        bool assignedAny = false;
        for (int i = 0; i < selected.Length; i++)
        {
            Entity unit = selected[i];
            if (!em.Exists(unit) || !em.HasComponent<UnitResourceHauler>(unit) || em.HasComponent<UnitAirMovement>(unit))
                continue;

            if (!TryIssueHaulerMoveToBuilding(em, unit, source, out int2 sourceGoal))
                continue;

            UnitResourceHaulOrder order = new()
            {
                SourceBuildingId = source.Id,
                DestinationBuildingId = destination.Id,
                TargetCell = sourceGoal,
                ActionEndsAt = 0f,
                Phase = (byte)ResourceHaulPhase.ToSource,
                ResourceKind = (byte)resourceKind
            };

            if (em.HasComponent<UnitResourceHaulOrder>(unit))
                em.SetComponentData(unit, order);
            else
                em.AddComponentData(unit, order);

            assignedAny = true;
        }

        return assignedAny;
    }

    private bool TryFindNearestBuilding(RuntimeBuildingData originBuilding, System.Predicate<RuntimeBuildingData> predicate, out RuntimeBuildingData result)
    {
        result = null;
        if (originBuilding == null || predicate == null)
            return false;

        Vector3 origin = ResolveBuildingFocusWorldPosition(originBuilding);
        float bestDistanceSq = float.MaxValue;

        foreach (var pair in _runtimeBuildings)
        {
            RuntimeBuildingData candidate = pair.Value;
            if (candidate == null || candidate == originBuilding || candidate.IsDestroyed || !predicate(candidate))
                continue;

            Vector3 candidatePosition = ResolveBuildingFocusWorldPosition(candidate);
            float distanceSq = (candidatePosition - origin).sqrMagnitude;
            if (distanceSq >= bestDistanceSq)
                continue;

            bestDistanceSq = distanceSq;
            result = candidate;
        }

        return result != null;
    }

    private bool TryIssueHaulerMoveToBuilding(EntityManager em, Entity unit, RuntimeBuildingData building, out int2 goal)
    {
        goal = default;
        if (building == null || building.IsDestroyed || !em.Exists(unit) || !TryGetGridData(out Entity gridEntity, out GridConfig grid, out _, out DynamicBlockerData blockerData))
            return false;

        var walkable = em.GetBuffer<GridWalkable>(gridEntity).AsNativeArray();
        var occupied = em.GetComponentData<DynamicOccupancyData>(gridEntity).Occupied;
        int2 referenceCell = em.GetComponentData<UnitGrid>(unit).Cell;
        int2 unitFootprint = em.HasComponent<UnitFootprint>(unit)
            ? em.GetComponentData<UnitFootprint>(unit).Size
            : new int2(1, 1);
        if (!TryFindBuildingApproachCell(grid, walkable, blockerData.Blocked, occupied, building.OriginCell, building.Definition.FootprintCells, unitFootprint, referenceCell, out goal))
            return false;

        if (em.HasComponent<EngageTarget>(unit))
            em.RemoveComponent<EngageTarget>(unit);
        if (em.HasComponent<UnitPathFollow>(unit))
            em.RemoveComponent<UnitPathFollow>(unit);
        if (em.HasComponent<UnitPathRange>(unit))
            em.RemoveComponent<UnitPathRange>(unit);
        if (em.HasComponent<AutoWanderMoveTag>(unit))
            em.RemoveComponent<AutoWanderMoveTag>(unit);

        if (em.HasComponent<UnitTarget>(unit))
            em.SetComponentData(unit, new UnitTarget { Cell = goal });
        else
            em.AddComponentData(unit, new UnitTarget { Cell = goal });

        if (em.HasComponent<UnitPathRequest>(unit))
            em.SetComponentData(unit, new UnitPathRequest { Goal = goal });
        else
            em.AddComponentData(unit, new UnitPathRequest { Goal = goal });

        if (!em.HasComponent<ManualMoveOrderTag>(unit))
            em.AddComponent<ManualMoveOrderTag>(unit);

        return true;
    }

    private bool TryGetRuntimeBuilding(int id, out RuntimeBuildingData building)
    {
        if (_runtimeBuildingSystem.TryGetBuilding(id, out building) && building != null && !building.IsDestroyed)
            return true;

        building = null;
        return false;
    }

    private static bool HasGoalOrPathRequest(EntityManager em, Entity entity, int2 goal)
    {
        bool sameTarget = em.HasComponent<UnitTarget>(entity) && em.GetComponentData<UnitTarget>(entity).Cell.Equals(goal);
        bool sameRequest = em.HasComponent<UnitPathRequest>(entity) && em.GetComponentData<UnitPathRequest>(entity).Goal.Equals(goal);
        return sameTarget || sameRequest;
    }

    private static bool IsOilSourceBuilding(RuntimeBuildingData building)
    {
        return building != null &&
               building.Definition != null &&
               building.Definition.OilBarrelsPerDay > 0f &&
               building.Definition.OilStorageCapacity > 0;
    }

    private static bool IsFuelBuilding(RuntimeBuildingData building)
    {
        return building != null &&
               building.Definition != null &&
               building.Definition.FuelBarrelsPerDay > 0f;
    }

    private static bool IsFuelStorageSourceBuilding(RuntimeBuildingData building)
    {
        return building != null &&
               building.Definition != null &&
               building.Definition.FuelBarrelsPerDay > 0f &&
               building.Definition.FuelStorageCapacity > 0;
    }

    private static bool HasAvailableFuelForHauler(RuntimeBuildingData building)
    {
        return IsFuelStorageSourceBuilding(building) &&
               building.StoredFuelBarrels >= 1f;
    }

    private static float GetOilReceivingFreeCapacity(RuntimeBuildingData building)
    {
        if (building == null || building.Definition == null)
            return 0f;

        if (building.Definition.OilStorageCapacity > 0)
            return Mathf.Max(0f, building.Definition.OilStorageCapacity - building.StoredOilBarrels);

        if (building.Definition.FuelBarrelsPerDay > 0f)
            return float.MaxValue;

        return 0f;
    }

    private static float GetFuelReceivingFreeCapacity(RuntimeBuildingData building)
    {
        if (building == null || building.Definition == null)
            return 0f;

        if (building.Definition.FuelStorageCapacity > 0)
            return Mathf.Max(0f, building.Definition.FuelStorageCapacity - building.StoredFuelBarrels);

        return 0f;
    }

    private static bool TryFindBuildingApproachCell(
        in GridConfig grid,
        in NativeArray<GridWalkable> walkable,
        in NativeBitArray blocked,
        in NativeBitArray occupied,
        Vector2Int originCell,
        Vector2Int footprintCells,
        int2 unitFootprint,
        int2 referenceCell,
        out int2 goal)
    {
        goal = default;
        int maxRadius = math.max(grid.Width, grid.Height);
        int bestScore = int.MaxValue;
        bool found = false;
        RectInt buildingRect = new(originCell, footprintCells);
        int2 clampedUnitFootprint = UnitFootprintUtility.ClampSize(unitFootprint);

        for (int extraRadius = 1; extraRadius <= maxRadius; extraRadius++)
        {
            int minX = originCell.x - extraRadius;
            int minY = originCell.y - extraRadius;
            int maxX = originCell.x + footprintCells.x - 1 + extraRadius;
            int maxY = originCell.y + footprintCells.y - 1 + extraRadius;

            for (int x = minX; x <= maxX; x++)
            {
                TryScoreBuildingApproachCandidate(grid, walkable, blocked, occupied, buildingRect, clampedUnitFootprint, referenceCell, x, minY, ref bestScore, ref goal, ref found);
                if (maxY != minY)
                    TryScoreBuildingApproachCandidate(grid, walkable, blocked, occupied, buildingRect, clampedUnitFootprint, referenceCell, x, maxY, ref bestScore, ref goal, ref found);
            }

            for (int y = minY + 1; y < maxY; y++)
            {
                TryScoreBuildingApproachCandidate(grid, walkable, blocked, occupied, buildingRect, clampedUnitFootprint, referenceCell, minX, y, ref bestScore, ref goal, ref found);
                if (maxX != minX)
                    TryScoreBuildingApproachCandidate(grid, walkable, blocked, occupied, buildingRect, clampedUnitFootprint, referenceCell, maxX, y, ref bestScore, ref goal, ref found);
            }

            if (found)
                return true;
        }

        return false;
    }

    private static void TryScoreBuildingApproachCandidate(
        in GridConfig grid,
        in NativeArray<GridWalkable> walkable,
        in NativeBitArray blocked,
        in NativeBitArray occupied,
        RectInt buildingRect,
        int2 unitFootprint,
        int2 referenceCell,
        int x,
        int y,
        ref int bestScore,
        ref int2 bestCell,
        ref bool found)
    {
        if ((uint)x >= (uint)grid.Width || (uint)y >= (uint)grid.Height)
            return;

        int2 candidateCell = new(x, y);
        int2 candidateMin = UnitFootprintUtility.GetMinCell(candidateCell, unitFootprint);
        RectInt unitRect = new(candidateMin.x, candidateMin.y, unitFootprint.x, unitFootprint.y);
        if (unitRect.Overlaps(buildingRect))
            return;

        if (!UnitFootprintUtility.CanPlace(grid, walkable, blocked, default, occupied, candidateCell, unitFootprint, referenceCell, 0))
            return;

        int score = math.abs(referenceCell.x - x) + math.abs(referenceCell.y - y);
        if (!found || score < bestScore)
        {
            bestScore = score;
            bestCell = candidateCell;
            found = true;
        }
    }

    private static bool ShouldUseExpandedSelectionArea(BuildingDefinition definition)
    {
        if (definition == null)
            return false;

        if (IsLinearWallDefinition(definition))
            return true;

        string displayName = definition.DisplayName ?? string.Empty;
        string prefabName = definition.Prefab != null ? definition.Prefab.name : string.Empty;
        return displayName.IndexOf("Road_Barrier", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
               prefabName.IndexOf("Road_Barrier", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private void CreatePlacementOutline()
    {
        _placementOutline = new GameObject("PlacementOutline");
        _placementOutline.transform.SetParent(_runtimeRoot, false);
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = "PlacementVolume";
        box.transform.SetParent(_placementOutline.transform, false);
        var collider = box.GetComponent<Collider>();
        if (collider != null)
            DestroyRuntimeObject(collider);
        

        _placementOutlineRenderer = box.GetComponent<MeshRenderer>();
        _placementOutlineRenderer.shadowCastingMode = ShadowCastingMode.Off;
        _placementOutlineRenderer.receiveShadows = false;
        _placementOutlineRenderer.sharedMaterial = CreatePlacementMaterial();

        ApplyPlacementMaterialColor(placementValidColor);
        _placementOutline.SetActive(false);
    }

    private void UpdatePlacementOutline(Vector2Int originCell, Vector2Int footprintCells, GridConfig grid, bool valid)
    {
        if (_placementOutline == null || _placementOutlineRenderer == null)
            return;

        float width = footprintCells.x * grid.CellSize;
        float depth = footprintCells.y * grid.CellSize;
        float height = GetPlacementOutlineHeight();
        Vector3 center = GetFootprintCenter(originCell, footprintCells, grid) + new Vector3(0f, height * 0.5f, 0f);

        _placementOutline.transform.SetPositionAndRotation(center, Quaternion.identity);
        _placementOutlineRenderer.transform.localPosition = Vector3.zero;
        _placementOutlineRenderer.transform.localScale = new Vector3(
            Mathf.Max(grid.CellSize, width),
            height,
            Mathf.Max(grid.CellSize, depth));

        ApplyPlacementMaterialColor(valid ? placementValidColor : placementInvalidColor);
        _placementOutline.SetActive(true);
    }

    private void HidePlacementOutline()
    {
        if (_placementOutline != null && _placementOutline.activeSelf)
            _placementOutline.SetActive(false);
    }

    private Material CreatePlacementMaterial()
    {
        Shader shader =
            Shader.Find("Universal Render Pipeline/Unlit") ??
            Shader.Find("Universal Render Pipeline/Simple Lit") ??
            Shader.Find("Universal Render Pipeline/Lit") ??
            Shader.Find("Sprites/Default") ??
            Shader.Find("Unlit/Color") ??
            Shader.Find("Standard");
        var material = new Material(shader);
        material.renderQueue = (int)RenderQueue.Transparent;
        material.SetOverrideTag("RenderType", "Transparent");
        if (material.HasProperty("_Surface"))
            material.SetFloat("_Surface", 1f);
        if (material.HasProperty("_Blend"))
            material.SetFloat("_Blend", 0f);
        if (material.HasProperty("_SrcBlend"))
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        if (material.HasProperty("_DstBlend"))
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        if (material.HasProperty("_ZWrite"))
            material.SetFloat("_ZWrite", 0f);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        return material;
    }

    private void ApplyPlacementMaterialColor(Color color)
    {
        if (_placementOutlineRenderer == null)
            return;

        Color c = color;
        c.a = 0.28f;
        Material material = _placementOutlineRenderer.sharedMaterial;
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", c);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", c);
    }

    private void CacheBuildingBounds(BuildingDefinition definition)
    {
        if (definition == null || definition.HasLocalBounds || (definition.VisualTemplate == null && definition.Prefab == null))
            return;

        GameObject temp = definition.VisualTemplate != null
            ? Object.Instantiate(definition.VisualTemplate)
            : Object.Instantiate(definition.Prefab);
        temp.hideFlags = HideFlags.HideAndDontSave;
        if (TryGetLocalBounds(temp, out Bounds localBounds))
        {
            definition.LocalBounds = localBounds;
            definition.HasLocalBounds = true;
        }

        DestroyRuntimeObject(temp);
    }

    private static void CleanupCombinedVisualTemplate(BuildingDefinition definition)
    {
        if (definition == null)
            return;

        if (definition.VisualTemplate != null)
            DestroyRuntimeObject(definition.VisualTemplate);

        if (definition.GeneratedMeshes != null)
        {
            for (int i = 0; i < definition.GeneratedMeshes.Count; i++)
            {
                Mesh mesh = definition.GeneratedMeshes[i];
                if (mesh != null)
                    DestroyRuntimeObject(mesh);
            }
        }

        definition.VisualTemplate = null;
        definition.GeneratedMeshes = null;
    }

    private static GameObject CreateBuildingVisualInstance(BuildingDefinition definition, Transform parent)
    {
        if (definition == null)
            return null;

        var wrapper = new GameObject($"{definition.DisplayName}_VisualRoot");
        wrapper.transform.SetParent(parent, false);
        wrapper.transform.localPosition = Vector3.zero;
        wrapper.transform.localRotation = Quaternion.identity;
        wrapper.transform.localScale = Vector3.one;

        GameObject visual = null;
        if (definition.Prefab != null)
        {
            Transform combinedMesh = definition.Prefab.transform.Find("CombinedMesh");
            if (combinedMesh != null)
                visual = Object.Instantiate(combinedMesh.gameObject, wrapper.transform);
            else
                visual = Object.Instantiate(definition.Prefab, wrapper.transform);
        }

        if (visual != null)
        {
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;
        }

        return wrapper;
    }

    private static void BuildCombinedVisualTemplate(BuildingDefinition definition)
    {
        // Building visuals are already authored/baked in the prefab asset.
        // Avoid any extra runtime combine step here.
        if (definition == null)
            return;

        definition.VisualTemplate = null;
    }

    private static bool TryGetLocalBounds(GameObject target, out Bounds bounds)
    {
        bounds = default;
        var renderers = target.GetComponentsInChildren<Renderer>(true);
        bool hasBounds = false;
        Matrix4x4 worldToLocal = target.transform.worldToLocalMatrix;
        foreach (Renderer renderer in renderers)
        {
            Bounds rendererBounds = renderer.bounds;
            Vector3 min = rendererBounds.min;
            Vector3 max = rendererBounds.max;
            for (int x = 0; x < 2; x++)
            {
                for (int y = 0; y < 2; y++)
                {
                    for (int z = 0; z < 2; z++)
                    {
                        Vector3 corner = new(
                            x == 0 ? min.x : max.x,
                            y == 0 ? min.y : max.y,
                            z == 0 ? min.z : max.z);
                        Vector3 localCorner = worldToLocal.MultiplyPoint3x4(corner);
                        if (!hasBounds)
                        {
                            bounds = new Bounds(localCorner, Vector3.zero);
                            hasBounds = true;
                        }
                        else
                        {
                            bounds.Encapsulate(localCorner);
                        }
                    }
                }
            }
        }

        return hasBounds;
    }

    private void PositionBuildingObject(GameObject instance, Vector2Int originCell, BuildingDefinition definition, GridConfig grid, bool rotateVertical = false)
    {
        if (instance == null)
            return;

        if (!rotateVertical && ShouldAlignGateToNearbyWall(definition) && TryResolveNearbyWallVertical(originCell, definition, out bool gateVertical))
            rotateVertical = gateVertical;

        Vector2Int footprintCells = GetPlacementFootprint(definition, rotateVertical);
        Vector3 center = GetFootprintCenter(originCell, footprintCells, grid);
        Vector3 offset = Vector3.zero;
        if (definition.HasLocalBounds)
            offset = new Vector3(definition.LocalBounds.center.x, 0f, definition.LocalBounds.center.z);

        Quaternion worldRotation = ResolvePlacementWorldRotation(definition, rotateVertical);
        instance.transform.SetPositionAndRotation(center, worldRotation);
        instance.transform.localScale = Vector3.one;

        if (instance.transform.childCount > 0)
        {
            Transform visualRoot = instance.transform.GetChild(0);
            visualRoot.localPosition = -offset;
            visualRoot.localRotation = Quaternion.identity;
            visualRoot.localScale = Vector3.one;
        }
    }

    private static bool IsLinearWallDefinition(BuildingDefinition definition)
    {
        return definition != null && definition.IsWall;
    }

    private static bool IsWallGateDefinition(BuildingDefinition definition)
    {
        if (definition == null)
            return false;

        string displayName = definition.DisplayName ?? string.Empty;
        string prefabName = definition.Prefab != null ? definition.Prefab.name : string.Empty;
        return displayName.IndexOf("Road_Barrier", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
               prefabName.IndexOf("Road_Barrier", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool ShouldAlignGateToNearbyWall(BuildingDefinition definition)
    {
        return IsWallGateDefinition(definition);
    }

    private static Quaternion ResolvePlacementWorldRotation(BuildingDefinition definition, bool rotateVertical)
    {
        bool rotateNinety = rotateVertical;
        if (IsLinearWallDefinition(definition) && IsWallLengthAxisLocalZ(definition))
            rotateNinety = !rotateNinety;

        return rotateNinety ? Quaternion.Euler(0f, 90f, 0f) : Quaternion.identity;
    }

    private static bool IsWallLengthAxisLocalZ(BuildingDefinition definition)
    {
        if (definition == null || !definition.HasLocalBounds)
            return false;

        return Mathf.Abs(definition.LocalBounds.size.z) > Mathf.Abs(definition.LocalBounds.size.x);
    }

    private bool TryResolveNearbyWallVertical(Vector2Int originCell, BuildingDefinition definition, out bool vertical)
    {
        vertical = false;
        if (definition == null || _runtimeBuildings == null || _runtimeBuildings.Count == 0)
            return false;

        RectInt gateRect = new(originCell, definition.FootprintCells);
        int bestDistance = int.MaxValue;
        bool found = false;

        foreach (var entry in _runtimeBuildings)
        {
            RuntimeBuildingData building = entry.Value;
            if (building?.Definition == null || !IsLinearWallDefinition(building.Definition))
                continue;

            Vector2Int wallSize = building.Definition.FootprintCells;
            RectInt wallRect = new(building.OriginCell, wallSize);
            int dx = AxisDistance(gateRect.xMin, gateRect.xMax, wallRect.xMin, wallRect.xMax);
            int dy = AxisDistance(gateRect.yMin, gateRect.yMax, wallRect.yMin, wallRect.yMax);
            int distance = dx + dy;
            if (distance > 1 || distance >= bestDistance)
                continue;

            bestDistance = distance;
            vertical = wallSize.y > wallSize.x;
            found = true;
        }

        return found;
    }

    private static int AxisDistance(int minA, int maxA, int minB, int maxB)
    {
        if (maxA <= minB)
            return minB - maxA;

        if (maxB <= minA)
            return minA - maxB;

        return 0;
    }

    private RectInt GetEffectivePlacementRect(BuildingDefinition definition, Vector2Int originCell, GridConfig grid, bool rotateVertical = false)
    {
        Vector2Int modelFootprint = GetPlacementFootprint(definition, rotateVertical);
        RectInt modelRect = new(originCell, modelFootprint);
        if (definition == null || !definition.HasRunway)
            return modelRect;

        if (!TryGetRunwayFootprintRect(definition, originCell, grid, rotateVertical, out RectInt runwayRect))
            return modelRect;

        return UnionRects(modelRect, runwayRect);
    }

    private bool TryGetRunwayFootprintRect(BuildingDefinition definition, Vector2Int originCell, GridConfig grid, bool rotateVertical, out RectInt runwayRect)
    {
        runwayRect = default;
        if (definition == null || !definition.HasRunway || grid.CellSize <= 0f)
            return false;

        Vector2Int modelFootprint = GetPlacementFootprint(definition, rotateVertical);
        Vector3 buildingCenter = GetFootprintCenter(originCell, modelFootprint, grid);
        Vector3 visualOffset = definition.HasLocalBounds
            ? new Vector3(definition.LocalBounds.center.x, 0f, definition.LocalBounds.center.z)
            : Vector3.zero;
        Quaternion placementRotation = rotateVertical ? Quaternion.Euler(0f, 90f, 0f) : Quaternion.identity;
        Vector3 runwayCenter = buildingCenter + placementRotation * (definition.RunwayLocalPosition - visualOffset);
        Quaternion runwayRotation = placementRotation * definition.RunwayLocalRotation;

        Vector3 halfExtents = definition.RunwayHalfExtents;
        Vector3[] corners =
        {
            runwayCenter + runwayRotation * new Vector3(-halfExtents.x, 0f, -halfExtents.z),
            runwayCenter + runwayRotation * new Vector3(-halfExtents.x, 0f, halfExtents.z),
            runwayCenter + runwayRotation * new Vector3(halfExtents.x, 0f, -halfExtents.z),
            runwayCenter + runwayRotation * new Vector3(halfExtents.x, 0f, halfExtents.z)
        };

        float minX = float.PositiveInfinity;
        float minZ = float.PositiveInfinity;
        float maxX = float.NegativeInfinity;
        float maxZ = float.NegativeInfinity;
        for (int i = 0; i < corners.Length; i++)
        {
            Vector3 local = corners[i] - (Vector3)grid.Origin;
            minX = Mathf.Min(minX, local.x);
            minZ = Mathf.Min(minZ, local.z);
            maxX = Mathf.Max(maxX, local.x);
            maxZ = Mathf.Max(maxZ, local.z);
        }

        int cellMinX = Mathf.FloorToInt(minX / grid.CellSize);
        int cellMinY = Mathf.FloorToInt(minZ / grid.CellSize);
        int cellMaxX = Mathf.CeilToInt(maxX / grid.CellSize);
        int cellMaxY = Mathf.CeilToInt(maxZ / grid.CellSize);
        if (cellMaxX <= cellMinX || cellMaxY <= cellMinY)
            return false;

        runwayRect = new RectInt(cellMinX, cellMinY, cellMaxX - cellMinX, cellMaxY - cellMinY);
        return true;
    }

    private bool OverlapsAnyRuntimeBuilding(RectInt candidateRect)
    {
        if (_runtimeBuildings == null || _runtimeBuildings.Count == 0)
            return false;
        if (!TryGetGridData(out _, out GridConfig grid, out _, out _))
            return false;

        foreach (var entry in _runtimeBuildings)
        {
            RuntimeBuildingData building = entry.Value;
            if (building?.Definition == null || building.IsDestroyed)
                continue;

            RectInt existingRect = GetEffectivePlacementRect(building.Definition, building.OriginCell, grid);
            if (candidateRect.Overlaps(existingRect))
                return true;
        }

        return false;
    }

    private static RectInt UnionRects(RectInt a, RectInt b)
    {
        int xMin = Mathf.Min(a.xMin, b.xMin);
        int yMin = Mathf.Min(a.yMin, b.yMin);
        int xMax = Mathf.Max(a.xMax, b.xMax);
        int yMax = Mathf.Max(a.yMax, b.yMax);
        return new RectInt(xMin, yMin, xMax - xMin, yMax - yMin);
    }

    private bool ResolvePlacementRotateVertical(PlacementState placement)
    {
        if (placement?.Definition == null)
            return false;

        if (IsLinearWallDefinition(placement.Definition))
            return IsWallPlacementVertical(placement);

        if (ShouldAlignGateToNearbyWall(placement.Definition) &&
            TryResolveNearbyWallVertical(placement.OriginCell, placement.Definition, out bool gateVertical))
            return gateVertical;

        return false;
    }

    private static Vector2Int GetPlacementFootprint(BuildingDefinition definition, bool rotateVertical)
    {
        if (definition == null)
            return Vector2Int.one;

        if (!rotateVertical)
            return definition.FootprintCells;

        if (IsLinearWallDefinition(definition))
            return GetWallSegmentFootprint(definition, true);

        return new Vector2Int(definition.FootprintCells.y, definition.FootprintCells.x);
    }

    private static void UpdateWallDragAxis(PlacementState placement)
    {
        Vector2Int delta = placement.DragCurrentOriginCell - placement.DragStartOriginCell;
        if (delta.x == 0 && delta.y == 0)
        {
            placement.DragFirstAxis = DragFirstAxis.None;
            return;
        }

        placement.DragFirstAxis = Mathf.Abs(delta.x) >= Mathf.Abs(delta.y)
            ? DragFirstAxis.Horizontal
            : DragFirstAxis.Vertical;
    }

    private static bool IsWallPlacementVertical(PlacementState placement)
    {
        if (placement == null)
            return false;

        Vector2Int delta = placement.DragCurrentOriginCell - placement.DragStartOriginCell;
        return Mathf.Abs(delta.y) > Mathf.Abs(delta.x);
    }

    private static List<Vector2Int> BuildWallPlacementOrigins(PlacementState placement)
    {
        var origins = new List<Vector2Int>();
        if (placement == null)
            return origins;

        Vector2Int start = placement.DragStartOriginCell;
        Vector2Int end = placement.DragCurrentOriginCell;
        bool vertical = IsWallPlacementVertical(placement);
        Vector2Int footprint = GetWallSegmentFootprint(placement.Definition, vertical);
        if (vertical)
            end.x = start.x;
        else
            end.y = start.y;

        origins.Add(start);
        if (start == end)
            return origins;

        if (vertical)
        {
            int stepCells = Mathf.Max(1, footprint.y);
            int delta = end.y - start.y;
            int direction = delta >= 0 ? 1 : -1;
            int segmentCount = Mathf.Abs(delta) / stepCells;
            for (int i = 1; i <= segmentCount; i++)
                origins.Add(new Vector2Int(start.x, start.y + (direction * stepCells * i)));
        }
        else
        {
            int stepCells = Mathf.Max(1, footprint.x);
            int delta = end.x - start.x;
            int direction = delta >= 0 ? 1 : -1;
            int segmentCount = Mathf.Abs(delta) / stepCells;
            for (int i = 1; i <= segmentCount; i++)
                origins.Add(new Vector2Int(start.x + (direction * stepCells * i), start.y));
        }

        return origins;
    }

    private static List<Vector2Int> GetAllWallPlacementOrigins(PlacementState placement, List<Vector2Int> currentOrigins)
    {
        var origins = new List<Vector2Int>();
        if (placement?.CommittedWallRuns != null)
        {
            for (int i = 0; i < placement.CommittedWallRuns.Count; i++)
            {
                PlacementState.WallRun run = placement.CommittedWallRuns[i];
                if (run?.Origins == null)
                    continue;
                origins.AddRange(run.Origins);
            }
        }

        if (!placement.HideCurrentWallPreview && currentOrigins != null)
            origins.AddRange(currentOrigins);

        return origins;
    }

    private static List<PlacementState.WallRun> BuildFinalWallRuns(PlacementState placement)
    {
        var runs = new List<PlacementState.WallRun>();
        if (placement?.CommittedWallRuns != null)
        {
            for (int i = 0; i < placement.CommittedWallRuns.Count; i++)
            {
                PlacementState.WallRun run = placement.CommittedWallRuns[i];
                if (run?.Origins == null || run.Origins.Count == 0)
                    continue;
                runs.Add(run);
            }
        }

        if (placement != null && !placement.HideCurrentWallPreview)
        {
            List<Vector2Int> currentOrigins = BuildWallPlacementOrigins(placement);
            if (currentOrigins.Count > 0)
            {
                runs.Add(new PlacementState.WallRun
                {
                    Origins = currentOrigins,
                    Vertical = IsWallPlacementVertical(placement)
                });
            }
        }

        return runs;
    }

    private static void CommitCurrentWallRun(PlacementState placement)
    {
        if (placement == null)
            return;

        List<Vector2Int> origins = BuildWallPlacementOrigins(placement);
        if (origins.Count == 0)
            return;

        placement.CommittedWallRuns ??= new List<PlacementState.WallRun>();
        placement.CommittedWallRuns.Add(new PlacementState.WallRun
        {
            Origins = origins,
            Vertical = IsWallPlacementVertical(placement)
        });
        placement.HideCurrentWallPreview = true;
    }

    private bool AreAllPendingWallRunsValid(
        PlacementState placement,
        GridConfig grid,
        DynamicBuffer<GridRoad> roads,
        DynamicBlockerData blockerData)
    {
        List<PlacementState.WallRun> runs = BuildFinalWallRuns(placement);
        if (runs.Count == 0)
            return false;

        for (int runIndex = 0; runIndex < runs.Count; runIndex++)
        {
            PlacementState.WallRun run = runs[runIndex];
            if (run?.Origins == null || run.Origins.Count == 0)
                return false;

            Vector2Int footprint = GetWallSegmentFootprint(placement.Definition, run.Vertical);
            for (int i = 0; i < run.Origins.Count; i++)
            {
                if (!IsWallPlacementValid(run.Origins[i], footprint, run.Vertical, grid, roads, blockerData))
                    return false;

                for (int otherRunIndex = 0; otherRunIndex < runs.Count; otherRunIndex++)
                {
                    if (otherRunIndex == runIndex)
                        continue;

                    PlacementState.WallRun otherRun = runs[otherRunIndex];
                    if (otherRun?.Origins == null || otherRun.Origins.Count == 0)
                        continue;

                    Vector2Int otherFootprint = GetWallSegmentFootprint(placement.Definition, otherRun.Vertical);
                    for (int otherIndex = 0; otherIndex < otherRun.Origins.Count; otherIndex++)
                    {
                        if (!BuildingPlacementValidationSystem.DoWallSegmentsConflict(run.Origins[i], footprint, run.Vertical, otherRun.Origins[otherIndex], otherFootprint, otherRun.Vertical))
                            continue;

                        return false;
                    }
                }
            }
        }

        return true;
    }

    private bool AreWallPlacementOriginsValid(
        PlacementState placement,
        List<Vector2Int> origins,
        Vector2Int footprintCells,
        bool vertical,
        GridConfig grid,
        DynamicBuffer<GridRoad> roads,
        DynamicBlockerData blockerData)
    {
        if (origins == null || origins.Count == 0)
            return false;

        for (int i = 0; i < origins.Count; i++)
        {
            if (!IsWallPlacementValid(origins[i], footprintCells, vertical, grid, roads, blockerData))
                return false;
        }

        if (placement?.CommittedWallRuns != null)
        {
            for (int runIndex = 0; runIndex < placement.CommittedWallRuns.Count; runIndex++)
            {
                PlacementState.WallRun run = placement.CommittedWallRuns[runIndex];
                if (run?.Origins == null)
                    continue;

                Vector2Int committedFootprint = GetWallSegmentFootprint(placement.Definition, run.Vertical);
                for (int i = 0; i < origins.Count; i++)
                {
                    for (int j = 0; j < run.Origins.Count; j++)
                    {
                        if (!BuildingPlacementValidationSystem.DoWallSegmentsConflict(origins[i], footprintCells, vertical, run.Origins[j], committedFootprint, run.Vertical))
                            continue;

                        return false;
                    }
                }
            }
        }

        return true;
    }

    private void RebuildWallPlacementPreview(PlacementState placement, List<Vector2Int> origins, bool vertical, GridConfig grid)
    {
        if (placement?.PreviewInstance == null)
            return;

        Transform root = placement.PreviewInstance.transform;
        for (int i = root.childCount - 1; i >= 0; i--)
            Destroy(root.GetChild(i).gameObject);

        if (placement.CommittedWallRuns != null)
        {
            for (int runIndex = 0; runIndex < placement.CommittedWallRuns.Count; runIndex++)
            {
                PlacementState.WallRun run = placement.CommittedWallRuns[runIndex];
                if (run?.Origins == null)
                    continue;

                for (int i = 0; i < run.Origins.Count; i++)
                {
                    GameObject segment = CreateBuildingVisualInstance(placement.Definition, root);
                    if (segment == null)
                        continue;

                    PositionBuildingObject(segment, run.Origins[i], placement.Definition, grid, run.Vertical);
                    SetPreviewSegmentValid(segment, true);
                }
            }
        }

        if (placement.HideCurrentWallPreview)
            return;

        for (int i = 0; i < origins.Count; i++)
        {
            GameObject segment = CreateBuildingVisualInstance(placement.Definition, root);
            if (segment == null)
                continue;

            PositionBuildingObject(segment, origins[i], placement.Definition, grid, vertical);
            SetPreviewSegmentValid(segment, placement.IsValid);
        }
    }

    private void SetPreviewSegmentValid(GameObject segment, bool valid)
    {
        if (segment == null)
            return;

        Renderer[] renderers = segment.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
                continue;

            renderer.GetPropertyBlock(_markerPropertyBlock);
            Color tint = valid ? Color.white : new Color(1f, 0.45f, 0.45f, 1f);
            if (renderer.sharedMaterial != null && renderer.sharedMaterial.HasProperty("_BaseColor"))
                _markerPropertyBlock.SetColor("_BaseColor", tint);
            if (renderer.sharedMaterial != null && renderer.sharedMaterial.HasProperty("_Color"))
                _markerPropertyBlock.SetColor("_Color", tint);
            renderer.SetPropertyBlock(_markerPropertyBlock);
        }
    }

    private static Vector2Int GetWallSegmentFootprint(BuildingDefinition definition, bool vertical)
    {
        if (definition == null)
            return Vector2Int.one;

        int lengthCells = Mathf.Max(1, Mathf.RoundToInt(Mathf.Max(
            Mathf.Abs(definition.LocalBounds.size.x),
            Mathf.Abs(definition.LocalBounds.size.z))));
        int thicknessCells = Mathf.Max(1, Mathf.RoundToInt(Mathf.Min(
            Mathf.Abs(definition.LocalBounds.size.x),
            Mathf.Abs(definition.LocalBounds.size.z))));

        Vector2Int footprint = new(lengthCells, thicknessCells);
        return vertical ? new Vector2Int(footprint.y, footprint.x) : footprint;
    }

    private static BuildingDefinition CloneDefinitionWithFootprint(BuildingDefinition definition, Vector2Int footprintCells)
    {
        if (definition == null)
            return null;

        return new BuildingDefinition
        {
            DisplayName = definition.DisplayName,
            Description = definition.Description,
            MaxHealth = definition.MaxHealth,
            ProductionSlots = definition.ProductionSlots,
            SpawnUnitPrefab = definition.SpawnUnitPrefab,
            SecondarySpawnUnitPrefab = definition.SecondarySpawnUnitPrefab,
            TertiarySpawnUnitPrefab = definition.TertiarySpawnUnitPrefab,
            QuaternarySpawnUnitPrefab = definition.QuaternarySpawnUnitPrefab,
            Prefab = definition.Prefab,
            FootprintCells = footprintCells,
            Role = definition.Role,
            IsWall = definition.IsWall,
            OilBarrelsPerDay = definition.OilBarrelsPerDay,
            OilStorageCapacity = definition.OilStorageCapacity,
            FuelBarrelsPerDay = definition.FuelBarrelsPerDay,
            FuelStorageCapacity = definition.FuelStorageCapacity,
            RefugeeCapacity = definition.RefugeeCapacity,
            RefugeeUpkeepPerCitizenPerDay = definition.RefugeeUpkeepPerCitizenPerDay,
            LocalBounds = definition.LocalBounds,
            HasLocalBounds = definition.HasLocalBounds,
            VisualTemplate = definition.VisualTemplate,
            GeneratedMeshes = definition.GeneratedMeshes,
            ProductionSpawnLocalPositions = definition.ProductionSpawnLocalPositions,
            HasRunway = definition.HasRunway,
            RunwayLocalPosition = definition.RunwayLocalPosition,
            RunwayLocalRotation = definition.RunwayLocalRotation,
            RunwayHalfExtents = definition.RunwayHalfExtents
        };
    }

    private void UpdateWallPlacementOutline(List<Vector2Int> origins, Vector2Int footprintCells, GridConfig grid, bool valid)
    {
        if (origins == null || origins.Count == 0)
        {
            HidePlacementOutline();
            return;
        }

        int minX = int.MaxValue;
        int minY = int.MaxValue;
        int maxX = int.MinValue;
        int maxY = int.MinValue;
        for (int i = 0; i < origins.Count; i++)
        {
            Vector2Int origin = origins[i];
            minX = Mathf.Min(minX, origin.x);
            minY = Mathf.Min(minY, origin.y);
            maxX = Mathf.Max(maxX, origin.x + footprintCells.x);
            maxY = Mathf.Max(maxY, origin.y + footprintCells.y);
        }

        UpdatePlacementOutline(
            new Vector2Int(minX, minY),
            new Vector2Int(maxX - minX, maxY - minY),
            grid,
            valid);
    }

    private Vector3 GetFootprintCenter(Vector2Int originCell, Vector2Int footprintCells, GridConfig grid)
    {
        return new Vector3(
            grid.Origin.x + (originCell.x + footprintCells.x * 0.5f) * grid.CellSize,
            buildPlaneY,
            grid.Origin.z + (originCell.y + footprintCells.y * 0.5f) * grid.CellSize);
    }

    private Vector2Int GetCenterScreenPlacementOrigin(Vector2Int footprintCells)
    {
        if (!TryGetGridData(out _, out GridConfig grid, out _, out _))
            return Vector2Int.zero;

        Vector2 centerScreen = new(Screen.width * 0.5f, Screen.height * 0.5f);
        if (TryGetGridCell(centerScreen, grid, out Vector2Int centerCell))
            return CenterCellToOrigin(centerCell, footprintCells);

        return Vector2Int.zero;
    }

    private static Vector2Int CenterCellToOrigin(Vector2Int centerCell, Vector2Int footprintCells)
    {
        return new Vector2Int(
            centerCell.x - Mathf.FloorToInt(footprintCells.x * 0.5f),
            centerCell.y - Mathf.FloorToInt(footprintCells.y * 0.5f));
    }

    private bool IsPlacementValid(Vector2Int originCell, Vector2Int footprintCells, GridConfig grid, DynamicBuffer<GridRoad> roads, DynamicBlockerData blockerData)
    {
        return IsPlacementValid(_activePlacement?.Definition, originCell, footprintCells, ResolvePlacementRotateVertical(_activePlacement), grid, roads, blockerData);
    }

    private bool IsPlacementValid(BuildingDefinition definition, Vector2Int originCell, Vector2Int footprintCells, bool rotateVertical, GridConfig grid, DynamicBuffer<GridRoad> roads, DynamicBlockerData blockerData)
    {
        RectInt placementRect = definition != null
            ? GetEffectivePlacementRect(definition, originCell, grid, rotateVertical)
            : new RectInt(originCell, footprintCells);

        return BuildingPlacementValidationSystem.IsPlacementRectValid(
            placementRect,
            grid,
            roads,
            blockerData,
            _hasPlacementInvalidPrefix,
            _placementInvalidPrefix,
            _placementInvalidPrefixWidth,
            _placementInvalidPrefixHeight,
            IsRuntimeBlockerCell,
            _roadBuildController != null ? _roadBuildController.HasRoadInFootprint : null,
            OverlapsAnyRuntimeBuilding);
    }

    private bool IsWallPlacementValid(
        Vector2Int originCell,
        Vector2Int footprintCells,
        bool vertical,
        GridConfig grid,
        DynamicBuffer<GridRoad> roads,
        DynamicBlockerData blockerData,
        bool allowExistingWallOverlap = false)
    {
        return BuildingPlacementValidationSystem.IsWallFootprintValid(
            originCell,
            footprintCells,
            vertical,
            grid,
            roads,
            blockerData,
            allowExistingWallOverlap,
            IsRuntimeBlockerCell,
            (x, y) => IsPerpendicularWallOverlapCell(x, y, vertical),
            IsLinearWallOverlapCell,
            _roadBuildController != null ? _roadBuildController.HasRoadInFootprint : null);
    }

    private bool IsLinearWallOverlapCell(int x, int y)
    {
        foreach (var entry in _runtimeBuildings)
        {
            RuntimeBuildingData building = entry.Value;
            if (building?.Definition == null || !IsLinearWallDefinition(building.Definition))
                continue;

            Vector2Int min = building.OriginCell;
            Vector2Int size = building.Definition.FootprintCells;
            if (x >= min.x && x < min.x + size.x &&
                y >= min.y && y < min.y + size.y)
                return true;
        }

        return false;
    }

    private bool IsPerpendicularWallOverlapCell(int x, int y, bool vertical)
    {
        foreach (var entry in _runtimeBuildings)
        {
            RuntimeBuildingData building = entry.Value;
            if (building?.Definition == null || !IsLinearWallDefinition(building.Definition))
                continue;

            bool buildingVertical = building.Definition.FootprintCells.y > building.Definition.FootprintCells.x;
            if (buildingVertical == vertical)
                continue;

            Vector2Int min = building.OriginCell;
            Vector2Int size = building.Definition.FootprintCells;
            if (x >= min.x && x < min.x + size.x &&
                y >= min.y && y < min.y + size.y)
                return true;
        }

        return false;
    }

    private Entity CreateBlockerEntity(BuildingDefinition definition, Vector2Int originCell, Vector2Int footprintCells)
    {
        if (!TryGetEntityManager(out EntityManager em))
            return Entity.Null;

        Entity entity = em.CreateEntity();
        em.AddComponentData(entity, new UnitGrid { Cell = new int2(originCell.x, originCell.y) });
        em.AddComponentData(entity, new GridBlockerSize { Size = new int2(footprintCells.x, footprintCells.y) });
        em.AddComponent<StaticGridBlocker>(entity);
        return entity;
    }

    private static bool ShouldRuntimeBuildingBlockPathing(BuildingDefinition definition)
    {
        return !RuntimeDefinitionMatchesId(definition, NormalizeSpawnableKey("Building_Helipad"));
    }

    private Entity CreateBuildingCombatEntity(Vector2Int originCell, BuildingDefinition definition, byte ownerFactionId, Quaternion worldRotation)
    {
        if (definition == null)
            return Entity.Null;
        if (!TryGetEntityManager(out EntityManager em))
            return Entity.Null;
        if (!TryGetGridData(out _, out GridConfig grid, out _, out _))
            return Entity.Null;

        Vector2Int footprintCells = new(Mathf.Max(1, definition.FootprintCells.x), Mathf.Max(1, definition.FootprintCells.y));
        float3 center = (float3)GetFootprintCenter(originCell, footprintCells, grid);
        int maxHealth = Mathf.Max(1, definition.MaxHealth);
        Entity entity = em.CreateEntity();
        em.AddComponentData(entity, new LocalTransform
        {
            Position = center,
            Rotation = new quaternion(worldRotation.x, worldRotation.y, worldRotation.z, worldRotation.w),
            Scale = 1f
        });
        em.AddComponentData(entity, new LocalToWorld());
        em.AddComponentData(entity, new UnitGrid
        {
            Cell = new int2(originCell.x + footprintCells.x / 2, originCell.y + footprintCells.y / 2)
        });
        em.AddComponentData(entity, new UnitFootprint
        {
            Size = new int2(footprintCells.x, footprintCells.y)
        });
        em.AddComponent<RuntimeBuildingCombatTag>(entity);
        em.AddComponentData(entity, new UnitGridInitialized());
        em.AddComponentData(entity, new Faction { Id = ownerFactionId });
        em.AddComponentData(entity, new UnitHealth { Current = maxHealth, Max = maxHealth });
        em.AddComponentData(entity, new UnitRespawnPrefab { Prefab = Entity.Null });
        em.AddComponentData(entity, new UnitSourcePrefabKey
        {
            Value = new FixedString64Bytes(definition.Prefab != null ? definition.Prefab.name : definition.DisplayName)
        });
        em.AddComponentData(entity, new UnitDisplayInfo
        {
            Name = new FixedString64Bytes(string.IsNullOrWhiteSpace(definition.DisplayName) ? "Building" : definition.DisplayName),
            Description = new FixedString128Bytes(definition.Description ?? string.Empty)
        });
        if (definition.ThreatDetectionKind != ThreatDetectionKind.None && definition.ThreatDetectionRadiusCells > 0)
        {
            em.AddComponentData(entity, new ThreatDetector
            {
                Kind = (byte)definition.ThreatDetectionKind,
                RadiusCells = Mathf.Max(0, definition.ThreatDetectionRadiusCells)
            });
        }
        em.AddComponentData(entity, new UnitPrevWorldPos { Value = center });
        em.AddComponentData(entity, new UnitMoveVisualState { IsMoving = 0, StillSeconds = 0f });
        em.AddComponentData(entity, new UnitAnimationSettings
        {
            AttackAnimationSeconds = 0.1f,
            DeathAnimationSeconds = 0.01f
        });
        return entity;
    }

    private static BuildingDefinition CreateDefinition(
        GameObject prefab,
        string fallbackDisplayName,
        string fallbackDescription,
        int fallbackMaxHealth,
        GameObject fallbackPrimarySpawnUnitPrefab,
        GameObject fallbackSecondarySpawnUnitPrefab,
        GameObject fallbackTertiarySpawnUnitPrefab)
    {
        BuildingDefinitionAuthoring authoring = prefab != null ? prefab.GetComponent<BuildingDefinitionAuthoring>() : null;
        if (authoring != null)
            authoring.ApplyConfigIfAvailable();
        Vector2Int footprint = authoring != null
            ? new Vector2Int(Mathf.Max(1, authoring.ConfiguredFootprintCells.x), Mathf.Max(1, authoring.ConfiguredFootprintCells.y))
            : Vector2Int.one;
        if (TryGetFootprintFromVisualBounds(prefab, out Vector2Int visualFootprint))
            footprint = visualFootprint;

        Bounds localBounds = default;
        bool hasLocalBounds = TryGetPrefabLocalBounds(prefab, out localBounds);
        bool hasRunway = TryGetRunwayLocalData(prefab, out Vector3 runwayLocalPosition, out Quaternion runwayLocalRotation, out Vector3 runwayHalfExtents);

        List<BuildingDefinition.ProductionSlotDefinition> productionSlots = BuildProductionSlots(
            authoring,
            fallbackPrimarySpawnUnitPrefab,
            fallbackSecondarySpawnUnitPrefab,
            fallbackTertiarySpawnUnitPrefab,
            null);

        return new BuildingDefinition
        {
            DisplayName = authoring != null && !string.IsNullOrWhiteSpace(authoring.ConfiguredDisplayName) ? authoring.ConfiguredDisplayName : fallbackDisplayName,
            Description = authoring != null && !string.IsNullOrWhiteSpace(authoring.ConfiguredDescription) ? authoring.ConfiguredDescription : fallbackDescription,
            MaxHealth = authoring != null ? Mathf.Max(1, authoring.ConfiguredMaxHealth) : Mathf.Max(1, fallbackMaxHealth),
            ProductionSlots = productionSlots,
            SpawnUnitPrefab = GetProductionPrefab(productionSlots, 0),
            SecondarySpawnUnitPrefab = GetProductionPrefab(productionSlots, 1),
            TertiarySpawnUnitPrefab = GetProductionPrefab(productionSlots, 2),
            QuaternarySpawnUnitPrefab = GetProductionPrefab(productionSlots, 3),
            Prefab = prefab,
            FootprintCells = footprint,
            Role = authoring != null ? authoring.ConfiguredRole : BuildingRole.None,
            IsWall = authoring != null && authoring.ConfiguredIsWall,
            OilBarrelsPerDay = authoring != null ? Mathf.Max(0f, authoring.ConfiguredOilBarrelsPerDay) : 0f,
            OilStorageCapacity = authoring != null ? Mathf.Max(0, authoring.ConfiguredOilStorageCapacity) : 0,
            FuelBarrelsPerDay = authoring != null ? Mathf.Max(0f, authoring.ConfiguredFuelBarrelsPerDay) : 0f,
            FuelStorageCapacity = authoring != null ? Mathf.Max(0, authoring.ConfiguredFuelStorageCapacity) : 0,
            RefugeeCapacity = authoring != null ? Mathf.Max(0, authoring.ConfiguredRefugeeCapacity) : 0,
            RefugeeUpkeepPerCitizenPerDay = authoring != null ? Mathf.Max(0, authoring.ConfiguredRefugeeUpkeepPerCitizenPerDay) : 0,
            ThreatDetectionKind = authoring != null ? authoring.ConfiguredThreatDetectionKind : ThreatDetectionKind.None,
            ThreatDetectionRadiusCells = authoring != null ? Mathf.Max(0, authoring.ConfiguredThreatDetectionRadiusCells) : 0,
            LocalBounds = localBounds,
            HasLocalBounds = hasLocalBounds,
            ProductionSpawnLocalPositions = FindProductionSpawnLocalPositions(prefab),
            HasRunway = hasRunway,
            RunwayLocalPosition = runwayLocalPosition,
            RunwayLocalRotation = runwayLocalRotation,
            RunwayHalfExtents = runwayHalfExtents
        };
    }

    private BuildingDefinition CreateConfiguredDefinition(
        string fallbackDisplayName,
        string fallbackDescription,
        int fallbackMaxHealth,
        GameObject fallbackPrimarySpawnUnitPrefab,
        GameObject fallbackSecondarySpawnUnitPrefab,
        GameObject fallbackTertiarySpawnUnitPrefab)
    {
        GameObject prefab = TryGetSpawnablePrefab(fallbackDisplayName);
        return CreateDefinition(
            prefab,
            fallbackDisplayName,
            fallbackDescription,
            fallbackMaxHealth,
            fallbackPrimarySpawnUnitPrefab,
            fallbackSecondarySpawnUnitPrefab,
            fallbackTertiarySpawnUnitPrefab);
    }

    private GameObject TryGetSpawnablePrefab(string displayName)
    {
        string key = NormalizeSpawnableKey(displayName);
        return !string.IsNullOrEmpty(key) && _spawnablesByKey.TryGetValue(key, out GameObject prefab) ? prefab : null;
    }

    private static string GetSpawnableLookupKey(GameObject prefab)
    {
        if (prefab == null)
            return string.Empty;

        BuildingDefinitionAuthoring authoring = prefab.GetComponent<BuildingDefinitionAuthoring>();
        if (authoring != null && !string.IsNullOrWhiteSpace(authoring.ConfiguredDisplayName))
            return NormalizeSpawnableKey(authoring.ConfiguredDisplayName);

        return NormalizeSpawnableKey(prefab.name);
    }

    private static string GetSpawnableLookupKey(string name)
    {
        return NormalizeSpawnableKey(name);
    }

    private static string NormalizeSpawnableKey(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return value.Trim().ToLowerInvariant();
    }

    private static bool RuntimeBuildingMatchesId(RuntimeBuildingData building, string normalizedBuildingId)
    {
        return building?.Definition != null && RuntimeDefinitionMatchesId(building.Definition, normalizedBuildingId);
    }

    private static bool IsRuntimeBuildingId(RuntimeBuildingData building, string buildingId)
    {
        return RuntimeBuildingMatchesId(building, NormalizeSpawnableKey(buildingId));
    }

    private static bool IsHelicopterUnitPrefab(GameObject prefab)
    {
        return prefab != null && prefab.name.StartsWith("Unit_Veh_Helicopter_", System.StringComparison.OrdinalIgnoreCase);
    }

    private bool RuntimeProducedUnitMatchesId(RuntimeBuildingData building, Entity unit, string normalizedUnitId)
    {
        if (string.IsNullOrEmpty(normalizedUnitId))
            return true;
        if (building?.ProducedUnitPrefabs != null &&
            building.ProducedUnitPrefabs.TryGetValue(unit, out GameObject prefab) &&
            UnitPrefabMatchesId(prefab, normalizedUnitId))
        {
            return true;
        }
        if (TryGetEntityManager(out EntityManager em) &&
            unit != Entity.Null &&
            em.Exists(unit) &&
            em.HasComponent<UnitSourcePrefabKey>(unit))
        {
            string sourceKey = em.GetComponentData<UnitSourcePrefabKey>(unit).Value.ToString();
            if (NormalizeSpawnableKey(sourceKey) == normalizedUnitId)
                return true;
        }

        return false;
    }

    private static bool UnitPrefabMatchesId(GameObject prefab, string normalizedUnitId)
    {
        if (string.IsNullOrEmpty(normalizedUnitId))
            return true;
        if (prefab == null)
            return false;

        if (NormalizeSpawnableKey(prefab.name) == normalizedUnitId)
            return true;

        UnitGridAuthoring authoring = prefab.GetComponent<UnitGridAuthoring>();
        if (authoring != null && NormalizeSpawnableKey(authoring.ConfiguredDisplayName) == normalizedUnitId)
            return true;

        return false;
    }

    private static bool RuntimeDefinitionMatchesId(BuildingDefinition definition, string normalizedBuildingId)
    {
        if (definition == null || string.IsNullOrEmpty(normalizedBuildingId))
            return false;

        if (NormalizeSpawnableKey(definition.DisplayName) == normalizedBuildingId)
            return true;

        if (definition.Prefab != null)
        {
            if (NormalizeSpawnableKey(definition.Prefab.name) == normalizedBuildingId)
                return true;

            BuildingDefinitionAuthoring authoring = definition.Prefab.GetComponent<BuildingDefinitionAuthoring>();
            if (authoring != null && NormalizeSpawnableKey(authoring.ConfiguredDisplayName) == normalizedBuildingId)
                return true;
        }

        return false;
    }

    private static void RegisterSpawnableLookupAliases(Dictionary<string, GameObject> lookup, GameObject prefab)
    {
        if (lookup == null || prefab == null)
            return;

        string prefabNameKey = NormalizeSpawnableKey(prefab.name);
        if (!string.IsNullOrEmpty(prefabNameKey))
            lookup[prefabNameKey] = prefab;

        string displayNameKey = GetSpawnableLookupKey(prefab);
        if (!string.IsNullOrEmpty(displayNameKey) && displayNameKey != prefabNameKey && !lookup.ContainsKey(displayNameKey))
            lookup[displayNameKey] = prefab;
    }

    private static Vector3[] FindProductionSpawnLocalPositions(GameObject prefab)
    {
        if (prefab == null)
            return null;

        List<(int index, Vector3 position)> matches = new();
        Transform[] transforms = prefab.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform candidate = transforms[i];
            if (candidate == null)
                continue;

            if (!TryParseSpawnPointIndex(candidate.name, out int index))
                continue;

            matches.Add((index, candidate.localPosition));
        }

        if (matches.Count == 0)
            return null;

        matches.Sort((a, b) => a.index.CompareTo(b.index));
        Vector3[] ordered = new Vector3[matches.Count];
        for (int i = 0; i < matches.Count; i++)
            ordered[i] = matches[i].position;
        return ordered;
    }

    private static bool TryGetRunwayLocalData(GameObject prefab, out Vector3 localPosition, out Quaternion localRotation, out Vector3 halfExtents)
    {
        localPosition = Vector3.zero;
        localRotation = Quaternion.identity;
        halfExtents = new Vector3(8f, 0.5f, 24f);
        if (prefab == null)
            return false;

        Transform runway = null;
        Transform[] transforms = prefab.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i] != null && transforms[i].name == "Runway")
            {
                runway = transforms[i];
                break;
            }
        }

        if (runway == null)
            return false;

        Transform runwayStart = null;
        Transform runwayEnd = null;
        for (int i = 0; i < runway.childCount; i++)
        {
            Transform child = runway.GetChild(i);
            if (child == null)
                continue;
            if (child.name == "Runway_Start")
                runwayStart = child;
            else if (child.name == "Runway_End")
                runwayEnd = child;
        }

        if (runwayStart != null && runwayEnd != null)
        {
            Vector3 worldStart = runwayStart.position;
            Vector3 worldEnd = runwayEnd.position;
            Vector3 worldDirection = worldEnd - worldStart;
            Vector3 planarDirection = new Vector3(worldDirection.x, 0f, worldDirection.z);
            if (planarDirection.sqrMagnitude > 0.0001f)
            {
                Vector3 worldCenter = (worldStart + worldEnd) * 0.5f;
                localPosition = prefab.transform.InverseTransformPoint(worldCenter);
                Quaternion worldRotation = Quaternion.LookRotation(planarDirection.normalized, Vector3.up);
                localRotation = Quaternion.Inverse(prefab.transform.rotation) * worldRotation;
                halfExtents = new Vector3(
                    8f,
                    0.5f,
                    Mathf.Max(8f, planarDirection.magnitude * 0.5f));
                return true;
            }
        }

        localPosition = runway.localPosition;
        localRotation = runway.localRotation;

        Renderer runwayRenderer = runway.GetComponentInChildren<Renderer>(true);
        if (runwayRenderer != null)
        {
            Bounds bounds = runwayRenderer.localBounds;
            halfExtents = bounds.extents;
            if (halfExtents.x <= 0.01f || halfExtents.z <= 0.01f)
                halfExtents = new Vector3(8f, 0.5f, 24f);
        }

        return true;
    }

    private static bool TryParseSpawnPointIndex(string name, out int index)
    {
        index = -1;
        if (string.IsNullOrWhiteSpace(name) || !name.StartsWith("Spawn_", System.StringComparison.OrdinalIgnoreCase))
            return false;

        string suffix = name.Substring("Spawn_".Length);
        return int.TryParse(suffix, NumberStyles.Integer, CultureInfo.InvariantCulture, out index);
    }

    private static BuildingDefinition.ProductionSlotDefinition GetProductionOrFallback(
        BuildingDefinitionAuthoring authoring,
        int index,
        GameObject fallbackSpawnUnitPrefab)
    {
        if (authoring != null)
        {
            BuildingDefinitionAuthoring.ProductionDefinition production = authoring.GetProductionOrDefault(index);
            if (production != null)
            {
                return new BuildingDefinition.ProductionSlotDefinition
                {
                    SpawnUnitPrefab = production.spawnUnitPrefab
                };
            }
        }

        return new BuildingDefinition.ProductionSlotDefinition
        {
            SpawnUnitPrefab = fallbackSpawnUnitPrefab
        };
    }

    private static List<BuildingDefinition.ProductionSlotDefinition> BuildProductionSlots(
        BuildingDefinitionAuthoring authoring,
        params GameObject[] fallbackSpawnUnitPrefabs)
    {
        int configuredCount = authoring != null ? Mathf.Max(0, authoring.ConfiguredProductionCount) : 0;
        int fallbackCount = fallbackSpawnUnitPrefabs != null ? fallbackSpawnUnitPrefabs.Length : 0;
        int count = Mathf.Max(configuredCount, fallbackCount);
        var slots = new List<BuildingDefinition.ProductionSlotDefinition>(count);
        for (int i = 0; i < count; i++)
        {
            GameObject fallback = i < fallbackCount ? fallbackSpawnUnitPrefabs[i] : null;
            BuildingDefinition.ProductionSlotDefinition slot = GetProductionOrFallback(authoring, i, fallback);
            if (slot == null || slot.SpawnUnitPrefab == null)
                continue;
            slots.Add(slot);
        }

        return slots;
    }

    private static int GetProductionCount(BuildingDefinition definition)
    {
        if (definition == null)
            return 0;

        if (definition.ProductionSlots != null && definition.ProductionSlots.Count > 0)
            return definition.ProductionSlots.Count;

        int count = 0;
        if (definition.SpawnUnitPrefab != null) count = 1;
        if (definition.SecondarySpawnUnitPrefab != null) count = 2;
        if (definition.TertiarySpawnUnitPrefab != null) count = 3;
        if (definition.QuaternarySpawnUnitPrefab != null) count = 4;
        return count;
    }

    private static GameObject GetProductionPrefab(List<BuildingDefinition.ProductionSlotDefinition> slots, int index)
    {
        if (slots == null || index < 0 || index >= slots.Count)
            return null;

        return slots[index]?.SpawnUnitPrefab;
    }

    private static GameObject GetProductionPrefab(BuildingDefinition definition, int index)
    {
        if (definition == null || index < 0)
            return null;

        if (definition.ProductionSlots != null && index < definition.ProductionSlots.Count)
            return definition.ProductionSlots[index]?.SpawnUnitPrefab;

        return index switch
        {
            0 => definition.SpawnUnitPrefab,
            1 => definition.SecondarySpawnUnitPrefab,
            2 => definition.TertiarySpawnUnitPrefab,
            3 => definition.QuaternarySpawnUnitPrefab,
            _ => null
        };
    }

    private bool TryFindFirstFriendlyProducerBuilding(GameObject unitPrefab, out int buildingId, out int productionIndex, out string buildingDisplayName)
    {
        buildingId = 0;
        productionIndex = -1;
        buildingDisplayName = string.Empty;
        if (unitPrefab == null)
            return false;

        foreach (KeyValuePair<int, RuntimeBuildingData> pair in _runtimeBuildings)
        {
            RuntimeBuildingData building = pair.Value;
            if (building?.Definition == null || building.IsDestroyed)
                continue;
            if (building.IsCityGenerated)
                continue;
            if (building.HasOwnerFaction && building.OwnerFactionId != 0)
                continue;

            int productionCount = GetProductionCount(building.Definition);
            for (int i = 0; i < productionCount; i++)
            {
                if (GetProductionPrefab(building.Definition, i) != unitPrefab)
                    continue;
                if (!CanQueueUnitFromBuilding(building, unitPrefab, false))
                    continue;

                buildingId = pair.Key;
                productionIndex = i;
                buildingDisplayName = building.Definition.DisplayName ?? string.Empty;
                return true;
            }
        }

        return false;
    }

    private bool TryFindFirstFactionProducerBuilding(byte factionId, GameObject unitPrefab, out int buildingId, out int productionIndex, out string buildingDisplayName)
    {
        buildingId = 0;
        productionIndex = -1;
        buildingDisplayName = string.Empty;
        if (unitPrefab == null)
            return false;

        foreach (KeyValuePair<int, RuntimeBuildingData> pair in _runtimeBuildings)
        {
            RuntimeBuildingData building = pair.Value;
            if (building?.Definition == null || building.IsDestroyed)
                continue;
            if (building.IsCityGenerated)
                continue;
            if (!building.HasOwnerFaction || building.OwnerFactionId != factionId)
                continue;

            int productionCount = GetProductionCount(building.Definition);
            for (int i = 0; i < productionCount; i++)
            {
                if (GetProductionPrefab(building.Definition, i) != unitPrefab)
                    continue;
                if (!CanQueueUnitFromBuilding(building, unitPrefab, false))
                    continue;

                buildingId = pair.Key;
                productionIndex = i;
                buildingDisplayName = building.Definition.DisplayName ?? string.Empty;
                return true;
            }
        }

        return false;
    }

    private bool TryGetRequiredProducerDisplayName(GameObject unitPrefab, out string buildingDisplayName)
    {
        buildingDisplayName = string.Empty;
        if (unitPrefab == null)
            return false;

        for (int i = 0; i < _configuredSpawnableDefinitions.Count; i++)
        {
            BuildingDefinition definition = _configuredSpawnableDefinitions[i];
            if (definition == null)
                continue;

            int productionCount = GetProductionCount(definition);
            for (int productionIndex = 0; productionIndex < productionCount; productionIndex++)
            {
                if (GetProductionPrefab(definition, productionIndex) != unitPrefab)
                    continue;

                buildingDisplayName = string.IsNullOrWhiteSpace(definition.DisplayName) ? "Building" : definition.DisplayName;
                return true;
            }
        }

        return false;
    }

    private void SelectBuildingForProductionRequest(RuntimeBuildingData building, GameObject producedUnitPrefab)
    {
        if (building == null)
            return;

        _runtimeBuildingSystem.SelectBuilding(building.Id);
        InitialUnitsRuntimeState.SuppressNextWorldClick = true;
        RefreshBuildingMarkerVisibility();

        _selectionSystem?.ClearFocusedUnit();

        Vector3 focusWorldPosition = ResolveProductionRequestFocusWorldPosition(building, producedUnitPrefab);
        _selectionSystem?.SmoothMoveCameraGroundCenterTo(focusWorldPosition);
    }

    private void RememberCampProductionFocus(RuntimeBuildingData building, GameObject producedUnitPrefab)
    {
        _lastCampProductionFocusBuilding = building;
        _lastCampProductionFocusPrefab = producedUnitPrefab;
    }

    private Vector3 ResolveProductionRequestFocusWorldPosition(RuntimeBuildingData producerBuilding, GameObject producedUnitPrefab)
    {
        if (producerBuilding == null)
            return Vector3.zero;

        ResolveProductionTransportSettings(
            producedUnitPrefab,
            out _,
            out _,
            out _,
            out _,
            out ProductionTransportMode transportMode,
            out bool transportRequiresAirportRunway);

        if (transportMode == ProductionTransportMode.Plane &&
            transportRequiresAirportRunway &&
            TryGetNearestAirportRunway(
                producerBuilding.Instance != null ? producerBuilding.Instance.transform.position : Vector3.zero,
                out _,
                out Vector3 runwayCenter,
                out _,
                out _))
        {
            runwayCenter.y = 0f;
            return runwayCenter;
        }

        return ResolveBuildingFocusWorldPosition(producerBuilding);
    }

    private static bool TryGetFootprintFromVisualBounds(GameObject prefab, out Vector2Int footprint)
    {
        footprint = default;
        if (prefab == null)
            return false;

        if (!TryGetPrefabLocalBounds(prefab, out Bounds localBounds))
            return false;

        int width = Mathf.Max(1, Mathf.CeilToInt(Mathf.Abs(localBounds.size.x)));
        int height = Mathf.Max(1, Mathf.CeilToInt(Mathf.Abs(localBounds.size.z)));
        footprint = new Vector2Int(width, height);
        return true;
    }

    private static bool TryGetPrefabLocalBounds(GameObject prefab, out Bounds localBounds)
    {
        localBounds = default;
        if (prefab == null)
            return false;

        if (TryGetModelLocalBounds(prefab.transform, out localBounds))
            return true;

        return TryGetLocalBounds(prefab, out localBounds);
    }

    private static bool TryGetModelLocalBounds(Transform root, out Bounds combinedBounds)
    {
        combinedBounds = default;
        if (root == null)
            return false;

        Transform modelRoot = root.Find("Model");
        if (modelRoot == null)
            return false;

        MeshRenderer[] renderers = modelRoot.GetComponentsInChildren<MeshRenderer>(true);
        Matrix4x4 worldToLocal = root.worldToLocalMatrix;
        bool hasBounds = false;

        for (int i = 0; i < renderers.Length; i++)
        {
            MeshRenderer renderer = renderers[i];
            if (renderer == null)
                continue;

            Bounds localBounds = TransformRendererBounds(worldToLocal * renderer.localToWorldMatrix, renderer.localBounds);
            if (!hasBounds)
            {
                combinedBounds = localBounds;
                hasBounds = true;
            }
            else
            {
                combinedBounds.Encapsulate(localBounds);
            }
        }

        return hasBounds;
    }

    private static Bounds TransformRendererBounds(Matrix4x4 matrix, Bounds bounds)
    {
        Vector3 center = bounds.center;
        Vector3 extents = bounds.extents;

        Vector3 min = new(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
        Vector3 max = new(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

        for (int x = -1; x <= 1; x += 2)
        {
            for (int y = -1; y <= 1; y += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    Vector3 corner = center + Vector3.Scale(extents, new Vector3(x, y, z));
                    Vector3 transformed = matrix.MultiplyPoint3x4(corner);
                    min = Vector3.Min(min, transformed);
                    max = Vector3.Max(max, transformed);
                }
            }
        }

        Bounds transformedBounds = new();
        transformedBounds.SetMinMax(min, max);
        return transformedBounds;
    }

    private void AttachRuntimeLink(RuntimeBuildingData building)
    {
        RuntimeBuildingEntityLink link = building.Instance.GetComponent<RuntimeBuildingEntityLink>();
        if (link == null)
            link = building.Instance.AddComponent<RuntimeBuildingEntityLink>();
        link.Configure(this, building.Id, building.CombatEntity, building.BlockerEntity);
    }

    private bool TryGetAvailableProductionSpawnSlot(RuntimeBuildingData building, EntityManager em, out int slotIndex, out Vector3 spawnLocalPosition)
    {
        slotIndex = -1;
        spawnLocalPosition = Vector3.zero;
        if (building == null || building.ProductionSpawnLocalPositions == null || building.ProducedUnitSlots == null)
            return false;

        int count = math.min(building.ProductionSpawnLocalPositions.Length, building.ProducedUnitSlots.Length);
        for (int i = 0; i < count; i++)
        {
            Entity occupant = building.ProducedUnitSlots[i];
            if (occupant != Entity.Null)
            {
                bool occupied = em.Exists(occupant);
                if (occupied && em.HasComponent<UnitHealth>(occupant))
                    occupied = em.GetComponentData<UnitHealth>(occupant).Current > 0;

                if (occupied)
                    continue;

                building.ProducedUnitSlots[i] = Entity.Null;
            }

            slotIndex = i;
            spawnLocalPosition = building.ProductionSpawnLocalPositions[i];
            return true;
        }

        return false;
    }

    private bool TryResolveHelicopterSpawnForFaction(
        byte factionId,
        RuntimeBuildingData sourceBuilding,
        EntityManager em,
        ref Unity.Mathematics.Random rng,
        in GridConfig grid,
        in NativeArray<GridWalkable> walkable,
        in NativeBitArray blocked,
        in NativeBitArray occupied,
        ref NativeBitArray reserved,
        int2 unitFootprint,
        out int2 cell,
        out float3 worldPosition,
        out RuntimeBuildingData slotBuilding,
        out int slotIndex)
    {
        cell = default;
        worldPosition = default;
        slotBuilding = null;
        slotIndex = -1;

        bool foundHelipad = false;
        int2 helipadSearchCenter = default;
        int helipadSearchRadius = 0;
        string helipadKey = NormalizeSpawnableKey("Building_Helipad");

        foreach (KeyValuePair<int, RuntimeBuildingData> entry in _runtimeBuildings)
        {
            RuntimeBuildingData building = entry.Value;
            if (!IsOwnedRuntimeBuildingForFaction(building, factionId) ||
                building.Instance == null ||
                building.ProductionSpawnLocalPositions == null ||
                building.ProductionSpawnLocalPositions.Length == 0 ||
                !RuntimeBuildingMatchesId(building, helipadKey))
                continue;

            foundHelipad = true;
            Vector2Int footprint = building.Definition != null ? building.Definition.FootprintCells : Vector2Int.one;
            int2 buildingCenter = new(building.OriginCell.x + footprint.x / 2, building.OriginCell.y + footprint.y / 2);
            if (helipadSearchRadius == 0)
                helipadSearchCenter = buildingCenter;
            helipadSearchRadius = math.max(helipadSearchRadius, math.max(footprint.x, footprint.y) + math.max(unitFootprint.x, unitFootprint.y) + 12);

            int count = building.ProducedUnitSlots != null && building.ProducedUnitSlots.Length > 0
                ? math.min(building.ProductionSpawnLocalPositions.Length, building.ProducedUnitSlots.Length)
                : building.ProductionSpawnLocalPositions.Length;
            for (int i = 0; i < count; i++)
            {
                if (IsProductionSlotReservedByPending(building, i))
                    continue;
                if (IsProductionSlotOccupied(building, em, i))
                    continue;

                Vector3 candidateWorld = building.Instance.transform.TransformPoint(building.ProductionSpawnLocalPositions[i]);
                int2 candidateCell = GridUtils.WorldToCell(grid, candidateWorld);
                if (!GridUtils.InBounds(candidateCell, grid.Width, grid.Height))
                    continue;
                if (OverlapsRecentSpawnReservation(candidateCell, unitFootprint))
                    continue;
                if (OverlapsExistingUnitFootprint(em, candidateCell, unitFootprint))
                    continue;

                cell = candidateCell;
                worldPosition = candidateWorld;
                slotBuilding = building;
                slotIndex = i;
                return true;
            }
        }

        if (foundHelipad)
        {
            foreach (KeyValuePair<int, RuntimeBuildingData> entry in _runtimeBuildings)
            {
                RuntimeBuildingData building = entry.Value;
                if (!IsOwnedRuntimeBuildingForFaction(building, factionId) || !RuntimeBuildingMatchesId(building, helipadKey))
                    continue;

                Vector2Int footprint = building.Definition != null ? building.Definition.FootprintCells : Vector2Int.one;
                int2 center = new(building.OriginCell.x + footprint.x / 2, building.OriginCell.y + footprint.y / 2);
                int radius = math.max(footprint.x, footprint.y) + math.max(unitFootprint.x, unitFootprint.y) + 10;
                if (TryFindStrictSpawnCell(em, ref rng, grid, walkable, blocked, occupied, ref reserved, center, radius, unitFootprint, out cell))
                {
                    worldPosition = GridUtils.CellToWorldCenter(grid, cell);
                    return true;
                }
            }

            if (TryFindStrictSpawnCell(em, ref rng, grid, walkable, blocked, occupied, ref reserved, helipadSearchCenter, helipadSearchRadius + 24, unitFootprint, out cell))
            {
                worldPosition = GridUtils.CellToWorldCenter(grid, cell);
                return true;
            }
        }

        if (TryGetFactionRuntimeBuildingCenter(factionId, sourceBuilding, out int2 baseCenter))
        {
            int baseRadius = foundHelipad ? 96 : 140;
            if (TryFindStrictSpawnCell(em, ref rng, grid, walkable, blocked, occupied, ref reserved, baseCenter, baseRadius, unitFootprint, out cell))
            {
                worldPosition = GridUtils.CellToWorldCenter(grid, cell);
                return true;
            }
        }

        return false;
    }

    private static bool IsOwnedRuntimeBuildingForFaction(RuntimeBuildingData building, byte factionId)
    {
        return building != null &&
               !building.IsDestroyed &&
               building.HasOwnerFaction &&
               building.OwnerFactionId == factionId;
    }

    private static bool IsProductionSlotReservedByPending(RuntimeBuildingData building, int slotIndex)
    {
        if (building?.PendingProductions == null)
            return false;

        for (int i = 0; i < building.PendingProductions.Count; i++)
        {
            RuntimeBuildingData.PendingProduction pending = building.PendingProductions[i];
            if (pending != null && pending.ReservedProductionSlotIndex == slotIndex)
                return true;
        }

        return false;
    }

    private static bool IsProductionSlotOccupied(RuntimeBuildingData building, EntityManager em, int slotIndex)
    {
        if (building?.ProducedUnitSlots == null ||
            slotIndex < 0 ||
            slotIndex >= building.ProducedUnitSlots.Length)
            return false;

        Entity occupant = building.ProducedUnitSlots[slotIndex];
        bool occupied = occupant != Entity.Null && em.Exists(occupant);
        if (occupied && em.HasComponent<UnitHealth>(occupant))
            occupied = em.GetComponentData<UnitHealth>(occupant).Current > 0;

        if (!occupied && occupant != Entity.Null)
            building.ProducedUnitSlots[slotIndex] = Entity.Null;

        return occupied;
    }

    private bool TryGetFactionRuntimeBuildingCenter(byte factionId, RuntimeBuildingData sourceBuilding, out int2 center)
    {
        center = default;
        int2 sum = default;
        int count = 0;
        foreach (KeyValuePair<int, RuntimeBuildingData> entry in _runtimeBuildings)
        {
            RuntimeBuildingData building = entry.Value;
            if (!IsOwnedRuntimeBuildingForFaction(building, factionId))
                continue;

            Vector2Int footprint = building.Definition != null ? building.Definition.FootprintCells : Vector2Int.one;
            sum += new int2(building.OriginCell.x + footprint.x / 2, building.OriginCell.y + footprint.y / 2);
            count++;
        }

        if (count > 0)
        {
            center = new int2(sum.x / count, sum.y / count);
            return true;
        }

        if (sourceBuilding != null)
        {
            Vector2Int footprint = sourceBuilding.Definition != null ? sourceBuilding.Definition.FootprintCells : Vector2Int.one;
            center = new int2(sourceBuilding.OriginCell.x + footprint.x / 2, sourceBuilding.OriginCell.y + footprint.y / 2);
            return true;
        }

        return false;
    }

    private bool TryQueuePlayerUnitFromBuilding(RuntimeBuildingData building, int productionIndex, GameObject spawnUnitPrefab)
    {
        if (building == null || spawnUnitPrefab == null)
            return false;

        building.PendingProductions ??= new List<RuntimeBuildingData.PendingProduction>();
        building.ProducedUnits ??= new List<Entity>();
        if (TryGetEntityManager(out EntityManager em))
        {
            for (int i = building.ProducedUnits.Count - 1; i >= 0; i--)
            {
                Entity unit = building.ProducedUnits[i];
                bool alive = unit != Entity.Null && em.Exists(unit);
                if (alive && em.HasComponent<UnitHealth>(unit))
                    alive = em.GetComponentData<UnitHealth>(unit).Current > 0;
                if (!alive)
                    building.ProducedUnits.RemoveAt(i);
            }
        }

        int reservedProductionSlotIndex = -1;
        if (building.ProductionSpawnLocalPositions != null &&
            building.ProducedUnitSlots != null &&
            building.ProductionSpawnLocalPositions.Length > 0)
        {
            int count = math.min(building.ProductionSpawnLocalPositions.Length, building.ProducedUnitSlots.Length);
            for (int i = 0; i < count; i++)
            {
                bool reservedByPending = false;
                for (int pendingIndex = 0; pendingIndex < building.PendingProductions.Count; pendingIndex++)
                {
                    RuntimeBuildingData.PendingProduction pending = building.PendingProductions[pendingIndex];
                    if (pending != null && pending.ReservedProductionSlotIndex == i)
                    {
                        reservedByPending = true;
                        break;
                    }
                }

                if (reservedByPending)
                    continue;

                Entity occupant = building.ProducedUnitSlots[i];
                bool occupied = occupant != Entity.Null && em.Exists(occupant);
                if (occupied && em.HasComponent<UnitHealth>(occupant))
                    occupied = em.GetComponentData<UnitHealth>(occupant).Current > 0;
                if (occupied)
                    continue;

                if (occupant != Entity.Null && !occupied)
                    building.ProducedUnitSlots[i] = Entity.Null;

                reservedProductionSlotIndex = i;
                break;
            }

            bool allowUnreservedHelicopterHelipadSpawn =
                IsHelicopterUnitPrefab(spawnUnitPrefab) &&
                building.HasOwnerFaction &&
                IsRuntimeBuildingId(building, "Building_Helipad");
            if (reservedProductionSlotIndex < 0 && !allowUnreservedHelicopterHelipadSpawn)
                return false;
        }

        float now = Time.time;
        ResolveProductionTransportSettings(
            spawnUnitPrefab,
            out GameObject transportPrefab,
            out float transportArrivalSeconds,
            out float transportHoldForNextReadySeconds,
            out int transportMaxConcurrent,
            out ProductionTransportMode transportMode,
            out bool transportRequiresAirportRunway);
        building.PendingProductions.Add(new RuntimeBuildingData.PendingProduction
        {
            ProductionIndex = productionIndex,
            Prefab = spawnUnitPrefab,
            StartedAt = now,
            ReadyAt = now + ResolveProductionDurationSeconds(spawnUnitPrefab),
            ReservedProductionSlotIndex = reservedProductionSlotIndex,
            TransportPrefab = transportPrefab,
            TransportArrivalSeconds = transportArrivalSeconds,
            TransportHoldForNextReadySeconds = transportHoldForNextReadySeconds,
            TransportMaxConcurrent = transportMaxConcurrent,
            TransportMode = transportMode,
            TransportRequiresAirportRunway = transportRequiresAirportRunway
        });
        return true;
    }

    private void ProcessPendingProductions()
    {
        if (_runtimeBuildings.Count == 0)
            return;

        float now = Time.time;
        foreach (var pair in _runtimeBuildings)
        {
            RuntimeBuildingData building = pair.Value;
            if (building == null || building.PendingProductions == null || building.PendingProductions.Count == 0)
            {
                UpdateActiveProductionTransport(building, now);
                continue;
            }

            UpdateActiveProductionTransport(building, now);

            for (int i = building.PendingProductions.Count - 1; i >= 0; i--)
            {
                RuntimeBuildingData.PendingProduction pending = building.PendingProductions[i];
                if (pending == null)
                {
                    building.PendingProductions.RemoveAt(i);
                    continue;
                }

                if (pending.TransportPrefab != null)
                {
                    float launchAt = pending.ReadyAt - Mathf.Max(0.5f, pending.TransportArrivalSeconds);
                    if (now >= launchAt)
                    {
                        if (TryEnsureActiveProductionTransport(building, pending))
                        {
                        }
                        else
                        {
                            pending.StartedAt += Time.deltaTime;
                            pending.ReadyAt += Time.deltaTime;
                        }
                    }
                    continue;
                }

                if (now < pending.ReadyAt)
                    continue;

                if (TrySpawnPlayerUnitNearBuilding(building, pending.ProductionIndex, pending.ReservedProductionSlotIndex))
                    building.PendingProductions.RemoveAt(i);
            }
        }
    }

    private bool TryEnsureActiveProductionTransport(RuntimeBuildingData building, RuntimeBuildingData.PendingProduction pending)
    {
        if (building == null || pending == null || pending.TransportPrefab == null || building.ActiveTransport != null)
            return building?.ActiveTransport != null;

        Vector3 hoverPosition;
        Vector3 entryPosition;
        Vector3 touchdownPosition;
        Vector3 exitPosition;
        Quaternion hoverRotation;
        Quaternion entryRotation;
        Quaternion exitRotation;
        int laneIndex = 0;

        if (pending.TransportMode == ProductionTransportMode.Plane)
        {
            if (!TryGetNearestAirportRunway(
                building.Instance != null ? building.Instance.transform.position : Vector3.zero,
                out _,
                out Vector3 runwayCenter,
                out Quaternion runwayRotation,
                out Vector3 runwayHalfExtents))
            {
                return false;
            }

            if (!TryAcquireProductionTransportLane(pending.TransportPrefab, pending.TransportMaxConcurrent, out laneIndex))
                return false;

            Vector3 runwayAxis = runwayRotation * Vector3.forward;
            runwayAxis.y = 0f;
            if (runwayAxis.sqrMagnitude <= 0.0001f)
                runwayAxis = Vector3.forward;
            runwayAxis.Normalize();

            float runwayHalfLength = Mathf.Max(8f, runwayHalfExtents.z);
            Vector3 runwayStart = runwayCenter - (runwayAxis * runwayHalfLength);
            touchdownPosition = runwayStart + (runwayAxis * Mathf.Min(8f, runwayHalfLength * 0.35f));
            touchdownPosition.y = Mathf.Max(0.5f, runwayCenter.y + 0.25f);
            hoverPosition = runwayCenter;
            hoverPosition.y = touchdownPosition.y;
            entryPosition = touchdownPosition - (runwayAxis * Mathf.Max(80f, runwayHalfExtents.z * 5f)) + new Vector3(0f, 28f, 0f);
            exitPosition = hoverPosition + (runwayAxis * Mathf.Max(90f, runwayHalfExtents.z * 6f)) + new Vector3(0f, 32f, 0f);
            hoverRotation = Quaternion.LookRotation(runwayAxis, Vector3.up);
            entryRotation = hoverRotation;
            exitRotation = hoverRotation;
        }
        else if (pending.TransportMode == ProductionTransportMode.AirSelf)
        {
            if (!TryAcquireProductionTransportLane(pending.TransportPrefab, pending.TransportMaxConcurrent, out laneIndex))
                return false;

            touchdownPosition = ResolveProductionTransportDropPosition(building, pending);
            hoverPosition = touchdownPosition + new Vector3(0f, 6f, 0f);
            hoverPosition += ResolveProductionTransportLaneOffset(laneIndex, pending.TransportMaxConcurrent);
            Vector3 horizontalOffset = worldCamera != null
                ? -worldCamera.transform.right.normalized * 70f
                : new Vector3(-70f, 0f, 0f);
            entryPosition = hoverPosition + horizontalOffset + new Vector3(0f, 16f, 0f);
            exitPosition = hoverPosition;
            hoverRotation = Quaternion.LookRotation((hoverPosition - entryPosition).normalized, Vector3.up);
            entryRotation = hoverRotation;
            exitRotation = hoverRotation;
        }
        else
        {
            if (!TryAcquireProductionTransportLane(pending.TransportPrefab, pending.TransportMaxConcurrent, out laneIndex))
                return false;

            hoverPosition = ResolveProductionTransportHoverPosition(building, pending);
            hoverPosition += ResolveProductionTransportLaneOffset(laneIndex, pending.TransportMaxConcurrent);
            Vector3 horizontalOffset = worldCamera != null
                ? -worldCamera.transform.right.normalized * 60f
                : new Vector3(-60f, 0f, 0f);
            entryPosition = hoverPosition + horizontalOffset;
            exitPosition = hoverPosition - horizontalOffset;
            entryPosition.y = hoverPosition.y + 12f;
            exitPosition.y = hoverPosition.y + 12f;
            touchdownPosition = hoverPosition;
            hoverRotation = Quaternion.LookRotation((hoverPosition - entryPosition).normalized, Vector3.up);
            entryRotation = hoverRotation;
            exitRotation = Quaternion.LookRotation((exitPosition - hoverPosition).normalized, Vector3.up);
        }

        GameObject instance = Instantiate(pending.TransportPrefab);
        instance.name = $"{pending.TransportPrefab.name}_Delivery_{building.Id}";
        HideTransportRuntimeMarkers(instance.transform);
        Transform doorTransform = _buildingVisualSystem.FindDescendantByName(instance.transform, "Door_X");

        RuntimeBuildingData.ActiveProductionTransport transport = new RuntimeBuildingData.ActiveProductionTransport
        {
            LaneIndex = laneIndex,
            Prefab = pending.TransportPrefab,
            Instance = instance,
            Transform = instance.transform,
            DoorTransform = doorTransform,
            DoorOpenLocalEulerX = doorTransform != null ? doorTransform.localEulerAngles.x : 0f,
            HoverPosition = hoverPosition,
            EntryPosition = entryPosition,
            TouchdownPosition = touchdownPosition,
            ExitPosition = exitPosition,
            HoverRotation = hoverRotation,
            EntryRotation = entryRotation,
            ExitRotation = exitRotation,
            ArrivalSeconds = Mathf.Max(0.5f, pending.TransportArrivalSeconds),
            HoldForNextReadySeconds = Mathf.Max(0.5f, pending.TransportHoldForNextReadySeconds),
            PhaseStartedAt = Time.time,
            HoverEnteredAt = -1f,
            NextDropReadyAt = Time.time,
            Phase = 0,
            Mode = pending.TransportMode
        };

        transport.Transform.position = transport.EntryPosition;
        transport.Transform.rotation = transport.EntryRotation;
        SetProductionTransportDoorOpen01(transport, 0f);
        building.ActiveTransport = transport;
        return true;
    }

    private void UpdateActiveProductionTransport(RuntimeBuildingData building, float now)
    {
        if (building == null || building.ActiveTransport == null || building.ActiveTransport.Transform == null)
            return;

        RuntimeBuildingData.ActiveProductionTransport transport = building.ActiveTransport;
        if (transport.Mode == ProductionTransportMode.Helicopter || transport.Mode == ProductionTransportMode.AirSelf)
            RotateProductionTransportBlades(transport.Transform, Time.deltaTime);

        switch (transport.Phase)
        {
            case 0:
            {
                float duration = Mathf.Max(0.5f, transport.ArrivalSeconds);
                float t = Mathf.Clamp01((now - transport.PhaseStartedAt) / duration);
                if (transport.Mode == ProductionTransportMode.Plane)
                {
                    if (t < 0.65f)
                    {
                        float landingT = t / 0.65f;
                        transport.Transform.position = Vector3.Lerp(transport.EntryPosition, transport.TouchdownPosition, landingT);
                    }
                    else
                    {
                        float taxiT = (t - 0.65f) / 0.35f;
                        transport.Transform.position = Vector3.Lerp(transport.TouchdownPosition, transport.HoverPosition, taxiT);
                    }
                }
                else
                {
                    transport.Transform.position = Vector3.Lerp(transport.EntryPosition, transport.HoverPosition, t);
                }
                transport.Transform.rotation = Quaternion.Slerp(transport.EntryRotation, transport.HoverRotation, t);
                if (transport.Mode == ProductionTransportMode.Plane)
                    SetProductionTransportDoorOpen01(transport, 0f);

                if (t >= 1f)
                {
                    transport.Phase = 1;
                    transport.PhaseStartedAt = now;
                    transport.HoverEnteredAt = now;
                    transport.NextDropReadyAt = transport.Mode == ProductionTransportMode.Plane ? now + 2f : now;
                }
                break;
            }

            case 1:
            {
                if (transport.Mode == ProductionTransportMode.AirSelf)
                {
                    float landingT = Mathf.Clamp01((now - transport.PhaseStartedAt) / 1.5f);
                    transport.Transform.position = Vector3.Lerp(transport.HoverPosition, transport.TouchdownPosition, landingT);
                    transport.Transform.rotation = transport.HoverRotation;

                    if (landingT < 1f)
                        break;
                }
                else
                {
                    transport.Transform.position = transport.HoverPosition;
                    transport.Transform.rotation = transport.HoverRotation;
                }

                if (transport.Mode == ProductionTransportMode.Plane)
                    SetProductionTransportDoorOpen01(transport, Mathf.Clamp01((now - transport.PhaseStartedAt) / 1.25f));

                if (transport.Mode == ProductionTransportMode.AirSelf)
                {
                    RuntimeBuildingData.PendingProduction readyAirPending = FindNextReadyTransportPending(building, transport.Prefab, now);
                    if (readyAirPending != null)
                    {
                        int2 airCell = ResolveProductionGroundGoalCell(building, readyAirPending, transport.TouchdownPosition);
                        if (TrySpawnPlayerUnitNearBuilding(building, readyAirPending.ProductionIndex, readyAirPending.ReservedProductionSlotIndex, transport.TouchdownPosition, airCell))
                        {
                            int pendingIndex = building.PendingProductions.IndexOf(readyAirPending);
                            if (pendingIndex >= 0)
                                building.PendingProductions.RemoveAt(pendingIndex);
                            AlignNewestProducedUnitRotation(building, transport.Transform.forward);
                        }

                        if (transport.Instance != null)
                            Destroy(transport.Instance);
                        building.ActiveTransport = null;
                        return;
                    }
                }

                if (transport.Mode == ProductionTransportMode.Plane)
                {
                    RuntimeBuildingData.PendingProduction readySelfArrivalPending = FindNextReadyTransportPending(building, transport.Prefab, now);
                    if (readySelfArrivalPending != null && readySelfArrivalPending.Prefab == transport.Prefab)
                    {
                        Vector3 runwaySpawnPosition = transport.HoverPosition;
                        int2 runwayCell = ResolveProductionGroundGoalCell(building, readySelfArrivalPending, runwaySpawnPosition);
                        int2 finalGoalCell = ResolveProductionGroundGoalCell(
                            building,
                            readySelfArrivalPending,
                            ResolveProductionTransportDropPosition(building, readySelfArrivalPending));

                        if (TrySpawnPlayerUnitNearBuilding(
                            building,
                            readySelfArrivalPending.ProductionIndex,
                            readySelfArrivalPending.ReservedProductionSlotIndex,
                            runwaySpawnPosition,
                            runwayCell))
                        {
                            int pendingIndex = building.PendingProductions.IndexOf(readySelfArrivalPending);
                            if (pendingIndex >= 0)
                                building.PendingProductions.RemoveAt(pendingIndex);

                            AlignNewestProducedUnitRotation(building, transport.Transform.forward);
                            if (TryGetEntityManager(out EntityManager em) &&
                                building.ProducedUnits != null &&
                                building.ProducedUnits.Count > 0)
                            {
                                Entity newest = building.ProducedUnits[building.ProducedUnits.Count - 1];
                                if (newest != Entity.Null && em.Exists(newest))
                                {
                                    if (!em.HasComponent<UnitSpawnTransitTag>(newest))
                                        em.AddComponent<UnitSpawnTransitTag>(newest);

                                    if (em.HasComponent<UnitAirState>(newest))
                                    {
                                        UnitAirState airState = em.GetComponentData<UnitAirState>(newest);
                                        airState.UsesRunway = 1;
                                        airState.RunwayTakeoffPosition = transport.TouchdownPosition;
                                        airState.RunwayTakeoffCell = ResolveProductionGroundGoalCell(building, readySelfArrivalPending, transport.TouchdownPosition);
                                        airState.RunwayLandingPosition = transport.HoverPosition;
                                        airState.RunwayLandingCell = runwayCell;
                                        airState.Airborne = 0;
                                        airState.ReturningHome = 0;
                                        em.SetComponentData(newest, airState);
                                    }
                                }
                            }
                            MoveNewestProducedUnitToCell(building, finalGoalCell);
                        }

                        if (transport.Instance != null)
                            Destroy(transport.Instance);
                        building.ActiveTransport = null;
                        return;
                    }
                }

                if (transport.ActiveDrop != null)
                {
                    UpdateActiveTransportDrop(building, transport, now);
                }
                else
                {
                    RuntimeBuildingData.PendingProduction readyPending = FindNextReadyTransportPending(building, transport.Prefab, now);
                    if (readyPending != null && now >= transport.NextDropReadyAt)
                    {
                        StartActiveTransportDrop(building, transport, readyPending, now);
                    }
                    else
                    {
                        RuntimeBuildingData.PendingProduction soonPending = FindNextSoonTransportPending(building, transport.Prefab, now, transport.HoldForNextReadySeconds);
                        bool shouldDepart = soonPending == null && now >= transport.HoverEnteredAt + transport.HoldForNextReadySeconds;
                        if (shouldDepart)
                        {
                            transport.Phase = 2;
                            transport.PhaseStartedAt = now;
                        }
                    }
                }
                break;
            }

            case 2:
            {
                float duration = Mathf.Max(0.5f, transport.ArrivalSeconds);
                float t = Mathf.Clamp01((now - transport.PhaseStartedAt) / duration);
                transport.Transform.position = Vector3.Lerp(transport.HoverPosition, transport.ExitPosition, t);
                transport.Transform.rotation = Quaternion.Slerp(transport.HoverRotation, transport.ExitRotation, t);
                if (transport.Mode == ProductionTransportMode.Plane)
                    SetProductionTransportDoorOpen01(transport, 1f - t);

                if (t >= 1f)
                {
                    if (transport.Instance != null)
                        Destroy(transport.Instance);
                    building.ActiveTransport = null;
                    return;
                }
                break;
            }
        }
    }

    private bool TryAcquireProductionTransportLane(GameObject transportPrefab, int maxConcurrent, out int laneIndex)
    {
        int safeMax = Mathf.Max(1, maxConcurrent);
        bool[] used = new bool[safeMax];
        foreach (var pair in _runtimeBuildings)
        {
            RuntimeBuildingData.ActiveProductionTransport transport = pair.Value?.ActiveTransport;
            if (transport == null || transport.Prefab != transportPrefab)
                continue;

            if (transport.LaneIndex >= 0 && transport.LaneIndex < used.Length)
                used[transport.LaneIndex] = true;
        }

        for (int i = 0; i < used.Length; i++)
        {
            if (used[i])
                continue;

            laneIndex = i;
            return true;
        }

        laneIndex = -1;
        return false;
    }

    private Vector3 ResolveProductionTransportLaneOffset(int laneIndex, int maxConcurrent)
    {
        int safeMax = Mathf.Max(1, maxConcurrent);
        float centered = laneIndex - ((safeMax - 1) * 0.5f);
        Vector3 axis = worldCamera != null
            ? worldCamera.transform.forward.normalized
            : Vector3.forward;
        axis.y = 0f;
        if (axis.sqrMagnitude <= 0.0001f)
            axis = Vector3.forward;
        axis.Normalize();
        return axis * (centered * ProductionTransportLaneSpacing);
    }

    private void StartActiveTransportDrop(RuntimeBuildingData building, RuntimeBuildingData.ActiveProductionTransport transport, RuntimeBuildingData.PendingProduction pending, float now)
    {
        if (building == null || transport == null || pending == null)
            return;

        Vector3 dropStartPosition = transport.Mode == ProductionTransportMode.Plane
            ? ResolvePlaneTransportInteriorWorldPosition(transport)
            : transport.HoverPosition;
        Vector3 finalSpawnPosition = ResolveProductionTransportDropPosition(building, pending);
        Vector3 dropEndPosition = transport.Mode == ProductionTransportMode.Plane
            ? ResolvePlaneTransportRolloutWorldPosition(transport)
            : finalSpawnPosition;
        int2 finalGoalCell = transport.Mode == ProductionTransportMode.Plane
            ? ResolveProductionGroundGoalCell(building, pending, finalSpawnPosition)
            : ResolveProductionGroundGoalCell(building, pending, dropEndPosition);

        GameObject visual = Instantiate(pending.Prefab);
        visual.name = $"{pending.Prefab.name}_TransportDrop";
        HideTransportRuntimeMarkers(visual.transform);
        ApplyTemporaryCharacterIdlePose(visual);

        if (visual.TryGetComponent<UnitGridAuthoring>(out UnitGridAuthoring authoring))
            authoring.enabled = false;

        visual.transform.position = dropStartPosition;
        if (transport.Mode == ProductionTransportMode.Plane && transport.Transform != null)
            visual.transform.rotation = Quaternion.LookRotation(-transport.Transform.forward, Vector3.up);

        LineRenderer rope = null;
        if (transport.Mode == ProductionTransportMode.Helicopter)
        {
            rope = new GameObject("TransportDropRope").AddComponent<LineRenderer>();
            rope.transform.SetParent(transport.Transform, false);
            rope.positionCount = 2;
            rope.widthMultiplier = 0.05f;
            rope.material = new Material(Shader.Find("Sprites/Default"));
            rope.startColor = new Color(0.82f, 0.82f, 0.82f, 0.95f);
            rope.endColor = rope.startColor;
        }

        transport.ActiveDrop = new RuntimeBuildingData.PendingDropVisual
        {
            Production = pending,
            Visual = visual,
            Rope = rope,
            StartedAt = now,
            Duration = transport.Mode == ProductionTransportMode.Plane ? 3f : 2f,
            StartPosition = dropStartPosition,
            EndPosition = dropEndPosition,
            FinalGoalCell = finalGoalCell
        };
    }

    private static void ApplyTemporaryCharacterIdlePose(GameObject visual)
    {
        if (visual == null || !visual.name.StartsWith("Unit_Chr_", System.StringComparison.Ordinal))
            return;

        MaterialAnimatorIndexAuthoring indexAuthoring = visual.GetComponentInChildren<MaterialAnimatorIndexAuthoring>(true);
        if (indexAuthoring == null || indexAuthoring.animator == null)
            return;

        MaterialAnimatorAuthoring animatorAuthoring = indexAuthoring.animator.GetComponent<MaterialAnimatorAuthoring>();
        if (animatorAuthoring == null || animatorAuthoring.animations == null || animatorAuthoring.animations.Count < 2)
            return;

        MaterialAnimatorBake idleAnimation = animatorAuthoring.animations[1];
        int startPixel = idleAnimation.start;
        int endPixel = startPixel + Mathf.Max(1, idleAnimation.frames);
        Transform animatedRoot = indexAuthoring.transform;
        LODGroup lodGroup = animatedRoot.GetComponentInChildren<LODGroup>(true);
        if (lodGroup == null)
            return;

        MaterialPropertyBlock propertyBlock = new();
        int modelShownId = Shader.PropertyToID("_SnivelerModelShown");
        int renderPixelId = Shader.PropertyToID("_SnivelerRenderPixel");
        var lods = lodGroup.GetLODs();
        for (int i = 0; i < lods.Length; ++i)
        {
            if (lods[i].renderers == null)
                continue;

            for (int rendererIndex = 0; rendererIndex < lods[i].renderers.Length; rendererIndex++)
            {
                Renderer lodRenderer = lods[i].renderers[rendererIndex];
                if (lodRenderer == null)
                    continue;

                for (int materialIndex = 0; materialIndex < lodRenderer.sharedMaterials.Length; materialIndex++)
                {
                    lodRenderer.GetPropertyBlock(propertyBlock, materialIndex);
                    propertyBlock.SetFloat(modelShownId, 1f);
                    propertyBlock.SetVector(renderPixelId, new Vector4(startPixel, endPixel, 0f, 0f));
                    lodRenderer.SetPropertyBlock(propertyBlock, materialIndex);
                }
            }
        }
    }

    private void UpdateActiveTransportDrop(RuntimeBuildingData building, RuntimeBuildingData.ActiveProductionTransport transport, float now)
    {
        RuntimeBuildingData.PendingDropVisual drop = transport.ActiveDrop;
        if (drop == null)
            return;

        float t = Mathf.Clamp01((now - drop.StartedAt) / Mathf.Max(0.01f, drop.Duration));
        Vector3 unitPosition = Vector3.Lerp(drop.StartPosition, drop.EndPosition, t);
        if (transport.Mode == ProductionTransportMode.Plane)
            unitPosition.y = Mathf.Lerp(drop.StartPosition.y, drop.EndPosition.y, Mathf.SmoothStep(0f, 1f, t));

        if (drop.Visual != null)
        {
            drop.Visual.transform.position = unitPosition;
            if (transport.Mode == ProductionTransportMode.Plane && transport.Transform != null)
            {
                Vector3 rolloutDirection = -transport.Transform.forward;
                rolloutDirection.y = 0f;
                if (rolloutDirection.sqrMagnitude > 0.0001f)
                {
                    rolloutDirection.Normalize();
                    float pitch = Mathf.Lerp(26f, 0f, Mathf.SmoothStep(0f, 1f, t));
                    drop.Visual.transform.rotation = Quaternion.LookRotation(rolloutDirection, Vector3.up) * Quaternion.Euler(pitch, 0f, 0f);
                }
            }
        }
        if (drop.Rope != null)
        {
            drop.Rope.SetPosition(0, ResolveTransportVisualCenterWorld(transport));
            drop.Rope.SetPosition(1, unitPosition);
        }

        if (t < 1f)
            return;

        if (drop.Visual != null)
            Destroy(drop.Visual);
        if (drop.Rope != null)
            Destroy(drop.Rope.gameObject);

        RuntimeBuildingData.PendingProduction production = drop.Production;
        int pendingIndex = building.PendingProductions.IndexOf(production);
        if (pendingIndex >= 0)
            building.PendingProductions.RemoveAt(pendingIndex);

        if (transport.Mode == ProductionTransportMode.Plane)
        {
            int2 startCell = ResolveProductionGroundGoalCell(building, production, drop.EndPosition);
            if (TrySpawnPlayerUnitNearBuilding(building, production.ProductionIndex, production.ReservedProductionSlotIndex, drop.EndPosition, startCell))
            {
                AlignNewestProducedUnitRotation(building, -transport.Transform.forward);
                MoveNewestProducedUnitToCell(building, drop.FinalGoalCell);
            }
        }
        else if (TrySpawnPlayerUnitNearBuilding(building, production.ProductionIndex, production.ReservedProductionSlotIndex))
        {
            MoveNewestProducedUnitToCell(building, drop.FinalGoalCell);
        }

        transport.ActiveDrop = null;
        transport.NextDropReadyAt = now;
    }

    private static Vector3 ResolveTransportVisualCenterWorld(RuntimeBuildingData.ActiveProductionTransport transport)
    {
        if (transport?.Instance == null)
            return transport?.Transform != null ? transport.Transform.position : Vector3.zero;

        Renderer[] renderers = transport.Instance.GetComponentsInChildren<Renderer>(true);
        bool hasBounds = false;
        Bounds bounds = default;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
                continue;

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        if (hasBounds)
            return bounds.center;

        Transform model = transport.Instance.transform.Find("Model");
        if (model != null)
            return model.position;

        return transport.Transform != null ? transport.Transform.position : transport.Instance.transform.position;
    }

    private bool TryGetNearestAirportRunway(
        Vector3 origin,
        out RuntimeBuildingData airport,
        out Vector3 runwayCenter,
        out Quaternion runwayRotation,
        out Vector3 runwayHalfExtents)
    {
        airport = null;
        runwayCenter = Vector3.zero;
        runwayRotation = Quaternion.identity;
        runwayHalfExtents = new Vector3(8f, 0.5f, 24f);
        float bestDistance = float.PositiveInfinity;

        foreach (var pair in _runtimeBuildings)
        {
            RuntimeBuildingData candidate = pair.Value;
            if (candidate == null || candidate.IsDestroyed || candidate.Instance == null || candidate.Definition == null || !candidate.Definition.HasRunway)
                continue;

            Vector3 candidateCenter = candidate.Instance.transform.TransformPoint(candidate.Definition.RunwayLocalPosition);
            float distance = (candidateCenter - origin).sqrMagnitude;
            if (distance >= bestDistance)
                continue;

            bestDistance = distance;
            airport = candidate;
            runwayCenter = candidateCenter;
            runwayRotation = candidate.Instance.transform.rotation * candidate.Definition.RunwayLocalRotation;
            runwayHalfExtents = Vector3.Scale(candidate.Definition.RunwayHalfExtents, candidate.Instance.transform.lossyScale);
        }

        return airport != null;
    }

    private static void SetProductionTransportDoorOpen01(RuntimeBuildingData.ActiveProductionTransport transport, float open01)
    {
        if (transport?.DoorTransform == null)
            return;

        Vector3 localEuler = transport.DoorTransform.localEulerAngles;
        localEuler.x = Mathf.LerpAngle(0f, transport.DoorOpenLocalEulerX, Mathf.Clamp01(open01));
        transport.DoorTransform.localEulerAngles = localEuler;
    }

    private static Vector3 ResolvePlaneTransportDoorWorldPosition(RuntimeBuildingData.ActiveProductionTransport transport)
    {
        if (transport?.DoorTransform != null)
        {
            Vector3 localPosition = transport.DoorTransform.localPosition;
            localPosition.x = 0f;
            return transport.Transform.TransformPoint(localPosition);
        }
        if (transport?.Transform != null)
            return transport.Transform.position - (transport.Transform.forward * 6f);
        return Vector3.zero;
    }

    private static Vector3 ResolvePlaneTransportInteriorWorldPosition(RuntimeBuildingData.ActiveProductionTransport transport)
    {
        Vector3 doorPosition = ResolvePlaneTransportDoorWorldPosition(transport);
        if (transport?.Transform == null)
            return doorPosition + new Vector3(0f, 1.2f, 5f);

        Vector3 inwardDirection = transport.Transform.forward;
        inwardDirection.y = 0f;
        if (inwardDirection.sqrMagnitude <= 0.0001f)
            inwardDirection = Vector3.forward;
        inwardDirection.Normalize();
        Vector3 interior = doorPosition + (inwardDirection * 9.5f);
        interior.y += 1.45f;
        return interior;
    }

    private static Vector3 ResolvePlaneTransportRolloutWorldPosition(RuntimeBuildingData.ActiveProductionTransport transport)
    {
        Vector3 doorPosition = ResolvePlaneTransportDoorWorldPosition(transport);
        if (transport?.Transform == null)
            return new Vector3(doorPosition.x, 0.5f, doorPosition.z);

        Vector3 backDirection = -transport.Transform.forward;
        backDirection.y = 0f;
        if (backDirection.sqrMagnitude <= 0.0001f)
            backDirection = Vector3.back;
        backDirection.Normalize();
        Vector3 rollout = doorPosition + (backDirection * 6f);
        rollout.y = 0.5f;
        return rollout;
    }

    private RuntimeBuildingData.PendingProduction FindNextReadyTransportPending(RuntimeBuildingData building, GameObject transportPrefab, float now)
    {
        if (building?.PendingProductions == null)
            return null;

        for (int i = 0; i < building.PendingProductions.Count; i++)
        {
            RuntimeBuildingData.PendingProduction pending = building.PendingProductions[i];
            if (pending == null || pending.TransportPrefab != transportPrefab)
                continue;
            if (now >= pending.ReadyAt)
                return pending;
        }

        return null;
    }

    private RuntimeBuildingData.PendingProduction FindNextSoonTransportPending(RuntimeBuildingData building, GameObject transportPrefab, float now, float maxSeconds)
    {
        if (building?.PendingProductions == null)
            return null;

        for (int i = 0; i < building.PendingProductions.Count; i++)
        {
            RuntimeBuildingData.PendingProduction pending = building.PendingProductions[i];
            if (pending == null || pending.TransportPrefab != transportPrefab)
                continue;
            float remaining = pending.ReadyAt - now;
            if (remaining > 0f && remaining <= maxSeconds)
                return pending;
        }

        return null;
    }

    private Vector3 ResolveProductionTransportHoverPosition(RuntimeBuildingData building, RuntimeBuildingData.PendingProduction pending)
    {
        return ResolveProductionTransportDropPosition(building, pending) + new Vector3(0f, 8f, 0f);
    }

    private Vector3 ResolveProductionTransportDropPosition(RuntimeBuildingData building, RuntimeBuildingData.PendingProduction pending)
    {
        if (building?.Instance != null &&
            pending != null &&
            pending.ReservedProductionSlotIndex >= 0 &&
            building.ProductionSpawnLocalPositions != null &&
            pending.ReservedProductionSlotIndex < building.ProductionSpawnLocalPositions.Length)
        {
            Vector3 slotWorld = building.Instance.transform.TransformPoint(building.ProductionSpawnLocalPositions[pending.ReservedProductionSlotIndex]);
            return new Vector3(slotWorld.x, 0.5f, slotWorld.z);
        }

        if (building?.Instance != null)
        {
            Vector3 position = building.Instance.transform.position + (building.Instance.transform.forward * 4f);
            return new Vector3(position.x, 0.5f, position.z);
        }

        return new Vector3(0f, 0.5f, 0f);
    }

    private int2 ResolveProductionGroundGoalCell(RuntimeBuildingData building, RuntimeBuildingData.PendingProduction pending, Vector3 worldPosition)
    {
        if (!TryGetGridData(out _, out GridConfig grid, out _, out _))
            return int2.zero;

        return GridUtils.WorldToCell(grid, worldPosition);
    }

    private void MoveNewestProducedUnitToCell(RuntimeBuildingData building, int2 goalCell)
    {
        if (building?.ProducedUnits == null || building.ProducedUnits.Count == 0)
            return;
        if (!TryGetEntityManager(out EntityManager em))
            return;

        Entity entity = building.ProducedUnits[building.ProducedUnits.Count - 1];
        if (entity == Entity.Null || !em.Exists(entity))
            return;

        bool isAirUnit = em.HasComponent<UnitAirMovement>(entity);
        bool isSpawnTransit = em.HasComponent<UnitSpawnTransitTag>(entity);
        if (isAirUnit && !isSpawnTransit)
            return;

        if (em.HasComponent<UnitTarget>(entity))
            em.SetComponentData(entity, new UnitTarget { Cell = goalCell });
        else
            em.AddComponentData(entity, new UnitTarget { Cell = goalCell });

        if (em.HasComponent<UnitPathRequest>(entity))
            em.SetComponentData(entity, new UnitPathRequest { Goal = goalCell });
        else
            em.AddComponentData(entity, new UnitPathRequest { Goal = goalCell });
    }

    private void AlignNewestProducedUnitRotation(RuntimeBuildingData building, Vector3 forward)
    {
        if (building?.ProducedUnits == null || building.ProducedUnits.Count == 0)
            return;
        if (!TryGetEntityManager(out EntityManager em))
            return;

        Entity entity = building.ProducedUnits[building.ProducedUnits.Count - 1];
        if (entity == Entity.Null || !em.Exists(entity) || !em.HasComponent<LocalTransform>(entity))
            return;

        forward.y = 0f;
        if (forward.sqrMagnitude <= 0.0001f)
            return;

        forward.Normalize();
        LocalTransform transform = em.GetComponentData<LocalTransform>(entity);
        transform.Rotation = quaternion.LookRotationSafe((float3)forward, math.up());
        em.SetComponentData(entity, transform);
    }

    private static void HideTransportRuntimeMarkers(Transform root)
    {
        if (root == null)
            return;

        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            string name = child.name;
            if (name == "Destroyed" || name == "SelectionMarker" || name == "FactionMarker")
                child.gameObject.SetActive(false);
        }
    }

    private static void RotateProductionTransportBlades(Transform root, float deltaTime)
    {
        if (root == null)
            return;

        float degrees = 1440f * deltaTime;
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            string name = child.name;
            if (name.EndsWith("_X", System.StringComparison.Ordinal))
                child.Rotate(Vector3.right, degrees, Space.Self);
            else if (name.EndsWith("_Y", System.StringComparison.Ordinal))
                child.Rotate(Vector3.up, degrees, Space.Self);
            else if (name.EndsWith("_Z", System.StringComparison.Ordinal))
                child.Rotate(Vector3.forward, degrees, Space.Self);
        }
    }

    private static float ResolveProductionDurationSeconds(GameObject spawnUnitPrefab)
    {
        if (spawnUnitPrefab == null)
            return 60f;

        UnitGridAuthoring authoring = spawnUnitPrefab.GetComponent<UnitGridAuthoring>();
        if (authoring == null)
            return 60f;

        return Mathf.Max(0.01f, authoring.ProductionDurationSeconds);
    }

    private void ResolveProductionTransportSettings(
        GameObject spawnUnitPrefab,
        out GameObject transportPrefab,
        out float arrivalSeconds,
        out float holdForNextReadySeconds,
        out int maxConcurrent,
        out ProductionTransportMode transportMode,
        out bool requiresAirportRunway)
    {
        transportPrefab = null;
        arrivalSeconds = 5f;
        holdForNextReadySeconds = 4f;
        maxConcurrent = 1;
        transportMode = ProductionTransportMode.Helicopter;
        requiresAirportRunway = false;
        if (spawnUnitPrefab == null)
            return;

        UnitGridAuthoring producedAuthoring = spawnUnitPrefab.GetComponent<UnitGridAuthoring>();
        transportPrefab = producedAuthoring != null ? producedAuthoring.ProductionTransportPrefab : null;

        if (transportPrefab == null)
            transportPrefab = TryResolveDefaultProductionTransportPrefab(spawnUnitPrefab);

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
            return;

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
    }

    private GameObject TryResolveDefaultProductionTransportPrefab(GameObject spawnUnitPrefab)
    {
        if (spawnUnitPrefab == null)
            return null;

        UnitGridAuthoring authoring = spawnUnitPrefab.GetComponent<UnitGridAuthoring>();
        if (authoring == null)
            return null;

        if (!_unitSpawnPrefabsByKey.TryGetValue(GetSpawnableLookupKey("Unit_Veh_Helicopter_Transport"), out GameObject helicopter))
        {
            foreach (GameObject candidate in unitSpawnPrefabs)
            {
                if (candidate == null || !string.Equals(candidate.name, "Unit_Veh_Helicopter_Transport", System.StringComparison.Ordinal))
                    continue;

                helicopter = candidate;
                break;
            }
        }

        if (helicopter == null)
            return null;

        if (authoring.IsAirUnit)
            return null;

        bool isLikelyVehicle = IsLikelyGroundVehiclePrefab(spawnUnitPrefab);
        if (!isLikelyVehicle)
            return helicopter;

        Vector2Int size = ResolveEffectiveProductionFootprintCells(spawnUnitPrefab, authoring);
        if (size.x > 1 || size.y > 1)
        {
            if (!_unitSpawnPrefabsByKey.TryGetValue(GetSpawnableLookupKey("Unit_Veh_Plane_Transport"), out GameObject plane))
            {
                foreach (GameObject candidate in unitSpawnPrefabs)
                {
                    if (candidate == null || !string.Equals(candidate.name, "Unit_Veh_Plane_Transport", System.StringComparison.Ordinal))
                        continue;

                    plane = candidate;
                    break;
                }
            }

            return plane;
        }

        return helicopter;
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

    private static Vector2Int ResolveEffectiveProductionFootprintCells(GameObject spawnUnitPrefab, UnitGridAuthoring authoring)
    {
        Vector2Int configured = authoring != null ? authoring.GetConfiguredFootprintCells() : Vector2Int.one;
        if (configured.x > 1 || configured.y > 1)
            return configured;

        if (TryGetPrefabLocalBounds(spawnUnitPrefab, out Bounds localBounds))
        {
            Vector2Int modelFootprint = new(
                Mathf.Max(1, Mathf.CeilToInt(localBounds.size.x)),
                Mathf.Max(1, Mathf.CeilToInt(localBounds.size.z)));
            if (modelFootprint.x > configured.x || modelFootprint.y > configured.y)
                return modelFootprint;
        }

        return configured;
    }

    private bool TrySpawnPlayerUnitNearBuilding(RuntimeBuildingData building, int productionIndex)
    {
        return TrySpawnPlayerUnitNearBuilding(building, productionIndex, -1, null, null);
    }

    private bool TrySpawnPlayerUnitNearBuilding(RuntimeBuildingData building, int productionIndex, int reservedProductionSlotIndex)
    {
        return TrySpawnPlayerUnitNearBuilding(building, productionIndex, reservedProductionSlotIndex, null, null);
    }

    private bool TrySpawnPlayerUnitNearBuilding(RuntimeBuildingData building, int productionIndex, int reservedProductionSlotIndex, Vector3? overrideWorldPosition, int2? overrideCell)
    {
        if (!TryGetEntityManager(out EntityManager em))
            return false;
        
        if (!TryGetGridData(out Entity gridEntity, out GridConfig grid, out _, out DynamicBlockerData blockerData))
            return false;
        
        GameObject spawnUnitPrefab = GetProductionPrefab(building.Definition, productionIndex);

        if (!TryGetSpawnUnitPrefabEntity(em, spawnUnitPrefab, out Entity prefabEntity))
        {
#if UNITY_EDITOR
            Debug.LogWarning($"[BuildingSpawn] Could not resolve ECS prefab entity for spawn prefab '{(spawnUnitPrefab != null ? spawnUnitPrefab.name : "<null>")}' from building '{building.Definition.DisplayName}'.");
#endif
            return false;
        }

        int2 unitFootprint = em.HasComponent<UnitFootprint>(prefabEntity)
            ? em.GetComponentData<UnitFootprint>(prefabEntity).Size
            : new int2(1, 1);
        bool isAirUnit = em.HasComponent<UnitAirMovement>(prefabEntity);
        bool useHelicopterSpawnResolver =
            !overrideWorldPosition.HasValue &&
            !overrideCell.HasValue &&
            isAirUnit &&
            IsHelicopterUnitPrefab(spawnUnitPrefab) &&
            building.HasOwnerFaction;
        int productionSlotIndex = -1;
        Vector3 productionSpawnLocalPosition = Vector3.zero;
        RuntimeBuildingData productionSlotBuilding = building;
        bool hasProductionSpawnSlots = building.ProductionSpawnLocalPositions != null &&
                                       building.ProducedUnitSlots != null &&
                                       building.ProductionSpawnLocalPositions.Length > 0;
        if (hasProductionSpawnSlots && !useHelicopterSpawnResolver)
        {
            if (reservedProductionSlotIndex >= 0 &&
                reservedProductionSlotIndex < building.ProductionSpawnLocalPositions.Length &&
                reservedProductionSlotIndex < building.ProducedUnitSlots.Length)
            {
                productionSlotIndex = reservedProductionSlotIndex;
                productionSpawnLocalPosition = building.ProductionSpawnLocalPositions[reservedProductionSlotIndex];
            }
            else if (!TryGetAvailableProductionSpawnSlot(building, em, out productionSlotIndex, out productionSpawnLocalPosition))
            {
                return false;
            }
        }

        var walkable = em.GetBuffer<GridWalkable>(gridEntity).AsNativeArray();
        var occupied = em.GetComponentData<DynamicOccupancyData>(gridEntity).Occupied;
        var reserved = new NativeBitArray(grid.Width * grid.Height, Allocator.Temp);
        try
        {
            _buildingSpawnRandomState = math.max(1u, _buildingSpawnRandomState + 1u);
            var rng = new Unity.Mathematics.Random(_buildingSpawnRandomState);
            Vector2Int size = building.Definition.FootprintCells;
            ReserveBuildingBuffer(ref reserved, grid, building.OriginCell, size, 1);
            ReserveRecentSpawnBuffers(ref reserved, grid);
            int2 center = new(building.OriginCell.x + size.x / 2, building.OriginCell.y + size.y / 2);
            int2 cell = center;
            float3 pos;
            if (overrideWorldPosition.HasValue && overrideCell.HasValue)
            {
                pos = overrideWorldPosition.Value;
                cell = overrideCell.Value;
            }
            else if (useHelicopterSpawnResolver)
            {
                if (!TryResolveHelicopterSpawnForFaction(
                        building.OwnerFactionId,
                        building,
                        em,
                        ref rng,
                        grid,
                        walkable,
                        blockerData.Blocked,
                        occupied,
                        ref reserved,
                        unitFootprint,
                        out cell,
                        out pos,
                        out productionSlotBuilding,
                        out productionSlotIndex))
                {
                    return false;
                }
            }
            else if (hasProductionSpawnSlots)
            {
                pos = building.Instance != null
                    ? (float3)building.Instance.transform.TransformPoint(productionSpawnLocalPosition)
                    : (float3)productionSpawnLocalPosition;
                cell = GridUtils.WorldToCell(grid, pos);
                if (!GridUtils.InBounds(cell, grid.Width, grid.Height))
                    return false;

                if (!isAirUnit)
                {
                    bool slotCellAvailable =
                        TryReserveSpawnCandidate(grid, walkable, blockerData.Blocked, occupied, ref reserved, cell, unitFootprint) &&
                        !OverlapsRecentSpawnReservation(cell, unitFootprint) &&
                        !OverlapsExistingUnitFootprint(em, cell, unitFootprint);

                    if (!slotCellAvailable)
                    {
                        int radius = math.max(size.x, size.y) + math.max(unitFootprint.x, unitFootprint.y) + 6;
                        bool foundNearby = TryFindStrictSpawnCell(
                            em,
                            ref rng,
                            grid,
                            walkable,
                            blockerData.Blocked,
                            occupied,
                            ref reserved,
                            cell,
                            radius,
                            unitFootprint,
                            out cell);
                        if (!foundNearby)
                            return false;

                        pos = GridUtils.CellToWorldCenter(grid, cell);
                    }
                    else
                    {
                        pos = GridUtils.CellToWorldCenter(grid, cell);
                    }
                }
            }
            else if (isAirUnit)
            {
                int frontX = math.clamp(building.OriginCell.x + size.x / 2, 0, grid.Width - 1);
                int frontY = math.clamp(building.OriginCell.y + size.y, 0, grid.Height - 1);
                cell = new int2(frontX, frontY);
                pos = GridUtils.CellToWorldCenter(grid, cell);
            }
            else
            {
                int radius = math.max(size.x, size.y) + 4;
                bool foundAdjacent = TryFindStrictSpawnCellAdjacentToBuilding(
                    em,
                    ref rng,
                    grid,
                    walkable,
                    blockerData.Blocked,
                    occupied,
                    ref reserved,
                    building.OriginCell,
                    size,
                    unitFootprint,
                    out cell);
                if (!foundAdjacent &&
                    !TryFindStrictSpawnCell(em, ref rng, grid, walkable, blockerData.Blocked, occupied, ref reserved, center, radius + math.max(unitFootprint.x, unitFootprint.y), unitFootprint, out cell))
                    return false;

                pos = GridUtils.CellToWorldCenter(grid, cell);
            }

            Entity instance = em.Instantiate(prefabEntity);
            em.SetComponentData(instance, new UnitGrid { Cell = cell });
            em.SetComponentData(instance, LocalTransform.FromPosition(pos));
            building.ProducedUnits ??= new List<Entity>();
            building.ProducedUnitPrefabs ??= new Dictionary<Entity, GameObject>();
            building.ProducedUnits.Add(instance);
            building.ProducedUnitPrefabs[instance] = spawnUnitPrefab;
            if (!isAirUnit)
            {
                ReserveDynamicOccupancy(gridEntity, grid, cell, unitFootprint);
                AddRecentSpawnReservation(cell, unitFootprint);
            }
            if (productionSlotIndex >= 0 &&
                productionSlotBuilding?.ProducedUnitSlots != null &&
                productionSlotIndex < productionSlotBuilding.ProducedUnitSlots.Length)
            {
                productionSlotBuilding.ProducedUnitSlots[productionSlotIndex] = instance;
            }

            if (em.HasComponent<UnitGridInitialized>(instance))
                em.RemoveComponent<UnitGridInitialized>(instance);
            if (em.HasComponent<UnitPrevWorldPos>(instance))
                em.SetComponentData(instance, new UnitPrevWorldPos { Value = pos });
            if (em.HasComponent<UnitAirState>(instance))
            {
                em.SetComponentData(instance, new UnitAirState
                {
                    HomePosition = pos,
                    HomeCell = cell,
                    HomeInitialized = 1,
                    ReturningHome = 0,
                    Airborne = 0
                });
            }
            if (em.HasComponent<UnitMoveVisualState>(instance))
                em.SetComponentData(instance, new UnitMoveVisualState { IsMoving = 0, StillSeconds = 0f });
            if (em.HasComponent<Faction>(instance))
                em.SetComponentData(instance, new Faction { Id = building.HasOwnerFaction ? building.OwnerFactionId : (byte)0 });
            if (em.HasComponent<UnitRespawnPrefab>(instance))
                em.SetComponentData(instance, new UnitRespawnPrefab { Prefab = Entity.Null });
            if (em.HasComponent<UnitAttackState>(instance))
                em.SetComponentData(instance, new UnitAttackState { CooldownRemaining = 0f });
            if (em.HasComponent<UnitIdleWanderState>(instance))
            {
                _buildingSpawnRandomState = math.max(1u, _buildingSpawnRandomState + 1u);
                em.SetComponentData(instance, new UnitIdleWanderState
                {
                    RandomState = _buildingSpawnRandomState,
                    RetrySeconds = 0f,
                    CurrentIdleDelaySeconds = 0f
                });
            }
            if (em.HasComponent<UnitMovementBehavior>(instance) && em.GetComponentData<UnitMovementBehavior>(instance).AllowIdleWander == 0)
            {
                if (em.HasComponent<AutoWanderMoveTag>(instance))
                    em.RemoveComponent<AutoWanderMoveTag>(instance);
            }
            if (em.HasComponent<UnitPathFollow>(instance))
                em.RemoveComponent<UnitPathFollow>(instance);
            if (em.HasComponent<UnitPathRange>(instance))
                em.RemoveComponent<UnitPathRange>(instance);
            if (em.HasComponent<EngageTarget>(instance))
                em.RemoveComponent<EngageTarget>(instance);
            if (em.HasComponent<UnitPathRequest>(instance))
                em.RemoveComponent<UnitPathRequest>(instance);
            if (em.HasComponent<UnitTarget>(instance))
                em.RemoveComponent<UnitTarget>(instance);
            if (em.HasComponent<AutoWanderMoveTag>(instance))
                em.RemoveComponent<AutoWanderMoveTag>(instance);
            if (em.HasComponent<SelectedUnitTag>(instance))
                em.RemoveComponent<SelectedUnitTag>(instance);
            return true;
        }
        finally
        {
            reserved.Dispose();
        }
    }

    private void ReserveRecentSpawnBuffers(ref NativeBitArray reserved, GridConfig grid)
    {
        if (_recentSpawnReservations.Count == 0)
            return;

        float now = Time.time;
        for (int i = 0; i < _recentSpawnReservations.Count; i++)
        {
            RecentSpawnReservation reservation = _recentSpawnReservations[i];
            if (reservation == null || reservation.ExpiresAt <= now)
                continue;

            int2 size = UnitFootprintUtility.ClampSize(reservation.Size);
            int2 min = UnitFootprintUtility.GetMinCell(reservation.Cell, size);
            int2 max = min + size;
            for (int y = min.y; y < max.y; y++)
            {
                if ((uint)y >= (uint)grid.Height)
                    continue;

                int row = y * grid.Width;
                for (int x = min.x; x < max.x; x++)
                {
                    if ((uint)x >= (uint)grid.Width)
                        continue;

                    reserved.Set(row + x, true);
                }
            }
        }
    }

    private void AddRecentSpawnReservation(int2 cell, int2 size)
    {
        _recentSpawnReservations.Add(new RecentSpawnReservation
        {
            Cell = cell,
            Size = UnitFootprintUtility.ClampSize(size),
            ExpiresAt = Time.time + 0.5f
        });
    }

    private bool OverlapsRecentSpawnReservation(int2 cell, int2 size)
    {
        if (_recentSpawnReservations.Count == 0)
            return false;

        float now = Time.time;
        int2 clampedSize = UnitFootprintUtility.ClampSize(size);
        for (int i = 0; i < _recentSpawnReservations.Count; i++)
        {
            RecentSpawnReservation reservation = _recentSpawnReservations[i];
            if (reservation == null || reservation.ExpiresAt <= now)
                continue;

            if (UnitFootprintUtility.Overlaps(cell, clampedSize, reservation.Cell, reservation.Size))
                return true;
        }

        return false;
    }

    private bool TryFindStrictSpawnCell(
        EntityManager em,
        ref Unity.Mathematics.Random rng,
        in GridConfig grid,
        in NativeArray<GridWalkable> walkable,
        in NativeBitArray blocked,
        in NativeBitArray occupied,
        ref NativeBitArray reserved,
        int2 center,
        int radiusCells,
        int2 footprintSize,
        out int2 result)
    {
        result = default;
        const int randomTries = 192;
        for (int i = 0; i < randomTries; i++)
        {
            int2 candidate = new(
                center.x + rng.NextInt(-radiusCells, radiusCells + 1),
                center.y + rng.NextInt(-radiusCells, radiusCells + 1));

            if (!TryReserveSpawnCandidate(grid, walkable, blocked, occupied, ref reserved, candidate, footprintSize))
                continue;
            if (OverlapsRecentSpawnReservation(candidate, footprintSize))
                continue;
            if (OverlapsExistingUnitFootprint(em, candidate, footprintSize))
                continue;

            result = candidate;
            return true;
        }

        int maxRadius = math.max(8, radiusCells + 32);
        for (int r = 0; r <= maxRadius; r++)
        {
            for (int dy = -r; dy <= r; dy++)
            {
                for (int dx = -r; dx <= r; dx++)
                {
                    if (math.abs(dx) != r && math.abs(dy) != r)
                        continue;

                    int2 candidate = new(center.x + dx, center.y + dy);
                    if (!TryReserveSpawnCandidate(grid, walkable, blocked, occupied, ref reserved, candidate, footprintSize))
                        continue;
                    if (OverlapsRecentSpawnReservation(candidate, footprintSize))
                        continue;
                    if (OverlapsExistingUnitFootprint(em, candidate, footprintSize))
                        continue;

                    result = candidate;
                    return true;
                }
            }
        }

        return false;
    }

    private bool TryFindStrictSpawnCellAdjacentToBuilding(
        EntityManager em,
        ref Unity.Mathematics.Random rng,
        in GridConfig grid,
        in NativeArray<GridWalkable> walkable,
        in NativeBitArray blocked,
        in NativeBitArray occupied,
        ref NativeBitArray reserved,
        Vector2Int originCell,
        Vector2Int footprintCells,
        int2 unitFootprint,
        out int2 result)
    {
        result = default;
        int maxExtraRadius = math.max(6, math.max(unitFootprint.x, unitFootprint.y) + 2);
        for (int extraRadius = 1; extraRadius <= maxExtraRadius; extraRadius++)
        {
            var candidates = new NativeList<int2>(Allocator.Temp);
            try
            {
                int minX = originCell.x - extraRadius;
                int minY = originCell.y - extraRadius;
                int maxX = originCell.x + footprintCells.x - 1 + extraRadius;
                int maxY = originCell.y + footprintCells.y - 1 + extraRadius;

                for (int x = minX; x <= maxX; x++)
                {
                    candidates.Add(new int2(x, minY));
                    if (maxY != minY)
                        candidates.Add(new int2(x, maxY));
                }

                for (int y = minY + 1; y < maxY; y++)
                {
                    candidates.Add(new int2(minX, y));
                    if (maxX != minX)
                        candidates.Add(new int2(maxX, y));
                }

                if (candidates.Length == 0)
                    continue;

                int startIndex = rng.NextInt(candidates.Length);
                for (int offset = 0; offset < candidates.Length; offset++)
                {
                    int2 candidate = candidates[(startIndex + offset) % candidates.Length];
                    if (!TryReserveSpawnCandidate(grid, walkable, blocked, occupied, ref reserved, candidate, unitFootprint))
                        continue;
                    if (OverlapsRecentSpawnReservation(candidate, unitFootprint))
                        continue;
                    if (OverlapsExistingUnitFootprint(em, candidate, unitFootprint))
                        continue;

                    result = candidate;
                    return true;
                }
            }
            finally
            {
                if (candidates.IsCreated)
                    candidates.Dispose();
            }
        }

        return false;
    }

    private bool OverlapsExistingUnitFootprint(EntityManager em, int2 cell, int2 size)
    {
        EnsureEntityQueries(em);
        using var entities = _liveUnitFootprintQuery.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            if (em.HasComponent<Prefab>(entity) ||
                em.HasComponent<StaticGridBlocker>(entity) ||
                em.HasComponent<RuntimeBuildingCombatTag>(entity))
            {
                continue;
            }
            if (!em.HasComponent<UnitGrid>(entity) || !em.HasComponent<UnitFootprint>(entity))
                continue;

            UnitGrid otherGrid = em.GetComponentData<UnitGrid>(entity);
            UnitFootprint otherFootprint = em.GetComponentData<UnitFootprint>(entity);
            if (UnitFootprintUtility.Overlaps(cell, size, otherGrid.Cell, otherFootprint.Size))
                return true;
        }

        return false;
    }

    private static bool TryReserveSpawnCandidate(
        in GridConfig grid,
        in NativeArray<GridWalkable> walkable,
        in NativeBitArray blocked,
        in NativeBitArray occupied,
        ref NativeBitArray reserved,
        int2 cell,
        int2 footprintSize)
    {
        int2 size = UnitFootprintUtility.ClampSize(footprintSize);
        int2 min = UnitFootprintUtility.GetMinCell(cell, size);
        int2 max = min + size;
        if (min.x < 0 || min.y < 0 || max.x > grid.Width || max.y > grid.Height)
            return false;

        for (int y = min.y; y < max.y; y++)
        {
            int row = y * grid.Width;
            for (int x = min.x; x < max.x; x++)
            {
                int idx = row + x;
                if (walkable[idx].Value == 0)
                    return false;
                if (blocked.IsSet(idx) || occupied.IsSet(idx) || reserved.IsSet(idx))
                    return false;
            }
        }

        for (int y = min.y; y < max.y; y++)
        {
            int row = y * grid.Width;
            for (int x = min.x; x < max.x; x++)
                reserved.Set(row + x, true);
        }

        return true;
    }

    private void ReserveDynamicOccupancy(Entity gridEntity, in GridConfig grid, int2 centerCell, int2 footprintSize)
    {
        if (!TryGetEntityManager(out EntityManager em) || !em.HasComponent<DynamicOccupancyData>(gridEntity))
            return;

        DynamicOccupancyData occupancy = em.GetComponentData<DynamicOccupancyData>(gridEntity);
        if (!occupancy.Occupied.IsCreated)
            return;

        int2 size = UnitFootprintUtility.ClampSize(footprintSize);
        int2 min = UnitFootprintUtility.GetMinCell(centerCell, size);
        int2 max = min + size;
        for (int y = min.y; y < max.y; y++)
        {
            int row = y * grid.Width;
            for (int x = min.x; x < max.x; x++)
                occupancy.Occupied.Set(row + x, true);
        }
    }

    private static bool TryGetPrefabModelBounds(GameObject prefab, out Bounds combinedBounds)
    {
        combinedBounds = default;
        if (prefab == null)
            return false;

        Transform modelRoot = prefab.transform.Find("Model");
        if (modelRoot == null)
            return false;

        Renderer[] renderers = modelRoot.GetComponentsInChildren<Renderer>(true);
        bool hasBounds = false;
        Matrix4x4 worldToLocal = prefab.transform.worldToLocalMatrix;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
                continue;

            Bounds localBounds = TransformBounds(worldToLocal * renderer.localToWorldMatrix, renderer.localBounds);
            if (!hasBounds)
            {
                combinedBounds = localBounds;
                hasBounds = true;
            }
            else
            {
                combinedBounds.Encapsulate(localBounds);
            }
        }

        return hasBounds;
    }

    private static Bounds TransformBounds(Matrix4x4 matrix, Bounds bounds)
    {
        Vector3 center = bounds.center;
        Vector3 extents = bounds.extents;

        Vector3 min = new(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
        Vector3 max = new(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

        for (int x = -1; x <= 1; x += 2)
        {
            for (int y = -1; y <= 1; y += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    Vector3 corner = center + Vector3.Scale(extents, new Vector3(x, y, z));
                    Vector3 transformed = matrix.MultiplyPoint3x4(corner);
                    min = Vector3.Min(min, transformed);
                    max = Vector3.Max(max, transformed);
                }
            }
        }

        Bounds transformedBounds = new();
        transformedBounds.SetMinMax(min, max);
        return transformedBounds;
    }

    private bool TryGetSpawnUnitPrefabEntity(EntityManager em, GameObject spawnUnitPrefab, out Entity prefabEntity)
    {
        prefabEntity = Entity.Null;
        if (spawnUnitPrefab == null)
            return false;

        EnsureEntityQueries(em);

        return TryGetSpawnUnitPrefabEntityFromRegistry(em, spawnUnitPrefab, out prefabEntity) ||
               TryGetSpawnUnitPrefabEntityFromPrefabQuery(em, spawnUnitPrefab, out prefabEntity) ||
               TryGetPlayerUnitPrefabEntityFromLiveUnits(em, spawnUnitPrefab, out prefabEntity);
    }

    private bool TryGetSpawnUnitPrefabEntityFromRegistry(EntityManager em, GameObject spawnUnitPrefab, out Entity prefabEntity)
    {
        prefabEntity = Entity.Null;
        if (_unitPrefabRegistryQuery.IsEmptyIgnoreFilter || unitSpawnPrefabs == null || unitSpawnPrefabs.Count == 0)
            return false;

        Entity registryEntity = _unitPrefabRegistryQuery.GetSingletonEntity();
        DynamicBuffer<UnitPrefabRegistryEntry> registry = em.GetBuffer<UnitPrefabRegistryEntry>(registryEntity);
        string targetKey = GetSpawnableLookupKey(spawnUnitPrefab);
        int count = math.min(registry.Length, unitSpawnPrefabs.Count);
        if (string.IsNullOrEmpty(targetKey) || count <= 0)
            return false;

        for (int i = 0; i < count; i++)
        {
            GameObject configuredPrefab = unitSpawnPrefabs[i];
            if (configuredPrefab == null)
                continue;

            if (!NamesMatch(GetSpawnableLookupKey(configuredPrefab), targetKey))
                continue;

            prefabEntity = registry[i].Prefab;
            if (prefabEntity == Entity.Null)
                return false;

            return true;
        }

        return false;
    }

    private bool TryGetSpawnUnitPrefabEntityFromPrefabQuery(EntityManager em, GameObject spawnUnitPrefab, out Entity prefabEntity)
    {
        prefabEntity = Entity.Null;

        EnsureEntityQueries(em);
        using var entities = _spawnPrefabCandidatesQuery.ToEntityArray(Allocator.Temp);
        string targetName = spawnUnitPrefab.name;

        for (int i = 0; i < entities.Length; i++)
        {
            Entity candidate = entities[i];
            if (!NamesMatch(em.GetName(candidate), targetName))
                continue;

            prefabEntity = candidate;
            return true;
        }

        return false;
    }

    private bool TryGetPlayerUnitPrefabEntityFromLiveUnits(EntityManager em, GameObject spawnUnitPrefab, out Entity prefabEntity)
    {
        prefabEntity = Entity.Null;
        string targetName = spawnUnitPrefab != null ? spawnUnitPrefab.name : string.Empty;
        EnsureEntityQueries(em);
        using var entities = _livePlayerUnitsQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            if (em.HasComponent<StaticGridBlocker>(entity))
                continue;
            if (em.GetComponentData<Faction>(entity).Id != 0)
                continue;

            Entity candidate = em.GetComponentData<UnitRespawnPrefab>(entity).Prefab;
            if (candidate == Entity.Null)
                continue;
            if (!NamesMatch(em.GetName(candidate), targetName))
                continue;

            prefabEntity = candidate;
            return true;
        }

        return false;
    }

    private static bool NamesMatch(string candidateName, string targetName)
    {
        if (string.IsNullOrWhiteSpace(candidateName) || string.IsNullOrWhiteSpace(targetName))
            return false;

        return string.Equals(candidateName, targetName, System.StringComparison.OrdinalIgnoreCase) ||
               string.Equals(candidateName.Replace(" (Clone)", string.Empty), targetName, System.StringComparison.OrdinalIgnoreCase);
    }

    private static int2 FindSpawnCellAdjacentToBuilding(
        ref Unity.Mathematics.Random rng,
        in GridConfig grid,
        in NativeArray<GridWalkable> walkable,
        in NativeBitArray blocked,
        in NativeBitArray occupied,
        ref NativeBitArray reserved,
        Vector2Int originCell,
        Vector2Int footprintCells,
        int2 fallbackCenter)
    {
        int maxRadius = math.max(grid.Width, grid.Height);
        for (int extraRadius = 1; extraRadius <= maxRadius; extraRadius++)
        {
            if (TryReservePerimeterCell(
                ref rng,
                grid,
                walkable,
                blocked,
                occupied,
                ref reserved,
                originCell,
                footprintCells,
                extraRadius,
                out int2 cell))
            {
                return cell;
            }
        }

        return SpawnCellUtility.FindSpawnCellNear(ref rng, grid, walkable, blocked, occupied, ref reserved, fallbackCenter, math.max(footprintCells.x, footprintCells.y) + 4);
    }

    private static bool TryReservePerimeterCell(
        ref Unity.Mathematics.Random rng,
        in GridConfig grid,
        in NativeArray<GridWalkable> walkable,
        in NativeBitArray blocked,
        in NativeBitArray occupied,
        ref NativeBitArray reserved,
        Vector2Int originCell,
        Vector2Int footprintCells,
        int extraRadius,
        out int2 cell)
    {
        cell = default;

        int minX = originCell.x - extraRadius;
        int minY = originCell.y - extraRadius;
        int maxX = originCell.x + footprintCells.x - 1 + extraRadius;
        int maxY = originCell.y + footprintCells.y - 1 + extraRadius;

        var candidates = new NativeList<int2>(Allocator.Temp);
        try
        {
            for (int x = minX; x <= maxX; x++)
            {
                TryAddPerimeterCandidate(grid, walkable, blocked, occupied, reserved, x, minY, ref candidates);
                if (maxY != minY)
                    TryAddPerimeterCandidate(grid, walkable, blocked, occupied, reserved, x, maxY, ref candidates);
            }

            for (int y = minY + 1; y < maxY; y++)
            {
                TryAddPerimeterCandidate(grid, walkable, blocked, occupied, reserved, minX, y, ref candidates);
                if (maxX != minX)
                    TryAddPerimeterCandidate(grid, walkable, blocked, occupied, reserved, maxX, y, ref candidates);
            }

            if (candidates.Length == 0)
                return false;

            int startIndex = rng.NextInt(candidates.Length);
            for (int offset = 0; offset < candidates.Length; offset++)
            {
                int2 candidate = candidates[(startIndex + offset) % candidates.Length];
                int index = GridUtils.CellToIndex(candidate, grid.Width);
                if (reserved.IsSet(index))
                    continue;

                reserved.Set(index, true);
                cell = candidate;
                return true;
            }

            return false;
        }
        finally
        {
            if (candidates.IsCreated)
                candidates.Dispose();
        }
    }

    private static void TryAddPerimeterCandidate(
        in GridConfig grid,
        in NativeArray<GridWalkable> walkable,
        in NativeBitArray blocked,
        in NativeBitArray occupied,
        in NativeBitArray reserved,
        int x,
        int y,
        ref NativeList<int2> candidates)
    {
        if ((uint)x >= (uint)grid.Width || (uint)y >= (uint)grid.Height)
            return;

        int index = GridUtils.CellToIndex(new int2(x, y), grid.Width);
        if (walkable[index].Value == 0 || blocked.IsSet(index) || occupied.IsSet(index) || reserved.IsSet(index))
            return;

        candidates.Add(new int2(x, y));
    }

    private static bool DoesRemainingPathIntersectFootprint(
        EntityManager em,
        Entity unit,
        NativeArray<int2> pathPool,
        Vector2Int originCell,
        Vector2Int footprintCells)
    {
        UnitPathFollow follow = em.GetComponentData<UnitPathFollow>(unit);
        UnitPathRange range = em.GetComponentData<UnitPathRange>(unit);
        int startIndex = math.max(0, follow.PathIndex);
        int endIndex = math.min(range.Length, pathPool.Length - range.Start);
        for (int i = startIndex; i < endIndex; i++)
        {
            int poolIndex = range.Start + i;
            if ((uint)poolIndex >= (uint)pathPool.Length)
                break;

            if (IsCellInsideFootprint(pathPool[poolIndex], originCell, footprintCells))
                return true;
        }

        return false;
    }

    private static bool TryFindNearestPerimeterCell(
        in GridConfig grid,
        in NativeArray<GridWalkable> walkable,
        in NativeBitArray blocked,
        in NativeBitArray occupied,
        ref NativeBitArray reserved,
        Vector2Int originCell,
        Vector2Int footprintCells,
        int2 referenceCell,
        out int2 goal)
    {
        goal = default;
        int maxRadius = math.max(grid.Width, grid.Height);
        int bestScore = int.MaxValue;
        bool found = false;

        for (int extraRadius = 1; extraRadius <= maxRadius; extraRadius++)
        {
            int minX = originCell.x - extraRadius;
            int minY = originCell.y - extraRadius;
            int maxX = originCell.x + footprintCells.x - 1 + extraRadius;
            int maxY = originCell.y + footprintCells.y - 1 + extraRadius;

            for (int x = minX; x <= maxX; x++)
            {
                TryScorePerimeterGoal(grid, walkable, blocked, occupied, reserved, referenceCell, x, minY, ref bestScore, ref goal, ref found);
                if (maxY != minY)
                    TryScorePerimeterGoal(grid, walkable, blocked, occupied, reserved, referenceCell, x, maxY, ref bestScore, ref goal, ref found);
            }

            for (int y = minY + 1; y < maxY; y++)
            {
                TryScorePerimeterGoal(grid, walkable, blocked, occupied, reserved, referenceCell, minX, y, ref bestScore, ref goal, ref found);
                if (maxX != minX)
                    TryScorePerimeterGoal(grid, walkable, blocked, occupied, reserved, referenceCell, maxX, y, ref bestScore, ref goal, ref found);
            }

            if (found)
            {
                reserved.Set(GridUtils.CellToIndex(goal, grid.Width), true);
                return true;
            }
        }

        return false;
    }

    private static void TryScorePerimeterGoal(
        in GridConfig grid,
        in NativeArray<GridWalkable> walkable,
        in NativeBitArray blocked,
        in NativeBitArray occupied,
        in NativeBitArray reserved,
        int2 referenceCell,
        int x,
        int y,
        ref int bestScore,
        ref int2 bestCell,
        ref bool found)
    {
        if ((uint)x >= (uint)grid.Width || (uint)y >= (uint)grid.Height)
            return;

        int2 candidate = new(x, y);
        int index = GridUtils.CellToIndex(candidate, grid.Width);
        if (walkable[index].Value == 0 || blocked.IsSet(index) || occupied.IsSet(index) || reserved.IsSet(index))
            return;

        int score = math.abs(referenceCell.x - x) + math.abs(referenceCell.y - y);
        if (!found || score < bestScore)
        {
            bestScore = score;
            bestCell = candidate;
            found = true;
        }
    }

    private static bool IsCellInsideFootprint(int2 cell, Vector2Int originCell, Vector2Int footprintCells)
    {
        return cell.x >= originCell.x &&
               cell.y >= originCell.y &&
               cell.x < originCell.x + footprintCells.x &&
               cell.y < originCell.y + footprintCells.y;
    }

    private bool TryGetGridData(out Entity gridEntity, out GridConfig grid, out DynamicBuffer<GridRoad> roads, out DynamicBlockerData blockerData)
    {
        gridEntity = Entity.Null;
        grid = default;
        roads = default;
        blockerData = default;

        if (!TryGetEntityManager(out EntityManager em))
            return false;

        EnsureEntityQueries(em);
        if (_gridDataQuery.IsEmptyIgnoreFilter)
            return false;

        gridEntity = _gridDataQuery.GetSingletonEntity();
        grid = em.GetComponentData<GridConfig>(gridEntity);
        roads = em.GetBuffer<GridRoad>(gridEntity);
        blockerData = em.GetComponentData<DynamicBlockerData>(gridEntity);
        return true;
    }

    private static bool TryGetEntityManager(out EntityManager entityManager)
    {
        entityManager = default;
        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return false;

        entityManager = world.EntityManager;
        return true;
    }

    private bool TryGetGridCell(Vector2 screenPosition, GridConfig grid, out Vector2Int cell)
    {
        cell = default;
        Ray ray = worldCamera.ScreenPointToRay(screenPosition);
        Plane plane = new(Vector3.up, new Vector3(0f, buildPlaneY, 0f));
        if (!plane.Raycast(ray, out float distance))
            return false;

        int2 gridCell = GridUtils.WorldToCell(grid, ray.GetPoint(distance));
        if (!GridUtils.InBounds(gridCell, grid.Width, grid.Height))
            return false;

        cell = new Vector2Int(gridCell.x, gridCell.y);
        return true;
    }

    private static bool IsPointerOverUI(Vector2 screenPosition)
    {
        return false;
    }

    private static bool IsPointerOverBlockingUI(Vector2 screenPosition)
    {
        return false;
    }

    private float GetPlacementOutlineHeight()
    {
        float baseHeight = Mathf.Max(0.5f, placementOutlineHeight);
        if (_activePlacement?.Definition?.HasLocalBounds == true)
            baseHeight = Mathf.Max(baseHeight, _activePlacement.Definition.LocalBounds.size.y + placementOutlineHeight);

        return baseHeight;
    }

    private bool IsPointerOverPlacementUi(Vector2 screenPosition)
    {
        return _mainMenuPlayUi != null && _mainMenuPlayUi.IsPointerOverPlacementUi(screenPosition);
    }

    private bool IsPointerOverAnyGameplayUi(Vector2 screenPosition)
    {
        return _mainMenuPlayUi != null && _mainMenuPlayUi.IsPointerOverAnyGameplayUi(screenPosition, out _);
    }

    private bool IsPointerOverActivePlacement(Vector2 screenPosition)
    {
        if (_activePlacement == null)
            return false;
        if (!TryGetGridData(out _, out GridConfig grid, out _, out _))
            return false;
        if (!TryGetGridCell(screenPosition, grid, out Vector2Int cell))
            return false;

        Vector2Int origin = _activePlacement.OriginCell;
        Vector2Int size = _activePlacement.Definition.FootprintCells;
        return cell.x >= origin.x &&
               cell.y >= origin.y &&
               cell.x < origin.x + size.x &&
               cell.y < origin.y + size.y;
    }

    private static void ReserveBuildingBuffer(ref NativeBitArray reserved, GridConfig grid, Vector2Int originCell, Vector2Int footprintCells, int extraRadius)
    {
        int minX = Mathf.Max(0, originCell.x - extraRadius);
        int minY = Mathf.Max(0, originCell.y - extraRadius);
        int maxX = Mathf.Min(grid.Width, originCell.x + footprintCells.x + extraRadius);
        int maxY = Mathf.Min(grid.Height, originCell.y + footprintCells.y + extraRadius);

        for (int y = minY; y < maxY; y++)
        {
            for (int x = minX; x < maxX; x++)
            {
                int index = GridUtils.CellToIndex(new int2(x, y), grid.Width);
                reserved.Set(index, true);
            }
        }
    }

}
