#!/usr/bin/env python3
"""Compare stable building identities; never import captured Campaign state."""
import argparse
import base64
import collections
import hashlib
import json
from pathlib import Path
from audit_s073_streamed_source import audit as audit_source


def sha(path):
    digest = hashlib.sha256()
    with path.open('rb') as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b''):
            digest.update(block)
    return digest.hexdigest()


def need(condition, message):
    if not condition:
        raise ValueError(message)


def string_value(value):
    need(value['codec'] == 'binary-v1' and value['encoding'] == 'fixed-utf8-v1', 'Fixed string codec required')
    raw = base64.b64decode(value['base64'], validate=True)
    need(len(raw) == value['byte_count'] and hashlib.sha256(raw).hexdigest() == value['sha256'],
         'Fixed string bytes changed')
    return raw.decode('utf-8')


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--source', required=True, type=Path)
    parser.add_argument('--source-sha256', required=True)
    parser.add_argument('--source-receipt', required=True, type=Path)
    parser.add_argument('--native', required=True, type=Path)
    parser.add_argument('--native-sha256', required=True)
    parser.add_argument('--native-receipt', required=True, type=Path)
    parser.add_argument('--output', required=True, type=Path)
    args = parser.parse_args()
    need(sha(args.source) == args.source_sha256 and sha(args.native) == args.native_sha256, 'Input hash mismatch')
    source_receipt = json.loads(args.source_receipt.read_text())
    native_receipt = json.loads(args.native_receipt.read_text())
    need(source_receipt['capture_sha256'] == args.source_sha256 and
         native_receipt['census_sha256'] == args.native_sha256 and native_receipt['wrapper_exit_code'] == 0 and
         native_receipt['required_marker_present'] is True and native_receipt['validation_status'] == 'Passed',
         'Checked source and native wrapper receipts required')
    audit_source(args.source, source_receipt)
    source = json.loads(args.source.read_text())
    native = json.loads(args.native.read_text())
    need(source['kind'] == 'S073IsolatedStreamedSourceWorldCapture' and
         source['scene_guid'] == 'c00140f2e94a04c3084c8dcb0c18cbd0' and
         source['closure_complete'] is False and source['mission_ready'] is False, 'Exact isolated source required')
    need(native['kind'] == 'S073PrepareMapBoundaryAndStreamCensus' and native['status'] == 'Passed' and
         native['mission_ready'] is False and native['mission_outcome_observed'] is False,
         'Same-session observed native census required')
    post = native['post_stream']
    need(isinstance(native.get('session_id'), str) and bool(native['session_id']), 'Native session identity required')
    before, boundary = native['before'], native['after']
    need(before['phase'] == 'Queued' and boundary['phase'] == 'Preparing', 'Original queued-to-preparing boundary required')
    for field in ('world_sequence', 'session_entity', 'map_root_entity', 'generation',
                  'source_identity_hash', 'source_content_hash'):
        need(before[field] == boundary[field] == post[field], 'Native identity changed across observation: ' + field)
    need(post['ecs_scene_guid'] == source['scene_guid'] and post['source_scene_loaded'] and
         post['ecs_scene_loaded'] and post['authored_grid_section_owned'] and
         post['physical_source_validated'] == 1 and post['phase'] in ('Preparing', 'Playing') and
         native['source_pins'].get(source['scene_path']) == source['scene_sha256'],
         'Same physical source, loaded scene and owned grid evidence required')
    roots = set(source['root_entity_ids'])
    need(len(roots) == source['building_faction_query_count'], 'Unique source roots required')
    before, source_factions = [], collections.Counter()
    for row in source['entities']:
        if row['id'] not in roots:
            continue
        fields = row['components']['Game.Components.OperationMapBuildingComponent']['value']['fields']
        prefix = 'Game.Components.OperationMapBuildingComponent::'
        key = (string_value(fields[prefix + 'StableId']), string_value(fields[prefix + 'SourceGlobalObjectId']),
               int(fields[prefix + 'PlacementIndex']['value']))
        before.append(key)
        source_factions[int(row['components']['Game.Components.Faction']['value']['fields']['Game.Components.Faction::Id']['value'])] += 1
    after = [(row['stable_id'], row['source_global_object_id'], row['placement_index']) for row in post['entities']]
    need(all(row.get('source_section_owned') is True for row in post['entities']),
         'All compared native buildings must be owned by the source section')
    need(len(before) == len(roots) == len(set(before)) and len(after) == post['entity_count'] == len(set(after)),
         'Unique stable identities required in both worlds')
    left, right = set(before), set(after)
    unmatched_source, unmatched_native = left - right, right - left
    neutral = sum(row['faction'] == 0 for row in post['entities'])
    owner = sum(row['source_section_owned'] for row in post['entities'])
    result = {'kind': 'S073SourceBuildingIdentityComparison',
              'status': 'IdentitySetsMatch' if not unmatched_source and not unmatched_native else 'IdentityMismatch',
              'source_sha256': args.source_sha256, 'native_sha256': args.native_sha256,
              'source_roots': len(before), 'native_roots': len(after), 'matched': len(left & right),
              'source_only': len(unmatched_source), 'native_only': len(unmatched_native),
              'source_factions': dict(source_factions), 'native_neutral': neutral, 'native_section_owned': owner,
              'session_id': native['session_id'], 'generation': post['generation'],
              'scope': 'Identity and observed ownership only; no entity state transferred to mission',
              'entity_payload_parity': False, 'source_fit_certified': False, 'mission_ready': False}
    with args.output.open('x') as output:
        json.dump(result, output, indent=2)
        output.write('\n')
    print(json.dumps(result, separators=(',', ':')))
    return 0 if result['status'] == 'IdentitySetsMatch' else 2


if __name__ == '__main__':
    raise SystemExit(main())
