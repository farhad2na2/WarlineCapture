import importlib.util
import json
import shutil
import tempfile
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[4]
SCRIPT = Path(__file__).with_name("validate_s073_native_r13_diagnostic.py")
EVIDENCE = ROOT / "Logs/SkirmishProduction/s073-native-production-window-20261102-r13-qualified"
SPEC = importlib.util.spec_from_file_location("s073_r13_validator", SCRIPT)
VALIDATOR = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(VALIDATOR)


class S073R13DiagnosticEvidenceTests(unittest.TestCase):
    def test_retained_receipt_and_capture_correlate_as_diagnostic_only(self):
        report = VALIDATOR.validate(EVIDENCE)
        self.assertEqual(report["validation_status"], "Passed", report["errors"])
        self.assertEqual(report["use_class"], "diagnostic_only")
        self.assertEqual(report["native_capture"]["bounds_count"], 1899)
        self.assertEqual(report["native_capture"]["bounds_area"], [903.0, 332.0, 197.0, 197.0])
        self.assertEqual(report["native_capture"]["preferred_cell"], [1001, 430])
        self.assertEqual(report["native_capture"]["bounds_time"], report["native_capture"]["camera_sample_time"])
        self.assertEqual(report["native_capture"]["random_state_observed"], False)
        self.assertEqual(report["wrapper_evidence"]["reported_exit_code"], 0)
        self.assertEqual(report["wrapper_evidence"]["exit_code_basis"],
                         "r13 wrapper result reported by task owner")
        self.assertFalse(report["wrapper_evidence"]["exit_code_independently_verified"])
        self.assertEqual(report["unknown_domains"]["source_revision"], "Unknown_NotPinnedByRetainedReceipt")
        self.assertEqual(report["unknown_domains"]["provider_identity"], "Unknown_NotCapturedInStaticBoundsRow")
        self.assertEqual(report["unknown_domains"]["grid_revision"], "Unknown_NotCapturedInRetainedRows")
        self.assertEqual(report["unknown_domains"]["rng"], "Unknown_random_state_observed_false")
        self.assertFalse(report["replay"])
        self.assertFalse(report["admission"])
        self.assertFalse(report["mission_ready"])

    def _copy_evidence(self, temp):
        target = Path(temp) / "evidence"
        shutil.copytree(EVIDENCE, target)
        return target

    def _validate_fixture(self, directory):
        receipt_path = directory / "qualification-receipt.json"
        return VALIDATOR.validate(directory, VALIDATOR._sha(receipt_path))

    def test_tampered_event_artifact_fails_hash_gate(self):
        with tempfile.TemporaryDirectory() as temp:
            copied = self._copy_evidence(temp)
            with (copied / "delivery.jsonl").open("ab") as stream:
                stream.write(b"{}\n")
            report = self._validate_fixture(copied)
            self.assertEqual(report["validation_status"], "Failed")
            self.assertTrue(any("delivery.jsonl hash mismatch" in error for error in report["errors"]))

    def test_tampered_receipt_fails_pinned_trust_anchor(self):
        with tempfile.TemporaryDirectory() as temp:
            copied = self._copy_evidence(temp)
            receipt_path = copied / "qualification-receipt.json"
            receipt = json.loads(receipt_path.read_text(encoding="utf-8-sig"))
            receipt["session"]["seed"] = 7
            receipt_path.write_text(json.dumps(receipt), encoding="utf-8")
            report = VALIDATOR.validate(copied)
            self.assertEqual(report["validation_status"], "Failed")
            self.assertTrue(any("qualification receipt SHA-256" in error for error in report["errors"]))

    def test_cross_session_row_fails_even_when_artifact_hash_is_resealed(self):
        with tempfile.TemporaryDirectory() as temp:
            copied = self._copy_evidence(temp)
            receipt_path = copied / "qualification-receipt.json"
            receipt = json.loads(receipt_path.read_text(encoding="utf-8-sig"))
            delivery_path = copied / "delivery.jsonl"
            rows = VALIDATOR._load_rows(delivery_path)
            rows[0]["session_id"] = "foreign-session"
            raw = "\ufeff" + "\n".join(json.dumps(row, separators=(",", ":")) for row in rows) + "\n"
            delivery_path.write_text(raw, encoding="utf-8")
            receipt["source_artifacts"]["delivery.jsonl"]["sha256"] = VALIDATOR._sha(delivery_path)
            receipt_path.write_text(json.dumps(receipt), encoding="utf-8")
            report = self._validate_fixture(copied)
            self.assertEqual(report["validation_status"], "Failed")
            self.assertTrue(any("other sessions" in error for error in report["errors"]))

    def test_provider_rng_or_source_revision_are_never_promoted_to_known(self):
        report = VALIDATOR.validate(EVIDENCE)
        self.assertTrue(all(value.startswith("Unknown_") for value in report["unknown_domains"].values()))
        self.assertIn("source_revision_provenance", report["prohibited_claims"])
        self.assertNotIn("provider_identity", report["native_capture"])
        self.assertNotIn("grid_revision", report["native_capture"])


if __name__ == "__main__":
    unittest.main()
