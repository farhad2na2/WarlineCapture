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
        var root=em.CreateEntity(typeof(CampaignMissionExtractionState),typeof(CampaignMissionRuntimeComponent));
        em.SetComponentData(root,new CampaignMissionRuntimeComponent {MissionId="saga.ch01.m04.airlift"});
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
    public void ReturningFromAirliftDoesNotRedirectOtherMissionSelection()
    {
        using var world=new World("Post-airlift tutorial selection");var em=world.EntityManager;
        var root=em.CreateEntity(typeof(CampaignMissionExtractionState),typeof(CampaignMissionRuntimeComponent));
        var currentActor=Actor(em);
        var oldCarrier=Actor(em);var oldAircraft=Actor(em);
        foreach(var mission in new[]{"saga.ch01.m01.first_contact","saga.ch01.m02.establish_base","saga.ch01.m03.radar_warning","saga.ch01.m05.breach_assault"})
        {
            em.SetComponentData(root,new CampaignMissionRuntimeComponent {MissionId=mission,SessionToken="current-attempt"});
            foreach(bool retainedActors in new[]{false,true})
            {
                em.SetComponentData(root,retainedActors ? new CampaignMissionExtractionState {Carrier=oldCarrier,Aircraft=oldAircraft} : default);
                Assert.AreEqual(currentActor,Resolve(em,root,new CampaignMissionGuidanceProjectionComponent {SourceEntity=currentActor},float3.zero),mission);
            }
        }
    }

    [Test]
    public void TravelCueWaitsForTheLastSelectedSoldier()
    {
        using var world = new World("Formation arrival guidance"); var em = world.EntityManager;
        Entity arrived = Actor(em), moving = Actor(em), oldAttempt = Actor(em);
        foreach (var unit in new[] { arrived, moving, oldAttempt })
        {
            em.AddComponent<SelectedUnitTag>(unit);
            em.AddComponentData(unit, new CampaignMissionUnitRoleComponent
            { SessionToken = unit == oldAttempt ? "old" : "current" });
        }
        em.AddComponent<UnitPathFollow>(moving); em.AddComponent<UnitPathFollow>(oldAttempt);
        Assert.IsTrue(GroupMoving(em, "current"));
        em.RemoveComponent<UnitPathFollow>(moving);
        Assert.IsFalse(GroupMoving(em, "current"), "Ignore an older attempt's moving actor.");
        em.AddComponent<UnitPathFollow>(moving); em.RemoveComponent<SelectedUnitTag>(moving);
        Assert.IsFalse(GroupMoving(em, "current"), "Unselected actors do not block the instructed group.");
    }

    private static bool GroupMoving(EntityManager em, Unity.Collections.FixedString64Bytes session) =>
        (bool)typeof(UiShellEcsGateway).GetMethod("IsSelectedTutorialGroupMoving",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{em,session});

    [Test]
    public void AcceptedAttackWaitsForItsActualTarget()
    {
        using var world = new World("Attack feedback"); var em = world.EntityManager;
        var actor = Actor(em); var target = Actor(em); var other = Actor(em);
        bool Busy() => (bool)typeof(UiShellEcsGateway).GetMethod("IsTutorialAttackInProgress",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
            .Invoke(null, new object[] { em, actor, target });
        em.AddComponentData(actor, new EngageTarget {Target=target,IsCommanded=1});
        Assert.IsTrue(Busy(), "One accepted attack must not demand the same click again.");
        em.SetComponentData(actor, new EngageTarget {Target=other,IsCommanded=1});
        Assert.IsFalse(Busy(), "A wrong target must still offer corrective guidance.");
        em.SetComponentData(actor, new EngageTarget {Target=target,IsCommanded=0});
        Assert.IsFalse(Busy(), "Auto-engagement is not the requested command.");
        em.SetComponentData(actor, new EngageTarget {Target=target,IsCommanded=1});
        em.SetComponentData(target, new UnitHealth {Current=0}); Assert.IsFalse(Busy());
        em.DestroyEntity(target); Assert.IsFalse(Busy());
    }

    public static void ValidateAttackCue()
    {
        new MissionGroupSelectionTests().AcceptedAttackWaitsForItsActualTarget();
        Debug.Log("[TutorialAttackFeedback] result=Passed accepted, wrong, automatic, dead and removed targets");
    }

    public static void ValidateTravelCue()
    {
        new MissionGroupSelectionTests().TravelCueWaitsForTheLastSelectedSoldier();
        Debug.Log("[TutorialFormationTravel] result=Passed last-selected-arrival and attempt isolation");
    }

    public static void ValidateRouting()
    {
        var tests=new MissionGroupSelectionTests();
        tests.RescueSelectionResolvesSpecialistsInsteadOfOverlappingTransports();
        tests.ReturningFromAirliftDoesNotRedirectOtherMissionSelection();
        tests.DefendersSpanBothSquadsButExcludeOtherRolesAndAttempts();
        Debug.Log("[MissionGroupSelectionRouting] result=Passed rescue, cross-mission return, role and attempt isolation");
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
            ValidateRouting();new MissionGroupSelectionTests().SelectionCopyExistsInBothLanguageConfigs();
            MatchHudAssistantUiSystemHelperTests.RunShowMeValidation();
            new HudRightColumnLayoutValidation().MinimapDockAndContentHeightFollowActualControlsAndCopy();
            Debug.Log("[MissionGroupSelection] result=Passed M1-M5 HUD transitions, group routing, bilingual copy, layout");
            ValidationExit.Passed();
        }
        catch(Exception error){Debug.LogException(error);Debug.LogError("[MissionGroupSelection] result=Failed");ValidationExit.Failed();}
    }
}
