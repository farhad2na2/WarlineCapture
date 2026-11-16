import copy
import importlib.util
from pathlib import Path
import unittest

SCRIPT = Path(__file__).with_name('project_s050_audited_factory_handoffs.py')
spec = importlib.util.spec_from_file_location('project_s050_audited_factory_handoffs', SCRIPT)
projector = importlib.util.module_from_spec(spec)
spec.loader.exec_module(projector)


class ProjectS050AuditedFactoryHandoffsTests(unittest.TestCase):
    def setUp(self):
        self.context_desc = {'path': '/repo/context.json', 'sha256': 'c' * 64}
        self.target = {'catalog_id': 'S050', 'definition_id': 'skirmish.s050', 'content_version': 2,
                       'seed': 104779, 'size': 'Standard', 'difficulty': 'Regular', 'setup_hash': 472937146,
                       'operation_map_id': 'opmap.ch05.citywide_alert', 'layout_binding_fingerprint': 'layout-binding'}
        building = {'key': 'Building_Barrack', 'prefab': {'key': 'Building_Barrack'},
                    'fields': {'Health': {'codec': 'integer-v1', 'value': '100'}}}
        unit = {'index': 0, 'prefab': {'key': 'Unit_Rifle'}, 'has_definition_metadata': True,
                'has_production_metadata': True, 'definition_metadata': {'CanRequest': True},
                'production_metadata': {'MaterialsCost': 5}, 'authoring_catalog_properties': {'CanRequest': True}}
        transport = {'prefab': {'key': 'Transport'}, 'nodes': []}
        building['index'] = 0
        building['construction_index'] = 0
        building['scene_index'] = 1
        building['configured_entry'] = {'fields': {'DisplayName': {'value': 'Barracks'},
                                                   'CanRequest': {'value': True}}}
        configured = {'index': 0, 'prefab': building['prefab'], 'construction_index': 0, 'scene_index': 1,
                      'configured_entry': {'fields': {'DisplayName': {'value': 'Barracks'},
                                                     'CanRequest': {'value': True}}}}
        self.native_origin = {'catalog_id': 'S026', 'size': 'Standard', 'difficulty': 'Regular',
                              'seed': 104755, 'setup_hash': 1494420755,
                              'metadata_key_sha256': 'm' * 64}
        self.native_reference = {'kind': 'NativeS026LogicalBoundMetadataQualification', 'sha256': 'n' * 64}
        self.template_origin = dict(self.native_origin, kind=self.native_reference['kind'],
                                    artifact_sha256=self.native_reference['sha256'])
        self.context = {'schema_version': 1, 'kind': 'OriginalCompilerDerivedNativeFactoryContext',
                        'authority': 'ActualSourceCompilerContext_ImmutableNativeFactoryFacts_NotNativeS050',
                        'target_context': self.target, 'native_origin': self.native_origin,
                        'native_factory_reference': self.native_reference,
                        'native_S050_capture': False, 'world_allocated': False, 'mission_ready': False,
                        'runtime_loader_admitted': False,
                        'derived_metadata': {'buildings': [building], 'units': [unit], 'transports': [transport]}}
        self.qualification = {'kind': 'ExperimentalGroundConfigurationQualification',
                              'runtime_backend': 'TypedGroundS026V1', 'compiler_context': self.context_desc,
                              'mission_ready': False, 'native_context_qualified': False}
        semantic = 'skirmish.s050.v2/enemy-policy-v1/' + 'a' * 64
        selection = {'catalog_id': 'S050', 'seed': 104779, 'size': 'Standard', 'difficulty': 'Regular',
                     'setup_hash': 472937146, 'operation_map_id': 'opmap.ch05.citywide_alert',
                     'layout_fingerprint': 'layout-binding', 'source_content_hash': semantic,
                     'world_allocated': False, 'source_fit_certified': False, 'mission_ready': False,
                     'native_acceptance': False}
        self.building_owner = {'compiler_selection': copy.deepcopy(selection),
                               'definitions': [{'key': building['key'], 'prefab': copy.deepcopy(building['prefab']),
                                                'fields': copy.deepcopy(building['fields']), 'factory': 'source-owner'}]}
        self.unit_owner = {'compiler_selection': copy.deepcopy(selection),
                           'units': [dict(copy.deepcopy(unit), key='Unit_Rifle')],
                           'transports': [copy.deepcopy(transport)]}
        common = {'schema_version': 3, 'catalog_id': 'S050', 'seed': 104779, 'setup_hash': 472937146,
                  'metadata_compiler_context': self.context_desc, 'native_origin': self.template_origin,
                  'native_S050_capture': False, 'full_mission_initialized': False,
                  'native_visual_readiness': False, 'unsupported_values': []}
        self.building_template = dict(common, kind='CompilerContextDerivedSkirmishBuildingDefinitions',
                                      definitions=[{'key': building['key'], 'prefab': building['prefab'],
                                                    'fields': building['fields']}])
        self.unit_template = dict(common, kind='CompilerContextDerivedSkirmishUnitProductionMetadata',
                                  units=[dict(unit, key='Unit_Rifle')], transports=[transport])
        configured_row = {'definition_index': 0, 'key': 'Building_Barrack', 'prefab': building['prefab'],
                          'display_name': 'Barracks', 'can_request': True,
                          'construction_catalog_index': 0, 'scene_catalog_index': 1}
        self.configured_template = dict(common, kind='CompilerContextDerivedConfiguredBuildingMetadata',
                                         rows=[configured_row])

    def compare(self):
        return projector.validate_context_and_owner_fields(
            self.context, self.context_desc, self.qualification,
            self.building_owner, self.unit_owner,
            self.building_template, self.unit_template, self.configured_template)

    def test_current_direct_owner_rows_match_compiler_context(self):
        result = self.compare()
        self.assertEqual(result['owner_semantic_content_hash'], self.building_owner['compiler_selection']['source_content_hash'])
        self.assertEqual(result['building_definitions'], self.building_template['definitions'])

    def test_building_field_mismatch_is_rejected(self):
        self.building_owner['definitions'][0]['fields']['Health']['value'] = '101'
        with self.assertRaisesRegex(ValueError, 'building owner fields differ'):
            self.compare()

    def test_unit_field_mismatch_is_rejected(self):
        self.unit_owner['units'][0]['production_metadata']['MaterialsCost'] = 6
        with self.assertRaisesRegex(ValueError, 'unit owner fields differ'):
            self.compare()

    def test_selection_layout_mismatch_is_rejected(self):
        self.unit_owner['compiler_selection']['layout_fingerprint'] = 'other-layout'
        with self.assertRaisesRegex(ValueError, 'unit owner selection differs'):
            self.compare()

    def test_unit_and_building_semantic_identity_must_match(self):
        self.unit_owner['compiler_selection']['source_content_hash'] = 'skirmish.s050.v2/enemy-policy-v1/' + 'b' * 64
        with self.assertRaisesRegex(ValueError, 'semantic content hashes differ'):
            self.compare()


if __name__ == '__main__':
    unittest.main()
