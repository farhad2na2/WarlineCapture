#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

public sealed class CustomGameStartupSystemTests
{
    private World _world;
    private NativeArray<int> _blockerCounts;
    private NativeBitArray _blocked;
    private NativeBitArray _occupied;
    private NativeArray<byte> _friendlyPassFactionIds;

    public static void RunBatchValidation()
    {
        var tests = new CustomGameStartupSystemTests();
        RunWithLifecycle(tests, tests.Initialize_CreatesRuntimeSingletonsAndBuffersInEmptyWorld);
        RunWithLifecycle(tests, tests.Initialize_DoesNotCreateMissionSession);
        RunWithLifecycle(tests, tests.Initialize_UpdatesExistingStartupEntityInsteadOfDuplicating);
        RunWithLifecycle(tests, tests.InitializeFromLegacyConfigs_CreatesSourceKeyEntriesWithoutConvertedPrefabs);
        RunWithLifecycle(tests, tests.InitializeFromLegacyConfigs_UsesConvertedPrefabEntitiesWhenAvailable);
        RunWithLifecycle(tests, tests.InitializeFromLegacyConfigs_UsesExistingRegistryOrderWhenPrefabEntityNamesAreUnavailable);
        RunWithLifecycle(tests, tests.InitialUnitsSpawnSystem_SkipsSourceKeyUnitsWithoutConvertedPrefabs);
        RunWithLifecycle(tests, tests.UnitImpostorRenderSystem_DoesNotDrawFallbackOverRenderableSourceKeyUnits);
        RunWithLifecycle(tests, tests.UnitImpostorRenderSystem_DoesNotDrawFarImpostorOverVisibleRenderableUnits);
        RunWithLifecycle(tests, tests.InitialUnitsSpawnSystem_DoesNotRouteConvertedPrefabUnitsThroughSourceKeyImpostors);
        RunWithLifecycle(tests, tests.RuntimeGridBootstrapSystem_CreatesBuffersWithoutInvalidatingHandles);
        tests.GameBootstrapDelegatesNoMissionStartupToCustomGameStartupSystem();
        tests.GameScene_AutoloadsGameSubSceneUntilRuntimePrefabReplacementExists();
    }

    private static void RunWithLifecycle(CustomGameStartupSystemTests tests, System.Action action)
    {
        tests.SetUp();
        try
        {
            action();
        }
        finally
        {
            tests.TearDown();
        }
    }

    [SetUp]
    public void SetUp()
    {
        WarlineCaptureMissionSession.Clear();
        _world = new World("CustomGameStartupSystemTests");
    }

    [TearDown]
    public void TearDown()
    {
        WarlineCaptureMissionSession.Clear();
        InitialUnitsRuntimeState.PlayRequested = false;
        if (_world != null && _world.IsCreated)
            _world.Dispose();
        _world = null;
        DisposeGridData();
    }

    [Test]
    public void Initialize_CreatesRuntimeSingletonsAndBuffersInEmptyWorld()
    {
        CustomGameStartupConfig config = CreateStartupConfig();

        CustomGameStartupSystem.Result result = new CustomGameStartupSystem().Initialize(_world, config);

        Assert.IsTrue(result.Initialized);
        Assert.AreEqual(2, result.FactionCount);
        Assert.AreEqual(2, result.UnitRosterCount);
        Assert.AreEqual(3, result.InitialUnitEntryCount);
        Assert.AreEqual(1, result.InitialBuildingEntryCount);
        Assert.AreEqual(2, result.VisualEntryCount);

        EntityManager em = _world.EntityManager;
        using EntityQuery query = em.CreateEntityQuery(typeof(CustomGameStartupStateComponent));
        Assert.AreEqual(1, query.CalculateEntityCount());
        Entity entity = query.GetSingletonEntity();

        CustomGameStartupStateComponent state = em.GetComponentData<CustomGameStartupStateComponent>(entity);
        Assert.AreEqual("custom.skirmish.test", state.GameModeId.ToString());
        Assert.AreEqual(128, state.GridWidth);
        Assert.AreEqual(96, state.GridHeight);
        Assert.AreEqual(2, state.FactionCount);
        Assert.AreEqual(2, state.UnitRosterCount);
        Assert.AreEqual(3, state.InitialUnitEntryCount);
        Assert.AreEqual(2, state.VisualEntryCount);

        Assert.IsTrue(em.HasComponent<InitialUnitsSpawnConfig>(entity));
        Assert.IsTrue(em.HasComponent<InitialUnitsBlockerChurnConfig>(entity));
        Assert.IsTrue(em.HasComponent<InitialUnitsBlockerChurnState>(entity));
        Assert.AreEqual(2, em.GetBuffer<InitialUnitsFactionSpawnEntry>(entity).Length);
        Assert.AreEqual(3, em.GetBuffer<InitialUnitsFactionUnitSpawnEntry>(entity).Length);
        Assert.AreEqual(1, em.GetBuffer<InitialUnitsFactionBuildingSpawnEntry>(entity).Length);
        Assert.AreEqual(3, em.GetBuffer<CustomGameFactionUnitSourceSpawnEntry>(entity).Length);
        Assert.AreEqual(2, em.GetBuffer<CustomGameUnitSourceRegistryEntry>(entity).Length);
        Assert.AreEqual(2, em.GetBuffer<CustomGameVisualRegistryEntry>(entity).Length);

        DynamicBuffer<CustomGameFactionUnitSourceSpawnEntry> sourceSpawns =
            em.GetBuffer<CustomGameFactionUnitSourceSpawnEntry>(entity);
        Assert.AreEqual("Unit_Chr_Rifleman", sourceSpawns[0].SourceKey.ToString());
        Assert.AreEqual(new int2(2, 3), sourceSpawns[0].SpawnOffset);
    }

    [Test]
    public void Initialize_DoesNotCreateMissionSession()
    {
        WarlineCaptureMissionSession.Clear();

        new CustomGameStartupSystem().Initialize(_world, CreateStartupConfig());

        Assert.IsFalse(WarlineCaptureMissionSession.HasActiveMission);
    }

    [Test]
    public void Initialize_UpdatesExistingStartupEntityInsteadOfDuplicating()
    {
        CustomGameStartupSystem system = new();
        CustomGameStartupConfig config = CreateStartupConfig();

        system.Initialize(_world, config);
        system.Initialize(_world, config);

        EntityManager em = _world.EntityManager;
        using EntityQuery query = em.CreateEntityQuery(typeof(CustomGameStartupStateComponent));
        Assert.AreEqual(1, query.CalculateEntityCount());
        Entity entity = query.GetSingletonEntity();
        Assert.AreEqual(3, em.GetBuffer<CustomGameFactionUnitSourceSpawnEntry>(entity).Length);
    }

    [Test]
    public void InitializeFromLegacyConfigs_CreatesSourceKeyEntriesWithoutConvertedPrefabs()
    {
        GameObject rifleman = new("Unit_Chr_Rifleman");
        GameObject truck = new("Unit_Veh_Truck");
        GameObject depot = new("Building_Command_Depot");
        try
        {
            InitialUnitsSpawnerAuthoringConfig spawnConfig = CreateLegacySpawnConfig(rifleman, truck, depot);
            UnitPrefabRegistryAuthoringConfig registryConfig = ScriptableObject.CreateInstance<UnitPrefabRegistryAuthoringConfig>();
            registryConfig.UnitSpawnPrefabs.Add(rifleman);
            registryConfig.UnitSpawnPrefabs.Add(truck);

            CustomGameStartupSystem.Result result =
                new CustomGameStartupSystem().InitializeFromLegacyConfigs(_world, spawnConfig, registryConfig);

            Assert.IsTrue(result.Initialized);
            Assert.AreEqual(1, result.FactionCount);
            Assert.AreEqual(2, result.InitialUnitEntryCount);
            Assert.AreEqual(1, result.InitialBuildingEntryCount);
            Assert.AreEqual(2, result.UnitRegistryEntryCount);
            Assert.AreEqual(2, result.VisualEntryCount);
            Assert.AreEqual(2, result.MissingVisualReferenceCount);

            EntityManager em = _world.EntityManager;
            using EntityQuery query = em.CreateEntityQuery(typeof(CustomGameStartupStateComponent));
            Assert.AreEqual(1, query.CalculateEntityCount());
            Entity entity = query.GetSingletonEntity();

            Assert.IsFalse(em.HasComponent<UnitPrefabRegistryTag>(entity));
            Assert.AreEqual(2, em.GetBuffer<UnitPrefabRegistryEntry>(entity).Length);
            Assert.AreEqual(2, em.GetBuffer<InitialUnitsFactionUnitSpawnEntry>(entity).Length);
            Assert.AreEqual(2, em.GetBuffer<CustomGameFactionUnitSourceSpawnEntry>(entity).Length);
            Assert.AreEqual("Unit_Chr_Rifleman", em.GetBuffer<CustomGameFactionUnitSourceSpawnEntry>(entity)[0].SourceKey.ToString());

            DynamicBuffer<InitialUnitsFactionUnitSpawnEntry> initialUnits =
                em.GetBuffer<InitialUnitsFactionUnitSpawnEntry>(entity);
            Assert.AreEqual(Entity.Null, initialUnits[0].Prefab);
            Assert.AreEqual(Entity.Null, initialUnits[1].Prefab);
        }
        finally
        {
            Object.DestroyImmediate(rifleman);
            Object.DestroyImmediate(truck);
            Object.DestroyImmediate(depot);
        }
    }

    [Test]
    public void InitializeFromLegacyConfigs_UsesConvertedPrefabEntitiesWhenAvailable()
    {
        GameObject rifleman = new("Unit_Chr_Rifleman");
        GameObject truck = new("Unit_Veh_Truck");
        GameObject depot = new("Building_Command_Depot");
        try
        {
            EntityManager em = _world.EntityManager;
            Entity riflemanPrefab = em.CreateEntity(typeof(Prefab), typeof(UnitGrid));
            Entity truckPrefab = em.CreateEntity(typeof(Prefab), typeof(UnitGrid));
            em.SetName(riflemanPrefab, "Unit_Chr_Rifleman");
            em.SetName(truckPrefab, "Unit_Veh_Truck");

            InitialUnitsSpawnerAuthoringConfig spawnConfig = CreateLegacySpawnConfig(rifleman, truck, depot);
            UnitPrefabRegistryAuthoringConfig registryConfig = ScriptableObject.CreateInstance<UnitPrefabRegistryAuthoringConfig>();
            registryConfig.UnitSpawnPrefabs.Add(rifleman);
            registryConfig.UnitSpawnPrefabs.Add(truck);

            new CustomGameStartupSystem().InitializeFromLegacyConfigs(_world, spawnConfig, registryConfig);

            using EntityQuery query = em.CreateEntityQuery(typeof(CustomGameStartupStateComponent));
            Entity entity = query.GetSingletonEntity();
            DynamicBuffer<UnitPrefabRegistryEntry> registry = em.GetBuffer<UnitPrefabRegistryEntry>(entity);
            Assert.AreEqual(riflemanPrefab, registry[0].Prefab);
            Assert.AreEqual(truckPrefab, registry[1].Prefab);

            DynamicBuffer<InitialUnitsFactionUnitSpawnEntry> initialUnits =
                em.GetBuffer<InitialUnitsFactionUnitSpawnEntry>(entity);
            Assert.AreEqual(riflemanPrefab, initialUnits[0].Prefab);
            Assert.AreEqual(truckPrefab, initialUnits[1].Prefab);
        }
        finally
        {
            Object.DestroyImmediate(rifleman);
            Object.DestroyImmediate(truck);
            Object.DestroyImmediate(depot);
        }
    }

    [Test]
    public void InitializeFromLegacyConfigs_UsesExistingRegistryOrderWhenPrefabEntityNamesAreUnavailable()
    {
        GameObject rifleman = new("Unit_Chr_Rifleman");
        GameObject apc = new("Unit_Veh_APC_Heavy");
        GameObject depot = new("Building_Command_Depot");
        try
        {
            EntityManager em = _world.EntityManager;
            Entity existingRegistryEntity = em.CreateEntity(typeof(UnitPrefabRegistryTag));
            DynamicBuffer<UnitPrefabRegistryEntry> existingRegistry = em.AddBuffer<UnitPrefabRegistryEntry>(existingRegistryEntity);
            Entity riflemanPrefab = em.CreateEntity(typeof(Prefab), typeof(UnitGrid));
            Entity apcPrefab = em.CreateEntity(typeof(Prefab), typeof(UnitGrid));
            existingRegistry.Add(new UnitPrefabRegistryEntry { Prefab = riflemanPrefab });
            existingRegistry.Add(new UnitPrefabRegistryEntry { Prefab = apcPrefab });

            InitialUnitsSpawnerAuthoringConfig spawnConfig = CreateLegacySpawnConfig(rifleman, apc, depot);
            UnitPrefabRegistryAuthoringConfig registryConfig = ScriptableObject.CreateInstance<UnitPrefabRegistryAuthoringConfig>();
            registryConfig.UnitSpawnPrefabs.Add(rifleman);
            registryConfig.UnitSpawnPrefabs.Add(apc);

            new CustomGameStartupSystem().InitializeFromLegacyConfigs(_world, spawnConfig, registryConfig);

            using EntityQuery query = em.CreateEntityQuery(typeof(CustomGameStartupStateComponent));
            Entity entity = query.GetSingletonEntity();
            DynamicBuffer<UnitPrefabRegistryEntry> registry = em.GetBuffer<UnitPrefabRegistryEntry>(entity);
            Assert.AreEqual(riflemanPrefab, registry[0].Prefab);
            Assert.AreEqual(apcPrefab, registry[1].Prefab);
            using EntityQuery registryQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<UnitPrefabRegistryTag>(),
                ComponentType.ReadOnly<UnitPrefabRegistryEntry>());
            Assert.AreEqual(1, registryQuery.CalculateEntityCount(), "Custom Game startup must not create a second UnitPrefabRegistryTag singleton.");

            DynamicBuffer<InitialUnitsFactionUnitSpawnEntry> initialUnits =
                em.GetBuffer<InitialUnitsFactionUnitSpawnEntry>(entity);
            Assert.AreEqual(riflemanPrefab, initialUnits[0].Prefab);
            Assert.AreEqual(apcPrefab, initialUnits[1].Prefab);
        }
        finally
        {
            Object.DestroyImmediate(rifleman);
            Object.DestroyImmediate(apc);
            Object.DestroyImmediate(depot);
        }
    }

    [Test]
    public void RuntimeGridBootstrapSystem_CreatesBuffersWithoutInvalidatingHandles()
    {
        RuntimeGridBootstrapSystem system = new();

        Assert.DoesNotThrow(() => system.Ensure(_world, 8, 6, 1f, Vector3.zero));

        EntityManager em = _world.EntityManager;
        using EntityQuery query = em.CreateEntityQuery(typeof(GridConfig));
        Assert.AreEqual(1, query.CalculateEntityCount());

        Entity gridEntity = query.GetSingletonEntity();
        Assert.AreEqual(48, em.GetBuffer<GridWalkable>(gridEntity).Length);
        Assert.AreEqual(48, em.GetBuffer<GridRoad>(gridEntity).Length);
        Assert.AreEqual(48, em.GetBuffer<GridRoadSidewalk>(gridEntity).Length);
        Assert.AreEqual(48, em.GetBuffer<GridRoadDirt>(gridEntity).Length);
        Assert.IsTrue(em.HasComponent<DynamicBlockerData>(gridEntity));
        Assert.AreEqual(1, em.GetBuffer<GridWalkable>(gridEntity)[0].Value);
        Assert.AreEqual(0, em.GetBuffer<GridRoad>(gridEntity)[0].Value);
    }

    [Test]
    public void InitialUnitsSpawnSystem_SkipsSourceKeyUnitsWithoutConvertedPrefabs()
    {
        EntityManager em = _world.EntityManager;
        SpawnCustomGameSourceKeyUnitsForValidation();

        Assert.AreEqual(0, CountSpawnedSourceKeyUnits(em), "Custom Game must not create 2D source-key unit stand-ins when converted prefab entities are missing.");
        Assert.AreEqual(0, CountSpawnedSourceKeyFallbackVisuals(em), "Missing prefab entities must not become standalone impostor sprites.");
        AssertNoMissionTutorialRuntimeStarted(em);
    }

    [Test]
    public void UnitImpostorRenderSystem_DoesNotDrawFallbackOverRenderableSourceKeyUnits()
    {
        EntityManager em = _world.EntityManager;
        Entity entity = em.CreateEntity(
            typeof(UnitGrid),
            typeof(LocalTransform),
            typeof(UnitSourcePrefabKey),
            typeof(Faction),
            typeof(RenderBounds));
        em.SetComponentData(entity, new UnitGrid { Cell = new int2(12, 12) });
        em.SetComponentData(entity, LocalTransform.FromPosition(new float3(12f, 0f, 12f)));
        em.SetComponentData(entity, new UnitSourcePrefabKey { Value = new FixedString64Bytes("Unit_Veh_Truck") });
        em.SetComponentData(entity, new Faction { Id = 0 });
        em.SetComponentData(entity, new RenderBounds { Value = new AABB { Center = float3.zero, Extents = new float3(1f) } });

        World previousWorld = World.DefaultGameObjectInjectionWorld;
        GameObject cameraObject = new("RenderableSourceKeyCamera");
        UnitImpostorRenderSystem impostors = new();
        try
        {
            World.DefaultGameObjectInjectionWorld = _world;
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(12f, 20f, -12f);
            camera.transform.rotation = Quaternion.Euler(60f, 0f, 0f);
            impostors.Init(camera, 0, null);
            impostors.LateUpdate();

            Assert.AreEqual(0, impostors.LastSourceKeyFallbackCandidateCount, "Renderable 3D units must not receive source-key fallback impostor overlays.");
            Assert.AreEqual(0, impostors.LastMissionFallbackCandidateCount, "Renderable 3D units must not receive mission fallback impostor overlays.");
        }
        finally
        {
            impostors.Dispose();
            World.DefaultGameObjectInjectionWorld = previousWorld;
            Object.DestroyImmediate(cameraObject);
        }
    }

    [Test]
    public void UnitImpostorRenderSystem_DoesNotDrawFarImpostorOverVisibleRenderableUnits()
    {
        EntityManager em = _world.EntityManager;
        Entity entity = em.CreateEntity(
            typeof(UnitGrid),
            typeof(LocalTransform),
            typeof(UnitSourcePrefabKey),
            typeof(Faction),
            typeof(RenderBounds),
            typeof(UnitRenderBudgetCulledUnitTag));
        em.SetComponentData(entity, new UnitGrid { Cell = new int2(12, 12) });
        em.SetComponentData(entity, LocalTransform.FromPosition(new float3(12f, 0f, 12f)));
        em.SetComponentData(entity, new UnitSourcePrefabKey { Value = new FixedString64Bytes("Unit_Veh_Truck") });
        em.SetComponentData(entity, new Faction { Id = 0 });
        em.SetComponentData(entity, new RenderBounds { Value = new AABB { Center = float3.zero, Extents = new float3(1f) } });

        World previousWorld = World.DefaultGameObjectInjectionWorld;
        GameObject cameraObject = new("VisibleRenderableFarImpostorCamera");
        UnitImpostorRenderSystem impostors = new();
        try
        {
            World.DefaultGameObjectInjectionWorld = _world;
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(12f, 20f, -12f);
            camera.transform.rotation = Quaternion.Euler(60f, 0f, 0f);
            impostors.Init(camera, 0, null);
            impostors.LateUpdate();

            Assert.AreEqual(0, impostors.LastCulledCandidateCount, "Far impostors must not draw over a 3D renderer that is still visibly active.");
        }
        finally
        {
            impostors.Dispose();
            World.DefaultGameObjectInjectionWorld = previousWorld;
            Object.DestroyImmediate(cameraObject);
        }
    }

    [Test]
    public void InitialUnitsSpawnSystem_DoesNotRouteConvertedPrefabUnitsThroughSourceKeyImpostors()
    {
        EntityManager em = _world.EntityManager;
        SpawnConvertedPrefabUnitsForValidation();

        Assert.AreEqual(3, CountSpawnedPrefabUnitsWithoutSourceKey(em), "Converted prefab units should remain real prefab-backed units.");
        Assert.AreEqual(0, CountSpawnedSourceKeyFallbackVisuals(em), "Converted prefab units must not be drawn by the source-key impostor fallback.");
    }

    [Test]
    public void GameBootstrapDelegatesNoMissionStartupToCustomGameStartupSystem()
    {
        string bootstrap = File.ReadAllText("Assets/Game/Scripts/Bootstrap/GameBootstrap.cs");
        StringAssert.Contains("private readonly CustomGameStartupSystem _customGameStartupSystem = new();", bootstrap);
        StringAssert.Contains("_customGameStartupSystem.InitializeFromLegacyConfigs(", bootstrap);
        StringAssert.DoesNotContain("SkirmishRuntimeConfigBootstrapSystem _skirmishRuntimeConfigBootstrapSystem", bootstrap);
        StringAssert.DoesNotContain("_skirmishRuntimeConfigBootstrapSystem.EnsureRuntimeConfigs(", bootstrap);
        Assert.IsFalse(File.Exists("Assets/Game/Scripts/Systems/SkirmishRuntimeConfigBootstrapSystem.cs"));
    }

    [Test]
    public void GameScene_AutoloadsGameSubSceneUntilRuntimePrefabReplacementExists()
    {
        string scene = File.ReadAllText("Assets/Game/Scenes/Game.unity");
        int nameIndex = scene.IndexOf("m_Name: GameSubScene", System.StringComparison.Ordinal);
        Assert.GreaterOrEqual(nameIndex, 0, "Game scene should keep GameSubScene active until Custom Game owns a real runtime ECS prefab replacement.");
        int blockStart = scene.LastIndexOf("--- !u!1", nameIndex, System.StringComparison.Ordinal);
        int blockEnd = scene.IndexOf("--- !u!1", nameIndex + 1, System.StringComparison.Ordinal);
        string block = scene.Substring(blockStart, blockEnd - blockStart);
        string subSceneComponentWindow = scene.Substring(nameIndex, Mathf.Min(1200, scene.Length - nameIndex));

        StringAssert.Contains("m_Name: GameSubScene", block);
        StringAssert.Contains("m_IsActive: 1", block);
        StringAssert.Contains("AutoLoadScene: 1", subSceneComponentWindow);
    }

    private static CustomGameStartupConfig CreateStartupConfig()
    {
        CustomGameMapConfig map = ScriptableObject.CreateInstance<CustomGameMapConfig>();
        SetPrivateField(map, "gridWidth", 128);
        SetPrivateField(map, "gridHeight", 96);
        SetPrivateField(map, "cellSize", 1f);
        SetPrivateField(map, "initialDollars", 5000);
        SetPrivateField(map, "initialOil", 40);
        SetPrivateField(map, "initialFuel", 30);
        SetPrivateField(map, "baseMinimumUnitsPerFaction", 4);

        CustomGameFactionConfig.UnitSpawnEntry playerInfantry = CreateUnitSpawn("Unit_Chr_Rifleman", 2, new Vector2Int(2, 3));
        CustomGameFactionConfig.UnitSpawnEntry playerTruck = CreateUnitSpawn("Unit_Veh_Truck", 1, new Vector2Int(4, 5));
        CustomGameFactionConfig.UnitSpawnEntry enemyInfantry = CreateUnitSpawn("Unit_Chr_Rifleman", 3, new Vector2Int(-2, -3));
        CustomGameFactionConfig.BuildingSpawnEntry enemyBuilding = CreateBuildingSpawn("building.command_post", new Vector2Int(8, 9));
        CustomGameFactionConfig.FactionEntry playerFaction = CreateFaction(1, "Player", new Vector2Int(20, 24), playerInfantry, playerTruck, null);
        CustomGameFactionConfig.FactionEntry enemyFaction = CreateFaction(2, "Enemy", new Vector2Int(80, 74), enemyInfantry, null, enemyBuilding);
        CustomGameFactionConfig factions = ScriptableObject.CreateInstance<CustomGameFactionConfig>();
        SetPrivateField(factions, "factions", new List<CustomGameFactionConfig.FactionEntry> { playerFaction, enemyFaction });

        CustomGameUnitRosterConfig.UnitEntry rifleman = CreateUnitRosterEntry("Unit_Chr_Rifleman", "Rifleman");
        CustomGameUnitRosterConfig.UnitEntry truck = CreateUnitRosterEntry("Unit_Veh_Truck", "Truck");
        CustomGameUnitRosterConfig roster = ScriptableObject.CreateInstance<CustomGameUnitRosterConfig>();
        SetPrivateField(roster, "units", new List<CustomGameUnitRosterConfig.UnitEntry> { rifleman, truck });

        CustomGameVisualRegistryConfig.VisualEntry riflemanVisual = CreateVisualEntry("Unit_Chr_Rifleman");
        CustomGameVisualRegistryConfig.VisualEntry truckVisual = CreateVisualEntry("Unit_Veh_Truck");
        CustomGameVisualRegistryConfig visuals = ScriptableObject.CreateInstance<CustomGameVisualRegistryConfig>();
        SetPrivateField(visuals, "visuals", new List<CustomGameVisualRegistryConfig.VisualEntry> { riflemanVisual, truckVisual });

        CustomGameStartupConfig startup = ScriptableObject.CreateInstance<CustomGameStartupConfig>();
        SetPrivateField(startup, "gameModeId", "custom.skirmish.test");
        SetPrivateField(startup, "mapConfig", map);
        SetPrivateField(startup, "factionConfig", factions);
        SetPrivateField(startup, "unitRosterConfig", roster);
        SetPrivateField(startup, "visualRegistryConfig", visuals);
        return startup;
    }

    private static CustomGameStartupConfig CreateSourceKeySpawnStartupConfig()
    {
        CustomGameMapConfig map = ScriptableObject.CreateInstance<CustomGameMapConfig>();
        SetPrivateField(map, "gridWidth", 96);
        SetPrivateField(map, "gridHeight", 96);
        SetPrivateField(map, "cellSize", 1f);
        SetPrivateField(map, "spawnRadiusCells", 4);
        SetPrivateField(map, "randomSeed", 77u);
        SetPrivateField(map, "initialDollars", 5000);
        SetPrivateField(map, "blockerCount", 0);
        SetPrivateField(map, "createFactionBases", false);
        SetPrivateField(map, "baseMinimumUnitsPerFaction", 0);
        SetPrivateField(map, "enableBlockerChurn", false);

        CustomGameFactionConfig.UnitSpawnEntry playerInfantry = CreateUnitSpawn("Unit_Chr_Rifleman", 2, new Vector2Int(1, 1));
        CustomGameFactionConfig.UnitSpawnEntry enemyInfantry = CreateUnitSpawn("Unit_Chr_Enemy_Rifleman", 3, new Vector2Int(-1, -1));
        CustomGameFactionConfig.FactionEntry playerFaction = CreateFaction(0, "Player", new Vector2Int(16, 16), playerInfantry, null, null);
        CustomGameFactionConfig.FactionEntry enemyFaction = CreateFaction(1, "Enemy", new Vector2Int(72, 72), enemyInfantry, null, null);
        CustomGameFactionConfig factions = ScriptableObject.CreateInstance<CustomGameFactionConfig>();
        SetPrivateField(factions, "factions", new List<CustomGameFactionConfig.FactionEntry> { playerFaction, enemyFaction });

        CustomGameUnitRosterConfig roster = ScriptableObject.CreateInstance<CustomGameUnitRosterConfig>();
        SetPrivateField(roster, "units", new List<CustomGameUnitRosterConfig.UnitEntry>
        {
            CreateUnitRosterEntry("Unit_Chr_Rifleman", "Rifleman"),
            CreateUnitRosterEntry("Unit_Chr_Enemy_Rifleman", "Enemy Rifleman")
        });

        CustomGameVisualRegistryConfig visuals = ScriptableObject.CreateInstance<CustomGameVisualRegistryConfig>();
        SetPrivateField(visuals, "visuals", new List<CustomGameVisualRegistryConfig.VisualEntry>
        {
            CreateVisualEntry("Unit_Chr_Rifleman"),
            CreateVisualEntry("Unit_Chr_Enemy_Rifleman")
        });

        CustomGameStartupConfig startup = ScriptableObject.CreateInstance<CustomGameStartupConfig>();
        SetPrivateField(startup, "gameModeId", "custom.skirmish.source-key-spawn-test");
        SetPrivateField(startup, "mapConfig", map);
        SetPrivateField(startup, "factionConfig", factions);
        SetPrivateField(startup, "unitRosterConfig", roster);
        SetPrivateField(startup, "visualRegistryConfig", visuals);
        return startup;
    }

    private static InitialUnitsSpawnerAuthoringConfig CreateLegacySpawnConfig(
        GameObject rifleman,
        GameObject truck,
        GameObject depot)
    {
        InitialUnitsSpawnerAuthoringConfig config = ScriptableObject.CreateInstance<InitialUnitsSpawnerAuthoringConfig>();
        InitialUnitsSpawnerAuthoringConfig.FactionUnitEntry riflemanSpawn =
            CreateLegacyUnitSpawn(rifleman, 2, new Vector2Int(1, 2));
        InitialUnitsSpawnerAuthoringConfig.FactionUnitEntry truckSpawn =
            CreateLegacyUnitSpawn(truck, 1, new Vector2Int(3, 4));
        InitialUnitsSpawnerAuthoringConfig.FactionBuildingEntry depotSpawn =
            CreateLegacyBuildingSpawn(depot, new Vector2Int(5, 6));
        InitialUnitsSpawnerAuthoringConfig.FactionEntry faction = new();
        SetPrivateField(faction, "factionId", 1);
        SetPrivateField(faction, "spawnCell", new Vector2Int(24, 28));
        SetPrivateField(faction, "units", new List<InitialUnitsSpawnerAuthoringConfig.FactionUnitEntry>
        {
            riflemanSpawn,
            truckSpawn
        });
        SetPrivateField(faction, "buildings", new List<InitialUnitsSpawnerAuthoringConfig.FactionBuildingEntry>
        {
            depotSpawn
        });
        SetPrivateField(config, "factions", new List<InitialUnitsSpawnerAuthoringConfig.FactionEntry> { faction });
        SetPrivateField(config, "createFactionBases", false);
        return config;
    }

    private static InitialUnitsSpawnerAuthoringConfig.FactionUnitEntry CreateLegacyUnitSpawn(
        GameObject prefab,
        int count,
        Vector2Int offset)
    {
        var entry = new InitialUnitsSpawnerAuthoringConfig.FactionUnitEntry();
        SetPrivateField(entry, "prefab", prefab);
        SetPrivateField(entry, "count", count);
        SetPrivateField(entry, "spawnOffset", offset);
        return entry;
    }

    private static InitialUnitsSpawnerAuthoringConfig.FactionBuildingEntry CreateLegacyBuildingSpawn(
        GameObject prefab,
        Vector2Int offset)
    {
        var entry = new InitialUnitsSpawnerAuthoringConfig.FactionBuildingEntry();
        SetPrivateField(entry, "prefab", prefab);
        SetPrivateField(entry, "originOffset", offset);
        return entry;
    }

    private static CustomGameFactionConfig.UnitSpawnEntry CreateUnitSpawn(string sourceKey, int count, Vector2Int offset)
    {
        var entry = new CustomGameFactionConfig.UnitSpawnEntry();
        SetPrivateField(entry, "sourceKey", sourceKey);
        SetPrivateField(entry, "count", count);
        SetPrivateField(entry, "spawnOffset", offset);
        return entry;
    }

    private static CustomGameFactionConfig.BuildingSpawnEntry CreateBuildingSpawn(string lookupKey, Vector2Int offset)
    {
        var entry = new CustomGameFactionConfig.BuildingSpawnEntry();
        SetPrivateField(entry, "lookupKey", lookupKey);
        SetPrivateField(entry, "originOffset", offset);
        return entry;
    }

    private static CustomGameFactionConfig.FactionEntry CreateFaction(
        int factionId,
        string displayName,
        Vector2Int spawnCell,
        CustomGameFactionConfig.UnitSpawnEntry firstUnit,
        CustomGameFactionConfig.UnitSpawnEntry secondUnit,
        CustomGameFactionConfig.BuildingSpawnEntry building)
    {
        var entry = new CustomGameFactionConfig.FactionEntry();
        List<CustomGameFactionConfig.UnitSpawnEntry> units = new();
        if (firstUnit != null)
            units.Add(firstUnit);
        if (secondUnit != null)
            units.Add(secondUnit);

        List<CustomGameFactionConfig.BuildingSpawnEntry> buildings = new();
        if (building != null)
            buildings.Add(building);

        SetPrivateField(entry, "factionId", factionId);
        SetPrivateField(entry, "displayName", displayName);
        SetPrivateField(entry, "spawnCell", spawnCell);
        SetPrivateField(entry, "units", units);
        SetPrivateField(entry, "buildings", buildings);
        return entry;
    }

    private static CustomGameUnitRosterConfig.UnitEntry CreateUnitRosterEntry(string sourceKey, string displayName)
    {
        var entry = new CustomGameUnitRosterConfig.UnitEntry();
        SetPrivateField(entry, "sourceKey", sourceKey);
        SetPrivateField(entry, "displayName", displayName);
        return entry;
    }

    private static CustomGameVisualRegistryConfig.VisualEntry CreateVisualEntry(string sourceKey)
    {
        var entry = new CustomGameVisualRegistryConfig.VisualEntry();
        SetPrivateField(entry, "sourceKey", sourceKey);
        SetPrivateField(entry, "worldSize", new Vector2(1f, 2f));
        return entry;
    }

    private void CreateGrid(EntityManager em, int width, int height)
    {
        DisposeGridData();
        int gridSize = width * height;
        _blockerCounts = new NativeArray<int>(gridSize, Allocator.Persistent);
        _blocked = new NativeBitArray(gridSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        _occupied = new NativeBitArray(gridSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        _friendlyPassFactionIds = new NativeArray<byte>(gridSize, Allocator.Persistent);

        Entity gridEntity = em.CreateEntity(typeof(GridConfig), typeof(DynamicBlockerData), typeof(DynamicOccupancyData));
        em.SetComponentData(gridEntity, new GridConfig { Width = width, Height = height, CellSize = 1f, Origin = float3.zero });
        em.SetComponentData(gridEntity, new DynamicBlockerData
        {
            GridSize = gridSize,
            Counts = _blockerCounts,
            Blocked = _blocked,
            FriendlyPassFactionIds = _friendlyPassFactionIds
        });
        em.SetComponentData(gridEntity, new DynamicOccupancyData
        {
            GridSize = gridSize,
            Occupied = _occupied
        });

        DynamicBuffer<GridWalkable> walkable = em.AddBuffer<GridWalkable>(gridEntity);
        walkable.ResizeUninitialized(gridSize);
        for (int i = 0; i < walkable.Length; i++)
            walkable[i] = new GridWalkable { Value = 1 };
    }

    private void SpawnCustomGameSourceKeyUnitsForValidation()
    {
        CustomGameStartupConfig config = CreateSourceKeySpawnStartupConfig();
        EntityManager em = _world.EntityManager;
        CreateGrid(em, 96, 96);
        RuntimeGameplayStateTestHelper.SetPlayRequested(em, true);
        RuntimeGameplayStateTestHelper.SetBuildingPlacement(em, null);

        CustomGameStartupSystem.Result result = new CustomGameStartupSystem().Initialize(_world, config);
        Assert.IsTrue(result.Initialized);

        using (EntityQuery startupQuery = em.CreateEntityQuery(typeof(CustomGameStartupStateComponent)))
        {
            Assert.AreEqual(1, startupQuery.CalculateEntityCount());
            Entity startupEntity = startupQuery.GetSingletonEntity();
            Assert.IsTrue(em.HasComponent<InitialUnitsSpawnConfig>(startupEntity));
            Assert.IsFalse(em.HasComponent<UnitPrefabRegistryTag>(startupEntity));

            DynamicBuffer<InitialUnitsFactionUnitSpawnEntry> initialUnits =
                em.GetBuffer<InitialUnitsFactionUnitSpawnEntry>(startupEntity);
            for (int i = 0; i < initialUnits.Length; i++)
                Assert.AreEqual(Entity.Null, initialUnits[i].Prefab);
        }

        SystemHandle spawnSystem = _world.CreateSystem<InitialUnitsSpawnSystem>();
        for (int frame = 0; frame < 8 && CountSpawnedSourceKeyUnits(em) < 5; frame++)
        {
            spawnSystem.Update(_world.Unmanaged);
            em.CompleteAllTrackedJobs();
        }
    }

    private void SpawnConvertedPrefabUnitsForValidation()
    {
        EntityManager em = _world.EntityManager;
        GameObject rifleman = new("Unit_Chr_Rifleman");
        GameObject truck = new("Unit_Veh_Truck");
        GameObject depot = new("Building_Command_Depot");
        try
        {
            CreateGrid(em, 96, 96);
            RuntimeGameplayStateTestHelper.SetPlayRequested(em, true);
            RuntimeGameplayStateTestHelper.SetBuildingPlacement(em, null);

            Entity riflemanPrefab = em.CreateEntity(typeof(Prefab), typeof(UnitGrid), typeof(LocalTransform));
            Entity truckPrefab = em.CreateEntity(typeof(Prefab), typeof(UnitGrid), typeof(LocalTransform));
            em.SetName(riflemanPrefab, "Unit_Chr_Rifleman");
            em.SetName(truckPrefab, "Unit_Veh_Truck");

            InitialUnitsSpawnerAuthoringConfig spawnConfig = CreateLegacySpawnConfig(rifleman, truck, depot);
            UnitPrefabRegistryAuthoringConfig registryConfig = ScriptableObject.CreateInstance<UnitPrefabRegistryAuthoringConfig>();
            registryConfig.UnitSpawnPrefabs.Add(rifleman);
            registryConfig.UnitSpawnPrefabs.Add(truck);

            CustomGameStartupSystem.Result result =
                new CustomGameStartupSystem().InitializeFromLegacyConfigs(_world, spawnConfig, registryConfig);
            Assert.IsTrue(result.Initialized);

            using (EntityQuery startupQuery = em.CreateEntityQuery(typeof(CustomGameStartupStateComponent)))
            {
                Entity startupEntity = startupQuery.GetSingletonEntity();
                DynamicBuffer<InitialUnitsFactionUnitSpawnEntry> initialUnits =
                    em.GetBuffer<InitialUnitsFactionUnitSpawnEntry>(startupEntity);
                Assert.AreEqual(riflemanPrefab, initialUnits[0].Prefab);
                Assert.AreEqual(truckPrefab, initialUnits[1].Prefab);
            }

            SystemHandle spawnSystem = _world.CreateSystem<InitialUnitsSpawnSystem>();
            for (int frame = 0; frame < 8 && CountSpawnedPrefabUnitsWithoutSourceKey(em) < 3; frame++)
            {
                spawnSystem.Update(_world.Unmanaged);
                em.CompleteAllTrackedJobs();
            }
        }
        finally
        {
            Object.DestroyImmediate(rifleman);
            Object.DestroyImmediate(truck);
            Object.DestroyImmediate(depot);
        }
    }

    private void DisposeGridData()
    {
        if (_friendlyPassFactionIds.IsCreated)
            _friendlyPassFactionIds.Dispose();
        if (_occupied.IsCreated)
            _occupied.Dispose();
        if (_blocked.IsCreated)
            _blocked.Dispose();
        if (_blockerCounts.IsCreated)
            _blockerCounts.Dispose();
    }

    private static int CountSpawnedSourceKeyUnits(EntityManager em)
    {
        using EntityQuery query = em.CreateEntityQuery(
            ComponentType.ReadOnly<UnitGrid>(),
            ComponentType.ReadOnly<LocalTransform>(),
            ComponentType.ReadOnly<UnitSourcePrefabKey>(),
            ComponentType.ReadOnly<Faction>());
        return query.CalculateEntityCount();
    }

    private static int CountSpawnedPrefabUnitsWithoutSourceKey(EntityManager em)
    {
        using EntityQuery query = em.CreateEntityQuery(
            ComponentType.ReadOnly<UnitGrid>(),
            ComponentType.ReadOnly<LocalTransform>(),
            ComponentType.ReadOnly<Faction>(),
            ComponentType.Exclude<UnitSourcePrefabKey>(),
            ComponentType.Exclude<Prefab>());
        return query.CalculateEntityCount();
    }

    private static int CountSpawnedSourceKeyFallbackVisuals(EntityManager em)
    {
        using EntityQuery query = em.CreateEntityQuery(
            ComponentType.ReadOnly<UnitGrid>(),
            ComponentType.ReadOnly<LocalTransform>(),
            ComponentType.ReadOnly<UnitSourcePrefabKey>(),
            ComponentType.Exclude<UnitModelInstanceReference>(),
            ComponentType.Exclude<UnitRenderBudgetCulledUnitTag>(),
            ComponentType.Exclude<MissionRuntimeEntityId>());
        return query.CalculateEntityCount();
    }

    private static void AssertSpawnedSourceKeyUnits(
        EntityManager em,
        GridConfig gridConfig,
        int2 playerSpawnCenter,
        int2 enemySpawnCenter,
        int spawnRadiusCells)
    {
        using EntityQuery query = em.CreateEntityQuery(
            ComponentType.ReadOnly<UnitGrid>(),
            ComponentType.ReadOnly<LocalTransform>(),
            ComponentType.ReadOnly<UnitSourcePrefabKey>(),
            ComponentType.ReadOnly<Faction>());
        using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
        int playerCount = 0;
        int enemyCount = 0;
        int playerRiflemanCount = 0;
        int enemyRiflemanCount = 0;
        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            Assert.IsFalse(em.HasComponent<Prefab>(entity), "Source-key spawned Custom Game units must not be converted prefab entities.");
            Assert.IsTrue(em.HasComponent<UnitPrevWorldPos>(entity));
            Assert.IsTrue(em.HasComponent<UnitMoveVisualState>(entity));
            Assert.IsTrue(em.HasComponent<UnitRespawnPrefab>(entity));
            Assert.IsTrue(em.HasComponent<UnitAttackState>(entity));

            Faction faction = em.GetComponentData<Faction>(entity);
            UnitSourcePrefabKey sourceKey = em.GetComponentData<UnitSourcePrefabKey>(entity);
            UnitGrid grid = em.GetComponentData<UnitGrid>(entity);
            LocalTransform transform = em.GetComponentData<LocalTransform>(entity);
            string sourceKeyText = sourceKey.Value.ToString();
            Assert.IsTrue(IsValidCell(grid.Cell, gridConfig), $"Spawned unit must occupy a valid grid cell. cell={grid.Cell}");
            Assert.IsFalse(string.IsNullOrWhiteSpace(sourceKeyText));
            Assert.AreEqual(GridUtils.CellToWorldCenter(gridConfig, grid.Cell), transform.Position);
            Assert.IsFalse(em.HasComponent<SelectedUnitTag>(entity), "Custom Game startup should not auto-select initial units.");
            Assert.IsFalse(em.HasComponent<MissionRuntimeEntityId>(entity), "Custom Game units must not be tagged as mission runtime entities.");
            Assert.IsFalse(em.HasComponent<MissionRuntimeObjectiveTarget>(entity), "Custom Game units must not be bound to mission objectives.");
            Assert.IsFalse(em.HasComponent<MissionRuntimeCommandSquadTag>(entity), "Custom Game units must not use M01 command squad tags.");
            Assert.IsFalse(em.HasComponent<MissionRuntimeEnemyPatrolTag>(entity), "Custom Game units must not use M01 enemy patrol tags.");

            if (faction.Id == 0)
            {
                playerCount++;
                Assert.IsTrue(IsNear(grid.Cell, playerSpawnCenter, spawnRadiusCells), $"Player unit spawned outside configured player radius. cell={grid.Cell}");
                if (sourceKeyText == "Unit_Chr_Rifleman")
                    playerRiflemanCount++;
            }
            if (faction.Id == 1)
            {
                enemyCount++;
                Assert.IsTrue(IsNear(grid.Cell, enemySpawnCenter, spawnRadiusCells), $"Enemy unit spawned outside configured enemy radius. cell={grid.Cell}");
                if (sourceKeyText == "Unit_Chr_Enemy_Rifleman")
                    enemyRiflemanCount++;
            }
        }

        Assert.AreEqual(2, playerCount);
        Assert.AreEqual(3, enemyCount);
        Assert.AreEqual(2, playerRiflemanCount);
        Assert.AreEqual(3, enemyRiflemanCount);
        Assert.AreEqual(0, CountComponents<FocusedUnitUiReadModelComponent>(em), "Custom Game startup should not create focused-unit UI state before player selection.");
    }

    private static void AssertNoMissionTutorialRuntimeStarted(EntityManager em)
    {
        Assert.IsFalse(WarlineCaptureMissionSession.HasActiveMission);
        Assert.IsFalse(Chapter01M01PlayableRuntime.IsActiveMission());
        Assert.AreEqual(0, CountComponents<MissionRuntimeEntityId>(em));
        Assert.AreEqual(0, CountComponents<MissionRuntimeObjectiveTarget>(em));
        Assert.AreEqual(0, CountComponents<MissionRuntimeCommandSquadTag>(em));
        Assert.AreEqual(0, CountComponents<MissionRuntimeEnemyPatrolTag>(em));
        Assert.AreEqual(0, CountComponents<MissionRuntimePatrolRoute>(em));
        Assert.AreEqual(0, CountComponents<MissionRuntimeOpeningControlProtection>(em));
    }

    private static bool IsValidCell(int2 cell, GridConfig grid)
    {
        return cell.x >= 0 &&
            cell.y >= 0 &&
            cell.x < grid.Width &&
            cell.y < grid.Height;
    }

    private static bool IsNear(int2 cell, int2 center, int radiusCells)
    {
        return math.abs(cell.x - center.x) <= radiusCells &&
            math.abs(cell.y - center.y) <= radiusCells;
    }

    private static int CountComponents<T>(EntityManager em)
        where T : unmanaged, IComponentData
    {
        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<T>());
        return query.CalculateEntityCount();
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field, $"Missing field {fieldName} on {target.GetType().Name}.");
        field.SetValue(target, value);
    }
}
#endif
