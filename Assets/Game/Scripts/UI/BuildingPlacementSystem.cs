using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using static UnityEngine.Object;
using CampRequestFailure = BuildingUiCommandSystem.CampRequestFailure;
using PendingProductionUiEntry = BuildingUiQuerySystem.PendingProductionUiEntry;
using PlacementState = BuildingPlacementLifecycleSystem.PlacementState;
using ProducedUnitUiEntry = BuildingUiQuerySystem.ProducedUnitUiEntry;

public sealed class BuildingPlacementSystem
{
    private readonly RuntimeGameplayStateSystem _runtimeGameplayStateSystem = new();

    private static readonly bool EnableBuildingDestroyDiagnostics = false;
    private const float DestroyedBuildingLifetimeSeconds = 5f;

    [SerializeField] private BuildingPlacementSystemConfig config;
    [SerializeField, HideInInspector] private Camera worldCamera;
    [SerializeField, HideInInspector] private float buildPlaneY = 0f;
    [SerializeField, HideInInspector] private float placementOutlineHeight = 0.15f;
    [SerializeField, HideInInspector] private Color placementValidColor = new(0.15f, 0.85f, 0.2f, 1f);
    [SerializeField, HideInInspector] private Color placementInvalidColor = new(0.9f, 0.2f, 0.2f, 1f);

    private readonly RuntimeBuildingSystem<RuntimeBuildingData> _runtimeBuildingSystem = new();
    private readonly BuildingVisualSystem _buildingVisualSystem = new();
    private readonly BuildingRuntimeVisualSystem _buildingRuntimeVisualSystem = new();
    private readonly BuildingCombatSystem _buildingCombatSystem = new();
    private readonly FactionResourceSystem _factionResourceSystem = new();
    private readonly ResourceHaulerSystem _resourceHaulerSystem = new();
    private readonly BuildingProductionSystem _buildingProductionSystem = new();
    private readonly BuildingProductionUpdateSystem _buildingProductionUpdateSystem = new();
    private readonly BuildingProductionTransportSystem _buildingProductionTransportSystem = new();
    private readonly BuildingProductionTransportBridgeSystem _buildingProductionTransportBridgeSystem = new();
    private readonly BuildingProductionContextSystem _buildingProductionContextSystem = new();
    private readonly BuildingSpawnSystem _buildingSpawnSystem = new();
    private readonly BuildingSpawnPrefabSystem _buildingSpawnPrefabSystem = new();
    private readonly BuildingProductionSlotSystem _buildingProductionSlotSystem = new();
    private readonly BuildingPlacementQuerySystem _buildingPlacementQuerySystem = new();
    private readonly BuildingUiQuerySystem _buildingUiQuerySystem = new();
    private readonly BuildingUiCommandSystem _buildingUiCommandSystem = new();
    private readonly BuildingUiContextSystem _buildingUiContextSystem = new();
    private readonly BuildingPlacementInteractionSystem _buildingPlacementInteractionSystem = new();
    private readonly BuildingPlacementInteractionContextSystem _buildingPlacementInteractionContextSystem = new();
    private readonly BuildingRunwaySystem _buildingRunwaySystem = new();
    private readonly BuildingPlacementValidationSystem _buildingPlacementValidationSystem = new();
    private readonly BuildingPlacementPreviewSystem _buildingPlacementPreviewSystem = new();
    private readonly BuildingPlacementCommitSystem _buildingPlacementCommitSystem = new();
    private readonly BuildingPlacementInputSystem _buildingPlacementInputSystem = new();
    private readonly BuildingPlacementContextSystem _buildingPlacementContextSystem = new();
    private readonly BuildingPlacementSessionSystem _buildingPlacementSessionSystem = new();
    private readonly BuildingProductionRequestSystem _buildingProductionRequestSystem = new();
    private readonly BuildingRuntimeCreationSystem _buildingRuntimeCreationSystem = new();
    private readonly BuildingSelectionSystem _buildingSelectionSystem = new();
    private readonly BuildingSelectionClickSystem _buildingSelectionClickSystem = new();
    private readonly BuildingBarrierSystem _buildingBarrierSystem = new();
    private readonly BuildingRuntimeQuerySystem _buildingRuntimeQuerySystem = new();
    private readonly BuildingDefinitionSystem _buildingDefinitionSystem = new();
    private readonly BuildingPlacementLifecycleSystem _buildingPlacementLifecycleSystem = new();
    private readonly BuildingPlacementGridSystem _buildingPlacementGridSystem = new();
    private readonly BuildingPlacementVisualSystem _buildingPlacementVisualSystem = new();
    private readonly BuildingRuntimeSpawnSystem _buildingRuntimeSpawnSystem = new();
    private readonly BuildingRuntimeSpawnCommandSystem _buildingRuntimeSpawnCommandSystem = new();
    private readonly BuildingRuntimeContextSystem _buildingRuntimeContextSystem = new();
    private readonly BuildingRuntimeCitySpawnSystem _buildingRuntimeCitySpawnSystem = new();
    private readonly BuildingRuntimeOwnershipSystem _buildingRuntimeOwnershipSystem = new();
    private readonly BuildingRuntimeEntitySystem _buildingRuntimeEntitySystem = new();
    private readonly BuildingPlacementRedirectSystem _buildingPlacementRedirectSystem = new();
    private readonly BuildingResourceHaulerBridgeSystem _buildingResourceHaulerBridgeSystem = new();
    private readonly BuildingRuntimeBoundarySystem _buildingRuntimeBoundarySystem = new();
    private readonly BuildingPlacementRuntimeTickSystem _buildingPlacementRuntimeTickSystem = new();
    private readonly RuntimeResourceSystem _runtimeResourceSystem = new();
    private readonly RuntimeUnitPrefabSystem _runtimeUnitPrefabSystem = new();
    private readonly BuildingRuntimeResourcePrefabContextSystem _buildingRuntimeResourcePrefabContextSystem = new();
    private int[] _placementInvalidPrefix;
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
    private EntityQuery _buildingRuntimeBoundaryQuery;
    private uint _buildingSpawnRandomState = 0x12345678u;
    private MaterialPropertyBlock _markerPropertyBlock;
    private bool _hasPlacementInvalidPrefix;
    private int _placementInvalidPrefixWidth;
    private int _placementInvalidPrefixHeight;
    private Transform _runtimeRoot;
    private readonly List<BuildingPlacementPreviewSystem.WallPreviewRun> _wallPreviewRuns = new();
    private readonly List<BuildingPlacementCommitSystem.WallRun> _wallCommitRuns = new();
    private const float OilBarrelsPerFuelBarrel = 2f;

    private int? ActiveBuildingId => _runtimeBuildingSystem.CurrentActiveBuildingId;

    public bool HasPendingBuildingPlacement => _buildingPlacementLifecycleSystem.HasPendingBuildingPlacement;
    public bool CanConfirmBuildingPlacement => _buildingPlacementLifecycleSystem.CanConfirmBuildingPlacement;
    public bool HasSelectedBuilding => _runtimeBuildingSystem.HasSelectedBuilding();
    public bool HasActiveBuilding => ActiveBuildingId.HasValue;
    public int? CurrentActiveBuildingId => ActiveBuildingId;
    internal BuildingRuntimeCitySpawnSystem RuntimeCitySpawnSystem => _buildingRuntimeCitySpawnSystem;
    internal BuildingRuntimeQuerySystem RuntimeQuerySystem => _buildingRuntimeQuerySystem;
    internal BuildingRuntimeResourcePrefabContextSystem RuntimeResourcePrefabContextSystem => _buildingRuntimeResourcePrefabContextSystem;
    internal BuildingUiCommandSystem BuildingUiCommandSystem => _buildingUiCommandSystem;
    internal BuildingUiQuerySystem BuildingUiQuerySystem => _buildingUiQuerySystem;
    internal BuildingPlacementInteractionSystem BuildingPlacementInteractionSystem => _buildingPlacementInteractionSystem;
    internal BuildingPlacementRuntimeTickSystem RuntimeTickSystem => _buildingPlacementRuntimeTickSystem;
    internal (BuildingRuntimeVisualSystem RuntimeVisual, BuildingCombatSystem Combat, BuildingBarrierSystem Barrier, BuildingPlacementRedirectSystem Redirect, float DestroyedBuildingLifetime) RuntimeTickDomains => (_buildingRuntimeVisualSystem, _buildingCombatSystem, _buildingBarrierSystem, _buildingPlacementRedirectSystem, DestroyedBuildingLifetimeSeconds);
    internal (BuildingPlacementInputSystem PlacementInput, BuildingPlacementPreviewSystem Preview, RuntimeGameplayStateSystem RuntimeState, System.Func<MainMenuPlayUI> GetMainMenu, BuildingSelectionClickSystem SelectionClick) RuntimeInputDomains => (_buildingPlacementInputSystem, _buildingPlacementPreviewSystem, _runtimeGameplayStateSystem, () => _mainMenuPlayUi, _buildingSelectionClickSystem);
    internal Camera WorldCamera => worldCamera;
    internal PlacementState ActivePlacement => _buildingPlacementLifecycleSystem.ActivePlacement;
    internal bool PlayRequested => _runtimeGameplayStateSystem.PlayRequested;
    internal bool BuildModeActive => _runtimeGameplayStateSystem.BuildModeActive;
    internal RuntimeBuildingSystem<RuntimeBuildingData> RuntimeBuildingRegistry => _runtimeBuildingSystem;
    internal DayNightSystem DayNightSystem => _dayNightSystem;
    internal FactionResourceSystem FactionResourceSystem => _factionResourceSystem;
    internal BuildingProductionUpdateSystem ProductionUpdateSystem => _buildingProductionUpdateSystem;
    internal BuildingProductionContextSystem ProductionContextSystem => _buildingProductionContextSystem;
    internal BuildingResourceHaulerBridgeSystem ResourceHaulerBridgeSystem => _buildingResourceHaulerBridgeSystem;
    internal BuildingSpawnSystem BuildingSpawnSystem => _buildingSpawnSystem;
    internal BuildingRuntimeBoundarySystem RuntimeBoundarySystem => _buildingRuntimeBoundarySystem;
    internal BuildingDefinitionSystem DefinitionSystem => _buildingDefinitionSystem;
    internal BuildingRuntimeSpawnSystem RuntimeSpawnSystem => _buildingRuntimeSpawnSystem;
    internal BuildingRuntimeContextSystem RuntimeContextSystem => _buildingRuntimeContextSystem;
    internal BuildingProductionRequestSystem ProductionRequestSystem => _buildingProductionRequestSystem;
    internal EntityQuery RuntimeBoundaryQuery => _buildingRuntimeBoundaryQuery;
    internal float OilBarrelsPerFuelBarrelRatio => OilBarrelsPerFuelBarrel;
    internal uint BuildingSpawnRandomState
    {
        get => _buildingSpawnRandomState;
        set => _buildingSpawnRandomState = value;
    }
    public BuildingSelectionClickSystem BuildingSelectionClickSystem => _buildingSelectionClickSystem;
    public GameObject RoadPreviewPrefab => config != null ? config.RoadPreviewPrefab : null;
    public float BuildButtonPreviewDistanceMultiplier => config != null ? config.BuildButtonPreviewDistanceMultiplier : 1f;
    public float UnitCommandButtonPreviewDistanceMultiplier => config != null ? config.UnitCommandButtonPreviewDistanceMultiplier : 1f;

    private bool HasVisibleSelectableBuilding(Camera camera = null)
    {
        Camera targetCamera = camera != null ? camera : worldCamera;
        if (targetCamera == null)
            return false;

        Rect screenRect = new(0f, 0f, Screen.width, Screen.height);
        foreach (KeyValuePair<int, RuntimeBuildingData> pair in _runtimeBuildingSystem.Buildings)
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
        _buildingPlacementRedirectSystem.BeginDeferredRuntimeBuildingSideEffects(RebuildPlacementInvalidPrefix);
    }

    public void EndDeferredRuntimeBuildingSideEffects()
    {
        _buildingPlacementRedirectSystem.EndDeferredRuntimeBuildingSideEffects(
            CreateBuildingPlacementRedirectContext(),
            RefreshBuildingMarkerVisibility,
            () => _hasPlacementInvalidPrefix = false);
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

    private bool IsRuntimeBuildingCityGenerated(int buildingId)
    {
        return _buildingRuntimeQuerySystem.IsRuntimeBuildingCityGenerated(CreateBuildingRuntimeQueryContext(), buildingId);
    }

    private bool IsRuntimeBuildingWall(int buildingId)
    {
        return _buildingRuntimeQuerySystem.IsRuntimeBuildingWall(CreateBuildingRuntimeQueryContext(), buildingId);
    }

    private bool TryGetRuntimeBuildingOwnerFaction(int buildingId, out byte factionId)
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
        return _buildingBarrierSystem.TryResolveBaseBreachTarget(
            CreateBuildingBarrierContext(),
            attackerFactionId,
            finalTarget,
            finalTargetCell,
            attackerCell,
            out breachTarget,
            out breachCell,
            out breachPosition,
            out reason);
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
            _buildingRuntimeResourcePrefabContextSystem.CreateBuildingSpawnPrefabContext(CreateRuntimeResourcePrefabContextSource()),
            em,
            unitPrefab,
            out prefabEntity);
    }

    public bool TrySpendDollars(int amount)
    {
        return _runtimeResourceSystem.TrySpendDollars(amount);
    }

    public void SetInitialResourceTotals(int dollars, int oilBarrels, int fuelBarrels)
    {
        _runtimeResourceSystem.SetInitialDollars(dollars);
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

    public bool IsDraggingPlacementPreview => _buildingPlacementLifecycleSystem.HasPendingBuildingPlacement && _buildingPlacementInputSystem.IsDraggingPlacement;

    public bool TryResolveSpawnUnitPrefab(Entity prefabEntity, out GameObject spawnUnitPrefab)
    {
        spawnUnitPrefab = null;
        if (prefabEntity == Entity.Null || World.DefaultGameObjectInjectionWorld == null)
            return false;

        var em = World.DefaultGameObjectInjectionWorld.EntityManager;
        EnsureEntityQueries(em);
        return _buildingSpawnPrefabSystem.TryResolveSpawnUnitPrefabFromRegistry(
            _buildingRuntimeResourcePrefabContextSystem.CreateBuildingSpawnPrefabContext(CreateRuntimeResourcePrefabContextSource()),
            em,
            prefabEntity,
            out spawnUnitPrefab);
    }

    private bool TryResolveLiveUnitPreviewPrefab(Entity unitEntity, out GameObject prefab)
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

        foreach (var pair in _runtimeBuildingSystem.Buildings)
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

    private string SelectedBuildingDisplayName
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

    private bool TryGetSelectedBuildingPreviewPrefab(out GameObject prefab)
    {
        return _buildingPlacementQuerySystem.TryGetSelectedBuildingPreviewPrefab(
            CreateBuildingPlacementQueryContext(),
            out prefab);
    }

    private bool TryGetSelectedBuildingHealth(out int current, out int max)
    {
        return _buildingPlacementQuerySystem.TryGetSelectedBuildingHealth(
            CreateBuildingPlacementQueryContext(),
            out current,
            out max);
    }

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
        IReadOnlyList<GameObject> configuredSpawnables = config.Spawnables ?? new List<GameObject>();
        UnitPrefabRegistryAuthoringConfig configuredUnitPrefabRegistry = config.UnitPrefabRegistryConfig;
        IReadOnlyList<GameObject> configuredUnitSpawnPrefabs = configuredUnitPrefabRegistry != null && configuredUnitPrefabRegistry.UnitSpawnPrefabs != null
            ? configuredUnitPrefabRegistry.UnitSpawnPrefabs
            : new List<GameObject>();
        _buildingDefinitionSystem.RebuildSpawnablesLookup(configuredSpawnables, configuredUnitSpawnPrefabs);
        buildPlaneY = config.BuildPlaneY;
        placementOutlineHeight = config.PlacementOutlineHeight;
        placementValidColor = config.PlacementValidColor;
        placementInvalidColor = config.PlacementInvalidColor;
    }

    private void RebuildConfiguredSpawnableDefinitions()
    {
        _buildingDefinitionSystem.RebuildConfiguredSpawnableDefinitions(_buildingRunwaySystem, DestroyRuntimeObject);

        _soldierBaseDefinition = _buildingDefinitionSystem.FindConfiguredDefinition("Soldier Base");
        _soldierTentDefinition = _buildingDefinitionSystem.FindConfiguredDefinition("Soldier Tent");
        _factoryDefinition = _buildingDefinitionSystem.FindConfiguredDefinition("Factory");
    }

    public void Dispose()
    {
        ExitBuildMode();

        foreach (var building in _runtimeBuildingSystem.Buildings.Values)
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
        _buildingDefinitionSystem.ClearConfiguredPrefabLookups();
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

    internal void EnsureEntityQueries(EntityManager em)
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
        _buildingRuntimeBoundaryQuery = em.CreateEntityQuery(ComponentType.ReadOnly<BuildingRuntimeBoundaryTag>());
    }

    internal bool TryGetEntityManagerForRuntimeTick(out EntityManager entityManager)
    {
        return TryGetEntityManager(out entityManager);
    }

    internal BuildingPlacementInputSystem.ActivePlacementPointerContext CreateActivePlacementPointerContext()
    {
        return _buildingPlacementContextSystem.CreateActivePlacementPointerContext(CreatePlacementContextSource());
    }

    private bool TryGetGridForPlacementInput(out GridConfig grid)
    {
        bool hasGrid = TryGetGridData(out _, out grid, out _, out _);
        return hasGrid;
    }

    private bool IsHaulerAtBuildingApproach(int2 currentCell, int2 footprintSize, RuntimeBuildingData building, GridConfig grid)
    {
        return _buildingResourceHaulerBridgeSystem.IsRuntimeBuildingApproachCell(
            _buildingProductionContextSystem.CreateResourceHaulerBridgeContext(CreateBuildingProductionContextSource()),
            building,
            currentCell,
            footprintSize);
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

    private bool BeginPlacementForConfiguredSpawnable(GameObject prefab)
    {
        if (WarlineCaptureMissionRules.TryRejectBuildForActiveMission())
            return false;

        if (!_buildingDefinitionSystem.TryGetConfiguredDefinition(prefab, out BuildingDefinition definition))
            return false;

        BeginPlacement(definition);
        return true;
    }

    public bool ConfirmBuildingPlacement()
    {
        return _buildingPlacementSessionSystem.ConfirmBuildingPlacement(CreatePlacementSessionContext());
    }

    public void CancelBuildingPlacement()
    {
        _buildingPlacementSessionSystem.CancelBuildingPlacement(CreatePlacementSessionContext());
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
            _buildingProductionContextSystem.CreateProductionRequestContext(CreateBuildingProductionContextSource()),
            buildingId,
            productionIndex,
            Time.frameCount);
    }

    private CampRequestFailure GetCampRequestFailure(GameObject prefab, int price, out string requiredBuildingDisplayName)
    {
        return _buildingProductionRequestSystem.GetCampRequestFailure(
            _buildingProductionContextSystem.CreateProductionRequestContext(CreateBuildingProductionContextSource()),
            prefab,
            price,
            out requiredBuildingDisplayName);
    }

    private CampRequestFailure TryRequestCampItem(GameObject prefab, int price, out string requiredBuildingDisplayName, bool focusProducerOnSuccess)
    {
        return _buildingProductionRequestSystem.TryRequestCampItem(
            _buildingProductionContextSystem.CreateProductionRequestContext(CreateBuildingProductionContextSource()),
            prefab,
            price,
            focusProducerOnSuccess,
            Time.frameCount,
            out requiredBuildingDisplayName);
    }

    private void FocusLastCampProductionRequest()
    {
        _buildingProductionRequestSystem.FocusLastCampProductionRequest(_buildingProductionContextSystem.CreateProductionRequestContext(CreateBuildingProductionContextSource()));
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
            _buildingProductionContextSystem.CreateProductionRequestContext(CreateBuildingProductionContextSource()),
            ActiveBuildingId,
            productionIndex);
    }

    private bool CanQueueUnitFromBuilding(RuntimeBuildingData building, GameObject spawnUnitPrefab, bool logReason)
    {
        return _buildingProductionRequestSystem.CanQueueUnitFromBuilding(
            _buildingProductionContextSystem.CreateProductionRequestContext(CreateBuildingProductionContextSource()),
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
        _buildingPlacementSessionSystem.ExitBuildMode(CreatePlacementSessionContext());
    }

    private void ExitBuildMode(bool clearBuildingSelection)
    {
        _buildingPlacementSessionSystem.ExitBuildMode(CreatePlacementSessionContext(), clearBuildingSelection);
    }

    public void NotifyPlacementUiPointerDown()
    {
        _buildingPlacementSessionSystem.NotifyPlacementUiPointerDown(CreatePlacementSessionContext());
    }

    public void HandleRuntimeBuildingEntityDestroyed(int buildingId, Entity blockerEntity, GameObject buildingObject)
    {
        _buildingCombatSystem.HandleRuntimeBuildingEntityDestroyed(
            CreateBuildingCombatContext(),
            buildingId,
            blockerEntity,
            buildingObject);
    }

    private BuildingPlacementLifecycleSystem.CancelContext CreatePlacementCancelContext()
    {
        return _buildingPlacementContextSystem.CreateCancelContext(CreatePlacementContextSource());
    }

    private BuildingPlacementContextSystem.Source CreatePlacementContextSource()
    {
        return new BuildingPlacementContextSystem.Source(
            _runtimeGameplayStateSystem,
            _buildingPlacementLifecycleSystem,
            _buildingPlacementInputSystem,
            _buildingPlacementPreviewSystem,
            _buildingPlacementValidationSystem,
            _runtimeBuildingSystem,
            _buildingRoot,
            CreateBuildingVisualInstance,
            preview => DestroyRuntimeObject(preview),
            GetCenterScreenPlacementOrigin,
            TryResolveInitialPlacementOrigin,
            UpdatePlacementVisual,
            FocusActivePlacement,
            ValidateActivePlacementForConfirm,
            TrySpendDollars,
            PlaceBuilding,
            () => BattleHudGameplayBridge.ResolveActive()?.ApplyCommandMode(TacticalCommandMode.Build),
            () => ClearSelectedBuilding("BeginPlacement"),
            TryGetGridForPlacementInput,
            TryGetGridCell,
            IsPointerOverPlacementUi,
            UpdatePlacement,
            IsRuntimeBlockerCell,
            _roadBuildController != null ? _roadBuildController.HasRoadInFootprint : (System.Func<GridConfig, Vector2Int, Vector2Int, bool>)null,
            CreateBuildingVisualInstance,
            PositionBuildingObject,
            RegisterRuntimeBuilding,
            CloneDefinitionWithFootprint,
            GetPlacementFootprint,
            DestroyRuntimeObject);
    }

    private BuildingPlacementSessionSystem.Context CreatePlacementSessionContext()
    {
        return new BuildingPlacementSessionSystem.Context(
            _runtimeGameplayStateSystem,
            _buildingPlacementLifecycleSystem,
            _buildingPlacementInputSystem,
            _buildingPlacementPreviewSystem,
            CreatePlacementCancelContext,
            CreatePlacementBeginContext,
            CreatePlacementConfirmContext,
            GameRuntimeStats.RecordBuildingBuilt,
            () => _mainMenuPlayUi?.NotifyStaticMinimapChanged(),
            ClearSelectedBuilding,
            () => BattleHudGameplayBridge.ResolveActive()?.ClearCommandMode());
    }

    private BuildingPlacementLifecycleSystem.BeginContext CreatePlacementBeginContext()
    {
        return _buildingPlacementContextSystem.CreateBeginContext(CreatePlacementContextSource());
    }

    private BuildingPlacementLifecycleSystem.ConfirmContext CreatePlacementConfirmContext()
    {
        return _buildingPlacementContextSystem.CreateConfirmContext(CreatePlacementContextSource());
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
        _buildingPlacementSessionSystem.BeginPlacement(CreatePlacementSessionContext(), definition);
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
            BuildingPlacementGridSystem.CenterCellToOrigin);

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
            {
                IReadOnlyList<Vector2Int> allOrigins = _buildingPlacementInputSystem.GetAllWallPlacementOrigins(placement, wallOrigins);
                selectionSystem?.FollowCameraGroundCenterTo(
                    _buildingPlacementGridSystem.ResolvePlacementFocusWorldPosition(
                        placement,
                        allOrigins,
                        grid,
                        wallFootprint,
                        buildPlaneY));
            }
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

    private Vector3 ResolveCurrentPlacementFocusWorldPosition(PlacementState placement, GridConfig grid)
    {
        if (placement == null)
            return Vector3.zero;

        if (BuildingBarrierSystem.IsLinearWallDefinition(placement.Definition))
        {
            bool vertical = _buildingPlacementInputSystem.IsWallPlacementVertical(placement);
            Vector2Int wallFootprint = BuildingPlacementCommitSystem.GetWallSegmentFootprint(placement.Definition, vertical);
            List<Vector2Int> currentOrigins = _buildingPlacementInputSystem.BuildWallPlacementOrigins(placement, BuildingPlacementCommitSystem.GetWallSegmentFootprint);
            IReadOnlyList<Vector2Int> allOrigins = _buildingPlacementInputSystem.GetAllWallPlacementOrigins(placement, currentOrigins);
            return _buildingPlacementGridSystem.ResolvePlacementFocusWorldPosition(
                placement,
                allOrigins,
                grid,
                wallFootprint,
                buildPlaneY);
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
        BuildingPlacementContextSystem.Source placementContextSource = CreatePlacementContextSource();
        BuildingPlacementCommitSystem.CommitRequest request = _buildingPlacementContextSystem.CreateCommitRequest(placementContextSource, placement, _wallCommitRuns);
        BuildingPlacementCommitSystem.CommitContext context = _buildingPlacementContextSystem.CreateCommitContext(placementContextSource, hasGrid, placementGrid);

        RuntimeBuildingData building = _buildingPlacementCommitSystem.CommitPlacement(request, context);
        _buildingPlacementLifecycleSystem.ReleasePreviewOwnership(placement);
        if (building != null)
            SelectAndFocusBuilding(building);
    }

    private RuntimeBuildingData RegisterRuntimeBuilding(BuildingDefinition definition, GameObject instance, Vector2Int originCell, bool removeOverlappingBlockers = true)
    {
        return _buildingRuntimeCreationSystem.RegisterRuntimeBuilding(
            _buildingRuntimeContextSystem.CreateCreationContext(CreateBuildingRuntimeContextSource()),
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
        _buildingRuntimeSpawnCommandSystem.SpawnInitialTestRoster(CreateRuntimeSpawnCommandContext(), anchorCell);
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
        return _buildingRuntimeSpawnCommandSystem.TrySpawnRuntimeBuilding(
            CreateRuntimeSpawnCommandContext(),
            prefab,
            preferredOrigin,
            out buildingId,
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
        return _buildingRuntimeSpawnCommandSystem.TrySpawnRuntimeWallRun(CreateRuntimeSpawnCommandContext(), prefab, startOrigin, endOrigin, ownerFactionId);
    }

    public bool TryGetRuntimeWallSegmentFootprint(GameObject prefab, bool rotateVertical, out Vector2Int footprint)
    {
        return _buildingRuntimeSpawnCommandSystem.TryGetRuntimeWallSegmentFootprint(CreateRuntimeSpawnCommandContext(), prefab, rotateVertical, out footprint);
    }

    public bool TrySpawnRuntimeWallSegment(
        GameObject prefab,
        Vector2Int origin,
        bool rotateVertical,
        byte? ownerFactionId = null,
        bool allowExistingWallOverlap = false)
    {
        return _buildingRuntimeSpawnCommandSystem.TrySpawnRuntimeWallSegment(CreateRuntimeSpawnCommandContext(), prefab, origin, rotateVertical, ownerFactionId, allowExistingWallOverlap);
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
        return _buildingRuntimeSpawnCommandSystem.TrySpawnRuntimeBuilding(
            CreateRuntimeSpawnCommandContext(),
            prefab,
            preferredOrigin,
            out buildingId,
            out actualOrigin,
            out actualFootprint,
            fallbackDisplayName,
            fallbackDescription,
            fallbackFootprint,
            fallbackMaxHealth,
            isCityGenerated,
            ownerFactionId,
            rotateVertical);
    }

    public bool TryGetRuntimeBuildingPlacementFootprint(GameObject prefab, bool rotateVertical, out Vector2Int footprint)
    {
        return _buildingRuntimeSpawnCommandSystem.TryGetRuntimeBuildingPlacementFootprint(CreateRuntimeSpawnCommandContext(), prefab, rotateVertical, out footprint);
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
        foreach (KeyValuePair<int, RuntimeBuildingData> entry in _runtimeBuildingSystem.Buildings)
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
        return _buildingRuntimeSpawnCommandSystem.TrySpawnInitialBuilding(CreateRuntimeSpawnCommandContext(), definition, preferredOrigin, rotateVertical, out building);
    }

    private bool TrySpawnInitialBuilding(
        BuildingDefinition definition,
        Vector2Int preferredOrigin,
        out RuntimeBuildingData building)
    {
        return _buildingRuntimeSpawnCommandSystem.TrySpawnInitialBuilding(CreateRuntimeSpawnCommandContext(), definition, preferredOrigin, out building);
    }

    private bool TryResolveInitialPlacementOrigin(BuildingDefinition definition, Vector2Int preferredOrigin, out Vector2Int resolvedOrigin)
    {
        return _buildingRuntimeSpawnCommandSystem.TryResolveInitialPlacementOrigin(CreateRuntimeSpawnCommandContext(), definition, preferredOrigin, out resolvedOrigin);
    }

#if UNITY_EDITOR
    public void SyncDestroyedRuntimeBuildingCombatEntitiesForTests()
    {
        _buildingCombatSystem.SyncDestroyedRuntimeBuildingCombatEntities(
            CreateBuildingCombatContext(),
            Time.time,
            DestroyedBuildingLifetimeSeconds);
    }

    public void TickRuntimeForTests()
    {
        var contextSystem = new BuildingPlacementRuntimeTickContextSystem();
        _buildingPlacementRuntimeTickSystem.Update(contextSystem.Create(BuildingGameplayCompositionSystem.CreateRuntimeTickSource(this)));
    }
#endif

    private void InitializeBuildingVisuals(RuntimeBuildingData building)
    {
        _buildingRuntimeVisualSystem.InitializeBuildingVisuals(
            CreateBuildingRuntimeVisualContext(),
            building);
    }

    private void SetRuntimeBuildingOwnerFaction(RuntimeBuildingData building, byte? ownerFactionId)
    {
        _buildingRuntimeOwnershipSystem.SetRuntimeBuildingOwnerFaction(
            _buildingRuntimeContextSystem.CreateOwnershipContext(CreateBuildingRuntimeContextSource()),
            building,
            ownerFactionId);
    }

    private void RefreshBuildingMarkerVisibility()
    {
        _buildingRuntimeVisualSystem.RefreshBuildingMarkerVisibility(CreateBuildingRuntimeVisualContext());
    }

#if UNITY_EDITOR
    public void UpdateRoadBarrierDoorsForTests(float deltaTime)
    {
        _buildingBarrierSystem.UpdateRoadBarrierDoors(CreateBuildingBarrierContext(), deltaTime);
    }

    public bool TryGetRuntimeBuildingDoorOpen01ForTests(int buildingId, out float open01)
    {
        open01 = 0f;
        if (!_runtimeBuildingSystem.Buildings.TryGetValue(buildingId, out RuntimeBuildingData building) || building == null)
            return false;

        open01 = building.DoorOpen01;
        return true;
    }

    public bool TryGetRuntimeBuildingEntitiesForTests(int buildingId, out Entity combatEntity, out Entity blockerEntity)
    {
        combatEntity = Entity.Null;
        blockerEntity = Entity.Null;
        if (!_runtimeBuildingSystem.Buildings.TryGetValue(buildingId, out RuntimeBuildingData building) || building == null)
            return false;

        combatEntity = building.CombatEntity;
        blockerEntity = building.BlockerEntity;
        return true;
    }

    public bool IsRuntimeBuildingDestroyedForTests(int buildingId)
    {
        return _runtimeBuildingSystem.Buildings.TryGetValue(buildingId, out RuntimeBuildingData building) &&
               building != null &&
               building.IsDestroyed;
    }

    public int GetRuntimeRoadBarrierGateRectsForTests(byte factionId, List<RectInt> rects, List<int> buildingIds = null)
    {
        return _buildingBarrierSystem.GetRuntimeRoadBarrierGateRects(CreateBuildingBarrierContext(), factionId, rects, buildingIds);
    }
#endif

    private void RedirectUnitsAroundPlacedBuilding(RectInt footprintRect)
    {
        _buildingPlacementRedirectSystem.RedirectUnitsAroundPlacedBuilding(
            CreateBuildingPlacementRedirectContext(),
            footprintRect);
    }

    private bool TryAssignSelectedHaulerOrders(int clickedBuildingId)
    {
        return _buildingResourceHaulerBridgeSystem.TryAssignSelectedHaulerOrders(
            _buildingProductionContextSystem.CreateResourceHaulerBridgeContext(CreateBuildingProductionContextSource()),
            clickedBuildingId);
    }

    private bool TryGetRuntimeBuilding(int id, out RuntimeBuildingData building)
    {
        if (_runtimeBuildingSystem.TryGetBuilding(id, out building) && building != null && !building.IsDestroyed)
            return true;

        building = null;
        return false;
    }

    private GameObject CreateBuildingVisualInstance(BuildingDefinition definition, Transform parent)
    {
        return _buildingPlacementVisualSystem.CreateBuildingVisualInstance(definition, parent);
    }

    private void PositionBuildingObject(GameObject instance, Vector2Int originCell, BuildingDefinition definition, GridConfig grid, bool rotateVertical = false)
    {
        _buildingPlacementVisualSystem.PositionBuildingObject(
            instance,
            originCell,
            definition,
            grid,
            rotateVertical,
            GetPlacementFootprint,
            GetFootprintCenter,
            TryAlignGateToNearbyWall);
    }

    private bool TryAlignGateToNearbyWall(Vector2Int originCell, BuildingDefinition definition, out bool gateVertical)
    {
        return _buildingBarrierSystem.ShouldAlignGateToNearbyWall(
            CreateBuildingBarrierContext(),
            originCell,
            definition,
            out gateVertical);
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
        if (_runtimeBuildingSystem.Buildings == null || _runtimeBuildingSystem.Buildings.Count == 0)
            return false;
        if (!TryGetGridData(out _, out GridConfig grid, out _, out _))
            return false;

        foreach (var entry in _runtimeBuildingSystem.Buildings)
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

    private Vector2Int GetPlacementFootprint(BuildingDefinition definition, bool rotateVertical)
    {
        return _buildingPlacementGridSystem.GetPlacementFootprint(definition, rotateVertical);
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
        return BuildingRuntimeSpawnSystem.CloneDefinitionWithFootprint(definition, footprintCells);
    }

    private Vector3 GetFootprintCenter(Vector2Int originCell, Vector2Int footprintCells, GridConfig grid)
    {
        return _buildingPlacementGridSystem.GetFootprintCenter(originCell, footprintCells, grid, buildPlaneY);
    }

    private Vector2Int GetCenterScreenPlacementOrigin(Vector2Int footprintCells)
    {
        if (!TryGetGridData(out _, out GridConfig grid, out _, out _))
            return Vector2Int.zero;

        return _buildingPlacementGridSystem.GetCenterScreenPlacementOrigin(
            footprintCells,
            grid,
            worldCamera,
            buildPlaneY,
            new Vector2(Screen.width, Screen.height));
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
        return _buildingPlacementContextSystem.CreateWallValidationContext(CreatePlacementContextSource());
    }

    private Entity CreateBlockerEntity(BuildingDefinition definition, Vector2Int originCell, Vector2Int footprintCells)
    {
        return _buildingRuntimeEntitySystem.CreateBlockerEntity(
            CreateBuildingRuntimeEntityContext(),
            definition,
            originCell,
            footprintCells);
    }

    private bool ShouldRuntimeBuildingBlockPathing(BuildingDefinition definition)
    {
        return _buildingRuntimeEntitySystem.ShouldRuntimeBuildingBlockPathing(definition);
    }

    private Entity CreateBuildingCombatEntity(Vector2Int originCell, BuildingDefinition definition, byte ownerFactionId, Quaternion worldRotation)
    {
        return _buildingRuntimeEntitySystem.CreateBuildingCombatEntity(
            CreateBuildingRuntimeEntityContext(),
            originCell,
            definition,
            ownerFactionId,
            worldRotation);
    }

    internal BuildingProductionContextSystem.Source CreateBuildingProductionContextSource()
    {
        return new BuildingProductionContextSystem.Source(
            _runtimeBuildingSystem.Buildings,
            worldCamera,
            _buildingDefinitionSystem,
            _buildingProductionSystem,
            _buildingProductionUpdateSystem,
            _buildingProductionTransportSystem,
            _buildingProductionTransportBridgeSystem,
            _buildingProductionSlotSystem,
            _buildingRunwaySystem,
            _buildingVisualSystem,
            _buildingSpawnSystem,
            CreateBuildingSpawnContext(),
            _runtimeResourceSystem.CurrentDollars,
            BeginPlacementForConfiguredSpawnable,
            TrySpendDollars,
            _runtimeResourceSystem.AddDollars,
            cost => _buildingPlacementSessionSystem.SetActivePlacementCost(CreatePlacementSessionContext(), cost),
            QueuePlayerUnitProduction,
            buildingId => _runtimeBuildingSystem.SelectBuilding(buildingId),
            () => _runtimeGameplayStateSystem.SuppressNextWorldClick = true,
            RefreshBuildingMarkerVisibility,
            () => _selectionSystem?.ClearFocusedUnit(),
            position => _selectionSystem?.SmoothMoveCameraGroundCenterTo(position),
            ResolveBuildingFocusWorldPosition,
            GameRuntimeStats.RecordUnitOrdered,
            Debug.LogWarning,
            ResolvePendingProductionCountForFaction,
            ResolveRuntimeProducedUnitCountForFaction,
            _resourceHaulerSystem,
            _factionResourceSystem,
            TryGetEntityManager,
            TryGetGridData,
            EnsureEntityQueries,
            () => _haulerUnitsQuery,
            () => _selectedUnitsQuery,
            TryGetRuntimeBuilding,
            (building, grid) => GetEffectivePlacementRect(building.Definition, building.OriginCell, grid));
    }

    private int ResolvePendingProductionCountForFaction(byte factionId, string unitId)
    {
        return _buildingRuntimeQuerySystem.CountPendingProductionsForFaction(CreateBuildingRuntimeQueryContext(), factionId, unitId);
    }

    private int ResolveRuntimeProducedUnitCountForFaction(byte factionId, string unitId)
    {
        return _buildingRuntimeQuerySystem.CountRuntimeProducedUnitsForFaction(CreateBuildingRuntimeQueryContext(), factionId, unitId);
    }

    private bool QueuePlayerUnitProduction(RuntimeBuildingData building, int productionIndex, GameObject spawnUnitPrefab)
    {
        if (!TryGetEntityManager(out EntityManager em))
            return false;

        return _buildingProductionSystem.TryQueuePlayerUnitFromBuilding(
            _buildingProductionContextSystem.CreateProductionQueueContext(CreateBuildingProductionContextSource()),
            building,
            productionIndex,
            spawnUnitPrefab,
            em,
            Time.time);
    }

    private BuildingSpawnSystem.Context CreateBuildingSpawnContext()
    {
        return _buildingRuntimeContextSystem.CreateBuildingSpawnContext(CreateRuntimeContextSystemSource());
    }

    internal BuildingRuntimeContextSystem.Source CreateBuildingRuntimeContextSource()
    {
        return new BuildingRuntimeContextSystem.Source(
            _buildingRoot,
            _buildingDefinitionSystem,
            _buildingRunwaySystem,
            _buildingPlacementValidationSystem,
            CreateWallValidationContext(),
            TryGetGridData,
            GetPlacementFootprint,
            GetEffectivePlacementRect,
            IsPlacementValid,
            HasCachedInvalidCellInFootprint,
            CreateBuildingVisualInstance,
            PositionBuildingObject,
            RegisterRuntimeBuilding,
            SetRuntimeBuildingOwnerFaction,
            _runtimeBuildingSystem,
            _buildingPlacementInteractionSystem,
            CreateBuildingPlacementInteractionContext(),
            () => _buildingPlacementRedirectSystem.IsDeferringSideEffects,
            (out GridConfig grid) => TryGetGridData(out _, out grid, out _, out _),
            (definition, origin, grid) => GetEffectivePlacementRect(definition, origin, grid),
            ShouldRuntimeBuildingBlockPathing,
            (origin, footprint) => _runtimeGridBlockerSystem?.RemoveBlockersOverlappingFootprint(origin, footprint),
            CreateBlockerEntity,
            CreateBuildingCombatEntity,
            RedirectUnitsAroundPlacedBuilding,
            _buildingPlacementRedirectSystem.AddDeferredRedirectFootprint,
            _buildingPlacementRedirectSystem.MarkPendingMarkerRefresh,
            InitializeBuildingVisuals,
            RefreshBuildingMarkerVisibility,
            TryGetEntityManager,
            _buildingVisualSystem,
            _factionVisualSettings,
            _markerPropertyBlock,
            DeleteBuildingById,
            BeginDeferredRuntimeBuildingSideEffects,
            EndDeferredRuntimeBuildingSideEffects);
    }

    private BuildingRuntimeContextSystem.RuntimeSource CreateRuntimeContextSystemSource()
    {
        return new BuildingRuntimeContextSystem.RuntimeSource(
            _runtimeBuildingSystem,
            _buildingProductionSystem,
            _buildingProductionSlotSystem,
            _buildingSpawnPrefabSystem,
            _buildingRuntimeResourcePrefabContextSystem.CreateBuildingSpawnPrefabContext(CreateRuntimeResourcePrefabContextSource()),
            _buildingVisualSystem,
            _buildingRuntimeVisualSystem,
            _buildingBarrierSystem,
            _buildingResourceHaulerBridgeSystem,
            _buildingProductionContextSystem,
            CreateBuildingProductionContextSource(),
            _factionVisualSettings,
            _markerPropertyBlock,
            _liveUnitFootprintQuery,
            _redirectUnitsQuery,
            _liveFactionUnitsQuery,
            () => ActiveBuildingId,
            TryGetEntityManager,
            TryGetGridData,
            EnsureEntityQueries,
            GetFootprintCenter,
            IsHouseBuilding,
            TryResolveBuildingFocusWorldPosition,
            TryGetRuntimeBuildingApproachCell,
            IsRuntimeBuildingApproachCell,
            building => _buildingBarrierSystem.RememberOpenBaseBreach(CreateBuildingBarrierContext(), building),
            buildingId => _citizenPopulationSystem?.NotifyHomeBuildingDestroyed(buildingId),
            DestroyRuntimeObject,
            RefreshBuildingMarkerVisibility,
            () => _mainMenuPlayUi?.NotifyStaticMinimapChanged(),
            message => Debug.Log(message),
            EnableBuildingDestroyDiagnostics);
    }

    private BuildingRuntimeSpawnCommandSystem.Context CreateRuntimeSpawnCommandContext()
    {
        return new BuildingRuntimeSpawnCommandSystem.Context(
            _buildingRuntimeSpawnSystem,
            _buildingRuntimeContextSystem.CreateSpawnContext(CreateBuildingRuntimeContextSource()),
            _soldierBaseDefinition,
            _soldierTentDefinition,
            _factoryDefinition);
    }

    internal BuildingRuntimeQuerySystem.Context CreateRuntimeBuildingQueryContext()
    {
        return CreateBuildingRuntimeQueryContext();
    }

    internal BuildingRuntimeResourcePrefabContextSystem.Source CreateRuntimeResourcePrefabContextSource()
    {
        return new BuildingRuntimeResourcePrefabContextSystem.Source(
            _runtimeResourceSystem,
            _runtimeUnitPrefabSystem,
            _buildingDefinitionSystem,
            _buildingSpawnPrefabSystem,
            TryGetEntityManager,
            EnsureEntityQueries,
            _unitPrefabRegistryQuery,
            _spawnPrefabCandidatesQuery,
            _livePlayerUnitsQuery);
    }

    internal BuildingUiCommandSystem.Context CreateBuildingUiCommandContext()
    {
        return _buildingUiContextSystem.CreateCommandContext(CreateBuildingUiContextSource());
    }

    internal BuildingUiQuerySystem.Context CreateBuildingUiQueryContext()
    {
        return _buildingUiContextSystem.CreateQueryContext(CreateBuildingUiContextSource());
    }

    private BuildingUiContextSystem.Source CreateBuildingUiContextSource()
    {
        return new BuildingUiContextSystem.Source(
            _runtimeResourceSystem,
            _buildingDefinitionSystem,
            _runtimeBuildingSystem,
            _buildingProductionSystem,
            () => ActiveBuildingId,
            TryGetEntityManager,
            () => Time.time,
            () => HasActiveBuilding,
            () => SelectedBuildingDisplayName,
            TryGetSelectedBuildingHealth,
            TryGetSelectedBuildingPreviewPrefab,
            IsRuntimeBuildingWall,
            IsRuntimeBuildingCityGenerated,
            TryGetRuntimeBuildingOwnerFaction,
            HasVisibleSelectableBuilding,
            TryResolveLiveUnitPreviewPrefab,
            GetCampRequestFailure,
            TryRequestCampItem,
            DeleteSelectedBuilding,
            ConfirmBuildingPlacement,
            CancelBuildingPlacement,
            FocusLastCampProductionRequest,
            ClearSelectedBuilding,
            ExitBuildMode);
    }

    internal BuildingPlacementInteractionSystem.Context CreateBuildingPlacementInteractionContext()
    {
        return _buildingPlacementInteractionContextSystem.CreateContext(CreateBuildingPlacementInteractionContextSource());
    }

    private BuildingPlacementInteractionContextSystem.Source CreateBuildingPlacementInteractionContextSource()
    {
        return new BuildingPlacementInteractionContextSystem.Source(
            () => HasPendingBuildingPlacement,
            () => CanConfirmBuildingPlacement,
            () => HasSelectedBuilding,
            () => HasActiveBuilding,
            () => IsDraggingPlacementPreview,
            () => PlacementStatusText,
            () => SelectedBuildingLabel,
            BeginSoldierBasePlacement,
            ConfirmBuildingPlacement,
            CancelBuildingPlacement,
            CreateUnitFromSelectedBuilding,
            DeleteSelectedBuilding,
            ClearSelectedBuilding,
            HandleRuntimeBuildingEntityDestroyed,
            TryResolveBaseBreachTarget);
    }

    private BuildingRuntimeEntitySystem.Context CreateBuildingRuntimeEntityContext()
    {
        return _buildingRuntimeContextSystem.CreateRuntimeEntityContext(CreateRuntimeContextSystemSource());
    }

    internal BuildingRuntimeVisualSystem.Context CreateBuildingRuntimeVisualContext()
    {
        return _buildingRuntimeContextSystem.CreateRuntimeVisualContext(CreateRuntimeContextSystemSource());
    }

    internal BuildingPlacementRedirectSystem.Context CreateBuildingPlacementRedirectContext()
    {
        return _buildingRuntimeContextSystem.CreateRedirectContext(CreateRuntimeContextSystemSource());
    }

    internal BuildingCombatSystem.Context<RuntimeBuildingData> CreateBuildingCombatContext()
    {
        return _buildingRuntimeContextSystem.CreateCombatContext(CreateRuntimeContextSystemSource());
    }

    internal BuildingRuntimeQuerySystem.Context CreateBuildingRuntimeQueryContext()
    {
        return _buildingRuntimeContextSystem.CreateRuntimeQueryContext(CreateRuntimeContextSystemSource());
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
        return _buildingResourceHaulerBridgeSystem.TryGetRuntimeBuildingApproachCell(
            _buildingProductionContextSystem.CreateResourceHaulerBridgeContext(CreateBuildingProductionContextSource()),
            building,
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

    private BuildingSelectionSystem.Context CreateBuildingSelectionContext()
    {
        return new BuildingSelectionSystem.Context(
            _runtimeBuildingSystem,
            _runtimeBuildingSystem.Buildings,
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

    public BuildingSelectionClickSystem.Context CreateBuildingSelectionClickContext()
    {
        return new BuildingSelectionClickSystem.Context(
            () => UnitPathfindingSystem.HasPendingPathJob,
            TryGetGridForSelection,
            TryGetGridCell,
            (screenPosition, cell) => _buildingSelectionSystem.HandleBuildingSelectionClick(
                CreateBuildingSelectionContext(),
                screenPosition,
                cell));
    }

    private BuildingPlacementQuerySystem.Context CreateBuildingPlacementQueryContext()
    {
        bool hasEntityManager = TryGetEntityManager(out EntityManager em);
        return new BuildingPlacementQuerySystem.Context(
            _runtimeBuildingSystem.Buildings,
            ActiveBuildingId,
            BuildingDefinitionSystem.GetProductionCount,
            BuildingDefinitionSystem.GetProductionPrefab,
            hasEntityManager,
            em);
    }

    internal BuildingBarrierSystem.Context CreateBuildingBarrierContext()
    {
        return _buildingRuntimeContextSystem.CreateBarrierContext(CreateRuntimeContextSystemSource());
    }

    private bool TryGetGridForSelection(out GridConfig grid)
    {
        return TryGetGridData(out _, out grid, out _, out _);
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
        return _buildingPlacementGridSystem.TryGetGridCell(screenPosition, grid, worldCamera, buildPlaneY, out cell);
    }

    private bool IsPointerOverPlacementUi(Vector2 screenPosition)
    {
        return _mainMenuPlayUi != null && _mainMenuPlayUi.IsPointerOverPlacementUi(screenPosition);
    }

}
