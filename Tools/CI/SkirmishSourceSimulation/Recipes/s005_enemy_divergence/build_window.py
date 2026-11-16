#!/usr/bin/env python3
"""Compile a bounded same-binary S005 enemy-phase observation fixture."""
import hashlib
import json
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[5]
LOGS = ROOT / 'T Logs/SkirmishProduction'
BASE = LOGS / 's005-observation-scope-build-20261106-T1'
OUT = LOGS / 's005-enemy-window-build-20261106-T1'
TRACE = Path(__file__).with_name('EnemyPhaseTrace.cs')

def sha(path):
    h = hashlib.sha256()
    with Path(path).open('rb') as f:
        for part in iter(lambda: f.read(1024 * 1024), b''): h.update(part)
    return h.hexdigest()

def main():
    OUT.mkdir(exist_ok=False)
    old = json.loads((BASE / 'receipt.json').read_bytes())
    if old.get('status') != 'PassedCompileProbe' or old.get('runtime_admission') is not False:
        raise ValueError('Exact unqualified source build required')
    pins = dict(old['input_sha256'])
    for name, expected in pins.items():
        if not Path(name).is_file() or sha(name) != expected: raise ValueError('Pinned source changed: ' + name)
    options = [json.loads(x) for x in (BASE / 'compile.rsp').read_text().splitlines() if x.strip()]
    steps = [x for x in options if x.endswith('/S004AirFuelSafetyOwnerStep.cs')]
    helper = str(BASE / 'S005CurrentAuditedFactoryHelper.cs')
    if len(steps) != 1 or options.count(helper) != 1: raise ValueError('Exact source scheduler and fixture helper required')
    original = Path(steps[0]).read_text(); seam = 'Phase("enemy",()=>{enemyState.Begin();if(enemyState.Enabled&&enemyState.RequiredQueriesPresent)enemy.OnUpdate(ref enemyState);enemyState.End();});'
    if original.count(seam) != 1: raise ValueError('Exact original enemy scheduling seam required')
    replacement = 'Phase("enemy",()=>{S005EnemyPhaseTrace.Capture(Entities,Tick,"before");enemyState.Begin();if(enemyState.Enabled&&enemyState.RequiredQueriesPresent)enemy.OnUpdate(ref enemyState);enemyState.End();S005EnemyPhaseTrace.Capture(Entities,Tick,"after");});'
    scheduler = OUT / 'S005EnemyWindowOwnerStep.cs'; scheduler.write_text(original.replace(seam, replacement, 1))
    text = Path(helper).read_text()
    if text.count('step<2400') != 1: raise ValueError('Exact fixed tick bound required')
    bounded = OUT / 'S005EnemyWindowHelper.cs'; bounded.write_text(text.replace('step<2400', 'step<1200', 1))
    options = [str(scheduler) if x == steps[0] else str(bounded) if x == helper else '-out:' + str(OUT / 'source-force-ledger.exe') if x.startswith('-out:') else x for x in options]
    options.append(str(TRACE))
    for p in (Path(__file__), TRACE, BASE / 'receipt.json', BASE / 'compile.rsp', Path(steps[0]), Path(helper), scheduler, bounded): pins[str(p)] = sha(p)
    rsp = OUT / 'compile.rsp'; rsp.write_text(''.join(json.dumps(x) + '\n' for x in options))
    compiler = Path('/usr/local/share/dotnet/sdk/10.0.201/Roslyn/bincore/csc.dll'); pins[str(compiler)] = sha(compiler)
    result = subprocess.run(['dotnet', str(compiler), '@' + str(rsp)], capture_output=True, text=True, timeout=300)
    (OUT / 'compile.log').write_text(result.stdout + result.stderr)
    drift = [name for name, expected in pins.items() if not Path(name).is_file() or sha(name) != expected]
    receipt = dict(old); receipt.update({'status': 'PassedCompileProbe' if result.returncode == 0 and not drift else 'FailedCompileProbe',
        'compile_exit': result.returncode, 'input_sha256': pins, 'compile_log_sha256': sha(OUT / 'compile.log'),
        'scope': '1200 fixed ticks, opt-in read-only source enemy-phase window; no native parity or readiness',
        'source_window_ticks': [1080,1180], 'runtime_admission': False, 'mission_ready': False, 'changed_during_compile': drift})
    (OUT / 'receipt.json').write_text(json.dumps(receipt, indent=2) + '\n')
    print(json.dumps({'status': receipt['status'], 'compile_exit': result.returncode, 'changed_during_compile': len(drift)}))
    if receipt['status'] != 'PassedCompileProbe': raise SystemExit(1)

if __name__ == '__main__': main()
