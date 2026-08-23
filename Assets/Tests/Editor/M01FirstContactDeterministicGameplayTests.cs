#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Game.Components;
using Game.Configs;
using Game.Missions.Contracts;
using Game.Narrative.Contracts;
using Game.Runtime;
using Game.Tactical.Contracts;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

public static class M01FirstContactDeterministicGameplayTests
{
    private const int ExpectedCases = 10;
    private const string MissionPath =
        "Assets/Game/Configs/Missions/Chapter01/MissionDefinition_Ch01_M01_FirstContact.asset";
    private const string ScenarioPath =
        "Assets/Game/Configs/Scenarios/Chapter01/ScenarioSetup_Ch01_M01_FirstContact.asset";

    public static void RunFocusedValidation()
    {
        try
        {
            string[] first = RunMatrix();
            string[] second = RunMatrix();
            Require(first.Length == ExpectedCases - 1 && second.Length == first.Length,
                $"Expected {ExpectedCases - 1} state cases per repeat.");
            CollectionAssert.AreEqual(first, second, "Equal inputs must produce equal state transitions.");
            string hash = Hash(string.Join("\n", first));
            Require(hash == Hash(string.Join("\n", second)), "Repeated state hashes must match.");
            Debug.Log($"[M01FirstContactDeterministicGameplayValidation] result=Passed cases={ExpectedCases} repeats=2 hash={hash}");
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[M01FirstContactDeterministicGameplayValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    private static string[] RunMatrix()
    {
        MissionDefinitionConfig mission = AssetDatabase.LoadAssetAtPath<MissionDefinitionConfig>(MissionPath);
        ScenarioSetupConfig scenario = AssetDatabase.LoadAssetAtPath<ScenarioSetupConfig>(ScenarioPath);
        Require(mission != null && scenario != null, "Canonical M01 gameplay assets are required.");
        List<string> cases = new(9)
        {
            CommandAvailability(mission),
            PatrolBehavior(scenario),
            VictoryOutcome(),
            DefeatOutcome(),
            RetryOutcome(),
            StarBoundary(239999, 0, 3),
            StarBoundary(240000, 0, 2),
            RewardIdempotency(),
            CivilianContract(scenario)
        };
        return cases.ToArray();
    }

    private static string CommandAvailability(MissionDefinitionConfig mission)
    {
        ReadOnlySpan<TacticalCommandMode> commands = mission.CommandPolicy.AllowedCommands;
        TacticalCommandMode[] expected =
        {
            TacticalCommandMode.Select, TacticalCommandMode.Move, TacticalCommandMode.Attack,
            TacticalCommandMode.Hold, TacticalCommandMode.Stop
        };
        Require(commands.Length == expected.Length, "M01 must expose exactly five commands.");
        StringBuilder value = new();
        for (int i = 0; i < expected.Length; i++)
        {
            Require(commands[i] == expected[i], $"Unexpected command at index {i}.");
            if (i != 0) value.Append(',');
            value.Append(commands[i]);
        }
        return "commands|" + value;
    }

    private static string PatrolBehavior(ScenarioSetupConfig scenario)
    {
        Require(scenario.DeterministicSeed != 0 && scenario.PatrolRoutes.Length == 1,
            "M01 requires one deterministic hostile patrol route.");
        ScenarioPatrolRouteConfig route = scenario.PatrolRoutes[0];
        Require(route.RouteId == "route.ch01.m01.hostile_patrol" &&
                route.UnitGroupId == "group.ch01.m01.hostile_patrol" && route.AnchorIds.Length == 3 &&
                route.StartDelayMilliseconds >= scenario.EncounterStartMilliseconds,
            "M01 patrol identity, anchors, or start timing drifted.");
        return $"patrol|{scenario.DeterministicSeed}|{route.RouteId}|{route.UnitGroupId}|" +
               $"{route.AnchorIds[0]}>{route.AnchorIds[1]}>{route.AnchorIds[2]}|{route.StartDelayMilliseconds}";
    }

    private static string VictoryOutcome()
    {
        CampaignMissionRuntimeComponent runtime = Runtime(MissionPhaseKind.Engage);
        CampaignMissionAttemptFactsComponent facts = Facts(alive: 1, defeated: 3, elapsed: 90000);
        Require(CampaignMissionRuntimeSystem.TryEvaluate(in runtime, in facts, out var secure) &&
                secure.Phase == MissionPhaseKind.SecureCorridor,
            "Defeating the exact patrol must enter SecureCorridor.");
        Require(CampaignMissionRuntimeSystem.TryEvaluate(in secure, in facts, out var result) &&
                result.Phase == MissionPhaseKind.Result && result.Outcome == MissionOutcomeKind.Victory &&
                result.ReturnDestination == MissionReturnDestinationKind.CommandBase,
            "First-clear victory must produce the command-base result route.");
        return $"victory|{secure.Phase}:{secure.Version}|{result.Phase}:{result.Outcome}:" +
               $"{result.ReturnDestination}:{result.Version}";
    }

    private static string DefeatOutcome()
    {
        CampaignMissionRuntimeComponent runtime = Runtime(MissionPhaseKind.Engage);
        CampaignMissionAttemptFactsComponent facts = Facts(alive: 0, defeated: 1, elapsed: 45000);
        Require(CampaignMissionRuntimeSystem.TryEvaluate(in runtime, in facts, out var result) &&
                result.Phase == MissionPhaseKind.Result && result.Outcome == MissionOutcomeKind.Defeat &&
                result.ReturnDestination == MissionReturnDestinationKind.CampaignOperations,
            "Command-squad destruction must be the sole deterministic defeat.");
        return $"defeat|{result.Phase}:{result.Outcome}:{result.ReturnDestination}:{result.Version}";
    }

    private static string RetryOutcome()
    {
        using World world = new("M01DC033-retry");
        EntityManager em = world.EntityManager;
        Entity root = em.CreateEntity(typeof(CampaignMissionRootComponent));
        CampaignMissionRuntimeComponent runtime = Runtime(MissionPhaseKind.Result);
        runtime.Outcome = MissionOutcomeKind.Defeat;
        runtime.ReturnDestination = MissionReturnDestinationKind.CampaignOperations;
        runtime.TransitionToken = 900;
        runtime.AttemptOrdinal = 2;
        em.AddComponentData(root, runtime);
        em.AddBuffer<CampaignMissionActionRequestElement>(root);
        em.AddBuffer<CampaignMissionActionResultElement>(root);
        em.AddBuffer<CampaignMissionLaunchRequestElement>(root);
        em.AddBuffer<CampaignMissionSettlementResultElement>(root);
        DynamicBuffer<CampaignMissionActionRequestElement> requests =
            em.GetBuffer<CampaignMissionActionRequestElement>(root);
        requests.Add(new CampaignMissionActionRequestElement
        {
            Action = MissionActionKind.Retry, TransitionToken = runtime.TransitionToken,
            SessionToken = runtime.SessionToken, AttemptOrdinal = runtime.AttemptOrdinal
        });
        Require(CampaignMissionRuntimeSystem.TryConsumeAction(em, root), "Retry action was not consumed.");
        DynamicBuffer<CampaignMissionLaunchRequestElement> launches =
            em.GetBuffer<CampaignMissionLaunchRequestElement>(root);
        Require(launches.Length == 1 && launches[0].RunKind == MissionRunKind.Retry &&
                launches[0].AttemptOrdinal == 3 && launches[0].TransitionToken == 901 &&
                launches[0].DeterministicSeed == runtime.DeterministicSeed,
            "Retry must queue one correlated next attempt with the same deterministic seed.");
        return $"retry|{launches[0].RunKind}:{launches[0].AttemptOrdinal}:" +
               $"{launches[0].TransitionToken}:{launches[0].DeterministicSeed}";
    }

    private static string StarBoundary(int elapsed, int losses, byte expected)
    {
        using BlobAssetReference<CampaignMissionCatalogBlob> blob = StarCatalog();
        ref CampaignMissionDefinitionBlob definition = ref blob.Value.Missions[0];
        Require(CampaignMissionResultProjectionSystem.TryEvaluateStars(
                    MissionOutcomeKind.Victory, elapsed, losses, ref definition.StarRules, out byte stars) &&
                stars == expected,
            $"Unexpected star result at {elapsed} ms.");
        return $"stars|{elapsed}:{losses}:{stars}";
    }

    private static string RewardIdempotency()
    {
        string root = Path.Combine(Path.GetTempPath(), "M01DC033", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            SaveService service = new(new JsonSaveRepository(root));
            CampaignMissionProgressStore store = new(service);
            CampaignMissionRewardGrant[] rewards =
            {
                new(MissionRewardKind.None, "reward.commander_xp", 260),
                new(MissionRewardKind.Credits, string.Empty, 1200)
            };
            CampaignMissionSettlementReceipt first = store.SettleWithRewards(
                "saga.ch01.m01.first_contact", "deterministic", 1, true, 3, 90000,
                "saga.ch01.m02.establish_base", rewards);
            CampaignMissionSettlementReceipt duplicate = store.SettleWithRewards(
                "saga.ch01.m01.first_contact", "deterministic", 1, true, 3, 60000,
                "saga.ch01.m02.establish_base", rewards);
            PlayerProfileSaveData profile = service.LoadProfile();
            CampaignMissionProgressSaveData progress = store.ReadAll()[0];
            Require(first.Applied && duplicate.IsDuplicate && profile.commanderXp == 260 &&
                    profile.credits == 1200 && profile.intel == 0 && progress.settledTokens.Length == 1,
                "Duplicate settlement changed rewards or progress.");
            return $"rewards|{first.Applied}:{duplicate.IsDuplicate}:{profile.commanderXp}:" +
                   $"{profile.credits}:{profile.intel}:{progress.settledTokens.Length}";
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    private static string CivilianContract(ScenarioSetupConfig scenario)
    {
        Require(scenario.AmbientPresentations.Length == 1 &&
                scenario.AmbientPresentations[0].InstanceCount == 24 &&
                scenario.AmbientPresentations[0].InstanceCount <=
                    CampaignMissionAmbientPresentationSystem.MaxCivilianPresentations,
            "M01 civilians must remain at twenty-four within the hard cap of thirty-two.");
        string source = File.ReadAllText(
            "Assets/Game/Scripts/Runtime/Missions/CampaignMissionAmbientPresentationSystem.cs");
        Require(source.Contains("PresentationSystemGroup", StringComparison.Ordinal) &&
                source.Contains("StripGameplayComponents", StringComparison.Ordinal),
            "Civilians must remain presentation-owned and gameplay-inert.");
        return $"civilians|{scenario.AmbientPresentations[0].PresentationId}:" +
               $"{scenario.AmbientPresentations[0].InstanceCount}:" +
               $"{CampaignMissionAmbientPresentationSystem.MaxCivilianPresentations}";
    }

    private static CampaignMissionRuntimeComponent Runtime(MissionPhaseKind phase) => new()
    {
        MissionId = new FixedString64Bytes("saga.ch01.m01.first_contact"),
        ScenarioId = new FixedString64Bytes("scenario.ch01.m01.first_contact"),
        OperationMapId = new FixedString64Bytes("opmap.ch01.district_edge_01"),
        SessionToken = new FixedString64Bytes("m01dc033"), Phase = phase,
        Outcome = MissionOutcomeKind.None, LaunchOrigin = MissionLaunchOriginKind.FirstLaunch,
        RunKind = MissionRunKind.FirstClear, ReturnDestination = MissionReturnDestinationKind.None,
        Guidance = NarrativeGuidanceMode.Full, ReplayTutorialEnabled = 1, TransitionToken = 700,
        Version = 7, SourceVersion = 5, AttemptOrdinal = 1, DeterministicSeed = 104729
    };

    private static CampaignMissionAttemptFactsComponent Facts(byte alive, int defeated, int elapsed) => new()
    {
        ElapsedMilliseconds = elapsed, HostileTotalCount = 3, HostileDefeatedCount = defeated,
        CommandSquadSpawned = 1, CommandSquadAlive = alive
    };

    private static BlobAssetReference<CampaignMissionCatalogBlob> StarCatalog()
    {
        BlobBuilder builder = new(Allocator.Temp);
        ref CampaignMissionCatalogBlob catalog = ref builder.ConstructRoot<CampaignMissionCatalogBlob>();
        BlobBuilderArray<CampaignMissionDefinitionBlob> missions = builder.Allocate(ref catalog.Missions, 1);
        BlobBuilderArray<CampaignMissionStarRuleBlob> rules = builder.Allocate(ref missions[0].StarRules, 3);
        rules[0] = new CampaignMissionStarRuleBlob
            { StarIndex = 1, Rule = MissionStarRuleKind.CompleteMission };
        rules[1] = new CampaignMissionStarRuleBlob
            { StarIndex = 2, Rule = MissionStarRuleKind.NoSquadLoss };
        rules[2] = new CampaignMissionStarRuleBlob
            { StarIndex = 3, Rule = MissionStarRuleKind.CompleteUnderMilliseconds, Threshold = 240000 };
        BlobAssetReference<CampaignMissionCatalogBlob> result =
            builder.CreateBlobAssetReference<CampaignMissionCatalogBlob>(Allocator.Persistent);
        builder.Dispose();
        return result;
    }

    private static string Hash(string value)
    {
        using SHA256 algorithm = SHA256.Create();
        byte[] bytes = algorithm.ComputeHash(Encoding.UTF8.GetBytes(value));
        return BitConverter.ToString(bytes).Replace("-", string.Empty);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
#endif
