import importlib.util
import json
import sys
import tempfile
import unittest
from pathlib import Path

BASE = Path(__file__).parent
sys.path.insert(0, str(BASE))


def load(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


FINALIZER = load("s002_epoch_neutral_finalizer", BASE / "finalize_epoch_neutral_s002_template_capture.py")
PROJECTOR = load("s002_current_projector", BASE / "project_s002_current_focused_template_capture.py")
CAPTURE = Path("T Logs/SkirmishProduction/s002-focused-templates-refresh-20261105-T1/durable-prefab-reference-data.json").resolve()
EXPORT_CAPTURE = Path("Logs/SkirmishProduction/s002-focused-templates-refresh-20261105-T1/prefab-reference-data.json").resolve()
LOG = Path("T Logs/SkirmishProduction/s002-focused-templates-refresh-20261105-T1/editor.log")
KEYS = Path("/private/tmp/s002-focused-prefab-keys-20261102-r1.json")
REPO = Path(__file__).resolve().parents[4]


class CurrentS002FocusedTemplateTests(unittest.TestCase):
    def finalize(self, key_path=KEYS, key_sha=None, wrapper_exit=0, export_capture=EXPORT_CAPTURE):
        return FINALIZER.finalize(
            CAPTURE, export_capture, LOG, wrapper_exit, REPO, [Path("/private/tmp")], key_path,
            key_sha or FINALIZER.sha(Path(key_path).read_bytes()))

    def test_current_capture_finalizes_against_exact_external_key_path_hash(self):
        receipt = self.finalize()
        self.assertEqual(receipt["kind"], "S002CurrentFocusedTemplateCaptureFinalization")
        self.assertEqual(receipt["capture_sha256"], receipt["strict_finalizer"]["capture_sha256"])
        self.assertEqual(receipt["source_pin_count"], 8077)
        self.assertEqual(receipt["capture_path"], str(CAPTURE.resolve()))
        self.assertEqual(receipt["export_capture_path"], str(EXPORT_CAPTURE.resolve()))
        self.assertEqual(receipt["capture_sha256"], receipt["export_capture_sha256"])
        self.assertNotEqual(receipt["capture_path"], receipt["export_capture_path"])
        self.assertEqual(receipt["key_descriptor_path"], str(KEYS.resolve()))
        self.assertFalse(receipt["mission_ready"])
        self.assertFalse(receipt["replay_ready"])
        self.assertFalse(receipt["native_visual_readiness"])

    def test_historical_capture_is_not_silently_rebased(self):
        historical = Path("T Logs/SkirmishProduction/s002-current-baked-templates-20261104-T1/prefab-reference-data.json").resolve()
        historical_export = Path("/private/tmp/s002-current-baked-templates-20261104-T1/prefab-reference-data.json")
        historical_log = Path("T Logs/SkirmishProduction/s002-current-baked-templates-20261104-T1/editor.log")
        pins = json.loads(historical.read_bytes())["source_pins"]
        changed = []
        for name, expected in pins.items():
            path = Path(name)
            path = path if path.is_absolute() else REPO / path
            if path.is_relative_to(REPO) and (not path.is_file() or FINALIZER.sha(path.read_bytes()) != expected):
                changed.append(name)
        if not changed:
            self.skipTest("Historical source pins currently match; no drift to reject")
        with self.assertRaisesRegex(ValueError, "Source pin drift"):
            FINALIZER.finalize(historical, historical_export, historical_log, 0, REPO,
                               [Path("/private/tmp")], KEYS, FINALIZER.sha(KEYS.read_bytes()))

    def test_same_key_bytes_at_unpinned_path_do_not_pass(self):
        with tempfile.TemporaryDirectory(prefix="s002-key-path-negative-") as temp:
            copied = Path(temp) / "keys.json"
            copied.write_bytes(KEYS.read_bytes())
            with self.assertRaisesRegex(ValueError, "exact external key descriptor path and SHA"):
                self.finalize(copied)

    def test_nonzero_wrapper_exit_does_not_pass_even_with_log_marker(self):
        with self.assertRaisesRegex(ValueError, "wrapper exit code must be zero"):
            self.finalize(wrapper_exit=1)

    def test_durable_capture_must_match_original_export_bytes(self):
        with tempfile.TemporaryDirectory(prefix="s002-export-copy-negative-") as temp:
            altered = Path(temp) / "prefab-reference-data.json"
            altered.write_bytes(EXPORT_CAPTURE.read_bytes() + b" ")
            with self.assertRaisesRegex(ValueError, "differs from the exact exporter output bytes"):
                self.finalize(export_capture=altered)

    def test_exporter_locator_must_be_distinct_from_durable_copy(self):
        with self.assertRaisesRegex(ValueError, "Exporter output path must remain distinct"):
            self.finalize(export_capture=CAPTURE)

    def test_projection_binds_receipt_and_keeps_false_readiness(self):
        with tempfile.TemporaryDirectory(prefix="s002-current-project-test-") as temp:
            root = Path(temp)
            finalization_path = root / "finalization.json"
            finalization_path.write_text(json.dumps(self.finalize()), encoding="utf-8")
            result = PROJECTOR.produce(CAPTURE, finalization_path, root / "projected")
            descriptor = json.loads(Path(result["descriptor_path"]).read_text())
            proof = descriptor["current_capture_proof"]
            self.assertEqual(proof["kind"], "S002CurrentFocusedTemplateCaptureProof")
            self.assertEqual(proof["capture_path"], str(CAPTURE.resolve()))
            self.assertEqual(proof["export_capture_path"], str(EXPORT_CAPTURE.resolve()))
            self.assertEqual(proof["capture_sha256"], proof["export_capture_sha256"])
            self.assertNotEqual(proof["capture_path"], proof["export_capture_path"])
            self.assertEqual(proof["source_pin_count"], 8077)
            self.assertEqual(proof["unsupported_presentation_omission_count"], 14)
            self.assertFalse(proof["mission_ready"])
            self.assertFalse(proof["source_fit_ready"])
            self.assertFalse(proof["native_visual_readiness"])
            self.assertFalse(proof["replay_ready"])

    def test_projection_rejects_receipt_readiness_promotion(self):
        with tempfile.TemporaryDirectory(prefix="s002-proof-negative-") as temp:
            root = Path(temp)
            altered = self.finalize()
            altered["mission_ready"] = True
            receipt = root / "bad-finalization.json"
            receipt.write_text(json.dumps(altered), encoding="utf-8")
            with self.assertRaisesRegex(ValueError, "does not match recomputed strict evidence"):
                PROJECTOR.produce(CAPTURE, receipt, root / "projected")

    def test_projection_recomputes_receipt_instead_of_trusting_fabricated_json(self):
        with tempfile.TemporaryDirectory(prefix="s002-fabricated-proof-negative-") as temp:
            root = Path(temp)
            fabricated = self.finalize()
            fabricated["capture_log_sha256"] = "0" * 64
            receipt = root / "fabricated-finalization.json"
            receipt.write_text(json.dumps(fabricated), encoding="utf-8")
            with self.assertRaisesRegex(ValueError, "does not match recomputed strict evidence"):
                PROJECTOR.produce(CAPTURE, receipt, root / "projected")

    def test_projection_rechecks_every_pin_before_accepting_proof(self):
        with tempfile.TemporaryDirectory(prefix="s002-mutated-pin-negative-") as temp:
            root = Path(temp)
            changed_capture = root / "mutated-capture.json"
            capture = json.loads(CAPTURE.read_text())
            first_repo_pin = next(path for path in capture["source_pins"] if path.startswith(str(REPO)))
            capture["source_pins"][first_repo_pin] = "0" * 64
            changed_capture.write_text(json.dumps(capture), encoding="utf-8")
            receipt = root / "original-finalization.json"
            receipt.write_text(json.dumps(self.finalize()), encoding="utf-8")
            with self.assertRaisesRegex(ValueError, "byte-identical to original exporter output"):
                PROJECTOR.produce(changed_capture, receipt, root / "projected")

    def test_existing_view_has_distinct_current_s002_proof_path(self):
        source = (BASE.parent / "Adapters" / "419_GeneratedReferenceTemplateView.cs").read_text()
        controls = (BASE.parent / "Tests" / "CurrentS002TemplateViewProofControls.cs").read_text()
        caller = (BASE.parent / "Adapters" / "307_GeneratedS002StartingWorld.lifetime.cs").read_text()
        s026_host = (BASE.parent / "Adapters" / "S026" / "000-000_AutonomousWorld.cs").read_text()
        self.assertIn("S002CurrentFocusedTemplateCaptureProof", source)
        self.assertIn("ValidateCurrentS002Proof", source)
        self.assertIn("RequireHistoricalS002ExportLocator", source)
        historical = source.split("internal static void RequireHistoricalS002ExportLocator", 1)[1].split("internal static void RequireS002ExportLocator", 1)[0]
        self.assertNotIn("PinnedFile(exportPath", historical)
        self.assertIn("PinnedFile(exportPath,exportSha", source)
        self.assertIn("Durable capture hash must equal the original exporter hash recorded at finalization", source)
        self.assertIn("Path.GetDirectoryName(exportPath)", source)
        self.assertIn("Current S002 external key descriptor is not bound to capture source pins", source)
        self.assertIn("VerifyCurrentS002SourcePins", source)
        self.assertIn("Canonical root-relative S002 source pin required", source)
        self.assertIn("ValidateCurrentS002Omissions", source)
        self.assertIn("ValidateTrustedCurrentS002Root", source)
        self.assertIn("GeneratedReferenceTemplateView.ReadCurrentS002PackageDescriptor(reference,out referenceBinaryDirectory,SourceRepositoryRoot)", caller)
        self.assertIn("GeneratedReferenceTemplateView.Read(reference,out referenceBinaryDirectory,SourceRepositoryRoot)", caller)
        self.assertIn("internal string SourceRepositoryRoot { get; private set; }", s026_host)
        self.assertIn("SourceRepositoryRoot = metadataSourceRepo;", s026_host)
        self.assertIn("QualifiedS002ScopedPrefabSha", source)  # historical exact lane remains
        for negative in ("wrong_s003_proof_rejected", "readiness_promotion_rejected",
                         "changed_source_pin_rejected", "changed_external_key_pin_rejected",
                         "wrong_root_rejected", "parent_symlink_rejected", "inside_root_key_rejected",
                         "historical_export_locator_inside_root_rejected",
                         "historical_export_locator_relative_rejected",
                         "historical_export_locator_noncanonical_rejected",
                         "relative_pin_parent_traversal_rejected",
                         "additional_absolute_external_pin_rejected"):
            self.assertIn(negative, controls)


if __name__ == "__main__":
    unittest.main()
