import json
import tempfile
import unittest
from pathlib import Path

import audit_s073_aircraft_geometry_dependency_drift as audit_module


REPO = Path(__file__).resolve().parents[4]


class AircraftGeometryDependencyDriftAuditTests(unittest.TestCase):
    def test_pinned_capture_verifies_current_closure_and_reports_binding_limit(self):
        result = audit_module.audit(REPO / audit_module.REPORT_REL,
                                    REPO / audit_module.PROVENANCE_REL, REPO)
        self.assertEqual(result["status"], "Passed_ByteClosureOnly")
        self.assertEqual(result["counts"]["rotor_dependency_hash_mismatch_rows"], 17)
        self.assertEqual(result["counts"]["canonical_dependency_hash_mismatch_sources"], 14)
        self.assertEqual(result["counts"]["unique_mismatched_asset_paths"], 14)
        self.assertEqual(result["counts"]["mismatched_paths_covered"], 14)
        self.assertEqual(result["counts"]["raw_source_and_meta_hash_records_checked"], 1186)
        self.assertEqual(result["counts"]["raw_hash_mismatches"], 0)
        self.assertEqual(result["counts"]["missing_raw_hash_records"], 0)
        self.assertFalse(result["provenance_report_sha256_matches_installed_native_template_declaration"])
        self.assertFalse(result["authoring_receipt_binds_provenance_report_to_installed_asset"])
        self.assertFalse(result["mission_ready"])

    def test_report_tamper_fails_pinned_hash(self):
        with tempfile.TemporaryDirectory() as temp:
            report = Path(temp) / "report.json"
            report.write_text("{}", encoding="utf-8")
            with self.assertRaisesRegex(audit_module.AuditError, "drift report SHA256 mismatch"):
                audit_module.audit(report, REPO / audit_module.PROVENANCE_REL, REPO)

    def test_uncovered_mismatch_path_keeps_audit_incomplete(self):
        with tempfile.TemporaryDirectory() as temp:
            temp = Path(temp)
            repo = temp / "repo"
            repo.mkdir()
            asset = repo / "Assets/Game/Resources/SkirmishAircraftRotorGeometry_V1.asset"
            asset.parent.mkdir(parents=True)
            asset.write_text("NativeTemplateProvenanceSha256: " + audit_module.DECLARED_NATIVE_TEMPLATE_SHA256 + "\n")
            report = {
                "rotor_geometry": {
                    "installed_asset_path": "Assets/Game/Resources/SkirmishAircraftRotorGeometry_V1.asset",
                    "rows": [{"dependency_hash_mismatch": True,
                              "installed": {"source_path": "Assets/uncovered.prefab"}}],
                },
                "canonical_geometry": {"roles": []},
            }
            provenance = {"sources": []}
            report_path = temp / "report.json"
            provenance_path = temp / "provenance.json"
            report_path.write_text(json.dumps(report), encoding="utf-8")
            provenance_path.write_text(json.dumps(provenance), encoding="utf-8")
            old_report_pin, old_provenance_pin = audit_module.REPORT_SHA256, audit_module.PROVENANCE_SHA256
            audit_module.REPORT_SHA256 = audit_module.sha256_file(report_path)
            audit_module.PROVENANCE_SHA256 = audit_module.sha256_file(provenance_path)
            try:
                result = audit_module.audit(report_path, provenance_path, repo)
            finally:
                audit_module.REPORT_SHA256 = old_report_pin
                audit_module.PROVENANCE_SHA256 = old_provenance_pin
            self.assertEqual(result["status"], "Incomplete_ByteClosure")
            self.assertEqual(result["mismatched_paths_uncovered"], ["Assets/uncovered.prefab"])

    def test_changed_dependency_bytes_are_reported(self):
        with tempfile.TemporaryDirectory() as temp:
            temp = Path(temp)
            repo = temp / "repo"
            repo.mkdir()
            asset = repo / "Assets/Game/Resources/SkirmishAircraftRotorGeometry_V1.asset"
            asset.parent.mkdir(parents=True)
            asset.write_text("NativeTemplateProvenanceSha256: " + audit_module.DECLARED_NATIVE_TEMPLATE_SHA256 + "\n")
            dependency = repo / "Assets/source.prefab"
            dependency.parent.mkdir(parents=True, exist_ok=True)
            dependency.write_bytes(b"tampered-current-bytes")
            report = {"rotor_geometry": {"installed_asset_path": str(asset.relative_to(repo)), "rows": []},
                      "canonical_geometry": {"roles": []}}
            provenance = {"sources": [{"importerDependencyGraphs": [{"dependencies": [{
                "assetPath": "Assets/source.prefab",
                "source": {"exists": True, "physicalPath": str(dependency), "sha256": "0" * 64},
                "meta": {"exists": False},
            }]}]}]}
            report_path, provenance_path = temp / "report.json", temp / "provenance.json"
            report_path.write_text(json.dumps(report), encoding="utf-8")
            provenance_path.write_text(json.dumps(provenance), encoding="utf-8")
            old_report_pin, old_provenance_pin = audit_module.REPORT_SHA256, audit_module.PROVENANCE_SHA256
            audit_module.REPORT_SHA256 = audit_module.sha256_file(report_path)
            audit_module.PROVENANCE_SHA256 = audit_module.sha256_file(provenance_path)
            try:
                result = audit_module.audit(report_path, provenance_path, repo)
            finally:
                audit_module.REPORT_SHA256 = old_report_pin
                audit_module.PROVENANCE_SHA256 = old_provenance_pin
            self.assertEqual(result["status"], "Incomplete_ByteClosure")
            self.assertEqual(result["counts"]["raw_hash_mismatches"], 1)


if __name__ == "__main__":
    unittest.main()
