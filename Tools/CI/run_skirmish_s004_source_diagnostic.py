#!/usr/bin/env python3
"""Run the checked S004 startup diagnostic; exit 2 means no mission admission."""
import argparse,hashlib,json,os,signal,subprocess,sys,time
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]; RUNNER=ROOT/"Tools/CI/SkirmishSourceSimulation/Recipes/s004_source_graph/run_s004_bundle_driven_startup.py"
EXPECTED={"0d86a50b081a46415cbcab163f9e0bde0c9074eb922374c25f497f9d0e9b0af4",
          "5ebddd9cfaeeaa25e1f2b6a7f69e2bb8d04412f2b0254d2e5c5a4bac90aa415c",
          "4ed5f9620309bd32fd636b90a977c89fe7849e019eeca7fcb1d87e078e280550"}
def digest(path):return hashlib.sha256(Path(path).read_bytes()).hexdigest()
def classify_resource_diagnostic(result,environ):
 direct=environ.get("WARLINE_S004_DIRECT_RESOURCE_OWNER_DIAGNOSTIC")=="1"
 cadence=environ.get("WARLINE_S004_RESOURCE_CADENCE_DIAGNOSTIC")=="1"
 if direct and cadence:raise ValueError("Conflicting S004 resource diagnostics")
 direct_result=result.get("direct_resource_owner_diagnostic")
 cadence_result=result.get("resource_cadence_diagnostic")
 if (direct_result is not None)!=direct or (cadence_result is not None)!=cadence:raise ValueError("S004 resource diagnostic request and observed result differ")
 if direct:
  if not isinstance(direct_result,dict) or direct_result.get("scope")!="DirectSourceOwnerDiagnostic_OutsideInactiveScheduler" or direct_result.get("native_cadence_qualified") is not False:raise ValueError("Invalid direct S004 resource diagnostic")
  return "DirectResourceOwner"
 if cadence:
  if not isinstance(cadence_result,dict) or cadence_result.get("scope")!="DirectSourceCadenceAndOwner_OutsideInactiveScheduler" or cadence_result.get("native_cadence_qualified") is not False:raise ValueError("Invalid S004 resource cadence diagnostic")
  return "ResourceCadenceAndOwner"
 return None
def main():
 if any(os.environ.get(name)=="1" for name in ("WARLINE_S004_SOURCE_ROAD_DIAGNOSTIC","WARLINE_S004_SOURCE_GAMEPLAY_DIAGNOSTIC","WARLINE_S004_SOURCE_BANK_DIAGNOSTIC","WARLINE_S004_SOURCE_LAUNCH_DIAGNOSTIC","WARLINE_S004_SOURCE_OWNER_INITIALIZATION","WARLINE_S004_SOURCE_AIR_OWNER_DIAGNOSTIC","WARLINE_S004_SOURCE_TRANSPORT_OWNER_DIAGNOSTIC","WARLINE_S004_SOURCE_AIRDROP_OWNER_DIAGNOSTIC","WARLINE_S004_SOURCE_SUPPORT_OWNER_DIAGNOSTIC","WARLINE_S004_SOURCE_EXPERIMENT_TICK","WARLINE_S004_SOURCE_MISSILE_TICK","WARLINE_S004_SOURCE_SCAN_ROPE_TICK")):raise ValueError("S004 source owner experiments require the explicit source runner")
 p=argparse.ArgumentParser(description=__doc__);p.add_argument("--build-receipt",required=True);p.add_argument("--data-bundle",required=True);p.add_argument("--data-bundle-sha256",required=True);p.add_argument("--output",required=True);p.add_argument("--timeout",type=float,default=300);a=p.parse_args()
 output=Path(a.output).absolute();output.mkdir(parents=True,exist_ok=False); start=time.monotonic(); runner_hash=digest(RUNNER); bundle=Path(a.data_bundle); worker_exit=None; timed_out=False; worker_started=False; status="InputRejected"
 try:
  if not bundle.is_absolute() or bundle.is_symlink() or not bundle.is_file() or a.data_bundle_sha256 not in EXPECTED or digest(bundle)!=a.data_bundle_sha256:raise ValueError("Exact checked S004 source-diagnostic bundle required")
  if not (0<a.timeout<=1800):raise ValueError("Bounded positive timeout required")
  cmd=[sys.executable,"-B",str(RUNNER),"--build",str(Path(a.build_receipt).resolve(strict=True)),"--output",str(output/"worker"),"--bundle",str(bundle),"--bundle-sha256",a.data_bundle_sha256]
  status="WorkerFailed";process=subprocess.Popen(cmd,cwd=ROOT,stdout=subprocess.PIPE,stderr=subprocess.PIPE,text=True,start_new_session=True);worker_started=True
  try: stdout,stderr=process.communicate(timeout=a.timeout)
  except subprocess.TimeoutExpired:
   timed_out=True;status="TimedOut"
   if process.poll() is None:
    try:
     if os.getpgid(process.pid)==process.pid:os.killpg(process.pid,signal.SIGTERM)
    except ProcessLookupError:pass
   try:stdout,stderr=process.communicate(timeout=5)
   except subprocess.TimeoutExpired:
    if process.poll() is None:
     try:
      if os.getpgid(process.pid)==process.pid:os.killpg(process.pid,signal.SIGKILL)
     except ProcessLookupError:pass
    stdout,stderr=process.communicate()
  worker_exit=process.returncode;(output/"worker.log").write_text(stdout+stderr)
  if timed_out:raise TimeoutError("S004 diagnostic worker timed out; see worker.log")
  if worker_exit!=0:raise RuntimeError("S004 diagnostic worker failed; see worker.log")
  worker=output/"worker/receipt.json"; data=json.loads(worker.read_text()); result=data["result"]
  if data.get("status")!="ObservedDiagnosticSourcePlacement" or data.get("mission_ready") is not False or data.get("runtime_admission") is not False or result.get("source_bundle_preflight_passed_before_world") is not True or result.get("source_simulation_active")!=0:raise ValueError("Unqualified startup result required")
  resource_diagnostic=classify_resource_diagnostic(result,os.environ)
  if digest(RUNNER)!=runner_hash or digest(bundle)!=a.data_bundle_sha256:raise ValueError("Source or bundle changed during diagnostic")
  receipt={"status":"SourceStartupOwnerDiagnosticObserved" if resource_diagnostic else "SourceStartupDiagnosticObserved","mission":"S004","worker_exit":worker_exit,"worker_receipt":{"path":str(worker),"sha256":digest(worker)},"worker_log_sha256":digest(output/"worker.log"),"runner_sha256":runner_hash,"bundle_sha256":a.data_bundle_sha256,"resource_owner_diagnostic":resource_diagnostic,"wall_seconds":time.monotonic()-start,"scheduler_executed":False,"validation_passed":False,"mission_victory":None,"mission_ready":False};(output/"run-receipt.json").write_text(json.dumps(receipt,indent=2)+"\n");print(json.dumps(receipt));return 2
 except Exception as e:
  failure={"status":status,"mission":"S004","reason":str(e),"worker_started":worker_started,"worker_exit":worker_exit,"timed_out":timed_out,"wall_seconds":time.monotonic()-start,"scheduler_executed":False,"validation_passed":False,"mission_victory":None,"mission_ready":False}
  if (output/"worker.log").exists():failure["worker_log_sha256"]=digest(output/"worker.log")
  (output/"failure-receipt.json").write_text(json.dumps(failure,indent=2)+"\n");print(str(e),file=sys.stderr);return 1
if __name__=="__main__":
 try:sys.exit(main())
 except Exception as e:print(str(e),file=sys.stderr);sys.exit(1)
