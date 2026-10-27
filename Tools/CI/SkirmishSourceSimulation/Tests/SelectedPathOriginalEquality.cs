using System;using System.IO;using System.Linq;using System.Collections.Generic;using System.Reflection;using System.Diagnostics;using Newtonsoft.Json.Linq;using Game.Components;using Game.Runtime;using Game.Runtime.Pathfinding;using Unity.Collections;using Unity.Entities;using Unity.Jobs;using Unity.Mathematics;
internal static class SelectedPathOriginalEquality{
static void Equal(object expected,object actual,string message=""){if(!object.Equals(expected,actual))throw new InvalidOperationException(message+" expected="+expected+" actual="+actual);}static void PathEqual(int2[] expected,int2[] actual,string message=""){if(!expected.SequenceEqual(actual))throw new InvalidOperationException(message+" ordered path differs");}
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
                    DynamicBlocked = new NativeBitArray(size, Allocator.TempJob),
                    Occupied = new NativeBitArray(size, Allocator.TempJob),
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
            public void Dispose() { Job.DynamicBlocked.Dispose();Job.Occupied.Dispose();if (hasStream) stream.Dispose(); for (int i=owners.Count-1;i>=0;i--) owners[i].Dispose(); }
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
            Equal(original.Job.Status[0], actual.Job.Status[0], label+" status");
            Equal(original.Job.FailureCodes[0], actual.Job.FailureCodes[0], label+" failure");
            Equal(original.Job.ExpansionCounts[0], actual.Job.ExpansionCounts[0], label+" expansions");
            Equal(original.Job.Goals[0], actual.Job.Goals[0], label+" assigned goal");
            Equal(original.Job.AlternateAttempts[0], actual.Job.AlternateAttempts[0], label+" alternate attempts");
            Equal(original.Job.Segmented[0], actual.Job.Segmented[0], label+" segmented");
            PathEqual(original.ReadPath(), actual.ReadPath(), label+" complete ordered path");
            for (int i=0;i<actual.Job.GridSize;i++)
            {
                Equal(original.Job.ScratchEpoch[i], actual.Job.ScratchEpoch[i], label+" epoch "+i);
                Equal(original.Job.ScratchCameFrom[i], actual.Job.ScratchCameFrom[i], label+" came "+i);
                Equal(original.Job.ScratchGScore[i], actual.Job.ScratchGScore[i], label+" score "+i);
                Equal(original.Job.ScratchClosed[i], actual.Job.ScratchClosed[i], label+" closed "+i);
                Equal(original.Job.ScratchInOpen[i], actual.Job.ScratchInOpen[i], label+" inOpen "+i);
                Equal(original.Job.ScratchOpen[i], actual.Job.ScratchOpen[i], label+" heap "+i);
                Equal(original.Job.ScratchPath[i], actual.Job.ScratchPath[i], label+" reconstruction "+i);
            }
        }
public static int Run(){int cases=0;foreach(int width in new[]{24,40}){int count=width==24?128:12;using var actual=new Fixture(width);using var original=new Fixture(width);using var scheduled=new Fixture(width);for(int index=0;index<count;index++){int scenario=width==24?0:index+1,seed=width==24?773101+index:1000+scenario,epoch=width==24?1+index*128:100001+scenario*128;actual.Load(seed,scenario,epoch);original.Load(seed,scenario,epoch);scheduled.Load(seed,scenario,epoch);actual.Job.Execute(0);var oracle=Oracle(original.Job);oracle.Execute(0);Compare(actual,original,"original direct "+index);var handle=scheduled.Job.Schedule(1,default(JobHandle));handle.Complete();Compare(scheduled,original,"actual deferred "+index);cases++;}}return cases;}
}
