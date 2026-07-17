#!/usr/bin/env python3

from __future__ import annotations

import hashlib
import json
import subprocess
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[3]
ACCEPTANCE = ROOT / "Design/AgentReports/ArchitectureMaturity/am020_acceptance_record.json"
TRACKER = ROOT / "Design/Architecture/post_hardening_architecture_maturity_tracker.md"


class ArchitectureWorldOwnedRuntimeStateAcceptanceTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        cls.payload = json.loads(ACCEPTANCE.read_text(encoding="utf-8"))

    def test_evidence_commit_tree_and_files_are_exact(self) -> None:
        accepted = self.payload["acceptedEvidence"]
        self.assertEqual(
            accepted["tree"],
            self._git("rev-parse", f"{accepted['commit']}^{{tree}}"),
        )
        for item in accepted["files"]:
            content = subprocess.check_output(
                ["git", "show", f"{accepted['commit']}:{item['path']}"],
                cwd=ROOT,
            )
            self.assertEqual(item["sha256"], hashlib.sha256(content).hexdigest(), item["path"])

    def test_evidence_descends_from_final_implementation(self) -> None:
        subprocess.run(
            [
                "git",
                "merge-base",
                "--is-ancestor",
                self.payload["sourceBaseline"]["commit"],
                self.payload["acceptedEvidence"]["commit"],
            ],
            cwd=ROOT,
            check=True,
        )

    def test_acceptance_is_core_only_and_routes_to_am021(self) -> None:
        self.assertEqual("Accepted", self.payload["result"])
        self.assertEqual(0, self.payload["validation"]["compilerErrors"])
        self.assertEqual("PASS", self.payload["validation"]["independentRereview"])
        self.assertEqual("AM-021", self.payload["nextTask"])
        self.assertIn("Release-only certification remains intentionally deferred", TRACKER.read_text(encoding="utf-8"))

    def test_tracker_closes_am020_and_advances_progress(self) -> None:
        tracker = TRACKER.read_text(encoding="utf-8")
        self.assertIn("- [x] `AM-020` Move mutable runtime state", tracker)
        self.assertIn("`20 / 86` (`23.3%`)", tracker)
        self.assertIn("`20 / 68` (`29.4%`)", tracker)
        self.assertIn("`AM-021` ready", tracker)

    @staticmethod
    def _git(*args: str) -> str:
        return subprocess.check_output(["git", *args], cwd=ROOT, text=True).strip()


if __name__ == "__main__":
    unittest.main()
