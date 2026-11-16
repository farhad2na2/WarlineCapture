#!/usr/bin/env python3
"""Generate a source-host-only cell-pair memo overlay under pinned fixed-context proof."""
import argparse, hashlib, json, re, sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[5]
BASE_DIR = Path(__file__).resolve().parents[1] / "pathfind_canplace_memo_boundary"
JOB = Path("Assets/Game/Scripts/Systems/Pathfinding/PathfindBatchJob.cs")
FIXED_CONTEXT_SOURCES = {
    "Assets/Game/Scripts/Systems/Pathfinding/PathfindBatchJob.cs": "197f5b6997f4430c07666dfb91aaca749dff99ee20df117a5e8c61f99482be0a",
    "Assets/Game/Scripts/Systems/Pathfinding/GridPathRoutingKernel.cs": "a18c4aa5af645c348ffc0f767856e3de24c5d674ebc45c4ef69ce4886d421ad5",
    "Assets/Game/Scripts/Systems/Pathfinding/GridPathSearchKernel.cs": "c697791bdf0ce45ed3adeca8b92df634a429e95de85f951dd8a38ba19c39ffea",
    "Assets/Game/Scripts/Systems/UnitPathGridSnapshot.cs": "53d06bc98e48817079dd31ba1b341209e9509ce31d23b72dea972519b56ac9cb",
    "Assets/Game/Scripts/Systems/UnitPathLiveUnitSnapshot.cs": "c4692f93cda036eb26ad6f7ba9f75d6c7da0cc0b72b3fcfebc7736588819eab9",
}
JOB_CONTEXT = '''                var context = new NativeSearchContext
                {
                    Job = this, Writer = writer, MovingEntity = Entities[index], IgnoredEntity = IgnoredOccupancyEntities[index],
                    IgnoredCell = IgnoredOccupancyCells[index], IgnoredSize = IgnoredOccupancySizes[index],
                    FootprintSize = footprint.Size, IsVehicle = vehicle, ManualMove = manual, FactionId = Factions[index]
                };'''
JOB_CALL = '''            public bool CanPlace(int x, int z, int cx, int cz) => UnitPathPlacementValidation.CanPlaceForPathing(
                Job.Grid, Job.Walkable, Job.DynamicBlocked, Job.FriendlyPassFactionIds, Job.Occupied,
                Job.LiveUnitEntities, Job.LiveUnitGrids, Job.LiveUnitFootprints, Job.LiveUnitManualGroupMembers,
                MovingEntity, new int2(x, z), FootprintSize, new int2(cx, cz), IsVehicle, ManualMove, FactionId,
                IgnoredEntity, IgnoredCell, IgnoredSize, Job.LiveUnitCoverage);'''
ROUTE_CALL = "                GridPathRoutingKernel.Route(ref context, request, ref state);"

def load_base():
    import importlib.util
    spec=importlib.util.spec_from_file_location("memo_boundary_base", BASE_DIR/"generate.py")
    mod=importlib.util.module_from_spec(spec);spec.loader.exec_module(mod);return mod

def prove_fixed_context(job:str):
    if job.count(JOB_CONTEXT)!=1 or job.count(JOB_CALL)!=1 or job.count("UnitPathPlacementValidation.CanPlaceForPathing(")!=1 or job.count(ROUTE_CALL)!=1:
        raise ValueError("Fail-closed: pinned PathfindBatchJob fixed-context, route, and sole CanPlace call anchors required")
    if "context.CanPlaceMemo" in job or "CanPlaceForPathingOriginal" in job:
        raise ValueError("Fail-closed: unexpected memo or alternate placement route in pinned PathfindBatchJob")

def transform_validator(src:str)->str:
    base=load_base()
    out=base.transform_validator(src)
    # Drop every non-cell input from the key: the fixed-context proof is the only basis for this.
    key_start=out.index("        private readonly struct MemoKey")
    key_end=out.index("        private static bool CanPlaceForPathingOriginal",key_start)
    pair_key='''        private readonly struct MemoKey : System.IEquatable<MemoKey>
        {
            readonly int2 target,current;
            public MemoKey(int2 t,int2 c){target=t;current=c;}
            public bool Equals(MemoKey x)=>target.Equals(x.target)&&current.Equals(x.current);
            public override bool Equals(object x)=>x is MemoKey other&&Equals(other);
            public override int GetHashCode(){unchecked{return target.GetHashCode()*31+current.GetHashCode();}}
        }
'''
    out=out[:key_start]+pair_key+out[key_end:]
    out=out.replace("new MemoKey(grid,walkable,dynamicBlocked,friendlyPassFactionIds,occupied,liveUnitEntities,liveUnitGrids,liveUnitFootprints,liveUnitManualGroupMembers,movingEntity,cell,footprintSize,currentCell,isVehicle,manualMove,factionId,ignoredOccupancyEntity,ignoredOccupancyCell,ignoredOccupancySize,unitCoverage)","new MemoKey(cell,currentCell)")
    return out

def generate(repo:Path, output:Path)->dict:
    root=repo.resolve(strict=True); base=load_base(); output=output.absolute()
    for path, expected in FIXED_CONTEXT_SOURCES.items():
        if hashlib.sha256((root/path).read_bytes()).hexdigest()!=expected:
            raise ValueError("Fixed-context source changed: "+path)
    job_path=root/JOB; job_raw=job_path.read_bytes(); prove_fixed_context(job_raw.decode("utf-8"))
    output.mkdir(parents=True,exist_ok=False)
    import tempfile
    with tempfile.TemporaryDirectory(prefix="cell-pair-base-") as td:
        # Reuse the boundary recipe's resolver and generated original validator, retaining its source closure.
        import importlib.util
        rp=root/"Tools/CI/SkirmishSourceSimulation/Recipes/regenerate_path_partials.py"
        spec=importlib.util.spec_from_file_location("cell_pair_resolver",rp);resolver=importlib.util.module_from_spec(spec);spec.loader.exec_module(resolver)
        proof=resolver.generate(root,Path(td)/"resolver")
        validator=(Path(td)/"resolver"/"UnitPathPlacementValidation.cs").read_text(encoding="utf-8")
    generated=transform_validator(validator)
    backend_path=root/base.BACKEND; backend_raw=backend_path.read_bytes()
    backend=base.transform_backend(backend_raw.decode("utf-8"))
    name="192_UnitPathPlacementValidation.CellPairMemo.cs"
    with (output/name).open("w",encoding="utf-8",newline="") as f:f.write(generated)
    backend_name="119-123_OwnedSystemRuntime.CellPairMemo.cs"
    with (output/backend_name).open("w",encoding="utf-8",newline="") as f:f.write(backend)
    if job_path.read_bytes()!=job_raw or backend_path.read_bytes()!=backend_raw: raise ValueError("Pinned source changed during generation")
    outputs={n:hashlib.sha256((output/n).read_bytes()).hexdigest() for n in (name,backend_name)}
    proof["resolver_outputs"]=dict(proof["outputs"]);proof["outputs"]=outputs
    proof["source_pins"]=dict(proof["source_inputs"])
    proof["source_pins"].update({str(root/path):digest for path,digest in FIXED_CONTEXT_SOURCES.items()})
    proof["source_pins"][str(job_path)]=hashlib.sha256(job_raw).hexdigest()
    proof["source_pins"][str(backend_path)]=hashlib.sha256(backend_raw).hexdigest()
    proof.update({"experimental_only":True,"pathfind_batch_job_native_direct_preserved":True,"build_manifest_changed":False,"mission_run":False,"compile_status":"unverified; generator controls only, no source-host build or mission run"})
    proof["source_host_cell_pair_memo"]={"kind":"S026SourceHostCellPairMemo","key":"target x/z plus current x/z only","fixed_context_proof":"Pinned PathfindBatchJob source must match exact context initialization and sole UnitPathPlacementValidation.CanPlaceForPathing call anchors; every other argument is constant for that synchronous Execute.","job_source_sha256":hashlib.sha256(job_raw).hexdigest(),"backend_source_sha256":hashlib.sha256(backend_raw).hexdigest(),"generated_output_sha256":outputs,"capacity":4096,"full_cache":"misses call original validator and bypass insertion after capacity","shadow":"all hits recompute original; mismatch is counted, cache cleared, and exception thrown; matching hit returns original result","scope":"one synchronous Execute; adapter begins scope per Execute and always ends it in finally","concurrency":"assumes no concurrent writer mutates the placement inputs during a synchronous Execute; no locking or native synchronization support","native_support":"none; source-host-only managed dictionary overlay; no native/Burst support","telemetry":"per Execute aggregate hit/miss/fallback/mismatch counts"}
    proof["source_pins"][str(backend_path)]=hashlib.sha256(backend_raw).hexdigest()
    (output/"source-extraction.json").write_text(json.dumps(proof,indent=2)+"\n")
    return proof

if __name__=="__main__":
    p=argparse.ArgumentParser();p.add_argument("--repo",type=Path,default=ROOT);p.add_argument("--output",type=Path,required=True);a=p.parse_args();print(json.dumps(generate(a.repo,a.output),indent=2))
