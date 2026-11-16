import json
import tempfile
import unittest
from pathlib import Path

from finalize_s003_refreshed_prefab_capture import qualify


class S003RefreshedPrefabCaptureTests(unittest.TestCase):
    def test_checked_capture_rejects_failed_wrapper_and_gameplay_reference(self):
        root = Path(__file__).resolve().parents[4]
        capture = root / "Logs/SkirmishProduction/s003-refreshed-prefab-capture-20261104-r1/data/prefab-reference-data.json"
        log = root / "T Logs/SkirmishProduction/s003-refreshed-prefab-capture-20261104-T1/editor.log"
        result = qualify(root, capture, log, 0)
        self.assertEqual(19, result["roots"])
        self.assertFalse(result["import_admitted"])
        with self.assertRaises(ValueError):
            qualify(root, capture, log, 1)
        with tempfile.TemporaryDirectory() as directory:
            altered = json.loads(capture.read_bytes())
            altered["unsupported_values"][0]["path"] = altered["unsupported_values"][0]["path"].replace(
                "UnitAttackImpactVfxReference", "UnitHealth")
            path = Path(directory) / "capture.json"
            path.write_text(json.dumps(altered))
            with self.assertRaises(ValueError):
                qualify(root, path, log, 0)


if __name__ == "__main__":
    unittest.main()
