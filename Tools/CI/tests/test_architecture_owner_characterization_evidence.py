#!/usr/bin/env python3

from __future__ import annotations

import hashlib
import json
import re
import subprocess
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[3]
EVIDENCE_PATH = ROOT / "Design/AgentReports/ArchitectureMaturity/selected_owner_characterization_evidence.json"


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def git_blob_sha256(commit: str, path: str) -> str:
    content = subprocess.run(
        ["git", "show", f"{commit}:{path}"],
        cwd=ROOT,
        check=True,
        capture_output=True,
    ).stdout
    return hashlib.sha256(content).hexdigest()


def git_history_contains_sha256(path: str, expected: str) -> bool:
    commits = subprocess.run(
        ["git", "rev-list", "--all", "--", path],
        cwd=ROOT,
        check=True,
        capture_output=True,
        text=True,
    ).stdout.splitlines()
    return any(git_blob_sha256(commit, path) == expected for commit in commits)


class ArchitectureOwnerCharacterizationEvidenceTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        cls.data = json.loads(EVIDENCE_PATH.read_text(encoding="utf-8"))

    def test_baseline_and_accepted_characterization_sources_are_immutable(self) -> None:
        baseline = self.data["sourceBaseline"]
        baseline_tree = subprocess.run(
            ["git", "rev-parse", f"{baseline['commit']}^{{tree}}"],
            cwd=ROOT,
            check=True,
            capture_output=True,
            text=True,
        ).stdout.strip()
        self.assertEqual(baseline_tree, baseline["tree"])
        accepted = self.data["acceptedEvidence"]
        accepted_tree = subprocess.run(
            ["git", "rev-parse", f"{accepted['commit']}^{{tree}}"],
            cwd=ROOT,
            check=True,
            capture_output=True,
            text=True,
        ).stdout.strip()
        self.assertEqual(accepted_tree, accepted["tree"])
        for owner in self.data["productionOwners"]:
            self.assertEqual(owner["sha256"], git_blob_sha256(accepted["commit"], owner["path"]))
            diff = subprocess.run(
                ["git", "diff", "--exit-code", baseline["commit"], accepted["commit"], "--", owner["path"]],
                cwd=ROOT,
                capture_output=True,
                text=True,
            )
            self.assertEqual(diff.returncode, 0, diff.stdout + diff.stderr)

    def test_exact_test_files_and_validator_are_hash_bound(self) -> None:
        accepted_commit = self.data["acceptedEvidence"]["commit"]
        self.assertEqual(
            [entry["path"] for entry in self.data["testFiles"]],
            sorted(self.data["allowedTestWritePaths"]),
        )
        for entry in self.data["testFiles"]:
            self.assertEqual(entry["sha256"], git_blob_sha256(accepted_commit, entry["path"]), entry["path"])
        authority = self.data["validatorAuthority"]
        self.assertEqual(authority["path"], Path(__file__).resolve().relative_to(ROOT).as_posix())
        self.assertTrue(git_history_contains_sha256(authority["path"], authority["sha256"]))

    def test_all_nine_characterizations_exist_and_run_in_focused_batches(self) -> None:
        cases = self.data["characterizationCases"]
        self.assertEqual(len(cases), 9)
        self.assertEqual(len({case["id"] for case in cases}), 9)
        self.assertEqual(len({case["testMethod"] for case in cases}), 9)
        dispositions = {"preserved-behavior", "known-limitation"}
        for case in cases:
            self.assertIn(case["disposition"], dispositions)
            self.assertTrue(case["expectedContract"])
            source = (ROOT / case["testPath"]).read_text(encoding="utf-8")
            declaration = rf"public\s+void\s+{re.escape(case['testMethod'])}\s*\("
            self.assertEqual(len(re.findall(declaration, source)), 1, case["testMethod"])
            runner_calls = (
                f"tests.{case['testMethod']}();",
                f"test => test.{case['testMethod']}());",
            )
            self.assertTrue(any(call in source for call in runner_calls), case["testMethod"])
        self.assertEqual(
            {case["ownerId"] for case in cases},
            {"ground-missile-runtime", "transport-boarding-runtime"},
        )

    def test_unity_validation_matrix_is_complete_and_green(self) -> None:
        validations = self.data["unityValidations"]
        expected_passed_tests = {
            "ground-characterization": 3,
            "transport-characterization": 6,
            "transport-full-regression": 88,
            "ground-attack-regression": 5,
            "ground-visual-regression": 1,
        }
        self.assertEqual(len(validations), 5)
        self.assertEqual(len({entry["id"] for entry in validations}), 5)
        self.assertEqual({entry["id"] for entry in validations}, set(expected_passed_tests))
        for entry in validations:
            self.assertEqual(entry["result"], "Passed")
            self.assertEqual(entry["compilerErrors"], 0)
            self.assertTrue(entry["command"].startswith("Tools/CI/invoke_unity_macos.sh "))
            self.assertEqual(entry["passedTests"], expected_passed_tests[entry["id"]])

    def test_performance_reports_are_current_zero_allocation_measurements(self) -> None:
        for entry in self.data["performanceEvidence"]:
            report_path = ROOT / entry["reportPath"]
            self.assertEqual(entry["reportSha256"], sha256(report_path))
            report = json.loads(report_path.read_text(encoding="utf-8"))
            self.assertEqual(report["warmupScenarios"], 16)
            self.assertEqual(report["measuredScenarios"], 64)
            self.assertEqual(report["allocatedBytesCurrentThread"], 0)
            self.assertGreater(report[entry["averageMetric"]], 0)
            self.assertLessEqual(report[entry["averageMetric"]], entry["acceptedAverageCeilingMs"])

    def test_no_production_change_is_claimed(self) -> None:
        self.assertEqual(self.data["productionChangePaths"], [])
        self.assertEqual(
            sorted(self.data["allowedTestWritePaths"]),
            sorted(entry["path"] for entry in self.data["testFiles"]),
        )


if __name__ == "__main__":
    unittest.main()
