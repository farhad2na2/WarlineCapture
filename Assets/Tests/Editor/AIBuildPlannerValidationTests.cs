using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class AIBuildPlannerValidationTests
{
    private World _previousDefaultWorld;
    private World _world;
    private BuildingGameplayCompositionSystem _buildingComposition;
    private BuildingGameplayResultCompositionSystemHelper.Result _buildingGameplay;
    private bool _buildingGameplayInitialized;
    private NativeArray<int> _blockerCounts;
    private NativeBitArray _blocked;
    private NativeBitArray _occupied;
    private NativeArray<byte> _friendlyPassFactionIds;
    private GameObject _runtimeRoot;
    private GameObject _buildingPrefab;
    private BuildingPlacementSystemConfig _buildingConfig;

    public static void RunFocusedValidation()
    {
        var tests = new AIBuildPlannerValidationTests();
        try
        {
            tests.SetUp();
            tests.AssertPlacesConfiguredBuildingAndSpendsFactionMoney(assertDiagnosticLog: false);
            UnityEngine.Debug.Log("[AIBuildPlannerFocusedValidation] result=Passed tests=1");
        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogException(ex);
            UnityEngine.Debug.LogError("[AIBuildPlannerFocusedValidation] result=Failed");
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
        if (_buildingConfig != null)
            Object.DestroyImmediate(_buildingConfig);
    }

    [Test]
    public void AIBuildPlannerSystem_PlacesConfiguredBuildingAndSpendsFactionMoney()
    {
        AssertPlacesConfiguredBuildingAndSpendsFactionMoney(assertDiagnosticLog: true);
    }

    private void AssertPlacesConfiguredBuildingAndSpendsFactionMoney(bool assertDiagnosticLog)
    {
        _previousDefaultWorld = World.DefaultGameObjectInjectionWorld;
        _world = new World("AIBuildPlannerValidationTests");
        World.DefaultGameObjectInjectionWorld = _world;
        EntityManager em = _world.EntityManager;

        CreateGrid(em, 64, 64);
        _buildingPrefab = CreateBuildingPrefab("Tent_Regular", 2, 2, 20000);
        _buildingConfig = ScriptableObject.CreateInstance<BuildingPlacementSystemConfig>();
        SetPrivateField(_buildingConfig, "spawnables", new System.Collections.Generic.List<GameObject> { _buildingPrefab });

        _runtimeRoot = new GameObject("AIBuildPlanner_RuntimeRoot");
        _buildingComposition = new BuildingGameplayCompositionSystem();
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
        RuntimeGameplayStateTestHelper.SetBuildingPlacement(em, TickBuildingRuntime);

        Entity economyEntity = em.CreateEntity(typeof(FactionEconomy), typeof(FactionEconomyPolicy));
        em.SetComponentData(economyEntity, new FactionEconomy { FactionId = 1, Money = 30000, LastLogTime = -999f });
        em.SetComponentData(economyEntity, new FactionEconomyPolicy { Enabled = 1, SellIntervalSeconds = 8f });

        Entity controlEntity = em.CreateEntity(typeof(FactionControlConfigTag));
        DynamicBuffer<FactionControlEntry> controls = em.AddBuffer<FactionControlEntry>(controlEntity);
        controls.Add(new FactionControlEntry { FactionId = 1, AIControlled = 1 });

        Entity planEntity = em.CreateEntity(typeof(AIBuildPlan));
        em.SetComponentData(planEntity, new AIBuildPlan
        {
            FactionId = 1,
            Enabled = 1,
            BaseCenterCell = new int2(24, 24),
            BuildIntervalSeconds = 1f,
            LastBuildTime = -999f,
            LastLogTime = -999f
        });
        DynamicBuffer<AIBuildPlanEntry> entries = em.AddBuffer<AIBuildPlanEntry>(planEntity);
        entries.Add(new AIBuildPlanEntry { BuildingId = new FixedString64Bytes("Tent_Regular") });

        RuntimeGameplayStateTestHelper.SetPlayRequested(em, true);
        RuntimeGameplayStateTestHelper.PublishBuildingRuntimeBoundary(em, TickBuildingRuntime);
        SystemHandle system = _world.CreateSystem<AIBuildPlannerSystem>();
        SystemHandle logFlushSystem = _world.CreateSystem<AIDiagnosticLogFlushSystem>();

        if (assertDiagnosticLog)
            LogAssert.Expect(LogType.Log, new Regex(@"\[AIBuild\] faction=1 building=Tent_Regular cell=int2\(\d+, \d+\) cost=20000 result=Requested"));
        system.Update(_world.Unmanaged);
        logFlushSystem.Update(_world.Unmanaged);
        if (assertDiagnosticLog)
            LogAssert.NoUnexpectedReceived();
        RuntimeGameplayStateTestHelper.PublishBuildingRuntimeBoundary(em, TickBuildingRuntime);

        if (assertDiagnosticLog)
            LogAssert.Expect(LogType.Log, new Regex(@"\[AIBuild\] faction=1 building=Tent_Regular cell=int2\(\d+, \d+\) cost=20000 result=Placed"));
        system.Update(_world.Unmanaged);
        logFlushSystem.Update(_world.Unmanaged);
        if (assertDiagnosticLog)
            LogAssert.NoUnexpectedReceived();

        FactionEconomy economy = em.GetComponentData<FactionEconomy>(economyEntity);
        Assert.AreEqual(10000, economy.Money);
        RuntimeGameplayStateTestHelper.PublishBuildingRuntimeBoundary(em, TickBuildingRuntime);
        Assert.AreEqual(1, RuntimeGameplayStateTestHelper.CountRuntimeBuildingsForFaction(em, (byte)1, "Tent_Regular"));

        AIBuildPlan plan = em.GetComponentData<AIBuildPlan>(planEntity);
        Assert.AreEqual(1, plan.NextBuildIndex);
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

    private static GameObject CreateBuildingPrefab(string id, int width, int height, int price)
    {
        GameObject prefab = new(id);
        BuildingDefinitionAuthoring authoring = prefab.AddComponent<BuildingDefinitionAuthoring>();
        SetPrivateField(authoring, "displayName", id);
        SetPrivateField(authoring, "description", "AI test building.");
        SetPrivateField(authoring, "footprintCells", new Vector2Int(width, height));
        SetPrivateField(authoring, "maxHealth", 500);
        SetPrivateField(authoring, "canRequest", true);
        SetPrivateField(authoring, "price", price);
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
