using System;using Unity.Jobs;using System.Collections.Generic;using System.IO;using System.Linq;using System.Diagnostics;using Newtonsoft.Json.Linq;using Game.Configs;using Game.Components;using Game.Runtime;using Game.Runtime.Pathfinding;using Unity.Entities;using Unity.Collections;using Unity.Mathematics;
internal static class SelectedPathSurfaceEquality {
 static JObject Measure(string name,Func<int> call){long bytes=GC.GetAllocatedBytesForCurrentThread(),start=Stopwatch.GetTimestamp();int checksum=0;for(int repeat=0;repeat<64;repeat++)checksum=unchecked(checksum+call());long end=Stopwatch.GetTimestamp(),allocated=GC.GetAllocatedBytesForCurrentThread()-bytes;return new JObject{["name"]=name,["calls"]=192,["seconds"]=(double)(end-start)/Stopwatch.Frequency,["allocated_bytes"]=allocated,["checksum"]=checksum};}
 static JObject File(JObject d,int maximum){return JObject.Parse(System.Text.Encoding.UTF8.GetString(AutonomousWorld.PinnedFile((string)d["path"],(string)d["sha256"],maximum)),new JsonLoadSettings{DuplicatePropertyNameHandling=DuplicatePropertyNameHandling.Error});}
        private sealed class Fixture : IDisposable
        {
            public PathfindBatchJob Job;
            private readonly List<IDisposable> owners = new List<IDisposable>();
            private NativeStream stream;
            private bool hasStream;
            public readonly int Width;
            private T Own<T>(T value) where T : struct, IDisposable { owners.Add(value); return value; }
            public Fixture(int width,int height)
            {
                Width = width; int size = width * height;
                Job = new PathfindBatchJob { Grid = new GridConfig { Width = width, Height = height, CellSize = 1 }, GridSize = size,
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
            public void ResetStream(){if(hasStream)stream.Dispose();stream=new NativeStream(1,Allocator.TempJob);hasStream=true;Job.Output=stream.AsWriter();}
            public int2[] ReadPath()
            {
                var reader = stream.AsReader(); int count = reader.BeginForEachIndex(0); var path = new int2[count];
                for (int i = 0; i < count; i++) path[i] = reader.Read<int2>(); reader.EndForEachIndex(); return path;
            }
            public void Dispose() { Job.DynamicBlocked.Dispose();Job.Occupied.Dispose();if (hasStream) stream.Dispose(); for (int i=owners.Count-1;i>=0;i--) owners[i].Dispose(); }
        }

 static void ConfigureFull(Fixture f,MapSurfaceComponent surface,UnitFootprint footprint,UnitMovementBehavior movement,byte[] walk,byte[] blocked,int2 start,int epoch){int count=2048*1024;for(int i=0;i<count;i++){f.Job.Walkable[i]=new GridWalkable{Value=walk[i]};f.Job.DynamicBlocked.Set(i,blocked[i]!=0);f.Job.FriendlyPassFactionIds[i]=255;}var entity=new Entity{Index=1220,Version=1};f.Job.Entities[0]=entity;f.Job.UnitGrids[0]=new UnitGrid{Cell=start};f.Job.Footprints[0]=footprint;f.Job.MovementBehaviors[0]=movement;f.Job.Factions[0]=1;f.Job.RequestedGoals[0]=new UnitPathRequest{Goal=new int2(825,388)};f.Job.Goals[0]=new int2(825,388);for(int i=0;i<4;i++){f.Job.LiveUnitEntities[i]=new Entity{Index=i==0?1220:i==1?1231:2000+i,Version=1};f.Job.LiveUnitGrids[i]=new UnitGrid{Cell=i==0?start:i==1?new int2(844,328):new int2(-10,-10)};f.Job.LiveUnitFootprints[i]=footprint;}for(int z=0;z<1024;z++)for(int x=0;x<2048;x++)if(UnitFootprintUtility.ContainsCell(start,footprint.Size,new int2(x,z))||UnitFootprintUtility.ContainsCell(new int2(844,328),footprint.Size,new int2(x,z)))f.Job.Occupied.Set(z*2048+x,true);f.Job.MapSurface=surface;f.Job.HasMapSurface=1;f.Job.SurfaceValidation=new MapSurfaceTraversalValidation();f.Job.SurfacePathCost=new MapSurfacePathCost();f.Job.SurfaceRoadPriority=new MapSurfaceRoadPriorityPolicy();f.Job.MapSurfacePathCost=new MapSurfacePathCostComponent{EnableSlopeCost=0};f.Job.SearchEpochBase=epoch;f.Job.AlternateSearchSkipped[0]=1;f.ResetStream();}
static void Equal(object a,object b,string m=""){if(!object.Equals(a,b))throw new InvalidOperationException(m);}static void PathEqual(int2[]a,int2[]b,string m=""){if(!a.SequenceEqual(b))throw new InvalidOperationException(m);}
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
 public static int Run(string[]args){try{if(args.Length!=2||System.IO.File.Exists(args[1]))throw new ArgumentException("pinnedinput freshoutput required");var input=JObject.Parse(System.IO.File.ReadAllText(args[0]));var graph=File((JObject)input["config_graph"],32*1024*1024);var assets=new SourceConfigMaterializer(graph);var source=assets.ByPath<OperationMapDefinition>((string)input["source_path"]);var surfaceAsset=assets.ByPath<MapSurfaceDataAsset>((string)input["surface_path"]);var raw=File((JObject)input["native_capture"],128*1024*1024);var header=raw["headlessPlacementInputs"];if((string)raw["result"]!="Collected"||(int)header["gridWidth"]!=2048||(int)header["gridHeight"]!=1024||(string)header["sourceDefinitionSha256"]!=(string)input["source_definition_sha256"]||source.GridMetadata.Dimensions.x!=2048||source.GridMetadata.Dimensions.y!=1024)throw new InvalidDataException("Actual fullRefinerysource/frame required");
 var view=GeneratedReferenceTemplateView.Read((JObject)input["reference_templates"],out string binary);GeneratedReferenceTemplateImporter.Preflight(view,binary,((JObject)view["opening_prefabs"]).Properties().Select(v=>v.Name));const string key="Unit_Veh_Truck_Tray";string id=(string)view["opening_prefabs"][key];var root=((JArray)view["entities"]).OfType<JObject>().Single(e=>(string)e["id"]==id);var codec=new ExactInitialValueCodec(binary,new JObject());UnitFootprint footprint=(UnitFootprint)codec.Decode(root["components"][typeof(UnitFootprint).FullName]["value"],typeof(UnitFootprint),id+"/footprint");UnitMovementBehavior movement=(UnitMovementBehavior)codec.Decode(root["components"][typeof(UnitMovementBehavior).FullName]["value"],typeof(UnitMovementBehavior),id+"/movement");if(movement.AllowIdleWander!=1||movement.UsesVehicleMotion!=1)throw new InvalidDataException("Actual nativeTruck movementdomain differs");
 if(!surfaceAsset.TryCreateRuntimeBlobAsset(Allocator.Persistent,out var blob))throw new InvalidDataException("OriginalfullSurface decoder rejected");try{ref var full=ref blob.Value;if(full.Dimensions.x!=2048||full.Dimensions.y!=1024||full.CellSize!=1||math.any(full.GridOrigin!=float3.zero))throw new InvalidDataException("Actual fullsurface frame differs");GeneratedMapSurfaceFlags.GetSurfaceFeatureFlags(ref full,out byte layered,out byte roads,out byte bridges);var surface=new MapSurfaceComponent{SurfaceBlob=blob,GridOrigin=full.GridOrigin,CellSize=full.CellSize,Dimensions=full.Dimensions,HasSurfaceData=1,HasLayeredCells=layered,HasRoadSurfaces=roads,HasBridgeSurfaces=bridges};
 int count=2048*1024;var walk=Convert.FromBase64String((string)header["gridWalkable"]["base64"]);var blocked=Convert.FromBase64String((string)header["authoredBuildings"]["base64"]);if(walk.Length!=count||blocked.Length!=count||walk.Any(b=>b>1)||blocked.Any(b=>b>1))throw new InvalidDataException("Actual fullnativeboolarrays required");
 using var direct=new Fixture(2048,1024);using var original=new Fixture(2048,1024);using var deferred=new Fixture(2048,1024);ConfigureFull(direct,surface,footprint,movement,walk,blocked,new int2(836,345),1);ConfigureFull(original,surface,footprint,movement,walk,blocked,new int2(836,345),1);ConfigureFull(deferred,surface,footprint,movement,walk,blocked,new int2(836,345),1);
 direct.Job.Execute(0);var oracle=Oracle(original.Job);oracle.Execute(0);Compare(direct,original,"genuinefullsurface direct/original");var handle=deferred.Job.Schedule(1,default(JobHandle));handle.Complete();Compare(deferred,original,"genuinefullsurface actualworker/original");
 var result=new JObject{["result"]="Passed",["actual_full_surface"]=true,["grid"]=new JArray(2048,1024),["source_prefab"]=key,["source_entity"]=id,["native_footprint"]=new JArray(footprint.Size.x,footprint.Size.y),["status"]=deferred.Job.Status[0],["failure"]=deferred.Job.FailureCodes[0],["expansions"]=deferred.Job.ExpansionCounts[0],["path_count"]=deferred.ReadPath().Length,["complete_ordered_path_and_ALLscratch_exact"]=true,["worker_joined_before_fixture_and_blob_disposal"]=handle.IsCompleted,["world"]=false,["entity_ids_created"]=0,["scope"]="Retained actual57553 fullframe/fulloriginalSurface decoder/nativeTruck and originalFixture/ConfigureFull/Oracle/Compare. Controlledoccupancy/neutralfriendlypass/manualfalse/unindexedcoverage; NOT retainedfullrequest replay/native timing/mission proof."};System.IO.File.WriteAllText(args[1],result.ToString());Console.WriteLine("[SelectedPathSurfaceEquality] result=Passed fullSurface=True pathAndAllScratch=Exact World=False");
 }finally{blob.Dispose();}return 0;}catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
