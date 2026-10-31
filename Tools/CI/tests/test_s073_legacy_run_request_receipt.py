import importlib.util
from pathlib import Path
import tempfile
import unittest


RUNNER_PATH = (Path(__file__).resolve().parents[1] /
               "SkirmishSourceSimulation/Tests/run_s073_legacy_placement_owner_harness.py")
SPEC = importlib.util.spec_from_file_location("s073_legacy_owner_runner", RUNNER_PATH)
RUNNER = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(RUNNER)


class S073LegacyRunRequestReceiptTests(unittest.TestCase):
    def test_pins_exact_paths_and_content(self):
        with tempfile.TemporaryDirectory() as directory:
            source = Path(directory) / "captured-input.json"
            source.write_text('{"world":"original"}\n')
            pins = RUNNER.pin_input_files({"physical_map": source})

            self.assertEqual(pins["physical_map"]["path"], str(source.resolve()))
            self.assertEqual(pins["physical_map"]["sha256"], RUNNER.sha(source))
            self.assertTrue(RUNNER.input_pins_replayable(pins))

            source.write_text('{"world":"changed"}\n')
            self.assertFalse(RUNNER.input_pins_replayable(pins))

    def test_missing_input_is_not_replayable(self):
        pins = {"startup": {"path": "/missing/s073-startup.json", "sha256": "0" * 64}}
        self.assertFalse(RUNNER.input_pins_replayable(pins))


if __name__ == "__main__":
    unittest.main()
