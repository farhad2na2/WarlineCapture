import ast,hashlib,json,tempfile,datetime
from pathlib import Path
HERE=Path(__file__).parent
source=(HERE/"requested-CC-controller.py.proposal").read_text();tree=ast.parse(source)
functions=[n for n in tree.body if isinstance(n,ast.FunctionDef) and n.name in {"need","inspect_runtime_inputs"}]
ns={"Path":Path,"hashlib":hashlib,"json":json,"HERE":HERE,"ROOT":Path("/Volumes/T7/Projects/WarlineCapture-mac-native-s003")}
exec(compile(ast.Module(body=functions,type_ignores=[]),"actual_corrected_runtime_guard_AST","exec"),ns)
actual=ns["inspect_runtime_inputs"](json.loads((HERE/"plan.disabled.json").read_text()));assert not actual["errors"] and len(actual["inputs"])==18
cases=[{"case":"actual_18_input_manifest","passed":True,"rejected":False}]
for case in ["fixture_unchanged","missing_file","same_size_hash_drift","size_drift","manifest_bytes_changed","unsafe_path","duplicate_path","symlink_file","membership_omission","foreign_source_epoch"]:
 with tempfile.TemporaryDirectory(prefix="warline-cc-runtime-",dir="/private/tmp") as td:
  base=Path(td);inputs=base/"own-project";inputs.mkdir();(inputs/"Design").mkdir();packet=base/"packet";packet.mkdir();rows=[]
  for i in range(18):
   p=inputs/"Design"/(str(i)+".dat");p.write_bytes(b"good");rows.append({"path":str(p.relative_to(inputs)),"sha256":hashlib.sha256(b"good").hexdigest(),"bytes":4})
  m={"schema_version":1,"scope":"RequestedCCAndSharedControls_ExplicitRuntimeDiskInputs","source_commit":"own-commit","source_candidate_sha256":"own-candidate","selector":"CC","expected_input_count":18,"inputs":rows}
  p=packet/"runtime-inputs.json";p.write_text(json.dumps(m));plan={"source_commit":"own-commit","candidate_sha256":"own-candidate","runtime_inputs_sha256":hashlib.sha256(p.read_bytes()).hexdigest()}
  if case=="missing_file":(inputs/"Design/0.dat").unlink()
  if case=="same_size_hash_drift":(inputs/"Design/0.dat").write_bytes(b"evil")
  if case=="size_drift":(inputs/"Design/0.dat").write_bytes(b"longer")
  if case=="manifest_bytes_changed":p.write_bytes(p.read_bytes()+b" ")
  if case in {"unsafe_path","duplicate_path","membership_omission","foreign_source_epoch"}:
   if case=="unsafe_path":m["inputs"][0]["path"]="../escape"
   if case=="duplicate_path":m["inputs"][1]["path"]=m["inputs"][0]["path"]
   if case=="membership_omission":m["inputs"].pop()
   if case=="foreign_source_epoch":m["source_candidate_sha256"]="foreign"
   p.write_text(json.dumps(m));plan["runtime_inputs_sha256"]=hashlib.sha256(p.read_bytes()).hexdigest()
  if case=="symlink_file":
   target=inputs/"Design/0.dat";target.unlink();target.symlink_to(inputs/"Design/1.dat")
  ns["HERE"]=packet;ns["ROOT"]=inputs;error=None;state=None
  try:state=ns["inspect_runtime_inputs"](plan)
  except RuntimeError as e:error=str(e)
  rejected=error is not None or bool(state["errors"]);assert rejected==(case!="fixture_unchanged"),(case,error,state)
  cases.append({"case":case,"passed":True,"rejected":rejected,"reason":error,"mismatch_paths":state["errors"] if state else None})
execute=next(n for n in tree.body if isinstance(n,ast.FunctionDef) and n.name=="execute_reviewed")
withnode=next(n for n in ast.walk(execute) if isinstance(n,ast.With) and any(isinstance(x,ast.Call) and isinstance(x.func,ast.Attribute) and x.func.attr=="Popen" for x in ast.walk(n)))
i=next(i for i,x in enumerate(withnode.body) if isinstance(x,ast.Assign) and isinstance(x.value,ast.Call) and isinstance(x.value.func,ast.Attribute) and x.value.func.attr=="Popen")
assert isinstance(withnode.body[i-1],ast.Assign) and withnode.body[i-1].value.func.id=="verify_immediate_launch_pins"
assert any(isinstance(x,ast.Call) and isinstance(x.func,ast.Name) and x.func.id=="inspect_runtime_inputs" for n in withnode.body[:i-1] for x in ast.walk(n))
assert all(x in source for x in ['save("runtime-inputs-before.json",runtime_before)','save("runtime-inputs-at-launch.json",runtime_launch)','save("runtime-inputs-after.json",runtime_after)','result.get("runtime_inputs_unchanged")','g.get("runtime_inputs_sha256")==plan["runtime_inputs_sha256"]','"--untracked-files=all"'])
result={"scope":"ActualRuntimeInputGuardMetadataCases_AndSourceWiring_NoNativeRun","cases_passed":len(cases),"source_wiring_groups_passed":3,"failures":0,"actual_runtime_inputs_admitted":18,"native_queries":0,"Unity_launches":0,"controller_execute_called":False,"cases":cases}
with (HERE/"runtime-input-controls.log").open("x") as f:json.dump(result,f,indent=2);f.write("\n")
print(json.dumps(result,indent=2))
