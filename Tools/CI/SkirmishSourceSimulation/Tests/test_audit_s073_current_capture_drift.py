import hashlib
import json
import tempfile
import unittest
from pathlib import Path

from Tools.CI.SkirmishSourceSimulation.Tests.audit_s073_current_capture_drift import audit


class CaptureDriftAuditControls(unittest.TestCase):
    def test_unqualified_receipt_rejected(self):
        with tempfile.TemporaryDirectory() as directory:
            receipt = Path(directory) / "receipt.json"
            receipt.write_text(json.dumps({"status": "Failed", "project_files": 6522, "mission_ready": False}))
            with self.assertRaisesRegex(ValueError, "preservation inventory"):
                audit(Path(directory), receipt)

    def test_changed_capture_rejected_before_inventory_read(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            capture = root / "capture.json"
            capture.write_text("{}")
            receipt = root / "receipt.json"
            receipt.write_text(json.dumps({"status": "Passed", "project_files": 6522, "mission_ready": False,
                                           "capture_path": str(capture), "capture_sha256": "0" * 64}))
            with self.assertRaisesRegex(ValueError, "capture changed"):
                audit(root, receipt)

    def test_duplicate_preservation_row_rejected(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            preserved = root / "preserved"
            (preserved / "Assets").mkdir(parents=True)
            (root / "Assets").mkdir()
            raw = b"fixture"
            (preserved / "Assets/x").write_bytes(raw)
            (root / "Assets/x").write_bytes(raw)
            source_hash = hashlib.sha256(raw).hexdigest()
            capture = root / "capture.json"
            capture.write_text(json.dumps({"readPins": {"Assets/x": source_hash}}))
            row = {"relative_path": "Assets/x", "sha256": source_hash}
            receipt = root / "receipt.json"
            receipt.write_text(json.dumps({"status": "Passed", "project_files": 6522, "mission_ready": False,
                "capture_path": str(capture), "capture_sha256": hashlib.sha256(capture.read_bytes()).hexdigest(),
                "preserved_root": str(preserved), "original_root": "/fixture", "files": [row] * 6522}))
            with self.assertRaisesRegex(ValueError, "Duplicate"):
                audit(root, receipt)


if __name__ == "__main__":
    unittest.main()
