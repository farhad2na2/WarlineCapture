import json
import tempfile
import unittest
from pathlib import Path

from finalize_s003_refreshed_metadata import qualify


class RefreshedS003CaptureTests(unittest.TestCase):
    def test_checked_capture_and_fail_closed_controls(self):
        root = Path(__file__).resolve().parents[4]
        capture = root / "Logs/SkirmishProduction/s003-native-metadata-refresh-20261104-r3/data/s003-native-metadata.json"
        log = root / "T Logs/SkirmishProduction/s003-native-metadata-refresh-20261104-T3/editor.log"
        compiler = root / "T Logs/SkirmishProduction/s003-current-original-compiler-20261104-T1/receipt.json"
        receipt = qualify(root, capture, log, compiler, 0)
        self.assertGreaterEqual(receipt["current_source_pins_verified"], 8000)
        self.assertFalse(receipt["mission_ready"])
        with self.assertRaises(ValueError):
            qualify(root, capture, log, compiler, 1)
        with tempfile.TemporaryDirectory() as directory:
            value = json.loads(capture.read_bytes())
            value["context"]["setup_hash"] += 1
            altered = Path(directory) / "capture.json"
            altered.write_text(json.dumps(value))
            with self.assertRaises(ValueError):
                qualify(root, altered, log, compiler, 0)


if __name__ == "__main__":
    unittest.main()
