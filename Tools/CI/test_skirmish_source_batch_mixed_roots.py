import hashlib,importlib.util,json,tempfile
from pathlib import Path
spec=importlib.util.spec_from_file_location("batch",Path(__file__).with_name("run_skirmish_source_batch.py"));m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
def pin(p):return {"path":str(p),"sha256":hashlib.sha256(p.read_bytes()).hexdigest()}
def dump(p,v):p.write_text(json.dumps(v));return p
def arg(a,k):return a[a.index(k)+1]
with tempfile.TemporaryDirectory(prefix="batch-roots-") as t:
 root=Path(t).resolve();jobs=[];roots=[]
 for i in range(2):
  repo=root/("repo"+str(i));repo.mkdir();package=root/("package"+str(i));cli=package/"Tools/CI/run_skirmish_source_simulation.py";cli.parent.mkdir(parents=True);cli.write_text("# controlled cli, not executed\n");data=root/("data"+str(i));data.mkdir();image=root/("image"+str(i));image.write_bytes(b"controlled image, not executed");mission="S002" if i==0 else "S003";seed=104731+i;backend="LegacyGroundS002V1" if i==0 else "TypedGroundS026V1";bundle=dump(root/("bundle"+str(i)),{"mission":mission,"seed":seed,"runtime_backend":backend});build=dump(root/("build"+str(i)),{"kind":"SkirmishSourceSimulationBuild","status":"Passed","compile_exit":0,"repository_root":str(repo),"mission":mission,"seed":seed,"runtime_backend":backend,"source_and_reference_pins":{},"executable":pin(image)});jobs.append({"id":"j"+str(i),"mission":mission,"seed":seed,"data_bundle":pin(bundle),"build_receipt":pin(build),"source_repo":str(repo),"package_root":str(package),"data_repo":str(data),"timeout":1});roots.append((repo,package,data,cli))
 manifest=dump(root/"jobs.json",{"schema_version":1,"jobs":jobs});seen=[]
 def seam(argv,log,timeout):
  i=int(Path(arg(argv,"--output")).parent.name[1:]);job=jobs[i];repo,package,data,cli=roots[i];assert argv[2]==str(cli) and arg(argv,"--repo")==str(repo) and arg(argv,"--package-root")==str(package) and arg(argv,"--data-repo")==str(data);seen.append(i);dest=Path(arg(argv,"--output"));dest.mkdir();receipt={"schema_version":1,"kind":"SkirmishSourceSimulationRunReceipt","mission":job["mission"],"seed":job["seed"],"process_exit":0,"timed_out":False,"status":"SourceOutcomeObserved","source_result":{"status":"SourceTerminal","source_outcome":"Victory"},"full_outcome_observed":True,"diagnostic_prefix_completed":False,"validation_passed":True,"source_repository_root":str(repo),"data_repository_root":str(data),"data_bundle":job["data_bundle"],"build_receipt":job["build_receipt"]};dump(dest/"run-receipt.json",receipt);return {"cli_exit":0,"outer_timeout":False}
 out=m.execute(manifest,root/"out",roots[0][0],roots[0][1],roots[0][2],seam);assert out["all_jobs_execution_complete"] and seen==[0,1]
 def badroot(argv,log,timeout):
  result=seam(argv,log,timeout);p=Path(arg(argv,"--output"))/"run-receipt.json";r=json.loads(p.read_text());r["source_repository_root"]=str(roots[1][0]);dump(p,r);return result
 one=dump(root/"one.json",{"schema_version":1,"jobs":[jobs[0]]});bad=m.execute(one,root/"bad",roots[0][0],roots[0][1],roots[0][2],badroot);assert not bad["all_jobs_execution_complete"] and "roots mismatch" in bad["jobs"][0]["receipt_error"]
 def drift(argv,log,timeout):
  result=seam(argv,log,timeout);roots[1][3].write_text("# changed after launch\n");return result
 changed=m.execute(manifest,root/"drift",roots[0][0],roots[0][1],roots[0][2],drift);assert not changed["all_jobs_execution_complete"] and changed["jobs"][0]["status"]=="InputDrift"
print("[MixedRootBatchControls] result=Passed forwarding=2 receipt_root_rejected=1 other_cli_drift_rejected=1 controlled_only=1")
