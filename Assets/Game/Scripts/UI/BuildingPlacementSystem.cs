using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.InputSystem;
using SnivelerCode.GpuAnimation.Scripts.Authoring;
using SnivelerCode.GpuAnimation.Scripts.Components;
using static UnityEngine.Object;
using PlacementState = BuildingPlacementLifecycleSystem.PlacementState;
using ProductionTransportMode = BuildingProductionSystem.ProductionTransportMode;
using ResourceHaulKind = ResourceHaulerSystem.ResourceHaulKind;
using ResourceHaulPhase = ResourceHaulerSystem.ResourceHaulPhase;

public sealed class BuildingPlacementSystem
{
    private readonly RuntimeGameplayStateSystem _runtimeGameplayStateSystem = new();

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

    internal sealed class BuildingDefinition
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

    internal sealed class RuntimeBuildingData : BuildingCombatSystem.IRuntimeBuildingVisualState, FactionResourceSystem.IResourceBuilding
    {
        internal sealed class PendingDropVisual
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

        internal sealed class ActiveProductionTransport
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

        internal sealed class PendingProduction : BuildingProductionSystem.IPendingProduction
        {
            public int ProductionIndex { get; set; }
            public GameObject Prefab { get; set; }
            public float StartedAt { get; set; }
            public float ReadyAt { get; set; }
            public int ReservedProductionSlotIndex { get; set; }
            public GameObject TransportPrefab { get; set; }
            public float TransportArrivalSeconds { get; set; }
            public float TransportHoldForNextReadySeconds { get; set; }
            public int TransportMaxConcurrent { get; set; }
            public ProductionTransportMode TransportMode { get; set; }
            public bool TransportRequiresAirportRunway { get; set; }
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
        public GameObject InstanceObject => Instance;
        public Transform FactionMarkerTransform => FactionMarker;
        public Transform SelectionMarkerTransform => SelectionMarker;
        public Transform DestroyedVisualTransform => DestroyedVisual;
        public IReadOnlyList<Transform> AliveVisualRootTransforms => AliveVisualRoots;
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
    private readonly ResourceHaulerSystem _resourceHaulerSystem = new();
    private readonly BuildingProductionSystem _buildingProductionSystem = new();
    private readonly BuildingProductionTransportSystem _buildingProductionTransportSystem = new();
    private readonly BuildingSpawnSystem _buildingSpawnSystem = new();
    private readonly BuildingSpawnPrefabSystem _buildingSpawnPrefabSystem = new();
    private readonly BuildingProductionSlotSystem _buildingProductionSlotSystem = new();
    private readonly BuildingPlacementQuerySystem _buildingPlacementQuerySystem = new();
    private readonly BuildingUiQuerySystem _buildingUiQuerySystem = new();
    private readonly BuildingRunwaySystem _buildingRunwaySystem = new();
    private readonly BuildingPlacementValidationSystem _buildingPlacementValidationSystem = new();
    private readonly BuildingPlacementPreviewSystem _buildingPlacementPreviewSystem = new();
    private readonly BuildingPlacementCommitSystem _buildingPlacementCommitSystem = new();
    private readonly BuildingPlacementInputSystem _buildingPlacementInputSystem = new();
    private readonly BuildingProductionRequestSystem _buildingProductionRequestSystem = new();
    private readonly BuildingRuntimeCreationSystem _buildingRuntimeCreationSystem = new();
    private readonly BuildingSelectionSystem _buildingSelectionSystem = new();
    private readonly BuildingBarrierSystem _buildingBarrierSystem = new();
    private readonly BuildingRuntimeQuerySystem _buildingRuntimeQuerySystem = new();
    private readonly BuildingDefinitionSystem _buildingDefinitionSystem = new();
    private readonly BuildingPlacementLifecycleSystem _buildingPlacementLifecycleSystem = new();
    private readonly BuildingProductionTransportSystem.TrySpawnPlayerUnitNearBuildingDelegate _trySpawnPlayerUnitNearBuildingForTransport;
    private readonly BuildingProductionTransportSystem.ResolveProductionGroundGoalCellDelegate _resolveProductionGroundGoalCellForTransport;
    private readonly BuildingProductionTransportSystem.BuildingCellAction _moveNewestProducedUnitToCellForTransport;
    private readonly BuildingProductionTransportSystem.BuildingForwardAction _alignNewestProducedUnitRotationForTransport;
    private IReadOnlyDictionary<int, RuntimeBuildingData> _runtimeBuildings => _runtimeBuildingSystem.Buildings;
    private readonly List<RectInt> _deferredRedirectFootprints = new();
    private int[] _placementInvalidPrefix;
    private int _resourceDollars;
    private Transform _buildingRoot;
    private BuildingDefinition _soldierBaseDefinition;
    private BuildingDefinition _soldierTentDefinition;
    private BuildingDefinition _factoryDefinition;
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
    private MaterialPropertyBlock _markerPropertyBlock;
    private int _deferRuntimeBuildingSideEffectsDepth;
    private bool _pendingMarkerRefresh;
    private bool _hasPlacementInvalidPrefix;
    private int _placementInvalidPrefixWidth;
    private int _placementInvalidPrefixHeight;
    private Transform _runtimeRoot;
    private readonly List<BuildingPlacementPreviewSystem.WallPreviewRun> _wallPreviewRuns = new();
    private readonly List<BuildingPlacementCommitSystem.WallRun> _wallCommitRuns = new();
    private bool _preserveBuildingSelectionOnNextExitBuildMode;
    private const float OilBarrelsPerFuelBarrel = 2f;

    private int? ActiveBuildingId => _runtimeBuildingSystem.CurrentActiveBuildingId;

    public bool HasPendingBuildingPlacement => _buildingPlacementLifecycleSystem.HasPendingBuildingPlacement;
    public bool CanConfirmBuildingPlacement => _buildingPlacementLifecycleSystem.CanConfirmBuildingPlacement;
    public bool HasSelectedBuilding => _runtimeBuildingSystem.HasSelectedBuilding();
    public bool HasActiveBuilding => ActiveBuildingId.HasValue;
    public int? CurrentActiveBuildingId => ActiveBuildingId;
    public GameObject RoadPreviewPrefab => config != null ? config.RoadPreviewPrefab : null;
    public float BuildButtonPreviewDistanceMultiplier => config != null ? config.BuildButtonPreviewDistanceMultiplier : 1f;
    public float UnitCommandButtonPreviewDistanceMultiplier => config != null ? config.UnitCommandButtonPreviewDistanceMultiplier : 1f;
    public int ConfiguredSpawnableCount => _buildingDefinitionSystem.ConfiguredSpawnableCount;
    public int ConfiguredUnitCount => unitSpawnPrefabs != null ? unitSpawnPrefabs.Count : 0;

    public BuildingPlacementSystem()
    {
        _trySpawnPlayerUnitNearBuildingForTransport = TrySpawnPlayerUnitNearBuilding;
        _resolveProductionGroundGoalCellForTransport = ResolveProductionGroundGoalCell;
        _moveNewestProducedUnitToCellForTransport = MoveNewestProducedUnitToCell;
        _alignNewestProducedUnitRotationForTransport = AlignNewestProducedUnitRotation;
    }

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
        _buildingPlacementQuerySystem.GetSelectedBuildingProductionPrefabs(
            CreateBuildingPlacementQueryContext(),
            prefabs);
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

        building.ProducedUnits ??= new List<Entity>();
        _buildingUiQuerySystem.GetProducedUnits(building.ProducedUnits, em, _buildingProductionSystem, units);
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

        building.ProducedUnits ??= new List<Entity>();
        building.ProducedUnitPrefabs ??= new Dictionary<Entity, GameObject>();
        _buildingUiQuerySystem.AddProducedUnitEntries(
            building.ProducedUnits,
            building.ProducedUnitPrefabs,
            building.PendingProductions,
            em,
            _buildingProductionSystem,
            Time.time,
            entries);
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

            _buildingUiQuerySystem.AddPendingProductionUiEntries(
                pair.Key,
                building.PendingProductions,
                _buildingProductionSystem,
                now,
                entries);
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
        return _buildingRuntimeQuerySystem.CountRuntimeBuildingsForFaction(CreateBuildingRuntimeQueryContext(), factionId);
    }

    public int CountRuntimeBuildingsForFaction(byte factionId, string buildingId)
    {
        return _buildingRuntimeQuerySystem.CountRuntimeBuildingsForFaction(CreateBuildingRuntimeQueryContext(), factionId, buildingId);
    }

    public int CountRuntimeProducedUnitsForFaction(byte factionId, string unitId)
    {
        return _buildingRuntimeQuerySystem.CountRuntimeProducedUnitsForFaction(CreateBuildingRuntimeQueryContext(), factionId, unitId);
    }

    public int CountPendingProductionsForFaction(byte factionId, string unitId)
    {
        return _buildingRuntimeQuerySystem.CountPendingProductionsForFaction(CreateBuildingRuntimeQueryContext(), factionId, unitId);
    }

    public bool TryGetConfiguredUnit(string unitId, out ConfiguredUnitEntry entry)
    {
        string normalized = BuildingDefinitionSystem.NormalizeSpawnableKey(unitId);
        if (string.IsNullOrEmpty(normalized))
        {
            entry = default;
            return false;
        }

        for (int i = 0; i < ConfiguredUnitCount; i++)
        {
            if (!TryGetConfiguredUnit(i, out ConfiguredUnitEntry candidate))
                continue;
            if (!BuildingDefinitionSystem.UnitPrefabMatchesId(candidate.Prefab, normalized))
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

        if (!QueuePlayerUnitProduction(producerBuilding, productionIndex, unit.Prefab))
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
        _buildingRuntimeQuerySystem.GetRuntimeHouseBuildingIds(CreateBuildingRuntimeQueryContext(), results);
    }

    public void GetRuntimeBuildingIdsByRole(BuildingRole role, List<int> results)
    {
        _buildingRuntimeQuerySystem.GetRuntimeBuildingIdsByRole(CreateBuildingRuntimeQueryContext(), role, results);
    }

    public bool TryGetRuntimeBuildingFocusWorldPosition(int buildingId, out Vector3 worldPosition)
    {
        return _buildingRuntimeQuerySystem.TryGetRuntimeBuildingFocusWorldPosition(CreateBuildingRuntimeQueryContext(), buildingId, out worldPosition);
    }

    public bool TryGetRuntimeBuildingDestroyedState(int buildingId, out bool isDestroyed)
    {
        return _buildingRuntimeQuerySystem.TryGetRuntimeBuildingDestroyedState(CreateBuildingRuntimeQueryContext(), buildingId, out isDestroyed);
    }

    public bool TryGetRuntimeBuildingRefugeeSettings(int buildingId, out int refugeeCapacity, out int upkeepPerCitizenPerDay)
    {
        return _buildingRuntimeQuerySystem.TryGetRuntimeBuildingRefugeeSettings(
            CreateBuildingRuntimeQueryContext(),
            buildingId,
            out refugeeCapacity,
            out upkeepPerCitizenPerDay);
    }

    public bool IsRuntimeBuildingCityGenerated(int buildingId)
    {
        return _buildingRuntimeQuerySystem.IsRuntimeBuildingCityGenerated(CreateBuildingRuntimeQueryContext(), buildingId);
    }

    public bool IsRuntimeBuildingWall(int buildingId)
    {
        return _buildingRuntimeQuerySystem.IsRuntimeBuildingWall(CreateBuildingRuntimeQueryContext(), buildingId);
    }

    public bool TryGetRuntimeBuildingOwnerFaction(int buildingId, out byte factionId)
    {
        return _buildingRuntimeQuerySystem.TryGetRuntimeBuildingOwnerFaction(CreateBuildingRuntimeQueryContext(), buildingId, out factionId);
    }

    public bool TryGetRuntimeBuildingCombatInfo(Entity combatEntity, out bool isGate, out bool isWall, out byte ownerFactionId)
    {
        return _buildingRuntimeQuerySystem.TryGetRuntimeBuildingCombatInfo(
            CreateBuildingRuntimeQueryContext(),
            combatEntity,
            out isGate,
            out isWall,
            out ownerFactionId);
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
            (finalBuilding.Definition.IsWall || BuildingBarrierSystem.IsWallGateDefinition(finalBuilding.Definition)))
            return false;

        BuildingBarrierSystem.Context barrierContext = CreateBuildingBarrierContext();
        if (!_buildingBarrierSystem.TryFindEnemyWallPerimeterContainingCell(barrierContext, attackerFactionId, finalTargetCell, out byte breachedFactionId, out RectInt breachedPerimeter))
            return false;

        if (_buildingBarrierSystem.HasOpenBaseBreach(barrierContext, breachedFactionId, breachedPerimeter))
            return false;

        if (!_buildingBarrierSystem.TryFindBreachBuilding(barrierContext, breachedFactionId, attackerCell, preferGate: true, out RuntimeBuildingData breachBuilding, out reason) &&
            !_buildingBarrierSystem.TryFindBreachBuilding(barrierContext, breachedFactionId, attackerCell, preferGate: false, out breachBuilding, out reason))
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
        return _buildingRuntimeQuerySystem.TryGetRuntimeBuildingApproachCell(
            CreateBuildingRuntimeQueryContext(),
            buildingId,
            unitFootprint,
            referenceCell,
            out goal);
    }

    public bool IsRuntimeBuildingApproachCell(int buildingId, int2 currentCell, int2 unitFootprint)
    {
        return _buildingRuntimeQuerySystem.IsRuntimeBuildingApproachCell(
            CreateBuildingRuntimeQueryContext(),
            buildingId,
            currentCell,
            unitFootprint);
    }

    public bool TryResolveConfiguredUnitPrefabEntity(GameObject unitPrefab, out Entity prefabEntity)
    {
        prefabEntity = Entity.Null;
        if (unitPrefab == null || !TryGetEntityManager(out EntityManager em))
            return false;

        EnsureEntityQueries(em);
        return _buildingSpawnPrefabSystem.TryGetSpawnUnitPrefabEntity(
            CreateBuildingSpawnPrefabContext(),
            em,
            unitPrefab,
            out prefabEntity);
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
        return _buildingDefinitionSystem.TryResolveConfiguredSpawnablePrefab(em.GetName(prefabEntity), out prefab);
    }

    public bool TryResolveConfiguredSpawnablePrefab(string lookupKey, out GameObject prefab)
    {
        return _buildingDefinitionSystem.TryResolveConfiguredSpawnablePrefab(lookupKey, out prefab);
    }

    public bool TryResolveConfiguredUnitSpawnPrefab(string lookupKey, out GameObject prefab)
    {
        return _buildingDefinitionSystem.TryResolveConfiguredUnitSpawnPrefab(lookupKey, out prefab);
    }

    public bool IsDraggingPlacementPreview => _buildingPlacementLifecycleSystem.HasPendingBuildingPlacement && _buildingPlacementInputSystem.IsDraggingPlacement;

    public bool TryResolveSpawnUnitPrefab(Entity prefabEntity, out GameObject spawnUnitPrefab)
    {
        spawnUnitPrefab = null;
        if (prefabEntity == Entity.Null || World.DefaultGameObjectInjectionWorld == null)
            return false;

        var em = World.DefaultGameObjectInjectionWorld.EntityManager;
        EnsureEntityQueries(em);
        return _buildingSpawnPrefabSystem.TryResolveSpawnUnitPrefabFromRegistry(
            CreateBuildingSpawnPrefabContext(),
            em,
            prefabEntity,
            out spawnUnitPrefab);
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
            if (!string.IsNullOrEmpty(key) && _buildingDefinitionSystem.TryResolveConfiguredUnitSpawnPrefab(key, out prefab))
            {
                return true;
            }
        }

        return false;
    }

    private GameObject TryGetSelectedBuildingProductionPrefab(CreateSlot slot)
    {
        return TryGetSelectedBuildingProductionPrefab((int)slot);
    }

    public GameObject TryGetSelectedBuildingProductionPrefab(int productionIndex)
    {
        return _buildingPlacementQuerySystem.GetSelectedBuildingProductionPrefab(
            CreateBuildingPlacementQueryContext(),
            productionIndex);
    }

    public string PlacementStatusText
    {
        get
        {
            return _buildingPlacementQuerySystem.GetPlacementStatusText(_buildingPlacementLifecycleSystem.ActivePlacement);
        }
    }

    public string SelectedBuildingLabel
    {
        get
        {
            return _buildingPlacementQuerySystem.GetSelectedBuildingLabel(CreateBuildingPlacementQueryContext());
        }
    }

    public string SelectedBuildingDisplayName
    {
        get
        {
            return _buildingPlacementQuerySystem.GetSelectedBuildingDisplayName(CreateBuildingPlacementQueryContext());
        }
    }

    public string SelectedBuildingDescription
    {
        get
        {
            return _buildingPlacementQuerySystem.GetSelectedBuildingDescription(CreateBuildingPlacementQueryContext());
        }
    }

    public bool TryGetSelectedBuildingPreviewPrefab(out GameObject prefab)
    {
        return _buildingPlacementQuerySystem.TryGetSelectedBuildingPreviewPrefab(
            CreateBuildingPlacementQueryContext(),
            out prefab);
    }

    public bool TryGetSelectedBuildingHealth(out int current, out int max)
    {
        return _buildingPlacementQuerySystem.TryGetSelectedBuildingHealth(
            CreateBuildingPlacementQueryContext(),
            out current,
            out max);
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
        _buildingPlacementPreviewSystem.Init(
            _runtimeRoot,
            placementOutlineHeight,
            placementValidColor,
            placementInvalidColor,
            DestroyRuntimeObject);
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
        if (spawnables == null)
            spawnables = new List<GameObject>();

        _buildingDefinitionSystem.RebuildSpawnablesLookup(spawnables, unitSpawnPrefabs);
    }

    private void RebuildConfiguredSpawnableDefinitions()
    {
        _buildingDefinitionSystem.RebuildConfiguredSpawnableDefinitions(spawnables, _buildingRunwaySystem, DestroyRuntimeObject);

        _soldierBaseDefinition = _buildingDefinitionSystem.FindConfiguredDefinition("Soldier Base");
        _soldierTentDefinition = _buildingDefinitionSystem.FindConfiguredDefinition("Soldier Tent");
        _factoryDefinition = _buildingDefinitionSystem.FindConfiguredDefinition("Factory");
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

        _buildingDefinitionSystem.ClearConfiguredSpawnableDefinitions(DestroyRuntimeObject);
        _buildingDefinitionSystem.ClearUnitLookup();
        _soldierBaseDefinition = null;
        _soldierTentDefinition = null;
        _factoryDefinition = null;

        _buildingPlacementPreviewSystem.Dispose();
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
        _buildingBarrierSystem.UpdateRoadBarrierDoors(CreateBuildingBarrierContext(), Time.deltaTime);
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

        PlacementState activePlacement = _buildingPlacementLifecycleSystem.ActivePlacement;
        if (activePlacement != null)
        {
            _buildingPlacementInputSystem.UpdateActivePlacementPointer(
                activePlacement,
                pointer,
                CreateActivePlacementPointerContext());
            afterInput = Time.realtimeSinceStartupAsDouble;
            afterInputOutline = afterInput;
            afterInputUi = afterInput;
            afterInputBuildingClick = afterInput;
            return;
        }

        if (!_runtimeGameplayStateSystem.PlayRequested)
        {
            _buildingPlacementPreviewSystem.HideOutline();
            afterInputOutline = Time.realtimeSinceStartupAsDouble;
            afterInput = afterInputOutline;
            afterInputUi = afterInput;
            afterInputBuildingClick = afterInput;
            return;
        }

        if (!_runtimeGameplayStateSystem.BuildModeActive)
            _buildingPlacementPreviewSystem.HideOutline();
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
                _runtimeGameplayStateSystem.SuppressNextWorldClick = true;
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

    private BuildingPlacementInputSystem.ActivePlacementPointerContext CreateActivePlacementPointerContext()
    {
        return new BuildingPlacementInputSystem.ActivePlacementPointerContext(
            TryGetGridForPlacementInput,
            TryGetGridCell,
            CenterCellToOrigin,
            BuildingPlacementCommitSystem.GetWallSegmentFootprint,
            IsPointerOverPlacementUi,
            BuildingBarrierSystem.IsLinearWallDefinition,
            UpdatePlacement);
    }

    private bool TryGetGridForPlacementInput(out GridConfig grid)
    {
        bool hasGrid = TryGetGridData(out _, out grid, out _, out _);
        return hasGrid;
    }

    private void UpdateResourceProduction()
    {
        if (_runtimeBuildings.Count == 0)
            return;

        float secondsPerDay = _dayNightSystem != null
            ? Mathf.Max(1f, _dayNightSystem.FullDayDurationMinutes * 60f)
            : 300f;
        float deltaTime = Time.deltaTime;

        FactionResourceSystem.ResourceProductionTickResult result = _factionResourceSystem.UpdateResourceProduction(
            _runtimeBuildings,
            secondsPerDay,
            deltaTime,
            OilBarrelsPerFuelBarrel);
        if (result.OilExtractedBarrels > 0f)
            GameRuntimeStats.RecordOilExtracted(result.OilExtractedBarrels);
        if (result.FuelProducedBarrels > 0f)
            GameRuntimeStats.RecordFuelProduced(result.FuelProducedBarrels);
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

                    _resourceHaulerSystem.SetTravelPhase(ref order, ResourceHaulPhase.ToSource, goal);
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
                    _resourceHaulerSystem.SetPhase(ref order, ResourceHaulPhase.Loading);
                    em.SetComponentData(entity, order);
                    break;
                }

                case ResourceHaulPhase.Loading:
                {
                    float loadAmount = _resourceHaulerSystem.GetLoadAmount(hauler);
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
                    if (!_resourceHaulerSystem.HasEnoughSourceResource(source, resourceKind, loadAmount))
                    {
                        if (VerboseResourceHaulerLogs)
                            Debug.Log($"[ResourceHauler] entity={entity} waiting-for-resource resource={resourceKind} source={source.Id} stored={sourceStored:0.##} need={loadAmount:0.##}");
                        break;
                    }

                    ResourceHaulerSystem.TimedActionState loadTimer = _resourceHaulerSystem.AdvanceTimedAction(ref order, now, hauler.FillDurationSeconds);
                    if (loadTimer == ResourceHaulerSystem.TimedActionState.Started)
                    {
                        em.SetComponentData(entity, order);
                        if (VerboseResourceHaulerLogs)
                            Debug.Log($"[ResourceHauler] entity={entity} loading-started source={source.Id} fillDuration={hauler.FillDurationSeconds:0.##} completeAt={order.ActionEndsAt:0.##}");
                        break;
                    }
                    if (loadTimer == ResourceHaulerSystem.TimedActionState.Waiting)
                    {
                        if (VerboseResourceHaulerLogs)
                            Debug.Log($"[ResourceHauler] entity={entity} loading-in-progress source={source.Id} remaining={order.ActionEndsAt - now:0.##}");
                        break;
                    }

                    sourceStored = resourceKind == ResourceHaulKind.Fuel ? source.StoredFuelBarrels : source.StoredOilBarrels;
                    if (!_resourceHaulerSystem.HasEnoughSourceResource(source, resourceKind, loadAmount))
                    {
                        _resourceHaulerSystem.ResetActionTimer(ref order);
                        em.SetComponentData(entity, order);
                        if (VerboseResourceHaulerLogs)
                            Debug.Log($"[ResourceHauler] entity={entity} loading-reset-insufficient-resource resource={resourceKind} source={source.Id} stored={sourceStored:0.##} need={loadAmount:0.##}");
                        break;
                    }

                    if (!_resourceHaulerSystem.TryCompleteLoad(source, resourceKind, loadAmount, ref hauler))
                        break;
                    em.SetComponentData(entity, hauler);
                    if (VerboseResourceHaulerLogs)
                        Debug.Log($"[ResourceHauler] entity={entity} loading-complete resource={resourceKind} source={source.Id} loaded={loadAmount:0.##}");

                    if (!TryIssueHaulerMoveToBuilding(em, entity, destination, out int2 destinationGoal))
                    {
                        _resourceHaulerSystem.RevertLoad(source, resourceKind, loadAmount, ref hauler);
                        em.SetComponentData(entity, hauler);
                        if (VerboseResourceHaulerLogs)
                            Debug.LogWarning($"[ResourceHauler] entity={entity} failed-destination-move destination={destination.Id} revertedLoad={loadAmount:0.##}");
                        break;
                    }

                    _resourceHaulerSystem.SetTravelPhase(ref order, ResourceHaulPhase.ToDestination, destinationGoal);
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

                    _resourceHaulerSystem.SetPhase(ref order, ResourceHaulPhase.Unloading);
                    em.SetComponentData(entity, order);
                    break;
                }

                case ResourceHaulPhase.Unloading:
                {
                    float cargo = _resourceHaulerSystem.GetCargo(hauler, resourceKind);
                    if (cargo <= 0f)
                    {
                        _resourceHaulerSystem.SetPhase(ref order, ResourceHaulPhase.None);
                        em.SetComponentData(entity, order);
                        break;
                    }

                    if (!_resourceHaulerSystem.HasReceivingCapacity(destination, resourceKind, cargo))
                        break;

                    ResourceHaulerSystem.TimedActionState unloadTimer = _resourceHaulerSystem.AdvanceTimedAction(ref order, now, hauler.UnloadDurationSeconds);
                    if (unloadTimer == ResourceHaulerSystem.TimedActionState.Started ||
                        unloadTimer == ResourceHaulerSystem.TimedActionState.Waiting)
                    {
                        em.SetComponentData(entity, order);
                        break;
                    }

                    if (!_resourceHaulerSystem.HasReceivingCapacity(destination, resourceKind, cargo))
                    {
                        _resourceHaulerSystem.ResetActionTimer(ref order);
                        em.SetComponentData(entity, order);
                        break;
                    }

                    if (!_resourceHaulerSystem.TryCompleteUnload(destination, resourceKind, ref hauler))
                        break;
                    em.SetComponentData(entity, hauler);

                    if (!TryIssueHaulerMoveToBuilding(em, entity, source, out int2 sourceGoal))
                    {
                        _resourceHaulerSystem.SetPhase(ref order, ResourceHaulPhase.None);
                        em.SetComponentData(entity, order);
                        break;
                    }

                    _resourceHaulerSystem.SetTravelPhase(ref order, ResourceHaulPhase.ToSource, sourceGoal);
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

    private static int AxisDistance(int minA, int maxA, int minB, int maxB)
    {
        if (maxA <= minB)
            return minB - maxA;

        if (maxB <= minA)
            return minA - maxB;

        return 0;
    }

    private void CleanupRecentSpawnReservations()
    {
        _buildingSpawnSystem.CleanupRecentSpawnReservations(Time.time);
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
        return _buildingDefinitionSystem.TryGetConfiguredSpawnable(index, out entry);
    }

    public bool TryGetConfiguredSpawnable(string buildingId, out ConfiguredSpawnableEntry entry)
    {
        return _buildingDefinitionSystem.TryGetConfiguredSpawnable(buildingId, out entry);
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

        if (!_buildingDefinitionSystem.TryGetConfiguredDefinition(index, out BuildingDefinition definition) || definition.Prefab == null)
            return false;

        BeginPlacement(definition);
        return true;
    }

    public bool BeginPlacementForConfiguredSpawnable(GameObject prefab)
    {
        if (WarlineCaptureMissionRules.TryRejectBuildForActiveMission())
            return false;

        if (!_buildingDefinitionSystem.TryGetConfiguredDefinition(prefab, out BuildingDefinition definition))
            return false;

        BeginPlacement(definition);
        return true;
    }

    public bool IsConfiguredSpawnablePrefab(GameObject prefab)
    {
        return _buildingDefinitionSystem.IsConfiguredSpawnablePrefab(prefab);
    }

    public bool ConfirmBuildingPlacement()
    {
        if (!_buildingPlacementLifecycleSystem.Confirm(CreatePlacementConfirmContext()))
            return false;

        GameRuntimeStats.RecordBuildingBuilt();
        _mainMenuPlayUi?.NotifyStaticMinimapChanged();
        _preserveBuildingSelectionOnNextExitBuildMode = true;
        ExitBuildMode(clearBuildingSelection: false);
        return true;
    }

    public void CancelBuildingPlacement()
    {
        CancelActivePlacement();
        _runtimeGameplayStateSystem.BuildModeActive = false;
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
        _buildingProductionRequestSystem.CreateUnitFromBuilding(
            CreateBuildingProductionRequestContext(),
            buildingId,
            productionIndex,
            Time.frameCount);
    }

    public CampRequestFailure GetCampRequestFailure(GameObject prefab, int price, out string requiredBuildingDisplayName)
    {
        return _buildingProductionRequestSystem.GetCampRequestFailure(
            CreateBuildingProductionRequestContext(),
            prefab,
            price,
            out requiredBuildingDisplayName);
    }

    public CampRequestFailure TryRequestCampItem(GameObject prefab, int price, out string requiredBuildingDisplayName)
    {
        return TryRequestCampItem(prefab, price, out requiredBuildingDisplayName, true);
    }

    public CampRequestFailure TryRequestCampItem(GameObject prefab, int price, out string requiredBuildingDisplayName, bool focusProducerOnSuccess)
    {
        return _buildingProductionRequestSystem.TryRequestCampItem(
            CreateBuildingProductionRequestContext(),
            prefab,
            price,
            focusProducerOnSuccess,
            Time.frameCount,
            out requiredBuildingDisplayName);
    }

    public void FocusLastCampProductionRequest()
    {
        _buildingProductionRequestSystem.FocusLastCampProductionRequest(CreateBuildingProductionRequestContext());
    }

    public void ArmNextProductionFromUi()
    {
        _buildingProductionRequestSystem.ArmNextProductionFromUi(Time.frameCount);
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
        return _buildingProductionRequestSystem.CanCreateUnitFromSelectedBuilding(
            CreateBuildingProductionRequestContext(),
            ActiveBuildingId,
            productionIndex);
    }

    private bool CanQueueUnitFromBuilding(RuntimeBuildingData building, GameObject spawnUnitPrefab, bool logReason)
    {
        return _buildingProductionRequestSystem.CanQueueUnitFromBuilding(
            CreateBuildingProductionRequestContext(),
            building,
            spawnUnitPrefab,
            logReason);
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
        return _buildingCombatSystem.DeleteBuilding(
            CreateBuildingCombatContext(),
            buildingId,
            destroyVisual: true,
            Time.time,
            DestroyedBuildingLifetimeSeconds);
    }

    public void ClearSelectedBuilding()
    {
        ClearSelectedBuilding("Unknown");
    }

    public void ClearSelectedBuilding(string reason)
    {
        _buildingSelectionSystem.ClearSelectedBuilding(CreateBuildingSelectionContext());
    }

    public void ExitBuildMode()
    {
        ExitBuildMode(true);
    }

    private void ExitBuildMode(bool clearBuildingSelection)
    {
        bool shouldClearSelection = clearBuildingSelection && !_preserveBuildingSelectionOnNextExitBuildMode;
        _runtimeGameplayStateSystem.BuildModeActive = false;
        _buildingPlacementInputSystem.Reset();
        CancelActivePlacement();
        if (shouldClearSelection)
            ClearSelectedBuilding("ExitBuildMode");
        _preserveBuildingSelectionOnNextExitBuildMode = false;
        _buildingPlacementPreviewSystem.HideOutline();
        BattleHudGameplayBridge.ResolveActive()?.ClearCommandMode();
    }

    public void NotifyPlacementUiPointerDown()
    {
        _buildingPlacementLifecycleSystem.NotifyPlacementUiPointerDown(_buildingPlacementInputSystem);
    }

    public void HandleRuntimeBuildingEntityDestroyed(int buildingId, Entity blockerEntity, GameObject buildingObject)
    {
        _buildingCombatSystem.HandleRuntimeBuildingEntityDestroyed(
            CreateBuildingCombatContext(),
            buildingId,
            blockerEntity,
            buildingObject);
    }

    private void CancelActivePlacement()
    {
        _buildingPlacementLifecycleSystem.Cancel(CreatePlacementCancelContext());
    }

    private BuildingPlacementLifecycleSystem.CancelContext CreatePlacementCancelContext()
    {
        return new BuildingPlacementLifecycleSystem.CancelContext(
            _buildingPlacementInputSystem,
            _buildingPlacementPreviewSystem,
            preview => DestroyRuntimeObject(preview));
    }

    private BuildingPlacementLifecycleSystem.BeginContext CreatePlacementBeginContext()
    {
        return new BuildingPlacementLifecycleSystem.BeginContext(
            _runtimeGameplayStateSystem,
            _buildingPlacementInputSystem,
            _buildingPlacementPreviewSystem,
            _buildingRoot,
            CreateBuildingVisualInstance,
            preview => DestroyRuntimeObject(preview),
            GetCenterScreenPlacementOrigin,
            TryResolveInitialPlacementOrigin,
            UpdatePlacementVisual,
            FocusActivePlacement,
            () => BattleHudGameplayBridge.ResolveActive()?.ApplyCommandMode(TacticalCommandMode.Build),
            () => ClearSelectedBuilding("BeginPlacement"));
    }

    private BuildingPlacementLifecycleSystem.ConfirmContext CreatePlacementConfirmContext()
    {
        return new BuildingPlacementLifecycleSystem.ConfirmContext(
            ValidateActivePlacementForConfirm,
            TrySpendDollars,
            PlaceBuilding);
    }

    private void FocusActivePlacement(PlacementState placement)
    {
        if (placement != null &&
            TryGetGridData(out _, out GridConfig grid, out _, out _))
        {
            _selectionSystem?.SmoothMoveCameraGroundCenterTo(
                ResolveCurrentPlacementFocusWorldPosition(placement, grid));
        }
    }

    private bool ValidateActivePlacementForConfirm(PlacementState placement)
    {
        if (placement == null)
            return false;

        if (!BuildingBarrierSystem.IsLinearWallDefinition(placement.Definition))
            return true;

        return TryGetGridData(out _, out GridConfig grid, out DynamicBuffer<GridRoad> roads, out DynamicBlockerData blockerData) &&
               _buildingPlacementValidationSystem.AreAllPendingWallRunsValid(
                   placement,
                   _buildingPlacementInputSystem,
                   BuildingPlacementCommitSystem.GetWallSegmentFootprint,
                   grid,
                   roads,
                   blockerData,
                   CreateWallValidationContext());
    }

    private void BeginPlacement(BuildingDefinition definition)
    {
        if (WarlineCaptureMissionRules.TryRejectBuildForActiveMission())
            return;

        _buildingPlacementLifecycleSystem.Begin(definition, CreatePlacementBeginContext());
    }

    private void UpdatePlacement(Vector2 screenPosition)
    {
        PlacementState activePlacement = _buildingPlacementLifecycleSystem.ActivePlacement;
        if (activePlacement == null)
            return;

        UpdatePlacementVisual(activePlacement, _buildingPlacementInputSystem.ShouldUpdateCellFromPointer, screenPosition);
    }

    private void UpdatePlacementVisual(PlacementState placement, bool updateCellFromPointer, Vector2 screenPosition)
    {
        if (placement == null || placement.PreviewInstance == null)
            return;

        if (!TryGetGridData(out _, out GridConfig grid, out DynamicBuffer<GridRoad> roads, out DynamicBlockerData blockerData))
        {
            placement.IsValid = false;
            _buildingPlacementPreviewSystem.HideOutline();
            return;
        }

        RTSSelectionSystem selectionSystem = _selectionSystem;

        bool shouldFollowCamera = _buildingPlacementInputSystem.ApplyPointerHover(
            placement,
            updateCellFromPointer,
            screenPosition,
            grid,
            Time.time,
            TryGetGridCell,
            CenterCellToOrigin);

        if (BuildingBarrierSystem.IsLinearWallDefinition(placement.Definition))
        {
            List<Vector2Int> wallOrigins = placement.HideCurrentWallPreview
                ? new List<Vector2Int>()
                : _buildingPlacementInputSystem.BuildWallPlacementOrigins(placement, BuildingPlacementCommitSystem.GetWallSegmentFootprint);
            bool vertical = _buildingPlacementInputSystem.IsWallPlacementVertical(placement);
            placement.AutoRotateVertical = vertical;
            Vector2Int wallFootprint = BuildingPlacementCommitSystem.GetWallSegmentFootprint(placement.Definition, vertical);
            placement.IsValid = placement.HideCurrentWallPreview
                ? _buildingPlacementValidationSystem.AreAllPendingWallRunsValid(
                    placement,
                    _buildingPlacementInputSystem,
                    BuildingPlacementCommitSystem.GetWallSegmentFootprint,
                    grid,
                    roads,
                    blockerData,
                    CreateWallValidationContext())
                : _buildingPlacementValidationSystem.AreWallPlacementOriginsValid(
                    placement,
                    wallOrigins,
                    wallFootprint,
                    vertical,
                    grid,
                    roads,
                    blockerData,
                    CreateWallValidationContext(),
                    BuildingPlacementCommitSystem.GetWallSegmentFootprint);
            RebuildWallPlacementPreview(placement, wallOrigins, vertical, grid);
            _buildingPlacementPreviewSystem.UpdateWallOutline(
                _buildingPlacementInputSystem.GetAllWallPlacementOrigins(placement, wallOrigins),
                wallFootprint,
                grid,
                placement.Definition,
                placement.IsValid,
                GetFootprintCenter);
            if (shouldFollowCamera)
                selectionSystem?.FollowCameraGroundCenterTo(ResolvePlacementFocusWorldPosition(placement, grid, wallOrigins, wallFootprint));
            return;
        }

        placement.AutoRotateVertical = ResolvePlacementRotateVertical(placement);
        Vector2Int placementFootprint = GetPlacementFootprint(placement.Definition, placement.AutoRotateVertical);
        placement.IsValid = IsPlacementValid(placement.OriginCell, placementFootprint, grid, roads, blockerData);
        PositionBuildingObject(placement.PreviewInstance, placement.OriginCell, placement.Definition, grid, placement.AutoRotateVertical);
        _buildingPlacementPreviewSystem.UpdateOutline(
            placement.OriginCell,
            placementFootprint,
            grid,
            placement.Definition,
            placement.IsValid,
            GetFootprintCenter);
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

        List<Vector2Int> allOrigins = _buildingPlacementInputSystem.GetAllWallPlacementOrigins(placement, currentWallOrigins);
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

        if (BuildingBarrierSystem.IsLinearWallDefinition(placement.Definition))
        {
            bool vertical = _buildingPlacementInputSystem.IsWallPlacementVertical(placement);
            Vector2Int wallFootprint = BuildingPlacementCommitSystem.GetWallSegmentFootprint(placement.Definition, vertical);
            return ResolvePlacementFocusWorldPosition(placement, grid, _buildingPlacementInputSystem.BuildWallPlacementOrigins(placement, BuildingPlacementCommitSystem.GetWallSegmentFootprint), wallFootprint);
        }

        bool rotateVertical = ResolvePlacementRotateVertical(placement);
        Vector2Int footprint = GetPlacementFootprint(placement.Definition, rotateVertical);
        return GetFootprintCenter(placement.OriginCell, footprint, grid);
    }

    private void PlaceBuilding(PlacementState placement)
    {
        if (placement == null)
            return;

        bool hasGrid = TryGetGridData(out _, out GridConfig placementGrid, out _, out _);
        _wallCommitRuns.Clear();
        if (placement.CommittedWallRuns != null)
        {
            for (int i = 0; i < placement.CommittedWallRuns.Count; i++)
            {
                BuildingPlacementInputSystem.WallRun run = placement.CommittedWallRuns[i];
                if (run?.Origins == null || run.Origins.Count == 0)
                    continue;

                _wallCommitRuns.Add(new BuildingPlacementCommitSystem.WallRun(run.Origins, run.Vertical));
            }
        }

        List<Vector2Int> currentWallOrigins = null;
        bool currentWallVertical = false;
        if (BuildingBarrierSystem.IsLinearWallDefinition(placement.Definition))
        {
            currentWallVertical = _buildingPlacementInputSystem.IsWallPlacementVertical(placement);
            if (!placement.HideCurrentWallPreview)
                currentWallOrigins = _buildingPlacementInputSystem.BuildWallPlacementOrigins(placement, BuildingPlacementCommitSystem.GetWallSegmentFootprint);
        }

        var request = new BuildingPlacementCommitSystem.CommitRequest(
            placement.Definition,
            placement.PreviewInstance,
            placement.OriginCell,
            placement.AutoRotateVertical,
            BuildingBarrierSystem.IsLinearWallDefinition(placement.Definition),
            placement.HideCurrentWallPreview,
            _wallCommitRuns,
            currentWallOrigins,
            currentWallVertical);
        var context = new BuildingPlacementCommitSystem.CommitContext(
            _buildingRoot,
            hasGrid,
            placementGrid,
            CreateBuildingVisualInstance,
            PositionBuildingObject,
            RegisterRuntimeBuilding,
            CloneDefinitionWithFootprint,
            GetPlacementFootprint,
            BuildingPlacementCommitSystem.GetWallSegmentFootprint,
            DestroyRuntimeObject);

        RuntimeBuildingData building = _buildingPlacementCommitSystem.CommitPlacement(request, context);
        _buildingPlacementLifecycleSystem.ReleasePreviewOwnership(placement);
        if (building != null)
            SelectAndFocusBuilding(building);
    }

    private RuntimeBuildingData RegisterRuntimeBuilding(BuildingDefinition definition, GameObject instance, Vector2Int originCell, bool removeOverlappingBlockers = true)
    {
        return _buildingRuntimeCreationSystem.RegisterRuntimeBuilding(
            CreateBuildingRuntimeCreationContext(),
            definition,
            instance,
            originCell,
            removeOverlappingBlockers);
    }

    private void SelectAndFocusBuilding(RuntimeBuildingData building)
    {
        _buildingSelectionSystem.SelectAndFocusBuilding(CreateBuildingSelectionContext(), building);
    }

    private Vector3 ResolveBuildingFocusWorldPosition(RuntimeBuildingData building)
    {
        return _buildingSelectionSystem.ResolveBuildingFocusWorldPosition(CreateBuildingSelectionContext(), building);
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

        BuildingDefinition definition = _buildingDefinitionSystem.CreateRuntimeBuildingDefinition(
            prefab,
            prefab.name,
            "Defensive wall.",
            new Vector2Int(4, 1),
            500,
            _buildingRunwaySystem);
        definition.IsWall = true;
        if (!BuildingBarrierSystem.IsLinearWallDefinition(definition))
            return 0;

        bool vertical = Mathf.Abs(endOrigin.y - startOrigin.y) > Mathf.Abs(endOrigin.x - startOrigin.x);
        if (vertical)
            endOrigin.x = startOrigin.x;
        else
            endOrigin.y = startOrigin.y;

        Vector2Int wallFootprint = BuildingPlacementCommitSystem.GetWallSegmentFootprint(definition, vertical);
        List<Vector2Int> origins = BuildingPlacementCommitSystem.BuildWallRunOrigins(startOrigin, endOrigin, wallFootprint, vertical);
        int spawned = 0;
        for (int i = 0; i < origins.Count; i++)
        {
            Vector2Int origin = origins[i];
            if (!TryGetGridData(out _, out grid, out DynamicBuffer<GridRoad> currentRoads, out DynamicBlockerData currentBlockerData))
                break;

            if (!_buildingPlacementValidationSystem.IsWallPlacementValid(
                    origin,
                    wallFootprint,
                    vertical,
                    grid,
                    currentRoads,
                    currentBlockerData,
                    CreateWallValidationContext()))
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

        BuildingDefinition definition = _buildingDefinitionSystem.CreateRuntimeBuildingDefinition(
            prefab,
            prefab.name,
            "Defensive wall.",
            new Vector2Int(4, 1),
            500,
            _buildingRunwaySystem);
        definition.IsWall = true;
        footprint = BuildingPlacementCommitSystem.GetWallSegmentFootprint(definition, rotateVertical);
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

        BuildingDefinition definition = _buildingDefinitionSystem.CreateRuntimeBuildingDefinition(
            prefab,
            prefab.name,
            "Defensive wall.",
            new Vector2Int(4, 1),
            500,
            _buildingRunwaySystem);
        definition.IsWall = true;
        if (!BuildingBarrierSystem.IsLinearWallDefinition(definition))
            return false;

        Vector2Int wallFootprint = BuildingPlacementCommitSystem.GetWallSegmentFootprint(definition, rotateVertical);
        if (!_buildingPlacementValidationSystem.IsWallPlacementValid(
                origin,
                wallFootprint,
                rotateVertical,
                grid,
                roads,
                blockerData,
                CreateWallValidationContext(),
                allowExistingWallOverlap))
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

        BuildingDefinition definition = _buildingDefinitionSystem.CreateRuntimeBuildingDefinition(
            prefab,
            fallbackDisplayName,
            fallbackDescription,
            fallbackFootprint ?? new Vector2Int(10, 10),
            fallbackMaxHealth,
            _buildingRunwaySystem);
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

        BuildingDefinition definition = _buildingDefinitionSystem.CreateRuntimeBuildingDefinition(
            prefab,
            prefab.name,
            "Operational building.",
            new Vector2Int(10, 10),
            500,
            _buildingRunwaySystem);
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
        string normalizedBuildingId = BuildingDefinitionSystem.NormalizeSpawnableKey(buildingId);
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
                !BuildingDefinitionSystem.RuntimeBuildingMatchesId(building, normalizedBuildingId))
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

        EnsureEntityQueries(em);
        return _buildingSpawnSystem.TryResolveAvailableFactionHelipadSpawn(
            CreateBuildingSpawnContext(),
            factionId,
            em,
            gridEntity,
            grid,
            blockerData,
            unitFootprint,
            ref _buildingSpawnRandomState,
            out cell,
            out worldPosition);
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

    private void UpdateDestroyedBuildings()
    {
        _buildingCombatSystem.UpdateDestroyedBuildings(CreateBuildingCombatContext(), Time.time);
    }

    private void SyncDestroyedRuntimeBuildingCombatEntities()
    {
        _buildingCombatSystem.SyncDestroyedRuntimeBuildingCombatEntities(
            CreateBuildingCombatContext(),
            Time.time,
            DestroyedBuildingLifetimeSeconds);
    }

#if UNITY_EDITOR
    public void SyncDestroyedRuntimeBuildingCombatEntitiesForTests()
    {
        SyncDestroyedRuntimeBuildingCombatEntities();
    }
#endif

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
            _buildingBarrierSystem.SetBarrierDoorOpen01(building, 0f);
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
            !BuildingBarrierSystem.IsWallGateDefinition(building.Definition) ||
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

#if UNITY_EDITOR
    public void UpdateRoadBarrierDoorsForTests(float deltaTime)
    {
        _buildingBarrierSystem.UpdateRoadBarrierDoors(CreateBuildingBarrierContext(), deltaTime);
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
        return _buildingBarrierSystem.GetRuntimeRoadBarrierGateRects(CreateBuildingBarrierContext(), factionId, rects, buildingIds);
    }
#endif

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

        _buildingSelectionSystem.HandleBuildingSelectionClick(CreateBuildingSelectionContext(), screenPosition, cell);
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

        bool clickedIsOilSource = _resourceHaulerSystem.IsOilSourceBuilding(clickedBuilding);
        bool clickedIsFuelBuilding = _resourceHaulerSystem.IsFuelBuilding(clickedBuilding);
        bool clickedIsStorage = _factionResourceSystem.IsResourceStorageBuilding(clickedBuilding);
        if (!clickedIsOilSource && !clickedIsFuelBuilding && !clickedIsStorage)
            return false;

        RuntimeBuildingData source = clickedBuilding;
        RuntimeBuildingData destination = clickedBuilding;
        ResourceHaulKind resourceKind = ResourceHaulKind.Oil;
        if (clickedIsOilSource)
        {
            if (!TryFindNearestBuilding(clickedBuilding, candidate => _resourceHaulerSystem.IsFuelBuilding(candidate), out destination))
                return false;
            resourceKind = ResourceHaulKind.Oil;
        }
        else if (clickedIsFuelBuilding)
        {
            if (!TryFindNearestBuilding(clickedBuilding, candidate => _resourceHaulerSystem.IsOilSourceBuilding(candidate), out source))
                return false;
            destination = clickedBuilding;
            resourceKind = ResourceHaulKind.Oil;
        }
        else
        {
            destination = clickedBuilding;
            if (TryFindNearestBuilding(clickedBuilding, candidate => _resourceHaulerSystem.HasAvailableFuelForHauler(candidate), out source))
                resourceKind = ResourceHaulKind.Fuel;
            else if (TryFindNearestBuilding(clickedBuilding, candidate => _resourceHaulerSystem.IsOilSourceBuilding(candidate), out source))
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

            UnitResourceHaulOrder order = _resourceHaulerSystem.CreateOrder(source.Id, destination.Id, sourceGoal, resourceKind);

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

    private void PositionBuildingObject(GameObject instance, Vector2Int originCell, BuildingDefinition definition, GridConfig grid, bool rotateVertical = false)
    {
        if (instance == null)
            return;

        if (!rotateVertical &&
            _buildingBarrierSystem.ShouldAlignGateToNearbyWall(CreateBuildingBarrierContext(), originCell, definition, out bool gateVertical))
            rotateVertical = gateVertical;

        Vector2Int footprintCells = GetPlacementFootprint(definition, rotateVertical);
        Vector3 center = GetFootprintCenter(originCell, footprintCells, grid);
        Vector3 offset = Vector3.zero;
        if (definition.HasLocalBounds)
            offset = new Vector3(definition.LocalBounds.center.x, 0f, definition.LocalBounds.center.z);

        Quaternion worldRotation = BuildingPlacementCommitSystem.ResolvePlacementWorldRotation(definition, rotateVertical);
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

    private RectInt GetEffectivePlacementRect(BuildingDefinition definition, Vector2Int originCell, GridConfig grid, bool rotateVertical = false)
    {
        return _buildingRunwaySystem.GetEffectivePlacementRect(
            definition,
            originCell,
            grid,
            rotateVertical,
            buildPlaneY,
            GetPlacementFootprint);
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

    private bool ResolvePlacementRotateVertical(PlacementState placement)
    {
        if (placement?.Definition == null)
            return false;

        if (BuildingBarrierSystem.IsLinearWallDefinition(placement.Definition))
            return _buildingPlacementInputSystem.IsWallPlacementVertical(placement);

        if (_buildingBarrierSystem.ShouldAlignGateToNearbyWall(CreateBuildingBarrierContext(), placement.OriginCell, placement.Definition, out bool gateVertical))
            return gateVertical;

        return false;
    }

    private static Vector2Int GetPlacementFootprint(BuildingDefinition definition, bool rotateVertical)
    {
        if (definition == null)
            return Vector2Int.one;

        if (!rotateVertical)
            return definition.FootprintCells;

        if (BuildingBarrierSystem.IsLinearWallDefinition(definition))
            return BuildingPlacementCommitSystem.GetWallSegmentFootprint(definition, true);

        return new Vector2Int(definition.FootprintCells.y, definition.FootprintCells.x);
    }

    private void RebuildWallPlacementPreview(PlacementState placement, List<Vector2Int> origins, bool vertical, GridConfig grid)
    {
        if (placement?.PreviewInstance == null)
            return;

        _wallPreviewRuns.Clear();
        if (placement.CommittedWallRuns != null)
        {
            for (int runIndex = 0; runIndex < placement.CommittedWallRuns.Count; runIndex++)
            {
                BuildingPlacementInputSystem.WallRun run = placement.CommittedWallRuns[runIndex];
                if (run?.Origins == null)
                    continue;

                _wallPreviewRuns.Add(new BuildingPlacementPreviewSystem.WallPreviewRun(run.Origins, run.Vertical));
            }
        }

        _buildingPlacementPreviewSystem.RebuildWallPreview(
            placement.PreviewInstance,
            placement.Definition,
            _wallPreviewRuns,
            origins,
            vertical,
            placement.HideCurrentWallPreview,
            placement.IsValid,
            grid,
            CreateBuildingVisualInstance,
            PositionBuildingObject);
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
        PlacementState activePlacement = _buildingPlacementLifecycleSystem.ActivePlacement;
        return IsPlacementValid(activePlacement?.Definition, originCell, footprintCells, ResolvePlacementRotateVertical(activePlacement), grid, roads, blockerData);
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

    private BuildingPlacementValidationSystem.WallValidationContext CreateWallValidationContext()
    {
        return new BuildingPlacementValidationSystem.WallValidationContext(
            _runtimeBuildings,
            IsRuntimeBlockerCell,
            _roadBuildController != null ? _roadBuildController.HasRoadInFootprint : null);
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
        return !BuildingDefinitionSystem.RuntimeDefinitionMatchesId(
            definition,
            BuildingDefinitionSystem.NormalizeSpawnableKey("Building_Helipad"));
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

            int productionCount = BuildingDefinitionSystem.GetProductionCount(building.Definition);
            for (int i = 0; i < productionCount; i++)
            {
                if (BuildingDefinitionSystem.GetProductionPrefab(building.Definition, i) != unitPrefab)
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

    private void ProcessPendingProductions()
    {
        if (_runtimeBuildings.Count == 0)
            return;

        float now = Time.time;
        BuildingProductionTransportSystem.Context transportContext = CreateProductionTransportContext();
        foreach (var pair in _runtimeBuildings)
        {
            RuntimeBuildingData building = pair.Value;
            if (building == null || building.PendingProductions == null || building.PendingProductions.Count == 0)
            {
                _buildingProductionTransportSystem.UpdateActiveProductionTransport(transportContext, building, now, Time.deltaTime);
                continue;
            }

            _buildingProductionTransportSystem.UpdateActiveProductionTransport(transportContext, building, now, Time.deltaTime);

            for (int i = building.PendingProductions.Count - 1; i >= 0; i--)
            {
                RuntimeBuildingData.PendingProduction pending = building.PendingProductions[i];
                if (pending == null)
                {
                    _buildingProductionSystem.RemovePendingAt(building.PendingProductions, i);
                    continue;
                }

                BuildingProductionSystem.PendingProductionProgress progress = _buildingProductionSystem.GetProgress(
                    pending,
                    now,
                    pending.TransportPrefab != null);

                if (pending.TransportPrefab != null)
                {
                    float transportLaunchWindow = Mathf.Max(0.5f, pending.TransportArrivalSeconds);
                    if (_buildingProductionSystem.IsReadyWithin(pending, now, transportLaunchWindow) ||
                        _buildingProductionSystem.ShouldLaunchTransport(pending, now))
                    {
                        if (_buildingProductionTransportSystem.TryEnsureActiveProductionTransport(transportContext, building, pending, now))
                        {
                        }
                        else
                        {
                            _buildingProductionSystem.DelayPendingProduction(pending, Time.deltaTime);
                        }
                    }
                    continue;
                }

                if (progress.RemainingSeconds > 0f || !_buildingProductionSystem.IsReady(pending, now))
                    continue;

                if (TrySpawnPlayerUnitNearBuilding(building, pending.ProductionIndex, pending.ReservedProductionSlotIndex))
                    _buildingProductionSystem.RemovePendingAt(building.PendingProductions, i);
            }
        }
    }

    private BuildingProductionTransportSystem.Context CreateProductionTransportContext()
    {
        return new BuildingProductionTransportSystem.Context(
            _runtimeBuildings,
            worldCamera,
            _buildingProductionSystem,
            _buildingVisualSystem,
            _buildingRunwaySystem,
            _trySpawnPlayerUnitNearBuildingForTransport,
            _resolveProductionGroundGoalCellForTransport,
            _moveNewestProducedUnitToCellForTransport,
            _alignNewestProducedUnitRotationForTransport);
    }

    private BuildingProductionRequestSystem.Context CreateBuildingProductionRequestContext()
    {
        return new BuildingProductionRequestSystem.Context(
            _runtimeBuildings,
            _buildingDefinitionSystem.ConfiguredSpawnableDefinitions,
            _buildingDefinitionSystem.ConfiguredDefinitionsByPrefab,
            unitSpawnPrefabs,
            _buildingDefinitionSystem.UnitSpawnPrefabsByKey,
            _resourceDollars,
            _buildingProductionSystem,
            _buildingRunwaySystem,
            BuildingDefinitionSystem.GetProductionPrefab,
            BuildingDefinitionSystem.TryGetPrefabLocalBounds,
            BeginPlacementForConfiguredSpawnable,
            TrySpendDollars,
            amount => _resourceDollars += Mathf.Max(0, amount),
            _buildingPlacementLifecycleSystem.SetActivePlacementCost,
            QueuePlayerUnitProduction,
            buildingId => _runtimeBuildingSystem.SelectBuilding(buildingId),
            () => _runtimeGameplayStateSystem.SuppressNextWorldClick = true,
            RefreshBuildingMarkerVisibility,
            () => _selectionSystem?.ClearFocusedUnit(),
            position => _selectionSystem?.SmoothMoveCameraGroundCenterTo(position),
            ResolveBuildingFocusWorldPosition,
            GameRuntimeStats.RecordUnitOrdered,
            Debug.LogWarning);
    }

    private bool QueuePlayerUnitProduction(RuntimeBuildingData building, int productionIndex, GameObject spawnUnitPrefab)
    {
        if (!TryGetEntityManager(out EntityManager em))
            return false;

        return _buildingProductionSystem.TryQueuePlayerUnitFromBuilding(
            CreateBuildingProductionQueueContext(),
            building,
            productionIndex,
            spawnUnitPrefab,
            em,
            Time.time);
    }

    private BuildingProductionSystem.QueueContext CreateBuildingProductionQueueContext()
    {
        return new BuildingProductionSystem.QueueContext(
            unitSpawnPrefabs,
            _buildingDefinitionSystem.UnitSpawnPrefabsByKey,
            _buildingProductionSlotSystem,
            BuildingDefinitionSystem.TryGetPrefabLocalBounds,
            BuildingDefinitionSystem.RuntimeBuildingMatchesId);
    }

    private BuildingSpawnSystem.Context CreateBuildingSpawnContext()
    {
        return new BuildingSpawnSystem.Context(
            _runtimeBuildings,
            _liveUnitFootprintQuery,
            _buildingProductionSystem,
            _buildingSpawnPrefabSystem,
            CreateBuildingSpawnPrefabContext(),
            _buildingProductionSlotSystem,
            BuildingDefinitionSystem.GetProductionPrefab,
            BuildingDefinitionSystem.RuntimeBuildingMatchesId);
    }

    private BuildingSpawnPrefabSystem.Context CreateBuildingSpawnPrefabContext()
    {
        return new BuildingSpawnPrefabSystem.Context(
            unitSpawnPrefabs,
            _unitPrefabRegistryQuery,
            _spawnPrefabCandidatesQuery,
            _livePlayerUnitsQuery);
    }

    private BuildingRuntimeCreationSystem.Context CreateBuildingRuntimeCreationContext()
    {
        return new BuildingRuntimeCreationSystem.Context(
            _runtimeBuildingSystem,
            this,
            _deferRuntimeBuildingSideEffectsDepth > 0,
            TryGetGridForRuntimeCreation,
            (definition, origin, grid) => GetEffectivePlacementRect(definition, origin, grid),
            ShouldRuntimeBuildingBlockPathing,
            (origin, footprint) => _runtimeGridBlockerSystem?.RemoveBlockersOverlappingFootprint(origin, footprint),
            CreateBlockerEntity,
            CreateBuildingCombatEntity,
            RedirectUnitsAroundPlacedBuilding,
            rect => _deferredRedirectFootprints.Add(rect),
            () => _pendingMarkerRefresh = true,
            InitializeBuildingVisuals,
            RefreshBuildingMarkerVisibility);
    }

    private BuildingCombatSystem.Context<RuntimeBuildingData> CreateBuildingCombatContext()
    {
        return new BuildingCombatSystem.Context<RuntimeBuildingData>(
            _runtimeBuildingSystem,
            _runtimeBuildings,
            TryGetEntityManager,
            building => _buildingBarrierSystem.RememberOpenBaseBreach(CreateBuildingBarrierContext(), building),
            buildingId => _citizenPopulationSystem?.NotifyHomeBuildingDestroyed(buildingId),
            _buildingVisualSystem.SetTransformVisible,
            DestroyRuntimeObject,
            RefreshBuildingMarkerVisibility,
            () => _mainMenuPlayUi?.NotifyStaticMinimapChanged(),
            message => Debug.Log(message),
            EnableBuildingDestroyDiagnostics);
    }

    private BuildingRuntimeQuerySystem.Context CreateBuildingRuntimeQueryContext()
    {
        return new BuildingRuntimeQuerySystem.Context(
            _runtimeBuildings,
            TryGetEntityManager,
            _buildingProductionSystem,
            BuildingDefinitionSystem.NormalizeSpawnableKey,
            IsHouseBuilding,
            BuildingDefinitionSystem.RuntimeBuildingMatchesId,
            BuildingDefinitionSystem.UnitPrefabMatchesId,
            TryResolveBuildingFocusWorldPosition,
            TryGetRuntimeBuildingApproachCell,
            IsRuntimeBuildingApproachCell,
            BuildingBarrierSystem.IsWallGateDefinition);
    }

    private bool TryResolveBuildingFocusWorldPosition(RuntimeBuildingData building, out Vector3 worldPosition)
    {
        worldPosition = Vector3.zero;
        if (building == null)
            return false;

        worldPosition = ResolveBuildingFocusWorldPosition(building);
        return true;
    }

    private bool TryGetRuntimeBuildingApproachCell(RuntimeBuildingData building, int2 unitFootprint, int2 referenceCell, out int2 goal)
    {
        goal = default;
        if (building == null || building.IsDestroyed)
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

    private bool IsRuntimeBuildingApproachCell(RuntimeBuildingData building, int2 currentCell, int2 unitFootprint)
    {
        if (building == null || building.IsDestroyed)
            return false;
        if (!TryGetGridData(out _, out GridConfig grid, out _, out _))
            return false;

        return IsHaulerAtBuildingApproach(currentCell, unitFootprint, building, grid);
    }

    private bool TryGetGridForRuntimeCreation(out GridConfig grid)
    {
        return TryGetGridData(out _, out grid, out _, out _);
    }

    private BuildingSelectionSystem.Context CreateBuildingSelectionContext()
    {
        return new BuildingSelectionSystem.Context(
            _runtimeBuildingSystem,
            _runtimeBuildings,
            TryGetGridForSelection,
            GetFootprintCenter,
            () => _runtimeGameplayStateSystem.SuppressNextWorldClick = true,
            RefreshBuildingMarkerVisibility,
            () => _selectionSystem?.ClearFocusedUnit(),
            position => _selectionSystem?.SmoothMoveCameraGroundCenterTo(position),
            position => _selectionSystem != null && _selectionSystem.IsBoardablePlayerTransportClick(position),
            TryAssignSelectedHaulerOrders,
            (min, size) => _selectionSystem != null && _selectionSystem.TryIssueMoveOrderToBuilding(min, size),
            BuildingBarrierSystem.ShouldUseExpandedSelectionArea);
    }

    private BuildingPlacementQuerySystem.Context CreateBuildingPlacementQueryContext()
    {
        bool hasEntityManager = TryGetEntityManager(out EntityManager em);
        return new BuildingPlacementQuerySystem.Context(
            _runtimeBuildings,
            ActiveBuildingId,
            BuildingDefinitionSystem.GetProductionCount,
            BuildingDefinitionSystem.GetProductionPrefab,
            hasEntityManager,
            em);
    }

    private BuildingBarrierSystem.Context CreateBuildingBarrierContext()
    {
        return new BuildingBarrierSystem.Context(
            _runtimeBuildings,
            TryGetEntityManager,
            EnsureEntityQueries,
            () => _liveFactionUnitsQuery,
            BuildingBarrierSystem.IsWallGateDefinition);
    }

    private bool TryGetGridForSelection(out GridConfig grid)
    {
        return TryGetGridData(out _, out grid, out _, out _);
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

        EnsureEntityQueries(em);
        return _buildingSpawnSystem.TrySpawnPlayerUnitNearBuilding(
            CreateBuildingSpawnContext(),
            building,
            productionIndex,
            reservedProductionSlotIndex,
            overrideWorldPosition,
            overrideCell,
            em,
            gridEntity,
            grid,
            blockerData,
            ref _buildingSpawnRandomState);
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

    private bool IsPointerOverPlacementUi(Vector2 screenPosition)
    {
        return _mainMenuPlayUi != null && _mainMenuPlayUi.IsPointerOverPlacementUi(screenPosition);
    }

    private bool IsPointerOverAnyGameplayUi(Vector2 screenPosition)
    {
        return _mainMenuPlayUi != null && _mainMenuPlayUi.IsPointerOverAnyGameplayUi(screenPosition, out _);
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
