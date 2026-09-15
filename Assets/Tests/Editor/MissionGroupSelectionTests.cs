using System;
using Game.Components;
using Game.UI.Shell.Ecs;
using Game.Configs;
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public sealed class MissionGroupSelectionTests
{
    [Test]
    public void RescueSelectionResolvesSpecialistsInsteadOfOverlappingTransports()
    {
        using var world=new World("Mission group selection");var em=world.EntityManager;
        var root=em.CreateEntity(typeof(CampaignMissionExtractionState));
        Entity carrier=Actor(em),helicopter=Actor(em),specialist=Actor(em),second=Actor(em);
        em.SetComponentData(root,new CampaignMissionExtractionState {Carrier=carrier,Aircraft=helicopter});
        var members=em.AddBuffer<CampaignMissionExtractionMember>(root);
        members.Add(new CampaignMissionExtractionMember {Entity=carrier,Kind=2});
        members.Add(new CampaignMissionExtractionMember {Entity=helicopter,Kind=3});
        members.Add(new CampaignMissionExtractionMember {Entity=specialist,Kind=1});
        members.Add(new CampaignMissionExtractionMember {Entity=second,Kind=1});
        foreach(int step in new[]{4,5,9})
        {
            var guidance=new CampaignMissionGuidanceProjectionComponent {GuidanceId=55000+step,SourceEntity=helicopter};
            Assert.AreEqual(specialist,Resolve(em,root,guidance,float3.zero));
        }
        em.AddComponentData(specialist,new UnitTransportPassenger {Transport=helicopter});
        Assert.AreEqual(second,Resolve(em,root,new CampaignMissionGuidanceProjectionComponent {GuidanceId=55009},float3.zero));
        em.SetComponentData(second,new UnitHealth {Current=0});
        Assert.AreEqual(Entity.Null,Resolve(em,root,new CampaignMissionGuidanceProjectionComponent {GuidanceId=55009},float3.zero));
        Assert.AreEqual(carrier,Resolve(em,root,new CampaignMissionGuidanceProjectionComponent {GuidanceId=55002},float3.zero));
        Assert.AreEqual(helicopter,Resolve(em,root,new CampaignMissionGuidanceProjectionComponent {GuidanceId=55008},float3.zero));
    }
    [Test]
    public void DefendersSpanBothSquadsButExcludeOtherRolesAndAttempts()
    {
        var lead=new CampaignMissionUnitRoleComponent {SessionToken="attempt-a",UnitGroupId="squad-a",MissionRoleId="defender"};
        var other=lead;other.UnitGroupId="squad-b";
        Assert.IsFalse(Game.Runtime.RtsSelectionExternalCommandUtility.MatchesMissionSelection(other,lead,false),"Ordinary squad selection stays scoped to its squad.");
        Assert.IsTrue(Game.Runtime.RtsSelectionExternalCommandUtility.MatchesMissionSelection(other,lead,true),"Mission selection includes the second defensive squad.");
        other.MissionRoleId="sensor";
        Assert.IsFalse(Game.Runtime.RtsSelectionExternalCommandUtility.MatchesMissionSelection(other,lead,true));
        other.MissionRoleId="civilian";
        Assert.IsFalse(Game.Runtime.RtsSelectionExternalCommandUtility.MatchesMissionSelection(other,lead,true));
        other.MissionRoleId="defender";other.SessionToken="previous-attempt";
        Assert.IsFalse(Game.Runtime.RtsSelectionExternalCommandUtility.MatchesMissionSelection(other,lead,true));
    }
    [Test]
    public void SelectionCopyExistsInBothLanguageConfigs()
    {
        foreach(string key in new[]{"ui.aria.select_group","ui.aria.select_defenders","ui.aria.select_specialists","ui.aria.select_carrier","ui.aria.select_helicopter","ui.aria.select_group.title","ui.aria.select_group.body"})
        {
            bool found=false;
            foreach(var item in M03RadarWarningUiCopyCatalog.Entries)
                if(item.Key==key){Assert.IsNotEmpty(item.English);Assert.IsNotEmpty(item.Persian);Assert.AreNotEqual(item.English,item.Persian);found=true;}
            Assert.IsTrue(found,key);
        }
    }
    private static Entity Resolve(EntityManager em,Entity root,CampaignMissionGuidanceProjectionComponent guidance,float3 position)
        => (Entity)typeof(UiShellEcsGateway).GetMethod("ResolveTutorialSelectionActor",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{em,root,guidance,position});
    private static Entity Actor(EntityManager em)
    {
        var actor=em.CreateEntity(typeof(UnitMove),typeof(Faction),typeof(UnitHealth),typeof(LocalTransform));
        em.SetComponentData(actor,new Faction {Id=1});em.SetComponentData(actor,new UnitHealth {Current=100});
        return actor;
    }
    public static void RunFull()
    {
        try
        {
            using(ValidationExit.SuppressProcessExit())
            {
                ValidationExit.ClearLastExitCode();Run();
                if(ValidationExit.LastExitCode!=0) throw new Exception("Group selection checks failed.");
                ValidationExit.ClearLastExitCode();MissionReadinessArchitectureValidation.Run();
                if(ValidationExit.LastExitCode!=0) throw new Exception("Architecture checks failed.");
            }
            Debug.Log("[MissionGroupSelectionFull] result=Passed");ValidationExit.Passed();
        }
        catch(Exception error){Debug.LogException(error);Debug.LogError("[MissionGroupSelectionFull] result=Failed");ValidationExit.Failed();}
    }
    public static void Run()
    {
        try
        {
            Game.Editor.M03RadarWarningLocalizationBuilder.Import();
            var tests=new MissionGroupSelectionTests();tests.RescueSelectionResolvesSpecialistsInsteadOfOverlappingTransports();tests.SelectionCopyExistsInBothLanguageConfigs();tests.DefendersSpanBothSquadsButExcludeOtherRolesAndAttempts();
            MatchHudAssistantUiSystemHelperTests.RunShowMeValidation();
            new HudRightColumnLayoutValidation().MinimapDockAndContentHeightFollowActualControlsAndCopy();
            Debug.Log("[MissionGroupSelection] result=Passed M1-M5 HUD transitions, group routing, bilingual copy, layout");
            ValidationExit.Passed();
        }
        catch(Exception error){Debug.LogException(error);Debug.LogError("[MissionGroupSelection] result=Failed");ValidationExit.Failed();}
    }
}
