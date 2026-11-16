#!/usr/bin/env python3
"""Run one pinned S005 diagnostic binary with scoped observation off and on.

This is a diagnostic comparison, not a gameplay, native parity or speed claim.
The retained runner checks its original source/capture pins before and after.
"""
import hashlib
import json
import os
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
LOGS = ROOT / 'T Logs/SkirmishProduction'
BUILD = LOGS / 's005-observation-scope-build-20261106-T1'
SOURCE_RUNNER = BUILD / 'run-policy.py'
PAIR = LOGS / 's005-controlled-observation-pair-20261106-T1'
OLD_OUTPUT = 's005-observation-scope-run-20261106-T1'

def sha(path):
    h = hashlib.sha256()
    with Path(path).open('rb') as f:
        for data in iter(lambda: f.read(1024 * 1024), b''): h.update(data)
    return h.hexdigest()

def main():
    PAIR.mkdir(exist_ok=False)
    runner = SOURCE_RUNNER.read_text()
    if runner.count(OLD_OUTPUT) != 1: raise ValueError('Exact output replacement required')
    build = json.loads((BUILD / 'receipt.json').read_bytes())
    if build.get('status') != 'PassedCompileProbe' or build.get('runtime_admission') is not False:
        raise ValueError('Pinned unqualified diagnostic build required')
    binary = BUILD / 'source-force-ledger.exe'
    # Remove unrelated experimental flags from both modes. Preserve the same
    # remaining parent environment without writing its potentially secret values.
    env = {k: v for k, v in os.environ.items() if not k.startswith('WARLINE_')}
    env['WARLINE_S005_SETUP_RECEIPT_CACHE'] = '0'
    common_env_hash = hashlib.sha256(json.dumps(env, sort_keys=True, separators=(',', ':')).encode()).hexdigest()
    records = []
    for mode in ('off', 'on'):
        target_name = 's005-controlled-observation-' + mode + '-20261106-T1'
        target = LOGS / target_name
        if target.exists(): raise FileExistsError(target)
        script = PAIR / (mode + '-runner.py')
        script.write_text(runner.replace(OLD_OUTPUT, target_name, 1))
        child_env = dict(env); child_env['WARLINE_S005_OBSERVATION_SCOPE'] = '1' if mode == 'on' else '0'
        with (PAIR / (mode + '-process.log')).open('w') as log:
            result = subprocess.run([sys.executable, '-B', str(script)], cwd=ROOT, env=child_env, stdout=log, stderr=subprocess.STDOUT, timeout=1000)
        record = {'mode': mode, 'observation_scope': child_env['WARLINE_S005_OBSERVATION_SCOPE'],
                  'setup_receipt_cache': child_env['WARLINE_S005_SETUP_RECEIPT_CACHE'],
                  'exit_code': result.returncode, 'output': str(target), 'runner_sha256': sha(script),
                  'process_log_sha256': sha(PAIR / (mode + '-process.log'))}
        if (target / 'run-receipt.json').is_file(): record['run_receipt_sha256'] = sha(target / 'run-receipt.json')
        records.append(record)
        (PAIR / 'progress.json').write_text(json.dumps({'records': records, 'mission_ready': False}, indent=2) + '\n')
        if result.returncode: break
    report = {'status': 'CompletedBoth' if len(records) == 2 and all(x['exit_code'] == 0 for x in records) else 'FailedOrIncomplete',
              'binary': str(binary), 'binary_sha256': sha(binary), 'source_runner_sha256': sha(SOURCE_RUNNER),
              'common_environment_sha256': common_env_hash, 'seed_source': 'retained pinned session.cs Seed=1',
              'modes': records, 'scope': 'Same-binary source diagnostic; historical geometry and assumed empty water; no native input or readiness',
              'mission_ready': False}
    (PAIR / 'receipt.json').write_text(json.dumps(report, indent=2) + '\n')
    print(json.dumps(report), flush=True)
    return 0 if report['status'] == 'CompletedBoth' else 1

if __name__ == '__main__': raise SystemExit(main())
