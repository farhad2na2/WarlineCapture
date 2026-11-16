import hashlib
import importlib.util
import json
from pathlib import Path
import shutil
import tempfile
import unittest
from unittest.mock import patch


SCRIPT = Path(__file__).with_name('project_compiler_selected_camera_profile.py')
spec = importlib.util.spec_from_file_location('compiler_selected_camera_profile', SCRIPT)
projector = importlib.util.module_from_spec(spec)
spec.loader.exec_module(projector)


def write_json(path, value):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, separators=(',', ':')) + '\n')
    return {'path': str(path.resolve()), 'sha256': hashlib.sha256(path.read_bytes()).hexdigest()}


class CompilerSelectedCameraProfileTests(unittest.TestCase):
    def setUp(self):
        self.temp = Path(tempfile.mkdtemp(prefix='compiler-selected-camera-')).resolve()
        self.logs = self.temp / 'T Logs'
        self.logs.mkdir()
        self.profile = {'kind': 'SkirmishEngineeringCameraProfileReference', 'catalog_id': 'S026'}
        self.function_desc = write_json(self.temp / 'function-profile.json', self.profile)
        self.function_sha = self.function_desc['sha256']
        self.adapter = {
            'schema_version': 2, 'kind': 'SourceNumericCameraEngineAdapterContract',
            'adapter_version': 'EngineeringWorldNearPointTRS_v2',
            'source_template_sha256': '30f11fab477a2b014fb6bf0c8c42cc45305d9811947fbb4dfa1b3c65fb0d4c4b',
            'engineering_profile_sha256': self.function_sha,
            'native_values_sha256': 'c' * 64, 'native_motion_sha256': 'd' * 64,
            'native_values_schema': 2, 'native_motion_input_schema': 2,
        }
        self.adapter_desc = write_json(self.temp / 'adapter.json', self.adapter)
        self.adapter_sha = self.adapter_desc['sha256']
        self.target = {'catalog_id': 'S050', 'definition_id': 'skirmish.s050', 'content_version': 2,
                       'seed': 104779, 'size': 'Standard', 'difficulty': 'Regular', 'setup_hash': 123,
                       'operation_map_id': 'opmap.example', 'source_content_hash': '1' * 64,
                       'layout_fingerprint': '2' * 64, 'layout_binding_fingerprint': '3' * 64,
                       'mission_logical_binding_fingerprint': '4' * 64}
        self.pin_file = self.temp / 'compiler-input.asset'
        self.pin_file.write_text('pinned')
        self.context = {'schema_version': 1, 'kind': 'OriginalCompilerDerivedNativeFactoryContext',
                        'authority': 'ActualSourceCompilerContext_ImmutableNativeFactoryFacts_NotNativeS050',
                        'target_context': self.target, 'native_S050_capture': False,
                        'world_allocated': False, 'identities_allocated': 0,
                        'source_fit_certified': False, 'mission_ready': False,
                        'runtime_loader_admitted': False,
                        'consumed_input_pins': {str(self.pin_file): hashlib.sha256(self.pin_file.read_bytes()).hexdigest()}}
        self.context_desc = write_json(self.temp / 'context.json', self.context)
        self.binding_path = self.temp / 'binding.json'
        self.view = {key: None for key in projector.STRING_FIELDS}
        self.view.update({key: 1.0 for key in projector.FLOAT_FIELDS})
        self.view.update({key: 1 for key in projector.INTEGER_FIELDS})
        self.view.update({key: False for key in projector.BOOLEAN_FIELDS})
        self.view.update(CatalogId='S050', DefinitionId='skirmish.s050',
                         CampaignSourceContentHash=self.target['source_content_hash'])
        self.binding = {'CatalogId': 'S050', 'DefinitionId': 'skirmish.s050', 'ContentVersion': 1, 'DefinitionVersion': 2,
                        'SourceOperationMapId': self.target['operation_map_id'],
                        'SourceContentHash': self.target['source_content_hash'],
                        'ReviewedLayoutSemanticFingerprint': self.target['layout_fingerprint'],
                        'Variants': [{'Size': 1, 'Difficulty': 2, 'View': self.view}]}
        self.binding_desc = write_json(self.binding_path, self.binding)
        self.descriptors = {}
        for key in ('graph', 'layout', 'setup_matrix', 'runtime_config'):
            path = self.temp / f'{key}.input'
            path.write_text(key)
            self.descriptors[key] = {'path': str(path.resolve()), 'sha256': hashlib.sha256(path.read_bytes()).hexdigest()}
        self.request = {'catalog_id': 'S050', 'seed': 104779, **self.descriptors, 'binding': self.binding_desc}
        self.request_desc = write_json(self.temp / 'author-request.json', self.request)
        self.result = {'kind': 'SkirmishMissionAuthoringPreflightResult', 'status': 'ConfigurationCompiled',
                       'catalog_id': 'S050', 'seed': 104779, 'operation_map_id': self.target['operation_map_id'],
                       'source_content_hash': self.target['source_content_hash'],
                       'layout_fingerprint': self.target['layout_fingerprint'],
                       'resolved_setup': {'setup_hash': self.target['setup_hash']},
                       'partial_authoring_capture_for_missing_binding': False,
                       'world_allocated': False, 'source_fit_verified': False, 'mission_ready': False,
                       'input_pins': {str(Path(d['path'])): {'sha256': d['sha256']} for d in self.descriptors.values()}}
        self.result['input_pins'][str(self.request_path())] = {'sha256': self.request_desc['sha256']}
        self.result['input_pins'][str(self.binding_path)] = {'sha256': self.binding_desc['sha256']}
        self.result_desc = write_json(self.temp / 'author-result.json', self.result)
        self.author_receipt = {'kind': 'SkirmishMissionAuthoringInvocation', 'status': 'ConfigurationCompiled',
                               'process_exit': 0, 'timed_out': False, 'world_allocated': False,
                               'mission_ready': False, 'result_pin': self.result_desc}
        self.author_receipt_desc = write_json(self.temp / 'author-receipt.json', self.author_receipt)
        self.binding_result = {'compiler_accepted': True, 'world_initialized': False, 'mission_ready': False,
                               'binding_roundtrip': {'accepted': True, 'setup_hash': self.target['setup_hash'],
                                   'logical_binding_fingerprint': self.target['mission_logical_binding_fingerprint'],
                                   'native_visual_accepted': False, 'source_fit_verified': False}}
        self.binding_result_desc = write_json(self.temp / 'binding-result.json', self.binding_result)
        self.binding_receipt = {'kind': 'OriginalCompilerLogicalBinding', 'status': 'OriginalCompilerRoundtripAccepted',
                                'binding': self.binding_desc, 'result': self.binding_result_desc,
                                'setup_hash': self.target['setup_hash'],
                                'logical_binding_fingerprint': self.target['mission_logical_binding_fingerprint'],
                                'physical_source_fit_verified': False, 'native_visual_accepted': False,
                                'mission_ready': False}
        self.binding_receipt_desc = write_json(self.temp / 'binding-receipt.json', self.binding_receipt)

    def request_path(self):
        return self.temp / 'author-request.json'

    def tearDown(self):
        shutil.rmtree(self.temp)

    def r9_binding_receipt(self):
        controls = ['source_content_hash', 'view_fingerprint', 'missing_role_framing']
        result = {'kind': 'S050IncompleteOriginalCompilerProbe', 'catalog_id': 'S050',
                  'compiler_accepted': True, 'world_initialized': False, 'mission_ready': False,
                  'engineering_view': {'layout_semantic_fingerprint': self.target['layout_fingerprint']},
                  'resolved_setup': {'layout_id': self.target.get('layout_id', 'layout.s050.test')},
                  'binding_roundtrip': {'accepted': True, 'setup_hash': self.target['setup_hash'],
                      'logical_binding_fingerprint': self.target['mission_logical_binding_fingerprint'],
                      'negative_controls': controls, 'native_visual_accepted': False,
                      'source_fit_verified': False}}
        result_desc = write_json(self.temp / 'r9-binding-result.json', result)
        source_pin = {str(self.pin_file): hashlib.sha256(self.pin_file.read_bytes()).hexdigest()}
        build = {'kind': 'SkirmishSourceSimulationBuild', 'status': 'Passed', 'compile_exit': 0,
                 'repository_root': str(self.temp), 'mission': 'S002', 'seed': 104731,
                 'source_and_reference_pins': source_pin}
        build_desc = write_json(self.temp / 'r9-source-build.json', build)
        source_inputs = {
            'layout': self.descriptors['layout'], 'graph': self.descriptors['graph'],
            'matrix': self.descriptors['setup_matrix'],
            **{name: {'path': str(self.pin_file), 'sha256': source_pin[str(self.pin_file)]}
               for name in ('template', 'probe_source', 'probe_executable', 'compile_response')}}
        receipt = {'schema_version': 1, 'kind': 'S050R9OriginalCompilerBindingRoundtrip',
                   'status': 'PassedSourceOnly', 'binding': self.binding_desc,
                   'result': result_desc,
                   'source_build': {'path': build_desc['path'], 'sha256': build_desc['sha256'],
                       'status': 'Passed', 'mission': 'S002', 'seed': 104731,
                       'pin_count': len(source_pin), 'pin_mismatches': 0},
                   'source_inputs': source_inputs,
                   'compiler_result': {'catalog_id': 'S050', 'seed': 104779,
                       'layout_id': result['resolved_setup']['layout_id'],
                       'setup_hash': self.target['setup_hash'],
                       'layout_semantic_fingerprint': self.target['layout_fingerprint'],
                       'logical_binding_fingerprint': self.target['mission_logical_binding_fingerprint'],
                       'negative_controls': controls},
                   'world_initialized': False, 'mission_ready': False,
                   'source_fit_verified': False,
                   'native_visual_accepted': False, 'runtime_admission': False,
                   'resource_preset_applied': False}
        return write_json(self.temp / 'r9-binding-receipt.json', receipt)

    def run_project(self, name='projection'):
        output = self.logs / name
        with patch.object(projector, 'ROOT', self.temp), patch.object(projector, 'ADAPTERS', {
                self.adapter_sha: (self.function_sha, self.adapter['native_values_sha256'], self.adapter['native_motion_sha256'])}):
            return projector.project(self.context_desc, self.request_desc, self.result_desc,
                                     self.author_receipt_desc, self.binding_desc,
                                     self.binding_receipt_desc, self.adapter_desc,
                                     self.function_desc, output)

    def test_projects_selected_view_as_nonready_source_profile(self):
        receipt = self.run_project()
        profile = json.loads(Path(receipt['profile']['path']).read_text())
        self.assertEqual(profile['target_context'], self.target)
        self.assertEqual(profile['profile_fields']['Pitch'], projector._float_bits(1.0))
        self.assertIs(profile['native_camera_context_qualified'], False)
        self.assertIs(receipt['mission_ready'], False)
        self.assertEqual(receipt['adapter_function_profile_sha256'], self.function_sha)

    def test_projects_checked_s050_binding_owner_shape(self):
        owner_receipt = {
            'kind': 'S050GridAlignedOriginalCompilerBindingRoundtrip',
            'status': 'OriginalCompilerReturned',
            'binding': self.binding_desc,
            'compiler_probe_result': self.binding_result_desc,
            'setup_hash': self.target['setup_hash'],
            'logical_binding_fingerprint': self.target['mission_logical_binding_fingerprint'],
            'compiler_accepted': True,
            'world_initialized': False,
            'mission_ready': False,
            'native_visual_accepted': False,
            'source_fit_verified': False,
            'runtime_admission': False,
            'binding_roundtrip': dict(self.binding_result['binding_roundtrip']),
        }
        owner_receipt_desc = write_json(self.temp / 's050-owner-roundtrip-receipt.json', owner_receipt)
        with patch.object(projector, 'ROOT', self.temp), patch.object(projector, 'ADAPTERS', {
                self.adapter_sha: (self.function_sha, self.adapter['native_values_sha256'], self.adapter['native_motion_sha256'])}):
            receipt = projector.project(self.context_desc, self.request_desc, self.result_desc,
                                        self.author_receipt_desc, self.binding_desc,
                                        owner_receipt_desc, self.adapter_desc,
                                        self.function_desc, self.logs / 'owner-shape')
        self.assertIs(receipt['mission_ready'], False)

    def test_projects_r9_original_compiler_binding_receipt(self):
        owner_receipt_desc = self.r9_binding_receipt()
        with patch.object(projector, 'ROOT', self.temp), patch.object(projector, 'ADAPTERS', {
                self.adapter_sha: (self.function_sha, self.adapter['native_values_sha256'], self.adapter['native_motion_sha256'])}):
            receipt = projector.project(self.context_desc, self.request_desc, self.result_desc,
                                        self.author_receipt_desc, self.binding_desc,
                                        owner_receipt_desc, self.adapter_desc,
                                        self.function_desc, self.logs / 'r9-owner-shape')
        self.assertIs(receipt['mission_ready'], False)

    def test_rejects_r9_binding_owner_control_or_source_pin_drift(self):
        owner_receipt_desc = self.r9_binding_receipt()
        owner_path = Path(owner_receipt_desc['path'])
        owner_receipt = json.loads(owner_path.read_bytes())
        result_path = Path(owner_receipt['result']['path'])
        result = json.loads(result_path.read_bytes())
        result['binding_roundtrip']['negative_controls'] = ['source_content_hash']
        bad_result_desc = write_json(result_path, result)
        owner_receipt['result'] = bad_result_desc
        owner_receipt['compiler_result']['negative_controls'] = ['source_content_hash']
        bad_owner = write_json(self.temp / 'bad-r9-controls.json', owner_receipt)
        with patch.object(projector, 'ROOT', self.temp), patch.object(projector, 'ADAPTERS', {
                self.adapter_sha: (self.function_sha, self.adapter['native_values_sha256'], self.adapter['native_motion_sha256'])}):
            with self.assertRaisesRegex(ValueError, 'exact target, roundtrip, controls or nonclaims'):
                projector.project(self.context_desc, self.request_desc, self.result_desc,
                                  self.author_receipt_desc, self.binding_desc, bad_owner,
                                  self.adapter_desc, self.function_desc, self.logs / 'bad-r9-controls')

        owner_receipt_desc = self.r9_binding_receipt()
        owner_receipt = json.loads(Path(owner_receipt_desc['path']).read_bytes())
        build_path = Path(owner_receipt['source_build']['path'])
        build = json.loads(build_path.read_bytes())
        build['source_and_reference_pins'][str(self.pin_file)] = '0' * 64
        bad_build_desc = write_json(build_path, build)
        owner_receipt['source_build']['sha256'] = bad_build_desc['sha256']
        owner_receipt['source_build']['pin_count'] = len(build['source_and_reference_pins'])
        bad_owner = write_json(self.temp / 'bad-r9-source-build.json', owner_receipt)
        with patch.object(projector, 'ROOT', self.temp), patch.object(projector, 'ADAPTERS', {
                self.adapter_sha: (self.function_sha, self.adapter['native_values_sha256'], self.adapter['native_motion_sha256'])}):
            with self.assertRaisesRegex(ValueError, 'source build pin changed'):
                projector.project(self.context_desc, self.request_desc, self.result_desc,
                                  self.author_receipt_desc, self.binding_desc, bad_owner,
                                  self.adapter_desc, self.function_desc, self.logs / 'bad-r9-source-build')

    def test_rejects_s050_owner_roundtrip_target_or_nonclaim_drift(self):
        owner_receipt = {
            'kind': 'S050GridAlignedOriginalCompilerBindingRoundtrip',
            'status': 'OriginalCompilerReturned',
            'binding': self.binding_desc,
            'compiler_probe_result': self.binding_result_desc,
            'setup_hash': self.target['setup_hash'],
            'logical_binding_fingerprint': self.target['mission_logical_binding_fingerprint'],
            'compiler_accepted': True,
            'world_initialized': False,
            'mission_ready': False,
            'native_visual_accepted': False,
            'source_fit_verified': False,
            'runtime_admission': False,
            'binding_roundtrip': dict(self.binding_result['binding_roundtrip']),
        }
        owner_receipt['binding_roundtrip']['setup_hash'] += 1
        bad = write_json(self.temp / 'bad-s050-owner-roundtrip-receipt.json', owner_receipt)
        with patch.object(projector, 'ROOT', self.temp), patch.object(projector, 'ADAPTERS', {
                self.adapter_sha: (self.function_sha, self.adapter['native_values_sha256'], self.adapter['native_motion_sha256'])}):
            with self.assertRaisesRegex(ValueError, 'lacks exact roundtrip'):
                projector.project(self.context_desc, self.request_desc, self.result_desc,
                                  self.author_receipt_desc, self.binding_desc, bad,
                                  self.adapter_desc, self.function_desc, self.logs / 'bad-s050-owner-roundtrip')

    def test_projects_current_source_only_owner_receipt_schema(self):
        owner_receipt = {
            'schema_version': 1,
            'kind': 'S050SourceOnlyOriginalCompilerBindingRoundtrip',
            'status': 'OriginalCompilerReturned',
            'mission': self.target['catalog_id'],
            'seed': self.target['seed'],
            'binding': self.binding_desc,
            'result': self.binding_result_desc,
            'binding_setup_hash': self.target['setup_hash'],
            'logical_binding_fingerprint': self.target['mission_logical_binding_fingerprint'],
            'compiler_accepted': True,
            'world_initialized': False,
            'mission_ready': False,
            'native_visual_accepted': False,
            'source_fit_verified': False,
            'runtime_admission': False,
            'resource_preset_applied': False,
            'binding_roundtrip': dict(self.binding_result['binding_roundtrip']),
        }
        owner_receipt_path = write_json(self.temp / 'current-source-owner-receipt.json', owner_receipt)
        with patch.object(projector, 'ROOT', self.temp), patch.object(projector, 'ADAPTERS', {
                self.adapter_sha: (self.function_sha, self.adapter['native_values_sha256'], self.adapter['native_motion_sha256'])}):
            receipt = projector.project(self.context_desc, self.request_desc, self.result_desc,
                                        self.author_receipt_desc, self.binding_desc, owner_receipt_path,
                                        self.adapter_desc, self.function_desc, self.logs / 'current-owner-shape')
        self.assertIs(receipt['mission_ready'], False)

    def test_projects_current_original_compiler_logical_binding_projection(self):
        self.view.update(ContentVersion=1, DefinitionVersion=2, name=None, hideFlags=0)
        self.binding_desc = write_json(self.binding_path, self.binding)
        self.request['binding'] = self.binding_desc
        self.request_desc = write_json(self.request_path(), self.request)
        self.result['input_pins'][str(self.binding_path)] = {'sha256': self.binding_desc['sha256']}
        self.result['input_pins'][str(self.request_path())] = {'sha256': self.request_desc['sha256']}
        self.result_desc = write_json(self.temp / 'author-result.json', self.result)
        self.author_receipt['result_pin'] = self.result_desc
        self.author_receipt_desc = write_json(self.temp / 'author-receipt.json', self.author_receipt)

        roundtrip = {
            'kind': 'CurrentAuthoringLogicalBindingRoundtrip', 'catalog_id': self.target['catalog_id'],
            'definition_id': self.target['definition_id'], 'content_version': self.target['content_version'],
            'seed': self.target['seed'], 'operation_map_id': self.target['operation_map_id'],
            'source_content_hash': self.target['source_content_hash'],
            'layout_fingerprint': self.target['layout_fingerprint'],
            'setup_hash_with_binding': self.target['setup_hash'],
            'logical_binding_fingerprint': self.target['mission_logical_binding_fingerprint'],
            'portable_roundtrip_setup_hash': self.target['setup_hash'],
            'portable_roundtrip_fingerprint': self.target['mission_logical_binding_fingerprint'],
            'negative_controls': ['wrong_source_content_hash_rejected', 'missing_role_framing_rejected'],
            'world_allocated': False, 'placement_performed': False, 'native_capture': False,
            'runtime_admission': False, 'source_fit_certified': False, 'mission_ready': False,
        }
        roundtrip_desc = write_json(self.temp / 'binding-codec-roundtrip.json', roundtrip)
        manifest_path = self.temp / 'source-manifest.json'
        manifest_path.write_text(json.dumps({'schema_version': 1, 'sources': []}) + '\n')
        manifest_desc = {'path': str(manifest_path), 'sha256': hashlib.sha256(manifest_path.read_bytes()).hexdigest(),
                         'bytes': manifest_path.stat().st_size}
        host_pins = {str(self.pin_file): hashlib.sha256(self.pin_file.read_bytes()).hexdigest()}
        host_build_desc = write_json(self.temp / 'source-build.json', {
            'kind': 'SkirmishSourceSimulationBuild', 'status': 'Passed', 'compile_exit': 0,
            'mission': 'S002', 'seed': 104731, 'repository_root': str(self.temp),
            'native_or_player_launched': False, 'source_and_reference_pins': host_pins})
        codec_path = self.temp / 'codec.cs'
        codec_path.write_text('codec source')
        codec_desc = {'path': str(codec_path), 'sha256': hashlib.sha256(codec_path.read_bytes()).hexdigest()}
        portable_desc = write_json(self.temp / 'portable-binding.json', {
            'kind': 'DeclaredOriginalLogicalBindingFieldsWithInlineViews',
            'fields': {'CatalogId': self.target['catalog_id'], 'DefinitionId': self.target['definition_id'],
                       'DefinitionVersion': self.target['content_version'],
                       'SourceContentHash': self.target['source_content_hash']}})
        owner_receipt = {
            'kind': 'CurrentOriginalCompilerLogicalBindingProjection', 'status': 'ProjectedSourceOnly',
            'binding_input': self.binding_desc, 'baseline_request': self.request_desc,
            'baseline_result': self.result_desc,
            'setup_hash_unbound': self.target['setup_hash'] - 1, 'setup_hash_bound': self.target['setup_hash'],
            'logical_binding_fingerprint': self.target['mission_logical_binding_fingerprint'],
            'negative_controls': ['wrong_source_content_hash_rejected', 'missing_role_framing_rejected'],
            'world_allocated': False, 'native_capture': False, 'runtime_admission': False,
            'source_fit_certified': False, 'mission_ready': False,
            'authoring_input_pin_count': len(self.result['input_pins']),
            'factory_context_consumed_pin_count': len(self.context['consumed_input_pins']),
            'host_source_pin_count': len(host_pins), 'source_manifest': manifest_desc,
            'host_build': host_build_desc, 'roundtrip': roundtrip_desc,
            'codec_source': codec_desc, 'portable_binding': portable_desc,
        }
        owner_receipt_desc = write_json(self.temp / 'current-logical-projection-receipt.json', owner_receipt)
        with patch.object(projector, 'ROOT', self.temp), patch.object(projector, 'ADAPTERS', {
                self.adapter_sha: (self.function_sha, self.adapter['native_values_sha256'], self.adapter['native_motion_sha256'])}):
            receipt = projector.project(self.context_desc, self.request_desc, self.result_desc,
                                        self.author_receipt_desc, self.binding_desc, owner_receipt_desc,
                                        self.adapter_desc, self.function_desc, self.logs / 'current-logical-projection')
        self.assertIs(receipt['mission_ready'], False)
        self.assertIs(receipt['native_camera_context_qualified'], False)

    def test_rejects_current_source_only_owner_receipt_drift(self):
        owner_receipt = {
            'schema_version': 1,
            'kind': 'S050SourceOnlyOriginalCompilerBindingRoundtrip',
            'status': 'OriginalCompilerReturned',
            'mission': self.target['catalog_id'],
            'seed': self.target['seed'],
            'binding': self.binding_desc,
            'result': self.binding_result_desc,
            'binding_setup_hash': self.target['setup_hash'] + 1,
            'logical_binding_fingerprint': self.target['mission_logical_binding_fingerprint'],
            'compiler_accepted': True,
            'world_initialized': False,
            'mission_ready': False,
            'native_visual_accepted': False,
            'source_fit_verified': False,
            'runtime_admission': False,
            'resource_preset_applied': False,
            'binding_roundtrip': dict(self.binding_result['binding_roundtrip']),
        }
        bad = write_json(self.temp / 'bad-current-source-owner-receipt.json', owner_receipt)
        with patch.object(projector, 'ROOT', self.temp), patch.object(projector, 'ADAPTERS', {
                self.adapter_sha: (self.function_sha, self.adapter['native_values_sha256'], self.adapter['native_motion_sha256'])}):
            with self.assertRaisesRegex(ValueError, 'does not qualify this exact compiler target'):
                projector.project(self.context_desc, self.request_desc, self.result_desc,
                                  self.author_receipt_desc, self.binding_desc, bad,
                                  self.adapter_desc, self.function_desc, self.logs / 'bad-current-owner-shape')

    def test_rejects_current_source_only_owner_nested_fingerprint_drift(self):
        owner_receipt = {
            'schema_version': 1,
            'kind': 'S050SourceOnlyOriginalCompilerBindingRoundtrip',
            'status': 'OriginalCompilerReturned',
            'mission': self.target['catalog_id'],
            'seed': self.target['seed'],
            'binding': self.binding_desc,
            'result': self.binding_result_desc,
            'binding_setup_hash': self.target['setup_hash'],
            'logical_binding_fingerprint': self.target['mission_logical_binding_fingerprint'],
            'compiler_accepted': True,
            'world_initialized': False,
            'mission_ready': False,
            'native_visual_accepted': False,
            'source_fit_verified': False,
            'runtime_admission': False,
            'resource_preset_applied': False,
            'binding_roundtrip': dict(self.binding_result['binding_roundtrip']),
        }
        owner_receipt['binding_roundtrip']['logical_binding_fingerprint'] = 'drift'
        bad = write_json(self.temp / 'bad-current-nested-fingerprint.json', owner_receipt)
        with patch.object(projector, 'ROOT', self.temp), patch.object(projector, 'ADAPTERS', {
                self.adapter_sha: (self.function_sha, self.adapter['native_values_sha256'], self.adapter['native_motion_sha256'])}):
            with self.assertRaisesRegex(ValueError, 'lacks exact roundtrip'):
                projector.project(self.context_desc, self.request_desc, self.result_desc,
                                  self.author_receipt_desc, self.binding_desc, bad,
                                  self.adapter_desc, self.function_desc, self.logs / 'bad-current-nested-fingerprint')

    def test_rejects_adapter_and_function_profile_mismatch(self):
        unsupported_adapter = dict(self.adapter)
        unsupported_adapter['adapter_version'] = 'unknown'
        bad_adapter = write_json(self.temp / 'unsupported-adapter.json', unsupported_adapter)
        with patch.object(projector, 'ROOT', self.temp), patch.object(projector, 'ADAPTERS', {
                self.adapter_sha: (self.function_sha, self.adapter['native_values_sha256'], self.adapter['native_motion_sha256'])}):
            with self.assertRaisesRegex(ValueError, 'allowlist'):
                projector.project(self.context_desc, self.request_desc, self.result_desc,
                                  self.author_receipt_desc, self.binding_desc, self.binding_receipt_desc,
                                  bad_adapter, self.function_desc, self.logs / 'bad-adapter')
            bad_profile_path = self.temp / 'bad-function-profile.json'
            wrong = write_json(bad_profile_path, dict(self.profile, catalog_id='S050'))
            with self.assertRaisesRegex(ValueError, 'adapter-bound'):
                projector.project(self.context_desc, self.request_desc, self.result_desc,
                                  self.author_receipt_desc, self.binding_desc, self.binding_receipt_desc,
                                  self.adapter_desc, wrong, self.logs / 'bad-profile')

    def test_rejects_selected_view_drift_against_author_pin(self):
        changed = json.loads(self.binding_path.read_text())
        changed['Variants'][0]['View']['Pitch'] = 2.0
        changed_desc = write_json(self.binding_path, changed)
        with patch.object(projector, 'ROOT', self.temp), patch.object(projector, 'ADAPTERS', {
                self.adapter_sha: (self.function_sha, self.adapter['native_values_sha256'], self.adapter['native_motion_sha256'])}):
            with self.assertRaisesRegex(ValueError, 'Author request does not select this exact binding'):
                projector.project(self.context_desc, self.request_desc, self.result_desc,
                                  self.author_receipt_desc, changed_desc, self.binding_receipt_desc,
                                  self.adapter_desc, self.function_desc, self.logs / 'bad-view')


if __name__ == '__main__':
    unittest.main()
