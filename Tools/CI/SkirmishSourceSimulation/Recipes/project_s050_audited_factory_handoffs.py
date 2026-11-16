#!/usr/bin/env python3
"""Validate current S050 Unity-owner handoffs against the compiler context and emit schema-3 runtime projections."""
import argparse
import copy
import hashlib
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
CONTEXT_DEFAULT = ROOT / 'T Logs/SkirmishProduction/s050-source-factory-context-probe-20261107-r1/source-factory-context.json'
QUALIFICATION_DEFAULT = ROOT / 'T Logs/SkirmishProduction/s050-source-qualification-20261107-r1/qualification.json'
TEMPLATE_ROOT = ROOT / 'T Logs/SkirmishProduction/s050-derived-shared-factory-projections-20261107-r1'


def sha_bytes(raw):
    return hashlib.sha256(raw).hexdigest()


def sha(path):
    return sha_bytes(Path(path).read_bytes())


def descriptor(path):
    path = Path(path).resolve(strict=True)
    if path.is_symlink() or not path.is_file() or not path.is_relative_to(ROOT):
        raise ValueError(f'Repository-owned regular input required: {path}')
    return {'path': str(path), 'sha256': sha(path)}


def read(path):
    path = Path(path).resolve(strict=True)
    descriptor(path)
    return json.loads(path.read_bytes())


def need(value, message):
    if not value:
        raise ValueError(message)


def _expected_buildings(context):
    return [{'key': row['prefab']['key'], 'prefab': copy.deepcopy(row['prefab']),
             'fields': copy.deepcopy(row['fields'])}
            for row in context['derived_metadata']['buildings']]


def _expected_units(context):
    return [dict(copy.deepcopy(row), key=row['prefab']['key'])
            for row in context['derived_metadata']['units']]


def _owner_building_rows(owner):
    return [{'key': row['key'], 'prefab': copy.deepcopy(row['prefab']),
             'fields': copy.deepcopy(row['fields'])}
            for row in owner.get('definitions', [])]


def validate_context_and_owner_fields(context, context_desc, qualification,
                                      building_owner, unit_owner,
                                      building_template, unit_template,
                                      configured_template):
    """Pure field-level join; raises on stale target or any owner/context field drift."""
    target = context.get('target_context', {})
    need(context.get('schema_version') == 1 and
         context.get('kind') == 'OriginalCompilerDerivedNativeFactoryContext' and
         context.get('authority') == 'ActualSourceCompilerContext_ImmutableNativeFactoryFacts_NotNativeS050' and
         all(context.get(k) is False for k in ('native_S050_capture', 'world_allocated', 'mission_ready', 'runtime_loader_admitted')),
         'Current source-only S050 compiler context required')
    need(qualification.get('kind') == 'ExperimentalGroundConfigurationQualification' and
         qualification.get('runtime_backend') == 'TypedGroundS026V1' and
         qualification.get('compiler_context') == context_desc and
         qualification.get('mission_ready') is False and qualification.get('native_context_qualified') is False,
         'Exact source-only qualification/context pair required')

    for owner_name, owner in (('building', building_owner), ('unit', unit_owner)):
        selection = owner.get('compiler_selection') or {}
        expected = {
            'catalog_id': target.get('catalog_id'), 'seed': target.get('seed'),
            'size': target.get('size'), 'difficulty': target.get('difficulty'),
            'setup_hash': target.get('setup_hash'), 'operation_map_id': target.get('operation_map_id'),
            'layout_fingerprint': target.get('layout_binding_fingerprint'),
        }
        need(all(selection.get(k) == v for k, v in expected.items()),
             f'{owner_name} owner selection differs from exact compiler target')
        semantic = selection.get('source_content_hash')
        prefix = f"{target.get('definition_id')}.v{target.get('content_version')}/enemy-policy-v1/"
        need(isinstance(semantic, str) and re.fullmatch(re.escape(prefix) + r'[0-9a-f]{64}', semantic),
             f'{owner_name} owner semantic content hash lacks canonical definition/version identity')
        need(all(selection.get(k) is False for k in ('world_allocated', 'source_fit_certified', 'mission_ready', 'native_acceptance')),
             f'{owner_name} owner result contains an admission/readiness claim')
    need(building_owner['compiler_selection']['source_content_hash'] == unit_owner['compiler_selection']['source_content_hash'],
         'Unit/building owner semantic content hashes differ')

    expected_buildings = _expected_buildings(context)
    expected_units = _expected_units(context)
    expected_transports = copy.deepcopy(context['derived_metadata']['transports'])
    direct_buildings = _owner_building_rows(building_owner)
    need(len(building_owner.get('definitions', [])) == len(direct_buildings),
         'Every building owner row must expose exact key/prefab/fields')
    need(direct_buildings == expected_buildings == building_template.get('definitions'),
         'Audited building owner fields differ from compiler-context factory facts')
    need(unit_owner.get('units') == expected_units == unit_template.get('units'),
         'Audited unit owner fields differ from compiler-context factory facts')
    need(unit_owner.get('transports') == expected_transports == unit_template.get('transports'),
         'Audited transport fields differ from compiler-context factory facts')

    expected_configured = []
    for row in context['derived_metadata']['buildings']:
        expected_configured.append({
            'definition_index': row['index'], 'key': row['prefab']['key'], 'prefab': row['prefab'],
            'display_name': row['configured_entry']['fields']['DisplayName']['value'],
            'can_request': row['configured_entry']['fields']['CanRequest']['value'],
            'construction_catalog_index': None if row['construction_index'] < 0 else row['construction_index'],
            'scene_catalog_index': None if row['scene_index'] < 0 else row['scene_index'],
        })
    need(configured_template.get('rows') == expected_configured,
         'Configured-building template differs from current compiler-context factory facts')
    native_reference = context.get('native_factory_reference', {})
    native_reference_kind = native_reference.get('kind')
    native_reference_sha = native_reference.get('sha256')
    if native_reference_kind is None and isinstance(native_reference.get('path'), str):
        native_reference_path = Path(native_reference['path']).resolve(strict=True)
        need(descriptor(native_reference_path) == native_reference,
             'Pinned native factory reference changed')
        native_reference_kind = read(native_reference_path).get('kind')
    native_origin = context.get('native_origin', {})
    for name, template, kind in (
        ('building', building_template, 'CompilerContextDerivedSkirmishBuildingDefinitions'),
        ('unit', unit_template, 'CompilerContextDerivedSkirmishUnitProductionMetadata'),
        ('configured', configured_template, 'CompilerContextDerivedConfiguredBuildingMetadata')):
        template_origin = template.get('native_origin', {})
        origin_matches = (all(template_origin.get(k) == native_origin.get(k) for k in
                              ('catalog_id', 'size', 'difficulty', 'seed', 'setup_hash', 'metadata_key_sha256')) and
                          template_origin.get('kind') == native_reference_kind and
                          template_origin.get('artifact_sha256') == native_reference_sha)
        need(template.get('schema_version') == 3 and template.get('kind') == kind and
             template.get('metadata_compiler_context') == context_desc and
             template.get('catalog_id') == target.get('catalog_id') and
             template.get('seed') == target.get('seed') and
             template.get('setup_hash') == target.get('setup_hash') and
             origin_matches and
             all(template.get(k) is False for k in ('native_S050_capture', 'full_mission_initialized', 'native_visual_readiness')) and
             template.get('unsupported_values') == [],
             f'Exact current compiler-context {name} template header required')
    return {'building_definitions': expected_buildings, 'units': expected_units,
            'transports': expected_transports, 'configured_rows': expected_configured,
            'owner_semantic_content_hash': building_owner['compiler_selection']['source_content_hash']}


def validate_audit(owner_name, owner, owner_desc, audit, audit_desc, context_target,
                   author_desc, physical_desc, preset_desc):
    need(audit.get('kind') == 'S050CompilerSelectedPresetUnityOwnerAudit' and
         audit.get('owner') == owner_name and audit.get('status') == 'Passed',
         f'Passed {owner_name} Unity owner audit required')
    need(audit.get('source_pin_missing_count') == 0 and audit.get('source_pin_changed_count') == 0 and
         audit.get('source_pin_mismatches') == [] and audit.get('source_pin_count') == len(owner.get('source_pins', {})),
         f'{owner_name} audit/source pin census mismatch')
    for check, value in audit.get('checks', {}).items():
        if isinstance(value, bool):
            need(value is True, f'{owner_name} audit control failed: {check}')
    need(audit.get('compiler_selection', {}).get('result') == author_desc and
         audit.get('compiler_selection', {}).get('physical_input_request') == physical_desc and
         audit.get('physical_source', {}).get('request_path') == physical_desc['path'] and
         audit.get('physical_source', {}).get('request_sha256') == physical_desc['sha256'],
         f'{owner_name} audit compiler/physical selection differs')
    for key in ('catalog_id', 'seed', 'size', 'difficulty', 'setup_hash', 'operation_map_id'):
        expected = context_target.get(key)
        need(audit.get('compiler_selection', {}).get(key) == expected,
             f'{owner_name} audit target differs: {key}')
    need(audit.get('preset', {}).get('path') == preset_desc['path'] and
         audit.get('preset', {}).get('sha256') == preset_desc['sha256'] and
         audit.get('preset', {}).get('supply_day_seconds_from_pinned_source_graph') == 120 and
         audit.get('preset', {}).get('unity_field_value_parity_proven') is False,
         f'{owner_name} audit selected-preset identity/nonclaim differs')
    need(owner_desc['sha256'] == sha(owner_desc['path']) and audit_desc['sha256'] == sha(audit_desc['path']),
         f'{owner_name} owner/audit descriptor drift')
    pins = owner.get('source_pins')
    need(isinstance(pins, dict) and len(pins) > 0, f'{owner_name} handoff lacks source pin closure')
    for path, expected_sha in pins.items():
        p = Path(path)
        p = p if p.is_absolute() else ROOT / p
        need(not p.is_symlink() and p.is_file() and sha(p) == expected_sha,
             f'{owner_name} handoff source pin drift: {path}')
    output = audit.get('owner_output', {})
    if owner_name == 'building':
        need(output.get('definitions') == len(owner.get('definitions', [])) and
             output.get('unsupported_values') == 0 and output.get('full_mission_initialized') is False and
             output.get('native_visual_readiness') is False,
             'Building audit output census/nonclaims differ')
    else:
        need(output.get('units') == len(owner.get('units', [])) and
             output.get('transports') == len(owner.get('transports', [])) and
             output.get('unsupported_values') == 0 and output.get('source_preset_index') == 0 and
             output.get('full_mission_initialized') is False and output.get('native_visual_readiness') is False,
             'Unit audit output census/nonclaims differ')
    need(all(audit.get('nonclaims', {}).get(k) is False for k in
             ('world_allocated', 'runtime_admission', 'source_fit_certified', 'mission_ready', 'native_acceptance', 'unity_asset_field_value_parity_proven')),
         f'{owner_name} owner audit must retain all nonclaims')


def project(context_path, qualification_path, building_path, building_audit_path,
            unit_path, unit_audit_path, template_root, output):
    output = Path(output).absolute()
    if not output.is_relative_to(ROOT) or output.exists():
        raise ValueError('Fresh repository-owned projection output required')
    context_desc = descriptor(context_path); qualification_desc = descriptor(qualification_path)
    building_desc = descriptor(building_path); building_audit_desc = descriptor(building_audit_path)
    unit_desc = descriptor(unit_path); unit_audit_desc = descriptor(unit_audit_path)
    context = read(context_desc['path']); qualification = read(qualification_desc['path'])
    building = read(building_desc['path']); building_audit = read(building_audit_desc['path'])
    unit = read(unit_desc['path']); unit_audit = read(unit_audit_desc['path'])
    template_root = Path(template_root).resolve(strict=True)
    template_paths = {key: template_root / name for key, name in (
        ('building', 'building-definitions.json'), ('unit', 'unit-production-metadata.json'),
        ('configured', 'configured-metadata.json'))}
    templates = {key: read(path) for key, path in template_paths.items()}
    target = context.get('target_context', {})
    author_desc = building.get('compiler_selection', {}).get('result')
    physical_desc = building.get('compiler_selection', {}).get('physical_input_request')
    need(author_desc == unit.get('compiler_selection', {}).get('result') and
         physical_desc == unit.get('compiler_selection', {}).get('physical_input_request'),
         'Unit/building handoffs select different compiler result or physical request')
    author_desc = descriptor(author_desc['path']); physical_desc = descriptor(physical_desc['path'])
    need(building.get('compiler_selection', {}).get('result') == author_desc and
         unit.get('compiler_selection', {}).get('result') == author_desc,
         'Selected compiler result descriptor changed')
    author_result = read(author_desc['path'])
    need(author_result.get('kind') == 'SkirmishMissionAuthoringPreflightResult' and
         author_result.get('status') == 'ConfigurationCompiled' and
         author_result.get('catalog_id') == target.get('catalog_id') and
         author_result.get('seed') == target.get('seed') and
         author_result.get('setup_hash', author_result.get('resolved_setup', {}).get('setup_hash')) == target.get('setup_hash') and
         author_result.get('operation_map_id') == target.get('operation_map_id'),
         'Pinned compiler result differs from exact current context target')
    preset = building_audit.get('preset', {})
    preset_desc = descriptor(preset.get('path', ''))
    need(preset_desc['sha256'] == preset.get('sha256') and
         author_result.get('input_pins', {}).get(preset_desc['path'], {}).get('sha256') == preset_desc['sha256'],
         'Compiler result must pin the audited selected preset')
    physical_request = read(physical_desc['path'])
    need(physical_request.get('selection') == author_desc and physical_request.get('operation_map_id') == target.get('operation_map_id') and
         physical_request.get('source_fit_certified') is False and physical_request.get('mission_ready') is False,
         'Pinned physical request differs from current compiler selection')
    validate_audit('building', building, building_desc, building_audit, building_audit_desc,
                   target, author_desc, physical_desc, preset_desc)
    validate_audit('unit', unit, unit_desc, unit_audit, unit_audit_desc,
                   target, author_desc, physical_desc, preset_desc)
    comparisons = validate_context_and_owner_fields(context, context_desc, qualification,
        building, unit, templates['building'], templates['unit'], templates['configured'])

    output.mkdir(parents=True)
    # Current S050 context templates are exact schema-3 compiler projections.  The audited
    # direct owner rows were compared field-for-field to context-derived facts and templates;
    # preserving template bytes also preserves the configured projection's exact definition pin.
    projections = {}
    for key, filename in (('building', 'building-definitions.json'),
                          ('unit', 'unit-production-metadata.json'),
                          ('configured', 'configured-metadata.json')):
        source = template_paths[key]
        target_path = output / filename
        target_path.write_bytes(source.read_bytes())
        projections[key] = descriptor(target_path)
    need(projections['building']['sha256'] == templates['building'].get('_projection_sha256', projections['building']['sha256']),
         'Building projection serialization changed unexpectedly')
    provenance = {
        'kind': 'S050AuditedDirectFactoryOwnerProjectionInputs',
        'building_owner_handoff': building_desc, 'building_owner_audit': building_audit_desc,
        'unit_owner_handoff': unit_desc, 'unit_owner_audit': unit_audit_desc,
        'compiler_result': author_desc, 'physical_input_request': physical_desc,
        'selected_resource_preset': preset_desc,
        'compiler_target': copy.deepcopy(target),
        'owner_semantic_source_content_hash': comparisons['owner_semantic_content_hash'],
        'building_rows_field_equal_context': True, 'unit_rows_field_equal_context': True,
        'transport_rows_field_equal_context': True,
        'source_content_hash_role': 'Unity-owner setup.ContentHash semantic identity; not physical Campaign content hash',
        'unity_asset_field_value_parity_proven': False,
    }
    receipt = {
        'schema_version': 1, 'kind': 'S050AuditedCompilerContextMetadataProjection',
        'status': 'ProjectedCompilerContextSourceOnly', 'metadata_compiler_context': context_desc,
        'generator': descriptor(Path(__file__).resolve()),
        'qualification': qualification_desc, 'projection_templates': {k: descriptor(p) for k, p in template_paths.items()},
        'owner_provenance': provenance, 'comparisons': {k: v for k, v in comparisons.items() if k != 'owner_semantic_content_hash'},
        'projections': projections,
        'world_allocated': False, 'runtime_admission': False, 'source_fit_certified': False,
        'native_S050_capture': False, 'mission_ready': False, 'native_acceptance': False,
        'unity_asset_field_value_parity_proven': False,
        'scope': 'Schema-3 runtime projection files retain exact current compiler-context bytes because audited direct-owner fields equal the current context factory facts; receipt binds those owner inputs and comparisons.',
    }
    receipt_path = output / 'projection-receipt.json'
    receipt_path.write_text(json.dumps(receipt, indent=2) + '\n')
    receipt['receipt'] = descriptor(receipt_path)
    return receipt


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--context', type=Path, default=CONTEXT_DEFAULT)
    parser.add_argument('--qualification', type=Path, default=QUALIFICATION_DEFAULT)
    parser.add_argument('--building-owner', type=Path, required=True)
    parser.add_argument('--building-audit', type=Path, required=True)
    parser.add_argument('--unit-owner', type=Path, required=True)
    parser.add_argument('--unit-audit', type=Path, required=True)
    parser.add_argument('--template-root', type=Path, default=TEMPLATE_ROOT)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    report = project(args.context, args.qualification, args.building_owner, args.building_audit,
                     args.unit_owner, args.unit_audit, args.template_root, args.output)
    print(json.dumps({'status': report['status'], 'projections': report['projections'],
                      'mission_ready': report['mission_ready'], 'world_allocated': report['world_allocated']}))


if __name__ == '__main__':
    main()
