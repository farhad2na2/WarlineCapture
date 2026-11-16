import copy
import json
from pathlib import Path
import sys
import tempfile
import unittest
from unittest.mock import patch

import compare_s073_static_histories as comparison


class StaticHistoryComparisonTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.root = Path(self.temp.name)
        row = {'stable_id': 'a', 'source_global_object_id': 'b', 'placement_index': 3,
               'static_history': {'min': [2, 3], 'max': [4, 5], 'friendly_pass_faction': 255,
                                  'bound_to_grid_epoch': True}}
        self.isolated = {'capture_sha256': 'source', 'status': 'IsolatedRoundtrip_NotMissionImport',
                         'static_blocker_owner': {'owner': 'StaticGridBlockerUpdateSystem.OnUpdate'},
                         'manager_exposed_to_mission': False, 'mission_ready': False, 'normalized_buildings': [row]}
        self.native = {'post_stream': {'static_grid_committed': True, 'static_history_bound_count': 1,
                                       'entity_count': 1, 'entities': [copy.deepcopy(row)]}}

    def tearDown(self):
        self.temp.cleanup()

    def run_case(self):
        isolated, native, receipt, identity, output = (self.root / name for name in
            ('isolated.json', 'native.json', 'receipt.json', 'identity.json', 'output.json'))
        isolated.write_text(json.dumps(self.isolated)); native.write_text(json.dumps(self.native))
        native_sha = comparison.sha(native)
        receipt.write_text(json.dumps({'wrapper_exit_code': 0, 'required_marker_present': True,
                                       'validation_status': 'Passed', 'census_sha256': native_sha}))
        identity.write_text(json.dumps({'native_sha256': native_sha, 'source_sha256': 'source', 'status': 'IdentitySetsMatch'}))
        args = ['compare', '--isolated', str(isolated), '--isolated-sha256', comparison.sha(isolated),
                '--native', str(native), '--native-receipt', str(receipt), '--identity-comparison', str(identity),
                '--output', str(output)]
        with patch.object(sys, 'argv', args):
            code = comparison.main()
        return code, json.loads(output.read_text())

    def test_matching_history_is_not_full_grid_parity(self):
        code, result = self.run_case()
        self.assertEqual(code, 0)
        self.assertFalse(result['full_grid_parity'])
        self.assertFalse(result['mission_ready'])

    def test_different_bounds_reported(self):
        self.native['post_stream']['entities'][0]['static_history']['max'] = [8, 9]
        code, result = self.run_case()
        self.assertEqual(code, 2)
        self.assertEqual(result['field_difference_counts']['max'], 1)

    def test_unbound_history_rejected(self):
        self.native['post_stream']['entities'][0]['static_history']['bound_to_grid_epoch'] = False
        with self.assertRaisesRegex(ValueError, 'bind its own'):
            self.run_case()


if __name__ == '__main__':
    unittest.main()
