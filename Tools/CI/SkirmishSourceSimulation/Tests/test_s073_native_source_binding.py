import hashlib
import json
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[4]
EVIDENCE = ROOT / "Logs/SkirmishProduction/s073-native-entity-scene-binding-20261101-r2"


class NativeSourceBindingEvidenceTests(unittest.TestCase):
    def test_strict_r3_receipt_observation_log_and_current_pins_join(self):
        receipt_bytes = (EVIDENCE / "receipt-strict-r3.json").read_bytes()
        receipt = json.loads(receipt_bytes)
        observation_bytes = (EVIDENCE / "observation.json").read_bytes()
        observation = json.loads(observation_bytes)
        log_bytes = (EVIDENCE / "editor.log").read_bytes()
        digest = lambda data: hashlib.sha256(data).hexdigest()

        self.assertEqual(receipt["kind"], "S073NativeEntitySceneBindingFinalization")
        self.assertEqual(receipt["unity_exit_code"], 0)
        self.assertEqual(receipt["recorded_observation"], "Recorded")
        self.assertEqual(receipt["observation_sha256"], digest(observation_bytes))
        self.assertEqual(receipt["unity_log_sha256"], digest(log_bytes))
        self.assertIn(
            "[S073NativeEntitySceneBinding] result=Recorded scope=actual_binding_outcome "
            "binding_read_succeeded=True path=" + receipt["observation_path"],
            log_bytes.decode("utf-8"),
        )
        self.assertEqual(observation["session_id"], receipt["session_id"])
        self.assertEqual(observation["source_pins_sha256"], receipt["source_pins_sha256"])
        self.assertEqual(len(receipt["source_pins_sha256"]), 5)
        for relative, expected in receipt["source_pins_sha256"].items():
            self.assertEqual(digest((ROOT / relative).read_bytes()), expected, relative)

        self.assertEqual(observation["map"]["operation_map_id"], "opmap.skirmish.industrial_basin")
        self.assertEqual(observation["map"]["source_identity_hash"], observation["binding"]["source_identity"])
        self.assertEqual(observation["map"]["source_content_hash"], observation["binding"]["source_content"])
        self.assertEqual(observation["map"]["generated_metadata_hash"], observation["binding"]["generated_metadata"])
        self.assertEqual(observation["grid"]["width"], 2048)
        self.assertEqual(observation["grid"]["height"], 1024)
        self.assertEqual(observation["grid"]["cell_size"], 1.0)
        self.assertEqual(observation["grid"]["origin"], [0.0, 0.0, 0.0])
        self.assertFalse(receipt["mission_ready"])
        self.assertFalse(receipt["player_readiness"])
        self.assertFalse(receipt["normal_input_playthrough"])
        self.assertFalse(receipt["source_fit_certified"])

if __name__ == "__main__":
    unittest.main()
