"""Small semantic/refusal controls. Artifact hashing/native provenance is NOT stubbed into proof."""
import copy,json,tempfile,unittest
from pathlib import Path
from unittest.mock import patch
import native_compile_closure as native
import composite_compile_evidence as composite
import skirmish_operational_compile_connector as connector
import skirmish_native_compile_admission as producer

class NativeControls(unittest.TestCase):
 def sample(self):
  row={"rsp":"rsp","rspSha256":"a"*64,"inputs":{"source":"b"*64},"sources":["source"],"outputs":{"out":"c"*64}}
  value={"schema_version":1,"scope":"ObservedNativeDeclaredCompilerInputs-v1","inputs":{"rsp":"a"*64,"source":"b"*64},"owners":{"Game.Editor":row},"outputs":{"out":"c"*64}}
  return value,"[1/1 0] Csc Library/Bee/artifacts/x/Game.Editor.dll"
 def test_stable_window_and_required_Csc(self):
  value,text=self.sample();r=native.validate_values(value,copy.deepcopy(value),text,["Game.Editor"],["Game.Editor"]);self.assertEqual(r["freshOwners"],["Game.Editor"])
 def test_changed_reference_stays_pending_even_allowed_output(self):
  value,text=self.sample();after=copy.deepcopy(value);after["inputs"]["source"]="e"*64
  with self.assertRaisesRegex(ValueError,"Declared input drift"):native.validate_values(value,after,text,["Game.Editor"],["Game.Editor"])
 def test_missing_required_Csc_refuses(self):
  value,_=self.sample()
  with self.assertRaises(ValueError):native.validate_values(value,value,"[BUSY] Csc Library/Bee/artifacts/x/Game.Editor.dll",["Game.Editor"],["Game.Editor"])
 def test_omitted_owner_input_refuses(self):
  value,text=self.sample();value["owners"]["Game.Editor"]["sources"]=["missing"]
  with self.assertRaises(ValueError):native.validate_values(value,value,text,["Game.Editor"],["Game.Editor"])
 def test_actual_temporary_path_boundaries(self):
  with tempfile.TemporaryDirectory()as a,tempfile.TemporaryDirectory()as b:
   root=Path(a);owned=root/"owned.txt";owned.write_bytes(b"actual");foreign=Path(b)/"foreign.txt";foreign.write_bytes(b"actual")
   self.assertEqual(native.ordinary(root,owned),owned)
   for path in [foreign,root/".."/"foreign.txt"]:
    with self.assertRaises(ValueError):native.ordinary(root,path)
 def test_rsp_actual_parser_retains_declared_classes(self):
  text='"Assets/A.cs"\n-reference:"Library/R.dll"\n-analyzer:"Library/A.dll"\n-additionalfile:"Assets/data.txt"\n-out:"Library/Bee/artifacts/x/Game.Editor.dll"'
  inputs,outputs,sources=native.declared(Path.cwd(),text);self.assertEqual(len(inputs),4);self.assertEqual(len(outputs),1);self.assertEqual(sources,["Assets/A.cs"])
  with self.assertRaises(ValueError):native.declared(Path.cwd(),text+"\n-unknown:path")
 def test_coherent_wrong_source_hash_refuses_snapshot_join(self):
  value,text=self.sample();root=Path.cwd();path=native.key(root,"source");value["inputs"][path]=value["inputs"].pop("source");value["owners"]["Game.Editor"]["inputs"]={path:"b"*64};value["owners"]["Game.Editor"]["sources"]=[path];snapshot={"source_manifest":[dict(path="source",sha256="b"*64)]}
  native.validate_source_hashes(root,value,value,snapshot,snapshot)
  wrong=copy.deepcopy(value);wrong["inputs"][path]=wrong["owners"]["Game.Editor"]["inputs"][path]="e"*64
  native.validate_values(wrong,wrong,text,["Game.Editor"],["Game.Editor"])
  with self.assertRaisesRegex(ValueError,"actual source snapshot"):native.validate_source_hashes(root,wrong,wrong,snapshot,snapshot)
 def test_unrelated_same_hash_cannot_replace_declared_native_output(self):
  root=Path.cwd();output=native.key(root,"Library/Bee/artifacts/x/Game.Editor.dll");values={output:"a"*64}
  native.validate_output_artifact_refs(root,values,{output:dict(path=output,sha256="a"*64)})
  with self.assertRaisesRegex(ValueError,"exact canonical declared native output"):native.validate_output_artifact_refs(root,values,{output:dict(path=str(root/"Logs/unrelated.bin"),sha256="a"*64)})
 def test_later_unsealed_closure_cannot_impersonate_native_capture(self):
  ref=dict(path="native-compile-closure.json",sha256="a"*64)
  native.validate_capture_origin(dict(nativeCompileInputClosure=ref),ref)
  for result in [{},dict(nativeCompileInputClosure=dict(path=ref["path"],sha256="b"*64))]:
   with self.assertRaisesRegex(ValueError,"native result must seal"):native.validate_capture_origin(result,ref)
 def test_empty_global_output_union_refuses(self):
  value,text=self.sample();value["outputs"]={}
  with self.assertRaisesRegex(ValueError,"global owning output union"):native.validate_values(value,value,text,["Game.Editor"],["Game.Editor"])
 def test_duplicate_owning_output_refuses(self):
  value,text=self.sample();value["owners"]["Other"]=copy.deepcopy(value["owners"]["Game.Editor"])
  with self.assertRaisesRegex(ValueError,"unique owning output"):native.validate_values(value,value,text,["Game.Editor"],["Game.Editor"])
 def test_current_missing_native_closure_cannot_emit_admission(self):
  proof={"schema_version":3,**{x:None for x in producer.REFS}}
  result=producer.admit(Path.cwd(),{},proof);self.assertEqual(result["result"],"MissingOrFailedAdmission");self.assertFalse(result["qualified"])

class CompositeControls(unittest.TestCase):
 def fixture(self):
  root=Path.cwd();names=["Owner"+str(i)for i in range(24)];latest=root/"Logs/fixture/latest/result.json";origin=root/"Logs/fixture/origin/result.json";sdk="D:/Program Files/Unity/6000.5.8f1/Editor/Data/DotNetSdk/"
  oldpins={};now={};texts={};objects={};sourceOld=[];sourceNow=[];oldoutputs={};outputs={};oldowners=[];owners=[];responses={}
  for i,n in enumerate(names):
   oldsrc=str(origin.parent/(n+".cs"));newsrc=str(latest.parent/(n+".cs"));h=("a"if i<10 else "b")*64
   oldhash="b"*64;sourceOld.append(dict(path=n+".cs",owner=n,compiledSource=oldsrc,sha256=oldhash));sourceNow.append(dict(path=n+".cs",owner=n,compiledSource=newsrc,sha256=h))
   oldpins[oldsrc]=oldhash;now[oldsrc]=oldhash;now[newsrc]=h
   oldowners.append(dict(owner=n,exit=0,stopReason=None));owners.append(dict(owner=n,exit=0,stopReason=None,reused=i>=10,**({"priorResult":str(origin)}if i>=10 else {})))
   for suffix in [".dll",".ref.dll",".pdb"]:
    oldoutputs[str(origin.parent/(n+suffix))]="d"*64;outputs[str((latest if i<10 else origin).parent/(n+suffix))]="d"*64
   run=latest if i<10 else origin;rsp=str(run.parent/(n+".rsp"));src=newsrc if i<10 else oldsrc
   text='"'+src+'"\n-reference:"'+str(root/"Library/framework.dll")+'"\n-out:"'+str(run.parent/(n+".dll"))+'"\n-refout:"'+str(run.parent/(n+".ref.dll"))+'"'
   texts[rsp]=text;oldpins[rsp]="c"*64;now[rsp]="c"*64;responses[n]=dict(path=rsp,sha256="c"*64)
  reference=str(root/"Library/framework.dll");oldpins[reference]=now[reference]="e"*64
  for suffix in ["dotnet.exe","sdk/8.0.318/Roslyn/bincore/csc.dll"]:oldpins[sdk+suffix]=now[sdk+suffix]="f"*64
  refs={k:dict(path=str(origin.parent/n),sha256=(str(i+1)*64)[:64])for i,(k,n)in enumerate([("result","result.json"),("pinsBefore","pins-before.json"),("sources","source-manifest.json")])}
  for ref in refs.values():now[ref["path"]]=ref["sha256"]
  old=dict(result="Passed",mismatches=[],owners=oldowners,outputs=oldoutputs,ownersFresh=24,ownersReused=0,stopBeforeLaunchReason=None)
  objects[refs["result"]["path"]]=old;objects[refs["pinsBefore"]["path"]]=oldpins;texts[refs["sources"]["path"]]=json.dumps(sourceOld)
  current=dict(result="Passed",mismatches=[],owners=owners,outputs=outputs,ownersFresh=10,ownersReused=14,priorResult=str(origin))
  class FakeReader:
   def __init__(self):self.root=root
   def path(self,p):p=Path(p);return p if p.is_absolute()else root/p
   def obj(self,ref):return self.path(ref["path"]),objects[ref["path"]]
   def read(self,ref):return self.path(ref["path"]),texts[ref["path"]]
  data=dict(offline_result=current,offline_scope=dict(ownersFresh=names[:10],ownersReused=names[10:]),offline_pins_before=now,owner_graph=dict(owners=names,dependencies={n:[]for n in names}),compiler_lineage=dict(runs=[refs],responses=responses),offline_sources=sourceNow)
  return FakeReader(),dict(offline_result=dict(path=str(latest))),data,reference,sdk
 def test_retained_sources_remap_without_fabricating_compiled_path(self):
  read,proof,data,_,_=self.fixture();r=composite.validate(read,proof,data);self.assertEqual((r["fresh"],r["retained"]),(10,14));self.assertEqual(r["declaredRequiredInputs"],27)
 def test_required_reference_omission_refuses(self):
  read,proof,data,ref,_=self.fixture();del data["offline_pins_before"][ref]
  with self.assertRaisesRegex(ValueError,"reference/analyzer"):composite.validate(read,proof,data)
 def test_sdk_omission_refuses(self):
  read,proof,data,_,sdk=self.fixture();del data["offline_pins_before"][sdk+"dotnet.exe"]
  with self.assertRaisesRegex(ValueError,"SDK"):composite.validate(read,proof,data)
 def test_retained_source_change_refuses(self):
  read,proof,data,_,_=self.fixture();data["offline_sources"][-1]["sha256"]="9"*64
  with self.assertRaises(ValueError):composite.validate(read,proof,data)
 def test_alias_conflict_refuses(self):
  read,proof,data,ref,_=self.fixture();data["offline_pins_before"][ref.upper()]="9"*64
  with self.assertRaisesRegex(ValueError,"alias"):composite.validate(read,proof,data)

class ConnectorControls(unittest.TestCase):
 def run_connector(self,fail=False,stable=False,config=False):
  base=dict(result="NormalCompletedUnqualified",wins=1,proof_pins=[dict(path="operational",sha256="a"*64)],attempts=[dict(job_sha256="sha1",native_config_sha256="c"*64),dict(job_sha256="sha2",native_config_sha256="c"*64)],admission=dict(result="MissingAdmission",missing=["compile","live_api","source_fit","paid_resources"],profile_receipts=[{"real":"profile"}],observer_receipts=[{"real":"observer"}],profile_errors=[]))
  jobs=[dict(candidate_sha256="candidate",build_sha256="build",id="sha1"),dict(candidate_sha256="candidate",build_sha256="build",id="sha2")]
  class FakeReader:
   def __init__(self,root):self.pins={}
   def path(self,path):return Path(path)
   def obj(self,ref):return None,jobs[ref["index"]]
   def stable(self):
    if stable:raise ValueError("actual test mutation")
  def admit(root,job,proof):
   return dict(admission="compile",result="MeasuredNativeEditorCompileForExactCandidate",job_sha256="wrong"if fail and job["id"]=="sha2"else job["id"],candidate_sha256="candidate",build_sha256="build",qualified=False,queue_success_enabled=False,native_config_sha256=("d"if config else"c")*64,proof_pins=[dict(path="compile"+job["id"],sha256="a"*64)])
  spec=dict(schema_version=2,operational=dict(attempts=[dict(job=dict(index=0)),dict(job=dict(index=1))]),compile_proof={})
  with patch.object(connector,"inspect_operational",return_value=copy.deepcopy(base)),patch.object(connector,"Reader",FakeReader),patch.object(connector,"digest",return_value="a"*64),patch.object(connector,"fingerprint",side_effect=lambda j:j["id"]),patch.object(connector,"admit",side_effect=admit):
   return connector.inspect(Path.cwd(),spec)
 def test_actual_late_earlier_compile_proof_mutation_refuses(self):
  import hashlib
  with tempfile.TemporaryDirectory()as directory:
   root=Path(directory);op=root/"op.bin";prior=root/"prior.bin";op.write_bytes(bytes([255,0,1]));prior.write_bytes(b"earlier compile")
   ref=lambda p:dict(path=str(p),sha256=hashlib.sha256(p.read_bytes()).hexdigest())
   priorRef=ref(prior);jobs=[];items=[];attempts=[]
   for i in range(2):
    job=dict(candidate_sha256="candidate",build_sha256="build",id=i);path=root/(str(i)+".json");path.write_text(json.dumps(job));jobs.append(job);items.append(dict(job=ref(path)));attempts.append(dict(job_sha256=connector.fingerprint(job),native_config_sha256="c"*64))
   base=dict(result="NormalCompletedUnqualified",wins=0,attempts=attempts,proof_pins=[ref(op)],admission=dict(missing=["compile","live_api"],profile_receipts=["profile"],observer_receipts=["observer"]))
   def admit(project,job,proof):
    if job["id"]==1:prior.write_bytes(b"late mutation")
    return dict(admission="compile",result="MeasuredNativeEditorCompileForExactCandidate",job_sha256=connector.fingerprint(job),candidate_sha256="candidate",build_sha256="build",qualified=False,queue_success_enabled=False,native_config_sha256="c"*64,proof_pins=[priorRef]if job["id"]==0 else[ref(op)])
   spec=dict(schema_version=2,operational=dict(attempts=items),compile_proof={})
   with patch.object(connector,"inspect_operational",return_value=base),patch.object(connector,"admit",side_effect=admit):result=connector.inspect(root,spec)
   self.assertIn("compile",result["admission"]["missing"]);self.assertTrue(result["admission"]["compile_errors"]);self.assertEqual(result["admission"]["profile_receipts"],["profile"])
 def test_preserves_profile_observer_and_real_loss_win_count(self):
  r=self.run_connector();self.assertNotIn("compile",r["admission"]["missing"]);self.assertEqual(r["admission"]["profile_receipts"],[{"real":"profile"}]);self.assertEqual(r["admission"]["observer_receipts"],[{"real":"observer"}]);self.assertEqual(r["wins"],1);self.assertFalse(r["qualified"])
 def test_same_job_different_actual_config_keeps_compile_pending(self):
  r=self.run_connector(config=True);self.assertIn("compile",r["admission"]["missing"]);self.assertTrue(r["admission"]["compile_errors"]);self.assertEqual(r["admission"]["profile_receipts"],[{"real":"profile"}])
 def test_second_job_splice_keeps_compile_pending(self):
  r=self.run_connector(fail=True);self.assertIn("compile",r["admission"]["missing"]);self.assertEqual(r["wins"],1);self.assertIn("observer_receipts",r["admission"])
 def test_final_read_stability_failure_keeps_compile_pending(self):
  r=self.run_connector(stable=True);self.assertIn("compile",r["admission"]["missing"]);self.assertTrue(r["admission"]["compile_errors"])
if __name__=="__main__":unittest.main(verbosity=2)
