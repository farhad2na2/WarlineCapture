using System;
using Game.Components;
using Game.Missions.Contracts;
using Game.Runtime;
using NUnit.Framework;
using UnityEngine;
public sealed class M05BreachAssaultRuleTests
{
    public static void RunFocusedValidation()
    {
        try {var t=new M05BreachAssaultRuleTests();t.AllThreeObjectivesRequired();t.SupportLossIsRecoverable();t.FailureWinsAtDeadline();t.OpeningAndFinaleGateGameplay();Debug.Log("[M05BreachRules] result=Passed");ValidationExit.Passed();}
        catch(Exception e){Debug.LogException(e);ValidationExit.Failed();}
    }
    private static CampaignMissionAttemptFactsComponent Success()=>new(){CommandSquadSpawned=1,CommandSquadAlive=1,BreachGateDestroyed=1,BreachCoreDestroyed=1,BreachArchiveSecured=1};
    private static CampaignMissionRuntimeComponent Runtime(MissionPhaseKind phase)=>new(){MissionId="saga.ch01.m05.breach_assault",ScenarioId="scenario.ch01.m05.breach_assault",OperationMapId="opmap.ch01.breach_assault_01",SessionToken="m05-test",AttemptOrdinal=1,SourceVersion=1,Version=1,DeterministicSeed=5005,Phase=phase};
    [Test] public void AllThreeObjectivesRequired()
    {
        Assert.IsTrue(CampaignMissionBreachRuleUtility.IsVictory(Success()));
        for(int i=0;i<3;i++){var f=Success();if(i==0)f.BreachGateDestroyed=0;else if(i==1)f.BreachCoreDestroyed=0;else f.BreachArchiveSecured=0;Assert.IsFalse(CampaignMissionBreachRuleUtility.IsVictory(f));}
    }
    [Test] public void SupportLossIsRecoverable()
    {
        var f=Success();f.BreachSupportLost=1;f.SquadLossCount=1;
        Assert.IsTrue(CampaignMissionBreachRuleUtility.IsVictory(f));
        f.CommandSquadAlive=0;Assert.IsFalse(CampaignMissionBreachRuleUtility.IsVictory(f));Assert.IsTrue(CampaignMissionBreachRuleUtility.IsFailure(f));
    }
    [Test] public void FailureWinsAtDeadline()
    {
        var f=Success();f.BreachTimedOut=1;
        Assert.IsTrue(CampaignMissionBreachRuleUtility.TryAdvance(Runtime(MissionPhaseKind.Engage),f,true,true,out var next));Assert.AreEqual(MissionOutcomeKind.Defeat,next.Outcome);
        f=Success();f.HostileRosterIntegrityFault=1;Assert.IsFalse(CampaignMissionBreachRuleUtility.IsVictory(f));
    }
    [Test] public void OpeningAndFinaleGateGameplay()
    {
        var f=Success();Assert.IsFalse(CampaignMissionBreachRuleUtility.TryAdvance(Runtime(MissionPhaseKind.FindSquad),f,true,false,out _));
        Assert.IsFalse(CampaignMissionBreachRuleUtility.TryAdvance(Runtime(MissionPhaseKind.FindSquad),f,false,true,out _));
        f.FinalePresentationRequired=1;
        Assert.IsTrue(CampaignMissionBreachRuleUtility.TryAdvance(Runtime(MissionPhaseKind.Engage),f,true,true,out var next));Assert.AreEqual(MissionPhaseKind.SecureCorridor,next.Phase);
        Assert.IsFalse(CampaignMissionBreachRuleUtility.TryAdvance(next,f,true,true,out _));f.FinalePresentationComplete=1;
        Assert.IsTrue(CampaignMissionBreachRuleUtility.TryAdvance(next,f,true,true,out var result));Assert.AreEqual(MissionOutcomeKind.Victory,result.Outcome);Assert.AreEqual(MissionReturnDestinationKind.CampaignOperations,result.ReturnDestination);
    }
}
