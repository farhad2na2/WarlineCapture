#!/usr/bin/env python3
"""Export disposable S006 configuration values with the exact source evidence."""
import argparse
import hashlib
import json
import math
from pathlib import Path


def descriptor(path):
    path = path.resolve(strict=True)
    h = hashlib.sha256()
    with path.open('rb') as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b''):
            h.update(block)
    return {'path': str(path), 'sha256': h.hexdigest()}


def finite(value):
    if isinstance(value, float) and not math.isfinite(value):
        return False
    if isinstance(value, dict):
        return all(finite(item) for item in value.values())
    if isinstance(value, list):
        return all(finite(item) for item in value)
    return True


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--candidate', type=Path, required=True)
    parser.add_argument('--audit', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    root = Path(__file__).resolve().parents[4]
    candidate, audit_path, output = args.candidate.resolve(strict=True), args.audit.resolve(strict=True), args.output.resolve()
    if any(not path.is_relative_to(root) for path in (candidate, audit_path, output)):
        raise ValueError('Repository-owned candidate, audit and output required')
    receipt_path, result_path = candidate / 'receipt.json', candidate / 'result.json'
    receipt, result, audit = (json.loads(path.read_bytes()) for path in (receipt_path, result_path, audit_path))
    if (receipt.get('status') != 'CollectedDisposablePadCandidate' or receipt.get('result_sha256') != descriptor(result_path)['sha256']
            or receipt.get('mission_ready') is not False or audit.get('kind') != 'S006BoundCandidateFixedDataAudit'
            or audit.get('input_sha256', {}).get(str(result_path)) != descriptor(result_path)['sha256']
            or audit.get('runtime_admission') is not False or audit.get('mission_ready') is not False):
        raise ValueError('Exact linked source-only compiler and audit required')
    for name, expected in receipt['source_pins'].items():
        if descriptor(Path(name))['sha256'] != expected:
            raise ValueError('Source pin changed: ' + name)
    for name, expected in audit.get('input_sha256', {}).items():
        if descriptor(Path(name))['sha256'] != expected:
            raise ValueError('Audit input changed: ' + name)
    audit_sizes = {row['size']: row for row in audit['sizes']}
    rows = []
    for row in result['rows']:
        size = row['size']
        projection = row['disposable_pad_projection']
        compiled = projection['bound_compiler']
        delta = projection['candidate_additions']
        check = audit_sizes[size]
        if (compiled.get('compiled') is not True or projection.get('binding_applied') is not True
                or projection.get('missing_binding_rejected') is not True or check.get('fixed_scalar_mismatches') != []
                or check.get('force_identity_and_quantity_equal') is not True
                or check.get('structure_identity_and_count_equal') is not True
                or delta.get('declared_size') != size or delta.get('declared_difficulty') != 'Regular'
                or delta.get('measured_deployments') is not False or delta.get('bind_all_configurations') is not False
                or len(delta['anchors']) != len(delta['pads']) or len(delta['pads']) != len(delta['instance_bindings'])
                or not delta['pads'] or not finite(delta)):
            raise ValueError('Complete finite and fixed-data-consistent candidate required: ' + size)
        rows.append({'size': size, 'disposable_pad_projection': {'candidate_additions': delta,
                     'bound_compiler': {'setup_hash': compiled['setup_hash']}}})
    if {row['size'] for row in rows} != {'Standard', 'War', 'LargeWar'} or len(rows) != 3:
        raise ValueError('Exactly the three original size variants required')
    payload = {'kind': 'S006ExternalConfigDeltaCandidate', 'schema_version': 1,
               'scope': 'Disposable data-only candidate on existing Campaign geometry; enemy construction behavior remains unqualified',
               'candidate_receipt': descriptor(receipt_path), 'candidate_result': descriptor(result_path),
               'fixed_data_audit': descriptor(audit_path), 'exporter': descriptor(Path(__file__)),
               'rows': rows, 'runtime_admission': False, 'source_fit_certified': False, 'mission_ready': False}
    output.parent.mkdir(parents=True, exist_ok=False)
    with output.open('x') as stream:
        json.dump(payload, stream, indent=2, allow_nan=False)
        stream.write('\n')
    print(json.dumps({'kind': payload['kind'], 'sizes': [row['size'] for row in rows], 'mission_ready': False}))


if __name__ == '__main__':
    main()
