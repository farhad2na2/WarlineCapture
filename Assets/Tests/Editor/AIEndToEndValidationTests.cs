using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.TestTools;
using Game.Components;
using Game.Configs;
using Game.Authoring;
using Game.Runtime;
using Game.Composition;

public sealed class AIEndToEndValidationTests
{
    private World _previousDefaultWorld;
    private World _world;
    private BuildingGameplayCompositionSystemHelper _buildingComposition;
    private BuildingGameplayResultCompositionSystemHelper.Result _buildingGameplay;
    private bool _buildingGameplayInitialized;
    private NativeArray<int> _blockerCounts;
    private NativeBitArray _blocked;
    private NativeBitArray _occupied;
    private NativeArray<byte> _friendlyPassFactionIds;
    private GameObject _runtimeRoot;
    private GameObject _buildingPrefab;
    private GameObject _unitPrefab;
    private BuildingPlacementSystemConfig _buildingConfig;
    private UnitPrefabRegistryAuthoringConfig _unitRegistryConfig;

    public static void RunFocusedValidation()
    {
        var tests = new AIEndToEndValidationTests();
        try
        {
            tests.SetUp();
            tests.AssertEnemyAILoop_BuildsProducesFormsSquadTargetsAndOrdersAttack(assertDiagnosticLog: false);
            Debug.Log("[AIEndToEndFocusedValidation] result=Passed tests=1");
        }
        catch (System.Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError("[AIEndToEndFocusedValidation] result=Failed");
            throw;
        }
        finally
        {
            tests.TearDown();
        }
    }

    [SetUp]
    public void SetUp()
    {
        InitialUnitsRuntimeState.VerboseAILogs = true;
    }

    [TearDown]
    public void TearDown()
    {
        RuntimeGameplayStateTestHelper.SetPlayRequested(false);
        InitialUnitsRuntimeState.VerboseAILogs = false;

        if (_buildingGameplayInitialized)
            _buildingGameplay.Dispose?.Invoke();
        _buildingGameplayInitialized = false;
        _buildingComposition = null;

        if (_world != null && _world.IsCreated)
            _world.Dispose();
        World.DefaultGameObjectInjectionWorld = _previousDefaultWorld;
        _world = null;

        if (_blockerCounts.IsCreated)
            _blockerCounts.Dispose();
        if (_blocked.IsCreated)
            _blocked.Dispose();
        if (_occupied.IsCreated)
            _occupied.Dispose();
        if (_friendlyPassFactionIds.IsCreated)
            _friendlyPassFactionIds.Dispose();

        if (_runtimeRoot != null)
            Object.DestroyImmediate(_runtimeRoot);
        if (_buildingPrefab != null)
            Object.DestroyImmediate(_buildingPrefab);
        if (_unitPrefab != null)
            Object.DestroyImmediate(_unitPrefab);
        if (_buildingConfig != null)
            Object.DestroyImmediate(_buildingConfig);
        if (_unitRegistryConfig != null)
            Object.DestroyImmediate(_unitRegistryConfig);
    }

    [Test]
    public void EnemyAILoop_BuildsProducesFormsSquadTargetsAndOrdersAttack()
    {
        AssertEnemyAILoop_BuildsProducesFormsSquadTargetsAndOrdersAttack(assertDiagnosticLog: true);
    }

    private void AssertEnemyAILoop_BuildsProducesFormsSquadTargetsAndOrdersAttack(bool assertDiagnosticLog)
    {
        _previousDefaultWorld = World.DefaultGameObjectInjectionWorld;
        _world = new World("AIEndToEndValidationTests");
        World.DefaultGameObjectInjectionWorld = _world;
        EntityManager em = _world.EntityManager;

        CreateGrid(em, 80, 80);
        CreateBuildingPlacementHarness();
        RuntimeGameplayStateTestHelper.SetBuildingPlacement(em, TickBuildingRuntime);

        Entity economyEntity = em.CreateEntity(
            typeof(FactionEconomy),
            typeof(FactionEconomyPolicy),
            typeof(FactionTacticalMaterialsComponent));
        em.SetComponentData(economyEntity, new FactionEconomy { FactionId = FactionIdentity.EnemyFactionId, Money = 100000, LastLogTime = -999f });
        em.SetComponentData(economyEntity, new FactionEconomyPolicy { Enabled = 1, SellIntervalSeconds = 8f });
        em.SetComponentData(economyEntity, new FactionTacticalMaterialsComponent
        {
            FactionId = FactionIdentity.EnemyFactionId,
            Capacity = 0
        });

        Entity controlEntity = em.CreateEntity(typeof(FactionControlConfigTag));
        DynamicBuffer<FactionControlEntry> controls = em.AddBuffer<FactionControlEntry>(controlEntity);
        controls.Add(new FactionControlEntry { FactionId = FactionIdentity.PlayerFactionId, AIControlled = 0, IsPlayerFaction = 1 });
        controls.Add(new FactionControlEntry { FactionId = FactionIdentity.EnemyFactionId, AIControlled = 1 });

        CreateBuildPlan(em);
        CreateProductionPlan(em);
        CreateSquadPlan(em);

        Entity target = CreateTarget(em, FactionIdentity.PlayerFactionId, new int2(50, 50), new float3(50f, 0f, 50f));
        Entity unitA = CreateAttacker(em, FactionIdentity.EnemyFactionId, new int2(20, 20), new float3(20f, 0f, 20f));
        Entity unitB = CreateAttacker(em, FactionIdentity.EnemyFactionId, new int2(21, 20), new float3(21f, 0f, 20f));
        Entity unitC = CreateAttacker(em, FactionIdentity.EnemyFactionId, new int2(22, 20), new float3(22f, 0f, 20f));
        Entity unitD = CreateAttacker(em, FactionIdentity.EnemyFactionId, new int2(23, 20), new float3(23f, 0f, 20f));

        RuntimeGameplayStateTestHelper.SetPlayRequested(em, true);
        RuntimeGameplayStateTestHelper.PublishBuildingRuntimeState(em, TickBuildingRuntime);
        SystemHandle buildSystem = _world.CreateSystem<AIBuildPlannerSystem>();
        SystemHandle logFlushSystem = _world.CreateSystem<AIDiagnosticLogFlushSystem>();
        SystemHandle productionSystem = _world.CreateSystem<AIProductionSystem>();
        SystemHandle squadSystem = _world.CreateSystem<AISquadSystem>();
        SystemHandle targetingSystem = _world.CreateSystem<AITargetingSystem>();
        SystemHandle combatSystem = _world.CreateSystem<AICombatOrderSystem>();

        if (assertDiagnosticLog)
            LogAssert.Expect(LogType.Log, new Regex(@"\[AIBuild\] faction=2 building=Tent_Regular cell=int2\(\d+, \d+\) cost=20000 result=Requested"));
        buildSystem.Update(_world.Unmanaged);
        logFlushSystem.Update(_world.Unmanaged);
        if (assertDiagnosticLog)
            LogAssert.NoUnexpectedReceived();
        ProcessPendingRuntimeSpawnRequests(em);
        RuntimeGameplayStateTestHelper.PublishBuildingRuntimeState(em, TickBuildingRuntime);

        if (assertDiagnosticLog)
            LogAssert.Expect(LogType.Log, new Regex(@"\[AIBuild\] faction=2 building=Tent_Regular cell=int2\(\d+, \d+\) cost=20000 result=Placed"));
        buildSystem.Update(_world.Unmanaged);
        logFlushSystem.Update(_world.Unmanaged);
        if (assertDiagnosticLog)
            LogAssert.NoUnexpectedReceived();
        RuntimeGameplayStateTestHelper.PublishBuildingRuntimeState(em, TickBuildingRuntime);
        Assert.AreEqual(1, RuntimeGameplayStateTestHelper.CountRuntimeBuildingsForFaction(em, FactionIdentity.EnemyFactionId, "Tent_Regular"));

        if (assertDiagnosticLog)
            LogAssert.Expect(LogType.Log, new Regex(@"\[AIProduction\] faction=2 unit=Rifleman cost=10000 result=Requested"));
        productionSystem.Update(_world.Unmanaged);
        logFlushSystem.Update(_world.Unmanaged);
        if (assertDiagnosticLog)
            LogAssert.NoUnexpectedReceived();
        SetPrivateField(
            _buildingGameplay.RuntimeCitySpawnContext.RuntimeBoundarySystem,
            "_nextProductionRequestProbeAt",
            0f);
        RuntimeGameplayStateTestHelper.PublishBuildingRuntimeState(em, TickBuildingRuntime);

        if (assertDiagnosticLog)
            LogAssert.Expect(LogType.Log, new Regex(@"\[AIProduction\] faction=2 producer=Tent_Regular unit=Rifleman cost=10000 queue=1 result=Queued"));
        productionSystem.Update(_world.Unmanaged);
        logFlushSystem.Update(_world.Unmanaged);
        if (assertDiagnosticLog)
            LogAssert.NoUnexpectedReceived();
        RuntimeGameplayStateTestHelper.PublishBuildingRuntimeState(em, TickBuildingRuntime);
        RuntimeGameplayStateTestHelper.PublishBuildingRuntimeState(em, TickBuildingRuntime);
        Assert.AreEqual(
            1,
            RuntimeGameplayStateTestHelper.CountPendingProductionsForFaction(em, FactionIdentity.EnemyFactionId, "Rifleman"),
            RuntimeGameplayStateTestHelper.DescribeUnitProductionBoundary(em));

        if (assertDiagnosticLog)
            LogAssert.Expect(LogType.Log, new Regex(@"\[AISquad\] faction=2 squad=1 purpose=Attack units=4 targetFaction=1 targetCell=int2\(50, 50\)"));
        squadSystem.Update(_world.Unmanaged);
        logFlushSystem.Update(_world.Unmanaged);
        if (assertDiagnosticLog)
            LogAssert.NoUnexpectedReceived();

        Entity squadEntity = GetSingleSquad(em);
        AISquad squad = em.GetComponentData<AISquad>(squadEntity);
        Assert.AreEqual(1, squad.SquadId);
        Assert.AreEqual(4, em.GetBuffer<AISquadUnit>(squadEntity).Length);

        if (assertDiagnosticLog)
            LogAssert.Expect(LogType.Log, new Regex(@"\[AITarget\] faction=2 squad=1 target=Threat score=\d+ reason=Threat targetFaction=1 targetCell=int2\(50, 50\)"));
        targetingSystem.Update(_world.Unmanaged);
        logFlushSystem.Update(_world.Unmanaged);
        if (assertDiagnosticLog)
            LogAssert.NoUnexpectedReceived();

        squad = em.GetComponentData<AISquad>(squadEntity);
        Assert.AreEqual(target, squad.TargetEntity);
        Assert.AreEqual((byte)AITargetKind.Threat, squad.TargetKind);

        if (assertDiagnosticLog)
            LogAssert.Expect(LogType.Log, new Regex(@"\[AICombat\] faction=2 squad=1 order=Attack target=Entity\(\d+:\d+\) units=4"));
        combatSystem.Update(_world.Unmanaged);
        logFlushSystem.Update(_world.Unmanaged);
        if (assertDiagnosticLog)
            LogAssert.NoUnexpectedReceived();

        AssertEngageOrder(em, unitA, target);
        AssertEngageOrder(em, unitB, target);
        AssertEngageOrder(em, unitC, target);
        AssertEngageOrder(em, unitD, target);

        FactionEconomy economy = em.GetComponentData<FactionEconomy>(economyEntity);
        Assert.AreEqual(70000, economy.Money);
    }

    private void CreateBuildingPlacementHarness()
    {
        _unitPrefab = CreateUnitPrefab("Rifleman", 10000);
        _buildingPrefab = CreateBuildingPrefab("Tent_Regular", 2, 2, 20000, _unitPrefab);

        _unitRegistryConfig = ScriptableObject.CreateInstance<UnitPrefabRegistryAuthoringConfig>();
        _unitRegistryConfig.UnitSpawnPrefabs.Add(_unitPrefab);

        _buildingConfig = ScriptableObject.CreateInstance<BuildingPlacementSystemConfig>();
        SetPrivateField(_buildingConfig, "spawnables", new List<GameObject> { _buildingPrefab });
        SetPrivateField(_buildingConfig, "unitPrefabRegistryConfig", _unitRegistryConfig);

        _runtimeRoot = new GameObject("AIEndToEnd_RuntimeRoot");
        _buildingComposition = new BuildingGameplayCompositionSystemHelper();
        _buildingGameplay = _buildingComposition.Initialize(
            buildingPlacementConfig: _buildingConfig,
            worldCamera: null,
            runtimeTransportsRoot: _runtimeRoot.transform,
            runtimeUiRoot: _runtimeRoot.transform,
            roadFootprintState: default,
            factionVisuals: null,
            dayNight: null,
            resolveSpawnableLookupKey: BuildingSpawnPrefabLookupKeyPrefabSystemHelper.ResolveSpawnableLookupKey,
            tryGetBuildingDefinitionMetadata: BuildingDefinitionAuthoringMetadataPrefabSystemHelper.TryGetBuildingDefinitionMetadata,
            tryGetUnitDefinitionMetadata: BuildingDefinitionAuthoringMetadataPrefabSystemHelper.TryGetUnitDefinitionMetadata);
        _buildingGameplayInitialized = true;
    }

    private void TickBuildingRuntime()
    {
        if (_buildingGameplayInitialized)
            _buildingGameplay.RuntimeUpdate.Update(_buildingGameplay.RuntimeUpdateContext);
    }

    private void ProcessPendingRuntimeSpawnRequests(EntityManager entityManager)
    {
        using EntityQuery boundaryQuery = entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<BuildingRuntimeStateTag>(),
            ComponentType.ReadWrite<BuildingRuntimeSpawnRequest>());
        Entity boundaryEntity = boundaryQuery.GetSingletonEntity();
        BuildingRuntimeCitySpawnBridgeCompositionSystemHelper.Context context =
            _buildingGameplay.RuntimeCitySpawnContext;
        context.RuntimeBoundarySystem.ProcessRuntimeSpawnRequestsForBoundary(
            context.DefinitionSystem,
            context.RuntimeSpawnCommandContext.RuntimeSpawnSystem,
            context.RuntimeSpawnCommandContext.SpawnContext,
            entityManager,
            boundaryEntity);
    }

    private static void CreateBuildPlan(EntityManager em)
    {
        Entity planEntity = em.CreateEntity(typeof(AIBuildPlan));
        em.SetComponentData(planEntity, new AIBuildPlan
        {
            FactionId = FactionIdentity.EnemyFactionId,
            Enabled = 1,
            BaseCenterCell = new int2(30, 30),
            BuildIntervalSeconds = 1f,
            LastBuildTime = -999f,
            LastLogTime = -999f
        });
        DynamicBuffer<AIBuildPlanEntry> entries = em.AddBuffer<AIBuildPlanEntry>(planEntity);
        entries.Add(new AIBuildPlanEntry { BuildingId = new FixedString64Bytes("Tent_Regular") });
    }

    private static void CreateProductionPlan(EntityManager em)
    {
        Entity planEntity = em.CreateEntity(typeof(AIProductionPlan));
        em.SetComponentData(planEntity, new AIProductionPlan
        {
            FactionId = FactionIdentity.EnemyFactionId,
            Enabled = 1,
            TargetProducedUnits = 1,
            MaxQueuedUnits = 1,
            UnitProductionIntervalSeconds = 1f,
            LastProductionTime = -999f,
            LastLogTime = -999f
        });
        DynamicBuffer<AIProductionPlanEntry> entries = em.AddBuffer<AIProductionPlanEntry>(planEntity);
        entries.Add(new AIProductionPlanEntry { UnitId = new FixedString64Bytes(BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey("Rifleman")) });
    }

    private static void CreateSquadPlan(EntityManager em)
    {
        Entity planEntity = em.CreateEntity(typeof(AISquadPlan));
        em.SetComponentData(planEntity, new AISquadPlan
        {
            FactionId = FactionIdentity.EnemyFactionId,
            Enabled = 1,
            MinUnits = 4,
            MaxUnits = 4,
            MaxActiveSquads = 1,
            NextSquadId = 1,
            LastLogTime = -999f
        });
    }

    private void CreateGrid(EntityManager em, int width, int height)
    {
        int gridSize = width * height;
        _blockerCounts = new NativeArray<int>(gridSize, Allocator.Persistent);
        _blocked = new NativeBitArray(gridSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        _occupied = new NativeBitArray(gridSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        _friendlyPassFactionIds = new NativeArray<byte>(gridSize, Allocator.Persistent);

        Entity gridEntity = em.CreateEntity(typeof(GridConfig), typeof(DynamicBlockerComponent), typeof(DynamicOccupancyComponent));
        em.SetComponentData(gridEntity, new GridConfig { Width = width, Height = height, CellSize = 1f, Origin = float3.zero });
        em.SetComponentData(gridEntity, new DynamicBlockerComponent
        {
            GridSize = gridSize,
            Counts = _blockerCounts,
            Blocked = _blocked,
            FriendlyPassFactionIds = _friendlyPassFactionIds
        });
        em.SetComponentData(gridEntity, new DynamicOccupancyComponent
        {
            GridSize = gridSize,
            Occupied = _occupied
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

    private static Entity CreateAttacker(EntityManager em, byte factionId, int2 cell, float3 position)
    {
        Entity entity = em.CreateEntity(
            typeof(Faction),
            typeof(UnitGrid),
            typeof(UnitHealth),
            typeof(UnitCombat),
            typeof(UnitAttack),
            typeof(AIControlledTag),
            typeof(LocalTransform));
        em.SetComponentData(entity, new Faction { Id = factionId });
        em.SetComponentData(entity, new UnitGrid { Cell = cell });
        em.SetComponentData(entity, new UnitHealth { Current = 100, Max = 100 });
        em.SetComponentData(entity, new UnitCombat { CanAttack = 1, AutoEngage = 1 });
        em.SetComponentData(entity, new UnitAttack { Range = 4f, CooldownSeconds = 1f, Damage = 10 });
        em.SetComponentData(entity, LocalTransform.FromPosition(position));
        return entity;
    }

    private static Entity CreateTarget(EntityManager em, byte factionId, int2 cell, float3 position)
    {
        Entity entity = em.CreateEntity(
            typeof(Faction),
            typeof(UnitGrid),
            typeof(UnitHealth),
            typeof(UnitCombat),
            typeof(UnitAttack),
            typeof(LocalTransform));
        em.SetComponentData(entity, new Faction { Id = factionId });
        em.SetComponentData(entity, new UnitGrid { Cell = cell });
        em.SetComponentData(entity, new UnitHealth { Current = 500, Max = 500 });
        em.SetComponentData(entity, new UnitCombat { CanAttack = 1, AutoEngage = 1 });
        em.SetComponentData(entity, new UnitAttack { Range = 4f, CooldownSeconds = 1f, Damage = 10 });
        em.SetComponentData(entity, LocalTransform.FromPosition(position));
        return entity;
    }

    private static Entity GetSingleSquad(EntityManager em)
    {
        EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<AISquad>(), ComponentType.ReadOnly<AISquadUnit>());
        using NativeArray<Entity> squads = query.ToEntityArray(Allocator.Temp);
        query.Dispose();

        Assert.AreEqual(1, squads.Length);
        return squads[0];
    }

    private static void AssertEngageOrder(EntityManager em, Entity unit, Entity target)
    {
        Assert.IsTrue(em.HasComponent<EngageTarget>(unit));
        EngageTarget order = em.GetComponentData<EngageTarget>(unit);
        Assert.AreEqual(target, order.Target);
        Assert.AreEqual(new int2(50, 50), order.Cell);
        Assert.AreEqual(1, order.IsCommanded);
        Assert.IsTrue(em.HasComponent<AICombatOrderTag>(unit));
    }

    private static GameObject CreateBuildingPrefab(string id, int width, int height, int price, GameObject producedUnitPrefab)
    {
        GameObject prefab = new(id);
        BuildingDefinitionAuthoring authoring = prefab.AddComponent<BuildingDefinitionAuthoring>();
        SetPrivateField(authoring, "displayName", id);
        SetPrivateField(authoring, "description", "AI end-to-end test producer.");
        SetPrivateField(authoring, "footprintCells", new Vector2Int(width, height));
        SetPrivateField(authoring, "maxHealth", 500);
        SetPrivateField(authoring, "canRequest", true);
        SetPrivateField(authoring, "price", price);
        SetPrivateField(authoring, "productions", new List<BuildingDefinitionAuthoring.ProductionDefinition>
        {
            new() { spawnUnitPrefab = producedUnitPrefab }
        });
        return prefab;
    }

    private static GameObject CreateUnitPrefab(string id, int price)
    {
        GameObject prefab = new(id);
        UnitGridAuthoring authoring = prefab.AddComponent<UnitGridAuthoring>();
        SetPrivateField(authoring, "displayName", id);
        SetPrivateField(authoring, "description", "AI end-to-end test unit.");
        SetPrivateField(authoring, "footprintCells", Vector2Int.one);
        SetPrivateField(authoring, "canRequest", true);
        SetPrivateField(authoring, "price", price);
        SetPrivateField(authoring, "productionDurationSeconds", 60f);
        return prefab;
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = null;
        for (System.Type type = target.GetType(); type != null && field == null; type = type.BaseType)
            field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(field, $"Missing field '{fieldName}' on {target.GetType().Name}.");
        field.SetValue(target, value);
    }
}
