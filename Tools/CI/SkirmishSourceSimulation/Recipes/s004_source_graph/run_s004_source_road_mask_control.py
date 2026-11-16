#!/usr/bin/env python3
"""Project the pinned S004 base surface with the original road-mask owner."""
import hashlib,json,os,shutil,subprocess
from pathlib import Path
R=Path(__file__).resolve().parents[5]
D=R/"T Logs/SkirmishProduction/s004-current-physical-input-projection-20261104-T1"
B=R/"T Logs/SkirmishProduction/s004-building-definition-source-build-20261104-T1/build.json"
C=R/"Tools/CI/SkirmishSourceSimulation/Tests/S004SourceRoadMaskControl.cs"
O=R/"T Logs/SkirmishProduction/s004-current-source-road-mask-20261104-T2"
def sha(p): return hashlib.sha256(Path(p).read_bytes()).hexdigest()
def main():
 O.mkdir(parents=True,exist_ok=False)
 build=json.loads(B.read_text()); source=json.loads((D/"receipt.json").read_text()); graph=Path(source["extended_graph"]["path"])
 if build.get("status")!="Passed" or build.get("compile_exit")!=0 or source.get("status")!="Passed" or sha(graph)!=source["extended_graph"]["sha256"]: raise ValueError("source evidence invalid")
 pins=build["source_and_reference_pins"]
 def check():
  for path,digest in pins.items():
   if sha(path)!=digest: raise ValueError("source pin changed: "+path)
  for row in source["inputs"]:
   if sha(row["path"])!=row["sha256"]: raise ValueError("physical input changed")
 check(); initial={str(p):sha(p) for p in (B,C,graph,D/"receipt.json",D/"mono-config.xml")}
 rsp0=Path(build["compiler_response"]["path"]); assert sha(rsp0)==build["compiler_response"]["sha256"]
 exe=O/"S004SourceRoadMaskControl.exe"; lines=[]
 for line in rsp0.read_text().splitlines():
  v=line.strip().strip(chr(34))
  if v.startswith("-main:"): line='"-main:S004SourceRoadMaskControl"'
  elif v.startswith("-out:"): line='"-out:'+str(exe)+'"'
  lines.append(line)
 lines.append('"'+str(C)+'"'); rsp=O/"compile.rsp"; rsp.write_text("\n".join(lines)+"\n")
 csc=next(p for p in pins if p.endswith("/Roslyn/bincore/csc.dll")); dotnet=shutil.which("dotnet")
 if not dotnet or not any(Path(p).name=="dotnet" and Path(p).resolve()==Path(dotnet).resolve() for p in pins): raise ValueError("dotnet pin mismatch")
 c=subprocess.run([dotnet,csc,"@"+str(rsp)],capture_output=True,text=True,timeout=180); (O/"compile.log").write_text(c.stdout+c.stderr)
 if c.returncode: raise RuntimeError("compile failed")
 runtime=build["runtime_selection"]; env=os.environ.copy(); env["MONO_PATH"]=os.pathsep.join((str(Path(build["placement_build"]).parent),runtime["engine"],str(Path(runtime["newtonsoft"]).parent),str(R/"Library/ScriptAssemblies"),str(O)))
 out=O/"base-surface-roads.u8"; x=subprocess.run([runtime["mono"],"--config",str(D/"mono-config.xml"),str(exe),str(graph),str(out)],cwd=R,env=env,capture_output=True,text=True,timeout=180); (O/"run.log").write_text(x.stdout+x.stderr)
 marker="[S004SourceRoadMaskControl] result=Passed roadCells=23500 sceneOverlay=unavailable sidewalk=unknown readiness=0"
 if x.returncode or x.stdout.splitlines().count(marker)!=1: raise RuntimeError("road owner failed")
 raw=out.read_bytes()
 if len(raw)!=2048*1024 or set(raw)!={0,1} or sum(raw)!=23500 or sha(out)!="5ee089b3aac011e33600f9181a35e0e2ddeff04411519ce228f96c30b08ad036": raise ValueError("original owner output differs from retained independent capture")
 check()
 if initial!={str(p):sha(p) for p in (B,C,graph,D/"receipt.json",D/"mono-config.xml")}: raise ValueError("input drift")
 receipt={"status":"PassedSourceOnlyBaseRoadProjection","input_sha256":initial,"source_build_pins_rechecked":len(pins),"road_cells":sum(raw),"road_mask_sha256":sha(out),"same_owner_independent_capture_match":True,"scene_overlays":"unavailable","sidewalk":"unknown","water":"unknown","physical_source_validated":False,"placement_attempted":False,"mission_ready":False,"compile_log_sha256":sha(O/"compile.log"),"run_log_sha256":sha(O/"run.log")}
 (O/"receipt.json").write_text(json.dumps(receipt,indent=2)+"\n"); print(json.dumps(receipt))
if __name__=="__main__": main()
