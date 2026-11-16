#!/usr/bin/env python3
"""Exercise the current authored S004 loader with its pinned layout replay."""
import hashlib
import json
import os
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
BASE = ROOT / 'T Logs/SkirmishProduction/s006-logical-binding-probe-build-20261106-T3'
BUILD = ROOT / 'T Logs/SkirmishProduction/s003-current-prefab-import-build-20261104-T1/build.json'
GRAPH = ROOT / 'T Logs/SkirmishProduction/s004-current-physical-graph-extension-20261106-T1/source-config-graph.json'
LAYOUT = ROOT / 'T Logs/SkirmishProduction/s004-current-layout-replay-20261104-T1/replayed-layout.json'
MATRIX = ROOT / 'Assets/Game/Resources/SkirmishExpansion/InitialSetupMatrix.csv'
CONTROL = ROOT / 'Tools/CI/SkirmishSourceSimulation/Tests/SourceExpandedAuthoredSetupControl.cs'
OUT = ROOT / 'T Logs/SkirmishProduction/s004-expanded-four-difficulty-replay-20261106-T4'

def sha(p):
    h = hashlib.sha256()
    with p.open('rb') as f:
        for block in iter(lambda: f.read(1024 * 1024), b''): h.update(block)
    return h.hexdigest()

def main():
    OUT.mkdir(parents=True, exist_ok=False)
    args = [json.loads(x) for x in (BASE / 'compile.rsp').read_text().splitlines() if x.strip()]
    old = str(ROOT / 'T Logs/SkirmishProduction/s006-logical-binding-proposal-probe-20261105-T1/S006LogicalProbeControl.cs')
    if args.count(old) != 1: raise ValueError('Exact prior fixture source required')
    args = [str(CONTROL) if x == old else '-main:SourceExpandedAuthoredSetupControl' if x.startswith('-main:') else '-out:' + str(OUT / 'control.exe') if x.startswith('-out:') else x for x in args]
    loader = str(ROOT / 'Tools/CI/SkirmishSourceSimulation/Adapters/SourceExpandedAuthoredSetupInput.cs')
    if loader not in args: args.append(loader)
    observed = str(ROOT / 'T Logs/SkirmishProduction/s006-current-source-candidate-20261105-T1/ObservedSourceConfigJson.cs')
    if args.count(observed) != 1: raise ValueError('Exact obsolete JSON shim required')
    args = [str(ROOT / 'Tools/CI/SkirmishSourceSimulation/Adapters/050_SourceConfigJson.cs') if x == observed else x for x in args]
    args.append(str(ROOT / 'Tools/CI/SkirmishSourceSimulation/Adapters/SourceValidatedLayoutJson.cs'))
    build = json.loads(BUILD.read_bytes()); runtime = build['runtime_selection']
    csc = next(Path(p) for p in build['source_and_reference_pins'] if p.endswith('/Roslyn/bincore/csc.dll'))
    pins = {}
    for x in args:
        name = x[3:] if x.startswith('-r:') else x
        if x.startswith('-r:') and '=' in name: name = name.split('=', 1)[1]
        if name.endswith(('.cs', '.dll')) and Path(name).is_file(): pins[name] = sha(Path(name))
    for p in (Path(__file__), BASE / 'compile.rsp', BUILD, GRAPH, LAYOUT, MATRIX, csc, Path(runtime['mono'])): pins[str(p)] = sha(p)
    (OUT / 'compile.rsp').write_text(''.join(json.dumps(x) + '\n' for x in args))
    compiled = subprocess.run(['dotnet', str(csc), '@' + str(OUT / 'compile.rsp')], capture_output=True, text=True, timeout=300)
    (OUT / 'compile.log').write_text(compiled.stdout + compiled.stderr)
    if compiled.returncode: raise RuntimeError('Compilation failed; see compile.log')
    env = os.environ.copy(); env['MONO_PATH'] = os.pathsep.join((str(BUILD.parent / 'placement'), runtime['engine'], str(Path(runtime['newtonsoft']).parent), str(ROOT / 'Library/ScriptAssemblies'), str(OUT)))
    command = [runtime['mono'], str(OUT / 'control.exe'), str(GRAPH), sha(GRAPH), str(MATRIX), sha(MATRIX), str(ROOT), 'S004', '104733', str(LAYOUT), sha(LAYOUT)]
    run = subprocess.run(command, cwd=ROOT, env=env, capture_output=True, text=True, timeout=300)
    (OUT / 'run.log').write_text(run.stdout + run.stderr)
    drift = [p for p, digest in pins.items() if not Path(p).is_file() or sha(Path(p)) != digest]
    markers = [x for x in run.stdout.splitlines() if x.startswith('[SourceExpandedAuthoredSetupControl]')]
    result = {'status': 'Passed' if run.returncode == 0 and len(markers) == 12 and all('result=Passed' in m for m in markers) and not drift else 'Failed', 'compile_exit': compiled.returncode, 'run_exit': run.returncode, 'markers': markers, 'source_pins': pins, 'source_drift': drift, 'scope': 'Current-source S004 authored compiler and typed replay, source-only; no world or readiness', 'mission_ready': False}
    (OUT / 'receipt.json').write_text(json.dumps(result, indent=2) + '\n')
    print(json.dumps({k: v for k, v in result.items() if k != 'source_pins'}))
    if result['status'] != 'Passed': raise SystemExit(1)

if __name__ == '__main__': main()
