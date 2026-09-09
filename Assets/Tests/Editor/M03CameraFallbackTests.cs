using System;
using Game.Components;
using Game.Missions.Contracts;
using Game.Runtime;
using NUnit.Framework;
using Unity.Collections;
using Unity.Core;
using Unity.Entities;
using UnityEngine;

public sealed class M03CameraFallbackTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            var tests=new M03CameraFallbackTests();
            tests.MissingCameraFinishesOnlyTheOptionalOpening();
            tests.PausedOpeningDoesNotSpendItsWatchdog();
            tests.FinaleWithoutCameraIsBoundedAndDefeatDoesNotCelebrate();
            Debug.Log("[M03CameraFallbackValidation] result=Passed tests=3"); ValidationExit.Passed();
        }
        catch(Exception exception)
        {Debug.LogException(exception); Debug.LogError("[M03CameraFallbackValidation] result=Failed"); ValidationExit.Failed();}
    }
    [Test] public void MissingCameraFinishesOnlyTheOptionalOpening()
    {
        using var f=new Fixture();
        for(int i=0;i<29;i++) f.Tick();
        Assert.AreEqual(0,f.Em.GetComponentData<CampaignMissionOpeningPresentationComponent>(f.Root).Stage);
        f.Tick();
        Assert.AreEqual(6,f.Em.GetComponentData<CampaignMissionOpeningPresentationComponent>(f.Root).Stage);
        var facts=f.Em.GetComponentData<CampaignMissionAttemptFactsComponent>(f.Root);
        Assert.AreEqual(0,facts.ElapsedMilliseconds);
        Assert.AreEqual(0,facts.ForwardPostBound,"A media fallback must not invent world readiness.");
        Assert.AreEqual(MissionOutcomeKind.None,f.Em.GetComponentData<CampaignMissionRuntimeComponent>(f.Root).Outcome);
    }
    [Test] public void PausedOpeningDoesNotSpendItsWatchdog()
    {
        using var f=new Fixture();
        RuntimeGameplayStateTestHelper.SetSimulationActive(f.Em,false);
        for(int i=0;i<40;i++) f.Tick();
        Assert.AreEqual(0,f.Em.GetComponentData<CampaignMissionCameraTourState>(f.Root).OpeningWatchdogMilliseconds);
    }
    [Test] public void FinaleWithoutCameraIsBoundedAndDefeatDoesNotCelebrate()
    {
        using var f=new Fixture();
        var runtime=f.Em.GetComponentData<CampaignMissionRuntimeComponent>(f.Root);
        runtime.Phase=MissionPhaseKind.SecureCorridor;
        f.Em.SetComponentData(f.Root,runtime);
        f.Em.AddComponentData(f.Root,new CampaignMissionFinalePresentationComponent {Required=1,SessionToken=runtime.SessionToken});
        f.Tick(); f.Tick();
        Assert.AreEqual(0,f.Em.GetComponentData<CampaignMissionAttemptFactsComponent>(f.Root).FinalePresentationComplete);
        f.Tick();
        Assert.AreEqual(1,f.Em.GetComponentData<CampaignMissionAttemptFactsComponent>(f.Root).FinalePresentationComplete);
        runtime.Outcome=MissionOutcomeKind.Defeat; f.Em.SetComponentData(f.Root,runtime);
        f.Em.SetComponentData(f.Root,new CampaignMissionFinalePresentationComponent {Required=1,SessionToken=runtime.SessionToken});
        f.Tick();
        Assert.AreEqual(0,f.Em.GetComponentData<CampaignMissionFinalePresentationComponent>(f.Root).Stage);
    }
    private sealed class Fixture:IDisposable
    {
        private readonly World world=new("M3 camera failure fixture");
        public EntityManager Em=>world.EntityManager;
        public readonly Entity Root;
        private readonly SystemHandle system;
        private BlobAssetReference<CampaignMissionCatalogBlob> catalog;
        private BlobAssetReference<OperationMapBlob> map;
        private int elapsed;
        public Fixture()
        {
            var runtime=M03RadarWarningRuleTests.Runtime(); runtime.Phase=MissionPhaseKind.FindSquad;
            Root=Em.CreateEntity(typeof(CampaignMissionRuntimeComponent),typeof(CampaignMissionAttemptFactsComponent),
                typeof(CampaignMissionOpeningPresentationComponent),typeof(CampaignMissionCameraTourState));
            Em.SetComponentData(Root,runtime);
            Em.SetComponentData(Root,new CampaignMissionAttemptFactsComponent {CommandSquadSpawned=1});
            Em.SetComponentData(Root,new CampaignMissionOpeningPresentationComponent {SessionToken=runtime.SessionToken});
            Em.SetComponentData(Root,new CampaignMissionCameraTourState {SessionToken=runtime.SessionToken,AttemptOrdinal=1,SourceVersion=1});
            using(var builder=new BlobBuilder(Allocator.Temp))
            {
                ref var data=ref builder.ConstructRoot<CampaignMissionCatalogBlob>();
                var missions=builder.Allocate(ref data.Missions,1);
                missions[0].MissionId=runtime.MissionId; missions[0].ScenarioId=runtime.ScenarioId;
                missions[0].OperationMapId=runtime.OperationMapId; missions[0].MissionRuntimeEnabled=1; missions[0].Defense.Enabled=1;
                catalog=builder.CreateBlobAssetReference<CampaignMissionCatalogBlob>(Allocator.Persistent);
            }
            using(var builder=new BlobBuilder(Allocator.Temp))
            {builder.ConstructRoot<OperationMapBlob>(); map=builder.CreateBlobAssetReference<OperationMapBlob>(Allocator.Persistent);}
            Em.AddComponentData(Root,new CampaignMissionCatalogComponent {Blob=catalog});
            Em.AddComponentData(Root,new OperationMapMetadataComponent {Blob=map});
            RuntimeGameplayStateTestHelper.SetPlayRequested(Em,true);
            using(var cameras=Em.CreateEntityQuery(typeof(RuntimeCameraFocusRequestComponent))) Em.RemoveComponent<RuntimeCameraFocusRequestComponent>(cameras);
            system=world.GetOrCreateSystem<CampaignMissionPatrolOrderSystem>();
        }
        public void Tick() {world.SetTime(new TimeData(++elapsed,1)); system.Update(world.Unmanaged);}
        public void Dispose() {world.Dispose(); if(catalog.IsCreated) catalog.Dispose(); if(map.IsCreated) map.Dispose();}
    }
}
