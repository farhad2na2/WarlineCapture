#!/usr/bin/env python3
"""One source smoke/build/run entry. SmokePassed covers declared fast checks only; it never grants mission readiness. Author inspects S026 drafts without World allocation."""
from __future__ import annotations
import argparse,json,sys,time,hashlib,subprocess,os
from pathlib import Path
sys.dont_write_bytecode=True
def _bootstrap_owned_source():
 import hashlib,types
 root=Path(__file__).resolve().parent;path=root/'skirmish_owned_source.py';raw=path.read_bytes()
 if len(raw)>2*1024*1024:raise ValueError('Bounded owned loader source required')
 if 'skirmish_owned_source' in sys.modules:raise ValueError('Unowned loader module collision')
 module=types.ModuleType('skirmish_owned_source');module.__file__=str(path);module.__executed_source_pin__={'path':str(path),'sha256':hashlib.sha256(raw).hexdigest()};sys.modules[module.__name__]=module
 try:
  exec(compile(raw,str(path),'exec'),module.__dict__)
  if path.read_bytes()!=raw:raise ValueError('Owned loader source changed during bootstrap')
  module.load_cli(root)
 except BaseException:
  if sys.modules.get(module.__name__)is module:del sys.modules[module.__name__]
  raise
 return module
owned=_bootstrap_owned_source()
from skirmish_source_manifest import resolve_manifest,UnsupportedSourceShape

def default_manifest(package,mission,command,data_bundle_present):
 if command=='author':
  return package/'Tools/CI/SkirmishSourceSimulation'/'source_manifest.plan.json'
 runtime=owned.require('skirmish_source_profiles').runtime_backend(mission)
 if mission=='S003' and (command in {'resolve','build','run'}or command=='smoke'and data_bundle_present):
  name='source_manifest.s003.plan.json'
 elif owned.require('skirmish_source_profiles').uses_s003_derived_factory(mission) and (command in {'resolve','build','run'}or command=='smoke'and data_bundle_present):
  name='source_manifest.s003derived.plan.json'
 elif runtime=='TypedGroundS026V1'and(command in {'resolve','build','run'}or command=='smoke'and data_bundle_present):
  name='source_manifest.s026.plan.json'
 else:
  name='source_manifest.plan.json'
 return package/'Tools/CI/SkirmishSourceSimulation'/name

def main():
 p=argparse.ArgumentParser(description=__doc__);p.add_argument('command',choices=['resolve','build','run','author','smoke','s004-diagnostic','s006-binding-inspect']);p.add_argument('--repo',default=str(Path(__file__).resolve().parents[2]));p.add_argument('--package-root',default=str(Path(__file__).resolve().parents[2]));p.add_argument('--manifest');p.add_argument('--output',required=True);p.add_argument('--mission',required=True);p.add_argument('--seed',type=int,default=104731);p.add_argument('--build-receipt');p.add_argument('--s002-derived-seed');p.add_argument('--configuration-qualification',help='Explicit experimental ground compiler/factory qualification JSON; no catalog/readiness promotion');p.add_argument('--objective-build',help='Existing checked .NET8 objective build directory for smoke reuse');p.add_argument('--data-repo');p.add_argument('--data-bundle');p.add_argument('--data-bundle-sha256');p.add_argument('--authoring-input',help='Hash-bound typed config diagnostics; ConfigurationCompiled/DraftInspected do not certify source-fit or mission readiness');p.add_argument('--authoring-input-sha256',help='Exact SHA256 for S006 source-only binding inspection request');p.add_argument('--owner-projection',action='store_true',help='Opt in to a separate partial compiled-owner projection; never creates GeneratedMissionInitialization or grants admission');p.add_argument('--prepare-only',action='store_true',help='Resolve/hash/prepare ordinary run inputs without allocating World or launching worker');p.add_argument('--max-ticks',type=int);p.add_argument('--timeout',type=float);p.add_argument('--s073-current-source-diagnostic',action='store_true',help='S073 only: invoke the original AI startup owner from current authored inputs; native observation parity remains unverified');p.add_argument('--s073-source-host-diagnostic-admission',action='store_true',help='S073 only: use same-world original startup facts for an unqualified source-host gameplay/ARIA diagnostic; native readiness, blocker parity, scheduler and outcome remain unverified');p.add_argument('--s073-source-transport-diagnostic',action='store_true',help='S073 only: opt in to source-host simulated transport presentation with captured static projection; no native parity/readiness claim');p.add_argument('--s073-ui-shell-state-owner-diagnostic',action='store_true',help='S073 only: explicitly invoke original UiShellStateSystem.OnCreate in the derived non-native source profile; requires its matching derived manifest');p.add_argument('--s073-historical-input-diagnostic',action='store_true',help='S073 historical Campaign input diagnostic; never readiness');p.add_argument('--s002-historical-input-diagnostic',action='store_true',help='S002 only: explicit bounded historical-input source-host diagnostic; unqualified and never victory evidence');p.add_argument('--s002-current-source-diagnostic-package');p.add_argument('--s002-current-source-diagnostic-package-sha256');a=p.parse_args();a.timeout=a.timeout if a.timeout is not None else(120 if a.command in {'smoke','author'}else 1800)
 if (os.environ.get('WARLINE_SOURCE_DIAGNOSTIC_CONFIG') or os.environ.get('WARLINE_SOURCE_DIAGNOSTIC_CONFIG_SHA256')) and (a.command!='run' or a.mission!='S003'):
  p.error('Source diagnostic configuration is valid only for an S003 run')
 if a.authoring_input_sha256 and a.command!='s006-binding-inspect':p.error('--authoring-input-sha256 is limited to s006-binding-inspect')
 if a.owner_projection and a.command!='author':p.error('--owner-projection is valid only for author')
 if a.authoring_input and a.command in {'run','build','resolve'}:p.error('--authoring-input is not consumed by this command; use a verified data bundle for run or the author command for inspection')
 if a.command=='s004-diagnostic':
  if a.mission!='S004' or a.seed!=104733 or not a.build_receipt or not a.data_bundle or not a.data_bundle_sha256:p.error('s004-diagnostic requires --mission S004 --seed 104733, --build-receipt, --data-bundle and --data-bundle-sha256')
  if a.manifest or a.data_repo or a.authoring_input or a.configuration_qualification or a.s002_derived_seed or a.objective_build or a.prepare_only or a.max_ticks is not None or a.s073_current_source_diagnostic or a.s073_source_host_diagnostic_admission or a.s073_source_transport_diagnostic or a.s073_ui_shell_state_owner_diagnostic or a.s073_historical_input_diagnostic or a.s002_historical_input_diagnostic or a.s002_current_source_diagnostic_package or a.s002_current_source_diagnostic_package_sha256:p.error('s004-diagnostic accepts only its explicit source-diagnostic inputs')
  root=Path(__file__).resolve().parents[2]
  if Path(a.repo).resolve()!=root or Path(a.package_root).resolve()!=root:p.error('s004-diagnostic requires the current checked source root')
  runner=root/'Tools/CI/run_skirmish_s004_source_diagnostic.py';raw=runner.read_bytes();runner_hash=hashlib.sha256(raw).hexdigest()
  args=[sys.executable,'-B',str(runner),'--build-receipt',a.build_receipt,'--data-bundle',a.data_bundle,'--data-bundle-sha256',a.data_bundle_sha256,'--output',a.output,'--timeout',str(a.timeout)]
  try: result=subprocess.run(args,cwd=root,capture_output=True,text=True,timeout=a.timeout+30)
  except subprocess.TimeoutExpired:print('S004 diagnostic wrapper timed out',file=sys.stderr);return 1
  if hashlib.sha256(runner.read_bytes()).hexdigest()!=runner_hash:print('S004 diagnostic runner changed during execution',file=sys.stderr);return 1
  if result.stdout:print(result.stdout,end='')
  if result.stderr:print(result.stderr,end='',file=sys.stderr)
  return result.returncode if result.returncode in (1,2) else 1
 if a.command=='s006-binding-inspect':
  import math,signal
  if a.mission!='S006' or a.seed!=104735 or not a.authoring_input or not a.authoring_input_sha256:p.error('s006-binding-inspect requires S006/104735 and a hash-pinned --authoring-input')
  if a.manifest or a.build_receipt or a.data_repo or a.data_bundle or a.data_bundle_sha256 or a.configuration_qualification or a.s002_derived_seed or a.objective_build or a.prepare_only or a.max_ticks is not None or a.s073_current_source_diagnostic or a.s073_source_host_diagnostic_admission or a.s073_source_transport_diagnostic or a.s073_ui_shell_state_owner_diagnostic or a.s073_historical_input_diagnostic or a.s002_historical_input_diagnostic or a.s002_current_source_diagnostic_package or a.s002_current_source_diagnostic_package_sha256:p.error('s006-binding-inspect accepts only its explicit unqualified inspection inputs')
  root=Path(__file__).resolve().parents[2]
  if Path(a.repo).resolve()!=root or Path(a.package_root).resolve()!=root:p.error('s006-binding-inspect requires the current checked source root')
  if not math.isfinite(a.timeout) or a.timeout<=0:p.error('A finite positive timeout is required')
  try:
   request_path=Path(a.authoring_input).resolve(strict=True)
   if not request_path.is_relative_to(root) or request_path.stat().st_size>1024*1024:raise ValueError('Bounded repository-owned request required')
   request_raw=request_path.read_bytes();request_hash=hashlib.sha256(request_raw).hexdigest()
   if request_hash!=a.authoring_input_sha256:raise ValueError('Inspection request hash mismatch')
   request=json.loads(request_raw,object_pairs_hook=owned.require('skirmish_source_syntax').no_duplicates,parse_constant=lambda value:(_ for _ in ()).throw(ValueError('Nonfinite inspection JSON')))
   if not isinstance(request,dict) or set(request)!={'schema_version','kind','mission','seed','size','difficulty','build','graph','matrix','candidate'} or type(request['schema_version']) is not int or request['schema_version']!=1 or request['kind']!='S006UnqualifiedBindingInspectionRequest' or request['mission']!='S006' or type(request['seed']) is not int or request['seed']!=104735:raise ValueError('Exact unqualified S006 inspection request required')
   if request['size'] not in {'Standard','War','LargeWar'} or request['difficulty'] not in {'Recruit','Regular','Veteran','Commander'}:raise ValueError('Exact native size and difficulty required')
   descriptors={}
   descriptor_limits={'build':16*1024*1024,'graph':16*1024*1024,'matrix':4*1024*1024,'candidate':2*1024*1024}
   for name in ('build','graph','matrix','candidate'):
    value=request[name]
    if not isinstance(value,dict) or set(value)!={'path','sha256'} or not isinstance(value['path'],str) or not Path(value['path']).is_absolute() or not isinstance(value['sha256'],str) or len(value['sha256'])!=64 or any(c not in '0123456789abcdef' for c in value['sha256']):raise ValueError('Exact absolute path/SHA256 descriptor required: '+name)
    path=Path(value['path']).resolve(strict=True)
    if not path.is_relative_to(root) or not path.is_file() or path.stat().st_size>descriptor_limits[name]:raise ValueError('Inspection descriptor size limit or source root invalid: '+name)
    if hashlib.sha256(path.read_bytes()).hexdigest()!=value['sha256']:raise ValueError('Inspection descriptor changed: '+name)
    descriptors[name]=str(path)
   entry=Path(__file__).resolve(strict=True);entry_hash=hashlib.sha256(entry.read_bytes()).hexdigest();runner=root/'Tools/CI/SkirmishSourceSimulation/Recipes/s006_external_delta/inspect_external_binding.py';runner_hash=hashlib.sha256(runner.read_bytes()).hexdigest()
   command=[sys.executable,'-B',str(runner),'inspect','--output',a.output,'--build-receipt',descriptors['build'],'--graph',descriptors['graph'],'--graph-sha256',request['graph']['sha256'],'--matrix',descriptors['matrix'],'--matrix-sha256',request['matrix']['sha256'],'--candidate',descriptors['candidate'],'--candidate-sha256',request['candidate']['sha256'],'--size',request['size'],'--difficulty',request['difficulty']]
   process=subprocess.Popen(command,cwd=root,stdout=subprocess.PIPE,stderr=subprocess.PIPE,text=True,start_new_session=True)
   try:stdout,stderr=process.communicate(timeout=a.timeout)
   except subprocess.TimeoutExpired:
    try:os.killpg(process.pid,signal.SIGTERM)
    except ProcessLookupError:pass
    try:process.communicate(timeout=5)
    except subprocess.TimeoutExpired:
     try:os.killpg(process.pid,signal.SIGKILL)
     except ProcessLookupError:pass
     process.communicate()
    raise
   result=subprocess.CompletedProcess(command,process.returncode,stdout,stderr)
   if hashlib.sha256(entry.read_bytes()).hexdigest()!=entry_hash or hashlib.sha256(runner.read_bytes()).hexdigest()!=runner_hash or hashlib.sha256(request_path.read_bytes()).hexdigest()!=request_hash:raise ValueError('Inspection entry, runner or request changed during execution')
   if result.returncode==0:
    output=Path(a.output).resolve(strict=True);child_receipt=output/'receipt.json';child_raw=child_receipt.read_bytes();child=json.loads(child_raw,object_pairs_hook=owned.require('skirmish_source_syntax').no_duplicates)
    if child.get('status')!='InspectedUnqualifiedCandidate' or child.get('world_allocated') is not False or child.get('runtime_admission') is not False or child.get('mission_ready') is not False:raise ValueError('Unqualified child receipt required')
    report={'kind':'S006UnqualifiedBindingCliInspection','status':'InspectedUnqualifiedCandidate','entry':{'path':str(entry),'sha256':entry_hash},'request':{'path':str(request_path),'sha256':request_hash},'runner':{'path':str(runner),'sha256':runner_hash},'worker_receipt':{'path':str(child_receipt),'sha256':hashlib.sha256(child_raw).hexdigest()},'binding_compiler_accepted':child['result']['binding_compiler_accepted'],'world_allocated':False,'validation_passed':False,'source_fit_verified':False,'runtime_admission':False,'mission_ready':False}
    with (output/'cli-inspection-receipt.json').open('x') as stream:json.dump(report,stream,indent=2);stream.write('\n')
  except (OSError,ValueError,KeyError,json.JSONDecodeError,subprocess.TimeoutExpired) as error:
   print('S006 unqualified binding inspection failed: '+str(error),file=sys.stderr);return 2
  if result.stdout:print(result.stdout,end='')
  if result.stderr:print(result.stderr,end='',file=sys.stderr)
  return result.returncode if result.returncode in (0,1,2) else 1
 if a.authoring_input_sha256:p.error('--authoring-input-sha256 is limited to s006-binding-inspect')
 if a.data_bundle_sha256:p.error('--data-bundle-sha256 is limited to s004-diagnostic')
 if a.s002_derived_seed:owned.require("skirmish_source_profiles").bind_s002_derived_seed(a.s002_derived_seed)
 if a.configuration_qualification:owned.require('skirmish_source_profiles').bind_configuration_qualification(a.configuration_qualification)
 package=Path(a.package_root).resolve();manifest=Path(a.manifest)if a.manifest else default_manifest(package,a.mission,a.command,a.data_bundle is not None)
 if a.s073_current_source_diagnostic and(a.command!='run'or a.mission!='S073'):p.error('--s073-current-source-diagnostic is valid only for an S073 run')
 if a.s073_historical_input_diagnostic and(a.command!='run' or a.mission!='S073' or not a.s073_current_source_diagnostic or (not a.s073_source_host_diagnostic_admission and a.max_ticks is None)):p.error('--s073-historical-input-diagnostic requires current-source S073 and either source-host admission or an explicit bounded preparing diagnostic')
 if a.s002_historical_input_diagnostic and(a.command!='run'or a.mission!='S002' or a.max_ticks is None):p.error('--s002-historical-input-diagnostic requires an S002 run with explicit bounded --max-ticks')
 if bool(a.s002_current_source_diagnostic_package)!=bool(a.s002_current_source_diagnostic_package_sha256):p.error('S002 diagnostic package path and SHA256 must be supplied together')
 if a.s002_current_source_diagnostic_package and(a.command!='run'or a.mission!='S002'or not a.s002_historical_input_diagnostic or a.max_ticks is None or a.s002_derived_seed):p.error('Current-source package is limited to bounded S002 historical-input diagnostic runs and cannot combine with derived-seed mode')
 if a.s073_source_host_diagnostic_admission and(a.command!='run'or a.mission!='S073'):p.error('--s073-source-host-diagnostic-admission is valid only for an S073 run')
 if a.s073_ui_shell_state_owner_diagnostic and(a.command!='run'or a.mission!='S073'):p.error('--s073-ui-shell-state-owner-diagnostic is valid only for an S073 run')
 if a.s073_source_transport_diagnostic and(a.command!='run'or a.mission!='S073' or not a.s073_current_source_diagnostic or not a.s073_source_host_diagnostic_admission):p.error('--s073-source-transport-diagnostic requires an S073 run with both existing diagnostic opt-ins')
 if os.environ.get('WARLINE_S073_NO_PLAYER_POLICY_DIAGNOSTIC')=='1' and (a.command!='run' or a.mission!='S073' or not a.s073_source_host_diagnostic_admission or a.max_ticks is None or os.environ.get('WARLINE_S073_SYNTHETIC_PLAYER_RECRUIT_DIAGNOSTIC')):p.error('No-player-policy diagnostic requires bounded S073 source-host admission and no explicit recruit command')
 try:
  if a.command=='smoke':
   result=owned.require('skirmish_source_smoke').smoke(a.repo,package,manifest,a.output,a.mission,a.seed,a.data_bundle,a.authoring_input,a.build_receipt,a.objective_build,a.data_repo,a.timeout,configuration_qualification=a.configuration_qualification)
   print(json.dumps({k:result[k]for k in ['status','exit_code','mission','seed','wall_seconds','validation_passed','mission_ready','full_outcome_observed']}));return result['exit_code']
  if a.command=='author':
   if not a.authoring_input:p.error('author requires --authoring-input')
   result=owned.require('skirmish_source_authoring').author(a.repo,package,manifest,a.output,a.mission,a.seed,a.authoring_input,a.build_receipt,a.data_repo,a.timeout,owner_projection=a.owner_projection);print(json.dumps({k:result[k]for k in ['status','authoring_mission','authoring_seed','process_exit','world_allocated','validation_passed','source_fit_verified']}));return 0 if result['status']in{'DraftInspected','ConfigurationCompiled'}else 1
  require_profile=owned.require("skirmish_source_profiles").require_profile
  require_profile(a.mission,a.seed)
  if a.command=='resolve':
   result=resolve_manifest(a.repo,package,manifest,a.output,a.mission,a.seed);print(json.dumps({k:result[k]for k in ['status','native_direct_count','fresh_extracted_count','adapter_count','external_temp_source_dependencies']}));
   if result['unresolved']:print('Unsupported source recipes: '+', '.join(x['id']for x in result['unresolved'][:8])+f" …({len(result['unresolved'])} total)",file=sys.stderr);return 2
  elif a.command=='build':
   from skirmish_source_build import build
   result=build(a.repo,package,manifest,a.output,a.mission,a.seed);print(json.dumps({k:result[k]for k in ['status','source_count','compile_wall_seconds','numeric_runtime']}))
  else:
   from skirmish_source_build import run,build,pin,s049_projection17_diagnostic_identity,S049_DIAGNOSTIC_PREFLIGHT_KIND,s073_natural_terminal_diagnostic_completed
   if not a.data_bundle:p.error('run requires --data-bundle')
   if owned.require('skirmish_source_profiles').runtime_backend(a.mission)=='LegacyS073OwnerV1':
    if a.prepare_only:p.error('S073 prepare-only is unsupported; no build or worker will be launched')
    root=Path(a.output).absolute()
    if not a.build_receipt:
     if root.exists():raise UnsupportedSourceShape('Fresh isolated one-command output required')
     root.mkdir(parents=True);build(a.repo,package,manifest,root/'build',a.mission,a.seed);a.build_receipt=str(root/'build/build.json');run_output=str(root/'run')
    else:run_output=str(root)
    result=run(a.repo,a.build_receipt,a.data_bundle,run_output,a.max_ticks,a.timeout,a.data_repo,a.prepare_only,s073_source_diagnostic=a.s073_current_source_diagnostic,s073_source_host_diagnostic_admission=a.s073_source_host_diagnostic_admission,s073_source_transport_diagnostic=a.s073_source_transport_diagnostic,s073_ui_shell_state_owner_diagnostic=a.s073_ui_shell_state_owner_diagnostic,s073_historical_input_diagnostic=a.s073_historical_input_diagnostic)
    print(json.dumps({k:result[k]for k in ['status','process_exit','timed_out','wall_seconds','preparing_session_observed','source_host_diagnostic_observed','full_outcome_observed','diagnostic_prefix_completed','terminal_phase_observed','terminal_outcome_observed','validation_passed','mission_ready','map_blocker_parity_status','map_blocker_parity_verified','ai_startup_mode','blocking_gate']}))
    if s073_natural_terminal_diagnostic_completed(result) and not a.s073_historical_input_diagnostic:return 0
    return 2 if result['preparing_session_observed'] or result['source_host_diagnostic_observed'] else 1
   run_output=a.output
   if not a.build_receipt and(a.max_ticks is not None or a.prepare_only):
    root=Path(a.output).absolute()
    if root.exists():raise UnsupportedSourceShape('Fresh isolated one-command output required')
    root.mkdir(parents=True);build(a.repo,package,manifest,root/'build',a.mission,a.seed);a.build_receipt=str(root/'build/build.json');run_output=str(root/'run')
   preflight=None;preflight_path=None;diagnostic_preflight=None;began=time.monotonic()
   if a.max_ticks is None and not a.prepare_only:
    if not a.build_receipt:
     root=Path(a.output).absolute()
     if root.exists():raise UnsupportedSourceShape('Fresh isolated one-command output required')
     run_output=str(root/'run');preflight_path=root/'smoke'
    else:preflight_path=Path(run_output).with_name(Path(run_output).name+'-smoke')
    preflight=owned.require('skirmish_source_smoke').smoke(a.repo,package,manifest,preflight_path,a.mission,a.seed,a.data_bundle,None,a.build_receipt,a.objective_build,a.data_repo,a.timeout,startup=False,configuration_qualification=a.configuration_qualification)
    print(json.dumps({'smoke_preflight':preflight['status'],'report':str(preflight_path/'smoke-report.json'),'validation_passed':False}))
    if preflight['status']!='PreflightPassed':return preflight['exit_code']
    a.build_receipt=preflight['build_receipt']
    if a.mission=='S049':
     stages={row['id']:row for row in preflight.get('stages',[])};native_stage=stages.get('native_data',{});build_stage=stages.get('source_build',{});bundle_stage=stages.get('input_integrity',{}).get('evidence',{}).get('data_bundle',{})
     bundle_value=json.loads(Path(a.data_bundle).read_bytes());initial_value=json.loads(Path(bundle_value['initialization']['path']).read_bytes());identity=s049_projection17_diagnostic_identity(initial_value,bundle_value,pin(a.data_bundle))
     if identity is not None and native_stage.get('status')=='Passed' and build_stage.get('status')=='Passed':
      evidence_path=preflight_path/'s049-native-diagnostic-preflight-evidence.json'
      if evidence_path.exists():raise UnsupportedSourceShape('Fresh S049 diagnostic evidence artifact required')
      evidence={'schema_version':1,'kind':S049_DIAGNOSTIC_PREFLIGHT_KIND,'mission':'S049','seed':104734,'identity':identity,'data_bundle':bundle_stage,'native_data':native_stage.get('evidence'),'build_receipt':build_stage.get('evidence',{}).get('build_receipt')}
      evidence_path.write_text(json.dumps(evidence,indent=2)+'\n');diagnostic_preflight=pin(evidence_path)
   remaining=a.timeout-(time.monotonic()-began)
   if remaining<=0:raise UnsupportedSourceShape('Smoke/fullrun wall budget exhausted before worker')
   result=run(a.repo,a.build_receipt,a.data_bundle,run_output,a.max_ticks,remaining,a.data_repo,a.prepare_only,diagnostic_preflight,s002_historical_input_only=a.s002_historical_input_diagnostic,s002_current_source_input_package={'path':a.s002_current_source_diagnostic_package,'sha256':a.s002_current_source_diagnostic_package_sha256} if a.s002_current_source_diagnostic_package else None)
   if a.prepare_only:print(json.dumps(result));return 0
   if preflight is not None:
    from skirmish_source_build import write,pin
    marker='[Actual'+a.mission+'AutonomousStart]'
    worker_log=Path(run_output)/'process.log';startup_seen=worker_log.is_file()and marker in worker_log.read_text(errors='replace')
    preflight['full_worker_startup']={'status':'ObservedInFullRun'if startup_seen else'Failed','field':'startup','source_process_exit':result['process_exit'],'source_status':result['status'],'extra_world_initialized':False}
    if not startup_seen:preflight['status']='Failed';preflight['exit_code']=1
    write(preflight_path/'smoke-report.json',preflight);result['smoke_preflight']={'report':pin(preflight_path/'smoke-report.json'),'status':preflight['status'],'startup_deferred_to_actual_full_worker':True};write(Path(run_output)/'run-receipt.json',result)
    if preflight['status']=='Failed':return 1
   print(json.dumps({k:result[k]for k in ['status','process_exit','timed_out','wall_seconds','validation_passed','full_outcome_observed','diagnostic_prefix_completed']}));return 0 if result['validation_passed']or result['diagnostic_prefix_completed']else 1
  return 0
 except (UnsupportedSourceShape,ValueError,KeyError,OSError)as error:
  print('Source simulation admission failed: '+str(error),file=sys.stderr);return 2
if __name__=='__main__':raise SystemExit(main())
