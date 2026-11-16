#!/usr/bin/env python3
"""Compile and exercise the shared C# source-only S004 bundle reader."""
import hashlib,json,os,shutil,subprocess
from pathlib import Path
R=Path(__file__).resolve().parents[5];B=R/"T Logs/SkirmishProduction/s004-resource-census-source-build-20261104-T1/build.json";S=R/"Tools/CI/SkirmishSourceSimulation/Adapters/DeclaredGround/SourceS004BundlePreflight.cs";D=R/"T Logs/SkirmishProduction/s004-source-diagnostic-bundle-current-20261104-T1/bundle.json";O=R/"T Logs/SkirmishProduction/s004-bundle-current-preflight-20261104-T2"
def sha(p):return hashlib.sha256(Path(p).read_bytes()).hexdigest()
def main():
 O.mkdir(parents=True,exist_ok=False); build=json.loads(B.read_text()); pins=build["source_and_reference_pins"]
 if build.get("status")!="Passed" or build.get("compile_exit")!=0:raise ValueError("Passed build required")
 def check():
  for path,value in pins.items():
   if sha(path)!=value:raise ValueError("Source pin changed: "+path)
 check();before={str(p):sha(p) for p in (B,S,D)};response=Path(build["compiler_response"]["path"])
 if sha(response)!=build["compiler_response"]["sha256"]:raise ValueError("Compiler response changed")
 args=[json.loads(line) for line in response.read_text().splitlines() if line.strip()];exe=O/"S004BundlePreflight.exe"
 args=["-out:"+str(exe) if value.startswith("-out:") else "-main:SourceS004BundlePreflight" if value.startswith("-main:") else value for value in args];args.append(str(S));rsp=O/"compile.rsp";rsp.write_text("\n".join(json.dumps(v) for v in args)+"\n")
 dotnet=shutil.which("dotnet");csc=next(p for p in pins if p.endswith("/Roslyn/bincore/csc.dll"));compiled=subprocess.run([dotnet,csc,"@"+str(rsp)],capture_output=True,text=True,timeout=180);(O/"compile.log").write_text(compiled.stdout+compiled.stderr)
 if compiled.returncode:raise RuntimeError("Shared C# compile failed")
 runtime=build["runtime_selection"];env=os.environ.copy();env["MONO_PATH"]=os.pathsep.join((str(Path(build["placement_build"]).parent),runtime["engine"],str(Path(runtime["newtonsoft"]).parent),str(R/"Library/ScriptAssemblies"),str(O)))
 def run(name,path,digest):
  output=O/(name+".json");x=subprocess.run([runtime["mono"],str(exe),str(R),str(path),digest,str(output)],cwd=R,env=env,capture_output=True,text=True,timeout=180);(O/(name+".log")).write_text(x.stdout+x.stderr);return x,output
 ok,result=run("positive",D,sha(D));marker="[S004SourceBundlePreflight] result=Passed inputs=11 world=0 admission=0 readiness=0"
 if ok.returncode or ok.stdout.splitlines().count(marker)!=1:raise RuntimeError("Positive preflight failed")
 parsed=json.loads(result.read_text());assert parsed["world_created"] is False and parsed["mission_ready"] is False
 tests={}
 for name,mutation in (("qualification",lambda j:j["qualification"].__setitem__("mission_ready",True)),("input_hash",lambda j:j["inputs"]["authored_grid"].__setitem__("sha256","0"*64)),("missing_descriptor",lambda j:j["inputs"].pop("authored_grid")),("unknown_evidence",lambda j:j["evidence"].__setitem__("sha256","0"*64)),("evidence_alias",lambda j:j["evidence"].__setitem__("path",j["evidence"]["path"]+".alias"))):
  doc=json.loads(D.read_text());mutation(doc);path=O/(name+"-bundle.json");path.write_text(json.dumps(doc)+"\n");x,out=run(name,path,sha(path));tests[name]={"exit":x.returncode,"output_created":out.exists()}
  if x.returncode==0 or out.exists():raise RuntimeError("Negative control accepted: "+name)
 check()
 if before!={str(p):sha(p) for p in (B,S,D)}:raise ValueError("Input drift")
 receipt={"status":"PassedSharedCSharpBundlePreflight","input_sha256":before,"source_pins_rechecked":len(pins),"positive_result_sha256":sha(result),"negative_controls":tests,"world_created":False,"runtime_admission":False,"mission_ready":False,"compile_log_sha256":sha(O/"compile.log"),"run_log_sha256":sha(O/"positive.log")};(O/"receipt.json").write_text(json.dumps(receipt,indent=2)+"\n");print(json.dumps(receipt))
if __name__=="__main__":main()
