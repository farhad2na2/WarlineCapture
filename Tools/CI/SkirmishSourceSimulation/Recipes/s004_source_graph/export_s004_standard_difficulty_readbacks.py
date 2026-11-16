#!/usr/bin/env python3
"""Extract four source-only S004 compiler readbacks; never establish admission."""
import argparse
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[5]
INPUT = ROOT / 'T Logs/SkirmishProduction/s004-current-standard-difficulty-compiler-20261105-T3'
EXPECTED = {'Recruit': (1, 106654757), 'Regular': (2, 1130726651),
            'Veteran': (3, 3038972422), 'Commander': (4, 3329072857)}

def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest()

def verify(receipt):
    if receipt.get('status') != 'PassedOriginalCompilerSourceOnly' or receipt.get('runtime_admission') is not False or receipt.get('mission_ready') is not False:
        raise ValueError('Unqualified original source compiler receipt required')
    for name, expected in {**receipt['source_pins'], **receipt['explicit_input_sha256']}.items():
        path = Path(name)
        if not path.is_file() or sha(path) != expected: raise ValueError('Compiler source pin changed: ' + name)
    if sha(INPUT / 'result.json') != receipt['result_sha256']: raise ValueError('Compiler result changed')

def export(output):
    receipt_path = INPUT / 'receipt.json'; receipt = json.loads(receipt_path.read_bytes()); verify(receipt)
    result = json.loads((INPUT / 'result.json').read_bytes())
    if result.get('world_allocated') is not False or result.get('runtime_admission') is not False or result.get('mission_ready') is not False:
        raise ValueError('Source-only compiler scope required')
    rows = [r for r in result['rows'] if r['size'] == 'Standard']
    if len(rows) != 4 or {r['difficulty'] for r in rows} != set(EXPECTED): raise ValueError('Four exact Standard rows required')
    output.mkdir(exist_ok=False)
    descriptors = {}
    for row in rows:
        name = row['difficulty']; difficulty, setup_hash = EXPECTED[name]; setup = row['compiled_setup']
        if (not row['compiled'] or row['compiler_exception'] is not None or row['seed'] != 104733 or
            row['setup_hash'] != setup_hash or setup['DifficultyId'] != difficulty or setup['SizeId'] != 1 or
            setup['CatalogId'] != 'S004' or setup['DefinitionId'] != 'skirmish.s004' or
            setup['MeasuredLayoutBound'] is not True or len(setup['Forces']) != 18 or len(setup['Structures']) != 20 or
            any(setup[k] <= 0 for k in ('WorldWidthMetres', 'WorldDepthMetres', 'CellSize'))):
            raise ValueError('Original compiler row identity mismatch: ' + name)
        value = {'kind': 'S004StandardDifficultySourceCompilerReadback', 'status': 'Collected',
                 'difficulty': name, 'source_compiler_receipt': {'path': str(receipt_path), 'sha256': sha(receipt_path)},
                 'source_compiler_result': {'path': str(INPUT / 'result.json'), 'sha256': receipt['result_sha256']},
                 'compiled_setup': setup, 'world_initialized': False, 'physical_source_fit_verified': False,
                 'runtime_admission': False, 'mission_readiness': False}
        target = output / (name.lower() + '.json')
        with target.open('x') as stream: json.dump(value, stream, indent=2); stream.write('\n')
        descriptors[name] = {'path': str(target.resolve()), 'sha256': sha(target), 'setup_hash': setup_hash}
    verify(receipt)
    summary = {'status': 'CollectedSourceOnlyReadbacks', 'source_receipt_sha256': sha(receipt_path),
               'readbacks': descriptors, 'world_initialized': False, 'runtime_admission': False, 'mission_ready': False}
    with (output / 'receipt.json').open('x') as stream: json.dump(summary, stream, indent=2); stream.write('\n')
    return summary

if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__); parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--input', type=Path, help='Explicit checked source compiler output directory')
    args = parser.parse_args()
    if args.input is not None:
        INPUT = args.input.resolve(strict=True)
        if not INPUT.is_relative_to(ROOT):
            raise ValueError('Source compiler output must be inside the repository')
    print(json.dumps(export(args.output.resolve())))
