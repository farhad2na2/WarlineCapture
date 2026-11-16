#!/usr/bin/env python3
"""Compare an original-owner isolated diagnostic against native observation."""
import argparse
import hashlib
import json
from pathlib import Path


def sha(path):
    digest = hashlib.sha256()
    with path.open('rb') as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b''):
            digest.update(block)
    return digest.hexdigest()


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--isolated', type=Path, required=True)
    parser.add_argument('--isolated-sha256', required=True)
    parser.add_argument('--native', type=Path, required=True)
    parser.add_argument('--native-receipt', type=Path, required=True)
    parser.add_argument('--identity-comparison', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    if sha(args.isolated) != args.isolated_sha256:
        raise ValueError('Isolated result hash differs')
    isolated = json.loads(args.isolated.read_text())
    native = json.loads(args.native.read_text())
    receipt = json.loads(args.native_receipt.read_text())
    identity = json.loads(args.identity_comparison.read_text())
    if (receipt['wrapper_exit_code'] != 0 or receipt['required_marker_present'] is not True or
        receipt['validation_status'] != 'Passed' or sha(args.native) != receipt['census_sha256'] or
        identity['native_sha256'] != receipt['census_sha256'] or identity['status'] != 'IdentitySetsMatch' or
        isolated['capture_sha256'] != identity['source_sha256'] or
        isolated['status'] != 'IsolatedRoundtrip_NotMissionImport' or
        isolated['normalization_owner'] != 'SkirmishWorldSetup.NormalizeScenery' or
        isolated['manager_exposed_to_mission'] is not False or isolated['mission_ready'] is not False):
        raise ValueError('Exact retained native and isolated diagnostic lineage required')
    key = lambda row: (row['stable_id'], row['source_global_object_id'], row['placement_index'])
    a, b = isolated['normalized_buildings'], native['post_stream']['entities']
    left, right = {key(row): row for row in a}, {key(row): row for row in b}
    if len(left) != len(a) or len(right) != len(b) or set(left) != set(right):
        raise ValueError('Unique matching stable source-building identities required')
    fields = ('faction', 'ai', 'combat_owner', 'storage_owner', 'health_current')
    differences = {field: sum(left[k][field] != right[k][field] for k in left) for field in fields}
    result = {'kind': 'S073NormalizedBuildingComparison',
              'status': 'ObservedFieldsMatch' if not any(differences.values()) else 'ObservedFieldsDiffer',
              'isolated_sha256': args.isolated_sha256, 'native_sha256': receipt['census_sha256'],
              'building_count': len(left), 'field_difference_counts': differences,
              'scope': 'Five observed fields after original owner in isolated source world; no mission state transferred',
              'full_payload_parity': False, 'source_fit_certified': False, 'scheduler_admitted': False,
              'mission_ready': False}
    with args.output.open('x') as output:
        json.dump(result, output, indent=2)
        output.write('\n')
    print(json.dumps(result, separators=(',', ':')))
    return 0 if result['status'] == 'ObservedFieldsMatch' else 2


if __name__ == '__main__':
    raise SystemExit(main())
