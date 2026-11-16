#!/usr/bin/env python3
"""Audit the retained isolated S073 source capture without importing a world."""
import argparse
import collections
import hashlib
import json
import re
from pathlib import Path


# Exact source surface reviewed for this narrow interpretation. Any change in a
# file that declares, copies, authors, serializes, or consumes these references
# invalidates the audit until the boundary is reviewed again.
SOURCE_PINS = {
    'Assets/Game/Scripts/Components/AirMissileComponents.cs': '40a4c56e6b72879aee21478b6ad2f5fdcbf9923be048059e2ecba92eacc53b4b',
    'Assets/Game/Scripts/Components/GroundMissileComponents.cs': '60b62c7995a1e7e45c5d94196d759c7a01a8c368fe728e8b94000c796978cb9b',
    'Assets/Game/Scripts/Components/UnitCombatComponents.cs': 'ee997571f3c93adfeeb0d39b49e13d48df34c954a2d5f8cbce9c936c18d1e5dc',
    'Assets/Game/Scripts/Authorings/UnitGridAuthoring.cs': 'cf5f9decee267a41f823e37c6dd0573debfadf2a4c5e140d84af475d1b998f4b',
    'Assets/Game/Scripts/Authorings/UnitGridAuthoring.BakerAirMissile.cs': '4710aef9e63a053638fbfac479c1f0c700aa5b699d59cb0d5e5610b488f5770a',
    'Assets/Game/Scripts/Rendering/UnitAttackTracePresentationSystemHelper.cs': '5cd4d8ae303cc64983b6f654a3bb85de55b74e1cc658da2059a95fd1dd5e88c8',
    'Assets/Game/Scripts/Systems/AirMissileLauncherSystems.cs': '8b8d90a54131b6b9fdecc518b5f9414b3db0859375465f09fad14410fc985691',
    'Assets/Game/Scripts/Systems/UnitAttackSystem.cs': '02641b2f4119d84c6a1324c41734f7e3d83612fd227c416f7a58742df77d9545',
    'Assets/Game/Scripts/Systems/BuildingRuntimeEntityCompositionSystemHelper.cs': 'd07d42abbddeaba089bfee3061614a685ea08e66b4d16aa03fd572d63cf3002b',
    'Assets/Game/Scripts/Systems/Support/SupportAirValidationUtilitySystemHelper.cs': '372098edc92698282c704462ed4a13c8c747980411c75efda9c5998e088c04cb',
    'Assets/Game/Scripts/Systems/GroundMissileLauncherSystems.cs': '6ea5676b0d7fcec34483209a21d36336c147dbfd3a4681c65b45618dbc06314b',
    'Assets/Game/Scripts/Editor/MatchRuntimeShellSmokeValidation.cs': '31b7b702be2478e10b8fb85eab11722819fadadd4f8211cadbca3b7110c5777d',
    'Assets/Game/Scripts/Editor/MatchGcAllocationCallstackCapture.cs': 'd58d7fe81bf924862bdca784907e1513575f909159a37fdf719aaf02ce003bad',
    'Assets/Game/Scripts/Editor/AirMissileLauncherValidationRunner.cs': '52026ee0ee8d27128d2ca80fc19b1ae3c1dc2212b5b11a50c0af6c7f5cd9e8c7',
    'Assets/Game/Scripts/Editor/TacticalFollowAttackCinematicPlayModeValidation.cs': 'a5903f5c0783e623ae32b68905e592f14a05c0c0cc13c70d2ec453cf563b3820',
    'Assets/Game/Scripts/Editor/Skirmish/SkirmishBakedActorSnapshotExporter.S073PrefabData.cs': 'fc7dad0efb27dc9f1f9835bb25f94df06271c9b64547f6e69fcf1f7acd02efa6',
    'Assets/Game/Scripts/Editor/Skirmish/SkirmishBakedActorSnapshotExporter.GroundMissileReference.cs': 'bd5df88923413f575de1dca7aa7d91aa341eb6b20313ee57396c86a98eb4b301',
    'Assets/Game/Scripts/Systems/UnitAttackVfxSystems.cs': 'bf6a1fe3b3b54d69f326e2d77d5052b08fb9555a58d276ca0ce2b791aedbd4f7',
    'Assets/Game/Scripts/Systems/TacticalFollowAttackCinematicSystem.cs': 'd38cf9b474f0560051df6c41be575072c4bb22c791e6c38f96a730764fa4c67a',
    'Assets/Game/Scripts/Systems/BuildingDefenseAttackSystem.cs': '70662ecc2a90de119652789a1025f81d45e6cd05e0c10119e11616f3d85c9668',
    'Assets/Game/Scripts/Systems/Support/SupportFlightSystem.cs': 'c69bdff5ed70c41598fdf483e01d99a9d356a1097c65fe6300fa66768b23d94b',
    'Assets/Game/Scripts/Editor/Skirmish/SkirmishBakedActorSnapshotExporter.S073StreamedWorld.cs': 'f278e4383dfb4ae3ad3f00d7b1da4cedce75625cbdceeaff7bdf60d9d9614346',
}

UNRESOLVED_TYPE = ('Unity.Entities.UnityObjectRef`1[[UnityEngine.GameObject, '
                   'UnityEngine.CoreModule, Version=0.0.0.0, Culture=neutral, '
                   'PublicKeyToken=null]]')
ALLOWED_UNRESOLVED_FIELDS = {
    'Game.Components.UnitAttackImpactVfxReference': {'Prefab'},
    'Game.Components.UnitMuzzleFlashVfxReference': {'Prefab'},
    'Game.Components.AirMissileLauncherVfxReferenceComponent': {
        'AirTargetImpactPrefab', 'AirburstExplosionPrefab', 'InterceptExplosionPrefab',
        'LaunchFlashPrefab', 'LaunchSmokePrefab', 'MissileTrailPrefab', 'MissileVisualPrefab'},
    'Game.Components.GroundMissileLauncherVfxReferenceComponent': {
        'ImpactExplosionPrefab', 'ImpactSmokePrefab', 'LauncherBackfirePrefab', 'RocketTrailPrefab'},
}


def digest(path):
    h = hashlib.sha256()
    with path.open('rb') as source:
        for block in iter(lambda: source.read(1024 * 1024), b''):
            h.update(block)
    return h.hexdigest()


def require(condition, message):
    if not condition:
        raise ValueError(message)


def audit_simulation_payload(capture, receipt, repo_root):
    """Classify captured unresolved refs; do not infer complete simulation payload."""
    require(digest(capture) == receipt['capture_sha256'], 'Capture hash differs from retained receipt')
    require(receipt.get('capture_source_sha256') == SOURCE_PINS[
        'Assets/Game/Scripts/Editor/Skirmish/SkirmishBakedActorSnapshotExporter.S073StreamedWorld.cs'],
        'Capture implementation source pin differs from reviewed source')

    actual_names = set()
    for path, expected in SOURCE_PINS.items():
        source = repo_root / path
        require(source.is_file() and digest(source) == expected, f'Reviewed source drift: {path}')
        actual_names.add(path)
    # Any newly added or moved reference consumer is outside the reviewed set.
    names = '|'.join(re.escape(name) for name in (
        *(type_name.rsplit('.', 1)[-1] for type_name in ALLOWED_UNRESOLVED_FIELDS),
        'UnitAttackVfxRequest', 'CombatGameObjectVfxRequest'))
    matches = set()
    for source in (repo_root / 'Assets/Game/Scripts').rglob('*.cs'):
        text = source.read_text(encoding='utf-8')
        if re.search(names, text):
            matches.add(source.relative_to(repo_root).as_posix())
    require(matches == actual_names - {
        'Assets/Game/Scripts/Editor/Skirmish/SkirmishBakedActorSnapshotExporter.S073StreamedWorld.cs'},
        'Reference consumer/source declaration set changed')

    data = json.loads(capture.read_text())
    require(data.get('kind') == 'S073IsolatedStreamedSourceWorldCapture' and
            data.get('schema_version') == 1 and
            data.get('scope') == 'IsolatedPreGameSource_NotLiveSessionOrReadiness' and
            data.get('scene_guid') == 'c00140f2e94a04c3084c8dcb0c18cbd0' and
            data.get('scene_sha256') == receipt.get('scene_sha256') and
            data.get('capture_domain') ==
            'Game.Components, Unity.Transforms and linked/prefab/disabled/simulate components' and
            data.get('closure_complete') is False and
            data.get('live_s073_session_observed') is False and
            data.get('physical_source_validated') is False and
            data.get('scheduler_admitted') is False and data.get('mission_ready') is False,
            'Exact isolated pre-game capture scope required')
    by_id = {row['id']: row for row in data['entities']}
    allowed_paths = set()
    for row in data['unsupported_values']:
        parts = row.get('path', '').split('/')
        require(len(parts) == 3 and parts[0] in by_id, 'Malformed unresolved reference path')
        entity_id, component_type, field = parts
        require(component_type in ALLOWED_UNRESOLVED_FIELDS and
                field in ALLOWED_UNRESOLVED_FIELDS[component_type],
                f'Unknown or gameplay-relevant unresolved field: {row.get("path")}')
        require(row.get('reason') == 'unity_object_ref_requires_bound_source_identity' and
                row.get('type') == UNRESOLVED_TYPE, 'Unresolved field codec/type changed')
        component = by_id[entity_id]['components'].get(component_type)
        require(component is not None and component.get('present') is True and
                component.get('buffer') is False, 'Unresolved reference component envelope changed')
        envelope = component.get('value', {})
        field_value = envelope.get('fields', {}).get(f'{component_type}::{field}')
        require(envelope.get('codec') == 'struct-fields-v1' and
                envelope.get('type') == component_type and
                field_value == {'codec': 'unsupported-v1', 'type': UNRESOLVED_TYPE,
                                'reason': 'unity_object_ref_requires_bound_source_identity'},
                'Unresolved field does not match captured component payload')
        require(row['path'] not in allowed_paths, 'Duplicate unresolved reference path')
        allowed_paths.add(row['path'])

    # Every unsupported codec in the retained payload must be represented by
    # exactly one allowed path. No gameplay field can silently disappear.
    encoded_paths = set()
    for entity in data['entities']:
        for component_type, component in entity['components'].items():
            envelope = component.get('value', {})
            fields = envelope.get('fields', {}) if isinstance(envelope, dict) else {}
            if isinstance(fields, dict):
                for key, field_value in fields.items():
                    if isinstance(field_value, dict) and field_value.get('codec') == 'unsupported-v1':
                        require(key.startswith(component_type + '::'),
                                'Unsupported field has no source field identity')
                        encoded_paths.add(f'{entity["id"]}/{component_type}/{key.split("::", 1)[1]}')
            stack = [envelope]
            while stack:
                current = stack.pop()
                if isinstance(current, dict):
                    if current.get('codec') == 'unsupported-v1':
                        require(any(current is field_value for field_value in fields.values()),
                                'Nested or unqualified unsupported field is outside the audited allowlist')
                    stack.extend(current.values())
                elif isinstance(current, list):
                    stack.extend(current)
    require(encoded_paths == allowed_paths, 'Payload contains unreported or extra unsupported fields')
    require(len(allowed_paths) == 71 and len(data['unsupported_values']) == 71,
            'Retained unsupported reference count changed')
    unresolved_by_component = collections.Counter(path.split('/')[1] for path in allowed_paths)
    return {
        'simulation_payload_scope': 'CapturedUnsupportedObjectRefs_ClassifiedOnly',
        'captured_unsupported_refs_classified_as_presentation': True,
        'simulation_payload_complete': False,
        'unresolved_presentation_reference_count': len(allowed_paths),
        'unresolved_presentation_references_by_component': dict(sorted(unresolved_by_component.items())),
        'allowed_presentation_reference_fields': {name: sorted(fields)
                                                  for name, fields in sorted(ALLOWED_UNRESOLVED_FIELDS.items())},
        'source_consumer_audit': 'ExactPinnedFilesAndConsumerSet',
        'full_source_authority_closure_complete': False,
        'import_admitted': False,
        'mission_ready': False,
    }


def audit(capture, receipt):
    require(digest(capture) == receipt['capture_sha256'], 'Capture hash differs from retained receipt')
    require(receipt['wrapper_exit_code'] == 0 and receipt['required_marker_present'] is True,
            'Checked Unity capture receipt required')
    with capture.open() as source:
        data = json.load(source)
    require(data['kind'] == 'S073IsolatedStreamedSourceWorldCapture' and data['schema_version'] == 1 and
            data['scene_guid'] == 'c00140f2e94a04c3084c8dcb0c18cbd0' and
            data['scene_sha256'] == receipt['scene_sha256'] and
            data['closure_complete'] is False and data['live_s073_session_observed'] is False and
            data['physical_source_validated'] is False and data['scheduler_admitted'] is False and
            data['mission_ready'] is False, 'Exact scoped isolated source capture required')
    rows = data['entities']
    ids = [row['id'] for row in rows]
    require(len(ids) == len(set(ids)) and all(re.fullmatch(r'[1-9][0-9]*:[1-9][0-9]*', v) for v in ids),
            'Unique valid source entity identities required')
    known = set(ids)
    roots = data['root_entity_ids']
    require(len(roots) == len(set(roots)) == data['building_faction_query_count'] and set(roots) <= known,
            'Unique complete root identity list required')
    by_id = {row['id']: row for row in rows}
    required = {'Game.Components.OperationMapBuildingComponent', 'Game.Components.Faction',
                'Game.Components.OperationMapBuildingProductionPrefab'}
    require(all(required <= by_id[root]['components'].keys() for root in roots), 'Root component domain changed')
    references = 0
    stack = [rows]
    while stack:
        value = stack.pop()
        if isinstance(value, dict):
            if value.get('codec') == 'entity-v1':
                references += 1
                require(value.get('id') == '0:0' or value.get('id') in known,
                        'Captured entity reference is outside the serialized graph')
            stack.extend(value.values())
        elif isinstance(value, list):
            stack.extend(value)
    unsupported = collections.Counter(row['reason'] for row in data['unsupported_values'])
    production = collections.Counter()
    for root in roots:
        component = by_id[root]['components']['Game.Components.OperationMapBuildingProductionPrefab']
        envelope = component['value']
        require(component['buffer'] is True and envelope['codec'] == 'template-buffer-values-v1' and
                envelope['element_type'] == 'Game.Components.OperationMapBuildingProductionPrefab',
                'Original production buffer envelope required')
        production[len(envelope['items'])] += 1
    return {'kind': 'S073StreamedSourceGraphAudit', 'status': 'GraphAudited_NotImportAdmission',
            'capture_sha256': receipt['capture_sha256'], 'roots': len(roots), 'entities': len(rows),
            'component_types': len({name for row in rows for name in row['components']}),
            'encoded_entity_references': references, 'unresolved_encoded_entity_references': 0,
            'unsupported_reasons': dict(unsupported), 'production_buffer_lengths': dict(sorted(production.items())),
            'entity_reference_scope': 'Serialized included fields only; excluded engine data and object refs not proved',
            'source_authority_closure_complete': False, 'import_admitted': False, 'mission_ready': False}


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--capture', required=True, type=Path)
    parser.add_argument('--receipt', required=True, type=Path)
    parser.add_argument('--output', required=True, type=Path)
    args = parser.parse_args()
    result = audit(args.capture, json.loads(args.receipt.read_text()))
    result.update(audit_simulation_payload(args.capture, json.loads(args.receipt.read_text()),
                                           Path(__file__).resolve().parents[4]))
    with args.output.open('x') as output:
        json.dump(result, output, indent=2)
        output.write('\n')
    print(json.dumps(result, separators=(',', ':')))


if __name__ == '__main__':
    main()
