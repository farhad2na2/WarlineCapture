"""Pinned data-only invocation of the current native config compiler; never constructs a simulation world."""
from pathlib import Path
import json,math,os,subprocess,time
import skirmish_owned_source as owned
import skirmish_source_build as b
def read_author_json(path):
 p=Path(path).resolve(strict=True);raw=p.read_bytes();b.need(len(raw)<=16*1024*1024,'Bounded authoring JSON required');value=json.loads(raw,object_pairs_hook=owned.require('skirmish_source_syntax').no_duplicates,parse_constant=lambda x:(_ for _ in ()).throw(ValueError('Nonfinite authoring JSON')));return value,{'path':str(p),'sha256':b.sha(raw),'bytes':len(raw)}
def author(repo,package,manifest,output,mission,seed,authoring_input,build_receipt=None,data_repo=None,timeout=120):
 draft,draft_pin=read_author_json(authoring_input)
 if isinstance(draft,dict)and draft.get('kind')=='SkirmishDeclaredCompilerExportDraft':return author_declared(repo,package,manifest,output,mission,seed,draft,draft_pin,build_receipt,data_repo,timeout)
 repo=Path(repo).resolve(strict=True);data_repo=Path(data_repo).resolve(strict=True)if data_repo else repo;out=Path(output).absolute();b.need(mission=='S026'and seed==104755,'Current data authoring scope is S026/104755; no runtime admission');b.need(math.isfinite(timeout)and timeout>0,'Finite positive authoring timeout required');b.need(not out.exists(),'Fresh isolated authoring output required');out.mkdir(parents=True)
 if build_receipt is None:
  # Compilation profile selects the existing finite host closure ONLY; no S002 world is initialized.
  b.build(repo,package,manifest,out/'build','S002',104731);build_receipt=out/'build/build.json'
 receipt,build_pin=b.read_json_pinned(build_receipt);b.need(receipt.get('status')=='Passed'and receipt.get('mission')=='S002'and receipt.get('seed')==104731,'Qualified finite tool build profile required');b.need(receipt.get('repository_root')==str(repo),'Authoring source root differs from build');b.verify(receipt['source_and_reference_pins']);draft,draft_pin=read_author_json(authoring_input)
 required={'schema_version','kind','catalog_id','seed','size','difficulty','graph','profiles','campaign_source_path','layout','setup_matrix'};b.need(set(draft)in [required,required|{'binding'}],'Exact finite authoring input fields required');b.need(draft.get('schema_version')==1 and draft.get('kind')=='SkirmishMissionAuthoringDraft'and draft.get('catalog_id')==mission and draft.get('seed')==seed,'Actual new mission authoring identity required');b.need(draft.get('size')in {'Standard','War','LargeWar'}and draft.get('difficulty')in {'Recruit','Regular','Veteran','Commander'},'Native size/difficulty names required')
 pins=dict(receipt['source_and_reference_pins']);b.merge_pins(pins,owned.pins());b.merge_pins(pins,{build_pin['path']:build_pin['sha256'],draft_pin['path']:draft_pin['sha256']});request=dict(draft);request['kind']='SkirmishMissionAuthoringPreflight';request['source_repository_root']=str(repo);request['data_repository_root']=str(data_repo);request['output_directory']=str(out)
 for key in ['graph','layout','setup_matrix']+(['binding']if'binding'in draft else[]):
  row=draft[key];b.need(isinstance(row,dict)and set(row)=={'path','sha256'},'Exact descriptor '+key);value,pin=read_author_json(row['path'])if key!='setup_matrix'else(None,b.pin(row['path']));b.need(pin['sha256']==row['sha256'],'Authoring descriptor changed: '+key);b.merge_pins(pins,{pin['path']:pin['sha256']});request[key]={'path':pin['path'],'sha256':pin['sha256']}
  if key=='graph':
   for path,sha in value['source_pins'].items():
    target=Path(path).resolve(strict=True);root=repo if target.suffix=='.cs'else data_repo;b.need(target.is_relative_to(root),'Authoring graph pin outside explicit source/data root');b.need(b.pin(target)['sha256']==sha,'Actual profile/source asset changed: '+path);b.merge_pins(pins,{str(target):sha})
 tools={k:Path(v).resolve(strict=True)for k,v in receipt['runtime_selection'].items()};b.need(set(tools)=={'contents','script','engine','mono','newtonsoft'},'Recorded runtime selection required');b.need(str(tools['mono'])in pins and str(tools['newtonsoft'])in pins,'Unpinned runtime selection');config,extra=b.prepare_mono_config(tools,out);b.merge_pins(pins,extra);runtime_pin=b.pin(config);request['runtime_config']={'path':runtime_pin['path'],'sha256':runtime_pin['sha256']};b.merge_pins(pins,{str(config):runtime_pin['sha256']});request_path=out/'authoring-request.json';b.write(request_path,request);request_pin=b.pin(request_path);b.merge_pins(pins,{str(request_path):request_pin['sha256']});exe=receipt['executable'];b.merge_pins(pins,{exe['path']:exe['sha256']});b.verify(pins)
 env=os.environ.copy();env['MONO_PATH']=os.pathsep.join([str(repo/'Library/ScriptAssemblies'),str(tools['engine']),str(tools['newtonsoft'].parent),str(Path(receipt['placement_build']).parent)]);start=time.monotonic();timed_out=False
 with(out/'process.log').open('w')as log:
  process=subprocess.Popen([str(tools['mono']),'--config',str(config),'--optimize=-float32',exe['path'],'--authoring-preflight',str(request_path),request_pin['sha256']],env=env,stdout=log,stderr=subprocess.STDOUT)
  try:process.wait(timeout=timeout)
  except subprocess.TimeoutExpired:timed_out=True
  finally:
   if process.poll()is None:
    process.terminate()
    try:process.wait(timeout=5)
    except subprocess.TimeoutExpired:process.kill();process.wait(timeout=5)
 b.verify(pins);result_path=out/'authoring-result.json';raw,result_pin=b.read_json_pinned(result_path)if result_path.exists()else(None,None);valid=not timed_out and process.returncode==0 and raw is not None and raw.get('catalog_id')==mission and raw.get('seed')==seed and raw.get('world_allocated')is False and raw.get('source_fit_verified')is False and raw.get('validation_passed')is False and raw.get('mission_ready')is False and raw.get('native_acceptance')is False and ((raw.get('status')=='CompilerRejectedDraft'and raw.get('compiler_accepted')is False)or(raw.get('status')=='ConfigurationCompiled'and raw.get('compiler_accepted')is True and raw.get('native_authoring_inputs_returned')is True))
 if valid:
  for name in ['runtime-before-authoring.json','runtime-after-authoring.json']:b.need((out/name).is_file(),'Actual loaded-runtime authoring manifest missing')
 report={'schema_version':1,'kind':'SkirmishMissionAuthoringInvocation','status':('ConfigurationCompiled'if raw['status']=='ConfigurationCompiled'else'DraftInspected')if valid else'Failed','authoring_mission':mission,'authoring_seed':seed,'compilation_profile':{'mission':'S002','seed':104731,'scope':'Finite source-tool compilation only; no S002 world initialized'},'process_exit':process.returncode,'timed_out':timed_out,'wall_seconds':time.monotonic()-start,'source_repository_root':str(repo),'data_repository_root':str(data_repo),'source_result':raw,'result_pin':result_pin,'input_and_runtime_pins':pins,'world_allocated':False,'validation_passed':False,'source_fit_verified':False,'mission_ready':False,'native_acceptance':False};b.write(out/'authoring-receipt.json',report);return report

def declared_origin_roots(descriptor):
 b.need(isinstance(descriptor,dict)and set(descriptor)=={'path','sha256'},'Exact declared native origin descriptor required');origin,pin=read_author_json(descriptor['path']);b.need(pin['sha256']==descriptor['sha256'],'Declared native origin descriptor changed')
 fields={'schema_version','kind','native_metadata','source_root','data_root','input_pins'};b.need(isinstance(origin,dict)and set(origin)==fields and type(origin['schema_version'])is int and origin['schema_version']==1 and origin['kind']=='QualifiedNativeMetadataOrigin','Exact qualified native origin shape required')
 roots=[]
 for key in ['source_root','data_root']:
  b.need(isinstance(origin[key],str)and bool(origin[key]),'Explicit native origin '+key+' required');root=Path(origin[key]).resolve(strict=True);b.need(root.is_dir(),'Native origin root must be a directory');roots.append(root)
 return tuple(roots),pin
def declared_graph_pin(path,digest,repo,data_repo,origin_roots):
 target=Path(path).resolve(strict=True);current_root=repo if target.suffix=='.cs'else data_repo;b.need(any(target.is_relative_to(root)for root in(current_root,)+origin_roots),'Declared graph pin outside explicit current/native-origin source/data roots');pin=b.pin(target);b.need(pin['sha256']==digest,'Actual source/profile asset changed: '+path);return pin

def author_declared(repo,package,manifest,output,mission,seed,draft,draft_pin,build_receipt=None,data_repo=None,timeout=120):
 """Existing DATA-only author boundary; target identity remains the original authored catalog."""
 repo=Path(repo).resolve(strict=True);data_repo=Path(data_repo).resolve(strict=True)if data_repo else repo;out=Path(output).absolute();b.need(math.isfinite(timeout)and timeout>0,'Finite positive export timeout required');b.need(not out.exists(),'Fresh isolated authoring output required')
 required={'schema_version','kind','catalog_id','seed','size','difficulty','graph','profiles','campaign_source_path','layout','binding_fields','setup_matrix','metadata_source_origin'};b.need(required<=set(draft)<=required|{'factory_projection_version'} and draft.get('schema_version')==1 and draft.get('catalog_id')==mission and type(draft.get('seed'))is int and draft['seed']==seed,'Exact declared source compiler input required');projection_version=draft.get('factory_projection_version',1);b.need(type(projection_version)is int and projection_version in{1,2},'Explicit supported factory projection version required');b.need(draft['size']in{'Standard','War','LargeWar'}and draft['difficulty']in{'Recruit','Regular','Veteran','Commander'},'Original size/difficulty names required');out.mkdir(parents=True)
 if build_receipt is None:
  # Only the existing typed host closure is selected here; no profile World/target alias.
  b.build(repo,package,manifest,out/'build','S026',104755);build_receipt=out/'build/build.json'
 receipt,build_pin=b.read_json_pinned(build_receipt);b.need(receipt.get('status')=='Passed'and receipt.get('runtime_backend')=='TypedGroundS026V1'and receipt.get('repository_root')==str(repo),'Current typed DATA-tool build/root required');b.verify(receipt['source_and_reference_pins']);pins=dict(receipt['source_and_reference_pins']);b.merge_pins(pins,owned.pins());b.merge_pins(pins,{build_pin['path']:build_pin['sha256'],draft_pin['path']:draft_pin['sha256']});request=dict(draft);request.update(kind='SkirmishDeclaredCompilerExportPreflight',source_repository_root=str(repo),data_repository_root=str(data_repo),output_directory=str(out))
 origin_roots,origin_pin=declared_origin_roots(draft['metadata_source_origin']);b.merge_pins(pins,{origin_pin['path']:origin_pin['sha256']})
 for key in ['graph','layout','binding_fields','setup_matrix','metadata_source_origin']:
  row=draft[key];b.need(isinstance(row,dict)and set(row)=={'path','sha256'},'Exact declared descriptor '+key);value,pin=read_author_json(row['path'])if key!='setup_matrix'else(None,b.pin(row['path']));b.need(pin['sha256']==row['sha256'],'Declared source descriptor changed: '+key);b.merge_pins(pins,{pin['path']:pin['sha256']});request[key]={'path':pin['path'],'sha256':pin['sha256']}
  if key=='graph':
   graph_pins={}
   for path,digest in value['source_pins'].items():
    graph_pin=declared_graph_pin(path,digest,repo,data_repo,origin_roots);b.need(graph_pin['path'] not in graph_pins or graph_pins[graph_pin['path']]==graph_pin['sha256'],'Conflicting declared graph pin');graph_pins[graph_pin['path']]=graph_pin['sha256']
   b.merge_pins(pins,graph_pins)
 tools={k:Path(v).resolve(strict=True)for k,v in receipt['runtime_selection'].items()};b.need(set(tools)=={'contents','script','engine','mono','newtonsoft'},'Recorded runtime selection required');b.need(str(tools['mono'])in pins and str(tools['newtonsoft'])in pins,'Unpinned original runtime');config,extra=b.prepare_mono_config(tools,out);b.merge_pins(pins,extra);runtime=b.pin(config);request['runtime_config']={'path':runtime['path'],'sha256':runtime['sha256']};request_path=out/'authoring-request.json';b.write(request_path,request);rp=b.pin(request_path);b.merge_pins(pins,{rp['path']:rp['sha256']});exe=receipt['executable'];b.merge_pins(pins,{exe['path']:exe['sha256']});b.verify(pins);env=os.environ.copy();env['MONO_PATH']=os.pathsep.join([str(repo/'Library/ScriptAssemblies'),str(tools['engine']),str(tools['newtonsoft'].parent),str(Path(receipt['placement_build']).parent)]);start=time.monotonic();timed=False
 try:
  with(out/'process.log').open('w')as log:
   process=subprocess.Popen([str(tools['mono']),'--config',str(config),'--optimize=-float32',exe['path'],'--authoring-preflight',str(request_path),rp['sha256']],env=env,stdout=log,stderr=subprocess.STDOUT)
   try:process.wait(timeout=timeout)
   except subprocess.TimeoutExpired:timed=True
   finally:
    if process.poll()is None:
     process.terminate()
     try:process.wait(timeout=5)
     except subprocess.TimeoutExpired:process.kill();process.wait(timeout=5)
  elapsed=time.monotonic()-start;result_path=out/'authoring-result.json';raw,result_pin=b.read_json_pinned(result_path)if result_path.exists()else(None,None);valid=not timed and process.returncode==0 and raw is not None and raw.get('catalog_id')==mission and raw.get('seed')==seed and raw.get('world_allocated')is False and raw.get('mission_ready')is False and raw.get('validation_passed')is False;compiled=valid and raw['actual_original_compiler']['original_complete_compiler_passed']and raw['actual_original_compiler']['actual_derived_factory_context']is not None
  if valid:
   for name in ['runtime-before-authoring.json','runtime-after-authoring.json']:b.need((out/name).is_file(),'Actual loaded-runtime DATA manifest missing')
  report={'schema_version':1,'kind':'SkirmishDeclaredCompilerExportInvocation','status':'CompilerContextExported'if compiled else'CompilerRejected'if valid else'Failed','authoring_mission':mission,'authoring_seed':seed,'process_exit':process.returncode,'timed_out':timed,'wall_seconds':elapsed,'source_repository_root':str(repo),'data_repository_root':str(data_repo),'source_result':raw,'result_pin':result_pin,'input_and_runtime_pins':pins,'world_allocated':False,'validation_passed':False,'source_fit_verified':False,'mission_ready':False,'native_acceptance':False};b.write(out/'authoring-receipt.json',report);return report
 finally:b.verify(pins)
