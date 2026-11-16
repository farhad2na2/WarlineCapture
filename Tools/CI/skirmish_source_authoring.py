"""Pinned data-only invocation of the current native config compiler; never constructs a simulation world."""
from pathlib import Path
import json,math,os,subprocess,time
import skirmish_owned_source as owned
import skirmish_source_build as b

AUTHOR_BASE_MANIFEST_SHA256='6157fbec94e3ac2db05f9a277935d699e4690a400e19816981c8a526a41ed9e2'
AUTHOR_CONFIGURATION_QUALIFICATION={
 'id':'SourceConfigurationQualification.cs',
 'kind':'finite_adapter',
 'path':'Tools/CI/SkirmishSourceSimulation/Adapters/SourceConfigurationQualification.cs',
 'role':'data_decoder',
 'contains_original_gameplay':False,
 'scope':'Explicit runtime experimental configuration/factory qualification; no mission-ID branches or world orchestration.'
}

def prepare_authoring_manifest(manifest,package,output):
 base=Path(manifest).resolve(strict=True)
 expected=(Path(package).resolve(strict=True)/'Tools/CI/SkirmishSourceSimulation/source_manifest.plan.json').resolve(strict=True)
 raw=base.read_bytes();base_sha=b.sha(raw)
 if base==expected:b.need(base_sha==AUTHOR_BASE_MANIFEST_SHA256,'Pinned shared S002 source manifest changed')
 data=json.loads(raw,object_pairs_hook=owned.require('skirmish_source_syntax').no_duplicates,
                 parse_constant=lambda value:(_ for _ in ()).throw(ValueError('Nonfinite source manifest')))
 b.need(data.get('schema_version')==1 and data.get('profile')=={
  'mission':'S002','seed':104731,'numeric_runtime':'InstalledMono'
 } and 'qualification_lineage' not in data.get('profile',{}),
  'Shared authoring manifest must retain the exact S002/104731 compilation profile')
 entries=data.get('sources')
 b.need(isinstance(entries,list) and 0<len(entries)<=2048,'Finite shared source manifest required')
 matches=[entry for entry in entries if entry.get('id')==AUTHOR_CONFIGURATION_QUALIFICATION['id'] or
          entry.get('path')==AUTHOR_CONFIGURATION_QUALIFICATION['path']]
 if base!=expected:
  b.need(len(matches)==1 and matches[0]==AUTHOR_CONFIGURATION_QUALIFICATION,
         'Custom author manifest must contain one exact shared qualification adapter')
  b.need(base.read_bytes()==raw,'Custom author manifest changed during inspection')
  return base,None,None
 b.need(len(matches)==1 and matches[0]==AUTHOR_CONFIGURATION_QUALIFICATION,
        'Pinned base manifest must contain one exact shared qualification adapter')
 derived=Path(output)/'authoring-source-manifest.json'
 b.need(not derived.exists(),'Fresh authoring manifest path required')
 derived.write_text(json.dumps(data,indent=2)+'\n')
 derived_pin=b.pin(derived)
 b.need(base.read_bytes()==raw and b.sha(base.read_bytes())==base_sha,'Shared base manifest changed during author-only derivation')
 return derived,{'path':str(base),'sha256':base_sha,'bytes':len(raw)},derived_pin

def read_author_json(path):
 p=Path(path).resolve(strict=True);raw=p.read_bytes();b.need(len(raw)<=16*1024*1024,'Bounded authoring JSON required');value=json.loads(raw,object_pairs_hook=owned.require('skirmish_source_syntax').no_duplicates,parse_constant=lambda x:(_ for _ in ()).throw(ValueError('Nonfinite authoring JSON')));return value,{'path':str(p),'sha256':b.sha(raw),'bytes':len(raw)}
def merge_authoring_graph_pins(pins,graph_pins,repo,data_repo):
 """Verify every graph source, then merge once; final pre/post-worker checks remain."""
 collected={}
 for path,digest in graph_pins.items():
  target=Path(path).resolve(strict=True);root=repo if target.suffix=='.cs'else data_repo
  b.need(target.is_relative_to(root),'Authoring graph pin outside explicit source/data root')
  b.need(b.pin(target)['sha256']==digest,'Actual profile/source asset changed: '+path)
  key=str(target);b.need(key not in collected or collected[key]==digest,'Conflicting authoring graph pin: '+key);collected[key]=digest
 b.merge_pins(pins,collected)
def validate_resource_preset_selection(descriptor,graph,data_repo):
 b.need(type(descriptor)is dict and set(descriptor)=={'path','sha256'},'Exact optional resource-preset descriptor required')
 target=Path(descriptor['path']).resolve(strict=True);data_repo=Path(data_repo).resolve(strict=True)
 b.need(target.is_relative_to(data_repo) and target.suffix=='.asset','Resource preset must be a project asset within the explicit data root')
 actual=b.pin(target);b.need(actual['sha256']==descriptor['sha256'],'Resource-preset descriptor hash mismatch')
 graph_pins=graph.get('source_pins');b.need(type(graph_pins)is dict and graph_pins.get(str(target))==actual['sha256'],'Resource preset must match an exact source-graph pin')
 relative=target.relative_to(data_repo).as_posix();nodes=graph.get('nodes')
 matches=[n for n in nodes if type(n)is dict and n.get('source_path')==relative]if type(nodes)is list else []
 b.need(len(matches)==1 and matches[0].get('source_type')=='SkirmishPresetConfig' and matches[0].get('source_sha256')==actual['sha256'],
        'Resource preset must resolve to exactly one source-hash-matched SkirmishPresetConfig graph node')
 fields=matches[0].get('fields');value=fields.get('supplyDaySeconds')if type(fields)is dict else None
 spellings=matches[0].get('scalar_spellings');raw=spellings.get('supplyDaySeconds')if type(spellings)is dict else None
 try:raw_value=float(raw)if type(raw)is str else float('nan')
 except (ValueError,OverflowError):raw_value=float('nan')
 b.need(type(value)in (int,float)and math.isfinite(value)and value>0 and math.isfinite(raw_value)and raw_value==value,
        'Resource preset supplyDaySeconds must be a matching finite positive serialized graph scalar')
 return {'path':str(target),'sha256':actual['sha256']}
def validate_authoring_draft_fields(draft,mission,seed):
 required={'schema_version','kind','catalog_id','seed','size','difficulty','graph','profiles','campaign_source_path','layout','setup_matrix'}
 optional=[set(),{'binding'},{'native_binding'},{'native_binding','external_pad_candidate'}]
 accepted={frozenset(required|items|({'resource_preset'} if use_preset else set())) for items in optional for use_preset in (False,True)}
 b.need(type(draft)is dict and frozenset(draft) in accepted,'Exact finite authoring input fields required')
 b.need(draft.get('schema_version')==1 and draft.get('kind')=='SkirmishMissionAuthoringDraft'and draft.get('catalog_id')==mission and draft.get('seed')==seed,'Actual new mission authoring identity required')
 b.need(draft.get('size')in {'Standard','War','LargeWar'}and draft.get('difficulty')in {'Recruit','Regular','Veteran','Commander'},'Native size/difficulty names required')
def validate_owner_projection(projection,request_pin,result,mission,seed):
 b.need(projection.get('kind')=='SkirmishMissionAuthoringOwnerProjection' and projection.get('status')=='Partial_CompiledOwnerInputsOnly' and projection.get('scope')=='OriginalConfigureAndTryCompileOwnerOutputAndExactRequestPins;NoInitializationOrRuntimeAdmission','Explicit non-initialization owner projection required')
 target=projection.get('target',{});resolved=result.get('resolved_setup',{})
 b.need(target.get('mission_id')==mission and target.get('seed')==seed and target.get('setup_hash')==resolved.get('setup_hash') and target.get('size')==result.get('size') and target.get('difficulty')==result.get('difficulty'),'Owner projection target differs from actual compiled authoring result')
 b.need(isinstance(resolved.get('operation_map_id'),str) and resolved['operation_map_id'] and target.get('operation_map_id')==resolved['operation_map_id'] and isinstance(result.get('operation_map_id'),str) and result['operation_map_id'] and target.get('source_definition_operation_map_id')==result['operation_map_id'] and isinstance(result.get('physical_source_operation_map_id'),str) and result['physical_source_operation_map_id'] and target.get('source_operation_map_id')==result['physical_source_operation_map_id'],'Logical setup, source definition and physical source map identities must match their respective owner outputs')
 owner_request=projection.get('owner_request',{})
 b.need(owner_request.get('path')==request_pin.get('path') and owner_request.get('sha256')==request_pin.get('sha256') and projection.get('world_allocated') is False and projection.get('runtime_admission') is False and projection.get('mission_ready') is False and projection.get('source_fit_certified') is False,'Owner request pin or non-readiness boundary differs')
 b.need(b.pin(owner_request['path'])['sha256']==owner_request['sha256'],'Owner request changed since projection')
 descriptors=projection.get('owner_descriptors',{})
 base={'source_config_graph','setup_matrix','layout','logical_binding','canonical_battle_catalog'}
 b.need(set(descriptors) in (base,base|{'resource_preset'}),'Exact finite owner descriptor set required')
 for name,value in descriptors.items():
  b.need(type(value)is dict and set(value)=={'path','sha256'} and Path(value['path']).is_absolute() and len(value['sha256'])==64,'Exact pinned owner descriptor required: '+name)
  actual=b.pin(value['path']);b.need(actual['sha256']==value['sha256'],'Owner descriptor changed: '+name)
 if 'resource_preset' in descriptors:
  result_pins=result.get('input_pins',{})
  request_record=result_pins.get(owner_request['path'],{})
  preset=descriptors['resource_preset'];preset_record=result_pins.get(preset['path'],{})
  b.need(request_record.get('sha256')==owner_request['sha256'] and preset_record.get('sha256')==preset['sha256'],
         'Selected resource preset is absent from exact compiler input pins')
 missing=projection.get('runtime_fields_not_supplied_by_this_projection',[])
 b.need(all(name in missing for name in ('GeneratedMissionInitialization','camera_profile','camera_values','camera_motion')),'Missing runtime-input disclosure required')
 b.need(all(projection.get(k)is False for k in ('world_allocated','runtime_admission','source_fit_certified','mission_ready','native_acceptance')),'Owner projection must remain non-admitted')
 return True
def author(repo,package,manifest,output,mission,seed,authoring_input,build_receipt=None,data_repo=None,timeout=120,owner_projection=False):
 repo=Path(repo).resolve(strict=True);data_repo=Path(data_repo).resolve(strict=True)if data_repo else repo;out=Path(output).absolute();b.need(isinstance(mission,str) and len(mission)==4 and mission[0]=='S' and mission[1:].isascii() and mission[1:].isdigit() and 1<=int(mission[1:])<=120 and type(seed)is int and 1<=seed<=2147483647 and (mission!='S026' or seed==104755),'Canonical mission and positive seed required; S026 retains seed 104755; no runtime admission');b.need(math.isfinite(timeout)and timeout>0,'Finite positive authoring timeout required');b.need(not out.exists(),'Fresh isolated authoring output required');out.mkdir(parents=True)
 compile_manifest_pin=None;derived_manifest_pin=None
 if build_receipt is None:
  # Compilation profile selects the existing finite host closure ONLY; no S002 world is initialized.
  compile_manifest,compile_manifest_pin,derived_manifest_pin=prepare_authoring_manifest(manifest,package,out)
  b.build(repo,package,compile_manifest,out/'build','S002',104731);build_receipt=out/'build/build.json'
 receipt,build_pin=b.read_json_pinned(build_receipt);b.need(receipt.get('status')=='Passed'and receipt.get('mission')=='S002'and receipt.get('seed')==104731,'Qualified finite tool build profile required');b.need(receipt.get('repository_root')==str(repo),'Authoring source root differs from build');b.verify(receipt['source_and_reference_pins'])
 qualification_source=repo/AUTHOR_CONFIGURATION_QUALIFICATION['path'];qualification_pin=b.pin(qualification_source)
 b.need(receipt['source_and_reference_pins'].get(qualification_pin['path'])==qualification_pin['sha256'],
        'Qualified finite tool build is missing the shared configuration qualification adapter')
 draft,draft_pin=read_author_json(authoring_input)
 validate_authoring_draft_fields(draft,mission,seed)
 pins=dict(receipt['source_and_reference_pins']);b.merge_pins(pins,owned.pins());b.merge_pins(pins,{build_pin['path']:build_pin['sha256'],draft_pin['path']:draft_pin['sha256']});
 if compile_manifest_pin is not None:b.merge_pins(pins,{compile_manifest_pin['path']:compile_manifest_pin['sha256']})
 request=dict(draft);request['kind']='SkirmishMissionAuthoringPreflight';request['source_repository_root']=str(repo);request['data_repository_root']=str(data_repo);request['output_directory']=str(out)
 graph_value=None
 for key in ['graph','layout','setup_matrix']+(['binding']if'binding'in draft else[])+(['native_binding']if'native_binding'in draft else[])+(['external_pad_candidate']if'external_pad_candidate'in draft else[])+(['resource_preset']if'resource_preset'in draft else[]):
  row=draft[key];b.need(isinstance(row,dict)and set(row)=={'path','sha256'},'Exact descriptor '+key);value,pin=read_author_json(row['path'])if key in ('graph','layout','binding','native_binding','external_pad_candidate')else(None,b.pin(row['path']));b.need(pin['sha256']==row['sha256'],'Authoring descriptor changed: '+key);b.merge_pins(pins,{pin['path']:pin['sha256']});request[key]={'path':pin['path'],'sha256':pin['sha256']}
  if key=='graph':
   graph_value=value
   merge_authoring_graph_pins(pins,value['source_pins'],repo,data_repo)
  if key=='resource_preset':
   b.need(graph_value is not None,'Resource preset requires the actual source graph descriptor')
   request[key]=validate_resource_preset_selection(row,graph_value,data_repo)
  if key=='external_pad_candidate':
   b.need(pin['sha256'] in ('4588397b1c8c67bb4c71d83afc18bfdbce69eaa44245cb9792ab3d985e55c7f5','0a412bf51e718e3fba00d1a89f3699a9b8959c200933eb8d20c097fc0db86f61') and value.get('runtime_admission') is False and value.get('source_fit_certified') is False and value.get('mission_ready') is False,'Exact unqualified S026 candidate required')
   for evidence in ('source_owner_control','footprint_owner_control')+(('source_prior_candidate',) if value.get('kind')=='S026ExternalDeclaredTypedPadCandidate' else ()):
    entry=value.get(evidence);b.need(type(entry)is dict and set(entry)=={'path','sha256'},'Exact candidate evidence descriptor required');ep=b.pin(entry['path']);b.need(ep['sha256']==entry['sha256'],'Candidate evidence changed: '+evidence);b.merge_pins(pins,{ep['path']:ep['sha256']})
 tools={k:Path(v).resolve(strict=True)for k,v in receipt['runtime_selection'].items()};b.need(set(tools)=={'contents','script','engine','mono','newtonsoft'},'Recorded runtime selection required');b.need(str(tools['mono'])in pins and str(tools['newtonsoft'])in pins,'Unpinned runtime selection');config,extra=b.prepare_mono_config(tools,out);b.merge_pins(pins,extra);runtime_pin=b.pin(config);request['runtime_config']={'path':runtime_pin['path'],'sha256':runtime_pin['sha256']};b.merge_pins(pins,{str(config):runtime_pin['sha256']});request_path=out/'authoring-request.json';b.write(request_path,request);request_pin=b.pin(request_path);b.merge_pins(pins,{str(request_path):request_pin['sha256']});exe=receipt['executable'];b.merge_pins(pins,{exe['path']:exe['sha256']});b.verify(pins)
 env=os.environ.copy();env['MONO_PATH']=os.pathsep.join([str(repo/'Library/ScriptAssemblies'),str(tools['engine']),str(tools['newtonsoft'].parent),str(Path(receipt['placement_build']).parent)]);start=time.monotonic();timed_out=False
 owner_projection_path=out/'source-owner-projection.json' if owner_projection else None
 with(out/'process.log').open('w')as log:
  command=[str(tools['mono']),'--config',str(config),'--optimize=-float32',exe['path']]
  if owner_projection:command+=['--authoring-owner-projection',str(request_path),request_pin['sha256'],str(owner_projection_path)]
  else:command+=['--authoring-preflight',str(request_path),request_pin['sha256']]
  process=subprocess.Popen(command,env=env,stdout=log,stderr=subprocess.STDOUT)
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
 projection_pin=None
 if owner_projection and valid:
  projection,projection_pin=b.read_json_pinned(owner_projection_path)
  validate_owner_projection(projection,request_pin,raw,mission,seed)
 report={'schema_version':1,'kind':'SkirmishMissionAuthoringInvocation','status':('ConfigurationCompiled'if raw['status']=='ConfigurationCompiled'else'DraftInspected')if valid else'Failed','authoring_mission':mission,'authoring_seed':seed,'compilation_profile':{'mission':'S002','seed':104731,'scope':'Finite source-tool compilation only; no S002 world initialized'},'process_exit':process.returncode,'timed_out':timed_out,'wall_seconds':time.monotonic()-start,'source_repository_root':str(repo),'data_repository_root':str(data_repo),'source_result':raw,'result_pin':result_pin,'input_and_runtime_pins':pins,'world_allocated':False,'validation_passed':False,'source_fit_verified':False,'mission_ready':False,'native_acceptance':False}
 if compile_manifest_pin is not None:report['authoring_build_manifest']={'base':compile_manifest_pin,'derived':derived_manifest_pin,'existing_adapter':AUTHOR_CONFIGURATION_QUALIFICATION['path'],'base_profile_preserved':True,'base_manifest_bytes_unchanged':True}
 if owner_projection:report['partial_owner_projection']={'path':str(owner_projection_path),'sha256':projection_pin['sha256'] if projection_pin else None,'status':'PassedPartialOwnerProjection' if projection_pin else 'Failed'}
 b.write(out/'authoring-receipt.json',report);return report
