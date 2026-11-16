"""Read-only validation for the S074 eight-metre source-owner bundle joins."""
from pathlib import Path
import hashlib
import json
import csv

TARGET = {'catalog_id': 'S074', 'seed': 104803, 'size': 'Standard', 'difficulty': 'Regular',
          'setup_hash': 1559121027, 'operation_map_id': 'opmap.skirmish.industrial_basin'}
FALSE = ('world_allocated', 'runtime_admission', 'native_capture', 'native_context_qualified',
         'active_mission_water_qualified', 'source_fit_certified', 'camera_framing_observed', 'mission_ready')


def _pin(path):
    p = Path(path).resolve(strict=True)
    if p.is_symlink() or not p.is_file():
        raise ValueError(f'Canonical regular file required: {p}')
    return {'path': str(p), 'sha256': hashlib.sha256(p.read_bytes()).hexdigest()}


def _verify(desc, label):
    if not isinstance(desc, dict) or set(desc) != {'path', 'sha256'}:
        raise ValueError(f'Exact {label} path/hash descriptor required')
    actual = _pin(desc['path'])
    if actual != desc:
        raise ValueError(f'{label} descriptor drift: {desc.get("path")}')
    return actual


def _semantic(value):
    if isinstance(value, dict):
        return {k: _semantic(v) for k, v in value.items() if k not in ('name', 'hideFlags')}
    if isinstance(value, list):
        return [_semantic(v) for v in value]
    if isinstance(value, float):
        import struct
        return struct.unpack('!f', struct.pack('!f', value))[0]
    return value


def validate_s074_eight_metre_owner_inputs(initial, bundle, repo):
    """Validate candidate joins without writing files or changing runtime state.

    Returns the verified owner pin map for inclusion in normal run input pins.
    The admission receipt pins initialization, while the data bundle points to
    the receipt; the receipt deliberately never pins the data bundle.
    """
    root = Path(repo).resolve(strict=True)
    if not isinstance(initial, dict) or initial.get('mission_id') != 'S074' or \
            initial.get('source_epoch') != 'OriginalCompilerDerivedNativeFactoryContext/v1' or \
            initial.get('selection') != {'difficulty_id': 2, 'size_id': 1, 'seed': TARGET['seed']}:
        raise ValueError('Exact S074 eight-metre initialization target required')
    if not isinstance(bundle, dict) or bundle.get('kind') != 'SkirmishSourceSimulationDataBundle' or \
            bundle.get('mission') != 'S074' or bundle.get('seed') != TARGET['seed'] or \
            bundle.get('runtime_backend') != 'TypedGroundS026V1':
        raise ValueError('Exact typed-ground S074 runtime data bundle required')
    admission_descriptor = bundle.get('s074_source_owner_admission')
    initialization_descriptor = bundle.get('initialization')
    if not isinstance(admission_descriptor, dict) or not isinstance(initialization_descriptor, dict):
        raise ValueError('Bundle must pin initialization and source-owner admission receipt')
    admission_descriptor = _verify(admission_descriptor, 'source-owner admission receipt')
    initialization_descriptor = _verify(initialization_descriptor, 'initialization')
    on_disk_initial = json.loads(Path(initialization_descriptor['path']).read_bytes())
    if on_disk_initial != initial:
        raise ValueError('In-memory initialization differs from the pinned run request')
    admission = json.loads(Path(admission_descriptor['path']).read_bytes())
    if admission.get('kind') != 'S074EightMetreSourceOwnerAdmissionReceipt' or \
            admission.get('status') != 'PassedSourceOwnerInputs_SourceOnly' or admission.get('target') != TARGET or \
            admission.get('inputs', {}).get('initialization') != initialization_descriptor or \
            bundle.get('initialization') != initialization_descriptor:
        raise ValueError('Initialization, bundle and owner admission receipt are not joined exactly')
    if initial.get('source_config_graph') != admission['inputs'].get('source_graph') or \
            initial.get('setup_matrix') != admission['inputs'].get('setup_matrix') or \
            initial.get('typed_mission_configuration', {}).get('layout') != admission['inputs'].get('layout') or \
            initial.get('metadata_compiler_context') != admission['inputs'].get('factory_context') or \
            initial.get('mission_logical_binding') != admission['inputs'].get('runtime_logical_binding_wrapper'):
        raise ValueError('Runtime initialization does not consume exact owner-compiled inputs')
    if initial.get('configuration_qualification') != admission['inputs'].get('configuration_profile_descriptor'):
        raise ValueError('Runtime qualification descriptor differs from source-only candidate context')
    if initial.get('building_definitions') != admission['inputs'].get('building_definitions') or \
            initial.get('unit_production_metadata') != admission['inputs'].get('unit_production_metadata') or \
            initial.get('building_request_metadata') != admission['inputs'].get('building_request_metadata'):
        raise ValueError('Projected factory or focused-prefab data differs from the owner receipt')
    reference_templates = json.loads(Path(admission['inputs']['focused_prefab_descriptor']['path']).read_bytes())
    if initial.get('reference_templates') != reference_templates:
        raise ValueError('Initialization reference-template object differs from its pinned descriptor bytes')
    if admission.get('binding_semantic_equal_to_retained_origin') is not True or \
            admission.get('focused_prefab_raw_failure_preserved') is not True or \
            any(admission.get(k) is not False for k in FALSE):
        raise ValueError('Source-only receipt overstates native/runtime/fit/readiness gates')
    if any(bundle.get('nonclaims', {}).get(k) is not False for k in FALSE):
        raise ValueError('Bundle nonclaim flags must remain explicitly false')

    refs = admission['inputs']
    req = json.loads(Path(refs['author_request']['path']).read_bytes())
    author_result = json.loads(Path(refs['author_result']['path']).read_bytes())
    author_run = json.loads(Path(refs['author_process_receipt']['path']).read_bytes())
    layout = _verify(refs['layout'], 'candidate layout')
    matrix = _verify(refs['setup_matrix'], 'setup matrix')
    graph = json.loads(Path(refs['source_graph']['path']).read_bytes())
    if req.get('kind') != 'SkirmishMissionAuthoringPreflight' or \
            (req.get('catalog_id'), req.get('seed'), req.get('size'), req.get('difficulty')) != \
            ('S074', 104803, 'Standard', 'Regular') or req.get('layout') != layout or \
            req.get('setup_matrix') != matrix or req.get('graph') != refs['source_graph'] or \
            not isinstance(req.get('binding'), dict) or req['binding'].get('sha256') != refs['source_owner_binding']['sha256']:
        raise ValueError('Original owner author request does not bind current candidate layout/matrix/graph/binding')
    if author_result.get('status') != 'ConfigurationCompiled' or author_result.get('compiler_accepted') is not True or \
            author_result.get('catalog_id') != TARGET['catalog_id'] or author_result.get('seed') != TARGET['seed'] or \
            author_result.get('resolved_setup', {}).get('setup_hash') != TARGET['setup_hash'] or \
            author_result.get('world_allocated') is not False or author_result.get('source_fit_verified') is not False or \
            author_result.get('mission_ready') is not False or \
            author_result.get('layout_fingerprint') != admission.get('layout_fingerprint') or \
            author_result.get('source_fingerprint') != admission.get('source_fingerprint'):
        raise ValueError('Original compiler result does not prove the exact source-only candidate')
    if author_run.get('process_exit') != 0 or author_run.get('timed_out') is not False or \
            author_run.get('mission_ready') is not False or author_run.get('source_fit_verified') is not False:
        raise ValueError('Original source-owner authoring process did not pass source-only gates')
    if graph.get('authority') != 'AuthoredConfigAssets' or graph.get('compiled_setup') is not False or \
            graph.get('world_initialized') is not False or graph.get('native_world_state_used') is not False:
        raise ValueError('S074 source graph is compiled or carries runtime state')
    with Path(matrix['path']).open(newline='') as stream:
        rows = [row for row in csv.DictReader(stream) if row.get('catalog_id') == 'S074' and row.get('size_id') == 'Standard']
    if len(rows) != 1 or rows[0].get('objective_id') != 'BA' or any(
            rows[0].get(k) != v for k, v in {'player_rifle': '10', 'player_gunner': '6',
                                              'enemy_rifle': '10', 'enemy_gunner': '6'}.items()):
        raise ValueError('S074 owner matrix must retain exact Standard BA 10/6 roster')

    binding_receipt = json.loads(Path(refs['binding_replay_receipt']['path']).read_bytes())
    binding_wrapper = json.loads(Path(refs['portable_binding_wrapper']['path']).read_bytes())
    owner_binding = json.loads(Path(refs['source_owner_binding']['path']).read_bytes())
    proof_descriptor = binding_receipt.get('owner_proof')
    _verify(proof_descriptor, 'original C# binding owner proof')
    if binding_receipt.get('kind') != 'S074EightMetreOriginalCompilerLogicalBindingReplay' or \
            binding_receipt.get('status') != 'PassedSourceOwnerCompile' or \
            binding_receipt.get('setup_hash') != TARGET['setup_hash'] or \
            binding_receipt.get('binding') != refs['portable_binding_wrapper'] or \
            binding_wrapper.get('source_owner_binding') != refs['source_owner_binding'] or \
            binding_wrapper.get('target_context', {}).get('setup_hash') != TARGET['setup_hash'] or \
            owner_binding.get('CatalogId') != 'S074' or owner_binding.get('DefinitionId') != 'skirmish.s074' or \
            owner_binding.get('SourceOperationMapId') != TARGET['operation_map_id'] or \
            binding_receipt.get('mission_logical_binding_fingerprint') != admission.get('logical_binding_fingerprint') or \
            binding_receipt.get('retained_origin_view_semantically_preserved') is not True or \
            any(binding_receipt.get(k) is not False for k in ('world_allocated', 'placement_performed',
                    'runtime_admission', 'native_context_qualified', 'source_fit_certified', 'mission_ready',
                    'camera_framing_observed')):
        raise ValueError('Fresh original compiler/binding receipt semantics or gates are invalid')
    if binding_wrapper.get('kind') != 'OriginalCompilerDeclaredLogicalBinding' or \
            binding_wrapper.get('camera_framing_unobserved') is not True or \
            any(binding_wrapper.get(k) is not False for k in ('world_allocated', 'native_capture',
                    'native_context_qualified', 'runtime_admission', 'source_fit_certified', 'mission_ready')):
        raise ValueError('Portable binding wrapper must remain source-only and camera-unobserved')
    retained_binding_desc = binding_receipt.get('retained_origin_binding')
    retained_binding_desc = _verify(retained_binding_desc, 'retained origin binding')
    retained_binding = json.loads(Path(retained_binding_desc['path']).read_bytes())
    view_fields = ('MinHeight', 'MaxHeight', 'NormalHeight', 'BuildHeight', 'Pitch', 'Yaw',
                   'NormalFieldOfView', 'BuildFieldOfView', 'GroundReferenceY',
                   'MaximumGroundCenterSlantDistance', 'InfantryMinimumViewportHeight',
                   'PlaneMaximumViewportDimension', 'AircraftPitch', 'AircraftFieldOfView',
                   'AircraftMinimumDistance', 'AircraftMaximumDistance')
    owner_variant = owner_binding.get('Variants', [{}])[0]
    origin_variant = retained_binding.get('Variants', [{}])[0]
    owner_view = _semantic(owner_variant.get('View', {}))
    origin_view = _semantic(origin_variant.get('View', {}))
    if {k: owner_view.get(k) for k in view_fields} != {k: origin_view.get(k) for k in view_fields} or \
            _semantic(owner_variant.get('Framing')) != _semantic(origin_variant.get('Framing')) or \
            binding_wrapper.get('portable_binding', {}).get('kind') != 'DeclaredOriginalLogicalBindingFieldsWithInlineViews' or \
            binding_wrapper.get('portable_binding', {}).get('unity_object_ids_exported') is not False or \
            binding_wrapper.get('portable_binding', {}).get('float_encoding') != 'IEEE754_float32_hex_bits':
        raise ValueError('Portable owner binding differs from retained authored origin or codec')
    proof = json.loads(Path(proof_descriptor['path']).read_bytes())
    if proof.get('kind') != 'S074EightMetreOriginalCompilerBindingReplay' or \
            proof.get('setup_hash_with_binding') != TARGET['setup_hash'] or \
            proof.get('portable_roundtrip_setup_hash') != TARGET['setup_hash'] or \
            proof.get('logical_binding_fingerprint') != admission.get('logical_binding_fingerprint') or \
            proof.get('camera_framing_unobserved') is not True or proof.get('world_allocated') is not False or \
            proof.get('placement_performed') is not False or proof.get('mission_ready') is not False:
        raise ValueError('Original C# bound/portable setup proof is invalid')

    factory = json.loads(Path(refs['factory_projection_receipt']['path']).read_bytes())
    fresh_factory = json.loads(Path(refs['fresh_factory_receipt']['path']).read_bytes())
    context = json.loads(Path(refs['factory_context']['path']).read_bytes())
    fresh_context = json.loads(Path(refs['fresh_factory_context']['path']).read_bytes())
    runtime_wrapper = json.loads(Path(refs['runtime_logical_binding_wrapper']['path']).read_bytes())
    context_target = context.get('target_context', {})
    replay_context_desc = binding_wrapper.get('metadata_compiler_context')
    replay_context_desc = _verify(replay_context_desc, 'portable binding replay context')
    replay_context = json.loads(Path(replay_context_desc['path']).read_bytes())
    replay_target = binding_wrapper.get('target_context', {})
    shared_target_fields = ('catalog_id', 'definition_id', 'seed', 'size', 'difficulty', 'setup_hash',
                            'operation_map_id', 'source_content_hash', 'source_fingerprint',
                            'layout_fingerprint', 'mission_logical_binding_fingerprint')
    if factory.get('kind') != 'S074EightMetreOriginalFactoryMetadataProjection' or \
            factory.get('status') != 'ProjectedSourceOnly' or \
            any(factory.get('target_context', {}).get(k) != v for k, v in TARGET.items()) or \
            factory.get('source_factory_context') != refs['factory_context'] or \
            factory.get('bound_author_result') != refs['author_result'] or \
            factory.get('projection_receipt') != refs.get('factory_projection_detail_receipt') or \
            any(factory.get(k) is not False for k in ('world_allocated', 'native_context_qualified',
                    'runtime_loader_admitted', 'source_fit_certified', 'active_mission_water_qualified',
                    'mission_ready', 'camera_framing_observed')) or \
            context.get('kind') != 'OriginalCompilerDerivedNativeFactoryContext' or \
            any(context_target.get(k) != v for k, v in TARGET.items()) or \
            context_target.get('source_fingerprint') != admission.get('source_fingerprint') or \
            context_target.get('layout_fingerprint') != admission.get('layout_fingerprint') or \
            context_target.get('mission_logical_binding_fingerprint') != admission.get('logical_binding_fingerprint') or \
            context.get('world_allocated') is not False or context.get('identities_allocated') != 0 or \
            any(context.get(k) is not False for k in ('runtime_loader_admitted', 'native_S050_capture',
                    'active_mission_water_qualified', 'source_fit_certified', 'mission_ready')) or \
            replay_context.get('kind') != 'OriginalCompilerDerivedS074EightMetreCandidateSetupContext' or \
            replay_context.get('target_context') != replay_target or \
            replay_context.get('world_allocated') is not False or replay_context.get('identities_allocated') != 0 or \
            any(replay_context.get(k) is not False for k in ('runtime_loader_admitted', 'native_capture',
                    'native_context_qualified', 'source_fit_certified', 'active_mission_water_qualified',
                    'camera_framing_observed', 'mission_ready')) or \
            any(replay_target.get(k) != context_target.get(k) for k in shared_target_fields):
        raise ValueError('Retained original factory context/projection does not join the candidate')
    bridge_desc = refs.get('factory_context_projection_bridge')
    bridge = json.loads(Path(_verify(bridge_desc, 'factory context/projection bridge')['path']).read_bytes())
    if bridge.get('kind') != 'S074EightMetreFactoryContextProjectionBridge' or \
            bridge.get('status') != 'PassedSourceOnlyContextAndFactoryDomainBridge' or \
            bridge.get('retained_context') != refs['factory_context'] or \
            bridge.get('fresh_context') != refs['fresh_factory_context'] or \
            bridge.get('retained_factory_receipt') != refs['factory_projection_receipt'] or \
            bridge.get('fresh_factory_receipt') != refs['fresh_factory_receipt'] or \
            bridge.get('retained_projection_receipt') != refs.get('factory_projection_detail_receipt') or \
            bridge.get('fresh_projection_receipt') != refs.get('fresh_factory_projection_receipt') or \
            bridge.get('retained_stale_source_pins_promoted') is not False:
        raise ValueError('Explicit retained/fresh factory bridge receipt required')
    if any(bridge.get(k) is not False for k in ('world_allocated', 'runtime_admission',
            'native_context_qualified', 'source_fit_certified', 'mission_ready')):
        raise ValueError('Factory bridge must remain source-only')
    bridge_checks = bridge.get('checks', {})
    if any(bridge_checks.get(k) is not True for k in ('context_bytes_equal',
            'building_definitions_equal_after_descriptor_normalization',
            'unit_metadata_equal_after_descriptor_normalization',
            'configured_metadata_equal_after_descriptor_normalization',
            'fresh_factory_pin_map_current')):
        raise ValueError('Factory context/projection bridge checks are incomplete')
    fresh_target = fresh_context.get('target_context', {})
    if fresh_context.get('kind') != 'OriginalCompilerDerivedNativeFactoryContext' or \
            any(fresh_target.get(k) != v for k, v in TARGET.items()) or \
            context_target != fresh_target or \
            Path(refs['factory_context']['path']).read_bytes() != Path(refs['fresh_factory_context']['path']).read_bytes() or \
            fresh_factory.get('kind') != 'S074EightMetreOriginalFactoryMetadataProjection' or \
            fresh_factory.get('status') != 'ProjectedSourceOnly' or \
            fresh_factory.get('source_factory_context') != refs['fresh_factory_context'] or \
            fresh_factory.get('bound_author_result') != refs['author_result'] or \
            fresh_factory.get('binding_replay') != refs['binding_replay_receipt']:
        raise ValueError('Fresh original factory export does not match retained context and current binding')
    old_files = bridge.get('retained_projection_files', {})
    fresh_files = bridge.get('fresh_projection_files', {})
    for name, expected in (('building-definitions.json', refs['building_definitions']),
                           ('unit-production-metadata.json', refs['unit_production_metadata']),
                           ('configured-metadata.json', refs['building_request_metadata'])):
        if old_files.get(name) != expected:
            raise ValueError(f'Bridge retained metadata descriptor mismatch: {name}')
    for side in (old_files, fresh_files):
        for desc in side.values():
            _verify(desc, 'factory projection artifact')
    def normalized_projection_set(files, context_desc, building_desc):
        values = {name: json.loads(Path(files[name]['path']).read_bytes()) for name in
                  ('building-definitions.json', 'unit-production-metadata.json', 'configured-metadata.json')}
        for value in values.values():
            if value.get('metadata_compiler_context') != context_desc:
                raise ValueError('Projection compiler context descriptor differs from bridge side')
            value['metadata_compiler_context'] = {'path': '<bound-context>', 'sha256': context_desc['sha256']}
        values['configured-metadata.json']['reference'] = {
            'path': '<projected-building-definitions>', 'sha256': '<equal-projected-domain>'}
        return values
    old_norm = normalized_projection_set(old_files, refs['factory_context'], old_files['building-definitions.json'])
    fresh_norm = normalized_projection_set(fresh_files, refs['fresh_factory_context'], fresh_files['building-definitions.json'])
    if old_norm != fresh_norm:
        raise ValueError('Bridge projection domain differs after exact path normalization')
    old_source_pins = factory.get('source_and_reference_pins', {})
    actual_stale = []
    for path, digest in old_source_pins.items():
        p = Path(path)
        actual = _pin(p)['sha256'] if p.is_file() and not p.is_symlink() else 'missing'
        if actual != digest:
            actual_stale.append({'path': path, 'receipt_sha256': digest, 'current_sha256': actual})
    if actual_stale != bridge.get('retained_receipt_pin_drift') or \
            bridge.get('retained_receipt_pin_drift_count') != len(actual_stale) or \
            bridge.get('checks', {}).get('fresh_factory_pin_map_current') is not True:
        raise ValueError('Retained stale pins must be explicitly quarantined and reconciled')
    if fresh_factory.get('source_and_reference_pins') is None:
        raise ValueError('Fresh factory exporter source pin closure required')
    expected_runtime_wrapper = json.loads(json.dumps(binding_wrapper))
    expected_runtime_wrapper['metadata_compiler_context'] = refs['factory_context']
    expected_runtime_wrapper['target_context'] = context_target
    if runtime_wrapper != expected_runtime_wrapper or \
            runtime_wrapper.get('metadata_compiler_context') != refs['factory_context'] or \
            runtime_wrapper.get('target_context') != context_target or \
            runtime_wrapper.get('portable_binding') != binding_wrapper.get('portable_binding') or \
            runtime_wrapper.get('source_owner_binding') != refs['source_owner_binding']:
        raise ValueError('Runtime binding wrapper must differ only by joined original context and full target descriptors')
    for name in ('building_definitions', 'unit_production_metadata', 'building_request_metadata'):
        value = json.loads(Path(refs[name]['path']).read_bytes())
        if value.get('catalog_id') != 'S074' or value.get('setup_hash') != TARGET['setup_hash'] or \
                value.get('seed') != TARGET['seed']:
            raise ValueError(f'Factory projected metadata target mismatch: {name}')

    focused_receipt = json.loads(Path(refs['focused_prefab_receipt']['path']).read_bytes())
    focused_proof = json.loads(Path(refs['focused_prefab_proof']['path']).read_bytes())
    focused_descriptor = json.loads(Path(refs['focused_prefab_descriptor']['path']).read_bytes())
    domain_descriptor = {'path': focused_descriptor.get('path'), 'sha256': focused_descriptor.get('sha256')}
    proof_descriptor = focused_descriptor.get('s074_capture_proof')
    source_reference = {'path': focused_descriptor.get('source_reference_path'),
                        'sha256': focused_descriptor.get('source_reference_sha256')}
    projection_manifest = {'path': focused_descriptor.get('projection_manifest_path'),
                           'sha256': focused_descriptor.get('projection_manifest_sha256')}
    _verify(domain_descriptor, 'focused-prefab domain')
    _verify(proof_descriptor, 'focused-prefab proof')
    _verify(source_reference, 'raw captured prefab reference')
    _verify(projection_manifest, 'focused-prefab projection manifest')
    if focused_receipt.get('kind') != 'S074FocusedPrefabSourceOnlyProjectionReceipt' or \
            focused_receipt.get('status') != 'PassedSourceOnlyProjectionOfFailedRawCapture' or \
            focused_receipt.get('raw_capture_replay_ready') is not False or \
            focused_receipt.get('runtime_admission') is not False or \
            focused_receipt.get('native_visual_readiness') is not False or \
            focused_receipt.get('source_fit_certified') is not False or focused_receipt.get('mission_ready') is not False or \
            focused_proof.get('target') != {'mission_id': 'S074', 'seed': 104803, 'size': 'Standard',
                'difficulty': 'Regular', 'setup_hash': TARGET['setup_hash'],
                'operation_map_id': TARGET['operation_map_id']} or \
            focused_proof.get('source_context') != refs['factory_context'] or \
            focused_proof.get('configuration_qualification') != refs['configuration_profile_descriptor'] or \
            focused_proof.get('source_pins_verified_current') is not True or focused_proof.get('source_pin_count') != 8087 or \
            focused_proof.get('raw_capture_result') != 'Failed_UnconsumedPresentationUnityObjectRefs' or \
            focused_proof.get('raw_capture_pass_marker_present') is not False or \
            focused_descriptor.get('s074_capture_proof') != refs['focused_prefab_proof'] or \
            focused_receipt.get('projection_descriptor') != refs['focused_prefab_descriptor'] or \
            focused_receipt.get('raw_capture') != source_reference:
        raise ValueError('Focused prefab projection must preserve exact target and failed raw capture')
    qualification = json.loads(Path(refs['configuration_profile_descriptor']['path']).read_bytes())
    if qualification.get('kind') != 'ExperimentalGroundConfigurationQualification' or \
            qualification.get('runtime_backend') != 'TypedGroundS026V1' or \
            qualification.get('scope') != 'OriginalCompilerAndQualifiedFactoryInputs_SourceOnly' or \
            qualification.get('compiler_context') != refs['factory_context'] or \
            qualification.get('native_context_qualified') is not False or qualification.get('mission_ready') is not False:
        raise ValueError('Candidate source qualification is not bound to current factory context')

    pins = {}
    for desc, label in ((admission_descriptor, 'source-owner admission receipt'),
                        (initialization_descriptor, 'initialization')):
        actual = _verify(desc, label)
        pins[actual['path']] = actual['sha256']
    for label, desc in admission['inputs'].items():
        actual = _verify(desc, label)
        if actual['path'] in pins and pins[actual['path']] != actual['sha256']:
            raise ValueError(f'Conflicting source-owner descriptor: {actual["path"]}')
        pins[actual['path']] = actual['sha256']
    for key, root_path in (('source_graph', 'source_pins'), ('binding_replay_receipt', 'source_and_reference_pins'),
                           ('fresh_factory_receipt', 'source_and_reference_pins'),
                           ('host_source_build', 'source_and_reference_pins'),
                           ('candidate_source_build', 'source_and_reference_pins')):
        source = json.loads(Path(admission['inputs'][key]['path']).read_bytes())
        source_map = source.get(root_path)
        if not isinstance(source_map, dict) or not source_map:
            raise ValueError(f'{key} lacks its expected closed source pin map')
        for path, digest in source_map.items():
            p = Path(path) if Path(path).is_absolute() else root / path
            actual = _pin(p)
            if actual['sha256'] != digest:
                raise ValueError(f'{key} source pin drift: {path}')
            if actual['path'] in pins and pins[actual['path']] != digest:
                raise ValueError(f'Conflicting source-owner pin: {path}')
            pins[actual['path']] = digest
        if key in ('host_source_build', 'candidate_source_build') and (
                source.get('status') != 'Passed' or source.get('compile_exit') != 0 or
                source.get('mission') != 'S074' or source.get('seed') != TARGET['seed'] or
                source.get('runtime_backend') != 'TypedGroundS026V1'):
            raise ValueError(f'{key} is not a passed build for the exact S074 target')
        if key in ('host_source_build', 'candidate_source_build'):
            executable = source.get('executable')
            if not isinstance(executable, dict) or set(executable) != {'path', 'sha256', 'bytes'}:
                raise ValueError(f'{key} executable pin is invalid')
            actual_executable = _pin(executable['path'])
            if actual_executable['sha256'] != executable['sha256'] or \
                    Path(executable['path']).stat().st_size != executable['bytes']:
                raise ValueError(f'{key} executable pin is stale')
            pins[actual_executable['path']] = actual_executable['sha256']
    for path, descriptor in author_result.get('input_pins', {}).items():
        digest = descriptor.get('sha256') if isinstance(descriptor, dict) else None
        actual = _pin(path if Path(path).is_absolute() else root / path)
        if not isinstance(digest, str) or actual['sha256'] != digest:
            raise ValueError(f'Original compiler author input pin drift: {path}')
        if actual['path'] in pins and pins[actual['path']] != digest:
            raise ValueError(f'Conflicting author input pin: {path}')
        pins[actual['path']] = digest
    raw_capture = json.loads(Path(focused_receipt['raw_capture']['path']).read_bytes())
    source_pins = raw_capture.get('source_pins')
    if not isinstance(source_pins, dict) or len(source_pins) != 8087:
        raise ValueError('Exact current focused-prefab projection source pin map required')
    for path, digest in source_pins.items():
        p = Path(path) if Path(path).is_absolute() else root / path
        actual = _pin(p)
        if actual['sha256'] != digest:
            raise ValueError(f'Focused-prefab source pin drift: {path}')
        if actual['path'] in pins and pins[actual['path']] != digest:
            raise ValueError(f'Conflicting focused-prefab source pin: {path}')
        pins[actual['path']] = digest
    return pins
