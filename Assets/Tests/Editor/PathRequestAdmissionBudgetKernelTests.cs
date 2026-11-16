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
using Unity.Mathematics;
using UnityEngine;
namespace Game.Tests.Editor
{
    public sealed class PathRequestAdmissionBudgetKernelTests
    {
        public static int RunChecks()
        {
            var tests=new PathRequestAdmissionBudgetKernelTests();
            tests.OriginalSourcesPinned();tests.BudgetTraceManagedAndBurst();tests.AdmissionBoundariesManagedAndBurst();tests.NativeCollectionMatchesOriginal();
            Debug.Log("[PathRequestAdmissionBudgetParity] result=Passed checks=4 budgetCases=8192 nativeCollection=Passed burst=Passed missionReady=0");return 4;
        }
        [Test] public void OriginalSourcesPinned()
        {
            string[] names={"UnitPathfindingBudget","UnitPathRequestCollection"};
            string[] hashes={"27980673e9b6453a95fab9e6c711c086b108c7697a0e9e85c4f25b42df75e1d7","29cb0dbbbd2baba149b5790a4b733df84440956aebfe18fa9570d8e7fe2be369"};
            using var sha=SHA256.Create();
            for(int i=0;i<names.Length;i++)Assert.AreEqual(hashes[i],BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(File.ReadAllText(Path.Combine(Application.dataPath,"Tests/Editor/"+names[i]+"Original.cs.txt")).Replace("\r\n","\n")))).Replace("-","").ToLowerInvariant());
        }
        private struct BudgetInput {public int Operation,Frame,ScheduleFrame,Budget,Frames,Count,Manual,Vehicles;public double Wall;}
        private struct BudgetOutput {public PathRequestBudgetState Expected,Actual;public int NativeAdaptive,NativeCurrent,ExpectedCurrent;}
        [BurstCompile(CompileSynchronously=true)] private struct BudgetJob:IJob
        {
            [ReadOnly]public NativeArray<BudgetInput> Inputs;public NativeArray<BudgetOutput> Outputs;public NativeArray<int> Managed;
            [BurstDiscard]private static void MarkManaged(ref int value){value=1;}
            public void Execute()
            {
                int flag=0;MarkManaged(ref flag);Managed[0]=flag;
                var original=new OriginalBudget();var native=new UnitPathfindingBudget();var shared=new PathRequestBudgetState();
                original.Initialize();native.Initialize();PathRequestBudgetKernel.Initialize(ref shared);
                for(int i=0;i<Inputs.Length;i++)
                {
                    var x=Inputs[i];
                    if(x.Operation==0){original.Initialize();native.Initialize();PathRequestBudgetKernel.Initialize(ref shared);}
                    else if(x.Operation==1){original.ResetPendingJobReduction();native.ResetPendingJobReduction();PathRequestBudgetKernel.ResetPendingJobReduction(ref shared);}
                    else if(x.Operation==2){original.ReduceForPendingJob(x.Frame,x.ScheduleFrame,x.Budget);native.ReduceForPendingJob(x.Frame,x.ScheduleFrame,x.Budget);PathRequestBudgetKernel.ReduceForPendingJob(ref shared,x.Frame,x.ScheduleFrame,x.Budget);}
                    else {original.ReportCompletedJob(x.Frames,x.Wall,x.Count,x.Manual,x.Vehicles,x.Budget);native.ReportCompletedJob(x.Frames,x.Wall,x.Count,x.Manual,x.Vehicles,x.Budget);PathRequestBudgetKernel.ReportCompletedJob(ref shared,x.Frames,x.Wall,x.Count,x.Manual,x.Vehicles,x.Budget);}
                    Outputs[i]=new BudgetOutput{Expected=original.State,Actual=shared,NativeAdaptive=native.AdaptiveRequestsPerFrame,NativeCurrent=native.GetCurrentRequestBudget(),ExpectedCurrent=original.GetCurrentRequestBudget()};
                }
            }
        }
        private static void VerifyBudget(NativeArray<BudgetOutput> values)
        {
            for(int i=0;i<values.Length;i++){var x=values[i];Assert.AreEqual(x.Expected.AdaptiveRequestsPerFrame,x.Actual.AdaptiveRequestsPerFrame,"adaptive "+i);Assert.AreEqual(x.Expected.StableOneFrameBatchCount,x.Actual.StableOneFrameBatchCount,"stable "+i);Assert.AreEqual(x.Expected.PendingBudgetReduced,x.Actual.PendingBudgetReduced,"reduced "+i);Assert.AreEqual(x.Expected.AdaptiveRequestsPerFrame,x.NativeAdaptive);Assert.AreEqual(x.ExpectedCurrent,x.NativeCurrent);}
        }
        [Test]public void BudgetTraceManagedAndBurst()
        {
            using var owner=new NativeArray<BudgetInput>(8192,Allocator.TempJob);var inputs=owner;
            using var outputs=new NativeArray<BudgetOutput>(inputs.Length,Allocator.TempJob);using var managed=new NativeArray<int>(1,Allocator.TempJob);
            double[] times={-1,0,.000001,.005999999999,.006,.006000000001,.007,.011999999999,.012,.012000000001,.049999999999,.05,.050000000001,double.NaN,double.PositiveInfinity};
            var random=new System.Random(78249);
            for(int i=0;i<inputs.Length;i++){int count=random.Next(0,34);inputs[i]=new BudgetInput{Operation=i%101==0?0:i%11==0?1:i%7==0?2:3,Frame=i%5,ScheduleFrame=2,Budget=random.Next(1,33),Frames=i%3,Count=count,Manual=i%2==0?count:random.Next(0,count+1),Vehicles=i%5==0?1:0,Wall=times[i%times.Length]};}
            var job=new BudgetJob{Inputs=inputs,Outputs=outputs,Managed=managed};job.Execute();Assert.AreEqual(1,managed[0]);VerifyBudget(outputs);job.Schedule().Complete();Assert.AreEqual(0,managed[0],"Actual Burst required");VerifyBudget(outputs);
            // Stable full manual batches grow to four, while waiting reduces at most once until reset.
            var state=new PathRequestBudgetState();PathRequestBudgetKernel.Initialize(ref state);
            for(int i=0;i<12;i++){int n=PathRequestBudgetKernel.GetCurrentRequestBudget(state);PathRequestBudgetKernel.ReportCompletedJob(ref state,1,.001,n,n,0,n);}
            Assert.AreEqual(4,state.AdaptiveRequestsPerFrame);PathRequestBudgetKernel.ReduceForPendingJob(ref state,2,1,4);Assert.AreEqual(2,state.AdaptiveRequestsPerFrame);
            PathRequestBudgetKernel.ReduceForPendingJob(ref state,3,1,2);Assert.AreEqual(2,state.AdaptiveRequestsPerFrame);PathRequestBudgetKernel.ResetPendingJobReduction(ref state);PathRequestBudgetKernel.ReduceForPendingJob(ref state,3,1,2);Assert.AreEqual(1,state.AdaptiveRequestsPerFrame);
            var previous=UnitPathfindingBudget.CompletionObserved;
            try
            {
                int calls=0;PathRequestBudgetCompletionObservation observed=default;
                UnitPathfindingBudget.CompletionObserved=value=>{calls++;observed=value;};
                var native=new UnitPathfindingBudget();native.Initialize();native.ReportCompletedJob(3,.023456789,7,2,1,9);
                Assert.AreEqual(1,calls);Assert.AreEqual(3,observed.PendingFrames);Assert.AreEqual(.023456789,observed.PendingWallTime);
                Assert.AreEqual(7,observed.RequestCount);Assert.AreEqual(2,observed.ManualRequestCount);Assert.AreEqual(1,observed.VehicleLikeCount);Assert.AreEqual(9,observed.PendingRequestBudget);
                Assert.AreEqual(1,observed.Before.AdaptiveRequestsPerFrame);Assert.AreEqual(4,observed.After.AdaptiveRequestsPerFrame);Assert.AreEqual(native.AdaptiveRequestsPerFrame,observed.After.AdaptiveRequestsPerFrame);
                job.Schedule().Complete();Assert.AreEqual(0,managed[0]);Assert.AreEqual(1,calls,"Burst parity must not execute the editor observer");VerifyBudget(outputs);
            }
            finally {UnitPathfindingBudget.CompletionObserved=previous;}
        }
        [BurstCompile(CompileSynchronously=true)]private struct AdmissionJob:IJob
        {
            public NativeArray<int> Results;public NativeArray<int> Managed;
            [BurstDiscard]private static void MarkManaged(ref int value){value=1;}
            public void Execute()
            {
                int flag=0;MarkManaged(ref flag);Managed[0]=flag;int at=0;
                for(int budget=-1;budget<=8;budget++)for(int manualCount=0;manualCount<=3;manualCount++)
                {
                    int expectedCount=0,expectedCode=0;
                    for(int i=0;i<manualCount;i++){expectedCode=expectedCode*10+i+1;expectedCount++;if(expectedCount>=budget)break;}
                    if(expectedCount<budget)for(int i=0;i<6;i++){if(i%2==0)continue;expectedCode=expectedCode*10+i+1;expectedCount++;if(expectedCount>=budget)break;}
                    var state=PathRequestAdmissionKernel.Initialize(budget);int actualCode=0;
                    while(PathRequestAdmissionKernel.TryBeginPass(ref state,out bool manual))for(int i=0;i<(manual?manualCount:6);i++)
                    {if(!PathRequestAdmissionKernel.IncludeCandidate(manual,!manual&&i%2==0))continue;actualCode=actualCode*10+i+1;if(PathRequestAdmissionKernel.RecordAppend(ref state))break;}
                    Results[at++]=expectedCode;Results[at++]=actualCode;Results[at++]=expectedCount;Results[at++]=state.CollectedCount;
                }
            }
        }
        [Test]public void AdmissionBoundariesManagedAndBurst()
        {
            using var results=new NativeArray<int>(160,Allocator.TempJob);using var managed=new NativeArray<int>(1,Allocator.TempJob);var job=new AdmissionJob{Results=results,Managed=managed};
            void Check(){for(int i=0;i<results.Length;i+=2)Assert.AreEqual(results[i],results[i+1],"admission pair "+i);}
            job.Execute();Assert.AreEqual(1,managed[0]);Check();job.Schedule().Complete();Assert.AreEqual(0,managed[0],"Actual Burst required");Check();
        }
        [DisableAutoCreation]public partial class CollectionHarness:SystemBase
        {
            internal UnitPathfindingEntitySets Queries;
            internal UnitPathRequestCollection Current;
            internal OriginalCollection Original;
            internal UnitPathRequestBuffer CurrentBuffer,OriginalBuffer;
            internal UnitPathIgnoredOccupancy Ignored;
            protected override void OnCreate(){Queries.Initialize(ref CheckedStateRef);Current.Initialize(ref CheckedStateRef);Original.Initialize(ref CheckedStateRef);CurrentBuffer.Initialize();OriginalBuffer.Initialize();}
            protected override void OnUpdate(){}
            protected override void OnDestroy(){CurrentBuffer.Dispose();OriginalBuffer.Dispose();}
        }
        [Test]public void NativeCollectionMatchesOriginal()
        {
            using var world=new World("PathRequestAdmissionNativeParity");var em=world.EntityManager;
            var harness=world.GetOrCreateSystemManaged<CollectionHarness>();
            for(int i=0;i<12;i++)
            {
                var e=em.CreateEntity(typeof(UnitGrid),typeof(UnitPathRequest),typeof(UnitFootprint),typeof(UnitMovementBehavior),typeof(Faction));
                em.SetComponentData(e,new UnitGrid{Cell=new int2(i,2)});em.SetComponentData(e,new UnitPathRequest{Goal=new int2(i,10)});em.SetComponentData(e,new UnitFootprint{Size=new int2(i%2+1,1)});em.SetComponentData(e,new Faction{Id=(byte)(i%3+1)});
                if(i%2==0)em.AddComponent<ManualMoveOrderTag>(e);if(i%3==0)em.AddComponent<UnitLongDistanceMove>(e);if(i==2||i==3)em.AddComponent<StaticGridBlocker>(e);if(i==4||i==5)em.AddComponent<UnitAirMovement>(e);if(i==6)em.AddComponent<EngageTarget>(e);if(i==8)em.AddComponent<UnitDeathAnimationComponent>(e);
            }
            foreach(int budget in new[]{-1,0,1,2,4,32})
            {
                harness.Current.Collect(ref harness.CheckedStateRef,ref harness.Queries,ref harness.CurrentBuffer,ref harness.Ignored,budget);
                harness.Original.Collect(ref harness.CheckedStateRef,ref harness.Queries,ref harness.OriginalBuffer,ref harness.Ignored,budget);
                var a=harness.CurrentBuffer;var b=harness.OriginalBuffer;Assert.AreEqual(b.Entities.Length,a.Entities.Length);
                for(int i=0;i<a.Entities.Length;i++){Assert.AreEqual(b.Entities[i],a.Entities[i]);Assert.AreEqual(b.UnitGrids[i].Cell,a.UnitGrids[i].Cell);Assert.AreEqual(b.Goals[i].Goal,a.Goals[i].Goal);Assert.AreEqual(b.Footprints[i].Size,a.Footprints[i].Size);Assert.AreEqual(b.MovementBehaviors[i],a.MovementBehaviors[i]);Assert.AreEqual(b.Factions[i],a.Factions[i]);Assert.AreEqual(b.ManualMoves[i],a.ManualMoves[i]);Assert.AreEqual(b.ContinuationMoves[i],a.ContinuationMoves[i]);Assert.AreEqual(b.IgnoredOccupancyEntities[i],a.IgnoredOccupancyEntities[i]);Assert.AreEqual(b.IgnoredOccupancyCells[i],a.IgnoredOccupancyCells[i]);Assert.AreEqual(b.IgnoredOccupancySizes[i],a.IgnoredOccupancySizes[i]);}
            }
        }
    }
}
namespace Game.Tests.Editor
{
    internal struct OriginalBudget
    {
        public const int MaxRequestsPerFrame = 32;
        public const int MinRequestsPerFrame = 1;

        private const double FreezeLogThresholdSeconds = 0.05d;
        private const double TargetPathJobWallSeconds = 0.008d;
        private const double LowPathJobWallSeconds = 0.006d;
        private const double HighPathJobWallSeconds = 0.012d;
        private const int MaxManualInfantryRequestsPerFrame = 4;
        private const int StableManualInfantryBatchesBeforeIncrease = 2;
        private const int StableOneFrameBatchesBeforeIncrease = 3;

        private int _adaptiveRequestsPerFrame;
        private int _stableOneFrameBatchCount;
        private bool _pendingBudgetReduced;

        public int AdaptiveRequestsPerFrame => _adaptiveRequestsPerFrame;
        public PathRequestBudgetState State=>new PathRequestBudgetState{AdaptiveRequestsPerFrame=_adaptiveRequestsPerFrame,StableOneFrameBatchCount=_stableOneFrameBatchCount,PendingBudgetReduced=_pendingBudgetReduced};

        public void Initialize()
        {
            _adaptiveRequestsPerFrame = MinRequestsPerFrame;
            _stableOneFrameBatchCount = 0;
            _pendingBudgetReduced = false;
        }

        public int GetCurrentRequestBudget()
        {
            return math.clamp(_adaptiveRequestsPerFrame, MinRequestsPerFrame, MaxRequestsPerFrame);
        }

        public void ResetPendingJobReduction()
        {
            _pendingBudgetReduced = false;
        }

        public void ReduceForPendingJob(int frameCount, int pendingScheduleFrame, int pendingRequestBudget)
        {
            if (_pendingBudgetReduced || frameCount <= pendingScheduleFrame)
                return;

            _adaptiveRequestsPerFrame = math.max(MinRequestsPerFrame, pendingRequestBudget / 2);
            _stableOneFrameBatchCount = 0;
            _pendingBudgetReduced = true;
        }

        public void ReportCompletedJob(
            int pendingFrames,
            double pendingWallTime,
            int requestCount,
            int manualRequestCount,
            int vehicleLikeCount,
            int pendingRequestBudget)
        {
            if (requestCount <= 0)
                return;

            bool allManualInfantry = manualRequestCount == requestCount && vehicleLikeCount == 0;
            if (pendingFrames > 1 || pendingWallTime >= FreezeLogThresholdSeconds)
            {
                _adaptiveRequestsPerFrame = math.max(MinRequestsPerFrame, pendingRequestBudget / 2);
                _stableOneFrameBatchCount = 0;
                return;
            }

            if (allManualInfantry)
            {
                if (requestCount >= pendingRequestBudget)
                {
                    _stableOneFrameBatchCount++;
                    if (_stableOneFrameBatchCount >= StableManualInfantryBatchesBeforeIncrease)
                    {
                        _adaptiveRequestsPerFrame = math.min(MaxManualInfantryRequestsPerFrame, pendingRequestBudget + 1);
                        _stableOneFrameBatchCount = 0;
                    }
                    else
                    {
                        _adaptiveRequestsPerFrame = math.max(_adaptiveRequestsPerFrame, pendingRequestBudget);
                    }
                }
                else
                {
                    _stableOneFrameBatchCount = 0;
                    _adaptiveRequestsPerFrame = math.max(MinRequestsPerFrame, math.min(_adaptiveRequestsPerFrame, pendingRequestBudget));
                }
                return;
            }

            if (pendingWallTime >= HighPathJobWallSeconds)
            {
                _adaptiveRequestsPerFrame = math.max(MinRequestsPerFrame, pendingRequestBudget / 2);
                _stableOneFrameBatchCount = 0;
                return;
            }

            int targetBudget = math.clamp(
                (int)math.floor(TargetPathJobWallSeconds / math.max(1e-6d, pendingWallTime / requestCount)),
                MinRequestsPerFrame,
                MaxRequestsPerFrame);

            if (pendingWallTime <= LowPathJobWallSeconds && requestCount >= pendingRequestBudget)
            {
                _stableOneFrameBatchCount++;
                if (_stableOneFrameBatchCount >= StableOneFrameBatchesBeforeIncrease)
                {
                    _adaptiveRequestsPerFrame = math.min(targetBudget, pendingRequestBudget + 1);
                    _stableOneFrameBatchCount = 0;
                }
                else
                {
                    _adaptiveRequestsPerFrame = math.min(_adaptiveRequestsPerFrame, targetBudget);
                }
            }
            else
            {
                _stableOneFrameBatchCount = 0;
                _adaptiveRequestsPerFrame = math.min(pendingRequestBudget, targetBudget);
            }
        }
    }
}

namespace Game.Tests.Editor
{
    internal struct OriginalCollection
    {
        private EntityTypeHandle _entityType;
        private ComponentTypeHandle<UnitGrid> _unitGridType;
        private ComponentTypeHandle<UnitPathRequest> _requestType;
        private ComponentTypeHandle<UnitFootprint> _footprintType;
        private ComponentTypeHandle<UnitMovementBehavior> _movementBehaviorType;
        private ComponentTypeHandle<Faction> _factionType;
        private ComponentLookup<UnitLongDistanceMove> _longDistanceLookup;
        private ComponentLookup<ManualMoveOrderTag> _manualMoveLookup;

        public void Initialize(ref SystemState state)
        {
            _entityType = state.GetEntityTypeHandle();
            _unitGridType = state.GetComponentTypeHandle<UnitGrid>(true);
            _requestType = state.GetComponentTypeHandle<UnitPathRequest>(true);
            _footprintType = state.GetComponentTypeHandle<UnitFootprint>(true);
            _movementBehaviorType = state.GetComponentTypeHandle<UnitMovementBehavior>(true);
            _factionType = state.GetComponentTypeHandle<Faction>(true);
            _longDistanceLookup = state.GetComponentLookup<UnitLongDistanceMove>(true);
            _manualMoveLookup = state.GetComponentLookup<ManualMoveOrderTag>(true);
        }

        public int Collect(
            ref SystemState state,
            ref UnitPathfindingEntitySets queries,
            ref UnitPathRequestBuffer requestBuffers,
            ref UnitPathIgnoredOccupancy ignoredOccupancy,
            int requestBudget)
        {
            requestBuffers.ClearForCollection();
            Update(ref state);

            CollectFromQuery(
                ref state,
                queries.ManualRequestQuery,
                ref requestBuffers,
                ref ignoredOccupancy,
                requestBudget,
                _entityType,
                _unitGridType,
                _requestType,
                _footprintType,
                _movementBehaviorType,
                _factionType,
                _longDistanceLookup,
                _manualMoveLookup,
                manualMove: true,
                skipManualMoveEntities: false);

            if (requestBuffers.Entities.Length < requestBudget)
            {
                CollectFromQuery(
                    ref state,
                    queries.RequestQuery,
                    ref requestBuffers,
                    ref ignoredOccupancy,
                    requestBudget,
                    _entityType,
                    _unitGridType,
                    _requestType,
                    _footprintType,
                    _movementBehaviorType,
                    _factionType,
                    _longDistanceLookup,
                    _manualMoveLookup,
                    manualMove: false,
                    skipManualMoveEntities: true);
            }

            return requestBuffers.Entities.Length;
        }

        private void Update(ref SystemState state)
        {
            _entityType.Update(ref state);
            _unitGridType.Update(ref state);
            _requestType.Update(ref state);
            _footprintType.Update(ref state);
            _movementBehaviorType.Update(ref state);
            _factionType.Update(ref state);
            _longDistanceLookup.Update(ref state);
            _manualMoveLookup.Update(ref state);
        }

        private static void CollectFromQuery(
            ref SystemState state,
            EntityQuery query,
            ref UnitPathRequestBuffer requestBuffers,
            ref UnitPathIgnoredOccupancy ignoredOccupancy,
            int requestBudget,
            EntityTypeHandle entityType,
            ComponentTypeHandle<UnitGrid> unitGridType,
            ComponentTypeHandle<UnitPathRequest> requestType,
            ComponentTypeHandle<UnitFootprint> footprintType,
            ComponentTypeHandle<UnitMovementBehavior> movementBehaviorType,
            ComponentTypeHandle<Faction> factionType,
            ComponentLookup<UnitLongDistanceMove> longDistanceLookup,
            ComponentLookup<ManualMoveOrderTag> manualMoveLookup,
            bool manualMove,
            bool skipManualMoveEntities)
        {
            using NativeArray<ArchetypeChunk> chunks = query.ToArchetypeChunkArray(Allocator.Temp);
            for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
            {
                ArchetypeChunk chunk = chunks[chunkIndex];
                NativeArray<Entity> entities = chunk.GetNativeArray(entityType);
                NativeArray<UnitGrid> unitGrids = chunk.GetNativeArray(ref unitGridType);
                NativeArray<UnitPathRequest> requests = chunk.GetNativeArray(ref requestType);
                NativeArray<UnitFootprint> footprints = chunk.GetNativeArray(ref footprintType);
                NativeArray<UnitMovementBehavior> movementBehaviors = chunk.GetNativeArray(ref movementBehaviorType);
                NativeArray<Faction> factions = chunk.GetNativeArray(ref factionType);

                for (int i = 0; i < entities.Length; i++)
                {
                    Entity entity = entities[i];
                    if (skipManualMoveEntities && manualMoveLookup.HasComponent(entity))
                        continue;

                    requestBuffers.Entities.Add(entity);
                    requestBuffers.UnitGrids.Add(unitGrids[i]);
                    requestBuffers.Goals.Add(requests[i]);
                    requestBuffers.Footprints.Add(footprints[i]);
                    requestBuffers.MovementBehaviors.Add(movementBehaviors[i]);
                    requestBuffers.Factions.Add(factions[i].Id);
                    requestBuffers.ManualMoves.Add((byte)(manualMove ? 1 : 0));
                    ignoredOccupancy.AddForRequest(ref state, ref requestBuffers, entity);
                    requestBuffers.ContinuationMoves.Add((byte)(longDistanceLookup.HasComponent(entity) ? 1 : 0));

                    if (requestBuffers.Entities.Length >= requestBudget)
                        return;
                }
            }
        }
    }
}
