#!/usr/bin/env python3

from __future__ import annotations

import hashlib
import json
import re
import subprocess
import unittest
from pathlib import Path, PurePosixPath


ROOT = Path(__file__).resolve().parents[3]
EVIDENCE_PATH = ROOT / "Design/AgentReports/ArchitectureMaturity/boundary_equivalence_audit_evidence.json"


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def git(*args: str) -> str:
    return subprocess.run(
        ["git", *args], cwd=ROOT, check=True, capture_output=True, text=True
    ).stdout


def git_file_exists(commit: str, path: str) -> bool:
    return subprocess.run(
        ["git", "cat-file", "-e", f"{commit}:{path}"],
        cwd=ROOT,
        capture_output=True,
    ).returncode == 0


def git_text(commit: str, path: str) -> str:
    return git("show", f"{commit}:{path}")


def assembly_name(commit: str, path: str) -> str:
    source_path = PurePosixPath(path)
    asmdefs = [
        PurePosixPath(item)
        for item in git("ls-tree", "-r", "--name-only", commit, "--", "Assets/Game/Scripts").splitlines()
        if item.endswith(".asmdef") and PurePosixPath(item).parent in source_path.parents
    ]
    if not asmdefs:
        raise AssertionError(f"No assembly owns {path} at {commit}")
    owner = max(asmdefs, key=lambda item: len(item.parts))
    return json.loads(git_text(commit, str(owner)))["name"]


def system_types(source: str) -> list[str]:
    unmanaged = re.findall(
        r"\b(?:partial\s+)?struct\s+(\w+)\s*:\s*[^\n{]*\bISystem\b",
        source,
    )
    managed = re.findall(
        r"\bclass\s+(\w+)\s*:\s*[^\n{]*\bSystemBase\b",
        source,
    )
    return sorted(set(unmanaged + managed))


def update_order_contract(source: str, system_type: str) -> list[str]:
    lines = source.splitlines()
    declaration_index = next(
        index for index, line in enumerate(lines)
        if re.search(rf"partial\s+struct\s+{re.escape(system_type)}\s*:\s*ISystem\b", line)
    )
    contract = [lines[declaration_index].strip()]
    index = declaration_index - 1
    while index >= 0 and lines[index].strip().startswith("["):
        contract.insert(0, lines[index].strip())
        index -= 1
    return contract


class ArchitectureBoundaryEquivalenceEvidenceTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        cls.data = json.loads(EVIDENCE_PATH.read_text(encoding="utf-8"))

    def test_identity_authority_and_documentation_only_scope(self) -> None:
        baseline = self.data["sourceBaseline"]
        self.assertEqual(git("rev-parse", f"{baseline['commit']}^{{tree}}").strip(), baseline["tree"])
        authority = self.data["validatorAuthority"]
        self.assertEqual(authority["path"], str(Path(__file__).resolve().relative_to(ROOT)))
        self.assertEqual(authority["sha256"], sha256(ROOT / authority["path"]))

        accepted = self.data.get("acceptedEvidence")
        end = accepted["commit"] if accepted else "HEAD"
        if accepted:
            self.assertEqual(git("rev-parse", f"{end}^{{tree}}").strip(), accepted["tree"])
        changed = git("diff", "--name-only", baseline["commit"], end, "--", "Assets/Game/Scripts").splitlines()
        self.assertEqual(changed, [])
        if not accepted:
            untracked = git("ls-files", "--others", "--exclude-standard", "--", "Assets/Game/Scripts").splitlines()
            self.assertEqual(untracked, [])

    def test_exact_accepted_phase_one_decomposition_set(self) -> None:
        decompositions = {entry["id"]: entry for entry in self.data["governedDecompositions"]}
        self.assertEqual(
            set(decompositions),
            {"am012-transport-capacity-rules", "am013-world-scoped-query-cache"},
        )
        for entry in decompositions.values():
            path = ROOT / entry["evidencePath"]
            self.assertEqual(entry["evidenceSha256"], sha256(path))
            source = json.loads(path.read_text(encoding="utf-8"))
            self.assertEqual(entry["acceptedCommit"], source["acceptedEvidence"]["commit"])
            self.assertEqual(entry["acceptedTree"], source["acceptedEvidence"]["tree"])
            self.assertEqual(
                git("rev-parse", f"{entry['acceptedCommit']}^{{tree}}").strip(),
                entry["acceptedTree"],
            )

    def test_every_changed_production_type_remains_in_game_runtime(self) -> None:
        for entry in self.data["governedDecompositions"]:
            source = json.loads((ROOT / entry["evidencePath"]).read_text(encoding="utf-8"))
            baseline_commit = source["sourceBaseline"]["commit"]
            accepted_commit = entry["acceptedCommit"]
            observed_before = set()
            observed_after = set()
            for path in source["productionChangePaths"]:
                if git_file_exists(baseline_commit, path):
                    self.assertEqual(assembly_name(baseline_commit, path), entry["assemblyBefore"])
                    observed_before.update(system_types(git_text(baseline_commit, path)))
                if git_file_exists(accepted_commit, path):
                    self.assertEqual(assembly_name(accepted_commit, path), entry["assemblyAfter"])
                    observed_after.update(system_types(git_text(accepted_commit, path)))
            self.assertEqual(sorted(observed_before), entry["systemTypesBefore"])
            self.assertEqual(sorted(observed_after), entry["systemTypesAfter"])
            self.assertEqual(entry["assemblyBefore"], entry["assemblyAfter"])

    def test_update_order_contract_is_unchanged(self) -> None:
        entry = next(
            item for item in self.data["governedDecompositions"]
            if item["id"] == "am012-transport-capacity-rules"
        )
        source_evidence = json.loads((ROOT / entry["evidencePath"]).read_text(encoding="utf-8"))
        path = "Assets/Game/Scripts/Systems/UnitTransportBoardingSystem.cs"
        before = git_text(source_evidence["sourceBaseline"]["commit"], path)
        after = git_text(entry["acceptedCommit"], path)
        self.assertEqual(
            update_order_contract(before, "UnitTransportBoardingSystem"),
            update_order_contract(after, "UnitTransportBoardingSystem"),
        )

    def test_boundary_decision_and_equivalence_coverage_are_fail_closed(self) -> None:
        crossing_ids = []
        for entry in self.data["governedDecompositions"]:
            source = json.loads((ROOT / entry["evidencePath"]).read_text(encoding="utf-8"))
            validations = {item["id"]: item for item in source["unityValidations"]}
            for validation_id in entry["behaviorEquivalenceValidationIds"]:
                self.assertIn(validation_id, validations)
                self.assertEqual(validations[validation_id]["result"], "Passed")
                self.assertEqual(validations[validation_id]["compilerErrors"], 0)
                self.assertGreater(validations[validation_id]["passedTests"], 0)
            crosses = entry["crossesSystemBoundary"] or entry["crossesAssemblyBoundary"]
            if crosses:
                crossing_ids.append(entry["id"])
                self.assertTrue(entry["requiredBoundaryTests"])
            else:
                self.assertEqual(entry["requiredBoundaryTests"], [])
        self.assertEqual(crossing_ids, self.data["crossBoundaryDecompositionIds"])
        self.assertEqual(self.data["crossBoundaryDecompositionIds"], [])
        self.assertEqual(self.data["newBoundaryTestPaths"], [])
        self.assertTrue(self.data["decision"])


if __name__ == "__main__":
    unittest.main()
