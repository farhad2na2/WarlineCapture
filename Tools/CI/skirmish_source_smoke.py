"""One bounded source smoke report. No Unity, player build or mission-readiness certificate."""
from pathlib import Path
import json,math,os,signal,subprocess,sys,time
import skirmish_owned_source as owned
from skirmish_source_profiles import require_profile # admitted first by owned.load_cli
import skirmish_source_build as b

PENDING_COVERAGE=[
 {"id":"physical_layout_routes_packing","status":"Unsupported","reason":"The existing temporary physical harness is not a durable registered stage."},
 {"id":"native_root_required_infrastructure","status":"Unsupported","reason":"Source-root/required-anchor runtime owner join is not yet in the published ordinary profile."},
 {"id":"full_gameplay_and_player_acceptance","status":"NotChecked","reason":"Smoke does not establish autonomous outcome, nativeUI/render/result/return or player acceptance."}]

def _json(path,maximum=16*1024*1024):
 p=Path(path).resolve(strict=True)
 with p.open('rb')as stream:raw=stream.read(maximum+1)
 b.need(len(raw)<=maximum,'Bounded smoke JSON required: '+str(p))
 value=json.loads(raw,object_pairs_hook=owned.require('skirmish_source_syntax').no_duplicates,parse_constant=lambda value:(_ for _ in()).throw(ValueError('Nonfinite JSON: '+value)))
 return value,{'path':str(p),'sha256':b.sha(raw),'bytes':len(raw)}

def _descriptor(row,maximum=256*1024*1024):
 b.need(isinstance(row,dict)and set(row)in[{'path','sha256'},{'path','sha256','bytes'}],'Exact smoke path/sha descriptor required')
 b.need(Path(row['path']).is_absolute(),'Absolute smoke descriptor path required');path=Path(row['path']).resolve(strict=True)
 b.need(path.stat().st_size<=maximum,'Smoke input exceeds bound: '+str(path))
 pin=b.pin(path);b.need(pin['sha256']==row['sha256'],'Input changed: '+str(path));b.need('bytes'not in row or type(row['bytes'])is int and row['bytes']==pin['bytes'],'Descriptor byte count changed: '+str(path));return pin

def _command(command,out,timeout,env=None):
 b.need(math.isfinite(timeout)and timeout>0,'Smoke wall budget exhausted')
 out=Path(out);out.mkdir(parents=True,exist_ok=True);start=time.monotonic();timed_out=False
 with(out/'process.log').open('w')as log:
  process=subprocess.Popen(list(map(str,command)),stdout=log,stderr=subprocess.STDOUT,start_new_session=True,env=env)
  try:process.wait(timeout=timeout)
  except subprocess.TimeoutExpired:timed_out=True
  finally:
   if process.poll()is None:
    # Only this newly created owned process group, never Hub/Unity or another job.
    os.killpg(process.pid,signal.SIGTERM)
    try:process.wait(timeout=.25)
    except subprocess.TimeoutExpired:os.killpg(process.pid,signal.SIGKILL);process.wait()
 result={'argv':list(map(str,command)),'process_exit':process.returncode,'timed_out':timed_out,'wall_seconds':time.monotonic()-start}
 b.write(out/'process.json',result);return result

def _checked_source_build(ctx):
 receipt,pin=_json(ctx['build_receipt']);b.need(receipt.get('kind')=='SkirmishSourceSimulationBuild'and receipt.get('status')=='Passed'and type(receipt.get('compile_exit'))is int and receipt['compile_exit']==0,'Passed current finite source build required')
 target_mission=ctx['mission']if ctx['mission']=='S002'or ctx['bundle']is not None else'S002';target_seed=ctx['seed']if ctx['mission']=='S002'or ctx['bundle']is not None else 104731
 expected_backend=owned.require('skirmish_source_profiles').runtime_backend(target_mission)
 owned.require('run_skirmish_source_batch').require_build_target(receipt,target_mission,target_seed,expected_backend,owned.require('skirmish_source_profiles').derived())
 if ctx['bundle']is not None:
  bundle,_=_json(ctx['bundle']);b.need(bundle.get('schema_version')==1 and bundle.get('mission')==ctx['mission']and type(bundle.get('seed'))is int and bundle['seed']==ctx['seed'],'Actual smoke bundle tuple required');b.need(bundle.get('runtime_backend','LegacyGroundS002V1')==expected_backend,'Bundle/build runtime backend mismatch')
 b.need(receipt.get('repository_root')==str(ctx['repo']),'Smoke source root differs from build')
 b.verify(receipt['source_and_reference_pins']);b.need(b.pin(ctx['manifest'])['sha256']==receipt['source_manifest']['sha256'],'Current source manifest differs; rebuild')
 resolution,_=_json(receipt['source_resolution']);b.need(any(row.get('id')=='SourceSmokeTool.cs'for row in resolution['resolved']),'Build lacks current preworld smoke entry; rebuild')
 exe=receipt['executable'];b.need(b.pin(exe['path'])['sha256']==exe['sha256'],'Built executable changed');ctx['build']=receipt;ctx['pins'][pin['path']]=pin['sha256'];return {'source_count':receipt['source_count'],'build_receipt':pin,'reuse_checked':True}

def _profile(ctx):
 if(ctx['mission'],ctx['seed'])!=('S026',104755):
  try:require_profile(ctx['mission'],ctx['seed'])
  except ValueError as error:return 'Unsupported','profile',str(error),None
 return 'Passed','profile','Actual requested mission/seed selected; build profile is checked separately and runtime backend must agree.',None

def _inputs(ctx):
 if ctx['mission']=='S026'and ctx['bundle']is None:
  b.need(ctx['authoring_input']is not None,'S026 smoke requires --authoring-input')
  draft,pin=_json(ctx['authoring_input']);b.need(draft.get('catalog_id')=='S026'and draft.get('seed')==104755,'Wrong actual authoring identity');ctx['pins'][pin['path']]=pin['sha256'];return 'Passed','authoring_input','Pinned actual authoring draft; native rejection remains authoritative.',pin
 b.need(ctx['bundle']is not None,'S002 smoke requires --data-bundle');bundle,pin=_json(ctx['bundle']);b.need(bundle.get('schema_version')==1 and bundle.get('mission')==ctx['mission']and bundle.get('seed')==ctx['seed'],f"Actual {ctx['mission']}/{ctx['seed']} bundle required");ctx['pins'][pin['path']]=pin['sha256'];ctx['bundle_value']=bundle
 for name in ['initialization','unit_metadata','static_clearance','camera_values','camera_motion']:
  row=_descriptor(bundle[name]);ctx['pins'][row['path']]=row['sha256']
 initial,_=_json(bundle['initialization']['path']);require_profile(initial['mission_id'],initial['selection']['seed'],initial['selection']['difficulty_id'],initial['selection']['size_id']);b.need(initial.get('mission_id')==ctx['mission']and initial.get('selection',{}).get('seed')==ctx['seed'],'Actual initialization mission/seed mismatch')
 for name in ['source_config_graph','setup_matrix']:
  row=_descriptor(initial[name]);ctx['pins'][row['path']]=row['sha256']
 graph,_=_json(initial['source_config_graph']['path'])
 for path,digest in graph['source_pins'].items():
  row=_descriptor({'path':path,'sha256':digest});ctx['pins'][row['path']]=row['sha256']
 return 'Passed','data_bundle','Declared bundle, initialization, graph/matrix and actual asset pins are stable.',{'data_bundle':pin,'mission':ctx['mission'],'seed':ctx['seed']}

def _catalog_cases(ctx):
 module=owned.require('skirmish_offline_sim_cases');suite,manifest=module.generate(ctx['data_repo']);out=ctx['out']/'objective-cases';out.mkdir();b.write(out/'cases.json',suite);b.write(out/'manifest.json',manifest);ctx['cases']=out/'cases.json'
 for path in [ctx['data_repo']/'Design/Roadmap/Skirmish_Expansion/SCENARIO_CATALOG.csv',ctx['data_repo']/'Design/Roadmap/Skirmish_Expansion/INITIAL_SETUP_MATRIX.csv']:
  pin=b.pin(path);ctx['pins'][pin['path']]=pin['sha256']
 return 'Passed','canonical_catalog_and_matrix','Existing validator accepted120IDs/360vectors; generated synthetic rule probes.',manifest

def _source_build(ctx):
 if ctx['build_receipt']is None:
  command=[sys.executable,'-B',ctx['package']/'Tools/CI/run_skirmish_source_simulation.py','build','--repo',ctx['repo'],'--package-root',ctx['package'],'--manifest',ctx['manifest'],'--mission',ctx['mission']if ctx['mission']=='S002'or ctx['bundle']is not None else'S002','--seed',str(ctx['seed']if ctx['mission']=='S002'or ctx['bundle']is not None else 104731),'--output',ctx['out']/'source-build']
  bound=owned.require('skirmish_source_profiles').derived()
  if bound is not None:
   b.need(ctx['mission']=='S002' and ctx['seed']==bound[0]['seed'],'Derived seed must match smoke build target')
   command+=['--s002-derived-seed',bound[1]['path']]
   ctx['pins'][bound[1]['path']]=bound[1]['sha256']
  result=_command(command,ctx['out']/'source-build-process',ctx['remaining']());b.need(not result['timed_out']and result['process_exit']==0,'Source build failed/timedout; see source-build-process/process.log');ctx['build_receipt']=ctx['out']/'source-build/build.json'
 evidence=_checked_source_build(ctx);return 'Passed','build','Checked source build reused by native diagnostics and startup/full worker.',evidence

def _native_data(ctx):
 if owned.require('skirmish_source_profiles').runtime_backend(ctx['mission'])=='TypedGroundS026V1'and ctx['bundle']is not None:
  out=ctx['out']/'native-runtime-data';prepared=b.run(ctx['repo'],ctx['build_receipt'],ctx['bundle'],out,None,ctx['remaining'](),ctx['data_repo'],True);receipt=ctx['build'];tools={k:Path(v)for k,v in receipt['runtime_selection'].items()};request=out/'run-request.json';pins,_=_json(out/'input-pins.json');b.verify(pins)
  env=os.environ.copy();env['MONO_PATH']=os.pathsep.join([str(ctx['repo']/'Library/ScriptAssemblies'),str(tools['engine']),str(tools['newtonsoft'].parent),str(Path(receipt['placement_build']).parent)])
  command=[tools['mono'],'--config',out/'mono-config.xml','--optimize=-float32',receipt['executable']['path'],'--source-run-preflight',request]
  invocation=_command(command,out/'invocation',ctx['remaining'](),env);b.verify(pins);b.need(not invocation['timed_out']and invocation['process_exit']==0,'S026 original compiler/metadata preflight failed; see native-runtime-data/invocation/process.log');log=(out/'invocation/process.log').read_text();marker='[SourceRunPreflight] ';lines=[x[len(marker):]for x in log.splitlines()if x.startswith(marker)];b.need(len(lines)==1,'One exact source preflight receipt required');raw=json.loads(lines[0]);b.need(raw.get('status')=='Passed'and raw.get('world_allocated')is False and raw.get('entity_ids_allocated')==0,'NoWorld preflight contract differs');initial,_=_json(ctx['bundle_value']['initialization']['path'])
  if initial.get('metadata_compiler_context')is not None:
   compiler_context,_=_json(initial['metadata_compiler_context']['path']);target=compiler_context['target_context']
   raw['configuration_qualification_scope']={'selected_context':{key:target[key]for key in ['catalog_id','size','difficulty','seed','setup_hash']},'selected_original_compiler_result':'Passed'if raw.get('status')=='Passed'else raw.get('status'),'unselected_binding_contexts':[{'size':size,'difficulty':target['difficulty'],'qualification':'ExplicitUnsupported','execution':'NotRun'}for size in ['Standard','War','LargeWar']if size!=target['size']],'canonical_all_size_budget_diagnostics':'Separate unchanged catalog_matrix_csv diagnostics; not normal binding compilation or source-fit','mission_ready':False}
  ctx['native_data']=raw;return 'Passed','native_data','Actual shared initialization compiler and production dual-origin metadata reader; no World/IDs.',raw
 if ctx['mission']=='S026':
  author=owned.require('skirmish_source_authoring').author(ctx['repo'],ctx['package'],ctx['manifest'],ctx['out']/'native-author',ctx['mission'],ctx['seed'],ctx['authoring_input'],ctx['build_receipt'],ctx['data_repo'],ctx['remaining']())
  if author.get('status')not in{'DraftInspected','ConfigurationCompiled'}:return 'Failed','authoring','Current native author invocation failed.',author
  if author['status']=='ConfigurationCompiled':return 'Passed','authoring.compiler','Original config compiler/capture consistent; runtime/source-fit/player gates remain separate.',author['source_result']
  return 'Blocked','authoring.compiler','Original source compiler rejects this draft; no world was allocated.',author['source_result']
 receipt=ctx['build'];tools={k:Path(v)for k,v in receipt['runtime_selection'].items()};out=ctx['out']/'native-data';out.mkdir();config,extra=b.prepare_mono_config(tools,out);request={'schema_version':1,'kind':'SkirmishSourceSmokeData','mission':'S002','seed':ctx['seed'],'initialization':{k:ctx['bundle_value']['initialization'][k]for k in['path','sha256']},'runtime_config':b.pin(config),'output_directory':str(out)};request['runtime_config']={k:request['runtime_config'][k]for k in ['path','sha256']};b.write(out/'request.json',request);request_pin=b.pin(out/'request.json');pins=dict(receipt['source_and_reference_pins']);b.merge_pins(pins,owned.pins());b.merge_pins(pins,extra);b.merge_pins(pins,{request_pin['path']:request_pin['sha256'],str(config):b.pin(config)['sha256']});b.verify(pins)
 env=os.environ.copy();env['MONO_PATH']=os.pathsep.join([str(ctx['repo']/'Library/ScriptAssemblies'),str(tools['engine']),str(tools['newtonsoft'].parent),str(Path(receipt['placement_build']).parent)]);command=[tools['mono'],'--config',config,'--optimize=-float32',receipt['executable']['path'],'--source-smoke-data',out/'request.json',request_pin['sha256']]
 invocation=_command(command,out/'invocation',ctx['remaining'](),env);b.verify(pins);b.need(not invocation['timed_out']and invocation['process_exit']==0,'Native data smoke failed/timedout; see native-data/invocation/process.log');raw,_=_json(out/'native-data-result.json');b.need(raw.get('kind')=='SkirmishNativeDataSmokeResult'and raw.get('world_allocated')is False,'Native data smoke contract mismatch');ctx['native_data']=raw
 status='Passed'if all(x['status']=='Passed'for x in raw['stages'])else'Failed'
 return status,'native_data','Current original authored-catalog/matrix/all-size-budget/selected-compiler diagnostics.',raw

def _objective_build(ctx):
 module=owned.require('skirmish_offline_sim')
 if ctx['objective_build']is None:
  target=ctx['out']/'objective-build';command=[sys.executable,'-B',ctx['package']/'Tools/CI/skirmish_offline_sim.py','build','--project',ctx['repo'],'--output',target];result=_command(command,ctx['out']/'objective-build-process',ctx['remaining']());b.need(not result['timed_out']and result['process_exit']==0,'Objective producer build failed/timedout');ctx['objective_build']=target
 manifest,pin=_json(Path(ctx['objective_build'])/'build.json');b.need(set(manifest['source_pins'])==set(module.SOURCES+[module.BASE,module.HARNESS]),'Objective build source coverage mismatch')
 for path,digest in manifest['source_pins'].items():b.need(b.pin(ctx['repo']/path)['sha256']==digest,'Objective build differs from current source: '+path)
 for path,digest in manifest['build_files'].items():b.need(b.pin(Path(ctx['objective_build'])/path)['sha256']==digest,'Objective build artifact changed: '+path)
 ctx['pins'][pin['path']]=pin['sha256'];return 'Passed','objective_build','Checked existing .NET8 objective producer; no Unity or source-world rebuild.',{'build_manifest':pin,'sdk':manifest['sdk'],'scope':manifest['scope']}

def _objective_rules(ctx):
 command=[sys.executable,'-B',ctx['package']/'Tools/CI/skirmish_offline_sim.py','run','--build',ctx['objective_build'],'--input',ctx['cases'],'--output',ctx['out']/'objective-run'];result=_command(command,ctx['out']/'objective-run-process',ctx['remaining']());b.need(not result['timed_out']and result['process_exit']==0,'Shared objective replay failed/timedout; see objective-run-process/process.log');raw,_=_json(ctx['out']/'objective-run/report.json');b.need(raw.get('result')=='passed'and raw.get('failed')==0,'Original objective rule probes failed');return 'Passed','objective_rules','Existing BA/FC/BT/CE original reducers passed synthetic probes; no battle facts produced.',{k:raw[k]for k in ['result','passed','failed','elapsed_seconds','native_runs','full_battle_simulated']if k in raw}

def _startup(ctx):
 if not ctx['startup']:return 'DeferredToFullRun','startup','Actual full worker startup is the check; no duplicate world initialization.',None
 result=b.run(ctx['repo'],ctx['build_receipt'],ctx['bundle'],ctx['out']/'startup',5,ctx['remaining'](),ctx['data_repo']);ctx['startup_result']=result
 return ('Passed'if result.get('diagnostic_prefix_completed')and result.get('process_exit')==0 else'Failed'),'startup','Ordinary five-tick source startup; actual failures propagate.',{k:result.get(k)for k in ['status','process_exit','timed_out','wall_seconds','validation_passed','full_outcome_observed','diagnostic_prefix_completed','source_result']}

# Finite stage hooks. Add an actual shared-source adapter here, never a saved success flag.
STAGES=[('profile',_profile,()),('input_integrity',_inputs,('profile',)),('catalog_matrix_csv',_catalog_cases,()),('source_build',_source_build,('profile','input_integrity','catalog_matrix_csv')),('native_data',_native_data,('source_build','input_integrity')),('objective_build',_objective_build,('catalog_matrix_csv',)),('objective_rules',_objective_rules,('objective_build','catalog_matrix_csv')),('startup',_startup,('native_data','source_build','input_integrity','objective_rules'))]

def smoke(repo,package,manifest,output,mission,seed,bundle=None,authoring_input=None,build_receipt=None,objective_build=None,data_repo=None,timeout=120,startup=True):
 b.need(math.isfinite(timeout)and timeout>0,'Finite positive smoke wall timeout required');out=Path(output).absolute();b.need(not out.exists(),'Fresh isolated smoke output required');out.mkdir(parents=True);start=time.monotonic();deadline=start+timeout
 ctx={'repo':Path(repo).resolve(strict=True),'package':Path(package).resolve(strict=True),'manifest':Path(manifest).resolve(strict=True),'out':out,'mission':mission,'seed':seed,'bundle':bundle,'authoring_input':authoring_input,'build_receipt':build_receipt,'objective_build':objective_build,'data_repo':Path(data_repo).resolve(strict=True)if data_repo else Path(repo).resolve(strict=True),'remaining':lambda:max(.001,deadline-time.monotonic()),'pins':owned.pins(),'startup':startup};records=[];by_id={}
 for ident,call,deps in STAGES:
  began=time.monotonic();blocked=[name for name in deps if by_id[name]['status']!='Passed']
  if blocked:status,field,reason,evidence='Skipped',ident,'Depends on nonpassing stages: '+', '.join(blocked),None
  elif time.monotonic()>=deadline:status,field,reason,evidence='Blocked','wall_timeout','Overall smoke wall budget exhausted.',None
  else:
   try:
    status,field,reason,evidence=call(ctx)
    b.need(status in {'Passed','Failed','Blocked','Unsupported','Skipped','DeferredToFullRun'},'Unknown smoke stage status: '+str(status))
    b.need(status!='DeferredToFullRun'or ident=='startup'and not startup,'Only fullrun startup can be deferred')
   except Exception as error:status,field,reason,evidence='Failed',ident,str(error),None
  row={'id':ident,'status':status,'field':field,'reason':reason,'wall_seconds':time.monotonic()-began,'evidence':evidence};records.append(row);by_id[ident]=row;b.write(out/'smoke-progress.json',{'stages':records,'validation_passed':False})
 try:b.verify(ctx['pins'])
 except Exception as error:records.append({'id':'postrun_input_epoch','status':'Failed','field':'input_pins','reason':str(error),'wall_seconds':0,'evidence':None})
 if time.monotonic()>deadline:records.append({'id':'wall_budget','status':'Blocked','field':'timeout','reason':'Overall smoke wall budget exceeded; no further dependent stage was launched.','wall_seconds':0,'evidence':None})
 failed=any(x['status']=='Failed'for x in records);blocked=any(x['status']in{'Blocked','Unsupported','Skipped'}for x in records);status='Failed'if failed else'Blocked'if blocked else'SmokePassed'if startup else'PreflightPassed';exit_code=1 if failed else 2 if blocked else 0
 result={'schema_version':1,'kind':'SkirmishSourceSmokeReport','status':status,'exit_code':exit_code,'mission':mission,'seed':seed,'source_repository_root':str(ctx['repo']),'data_repository_root':str(ctx['data_repo']),'compilation_profile':{'mission':(ctx.get('build')or{}).get('mission','S002'),'seed':(ctx.get('build')or{}).get('seed',ctx['seed']if mission=='S002'else 104731),'scope':'Tool closure only, separate from actual authoring identity'},'stages':records,'unimplemented_coverage':PENDING_COVERAGE,'build_receipt':str(ctx['build_receipt'])if ctx['build_receipt']else None,'objective_build':str(ctx['objective_build'])if ctx['objective_build']else None,'checked_build_reused':build_receipt is not None,'wall_seconds':time.monotonic()-start,'wall_timeout_seconds':timeout,'input_pins':ctx['pins'],'validation_passed':False,'mission_ready':False,'source_fit_verified':False,'full_outcome_observed':False,'native_acceptance':False,'scope':'Only declared required smoke stages; synthetic objective facts are not real battle evidence. Unsupported future coverage is explicit.','repro':['python3','-B',str(ctx['package']/'Tools/CI/run_skirmish_source_simulation.py'),'smoke','--repo',str(ctx['repo']),'--data-repo',str(ctx['data_repo']),'--mission',mission,'--seed',str(seed),'--output','<fresh-output>']}
 for name,value in [('data-bundle',bundle),('authoring-input',authoring_input),('build-receipt',ctx['build_receipt']),('objective-build',ctx['objective_build'])]:
  if value is not None:result['repro']+=['--'+name,str(value)]
 bound=owned.require('skirmish_source_profiles').derived()
 if bound is not None:result['repro']+=['--s002-derived-seed',bound[1]['path']]
 result['repro']+=['--timeout',str(timeout)]
 for row in records:row['repro']=result['repro']
 b.write(out/'smoke-report.json',result);return result
