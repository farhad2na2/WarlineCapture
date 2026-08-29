#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using System.IO;
using Game.Components;
using Game.Missions.Contracts;
using Game.Runtime;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

public sealed class M02EstablishBaseResultSettlementTests
{
    private const string M02 = "saga.ch01.m02.establish_base";
    private const string M03 = "saga.ch01.m03.radar_warning";
    private const string Barracks = "Building_Barrack";
    private const string TrainingFacilities = "upgrade.building.training_facilities";
    private const string FocusedMarker =
        "[M02EstablishBaseResultSettlementValidation] result=Passed tests=15";

    [MenuItem("Game/Validation/Run M02 Establish Base Result Settlement Focused")]
    public static void RunFocusedValidation()
    {
        try
        {
            M02EstablishBaseResultSettlementTests tests = new();
            tests.RuntimeCompletesAfterBuildingAndRifleProductionResolve();
            tests.IncompleteRequirementsCannotResolveVictory();
            tests.M02VictoryDoesNotRequireHostilesOrDefenseWave();
            tests.CivilianAndFiveMinuteStarsAreIndependent();
            tests.InvalidOrContradictoryResultFactsFailClosed();
            tests.FirstClearGrantsRewardsBarracksAndM03ExactlyOnce();
            tests.ExistingBarracksConvertsUnlockToBlueprintPartsExactlyOnce();
            tests.ReplayGrantsOnlyReplayCredits();
            tests.CampaignRetryBeforeFirstClearGrantsFirstClearRewards();
            tests.ReplayRetryAfterFirstClearGrantsOnlyReplayRewards();
            tests.FirstClearUsesDebriefWhileReplayReturnsDirectly();
            tests.FirstClearResultQueuesDebriefBeforeVictory();
            tests.DebriefCompletionShowsFinalVictoryBeforeReturn();
            tests.RestartPreservesUnlockRewardsAndSettlementHistory();
            tests.UnknownOrMisScopedCustomRewardsFailClosed();
            Debug.Log(FocusedMarker);
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[M02EstablishBaseResultSettlementValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    [MenuItem("Game/Validation/Run M02 Establish Base Result Settlement Regressions")]
    public static void RunRegressionValidation()
    {
        try
        {
            RunValidation(RunFocusedValidation);
            RunValidation(M02EstablishBaseObjectiveTests.RunFocusedValidation);
            RunValidation(M02EstablishBaseWaveTests.RunFocusedValidation);
            RunValidation(M01FirstContactResultRuleTests.RunFocusedValidation);
            RunValidation(M01FirstContactSettlementTests.RunFocusedValidation);
            RunValidation(ProductionSourceGrowthArchitectureTests.RunFocusedValidation);
            Debug.Log("[M02EstablishBaseResultSettlementRegressionValidation] result=Passed suites=6");
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[M02EstablishBaseResultSettlementRegressionValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    [Test]
    public void RuntimeCompletesAfterBuildingAndRifleProductionResolve()
    {
        using BlobAssetReference<CampaignMissionCatalogBlob> blob = CreateBlob();
        ref CampaignMissionDefinitionBlob definition = ref blob.Value.Missions[0];
        CampaignMissionRuntimeComponent runtime = Runtime(MissionPhaseKind.Engage);
        CampaignMissionAttemptFactsComponent facts = CompleteFacts();
        Assert.IsTrue(CampaignMissionRuntimeProgressUtility.TryEvaluateSettled(
            in runtime, in facts, false, ref definition, out CampaignMissionRuntimeComponent result));
        Assert.AreEqual(MissionPhaseKind.Result, result.Phase);
        Assert.AreEqual(MissionOutcomeKind.Victory, result.Outcome);
        Assert.AreEqual(MissionReturnDestinationKind.CampaignOperations, result.ReturnDestination);
    }

    [Test]
    public void IncompleteRequirementsCannotResolveVictory()
    {
        using BlobAssetReference<CampaignMissionCatalogBlob> blob = CreateBlob();
        ref CampaignMissionDefinitionBlob definition = ref blob.Value.Missions[0];
        CampaignMissionRuntimeComponent runtime = Runtime(MissionPhaseKind.Engage);
        CampaignMissionAttemptFactsComponent complete = CompleteFacts();
        CampaignMissionAttemptFactsComponent[] incomplete =
        {
            WithBuildingCount(complete, 0),
            WithUnitCount(complete, 0)
        };
        for (int index = 0; index < incomplete.Length; index++)
            Assert.IsFalse(CampaignMissionRuntimeProgressUtility.TryEvaluateSettled(
                in runtime, in incomplete[index], false, ref definition, out _), $"case={index}");
    }

    [Test]
    public void M02VictoryDoesNotRequireHostilesOrDefenseWave()
    {
        using BlobAssetReference<CampaignMissionCatalogBlob> blob = CreateBlob();
        ref CampaignMissionDefinitionBlob definition = ref blob.Value.Missions[0];
        CampaignMissionRuntimeComponent runtime = Runtime(MissionPhaseKind.Engage);
        CampaignMissionAttemptFactsComponent facts = CompleteFacts();
        facts.HostileTotalCount = 0;
        facts.HostileDefeatedCount = 0;
        facts.DefenseWaveActivated = 0;
        facts.ForwardPostBound = 0;
        facts.ForwardPostDestroyed = 1;
        Assert.IsTrue(CampaignMissionRuntimeProgressUtility.TryEvaluateSettled(
            in runtime, in facts, false, ref definition, out CampaignMissionRuntimeComponent result));
        Assert.AreEqual(MissionOutcomeKind.Victory, result.Outcome);
        Assert.AreEqual(MissionReturnDestinationKind.CampaignOperations, result.ReturnDestination);
    }

    [Test]
    public void CivilianAndFiveMinuteStarsAreIndependent()
    {
        AssertProjectedStars(299999, 0, 3);
        AssertProjectedStars(299999, 1, 2);
        AssertProjectedStars(300000, 0, 2);
        AssertProjectedStars(300000, 1, 1);
    }

    [Test]
    public void InvalidOrContradictoryResultFactsFailClosed()
    {
        using BlobAssetReference<CampaignMissionCatalogBlob> blob = CreateBlob();
        ref CampaignMissionDefinitionBlob definition = ref blob.Value.Missions[0];
        CampaignMissionRuntimeComponent runtime = Runtime(
            MissionPhaseKind.Result, MissionOutcomeKind.Victory);
        CampaignMissionAttemptFactsComponent facts = CompleteFacts();
        facts.CivilianLossCount = facts.CivilianTotalCount + 1;
        Assert.IsFalse(CampaignMissionResultProjectionSystem.TryProject(
            in runtime, in facts, ref definition, out _));
        facts = CompleteFacts();
        facts.RequiredUnitProducedCount = 0;
        Assert.IsFalse(CampaignMissionResultProjectionSystem.TryProject(
            in runtime, in facts, ref definition, out _));
    }

    [Test]
    public void FirstClearGrantsRewardsBarracksAndM03ExactlyOnce() => WithStore(context =>
    {
        using BlobAssetReference<CampaignMissionCatalogBlob> blob = CreateBlob();
        CampaignMissionSettlementResultElement first = Settle(
            context.Store, blob, "first", 1, MissionRunKind.FirstClear, 3, 240000);
        CampaignMissionSettlementResultElement duplicate = Settle(
            context.Store, blob, "first", 1, MissionRunKind.FirstClear, 1, 100000);
        PlayerProfileSaveData profile = context.Service.LoadProfile();
        Assert.AreEqual(1, first.Accepted);
        Assert.AreEqual(1, first.FirstClear);
        Assert.AreEqual(1, duplicate.FirstClear);
        Assert.AreEqual("already-settled", duplicate.ReasonCode.ToString());
        Assert.AreEqual(320, profile.commanderXp);
        Assert.AreEqual(1500, profile.credits);
        CollectionAssert.AreEqual(new[] { Barracks }, profile.ownedBuildingUnlocks);
        CampaignMissionProgressSaveData[] progress = context.Store.ReadAll();
        Assert.IsTrue(Array.Find(progress, entry => entry.missionId == M03).available);
        Assert.AreEqual(1, Array.Find(progress, entry => entry.missionId == M02).settledTokens.Length);
    });

    [Test]
    public void ExistingBarracksConvertsUnlockToBlueprintPartsExactlyOnce() => WithStore(context =>
    {
        context.Service.SaveProfile(new PlayerProfileSaveData
        {
            ownedBuildingUnlocks = new[] { Barracks }
        });
        using BlobAssetReference<CampaignMissionCatalogBlob> blob = CreateBlob();
        Settle(context.Store, blob, "conversion", 1, MissionRunKind.FirstClear, 2, 250000);
        Settle(context.Store, blob, "conversion", 1, MissionRunKind.FirstClear, 3, 200000);
        PlayerProfileSaveData profile = context.Service.LoadProfile();
        Assert.AreEqual(1, profile.blueprintParts.Length);
        Assert.AreEqual(TrainingFacilities, profile.blueprintParts[0].targetItemId);
        Assert.AreEqual(1, profile.blueprintParts[0].amount);
    });

    [Test]
    public void ReplayGrantsOnlyReplayCredits() => WithStore(context =>
    {
        using BlobAssetReference<CampaignMissionCatalogBlob> blob = CreateBlob();
        Settle(context.Store, blob, "first", 1, MissionRunKind.FirstClear, 2, 260000);
        CampaignMissionSettlementResultElement replay = Settle(
            context.Store, blob, "replay", 2, MissionRunKind.Replay, 3, 180000);
        Assert.AreEqual(0, replay.FirstClear);
        PlayerProfileSaveData profile = context.Service.LoadProfile();
        Assert.AreEqual(320, profile.commanderXp);
        Assert.AreEqual(1800, profile.credits);
        Assert.AreEqual(0, profile.blueprintParts.Length);
        Assert.AreEqual(1, Array.Find(context.Store.ReadAll(), entry => entry.missionId == M02)
            .successfulReplayCount);
    });

    [Test]
    public void CampaignRetryBeforeFirstClearGrantsFirstClearRewards() => WithStore(context =>
    {
        context.Store.SetPendingResume(M02, true, 1);
        using BlobAssetReference<CampaignMissionCatalogBlob> blob = CreateBlob();

        CampaignMissionSettlementResultElement result = Settle(
            context.Store, blob, "retry-first-clear", 2, MissionRunKind.Retry, 3, 210000);

        PlayerProfileSaveData profile = context.Service.LoadProfile();
        Assert.AreEqual(1, result.Accepted, result.ReasonCode.ToString());
        Assert.AreEqual(1, result.FirstClear);
        Assert.AreEqual(320, profile.commanderXp);
        Assert.AreEqual(1500, profile.credits);
        CollectionAssert.AreEqual(new[] { Barracks }, profile.ownedBuildingUnlocks);
        CampaignMissionProgressSaveData progress =
            Array.Find(context.Store.ReadAll(), entry => entry.missionId == M02);
        Assert.IsTrue(progress.firstClearCompleted);
        Assert.IsFalse(progress.pendingResume);
    });

    [Test]
    public void ReplayRetryAfterFirstClearGrantsOnlyReplayRewards() => WithStore(context =>
    {
        using BlobAssetReference<CampaignMissionCatalogBlob> blob = CreateBlob();
        Settle(context.Store, blob, "first", 1, MissionRunKind.FirstClear, 2, 260000);
        context.Store.SetPendingResume(M02, true, 2);

        CampaignMissionSettlementResultElement result = Settle(
            context.Store, blob, "retry-replay", 3, MissionRunKind.Retry, 3, 180000);

        PlayerProfileSaveData profile = context.Service.LoadProfile();
        Assert.AreEqual(1, result.Accepted, result.ReasonCode.ToString());
        Assert.AreEqual(0, result.FirstClear);
        Assert.AreEqual(320, profile.commanderXp);
        Assert.AreEqual(1800, profile.credits);
        CampaignMissionProgressSaveData progress =
            Array.Find(context.Store.ReadAll(), entry => entry.missionId == M02);
        Assert.AreEqual(1, progress.successfulReplayCount);
        Assert.IsFalse(progress.pendingResume);
    });

    [Test]
    public void FirstClearUsesDebriefWhileReplayReturnsDirectly()
    {
        Assert.AreEqual(MissionPhaseKind.DebriefFirstClear, ContinuePhase(firstClear: true));
        Assert.AreEqual(MissionPhaseKind.ReturnReplay, ContinuePhase(firstClear: false));
    }

    [Test]
    public void DebriefCompletionShowsFinalVictoryBeforeReturn()
    {
        using World world = new(nameof(DebriefCompletionShowsFinalVictoryBeforeReturn));
        EntityManager entityManager = world.EntityManager;
        Entity root = entityManager.CreateEntity(typeof(CampaignMissionRootComponent));
        CampaignMissionRuntimeComponent runtime = Runtime(
            MissionPhaseKind.DebriefFirstClear,
            MissionOutcomeKind.Victory);
        runtime.TransitionToken = 41;
        entityManager.AddComponentData(root, runtime);
        entityManager.AddBuffer<CampaignMissionActionRequestElement>(root);
        entityManager.AddBuffer<CampaignMissionActionResultElement>(root);
        using EntityQuery query = entityManager.CreateEntityQuery(
            ComponentType.ReadWrite<CampaignMissionRuntimeComponent>());

        Assert.IsTrue(CampaignMissionRuntimeProgressUtility.TryCompleteDebrief(
            entityManager, query, runtime.SessionToken, runtime.AttemptOrdinal));
        CampaignMissionRuntimeComponent finalResult =
            entityManager.GetComponentData<CampaignMissionRuntimeComponent>(root);
        Assert.AreEqual(MissionPhaseKind.ResultAfterDebrief, finalResult.Phase);

        entityManager.GetBuffer<CampaignMissionActionRequestElement>(root).Add(new()
        {
            Action = MissionActionKind.Continue,
            TransitionToken = finalResult.TransitionToken,
            SessionToken = finalResult.SessionToken,
            AttemptOrdinal = finalResult.AttemptOrdinal
        });
        Assert.IsTrue(CampaignMissionRuntimeSystem.TryConsumeAction(entityManager, root));
        Assert.AreEqual(
            MissionPhaseKind.ReturnReplay,
            entityManager.GetComponentData<CampaignMissionRuntimeComponent>(root).Phase);
    }

    [Test]
    public void FirstClearResultQueuesDebriefBeforeVictory()
    {
        using BlobAssetReference<CampaignMissionCatalogBlob> blob = CreateBlob();
        using World world = new(nameof(FirstClearResultQueuesDebriefBeforeVictory));
        EntityManager entityManager = world.EntityManager;
        Entity root = entityManager.CreateEntity(typeof(CampaignMissionRootComponent));
        CampaignMissionRuntimeComponent runtime = Runtime(
            MissionPhaseKind.Result,
            MissionOutcomeKind.Victory);
        runtime.TransitionToken = 41;
        entityManager.AddComponentData(root, runtime);
        entityManager.AddComponentData(root, new CampaignMissionCatalogComponent
        {
            Blob = blob,
            SourceVersion = runtime.SourceVersion,
            OwnsBlob = 0
        });
        entityManager.AddComponentData(root, new CampaignMissionResultComponent
        {
            MissionId = runtime.MissionId,
            SessionToken = runtime.SessionToken,
            AttemptOrdinal = runtime.AttemptOrdinal,
            SourceVersion = runtime.Version,
            Outcome = runtime.Outcome,
            ReturnDestination = runtime.ReturnDestination
        });
        entityManager.AddBuffer<CampaignMissionSettlementResultElement>(root).Add(new()
        {
            SourceVersion = runtime.Version,
            SessionToken = runtime.SessionToken,
            Accepted = 1,
            FirstClear = 1
        });
        DynamicBuffer<CampaignMissionActionRequestElement> requests =
            entityManager.AddBuffer<CampaignMissionActionRequestElement>(root);
        using EntityQuery query = entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<CampaignMissionRuntimeComponent>());

        Assert.IsTrue(CampaignMissionResultDebriefTransitionUtility.TryQueueFirstClearDebrief(
            entityManager, query, runtime.MissionId));
        Assert.AreEqual(1, requests.Length);
        Assert.AreEqual(MissionActionKind.Continue, requests[0].Action);
        Assert.IsTrue(CampaignMissionResultDebriefTransitionUtility.TryQueueFirstClearDebrief(
            entityManager, query, runtime.MissionId));
        Assert.AreEqual(1, requests.Length, "The automatic debrief request must be idempotent.");
    }

    [Test]
    public void RestartPreservesUnlockRewardsAndSettlementHistory() => WithStore(context =>
    {
        using BlobAssetReference<CampaignMissionCatalogBlob> blob = CreateBlob();
        Settle(context.Store, blob, "persist", 4, MissionRunKind.FirstClear, 3, 200000);
        CampaignMissionProgressStore restarted = new(
            new SaveService(new JsonSaveRepository(context.Root)));
        CampaignMissionSettlementReceipt duplicate = restarted.SettleWithRewards(
            M02, "persist", 4, true, 1, 100000, M03, FirstClearRewards());
        PlayerProfileSaveData profile = context.Service.LoadProfile();
        Assert.IsTrue(duplicate.IsDuplicate);
        CollectionAssert.AreEqual(new[] { Barracks }, profile.ownedBuildingUnlocks);
        Assert.AreEqual(1500, profile.credits);
    });

    [Test]
    public void UnknownOrMisScopedCustomRewardsFailClosed() => WithStore(context =>
    {
        Assert.Throws<ArgumentException>(() => context.Store.SettleWithRewards(
            M02, "unknown", 1, true, 1, 1, M03,
            new[] { new CampaignMissionRewardGrant(MissionRewardKind.None, "reward.unknown", 1) }));
        Assert.Throws<ArgumentException>(() => context.Store.SettleWithRewards(
            M02, "replay-unlock", 2, false, 1, 1, M03,
            new[] { new CampaignMissionRewardGrant(
                MissionRewardKind.None, "reward.ch01.m02.production_unlock", 1) }));
    });

    private static void AssertProjectedStars(int elapsed, int civilianLosses, byte expected)
    {
        using BlobAssetReference<CampaignMissionCatalogBlob> blob = CreateBlob();
        ref CampaignMissionDefinitionBlob definition = ref blob.Value.Missions[0];
        CampaignMissionRuntimeComponent runtime = Runtime(
            MissionPhaseKind.Result, MissionOutcomeKind.Victory);
        CampaignMissionAttemptFactsComponent facts = CompleteFacts();
        facts.ElapsedMilliseconds = elapsed;
        facts.CivilianLossCount = civilianLosses;
        Assert.IsTrue(CampaignMissionResultProjectionSystem.TryProject(
            in runtime, in facts, ref definition, out CampaignMissionResultComponent result));
        Assert.AreEqual(expected, result.Stars);
        Assert.AreEqual(civilianLosses, result.CivilianLossCount);
    }

    private static MissionPhaseKind ContinuePhase(bool firstClear)
    {
        using World world = new("M02 result route");
        EntityManager entityManager = world.EntityManager;
        Entity root = entityManager.CreateEntity(typeof(CampaignMissionRootComponent));
        CampaignMissionRuntimeComponent runtime = Runtime(
            MissionPhaseKind.Result, MissionOutcomeKind.Victory);
        runtime.TransitionToken = 41;
        entityManager.AddComponentData(root, runtime);
        entityManager.AddBuffer<CampaignMissionActionRequestElement>(root).Add(new()
        {
            Action = MissionActionKind.Continue,
            TransitionToken = 41,
            SessionToken = runtime.SessionToken,
            AttemptOrdinal = runtime.AttemptOrdinal
        });
        entityManager.AddBuffer<CampaignMissionActionResultElement>(root);
        entityManager.AddBuffer<CampaignMissionLaunchRequestElement>(root);
        entityManager.AddBuffer<CampaignMissionSettlementResultElement>(root).Add(new()
        {
            SourceVersion = runtime.Version,
            SessionToken = runtime.SessionToken,
            Accepted = 1,
            FirstClear = firstClear ? (byte)1 : (byte)0
        });
        Assert.IsTrue(CampaignMissionRuntimeSystem.TryConsumeAction(entityManager, root));
        return entityManager.GetComponentData<CampaignMissionRuntimeComponent>(root).Phase;
    }

    private static CampaignMissionSettlementResultElement Settle(
        CampaignMissionProgressStore store,
        BlobAssetReference<CampaignMissionCatalogBlob> blob,
        string session,
        int attempt,
        MissionRunKind runKind,
        byte stars,
        int elapsed)
    {
        FixedString64Bytes missionId = new(M02);
        FixedString64Bytes token = new(session);
        CampaignMissionSettlementRequestElement request = new()
        {
            SourceVersion = 4,
            MissionId = missionId,
            SessionToken = token,
            AttemptOrdinal = attempt,
            Outcome = MissionOutcomeKind.Victory
        };
        CampaignMissionRuntimeComponent runtime = Runtime(
            MissionPhaseKind.Result, MissionOutcomeKind.Victory);
        runtime.SessionToken = token;
        runtime.AttemptOrdinal = attempt;
        runtime.RunKind = runKind;
        runtime.LaunchOrigin = MissionLaunchOriginKind.CampaignOperations;
        CampaignMissionResultComponent result = new()
        {
            MissionId = missionId,
            SessionToken = token,
            AttemptOrdinal = attempt,
            SourceVersion = 4,
            Outcome = MissionOutcomeKind.Victory,
            ReturnDestination = MissionReturnDestinationKind.CampaignOperations,
            Stars = stars,
            ElapsedMilliseconds = elapsed
        };
        ref CampaignMissionDefinitionBlob definition = ref blob.Value.Missions[0];
        return CampaignMissionProgressSettlementSystem.Settle(
            store, in request, in runtime, in result, ref definition);
    }

    private static CampaignMissionRuntimeComponent Runtime(
        MissionPhaseKind phase,
        MissionOutcomeKind outcome = MissionOutcomeKind.None) => new()
    {
        MissionId = new FixedString64Bytes(M02),
        ScenarioId = new FixedString64Bytes("scenario.ch01.m02.establish_base"),
        OperationMapId = new FixedString64Bytes("opmap.ch01.forward_post_01"),
        SessionToken = new FixedString64Bytes("m02-result"),
        AttemptOrdinal = 1,
        Version = 4,
        SourceVersion = 3,
        DeterministicSeed = 2002001,
        Phase = phase,
        Outcome = outcome,
        LaunchOrigin = MissionLaunchOriginKind.CampaignOperations,
        RunKind = MissionRunKind.FirstClear,
        ReturnDestination = outcome == MissionOutcomeKind.None
            ? MissionReturnDestinationKind.None
            : MissionReturnDestinationKind.CampaignOperations
    };

    private static CampaignMissionAttemptFactsComponent CompleteFacts() => new()
    {
        ElapsedMilliseconds = 240000,
        HostileTotalCount = 0,
        HostileDefeatedCount = 0,
        RequiredBuildingPlacedCount = 1,
        RequiredBuildingCompletedCount = 1,
        RequiredUnitProducedCount = 1,
        CivilianTotalCount = 12,
        ForwardPostBound = 0,
        DefenseWaveActivated = 0,
        CommandSquadSpawned = 1,
        CommandSquadAlive = 1,
        FinalePresentationComplete = 1
    };

    private static CampaignMissionAttemptFactsComponent WithBuildingCount(
        CampaignMissionAttemptFactsComponent facts, int value)
    {
        facts.RequiredBuildingCompletedCount = value;
        return facts;
    }

    private static CampaignMissionAttemptFactsComponent WithUnitCount(
        CampaignMissionAttemptFactsComponent facts, int value)
    {
        facts.RequiredUnitProducedCount = value;
        return facts;
    }

    private static CampaignMissionAttemptFactsComponent WithWaveActivation(
        CampaignMissionAttemptFactsComponent facts, byte value)
    {
        facts.DefenseWaveActivated = value;
        return facts;
    }

    private static CampaignMissionAttemptFactsComponent WithHostilesDefeated(
        CampaignMissionAttemptFactsComponent facts, int value)
    {
        facts.HostileDefeatedCount = value;
        return facts;
    }

    private static CampaignMissionAttemptFactsComponent WithForwardPostBound(
        CampaignMissionAttemptFactsComponent facts, byte value)
    {
        facts.ForwardPostBound = value;
        return facts;
    }

    private static CampaignMissionRewardGrant[] FirstClearRewards() => new CampaignMissionRewardGrant[]
    {
        new(MissionRewardKind.None, "reward.commander_xp", 320),
        new(MissionRewardKind.Credits, string.Empty, 1500),
        new(MissionRewardKind.None, "reward.ch01.m02.production_unlock", 1)
    };

    private static BlobAssetReference<CampaignMissionCatalogBlob> CreateBlob()
    {
        BlobBuilder builder = new(Allocator.Temp);
        ref CampaignMissionCatalogBlob catalog = ref builder.ConstructRoot<CampaignMissionCatalogBlob>();
        BlobBuilderArray<CampaignMissionDefinitionBlob> missions = builder.Allocate(ref catalog.Missions, 1);
        ref CampaignMissionDefinitionBlob mission = ref missions[0];
        mission.MissionId = new FixedString64Bytes(M02);
        mission.ScenarioId = new FixedString64Bytes("scenario.ch01.m02.establish_base");
        mission.OperationMapId = new FixedString64Bytes("opmap.ch01.forward_post_01");
        mission.DebriefSequenceId = new FixedString64Bytes("seq.ch01.m02.debrief");
        BlobBuilderArray<CampaignMissionObjectiveBlob> objectives =
            builder.Allocate(ref mission.Objectives, 2);
        objectives[0] = Objective(
            "obj.ch01.m02.build_forward_barracks", MissionObjectiveRuleKind.BuildStructure,
            string.Empty, Barracks, 1, false);
        objectives[1] = Objective(
            "obj.ch01.m02.produce_rifle_squad", MissionObjectiveRuleKind.ProduceUnit,
            string.Empty, "Unit_Chr_Soldier_Male_02_Alt_04", 1, false);
        BlobBuilderArray<CampaignMissionStarRuleBlob> stars = builder.Allocate(ref mission.StarRules, 3);
        stars[0] = new CampaignMissionStarRuleBlob
            { StarIndex = 1, Rule = MissionStarRuleKind.CompleteMission };
        stars[1] = new CampaignMissionStarRuleBlob
            { StarIndex = 2, Rule = MissionStarRuleKind.NoCivilianLoss };
        stars[2] = new CampaignMissionStarRuleBlob
        {
            StarIndex = 3,
            Rule = MissionStarRuleKind.CompleteUnderMilliseconds,
            Threshold = 300000
        };
        BlobBuilderArray<CampaignMissionRewardBlob> first =
            builder.Allocate(ref mission.FirstClearRewards, 3);
        first[0] = Reward(MissionRewardKind.None, "reward.commander_xp", 320);
        first[1] = Reward(MissionRewardKind.Credits, string.Empty, 1500);
        first[2] = Reward(MissionRewardKind.None, "reward.ch01.m02.production_unlock", 1);
        BlobBuilderArray<CampaignMissionRewardBlob> replay =
            builder.Allocate(ref mission.ReplayRewards, 1);
        replay[0] = Reward(MissionRewardKind.Credits, string.Empty, 300);
        BlobAssetReference<CampaignMissionCatalogBlob> blob =
            builder.CreateBlobAssetReference<CampaignMissionCatalogBlob>(Allocator.Persistent);
        builder.Dispose();
        return blob;
    }

    private static CampaignMissionObjectiveBlob Objective(
        string id,
        MissionObjectiveRuleKind rule,
        string role,
        string target,
        int requiredCount,
        bool failure) => new()
    {
        ObjectiveId = new FixedString64Bytes(id),
        MissionRoleId = new FixedString64Bytes(role),
        TargetConfigId = new FixedString64Bytes(target),
        Rule = rule,
        RequiredCount = requiredCount,
        FailureOnRuleBreak = failure ? (byte)1 : (byte)0
    };

    private static CampaignMissionRewardBlob Reward(
        MissionRewardKind kind,
        string id,
        int amount) => new()
    {
        Kind = kind,
        RewardConfigId = new FixedString64Bytes(id),
        Amount = amount
    };

    private static void RunValidation(Action validation)
    {
        ValidationExit.ClearLastExitCode();
        using (ValidationExit.SuppressProcessExit())
            validation();
        if (ValidationExit.LastExitCode is int exitCode && exitCode != 0)
            throw new InvalidOperationException(
                $"{validation.Method.DeclaringType?.Name}.{validation.Method.Name} failed validation.");
    }

    private static void WithStore(Action<StoreContext> action)
    {
        string root = Path.Combine(Path.GetTempPath(), "M02Settlement", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            SaveService service = new(new JsonSaveRepository(root));
            action(new StoreContext(root, service, new CampaignMissionProgressStore(service)));
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, true);
        }
    }

    private readonly struct StoreContext
    {
        public StoreContext(string root, SaveService service, CampaignMissionProgressStore store)
        {
            Root = root;
            Service = service;
            Store = store;
        }

        public string Root { get; }
        public SaveService Service { get; }
        public CampaignMissionProgressStore Store { get; }
    }
}
#endif
