using System;
using System.Collections.Generic;
using Game.Components;
using Game.Runtime;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public sealed class M03AttemptCleanupTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            var tests=new M03AttemptCleanupTests();
            tests.RemovesOnlyThisAttemptsBuildingsAndProducedUnits();
            tests.StaleDefenseOwnershipCannotRemoveBuildings();
            tests.ImmediateCleanupAndNormalDemolitionHaveSeparateCallbacks();
            tests.AuthoredMapIdsDoNotRaiseRuntimeOwnershipBaseline();
            tests.ExitClearsDefenseMembershipRequestsAndPresentationState();
            Debug.Log("[M03AttemptCleanupValidation] result=Passed tests=5"); ValidationExit.Passed();
        }
        catch(Exception error) {Debug.LogException(error); Debug.LogError("[M03AttemptCleanupValidation] result=Failed"); ValidationExit.Failed();}
    }
    [Test] public void RemovesOnlyThisAttemptsBuildingsAndProducedUnits()
    {
        using var f=new Fixture();
        Entity old=f.Unit(50,1),produced=f.Unit(101,1),foreign=f.Unit(104,2);
        var units=f.Em.GetBuffer<BuildingProducedUnitReadModel>(f.Boundary);
        units.Add(units[1]); // A duplicate read-model row must not double-destroy an entity.
        f.Building(50,1); Entity loan=f.Building(101,1),tower=f.Building(102,1),map=f.Building(103,1,true),enemy=f.Building(104,2);
        Entity initial=f.Em.CreateEntity(typeof(CampaignMissionUnitRoleComponent));
        f.Cleanup();
        var deletes=f.Em.GetBuffer<BuildingRuntimeDeleteRequest>(f.Boundary);
        var ids=new List<int>(); foreach(var request in deletes) {ids.Add(request.BuildingRuntimeId); Assert.AreEqual(1,request.ImmediateCleanup);}
        CollectionAssert.AreEquivalent(new[]{101,102},ids);
        Assert.IsFalse(f.Em.Exists(produced)); Assert.IsFalse(f.Em.Exists(initial));
        Assert.IsTrue(f.Em.Exists(old)); Assert.IsTrue(f.Em.Exists(foreign)); Assert.IsTrue(f.Em.Exists(map)); Assert.IsTrue(f.Em.Exists(enemy));
        Assert.IsTrue(f.Em.Exists(loan)); Assert.IsTrue(f.Em.Exists(tower),"The building owner must remove the full runtime object after consuming its request.");
        f.Cleanup(); Assert.AreEqual(2,f.Em.GetBuffer<BuildingRuntimeDeleteRequest>(f.Boundary).Length);
    }
    [Test] public void StaleDefenseOwnershipCannotRemoveBuildings()
    {
        using var f=new Fixture(); f.Unit(50,1); Entity produced=f.Unit(101,1); f.Building(101,1);
        var defense=f.Em.GetComponentData<CampaignMissionDefenseStateComponent>(f.Root); defense.AttemptOrdinal++; f.Em.SetComponentData(f.Root,defense);
        f.Cleanup(); Assert.AreEqual(0,f.Em.GetBuffer<BuildingRuntimeDeleteRequest>(f.Boundary).Length); Assert.IsTrue(f.Em.Exists(produced));
    }
    [Test] public void ImmediateCleanupAndNormalDemolitionHaveSeparateCallbacks()
    {
        using var world=new World("M3 cleanup commands"); var em=world.EntityManager;
        Entity boundary=em.CreateEntity(); var requests=em.AddBuffer<BuildingRuntimeDeleteRequest>(boundary);
        requests.Add(new BuildingRuntimeDeleteRequest {BuildingRuntimeId=101});
        requests.Add(new BuildingRuntimeDeleteRequest {BuildingRuntimeId=101,ImmediateCleanup=1});
        requests.Add(new BuildingRuntimeDeleteRequest {BuildingRuntimeId=102});
        requests.Add(new BuildingRuntimeDeleteRequest {BuildingRuntimeId=103,ImmediateCleanup=2});
        var clean=new List<int>(); var demolished=new List<int>(); var processor=new BuildingRuntimeDeleteCommandProcessor();
        processor.Process(id=>{demolished.Add(id); return true;},em,boundary,id=>clean.Add(id));
        processor.Process(id=>{demolished.Add(id); return true;},em,boundary,id=>clean.Add(id));
        CollectionAssert.AreEqual(new[]{101},clean); CollectionAssert.AreEqual(new[]{102},demolished);
        Assert.AreEqual(0,em.GetBuffer<BuildingRuntimeDeleteRequest>(boundary).Length);
    }
    [Test] public void AuthoredMapIdsDoNotRaiseRuntimeOwnershipBaseline()
    {
        using var f=new Fixture();
        f.Building(800000,1,true);
        Assert.AreEqual(0,CampaignMissionSpawnSystem.ReadRuntimeBuildingBaseline(f.Em));
        f.Building(1,1);
        Assert.AreEqual(1,CampaignMissionSpawnSystem.ReadRuntimeBuildingBaseline(f.Em));
    }
    [Test] public void ExitClearsDefenseMembershipRequestsAndPresentationState()
    {
        using var f=new Fixture();
        Entity member=f.Em.CreateEntity(typeof(CampaignMissionUnitRoleComponent));
        f.Em.AddBuffer<CampaignMissionDefenseMember>(f.Root).Add(new CampaignMissionDefenseMember {Entity=member});
        f.Em.AddBuffer<CampaignMissionConvoyElementState>(f.Root).Add(new CampaignMissionConvoyElementState {Activated=1});
        f.Em.AddBuffer<RadarPingRequest>(f.Root).Add(new RadarPingRequest {RequestId=4});
        f.Em.AddBuffer<MissionDefenseInteractionRequest>(f.Root).Add(new MissionDefenseInteractionRequest {Kind=MissionDefenseInteractionKind.FocusWarning});
        f.Em.AddBuffer<ThreatWarningRecord>(f.Root).Add(default);
        f.Em.AddBuffer<ThreatWarningObservation>(f.Root).Add(default);
        f.Em.AddComponentData(f.Root,new RadarPingState {Charges=1,PendingRequestId=4,PendingSensor=member});
        f.Em.AddComponentData(f.Root,new ThreatWarningLedgerState {Active=1,Version=8});
        f.Em.AddComponentData(f.Root,new CampaignMissionCameraTourState {Captured=1,SkipRequested=1});
        f.Cleanup(); f.Cleanup();
        Assert.IsFalse(f.Em.Exists(member));
        Assert.AreEqual(0,f.Em.GetBuffer<CampaignMissionDefenseMember>(f.Root).Length);
        Assert.AreEqual(0,f.Em.GetBuffer<CampaignMissionConvoyElementState>(f.Root).Length);
        Assert.AreEqual(0,f.Em.GetBuffer<RadarPingRequest>(f.Root).Length);
        Assert.AreEqual(0,f.Em.GetBuffer<MissionDefenseInteractionRequest>(f.Root).Length);
        Assert.AreEqual(0,f.Em.GetBuffer<ThreatWarningRecord>(f.Root).Length);
        Assert.AreEqual(0,f.Em.GetBuffer<ThreatWarningObservation>(f.Root).Length);
        Assert.AreEqual(default(RadarPingState),f.Em.GetComponentData<RadarPingState>(f.Root));
        Assert.AreEqual(default(ThreatWarningLedgerState),f.Em.GetComponentData<ThreatWarningLedgerState>(f.Root));
        Assert.AreEqual(default(CampaignMissionCameraTourState),f.Em.GetComponentData<CampaignMissionCameraTourState>(f.Root));
        Assert.AreEqual(default(CampaignMissionDefenseStateComponent),f.Em.GetComponentData<CampaignMissionDefenseStateComponent>(f.Root));
    }

    private sealed class Fixture:IDisposable
    {
        private readonly World world=new("M3 attempt cleanup");
        private readonly BlobAssetReference<CampaignMissionCatalogBlob> blob;
        public EntityManager Em=>world.EntityManager;
        public readonly Entity Root,Boundary;
        public Fixture()
        {
            var runtime=M03RadarWarningRuleTests.Runtime();
            using var builder=new BlobBuilder(Allocator.Temp);
            ref var catalog=ref builder.ConstructRoot<CampaignMissionCatalogBlob>();
            var missions=builder.Allocate(ref catalog.Missions,1);
            missions[0].MissionId=runtime.MissionId; missions[0].ScenarioId=runtime.ScenarioId; missions[0].OperationMapId=runtime.OperationMapId;
            missions[0].Defense.Enabled=1;
            blob=builder.CreateBlobAssetReference<CampaignMissionCatalogBlob>(Allocator.Persistent);
            Root=Em.CreateEntity(typeof(CampaignMissionRootComponent),typeof(CampaignMissionRuntimeComponent),typeof(CampaignMissionCatalogComponent),
                typeof(CampaignMissionAttemptFactProjectionStateComponent),typeof(CampaignMissionDefenseStateComponent));
            Em.SetComponentData(Root,runtime); Em.SetComponentData(Root,new CampaignMissionCatalogComponent {Blob=blob,SourceVersion=1});
            Em.SetComponentData(Root,new CampaignMissionDefenseStateComponent {SessionToken=runtime.SessionToken,AttemptOrdinal=runtime.AttemptOrdinal,
                SourceVersion=runtime.SourceVersion,Initialized=1,RuntimeBuildingBaselineId=100,ProducedUnitBaselineCount=1});
            Boundary=Em.CreateEntity(typeof(BuildingRuntimeStateTag));
            Em.AddBuffer<BuildingRuntimeDeleteRequest>(Boundary); Em.AddBuffer<BuildingProducedUnitReadModel>(Boundary);
        }
        public Entity Building(int id,byte owner,bool map=false)
        {
            Entity entity=Em.CreateEntity(typeof(RuntimeBuildingCombatInfo)); Em.SetComponentData(entity,new RuntimeBuildingCombatInfo {RuntimeBuildingId=id,OwnerFactionId=owner});
            if(map) Em.AddComponent<OperationMapBuildingComponent>(entity); return entity;
        }
        public Entity Unit(int producer,byte owner)
        {
            Entity entity=Em.CreateEntity(); Em.GetBuffer<BuildingProducedUnitReadModel>(Boundary).Add(new BuildingProducedUnitReadModel
                {Unit=entity,BuildingRuntimeId=producer,HasOwnerFaction=1,OwnerFactionId=owner}); return entity;
        }
        public void Cleanup() {using var cleanup=new EntityCommandBuffer(Allocator.Temp); var commands=cleanup; CampaignMissionLaunchSystem.QueueAttemptCleanup(Em,ref commands,Root); commands.Playback(Em);}
        public void Dispose() {world.Dispose(); blob.Dispose();}
    }
}
