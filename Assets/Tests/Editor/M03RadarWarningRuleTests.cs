using System;
using Game.Components;
using Game.Missions.Contracts;
using Game.Runtime;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public sealed class M03RadarWarningRuleTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            M03RadarWarningRuleTests tests = new();
            tests.AllDeclaredMembersMustBeDefeatedAndActivated();
            tests.LossWinsOverLastKill(0);
            tests.LossWinsOverLastKill(1);
            tests.LossWinsOverLastKill(2);
            tests.SquadLossDoesNotInventAnAdditionalDefeatCondition();
            tests.CameraAndProducerReadinessGateControlHandoff();
            tests.TerminalOutcomeDoesNotChange();
            tests.FinaleDelaysPresentationWithoutAdvancingOrOverridingOutcome();
            for (int civilianLoss = 0; civilianLoss <= 1; civilianLoss++)
                for (byte damaged = 0; damaged <= 1; damaged++)
                    tests.StarGoalsAreIndependent(civilianLoss, damaged);
            Debug.Log("[M03RadarWarningRuleValidation] result=Passed tests=12");
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[M03RadarWarningRuleValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    [Test]
    public void AllDeclaredMembersMustBeDefeatedAndActivated()
    {
        CampaignMissionRuntimeComponent runtime = Runtime();
        CampaignMissionAttemptFactsComponent facts = VictoryFacts();
        facts.DefenseWaveActivated = 0;
        Assert.IsFalse(CampaignMissionDefenseRuleUtility.TryAdvance(in runtime, in facts, true, out _));
        facts.DefenseWaveActivated = 1;
        facts.HostileDefeatedCount = 6;
        Assert.IsFalse(CampaignMissionDefenseRuleUtility.TryAdvance(in runtime, in facts, true, out _));
        facts.HostileDefeatedCount = 7;
        Assert.IsTrue(CampaignMissionDefenseRuleUtility.TryAdvance(in runtime, in facts, true, out CampaignMissionRuntimeComponent result));
        Assert.AreEqual(MissionOutcomeKind.Victory, result.Outcome);
    }

    [TestCase(0), TestCase(1), TestCase(2)]
    public void LossWinsOverLastKill(int loss)
    {
        CampaignMissionRuntimeComponent runtime = Runtime();
        CampaignMissionAttemptFactsComponent facts = VictoryFacts();
        if (loss == 0) facts.ForwardPostDestroyed = 1;
        if (loss == 1) facts.CoreBreached = 1;
        if (loss == 2) facts.HostileRosterIntegrityFault = 1;
        Assert.IsTrue(CampaignMissionDefenseRuleUtility.TryAdvance(in runtime, in facts, true, out CampaignMissionRuntimeComponent result));
        Assert.AreEqual(MissionOutcomeKind.Defeat, result.Outcome);
    }

    [Test]
    public void SquadLossDoesNotInventAnAdditionalDefeatCondition()
    {
        CampaignMissionRuntimeComponent runtime = Runtime();
        CampaignMissionAttemptFactsComponent facts = VictoryFacts();
        facts.CommandSquadAlive = 0;
        facts.SquadLossCount = 8;
        Assert.IsTrue(CampaignMissionDefenseRuleUtility.TryAdvance(in runtime, in facts, true, out CampaignMissionRuntimeComponent result));
        Assert.AreEqual(MissionOutcomeKind.Victory, result.Outcome);
    }

    [Test]
    public void CameraAndProducerReadinessGateControlHandoff()
    {
        CampaignMissionRuntimeComponent runtime = Runtime();
        runtime.Phase = MissionPhaseKind.FindSquad;
        CampaignMissionAttemptFactsComponent facts = VictoryFacts();
        Assert.IsFalse(CampaignMissionDefenseRuleUtility.TryAdvance(in runtime, in facts, false, out _));
        Assert.IsTrue(CampaignMissionDefenseRuleUtility.TryAdvance(in runtime, in facts, true, out CampaignMissionRuntimeComponent result));
        Assert.AreEqual(MissionPhaseKind.Engage, result.Phase);
        Assert.AreEqual(MissionOutcomeKind.None, result.Outcome);
    }

    [Test]
    public void FinaleDelaysPresentationWithoutAdvancingOrOverridingOutcome()
    {
        var runtime=Runtime(); var facts=VictoryFacts(); facts.FinalePresentationRequired=1;
        Assert.IsTrue(CampaignMissionDefenseRuleUtility.TryAdvance(in runtime,in facts,true,out runtime));
        Assert.AreEqual(MissionPhaseKind.SecureCorridor,runtime.Phase);
        Assert.AreEqual(MissionOutcomeKind.None,runtime.Outcome);
        Assert.IsFalse(CampaignMissionDefenseRuleUtility.TryAdvance(in runtime,in facts,true,out _));
        facts.FinalePresentationComplete=1;
        Assert.IsTrue(CampaignMissionDefenseRuleUtility.TryAdvance(in runtime,in facts,true,out var result));
        Assert.AreEqual(MissionOutcomeKind.Victory,result.Outcome);
        facts.ForwardPostDestroyed=1;
        Assert.IsTrue(CampaignMissionDefenseRuleUtility.TryAdvance(in runtime,in facts,true,out result));
        Assert.AreEqual(MissionOutcomeKind.Defeat,result.Outcome);
    }

    [Test]
    public void TerminalOutcomeDoesNotChange()
    {
        CampaignMissionRuntimeComponent runtime = Runtime();
        runtime.Phase = MissionPhaseKind.Result;
        runtime.Outcome = MissionOutcomeKind.Defeat;
        runtime.ReturnDestination = MissionReturnDestinationKind.CampaignOperations;
        CampaignMissionAttemptFactsComponent facts = VictoryFacts();
        Assert.IsFalse(CampaignMissionDefenseRuleUtility.TryAdvance(in runtime, in facts, true, out _));
    }

    [TestCase(0, (byte)0), TestCase(1, (byte)0), TestCase(0, (byte)1), TestCase(1, (byte)1)]
    public void StarGoalsAreIndependent(int civilianLoss, byte postDamaged)
    {
        using BlobBuilder builder = new(Allocator.Temp);
        ref CampaignMissionCatalogBlob catalog = ref builder.ConstructRoot<CampaignMissionCatalogBlob>();
        BlobBuilderArray<CampaignMissionDefinitionBlob> missions = builder.Allocate(ref catalog.Missions, 1);
        BlobBuilderArray<CampaignMissionStarRuleBlob> stars = builder.Allocate(ref missions[0].StarRules, 3);
        stars[0] = new() { Rule = MissionStarRuleKind.CompleteMission, StarIndex = 1 };
        stars[1] = new() { Rule = MissionStarRuleKind.NoCivilianLoss, StarIndex = 2 };
        stars[2] = new() { Rule = MissionStarRuleKind.NoPostDamage, StarIndex = 3 };
        using BlobAssetReference<CampaignMissionCatalogBlob> blob = builder.CreateBlobAssetReference<CampaignMissionCatalogBlob>(Allocator.Temp);
        Assert.IsTrue(CampaignMissionResultProjectionSystem.TryEvaluateStars(MissionOutcomeKind.Victory,
            190000, 2, civilianLoss, postDamaged, ref blob.Value.Missions[0].StarRules, out byte count));
        Assert.AreEqual(3 - civilianLoss - postDamaged, count);
        Assert.IsTrue(CampaignMissionResultProjectionSystem.TryEvaluateStars(MissionOutcomeKind.Defeat,
            190000, 2, civilianLoss, postDamaged, ref blob.Value.Missions[0].StarRules, out count));
        Assert.AreEqual(0, count);
    }

    internal static CampaignMissionRuntimeComponent Runtime() => new()
    {
        MissionId = "saga.ch01.m03.radar_warning", ScenarioId = "scenario.ch01.m03.radar_warning",
        OperationMapId = "opmap.ch01.convoy_approach_01", SessionToken = "m03-test", AttemptOrdinal = 1,
        Version = 1, SourceVersion = 1, DeterministicSeed = 3003001, Phase = MissionPhaseKind.Engage,
        LaunchOrigin = MissionLaunchOriginKind.CampaignOperations, RunKind = MissionRunKind.FirstClear
    };

    internal static CampaignMissionAttemptFactsComponent VictoryFacts() => new()
    {
        CommandSquadSpawned = 1, CommandSquadAlive = 1, ForwardPostBound = 1,
        HostileTotalCount = 7, HostileDefeatedCount = 7, DefenseWaveActivated = 1
    };
}
