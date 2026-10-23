"""Offline tests for additive diagnostics; temporary fixtures only, no Unity/LFS."""
import copy
import csv
import hashlib
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import patch

import skirmish_catalog_diagnostics as subject

ROOT = Path(__file__).resolve().parents[2]
INDEX = ROOT / "Design/AgentReports/SkirmishExpansion/Full120/source-fit-authoring-batches-20261015-v2.json"
GROUND = ROOT / "Tools/CI/SkirmishProductionDrafts/GroundObjective30/ground-objective-thirty.authoring.json"


class DiagnosticTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix="skirmish-diagnostics-")
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        for relative in (subject.CATALOG, subject.MATRIX):
            dest = self.root / relative
            dest.parent.mkdir(parents=True, exist_ok=True)
            dest.write_bytes((ROOT / relative).read_bytes())
        self.doc = json.loads(INDEX.read_text())
        self.pinfile = self.root / "fixture.txt"
        self.pinfile.write_text("Test-only synthetic pinned payload, not physical source evidence")
        self.digest = hashlib.sha256(self.pinfile.read_bytes()).hexdigest()
        self.doc["catalog_sha256"] = self.sha(self.root / subject.CATALOG)
        self.doc["matrix_sha256"] = self.sha(self.root / subject.MATRIX)
        for source in self.doc["source_candidates"].values():
            for k, h in (("sourcePath", "sourceAssetSha256"), ("gridPath", "gridAssetSha256"), ("surfacePath", "surfaceAssetSha256")):
                source[k], source[h] = "fixture.txt", self.digest
        for mission in self.doc["missions"]:
            mission["packet"] = dict(path="fixture.txt", sha256=self.digest)

    @staticmethod
    def sha(path):
        return hashlib.sha256(path.read_bytes()).hexdigest()

    def run_doc(self, doc=None):
        path = self.root / "input.json"
        path.write_text(json.dumps(self.doc if doc is None else doc))
        return subject.diagnose(self.root, inputs=[path])

    @staticmethod
    def codes(report):
        return {e["code"] for e in report["errors"]}

    def test_actual_current_canonical_120_and_360(self):
        report = subject.diagnose(ROOT)
        self.assertTrue(report["canonical_authority_valid"])
        self.assertEqual([], report["errors"])
        self.assertFalse(report["admission_replacement"])

    def test_actual_index_checked_without_changing_any_source(self):
        before = self.sha(INDEX)
        report = subject.diagnose(ROOT, inputs=[INDEX])
        self.assertTrue(report["canonical_authority_valid"])
        self.assertEqual(before, self.sha(INDEX))
        self.assertFalse(report["source_fit_certified"])
        self.assertEqual("NotAdmission_Full120DiagnosticsOnly", report["inputs"][0]["strict_contract"])
        # Actual drift is reported, never repaired or assumed to be a test pass.
        for error in report["errors"]:
            self.assertIn("location", error)
            self.assertIn("source", error)

    def test_valid_fixture_diagnostics_are_not_admission(self):
        report = self.run_doc()
        self.assertEqual([], report["errors"])
        self.assertEqual("NoDiagnosticErrors", report["result"])
        self.assertFalse(report["mission_ready"])

    def test_collects_budget_packet_source_and_logical_errors_together(self):
        self.doc["missions"][0]["canonical_size_rows"][0]["materials_each"] = "999999"
        self.doc["missions"][1]["canonical_size_rows"][1]["player_rifle"] = "0"
        self.doc["missions"][2]["packet"]["sha256"] = "0" * 64
        self.doc["missions"][3]["required_logical_inputs"] = None
        self.doc["source_candidates"]["MP"]["gridAssetSha256"] = "0" * 64
        report = self.run_doc()
        self.assertTrue({"mission.budget", "pin.drift", "logical.requirements"}.issubset(self.codes(report)))
        self.assertEqual(2, sum(e["code"] == "mission.budget" for e in report["errors"]))
        self.assertTrue(any(e["location"] == "/missions/2/packet" for e in report["errors"]))

    def test_missing_duplicate_and_wrong_tuples_all_reported(self):
        path = self.root / subject.CATALOG
        with path.open(newline="") as stream:
            reader = csv.DictReader(stream)
            rows, fields = list(reader), reader.fieldnames
        rows[1]["scenario_id"] = "S001"
        for key in subject.TUPLE:
            rows[2][key] = rows[0][key]
        with path.open("w", newline="") as stream:
            writer = csv.DictWriter(stream, fields)
            writer.writeheader()
            writer.writerows(rows)
        report = self.run_doc()
        self.assertTrue({"catalog.id", "catalog.missing", "catalog.tuple_duplicate", "catalog.tuple_missing"}.issubset(self.codes(report)))
        self.assertNotIn("mission.budget", self.codes(report))
        self.assertTrue(any(b["check"] == "mission_canonical_joins_and_strict_admission" for b in report["blocked_checks"]))

    def test_matrix_duplicate_missing_and_mismatch(self):
        path = self.root / subject.MATRIX
        with path.open(newline="") as stream:
            reader = csv.DictReader(stream)
            rows, fields = list(reader), reader.fieldnames
        rows[1] = dict(rows[0])
        rows[3]["army_profile"] = "INVALID"
        with path.open("w", newline="") as stream:
            writer = csv.DictWriter(stream, fields)
            writer.writeheader()
            writer.writerows(rows)
        report = subject.diagnose(self.root)
        self.assertTrue({"matrix.key", "matrix.missing", "matrix.tuple"}.issubset(self.codes(report)))
        self.assertFalse(report["canonical_authority_valid"])

    def test_bad_catalog_headers_block_joins_but_keep_matrix_shape_checks(self):
        (self.root / subject.CATALOG).write_text("scenario_id,scenario_id\nS001,S002\n")
        report = self.run_doc()
        self.assertIn("csv.header", self.codes(report))
        self.assertNotIn("mission.budget", self.codes(report))
        self.assertTrue(report["blocked_checks"])

    def test_malformed_missions_dont_abort_later_errors(self):
        self.doc["missions"][0] = None
        self.doc["missions"][1]["catalog_id"] = ["bad"]
        self.doc["missions"][2]["packet"] = None
        self.doc["missions"][3]["definition_id"] = "wrong"
        report = self.run_doc()
        self.assertTrue({"mission.shape", "mission.id", "pin.shape", "mission.identity"}.issubset(self.codes(report)))

    def test_path_escape_and_lfs_pointer_are_not_fetched_or_accepted(self):
        self.doc["missions"][0]["packet"] = dict(path="../outside.txt", sha256="0" * 64)
        self.pinfile.write_text("version https://git-lfs.github.com/spec/v1\noid sha256:" + "0" * 64 + "\nsize 99\n")
        report = self.run_doc()
        self.assertIn("pin.escape", self.codes(report))
        self.assertTrue(any("LFS pointer" in e["message"] for e in report["errors"]))
        self.assertTrue(self.pinfile.exists())

    def test_pin_drift_blocks_budget_claim_even_if_catalog_is_valid(self):
        self.doc["matrix_sha256"] = "0" * 64
        self.doc["missions"][0]["canonical_size_rows"] = []
        report = self.run_doc()
        self.assertIn("pin.drift", self.codes(report))
        self.assertNotIn("mission.budget", self.codes(report))
        self.assertTrue(report["blocked_checks"])

    def test_ground_bad_authority_blocks_existing_admission_and_collects_logical_errors(self):
        doc = json.loads(GROUND.read_text())
        doc["missions"][0]["view_intent"] = None
        doc["missions"][1]["pending_gates"] = []
        doc["missions"][2]["typed_objective_areas"] = None
        with patch.object(subject, "validate_ground30", side_effect=AssertionError("Must not call blocked admission")):
            report = self.run_doc(doc)
        self.assertTrue({"logical.view", "logical.gates", "logical.areas"}.issubset(self.codes(report)))
        self.assertEqual("BlockedByAuthority", report["inputs"][0]["strict_contract"])

    def test_hash_matching_but_malformed_authority_blocks(self):
        doc = json.loads(GROUND.read_text())
        bad = self.root / "bad-authority.json"
        bad.write_text('{"missions": []}')
        for key in doc["authority"]:
            doc["authority"][key] = dict(path=bad.name, sha256=self.sha(bad))
        with patch.object(subject, "validate_ground30", side_effect=AssertionError("Malformed authority must block")):
            report = self.run_doc(doc)
        self.assertIn("authority.shape", self.codes(report))
        self.assertNotIn("mission.budget", self.codes(report))

    def ground_with_structurally_valid_authority(self):
        doc = json.loads(GROUND.read_text())
        authorities = {
            "full_index": self.doc,
            "logical_inputs_manifest": {"files": 20, "missions": 120, "families": [dict(path="fixture.txt", sha256=self.digest) for _ in range(20)]},
            "pilot_candidates": {"candidates": [{"catalog_id": sid} for sid in sorted(subject.PILOTS)]},
            "source_survey": {"sources": [{}]},
        }
        for key, value in authorities.items():
            path = self.root / (key + ".json")
            path.write_text(json.dumps(value))
            doc["authority"][key] = dict(path=path.name, sha256=self.sha(path))
        for mission in doc["missions"]:
            mission["packet"] = dict(path="fixture.txt", sha256=self.digest)
            for k, h in (("sourcePath", "sourceAssetSha256"), ("gridPath", "gridAssetSha256"), ("surfacePath", "surfaceAssetSha256")):
                mission["source_candidate"][k], mission["source_candidate"][h] = "fixture.txt", self.digest
        return doc

    def test_existing_strict_admission_is_retained_and_failure_reported(self):
        doc = self.ground_with_structurally_valid_authority()
        with patch.object(subject, "validate_ground30", side_effect=ValueError("original exact gate refused")) as gate:
            report = self.run_doc(doc)
        gate.assert_called_once()
        self.assertEqual("ExistingGround30ContractFailed", report["inputs"][0]["strict_contract"])
        self.assertTrue(any(e["code"] == "existing_contract.failure" and "original exact gate refused" in e["message"] for e in report["errors"]))

    def test_internally_inconsistent_pinned_index_blocks_downstream(self):
        doc = self.ground_with_structurally_valid_authority()
        path = self.root / "full_index.json"
        value = json.loads(path.read_text())
        value["missions"][0]["canonical_size_rows"] = []
        path.write_text(json.dumps(value))
        doc["authority"]["full_index"]["sha256"] = self.sha(path)
        with patch.object(subject, "validate_ground30", side_effect=AssertionError("Invalid index must block")):
            report = self.run_doc(doc)
        self.assertIn("authority.shape", self.codes(report))
        self.assertNotIn("mission.budget", self.codes(report))

    def test_json_duplicate_keys_and_nonfinite_rejected(self):
        path = self.root / "bad.json"
        for text in ('{"kind":1,"kind":2}', '{"value":NaN}', '[]'):
            path.write_text(text)
            report = subject.diagnose(self.root, inputs=[path])
            self.assertIn("json.read", self.codes(report))

    def test_unknown_schema_is_not_partially_admitted(self):
        report = self.run_doc({"schema_version": 1, "kind": "UnsupportedFamily"})
        self.assertIn("input.schema", self.codes(report))

    def test_actual_ground_data_missing_authority_is_explicit(self):
        report = subject.diagnose(ROOT, inputs=[GROUND])
        self.assertTrue(report["canonical_authority_valid"])
        self.assertFalse(report["admission_replacement"])
        # Either the existing strict contract really passes, or it is failed/blocked.
        self.assertIn(report["inputs"][0]["strict_contract"],
                      {"BlockedByAuthority", "BlockedByDiagnostics", "ExistingGround30ContractFailed", "ExistingGround30ContractPassed", "DiagnosticsFailed"})

    def test_cli_has_nonzero_exit_for_errors_and_writes_only_stdout(self):
        path = self.root / "bad.json"
        path.write_text("null")
        result = subprocess.run([sys.executable, "-B", str(ROOT / "Tools/CI/skirmish_catalog_diagnostics.py"),
                                 "--root", str(self.root), "--input", str(path)], capture_output=True, text=True)
        self.assertEqual(1, result.returncode, result.stderr)
        self.assertEqual("ErrorsFound", json.loads(result.stdout)["result"])
        self.assertEqual("null", path.read_text())


if __name__ == "__main__":
    unittest.main()
