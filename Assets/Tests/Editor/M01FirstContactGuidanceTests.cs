using System;
using System.IO;
using System.Linq;
using Game.Components;
using Game.Missions.Contracts;
using Game.Narrative.Contracts;
using Game.Runtime;
using Game.UI.Shell.Ecs;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public sealed class M01FirstContactGuidanceTests
{
    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            FullGuidanceProjectsAllFivePhases(); passed++; ShowMeAndDoItStayInsideAriaAuthority(); passed++;
            FindSquadUsesTypedSquadSelectionTarget(); passed++;
            MissingOptionalUiSettingsPreserveMissionGuidance(); passed++;
            LiveSystemProjectsFirstClearGuidanceWithoutOptionalSettings(); passed++;
            StablePhaseHonorsCooldownAndAcknowledgement(); passed++; AccessibilitySettingsAreProjectedWithoutChangingGameplay(); passed++;
            ReplayTutorialOffSuppressesGuidance(); passed++; StableProjectionAllocatesZeroManagedBytes(); passed++;
            ExactlyOneGuidanceProjectionWriterExists(); passed++; ContextualEscalatesWithoutUnsafeExecution(); passed++;
            MinimalPublishesMandatoryInformationOnly(); passed++; GuidanceModesPreserveGameplayTruth(); passed++;
            Debug.Log($"[M01FirstContactGuidanceValidation] result=Passed tests={passed}"); ValidationExit.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); Debug.LogError($"[M01FirstContactGuidanceValidation] result=Failed passed={passed}"); ValidationExit.Exit(1); }
    }

    [Test] public static void FullGuidanceProjectsAllFivePhases()
    {
        MissionPhaseKind[] phases = { MissionPhaseKind.FindSquad, MissionPhaseKind.MoveToCover, MissionPhaseKind.ConfirmThreat, MissionPhaseKind.Engage, MissionPhaseKind.SecureCorridor };
        for (int i = 0; i < phases.Length; i++)
        { var runtime = Runtime(phases[i]); Assert.That(CampaignMissionGuidanceProjectionSystem.TryBuildProjection(default, runtime, Facts(), Settings(),
              new Entity { Index = 10, Version = 1 }, new Entity { Index = 11, Version = 1 }, new float3(1), new float3(2), out var next), Is.True);
          Assert.That((int)next.Prompt, Is.EqualTo(i + 1)); Assert.That(next.Active, Is.EqualTo(1)); }
    }

    [Test] public static void ShowMeAndDoItStayInsideAriaAuthority()
    {
        var runtime = Runtime(MissionPhaseKind.MoveToCover); CampaignMissionGuidanceProjectionSystem.TryBuildProjection(default, runtime, Facts(), Settings(),
            new Entity { Index = 2, Version = 1 }, Entity.Null, new float3(4), default, out var projected);
        Assert.That(AssistantObjectiveProjectionUtility.TryBuildCampaignGuidanceRecommendation(projected, out var recommendation), Is.True);
        Assert.That(recommendation.ActionLabel.ToString(), Is.EqualTo("DO IT"));
        string source = File.ReadAllText("Assets/Game/Scripts/Runtime/Missions/CampaignMissionGuidanceProjectionSystem.cs");
        Assert.That(source, Does.Not.Contain("AssistantCommandIntentRequestElement")); Assert.That(source, Does.Not.Contain("UnitMoveOrder"));
    }

    [Test] public static void FindSquadUsesTypedSquadSelectionTarget()
    {
        Entity representative = new Entity { Index = 4, Version = 1 };
        CampaignMissionGuidanceProjectionSystem.TryBuildProjection(
            default, Runtime(MissionPhaseKind.FindSquad), Facts(), Settings(),
            representative, Entity.Null, default, default, out var projected);
        Assert.That(projected.TargetKind, Is.EqualTo(AssistantTargetKind.Squad));
        Assert.That(projected.TargetEntity, Is.EqualTo(representative));
        Assert.That(projected.CanExecute, Is.EqualTo(1));
        Assert.That(projected.ActionLabel.ToString(), Is.EqualTo("DO IT"));
    }

    [Test] public static void MissingOptionalUiSettingsPreserveMissionGuidance()
    {
        Entity representative = new Entity { Index = 4, Version = 1 };
        Assert.That(CampaignMissionGuidanceProjectionSystem.TryBuildProjection(
            default, Runtime(MissionPhaseKind.FindSquad), Facts(), default,
            representative, Entity.Null, default, default, out var projected), Is.True);
        Assert.That(projected.Title.ToString(), Is.EqualTo("Find your squad"));
        Assert.That(projected.TargetKind, Is.EqualTo(AssistantTargetKind.Squad));
        Assert.That(projected.TargetEntity, Is.EqualTo(representative));
        Assert.That(projected.CanExecute, Is.EqualTo(1));
        Assert.That(projected.SubtitlesEnabled + projected.LargeTextEnabled + projected.HighContrastEnabled, Is.Zero);
    }

    [Test] public static void LiveSystemProjectsFirstClearGuidanceWithoutOptionalSettings()
    {
        using World world = new("M01 guidance live-system test");
        EntityManager em = world.EntityManager;
        Entity root = em.CreateEntity(
            typeof(CampaignMissionRootComponent),
            typeof(CampaignMissionRuntimeComponent),
            typeof(CampaignMissionAttemptFactsComponent),
            typeof(CampaignMissionGuidanceProjectionComponent));
        em.AddBuffer<CampaignMissionGuidanceAcknowledgementRequestElement>(root);
        em.SetComponentData(root, Runtime(MissionPhaseKind.FindSquad));
        em.SetComponentData(root, Facts());
        Entity representative = em.CreateEntity(
            typeof(CampaignMissionUnitRoleComponent), typeof(Faction));
        em.SetComponentData(representative, new CampaignMissionUnitRoleComponent
        {
            MissionRoleId = new FixedString64Bytes("role.command_squad"),
            SessionToken = new FixedString64Bytes("session")
        });
        em.SetComponentData(representative, new Faction { Id = FactionIdentity.PlayerFactionId });

        SystemHandle system = world.CreateSystem<CampaignMissionGuidanceProjectionSystem>();
        system.Update(world.Unmanaged);

        CampaignMissionGuidanceProjectionComponent projected =
            em.GetComponentData<CampaignMissionGuidanceProjectionComponent>(root);
        Assert.That(projected.Active, Is.EqualTo(1));
        Assert.That(projected.Title.ToString(), Is.EqualTo("Find your squad"));
        Assert.That(projected.TargetKind, Is.EqualTo(AssistantTargetKind.Squad));
        Assert.That(projected.TargetEntity, Is.EqualTo(representative));
        Assert.That(projected.CanExecute, Is.EqualTo(1));
    }

    [Test] public static void StablePhaseHonorsCooldownAndAcknowledgement()
    {
        var runtime = Runtime(MissionPhaseKind.FindSquad); CampaignMissionGuidanceProjectionSystem.TryBuildProjection(default, runtime, Facts(), Settings(),
            new Entity { Index = 1, Version = 1 }, Entity.Null, default, default, out var current);
        Assert.That(CampaignMissionGuidanceProjectionSystem.TryBuildProjection(current, runtime, Facts(), Settings(), current.TargetEntity, Entity.Null, default, default, out _), Is.False);
        current.AcknowledgedGuidanceId = current.GuidanceId;
        Assert.That(CampaignMissionGuidanceProjectionSystem.TryBuildProjection(current, runtime, Facts(), Settings(), current.TargetEntity, Entity.Null, default, default, out _), Is.False);
        Assert.That(current.CooldownUntilMilliseconds, Is.GreaterThan(0));
    }

    [Test] public static void AccessibilitySettingsAreProjectedWithoutChangingGameplay()
    {
        var settings = Settings(); settings.LargeTextEnabled = 1; settings.HighContrastEnabled = 1;
        Entity hostile = new Entity { Index = 7, Version = 1 };
        CampaignMissionGuidanceProjectionSystem.TryBuildProjection(default, Runtime(MissionPhaseKind.ConfirmThreat), Facts(), settings,
            Entity.Null, hostile, default, new float3(3), out var projected);
        Assert.That(projected.SubtitlesEnabled + projected.LargeTextEnabled + projected.HighContrastEnabled, Is.EqualTo(3));
        Assert.That(projected.RecommendationKind, Is.EqualTo(AssistantRecommendationKind.CameraFocus));
        Assert.That(projected.TargetKind, Is.EqualTo(AssistantTargetKind.Entity));
        Assert.That(projected.TargetEntity, Is.EqualTo(hostile));
    }

    [Test] public static void ReplayTutorialOffSuppressesGuidance()
    {
        var runtime = Runtime(MissionPhaseKind.FindSquad); runtime.RunKind = MissionRunKind.Replay; runtime.ReplayTutorialEnabled = 0;
        Assert.That(CampaignMissionGuidanceProjectionSystem.TryBuildProjection(default, runtime, Facts(), Settings(), Entity.Null, Entity.Null, default, default, out _), Is.False);
    }

    [Test] public static void StableProjectionAllocatesZeroManagedBytes()
    {
        var runtime = Runtime(MissionPhaseKind.Engage); CampaignMissionGuidanceProjectionSystem.TryBuildProjection(default, runtime, Facts(), Settings(), Entity.Null, Entity.Null, default, default, out var current);
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 128; i++) CampaignMissionGuidanceProjectionSystem.TryBuildProjection(current, runtime, Facts(), Settings(), Entity.Null, Entity.Null, default, default, out _);
        Assert.That(GC.GetAllocatedBytesForCurrentThread() - before, Is.EqualTo(0));
    }

    [Test] public static void ExactlyOneGuidanceProjectionWriterExists()
    {
        string[] writers = Directory.GetFiles("Assets/Game/Scripts", "*.cs", SearchOption.AllDirectories).Where(path =>
            File.ReadAllText(path).Contains("SetComponentData(root, next)") && File.ReadAllText(path).Contains("CampaignMissionGuidanceProjectionComponent")).ToArray();
        Assert.That(writers, Has.Length.EqualTo(1)); Assert.That(writers[0].Replace('\\', '/'), Does.EndWith("CampaignMissionGuidanceProjectionSystem.cs"));
    }

    [Test] public static void ContextualEscalatesWithoutUnsafeExecution()
    {
        var runtime = Runtime(MissionPhaseKind.Engage); runtime.Guidance = NarrativeGuidanceMode.Contextual;
        CampaignMissionGuidanceProjectionSystem.TryBuildProjection(default, runtime, Facts(), Settings(),
            new Entity { Index = 2, Version = 1 }, new Entity { Index = 3, Version = 1 }, default, default, out var first);
        Assert.That(first.HintStrength, Is.EqualTo(1)); Assert.That(first.CanExecute, Is.Zero);
        var delayed = Facts(); delayed.ElapsedMilliseconds = first.CooldownUntilMilliseconds;
        Assert.That(CampaignMissionGuidanceProjectionSystem.TryBuildProjection(first, runtime, delayed, Settings(),
            first.SourceEntity, first.TargetEntity, default, default, out var stronger), Is.True);
        Assert.That(stronger.HintStrength, Is.EqualTo(2)); Assert.That(stronger.ActionLabel.ToString(), Is.EqualTo("SHOW ME"));
    }

    [Test] public static void MinimalPublishesMandatoryInformationOnly()
    {
        var runtime = Runtime(MissionPhaseKind.MoveToCover); runtime.Guidance = NarrativeGuidanceMode.Minimal;
        Assert.That(CampaignMissionGuidanceProjectionSystem.TryBuildProjection(default, runtime, Facts(), Settings(),
            Entity.Null, Entity.Null, default, default, out _), Is.False);
        runtime.Phase = MissionPhaseKind.ConfirmThreat;
        Assert.That(CampaignMissionGuidanceProjectionSystem.TryBuildProjection(default, runtime, Facts(), Settings(),
            Entity.Null, Entity.Null, default, new float3(2), out var mandatory), Is.True);
        Assert.That(mandatory.CanExecute, Is.Zero); Assert.That(mandatory.ActionLabel.ToString(), Is.EqualTo("SHOW ME"));
    }

    [Test] public static void GuidanceModesPreserveGameplayTruth()
    {
        var facts = Facts(); var baseline = Runtime(MissionPhaseKind.Engage);
        foreach (NarrativeGuidanceMode mode in new[] { NarrativeGuidanceMode.Full, NarrativeGuidanceMode.Contextual, NarrativeGuidanceMode.Minimal })
        {
            var runtime = baseline; runtime.Guidance = mode;
            CampaignMissionGuidanceProjectionSystem.TryBuildProjection(default, runtime, facts, Settings(), Entity.Null, Entity.Null, default, default, out _);
            Assert.That(runtime.MissionId, Is.EqualTo(baseline.MissionId)); Assert.That(runtime.Phase, Is.EqualTo(baseline.Phase));
            Assert.That(runtime.Outcome, Is.EqualTo(baseline.Outcome)); Assert.That(facts.HostileTotalCount, Is.EqualTo(3));
        }
    }

    private static CampaignMissionRuntimeComponent Runtime(MissionPhaseKind phase) => new()
    { MissionId = new FixedString64Bytes("saga.ch01.m01.first_contact"), SessionToken = new FixedString64Bytes("session"), Phase = phase,
      Outcome = MissionOutcomeKind.None, Guidance = NarrativeGuidanceMode.Full, RunKind = MissionRunKind.FirstClear,
      Version = 3, SourceVersion = 2, AttemptOrdinal = 0, ReplayTutorialEnabled = 1 };
    private static CampaignMissionAttemptFactsComponent Facts() => new() { ElapsedMilliseconds = 1000, CommandSquadSpawned = 1, CommandSquadAlive = 1, HostileTotalCount = 3 };
    private static AssistantSettingsComponent Settings() => new() { GuidanceLevel = AssistantGuidanceLevel.FullGuidance, SubtitlesEnabled = 1 };
}
