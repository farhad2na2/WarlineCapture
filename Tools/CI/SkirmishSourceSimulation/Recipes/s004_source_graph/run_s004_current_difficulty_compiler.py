#!/usr/bin/env python3
"""Compile S004 difficulty readbacks with pinned source, references and runtime."""
import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess

ROOT = Path(__file__).resolve().parents[5]
OUT = ROOT / 'T Logs/SkirmishProduction/s004-current-standard-difficulty-compiler-20261105-T3'
PRIOR = ROOT / 'T Logs/SkirmishProduction/s004-difficulty-setup-comparison-20261105-T1'
BUILD = ROOT / 'T Logs/SkirmishProduction/s002-four-difficulty-reader-build-20261105-T1/build.json'
GRAPH = ROOT / 'T Logs/SkirmishProduction/s004-catalog-closure-refresh-20261104-T1/source-config-graph.json'
LAYOUT = ROOT / 'T Logs/SkirmishProduction/s004-current-layout-replay-20261104-T1/replayed-layout.json'
MATRIX = ROOT / 'Assets/Game/Resources/SkirmishExpansion/InitialSetupMatrix.csv'
CSC = Path('/usr/local/share/dotnet/sdk/10.0.201/Roslyn/bincore/csc.dll')

def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest()

def checked(path):
    path = Path(path).resolve(strict=True)
    if not path.is_file(): raise ValueError('Regular input required: ' + str(path))
    return path

def main():
    OUT.mkdir(exist_ok=False)
    build = json.loads(BUILD.read_bytes())
    if build.get('status') != 'Passed' or build.get('compile_exit') != 0:
        raise ValueError('Fresh passing source build required')
    pins = {}
    for name, expected in build['source_and_reference_pins'].items():
        path = checked(name)
        if sha(path) != expected: raise ValueError('Source build pin changed: ' + name)
        pins[str(path)] = expected
    graph = json.loads(GRAPH.read_bytes())
    for name, expected in graph['source_pins'].items():
        path = Path(name); path = checked(path if path.is_absolute() else ROOT / path)
        if sha(path) != expected: raise ValueError('Authored graph pin changed: ' + name)
        pins[str(path)] = expected
    args = [json.loads(line) for line in (PRIOR / 'compile.rsp').read_text().splitlines() if line.strip()]
    outputs = [i for i, arg in enumerate(args) if arg.startswith('-out:')]
    if len(outputs) != 1 or args[outputs[0]] != '-out:' + str(PRIOR / 'fixture.exe'):
        raise ValueError('Exact captured compiler response required')
    exe = OUT / 'fixture.exe'; args[outputs[0]] = '-out:' + str(exe)
    refs = []
    for arg in args:
        if arg.startswith('-r:'):
            raw = arg[3:].split('=', 1)[-1]
            path = checked(raw); refs.append(path.parent); pins[str(path)] = sha(path)
        elif arg.endswith('.cs') and not arg.startswith('-'):
            path = checked(arg); pins[str(path)] = sha(path)
    dotnet = checked(shutil.which('dotnet')); csc = checked(CSC); mono = checked(build['runtime_selection']['mono'])
    for path in (dotnet, csc, mono, BUILD, GRAPH, LAYOUT, MATRIX, Path(__file__).resolve(), PRIOR / 'compile.rsp'):
        path = checked(path); pins[str(path)] = sha(path)
    rsp = OUT / 'compile.rsp'; rsp.write_text('\n'.join(json.dumps(x) for x in args) + '\n'); pins[str(rsp)] = sha(rsp)
    compile_cmd = [str(dotnet), str(csc), '@' + str(rsp)]
    compiled = subprocess.run(compile_cmd, cwd=ROOT, capture_output=True, text=True)
    (OUT / 'compile.log').write_text(compiled.stdout + compiled.stderr)
    if compiled.returncode != 0: raise RuntimeError('Compiler failed; see compile.log')
    env = os.environ.copy(); env['MONO_PATH'] = os.pathsep.join(map(str, dict.fromkeys(refs)))
    result_path = OUT / 'result.json'
    run_cmd = [str(mono), '--optimize=-float32', str(exe), str(GRAPH), str(MATRIX), sha(MATRIX), str(LAYOUT), str(result_path)]
    run = subprocess.run(run_cmd, cwd=ROOT, env=env, capture_output=True, text=True)
    (OUT / 'run.log').write_text(run.stdout + run.stderr)
    if run.returncode != 0 or '[SourceS004DifficultyCompilerProbe] result=Collected tuples=12' not in run.stdout:
        raise RuntimeError('Original compiler probe failed; see run.log')
    for name, expected in pins.items():
        if sha(Path(name)) != expected: raise ValueError('Compiler input changed during execution: ' + name)
    result = json.loads(result_path.read_bytes())
    rows = [row for row in result['rows'] if row['size'] == 'Standard']
    if len(rows) != 4 or {x['difficulty'] for x in rows} != {'Recruit','Regular','Veteran','Commander'} or any(
        not row['compiled'] or not row['compiled_setup']['MeasuredLayoutBound'] for row in rows):
        raise ValueError('Four real bound Standard compiler outputs required')
    receipt = {'status': 'PassedOriginalCompilerSourceOnly', 'source_pins': pins,
               'explicit_input_sha256': {}, 'compile_command': compile_cmd, 'runtime_command': run_cmd,
               'mono_path': env['MONO_PATH'], 'executable_sha256': sha(exe),
               'compile_log_sha256': sha(OUT / 'compile.log'), 'run_log_sha256': sha(OUT / 'run.log'),
               'result_sha256': sha(result_path), 'graph_source_pin_count': len(graph['source_pins']),
               'reference_assembly_count': len(refs), 'standard_setup_hashes': {r['difficulty']:r['setup_hash'] for r in rows},
               'runtime_admission': False, 'source_fit_certified': False, 'mission_ready': False}
    with (OUT / 'receipt.json').open('x') as stream: json.dump(receipt, stream, indent=2); stream.write('\n')
    print(json.dumps({'status': receipt['status'], 'pins': len(pins), 'references': len(refs),
                      'result_sha256': receipt['result_sha256'], 'mission_ready': False}))

if __name__ == '__main__': main()
