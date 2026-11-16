import importlib.util,json,copy,hashlib,argparse,tempfile,shutil
from pathlib import Path
parser=argparse.ArgumentParser();parser.add_argument("--s002-receipt",required=True);parser.add_argument("--s003-prefix",required=True);parser.add_argument("--s003-terminal",required=True);parser.add_argument("--qualification",required=True);parser.add_argument("--bundle",required=True);args=parser.parse_args()
p=Path(__file__).with_name("run_skirmish_source_batch.py");spec=importlib.util.spec_from_file_location("batch",p);m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
path=Path(args.s002_receipt);before=hashlib.sha256(path.read_bytes()).hexdigest();r=json.loads(path.read_text());job={"mission":"S002","seed":104731};process={"cli_exit":1,"outer_timeout":False};out=m.classify(job,process,r);assert out["execution_complete"] and out["status"]=="DiagnosticSourceOutcomeObserved" and out["diagnostic_source_outcome"]=="Victory" and out["mission_victory"] is None and not out["validation_passed"]
for field in ["camera_qualified","active_mission_water_qualified","runtime_presentation_qualified","source_fit_certified","native_acceptance","mission_ready"]:
 for value in [True,None]:
  bad=copy.deepcopy(r);bad[field]=value;assert not m.classify(job,process,bad)["execution_complete"],field
for field,value in [("diagnostic_only",False),("validation_passed",True),("timed_out",True),("seed",1)]:
 bad=copy.deepcopy(r);bad[field]=value;assert not m.classify(job,process,bad)["execution_complete"],field

for invalid in [1,0,"true",None]:
 bad=copy.deepcopy(r);bad["diagnostic_only"]=invalid;bad["validation_passed"]=True;assert not m.classify(job,{"cli_exit":0,"outer_timeout":False},bad)["execution_complete"]
prefix=copy.deepcopy(r);prefix.update(status="BoundedPrefix",source_result={"status":"BoundedPrefix","source_outcome":"None"},full_outcome_observed=False,diagnostic_prefix_completed=True);prefixjob=dict(job,max_ticks=1);assert m.classify(prefixjob,{"cli_exit":0,"outer_timeout":False},prefix)["execution_complete"];prefix["camera_qualified"]=True;assert not m.classify(prefixjob,{"cli_exit":0,"outer_timeout":False},prefix)["execution_complete"]
legacy_path=Path(args.s003_prefix);legacy_hash=hashlib.sha256(legacy_path.read_bytes()).hexdigest();legacy=json.loads(legacy_path.read_text());legacy_job={"mission":"S003","seed":104732,"max_ticks":1};prefix_process={"cli_exit":0,"outer_timeout":False};assert m.classify(legacy_job,prefix_process,legacy)["execution_complete"]
for key in ["runtime_presentation_qualified","mission_ready"]:
 bad=copy.deepcopy(legacy);bad[key]=True;assert not m.classify(legacy_job,prefix_process,bad)["execution_complete"]
for key in ["camera_qualified","active_mission_water_qualified","source_fit_certified","native_acceptance"]:
 bad=copy.deepcopy(legacy);bad.pop(key);assert not m.classify(legacy_job,prefix_process,bad)["execution_complete"]
terminal_path=Path(args.s003_terminal);terminal_hash=hashlib.sha256(terminal_path.read_bytes()).hexdigest();terminal=json.loads(terminal_path.read_text());actual=m.classify(legacy_job,{"cli_exit":1,"outer_timeout":False},terminal);assert actual["execution_complete"] and actual["mission_victory"] is None and actual["validation_passed"] is False and set(actual["unqualified_fields_missing"])=={"runtime_presentation_qualified","mission_ready"}
for key in ["runtime_presentation_qualified","mission_ready","camera_qualified","active_mission_water_qualified","source_fit_certified","native_acceptance"]:
 bad=copy.deepcopy(terminal);bad[key]=True;assert not m.classify(legacy_job,{"cli_exit":1,"outer_timeout":False},bad)["execution_complete"]
bad=copy.deepcopy(terminal);bad["diagnostic_only"]=1;assert not m.classify(legacy_job,{"cli_exit":1,"outer_timeout":False},bad)["execution_complete"];assert hashlib.sha256(terminal_path.read_bytes()).hexdigest()==terminal_hash;assert hashlib.sha256(legacy_path.read_bytes()).hexdigest()==legacy_hash
defeat=copy.deepcopy(r);defeat["source_result"]["source_outcome"]="Defeat";assert m.classify(job,process,defeat)["diagnostic_source_outcome"]=="Defeat";defeat["camera_qualified"]=True;assert not m.classify(job,process,defeat)["execution_complete"];assert not m.classify(job,{"cli_exit":0,"outer_timeout":False},r)["execution_complete"];assert hashlib.sha256(path.read_bytes()).hexdigest()==before
with tempfile.TemporaryDirectory(prefix="batch-qualified-input-") as t:
 root=Path(t).resolve();q=root/"qualification.json";shutil.copyfile(args.qualification,q);bundle=Path(args.bundle).resolve();pin=lambda f:{"path":str(f),"sha256":hashlib.sha256(f.read_bytes()).hexdigest()};manifest=root/"jobs.json";document={"schema_version":1,"jobs":[{"id":"s003","mission":"S003","seed":104732,"data_bundle":pin(bundle),"configuration_qualification":pin(q),"timeout":5}]};manifest.write_text(json.dumps(document));jobs,pins=m.load_jobs(manifest,root);assert jobs[0]["configuration_qualification"]["path"]==str(q) and str(q) in pins;m.verify(pins)
 file_link=root/"qualification-link.json";file_link.symlink_to(q);directory=root/"real-directory";directory.mkdir();real_child=directory/"qualification.json";shutil.copyfile(q,real_child);directory_link=root/"directory-link";directory_link.symlink_to(directory,target_is_directory=True)
 for candidate in [file_link,directory_link/"qualification.json"]:
  bad=copy.deepcopy(document);bad["jobs"][0]["configuration_qualification"]=pin(candidate);manifest.write_text(json.dumps(bad))
  try:m.load_jobs(manifest,root)
  except ValueError:pass
  else:raise AssertionError("symlinked qualification accepted")
 manifest.write_text(json.dumps(document));q.write_text(q.read_text()+" ");
 try:m.verify(pins)
 except ValueError:pass
 else:raise AssertionError("changed qualification not rejected")
 try:m.load_jobs(manifest,root)
 except ValueError:pass
 else:raise AssertionError("qualification hash mismatch accepted")
print("[S002DiagnosticBatchControls] result=Passed actual_s002_receipt=1 actual_s003_legacy_prefix=1 diagnostic_separate=1 false_flags=6 tamper_rejected=1 original_unchanged=1")
