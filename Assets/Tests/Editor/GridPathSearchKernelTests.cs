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
    public sealed class GridPathSearchKernelTests
    {
        public static int RunChecks()
        {
            var tests = new GridPathSearchKernelTests();
            tests.OriginalSearchAndPlacementDependencyArePinned();
            tests.NativeCompleteSearchJobMatchesOriginalManagedAndBurst();
            tests.NamedSearchEndpointBudgetAndRouteControls();
            tests.SharedHelpersPreserveBudgetsAndRasterOrder();
            Debug.Log("[GridPathSearchParity] result=Passed tests=4 missionReady=0");
            return 4;
        }
        public static void RunCombinedValidation()
        {
            try
            {
                RunChecks();
                GridOccupancyKernelTests.RunChecks();
                int placement = PathPlacementKernelTests.RunChecks();
                Assert.AreEqual(3, placement);
                GridPathMovementKernelTests.RunChecks();
                DirectDamageSequencerTests.RunChecks();
                var attack = new StandardAttackPlannerTests();
                attack.OriginalOracleIsPinnedVerbatimProduction();
                attack.InstalledMathNaNEqualAndClampSemanticsMatchKernel();
                attack.ManagedRandomAndBoundaryPlansMatchEveryField();
                attack.BurstScheduledPlansMatchOriginalAndManagedResults();
                attack.ActualPrivateRuntimeAdapterPreservesEveryPlanField();
                var combat = new global::UnitCombatFocusedEditModeTests();
                combat.StandardAttack_RespectsAuthoredDomainAndVisibilityPolicy();
                combat.StandardAttack_NonLethalHitDamagesTargetAndRecordsFeedbackState();
                combat.StandardAttack_CooldownRecoversAcrossLostEngagementAndCanFireOnReacquisition();
                combat.StandardAttack_NoEngagementRecoverySkipsIneligibleActorsAndDoesNotDoubleTick();
                combat.AttackVfxRequestResolution_PopulatesMuzzlePlaybackData();
                combat.AttackVfxRequestResolution_PopulatesImpactPlaybackData();
                Debug.Log("[SharedNavigationCombatParity] result=Passed search=4 occupancy=2 occupancyLifecycle=7 placement=3 priorRegressions=25 missionReady=0");
                ValidationExit.Passed();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception); Debug.LogError("[SharedNavigationCombatParity] result=Failed missionReady=0");
                ValidationExit.Failed();
            }
        }

        [Test] public void OriginalSearchAndPlacementDependencyArePinned()
        {
            string dir = Path.Combine(Application.dataPath, "Tests/Editor");
            string original = File.ReadAllText(Path.Combine(dir, "GridPathSearchOriginalSource.cs.txt")).Replace("\r\n", "\n");
            using var sha = SHA256.Create();
            string hash = BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(original))).Replace("-", "").ToLowerInvariant();
            Assert.AreEqual("f1bf77ca9dbce1918c8ae1c74883ae4a5fff1043e9a52f2d771c8f8c1b529f19", hash);
            string expected = original.Replace("internal struct PathfindBatchJob : IJobFor", "internal struct GridPathSearchOriginalOracle : IJobFor")
                .Replace("UnitPathPlacementValidation", "PathPlacementOriginalOracle");
            Assert.AreEqual(expected, File.ReadAllText(Path.Combine(dir, "GridPathSearchOriginalOracle.cs")).Replace("\r\n", "\n"));
            new PathPlacementKernelTests().OriginalOracleIsPinned();
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

        [BurstCompile(CompileSynchronously = true)]
        private struct PairJob : IJob
        {
            public PathfindBatchJob Actual;
            public GridPathSearchOriginalOracle Original;
            public NativeArray<byte> Compiled;
            [BurstDiscard] private static void ManagedOnly(ref bool value) { value = false; }
            public void Execute()
            {
                bool compiled = true; ManagedOnly(ref compiled); Compiled[0] = compiled ? (byte)1 : (byte)0;
                Actual.Execute(0); Original.Execute(0);
            }
        }
        private static GridPathSearchOriginalOracle Oracle(PathfindBatchJob job) => new GridPathSearchOriginalOracle
        {
            Grid = job.Grid,
            Walkable = job.Walkable,
            Roads = job.Roads,
            Sidewalks = job.Sidewalks,
            DirtRoads = job.DirtRoads,
            MapSurface = job.MapSurface,
            HasMapSurface = job.HasMapSurface,
            SurfaceValidation = job.SurfaceValidation,
            MapSurfacePathCost = job.MapSurfacePathCost,
            SurfacePathCost = job.SurfacePathCost,
            SurfaceRoadPriority = job.SurfaceRoadPriority,
            DynamicBlocked = job.DynamicBlocked,
            FriendlyPassFactionIds = job.FriendlyPassFactionIds,
            Occupied = job.Occupied,
            LiveUnitEntities = job.LiveUnitEntities,
            LiveUnitGrids = job.LiveUnitGrids,
            LiveUnitFootprints = job.LiveUnitFootprints,
            LiveUnitManualGroupMembers = job.LiveUnitManualGroupMembers,
            Entities = job.Entities,
            UnitGrids = job.UnitGrids,
            Footprints = job.Footprints,
            MovementBehaviors = job.MovementBehaviors,
            Factions = job.Factions,
            ManualMoves = job.ManualMoves,
            IgnoredOccupancyEntities = job.IgnoredOccupancyEntities,
            IgnoredOccupancyCells = job.IgnoredOccupancyCells,
            IgnoredOccupancySizes = job.IgnoredOccupancySizes,
            RequestedGoals = job.RequestedGoals,
            Goals = job.Goals,
            Segmented = job.Segmented,
            Output = job.Output,
            Status = job.Status,
            FailureCodes = job.FailureCodes,
            ExpansionCounts = job.ExpansionCounts,
            CheapSegmentModes = job.CheapSegmentModes,
            AlternateSearchSkipped = job.AlternateSearchSkipped,
            AlternateAttempts = job.AlternateAttempts,
            GridSize = job.GridSize,
            ScratchCameFrom = job.ScratchCameFrom,
            ScratchGScore = job.ScratchGScore,
            ScratchClosed = job.ScratchClosed,
            ScratchInOpen = job.ScratchInOpen,
            ScratchEpoch = job.ScratchEpoch,
            ScratchOpen = job.ScratchOpen,
            ScratchPath = job.ScratchPath,
            SearchEpochBase = job.SearchEpochBase,
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
        private static void ExecutePair(Fixture actual, Fixture original, bool burst)
        {
            if (!burst) { actual.Job.Execute(0); var oracle=Oracle(original.Job); oracle.Execute(0); return; }
            using var compiled = new NativeArray<byte>(1, Allocator.TempJob);
            new PairJob { Actual=actual.Job, Original=Oracle(original.Job), Compiled=compiled }.Schedule().Complete();
            Assert.AreEqual(1, compiled[0], "Search parity must execute actual Burst, no managed fallback");
        }
        [Test] public void NativeCompleteSearchJobMatchesOriginalManagedAndBurst()
        {
            Assert.IsTrue(BurstCompiler.IsEnabled);
            using var actual = new Fixture(24); using var original = new Fixture(24);
            int found=0, blocked=0;
            foreach (bool burst in new[] { false, true })
            for (int i=0;i<128;i++)
            {
                int seed=773101+i, epoch=1+(i+(burst?128:0))*128;
                actual.Load(seed,0,epoch); original.Load(seed,0,epoch);
                ExecutePair(actual,original,burst); Compare(actual,original,(burst?"Burst":"Managed")+" seed="+seed);
                if(actual.Job.Status[0]!=0)found++;else blocked++;
            }
            Assert.Greater(found,0);Assert.Greater(blocked,0);
            Debug.Log($"[GridPathSearchSeeded] managed=128 burst=128 found={found} blocked={blocked} completeState=Exact");
        }
        [Test] public void NamedSearchEndpointBudgetAndRouteControls()
        {
            using var actual=new Fixture(40);using var original=new Fixture(40);
            foreach(bool burst in new[]{false,true})
            for(int scenario=1;scenario<=12;scenario++)
            {
                int epoch=100001+(scenario+(burst?10:0))*128;
                actual.Load(1000+scenario,scenario,epoch);original.Load(1000+scenario,scenario,epoch);
                ExecutePair(actual,original,burst);Compare(actual,original,"named "+scenario+" burst="+burst);
                if(scenario==1||scenario==4||scenario==10)Assert.AreEqual(1,actual.Job.Status[0],"named reachable "+scenario);
                if(scenario==2)Assert.AreEqual(1,actual.Job.FailureCodes[0]);
                if(scenario==3)Assert.AreEqual(4,actual.Job.FailureCodes[0]);
                if(scenario==5)Assert.AreEqual(2,actual.Job.FailureCodes[0]);
                if(scenario==6)Assert.AreEqual(5,actual.Job.FailureCodes[0]);
                if(scenario==7)Assert.AreEqual(7,actual.Job.FailureCodes[0]);
                if(scenario==9)Assert.AreEqual(9,actual.Job.FailureCodes[0],"original expansion budget is enforced");
                if(scenario==11)Assert.AreEqual(10,actual.Job.FailureCodes[0],"original exhausted-open no path");
                if(scenario==12){Assert.AreEqual(1,actual.Job.Status[0]);Assert.AreEqual(1,actual.Job.Segmented[0],"manual progress fallback");Assert.AreNotEqual(actual.Job.RequestedGoals[0].Goal,actual.Job.Goals[0]);}
            }
        }
        [Test] public void SharedHelpersPreserveBudgetsAndRasterOrder()
        {
            for(int mode=0;mode<4;mode++)foreach(bool vehicle in new[]{false,true})foreach(bool manual in new[]{false,true})
            {
                bool cheap=mode!=0;
                int padding=mode>=2?180:vehicle?12:cheap?180:8;
                int max=vehicle?(manual||mode==3?12000:1600):(cheap?30000:450);
                Assert.AreEqual(padding,GridPathSearchKernel.ResolveSearchPadding(vehicle,cheap,true,(byte)mode));
                Assert.AreEqual(max,GridPathSearchKernel.ResolveExpansionLimit(vehicle,manual,cheap,true,(byte)mode));
            }
            var random=new System.Random(892214);
            for(int i=0;i<4096;i++)
            {
                int2 start=new int2(random.Next(-100,101),random.Next(-100,101));int2 goal=new int2(random.Next(-100,101),random.Next(-100,101));
                int steps=math.max(math.abs(goal.x-start.x),math.abs(goal.y-start.y));if(steps==0)continue;
                int step=random.Next(1,steps+1);float t=(float)step/steps;
                var expected=new int2((int)math.round(math.lerp(start.x,goal.x,t)),(int)math.round(math.lerp(start.y,goal.y,t)));
                var actual=GridPathSearchKernel.DirectPathCell(start.x,start.y,goal.x,goal.y,step,steps);
                Assert.AreEqual(expected,new int2(actual.X,actual.Z),"direct raster "+i);
            }
            foreach(int score in new[]{int.MinValue,-1,0,1,int.MaxValue})foreach(int index in new[]{0,1,int.MaxValue})
                Assert.AreEqual(index,GridPathSearchKernel.UnpackHeapIndex(GridPathSearchKernel.PackHeapEntry(score,index)));
        }
    }
}
#endif
