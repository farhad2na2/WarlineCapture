#!/usr/bin/env python3
"""Measure original source placement on a blocked-grid fixture."""
import argparse, hashlib, json, os, shutil, subprocess
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4]
HARNESS=ROOT/"Tools/CI/SkirmishSourceSimulation/Tests/S003TerrainPlacementControl.cs"
MARKER="[S003TerrainPlacementControl] result=Observed open=1 blocked="
def sha(path): return hashlib.sha256(Path(path).read_bytes()).hexdigest()
def main():
 p=argparse.ArgumentParser();p.add_argument("--build",required=True);p.add_argument("--output",required=True);p.add_argument("--expected-blocked",required=True,choices=("0","1"));a=p.parse_args()
 out=Path(a.output).absolute();out.mkdir(parents=True,exist_ok=False);build_path=Path(a.build).resolve(strict=True);b=json.loads(build_path.read_bytes())
 if b.get("status")!="Passed" or b.get("mission")!="S003" or b.get("compile_exit")!=0:raise ValueError("Passed S003 source build required")
 pins=b["source_and_reference_pins"];control_pin=sha(HARNESS)
 def verify():
  for name,digest in pins.items():
   if sha(name)!=digest:raise ValueError("Source build pin changed: "+name)
  if sha(HARNESS)!=control_pin:raise ValueError("Harness changed")
 verify();csc=next(Path(name) for name in pins if name.endswith("/Roslyn/bincore/csc.dll"));dotnet=shutil.which("dotnet")
 if not dotnet or not any(Path(name).name=="dotnet" and Path(name).resolve()==Path(dotnet).resolve() for name in pins):raise ValueError("Pinned dotnet required")
 rsp_path=Path(b["compiler_response"]["path"])
 if sha(rsp_path)!=b["compiler_response"]["sha256"]:raise ValueError("Response pin changed")
 exe=out/"S003TerrainPlacementControl.exe";args=[json.loads(x) for x in rsp_path.read_text().splitlines() if x.strip()]
 args=["-main:S003TerrainPlacementControl" if x.startswith("-main:") else "-out:"+str(exe) if x.startswith("-out:") else x for x in args];args.append(str(HARNESS));rsp=out/"compile.rsp";rsp.write_text("\n".join(json.dumps(x) for x in args)+"\n")
 c=subprocess.run([dotnet,str(csc),"@"+str(rsp)],capture_output=True,text=True,timeout=300);(out/"compile.log").write_text(c.stdout+c.stderr)
 if c.returncode:raise RuntimeError("Control compile failed")
 runtime=b["runtime_selection"];env=os.environ.copy();env["MONO_PATH"]=os.pathsep.join((str(build_path.parent/"placement"),runtime["engine"],str(Path(runtime["newtonsoft"]).parent),str(ROOT/"Library/ScriptAssemblies"),str(out)))
 r=subprocess.run([runtime["mono"],str(exe)],cwd=ROOT,env=env,capture_output=True,text=True,timeout=300);(out/"run.log").write_text(r.stdout+r.stderr)
 if r.returncode or (MARKER+a.expected_blocked+" outside=0 fixtureOnly=1 readiness=0") not in r.stdout:raise RuntimeError("Control failed or expected marker missing")
 verify();receipt={"status":"ObservedSourcePlacementFixture","build_sha256":sha(build_path),"harness_sha256":control_pin,"source_pin_count":len(pins),"source_pins_before_after":True,"open_cell_accepted":True,"blocked_cell_accepted":a.expected_blocked=="1","out_of_bounds_accepted":False,"fixture_only":True,"native_input_observed":False,"mission_ready":False};(out/"receipt.json").write_text(json.dumps(receipt,indent=2)+"\n");print(json.dumps(receipt))
if __name__=="__main__":main()
