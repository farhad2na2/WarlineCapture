import importlib.util
import json
import tempfile
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
SCRIPT = Path(__file__).with_name('project_s002_focused_prefab_diagnostic.py')
SPEC = importlib.util.spec_from_file_location('s002_focused_diagnostic', SCRIPT)
MODULE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(MODULE)
CAPTURE = Path('/private/tmp/s002-focused-prefab-capture-20261102-r1/prefab-reference-data.json')
LOG = Path('/private/tmp/s002-focused-prefab-export-20261102-r1.log')
METADATA = Path('/private/tmp/s002-two-seed-current-20261102-r2/two-seed-metadata.json')
KEYS = Path('/private/tmp/s002-focused-prefab-keys-20261102-r1.json')


class S002FocusedPrefabDiagnosticTests(unittest.TestCase):
    def test_exact_failed_capture_projects_without_upgrading_validation(self):
        with tempfile.TemporaryDirectory(prefix='s002-focused-projection-') as temp:
            result = MODULE.project(CAPTURE, LOG, 0, METADATA, KEYS, Path(temp) / 'projection', ROOT)
            self.assertEqual(result['capture_validation_status'], 'Failed')
            self.assertFalse(result['capture_pass_marker_present'])
            self.assertEqual(result['unsupported_presentation_omission_count'], 14)
            self.assertFalse(result['replay_ready'])
            self.assertFalse(result['mission_ready'])
            self.assertEqual(result['descriptor']['capture_validation_status'], 'Failed')
            self.assertFalse(result['descriptor']['capture_pass_marker_present'])
            domain = json.loads(Path(result['descriptor']['path']).read_text())
            capture = json.loads(CAPTURE.read_text())
            for field in MODULE.FIELDS:
                self.assertEqual(domain[field], capture[field])
            self.assertEqual(domain['source_reference_sha256'], MODULE.CAPTURE_SHA)
            self.assertEqual(domain['unsupported_values'], capture['unsupported_values'])

    def test_other_unsupported_fields_are_rejected(self):
        with self.assertRaisesRegex(ValueError, 'Unsupported required template value'):
            MODULE.classify_unsupported([{
                'path': '1:1/Game.Components.OtherComponent/Prefab',
                'reason': 'unity_object_ref_requires_bound_source_identity',
                'type': 'Unity.Entities.UnityObjectRef`1[[UnityEngine.GameObject,',
            }])


if __name__ == '__main__':
    unittest.main()
