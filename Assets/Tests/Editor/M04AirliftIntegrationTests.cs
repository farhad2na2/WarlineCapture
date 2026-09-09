using System;
using System.IO;
using System.Linq;
using Game.Components;
using Game.Missions.Contracts;
using Game.Runtime;
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public sealed class M04AirliftIntegrationTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            var tests=new M04AirliftIntegrationTests();
            tests.BoardedDisabledPassengersSurviveProjectionAndRequireRealDeparture();
            tests.HostilesResetClearanceAndCarrierLossOnlyFailsBeforeTransfer();
            tests.MissingMemberAndUninitializedHealthFailDifferently();
            tests.NamedUnlocksSettleOnceAndReplayImprovesStars();
            tests.EdgeFocusSettlesInsideTheViewportBoundary();
            tests.OrdinaryHelicopterMovePublishesAirborneState();
            tests.FailedSavePreservesProfileAndSameAttemptCanRetry();
            tests.CleanupRemovesBoardedMissionPassengersAndPreservesUnrelatedUnits();
            Debug.Log("[M04AirliftIntegration] result=Passed tests=8"); ValidationExit.Passed();
        }
        catch(Exception e){Debug.LogException(e);Debug.LogError("[M04AirliftIntegration] result=Failed");ValidationExit.Failed();}
    }

    private sealed class Roster : IDisposable
    {
        public readonly World World=new("M04 extraction ownership test");
        public EntityManager Em=>World.EntityManager;
        public Entity Root,Escort,Hostile;public Entity[] People=new Entity[4];
        public CampaignMissionExtractionState State;
        public CampaignMissionAttemptFactsComponent Facts;
        public CampaignMissionRuntimeComponent Runtime=new(){Phase=MissionPhaseKind.Engage,
            MissionId="saga.ch01.m04.airlift",ScenarioId="scenario.ch01.m04.airlift",OperationMapId="opmap.ch01.airlift_01",
            SessionToken="m04-integration",SourceVersion=1,Version=1,DeterministicSeed=4004};
        public CampaignMissionExtractionDefinitionBlob Config=new(){Enabled=1,RequiredPassengers=4,LandingRadius=18,DepartureRadius=14,SecureHoldMilliseconds=20000};
        public Roster()
        {
            Root=Em.CreateEntity();Em.AddBuffer<CampaignMissionExtractionMember>(Root);
            Escort=Add(0);State.Carrier=Add(2);State.Aircraft=Add(3);Hostile=Add(4);
            Em.SetComponentData(Hostile,LocalTransform.FromPosition(new float3(100,0,100)));
            Em.AddComponentData(State.Aircraft,new UnitAirComponent());
            State.DepartureCenter=new float3(60,0,0);
            for(int i=0;i<4;i++)People[i]=Add(1);
            Facts.CommandSquadSpawned=1;
        }
        private Entity Add(byte kind)
        {
            var e=Em.CreateEntity(typeof(UnitHealth),typeof(LocalTransform));
            Em.SetComponentData(e,new UnitHealth{Current=100,Max=100});Em.SetComponentData(e,LocalTransform.Identity);
            Em.GetBuffer<CampaignMissionExtractionMember>(Root).Add(new(){Entity=e,Kind=kind});return e;
        }
        public void Board(Entity vehicle)
        {
            foreach(var person in People)
            {
                if(!Em.HasComponent<UnitTransportPassenger>(person))Em.AddComponent<UnitTransportPassenger>(person);
                Em.SetComponentData(person,new UnitTransportPassenger{Transport=vehicle});
                if(!Em.HasComponent<Disabled>(person))Em.AddComponent<Disabled>(person);
            }
        }
        public void Project(float seconds=0)=>CampaignMissionRuntimeSystem.ProjectExtractionRoster(Em,Root,ref State,ref Config,in Runtime,ref Facts,seconds);
        public void Dispose()=>World.Dispose();
    }
    [Test] public void BoardedDisabledPassengersSurviveProjectionAndRequireRealDeparture()
    {
        using var r=new Roster();r.Project();Assert.AreEqual(1,r.State.Ready);
        r.Board(r.State.Aircraft);r.Project(30);Assert.AreEqual(0,r.State.DepartureCleared,"Helicopter pickup cannot skip the APC leg.");
        r.Board(r.State.Carrier);r.Project();Assert.AreEqual(4,r.Facts.ExtractionCarrierLegCount);Assert.AreEqual(4,r.Facts.ExtractionPassengersAboard);
        r.Board(r.State.Aircraft);r.Project(19);Assert.AreEqual(0,r.State.DepartureCleared);
        r.Project(1);Assert.AreEqual(1,r.State.DepartureCleared);Assert.AreEqual(0,r.Facts.CivilianLossCount);
        r.Em.SetComponentData(r.State.Aircraft,LocalTransform.FromPosition(r.State.DepartureCenter));r.Project();
        Assert.AreEqual(0,r.Facts.ExtractionPassengersDelivered,"Driving a grounded aircraft into the exit is insufficient.");
        r.Em.SetComponentData(r.State.Aircraft,new UnitAirComponent{Airborne=1});r.Project();
        Assert.AreEqual(4,r.Facts.ExtractionPassengersDelivered);Assert.IsTrue(CampaignMissionExtractionRuleUtility.IsVictory(r.Facts,4));
    }
    [Test] public void HostilesResetClearanceAndCarrierLossOnlyFailsBeforeTransfer()
    {
        using var r=new Roster();r.Board(r.State.Carrier);r.Project();r.Board(r.State.Aircraft);r.Project(12);
        r.Em.SetComponentData(r.Hostile,LocalTransform.Identity);r.Project(8);Assert.AreEqual(0,r.State.SecureHoldMilliseconds);
        r.Em.AddComponent<CampaignMissionCombatSuppressedTag>(r.Hostile);r.Project(20);Assert.AreEqual(1,r.State.DepartureCleared);
        r.Em.SetComponentData(r.State.Carrier,new UnitHealth{Max=100,Current=0});r.Project();Assert.AreEqual(0,r.Facts.ExtractionCarrierLost);
        r.Em.RemoveComponent<UnitTransportPassenger>(r.People[0]);r.Project();
        Assert.AreEqual(0,r.Facts.ExtractionCarrierLost,"A later disembark cannot retroactively undo the completed APC transfer.");
        using var early=new Roster();early.Project();early.Em.SetComponentData(early.State.Carrier,new UnitHealth{Max=100,Current=0});early.Project();
        Assert.AreEqual(1,early.Facts.ExtractionCarrierLost);Assert.IsTrue(CampaignMissionExtractionRuleUtility.IsFailure(early.Facts));
    }
    [Test] public void MissingMemberAndUninitializedHealthFailDifferently()
    {
        using var r=new Roster();r.Em.SetComponentData(r.People[0],default(UnitHealth));r.Project();
        Assert.AreEqual(0,r.State.Ready);Assert.AreEqual(0,r.Facts.CivilianLossCount,"Prefab health initialization is not a death.");
        r.Em.SetComponentData(r.People[0],new UnitHealth{Current=100,Max=100});r.Project();
        r.Em.SetComponentData(r.People[0],new UnitHealth{Current=0,Max=100});r.Project();Assert.AreEqual(1,r.Facts.CivilianLossCount);
        using var absent=new Roster();absent.Em.DestroyEntity(absent.People[0]);absent.Project();
        Assert.AreEqual(1,absent.Facts.HostileRosterIntegrityFault);
        Assert.IsTrue(CampaignMissionExtractionRuleUtility.TryAdvance(absent.Runtime,absent.Facts,false,true,4,out var result));
        Assert.AreEqual(MissionOutcomeKind.Defeat,result.Outcome);
        using var duplicate=new Roster();var members=duplicate.Em.GetBuffer<CampaignMissionExtractionMember>(duplicate.Root);
        for(int i=0;i<members.Length;i++)if(members[i].Entity==duplicate.People[1])
        {var member=members[i];member.Entity=duplicate.People[0];members[i]=member;break;}
        duplicate.Project();Assert.AreEqual(1,duplicate.Facts.HostileRosterIntegrityFault,"Repeated references cannot count one person twice.");
    }
    [Test] public void NamedUnlocksSettleOnceAndReplayImprovesStars()
    {
        const string mission="saga.ch01.m04.airlift";string path=Path.Combine(Path.GetTempPath(),"warline-m04-settle-"+Guid.NewGuid().ToString("N"));
        try
        {
            var save=new SaveService(new JsonSaveRepository(path));save.SaveProfile(new PlayerProfileSaveData{credits=100,ownedUnitUnlocks=new[]{"Unit_Veh_APC_Fast"}});
            var store=new CampaignMissionProgressStore(save);
            var rewards=new[]{new CampaignMissionRewardGrant(MissionRewardKind.None,"reward.commander_xp",500),new CampaignMissionRewardGrant(MissionRewardKind.Credits,"",2500),
                new CampaignMissionRewardGrant(MissionRewardKind.None,"reward.ch01.m04.laila_unlock",1),new CampaignMissionRewardGrant(MissionRewardKind.None,"reward.ch01.m04.transport_unlock",1)};
            Assert.Throws<ArgumentException>(()=>store.SettleWithRewards("saga.ch01.m03.radar_warning","foreign",1,true,2,400000,null,rewards));
            Assert.IsTrue(store.SettleWithRewards(mission,"first",1,true,2,500000,null,rewards).Applied);
            Assert.IsTrue(store.SettleWithRewards(mission,"first",1,true,2,500000,null,rewards).IsDuplicate);
            Assert.IsTrue(store.SettleWithRewards(mission,"other-first",2,true,3,300000,null,rewards).IsDuplicate);
            Assert.Throws<ArgumentException>(()=>store.SettleWithRewards(mission,"replay",2,false,3,300000,null,rewards));
            Assert.IsTrue(store.SettleWithRewards(mission,"replay",2,false,3,300000,null,new[]{new CampaignMissionRewardGrant(MissionRewardKind.Credits,"",300)}).Applied);
            var profile=save.LoadProfile();Assert.AreEqual(2900,profile.credits);Assert.AreEqual(500,profile.commanderXp);
            foreach(string id in new[]{"Unit_Chr_Pilot_Female_01","Unit_Veh_APC_Fast","Unit_Veh_Helicopter_Transport"})Assert.AreEqual(1,profile.ownedUnitUnlocks.Count(x=>x==id));
            var progress=new CampaignMissionProgressStore(save).ReadAll().Single(x=>x.missionId==mission);
            Assert.AreEqual(3,progress.bestStars);Assert.AreEqual(300000,progress.bestCompletionMilliseconds);Assert.AreEqual(1,progress.successfulReplayCount);
        }
        finally{if(Directory.Exists(path))Directory.Delete(path,true);}
    }
    [Test] public void EdgeFocusSettlesInsideTheViewportBoundary()
    {
        using var world=new World("M04 camera edge");var system=world.GetOrCreateSystemManaged<RtsCameraSystem>();
        var go=new GameObject("M04 edge camera");
        try
        {
            var camera=go.AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=10;camera.aspect=1;
            camera.transform.SetPositionAndRotation(new Vector3(90,20,90),Quaternion.Euler(90,0,0));system.SetGroundBoundary(new Rect(0,0,100,100));
            system.SetSmoothFocusTarget(new Vector3(100,0,100),true);
            var center=system.UpdateSmoothFocus(system.GetCameraGroundCenterWorld(camera),.5f,camera);
            Assert.IsFalse(system.HasSmoothFocusTarget,"A target at the map edge must settle at the nearest reachable viewport center.");
            Assert.That(center.x,Is.EqualTo(90).Within(.01f));Assert.That(center.z,Is.EqualTo(90).Within(.01f));
        }
        finally{UnityEngine.Object.DestroyImmediate(go);}
    }
    [Test] public void OrdinaryHelicopterMovePublishesAirborneState()
    {
        using var world=new World("M04 helicopter flight state");var em=world.EntityManager;
        var grid=em.CreateEntity();em.AddComponentData(grid,new GridConfig{Width=64,Height=64,CellSize=1});
        var helicopter=em.CreateEntity(typeof(LocalTransform),typeof(UnitGrid),typeof(UnitMove),typeof(UnitAirMovement),typeof(UnitAirComponent));
        em.SetComponentData(helicopter,LocalTransform.FromPosition(new float3(5.5f,0,5.5f)));
        em.SetComponentData(helicopter,new UnitGrid{Cell=new int2(5,5)});
        em.SetComponentData(helicopter,new UnitMove{Speed=12,WalkSpeed=12,ArriveDistance=.1f,RoadSpeedMultiplier=1});
        em.SetComponentData(helicopter,new UnitAirMovement{CruiseHeight=12,RunwayTaxiSpeed=5});
        var movement=world.CreateSystem<UnitAirMovementSystem>();
        world.SetTime(new Unity.Core.TimeData(0,.25f));movement.Update(world.Unmanaged);
        Assert.AreEqual(0,em.GetComponentData<UnitAirComponent>(helicopter).Airborne);
        new UnitMoveOrderSystem().IssueGroupedManualMoveOrder(em,helicopter,new int2(40,40),true,false,1,1);
        for(int i=1;i<=8;i++){world.SetTime(new Unity.Core.TimeData(i*.25,.25f));movement.Update(world.Unmanaged);}
        Assert.That(em.GetComponentData<LocalTransform>(helicopter).Position.y,Is.GreaterThan(TransportBoardingData.AirBoardingGroundedHeightTolerance));
        Assert.AreEqual(1,em.GetComponentData<UnitAirComponent>(helicopter).Airborne,"Ordinary VTOL flight must publish the same state used by transport and extraction.");
    }
    [Test] public void FailedSavePreservesProfileAndSameAttemptCanRetry()
    {
        string path=Path.Combine(Path.GetTempPath(),"warline-m04-save-failure-"+Guid.NewGuid().ToString("N"));
        try
        {
            var save=new SaveService(new JsonSaveRepository(path));save.SaveProfile(new PlayerProfileSaveData{credits=100});
            var store=new CampaignMissionProgressStore(save);string profile=Path.Combine(path,SaveService.ProfileFileName);
            string before=File.ReadAllText(profile);Directory.CreateDirectory(profile+".tmp");
            var rewards=new[]{new CampaignMissionRewardGrant(MissionRewardKind.Credits,"",2500),new CampaignMissionRewardGrant(MissionRewardKind.None,"reward.ch01.m04.laila_unlock",1)};
            void Settle()=>store.SettleWithRewards("saga.ch01.m04.airlift","same-attempt",1,true,3,90000,null,rewards);
            var error=Assert.Catch<Exception>(Settle);Assert.IsTrue(error is IOException or UnauthorizedAccessException);
            Assert.AreEqual(before,File.ReadAllText(profile),"Failed commit must preserve both rewards and the receipt atomically.");
            Directory.Delete(profile+".tmp");Settle();Settle();
            Assert.AreEqual(2600,save.LoadProfile().credits);Assert.AreEqual(1,save.LoadProfile().ownedUnitUnlocks.Count(x=>x=="Unit_Chr_Pilot_Female_01"));
            Assert.IsTrue(new CampaignMissionProgressStore(save).ReadAll().Single(x=>x.missionId=="saga.ch01.m04.airlift").firstClearRewardSettled);
        }
        finally{if(Directory.Exists(path))Directory.Delete(path,true);}
    }
    [Test] public void CleanupRemovesBoardedMissionPassengersAndPreservesUnrelatedUnits()
    {
        using var r=new Roster();r.Board(r.State.Carrier);r.Project();
        using(var members=r.Em.GetBuffer<CampaignMissionExtractionMember>(r.Root).ToNativeArray(Unity.Collections.Allocator.Temp))
            foreach(var member in members)r.Em.AddComponent<CampaignMissionUnitRoleComponent>(member.Entity);
        r.Em.AddComponentData(r.Root,r.State);r.Em.AddComponentData(r.Root,new CampaignMissionCameraTourState{Captured=1});
        var unrelated=r.Em.CreateEntity(typeof(Disabled),typeof(UnitTransportPassenger));
        for(int i=0;i<2;i++)
        {
            using var commands=new EntityCommandBuffer(Unity.Collections.Allocator.Temp);var mutable=commands;
            CampaignMissionLaunchSystem.QueueAttemptCleanup(r.Em,ref mutable,r.Root);mutable.Playback(r.Em);
        }
        foreach(var person in r.People)Assert.IsFalse(r.Em.Exists(person));Assert.IsFalse(r.Em.Exists(r.State.Carrier));
        Assert.IsTrue(r.Em.Exists(unrelated));Assert.AreEqual(0,r.Em.GetBuffer<CampaignMissionExtractionMember>(r.Root).Length);
        Assert.AreEqual(default(CampaignMissionExtractionState),r.Em.GetComponentData<CampaignMissionExtractionState>(r.Root));
        Assert.AreEqual(default(CampaignMissionCameraTourState),r.Em.GetComponentData<CampaignMissionCameraTourState>(r.Root));
    }
}
