#!/usr/bin/env python3
"""One source smoke/build/run entry. SmokePassed covers declared fast checks only; it never grants mission readiness. Author inspects S026 drafts without World allocation."""
from __future__ import annotations
import argparse,json,sys,time
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

def main():
 p=argparse.ArgumentParser(description=__doc__);p.add_argument('command',choices=['resolve','build','run','author','smoke']);p.add_argument('--repo',default=str(Path(__file__).resolve().parents[2]));p.add_argument('--package-root',default=str(Path(__file__).resolve().parents[2]));p.add_argument('--manifest');p.add_argument('--output',required=True);p.add_argument('--mission',required=True);p.add_argument('--seed',type=int,default=104731);p.add_argument('--build-receipt');p.add_argument('--objective-build',help='Existing checked .NET8 objective build directory for smoke reuse');p.add_argument('--data-repo');p.add_argument('--data-bundle');p.add_argument('--authoring-input',help='Hash-bound typed config diagnostics; ConfigurationCompiled/DraftInspected do not certify source-fit or mission readiness');p.add_argument('--max-ticks',type=int);p.add_argument('--timeout',type=float);a=p.parse_args();a.timeout=a.timeout if a.timeout is not None else(120 if a.command in {'smoke','author'}else 1800)
 package=Path(a.package_root).resolve();manifest=Path(a.manifest)if a.manifest else package/'Tools/CI/SkirmishSourceSimulation/source_manifest.plan.json'
 try:
  if a.command=='smoke':
   result=owned.require('skirmish_source_smoke').smoke(a.repo,package,manifest,a.output,a.mission,a.seed,a.data_bundle,a.authoring_input,a.build_receipt,a.objective_build,a.data_repo,a.timeout)
   print(json.dumps({k:result[k]for k in ['status','exit_code','mission','seed','wall_seconds','validation_passed','mission_ready','full_outcome_observed']}));return result['exit_code']
  if a.command=='author':
   if not a.authoring_input:p.error('author requires --authoring-input')
   result=owned.require('skirmish_source_authoring').author(a.repo,package,manifest,a.output,a.mission,a.seed,a.authoring_input,a.build_receipt,a.data_repo,a.timeout);print(json.dumps({k:result[k]for k in ['status','authoring_mission','authoring_seed','process_exit','world_allocated','validation_passed','source_fit_verified']}));return 0 if result['status']in{'DraftInspected','ConfigurationCompiled'}else 1
  require_profile=owned.require("skirmish_source_profiles").require_profile
  require_profile(a.mission,a.seed)
  if a.command=='resolve':
   result=resolve_manifest(a.repo,package,manifest,a.output,a.mission,a.seed);print(json.dumps({k:result[k]for k in ['status','native_direct_count','fresh_extracted_count','adapter_count','external_temp_source_dependencies']}));
   if result['unresolved']:print('Unsupported source recipes: '+', '.join(x['id']for x in result['unresolved'][:8])+f" …({len(result['unresolved'])} total)",file=sys.stderr);return 2
  elif a.command=='build':
   from skirmish_source_build import build
   result=build(a.repo,package,manifest,a.output,a.mission,a.seed);print(json.dumps({k:result[k]for k in ['status','source_count','compile_wall_seconds','numeric_runtime']}))
  else:
   from skirmish_source_build import run,build
   if not a.data_bundle:p.error('run requires --data-bundle')
   run_output=a.output
   if not a.build_receipt and a.max_ticks is not None:
    root=Path(a.output).absolute()
    if root.exists():raise UnsupportedSourceShape('Fresh isolated one-command output required')
    root.mkdir(parents=True);build(a.repo,package,manifest,root/'build',a.mission,a.seed);a.build_receipt=str(root/'build/build.json');run_output=str(root/'run')
   preflight=None;preflight_path=None;began=time.monotonic()
   if a.max_ticks is None:
    if not a.build_receipt:
     root=Path(a.output).absolute()
     if root.exists():raise UnsupportedSourceShape('Fresh isolated one-command output required')
     run_output=str(root/'run');preflight_path=root/'smoke'
    else:preflight_path=Path(run_output).with_name(Path(run_output).name+'-smoke')
    preflight=owned.require('skirmish_source_smoke').smoke(a.repo,package,manifest,preflight_path,a.mission,a.seed,a.data_bundle,None,a.build_receipt,a.objective_build,a.data_repo,a.timeout,startup=False)
    print(json.dumps({'smoke_preflight':preflight['status'],'report':str(preflight_path/'smoke-report.json'),'validation_passed':False}))
    if preflight['status']!='PreflightPassed':return preflight['exit_code']
    a.build_receipt=preflight['build_receipt']
   remaining=a.timeout-(time.monotonic()-began)
   if remaining<=0:raise UnsupportedSourceShape('Smoke/fullrun wall budget exhausted before worker')
   result=run(a.repo,a.build_receipt,a.data_bundle,run_output,a.max_ticks,remaining,a.data_repo)
   if preflight is not None:
    from skirmish_source_build import write,pin
    marker='[ActualS002AutonomousStart]'
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
