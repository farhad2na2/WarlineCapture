#!/usr/bin/env python3
"""Audit and preserve an original-owner S050 mechanical death capture."""
import argparse
import hashlib
import json
from pathlib import Path

from project_s050_death_mechanical_readset import (
    EXPECTED_KEY_SHA, EXPECTED_MARKER, EXPECTED_KIND, EXPECTED_AUTHORITY,
    FALSE_FLAGS, MAX_CAPTURE_BYTES, MAX_PROJECTION_BYTES, read_json, sha,
    verify_source_pins,
)


def audit(capture, unity_log, keys_path, capture_root):
    capture_path, raw_bytes = read_json(capture, MAX_CAPTURE_BYTES)
    log_path, log_bytes = read_json(unity_log, 32 * 1024 * 1024)
    key_path, keys_bytes = read_json(keys_path, 4096)
    raw = json.loads(raw_bytes)
    keys = json.loads(keys_bytes)
    marker_count = log_bytes.count(EXPECTED_MARKER.encode())
    unity_version = next((line.split(':', 1)[1].strip() for line in log_bytes.decode('utf-8', errors='replace').splitlines()
                          if line.startswith('Unity Editor version:')), None)
    batch_mode = next((line.split(':', 1)[1].strip() for line in log_bytes.decode('utf-8', errors='replace').splitlines()
                       if line.startswith('Batch mode:')), None)
    if (raw.get('schema_version') != 1 or raw.get('kind') != EXPECTED_KIND or
            raw.get('authority') != EXPECTED_AUTHORITY or raw.get('keys_sha256') != EXPECTED_KEY_SHA or
            raw.get('selected_prefab_set_mode') != 'ExplicitHashPinnedGenericSet' or
            any(raw.get(flag) is not False for flag in FALSE_FLAGS)):
        raise ValueError('Exact selected-16 S050 mechanical source capture required')
    if (not isinstance(keys, list) or len(keys) != 16 or len(set(keys)) != 16 or
            any(not isinstance(k, str) or not k for k in keys) or sha(keys_bytes) != EXPECTED_KEY_SHA or
            raw.get('source_pins', {}).get(str(key_path)) != EXPECTED_KEY_SHA):
        raise ValueError('Capture must pin the current exact selected-16 key descriptor')
    pin_count = verify_source_pins(raw, capture_root)
    rows = raw.get('rows')
    if not isinstance(rows, list) or len(rows) != 16 or [r.get('key') for r in rows] != keys:
        raise ValueError('Exact selected-16 original Prepare rows required')
    for row in rows:
        if (row.get('actual_original_prepare_bits') != row.get('reversed_child_original_prepare_bits') or
                row.get('actual_original_prepare_bits') != row.get('original_fold_bits') or
                row.get('actual_original_prepare_bits') != row.get('unit_death_time_remaining_assignment_bits')):
            raise ValueError(f'Original Prepare / child order / fold evidence differs: {row.get("key")}')
        visual = row.get('visual_readset')
        if not isinstance(visual, list):
            raise ValueError('Native visual readset required')
        for link in visual:
            if link.get('has_animator_link') is True:
                clips = link.get('clips')
                if (not isinstance(clips, list) or len(clips) > 4096 or
                        link.get('animation_count') != len(clips) or
                        any(set(clip) != {'frames', 'fps', 'speed'} for clip in clips)):
                    raise ValueError('Bounded original linked animation clip facts required')
    cleanup = raw.get('native_cleanup_rows')
    if (not isinstance(cleanup, list) or len(cleanup) != 16 or [r.get('key') for r in cleanup] != keys or
            any(r.get('mission_lifecycle_hooks_executed') is not False or
                not r.get('after_original_parent_cleanup') or
                any(e.get('exists') is not False for e in r['after_original_parent_cleanup'])
                for r in cleanup)):
        raise ValueError('Exact per-prefab native cleanup evidence required')
    if marker_count != 1 or unity_version is None or batch_mode != 'NO':
        raise ValueError('Exactly one owner pass marker and GUI-licensed non-batch Unity log required')
    return {
        'schema_version': 1, 'kind': 'S050DeathMechanicalReadsetAudit',
        'status': 'PassedForMechanicalReadsetOnly',
        'unity_log': {'path': None, 'source_path': str(log_path), 'sha256': sha(log_bytes)},
        'readset': {'path': str(capture_path), 'sha256': sha(raw_bytes)},
        'unity': {'version': unity_version, 'batch_mode': batch_mode},
        'exact_pass_marker': EXPECTED_MARKER, 'exact_pass_marker_count': marker_count,
        'prefab_key_set': {'path': str(key_path), 'sha256': sha(keys_bytes),
                           'keys_sha256': raw['keys_sha256'], 'prefabs': len(keys)},
        'source_pin_audit': {'count': pin_count, 'mismatches': 0},
        'mechanical_rows': {'prefab_rows': len(rows), 'native_cleanup_rows': len(cleanup),
                            'original_prepare_rows': 16, 'child_permutation_rows': 16,
                            'all_original_prepare_bits_match_child_control_and_fold': True,
                            'all_entities_gone_after_parent_cleanup': True,
                            'mission_lifecycle_hooks_executed': False},
        'flags': {'full_mission_initialized': False, 'renderer_qualified': False, 'runtime_admitted': False},
        'scope': 'Original native death Prepare mechanical reference only; selected 16 prefabs, no scheduler, mission startup, renderer, or readiness claim.',
    }


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--capture', type=Path, required=True)
    parser.add_argument('--unity-log', type=Path, required=True)
    parser.add_argument('--keys', type=Path, required=True)
    parser.add_argument('--capture-root', type=Path, required=True)
    parser.add_argument('--output-dir', type=Path, required=True)
    args = parser.parse_args()
    output = args.output_dir.absolute()
    repo = Path(__file__).resolve().parents[4]
    if output.exists() or not output.is_relative_to(repo):
        raise ValueError('Fresh repository-owned audit directory required')
    receipt = audit(args.capture, args.unity_log, args.keys, args.capture_root)
    output.mkdir(parents=True)
    preserved_log = output / 'unity.log'
    preserved_log.write_bytes(Path(args.unity_log).read_bytes())
    receipt['unity_log']['path'] = str(preserved_log.resolve())
    (output / 'audit-receipt.json').write_text(json.dumps(receipt, indent=2, sort_keys=True) + '\n')
    print(json.dumps(receipt, sort_keys=True))


if __name__ == '__main__':
    main()
