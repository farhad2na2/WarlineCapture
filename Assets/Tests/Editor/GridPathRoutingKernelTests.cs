#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using System;
using System.Collections.Generic;
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
    public sealed class GridPathRoutingKernelTests
    {
        public static int RunChecks()
        {
            var tests = new GridPathRoutingKernelTests();
            tests.PreviousPublishedOuterPolicyIsPinned();
            tests.SeededOuterPolicyAndUntouchedMetadataMatchOriginal();
            tests.NamedAlternatesLimitsAndManualFallbackMatchOriginal();
            tests.DetailedHaulerUsesOriginalNativeBudgetAndWeightedRetry();
            tests.ExplicitBudgetAttemptTracePreservesEpochAndWeight();
            Debug.Log("[GridPathRoutingParity] result=Passed tests=5 missionReady=0");
            return 5;
        }
        public static void RunCombinedValidation()
        {
            try
            {
                RunChecks();
                Assert.AreEqual(13,SkirmishMissionOrchestrationKernelTests.RunChecks());
                GridPathSearchKernelTests.RunChecks();GridOccupancyKernelTests.RunChecks();Assert.AreEqual(3,PathPlacementKernelTests.RunChecks());
                GridPathMovementKernelTests.RunChecks();DirectDamageSequencerTests.RunChecks();
                var attack=new StandardAttackPlannerTests();attack.OriginalOracleIsPinnedVerbatimProduction();attack.InstalledMathNaNEqualAndClampSemanticsMatchKernel();attack.ManagedRandomAndBoundaryPlansMatchEveryField();attack.BurstScheduledPlansMatchOriginalAndManagedResults();attack.ActualPrivateRuntimeAdapterPreservesEveryPlanField();
                var combat=new global::UnitCombatFocusedEditModeTests();combat.StandardAttack_RespectsAuthoredDomainAndVisibilityPolicy();combat.StandardAttack_NonLethalHitDamagesTargetAndRecordsFeedbackState();combat.StandardAttack_CooldownRecoversAcrossLostEngagementAndCanFireOnReacquisition();combat.StandardAttack_NoEngagementRecoverySkipsIneligibleActorsAndDoesNotDoubleTick();combat.AttackVfxRequestResolution_PopulatesMuzzlePlaybackData();combat.AttackVfxRequestResolution_PopulatesImpactPlaybackData();
                Debug.Log("[SharedRoutingMissionParity] result=Passed routing=5 mission=13 priorNavigationCombat=41 missionReady=0");ValidationExit.Passed();
            }
            catch(Exception exception){Debug.LogException(exception);Debug.LogError("[SharedRoutingMissionParity] result=Failed missionReady=0");ValidationExit.Failed();}
        }
        public static void RunFocusedValidation()
        {
            try { RunChecks(); ValidationExit.Passed(); }
            catch(Exception exception) {Debug.LogException(exception);Debug.LogError("[GridPathRoutingParity] result=Failed missionReady=0");ValidationExit.Failed();}
        }
        [Test] public void PreviousPublishedOuterPolicyIsPinned()
        {
            string path=Path.Combine(Application.dataPath,"Tests/Editor/GridPathRoutingOriginalSource.cs.txt");
            string original=File.ReadAllText(path).Replace("\r\n","\n");using var sha=SHA256.Create();
            Assert.AreEqual("704c0fa39a675026397e65e5d082a542de1c720e2f724dd62e1fe5f455fd5510",BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(original))).Replace("-","").ToLowerInvariant());
            string expected="#if UNITY_EDITOR\n"+original.Replace("PathfindBatchJob","GridPathRoutingOriginalOracle")+"\n#endif\n";
            Assert.AreEqual(expected,File.ReadAllText(Path.Combine(Application.dataPath,"Game/Scripts/Systems/Pathfinding/GridPathRoutingOriginalOracle.cs")).Replace("\r\n","\n"));
        }
        private sealed class Fixture : IDisposable
        {
            public PathfindBatchJob Job;
            private readonly List<IDisposable> owners = new List<IDisposable>();
            private NativeStream stream;
            private bool hasStream;
            public readonly int Width;
            private T Own<T>(T value) where T : struct, IDisposable { owners.Add(value); return value; }
            public Fixture(int width)
            {
                Width = width; int size = width * width;
                Job = new PathfindBatchJob { Grid = new GridConfig { Width = width, Height = width, CellSize = 1 }, GridSize = size,
                    Walkable = Own(new NativeArray<GridWalkable>(size, Allocator.TempJob)),
                    Roads = Own(new NativeArray<GridRoad>(size, Allocator.TempJob)),
                    Sidewalks = Own(new NativeArray<GridRoadSidewalk>(size, Allocator.TempJob)),
                    DirtRoads = Own(new NativeArray<GridRoadDirt>(size, Allocator.TempJob)),
                    DynamicBlocked = Own(new NativeBitArray(size, Allocator.TempJob)),
                    Occupied = Own(new NativeBitArray(size, Allocator.TempJob)),
                    FriendlyPassFactionIds = Own(new NativeArray<byte>(size, Allocator.TempJob)),
                    LiveUnitEntities = Own(new NativeArray<Entity>(4, Allocator.TempJob)),
                    LiveUnitGrids = Own(new NativeArray<UnitGrid>(4, Allocator.TempJob)),
                    LiveUnitFootprints = Own(new NativeArray<UnitFootprint>(4, Allocator.TempJob)),
                    LiveUnitManualGroupMembers = Own(new NativeArray<byte>(4, Allocator.TempJob)),
                    Entities = Own(new NativeArray<Entity>(1, Allocator.TempJob)),
                    UnitGrids = Own(new NativeArray<UnitGrid>(1, Allocator.TempJob)),
                    Footprints = Own(new NativeArray<UnitFootprint>(1, Allocator.TempJob)),
                    MovementBehaviors = Own(new NativeArray<UnitMovementBehavior>(1, Allocator.TempJob)),
                    Factions = Own(new NativeArray<byte>(1, Allocator.TempJob)),
                    ManualMoves = Own(new NativeArray<byte>(1, Allocator.TempJob)),
                    IgnoredOccupancyEntities = Own(new NativeArray<Entity>(1, Allocator.TempJob)),
                    IgnoredOccupancyCells = Own(new NativeArray<int2>(1, Allocator.TempJob)),
                    IgnoredOccupancySizes = Own(new NativeArray<int2>(1, Allocator.TempJob)),
                    RequestedGoals = Own(new NativeArray<UnitPathRequest>(1, Allocator.TempJob)),
                    Goals = Own(new NativeArray<int2>(1, Allocator.TempJob)),
                    Segmented = Own(new NativeArray<byte>(1, Allocator.TempJob)),
                    Status = Own(new NativeArray<byte>(1, Allocator.TempJob)),
                    FailureCodes = Own(new NativeArray<int>(1, Allocator.TempJob)),
                    ExpansionCounts = Own(new NativeArray<int>(1, Allocator.TempJob)),
                    CheapSegmentModes = Own(new NativeArray<byte>(1, Allocator.TempJob)),
                    AlternateSearchSkipped = Own(new NativeArray<byte>(1, Allocator.TempJob)),
                    AlternateAttempts = Own(new NativeArray<int>(1, Allocator.TempJob)),
                    ScratchCameFrom = Own(new NativeArray<int>(size, Allocator.TempJob)),
                    ScratchGScore = Own(new NativeArray<int>(size, Allocator.TempJob)),
                    ScratchClosed = Own(new NativeArray<byte>(size, Allocator.TempJob)),
                    ScratchInOpen = Own(new NativeArray<byte>(size, Allocator.TempJob)),
                    ScratchEpoch = Own(new NativeArray<int>(size, Allocator.TempJob)),
                    ScratchOpen = Own(new NativeArray<long>(size, Allocator.TempJob)),
                    ScratchPath = Own(new NativeArray<int>(size, Allocator.TempJob)) };
            }
            public void Load(int seed, int scenario, int epoch)
            {
                var random = new System.Random(seed); int size = Width * Width;
                for (int i = 0; i < size; i++)
                {
                    Job.Walkable[i] = new GridWalkable { Value = (byte)(scenario == 0 && random.Next(11) == 0 ? 0 : 1) };
                    Job.DynamicBlocked.Set(i, scenario == 0 && random.Next(15) == 0); Job.Occupied.Set(i, false);
                    Job.FriendlyPassFactionIds[i] = (byte)(scenario == 0 && random.Next(4) == 0 ? 1 : 255);
                    Job.Roads[i] = default; Job.Sidewalks[i] = new GridRoadSidewalk { Value = (byte)(scenario == 0 && random.Next(5) == 0 ? 1 : 0) };
                    Job.DirtRoads[i] = new GridRoadDirt { Value = (byte)(scenario == 0 && random.Next(6) == 0 ? 1 : 0) };
                }
                if (scenario == 11) for (int i=0;i<size;i++) Job.Walkable[i]=default;
                int2 start = scenario == 0 ? new int2(random.Next(2, Width-2), random.Next(2, Width-2)) : new int2(3, Width/2);
                int2 goal = scenario == 0 ? new int2(random.Next(2, Width-2), random.Next(2, Width-2)) : new int2(Width-4, Width/2);
                bool vehicle = scenario == 0 && seed % 3 == 0;
                if (scenario == 2) start = new int2(-1, 3);
                if (scenario == 3) goal = new int2(-1, Width+1);
                if (scenario == 4) goal = start;
                if (GridUtils.InBounds(start, Width, Width)) Job.Walkable[GridUtils.CellToIndex(start, Width)] = new GridWalkable { Value = (byte)(scenario == 5 ? 0 : 1) };
                if (GridUtils.InBounds(goal, Width, Width))
                {
                    int gi = GridUtils.CellToIndex(goal, Width);
                    if (scenario == 6) Job.Walkable[gi] = default;
                    if (scenario == 7 || scenario == 12) Job.DynamicBlocked.Set(gi, true);
                    if (scenario == 11) Job.Walkable[gi]=new GridWalkable {Value=1};
                }
                if (scenario == 8 || scenario == 9)
                {
                    int wall = scenario == 9 ? Width-10 : Width/2;
                    for (int z = 0; z < Width; z++) Job.DynamicBlocked.Set(z*Width+wall, true);
                }
                if (scenario == 10)
                {
                    for (int z = Width/2-4; z <= Width/2+4; z++) Job.DynamicBlocked.Set(z*Width+Width/2, true);
                }
                var moving = new Entity { Index = 1, Version = 1 };
                Job.Entities[0] = moving; Job.UnitGrids[0] = new UnitGrid { Cell = start };
                Job.Footprints[0] = new UnitFootprint { Size = vehicle ? new int2(2,2) : new int2(1,1) };
                Job.MovementBehaviors[0] = new UnitMovementBehavior { UsesVehicleMotion = (byte)(vehicle ? 1 : 0) };
                Job.Factions[0] = 1; Job.ManualMoves[0] = (byte)(scenario == 12 || scenario == 0 && seed % 2 == 0 ? 1 : 0);
                Job.CheapSegmentModes[0] = (byte)(scenario == 0 ? seed % 4 : 0);
                Job.AlternateSearchSkipped[0] = (byte)(scenario == 0 ? seed % 2 : 1);
                Job.IgnoredOccupancyEntities[0] = default; Job.IgnoredOccupancyCells[0] = default; Job.IgnoredOccupancySizes[0] = default;
                for (int i = 0; i < 4; i++)
                {
                    var cell = i == 0 ? start : scenario == 0 ? new int2(random.Next(Width), random.Next(Width)) : new int2(-10,-10);
                    Job.LiveUnitEntities[i] = new Entity { Index = i+1, Version = i%2+1 };
                    Job.LiveUnitGrids[i] = new UnitGrid { Cell = cell };
                    Job.LiveUnitFootprints[i] = new UnitFootprint { Size = i == 0 ? Job.Footprints[0].Size : new int2(i%2+1,1) };
                    Job.LiveUnitManualGroupMembers[i] = (byte)(i%2);
                    for (int z = 0; z < Width; z++) for (int x = 0; x < Width; x++)
                        if (UnitFootprintUtility.ContainsCell(cell, Job.LiveUnitFootprints[i].Size, new int2(x,z))) Job.Occupied.Set(z*Width+x,true);
                }
                if (scenario == 0 && seed % 7 == 0)
                {
                    Job.IgnoredOccupancyEntities[0] = Job.LiveUnitEntities[1]; Job.IgnoredOccupancyCells[0] = Job.LiveUnitGrids[1].Cell;
                    Job.IgnoredOccupancySizes[0] = Job.LiveUnitFootprints[1].Size;
                }
                Job.RequestedGoals[0] = new UnitPathRequest { Goal = goal }; Job.Goals[0] = goal;
                Job.Status[0] = 0; Job.FailureCodes[0] = 0; Job.ExpansionCounts[0] = 773; Job.AlternateAttempts[0] = 0;
                Job.Segmented[0] = 0; Job.SearchEpochBase = epoch;
                if (hasStream) stream.Dispose();
                stream = new NativeStream(1, Allocator.TempJob); hasStream = true; Job.Output = stream.AsWriter();
            }
            public int2[] ReadPath()
            {
                var reader = stream.AsReader(); int count = reader.BeginForEachIndex(0); var path = new int2[count];
                for (int i = 0; i < count; i++) path[i] = reader.Read<int2>(); reader.EndForEachIndex(); return path;
            }
            public void Dispose() { if (hasStream) stream.Dispose(); for (int i=owners.Count-1;i>=0;i--) owners[i].Dispose(); }
        }
        [BurstCompile(CompileSynchronously=true)]
        private struct PairJob:IJob
        {
            public PathfindBatchJob Actual;
            public GridPathRoutingOriginalOracle Original;
            public NativeArray<byte> Compiled;
            [BurstDiscard] private static void ManagedOnly(ref bool value){value=false;}
            public void Execute(){bool compiled=true;ManagedOnly(ref compiled);Compiled[0]=compiled?(byte)1:(byte)0;Actual.Execute(0);Original.Execute(0);}
        }
        private static GridPathRoutingOriginalOracle Oracle(PathfindBatchJob job)=>new GridPathRoutingOriginalOracle
        {
            Grid=job.Grid,
            Walkable=job.Walkable,
            Roads=job.Roads,
            Sidewalks=job.Sidewalks,
            DirtRoads=job.DirtRoads,
            MapSurface=job.MapSurface,
            HasMapSurface=job.HasMapSurface,
            SurfaceValidation=job.SurfaceValidation,
            MapSurfacePathCost=job.MapSurfacePathCost,
            SurfacePathCost=job.SurfacePathCost,
            SurfaceRoadPriority=job.SurfaceRoadPriority,
            DynamicBlocked=job.DynamicBlocked,
            FriendlyPassFactionIds=job.FriendlyPassFactionIds,
            Occupied=job.Occupied,
            LiveUnitEntities=job.LiveUnitEntities,
            LiveUnitGrids=job.LiveUnitGrids,
            LiveUnitFootprints=job.LiveUnitFootprints,
            LiveUnitManualGroupMembers=job.LiveUnitManualGroupMembers,
            Entities=job.Entities,
            UnitGrids=job.UnitGrids,
            Footprints=job.Footprints,
            MovementBehaviors=job.MovementBehaviors,
            Factions=job.Factions,
            ManualMoves=job.ManualMoves,
            IgnoredOccupancyEntities=job.IgnoredOccupancyEntities,
            IgnoredOccupancyCells=job.IgnoredOccupancyCells,
            IgnoredOccupancySizes=job.IgnoredOccupancySizes,
            RequestedGoals=job.RequestedGoals,
            Goals=job.Goals,
            Segmented=job.Segmented,
            Output=job.Output,
            Status=job.Status,
            FailureCodes=job.FailureCodes,
            ExpansionCounts=job.ExpansionCounts,
            CheapSegmentModes=job.CheapSegmentModes,
            AlternateSearchSkipped=job.AlternateSearchSkipped,
            AlternateAttempts=job.AlternateAttempts,
            GridSize=job.GridSize,
            ScratchCameFrom=job.ScratchCameFrom,
            ScratchGScore=job.ScratchGScore,
            ScratchClosed=job.ScratchClosed,
            ScratchInOpen=job.ScratchInOpen,
            ScratchEpoch=job.ScratchEpoch,
            ScratchOpen=job.ScratchOpen,
            ScratchPath=job.ScratchPath,
            SearchEpochBase=job.SearchEpochBase,
        };
        private static void Compare(Fixture actual, Fixture original, string label)
        {
            Assert.AreEqual(original.Job.Status[0], actual.Job.Status[0], label+" status");
            Assert.AreEqual(original.Job.FailureCodes[0], actual.Job.FailureCodes[0], label+" failure");
            Assert.AreEqual(original.Job.ExpansionCounts[0], actual.Job.ExpansionCounts[0], label+" expansions");
            Assert.AreEqual(original.Job.Goals[0], actual.Job.Goals[0], label+" assigned goal");
            Assert.AreEqual(original.Job.AlternateAttempts[0], actual.Job.AlternateAttempts[0], label+" alternate attempts");
            Assert.AreEqual(original.Job.Segmented[0], actual.Job.Segmented[0], label+" segmented");
            CollectionAssert.AreEqual(original.ReadPath(), actual.ReadPath(), label+" complete ordered path");
            for (int i=0;i<actual.Job.GridSize;i++)
            {
                Assert.AreEqual(original.Job.ScratchEpoch[i], actual.Job.ScratchEpoch[i], label+" epoch "+i);
                Assert.AreEqual(original.Job.ScratchCameFrom[i], actual.Job.ScratchCameFrom[i], label+" came "+i);
                Assert.AreEqual(original.Job.ScratchGScore[i], actual.Job.ScratchGScore[i], label+" score "+i);
                Assert.AreEqual(original.Job.ScratchClosed[i], actual.Job.ScratchClosed[i], label+" closed "+i);
                Assert.AreEqual(original.Job.ScratchInOpen[i], actual.Job.ScratchInOpen[i], label+" inOpen "+i);
                Assert.AreEqual(original.Job.ScratchOpen[i], actual.Job.ScratchOpen[i], label+" heap "+i);
                Assert.AreEqual(original.Job.ScratchPath[i], actual.Job.ScratchPath[i], label+" reconstruction "+i);
            }
        }
        private static void ExecutePair(Fixture actual,Fixture original,bool burst)
        {
            if(!burst){actual.Job.Execute(0);var oracle=Oracle(original.Job);oracle.Execute(0);return;}
            using var compiled=new NativeArray<byte>(1,Allocator.TempJob);
            new PairJob{Actual=actual.Job,Original=Oracle(original.Job),Compiled=compiled}.Schedule().Complete();
            Assert.AreEqual(1,compiled[0],"Outer-policy parity must execute actual Burst");
        }
        private static void Metadata(Fixture fixture)
        {fixture.Job.Status[0]=7;fixture.Job.FailureCodes[0]=37;fixture.Job.ExpansionCounts[0]=773;fixture.Job.AlternateAttempts[0]=29;fixture.Job.Segmented[0]=2;}
        private static void Vehicle(Fixture fixture)
        {
            fixture.Job.Footprints[0]=new UnitFootprint{Size=new int2(2,2)};
            fixture.Job.MovementBehaviors[0]=new UnitMovementBehavior{UsesVehicleMotion=1};
            fixture.Job.LiveUnitFootprints[0]=fixture.Job.Footprints[0];
            for(int z=0;z<fixture.Width;z++)for(int x=0;x<fixture.Width;x++)
                if(UnitFootprintUtility.ContainsCell(fixture.Job.UnitGrids[0].Cell,new int2(2,2),new int2(x,z)))fixture.Job.Occupied.Set(z*fixture.Width+x,true);
        }
        [Test] public void SeededOuterPolicyAndUntouchedMetadataMatchOriginal()
        {
            Assert.IsTrue(BurstCompiler.IsEnabled);using var actual=new Fixture(24);using var original=new Fixture(24);
            int found=0,failed=0;
            foreach(bool burst in new[]{false,true})for(int i=0;i<96;i++)
            {
                int seed=983101+i,epoch=500001+(i+(burst?96:0))*128;
                actual.Load(seed,0,epoch);original.Load(seed,0,epoch);Metadata(actual);Metadata(original);
                if(i%3==0){var desired=new UnitPathRequest{Goal=new int2(31,12)};actual.Job.RequestedGoals[0]=desired;original.Job.RequestedGoals[0]=desired;}
                ExecutePair(actual,original,burst);Compare(actual,original,"outer seed="+seed+" burst="+burst);
                if(actual.Job.Status[0]!=0)found++;else failed++;
            }
            Assert.Greater(found,0);Assert.Greater(failed,0);
            Debug.Log($"[GridPathRoutingSeeded] managed=96 burst=96 found={found} failed={failed} metadataAndScratch=Exact");
        }
        private static void ConfigureNamed(Fixture fixture,int scenario,int epoch)
        {
            int baseScenario=scenario==0?1:scenario==1?2:scenario==2?3:scenario==3?7:scenario==4||scenario==5?8:scenario==6||scenario==8||scenario==9?12:11;
            fixture.Load(1000+scenario,baseScenario,epoch);Metadata(fixture);
            if(scenario==2||scenario==3||scenario==4||scenario==5)fixture.Job.AlternateSearchSkipped[0]=0;
            if(scenario==5)Vehicle(fixture);
            if(scenario==7)fixture.Job.ManualMoves[0]=1;
            if(scenario==8)fixture.Job.RequestedGoals[0]=new UnitPathRequest{Goal=new int2(3,30)};
            if(scenario==9)fixture.Job.RequestedGoals[0]=new UnitPathRequest{Goal=new int2(int.MaxValue,int.MinValue)};
        }
        [Test] public void NamedAlternatesLimitsAndManualFallbackMatchOriginal()
        {
            using var actual=new Fixture(24);using var original=new Fixture(24);
            foreach(bool burst in new[]{false,true})for(int scenario=0;scenario<10;scenario++)
            {
                int epoch=600001+(scenario+(burst?10:0))*128;
                ConfigureNamed(actual,scenario,epoch);ConfigureNamed(original,scenario,epoch);
                ExecutePair(actual,original,burst);Compare(actual,original,"outer named="+scenario+" burst="+burst);
                if(scenario==0){Assert.AreEqual(29,actual.Job.AlternateAttempts[0]);Assert.AreEqual(2,actual.Job.Segmented[0]);}
                if(scenario==1){Assert.AreEqual(1,actual.Job.FailureCodes[0]);Assert.AreEqual(773,actual.Job.ExpansionCounts[0]);Assert.AreEqual(29,actual.Job.AlternateAttempts[0]);}
                if(scenario==2){Assert.AreEqual(1,actual.Job.AlternateAttempts[0]);Assert.AreEqual(new int2(1,23),actual.Job.Goals[0],"clamped-center ring order");}
                if(scenario==3){Assert.AreEqual(1,actual.Job.AlternateAttempts[0]);Assert.AreEqual(new int2(19,13),actual.Job.Goals[0],"top-left ring candidate first");}
                if(scenario==4)Assert.AreEqual(16,actual.Job.AlternateAttempts[0],"infantry alternate quota");
                if(scenario==5)Assert.AreEqual(32,actual.Job.AlternateAttempts[0],"vehicle alternate quota");
                if(scenario==6){Assert.AreEqual(1,actual.Job.Status[0]);Assert.AreEqual(1,actual.Job.Segmented[0]);Assert.AreEqual(7,actual.Job.FailureCodes[0],"manual success preserves prior failure metadata");Assert.AreEqual(2,actual.ReadPath().Length);}
                if(scenario==7){Assert.AreEqual(0,actual.Job.Status[0]);Assert.AreEqual(10,actual.Job.FailureCodes[0]);}
                if(scenario==8)Assert.AreEqual(new int2(19,23),actual.Job.Goals[0],"outermost usable progress ring has priority over closer interior rings");
            }
        }
        [Test] public void DetailedHaulerUsesOriginalNativeBudgetAndWeightedRetry()
        {
            using var actual=new Fixture(160);using var original=new Fixture(160);
            foreach(bool burst in new[]{false,true})
            {
                int epoch=700001+(burst?128:0);actual.Load(7,9,epoch);original.Load(7,9,epoch);Vehicle(actual);Vehicle(original);
                actual.Job.CheapSegmentModes[0]=3;original.Job.CheapSegmentModes[0]=3;
                ExecutePair(actual,original,burst);Compare(actual,original,"detailed hauler native12000 burst="+burst);
                Assert.AreEqual(9,actual.Job.FailureCodes[0]);Assert.AreEqual(12001,actual.Job.ExpansionCounts[0]);
                int retried=0;for(int i=0;i<actual.Job.GridSize;i++)if(actual.Job.ScratchEpoch[i]==epoch+1)retried++;
                Assert.Greater(retried,0,"mode3 budget failure must run weighted retry at epoch+1");
            }
        }

        private struct TraceContext:IPathSearchContext
        {
            public int Width;
            public NativeArray<byte> Blocked,ClosedCells,InOpen;
            public NativeArray<int> Epochs,Came,Scores,Paths;
            public NativeArray<long> Heap;
            public NativeArray<int2> Output;
            public int OutputCount;
            public bool IsWalkable(int index)=>true;
            public bool IsSurfaceCellPathable(int x,int z)=>true;
            public bool IsSurfaceFootprintPathable(int x,int z)=>true;
            public bool CanPlace(int x,int z,int cx,int cz)=>GridPathSearchKernel.InBounds(x,z,Width,Width)&&Blocked[z*Width+x]==0;
            public int TraversalCost(int index,bool diagonal)=>diagonal?14:10;
            public int Epoch(int i)=>Epochs[i];public void SetEpoch(int i,int value)=>Epochs[i]=value;
            public int CameFrom(int i)=>Came[i];public void SetCameFrom(int i,int value)=>Came[i]=value;
            public int GScore(int i)=>Scores[i];public void SetGScore(int i,int value)=>Scores[i]=value;
            public byte Closed(int i)=>ClosedCells[i];public void SetClosed(int i,byte value)=>ClosedCells[i]=value;
            public void SetInOpen(int i,byte value)=>InOpen[i]=value;
            public long Open(int i)=>Heap[i];public void SetOpen(int i,long value)=>Heap[i]=value;
            public int Path(int i)=>Paths[i];public void SetPath(int i,int value)=>Paths[i]=value;
            public void WriteCell(int x,int z)=>Output[OutputCount++]=new int2(x,z);
        }
        private struct Observer:IPathRoutingObserver
        {
            public NativeArray<PathRoutingAttempt> Attempts;public int Count;
            public void OnAttempt(in PathRoutingAttempt attempt){Attempts[Count++]=attempt;}
        }
        [BurstCompile(CompileSynchronously=true)]private struct TraceJob:IJob
        {
            public TraceContext Context;public Observer Observer;public PathRoutingRequest Request;
            public NativeArray<int> Results;
            [BurstDiscard]private static void ManagedOnly(ref bool value){value=false;}
            public void Execute(){bool compiled=true;ManagedOnly(ref compiled);var state=new PathRoutingState{GoalX=4,GoalZ=2,ExpansionCount=773};GridPathRoutingKernel.Route(ref Context,Request,ref state,ref Observer);Results[0]=Observer.Count;Results[1]=state.FailureCode;Results[2]=state.ExpansionCount;Results[3]=compiled?1:0;}
        }
        [Test]public void ExplicitBudgetAttemptTracePreservesEpochAndWeight()
        {
            using var blockedOwner=new NativeArray<byte>(25,Allocator.TempJob);var blocked=blockedOwner;blocked[12]=1;
            using var closed=new NativeArray<byte>(25,Allocator.TempJob);using var inOpen=new NativeArray<byte>(25,Allocator.TempJob);
            using var epochs=new NativeArray<int>(25,Allocator.TempJob);using var came=new NativeArray<int>(25,Allocator.TempJob);using var scores=new NativeArray<int>(25,Allocator.TempJob);using var paths=new NativeArray<int>(25,Allocator.TempJob);using var heap=new NativeArray<long>(25,Allocator.TempJob);using var output=new NativeArray<int2>(25,Allocator.TempJob);using var attempts=new NativeArray<PathRoutingAttempt>(2,Allocator.TempJob);using var results=new NativeArray<int>(4,Allocator.TempJob);
            var context=new TraceContext{Width=5,Blocked=blocked,ClosedCells=closed,InOpen=inOpen,Epochs=epochs,Came=came,Scores=scores,Paths=paths,Heap=heap,Output=output};
            var request=new PathRoutingRequest{Search=new PathSearchRequest{Width=5,Height=5,GridSize=25,StartX=0,StartZ=2,GoalX=4,GoalZ=2,SearchEpoch=9100,SearchBoundsPadding=8,MaxExpansions=0},DesiredGoalX=4,DesiredGoalZ=2,GoalSearchRadius=10,MaxCandidateAttempts=16,CheapMode=3,HasModes=true,SkipAlternateSearch=true};
            new TraceJob{Context=context,Observer=new Observer{Attempts=attempts},Request=request,Results=results}.Schedule().Complete();
            Assert.AreEqual(1,results[3],"actual Burst trace control");Assert.AreEqual(2,results[0]);Assert.AreEqual(9,results[1]);Assert.AreEqual(1,results[2]);
            Assert.AreEqual(9100,attempts[0].SearchEpoch);Assert.AreEqual(1,attempts[0].HeuristicWeight);Assert.AreEqual(9101,attempts[1].SearchEpoch);Assert.AreEqual(2,attempts[1].HeuristicWeight);
            Assert.AreEqual(9,attempts[0].FailureCode);Assert.AreEqual(9,attempts[1].FailureCode);
            SyntheticWeightedSuccessUsesActualPlacement();
        }

        // Discriminating synthetic budget15 control; native vehicle budget remains12000.
        // Placement queries use the actual shared native adapter on an explicit flat grid.
        private struct NativeFactTraceContext:IPathSearchContext
        {
            public PathfindBatchJob Job;public NativeArray<int2> Output;public int Count;
            public bool IsWalkable(int i)=>Job.Walkable[i].Value!=0;
            public bool IsSurfaceCellPathable(int x,int z)=>true;
            public bool IsSurfaceFootprintPathable(int x,int z)=>true;
            public bool CanPlace(int x,int z,int cx,int cz)=>UnitPathPlacementValidation.CanPlaceForPathing(Job.Grid,Job.Walkable,Job.DynamicBlocked,Job.FriendlyPassFactionIds,Job.Occupied,Job.LiveUnitEntities,Job.LiveUnitGrids,Job.LiveUnitFootprints,Job.LiveUnitManualGroupMembers,Job.Entities[0],new int2(x,z),new int2(1,1),new int2(cx,cz),true,false,1);
            public int TraversalCost(int i,bool diagonal)=>diagonal?14:10;
            public int Epoch(int i)=>Job.ScratchEpoch[i];public void SetEpoch(int i,int value)=>Job.ScratchEpoch[i]=value;
            public int CameFrom(int i)=>Job.ScratchCameFrom[i];public void SetCameFrom(int i,int value)=>Job.ScratchCameFrom[i]=value;
            public int GScore(int i)=>Job.ScratchGScore[i];public void SetGScore(int i,int value)=>Job.ScratchGScore[i]=value;
            public byte Closed(int i)=>Job.ScratchClosed[i];public void SetClosed(int i,byte value)=>Job.ScratchClosed[i]=value;
            public void SetInOpen(int i,byte value)=>Job.ScratchInOpen[i]=value;
            public long Open(int i)=>Job.ScratchOpen[i];public void SetOpen(int i,long value)=>Job.ScratchOpen[i]=value;
            public int Path(int i)=>Job.ScratchPath[i];public void SetPath(int i,int value)=>Job.ScratchPath[i]=value;
            public void WriteCell(int x,int z)=>Output[Count++]=new int2(x,z);
        }
        private static void SyntheticWeightedSuccessUsesActualPlacement()
        {
            using var fixture=new Fixture(15);fixture.Load(1,1,10101);
            var start=new int2(2,7);fixture.Job.UnitGrids[0]=new UnitGrid{Cell=start};fixture.Job.LiveUnitGrids[0]=fixture.Job.UnitGrids[0];
            for(int i=0;i<225;i++)fixture.Job.Occupied.Set(i,false);fixture.Job.Occupied.Set(7*15+2,true);
            for(int z=2;z<=7;z++)fixture.Job.DynamicBlocked.Set(z*15+7,true);
            using var output=new NativeArray<int2>(225,Allocator.Temp);using var attempts=new NativeArray<PathRoutingAttempt>(2,Allocator.Temp);
            var context=new NativeFactTraceContext{Job=fixture.Job,Output=output};var observer=new Observer{Attempts=attempts};
            var request=new PathRoutingRequest{Search=new PathSearchRequest{Width=15,Height=15,GridSize=225,StartX=2,StartZ=7,GoalX=12,GoalZ=7,SearchBoundsPadding=180,MaxExpansions=15,SearchEpoch=10101,IsVehicle=true},DesiredGoalX=12,DesiredGoalZ=7,GoalSearchRadius=20,MaxCandidateAttempts=32,CheapMode=3,HasModes=true,SkipAlternateSearch=true};
            var state=new PathRoutingState{GoalX=12,GoalZ=7};GridPathRoutingKernel.Route(ref context,request,ref state,ref observer);
            Assert.AreEqual(2,observer.Count);Assert.AreEqual(9,attempts[0].FailureCode);Assert.AreEqual(16,attempts[0].ExpansionCount);Assert.IsTrue(attempts[1].Found);Assert.AreEqual(13,attempts[1].ExpansionCount);Assert.AreEqual(1,attempts[0].HeuristicWeight);Assert.AreEqual(2,attempts[1].HeuristicWeight);Assert.AreEqual(10102,attempts[1].SearchEpoch);
            var expected=new[]{new int2(2,7),new int2(5,7),new int2(6,8),new int2(8,8),new int2(9,7),new int2(12,7)};Assert.AreEqual(expected.Length,context.Count);for(int i=0;i<context.Count;i++)Assert.AreEqual(expected[i],output[i],"synthetic weighted path node"+i);
            Debug.Log("[GridPathRoutingWeightedControl] syntheticBudget=15 normalFailure=9 normalExpansions=16 weightedExpansions=13 nativeVehicleBudget=12000");
        }
    }
}
#endif
