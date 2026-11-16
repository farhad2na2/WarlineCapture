#!/usr/bin/env python3
"""Prepare explicit unqualified S004 source-world inputs; never allocate a world."""
import argparse
import hashlib
import json
from pathlib import Path
import verify_s004_historical_source_epoch as historical
import verify_s004_difficulty_readback as readback

ROOT = Path(__file__).resolve().parents[5]
BUILD = ROOT / 'T Logs/SkirmishProduction/s004-unqualified-source-build-20261105-T1/build.json'
HELPER = ROOT / 'Tools/CI/SkirmishSourceSimulation/Adapters/DeclaredGround/SourceS004HistoricalReadmodelCompatibility.cs'
CONTROL = ROOT / 'T Logs/SkirmishProduction/s004-historical-readmodel-compatibility-control-20261105-T1/receipt.json'
READBACK_RECEIPT = ROOT / 'T Logs/SkirmishProduction/s004-current-standard-difficulty-readbacks-20261105-T3/receipt.json'

def sha(path): return hashlib.sha256(Path(path).read_bytes()).hexdigest()
def descriptor(path): return {'path':str(path.resolve(strict=True)),'sha256':sha(path),'bytes':path.stat().st_size}

def prepare(difficulty, output):
    if output.exists(): raise ValueError('Fresh diagnostic descriptor directory required')
    build = json.loads(BUILD.read_bytes()); control = json.loads(CONTROL.read_bytes()); entries = json.loads(READBACK_RECEIPT.read_bytes())['readbacks']
    if difficulty not in entries: raise ValueError('Exact available S004 Standard difficulty required')
    if build.get('status') != 'Passed' or build.get('compile_exit') != 0 or build.get('source_manifest',{}).get('sha256') == build.get('canonical_source_manifest',{}).get('sha256'):
        raise ValueError('Passing noncanonical source-only diagnostic build required')
    if control.get('status') != 'PassedActualSourceCompatibilityFixture' or control.get('helper_sha256') != sha(HELPER) or control.get('compatible') != 4 or control.get('rejected') != 3:
        raise ValueError('Exact actual compatibility fixture required')
    for name, expected in build['source_and_reference_pins'].items():
        if sha(name) != expected: raise ValueError('Current source build pin changed: ' + name)
    old = historical.verify(); selected = readback.verify(difficulty, entries[difficulty]['sha256'])
    output.mkdir(parents=True, exist_ok=False)
    selected_path = Path(selected['readback']['path'])
    evidence = {'kind':'S004HistoricalWorldDiagnosticInputs','status':'PreparedUnqualified',
                'scope':'Original host candidate with explicitly historical captures and source-compiled selected setup; no World',
                'historical_bundle':old['bundle'],'historical_source_epoch':old,
                'selected_readback':descriptor(selected_path),'selected_readback_verification':selected,
                'source_build':descriptor(BUILD),'compatibility_helper':descriptor(HELPER),'compatibility_fixture':descriptor(CONTROL),
                'readback_receipt':descriptor(READBACK_RECEIPT),'difficulty':difficulty,
                'native_numeric_parity':False,'physical_source_validated':False,'world_initialized':False,
                'runtime_admission':False,'source_fit_certified':False,'mission_ready':False}
    path = output / 'diagnostic-inputs.json'
    with path.open('x') as stream: json.dump(evidence, stream, indent=2); stream.write('\n')
    for name, expected in build['source_and_reference_pins'].items():
        if sha(name) != expected: raise ValueError('Current source build changed during preparation: ' + name)
    if sha(selected_path) != selected['readback']['sha256']: raise ValueError('Selected setup changed during preparation')
    result = {'status':'PreparedUnqualified','descriptor':descriptor(path),'difficulty':difficulty,'world_initialized':False,'mission_ready':False}
    with (output/'receipt.json').open('x') as stream: json.dump(result,stream,indent=2);stream.write('\n')
    return result

if __name__ == '__main__':
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--difficulty', required=True)
    parser.add_argument('--output',type=Path,required=True);args=parser.parse_args()
    print(json.dumps(prepare(args.difficulty,args.output.resolve())))
