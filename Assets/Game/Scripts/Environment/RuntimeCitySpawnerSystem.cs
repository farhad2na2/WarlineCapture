using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using CityChainAxis = RuntimeCityLayoutSystem.CityChainAxis;
using CityLayoutData = RuntimeCityLayoutSystem.CityLayoutData;

public sealed class RuntimeCitySpawnerSystem
{
    private static readonly bool EnableRuntimeCityDiagnostics = false;

    private RuntimeCitySpawnerSystemConfig config;
    private readonly RuntimeCityConfigSystem _runtimeCityConfigSystem = new();
    private readonly RuntimeCityLayoutSystem _runtimeCityLayoutSystem = new();
    private readonly RuntimeCityRoadLayoutSystem _runtimeCityRoadLayoutSystem = new();
    private readonly RuntimeCityBuildingPlotSystem _runtimeCityBuildingPlotSystem = new();
    private readonly RuntimeCityWalkabilitySystem _runtimeCityWalkabilitySystem = new();
    private readonly RuntimeCityPrefabSelectionSystem _runtimeCityPrefabSelectionSystem = new();
    private readonly RuntimeCityBuildingSpawnSystem _runtimeCityBuildingSpawnSystem = new();
    private readonly RuntimeCityVisualSystem _runtimeCityVisualSystem = new();
    private readonly RuntimeCitySpawnBridgeSystem _runtimeCitySpawnBridgeSystem = new();
    private readonly RuntimeCityRoadBuildBridgeSystem _runtimeCityRoadBuildBridgeSystem = new();
    private readonly RuntimeGameplayStateSystem _runtimeGameplayStateSystem = new();

    private RuntimeCityConfigSystem.Snapshot cityConfig => _runtimeCityConfigSystem.Current;
    private bool spawnOnStart => cityConfig.SpawnOnStart;
    private bool generateBuildings => cityConfig.GenerateBuildings;
    private uint randomSeed => cityConfig.RandomSeed;
    private int cityCount => cityConfig.CityCount;
    private Vector2Int startCell => cityConfig.StartCell;
    private int generationYieldInterval => cityConfig.GenerationYieldInterval;
    private int hallPlazaRadiusRoadCells => cityConfig.HallPlazaRadiusRoadCells;
    private int extraTownRadiusRoadCells => cityConfig.ExtraTownRadiusRoadCells;
    private int cityMinSpacingRoadCells => cityConfig.CityMinSpacingRoadCells;
    private int autobahnMinLengthRoadCells => cityConfig.AutobahnMinLengthRoadCells;
    private int autobahnEdgeMarginRoadCells => cityConfig.AutobahnEdgeMarginRoadCells;
    private List<GameObject> hallPrefabs => cityConfig.HallPrefabs;
    private List<GameObject> shopPrefabs => cityConfig.ShopPrefabs;
    private List<GameObject> housePrefabs => cityConfig.HousePrefabs;

    private MainMenuPlayUI _mainMenuPlayUi;
    private IEnumerator _generationRoutine;
    private int _generationStartedFrame = -1;
    private int _generationMoveNextCount;
    private int _nextGenerationDiagnosticFrame;
    private int _nextInitialSpawnWaitDiagnosticFrame;
    private bool _spawned;
    private World _queryWorld;
    private EntityQuery _gridDataQuery;
    private Transform _runtimeRoot;

    public bool SpawnOnStartEnabled => spawnOnStart;
    public bool HasSpawned => _spawned || cityCount <= 0;
    public bool IsGenerating => _generationRoutine != null;

    private static readonly Vector2Int North = new(0, 1);
    private static readonly Vector2Int East = new(1, 0);
    private static readonly Vector2Int South = new(0, -1);
    private static readonly Vector2Int West = new(-1, 0);
    private static readonly Vector2Int[] CardinalDirections = { North, East, South, West };

    internal void Init(
        RuntimeCitySpawnerSystemConfig configAsset,
        RoadBuildSystem roadBuildController,
        BuildingRuntimeCitySpawnSystem buildingRuntimeCitySpawnSystem,
        BuildingRuntimeCitySpawnSystem.Context buildingRuntimeCitySpawnContext,
        Transform runtimeRoot,
        MainMenuPlayUI mainMenuPlayUi = null)
    {
        config = configAsset;
        _runtimeCityRoadBuildBridgeSystem.Configure(roadBuildController);
        _runtimeCitySpawnBridgeSystem.Configure(buildingRuntimeCitySpawnSystem, buildingRuntimeCitySpawnContext);
        _runtimeRoot = runtimeRoot;
        _runtimeCityVisualSystem.SetRuntimeRoot(runtimeRoot);
        _mainMenuPlayUi = mainMenuPlayUi;
        ApplyConfigIfAvailable();
    }

    public void InitForRoadOnly(
        RuntimeCitySpawnerSystemConfig configAsset,
        RoadBuildSystem roadBuildController,
        Transform runtimeRoot,
        MainMenuPlayUI mainMenuPlayUi = null)
    {
        Init(configAsset, roadBuildController, null, default, runtimeRoot, mainMenuPlayUi);
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
        _generationRoutine = null;
        _runtimeCityVisualSystem.Dispose();
        _runtimeCitySpawnBridgeSystem.Clear();
        _runtimeCityRoadBuildBridgeSystem.Clear();
        _runtimeRoot = null;
    }

    public bool IsConfiguredHousePrefab(GameObject prefab)
    {
        return _runtimeCityPrefabSelectionSystem.IsConfiguredPrefab(prefab, housePrefabs);
    }

    private void ApplyConfigIfAvailable()
    {
        _runtimeCityConfigSystem.Apply(config);
        _runtimeCityBuildingSpawnSystem.Configure(
            cityConfig,
            _runtimeCityBuildingPlotSystem,
            _runtimeCityWalkabilitySystem,
            _runtimeCityPrefabSelectionSystem,
            _runtimeCityVisualSystem,
            _runtimeCitySpawnBridgeSystem);
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
        if (!_runtimeGameplayStateSystem.PlayRequested)
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

        if (!_runtimeCityRoadBuildBridgeSystem.HasRoadBuildSystem)
            return;
        if (generateBuildings && !_runtimeCitySpawnBridgeSystem.HasSpawnSystem)
            return;
        if (!_runtimeCityRoadBuildBridgeSystem.TryGetRoadCellSizeInGridCells(out int roadCellSizeInGridCells))
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

        if (!_runtimeCityRoadBuildBridgeSystem.HasRoadBuildSystem)
            return;
        if (generateBuildings && !_runtimeCitySpawnBridgeSystem.HasSpawnSystem)
            return;
        if (!_runtimeCityRoadBuildBridgeSystem.TryGetRoadCellSizeInGridCells(out int roadCellSizeInGridCells))
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

        uint generationSeed = randomSeed == 0 ? 1 : randomSeed;
        var rng = new Unity.Mathematics.Random(generationSeed);
        int townRadius = _runtimeCityLayoutSystem.CalculateTownRadius(cityConfig);
        List<RectInt> baseExclusionRoadRects = CollectInitialBaseExclusionRoadRects(roadCellSizeInGridCells);
        var cities = new List<CityLayoutData>(Mathf.Max(0, cityCount));
        var occupiedRoadCells = new HashSet<Vector2Int>();
        _runtimeCityRoadBuildBridgeSystem.BeginDeferredRoadEcsSync();
        if (generateBuildings)
            _runtimeCitySpawnBridgeSystem.BeginDeferredSideEffects();

        try
        {
            Vector2Int firstCenter = _runtimeCityLayoutSystem.ClampRoadCellToBuildableArea(
                startCell / roadCellSizeInGridCells,
                grid,
                roadCellSizeInGridCells,
                townRadius,
                hallPlazaRadiusRoadCells);
            firstCenter = _runtimeCityLayoutSystem.FindNearestRoadCellOutsideBaseExclusions(
                firstCenter,
                baseExclusionRoadRects,
                grid,
                roadCellSizeInGridCells,
                townRadius,
                hallPlazaRadiusRoadCells);
            CityLayoutData currentCity = CreateCityLayout(firstCenter, townRadius, null, default, ref rng);
            CommitCityRoadNetwork(currentCity, occupiedRoadCells);
            if (generateBuildings)
            {
                _runtimeCityBuildingSpawnSystem.EnsureCityHall(currentCity, roadCellSizeInGridCells, ref rng);
                _runtimeCityBuildingSpawnSystem.SpawnCityImportantBuildings(currentCity, roadCellSizeInGridCells, ref rng);
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

                if (!_runtimeCityRoadBuildBridgeSystem.CreateRoadStrokeFromRoadCells(sourceExitRoad))
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

                if (!_runtimeCityRoadBuildBridgeSystem.CreateAutobahnStrokeFromRoadCells(extendedAutobahnPath, true, true))
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
                if (!_runtimeCityRoadBuildBridgeSystem.CreateStandaloneStraightRoadChainFromConnector(
                        endConnectorCell,
                        travelDirection,
                        debugStraightRoadLength))
                {
                    yield return null;
                    break;
                }

                if (!_runtimeCityRoadBuildBridgeSystem.TryGetStandaloneStraightChainEndRoadCell(travelDirection, out Vector2Int secondCityAnchorCell))
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

                _runtimeCityWalkabilitySystem.ReserveStandaloneEntranceCorridor(
                    anchoredNextCity,
                    endConnectorCell,
                    travelDirection,
                    debugStraightRoadLength,
                    roadCellSizeInGridCells);
                CommitCityRoadNetwork(anchoredNextCity, occupiedRoadCells);
                if (generateBuildings)
                {
                    _runtimeCityBuildingSpawnSystem.SpawnCityImportantBuildings(anchoredNextCity, roadCellSizeInGridCells, ref rng);
                    _runtimeCityBuildingSpawnSystem.SpawnCorridorEntranceBuildings(
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

            _runtimeCityRoadBuildBridgeSystem.EndDeferredRoadEcsSync();

            for (int i = 0; i < cities.Count; i++)
            {
                if (generateBuildings)
                {
                    var bulkRng = new RuntimeCityBuildingSpawnSystem.GenerationRandomState { Value = rng };
                    IEnumerator bulkRoutine = _runtimeCityBuildingSpawnSystem.SpawnCityBulkBuildingsRoutine(cities[i], grid, roadCellSizeInGridCells, bulkRng);
                    while (bulkRoutine.MoveNext())
                        yield return null;
                    rng = bulkRng.Value;
                }

                if (ShouldYield((cityCount * 3) + i + 1))
                    yield return null;
            }

            if (generateBuildings)
                _runtimeCitySpawnBridgeSystem.EndDeferredSideEffects();

            _mainMenuPlayUi?.NotifyStaticMinimapChanged();
            _spawned = true;
            if (EnableRuntimeCityDiagnostics)
                Debug.Log($"[RuntimeCityState] frame={Time.frameCount} reason=completed cities={cities.Count} ageFrames={Time.frameCount - _generationStartedFrame} steps={_generationMoveNextCount}");
            _generationRoutine = null;
        }
        finally
        {
            if (generateBuildings)
                _runtimeCitySpawnBridgeSystem.EndDeferredSideEffects();
            _runtimeCityRoadBuildBridgeSystem.EndDeferredRoadEcsSync();
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
            RoadStrokes = _runtimeCityRoadLayoutSystem.BuildTownRoadStrokes(centerRoadCell, townRadius, hallPlazaRadiusRoadCells, ref rng)
        };

        if (incomingAnchorCell.HasValue)
        {
            city.HasIncomingAnchor = true;
            city.IncomingAnchorCell = incomingAnchorCell.Value;
            city.IncomingOutwardDirection = incomingOutwardDirection;
            Vector2Int innerConnectionCell = GetCityInnerConnectionCell(centerRoadCell, incomingOutwardDirection);
            _runtimeCityRoadLayoutSystem.AddStroke(city.RoadStrokes, incomingAnchorCell.Value, innerConnectionCell);
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
            _runtimeCityRoadBuildBridgeSystem.CreateRoadStrokeFromRoadCells(stroke);
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
        _runtimeCityPrefabSelectionSystem.Shuffle(directions, ref rng);

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
        _runtimeCityLayoutSystem.GetRoadGridBounds(
            grid,
            roadCellSizeInGridCells,
            townRadius,
            hallPlazaRadiusRoadCells,
            out int minRoadX,
            out int maxRoadX,
            out int minRoadY,
            out int maxRoadY);

        for (int dirIndex = 0; dirIndex < directions.Count; dirIndex++)
        {
            Vector2Int direction = directions[dirIndex];
            Vector2Int sourceInnerConnection = GetCityInnerConnectionCell(currentCity.CenterRoadCell, direction);
            Vector2Int targetCenter = currentCity.CenterRoadCell + direction * (autobahnLength + cityConnectionOffset * 2);

            if (!RuntimeCityLayoutSystem.IsRoadCellWithinBounds(sourceInnerConnection, minRoadX, maxRoadX, minRoadY, maxRoadY) ||
                !RuntimeCityLayoutSystem.IsRoadCellWithinBounds(targetCenter, minRoadX, maxRoadX, minRoadY, maxRoadY))
            {
                continue;
            }

            if (!_runtimeCityLayoutSystem.IsCityCenterFarEnough(targetCenter, existingCities, townRadius, baseExclusionRoadRects, cityConfig))
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

            if (!RuntimeCityLayoutSystem.IsRoadCellWithinBounds(sourceConnectionCell, minRoadX, maxRoadX, minRoadY, maxRoadY) ||
                !RuntimeCityLayoutSystem.IsRoadCellWithinBounds(targetConnectionCell, minRoadX, maxRoadX, minRoadY, maxRoadY))
            {
                continue;
            }

            List<Vector2Int> candidateExitRoad = _runtimeCityRoadLayoutSystem.BuildStraightRoadPath(sourceInnerConnection, sourceConnectionCell);
            if (candidateExitRoad.Count < 2)
            {
                continue;
            }
            if (!IsCityExitPathValid(candidateExitRoad, occupiedRoadCells, currentCity))
            {
                continue;
            }

            List<Vector2Int> candidatePath = _runtimeCityRoadLayoutSystem.BuildStraightRoadPath(sourceConnectionCell, targetConnectionCell);
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

    private void ConnectCitiesWithAutobahn(CityLayoutData fromCity, CityLayoutData toCity, CityChainAxis chainAxis)
    {
        List<Vector2Int> autobahnPath = _runtimeCityRoadLayoutSystem.BuildCityToCityAutobahnPath(fromCity, toCity, chainAxis);
        if (autobahnPath.Count < 3)
            return;
        if (!_runtimeCityRoadBuildBridgeSystem.CreateAutobahnStrokeFromRoadCells(autobahnPath, true, true))
            return;

        for (int cellIndex = 0; cellIndex < autobahnPath.Count; cellIndex++)
        {
            Vector2Int cell = autobahnPath[cellIndex];
            fromCity.RoadCells.Add(cell);
            toCity.RoadCells.Add(cell);
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
