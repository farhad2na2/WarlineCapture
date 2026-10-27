"""Run the existing public DATA author and compare explicit pinned original results; never a World."""
from pathlib import Path
import argparse,hashlib,json,os,re,subprocess,sys,time,traceback
sys.dont_write_bytecode=True
p=argparse.ArgumentParser(description=__doc__);p.add_argument('--source-repo',type=Path,required=True);p.add_argument('--package-root',type=Path,required=True);p.add_argument('--data-repo',type=Path,required=True);p.add_argument('--build-receipt',type=Path,required=True);p.add_argument('--cases',type=Path,required=True);p.add_argument('--output',type=Path,required=True);p.add_argument('--timeout',type=int,default=120);args=p.parse_args()
repo=args.source_repo.resolve(strict=True);package=args.package_root.resolve(strict=True);data=args.data_repo.resolve(strict=True);out=args.output.absolute();assert args.timeout>0 and not out.exists();out.mkdir(parents=True)
sha=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest();pins={};rows=[]
def load(path):
 path=Path(path).resolve(strict=True);pins[str(path)]=sha(path);return json.loads(path.read_text())
def descriptor(value):
 assert isinstance(value,dict) and set(value)=={'path','sha256'};path=Path(value['path']).resolve(strict=True);assert sha(path)==value['sha256'];return path

def write(name,value):
 with (out/name).open('x') as f:f.write(json.dumps(value,indent=2)+'\n')
def verify():
 for path,digest in pins.items():assert sha(path)==digest,path
try:
 cases=load(args.cases);assert set(cases)=={'schema_version','cases'} and cases['schema_version']==1 and isinstance(cases['cases'],list) and cases['cases'];build=load(args.build_receipt);assert build['status']=='Passed' and build['runtime_backend']=='TypedGroundS026V1' and build['repository_root']==str(repo);pins.update(build['source_and_reference_pins']);pins[str(Path(__file__).resolve())]=sha(__file__);cli=package/'Tools/CI/run_skirmish_source_simulation.py';pins[str(cli)]=sha(cli);seen=set();verify()
 for case in cases['cases']:
  assert set(case)=={'catalog_id','seed','request','expected_actual_setup','original_qualified_comparison_result'};name=case['catalog_id'];assert re.fullmatch(r'S[0-9]{3}',name) and name not in seen and type(case['seed'])is int;seen.add(name);request=descriptor(case['request']);reference=descriptor(case['original_qualified_comparison_result']);draft=load(request);expected=load(reference)['actual_original_compiler'];assert draft['catalog_id']==name and draft['seed']==case['seed'];folder=out/'exports'/name;folder.parent.mkdir(exist_ok=True);cmd=[sys.executable,'-B',str(cli),'author','--repo',str(repo),'--package-root',str(package),'--mission',name,'--seed',str(case['seed']),'--authoring-input',str(request),'--build-receipt',str(args.build_receipt.resolve()),'--data-repo',str(data),'--timeout',str(args.timeout),'--output',str(folder)];verify()
  log=out/(name+'-public-author.log');start=time.monotonic()
  with log.open('x') as f:
   child=subprocess.Popen(cmd,stdout=f,stderr=subprocess.STDOUT);pid=child.pid
   try:code=child.wait(timeout=args.timeout+20)
   finally:
    if child.poll()is None:
     child.terminate()
     try:child.wait(timeout=5)
     except subprocess.TimeoutExpired:child.kill();child.wait(timeout=5)
  elapsed=time.monotonic()-start;row={'catalog_id':name,'pid':pid,'exit_code':code,'child_elapsed_seconds':elapsed,'argv':cmd,'log':str(log)};rows.append(row);write(name+'-public-author-child.json',row);assert code==0,'First actual public author failure'
  receipt=load(folder/'authoring-receipt.json');actual=load(folder/'authoring-result.json');a=actual['actual_original_compiler'];c=a['actual_derived_factory_context'];e=expected['actual_derived_factory_context'];assert receipt['status']=='CompilerContextExported' and receipt['process_exit']==0 and not receipt['timed_out'];assert actual['world_allocated']is False and actual['mission_ready']is False and actual['native_acceptance']is False;assert a['original_complete_compiler_passed'] and a['actual_binding_roundtrip_same_setup_and20anchors'];assert a['setup_hash']==case['expected_actual_setup'];assert a['compiled_structure_count']==len(expected['compiled_structures']);assert a['compiled_structures']==[{k:x[k] for k in ('StructureId','FactionId','SpawnAnchorId','SpawnWorldX','SpawnWorldZ')} for x in expected['compiled_structures']];assert a['actual_portable_binding_fields']==expected['actual_portable_binding_fields'];assert c['target_context']==e['target_context'] and c['derived_metadata']==e['derived_metadata'] and c['derived_metadata_key_sha256']==e['derived_metadata_key_sha256'];assert c['unchanged_native_sections_sha256']==e['unchanged_native_sections_sha256'] and len(c['unchanged_native_sections_sha256'])==6;assert c['native_factory_reference']==e['native_factory_reference'] and c['native_origin']==e['native_origin'];assert c['current_factory_source_root']==str(repo) and c['world_allocated']is False and c['identities_allocated']==0 and c['runtime_loader_admitted']is False
  before=load(folder/'runtime-before-authoring.json');after=load(folder/'runtime-after-authoring.json');assert before['managed_entry']==after['managed_entry']==build['executable'];loaded={x['identity']:x for x in after['actually_loaded_managed_assemblies']};assert all(loaded.get(x['identity'])==x for x in before['actually_loaded_managed_assemblies']);pins.update(receipt['input_and_runtime_pins']);verify();row.update(actual_setup_hash=a['setup_hash'],compiled_structure_count=a['compiled_structure_count'],portable_fields_exact=True,original_target_context_exact=True,original_six_native_sections_exact=True,genuine_current_root=str(repo),authoring_result_sha256=sha(folder/'authoring-result.json'),actual_consumed_pins_verified=len(receipt['input_and_runtime_pins']))
 write('public-comparison-result.json',{'result':'Passed','sources':rows,'world':False,'runtime_admission':False});print('[PublicDeclaredCompilerComparison] result=Passed sources='+str(len(rows))+' originalSetupPortableAndNativeFactsExact=True World=False')
except BaseException as e:
 write('public-comparison-failure.json',{'error':str(e),'traceback':traceback.format_exc(),'sources':rows});raise
finally:
 verify();write('comparison-postpins.json',{'pins_verified_unchanged':len(pins)})
