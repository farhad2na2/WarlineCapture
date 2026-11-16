#!/usr/bin/env python3
"""Run the bounded source enemy observation binary with scope off and on."""
import argparse
import hashlib
import json
import os
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[5]
LOGS = ROOT / 'T Logs/SkirmishProduction'
BASE = LOGS / 's005-observation-scope-build-20261106-T1'
BUILD = LOGS / 's005-enemy-window-build-20261106-T1'
PAIR = LOGS / 's005-enemy-window-pair-20261106-T1'

def sha(path):
    h = hashlib.sha256()
    with Path(path).open('rb') as f:
        for part in iter(lambda: f.read(1024 * 1024), b''): h.update(part)
    return h.hexdigest()

def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--repeat-off', action='store_true', help='Run two scope-off controls under one recorded environment')
    parser.add_argument('--early-path', action='store_true', help='Use the separately built early actor and original budget observation fixture')
    args=parser.parse_args()
    pair = LOGS / 's005-early-path-window-off-repeat-pair-20261106-T1' if args.early_path and args.repeat_off else LOGS / 's005-early-path-window-pair-20261106-T1' if args.early_path else LOGS / 's005-enemy-window-off-repeat-pair-20261106-T1' if args.repeat_off else PAIR
    pair.mkdir(exist_ok=False)
    build_dir = LOGS / 's005-early-path-window-build-20261106-T2' if args.early_path else BUILD
    build = json.loads((build_dir / 'receipt.json').read_bytes())
    if build.get('status') != 'PassedCompileProbe' or build.get('mission_ready') is not False:
        raise ValueError('Passed diagnostic source build required')
    source = (BASE / 'run-policy.py').read_text()
    old_build, old_output = BASE.name, 's005-observation-scope-run-20261106-T1'
    if source.count(old_build) != 1 or source.count(old_output) != 1: raise ValueError('Exact retained runner seams required')
    env = {k: v for k, v in os.environ.items() if not k.startswith('WARLINE_')}
    env['WARLINE_S005_SETUP_RECEIPT_CACHE'] = '0'
    common_env = hashlib.sha256(json.dumps(env, sort_keys=True, separators=(',', ':')).encode()).hexdigest()
    binary = build_dir / 'source-force-ledger.exe'; binary_hash = sha(binary); records = []
    for mode in (('off_a', 'off_b') if args.repeat_off else ('off', 'on')):
        target_name = ('s005-early-path-window-' if args.early_path else 's005-enemy-window-') + mode + '-20261106-T1'; target = LOGS / target_name
        if target.exists(): raise FileExistsError(target)
        runner = pair / (mode + '-runner.py')
        runner.write_text(source.replace(old_build, build_dir.name, 1).replace(old_output, target_name, 1))
        child_env = dict(env); child_env['WARLINE_S005_OBSERVATION_SCOPE'] = '1' if mode == 'on' else '0'
        log = pair / (mode + '-process.log')
        with log.open('w') as stream:
            result = subprocess.run([sys.executable, '-B', str(runner)], cwd=ROOT, env=child_env, stdout=stream, stderr=subprocess.STDOUT, timeout=1000)
        record = {'mode': mode, 'observation_scope': child_env['WARLINE_S005_OBSERVATION_SCOPE'],
                  'setup_receipt_cache': '0', 'exit_code': result.returncode, 'output': str(target),
                  'runner_sha256': sha(runner), 'process_log_sha256': sha(log)}
        for name in ('run-receipt.json', 'enemy-phase-window.jsonl', 'path-budget-completions.jsonl', 'path-budget-window.json'):
            p = target / name
            if p.is_file(): record[name + '_sha256'] = sha(p)
        records.append(record); (pair / 'progress.json').write_text(json.dumps({'modes':records,'mission_ready':False},indent=2)+'\n')
        if result.returncode: break
    if sha(binary) != binary_hash: raise ValueError('Diagnostic binary changed during pair')
    report = {'status': 'CompletedBoth' if len(records) == 2 and all(x['exit_code'] == 0 for x in records) else 'FailedOrIncomplete',
              'binary': str(binary), 'binary_sha256': binary_hash, 'common_environment_sha256': common_env,
              'source_runner_sha256': sha(BASE / 'run-policy.py'), 'seed_source': 'Pinned session.cs Seed=1',
              'modes': records, 'scope': '1200 ordinary fixed ticks, source-only, read-only actor and optional budget trace may affect timing; historical map masks and assumed-empty water',
              'mission_ready': False}
    (pair / 'receipt.json').write_text(json.dumps(report,indent=2)+'\n'); print(json.dumps(report),flush=True)
    return 0 if report['status']=='CompletedBoth' else 1

if __name__ == '__main__': raise SystemExit(main())
