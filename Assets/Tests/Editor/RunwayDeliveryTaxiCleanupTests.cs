using System;
using System.Collections.Generic;
using Game.Components;
using Game.Runtime;
using NUnit.Framework;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public sealed class RunwayDeliveryTaxiCleanupTests
{
    sealed class Fixture : IDisposable
    {
        internal readonly World World = new World("RunwayDeliveryTaxiCleanup");
        internal EntityManager Em => World.EntityManager;
        internal readonly Entity Actor;
        internal readonly SystemHandle System;
        internal readonly int2 Home = new int2(40,40);
        internal readonly int2 Remapped = new int2(39,33);
        internal readonly float3 HomeWorld = new float3(40.5f,0,40.5f);
        int frame;
        internal Fixture(bool atHome = true)
        {
            var grid=Em.CreateEntity(typeof(GridConfig));
            Em.SetComponentData(grid,new GridConfig{Width=128,Height=128,CellSize=1,Origin=float3.zero});
            Actor=Em.CreateEntity(typeof(LocalTransform),typeof(UnitGrid),typeof(UnitMove),typeof(UnitAirMovement),typeof(UnitAirComponent),typeof(UnitSpawnTransitTag));
            var position=atHome?HomeWorld:new float3(40.5f,0,30.5f);
            Em.SetComponentData(Actor,LocalTransform.FromPosition(position));
            Em.SetComponentData(Actor,new UnitGrid{Cell=atHome?Home:new int2(40,30)});
            Em.SetComponentData(Actor,new UnitMove{Speed=36,WalkSpeed=36,ArriveDistance=.05f});
            Em.SetComponentData(Actor,new UnitAirMovement{CruiseHeight=8,RunwayTaxiSpeed=10});
            Em.SetComponentData(Actor,new UnitAirComponent{HomeInitialized=1,HomeCell=Home,HomePosition=HomeWorld,UsesRunway=1,
                RunwayTakeoffCell=new int2(40,55),RunwayTakeoffPosition=new float3(40.5f,0,55.5f),
                RunwayLandingCell=new int2(40,5),RunwayLandingPosition=new float3(40.5f,0,5.5f)});
            System=World.CreateSystem<UnitAirMovementSystem>();
        }
        bool ReadManager(out EntityManager em){em=Em;return true;}
        internal void Deliver(int2 goal)
        {
            var context=new BuildingProductionTransportBridgeCompositionSystemHelper.Context(ReadManager,null,null,null,default,null,null);
            var building=new RuntimeBuildingEntity{Id=15,ProducedUnits=new List<Entity>{Actor}};
            new BuildingProductionTransportBridgeCompositionSystemHelper().MoveNewestProducedUnitToCell(context,building,goal);
        }
        internal void Tick(float delta=.1f){frame++;World.SetTime(new TimeData(frame*.1d,delta));System.Update(World.Unmanaged);Em.CompleteAllTrackedJobs();}
        internal void Parked()
        {
            var air=Em.GetComponentData<UnitAirComponent>(Actor);
            Assert.AreEqual(0,air.Airborne);Assert.AreEqual(0,air.TakeoffRolling);Assert.AreEqual(0,air.ReturningHome);
            Assert.AreEqual(Home,air.HomeCell);Assert.AreEqual(HomeWorld,air.HomePosition);
            Assert.AreEqual(HomeWorld,Em.GetComponentData<LocalTransform>(Actor).Position);
        }
        public void Dispose()=>World.Dispose();
    }
    public static void RunFocusedValidation()
    {
        var t=new RunwayDeliveryTaxiCleanupTests();
        t.InitializedRunwayDeliveryUsesNativeTaxiWithoutInternalGroundOrder();
        t.ActualManualMoveQueuedDuringTaxiSurvivesAndTakesOff();
        t.DeliveryDoesNotOverwriteAnyQueuedTargetOrRequest();
        t.UninitializedRunwayStillUsesExistingTargetPathAdmission();
        t.NonRunwayTransitStillUsesExistingTargetPathAdmission();
        t.GroundDeliveryStillUsesExistingTargetPathAdmission();
        t.ActualManualHomeMoveSurvivesTaxiThenCompletesWithoutTakeoff();
        t.ZeroDeltaRetainsTransitUntilNativeResume();
        Debug.Log("[RunwayDeliveryTaxiCleanupTests] result=Passed cases=8 scope=IsolatedActualSystemNotNormalMission");
    }
    [Test] public void InitializedRunwayDeliveryUsesNativeTaxiWithoutInternalGroundOrder()
    {
        using var f=new Fixture(false);f.Deliver(f.Home);
        Assert.IsFalse(f.Em.HasComponent<UnitTarget>(f.Actor));Assert.IsFalse(f.Em.HasComponent<UnitPathRequest>(f.Actor));
        f.Tick();Assert.IsTrue(f.Em.HasComponent<UnitSpawnTransitTag>(f.Actor));
        for(int i=0;i<30&&f.Em.HasComponent<UnitSpawnTransitTag>(f.Actor);i++)f.Tick();
        Assert.IsFalse(f.Em.HasComponent<UnitSpawnTransitTag>(f.Actor));
        for(int i=0;i<120;i++){f.Tick();f.Parked();}
    }
    [Test] public void ActualManualMoveQueuedDuringTaxiSurvivesAndTakesOff()
    {
        using var f=new Fixture();var goal=new int2(90,75);
        new UnitMoveOrderSystem().IssueImmediateMoveCommand(f.Em,f.Actor,goal);f.Deliver(f.Home);
        Assert.IsTrue(f.Em.HasComponent<ManualMoveOrderTag>(f.Actor));f.Tick();f.Parked();
        Assert.IsFalse(f.Em.HasComponent<UnitSpawnTransitTag>(f.Actor));
        Assert.AreEqual(goal,f.Em.GetComponentData<UnitTarget>(f.Actor).Cell);
        Assert.IsTrue(f.Em.HasComponent<ManualMoveOrderTag>(f.Actor));
        bool rolling=false,airborne=false;
        for(int i=0;i<600;i++)
        {
            f.Tick();var air=f.Em.GetComponentData<UnitAirComponent>(f.Actor);
            rolling|=air.TakeoffRolling!=0;airborne|=air.Airborne!=0;
            if(airborne)break;
        }
        Assert.IsTrue(rolling,"Actual queued Move must retain native takeoff roll.");
        Assert.IsTrue(airborne,"Actual queued Move must retain positive-time native lift-off.");
    }
    [Test] public void DeliveryDoesNotOverwriteAnyQueuedTargetOrRequest()
    {
        using var f=new Fixture();var other=new int2(88,70);
        f.Em.AddComponentData(f.Actor,new UnitTarget{Cell=other});
        f.Em.AddComponentData(f.Actor,new UnitPathRequest{Goal=other});
        Assert.IsFalse(f.Em.HasComponent<ManualMoveOrderTag>(f.Actor));f.Deliver(f.Home);
        Assert.AreEqual(other,f.Em.GetComponentData<UnitTarget>(f.Actor).Cell);
        Assert.AreEqual(other,f.Em.GetComponentData<UnitPathRequest>(f.Actor).Goal);
        // This control proves noninterference with a supplied foreign owner; it
        // does not claim that arbitrary seeded targets represent public input.
    }
    static void AssertAdmitted(Fixture f)
    {
        f.Deliver(f.Remapped);
        Assert.AreEqual(f.Remapped,f.Em.GetComponentData<UnitTarget>(f.Actor).Cell);
        Assert.AreEqual(f.Remapped,f.Em.GetComponentData<UnitPathRequest>(f.Actor).Goal);
    }
    [Test] public void UninitializedRunwayStillUsesExistingTargetPathAdmission()
    {
        using var f=new Fixture();var air=f.Em.GetComponentData<UnitAirComponent>(f.Actor);air.HomeInitialized=0;f.Em.SetComponentData(f.Actor,air);AssertAdmitted(f);
    }
    [Test] public void NonRunwayTransitStillUsesExistingTargetPathAdmission()
    {
        using var f=new Fixture();var air=f.Em.GetComponentData<UnitAirComponent>(f.Actor);air.UsesRunway=0;f.Em.SetComponentData(f.Actor,air);AssertAdmitted(f);
    }
    [Test] public void GroundDeliveryStillUsesExistingTargetPathAdmission()
    {
        using var f=new Fixture();f.Em.RemoveComponent<UnitAirMovement>(f.Actor);AssertAdmitted(f);
    }
    [Test] public void ActualManualHomeMoveSurvivesTaxiThenCompletesWithoutTakeoff()
    {
        using var f=new Fixture();new UnitMoveOrderSystem().IssueImmediateMoveCommand(f.Em,f.Actor,f.Home);f.Deliver(f.Home);
        f.Em.AddComponent<SelectedUnitTag>(f.Actor);f.Tick();
        Assert.IsTrue(f.Em.HasComponent<ManualMoveOrderTag>(f.Actor));Assert.AreEqual(f.Home,f.Em.GetComponentData<UnitTarget>(f.Actor).Cell);
        f.Tick();Assert.IsFalse(f.Em.HasComponent<UnitTarget>(f.Actor));Assert.IsFalse(f.Em.HasComponent<ManualMoveOrderTag>(f.Actor));f.Parked();
    }
    [Test] public void ZeroDeltaRetainsTransitUntilNativeResume()
    {
        using var f=new Fixture();f.Deliver(f.Home);for(int i=0;i<10;i++)f.Tick(0);
        Assert.IsTrue(f.Em.HasComponent<UnitSpawnTransitTag>(f.Actor));Assert.IsFalse(f.Em.HasComponent<UnitTarget>(f.Actor));f.Parked();
        f.Tick();Assert.IsFalse(f.Em.HasComponent<UnitSpawnTransitTag>(f.Actor));f.Parked();
    }
}
