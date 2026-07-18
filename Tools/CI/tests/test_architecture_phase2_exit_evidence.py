import copy
import hashlib
import json
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / "Tools/CI"))

import architecture_phase2_ownership_delta as delta


def artifact(artifact_id, categories, **extra):
    return {"artifactId": artifact_id, "categories": categories, **extra}


def lifecycle_fixture():
    return artifact(
        "AM-007",
        {
            "nativeContainers": [{"path": "Assets\\A.cs", "ownerType": "Owner", "field": "Cells", "line": 10}],
            "presentationPools": [],
            "queryCaches": [],
            "sceneRoots": [],
            "staticCaches": [{"path": "./Assets/B.cs", "ownerType": "Cache", "field": "Rows", "line": 20}],
            "subscriptions": [],
            "worlds": [],
        },
        policy={"candidateSemantics": "Unmapped lifecycle candidates require explicit follow-up."},
    )


def hazards_fixture():
    return artifact(
        "AM-018",
        {
            "globalWorldLookups": [{
                "path": "Assets/C.cs", "ownerType": "Edge", "memberName": "Boot", "symbol": "World.Default",
                "accessKind": "global-world-property", "disposition": "CE", "rationale": "Explicit composition edge.",
                "protectedOwnerIds": [], "line": 30,
            }],
            "hiddenSingletons": [],
            "mutableStaticCaches": [{
                "path": "Assets/D.cs", "ownerType": "State", "memberName": "<member>", "symbol": "Cache",
                "disposition": "MSL", "rationale": "Needs a lifecycle owner.", "protectedOwnerIds": [], "line": 40,
            }],
            "runtimeObjectDiscovery": [],
            "staticEventSubscriptions": [],
        },
        classification={
            "globalWorldLookups": "Explicit World access classifications.",
            "hiddenSingletons": "Explicit singleton classifications.",
            "mutableStaticCaches": "Explicit static-state classifications.",
            "runtimeObjectDiscovery": "Explicit discovery classifications.",
            "staticEventSubscriptions": "Explicit subscription classifications.",
        },
    )


def ownership_fixture():
    return artifact(
        "AM-021",
        {
            "eventSubscriptions": [],
            "persistentNativeContainers": [{
                "path": "Assets/A.cs", "ownerType": "Owner", "field": "Cells", "status": "explicit",
                "protectedOwnerIds": [], "line": 100,
            }, {
                "path": "Assets/New.cs", "ownerType": "NewOwner", "field": "Data", "status": "explicit",
                "protectedOwnerIds": [], "line": 101,
            }],
            "persistentQueries": [],
            "presentationRoots": [],
        },
    )


def closure_fixture():
    return {
        "artifactId": "AM025-PHASE2-CLOSURE-AUDIT",
        "schemaVersion": 1,
        "projection": {
            "historicalIntakeRowCount": 2,
            "reviewedRowCount": 2,
            "resolvedNonDebtRowCount": 1,
            "protectedDeferredRowCount": 0,
            "genuineDebtRowCount": 1,
            "uniqueDebtItemCount": 1,
            "unclassifiedRowCount": 0,
        },
        "reviewRules": [{
            "id": "baseline-debt",
            "decision": "genuine-debt",
            "reasonCode": "fixture-debt",
            "expectedCount": 1,
            "debtIdPrefix": "FIXTURE-DEBT",
            "ownerDomain": "fixture",
            "workPackageId": "FIXTURE-WP",
            "match": {"sourceArtifact": "AM-007", "sourceCategory": "staticCaches"},
            "authority": ["fixture-authority.json"],
        }, {
            "id": "hazard-resolved",
            "decision": "resolved",
            "reasonCode": "fixture-resolved",
            "expectedCount": 1,
            "match": {"sourceArtifact": "AM-018", "sourceCategory": "mutableStaticCaches"},
            "authority": ["fixture-authority.json"],
        }],
    }


class Phase2OwnershipDeltaTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.root = Path(self.temp.name)
        self.lifecycle = self.write("lifecycle.json", lifecycle_fixture())
        self.hazards = self.write("hazards.json", hazards_fixture())
        self.ownership = self.write("ownership.json", ownership_fixture())

    def tearDown(self):
        self.temp.cleanup()

    def write(self, name, value):
        path = self.root / name
        path.write_text(json.dumps(value, indent=2, sort_keys=True) + "\n", encoding="utf-8")
        return path

    def report(self):
        return delta.build_report(self.lifecycle, self.hazards, self.ownership)

    def reviewed_report(self, closure=None):
        closure_path = self.write("closure.json", closure or closure_fixture())
        self.write("fixture-authority.json", {"accepted": True})
        source = self.root / "Assets/B.cs"
        source.parent.mkdir(parents=True, exist_ok=True)
        source.write_text("static cache fixture\n", encoding="utf-8")
        return delta.build_report(
            self.lifecycle,
            self.hazards,
            self.ownership,
            closure_audit_path=closure_path,
            source_root=self.root,
        )

    def test_rows_are_unique_and_classified_exactly_once(self):
        report = self.report()
        rows = report["baselineClassifications"] + report["hazardClassifications"]
        keys = [(row["sourceArtifact"], row["sourceKey"]) for row in rows]
        self.assertEqual(len(keys), len(set(keys)))
        self.assertEqual({"resolved", "open"}, {row["classification"] for row in rows})
        self.assertEqual(len(rows), sum(report["summary"]["classificationCounts"].values()))

    def test_identical_baseline_candidates_receive_line_independent_occurrences(self):
        lifecycle = lifecycle_fixture()
        duplicate = copy.deepcopy(lifecycle["categories"]["staticCaches"][0])
        duplicate["line"] = 999
        lifecycle["categories"]["staticCaches"].append(duplicate)
        self.write("lifecycle.json", lifecycle)
        rows = self.report()["baselineClassifications"]
        keys = [row["sourceKey"] for row in rows if row["sourceCategory"] == "staticCaches"]
        self.assertEqual(2, len(keys))
        self.assertEqual(2, len(set(keys)))
        self.assertTrue(keys[0].endswith("<occurrence:1-of-2>"))
        self.assertTrue(keys[1].endswith("<occurrence:2-of-2>"))

    def test_identical_hazards_receive_line_independent_occurrences(self):
        hazards = hazards_fixture()
        duplicate = copy.deepcopy(hazards["categories"]["globalWorldLookups"][0])
        duplicate["line"] = 999
        hazards["categories"]["globalWorldLookups"].append(duplicate)
        self.write("hazards.json", hazards)
        rows = self.report()["hazardClassifications"]
        keys = [row["sourceKey"] for row in rows if row["sourceCategory"] == "globalWorldLookups"]
        self.assertEqual(2, len(keys))
        self.assertEqual(2, len(set(keys)))
        self.assertTrue(keys[0].endswith("<occurrence:1-of-2>"))
        self.assertTrue(keys[1].endswith("<occurrence:2-of-2>"))

    def test_output_is_deterministic_and_line_independent(self):
        first = self.report()
        lifecycle = json.loads(self.lifecycle.read_text())
        hazards = json.loads(self.hazards.read_text())
        ownership = json.loads(self.ownership.read_text())
        for document in (lifecycle, hazards, ownership):
            for rows in document["categories"].values():
                for row in rows:
                    row["line"] = row.get("line", 0) + 9000
        self.write("lifecycle.json", lifecycle)
        self.write("hazards.json", hazards)
        self.write("ownership.json", ownership)
        second = self.report()
        first_keys = [row["sourceKey"] for row in first["baselineClassifications"] + first["hazardClassifications"]]
        second_keys = [row["sourceKey"] for row in second["baselineClassifications"] + second["hazardClassifications"]]
        self.assertEqual(first_keys, second_keys)
        self.assertEqual(delta.json_bytes(second), delta.json_bytes(self.report()))

    def test_missing_and_ambiguous_authority_fail_closed(self):
        hazards = hazards_fixture()
        del hazards["categories"]["mutableStaticCaches"][0]["rationale"]
        self.write("hazards.json", hazards)
        with self.assertRaises(delta.DeltaError):
            self.report()

        self.write("hazards.json", hazards_fixture())
        ownership = ownership_fixture()
        ownership["categories"]["persistentNativeContainers"].append(
            copy.deepcopy(ownership["categories"]["persistentNativeContainers"][0])
        )
        self.write("ownership.json", ownership)
        with self.assertRaises(delta.DeltaError):
            self.report()

    def test_unknown_disposition_does_not_become_open(self):
        hazards = hazards_fixture()
        hazards["categories"]["mutableStaticCaches"][0]["disposition"] = "UNKNOWN"
        self.write("hazards.json", hazards)
        with self.assertRaises(delta.DeltaError):
            self.report()

    def test_open_count_and_new_after_baseline_are_explicit(self):
        report = self.report()
        self.assertEqual(2, report["summary"]["openCount"])
        self.assertEqual(1, report["summary"]["newAfterBaselineCount"])
        self.assertEqual("Assets/New.cs", report["newAfterBaseline"][0]["sourcePath"])

    def test_protected_hazard_uses_named_owner_authority(self):
        hazards = hazards_fixture()
        row = hazards["categories"]["mutableStaticCaches"][0]
        row["protectedOwnerIds"] = ["audio"]
        self.write("hazards.json", hazards)
        protected = self.report()["hazardClassifications"][1]
        self.assertEqual("protected-deferred", protected["classification"])
        self.assertEqual(["audio"], protected["protectedOwnerIds"])

    def test_cli_writes_and_check_detects_stale_output(self):
        json_output = self.root / "out/report.json"
        markdown_output = self.root / "out/report.md"
        command = [
            sys.executable, str(ROOT / "Tools/CI/architecture_phase2_ownership_delta.py"),
            "--lifecycle", str(self.lifecycle), "--hazards", str(self.hazards),
            "--ownership", str(self.ownership), "--json-output", str(json_output),
            "--markdown-output", str(markdown_output),
        ]
        subprocess.run(command, check=True, capture_output=True, text=True)
        subprocess.run([*command, "--check"], check=True, capture_output=True, text=True)
        markdown_output.write_text("stale\n", encoding="utf-8")
        checked = subprocess.run([*command, "--check"], capture_output=True, text=True)
        self.assertEqual(1, checked.returncode)
        self.assertIn("stale or missing", checked.stdout)

    def test_row_bound_review_classifies_every_historical_open_row(self):
        report = self.reviewed_report()
        review = report["summary"]["review"]
        self.assertEqual(2, review["historicalInitialOpenRowCount"])
        self.assertEqual(1, review["resolvedNonDebtRowCount"])
        self.assertEqual(1, review["genuineDebtRowCount"])
        self.assertEqual(0, review["unclassifiedRowCount"])
        debt = next(row for row in report["baselineClassifications"] if row["classification"] == "open")
        self.assertEqual("genuine-debt", debt["reviewDecision"])
        self.assertEqual("FIXTURE-DEBT-001", debt["debtId"])
        self.assertEqual(hashlib.sha256((self.root / "Assets/B.cs").read_bytes()).hexdigest(), debt["currentSourceSha256"])
        self.assertEqual(
            hashlib.sha256((self.root / "fixture-authority.json").read_bytes()).hexdigest(),
            debt["reviewAuthority"][0]["sha256"],
        )

    def test_review_rule_count_drift_fails_closed(self):
        closure = closure_fixture()
        closure["reviewRules"][0]["expectedCount"] = 2
        with self.assertRaises(delta.DeltaError):
            self.reviewed_report(closure)

    def test_unclassified_review_row_fails_closed(self):
        closure = closure_fixture()
        closure["reviewRules"] = closure["reviewRules"][:1]
        with self.assertRaises(delta.DeltaError):
            self.reviewed_report(closure)

    def test_missing_review_authority_fails_closed(self):
        closure = closure_fixture()
        closure["reviewRules"][0]["authority"] = ["missing-authority.json"]
        with self.assertRaises(delta.DeltaError):
            self.reviewed_report(closure)


class Phase2ClosureAuditContractTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.policy_path = ROOT / "Design/AgentReports/ArchitectureMaturity/am025_phase2_exit_policy.json"
        cls.audit_path = ROOT / "Design/AgentReports/ArchitectureMaturity/am025_phase2_closure_audit.json"
        cls.policy = json.loads(cls.policy_path.read_text(encoding="utf-8"))
        cls.audit = json.loads(cls.audit_path.read_text(encoding="utf-8"))
        cls.delta_path = ROOT / "Design/AgentReports/ArchitectureMaturity/am025_phase2_ownership_delta.json"
        cls.delta = json.loads(cls.delta_path.read_text(encoding="utf-8"))

    def test_distinct_575_populations_and_projection_arithmetic(self):
        terminology = self.audit["terminology"]
        projection = self.audit["projection"]
        self.assertEqual(575, terminology["am021PersistentResourceCount"])
        self.assertEqual(0, terminology["am021OwnershipGapCount"])
        self.assertEqual(575, terminology["am025HistoricalIntakeRowCount"])
        self.assertEqual(575, projection["reviewedRowCount"])
        self.assertEqual(
            projection["reviewedRowCount"],
            projection["resolvedNonDebtRowCount"]
            + projection["protectedDeferredRowCount"]
            + projection["genuineDebtRowCount"],
        )
        self.assertEqual(550, projection["reviewedNonDebtRowCount"])
        self.assertEqual(25, projection["genuineDebtRowCount"])
        self.assertEqual(21, projection["uniqueDebtItemCount"])
        self.assertEqual(0, projection["unclassifiedRowCount"])
        self.assertFalse(projection["acceptanceCreditGranted"])

    def test_audit_inputs_are_hash_bound(self):
        for item in self.audit["inputs"]:
            path = ROOT / item["path"]
            self.assertTrue(path.is_file(), item["path"])
            self.assertEqual(item["sha256"], hashlib.sha256(path.read_bytes()).hexdigest(), item["path"])

    def test_policy_lists_all_five_live_source_growth_blockers(self):
        blockers = self.policy["currentExternalExitBlockers"]
        rows = blockers["sourceGrowthBlockers"]
        self.assertEqual(5, blockers["sourceGrowthUnresolvedBlockerCount"])
        self.assertEqual(5, len(rows))
        self.assertEqual(5, len({row["path"] for row in rows}))
        for row in rows:
            path = ROOT / row["path"]
            raw = path.read_bytes()
            self.assertEqual(row["sha256"], hashlib.sha256(raw).hexdigest(), row["path"])
            text = raw.decode("utf-8")
            self.assertEqual(row["lines"], len(text.splitlines()), row["path"])
            self.assertEqual(row["bytes"], len(raw), row["path"])
            self.assertEqual("blocked-owner-action", row["status"])
        self.assertEqual(0, blockers["requiredUnresolvedSourceGrowthBlockerCountForAcceptance"])

    def test_documents_keep_projection_non_accepting(self):
        tracker = (ROOT / "Design/Architecture/post_hardening_architecture_maturity_tracker.md").read_text(
            encoding="utf-8"
        )
        package = (ROOT / "Design/Architecture/WorkPackages/am_wp_028_phase2_debt_reconciliation.md").read_text(
            encoding="utf-8"
        )
        self.assertIn("`550` non-debt and `25` genuine-debt", tracker)
        self.assertIn("`21` unique file/rule items", tracker)
        self.assertIn("`25` remaining genuine-debt rows", package)
        self.assertIn("remains non-accepting", package)
        self.assertIn("acceptanceCreditGranted", json.dumps(self.audit, sort_keys=True))
        self.assertIn("requiredGenuineDebtCountForAcceptance", json.dumps(self.policy, sort_keys=True))

    def test_production_delta_has_one_hash_bound_decision_per_intake_row(self):
        self.assertEqual(2, self.delta["schemaVersion"])
        review = self.delta["summary"]["review"]
        self.assertEqual(575, review["historicalInitialOpenRowCount"])
        self.assertEqual(575, review["reviewedRowCount"])
        self.assertEqual(542, review["resolvedNonDebtRowCount"])
        self.assertEqual(8, review["protectedDeferredRowCount"])
        self.assertEqual(25, review["genuineDebtRowCount"])
        self.assertEqual(21, review["uniqueDebtItemCount"])
        self.assertEqual(0, review["unclassifiedRowCount"])
        reviewed = [
            row for row in self.delta["baselineClassifications"] + self.delta["hazardClassifications"]
            if "reviewDecision" in row
        ]
        self.assertEqual(575, len(reviewed))
        self.assertEqual(575, len({(row["sourceArtifact"], row["sourceKey"]) for row in reviewed}))
        debt_rows = [row for row in reviewed if row["reviewDecision"] == "genuine-debt"]
        self.assertEqual(25, len(debt_rows))
        for row in debt_rows:
            source = ROOT / row["sourcePath"]
            self.assertEqual(row["currentSourceSha256"], hashlib.sha256(source.read_bytes()).hexdigest())
        for row in reviewed:
            self.assertTrue(row["reviewAuthority"])
            for authority in row["reviewAuthority"]:
                source = ROOT / authority["path"]
                self.assertEqual(authority["sha256"], hashlib.sha256(source.read_bytes()).hexdigest())

    def test_production_delta_regenerates_byte_identically(self):
        command = [
            sys.executable,
            str(ROOT / "Tools/CI/architecture_phase2_ownership_delta.py"),
            "--lifecycle", "Design/AgentReports/ArchitectureMaturity/lifecycle_inventory.json",
            "--hazards", "Design/AgentReports/ArchitectureMaturity/am018_dependency_hazard_inventory.json",
            "--ownership", "Design/AgentReports/ArchitectureMaturity/am021_persistent_resource_ownership.json",
            "--closure-audit", "Design/AgentReports/ArchitectureMaturity/am025_phase2_closure_audit.json",
            "--source-root", ".",
            "--json-output", "Design/AgentReports/ArchitectureMaturity/am025_phase2_ownership_delta.json",
            "--markdown-output", "Design/AgentReports/ArchitectureMaturity/am025_phase2_ownership_delta.md",
            "--check",
        ]
        subprocess.run(command, cwd=ROOT, check=True, capture_output=True, text=True)


if __name__ == "__main__":
    unittest.main()
