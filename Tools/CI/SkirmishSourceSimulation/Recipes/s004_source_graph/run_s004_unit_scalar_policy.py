#!/usr/bin/env python3
"""Compile and run the current original scalar policy against pinned S004 source data."""
import argparse, hashlib, json, os, shlex, subprocess
from pathlib import Path

ROOT=Path(__file__).resolve().parents[5]; HARNESS=Path(__file__).with_name("S004UnitScalarPolicyControl.cs")
MARKER="[S004UnitScalarPolicyControl] result=Passed rows=3 world=0 native=0 readiness=0"
def sha(path): return hashlib.sha256(Path(path).read_bytes()).hexdigest()
def main():
 p=argparse.ArgumentParser();p.add_argument("--input",type=Path,required=True);p.add_argument("--probe",type=Path,required=True);p.add_argument("--build",type=Path,required=True);p.add_argument("--output",type=Path,required=True);a=p.parse_args()
 inp=a.input.resolve(strict=True); probe=a.probe.resolve(strict=True); bp=a.build.resolve(strict=True); out=a.output.resolve();out.mkdir(parents=True,exist_ok=False)
 data=json.loads(inp.read_bytes()); build=json.loads(bp.read_bytes()); readset=json.loads((probe/"S004BundleDrivenStartup.exe.transform-readset.json").read_bytes()); input_hash=sha(inp)
 if data.get("kind")!="S004SerializedProductionScalarInputs" or build.get("status")!="Passed":raise ValueError("Actual source inputs and passing build required")
 pins=dict(readset["source_pins"])
 for k,v in data["source_pins"].items():
  if k in pins and pins[k]!=v:raise ValueError("Conflicting source epoch: "+k)
  pins[k]=v
 for k,v in pins.items():
  if sha(k)!=v:raise ValueError("Changed source pin: "+k)
 args=[shlex.split(x)[0] for x in (probe/"compile.rsp").read_text().splitlines() if x.strip()];m=[i for i,x in enumerate(args) if x.startswith("-main:")];o=[i for i,x in enumerate(args) if x.startswith("-out:")]
 if len(m)!=1 or len(o)!=1:raise ValueError("Exact compiler response required")
 exe=out/"S004UnitScalarPolicyControl.exe";args[m[0]]="-main:S004UnitScalarPolicyControl";args[o[0]]="-out:"+str(exe);args.append(str(HARNESS));rsp=out/"compile.rsp";rsp.write_text("\n".join(json.dumps(x) for x in args)+"\n")
 bpins=build["source_and_reference_pins"];dotnet=next(x for x in bpins if Path(x).name=="dotnet");csc=next(x for x in bpins if x.endswith("/Roslyn/bincore/csc.dll"));c=subprocess.run([dotnet,csc,"@"+str(rsp)],capture_output=True,text=True,timeout=300);(out/"compile.log").write_text(c.stdout+c.stderr)
 if c.returncode:raise RuntimeError("Original policy source compile failed")
 runtime=build["runtime_selection"];env=os.environ.copy();env["MONO_PATH"]=os.pathsep.join((str(bp.parent/"placement"),runtime["engine"],str(Path(runtime["newtonsoft"]).parent),str(ROOT/"Library/ScriptAssemblies"),str(out)))
 cfg=ROOT/"T Logs/SkirmishProduction/s004-current-physical-input-projection-20261104-T1/mono-config.xml";projection=out/"projection.json";r=subprocess.run([runtime["mono"],"--config",str(cfg),str(exe),str(inp),str(projection)],cwd=ROOT,env=env,capture_output=True,text=True,timeout=300);(out/"run.log").write_text(r.stdout+r.stderr)
 if r.returncode or MARKER not in r.stdout:raise RuntimeError("Original scalar policy run failed")
 if sha(inp)!=input_hash:raise ValueError("Scalar inputs changed during execution")
 for k,v in pins.items():
  if sha(k)!=v:raise ValueError("Source changed during execution: "+k)
 receipt={"status":"PassedSourceOnlyScalarPolicy","input_sha256":input_hash,"projection_sha256":sha(projection),"source_pins_checked":len(pins),"native_prefab_resolution":False,"runtime_admission":False,"mission_ready":False}
 (out/"receipt.json").write_text(json.dumps(receipt,indent=2)+"\n");print(json.dumps(receipt))
if __name__=="__main__":main()
