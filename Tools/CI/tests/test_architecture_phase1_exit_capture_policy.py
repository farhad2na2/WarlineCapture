#!/usr/bin/env python3

from __future__ import annotations

import hashlib
import json
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[3]
POLICY_PATH = ROOT / "Design/AgentReports/ArchitectureMaturity/phase1_exit_capture_policy.json"


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


class ArchitecturePhase1ExitCapturePolicyTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        cls.data = json.loads(POLICY_PATH.read_text(encoding="utf-8"))

    def test_authorities_are_content_bound(self) -> None:
        self.assertEqual(self.data["taskId"], "AM-017")
        self.assertEqual(self.data["state"], "frozen-before-acceptance-capture")
        for entry in self.data["authorityFiles"]:
            self.assertEqual(entry["sha256"], sha256(ROOT / entry["path"]), entry["path"])

    def test_identity_fails_closed(self) -> None:
        identity = self.data["identity"]
        self.assertTrue(all(identity.values()))

    def test_relative_ceilings_are_exact_and_not_loosened(self) -> None:
        comparison = self.data["comparison"]
        self.assertEqual(comparison["relativeMarginPercent"], 25)
        multiplier = 1.25
        match = comparison["canonicalMatchFrame"]
        self.assertAlmostEqual(match["relativeP95CeilingMs"], match["historicalP95Ms"] * multiplier)
        self.assertLess(match["relativeP95CeilingMs"], match["absoluteP95CeilingMs"])
        self.assertEqual(match["acceptance"], "both-relative-and-absolute")

        for owner in ("groundMissile", "transportBoarding"):
            budget = comparison[owner]
            self.assertAlmostEqual(budget["averageTotalMsCeiling"], budget["baselineAverageTotalMs"] * multiplier)
            self.assertAlmostEqual(budget["p95TotalMsCeiling"], budget["baselineP95TotalMs"] * multiplier)
            self.assertEqual(budget["allocatedBytesCeiling"], 0)

    def test_measurement_windows_and_gc_limits_match_scorecard(self) -> None:
        comparison = self.data["comparison"]
        self.assertEqual(comparison["repeatedBatchCount"], 3)
        self.assertEqual(comparison["canonicalMatchGc"], {
            "warmupFrames": 180,
            "measuredFrames": 300,
            "playerRelevantAllocatedBytesCeiling": 1024,
        })
        self.assertEqual(comparison["changedOwnerFocusedGc"], {
            "warmupFrames": 180,
            "measuredFrames": 300,
            "allocatedBytesCeiling": 0,
        })
        shell = comparison["resourceExchangeShell"]
        self.assertEqual(shell["warmupFrames"], 180)
        self.assertEqual(shell["measuredFrames"], 300)
        self.assertEqual(shell["unchangedStateP95MsCeiling"], 20.0)
        self.assertEqual(shell["unchangedStateAllocatedBytesCeiling"], 0)
        self.assertEqual(shell["warmupOpenCloseTransitions"], 1)
        self.assertGreaterEqual(shell["measuredOpenCloseTransitions"], 100)
        self.assertEqual(shell["openP95MsCeiling"], 20.0)
        self.assertEqual(shell["closeP95MsCeiling"], 20.0)
        self.assertEqual(shell["recurringTransitionAllocatedBytesCeiling"], 0)


if __name__ == "__main__":
    unittest.main()
