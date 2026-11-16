import importlib.util
import json
import tempfile
import unittest
from pathlib import Path
from unittest import mock

MODULE_PATH = Path(__file__).with_name("finalize_s002_focused_prefab_capture.py")
SPEC = importlib.util.spec_from_file_location("finalize_s002_focused_prefab_capture", MODULE_PATH)
MODULE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(MODULE)
CAPTURE_PATH = Path("/private/tmp/s002-focused-prefab-capture-20261102-r1/prefab-reference-data.json")


class S002FocusedPrefabCaptureFinalizerTests(unittest.TestCase):
    def setUp(self):
        self.capture = json.loads(CAPTURE_PATH.read_text(encoding="utf-8"))

    def test_exact_presentation_whitelist_and_entity_namespace(self):
        with tempfile.TemporaryDirectory(prefix="s002-capture-finalizer-") as temp:
            root = Path(temp)
            output = root / "capture"
            output.mkdir()
            capture_path = output / "prefab-reference-data.json"
            capture_path.write_text(json.dumps(self.capture), encoding="utf-8")
            log_path = root / "editor.log"
            log_path.write_text(
                "executeMethod " + MODULE.METHOD + " output=" + str(output.resolve()) + "\n" +
                MODULE.MARKER + "\n", encoding="utf-8")
            with mock.patch.object(MODULE, "verify_pins", return_value=7965):
                receipt = MODULE.validate(capture_path, log_path, 0, root, [])
            self.assertEqual(receipt["validation_status"], "Passed")
            self.assertEqual(receipt["source_pin_count"], 7965)
            self.assertEqual(receipt["unsupported_presentation_omission_count"], 14)
            self.assertFalse(receipt["replay_ready"])
            self.assertFalse(receipt["mission_ready"])

    def test_other_unsupported_value_is_rejected(self):
        unsupported = list(self.capture["unsupported_values"])
        unsupported[0] = dict(unsupported[0], path="1:1/Game.Components.OtherComponent/Prefab")
        with self.assertRaisesRegex(ValueError, "Unsupported required template value"):
            MODULE.classify_unsupported(unsupported)

    def test_wrapper_exit_and_marker_are_fail_closed(self):
        with tempfile.TemporaryDirectory(prefix="s002-capture-finalizer-fail-") as temp:
            root = Path(temp)
            output = root / "capture"
            output.mkdir()
            capture_path = output / "prefab-reference-data.json"
            capture_path.write_text(json.dumps(self.capture), encoding="utf-8")
            log_path = root / "editor.log"
            log_path.write_text(MODULE.METHOD + " " + str(output) + "\n", encoding="utf-8")
            with mock.patch.object(MODULE, "verify_pins", return_value=7965):
                with self.assertRaisesRegex(ValueError, "wrapper exit code"):
                    MODULE.validate(capture_path, log_path, 1, root, [])
                with self.assertRaisesRegex(ValueError, "pass marker"):
                    MODULE.validate(capture_path, log_path, 0, root, [])


if __name__ == "__main__":
    unittest.main()
