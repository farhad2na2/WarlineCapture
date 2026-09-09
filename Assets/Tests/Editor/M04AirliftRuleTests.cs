using System;
using Game.Components;
using Game.Missions.Contracts;
using Game.Runtime;
using NUnit.Framework;
using UnityEngine;

public sealed class M04AirliftRuleTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            var tests = new M04AirliftRuleTests();
            tests.FullManifestAndGroundLegAreRequired();
            tests.FailureWinsOverSimultaneousDeparture();
            tests.OpeningMustFinishBeforeMissionStarts();
            tests.VictoryWaitsForFinale();
            Debug.Log("[M04AirliftRules] result=Passed cases=15"); ValidationExit.Passed();
        }
        catch (Exception error) { Debug.LogException(error); ValidationExit.Failed(); }
    }
    private static CampaignMissionRuntimeComponent Runtime(MissionPhaseKind phase) => new()
    {
        MissionId = "saga.ch01.m04.airlift", ScenarioId = "scenario.ch01.m04.airlift", OperationMapId = "opmap.ch01.airlift_01",
        SessionToken = "m04-test", AttemptOrdinal = 0, SourceVersion = 1, Version = 1, DeterministicSeed = 4004, Phase = phase
    };
    private static CampaignMissionAttemptFactsComponent Success() => new()
    {
        CommandSquadSpawned = 1, CommandSquadAlive = 1, ExtractionPassengerTotal = 4, ExtractionCarrierLegCount = 4,
        ExtractionPassengersDelivered = 4, ExtractionDeparted = 1
    };
    [Test] public void FullManifestAndGroundLegAreRequired()
    {
        var facts = Success(); Assert.IsTrue(CampaignMissionExtractionRuleUtility.IsVictory(facts, 4));
        facts.ExtractionPassengersDelivered = 3; Assert.IsFalse(CampaignMissionExtractionRuleUtility.IsVictory(facts, 4));
        facts = Success(); facts.ExtractionCarrierLegCount = 3; Assert.IsFalse(CampaignMissionExtractionRuleUtility.IsVictory(facts, 4));
        facts = Success(); facts.ExtractionDeparted = 0; Assert.IsFalse(CampaignMissionExtractionRuleUtility.IsVictory(facts, 4));
        facts = Success(); facts.ExtractionPassengerTotal = 5; Assert.IsFalse(CampaignMissionExtractionRuleUtility.IsVictory(facts, 4));
    }
    [Test] public void FailureWinsOverSimultaneousDeparture()
    {
        for (int i = 0; i < 6; i++)
        {
            var facts = Success();
            switch (i) { case 0: facts.ExtractionAircraftLost = 1; break; case 1: facts.ExtractionCarrierLost = 1; break;
                case 2: facts.CivilianLossCount = 1; break; case 3: facts.HostileRosterIntegrityFault = 1; break;
                case 4: facts.ExtractionTimedOut = 1; break; case 5: facts.CommandSquadAlive = 0; break; }
            Assert.IsFalse(CampaignMissionExtractionRuleUtility.IsVictory(facts, 4));
            Assert.IsTrue(CampaignMissionExtractionRuleUtility.TryAdvance(Runtime(MissionPhaseKind.Engage), facts, true, true, 4, out var next));
            Assert.AreEqual(MissionOutcomeKind.Defeat, next.Outcome);
        }
    }
    [Test] public void OpeningMustFinishBeforeMissionStarts()
    {
        var facts = Success();
        Assert.IsFalse(CampaignMissionExtractionRuleUtility.TryAdvance(Runtime(MissionPhaseKind.FindSquad), facts, true, false, 4, out _));
        Assert.IsFalse(CampaignMissionExtractionRuleUtility.TryAdvance(Runtime(MissionPhaseKind.FindSquad), facts, false, true, 4, out _));
        Assert.IsTrue(CampaignMissionExtractionRuleUtility.TryAdvance(Runtime(MissionPhaseKind.FindSquad), facts, true, true, 4, out var next));
        Assert.AreEqual(MissionPhaseKind.Engage, next.Phase);
    }
    [Test] public void VictoryWaitsForFinale()
    {
        var facts = Success(); facts.FinalePresentationRequired = 1;
        Assert.IsTrue(CampaignMissionExtractionRuleUtility.TryAdvance(Runtime(MissionPhaseKind.Engage), facts, true, true, 4, out var next));
        Assert.AreEqual(MissionPhaseKind.SecureCorridor, next.Phase); Assert.AreEqual(MissionOutcomeKind.None, next.Outcome);
        facts.FinalePresentationComplete = 1;
        Assert.IsTrue(CampaignMissionExtractionRuleUtility.TryAdvance(next, facts, true, true, 4, out var result));
        Assert.AreEqual(MissionOutcomeKind.Victory, result.Outcome);
    }
}
