#!/usr/bin/env python3
"""Validate fail-closed no-native-visual cleanup around extracted original owners."""
import hashlib,json,os,subprocess,shutil
from pathlib import Path
R=Path(__file__).resolve().parents[5]; OLD=R/"T Logs/SkirmishProduction/s004-full-spawn-ledger-probe-20261104-T2"; OUT=R/"T Logs/SkirmishProduction/s004-cleanup-boundary-control-20261104-T1"; BUILD=R/"T Logs/SkirmishProduction/s004-building-definition-source-build-20261104-T1/build.json"; TEST=R/"Tools/CI/SkirmishSourceSimulation/Tests/S004CleanupBoundaryControl.cs"
def sha(p):return hashlib.sha256(Path(p).read_bytes()).hexdigest()
def main():
 OUT.mkdir(parents=True,exist_ok=False); build=json.loads(BUILD.read_text()); prior=json.loads((OLD/"receipt.json").read_text()); pins=build["source_and_reference_pins"]
 if prior.get("status")!="PassedCompileOnly" or sha(OLD/"S004FullSpawnLedgerProbe.cs")!=prior["probe_sha256"] or sha(OLD/"S004OriginalCleanup.cs")!=prior["cleanup_sha256"]:raise ValueError("checked owner probe required")
 def check():
  for p,h in pins.items():
   if sha(p)!=h:raise ValueError("source pin changed: "+p)
 check(); before={str(p):sha(p) for p in (BUILD,TEST,OLD/"receipt.json",OLD/"S004FullSpawnLedgerProbe.cs",OLD/"S004OriginalCleanup.cs")}
 probe=(OLD/"S004FullSpawnLedgerProbe.cs").read_text(); anchor="            SkirmishEnemyConstructionAdapter.CleanupAttempt(em, sessionId);"
 if probe.count(anchor)!=1:raise ValueError("original cleanup anchor changed")
 probe=probe.replace(anchor,"            S004VisualCleanupBoundary.Preflight(em, sessionId);\n"+anchor)
 target=OUT/"S004FullSpawnLedgerProbe.cs";target.write_text(probe)
 cleanup=(OLD/"S004OriginalCleanup.cs").read_text();anchor="internal static class S004VisualCleanupBoundary { internal static void DestroyVisual"
 if cleanup.count(anchor)!=1:raise ValueError("visual boundary anchor changed")
 preflight="""internal static class S004VisualCleanupBoundary { internal static void Preflight(EntityManager em, FixedString64Bytes id) {
 using var q=em.CreateEntityQuery(new EntityQueryDesc { All=new[]{ComponentType.ReadOnly<SkirmishAttemptOwnedComponent>()}, Options=EntityQueryOptions.IncludeDisabledEntities|EntityQueryOptions.IgnoreComponentEnabledState });
 using var rows=q.ToEntityArray(Allocator.Temp); foreach(var e in rows) if(em.GetComponentData<SkirmishAttemptOwnedComponent>(e).SessionId.Equals(id) && em.HasComponent<SkirmishVisualInstanceRecord>(e)) throw new NotSupportedException("Native visual instance cleanup unsupported in source host"); }
 internal static void DestroyVisual"""
 cleanup=cleanup.replace(anchor,preflight); cfile=OUT/"S004OriginalCleanup.cs";cfile.write_text(cleanup)
 args=[json.loads(x) for x in (OLD/"compile.rsp").read_text().splitlines() if x.strip()];exe=OUT/"S004CleanupBoundaryControl.exe"
 args=[str(target) if x==str(OLD/"S004FullSpawnLedgerProbe.cs") else str(cfile) if x==str(OLD/"S004OriginalCleanup.cs") else "-out:"+str(exe) if x.startswith("-out:") else "-target:exe" if x=="-target:library" else x for x in args];args.extend(("-main:S004CleanupBoundaryControl",str(TEST)));rsp=OUT/"compile.rsp";rsp.write_text("\n".join(json.dumps(x) for x in args)+"\n")
 dotnet=shutil.which("dotnet");csc=next(x for x in pins if x.endswith("/Roslyn/bincore/csc.dll")); c=subprocess.run([dotnet,csc,"@"+str(rsp)],capture_output=True,text=True,timeout=180);(OUT/"compile.log").write_text(c.stdout+c.stderr)
 if c.returncode:raise RuntimeError("compile failed")
 runtime=build["runtime_selection"];env=os.environ.copy();env["MONO_PATH"]=os.pathsep.join((str(Path(build["placement_build"]).parent),runtime["engine"],str(Path(runtime["newtonsoft"]).parent),str(R/"Library/ScriptAssemblies"),str(OUT)))
 x=subprocess.run([runtime["mono"],str(exe)],cwd=R,env=env,capture_output=True,text=True,timeout=120);(OUT/"run.log").write_text(x.stdout+x.stderr)
 marker="[S004CleanupBoundaryControl] result=Passed unsupportedVisualAtomic=1 ordinaryOriginalCleanup=1 nativeVisualParity=0"
 if x.returncode or x.stdout.splitlines().count(marker)!=1:raise RuntimeError("cleanup control failed")
 check()
 if before!={str(p):sha(p) for p in (BUILD,TEST,OLD/"receipt.json",OLD/"S004FullSpawnLedgerProbe.cs",OLD/"S004OriginalCleanup.cs")}:raise ValueError("input drift")
 receipt={"status":"PassedSourceCleanupBoundaryControl","inputs":before,"build_pins_rechecked":len(pins),"probe_sha256":sha(target),"cleanup_sha256":sha(cfile),"run_log_sha256":sha(OUT/"run.log"),"unsupported_visual_preflight_before_cleanup":True,"ordinary_original_cleanup_fixture_passed":True,"native_visual_parity":False,"mission_ready":False};(OUT/"receipt.json").write_text(json.dumps(receipt,indent=2)+"\n");print(json.dumps(receipt))
if __name__=="__main__":main()
