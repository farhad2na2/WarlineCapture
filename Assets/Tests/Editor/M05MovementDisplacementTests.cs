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
            test.TruncatedPathRequestsOriginalDestination(0, 3, false);
            test.TruncatedPathRequestsOriginalDestination(0, 3, true);
            test.TruncatedPathRequestsOriginalDestination(-1, 2, false);
            test.TruncatedPathRequestsOriginalDestination(int.MaxValue, 2, false);
            Debug.Log("[M05Displacement] result=Passed tests=7");ValidationExit.Passed();
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

    [TestCase(0, 3, false)]
    [TestCase(0, 3, true)]
    [TestCase(-1, 2, false)]
    [TestCase(int.MaxValue, 2, false)]
    public void TruncatedPathRequestsOriginalDestination(int start, int length, bool segmented)
    {
        using var world = new World("Truncated movement path regression");
        var em = world.EntityManager;
        var unit = em.CreateEntity(typeof(UnitTarget), typeof(UnitPathFollow), typeof(UnitPathRange));
        var goal = new int2(10, 12);
        em.SetComponentData(unit, new UnitTarget { Cell = goal });
        if (segmented)
            em.AddComponentData(unit, new UnitLongDistanceMove { FinalGoal = new int2(20, 22), ManualMove = 1 });
        var handle = world.GetOrCreateSystem<UnitGridMovementSystem>();
        ref var state = ref world.Unmanaged.ResolveSystemStateRef(handle);
        using var pool = new NativeArray<int2>(2, Allocator.TempJob);
        using var commands = new EntityCommandBuffer(Allocator.TempJob);
        var job = new UnitGridMoveJob
        {
            Pool = pool, Ecb = commands.AsParallelWriter(),
            Grid = new GridConfig { Width = 32, Height = 32, CellSize = 1 },
            CampaignGuidedMoveLookup = state.GetComponentLookup<CampaignMissionGuidedMoveInProgressTag>(true),
            ManualMoveGroupLookup = state.GetComponentLookup<ManualMoveGroupMemberTag>(true),
            BoardingTargetLookup = state.GetComponentLookup<UnitTransportBoardingTarget>(true),
            UnitTargetLookup = state.GetComponentLookup<UnitTarget>(true),
            LongDistanceMoveLookup = state.GetComponentLookup<UnitLongDistanceMove>(true)
        };
        var transform = Unity.Transforms.LocalTransform.Identity;
        var grid = new UnitGrid();
        var follow = new UnitPathFollow();
        var kinematics = new UnitVehicleKinematics { CurrentSpeed = 5, StallSeconds = 1 };
        job.Execute(0, unit, ref transform, ref grid, ref follow, ref kinematics,
            new UnitMove(), new UnitPathRange { Start = start, Length = length },
            new UnitFootprint { Size = new int2(1, 1) }, new UnitMovementBehavior(),
            new UnitVehicleMovement(), new Faction { Id = 1 });
        commands.Playback(em);
        Assert.That(em.HasComponent<UnitPathRange>(unit), Is.False);
        Assert.That(em.HasComponent<UnitPathFollow>(unit), Is.False);
        Assert.That(em.GetComponentData<UnitPathRequest>(unit).Goal,
            Is.EqualTo(segmented ? new int2(20, 22) : goal));
        Assert.That(kinematics.CurrentSpeed, Is.Zero);
        Assert.That(kinematics.StallSeconds, Is.Zero);
    }
}
