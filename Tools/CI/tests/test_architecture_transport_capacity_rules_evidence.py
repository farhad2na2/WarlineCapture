#!/usr/bin/env python3

from __future__ import annotations

import hashlib
import json
import re
import subprocess
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[3]
EVIDENCE_PATH = ROOT / "Design/AgentReports/ArchitectureMaturity/transport_capacity_rules_extraction_evidence.json"


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


class ArchitectureTransportCapacityRulesEvidenceTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        cls.data = json.loads(EVIDENCE_PATH.read_text(encoding="utf-8"))

    def test_baseline_identity_and_exact_owned_paths(self) -> None:
        baseline = self.data["sourceBaseline"]
        tree = subprocess.run(
            ["git", "rev-parse", f"{baseline['commit']}^{{tree}}"],
            cwd=ROOT,
            check=True,
            capture_output=True,
            text=True,
        ).stdout.strip()
        self.assertEqual(tree, baseline["tree"])
        expected_paths = {
            "Assets/Game/Scripts/Systems/UnitTransportBoardingSystem.cs",
            "Assets/Game/Scripts/Systems/UnitTransportBoardingCapacityRules.cs",
            "Assets/Tests/Editor/UnitTransportBoardingCapacityRulesTests.cs",
        }
        expected_production_paths = {
            "Assets/Game/Scripts/Systems/UnitTransportBoardingCapacityRules.cs",
            "Assets/Game/Scripts/Systems/UnitTransportBoardingSystem.cs",
        }
        self.assertEqual(set(self.data["productionChangePaths"]), expected_production_paths)
        self.assertEqual({entry["path"] for entry in self.data["ownedFiles"]}, expected_paths)
        for entry in self.data["ownedFiles"]:
            self.assertEqual(entry["sha256"], sha256(ROOT / entry["path"]), entry["path"])

        accepted = self.data.get("acceptedEvidence")
        comparison_commit = accepted["commit"] if accepted else "HEAD"
        if accepted:
            accepted_tree = subprocess.run(
                ["git", "rev-parse", f"{comparison_commit}^{{tree}}"],
                cwd=ROOT,
                check=True,
                capture_output=True,
                text=True,
            ).stdout.strip()
            self.assertEqual(accepted_tree, accepted["tree"])
        diff_command = ["git", "diff", "--name-only", baseline["commit"]]
        if accepted:
            diff_command.append(comparison_commit)
        diff_command.extend(["--", "Assets/Game/Scripts"])
        tracked_changes = subprocess.run(
            diff_command,
            cwd=ROOT,
            check=True,
            capture_output=True,
            text=True,
        ).stdout.splitlines()
        untracked_changes = [] if accepted else subprocess.run(
            ["git", "ls-files", "--others", "--exclude-standard", "--", "Assets/Game/Scripts"],
            cwd=ROOT,
            check=True,
            capture_output=True,
            text=True,
        ).stdout.splitlines()
        actual_production_paths = {
            path for path in tracked_changes + untracked_changes if path.endswith(".cs")
        }
        self.assertEqual(actual_production_paths, expected_production_paths)

    def test_validator_is_hash_bound(self) -> None:
        authority = self.data["validatorAuthority"]
        self.assertEqual(authority["path"], str(Path(__file__).resolve().relative_to(ROOT)))
        self.assertEqual(authority["sha256"], sha256(ROOT / authority["path"]))

    def test_rules_are_stateless_and_do_not_own_ecs_or_managed_runtime_boundaries(self) -> None:
        rules = (ROOT / "Assets/Game/Scripts/Systems/UnitTransportBoardingCapacityRules.cs").read_text(encoding="utf-8")
        self.assertIn("internal static class UnitTransportBoardingCapacityRules", rules)
        for forbidden in (
            "EntityManager",
            "EntityQuery",
            "ComponentLookup",
            "BufferLookup",
            "DynamicBuffer",
            "EntityCommandBuffer",
            "SystemBase",
            "MonoBehaviour",
            "UnityEngine.Time",
            "Allocator.",
        ):
            self.assertNotIn(forbidden, rules)
        field_pattern = re.compile(r"^\s*(?:public|internal|private|protected)\s+(?:static\s+)?(?:readonly\s+)?[\w<>]+\s+_?\w+\s*(?:=|;)", re.MULTILINE)
        self.assertEqual(field_pattern.findall(rules), [])

    def test_selected_system_uses_rules_at_job_and_main_thread_boundaries(self) -> None:
        system = (ROOT / "Assets/Game/Scripts/Systems/UnitTransportBoardingSystem.cs").read_text(encoding="utf-8")
        self.assertIn("public partial struct UnitTransportBoardingSystem : ISystem", system)
        self.assertEqual(system.count("UnitTransportBoardingCapacityRules.NormalizePassengerKind("), 1)
        self.assertEqual(system.count("UnitTransportBoardingCapacityRules.ResolveCapacity("), 2)
        self.assertEqual(system.count("UnitTransportBoardingCapacityRules.CountsTowardOccupancy("), 2)
        self.assertNotIn("private static byte ResolvePassengerKind", system)
        self.assertNotIn("private byte ResolvePassengerKind", system)

    def test_six_direct_rule_tests_exist_and_run(self) -> None:
        source = (ROOT / "Assets/Tests/Editor/UnitTransportBoardingCapacityRulesTests.cs").read_text(encoding="utf-8")
        methods = self.data["directRuleTests"]
        self.assertEqual(len(methods), 6)
        self.assertEqual(len(set(methods)), 6)
        for method in methods:
            self.assertEqual(len(re.findall(rf"public\s+void\s+{re.escape(method)}\s*\(", source)), 1)
            self.assertIn(f"test => test.{method}());", source)
        self.assertIn('[TransportBoardingCapacityRules] result=Passed tests=6', source)

    def test_unity_validation_matrix_is_exact_and_green(self) -> None:
        expected = {
            "architecture-naming-contract": 1,
            "capacity-rules": 6,
            "transport-characterization": 6,
            "transport-full-regression": 88,
        }
        validations = self.data["unityValidations"]
        self.assertEqual({entry["id"] for entry in validations}, set(expected))
        for entry in validations:
            self.assertEqual(entry["result"], "Passed")
            self.assertEqual(entry["compilerErrors"], 0)
            self.assertEqual(entry["passedTests"], expected[entry["id"]])
            self.assertTrue(entry["command"].startswith("Tools/CI/invoke_unity_macos.sh "))

    def test_performance_is_zero_allocation_and_not_regressed_from_characterized_baseline(self) -> None:
        performance = self.data["performanceEvidence"]
        baseline_path = ROOT / performance["baselineReportPath"]
        current_path = ROOT / performance["currentReportPath"]
        self.assertEqual(performance["baselineReportSha256"], sha256(baseline_path))
        self.assertEqual(performance["currentReportSha256"], sha256(current_path))
        baseline = json.loads(baseline_path.read_text(encoding="utf-8"))
        current = json.loads(current_path.read_text(encoding="utf-8"))
        self.assertEqual(current["warmupScenarios"], 16)
        self.assertEqual(current["measuredScenarios"], 64)
        self.assertEqual(current["allocatedBytesCurrentThread"], 0)
        self.assertLessEqual(current["averageTotalMs"], baseline["averageTotalMs"] * 1.25)
        self.assertLessEqual(current["p95TotalMs"], baseline["p95TotalMs"] * 1.25)


if __name__ == "__main__":
    unittest.main()
