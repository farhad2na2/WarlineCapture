import base64
import hashlib
import json
from pathlib import Path
import sys
import tempfile
import unittest
from unittest.mock import patch

import compare_s073_source_buildings as comparator


def text_value(value):
    raw = value.encode()
    return {'codec': 'binary-v1', 'encoding': 'fixed-utf8-v1', 'byte_count': len(raw),
            'sha256': hashlib.sha256(raw).hexdigest(), 'base64': base64.b64encode(raw).decode()}


class BuildingComparisonTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.root = Path(self.temp.name)
        prefix = 'Game.Components.OperationMapBuildingComponent::'
        self.source = {'kind': 'S073IsolatedStreamedSourceWorldCapture', 'schema_version': 1,
                       'scene_guid': 'c00140f2e94a04c3084c8dcb0c18cbd0',
                       'scene_path': 'scene.unity', 'scene_sha256': 'fixture',
                       'closure_complete': False, 'mission_ready': False, 'live_s073_session_observed': False,
                       'physical_source_validated': False, 'scheduler_admitted': False, 'unsupported_values': [],
                       'root_entity_ids': ['1:1'], 'building_faction_query_count': 1,
                       'entities': [{'id': '1:1', 'components': {
                           'Game.Components.OperationMapBuildingComponent': {'value': {'fields': {
                               prefix + 'StableId': text_value('stable'),
                               prefix + 'SourceGlobalObjectId': text_value('object'),
                               prefix + 'PlacementIndex': {'value': '3'}}}},
                           'Game.Components.Faction': {'value': {'fields': {
                               'Game.Components.Faction::Id': {'value': '1'}}}},
                           'Game.Components.OperationMapBuildingProductionPrefab': {'buffer': True, 'value': {
                               'codec': 'template-buffer-values-v1',
                               'element_type': 'Game.Components.OperationMapBuildingProductionPrefab', 'items': []}}}}]}
        self.native = {'kind': 'S073PrepareMapBoundaryAndStreamCensus', 'status': 'Passed',
                       'mission_ready': False, 'mission_outcome_observed': False, 'session_id': 'fixture',
                       'source_pins': {'scene.unity': 'fixture'}, 'post_stream': {
                           'ecs_scene_guid': self.source['scene_guid'], 'source_scene_loaded': True,
                           'ecs_scene_loaded': True, 'authored_grid_section_owned': True,
                           'physical_source_validated': 1, 'phase': 'Preparing', 'generation': 1,
                           'entity_count': 1, 'entities': [{'stable_id': 'stable', 'source_global_object_id': 'object',
                               'placement_index': 3, 'faction': 0, 'source_section_owned': True}]}}
        identity = {'world_sequence': '1', 'session_entity': [1, 1], 'map_root_entity': [2, 1],
                    'generation': 1, 'source_identity_hash': 'identity', 'source_content_hash': 'content'}
        self.native['before'] = dict(identity, phase='Queued')
        self.native['after'] = dict(identity, phase='Preparing')
        self.native['post_stream'].update(identity)

    def tearDown(self):
        self.temp.cleanup()

    def run_case(self):
        source, native, output = (self.root / name for name in ('source.json', 'native.json', 'result.json'))
        source.write_text(json.dumps(self.source)); native.write_text(json.dumps(self.native))
        source_receipt, native_receipt = self.root / 'source-receipt.json', self.root / 'native-receipt.json'
        source_receipt.write_text(json.dumps({'capture_sha256': comparator.sha(source), 'scene_sha256': 'fixture',
            'wrapper_exit_code': 0, 'required_marker_present': True}))
        native_receipt.write_text(json.dumps({'census_sha256': comparator.sha(native), 'wrapper_exit_code': 0,
            'required_marker_present': True, 'validation_status': 'Passed'}))
        arguments = ['compare', '--source', str(source), '--source-sha256', comparator.sha(source),
                     '--source-receipt', str(source_receipt), '--native', str(native),
                     '--native-sha256', comparator.sha(native), '--native-receipt', str(native_receipt), '--output', str(output)]
        with patch.object(sys, 'argv', arguments):
            code = comparator.main()
        return code, json.loads(output.read_text())

    def test_matching_identities_does_not_assert_readiness(self):
        code, result = self.run_case()
        self.assertEqual(code, 0)
        self.assertEqual(result['matched'], 1)
        self.assertFalse(result['mission_ready'])
        self.assertFalse(result['entity_payload_parity'])

    def test_different_placement_is_reported(self):
        self.native['post_stream']['entities'][0]['placement_index'] = 4
        code, result = self.run_case()
        self.assertEqual(code, 2)
        self.assertEqual((result['source_only'], result['native_only']), (1, 1))

    def test_unloaded_scene_rejected(self):
        self.native['post_stream']['ecs_scene_loaded'] = False
        with self.assertRaisesRegex(ValueError, 'Same physical source'):
            self.run_case()

    def test_unowned_building_rejected(self):
        self.native['post_stream']['entities'][0]['source_section_owned'] = False
        with self.assertRaisesRegex(ValueError, 'owned by the source section'):
            self.run_case()

    def test_changed_session_entity_rejected(self):
        self.native['post_stream']['session_entity'] = [3, 1]
        with self.assertRaisesRegex(ValueError, 'identity changed'):
            self.run_case()


if __name__ == '__main__':
    unittest.main()
