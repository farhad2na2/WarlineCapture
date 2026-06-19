#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public sealed class CustomGameStartupSystemTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            var tests = new CustomGameStartupSystemTests();
            tests.InitializeFromLegacyConfigsCreatesStartupEntityAndBuffers();
            tests.InitializeFromLegacyConfigsResetsInitialSpawnLifecycleAndRequests();
            tests.InitializeFromLegacyConfigsKeepsFaction2TentBuildingKey();
            tests.InitializeCreatesSourceKeyStartupBuffers();
            Debug.Log("[CustomGameStartupFocusedValidation] result=Passed tests=4");
        }
        catch (Exception exception)
        {
            Debug.LogError("[CustomGameStartupFocusedValidation] result=Failed");
            Debug.LogException(exception);
            throw;
        }
    }

    [Test]
    public void InitializeFromLegacyConfigsCreatesStartupEntityAndBuffers()
    {
        using var world = new World("CustomGameStartupSystemTests");
        CustomGameStartupSystem system = world.GetOrCreateSystemManaged<CustomGameStartupSystem>();

        CustomGameStartupSystem.Result result = system.InitializeFromLegacyConfigs(null, null);

        Assert.IsTrue(result.Initialized);
        Assert.AreEqual(0, result.FactionCount);
        Assert.AreEqual(0, result.InitialUnitEntryCount);
        Assert.AreEqual(0, result.InitialBuildingEntryCount);
        Assert.AreEqual(0, result.UnitRegistryEntryCount);
        Assert.AreEqual(0, result.VisualEntryCount);

        EntityManager em = world.EntityManager;
        using EntityQuery query = em.CreateEntityQuery(
            ComponentType.ReadOnly<CustomGameStartupStateComponent>(),
            ComponentType.ReadOnly<InitialUnitsSpawnConfig>());
        Assert.AreEqual(1, query.CalculateEntityCount());

        Entity entity = query.GetSingletonEntity();
        Assert.IsTrue(em.HasBuffer<UnitPrefabRegistryEntry>(entity));
        Assert.IsTrue(em.HasBuffer<InitialUnitsFactionSpawnEntry>(entity));
        Assert.IsTrue(em.HasBuffer<InitialUnitsFactionUnitSpawnEntry>(entity));
        Assert.IsTrue(em.HasBuffer<InitialUnitsFactionBuildingSpawnEntry>(entity));
        Assert.IsTrue(em.HasBuffer<CustomGameFactionUnitSourceSpawnEntry>(entity));
        Assert.IsTrue(em.HasBuffer<CustomGameUnitSourceRegistryEntry>(entity));
        Assert.IsTrue(em.HasBuffer<CustomGameVisualRegistryEntry>(entity));

        CustomGameStartupStateComponent state = em.GetComponentData<CustomGameStartupStateComponent>(entity);
        Assert.AreEqual(new FixedString64Bytes("custom.skirmish.legacy"), state.GameModeId);
    }

    [Test]
    public void InitializeFromLegacyConfigsResetsInitialSpawnLifecycleAndRequests()
    {
        using var world = new World("CustomGameStartupLifecycleResetTests");
        CustomGameStartupSystem system = world.GetOrCreateSystemManaged<CustomGameStartupSystem>();
        EntityManager em = world.EntityManager;

        Entity startupEntity = em.CreateEntity(
            typeof(CustomGameStartupStateComponent),
            typeof(InitialUnitsSpawnConfig),
            typeof(InitialUnitsSpawnInitialized));
        em.AddComponentData(startupEntity, new InitialUnitsSpawnProgress
        {
            RandomState = 17u,
            InitialBuildingRequestsIssued = 1,
            InitialBuildingsSpawned = 1
        });
        DynamicBuffer<InitialUnitsFactionUnitSpawnProgress> unitProgress =
            em.AddBuffer<InitialUnitsFactionUnitSpawnProgress>(startupEntity);
        unitProgress.Add(new InitialUnitsFactionUnitSpawnProgress { Spawned = 9 });

        Entity unrelatedPlan = em.CreateEntity();
        Entity boundary = em.CreateEntity(typeof(BuildingRuntimeBoundaryTag));
        DynamicBuffer<BuildingRuntimeSpawnRequest> requests = em.AddBuffer<BuildingRuntimeSpawnRequest>(boundary);
        requests.Add(new BuildingRuntimeSpawnRequest
        {
            PlanEntity = startupEntity,
            BuildingId = new FixedString128Bytes("stale_tent_request"),
            Status = BuildingRuntimeSpawnRequest.Pending
        });
        requests.Add(new BuildingRuntimeSpawnRequest
        {
            PlanEntity = unrelatedPlan,
            BuildingId = new FixedString128Bytes("unrelated_request"),
            Status = BuildingRuntimeSpawnRequest.Pending
        });

        CustomGameStartupSystem.Result result = system.InitializeFromLegacyConfigs(null, null);

        Assert.IsTrue(result.Initialized);
        Assert.IsFalse(em.HasComponent<InitialUnitsSpawnInitialized>(startupEntity));
        Assert.IsFalse(em.HasComponent<InitialUnitsSpawnProgress>(startupEntity));
        Assert.IsFalse(em.HasBuffer<InitialUnitsFactionUnitSpawnProgress>(startupEntity));

        DynamicBuffer<BuildingRuntimeSpawnRequest> remainingRequests =
            em.GetBuffer<BuildingRuntimeSpawnRequest>(boundary, true);
        Assert.AreEqual(1, remainingRequests.Length);
        Assert.AreEqual(unrelatedPlan, remainingRequests[0].PlanEntity);
        Assert.AreEqual(new FixedString128Bytes("unrelated_request"), remainingRequests[0].BuildingId);
    }

    [Test]
    public void InitializeFromLegacyConfigsKeepsFaction2TentBuildingKey()
    {
        using var world = new World("CustomGameStartupFaction2TentTests");
        CustomGameStartupSystem system = world.GetOrCreateSystemManaged<CustomGameStartupSystem>();
        InitialUnitsSpawnerAuthoringConfig initialConfig =
            ScriptableObject.CreateInstance<InitialUnitsSpawnerAuthoringConfig>();
        GameObject tentPrefab = new("Tent_Regular");
        try
        {
            InitialUnitsSpawnerAuthoringConfig.FactionBuildingEntry building = new();
            SetPrivateField(building, "prefab", tentPrefab);
            SetPrivateField(building, "originOffset", new Vector2Int(250, 150));

            InitialUnitsSpawnerAuthoringConfig.FactionEntry faction = new();
            SetPrivateField(faction, "factionId", 2);
            SetPrivateField(faction, "spawnCell", new Vector2Int(150, 50));
            SetPrivateField(
                faction,
                "buildings",
                new List<InitialUnitsSpawnerAuthoringConfig.FactionBuildingEntry> { building });

            SetPrivateField(
                initialConfig,
                "factions",
                new List<InitialUnitsSpawnerAuthoringConfig.FactionEntry> { faction });

            CustomGameStartupSystem.Result result = system.InitializeFromLegacyConfigs(initialConfig, null);

            Assert.IsTrue(result.Initialized);
            Assert.AreEqual(1, result.FactionCount);
            Assert.AreEqual(1, result.InitialBuildingEntryCount);

            EntityManager em = world.EntityManager;
            using EntityQuery query = em.CreateEntityQuery(
                ComponentType.ReadOnly<CustomGameStartupStateComponent>(),
                ComponentType.ReadOnly<InitialUnitsSpawnConfig>());
            Assert.AreEqual(1, query.CalculateEntityCount());

            Entity startupEntity = query.GetSingletonEntity();
            DynamicBuffer<InitialUnitsFactionBuildingSpawnEntry> buildingSpawns =
                em.GetBuffer<InitialUnitsFactionBuildingSpawnEntry>(startupEntity, true);
            Assert.AreEqual(1, buildingSpawns.Length);
            Assert.AreEqual(2, buildingSpawns[0].FactionId);
            Assert.AreEqual(new FixedString128Bytes("tent_regular"), buildingSpawns[0].PrefabLookupKey);
            Assert.AreEqual(new int2(250, 150), buildingSpawns[0].OriginOffset);
            Assert.AreNotEqual(new FixedString128Bytes("building_oilpump"), buildingSpawns[0].PrefabLookupKey);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(tentPrefab);
            UnityEngine.Object.DestroyImmediate(initialConfig);
        }
    }

    [Test]
    public void InitializeCreatesSourceKeyStartupBuffers()
    {
        using var world = new World("CustomGameStartupSourceKeyBufferTests");
        CustomGameStartupSystem system = world.GetOrCreateSystemManaged<CustomGameStartupSystem>();
        CustomGameStartupConfig startupConfig = ScriptableObject.CreateInstance<CustomGameStartupConfig>();
        CustomGameFactionConfig factionConfig = ScriptableObject.CreateInstance<CustomGameFactionConfig>();
        CustomGameUnitRosterConfig unitRosterConfig = ScriptableObject.CreateInstance<CustomGameUnitRosterConfig>();
        try
        {
            CustomGameFactionConfig.UnitSpawnEntry unitSpawn = new();
            SetPrivateField(unitSpawn, "sourceKey", "Unit_Veh_Tank_USA");
            SetPrivateField(unitSpawn, "count", 2);
            SetPrivateField(unitSpawn, "spawnOffset", new Vector2Int(3, -1));

            CustomGameFactionConfig.BuildingSpawnEntry buildingSpawn = new();
            SetPrivateField(buildingSpawn, "lookupKey", "Building_Ammunition_Depot");
            SetPrivateField(buildingSpawn, "originOffset", new Vector2Int(5, 7));

            CustomGameFactionConfig.FactionEntry faction = new();
            SetPrivateField(faction, "factionId", 4);
            SetPrivateField(faction, "spawnCell", new Vector2Int(40, 50));
            SetPrivateField(
                faction,
                "units",
                new List<CustomGameFactionConfig.UnitSpawnEntry> { unitSpawn });
            SetPrivateField(
                faction,
                "buildings",
                new List<CustomGameFactionConfig.BuildingSpawnEntry> { buildingSpawn });
            SetPrivateField(
                factionConfig,
                "factions",
                new List<CustomGameFactionConfig.FactionEntry> { faction });

            CustomGameUnitRosterConfig.UnitEntry unitRoster = new();
            SetPrivateField(unitRoster, "sourceKey", "Unit_Veh_Tank_USA");
            SetPrivateField(unitRoster, "displayName", "USA Tank");
            SetPrivateField(
                unitRosterConfig,
                "units",
                new List<CustomGameUnitRosterConfig.UnitEntry> { unitRoster });

            SetPrivateField(startupConfig, "gameModeId", "custom.test.sourcekeys");
            SetPrivateField(startupConfig, "factionConfig", factionConfig);
            SetPrivateField(startupConfig, "unitRosterConfig", unitRosterConfig);

            CustomGameStartupSystem.Result result = system.Initialize(startupConfig);

            Assert.IsTrue(result.Initialized);
            Assert.AreEqual(1, result.FactionCount);
            Assert.AreEqual(1, result.InitialUnitEntryCount);
            Assert.AreEqual(1, result.InitialBuildingEntryCount);
            Assert.AreEqual(1, result.UnitRegistryEntryCount);

            EntityManager em = world.EntityManager;
            using EntityQuery query = em.CreateEntityQuery(
                ComponentType.ReadOnly<CustomGameStartupStateComponent>(),
                ComponentType.ReadOnly<InitialUnitsSpawnConfig>());
            Assert.AreEqual(1, query.CalculateEntityCount());
            Entity entity = query.GetSingletonEntity();

            CustomGameStartupStateComponent state = em.GetComponentData<CustomGameStartupStateComponent>(entity);
            Assert.AreEqual(new FixedString64Bytes("custom.test.sourcekeys"), state.GameModeId);

            DynamicBuffer<InitialUnitsFactionSpawnEntry> factionSpawns =
                em.GetBuffer<InitialUnitsFactionSpawnEntry>(entity, true);
            Assert.AreEqual(1, factionSpawns.Length);
            Assert.AreEqual(4, factionSpawns[0].FactionId);
            Assert.AreEqual(new int2(40, 50), factionSpawns[0].SpawnCell);

            DynamicBuffer<CustomGameFactionUnitSourceSpawnEntry> sourceSpawns =
                em.GetBuffer<CustomGameFactionUnitSourceSpawnEntry>(entity, true);
            Assert.AreEqual(1, sourceSpawns.Length);
            Assert.AreEqual(4, sourceSpawns[0].FactionId);
            Assert.AreEqual(new FixedString64Bytes("Unit_Veh_Tank_USA"), sourceSpawns[0].SourceKey);
            Assert.AreEqual(2, sourceSpawns[0].Count);
            Assert.AreEqual(new int2(3, -1), sourceSpawns[0].SpawnOffset);

            DynamicBuffer<InitialUnitsFactionUnitSpawnEntry> unitSpawns =
                em.GetBuffer<InitialUnitsFactionUnitSpawnEntry>(entity, true);
            Assert.AreEqual(1, unitSpawns.Length);
            Assert.AreEqual(Entity.Null, unitSpawns[0].Prefab);
            Assert.AreEqual(2, unitSpawns[0].Count);

            DynamicBuffer<InitialUnitsFactionBuildingSpawnEntry> buildingSpawns =
                em.GetBuffer<InitialUnitsFactionBuildingSpawnEntry>(entity, true);
            Assert.AreEqual(1, buildingSpawns.Length);
            Assert.AreEqual(4, buildingSpawns[0].FactionId);
            Assert.AreEqual(Entity.Null, buildingSpawns[0].Prefab);
            Assert.AreEqual(new FixedString128Bytes("Building_Ammunition_Depot"), buildingSpawns[0].PrefabLookupKey);
            Assert.AreEqual(new int2(5, 7), buildingSpawns[0].OriginOffset);

            DynamicBuffer<CustomGameUnitSourceRegistryEntry> unitSources =
                em.GetBuffer<CustomGameUnitSourceRegistryEntry>(entity, true);
            Assert.AreEqual(1, unitSources.Length);
            Assert.AreEqual(new FixedString64Bytes("Unit_Veh_Tank_USA"), unitSources[0].SourceKey);
            Assert.AreEqual(new FixedString64Bytes("USA Tank"), unitSources[0].DisplayName);
            Assert.AreEqual(Entity.Null, unitSources[0].LegacyUnitPrefab);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(unitRosterConfig);
            UnityEngine.Object.DestroyImmediate(factionConfig);
            UnityEngine.Object.DestroyImmediate(startupConfig);
        }
    }

    private static void SetPrivateField<T>(object instance, string fieldName, T value)
    {
        FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field, $"Field `{fieldName}` was not found on {instance.GetType().Name}.");
        field.SetValue(instance, value);
    }
}
#endif
