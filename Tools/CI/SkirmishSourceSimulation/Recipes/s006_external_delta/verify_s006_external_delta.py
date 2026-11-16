#!/usr/bin/env python3
"""Verify a pinned S006 diagnostic delta; this never grants runtime admission."""
import argparse
import hashlib
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[5]
EXPORTER = ROOT / 'Tools/CI/SkirmishSourceSimulation/Recipes/export_s006_config_delta.py'
TOOL_ROOTS = (
    Path('/Applications/Unity/Hub/Editor/6000.5.8f1/Unity.app/Contents/Resources/Scripting'),
    Path('/usr/local/share/dotnet/sdk/8.0.423/Roslyn/bincore'),
)
KEYS = {'kind', 'schema_version', 'scope', 'candidate_receipt', 'candidate_result',
        'fixed_data_audit', 'exporter', 'rows', 'runtime_admission',
        'source_fit_certified', 'mission_ready'}


def digest(data):
    return hashlib.sha256(data).hexdigest()


def pairs(rows):
    result = {}
    for key, value in rows:
        if key in result:
            raise ValueError('Duplicate JSON key: ' + key)
        result[key] = value
    return result


def parse(raw):
    def invalid(value):
        raise ValueError('Non-finite JSON number: ' + value)
    return json.loads(raw, object_pairs_hook=pairs, parse_constant=invalid)


def shape(delta):
    if (type(delta) is not dict or set(delta) != KEYS or
            delta['kind'] != 'S006ExternalConfigDeltaCandidate' or
            type(delta['schema_version']) is not int or delta['schema_version'] != 1 or
            any(delta[k] is not False for k in ('runtime_admission', 'source_fit_certified', 'mission_ready')) or
            type(delta['rows']) is not list or len(delta['rows']) != 3):
        raise ValueError('Exact non-admitting S006 diagnostic payload required')
    sizes = [r.get('size') if type(r) is dict else None for r in delta['rows']]
    if len(set(sizes)) != 3 or set(sizes) != {'Standard', 'War', 'LargeWar'}:
        raise ValueError('Exactly three distinct S006 sizes required')


def checked_path(value, allow_tools=False):
    path = Path(value)
    allowed = path.is_relative_to(ROOT) or (allow_tools and any(path.is_relative_to(root) for root in TOOL_ROOTS))
    if not path.is_absolute() or path.resolve(strict=True) != path or not allowed:
        raise ValueError('Canonical repository or pinned tool evidence path required')
    return path


def descriptor(value, allow_tools=False):
    if (type(value) is not dict or set(value) != {'path', 'sha256'} or
            type(value['path']) is not str or type(value['sha256']) is not str or
            re.fullmatch('[0-9a-f]{64}', value['sha256']) is None):
        raise ValueError('Exact SHA-256 descriptor required')
    path = checked_path(value['path'], allow_tools)
    raw = path.read_bytes()
    if digest(raw) != value['sha256']:
        raise ValueError('Evidence bytes differ: ' + str(path))
    return path, raw


def verify(delta_path, expected_sha256):
    if type(expected_sha256) is not str or re.fullmatch('[0-9a-f]{64}', expected_sha256) is None:
        raise ValueError('Independent expected input SHA-256 required')
    path = checked_path(str(delta_path))
    raw = path.read_bytes()
    if digest(raw) != expected_sha256:
        raise ValueError('External delta differs from caller-pinned bytes')
    delta = parse(raw)
    shape(delta)
    evidence = {k: descriptor(delta[k]) for k in ('candidate_receipt', 'candidate_result', 'fixed_data_audit', 'exporter')}
    receipt_path, receipt_raw = evidence['candidate_receipt']
    result_path, result_raw = evidence['candidate_result']
    audit_path, audit_raw = evidence['fixed_data_audit']
    if evidence['exporter'][0] != EXPORTER or receipt_path.parent != result_path.parent or \
            receipt_path.name != 'receipt.json' or result_path.name != 'result.json':
        raise ValueError('Original exporter and paired compiler evidence required')
    receipt, result, audit = parse(receipt_raw), parse(result_raw), parse(audit_raw)
    if (receipt.get('status') != 'CollectedDisposablePadCandidate' or
            receipt.get('result_sha256') != digest(result_raw) or receipt.get('mission_ready') is not False or
            audit.get('kind') != 'S006BoundCandidateFixedDataAudit' or
            audit.get('input_sha256', {}).get(str(result_path)) != digest(result_raw) or
            audit.get('runtime_admission') is not False or audit.get('mission_ready') is not False):
        raise ValueError('Linked source-only compiler and audit required')
    rows = [{'size': r['size'], 'disposable_pad_projection': {
        'candidate_additions': r['disposable_pad_projection']['candidate_additions'],
        'bound_compiler': {'setup_hash': r['disposable_pad_projection']['bound_compiler']['setup_hash']}}}
        for r in result['rows']]
    if delta['rows'] != rows:
        raise ValueError('Delta differs from the original compiler candidate')
    pins = {}
    for name, expected in list(receipt['source_pins'].items()) + list(audit['input_sha256'].items()):
        _, pin_raw = descriptor({'path': name, 'sha256': expected}, allow_tools=True)
        if name in pins and pins[name] != expected:
            raise ValueError('Conflicting evidence pins')
        pins[name] = digest(pin_raw)
    # Recheck the four descriptors and input after transitive reads; callers must
    # similarly recheck before and after consuming them in a running host.
    for name in evidence:
        descriptor(delta[name])
    if digest(path.read_bytes()) != expected_sha256:
        raise ValueError('External delta changed during verification')
    return {'status': 'PassedDiagnosticEvidence', 'delta': {'path': str(path), 'sha256': expected_sha256},
            'evidence_pin_count': len(pins), 'runtime_admission': False,
            'source_fit_certified': False, 'mission_ready': False}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--delta', type=Path, required=True)
    parser.add_argument('--expected-sha256', required=True)
    args = parser.parse_args()
    print(json.dumps(verify(args.delta, args.expected_sha256), sort_keys=True))


if __name__ == '__main__':
    main()
