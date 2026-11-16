"""Receipt-driven, source-only admission for compiled S074 layout candidates.

This validator deliberately derives candidate identity from the pinned author
result and original factory context. It does not whitelist setup hashes or
candidate run names. The admission receipt pins initialization; the bundle
pins both, avoiding a circular receipt/bundle dependency.
"""
from pathlib import Path
import hashlib
import json

FALSE = ('world_allocated', 'runtime_admission', 'native_capture',
         'native_context_qualified', 'active_mission_water_qualified',
         'source_fit_certified', 'camera_framing_observed', 'mission_ready')
DESCRIPTORS = ('author_process_receipt', 'author_request', 'author_result',
               'binding_replay_receipt', 'building_definitions',
               'building_request_metadata', 'candidate_source_build',
               'configuration_profile_descriptor', 'factory_context',
               'factory_context_projection_bridge', 'factory_projection_detail_receipt',
               'factory_projection_receipt', 'focused_prefab_descriptor',
               'focused_prefab_proof', 'focused_prefab_receipt',
               'fresh_factory_context', 'fresh_factory_projection_receipt',
               'fresh_factory_receipt', 'host_source_build', 'initialization',
               'layout', 'portable_binding_wrapper', 'runtime_logical_binding_wrapper',
               'setup_matrix', 'source_graph', 'source_owner_binding',
               'unit_production_metadata')


def _pairs(items):
    result = {}
    for key, value in items:
        if key in result:
            raise ValueError(f'Duplicate JSON property: {key}')
        result[key] = value
    return result


def _constant(value):
    raise ValueError(f'Non-finite JSON number forbidden: {value}')


def _read(desc, label, root=None, allow_external=False):
    if not isinstance(desc, dict) or set(desc) != {'path', 'sha256'}:
        raise ValueError(f'Exact {label} descriptor required')
    path = Path(desc['path'])
    if not path.is_absolute() or path.is_symlink() or not path.is_file():
        raise ValueError(f'Canonical regular {label} required')
    resolved = path.resolve(strict=True)
    if str(resolved) != str(path):
        raise ValueError(f'Canonical {label} path required')
    if root is not None and not allow_external and not resolved.is_relative_to(Path(root).resolve(strict=True)):
        raise ValueError(f'{label} must be within the trusted repository root')
    data = path.read_bytes()
    digest = hashlib.sha256(data).hexdigest()
    if digest != desc['sha256']:
        raise ValueError(f'{label} changed beneath receipt')
    try:
        value = json.loads(data, object_pairs_hook=_pairs, parse_constant=_constant)
    except (json.JSONDecodeError, UnicodeDecodeError):
        value = data
    return value, {'path': str(path), 'sha256': digest}


def _verify(desc, label, root=None, allow_external=False):
    _, pin = _read(desc, label, root, allow_external)
    return pin


def preflight_s074_selected_build(bundle, repo, selected_build):
    """Reject a mismatched declared owner build early; never grant admission."""
    if not isinstance(bundle, dict) or bundle.get('mission') != 'S074' or 's074_source_owner_admission' not in bundle:
        return False
    root = Path(repo).resolve(strict=True)
    # Oversized inputs fall through to the authoritative checks; this optional
    # rejection-only preflight must not turn an input check into a large read.
    for key, limit in (('initialization', 16 * 1024 * 1024), ('s074_source_owner_admission', 4 * 1024 * 1024)):
        desc = bundle.get(key)
        if isinstance(desc, dict) and isinstance(desc.get('path'), str):
            path = Path(desc['path'])
            if path.is_file() and path.stat().st_size > limit:
                return False
    initial, _ = _read(bundle.get('initialization'), 'initialization', root)
    if not isinstance(initial, dict) or 's074_candidate_author' not in initial:
        return False
    admission, _ = _read(bundle.get('s074_source_owner_admission'), 'owner admission receipt', root)
    if not isinstance(admission, dict) or not isinstance(admission.get('inputs'), dict):
        raise ValueError('Complete owner receipt descriptor set required')
    if selected_build != admission['inputs'].get('candidate_source_build'):
        raise ValueError('Selected runtime build differs from owner receipt build')
    return True


def validate_s074_source_owner_receipt_v1(initial, bundle, repo, selected_build):
    """Validate a bundle joined to one compiled layout receipt; return live pins."""
    root = Path(repo).resolve(strict=True)
    if not isinstance(initial, dict) or initial.get('mission_id') != 'S074' or \
            initial.get('source_epoch') != 'OriginalCompilerDerivedNativeFactoryContext/v1':
        raise ValueError('S074 original-compiler source context required')
    if not isinstance(bundle, dict) or bundle.get('kind') != 'SkirmishSourceSimulationDataBundle' or \
            bundle.get('mission') != 'S074' or bundle.get('runtime_backend') != 'TypedGroundS026V1':
        raise ValueError('Typed-ground S074 source bundle required')
    admission_desc = bundle.get('s074_source_owner_admission')
    initialization_desc = bundle.get('initialization')
    if not isinstance(admission_desc, dict) or not isinstance(initialization_desc, dict):
        raise ValueError('Bundle must pin admission and initialization')
    admission, admission_pin = _read(admission_desc, 'owner admission receipt', root)
    pinned_initial, initial_pin = _read(initialization_desc, 'initialization', root)
    admission_kind = admission.get('kind')
    if pinned_initial != initial or admission.get('schema_version') != 1 or \
            not isinstance(admission_kind, str) or not admission_kind.startswith('S074') or \
            not admission_kind.endswith('SourceOwnerAdmissionReceipt') or \
            admission.get('status') != 'PassedSourceOwnerInputs_SourceOnly' or \
            admission.get('inputs', {}).get('initialization') != initial_pin or \
            bundle.get('initialization') != initial_pin:
        raise ValueError('Bundle, receipt and initialization do not join')
    refs = admission.get('inputs')
    if not isinstance(refs, dict) or not set(DESCRIPTORS).issubset(refs):
        raise ValueError('Complete owner receipt descriptor set required')
    candidate_author = initial.get('s074_candidate_author')
    if not isinstance(candidate_author, dict) or set(candidate_author) != {'request', 'result'} or \
            candidate_author.get('request') != refs.get('author_request') or \
            candidate_author.get('result') != refs.get('author_result'):
        raise ValueError('Initialization candidate-author descriptors must exactly join receipt author inputs')
    if selected_build != refs.get('candidate_source_build'):
        raise ValueError('Selected runtime build differs from owner receipt build')
    pins = {admission_pin['path']: admission_pin['sha256'], initial_pin['path']: initial_pin['sha256']}
    docs = {}
    for name in DESCRIPTORS:
        doc, pin = _read(refs[name], name, root)
        docs[name] = doc
        pins[pin['path']] = pin['sha256']

    req, result, run = docs['author_request'], docs['author_result'], docs['author_process_receipt']
    context = docs['factory_context']
    target = context.get('target_context')
    setup_hash = result.get('resolved_setup', {}).get('setup_hash')
    if not isinstance(setup_hash, int) or isinstance(setup_hash, bool):
        raise ValueError('Original compiler result must provide integer setup identity')
    if not isinstance(target, dict) or target.get('setup_hash') != setup_hash:
        raise ValueError('Original factory context setup does not derive from compiler result')
    identity = {'catalog_id': result.get('catalog_id'), 'seed': result.get('seed'),
                'size': result.get('size'), 'difficulty': result.get('difficulty'),
                'setup_hash': setup_hash, 'operation_map_id': result.get('operation_map_id')}
    if identity['catalog_id'] != 'S074' or identity['operation_map_id'] != 'opmap.skirmish.industrial_basin' or \
            identity['size'] != 'Standard' or identity['difficulty'] != 'Regular' or \
            bundle.get('seed') != identity['seed'] or initial.get('selection') != {
                'difficulty_id': 2, 'size_id': 1, 'seed': identity['seed']}:
        raise ValueError('Compiled S074 mission identity mismatch')
    if req.get('kind') != 'SkirmishMissionAuthoringPreflight' or any(
            req.get(k) != result.get(k) for k in ('catalog_id', 'seed', 'size', 'difficulty')) or \
            req.get('layout') != refs['layout'] or req.get('setup_matrix') != refs['setup_matrix'] or \
            req.get('graph') != refs['source_graph'] or \
            req.get('binding', {}).get('sha256') != refs['source_owner_binding'].get('sha256'):
        raise ValueError('Original author request must bind the receipt layout/matrix/graph')
    if result.get('status') != 'ConfigurationCompiled' or result.get('compiler_accepted') is not True or \
            result.get('world_allocated') is not False or result.get('source_fit_verified') is not False or \
            result.get('mission_ready') is not False or run.get('process_exit') != 0 or run.get('timed_out') is not False:
        raise ValueError('Original compiler/process source-only gates failed')
    if result.get('layout_fingerprint') != admission.get('layout_fingerprint') or \
            result.get('source_fingerprint') != admission.get('source_fingerprint') or \
            target.get('layout_fingerprint') != admission.get('layout_fingerprint') or \
            target.get('source_fingerprint') != admission.get('source_fingerprint'):
        raise ValueError('Layout/source fingerprints do not join compiler, admission and context')
    if context.get('kind') != 'OriginalCompilerDerivedNativeFactoryContext' or \
            context.get('authority') != 'ActualSourceCompilerContext_ImmutableNativeFactoryFacts_NotNativeS050' or \
            any(target.get(k) != v for k, v in {'catalog_id': identity['catalog_id'],
                'seed': identity['seed'], 'size': identity['size'], 'difficulty': identity['difficulty'],
                'operation_map_id': identity['operation_map_id']}.items()) or \
            context.get('world_allocated') is not False or context.get('identities_allocated') != 0 or \
            any(context.get(k) is not False for k in ('runtime_loader_admitted', 'native_S050_capture',
                'active_mission_water_qualified', 'source_fit_certified', 'mission_ready')):
        raise ValueError('Original factory context authority or source-only gates invalid')

    # Every author/compiler input pin is still current. Do not promote historical
    # pins from the retained factory projection as current evidence.
    author_pins = result.get('input_pins')
    if not isinstance(author_pins, dict) or not author_pins:
        raise ValueError('Original compiler input closure required')
    for path, descriptor in author_pins.items():
        expected = descriptor.get('sha256') if isinstance(descriptor, dict) else descriptor
        actual = _verify({'path': path, 'sha256': expected}, 'original compiler input', root)
        pins[actual['path']] = actual['sha256']
    build = docs['candidate_source_build']
    if build.get('kind') != 'SkirmishSourceSimulationBuild' or build.get('mission') != 'S074' or \
            build.get('status') != 'Passed' or build.get('compile_exit') != 0:
        raise ValueError('Current source build receipt did not pass')
    for closure_name, closure in (('build', build.get('source_and_reference_pins')),
                                  ('binding replay', docs['binding_replay_receipt'].get('source_and_reference_pins'))):
        if not isinstance(closure, dict) or not closure:
            raise ValueError(f'{closure_name} source/reference closure required')
        for path, digest in closure.items():
            current = _verify({'path': path, 'sha256': digest}, f'{closure_name} source input', root, allow_external=True)
            pins[current['path']] = current['sha256']

    # Original C# binding replay and portable serialization must both resolve
    # to the compiler-produced setup and preserve the retained origin fields.
    replay = docs['binding_replay_receipt']
    wrapper = docs['portable_binding_wrapper']
    if replay.get('status') != 'PassedSourceOwnerCompile' or replay.get('setup_hash') != setup_hash or \
            replay.get('retained_origin_view_semantically_preserved') is not True or \
            replay.get('mission_logical_binding_fingerprint') != admission.get('logical_binding_fingerprint') or \
            any(replay.get(k) is not False for k in ('world_allocated', 'runtime_admission',
                'native_context_qualified', 'source_fit_certified', 'mission_ready', 'camera_framing_observed')):
        raise ValueError('Original compiler binding replay did not prove source-only roundtrip')
    proof, proof_pin = _read(replay.get('owner_proof'), 'original binding proof', root)
    pins[proof_pin['path']] = proof_pin['sha256']
    if proof.get('setup_hash_with_binding') != setup_hash or proof.get('portable_roundtrip_setup_hash') != setup_hash or \
            proof.get('logical_binding_fingerprint') != admission.get('logical_binding_fingerprint') or \
            proof.get('mission_ready') is not False or wrapper.get('kind') != 'OriginalCompilerDeclaredLogicalBinding' or \
            wrapper.get('source_owner_binding') != refs['source_owner_binding'] or \
            any(wrapper.get(k) is not False for k in ('world_allocated', 'runtime_admission', 'native_capture',
                'native_context_qualified', 'source_fit_certified', 'mission_ready')):
        raise ValueError('Portable binding proof or false gates invalid')
    initial_joins = {
        'source_config_graph': 'source_graph', 'setup_matrix': 'setup_matrix',
        'metadata_compiler_context': 'factory_context',
        'mission_logical_binding': 'runtime_logical_binding_wrapper',
        'building_definitions': 'building_definitions',
        'unit_production_metadata': 'unit_production_metadata',
        'building_request_metadata': 'building_request_metadata',
        'configuration_qualification': 'configuration_profile_descriptor'}
    if any(initial.get(key) != refs[descriptor] for key, descriptor in initial_joins.items()) or \
            initial.get('typed_mission_configuration', {}).get('layout') != refs['layout']:
        raise ValueError('Runtime initialization differs from receipt-bound owner inputs')
    runtime_wrapper = docs['runtime_logical_binding_wrapper']
    expected_wrapper = json.loads(json.dumps(wrapper))
    expected_wrapper['metadata_compiler_context'] = refs['factory_context']
    expected_wrapper['target_context'] = target
    if runtime_wrapper != expected_wrapper:
        raise ValueError('Runtime wrapper may only join original factory context and its full target')

    # Fresh factory projection/context are required; retained stale pins are
    # represented as drift on the bridge, never accepted as current.
    bridge = docs['factory_context_projection_bridge']
    if bridge.get('retained_stale_source_pins_promoted') is not False or \
            bridge.get('checks', {}).get('fresh_factory_pin_map_current') is not True:
        raise ValueError('Factory bridge must quarantine stale pins and verify fresh pins')
    fresh_factory = docs['fresh_factory_receipt']
    retained_factory = docs['factory_projection_receipt']
    fresh_ctx = docs['fresh_factory_context']
    bridge = docs['factory_context_projection_bridge']
    for projection in (retained_factory, fresh_factory):
        if projection.get('status') != 'ProjectedSourceOnly' or \
                projection.get('bound_author_result') != refs['author_result'] or \
                projection.get('target_context', {}).get('setup_hash') != setup_hash or \
                any(projection.get(k) is not False for k in ('world_allocated', 'native_context_qualified',
                    'runtime_loader_admitted', 'source_fit_certified', 'active_mission_water_qualified', 'mission_ready')):
            raise ValueError('Factory projection does not directly bind compiler result and source-only target')
    if fresh_factory.get('source_factory_context') != refs['fresh_factory_context'] or \
            fresh_factory.get('source_factory_context') != refs['fresh_factory_context'] or \
            fresh_ctx.get('target_context', {}).get('setup_hash') != setup_hash or \
            fresh_ctx.get('target_context', {}).get('layout_fingerprint') != admission.get('layout_fingerprint'):
        raise ValueError('Fresh factory projection/context does not join this compiled candidate')
    if bridge.get('retained_context') != refs['factory_context'] or \
            bridge.get('fresh_context') != refs['fresh_factory_context'] or \
            bridge.get('retained_factory_receipt') != refs['factory_projection_receipt'] or \
            bridge.get('fresh_factory_receipt') != refs['fresh_factory_receipt'] or \
            bridge.get('retained_stale_source_pins_promoted') is not False or \
            bridge.get('checks', {}).get('fresh_factory_pin_map_current') is not True:
        raise ValueError('Retained/fresh factory bridge does not join exact descriptors')
    required_bridge_checks = ('context_bytes_equal',
        'building_definitions_equal_after_descriptor_normalization',
        'unit_metadata_equal_after_descriptor_normalization',
        'configured_metadata_equal_after_descriptor_normalization',
        'fresh_factory_pin_map_current')
    if any(bridge.get('checks', {}).get(k) is not True for k in required_bridge_checks):
        raise ValueError('Factory bridge semantic checks incomplete')
    retained_files, fresh_files = bridge.get('retained_projection_files'), bridge.get('fresh_projection_files')
    if not isinstance(retained_files, dict) or not isinstance(fresh_files, dict):
        raise ValueError('Factory bridge projection file maps required')
    def normalized(files, context_desc):
        names = ('building-definitions.json', 'unit-production-metadata.json', 'configured-metadata.json')
        values = {}
        for name in names:
            doc, _ = _read(files.get(name), 'factory projection artifact', root)
            if doc.get('metadata_compiler_context') != context_desc:
                raise ValueError('Projection context differs from bridge side')
            if name == 'configured-metadata.json':
                building_desc = files.get('building-definitions.json')
                reference = doc.get('reference')
                if not isinstance(reference, dict) or reference.get('path') != building_desc.get('path') or \
                        reference.get('sha256') != building_desc.get('sha256'):
                    raise ValueError('Configured metadata reference must bind side-local building definitions')
            doc['metadata_compiler_context'] = {'path': '<bound-context>', 'sha256': context_desc['sha256']}
            values[name] = doc
        values['configured-metadata.json']['reference'] = {
            'path': '<projected-building-definitions>', 'sha256': '<equal-projected-domain>'}
        return values
    retained_context_doc, retained_context_pin = _read(refs['factory_context'], 'retained factory context', root)
    fresh_context_doc, fresh_context_pin = _read(refs['fresh_factory_context'], 'fresh factory context', root)
    if retained_context_pin['sha256'] != fresh_context_pin['sha256'] or retained_context_doc != fresh_context_doc:
        raise ValueError('Retained and fresh factory context bytes differ')
    if normalized(retained_files, refs['factory_context']) != normalized(fresh_files, refs['fresh_factory_context']):
        raise ValueError('Retained and fresh factory projection domains differ')
    fresh_pins = fresh_factory.get('source_and_reference_pins')
    if not isinstance(fresh_pins, dict) or not fresh_pins:
        raise ValueError('Fresh factory source/reference closure required')
    for path, digest in fresh_pins.items():
        current = _verify({'path': path, 'sha256': digest}, 'fresh factory source input', root, allow_external=True)
        pins[current['path']] = current['sha256']

    focused = docs['focused_prefab_receipt']
    focused_proof = docs['focused_prefab_proof']
    raw_desc = focused.get('raw_capture')
    if focused.get('status') != 'PassedSourceOnlyProjectionOfFailedRawCapture' or \
            focused.get('raw_capture_failure_preserved') is not True or \
            focused_proof.get('status') != 'PassedSourceOnlyProjectionOfFailedRawCapture' or \
            not isinstance(focused_proof.get('raw_capture_result'), str) or \
            not focused_proof.get('raw_capture_result').startswith('Failed') or \
            focused_proof.get('raw_capture_pass_marker_present') is not False or \
            focused_proof.get('source_pins_verified_current') is not True:
        raise ValueError('Focused prefab proof must retain failed raw capture status')
    focus_target = focused_proof.get('target', {})
    focused_identity = {'mission_id': identity['catalog_id'], 'seed': identity['seed'],
                        'size': identity['size'], 'difficulty': identity['difficulty'],
                        'setup_hash': setup_hash, 'operation_map_id': identity['operation_map_id']}
    if focus_target != focused_identity or \
            focused_proof.get('source_context') != refs['factory_context'] or \
            focused_proof.get('configuration_qualification') != refs['configuration_profile_descriptor'] or \
            focused_proof.get('source_config_graph') != refs['source_graph'] or \
            focused_proof.get('building_definitions') != refs['building_definitions'] or \
            focused_proof.get('unit_production_metadata') != refs['unit_production_metadata'] or \
            focused_proof.get('unsupported_vfx_rows_preserved') != 23 or \
            focused_proof.get('exact_unconsumed_fields') != {
                'Game.Components.UnitAttackImpactVfxReference/Prefab': 10,
                'Game.Components.UnitMuzzleFlashVfxReference/Prefab': 9,
                'Game.Components.GroundMissileLauncherVfxReferenceComponent/ImpactExplosionPrefab': 1,
                'Game.Components.GroundMissileLauncherVfxReferenceComponent/ImpactSmokePrefab': 1,
                'Game.Components.GroundMissileLauncherVfxReferenceComponent/LauncherBackfirePrefab': 1,
                'Game.Components.GroundMissileLauncherVfxReferenceComponent/RocketTrailPrefab': 1}:
        raise ValueError('Focused prefab proof context or unconsumed presentation domain changed')
    raw_capture, raw_pin = _read(raw_desc, 'failed raw focused capture', root)
    pins[raw_pin['path']] = raw_pin['sha256']
    focused_descriptor = docs['focused_prefab_descriptor']
    domain_doc, domain_pin = _read({'path': focused_descriptor.get('path'),
        'sha256': focused_descriptor.get('sha256')}, 'focused prefab domain', root)
    if focused_descriptor.get('s074_capture_proof') != refs['focused_prefab_proof'] or \
            focused_descriptor.get('source_reference_path') != raw_desc.get('path') or \
            focused_descriptor.get('source_reference_sha256') != raw_desc.get('sha256'):
        raise ValueError('Focused descriptor does not bind exact failed raw capture and proof')
    proof_ref, proof_ref_pin = _read(focused_descriptor.get('s074_capture_proof'), 'embedded focused proof', root)
    source_ref, source_ref_pin = _read({'path': focused_descriptor.get('source_reference_path'),
        'sha256': focused_descriptor.get('source_reference_sha256')}, 'focused raw source reference', root)
    manifest_ref, manifest_pin = _read({'path': focused_descriptor.get('projection_manifest_path'),
        'sha256': focused_descriptor.get('projection_manifest_sha256')}, 'focused projection manifest', root)
    if proof_ref_pin != {'path': refs['focused_prefab_proof']['path'], 'sha256': refs['focused_prefab_proof']['sha256']} or \
            source_ref_pin != raw_pin or proof_ref != focused_proof or source_ref != raw_capture or \
            manifest_ref.get('source_path') != focused_descriptor.get('source_reference_path') or \
            manifest_ref.get('source_sha256') != focused_descriptor.get('source_reference_sha256') or \
            manifest_ref.get('slice_path') != focused_descriptor.get('path') or \
            manifest_ref.get('slice_sha256') != focused_descriptor.get('sha256'):
        raise ValueError('Focused domain/proof/raw/manifest identity mismatch')
    pins[domain_pin['path']] = domain_pin['sha256']
    pins[proof_ref_pin['path']] = proof_ref_pin['sha256']
    pins[source_ref_pin['path']] = source_ref_pin['sha256']
    pins[manifest_pin['path']] = manifest_pin['sha256']
    if initial.get('reference_templates') != focused_descriptor:
        raise ValueError('Runtime focused prefab descriptor differs from pinned domain bytes')
    if raw_capture.get('kind') != 'AuthoredSkirmishPrefabReferenceData' or \
            raw_capture.get('authority') != 'ActualSourceAssetsAndNativeBakers_NotMissionState':
        raise ValueError('Exact source prefab raw capture authority required')
    expected_vfx = {
        'Game.Components.UnitAttackImpactVfxReference/Prefab': 10,
        'Game.Components.UnitMuzzleFlashVfxReference/Prefab': 9,
        'Game.Components.GroundMissileLauncherVfxReferenceComponent/ImpactExplosionPrefab': 1,
        'Game.Components.GroundMissileLauncherVfxReferenceComponent/ImpactSmokePrefab': 1,
        'Game.Components.GroundMissileLauncherVfxReferenceComponent/LauncherBackfirePrefab': 1,
        'Game.Components.GroundMissileLauncherVfxReferenceComponent/RocketTrailPrefab': 1}
    entities = {row.get('id') for row in raw_capture.get('entities', []) if isinstance(row, dict)}
    vfx_counts, vfx_paths = {}, set()
    for row in raw_capture.get('unsupported_values', []):
        parts = row.get('path', '').split('/') if isinstance(row, dict) else []
        key = '/'.join(parts[1:]) if len(parts) == 3 else ''
        if len(parts) != 3 or parts[0] not in entities or key not in expected_vfx or \
                row.get('reason') != 'unity_object_ref_requires_bound_source_identity' or \
                not row.get('type', '').startswith('Unity.Entities.UnityObjectRef') or row['path'] in vfx_paths:
            raise ValueError('Raw capture contains unsupported data outside exact presentation VFX domain')
        vfx_paths.add(row['path'])
        vfx_counts[key] = vfx_counts.get(key, 0) + 1
    if vfx_counts != expected_vfx or focused_proof.get('exact_unconsumed_fields') != expected_vfx:
        raise ValueError('Raw capture VFX omissions differ from focused proof')
    raw_sources = raw_capture.get('source_pins') if isinstance(raw_capture, dict) else None
    if not isinstance(raw_sources, dict) or focused_proof.get('source_pin_count') != len(raw_sources):
        raise ValueError('Raw capture source-pin closure/count missing')
    for path, digest in raw_sources.items():
        source_path = Path(path)
        if not source_path.is_absolute():
            if '\\' in path or any(part in ('', '.', '..') for part in path.split('/')):
                raise ValueError('Canonical relative raw source path required')
            source_path = root / source_path
        current = _verify({'path': str(source_path), 'sha256': digest}, 'raw capture source input', root, allow_external=True)
        pins[current['path']] = current['sha256']
    if focused.get('raw_capture_sha256') != raw_pin['sha256'] or \
            any(focused.get(k) is not False for k in ('raw_capture_replay_ready', 'runtime_import_attempted',
                'runtime_admission', 'native_visual_readiness', 'source_fit_certified', 'mission_ready')):
        raise ValueError('Raw capture identity or focused-prefab nonclaims invalid')

    if admission.get('binding_semantic_equal_to_retained_origin') is not True or \
            admission.get('focused_prefab_raw_failure_preserved') is not True or \
            any(admission.get(k) is not False for k in FALSE) or \
            any(bundle.get('nonclaims', {}).get(k) is not False for k in FALSE):
        raise ValueError('Admission and bundle must preserve every explicit nonclaim')
    return pins
