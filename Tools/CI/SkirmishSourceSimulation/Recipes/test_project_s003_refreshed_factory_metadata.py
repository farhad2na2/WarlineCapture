import copy
import json
import unittest
from pathlib import Path

from project_s003_current_factory_metadata import project_payload, read_factory_sources
from project_s003_refreshed_factory_metadata import CAPTURE_SHA


class RefreshedS003FactoryProjectionTests(unittest.TestCase):
    def test_fresh_capture_and_foreign_or_mutated_inputs(self):
        root = Path(__file__).resolve().parents[4]
        capture = json.loads((root / "Logs/SkirmishProduction/s003-native-metadata-refresh-20261104-r3/data/s003-native-metadata.json").read_bytes())
        pins = capture["source_pins"]
        sources = read_factory_sources(root)
        result = project_payload(capture, CAPTURE_SHA, pins, sources, CAPTURE_SHA)
        self.assertEqual(10, len(result["building-definitions.json"]["definitions"]))
        self.assertEqual(19, len(result["unit-production-metadata.json"]["units"]))
        with self.assertRaises(ValueError):
            project_payload(capture, "0" * 64, pins, sources, "0" * 64)
        altered = copy.deepcopy(capture)
        altered["context"]["metadata"]["units"][0]["index"] = 999
        with self.assertRaises(ValueError):
            project_payload(altered, CAPTURE_SHA, pins, sources, CAPTURE_SHA)


if __name__ == "__main__":
    unittest.main()
