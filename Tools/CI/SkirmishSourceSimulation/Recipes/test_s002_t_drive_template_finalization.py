import importlib.util
import unittest
from pathlib import Path

REPO = Path(__file__).resolve().parents[4]
SCRIPT = Path(__file__).with_name('finalize_epoch_neutral_s002_template_capture.py')
SPEC = importlib.util.spec_from_file_location('s002_t_drive_finalizer', SCRIPT)
FINALIZER = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(FINALIZER)
CAPTURE_DIR = REPO / 'T Logs/SkirmishProduction/s002-focused-templates-refresh-20261105-T1'
CAPTURE = CAPTURE_DIR / 'durable-prefab-reference-data.json'
EXPORT = REPO / 'Logs/SkirmishProduction/s002-focused-templates-refresh-20261105-T1/prefab-reference-data.json'
LOG = CAPTURE_DIR / 'editor.log'
KEY = Path('/private/tmp/s002-focused-prefab-keys-20261102-r1.json')
KEY_SHA = 'f2d08edd3a4e495448d3f4df2ad6608d0180e6428be3d32f4844744dcbf23f09'


class TDriveTemplateFinalizationTests(unittest.TestCase):
    def call(self, export=EXPORT, log=LOG, exit_code=0):
        return FINALIZER.finalize(CAPTURE, export, log, exit_code, REPO, [Path('/private/tmp')], KEY, KEY_SHA)

    def test_actual_checked_project_logs_output_is_accepted(self):
        result = self.call()
        self.assertEqual(result['validation_status'], 'PassedCurrentPinnedSourceOnly')
        self.assertFalse(result['replay_ready'])
        self.assertFalse(result['mission_ready'])

    def test_same_file_and_unrelated_repository_file_are_rejected(self):
        for path in (CAPTURE, REPO / 'Packages/manifest.json'):
            with self.assertRaisesRegex(ValueError, 'Exporter output path must remain distinct'):
                self.call(export=path)

    def test_unrelated_log_and_nonzero_exit_are_rejected(self):
        with self.assertRaises(ValueError):
            self.call(log=REPO / 'T Logs/SkirmishProduction/s002-building-metadata-refresh-20261105-T1/editor.log')
        with self.assertRaisesRegex(ValueError, 'wrapper exit code must be zero'):
            self.call(exit_code=1)


if __name__ == '__main__':
    unittest.main()
