#!/usr/bin/env python3
"""Generate paired source-host-only placement-memo and S026 Execute-boundary overlays."""
import argparse, hashlib, json, tempfile
from pathlib import Path

ROOT=Path(__file__).resolve().parents[5]
VALIDATOR=Path("Assets/Game/Scripts/Systems/UnitPathPlacementValidation.cs")
BACKEND=Path("Tools/CI/SkirmishSourceSimulation/Adapters/S026/119-123_OwnedSystemRuntime.cs")
VALIDATOR_ANCHOR='''            var context = new NativeContext { Grid=grid, WalkableCells=walkable,BlockedCells=dynamicBlocked,FriendlyIds=friendlyPassFactionIds,OccupiedCells=occupied,Entities=liveUnitEntities,Grids=liveUnitGrids,Footprints=liveUnitFootprints,Groups=liveUnitManualGroupMembers };
            return PathPlacementKernel.CanPlaceForPathingWithCoverage(ref context,Identity(movingEntity),Cell(cell),Cell(footprintSize),Cell(currentCell),isVehicle,manualMove,factionId,Identity(ignoredOccupancyEntity),Cell(ignoredOccupancyCell),Cell(ignoredOccupancySize),unitCoverage.CanSkip(grid,cell,footprintSize));'''
ORIGINAL='''  public static JobHandle Schedule<T>(this T job,int count,JobHandle dependency)where T:struct,IJobFor{dependency.Complete();if(count<0||count>32)throw new NotSupportedException("Owned original path batch bound");var m=typeof(T).GetMethod("Execute",new[]{typeof(int)})??throw new NotSupportedException("Source Execute missing");object boxed=job;for(int i=0;i<count;i++)m.Invoke(boxed,new object[]{i});return default;}'''

VALIDATOR_MEMO=r'''
        [System.ThreadStatic] private static Dictionary<MemoKey,bool> sourceHostMemo;
        [System.ThreadStatic] private static bool sourceHostShadow;
        [System.ThreadStatic] private static int sourceHostHits,sourceHostMisses,sourceHostFallbacks,sourceHostMismatches;
        private const int SourceHostMemoCapacity=4096;
        internal static void BeginSourceHostMemo(bool shadow){if(sourceHostMemo!=null)throw new System.InvalidOperationException("Nested source-host CanPlace memo scope");sourceHostMemo=new Dictionary<MemoKey,bool>();sourceHostShadow=shadow;sourceHostHits=sourceHostMisses=sourceHostFallbacks=sourceHostMismatches=0;}
        internal static void EndSourceHostMemo(){try{System.Console.WriteLine("[CanPlaceMemoTelemetry] frame="+UnityEngine.Time.frameCount+" hits="+sourceHostHits+" misses="+sourceHostMisses+" fallbacks="+sourceHostFallbacks+" mismatches="+sourceHostMismatches);}finally{sourceHostMemo?.Clear();sourceHostMemo=null;sourceHostShadow=false;sourceHostHits=sourceHostMisses=sourceHostFallbacks=sourceHostMismatches=0;}}
        private readonly struct MemoKey : System.IEquatable<MemoKey>
        {
            readonly GridConfig grid; readonly NativeArray<GridWalkable> walkable; readonly NativeBitArray blocked,occupied;
            readonly NativeArray<byte> friendly,groups; readonly NativeArray<Entity> entities;
            readonly NativeArray<UnitGrid> grids; readonly NativeArray<UnitFootprint> footprints;
            readonly Entity moving,ignored; readonly int2 cell,size,current,ignoredCell,ignoredSize;
            readonly bool vehicle,manual; readonly byte faction; readonly UnitCoverage coverage;
            public MemoKey(in GridConfig g,NativeArray<GridWalkable>w,NativeBitArray b,NativeArray<byte> f,NativeBitArray o,NativeArray<Entity>e,NativeArray<UnitGrid>ug,NativeArray<UnitFootprint>uf,NativeArray<byte>gm,Entity m,int2 c,int2 s,int2 cur,bool v,bool man,byte fac,Entity ie,int2 ic,int2 iz,UnitCoverage cov){grid=g;walkable=w;blocked=b;friendly=f;occupied=o;entities=e;grids=ug;footprints=uf;groups=gm;moving=m;cell=c;size=s;current=cur;vehicle=v;manual=man;faction=fac;ignored=ie;ignoredCell=ic;ignoredSize=iz;coverage=cov;}
            public bool Equals(MemoKey x)=>grid.Width==x.grid.Width&&grid.Height==x.grid.Height&&grid.CellSize.Equals(x.grid.CellSize)&&grid.Origin.Equals(x.grid.Origin)&&walkable.Equals(x.walkable)&&blocked.Equals(x.blocked)&&friendly.Equals(x.friendly)&&occupied.Equals(x.occupied)&&entities.Equals(x.entities)&&grids.Equals(x.grids)&&footprints.Equals(x.footprints)&&groups.Equals(x.groups)&&moving.Equals(x.moving)&&ignored.Equals(x.ignored)&&cell.Equals(x.cell)&&size.Equals(x.size)&&current.Equals(x.current)&&ignoredCell.Equals(x.ignoredCell)&&ignoredSize.Equals(x.ignoredSize)&&vehicle==x.vehicle&&manual==x.manual&&faction==x.faction&&coverage.Equals(x.coverage);
            public override bool Equals(object x)=>x is MemoKey other&&Equals(other);
            public override int GetHashCode(){unchecked{int h=grid.Width;h=h*31+grid.Height;h=h*31+grid.CellSize.GetHashCode();h=h*31+grid.Origin.GetHashCode();h=h*31+walkable.GetHashCode();h=h*31+blocked.GetHashCode();h=h*31+friendly.GetHashCode();h=h*31+occupied.GetHashCode();h=h*31+entities.GetHashCode();h=h*31+grids.GetHashCode();h=h*31+footprints.GetHashCode();h=h*31+groups.GetHashCode();h=h*31+moving.GetHashCode();h=h*31+ignored.GetHashCode();h=h*31+cell.GetHashCode();h=h*31+size.GetHashCode();h=h*31+current.GetHashCode();h=h*31+ignoredCell.GetHashCode();h=h*31+ignoredSize.GetHashCode();h=h*31+(vehicle?1:0);h=h*31+(manual?1:0);h=h*31+faction;return h*31+coverage.GetHashCode();}}
        }
        private static bool CanPlaceForPathingOriginal(in GridConfig grid,NativeArray<GridWalkable> walkable,NativeBitArray dynamicBlocked,NativeArray<byte> friendlyPassFactionIds,NativeBitArray occupied,NativeArray<Entity> liveUnitEntities,NativeArray<UnitGrid> liveUnitGrids,NativeArray<UnitFootprint> liveUnitFootprints,NativeArray<byte> liveUnitManualGroupMembers,Entity movingEntity,int2 cell,int2 footprintSize,int2 currentCell,bool isVehicle,bool manualMove,byte factionId,Entity ignoredOccupancyEntity,int2 ignoredOccupancyCell,int2 ignoredOccupancySize,UnitCoverage unitCoverage){var context=new NativeContext{Grid=grid,WalkableCells=walkable,BlockedCells=dynamicBlocked,FriendlyIds=friendlyPassFactionIds,OccupiedCells=occupied,Entities=liveUnitEntities,Grids=liveUnitGrids,Footprints=liveUnitFootprints,Groups=liveUnitManualGroupMembers};return PathPlacementKernel.CanPlaceForPathingWithCoverage(ref context,Identity(movingEntity),Cell(cell),Cell(footprintSize),Cell(currentCell),isVehicle,manualMove,factionId,Identity(ignoredOccupancyEntity),Cell(ignoredOccupancyCell),Cell(ignoredOccupancySize),unitCoverage.CanSkip(grid,cell,footprintSize));}
'''

def transform_validator(src):
    if src.count(VALIDATOR_ANCHOR)!=1: raise ValueError("Fail-closed: exact UnitPathPlacementValidation original body anchor required")
    if "BeginSourceHostMemo" in src: raise ValueError("Validator overlay already applied")
    signature=src[src.index("        public static bool CanPlaceForPathing("):src.index("        {",src.index("        public static bool CanPlaceForPathing("))]
    # Preserve the canonical public signature verbatim, then route to isolated memo helper.
    wrapper=signature+'''        {
            if(sourceHostMemo==null)return CanPlaceForPathingOriginal(grid,walkable,dynamicBlocked,friendlyPassFactionIds,occupied,liveUnitEntities,liveUnitGrids,liveUnitFootprints,liveUnitManualGroupMembers,movingEntity,cell,footprintSize,currentCell,isVehicle,manualMove,factionId,ignoredOccupancyEntity,ignoredOccupancyCell,ignoredOccupancySize,unitCoverage);
            var key=new MemoKey(grid,walkable,dynamicBlocked,friendlyPassFactionIds,occupied,liveUnitEntities,liveUnitGrids,liveUnitFootprints,liveUnitManualGroupMembers,movingEntity,cell,footprintSize,currentCell,isVehicle,manualMove,factionId,ignoredOccupancyEntity,ignoredOccupancyCell,ignoredOccupancySize,unitCoverage);
            if(sourceHostMemo.TryGetValue(key,out bool cached)){sourceHostHits++;if(sourceHostShadow){bool actual=CanPlaceForPathingOriginal(grid,walkable,dynamicBlocked,friendlyPassFactionIds,occupied,liveUnitEntities,liveUnitGrids,liveUnitFootprints,liveUnitManualGroupMembers,movingEntity,cell,footprintSize,currentCell,isVehicle,manualMove,factionId,ignoredOccupancyEntity,ignoredOccupancyCell,ignoredOccupancySize,unitCoverage);if(actual!=cached){sourceHostMismatches++;sourceHostMemo.Clear();throw new System.InvalidOperationException("Source-host CanPlace memo shadow mismatch; cache cleared");}return actual;}return cached;}
            sourceHostMisses++;
            bool result=CanPlaceForPathingOriginal(grid,walkable,dynamicBlocked,friendlyPassFactionIds,occupied,liveUnitEntities,liveUnitGrids,liveUnitFootprints,liveUnitManualGroupMembers,movingEntity,cell,footprintSize,currentCell,isVehicle,manualMove,factionId,ignoredOccupancyEntity,ignoredOccupancyCell,ignoredOccupancySize,unitCoverage);
            if(sourceHostMemo.Count<SourceHostMemoCapacity)sourceHostMemo.Add(key,result);else sourceHostFallbacks++;
            return result;
        }
'''
    # Exact params preserved by copied source signature. replace body through balanced brace.
    start=src.index(VALIDATOR_ANCHOR)
    method_start=src.rfind("        public static bool CanPlaceForPathing(",0,start)
    brace=src.index("        {",method_start); depth=0; end=None
    for i in range(brace, len(src)):
        if src[i]=="{": depth+=1
        elif src[i]=="}":
            depth-=1
            if depth==0: end=i+1; break
    if end is None: raise ValueError("Fail-closed: unclosed validator declaration")
    out=src[:method_start]+VALIDATOR_MEMO+"\n"+wrapper+src[end:]
    out=out.replace("using Unity.Collections;","using Unity.Collections;\nusing System.Collections.Generic;",1)
    return out

def transform_backend(src):
    if src.count(ORIGINAL)!=1: raise ValueError("Fail-closed: exact S026 IJobFor boundary anchor required")
    replacement='''  public static JobHandle Schedule<T>(this T job,int count,JobHandle dependency)where T:struct,IJobFor{dependency.Complete();if(count<0||count>32)throw new NotSupportedException("Owned original path batch bound");var m=typeof(T).GetMethod("Execute",new[]{typeof(int)})??throw new NotSupportedException("Source Execute missing");object boxed=job;bool memo=typeof(T).FullName=="Game.Runtime.PathfindBatchJob"&&Environment.GetEnvironmentVariable("WARLINE_SOURCE_CANPLACE_MEMO")=="1";bool shadow=Environment.GetEnvironmentVariable("WARLINE_SOURCE_CANPLACE_MEMO_SHADOW")=="1";for(int i=0;i<count;i++){if(memo)Game.Runtime.UnitPathPlacementValidation.BeginSourceHostMemo(shadow);try{m.Invoke(boxed,new object[]{i});}finally{if(memo)Game.Runtime.UnitPathPlacementValidation.EndSourceHostMemo();}}return default;}'''
    return src.replace(ORIGINAL,replacement,1)

def generate(repo:Path,output:Path)->dict:
    root=repo.resolve(strict=True);output=output.absolute();output.mkdir(parents=True,exist_ok=False)
    import importlib.util
    resolver_path=root/"Tools/CI/SkirmishSourceSimulation/Recipes/regenerate_path_partials.py"
    spec=importlib.util.spec_from_file_location("current_path_partials",resolver_path);resolver=importlib.util.module_from_spec(spec);spec.loader.exec_module(resolver)
    with tempfile.TemporaryDirectory(prefix="path-partials-resolver-") as temp:
        resolver_dir=Path(temp)/"resolver-output"
        proof=resolver.generate(root,resolver_dir)
        base_validator=(resolver_dir/"UnitPathPlacementValidation.cs").read_text(encoding="utf-8")
    original_resolver_outputs=dict(proof["outputs"])
    generated_validator=output/"192_UnitPathPlacementValidation.cs"
    candidate_validator=transform_validator(base_validator)
    with generated_validator.open("w",encoding="utf-8",newline="") as f:f.write(candidate_validator)
    backend_input=(root/BACKEND).read_bytes();backend_candidate=transform_backend(backend_input.decode("utf-8"))
    backend_name="119-123_OwnedSystemRuntime.SourceHostMemo.cs";backend_output=output/backend_name
    with backend_output.open("w",encoding="utf-8",newline="") as f:f.write(backend_candidate)
    if (root/BACKEND).read_bytes()!=backend_input:raise ValueError("S026 backend source changed during generation")
    hashes={"192_UnitPathPlacementValidation.cs":hashlib.sha256(generated_validator.read_bytes()).hexdigest(),backend_name:hashlib.sha256(backend_output.read_bytes()).hexdigest()}
    proof["resolver_outputs"]=original_resolver_outputs
    proof["outputs"]=dict(hashes)
    proof["source_pins"]=dict(proof["source_inputs"])
    proof["source_pins"][str(root/BACKEND)]=hashlib.sha256(backend_input).hexdigest()
    proof["experimental_only"]=True
    proof["source_host_memo_overlay"]={"kind":"S026OwnedCanPlaceRequestMemo","source_inputs_preserved":True,"required_current_links_preserved":True,"backend_source":str(root/BACKEND),"backend_source_sha256":hashlib.sha256(backend_input).hexdigest(),"generated_output_sha256":dict(proof["outputs"]),"capacity":4096,"telemetry":"one bounded aggregate row per Execute: hit/miss/fallback/mismatch counters","shadow":"cache hits recompute original; mismatch increments telemetry, clears cache and throws; matching shadow hit returns fresh original answer","allocations":"managed dictionary and key/value/telemetry allocations may occur; no allocation-free claim","scope":"per synchronous reflected PathfindBatchJob Execute; cleanup in adapter finally"}
    proof["pathfind_batch_job_native_direct_preserved"]=True
    proof["build_manifest_changed"]=False
    proof["mission_run"]=False
    proof["compile_status"]="unverified; no source-host resolver build was run"
    (output/"source-extraction.json").write_text(json.dumps(proof,indent=2)+"\n")
    return proof

if __name__=="__main__":
    p=argparse.ArgumentParser();p.add_argument("--repo",type=Path,default=ROOT);p.add_argument("--output",type=Path,required=True);a=p.parse_args();print(json.dumps(generate(a.repo,a.output),indent=2))
