"""Compile focused controls against the exact experimental source build."""
import argparse, hashlib, json, os, subprocess
from pathlib import Path

ROOT=Path(__file__).resolve().parents[4]; HARNESS=Path(__file__).with_name("Control.cs")
def sha(p): return hashlib.sha256(Path(p).read_bytes()).hexdigest()
def main():
 p=argparse.ArgumentParser();p.add_argument("--build",required=True);p.add_argument("--output",required=True);a=p.parse_args()
 buildpath=Path(a.build).resolve(strict=True); build=json.loads(buildpath.read_bytes());out=Path(a.output).resolve();out.mkdir(parents=True,exist_ok=False)
 if build.get("status")!="Passed" or build.get("mission")!="S003": raise ValueError("Passing S003 build required")
 pins=build["source_and_reference_pins"]
 for name,digest in pins.items():
  if sha(name)!=digest: raise ValueError("Changed build source: "+name)
 response=Path(build["compiler_response"]["path"])
 if sha(response)!=build["compiler_response"]["sha256"]: raise ValueError("Compiler response changed")
 exe=out/"WitnessCacheControl.exe"; lines=[]
 for line in response.read_text().splitlines():
  value=line.strip().strip('"')
  if value.startswith("-main:"): line='"-main:WitnessCacheControl"'
  elif value.startswith("-out:"): line='"-out:'+str(exe)+'"'
  lines.append(line)
 lines.append('"'+str(HARNESS)+'"');rsp=out/"compile.rsp";rsp.write_text("\n".join(lines)+"\n")
 dotnet=next(n for n in pins if Path(n).name=="dotnet"); csc=next(n for n in pins if n.endswith("/Roslyn/bincore/csc.dll"))
 c=subprocess.run([dotnet,csc,"@"+str(rsp)],capture_output=True,text=True,timeout=300);(out/"compile.log").write_text(c.stdout+c.stderr)
 if c.returncode: raise RuntimeError("Fixture compile failed")
 runtime=build["runtime_selection"];env=os.environ.copy();env["MONO_PATH"]=os.pathsep.join((str(buildpath.parent/"placement"),runtime["engine"],str(Path(runtime["newtonsoft"]).parent),str(ROOT/"Library/ScriptAssemblies"),str(out)))
 run=subprocess.run([runtime["mono"],str(exe)],cwd=ROOT,env=env,capture_output=True,text=True,timeout=300);(out/"run.log").write_text(run.stdout+run.stderr)
 marker="[WitnessCacheControl] result=Passed mutation_alias_copy_identity=Passed native=0 readiness=0"
 if run.returncode or marker not in run.stdout: raise RuntimeError("Fixture failed or marker missing")
 for name,digest in pins.items():
  if sha(name)!=digest: raise ValueError("Build source changed during control: "+name)
 receipt={"status":"PassedSourceOnlyControl","build_sha256":sha(buildpath),"harness_sha256":sha(HARNESS),"source_pins_verified":len(pins),"native_parity":False,"mission_ready":False}
 (out/"receipt.json").write_text(json.dumps(receipt,indent=2)+"\n");print(json.dumps(receipt))
if __name__=="__main__":main()
