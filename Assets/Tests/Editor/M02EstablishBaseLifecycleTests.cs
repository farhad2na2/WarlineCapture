#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Game.Components;
using Game.Composition;
using Game.Missions.Contracts;
using Game.Runtime;
using Game.UI.Shell.Ecs;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

public sealed class M02EstablishBaseLifecycleTests
{
    private const string MissionId = "saga.ch01.m02.establish_base";
    private const string Session = "campaign-m02-lifecycle";
    private const string ScenarioId = "scenario.ch01.m02.establish_base";
    private const string OperationMapId = "opmap.ch01.forward_post_01";
    private const string BarracksId = "Building_Barrack";
    private const string RifleId = "Unit_Chr_Soldier_Male_02_Alt_04";
    private const string FocusedMarker =
        "[M02EstablishBaseLifecycleValidation] result=Passed tests=15";

    [MenuItem("Game/Validation/Run M02 Establish Base Lifecycle Focused")]
    public static void RunFocusedValidation()
    {
        try
        {
            M02EstablishBaseLifecycleTests tests = new();
            tests.AttemptCleanupPreservesMapBuildingAndClearsTransientOwnership();
            tests.AttemptCleanupQueuesBarracksDeletionAndDestroysProducedRifle();
            tests.BuildingBoundaryBootstrapCreatesDeleteBufferBeforeFirstUpdate();
            tests.BuildingOwnerConsumesDeleteRequestsExactlyOnce();
            tests.M02ResultResolvesFromTwoMissionChapterCatalog();
            tests.ExitPersistsResumeAndClearsAttemptOwnership();
            tests.NarrativePauseStopsAutomaticMissionPhaseProgression();

            M02EstablishBaseLaunchTests launch = new();
            launch.M02RetryPreservesSeedAndIncrementsAttemptIdentity();
            launch.M02ReplayUsesCanonicalIdentityAndSeed();
            launch.M01LaunchFromChapterCatalogRemainsUnchanged();
            launch.SwitchingFromM01ToM02AdvancesTheSoleMapGeneration();
            launch.MenuBootstrapRebindsAfterWorldRecreation();

            new M02EstablishBaseObjectiveTests().NewAttemptRebindsForwardPostSession();
            new M02EstablishBaseResultSettlementTests()
                .FirstClearGrantsRewardsBarracksAndM03ExactlyOnce();
            new WorldLifecycleRecoveryMatrixTests()
                .WorldReplacement_RebindsGovernedQueryAndGatewayOwners();

            Debug.Log(FocusedMarker);
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[M02EstablishBaseLifecycleValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    [MenuItem("Game/Validation/Run M02 Establish Base Lifecycle Regressions")]
    public static void RunRegressionValidation()
    {
        try
        {
            RunValidation(RunFocusedValidation);
            RunValidation(M02EstablishBaseLaunchTests.RunFocusedValidation);
            RunValidation(M02EstablishBaseResultSettlementTests.RunFocusedValidation);
            RunValidation(M01FirstContactContractValidation.RunFocusedValidation);
            RunValidation(ProductionSourceGrowthArchitectureTests.RunFocusedValidation);
            Debug.Log("[M02EstablishBaseLifecycleRegressionValidation] result=Passed suites=5");
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[M02EstablishBaseLifecycleRegressionValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    [Test]
    public void AttemptCleanupPreservesMapBuildingAndClearsTransientOwnership()
    {
        using World world = new(nameof(AttemptCleanupPreservesMapBuildingAndClearsTransientOwnership));
        EntityManager entityManager = world.EntityManager;
        Entity root = CreateAttemptRoot(entityManager);
        SeedTransientRootState(entityManager, root);

        Entity forwardPost = entityManager.CreateEntity(
            typeof(OperationMapBuildingComponent), typeof(CampaignMissionUnitRoleComponent));
        entityManager.SetComponentData(forwardPost, new CampaignMissionUnitRoleComponent
        {
            MissionRoleId = new FixedString64Bytes("role.friendly.forward_post"),
            SessionToken = new FixedString64Bytes(Session)
        });
        Entity spawnedUnit = entityManager.CreateEntity(typeof(CampaignMissionUnitRoleComponent));
        Entity ambientCivilian = entityManager.CreateEntity(typeof(CampaignMissionAmbientCivilianComponent));

        EntityCommandBuffer cleanup = new(Allocator.Temp);
        try
        {
            CampaignMissionLaunchSystem.QueueAttemptCleanup(entityManager, ref cleanup, root);
            cleanup.Playback(entityManager);
        }
        finally
        {
            cleanup.Dispose();
        }

        Assert.IsTrue(entityManager.Exists(forwardPost));
        Assert.IsFalse(entityManager.HasComponent<CampaignMissionUnitRoleComponent>(forwardPost));
        Assert.IsFalse(entityManager.Exists(spawnedUnit));
        Assert.IsFalse(entityManager.Exists(ambientCivilian));
        AssertTransientRootStateCleared(entityManager, root);
    }

    [Test]
    public void NarrativePauseStopsAutomaticMissionPhaseProgression()
    {
        using BlobAssetReference<CampaignMissionCatalogBlob> catalogBlob = CreateLifecycleCatalog();
        using World world = new(nameof(NarrativePauseStopsAutomaticMissionPhaseProgression));
        EntityManager entityManager = world.EntityManager;
        Entity root = entityManager.CreateEntity(
            typeof(CampaignMissionRootComponent),
            typeof(CampaignMissionRuntimeComponent),
            typeof(CampaignMissionAttemptFactsComponent));
        entityManager.AddComponentData(root, new CampaignMissionCatalogComponent
        {
            Blob = catalogBlob,
            SourceVersion = 1
        });
        entityManager.AddBuffer<CampaignMissionActionRequestElement>(root);
        entityManager.AddBuffer<CampaignMissionActionResultElement>(root);
        entityManager.SetComponentData(root, new CampaignMissionRuntimeComponent
        {
            MissionId = new FixedString64Bytes(MissionId),
            ScenarioId = new FixedString64Bytes(ScenarioId),
            OperationMapId = new FixedString64Bytes(OperationMapId),
            SessionToken = new FixedString64Bytes(Session),
            Phase = MissionPhaseKind.InteractiveBrief,
            LaunchOrigin = MissionLaunchOriginKind.CampaignOperations,
            RunKind = MissionRunKind.FirstClear,
            TransitionToken = 21,
            Version = 1,
            SourceVersion = 1,
            AttemptOrdinal = 1,
            DeterministicSeed = 2202
        });
        Entity gameplay = entityManager.CreateEntity(typeof(RuntimeGameplayStateComponent));
        entityManager.SetComponentData(gameplay, new RuntimeGameplayStateComponent
        {
            PlayRequested = 1,
            SimulationActive = 0
        });
        SystemHandle handle = world.GetOrCreateSystem<CampaignMissionRuntimeSystem>();

        world.Unmanaged.GetUnsafeSystemRef<CampaignMissionRuntimeSystem>(handle)
            .OnUpdate(ref world.Unmanaged.ResolveSystemStateRef(handle));
        Assert.AreEqual(MissionPhaseKind.InteractiveBrief,
            entityManager.GetComponentData<CampaignMissionRuntimeComponent>(root).Phase);

        RuntimeGameplayStateComponent state =
            entityManager.GetComponentData<RuntimeGameplayStateComponent>(gameplay);
        state.SimulationActive = 1;
        entityManager.SetComponentData(gameplay, state);
        world.Unmanaged.GetUnsafeSystemRef<CampaignMissionRuntimeSystem>(handle)
            .OnUpdate(ref world.Unmanaged.ResolveSystemStateRef(handle));
        Assert.AreEqual(MissionPhaseKind.InteractiveBrief,
            entityManager.GetComponentData<CampaignMissionRuntimeComponent>(root).Phase,
            "M02 advanced before the narrative presenter acknowledged the brief.");

        CampaignMissionAttemptFactsComponent facts =
            entityManager.GetComponentData<CampaignMissionAttemptFactsComponent>(root);
        facts.InteractiveBriefCompleted = 1;
        entityManager.SetComponentData(root, facts);
        world.Unmanaged.GetUnsafeSystemRef<CampaignMissionRuntimeSystem>(handle)
            .OnUpdate(ref world.Unmanaged.ResolveSystemStateRef(handle));
        Assert.AreEqual(MissionPhaseKind.FindSquad,
            entityManager.GetComponentData<CampaignMissionRuntimeComponent>(root).Phase);
    }

    [Test]
    public void ExitPersistsResumeAndClearsAttemptOwnership()
    {
        string saveRoot = Path.Combine(
            Path.GetTempPath(), "WarlineCapture", nameof(M02EstablishBaseLifecycleTests),
            Guid.NewGuid().ToString("N"));
        try
        {
            using World world = new(nameof(ExitPersistsResumeAndClearsAttemptOwnership));
            EntityManager entityManager = world.EntityManager;
            Entity root = CreateAttemptRoot(entityManager);
            SeedTransientRootState(entityManager, root);
            CampaignMissionRuntimeComponent runtime = new()
            {
                MissionId = new FixedString64Bytes(MissionId),
                SessionToken = new FixedString64Bytes(Session),
                Phase = MissionPhaseKind.Preparing,
                TransitionToken = 31,
                Version = 4,
                AttemptOrdinal = 2
            };
            entityManager.SetComponentData(root, runtime);
            CampaignMissionProgressStore store = new(
                new SaveService(new JsonSaveRepository(saveRoot)));
            entityManager.AddComponentObject(root, new CampaignMissionProgressStoreReferenceComponent
            {
                Store = store
            });
            entityManager.GetBuffer<CampaignMissionActionRequestElement>(root).Clear();
            entityManager.GetBuffer<CampaignMissionActionResultElement>(root).Clear();
            entityManager.GetBuffer<CampaignMissionActionRequestElement>(root).Add(new()
            {
                Action = MissionActionKind.Exit,
                TransitionToken = runtime.TransitionToken,
                SessionToken = runtime.SessionToken,
                AttemptOrdinal = runtime.AttemptOrdinal
            });
            Entity spawnedUnit = entityManager.CreateEntity(typeof(CampaignMissionUnitRoleComponent));
            Entity ambientCivilian = entityManager.CreateEntity(typeof(CampaignMissionAmbientCivilianComponent));

            bool consumed = false;
            CampaignMissionRuntimeSystem.TryConsumeActionManaged(entityManager, root, ref consumed);

            Assert.IsTrue(consumed);
            DynamicBuffer<CampaignMissionActionResultElement> results =
                entityManager.GetBuffer<CampaignMissionActionResultElement>(root);
            Assert.AreEqual(1, results.Length);
            Assert.AreEqual(1, results[0].Accepted, results[0].ReasonCode.ToString());
            Assert.AreEqual(0, entityManager.GetComponentData<CampaignMissionRuntimeComponent>(root).Version);
            Assert.AreEqual(0, entityManager
                .GetComponentData<CampaignMissionAttemptFactsComponent>(root).ElapsedMilliseconds);
            Assert.IsFalse(entityManager.Exists(spawnedUnit));
            Assert.IsFalse(entityManager.Exists(ambientCivilian));
            Assert.IsTrue(Array.Exists(
                store.ReadAll(),
                entry => entry.missionId == MissionId && entry.pendingResume && entry.lastAttemptOrdinal == 2));
            AssertTransientRootStateCleared(entityManager, root, expectActionResult: true);
        }
        finally
        {
            if (Directory.Exists(saveRoot))
                Directory.Delete(saveRoot, true);
        }
    }

    [Test]
    public void AttemptCleanupQueuesBarracksDeletionAndDestroysProducedRifle()
    {
        using BlobAssetReference<CampaignMissionCatalogBlob> catalogBlob = CreateLifecycleCatalog();
        using World world = new(nameof(AttemptCleanupQueuesBarracksDeletionAndDestroysProducedRifle));
        EntityManager entityManager = world.EntityManager;
        Entity root = CreateAttemptRoot(entityManager);
        FixedString64Bytes session = new(Session);
        entityManager.AddComponentData(root, new CampaignMissionCatalogComponent
        {
            Blob = catalogBlob,
            SourceVersion = 7
        });
        entityManager.SetComponentData(root, new CampaignMissionRuntimeComponent
        {
            MissionId = new FixedString64Bytes(MissionId),
            ScenarioId = new FixedString64Bytes(ScenarioId),
            OperationMapId = new FixedString64Bytes(OperationMapId),
            SessionToken = session,
            Version = 2,
            SourceVersion = 7,
            AttemptOrdinal = 3
        });
        entityManager.SetComponentData(root, new CampaignMissionAttemptFactProjectionStateComponent
        {
            SessionToken = session,
            AttemptOrdinal = 3,
            BuildingRequestBaselineId = 10,
            ProducedUnitReadModelBaselineCount = 1,
            SourceVersion = 7,
            Initialized = 1
        });

        Entity boundary = entityManager.CreateEntity(typeof(BuildingRuntimeStateTag));
        DynamicBuffer<BuildingRuntimeSpawnRequest> spawnRequests =
            entityManager.AddBuffer<BuildingRuntimeSpawnRequest>(boundary);
        spawnRequests.Add(CreateBuildingRequest(10, BarracksId, 41));
        spawnRequests.Add(CreateBuildingRequest(11, BarracksId, 42));
        spawnRequests.Add(CreateBuildingRequest(12, "Building_Tent", 43));
        DynamicBuffer<BuildingRuntimeDeleteRequest> deleteRequests =
            entityManager.AddBuffer<BuildingRuntimeDeleteRequest>(boundary);
        deleteRequests.Add(new BuildingRuntimeDeleteRequest { BuildingRuntimeId = 99 });
        DynamicBuffer<BuildingProducedUnitReadModel> producedUnits =
            entityManager.AddBuffer<BuildingProducedUnitReadModel>(boundary);
        Entity priorRifle = entityManager.CreateEntity();
        Entity attemptRifle = entityManager.CreateEntity();
        Entity unrelatedUnit = entityManager.CreateEntity();
        producedUnits.Add(CreateProducedUnit(priorRifle, RifleId));
        producedUnits.Add(CreateProducedUnit(attemptRifle, RifleId));
        producedUnits.Add(CreateProducedUnit(unrelatedUnit, "Unit_Unrelated"));

        Entity threatWarning = entityManager.CreateEntity(typeof(ThreatWarningRuntimeStateComponent));
        entityManager.SetComponentData(threatWarning, new ThreatWarningRuntimeStateComponent
        {
            HasPendingWarning = 1,
            PendingType = ThreatWarningType.Air,
            PendingEtaSeconds = 12f,
            PendingThreatCount = 3,
            Version = 4
        });
        Entity cameraFocus = entityManager.CreateEntity(typeof(RuntimeCameraFocusRequestComponent));
        entityManager.SetComponentData(cameraFocus, new RuntimeCameraFocusRequestComponent
        {
            Requested = 1,
            Smooth = 1,
            SmoothTimeSeconds = 2f
        });

        EntityCommandBuffer cleanup = new(Allocator.Temp);
        try
        {
            CampaignMissionLaunchSystem.QueueAttemptCleanup(entityManager, ref cleanup, root);
            cleanup.Playback(entityManager);
        }
        finally
        {
            cleanup.Dispose();
        }

        Assert.IsTrue(entityManager.Exists(priorRifle));
        Assert.IsFalse(entityManager.Exists(attemptRifle));
        Assert.IsTrue(entityManager.Exists(unrelatedUnit));
        deleteRequests = entityManager.GetBuffer<BuildingRuntimeDeleteRequest>(boundary);
        Assert.AreEqual(2, deleteRequests.Length);
        Assert.AreEqual(99, deleteRequests[0].BuildingRuntimeId);
        Assert.AreEqual(42, deleteRequests[1].BuildingRuntimeId);
        Assert.AreEqual(0, entityManager
            .GetComponentData<ThreatWarningRuntimeStateComponent>(threatWarning).HasPendingWarning);
        Assert.AreEqual(0, entityManager
            .GetComponentData<RuntimeCameraFocusRequestComponent>(cameraFocus).Requested);
    }

    [Test]
    public void BuildingBoundaryBootstrapCreatesDeleteBufferBeforeFirstUpdate()
    {
        using World world = new(nameof(BuildingBoundaryBootstrapCreatesDeleteBufferBeforeFirstUpdate));
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        try
        {
            World.DefaultGameObjectInjectionWorld = world;
            Entity boundary = MatchBuildingRuntimeBootstrapStartupSystemHelper.Ensure(Entity.Null);

            Assert.IsTrue(world.EntityManager.Exists(boundary));
            Assert.IsTrue(world.EntityManager.HasBuffer<BuildingRuntimeDeleteRequest>(boundary));
        }
        finally
        {
            World.DefaultGameObjectInjectionWorld = previousWorld;
        }
    }

    [Test]
    public void BuildingOwnerConsumesDeleteRequestsExactlyOnce()
    {
        using World world = new(nameof(BuildingOwnerConsumesDeleteRequestsExactlyOnce));
        EntityManager entityManager = world.EntityManager;
        Entity boundary = entityManager.CreateEntity(typeof(BuildingRuntimeStateTag));
        DynamicBuffer<BuildingRuntimeDeleteRequest> requests =
            entityManager.AddBuffer<BuildingRuntimeDeleteRequest>(boundary);
        requests.Add(new BuildingRuntimeDeleteRequest { BuildingRuntimeId = 42 });
        requests.Add(new BuildingRuntimeDeleteRequest { BuildingRuntimeId = 42 });
        requests.Add(new BuildingRuntimeDeleteRequest { BuildingRuntimeId = 43 });
        List<int> deleted = new();

        BuildingRuntimeDeleteCommandProcessor owner = new();
        owner.Process(
            buildingId =>
            {
                deleted.Add(buildingId);
                return true;
            },
            entityManager,
            boundary);

        CollectionAssert.AreEqual(new[] { 42, 43 }, deleted);
        Assert.AreEqual(0, entityManager.GetBuffer<BuildingRuntimeDeleteRequest>(boundary).Length);
    }

    [Test]
    public void M02ResultResolvesFromTwoMissionChapterCatalog()
    {
        using BlobAssetReference<CampaignMissionCatalogBlob> catalogBlob = CreateLifecycleCatalog();
        using World world = new(nameof(M02ResultResolvesFromTwoMissionChapterCatalog));
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        try
        {
            World.DefaultGameObjectInjectionWorld = world;
            UiShellEcsGateway.RegisterAsRuntimeGateway();
            Entity root = world.EntityManager.CreateEntity(
                typeof(CampaignMissionRootComponent),
                typeof(CampaignMissionRuntimeComponent),
                typeof(CampaignMissionResultComponent),
                typeof(CampaignMissionAttemptFactsComponent));
            FixedString64Bytes session = new(Session);
            world.EntityManager.AddComponentData(root, new CampaignMissionCatalogComponent
            {
                Blob = catalogBlob,
                SourceVersion = 7
            });
            world.EntityManager.SetComponentData(root, new CampaignMissionRuntimeComponent
            {
                MissionId = new FixedString64Bytes(MissionId),
                ScenarioId = new FixedString64Bytes(ScenarioId),
                OperationMapId = new FixedString64Bytes(OperationMapId),
                SessionToken = session,
                Phase = MissionPhaseKind.Result,
                Outcome = MissionOutcomeKind.Victory,
                ReturnDestination = MissionReturnDestinationKind.CampaignOperations,
                Version = 3,
                SourceVersion = 7,
                AttemptOrdinal = 1
            });
            world.EntityManager.SetComponentData(root, new CampaignMissionResultComponent
            {
                MissionId = new FixedString64Bytes(MissionId),
                SessionToken = session,
                AttemptOrdinal = 1,
                SourceVersion = 7,
                Outcome = MissionOutcomeKind.Victory,
                ReturnDestination = MissionReturnDestinationKind.CampaignOperations,
                Stars = 2,
                ElapsedMilliseconds = 120000
            });

            Assert.IsTrue(UiShellEcsGateway.TryReadMissionResult(out var result));
            Assert.AreEqual(MissionId, result.MissionId);
            Assert.AreEqual("22 CREDITS", result.RewardsText);
        }
        finally
        {
            World.DefaultGameObjectInjectionWorld = previousWorld;
            UiShellEcsGateway.RegisterAsRuntimeGateway();
        }
    }

    private static Entity CreateAttemptRoot(EntityManager entityManager)
    {
        Entity root = entityManager.CreateEntity(
            typeof(CampaignMissionRootComponent),
            typeof(CampaignMissionRuntimeComponent),
            typeof(CampaignMissionAttemptFactsComponent),
            typeof(CampaignMissionAttemptResourceInitializationComponent),
            typeof(CampaignMissionAttemptFactProjectionStateComponent),
            typeof(CampaignMissionDelayedWaveStateComponent),
            typeof(CampaignMissionOpeningPresentationComponent),
            typeof(CampaignMissionFinalePresentationComponent),
            typeof(CampaignMissionGuidanceProjectionComponent),
            typeof(CampaignMissionResultComponent));
        entityManager.AddBuffer<CampaignMissionActionRequestElement>(root);
        entityManager.AddBuffer<CampaignMissionActionResultElement>(root);
        entityManager.AddBuffer<CampaignMissionLaunchResultElement>(root);
        entityManager.AddBuffer<CampaignMissionSettlementRequestElement>(root);
        entityManager.AddBuffer<CampaignMissionSettlementResultElement>(root);
        entityManager.AddBuffer<CampaignMissionGuidanceAcknowledgementRequestElement>(root);
        return root;
    }

    private static void SeedTransientRootState(EntityManager entityManager, Entity root)
    {
        FixedString64Bytes session = new(Session);
        entityManager.SetComponentData(root, new CampaignMissionAttemptFactsComponent
        {
            ElapsedMilliseconds = 1234,
            RequiredBuildingPlacedCount = 1
        });
        entityManager.SetComponentData(root, new CampaignMissionAttemptResourceInitializationComponent
        {
            SessionToken = session,
            AttemptOrdinal = 2,
            Applied = 1
        });
        entityManager.SetComponentData(root, new CampaignMissionAttemptFactProjectionStateComponent
        {
            SessionToken = session,
            AttemptOrdinal = 2,
            Initialized = 1
        });
        entityManager.SetComponentData(root, new CampaignMissionDelayedWaveStateComponent
        {
            SessionToken = session,
            AttemptOrdinal = 2,
            Activated = 1,
            Initialized = 1
        });
        entityManager.SetComponentData(root, new CampaignMissionOpeningPresentationComponent
        {
            SessionToken = session,
            Stage = 2
        });
        entityManager.SetComponentData(root, new CampaignMissionFinalePresentationComponent
        {
            SessionToken = session,
            Required = 1,
            Stage = 3
        });
        entityManager.SetComponentData(root, new CampaignMissionGuidanceProjectionComponent
        {
            Active = 1,
            Version = 3
        });
        entityManager.SetComponentData(root, new CampaignMissionResultComponent
        {
            SessionToken = session,
            Outcome = MissionOutcomeKind.Defeat
        });
        entityManager.GetBuffer<CampaignMissionActionRequestElement>(root).Add(default);
        entityManager.GetBuffer<CampaignMissionActionResultElement>(root).Add(default);
        entityManager.GetBuffer<CampaignMissionLaunchResultElement>(root).Add(default);
        entityManager.GetBuffer<CampaignMissionSettlementRequestElement>(root).Add(default);
        entityManager.GetBuffer<CampaignMissionSettlementResultElement>(root).Add(default);
        entityManager.GetBuffer<CampaignMissionGuidanceAcknowledgementRequestElement>(root).Add(default);
    }

    private static void AssertTransientRootStateCleared(
        EntityManager entityManager, Entity root, bool expectActionResult = false)
    {
        Assert.AreEqual(0, entityManager.GetBuffer<CampaignMissionActionRequestElement>(root).Length);
        Assert.AreEqual(expectActionResult ? 1 : 0,
            entityManager.GetBuffer<CampaignMissionActionResultElement>(root).Length);
        Assert.AreEqual(0, entityManager.GetBuffer<CampaignMissionLaunchResultElement>(root).Length);
        Assert.AreEqual(0, entityManager.GetBuffer<CampaignMissionSettlementRequestElement>(root).Length);
        Assert.AreEqual(0, entityManager.GetBuffer<CampaignMissionSettlementResultElement>(root).Length);
        Assert.AreEqual(0,
            entityManager.GetBuffer<CampaignMissionGuidanceAcknowledgementRequestElement>(root).Length);
        Assert.AreEqual(0, entityManager
            .GetComponentData<CampaignMissionAttemptResourceInitializationComponent>(root).Applied);
        Assert.AreEqual(0, entityManager
            .GetComponentData<CampaignMissionAttemptFactProjectionStateComponent>(root).Initialized);
        Assert.AreEqual(0, entityManager
            .GetComponentData<CampaignMissionDelayedWaveStateComponent>(root).Initialized);
        Assert.AreEqual(0, entityManager
            .GetComponentData<CampaignMissionOpeningPresentationComponent>(root).Stage);
        Assert.AreEqual(0, entityManager
            .GetComponentData<CampaignMissionFinalePresentationComponent>(root).Required);
        Assert.AreEqual(0, entityManager
            .GetComponentData<CampaignMissionGuidanceProjectionComponent>(root).Active);
        Assert.AreEqual(MissionOutcomeKind.None,
            entityManager.GetComponentData<CampaignMissionResultComponent>(root).Outcome);
    }

    private static BuildingRuntimeSpawnRequest CreateBuildingRequest(
        int requestId,
        string buildingId,
        int buildingRuntimeId) =>
        new()
        {
            RequestId = requestId,
            RequestKind = BuildingRuntimeSpawnRequest.KindBuilding,
            FactionId = FactionIdentity.PlayerFactionId,
            HasOwnerFaction = 1,
            BuildingId = new FixedString128Bytes(buildingId),
            Status = BuildingRuntimeSpawnRequest.Succeeded,
            BuildingRuntimeId = buildingRuntimeId
        };

    private static BuildingProducedUnitReadModel CreateProducedUnit(Entity unit, string sourceKey) =>
        new()
        {
            OwnerFactionId = FactionIdentity.PlayerFactionId,
            HasOwnerFaction = 1,
            Unit = unit,
            UnitSourceKey = new FixedString64Bytes(sourceKey)
        };

    private static BlobAssetReference<CampaignMissionCatalogBlob> CreateLifecycleCatalog()
    {
        BlobBuilder builder = new(Allocator.Temp);
        ref CampaignMissionCatalogBlob root = ref builder.ConstructRoot<CampaignMissionCatalogBlob>();
        BlobBuilderArray<CampaignMissionDefinitionBlob> missions = builder.Allocate(ref root.Missions, 2);
        ref CampaignMissionDefinitionBlob firstContact = ref missions[0];
        firstContact.MissionId = new FixedString64Bytes("saga.ch01.m01.first_contact");
        firstContact.ScenarioId = new FixedString64Bytes("scenario.ch01.m01.first_contact");
        firstContact.OperationMapId = new FixedString64Bytes("opmap.ch01.old_market_01");
        BlobBuilderArray<CampaignMissionRewardBlob> firstContactReplayRewards =
            builder.Allocate(ref firstContact.ReplayRewards, 1);
        firstContactReplayRewards[0] = new CampaignMissionRewardBlob
        {
            Kind = MissionRewardKind.Credits,
            Amount = 11
        };

        ref CampaignMissionDefinitionBlob definition = ref missions[1];
        definition.MissionId = new FixedString64Bytes(MissionId);
        definition.ScenarioId = new FixedString64Bytes(ScenarioId);
        definition.OperationMapId = new FixedString64Bytes(OperationMapId);
        definition.MissionRuntimeEnabled = 1;
        BlobBuilderArray<CampaignMissionObjectiveBlob> objectives = builder.Allocate(ref definition.Objectives, 3);
        objectives[0] = new CampaignMissionObjectiveBlob
        {
            ObjectiveId = new FixedString64Bytes("objective.build.barracks"),
            Rule = MissionObjectiveRuleKind.BuildStructure,
            TargetConfigId = new FixedString64Bytes(BarracksId),
            RequiredCount = 1
        };
        objectives[1] = new CampaignMissionObjectiveBlob
        {
            ObjectiveId = new FixedString64Bytes("objective.produce.rifle"),
            Rule = MissionObjectiveRuleKind.ProduceUnit,
            TargetConfigId = new FixedString64Bytes(RifleId),
            RequiredCount = 1
        };
        objectives[2] = new CampaignMissionObjectiveBlob
        {
            ObjectiveId = new FixedString64Bytes("objective.defend.forward_post"),
            Rule = MissionObjectiveRuleKind.DefendMissionRole,
            MissionRoleId = new FixedString64Bytes("role.friendly.forward_post"),
            RequiredCount = 1,
            FailureOnRuleBreak = 1
        };
        BlobBuilderArray<CampaignMissionRewardBlob> replayRewards =
            builder.Allocate(ref definition.ReplayRewards, 1);
        replayRewards[0] = new CampaignMissionRewardBlob
        {
            Kind = MissionRewardKind.Credits,
            Amount = 22
        };
        BlobAssetReference<CampaignMissionCatalogBlob> result =
            builder.CreateBlobAssetReference<CampaignMissionCatalogBlob>(Allocator.Persistent);
        builder.Dispose();
        return result;
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
}
#endif
