using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using Game.Components;
using Game.Runtime;
using Game.Runtime.Spawning;

static class SpawnProgram
{
    static long work;
    internal static void Charge(long cells){work+=cells;SpawnJson.Need(cells>=0&&work<=512L*1024*1024,"Aggregate replay work exceeds512million bounded cell inspections");}
    internal static int GridSize(BlockerGrid grid)
    {SpawnJson.Need(grid.Width>0&&grid.Height>0&&(long)grid.Width*grid.Height<=16*1024*1024&&grid.CellSize>0,"Bounded complete positive grid required; no crops");return grid.Width*grid.Height;}
    static object Actor(JsonElement scenario)
    {
        SpawnJson.Shape(scenario,new[]{"id","kind","state","overlay","shot_interval"},"expect");
        var state=SpawnJson.Facts<SkirmishActorInitializationState>(scenario.GetProperty("state"));var overlay=SpawnJson.Facts<SkirmishActorOverlayValues>(scenario.GetProperty("overlay"));
        float interval=scenario.GetProperty("shot_interval").GetSingle();SpawnJson.Need(float.IsFinite(interval),"Finite interval required");
        SkirmishActorInitializationKernel.ApplyHealth(ref state,overlay);SkirmishActorInitializationKernel.BindWeapons(ref state,overlay,interval);
        return new{id=SpawnJson.Id(scenario.GetProperty("id")),kind="actor_initialization",state};
    }
    static object Search(JsonElement scenario,SpawnArtifacts artifacts,bool surfaceOnly)
    {
        string[] fields=surfaceOnly?new[]{"id","kind","width","height","walkable","blocked","occupied","reserved","cell","footprint","has_match","surface_count","surface_admission","emit_arrays"}:new[]{"id","kind","width","height","walkable","blocked","occupied","reserved","center","radius","footprint","is_air","random_state","emit_arrays"};
        SpawnJson.Shape(scenario,fields,"expect");int width=SpawnJson.Int(scenario.GetProperty("width"),1,1048576),height=SpawnJson.Int(scenario.GetProperty("height"),1,1048576);int count=GridSize(new BlockerGrid{Width=width,Height=height,CellSize=1});
        int[] size=SpawnJson.Cell(scenario.GetProperty("footprint"),64);SpawnJson.Need(size[0]>=1&&size[1]>=1,"Positive raw footprints required for safe surface release");
        Charge((long)count*size[0]*size[1]);
        bool arrays=scenario.GetProperty("emit_arrays").GetBoolean();SpawnJson.Need(!arrays||count<=65536,"Fullmask outputs restricted to small fixtures");
        var storage=new SpawnStorage{Walkable=artifacts.Bytes(scenario.GetProperty("walkable"),count,true),Blocked=artifacts.Bytes(scenario.GetProperty("blocked"),count,true),Occupied=artifacts.Bytes(scenario.GetProperty("occupied"),count,true),Reserved=artifacts.Bytes(scenario.GetProperty("reserved"),count,true)};
        int x,z;bool found;uint? state=null;
        if(surfaceOnly)
        {
            int[] cell=SpawnJson.Cell(scenario.GetProperty("cell"));x=cell[0];z=cell[1];int minX=x-(size[0]-1)/2,minZ=z-(size[1]-1)/2;
            SpawnJson.Need(minX>=0&&minZ>=0&&minX+size[0]<=width&&minZ+size[1]<=height,"Surface correction requires an inbounds just-reserved footprint");
            var surface=new SpawnSurface{Admission=artifacts.Bytes(scenario.GetProperty("surface_admission"),count,true),Width=width,Height=height,SizeX=size[0],SizeZ=size[1]};
            // The callback facts must reject footprint placements outside grid, as native traversal does.
            for(int zz=0;zz<height;zz++)for(int xx=0;xx<width;xx++)if(surface.Admission[xx+zz*width]!=0)
            {int mx=xx-(size[0]-1)/2,mz=zz-(size[1]-1)/2;SpawnJson.Need(mx>=0&&mz>=0&&mx+size[0]<=width&&mz+size[1]<=height,"Surface footprint facts admit out-of-grid footprint");}
            found=SpawnCellSearchKernel.EnsureTraversable(ref storage,ref surface,width,height,size[0],size[1],scenario.GetProperty("has_match").GetBoolean(),SpawnJson.Int(scenario.GetProperty("surface_count"),0,64),ref x,ref z);
        }
        else
        {
            int[] center=SpawnJson.Cell(scenario.GetProperty("center"));int radius=SpawnJson.Int(scenario.GetProperty("radius"),0,64);var rng=new SpawnCellRandom{State=scenario.GetProperty("random_state").GetUInt32()};SpawnJson.Need(rng.State!=0,"Nonzero live RNG state required");
            found=SpawnCellSearchKernel.TryFindInitial(ref storage,ref rng,width,height,center[0],center[1],radius,size[0],size[1],scenario.GetProperty("is_air").GetBoolean(),out x,out z);state=rng.State;
        }
        return new{id=SpawnJson.Id(scenario.GetProperty("id")),kind=surfaceOnly?"surface_correction":"initial_search",found,cell=new[]{x,z},random_state=state,reserved_sha256=SpawnArtifacts.Hash(storage.Reserved),reserved=arrays?storage.Reserved.Select(v=>(int)v).ToArray():null};
    }
    static int Main(string[] args)
    {
        var timer=Stopwatch.StartNew();var results=new List<object>();var errors=new List<object>();
        try
        {
            SpawnJson.Need(args.Length==1,"One captured input path required");byte[] raw=File.ReadAllBytes(args[0]);SpawnJson.Need(raw.Length<=16*1024*1024,"JSON exceeds16MiB");using var document=JsonDocument.Parse(raw);var input=document.RootElement;SpawnJson.Unique(input);SpawnJson.Shape(input,new[]{"schema_version","artifacts","cases"});SpawnJson.Need(input.GetProperty("schema_version").GetInt32()==1,"Unsupported schema");
            var artifacts=new SpawnArtifacts(input.GetProperty("artifacts"),args[0]);var ids=new HashSet<string>();var cases=SpawnJson.Array(input.GetProperty("cases"),256);SpawnJson.Need(cases.Length>0,"At least one case required");
            foreach(var scenario in cases)
            {
                string id=SpawnJson.Id(scenario.GetProperty("id"));SpawnJson.Need(ids.Add(id),"Duplicate case ID");
                try
                {
                    object result=scenario.GetProperty("kind").GetString() switch{"actor_initialization"=>Actor(scenario),"initial_search"=>Search(scenario,artifacts,false),"surface_correction"=>Search(scenario,artifacts,true),"blockers"=>SpawnBlockers.Run(scenario,artifacts),"placement_sequence"=>SpawnSequence.Run(scenario,artifacts),_=>throw new ArgumentException("Unsupported case kind")};
                    if(scenario.TryGetProperty("expect",out var expected))SpawnJson.Expect(expected,JsonSerializer.SerializeToElement(result,SpawnJson.Options),id);
                    results.Add(result);
                }
                catch(Exception error){errors.Add(new{id,error=error.Message});}
            }
        }
        catch(Exception error){errors.Add(new{id="input",error=error.Message});}
        Console.WriteLine(JsonSerializer.Serialize(new{result=errors.Count==0?"passed":"failed",passed=results.Count,failed=errors.Count,results,errors,elapsed_seconds=timer.Elapsed.TotalSeconds,native_runs=0,full_mission_initialized=false,player_ready=false,scope="SharedSpawnBlockerActorSnapshotReplay"},SpawnJson.Options));return errors.Count==0?0:1;
    }
}
