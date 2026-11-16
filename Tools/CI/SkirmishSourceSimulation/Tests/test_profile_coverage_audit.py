"""Keep mission profile coverage reports distinct from simulation evidence."""

import importlib.util
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[4]
AUDIT_PATH = ROOT / "Tools/CI/SkirmishSourceSimulation/audit_profile_coverage.py"
SPEC = importlib.util.spec_from_file_location("audit_profile_coverage", AUDIT_PATH)
AUDIT = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(AUDIT)


class ProfileCoverageAuditTests(unittest.TestCase):
    def test_planned_id_set_is_120_and_keeps_preserved_ids(self):
        ids = AUDIT.planned_mission_ids()
        self.assertEqual(len(ids), 120)
        self.assertEqual(len(set(ids)), 120)
        self.assertTrue({"S001", "S002", "S003", "S004", "S025", "S073"}.issubset(ids))
        self.assertEqual(ids[0], "S001")
        self.assertEqual(ids[-1], "S120")

    def test_report_marks_execution_evidence_unassessed(self):
        report = AUDIT.build_report()
        self.assertEqual(report["classification"], "configuration_census_only")
        self.assertEqual(report["planned_mission_count"], 120)
        self.assertEqual(report["profile_manifest_scope"], "default_checked_in_base_manifest_only")
        self.assertIn("not parsed", report["mission_id_basis"])
        self.assertEqual(report["configured_mission_ids"], ["S002", "S026", "S073"])
        self.assertEqual(report["execution_evidence_assessment"], "not_assessed")
        self.assertEqual(report["external_qualification_contexts_assessment"], "not_assessed")
        self.assertEqual(report["s002_derived_seed_assessment"], "not_assessed")
        self.assertEqual(report["missions_without_configured_profile_ranges"], ["S001", "S003-S025", "S027-S072", "S074-S120"])
        s073 = next(row for row in report["configured_profiles"] if row["mission_id"] == "S073")
        self.assertEqual(s073["profile_scope_values"], ["LegacyStartupAndQueueOnly"])
        self.assertIn("external qualification contexts and recorded runs are not assessed", report["unconfigured_mission_profile_reason"])
        self.assertIn("S003-S025", report["missions_without_configured_profile_ranges"])


if __name__ == "__main__":
    unittest.main()
