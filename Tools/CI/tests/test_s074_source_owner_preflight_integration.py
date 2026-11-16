"""Integration boundary tests for the S074 selected-build early preflight."""
import importlib.util
import json
import shutil
import sys
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch


CI = Path(__file__).resolve().parents[1]
ROOT = CI.parents[1]
sys.dont_write_bytecode = True
spec = importlib.util.spec_from_file_location(
    '_s074_checked_cli_bootstrap', CI / 'run_skirmish_source_simulation.py')
bootstrap = importlib.util.module_from_spec(spec)
sys.modules[spec.name] = bootstrap
spec.loader.exec_module(bootstrap)
source_build = bootstrap.owned.require('skirmish_source_build')

RECEIPT_DIR = ROOT / 'T Logs/SkirmishProduction/s074-both-vehicle-generic-source-only-bundle-20261114-r2'
BUNDLE_PATH = RECEIPT_DIR / 'data-bundle.json'
ADMISSION = json.loads((RECEIPT_DIR / 'source-owner-admission-receipt.json').read_text())
BUILD_PATH = Path(ADMISSION['inputs']['candidate_source_build']['path'])


class S074SelectedBuildPreflightIntegrationTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix='s074-run-preflight-')
        self.addCleanup(self.temp.cleanup)
        self.base = Path(self.temp.name)

    def run_with(self, build_path):
        output = self.base / ('output-' + str(len(list(self.base.iterdir()))))
        return source_build.run(ROOT, build_path, BUNDLE_PATH, output, timeout=1)

    def test_matching_admission_reaches_existing_full_pin_verification(self):
        class ReachedFullVerify(Exception):
            pass

        pins = json.loads(BUILD_PATH.read_text())['source_and_reference_pins']
        with patch.object(source_build, 'verify', side_effect=ReachedFullVerify) as verify:
            with self.assertRaises(ReachedFullVerify):
                self.run_with(BUILD_PATH)
        verify.assert_called_once_with(pins)

    def test_mismatched_selected_build_fails_before_full_pin_verification(self):
        alternate_build = self.base / 'same-build-different-selected-path.json'
        shutil.copyfile(BUILD_PATH, alternate_build)
        with patch.object(source_build, 'verify') as verify:
            with self.assertRaises(ValueError):
                self.run_with(alternate_build)
        verify.assert_not_called()


if __name__ == '__main__':
    unittest.main()
