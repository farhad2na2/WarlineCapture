using System;
using System.Reflection;
using Game.Components;
using Game.Runtime;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
public sealed class M05MovementDisplacementTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            var test = new M05MovementDisplacementTests();
            test.DisplacementSurvivesTargetCleanupBeforePlayback();
            test.VehicleYieldPreservesActiveInfantryDestination(false);
            test.VehicleYieldPreservesActiveInfantryDestination(true);
            Debug.Log("[M05Displacement] result=Passed tests=3");ValidationExit.Passed();
        }
        catch(Exception e){Debug.LogException(e);ValidationExit.Failed();}
    }
    [Test] public void DisplacementSurvivesTargetCleanupBeforePlayback()
    {
        using var world=new World("M5 displacement regression");var em=world.EntityManager;
        var unit=em.CreateEntity(typeof(UnitTarget));em.SetComponentData(unit,new UnitTarget{Cell=new int2(2,2)});
        var handle=world.GetOrCreateSystem<UnitGridMovementSystem>();
        ref var state=ref world.Unmanaged.ResolveSystemStateRef(handle);
        using var walkable=new NativeArray<GridWalkable>(25,Allocator.TempJob);
        var cells=walkable;for(int i=0;i<25;i++)cells[i]=new GridWalkable{Value=1};
        using var blocked=new NativeBitArray(25,Allocator.TempJob);
        using var occupied=new NativeBitArray(25,Allocator.TempJob);
        using var factions=new NativeArray<byte>(25,Allocator.TempJob);
        using var commands=new EntityCommandBuffer(Allocator.TempJob);
        var job=new UnitGridMoveJob{Grid=new GridConfig{Width=5,Height=5},Walkable=walkable,
            DynamicBlocked=blocked,Occupied=occupied,FriendlyPassFactionIds=factions,
            UnitTargetLookup=state.GetComponentLookup<UnitTarget>(true),
            ManualMoveLookup=state.GetComponentLookup<ManualMoveOrderTag>(true),Ecb=commands.AsParallelWriter()};
        typeof(UnitGridMoveJob).GetMethod("RequestSoftBlockerMove",BindingFlags.NonPublic|BindingFlags.Instance)
            .Invoke(job,new object[]{0,unit,new int2(2,2),new int2(1,2)});
        // Another completed path removes its target after recording, before EndSimulation playback.
        em.RemoveComponent<UnitTarget>(unit);
        Assert.DoesNotThrow(()=>commands.Playback(em));
        Assert.IsTrue(em.HasComponent<UnitTarget>(unit));Assert.IsTrue(em.HasComponent<UnitPathRequest>(unit));
        Assert.AreEqual(em.GetComponentData<UnitTarget>(unit).Cell,em.GetComponentData<UnitPathRequest>(unit).Goal);
        Assert.AreNotEqual(new int2(2,2),em.GetComponentData<UnitTarget>(unit).Cell);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void VehicleYieldPreservesActiveInfantryDestination(bool segmented)
    {
        using var world = new World("Vehicle yield preserves infantry move");
        var em = world.EntityManager;
        var unit = em.CreateEntity(typeof(UnitTarget), typeof(ManualMoveOrderTag), typeof(UnitPathFollow), typeof(UnitPathRange));
        var destination = new int2(930, 430);
        em.SetComponentData(unit, new UnitTarget { Cell = destination });
        em.SetComponentData(unit, new UnitPathFollow { PathIndex = 7 });
        em.SetComponentData(unit, new UnitPathRange { Start = 20, Length = 80 });
        if (segmented) em.AddComponentData(unit, new UnitLongDistanceMove { FinalGoal = destination, ManualMove = 1 });
        var handle = world.GetOrCreateSystem<UnitGridMovementSystem>();
        ref var state = ref world.Unmanaged.ResolveSystemStateRef(handle);
        using var commands = new EntityCommandBuffer(Allocator.TempJob);
        var job = new UnitGridMoveJob {
            ManualMoveLookup = state.GetComponentLookup<ManualMoveOrderTag>(true),
            Ecb = commands.AsParallelWriter()
        };
        typeof(UnitGridMoveJob).GetMethod("RequestSoftBlockerMove", BindingFlags.NonPublic | BindingFlags.Instance)
            .Invoke(job, new object[] { 0, unit, new int2(875, 506), new int2(874, 506) });
        commands.Playback(em);
        Assert.AreEqual(destination, em.GetComponentData<UnitTarget>(unit).Cell);
        Assert.AreEqual(7, em.GetComponentData<UnitPathFollow>(unit).PathIndex);
        Assert.AreEqual(80, em.GetComponentData<UnitPathRange>(unit).Length);
        Assert.IsFalse(em.HasComponent<UnitPathRequest>(unit), "Yield must not replace the path with a nearby destination.");
        if (segmented) Assert.AreEqual(destination, em.GetComponentData<UnitLongDistanceMove>(unit).FinalGoal);
    }
}
