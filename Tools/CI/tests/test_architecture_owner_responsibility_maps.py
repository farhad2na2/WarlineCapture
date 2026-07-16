#!/usr/bin/env python3

from __future__ import annotations

import hashlib
import importlib.util
import json
import subprocess
import sys
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[3]
MAP_PATH = ROOT / "Design/AgentReports/ArchitectureMaturity/selected_owner_responsibility_maps.json"
RANKING_PATH = ROOT / "Design/AgentReports/ArchitectureMaturity/owner_risk_ranking.json"
RENDERER_PATH = ROOT / "Tools/CI/architecture_owner_responsibility_maps.py"
SPEC = importlib.util.spec_from_file_location("architecture_owner_responsibility_maps", RENDERER_PATH)
assert SPEC and SPEC.loader
renderer = importlib.util.module_from_spec(SPEC)
sys.modules[SPEC.name] = renderer
SPEC.loader.exec_module(renderer)

REQUIRED_LIST_FIELDS = (
    "declaredTypes",
    "inputs",
    "outputs",
    "stateAuthority",
    "updateOrder",
    "sideEffects",
    "failureBehavior",
    "tests",
    "allowedDependencies",
    "forbiddenDependencies",
    "boundedExtractionCandidates",
)


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


class ArchitectureOwnerResponsibilityMapTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        cls.data = json.loads(MAP_PATH.read_text(encoding="utf-8"))
        cls.ranking = json.loads(RANKING_PATH.read_text(encoding="utf-8"))

    def test_maps_cover_exact_am009_first_wave(self) -> None:
        selected = {
            row["path"]: row
            for row in self.ranking["rankedCandidates"]
            if row["firstWaveSelected"]
        }
        expected = self.ranking["firstWave"]["selectedPaths"]
        actual = [entry["path"] for entry in self.data["owners"]]
        self.assertEqual(actual, sorted(expected))
        for entry in self.data["owners"]:
            ranked = selected[entry["path"]]
            self.assertEqual(entry["modificationScope"], ranked["modificationScope"])
            self.assertEqual(entry["initialAllowedPaths"], ranked["responsibilityAudit"]["initialAllowedPaths"])
        self.assertEqual(self.data["sourceAuthorities"]["rankingPath"], str(RANKING_PATH.relative_to(ROOT)))
        self.assertEqual(self.data["sourceAuthorities"]["rankingSha256"], sha256(RANKING_PATH))
        authorities = self.data["artifactAuthorities"]
        self.assertEqual(authorities["rendererPath"], str(RENDERER_PATH.relative_to(ROOT)))
        self.assertEqual(authorities["rendererSha256"], sha256(RENDERER_PATH))
        self.assertEqual(authorities["validatorPath"], str(Path(__file__).resolve().relative_to(ROOT)))
        self.assertEqual(authorities["validatorSha256"], sha256(Path(__file__).resolve()))

    def test_each_map_is_bound_to_current_source_and_complete(self) -> None:
        for entry in self.data["owners"]:
            source = ROOT / entry["path"]
            self.assertTrue(source.is_file(), entry["path"])
            self.assertEqual(entry["sourceSha256"], sha256(source), entry["path"])
            self.assertEqual(entry["initialAllowedPaths"], [entry["path"]])
            self.assertTrue(entry["modificationScope"])
            for field in REQUIRED_LIST_FIELDS:
                values = entry[field]
                self.assertIsInstance(values, list, f"{entry['path']}:{field}")
                self.assertTrue(values, f"{entry['path']}:{field}")
                for value in values:
                    self.assertIsInstance(value, dict, f"{entry['path']}:{field}")
                    self.assertTrue(value.get("statement"), f"{entry['path']}:{field}")
                    references = value.get("references")
                    self.assertIsInstance(references, list, f"{entry['path']}:{field}")
                    self.assertTrue(references, f"{entry['path']}:{field}")
                    owner_reference_found = False
                    for reference in references:
                        owner_reference_found |= reference["path"] == entry["path"]
                        self.assertGreater(reference["lineStart"], 0)
                        self.assertGreaterEqual(reference["lineEnd"], reference["lineStart"])
                        reference_path = ROOT / reference["path"]
                        self.assertTrue(reference_path.is_file(), reference["path"])
                        line_count = len(reference_path.read_text(encoding="utf-8").splitlines())
                        self.assertLessEqual(reference["lineEnd"], line_count, reference["path"])
                    if field != "tests":
                        self.assertTrue(owner_reference_found, f"{entry['path']}:{field}:missing owner reference")

    def test_source_baseline_is_git_resolvable_and_sources_are_unchanged(self) -> None:
        baseline = self.data["sourceBaseline"]
        commit_tree = subprocess.run(
            ["git", "rev-parse", f"{baseline['commit']}^{{tree}}"],
            cwd=ROOT,
            check=True,
            capture_output=True,
            text=True,
        ).stdout.strip()
        self.assertEqual(commit_tree, baseline["tree"])
        authorized_changes = {}
        for change in self.data.get("authorizedProductionChanges", []):
            self.assertNotIn(change["path"], authorized_changes)
            evidence_path = ROOT / change["evidencePath"]
            self.assertEqual(change["evidenceSha256"], sha256(evidence_path))
            extraction = json.loads(evidence_path.read_text(encoding="utf-8"))
            self.assertIn(change["path"], extraction["productionChangePaths"])
            extraction_files = {entry["path"]: entry["sha256"] for entry in extraction["ownedFiles"]}
            self.assertEqual(extraction_files[change["path"]], sha256(ROOT / change["path"]))
            authorized_changes[change["path"]] = change
        referenced_paths = {
            reference["path"]
            for entry in self.data["owners"]
            for field in REQUIRED_LIST_FIELDS
            for value in entry[field]
            for reference in value["references"]
        }
        referenced_paths.add(self.data["sourceAuthorities"]["rankingPath"])
        evidence = {entry["path"]: entry["sha256"] for entry in self.data["evidenceFiles"]}
        self.assertEqual(sorted(evidence), sorted(referenced_paths))
        for relative_path in sorted(referenced_paths):
            self.assertEqual(evidence[relative_path], sha256(ROOT / relative_path), relative_path)
            if relative_path.startswith("Assets/Tests/"):
                continue
            if relative_path in authorized_changes:
                continue
            diff = subprocess.run(
                ["git", "diff", "--exit-code", baseline["commit"], "--", relative_path],
                cwd=ROOT,
                capture_output=True,
                text=True,
            )
            self.assertEqual(diff.returncode, 0, diff.stdout + diff.stderr)

    def test_allowed_paths_and_candidates_are_non_overlapping(self) -> None:
        allowed = {
            path
            for entry in self.data["owners"]
            for path in entry["initialAllowedPaths"]
        }
        self.assertEqual(len(allowed), len(self.data["owners"]))
        candidate_paths: set[str] = set()
        for entry in self.data["owners"]:
            for candidate in entry["boundedExtractionCandidates"]:
                status = candidate.get("status", "proposed")
                self.assertIn(status, {"proposed", "completed"})
                output_paths = candidate.get("proposedOutputPaths")
                self.assertIsInstance(output_paths, list)
                self.assertTrue(output_paths)
                self.assertEqual(candidate.get("requiredExpandedAllowedPaths"), output_paths)
                for field in ("authorityBoundary", "proposedUpdateOrder", "cleanupAuthority"):
                    self.assertTrue(candidate.get(field), f"{entry['path']}:{field}")
                if status == "completed":
                    evidence_path = ROOT / candidate["evidencePath"]
                    self.assertEqual(candidate["evidenceSha256"], sha256(evidence_path))
                for output_path in output_paths:
                    self.assertTrue(output_path.startswith("Assets/Game/Scripts/"), output_path)
                    self.assertTrue(output_path.endswith(".cs"), output_path)
                    self.assertNotIn(output_path, candidate_paths)
                    candidate_paths.add(output_path)
        self.assertTrue(allowed.isdisjoint(candidate_paths))

    def test_markdown_summary_is_bound_to_json(self) -> None:
        summary_path = ROOT / self.data["summaryPath"]
        self.assertTrue(summary_path.is_file())
        expected = renderer.render(self.data, sha256(MAP_PATH))
        self.assertEqual(summary_path.read_text(encoding="utf-8"), expected)


if __name__ == "__main__":
    unittest.main()
