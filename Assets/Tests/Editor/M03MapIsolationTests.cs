using System;
using Game.Components;
using Game.Runtime;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public sealed class M03MapIsolationTests
{
    [Test] public void AuthoredVehiclesAreDormantOnlyDuringDefenseAndOriginalDisabledStateSurvives()
    {
        using var world=new World("M3 authored map isolation"); var em=world.EntityManager;
        using var builder=new BlobBuilder(Allocator.Temp);
        ref var catalog=ref builder.ConstructRoot<CampaignMissionCatalogBlob>();
        var missions=builder.Allocate(ref catalog.Missions,1);
        missions[0].MissionId="saga.ch01.m03.radar_warning"; missions[0].ScenarioId="test.scenario"; missions[0].OperationMapId="test.map";
        missions[0].Defense.Enabled=1; missions[0].Defense.AuthoredMapDefensesDormant=1;
        using var blob=builder.CreateBlobAssetReference<CampaignMissionCatalogBlob>(Allocator.Persistent);
        var root=em.CreateEntity(typeof(CampaignMissionRootComponent),typeof(CampaignMissionRuntimeComponent),typeof(CampaignMissionCatalogComponent));
        em.SetComponentData(root,new CampaignMissionCatalogComponent {Blob=blob});
        em.SetComponentData(root,new CampaignMissionRuntimeComponent {MissionId="saga.ch01.m03.radar_warning",ScenarioId="test.scenario",OperationMapId="test.map"});
        var gameplay=em.CreateEntity(typeof(RuntimeGameplayStateComponent));
        em.SetComponentData(gameplay,new RuntimeGameplayStateComponent {PlayRequested=1});
        em.CreateEntity(typeof(OperationMapMetadataComponent));
        var authored=em.CreateEntity(typeof(OperationMapAuthoredVehiclePresentation),typeof(UnitGrid),typeof(UnitMove));
        var alreadyDisabled=em.CreateEntity(typeof(OperationMapAuthoredVehiclePresentation),typeof(UnitGrid),typeof(UnitMove),typeof(Disabled));
        var mission=em.CreateEntity(typeof(OperationMapAuthoredVehiclePresentation),typeof(UnitGrid),typeof(UnitMove),typeof(CampaignMissionUnitRoleComponent));
        var produced=em.CreateEntity(typeof(UnitGrid),typeof(UnitMove));
        var system=world.GetOrCreateSystem<CampaignMissionSpawnSystem>();
        for(int cycle=0;cycle<2;cycle++)
        {
            em.SetComponentData(gameplay,new RuntimeGameplayStateComponent {PlayRequested=1}); system.Update(world.Unmanaged);
            Assert.IsTrue(em.HasComponent<Disabled>(authored)); Assert.IsTrue(em.HasComponent<CampaignMissionDormantMapUnitTag>(authored));
            Assert.IsFalse(em.HasComponent<CampaignMissionDormantMapUnitTag>(alreadyDisabled));
            Assert.IsFalse(em.HasComponent<Disabled>(mission)); Assert.IsFalse(em.HasComponent<Disabled>(produced));
            em.SetComponentData(gameplay,default(RuntimeGameplayStateComponent)); system.Update(world.Unmanaged);
            Assert.IsFalse(em.HasComponent<Disabled>(authored)); Assert.IsFalse(em.HasComponent<CampaignMissionDormantMapUnitTag>(authored));
            Assert.IsTrue(em.HasComponent<Disabled>(alreadyDisabled));
        }
    }
    public static void RunFocusedValidation()
    {
        try {new M03MapIsolationTests().AuthoredVehiclesAreDormantOnlyDuringDefenseAndOriginalDisabledStateSurvives();
            Debug.Log("[M03MapIsolation] result=Passed authoredVehiclesDormant=true exitRestores=true originalDisabledPreserved=true missionAndProductionUnaffected=true"); ValidationExit.Passed();}
        catch(Exception error) {Debug.LogException(error); Debug.LogError("[M03MapIsolation] result=Failed"); ValidationExit.Failed();}
    }
}
