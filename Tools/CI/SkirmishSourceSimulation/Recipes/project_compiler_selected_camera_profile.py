#!/usr/bin/env python3
"""Project a pinned compiler-selected logical view as source-only camera input."""
import argparse
import hashlib
import json
import math
import struct
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
FLOAT_FIELDS = {'AircraftYaw', 'AircraftPitch', 'AircraftFieldOfView', 'AircraftMinimumDistance',
                'AircraftMaximumDistance', 'MinHeight', 'MaxHeight', 'NormalHeight', 'BuildHeight',
                'Pitch', 'Yaw', 'NormalFieldOfView', 'BuildFieldOfView', 'GroundReferenceY',
                'MaximumGroundCenterSlantDistance', 'InfantryMinimumViewportHeight',
                'PlaneMaximumViewportDimension'}
INTEGER_FIELDS = {'OpeningFocusPolicy', 'ObjectiveKind', 'SchemaVersion', 'ContentVersion', 'DefinitionVersion'}
BOOLEAN_FIELDS = {'UseSeparateAircraftYaw', 'AuthoredNonBaseAssault', 'AuthoredGenericBaseAssault'}
STRING_FIELDS = {'OpeningFocusStableActorId', 'ActiveOperationMapId', 'ActiveMapContentHash',
                 'ActiveMapSourceIdentityHash', 'CatalogId', 'DefinitionId',
                 'CampaignSourceContentHash', 'LayoutContentHash'}
SIZE_ENUMS = {'Standard': 1, 'War': 2, 'LargeWar': 3}
DIFFICULTY_ENUMS = {'Recruit': 1, 'Regular': 2, 'Veteran': 3, 'Commander': 4}
ADAPTERS = {
    '09e83dd1cabfa3cbb0c8ad67018b05f00ba49dbaf0365107cae509f1b9de1a5e':
        ('ed81766e5ff2dcf45e04c3f00a78d8d03c93e2c0444f4c33dc514dd970adb446',
         '6b15899f81bdbd364423ffe464b534369aace9000379c3b6f65976fbde72577f',
         'a9150df273b62fb34fcd5e99e9dc8429f02203c9965b62d1306035b18d6ecfb3'),
    '1df644233d5fdfdaba72b5a7a9f9d912aeaba5f4247d0451166e7c33bec9112f':
        ('c405085a36815db00034a4774cc362c0f4b1f9cb36ec8900158889b015f5b68e',
         'd0ae7daf2162e96763fa5ca6dc6c91a43853648a9e477c333cb056af96539a3e',
         'e493247648fc7f360f6f0eb4305fec9c47c22bac34ec68a1f82f7359b7bda3ef'),
    '43f3fa4e824dc370aa2ccd2acaff7ff063ac648481faea40a2c3d2c137a26367':
        ('3bf7d79b8a2a372f53f609a016a6272df54129d1411449a0a7306e4fb0f1f4ed',
         '7af2a92f51cb7af23fa9c01344a6ba227e799f216a7163538edd9a37208af48a',
         'a5982e6cffc9dee7204375c306d619f3a8183fce1910e0ac26305e574f61cb96'),
    '57e1d7569b8d8f773bbcb68f50ee342f71e41cf4d92aeec11ebc1df525237d91':
        ('26188d80946e4d287a085dff6e01a3aafa2fd8f1ff2788b59ff376372cf3760b',
         'd37c03922b44fbfc10cfcf371244569851145ce142d0a13c2246922c578e182f',
         '502b3ec9f76282f92abe84264f01c8a99e4e9e871ce00bf97a4400bffa549925'),
}


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def read_descriptor(descriptor, label):
    if not isinstance(descriptor, dict) or set(descriptor) != {'path', 'sha256'}:
        raise ValueError(f'Exact {label} path/hash descriptor required')
    path = Path(descriptor['path']).resolve(strict=True)
    if not path.is_relative_to(ROOT) or not path.is_file() or path.is_symlink() or sha(path) != descriptor['sha256']:
        raise ValueError(f'Pinned {label} changed or outside repository: {path}')
    return path, json.loads(path.read_bytes())


def descriptor(path):
    path = Path(path).resolve(strict=True)
    return {'path': str(path), 'sha256': sha(path)}


def _float_bits(value):
    if isinstance(value, bool) or not isinstance(value, (int, float)) or not math.isfinite(value):
        raise ValueError('Finite compiled float required')
    raw = struct.unpack('!I', struct.pack('!f', float(value)))[0]
    return {'float_bits': f'{raw:08x}'}


def _profile_fields(view):
    expected = FLOAT_FIELDS | INTEGER_FIELDS | BOOLEAN_FIELDS | STRING_FIELDS
    if not isinstance(view, dict) or set(view) != expected:
        raise ValueError('Complete declared compiler-selected view profile required')
    fields = {}
    for key, value in view.items():
        if key in FLOAT_FIELDS:
            fields[key] = _float_bits(value)
        elif key in INTEGER_FIELDS:
            if isinstance(value, bool) or not isinstance(value, int):
                raise ValueError(f'Compiled integer profile field required: {key}')
            fields[key] = value
        elif key in BOOLEAN_FIELDS:
            if not isinstance(value, bool):
                raise ValueError(f'Compiled boolean profile field required: {key}')
            fields[key] = value
        elif key in STRING_FIELDS:
            if value is not None and not isinstance(value, str):
                raise ValueError(f'Compiled string profile field required: {key}')
            fields[key] = value
        else:
            raise ValueError(f'Unsupported compiled profile field: {key}')
    return fields


def _validate_adapter(adapter_desc, function_desc):
    _, adapter = read_descriptor(adapter_desc, 'camera adapter')
    expected = ADAPTERS.get(adapter_desc['sha256'])
    if expected is None:
        raise ValueError('Camera adapter hash is not in the checked engine contract allowlist')
    fields = {'schema_version', 'kind', 'adapter_version', 'source_template_sha256',
              'engineering_profile_sha256', 'native_values_sha256', 'native_motion_sha256',
              'native_values_schema', 'native_motion_input_schema'}
    if (set(adapter) != fields or adapter.get('schema_version') != 2 or
            adapter.get('kind') != 'SourceNumericCameraEngineAdapterContract' or
            adapter.get('adapter_version') != 'EngineeringWorldNearPointTRS_v2' or
            adapter.get('source_template_sha256') != '30f11fab477a2b014fb6bf0c8c42cc45305d9811947fbb4dfa1b3c65fb0d4c4b' or
            tuple(adapter.get(k) for k in ('engineering_profile_sha256', 'native_values_sha256', 'native_motion_sha256')) != expected or
            adapter.get('native_values_schema') != 2 or adapter.get('native_motion_input_schema') != 2):
        raise ValueError('Pinned camera adapter content differs from its checked contract')
    _, function_profile = read_descriptor(function_desc, 'historical function profile')
    if function_desc['sha256'] != expected[0] or function_profile.get('kind') != 'SkirmishEngineeringCameraProfileReference' or function_profile.get('catalog_id') not in {'S002', 'S026', 'S049'}:
        raise ValueError('Historical function profile is not the exact adapter-bound reference')
    return expected


def project(context_desc, author_request_desc, author_result_desc, author_receipt_desc, binding_desc,
            binding_receipt_desc, adapter_desc, function_profile_desc, output):
    context_path, context = read_descriptor(context_desc, 'compiler context')
    request_path, request = read_descriptor(author_request_desc, 'author request')
    result_path, result = read_descriptor(author_result_desc, 'author result')
    author_receipt_path, author_receipt = read_descriptor(author_receipt_desc, 'author execution receipt')
    binding_path, binding = read_descriptor(binding_desc, 'compiler-selected logical binding')
    receipt_path, receipt = read_descriptor(binding_receipt_desc, 'logical binding compiler receipt')
    context_desc, author_request_desc, author_result_desc = descriptor(context_path), descriptor(request_path), descriptor(result_path)
    author_receipt_desc, binding_desc, binding_receipt_desc = descriptor(author_receipt_path), descriptor(binding_path), descriptor(receipt_path)
    adapter_path, _ = read_descriptor(adapter_desc, 'camera adapter')
    function_profile_path, _ = read_descriptor(function_profile_desc, 'historical function profile')
    adapter_desc, function_profile_desc = descriptor(adapter_path), descriptor(function_profile_path)
    adapter_tuple = _validate_adapter(adapter_desc, function_profile_desc)

    target = context.get('target_context')
    if (context.get('kind') != 'OriginalCompilerDerivedNativeFactoryContext' or
            context.get('schema_version') != 1 or
            context.get('authority') != 'ActualSourceCompilerContext_ImmutableNativeFactoryFacts_NotNativeS050' or
            any(context.get(k) is not False for k in ('world_allocated', 'native_S050_capture', 'source_fit_certified', 'mission_ready', 'runtime_loader_admitted')) or
            context.get('identities_allocated') != 0 or not isinstance(target, dict)):
        raise ValueError('Explicit immutable source-only compiler context required')
    target_keys = ('catalog_id', 'definition_id', 'content_version', 'seed', 'size', 'difficulty',
                   'setup_hash', 'operation_map_id', 'source_content_hash', 'layout_fingerprint',
                   'layout_binding_fingerprint', 'mission_logical_binding_fingerprint')
    if any(k not in target for k in target_keys):
        raise ValueError('Complete compiler target identity required')

    request_binding = request.get('binding')
    if request_binding != binding_desc or request.get('catalog_id') != target['catalog_id'] or request.get('seed') != target['seed']:
        raise ValueError('Author request does not select this exact binding/target')
    if (result.get('kind') != 'SkirmishMissionAuthoringPreflightResult' or
            result.get('status') != 'ConfigurationCompiled' or result.get('catalog_id') != target['catalog_id'] or
            result.get('seed') != target['seed'] or result.get('operation_map_id') != target['operation_map_id'] or
            result.get('source_content_hash') != target['source_content_hash'] or
            result.get('layout_fingerprint') != target['layout_fingerprint'] or
            result.get('resolved_setup', {}).get('setup_hash') != target['setup_hash'] or
            result.get('partial_authoring_capture_for_missing_binding') is not False or
            result.get('world_allocated') is not False or result.get('source_fit_verified') is not False or
            result.get('mission_ready') is not False):
        raise ValueError('Compiler author result differs from source context or has unsupported scope')
    if (author_receipt.get('kind') != 'SkirmishMissionAuthoringInvocation' or
            author_receipt.get('status') != 'ConfigurationCompiled' or author_receipt.get('process_exit') != 0 or
            author_receipt.get('timed_out') is not False or
            author_receipt.get('result_pin', {}).get('path') != str(result_path) or
            author_receipt.get('result_pin', {}).get('sha256') != author_result_desc['sha256'] or
            author_receipt.get('world_allocated') is not False or author_receipt.get('mission_ready') is not False):
        raise ValueError('Completed source-only author result receipt required')
    result_pins = result.get('input_pins', {})
    if not isinstance(result_pins, dict) or result_pins.get(str(request_path), {}).get('sha256') != author_request_desc['sha256'] or result_pins.get(str(binding_path), {}).get('sha256') != binding_desc['sha256']:
        raise ValueError('Compiler author result does not pin exact selected binding')
    for key in ('graph', 'layout', 'setup_matrix', 'binding', 'runtime_config'):
        d = request.get(key)
        if not isinstance(d, dict) or set(d) != {'path', 'sha256'} or not Path(d['path']).is_file() or sha(d['path']) != d['sha256']:
            raise ValueError('Author request source descriptor drift: ' + key)
        if key != 'runtime_config' and result_pins.get(d['path'], {}).get('sha256') != d['sha256']:
            raise ValueError('Compiler result omitted author request source pin: ' + key)

    current_projection = receipt.get('kind') == 'CurrentOriginalCompilerLogicalBindingProjection'
    variants = binding.get('Variants')
    if (binding.get('CatalogId') != target['catalog_id'] or binding.get('DefinitionId') != target['definition_id'] or
            binding.get('DefinitionVersion') != target['content_version'] or binding.get('SourceOperationMapId') != target['operation_map_id'] or
            binding.get('SourceContentHash') != target['source_content_hash'] or
            binding.get('ReviewedLayoutSemanticFingerprint') != target['layout_fingerprint'] or
            not isinstance(variants, list)):
        raise ValueError('Compiler-selected binding identity differs from target')
    if target['size'] not in SIZE_ENUMS or target['difficulty'] not in DIFFICULTY_ENUMS:
        raise ValueError('Supported declared source selection required')
    selected = [v for v in variants if v.get('Size') == SIZE_ENUMS[target['size']] and
                v.get('Difficulty') == DIFFICULTY_ENUMS[target['difficulty']]]
    if len(selected) != 1 or not isinstance(selected[0].get('View'), dict):
        raise ValueError('Exactly one compiler-selected view variant required')
    view = selected[0]['View']
    if (view.get('CatalogId') != target['catalog_id'] or view.get('DefinitionId') != target['definition_id'] or
            view.get('CampaignSourceContentHash') != target['source_content_hash']):
        raise ValueError('Selected view profile identity differs from compiler target')
    if current_projection:
        profile_keys = FLOAT_FIELDS | INTEGER_FIELDS | BOOLEAN_FIELDS | STRING_FIELDS
        serialization_keys = {'ContentVersion', 'DefinitionVersion', 'name', 'hideFlags'}
        if (set(view) != profile_keys | serialization_keys or view.get('name') is not None or
                view.get('hideFlags') != 0 or view.get('DefinitionVersion') != target['content_version']):
            raise ValueError('Current selected view has unexpected serialization metadata')
        view = {key: value for key, value in view.items() if key in profile_keys}

    binding_receipt_binding = receipt.get('binding_input') if current_projection else receipt.get('binding')
    source_only_owner = receipt.get('kind') == 'S050SourceOnlyOriginalCompilerBindingRoundtrip'
    r9_owner = receipt.get('kind') == 'S050R9OriginalCompilerBindingRoundtrip'
    r9_compiler_result = receipt.get('compiler_result', {}) if r9_owner else {}
    receipt_setup_hash = (receipt.get('setup_hash_bound') if current_projection else
                          receipt.get('binding_setup_hash') if source_only_owner else
                          r9_compiler_result.get('setup_hash') if r9_owner else receipt.get('setup_hash'))
    receipt_binding_fingerprint = (receipt.get('logical_binding_fingerprint') if current_projection else
                                   r9_compiler_result.get('logical_binding_fingerprint') if r9_owner else
                                   receipt.get('logical_binding_fingerprint'))
    if (not isinstance(receipt.get('kind'), str) or
            binding_receipt_binding != binding_desc or
            receipt_setup_hash != target['setup_hash'] or
            receipt_binding_fingerprint != target['mission_logical_binding_fingerprint']):
        raise ValueError('Binding owner/compiler receipt does not qualify this exact compiler target')
    if current_projection and receipt.get('status') == 'ProjectedSourceOnly':
        # This producer projects a current S026 compiler binding from an alias
        # authoring request. Validate its independently pinned compiler inputs,
        # portable codec roundtrip and negative controls before accepting it.
        expected_controls = ['wrong_source_content_hash_rejected', 'missing_role_framing_rejected']
        binding_receipt_result = receipt.get('roundtrip')
        roundtrip_desc = binding_receipt_result
        _, roundtrip_result = read_descriptor(roundtrip_desc, 'current logical binding codec roundtrip')
        source_manifest_record = receipt.get('source_manifest')
        if not isinstance(source_manifest_record, dict) or set(source_manifest_record) != {'bytes', 'path', 'sha256'}:
            raise ValueError('Current logical binding source manifest descriptor changed')
        source_manifest_desc = {'path': source_manifest_record['path'], 'sha256': source_manifest_record['sha256']}
        source_manifest_path, source_manifest = read_descriptor(source_manifest_desc, 'current logical binding source manifest')
        host_build_desc = receipt.get('host_build')
        host_build_path, host_build = read_descriptor(host_build_desc, 'current logical binding host source build')
        portable_binding_desc = receipt.get('portable_binding')
        _, portable_binding = read_descriptor(portable_binding_desc, 'current portable logical binding')
        codec_desc = receipt.get('codec_source')
        if (not isinstance(codec_desc, dict) or set(codec_desc) != {'path', 'sha256'}):
            raise ValueError('Current logical binding codec source descriptor changed')
        codec_path = Path(codec_desc['path']).resolve(strict=True)
        if (not codec_path.is_relative_to(ROOT) or codec_path.is_symlink() or
                not codec_path.is_file() or sha(codec_path) != codec_desc['sha256']):
            raise ValueError('Current logical binding codec source changed or is outside repository')
        roundtrip_expected = {
            'catalog_id': target['catalog_id'], 'definition_id': target['definition_id'],
            'content_version': target['content_version'], 'seed': target['seed'],
            'operation_map_id': target['operation_map_id'], 'source_content_hash': target['source_content_hash'],
            'layout_fingerprint': target['layout_fingerprint'],
            'setup_hash_with_binding': target['setup_hash'],
            'logical_binding_fingerprint': target['mission_logical_binding_fingerprint']}
        if (receipt.get('binding_input') != binding_desc or
                receipt.get('baseline_request') != author_request_desc or
                receipt.get('baseline_result') != author_result_desc or
                receipt.get('setup_hash_unbound') == target['setup_hash'] or
                receipt.get('setup_hash_bound') != target['setup_hash'] or
                receipt.get('negative_controls') != expected_controls or
                receipt.get('authoring_input_pin_count') != len(result_pins) or
                receipt.get('factory_context_consumed_pin_count') != len(context.get('consumed_input_pins', {})) or
                receipt.get('host_source_pin_count') != len(host_build.get('source_and_reference_pins', {})) or
                host_build.get('kind') != 'SkirmishSourceSimulationBuild' or host_build.get('status') != 'Passed' or
                host_build.get('compile_exit') != 0 or not isinstance(host_build.get('mission'), str) or
                isinstance(host_build.get('seed'), bool) or not isinstance(host_build.get('seed'), int) or
                host_build.get('repository_root') != str(ROOT) or
                host_build.get('native_or_player_launched') is not False or
                receipt.get('world_allocated') is not False or receipt.get('native_capture') is not False or
                receipt.get('runtime_admission') is not False or receipt.get('source_fit_certified') is not False or
                receipt.get('mission_ready') is not False or
                source_manifest_record['bytes'] != source_manifest_path.stat().st_size or
                source_manifest.get('schema_version') != 1 or not isinstance(source_manifest.get('sources'), list) or
                portable_binding.get('kind') != 'DeclaredOriginalLogicalBindingFieldsWithInlineViews' or
                portable_binding.get('fields', {}).get('CatalogId') != target['catalog_id'] or
                portable_binding.get('fields', {}).get('DefinitionId') != target['definition_id'] or
                portable_binding.get('fields', {}).get('DefinitionVersion') != target['content_version'] or
                portable_binding.get('fields', {}).get('SourceContentHash') != target['source_content_hash'] or
                roundtrip_result.get('kind') != 'CurrentAuthoringLogicalBindingRoundtrip' or
                any(roundtrip_result.get(k) != v for k, v in roundtrip_expected.items()) or
                roundtrip_result.get('portable_roundtrip_setup_hash') != target['setup_hash'] or
                roundtrip_result.get('portable_roundtrip_fingerprint') != target['mission_logical_binding_fingerprint'] or
                roundtrip_result.get('negative_controls') != expected_controls or
                any(roundtrip_result.get(k) is not False for k in
                    ('world_allocated', 'placement_performed', 'native_capture', 'runtime_admission',
                     'source_fit_certified', 'mission_ready'))):
            raise ValueError('Current logical binding projection lacks exact baseline, codec roundtrip, controls or nonclaims')
        host_pins = host_build.get('source_and_reference_pins')
        if not isinstance(host_pins, dict):
            raise ValueError('Current logical binding host build source pin set required')
        for source_path, expected_sha in host_pins.items():
            path = Path(source_path)
            if path.is_symlink() or not path.is_file() or sha(path) != expected_sha:
                raise ValueError('Current logical binding host source pin changed: ' + source_path)
        binding_result = {'compiler_accepted': True, 'world_initialized': False, 'mission_ready': False,
                          'binding_roundtrip': {'accepted': True, 'setup_hash': target['setup_hash'],
                              'logical_binding_fingerprint': target['mission_logical_binding_fingerprint'],
                              'native_visual_accepted': False, 'source_fit_verified': False}}
    elif receipt.get('kind', '').endswith('LogicalBinding') and receipt.get('status') == 'OriginalCompilerRoundtripAccepted':
        binding_receipt_result = receipt.get('result')
        if (receipt.get('physical_source_fit_verified') is not False or
                receipt.get('native_visual_accepted') is not False or receipt.get('mission_ready') is not False):
            raise ValueError('Binding owner/compiler receipt must preserve nonreadiness flags')
    elif (receipt.get('kind') == 'S050GridAlignedOriginalCompilerBindingRoundtrip' and
          receipt.get('status') == 'OriginalCompilerReturned'):
        # The checked S050 owner probe emits its compiler result descriptor as
        # compiler_probe_result and nests the exact roundtrip census in the
        # receipt.  Accept that owner shape only with its complete fail-closed
        # nonclaims and current binding descriptor.
        binding_receipt_result = receipt.get('compiler_probe_result')
        roundtrip_receipt = receipt.get('binding_roundtrip', {})
        if (receipt.get('compiler_accepted') is not True or
                receipt.get('world_initialized') is not False or
                receipt.get('mission_ready') is not False or
                receipt.get('native_visual_accepted') is not False or
                receipt.get('source_fit_verified') is not False or
                receipt.get('runtime_admission') is not False or
                roundtrip_receipt.get('accepted') is not True or
                roundtrip_receipt.get('setup_hash') != target['setup_hash'] or
                roundtrip_receipt.get('logical_binding_fingerprint') != target['mission_logical_binding_fingerprint'] or
                roundtrip_receipt.get('native_visual_accepted') is not False or
                roundtrip_receipt.get('source_fit_verified') is not False):
            raise ValueError('S050 binding owner result lacks exact roundtrip or nonreadiness proof')
    elif (source_only_owner and
          receipt.get('status') == 'OriginalCompilerReturned'):
        # Current standalone owner emits the same roundtrip proof with the
        # result descriptor under `result` and setup/fingerprint under the
        # binding-specific fields. Accept only this exact nested schema.
        binding_receipt_result = receipt.get('result')
        roundtrip_receipt = receipt.get('binding_roundtrip', {})
        if (receipt.get('mission') != target['catalog_id'] or
                receipt.get('seed') != target['seed'] or
                receipt.get('binding_setup_hash') != target['setup_hash'] or
                receipt.get('logical_binding_fingerprint') != target['mission_logical_binding_fingerprint'] or
                receipt.get('compiler_accepted') is not True or
                receipt.get('world_initialized') is not False or
                receipt.get('mission_ready') is not False or
                receipt.get('native_visual_accepted') is not False or
                receipt.get('source_fit_verified') is not False or
                receipt.get('runtime_admission') is not False or
                receipt.get('resource_preset_applied') is not False or
                roundtrip_receipt.get('accepted') is not True or
                roundtrip_receipt.get('setup_hash') != target['setup_hash'] or
                roundtrip_receipt.get('logical_binding_fingerprint') != target['mission_logical_binding_fingerprint'] or
                roundtrip_receipt.get('native_visual_accepted') is not False or
                roundtrip_receipt.get('source_fit_verified') is not False):
            raise ValueError('S050 source-only binding owner result lacks exact roundtrip or nonreadiness proof')
    elif (r9_owner and receipt.get('status') == 'PassedSourceOnly'):
        # This standalone S050 probe receipt is a different, explicit producer
        # schema from the earlier checked roundtrip receipts. Validate its own
        # source build, exact input descriptors, probe output and controls; do
        # not relabel it as an older receipt kind.
        binding_receipt_result = receipt.get('result')
        source_build_record = receipt.get('source_build')
        if not isinstance(source_build_record, dict):
            raise ValueError('S050 r9 binding source build descriptor required')
        source_build_desc = {'path': source_build_record.get('path'),
                             'sha256': source_build_record.get('sha256')}
        _, source_build = read_descriptor(source_build_desc, 'S050 r9 binding probe source build')
        source_pins = source_build.get('source_and_reference_pins')
        if (source_build.get('kind') != 'SkirmishSourceSimulationBuild' or
                source_build.get('status') != 'Passed' or source_build.get('compile_exit') != 0 or
                source_build.get('mission') != 'S002' or source_build.get('seed') != 104731 or
                source_build.get('repository_root') != str(ROOT) or
                not isinstance(source_pins, dict) or
                source_build_record.get('status') != 'Passed' or
                source_build_record.get('mission') != 'S002' or
                source_build_record.get('seed') != 104731 or
                source_build_record.get('pin_count') != len(source_pins) or
                source_build_record.get('pin_mismatches') != 0):
            raise ValueError('S050 r9 binding receipt does not name a complete passed source build')
        for source_path, expected_sha in source_pins.items():
            path = Path(source_path)
            if (not path.is_file() or path.is_symlink() or sha(path) != expected_sha):
                raise ValueError('S050 r9 binding source build pin changed: ' + source_path)
        input_descriptors = receipt.get('source_inputs')
        required_inputs = {'layout', 'graph', 'matrix', 'template', 'probe_source',
                           'probe_executable', 'compile_response'}
        if not isinstance(input_descriptors, dict) or set(input_descriptors) != required_inputs:
            raise ValueError('S050 r9 binding source input descriptor set changed')
        for name, desc in input_descriptors.items():
            if not isinstance(desc, dict) or set(desc) != {'path', 'sha256'}:
                raise ValueError('S050 r9 binding source descriptor changed: ' + name)
            path = Path(desc['path']).resolve(strict=True)
            if (not path.is_relative_to(ROOT) or path.is_symlink() or not path.is_file() or
                    sha(path) != desc['sha256']):
                raise ValueError('S050 r9 binding source input changed: ' + name)
        for name, request_key in (('layout', 'layout'), ('graph', 'graph'), ('matrix', 'setup_matrix')):
            if input_descriptors[name] != request.get(request_key):
                raise ValueError('S050 r9 binding input no longer matches author request: ' + name)
        _, binding_result = read_descriptor(binding_receipt_result, 'S050 r9 original compiler result')
        roundtrip_receipt = binding_result.get('binding_roundtrip', {})
        expected_controls = ['source_content_hash', 'view_fingerprint', 'missing_role_framing']
        if (receipt.get('compiler_result') != {
                'catalog_id': target['catalog_id'], 'seed': target['seed'],
                'layout_id': target.get('layout_id', binding_result.get('resolved_setup', {}).get('layout_id')),
                'setup_hash': target['setup_hash'],
                'layout_semantic_fingerprint': target['layout_fingerprint'],
                'logical_binding_fingerprint': target['mission_logical_binding_fingerprint'],
                'negative_controls': expected_controls} or
                binding_result.get('kind') != 'S050IncompleteOriginalCompilerProbe' or
                binding_result.get('catalog_id') != target['catalog_id'] or
                binding_result.get('compiler_accepted') is not True or
                binding_result.get('world_initialized') is not False or
                binding_result.get('mission_ready') is not False or
                binding_result.get('engineering_view', {}).get('layout_semantic_fingerprint') != target['layout_fingerprint'] or
                binding_result.get('resolved_setup', {}).get('layout_id') !=
                    r9_compiler_result.get('layout_id') or
                (target.get('layout_id') is not None and
                 binding_result.get('resolved_setup', {}).get('layout_id') != target['layout_id']) or
                roundtrip_receipt.get('accepted') is not True or
                roundtrip_receipt.get('setup_hash') != target['setup_hash'] or
                roundtrip_receipt.get('logical_binding_fingerprint') != target['mission_logical_binding_fingerprint'] or
                roundtrip_receipt.get('negative_controls') != expected_controls or
                roundtrip_receipt.get('native_visual_accepted') is not False or
                roundtrip_receipt.get('source_fit_verified') is not False or
                receipt.get('world_initialized') is not False or
                receipt.get('mission_ready') is not False or
                receipt.get('native_visual_accepted') is not False or
                receipt.get('runtime_admission') is not False or
                receipt.get('source_fit_verified') is not False):
            raise ValueError('S050 r9 binding owner result lacks exact target, roundtrip, controls or nonclaims')
    else:
        raise ValueError('Binding owner/compiler receipt status or schema is unsupported')
    if not current_projection:
        _, binding_result = read_descriptor(binding_receipt_result, 'binding compiler result')
    roundtrip = binding_result.get('binding_roundtrip', {})
    if (binding_result.get('compiler_accepted') is not True or binding_result.get('world_initialized') is not False or
            binding_result.get('mission_ready') is not False or roundtrip.get('accepted') is not True or
            roundtrip.get('setup_hash') != target['setup_hash'] or
            roundtrip.get('logical_binding_fingerprint') != target['mission_logical_binding_fingerprint'] or
            roundtrip.get('native_visual_accepted') is not False or roundtrip.get('source_fit_verified') is not False):
        raise ValueError('Binding roundtrip result does not prove selected compiler-owned profile')

    context_descriptor = {'path': str(context_path), 'sha256': context_desc['sha256']}
    profile = {'schema_version': 1, 'kind': 'OriginalCompilerDerivedEngineeringCameraContext',
               'metadata_compiler_context': context_descriptor, 'target_context': target,
               'profile_fields': _profile_fields(view), 'native_function_profile': function_profile_desc,
               'native_camera_context_qualified': False, 'source_profile_derived': True}
    output = Path(output).resolve()
    if not output.is_relative_to(ROOT / 'T Logs') or output.exists():
        raise ValueError('Fresh repository T Logs output required')
    output.mkdir(parents=True)
    path = output / 'engineering-camera-context.json'
    path.write_text(json.dumps(profile, indent=2, ensure_ascii=False) + '\n')
    profile_desc = descriptor(path)
    inputs = {str(p): h for p, h in ((context_path, context_desc['sha256']), (request_path, author_request_desc['sha256']),
             (result_path, author_result_desc['sha256']), (author_receipt_path, author_receipt_desc['sha256']), (binding_path, binding_desc['sha256']),
             (receipt_path, binding_receipt_desc['sha256']), (Path(binding_receipt_result['path']), binding_receipt_result['sha256']),
             (Path(adapter_desc['path']), adapter_desc['sha256']), (Path(function_profile_desc['path']), function_profile_desc['sha256']))}
    if current_projection:
        for pinned_path, pinned_hash in ((source_manifest_path, source_manifest_desc['sha256']),
                (host_build_path, host_build_desc['sha256']), (codec_path, codec_desc['sha256']),
                (Path(portable_binding_desc['path']), portable_binding_desc['sha256'])):
            inputs[str(pinned_path)] = pinned_hash
    for name, expected in context.get('consumed_input_pins', {}).items():
        inputs[name] = expected
    receipt_out = {'kind': 'CompilerSelectedSourceOnlyCameraProfileProjection', 'status': 'ProjectedSourceOnly',
                   'target_context': target, 'profile': profile_desc, 'inputs': inputs,
                   'camera_adapter': adapter_desc, 'historical_function_profile': function_profile_desc,
                   'adapter_function_profile_sha256': adapter_tuple[0],
                   'profile_fields_source': 'Exact selected View from compiler-accepted logical binding; decoded again by the runtime source reader',
                   'world_allocated': False, 'native_camera_context_qualified': False,
                   'native_visual_readiness': False, 'player_camera_readiness': False,
                   'source_fit_certified': False, 'mission_ready': False,
                   'scope': 'Compiler-selected source camera profile projection only; historical adapter/oracle is a separately pinned function reference, not S050 native camera evidence.'}
    receipt_out_path = output / 'receipt.json'
    receipt_out_path.write_text(json.dumps(receipt_out, indent=2, ensure_ascii=False) + '\n')
    for name, expected in inputs.items():
        if not Path(name).is_file() or sha(name) != expected:
            raise ValueError('Projection input changed during write: ' + name)
    return receipt_out


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ('context', 'author-request', 'author-result', 'author-receipt', 'binding', 'binding-receipt',
                 'camera-adapter', 'function-profile', 'output'):
        parser.add_argument('--' + name, required=True)
    a = parser.parse_args()
    out = project({'path': a.context, 'sha256': sha(a.context)},
                  {'path': a.author_request, 'sha256': sha(a.author_request)},
                  {'path': a.author_result, 'sha256': sha(a.author_result)},
                  {'path': a.author_receipt, 'sha256': sha(a.author_receipt)},
                  {'path': a.binding, 'sha256': sha(a.binding)},
                  {'path': a.binding_receipt, 'sha256': sha(a.binding_receipt)},
                  {'path': a.camera_adapter, 'sha256': sha(a.camera_adapter)},
                  {'path': a.function_profile, 'sha256': sha(a.function_profile)}, a.output)
    print(json.dumps({'status': out['status'], 'profile': out['profile'],
                      'native_camera_context_qualified': False, 'mission_ready': False}))
