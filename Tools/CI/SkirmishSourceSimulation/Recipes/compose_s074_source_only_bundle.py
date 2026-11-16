#!/usr/bin/env python3
"""Compose an S074 source-host envelope from pinned current owners; never native readiness."""
import argparse
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
R9 = ROOT / 'T Logs/SkirmishProduction/s074-source-factory-replay-20261107-r9'
GRAPH = ROOT / 'T Logs/SkirmishProduction/five-mission-pilot-20261106-T1/s074-industrial-source-graph-r1/source-config-graph.json'
AUTHORING = R9 / 'exporter/s074/authoring-request.json'
CONTEXT = R9 / 'exporter/s074/source-factory-context.json'
QUALIFICATION = R9 / 'qualification/configuration-qualification.json'
PACKAGE = ROOT / 'T Logs/SkirmishProduction/s026-source-map-bundle-20261107-r2/map-package.json'
TEMPLATES = ROOT / 'T Logs/SkirmishProduction/s074-focused-prefab-projection-20261107-r1/reference-template-descriptor.json'
DEATH = ROOT / 'Logs/SkirmishProduction/s074-owner-data-capture-20261107-r1/death-16/death-mechanical-readset.json'
DEATH_AUDIT = ROOT / 'T Logs/SkirmishProduction/s074-owner-data-capture-inputs-20261107-r1/death-readset-audit-receipt.json'
ROTATION = ROOT / 'Logs/SkirmishProduction/s073-operation-map-rotation-capture-20261101-r4/rotation-reference.json'
CLEARANCE = ROOT / 'T Logs/SkirmishProduction/source-map-clearance-host-20261107-r3/valid-projection.json'
TEXT = Path('/Volumes/T7/Projects/WarlineCapture/Logs/private-candidates/s049-physical-export-qualified-20261028-r1/project/Logs/SkirmishProduction/s049-command-text-r1/command-text.json')
TEXT_CAPTURE_ROOT = TEXT.parents[3]
LOGICAL = ROOT / 'Assets/Game/Configs/Skirmish/IndustrialBasin/OperationMap_IndustrialBasin.asset'
CONSTRUCTION = ROOT / 'Assets/Game/Configs/Skirmish/IndustrialBasin/Construction.asset'
RESOURCE_PRESET = ROOT / 'Assets/Game/Resources/SkirmishIndustrialBasin.asset'
CATALOG = ROOT / 'Assets/Game/Resources/SkirmishBattleCatalog.asset'


def digest(raw):
    return hashlib.sha256(raw).hexdigest()


def pin(path):
    path = Path(path).resolve(strict=True)
    if path.is_symlink() or not path.is_file():
        raise ValueError(f'Canonical file required: {path}')
    raw = path.read_bytes()
    if not raw:
        raise ValueError(f'Empty owner input: {path}')
    return {'path': str(path), 'sha256': digest(raw)}


def read(path):
    return json.loads(Path(path).read_bytes())


def descriptor_from(row):
    if not isinstance(row, dict) or set(row) != {'path', 'sha256'}:
        raise ValueError('Exact path/SHA owner descriptor required')
    result = pin(row['path'])
    if result['sha256'] != row['sha256']:
        raise ValueError(f'Owner descriptor drift: {row["path"]}')
    return result


def validate_owner_setup_matrix(author, candidate_receipt, selected):
    """Bind the matrix bytes consumed by the original S074 source owner."""
    owner_descriptor = descriptor_from(author.get('setup_matrix'))
    source_inputs = candidate_receipt.get('source_inputs_sha256')
    if (candidate_receipt.get('status') != 'PassedSourceOwnerCompile' or
            not isinstance(source_inputs, dict) or source_inputs.get(owner_descriptor['path']) != owner_descriptor['sha256']):
        raise ValueError('S074 source-owner compile receipt does not pin the authoring-request setup matrix')
    if selected != owner_descriptor:
        raise ValueError('Effective setup matrix must be the exact original S074 authoring-request descriptor')
    return owner_descriptor


def validate_native_factory_origin(context, metadata, origin):
    """Carry the exact factory metadata/origin pair exported by the S074 context."""
    metadata_descriptor = descriptor_from(context.get('native_factory_reference'))
    origin_descriptor = descriptor_from(context.get('native_origin_descriptor'))
    if metadata != metadata_descriptor or origin != origin_descriptor:
        raise ValueError('Native metadata descriptors must match the exact S074 compiler context')
    origin_artifact = read(origin_descriptor['path'])
    if (origin_artifact.get('schema_version') != 1 or
            origin_artifact.get('kind') != 'QualifiedNativeMetadataOrigin' or
            origin_artifact.get('native_metadata') != metadata_descriptor):
        raise ValueError('Exact r9 native factory reference and qualified origin pair required')
    return metadata_descriptor, origin_descriptor


def validate_logical_binding_projection(wrapper, receipt, wrapper_path, source_binding_path, context_path, target):
    context_descriptor = pin(context_path)
    wrapper_path = Path(wrapper_path).resolve(strict=True)
    source_binding_path = Path(source_binding_path).resolve(strict=True)
    if (wrapper.get('schema_version') != 1 or
            wrapper.get('kind') != 'OriginalCompilerDeclaredLogicalBinding' or
            wrapper.get('metadata_compiler_context') != context_descriptor or
            wrapper.get('target_context') != target or
            wrapper.get('native_capture') is not False or
            wrapper.get('source_fit_certified') is not False or
            wrapper.get('mission_ready') is not False or
            wrapper.get('portable_binding', {}).get('kind') != 'DeclaredOriginalLogicalBindingFieldsWithInlineViews' or
            wrapper.get('portable_binding', {}).get('float_encoding') != 'IEEE754_float32_hex_bits' or
            wrapper.get('portable_binding', {}).get('unity_object_ids_exported') is not False or
            wrapper.get('portable_binding', {}).get('fields', {}).get('CatalogId') != 'S074'):
        raise ValueError('Original-codec S074 logical binding envelope/context mismatch')
    raw_source_binding = wrapper.get('source_owner_binding')
    retained_source_binding = pin(source_binding_path)
    if (not isinstance(raw_source_binding, dict) or set(raw_source_binding) != {'path', 'sha256'} or
            pin(raw_source_binding['path']) != raw_source_binding or
            raw_source_binding.get('sha256') != retained_source_binding['sha256']):
        raise ValueError('Fresh compiler-authored source binding bytes differ from retained S074 owner')
    owner_replay_descriptor = receipt.get('source_owner_replay')
    if (not isinstance(owner_replay_descriptor, dict) or set(owner_replay_descriptor) != {'path', 'sha256'} or
            pin(owner_replay_descriptor['path']) != owner_replay_descriptor):
        raise ValueError('Exact source-owner replay receipt pin required')
    owner_replay = read(owner_replay_descriptor['path'])
    if (owner_replay.get('status') != 'PassedSourceOwnerCompile' or
            owner_replay.get('binding_sha256') != raw_source_binding['sha256'] or
            owner_replay.get('world_allocated') is not False or owner_replay.get('mission_ready') is not False):
        raise ValueError('Fresh original S074 source-owner replay receipt required')
    if (
            receipt.get('kind') != 'S074OriginalCompilerDeclaredLogicalBindingPortableProjection' or
            receipt.get('status') != 'ProjectedSourceOnly' or
            receipt.get('compiler_context') != context_descriptor or
            receipt.get('logical_binding') != pin(wrapper_path) or
            receipt.get('source_owner_binding') != raw_source_binding or
            receipt.get('original_reader_decode_and_compiler_roundtrip') != 'pending-source-run-preflight'):
        raise ValueError('Fresh original-codec S074 portable projection receipt required')
    return {'logical_binding_projection': pin(wrapper_path), 'logical_binding_owner': raw_source_binding}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output-dir', type=Path, required=True)
    parser.add_argument('--logical-binding-projection', type=Path, required=True,
                        help='Fresh output from project_s074_declared_logical_binding.py')
    args = parser.parse_args()
    out = args.output_dir.absolute()
    if not out.is_relative_to(ROOT) or out.exists():
        raise ValueError('Fresh repository-owned T output directory required')

    author = read(AUTHORING)
    graph = read(GRAPH)
    context = read(CONTEXT)
    qualification = read(QUALIFICATION)
    package = read(PACKAGE)
    death_audit = read(DEATH_AUDIT)
    compiler_target = context['target_context']
    if (author.get('catalog_id'), author.get('seed'), author.get('size'), author.get('difficulty')) != ('S074', 104803, 'Standard', 'Regular'):
        raise ValueError('Original authoring request does not select S074/104803/Standard/Regular')
    if compiler_target.get('catalog_id') != 'S074' or compiler_target.get('seed') != 104803 or compiler_target.get('setup_hash') != 1298808707:
        raise ValueError('Original compiler context target differs from S074')
    if (context.get('kind') != 'OriginalCompilerDerivedNativeFactoryContext' or context.get('world_allocated') is not False or
            context.get('identities_allocated') != 0 or context.get('runtime_loader_admitted') is not False):
        raise ValueError('Source-only compiler context required')
    if qualification != {'schema_version': 1, 'kind': 'ExperimentalGroundConfigurationQualification',
                         'runtime_backend': 'TypedGroundS026V1', 'compiler_context': pin(CONTEXT),
                         'scope': 'OriginalCompilerAndQualifiedFactoryInputs_SourceOnly',
                         'native_context_qualified': False, 'mission_ready': False}:
        raise ValueError('Exact S074 configuration qualification required')
    if author.get('graph') != pin(GRAPH) or author.get('binding') != pin(R9 / 'candidate/mission-logical-binding.json'):
        raise ValueError('Authoring request graph or logical binding differs from r9 owner')
    if graph.get('compiled_setup') is not False or graph.get('world_initialized') is not False or graph.get('native_world_state_used') is not False:
        raise ValueError('Fresh source configuration graph required')
    if not isinstance(graph.get('source_pins'), dict):
        raise ValueError('Pinned source graph required')
    for rel, expected in graph['source_pins'].items():
        source = Path(rel) if Path(rel).is_absolute() else ROOT / rel
        if pin(source)['sha256'] != expected:
            raise ValueError(f'Current graph source drift: {source}')
    if qualification['native_context_qualified'] or qualification['mission_ready']:
        raise ValueError('Qualification cannot confer native or mission readiness')

    projection = read(R9 / 'projection/receipt.json')
    if projection.get('status') != 'ProjectedSourceOnly' or projection.get('runtime_admission') is not False:
        raise ValueError('Current r9 source projection receipt required')
    projection_root = R9 / 'projection'
    building_defs = pin(projection_root / 'building-definitions.json')
    unit_meta = pin(projection_root / 'unit-production-metadata.json')
    building_requests = pin(projection_root / 'configured-metadata.json')
    for artifact in (building_defs, unit_meta, building_requests):
        value = read(artifact['path'])
        if value.get('catalog_id') != 'S074' or value.get('setup_hash') != 1298808707 or value.get('native_S050_capture') is not False:
            raise ValueError(f'S074 source projection identity differs: {artifact["path"]}')

    logical = pin(LOGICAL)
    if logical['sha256'] != 'b145eae26821fa3ee559de12473354813c83d0a4c1997159086c20654ea0112a':
        raise ValueError('Industrial Basin logical source drift')
    if package.get('static_map', {}).get('operation_map_id') != 'opmap.skirmish.desert_base_01':
        raise ValueError('Expected physical package identity differs')
    package_pin = pin(PACKAGE)
    physical_static = package['static_map']
    static_map = {'operation_map_id': 'opmap.skirmish.industrial_basin', 'definition': logical,
                  'grid_config': descriptor_from(physical_static['grid_config']),
                  'surface_data': descriptor_from(physical_static['surface_data'])}
    if static_map['grid_config'] != physical_static['grid_config'] or static_map['surface_data'] != physical_static['surface_data']:
        raise ValueError('Logical grid/surface inputs must exactly match the source-derived package')

    source_binding_path = R9 / 'candidate/mission-logical-binding.json'
    logical_binding_path = args.logical_binding_projection.resolve(strict=True)
    binding_receipt = read(R9 / 'candidate/receipt.json')
    binding = read(source_binding_path)
    if (binding_receipt.get('status') != 'PassedSourceOwnerCompile' or binding_receipt.get('runtime_admission') is not False or
            binding.get('CatalogId') != 'S074' or binding.get('DefinitionId') != 'skirmish.s074' or
            binding.get('SourceOperationMapId') != 'opmap.skirmish.industrial_basin'):
        raise ValueError('Actual S074 candidate logical binding required')
    portable_projection = read(logical_binding_path)
    portable_receipt_path = logical_binding_path.parent / 'receipt.json'
    portable_receipt = read(portable_receipt_path)
    binding_projection = validate_logical_binding_projection(portable_projection, portable_receipt,
        logical_binding_path, source_binding_path, CONTEXT, compiler_target)
    context_descriptor = pin(CONTEXT)
    native_metadata, native_origin = validate_native_factory_origin(
        context, context.get('native_factory_reference'), context.get('native_origin_descriptor'))

    if (death_audit.get('status') != 'Passed' or death_audit.get('readset', {}).get('source_pin_count') != 8091 or
            death_audit.get('readset', {}).get('rows') != 16 or death_audit.get('current_pin_audit', {}).get('missing_count') != 0 or
            death_audit.get('current_pin_audit', {}).get('mismatch_count') != 0):
        raise ValueError('Current exact S074 death readset audit required')
    death_pin = pin(DEATH)
    if death_pin['sha256'] != death_audit.get('readset', {}).get('sha256'):
        raise ValueError('S074 death readset audit/raw hash mismatch')

    graph_descriptor = pin(GRAPH)
    context_descriptor = pin(CONTEXT)
    qualification_descriptor = pin(QUALIFICATION)
    layout = descriptor_from(author['layout'])
    profiles = author.get('profiles')
    if not isinstance(profiles, dict) or set(profiles) != {'objective', 'army', 'start', 'economy', 'upgrades', 'intel', 'roles'}:
        raise ValueError('Exact authored S074 profile selection required')
    for path in profiles.values():
        matches = [key for key in graph['source_pins'] if Path(key).as_posix().endswith('/' + path)]
        if len(matches) != 1 or pin(ROOT / path)['sha256'] != graph['source_pins'][matches[0]]:
            raise ValueError(f'Profile is not owned by current graph: {path}')
    setup_matrix = validate_owner_setup_matrix(author, binding_receipt, author.get('setup_matrix'))

    template_descriptor = read(TEMPLATES)
    if not isinstance(template_descriptor, dict) or not {'path', 'sha256', 'source_reference_path', 'source_reference_sha256', 'projection_manifest_path', 'projection_manifest_sha256'} <= set(template_descriptor):
        raise ValueError('Exact captured S074 reference-template descriptor required')
    if pin(template_descriptor['path'])['sha256'] != template_descriptor['sha256']:
        raise ValueError('S074 template-domain descriptor drift')

    text_owner = read(TEXT)
    text_source_pins = text_owner.get('source_pins')
    if not isinstance(text_source_pins, dict) or len(text_source_pins) != 12:
        raise ValueError('Exact shared command-text source pin set required')
    current_text_pins = {}
    for captured_path, expected in text_source_pins.items():
        source = Path(captured_path)
        if not source.is_absolute() or not source.is_relative_to(TEXT_CAPTURE_ROOT):
            raise ValueError('Command-text source escaped its captured project root')
        current = ROOT / source.relative_to(TEXT_CAPTURE_ROOT)
        if pin(current)['sha256'] != expected:
            raise ValueError(f'Current shared command-text owner differs: {current}')
        current_text_pins[str(current)] = expected

    resource_preset = pin(RESOURCE_PRESET)
    initial = {
        'schema_version': 1, 'kind': 'GeneratedMissionInitialization',
        'authority': 'AuthoredConfigAndBakedCampaignMap',
        'source_epoch': 'OriginalCompilerDerivedNativeFactoryContext/v1',
        'mission_id': 'S074', 'selection': {'difficulty_id': 2, 'size_id': 1, 'seed': 104803},
        'source_config_graph': graph_descriptor, 'setup_matrix': setup_matrix,
        'canonical_battle_catalog': pin(CATALOG), 'selected_operation_map': logical,
        'static_map': static_map,
        'world_profile': {'identity_backend': 'FreshLogicalEntitiesV1', 'system_initialization': 'OriginalOnCreate', 'future_native_inputs': False},
        'reference_templates': template_descriptor, 'building_definitions': building_defs,
        'building_placement_config': pin(CONSTRUCTION), 'placement_domain': {},
        'resource_preset': resource_preset, 'unit_production_metadata': unit_meta,
        'building_request_metadata': building_requests,
        'operation_map_rotation_reference': pin(ROTATION),
        'metadata_compiler_context': context_descriptor, 'configuration_qualification': qualification_descriptor,
        'metadata_compatibility': native_metadata, 'metadata_source_origin': native_origin,
        'typed_mission_configuration': {'profiles': profiles, 'campaign_source_path': author['campaign_source_path'], 'layout': layout},
        'mission_logical_binding': pin(logical_binding_path),
        'death_mechanical_inputs': death_pin | {'capture_root': str(ROOT)},
        'command_text_data': pin(TEXT),
    }
    if author.get('resource_preset') is not None:
        selected_preset = descriptor_from(author['resource_preset'])
        if selected_preset != resource_preset:
            raise ValueError('Runtime resource preset differs from explicit compiler authoring selection')
        author_request_pin = pin(AUTHORING)
        author_result_path = AUTHORING.parent / 'authoring-result.json'
        author_result_pin = pin(author_result_path)
        author_result = read(author_result_path)
        result_pins = author_result.get('input_pins')
        if (author_result.get('kind') != 'SkirmishMissionAuthoringPreflightResult' or
                author_result.get('status') != 'ConfigurationCompiled' or
                author_result.get('compiler_accepted') is not True or
                result_pins.get(author_request_pin['path'], {}).get('sha256') != author_request_pin['sha256'] or
                result_pins.get(resource_preset['path'], {}).get('sha256') != resource_preset['sha256']):
            raise ValueError('Exact compiler result must pin author request and resource preset selection')
        initial['source_owner_authoring'] = {'request': author_request_pin, 'result': author_result_pin}
    # These are source-owned descriptor contracts, not fabricated camera/native fields.
    clearance_pin = pin(CLEARANCE)
    clearance = read(CLEARANCE)
    if (clearance.get('kind') != 'SourceDerivedCampaignMapDropClearanceProjection' or
            clearance.get('source_fit_certified') is not False or clearance.get('mission_ready') is not False):
        raise ValueError('Source-derived clearance projection required')
    run_inputs = {
        'schema_version': 1, 'kind': 'SkirmishSourceSimulationDataBundle', 'mission': 'S074', 'seed': 104803,
        'runtime_backend': 'TypedGroundS026V1',
        'runtime_data_inputs': {'schema_version': 1, 'kind': 'CurrentRoadSceneRuntimeInputs', 'data_root': str(ROOT),
            'data_files': {path: digest((ROOT / path).read_bytes()) for path in (
                'Assets/Game/Configs/Scene/Game_RoadBuild_Config.asset', 'Assets/Game/Configs/Scene/Game_RoadBuild_Config.asset.meta',
                'Assets/Game/Configs/Scene/Game_RuntimeCitySpawner_Config.asset', 'Assets/Game/Configs/Scene/Game_RuntimeCitySpawner_Config.asset.meta')},
            'source_files': {path: digest((ROOT / path).read_bytes()) for path in (
                'Assets/Game/Scripts/Configs/Scene/RoadBuildSystemSceneConfigAsset.cs.meta',
                'Assets/Game/Scripts/Configs/Scene/RuntimeCitySpawnerSystemSceneConfigAsset.cs.meta')}},
        'initialization': None, 'unit_metadata': unit_meta, 'static_clearance': clearance_pin,
        'source_derived_map': package_pin,
        'source_camera_policy': {'schema_version': 1, 'kind': 'SkirmishSourceCameraPolicy', 'mission': 'S074',
            'mode': 'NoCameraLogicalDeliveryDiagnostic', 'configuration_qualification': qualification_descriptor},
        'command_text_sources': {'captured_root': str(TEXT_CAPTURE_ROOT), 'current_root': str(ROOT),
            'input_pins': current_text_pins, 'artifact_sha256': pin(TEXT)['sha256']},
        'nonclaims': {'world_allocated': False, 'mission_ready': False, 'source_fit_certified': False,
                      'native_context_qualified': False, 'camera_qualified': False},
    }
    out.mkdir(parents=True)
    init_path = out / 'initialization.json'
    init_path.write_text(json.dumps(initial, indent=2) + '\n')
    init_pin = pin(init_path)
    run_inputs['initialization'] = init_pin
    bundle_path = out / 'data-bundle.json'
    bundle_path.write_text(json.dumps(run_inputs, indent=2) + '\n')
    bundle_pin = pin(bundle_path)
    receipt = {'schema_version': 1, 'kind': 'S074SourceOwnedDiagnosticInitializationComposerReceipt',
        'status': 'ComposedSourceOnlyRequestEnvelope', 'initialization': init_pin, 'data_bundle': bundle_pin,
        'source_graph': graph_descriptor, 'compiler_context': context_descriptor, 'configuration_qualification': qualification_descriptor,
        **binding_projection,
        'logical_map': logical, 'source_derived_physical_package': package_pin,
        'source_derived_clearance': clearance_pin, 'mission_ready': False, 'native_context_qualified': False,
        'source_fit_certified': False, 'world_allocated': False,
        'scope': 'Source-host request composed only from current S074 authored/compiler owners and explicitly pinned diagnostic inputs.'}
    receipt_path = out / 'composition-receipt.json'
    receipt_path.write_text(json.dumps(receipt, indent=2) + '\n')
    for path in (AUTHORING, GRAPH, CONTEXT, QUALIFICATION, PACKAGE, DEATH, DEATH_AUDIT, ROTATION, CLEARANCE,
                 LOGICAL, CONSTRUCTION, RESOURCE_PRESET, CATALOG, TEXT, TEMPLATES, source_binding_path,
                 logical_binding_path, portable_receipt_path):
        pin(path)
    print(json.dumps({'status': receipt['status'], 'initialization': str(init_path), 'data_bundle': str(bundle_path),
                      'mission_ready': False, 'world_allocated': False}))


if __name__ == '__main__':
    main()
