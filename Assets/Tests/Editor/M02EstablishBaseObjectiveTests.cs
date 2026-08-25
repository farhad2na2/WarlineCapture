#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using Game.Components;
using Game.Composition;
using Game.Configs;
using Game.Editor;
using Game.Missions.Contracts;
using Game.Runtime;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

public sealed class M02EstablishBaseObjectiveTests
{
    private const string Marker = "[M02EstablishBaseObjectiveValidation] result=Passed tests=16";
    private const string MissionId = "saga.ch01.m02.establish_base";
    private const string ScenarioId = "scenario.ch01.m02.establish_base";
    private const string MapId = "opmap.ch01.forward_post_01";
    private const string BarracksId = "Building_Barrack";
    private const string RifleId = "Unit_Chr_Soldier_Male_02_Alt_04";

    [MenuItem("Game/Validation/Run M02 Establish Base Objective Focused")]
    public static void RunFocusedValidation()
    {
        try
        {
            M02EstablishBaseObjectiveTests tests = new();
            tests.CanonicalProjectionCreatesAttemptFactState();
            tests.AuthoritativeBarracksCompletionAdvancesFacts();
            tests.SuccessfulRequestWithoutLiveBuildingFailsClosed();
            tests.LiveBuildingWithoutSuccessfulRequestFailsClosed();
            tests.PreAttemptBarracksIsIgnored();
            tests.UnrelatedAndNonPlayerBuildingsAreIgnored();
            tests.CompletedFactRemainsMonotonicAfterBuildingRemoval();
            tests.NewAttemptCapturesASeparateRequestBaseline();
            tests.DisabledMissionRuntimeLeavesFactsUntouched();
            tests.AmbiguousBuildObjectiveFailsClosed();
            tests.AuthoritativeProducedUnitCompletionAdvancesFactOnce();
            tests.PreAttemptProducedUnitIsIgnored();
            tests.DestroyedInvalidAndUnrelatedProducedUnitsAreIgnored();
            tests.ProducedUnitFactRemainsMonotonicAfterDestruction();
            tests.NewAttemptCapturesASeparateProducedUnitBaseline();
            tests.AmbiguousProduceUnitObjectiveFailsClosed();
            Debug.Log(Marker);
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.Log("[M02EstablishBaseObjectiveValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    [MenuItem("Game/Validation/Run M02 Establish Base Objective Regressions")]
    public static void RunRegressionValidation()
    {
        try
        {
            RunValidation(RunFocusedValidation);
            RunValidation(M02EstablishBasePlacementTests.RunRegressionValidation);
            Debug.Log("[M02EstablishBaseObjectiveRegressionValidation] result=Passed suites=2");
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.Log("[M02EstablishBaseObjectiveRegressionValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    [Test]
    public void CanonicalProjectionCreatesAttemptFactState()
    {
        MissionDefinitionConfig mission = AssetDatabase.LoadAssetAtPath<MissionDefinitionConfig>(
            M02EstablishBaseConfigBuilder.MissionPath);
        ScenarioSetupConfig scenario = AssetDatabase.LoadAssetAtPath<ScenarioSetupConfig>(
            M02EstablishBaseConfigBuilder.ScenarioPath);
        OperationMapCatalogConfig maps = AssetDatabase.LoadAssetAtPath<OperationMapCatalogConfig>(
            M02EstablishBaseConfigBuilder.OperationMapCatalogPath);
        using World world = new(nameof(CanonicalProjectionCreatesAttemptFactState));
        Assert.IsTrue(CampaignMissionCatalogProjection.TryProject(
            world.EntityManager, mission, scenario, maps, 1, out Entity root, out string error), error);
        Assert.IsTrue(world.EntityManager.HasComponent<CampaignMissionAttemptFactsComponent>(root));
        Assert.IsTrue(world.EntityManager.HasComponent<CampaignMissionAttemptFactProjectionStateComponent>(root));
        DisposeCatalog(world.EntityManager, root);
    }

    [Test]
    public void AuthoritativeBarracksCompletionAdvancesFacts()
    {
        using World world = CreateRuntimeWorld(enabled: true, duplicateBuildObjective: false,
            out BlobAssetReference<CampaignMissionCatalogBlob> blob);
        try
        {
            InitializeAttempt(world);
            AddCompletedBuilding(world.EntityManager, requestId: 1, runtimeBuildingId: 1001, BarracksId,
                FactionIdentity.PlayerFactionId);
            UpdateFacts(world);
            CampaignMissionAttemptFactsComponent facts = GetFacts(world.EntityManager);
            Assert.AreEqual(1, facts.RequiredBuildingPlacedCount);
            Assert.AreEqual(1, facts.RequiredBuildingCompletedCount);
        }
        finally
        {
            blob.Dispose();
        }
    }

    [Test]
    public void SuccessfulRequestWithoutLiveBuildingFailsClosed()
    {
        using World world = CreateRuntimeWorld(true, false, out BlobAssetReference<CampaignMissionCatalogBlob> blob);
        try
        {
            InitializeAttempt(world);
            AddRequest(world.EntityManager, requestId: 1, runtimeBuildingId: 1001, BarracksId,
                FactionIdentity.PlayerFactionId, BuildingRuntimeSpawnRequest.Succeeded);
            UpdateFacts(world);
            Assert.AreEqual(0, GetFacts(world.EntityManager).RequiredBuildingCompletedCount);
        }
        finally
        {
            blob.Dispose();
        }
    }

    [Test]
    public void LiveBuildingWithoutSuccessfulRequestFailsClosed()
    {
        using World world = CreateRuntimeWorld(true, false, out BlobAssetReference<CampaignMissionCatalogBlob> blob);
        try
        {
            InitializeAttempt(world);
            CreateBuilding(world.EntityManager, runtimeBuildingId: 1001, FactionIdentity.PlayerFactionId);
            UpdateFacts(world);
            Assert.AreEqual(0, GetFacts(world.EntityManager).RequiredBuildingCompletedCount);
        }
        finally
        {
            blob.Dispose();
        }
    }

    [Test]
    public void PreAttemptBarracksIsIgnored()
    {
        using World world = CreateRuntimeWorld(true, false, out BlobAssetReference<CampaignMissionCatalogBlob> blob);
        try
        {
            AddCompletedBuilding(world.EntityManager, requestId: 7, runtimeBuildingId: 1007, BarracksId,
                FactionIdentity.PlayerFactionId);
            InitializeAttempt(world);
            UpdateFacts(world);
            Assert.AreEqual(0, GetFacts(world.EntityManager).RequiredBuildingCompletedCount);
        }
        finally
        {
            blob.Dispose();
        }
    }

    [Test]
    public void UnrelatedAndNonPlayerBuildingsAreIgnored()
    {
        using World world = CreateRuntimeWorld(true, false, out BlobAssetReference<CampaignMissionCatalogBlob> blob);
        try
        {
            InitializeAttempt(world);
            AddCompletedBuilding(world.EntityManager, 1, 1001, "Building_Refinery",
                FactionIdentity.PlayerFactionId);
            AddCompletedBuilding(world.EntityManager, 2, 1002, BarracksId,
                FactionIdentity.EnemyFactionId);
            AddRequest(world.EntityManager, 3, 1003, BarracksId, FactionIdentity.PlayerFactionId,
                BuildingRuntimeSpawnRequest.Failed);
            CreateBuilding(world.EntityManager, 1003, FactionIdentity.PlayerFactionId);
            UpdateFacts(world);
            Assert.AreEqual(0, GetFacts(world.EntityManager).RequiredBuildingCompletedCount);
        }
        finally
        {
            blob.Dispose();
        }
    }

    [Test]
    public void CompletedFactRemainsMonotonicAfterBuildingRemoval()
    {
        using World world = CreateRuntimeWorld(true, false, out BlobAssetReference<CampaignMissionCatalogBlob> blob);
        try
        {
            InitializeAttempt(world);
            Entity building = AddCompletedBuilding(world.EntityManager, 1, 1001, BarracksId,
                FactionIdentity.PlayerFactionId);
            UpdateFacts(world);
            world.EntityManager.DestroyEntity(building);
            GetRequests(world.EntityManager).Clear();
            UpdateFacts(world);
            Assert.AreEqual(1, GetFacts(world.EntityManager).RequiredBuildingCompletedCount);
        }
        finally
        {
            blob.Dispose();
        }
    }

    [Test]
    public void NewAttemptCapturesASeparateRequestBaseline()
    {
        using World world = CreateRuntimeWorld(true, false, out BlobAssetReference<CampaignMissionCatalogBlob> blob);
        try
        {
            EntityManager entityManager = world.EntityManager;
            InitializeAttempt(world);
            AddCompletedBuilding(entityManager, 1, 1001, BarracksId, FactionIdentity.PlayerFactionId);
            UpdateFacts(world);
            Assert.AreEqual(1, GetFacts(entityManager).RequiredBuildingCompletedCount);

            Entity root = GetRoot(entityManager);
            CampaignMissionRuntimeComponent runtime =
                entityManager.GetComponentData<CampaignMissionRuntimeComponent>(root);
            runtime.SessionToken = "m02-facts-attempt-2";
            runtime.AttemptOrdinal = 1;
            runtime.Version++;
            entityManager.SetComponentData(root, runtime);
            entityManager.SetComponentData(root, default(CampaignMissionAttemptFactsComponent));
            UpdateFacts(world);
            Assert.AreEqual(0, GetFacts(entityManager).RequiredBuildingCompletedCount);

            AddCompletedBuilding(entityManager, 2, 1002, BarracksId, FactionIdentity.PlayerFactionId);
            UpdateFacts(world);
            Assert.AreEqual(1, GetFacts(entityManager).RequiredBuildingCompletedCount);
        }
        finally
        {
            blob.Dispose();
        }
    }

    [Test]
    public void DisabledMissionRuntimeLeavesFactsUntouched()
    {
        using World world = CreateRuntimeWorld(false, false, out BlobAssetReference<CampaignMissionCatalogBlob> blob);
        try
        {
            Entity root = GetRoot(world.EntityManager);
            world.EntityManager.SetComponentData(root, new CampaignMissionAttemptFactsComponent
            {
                RequiredBuildingPlacedCount = 4,
                RequiredBuildingCompletedCount = 3
            });
            InitializeAttempt(world);
            Assert.AreEqual(3, GetFacts(world.EntityManager).RequiredBuildingCompletedCount);
        }
        finally
        {
            blob.Dispose();
        }
    }

    [Test]
    public void AmbiguousBuildObjectiveFailsClosed()
    {
        using World world = CreateRuntimeWorld(true, true, out BlobAssetReference<CampaignMissionCatalogBlob> blob);
        try
        {
            InitializeAttempt(world);
            AddCompletedBuilding(world.EntityManager, 1, 1001, BarracksId, FactionIdentity.PlayerFactionId);
            UpdateFacts(world);
            Assert.AreEqual(0, GetFacts(world.EntityManager).RequiredBuildingCompletedCount);
        }
        finally
        {
            blob.Dispose();
        }
    }

    [Test]
    public void AuthoritativeProducedUnitCompletionAdvancesFactOnce()
    {
        using World world = CreateRuntimeWorld(true, false, out BlobAssetReference<CampaignMissionCatalogBlob> blob);
        try
        {
            InitializeAttempt(world);
            Entity unit = CreateProducedUnit(
                world.EntityManager, RifleId, FactionIdentity.PlayerFactionId, currentHealth: 100);
            AddProducedUnitRow(world.EntityManager, unit, RifleId, FactionIdentity.PlayerFactionId);

            UpdateFacts(world);
            UpdateFacts(world);

            Assert.AreEqual(1, GetFacts(world.EntityManager).RequiredUnitProducedCount);
        }
        finally
        {
            blob.Dispose();
        }
    }

    [Test]
    public void PreAttemptProducedUnitIsIgnored()
    {
        using World world = CreateRuntimeWorld(true, false, out BlobAssetReference<CampaignMissionCatalogBlob> blob);
        try
        {
            Entity unit = CreateProducedUnit(
                world.EntityManager, RifleId, FactionIdentity.PlayerFactionId, currentHealth: 100);
            AddProducedUnitRow(world.EntityManager, unit, RifleId, FactionIdentity.PlayerFactionId);

            InitializeAttempt(world);
            UpdateFacts(world);

            Assert.AreEqual(0, GetFacts(world.EntityManager).RequiredUnitProducedCount);
        }
        finally
        {
            blob.Dispose();
        }
    }

    [Test]
    public void DestroyedInvalidAndUnrelatedProducedUnitsAreIgnored()
    {
        using World world = CreateRuntimeWorld(true, false, out BlobAssetReference<CampaignMissionCatalogBlob> blob);
        try
        {
            EntityManager entityManager = world.EntityManager;
            InitializeAttempt(world);

            Entity dead = CreateProducedUnit(
                entityManager, RifleId, FactionIdentity.PlayerFactionId, currentHealth: 0);
            AddProducedUnitRow(entityManager, dead, RifleId, FactionIdentity.PlayerFactionId);
            Entity unrelated = CreateProducedUnit(
                entityManager, "Unit_Chr_Enemy", FactionIdentity.PlayerFactionId, currentHealth: 100);
            AddProducedUnitRow(entityManager, unrelated, "Unit_Chr_Enemy", FactionIdentity.PlayerFactionId);
            Entity enemy = CreateProducedUnit(
                entityManager, RifleId, FactionIdentity.EnemyFactionId, currentHealth: 100);
            AddProducedUnitRow(entityManager, enemy, RifleId, FactionIdentity.EnemyFactionId);
            Entity mismatchedSource = CreateProducedUnit(
                entityManager, "Unit_Chr_Enemy", FactionIdentity.PlayerFactionId, currentHealth: 100);
            AddProducedUnitRow(entityManager, mismatchedSource, RifleId, FactionIdentity.PlayerFactionId);
            AddProducedUnitRow(entityManager, Entity.Null, RifleId, FactionIdentity.PlayerFactionId);

            UpdateFacts(world);

            Assert.AreEqual(0, GetFacts(entityManager).RequiredUnitProducedCount);
        }
        finally
        {
            blob.Dispose();
        }
    }

    [Test]
    public void ProducedUnitFactRemainsMonotonicAfterDestruction()
    {
        using World world = CreateRuntimeWorld(true, false, out BlobAssetReference<CampaignMissionCatalogBlob> blob);
        try
        {
            EntityManager entityManager = world.EntityManager;
            InitializeAttempt(world);
            Entity unit = CreateProducedUnit(
                entityManager, RifleId, FactionIdentity.PlayerFactionId, currentHealth: 100);
            AddProducedUnitRow(entityManager, unit, RifleId, FactionIdentity.PlayerFactionId);
            UpdateFacts(world);
            Assert.AreEqual(1, GetFacts(entityManager).RequiredUnitProducedCount);

            entityManager.DestroyEntity(unit);
            UpdateFacts(world);

            Assert.AreEqual(1, GetFacts(entityManager).RequiredUnitProducedCount);
        }
        finally
        {
            blob.Dispose();
        }
    }

    [Test]
    public void NewAttemptCapturesASeparateProducedUnitBaseline()
    {
        using World world = CreateRuntimeWorld(true, false, out BlobAssetReference<CampaignMissionCatalogBlob> blob);
        try
        {
            EntityManager entityManager = world.EntityManager;
            InitializeAttempt(world);
            Entity first = CreateProducedUnit(
                entityManager, RifleId, FactionIdentity.PlayerFactionId, currentHealth: 100);
            AddProducedUnitRow(entityManager, first, RifleId, FactionIdentity.PlayerFactionId);
            UpdateFacts(world);
            Assert.AreEqual(1, GetFacts(entityManager).RequiredUnitProducedCount);

            Entity root = GetRoot(entityManager);
            CampaignMissionRuntimeComponent runtime =
                entityManager.GetComponentData<CampaignMissionRuntimeComponent>(root);
            runtime.SessionToken = "m02-unit-facts-attempt-2";
            runtime.AttemptOrdinal = 1;
            runtime.Version++;
            entityManager.SetComponentData(root, runtime);
            entityManager.SetComponentData(root, default(CampaignMissionAttemptFactsComponent));
            UpdateFacts(world);
            Assert.AreEqual(0, GetFacts(entityManager).RequiredUnitProducedCount);

            Entity second = CreateProducedUnit(
                entityManager, RifleId, FactionIdentity.PlayerFactionId, currentHealth: 100);
            AddProducedUnitRow(entityManager, second, RifleId, FactionIdentity.PlayerFactionId);
            UpdateFacts(world);
            Assert.AreEqual(1, GetFacts(entityManager).RequiredUnitProducedCount);
        }
        finally
        {
            blob.Dispose();
        }
    }

    [Test]
    public void AmbiguousProduceUnitObjectiveFailsClosed()
    {
        using World world = CreateRuntimeWorld(
            true,
            false,
            out BlobAssetReference<CampaignMissionCatalogBlob> blob,
            duplicateProduceObjective: true);
        try
        {
            InitializeAttempt(world);
            Entity unit = CreateProducedUnit(
                world.EntityManager, RifleId, FactionIdentity.PlayerFactionId, currentHealth: 100);
            AddProducedUnitRow(world.EntityManager, unit, RifleId, FactionIdentity.PlayerFactionId);
            UpdateFacts(world);
            Assert.AreEqual(0, GetFacts(world.EntityManager).RequiredUnitProducedCount);
        }
        finally
        {
            blob.Dispose();
        }
    }

    private static World CreateRuntimeWorld(
        bool enabled,
        bool duplicateBuildObjective,
        out BlobAssetReference<CampaignMissionCatalogBlob> blob,
        bool duplicateProduceObjective = false)
    {
        World world = new($"M02 facts enabled={enabled} duplicate={duplicateBuildObjective}");
        EntityManager entityManager = world.EntityManager;
        Entity root = entityManager.CreateEntity(
            typeof(CampaignMissionRootComponent),
            typeof(CampaignMissionCatalogComponent),
            typeof(CampaignMissionRuntimeComponent),
            typeof(CampaignMissionAttemptFactsComponent),
            typeof(CampaignMissionAttemptFactProjectionStateComponent));
        using BlobBuilder builder = new(Allocator.Temp);
        ref CampaignMissionCatalogBlob catalog = ref builder.ConstructRoot<CampaignMissionCatalogBlob>();
        BlobBuilderArray<CampaignMissionDefinitionBlob> missions = builder.Allocate(ref catalog.Missions, 1);
        missions[0].MissionId = MissionId;
        missions[0].ScenarioId = ScenarioId;
        missions[0].OperationMapId = MapId;
        missions[0].MissionRuntimeEnabled = enabled ? (byte)1 : (byte)0;
        int objectiveCount = 2 + (duplicateBuildObjective ? 1 : 0) + (duplicateProduceObjective ? 1 : 0);
        BlobBuilderArray<CampaignMissionObjectiveBlob> objectives =
            builder.Allocate(ref missions[0].Objectives, objectiveCount);
        int objectiveIndex = 0;
        objectives[objectiveIndex++] = new CampaignMissionObjectiveBlob
        {
            ObjectiveId = "obj.ch01.m02.build_barracks",
            TargetConfigId = BarracksId,
            Rule = MissionObjectiveRuleKind.BuildStructure,
            RequiredCount = 1
        };
        if (duplicateBuildObjective)
        {
            objectives[objectiveIndex++] = new CampaignMissionObjectiveBlob
            {
                ObjectiveId = "obj.ch01.m02.build_duplicate",
                TargetConfigId = BarracksId,
                Rule = MissionObjectiveRuleKind.BuildStructure,
                RequiredCount = 1
            };
        }
        objectives[objectiveIndex++] = new CampaignMissionObjectiveBlob
        {
            ObjectiveId = "obj.ch01.m02.produce_rifle",
            TargetConfigId = RifleId,
            Rule = MissionObjectiveRuleKind.ProduceUnit,
            RequiredCount = 1
        };
        if (duplicateProduceObjective)
        {
            objectives[objectiveIndex] = new CampaignMissionObjectiveBlob
            {
                ObjectiveId = "obj.ch01.m02.produce_duplicate",
                TargetConfigId = RifleId,
                Rule = MissionObjectiveRuleKind.ProduceUnit,
                RequiredCount = 1
            };
        }

        blob = builder.CreateBlobAssetReference<CampaignMissionCatalogBlob>(Allocator.Persistent);
        entityManager.SetComponentData(root, new CampaignMissionCatalogComponent
        {
            Blob = blob,
            SourceVersion = 1
        });
        entityManager.SetComponentData(root, new CampaignMissionRuntimeComponent
        {
            MissionId = MissionId,
            ScenarioId = ScenarioId,
            OperationMapId = MapId,
            SessionToken = "m02-facts-attempt-1",
            Phase = MissionPhaseKind.Preparing,
            Version = 1,
            SourceVersion = 1,
            AttemptOrdinal = 0
        });
        Entity boundary = entityManager.CreateEntity(typeof(BuildingRuntimeStateTag));
        entityManager.AddBuffer<BuildingRuntimeSpawnRequest>(boundary);
        entityManager.AddBuffer<BuildingProducedUnitReadModel>(boundary);
        return world;
    }

    private static void InitializeAttempt(World world) => UpdateFacts(world);

    private static Entity AddCompletedBuilding(
        EntityManager entityManager,
        int requestId,
        int runtimeBuildingId,
        string buildingId,
        byte factionId)
    {
        AddRequest(entityManager, requestId, runtimeBuildingId, buildingId, factionId,
            BuildingRuntimeSpawnRequest.Succeeded);
        return CreateBuilding(entityManager, runtimeBuildingId, factionId);
    }

    private static void AddRequest(
        EntityManager entityManager,
        int requestId,
        int runtimeBuildingId,
        string buildingId,
        byte factionId,
        byte status)
    {
        GetRequests(entityManager).Add(new BuildingRuntimeSpawnRequest
        {
            RequestId = requestId,
            RequestKind = BuildingRuntimeSpawnRequest.KindBuilding,
            FactionId = factionId,
            HasOwnerFaction = 1,
            BuildingId = buildingId,
            Status = status,
            BuildingRuntimeId = runtimeBuildingId,
            ActualOrigin = new int2(10 + runtimeBuildingId, 20),
            ActualFootprint = new int2(20, 10)
        });
    }

    private static Entity CreateBuilding(EntityManager entityManager, int runtimeBuildingId, byte factionId)
    {
        Entity building = entityManager.CreateEntity(
            typeof(RuntimeBuildingCombatTag),
            typeof(RuntimeBuildingCombatInfo),
            typeof(Faction),
            typeof(UnitHealth));
        entityManager.SetComponentData(building, new RuntimeBuildingCombatInfo
        {
            RuntimeBuildingId = runtimeBuildingId,
            OwnerFactionId = factionId,
            OriginCell = new int2(10 + runtimeBuildingId, 20),
            FootprintCells = new int2(20, 10)
        });
        entityManager.SetComponentData(building, new Faction { Id = factionId });
        entityManager.SetComponentData(building, new UnitHealth { Current = 1000, Max = 1000 });
        return building;
    }

    private static DynamicBuffer<BuildingRuntimeSpawnRequest> GetRequests(EntityManager entityManager)
    {
        using EntityQuery query = entityManager.CreateEntityQuery(typeof(BuildingRuntimeStateTag));
        return entityManager.GetBuffer<BuildingRuntimeSpawnRequest>(query.GetSingletonEntity());
    }

    private static Entity CreateProducedUnit(
        EntityManager entityManager,
        string sourceKey,
        byte factionId,
        int currentHealth)
    {
        Entity unit = entityManager.CreateEntity(
            typeof(Faction),
            typeof(UnitHealth),
            typeof(UnitSourcePrefabKey));
        entityManager.SetComponentData(unit, new Faction { Id = factionId });
        entityManager.SetComponentData(unit, new UnitHealth { Current = currentHealth, Max = 100 });
        entityManager.SetComponentData(unit, new UnitSourcePrefabKey
        {
            Value = new FixedString64Bytes(sourceKey)
        });
        return unit;
    }

    private static void AddProducedUnitRow(
        EntityManager entityManager,
        Entity unit,
        string sourceKey,
        byte factionId)
    {
        GetProducedUnits(entityManager).Add(new BuildingProducedUnitReadModel
        {
            BuildingRuntimeId = 1001,
            ProductionIndex = 0,
            OwnerFactionId = factionId,
            HasOwnerFaction = 1,
            Unit = unit,
            UnitSourceKey = new FixedString64Bytes(sourceKey)
        });
    }

    private static DynamicBuffer<BuildingProducedUnitReadModel> GetProducedUnits(EntityManager entityManager)
    {
        using EntityQuery query = entityManager.CreateEntityQuery(typeof(BuildingRuntimeStateTag));
        return entityManager.GetBuffer<BuildingProducedUnitReadModel>(query.GetSingletonEntity());
    }

    private static CampaignMissionAttemptFactsComponent GetFacts(EntityManager entityManager) =>
        entityManager.GetComponentData<CampaignMissionAttemptFactsComponent>(GetRoot(entityManager));

    private static Entity GetRoot(EntityManager entityManager)
    {
        using EntityQuery query = entityManager.CreateEntityQuery(typeof(CampaignMissionRootComponent));
        return query.GetSingletonEntity();
    }

    private static void UpdateFacts(World world)
    {
        SystemHandle system = world.CreateSystem<CampaignMissionAttemptFactProjectionSystem>();
        ref SystemState state = ref world.Unmanaged.ResolveSystemStateRef(system);
        world.Unmanaged.GetUnsafeSystemRef<CampaignMissionAttemptFactProjectionSystem>(system).OnUpdate(ref state);
        state.Dependency.Complete();
        world.EntityManager.CompleteAllTrackedJobs();
        world.DestroySystem(system);
    }

    private static void RunValidation(Action validation)
    {
        ValidationExit.ClearLastExitCode();
        using (ValidationExit.SuppressProcessExit())
            validation();
        if (ValidationExit.LastExitCode is int exitCode && exitCode != 0)
            throw new InvalidOperationException(
                $"{validation.Method.DeclaringType?.Name}.{validation.Method.Name} failed validation.");
    }

    private static void DisposeCatalog(EntityManager entityManager, Entity root)
    {
        CampaignMissionCatalogComponent catalog =
            entityManager.GetComponentData<CampaignMissionCatalogComponent>(root);
        CampaignMissionCatalogDisposalSystem.DisposeOwned(ref catalog);
        entityManager.SetComponentData(root, catalog);
    }
}
#endif
