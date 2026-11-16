#!/usr/bin/env python3
"""Compare source-building blocker histories, not the complete live grid."""
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
        isolated['static_blocker_owner']['owner'] != 'StaticGridBlockerUpdateSystem.OnUpdate' or
        isolated['manager_exposed_to_mission'] is not False or isolated['mission_ready'] is not False):
        raise ValueError('Exact retained native and isolated diagnostic lineage required')
    post = native['post_stream']
    if not post['static_grid_committed'] or post['static_history_bound_count'] != post['entity_count']:
        raise ValueError('Committed native grid and all source histories required')
    key = lambda row: (row['stable_id'], row['source_global_object_id'], row['placement_index'])
    a, b = isolated['normalized_buildings'], post['entities']
    left, right = {key(row): row for row in a}, {key(row): row for row in b}
    if len(left) != len(a) or len(right) != len(b) or set(left) != set(right):
        raise ValueError('Unique matching source-building identities required')
    if any(row['static_history'] is None or row['static_history']['bound_to_grid_epoch'] is not True
           for row in a + b):
        raise ValueError('Every compared history must bind its own committed grid epoch')
    fields = ('min', 'max', 'friendly_pass_faction')
    differences = {field: sum(left[k]['static_history'][field] != right[k]['static_history'][field] for k in left)
                   for field in fields}
    result = {'kind': 'S073StaticBlockerHistoryComparison',
              'status': 'HistoriesMatch' if not any(differences.values()) else 'HistoriesDiffer',
              'isolated_sha256': args.isolated_sha256, 'native_sha256': receipt['census_sha256'],
              'building_count': len(left), 'field_difference_counts': differences,
              'scope': 'Original owner history for source buildings only; live grid may include other entities',
              'full_grid_parity': False, 'source_fit_certified': False, 'scheduler_admitted': False,
              'mission_ready': False}
    with args.output.open('x') as output:
        json.dump(result, output, indent=2)
        output.write('\n')
    print(json.dumps(result, separators=(',', ':')))
    return 0 if result['status'] == 'HistoriesMatch' else 2


if __name__ == '__main__':
    raise SystemExit(main())
