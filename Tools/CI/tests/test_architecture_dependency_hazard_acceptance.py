#!/usr/bin/env python3

from __future__ import annotations

import hashlib
import json
import subprocess
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[3]
RECORD_PATH = ROOT / "Design/AgentReports/ArchitectureMaturity/am018_acceptance_record.json"
TRACKER_PATH = ROOT / "Design/Architecture/post_hardening_architecture_maturity_tracker.md"


def git_bytes(*arguments: str) -> bytes:
    return subprocess.run(
        ["git", *arguments],
        cwd=ROOT,
        check=True,
        capture_output=True,
    ).stdout


def git_text(*arguments: str) -> str:
    return git_bytes(*arguments).decode("utf-8").strip()


class ArchitectureDependencyHazardAcceptanceTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        cls.record = json.loads(RECORD_PATH.read_text(encoding="utf-8"))

    def test_evidence_commit_tree_and_scope_are_exact(self) -> None:
        evidence = self.record["acceptedEvidence"]
        baseline = self.record["sourceBaseline"]
        self.assertEqual(git_text("rev-parse", f"{evidence['commit']}^{{commit}}"), evidence["commit"])
        self.assertEqual(git_text("rev-parse", f"{evidence['commit']}^{{tree}}"), evidence["tree"])
        self.assertEqual(
            git_text("diff", "--name-only", baseline["commit"], evidence["commit"]).splitlines(),
            evidence["changedPaths"],
        )

    def test_evidence_file_hashes_match_committed_bytes(self) -> None:
        evidence = self.record["acceptedEvidence"]
        for row in evidence["files"]:
            committed = git_bytes("show", f"{evidence['commit']}:{row['path']}")
            self.assertEqual(hashlib.sha256(committed).hexdigest(), row["sha256"], row["path"])

    def test_acceptance_test_and_inventory_summary_are_bound(self) -> None:
        acceptance_test = self.record["acceptanceTest"]
        authority_commits = []
        for commit in git_text("rev-list", "HEAD", "--", acceptance_test["path"]).splitlines():
            committed_test = git_bytes("show", f"{commit}:{acceptance_test['path']}")
            if hashlib.sha256(committed_test).hexdigest() == acceptance_test["sha256"]:
                authority_commits.append(commit)
        self.assertTrue(authority_commits, "The accepted AM-018 test bytes must remain in HEAD history.")
        inventory_path = "Design/AgentReports/ArchitectureMaturity/am018_dependency_hazard_inventory.json"
        inventory = json.loads(git_bytes("show", f"{self.record['acceptedEvidence']['commit']}:{inventory_path}"))
        self.assertEqual(inventory["baseline"], self.record["sourceBaseline"])
        self.assertEqual(inventory["summary"], self.record["inventorySummary"])

    def test_validation_and_tracker_disposition_are_accepted(self) -> None:
        validation = self.record["validation"]
        self.assertEqual(validation["focusedTests"], 11)
        self.assertEqual(validation["integratedArchitectureTests"], 102)
        self.assertEqual(validation["independentRereview"], "PASS")
        tracker = TRACKER_PATH.read_text(encoding="utf-8")
        self.assertIn("- [x] `AM-018` Inventory production uses", tracker)
        self.assertIn("- Next task: `AM-019` defines one standard World-bound query/entity cache contract", tracker)


if __name__ == "__main__":
    unittest.main()
