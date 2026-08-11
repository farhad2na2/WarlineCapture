#!/usr/bin/env python3

from __future__ import annotations

import hashlib
import json
import subprocess
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[3]
EVIDENCE_PATH = ROOT / "Design/AgentReports/ArchitectureMaturity/query_cache_consolidation_evidence.json"


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def committed_sha256(commit: str, path: str) -> str:
    content = subprocess.run(
        ["git", "show", f"{commit}:{path}"],
        cwd=ROOT,
        check=True,
        capture_output=True,
    ).stdout
    return hashlib.sha256(content).hexdigest()


class ArchitectureQueryCacheConsolidationEvidenceTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        cls.data = json.loads(EVIDENCE_PATH.read_text(encoding="utf-8"))

    def test_identity_hashes_and_exact_production_scope(self) -> None:
        baseline = self.data["sourceBaseline"]
        tree = subprocess.run(
            ["git", "rev-parse", f"{baseline['commit']}^{{tree}}"], cwd=ROOT, check=True,
            capture_output=True, text=True).stdout.strip()
        self.assertEqual(tree, baseline["tree"])
        accepted = self.data.get("acceptedEvidence")
        diff_command = ["git", "diff", "--name-only", baseline["commit"]]
        if accepted:
            accepted_tree = subprocess.run(
                ["git", "rev-parse", f"{accepted['commit']}^{{tree}}"], cwd=ROOT, check=True,
                capture_output=True, text=True).stdout.strip()
            self.assertEqual(accepted_tree, accepted["tree"])
            subprocess.run(
                ["git", "merge-base", "--is-ancestor", baseline["commit"], accepted["commit"]],
                cwd=ROOT,
                check=True,
            )
            subprocess.run(
                ["git", "merge-base", "--is-ancestor", accepted["commit"], "HEAD"],
                cwd=ROOT,
                check=True,
            )
            diff_command.append(accepted["commit"])
            for entry in self.data["ownedFiles"]:
                self.assertEqual(
                    entry["sha256"],
                    committed_sha256(accepted["commit"], entry["path"]),
                    entry["path"],
                )
            authority = self.data["validatorAuthority"]
            self.assertEqual(authority["path"], Path(__file__).resolve().relative_to(ROOT).as_posix())
            self.assertEqual(
                authority["sha256"],
                committed_sha256(accepted["commit"], authority["path"]),
            )
        else:
            for entry in self.data["ownedFiles"]:
                self.assertEqual(entry["sha256"], sha256(ROOT / entry["path"]), entry["path"])
            authority = self.data["validatorAuthority"]
            self.assertEqual(authority["path"], Path(__file__).resolve().relative_to(ROOT).as_posix())
            self.assertEqual(authority["sha256"], sha256(ROOT / authority["path"]))
        diff_command.extend(["--", "Assets/Game/Scripts"])
        changed = subprocess.run(diff_command, cwd=ROOT, check=True, capture_output=True, text=True).stdout.splitlines()
        if not accepted:
            changed += subprocess.run(
                ["git", "ls-files", "--others", "--exclude-standard", "--", "Assets/Game/Scripts"],
                cwd=ROOT, check=True, capture_output=True, text=True).stdout.splitlines()
        actual = {path for path in changed if path.endswith(".cs")}
        self.assertEqual(actual, set(self.data["productionChangePaths"]))

    def test_old_single_component_wrappers_are_removed(self) -> None:
        for path in self.data["removedProductionPaths"]:
            self.assertFalse((ROOT / path).exists(), path)

    def test_shared_cache_remains_one_component_and_world_scoped(self) -> None:
        source = (ROOT / "Assets/Game/Scripts/Systems/WorldScopedComponentQueryCache.cs").read_text(encoding="utf-8")
        self.assertIn("internal sealed class WorldScopedComponentQueryCache<T>", source)
        self.assertIn("where T : unmanaged, IComponentData", source)
        self.assertEqual(source.count("CreateEntityQuery("), 1)
        self.assertIn("_world == world && world != null && world.IsCreated", source)
        for forbidden in ("SystemBase", "MonoBehaviour", "UnityEngine", "static World", "static EntityQuery"):
            self.assertNotIn(forbidden, source)
        faction_resource = (ROOT / "Assets/Game/Scripts/Systems/FactionResourceCompositionSystemHelper.cs").read_text(encoding="utf-8")
        hauler_bridge = (ROOT / "Assets/Game/Scripts/Systems/BuildingResourceHaulerBridgeCompositionSystemHelper.cs").read_text(encoding="utf-8")
        self.assertIn(
            "WorldScopedComponentQueryCache<BuildingResourceStorageComponent> _storageQueryCache = new(readOnly: false)",
            faction_resource,
        )
        self.assertIn(
            "WorldScopedComponentQueryCache<UnitMoveOrderQueueComponent> _moveOrderQueueQueryCache = new(readOnly: true)",
            hauler_bridge,
        )
        self.assertNotIn("BuildingResourceStorageQueryCache", faction_resource)
        self.assertNotIn("UnitMoveOrderQueueQueryCache", hauler_bridge)

    def test_only_identical_query_cache_mechanic_was_consolidated(self) -> None:
        decisions = {entry["category"]: entry for entry in self.data["auditDecisions"]}
        self.assertEqual(set(decisions), {"query-cache", "command-queue", "fixed-capacity-scratch", "projection-cache"})
        self.assertEqual(decisions["query-cache"]["decision"], "consolidated")
        for category in ("command-queue", "fixed-capacity-scratch", "projection-cache"):
            self.assertEqual(decisions[category]["decision"], "retained-separate")
            self.assertTrue(decisions[category]["reason"])

    def test_unity_validation_matrix_is_exact_and_green(self) -> None:
        expected = {"query-cache": 3, "faction-resource": 13, "building-resource": 52, "architecture-contract": 1}
        validations = self.data["unityValidations"]
        self.assertEqual({entry["id"] for entry in validations}, set(expected))
        for entry in validations:
            self.assertEqual(entry["result"], "Passed")
            self.assertEqual(entry["compilerErrors"], 0)
            self.assertEqual(entry["passedTests"], expected[entry["id"]])


if __name__ == "__main__":
    unittest.main()
