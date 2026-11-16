#!/usr/bin/env python3
"""Compare two pinned original-compiler S003 setups; never admit runtime inputs."""
import argparse
import hashlib
import json
from pathlib import Path


def pairs(rows):
    result = {}
    for key, value in rows:
        if key in result:
            raise ValueError('duplicate JSON key: ' + key)
        result[key] = value
    return result


def fail_constant(value):
    raise ValueError('non-finite JSON value: ' + value)


def read(path):
    return json.loads(Path(path).read_bytes(), object_pairs_hook=pairs, parse_constant=fail_constant)


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def descriptor(path):
    path = Path(path).resolve(strict=True)
    return {'path': str(path), 'sha256': sha(path)}


def check_descriptor(value):
    if not isinstance(value, dict) or set(value) != {'path', 'sha256'} or descriptor(value['path']) != value:
        raise ValueError('source descriptor differs from current bytes')


def setup(receipt, difficulty, size):
    if (receipt.get('kind'), receipt.get('status'), receipt.get('seed')) != ('S003OriginalCompilerHarnessReceipt', 'Compiled', 104732):
        raise ValueError('original S003 compiler receipt required')
    if any(receipt.get(k) is not False for k in ('world_initialized', 'physical_source_fit_verified', 'mission_readiness')):
        raise ValueError('source-only scope required')
    result = receipt.get('result', {})
    if result.get('compiled') is not True or result.get('kind') != 'S003OriginalCompilerSourceOnlyResult':
        raise ValueError('original successful compiler result required')
    matches = [x for x in result.get('setups', []) if x.get('size_id') == size]
    if len(matches) != 1:
        raise ValueError('exactly one setup for selected size required')
    row = matches[0]
    data = row.get('setup_data')
    if (row.get('catalog_id'), row.get('definition_id'), row.get('seed'), row.get('difficulty_id'), row.get('compiled')) != ('S003', 'skirmish.s003', 104732, difficulty, True) or not isinstance(data, dict):
        raise ValueError('compiled target identity differs')
    if (data.get('CatalogId'), data.get('DefinitionId'), data.get('Seed'), data.get('DifficultyId'), data.get('SizeId'), data.get('SetupHash')) != ('S003', 'skirmish.s003', 104732, {'Recruit': 1, 'Regular': 2, 'Veteran': 3, 'Commander': 4}[difficulty], {'Standard': 1, 'War': 2, 'LargeWar': 3}[size], row.get('setup_hash')):
        raise ValueError('serialized setup identity differs from compiler row')
    return data


def check_logs(path, receipt):
    directory = Path(path).resolve(strict=True).parent
    compile_log = directory / 'compile.log'
    run_log = directory / 'run.log'
    if sha(compile_log) != receipt.get('compile_log_sha256') or sha(run_log) != receipt.get('run_log_sha256'):
        raise ValueError('compiler logs differ from receipt')
    lines = run_log.read_text().strip().splitlines()
    if not lines or json.loads(lines[-1], object_pairs_hook=pairs, parse_constant=fail_constant) != receipt.get('result'):
        raise ValueError('compiler output differs from receipt')


def compare(baseline, candidate, difficulty, size):
    a = setup(baseline, 'Regular', size)
    b = setup(candidate, difficulty, size)
    for key in ('source_build', 'source_graph', 'source_response', 'matrix'):
        if baseline.get(key) != candidate.get(key):
            raise ValueError('compiler provenance differs: ' + key)
        check_descriptor(baseline[key])
    if baseline.get('harness_source') != candidate.get('harness_source') or baseline.get('runner_source') != candidate.get('runner_source'):
        raise ValueError('compiler harness differs')
    for item in (baseline, candidate):
        for key in ('harness_source', 'runner_source'):
            check_descriptor(item[key])
        for name, digest in item.get('source_graph_source_pins', {}).items():
            if sha(name) != digest:
                raise ValueError('graph input drift: ' + name)
    build = read(baseline['source_build']['path'])
    if build.get('status') != 'Passed' or not build.get('source_and_reference_pins'):
        raise ValueError('successful pinned source build required')
    for name, digest in build['source_and_reference_pins'].items():
        if sha(name) != digest:
            raise ValueError('build input drift: ' + name)
    if difficulty == 'Regular' or a['SetupHash'] == b['SetupHash']:
        raise ValueError('distinct non-Regular compiler identity required')
    ignored = {'DifficultyId', 'SetupHash', 'Report'}
    aa = {k: v for k, v in a.items() if k not in ignored}
    bb = {k: v for k, v in b.items() if k not in ignored}
    if json.dumps(aa, sort_keys=True, separators=(',', ':'), allow_nan=False) != json.dumps(bb, sort_keys=True, separators=(',', ':'), allow_nan=False):
        raise ValueError('compiled setup differs beyond difficulty identity')
    return {'difficulty': difficulty, 'size': size, 'baseline_setup_hash': a['SetupHash'], 'candidate_setup_hash': b['SetupHash']}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--baseline', required=True)
    parser.add_argument('--candidate', required=True)
    parser.add_argument('--difficulty', required=True, choices=('Recruit', 'Veteran', 'Commander'))
    parser.add_argument('--size', required=True, choices=('Standard', 'War', 'LargeWar'))
    parser.add_argument('--output', required=True)
    args = parser.parse_args()
    baseline, candidate = read(args.baseline), read(args.candidate)
    check_logs(args.baseline, baseline)
    check_logs(args.candidate, candidate)
    identity = compare(baseline, candidate, args.difficulty, args.size)
    result = {'kind': 'S003SourceDifficultySetupCompatibility', 'status': 'PassedSourceOnly', 'baseline': descriptor(args.baseline), 'candidate': descriptor(args.candidate), 'verifier': descriptor(__file__), 'identity': identity, 'runtime_admission': False, 'native_context_qualified': False, 'mission_ready': False}
    with Path(args.output).open('x') as stream:
        json.dump(result, stream, indent=2)
        stream.write('\n')
    print(json.dumps({'status': result['status'], 'identity': identity, 'runtime_admission': False}))


if __name__ == '__main__':
    main()
