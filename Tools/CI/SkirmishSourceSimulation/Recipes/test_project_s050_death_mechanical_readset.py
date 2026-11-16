import hashlib
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest


ROOT = Path(__file__).resolve().parents[4]
MODULE_PATH = Path(__file__).with_name('project_s050_death_mechanical_readset.py')
SPEC = importlib.util.spec_from_file_location('project_s050_death_mechanical_readset', MODULE_PATH)
MODULE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(MODULE)


class ProjectS050DeathMechanicalReadsetTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.root = Path(self.temp.name)
        self.keys_path = self.root / 'T Logs/keys.json'
        self.keys_path.parent.mkdir(parents=True)
        self.keys = [f'Unit_{i:02d}' for i in range(16)]
        self.keys_bytes = (json.dumps(self.keys, indent=2) + '\n').encode()
        self.keys_path.write_bytes(self.keys_bytes)
        self.key_sha = hashlib.sha256(self.keys_bytes).hexdigest()
        self.source_path = self.root / 'Assets/source.cs'
        self.source_path.parent.mkdir(parents=True)
        self.source_path.write_text('source pin\n')
        self.log_path = self.root / 'T Logs/unity.log'
        self.log_path.write_text(MODULE.EXPECTED_MARKER + '\n')
        self.capture_path = self.root / 'Logs/capture.json'
        self.capture_path.parent.mkdir()
        self.audit_path = self.root / 'T Logs/audit.json'
        self.raw = self.make_raw()
        self.write_capture()
        self.write_audit()

    def tearDown(self):
        self.temp.cleanup()

    def make_raw(self):
        rows = []
        cleanup = []
        for key in self.keys:
            rows.append({
                'key': key,
                'configured_duration_bits': '3f800000',
                'death_animation_index': 0,
                'has_death_animation_selection': True,
                'configured_wreck_bypasses_prepare': False,
                'legacy_wreck_bypasses_prepare': False,
                'actual_original_prepare_bits': '40400000',
                'reversed_child_original_prepare_bits': '40400000',
                'original_fold_bits': '40400000',
                'unit_death_time_remaining_assignment_bits': '40400000',
                'visual_readset': [{
                    'has_animator_link': True, 'animator_exists': True, 'blob_created': True,
                    'animation_count': 1, 'clips': [{'frames': 30, 'fps': 30, 'speed': 1}],
                }],
            })
            cleanup.append({'key': key, 'mission_lifecycle_hooks_executed': False,
                            'after_original_parent_cleanup': [{'exists': False}]})
        return {
            'schema_version': 1, 'kind': MODULE.EXPECTED_KIND, 'authority': MODULE.EXPECTED_AUTHORITY,
            'source_pins': {str(self.keys_path.resolve()): self.key_sha, str(self.source_path.resolve()): hashlib.sha256(self.source_path.read_bytes()).hexdigest()},
            'keys_sha256': self.key_sha, 'rows': rows, 'native_cleanup_rows': cleanup,
            'full_mission_initialized': False, 'renderer_qualified': False, 'runtime_admitted': False,
            'selected_prefab_set_mode': 'ExplicitHashPinnedGenericSet',
        }

    def write_capture(self):
        self.capture_path.write_text(json.dumps(self.raw, indent=2) + '\n')

    def write_audit(self):
        capture_bytes = self.capture_path.read_bytes()
        log_bytes = self.log_path.read_bytes()
        self.audit_path.write_text(json.dumps({
            'kind': 'S050DeathMechanicalReadsetAudit', 'status': 'PassedForMechanicalReadsetOnly',
            'readset': {'path': str(self.capture_path.resolve()), 'sha256': hashlib.sha256(capture_bytes).hexdigest()},
            'exact_pass_marker': MODULE.EXPECTED_MARKER,
            'source_pin_audit': {'count': len(self.raw['source_pins']), 'mismatches': 0},
            'prefab_key_set': {'path': str(self.keys_path.resolve()), 'sha256': self.key_sha, 'prefabs': 16},
            'unity_log': {'path': str(self.log_path.resolve()), 'source_path': str(self.log_path.resolve()),
                          'sha256': hashlib.sha256(log_bytes).hexdigest()},
            'flags': {'full_mission_initialized': False, 'renderer_qualified': False, 'runtime_admitted': False},
        }, indent=2) + '\n')

    def project(self):
        return MODULE.make_projection(self.capture_path, self.audit_path, self.keys_path,
                                      self.root, expected_key_sha=self.key_sha)

    def test_projects_bounded_source_diagnostic_without_admission_claims(self):
        projected, receipt = self.project()
        self.assertLess(len(projected), MODULE.MAX_PROJECTION_BYTES)
        doc = json.loads(projected)
        self.assertEqual(doc['kind'], 'S050SourceDiagnosticDeathMechanicalProjection')
        self.assertFalse(doc['runtime_admitted'])
        self.assertFalse(doc['mission_ready'])
        self.assertEqual(len(doc['rows']), 16)
        self.assertEqual(receipt['status'], 'ProjectedSourceDiagnosticOnly')

    def test_rejects_stale_source_pin(self):
        self.source_path.write_text('changed after capture\n')
        with self.assertRaisesRegex(ValueError, 'source pin drift'):
            self.project()

    def test_rejects_prepare_or_child_order_mismatch(self):
        self.raw['rows'][3]['reversed_child_original_prepare_bits'] = '00000000'
        self.write_capture()
        self.write_audit()
        with self.assertRaisesRegex(ValueError, 'child-order'):
            self.project()

    def test_rejects_invalid_clip_count_and_unbounded_clip_domain(self):
        self.raw['rows'][0]['visual_readset'][0]['clips'] = [{'frames': 1, 'fps': 30, 'speed': 1}] * 4097
        self.raw['rows'][0]['visual_readset'][0]['animation_count'] = 4097
        self.write_capture()
        self.write_audit()
        with self.assertRaisesRegex(ValueError, 'clip domain'):
            self.project()

    def test_rejects_owner_log_without_exact_pass_marker(self):
        self.log_path.write_text('result=Passed prefabs=16\n')
        self.write_audit()
        with self.assertRaisesRegex(ValueError, 'pass marker absent'):
            self.project()

    def test_rejects_admission_claim_in_capture_or_audit(self):
        self.raw['runtime_admitted'] = True
        self.write_capture()
        self.write_audit()
        with self.assertRaisesRegex(ValueError, 'scope/authority'):
            self.project()


if __name__ == '__main__':
    unittest.main()
