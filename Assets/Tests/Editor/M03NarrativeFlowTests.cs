using System;
using Game.Components;
using Game.Composition;
using Game.Configs;
using Game.Missions.Contracts;
using Game.Runtime;
using Game.UI.Shell.Contracts.Ecs;
using Game.UI.Shell.Ecs;
using NUnit.Framework;
using Unity.Entities;
using UnityEngine;

public sealed class M03NarrativeFlowTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            new M03NarrativeFlowTests().M03CombatDoesNotSelectTheBlockingComic();
            new M03NarrativeFlowTests().RadioUsesExistingMessagesOncePerAttemptAndClearsOnExit();
            new M03NarrativeFlowTests().DebriefFollowsIndependentPostAndCivilianFacts();
            Debug.Log("[M03NarrativeFlowValidation] result=Passed tests=3"); ValidationExit.Passed();
        }
        catch(Exception exception) {Debug.LogException(exception); Debug.LogError("[M03NarrativeFlowValidation] result=Failed"); ValidationExit.Failed();}
    }
    [Test] public void M03CombatDoesNotSelectTheBlockingComic()
    {
        var runtime=new CampaignMissionRuntimeComponent {MissionId="saga.ch01.m03.radar_warning",Phase=MissionPhaseKind.Engage};
        var facts=new CampaignMissionAttemptFactsComponent {DefenseWaveWarningIssued=1};
        Assert.AreEqual(CampaignMissionDebriefCompositionSystemHelper.SequenceStage.None,
            CampaignMissionDebriefCompositionSystemHelper.ResolveStage(runtime,facts,true,false));
        runtime.MissionId="saga.ch01.m02.establish_base";
        Assert.AreEqual(CampaignMissionDebriefCompositionSystemHelper.SequenceStage.Comms,
            CampaignMissionDebriefCompositionSystemHelper.ResolveStage(runtime,facts,true,false));
    }
    [Test] public void DebriefFollowsIndependentPostAndCivilianFacts()
    {
        Unity.Collections.FixedString64Bytes mission="saga.ch01.m03.radar_warning",fallback="seq.legacy";
        foreach(byte damaged in new byte[]{0,1}) foreach(int lost in new[]{0,2})
        {
            var facts=new CampaignMissionAttemptFactsComponent{ForwardPostDamaged=damaged,CivilianLossCount=lost};
            string variant=lost>0 ? damaged!=0 ? "mixed" : "civilian-loss" : damaged!=0 ? "damaged" : "clean";
            Assert.AreEqual("seq.ch01.m03.debrief."+variant,CampaignMissionNarrativePolicy.ResolveDebrief(mission,facts,fallback).ToString());
        }
        mission="saga.ch01.m02.establish_base";
        Assert.AreEqual(fallback,CampaignMissionNarrativePolicy.ResolveDebrief(mission,default,fallback));
    }
    [Test] public void RadioUsesExistingMessagesOncePerAttemptAndClearsOnExit()
    {
        string original=GameLocalization.CurrentLocaleCode;
        try
        {
            GameLocalization.SetLocale("en",false);
            using var world=new World("M03 radio lifecycle"); var em=world.EntityManager;
            var root=em.CreateEntity(typeof(CampaignMissionRuntimeComponent),typeof(CampaignMissionAttemptFactsComponent),typeof(RuntimeGameplayStateComponent));
            var runtime=new CampaignMissionRuntimeComponent {MissionId="saga.ch01.m03.radar_warning",SessionToken="radio-test",AttemptOrdinal=1,SourceVersion=1,Phase=MissionPhaseKind.Engage};
            em.SetComponentData(root,runtime);
            em.SetComponentData(root,new CampaignMissionAttemptFactsComponent {ElapsedMilliseconds=45000});
            var boundary=em.CreateEntity(typeof(UiShellStateComponent)); em.AddBuffer<AssistantMessageElement>(boundary);
            em.SetComponentData(boundary,new UiShellStateComponent {ActiveRoute=Game.UI.Contracts.UIRoute.Match});
            var system=world.CreateSystem<M03RadioReportProjectionSystem>();
            system.Update(world.Unmanaged); Assert.AreEqual(0,em.GetBuffer<AssistantMessageElement>(boundary).Length,"Paused radio is held.");
            em.SetComponentData(root,new RuntimeGameplayStateComponent {PlayRequested=1,SimulationActive=1});
            system.Update(world.Unmanaged); system.Update(world.Unmanaged);
            Assert.AreEqual(1,em.GetBuffer<AssistantMessageElement>(boundary).Length);
            Assert.AreEqual("vo.aria.m03.comms.01.en",em.GetBuffer<AssistantMessageElement>(boundary)[0].AudioEventId.ToString());
            em.SetComponentData(root,new RuntimeGameplayStateComponent {PlayRequested=1,SimulationActive=0});
            system.Update(world.Unmanaged); Assert.AreEqual(1,em.GetBuffer<AssistantMessageElement>(boundary).Length,"Pause preserves the existing clue.");
            em.SetComponentData(root,new RuntimeGameplayStateComponent {PlayRequested=1,SimulationActive=1});
            GameLocalization.SetLocale("fa-IR",false); system.Update(world.Unmanaged);
            var radio=em.GetBuffer<AssistantMessageElement>(boundary)[0];
            Assert.AreEqual(AssistantMessagePriority.Normal,radio.Priority);
            Assert.IsTrue(radio.Text.ToString().Contains("کاروان"));
            Assert.AreEqual("vo.aria.m03.comms.01.fa",radio.AudioEventId.ToString());
            Assert.AreEqual(1,em.GetComponentData<RuntimeGameplayStateComponent>(root).SimulationActive);
            runtime.Outcome=MissionOutcomeKind.Defeat; em.SetComponentData(root,runtime); system.Update(world.Unmanaged);
            Assert.AreEqual(0,em.GetBuffer<AssistantMessageElement>(boundary).Length);
            runtime.Outcome=MissionOutcomeKind.None; runtime.AttemptOrdinal=2; em.SetComponentData(root,runtime);
            system.Update(world.Unmanaged); Assert.AreEqual(1,em.GetBuffer<AssistantMessageElement>(boundary).Length,"A fresh attempt may replay the clue once.");
            em.SetComponentData(root,default(RuntimeGameplayStateComponent));
            system.Update(world.Unmanaged); Assert.AreEqual(0,em.GetBuffer<AssistantMessageElement>(boundary).Length,"Exit clears the clue even before the old mission phase resets.");
        }
        finally {GameLocalization.SetLocale(original,false);}
    }
}
