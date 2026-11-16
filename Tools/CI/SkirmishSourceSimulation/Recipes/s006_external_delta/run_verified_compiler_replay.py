#!/usr/bin/env python3
"""Run the S006 external delta through the original source compiler, diagnostically."""
import argparse
import hashlib
import json
import os
import subprocess
from pathlib import Path

import verify_s006_external_delta as delta_verifier

ROOT = delta_verifier.ROOT
RECIPE = Path(__file__).resolve().parent
BASE = ROOT / 'T Logs/SkirmishProduction/s006-reusable-external-delta-control-20261105-T1'
COMPILER = ROOT / 'T Logs/SkirmishProduction/s006-authored-origin-hash-census-20261105-T3'
BUILD = ROOT / 'T Logs/SkirmishProduction/s003-current-prefab-import-build-20261104-T1/build.json'
GRAPH = ROOT / 'T Logs/SkirmishProduction/s005-original-compiler-probe-inputs-20261105-T2/source-config-graph.json'
MATRIX = ROOT / 'Assets/Game/Resources/SkirmishExpansion/InitialSetupMatrix.csv'
MARKER = '[S006SourceCandidate] result=Collected sizes=3 candidateOnly=1 readiness=0'


def sha(path):
    h = hashlib.sha256()
    with Path(path).open('rb') as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b''):
            h.update(block)
    return h.hexdigest()


def pin(pins, path):
    path = Path(path).resolve(strict=True)
    value = sha(path)
    if str(path) in pins and pins[str(path)] != value:
        raise ValueError('Conflicting source pin: ' + str(path))
    pins[str(path)] = value


def recheck(pins):
    for path, expected in pins.items():
        if sha(path) != expected:
            raise ValueError('Compiler or input pin changed: ' + path)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--delta', type=Path, required=True)
    parser.add_argument('--expected-sha256', required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    output = args.output.resolve()
    if not output.is_relative_to((ROOT / 'T Logs/SkirmishProduction').resolve()) or output.exists():
        raise ValueError('Fresh T Logs/SkirmishProduction output required')
    verified = delta_verifier.verify(args.delta, args.expected_sha256)
    compiler_receipt = json.loads((COMPILER / 'receipt.json').read_bytes())
    if compiler_receipt.get('status') != 'Passed' or compiler_receipt.get('mission_ready') is not False:
        raise ValueError('Exact tested source compiler receipt required')
    pins = {}
    for name, expected in compiler_receipt['candidate_source_pins'].items():
        pin(pins, name)
        if pins[str(Path(name).resolve())] != expected:
            raise ValueError('Tested candidate source changed: ' + name)
    generated = COMPILER / 'MergedSkirmishSetupCompiler.cs'
    if sha(generated) != compiler_receipt['generated_compiler_sha256']:
        raise ValueError('Tested generated compiler changed')

    build = json.loads(BUILD.read_bytes())
    runtime = build['runtime_selection']
    arguments = [json.loads(line) for line in (BASE / 'compile.rsp').read_text().splitlines() if line.strip()]
    counts = {'driver': 0, 'compiler': 0, 'materializer': 0, 'output': 0}
    for index, value in enumerate(arguments):
        if value.startswith('-out:'):
            arguments[index] = '-out:' + str(output / 'control.exe'); counts['output'] += 1
        elif value.endswith('/S006ExternalDeltaControl.cs'):
            arguments[index] = str(RECIPE / 'SourceS006ExternalDeltaControl.cs'); counts['driver'] += 1
        elif value.endswith('/MergedSkirmishSetupCompiler.cs'):
            arguments[index] = str(generated); counts['compiler'] += 1
        elif value.endswith('/SourceS006ExternalDeltaMaterializer.cs'):
            arguments[index] = str(RECIPE / 'SourceS006ExternalDeltaMaterializer.cs'); counts['materializer'] += 1
    if any(count != 1 for count in counts.values()):
        raise ValueError('Exact source replay compiler entries required')

    csc = Path('/usr/local/share/dotnet/sdk/10.0.201/Roslyn/bincore/csc.dll')
    for path in (Path(__file__), RECIPE / 'verify_s006_external_delta.py', BASE / 'compile.rsp',
                 COMPILER / 'receipt.json', BUILD, GRAPH, MATRIX, args.delta, csc, Path(runtime['mono'])):
        pin(pins, path)
    for value in arguments:
        if value.startswith('-r:'):
            pin(pins, value[3:].split('=', 1)[-1])
        elif not value.startswith('-'):
            pin(pins, value)
    recheck(pins)
    output.mkdir(parents=True)
    (output / 'compile.rsp').write_text(''.join(json.dumps(v) + '\n' for v in arguments))
    compile_result = subprocess.run(['dotnet', str(csc), '@' + str(output / 'compile.rsp')],
                                    cwd=ROOT, capture_output=True, text=True, timeout=300)
    (output / 'compile.log').write_text(compile_result.stdout + compile_result.stderr)
    if compile_result.returncode:
        raise RuntimeError('Source compiler replay build failed; compile.log preserved')
    env = os.environ.copy()
    env['MONO_PATH'] = os.pathsep.join((str(BUILD.parent / 'placement'), runtime['engine'],
        str(Path(runtime['newtonsoft']).parent), str(ROOT / 'Library/ScriptAssemblies'), str(output)))
    command = [runtime['mono'], str(output / 'control.exe'), str(GRAPH), str(MATRIX),
               str(output / 'result.json'), str(output / 'layout.json'), '1', str(args.delta), args.expected_sha256]
    run = subprocess.run(command, cwd=ROOT, env=env, capture_output=True, text=True, timeout=300)
    (output / 'run.log').write_text(run.stdout + run.stderr)
    if run.returncode or run.stdout.splitlines().count(MARKER) != 1:
        raise RuntimeError('Source compiler replay failed; run.log preserved')
    result = json.loads((output / 'result.json').read_bytes())
    rows = []
    for row in result['rows']:
        data = row['disposable_pad_projection']
        rows.append({'size': row['size'], 'compiled': data['compiled'], 'setup_hash': data['setup_hash'],
                     'expected_setup_hash': data['expected_setup_hash'], 'negative_rejections': data['negative_rejections'],
                     'byte_negative_rejections': data['byte_negative_rejections'],
                     'source_definition_unchanged': data['source_definition_unchanged'],
                     'requires_logical_binding': row['requires_logical_binding'],
                     'logical_binding_present': row['logical_binding_present']})
    if len(rows) != 3 or {r['size'] for r in rows} != {'Standard', 'War', 'LargeWar'} or any(
        not r['compiled'] or r['setup_hash'] != r['expected_setup_hash'] or r['negative_rejections'] != 5 or r['byte_negative_rejections'] != 4 or
        not r['source_definition_unchanged'] or not r['requires_logical_binding'] or r['logical_binding_present']
        for r in rows):
        raise RuntimeError('Compiler or logical-binding diagnostic differs; result.json preserved')
    recheck(pins)
    if delta_verifier.verify(args.delta, args.expected_sha256) != verified:
        raise ValueError('External delta evidence changed during compiler replay')
    receipt = {'status': 'PassedSourceCompilerDiagnostic', 'marker': MARKER, 'source_pins': pins,
               'delta_verification': verified, 'result_sha256': sha(output / 'result.json'),
               'rows': rows, 'runtime_admission': False, 'source_fit_certified': False, 'mission_ready': False,
               'scope': 'Verified external data and original source compiler; required mission logical binding absent'}
    (output / 'receipt.json').write_text(json.dumps(receipt, indent=2) + '\n')
    print(json.dumps({'status': receipt['status'], 'rows': rows, 'mission_ready': False}))


if __name__ == '__main__':
    main()
