using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public sealed class RuntimeCitySpawnerSystem
{
    private static readonly bool EnableRuntimeCityDiagnostics = false;

    public static RuntimeCitySpawnerSystem Instance { get; private set; }

    private enum YardSide
    {
        North,
        East,
        South,
        West
    }

    [Serializable]
    private struct PlotCandidate
    {
        public Vector2Int PlotCell;
        public int DistanceFromCenter;
    }

    private struct ReservedFootprint
    {
        public RectInt Rect;
        public int ClearanceCells;
    }

    private struct AutobahnAnchorCandidate
    {
        public Vector2Int AnchorCell;
        public Vector2Int OutwardDirection;
        public int Score;
    }

    private enum CityChainAxis
    {
        Horizontal,
        Vertical
    }

    private sealed class CityLayoutData
    {
        public Vector2Int CenterRoadCell;
        public int TownRadius;
        public int ChainCoordinate;
        public bool HallPlaced;
        public bool HasIncomingAnchor;
        public Vector2Int IncomingAnchorCell;
        public Vector2Int IncomingOutwardDirection;
        public List<List<Vector2Int>> RoadStrokes = new();
        public HashSet<Vector2Int> RoadCells = new();
        public List<ReservedFootprint> ReservedFootprints = new();
    }

    private RuntimeCitySpawnerSystemConfig config;

    private bool spawnOnStart = true;
    private bool generateBuildings = true;
    private uint randomSeed = 24681357;
    private int cityCount = 1;
    private Vector2Int startCell = new(180, 180);
    private int generationYieldInterval;

    private int gasStationCount = 3;
    private int shopCount = 20;
    private int houseCount = 32;
    private int otherBuildingCount = 8;
    private int cityDecorationBuildingCount = 16;

    private int hallPlazaRadiusRoadCells = 2;
    private int extraTownRadiusRoadCells = 5;
    private int cityMinSpacingRoadCells = 16;
    private float ruralHouseRatio = 0.35f;
    private int gasStationMinSpacingRoadCells = 3;
    private float houseWallChance = 0.5f;
    private int houseWallMinDistanceCells = 2;
    private int houseWallMaxDistanceCells = 4;
    private int landmarkMinDistanceFromHallRoadCells = 3;
    private int landmarkClearanceCells = 4;
    private int autobahnMinLengthRoadCells = 8;
    private int autobahnEdgeMarginRoadCells = 3;
    private int defaultBuildingMaxHealth = 300;

    private GameObject clockTowerPrefab;
    private List<GameObject> fountainPrefabs = new();
    private List<GameObject> monumentPrefabs = new();
    private List<GameObject> pillarPrefabs = new();
    private List<GameObject> hallPrefabs = new();
    private List<GameObject> gasStationPrefabs = new();
    private List<GameObject> shopPrefabs = new();
    private List<GameObject> housePrefabs = new();
    private List<GameObject> otherBuildingPrefabs = new();
    private List<GameObject> cityDecorationPrefabs = new();
    private List<GameObject> houseWallPrefabs = new();
    private GameObject houseWallGatePrefab;
    private GameObject houseWallPillarPrefab;

    private BuildingPlacementSystem _buildingPlacementController;
    private RoadBuildSystem _roadBuildController;
    private Transform _cityVisualRoot;
    private IEnumerator _generationRoutine;
    private int _generationStartedFrame = -1;
    private int _generationMoveNextCount;
    private int _nextGenerationDiagnosticFrame;
    private int _nextInitialSpawnWaitDiagnosticFrame;
    private bool _spawned;
    private World _queryWorld;
    private EntityQuery _gridDataQuery;
    private readonly Dictionary<GameObject, Vector2Int> _prefabFootprintCache = new();
    private Transform _runtimeRoot;

    public bool SpawnOnStartEnabled => spawnOnStart;
    public bool HasSpawned => _spawned || cityCount <= 0;
    public bool IsGenerating => _generationRoutine != null;

    private static readonly Vector2Int North = new(0, 1);
    private static readonly Vector2Int East = new(1, 0);
    private static readonly Vector2Int South = new(0, -1);
    private static readonly Vector2Int West = new(-1, 0);
    private static readonly Vector2Int[] CardinalDirections = { North, East, South, West };

    public void Init(
        RuntimeCitySpawnerSystemConfig configAsset,
        RoadBuildSystem roadBuildController,
        BuildingPlacementSystem buildingPlacementController,
        Transform runtimeRoot)
    {
        Instance = this;
        config = configAsset;
        _roadBuildController = roadBuildController;
        _buildingPlacementController = buildingPlacementController;
        _runtimeRoot = runtimeRoot;
        ApplyConfigIfAvailable();
    }

    private bool ShouldYield(int completedWorkItems)
    {
        return generationYieldInterval > 0 &&
            completedWorkItems > 0 &&
            (completedWorkItems % generationYieldInterval) == 0;
    }

    public void Update()
    {
        ApplyConfigIfAvailable();
        if (_generationRoutine != null)
        {
            _generationMoveNextCount++;
            if (EnableRuntimeCityDiagnostics && Time.frameCount >= _nextGenerationDiagnosticFrame)
            {
                _nextGenerationDiagnosticFrame = Time.frameCount + 120;
                Debug.Log($"[RuntimeCityState] frame={Time.frameCount} reason=generating ageFrames={Time.frameCount - _generationStartedFrame} steps={_generationMoveNextCount} cityCount={cityCount} generateBuildings={(generateBuildings ? 1 : 0)} yieldInterval={generationYieldInterval}");
            }

            if (!_generationRoutine.MoveNext())
            {
                if (EnableRuntimeCityDiagnostics)
                    Debug.Log($"[RuntimeCityState] frame={Time.frameCount} reason=ended spawned={(_spawned ? 1 : 0)} ageFrames={Time.frameCount - _generationStartedFrame} steps={_generationMoveNextCount}");
                _generationRoutine = null;
            }
        }

        TryAutoSpawn();
    }

    public void Dispose()
    {
        if (Instance == this)
            Instance = null;
        _generationRoutine = null;
        _cityVisualRoot = null;
        _runtimeRoot = null;
    }

    public bool IsConfiguredHousePrefab(GameObject prefab)
    {
        if (prefab == null || housePrefabs == null)
            return false;

        for (int i = 0; i < housePrefabs.Count; i++)
        {
            if (housePrefabs[i] == prefab)
                return true;
        }

        return false;
    }

    private void ApplyConfigIfAvailable()
    {
        if (config == null)
            return;

        spawnOnStart = config.SpawnOnStart;
        generateBuildings = config.GenerateBuildings;
        randomSeed = config.RandomSeed;
        cityCount = config.CityCount;
        startCell = config.StartCell;
        generationYieldInterval = config.GenerationYieldInterval;
        gasStationCount = config.GasStationCount;
        shopCount = config.ShopCount;
        houseCount = config.HouseCount;
        otherBuildingCount = config.OtherBuildingCount;
        cityDecorationBuildingCount = config.CityDecorationBuildingCount;
        hallPlazaRadiusRoadCells = config.HallPlazaRadiusRoadCells;
        extraTownRadiusRoadCells = config.ExtraTownRadiusRoadCells;
        cityMinSpacingRoadCells = config.CityMinSpacingRoadCells;
        ruralHouseRatio = config.RuralHouseRatio;
        gasStationMinSpacingRoadCells = config.GasStationMinSpacingRoadCells;
        houseWallChance = config.HouseWallChance;
        houseWallMinDistanceCells = config.HouseWallMinDistanceCells;
        houseWallMaxDistanceCells = config.HouseWallMaxDistanceCells;
        landmarkMinDistanceFromHallRoadCells = config.LandmarkMinDistanceFromHallRoadCells;
        landmarkClearanceCells = config.LandmarkClearanceCells;
        autobahnMinLengthRoadCells = config.AutobahnMinLengthRoadCells;
        autobahnEdgeMarginRoadCells = config.AutobahnEdgeMarginRoadCells;
        defaultBuildingMaxHealth = config.DefaultBuildingMaxHealth;
        clockTowerPrefab = config.ClockTowerPrefab;
        fountainPrefabs = config.FountainPrefabs ?? new List<GameObject>();
        monumentPrefabs = config.MonumentPrefabs ?? new List<GameObject>();
        pillarPrefabs = config.PillarPrefabs ?? new List<GameObject>();
        hallPrefabs = config.HallPrefabs ?? new List<GameObject>();
        gasStationPrefabs = config.GasStationPrefabs ?? new List<GameObject>();
        shopPrefabs = config.ShopPrefabs ?? new List<GameObject>();
        housePrefabs = config.HousePrefabs ?? new List<GameObject>();
        otherBuildingPrefabs = config.OtherBuildingPrefabs ?? new List<GameObject>();
        cityDecorationPrefabs = config.CityDecorationPrefabs ?? new List<GameObject>();
        houseWallPrefabs = config.HouseWallPrefabs ?? new List<GameObject>();
        houseWallGatePrefab = config.HouseWallGatePrefab;
        houseWallPillarPrefab = config.HouseWallPillarPrefab;
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
    }

    private void TryAutoSpawn()
    {
        if (!spawnOnStart || _spawned)
            return;
        if (cityCount <= 0)
            return;
        if (!InitialUnitsRuntimeState.PlayRequested)
            return;
        if (Chapter01M01PlayableRuntime.IsActiveMission())
        {
            _spawned = true;
            _generationRoutine = null;
            return;
        }
        if (HasPendingInitialUnitsSpawn(out int initialSpawnConfigs, out int initializedInitialSpawnConfigs))
        {
            if (EnableRuntimeCityDiagnostics && Time.frameCount >= _nextInitialSpawnWaitDiagnosticFrame)
            {
                _nextInitialSpawnWaitDiagnosticFrame = Time.frameCount + 120;
                Debug.Log($"[RuntimeCityState] frame={Time.frameCount} reason=waiting-initial-units configs={initialSpawnConfigs} initialized={initializedInitialSpawnConfigs}");
            }

            return;
        }

        _buildingPlacementController ??= BuildingPlacementSystem.Instance;
        _roadBuildController ??= RoadBuildSystem.Instance;

        if (_roadBuildController == null)
            return;
        if (generateBuildings && _buildingPlacementController == null)
            return;
        if (!_roadBuildController.TryGetRoadCellSizeInGridCells(out int roadCellSizeInGridCells))
            return;
        if (!TryGetGridData(out _, out GridConfig grid, out _, out _))
            return;
        if (hallPrefabs == null || hallPrefabs.Count == 0 || shopPrefabs == null || shopPrefabs.Count == 0 || housePrefabs == null || housePrefabs.Count == 0)
            return;

        GenerateCity(grid, roadCellSizeInGridCells);
    }

    private static bool HasPendingInitialUnitsSpawn(out int totalConfigs, out int initializedConfigs)
    {
        totalConfigs = 0;
        initializedConfigs = 0;

        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return false;

        EntityManager em = world.EntityManager;
        EntityQuery configQuery = em.CreateEntityQuery(ComponentType.ReadOnly<InitialUnitsSpawnConfig>());
        EntityQuery initializedQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<InitialUnitsSpawnConfig>(),
            ComponentType.ReadOnly<InitialUnitsSpawnInitialized>());

        totalConfigs = configQuery.CalculateEntityCount();
        initializedConfigs = initializedQuery.CalculateEntityCount();
        configQuery.Dispose();
        initializedQuery.Dispose();

        return totalConfigs > 0 && initializedConfigs < totalConfigs;
    }

    private static List<RectInt> CollectInitialBaseExclusionRoadRects(int roadCellSizeInGridCells)
    {
        var exclusions = new List<RectInt>();
        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return exclusions;

        EntityManager em = world.EntityManager;
        using EntityQuery configQuery = em.CreateEntityQuery(ComponentType.ReadOnly<InitialUnitsSpawnConfig>());
        using var entities = configQuery.ToEntityArray(Allocator.Temp);
        int roadCellSize = Mathf.Max(1, roadCellSizeInGridCells);

        for (int entityIndex = 0; entityIndex < entities.Length; entityIndex++)
        {
            Entity entity = entities[entityIndex];
            if (!em.Exists(entity) ||
                !em.HasComponent<InitialUnitsSpawnConfig>(entity) ||
                !em.HasBuffer<InitialUnitsFactionSpawnEntry>(entity))
                continue;

            InitialUnitsSpawnConfig config = em.GetComponentData<InitialUnitsSpawnConfig>(entity);
            if (config.CreateFactionBases == 0)
                continue;

            int halfWidthRoadCells = Mathf.CeilToInt((config.BaseHalfWidthCells + 220) / (float)roadCellSize);
            int halfHeightRoadCells = Mathf.CeilToInt((config.BaseHalfHeightCells + 220) / (float)roadCellSize);
            DynamicBuffer<InitialUnitsFactionSpawnEntry> spawns = em.GetBuffer<InitialUnitsFactionSpawnEntry>(entity);
            for (int i = 0; i < spawns.Length; i++)
            {
                Vector2Int center = new(spawns[i].SpawnCell.x / roadCellSize, spawns[i].SpawnCell.y / roadCellSize);
                exclusions.Add(new RectInt(
                    center.x - halfWidthRoadCells,
                    center.y - halfHeightRoadCells,
                    halfWidthRoadCells * 2 + 1,
                    halfHeightRoadCells * 2 + 1));
            }
        }

        return exclusions;
    }

    public void GenerateCity()
    {
        if (_spawned)
            return;
        if (cityCount <= 0)
            return;

        _buildingPlacementController ??= BuildingPlacementSystem.Instance;
        _roadBuildController ??= RoadBuildSystem.Instance;

        if (_roadBuildController == null)
            return;
        if (generateBuildings && _buildingPlacementController == null)
            return;
        if (!_roadBuildController.TryGetRoadCellSizeInGridCells(out int roadCellSizeInGridCells))
            return;
        if (!TryGetGridData(out _, out GridConfig grid, out _, out _))
            return;

        GenerateCity(grid, roadCellSizeInGridCells);
    }

    private void GenerateCity(GridConfig grid, int roadCellSizeInGridCells)
    {
        if (_spawned || _generationRoutine != null)
            return;
        if (cityCount <= 0)
            return;

        _generationStartedFrame = Time.frameCount;
        _generationMoveNextCount = 0;
        _nextGenerationDiagnosticFrame = Time.frameCount;
        if (EnableRuntimeCityDiagnostics)
            Debug.Log($"[RuntimeCityState] frame={Time.frameCount} reason=start cityCount={cityCount} generateBuildings={(generateBuildings ? 1 : 0)} yieldInterval={generationYieldInterval}");
        _generationRoutine = GenerateCityRoutine(grid, roadCellSizeInGridCells);
    }

    private IEnumerator GenerateCityRoutine(GridConfig grid, int roadCellSizeInGridCells)
    {
        if (_spawned)
        {
            _generationRoutine = null;
            yield break;
        }
        if (cityCount <= 0)
        {
            _generationRoutine = null;
            yield break;
        }

        if (randomSeed == 0)
            randomSeed = 1;

        var rng = new Unity.Mathematics.Random(randomSeed);
        int townRadius = CalculateTownRadius();
        List<RectInt> baseExclusionRoadRects = CollectInitialBaseExclusionRoadRects(roadCellSizeInGridCells);
        var cities = new List<CityLayoutData>(Mathf.Max(0, cityCount));
        var occupiedRoadCells = new HashSet<Vector2Int>();
        _roadBuildController?.BeginDeferredRoadEcsSync();
        if (generateBuildings)
            _buildingPlacementController?.BeginDeferredRuntimeBuildingSideEffects();

        try
        {
            Vector2Int firstCenter = ClampRoadCellToBuildableArea(startCell / roadCellSizeInGridCells, grid, roadCellSizeInGridCells, townRadius);
            firstCenter = FindNearestRoadCellOutsideBaseExclusions(firstCenter, baseExclusionRoadRects, grid, roadCellSizeInGridCells, townRadius);
            CityLayoutData currentCity = CreateCityLayout(firstCenter, townRadius, null, default, ref rng);
            CommitCityRoadNetwork(currentCity, occupiedRoadCells);
            if (generateBuildings)
            {
                EnsureCityHall(currentCity, roadCellSizeInGridCells, ref rng);
                SpawnCityImportantBuildings(currentCity, roadCellSizeInGridCells, ref rng);
            }
            cities.Add(currentCity);
            if (ShouldYield(1))
                yield return null;

            Vector2Int? previousTravelDirection = null;
            for (int cityIndex = 1; cityIndex < cityCount; cityIndex++)
            {
                if (!TryPlanNextCity(
                        cities,
                        occupiedRoadCells,
                        currentCity,
                        previousTravelDirection,
                        grid,
                        roadCellSizeInGridCells,
                        townRadius,
                        baseExclusionRoadRects,
                        ref rng,
                        out List<Vector2Int> sourceExitRoad,
                        out List<Vector2Int> autobahnPath,
                        out Vector2Int travelDirection,
                        out CityLayoutData nextCity))
                {
                    Debug.LogWarning($"[RuntimeCitySpawnerSystem] Failed to plan city {cityIndex + 1}. Stopping city chain at {cities.Count} city/cities.");
                    break;
                }

                if (!_roadBuildController.CreateRoadStrokeFromRoadCells(sourceExitRoad))
                {
                    Debug.LogWarning($"[RuntimeCitySpawnerSystem] Failed to create source exit road for city {cityIndex + 1}. pathLength={sourceExitRoad.Count}.");
                    break;
                }

                for (int exitIndex = 0; exitIndex < sourceExitRoad.Count; exitIndex++)
                {
                    Vector2Int cell = sourceExitRoad[exitIndex];
                    occupiedRoadCells.Add(cell);
                    currentCity.RoadCells.Add(cell);
                }

                if (ShouldYield(cityIndex * 3 - 1))
                    yield return null;

                var extendedAutobahnPath = new List<Vector2Int>(autobahnPath);
                extendedAutobahnPath.Add(autobahnPath[autobahnPath.Count - 1] + travelDirection);

                if (!_roadBuildController.CreateAutobahnStrokeFromRoadCells(extendedAutobahnPath, true, true))
                {
                    Debug.LogWarning($"[RuntimeCitySpawnerSystem] Failed to create autobahn for city {cityIndex + 1}. pathLength={extendedAutobahnPath.Count}, direction={travelDirection}.");
                    break;
                }

                for (int pathIndex = 0; pathIndex < extendedAutobahnPath.Count; pathIndex++)
                {
                    Vector2Int cell = extendedAutobahnPath[pathIndex];
                    occupiedRoadCells.Add(cell);
                    currentCity.RoadCells.Add(cell);
                }

                Vector2Int endConnectorCell = extendedAutobahnPath[extendedAutobahnPath.Count - 1];
                const int debugStraightRoadLength = 9;
                if (!_roadBuildController.CreateStandaloneStraightRoadChainFromConnector(
                        endConnectorCell,
                        travelDirection,
                        debugStraightRoadLength))
                {
                    yield return null;
                    break;
                }

                if (!_roadBuildController.TryGetStandaloneStraightChainEndRoadCell(travelDirection, out Vector2Int secondCityAnchorCell))
                {
                    yield return null;
                    break;
                }

                Vector2Int secondCityOutwardDirection = -travelDirection;
                CityLayoutData anchoredNextCity = CreateCityLayout(
                    nextCity.CenterRoadCell,
                    townRadius,
                    secondCityAnchorCell,
                    secondCityOutwardDirection,
                    ref rng);

                ReserveStandaloneEntranceCorridor(
                    anchoredNextCity,
                    endConnectorCell,
                    travelDirection,
                    debugStraightRoadLength,
                    roadCellSizeInGridCells);
                CommitCityRoadNetwork(anchoredNextCity, occupiedRoadCells);
                if (generateBuildings)
                {
                    SpawnCityImportantBuildings(anchoredNextCity, roadCellSizeInGridCells, ref rng);
                    SpawnCorridorEntranceBuildings(
                        anchoredNextCity,
                        endConnectorCell,
                        travelDirection,
                        debugStraightRoadLength,
                        roadCellSizeInGridCells,
                        ref rng);
                }
                cities.Add(anchoredNextCity);
                currentCity = anchoredNextCity;
                previousTravelDirection = travelDirection;

                if (ShouldYield(cityIndex * 3))
                    yield return null;
            }

            _roadBuildController?.EndDeferredRoadEcsSync();

            for (int i = 0; i < cities.Count; i++)
            {
                if (generateBuildings)
                {
                    var bulkRng = new GenerationRandomState { Value = rng };
                    IEnumerator bulkRoutine = SpawnCityBulkBuildingsRoutine(cities[i], grid, roadCellSizeInGridCells, bulkRng);
                    while (bulkRoutine.MoveNext())
                        yield return null;
                    rng = bulkRng.Value;
                }

                if (ShouldYield((cityCount * 3) + i + 1))
                    yield return null;
            }

            if (generateBuildings)
                _buildingPlacementController?.EndDeferredRuntimeBuildingSideEffects();

            MainMenuPlayUI.Instance?.NotifyStaticMinimapChanged();
            _spawned = true;
            if (EnableRuntimeCityDiagnostics)
                Debug.Log($"[RuntimeCityState] frame={Time.frameCount} reason=completed cities={cities.Count} ageFrames={Time.frameCount - _generationStartedFrame} steps={_generationMoveNextCount}");
            _generationRoutine = null;
        }
        finally
        {
            if (generateBuildings)
                _buildingPlacementController?.EndDeferredRuntimeBuildingSideEffects();
            _roadBuildController?.EndDeferredRoadEcsSync();
        }
    }

    private CityLayoutData CreateCityLayout(
        Vector2Int centerRoadCell,
        int townRadius,
        Vector2Int? incomingAnchorCell,
        Vector2Int incomingOutwardDirection,
        ref Unity.Mathematics.Random rng)
    {
        var city = new CityLayoutData
        {
            CenterRoadCell = centerRoadCell,
            TownRadius = townRadius,
            RoadStrokes = BuildTownRoadStrokes(centerRoadCell, townRadius, hallPlazaRadiusRoadCells, ref rng)
        };

        if (incomingAnchorCell.HasValue)
        {
            city.HasIncomingAnchor = true;
            city.IncomingAnchorCell = incomingAnchorCell.Value;
            city.IncomingOutwardDirection = incomingOutwardDirection;
            Vector2Int innerConnectionCell = GetCityInnerConnectionCell(centerRoadCell, incomingOutwardDirection);
            AddStroke(city.RoadStrokes, incomingAnchorCell.Value, innerConnectionCell);
        }

        return city;
    }

    private static void PruneIngressCorridorStrokes(
        CityLayoutData city,
        Vector2Int incomingRoadAnchorCell,
        Vector2Int inwardDirection,
        int ingressRoadLength)
    {
        if (city.RoadStrokes == null || city.RoadStrokes.Count <= 1)
            return;

        var protectedCells = new HashSet<Vector2Int>();
        Vector2Int current = incomingRoadAnchorCell;
        protectedCells.Add(current);
        for (int i = 0; i < ingressRoadLength; i++)
        {
            current += inwardDirection;
            protectedCells.Add(current);
        }

        int incomingStrokeIndex = city.RoadStrokes.Count - 1;
        for (int strokeIndex = city.RoadStrokes.Count - 2; strokeIndex >= 0; strokeIndex--)
        {
            List<Vector2Int> stroke = city.RoadStrokes[strokeIndex];
            for (int cellIndex = 0; cellIndex < stroke.Count; cellIndex++)
            {
                if (!protectedCells.Contains(stroke[cellIndex]))
                    continue;

                city.RoadStrokes.RemoveAt(strokeIndex);
                break;
            }
        }
    }

    private void CommitCityRoadNetwork(CityLayoutData city, HashSet<Vector2Int> occupiedRoadCells)
    {
        PopulateCityRoadCells(city);
        for (int strokeIndex = 0; strokeIndex < city.RoadStrokes.Count; strokeIndex++)
        {
            List<Vector2Int> stroke = city.RoadStrokes[strokeIndex];
            _roadBuildController.CreateRoadStrokeFromRoadCells(stroke);
            for (int cellIndex = 0; cellIndex < stroke.Count; cellIndex++)
            {
                Vector2Int cell = stroke[cellIndex];
                city.RoadCells.Add(cell);
                occupiedRoadCells.Add(cell);
            }
        }
    }

    private static void PopulateCityRoadCells(CityLayoutData city)
    {
        city.RoadCells.Clear();
        for (int strokeIndex = 0; strokeIndex < city.RoadStrokes.Count; strokeIndex++)
        {
            List<Vector2Int> stroke = city.RoadStrokes[strokeIndex];
            for (int cellIndex = 0; cellIndex < stroke.Count; cellIndex++)
                city.RoadCells.Add(stroke[cellIndex]);
        }
    }

    private void SpawnCityImportantBuildings(CityLayoutData city, int roadCellSizeInGridCells, ref Unity.Mathematics.Random rng)
    {
        EnsureCityHall(city, roadCellSizeInGridCells, ref rng);
        TrySpawnClockTower(city.CenterRoadCell, roadCellSizeInGridCells, city.RoadCells, city.ReservedFootprints);
        TrySpawnFountain(city.CenterRoadCell, roadCellSizeInGridCells, city.RoadCells, ref rng, city.ReservedFootprints);
        TrySpawnMonument(city.CenterRoadCell, roadCellSizeInGridCells, city.RoadCells, ref rng, city.ReservedFootprints);
        TrySpawnPillar(city.CenterRoadCell, roadCellSizeInGridCells, city.RoadCells, ref rng, city.ReservedFootprints);
    }

    private void EnsureCityHall(CityLayoutData city, int roadCellSizeInGridCells, ref Unity.Mathematics.Random rng)
    {
        if (city.HallPlaced)
            return;

        city.HallPlaced = TrySpawnHall(city.CenterRoadCell, roadCellSizeInGridCells, ref rng, city.ReservedFootprints);
        if (!city.HallPlaced)
            Debug.LogWarning($"[RuntimeCitySpawnerSystem] Hall could not be placed for city at {city.CenterRoadCell}.");
    }

    private sealed class GenerationRandomState
    {
        public Unity.Mathematics.Random Value;
    }

    private IEnumerator SpawnCityBulkBuildingsRoutine(CityLayoutData city, GridConfig grid, int roadCellSizeInGridCells, GenerationRandomState rng)
    {
        Vector2Int centerRoadCell = city.CenterRoadCell;
        int townRadius = city.TownRadius;
        HashSet<Vector2Int> roadCells = city.RoadCells;
        List<ReservedFootprint> reservedFootprints = city.ReservedFootprints;

        List<PlotCandidate> centralPlots = CollectRoadsidePlots(roadCells, centerRoadCell, townRadius, hallPlazaRadiusRoadCells + 1, hallPlazaRadiusRoadCells + 3);
        List<PlotCandidate> outerPlots = CollectRoadsidePlots(roadCells, centerRoadCell, townRadius, hallPlazaRadiusRoadCells + 4, townRadius + 1);
        List<PlotCandidate> entryPlots = city.HasIncomingAnchor
            ? CollectEntryRoadsidePlots(city, townRadius)
            : new List<PlotCandidate>();
        Shuffle(centralPlots, ref rng.Value);
        Shuffle(outerPlots, ref rng.Value);
        Shuffle(entryPlots, ref rng.Value);

        EnsureCityVisualRoot();

        var usedPlotCells = new List<Vector2Int>();
        var shopAndHouseFootprints = new List<RectInt>();
        var houseFootprints = new List<RectInt>();
        PlaceFromPlots(shopPrefabs, entryPlots, Mathf.Min(2, Mathf.Max(0, shopCount)), 0, roadCellSizeInGridCells, "Entry Shop", "Roadside shop near the city entrance.", ref rng.Value, usedPlotCells, reservedFootprints, shopAndHouseFootprints);
        yield return null;
        PlaceFromPlots(housePrefabs, entryPlots, Mathf.Min(4, Mathf.Max(0, houseCount)), 0, roadCellSizeInGridCells, "Entry House", "House near the city entrance road.", ref rng.Value, usedPlotCells, reservedFootprints, shopAndHouseFootprints, houseFootprints);
        yield return null;

        int centralShopTarget = Mathf.Min(shopCount, Mathf.Max(0, Mathf.RoundToInt(shopCount * 0.65f)));
        PlaceFromPlots(shopPrefabs, centralPlots, centralShopTarget, 1, roadCellSizeInGridCells, "Market", "Old town market.", ref rng.Value, usedPlotCells, reservedFootprints, shopAndHouseFootprints);
        yield return null;
        PlaceFromPlots(gasStationPrefabs, outerPlots, gasStationCount, gasStationMinSpacingRoadCells, roadCellSizeInGridCells, "Gas Station", "Roadside fuel stop.", ref rng.Value, usedPlotCells, reservedFootprints);
        yield return null;
        PlaceFromPlots(shopPrefabs, outerPlots, Mathf.Max(0, shopCount - centralShopTarget), 1, roadCellSizeInGridCells, "Shop", "Old town shop.", ref rng.Value, usedPlotCells, reservedFootprints, shopAndHouseFootprints);
        yield return null;

        int ruralHouseTarget = Mathf.RoundToInt(Mathf.Max(0, houseCount) * Mathf.Clamp01(ruralHouseRatio));
        int roadsideHouseTarget = Mathf.Max(0, houseCount - ruralHouseTarget);
        PlaceFromPlots(housePrefabs, outerPlots, roadsideHouseTarget, 1, roadCellSizeInGridCells, "House", "Old town house.", ref rng.Value, usedPlotCells, reservedFootprints, shopAndHouseFootprints, houseFootprints);
        yield return null;
        PlaceRuralHouses(housePrefabs, ruralHouseTarget, centerRoadCell, townRadius, roadCellSizeInGridCells, roadCells, ref rng.Value, usedPlotCells, reservedFootprints, shopAndHouseFootprints, houseFootprints);
        yield return null;
        PlaceHouseYardWalls(houseFootprints, centerRoadCell, roadCellSizeInGridCells, roadCells, grid, ref rng.Value, reservedFootprints);
        yield return null;

        int ruralOtherTarget = Mathf.RoundToInt(Mathf.Max(0, otherBuildingCount) * Mathf.Clamp01(ruralHouseRatio));
        int roadsideOtherTarget = Mathf.Max(0, otherBuildingCount - ruralOtherTarget);
        PlaceFromPlots(otherBuildingPrefabs, outerPlots, roadsideOtherTarget, 1, roadCellSizeInGridCells, "Village Building", "Old town side building.", ref rng.Value, usedPlotCells, reservedFootprints);
        yield return null;
        PlaceRuralHouses(otherBuildingPrefabs, ruralOtherTarget, centerRoadCell, townRadius, roadCellSizeInGridCells, roadCells, ref rng.Value, usedPlotCells, reservedFootprints);
        yield return null;
        PlaceCityDecorationBuildings(cityDecorationPrefabs, cityDecorationBuildingCount, centerRoadCell, townRadius, roadCellSizeInGridCells, roadCells, ref rng.Value, usedPlotCells, reservedFootprints, shopAndHouseFootprints);
        yield return null;
    }

    private void SpawnCorridorEntranceBuildings(
        CityLayoutData city,
        Vector2Int connectorCell,
        Vector2Int direction,
        int corridorLength,
        int roadCellSizeInGridCells,
        ref Unity.Mathematics.Random rng)
    {
        if (corridorLength <= 0)
            return;

        Vector2Int left = new(-direction.y, direction.x);
        Vector2Int right = new(direction.y, -direction.x);
        var corridorPlots = new List<PlotCandidate>();
        var seen = new HashSet<Vector2Int>();

        for (int step = 1; step <= corridorLength; step++)
        {
            Vector2Int roadCell = connectorCell + direction * step;
            Vector2Int leftPlot = roadCell + left;
            Vector2Int rightPlot = roadCell + right;

            if (seen.Add(leftPlot))
            {
                corridorPlots.Add(new PlotCandidate
                {
                    PlotCell = leftPlot,
                    DistanceFromCenter = corridorLength - step
                });
            }

            if (seen.Add(rightPlot))
            {
                corridorPlots.Add(new PlotCandidate
                {
                    PlotCell = rightPlot,
                    DistanceFromCenter = corridorLength - step
                });
            }
        }

        if (corridorPlots.Count == 0)
            return;

        Shuffle(corridorPlots, ref rng);

        var usedPlotCells = new List<Vector2Int>();
        var placementAnchors = new List<RectInt>();
        PlaceFromPlots(shopPrefabs, corridorPlots, Mathf.Min(2, Mathf.Max(0, shopCount)), 0, roadCellSizeInGridCells, "Corridor Shop", "Shop near the city entrance road.", ref rng, usedPlotCells, city.ReservedFootprints, placementAnchors);
        PlaceFromPlots(housePrefabs, corridorPlots, Mathf.Min(6, Mathf.Max(0, houseCount)), 0, roadCellSizeInGridCells, "Corridor House", "House near the city entrance road.", ref rng, usedPlotCells, city.ReservedFootprints, placementAnchors);
    }

    private static List<PlotCandidate> CollectEntryRoadsidePlots(CityLayoutData city, int townRadius)
    {
        var results = new List<PlotCandidate>();
        if (!city.HasIncomingAnchor)
            return results;

        List<PlotCandidate> candidates = CollectRoadsidePlots(city.RoadCells, city.CenterRoadCell, townRadius, 0, townRadius + 1);
        Vector2Int inwardDirection = -city.IncomingOutwardDirection;
        Vector2Int inwardStart = city.IncomingAnchorCell + inwardDirection;
        for (int i = 0; i < candidates.Count; i++)
        {
            PlotCandidate candidate = candidates[i];
            Vector2Int relativeToEntry = candidate.PlotCell - inwardStart;
            int forwardDistance = relativeToEntry.x * inwardDirection.x + relativeToEntry.y * inwardDirection.y;
            if (forwardDistance < 0 || forwardDistance > 6)
                continue;

            int lateralDistance = Mathf.Abs(relativeToEntry.x * city.IncomingOutwardDirection.y - relativeToEntry.y * city.IncomingOutwardDirection.x);
            if (lateralDistance > 3)
                continue;

            results.Add(candidate);
        }

        return results;
    }

    private bool TryPlanNextCity(
        List<CityLayoutData> existingCities,
        HashSet<Vector2Int> occupiedRoadCells,
        CityLayoutData currentCity,
        Vector2Int? previousTravelDirection,
        GridConfig grid,
        int roadCellSizeInGridCells,
        int townRadius,
        List<RectInt> baseExclusionRoadRects,
        ref Unity.Mathematics.Random rng,
        out List<Vector2Int> sourceExitRoad,
        out List<Vector2Int> autobahnPath,
        out Vector2Int travelDirection,
        out CityLayoutData nextCity)
    {
        var directions = new List<Vector2Int>(CardinalDirections);
        Shuffle(directions, ref rng);

        if (previousTravelDirection.HasValue)
        {
            Vector2Int reverse = -previousTravelDirection.Value;
            directions.Sort((a, b) =>
            {
                bool aIsReverse = a == reverse;
                bool bIsReverse = b == reverse;
                if (aIsReverse == bIsReverse)
                    return 0;
                return aIsReverse ? 1 : -1;
            });
        }

        int cityConnectionOffset = GetCityConnectionOffset(townRadius);
        int autobahnLength = Mathf.Max(autobahnMinLengthRoadCells, cityMinSpacingRoadCells);
        GetRoadGridBounds(grid, roadCellSizeInGridCells, townRadius, out int minRoadX, out int maxRoadX, out int minRoadY, out int maxRoadY);

        for (int dirIndex = 0; dirIndex < directions.Count; dirIndex++)
        {
            Vector2Int direction = directions[dirIndex];
            Vector2Int sourceInnerConnection = GetCityInnerConnectionCell(currentCity.CenterRoadCell, direction);
            Vector2Int targetCenter = currentCity.CenterRoadCell + direction * (autobahnLength + cityConnectionOffset * 2);

            if (!IsRoadCellWithinBounds(sourceInnerConnection, minRoadX, maxRoadX, minRoadY, maxRoadY) ||
                !IsRoadCellWithinBounds(targetCenter, minRoadX, maxRoadX, minRoadY, maxRoadY))
            {
                continue;
            }

            if (!IsCityCenterFarEnough(targetCenter, existingCities, townRadius, baseExclusionRoadRects))
            {
                continue;
            }

            var trialRng = rng;
            CityLayoutData plannedCity = CreateCityLayout(targetCenter, townRadius, null, default, ref trialRng);
            PopulateCityRoadCells(plannedCity);

            if (!TryGetCityConnectionCell(currentCity, direction, out Vector2Int sourceConnectionCell))
            {
                continue;
            }

            if (!TryGetCityConnectionCell(plannedCity, -direction, out Vector2Int targetConnectionCell))
            {
                continue;
            }

            if (!IsRoadCellWithinBounds(sourceConnectionCell, minRoadX, maxRoadX, minRoadY, maxRoadY) ||
                !IsRoadCellWithinBounds(targetConnectionCell, minRoadX, maxRoadX, minRoadY, maxRoadY))
            {
                continue;
            }

            List<Vector2Int> candidateExitRoad = BuildStraightRoadPath(sourceInnerConnection, sourceConnectionCell);
            if (candidateExitRoad.Count < 2)
            {
                continue;
            }
            if (!IsCityExitPathValid(candidateExitRoad, occupiedRoadCells, currentCity))
            {
                continue;
            }

            List<Vector2Int> candidatePath = BuildStraightRoadPath(sourceConnectionCell, targetConnectionCell);
            if (!IsAutobahnPathValid(candidatePath, occupiedRoadCells, existingCities, currentCity, plannedCity))
            {
                continue;
            }

            sourceExitRoad = candidateExitRoad;
            autobahnPath = candidatePath;
            travelDirection = direction;
            nextCity = plannedCity;
            rng = trialRng;
            return true;
        }

        sourceExitRoad = null;
        autobahnPath = null;
        travelDirection = default;
        nextCity = null;
        return false;
    }

    private bool IsCityExitPathValid(List<Vector2Int> path, HashSet<Vector2Int> occupiedRoadCells, CityLayoutData sourceCity)
    {
        if (path == null || path.Count < 2)
            return false;

        for (int i = 0; i < path.Count; i++)
        {
            Vector2Int cell = path[i];
            if (sourceCity.RoadCells.Contains(cell))
                continue;
            if (occupiedRoadCells.Contains(cell))
                return false;
        }

        return true;
    }

    private bool IsAutobahnPathValid(
        List<Vector2Int> path,
        HashSet<Vector2Int> occupiedRoadCells,
        List<CityLayoutData> existingCities,
        CityLayoutData sourceCity,
        CityLayoutData targetCity)
    {
        if (path == null || path.Count < 3)
            return false;

        for (int i = 1; i < path.Count; i++)
        {
            if (occupiedRoadCells.Contains(path[i]))
                return false;
        }

        for (int i = 1; i < path.Count - 1; i++)
        {
            Vector2Int cell = path[i];
            for (int cityIndex = 0; cityIndex < existingCities.Count; cityIndex++)
            {
                CityLayoutData city = existingCities[cityIndex];
                if (ReferenceEquals(city, sourceCity))
                    continue;

                int distance = Mathf.Abs(cell.x - city.CenterRoadCell.x) + Mathf.Abs(cell.y - city.CenterRoadCell.y);
                if (distance <= city.TownRadius + hallPlazaRadiusRoadCells + 2)
                    return false;
            }
        }

        for (int i = 0; i < path.Count - 1; i++)
        {
            if (targetCity.RoadCells.Contains(path[i]))
                return false;
        }

        return true;
    }

    private bool IsCityCenterFarEnough(Vector2Int candidateCenter, List<CityLayoutData> existingCities, int townRadius, List<RectInt> baseExclusionRoadRects)
    {
        if (IsRoadCellInsideAnyBaseExclusion(candidateCenter, baseExclusionRoadRects))
            return false;

        int minDistance = Mathf.Max(cityMinSpacingRoadCells, townRadius * 2 + hallPlazaRadiusRoadCells + 4);
        for (int i = 0; i < existingCities.Count; i++)
        {
            CityLayoutData city = existingCities[i];
            int distance = Mathf.Abs(candidateCenter.x - city.CenterRoadCell.x) + Mathf.Abs(candidateCenter.y - city.CenterRoadCell.y);
            if (distance < minDistance)
                return false;
        }

        return true;
    }

    private static List<Vector2Int> BuildStraightRoadPath(Vector2Int start, Vector2Int end)
    {
        if (start.x != end.x && start.y != end.y)
            return new List<Vector2Int>();

        var path = new List<Vector2Int> { start };
        AppendStraightSegment(path, start, end);
        return path;
    }

    private static bool TryGetCityConnectionCell(CityLayoutData city, Vector2Int direction, out Vector2Int connectionCell)
    {
        connectionCell = default;
        bool found = false;
        int bestDistance = int.MinValue;

        foreach (Vector2Int roadCell in city.RoadCells)
        {
            Vector2Int delta = roadCell - city.CenterRoadCell;
            int distance;

            if (direction == East)
            {
                if (delta.y != 0 || delta.x <= 0)
                    continue;
                distance = delta.x;
            }
            else if (direction == West)
            {
                if (delta.y != 0 || delta.x >= 0)
                    continue;
                distance = -delta.x;
            }
            else if (direction == North)
            {
                if (delta.x != 0 || delta.y <= 0)
                    continue;
                distance = delta.y;
            }
            else if (direction == South)
            {
                if (delta.x != 0 || delta.y >= 0)
                    continue;
                distance = -delta.y;
            }
            else
            {
                continue;
            }

            if (distance <= bestDistance)
                continue;

            bestDistance = distance;
            connectionCell = roadCell;
            found = true;
        }

        return found;
    }

    private Vector2Int GetCityInnerConnectionCell(Vector2Int centerRoadCell, Vector2Int outwardDirection)
    {
        int ringRadius = hallPlazaRadiusRoadCells + 1;
        return centerRoadCell + outwardDirection * ringRadius;
    }

    private int GetCityConnectionOffset(int townRadius)
    {
        return Mathf.Max(townRadius + hallPlazaRadiusRoadCells + 3, hallPlazaRadiusRoadCells + 5);
    }

    private Vector2Int ClampRoadCellToBuildableArea(Vector2Int roadCell, GridConfig grid, int roadCellSizeInGridCells, int townRadius)
    {
        GetRoadGridBounds(grid, roadCellSizeInGridCells, townRadius, out int minRoadX, out int maxRoadX, out int minRoadY, out int maxRoadY);
        return new Vector2Int(
            Mathf.Clamp(roadCell.x, minRoadX, maxRoadX),
            Mathf.Clamp(roadCell.y, minRoadY, maxRoadY));
    }

    private Vector2Int FindNearestRoadCellOutsideBaseExclusions(
        Vector2Int roadCell,
        List<RectInt> baseExclusionRoadRects,
        GridConfig grid,
        int roadCellSizeInGridCells,
        int townRadius)
    {
        if (!IsRoadCellInsideAnyBaseExclusion(roadCell, baseExclusionRoadRects))
            return roadCell;

        GetRoadGridBounds(grid, roadCellSizeInGridCells, townRadius, out int minRoadX, out int maxRoadX, out int minRoadY, out int maxRoadY);
        int maxRadius = Mathf.Max(maxRoadX - minRoadX, maxRoadY - minRoadY);
        for (int radius = 1; radius <= maxRadius; radius++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    if (Mathf.Abs(x) != radius && Mathf.Abs(y) != radius)
                        continue;

                    Vector2Int candidate = roadCell + new Vector2Int(x, y);
                    if (!IsRoadCellWithinBounds(candidate, minRoadX, maxRoadX, minRoadY, maxRoadY))
                        continue;
                    if (IsRoadCellInsideAnyBaseExclusion(candidate, baseExclusionRoadRects))
                        continue;

                    return candidate;
                }
            }
        }

        return roadCell;
    }

    private static bool IsRoadCellInsideAnyBaseExclusion(Vector2Int roadCell, List<RectInt> baseExclusionRoadRects)
    {
        if (baseExclusionRoadRects == null)
            return false;

        for (int i = 0; i < baseExclusionRoadRects.Count; i++)
        {
            if (baseExclusionRoadRects[i].Contains(roadCell))
                return true;
        }

        return false;
    }

    private void GetRoadGridBounds(GridConfig grid, int roadCellSizeInGridCells, int townRadius, out int minRoadX, out int maxRoadX, out int minRoadY, out int maxRoadY)
    {
        int roadGridWidth = Mathf.Max(1, Mathf.CeilToInt(grid.Width / (float)Mathf.Max(1, roadCellSizeInGridCells)));
        int roadGridHeight = Mathf.Max(1, Mathf.CeilToInt(grid.Height / (float)Mathf.Max(1, roadCellSizeInGridCells)));
        int margin = Mathf.Max(8, townRadius + hallPlazaRadiusRoadCells + 3);
        minRoadX = margin;
        maxRoadX = Mathf.Max(margin, roadGridWidth - 1 - margin);
        minRoadY = margin;
        maxRoadY = Mathf.Max(margin, roadGridHeight - 1 - margin);
    }

    private static bool IsRoadCellWithinBounds(Vector2Int cell, int minRoadX, int maxRoadX, int minRoadY, int maxRoadY)
    {
        return cell.x >= minRoadX && cell.x <= maxRoadX && cell.y >= minRoadY && cell.y <= maxRoadY;
    }

    private int CalculateTownRadius()
    {
        int totalBuildings = 1 + Mathf.Max(0, gasStationCount) + Mathf.Max(0, shopCount) + Mathf.Max(0, houseCount) + Mathf.Max(0, otherBuildingCount) + Mathf.Max(0, cityDecorationBuildingCount);
        return Mathf.Max(hallPlazaRadiusRoadCells + 3, Mathf.CeilToInt(Mathf.Sqrt(totalBuildings)) + extraTownRadiusRoadCells);
    }

    private CityChainAxis ChooseCityChainAxis(GridConfig grid, int roadCellSizeInGridCells, int townRadius)
    {
        int roadGridWidth = Mathf.Max(1, Mathf.CeilToInt(grid.Width / (float)Mathf.Max(1, roadCellSizeInGridCells)));
        int roadGridHeight = Mathf.Max(1, Mathf.CeilToInt(grid.Height / (float)Mathf.Max(1, roadCellSizeInGridCells)));
        int margin = Mathf.Max(8, hallPlazaRadiusRoadCells + 6);
        int usableWidth = Mathf.Max(1, roadGridWidth - margin * 2);
        int usableHeight = Mathf.Max(1, roadGridHeight - margin * 2);
        return usableWidth >= usableHeight ? CityChainAxis.Horizontal : CityChainAxis.Vertical;
    }

    private List<Vector2Int> BuildCityCenters(GridConfig grid, int roadCellSizeInGridCells, int townRadius, CityChainAxis chainAxis, ref Unity.Mathematics.Random rng)
    {
        int requestedCount = Mathf.Max(0, cityCount);
        int roadGridWidth = Mathf.Max(1, Mathf.CeilToInt(grid.Width / (float)Mathf.Max(1, roadCellSizeInGridCells)));
        int roadGridHeight = Mathf.Max(1, Mathf.CeilToInt(grid.Height / (float)Mathf.Max(1, roadCellSizeInGridCells)));
        int margin = Mathf.Max(8, hallPlazaRadiusRoadCells + 6);
        int minX = Mathf.Clamp(margin, 0, Mathf.Max(0, roadGridWidth - 1));
        int minY = Mathf.Clamp(margin, 0, Mathf.Max(0, roadGridHeight - 1));
        int maxX = Mathf.Clamp(roadGridWidth - 1 - margin, minX, Mathf.Max(minX, roadGridWidth - 1));
        int maxY = Mathf.Clamp(roadGridHeight - 1 - margin, minY, Mathf.Max(minY, roadGridHeight - 1));

        Vector2Int preferredCenter = new(
            Mathf.Clamp(startCell.x / roadCellSizeInGridCells, minX, maxX),
            Mathf.Clamp(startCell.y / roadCellSizeInGridCells, minY, maxY));

        if (requestedCount == 1)
            return new List<Vector2Int> { preferredCenter };

        int minSpacing = Mathf.Max(cityMinSpacingRoadCells, townRadius + 2);
        if (chainAxis == CityChainAxis.Horizontal)
        {
            return BuildLinearCityCenters(preferredCenter, requestedCount, minX, maxX, minY, maxY, minSpacing, true, ref rng);
        }

        return BuildLinearCityCenters(preferredCenter, requestedCount, minY, maxY, minX, maxX, minSpacing, false, ref rng);
    }

    private List<Vector2Int> BuildLinearCityCenters(
        Vector2Int preferredCenter,
        int requestedCount,
        int minPrimary,
        int maxPrimary,
        int minSecondary,
        int maxSecondary,
        int minSpacing,
        bool horizontal,
        ref Unity.Mathematics.Random rng)
    {
        int availableSpan = Mathf.Max(0, maxPrimary - minPrimary);
        int maxCities = Mathf.Max(1, availableSpan / Mathf.Max(1, minSpacing) + 1);
        int actualCount = Mathf.Min(requestedCount, maxCities);
        if (actualCount <= 1)
            return new List<Vector2Int> { preferredCenter };

        int step = actualCount <= 1 ? 0 : Mathf.Max(minSpacing, Mathf.FloorToInt(availableSpan / (float)(actualCount - 1)));
        int totalSpan = step * Mathf.Max(0, actualCount - 1);
        int slack = Mathf.Max(0, availableSpan - totalSpan);
        int startPrimary = minPrimary + (slack > 0 ? rng.NextInt(0, slack + 1) : 0);
        int secondary = Mathf.Clamp(preferredCenter.y, minSecondary, maxSecondary);
        if (!horizontal)
            secondary = Mathf.Clamp(preferredCenter.x, minSecondary, maxSecondary);
        if (maxSecondary > minSecondary)
            secondary = Mathf.Clamp(secondary + rng.NextInt(-2, 3), minSecondary, maxSecondary);

        var centers = new List<Vector2Int>(actualCount);
        for (int i = 0; i < actualCount; i++)
        {
            int primary = Mathf.Clamp(startPrimary + step * i, minPrimary, maxPrimary);
            centers.Add(horizontal
                ? new Vector2Int(primary, secondary)
                : new Vector2Int(secondary, primary));
        }

        return centers;
    }

    private void ConnectCitiesWithAutobahn(CityLayoutData fromCity, CityLayoutData toCity, CityChainAxis chainAxis)
    {
        List<Vector2Int> autobahnPath = BuildCityToCityAutobahnPath(fromCity, toCity, chainAxis);
        if (autobahnPath.Count < 3)
            return;
        if (!_roadBuildController.CreateAutobahnStrokeFromRoadCells(autobahnPath, true, true))
            return;

        for (int cellIndex = 0; cellIndex < autobahnPath.Count; cellIndex++)
        {
            Vector2Int cell = autobahnPath[cellIndex];
            fromCity.RoadCells.Add(cell);
            toCity.RoadCells.Add(cell);
        }
    }

    private List<Vector2Int> BuildCityToCityAutobahnPath(CityLayoutData fromCity, CityLayoutData toCity, CityChainAxis chainAxis)
    {
        Vector2Int forwardDirection = chainAxis == CityChainAxis.Horizontal
            ? (toCity.CenterRoadCell.x >= fromCity.CenterRoadCell.x ? East : West)
            : (toCity.CenterRoadCell.y >= fromCity.CenterRoadCell.y ? North : South);
        Vector2Int backwardDirection = new(-forwardDirection.x, -forwardDirection.y);

        if (!TrySelectDirectionalAutobahnAnchor(fromCity.RoadCells, fromCity.CenterRoadCell, forwardDirection, chainAxis, out AutobahnAnchorCandidate fromAnchor))
            return new List<Vector2Int>();
        if (!TrySelectDirectionalAutobahnAnchor(toCity.RoadCells, toCity.CenterRoadCell, backwardDirection, chainAxis, out AutobahnAnchorCandidate toAnchor))
            return new List<Vector2Int>();

        if (chainAxis == CityChainAxis.Horizontal && fromAnchor.AnchorCell.y != toAnchor.AnchorCell.y)
            return new List<Vector2Int>();
        if (chainAxis == CityChainAxis.Vertical && fromAnchor.AnchorCell.x != toAnchor.AnchorCell.x)
            return new List<Vector2Int>();

        var path = new List<Vector2Int> { fromAnchor.AnchorCell };
        AppendStraightSegment(path, path[path.Count - 1], toAnchor.AnchorCell);
        return path;
    }

    private static bool TrySelectDirectionalAutobahnAnchor(
        HashSet<Vector2Int> roadCells,
        Vector2Int cityCenterRoadCell,
        Vector2Int desiredDirection,
        CityChainAxis chainAxis,
        out AutobahnAnchorCandidate selectedAnchor)
    {
        List<AutobahnAnchorCandidate> candidates = CollectAutobahnAnchorCandidates(roadCells, cityCenterRoadCell);
        int bestScore = int.MinValue;
        bool found = false;
        selectedAnchor = default;

        for (int i = 0; i < candidates.Count; i++)
        {
            AutobahnAnchorCandidate candidate = candidates[i];
            if (candidate.OutwardDirection != desiredDirection)
                continue;

            int perpendicularOffset = chainAxis == CityChainAxis.Horizontal
                ? Mathf.Abs(candidate.AnchorCell.y - cityCenterRoadCell.y)
                : Mathf.Abs(candidate.AnchorCell.x - cityCenterRoadCell.x);
            int score = candidate.Score * 4 - perpendicularOffset * 1000;
            if (score <= bestScore)
                continue;

            bestScore = score;
            selectedAnchor = candidate;
            found = true;
        }

        return found;
    }

    private void SpawnCityBuildings(CityLayoutData city, GridConfig grid, int roadCellSizeInGridCells, ref Unity.Mathematics.Random rng)
    {
        Vector2Int centerRoadCell = city.CenterRoadCell;
        int townRadius = city.TownRadius;
        HashSet<Vector2Int> roadCells = city.RoadCells;

        var reservedFootprints = new List<ReservedFootprint>();
        TrySpawnHall(centerRoadCell, roadCellSizeInGridCells, ref rng, reservedFootprints);

        TrySpawnClockTower(centerRoadCell, roadCellSizeInGridCells, roadCells, reservedFootprints);
        TrySpawnFountain(centerRoadCell, roadCellSizeInGridCells, roadCells, ref rng, reservedFootprints);
        TrySpawnMonument(centerRoadCell, roadCellSizeInGridCells, roadCells, ref rng, reservedFootprints);
        TrySpawnPillar(centerRoadCell, roadCellSizeInGridCells, roadCells, ref rng, reservedFootprints);

        List<PlotCandidate> centralPlots = CollectRoadsidePlots(roadCells, centerRoadCell, townRadius, hallPlazaRadiusRoadCells + 1, hallPlazaRadiusRoadCells + 3);
        List<PlotCandidate> outerPlots = CollectRoadsidePlots(roadCells, centerRoadCell, townRadius, hallPlazaRadiusRoadCells + 4, townRadius + 1);
        Shuffle(centralPlots, ref rng);
        Shuffle(outerPlots, ref rng);

        EnsureCityVisualRoot();

        var usedPlotCells = new List<Vector2Int>();
        var shopAndHouseFootprints = new List<RectInt>();
        var houseFootprints = new List<RectInt>();
        int centralShopTarget = Mathf.Min(shopCount, Mathf.Max(0, Mathf.RoundToInt(shopCount * 0.65f)));
        PlaceFromPlots(shopPrefabs, centralPlots, centralShopTarget, 1, roadCellSizeInGridCells, "Market", "Old town market.", ref rng, usedPlotCells, reservedFootprints, shopAndHouseFootprints);
        PlaceFromPlots(gasStationPrefabs, outerPlots, gasStationCount, gasStationMinSpacingRoadCells, roadCellSizeInGridCells, "Gas Station", "Roadside fuel stop.", ref rng, usedPlotCells, reservedFootprints);
        PlaceFromPlots(shopPrefabs, outerPlots, Mathf.Max(0, shopCount - centralShopTarget), 1, roadCellSizeInGridCells, "Shop", "Old town shop.", ref rng, usedPlotCells, reservedFootprints, shopAndHouseFootprints);

        int ruralHouseTarget = Mathf.RoundToInt(Mathf.Max(0, houseCount) * Mathf.Clamp01(ruralHouseRatio));
        int roadsideHouseTarget = Mathf.Max(0, houseCount - ruralHouseTarget);
        PlaceFromPlots(housePrefabs, outerPlots, roadsideHouseTarget, 1, roadCellSizeInGridCells, "House", "Old town house.", ref rng, usedPlotCells, reservedFootprints, shopAndHouseFootprints, houseFootprints);
        PlaceRuralHouses(housePrefabs, ruralHouseTarget, centerRoadCell, townRadius, roadCellSizeInGridCells, roadCells, ref rng, usedPlotCells, reservedFootprints, shopAndHouseFootprints, houseFootprints);
        PlaceHouseYardWalls(houseFootprints, centerRoadCell, roadCellSizeInGridCells, roadCells, grid, ref rng, reservedFootprints);

        int ruralOtherTarget = Mathf.RoundToInt(Mathf.Max(0, otherBuildingCount) * Mathf.Clamp01(ruralHouseRatio));
        int roadsideOtherTarget = Mathf.Max(0, otherBuildingCount - ruralOtherTarget);
        PlaceFromPlots(otherBuildingPrefabs, outerPlots, roadsideOtherTarget, 1, roadCellSizeInGridCells, "Village Building", "Old town side building.", ref rng, usedPlotCells, reservedFootprints);
        PlaceRuralHouses(otherBuildingPrefabs, ruralOtherTarget, centerRoadCell, townRadius, roadCellSizeInGridCells, roadCells, ref rng, usedPlotCells, reservedFootprints);
        PlaceCityDecorationBuildings(cityDecorationPrefabs, cityDecorationBuildingCount, centerRoadCell, townRadius, roadCellSizeInGridCells, roadCells, ref rng, usedPlotCells, reservedFootprints, shopAndHouseFootprints);
    }

    private bool TrySpawnHall(Vector2Int centerRoadCell, int roadCellSizeInGridCells, ref Unity.Mathematics.Random rng, List<ReservedFootprint> reservedFootprints)
    {
        if (hallPrefabs == null || hallPrefabs.Count == 0)
            return false;

        var hallCandidates = new List<GameObject>(hallPrefabs);
        Shuffle(hallCandidates, ref rng);

        Vector2Int[] offsets =
        {
            Vector2Int.zero,
            new Vector2Int(1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(0, -1),
            new Vector2Int(1, 1),
            new Vector2Int(-1, 1),
            new Vector2Int(1, -1),
            new Vector2Int(-1, -1),
            new Vector2Int(2, 0),
            new Vector2Int(-2, 0),
            new Vector2Int(0, 2),
            new Vector2Int(0, -2),
            new Vector2Int(2, 1),
            new Vector2Int(-2, 1),
            new Vector2Int(2, -1),
            new Vector2Int(-2, -1),
            new Vector2Int(1, 2),
            new Vector2Int(-1, 2),
            new Vector2Int(1, -2),
            new Vector2Int(-1, -2)
        };

        for (int prefabIndex = 0; prefabIndex < hallCandidates.Count; prefabIndex++)
        {
            GameObject hallPrefab = hallCandidates[prefabIndex];
            if (hallPrefab == null)
                continue;

            Vector2Int footprint = GetCachedFootprintCells(hallPrefab);
            for (int offsetIndex = 0; offsetIndex < offsets.Length; offsetIndex++)
            {
                Vector2Int hallOrigin = GetCenteredOriginForPlot(centerRoadCell + offsets[offsetIndex], footprint, roadCellSizeInGridCells);
                if (WouldBeTooCloseToReserved(hallOrigin, footprint, reservedFootprints, landmarkClearanceCells))
                    continue;

                if (!_buildingPlacementController.TrySpawnRuntimeBuilding(
                        hallPrefab,
                        hallOrigin,
                        out int buildingId,
                        out Vector2Int actualHallOrigin,
                        out Vector2Int actualHallFootprint,
                        hallPrefab.name,
                        "Old town civic center.",
                        footprint,
                        defaultBuildingMaxHealth,
                        true))
                {
                    continue;
                }

                if (WouldBeTooCloseToReserved(actualHallOrigin, actualHallFootprint, reservedFootprints, landmarkClearanceCells))
                {
                    _buildingPlacementController.DeleteBuildingById(buildingId);
                    continue;
                }

                ReserveFootprint(reservedFootprints, actualHallOrigin, actualHallFootprint, landmarkClearanceCells);
                return true;
            }
        }

        return false;
    }

    private void TrySpawnClockTower(Vector2Int centerRoadCell, int roadCellSizeInGridCells, HashSet<Vector2Int> roadCells, List<ReservedFootprint> reservedFootprints)
    {
        if (clockTowerPrefab == null)
            return;

            Vector2Int footprint = GetCachedFootprintCells(clockTowerPrefab);
        Vector2Int[] offsets =
        {
            new(3, 0),
            new(-3, 0),
            new(0, 3),
            new(0, -3),
            new(3, 2),
            new(-3, 2),
            new(3, -2),
            new(-3, -2),
            new(4, 1),
            new(-4, 1),
            new(1, 4),
            new(-1, 4)
        };

        for (int i = 0; i < offsets.Length; i++)
        {
            if (IsTooCloseToHall(centerRoadCell, offsets[i]))
                continue;

            Vector2Int preferredOrigin = GetCenteredOriginForPlot(centerRoadCell + offsets[i], footprint, roadCellSizeInGridCells);
            if (DoesRectOverlapRoadCells(new RectInt(preferredOrigin, footprint), roadCellSizeInGridCells, roadCells))
                continue;
            if (WouldBeTooCloseToReserved(preferredOrigin, footprint, reservedFootprints, landmarkClearanceCells))
                continue;

            if (_buildingPlacementController.TrySpawnRuntimeBuilding(
                    clockTowerPrefab,
                    preferredOrigin,
                    out int buildingId,
                    out Vector2Int actualOrigin,
                    out Vector2Int actualFootprint,
                    "Clock Tower",
                    "Clock tower at the heart of the old town.",
                    footprint,
                    defaultBuildingMaxHealth,
                    true))
            {
                if (DoesRectOverlapRoadCells(new RectInt(actualOrigin, actualFootprint), roadCellSizeInGridCells, roadCells))
                {
                    _buildingPlacementController.DeleteBuildingById(buildingId);
                    continue;
                }

                if (WouldBeTooCloseToReserved(actualOrigin, actualFootprint, reservedFootprints, landmarkClearanceCells))
                {
                    _buildingPlacementController.DeleteBuildingById(buildingId);
                    continue;
                }

                ReserveFootprint(reservedFootprints, actualOrigin, actualFootprint, landmarkClearanceCells);
                return;
            }
        }
    }

    private void TrySpawnFountain(Vector2Int centerRoadCell, int roadCellSizeInGridCells, HashSet<Vector2Int> roadCells, ref Unity.Mathematics.Random rng, List<ReservedFootprint> reservedFootprints)
    {
        GameObject fountainPrefab = GetRandomPrefab(fountainPrefabs, ref rng);
        if (fountainPrefab == null)
            return;

            Vector2Int footprint = GetCachedFootprintCells(fountainPrefab);
        Vector2Int[] offsets =
        {
            new(2, 3),
            new(-2, 3),
            new(3, 2),
            new(-3, 2),
            new(2, -3),
            new(-2, -3),
            new(3, -2),
            new(-3, -2),
            new(4, 0),
            new(-4, 0),
            new(0, 4),
            new(0, -4)
        };

        for (int i = 0; i < offsets.Length; i++)
        {
            if (IsTooCloseToHall(centerRoadCell, offsets[i]))
                continue;

            Vector2Int preferredOrigin = GetCenteredOriginForPlot(centerRoadCell + offsets[i], footprint, roadCellSizeInGridCells);
            if (DoesRectOverlapRoadCells(new RectInt(preferredOrigin, footprint), roadCellSizeInGridCells, roadCells))
                continue;
            if (WouldBeTooCloseToReserved(preferredOrigin, footprint, reservedFootprints, landmarkClearanceCells))
                continue;

            if (_buildingPlacementController.TrySpawnRuntimeBuilding(
                    fountainPrefab,
                    preferredOrigin,
                    out int buildingId,
                    out Vector2Int actualOrigin,
                    out Vector2Int actualFootprint,
                    "Fountain",
                    "Town fountain near the center square.",
                    footprint,
                    defaultBuildingMaxHealth,
                    true))
            {
                if (DoesRectOverlapRoadCells(new RectInt(actualOrigin, actualFootprint), roadCellSizeInGridCells, roadCells))
                {
                    _buildingPlacementController.DeleteBuildingById(buildingId);
                    continue;
                }

                if (WouldBeTooCloseToReserved(actualOrigin, actualFootprint, reservedFootprints, landmarkClearanceCells))
                {
                    _buildingPlacementController.DeleteBuildingById(buildingId);
                    continue;
                }

                ReserveFootprint(reservedFootprints, actualOrigin, actualFootprint, landmarkClearanceCells);
                return;
            }
        }
    }

    private void TrySpawnMonument(Vector2Int centerRoadCell, int roadCellSizeInGridCells, HashSet<Vector2Int> roadCells, ref Unity.Mathematics.Random rng, List<ReservedFootprint> reservedFootprints)
    {
        GameObject monumentPrefab = GetRandomPrefab(monumentPrefabs, ref rng);
        if (monumentPrefab == null)
            return;

            Vector2Int footprint = GetCachedFootprintCells(monumentPrefab);
        Vector2Int[] offsets =
        {
            new(3, 4),
            new(-3, 4),
            new(4, 3),
            new(-4, 3),
            new(3, -4),
            new(-3, -4),
            new(4, -3),
            new(-4, -3),
            new(5, 0),
            new(-5, 0),
            new(0, 5),
            new(0, -5)
        };

        for (int i = 0; i < offsets.Length; i++)
        {
            if (IsTooCloseToHall(centerRoadCell, offsets[i]))
                continue;

            Vector2Int preferredOrigin = GetCenteredOriginForPlot(centerRoadCell + offsets[i], footprint, roadCellSizeInGridCells);
            if (DoesRectOverlapRoadCells(new RectInt(preferredOrigin, footprint), roadCellSizeInGridCells, roadCells))
                continue;
            if (WouldBeTooCloseToReserved(preferredOrigin, footprint, reservedFootprints, landmarkClearanceCells))
                continue;

            if (_buildingPlacementController.TrySpawnRuntimeBuilding(
                    monumentPrefab,
                    preferredOrigin,
                    out int buildingId,
                    out Vector2Int actualOrigin,
                    out Vector2Int actualFootprint,
                    "Monument",
                    "Town monument near the center square.",
                    footprint,
                    defaultBuildingMaxHealth,
                    true))
            {
                if (DoesRectOverlapRoadCells(new RectInt(actualOrigin, actualFootprint), roadCellSizeInGridCells, roadCells))
                {
                    _buildingPlacementController.DeleteBuildingById(buildingId);
                    continue;
                }

                if (WouldBeTooCloseToReserved(actualOrigin, actualFootprint, reservedFootprints, landmarkClearanceCells))
                {
                    _buildingPlacementController.DeleteBuildingById(buildingId);
                    continue;
                }

                ReserveFootprint(reservedFootprints, actualOrigin, actualFootprint, landmarkClearanceCells);
                return;
            }
        }
    }

    private void TrySpawnPillar(Vector2Int centerRoadCell, int roadCellSizeInGridCells, HashSet<Vector2Int> roadCells, ref Unity.Mathematics.Random rng, List<ReservedFootprint> reservedFootprints)
    {
        GameObject pillarPrefab = GetRandomPrefab(pillarPrefabs, ref rng);
        if (pillarPrefab == null)
            return;

            Vector2Int footprint = GetCachedFootprintCells(pillarPrefab);
        Vector2Int[] offsets =
        {
            new(5, 2),
            new(-5, 2),
            new(2, 5),
            new(-2, 5),
            new(5, -2),
            new(-5, -2),
            new(2, -5),
            new(-2, -5),
            new(6, 0),
            new(-6, 0),
            new(0, 6),
            new(0, -6)
        };

        for (int i = 0; i < offsets.Length; i++)
        {
            if (IsTooCloseToHall(centerRoadCell, offsets[i]))
                continue;

            Vector2Int preferredOrigin = GetCenteredOriginForPlot(centerRoadCell + offsets[i], footprint, roadCellSizeInGridCells);
            if (DoesRectOverlapRoadCells(new RectInt(preferredOrigin, footprint), roadCellSizeInGridCells, roadCells))
                continue;
            if (WouldBeTooCloseToReserved(preferredOrigin, footprint, reservedFootprints, landmarkClearanceCells))
                continue;

            if (_buildingPlacementController.TrySpawnRuntimeBuilding(
                    pillarPrefab,
                    preferredOrigin,
                    out int buildingId,
                    out Vector2Int actualOrigin,
                    out Vector2Int actualFootprint,
                    "Pillar",
                    "Stone pillar near the center district.",
                    footprint,
                    defaultBuildingMaxHealth,
                    true))
            {
                if (DoesRectOverlapRoadCells(new RectInt(actualOrigin, actualFootprint), roadCellSizeInGridCells, roadCells))
                {
                    _buildingPlacementController.DeleteBuildingById(buildingId);
                    continue;
                }

                if (WouldBeTooCloseToReserved(actualOrigin, actualFootprint, reservedFootprints, landmarkClearanceCells))
                {
                    _buildingPlacementController.DeleteBuildingById(buildingId);
                    continue;
                }

                ReserveFootprint(reservedFootprints, actualOrigin, actualFootprint, landmarkClearanceCells);
                return;
            }
        }
    }

    private bool IsTooCloseToHall(Vector2Int centerRoadCell, Vector2Int offset)
    {
        int distance = Mathf.Abs(offset.x) + Mathf.Abs(offset.y);
        return distance < Mathf.Max(1, landmarkMinDistanceFromHallRoadCells);
    }

    private static void ReserveFootprint(List<ReservedFootprint> reservedFootprints, Vector2Int originCell, Vector2Int footprint, int clearanceCells)
    {
        reservedFootprints.Add(new ReservedFootprint
        {
            Rect = new RectInt(originCell, footprint),
            ClearanceCells = Mathf.Max(0, clearanceCells)
        });
    }

    private static void ReserveStandaloneEntranceCorridor(
        CityLayoutData city,
        Vector2Int startRoadCell,
        Vector2Int direction,
        int roadSegmentCount,
        int roadCellSizeInGridCells)
    {
        Vector2Int roadFootprint = new(
            Mathf.Max(1, roadCellSizeInGridCells),
            Mathf.Max(1, roadCellSizeInGridCells));

        for (int step = 0; step <= roadSegmentCount; step++)
        {
            Vector2Int roadCell = startRoadCell + direction * step;
            Vector2Int originCell = GetCenteredOriginForPlot(roadCell, roadFootprint, roadCellSizeInGridCells);
            ReserveFootprint(city.ReservedFootprints, originCell, roadFootprint, 0);
        }
    }

    private static bool WouldBeTooCloseToReserved(Vector2Int originCell, Vector2Int footprint, List<ReservedFootprint> reservedFootprints, int additionalClearanceCells)
    {
        RectInt candidateRect = new(originCell, footprint);
        for (int i = 0; i < reservedFootprints.Count; i++)
        {
            ReservedFootprint reserved = reservedFootprints[i];
            RectInt expandedReserved = ExpandRect(reserved.Rect, reserved.ClearanceCells + Mathf.Max(0, additionalClearanceCells));
            if (expandedReserved.Overlaps(candidateRect))
                return true;
        }

        return false;
    }

    private static RectInt ExpandRect(RectInt rect, int padding)
    {
        if (padding <= 0)
            return rect;

        return new RectInt(
            rect.xMin - padding,
            rect.yMin - padding,
            rect.width + padding * 2,
            rect.height + padding * 2);
    }

    private List<List<Vector2Int>> BuildTownRoadStrokes(Vector2Int center, int townRadius, int plazaRadius, ref Unity.Mathematics.Random rng)
    {
        var strokes = new List<List<Vector2Int>>();
        int ringRadius = plazaRadius + 1;

        AddStroke(strokes, new Vector2Int(center.x - ringRadius, center.y - ringRadius), new Vector2Int(center.x + ringRadius, center.y - ringRadius));
        AddStroke(strokes, new Vector2Int(center.x + ringRadius, center.y - ringRadius), new Vector2Int(center.x + ringRadius, center.y + ringRadius));
        AddStroke(strokes, new Vector2Int(center.x + ringRadius, center.y + ringRadius), new Vector2Int(center.x - ringRadius, center.y + ringRadius));
        AddStroke(strokes, new Vector2Int(center.x - ringRadius, center.y + ringRadius), new Vector2Int(center.x - ringRadius, center.y - ringRadius));

        int northLength = townRadius + rng.NextInt(0, 2);
        int southLength = townRadius - 1 + rng.NextInt(0, 3);
        int eastLength = townRadius + rng.NextInt(1, 3);
        int westLength = townRadius - 1 + rng.NextInt(0, 2);

        AddStroke(strokes, new Vector2Int(center.x, center.y + ringRadius), new Vector2Int(center.x, center.y + northLength));
        AddStroke(strokes, new Vector2Int(center.x, center.y - ringRadius), new Vector2Int(center.x, center.y - southLength));
        AddStroke(strokes, new Vector2Int(center.x + ringRadius, center.y), new Vector2Int(center.x + eastLength, center.y));
        AddStroke(strokes, new Vector2Int(center.x - ringRadius, center.y), new Vector2Int(center.x - westLength, center.y));

        AddStroke(strokes, new Vector2Int(center.x - ringRadius - 1, center.y + ringRadius + 2), new Vector2Int(center.x + ringRadius + 2, center.y + ringRadius + 2));
        AddStroke(strokes, new Vector2Int(center.x - ringRadius - 2, center.y - ringRadius - 2), new Vector2Int(center.x + ringRadius + 1, center.y - ringRadius - 2));

        AddStroke(strokes, new Vector2Int(center.x - ringRadius - 2, center.y - ringRadius), new Vector2Int(center.x - ringRadius - 2, center.y + ringRadius + 1));
        AddStroke(strokes, new Vector2Int(center.x + ringRadius + 2, center.y - ringRadius - 1), new Vector2Int(center.x + ringRadius + 2, center.y + ringRadius + 2));

        AddStroke(strokes, new Vector2Int(center.x, center.y + northLength - 1), new Vector2Int(center.x + 2 + rng.NextInt(2, 5), center.y + northLength - 1));
        AddStroke(strokes, new Vector2Int(center.x, center.y - southLength + 1), new Vector2Int(center.x - 1 - rng.NextInt(2, 5), center.y - southLength + 1));
        AddStroke(strokes, new Vector2Int(center.x + eastLength - 1, center.y), new Vector2Int(center.x + eastLength - 1, center.y + 1 + rng.NextInt(2, 5)));
        AddStroke(strokes, new Vector2Int(center.x - westLength + 1, center.y), new Vector2Int(center.x - westLength + 1, center.y - 2 - rng.NextInt(2, 5)));

        return strokes;
    }

    private List<Vector2Int> BuildAutobahnPath(
        HashSet<Vector2Int> roadCells,
        Vector2Int centerRoadCell,
        GridConfig grid,
        int roadCellSizeInGridCells)
    {
        List<AutobahnAnchorCandidate> candidates = CollectAutobahnAnchorCandidates(roadCells, centerRoadCell);
        if (candidates.Count == 0)
            return new List<Vector2Int>();

        int roadGridWidth = Mathf.Max(1, Mathf.CeilToInt(grid.Width / (float)Mathf.Max(1, roadCellSizeInGridCells)));
        int roadGridHeight = Mathf.Max(1, Mathf.CeilToInt(grid.Height / (float)Mathf.Max(1, roadCellSizeInGridCells)));
        for (int candidateIndex = 0; candidateIndex < candidates.Count; candidateIndex++)
        {
            AutobahnAnchorCandidate candidate = candidates[candidateIndex];
            int maxStepsToEdge = CalculateStepsToEdge(candidate.AnchorCell, candidate.OutwardDirection, roadGridWidth, roadGridHeight, autobahnEdgeMarginRoadCells);
            if (maxStepsToEdge < autobahnMinLengthRoadCells)
                continue;

            var path = new List<Vector2Int> { candidate.AnchorCell };
            Vector2Int current = candidate.AnchorCell;
            for (int step = 0; step < maxStepsToEdge; step++)
            {
                current += candidate.OutwardDirection;
                if (!IsWithinRoadGridBounds(current, roadGridWidth, roadGridHeight, autobahnEdgeMarginRoadCells))
                    break;

                if (roadCells.Contains(current))
                    break;

                path.Add(current);
            }

            if (path.Count >= 3)
                return path;
        }

        return new List<Vector2Int>();
    }

    private static List<AutobahnAnchorCandidate> CollectAutobahnAnchorCandidates(
        HashSet<Vector2Int> roadCells,
        Vector2Int centerRoadCell)
    {
        var candidates = new List<AutobahnAnchorCandidate>();

        foreach (Vector2Int cell in roadCells)
        {
            int neighborCount = 0;
            Vector2Int onlyNeighbor = default;
            for (int i = 0; i < CardinalDirections.Length; i++)
            {
                Vector2Int neighbor = cell + CardinalDirections[i];
                if (!roadCells.Contains(neighbor))
                    continue;

                neighborCount++;
                onlyNeighbor = neighbor;
            }

            if (neighborCount != 1)
                continue;

            Vector2Int direction = cell - onlyNeighbor;
            Vector2Int fromCenter = cell - centerRoadCell;
            int alignment = direction.x * fromCenter.x + direction.y * fromCenter.y;
            if (alignment <= 0)
                continue;

            int score = fromCenter.sqrMagnitude * 4 + alignment;
            candidates.Add(new AutobahnAnchorCandidate
            {
                AnchorCell = cell,
                OutwardDirection = direction,
                Score = score
            });
        }

        candidates.Sort((a, b) => b.Score.CompareTo(a.Score));
        return candidates;
    }

    private static int CalculateStepsToEdge(
        Vector2Int anchorCell,
        Vector2Int direction,
        int roadGridWidth,
        int roadGridHeight,
        int edgeMargin)
    {
        int minX = Mathf.Max(0, edgeMargin);
        int minY = Mathf.Max(0, edgeMargin);
        int maxX = Mathf.Max(minX, roadGridWidth - 1 - edgeMargin);
        int maxY = Mathf.Max(minY, roadGridHeight - 1 - edgeMargin);

        if (direction == East)
            return Mathf.Max(0, maxX - anchorCell.x);
        if (direction == West)
            return Mathf.Max(0, anchorCell.x - minX);
        if (direction == North)
            return Mathf.Max(0, maxY - anchorCell.y);
        if (direction == South)
            return Mathf.Max(0, anchorCell.y - minY);

        return 0;
    }

    private static bool IsWithinRoadGridBounds(Vector2Int cell, int roadGridWidth, int roadGridHeight, int edgeMargin)
    {
        int minX = Mathf.Max(0, edgeMargin);
        int minY = Mathf.Max(0, edgeMargin);
        int maxX = Mathf.Max(minX, roadGridWidth - 1 - edgeMargin);
        int maxY = Mathf.Max(minY, roadGridHeight - 1 - edgeMargin);
        return cell.x >= minX && cell.x <= maxX && cell.y >= minY && cell.y <= maxY;
    }

    private void PlaceFromPlots(
        List<GameObject> prefabs,
        List<PlotCandidate> candidates,
        int count,
        int minPlotSpacing,
        int roadCellSizeInGridCells,
        string fallbackDisplayName,
        string fallbackDescription,
        ref Unity.Mathematics.Random rng,
        List<Vector2Int> usedPlotCells,
        List<ReservedFootprint> reservedFootprints,
        List<RectInt> placementAnchors = null,
        List<RectInt> secondaryPlacementAnchors = null)
    {
        if (prefabs == null || prefabs.Count == 0 || count <= 0 || candidates == null || candidates.Count == 0)
            return;

        int placed = 0;
        for (int i = 0; i < candidates.Count && placed < count; i++)
        {
            PlotCandidate candidate = candidates[i];
            if (!HasPlotSpacing(candidate.PlotCell, usedPlotCells, minPlotSpacing))
                continue;

            GameObject prefab = GetRandomPrefab(prefabs, ref rng);
            if (prefab == null)
                continue;

            Vector2Int footprint = GetCachedFootprintCells(prefab);
            Vector2Int preferredOrigin = GetCenteredOriginForPlot(candidate.PlotCell, footprint, roadCellSizeInGridCells);
            if (WouldBeTooCloseToReserved(preferredOrigin, footprint, reservedFootprints, 0))
                continue;

            if (!_buildingPlacementController.TrySpawnRuntimeBuilding(
                    prefab,
                    preferredOrigin,
                    out int buildingId,
                    out Vector2Int actualOrigin,
                    out Vector2Int actualFootprint,
                    fallbackDisplayName,
                    fallbackDescription,
                    footprint,
                    defaultBuildingMaxHealth,
                    true))
                continue;

            if (WouldBeTooCloseToReserved(actualOrigin, actualFootprint, reservedFootprints, 0))
            {
                _buildingPlacementController.DeleteBuildingById(buildingId);
                continue;
            }

            usedPlotCells.Add(candidate.PlotCell);
            ReserveFootprint(reservedFootprints, actualOrigin, actualFootprint, 0);
            placementAnchors?.Add(new RectInt(actualOrigin, actualFootprint));
            secondaryPlacementAnchors?.Add(new RectInt(actualOrigin, actualFootprint));
            placed++;
        }
    }

    private void PlaceRuralHouses(
        List<GameObject> prefabs,
        int count,
        Vector2Int centerRoadCell,
        int townRadius,
        int roadCellSizeInGridCells,
        HashSet<Vector2Int> roadCells,
        ref Unity.Mathematics.Random rng,
        List<Vector2Int> usedPlotCells,
        List<ReservedFootprint> reservedFootprints,
        List<RectInt> placementAnchors = null,
        List<RectInt> secondaryPlacementAnchors = null)
    {
        if (prefabs == null || prefabs.Count == 0 || count <= 0)
            return;

        int attempts = 0;
        int placed = 0;
        int maxAttempts = Mathf.Max(120, count * 20);
        while (placed < count && attempts < maxAttempts)
        {
            attempts++;
            Vector2Int plotCell = new(
                centerRoadCell.x + rng.NextInt(-(townRadius + 3), townRadius + 4),
                centerRoadCell.y + rng.NextInt(-(townRadius + 3), townRadius + 4));

            int distance = Mathf.Abs(plotCell.x - centerRoadCell.x) + Mathf.Abs(plotCell.y - centerRoadCell.y);
            if (distance < hallPlazaRadiusRoadCells + 5 || distance > townRadius + 3)
                continue;
            if (roadCells.Contains(plotCell))
                continue;
            if (!HasPlotSpacing(plotCell, usedPlotCells, 1))
                continue;

            GameObject prefab = GetRandomPrefab(prefabs, ref rng);
            if (prefab == null)
                continue;

            Vector2Int footprint = GetCachedFootprintCells(prefab);
            Vector2Int preferredOrigin = GetCenteredOriginForPlot(plotCell, footprint, roadCellSizeInGridCells);
            if (WouldBeTooCloseToReserved(preferredOrigin, footprint, reservedFootprints, 0))
                continue;

            if (!_buildingPlacementController.TrySpawnRuntimeBuilding(
                    prefab,
                    preferredOrigin,
                    out int buildingId,
                    out Vector2Int actualOrigin,
                    out Vector2Int actualFootprint,
                    "House",
                    "Rural old town house.",
                    footprint,
                    defaultBuildingMaxHealth,
                    true))
            {
                continue;
            }

            if (WouldBeTooCloseToReserved(actualOrigin, actualFootprint, reservedFootprints, 0))
            {
                _buildingPlacementController.DeleteBuildingById(buildingId);
                continue;
            }

            usedPlotCells.Add(plotCell);
            ReserveFootprint(reservedFootprints, actualOrigin, actualFootprint, 0);
            placementAnchors?.Add(new RectInt(actualOrigin, actualFootprint));
            secondaryPlacementAnchors?.Add(new RectInt(actualOrigin, actualFootprint));
            placed++;
        }
    }

    private void PlaceHouseYardWalls(
        List<RectInt> houseFootprints,
        Vector2Int centerRoadCell,
        int roadCellSizeInGridCells,
        HashSet<Vector2Int> roadCells,
        GridConfig grid,
        ref Unity.Mathematics.Random rng,
        List<ReservedFootprint> reservedFootprints)
    {
        if (houseFootprints == null || houseFootprints.Count == 0)
            return;
        if (houseWallPrefabs == null || houseWallPrefabs.Count == 0 || houseWallGatePrefab == null)
            return;

        var shuffledHouses = new List<RectInt>(houseFootprints);
        Shuffle(shuffledHouses, ref rng);

        int targetCount = Mathf.RoundToInt(shuffledHouses.Count * Mathf.Clamp01(houseWallChance));
        int builtCount = 0;
        for (int i = 0; i < shuffledHouses.Count && builtCount < targetCount; i++)
        {
            if (TryBuildHouseYardWall(shuffledHouses[i], centerRoadCell, roadCellSizeInGridCells, roadCells, grid, ref rng, reservedFootprints))
                builtCount++;
        }
    }

    private bool TryBuildHouseYardWall(
        RectInt houseRect,
        Vector2Int centerRoadCell,
        int roadCellSizeInGridCells,
        HashSet<Vector2Int> roadCells,
        GridConfig grid,
        ref Unity.Mathematics.Random rng,
        List<ReservedFootprint> reservedFootprints)
    {
        int minPadding = Mathf.Max(1, Mathf.Min(houseWallMinDistanceCells, houseWallMaxDistanceCells));
        int maxPadding = Mathf.Max(minPadding, houseWallMaxDistanceCells);
        var candidatePaddings = new List<int>();
        for (int padding = minPadding; padding <= maxPadding; padding++)
            candidatePaddings.Add(padding);
        Shuffle(candidatePaddings, ref rng);

        for (int i = 0; i < candidatePaddings.Count; i++)
        {
            RectInt yardRect = ExpandRect(houseRect, candidatePaddings[i]);
            if (!CanPlaceHouseYardRect(yardRect, houseRect, roadCellSizeInGridCells, roadCells, reservedFootprints, grid))
                continue;

            Vector2Int cityCenterGridCell = new(
                centerRoadCell.x * roadCellSizeInGridCells + Mathf.FloorToInt(roadCellSizeInGridCells * 0.5f),
                centerRoadCell.y * roadCellSizeInGridCells + Mathf.FloorToInt(roadCellSizeInGridCells * 0.5f));
            YardSide gateSide = GetPreferredYardGateSide(houseRect, cityCenterGridCell);
            GameObject wallPrefab = GetRandomPrefab(houseWallPrefabs, ref rng);
            if (wallPrefab == null)
                return false;

            BuildYardBoundaryVisuals(yardRect, gateSide, wallPrefab, houseWallGatePrefab, houseWallPillarPrefab, grid);
            ReserveFootprint(reservedFootprints, yardRect.position, yardRect.size, 0);
            return true;
        }

        return false;
    }

    private bool CanPlaceHouseYardRect(
        RectInt yardRect,
        RectInt houseRect,
        int roadCellSizeInGridCells,
        HashSet<Vector2Int> roadCells,
        List<ReservedFootprint> reservedFootprints,
        GridConfig grid)
    {
        if (yardRect.xMin < 0 || yardRect.yMin < 0 || yardRect.xMax > grid.Width || yardRect.yMax > grid.Height)
            return false;
        if (DoesRectOverlapRoadCells(yardRect, roadCellSizeInGridCells, roadCells))
            return false;

        for (int i = 0; i < reservedFootprints.Count; i++)
        {
            RectInt reserved = reservedFootprints[i].Rect;
            if (RectsEqual(reserved, houseRect))
                continue;
            if (yardRect.Overlaps(reserved))
                return false;
        }

        return true;
    }

    private void BuildYardBoundaryVisuals(
        RectInt yardRect,
        YardSide gateSide,
        GameObject wallPrefab,
        GameObject gatePrefab,
        GameObject pillarPrefab,
        GridConfig grid)
    {
        int horizontalThickness = GetMinorFootprint(wallPrefab);
        int verticalThickness = GetMinorFootprint(wallPrefab);
        int horizontalGateLength = Mathf.Max(1, GetMajorFootprint(gatePrefab));
        int verticalGateLength = Mathf.Max(1, GetMajorFootprint(gatePrefab));

        int northGateStart = gateSide == YardSide.North ? GetCenteredOpeningStart(yardRect.width, horizontalGateLength) : -1;
        int southGateStart = gateSide == YardSide.South ? GetCenteredOpeningStart(yardRect.width, horizontalGateLength) : -1;
        int eastGateStart = gateSide == YardSide.East ? GetCenteredOpeningStart(yardRect.height, verticalGateLength) : -1;
        int westGateStart = gateSide == YardSide.West ? GetCenteredOpeningStart(yardRect.height, verticalGateLength) : -1;

        PlaceHorizontalWallSide(yardRect, yardRect.yMin, yardRect.width, wallPrefab, gatePrefab, grid, southGateStart, horizontalGateLength, false, horizontalThickness);
        PlaceHorizontalWallSide(yardRect, yardRect.yMax - horizontalThickness, yardRect.width, wallPrefab, gatePrefab, grid, northGateStart, horizontalGateLength, false, horizontalThickness);
        PlaceVerticalWallSide(yardRect, yardRect.xMin, yardRect.height, wallPrefab, gatePrefab, grid, westGateStart, verticalGateLength, verticalThickness);
        PlaceVerticalWallSide(yardRect, yardRect.xMax - verticalThickness, yardRect.height, wallPrefab, gatePrefab, grid, eastGateStart, verticalGateLength, verticalThickness);

        if (pillarPrefab != null)
        {
            Vector2Int pillarFootprint = GetCachedFootprintCells(pillarPrefab);
            SpawnVisualOnlyPrefab(pillarPrefab, new Vector2Int(yardRect.xMin, yardRect.yMin), pillarFootprint, Quaternion.identity, grid);
            SpawnVisualOnlyPrefab(pillarPrefab, new Vector2Int(yardRect.xMax - pillarFootprint.x, yardRect.yMin), pillarFootprint, Quaternion.identity, grid);
            SpawnVisualOnlyPrefab(pillarPrefab, new Vector2Int(yardRect.xMin, yardRect.yMax - pillarFootprint.y), pillarFootprint, Quaternion.identity, grid);
            SpawnVisualOnlyPrefab(pillarPrefab, new Vector2Int(yardRect.xMax - pillarFootprint.x, yardRect.yMax - pillarFootprint.y), pillarFootprint, Quaternion.identity, grid);
        }
    }

    private void PlaceHorizontalWallSide(
        RectInt yardRect,
        int yOrigin,
        int totalLength,
        GameObject wallPrefab,
        GameObject gatePrefab,
        GridConfig grid,
        int gateStartOffset,
        int gateLength,
        bool rotateGate,
        int thickness)
    {
        PlaceHorizontalWallRun(yardRect.xMin, yOrigin, totalLength, wallPrefab, grid, thickness, gateStartOffset, gateLength);
        if (gateStartOffset >= 0)
        {
            Vector2Int gateFootprint = new(Mathf.Max(1, gateLength), Mathf.Max(1, GetMinorFootprint(gatePrefab)));
            SpawnVisualOnlyPrefab(gatePrefab, new Vector2Int(yardRect.xMin + gateStartOffset, yOrigin), gateFootprint, rotateGate ? Quaternion.Euler(0f, 90f, 0f) : Quaternion.identity, grid);
        }
    }

    private void PlaceHorizontalWallRun(
        int xOrigin,
        int yOrigin,
        int totalLength,
        GameObject wallPrefab,
        GridConfig grid,
        int thickness,
        int gateStartOffset,
        int gateLength)
    {
        int segmentLength = Mathf.Max(1, GetMajorFootprint(wallPrefab));
        int current = 0;
        while (current < totalLength)
        {
            if (gateStartOffset >= 0 && current >= gateStartOffset && current < gateStartOffset + gateLength)
            {
                current = gateStartOffset + gateLength;
                continue;
            }

            int nextStop = totalLength;
            if (gateStartOffset >= 0 && current < gateStartOffset)
                nextStop = gateStartOffset;
            int pieceLength = Mathf.Min(segmentLength, nextStop - current);
            if (pieceLength <= 0)
                break;

            SpawnVisualOnlyPrefab(
                wallPrefab,
                new Vector2Int(xOrigin + current, yOrigin),
                new Vector2Int(pieceLength, Mathf.Max(1, thickness)),
                Quaternion.identity,
                grid);
            current += pieceLength;
        }
    }

    private void PlaceVerticalWallSide(
        RectInt yardRect,
        int xOrigin,
        int totalLength,
        GameObject wallPrefab,
        GameObject gatePrefab,
        GridConfig grid,
        int gateStartOffset,
        int gateLength,
        int thickness)
    {
        PlaceVerticalWallRun(xOrigin, yardRect.yMin, totalLength, wallPrefab, grid, thickness, gateStartOffset, gateLength);
        if (gateStartOffset >= 0)
        {
            Vector2Int gateFootprint = new(Mathf.Max(1, GetMinorFootprint(gatePrefab)), Mathf.Max(1, gateLength));
            SpawnVisualOnlyPrefab(gatePrefab, new Vector2Int(xOrigin, yardRect.yMin + gateStartOffset), gateFootprint, Quaternion.Euler(0f, 90f, 0f), grid);
        }
    }

    private void PlaceVerticalWallRun(
        int xOrigin,
        int yOrigin,
        int totalLength,
        GameObject wallPrefab,
        GridConfig grid,
        int thickness,
        int gateStartOffset,
        int gateLength)
    {
        int segmentLength = Mathf.Max(1, GetMajorFootprint(wallPrefab));
        int current = 0;
        while (current < totalLength)
        {
            if (gateStartOffset >= 0 && current >= gateStartOffset && current < gateStartOffset + gateLength)
            {
                current = gateStartOffset + gateLength;
                continue;
            }

            int nextStop = totalLength;
            if (gateStartOffset >= 0 && current < gateStartOffset)
                nextStop = gateStartOffset;
            int pieceLength = Mathf.Min(segmentLength, nextStop - current);
            if (pieceLength <= 0)
                break;

            SpawnVisualOnlyPrefab(
                wallPrefab,
                new Vector2Int(xOrigin, yOrigin + current),
                new Vector2Int(Mathf.Max(1, thickness), pieceLength),
                Quaternion.Euler(0f, 90f, 0f),
                grid);
            current += pieceLength;
        }
    }

    private static int GetCenteredOpeningStart(int totalLength, int openingLength)
    {
        if (openingLength >= totalLength - 1)
            return Mathf.Max(0, (totalLength - Mathf.Max(1, totalLength / 2)) / 2);

        return Mathf.Clamp((totalLength - openingLength) / 2, 1, Mathf.Max(1, totalLength - openingLength - 1));
    }

    private static YardSide GetPreferredYardGateSide(RectInt houseRect, Vector2Int centerRoadCell)
    {
        Vector2 houseCenter = new(houseRect.center.x, houseRect.center.y);
        Vector2 cityCenter = new(centerRoadCell.x, centerRoadCell.y);
        Vector2 delta = cityCenter - houseCenter;

        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            return delta.x >= 0f ? YardSide.East : YardSide.West;

        return delta.y >= 0f ? YardSide.North : YardSide.South;
    }

    private static bool DoesRectOverlapRoadCells(RectInt rect, int roadCellSizeInGridCells, HashSet<Vector2Int> roadCells)
    {
        foreach (Vector2Int roadCell in roadCells)
        {
            RectInt roadRect = new(
                roadCell.x * roadCellSizeInGridCells,
                roadCell.y * roadCellSizeInGridCells,
                roadCellSizeInGridCells,
                roadCellSizeInGridCells);
            if (rect.Overlaps(roadRect))
                return true;
        }

        return false;
    }

    private static bool RectsEqual(RectInt a, RectInt b)
    {
        return a.xMin == b.xMin && a.yMin == b.yMin && a.width == b.width && a.height == b.height;
    }

    private void PlaceCityDecorationBuildings(
        List<GameObject> prefabs,
        int count,
        Vector2Int centerRoadCell,
        int townRadius,
        int roadCellSizeInGridCells,
        HashSet<Vector2Int> roadCells,
        ref Unity.Mathematics.Random rng,
        List<Vector2Int> usedPlotCells,
        List<ReservedFootprint> reservedFootprints,
        List<RectInt> shopAndHouseFootprints)
    {
        if (prefabs == null || prefabs.Count == 0 || count <= 0)
            return;

        List<GameObject> clothCoverPrefabs = prefabs.FindAll(static prefab => prefab != null && prefab.name.Contains("ClothCover", StringComparison.OrdinalIgnoreCase));
        List<GameObject> archwayPrefabs = prefabs.FindAll(static prefab => prefab != null && prefab.name.Contains("Archway", StringComparison.OrdinalIgnoreCase));
        List<GameObject> freeScatterPrefabs = prefabs.FindAll(static prefab =>
            prefab != null &&
            !prefab.name.Contains("ClothCover", StringComparison.OrdinalIgnoreCase) &&
            !prefab.name.Contains("Archway", StringComparison.OrdinalIgnoreCase));
        int clothPlaced = PlaceClothCoverBuildings(
            clothCoverPrefabs,
            count,
            ref rng,
            reservedFootprints,
            shopAndHouseFootprints);
        int archwaysPlaced = PlaceCentralArchwayBuildings(
            archwayPrefabs,
            count - clothPlaced,
            centerRoadCell,
            roadCellSizeInGridCells,
            roadCells,
            ref rng,
            usedPlotCells,
            reservedFootprints);
        int remainingCount = count - clothPlaced - archwaysPlaced;
        if (remainingCount <= 0)
            return;

        int attempts = 0;
        int placed = 0;
        int maxAttempts = Mathf.Max(160, remainingCount * 24);
        int maxDistance = townRadius + 3;
        List<GameObject> randomPrefabs = freeScatterPrefabs.Count > 0 ? freeScatterPrefabs : prefabs;

        while (placed < remainingCount && attempts < maxAttempts)
        {
            attempts++;
            Vector2Int plotCell = GetRandomScatterPlotCell(centerRoadCell, maxDistance, ref rng);

            int distance = Mathf.Abs(plotCell.x - centerRoadCell.x) + Mathf.Abs(plotCell.y - centerRoadCell.y);
            if (distance > maxDistance)
                continue;
            if (roadCells.Contains(plotCell))
                continue;
            if (!HasPlotSpacing(plotCell, usedPlotCells, 1))
                continue;

            GameObject prefab = GetRandomPrefab(randomPrefabs, ref rng);
            if (prefab == null)
                continue;

            Vector2Int footprint = GetCachedFootprintCells(prefab);
            Vector2Int preferredOrigin = GetCenteredOriginForPlot(plotCell, footprint, roadCellSizeInGridCells);
            if (WouldBeTooCloseToReserved(preferredOrigin, footprint, reservedFootprints, 0))
                continue;

            if (!_buildingPlacementController.TrySpawnRuntimeBuilding(
                    prefab,
                    preferredOrigin,
                    out int buildingId,
                    out Vector2Int actualOrigin,
                    out Vector2Int actualFootprint,
                    "City Decoration",
                    "Decorative old-town structure.",
                    footprint,
                    defaultBuildingMaxHealth,
                    true))
            {
                continue;
            }

            if (WouldBeTooCloseToReserved(actualOrigin, actualFootprint, reservedFootprints, 0))
            {
                _buildingPlacementController.DeleteBuildingById(buildingId);
                continue;
            }

            usedPlotCells.Add(plotCell);
            ReserveFootprint(reservedFootprints, actualOrigin, actualFootprint, 0);
            placed++;
        }
    }

    private int PlaceCentralArchwayBuildings(
        List<GameObject> archwayPrefabs,
        int maxCount,
        Vector2Int centerRoadCell,
        int roadCellSizeInGridCells,
        HashSet<Vector2Int> roadCells,
        ref Unity.Mathematics.Random rng,
        List<Vector2Int> usedPlotCells,
        List<ReservedFootprint> reservedFootprints)
    {
        if (archwayPrefabs == null || archwayPrefabs.Count == 0 || maxCount <= 0)
            return 0;

        int placed = 0;
        int attempts = 0;
        int maxAttempts = Mathf.Max(120, maxCount * 24);
        int minDistance = Mathf.Max(1, hallPlazaRadiusRoadCells + 1);
        int maxDistance = hallPlazaRadiusRoadCells + 5;

        while (placed < maxCount && attempts < maxAttempts)
        {
            attempts++;
            Vector2Int plotCell = GetRandomScatterPlotCell(centerRoadCell, maxDistance, ref rng);
            int distance = Mathf.Abs(plotCell.x - centerRoadCell.x) + Mathf.Abs(plotCell.y - centerRoadCell.y);
            if (distance < minDistance || distance > maxDistance)
                continue;
            if (roadCells.Contains(plotCell))
                continue;
            if (!HasPlotSpacing(plotCell, usedPlotCells, 1))
                continue;

            GameObject prefab = archwayPrefabs[placed % archwayPrefabs.Count];
            if (prefab == null)
                continue;

            Vector2Int footprint = GetCachedFootprintCells(prefab);
            Vector2Int preferredOrigin = GetCenteredOriginForPlot(plotCell, footprint, roadCellSizeInGridCells);
            if (WouldBeTooCloseToReserved(preferredOrigin, footprint, reservedFootprints, 0))
                continue;

            if (!_buildingPlacementController.TrySpawnRuntimeBuilding(
                    prefab,
                    preferredOrigin,
                    out int buildingId,
                    out Vector2Int actualOrigin,
                    out Vector2Int actualFootprint,
                    "Archway",
                    "Decorative archway near the town center.",
                    footprint,
                    defaultBuildingMaxHealth,
                    true))
            {
                continue;
            }

            if (WouldBeTooCloseToReserved(actualOrigin, actualFootprint, reservedFootprints, 0))
            {
                _buildingPlacementController.DeleteBuildingById(buildingId);
                continue;
            }

            usedPlotCells.Add(plotCell);
            ReserveFootprint(reservedFootprints, actualOrigin, actualFootprint, 0);
            placed++;
        }

        return placed;
    }

    private int PlaceClothCoverBuildings(
        List<GameObject> clothCoverPrefabs,
        int maxCount,
        ref Unity.Mathematics.Random rng,
        List<ReservedFootprint> reservedFootprints,
        List<RectInt> shopAndHouseFootprints)
    {
        if (clothCoverPrefabs == null || clothCoverPrefabs.Count == 0 || maxCount <= 0 || shopAndHouseFootprints == null || shopAndHouseFootprints.Count == 0)
            return 0;

        var anchorIndices = new List<int>(shopAndHouseFootprints.Count);
        for (int i = 0; i < shopAndHouseFootprints.Count; i++)
            anchorIndices.Add(i);
        Shuffle(anchorIndices, ref rng);

        int placed = 0;
        int anchorCursor = 0;
        int prefabCursor = 0;
        int targetCount = Mathf.Min(maxCount, clothCoverPrefabs.Count);
        while (placed < targetCount && anchorCursor < anchorIndices.Count)
        {
            GameObject prefab = clothCoverPrefabs[prefabCursor % clothCoverPrefabs.Count];
            prefabCursor++;
            RectInt anchor = shopAndHouseFootprints[anchorIndices[anchorCursor]];
            anchorCursor++;

            if (TrySpawnAdjacentDecoration(prefab, anchor, ref rng, reservedFootprints))
                placed++;
        }

        return placed;
    }

    private bool TrySpawnAdjacentDecoration(
        GameObject prefab,
        RectInt anchorRect,
        ref Unity.Mathematics.Random rng,
        List<ReservedFootprint> reservedFootprints)
    {
        if (prefab == null)
            return false;

        Vector2Int footprint = GetCachedFootprintCells(prefab);
        var candidateOrigins = BuildAdjacentOrigins(anchorRect, footprint);
        Shuffle(candidateOrigins, ref rng);

        for (int i = 0; i < candidateOrigins.Count; i++)
        {
            Vector2Int preferredOrigin = candidateOrigins[i];
            if (WouldBeTooCloseToReserved(preferredOrigin, footprint, reservedFootprints, 0))
                continue;

            if (!_buildingPlacementController.TrySpawnRuntimeBuilding(
                    prefab,
                    preferredOrigin,
                    out int buildingId,
                    out Vector2Int actualOrigin,
                    out Vector2Int actualFootprint,
                    "City Decoration",
                    "Decorative structure beside a town building.",
                    footprint,
                    defaultBuildingMaxHealth,
                    true))
            {
                continue;
            }

            if (WouldBeTooCloseToReserved(actualOrigin, actualFootprint, reservedFootprints, 0) ||
                !TouchesRect(new RectInt(actualOrigin, actualFootprint), anchorRect))
            {
                _buildingPlacementController.DeleteBuildingById(buildingId);
                continue;
            }

            ReserveFootprint(reservedFootprints, actualOrigin, actualFootprint, 0);
            return true;
        }

        return false;
    }

    private static List<Vector2Int> BuildAdjacentOrigins(RectInt anchorRect, Vector2Int footprint)
    {
        var origins = new List<Vector2Int>();

        int leftMinY = anchorRect.yMin - footprint.y + 1;
        int leftMaxY = anchorRect.yMax - 1;
        for (int y = leftMinY; y <= leftMaxY; y++)
        {
            origins.Add(new Vector2Int(anchorRect.xMin - footprint.x, y));
            origins.Add(new Vector2Int(anchorRect.xMax, y));
        }

        int bottomMinX = anchorRect.xMin - footprint.x + 1;
        int bottomMaxX = anchorRect.xMax - 1;
        for (int x = bottomMinX; x <= bottomMaxX; x++)
        {
            origins.Add(new Vector2Int(x, anchorRect.yMin - footprint.y));
            origins.Add(new Vector2Int(x, anchorRect.yMax));
        }

        return origins;
    }

    private static bool TouchesRect(RectInt rectA, RectInt rectB)
    {
        bool horizontalTouch =
            (rectA.xMax == rectB.xMin || rectA.xMin == rectB.xMax) &&
            rectA.yMin < rectB.yMax &&
            rectA.yMax > rectB.yMin;
        bool verticalTouch =
            (rectA.yMax == rectB.yMin || rectA.yMin == rectB.yMax) &&
            rectA.xMin < rectB.xMax &&
            rectA.xMax > rectB.xMin;
        return horizontalTouch || verticalTouch;
    }

    private static Vector2Int GetRandomScatterPlotCell(Vector2Int centerRoadCell, int maxDistance, ref Unity.Mathematics.Random rng)
    {
        float angle = rng.NextFloat(0f, Mathf.PI * 2f);
        float radius = Mathf.Sqrt(rng.NextFloat()) * maxDistance;
        return new Vector2Int(
            centerRoadCell.x + Mathf.RoundToInt(Mathf.Cos(angle) * radius),
            centerRoadCell.y + Mathf.RoundToInt(Mathf.Sin(angle) * radius));
    }

    private static List<PlotCandidate> CollectRoadsidePlots(
        HashSet<Vector2Int> roadCells,
        Vector2Int centerRoadCell,
        int townRadius,
        int minDistance,
        int maxDistance)
    {
        var results = new List<PlotCandidate>();
        var seenPlots = new HashSet<Vector2Int>();

        foreach (Vector2Int roadCell in roadCells)
        {
            for (int i = 0; i < CardinalDirections.Length; i++)
            {
                Vector2Int plotCell = roadCell + CardinalDirections[i];
                if (roadCells.Contains(plotCell) || !seenPlots.Add(plotCell))
                    continue;

                int distance = Mathf.Abs(plotCell.x - centerRoadCell.x) + Mathf.Abs(plotCell.y - centerRoadCell.y);
                if (distance < minDistance || distance > maxDistance)
                    continue;
                if (Mathf.Abs(plotCell.x - centerRoadCell.x) > townRadius + 3 || Mathf.Abs(plotCell.y - centerRoadCell.y) > townRadius + 3)
                    continue;

                results.Add(new PlotCandidate
                {
                    PlotCell = plotCell,
                    DistanceFromCenter = distance
                });
            }
        }

        results.Sort((a, b) => a.DistanceFromCenter.CompareTo(b.DistanceFromCenter));
        return results;
    }

    private static bool HasPlotSpacing(Vector2Int candidate, List<Vector2Int> usedPlots, int minSpacing)
    {
        for (int i = 0; i < usedPlots.Count; i++)
        {
            Vector2Int used = usedPlots[i];
            if (Mathf.Abs(candidate.x - used.x) <= minSpacing && Mathf.Abs(candidate.y - used.y) <= minSpacing)
                return false;
        }

        return true;
    }

    private static void AddStroke(List<List<Vector2Int>> strokes, Vector2Int start, Vector2Int end)
    {
        var cells = new List<Vector2Int>();
        cells.Add(start);
        if (start.x == end.x || start.y == end.y)
        {
            AppendStraightSegment(cells, start, end);
            strokes.Add(cells);
            return;
        }

        Vector2Int corner = new(end.x, start.y);
        AppendStraightSegment(cells, start, corner);
        AppendStraightSegment(cells, corner, end);
        strokes.Add(cells);
    }

    private static void AppendStraightSegment(List<Vector2Int> cells, Vector2Int from, Vector2Int to)
    {
        Vector2Int direction = new(Math.Sign(to.x - from.x), Math.Sign(to.y - from.y));
        Vector2Int current = cells[cells.Count - 1];
        while (current != to)
        {
            current += direction;
            if (cells[cells.Count - 1] != current)
                cells.Add(current);
        }
    }

    private static Vector2Int GetCenteredOriginForPlot(Vector2Int plotCell, Vector2Int footprint, int roadCellSizeInGridCells)
    {
        return new Vector2Int(
            plotCell.x * roadCellSizeInGridCells + Mathf.FloorToInt((roadCellSizeInGridCells - footprint.x) * 0.5f),
            plotCell.y * roadCellSizeInGridCells + Mathf.FloorToInt((roadCellSizeInGridCells - footprint.y) * 0.5f));
    }

    private void EnsureCityVisualRoot()
    {
        if (_cityVisualRoot != null)
            return;

        var root = new GameObject("RuntimeCityVisuals");
        _cityVisualRoot = root.transform;
        _cityVisualRoot.SetParent(_runtimeRoot, false);
        _cityVisualRoot.localPosition = Vector3.zero;
        _cityVisualRoot.localRotation = Quaternion.identity;
        _cityVisualRoot.localScale = Vector3.one;
    }

    private GameObject SpawnVisualOnlyPrefab(GameObject prefab, Vector2Int originCell, Vector2Int footprintCells, Quaternion rotation, GridConfig grid)
    {
        if (prefab == null)
            return null;

        EnsureCityVisualRoot();

        var wrapper = new GameObject($"{prefab.name}_Visual");
        wrapper.transform.SetParent(_cityVisualRoot, false);
        wrapper.transform.SetPositionAndRotation(GetFootprintCenter(originCell, footprintCells, grid), rotation);
        wrapper.transform.localScale = Vector3.one;

        GameObject visual;
        Transform combinedMesh = prefab.transform.Find("CombinedMesh");
        if (combinedMesh != null)
            visual = UnityEngine.Object.Instantiate(combinedMesh.gameObject, wrapper.transform);
        else
            visual = UnityEngine.Object.Instantiate(prefab, wrapper.transform);

        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one;
        if (TryGetLocalBounds(visual, out Bounds bounds))
            visual.transform.localPosition = new Vector3(-bounds.center.x, 0f, -bounds.center.z);
        else
            visual.transform.localPosition = Vector3.zero;

        SetChildVisibleByName(visual.transform, "Destroyed", false);
        return wrapper;
    }

    private static bool TryGetLocalBounds(GameObject target, out Bounds bounds)
    {
        bounds = default;
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        bool hasBounds = false;
        Matrix4x4 worldToLocal = target.transform.worldToLocalMatrix;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
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

    private static void SetChildVisibleByName(Transform root, string targetName, bool visible)
    {
        Transform child = FindDescendantByName(root, targetName);
        if (child != null)
            child.gameObject.SetActive(visible);
    }

    private static Transform FindDescendantByName(Transform root, string targetName)
    {
        if (root == null || string.IsNullOrEmpty(targetName))
            return null;
        if (root.name == targetName)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDescendantByName(root.GetChild(i), targetName);
            if (found != null)
                return found;
        }

        return null;
    }

    private int GetMajorFootprint(GameObject prefab)
    {
        Vector2Int footprint = GetCachedFootprintCells(prefab);
        return Mathf.Max(1, Mathf.Max(footprint.x, footprint.y));
    }

    private int GetMinorFootprint(GameObject prefab)
    {
        Vector2Int footprint = GetCachedFootprintCells(prefab);
        return Mathf.Max(1, Mathf.Min(footprint.x, footprint.y));
    }

    private Vector3 GetFootprintCenter(Vector2Int originCell, Vector2Int footprintCells, GridConfig grid)
    {
        return new Vector3(
            grid.Origin.x + (originCell.x + footprintCells.x * 0.5f) * grid.CellSize,
            0f,
            grid.Origin.z + (originCell.y + footprintCells.y * 0.5f) * grid.CellSize);
    }

    private Vector2Int GetCachedFootprintCells(GameObject prefab)
    {
        if (prefab == null)
            return new Vector2Int(6, 6);

        if (_prefabFootprintCache.TryGetValue(prefab, out Vector2Int footprint))
            return footprint;

        footprint = EstimateFootprintCells(prefab);
        _prefabFootprintCache[prefab] = footprint;
        return footprint;
    }

    private static Vector2Int EstimateFootprintCells(GameObject prefab)
    {
        if (prefab == null)
            return new Vector2Int(6, 6);

        Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0)
            return new Vector2Int(6, 6);

        Bounds bounds = default;
        bool hasBounds = false;
        Matrix4x4 worldToLocal = prefab.transform.worldToLocalMatrix;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
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

        if (!hasBounds)
            return new Vector2Int(6, 6);

        return new Vector2Int(
            Mathf.Max(1, Mathf.CeilToInt(Mathf.Abs(bounds.size.x))),
            Mathf.Max(1, Mathf.CeilToInt(Mathf.Abs(bounds.size.z))));
    }

    private static GameObject GetRandomPrefab(List<GameObject> prefabs, ref Unity.Mathematics.Random rng)
    {
        if (prefabs == null || prefabs.Count == 0)
            return null;

        return prefabs[rng.NextInt(0, prefabs.Count)];
    }

    private static void Shuffle<T>(List<T> list, ref Unity.Mathematics.Random rng)
    {
        if (list == null)
            return;

        for (int i = list.Count - 1; i > 0; i--)
        {
            int swapIndex = rng.NextInt(0, i + 1);
            T value = list[i];
            list[i] = list[swapIndex];
            list[swapIndex] = value;
        }
    }

    private bool TryGetGridData(out Entity gridEntity, out GridConfig grid, out DynamicBuffer<GridRoad> roads, out DynamicBlockerData blockerData)
    {
        gridEntity = Entity.Null;
        grid = default;
        roads = default;
        blockerData = default;

        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return false;

        EntityManager em = world.EntityManager;
        EnsureEntityQueries(em);
        if (_gridDataQuery.IsEmptyIgnoreFilter)
            return false;

        gridEntity = _gridDataQuery.GetSingletonEntity();
        grid = em.GetComponentData<GridConfig>(gridEntity);
        roads = em.GetBuffer<GridRoad>(gridEntity);
        blockerData = em.GetComponentData<DynamicBlockerData>(gridEntity);
        return true;
    }
}
