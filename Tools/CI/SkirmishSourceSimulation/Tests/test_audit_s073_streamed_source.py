import copy
import json
import tempfile
import unittest
from pathlib import Path

from audit_s073_streamed_source import audit, digest


class SourceGraphAuditTests(unittest.TestCase):
    def setUp(self):
        self.directory = tempfile.TemporaryDirectory()
        self.path = Path(self.directory.name) / 'capture.json'
        self.data = {
            'kind': 'S073IsolatedStreamedSourceWorldCapture', 'schema_version': 1,
            'scene_guid': 'c00140f2e94a04c3084c8dcb0c18cbd0', 'scene_sha256': 'fixture',
            'closure_complete': False, 'live_s073_session_observed': False,
            'physical_source_validated': False, 'scheduler_admitted': False, 'mission_ready': False,
            'root_entity_ids': ['1:1'], 'building_faction_query_count': 1, 'unsupported_values': [],
            'entities': [{'id': '1:1', 'components': {
                'Game.Components.OperationMapBuildingComponent': {'value': {'codec': 'entity-v1', 'id': '2:1'}},
                'Game.Components.Faction': {},
                'Game.Components.OperationMapBuildingProductionPrefab': {'buffer': True, 'value': {
                    'codec': 'template-buffer-values-v1',
                    'element_type': 'Game.Components.OperationMapBuildingProductionPrefab', 'items': []}}}},
                {'id': '2:1', 'components': {}}]}

    def tearDown(self):
        self.directory.cleanup()

    def run_audit(self, data):
        self.path.write_text(json.dumps(data))
        return audit(self.path, {'capture_sha256': digest(self.path), 'scene_sha256': 'fixture',
                                 'wrapper_exit_code': 0, 'required_marker_present': True})

    def test_closed_graph_does_not_admit_import(self):
        result = self.run_audit(self.data)
        self.assertEqual(result['encoded_entity_references'], 1)
        self.assertFalse(result['import_admitted'])
        self.assertFalse(result['mission_ready'])

    def test_unbound_entity_reference_rejected_even_with_matching_hash(self):
        data = copy.deepcopy(self.data)
        data['entities'][0]['components']['Game.Components.OperationMapBuildingComponent']['value']['id'] = '3:1'
        with self.assertRaisesRegex(ValueError, 'outside the serialized graph'):
            self.run_audit(data)

    def test_duplicate_entity_rejected(self):
        data = copy.deepcopy(self.data)
        data['entities'].append(copy.deepcopy(data['entities'][0]))
        with self.assertRaisesRegex(ValueError, 'Unique valid'):
            self.run_audit(data)


if __name__ == '__main__':
    unittest.main()
