#!/usr/bin/env python3
"""Verify exact S004 source-compiler readbacks without admitting gameplay."""
import argparse
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[5]
READBACKS = ROOT / 'T Logs/SkirmishProduction/s004-current-standard-difficulty-readbacks-20261105-T3'
COMPILER = ROOT / 'T Logs/SkirmishProduction/s004-current-standard-difficulty-compiler-20261105-T3'
COMPILER_RECEIPT_SHA = 'd1f56c1ad4cbee9a38913596652f84fb9605b77cdecedacc7627f1cf106edf46'
EXPECTED = {'Recruit': (1, 106654757), 'Regular': (2, 1130726651),
            'Veteran': (3, 3038972422), 'Commander': (4, 3329072857)}

def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest()

def unique(pairs):
    result = {}
    for key, value in pairs:
        if key in result: raise ValueError('Duplicate JSON key: ' + key)
        result[key] = value
    return result

def read(path):
    return json.loads(path.read_bytes(), object_pairs_hook=unique,
                      parse_constant=lambda value: (_ for _ in ()).throw(ValueError('Invalid number: ' + value)))

def validate_content(value, difficulty, compiler_receipt_sha, compiler_result_sha, result):
    if difficulty not in EXPECTED: raise ValueError('Unsupported difficulty')
    fields = {'kind','status','difficulty','source_compiler_receipt','source_compiler_result','compiled_setup',
              'world_initialized','physical_source_fit_verified','runtime_admission','mission_readiness'}
    if set(value) != fields or value['kind'] != 'S004StandardDifficultySourceCompilerReadback' or value['status'] != 'Collected' or value['difficulty'] != difficulty:
        raise ValueError('Exact source readback schema and target required')
    for flag in ('world_initialized','physical_source_fit_verified','runtime_admission','mission_readiness'):
        if value[flag] is not False: raise ValueError('Source-only scope required: ' + flag)
    if value['source_compiler_receipt'] != {'path':str(COMPILER / 'receipt.json'),'sha256':compiler_receipt_sha} or value['source_compiler_result'] != {'path':str(COMPILER / 'result.json'),'sha256':compiler_result_sha}:
        raise ValueError('Exact compiler receipt and result links required')
    rows = [r for r in result.get('rows', []) if r.get('size') == 'Standard' and r.get('difficulty') == difficulty]
    number, digest = EXPECTED[difficulty]
    if len(rows) != 1 or rows[0].get('compiled') is not True or rows[0].get('compiler_exception') is not None or rows[0].get('seed') != 104733 or rows[0].get('setup_hash') != digest:
        raise ValueError('Original compiler row mismatch')
    setup = value['compiled_setup']
    if setup != rows[0]['compiled_setup'] or setup.get('DifficultyId') != number or setup.get('SetupHash') != digest or setup.get('MeasuredLayoutBound') is not True:
        raise ValueError('Readback differs from original compiler output')

def verify(difficulty, expected_readback_sha):
    if difficulty not in EXPECTED: raise ValueError('Unsupported difficulty')
    path = READBACKS / (difficulty.lower() + '.json')
    if not path.is_absolute() or path.is_symlink() or path.resolve(strict=True) != path or sha(path) != expected_readback_sha:
        raise ValueError('Exact canonical pinned readback required')
    receipt_path = COMPILER / 'receipt.json'
    if sha(receipt_path) != COMPILER_RECEIPT_SHA: raise ValueError('Pinned compiler receipt changed')
    receipt = read(receipt_path); result_path = COMPILER / 'result.json'
    if receipt.get('status') != 'PassedOriginalCompilerSourceOnly' or receipt.get('runtime_admission') is not False or receipt.get('mission_ready') is not False or sha(result_path) != receipt.get('result_sha256'):
        raise ValueError('Unqualified compiler result and receipt required')
    pins = receipt.get('source_pins')
    if not isinstance(pins, dict) or len(pins) < 4000: raise ValueError('Complete compiler input pins required')
    def check_pins():
        for name, expected in pins.items():
            p = Path(name)
            if not p.is_absolute() or not p.is_file() or sha(p) != expected:
                raise ValueError('Compiler input changed: ' + name)
        for name, key in (('fixture.exe','executable_sha256'), ('compile.log','compile_log_sha256'), ('run.log','run_log_sha256')):
            if sha(COMPILER / name) != receipt[key]: raise ValueError('Compiler artifact changed: ' + name)
    check_pins()
    value = read(path); result = read(result_path)
    if result.get('world_allocated') is not False or result.get('runtime_admission') is not False or result.get('mission_ready') is not False:
        raise ValueError('Compiler result is not source-only')
    validate_content(value, difficulty, COMPILER_RECEIPT_SHA, receipt['result_sha256'], result)
    check_pins()
    if sha(path) != expected_readback_sha or sha(receipt_path) != COMPILER_RECEIPT_SHA or sha(result_path) != receipt['result_sha256']:
        raise ValueError('Evidence changed during verification')
    return {'status':'PassedExactSourceOnlyReadback', 'difficulty':difficulty,
            'readback':{'path':str(path),'sha256':expected_readback_sha},
            'compiler_receipt_sha256':COMPILER_RECEIPT_SHA, 'source_pin_count':len(pins),
            'world_initialized':False,'runtime_admission':False,'source_fit_certified':False,'mission_ready':False}

if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--difficulty', required=True); parser.add_argument('--sha256', required=True)
    parser.add_argument('--receipt', required=True, type=Path)
    args = parser.parse_args(); result = verify(args.difficulty, args.sha256)
    with args.receipt.open('x') as stream: json.dump(result, stream, indent=2); stream.write('\n')
    print(json.dumps(result))
