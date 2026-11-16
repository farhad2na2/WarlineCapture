#!/usr/bin/env python3
"""Isolate runtime options affecting existing S006 source compiler fixture hashes."""
import hashlib
import json
import os
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[5]
LOGS = ROOT / 'T Logs/SkirmishProduction'
BASE = LOGS / 's006-current-typed-pad-replay-20261106-T4'
CONSTRAINED = LOGS / 's006-current-typed-pad-replay-20261106-T7'
BUILD = LOGS / 's003-current-prefab-import-build-20261104-T1/build.json'
CONFIG = LOGS / 's004-current-physical-input-projection-20261104-T1/mono-config.xml'
OUT = LOGS / 's006-mono-hash-options-20261106-T2'


def sha(path):
    h = hashlib.sha256()
    with Path(path).open('rb') as f:
        for chunk in iter(lambda: f.read(1024 * 1024), b''):
            h.update(chunk)
    return h.hexdigest()


def main():
    OUT.mkdir(exist_ok=False)
    try:
        old = json.loads((BASE / 'receipt.json').read_bytes())
        constrained = json.loads((CONSTRAINED / 'receipt.json').read_bytes())
        if old.get('status') != 'CollectedUnqualifiedCurrentTypedPadReplay' or constrained.get('status') != old['status']:
            raise ValueError('Exact diagnostic receipts required')
        if old['result_sha256'] != sha(BASE / 'result.json') or constrained['result_sha256'] != sha(CONSTRAINED / 'result.json'):
            raise ValueError('Exact unchanged diagnostic results required')
        pins = dict(old['source_pins'])
        stale_wrapper = str(ROOT / 'Tools/CI/SkirmishSourceSimulation/Recipes/s006_external_delta/run_current_typed_pad_replay.py')
        historical_wrapper_hash = pins.pop(stale_wrapper)
        for path in (Path(__file__), BASE / 'receipt.json', BASE / 'result.json', BASE / 'control.exe', CONSTRAINED / 'receipt.json', CONSTRAINED / 'result.json', BUILD, CONFIG):
            pins[str(path)] = sha(path)
        def verify():
            for path, expected in pins.items():
                if not Path(path).is_file() or sha(path) != expected:
                    raise ValueError('Pinned source changed: ' + path)
        verify()
        runtime = json.loads(BUILD.read_bytes())['runtime_selection']
        env = os.environ.copy(); env['MONO_PATH'] = os.pathsep.join((str(BUILD.parent / 'placement'), runtime['engine'], str(Path(runtime['newtonsoft']).parent), str(ROOT / 'Library/ScriptAssemblies'), str(BASE)))
        graph = LOGS / 's005-original-compiler-probe-inputs-20261105-T2/source-config-graph.json'
        matrix = ROOT / 'Assets/Game/Resources/SkirmishExpansion/InitialSetupMatrix.csv'
        delta = LOGS / 's006-external-config-candidate-20261105-T1/data.json'
        proposal = LOGS / 's006-static-footprint-proposals-20261105-T1/report.json'
        search = LOGS / 's006-original-initial-search-20261106-T1/result.json'
        cases = [('config_only', ['--config', str(CONFIG)]), ('float32_disabled_only', ['--optimize=-float32']), ('config_and_float32_disabled', ['--config', str(CONFIG), '--optimize=-float32'])]
        results = []
        for label, options in cases:
            result = OUT / (label + '.json'); layout = OUT / (label + '-layout.json')
            command = [runtime['mono'], *options, str(BASE / 'control.exe'), str(graph), str(matrix), str(result), str(layout), '1', str(delta), pins[str(delta)], str(proposal), str(search)]
            run = subprocess.run(command, cwd=ROOT, env=env, capture_output=True, text=True, timeout=300)
            (OUT / (label + '.log')).write_text(run.stdout + run.stderr)
            if run.returncode or run.stdout.splitlines().count('[S006SourceCandidate] result=Collected sizes=3 candidateOnly=1 readiness=0') != 1:
                raise RuntimeError('Option isolation failed: ' + label)
            data = json.loads(result.read_bytes())
            hashes = {row['size']: row['disposable_pad_projection']['setup_hash'] for row in data['rows']}
            results.append({'case': label, 'options': options, 'setup_hashes': hashes, 'result_sha256': sha(result)})
        verify()
        original = {row['size']: row['disposable_pad_projection']['setup_hash'] for row in json.loads((BASE / 'result.json').read_bytes())['rows']}
        target = {row['size']: row['disposable_pad_projection']['setup_hash'] for row in json.loads((CONSTRAINED / 'result.json').read_bytes())['rows']}
        report = {'status': 'CollectedMonoOptionHashComparison', 'source_pins': pins, 'historical_wrapper_path': stale_wrapper, 'historical_wrapper_sha256': historical_wrapper_hash, 'current_wrapper_sha256': sha(Path(stale_wrapper)), 'historical_wrapper_current': False, 'original_default_hashes': original, 'constrained_run_hashes': target, 'cases': results, 'scope': 'Retained executable diagnostic; original wrapper changed since its run, so build provenance remains historical. One compiler fixture per runtime option; not simulation parity or native hash qualification.', 'mission_ready': False}
        (OUT / 'receipt.json').write_text(json.dumps(report, indent=2) + '\n')
        print(json.dumps({'status': report['status'], 'default': original, 'target': target, 'cases': results, 'mission_ready': False}))
    except Exception as error:
        (OUT / 'failure.json').write_text(json.dumps({'status': 'Failed', 'error': repr(error), 'mission_ready': False}, indent=2) + '\n')
        raise


if __name__ == '__main__':
    main()
