#!/usr/bin/env python3
"""Resolve/build/run the shared current-source simulation (currently S002/104731)."""
from __future__ import annotations
import argparse,json,sys
from pathlib import Path
sys.dont_write_bytecode=True
from skirmish_source_manifest import resolve_manifest,UnsupportedSourceShape

def main():
 p=argparse.ArgumentParser(description=__doc__);p.add_argument('command',choices=['resolve','build','run']);p.add_argument('--repo',default=str(Path(__file__).resolve().parents[2]));p.add_argument('--package-root',default=str(Path(__file__).resolve().parents[2]));p.add_argument('--manifest');p.add_argument('--output',required=True);p.add_argument('--mission',required=True);p.add_argument('--seed',type=int,default=104731);p.add_argument('--build-receipt');p.add_argument('--data-repo');p.add_argument('--data-bundle');p.add_argument('--max-ticks',type=int);p.add_argument('--timeout',type=float,default=1800);a=p.parse_args()
 package=Path(a.package_root).resolve();manifest=Path(a.manifest)if a.manifest else package/'Tools/CI/SkirmishSourceSimulation/source_manifest.plan.json'
 try:
  if a.mission!='S002'or a.seed!=104731:raise UnsupportedSourceShape('Supported source profile is S002/104731 only')
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
   if not a.build_receipt:
    root=Path(a.output).absolute()
    if root.exists():raise UnsupportedSourceShape('Fresh isolated one-command output required')
    root.mkdir(parents=True);build(a.repo,package,manifest,root/'build',a.mission,a.seed);a.build_receipt=str(root/'build/build.json');run_output=str(root/'run')
   result=run(a.repo,a.build_receipt,a.data_bundle,run_output,a.max_ticks,a.timeout,a.data_repo);print(json.dumps({k:result[k]for k in ['status','process_exit','timed_out','wall_seconds','validation_passed','full_outcome_observed','diagnostic_prefix_completed']}));return 0 if result['validation_passed']or result['diagnostic_prefix_completed']else 1
  return 0
 except (UnsupportedSourceShape,ValueError,KeyError,OSError)as error:
  print('Source simulation admission failed: '+str(error),file=sys.stderr);return 2
if __name__=='__main__':raise SystemExit(main())
