import ast,hashlib,json,tempfile,datetime,types,sys
from pathlib import Path
sys.path.insert(0,"/private/tmp/warline-mac-probe35-lifecycle-review/Tools/CI")
import mac_tanker_probe35_controller as actual_life
HERE=Path(__file__).parent
source=(HERE/"requested-CC-controller.py.proposal").read_text();tree=ast.parse(source)
functions=[n for n in tree.body if isinstance(n,ast.FunctionDef) and n.name in {"need","grant_time","verify_immediate_launch_pins"}]
assert len(functions)==3
rows=[]
for case in ["unchanged","grant_changed_after_read","controller_changed_after_read","plan_changed_after_read","restoration_changed_after_read","lifecycle_helper_changed","expired_grant"]:
 with tempfile.TemporaryDirectory(prefix="warline-cc-r2-metadata-",dir="/private/tmp") as td:
  base=Path(td);controller=base/"controller.py";planpath=base/"plan.disabled.json";helper=base/"layout-restoration.py.proposal";grantpath=base/"grant.json"
  controller.write_bytes(b"controller-fixture");helper.write_bytes(b"restoration-fixture");plan={"restoration_contract":{"helper_sha256":actual_life.digest(helper)}};planpath.write_text(json.dumps(plan))
  now=datetime.datetime.now(datetime.timezone.utc);g={"controller_sha256":actual_life.digest(controller),"plan_sha256":actual_life.digest(planpath),"issued_utc":(now-datetime.timedelta(seconds=30)).isoformat(),"expires_utc":(now+datetime.timedelta(minutes=5)).isoformat()}
  if case=="expired_grant":g["expires_utc"]=(now-datetime.timedelta(seconds=1)).isoformat()
  grantpath.write_text(json.dumps(g));captured=actual_life.digest(grantpath)
  ns={"Path":Path,"datetime":datetime,"HERE":base,"__file__":str(controller),"life":actual_life}
  exec(compile(ast.Module(body=functions,type_ignores=[]),"actual_r2_guard_AST","exec"),ns)
  if case=="grant_changed_after_read":grantpath.write_bytes(grantpath.read_bytes()+b" ")
  if case=="controller_changed_after_read":controller.write_bytes(b"changed-controller")
  if case=="plan_changed_after_read":planpath.write_bytes(planpath.read_bytes()+b" ")
  if case=="restoration_changed_after_read":helper.write_bytes(b"changed-helper")
  if case=="lifecycle_helper_changed":
   bad=base/"lifecycle.py";bad.write_bytes(b"changed-lifecycle");ns["life"]=types.SimpleNamespace(__file__=str(bad),digest=actual_life.digest)
  error=None
  try:ns["verify_immediate_launch_pins"](g,grantpath,captured,g["plan_sha256"],g["controller_sha256"],plan)
  except RuntimeError as e:error=str(e)
  assert (error is None)==(case=="unchanged"),(case,error)
  rows.append({"case":case,"passed":True,"rejected":error is not None,"reason":error})
# Actual source wiring: gate is the statement immediately preceding Popen;
# post-job pins compare full actual_tool_pins arrays, not auth response blobs.
execute=next(n for n in tree.body if isinstance(n,ast.FunctionDef) and n.name=="execute_reviewed")
withnode=next(n for n in ast.walk(execute) if isinstance(n,ast.With) and any(isinstance(x,ast.Call) and isinstance(x.func,ast.Attribute) and x.func.attr=="Popen" for x in ast.walk(n)))
assert isinstance(withnode.body[0],ast.Assign) and isinstance(withnode.body[0].value,ast.Call) and withnode.body[0].value.func.id=="verify_immediate_launch_pins"
assert isinstance(withnode.body[1],ast.Assign) and withnode.body[1].value.func.attr=="Popen"
assert 'tools_before["actual_tool_pins"]==tools_after["actual_tool_pins"]' in source
assert 'save("tools-after.json",tools_after)' in source
assert 'result.get("actual_tool_pins_unchanged")' in source
assert 'result["auth_observations"]={"before_logged_in":tools_before["auth_logged_in"],"after_logged_in":tools_after["auth_logged_in"]}' in source
result={"scope":"ActualR2GuardFunctions_MetadataMutationControls_AndSourceWiringOnly","metadata_cases_passed":len(rows),"source_wiring_checks_passed":5,"failures":0,"native_queries":0,"Unity_launches":0,"controller_execute_called":False,"cases":rows}
print(json.dumps(result,indent=2))
