#!/usr/bin/env python3
"""Compose a separate S074 both-vehicle ten-metre source-only bundle and owner receipt.

This recipe validates the original bound author result, owner binding replay,
factory projection and focused-prefab source projection. It does not create or
claim native qualification, runtime admission, map fit, or mission readiness.
"""
import argparse
import csv
import hashlib
import json
from pathlib import Path
import importlib.util

ROOT = Path(__file__).resolve().parents[4]
BASE = ROOT / 'T Logs/SkirmishProduction'
AUTHOR_DIR = BASE / 's074-both-vehicle-ten-metre-authoring-20261114-r1'
AUTHOR_REQUEST = AUTHOR_DIR / 'authoring-request.json'
AUTHOR_RESULT = AUTHOR_DIR / 'authoring-result.json'
AUTHOR_RECEIPT = AUTHOR_DIR / 'authoring-receipt.json'
LAYOUT = BASE / 's074-provisional-both-vehicle-ten-metre-20261114-r1/layout.json'
CANDIDATE_RECEIPT = BASE / 's074-provisional-both-vehicle-ten-metre-20261114-r1/receipt.json'
BASE_CANDIDATE_LAYOUT = BASE / 's074-provisional-player-vehicle-ten-metre-20261114-r1/layout.json'
PLAYER_KERNEL_RECEIPT = BASE / 's074-vehicle-kernel-diagnostic-20261114-r1/receipt.json'
ENEMY_KERNEL_RECEIPT = BASE / 's074-enemy-support-kernel-diagnostic-20261114-r1/receipt.json'
GRAPH = BASE / 'five-mission-pilot-20261106-T1/s074-industrial-source-graph-r1/source-config-graph.json'
MATRIX = BASE / 'five-mission-pilot-20261106-T1/experimental-opening-mix-matrix.csv'
BINDING_DIR = BASE / 's074-both-vehicle-generic-origin-binding-20261114-r2'
BINDING_RECEIPT = BINDING_DIR / 'receipt.json'
BINDING = BINDING_DIR / 'owner/mission-logical-binding.json'
BINDING_WRAPPER = BINDING_DIR / 'logical-binding.json'
BINDING_PROOF = BINDING_DIR / 'owner/candidate-proof.json'
FACTORY_DIR = BASE / 's074-both-vehicle-ten-metre-factory-metadata-20261114-r1'
FACTORY_RECEIPT = FACTORY_DIR / 'receipt.json'
FACTORY_CONTEXT = FACTORY_DIR / 's074/source-factory-context.json'
FACTORY_PROJECTION_RECEIPT = FACTORY_DIR / 'projection/receipt.json'
FRESH_FACTORY_DIR = BASE / 's074-both-vehicle-ten-metre-factory-metadata-epoch-20261114-r2'
FRESH_FACTORY_RECEIPT = FRESH_FACTORY_DIR / 'receipt.json'
FRESH_FACTORY_CONTEXT = FRESH_FACTORY_DIR / 's074/source-factory-context.json'
FRESH_FACTORY_PROJECTION_RECEIPT = FRESH_FACTORY_DIR / 'projection/receipt.json'
FOCUSED_DIR = BASE / 's074-both-vehicle-ten-metre-focused-prefab-domain-20261114-r1'
FOCUSED_RECEIPT = FOCUSED_DIR / 'receipt.json'
FOCUSED_PROOF = FOCUSED_DIR / 's074-focused-prefab-capture-proof.json'
FOCUSED_DESCRIPTOR = FOCUSED_DIR / 'reference-template-descriptor.json'
HOST_BUILD = BASE / 's074-both-vehicle-generic-source-build-20261114-r2/build.json'
SOURCE_BUILD = HOST_BUILD
QUALIFICATION = BASE / 's074-both-vehicle-ten-metre-source-qualification-20261114-r1/qualification.json'
PACKAGE = BASE / 's026-source-map-bundle-20261107-r2/map-package.json'
ROTATION = ROOT / 'Logs/SkirmishProduction/s073-operation-map-rotation-capture-20261101-r4/rotation-reference.json'
CLEARANCE = BASE / 'source-map-clearance-host-20261107-r3/valid-projection.json'
LOGICAL_MAP = ROOT / 'Assets/Game/Configs/Skirmish/IndustrialBasin/OperationMap_IndustrialBasin.asset'
CONSTRUCTION = ROOT / 'Assets/Game/Configs/Skirmish/IndustrialBasin/Construction.asset'
RESOURCE_PRESET = ROOT / 'Assets/Game/Resources/SkirmishIndustrialBasin.asset'
CATALOG = ROOT / 'Assets/Game/Resources/SkirmishBattleCatalog.asset'
DEATH = ROOT / 'Logs/SkirmishProduction/s074-owner-data-capture-20261107-r1/death-16/death-mechanical-readset.json'
DEATH_AUDIT = BASE / 's074-owner-data-capture-inputs-20261107-r1/death-readset-audit-receipt.json'
TEXT = Path('/Volumes/T7/Projects/WarlineCapture/Logs/private-candidates/s049-physical-export-qualified-20261028-r1/project/Logs/SkirmishProduction/s049-command-text-r1/command-text.json')
TEXT_CAPTURE_ROOT = TEXT.parents[3]
TARGET = {'catalog_id': 'S074', 'seed': 104803, 'size': 'Standard', 'difficulty': 'Regular',
          'setup_hash': 1532795717, 'operation_map_id': 'opmap.skirmish.industrial_basin'}
FALSE_GATES = ('world_allocated', 'runtime_admission', 'native_context_qualified',
               'source_fit_certified', 'mission_ready')


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def pin(path):
    path = Path(path).resolve(strict=True)
    if path.is_symlink() or not path.is_file():
        raise ValueError(f'Canonical regular file required: {path}')
    raw = path.read_bytes()
    if not raw:
        raise ValueError(f'Nonempty input required: {path}')
    return {'path': str(path), 'sha256': hashlib.sha256(raw).hexdigest()}


def read(path):
    return json.loads(Path(path).read_bytes())


def verify_desc(desc, label):
    if not isinstance(desc, dict) or set(desc) != {'path', 'sha256'} or not isinstance(desc['path'], str):
        raise ValueError(f'Exact {label} descriptor required')
    actual = pin(desc['path'])
    if actual != desc:
        raise ValueError(f'{label} descriptor is stale: {desc.get("path")}')
    return actual


def verify_pin_map(pins, label):
    if not isinstance(pins, dict) or not pins:
        raise ValueError(f'Nonempty {label} pin map required')
    for path, digest in pins.items():
        if not isinstance(path, str) or not isinstance(digest, str) or pin(path)['sha256'] != digest:
            raise ValueError(f'{label} pin drift: {path}')


def factory_context_projection_bridge():
    """Bridge the exact retained runtime descriptors to the fresh owner export."""
    old_receipt = read(FACTORY_RECEIPT)
    fresh_receipt = read(FRESH_FACTORY_RECEIPT)
    old_context_bytes = FACTORY_CONTEXT.read_bytes()
    fresh_context_bytes = FRESH_FACTORY_CONTEXT.read_bytes()
    old_context_desc = pin(FACTORY_CONTEXT)
    fresh_context_desc = pin(FRESH_FACTORY_CONTEXT)
    if old_context_bytes != fresh_context_bytes or old_context_desc['sha256'] != fresh_context_desc['sha256']:
        raise ValueError('Fresh original factory context bytes differ from the retained qualification context')
    old_target = read(FACTORY_CONTEXT).get('target_context', {})
    fresh_target = read(FRESH_FACTORY_CONTEXT).get('target_context', {})
    if any(old_target.get(k) != v for k, v in TARGET.items()) or old_target != fresh_target:
        raise ValueError('Fresh/retained factory compiler target differs')
    retained_binding_replay = old_receipt.get('binding_replay')
    if not isinstance(retained_binding_replay, dict) or pin(retained_binding_replay.get('path')) != retained_binding_replay:
        raise ValueError('Retained factory receipt must pin its historical owner replay')
    retained_binding = read(retained_binding_replay['path'])
    retained_wrapper_desc = retained_binding.get('binding')
    if not isinstance(retained_wrapper_desc, dict) or pin(retained_wrapper_desc.get('path')) != retained_wrapper_desc:
        raise ValueError('Retained historical binding wrapper descriptor is stale')
    retained_wrapper = read(retained_wrapper_desc['path'])
    retained_raw_desc = retained_wrapper.get('source_owner_binding')
    if not isinstance(retained_raw_desc, dict) or pin(retained_raw_desc.get('path')) != retained_raw_desc or \
            Path(retained_raw_desc['path']).read_bytes() != BINDING.read_bytes():
        raise ValueError('Retained historical owner binding differs from fresh compiler binding bytes')
    if retained_binding.get('status') != 'PassedSourceOwnerCompile' or retained_binding.get('setup_hash') != TARGET['setup_hash']:
        raise ValueError('Retained historical owner replay target/status mismatch')
    if fresh_receipt.get('kind') != 'S074BothVehicleTenMetreOriginalFactoryMetadataProjection' or \
            fresh_receipt.get('status') != 'ProjectedSourceOnly' or \
            fresh_receipt.get('binding_replay') != pin(BINDING_RECEIPT) or \
            fresh_receipt.get('bound_author_result') != pin(AUTHOR_RESULT) or \
            fresh_receipt.get('source_factory_context') != fresh_context_desc or \
            fresh_receipt.get('projection_receipt') != pin(FRESH_FACTORY_PROJECTION_RECEIPT):
        raise ValueError('Fresh factory exporter receipt is not bound to current owner replay')
    verify_pin_map(fresh_receipt.get('source_and_reference_pins'), 'Fresh factory exporter')

    old_projection = read(FACTORY_PROJECTION_RECEIPT)
    fresh_projection = read(FRESH_FACTORY_PROJECTION_RECEIPT)
    for proj in (old_projection, fresh_projection):
        if proj.get('status') != 'ProjectedSourceOnly' or \
                proj.get('target_context', {}).get('setup_hash') != TARGET['setup_hash'] or \
                any(proj.get(k) is not False for k in ('world_allocated', 'runtime_admission',
                        'source_fit_certified', 'mission_ready')):
            raise ValueError('Factory projection exceeds its source-only scope')
    old_files = {n: pin(FACTORY_DIR / 'projection' / n) for n in
                 ('building-definitions.json', 'unit-production-metadata.json', 'configured-metadata.json')}
    fresh_files = {n: pin(FRESH_FACTORY_DIR / 'projection' / n) for n in
                   ('building-definitions.json', 'unit-production-metadata.json', 'configured-metadata.json')}
    old_values = {n: read(d['path']) for n, d in old_files.items()}
    fresh_values = {n: read(d['path']) for n, d in fresh_files.items()}
    old_context = old_context_desc
    fresh_context = fresh_context_desc
    old_building, fresh_building = old_values['building-definitions.json'], fresh_values['building-definitions.json']
    old_unit, fresh_unit = old_values['unit-production-metadata.json'], fresh_values['unit-production-metadata.json']
    old_config, fresh_config = old_values['configured-metadata.json'], fresh_values['configured-metadata.json']
    for old_value, fresh_value in ((old_building, fresh_building), (old_unit, fresh_unit), (old_config, fresh_config)):
        old_value['metadata_compiler_context'] = {'path': '<bound-context>', 'sha256': old_context['sha256']}
        fresh_value['metadata_compiler_context'] = {'path': '<bound-context>', 'sha256': fresh_context['sha256']}
    old_config['reference'] = {'path': '<projected-building-definitions>', 'sha256': '<equal-projected-domain>'}
    fresh_config['reference'] = {'path': '<projected-building-definitions>', 'sha256': '<equal-projected-domain>'}
    if old_building != fresh_building or old_unit != fresh_unit or old_config != fresh_config:
        raise ValueError('Retained runtime factory projection differs beyond context/reference descriptor paths')

    # The historical receipt's stale pins are recorded verbatim and never promoted.
    stale = []
    for path, expected in old_receipt.get('source_and_reference_pins', {}).items():
        actual = pin(path)['sha256'] if Path(path).is_file() and not Path(path).is_symlink() else 'missing'
        if actual != expected:
            stale.append({'path': path, 'receipt_sha256': expected, 'current_sha256': actual})
    checks = {'context_bytes_equal': True, 'building_definitions_equal_after_descriptor_normalization': True,
              'unit_metadata_equal_after_descriptor_normalization': True,
              'configured_metadata_equal_after_descriptor_normalization': True,
              'fresh_factory_pin_map_current': True, 'old_pin_drift_quarantined': bool(stale),
              'retained_and_current_owner_binding_bytes_equal': True}
    return {'schema_version': 1, 'kind': 'S074BothVehicleTenMetreFactoryContextProjectionBridge',
            'status': 'PassedSourceOnlyContextAndFactoryDomainBridge', 'target': TARGET,
            'retained_context': old_context_desc, 'fresh_context': fresh_context_desc,
            'retained_factory_receipt': pin(FACTORY_RECEIPT), 'fresh_factory_receipt': pin(FRESH_FACTORY_RECEIPT),
            'retained_projection_receipt': pin(FACTORY_PROJECTION_RECEIPT),
            'fresh_projection_receipt': pin(FRESH_FACTORY_PROJECTION_RECEIPT),
            'retained_binding_replay': retained_binding_replay,
            'retained_binding_wrapper': retained_wrapper_desc,
            'retained_owner_binding': retained_raw_desc,
            'current_binding_replay': pin(BINDING_RECEIPT),
            'current_owner_binding': pin(BINDING),
            'retained_projection_files': old_files, 'fresh_projection_files': fresh_files,
            'checks': checks, 'retained_receipt_pin_drift': stale,
            'retained_receipt_pin_drift_count': len(stale),
            'retained_stale_source_pins_promoted': False,
            'world_allocated': False, 'runtime_admission': False, 'native_context_qualified': False,
            'source_fit_certified': False, 'mission_ready': False,
            'scope': 'Fresh original factory exporter is byte-equal to retained context; projected factory domains are equal after only descriptor-path normalization; stale retained pin claims are recorded but not treated as current.'}


def derive_runtime_binding_wrapper(replay_wrapper, original_wrapper_desc, factory_context, factory_context_desc):
    """Retarget only wrapper context/target descriptors to the initializer's exact owner context."""
    if pin(original_wrapper_desc['path']) != original_wrapper_desc or \
            read(original_wrapper_desc['path']) != replay_wrapper:
        raise ValueError('Original replay wrapper descriptor is stale')
    if replay_wrapper.get('kind') != 'OriginalCompilerDeclaredLogicalBinding' or \
            replay_wrapper.get('camera_framing_unobserved') is not True or \
            any(replay_wrapper.get(k) is not False for k in ('world_allocated', 'native_capture',
                    'native_context_qualified', 'runtime_admission', 'source_fit_certified', 'mission_ready')):
        raise ValueError('Original owner portable binding wrapper must remain source-only')
    replay_context_desc = replay_wrapper.get('metadata_compiler_context')
    if not isinstance(replay_context_desc, dict) or set(replay_context_desc) != {'path', 'sha256'} or \
            pin(replay_context_desc.get('path')) != replay_context_desc:
        raise ValueError('Original portable binding compiler-context pin required')
    replay_context = read(replay_context_desc['path'])
    replay_target = replay_wrapper.get('target_context')
    factory_target = factory_context.get('target_context')
    required = ('catalog_id', 'definition_id', 'seed', 'size', 'difficulty', 'setup_hash', 'operation_map_id',
                'source_content_hash', 'source_fingerprint', 'layout_fingerprint', 'mission_logical_binding_fingerprint')
    if replay_context.get('kind') != 'OriginalCompilerDerivedS074BothVehicleTenMetreCandidateSetupContext' or \
            replay_context.get('target_context') != replay_target or \
            replay_context.get('world_allocated') is not False or replay_context.get('identities_allocated') != 0 or \
            any(replay_context.get(k) is not False for k in ('runtime_loader_admitted', 'native_capture',
                    'native_context_qualified', 'source_fit_certified', 'active_mission_water_qualified',
                    'camera_framing_observed', 'mission_ready')) or \
            any(replay_target.get(k) != factory_target.get(k) for k in required) or \
            any(factory_target.get(k) != v for k, v in TARGET.items()):
        raise ValueError('Owner portable binding target/fingerprints do not join the source factory context')
    runtime = json.loads(json.dumps(replay_wrapper))
    runtime['metadata_compiler_context'] = factory_context_desc
    runtime['target_context'] = factory_target
    if runtime.get('portable_binding') != replay_wrapper.get('portable_binding') or \
            runtime.get('source_owner_binding') != replay_wrapper.get('source_owner_binding'):
        raise ValueError('Runtime wrapper may not alter original portable codec or owner binding')
    return runtime


def compose(out, *, author_request=AUTHOR_REQUEST, matrix_path=MATRIX, binding_receipt_path=BINDING_RECEIPT,
            binding_path=BINDING, factory_receipt_path=FACTORY_RECEIPT, focused_receipt_path=FOCUSED_RECEIPT,
            source_build_path=SOURCE_BUILD):
    out = Path(out).absolute()
    if not out.is_relative_to(BASE) or out.exists():
        raise ValueError('Fresh output directory under T Logs/SkirmishProduction required')

    req = read(author_request)
    result = read(AUTHOR_RESULT)
    author_receipt = read(AUTHOR_RECEIPT)
    graph = read(GRAPH)
    binding_receipt = read(binding_receipt_path)
    binding = read(binding_path)
    binding_wrapper = read(BINDING_WRAPPER)
    binding_proof = read(BINDING_PROOF)
    factory = read(factory_receipt_path)
    factory_context = read(FACTORY_CONTEXT)
    factory_projection = read(FACTORY_PROJECTION_RECEIPT)
    fresh_factory = read(FRESH_FACTORY_RECEIPT)
    fresh_factory_context = read(FRESH_FACTORY_CONTEXT)
    fresh_factory_projection = read(FRESH_FACTORY_PROJECTION_RECEIPT)
    bridge = factory_context_projection_bridge()
    focused = read(focused_receipt_path)
    focused_proof = read(FOCUSED_PROOF)
    host = read(HOST_BUILD)
    source_build = read(source_build_path)
    qualification = read(QUALIFICATION)

    request_desc = pin(author_request)
    result_desc = pin(AUTHOR_RESULT)
    matrix_desc = pin(matrix_path)
    layout_desc = pin(LAYOUT)
    graph_desc = pin(GRAPH)
    binding_desc = pin(binding_path)
    binding_wrapper_desc = pin(BINDING_WRAPPER)
    binding_receipt_desc = pin(binding_receipt_path)
    factory_receipt_desc = pin(factory_receipt_path)
    factory_context_desc = pin(FACTORY_CONTEXT)
    focused_receipt_desc = pin(focused_receipt_path)
    focused_proof_desc = pin(FOCUSED_PROOF)
    focused_descriptor = read(FOCUSED_DESCRIPTOR)
    qualification_desc = pin(QUALIFICATION)
    host_desc = pin(HOST_BUILD)
    runtime_wrapper = derive_runtime_binding_wrapper(binding_wrapper, binding_wrapper_desc,
                                                     factory_context, factory_context_desc)
    runtime_wrapper_path = out / 'runtime-logical-binding.json'
    runtime_wrapper_bytes = (json.dumps(runtime_wrapper, indent=2, sort_keys=True) + '\n').encode()
    runtime_wrapper_desc = {'path': str(runtime_wrapper_path),
                            'sha256': hashlib.sha256(runtime_wrapper_bytes).hexdigest()}

    if req.get('schema_version') != 1 or req.get('kind') != 'SkirmishMissionAuthoringPreflight' or (
            req.get('catalog_id'), req.get('seed'), req.get('size'), req.get('difficulty')) != (
            'S074', 104803, 'Standard', 'Regular'):
        raise ValueError('Exact original S074 both-vehicle ten-metre author request required')
    if req.get('layout') != layout_desc:
        raise ValueError('Author request layout descriptor is stale or mismatched')
    if req.get('setup_matrix') != matrix_desc:
        raise ValueError('Author request setup matrix descriptor is stale or mismatched')
    if req.get('graph') != graph_desc:
        raise ValueError('Author request graph descriptor differs from current source graph')
    if result.get('status') != 'ConfigurationCompiled' or result.get('compiler_accepted') is not True or \
            result.get('catalog_id') != 'S074' or result.get('seed') != 104803 or \
            result.get('resolved_setup', {}).get('setup_hash') != TARGET['setup_hash'] or \
            result.get('source_fingerprint') is None or result.get('layout_fingerprint') is None or \
            result.get('world_allocated') is not False or result.get('source_fit_verified') is not False or \
            result.get('mission_ready') is not False:
        raise ValueError('Original compiler result must be bound to the exact source-only candidate')
    input_pins = result.get('input_pins')
    if not isinstance(input_pins, dict) or input_pins.get(request_desc['path'], {}).get('sha256') != request_desc['sha256'] or \
            input_pins.get(matrix_desc['path'], {}).get('sha256') != matrix_desc['sha256'] or \
            input_pins.get(layout_desc['path'], {}).get('sha256') != layout_desc['sha256']:
        raise ValueError('Author result does not pin exact request/layout/matrix inputs')
    if author_receipt.get('process_exit') != 0 or author_receipt.get('timed_out') is not False or \
            author_receipt.get('mission_ready') is not False or author_receipt.get('source_fit_verified') is not False:
        raise ValueError('Passed source-only author process receipt required')

    candidate = read(CANDIDATE_RECEIPT)
    candidate_layout = read(LAYOUT)
    base_candidate_layout = read(BASE_CANDIDATE_LAYOUT)
    player_kernel = read(PLAYER_KERNEL_RECEIPT)
    enemy_kernel = read(ENEMY_KERNEL_RECEIPT)
    changed_ids = {'s074.provisional.spawn.vehicle.enemy.pad'}
    def without_new_extents(value):
        result = json.loads(json.dumps(value)); result.pop('contentHash', None)
        for pad in result.get('pads', []):
            if pad.get('PadId') in changed_ids:
                original = next(x for x in base_candidate_layout['pads'] if x.get('PadId') == pad['PadId'])
                pad['WidthMetres'] = original['WidthMetres']; pad['DepthMetres'] = original['DepthMetres']
        return result
    layout_pads = {p.get('PadId'): p for p in candidate_layout.get('pads', [])}
    if candidate.get('kind') != 'S074ProvisionalBothVehiclePadsCandidate' or \
            candidate.get('status') != 'PreparedUnqualified' or candidate.get('layout') != pin(LAYOUT) or \
            candidate.get('base_layout') != pin(BASE_CANDIDATE_LAYOUT) or \
            candidate.get('change', {}).get('pad') != 's074.provisional.spawn.vehicle.enemy.pad' or \
            candidate.get('change', {}).get('old_metres') != [8, 8] or \
            candidate.get('change', {}).get('new_metres') != [10, 10] or \
            candidate.get('change', {}).get('center') != [906, 484] or \
            any(candidate.get(k) is not False for k in ('world_initialized', 'source_fit_certified', 'native_parity', 'mission_ready')) or \
            without_new_extents(candidate_layout) != without_new_extents(base_candidate_layout) or \
            any((layout_pads[k]['WidthMetres'], layout_pads[k]['DepthMetres']) != size for k, size in {
                's074.provisional.spawn.infantry.player.pad': (8, 8),
                's074.provisional.spawn.infantry.enemy.pad': (8, 8),
                's074.provisional.spawn.vehicle.player.pad': (10, 10),
                's074.provisional.spawn.vehicle.enemy.pad': (10, 10)}.items()):
        raise ValueError('Both-vehicle candidate receipt/layout must contain only the second vehicle-pad extent change and stay unqualified')
    if player_kernel.get('kind') != 'S074OriginalSpawnCellSearchKernelBoundedReplay' or \
            player_kernel.get('validation', {}).get('8m_control_matches_observed_car_apc') is not True or \
            player_kernel.get('validation', {}).get('8m_tank_reproduces_surface_rejection') is not True or \
            not player_kernel.get('replays', {}).get('10m') or \
            not all(x.get('search_found') is True and x.get('surface_traversable') is True for x in player_kernel['replays']['10m']) or \
            player_kernel.get('mission_ready') is not False or player_kernel.get('native_context_qualified') is not False:
        raise ValueError('Player vehicle conditional kernel rationale failed its exact control or false-scope gates')
    if enemy_kernel.get('kind') != 'S074EnemyVehicleSupportOriginalKernelConditionalReplay' or \
            enemy_kernel.get('status') != 'PassedConditionalKernelControlAndCounterfactual' or \
            enemy_kernel.get('validation', {}).get('eight_metre_control_reproduces_tanker_search_failure') is not True or \
            enemy_kernel.get('validation', {}).get('ten_metre_conditional_car_apc_tank_tanker_and_trucks_all_pass_surface_and_placement') is not True or \
            enemy_kernel.get('target', {}).get('center_cell') != [906, 484] or \
            not all(x.get('search_found') is True and x.get('surface_traversable') is True for x in enemy_kernel.get('conditional_ten_metre_outputs', [])) or \
            enemy_kernel.get('mission_ready') is not False or enemy_kernel.get('native_context_qualified') is not False:
        raise ValueError('Enemy vehicle conditional kernel rationale failed its exact control or false-scope gates')

    if graph.get('authority') != 'AuthoredConfigAssets' or graph.get('compiled_setup') is not False or \
            graph.get('world_initialized') is not False or graph.get('native_world_state_used') is not False:
        raise ValueError('Current uncompiled source graph required')
    verify_pin_map(graph.get('source_pins'), 'Current S074 source graph')
    with Path(matrix_desc['path']).open(newline='') as stream:
        rows = [row for row in csv.DictReader(stream) if row.get('catalog_id') == 'S074' and row.get('size_id') == 'Standard']
    if len(rows) != 1 or rows[0].get('objective_id') != 'BA' or any(
            rows[0].get(k) != v for k, v in {'player_rifle': '10', 'player_gunner': '6',
                                              'enemy_rifle': '10', 'enemy_gunner': '6'}.items()):
        raise ValueError('Exact authored S074 Standard BA 10/6 matrix row required')

    if not isinstance(req.get('binding'), dict) or req['binding'].get('sha256') != binding_desc['sha256'] or \
            binding_receipt.get('kind') != 'S074BothVehicleTenMetreOriginalCompilerLogicalBindingReplay' or \
            binding_receipt.get('status') != 'PassedSourceOwnerCompile' or \
            binding_receipt.get('setup_hash') != TARGET['setup_hash'] or \
            binding_receipt.get('binding') != binding_wrapper_desc or \
            binding_wrapper.get('source_owner_binding') != binding_desc or \
            binding_receipt.get('owner_proof') != pin(BINDING_PROOF) or \
            binding_receipt.get('mission_ready') is not False or \
            binding_receipt.get('native_context_qualified') is not False or \
            binding_receipt.get('source_fit_certified') is not False or binding_receipt.get('world_allocated') is not False or \
            binding_receipt.get('runtime_admission') is not False or binding_receipt.get('camera_framing_observed') is not False:
        raise ValueError('Exact r7 original-codec candidate binding replay required')
    verify_pin_map(binding_receipt.get('source_and_reference_pins'), 'Binding replay source/build')
    if binding_proof.get('kind') != 'S074BothVehicleTenMetreOriginalCompilerBindingReplay' or \
            binding_proof.get('status') != 'CompiledSourceOnlyCandidate' or \
            binding_proof.get('setup_hash_with_binding') != TARGET['setup_hash'] or \
            binding_proof.get('portable_roundtrip_setup_hash') != TARGET['setup_hash'] or \
            binding_proof.get('original_reader_decode_and_compiler_roundtrip') is not True or \
            binding_proof.get('camera_framing_unobserved') is not True or \
            binding_proof.get('world_allocated') is not False or binding_proof.get('mission_ready') is not False:
        raise ValueError('Bound owner proof must identify the exact both-vehicle ten-metre setup')
    if binding.get('CatalogId') != 'S074' or binding.get('DefinitionId') != 'skirmish.s074' or \
            binding.get('SourceOperationMapId') != TARGET['operation_map_id']:
        raise ValueError('Owner binding identity differs from S074 source target')

    factory_target = factory.get('target_context', {})
    if factory.get('kind') != 'S074BothVehicleTenMetreOriginalFactoryMetadataProjection' or \
            factory.get('status') != 'ProjectedSourceOnly' or any(factory_target.get(k) != v for k, v in TARGET.items()) or \
            factory_target.get('source_fingerprint') != result.get('source_fingerprint') or \
            factory_target.get('layout_fingerprint') != result.get('layout_fingerprint') or \
            factory_target.get('mission_logical_binding_fingerprint') != binding_receipt.get('mission_logical_binding_fingerprint') or \
            factory.get('binding_replay') != pin(BASE / 's074-both-vehicle-ten-metre-origin-binding-20261114-r1/receipt.json') or \
            factory.get('bound_author_result') != result_desc or \
            factory.get('source_factory_context') != factory_context_desc or \
            factory.get('projection_receipt') != pin(FACTORY_PROJECTION_RECEIPT) or \
            any(factory.get(k) is not False for k in ('world_allocated', 'native_context_qualified',
                    'runtime_loader_admitted', 'source_fit_certified', 'active_mission_water_qualified',
                    'mission_ready', 'camera_framing_observed')):
        raise ValueError('Retained factory projection is not the exact old candidate artifact')
    context_target = factory_context.get('target_context', {})
    if factory_context.get('kind') != 'OriginalCompilerDerivedNativeFactoryContext' or \
            any(context_target.get(k) != v for k, v in TARGET.items()) or \
            context_target.get('source_fingerprint') != result.get('source_fingerprint') or \
            context_target.get('layout_fingerprint') != result.get('layout_fingerprint') or \
            context_target.get('mission_logical_binding_fingerprint') != binding_receipt.get('mission_logical_binding_fingerprint') or \
            factory_context.get('world_allocated') is not False or \
            factory_context.get('identities_allocated') != 0 or factory_context.get('mission_ready') is not False:
        raise ValueError('Factory context target or source-only scope differs')
    if factory_projection.get('status') != 'ProjectedSourceOnly' or \
            factory_projection.get('target_context', {}).get('setup_hash') != TARGET['setup_hash'] or \
            any(factory_projection.get(k) is not False for k in ('world_allocated', 'runtime_admission',
                    'source_fit_certified', 'mission_ready')) or bridge.get('status') != 'PassedSourceOnlyContextAndFactoryDomainBridge':
        raise ValueError('Retained factory metadata projection or fresh context bridge failed')
    if FRESH_FACTORY_CONTEXT.read_bytes() != FACTORY_CONTEXT.read_bytes() or \
            fresh_factory.get('binding_replay') != binding_receipt_desc or \
            fresh_factory.get('bound_author_result') != result_desc:
        raise ValueError('Fresh factory exporter does not replay the current binding/result')

    if focused.get('kind') != 'S074BothVehicleTenMetreFocusedPrefabSourceOnlyProjectionReceipt' or \
            focused.get('status') != 'PassedSourceOnlyProjectionOfFailedRawCapture' or \
            focused.get('raw_capture_replay_ready') is not False or focused.get('runtime_admission') is not False or \
            focused.get('native_visual_readiness') is not False or focused.get('source_fit_certified') is not False or \
            focused.get('mission_ready') is not False or focused_proof.get('kind') != 'S074BothVehicleTenMetreFocusedPrefabSourceOnlyCaptureProof' or \
            focused_proof.get('target') != {
                'mission_id': 'S074', 'seed': 104803, 'size': 'Standard', 'difficulty': 'Regular',
                'setup_hash': TARGET['setup_hash'], 'operation_map_id': TARGET['operation_map_id']} or \
            focused_proof.get('source_context') != factory_context_desc or \
            focused_proof.get('configuration_qualification') != qualification_desc or \
            focused_proof.get('source_pins_verified_current') is not True or focused_proof.get('source_pin_count') != 8087 or \
            focused_proof.get('raw_capture_result') != 'Failed_UnconsumedPresentationUnityObjectRefs' or \
            focused_proof.get('raw_capture_pass_marker_present') is not False:
        raise ValueError('Focused prefab evidence must retain its failed raw-capture scope and exact target')
    if qualification.get('kind') != 'ExperimentalGroundConfigurationQualification' or \
            qualification.get('compiler_context') != factory_context_desc or \
            qualification.get('native_context_qualified') is not False or qualification.get('mission_ready') is not False:
        raise ValueError('Source-only profile descriptor differs from candidate factory context')

    if host.get('status') != 'Passed' or host.get('compile_exit') != 0 or host.get('mission') != 'S074' or \
            host.get('seed') != 104803:
        raise ValueError('Current passed S074 source owner build required')
    verify_pin_map(host.get('source_and_reference_pins'), 'Host source/build')
    if source_build.get('status') != 'Passed' or source_build.get('compile_exit') != 0 or \
            source_build.get('mission') != 'S074' or source_build.get('seed') != 104803 or \
            source_build.get('runtime_backend') != 'TypedGroundS026V1':
        raise ValueError('Fresh passed both-vehicle ten-metre S074 both-vehicle source build required')
    verify_pin_map(source_build.get('source_and_reference_pins'), 'Candidate runtime source/build')

    # The receipt is intentionally a compact source-owner join, not a readiness qualification.
    projection = read(FACTORY_PROJECTION_RECEIPT)
    building_defs = pin(FACTORY_DIR / 'projection/building-definitions.json')
    unit_meta = pin(FACTORY_DIR / 'projection/unit-production-metadata.json')
    building_requests = pin(FACTORY_DIR / 'projection/configured-metadata.json')
    if focused_descriptor.get('s074_capture_proof') != focused_proof_desc or \
            pin(focused_descriptor['path']) != {'path': focused_descriptor['path'],
                                               'sha256': focused_descriptor.get('sha256')} or \
            focused_descriptor.get('source_reference_sha256') != focused.get('raw_capture_sha256'):
        raise ValueError('Focused prefab reference-template descriptor does not bind the projected failed capture')
    receipts = {'author_request': request_desc, 'author_result': result_desc,
                'author_process_receipt': pin(AUTHOR_RECEIPT), 'layout': layout_desc,
                'candidate_layout_receipt': pin(CANDIDATE_RECEIPT), 'candidate_base_layout': pin(BASE_CANDIDATE_LAYOUT),
                'player_vehicle_kernel_rationale': pin(PLAYER_KERNEL_RECEIPT),
                'enemy_vehicle_kernel_rationale': pin(ENEMY_KERNEL_RECEIPT),
                'setup_matrix': matrix_desc, 'source_graph': graph_desc,
                'binding_replay_receipt': binding_receipt_desc, 'source_owner_binding': binding_desc,
                'portable_binding_wrapper': binding_wrapper_desc,
                'runtime_logical_binding_wrapper': runtime_wrapper_desc,
                'factory_projection_receipt': factory_receipt_desc,
                'factory_projection_detail_receipt': pin(FACTORY_PROJECTION_RECEIPT),
                'factory_context': factory_context_desc,
                'fresh_factory_receipt': pin(FRESH_FACTORY_RECEIPT),
                'fresh_factory_context': pin(FRESH_FACTORY_CONTEXT),
                'fresh_factory_projection_receipt': pin(FRESH_FACTORY_PROJECTION_RECEIPT),
                'focused_prefab_receipt': focused_receipt_desc, 'focused_prefab_proof': focused_proof_desc,
                'focused_prefab_descriptor': pin(FOCUSED_DESCRIPTOR),
                'building_definitions': building_defs, 'unit_production_metadata': unit_meta,
                'building_request_metadata': building_requests,
                'configuration_profile_descriptor': qualification_desc, 'host_source_build': host_desc,
                'candidate_source_build': pin(source_build_path)}
    verified_pin_count = len(graph['source_pins']) + len(binding_receipt['source_and_reference_pins']) + \
                         len(fresh_factory['source_and_reference_pins']) + len(host['source_and_reference_pins']) + \
                         len(source_build['source_and_reference_pins']) + 8087
    admission = {'schema_version': 1, 'kind': 'S074BothVehicleTenMetreSourceOwnerAdmissionReceipt',
                 'status': 'PassedSourceOwnerInputs_SourceOnly', 'target': TARGET,
                 'layout_fingerprint': result['layout_fingerprint'],
                 'source_fingerprint': result['source_fingerprint'],
                 'logical_binding_fingerprint': binding_receipt.get('mission_logical_binding_fingerprint'),
                 'verified_source_pin_count': verified_pin_count, 'inputs': receipts,
                 'binding_semantic_equal_to_retained_origin': binding_receipt.get('retained_origin_view_semantically_preserved') is True,
                 'original_factory_owner_checks': factory.get('checks'),
                 'focused_prefab_raw_failure_preserved': True,
                 'world_allocated': False, 'runtime_admission': False, 'native_capture': False,
                 'native_context_qualified': False, 'active_mission_water_qualified': False,
                 'source_fit_certified': False, 'camera_framing_observed': False, 'mission_ready': False,
                 'scope': 'Source-owner candidate inputs and immutable S026 factory projection only; no native qualification, runtime admission, current map-fit claim, or mission readiness.'}
    # Prepare the same runtime-facing GeneratedMissionInitialization shape as the
    # origin composer, substituting only source-owned both-vehicle ten-metre inputs.
    origin = importlib.util.spec_from_file_location(
        's074_origin_bundle_contract', ROOT / 'Tools/CI/SkirmishSourceSimulation/Recipes/compose_s074_origin_source_only_bundle.py')
    old = importlib.util.module_from_spec(origin); origin.loader.exec_module(old)
    profiles = req.get('profiles')
    for profile_path in profiles.values():
        matches = [k for k in graph['source_pins'] if Path(k).as_posix().endswith('/' + profile_path)]
        if len(matches) != 1 or pin(ROOT / profile_path)['sha256'] != graph['source_pins'][matches[0]]:
            raise ValueError(f'Author profile is outside current source graph: {profile_path}')
    setup_matrix = matrix_desc
    package_value = read(PACKAGE)
    package_pin = pin(PACKAGE)
    package_static = package_value['static_map']
    logical = pin(LOGICAL_MAP)
    static_map = {'operation_map_id': TARGET['operation_map_id'], 'definition': logical,
                  'grid_config': old.descriptor_from(package_static['grid_config']),
                  'surface_data': old.descriptor_from(package_static['surface_data'])}
    factory_native = factory_context.get('native_factory_reference')
    factory_origin = factory_context.get('native_origin_descriptor')
    native_metadata, native_origin = old.validate_native_factory_origin(
        factory_context, factory_native, factory_origin)
    death_audit = read(DEATH_AUDIT)
    death_pin = pin(DEATH)
    if death_audit.get('status') != 'Passed' or death_audit.get('readset', {}).get('sha256') != death_pin['sha256']:
        raise ValueError('Current exact S074 death readset audit required')
    text_owner = read(TEXT)
    text_sources = {}
    for captured, digest in text_owner.get('source_pins', {}).items():
        source = Path(captured)
        if not source.is_absolute() or not source.is_relative_to(TEXT_CAPTURE_ROOT):
            raise ValueError('Command-text capture pin escaped its owner root')
        current = ROOT / source.relative_to(TEXT_CAPTURE_ROOT)
        if pin(current)['sha256'] != digest:
            raise ValueError(f'Current shared command-text source drift: {current}')
        text_sources[str(current)] = digest
    clearance_value = read(CLEARANCE)
    if clearance_value.get('kind') != 'SourceDerivedCampaignMapDropClearanceProjection' or \
            clearance_value.get('source_fit_certified') is not False or clearance_value.get('mission_ready') is not False:
        raise ValueError('Current source-derived clearance projection required')
    init = {'schema_version': 1, 'kind': 'GeneratedMissionInitialization',
            'authority': 'AuthoredConfigAndBakedCampaignMap',
            'source_epoch': 'OriginalCompilerDerivedNativeFactoryContext/v1', 'mission_id': 'S074',
            'selection': {'difficulty_id': 2, 'size_id': 1, 'seed': TARGET['seed']},
            'source_config_graph': graph_desc, 'setup_matrix': setup_matrix,
            'canonical_battle_catalog': pin(CATALOG), 'selected_operation_map': logical, 'static_map': static_map,
            'world_profile': {'identity_backend': 'FreshLogicalEntitiesV1', 'system_initialization': 'OriginalOnCreate',
                              'future_native_inputs': False},
            'reference_templates': focused_descriptor, 'building_definitions': building_defs,
            'building_placement_config': pin(CONSTRUCTION), 'placement_domain': {},
            'resource_preset': pin(RESOURCE_PRESET), 'unit_production_metadata': unit_meta,
            'building_request_metadata': building_requests, 'operation_map_rotation_reference': pin(ROTATION),
            'metadata_compiler_context': factory_context_desc, 'configuration_qualification': qualification_desc,
            'metadata_compatibility': native_metadata, 'metadata_source_origin': native_origin,
            'typed_mission_configuration': {'profiles': profiles, 'campaign_source_path': req['campaign_source_path'],
                                            'layout': layout_desc},
            's074_candidate_author': {'request': request_desc, 'result': result_desc},
            'mission_logical_binding': runtime_wrapper_desc,
            'death_mechanical_inputs': death_pin | {'capture_root': str(ROOT)},
            'command_text_data': pin(TEXT)}
    init_path = out / 'initialization.json'
    out.mkdir(parents=True)
    bridge_path = out / 'factory-context-projection-bridge.json'
    bridge_path.write_text(json.dumps(bridge, indent=2, sort_keys=True) + '\n')
    admission['inputs']['factory_context_projection_bridge'] = pin(bridge_path)
    runtime_wrapper_path.write_bytes(runtime_wrapper_bytes)
    if pin(runtime_wrapper_path) != runtime_wrapper_desc:
        raise ValueError('Written runtime binding wrapper differs from its derived descriptor')
    init_path.write_text(json.dumps(init, indent=2) + '\n')
    init_descriptor = pin(init_path)
    admission['inputs']['initialization'] = init_descriptor
    (out / 'source-owner-admission-receipt.json').write_text(json.dumps(admission, indent=2, sort_keys=True) + '\n')
    admission_descriptor = pin(out / 'source-owner-admission-receipt.json')
    clear_pin = pin(CLEARANCE)
    text_pin = pin(TEXT)
    runtime_data = {'schema_version': 1, 'kind': 'CurrentRoadSceneRuntimeInputs', 'data_root': str(ROOT),
        'data_files': {p: sha(ROOT / p) for p in ('Assets/Game/Configs/Scene/Game_RoadBuild_Config.asset',
            'Assets/Game/Configs/Scene/Game_RoadBuild_Config.asset.meta', 'Assets/Game/Configs/Scene/Game_RuntimeCitySpawner_Config.asset',
            'Assets/Game/Configs/Scene/Game_RuntimeCitySpawner_Config.asset.meta')},
        'source_files': {p: sha(ROOT / p) for p in ('Assets/Game/Scripts/Configs/Scene/RoadBuildSystemSceneConfigAsset.cs.meta',
            'Assets/Game/Scripts/Configs/Scene/RuntimeCitySpawnerSystemSceneConfigAsset.cs.meta')}}
    bundle = {'schema_version': 1, 'kind': 'SkirmishSourceSimulationDataBundle', 'mission': 'S074',
        'seed': TARGET['seed'], 'runtime_backend': 'TypedGroundS026V1', 'runtime_data_inputs': runtime_data,
        'initialization': init_descriptor, 'unit_metadata': unit_meta, 'static_clearance': clear_pin,
        'source_derived_map': package_pin,
        'source_camera_policy': {'schema_version': 1, 'kind': 'SkirmishSourceCameraPolicy', 'mission': 'S074',
            'mode': 'NoCameraLogicalDeliveryDiagnostic', 'configuration_qualification': qualification_desc},
        'command_text_sources': {'captured_root': str(TEXT_CAPTURE_ROOT), 'current_root': str(ROOT),
            'input_pins': text_sources, 'artifact_sha256': text_pin['sha256']},
        's074_source_owner_admission': admission_descriptor,
        'nonclaims': {'world_allocated': False, 'native_capture': False, 'native_context_qualified': False,
            'runtime_admission': False, 'mission_ready': False, 'source_fit_certified': False,
            'active_mission_water_qualified': False, 'camera_qualified': False, 'camera_framing_observed': False}}
    bundle_path = out / 'data-bundle.json'
    bundle_path.write_text(json.dumps(bundle, indent=2) + '\n')
    # Pure read-only validator is shared with guard integration; no output is emitted by it.
    helper_path = ROOT / 'Tools/CI/SkirmishSourceSimulation/Recipes/validate_s074_source_owner_receipt_v1.py'
    spec = importlib.util.spec_from_file_location('s074_generic_source_owner_validator', helper_path)
    validator = importlib.util.module_from_spec(spec); spec.loader.exec_module(validator)
    validator.validate_s074_source_owner_receipt_v1(init, bundle, ROOT, pin(source_build_path))
    final = {'schema_version': 1, 'kind': 'S074BothVehicleTenMetreSourceOnlyBundleReceipt',
             'status': 'ComposedSourceOwnerInputs_SourceOnly', 'initialization': init_descriptor,
             'data_bundle': pin(bundle_path), 'source_owner_admission': admission_descriptor,
             'mission_ready': False, 'runtime_admission': False, 'native_context_qualified': False}
    (out / 'composition-receipt.json').write_text(json.dumps(final, indent=2, sort_keys=True) + '\n')
    return admission


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output-dir', type=Path, required=True)
    parser.add_argument('--author-request', type=Path, default=AUTHOR_REQUEST)
    parser.add_argument('--setup-matrix', type=Path, default=MATRIX)
    parser.add_argument('--binding-receipt', type=Path, default=BINDING_RECEIPT)
    parser.add_argument('--binding', type=Path, default=BINDING)
    parser.add_argument('--factory-receipt', type=Path, default=FACTORY_RECEIPT)
    parser.add_argument('--focused-prefab-receipt', type=Path, default=FOCUSED_RECEIPT)
    parser.add_argument('--source-build', type=Path, default=SOURCE_BUILD)
    args = parser.parse_args()
    admission = compose(args.output_dir, author_request=args.author_request, matrix_path=args.setup_matrix,
                        binding_receipt_path=args.binding_receipt, binding_path=args.binding,
                        factory_receipt_path=args.factory_receipt, focused_receipt_path=args.focused_prefab_receipt,
                        source_build_path=args.source_build)
    print(json.dumps({'status': admission['status'], 'target': admission['target'],
                      'source_pin_count': admission['verified_source_pin_count'], 'mission_ready': False}))


if __name__ == '__main__':
    try:
        main()
    except Exception as exc:
        import sys
        print('[S074BothVehicleTenMetreBundle] status=Failed ' + str(exc), file=sys.stderr)
        raise
