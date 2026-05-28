using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class AIProductionValidationTests
{
    private World _previousDefaultWorld;
    private World _world;
    private BuildingGameplayCompositionSystem _buildingComposition;
    private BuildingGameplayCompositionResultSystem.Result _buildingGameplay;
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

    [SetUp]
    public void SetUp()
    {
        InitialUnitsRuntimeState.VerboseAILogs = true;
    }

    [TearDown]
    public void TearDown()
    {
        InitialUnitsRuntimeState.PlayRequested = false;
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
    public void AIProductionSystem_QueuesUnitFromOwnedProducerAndSpendsFactionMoney()
    {
        _previousDefaultWorld = World.DefaultGameObjectInjectionWorld;
        _world = new World("AIProductionValidationTests");
        World.DefaultGameObjectInjectionWorld = _world;
        EntityManager em = _world.EntityManager;

        CreateGrid(em, 64, 64);
        _unitPrefab = CreateUnitPrefab("Rifleman", 10000);
        _buildingPrefab = CreateBuildingPrefab("Tent_Regular", 2, 2, 20000, _unitPrefab);

        _unitRegistryConfig = ScriptableObject.CreateInstance<UnitPrefabRegistryAuthoringConfig>();
        _unitRegistryConfig.UnitSpawnPrefabs.Add(_unitPrefab);

        _buildingConfig = ScriptableObject.CreateInstance<BuildingPlacementSystemConfig>();
        SetPrivateField(_buildingConfig, "spawnables", new List<GameObject> { _buildingPrefab });
        SetPrivateField(_buildingConfig, "unitPrefabRegistryConfig", _unitRegistryConfig);

        _runtimeRoot = new GameObject("AIProduction_RuntimeRoot");
        _buildingComposition = new BuildingGameplayCompositionSystem();
        _buildingGameplay = _buildingComposition.Initialize(
            _buildingConfig,
            null,
            _runtimeRoot.transform,
            null,
            default,
            null,
            null);
        _buildingGameplayInitialized = true;
        RuntimeGameplayStateTestHelper.SetBuildingPlacement(em, TickBuildingRuntime);

        Assert.IsTrue(_buildingGameplay.RuntimeSpawnCommand.TrySpawnRuntimeBuilding(
            _buildingGameplay.RuntimeSpawnCommandContext,
            _buildingPrefab,
            new Vector2Int(24, 24),
            out _,
            out _,
            out _,
            "Tent_Regular",
            "AI test producer.",
            null,
            500,
            false,
            1));

        Entity economyEntity = em.CreateEntity(typeof(FactionEconomy), typeof(FactionEconomyPolicy));
        em.SetComponentData(economyEntity, new FactionEconomy { FactionId = 1, Money = 50000, LastLogTime = -999f });
        em.SetComponentData(economyEntity, new FactionEconomyPolicy { Enabled = 1, SellIntervalSeconds = 8f });

        Entity controlEntity = em.CreateEntity(typeof(FactionControlConfigTag));
        DynamicBuffer<FactionControlEntry> controls = em.AddBuffer<FactionControlEntry>(controlEntity);
        controls.Add(new FactionControlEntry { FactionId = 1, AIControlled = 1 });

        Entity planEntity = em.CreateEntity(typeof(AIProductionPlan));
        em.SetComponentData(planEntity, new AIProductionPlan
        {
            FactionId = 1,
            Enabled = 1,
            TargetProducedUnits = 3,
            MaxQueuedUnits = 3,
            UnitProductionIntervalSeconds = 1f,
            LastProductionTime = -999f,
            LastLogTime = -999f
        });
        DynamicBuffer<AIProductionPlanEntry> entries = em.AddBuffer<AIProductionPlanEntry>(planEntity);
        entries.Add(new AIProductionPlanEntry { UnitId = new FixedString64Bytes("Rifleman") });

        RuntimeGameplayStateTestHelper.SetPlayRequested(em, true);
        RuntimeGameplayStateTestHelper.PublishBuildingRuntimeBoundary(em, TickBuildingRuntime);
        SystemHandle system = _world.CreateSystem<AIProductionSystem>();
        SystemHandle logFlushSystem = _world.CreateSystem<AIDiagnosticLogFlushSystem>();

        LogAssert.Expect(LogType.Log, new Regex(@"\[AIProduction\] faction=1 unit=Rifleman cost=10000 result=Requested"));
        system.Update(_world.Unmanaged);
        logFlushSystem.Update(_world.Unmanaged);
        LogAssert.NoUnexpectedReceived();

        RuntimeGameplayStateTestHelper.PublishBuildingRuntimeBoundary(em, TickBuildingRuntime);
        LogAssert.Expect(LogType.Log, new Regex(@"\[AIProduction\] faction=1 producer=Tent_Regular unit=Rifleman cost=10000 queue=1 result=Queued"));
        system.Update(_world.Unmanaged);
        logFlushSystem.Update(_world.Unmanaged);
        LogAssert.NoUnexpectedReceived();

        RuntimeGameplayStateTestHelper.PublishBuildingRuntimeBoundary(em, TickBuildingRuntime);
        FactionEconomy economy = em.GetComponentData<FactionEconomy>(economyEntity);
        Assert.AreEqual(40000, economy.Money);
        Assert.AreEqual(1, RuntimeGameplayStateTestHelper.CountPendingProductionsForFaction(em, (byte)1, "Rifleman"));

        AIProductionPlan plan = em.GetComponentData<AIProductionPlan>(planEntity);
        Assert.AreEqual(1, plan.NextUnitIndex);
    }

    private void TickBuildingRuntime()
    {
        if (_buildingGameplayInitialized)
            _buildingGameplay.RuntimeUpdate.Update(_buildingGameplay.RuntimeUpdateContext);
    }

    private void CreateGrid(EntityManager em, int width, int height)
    {
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

        DynamicBuffer<GridRoad> roads = em.AddBuffer<GridRoad>(gridEntity);
        roads.ResizeUninitialized(gridSize);
        for (int i = 0; i < roads.Length; i++)
            roads[i] = new GridRoad { Value = 0 };

        DynamicBuffer<GridWalkable> walkable = em.AddBuffer<GridWalkable>(gridEntity);
        walkable.ResizeUninitialized(gridSize);
        for (int i = 0; i < walkable.Length; i++)
            walkable[i] = new GridWalkable { Value = 1 };
    }

    private static GameObject CreateBuildingPrefab(string id, int width, int height, int price, GameObject producedUnitPrefab)
    {
        GameObject prefab = new(id);
        BuildingDefinitionAuthoring authoring = prefab.AddComponent<BuildingDefinitionAuthoring>();
        SetPrivateField(authoring, "displayName", id);
        SetPrivateField(authoring, "description", "AI test producer.");
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
        SetPrivateField(authoring, "description", "AI test unit.");
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
