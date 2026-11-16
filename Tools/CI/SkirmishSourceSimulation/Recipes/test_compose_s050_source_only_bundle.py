import hashlib
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

SCRIPT = Path(__file__).with_name('compose_s050_source_only_bundle.py')
spec = importlib.util.spec_from_file_location('compose_s050_source_only_bundle', SCRIPT)
composer = importlib.util.module_from_spec(spec)
spec.loader.exec_module(composer)


def write_json(path, value):
    path = Path(path)
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, sort_keys=True) + '\n')
    return {'path': str(path.resolve()), 'sha256': hashlib.sha256(path.read_bytes()).hexdigest()}


class S050SourceOnlyBundleComposerTests(unittest.TestCase):
    def test_current_binding_projection_requires_exact_context_owner_result_and_roundtrip(self):
        with tempfile.TemporaryDirectory() as td:
            root = Path(td).resolve()
            context_path = root / 'context.json'
            target = {'catalog_id':'S050','seed':104779,'setup_hash':3467652606,
                      'operation_map_id':'opmap.citywide','source_content_hash':'c'*64,
                      'source_fingerprint':'d'*64,'layout_fingerprint':'e'*64,
                      'layout_binding_fingerprint':'f'*64,'mission_logical_binding_fingerprint':'a'*64}
            context_desc = write_json(context_path, {'target_context':target})
            qualification_desc = {'path':str(root/'qualification.json'),'sha256':'b'*64}
            author_request = write_json(root/'baseline-request.json', {})
            author_result = write_json(root/'baseline-result.json', {})
            owner_binding = write_json(root/'owner-binding.json', {'source':'owner'})
            replay_request_path = root/'replay-request.json'
            replay_request = write_json(replay_request_path, {'binding':owner_binding})
            replay_result_path = root/'replay-result.json'
            replay_result = write_json(replay_result_path, {
                'kind':'SkirmishMissionAuthoringPreflightResult','status':'ConfigurationCompiled',
                'compiler_accepted':True,'catalog_id':'S050','seed':104779,
                'operation_map_id':target['operation_map_id'],'source_content_hash':target['source_content_hash'],
                'layout_fingerprint':target['layout_fingerprint'],'resolved_setup':{'setup_hash':target['setup_hash']},
                'world_allocated':False,'source_fit_verified':False,'mission_ready':False,'native_acceptance':False,
                'input_pins':{replay_request['path']:{'sha256':replay_request['sha256']},
                              owner_binding['path']:{'sha256':owner_binding['sha256']}}})
            roundtrip = write_json(root/'roundtrip.json', {
                'setup_hash_with_binding':target['setup_hash'],'portable_roundtrip_setup_hash':target['setup_hash'],
                'logical_binding_fingerprint':target['mission_logical_binding_fingerprint'],
                'portable_roundtrip_fingerprint':target['mission_logical_binding_fingerprint'],
                'negative_controls':['wrong_source_content_hash_rejected','missing_role_framing_rejected']})
            binding = write_json(root/'logical-binding.json', {
                'kind':'OriginalCompilerDeclaredLogicalBinding','metadata_compiler_context':context_desc,
                'target_context':target,'source_owner_binding':owner_binding,
                'mission_ready':False,'native_capture':False,'runtime_admission':False,
                'source_fit_certified':False,'world_allocated':False})
            receipt = write_json(root/'binding-receipt.json', {
                'kind':'CurrentOriginalCompilerLogicalBindingProjection','status':'ProjectedSourceOnly',
                'logical_binding':binding,'compiler_context':context_desc,
                'configuration_qualification':qualification_desc,'binding_input':owner_binding,
                'baseline_request':author_request,'baseline_result':author_result,
                'authoring_request':replay_request,'authoring_result':replay_result,'roundtrip':roundtrip,
                'setup_hash_bound':target['setup_hash'],'logical_binding_fingerprint':target['mission_logical_binding_fingerprint'],
                'negative_controls':['wrong_source_content_hash_rejected','missing_role_framing_rejected'],
                'world_allocated':False,'native_capture':False,'runtime_admission':False,
                'source_fit_certified':False,'mission_ready':False})
            with patch.object(composer, 'ROOT', root):
                self.assertEqual(binding['sha256'], composer.validate_current_binding_projection(
                    binding['path'], receipt['path'], context_desc, qualification_desc,
                    author_request, author_result)[0]['sha256'])
                stale = json.loads(Path(receipt['path']).read_text())
                stale['setup_hash_bound'] += 1
                bad_receipt = write_json(root/'bad-binding-receipt.json', stale)
                with self.assertRaisesRegex(ValueError, 'projection receipt does not prove'):
                    composer.validate_current_binding_projection(binding['path'], bad_receipt['path'],
                        context_desc, qualification_desc, author_request, author_result)

    def test_current_campaign_grid_graph_extension_is_pinned_and_fail_closed(self):
        repo = SCRIPT.resolve().parents[4]
        graph = repo / 'T Logs/SkirmishProduction/s050-source-grid-graph-extension-20261107-r1/source-config-graph.json'
        receipt = graph.with_name('receipt.json')
        package = repo / 'T Logs/SkirmishProduction/s050-source-derived-map-package-20261115-r3/map-package.json'
        preflight = package.with_name('preflight-report.json')
        proof = composer.validate_s050_grid_graph_extension(graph, receipt, package, preflight)
        self.assertEqual(proof['graph']['sha256'], '01b13111784526628496abf556befa0914fa6d587503b94db4d84da5d9467511')
        self.assertEqual(proof['receipt']['path'], str(receipt.resolve()))
        with tempfile.TemporaryDirectory() as td:
            bad_receipt = json.loads(receipt.read_bytes()); bad_receipt['runtime_admission'] = True
            bad_receipt_path = Path(td) / 'receipt.json'; bad_receipt_path.write_text(json.dumps(bad_receipt))
            with self.assertRaisesRegex(ValueError, 'Exact existing S050 selected Campaign grid graph extension receipt'):
                composer.validate_s050_grid_graph_extension(graph, bad_receipt_path, package, preflight)
            bad_package = json.loads(package.read_bytes()); bad_package['static_map']['grid_config']['sha256'] = '0' * 64
            bad_package_path = Path(td) / 'package.json'; bad_package_path.write_text(json.dumps(bad_package))
            with self.assertRaisesRegex(ValueError, 'Current map preflight does not bind base graph'):
                composer.validate_s050_grid_graph_extension(graph, receipt, bad_package_path, preflight)

    def test_current_focused_prefab_projection_joins_exact_initialization_descriptors(self):
        repo = SCRIPT.resolve().parents[4]
        base = repo / 'T Logs/SkirmishProduction'
        focused = base / 's050-focused-prefab-projection-20261115-r3/reference-template-descriptor.json'
        context = base / 's050-source-factory-context-probe-20261107-r1/source-factory-context.json'
        qualification = base / 's050-source-qualification-20261107-r1/qualification.json'
        graph = base / 's050-source-grid-graph-extension-20261107-r1/source-config-graph.json'
        building = base / 's050-audited-factory-projections-20261115-r2/building-definitions.json'
        unit = base / 's050-audited-factory-projections-20261115-r2/unit-production-metadata.json'
        author = base / 's050-baseassault-resource-preset-author-replay-20261115-r4/authoring-result.json'
        proof = composer.validate_focused_prefab_projection(focused, composer.pin(context),
            composer.pin(qualification), composer.pin(graph), composer.pin(building),
            composer.pin(unit), composer.pin(author))
        self.assertEqual(proof['source_only_capture_proof']['path'], str(
            (focused.parent / 'focused-prefab-capture-proof.json').resolve()))
        with self.assertRaisesRegex(ValueError, 'not bound to current initialization descriptors'):
            composer.validate_focused_prefab_projection(focused, composer.pin(context),
                composer.pin(qualification), composer.pin(graph), composer.pin(building),
                composer.pin(base / 's050-audited-factory-projections-20261115-r1/unit-production-metadata.json'),
                composer.pin(author))

    def setUp(self):
        self.temp = Path(tempfile.mkdtemp(prefix='s050-bundle-composer-')).resolve()
        self.target = {'catalog_id': 'S050', 'definition_id': 'skirmish.s050', 'seed': 104779,
                       'size': 'Standard', 'difficulty': 'Regular', 'setup_hash': 472937146,
                       'operation_map_id': 'opmap.ch05.citywide_alert',
                       'source_content_hash': '1' * 64, 'source_fingerprint': '2' * 64,
                       'layout_fingerprint': '3' * 64}
        self.context = {'kind': 'OriginalCompilerDerivedNativeFactoryContext', 'target_context': self.target}
        self.context_desc = write_json(self.temp / 'context.json', self.context)
        self.graph_desc = write_json(self.temp / 'graph.json', {'kind': 'SourceGraph'})
        self.layout_desc = write_json(self.temp / 'layout.json', {'kind': 'Layout'})
        self.matrix = self.temp / 'setup.csv'
        self.matrix.write_text('setup matrix')
        self.matrix_desc = {'path': str(self.matrix), 'sha256': hashlib.sha256(self.matrix.read_bytes()).hexdigest()}
        self.map = self.temp / 'campaign-map.asset'
        self.map.write_text('campaign map')
        self.preset = self.temp / 'preset.asset'
        self.preset.write_text('selected cadence source')
        self.preset_desc = {'path': str(self.preset), 'sha256': hashlib.sha256(self.preset.read_bytes()).hexdigest()}
        self.initial = {'schema_version': 1, 'kind': 'GeneratedMissionInitialization', 'mission_id': 'S050',
                        'selection': {'difficulty_id': 2, 'size_id': 1, 'seed': 104779},
                        'static_map': {'operation_map_id': self.target['operation_map_id']},
                        'source_config_graph': self.graph_desc,
                        'setup_matrix': self.matrix_desc,
                        'typed_mission_configuration': {'layout': self.layout_desc},
                        'selected_operation_map': {'path': str(self.map)},
                        'metadata_compiler_context': self.context_desc}
        self.run = {'schema_version': 1, 'kind': 'SkirmishSourceSimulationDataBundle', 'mission': 'S050'}
        self.graph_pins = {self.preset_desc['path']: self.preset_desc['sha256'],
                           self.graph_desc['path']: self.graph_desc['sha256']}

    def owner_pair(self, selected=True, pin_selected=True):
        request = {'schema_version': 1, 'kind': 'SkirmishMissionAuthoringPreflight',
                   'catalog_id': 'S050', 'seed': 104779, 'size': 'Standard', 'difficulty': 'Regular',
                   'layout': self.layout_desc, 'setup_matrix': self.matrix_desc,
                   'campaign_source_path': str(self.map)}
        if selected:
            request['resource_preset'] = self.preset_desc
        request_desc = write_json(self.temp / ('author-selected.json' if selected else 'author-legacy.json'), request)
        pins = {request_desc['path']: {'sha256': request_desc['sha256']},
                self.graph_desc['path']: {'sha256': self.graph_desc['sha256']},
                self.layout_desc['path']: {'sha256': self.layout_desc['sha256']},
                self.matrix_desc['path']: {'sha256': self.matrix_desc['sha256']}}
        if selected and pin_selected:
            pins[self.preset_desc['path']] = {'sha256': self.preset_desc['sha256']}
        result = {'schema_version': 1, 'kind': 'SkirmishMissionAuthoringPreflightResult',
                  'status': 'ConfigurationCompiled', 'catalog_id': 'S050', 'seed': 104779,
                  'operation_map_id': self.target['operation_map_id'],
                  'source_content_hash': self.target['source_content_hash'],
                  'source_fingerprint': self.target['source_fingerprint'],
                  'layout_fingerprint': self.target['layout_fingerprint'],
                  'resolved_setup': {'setup_hash': self.target['setup_hash']},
                  'compiler_accepted': True, 'world_allocated': False,
                  'source_fit_verified': False, 'mission_ready': False, 'native_acceptance': False,
                  'input_pins': pins}
        result_desc = write_json(self.temp / ('result-selected.json' if selected else 'result-legacy.json'), result)
        return {'request': request_desc, 'result': result_desc}

    def compose(self, output, owner, override=None):
        with patch.object(composer, 'ROOT', self.temp):
            return composer.compose_envelope(output, self.initial, self.run, self.graph_pins, owner, override)

    def test_missing_compiler_selected_resource_preset_stages_partial_receipt_only(self):
        output = self.temp / 'T Logs' / 'missing-resource'
        receipt = self.compose(output, self.owner_pair(selected=False))
        self.assertEqual(receipt['status'], 'AwaitingExplicitResourcePreset')
        self.assertIs(receipt['runnable_bundle_emitted'], False)
        self.assertIs(receipt['initialization_emitted'], False)
        self.assertFalse((output / 'initialization.json').exists())
        self.assertFalse((output / 'data-bundle.json').exists())
        staged = json.loads((output / 'partial-owner-inputs.json').read_text())
        self.assertEqual(staged['missing_explicit_fields'], ['resource_preset'])
        self.assertEqual(staged['source_owner_authoring']['request'], self.owner_pair(selected=False)['request'])

    def test_exact_compiler_selected_resource_preset_emits_bound_nonready_envelope(self):
        output = self.temp / 'T Logs' / 'with-resource'
        owner = self.owner_pair()
        receipt = self.compose(output, owner)
        self.assertEqual(receipt['status'], 'ComposedSourceOnlyRequestEnvelope')
        self.assertIs(receipt['runnable_bundle_emitted'], True)
        self.assertIs(receipt['world_allocated'], False)
        self.assertIs(receipt['mission_ready'], False)
        initialization = json.loads((output / 'initialization.json').read_text())
        bundle = json.loads((output / 'data-bundle.json').read_text())
        self.assertEqual(initialization['resource_preset'], self.preset_desc)
        self.assertEqual(initialization['source_owner_authoring'], owner)
        self.assertEqual(bundle['initialization'], receipt['initialization'])

    def test_graph_pinned_but_unselected_override_cannot_bypass_authoring_join(self):
        output = self.temp / 'T Logs' / 'unselected-override'
        owner = self.owner_pair(selected=False)
        with self.assertRaisesRegex(ValueError, 'does not exactly match compiler author selection'):
            self.compose(output, owner, self.preset_desc)
        self.assertFalse((output / 'data-bundle.json').exists())

    def test_mismatched_override_and_unpinned_compiler_selection_are_rejected(self):
        output = self.temp / 'T Logs' / 'mismatched-override'
        owner = self.owner_pair()
        other = self.temp / 'other-preset.asset'
        other.write_text('different cadence')
        other_desc = {'path': str(other), 'sha256': hashlib.sha256(other.read_bytes()).hexdigest()}
        self.graph_pins[other_desc['path']] = other_desc['sha256']
        with self.assertRaisesRegex(ValueError, 'does not exactly match compiler author selection'):
            self.compose(output, owner, other_desc)
        self.assertFalse((output / 'data-bundle.json').exists())

        unpinned = self.temp / 'unpinned-preset.asset'
        unpinned.write_text('not in graph')
        unpinned_desc = {'path': str(unpinned), 'sha256': hashlib.sha256(unpinned.read_bytes()).hexdigest()}
        request = {'schema_version': 1, 'kind': 'SkirmishMissionAuthoringPreflight',
                   'catalog_id': 'S050', 'seed': 104779, 'size': 'Standard', 'difficulty': 'Regular',
                   'layout': self.layout_desc, 'setup_matrix': self.matrix_desc,
                   'campaign_source_path': str(self.map), 'resource_preset': unpinned_desc}
        req_desc = write_json(self.temp / 'author-unpinned.json', request)
        result = json.loads(Path(owner['result']['path']).read_text())
        result['input_pins'][req_desc['path']] = {'sha256': req_desc['sha256']}
        result['input_pins'][unpinned_desc['path']] = {'sha256': unpinned_desc['sha256']}
        result['input_pins'][self.graph_desc['path']] = {'sha256': self.graph_desc['sha256']}
        result['input_pins'][self.layout_desc['path']] = {'sha256': self.layout_desc['sha256']}
        result['input_pins'][self.matrix_desc['path']] = {'sha256': self.matrix_desc['sha256']}
        result_desc = write_json(self.temp / 'result-unpinned.json', result)
        with self.assertRaisesRegex(ValueError, 'current authored graph source pin'):
            self.compose(self.temp / 'T Logs' / 'unpinned-selection',
                         {'request': req_desc, 'result': result_desc})

    def test_compiler_result_must_pin_selected_resource_preset(self):
        owner = self.owner_pair(pin_selected=False)
        with self.assertRaisesRegex(ValueError, 'does not pin the exact selected resource preset'):
            self.compose(self.temp / 'T Logs' / 'result-missing-preset-pin', owner)

    def test_compiler_physical_request_must_select_exact_result_and_match_source_package(self):
        source = self.temp / 'source.asset'; source.write_text('source')
        grid = self.temp / 'grid.asset'; grid.write_text('grid')
        surface = self.temp / 'surface.asset'; surface.write_text('surface')
        result_path = self.temp / 'r4-result.json'
        result_path.write_text('{}')
        result = {'_path': str(result_path), 'operation_map_id': 'map.city',
                  'source_content_hash': 'content.city'}
        frame = {'width': 10, 'height': 8, 'cell_size': 1, 'origin': [1, 0, 2]}
        source_desc = {'path': str(source.resolve()), 'sha256': hashlib.sha256(source.read_bytes()).hexdigest()}
        request = {'schema_version': 1, 'kind': 'PinnedCampaignPhysicalInputExport',
                   'source_path': 'source.asset', 'source_sha256': source_desc['sha256'],
                   'grid_path': 'grid.asset', 'grid_sha256': hashlib.sha256(grid.read_bytes()).hexdigest(),
                   'surface_path': 'surface.asset', 'surface_sha256': hashlib.sha256(surface.read_bytes()).hexdigest(),
                   'operation_map_id': 'map.city', 'content_hash': 'content.city', 'frame': frame,
                   'selection': {'path': str(result_path), 'sha256': hashlib.sha256(b'{}').hexdigest()},
                   'source_fit_certified': False, 'mission_ready': False}
        request_path = self.temp / 'physical-request.json'; request_path.write_text(json.dumps(request))
        package = {'static_map': {'operation_map_id': 'map.city', 'definition': source_desc}, 'frame': frame}
        with patch.object(composer, 'ROOT', self.temp):
            self.assertEqual(str(request_path.resolve()), composer.validate_compiler_physical_request(
                request_path, {}, result, package)['path'])
            bad = dict(request); bad['selection'] = dict(request['selection'], sha256='0' * 64)
            request_path.write_text(json.dumps(bad))
            with self.assertRaisesRegex(ValueError, 'exact current compiler result'):
                composer.validate_compiler_physical_request(request_path, {}, result, package)

    def test_compiler_selected_unit_and_building_handoffs_require_matching_r4_r3_audit_joins(self):
        result_path = self.temp / 'author-result.json'; result_path.write_text('{}')
        physical_path = self.temp / 'physical-request.json'; physical_path.write_text('{}')
        result_desc = {'path': str(result_path.resolve()), 'sha256': hashlib.sha256(result_path.read_bytes()).hexdigest()}
        physical_desc = {'path': str(physical_path.resolve()), 'sha256': hashlib.sha256(physical_path.read_bytes()).hexdigest()}
        preset_path = self.temp / 'SkirmishBaseAssault.asset'; preset_path.write_text('preset')
        preset_desc = {'path': str(preset_path.resolve()), 'sha256': hashlib.sha256(preset_path.read_bytes()).hexdigest()}
        author_result = {'resolved_setup': {'setup_hash': 17}, 'operation_map_id': 'map.city',
                         'layout_fingerprint': 'compiled-layout.hash',
                         'input_pins': {preset_desc['path']: {'sha256': preset_desc['sha256']}}}
        semantic_hash='skirmish.s050.v2/enemy-policy-v1/'+'a'*64
        for owner in ('unit', 'building'):
            folder = self.temp / owner; folder.mkdir()
            metadata_path = folder / ('unit-production-metadata.json' if owner == 'unit' else 'building-definitions.json')
            metadata = {'catalog_id': 'S050', 'seed': 104779, 'setup_hash': 17,
                        'compiler_selection': {'result': result_desc, 'physical_input_request': physical_desc,
                            'operation_map_id': 'map.city', 'layout_fingerprint': 'layout.hash',
                            'source_content_hash': semantic_hash,
                            'setup_hash': 17, 'world_allocated': False,
                            'source_fit_certified': False, 'mission_ready': False},
                        'unsupported_values': [], 'full_mission_initialized': False,
                        'native_visual_readiness': False, 'source_preset_index': 0,
                        'compiler_selection_asset_readback': {'status': 'VerifiedAssetIdentityOnly'}}
            metadata_path.write_text(json.dumps(metadata))
            metadata_sha = hashlib.sha256(metadata_path.read_bytes()).hexdigest()
            if owner == 'unit':
                (folder / 'unit-prefab-identity-readback.json').write_text(json.dumps({
                    'metadata_sha256': metadata_sha, 'catalog_id': 'S050', 'seed': 104779,
                    'setup_hash': 17, 'mission_ready': False}))
            audit_path = folder / 'audit.json'
            audit_path.write_text(json.dumps({'kind': 'S050CompilerSelectedPresetUnityOwnerAudit',
                'owner': owner, 'status': 'Passed', 'source_pin_count': 1,
                'source_pin_missing_count': 0, 'source_pin_changed_count': 0, 'source_pin_mismatches': [],
                'compiler_selection': {'result': result_desc, 'physical_input_request': physical_desc,
                    'layout_fingerprint':'layout.hash','source_content_hash':semantic_hash},
                'preset': {'path': preset_desc['path'], 'sha256': preset_desc['sha256'],
                           'supply_day_seconds_from_pinned_source_graph': 120,
                           'compiler_result_input_pin': {'sha256': preset_desc['sha256']}},
                'physical_source': {'request_path': physical_desc['path'], 'request_sha256': physical_desc['sha256']},
                'pass_marker_counts': {'[Owner] result=Passed': 1},
                'checks': {key:True for key in (
                    'author_result_sha','physical_request_sha','preset_descriptor_matches','preset_value_is_120',
                    'preset_result_input_pin','graph_result_input_pin','physical_map_matches_compiler',
                    'compiler_selection_author_result','unit_unity_assetdb_preset_identity',
                    'building_unity_assetdb_preset_identity','unit_preset_node_not_hydrated',
                    'building_preset_node_not_hydrated','unity_asset_field_parity_not_claimed',
                    'unit_identity_metadata_sha','unit_prefab_identity_joins','building_definition_keys',
                    'unit_all_source_pins_current','building_all_source_pins_current','unit_pins_compiler_result',
                    'unit_pins_preset_asset','unit_pins_physical_request','building_pins_compiler_result',
                    'building_pins_preset_asset','building_pins_physical_request','unit_exact_pass_markers',
                    'unit_editor_gui_mode','unit_durable_log_byte_match','building_exact_pass_markers',
                    'building_editor_gui_mode','building_durable_log_byte_match')},
                'nonclaims': {'world_allocated': False, 'source_fit_certified': False,
                              'mission_ready': False, 'native_acceptance': False}}))
            self.assertEqual(metadata_sha, composer.validate_preset_handoff(owner, metadata_path,
                audit_path, result_path, author_result, physical_desc, preset_desc,
                {'layout_binding_fingerprint': 'layout.hash','definition_id':'skirmish.s050',
                 'content_version':2})[0]['sha256'])
            bad_audit = json.loads(audit_path.read_text())
            bad_audit['compiler_selection']['physical_input_request']['sha256'] = '0' * 64
            audit_path.write_text(json.dumps(bad_audit))
            with self.assertRaisesRegex(ValueError, 'does not bind the exact compiler and physical request'):
                composer.validate_preset_handoff(owner, metadata_path, audit_path,
                    result_path, author_result, physical_desc, preset_desc,
                    {'layout_binding_fingerprint': 'layout.hash','definition_id':'skirmish.s050',
                     'content_version':2})

    def test_owner_layout_binds_context_layout_binding_and_semantic_hash_is_not_physical_map_hash(self):
        result_path=self.temp/'semantic-result.json';result_path.write_text('{}')
        request_path=self.temp/'semantic-request.json';request_path.write_text('{}')
        result_desc={'path':str(result_path.resolve()),'sha256':hashlib.sha256(result_path.read_bytes()).hexdigest()}
        request_desc={'path':str(request_path.resolve()),'sha256':hashlib.sha256(request_path.read_bytes()).hexdigest()}
        metadata_path=self.temp/'metadata.json'
        semantic='skirmish.s050.v2/enemy-policy-v1/'+'b'*64
        metadata_path.write_text(json.dumps({'catalog_id':'S050','seed':104779,'setup_hash':17,
            'compiler_selection':{'result':result_desc,'physical_input_request':request_desc,
                'operation_map_id':'map.city','layout_fingerprint':'other-layout',
                'source_content_hash':semantic,'setup_hash':17,'world_allocated':False,
                'source_fit_certified':False,'mission_ready':False},
            'unsupported_values':[],'full_mission_initialized':False,'native_visual_readiness':False}))
        audit_path=self.temp/'audit.json';audit_path.write_text('{}')
        with self.assertRaisesRegex(ValueError,'handoff differs from compiler/preset selection'):
            composer.validate_preset_handoff('building',metadata_path,audit_path,result_path,
                {'resolved_setup':{'setup_hash':17},'operation_map_id':'map.city','layout_fingerprint':'physical-layout'},
                request_desc,validated_context={'layout_binding_fingerprint':'expected-layout',
                    'definition_id':'skirmish.s050','content_version':2},expected_owner_semantic=semantic)
        metadata=json.loads(metadata_path.read_text())
        metadata['compiler_selection']['layout_fingerprint']='expected-layout'
        metadata_path.write_text(json.dumps(metadata))
        with self.assertRaisesRegex(ValueError,'handoff differs from compiler/preset selection'):
            composer.validate_preset_handoff('building',metadata_path,audit_path,result_path,
                {'resolved_setup':{'setup_hash':17},'operation_map_id':'map.city','layout_fingerprint':'physical-layout'},
                request_desc,validated_context={'layout_binding_fingerprint':'expected-layout',
                    'definition_id':'skirmish.s050','content_version':2},expected_owner_semantic='skirmish.s050.v2/enemy-policy-v1/'+'c'*64)

    def _death_projection_fixture(self):
        keys_source = Path(__file__).resolve().parents[4] / 'T Logs/SkirmishProduction/s050-unit-handoff-prefab-key-descriptor-20261115-r1/prefab-keys.json'
        keys = self.temp / 'T Logs/prefab-keys.json'; keys.parent.mkdir(parents=True, exist_ok=True); keys.write_bytes(keys_source.read_bytes())
        key_desc = {'path': str(keys.resolve()), 'sha256': hashlib.sha256(keys.read_bytes()).hexdigest()}
        capture_rows = [{'key': key, 'configured_duration_bits': '3f800000', 'death_animation_index': 0,
            'has_death_animation_selection': True, 'configured_wreck_bypasses_prepare': False,
            'legacy_wreck_bypasses_prepare': False, 'actual_original_prepare_bits': '40400000',
            'reversed_child_original_prepare_bits': '40400000', 'original_fold_bits': '40400000',
            'unit_death_time_remaining_assignment_bits': '40400000',
            'visual_readset': [{'has_animator_link': True, 'animator_exists': True, 'blob_created': True,
                'animation_count': 1, 'clips': [{'frames': 30, 'fps': 30, 'speed': 1}]}]} for key in json.loads(keys.read_text())]
        capture = write_json(self.temp / 'Logs/death-capture.json', {
            'kind': 'NativeSelectedPrefabDeathMechanicalReadset',
            'authority': 'OriginalNativePrepare_OriginalParentSystem_OriginalKernel_NotMissionState',
            'source_pins': {key_desc['path']: key_desc['sha256']}, 'keys_sha256': key_desc['sha256'],
            'selected_prefab_set_mode': 'ExplicitHashPinnedGenericSet', 'rows': capture_rows,
            'full_mission_initialized': False, 'renderer_qualified': False, 'runtime_admitted': False})
        audit = write_json(self.temp / 'T Logs/death-audit.json', {
            'kind': 'S050DeathMechanicalReadsetAudit', 'status': 'PassedForMechanicalReadsetOnly',
            'readset': capture, 'prefab_key_set': key_desc, 'source_pin_audit': {'count': 1, 'mismatches': 0},
            'exact_pass_marker_count': 1,
            'flags': {'full_mission_initialized': False, 'renderer_qualified': False, 'runtime_admitted': False}})
        projection = write_json(self.temp / 'T Logs/death-projection.json', {
            'schema_version': 1, 'kind': 'S050SourceDiagnosticDeathMechanicalProjection',
            'source_capture': capture, 'source_audit': audit, 'prefab_key_set': key_desc,
            'keys_sha256': key_desc['sha256'], 'rows': [{
                'key': row['key'], 'configured_duration_bits': row['configured_duration_bits'],
                'death_animation_index': row['death_animation_index'],
                'has_death_animation_selection': row['has_death_animation_selection'],
                'configured_wreck_bypasses_prepare': row['configured_wreck_bypasses_prepare'],
                'legacy_wreck_bypasses_prepare': row['legacy_wreck_bypasses_prepare'],
                'linked_animation_inputs': [{'animator_exists': True, 'blob_created': True,
                    'animation_count': 1, 'clips': [{'frames': 30, 'fps': 30, 'speed': 1}]}]}
                for row in capture_rows],
            'full_mission_initialized': False, 'renderer_qualified': False,
            'runtime_admitted': False, 'mission_ready': False})
        receipt = write_json(self.temp / 'T Logs/death-projection-receipt.json', {
            'schema_version': 1, 'kind': 'S050SourceDiagnosticDeathProjectionReceipt',
            'status': 'ProjectedSourceDiagnosticOnly', 'source_capture': capture, 'source_audit': audit,
            'prefab_key_set': key_desc, 'capture_root': str(self.temp), 'source_pin_count': 1,
            'projection_bytes': Path(projection['path']).stat().st_size, 'projection_sha256': projection['sha256'],
            'rows': 16, 'full_mission_initialized': False, 'renderer_qualified': False,
            'runtime_admitted': False, 'mission_ready': False, 'projection': projection})
        return Path(projection['path']), Path(receipt['path'])

    def test_death_projection_join_accepts_only_bounded_diagnostic_artifacts(self):
        projection_path, receipt_path = self._death_projection_fixture()
        with patch.object(composer, 'ROOT', self.temp):
            result = composer.validate_death_mechanical_projection(projection_path, receipt_path)
        self.assertEqual(set(result), {'s050_source_diagnostic'})
        self.assertFalse(json.loads(projection_path.read_text())['runtime_admitted'])

    def test_death_projection_join_rejects_admission_claims(self):
        projection_path, receipt_path = self._death_projection_fixture()
        projection = json.loads(projection_path.read_text()); projection['runtime_admitted'] = True
        changed = write_json(projection_path, projection)
        receipt = json.loads(receipt_path.read_text()); receipt['projection'] = changed
        receipt['projection_sha256'] = changed['sha256']; receipt['projection_bytes'] = Path(changed['path']).stat().st_size
        write_json(receipt_path, receipt)
        with patch.object(composer, 'ROOT', self.temp):
            with self.assertRaisesRegex(ValueError, 'diagnostic projection'):
                composer.validate_death_mechanical_projection(projection_path, receipt_path)

    def test_death_projection_join_rejects_stale_audit_source_link(self):
        projection_path, receipt_path = self._death_projection_fixture()
        receipt = json.loads(receipt_path.read_text()); receipt['source_audit']['sha256'] = '0' * 64
        write_json(receipt_path, receipt)
        with patch.object(composer, 'ROOT', self.temp):
            with self.assertRaisesRegex(ValueError, 'source/audit/key descriptor'):
                composer.validate_death_mechanical_projection(projection_path, receipt_path)

    def test_death_projection_join_rejects_altered_compact_row(self):
        projection_path, receipt_path = self._death_projection_fixture()
        projection = json.loads(projection_path.read_text())
        projection['rows'][0]['configured_duration_bits'] = '00000000'
        changed = write_json(projection_path, projection)
        receipt = json.loads(receipt_path.read_text()); receipt['projection'] = changed
        receipt['projection_sha256'] = changed['sha256']; receipt['projection_bytes'] = Path(changed['path']).stat().st_size
        write_json(receipt_path, receipt)
        with patch.object(composer, 'ROOT', self.temp):
            with self.assertRaisesRegex(ValueError, 'diagnostic projection'):
                composer.validate_death_mechanical_projection(projection_path, receipt_path)


if __name__ == '__main__':
    unittest.main()
