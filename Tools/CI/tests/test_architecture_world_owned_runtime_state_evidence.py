#!/usr/bin/env python3

from __future__ import annotations

import gzip
import hashlib
import json
import subprocess
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[3]
EVIDENCE = ROOT / "Design/AgentReports/ArchitectureMaturity/am020_world_owned_runtime_state_evidence.json"


class ArchitectureWorldOwnedRuntimeStateEvidenceTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        cls.payload = json.loads(EVIDENCE.read_text(encoding="utf-8"))

    def test_implementation_identity_and_ancestry_are_exact(self) -> None:
        implementation = self.payload["implementation"]
        final_commit = implementation["finalCommit"]
        self.assertEqual(
            implementation["finalTree"],
            self._git("rev-parse", f"{final_commit}^{{tree}}"),
        )
        subprocess.run(
            ["git", "merge-base", "--is-ancestor", implementation["initialCommit"], final_commit],
            cwd=ROOT,
            check=True,
        )

    def test_final_files_match_the_implementation_commit(self) -> None:
        final_commit = self.payload["implementation"]["finalCommit"]
        for item in self.payload["implementation"]["finalFiles"]:
            content = subprocess.check_output(
                ["git", "show", f"{final_commit}:{item['path']}"],
                cwd=ROOT,
            )
            self.assertEqual(item["sha256"], hashlib.sha256(content).hexdigest(), item["path"])

    def test_validation_logs_are_hash_bound_and_contain_markers(self) -> None:
        for result in self.payload["validation"].values():
            if not isinstance(result, dict) or "log" not in result:
                continue
            path = ROOT / result["log"]["path"]
            self.assertEqual(result["log"]["sha256"], self._sha256(path), str(path))
            with gzip.open(path, "rt", encoding="utf-8", errors="replace") as stream:
                text = stream.read()
            marker = result.get("resultMarker")
            if marker is not None:
                self.assertIn(marker, text)
            else:
                self.assertIn("OK", text)

    def test_contract_and_review_close_am020_without_release_claims(self) -> None:
        self.assertEqual("Passed", self.payload["result"])
        self.assertFalse(self.payload["ownership"]["defaultWorldLookupAdded"])
        self.assertFalse(self.payload["ownership"]["systemBaseAdded"])
        self.assertEqual("PASS", self.payload["independentReview"]["rereviewResult"])
        self.assertEqual("AM-021", self.payload["scope"]["nextTask"])

    @staticmethod
    def _sha256(path: Path) -> str:
        return hashlib.sha256(path.read_bytes()).hexdigest()

    @staticmethod
    def _git(*args: str) -> str:
        return subprocess.check_output(["git", *args], cwd=ROOT, text=True).strip()


if __name__ == "__main__":
    unittest.main()
