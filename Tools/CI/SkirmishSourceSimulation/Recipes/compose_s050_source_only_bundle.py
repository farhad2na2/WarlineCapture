#!/usr/bin/env python3
"""Stage current compiler-selected S050 inputs; require an explicit resource preset before a runnable bundle exists."""
import argparse
import hashlib
import importlib.util
import json
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[4]
BASE = ROOT / 'T Logs/SkirmishProduction'
AUTHOR = BASE / 's050-author-current-shared-20261115-r3'
GRAPH = BASE / 'five-mission-pilot-20261106-T1/s050-citywide-source-search-inputs-r1/source-config-graph.json'
CONTEXT = BASE / 's050-source-factory-context-probe-20261107-r1/source-factory-context.json'
QUALIFICATION = BASE / 's050-source-qualification-20261107-r1/qualification.json'
PROJECTED_BINDING = BASE / 's050-current-logical-binding-projection-20261115-r5/logical-binding.json'
BINDING_RECEIPT = BASE / 's050-current-logical-binding-projection-20261115-r5/receipt.json'
CAMERA = BASE / 's050-selected-camera-profile-projection-20261115-r1/engineering-camera-context.json'
CAMERA_RECEIPT = BASE / 's050-selected-camera-profile-projection-20261115-r1/receipt.json'
PACKAGE = BASE / 's050-source-derived-map-package-20261115-r2/map-package.json'
PACKAGE_PREFLIGHT = BASE / 's050-exact-map-owner-preflight-20261115-r1/preflight-report.json'
REFERENCE = BASE / 's050-focused-prefab-projection-20261115-r1/reference-template-descriptor.json'
BUILDINGS = ROOT / 'Logs/SkirmishProduction/s050-building-handoff-20261115-r2/building-definitions.json'
BUILDING_AUDIT = BASE / 's050-building-handoff-audit-20261115-r2/audit-receipt.json'
UNITS = ROOT / 'Logs/SkirmishProduction/s050-unit-handoff-unity-20261115-r5/unit-production-metadata.json'
UNIT_AUDIT = BASE / 's050-unit-handoff-audit-20261115-r5/audit-receipt.json'
DEATH = ROOT / 'Logs/SkirmishProduction/s050-death-mechanical-readset-20261115-r2/death-mechanical-readset.json'
DEATH_AUDIT = BASE / 's050-death-mechanical-readset-audit-20261115-r2/audit-receipt.json'
PHYSICAL_REQUEST = BASE / 's050-physical-input-request-20261115-r2/request.json'
ROTATION = BASE / 's050-current-rotation-reference-set-20261107-r1/reference-set.json'
CONFIGURED = BASE / 's050-derived-shared-factory-projections-20261107-r1/configured-metadata.json'
TEXT = Path('/Volumes/T7/Projects/WarlineCapture/Logs/private-candidates/s049-physical-export-qualified-20261028-r1/project/Logs/SkirmishProduction/s049-command-text-r1/command-text.json')
TEXT_CAPTURE_ROOT = TEXT.parents[3]
VALUES = ROOT / 'T Logs/SkirmishProduction/five-mission-pilot-20261106-T1/s026-current-camera-capture-r3/camera-values.json'
MOTION = ROOT / 'T Logs/SkirmishProduction/five-mission-pilot-20261106-T1/s026-current-camera-capture-r3/camera-motion.json'
ADAPTER = ROOT / 'T Logs/SkirmishProduction/five-mission-pilot-20261106-T1/s026-current-camera-adapter-candidate.json'
METADATA_PROJECTOR = Path(__file__).resolve().with_name('project_s050_audited_factory_handoffs.py')


def digest(raw):
    return hashlib.sha256(raw).hexdigest()


def pin(path):
    path = Path(path).resolve(strict=True)
    if path.is_symlink() or not path.is_file():
        raise ValueError(f'Regular current owner file required: {path}')
    return {'path': str(path), 'sha256': digest(path.read_bytes())}


def read(path):
    return json.loads(Path(path).read_bytes())


def descriptor(path):
    return pin(path)


def require_descriptor(row, pin_map, label):
    if not isinstance(row, dict) or set(row) != {'path', 'sha256'}:
        raise ValueError(f'Exact {label} path/SHA descriptor required')
    if not isinstance(row['path'], str) or not isinstance(row['sha256'], str):
        raise ValueError(f'Exact {label} path/SHA descriptor required')
    actual = pin(row['path'])
    if actual != row or pin_map.get(actual['path']) != actual['sha256']:
        raise ValueError(f'{label} must match the current authored graph source pin')
    return actual


def validate_s050_grid_graph_extension(graph_path, graph_receipt_path, package_path, package_preflight_path):
    graph_desc, receipt_desc = pin(graph_path), pin(graph_receipt_path)
    package_desc, preflight_desc = pin(package_path), pin(package_preflight_path)
    graph, receipt = read(graph_desc['path']), read(receipt_desc['path'])
    package, preflight = read(package_desc['path']), read(preflight_desc['path'])
    expected_receipt_keys = {'kind','status','base_graph','graph','grid','grid_comparison','input_sha256',
        'blocked_cells','source_only','world_allocated','runtime_admission','source_fit_certified','mission_ready'}
    if (set(receipt) != expected_receipt_keys or receipt.get('kind') != 'S050ExactCitywideGridGraphExtension' or
            receipt.get('status') != 'PreparedSourceOnly' or receipt.get('graph') != graph_desc or
            receipt.get('base_graph') != pin(GRAPH) or receipt.get('source_only') is not True or
            any(receipt.get(k) is not False for k in ('world_allocated','runtime_admission','source_fit_certified','mission_ready'))):
        raise ValueError('Exact existing S050 selected Campaign grid graph extension receipt required')
    input_pins = receipt.get('input_sha256')
    if not isinstance(input_pins,dict) or len(input_pins) < 8:
        raise ValueError('Complete current S050 grid extension input pin set required')
    for path, expected in input_pins.items():
        if pin(path)['sha256'] != expected:
            raise ValueError(f'S050 selected grid graph extractor input drift: {path}')
    if not isinstance(graph.get('source_pins'),dict) or any(pin(path)['sha256'] != digest for path,digest in graph['source_pins'].items()):
        raise ValueError('Current source pin closure required for selected S050 map graph')
    static = package.get('static_map',{})
    if (package.get('kind') != 'SourceDerivedCampaignMapPackage' or static.get('operation_map_id') != 'opmap.ch05.citywide_alert' or
            any(package.get(k) is not False for k in ('mission_initialized','mission_ready','source_fit_certified','live_occupancy_measured')) or
            preflight.get('kind') != 'SourceDerivedCampaignMapPackage' or preflight.get('status') != 'SourceOnlyPreflightPassed' or
            preflight.get('operation_map_id') != static.get('operation_map_id') or preflight.get('world_created') is not False or
            any(preflight.get(k) is not False for k in ('mission_initialized','mission_ready','source_fit_certified','live_occupancy_measured','active_water_qualified'))):
        raise ValueError('Exact nonready selected S050 Campaign map package/preflight required')
    verified = preflight.get('verified_inputs',[])
    if not isinstance(verified,list) or not all(any(row == desc for row in verified) for desc in (receipt['base_graph'], static['definition'], static['grid_config'], static['surface_data'])):
        raise ValueError('Current map preflight does not bind base graph and exact Campaign asset descriptors')
    grid = static['grid_config']; grid_asset = Path(grid['path']); meta = Path(str(grid_asset)+'.meta')
    meta_guid = re.findall(r'^guid: ([0-9a-f]{32})$',meta.read_text(),re.M)
    if (len(meta_guid)!=1 or receipt['grid'] != {'path':str(grid_asset.resolve()),'sha256':grid['sha256'],'guid':meta_guid[0]} or
            receipt.get('blocked_cells') != 16392):
        raise ValueError('Selected Campaign grid GUID/hash or measured authored cell count differs')
    node_matches=[n for n in graph.get('nodes',[]) if n.get('source_type')=='GridAuthoringSceneConfigAsset' and
        n.get('asset_guid')==meta_guid[0] and n.get('source_path')==str(grid_asset.relative_to(ROOT)) and n.get('source_sha256')==grid['sha256']]
    source_matches=[(p,d) for p,d in graph['source_pins'].items() if p.replace('\\','/').endswith('/'+str(grid_asset.relative_to(ROOT)))]
    if len(node_matches)!=1 or len(source_matches)!=1 or source_matches[0][1]!=grid['sha256'] or \
            node_matches[0].get('fields',{}).get('width')!=440 or node_matches[0].get('fields',{}).get('height')!=220 or \
            len(node_matches[0].get('fields',{}).get('blockedCells',[]))!=receipt['blocked_cells']:
        raise ValueError('Unique extracted selected Campaign GridAuthoringSceneConfigAsset row required')
    for source_type, key in (('OperationMapDefinition','definition'),('MapSurfaceDataAsset','surface_data')):
        asset=static[key];path=Path(asset['path']);asset_guid=re.findall(r'^guid: ([0-9a-f]{32})$',Path(str(path)+'.meta').read_text(),re.M)
        matches=[n for n in graph.get('nodes',[]) if n.get('source_type')==source_type and len(asset_guid)==1 and n.get('asset_guid')==asset_guid[0] and n.get('source_path')==str(path.relative_to(ROOT)) and n.get('source_sha256')==asset['sha256']]
        if len(matches)!=1: raise ValueError('Unique selected Campaign '+key+' graph row required')
    return {'graph':graph_desc,'receipt':receipt_desc,'base_graph':receipt['base_graph']}


def validate_focused_prefab_projection(descriptor_path, context_desc, qualification_desc, graph_desc,
                                       building_desc, unit_desc, authoring_result_desc):
    descriptor_file = Path(descriptor_path).resolve(strict=True)
    descriptor_file_pin = pin(descriptor_file)
    reference = read(descriptor_file)
    expected_keys = {'path','sha256','source_reference_path','source_reference_sha256',
        'projection_manifest_path','projection_manifest_sha256','source_only_capture_proof'}
    if set(reference) != expected_keys:
        raise ValueError('Exact focused prefab projection descriptor schema required')
    domain_desc = {'path':reference['path'],'sha256':reference['sha256']}
    raw_desc = {'path':reference['source_reference_path'],'sha256':reference['source_reference_sha256']}
    manifest_desc = {'path':reference['projection_manifest_path'],'sha256':reference['projection_manifest_sha256']}
    proof_desc = reference['source_only_capture_proof']
    for row, label in ((domain_desc,'projection'),(raw_desc,'raw capture'),(manifest_desc,'manifest'),
                       (proof_desc,'source-only proof')):
        if not isinstance(row,dict) or set(row)!={'path','sha256'} or pin(row['path'])!=row:
            raise ValueError(f'Current focused prefab {label} pin required')
    raw = read(raw_desc['path']); domain=read(domain_desc['path']); manifest=read(manifest_desc['path'])
    proof=read(proof_desc['path']); receipt=read(descriptor_file.parent/'receipt.json')
    if 'unit_owner_evidence' in proof and not isinstance(proof['unit_owner_evidence'], dict):
        raise ValueError('Optional focused prefab direct-owner evidence must be absent or a validated object')
    expected_receipt_keys={'schema_version','kind','status','raw_capture','raw_capture_sha256','raw_capture_failure_preserved',
        'raw_capture_replay_ready','projection_descriptor','source_pin_count','unsupported_vfx_rows_preserved',
        'runtime_import_attempted','runtime_admission','native_visual_readiness','source_fit_certified','mission_ready'}
    if (set(receipt)!=expected_receipt_keys or receipt.get('kind')!='FocusedPrefabSourceOnlyProjectionReceipt' or
            receipt.get('status')!='ProjectedSourceOnlyFromFailedRawCapture' or receipt.get('raw_capture')!=raw_desc or
            receipt.get('raw_capture_sha256')!=raw_desc['sha256'] or receipt.get('projection_descriptor')!=descriptor_file_pin or
            receipt.get('raw_capture_failure_preserved') is not True or receipt.get('raw_capture_replay_ready') is not False or
            any(receipt.get(k) is not False for k in ('runtime_import_attempted','runtime_admission','native_visual_readiness','source_fit_certified','mission_ready'))):
        raise ValueError('Exact source-only focused prefab projection receipt required')
    if (proof.get('kind')!='FocusedPrefabSourceOnlyCaptureProof' or proof.get('status')!='ProjectedSourceOnlyFromFailedRawCapture' or
            proof.get('raw_capture')!=raw_desc or proof.get('raw_capture_result')!='Failed_UnconsumedPresentationUnityObjectRefs' or
            proof.get('source_context')!=context_desc or proof.get('configuration_qualification')!=qualification_desc or
            proof.get('source_config_graph')!=graph_desc or proof.get('building_definitions')!=building_desc or
            proof.get('unit_production_metadata')!=unit_desc or proof.get('authoring_result')!=authoring_result_desc or
            proof.get('source_pins_verified_current') is not True or proof.get('raw_capture_pass_marker_present') is not False or
            any(proof.get(k) is not False for k in ('raw_capture_replay_ready','runtime_import_attempted','runtime_admission',
                'native_visual_readiness','source_fit_certified','mission_ready'))):
        raise ValueError('Focused prefab capture proof is not bound to current initialization descriptors')
    target=read(context_desc['path']).get('target_context',{})
    if (proof.get('target')!={k:target.get(k) for k in ('catalog_id','seed','size','difficulty','setup_hash','operation_map_id','source_content_hash')} or
            raw.get('kind')!='AuthoredSkirmishPrefabReferenceData' or raw.get('authority')!='ActualSourceAssetsAndNativeBakers_NotMissionState' or
            raw.get('requested_prefab_keys_sha256')!=proof.get('unit_key_input',{}).get('sha256') or
            raw.get('replay_ready') is not False or raw.get('full_mission_initialized') is not False or
            raw.get('native_visual_readiness') is not False or raw.get('geometry_exported') is not False):
        raise ValueError('Current focused prefab raw capture scope/key binding required')
    key_desc=proof.get('unit_key_input'); key_pin=pin(key_desc['path']) if isinstance(key_desc,dict) else None
    if key_pin!=key_desc or raw.get('source_pins',{}).get(key_desc['path'])!=key_desc['sha256']:
        raise ValueError('Focused prefab key bytes are not pinned by the current raw capture')
    projector_path=Path(__file__).resolve().with_name('project_source_only_focused_prefab_domain.py')
    spec=importlib.util.spec_from_file_location('_s050_focused_prefab_composition_validator',projector_path)
    if spec is None or spec.loader is None: raise ValueError('Focused prefab source projector unavailable')
    projector=importlib.util.module_from_spec(spec);spec.loader.exec_module(projector)
    current_units=read(unit_desc['path']); expected_keys=projector.extract_unit_keys(current_units)
    if (key_desc['sha256']!=pin(key_desc['path'])['sha256'] or read(key_desc['path'])!=expected_keys or
            raw.get('requested_prefab_keys')!=expected_keys or len(expected_keys)!=16):
        raise ValueError('Focused capture key set differs from current compiler-selected unit metadata')
    field_counts,component_counts=projector.validate_unsupported(raw)
    if not raw.get('unsupported_values') or sum(field_counts.values())!=len(raw['unsupported_values']) or \
            proof.get('unsupported_vfx_rows_preserved')!=len(raw['unsupported_values']) or \
            manifest.get('unsupported_values_preserved')!=len(raw['unsupported_values']) or \
            manifest.get('raw_capture_status')!='Failed_UnconsumedPresentationUnityObjectRefs' or \
            manifest.get('capture_proof')!=proof_desc or manifest.get('world_created') is not False or \
            any(manifest.get(k) is not False for k in ('runtime_import_attempted','source_fit_certified','native_visual_readiness','mission_ready')):
        raise ValueError('Unsupported VFX failure or source-only manifest evidence differs')
    if (domain.get('source_reference_sha256')!=raw_desc['sha256'] or domain.get('requested_prefab_keys')!=expected_keys or
            domain.get('requested_prefab_keys_sha256')!=key_desc['sha256'] or domain.get('unsupported_values')!=raw.get('unsupported_values') or
            any(domain.get(k) is not False for k in ('replay_ready','fresh_world_allocator_exported','full_mission_initialized',
                'native_visual_readiness','geometry_exported'))):
        raise ValueError('Focused prefab compact source domain differs from retained raw owner capture')
    proof_log=proof.get('wrapper_log'); log_pin=pin(proof_log['path']) if isinstance(proof_log,dict) else None
    if log_pin!=proof_log: raise ValueError('Current focused capture wrapper log descriptor required')
    log=Path(proof_log['path']).read_text(errors='replace'); unsupported_count=len(raw['unsupported_values'])
    request_desc=proof.get('capture_request'); request_pin=pin(request_desc['path']) if isinstance(request_desc,dict) else None
    request=read(request_desc['path']) if request_pin==request_desc else {}
    if (f'InvalidOperationException: Focused prefab reference contains unsupported values: {unsupported_count}' not in log or
            'ExitCode: 0' not in log or '[SkirmishFocusedPrefabReferenceData] result=Passed' in log or
            'Game.Editor.SkirmishBakedActorSnapshotExporter.RunFocusedPrefabReferenceData' not in log or
            request.get('execute_method')!='Game.Editor.SkirmishBakedActorSnapshotExporter.RunFocusedPrefabReferenceData' or
            request.get('focused_unit_key_descriptor')!=key_desc or request.get('focused_unit_prefab_keys')!=expected_keys or
            request.get('capture_output_directory')!=str(Path(raw_desc['path']).parent)):
        raise ValueError('Original focused capture failed VFX closure or invocation/request binding differs')
    projector.verify_source_pins(raw.get('source_pins'),ROOT)
    if (proof.get('source_pin_count')!=len(raw.get('source_pins',{})) or
            receipt.get('source_pin_count')!=len(raw.get('source_pins',{}))):
        raise ValueError('Complete current focused capture source pin closure required')
    return reference


def validate_source_owner_authoring(initialization, graph_pins, authority, authoring_graph_pin=None):
    if not isinstance(authority, dict) or set(authority) != {'request', 'result'}:
        raise ValueError('Exact source-owner authoring request/result pair required')
    request_desc = authority['request']
    result_desc = authority['result']
    if pin(request_desc['path']) != request_desc or pin(result_desc['path']) != result_desc:
        raise ValueError('Source-owner authoring request/result pin drift')
    request = read(request_desc['path'])
    result = read(result_desc['path'])
    context_descriptor = initialization.get('metadata_compiler_context')
    if not isinstance(context_descriptor, dict) or pin(context_descriptor['path']) != context_descriptor:
        raise ValueError('Exact selected compiler context required for authoring result join')
    context = read(context_descriptor['path'])
    target = context.get('target_context', {})
    if request.get('kind') != 'SkirmishMissionAuthoringPreflight' or request.get('schema_version') != 1:
        raise ValueError('Exact compiler authoring request required')
    selection = initialization.get('selection', {})
    if (request.get('catalog_id'), request.get('seed'), request.get('size'), request.get('difficulty')) != (
            initialization.get('mission_id'), selection.get('seed'), 'Standard', 'Regular'):
        raise ValueError('Compiler authoring request differs from the selected runtime target')
    request_layout = request.get('layout')
    request_matrix = request.get('setup_matrix')
    if (not isinstance(request_layout, dict) or not isinstance(request_matrix, dict) or
            pin(request_layout.get('path', '')) != request_layout or
            pin(request_matrix.get('path', '')) != request_matrix or
            request_layout != initialization.get('typed_mission_configuration', {}).get('layout') or
            request_matrix != initialization.get('setup_matrix') or
            str((ROOT / request.get('campaign_source_path', '')).resolve()) != initialization.get('selected_operation_map', {}).get('path')):
        raise ValueError('Compiler request layout, setup matrix, or Campaign source differs from the runtime selection')
    result_pins = result.get('input_pins')
    graph = initialization.get('source_config_graph')
    result_graph_pin = authoring_graph_pin if authoring_graph_pin is not None else graph
    if (result.get('kind') != 'SkirmishMissionAuthoringPreflightResult' or
            result.get('status') != 'ConfigurationCompiled' or result.get('compiler_accepted') is not True or
            any(result.get(key) is not False for key in ('world_allocated', 'source_fit_verified', 'mission_ready', 'native_acceptance')) or
            (result.get('catalog_id'), result.get('seed'), result.get('operation_map_id'), result.get('source_content_hash'),
             result.get('source_fingerprint'), result.get('layout_fingerprint')) !=
            (target.get('catalog_id'), target.get('seed'), target.get('operation_map_id'), target.get('source_content_hash'),
             target.get('source_fingerprint'), target.get('layout_fingerprint')) or
            result.get('resolved_setup', {}).get('setup_hash') != target.get('setup_hash') or
            not isinstance(result_pins, dict) or
            result_pins.get(request_desc['path'], {}).get('sha256') != request_desc['sha256'] or
            result_pins.get(result_graph_pin.get('path'), {}).get('sha256') != result_graph_pin.get('sha256') or
            result_pins.get(request_layout['path'], {}).get('sha256') != request_layout['sha256'] or
            result_pins.get(request_matrix['path'], {}).get('sha256') != request_matrix['sha256']):
        raise ValueError('Compiler result does not match and pin the exact request, graph, layout, matrix, and context')
    selected = request.get('resource_preset')
    if selected is None:
        return request_desc, result_desc, None
    preset = require_descriptor(selected, graph_pins, 'compiler-selected resource preset')
    if result_pins.get(preset['path'], {}).get('sha256') != preset['sha256']:
        raise ValueError('Compiler result does not pin the exact selected resource preset')
    return request_desc, result_desc, preset


def compose_envelope(output, initialization_without_resource, run_without_initialization, graph_pins,
                     source_owner_authoring, resource_preset_override=None, runtime_projection_desc=None,
                     source_graph_extension_desc=None):
    output = Path(output).absolute()
    if not output.is_relative_to(ROOT) or output.exists():
        raise ValueError('Fresh repository-owned T output directory required')
    request_desc, result_desc, selected_resource = validate_source_owner_authoring(
        initialization_without_resource, graph_pins, source_owner_authoring,
        source_graph_extension_desc.get('base_graph') if source_graph_extension_desc is not None else None)
    if resource_preset_override is not None:
        override = require_descriptor(resource_preset_override, graph_pins, 'resource preset override')
        if selected_resource is None or override != selected_resource:
            raise ValueError('Resource preset override does not exactly match compiler author selection')
    output.mkdir(parents=True)
    staged = {'schema_version': 1, 'kind': 'S050SourceOnlyOwnerInputStage',
              'verified_initialization_fields': initialization_without_resource,
              'source_owner_authoring': {'request': request_desc, 'result': result_desc},
              'verified_run_fields': run_without_initialization,
              'missing_explicit_fields': [] if selected_resource is not None else ['resource_preset'],
              'world_allocated': False, 'mission_ready': False, 'native_acceptance': False,
              'source_fit_certified': False}
    if selected_resource is None:
        staged['status'] = 'AwaitingExplicitResourcePreset'
        stage_path = output / 'partial-owner-inputs.json'
        stage_path.write_text(json.dumps(staged, indent=2) + '\n')
        receipt = {'schema_version': 1, 'kind': 'S050SourceOnlyBundleCompositionReceipt',
                   'status': staged['status'], 'partial_owner_inputs': pin(stage_path),
                   'runnable_bundle_emitted': False, 'initialization_emitted': False,
                   'missing_explicit_fields': ['resource_preset'], 'source_owner_authoring': staged['source_owner_authoring'], 'world_allocated': False,
                   'mission_ready': False, 'source_fit_certified': False}
        if source_graph_extension_desc is not None: receipt['source_graph_extension'] = source_graph_extension_desc
        receipt_path = output / 'composition-receipt.json'
        receipt_path.write_text(json.dumps(receipt, indent=2) + '\n')
        return receipt

    initialization = dict(initialization_without_resource)
    initialization['resource_preset'] = selected_resource
    initialization['source_owner_authoring'] = staged['source_owner_authoring']
    staged['status'] = 'ComposedSourceOnlyRequestEnvelope'
    init_path = output / 'initialization.json'
    init_path.write_text(json.dumps(initialization, indent=2) + '\n')
    run_inputs = dict(run_without_initialization)
    run_inputs['initialization'] = pin(init_path)
    bundle_path = output / 'data-bundle.json'
    bundle_path.write_text(json.dumps(run_inputs, indent=2) + '\n')
    receipt = {'schema_version': 1, 'kind': 'S050SourceOnlyBundleCompositionReceipt',
               'status': staged['status'], 'initialization': pin(init_path),
               'data_bundle': pin(bundle_path), 'runnable_bundle_emitted': True,
               'world_allocated': False, 'mission_ready': False,
               'source_fit_certified': False, 'native_acceptance': False}
    if runtime_projection_desc is not None:
        receipt['runtime_metadata_projection'] = runtime_projection_desc
    if source_graph_extension_desc is not None: receipt['source_graph_extension'] = source_graph_extension_desc
    receipt_path = output / 'composition-receipt.json'
    receipt_path.write_text(json.dumps(receipt, indent=2) + '\n')
    return receipt


def validate_compiler_physical_request(path, author_request, author_result, package):
    request_path = Path(path).resolve(strict=True)
    request = read(request_path)
    result_path = Path(author_result['_path']).resolve(strict=True)
    result_sha = pin(result_path)['sha256']
    if (request.get('schema_version') != 1 or request.get('kind') != 'PinnedCampaignPhysicalInputExport' or
            request.get('selection') != {'path': str(result_path), 'sha256': result_sha} or
            request.get('operation_map_id') != author_result.get('operation_map_id') or
            request.get('content_hash') != author_result.get('source_content_hash') or
            request.get('source_fit_certified') is not False or request.get('mission_ready') is not False):
        raise ValueError('Physical input request must select the exact current compiler result and retain nonclaims')
    for path_key, sha_key in (('source_path', 'source_sha256'), ('grid_path', 'grid_sha256'), ('surface_path', 'surface_sha256')):
        if pin(ROOT / request.get(path_key, ''))['sha256'] != request.get(sha_key):
            raise ValueError(f'Current physical source asset pin differs: {path_key}')
    static_map = package.get('static_map', {})
    if (static_map.get('operation_map_id') != request.get('operation_map_id') or
            static_map.get('definition') != pin(ROOT / request['source_path']) or
            package.get('frame') != request.get('frame')):
        raise ValueError('Physical request source/frame differs from the selected source-map package')
    return pin(request_path)


def validate_preset_handoff(owner, metadata_path, audit_path, author_result_path,
                            author_result, physical_request_desc, selected_preset=None,
                            validated_context=None, expected_owner_semantic=None):
    metadata_desc = pin(metadata_path)
    metadata = read(metadata_path)
    audit = read(audit_path)
    result_desc = pin(author_result_path)
    expected_selection = metadata.get('compiler_selection', {})
    semantic_hash=expected_selection.get('source_content_hash')
    semantic_prefix=(f"{validated_context.get('definition_id')}.v{validated_context.get('content_version')}/enemy-policy-v1/"
                     if validated_context is not None else None)
    semantic_valid=(isinstance(semantic_hash,str) and semantic_prefix is not None and
        re.fullmatch(re.escape(semantic_prefix)+r'[0-9a-f]{64}',semantic_hash) is not None and
        (expected_owner_semantic is None or semantic_hash==expected_owner_semantic))
    if (metadata.get('catalog_id') != 'S050' or metadata.get('seed') != 104779 or
            metadata.get('setup_hash') != author_result.get('resolved_setup', {}).get('setup_hash') or
            expected_selection.get('result') != result_desc or
            expected_selection.get('physical_input_request') != physical_request_desc or
            expected_selection.get('operation_map_id') != author_result.get('operation_map_id') or
            (validated_context is not None and expected_selection.get('layout_fingerprint') != validated_context.get('layout_binding_fingerprint')) or
            not semantic_valid or
            expected_selection.get('setup_hash') != author_result.get('resolved_setup', {}).get('setup_hash') or
            expected_selection.get('world_allocated') is not False or
            expected_selection.get('source_fit_certified') is not False or
            expected_selection.get('mission_ready') is not False or
            metadata.get('unsupported_values') not in ([], None) or
            metadata.get('full_mission_initialized') is not False or
            metadata.get('native_visual_readiness') is not False):
        raise ValueError(f'Current {owner} handoff differs from compiler/preset selection or nonclaims')
    if owner == 'unit':
        identity_path = Path(metadata_path).with_name('unit-prefab-identity-readback.json')
        identity = read(identity_path)
        if (identity.get('metadata_sha256') != metadata_desc['sha256'] or
                identity.get('catalog_id') != 'S050' or identity.get('seed') != 104779 or
                identity.get('setup_hash') != expected_selection.get('setup_hash') or
                identity.get('mission_ready') is not False):
            raise ValueError('Unit identity readback does not bind the exact unit handoff')
    if selected_preset is not None:
        if (pin(selected_preset.get('path', '')) != selected_preset or
                author_result.get('input_pins', {}).get(selected_preset['path'], {}).get('sha256') != selected_preset['sha256'] or
                metadata.get('compiler_selection_asset_readback', {}).get('status') != 'VerifiedAssetIdentityOnly'):
            raise ValueError(f'{owner} handoff does not bind the explicitly selected preset asset')
        if owner == 'unit' and metadata.get('source_preset_index') != 0:
            raise ValueError('Unit handoff source preset index differs from the explicit BaseAssault selection')
    selection = audit.get('compiler_selection', {})
    source = audit.get('physical_source', {})
    legacy_status = ('PassedForAuthoredBuildingMetadataOnly' if owner == 'building'
                     else 'PassedForMetadataAndIdentityOnly')
    if audit.get('kind') == 'S050CompilerSelectedPresetUnityOwnerAudit':
        valid_audit = (audit.get('owner') == owner and audit.get('status') == 'Passed' and
            audit.get('source_pin_missing_count') == 0 and audit.get('source_pin_changed_count') == 0 and
            audit.get('source_pin_mismatches') == [] and audit.get('source_pin_count', 0) > 0 and
            selection.get('result') == result_desc and selection.get('physical_input_request') == physical_request_desc and
            source.get('request_path') == physical_request_desc['path'] and
            source.get('request_sha256') == physical_request_desc['sha256'])
        if selected_preset is not None:
            preset = audit.get('preset', {})
            valid_audit = valid_audit and preset.get('path') == selected_preset['path'] and preset.get('sha256') == selected_preset['sha256'] and preset.get('supply_day_seconds_from_pinned_source_graph') == 120 and preset.get('compiler_result_input_pin', {}).get('sha256') == selected_preset['sha256']
        marker_counts = audit.get('pass_marker_counts', {})
        markers_valid = bool(marker_counts) and all(value == 1 for value in marker_counts.values())
        checks=audit.get('checks',{})
        common_checks=('author_result_sha','physical_request_sha','preset_descriptor_matches',
            'preset_value_is_120','preset_result_input_pin','graph_result_input_pin',
            'physical_map_matches_compiler','compiler_selection_author_result',
            f'{owner}_unity_assetdb_preset_identity',f'{owner}_preset_node_not_hydrated',
            f'{owner}_all_source_pins_current',f'{owner}_pins_compiler_result',
            f'{owner}_pins_preset_asset',f'{owner}_pins_physical_request',
            f'{owner}_exact_pass_markers',f'{owner}_editor_gui_mode',f'{owner}_durable_log_byte_match',
            'unity_asset_field_parity_not_claimed')
        if owner == 'unit': common_checks += ('unit_identity_metadata_sha','unit_prefab_identity_joins')
        markers_valid = markers_valid and all(checks.get(key) is True for key in common_checks)
    else:
        valid_audit = (audit.get('status') == legacy_status and
            audit.get('compiler_result') == result_desc and
            audit.get('physical_input_request') == physical_request_desc and
            audit.get('metadata') == metadata_desc)
        markers_valid = bool(audit.get('exact_pass_marker')) if owner == 'building' else bool(audit.get('pass_markers'))
    if (not valid_audit or not markers_valid or
            audit.get('nonclaims', {}).get('world_allocated') is not False or
            audit.get('nonclaims', {}).get('source_fit_certified') is not False or
            audit.get('nonclaims', {}).get('mission_ready') is not False or
            audit.get('nonclaims', {}).get('native_acceptance') is not False):
        raise ValueError(f'Current {owner} owner audit does not bind the exact compiler and physical request')
    return metadata_desc, pin(audit_path), semantic_hash


def validate_runtime_metadata_projection(projection_receipt_path, unit_projection_path,
                                         building_projection_path, context_pin,
                                         unit_owner_desc, unit_audit_desc,
                                         building_owner_desc, building_audit_desc,
                                         expected_owner_semantic, configured_metadata_path=CONFIGURED):
    receipt_desc = pin(projection_receipt_path)
    receipt = read(projection_receipt_path)
    generator = pin(METADATA_PROJECTOR)
    if (receipt.get('schema_version') != 1 or
            receipt.get('kind') != 'S050AuditedCompilerContextMetadataProjection' or
            receipt.get('status') != 'ProjectedCompilerContextSourceOnly' or
            receipt.get('metadata_compiler_context') != context_pin or
            receipt.get('generator') != generator or
            any(receipt.get(k) is not False for k in ('world_allocated', 'runtime_admission',
                'source_fit_certified', 'native_S050_capture', 'mission_ready',
                'native_acceptance', 'unity_asset_field_value_parity_proven'))):
        raise ValueError('Exact current nonready audited-owner projection receipt required')
    provenance = receipt.get('owner_provenance', {})
    expected_provenance = {
        'unit_owner_handoff': unit_owner_desc, 'unit_owner_audit': unit_audit_desc,
        'building_owner_handoff': building_owner_desc, 'building_owner_audit': building_audit_desc,
    }
    if any(provenance.get(k) != v for k, v in expected_provenance.items()):
        raise ValueError('Runtime projection provenance does not bind exact direct owner handoffs/audits')
    if provenance.get('owner_semantic_source_content_hash') != expected_owner_semantic or \
            provenance.get('unity_asset_field_value_parity_proven') is not False:
        raise ValueError('Runtime projection semantic identity or Unity nonclaim differs')
    projections = receipt.get('projections', {})
    building_desc, unit_desc = pin(building_projection_path), pin(unit_projection_path)
    if projections.get('building') != building_desc or projections.get('unit') != unit_desc or \
            projections.get('configured') != pin(configured_metadata_path):
        raise ValueError('Runtime schema-3 projections differ from the owner projection receipt')
    context = read(context_pin['path'])
    target = context.get('target_context', {})
    for owner, descriptor, kind in (
            ('building', building_desc, 'CompilerContextDerivedSkirmishBuildingDefinitions'),
            ('unit', unit_desc, 'CompilerContextDerivedSkirmishUnitProductionMetadata')):
        doc = read(descriptor['path'])
        if (doc.get('schema_version') != 3 or doc.get('kind') != kind or
                doc.get('metadata_compiler_context') != context_pin or
                (doc.get('catalog_id'), doc.get('seed'), doc.get('setup_hash')) !=
                (target.get('catalog_id'), target.get('seed'), target.get('setup_hash')) or
                any(doc.get(k) is not False for k in ('native_S050_capture', 'full_mission_initialized', 'native_visual_readiness')) or
                doc.get('unsupported_values') != []):
            raise ValueError(f'Exact schema-3 compiler-context {owner} projection required')
    return receipt_desc, unit_desc, building_desc


def validate_current_binding_projection(binding_path, binding_receipt_path, context_pin,
                                        qualification_pin, author_request_desc, author_result_desc):
    binding_desc = pin(binding_path)
    receipt_desc = pin(binding_receipt_path)
    binding = read(binding_desc['path'])
    receipt = read(receipt_desc['path'])
    context = read(context_pin['path'])
    target = context.get('target_context', {})
    if (binding.get('kind') != 'OriginalCompilerDeclaredLogicalBinding' or
            binding.get('metadata_compiler_context') != context_pin or
            binding.get('target_context') != target or
            binding.get('mission_ready') is not False or binding.get('native_capture') is not False or
            binding.get('runtime_admission') is not False or binding.get('source_fit_certified') is not False or
            binding.get('world_allocated') is not False):
        raise ValueError('Current source-only logical binding must match the exact compiler context')
    source_binding = binding.get('source_owner_binding')
    if not isinstance(source_binding, dict) or pin(source_binding.get('path', '')) != source_binding:
        raise ValueError('Logical binding must pin its original compiler owner input')
    expected_kind = 'CurrentOriginalCompilerLogicalBindingProjection'
    if (receipt.get('kind') != expected_kind or receipt.get('status') != 'ProjectedSourceOnly' or
            receipt.get('logical_binding') != binding_desc or receipt.get('compiler_context') != context_pin or
            receipt.get('configuration_qualification') != qualification_pin or
            receipt.get('binding_input') != source_binding or
            receipt.get('baseline_request') != author_request_desc or
            receipt.get('baseline_result') != author_result_desc or
            receipt.get('setup_hash_bound') != target.get('setup_hash') or
            receipt.get('logical_binding_fingerprint') != target.get('mission_logical_binding_fingerprint') or
            receipt.get('negative_controls') != ['wrong_source_content_hash_rejected', 'missing_role_framing_rejected'] or
            any(receipt.get(k) is not False for k in ('world_allocated', 'native_capture', 'runtime_admission',
                                                       'source_fit_certified', 'mission_ready'))):
        raise ValueError('Logical-binding projection receipt does not prove the exact current target')
    replay_request_desc = receipt.get('authoring_request')
    replay_result_desc = receipt.get('authoring_result')
    roundtrip_desc = receipt.get('roundtrip')
    if any(not isinstance(row, dict) or set(row) != {'path', 'sha256'} or pin(row.get('path', '')) != row
           for row in (replay_request_desc, replay_result_desc, roundtrip_desc)):
        raise ValueError('Current original compiler replay request/result/roundtrip descriptors required')
    replay_request = read(replay_request_desc['path'])
    replay_result = read(replay_result_desc['path'])
    roundtrip = read(roundtrip_desc['path'])
    if (replay_result.get('kind') != 'SkirmishMissionAuthoringPreflightResult' or
            replay_result.get('status') != 'ConfigurationCompiled' or replay_result.get('compiler_accepted') is not True or
            replay_result.get('catalog_id') != target.get('catalog_id') or
            replay_result.get('seed') != target.get('seed') or
            replay_result.get('operation_map_id') != target.get('operation_map_id') or
            replay_result.get('source_content_hash') != target.get('source_content_hash') or
            replay_result.get('layout_fingerprint') != target.get('layout_fingerprint') or
            replay_result.get('resolved_setup', {}).get('setup_hash') != target.get('setup_hash') or
            any(replay_result.get(k) is not False for k in ('world_allocated', 'source_fit_verified', 'mission_ready', 'native_acceptance')) or
            replay_request.get('binding') != source_binding or
            replay_result.get('input_pins', {}).get(replay_request_desc['path'], {}).get('sha256') != replay_request_desc['sha256'] or
            replay_result.get('input_pins', {}).get(source_binding['path'], {}).get('sha256') != source_binding['sha256']):
        raise ValueError('Fresh source-owner binding replay differs from the selected compiler target')
    if (roundtrip.get('setup_hash_with_binding') != target.get('setup_hash') or
            roundtrip.get('portable_roundtrip_setup_hash') != target.get('setup_hash') or
            roundtrip.get('logical_binding_fingerprint') != target.get('mission_logical_binding_fingerprint') or
            roundtrip.get('portable_roundtrip_fingerprint') != target.get('mission_logical_binding_fingerprint') or
            roundtrip.get('negative_controls') != ['wrong_source_content_hash_rejected', 'missing_role_framing_rejected']):
        raise ValueError('Original compiler binding roundtrip differs from exact selected target')
    return binding_desc, receipt_desc, source_binding


def validate_camera_projection(camera_path, camera_receipt_path, context_pin,
                               author_request_desc, author_result_desc, author_receipt_path,
                               binding_source_desc):
    camera_desc = pin(camera_path)
    camera_receipt_desc = pin(camera_receipt_path)
    camera = read(camera_desc['path'])
    receipt = read(camera_receipt_desc['path'])
    context = read(context_pin['path'])
    target = context.get('target_context', {})
    if (camera.get('schema_version') != 1 or camera.get('kind') != 'OriginalCompilerDerivedEngineeringCameraContext' or
            camera.get('metadata_compiler_context') != context_pin or camera.get('target_context') != target or
            camera.get('native_camera_context_qualified') is not False or camera.get('source_profile_derived') is not True):
        raise ValueError('Current source-only camera profile must match the exact compiler context')
    inputs = receipt.get('inputs', {})
    for key, expected in (('context', context_pin), ('author_request', author_request_desc),
                          ('author_result', author_result_desc), ('author_receipt', pin(author_receipt_path)),
                          ('binding', binding_source_desc)):
        if inputs.get(expected['path']) != expected['sha256']:
            raise ValueError(f'Camera projection receipt must bind current {key} bytes')
    binding_receipt_rows = [(path, digest) for path, digest in inputs.items()
                            if 'binding-roundtrip' in path and path.endswith('/receipt.json')]
    if len(binding_receipt_rows) != 1 or pin(binding_receipt_rows[0][0]) != {
            'path': binding_receipt_rows[0][0], 'sha256': binding_receipt_rows[0][1]}:
        raise ValueError('Camera projection receipt must bind the exact original compiler binding roundtrip')
    owner_receipt_path = Path(binding_receipt_rows[0][0])
    owner_receipt = read(owner_receipt_path)
    owner_result_desc = owner_receipt.get('compiler_probe_result')
    owner_kind = owner_receipt.get('kind')
    owner_current_schema = owner_kind == 'S050SourceOnlyOriginalCompilerBindingRoundtrip'
    owner_r9_schema = owner_kind == 'S050R9OriginalCompilerBindingRoundtrip'
    owner_result_desc = owner_receipt.get('result') if (owner_current_schema or owner_r9_schema) else owner_receipt.get('compiler_probe_result')
    owner_setup_hash = (owner_receipt.get('compiler_result', {}).get('setup_hash') if owner_r9_schema else
                        owner_receipt.get('binding_setup_hash') if owner_current_schema else owner_receipt.get('setup_hash'))
    owner_binding_fingerprint = (owner_receipt.get('compiler_result', {}).get('logical_binding_fingerprint') if owner_r9_schema else
                                 owner_receipt.get('logical_binding_fingerprint'))
    owner_roundtrip = (read(owner_result_desc['path']).get('binding_roundtrip', {})
                       if owner_r9_schema and isinstance(owner_result_desc, dict) else
                       owner_receipt.get('binding_roundtrip', {}))
    if owner_r9_schema:
        owner_build_record = owner_receipt.get('source_build')
        if not isinstance(owner_build_record, dict):
            raise ValueError('S050 r9 camera binding owner must retain its source build receipt')
        owner_build_desc = {'path': owner_build_record.get('path'), 'sha256': owner_build_record.get('sha256')}
        owner_build = read(owner_build_desc['path']) if isinstance(owner_build_desc['path'], str) else {}
        owner_source_pins = owner_build.get('source_and_reference_pins')
        if (set(owner_build_desc) != {'path', 'sha256'} or pin(owner_build_desc['path']) != owner_build_desc or
                owner_build.get('kind') != 'SkirmishSourceSimulationBuild' or owner_build.get('status') != 'Passed' or
                owner_build.get('compile_exit') != 0 or owner_build.get('mission') != 'S002' or
                owner_build.get('seed') != 104731 or owner_build.get('repository_root') != str(ROOT) or
                not isinstance(owner_source_pins, dict) or not owner_source_pins or
                owner_build_record.get('status') != 'Passed' or owner_build_record.get('mission') != 'S002' or
                owner_build_record.get('seed') != 104731 or owner_build_record.get('pin_count') != len(owner_source_pins) or
                owner_build_record.get('pin_mismatches') != 0):
            raise ValueError('S050 r9 camera binding owner source build is not a complete checked S002 build')
        for source_path, expected_sha in owner_source_pins.items():
            if pin(source_path)['sha256'] != expected_sha:
                raise ValueError('S050 r9 camera binding owner source build pin drift: ' + source_path)
        source_inputs = owner_receipt.get('source_inputs')
        required_owner_inputs = {'layout', 'graph', 'matrix', 'template', 'probe_source', 'probe_executable', 'compile_response'}
        if not isinstance(source_inputs, dict) or set(source_inputs) != required_owner_inputs:
            raise ValueError('S050 r9 camera binding owner exact source-input descriptor set required')
        for name, descriptor in source_inputs.items():
            if (not isinstance(descriptor, dict) or set(descriptor) != {'path', 'sha256'} or
                    pin(descriptor.get('path', '')) != descriptor):
                raise ValueError('S050 r9 camera binding owner source input drift: ' + name)
    if (owner_kind not in {'S050GridAlignedOriginalCompilerBindingRoundtrip',
                           'S050SourceOnlyOriginalCompilerBindingRoundtrip',
                           'S050R9OriginalCompilerBindingRoundtrip'} or
            owner_receipt.get('status') != ('PassedSourceOnly' if owner_r9_schema else 'OriginalCompilerReturned') or
            owner_receipt.get('binding') != binding_source_desc or
            (owner_current_schema and (owner_receipt.get('mission') != target.get('catalog_id') or
                                       owner_receipt.get('seed') != target.get('seed') or
                                       owner_receipt.get('resource_preset_applied') is not False)) or
            (owner_r9_schema and (owner_receipt.get('compiler_accepted') is not True or
                                  owner_receipt.get('compiler_result', {}).get('catalog_id') != target.get('catalog_id') or
                                  owner_receipt.get('compiler_result', {}).get('seed') != target.get('seed') or
                                  owner_receipt.get('resource_preset_applied') is not False or
                                  owner_receipt.get('source_fit_verified') is not False or
                                  owner_receipt.get('compiler_result') != {
                                      'catalog_id': target.get('catalog_id'), 'seed': target.get('seed'),
                                      'layout_id': read(owner_result_desc['path']).get('resolved_setup', {}).get('layout_id') if isinstance(owner_result_desc, dict) else None,
                                      'setup_hash': target.get('setup_hash'),
                                      'layout_semantic_fingerprint': target.get('layout_fingerprint'),
                                      'logical_binding_fingerprint': target.get('mission_logical_binding_fingerprint'),
                                      'negative_controls': ['source_content_hash', 'view_fingerprint', 'missing_role_framing']})) or
            owner_setup_hash != target.get('setup_hash') or
            owner_binding_fingerprint != target.get('mission_logical_binding_fingerprint') or
            owner_receipt.get('compiler_accepted') is not True or owner_receipt.get('world_initialized') is not False or
            owner_receipt.get('mission_ready') is not False or owner_receipt.get('native_visual_accepted') is not False or
            owner_receipt.get('source_fit_verified') is not False or owner_receipt.get('runtime_admission') is not False or
            owner_roundtrip.get('accepted') is not True or owner_roundtrip.get('setup_hash') != target.get('setup_hash') or
            owner_roundtrip.get('logical_binding_fingerprint') != target.get('mission_logical_binding_fingerprint') or
            owner_roundtrip.get('native_visual_accepted') is not False or owner_roundtrip.get('source_fit_verified') is not False or
            (owner_r9_schema and owner_roundtrip.get('negative_controls') !=
             ['source_content_hash', 'view_fingerprint', 'missing_role_framing']) or
            any(inputs.get(path) != digest for path, digest in binding_receipt_rows)):
        raise ValueError('Camera projection binding owner receipt differs from the selected source-owner proof')
    if not isinstance(owner_result_desc, dict) or set(owner_result_desc) != {'path', 'sha256'} or pin(owner_result_desc['path']) != owner_result_desc:
        raise ValueError('Camera projection binding owner result must be pinned')
    owner_result = read(owner_result_desc['path'])
    if inputs.get(owner_result_desc['path']) != owner_result_desc['sha256']:
        raise ValueError('Camera projection must hash-pin the binding owner result')
    owner_roundtrip = (owner_result.get('binding_roundtrip', {}) if owner_r9_schema else
                       owner_receipt.get('binding_roundtrip', {}))
    if (owner_result.get('compiler_accepted') is not True or owner_result.get('world_initialized') is not False or
            owner_result.get('mission_ready') is not False or owner_roundtrip.get('accepted') is not True or
            owner_roundtrip.get('setup_hash') != target.get('setup_hash') or
            owner_roundtrip.get('logical_binding_fingerprint') != target.get('mission_logical_binding_fingerprint') or
            owner_roundtrip.get('native_visual_accepted') is not False or
            owner_roundtrip.get('source_fit_verified') is not False):
        raise ValueError('Camera projection owner roundtrip result is not the current nonready source proof')
    if (owner_r9_schema and (owner_result.get('kind') != 'S050IncompleteOriginalCompilerProbe' or
            owner_result.get('catalog_id') != target.get('catalog_id') or
            owner_result.get('resolved_setup', {}).get('layout_id') != owner_receipt.get('compiler_result', {}).get('layout_id') or
            owner_result.get('engineering_view', {}).get('layout_semantic_fingerprint') != target.get('layout_fingerprint') or
            owner_result.get('binding_roundtrip') != owner_roundtrip)):
        raise ValueError('Camera projection r9 owner result differs from the exact target binding receipt')
    adapter_desc = receipt.get('camera_adapter')
    if not isinstance(adapter_desc, dict) or pin(adapter_desc.get('path', '')) != adapter_desc:
        raise ValueError('Pinned camera adapter required')
    projector_path = Path(__file__).resolve().with_name('project_compiler_selected_camera_profile.py')
    spec = importlib.util.spec_from_file_location('_s050_camera_contract', projector_path)
    if spec is None or spec.loader is None:
        raise ValueError('Current camera projection contract unavailable')
    camera_contract = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(camera_contract)
    camera_contract._validate_adapter(adapter_desc, receipt.get('historical_function_profile'))
    if (receipt.get('kind') != 'CompilerSelectedSourceOnlyCameraProfileProjection' or
            receipt.get('status') != 'ProjectedSourceOnly' or receipt.get('profile') != camera_desc or
            receipt.get('target_context') != target or
            any(receipt.get(k) is not False for k in ('world_allocated', 'native_camera_context_qualified',
                                                       'native_visual_readiness', 'player_camera_readiness',
                                                       'source_fit_certified', 'mission_ready'))):
        raise ValueError('Camera projection receipt does not prove current nonready compiler context')
    return camera_desc, camera_receipt_desc, adapter_desc


def validate_death_mechanical_projection(projection_path, receipt_path):
    projection_desc, receipt_desc = pin(projection_path), pin(receipt_path)
    if Path(projection_desc['path']).stat().st_size > 1024 * 1024 or Path(receipt_desc['path']).stat().st_size > 1024 * 1024:
        raise ValueError('Bounded S050 source diagnostic death projection and receipt required')
    projection, receipt = read(projection_path), read(receipt_path)
    expected_receipt_fields = {'schema_version', 'kind', 'status', 'source_capture', 'source_audit',
        'prefab_key_set', 'capture_root', 'source_pin_count', 'projection_bytes', 'projection_sha256',
        'rows', 'full_mission_initialized', 'renderer_qualified', 'runtime_admitted', 'mission_ready', 'projection'}
    if (set(receipt) != expected_receipt_fields or receipt.get('schema_version') != 1 or
            receipt.get('kind') != 'S050SourceDiagnosticDeathProjectionReceipt' or
            receipt.get('status') != 'ProjectedSourceDiagnosticOnly' or receipt.get('rows') != 16 or
            receipt.get('projection') != projection_desc or receipt.get('projection_bytes') != Path(projection_path).stat().st_size or
            receipt.get('projection_sha256') != projection_desc['sha256'] or
            any(receipt.get(field) is not False for field in ('full_mission_initialized', 'renderer_qualified', 'runtime_admitted', 'mission_ready'))):
        raise ValueError('Exact source-diagnostic death projection receipt required')
    source_capture, source_audit, keys = receipt.get('source_capture'), receipt.get('source_audit'), receipt.get('prefab_key_set')
    if (not isinstance(source_capture, dict) or pin(source_capture['path']) != source_capture or
            not isinstance(source_audit, dict) or pin(source_audit['path']) != source_audit or
            not isinstance(keys, dict) or pin(keys['path']) != keys or keys['sha256'] != '2e207fab1547fd55d56ddbb9919c73da4c942ccf2b85dd6d8619a5cf6bc481f7' or
            not isinstance(receipt.get('capture_root'), str) or Path(receipt['capture_root']).resolve() != ROOT):
        raise ValueError('Current S050 death source/audit/key descriptor closure required')
    audit = read(source_audit['path'])
    capture = read(source_capture['path'])
    key_values = read(keys['path'])
    if (audit.get('status') != 'PassedForMechanicalReadsetOnly' or audit.get('readset') != source_capture or
            audit.get('prefab_key_set', {}).get('sha256') != keys['sha256'] or
            audit.get('source_pin_audit') != {'count': receipt.get('source_pin_count'), 'mismatches': 0} or
            audit.get('exact_pass_marker_count') != 1 or
            any(audit.get('flags', {}).get(field) is not False for field in ('full_mission_initialized', 'renderer_qualified', 'runtime_admitted'))):
        raise ValueError('Current S050 original-owner mechanical audit receipt required')
    if (capture.get('kind') != 'NativeSelectedPrefabDeathMechanicalReadset' or
            capture.get('authority') != 'OriginalNativePrepare_OriginalParentSystem_OriginalKernel_NotMissionState' or
            capture.get('keys_sha256') != keys['sha256'] or
            capture.get('selected_prefab_set_mode') != 'ExplicitHashPinnedGenericSet' or
            any(capture.get(field) is not False for field in ('full_mission_initialized', 'renderer_qualified', 'runtime_admitted')) or
            not isinstance(key_values, list) or len(key_values) != 16 or len(set(key_values)) != 16 or
            capture.get('source_pins', {}).get(keys['path']) != keys['sha256']):
        raise ValueError('Exact selected-16 S050 original owner capture and key binding required')
    raw_rows = capture.get('rows')
    if not isinstance(raw_rows, list) or len(raw_rows) != 16 or [row.get('key') for row in raw_rows] != key_values:
        raise ValueError('Exact ordered S050 original owner death rows required')
    expected_rows = []
    for row in raw_rows:
        if (row.get('actual_original_prepare_bits') != row.get('reversed_child_original_prepare_bits') or
                row.get('actual_original_prepare_bits') != row.get('original_fold_bits') or
                row.get('actual_original_prepare_bits') != row.get('unit_death_time_remaining_assignment_bits')):
            raise ValueError('Original Prepare/child/fold result mismatch in death source')
        links = []
        for visual in row.get('visual_readset', []):
            if visual.get('has_animator_link') is True:
                links.append({field: visual[field] for field in ('animator_exists', 'blob_created', 'animation_count', 'clips')})
        expected_rows.append({field: row[field] for field in (
            'key', 'configured_duration_bits', 'death_animation_index', 'has_death_animation_selection',
            'configured_wreck_bypasses_prepare', 'legacy_wreck_bypasses_prepare')} |
            {'linked_animation_inputs': links})
    if (projection.get('schema_version') != 1 or projection.get('kind') != 'S050SourceDiagnosticDeathMechanicalProjection' or
            projection.get('source_capture') != source_capture or projection.get('source_audit') != source_audit or
            projection.get('prefab_key_set') != keys or projection.get('keys_sha256') != keys['sha256'] or
            projection.get('rows') != expected_rows or
            any(projection.get(field) is not False for field in ('full_mission_initialized', 'renderer_qualified', 'runtime_admitted', 'mission_ready'))):
        raise ValueError('Compact S050 diagnostic projection changed from current owner provenance')
    return {'s050_source_diagnostic': {'projection': projection_desc, 'receipt': receipt_desc}}


def build_inputs(*, author_request_path=None, author_result_path=None, physical_request_path=None,
                 unit_metadata_path=None, unit_audit_path=None, building_metadata_path=None,
                 building_audit_path=None, package_path=None, package_preflight_path=None,
                 runtime_unit_projection_path=None, runtime_building_projection_path=None,
                 runtime_projection_receipt_path=None, death_projection_path=None,
                 death_projection_receipt_path=None, source_graph_path=None,
                 source_graph_receipt_path=None, reference_templates_path=None,
                 context_path=None, qualification_path=None, binding_path=None,
                 binding_receipt_path=None, camera_path=None, camera_receipt_path=None,
                 configured_metadata_path=None):
    author_request_path = Path(author_request_path) if author_request_path else AUTHOR / 'authoring-request.json'
    author_result_path = Path(author_result_path) if author_result_path else AUTHOR / 'authoring-result.json'
    physical_request_path = Path(physical_request_path) if physical_request_path else PHYSICAL_REQUEST
    unit_metadata_path = Path(unit_metadata_path) if unit_metadata_path else UNITS
    unit_audit_path = Path(unit_audit_path) if unit_audit_path else UNIT_AUDIT
    building_metadata_path = Path(building_metadata_path) if building_metadata_path else BUILDINGS
    building_audit_path = Path(building_audit_path) if building_audit_path else BUILDING_AUDIT
    package_path = Path(package_path) if package_path else PACKAGE
    package_preflight_path = Path(package_preflight_path) if package_preflight_path else PACKAGE_PREFLIGHT
    reference_templates_path = Path(reference_templates_path) if reference_templates_path else REFERENCE
    context_path = Path(context_path) if context_path else CONTEXT
    qualification_path = Path(qualification_path) if qualification_path else QUALIFICATION
    binding_path = Path(binding_path) if binding_path else PROJECTED_BINDING
    binding_receipt_path = Path(binding_receipt_path) if binding_receipt_path else BINDING_RECEIPT
    camera_path = Path(camera_path) if camera_path else CAMERA
    camera_receipt_path = Path(camera_receipt_path) if camera_receipt_path else CAMERA_RECEIPT
    configured_metadata_path = Path(configured_metadata_path) if configured_metadata_path else CONFIGURED
    if bool(source_graph_path) != bool(source_graph_receipt_path):
        raise ValueError('Selected Campaign graph and extension receipt must be supplied together')
    author_request = read(author_request_path)
    author_result = read(author_result_path)
    author_result['_path'] = str(Path(author_result_path).resolve(strict=True))
    graph_path = Path(source_graph_path) if source_graph_path else GRAPH
    graph = read(graph_path)
    context = read(context_path)
    qualification = read(qualification_path)
    binding = read(binding_path)
    binding_receipt = read(binding_receipt_path)
    camera = read(camera_path)
    camera_receipt = read(camera_receipt_path)
    package = read(package_path)
    package_preflight = read(package_preflight_path)
    binding_pin = pin(binding_path)
    context_pin = pin(context_path)
    qualification_pin = pin(qualification_path)
    graph_pin = pin(graph_path)
    graph_extension_desc = None
    if source_graph_path:
        graph_extension_desc = validate_s050_grid_graph_extension(
            graph_path, source_graph_receipt_path, package_path, package_preflight_path)

    if (author_request.get('catalog_id'), author_request.get('seed'), author_request.get('size'), author_request.get('difficulty')) != ('S050', 104779, 'Standard', 'Regular'):
        raise ValueError('Exact current S050/104779/Standard/Regular author request required')
    if author_result.get('status') != 'ConfigurationCompiled' or author_result.get('compiler_accepted') is not True or any(author_result.get(k) is not False for k in ('world_allocated', 'source_fit_verified', 'mission_ready', 'native_acceptance')):
        raise ValueError('Source-only compiled author result required')
    if author_result.get('kind') != 'SkirmishMissionAuthoringPreflightResult' or author_result.get('catalog_id') != 'S050' or author_result.get('seed') != 104779:
        raise ValueError('Exact compiler-selected S050 result required')
    if context.get('kind') != 'OriginalCompilerDerivedNativeFactoryContext' or context.get('target_context', {}).get('setup_hash') != author_result.get('resolved_setup', {}).get('setup_hash') or context.get('target_context', {}).get('catalog_id') != 'S050':
        raise ValueError('Current compiler context and author result differ')
    context_target=context.get('target_context', {})
    for key in ('catalog_id','seed','operation_map_id','source_content_hash','source_fingerprint','layout_fingerprint'):
        expected=author_result.get('catalog_id' if key=='catalog_id' else key)
        if key=='seed': expected=author_result.get('seed')
        if context_target.get(key)!=expected:
            raise ValueError(f'Validated compiler context and selected result differ: {key}')
    if qualification != {'schema_version': 1, 'kind': 'ExperimentalGroundConfigurationQualification',
                         'runtime_backend': 'TypedGroundS026V1', 'compiler_context': context_pin,
                         'scope': 'OriginalCompilerAndQualifiedFactoryInputs_SourceOnly',
                         'native_context_qualified': False, 'mission_ready': False}:
        raise ValueError('Exact nonreadiness S050 source qualification required')
    if binding.get('metadata_compiler_context') != context_pin or binding.get('target_context') != context.get('target_context') or binding.get('mission_ready') is not False or binding.get('native_capture') is not False:
        raise ValueError('Current compiler-selected logical binding projection required')
    if binding_receipt.get('status') != 'ProjectedSourceOnly' or binding_receipt.get('logical_binding') != binding_pin or binding_receipt.get('compiler_context') != context_pin or binding_receipt.get('runtime_admission') is not False:
        raise ValueError('Current binding projection receipt/context required')
    binding_pin, binding_receipt_pin, source_binding_desc = validate_current_binding_projection(
        binding_path, binding_receipt_path, context_pin, qualification_pin,
        pin(author_request_path), pin(author_result_path))
    if camera.get('metadata_compiler_context') != context_pin or camera.get('target_context') != context.get('target_context') or camera.get('native_camera_context_qualified') is not False or camera.get('source_profile_derived') is not True:
        raise ValueError('Current selected engineering camera profile required')
    author_receipt_path = Path(author_result_path).with_name('authoring-receipt.json')
    camera_pin, camera_receipt_pin, adapter_desc = validate_camera_projection(
        camera_path, camera_receipt_path, context_pin, pin(author_request_path),
        pin(author_result_path), author_receipt_path, source_binding_desc)
    selected_map = descriptor(ROOT / author_request['campaign_source_path'])
    if package.get('static_map', {}).get('operation_map_id') != 'opmap.ch05.citywide_alert' or package.get('static_map', {}).get('definition') != selected_map:
        raise ValueError('S050 current selected Campaign map definition/package mismatch')
    if package_preflight.get('status') != 'SourceOnlyPreflightPassed' or package_preflight.get('world_created') is not False or package_preflight.get('operation_map_id') != 'opmap.ch05.citywide_alert' or any(package_preflight.get(k) is not False for k in ('source_fit_certified', 'mission_ready', 'active_water_qualified')):
        raise ValueError('Current source-only same-map package preflight required')
    if package_preflight.get('frame') != package.get('frame'):
        raise ValueError('Same-map package preflight frame differs')
    physical_request_desc = validate_compiler_physical_request(physical_request_path, author_request,
        author_result, package)

    source_pins = graph.get('source_pins')
    if not isinstance(source_pins, dict) or not source_pins:
        raise ValueError('Current compiler source graph pin set required')
    for path, expected in source_pins.items():
        if pin(path)['sha256'] != expected:
            raise ValueError(f'Current authored graph source drift: {path}')
    for path, entry in author_result.get('input_pins', {}).items():
        actual = pin(path)
        if actual['sha256'] != entry.get('sha256'):
            raise ValueError(f'Author-result input pin drift: {path}')

    building_desc, building_audit_desc, owner_semantic = validate_preset_handoff('building', building_metadata_path,
        building_audit_path, author_result_path, author_result, physical_request_desc,
        author_request.get('resource_preset'), context_target)
    unit_desc, unit_audit_desc, _ = validate_preset_handoff('unit', unit_metadata_path,
        unit_audit_path, author_result_path, author_result, physical_request_desc,
        author_request.get('resource_preset'), context_target, owner_semantic)
    runtime_projection_desc = None
    projection_options = (runtime_unit_projection_path, runtime_building_projection_path,
                          runtime_projection_receipt_path)
    if any(value is not None for value in projection_options):
        if not all(value is not None for value in projection_options):
            raise ValueError('Unit/building runtime projection paths and receipt must be supplied together')
        runtime_projection_desc, unit_desc, building_desc = validate_runtime_metadata_projection(
            runtime_projection_receipt_path, runtime_unit_projection_path,
            runtime_building_projection_path, context_pin,
            unit_metadata_path and pin(unit_metadata_path), unit_audit_desc,
            building_metadata_path and pin(building_metadata_path), building_audit_desc,
            owner_semantic, configured_metadata_path)
    if reference_templates_path != REFERENCE:
        reference_templates = validate_focused_prefab_projection(
            reference_templates_path, context_pin, qualification_pin, graph_pin,
            building_desc, unit_desc, pin(author_result_path))
        prefab_receipt = read(Path(reference_templates_path).resolve(strict=True).parent / 'receipt.json')
    else:
        reference_templates = read(REFERENCE)
        prefab_receipt = read(BASE / 's050-focused-prefab-projection-20261115-r1/receipt.json')
    if death_projection_path is None and death_projection_receipt_path is None:
        death_audit = read(DEATH_AUDIT)
        if death_audit.get('status') != 'PassedForMechanicalReadsetOnly' or death_audit.get('readset') != pin(DEATH):
            raise ValueError('Current mechanical death readset audit required')
    elif death_projection_path is None or death_projection_receipt_path is None:
        raise ValueError('S050 death projection and receipt must be supplied together')
    if prefab_receipt.get('status') != 'ProjectedSourceOnlyFromFailedRawCapture' or prefab_receipt.get('mission_ready') is not False:
        raise ValueError('Honest source-only projection of retained failed prefab capture required')

    context_data = context
    text_data = read(TEXT)
    current_text_pins = {}
    for captured_path, expected in text_data.get('source_pins', {}).items():
        captured = Path(captured_path)
        if not captured.is_absolute() or not captured.is_relative_to(TEXT_CAPTURE_ROOT):
            raise ValueError('Shared command text source escaped its original root')
        current = ROOT / captured.relative_to(TEXT_CAPTURE_ROOT)
        if pin(current)['sha256'] != expected:
            raise ValueError(f'Current shared command text pin drift: {current}')
        current_text_pins[str(current)] = expected

    profile_refs = author_request.get('profiles')
    if not isinstance(profile_refs, dict) or set(profile_refs) != {'objective', 'army', 'start', 'economy', 'upgrades', 'intel', 'roles'}:
        raise ValueError('Exact authored S050 profile set required')
    for path in profile_refs.values():
        matches = [p for p in source_pins if p.replace('\\', '/').endswith('/' + path)]
        if len(matches) != 1 or source_pins[matches[0]] != pin(ROOT / path)['sha256']:
            raise ValueError(f'Authored profile not in current graph: {path}')

    static_map = package['static_map']
    initial = {
        'schema_version': 1, 'kind': 'GeneratedMissionInitialization',
        'authority': 'AuthoredConfigAndBakedCampaignMap',
        'source_epoch': 'OriginalCompilerDerivedNativeFactoryContext/v1',
        'mission_id': 'S050', 'selection': {'difficulty_id': 2, 'size_id': 1, 'seed': 104779},
        'source_config_graph': graph_pin,
        'setup_matrix': author_request['setup_matrix'],
        'canonical_battle_catalog': descriptor(ROOT / 'Assets/Game/Resources/SkirmishBattleCatalog.asset'),
        'selected_operation_map': descriptor(ROOT / author_request['campaign_source_path']),
        'static_map': static_map,
        'world_profile': {'identity_backend': 'FreshLogicalEntitiesV1', 'system_initialization': 'OriginalOnCreate', 'future_native_inputs': False},
        'reference_templates': reference_templates,
        'building_definitions': building_desc,
        'building_placement_config': descriptor(ROOT / 'Assets/Game/Configs/Skirmish/Construction.asset'),
        'placement_domain': {},
        'unit_production_metadata': unit_desc,
        'building_request_metadata': descriptor(configured_metadata_path),
        'operation_map_rotation_reference': descriptor(ROTATION),
        'metadata_compiler_context': context_pin,
        'configuration_qualification': qualification_pin,
        'metadata_compatibility': context_data['native_factory_reference'],
        'metadata_source_origin': context_data['native_origin_descriptor'],
        'typed_mission_configuration': {'profiles': profile_refs, 'campaign_source_path': author_request['campaign_source_path'], 'layout': author_request['layout']},
        'mission_logical_binding': binding_pin,
        'death_mechanical_inputs': (validate_death_mechanical_projection(death_projection_path, death_projection_receipt_path)
            if death_projection_path is not None and death_projection_receipt_path is not None else descriptor(DEATH)),
        'command_text_data': descriptor(TEXT),
        'engineering_camera_profile': camera_pin,
        'camera_engine_adapter': adapter_desc,
    }
    descriptor_map = {p: sha for p, sha in source_pins.items()}
    # Runtime-only current assets and source-map artifacts remain hash-bound by the bundle and host preflight.
    road_data = ['Assets/Game/Configs/Scene/Game_RoadBuild_Config.asset',
                 'Assets/Game/Configs/Scene/Game_RoadBuild_Config.asset.meta',
                 'Assets/Game/Configs/Scene/Game_RuntimeCitySpawner_Config.asset',
                 'Assets/Game/Configs/Scene/Game_RuntimeCitySpawner_Config.asset.meta']
    road_sources = ['Assets/Game/Scripts/Configs/Scene/RoadBuildSystemSceneConfigAsset.cs.meta',
                    'Assets/Game/Scripts/Configs/Scene/RuntimeCitySpawnerSystemSceneConfigAsset.cs.meta']
    run_inputs = {
        'schema_version': 1, 'kind': 'SkirmishSourceSimulationDataBundle', 'mission': 'S050', 'seed': 104779,
        'runtime_backend': 'TypedGroundS026V1', 'unit_metadata': unit_desc,
        'static_clearance': descriptor(package_path.parent / 'clearance-projection.json'),
        'source_derived_map': descriptor(package_path),
        'compiler_selected_physical_request': physical_request_desc,
        'unit_owner_audit': unit_audit_desc,
        'building_owner_audit': building_audit_desc,
        'mission_scene': descriptor(ROOT / 'Assets/Game/Scenes/Match.unity'),
        'camera_profile': camera_pin, 'camera_values': descriptor(VALUES), 'camera_motion': descriptor(MOTION),
        'runtime_data_inputs': {'schema_version': 1, 'kind': 'CurrentRoadSceneRuntimeInputs', 'data_root': str(ROOT),
            'data_files': {p: digest((ROOT / p).read_bytes()) for p in road_data},
            'source_files': {p: digest((ROOT / p).read_bytes()) for p in road_sources}},
        'command_text_sources': {'captured_root': str(TEXT_CAPTURE_ROOT), 'current_root': str(ROOT),
            'input_pins': current_text_pins, 'artifact_sha256': pin(TEXT)['sha256']},
        'nonclaims': {'world_allocated': False, 'mission_ready': False, 'source_fit_certified': False,
                      'native_context_qualified': False, 'camera_qualified': False},
    }
    run_inputs['compiler_owner_projection_provenance'] = {
        'schema_version': 1, 'kind': 'S050CurrentCompilerOwnerProjectionChain',
        'compiler_context': context_pin, 'configuration_qualification': qualification_pin,
        'logical_binding': binding_pin, 'logical_binding_projection_receipt': binding_receipt_pin,
        'camera_profile': camera_pin, 'camera_projection_receipt': camera_receipt_pin,
        'camera_adapter': adapter_desc, 'metadata_projection_receipt': runtime_projection_desc,
        'unit_owner': pin(unit_metadata_path), 'unit_owner_audit': unit_audit_desc,
        'building_owner': pin(building_metadata_path), 'building_owner_audit': building_audit_desc,
        'configured_buildings': pin(configured_metadata_path),
        'focused_prefab_projection': reference_templates,
        'world_allocated': False, 'runtime_admission': False,
        'source_fit_certified': False, 'mission_ready': False,
    }
    if adapter_desc != pin(ADAPTER):
        raise ValueError('Camera adapter contract descriptor differs from the audited adapter')
    # Keep the selected map package exact; package preflight also consumes all source and capture pins.
    require_descriptor(descriptor(package_path), {str(package_path.resolve()): pin(package_path)['sha256']}, 'source-derived map package')
    return initial, run_inputs, descriptor_map, {
        'request': pin(author_request_path), 'result': pin(author_result_path)}, runtime_projection_desc, graph_extension_desc


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output-dir', type=Path, required=True)
    parser.add_argument('--resource-preset')
    parser.add_argument('--resource-preset-sha256')
    parser.add_argument('--author-request', type=Path)
    parser.add_argument('--author-result', type=Path)
    parser.add_argument('--physical-request', type=Path)
    parser.add_argument('--unit-metadata', type=Path)
    parser.add_argument('--unit-audit', type=Path)
    parser.add_argument('--building-metadata', type=Path)
    parser.add_argument('--building-audit', type=Path)
    parser.add_argument('--runtime-unit-projection', type=Path)
    parser.add_argument('--runtime-building-projection', type=Path)
    parser.add_argument('--runtime-projection-receipt', type=Path)
    parser.add_argument('--death-projection', type=Path)
    parser.add_argument('--death-projection-receipt', type=Path)
    parser.add_argument('--source-map-package', type=Path)
    parser.add_argument('--source-map-preflight', type=Path)
    parser.add_argument('--source-config-graph', type=Path)
    parser.add_argument('--source-config-graph-receipt', type=Path)
    parser.add_argument('--focused-prefab-projection', type=Path)
    parser.add_argument('--compiler-context', type=Path)
    parser.add_argument('--configuration-qualification', type=Path)
    parser.add_argument('--logical-binding', type=Path)
    parser.add_argument('--logical-binding-receipt', type=Path)
    parser.add_argument('--camera-profile', type=Path)
    parser.add_argument('--camera-profile-receipt', type=Path)
    parser.add_argument('--configured-building-metadata', type=Path)
    args = parser.parse_args()
    if bool(args.resource_preset) != bool(args.resource_preset_sha256):
        raise ValueError('Explicit resource-preset path and SHA256 must be supplied together')
    if bool(args.author_request) != bool(args.author_result):
        raise ValueError('Compiler author request and result paths must be supplied together')
    if bool(args.death_projection) != bool(args.death_projection_receipt):
        raise ValueError('S050 death projection and receipt must be supplied together')
    if bool(args.source_config_graph) != bool(args.source_config_graph_receipt):
        raise ValueError('Selected Campaign graph and receipt must be supplied together')
    initial, run_inputs, graph_pins, owner_authoring, runtime_projection_desc, graph_extension_desc = build_inputs(
        author_request_path=args.author_request, author_result_path=args.author_result,
        physical_request_path=args.physical_request, unit_metadata_path=args.unit_metadata,
        unit_audit_path=args.unit_audit, building_metadata_path=args.building_metadata,
        building_audit_path=args.building_audit, package_path=args.source_map_package,
        package_preflight_path=args.source_map_preflight,
        runtime_unit_projection_path=args.runtime_unit_projection,
        runtime_building_projection_path=args.runtime_building_projection,
        runtime_projection_receipt_path=args.runtime_projection_receipt,
        death_projection_path=args.death_projection,
        death_projection_receipt_path=args.death_projection_receipt,
        source_graph_path=args.source_config_graph,
        source_graph_receipt_path=args.source_config_graph_receipt,
        reference_templates_path=args.focused_prefab_projection,
        context_path=args.compiler_context,
        qualification_path=args.configuration_qualification,
        binding_path=args.logical_binding,
        binding_receipt_path=args.logical_binding_receipt,
        camera_path=args.camera_profile,
        camera_receipt_path=args.camera_profile_receipt,
        configured_metadata_path=args.configured_building_metadata)
    resource = None if not args.resource_preset else {'path': str(Path(args.resource_preset).resolve(strict=True)), 'sha256': args.resource_preset_sha256}
    receipt = compose_envelope(args.output_dir, initial, run_inputs, graph_pins, owner_authoring,
                               resource, runtime_projection_desc, graph_extension_desc)
    print(json.dumps(receipt, sort_keys=True))
    if receipt['status'] == 'AwaitingExplicitResourcePreset':
        raise SystemExit(2)


if __name__ == '__main__':
    main()
