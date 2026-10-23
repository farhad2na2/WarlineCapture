#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using Game.Components;
using Game.Runtime;
using Game.Runtime.Pathfinding;
using NUnit.Framework;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
namespace Game.Tests.Editor
{
    public sealed class MultiUnitGoalAssignmentKernelTests
    {
        public static int RunChecks()
        {
            var t=new MultiUnitGoalAssignmentKernelTests();t.OriginalSourcesAndInstalledHashProvenanceArePinned();
            t.NativeMultiRequestAssignmentMatchesOriginalManagedAndBurst();t.ReservationPreparationAndFootprintsMatchOriginal();
            t.SharedCandidateCallOrderAndSchedulerFallback();t.InstalledHashAndRingMatchManagedAndBurst();
            Debug.Log("[MultiUnitGoalAssignmentParity] result=Passed tests=5 nativeBatchCases=1024 hashCases=8192 managed=Passed burst=Passed missionReady=0");return 5;
        }
        private static string ReadPinned(string name,string hash)
        {
            string text=File.ReadAllText(Path.Combine(Application.dataPath,"Tests/Editor/"+name)).Replace("\r\n","\n");
            using var sha=SHA256.Create();Assert.AreEqual(hash,BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(text))).Replace("-","").ToLowerInvariant());return text;
        }
        [Test] public void OriginalSourcesAndInstalledHashProvenanceArePinned()
        {
            var assignment=ReadPinned("MultiUnitGoalUnitPathGoalAssignmentOriginal.cs.txt","cba8606a328972e7f0e6138ab804eb6c98fd3332ef403ba825a5d8c92686d1e1");
            var reservation=ReadPinned("MultiUnitGoalUnitPathReservedGoalOriginal.cs.txt","e659d4e26e865d489629ad23ac46ae0e0ba82af314d960c6d822170788dec299");
            ReadPinned("MultiUnitGoalUnitPathfindingSchedulerOriginal.cs.txt","bd0f8410b3cff62aefe40a2eb268b741629bd1447e5cee117dede13486626fe3");
            ReadPinned("MultiUnitGoalInstalledHashInt3.il.txt","fa5d69ae0a341d58f8c68a0712acb879d7848febc4313ce99ff5de83340282e0");
            Assert.AreEqual("#if UNITY_EDITOR && UNITY_INCLUDE_TESTS\n"+assignment.Replace("UnitPathGoalAssignment","MultiUnitGoalOriginalAssignment").Replace("UnitPathReservedGoal","MultiUnitGoalOriginalReservation")+"#endif\n",File.ReadAllText(Path.Combine(Application.dataPath,"Tests/Editor/MultiUnitGoalOriginalAssignment.cs")).Replace("\r\n","\n"));
            Assert.AreEqual("#if UNITY_EDITOR && UNITY_INCLUDE_TESTS\n"+reservation.Replace("UnitPathReservedGoal","MultiUnitGoalOriginalReservation")+"#endif\n",File.ReadAllText(Path.Combine(Application.dataPath,"Tests/Editor/MultiUnitGoalOriginalReservation.cs")).Replace("\r\n","\n"));
        }
        private struct Request
        {
            public int2 Desired,Start,Size,IgnoredCell,IgnoredSize;
            public Entity Entity,Ignored;
            public byte Faction;
        }
        [BurstCompile(CompileSynchronously=true)] private struct BatchJob:IJob
        {
            public GridConfig Grid;
            [ReadOnly] public NativeArray<GridWalkable> Walkable;
            [ReadOnly] public NativeBitArray Blocked,Occupied;
            [ReadOnly] public NativeArray<byte> Friendly;
            [ReadOnly] public NativeArray<Entity> Entities;
            [ReadOnly] public NativeArray<UnitGrid> Grids;
            [ReadOnly] public NativeArray<UnitFootprint> Sizes;
            [ReadOnly] public NativeArray<Request> Requests;
            public NativeArray<int> ActualReservations,OriginalReservations;
            public NativeArray<int2> ActualGoals,OriginalGoals;
            public NativeArray<byte> Compiled;
            [BurstDiscard] private static void ManagedOnly(ref bool value){value=false;}
            public void Execute()
            {
                bool burst=true;ManagedOnly(ref burst);Compiled[0]=burst?(byte)1:(byte)0;
                for(int i=0;i<Requests.Length;i++)
                {
                    var r=Requests[i];int index=GridUtils.InBounds(r.Start,Grid.Width,Grid.Height)?GridUtils.CellToIndex(r.Start,Grid.Width):-1;
                    ActualGoals[i]=new UnitPathGoalAssignment().FindNearestFreeGoal(Grid,Walkable,Blocked,Friendly,Occupied,Entities,Grids,Sizes,ActualReservations,7,r.Entity,r.Ignored,r.IgnoredCell,r.IgnoredSize,default,r.Desired,r.Start,r.Size,r.Faction,index);
                    OriginalGoals[i]=new MultiUnitGoalOriginalAssignment().FindNearestFreeGoal(Grid,Walkable,Blocked,Friendly,Occupied,Entities,Grids,Sizes,OriginalReservations,7,r.Entity,r.Ignored,r.IgnoredCell,r.IgnoredSize,default,r.Desired,r.Start,r.Size,r.Faction,index);
                }
            }
        }
        private static void Set<T>(NativeArray<T> values,int index,T value)where T:struct=>values[index]=value;
        [Test] public void NativeMultiRequestAssignmentMatchesOriginalManagedAndBurst()
        {
            Assert.IsTrue(BurstCompiler.IsEnabled);
            foreach(bool burst in new[]{false,true})for(int seed=0;seed<64;seed++)
            {
                using var walkable=new NativeArray<GridWalkable>(256,Allocator.TempJob);
                using var blocked=new NativeBitArray(256,Allocator.TempJob);using var occupied=new NativeBitArray(256,Allocator.TempJob);
                using var friendly=new NativeArray<byte>(256,Allocator.TempJob);using var entities=new NativeArray<Entity>(8,Allocator.TempJob);
                using var grids=new NativeArray<UnitGrid>(8,Allocator.TempJob);using var sizes=new NativeArray<UnitFootprint>(8,Allocator.TempJob);
                using var requests=new NativeArray<Request>(8,Allocator.TempJob);using var actual=new NativeArray<int>(256,Allocator.TempJob);
                using var original=new NativeArray<int>(256,Allocator.TempJob);using var ag=new NativeArray<int2>(8,Allocator.TempJob);using var og=new NativeArray<int2>(8,Allocator.TempJob);using var compiled=new NativeArray<byte>(1,Allocator.TempJob);
                var random=new System.Random(seed*971+27);
                for(int i=0;i<256;i++)
                {
                    Set(walkable,i,new GridWalkable{Value=(byte)(seed==1?0:random.Next(10)==0?0:1)});
                    blocked.Set(i,seed!=0&&random.Next(11)==0);occupied.Set(i,seed!=0&&random.Next(13)==0);
                    Set(friendly,i,(byte)(random.Next(3)==0?1:0));if(seed%3==0&&random.Next(17)==0){Set(actual,i,7);Set(original,i,7);}
                }
                for(int i=0;i<8;i++)
                {
                    var entity=new Entity{Index=i+1,Version=1};var start=new int2(1,i+2);var size=seed%5==0?new int2(2,2):new int2(1,1);
                    Set(entities,i,entity);Set(grids,i,new UnitGrid{Cell=start});Set(sizes,i,new UnitFootprint{Size=size});
                    var desired=seed%4==0?new int2(9,8):new int2(random.Next(-2,19),random.Next(-2,19));if(i==0&&seed%7==0)desired=start;
                    var r=new Request{Entity=entity,Start=start,Desired=desired,Size=size,Faction=1};
                    if(seed%9==0){r.Ignored=new Entity{Index=8,Version=1};r.IgnoredCell=new int2(1,9);r.IgnoredSize=size;}
                    Set(requests,i,r);
                }
                var job=new BatchJob{Grid=new GridConfig{Width=16,Height=16,CellSize=1},Walkable=walkable,Blocked=blocked,Occupied=occupied,Friendly=friendly,Entities=entities,Grids=grids,Sizes=sizes,Requests=requests,ActualReservations=actual,OriginalReservations=original,ActualGoals=ag,OriginalGoals=og,Compiled=compiled};
                if(burst)job.Schedule().Complete();else job.Execute();Assert.AreEqual(burst?1:0,compiled[0],"actual Burst sentinel");
                CollectionAssert.AreEqual(og.ToArray(),ag.ToArray(),"ordered batch goals seed="+seed);CollectionAssert.AreEqual(original.ToArray(),actual.ToArray(),"ordered reservations seed="+seed);
                if(seed==0)for(int i=1;i<8;i++)for(int j=1;j<i;j++)Assert.AreNotEqual(ag[j],ag[i],"unblocked shared goals reserve distinct cells");
            }
        }
        [Test] public void ReservationPreparationAndFootprintsMatchOriginal()
        {
            var actual=new UnitPathReservedGoal();var original=new MultiUnitGoalOriginalReservation();
            try
            {
                foreach(int size in new[]{64,64,64,0,0,0,16,16})
                {
                    actual.Prepare(size);original.Prepare(size);Assert.AreEqual(original.Generation,actual.Generation);Assert.AreEqual(original.Epochs.IsCreated,actual.Epochs.IsCreated);
                    CollectionAssert.AreEqual(original.Epochs.ToArray(),actual.Epochs.ToArray());
                    if(size>0){Set(actual.Epochs,0,13);Set(original.Epochs,0,13);}
                }
                // Exercise generation wrap on the existing same-size native allocation.
                actual.Generation=original.Generation=int.MaxValue;actual.Prepare(16);original.Prepare(16);Assert.AreEqual(1,actual.Generation);CollectionAssert.AreEqual(original.Epochs.ToArray(),actual.Epochs.ToArray());
            }
            finally{actual.Dispose();original.Dispose();}
            using var x=new NativeArray<int>(144,Allocator.TempJob);using var y=new NativeArray<int>(144,Allocator.TempJob);
            foreach(int sx in new[]{-1,0,1,2,3,4})foreach(int sz in new[]{0,1,2,5})
            {
                x.AsSpan().Clear();y.AsSpan().Clear();var grid=new GridConfig{Width=12,Height=12};var cell=new int2(6,6);var size=new int2(sx,sz);
                UnitPathReservedGoal.ReserveGoalFootprint(grid,x,9,cell,size);MultiUnitGoalOriginalReservation.ReserveGoalFootprint(grid,y,9,cell,size);CollectionAssert.AreEqual(y.ToArray(),x.ToArray());
            }
            var wrapped=PathGoalReservationKernel.DecidePrepare(true,0,int.MaxValue,0);Assert.IsTrue(wrapped.Clear);Assert.IsFalse(wrapped.Reallocate);Assert.AreEqual(1,wrapped.Generation);
        }
        private struct CandidateProbe:IPathGoalCandidateContext
        {
            public int Width=>8;public bool Placement,Surface;public int ReservedAt;public NativeList<int> Calls;
            public bool CanPlaceGoal(PlacementCell cell){Calls.Add(1);return Placement;}
            public bool CanTraverseGoalSurface(PlacementCell cell){Calls.Add(2);return Surface;}
            public int ReservedEpoch(int index){Calls.Add(100+index);return index==ReservedAt?5:0;}
        }
        private struct AssignmentProbe:IPathGoalIdentityContext
        {
            public int Width=>8;public int Height=>8;public NativeList<int> Calls;public int AcceptedIndex;
            public bool CanUseGoal(PlacementCell cell){int index=cell.y*8+cell.x;Calls.Add(index);return index==AcceptedIndex;}
            public void ReserveGoal(PlacementCell cell){Calls.Add(1000+cell.y*8+cell.x);}
        }
        [Test] public void SharedCandidateCallOrderAndSchedulerFallback()
        {
            using var calls=new NativeList<int>(Allocator.TempJob);
            var c=new CandidateProbe{Calls=calls,ReservedAt=-1};var cell=new PlacementCell(3,3);var size=new PlacementCell(2,2);
            Assert.IsFalse(PathGoalAssignmentKernel.CanUseGoal(ref c,cell,size,5));CollectionAssert.AreEqual(new[]{1},calls.AsArray().ToArray());calls.Clear();c.Placement=true;
            Assert.IsFalse(PathGoalAssignmentKernel.CanUseGoal(ref c,cell,size,5));CollectionAssert.AreEqual(new[]{1,2},calls.AsArray().ToArray());calls.Clear();c.Surface=true;
            Assert.IsTrue(PathGoalAssignmentKernel.CanUseGoal(ref c,cell,size,5));CollectionAssert.AreEqual(new[]{1,2,127,128,135,136},calls.AsArray().ToArray());calls.Clear();c.ReservedAt=28;
            Assert.IsFalse(PathGoalAssignmentKernel.CanUseGoal(ref c,cell,size,5));CollectionAssert.AreEqual(new[]{1,2,127,128},calls.AsArray().ToArray());calls.Clear();
            var a=new AssignmentProbe{Calls=calls,AcceptedIndex=-1};var start=new PlacementCell(1,1);
            Assert.AreEqual(start,PathGoalAssignmentKernel.FindNearestFreeGoal(ref a,start,start,new PlacementCell(1,1),9));Assert.AreEqual(0,calls.Length,"start bypasses legality and reservation");
            var desired=new PlacementCell(3,3);PathGoalAssignmentKernel.FindNearestFreeGoal(ref a,desired,start,new PlacementCell(1,1),9);
            Assert.AreEqual(27,calls[0]);int first=(int)(math.hash(new int3(3,3,9))%8u);var offset=MultiUnitGoalOriginalAssignment.SquareRingOffset(1,first);Assert.AreEqual((3+offset.y)*8+3+offset.x,calls[1]);
            foreach(var assigned in new[]{start,desired})foreach(var goal in new[]{start,desired,new PlacementCell(-1,2),new PlacementCell(8,1)})foreach(bool surface in new[]{false,true})
            {
                bool expected=assigned.Equals(start)&&!goal.Equals(start)&&goal.x>=0&&goal.y>=0&&goal.x<8&&goal.y<8;
                Assert.AreEqual(expected,PathGoalAssignmentKernel.ShouldRestoreDesiredGoal(assigned,start,goal,8,8));
                var output=expected?PathGoalAssignmentKernel.RestoreDesiredGoal(assigned,goal,surface):assigned;
                Assert.AreEqual(expected&&surface?goal:assigned,output);
            }
        }
        [BurstCompile(CompileSynchronously=true)] private struct HashJob:IJob
        {
            public NativeArray<int> Output;
            [BurstDiscard] private static void ManagedOnly(ref bool value){value=false;}
            public void Execute()
            {
                bool burst=true;ManagedOnly(ref burst);Output[0]=burst?1:0;int differences=0;
                for(int i=0;i<8192;i++)
                {
                    int x=unchecked(i*1283419601),z=unchecked(i*1210229737+int.MinValue),start=unchecked(i*1664525+1013904223);
                    if(math.hash(new int3(x,z,start))!=PathGoalAssignmentKernel.HashDesiredGoal(x,z,start))differences++;
                }
                for(int radius=1;radius<=20;radius++)for(int step=0;step<8*radius;step++)
                {var a=PathGoalAssignmentKernel.SquareRingOffset(radius,step);var b=MultiUnitGoalOriginalAssignment.SquareRingOffset(radius,step);if(a.x!=b.x||a.y!=b.y)differences++;}
                Output[1]=differences;
            }
        }
        [Test] public void InstalledHashAndRingMatchManagedAndBurst()
        {
            using var output=new NativeArray<int>(2,Allocator.TempJob);var job=new HashJob{Output=output};job.Execute();Assert.AreEqual(0,output[0]);Assert.AreEqual(0,output[1]);job.Schedule().Complete();Assert.AreEqual(1,output[0]);Assert.AreEqual(0,output[1]);
        }
    }
}
#endif
