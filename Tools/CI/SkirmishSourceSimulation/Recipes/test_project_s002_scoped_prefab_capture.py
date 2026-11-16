import importlib.util
import json
import tempfile
import unittest
from pathlib import Path

MODULE_PATH = Path(__file__).with_name("project_s002_scoped_prefab_capture.py")
SPEC = importlib.util.spec_from_file_location("project_s002_scoped_prefab_capture", MODULE_PATH)
MODULE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(MODULE)
CAPTURE = Path("/private/tmp/s002-scoped-prefab-capture-20261102-r1/prefab-reference-data.json")
RECEIPT = Path("/private/tmp/s002-scoped-prefab-capture-20261102-r1/capture-receipt.json")


class S002ScopedPrefabProjectionTests(unittest.TestCase):
    def test_exact_capture_projects_with_receipt_binding_and_false_readiness(self):
        with tempfile.TemporaryDirectory(prefix="s002-scoped-projection-test-") as temp:
            root = Path(temp)
            capture = root / "prefab-reference-data.json"
            receipt = root / "capture-receipt.json"
            capture.write_bytes(CAPTURE.read_bytes())
            receipt.write_bytes(RECEIPT.read_bytes())
            result = MODULE.produce(capture, receipt, root / "projection")
            self.assertEqual(result["capture_sha256"], MODULE.CAPTURE_SHA)
            self.assertEqual(result["capture_receipt_sha256"], MODULE.RECEIPT_SHA)
            self.assertEqual(result["unsupported_presentation_omission_count"], 14)
            self.assertEqual(result["affected_entity_count"], 7)
            self.assertFalse(result["source_fit_ready"])
            self.assertFalse(result["native_visual_readiness"])
            self.assertFalse(result["replay_ready"])
            self.assertFalse(result["mission_ready"])
            projection = Path(result["descriptor_path"]).parent
            descriptor = json.loads(Path(result["descriptor_path"]).read_text())
            manifest = json.loads((projection / "template-slice-manifest.json").read_text())
            domain = json.loads(Path(descriptor["path"]).read_text())
            source = json.loads(capture.read_text())
            self.assertEqual(descriptor["capture_receipt_sha256"], MODULE.RECEIPT_SHA)
            self.assertEqual(manifest["capture_receipt_sha256"], MODULE.RECEIPT_SHA)
            for field in MODULE.FIELDS:
                self.assertEqual(domain[field], source[field])
            self.assertEqual(domain["unsupported_values"], source["unsupported_values"])

    def test_old_failed_capture_is_not_relabelled(self):
        old = Path("/private/tmp/s002-focused-prefab-capture-20261102-r1/prefab-reference-data.json")
        with tempfile.TemporaryDirectory(prefix="s002-old-capture-test-") as temp:
            root = Path(temp)
            capture = root / "old-capture.json"
            capture.write_bytes(old.read_bytes())
            with self.assertRaisesRegex(ValueError, "Exact successful S002 capture"):
                MODULE.produce(capture, RECEIPT, root / "projection")

    def test_non_presentation_omission_fails_closed(self):
        capture = json.loads(CAPTURE.read_text())
        rows = list(capture["unsupported_values"])
        rows[0] = dict(rows[0], path="1:1/Game.Components.OtherComponent/Prefab")
        with self.assertRaisesRegex(ValueError, "Unsupported required template value"):
            MODULE.classify_omissions(rows)

    def test_runtime_consumer_binds_new_capture_and_receipt_hashes(self):
        source = (Path(__file__).resolve().parents[1] /
                  "Adapters/419_GeneratedReferenceTemplateView.cs").read_text(encoding="utf-8")
        self.assertIn(MODULE.CAPTURE_SHA, source)
        self.assertIn(MODULE.RECEIPT_SHA, source)
        self.assertIn("S002FocusedPrefabCaptureReceipt", source)
        self.assertIn("capture_validation_status", source)
        self.assertIn("source_fit_ready", source)


if __name__ == "__main__":
    unittest.main()
