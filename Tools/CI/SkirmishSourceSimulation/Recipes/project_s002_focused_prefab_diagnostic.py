"""Project the retained failed S002 focused capture without upgrading its validation status."""
import argparse
import copy
import hashlib
import json
import re
from collections import Counter
from pathlib import Path

CAPTURE_SHA = '31b939d930cf5d9e6d8eac1b3325e7272756cedac916fb8d36a17734954a011e'
METADATA_SHA = '50a86f262e48c07808a3e52e2efb1b8bf8c744c00f1fd1c4fbf01b2e0c0502f1'
KEYS_SHA = 'f2d08edd3a4e495448d3f4df2ad6608d0180e6428be3d32f4844744dcbf23f09'
ALLOWED_UNSUPPORTED = {
    'Game.Components.UnitAttackImpactVfxReference': {'Prefab'},
    'Game.Components.UnitMuzzleFlashVfxReference': {'Prefab'},
}
FIELDS = ['schema_version', 'kind', 'authority', 'source_scenes', 'unity_version', 'template_namespace_id',
          'requested_prefab_keys', 'requested_prefab_keys_sha256', 'opening_prefabs', 'entities',
          'unsupported_values', 'replay_ready', 'fresh_world_allocator_exported', 'full_mission_initialized',
          'native_visual_readiness', 'geometry_exported', 'consumer_rule']


def sha(raw):
    return hashlib.sha256(raw).hexdigest()


def classify_unsupported(rows):
    pairs = []
    for row in rows:
        parts = row.get('path', '').split('/')
        if (len(parts) != 3 or parts[1] not in ALLOWED_UNSUPPORTED or parts[2] not in ALLOWED_UNSUPPORTED[parts[1]]
                or row.get('reason') != 'unity_object_ref_requires_bound_source_identity'
                or not row.get('type', '').startswith('Unity.Entities.UnityObjectRef`1[[UnityEngine.GameObject,')):
            raise ValueError('Unsupported required template value; only exact consumer-omitted VFX references are accepted')
        pairs.append((parts[0], parts[1], parts[2]))
    counts = Counter((entity, component) for entity, component, _ in pairs)
    entities = {entity for entity, _, _ in pairs}
    if (len(rows) != 14 or len(entities) != 7 or len(pairs) != len(set(pairs))
            or any(counts[(entity, component)] != 1 for entity in entities for component in ALLOWED_UNSUPPORTED)):
        raise ValueError('Exact S002 unsupported presentation omission set required')
    return sorted(entities)


def verify_pins(root, pins):
    if not isinstance(pins, dict) or len(pins) != 7965:
        raise ValueError('Exact captured S002 focused source pin census required')
    for raw_path, digest in pins.items():
        path = Path(raw_path)
        path = path if path.is_absolute() else root / path
        if path.is_symlink() or not re.fullmatch('[0-9a-f]{64}', digest):
            raise ValueError('Regular hash-bound S002 source pin required: ' + raw_path)
        resolved = path.resolve(strict=True)
        if not resolved.is_relative_to(root) and not resolved.is_relative_to(Path('/private/tmp')):
            raise ValueError('S002 focused capture pin escaped project and explicit temporary evidence roots')
        if not resolved.is_file() or sha(resolved.read_bytes()) != digest:
            raise ValueError('S002 focused capture source pin changed: ' + raw_path)


def project(capture_path, log_path, wrapper_exit, metadata_path, key_path, output_path, root):
    capture_path, log_path = Path(capture_path).resolve(strict=True), Path(log_path).resolve(strict=True)
    metadata_path, key_path = Path(metadata_path).resolve(strict=True), Path(key_path).resolve(strict=True)
    root, output = Path(root).resolve(strict=True), Path(output_path).absolute()
    raw, log = capture_path.read_bytes(), log_path.read_text(encoding='utf-8', errors='strict')
    metadata_raw, keys_raw = metadata_path.read_bytes(), key_path.read_bytes()
    if sha(raw) != CAPTURE_SHA or sha(metadata_raw) != METADATA_SHA or sha(keys_raw) != KEYS_SHA:
        raise ValueError('Exact retained failed S002 source artifacts required')
    if wrapper_exit != 0 or '[SkirmishFocusedPrefabReferenceData] result=Passed' in log:
        raise ValueError('This diagnostic is only for the retained no-marker failed capture')
    if ('Game.Editor.SkirmishBakedActorSnapshotExporter.RunFocusedPrefabReferenceData' not in log
            or 'Focused prefab reference contains unsupported values: 14; diagnostic artifact retained' not in log):
        raise ValueError('Exact failed checked-exporter log required')
    capture, metadata, keys = json.loads(raw), json.loads(metadata_raw), json.loads(keys_raw)
    expected_keys = [row['prefab']['key'] for row in metadata['contexts'][0]['metadata']['units']]
    if (capture.get('schema_version') != 1 or capture.get('kind') != 'AuthoredSkirmishPrefabReferenceData'
            or capture.get('authority') != 'ActualSourceAssetsAndNativeBakers_NotMissionState'
            or len(capture.get('opening_prefabs', {})) != 12 or len(capture.get('entities', [])) != 352
            or capture.get('requested_prefab_keys') != expected_keys or keys != expected_keys
            or capture.get('requested_prefab_keys_sha256') != KEYS_SHA
            or capture.get('source_pins', {}).get(str(key_path)) != KEYS_SHA
            or capture.get('replay_ready') is not False or capture.get('full_mission_initialized') is not False
            or capture.get('native_visual_readiness') is not False or capture.get('geometry_exported') is not False
            or capture.get('fresh_world_allocator_exported') is not False):
        raise ValueError('Exact failed S002 12-prefab source-only capture required')
    namespace = capture.get('template_namespace_id', '')
    if not re.fullmatch('[0-9a-f]{32}', namespace):
        raise ValueError('Single captured S002 prefab namespace required')
    affected = classify_unsupported(capture.get('unsupported_values', []))
    verify_pins(root, capture['source_pins'])
    if output.exists():
        raise ValueError('Fresh isolated projection directory required')
    output.mkdir(parents=True)
    domain = {field: copy.deepcopy(capture[field]) for field in FIELDS}
    domain['source_reference_sha256'] = CAPTURE_SHA
    domain['derived_read_domain'] = 'ExactTypedPrefabClosureOnly'
    slice_path = output / 's002-focused-template-domain.json'
    slice_raw = (json.dumps(domain, indent=2) + '\n').encode()
    slice_path.write_bytes(slice_raw)
    manifest = {
        'schema_version': 1, 'kind': 'PinnedExactTemplateDomainProjection',
        'source_path': str(capture_path), 'source_sha256': CAPTURE_SHA, 'source_bytes': len(raw),
        'slice_path': str(slice_path), 'slice_sha256': sha(slice_raw), 'slice_bytes': len(slice_raw),
        'selected_fields': FIELDS, 'root_count': 12, 'entity_count': 352,
        'unsupported_presentation_omission_count': 14, 'unsupported_presentation_omission_entities': affected,
        'capture_validation_status': 'Failed', 'capture_pass_marker_present': False,
    }
    manifest_path = output / 'template-slice-manifest.json'
    manifest_raw = (json.dumps(manifest, indent=2) + '\n').encode()
    manifest_path.write_bytes(manifest_raw)
    descriptor = {
        'path': str(slice_path), 'sha256': sha(slice_raw),
        'source_reference_path': str(capture_path), 'source_reference_sha256': CAPTURE_SHA,
        'projection_manifest_path': str(manifest_path), 'projection_manifest_sha256': sha(manifest_raw),
        'capture_validation_status': 'Failed', 'capture_pass_marker_present': False,
        'capture_sha256': CAPTURE_SHA, 'unsupported_presentation_omission_count': 14,
        'consumer_qualification_scope': 'PresentationOnly diagnostics; not exporter validation or mission readiness',
    }
    descriptor_path = output / 'reference-template-descriptor.json'
    descriptor_path.write_text(json.dumps(descriptor, indent=2) + '\n')
    receipt = {
        'kind': 'S002FocusedPrefabConsumerPresentationDiagnostic',
        'capture_validation_status': 'Failed', 'capture_pass_marker_present': False,
        'capture_wrapper_exit_code': 0, 'capture_sha256': CAPTURE_SHA, 'capture_log_sha256': sha(log.encode()),
        'metadata_capture_sha256': METADATA_SHA, 'keyset_sha256': KEYS_SHA,
        'source_pin_count': 7965, 'source_pins_current': True,
        'unsupported_presentation_omission_count': 14, 'affected_entity_count': len(affected),
        'runtime_import_not_run': True, 'full_mission_initialized': False,
        'native_visual_readiness': False, 'replay_ready': False, 'mission_ready': False,
        'descriptor': descriptor, 'descriptor_path': str(descriptor_path),
        'scope': 'Exact existing importer PresentationOnly whitelist only; not exporter validation or readiness',
    }
    receipt_path = output / 'diagnostic-receipt.json'
    receipt_path.write_text(json.dumps(receipt, indent=2) + '\n')
    if capture_path.read_bytes() != raw or metadata_path.read_bytes() != metadata_raw or key_path.read_bytes() != keys_raw:
        raise ValueError('S002 focused projection input changed during projection')
    return receipt


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--capture', required=True)
    parser.add_argument('--log', required=True)
    parser.add_argument('--wrapper-exit', type=int, required=True)
    parser.add_argument('--metadata', required=True)
    parser.add_argument('--keys', required=True)
    parser.add_argument('--output', required=True)
    parser.add_argument('--root', default=str(Path(__file__).resolve().parents[4]))
    args = parser.parse_args()
    result = project(args.capture, args.log, args.wrapper_exit, args.metadata, args.keys, args.output, args.root)
    print(json.dumps(result, indent=2))
