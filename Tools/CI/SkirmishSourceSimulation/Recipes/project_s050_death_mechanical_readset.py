#!/usr/bin/env python3
"""Project an audited S050 native death readset into a bounded diagnostic input."""
import argparse
import hashlib
import json
from pathlib import Path

MAX_CAPTURE_BYTES = 4 * 1024 * 1024
MAX_PROJECTION_BYTES = 1024 * 1024
MAX_PIN_COUNT = 10_000
EXPECTED_KEY_SHA = '2e207fab1547fd55d56ddbb9919c73da4c942ccf2b85dd6d8619a5cf6bc481f7'
EXPECTED_MARKER = '[SkirmishDeathMechanicalReadset] result=Passed prefabs=16 originalPrepare=16 childPermutation=16 fullMission=0'
EXPECTED_KIND = 'NativeSelectedPrefabDeathMechanicalReadset'
EXPECTED_AUTHORITY = 'OriginalNativePrepare_OriginalParentSystem_OriginalKernel_NotMissionState'
FALSE_FLAGS = ('full_mission_initialized', 'renderer_qualified', 'runtime_admitted')
ROW_FIELDS = ('key', 'configured_duration_bits', 'death_animation_index',
              'has_death_animation_selection', 'configured_wreck_bypasses_prepare',
              'legacy_wreck_bypasses_prepare')


def sha(raw):
    return hashlib.sha256(raw).hexdigest()


def read_json(path, maximum):
    path = Path(path)
    if path.is_symlink():
        raise ValueError(f'Bounded regular input required: {path}')
    path = path.resolve(strict=True)
    if not path.is_file() or path.stat().st_size > maximum:
        raise ValueError(f'Bounded regular input required: {path}')
    return path, path.read_bytes()


def pin(path):
    path, raw = read_json(path, 512 * 1024 * 1024)
    return {'path': str(path), 'sha256': sha(raw)}


def verify_source_pins(raw, capture_root):
    pins = raw.get('source_pins')
    root = Path(capture_root).resolve(strict=True)
    if not isinstance(pins, dict) or not 1 <= len(pins) <= MAX_PIN_COUNT:
        raise ValueError('Bounded complete capture source pin map required')
    if root.is_symlink() or not root.is_dir():
        raise ValueError('Regular capture root required')
    for name, expected in pins.items():
        if not isinstance(name, str) or not isinstance(expected, str) or len(expected) != 64:
            raise ValueError(f'Malformed capture source pin: {name}')
        path = Path(name)
        if path.is_absolute():
            if path.resolve(strict=False) != path:
                raise ValueError(f'Noncanonical absolute source pin: {name}')
            full = path
        else:
            if '\\' in name or any(part in ('', '.', '..') for part in name.split('/')):
                raise ValueError(f'Noncanonical relative source pin: {name}')
            if not name.startswith(('Assets/', 'Packages/', 'Library/', 'ProjectSettings/', 'T Logs/')):
                raise ValueError(f'Source pin outside repository domains: {name}')
            full = root.joinpath(*name.split('/'))
        if not full.is_relative_to(root) or full == root or full.is_symlink() or not full.is_file():
            raise ValueError(f'Current regular in-root source pin required: {name}')
        for parent in full.parents:
            if parent == root:
                break
            if not parent.is_relative_to(root) or parent.is_symlink():
                raise ValueError(f'Source pin path symlink or escape: {name}')
        if sha(full.read_bytes()) != expected:
            raise ValueError(f'S050 death capture source pin drift: {name}')
    return len(pins)


def compact_rows(raw, keys):
    rows = raw.get('rows')
    if not isinstance(rows, list) or len(rows) != 16 or [r.get('key') for r in rows] != keys:
        raise ValueError('Exact ordered selected-16 mechanical rows required')
    projected = []
    for row in rows:
        if any(field not in row for field in ROW_FIELDS):
            raise ValueError('Required native mechanical row field missing')
        if (row.get('actual_original_prepare_bits') != row.get('reversed_child_original_prepare_bits') or
                row.get('actual_original_prepare_bits') != row.get('original_fold_bits') or
                row.get('actual_original_prepare_bits') != row.get('unit_death_time_remaining_assignment_bits')):
            raise ValueError(f'Original Prepare / child-order / fold controls differ: {row.get("key")}')
        visual = row.get('visual_readset')
        if not isinstance(visual, list):
            raise ValueError('Native visual readset required')
        links = []
        for item in visual:
            if item.get('has_animator_link') is not True:
                continue
            link = {field: item[field] for field in ('animator_exists', 'blob_created', 'animation_count', 'clips')
                    if field in item}
            if not {'animator_exists', 'blob_created', 'animation_count', 'clips'} <= set(link):
                raise ValueError('Linked native animation payload is incomplete')
            clips = link['clips']
            if (not isinstance(clips, list) or len(clips) > 4096 or
                    link['animation_count'] != len(clips)):
                raise ValueError('Bounded native clip domain mismatch')
            for clip in clips:
                if (not isinstance(clip, dict) or set(clip) != {'frames', 'fps', 'speed'} or
                        any(type(clip[k]) is not int or clip[k] < 0 for k in ('frames', 'fps', 'speed')) or
                        any(clip[k] > 255 for k in ('fps', 'speed'))):
                    raise ValueError('Malformed native clip facts')
            links.append(link)
        projected.append({field: row[field] for field in ROW_FIELDS} |
                         {'linked_animation_inputs': links})
    return projected


def make_projection(capture_path, audit_path, keys_path, capture_root, expected_key_sha=EXPECTED_KEY_SHA):
    capture_path, capture_bytes = read_json(capture_path, MAX_CAPTURE_BYTES)
    audit_path, audit_bytes = read_json(audit_path, MAX_PROJECTION_BYTES)
    keys_path, keys_bytes = read_json(keys_path, 4096)
    raw = json.loads(capture_bytes)
    audit = json.loads(audit_bytes)
    keys = json.loads(keys_bytes)
    capture_desc = {'path': str(capture_path), 'sha256': sha(capture_bytes)}
    audit_desc = {'path': str(audit_path), 'sha256': sha(audit_bytes)}
    keys_desc = {'path': str(keys_path), 'sha256': sha(keys_bytes)}
    if (raw.get('schema_version') != 1 or raw.get('kind') != EXPECTED_KIND or
            raw.get('authority') != EXPECTED_AUTHORITY or raw.get('keys_sha256') != expected_key_sha or
            raw.get('selected_prefab_set_mode') not in ('ExplicitHashPinnedGenericSet', 'LegacyFixedSelected16') or
            any(raw.get(flag) is not False for flag in FALSE_FLAGS)):
        raise ValueError('S050 source-only native death capture scope/authority required')
    if (not isinstance(keys, list) or len(keys) != 16 or
            any(not isinstance(key, str) or not key for key in keys) or len(set(keys)) != 16 or
            sha(keys_bytes) != expected_key_sha):
        raise ValueError('Exact pinned selected-16 prefab key file required')
    if raw.get('keys_sha256') != expected_key_sha or keys_desc['sha256'] != expected_key_sha or raw.get('source_pins', {}).get(str(keys_path)) != keys_desc['sha256']:
        raise ValueError('Capture does not pin the supplied exact prefab key descriptor')
    pin_count = verify_source_pins(raw, capture_root)
    if (audit.get('kind') != 'S050DeathMechanicalReadsetAudit' or
            audit.get('status') != 'PassedForMechanicalReadsetOnly' or
            audit.get('readset') != capture_desc or
            audit.get('exact_pass_marker') != EXPECTED_MARKER or
            audit.get('source_pin_audit') != {'count': pin_count, 'mismatches': 0} or
            audit.get('prefab_key_set', {}).get('path') != str(keys_path) or
            audit.get('prefab_key_set', {}).get('sha256') != keys_desc['sha256'] or
            audit.get('prefab_key_set', {}).get('prefabs') != 16 or
            any(audit.get('flags', {}).get(flag) is not False for flag in FALSE_FLAGS)):
        raise ValueError('Current source-only mechanical audit receipt does not bind this capture')
    log_desc = audit.get('unity_log')
    if not isinstance(log_desc, dict) or set(log_desc) != {'path', 'source_path', 'sha256'}:
        raise ValueError('Pinned Unity owner log descriptor required')
    log_path, log_bytes = read_json(log_desc['path'], 32 * 1024 * 1024)
    if str(log_path) != log_desc['path'] or sha(log_bytes) != log_desc['sha256'] or EXPECTED_MARKER.encode() not in log_bytes:
        raise ValueError('Exact original owner pass marker absent from current pinned Unity log')
    cleanup = raw.get('native_cleanup_rows')
    if (not isinstance(cleanup, list) or len(cleanup) != 16 or
            [r.get('key') for r in cleanup] != keys or
            any(r.get('mission_lifecycle_hooks_executed') is not False or
                any(e.get('exists') is not False for e in r.get('after_original_parent_cleanup', []))
                for r in cleanup)):
        raise ValueError('Exact per-prefab original cleanup evidence required')
    rows = compact_rows(raw, keys)
    projection = {'schema_version': 1, 'kind': 'S050SourceDiagnosticDeathMechanicalProjection',
                  'authority': EXPECTED_AUTHORITY, 'source_capture': capture_desc,
                  'source_audit': audit_desc, 'prefab_key_set': keys_desc,
                  'keys_sha256': EXPECTED_KEY_SHA,
                  'full_mission_initialized': False, 'renderer_qualified': False,
                  'runtime_admitted': False, 'mission_ready': False,
                  'rows': rows}
    projection_bytes = (json.dumps(projection, indent=2, sort_keys=True) + '\n').encode()
    if not 1 <= len(projection_bytes) <= MAX_PROJECTION_BYTES:
        raise ValueError('Bounded compact S050 death projection required')
    receipt = {'schema_version': 1, 'kind': 'S050SourceDiagnosticDeathProjectionReceipt',
               'status': 'ProjectedSourceDiagnosticOnly', 'source_capture': capture_desc,
               'source_audit': audit_desc, 'prefab_key_set': keys_desc,
               'capture_root': str(Path(capture_root).resolve(strict=True)),
               'source_pin_count': pin_count, 'projection_bytes': len(projection_bytes),
               'projection_sha256': sha(projection_bytes), 'rows': 16,
               'full_mission_initialized': False, 'renderer_qualified': False,
               'runtime_admitted': False, 'mission_ready': False}
    return projection_bytes, receipt


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--capture', type=Path, required=True)
    parser.add_argument('--audit', type=Path, required=True)
    parser.add_argument('--keys', type=Path, required=True)
    parser.add_argument('--capture-root', type=Path, required=True)
    parser.add_argument('--output-dir', type=Path, required=True)
    args = parser.parse_args()
    output = args.output_dir.absolute()
    if output.exists() or not output.is_relative_to(Path(__file__).resolve().parents[4]):
        raise ValueError('Fresh repository-owned output directory required')
    projected, receipt = make_projection(args.capture, args.audit, args.keys, args.capture_root)
    output.mkdir(parents=True)
    projection_path = output / 'death-mechanical-projection.json'
    projection_path.write_bytes(projected)
    receipt['projection'] = {'path': str(projection_path), 'sha256': sha(projected)}
    (output / 'projection-receipt.json').write_text(json.dumps(receipt, indent=2, sort_keys=True) + '\n')
    print(json.dumps(receipt, sort_keys=True))


if __name__ == '__main__':
    main()
