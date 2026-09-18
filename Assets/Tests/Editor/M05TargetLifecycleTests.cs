using System;
using System.Reflection;
using Game.Components;
using Game.Configs;
using Game.Missions.Contracts;
using Unity.Mathematics;
using Game.Runtime;
using NUnit.Framework;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

public sealed class M05TargetLifecycleTests
{
    [Test]
    public void DeadOrRemovedTargetsClearAllAttackMarkersBeforeTimerExpires()
    {
        using var world = new World("Attack marker death test");
        var em = world.EntityManager;
        foreach (bool remove in new[] { false, true })
        {
            var target = em.CreateEntity(typeof(UnitHealth));
            em.SetComponentData(target, new UnitHealth { Current = 100, Max = 100 });
            var marker = new GameObject("Marker under test");
            var ring = new GameObject("Ring under test");
            var selection = new GameObject("Frame under test");
            try
            {
                var helper = new SelectionOrderMarkerPresentationSystemHelper();
                Set(helper,"_queryWorld",world); Set(helper,"_attackMarkerTarget",target);
                Set(helper,"_attackOrderMarkerHideTime",Time.time+1000f);
                Set(helper,"_attackOrderMarker",marker); Set(helper,"_attackTargetRingMarker",ring);
                Set(helper,"_attackTargetSelectionMarker",selection);
                helper.UpdateAttackOrderMarkerVisibility(null);
                Assert.IsTrue(selection.activeSelf, "A live target must retain its marker");
                if(remove) em.DestroyEntity(target);
                else em.SetComponentData(target,new UnitHealth { Current=0,Max=100 });
                helper.UpdateAttackOrderMarkerVisibility(null);
                Assert.IsFalse(marker.activeSelf); Assert.IsFalse(ring.activeSelf); Assert.IsFalse(selection.activeSelf);
            }
            finally { UnityEngine.Object.DestroyImmediate(marker);UnityEngine.Object.DestroyImmediate(ring);UnityEngine.Object.DestroyImmediate(selection); }
        }
    }

    [Test]
    public void InactiveDestroyedBuildingChildAppearsWithItsOriginalWorldTransform()
    {
        using var world = new World("Building wreck child test");
        var root = new GameObject("Building");
        try
        {
            root.transform.position=new Vector3(10,2,5);
            var alive=new GameObject("Alive");alive.transform.SetParent(root.transform,false);
            var child=new GameObject("Destroyed");child.transform.SetParent(alive.transform,false);
            child.transform.localPosition=new Vector3(2,0,1);child.transform.localScale=new Vector3(2,3,4);
            child.SetActive(false);
            var position=child.transform.position;var scale=child.transform.lossyScale;
            var building=new RuntimeBuildingEntity { Instance=root,Definition=new BuildingDefinition(),AliveVisualRoots=new[]{alive.transform} };
            var helper=new BuildingDestroyedVisualPresentationSystemHelper();
            var context=new BuildingDestroyedVisualPresentationSystemHelper.Context(world.GetOrCreateSystemManaged<BuildingVisualSystem>(),UnityEngine.Object.DestroyImmediate);
            helper.BeginDestroyedVisual(context,building);
            Assert.IsFalse(alive.activeSelf);Assert.IsTrue(building.DestroyedVisualInstance.activeInHierarchy);
            Assert.AreEqual(position,building.DestroyedVisualInstance.transform.position);
            Assert.AreEqual(scale,building.DestroyedVisualInstance.transform.lossyScale);
            var wreck=building.DestroyedVisualInstance;helper.BeginDestroyedVisual(context,building);
            Assert.AreSame(wreck,building.DestroyedVisualInstance);
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    [Test]
    public void LegacyVehicleWreckRetainsAuthoredScale()
    {
        using var world=new World("Vehicle child wreck test");var em=world.EntityManager;
        var vehicle=em.CreateEntity();var alive=em.CreateEntity(typeof(LocalTransform));var wreck=em.CreateEntity(typeof(LocalTransform));
        em.SetComponentData(alive,LocalTransform.Identity);em.SetComponentData(wreck,LocalTransform.FromScale(0));
        em.AddComponentData(vehicle,new UnitDestroyedVisualReference { AliveVisual=alive,DestroyedVisual=wreck,AliveVisibleScale=1,DestroyedVisibleScale=2.5f });
        em.AddComponentData(vehicle,new VehicleDestroyedVisualPrefabReference { Prefab=Entity.Null });
        var children=em.AddBuffer<Child>(vehicle);children.Add(new Child { Value=alive });children.Add(new Child { Value=wreck });
        var method=typeof(UnitDeathSystem).GetMethod("TryBeginVehicleWreck",BindingFlags.NonPublic|BindingFlags.Static);
        Assert.IsTrue((bool)method.Invoke(null,new object[]{em,vehicle}));
        Assert.AreEqual(0,em.GetComponentData<LocalTransform>(alive).Scale);
        Assert.AreEqual(2.5f,em.GetComponentData<LocalTransform>(wreck).Scale);
    }

    [Test]
    public void ArchiveGuidanceUsesTheRecoveryRadiusAndRestartsAfterLeaving()
    {
        using var world=new World("Archive objective guidance");var em=world.EntityManager;
        var root=em.CreateEntity(typeof(CampaignMissionRootComponent),typeof(CampaignMissionRuntimeComponent),
            typeof(CampaignMissionAttemptFactsComponent),typeof(CampaignMissionBreachState),typeof(CampaignMissionGuidanceProjectionComponent));
        em.AddBuffer<CampaignMissionGuidanceAcknowledgementRequestElement>(root);
        var unit=em.CreateEntity(typeof(UnitHealth),typeof(LocalTransform),typeof(SelectedUnitTag));
        em.SetComponentData(unit,new UnitHealth { Current=100,Max=100 });
        em.AddBuffer<CampaignMissionBreachMember>(root).Add(new CampaignMissionBreachMember { Entity=unit,Kind=0 });
        var runtime=new CampaignMissionRuntimeComponent { MissionId="saga.ch01.m05.breach_assault",Phase=MissionPhaseKind.Engage };
        var breach=new CampaignMissionBreachState { Ready=1,GateInitialized=1,CoreInitialized=1,GateDestroyed=1,CoreDestroyed=1,
            CounterattackReleased=1,GuidanceCompletedMask=127,SupportLost=1,SecureRequiredMilliseconds=20000 };
        var definition=new CampaignMissionBreachDefinitionBlob { ArchiveRadius=6,SecureHoldMilliseconds=20000,GateHealth=100,CoreHealth=100 };
        var facts=new CampaignMissionAttemptFactsComponent();em.SetComponentData(root,runtime);
        var guidanceSystem=world.CreateSystem<CampaignMissionGuidanceProjectionSystem>();
        foreach(float distance in new[]{10f,4f,10f})
        {
            em.SetComponentData(unit,LocalTransform.FromPosition(new float3(distance,0,0)));
            CampaignMissionRuntimeSystem.ProjectBreachRoster(em,root,in runtime,ref breach,ref definition,ref facts,1f);
            em.SetComponentData(root,breach);em.SetComponentData(root,facts);guidanceSystem.Update(world.Unmanaged);
            var guidance=em.GetComponentData<CampaignMissionGuidanceProjectionComponent>(root);
            Assert.AreEqual(distance<=6?1:0,breach.FriendlyAtArchive);
            Assert.AreEqual(distance<=6?1000:0,breach.SecureHoldMilliseconds);
            Assert.AreEqual(1,guidance.CanShow,"The recovery area remains inspectable while waiting, as well as after leaving it.");
            Assert.AreEqual(distance<=6?0:1,guidance.CanExecute,"Waiting in the archive must not request another movement order.");
            Assert.AreEqual(distance<=6?AssistantRecommendationKind.Explain:AssistantRecommendationKind.Move,guidance.RecommendationKind);
        }
    }

    [Test]
    public void MinimapDropsDeadMarkersBetweenPositionRefreshes()
    {
        using var world=new World("Minimap death timing");var em=world.EntityManager;
        var source=em.CreateEntity(typeof(UnitHealth),typeof(LocalTransform),typeof(Faction));
        em.SetComponentData(source,new UnitHealth{Current=10,Max=10});em.SetComponentData(source,LocalTransform.Identity);
        em.SetComponentData(source,new Faction{Id=2});
        var system=world.CreateSystem<MatchHudMinimapMarkerSystem>();system.Update(world.Unmanaged);em.CompleteAllTrackedJobs();
        using var query=em.CreateEntityQuery(typeof(MatchHudMinimapMarkerStateComponent));var boundary=query.GetSingletonEntity();
        Assert.AreEqual(1,em.GetBuffer<MatchHudMinimapMarkerElement>(boundary).Length);
        em.SetComponentData(source,new UnitHealth{Current=0,Max=10});
        system.Update(world.Unmanaged);em.CompleteAllTrackedJobs();
        Assert.AreEqual(0,em.GetBuffer<MatchHudMinimapMarkerElement>(boundary).Length,"Death must not wait for the 0.2s minimap refresh");
    }

    public static void RunFocusedValidation()
    {
        var t=new M05TargetLifecycleTests();
        t.DeadOrRemovedTargetsClearAllAttackMarkersBeforeTimerExpires();
        t.InactiveDestroyedBuildingChildAppearsWithItsOriginalWorldTransform();
        t.LegacyVehicleWreckRetainsAuthoredScale();
        t.ArchiveGuidanceUsesTheRecoveryRadiusAndRestartsAfterLeaving();
        t.MinimapDropsDeadMarkersBetweenPositionRefreshes();
        new VehicleVisualAdornmentsSystemTests().VehicleDestroyedVisualSystemSpawnsDestroyedVisualAndCleansRuntimeAdornments();
        var b=new BuildingDestroyedVisualPresentationSystemHelperTests();b.SetUp();
        try { b.BeginDestroyedVisualHidesAliveRootsAndSpawnsConfiguredPrefab(); } finally { b.TearDown(); }
        Debug.Log("[M05TargetLifecycle] result=Passed markers clear on death/removal; building and vehicle wreck paths");
    }
    private static void Set(object instance,string name,object value)=>instance.GetType().GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(instance,value);
}
