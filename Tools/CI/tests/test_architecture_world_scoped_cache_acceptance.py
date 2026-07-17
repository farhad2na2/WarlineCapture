#!/usr/bin/env python3

from __future__ import annotations

import hashlib
import json
import subprocess
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[3]
RECORD_PATH = ROOT / "Design/AgentReports/ArchitectureMaturity/am019_acceptance_record.json"
TRACKER_PATH = ROOT / "Design/Architecture/post_hardening_architecture_maturity_tracker.md"


def committed_sha256(commit: str, path: str) -> str:
    content = subprocess.run(
        ["git", "show", f"{commit}:{path}"],
        cwd=ROOT,
        check=True,
        capture_output=True,
    ).stdout
    return hashlib.sha256(content).hexdigest()


class ArchitectureWorldScopedCacheAcceptanceTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        cls.record = json.loads(RECORD_PATH.read_text(encoding="utf-8"))

    def test_record_binds_exact_evidence_commit_and_scope(self) -> None:
        source = self.record["sourceBaseline"]
        accepted = self.record["acceptedEvidence"]
        self.assertEqual(
            source["tree"],
            subprocess.run(
                ["git", "rev-parse", f"{source['commit']}^{{tree}}"],
                cwd=ROOT,
                check=True,
                capture_output=True,
                text=True,
            ).stdout.strip(),
        )
        self.assertEqual(
            accepted["tree"],
            subprocess.run(
                ["git", "rev-parse", f"{accepted['commit']}^{{tree}}"],
                cwd=ROOT,
                check=True,
                capture_output=True,
                text=True,
            ).stdout.strip(),
        )
        subprocess.run(
            ["git", "merge-base", "--is-ancestor", source["commit"], accepted["commit"]],
            cwd=ROOT,
            check=True,
        )
        subprocess.run(
            ["git", "merge-base", "--is-ancestor", accepted["commit"], "HEAD"],
            cwd=ROOT,
            check=True,
        )
        changed = subprocess.run(
            ["git", "diff", "--name-only", source["commit"], accepted["commit"]],
            cwd=ROOT,
            check=True,
            capture_output=True,
            text=True,
        ).stdout.splitlines()
        self.assertEqual(changed, accepted["changedPaths"])
        for path in self.record["preservedUnrelatedWork"]:
            self.assertNotIn(path, changed)

    def test_evidence_files_and_acceptance_authority_are_hash_bound(self) -> None:
        accepted = self.record["acceptedEvidence"]
        for entry in accepted["files"]:
            self.assertEqual(
                entry["sha256"],
                committed_sha256(accepted["commit"], entry["path"]),
                entry["path"],
            )
        authority = self.record["acceptanceTest"]
        self.assertEqual(
            authority["sha256"],
            hashlib.sha256((ROOT / authority["path"]).read_bytes()).hexdigest(),
        )

    def test_validation_and_review_summary_are_accepted(self) -> None:
        self.assertEqual("AM-019", self.record["taskId"])
        self.assertEqual("Accepted", self.record["result"])
        validation = self.record["validation"]
        self.assertEqual(4, validation["acceptanceTests"])
        self.assertEqual(11, validation["unityBehaviorTests"])
        self.assertEqual(2, validation["unityPerformanceTests"])
        self.assertEqual(6, validation["zeroAllocationPhases"])
        self.assertEqual(1, validation["broadArchitectureTests"])
        self.assertEqual(0, validation["compilerErrors"])
        self.assertEqual(24, validation["focusedPythonTests"])
        self.assertEqual(116, validation["integratedArchitectureTests"])
        self.assertEqual("PASS", validation["independentRereview"])

    def test_tracker_advances_to_am020_with_exact_progress(self) -> None:
        tracker = TRACKER_PATH.read_text(encoding="utf-8")
        self.assertIn("| Checklist complete | `19 / 86` (`22.1%`) |", tracker)
        self.assertIn("| Core Architecture Lane | `19 / 68` (`27.9%`); active |", tracker)
        self.assertIn("| Current task | `AM-020` ready, not yet claimed |", tracker)
        self.assertIn("- [x] `AM-019` Define one standard World-bound query/entity cache contract", tracker)
        self.assertIn(self.record["acceptedEvidence"]["commit"], tracker)


if __name__ == "__main__":
    unittest.main()
