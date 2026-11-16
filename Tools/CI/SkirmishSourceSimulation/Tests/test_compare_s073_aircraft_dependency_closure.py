import copy
import json
import unittest
from pathlib import Path

import compare_s073_aircraft_dependency_closure as comparator


REPO = Path(__file__).resolve().parents[4]


def fixture():
    root = "Assets/Aircraft/root.prefab"
    dep_path = "Assets/Aircraft/root.prefab"
    source_hash, meta_hash = "a" * 64, "b" * 64
    report = {
        "rotor_geometry": {"rows": [{"installed": {"source_path": root}}]},
        "canonical_geometry": {"roles": []},
    }
    provenance = {"sources": [{"importerDependencyGraphs": [{
        "rootAssetPath": root, "status": "Collected", "captured": True,
        "dependencyHashBefore": "abcd1234", "dependencyHashAfter": "abcd1234",
        "dependencies": [{"assetPath": dep_path,
                          "source": {"exists": True, "status": "HashedRawBytes", "sha256": source_hash},
                          "meta": {"exists": True, "status": "HashedRawBytes", "sha256": meta_hash}}],
    }]}]}
    closure = {
        "kind": "S073AircraftDependencyClosureDiagnostic", "schema_version": 1,
        "scope": "PinnedDriftReportAircraftPrefabSources_AssetDatabaseDependenciesAndRawFileHashes_ReadOnly_NoPlayOrGameplayState",
        "status": "Passed", "native_readiness": False, "mission_world_loaded": False,
        "asset_database_closure_recursive": True,
        "sources": [{"source_path": root,
                     "asset_database_dependency_hash": {"status": "Available", "value": "abcd1234"},
                     "closure_status": "Available",
                     "dependencies": [{"asset_path": dep_path,
                                       "raw_bytes": {"status": "Available", "sha256": source_hash},
                                       "meta_bytes": {"status": "Available", "sha256": meta_hash}}]}],
    }
    return closure, provenance, report


class AircraftDependencyClosureComparisonTests(unittest.TestCase):
    def test_real_pinned_capture_exactly_matches_provenance_closures(self):
        result = comparator.run(REPO / comparator.CLOSURE_REL,
                                REPO / comparator.PROVENANCE_REL,
                                REPO / comparator.DRIFT_REL)
        self.assertEqual(result["status"], "Passed_ExactClosureComparison")
        self.assertEqual(result["counts"]["drift_report_expected_root_count"], 16)
        self.assertEqual(result["counts"]["roots_compared"], 16)
        self.assertEqual(result["counts"]["roots_with_matching_asset_database_dependency_hash"], 16)
        self.assertEqual(result["counts"]["dependency_paths_compared"], 493)
        self.assertEqual(result["counts"]["source_and_meta_hash_records_compared"], 986)
        self.assertFalse(result["provenance_report_sha256_matches_installed_native_template_declaration"])
        self.assertFalse(result["authoring_receipt_binds_provenance_report_to_installed_asset"])
        self.assertFalse(result["unity_dependency_hash_cause_proven"])
        self.assertFalse(result["mission_ready"])

    def test_added_dependency_path_is_rejected(self):
        closure, provenance, report = fixture()
        added = copy.deepcopy(closure["sources"][0]["dependencies"][0])
        added["asset_path"] = "Assets/Aircraft/extra.png"
        closure["sources"][0]["dependencies"].append(added)
        result = comparator.compare(closure, provenance, report)
        self.assertEqual(result["status"], "IncompleteOrMismatch")
        self.assertTrue(any("dependency paths added" in e for e in result["errors"]))

    def test_missing_dependency_path_is_rejected(self):
        closure, provenance, report = fixture()
        closure["sources"][0]["dependencies"] = []
        result = comparator.compare(closure, provenance, report)
        self.assertEqual(result["status"], "IncompleteOrMismatch")
        self.assertTrue(any("dependency paths missing" in e for e in result["errors"]))

    def test_byte_tamper_and_unknown_status_fail_closed(self):
        closure, provenance, report = fixture()
        closure["sources"][0]["dependencies"][0]["raw_bytes"]["sha256"] = "c" * 64
        closure["sources"][0]["dependencies"][0]["meta_bytes"]["status"] = "Unknown"
        result = comparator.compare(closure, provenance, report)
        self.assertEqual(result["status"], "IncompleteOrMismatch")
        self.assertTrue(any("bytes differ" in e for e in result["errors"]))
        self.assertTrue(any("unavailable/unknown" in e for e in result["errors"]))

    def test_duplicate_root_is_rejected(self):
        closure, provenance, report = fixture()
        closure["sources"].append(copy.deepcopy(closure["sources"][0]))
        result = comparator.compare(closure, provenance, report)
        self.assertTrue(any("duplicate current roots" in e for e in result["errors"]))


if __name__ == "__main__":
    unittest.main()
