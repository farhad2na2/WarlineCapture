using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CityLayoutData = RuntimeCityLayoutSystem.CityLayoutData;

internal sealed class RuntimeCityGenerationSystem
{
    private readonly RuntimeCityGenerationState _state = new();

    public RuntimeCityGenerationState State => _state;

    public bool TryBegin(Context context)
    {
        return _state.TryBegin(context);
    }

    public delegate List<RectInt> CollectInitialBaseExclusionRoadRectsDelegate(int roadCellSizeInGridCells);

    public delegate bool ShouldYieldDelegate(int completedWorkItems);

    public readonly struct Context
    {
        public readonly RuntimeCityConfigSystem.Snapshot CityConfig;
        public readonly GridConfig Grid;
        public readonly int RoadCellSizeInGridCells;
        public readonly RuntimeCityLifecycleState LifecycleState;
        public readonly RuntimeCityLifecycleSystem.Context LifecycleContext;
        public readonly RuntimeCityLayoutState LayoutSystem;
        public readonly RuntimeCityWalkabilityState WalkabilitySystem;
        public readonly RuntimeCityBuildingSpawnContextCompositionSystemHelper.Systems BuildingSpawnSystems;
        public readonly RuntimeCityBuildingSpawnContextCompositionSystemHelper.Context BuildingSpawnContext;
        public readonly RuntimeCityBuildingPlacementState BuildingPlacementSystem;
        public readonly RuntimeCityCorridorBuildingSpawnState CorridorBuildingSpawnSystem;
        public readonly RuntimeCityRoadBuildBridgeState RoadBuildBridgeSystem;
        public readonly RuntimeCitySpawnBridgeState SpawnBridgeSystem;
        public readonly RuntimeCityChainState ChainSystem;
        public readonly RuntimeCityChainUtilitySystemHelper.Context ChainContext;
        public readonly RuntimeCityRoadCommitState RoadCommitSystem;
        public readonly RuntimeCityRoadCommitCompositionSystemHelper.Context RoadCommitContext;
        public readonly RuntimeCityIngressState IngressSystem;
        public readonly RuntimeCityIngressSystem.Context IngressContext;
        public readonly CollectInitialBaseExclusionRoadRectsDelegate CollectInitialBaseExclusionRoadRects;
        public readonly ShouldYieldDelegate ShouldYield;
        public readonly RuntimeCityMinimapEventSystem MinimapEvents;
        public readonly RuntimeCityDiagnosticsSystemHelper Diagnostics;

        public Context(
            RuntimeCityConfigSystem.Snapshot cityConfig,
            GridConfig grid,
            int roadCellSizeInGridCells,
            RuntimeCityLifecycleState lifecycleState,
            RuntimeCityLifecycleSystem.Context lifecycleContext,
            RuntimeCityLayoutState layoutSystem,
            RuntimeCityWalkabilityState walkabilitySystem,
            RuntimeCityBuildingSpawnContextCompositionSystemHelper.Systems buildingSpawnSystems,
            RuntimeCityBuildingSpawnContextCompositionSystemHelper.Context buildingSpawnContext,
            RuntimeCityBuildingPlacementState buildingPlacementSystem,
            RuntimeCityCorridorBuildingSpawnState corridorBuildingSpawnSystem,
            RuntimeCityRoadBuildBridgeState roadBuildBridgeSystem,
            RuntimeCitySpawnBridgeState spawnBridgeSystem,
            RuntimeCityChainState chainSystem,
            RuntimeCityChainUtilitySystemHelper.Context chainContext,
            RuntimeCityRoadCommitState roadCommitSystem,
            RuntimeCityRoadCommitCompositionSystemHelper.Context roadCommitContext,
            RuntimeCityIngressState ingressSystem,
            RuntimeCityIngressSystem.Context ingressContext,
            CollectInitialBaseExclusionRoadRectsDelegate collectInitialBaseExclusionRoadRects,
            ShouldYieldDelegate shouldYield,
            RuntimeCityMinimapEventSystem minimapEvents,
            RuntimeCityDiagnosticsSystemHelper diagnostics)
        {
            CityConfig = cityConfig;
            Grid = grid;
            RoadCellSizeInGridCells = roadCellSizeInGridCells;
            LifecycleState = lifecycleState;
            LifecycleContext = lifecycleContext;
            LayoutSystem = layoutSystem;
            WalkabilitySystem = walkabilitySystem;
            BuildingSpawnSystems = buildingSpawnSystems;
            BuildingSpawnContext = buildingSpawnContext;
            BuildingPlacementSystem = buildingPlacementSystem;
            CorridorBuildingSpawnSystem = corridorBuildingSpawnSystem;
            RoadBuildBridgeSystem = roadBuildBridgeSystem;
            SpawnBridgeSystem = spawnBridgeSystem;
            ChainSystem = chainSystem;
            ChainContext = chainContext;
            RoadCommitSystem = roadCommitSystem;
            RoadCommitContext = roadCommitContext;
            IngressSystem = ingressSystem;
            IngressContext = ingressContext;
            CollectInitialBaseExclusionRoadRects = collectInitialBaseExclusionRoadRects;
            ShouldYield = shouldYield;
            MinimapEvents = minimapEvents;
            Diagnostics = diagnostics;
        }
    }
}

internal sealed class RuntimeCityGenerationState
{
    public bool TryBegin(RuntimeCityGenerationSystem.Context context)
    {
        if (context.LifecycleState == null)
            return false;
        if (context.LifecycleState.IsSpawned || context.LifecycleState.IsGenerating)
            return false;
        if (context.CityConfig.CityCount <= 0)
            return false;

        return context.LifecycleState.TryBeginGeneration(GenerateCityRoutine(context), context.LifecycleContext);
    }

    private IEnumerator GenerateCityRoutine(RuntimeCityGenerationSystem.Context context)
    {
        RuntimeCityConfigSystem.Snapshot cityConfig = context.CityConfig;
        if (context.LifecycleState.IsSpawned)
            yield break;

        if (cityConfig.CityCount <= 0)
            yield break;

        uint generationSeed = cityConfig.RandomSeed == 0 ? 1 : cityConfig.RandomSeed;
        var rng = new Unity.Mathematics.Random(generationSeed);
        int townRadius = context.LayoutSystem.CalculateTownRadius(cityConfig);
        List<RectInt> baseExclusionRoadRects = context.CollectInitialBaseExclusionRoadRects?.Invoke(context.RoadCellSizeInGridCells) ?? new List<RectInt>();
        var cities = new List<CityLayoutData>(Mathf.Max(0, cityConfig.CityCount));
        var occupiedRoadCells = new HashSet<Vector2Int>();
        context.RoadBuildBridgeSystem.BeginDeferredRoadEcsSync();
        if (cityConfig.GenerateBuildings)
            context.SpawnBridgeSystem.BeginDeferredSideEffects();

        try
        {
            Vector2Int firstCenter = context.LayoutSystem.ClampRoadCellToBuildableArea(
                cityConfig.StartCell / context.RoadCellSizeInGridCells,
                context.Grid,
                context.RoadCellSizeInGridCells,
                townRadius,
                cityConfig.HallPlazaRadiusRoadCells);
            firstCenter = context.LayoutSystem.FindNearestRoadCellOutsideBaseExclusions(
                firstCenter,
                baseExclusionRoadRects,
                context.Grid,
                context.RoadCellSizeInGridCells,
                townRadius,
                cityConfig.HallPlazaRadiusRoadCells);
            CityLayoutData currentCity = context.IngressSystem.CreateCityLayout(context.IngressContext, firstCenter, townRadius, null, default, ref rng);
            context.RoadCommitSystem.CommitCityRoadNetwork(context.RoadCommitContext, currentCity, occupiedRoadCells);
            if (cityConfig.GenerateBuildings)
            {
                EnsureCityHall(context, currentCity, ref rng);
                SpawnCityImportantBuildings(context, currentCity, ref rng);
            }
            cities.Add(currentCity);
            if (context.ShouldYield(1))
                yield return null;

            Vector2Int? previousTravelDirection = null;
            for (int cityIndex = 1; cityIndex < cityConfig.CityCount; cityIndex++)
            {
                if (!context.ChainSystem.TryPlanNextCity(
                        context.ChainContext,
                        cities,
                        occupiedRoadCells,
                        currentCity,
                        previousTravelDirection,
                        context.Grid,
                        context.RoadCellSizeInGridCells,
                        townRadius,
                        baseExclusionRoadRects,
                        ref rng,
                        out List<Vector2Int> sourceExitRoad,
                        out List<Vector2Int> autobahnPath,
                        out Vector2Int travelDirection,
                        out CityLayoutData nextCity))
                {
                    context.Diagnostics?.LogCityPlanningFailed(cityIndex + 1, cities.Count);
                    break;
                }

                if (!context.RoadCommitSystem.TryCommitSourceExitRoad(
                        context.RoadCommitContext,
                        cityIndex + 1,
                        sourceExitRoad,
                        currentCity,
                        occupiedRoadCells))
                    break;

                if (context.ShouldYield(cityIndex * 3 - 1))
                    yield return null;

                if (!context.RoadCommitSystem.TryCommitAutobahn(
                        context.RoadCommitContext,
                        cityIndex + 1,
                        autobahnPath,
                        travelDirection,
                        currentCity,
                        occupiedRoadCells,
                        out _,
                        out Vector2Int endConnectorCell))
                    break;

                const int debugStraightRoadLength = 9;
                if (!context.RoadCommitSystem.TryCreateStandaloneConnector(
                        context.RoadCommitContext,
                        endConnectorCell,
                        travelDirection,
                        debugStraightRoadLength,
                        out Vector2Int secondCityAnchorCell))
                {
                    yield return null;
                    break;
                }

                Vector2Int secondCityOutwardDirection = -travelDirection;
                CityLayoutData anchoredNextCity = context.IngressSystem.CreateCityLayout(
                    context.IngressContext,
                    nextCity.CenterRoadCell,
                    townRadius,
                    secondCityAnchorCell,
                    secondCityOutwardDirection,
                    ref rng);

                context.WalkabilitySystem.ReserveStandaloneEntranceCorridor(
                    anchoredNextCity,
                    endConnectorCell,
                    travelDirection,
                    debugStraightRoadLength,
                    context.RoadCellSizeInGridCells);
                context.RoadCommitSystem.CommitCityRoadNetwork(context.RoadCommitContext, anchoredNextCity, occupiedRoadCells);
                if (cityConfig.GenerateBuildings)
                {
                    SpawnCityImportantBuildings(context, anchoredNextCity, ref rng);
                    context.CorridorBuildingSpawnSystem.SpawnCorridorEntranceBuildings(
                        context.BuildingSpawnContext,
                        context.BuildingPlacementSystem,
                        anchoredNextCity,
                        endConnectorCell,
                        travelDirection,
                        debugStraightRoadLength,
                        context.RoadCellSizeInGridCells,
                        ref rng);
                }
                cities.Add(anchoredNextCity);
                currentCity = anchoredNextCity;
                previousTravelDirection = travelDirection;

                if (context.ShouldYield(cityIndex * 3))
                    yield return null;
            }

            context.RoadBuildBridgeSystem.EndDeferredRoadEcsSync();

            for (int i = 0; i < cities.Count; i++)
            {
                if (cityConfig.GenerateBuildings)
                {
                    var bulkRng = new RuntimeCityBulkBuildingSpawnRoutinePrefabSystemHelper.GenerationRandomState { Value = rng };
                    IEnumerator bulkRoutine = SpawnCityBulkBuildingsRoutine(context, cities[i], bulkRng);
                    while (bulkRoutine.MoveNext())
                        yield return null;
                    rng = bulkRng.Value;
                }

                if (context.ShouldYield((cityConfig.CityCount * 3) + i + 1))
                    yield return null;
            }

            if (cityConfig.GenerateBuildings)
                context.SpawnBridgeSystem.EndDeferredSideEffects();

            context.MinimapEvents?.PublishStaticMinimapChanged();
            context.LifecycleState.CompleteGeneration(cities.Count, context.LifecycleContext);
        }
        finally
        {
            if (cityConfig.GenerateBuildings)
                context.SpawnBridgeSystem.EndDeferredSideEffects();
            context.RoadBuildBridgeSystem.EndDeferredRoadEcsSync();
        }
    }

    private static void EnsureCityHall(RuntimeCityGenerationSystem.Context context, CityLayoutData city, ref Unity.Mathematics.Random rng)
    {
        context.BuildingSpawnSystems.HallSpawnSystem.EnsureCityHall(
            context.BuildingSpawnContext,
            context.BuildingSpawnSystems.PlacementSystem,
            context.BuildingSpawnSystems.LandmarkOffsetSystem,
            city,
            context.RoadCellSizeInGridCells,
            ref rng);
    }

    private static void SpawnCityImportantBuildings(RuntimeCityGenerationSystem.Context context, CityLayoutData city, ref Unity.Mathematics.Random rng)
    {
        context.BuildingSpawnSystems.HallSpawnSystem.EnsureCityHall(
            context.BuildingSpawnContext,
            context.BuildingSpawnSystems.PlacementSystem,
            context.BuildingSpawnSystems.LandmarkOffsetSystem,
            city,
            context.RoadCellSizeInGridCells,
            ref rng);
        context.BuildingSpawnSystems.LandmarkSpawnSystem.SpawnLandmarks(
            context.BuildingSpawnContext,
            context.BuildingSpawnSystems.PlacementSystem,
            context.BuildingSpawnSystems.LandmarkOffsetSystem,
            city.CenterRoadCell,
            context.RoadCellSizeInGridCells,
            city.RoadCells,
            ref rng,
            city.ReservedFootprints);
    }

    private static IEnumerator SpawnCityBulkBuildingsRoutine(RuntimeCityGenerationSystem.Context context, CityLayoutData city, RuntimeCityBulkBuildingSpawnRoutinePrefabSystemHelper.GenerationRandomState rng)
    {
        return context.BuildingSpawnSystems.BulkBuildingSpawnRoutineSystem.SpawnRoutine(
            context.BuildingSpawnContext,
            context.BuildingSpawnSystems.PlacementSystem,
            context.BuildingSpawnSystems.BulkPlotPlanSystem,
            context.BuildingSpawnSystems.EntryBuildingSpawnSystem,
            context.BuildingSpawnSystems.RoadsideBuildingSpawnSystem,
            context.BuildingSpawnSystems.RuralBuildingSpawnSystem,
            city,
            context.Grid,
            context.RoadCellSizeInGridCells,
            rng,
            (List<RectInt> houseFootprints, Vector2Int centerRoadCell, int callbackRoadCellSizeInGridCells, HashSet<Vector2Int> roadCells, GridConfig callbackGrid, ref Unity.Mathematics.Random callbackRng, List<RuntimeCityWalkabilitySystem.ReservedFootprint> reservedFootprints) =>
                context.BuildingSpawnSystems.HouseYardWallSystem.PlaceHouseYardWalls(
                    context.BuildingSpawnContext,
                    context.BuildingSpawnSystems.PlacementSystem,
                    context.BuildingSpawnContext.PrefabSelectionSystem,
                    context.BuildingSpawnContext.WalkabilitySystem,
                    context.BuildingSpawnSystems.YardWallPlanSystem,
                    context.BuildingSpawnSystems.YardGateSystem,
                    context.BuildingSpawnSystems.YardWallVisualSystem,
                    context.BuildingSpawnContext.VisualSystem,
                    context.BuildingSpawnContext.Config.HouseWallPrefabs,
                    context.BuildingSpawnContext.Config.HouseWallGatePrefab,
                    context.BuildingSpawnContext.Config.HouseWallPillarPrefab,
                    context.BuildingSpawnContext.Config.HouseWallChance,
                    context.BuildingSpawnContext.Config.HouseWallMinDistanceCells,
                    context.BuildingSpawnContext.Config.HouseWallMaxDistanceCells,
                    houseFootprints,
                    centerRoadCell,
                    callbackRoadCellSizeInGridCells,
                    roadCells,
                    callbackGrid,
                    ref callbackRng,
                    reservedFootprints),
            (RuntimeCityBuildingSpawnContextCompositionSystemHelper.Context callbackContext, List<GameObject> prefabs, int count, Vector2Int centerRoadCell, int townRadius, int callbackRoadCellSizeInGridCells, HashSet<Vector2Int> roadCells, ref Unity.Mathematics.Random callbackRng, List<Vector2Int> usedPlotCells, List<RuntimeCityWalkabilitySystem.ReservedFootprint> reservedFootprints, List<RectInt> shopAndHouseFootprints) =>
                context.BuildingSpawnSystems.DecorationBuildingSpawnSystem.PlaceCityDecorationBuildings(
                    callbackContext,
                    context.BuildingSpawnSystems.PlacementSystem,
                    context.BuildingSpawnSystems.DecorationPrefabGroupSystem,
                    context.BuildingSpawnSystems.ClothCoverSpawnSystem,
                    context.BuildingSpawnSystems.ArchwaySpawnSystem,
                    context.BuildingSpawnSystems.FreeScatterDecorationSystem,
                    prefabs,
                    count,
                    centerRoadCell,
                    townRadius,
                    callbackRoadCellSizeInGridCells,
                    roadCells,
                    ref callbackRng,
                    usedPlotCells,
                    reservedFootprints,
                    shopAndHouseFootprints));
    }

}
