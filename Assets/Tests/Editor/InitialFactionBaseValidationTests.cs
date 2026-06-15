using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

public sealed class InitialFactionBaseValidationTests
{
    private const string SceneConfigPath = "Assets/Game/Configs/Scene/MatchSubScene_InitialUnitsSpawner_Config.asset";
    private const string RuntimeCityConfigPath = "Assets/Game/Configs/Scene/Game_RuntimeCitySpawner_Config.asset";
    private const string BuildingPlacementConfigPath = "Assets/Game/Configs/Scene/Game_BuildingPlacement_Config.asset";
    private static readonly string[] RequiredInitialGroundVehiclePrefabs =
    {
        "Unit_Veh_APC_Fast",
        "Unit_Veh_APC_Heavy",
        "Unit_Veh_APC_Slow",
        "Unit_Veh_Light_Armored_Car",
        "Unit_Veh_Missle_Launcher_Air",
        "Unit_Veh_Missle_Launcher_Ground",
        "Unit_Veh_Radar_Tank",
        "Unit_Veh_Tank_USA",
        "Unit_Veh_Truck_Canopy",
        "Unit_Veh_Truck_Tanker",
        "Unit_Veh_Truck_Tray"
    };

    public static void RunBatchValidation()
    {
        try
        {
            var tests = new InitialFactionBaseValidationTests();
            tests.InitialFactionBaseLayoutPlanner_BuildsExactBaseRecipe();
            tests.SceneInitialUnitsConfig_DisablesAutomaticFactionBasesAndKeepsConfiguredStarts();
            tests.BuildingPlacementConfig_ResolvesEveryInitialBasePrefab();
            tests.InitialBaseAirPlatformPrefabs_HaveProductionSpawnPoints();
            tests.RuntimeHelipad_DoesNotCreateStaticPathBlocker();
            tests.InitialBaseRuntimePlacement_SpawnsRequiredBaseBuildings();
            tests.HelipadSpawnResolver_SkipsOccupiedPadForInitialTransportHelicopter();
            Debug.Log("[InitialFactionBaseValidation] result=Passed");
            EditorApplication.Exit(0);
        }
        catch (System.Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError("[InitialFactionBaseValidation] result=Failed");
            EditorApplication.Exit(1);
        }
    }

    public static void RunSceneInitialUnitsConfigValidation()
    {
        try
        {
            var tests = new InitialFactionBaseValidationTests();
            tests.SceneInitialUnitsConfig_DisablesAutomaticFactionBasesAndKeepsConfiguredStarts();
            Debug.Log("[InitialFactionSceneConfigValidation] result=Passed");
            EditorApplication.Exit(0);
        }
        catch (System.Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError("[InitialFactionSceneConfigValidation] result=Failed");
            EditorApplication.Exit(1);
        }
    }

    public static void RunBuildingGameplayRuntimeSmokeValidation()
    {
        try
        {
            var tests = new InitialFactionBaseValidationTests();
            tests.BuildingPlacementConfig_ResolvesEveryInitialBasePrefab();
            tests.InitialBaseAirPlatformPrefabs_HaveProductionSpawnPoints();
            tests.RuntimeHelipad_DoesNotCreateStaticPathBlocker();
            tests.InitialBaseRuntimePlacement_SpawnsRequiredBaseBuildings();
            tests.HelipadSpawnResolver_SkipsOccupiedPadForInitialTransportHelicopter();
            Debug.Log("[InitialFactionBaseBuildingGameplaySmokeValidation] result=Passed");
            EditorApplication.Exit(0);
        }
        catch (System.Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError("[InitialFactionBaseBuildingGameplaySmokeValidation] result=Failed");
            EditorApplication.Exit(1);
        }
    }

    [Test]
    public void InitialFactionBaseLayoutPlanner_BuildsExactBaseRecipe()
    {
        var placements = new List<InitialFactionBasePlacement>();
        var wallRuns = new List<InitialFactionBaseWallRun>();

        InitialFactionBaseLayoutPlanner.BuildPlacements(
            halfWidthCells: 120,
            halfHeightCells: 80,
            placements);
        InitialFactionBaseLayoutPlanner.BuildWallRuns(120, 80, wallRuns);

        Assert.AreEqual(6, wallRuns.Count);
        Assert.AreEqual(2, CountKind(placements, InitialFactionBasePlacementKind.Gate));
        Assert.AreEqual(1, CountKind(placements, InitialFactionBasePlacementKind.CoreBuilding));
        Assert.AreEqual(15, CountKind(placements, InitialFactionBasePlacementKind.Tent));
        Assert.AreEqual(21, CountKind(placements, InitialFactionBasePlacementKind.SupportBuilding));
        Assert.AreEqual(2, CountPrefab(placements, "Building_Road_Barrier"));
        Assert.AreEqual(2, CountPrefab(placements, "Building_GuardTower"));
        Assert.AreEqual(4, CountPrefab(placements, "Building_GuardTower_Big"));
        Assert.AreEqual(4, CountPrefab(placements, "Building_OilPump"));
        Assert.AreEqual(1, CountPrefab(placements, "Building_Refinery"));
        Assert.AreEqual(0, CountPrefab(placements, "Building_Refinery_Big"));
        Assert.AreEqual(1, CountPrefab(placements, "Building_Satelite_Dish"));
        Assert.AreEqual(3, CountPrefab(placements, "Building_WaterTank"));
        Assert.AreEqual(1, CountPrefab(placements, "Building_Airport"));
        Assert.AreEqual(3, CountPrefab(placements, "Building_Helipad"));
        Assert.AreEqual(1, CountPrefab(placements, "Building_Ammunition_Depot"));
        Assert.AreEqual(1, CountPrefab(placements, "Building_Barrack"));
        Assert.AreEqual(1, CountPrefab(placements, "Building_Fuel_Bladder"));
        CollectionAssert.IsSubsetOf(
            InitialFactionBaseLayoutPlanner.RequiredBuildingKeys,
            CollectPrefabKeys(placements));
        CollectionAssert.IsSubsetOf(
            InitialFactionBaseLayoutPlanner.TentKeys,
            CollectPrefabKeys(placements));
        Assert.IsTrue(placements.Exists(p => p.PrefabKey == "Building_Airport"));
        Assert.IsTrue(placements.Exists(p => p.PrefabKey == "Building_Ammunition_Depot" && p.Kind == InitialFactionBasePlacementKind.CoreBuilding));

        for (int i = 0; i < placements.Count; i++)
        {
            InitialFactionBasePlacement placement = placements[i];
            if (placement.Kind == InitialFactionBasePlacementKind.Gate ||
                placement.PrefabKey == "Building_OilPump" ||
                placement.PrefabKey == "Building_Refinery")
                continue;

            Assert.IsTrue(
                InitialFactionBaseLayoutPlanner.IsInsideInterior(placement.Offset, 120, 80),
                $"{placement.PrefabKey} should be inside the base walls at offset {placement.Offset}.");
        }

        for (int i = 0; i < placements.Count; i++)
        {
            InitialFactionBasePlacement placement = placements[i];
            if (placement.PrefabKey != "Building_OilPump" &&
                placement.PrefabKey != "Building_Refinery")
                continue;

            Assert.IsTrue(
                InitialFactionBaseLayoutPlanner.IsOutsideBase(placement.Offset, 120, 80),
                $"{placement.PrefabKey} should be outside the base walls at offset {placement.Offset}.");
        }

        InitialFactionBasePlacement airport = placements.Find(p => p.PrefabKey == "Building_Airport");
        Assert.AreEqual(new Vector2Int(8, 18), airport.Offset, "Airport should be near the base center so the full runway footprint fits inside the walls.");
        for (int i = 0; i < placements.Count; i++)
        {
            InitialFactionBasePlacement placement = placements[i];
            if (placement.PrefabKey == "Building_Airport" ||
                placement.Kind == InitialFactionBasePlacementKind.Gate ||
                placement.PrefabKey == "Building_OilPump" ||
                placement.PrefabKey == "Building_Refinery")
                continue;

            float distance = Vector2Int.Distance(airport.Offset, placement.Offset);
            Assert.GreaterOrEqual(distance, 38f, $"{placement.PrefabKey} is too close to the airport/runway.");
        }
    }

    [Test]
    public void SceneInitialUnitsConfig_DisablesAutomaticFactionBasesAndKeepsConfiguredStarts()
    {
        InitialUnitsSpawnerAuthoringConfig config =
            AssetDatabase.LoadAssetAtPath<InitialUnitsSpawnerAuthoringConfig>(SceneConfigPath);

        Assert.NotNull(config);
        Assert.IsFalse(config.CreateFactionBases);
        Assert.GreaterOrEqual(config.Factions.Count, 2);
        Assert.IsTrue(
            config.Factions.Exists(faction => faction != null && faction.FactionId == FactionIdentity.PlayerFactionId),
            "Initial match config should include the player faction.");

        BuildingPlacementSystemConfig placementConfig =
            AssetDatabase.LoadAssetAtPath<BuildingPlacementSystemConfig>(BuildingPlacementConfigPath);
        Assert.NotNull(placementConfig);
        Assert.NotNull(placementConfig.UnitPrefabRegistryConfig);
        var registeredUnitPrefabs = new HashSet<GameObject>(placementConfig.UnitPrefabRegistryConfig.UnitSpawnPrefabs);
        var registeredBuildingPrefabs = new HashSet<GameObject>(placementConfig.Spawnables);

        for (int i = 0; i < config.Factions.Count; i++)
        {
            InitialUnitsSpawnerAuthoringConfig.FactionEntry faction = config.Factions[i];
            Assert.NotNull(faction);
            Assert.IsNotEmpty(faction.Units, $"Faction {faction.FactionId} needs at least one configured unit prefab.");
            int totalUnits = 0;
            for (int unitIndex = 0; unitIndex < faction.Units.Count; unitIndex++)
            {
                InitialUnitsSpawnerAuthoringConfig.FactionUnitEntry unit = faction.Units[unitIndex];
                Assert.NotNull(unit);
                Assert.NotNull(unit.Prefab, $"Faction {faction.FactionId} unit {unitIndex} needs a prefab.");
                Assert.IsTrue(
                    registeredUnitPrefabs.Contains(unit.Prefab),
                    $"Faction {faction.FactionId} unit prefab {unit.Prefab.name} must be present in UnitPrefabRegistryConfig.");
                totalUnits += unit.Count;
            }

            Assert.Greater(totalUnits, 0);
            if (faction.Buildings == null)
                continue;

            for (int buildingIndex = 0; buildingIndex < faction.Buildings.Count; buildingIndex++)
            {
                InitialUnitsSpawnerAuthoringConfig.FactionBuildingEntry building = faction.Buildings[buildingIndex];
                Assert.NotNull(building);
                Assert.NotNull(building.Prefab, $"Faction {faction.FactionId} building {buildingIndex} needs a prefab.");
                Assert.IsTrue(
                    registeredBuildingPrefabs.Contains(building.Prefab),
                    $"Faction {faction.FactionId} building prefab {building.Prefab.name} must be present in BuildingPlacementSystemConfig spawnables.");
            }
        }

        InitialUnitsSpawnerAuthoringConfig.FactionEntry faction2 =
            config.Factions.Find(faction => faction != null && faction.FactionId == 2);
        Assert.NotNull(faction2, "Initial match config should include Faction 2.");
        Assert.IsNotEmpty(faction2.Units, "Faction 2 should have configured initial soldiers.");
        Assert.IsNotEmpty(faction2.Buildings, "Faction 2 should have its configured initial building.");

        RuntimeCitySpawnerSystemConfig cityConfig =
            AssetDatabase.LoadAssetAtPath<RuntimeCitySpawnerSystemConfig>(RuntimeCityConfigPath);
        Assert.NotNull(cityConfig);
        for (int i = 0; i < config.Factions.Count; i++)
        {
            InitialUnitsSpawnerAuthoringConfig.FactionEntry faction = config.Factions[i];
            float distance = Vector2Int.Distance(cityConfig.StartCell, faction.SpawnCell);
            Assert.GreaterOrEqual(distance, 300f, $"Runtime city start cell should stay away from faction {faction.FactionId} base.");
        }
    }

    [Test]
    public void BuildingPlacementConfig_ResolvesEveryInitialBasePrefab()
    {
        BuildingPlacementSystemConfig placementConfig =
            AssetDatabase.LoadAssetAtPath<BuildingPlacementSystemConfig>(BuildingPlacementConfigPath);
        Assert.NotNull(placementConfig);

        var plannedKeys = new HashSet<string>(CollectPrefabKeys(BuildPlannedPlacements()));
        plannedKeys.Add("Wall_Dirt_Straight");
        for (int i = 0; i < placementConfig.Spawnables.Count; i++)
        {
            GameObject prefab = placementConfig.Spawnables[i];
            if (prefab == null)
                continue;

            plannedKeys.Remove(prefab.name);
            string assetPath = AssetDatabase.GetAssetPath(prefab);
            if (!string.IsNullOrEmpty(assetPath))
                plannedKeys.Remove(System.IO.Path.GetFileNameWithoutExtension(assetPath));
        }

        Assert.IsEmpty(plannedKeys, "Missing initial base prefab(s) from BuildingPlacementSystemConfig spawnables.");
    }

    [Test]
    public void InitialBaseAirPlatformPrefabs_HaveProductionSpawnPoints()
    {
        GameObject airport = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Buildings/Building_Airport.prefab");
        GameObject helipad = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Buildings/Building_Helipad.prefab");

        Assert.NotNull(airport);
        Assert.NotNull(helipad);
        Assert.GreaterOrEqual(CountProductionSpawnPoints(airport), 3, "Airport needs at least three Spawn_XX points for initial jets and drone.");
        Assert.GreaterOrEqual(CountProductionSpawnPoints(helipad), 1, "Helipad needs a Spawn_XX point for initial helicopters.");
    }

    [Test]
    public void RuntimeHelipad_DoesNotCreateStaticPathBlocker()
    {
        BuildingPlacementSystemConfig placementConfig =
            AssetDatabase.LoadAssetAtPath<BuildingPlacementSystemConfig>(BuildingPlacementConfigPath);
        GameObject helipad = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Buildings/Building_Helipad.prefab");
        Assert.NotNull(placementConfig);
        Assert.NotNull(helipad);

        World previousWorld = World.DefaultGameObjectInjectionWorld;
        World world = new World("RuntimeHelipadPathBlockerValidation");
        World.DefaultGameObjectInjectionWorld = world;
        NativeArray<int> blockerCounts = default;
        NativeBitArray blocked = default;
        NativeBitArray occupied = default;
        NativeArray<byte> friendlyPassFactionIds = default;
        BuildingGameplayCompositionResultSystem.Result buildingGameplay = default;
        bool buildingGameplayInitialized = false;
        GameObject runtimeRoot = null;

        try
        {
            CreateGrid(world.EntityManager, 160, 120, out blockerCounts, out blocked, out occupied, out friendlyPassFactionIds);
            runtimeRoot = new GameObject("RuntimeHelipadPathBlocker_Root");
            buildingGameplay = CreateBuildingGameplay(placementConfig, runtimeRoot.transform);
            buildingGameplayInitialized = true;

            Assert.IsTrue(TrySpawnRuntimeBuilding(
                buildingGameplay,
                helipad,
                new Vector2Int(50, 60),
                out _,
                out _,
                out _,
                ownerFactionId: FactionIdentity.PlayerFactionId));
            using EntityQuery staticBlockers = world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<StaticGridBlocker>());
            Assert.AreEqual(0, staticBlockers.CalculateEntityCount(), "Building_Helipad should not block ground pathing or boarding approach.");
        }
        finally
        {
            if (buildingGameplayInitialized)
                buildingGameplay.Dispose?.Invoke();
            if (runtimeRoot != null)
                Object.DestroyImmediate(runtimeRoot);
            if (world.IsCreated)
                world.Dispose();
            World.DefaultGameObjectInjectionWorld = previousWorld;
            if (blockerCounts.IsCreated)
                blockerCounts.Dispose();
            if (blocked.IsCreated)
                blocked.Dispose();
            if (occupied.IsCreated)
                occupied.Dispose();
            if (friendlyPassFactionIds.IsCreated)
                friendlyPassFactionIds.Dispose();
        }
    }

    [Test]
    public void HelipadSpawnResolver_SkipsOccupiedPadForInitialTransportHelicopter()
    {
        BuildingPlacementSystemConfig placementConfig =
            AssetDatabase.LoadAssetAtPath<BuildingPlacementSystemConfig>(BuildingPlacementConfigPath);
        GameObject helipad = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Buildings/Building_Helipad.prefab");
        GameObject transport = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Vehicles/Unit_Veh_Helicopter_Transport.prefab");
        Assert.NotNull(placementConfig);
        Assert.NotNull(helipad);
        Assert.NotNull(transport);

        World previousWorld = World.DefaultGameObjectInjectionWorld;
        World world = new World("HelipadSpawnResolverValidation");
        World.DefaultGameObjectInjectionWorld = world;
        NativeArray<int> blockerCounts = default;
        NativeBitArray blocked = default;
        NativeBitArray occupied = default;
        NativeArray<byte> friendlyPassFactionIds = default;
        BuildingGameplayCompositionResultSystem.Result buildingGameplay = default;
        bool buildingGameplayInitialized = false;
        GameObject runtimeRoot = null;

        try
        {
            CreateGrid(world.EntityManager, 220, 160, out blockerCounts, out blocked, out occupied, out friendlyPassFactionIds);
            var grid = new GridConfig { Width = 220, Height = 160, CellSize = 1f, Origin = float3.zero };
            runtimeRoot = new GameObject("HelipadSpawnResolver_Root");
            buildingGameplay = CreateBuildingGameplay(placementConfig, runtimeRoot.transform);
            buildingGameplayInitialized = true;

            Assert.IsTrue(TrySpawnRuntimeBuilding(
                buildingGameplay,
                helipad,
                new Vector2Int(50, 60),
                out _,
                out _,
                out _,
                ownerFactionId: FactionIdentity.PlayerFactionId));
            Assert.IsTrue(TrySpawnRuntimeBuilding(
                buildingGameplay,
                helipad,
                new Vector2Int(85, 60),
                out _,
                out _,
                out _,
                ownerFactionId: FactionIdentity.PlayerFactionId));

            int slotsPerHelipad = CountProductionSpawnPoints(helipad);
            Assert.Greater(slotsPerHelipad, 0);
            BuildingSpawnSystem.Context spawnContext = buildingGameplay.CreateSpawnContext();
            Assert.IsTrue(buildingGameplay.Spawn.TryGetFactionProductionSpawnPoint(spawnContext, FactionIdentity.PlayerFactionId, "Building_Helipad", 0, grid, out int2 occupiedCell, out _));
            Assert.IsTrue(buildingGameplay.Spawn.TryGetFactionProductionSpawnPoint(spawnContext, FactionIdentity.PlayerFactionId, "Building_Helipad", slotsPerHelipad, grid, out int2 freeCell, out _));
            for (int slot = 0; slot < slotsPerHelipad; slot++)
            {
                Assert.IsTrue(buildingGameplay.Spawn.TryGetFactionProductionSpawnPoint(spawnContext, FactionIdentity.PlayerFactionId, "Building_Helipad", slot, grid, out int2 slotCell, out _));
                Entity occupyingHelicopter = world.EntityManager.CreateEntity(typeof(UnitGrid), typeof(UnitFootprint), typeof(UnitHealth));
                world.EntityManager.SetComponentData(occupyingHelicopter, new UnitGrid { Cell = slotCell });
                world.EntityManager.SetComponentData(occupyingHelicopter, new UnitFootprint { Size = new int2(3, 3) });
                world.EntityManager.SetComponentData(occupyingHelicopter, new UnitHealth { Current = 100, Max = 100 });
            }

            spawnContext = buildingGameplay.CreateSpawnContext();
            Entity gridEntity = GetGridEntity(world.EntityManager);
            DynamicBlockerComponent blockerData = world.EntityManager.GetComponentData<DynamicBlockerComponent>(gridEntity);
            Assert.IsTrue(
                buildingGameplay.Spawn.TryResolveAvailableFactionHelipadSpawn(
                    spawnContext,
                    FactionIdentity.PlayerFactionId,
                    world.EntityManager,
                    gridEntity,
                    grid,
                    blockerData,
                    new int2(3, 3),
                    out int2 resolvedCell,
                    out _),
                "Transport helicopter spawn should resolve to a usable owned helipad or fallback landing zone.");
            Assert.AreEqual(freeCell, resolvedCell, "The resolver should choose the free helipad before landing beside occupied pads.");
            Assert.AreNotEqual(occupiedCell, resolvedCell, "The resolver should not stack a transport helicopter on an occupied helipad.");
        }
        finally
        {
            if (buildingGameplayInitialized)
                buildingGameplay.Dispose?.Invoke();
            if (runtimeRoot != null)
                Object.DestroyImmediate(runtimeRoot);
            if (world.IsCreated)
                world.Dispose();
            World.DefaultGameObjectInjectionWorld = previousWorld;
            if (blockerCounts.IsCreated)
                blockerCounts.Dispose();
            if (blocked.IsCreated)
                blocked.Dispose();
            if (occupied.IsCreated)
                occupied.Dispose();
            if (friendlyPassFactionIds.IsCreated)
                friendlyPassFactionIds.Dispose();
        }
    }

    [Test]
    public void InitialBaseRuntimePlacement_SpawnsRequiredBaseBuildings()
    {
        InitialUnitsSpawnerAuthoringConfig spawnConfig =
            AssetDatabase.LoadAssetAtPath<InitialUnitsSpawnerAuthoringConfig>(SceneConfigPath);
        BuildingPlacementSystemConfig placementConfig =
            AssetDatabase.LoadAssetAtPath<BuildingPlacementSystemConfig>(BuildingPlacementConfigPath);
        Assert.NotNull(spawnConfig);
        Assert.NotNull(placementConfig);

        World previousWorld = World.DefaultGameObjectInjectionWorld;
        World world = new World("InitialBaseRuntimePlacementValidation");
        World.DefaultGameObjectInjectionWorld = world;
        NativeArray<int> blockerCounts = default;
        NativeBitArray blocked = default;
        NativeBitArray occupied = default;
        NativeArray<byte> friendlyPassFactionIds = default;
        BuildingGameplayCompositionResultSystem.Result buildingGameplay = default;
        bool buildingGameplayInitialized = false;
        GameObject runtimeRoot = null;

        try
        {
            ResolveInitialBaseValidationGridSize(spawnConfig, out int gridWidth, out int gridHeight);
            CreateGrid(world.EntityManager, gridWidth, gridHeight, out blockerCounts, out blocked, out occupied, out friendlyPassFactionIds);
            runtimeRoot = new GameObject("InitialBaseRuntimePlacement_Root");
            buildingGameplay = CreateBuildingGameplay(placementConfig, runtimeRoot.transform);
            buildingGameplayInitialized = true;
            void TickBuildingRuntime() => buildingGameplay.RuntimeUpdate.Update(buildingGameplay.RuntimeUpdateContext);

            var placements = new List<InitialFactionBasePlacement>();
            InitialFactionBaseLayoutPlanner.BuildPlacements(spawnConfig.BaseHalfWidthCells, spawnConfig.BaseHalfHeightCells, placements);
            Assert.IsTrue(TryGetRuntimeBuildingPlacementFootprint(buildingGameplay, spawnConfig.BaseGatePrefab, false, out Vector2Int bottomGateFootprint));
            Assert.IsTrue(TryGetRuntimeBuildingPlacementFootprint(buildingGameplay, spawnConfig.BaseGatePrefab, true, out Vector2Int sideGateFootprint));
            Assert.IsTrue(TryGetRuntimeWallSegmentFootprint(buildingGameplay, spawnConfig.BaseWallPrefab, false, out Vector2Int bottomWallFootprint));
            Assert.IsTrue(TryGetRuntimeWallSegmentFootprint(buildingGameplay, spawnConfig.BaseWallPrefab, true, out Vector2Int sideWallFootprint));
            int gateHalfGap = InitialFactionBaseLayoutPlanner.CalculateGateHalfGap(bottomGateFootprint, sideGateFootprint, bottomWallFootprint, sideWallFootprint);
            var wallRuns = new List<InitialFactionBaseWallRun>();
            InitialFactionBaseLayoutPlanner.BuildWallRuns(spawnConfig.BaseHalfWidthCells, spawnConfig.BaseHalfHeightCells, gateHalfGap, wallRuns);
            var gateFlankWalls = new List<InitialFactionBaseGateFlankWall>();
            InitialFactionBaseLayoutPlanner.BuildGateFlankWalls(
                spawnConfig.BaseHalfWidthCells,
                spawnConfig.BaseHalfHeightCells,
                bottomGateFootprint,
                sideGateFootprint,
                bottomWallFootprint,
                sideWallFootprint,
                gateFlankWalls);
            Assert.AreEqual(4, gateFlankWalls.Count, "Each base should add two flanking wall segments for each gate.");

            for (int factionIndex = 0; factionIndex < spawnConfig.Factions.Count; factionIndex++)
            {
                InitialUnitsSpawnerAuthoringConfig.FactionEntry faction = spawnConfig.Factions[factionIndex];
                Vector2Int anchor = faction.SpawnCell;
                for (int wallRunIndex = 0; wallRunIndex < wallRuns.Count; wallRunIndex++)
                {
                    InitialFactionBaseWallRun run = wallRuns[wallRunIndex];
                    int wallSegments = TrySpawnRuntimeWallRun(
                        buildingGameplay,
                        spawnConfig.BaseWallPrefab,
                        anchor + run.StartOffset,
                        anchor + run.EndOffset,
                        (byte)faction.FactionId);
                    Assert.Greater(wallSegments, 0, $"Faction {faction.FactionId} wall run {wallRunIndex} should spawn at anchor {anchor}.");
                }
                for (int flankIndex = 0; flankIndex < gateFlankWalls.Count; flankIndex++)
                {
                    InitialFactionBaseGateFlankWall flank = gateFlankWalls[flankIndex];
                    Assert.IsTrue(
                        TrySpawnRuntimeWallSegment(
                            buildingGameplay,
                            spawnConfig.BaseWallPrefab,
                            anchor + flank.OriginOffset,
                            flank.RotateVertical,
                            (byte)faction.FactionId,
                            allowExistingWallOverlap: true),
                        $"Faction {faction.FactionId} gate flank wall {flankIndex} should close the gate gap.");
                }

                for (int placementIndex = 0; placementIndex < placements.Count; placementIndex++)
                {
                    InitialFactionBasePlacement placement = placements[placementIndex];
                    GameObject prefab = placement.Kind == InitialFactionBasePlacementKind.Gate
                        ? spawnConfig.BaseGatePrefab
                        : FindSpawnablePrefab(placementConfig, placement.PrefabKey);
                    Assert.NotNull(prefab, $"Missing prefab for {placement.PrefabKey}.");

                    Assert.IsTrue(TryGetRuntimeBuildingPlacementFootprint(buildingGameplay, prefab, placement.RotateVertical, out Vector2Int plannedFootprint));
                    Vector2Int origin = InitialFactionBaseLayoutPlanner.ResolvePlacementOrigin(anchor, placement, plannedFootprint);
                    bool spawned = TrySpawnRuntimeBuilding(
                        buildingGameplay,
                        prefab,
                        origin,
                        out _,
                        out Vector2Int actualOrigin,
                        out Vector2Int actualFootprint,
                        ownerFactionId: (byte)faction.FactionId,
                        rotateVertical: placement.RotateVertical);
                    Assert.IsTrue(
                        spawned,
                        $"Faction {faction.FactionId} failed to place {placement.PrefabKey} kind={placement.Kind} requestedOrigin={origin}.");
                    if (placement.PrefabKey == "Building_Airport")
                        Assert.LessOrEqual(Vector2Int.Distance(origin, actualOrigin), 160f, "Airport fallback placement should stay near the planned base position.");
                    if (placement.Kind == InitialFactionBasePlacementKind.Gate)
                        AssertGateCenteredOnOpening((byte)faction.FactionId, anchor, placement, actualOrigin, actualFootprint);
                }

                RuntimeGameplayStateTestHelper.PublishBuildingRuntimeBoundary(world.EntityManager, TickBuildingRuntime);
                string ownedSummary = RuntimeGameplayStateTestHelper.DescribeOwnedBuildingSummaries(world.EntityManager);
                StringAssert.Contains("id=building_airport count=1", ownedSummary);
                StringAssert.Contains("id=building_helipad count=3", ownedSummary);
                StringAssert.Contains("id=building_ammunition_depot count=1", ownedSummary);
                StringAssert.Contains("id=building_road_barrier count=2", ownedSummary);
            }
        }
        finally
        {
            if (buildingGameplayInitialized)
                buildingGameplay.Dispose?.Invoke();
            if (runtimeRoot != null)
                Object.DestroyImmediate(runtimeRoot);
            if (world.IsCreated)
                world.Dispose();
            World.DefaultGameObjectInjectionWorld = previousWorld;
            if (blockerCounts.IsCreated)
                blockerCounts.Dispose();
            if (blocked.IsCreated)
                blocked.Dispose();
            if (occupied.IsCreated)
                occupied.Dispose();
            if (friendlyPassFactionIds.IsCreated)
                friendlyPassFactionIds.Dispose();
        }
    }

    private static List<string> CollectPrefabKeys(List<InitialFactionBasePlacement> placements)
    {
        var keys = new List<string>();
        for (int i = 0; i < placements.Count; i++)
        {
            if (!string.IsNullOrWhiteSpace(placements[i].PrefabKey))
                keys.Add(placements[i].PrefabKey);
        }

        return keys;
    }

    private static void AssertGateCenteredOnOpening(
        byte factionId,
        Vector2Int anchor,
        InitialFactionBasePlacement placement,
        Vector2Int actualOrigin,
        Vector2Int actualFootprint)
    {
        if (placement.Offset.x == 0 && placement.Offset.y != 0)
        {
            int gateCenterX = actualOrigin.x + actualFootprint.x / 2;
            Assert.LessOrEqual(
                Mathf.Abs(gateCenterX - anchor.x),
                1,
                $"Faction {factionId} bottom gate should be centered in the wall opening.");
            Assert.AreEqual(
                anchor.y + placement.Offset.y,
                actualOrigin.y,
                $"Faction {factionId} bottom gate should stay on the bottom wall line.");
            return;
        }

        if (placement.Offset.y == 0 && placement.Offset.x != 0)
        {
            int gateCenterY = actualOrigin.y + actualFootprint.y / 2;
            Assert.LessOrEqual(
                Mathf.Abs(gateCenterY - anchor.y),
                1,
                $"Faction {factionId} side gate should be centered in the wall opening.");
            Assert.AreEqual(
                anchor.x + placement.Offset.x,
                actualOrigin.x,
                $"Faction {factionId} side gate should stay on the side wall line.");
        }
    }

    private static List<InitialFactionBasePlacement> BuildPlannedPlacements()
    {
        var placements = new List<InitialFactionBasePlacement>();
        InitialFactionBaseLayoutPlanner.BuildPlacements(120, 80, placements);
        return placements;
    }

    private static GameObject FindSpawnablePrefab(BuildingPlacementSystemConfig placementConfig, string prefabKey)
    {
        if (placementConfig == null || string.IsNullOrWhiteSpace(prefabKey))
            return null;

        string normalizedKey = NormalizePrefabKey(prefabKey);
        for (int i = 0; i < placementConfig.Spawnables.Count; i++)
        {
            GameObject prefab = placementConfig.Spawnables[i];
            if (prefab == null)
                continue;

            if (NormalizePrefabKey(prefab.name) == normalizedKey)
                return prefab;

            string path = AssetDatabase.GetAssetPath(prefab);
            if (!string.IsNullOrEmpty(path) &&
                NormalizePrefabKey(Path.GetFileNameWithoutExtension(path)) == normalizedKey)
                return prefab;
        }

        return null;
    }

    private static string NormalizePrefabKey(string key)
    {
        return string.IsNullOrWhiteSpace(key)
            ? string.Empty
            : key.Trim().ToLowerInvariant().Replace(" ", string.Empty).Replace("_", string.Empty).Replace("-", string.Empty);
    }

    private static BuildingGameplayCompositionResultSystem.Result CreateBuildingGameplay(
        BuildingPlacementSystemConfig placementConfig,
        Transform runtimeRoot)
    {
        var composition = new BuildingGameplayCompositionSystem();
        return composition.Initialize(
            buildingPlacementConfig: placementConfig,
            worldCamera: null,
            runtimeTransportsRoot: runtimeRoot,
            runtimeUiRoot: runtimeRoot,
            roadFootprintState: default,
            factionVisuals: null,
            dayNight: null,
            resolveSpawnableLookupKey: BuildingSpawnPrefabLookupKeySystem.ResolveSpawnableLookupKey,
            tryGetBuildingDefinitionMetadata: BuildingDefinitionAuthoringMetadataSystem.TryGetBuildingDefinitionMetadata,
            tryGetUnitDefinitionMetadata: BuildingDefinitionAuthoringMetadataSystem.TryGetUnitDefinitionMetadata);
    }

    private static bool TrySpawnRuntimeBuilding(
        BuildingGameplayCompositionResultSystem.Result buildingGameplay,
        GameObject prefab,
        Vector2Int preferredOrigin,
        out int buildingId,
        out Vector2Int actualOrigin,
        out Vector2Int actualFootprint,
        byte? ownerFactionId = null,
        bool rotateVertical = false)
    {
        buildingId = 0;
        actualOrigin = default;
        actualFootprint = default;
        BuildingRuntimeSpawnCommandBoundary.Context commandContext = buildingGameplay.RuntimeSpawnCommandContext;
        if (commandContext.RuntimeSpawnSystem == null ||
            !commandContext.RuntimeSpawnSystem.TrySpawnRuntimeBuilding(
                commandContext.SpawnContext,
                prefab,
                preferredOrigin,
                "Building",
                "Operational building.",
                null,
                500,
                isCityGenerated: false,
                ownerFactionId: ownerFactionId,
                rotateVertical: rotateVertical,
                out BuildingRuntimeSpawnSystem.SpawnRuntimeBuildingResult result))
        {
            return false;
        }

        buildingId = result.BuildingId;
        actualOrigin = result.ActualOrigin;
        actualFootprint = result.ActualFootprint;
        return true;
    }

    private static int TrySpawnRuntimeWallRun(
        BuildingGameplayCompositionResultSystem.Result buildingGameplay,
        GameObject prefab,
        Vector2Int startOrigin,
        Vector2Int endOrigin,
        byte? ownerFactionId)
    {
        BuildingRuntimeSpawnCommandBoundary.Context commandContext = buildingGameplay.RuntimeSpawnCommandContext;
        return commandContext.RuntimeSpawnSystem != null
            ? commandContext.RuntimeSpawnSystem.TrySpawnRuntimeWallRun(
                commandContext.SpawnContext,
                prefab,
                startOrigin,
                endOrigin,
                ownerFactionId)
            : 0;
    }

    private static bool TrySpawnRuntimeWallSegment(
        BuildingGameplayCompositionResultSystem.Result buildingGameplay,
        GameObject prefab,
        Vector2Int origin,
        bool rotateVertical,
        byte? ownerFactionId,
        bool allowExistingWallOverlap)
    {
        BuildingRuntimeSpawnCommandBoundary.Context commandContext = buildingGameplay.RuntimeSpawnCommandContext;
        return commandContext.RuntimeSpawnSystem != null &&
               commandContext.RuntimeSpawnSystem.TrySpawnRuntimeWallSegment(
                   commandContext.SpawnContext,
                   prefab,
                   origin,
                   rotateVertical,
                   ownerFactionId,
                   allowExistingWallOverlap);
    }

    private static bool TryGetRuntimeWallSegmentFootprint(
        BuildingGameplayCompositionResultSystem.Result buildingGameplay,
        GameObject prefab,
        bool rotateVertical,
        out Vector2Int footprint)
    {
        footprint = default;
        BuildingRuntimeSpawnCommandBoundary.Context commandContext = buildingGameplay.RuntimeSpawnCommandContext;
        return commandContext.RuntimeSpawnSystem != null &&
               commandContext.RuntimeSpawnSystem.TryGetRuntimeWallSegmentFootprint(
                   commandContext.SpawnContext,
                   prefab,
                   rotateVertical,
                   out footprint);
    }

    private static bool TryGetRuntimeBuildingPlacementFootprint(
        BuildingGameplayCompositionResultSystem.Result buildingGameplay,
        GameObject prefab,
        bool rotateVertical,
        out Vector2Int footprint)
    {
        footprint = default;
        BuildingRuntimeSpawnCommandBoundary.Context commandContext = buildingGameplay.RuntimeSpawnCommandContext;
        return commandContext.RuntimeSpawnSystem != null &&
               commandContext.RuntimeSpawnSystem.TryGetRuntimeBuildingPlacementFootprint(
                   commandContext.SpawnContext,
                   prefab,
                   rotateVertical,
                   out footprint);
    }

    private static Entity GetGridEntity(EntityManager em)
    {
        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<GridConfig>());
        Assert.AreEqual(1, query.CalculateEntityCount(), "Initial faction base validation expects exactly one grid entity.");
        return query.GetSingletonEntity();
    }

    private static void ResolveInitialBaseValidationGridSize(
        InitialUnitsSpawnerAuthoringConfig spawnConfig,
        out int width,
        out int height)
    {
        width = 720;
        height = 360;
        if (spawnConfig == null)
            return;

        int margin = Mathf.Max(spawnConfig.BaseHalfWidthCells, spawnConfig.BaseHalfHeightCells) + 64;
        for (int i = 0; i < spawnConfig.Factions.Count; i++)
        {
            InitialUnitsSpawnerAuthoringConfig.FactionEntry faction = spawnConfig.Factions[i];
            width = Mathf.Max(width, faction.SpawnCell.x + spawnConfig.BaseHalfWidthCells + margin);
            height = Mathf.Max(height, faction.SpawnCell.y + spawnConfig.BaseHalfHeightCells + margin);
        }
    }

    private static void CreateGrid(
        EntityManager em,
        int width,
        int height,
        out NativeArray<int> blockerCounts,
        out NativeBitArray blocked,
        out NativeBitArray occupied,
        out NativeArray<byte> friendlyPassFactionIds)
    {
        int gridSize = width * height;
        blockerCounts = new NativeArray<int>(gridSize, Allocator.Persistent);
        blocked = new NativeBitArray(gridSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        occupied = new NativeBitArray(gridSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        friendlyPassFactionIds = new NativeArray<byte>(gridSize, Allocator.Persistent);

        Entity gridEntity = em.CreateEntity(typeof(GridConfig), typeof(DynamicBlockerComponent), typeof(DynamicOccupancyComponent));
        em.SetComponentData(gridEntity, new GridConfig { Width = width, Height = height, CellSize = 1f, Origin = float3.zero });
        em.SetComponentData(gridEntity, new DynamicBlockerComponent
        {
            GridSize = gridSize,
            Counts = blockerCounts,
            Blocked = blocked,
            FriendlyPassFactionIds = friendlyPassFactionIds
        });
        em.SetComponentData(gridEntity, new DynamicOccupancyComponent
        {
            GridSize = gridSize,
            Occupied = occupied
        });

        DynamicBuffer<GridRoad> roads = em.AddBuffer<GridRoad>(gridEntity);
        roads.ResizeUninitialized(gridSize);
        for (int i = 0; i < roads.Length; i++)
            roads[i] = new GridRoad { Value = 0 };

        DynamicBuffer<GridWalkable> walkable = em.AddBuffer<GridWalkable>(gridEntity);
        walkable.ResizeUninitialized(gridSize);
        for (int i = 0; i < walkable.Length; i++)
            walkable[i] = new GridWalkable { Value = 1 };
    }

    private static int CountKind(List<InitialFactionBasePlacement> placements, InitialFactionBasePlacementKind kind)
    {
        int count = 0;
        for (int i = 0; i < placements.Count; i++)
        {
            if (placements[i].Kind == kind)
                count++;
        }

        return count;
    }

    private static int CountPrefab(List<InitialFactionBasePlacement> placements, string prefabKey)
    {
        int count = 0;
        for (int i = 0; i < placements.Count; i++)
        {
            if (placements[i].PrefabKey == prefabKey)
                count++;
        }

        return count;
    }

    private static int CountProductionSpawnPoints(GameObject prefab)
    {
        int count = 0;
        Transform[] transforms = prefab.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            string name = transforms[i] != null ? transforms[i].name : string.Empty;
            if (name.StartsWith("Spawn_", System.StringComparison.OrdinalIgnoreCase))
                count++;
        }

        return count;
    }

    private static void AssertConfiguredUnitOffsetsMatchBaseZones(InitialUnitsSpawnerAuthoringConfig.FactionEntry faction)
    {
        Assert.GreaterOrEqual(faction.Units.Count, 7, $"Faction {faction.FactionId} should have infantry and vehicle entries.");
        for (int unitIndex = 0; unitIndex < faction.Units.Count; unitIndex++)
        {
            string prefabName = GetUnitPrefabName(faction.Units[unitIndex]);
            Vector2Int offset = faction.Units[unitIndex].SpawnOffset;
            if (prefabName.StartsWith("Unit_Chr_"))
            {
                Assert.LessOrEqual(offset.x, -56, $"Faction {faction.FactionId} infantry entry {unitIndex} should spawn around tents.");
                Assert.GreaterOrEqual(offset.y, 0, $"Faction {faction.FactionId} infantry entry {unitIndex} should spawn around tents.");
            }
            else if (prefabName.StartsWith("Unit_Veh_Helicopter_"))
            {
                Assert.GreaterOrEqual(offset.x, 70, $"Faction {faction.FactionId} helicopter entry {unitIndex} should spawn on helipads.");
                Assert.LessOrEqual(offset.y, -50, $"Faction {faction.FactionId} helicopter entry {unitIndex} should spawn on helipads.");
            }
            else if (prefabName == "Unit_Veh_Jet_01" ||
                     prefabName == "Unit_Veh_Jet_02" ||
                     prefabName == "Unit_Veh_Drone")
            {
                Assert.GreaterOrEqual(offset.x, 48, $"Faction {faction.FactionId} airfield entry {unitIndex} should spawn on airport spawn points.");
                Assert.GreaterOrEqual(offset.y, 28, $"Faction {faction.FactionId} airfield entry {unitIndex} should spawn on airport spawn points.");
            }
            else
            {
                Assert.GreaterOrEqual(offset.x, 18, $"Faction {faction.FactionId} vehicle entry {unitIndex} should spawn around the fuel bladder.");
                Assert.LessOrEqual(offset.y, -42, $"Faction {faction.FactionId} vehicle entry {unitIndex} should spawn around the fuel bladder.");
            }
        }
    }

    private static void AssertConfiguredAirUnitsMatchBasePlatforms(InitialUnitsSpawnerAuthoringConfig.FactionEntry faction)
    {
        Assert.GreaterOrEqual(CountUnitPrefab(faction, "Unit_Veh_Helicopter_Transport"), 1, $"Faction {faction.FactionId} should start with at least one transport helicopter.");
        Assert.AreEqual(1, CountUnitPrefab(faction, "Unit_Veh_Jet_01"), $"Faction {faction.FactionId} should start with one Jet 01.");
        Assert.AreEqual(1, CountUnitPrefab(faction, "Unit_Veh_Jet_02"), $"Faction {faction.FactionId} should start with one Jet 02.");
        Assert.AreEqual(1, CountUnitPrefab(faction, "Unit_Veh_Drone"), $"Faction {faction.FactionId} should start with one drone.");
        for (int i = 0; i < RequiredInitialGroundVehiclePrefabs.Length; i++)
        {
            string prefabName = RequiredInitialGroundVehiclePrefabs[i];
            Assert.AreEqual(1, CountUnitPrefab(faction, prefabName), $"Faction {faction.FactionId} should start with {prefabName}.");
        }

        Assert.GreaterOrEqual(CountDistinctGroundVehiclePrefabs(faction), RequiredInitialGroundVehiclePrefabs.Length, $"Faction {faction.FactionId} should start with every configured ground vehicle type.");
    }

    private static int CountUnitPrefab(InitialUnitsSpawnerAuthoringConfig.FactionEntry faction, string prefabName)
    {
        int count = 0;
        for (int i = 0; i < faction.Units.Count; i++)
        {
            if (GetUnitPrefabName(faction.Units[i]) == prefabName)
                count += faction.Units[i].Count;
        }

        return count;
    }

    private static int CountDistinctGroundVehiclePrefabs(InitialUnitsSpawnerAuthoringConfig.FactionEntry faction)
    {
        var names = new HashSet<string>();
        for (int i = 0; i < faction.Units.Count; i++)
        {
            string prefabName = GetUnitPrefabName(faction.Units[i]);
            if (prefabName.StartsWith("Unit_Veh_") &&
                !prefabName.StartsWith("Unit_Veh_Helicopter_") &&
                prefabName != "Unit_Veh_Jet_01" &&
                prefabName != "Unit_Veh_Jet_02" &&
                prefabName != "Unit_Veh_Drone")
            {
                names.Add(prefabName);
            }
        }

        return names.Count;
    }

    private static string GetUnitPrefabName(InitialUnitsSpawnerAuthoringConfig.FactionUnitEntry unit)
    {
        if (unit == null || unit.Prefab == null)
            return string.Empty;

        string path = AssetDatabase.GetAssetPath(unit.Prefab);
        return string.IsNullOrEmpty(path)
            ? unit.Prefab.name
            : Path.GetFileNameWithoutExtension(path);
    }
}
