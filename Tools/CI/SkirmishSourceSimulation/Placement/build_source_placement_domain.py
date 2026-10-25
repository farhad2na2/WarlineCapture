"""Build the existing CLR placement/data facade from current source; no Unity launch."""
from pathlib import Path
import argparse,hashlib,json,subprocess,shutil,sys,time
sys.dont_write_bytecode=True
from regenerate_placement_numeric import generate
def build(repo,engine,output):
 repo=Path(repo).resolve();engine=Path(engine).resolve();out=Path(output).absolute()
 if out.exists():raise ValueError("New placement build directory required")
 out.mkdir(parents=True);root=Path(__file__).resolve().parent
 proof=generate(repo,out/"generated")
 script=engine.parents[1];library=repo/"Library/ScriptAssemblies";newton=next((repo/"Library/PackageCache").glob("com.unity.nuget.newtonsoft-json@*/Runtime/Newtonsoft.Json.dll"))
 refs=[script/"NetStandard/ref/2.1.0/netstandard.dll",script/"NetStandard/compat/2.1.0/shims/netfx/mscorlib.dll",engine/"UnityEngine.CoreModule.dll",engine/"UnityEngine.MathematicsModule.dll",newton]+[library/n for n in ["Game.Components.dll","Game.Rendering.Contracts.dll","Unity.Mathematics.dll","Unity.Collections.dll","Unity.Entities.dll","Unity.Burst.dll"]]
 sources=[root/"SourcePackedCampaignGeometry.cs",repo/"Assets/Game/Scripts/Simulation/Mathematics/SharedPlacementGeometry.cs",out/"generated/SharedSidewalkGeometryKernel.cs",out/"generated/SharedWaterGeometryKernel.cs",root/"SourcePlacementDomain.cs"]
 dot=Path(shutil.which("dotnet")).resolve();sdk=sorted((dot.parent/"sdk").glob("*/Roslyn/bincore/csc.dll"))[-1];pins={str(p):hashlib.sha256(p.read_bytes()).hexdigest()for p in refs+sources+[dot,sdk,Path(__file__),root/"regenerate_placement_numeric.py",root.parents[1]/"skirmish_source_syntax.py"]}
 pins.update(proof["source_inputs"]);args=["-nologo","-unsafe+","-nostdlib+","-langversion:latest","-target:library","-out:"+str(out/"SourcePlacementDomain.dll")]+["-r:"+str(p)for p in refs]+[str(p)for p in sources]
 (out/"compile.rsp").write_text("\n".join(json.dumps(x)for x in args)+"\n");start=time.monotonic();r=subprocess.run([str(dot),str(sdk),"@"+str(out/"compile.rsp")],capture_output=True,text=True,timeout=120);(out/"compile.log").write_text(r.stdout+r.stderr)
 changed=[p for p,h in pins.items()if hashlib.sha256(Path(p).read_bytes()).hexdigest()!=h]
 result={"schema_version":1,"kind":"CurrentSourcePlacementDomainBuild","compile_exit":r.returncode,"source_and_reference_pins":pins,"input_drift":changed,"wall_seconds":time.monotonic()-start,"generated_numeric_proof":proof,"scope":"Existing CLR/data facade, current shared placement kernel and dynamically extracted current numeric loops. No native world/visual/source-scene rebuild claim."}
 if r.returncode or changed:result["status"]="Failed";(out/"build.json").write_text(json.dumps(result,indent=2));raise ValueError("Placement compile/source admission failed: "+r.stdout+r.stderr+str(changed))
 result.update(status="Passed",dll=str(out/"SourcePlacementDomain.dll"),dll_sha256=hashlib.sha256((out/"SourcePlacementDomain.dll").read_bytes()).hexdigest());(out/"build.json").write_text(json.dumps(result,indent=2));return result
if __name__=="__main__":
 p=argparse.ArgumentParser();p.add_argument("--repo",required=True);p.add_argument("--engine",required=True);p.add_argument("--output",required=True);a=p.parse_args();print(json.dumps(build(a.repo,a.engine,a.output),indent=2))
