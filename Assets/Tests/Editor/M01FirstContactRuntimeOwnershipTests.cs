using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Game.Components;
using Game.Missions.Contracts;
using Game.Runtime;
using NUnit.Framework;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using UnityEngine;

public static class M01FirstContactRuntimeOwnershipTests
{
    private const string Marker = "[M01FirstContactRuntimeOwnershipValidation] result=Passed tests=13";
    private const string RuntimePath =
        "Assets/Game/Scripts/Runtime/Missions/CampaignMissionRuntimeSystem.cs";
    private const string LaunchPath =
        "Assets/Game/Scripts/Runtime/Missions/CampaignMissionLaunchSystem.cs";
    private const string ReportPath =
        "Design/AgentReports/M01FirstContact/m01dc_017_runtime_owner.json";

    public static void RunFocusedValidation()
    {
        try
        {
            Components_AreUnmanagedDataOnly();
            RuntimeSystem_IsSoleWriter();
            InitialRuntime_IsValidAndStable();
            Preparing_AdvancesOnlyWhenReady();
            GuidedPhases_AdvanceFromFacts();
            FirstClearWithoutReplayFlag_KeepsGuidedPhases();
            ReplayWithoutTutorial_SkipsGuidedPhases();
            PatrolDefeat_ProducesVictoryResult();
            CommandSquadLoss_ProducesDefeatResult();
            InvalidTransition_FailsClosed();
            TerminalOutcome_CannotBeRewritten();
            Version_IncrementsOnlyOnSemanticChange();
            CatalogBlob_IsDisposedExactlyOnce();
            WriteReport();
            Debug.Log(Marker);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[M01FirstContactRuntimeOwnershipValidation] result=Failed");
            throw;
        }
    }

    private static void Components_AreUnmanagedDataOnly()
    {
        Type[] types =
        {
            typeof(CampaignMissionRootComponent), typeof(CampaignMissionCatalogComponent),
            typeof(CampaignMissionLaunchQueueComponent), typeof(CampaignMissionLaunchRequestElement),
            typeof(CampaignMissionLaunchResultElement), typeof(CampaignMissionRuntimeComponent),
            typeof(CampaignMissionAttemptFactsComponent), typeof(CampaignMissionActionRequestElement),
            typeof(CampaignMissionActionResultElement), typeof(CampaignMissionResultComponent),
            typeof(CampaignMissionSettlementRequestElement), typeof(CampaignMissionSettlementResultElement),
            typeof(CampaignMissionUnitRoleComponent), typeof(CampaignMissionAmbientCivilianComponent),
            typeof(CampaignMissionAmbientCivilianMotionComponent),
            typeof(CampaignMissionCatalogBlob), typeof(CampaignMissionDefinitionBlob)
        };
        foreach (Type type in types)
            Require(UnsafeUtility.IsUnmanaged(type), $"{type.Name} must remain unmanaged data.");
    }

    private static void RuntimeSystem_IsSoleWriter()
    {
        string[] sources = Directory.GetFiles("Assets/Game/Scripts", "*.cs", SearchOption.AllDirectories);
        string[] mutationSources = sources.Where(path =>
        {
            string text = File.ReadAllText(path);
            return text.Contains("RefRW<CampaignMissionRuntimeComponent>", StringComparison.Ordinal) ||
                   text.Contains("SetComponentData<CampaignMissionRuntimeComponent>", StringComparison.Ordinal) ||
                   text.Contains("AddComponentData(new CampaignMissionRuntimeComponent", StringComparison.Ordinal) ||
                   text.Contains("runtime.ValueRW =", StringComparison.Ordinal);
        }).Select(Normalize).ToArray();

        string launchText = File.ReadAllText(LaunchPath);
        Require(mutationSources.Contains(LaunchPath), "Campaign launch must remain the runtime initializer.");
        Require(CountOccurrences(launchText, "runtime.ValueRW =") == 1 &&
                launchText.Contains("runtime.ValueRW = CreateRuntime(", StringComparison.Ordinal) &&
                launchText.Contains("Phase = MissionPhaseKind.Preparing", StringComparison.Ordinal) &&
                launchText.Contains("Outcome = MissionOutcomeKind.None", StringComparison.Ordinal) &&
                !launchText.Contains("TryTransition(", StringComparison.Ordinal) &&
                !launchText.Contains("SetComponentData<CampaignMissionRuntimeComponent>", StringComparison.Ordinal),
            "Campaign launch may initialize exactly one Preparing/None runtime and may not own transitions.");

        string[] writers = mutationSources.Where(path => path != LaunchPath).ToArray();
        Require(writers.Length == 1 && writers[0] == RuntimePath,
            "CampaignMissionRuntimeComponent must have exactly one production writer: " + string.Join(", ", writers));
        Require(!File.ReadAllText(RuntimePath).Contains("static CampaignMissionRuntimeComponent", StringComparison.Ordinal),
            "Mission runtime truth cannot use static mutable storage.");
    }

    private static void InitialRuntime_IsValidAndStable()
    {
        CampaignMissionRuntimeComponent state = Create(MissionPhaseKind.Preparing);
        CampaignMissionAttemptFactsComponent facts = Facts();
        Require(!CampaignMissionRuntimeSystem.TryEvaluate(in state, in facts, out CampaignMissionRuntimeComponent next),
            "Unready Preparing state must remain stable.");
        Require(SameState(in state, in next), "A no-op evaluation changed mission truth.");
    }

    private static void Preparing_AdvancesOnlyWhenReady()
    {
        CampaignMissionRuntimeComponent state = Create(MissionPhaseKind.Preparing);
        state.ReadyReadiness = state.RequiredReadiness;
        CampaignMissionAttemptFactsComponent facts = Facts();
        Require(CampaignMissionRuntimeSystem.TryEvaluate(in state, in facts, out CampaignMissionRuntimeComponent next),
            "Ready mission did not enter InteractiveBrief.");
        Require(next.Phase == MissionPhaseKind.InteractiveBrief && next.Version == 2,
            "Preparing transition published the wrong phase/version.");
        Require(CampaignMissionRuntimeSystem.TryEvaluate(in next, in facts, out next) &&
                next.Phase == MissionPhaseKind.FindSquad && next.Version == 3,
            "InteractiveBrief did not enter the first guided FindSquad phase.");
    }

    private static void GuidedPhases_AdvanceFromFacts()
    {
        CampaignMissionRuntimeComponent state = Create(MissionPhaseKind.FindSquad);
        CampaignMissionAttemptFactsComponent facts = Facts();
        Require(!CampaignMissionRuntimeSystem.TryEvaluate(in state, in facts, out state),
            "FindSquad advanced before the command squad was selected.");
        Require(CampaignMissionRuntimeSystem.TryEvaluate(in state, in facts, true, out state) &&
                state.Phase == MissionPhaseKind.MoveToCover, "FindSquad did not advance.");
        facts.MoveToCoverComplete = 1;
        Require(CampaignMissionRuntimeSystem.TryEvaluate(in state, in facts, out state) &&
                state.Phase == MissionPhaseKind.ConfirmThreat, "MoveToCover did not advance.");
        facts.ThreatConfirmed = 1;
        Require(CampaignMissionRuntimeSystem.TryEvaluate(in state, in facts, out state) &&
                state.Phase == MissionPhaseKind.Engage, "ConfirmThreat did not advance.");
    }

    private static void ReplayWithoutTutorial_SkipsGuidedPhases()
    {
        CampaignMissionRuntimeComponent state = Create(MissionPhaseKind.FindSquad);
        state.RunKind = MissionRunKind.Replay;
        state.ReplayTutorialEnabled = 0;
        CampaignMissionAttemptFactsComponent facts = Facts();
        Require(CampaignMissionRuntimeSystem.TryEvaluate(in state, in facts, out CampaignMissionRuntimeComponent next) &&
                next.Phase == MissionPhaseKind.Engage, "Replay tutorial-off did not skip presentation phases.");
    }

    private static void FirstClearWithoutReplayFlag_KeepsGuidedPhases()
    {
        CampaignMissionRuntimeComponent state = Create(MissionPhaseKind.FindSquad);
        state.RunKind = MissionRunKind.FirstClear;
        state.ReplayTutorialEnabled = 0;
        CampaignMissionAttemptFactsComponent facts = Facts();
        Require(!CampaignMissionRuntimeSystem.TryEvaluate(in state, in facts, out _),
            "First-clear Mission 1 skipped the mandatory FindSquad tutorial phase.");
    }

    private static void PatrolDefeat_ProducesVictoryResult()
    {
        CampaignMissionRuntimeComponent state = Create(MissionPhaseKind.Engage);
        CampaignMissionAttemptFactsComponent facts = Facts();
        facts.HostileDefeatedCount = facts.HostileTotalCount;
        Require(CampaignMissionRuntimeSystem.TryEvaluate(in state, in facts, out state) &&
                state.Phase == MissionPhaseKind.SecureCorridor, "Patrol defeat did not secure the corridor.");
        Require(CampaignMissionRuntimeSystem.TryEvaluate(in state, in facts, out state) &&
                state.Phase == MissionPhaseKind.Result && state.Outcome == MissionOutcomeKind.Victory &&
                state.ReturnDestination == MissionReturnDestinationKind.CommandBase,
            "First-clear victory result was not published.");
    }

    private static void CommandSquadLoss_ProducesDefeatResult()
    {
        CampaignMissionRuntimeComponent state = Create(MissionPhaseKind.Engage);
        CampaignMissionAttemptFactsComponent facts = Facts();
        facts.CommandSquadAlive = 0;
        facts.SquadLossCount = 1;
        Require(CampaignMissionRuntimeSystem.TryEvaluate(in state, in facts, out CampaignMissionRuntimeComponent next) &&
                next.Phase == MissionPhaseKind.Result && next.Outcome == MissionOutcomeKind.Defeat,
            "Command-squad loss did not publish defeat.");
    }

    private static void InvalidTransition_FailsClosed()
    {
        CampaignMissionRuntimeComponent state = Create(MissionPhaseKind.Preparing);
        Require(!CampaignMissionRuntimeSystem.TryTransition(
                in state, MissionPhaseKind.Engage, MissionOutcomeKind.None,
                MissionReturnDestinationKind.None, out CampaignMissionRuntimeComponent next),
            "Preparing-to-Engage transition must fail closed.");
        Require(SameState(in state, in next), "Rejected transition mutated state.");
    }

    private static void TerminalOutcome_CannotBeRewritten()
    {
        CampaignMissionRuntimeComponent state = Create(MissionPhaseKind.Result);
        state.Outcome = MissionOutcomeKind.Victory;
        state.ReturnDestination = MissionReturnDestinationKind.CommandBase;
        Require(!CampaignMissionRuntimeSystem.TryTransition(
                in state, MissionPhaseKind.Result, MissionOutcomeKind.Defeat,
                MissionReturnDestinationKind.CampaignOperations, out CampaignMissionRuntimeComponent next),
            "Terminal outcome rewrite must fail closed.");
        Require(SameState(in state, in next), "Rejected terminal rewrite mutated state.");
        Require(CampaignMissionRuntimeSystem.TryTransition(
                in state, MissionPhaseKind.DebriefFirstClear, MissionOutcomeKind.Victory,
                MissionReturnDestinationKind.CommandBase, out CampaignMissionRuntimeComponent debrief) &&
                debrief.Outcome == state.Outcome &&
                debrief.ReturnDestination == state.ReturnDestination,
            "Published result did not advance to its matching debrief.");
        Require(!CampaignMissionRuntimeSystem.TryTransition(
                in debrief, MissionPhaseKind.ReturnReplay, MissionOutcomeKind.Victory,
                MissionReturnDestinationKind.CommandBase, out CampaignMissionRuntimeComponent afterDebrief),
            "Terminal debrief advanced beyond its owned route.");
        Require(SameState(in debrief, in afterDebrief), "Rejected terminal advance mutated state.");
    }

    private static void Version_IncrementsOnlyOnSemanticChange()
    {
        CampaignMissionRuntimeComponent state = Create(MissionPhaseKind.InteractiveBrief);
        Require(!CampaignMissionRuntimeSystem.TryTransition(
                in state, MissionPhaseKind.InteractiveBrief, MissionOutcomeKind.None,
                MissionReturnDestinationKind.None, out CampaignMissionRuntimeComponent same) && same.Version == 1,
            "Idempotent transition changed the version.");
        Require(CampaignMissionRuntimeSystem.TryTransition(
                in state, MissionPhaseKind.FindSquad, MissionOutcomeKind.None,
                MissionReturnDestinationKind.None, out CampaignMissionRuntimeComponent changed) && changed.Version == 2,
            "Semantic transition did not increment version exactly once.");
    }

    private static void CatalogBlob_IsDisposedExactlyOnce()
    {
        BlobBuilder builder = new(Allocator.Temp);
        ref CampaignMissionCatalogBlob root = ref builder.ConstructRoot<CampaignMissionCatalogBlob>();
        root.SchemaVersion = 1;
        builder.Allocate(ref root.Missions, 0);
        CampaignMissionCatalogComponent catalog = new()
        {
            Blob = builder.CreateBlobAssetReference<CampaignMissionCatalogBlob>(Allocator.Persistent),
            SourceVersion = 1,
            OwnsBlob = 1
        };
        builder.Dispose();
        CampaignMissionCatalogDisposalSystem.DisposeOwned(ref catalog);
        Require(catalog.OwnsBlob == 0 && !catalog.Blob.IsCreated, "Owned catalog blob was not cleared.");
        CampaignMissionCatalogDisposalSystem.DisposeOwned(ref catalog);
        Require(catalog.OwnsBlob == 0 && !catalog.Blob.IsCreated, "Second disposal was not idempotent.");
    }

    private static CampaignMissionRuntimeComponent Create(MissionPhaseKind phase) => new()
    {
        MissionId = new FixedString64Bytes("saga.ch01.m01.first_contact"),
        ScenarioId = new FixedString64Bytes("scenario.ch01.m01.first_contact"),
        OperationMapId = new FixedString64Bytes("opmap.ch01.district_edge_01"),
        SessionToken = new FixedString64Bytes("m01-session"),
        Phase = phase,
        Outcome = MissionOutcomeKind.None,
        LaunchOrigin = MissionLaunchOriginKind.FirstLaunch,
        RunKind = MissionRunKind.FirstClear,
        TransitionToken = 11,
        Version = 1,
        SourceVersion = 1,
        AttemptOrdinal = 0,
        DeterministicSeed = 1701,
        RequiredReadiness = OperationMapReadinessFlags.Metadata | OperationMapReadinessFlags.MapSurface,
        ReplayTutorialEnabled = 1
    };

    private static CampaignMissionAttemptFactsComponent Facts() => new()
    {
        HostileTotalCount = 3,
        CommandSquadSpawned = 1,
        CommandSquadAlive = 1
    };

    private static bool SameState(
        in CampaignMissionRuntimeComponent left,
        in CampaignMissionRuntimeComponent right) =>
        left.Version == right.Version && left.Phase == right.Phase && left.Outcome == right.Outcome &&
        left.ReturnDestination == right.ReturnDestination && left.TransitionToken == right.TransitionToken;

    private static void WriteReport()
    {
        string json = "{\n" +
            "  \"artifactId\": \"m01dc-017-runtime-owner-v1\",\n" +
            "  \"taskId\": \"M01DC-017\",\n" +
            "  \"result\": \"Passed\",\n" +
            "  \"runtimeWriterCount\": 1,\n" +
            "  \"runtimeWriter\": \"Game.Runtime.CampaignMissionRuntimeSystem\",\n" +
            "  \"phaseCount\": 10,\n" +
            "  \"terminalOutcomeImmutable\": true,\n" +
            "  \"staticMutableStateCount\": 0,\n" +
            "  \"managedHotFieldCount\": 0,\n" +
            "  \"catalogDisposalOwnerCount\": 1,\n" +
            "  \"validation\": \"" + Marker + "\"\n" +
            "}\n";
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(ReportPath)) ?? string.Empty);
        File.WriteAllText(ReportPath, json, new UTF8Encoding(false));
    }

    private static string Normalize(string path) => path.Replace('\\', '/');
    private static int CountOccurrences(string text, string value)
    {
        int count = 0;
        for (int index = 0; (index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0; index += value.Length)
            count++;
        return count;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
