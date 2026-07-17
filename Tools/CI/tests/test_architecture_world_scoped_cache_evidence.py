#!/usr/bin/env python3

from __future__ import annotations

import gzip
import hashlib
import json
import subprocess
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[3]
EVIDENCE_PATH = (
    ROOT
    / "Design/AgentReports/ArchitectureMaturity/am019_world_scoped_cache_contract_evidence.json"
)


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def committed_bytes(commit: str, path: str) -> bytes:
    return subprocess.run(
        ["git", "show", f"{commit}:{path}"],
        cwd=ROOT,
        check=True,
        capture_output=True,
    ).stdout


class ArchitectureWorldScopedCacheEvidenceTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        cls.data = json.loads(EVIDENCE_PATH.read_text(encoding="utf-8"))

    def test_commit_identity_ancestry_and_scope_are_exact(self) -> None:
        baseline = self.data["sourceBaseline"]
        implementation = self.data["implementation"]
        self.assertEqual(
            baseline["tree"],
            subprocess.run(
                ["git", "rev-parse", f"{baseline['commit']}^{{tree}}"],
                cwd=ROOT,
                check=True,
                capture_output=True,
                text=True,
            ).stdout.strip(),
        )
        self.assertEqual(
            implementation["tree"],
            subprocess.run(
                ["git", "rev-parse", f"{implementation['commit']}^{{tree}}"],
                cwd=ROOT,
                check=True,
                capture_output=True,
                text=True,
            ).stdout.strip(),
        )
        subprocess.run(
            ["git", "merge-base", "--is-ancestor", baseline["commit"], implementation["commit"]],
            cwd=ROOT,
            check=True,
        )
        subprocess.run(
            ["git", "merge-base", "--is-ancestor", implementation["commit"], "HEAD"],
            cwd=ROOT,
            check=True,
        )

        changed = subprocess.run(
            ["git", "diff", "--name-only", baseline["commit"], implementation["commit"]],
            cwd=ROOT,
            check=True,
            capture_output=True,
            text=True,
        ).stdout.splitlines()
        self.assertEqual(changed, implementation["changedPaths"])
        production = [
            path
            for path in changed
            if path.startswith("Assets/Game/Scripts/") and path.endswith(".cs")
        ]
        self.assertEqual(production, self.data["scope"]["productionChangePaths"])
        for path in self.data["scope"]["preservedUnrelatedWork"]:
            self.assertNotIn(path, changed)

    def test_implementation_files_match_committed_bytes(self) -> None:
        implementation = self.data["implementation"]
        for entry in implementation["files"]:
            self.assertEqual(
                entry["sha256"],
                hashlib.sha256(committed_bytes(implementation["commit"], entry["path"])).hexdigest(),
                entry["path"],
            )

    def test_behavior_performance_and_broad_logs_are_green(self) -> None:
        validation = self.data["validation"]
        for lane in ("behavior", "performance", "broadArchitecture"):
            record = validation[lane]
            log = record["log"]
            path = ROOT / log["path"]
            self.assertEqual(log["sha256"], sha256(path), log["path"])
            with gzip.open(path, "rt", encoding="utf-8", errors="replace") as handle:
                text = handle.read()
            self.assertIn(record["resultMarker"], text)
            self.assertNotIn("error CS", text)
            self.assertNotIn("result=Failed", text)

        performance_log = ROOT / validation["performance"]["log"]["path"]
        with gzip.open(performance_log, "rt", encoding="utf-8", errors="replace") as handle:
            allocation_lines = [
                line
                for line in handle
                if "[WorldScopedComponentQueryCachePerformanceValidation]" in line
                and "allocatedBytes=" in line
            ]
        self.assertEqual(validation["performance"]["measuredPhases"], len(allocation_lines))
        self.assertTrue(all("allocatedBytes=0" in line for line in allocation_lines))

    def test_contract_and_validation_summary_are_fail_closed(self) -> None:
        contract = self.data["contract"]
        self.assertTrue(contract["explicitWorldBinding"])
        self.assertEqual("fail closed with NotSupportedException", contract["enableableSingletonPolicy"])
        self.assertFalse(contract["defaultWorldLookup"])
        self.assertFalse(contract["staticMutableState"])
        self.assertFalse(contract["systemBaseAdded"])

        validation = self.data["validation"]
        self.assertEqual(11, validation["behavior"]["passedTests"])
        self.assertEqual(2, validation["performance"]["passedTests"])
        self.assertEqual(0, validation["performance"]["recurringManagedAllocatedBytesPerPhase"])
        self.assertEqual(1, validation["broadArchitecture"]["passedTests"])
        self.assertEqual(24, validation["focusedPythonTests"])
        self.assertEqual(112, validation["integratedArchitectureTests"])
        for lane in ("behavior", "performance", "broadArchitecture"):
            self.assertEqual(0, validation[lane]["compilerErrors"])

    def test_independent_rereview_passed_after_all_findings_closed(self) -> None:
        review = self.data["independentReview"]
        self.assertEqual("CHANGES_REQUESTED", review["initialResult"])
        self.assertEqual(4, review["findingsClosed"])
        self.assertEqual("PASS", review["rereviewResult"])


if __name__ == "__main__":
    unittest.main()
