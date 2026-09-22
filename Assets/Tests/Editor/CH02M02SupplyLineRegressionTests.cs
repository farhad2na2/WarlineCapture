using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Game.Components;
using Game.Composition;
using Game.Configs;
using Game.Editor;
using Game.Missions.Contracts;
using Game.Runtime;
using NUnit.Framework;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

public sealed class CH02M02SupplyLineRegressionTests
{
    [Test] public void AuthoredObjectivesPublishAndRejectInvalidChain()
    {
        using var world=new World("Supply Line objective regression");var em=world.EntityManager;
        var mission=AssetDatabase.LoadAssetAtPath<MissionDefinitionConfig>(CH02M02SupplyLineConfigBuilder.MissionPath);
        var scenario=AssetDatabase.LoadAssetAtPath<ScenarioSetupConfig>(CH02M02SupplyLineConfigBuilder.ScenarioPath);
        var maps=AssetDatabase.LoadAssetAtPath<OperationMapCatalogConfig>(M01FirstContactConfigBuilder.OperationMapCatalogPath);
        Assert.IsTrue(CampaignMissionCatalogProjection.TryProject(em,mission,scenario,maps,1,out var root,out var error),error);
        var catalog=em.GetComponentData<CampaignMissionCatalogComponent>(root);
        try
        {
            ref var d=ref catalog.Blob.Value.Missions[0];
            var runtime=new CampaignMissionRuntimeComponent {MissionId=d.MissionId,ScenarioId=d.ScenarioId,OperationMapId=d.OperationMapId,SessionToken="supply-test",Version=1,SourceVersion=1,AttemptOrdinal=1};
            var facts=new CampaignMissionAttemptFactsComponent {CommandSquadSpawned=1,CommandSquadAlive=1,HostileTotalCount=6};
            Assert.IsTrue(CampaignMissionObjectiveProjectionSystem.IsPublishable(runtime,facts,ref d));
            Assert.AreEqual(3,d.Objectives.Length);Assert.AreEqual(40,d.SupplyLine.ReserveBarrels);
            Assert.IsFalse(d.SupplyLine.AlternateLaneAnchorId.IsEmpty);
            d.SupplyLine.Enabled=0;
            Assert.IsFalse(CampaignMissionObjectiveProjectionSystem.IsPublishable(runtime,facts,ref d));
        }
        finally{catalog.Blob.Dispose();}
    }
    [Test] public void FirstClearAndReplaySettleOnlyOnceAcrossReload()
    {
        string path=Path.Combine(Path.GetTempPath(),"supply-settlement-"+Guid.NewGuid().ToString("N"));
        try
        {
            const string id=CampaignMissionSequence.SupplyLine;
            var service=new SaveService(new JsonSaveRepository(path));service.SaveProfile(new PlayerProfileSaveData {credits=100,commanderXp=50});
            var store=new CampaignMissionProgressStore(service);
            var first=new[]{new CampaignMissionRewardGrant(MissionRewardKind.None,"reward.commander_xp",800),new CampaignMissionRewardGrant(MissionRewardKind.Credits,"",4000)};
            var replay=new[]{new CampaignMissionRewardGrant(MissionRewardKind.Credits,"",300)};
            Assert.IsFalse(store.SettleWithRewards(id,"early-replay",1,false,3,270000,null,replay).Applied);
            Assert.IsTrue(store.SettleWithRewards(id,"first",1,true,3,270000,CampaignMissionSequence.Next(id),first).Applied);
            store=new CampaignMissionProgressStore(new SaveService(new JsonSaveRepository(path)));
            Assert.IsTrue(store.SettleWithRewards(id,"first",1,true,3,270000,CampaignMissionSequence.Next(id),first).IsDuplicate);
            Assert.IsTrue(store.SettleWithRewards(id,"replay",2,false,2,300000,null,replay).Applied);
            Assert.IsTrue(store.SettleWithRewards(id,"replay",2,false,2,300000,null,replay).IsDuplicate);
            Assert.AreEqual(4400,service.LoadProfile().credits);Assert.AreEqual(850,service.LoadProfile().commanderXp);
            var entry=store.ReadAll().Single(x=>x.missionId==id);Assert.AreEqual(1,entry.successfulReplayCount);Assert.AreEqual(3,entry.bestStars);
        }
        finally{if(Directory.Exists(path))Directory.Delete(path,true);}
    }
    [Test] public void MovingVehiclesCannotSpendCivilianFuel()
    {
        using var world=new World("Supply Line protected vehicle fuel");var em=world.EntityManager;
        var storage=em.CreateEntity(typeof(BuildingResourceStorageComponent));
        em.SetComponentData(storage,new BuildingResourceStorageComponent {RuntimeBuildingId=7,OwnerFactionId=1,FuelStorageCapacity=100,StoredFuelBarrels=22,CivilianFuelReserveBarrels=20});
        var unit=em.CreateEntity(typeof(UnitGrid),typeof(Faction),typeof(UnitMovementBehavior),typeof(UnitFuelConsumption),typeof(UnitFuelConsumptionState));
        em.SetComponentData(unit,new Faction {Id=1});em.SetComponentData(unit,new UnitMovementBehavior {UsesVehicleMotion=1});em.SetComponentData(unit,new UnitFuelConsumption {Enabled=1,GroundFuelPerCell=1});
        var system=world.CreateSystem<VehicleFuelConsumptionSystem>();system.Update(world.Unmanaged);
        em.SetComponentData(unit,new UnitGrid {Cell=new Unity.Mathematics.int2(10,0)});system.Update(world.Unmanaged);
        Assert.AreEqual(20,em.GetComponentData<BuildingResourceStorageComponent>(storage).StoredFuelBarrels);
        em.SetComponentData(unit,new UnitGrid {Cell=new Unity.Mathematics.int2(20,0)});system.Update(world.Unmanaged);
        Assert.AreEqual(20,em.GetComponentData<BuildingResourceStorageComponent>(storage).StoredFuelBarrels);
    }
    [Test] public void RuntimeLossesAndPauseCannotSettleVictory()
    {
        foreach(var failure in new[]{SupplyLineFailure.SquadLost,SupplyLineFailure.HaulerLost,SupplyLineFailure.LinkLost,SupplyLineFailure.Deadline,SupplyLineFailure.Integrity})
        {
            var result=ExerciseRuntime(failure,false);
            Assert.AreEqual(failure,result.failure);
            Assert.AreEqual(failure==SupplyLineFailure.Integrity?MissionOutcomeKind.None:MissionOutcomeKind.Defeat,result.outcome);
        }
        var paused=ExerciseRuntime(SupplyLineFailure.None,true);Assert.AreEqual(MissionOutcomeKind.None,paused.outcome);Assert.AreEqual(19000,paused.hold);Assert.AreEqual(100000,paused.elapsed);
        var win=ExerciseRuntime(SupplyLineFailure.None,false);Assert.AreEqual(MissionOutcomeKind.Victory,win.outcome);Assert.AreEqual(20000,win.hold);
    }
    private static (SupplyLineFailure failure,MissionOutcomeKind outcome,int hold,int elapsed) ExerciseRuntime(SupplyLineFailure failure,bool paused)
    {
        using var world=new World("Supply Line runtime loss regression");var em=world.EntityManager;
        var mission=AssetDatabase.LoadAssetAtPath<MissionDefinitionConfig>(CH02M02SupplyLineConfigBuilder.MissionPath);
        var scenario=AssetDatabase.LoadAssetAtPath<ScenarioSetupConfig>(CH02M02SupplyLineConfigBuilder.ScenarioPath);
        var maps=AssetDatabase.LoadAssetAtPath<OperationMapCatalogConfig>(M01FirstContactConfigBuilder.OperationMapCatalogPath);
        Assert.IsTrue(CampaignMissionCatalogProjection.TryProject(em,mission,scenario,maps,1,out var root,out var error),error);
        var catalog=em.GetComponentData<CampaignMissionCatalogComponent>(root);
        try
        {
            ref var d=ref catalog.Blob.Value.Missions[0];
            var runtime=new CampaignMissionRuntimeComponent {MissionId=d.MissionId,ScenarioId=d.ScenarioId,OperationMapId=d.OperationMapId,SessionToken="supply-loss",Phase=MissionPhaseKind.Engage,DeterministicSeed=2002001,Version=1,SourceVersion=1,AttemptOrdinal=1};
            em.SetComponentData(root,runtime);
            em.SetComponentData(root,new CampaignMissionAttemptFactsComponent {CommandSquadSpawned=1,CommandSquadAlive=1,HostileTotalCount=6});
            var state=new CampaignMissionSupplyLineState {SessionToken=runtime.SessionToken,SourceVersion=1,AttemptOrdinal=1,Ready=1,OilTransferred=1,FuelTransferred=1,RouteRecovered=1,AllocatedCivilianBarrels=20,StoredFuel=40,HoldMilliseconds=19000,ElapsedMilliseconds=failure==SupplyLineFailure.Deadline?719500:100000};
            if(!em.HasComponent<CampaignMissionSupplyLineState>(root))em.AddComponentData(root,state);else em.SetComponentData(root,state);
            if(!em.HasComponent<CampaignMissionOpeningPresentationComponent>(root))em.AddComponentData(root,new CampaignMissionOpeningPresentationComponent {Stage=7});else em.SetComponentData(root,new CampaignMissionOpeningPresentationComponent {Stage=7});
            if(!em.HasBuffer<CampaignMissionSupplyLineMember>(root))em.AddBuffer<CampaignMissionSupplyLineMember>(root);
            if(!em.HasBuffer<CampaignMissionSupplyLineLink>(root))em.AddBuffer<CampaignMissionSupplyLineLink>(root);
            for(int i=0;i<16;i++)
            {
                byte kind=(byte)(i<8?0:i==8?1:i==9?2:3);var unit=em.CreateEntity(typeof(UnitHealth));
                em.SetComponentData(unit,new UnitHealth {Max=100,Current=kind==3 || failure==SupplyLineFailure.SquadLost && kind==0 || failure==SupplyLineFailure.HaulerLost && kind==1?0:100});
                em.GetBuffer<CampaignMissionSupplyLineMember>(root).Add(new CampaignMissionSupplyLineMember {Entity=unit,Kind=kind,Initialized=1});
                if(failure==SupplyLineFailure.Integrity && i==0)em.DestroyEntity(unit);
            }
            for(int i=0;i<3;i++)
            {
                var link=em.CreateEntity(typeof(UnitHealth),typeof(BuildingResourceStorageComponent));
                em.SetComponentData(link,new UnitHealth {Max=100,Current=failure==SupplyLineFailure.LinkLost && i==0?0:100});
                em.SetComponentData(link,new BuildingResourceStorageComponent {StoredOilBarrels=1,StoredFuelBarrels=40,FuelStorageCapacity=200,CivilianFuelReserveBarrels=i==2?20:0});
                em.GetBuffer<CampaignMissionSupplyLineLink>(root).Add(new CampaignMissionSupplyLineLink {Entity=link,Initialized=1});
            }
            var gameplay=em.CreateEntity(typeof(RuntimeGameplayStateComponent));em.SetComponentData(gameplay,new RuntimeGameplayStateComponent {PlayRequested=1,SimulationActive=paused?(byte)0:(byte)1});
            world.SetTime(new Unity.Core.TimeData(1,1));
            var system=world.CreateSystem<CampaignMissionRuntimeSystem>();system.Update(world.Unmanaged);
            state=em.GetComponentData<CampaignMissionSupplyLineState>(root);runtime=em.GetComponentData<CampaignMissionRuntimeComponent>(root);
            return(state.Failure,runtime.Outcome,state.HoldMilliseconds,state.ElapsedMilliseconds);
        }
        finally{catalog.Blob.Dispose();}
    }
    public static void RunManualValidation()
    {
        if(RunTests())Game.Editor.CH02M02SupplyLineInputProbe.RunManual();else MissionEditorValidationExit.Complete(false);
    }
    public static void RunWatchValidation()
    {
        if(RunTests())Game.Editor.CH02M02SupplyLineInputProbe.RunWatch();else MissionEditorValidationExit.Complete(false);
    }
    public static void RunFocusedValidation(){if(RunTests())MissionEditorValidationExit.Complete(true);else MissionEditorValidationExit.Complete(false);}
    private static bool RunTests()
    {
        try
        {
            CH02M02SupplyLineRulesValidation.Run();int passed=0;
            foreach(var fixture in new[]{typeof(CH02M02SupplyLineRegressionTests),typeof(ResourceHaulerUtilitySystemHelperTests),typeof(FactionResourceCompositionSystemHelperTests),typeof(VehicleFuelConsumptionSystemTests)})
            {
                var methods=fixture.GetMethods();
                foreach(var test in methods.Where(m=>m.IsDefined(typeof(TestAttribute),true)))
                {
                    var target=Activator.CreateInstance(fixture);
                    try {foreach(var setup in methods.Where(m=>m.IsDefined(typeof(SetUpAttribute),true)))setup.Invoke(target,null);test.Invoke(target,null);passed++;}
                    finally{foreach(var teardown in methods.Where(m=>m.IsDefined(typeof(TearDownAttribute),true)))teardown.Invoke(target,null);}
                }
            }
            Debug.Log($"[SupplyLineRegression] result=Passed tests={passed} scope=objectives-settlement-hauling-resources");return true;
        }
        catch(Exception e){Debug.LogException(e is TargetInvocationException t?t.InnerException:e);Debug.LogError("[SupplyLineRegression] result=Failed");return false;}
    }
}
