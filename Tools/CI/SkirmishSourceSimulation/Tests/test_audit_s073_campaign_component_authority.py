import json
import tempfile
import unittest
from pathlib import Path

import audit_s073_campaign_component_authority as authority


REPO = Path(__file__).resolve().parents[4]
ARTIFACT = REPO / "Logs/SkirmishProduction/s073-streamed-source-capture-20261101-r4"


class CapturedCampaignComponentAuthorityTests(unittest.TestCase):
    def test_real_capture_classifies_only_and_rejects_copy(self):
        result = authority.audit(ARTIFACT / "source-world.json", ARTIFACT / "receipt.json", REPO)
        self.assertEqual(result["status"], "Passed_ClassificationOnly_NoImport")
        counts = result["counts"]
        self.assertEqual(counts["storage_components"], 43)
        self.assertEqual(counts["production_queue_components"], 21)
        self.assertEqual(counts["paired_request_buffers"], 21)
        self.assertEqual(counts["serialized_production_requests"], 0)
        self.assertEqual(counts["queue_last_request_id_distribution"], {"0": 21})
        self.assertEqual(counts["request_buffer_length_distribution"], {"0": 21})
        self.assertEqual(counts["authored_production_prefab_length_distribution"],
                         {"1": 3, "3": 9, "4": 3, "7": 1, "10": 3, "11": 2})
        self.assertFalse(result["campaign_runtime_state_copied"])
        self.assertFalse(result["import_admitted"])
        self.assertFalse(result["mission_ready"])

    def test_unknown_storage_field_is_rejected(self):
        data = json.loads((ARTIFACT / "source-world.json").read_text())
        root_id = data["root_entity_ids"][0]
        entity = next(row for row in data["entities"] if row["id"] == root_id)
        component = entity["components"].get(authority.STORAGE)
        if component is None:
            entity = next(row for row in data["entities"]
                          if authority.STORAGE in row.get("components", {}))
            component = entity["components"][authority.STORAGE]
        component["value"]["fields"][authority.STORAGE + "::UnreviewedCampaignState"] = {
            "codec": "integer-v1", "type": "System.Int32", "value": "9"
        }
        with tempfile.TemporaryDirectory() as temp:
            capture = Path(temp) / "tampered.json"
            receipt_path = Path(temp) / "receipt.json"
            capture.write_text(json.dumps(data, separators=(",", ":")))
            receipt = json.loads((ARTIFACT / "receipt.json").read_text())
            changed_sha = authority.sha256(capture)
            receipt["capture_sha256"] = changed_sha
            receipt_path.write_text(json.dumps(receipt))
            original_pin = authority.CAPTURE_SHA256
            authority.CAPTURE_SHA256 = changed_sha
            try:
                with self.assertRaisesRegex(authority.AuditError, "Unknown/missing storage field"):
                    authority.audit(capture, receipt_path, REPO)
            finally:
                authority.CAPTURE_SHA256 = original_pin


if __name__ == "__main__":
    unittest.main()
