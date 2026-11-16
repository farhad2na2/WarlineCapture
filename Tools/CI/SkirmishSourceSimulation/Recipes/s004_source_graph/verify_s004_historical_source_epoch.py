#!/usr/bin/env python3
"""Verify retained S004 inputs as historical evidence, never as current capture."""
import argparse
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[5]
BUNDLE = ROOT / 'T Logs/SkirmishProduction/s004-current-guard-input-bundle-20261105-T1/bundle.json'
STARTUP = ROOT / 'T Logs/SkirmishProduction/s004-current-guard-startup-run-20261105-T2/receipt.json'
SNAPSHOT = ROOT / 'T Logs/SkirmishProduction/s004-historical-readmodel-difficulty-join-audit-20261105-T1/historical-SkirmishMapLayoutBuilder.cs'
BUILDER = ROOT / 'Assets/Game/Scripts/Configs/Skirmish/SkirmishMapLayoutBuilder.cs'
HISTORICAL_SHA = 'f18146979c18d7d249df525a005401c49c281cb8a103ce5fc636f6d2826ced5c'
CURRENT_SHA = '3cb5a53aab2a641666029e50411a97a4e0dad2aeefa960bcb8c06c6a962cec13'
OLD = '''((setup.DifficultyId == SkirmishDifficultyId.Regular &&
                   setup.SizeId == SkirmishSizeId.Standard) ||'''
NEW = '''((setup.SizeId == SkirmishSizeId.Standard &&
                   (setup.DifficultyId == SkirmishDifficultyId.Regular ||
                    (setup.CatalogId == "S004" &&
                     (setup.DifficultyId == SkirmishDifficultyId.Recruit ||
                      setup.DifficultyId == SkirmishDifficultyId.Veteran ||
                      setup.DifficultyId == SkirmishDifficultyId.Commander)))) ||'''

def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest()

def verify():
    if sha(SNAPSHOT) != HISTORICAL_SHA or sha(BUILDER) != CURRENT_SHA:
        raise ValueError('Exact historical and current builder source required')
    old = SNAPSHOT.read_text(); current = BUILDER.read_text()
    if old.count(OLD) != 1 or old.replace(OLD, NEW, 1) != current:
        raise ValueError('Current builder is not the single reviewed binding change')
    bundle_sha, startup_sha = sha(BUNDLE), sha(STARTUP)
    bundle, startup = json.loads(BUNDLE.read_bytes()), json.loads(STARTUP.read_bytes())
    result = startup.get('result', {})
    if (bundle.get('kind') != 'S004SourceDiagnosticInputBundle' or startup.get('status') != 'ObservedDiagnosticSourcePlacement' or
        result.get('request_count') != 20 or result.get('succeeded') != 20 or result.get('registered') != 20 or
        startup.get('physical_source_validated') is not False or startup.get('runtime_admission') is not False or startup.get('mission_ready') is not False):
        raise ValueError('Exact prior source-only startup evidence required')
    inputs = bundle['inputs']; prior_pins = startup['input_sha256']; cache = {}; historical = []; maps = {}
    for label, descriptor in inputs.items():
        path = Path(descriptor['path'])
        if not path.is_absolute() or not path.is_file() or sha(path) != descriptor['sha256'] or prior_pins.get(str(path)) != descriptor['sha256']:
            raise ValueError('Retained input or original startup pin changed: ' + label)
        if path.suffix != '.json': continue
        payload = json.loads(path.read_bytes())
        key = next((k for k in ('source_pins','source_hashes','input_sha256') if isinstance(payload.get(k),dict)), None)
        if key is None: continue
        source_pins = payload[key]; maps[label] = len(source_pins)
        for raw, expected in source_pins.items():
            file = Path(raw); file = file if file.is_absolute() else ROOT / file
            resolved = file.resolve(strict=True); name = str(resolved)
            if name not in cache: cache[name] = sha(resolved)
            if cache[name] == expected: continue
            if resolved != BUILDER or expected != HISTORICAL_SHA or cache[name] != CURRENT_SHA:
                raise ValueError('Unexplained historical source mismatch: ' + raw)
            historical.append({'capture':label,'recorded_path':raw,'historical_sha256':expected,
                               'current_sha256':cache[name],'snapshot_path':str(SNAPSHOT)})
    if {x['capture'] for x in historical} != {'prefab_shared','prefab_s004','building_definitions','configured_building_readmodel','static_blockers'}:
        raise ValueError('Unexpected historical capture difference set')
    for name, expected in cache.items():
        if sha(Path(name)) != expected: raise ValueError('Source changed during historical verification: ' + name)
    for label, descriptor in inputs.items():
        if sha(Path(descriptor['path'])) != descriptor['sha256']:
            raise ValueError('Retained input changed during historical verification: ' + label)
    if sha(BUNDLE) != bundle_sha or sha(STARTUP) != startup_sha or sha(BUILDER) != CURRENT_SHA or sha(SNAPSHOT) != HISTORICAL_SHA:
        raise ValueError('Evidence changed during historical verification')
    return {'kind':'S004HistoricalSourceEpochVerification','status':'PassedHistoricalInputsOnly',
            'bundle':{'path':str(BUNDLE),'sha256':bundle_sha},'prior_startup':{'path':str(STARTUP),'sha256':startup_sha},
            'input_pin_counts':maps,'unique_source_paths_checked':len(cache),'historical_differences':historical,
            'scope':'Historical direct embedded pin maps; current capture, transitive closure and placement parity not asserted',
            'historical_capture_current_qualified':False,'world_initialized':False,'runtime_admission':False,
            'source_fit_certified':False,'mission_ready':False}

if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__); parser.add_argument('--receipt', type=Path, required=True)
    args = parser.parse_args(); result = verify()
    with args.receipt.open('x') as stream: json.dump(result, stream, indent=2); stream.write('\n')
    print(json.dumps({'status':result['status'],'historical_differences':len(result['historical_differences']),
                      'unique_source_paths_checked':result['unique_source_paths_checked'],'mission_ready':False}))
