#!/usr/bin/env python3
"""Compose an S074 source-origin envelope with historical placement provenance only."""
import argparse
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
ORIGIN = ROOT / 'T Logs/SkirmishProduction/s074-source-origin-authoring-20261107-r1'
ORIGIN_FACTORY = ROOT / 'T Logs/SkirmishProduction/s074-source-origin-factory-export-20261107-r1'
ORIGIN_PROJECTION = ROOT / 'T Logs/SkirmishProduction/s074-source-origin-factory-projection-20261107-r1'
PORTABLE_REPLAY = ROOT / 'T Logs/SkirmishProduction/s074-origin-portable-binding-replay-20261113-r12'
HOST_BUILD = ROOT / 'T Logs/SkirmishProduction/s074-origin-source-build-20261113-r5'
HISTORICAL_PLACEMENT = ROOT / 'T Logs/SkirmishProduction/s074-source-origin-historical-placement-20261107-r1/receipt.json'
GRAPH = ROOT / 'T Logs/SkirmishProduction/five-mission-pilot-20261106-T1/s074-industrial-source-graph-r1/source-config-graph.json'
AUTHORING = ORIGIN / 'authoring-request.json'
ORIGIN_BINDING = ROOT / 'T Logs/SkirmishProduction/s074-source-origin-authoring-draft-20261107-r1/binding.json'
CONTEXT = ORIGIN_FACTORY / 's074/source-factory-context.json'
QUALIFICATION = ROOT / 'T Logs/SkirmishProduction/s074-source-origin-runtime-inputs-20261107-r1/qualification.json'
PACKAGE = ROOT / 'T Logs/SkirmishProduction/s026-source-map-bundle-20261107-r2/map-package.json'
TEMPLATES = ROOT / 'T Logs/SkirmishProduction/s074-origin-focused-prefab-20261113-r1/reference-template-descriptor.json'
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


def verify_pin_map(pins, label):
    if not isinstance(pins, dict) or not pins:
        raise ValueError(f'Nonempty {label} input pin map required')
    for path, expected in pins.items():
        if pin(path)['sha256'] != expected:
            raise ValueError(f'{label} input pin drift: {path}')


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
    """Bind exact origin-owner matrix bytes and S074 Standard rifle/gunner counts."""
    owner_descriptor = descriptor_from(author.get('setup_matrix'))
    origin_inputs = candidate_receipt.get('origin_inputs_sha256')
    if (candidate_receipt.get('status') != 'PassedSourceOwnerCompile' or
            not isinstance(origin_inputs, dict) or origin_inputs.get(owner_descriptor['path']) != owner_descriptor['sha256']):
        raise ValueError('S074 origin owner replay receipt does not pin the authoring-request setup matrix')
    if selected != owner_descriptor:
        raise ValueError('Effective setup matrix must be the exact original S074 authoring-request descriptor')
    import csv
    with Path(owner_descriptor['path']).open(newline='') as stream:
        rows = [row for row in csv.DictReader(stream)
                if row.get('catalog_id') == 'S074' and row.get('size_id') == 'Standard']
    if (len(rows) != 1 or rows[0].get('objective_id') != 'BA' or
            any(rows[0].get(field) != expected for field, expected in (
                ('player_rifle', '10'), ('player_gunner', '6'),
                ('enemy_rifle', '10'), ('enemy_gunner', '6')))):
        raise ValueError('Exact S074 Standard BA 10/6 player/enemy rifle/gunner matrix row required')
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
        raise ValueError('Exact origin-context factory reference and qualified origin pair required')
    return metadata_descriptor, origin_descriptor


def validate_logical_binding_projection(wrapper, receipt, wrapper_path, source_binding_path, context_path, target):
    context_descriptor = pin(context_path)
    wrapper_path = Path(wrapper_path).resolve(strict=True)
    source_binding_path = Path(source_binding_path).resolve(strict=True)
    if (wrapper.get('schema_version') != 1 or
            wrapper.get('kind') != 'OriginalCompilerDeclaredLogicalBinding' or
            wrapper.get('metadata_compiler_context') != context_descriptor or
            wrapper.get('target_context') != target or
            wrapper.get('world_allocated') is not False or
            wrapper.get('native_capture') is not False or
            wrapper.get('native_context_qualified') is not False or
            wrapper.get('runtime_admission') is not False or
            wrapper.get('source_fit_certified') is not False or
            wrapper.get('mission_ready') is not False or
            wrapper.get('portable_binding', {}).get('kind') != 'DeclaredOriginalLogicalBindingFieldsWithInlineViews' or
            wrapper.get('portable_binding', {}).get('float_encoding') != 'IEEE754_float32_hex_bits' or
            wrapper.get('portable_binding', {}).get('unity_object_ids_exported') is not False or
            wrapper.get('portable_binding', {}).get('fields', {}).get('CatalogId') != 'S074'):
        raise ValueError('Original-codec S074 logical binding envelope/context mismatch')
    raw_source_binding = wrapper.get('source_owner_binding')
    retained_source_binding = pin(source_binding_path)
    origin_binding = read(source_binding_path)
    owner_binding_path = Path(raw_source_binding.get('path', '')) if isinstance(raw_source_binding, dict) else Path()
    owner_binding = read(owner_binding_path) if owner_binding_path.is_file() else {}
    if (not isinstance(raw_source_binding, dict) or set(raw_source_binding) != {'path', 'sha256'} or
            pin(raw_source_binding['path']) != raw_source_binding or
            raw_source_binding.get('sha256') != pin(owner_binding_path)['sha256'] or
            owner_binding.get('CatalogId') != 'S074' or owner_binding.get('DefinitionId') != 'skirmish.s074' or
            owner_binding.get('SourceOperationMapId') != target.get('operation_map_id') or
            owner_binding.get('SourceContentHash') != target.get('source_content_hash')):
        raise ValueError('Fresh compiler-authored origin binding identity/pin required')
    def semantic(value):
        if isinstance(value, dict):
            return {key: semantic(item) for key, item in value.items() if key not in ('name', 'hideFlags')}
        if isinstance(value, list):
            return [semantic(item) for item in value]
        if isinstance(value, float):
            import struct
            return struct.unpack('!f', struct.pack('!f', value))[0]
        return value
    if semantic(owner_binding.get('Variants')) != semantic(origin_binding.get('Variants')):
        raise ValueError('Fresh owner binding variant differs from retained origin binding')
    owner_replay_descriptor = receipt.get('source_owner_replay')
    if (not isinstance(owner_replay_descriptor, dict) or set(owner_replay_descriptor) != {'path', 'sha256'} or
            pin(owner_replay_descriptor['path']) != owner_replay_descriptor):
        raise ValueError('Exact source-owner replay receipt pin required')
    owner_replay = read(owner_replay_descriptor['path'])
    verify_pin_map(owner_replay.get('source_inputs_sha256'), 'Origin replay source/build')
    verify_pin_map(owner_replay.get('origin_inputs_sha256'), 'Origin replay authoring/context')
    if (owner_replay.get('status') != 'PassedSourceOwnerCompile' or
            owner_replay.get('binding_sha256') != raw_source_binding['sha256'] or
            owner_replay.get('binding_semantic_equal_to_origin') is not True or
            owner_replay.get('origin_binding_sha256') != retained_source_binding['sha256'] or
            owner_replay.get('world_allocated') is not False or owner_replay.get('runtime_admission') is not False or
            owner_replay.get('native_context_qualified') is not False or
            owner_replay.get('source_fit_certified') is not False or owner_replay.get('mission_ready') is not False):
        raise ValueError('Fresh original S074 source-owner replay receipt required')
    if (
            receipt.get('kind') != 'S074OriginCompilerDeclaredLogicalBindingPortableProjection' or
            receipt.get('status') != 'ProjectedSourceOnly' or
            receipt.get('compiler_context') != context_descriptor or
            receipt.get('logical_binding') != pin(wrapper_path) or
            receipt.get('source_owner_binding') != raw_source_binding or
            receipt.get('original_reader_decode_and_compiler_roundtrip') != 'PassedSourceOnly' or
            receipt.get('binding_semantic_equal_to_origin') is not True or
            receipt.get('origin_binding') != retained_source_binding or
            receipt.get('world_allocated') is not False or receipt.get('native_context_qualified') is not False or
            receipt.get('source_fit_certified') is not False or receipt.get('mission_ready') is not False):
        raise ValueError('Fresh original-codec S074 origin portable projection receipt required')
    proof = read(Path(owner_replay_descriptor['path']).parent / 'candidate-proof.json')
    if (proof.get('kind') != 'S074SourceOriginLogicalBindingReplay' or
            proof.get('setup_hash_with_binding') != 1716123768 or
            proof.get('portable_roundtrip_setup_hash') != 1716123768 or
            proof.get('original_reader_decode_and_compiler_roundtrip') is not True or
            proof.get('world_allocated') is not False or proof.get('mission_ready') is not False):
        raise ValueError('Exact S074 origin bound setup and source-reader roundtrip required')
    return {'logical_binding_projection': pin(wrapper_path), 'logical_binding_owner': raw_source_binding,
            'origin_binding': retained_source_binding,
            'origin_source_owner_replay': owner_replay_descriptor}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output-dir', type=Path, required=True)
    parser.add_argument('--logical-binding-projection', type=Path, required=True,
                        help='Fresh output from project_s074_origin_portable_binding.py')
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
    historical_receipt = read(HISTORICAL_PLACEMENT)
    historical_placement = descriptor_from(historical_receipt.get('new_placement'))
    if (historical_receipt.get('kind') != 'S074SourceOriginHistoricalPlacementComparison' or
            historical_receipt.get('status') != 'OwnerProcessingReturned' or
            historical_receipt.get('process_exit') != 0 or historical_receipt.get('timed_out') is not False or
            historical_receipt.get('registry_count') != 20 or
            historical_receipt.get('new_status_codes') != [1] * 20 or
            historical_placement != pin(historical_receipt['new_placement']['path']) or
            historical_receipt.get('source_fit_certified') is not False or
            historical_receipt.get('mission_ready') is not False or
            'historical pregame Campaign masks' not in historical_receipt.get('scope', '')):
        raise ValueError('Historical owner-placement receipt is invalid or overstated')
    verify_pin_map(historical_receipt.get('inputs'), 'Historical placement provenance')
    compiler_target = context['target_context']
    if (author.get('catalog_id'), author.get('seed'), author.get('size'), author.get('difficulty')) != ('S074', 104803, 'Standard', 'Regular'):
        raise ValueError('Original authoring request does not select S074/104803/Standard/Regular')
    if compiler_target.get('catalog_id') != 'S074' or compiler_target.get('seed') != 104803 or compiler_target.get('setup_hash') != 1716123768:
        raise ValueError('S074 source-origin compiler context must retain exact origin setup hash')
    if (context.get('kind') != 'OriginalCompilerDerivedNativeFactoryContext' or context.get('world_allocated') is not False or
            context.get('identities_allocated') != 0 or context.get('runtime_loader_admitted') is not False or
            context.get('native_S050_capture') is not False or context.get('source_fit_certified') is not False or
            context.get('active_mission_water_qualified') is not False or context.get('mission_ready') is not False):
        raise ValueError('Source-only compiler context required')
    if qualification != {'schema_version': 1, 'kind': 'ExperimentalGroundConfigurationQualification',
                         'runtime_backend': 'TypedGroundS026V1', 'compiler_context': pin(CONTEXT),
                         'scope': 'OriginalCompilerAndQualifiedFactoryInputs_SourceOnly',
                         'native_context_qualified': False, 'mission_ready': False}:
        raise ValueError('Exact S074 configuration qualification required')
    if author.get('graph') != pin(GRAPH) or author.get('binding') != pin(ORIGIN_BINDING):
        raise ValueError('Origin authoring request graph or retained origin binding descriptor drift')
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

    host_build = read(HOST_BUILD / 'build.json')
    if (host_build.get('status') != 'Passed' or host_build.get('compile_exit') != 0 or
            host_build.get('mission') != 'S074' or host_build.get('seed') != 104803 or
            not isinstance(host_build.get('source_and_reference_pins'), dict)):
        raise ValueError('Fresh passed S074 origin source host build required')
    host_build_pin = pin(HOST_BUILD / 'build.json')
    for path, expected in host_build['source_and_reference_pins'].items():
        if pin(path)['sha256'] != expected:
            raise ValueError(f'Fresh origin host source pin drift: {path}')
    projection = read(ORIGIN_PROJECTION / 'receipt.json')
    if (projection.get('kind') != 'S074SourceFactoryProjection' or
            projection.get('status') != 'ProjectedSourceOnly' or
            projection.get('target_context') != compiler_target or
            projection.get('world_allocated') is not False or
            projection.get('runtime_admission') is not False or
            projection.get('source_fit_certified') is not False or projection.get('mission_ready') is not False):
        raise ValueError('Exact source-origin S074 factory projection required')
    projection_root = ORIGIN_PROJECTION
    building_defs = pin(projection_root / 'building-definitions.json')
    unit_meta = pin(projection_root / 'unit-production-metadata.json')
    building_requests = pin(projection_root / 'configured-metadata.json')
    for artifact in (building_defs, unit_meta, building_requests):
        value = read(artifact['path'])
        if value.get('catalog_id') != 'S074' or value.get('setup_hash') != 1716123768 or value.get('native_S050_capture') is not False:
            raise ValueError(f'S074 source projection identity differs: {artifact["path"]}')
    for key, descriptor in projection.get('projections', {}).items():
        if descriptor_from(descriptor)['sha256'] != descriptor.get('sha256'):
            raise ValueError(f'Origin factory projection pin drift: {key}')

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

    source_binding_path = ORIGIN_BINDING
    logical_binding_path = args.logical_binding_projection.resolve(strict=True)
    binding_receipt_path = PORTABLE_REPLAY / 'owner/receipt.json'
    binding_receipt = read(binding_receipt_path)
    binding = read(source_binding_path)
    if (binding_receipt.get('status') != 'PassedSourceOwnerCompile' or binding_receipt.get('runtime_admission') is not False or
            binding_receipt.get('native_context_qualified') is not False or
            binding_receipt.get('binding_semantic_equal_to_origin') is not True or
            binding_receipt.get('origin_binding_sha256') != pin(source_binding_path)['sha256'] or
            binding_receipt.get('world_allocated') is not False or
            binding.get('CatalogId') != 'S074' or binding.get('DefinitionId') != 'skirmish.s074' or
            binding.get('SourceOperationMapId') != 'opmap.skirmish.industrial_basin'):
        raise ValueError('Actual S074 origin source binding and replay required')
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
        'resource_preset': pin(RESOURCE_PRESET), 'unit_production_metadata': unit_meta,
        'building_request_metadata': building_requests,
        'operation_map_rotation_reference': pin(ROTATION),
        'metadata_compiler_context': context_descriptor, 'configuration_qualification': qualification_descriptor,
        'metadata_compatibility': native_metadata, 'metadata_source_origin': native_origin,
        'typed_mission_configuration': {'profiles': profiles, 'campaign_source_path': author['campaign_source_path'], 'layout': layout},
        'mission_logical_binding': pin(logical_binding_path),
        'death_mechanical_inputs': death_pin | {'capture_root': str(ROOT)},
        'command_text_data': pin(TEXT),
    }
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
        'nonclaims': {'world_allocated': False, 'native_capture': False, 'native_context_qualified': False,
                      'runtime_admission': False, 'mission_ready': False, 'source_fit_certified': False,
                      'camera_qualified': False},
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
        'source_derived_clearance': clearance_pin, 'origin_host_build': host_build_pin,
        'historical_placement_provenance': {
            'receipt': pin(HISTORICAL_PLACEMENT), 'placement': historical_placement,
            'scope': historical_receipt['scope'], 'historical_only': True,
            'source_fit_certified': False, 'mission_ready': False,
        },
        'mission_ready': False, 'native_context_qualified': False, 'native_capture': False,
        'runtime_admission': False, 'source_fit_certified': False, 'world_allocated': False,
        'scope': 'Source-origin request composed only from pinned S074 source/compiler owners; historical placement is provenance only and does not establish current fit or readiness.'}
    receipt_path = out / 'composition-receipt.json'
    receipt_path.write_text(json.dumps(receipt, indent=2) + '\n')
    for path in (AUTHORING, ORIGIN_BINDING, GRAPH, CONTEXT, QUALIFICATION, PACKAGE, DEATH, DEATH_AUDIT, ROTATION, CLEARANCE,
                 LOGICAL, CONSTRUCTION, RESOURCE_PRESET, CATALOG, TEXT, TEMPLATES, source_binding_path,
                 logical_binding_path, portable_receipt_path, HISTORICAL_PLACEMENT,
                 Path(historical_placement['path']), HOST_BUILD / 'build.json',
                 ORIGIN_PROJECTION / 'receipt.json', projection_root / 'building-definitions.json',
                 projection_root / 'unit-production-metadata.json', projection_root / 'configured-metadata.json',
                 binding_receipt_path, Path(binding_projection['origin_source_owner_replay']['path'])):
        pin(path)
    verify_pin_map(host_build['source_and_reference_pins'], 'Final fresh origin host source')
    print(json.dumps({'status': receipt['status'], 'initialization': str(init_path), 'data_bundle': str(bundle_path),
                      'mission_ready': False, 'world_allocated': False}))


if __name__ == '__main__':
    main()
