using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CityLayoutData = RuntimeCityLayoutSystem.CityLayoutData;

internal sealed class RuntimeCityGenerationSystem
{
    public bool TryBegin(Context context)
    {
        if (context.LifecycleSystem == null)
            return false;
        if (context.LifecycleSystem.IsSpawned || context.LifecycleSystem.IsGenerating)
            return false;
        if (context.CityConfig.CityCount <= 0)
            return false;

        return context.LifecycleSystem.TryBeginGeneration(GenerateCityRoutine(context), context.LifecycleContext);
    }

    private IEnumerator GenerateCityRoutine(Context context)
    {
        RuntimeCityConfigSystem.Snapshot cityConfig = context.CityConfig;
        if (context.LifecycleSystem.IsSpawned)
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
                context.BuildingSpawnSystem.EnsureCityHall(currentCity, context.RoadCellSizeInGridCells, ref rng);
                context.BuildingSpawnSystem.SpawnCityImportantBuildings(currentCity, context.RoadCellSizeInGridCells, ref rng);
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
                    context.BuildingSpawnSystem.SpawnCityImportantBuildings(anchoredNextCity, context.RoadCellSizeInGridCells, ref rng);
                    context.BuildingSpawnSystem.SpawnCorridorEntranceBuildings(
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
                    var bulkRng = new RuntimeCityBuildingSpawnSystem.GenerationRandomState { Value = rng };
                    IEnumerator bulkRoutine = context.BuildingSpawnSystem.SpawnCityBulkBuildingsRoutine(cities[i], context.Grid, context.RoadCellSizeInGridCells, bulkRng);
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
            context.LifecycleSystem.CompleteGeneration(cities.Count, context.LifecycleContext);
        }
        finally
        {
            if (cityConfig.GenerateBuildings)
                context.SpawnBridgeSystem.EndDeferredSideEffects();
            context.RoadBuildBridgeSystem.EndDeferredRoadEcsSync();
        }
    }

    public delegate List<RectInt> CollectInitialBaseExclusionRoadRectsDelegate(int roadCellSizeInGridCells);

    public delegate bool ShouldYieldDelegate(int completedWorkItems);

    public readonly struct Context
    {
        public readonly RuntimeCityConfigSystem.Snapshot CityConfig;
        public readonly GridConfig Grid;
        public readonly int RoadCellSizeInGridCells;
        public readonly RuntimeCityLifecycleSystem LifecycleSystem;
        public readonly RuntimeCityLifecycleSystem.Context LifecycleContext;
        public readonly RuntimeCityLayoutSystem LayoutSystem;
        public readonly RuntimeCityWalkabilitySystem WalkabilitySystem;
        public readonly RuntimeCityBuildingSpawnSystem BuildingSpawnSystem;
        public readonly RuntimeCityRoadBuildBridgeSystem RoadBuildBridgeSystem;
        public readonly RuntimeCitySpawnBridgeSystem SpawnBridgeSystem;
        public readonly RuntimeCityChainSystem ChainSystem;
        public readonly RuntimeCityChainSystem.Context ChainContext;
        public readonly RuntimeCityRoadCommitSystem RoadCommitSystem;
        public readonly RuntimeCityRoadCommitSystem.Context RoadCommitContext;
        public readonly RuntimeCityIngressSystem IngressSystem;
        public readonly RuntimeCityIngressSystem.Context IngressContext;
        public readonly CollectInitialBaseExclusionRoadRectsDelegate CollectInitialBaseExclusionRoadRects;
        public readonly ShouldYieldDelegate ShouldYield;
        public readonly RuntimeCityMinimapEventSystem MinimapEvents;
        public readonly RuntimeCityDiagnosticSystem Diagnostics;

        public Context(
            RuntimeCityConfigSystem.Snapshot cityConfig,
            GridConfig grid,
            int roadCellSizeInGridCells,
            RuntimeCityLifecycleSystem lifecycleSystem,
            RuntimeCityLifecycleSystem.Context lifecycleContext,
            RuntimeCityLayoutSystem layoutSystem,
            RuntimeCityWalkabilitySystem walkabilitySystem,
            RuntimeCityBuildingSpawnSystem buildingSpawnSystem,
            RuntimeCityRoadBuildBridgeSystem roadBuildBridgeSystem,
            RuntimeCitySpawnBridgeSystem spawnBridgeSystem,
            RuntimeCityChainSystem chainSystem,
            RuntimeCityChainSystem.Context chainContext,
            RuntimeCityRoadCommitSystem roadCommitSystem,
            RuntimeCityRoadCommitSystem.Context roadCommitContext,
            RuntimeCityIngressSystem ingressSystem,
            RuntimeCityIngressSystem.Context ingressContext,
            CollectInitialBaseExclusionRoadRectsDelegate collectInitialBaseExclusionRoadRects,
            ShouldYieldDelegate shouldYield,
            RuntimeCityMinimapEventSystem minimapEvents,
            RuntimeCityDiagnosticSystem diagnostics)
        {
            CityConfig = cityConfig;
            Grid = grid;
            RoadCellSizeInGridCells = roadCellSizeInGridCells;
            LifecycleSystem = lifecycleSystem;
            LifecycleContext = lifecycleContext;
            LayoutSystem = layoutSystem;
            WalkabilitySystem = walkabilitySystem;
            BuildingSpawnSystem = buildingSpawnSystem;
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
