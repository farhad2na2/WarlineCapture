using System;
using System.IO;
using System.Security.Cryptography;
using Game.Components;
using Game.Runtime;
using Game.Runtime.Pathfinding;
using NUnit.Framework;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Game.Tests.Editor
{
    public sealed class PathJobSchedulingKernelTests
    {
        public static int RunChecks()
        {
            var t=new PathJobSchedulingKernelTests();t.OriginalSourcePinned();t.AdmissionMatchesOriginalManagedAndBurst();t.NativeInactivePublishesWithoutReadingGrid();
            var native=new global::UnitPathfindingFocusedPerformanceValidation();
            native.ManualGroupAndLongDistanceRequestsCompleteWithoutPathfindingDiagnostics();
            native.MixedInfantryVehicleGroupPathingCompletes();
            Debug.Log("[PathJobSchedulingParity] result=Passed tests=5 nativeRegressions=3 managed=Passed burst=Passed missionReady=0");return 5;
        }
        [Test] public void OriginalSourcePinned()
        {
            using var sha=SHA256.Create();var bytes=System.Text.Encoding.UTF8.GetBytes(File.ReadAllText(Path.Combine(Application.dataPath,"Tests/Editor/PathJobSchedulingOriginalSource.cs.txt")).Replace("\r\n","\n"));
            Assert.AreEqual("6d8e002bbb96864203391a96ded83679d38236029aa4326150ecccbfdd5d2e16",BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant());
        }
        private static PathJobAdmission Original(bool play,bool pending,bool complete)
        {
            if(!play) return pending?PathJobAdmission.CancelPending:PathJobAdmission.Inactive;
            if(pending)
            {
                if(!complete)return PathJobAdmission.WaitPending;
                return PathJobAdmission.ApplyThenSchedule;
            }
            return PathJobAdmission.Schedule;
        }
        [BurstCompile(CompileSynchronously=true)] private struct PairJob:IJob
        {
            public NativeArray<PathJobAdmission> Values;
            public NativeArray<byte> Managed;
            [BurstDiscard] private static void MarkManaged(ref byte value){value=1;}
            public void Execute()
            {
                byte managed=0;MarkManaged(ref managed);Managed[0]=managed;
                for(int i=0;i<8;i++)
                {
                    bool play=(i&1)!=0,pending=(i&2)!=0,complete=(i&4)!=0;
                    Values[2*i]=Original(play,pending,complete);
                    Values[2*i+1]=PathJobSchedulingKernel.Decide(play,pending,complete);
                }
            }
        }
        [Test] public void AdmissionMatchesOriginalManagedAndBurst()
        {
            using var values=new NativeArray<PathJobAdmission>(16,Allocator.TempJob);using var managed=new NativeArray<byte>(1,Allocator.TempJob);
            var job=new PairJob{Values=values,Managed=managed};job.Execute();Assert.AreEqual(1,managed[0]);
            for(int i=0;i<8;i++)Assert.AreEqual(values[2*i],values[2*i+1]);
            job.Schedule().Complete();Assert.AreEqual(0,managed[0],"Actual Burst required");for(int i=0;i<8;i++)Assert.AreEqual(values[2*i],values[2*i+1]);
            // Cancellation wins even when an external worker has completed.
            Assert.AreEqual(PathJobAdmission.CancelPending,PathJobSchedulingKernel.Decide(false,true,true));
            Assert.AreEqual(PathJobAdmission.WaitPending,PathJobSchedulingKernel.Decide(true,true,false));
            Assert.AreEqual(PathJobAdmission.ApplyThenSchedule,PathJobSchedulingKernel.Decide(true,true,true));
        }
        [Test] public void NativeInactivePublishesWithoutReadingGrid()
        {
            using var world=new World("NativeInactivePathScheduling");var em=world.EntityManager;
            em.SetComponentData(em.CreateEntity(typeof(RuntimeGameplayStateComponent)),new RuntimeGameplayStateComponent{PlayRequested=0});
            // Satisfy every RequireForUpdate so the test exercises OnUpdate. The grid's
            // native storage intentionally remains uninitialized: inactive admission must
            // publish state and return before any path/grid snapshot is read.
            var grid=em.CreateEntity(typeof(GridConfig),typeof(DynamicBlockerComponent),typeof(DynamicOccupancyComponent));
            em.AddBuffer<GridRoad>(grid);em.AddBuffer<GridRoadSidewalk>(grid);em.AddBuffer<GridRoadDirt>(grid);
            var system=world.CreateSystem<UnitPathfindingSystem>();
            using var query=em.CreateEntityQuery(typeof(UnitPathfindingPendingStateComponent));
            Assert.AreEqual(1,query.CalculateEntityCount());
            em.SetComponentData(query.GetSingletonEntity(),new UnitPathfindingPendingStateComponent{HasPendingPathJob=1,RequestCount=99});
            system.Update(world.Unmanaged);em.CompleteAllTrackedJobs();
            var published=query.GetSingleton<UnitPathfindingPendingStateComponent>();
            Assert.AreEqual(0,published.HasPendingPathJob);Assert.AreEqual(0,published.RequestCount,"OnUpdate must overwrite the sentinel");
        }
    }
}
